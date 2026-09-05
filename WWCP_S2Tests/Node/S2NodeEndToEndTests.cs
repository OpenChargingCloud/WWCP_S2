/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP S2 <https://github.com/OpenChargingCloud/WWCP_S2>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Node;
using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Node
{

    /// <summary>
    /// End-to-end behaviour of the node layer over real TCP loopback (PLAN.md Phase 10a): pairing
    /// raises <see cref="AS2Node.OnPaired"/> on both nodes and stores a <see cref="Pairing"/> with
    /// the right communication roles, a session forms automatically, the CEM selects FRBC, both
    /// unpair directions close the session, stopping either node ends the peer's session, a
    /// re-pairing keeps exactly one session and pairing, a lost session reconnects, and the events
    /// arrive in a sensible order. These tests use the reconnect back-off, so their timeouts are
    /// generous.
    /// </summary>
    [TestFixture]
    public sealed class S2NodeEndToEndTests
    {

        #region Test data (reused from the node smoke test)

        private static ResourceManagerDetails EVChargerDetails(Resource_Id ResourceId)
            => new (ResourceId,
                    [ new Role(RoleType.EnergyConsumer, Commodity.Electricity) ],
                    Duration.FromMilliseconds(3000),
                    [ ControlType.FillRateBasedControl ],
                    ProvidesForecast:                false,
                    ProvidesPowerMeasurementTypes:   [ CommodityQuantity.ElectricPower3PhaseSymmetric ],
                    Name:                            "My Electric Vehicle RM",
                    Manufacturer:                    "ACME",
                    Model:                           "WallBox-b100");

        private static FRBC_SystemDescription EVChargerSystemDescription()
        {

            var off       = new FRBC_OperationMode(
                                OperationMode_Id.Parse("om1"),
                                [ new FRBC_OperationModeElement(new NumberRange(0, 100), new NumberRange(0, 0),           [ new PowerRange(0, 0, CommodityQuantity.ElectricPower3PhaseSymmetric) ]) ],
                                AbnormalConditionOnly: false,
                                DiagnosticLabel:       "off");

            var charging  = new FRBC_OperationMode(
                                OperationMode_Id.Parse("om2"),
                                [ new FRBC_OperationModeElement(new NumberRange(0, 100), new NumberRange(0.00065, 0.0051), [ new PowerRange(1400, 11000, CommodityQuantity.ElectricPower3PhaseSymmetric) ]) ],
                                AbnormalConditionOnly: false,
                                DiagnosticLabel:       "charging");

            var actuator  = new FRBC_ActuatorDescription(
                                Actuator_Id.Parse("actuator1"),
                                [ Commodity.Electricity ],
                                [ off, charging ],
                                [ new Transition(Transition_Id.Parse("t1"), OperationMode_Id.Parse("om1"), OperationMode_Id.Parse("om2"), [], [], false, TransitionDuration: Duration.FromMilliseconds(3000)),
                                  new Transition(Transition_Id.Parse("t2"), OperationMode_Id.Parse("om2"), OperationMode_Id.Parse("om1"), [], [], false, TransitionDuration: Duration.FromMilliseconds(3000)) ],
                                []);

            var storage   = new FRBC_StorageDescription(
                                ProvidesLeakageBehaviour:        false,
                                ProvidesFillLevelTargetProfile:  true,
                                ProvidesUsageForecast:           false,
                                FillLevelRange:                  new NumberRange(0, 100),
                                DiagnosticLabel:                 "Battery SoC",
                                FillLevelLabel:                  "EV Battery SoC");

            return new FRBC_SystemDescription(DateTimeOffset.UtcNow, [ actuator ], storage);

        }

        private static readonly TimeSpan  ReconnectTimeout  = TimeSpan.FromSeconds(8);

        #endregion


        #region Pairing_RaisesOnPaired_OnBothNodes_WithCorrectRoles()

        [Test]
        public async Task Pairing_RaisesOnPaired_OnBothNodes_WithCorrectRoles()
        {

            await using var fixture = await S2NodeFixture.CreateAsync();

            var cemPaired = new List<Pairing>();
            var rmPaired  = new List<Pairing>();

            fixture.CEM.OnPaired += (_, _, pairing, _) => { lock (cemPaired) cemPaired.Add(pairing); return Task.CompletedTask; };
            fixture.RM. OnPaired += (_, _, pairing, _) => { lock (rmPaired)  rmPaired. Add(pairing); return Task.CompletedTask; };

            await fixture.PairRMWithCEMAsync();

            await S2NodeFixture.WaitUntil(() => {
                lock (cemPaired) { if (cemPaired.Count == 0) return false; }
                lock (rmPaired)  { if (rmPaired. Count == 0) return false; }
                return true;
            });

            var cemPairing  = await fixture.CEM.Store.GetPairingAsync(fixture.CEM.NodeId, fixture.RM. NodeId);
            var rmPairing   = await fixture.RM. Store.GetPairingAsync(fixture.RM. NodeId, fixture.CEM.NodeId);

            Assert.Multiple(() => {

                Assert.That(cemPairing, Is.Not.Null, "the CEM stored the pairing");
                Assert.That(rmPairing,  Is.Not.Null, "the RM stored the pairing");

                // The CEM is the communication server, the RM the communication client.
                Assert.That(cemPairing!.IsCommunicationServer,   Is.True);
                Assert.That(rmPairing!. IsCommunicationClient,   Is.True);

                // The RM (communication client) knows where to initiate the session.
                Assert.That(rmPairing!. InitiateSessionUrl.HasValue, Is.True);

            });

        }

        #endregion

        #region SessionFormsAutomatically_AfterPairing()

        [Test]
        public async Task SessionFormsAutomatically_AfterPairing()
        {

            await using var fixture = await S2NodeFixture.CreateAsync();

            var cemStarted = new List<S2NodeSession>();
            var rmStarted  = new List<S2NodeSession>();

            fixture.CEM.OnSessionStarted += (_, _, session) => { lock (cemStarted) cemStarted.Add(session); return Task.CompletedTask; };
            fixture.RM. OnSessionStarted += (_, _, session) => { lock (rmStarted)  rmStarted. Add(session); return Task.CompletedTask; };

            await fixture.PairRMWithCEMAsync();
            await fixture.WaitForSessionsAsync();

            await S2NodeFixture.WaitUntil(() => {
                lock (cemStarted) { if (cemStarted.Count == 0) return false; }
                lock (rmStarted)  { if (rmStarted. Count == 0) return false; }
                return true;
            });

            Assert.Multiple(() => {
                Assert.That(fixture.CEM.Sessions,                        Has.Count.EqualTo(1));
                Assert.That(fixture.RM. Sessions,                        Has.Count.EqualTo(1));
                Assert.That(fixture.CEM.Sessions[0].IsCommunicationServer, Is.True);
                Assert.That(fixture.RM. Sessions[0].IsCommunicationServer, Is.False);
            });

        }

        #endregion

        #region CEMSelectsFRBC_WhenControlTypesRegistered()

        [Test]
        public async Task CEMSelectsFRBC_WhenControlTypesRegistered()
        {

            var frbcRM   = new FRBCResourceManager(EVChargerSystemDescription());
            var frbcCEM  = new FRBCEnergyManager();

            await using var fixture = await S2NodeFixture.CreateAsync(
                                                Details:       EVChargerDetails(Resource_Id.Parse("acme_ev_e2e_01")),
                                                ConfigureCEM:  cem => cem.RegisterControlType(frbcCEM),
                                                ConfigureRM:   rm  => rm. RegisterControlType(frbcRM)
                                            );

            await fixture.PairRMWithCEMAsync();
            await fixture.WaitForSessionsAsync();

            var cemSession = fixture.CEM.Sessions[0];

            // The RM published its details and the CEM selected FRBC, activating the control type.
            await S2NodeFixture.WaitUntil(() => cemSession.ActiveControlType == ControlType.FillRateBasedControl, ReconnectTimeout);

            Assert.Multiple(() => {
                Assert.That(cemSession.ActiveControlType,        Is.EqualTo(ControlType.FillRateBasedControl));
                Assert.That(fixture.RM.ResourceManagerDetails,   Is.Not.Null);
                Assert.That(fixture.RM.CurrentSession,           Is.Not.Null);
            });

        }

        #endregion

        #region UnpairFromRM_ClosesSessionAndClearsPairing()

        [Test]
        public async Task UnpairFromRM_ClosesSessionAndClearsPairing()
        {

            await using var fixture = await S2NodeFixture.CreateAsync();

            var rmUnpaired = new List<Pairing>();
            fixture.RM.OnUnpaired += (_, _, pairing, _) => { lock (rmUnpaired) rmUnpaired.Add(pairing); return Task.CompletedTask; };

            await fixture.PairRMWithCEMAsync();
            await fixture.WaitForSessionsAsync();

            // Unpair from the RM (communication client).
            var unpaired = await fixture.RM.UnpairAsync(fixture.CEM.NodeId);
            Assert.That(unpaired, Is.True);

            await S2NodeFixture.WaitUntil(() => fixture.RM.Sessions.Count == 0);
            await S2NodeFixture.WaitUntil(() => { lock (rmUnpaired) return rmUnpaired.Count == 1; });

            var rmPairing = await fixture.RM.Store.GetPairingAsync(fixture.RM.NodeId, fixture.CEM.NodeId);

            Assert.Multiple(() => {
                Assert.That(fixture.RM.Sessions, Is.Empty);
                Assert.That(rmPairing,           Is.Null);
            });

        }

        #endregion

        #region UnpairFromCEM_ServerInitiated_ClosesSessionOnBothSides()

        [Test]
        public async Task UnpairFromCEM_ServerInitiated_ClosesSessionOnBothSides()
        {

            await using var fixture = await S2NodeFixture.CreateAsync();

            await fixture.PairRMWithCEMAsync();
            await fixture.WaitForSessionsAsync();

            // Unpair from the CEM (communication server): it unpairs locally and tells the peer to reconnect.
            var unpaired = await fixture.CEM.UnpairAsync(fixture.RM.NodeId);
            Assert.That(unpaired, Is.True);

            var cemPairing = await fixture.CEM.Store.GetPairingAsync(fixture.CEM.NodeId, fixture.RM.NodeId);
            Assert.That(cemPairing, Is.Null, "the CEM removed the pairing");

            await S2NodeFixture.WaitUntil(() => fixture.CEM.Sessions.Count == 0, TimeSpan.FromSeconds(5));

            // The RM's reconnecting client re-initiates, is told NoLongerPaired and eventually stops.
            await S2NodeFixture.WaitUntil(() => fixture.RM.Sessions.Count == 0, ReconnectTimeout);

            Assert.Multiple(() => {
                Assert.That(fixture.CEM.Sessions, Is.Empty);
                Assert.That(fixture.RM. Sessions, Is.Empty);
            });

        }

        #endregion

        #region StopRMNode_WhileSessionOpen_CEMSeesSessionEnd()

        [Test]
        public async Task StopRMNode_WhileSessionOpen_CEMSeesSessionEnd()
        {

            await using var fixture = await S2NodeFixture.CreateAsync();

            var cemEnded = new List<S2NodeSession>();
            fixture.CEM.OnSessionEnded += (_, _, session, _) => { lock (cemEnded) cemEnded.Add(session); return Task.CompletedTask; };

            await fixture.PairRMWithCEMAsync();
            await fixture.WaitForSessionsAsync();

            // Stopping the RM closes the session with a close frame; the CEM sees the session end.
            await fixture.RM.StopAsync();

            await S2NodeFixture.WaitUntil(() => { lock (cemEnded) return cemEnded.Count >= 1; }, ReconnectTimeout);

            Assert.Multiple(() => {
                Assert.That(cemEnded,           Is.Not.Empty);
                Assert.That(fixture.RM.State,   Is.EqualTo(S2NodeState.Stopped));
            });

        }

        #endregion

        #region StopCEMNode_WhileSessionOpen_RMSessionEnds()

        [Test]
        public async Task StopCEMNode_WhileSessionOpen_RMSessionEnds()
        {

            await using var fixture = await S2NodeFixture.CreateAsync();

            var rmEnded = new List<S2NodeSession>();
            fixture.RM.OnSessionEnded += (_, _, session, _) => { lock (rmEnded) rmEnded.Add(session); return Task.CompletedTask; };

            await fixture.PairRMWithCEMAsync();
            await fixture.WaitForSessionsAsync();

            // Stopping the CEM closes the session; the RM's session ends (the CEM is gone, so it cannot reconnect).
            await fixture.CEM.StopAsync();

            await S2NodeFixture.WaitUntil(() => fixture.RM.Sessions.Count == 0, ReconnectTimeout);
            await S2NodeFixture.WaitUntil(() => { lock (rmEnded) return rmEnded.Count >= 1; }, ReconnectTimeout);

            Assert.Multiple(() => {
                Assert.That(fixture.RM.Sessions, Is.Empty);
                Assert.That(rmEnded,             Is.Not.Empty);
            });

        }

        #endregion

        #region RePairing_SameNodes_YieldsOneSessionAndOnePairing()

        [Test]
        public async Task RePairing_SameNodes_YieldsOneSessionAndOnePairing()
        {

            await using var fixture = await S2NodeFixture.CreateAsync();

            await fixture.PairRMWithCEMAsync();
            await fixture.WaitForSessionsAsync();

            // Pair the same RM-CEM pair again (a fresh token): the pairing is replaced, the session kept.
            await fixture.PairRMWithCEMAsync();
            await fixture.WaitForSessionsAsync();

            await S2NodeFixture.WaitUntil(() => fixture.RM.Sessions.Count == 1 && fixture.CEM.Sessions.Count == 1, ReconnectTimeout);

            var rmPairings   = await fixture.RM. Store.GetPairingsAsync(fixture.RM. NodeId);
            var cemPairings  = await fixture.CEM.Store.GetPairingsAsync(fixture.CEM.NodeId);

            Assert.Multiple(() => {
                Assert.That(fixture.RM. Sessions, Has.Count.EqualTo(1));
                Assert.That(fixture.CEM.Sessions, Has.Count.EqualTo(1));
                Assert.That(rmPairings,           Has.Count.EqualTo(1));
                Assert.That(cemPairings,          Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region Reconnect_AfterSessionClosed_NewSessionForms()

        [Test]
        public async Task Reconnect_AfterSessionClosed_NewSessionForms()
        {

            await using var fixture = await S2NodeFixture.CreateAsync();

            await fixture.PairRMWithCEMAsync();
            await fixture.WaitForSessionsAsync();

            var firstSessionId = fixture.RM.Sessions[0].Session.Id;

            // Close the underlying S2 session directly (not the node session), leaving the
            // reconnecting client alive; it re-initiates a new session on its own.
            await fixture.RM.Sessions[0].Session.CloseAsync(new S2CloseReason("test", true));

            await S2NodeFixture.WaitUntil(() => {
                var sessions = fixture.RM.Sessions;
                return sessions.Count >= 1 && sessions[0].Session.Id != firstSessionId;
            }, ReconnectTimeout);

            var current = fixture.RM.Sessions;

            Assert.Multiple(() => {
                Assert.That(current,                  Is.Not.Empty);
                Assert.That(current[0].Session.Id,    Is.Not.EqualTo(firstSessionId));
            });

        }

        #endregion

        #region EventOrdering_PairedBeforeSessionStarted_EndedAroundUnpaired()

        [Test]
        public async Task EventOrdering_PairedBeforeSessionStarted_EndedAroundUnpaired()
        {

            await using var fixture = await S2NodeFixture.CreateAsync();

            var events = new List<String>();
            void Record(String Name) { lock (events) events.Add(Name); }

            fixture.RM.OnPaired         += (_, _, _, _) => { Record("paired");          return Task.CompletedTask; };
            fixture.RM.OnSessionStarted += (_, _, _)    => { Record("session-started"); return Task.CompletedTask; };
            fixture.RM.OnSessionEnded   += (_, _, _, _) => { Record("session-ended");   return Task.CompletedTask; };
            fixture.RM.OnUnpaired       += (_, _, _, _) => { Record("unpaired");        return Task.CompletedTask; };

            await fixture.PairRMWithCEMAsync();
            await fixture.WaitForSessionsAsync();

            await S2NodeFixture.WaitUntil(() => { lock (events) return events.Contains("paired") && events.Contains("session-started"); });

            await fixture.RM.UnpairAsync(fixture.CEM.NodeId);

            await S2NodeFixture.WaitUntil(() => { lock (events) return events.Contains("session-ended") && events.Contains("unpaired"); }, ReconnectTimeout);

            List<String> snapshot;
            lock (events)
                snapshot = [.. events];

            var paired         = snapshot.IndexOf("paired");
            var sessionStarted = snapshot.IndexOf("session-started");
            var sessionEnded   = snapshot.IndexOf("session-ended");
            var unpaired       = snapshot.IndexOf("unpaired");

            Assert.Multiple(() => {

                // The pairing completes before the session it enables.
                Assert.That(paired,         Is.GreaterThanOrEqualTo(0));
                Assert.That(sessionStarted, Is.GreaterThan(paired));

                // The session ends and the unpairing is announced, both after the session started.
                Assert.That(sessionEnded,   Is.GreaterThan(sessionStarted));
                Assert.That(unpaired,       Is.GreaterThan(sessionStarted));

            });

        }

        #endregion

    }

}

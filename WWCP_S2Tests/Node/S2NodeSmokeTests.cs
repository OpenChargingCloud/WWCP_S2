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
    /// End-to-end smoke tests of the node layer (PLAN.md Phase 10a): a CEM node and an RM node
    /// discover each other, pair, open a session, run a full FRBC exchange and unpair.
    /// </summary>
    [TestFixture]
    public sealed class S2NodeSmokeTests
    {

        #region Helpers

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

        #endregion


        #region DiscoverPairSessionUnpair_FRBC()

        [Test]
        public async Task DiscoverPairSessionUnpair_FRBC()
        {

            var resourceId  = Resource_Id.Parse("acme_ev_000001");

            var frbcRM      = new FRBCResourceManager(EVChargerSystemDescription());
            var frbcCEM     = new FRBCEnergyManager();

            var instructions  = new List<FRBC_Instruction>();
            frbcRM.OnInstruction += (session, instruction, ct) => {
                                        lock (instructions)
                                            instructions.Add(instruction);
                                        return Task.FromResult<ReceptionStatusValue?>(null);
                                    };

            var systemDescriptions = new List<FRBC_SystemDescription>();
            frbcCEM.OnSystemDescription += (session, description, ct) => {
                                               lock (systemDescriptions)
                                                   systemDescriptions.Add(description);
                                               return Task.CompletedTask;
                                           };

            await using var fixture = await S2NodeFixture.CreateAsync(
                                                Details:      EVChargerDetails(resourceId),
                                                ConfigureCEM: cem => cem.RegisterControlType(frbcCEM),
                                                ConfigureRM:  rm  => rm. RegisterControlType(frbcRM)
                                            );

            // The CEM is put into pairing mode, which makes it advertise itself.
            var token = fixture.MakeCEMPairable();

            // The RM discovers the CEM via DNS-SD (role CEM).
            await using var browser = await fixture.Discovery.BrowseAsync(EnergyManagementRole.CEM);
            var discovered = await browser.WaitForEndpointAsync(Timeout: TimeSpan.FromSeconds(5));

            Assert.That(discovered,               Is.Not.Null, "the CEM was not discovered");
            Assert.That(discovered!.PairingUrl,   Is.EqualTo(fixture.CEM.Options.PairingUrl));

            // Pair, then a session forms automatically (RM is the communication client).
            await fixture.PairRMWithCEMAsync(token);
            await fixture.WaitForSessionsAsync();

            Assert.Multiple(() => {
                Assert.That(fixture.CEM.Sessions,  Has.Count.EqualTo(1));
                Assert.That(fixture.RM. Sessions,  Has.Count.EqualTo(1));
            });

            // The RM published its details, the CEM selected FRBC, the RM sent its system description.
            await S2NodeFixture.WaitUntil(() => { lock (systemDescriptions) return systemDescriptions.Count == 1; });

            var cemSession = fixture.CEM.Sessions[0];
            await S2NodeFixture.WaitUntil(() => cemSession.ActiveControlType == ControlType.FillRateBasedControl);

            // The CEM sends an FRBC instruction; the RM receives and acknowledges it.
            var instruction = new FRBC_Instruction(
                                  Instruction_Id.Parse("instr1"),
                                  Actuator_Id.Parse("actuator1"),
                                  OperationMode_Id.Parse("om2"),
                                  1.0,
                                  DateTimeOffset.UtcNow,
                                  AbnormalCondition: false
                              );

            var acks = new List<InstructionStatusUpdate>();
            frbcCEM.OnInstructionStatusUpdate += (session, update, ct) => {
                                                     lock (acks)
                                                         acks.Add(update);
                                                     return Task.CompletedTask;
                                                 };

            var outcome = await cemSession.SendAndAwaitReceptionStatusAsync(instruction);
            Assert.That(outcome.IsOK, Is.True, outcome.ToString());

            await S2NodeFixture.WaitUntil(() => { lock (instructions) return instructions.Count == 1; });
            await S2NodeFixture.WaitUntil(() => { lock (acks) return acks.Count == 1; });

            Assert.Multiple(() => {
                lock (instructions)
                    Assert.That(instructions[0].Id,          Is.EqualTo(Instruction_Id.Parse("instr1")));
                lock (acks)
                    Assert.That(acks[0].StatusType,          Is.EqualTo(InstructionStatus.New));
                Assert.That(systemDescriptions,              Has.Count.EqualTo(1));
                Assert.That(frbcCEM.LastSystemDescription,   Is.Not.Null);
            });

            // Unpair from the RM (communication client): the local material is gone and the session closes.
            var unpaired = await fixture.RM.UnpairAsync(fixture.CEM.NodeId);
            Assert.That(unpaired, Is.True);

            await S2NodeFixture.WaitUntil(() => fixture.RM.Sessions.Count == 0);

            Assert.That(await fixture.RM.Store.GetPairingAsync(fixture.RM.NodeId, fixture.CEM.NodeId), Is.Null);

        }

        #endregion

        #region NodesStartAndStopCleanly()

        [Test]
        public async Task NodesStartAndStopCleanly()
        {

            await using var fixture = await S2NodeFixture.CreateAsync(Details: EVChargerDetails(Resource_Id.Parse("acme_ev_000002")));

            Assert.Multiple(() => {
                Assert.That(fixture.CEM.IsRunning,               Is.True);
                Assert.That(fixture.RM. IsRunning,               Is.True);
                Assert.That(fixture.CEM.IsCommunicationServer,   Is.True);
                Assert.That(fixture.CEM.IsCommunicationClient,   Is.False);
                Assert.That(fixture.RM. IsCommunicationServer,   Is.False);
                Assert.That(fixture.RM. IsCommunicationClient,   Is.True);
                Assert.That(fixture.CEM.WebSocketServer,         Is.Not.Null);
                Assert.That(fixture.RM. WebSocketServer,         Is.Null);
            });

            await fixture.RM. StopAsync();
            await fixture.CEM.StopAsync();

            Assert.Multiple(() => {
                Assert.That(fixture.CEM.State,  Is.EqualTo(S2NodeState.Stopped));
                Assert.That(fixture.RM. State,  Is.EqualTo(S2NodeState.Stopped));
            });

        }

        #endregion

    }

}

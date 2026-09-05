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

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Node
{

    /// <summary>
    /// The lifecycle of an S2 node (PLAN.md Phase 10a): starting is idempotent and observable via
    /// <see cref="AS2Node.OnStateChanged"/>, a started node exposes its pairing server (and, for a
    /// communication server, its WebSocket and session initiation servers and ports), stopping and
    /// disposing bring it to <see cref="S2NodeState.Stopped"/>, stopping before starting is
    /// harmless, pairing before starting is refused, and a LAN endpoint advertises itself only
    /// while one of its hosted nodes has a valid pairing token.
    /// </summary>
    [TestFixture]
    public sealed class S2NodeLifecycleTests
    {

        #region StartAsync_Twice_IsIdempotent()

        [Test]
        public async Task StartAsync_Twice_IsIdempotent()
        {

            await using var fixture = await S2NodeFixture.CreateAsync(Start: false);

            await fixture.CEM.StartAsync();
            await fixture.CEM.StartAsync();

            Assert.Multiple(() => {
                Assert.That(fixture.CEM.State,      Is.EqualTo(S2NodeState.Running));
                Assert.That(fixture.CEM.IsRunning,  Is.True);
            });

        }

        #endregion

        #region StateTransitions_CreatedToRunning_ObservedViaOnStateChanged()

        [Test]
        public async Task StateTransitions_CreatedToRunning_ObservedViaOnStateChanged()
        {

            await using var fixture = await S2NodeFixture.CreateAsync(Start: false);

            var transitions = new List<(S2NodeState Old, S2NodeState New)>();

            fixture.CEM.OnStateChanged += (_, _, oldState, newState) => {
                                              lock (transitions)
                                                  transitions.Add((oldState, newState));
                                              return Task.CompletedTask;
                                          };

            await fixture.CEM.StartAsync();

            List<(S2NodeState Old, S2NodeState New)> snapshot;
            lock (transitions)
                snapshot = [.. transitions];

            Assert.Multiple(() => {
                Assert.That(fixture.CEM.State, Is.EqualTo(S2NodeState.Running));
                Assert.That(snapshot,          Does.Contain((S2NodeState.Created,  S2NodeState.Starting)));
                Assert.That(snapshot,          Does.Contain((S2NodeState.Starting, S2NodeState.Running)));
            });

        }

        #endregion

        #region AfterStart_ServersAndPortsAreSet()

        [Test]
        public async Task AfterStart_ServersAndPortsAreSet()
        {

            await using var fixture = await S2NodeFixture.CreateAsync();

            Assert.Multiple(() => {

                // The CEM is the communication server: pairing + WebSocket + session initiation.
                Assert.That(fixture.CEM.PairingServer,            Is.Not.Null);
                Assert.That(fixture.CEM.WebSocketServer,          Is.Not.Null);
                Assert.That(fixture.CEM.SessionInitiationServer,  Is.Not.Null);
                Assert.That(fixture.CEM.HTTPPort,                 Is.Not.Null);
                Assert.That(fixture.CEM.WebSocketPort,            Is.Not.Null);

                // The RM is the communication client: pairing server only, no WebSocket server.
                Assert.That(fixture.RM.PairingServer,             Is.Not.Null);
                Assert.That(fixture.RM.WebSocketServer,           Is.Null);
                Assert.That(fixture.RM.SessionInitiationServer,   Is.Null);
                Assert.That(fixture.RM.HTTPPort,                  Is.Not.Null);
                Assert.That(fixture.RM.WebSocketPort,             Is.Null);

            });

        }

        #endregion

        #region StopAsync_SetsStateStopped()

        [Test]
        public async Task StopAsync_SetsStateStopped()
        {

            await using var fixture = await S2NodeFixture.CreateAsync();

            await fixture.CEM.StopAsync();

            Assert.Multiple(() => {
                Assert.That(fixture.CEM.State,     Is.EqualTo(S2NodeState.Stopped));
                Assert.That(fixture.CEM.IsRunning, Is.False);
                Assert.That(fixture.CEM.Sessions,  Is.Empty);
            });

        }

        #endregion

        #region StopAsync_BeforeStart_IsHarmless()

        [Test]
        public async Task StopAsync_BeforeStart_IsHarmless()
        {

            await using var fixture = await S2NodeFixture.CreateAsync(Start: false);

            Assert.DoesNotThrowAsync(async () => await fixture.CEM.StopAsync());

            Assert.That(fixture.CEM.State, Is.EqualTo(S2NodeState.Created));

        }

        #endregion

        #region DisposeAsync_StopsRunningNode()

        [Test]
        public async Task DisposeAsync_StopsRunningNode()
        {

            await using var fixture = await S2NodeFixture.CreateAsync(Start: false);

            await fixture.CEM.StartAsync();
            Assert.That(fixture.CEM.IsRunning, Is.True);

            await fixture.CEM.DisposeAsync();

            Assert.That(fixture.CEM.State, Is.EqualTo(S2NodeState.Stopped));

        }

        #endregion

        #region PairAsync_BeforeStart_ThrowsInvalidOperationException()

        [Test]
        public async Task PairAsync_BeforeStart_ThrowsInvalidOperationException()
        {

            await using var fixture = await S2NodeFixture.CreateAsync(Start: false);

            Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.RM.PairAsync(
                          fixture.CEM.Options.PairingUrl,
                          PairingToken.Parse("ABCDEF"),
                          Deployment.LAN
                      )
            );

        }

        #endregion

        #region UnpairAsync_BeforeStart_ReturnsFalse()

        [Test]
        public async Task UnpairAsync_BeforeStart_ReturnsFalse()
        {

            await using var fixture = await S2NodeFixture.CreateAsync(Start: false);

            // The pair was never paired, so there is nothing to unpair.
            var unpaired = await fixture.RM.UnpairAsync(fixture.CEM.NodeId);

            Assert.That(unpaired, Is.False);

        }

        #endregion

        #region CreatedWithStartFalse_HasStateCreated_AndEmptySessions()

        [Test]
        public async Task CreatedWithStartFalse_HasStateCreated_AndEmptySessions()
        {

            await using var fixture = await S2NodeFixture.CreateAsync(Start: false);

            Assert.Multiple(() => {
                Assert.That(fixture.CEM.State,     Is.EqualTo(S2NodeState.Created));
                Assert.That(fixture.RM. State,     Is.EqualTo(S2NodeState.Created));
                Assert.That(fixture.CEM.IsRunning, Is.False);
                Assert.That(fixture.RM. IsRunning, Is.False);
                Assert.That(fixture.CEM.Sessions,  Is.Empty);
                Assert.That(fixture.RM. Sessions,  Is.Empty);
            });

        }

        #endregion

        #region RMAdvertises_OnlyWhilePairingTokenExists()

        [Test]
        public async Task RMAdvertises_OnlyWhilePairingTokenExists()
        {

            await using var fixture = await S2NodeFixture.CreateAsync();

            Assert.That(fixture.RM.Advertiser, Is.Not.Null, "a started LAN node has an advertiser");

            // Without a pairing token the endpoint is not advertised (nobody could complete a pairing).
            Assert.That(fixture.RM.Advertiser!.IsAdvertised, Is.False);

            // Issuing a token makes the RM advertise itself.
            _ = fixture.RM.Node.IssueDynamicPairingToken();
            await S2NodeFixture.WaitUntil(() => fixture.RM.Advertiser!.IsAdvertised, TimeSpan.FromSeconds(5));
            Assert.That(fixture.RM.Advertiser!.IsAdvertised, Is.True);

            // Clearing the token withdraws the advertisement again.
            fixture.RM.Node.ClearPairingToken();
            await S2NodeFixture.WaitUntil(() => !fixture.RM.Advertiser!.IsAdvertised, TimeSpan.FromSeconds(5));
            Assert.That(fixture.RM.Advertiser!.IsAdvertised, Is.False);

        }

        #endregion

    }

}

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

using Microsoft.Extensions.Time.Testing;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The long-polling server driven in-process with a fake clock: the action queues (at most
    /// one action per client node and response), the 25 second response timeout, the automatic
    /// request of node descriptions, the events, purging, back-pressure, shutdown and
    /// cancellation (S2 Connect 1.0.0, "Long-polling for constrained endpoints in the LAN";
    /// PLAN.md Phase 6b).
    /// </summary>
    [TestFixture]
    public sealed class LongPollingServerTests
    {

        #region Helpers

        /// <summary>
        /// The upper bound of every wait for a request that is expected to complete.
        /// </summary>
        private static readonly TimeSpan             Wait             = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The normative response timeout of the server (S2 Connect: "respond within 25 seconds").
        /// </summary>
        private static readonly TimeSpan             ResponseTimeout  = TimeSpan.FromSeconds(25);

        /// <summary>
        /// The start of the fake clock.
        /// </summary>
        private static readonly DateTimeOffset       Start            = new (2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// The endpoint description a constrained client sends after 'sendNodeDescription'.
        /// </summary>
        private static readonly EndpointDescription  LANEndpoint      = new ("Constrained endpoint", null, Deployment.LAN);


        private static FakeTimeProvider NewClock()
            => new (Start);

        private static LongPollingServer NewServer(FakeTimeProvider  Clock,
                                                   Boolean           AutoRequestNodeDescriptions   = true,
                                                   Int32             MaxHangingRequests            = 64)

            => new (Clock,
                    ResponseTimeout,
                    MaxHangingRequests,
                    AutoRequestNodeDescriptions);

        private static NodeDescription NewNodeDescription(EnergyManagementRole Role)

            => new (Node_Id.NewRandom,
                    "ACME",
                    Role == EnergyManagementRole.CEM ? "EMS"        : "heat pump",
                    Role == EnergyManagementRole.CEM ? "PollingCEM" : "PollingRM",
                    Role);

        /// <summary>
        /// A waitForPairing request listing the given client nodes without descriptions.
        /// </summary>
        private static WaitForPairingRequest Poll(params Node_Id[] ClientNodeIds)
            => new ([.. ClientNodeIds.Select(id => new WaitForPairingRequestItem(id))]);

        /// <summary>
        /// A waitForPairing request with a single, fully specified item.
        /// </summary>
        private static WaitForPairingRequest PollWith(WaitForPairingRequestItem Item)
            => new ([ Item ]);

        /// <summary>
        /// Give a freshly started WaitAsync the chance to register its delay timer at the fake
        /// clock (and to raise its events) before the clock is advanced or the state is inspected.
        /// </summary>
        private static Task YieldAsync()
            => Task.Delay(50);

        /// <summary>
        /// Register the given client nodes at a server without automatic actions: their first
        /// poll hangs until the response timeout elapses.
        /// </summary>
        private static async Task RegisterAsync(LongPollingServer  Server,
                                                FakeTimeProvider   Clock,
                                                params Node_Id[]   ClientNodeIds)
        {

            var poll = Server.WaitAsync(Poll(ClientNodeIds));
            await YieldAsync();

            Clock.Advance(ResponseTimeout);

            var result = await poll.WaitAsync(Wait);
            Assert.That(result.Status, Is.EqualTo(LongPollingStatus.NoAction));

        }

        #endregion


        #region Constructor_UsesTheNormativeDefaults_AndRejectsInvalidArguments()

        [Test]
        [S2C("LongPolling.ServerTimeout")]
        public void Constructor_UsesTheNormativeDefaults_AndRejectsInvalidArguments()
        {

            using var server = new LongPollingServer();

            Assert.Multiple(() => {
                Assert.That(server.ResponseTimeout,              Is.EqualTo(S2ConnectDefaults.LongPollingServerTimeout));
                Assert.That(server.ResponseTimeout,              Is.EqualTo(TimeSpan.FromSeconds(25)));
                Assert.That(server.MaxHangingRequests,           Is.EqualTo(64));
                Assert.That(server.AutoRequestNodeDescriptions,  Is.True);
                Assert.That(server.IsAcceptingRequests,          Is.True);
                Assert.That(server.HangingRequests,              Is.Zero);
                Assert.That(server.ClientNodes,                  Is.Empty);
            });

            Assert.That(() => { using var s = new LongPollingServer(ResponseTimeout: TimeSpan.FromSeconds(26)); }, Throws.TypeOf<ArgumentOutOfRangeException>(), "longer than the normative 25 seconds");
            Assert.That(() => { using var s = new LongPollingServer(ResponseTimeout: TimeSpan.Zero);            }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { using var s = new LongPollingServer(MaxHangingRequests: 0);                     }, Throws.TypeOf<ArgumentOutOfRangeException>());

        }

        #endregion


        #region SendAction_IsFalseForUnknownNodes_AndQueuedActionsAreDeliveredImmediately()

        [Test]
        [S2C("LongPolling.Actions")]
        public async Task SendAction_IsFalseForUnknownNodes_AndQueuedActionsAreDeliveredImmediately()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock, AutoRequestNodeDescriptions: false);
            var nodeId         = Node_Id.NewRandom;

            Assert.That(server.SendAction(nodeId, WaitForPairingAction.RequestPairing), Is.False, "a node that never polled is unknown");
            Assert.That(server.ClientNodes, Is.Empty);

            await RegisterAsync(server, clock, nodeId);

            Assert.That(server.SendAction(nodeId, WaitForPairingAction.RequestPairing), Is.True);
            Assert.That(server.TryGetClientNode(nodeId, out var clientNode), Is.True);
            Assert.That(clientNode!.PendingActions, Is.EqualTo(new[] { WaitForPairingAction.RequestPairing }));

            // Delivered without any advance of the clock...
            var result = await server.WaitAsync(Poll(nodeId)).WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(result.Status,                          Is.EqualTo(LongPollingStatus.Actions));
                Assert.That(result.Response,                        Is.Not.Null);
                Assert.That(result.Response!.Items.Count,           Is.EqualTo(1));
                Assert.That(result.Response.Items[0].ClientNodeId,  Is.EqualTo(nodeId));
                Assert.That(result.Response.Items[0].Action,        Is.EqualTo(WaitForPairingAction.RequestPairing));
                Assert.That(clientNode!.PendingActions,             Is.Empty);
                Assert.That(server.HangingRequests,                 Is.Zero);
            });

        }

        #endregion

        #region QueuedActions_AreDeliveredOnePerResponse_InOrder()

        [Test]
        [S2C("LongPolling.OneActionPerNode")]
        public async Task QueuedActions_AreDeliveredOnePerResponse_InOrder()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock, AutoRequestNodeDescriptions: false);
            var nodeId         = Node_Id.NewRandom;

            await RegisterAsync(server, clock, nodeId);

            server.SendAction(nodeId, WaitForPairingAction.PreparePairing);
            server.SendAction(nodeId, WaitForPairingAction.RequestPairing);

            Assert.That(server.TryGetClientNode(nodeId, out var clientNode), Is.True);
            Assert.That(clientNode!.PendingActions, Is.EqualTo(new[] { WaitForPairingAction.PreparePairing, WaitForPairingAction.RequestPairing }));

            var first   = await server.WaitAsync(Poll(nodeId)).WaitAsync(Wait);
            var second  = await server.WaitAsync(Poll(nodeId)).WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(first.Status,                      Is.EqualTo(LongPollingStatus.Actions));
                Assert.That(first.Response!.Items.Count,       Is.EqualTo(1));
                Assert.That(first.Response.Items[0].Action,    Is.EqualTo(WaitForPairingAction.PreparePairing));
                Assert.That(second.Status,                     Is.EqualTo(LongPollingStatus.Actions));
                Assert.That(second.Response!.Items.Count,      Is.EqualTo(1));
                Assert.That(second.Response.Items[0].Action,   Is.EqualTo(WaitForPairingAction.RequestPairing));
                Assert.That(clientNode!.PendingActions,        Is.Empty);
            });

            // The queue is empty now: the next request hangs until the timeout.
            var third = server.WaitAsync(Poll(nodeId));
            await YieldAsync();
            Assert.That(third.IsCompleted, Is.False);

            clock.Advance(ResponseTimeout);
            Assert.That((await third.WaitAsync(Wait)).Status, Is.EqualTo(LongPollingStatus.NoAction));

        }

        #endregion

        #region IdenticalConsecutiveActions_AreDeduplicated()

        [Test]
        [S2C("LongPolling.OneActionPerNode")]
        public async Task IdenticalConsecutiveActions_AreDeduplicated()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock, AutoRequestNodeDescriptions: false);
            var nodeId         = Node_Id.NewRandom;

            await RegisterAsync(server, clock, nodeId);

            server.SendAction(nodeId, WaitForPairingAction.PreparePairing);
            server.SendAction(nodeId, WaitForPairingAction.PreparePairing);         // the same as the last queued action: dropped
            server.SendAction(nodeId, WaitForPairingAction.CancelPreparePairing);
            server.SendAction(nodeId, WaitForPairingAction.PreparePairing);         // not consecutive: kept

            Assert.That(server.TryGetClientNode(nodeId, out var clientNode), Is.True);
            Assert.That(clientNode!.PendingActions, Is.EqualTo(new[] { WaitForPairingAction.PreparePairing,
                                                                        WaitForPairingAction.CancelPreparePairing,
                                                                        WaitForPairingAction.PreparePairing }));

            var first   = await server.WaitAsync(Poll(nodeId)).WaitAsync(Wait);
            var second  = await server.WaitAsync(Poll(nodeId)).WaitAsync(Wait);
            var third   = await server.WaitAsync(Poll(nodeId)).WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(first. Response!.Items[0].Action,  Is.EqualTo(WaitForPairingAction.PreparePairing));
                Assert.That(second.Response!.Items[0].Action,  Is.EqualTo(WaitForPairingAction.CancelPreparePairing));
                Assert.That(third. Response!.Items[0].Action,  Is.EqualTo(WaitForPairingAction.PreparePairing));
                Assert.That(clientNode!.PendingActions,        Is.Empty);
            });

            var fourth = server.WaitAsync(Poll(nodeId));
            await YieldAsync();
            Assert.That(fourth.IsCompleted, Is.False, "the duplicate was not queued");

            clock.Advance(ResponseTimeout);
            Assert.That((await fourth.WaitAsync(Wait)).Status, Is.EqualTo(LongPollingStatus.NoAction));

        }

        #endregion

        #region RequestListingTwoNodes_GetsOneItemPerNodeWithAnAction()

        [Test]
        [S2C("LongPolling.OneActionPerNode")]
        public async Task RequestListingTwoNodes_GetsOneItemPerNodeWithAnAction()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock, AutoRequestNodeDescriptions: false);
            var nodeA          = Node_Id.NewRandom;
            var nodeB          = Node_Id.NewRandom;

            // One request registers both nodes.
            await RegisterAsync(server, clock, nodeA, nodeB);

            Assert.That(server.ClientNodes.Select(node => node.Id), Is.EquivalentTo(new[] { nodeA, nodeB }));

            server.SendAction(nodeA, WaitForPairingAction.RequestPairing);
            server.SendAction(nodeB, WaitForPairingAction.PreparePairing);

            var both     = await server.WaitAsync(Poll(nodeA, nodeB)).WaitAsync(Wait);
            var actions  = both.Response?.Items.ToDictionary(item => item.ClientNodeId, item => item.Action)
                               ?? new Dictionary<Node_Id, WaitForPairingAction>();

            Assert.Multiple(() => {
                Assert.That(both.Status,     Is.EqualTo(LongPollingStatus.Actions));
                Assert.That(actions,         Has.Count.EqualTo(2));
                Assert.That(actions[nodeA],  Is.EqualTo(WaitForPairingAction.RequestPairing));
                Assert.That(actions[nodeB],  Is.EqualTo(WaitForPairingAction.PreparePairing));
            });

            // Only one of the nodes has an action: only one item.
            server.SendAction(nodeA, WaitForPairingAction.CancelPreparePairing);

            var single = await server.WaitAsync(Poll(nodeA, nodeB)).WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(single.Status,                         Is.EqualTo(LongPollingStatus.Actions));
                Assert.That(single.Response!.Items.Count,          Is.EqualTo(1));
                Assert.That(single.Response.Items[0].ClientNodeId, Is.EqualTo(nodeA));
                Assert.That(single.Response.Items[0].Action,       Is.EqualTo(WaitForPairingAction.CancelPreparePairing));
            });

        }

        #endregion


        #region WithoutActions_TheRequestHangs_UntilTheResponseTimeout()

        [Test]
        [S2C("LongPolling.ServerTimeout")]
        public async Task WithoutActions_TheRequestHangs_UntilTheResponseTimeout()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock, AutoRequestNodeDescriptions: false);
            var nodeId         = Node_Id.NewRandom;

            var poll = server.WaitAsync(Poll(nodeId));
            await YieldAsync();

            Assert.That(server.TryGetClientNode(nodeId, out var clientNode), Is.True);

            Assert.Multiple(() => {
                Assert.That(poll.IsCompleted,            Is.False);
                Assert.That(server.HangingRequests,      Is.EqualTo(1));
                Assert.That(clientNode!.IsPolling,       Is.True);
                Assert.That(clientNode.PendingActions,   Is.Empty);
            });

            // One second before the deadline the request is still open...
            clock.Advance(ResponseTimeout - TimeSpan.FromSeconds(1));
            await YieldAsync();
            Assert.That(poll.IsCompleted, Is.False);

            // ...and it is released with 'no action' when the deadline is reached.
            clock.Advance(TimeSpan.FromSeconds(1));

            var result = await poll.WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(result.Status,               Is.EqualTo(LongPollingStatus.NoAction));
                Assert.That(result.Response,             Is.Null);
                Assert.That(server.HangingRequests,      Is.Zero);
                Assert.That(clientNode!.IsPolling,       Is.False);
                Assert.That(server.IsAcceptingRequests,  Is.True);
            });

        }

        #endregion

        #region SendAction_WhileHanging_CompletesTheRequestWithTheAction()

        [Test]
        [S2C("LongPolling.Actions")]
        public async Task SendAction_WhileHanging_CompletesTheRequestWithTheAction()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock, AutoRequestNodeDescriptions: false);
            var nodeId         = Node_Id.NewRandom;

            // The first poll registers the node and hangs.
            var poll = server.WaitAsync(Poll(nodeId));
            await YieldAsync();
            Assert.That(poll.IsCompleted, Is.False);

            Assert.That(server.SendAction(nodeId, WaitForPairingAction.RequestPairing), Is.True);

            // Completed without any advance of the clock...
            var result = await poll.WaitAsync(Wait);

            Assert.That(server.TryGetClientNode(nodeId, out var clientNode), Is.True);

            Assert.Multiple(() => {
                Assert.That(result.Status,                          Is.EqualTo(LongPollingStatus.Actions));
                Assert.That(result.Response!.Items.Count,           Is.EqualTo(1));
                Assert.That(result.Response.Items[0].ClientNodeId,  Is.EqualTo(nodeId));
                Assert.That(result.Response.Items[0].Action,        Is.EqualTo(WaitForPairingAction.RequestPairing));
                Assert.That(server.HangingRequests,                 Is.Zero);
                Assert.That(clientNode!.IsPolling,                  Is.False);
                Assert.That(clientNode.PendingActions,              Is.Empty);
            });

        }

        #endregion


        #region AutoRequestNodeDescriptions_AsksANewNodeExactlyOnce()

        [Test]
        [S2C("LongPolling.SendNodeDescription")]
        public async Task AutoRequestNodeDescriptions_AsksANewNodeExactlyOnce()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock);
            var nodeId         = Node_Id.NewRandom;

            // The first poll of a node without description is answered at once.
            var first = await server.WaitAsync(Poll(nodeId)).WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(server.AutoRequestNodeDescriptions,     Is.True);
                Assert.That(first.Status,                           Is.EqualTo(LongPollingStatus.Actions));
                Assert.That(first.Response!.Items.Count,            Is.EqualTo(1));
                Assert.That(first.Response.Items[0].ClientNodeId,   Is.EqualTo(nodeId));
                Assert.That(first.Response.Items[0].Action,         Is.EqualTo(WaitForPairingAction.SendNodeDescription));
            });

            // The second poll, still without description, is not asked again: it hangs.
            var second = server.WaitAsync(Poll(nodeId));
            await YieldAsync();

            Assert.That(server.TryGetClientNode(nodeId, out var clientNode), Is.True);

            Assert.Multiple(() => {
                Assert.That(second.IsCompleted,            Is.False);
                Assert.That(clientNode!.Description,       Is.Null);
                Assert.That(clientNode.PendingActions,     Is.Empty);
            });

            clock.Advance(ResponseTimeout);
            Assert.That((await second.WaitAsync(Wait)).Status, Is.EqualTo(LongPollingStatus.NoAction));

        }

        #endregion

        #region NodeDescription_IsStored_AndOnClientNodeDescriptionReceivedIsRaised()

        [Test]
        [S2C("LongPolling.SendNodeDescription")]
        public async Task NodeDescription_IsStored_AndOnClientNodeDescriptionReceivedIsRaised()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock);
            var description    = NewNodeDescription(EnergyManagementRole.RM);
            var received       = new List<LongPollingClientNode>();

            server.OnClientNodeDescriptionReceived += (_, _, node) => {
                                                          lock (received)
                                                              received.Add(node);
                                                          return Task.CompletedTask;
                                                      };

            // Asked for the description...
            var first = await server.WaitAsync(Poll(description.Id)).WaitAsync(Wait);
            Assert.That(first.Status,              Is.EqualTo(LongPollingStatus.Actions));
            Assert.That(first.Response!.Items[0].Action, Is.EqualTo(WaitForPairingAction.SendNodeDescription));
            Assert.That(received,                  Is.Empty);

            // ...the node sends it with its next request, which hangs as nothing else is queued.
            var second = server.WaitAsync(PollWith(new WaitForPairingRequestItem(description.Id, description, LANEndpoint)));
            await YieldAsync();

            Assert.That(server.TryGetClientNode(description.Id, out var clientNode), Is.True);

            Assert.Multiple(() => {
                Assert.That(second.IsCompleted,                 Is.False, "no action is queued after the description arrived");
                Assert.That(clientNode!.Description,            Is.EqualTo(description));
                Assert.That(clientNode.EndpointDescription,     Is.EqualTo(LANEndpoint));
                Assert.That(clientNode.PendingActions,          Is.Empty);
                Assert.That(received,                           Is.EqualTo(new[] { clientNode }));
                Assert.That(clientNode.ToString(),              Does.Contain("ACME").And.Contain("polling"));
            });

            clock.Advance(ResponseTimeout);
            Assert.That((await second.WaitAsync(Wait)).Status, Is.EqualTo(LongPollingStatus.NoAction));

        }

        #endregion

        #region AutoRequestNodeDescriptions_Disabled_QueuesNoAutomaticAction()

        [Test]
        [S2C("LongPolling.SendNodeDescription")]
        public async Task AutoRequestNodeDescriptions_Disabled_QueuesNoAutomaticAction()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock, AutoRequestNodeDescriptions: false);
            var nodeId         = Node_Id.NewRandom;

            var poll = server.WaitAsync(Poll(nodeId));
            await YieldAsync();

            Assert.That(server.TryGetClientNode(nodeId, out var clientNode), Is.True);

            Assert.Multiple(() => {
                Assert.That(server.AutoRequestNodeDescriptions,  Is.False);
                Assert.That(poll.IsCompleted,                    Is.False);
                Assert.That(clientNode!.Description,             Is.Null);
                Assert.That(clientNode.PendingActions,           Is.Empty);
            });

            clock.Advance(ResponseTimeout);

            var result = await poll.WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(result.Status,    Is.EqualTo(LongPollingStatus.NoAction));
                Assert.That(result.Response,  Is.Null);
            });

        }

        #endregion


        #region OnClientNodeSeen_ReportsIsNewForTheFirstPollOnly()

        [Test]
        [S2C("LongPolling.ClientNodes")]
        public async Task OnClientNodeSeen_ReportsIsNewForTheFirstPollOnly()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock);
            var nodeId         = Node_Id.NewRandom;
            var seen           = new List<(Node_Id Id, Boolean IsNew, DateTimeOffset Timestamp)>();

            server.OnClientNodeSeen += (timestamp, _, node, isNew) => {
                                           lock (seen)
                                               seen.Add((node.Id, isNew, timestamp));
                                           return Task.CompletedTask;
                                       };

            await server.WaitAsync(Poll(nodeId)).WaitAsync(Wait);   // answered at once: 'sendNodeDescription'

            clock.Advance(TimeSpan.FromSeconds(10));

            var second = server.WaitAsync(Poll(nodeId));
            await YieldAsync();

            Assert.That(seen, Has.Count.EqualTo(2));

            Assert.Multiple(() => {
                Assert.That(seen[0].Id,         Is.EqualTo(nodeId));
                Assert.That(seen[0].IsNew,      Is.True);
                Assert.That(seen[0].Timestamp,  Is.EqualTo(Start));
                Assert.That(seen[1].Id,         Is.EqualTo(nodeId));
                Assert.That(seen[1].IsNew,      Is.False);
                Assert.That(seen[1].Timestamp,  Is.EqualTo(Start + TimeSpan.FromSeconds(10)));
            });

            clock.Advance(ResponseTimeout);
            Assert.That((await second.WaitAsync(Wait)).Status, Is.EqualTo(LongPollingStatus.NoAction));

        }

        #endregion

        #region OnClientNodeError_ReportsTheError_AndTheNodeRemembersIt()

        [Test]
        [S2C("LongPolling.ErrorMessage")]
        public async Task OnClientNodeError_ReportsTheError_AndTheNodeRemembersIt()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock);
            var nodeId         = Node_Id.NewRandom;
            var errors         = new List<(Node_Id Id, WaitForPairingError Error, DateTimeOffset Timestamp)>();

            server.OnClientNodeError += (timestamp, _, node, error) => {
                                            lock (errors)
                                                errors.Add((node.Id, error, timestamp));
                                            return Task.CompletedTask;
                                        };

            clock.Advance(TimeSpan.FromMinutes(1));

            var result = await server.WaitAsync(PollWith(new WaitForPairingRequestItem(nodeId, ErrorMessage: WaitForPairingError.NoValidTokenOnPairingClient))).WaitAsync(Wait);

            // The new node is still asked for its description, so the request is answered at once.
            Assert.That(result.Status, Is.EqualTo(LongPollingStatus.Actions));
            Assert.That(server.TryGetClientNode(nodeId, out var clientNode), Is.True);
            Assert.That(errors, Has.Count.EqualTo(1));

            Assert.Multiple(() => {
                Assert.That(errors[0].Id,               Is.EqualTo(nodeId));
                Assert.That(errors[0].Error,            Is.EqualTo(WaitForPairingError.NoValidTokenOnPairingClient));
                Assert.That(errors[0].Timestamp,        Is.EqualTo(Start + TimeSpan.FromMinutes(1)));
                Assert.That(clientNode!.LastError,      Is.EqualTo(WaitForPairingError.NoValidTokenOnPairingClient));
                Assert.That(clientNode.LastErrorAt,     Is.EqualTo(Start + TimeSpan.FromMinutes(1)));
            });

        }

        #endregion

        #region FirstSeenAndLastSeen_FollowTheClock()

        [Test]
        [S2C("LongPolling.ClientNodes")]
        public async Task FirstSeenAndLastSeen_FollowTheClock()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock);
            var nodeId         = Node_Id.NewRandom;

            await server.WaitAsync(Poll(nodeId)).WaitAsync(Wait);   // answered at once: 'sendNodeDescription'

            Assert.That(server.TryGetClientNode(nodeId, out var clientNode), Is.True);

            Assert.Multiple(() => {
                Assert.That(clientNode!.Id,           Is.EqualTo(nodeId));
                Assert.That(clientNode.FirstSeen,     Is.EqualTo(Start));
                Assert.That(clientNode.LastSeen,      Is.EqualTo(Start));
                Assert.That(clientNode.LastError,     Is.Null);
                Assert.That(clientNode.LastErrorAt,   Is.Null);
                Assert.That(clientNode.IsPolling,     Is.False);
            });

            clock.Advance(TimeSpan.FromSeconds(10));

            var second = server.WaitAsync(Poll(nodeId));
            await YieldAsync();

            Assert.Multiple(() => {
                Assert.That(clientNode!.FirstSeen,    Is.EqualTo(Start));
                Assert.That(clientNode.LastSeen,      Is.EqualTo(Start + TimeSpan.FromSeconds(10)));
                Assert.That(clientNode.IsPolling,     Is.True);
            });

            clock.Advance(ResponseTimeout);
            Assert.That((await second.WaitAsync(Wait)).Status, Is.EqualTo(LongPollingStatus.NoAction));

        }

        #endregion

        #region Purge_ForgetsNodesThatAreNeitherPollingNorRecentlySeen()

        [Test]
        [S2C("LongPolling.ClientNodes")]
        public async Task Purge_ForgetsNodesThatAreNeitherPollingNorRecentlySeen()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock);
            var idleNodeId     = Node_Id.NewRandom;
            var pollingNode    = NewNodeDescription(EnergyManagementRole.RM);

            // The idle node is answered at once and never polls again...
            await server.WaitAsync(Poll(idleNodeId)).WaitAsync(Wait);

            // ...the polling node sends its description and hangs.
            var hanging = server.WaitAsync(PollWith(new WaitForPairingRequestItem(pollingNode.Id, pollingNode, LANEndpoint)));
            await YieldAsync();

            Assert.That(server.ClientNodes.Count, Is.EqualTo(2));

            clock.Advance(TimeSpan.FromSeconds(20));

            // The idle node was last seen 20 s ago, the polling node is protected by its hanging request.
            Assert.That(server.Purge(TimeSpan.FromSeconds(10)), Is.EqualTo(1));

            Assert.Multiple(() => {
                Assert.That(server.ClientNodes.Select(node => node.Id),  Is.EqualTo(new[] { pollingNode.Id }));
                Assert.That(server.TryGetClientNode(idleNodeId, out _),  Is.False);
                Assert.That(hanging.IsCompleted,                         Is.False);
            });

            // 25 s after its poll the hanging request is released...
            clock.Advance(TimeSpan.FromSeconds(5));
            Assert.That((await hanging.WaitAsync(Wait)).Status, Is.EqualTo(LongPollingStatus.NoAction));

            // ...a generous max age keeps the node, a strict one purges it now.
            Assert.That(server.Purge(TimeSpan.FromSeconds(30)),  Is.Zero);
            Assert.That(server.Purge(TimeSpan.FromSeconds(10)),  Is.EqualTo(1));
            Assert.That(server.ClientNodes,                      Is.Empty);

        }

        #endregion

        #region RemoveClientNode_ForgetsTheNodeAndItsQueuedActions()

        [Test]
        [S2C("LongPolling.ClientNodes")]
        public async Task RemoveClientNode_ForgetsTheNodeAndItsQueuedActions()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock);
            var nodeId         = Node_Id.NewRandom;

            await server.WaitAsync(Poll(nodeId)).WaitAsync(Wait);   // answered at once: 'sendNodeDescription'

            Assert.That(server.SendAction(nodeId, WaitForPairingAction.RequestPairing), Is.True);
            Assert.That(server.TryGetClientNode(nodeId, out var clientNode), Is.True);
            Assert.That(clientNode!.PendingActions.Count, Is.EqualTo(1));

            Assert.That(server.RemoveClientNode(nodeId), Is.True);

            Assert.Multiple(() => {
                Assert.That(server.RemoveClientNode(nodeId),                              Is.False);
                Assert.That(server.TryGetClientNode(nodeId, out _),                       Is.False);
                Assert.That(server.ClientNodes,                                           Is.Empty);
                Assert.That(server.SendAction(nodeId, WaitForPairingAction.RequestPairing), Is.False, "the queued action is gone with the node");
            });

            // A node that polls again after its removal is new again: it is asked for its description, not for pairing.
            var again = await server.WaitAsync(Poll(nodeId)).WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(again.Status,                      Is.EqualTo(LongPollingStatus.Actions));
                Assert.That(again.Response!.Items.Count,       Is.EqualTo(1));
                Assert.That(again.Response.Items[0].Action,    Is.EqualTo(WaitForPairingAction.SendNodeDescription));
                Assert.That(server.TryGetClientNode(nodeId, out var newNode), Is.True);
                Assert.That(newNode,                           Is.Not.SameAs(clientNode));
            });

        }

        #endregion


        #region MaxHangingRequests_AnswersUnavailable_WhileTheLimitIsReached()

        [Test]
        [S2C("LongPolling.Unavailable")]
        public async Task MaxHangingRequests_AnswersUnavailable_WhileTheLimitIsReached()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock, AutoRequestNodeDescriptions: false, MaxHangingRequests: 1);
            var nodeA          = Node_Id.NewRandom;
            var nodeB          = Node_Id.NewRandom;

            Assert.That(server.MaxHangingRequests, Is.EqualTo(1));

            var first = server.WaitAsync(Poll(nodeA));
            await YieldAsync();
            Assert.That(server.HangingRequests, Is.EqualTo(1));

            // The only slot is taken: the next request is rejected at once, without registering its node.
            var rejected = await server.WaitAsync(Poll(nodeB)).WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(rejected.Status,                          Is.EqualTo(LongPollingStatus.Unavailable));
                Assert.That(rejected.Response,                        Is.Null);
                Assert.That(server.HangingRequests,                   Is.EqualTo(1));
                Assert.That(server.TryGetClientNode(nodeB, out _),    Is.False);
                Assert.That(server.IsAcceptingRequests,               Is.True);
                Assert.That(first.IsCompleted,                        Is.False);
            });

            clock.Advance(ResponseTimeout);
            Assert.That((await first.WaitAsync(Wait)).Status, Is.EqualTo(LongPollingStatus.NoAction));
            Assert.That(server.HangingRequests, Is.Zero);

            // With the slot free again the next request is accepted.
            var accepted = server.WaitAsync(Poll(nodeB));
            await YieldAsync();

            Assert.Multiple(() => {
                Assert.That(accepted.IsCompleted,                     Is.False);
                Assert.That(server.HangingRequests,                   Is.EqualTo(1));
                Assert.That(server.TryGetClientNode(nodeB, out _),    Is.True);
            });

            clock.Advance(ResponseTimeout);
            Assert.That((await accepted.WaitAsync(Wait)).Status, Is.EqualTo(LongPollingStatus.NoAction));

        }

        #endregion

        #region Shutdown_ReleasesHangingRequests_AndRejectsNewOnes()

        [Test]
        [S2C("LongPolling.Unavailable")]
        public async Task Shutdown_ReleasesHangingRequests_AndRejectsNewOnes()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock, AutoRequestNodeDescriptions: false);
            var nodeId         = Node_Id.NewRandom;

            var hanging = server.WaitAsync(Poll(nodeId));
            await YieldAsync();

            Assert.Multiple(() => {
                Assert.That(hanging.IsCompleted,         Is.False);
                Assert.That(server.IsAcceptingRequests,  Is.True);
                Assert.That(server.HangingRequests,      Is.EqualTo(1));
            });

            server.Shutdown();

            // Released without any advance of the clock...
            var released  = await hanging.WaitAsync(Wait);
            var rejected  = await server.WaitAsync(Poll(Node_Id.NewRandom)).WaitAsync(Wait);

            Assert.That(server.TryGetClientNode(nodeId, out var clientNode), Is.True, "known nodes are kept");

            Assert.Multiple(() => {
                Assert.That(server.IsAcceptingRequests,  Is.False);
                Assert.That(released.Status,             Is.EqualTo(LongPollingStatus.Unavailable));
                Assert.That(released.Response,           Is.Null);
                Assert.That(rejected.Status,             Is.EqualTo(LongPollingStatus.Unavailable));
                Assert.That(server.HangingRequests,      Is.Zero);
                Assert.That(clientNode!.IsPolling,       Is.False);
            });

            Assert.That(() => server.Shutdown(), Throws.Nothing, "shutting down twice is harmless");

        }

        #endregion

        #region Dispose_ShutsDown_AndRejectsRequests()

        [Test]
        [S2C("LongPolling.Unavailable")]
        public async Task Dispose_ShutsDown_AndRejectsRequests()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock, AutoRequestNodeDescriptions: false);

            var hanging = server.WaitAsync(Poll(Node_Id.NewRandom));
            await YieldAsync();
            Assert.That(hanging.IsCompleted, Is.False);

            server.Dispose();

            var released  = await hanging.WaitAsync(Wait);
            var rejected  = await server.WaitAsync(Poll(Node_Id.NewRandom)).WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(server.IsAcceptingRequests,  Is.False);
                Assert.That(released.Status,             Is.EqualTo(LongPollingStatus.Unavailable));
                Assert.That(rejected.Status,             Is.EqualTo(LongPollingStatus.Unavailable));
                Assert.That(server.HangingRequests,      Is.Zero);
            });

            Assert.That(() => server.Dispose(), Throws.Nothing, "disposing twice is harmless");

        }

        #endregion

        #region CancelledToken_ThrowsOperationCanceledException_AndReleasesTheSlot()

        [Test]
        public async Task CancelledToken_ThrowsOperationCanceledException_AndReleasesTheSlot()
        {

            var clock          = NewClock();
            using var server   = NewServer(clock, AutoRequestNodeDescriptions: false);
            var nodeId         = Node_Id.NewRandom;

            using var cts = new CancellationTokenSource();

            var hanging = server.WaitAsync(Poll(nodeId), cts.Token);
            await YieldAsync();

            Assert.Multiple(() => {
                Assert.That(hanging.IsCompleted,     Is.False);
                Assert.That(server.HangingRequests,  Is.EqualTo(1));
            });

            await cts.CancelAsync();

            Assert.CatchAsync<OperationCanceledException>(async () => await hanging.WaitAsync(Wait));
            Assert.That(server.TryGetClientNode(nodeId, out var clientNode), Is.True, "the node was registered before the cancellation");

            Assert.Multiple(() => {
                Assert.That(server.HangingRequests,      Is.Zero);
                Assert.That(clientNode!.IsPolling,       Is.False);
                Assert.That(server.IsAcceptingRequests,  Is.True, "a cancelled request does not shut the server down");
            });

            // A token that is already cancelled never hangs.
            using var cancelled = new CancellationTokenSource();
            await cancelled.CancelAsync();

            Assert.CatchAsync<OperationCanceledException>(async () => await server.WaitAsync(Poll(nodeId), cancelled.Token).WaitAsync(Wait));

            Assert.Multiple(() => {
                Assert.That(server.HangingRequests,      Is.Zero);
                Assert.That(clientNode!.IsPolling,       Is.False);
            });

            // The server is still fully usable afterwards.
            Assert.That(server.SendAction(nodeId, WaitForPairingAction.RequestPairing), Is.True);

            var result = await server.WaitAsync(Poll(nodeId)).WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(result.Status,                          Is.EqualTo(LongPollingStatus.Actions));
                Assert.That(result.Response!.Items[0].Action,       Is.EqualTo(WaitForPairingAction.RequestPairing));
            });

        }

        #endregion

    }

}

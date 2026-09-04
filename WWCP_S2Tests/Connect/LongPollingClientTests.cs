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

using System.Diagnostics;
using System.Runtime.CompilerServices;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The long-polling client against the Phase 6 pairing server over real HTTP: every request
    /// lists every hosted node, the descriptions are sent when asked for, the prepare/cancel
    /// signals become events, "requestPairing" starts an automatic pairing attempt or reports
    /// the missing token once, and the status code policy of S2 Connect 1.0.0 ("Long-polling
    /// for constrained endpoints in the LAN") decides between polling again (204), waiting
    /// (503, 500, transport failures) and stopping (400, 401, 404); PLAN.md Phase 7.
    /// </summary>
    [TestFixture]
    public sealed class LongPollingClientTests
    {

        #region Helpers

        /// <summary>
        /// The upper bound of every wait.
        /// </summary>
        private static readonly TimeSpan  Wait           = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The server answers 204 after this time, so the client polls again quickly.
        /// </summary>
        private static readonly TimeSpan  ServerTimeout  = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// The delay of the client before the next request after a 503, a 500 or a transport failure.
        /// </summary>
        private static readonly TimeSpan  RetryDelay     = TimeSpan.FromMilliseconds(100);


        /// <summary>
        /// The test server options: the default test options with a short long-polling timeout.
        /// </summary>
        private static PairingServerOptions ServerOptions()

            => PairingServerFixture.DefaultOptions() with {
                   LongPollingTimeout = ServerTimeout
               };

        /// <summary>
        /// The test client options: short retry delays; the request timeout keeps its normative
        /// 30 seconds, as Validate() rejects smaller values.
        /// </summary>
        private static LongPollingClientOptions ClientOptions(Boolean AutoPair = true)

            => new () {
                   ServiceUnavailableDelay  = RetryDelay,
                   ServerErrorDelay         = RetryDelay,
                   AutoPair                 = AutoPair
               };


        /// <summary>
        /// Poll (bounded) until the given condition holds; never sleeps the thread.
        /// </summary>
        private static async Task WaitUntilAsync(Func<Boolean>  Condition,
                                                 TimeSpan?      Timeout      = null,
                                                 [CallerArgumentExpression(nameof(Condition))]
                                                 String?        Expression   = null)
        {

            var timeout  = Timeout ?? Wait;
            var timer    = Stopwatch.StartNew();

            while (!Condition())
            {

                if (timer.Elapsed >= timeout)
                    throw new TimeoutException($"The condition '{Expression}' was not met within {timeout}!");

                await Task.Delay(10);

            }

        }

        /// <summary>
        /// Wait (bounded) until the long-polling server of the fixture knows the given client
        /// node and has received its description (which the server asks for automatically).
        /// </summary>
        private static async Task<LongPollingClientNode> WaitUntilDescribedAsync(PairingServerFixture  Server,
                                                                                 HostedNode            Node)
        {

            var longPollingServer = Server.API.LongPollingServer ?? throw new InvalidOperationException("Long-polling is not enabled at this fixture!");

            await WaitUntilAsync(() => longPollingServer.TryGetClientNode(Node.Id, out var clientNode) && clientNode.Description is not null);

            if (!longPollingServer.TryGetClientNode(Node.Id, out var described))
                throw new InvalidOperationException($"Client node {Node.Id} is unknown to the long-polling server!");

            return described;

        }

        /// <summary>
        /// The numeric HTTP status code, or null.
        /// </summary>
        private static Int32? CodeOf(HTTPStatusCode? StatusCode)

            => StatusCode is null
                   ? null
                   : (Int32) StatusCode.Code;


        /// <summary>
        /// Records every event of a long-polling client; the snapshots are thread-safe.
        /// </summary>
        private sealed class EventRecorder
        {

            private readonly Lock                                                                lockObject  = new ();

            private readonly List<(HostedNode Node, WaitForPairingAction Action)>                actions     = [];
            private readonly List<HostedNode>                                                   prepared    = [];
            private readonly List<HostedNode>                                                   cancelled   = [];
            private readonly List<HostedNode>                                                   missing     = [];
            private readonly List<(HostedNode Node, PairingClientResult Result)>                completed   = [];
            private readonly List<(LongPollingStopReason Reason, HTTPStatusCode? StatusCode)>   stops       = [];

            public EventRecorder(LongPollingClient Client)
            {
                Client.OnActionReceived        += (_, _, node, action)  => Record(actions,   (node, action));
                Client.OnPreparePairing        += (_, _, node)          => Record(prepared,  node);
                Client.OnCancelPreparePairing  += (_, _, node)          => Record(cancelled, node);
                Client.OnMissingPairingToken   += (_, _, node)          => Record(missing,   node);
                Client.OnPairingCompleted      += (_, _, node, result)  => Record(completed, (node, result));
                Client.OnStopped               += (_, _, reason, code)  => Record(stops,     (reason, code));
            }

            private Task Record<T>(List<T>  Target,
                                   T        Item)
            {

                lock (lockObject)
                {
                    Target.Add(Item);
                }

                return Task.CompletedTask;

            }

            private IReadOnlyList<T> Snapshot<T>(List<T> Source)
            {
                lock (lockObject)
                {
                    return [.. Source];
                }
            }

            /// <summary>
            /// Every action the server sent, with the local node it was for.
            /// </summary>
            public IReadOnlyList<(HostedNode Node, WaitForPairingAction Action)>               Actions
                => Snapshot(actions);

            /// <summary>
            /// The local nodes that were asked to prepare pairing.
            /// </summary>
            public IReadOnlyList<HostedNode>                                                  Prepared
                => Snapshot(prepared);

            /// <summary>
            /// The local nodes whose prepare pairing signal was cancelled.
            /// </summary>
            public IReadOnlyList<HostedNode>                                                  Cancelled
                => Snapshot(cancelled);

            /// <summary>
            /// The local nodes that were asked to pair without a valid pairing token.
            /// </summary>
            public IReadOnlyList<HostedNode>                                                  MissingTokens
                => Snapshot(missing);

            /// <summary>
            /// The results of the automatic pairing attempts.
            /// </summary>
            public IReadOnlyList<(HostedNode Node, PairingClientResult Result)>               Completed
                => Snapshot(completed);

            /// <summary>
            /// Why the client stopped, with the status code that caused it.
            /// </summary>
            public IReadOnlyList<(LongPollingStopReason Reason, HTTPStatusCode? StatusCode)>  Stops
                => Snapshot(stops);

        }

        #endregion


        #region Start_AnnouncesTheNode_SendsItsDescriptionOnRequest_AndStopAsyncStops()

        [Test]
        [S2C("LongPolling.Client.204")]
        [S2C("LongPolling.Client.200.SendNodeDescription")]
        public async Task Start_AnnouncesTheNode_SendsItsDescriptionOnRequest_AndStopAsyncStops()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            var rm                   = client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            Assert.Multiple(() => {
                Assert.That(polling.Client,          Is.SameAs(client.Client));
                Assert.That(polling.IsRunning,       Is.False);
                Assert.That(polling.StopReason,      Is.Null);
                Assert.That(polling.PollCount,       Is.Zero);
                Assert.That(polling.LastStatusCode,  Is.Null);
            });

            polling.Start();

            Assert.That(polling.IsRunning, Is.True);

            // The first request registers the node and is answered at once with 'sendNodeDescription';
            // the second request carries the descriptions and hangs until the server timeout: 204.
            var clientNode  = await WaitUntilDescribedAsync(server, rm);
            var polls       = polling.PollCount;

            await WaitUntilAsync(() => polling.PollCount > polls);
            await WaitUntilAsync(() => CodeOf(polling.LastStatusCode) == 204);

            Assert.Multiple(() => {
                Assert.That(polling.IsRunning,                                  Is.True);
                Assert.That(polling.StopReason,                                 Is.Null);
                Assert.That(polling.PollCount,                                  Is.GreaterThan(polls), "the client polls again after every response");
                Assert.That(CodeOf(polling.LastStatusCode),                     Is.EqualTo(204));
                Assert.That(clientNode.Id,                                      Is.EqualTo(rm.Id));
                Assert.That(clientNode.Description,                             Is.EqualTo(rm.Description));
                Assert.That(clientNode.EndpointDescription,                     Is.EqualTo(client.Endpoint.Description));
                Assert.That(clientNode.LastError,                               Is.Null);
                Assert.That(recorder.Actions.Select(entry => entry.Action),     Does.Contain(WaitForPairingAction.SendNodeDescription));
                Assert.That(recorder.Actions.Select(entry => entry.Node),       Has.All.SameAs(rm));
                Assert.That(recorder.Prepared,                                  Is.Empty);
                Assert.That(recorder.Cancelled,                                 Is.Empty);
                Assert.That(recorder.MissingTokens,                             Is.Empty);
                Assert.That(recorder.Completed,                                 Is.Empty);
                Assert.That(recorder.Stops,                                     Is.Empty);
            });

            await polling.StopAsync();

            Assert.Multiple(() => {
                Assert.That(polling.IsRunning,                                  Is.False);
                Assert.That(polling.StopReason,                                 Is.EqualTo(LongPollingStopReason.Stopped));
                Assert.That(recorder.Stops.Select(entry => entry.Reason),       Is.EqualTo(new[] { LongPollingStopReason.Stopped }));
            });

        }

        #endregion

        #region Start_WithoutNodes_ThrowsInvalidOperationException()

        [Test]
        public async Task Start_WithoutNodes_ThrowsInvalidOperationException()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);
            await using var polling  = new LongPollingClient(client.Client, ClientOptions());

            Assert.That(client.Endpoint.Count, Is.Zero);
            Assert.That(() => polling.Start(), Throws.InvalidOperationException);

            Assert.Multiple(() => {
                Assert.That(polling.IsRunning,   Is.False);
                Assert.That(polling.PollCount,   Is.Zero);
                Assert.That(polling.StopReason,  Is.Null);
            });

        }

        #endregion

        #region Start_WhileRunning_ThrowsInvalidOperationException()

        [Test]
        public async Task Start_WhileRunning_ThrowsInvalidOperationException()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());

            polling.Start();

            Assert.That(() => polling.Start(), Throws.InvalidOperationException);
            Assert.That(polling.IsRunning,     Is.True, "the running loop is not affected");

            await WaitUntilAsync(() => polling.PollCount >= 1);
            await polling.StopAsync();

            Assert.Multiple(() => {
                Assert.That(polling.IsRunning,   Is.False);
                Assert.That(polling.StopReason,  Is.EqualTo(LongPollingStopReason.Stopped));
            });

        }

        #endregion

        #region NodesRemovedWhileRunning_StopsWithNoNodes()

        [Test]
        public async Task NodesRemovedWhileRunning_StopsWithNoNodes()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            var rm                   = client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            polling.Start();

            await WaitUntilDescribedAsync(server, rm);

            Assert.That(client.Endpoint.RemoveNode(rm.Id), Is.True);

            // The loop notices the empty endpoint before its next request.
            await WaitUntilAsync(() => !polling.IsRunning);

            Assert.Multiple(() => {
                Assert.That(polling.StopReason,                             Is.EqualTo(LongPollingStopReason.NoNodes));
                Assert.That(recorder.Stops.Select(entry => entry.Reason),   Is.EqualTo(new[] { LongPollingStopReason.NoNodes }));
            });

        }

        #endregion


        #region PreparePairing_RaisesOnPreparePairingForTheNode()

        [Test]
        [S2C("LongPolling.Client.200.PreparePairing")]
        public async Task PreparePairing_RaisesOnPreparePairingForTheNode()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            var rm                   = client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            polling.Start();

            var clientNode         = await WaitUntilDescribedAsync(server, rm);
            var longPollingServer  = server.API.LongPollingServer!;

            Assert.That(longPollingServer.SendAction(rm.Id, WaitForPairingAction.PreparePairing), Is.True, "the node is known to the long-polling server");

            await WaitUntilAsync(() => recorder.Prepared.Count == 1);

            Assert.Multiple(() => {
                Assert.That(recorder.Prepared[0],                               Is.SameAs(rm));
                Assert.That(recorder.Actions.Select(entry => entry.Action),     Does.Contain(WaitForPairingAction.PreparePairing));
                Assert.That(recorder.Cancelled,                                 Is.Empty);
                Assert.That(recorder.MissingTokens,                             Is.Empty);
                Assert.That(recorder.Completed,                                 Is.Empty);
                Assert.That(clientNode.PendingActions,                          Is.Empty, "the action was delivered");
                Assert.That(polling.IsRunning,                                  Is.True);
            });

        }

        #endregion

        #region CancelPreparePairing_RaisesOnCancelPreparePairingForTheNode()

        [Test]
        [S2C("LongPolling.Client.200.CancelPreparePairing")]
        public async Task CancelPreparePairing_RaisesOnCancelPreparePairingForTheNode()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            var rm                   = client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            polling.Start();

            await WaitUntilDescribedAsync(server, rm);

            var longPollingServer = server.API.LongPollingServer!;

            // The end user first intends to pair, then changes their mind.
            Assert.That(longPollingServer.SendAction(rm.Id, WaitForPairingAction.PreparePairing), Is.True);
            await WaitUntilAsync(() => recorder.Prepared.Count == 1);

            Assert.That(longPollingServer.SendAction(rm.Id, WaitForPairingAction.CancelPreparePairing), Is.True);
            await WaitUntilAsync(() => recorder.Cancelled.Count == 1);

            var signals = recorder.Actions.
                              Select(entry => entry.Action).
                              Where (action => action != WaitForPairingAction.SendNodeDescription).
                              ToList();

            Assert.Multiple(() => {
                Assert.That(recorder.Cancelled[0],   Is.SameAs(rm));
                Assert.That(recorder.Prepared,       Has.Count.EqualTo(1));
                Assert.That(signals,                 Is.EqualTo(new[] { WaitForPairingAction.PreparePairing, WaitForPairingAction.CancelPreparePairing }), "the actions arrive in order");
                Assert.That(recorder.MissingTokens,  Is.Empty);
                Assert.That(recorder.Completed,      Is.Empty);
                Assert.That(polling.IsRunning,       Is.True);
            });

        }

        #endregion


        #region RequestPairing_WithToken_PairsAutomatically()

        [Test]
        [S2C("LongPolling.Client.200.RequestPairing")]
        public async Task RequestPairing_WithToken_PairsAutomatically()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            var cem                  = server.AddNode(EnergyManagementRole.CEM);
            var rm                   = client.AddNode(EnergyManagementRole.RM);

            // The constrained RM shows its pairing code, the end user enters it at the CEM.
            var code                 = rm.IssueDynamicPairingToken();
            cem.EnterPairingToken(code.PairingToken, rm.Id);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            Assert.That(polling.Options.AutoPair, Is.True);

            polling.Start();

            await WaitUntilDescribedAsync(server, rm);

            Assert.That(server.API.LongPollingServer!.SendAction(rm.Id, WaitForPairingAction.RequestPairing), Is.True);

            await WaitUntilAsync(() => recorder.Completed.Count == 1);

            var (node, result)  = recorder.Completed[0];
            var clientPairing   = await client.Store.GetPairingAsync(rm.Id,  cem.Id);
            var serverPairing   = await server.Store.GetPairingAsync(cem.Id, rm.Id);

            Assert.Multiple(() => {
                Assert.That(result.IsSuccess,                          Is.True, result.ToString());
                Assert.That(result.Outcome,                            Is.EqualTo(PairingClientOutcome.Success));
                Assert.That(node,                                      Is.SameAs(rm));
                Assert.That(clientPairing,                             Is.Not.Null);
                Assert.That(serverPairing,                             Is.Not.Null);
                Assert.That(clientPairing!.RemoteNodeId,               Is.EqualTo(cem.Id));
                Assert.That(serverPairing!.RemoteNodeId,               Is.EqualTo(rm.Id));
                Assert.That(clientPairing.AccessToken,                 Is.EqualTo(serverPairing.AccessToken));
                Assert.That(clientPairing.LocalCommunicationRole,      Is.EqualTo(CommunicationRole.CommunicationClient));
                Assert.That(serverPairing.LocalCommunicationRole,      Is.EqualTo(CommunicationRole.CommunicationServer));
                Assert.That(result.Pairing,                            Is.EqualTo(clientPairing));
                Assert.That(rm.HasValidPairingToken,                   Is.False, "the dynamic token of the client node is consumed");
                Assert.That(server.CompletedPairings,                  Has.Count.EqualTo(1));
                Assert.That(client.Results,                            Has.Count.EqualTo(1), "the pairing client reported the attempt as well");
                Assert.That(recorder.MissingTokens,                    Is.Empty);
                Assert.That(polling.IsRunning,                         Is.True, "the polling loop continues after the pairing");
            });

            await polling.StopAsync();

            Assert.That(polling.StopReason, Is.EqualTo(LongPollingStopReason.Stopped));

        }

        #endregion

        #region RequestPairing_WithToken_AutoPairDisabled_StartsNoAttempt()

        [Test]
        [S2C("LongPolling.Client.200.RequestPairing")]
        public async Task RequestPairing_WithToken_AutoPairDisabled_StartsNoAttempt()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            var cem                  = server.AddNode(EnergyManagementRole.CEM);
            var rm                   = client.AddNode(EnergyManagementRole.RM);

            var code                 = rm.IssueDynamicPairingToken();
            cem.EnterPairingToken(code.PairingToken, rm.Id);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions(AutoPair: false));
            var recorder             = new EventRecorder(polling);

            polling.Start();

            await WaitUntilDescribedAsync(server, rm);

            Assert.That(server.API.LongPollingServer!.SendAction(rm.Id, WaitForPairingAction.RequestPairing), Is.True);

            await WaitUntilAsync(() => recorder.Actions.Any(entry => entry.Action == WaitForPairingAction.RequestPairing));

            // Two more polls: an automatic attempt would have reached the server by then.
            var polls = polling.PollCount;
            await WaitUntilAsync(() => polling.PollCount >= polls + 2);

            Assert.Multiple(() => {
                Assert.That(polling.Options.AutoPair,     Is.False);
                Assert.That(server.API.Attempts,          Is.Empty, "no pairing attempt was started");
                Assert.That(server.CompletedPairings,     Is.Empty);
                Assert.That(recorder.Completed,           Is.Empty);
                Assert.That(recorder.MissingTokens,       Is.Empty, "the node has a valid token, it just does not pair automatically");
                Assert.That(client.Results,               Is.Empty);
                Assert.That(rm.HasValidPairingToken,      Is.True, "the token is not consumed");
                Assert.That(polling.IsRunning,            Is.True);
            });

        }

        #endregion

        #region RequestPairing_WithoutToken_ReportsNoValidTokenOnPairingClientOnce()

        [Test]
        [S2C("LongPolling.Client.200.RequestPairing.NoToken")]
        public async Task RequestPairing_WithoutToken_ReportsNoValidTokenOnPairingClientOnce()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            var rm                   = client.AddNode(EnergyManagementRole.RM);
            var longPollingServer    = server.API.LongPollingServer!;
            var errors               = new List<(Node_Id NodeId, WaitForPairingError Error)>();

            longPollingServer.OnClientNodeError += (_, _, node, error) => {
                                                       lock (errors)
                                                           errors.Add((node.Id, error));
                                                       return Task.CompletedTask;
                                                   };

            List<(Node_Id NodeId, WaitForPairingError Error)> Reported()
            {
                lock (errors)
                    return [.. errors];
            }

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            polling.Start();

            var clientNode = await WaitUntilDescribedAsync(server, rm);

            Assert.That(rm.HasValidPairingToken, Is.False, "the node never issued a token");
            Assert.That(longPollingServer.SendAction(rm.Id, WaitForPairingAction.RequestPairing), Is.True);

            // "the client must perform a new request with an errorMessage containing the value NoValidTokenOnPairingClient"
            await WaitUntilAsync(() => recorder.MissingTokens.Count == 1);
            await WaitUntilAsync(() => Reported().Count == 1);

            // The error travels with exactly one request: two more polls do not repeat it.
            var polls = polling.PollCount;
            await WaitUntilAsync(() => polling.PollCount >= polls + 2);

            var reported = Reported();

            Assert.That(reported, Has.Count.EqualTo(1), "the error is reported once");

            Assert.Multiple(() => {
                Assert.That(recorder.MissingTokens[0],   Is.SameAs(rm));
                Assert.That(reported[0].NodeId,          Is.EqualTo(rm.Id));
                Assert.That(reported[0].Error,           Is.EqualTo(WaitForPairingError.NoValidTokenOnPairingClient));
                Assert.That(clientNode.LastError,        Is.EqualTo(WaitForPairingError.NoValidTokenOnPairingClient));
                Assert.That(clientNode.LastErrorAt,      Is.Not.Null);
                Assert.That(recorder.Completed,          Is.Empty, "no attempt without a token");
                Assert.That(server.API.Attempts,         Is.Empty);
                Assert.That(client.Results,              Is.Empty);
                Assert.That(polling.IsRunning,           Is.True);
            });

        }

        #endregion


        #region TwoNodes_AreListedInEveryRequest_AndActionsReachTheRightNode()

        [Test]
        [S2C("LongPolling.ClientNodes")]
        public async Task TwoNodes_AreListedInEveryRequest_AndActionsReachTheRightNode()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            var rm1                  = client.AddNode(EnergyManagementRole.RM);
            var rm2                  = client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            polling.Start();

            var node1              = await WaitUntilDescribedAsync(server, rm1);
            var node2              = await WaitUntilDescribedAsync(server, rm2);
            var longPollingServer  = server.API.LongPollingServer!;

            Assert.Multiple(() => {
                Assert.That(longPollingServer.ClientNodes.Select(node => node.Id),   Is.EquivalentTo(new[] { rm1.Id, rm2.Id }));
                Assert.That(node1.FirstSeen,                                         Is.EqualTo(node2.FirstSeen), "both nodes were announced by the same request");
                Assert.That(node1.Description,                                       Is.EqualTo(rm1.Description));
                Assert.That(node2.Description,                                       Is.EqualTo(rm2.Description));
                Assert.That(node1.EndpointDescription,                               Is.EqualTo(client.Endpoint.Description));
                Assert.That(node2.EndpointDescription,                               Is.EqualTo(client.Endpoint.Description));
                Assert.That(recorder.Actions.Where (entry => entry.Action == WaitForPairingAction.SendNodeDescription).
                                             Select(entry => entry.Node),            Is.EquivalentTo(new[] { rm1, rm2 }), "both nodes were asked for their descriptions");
            });

            Assert.That(longPollingServer.SendAction(rm2.Id, WaitForPairingAction.PreparePairing), Is.True);
            await WaitUntilAsync(() => recorder.Prepared.Count == 1);

            Assert.That(longPollingServer.SendAction(rm1.Id, WaitForPairingAction.CancelPreparePairing), Is.True);
            await WaitUntilAsync(() => recorder.Cancelled.Count == 1);

            Assert.Multiple(() => {
                Assert.That(recorder.Prepared[0],    Is.SameAs(rm2));
                Assert.That(recorder.Cancelled[0],   Is.SameAs(rm1));
                Assert.That(recorder.Prepared,       Has.Count.EqualTo(1));
                Assert.That(recorder.Cancelled,      Has.Count.EqualTo(1));
                Assert.That(polling.IsRunning,       Is.True);
            });

        }

        #endregion


        #region Server400_LongPollingNotAvailable_StopsWithNotAvailable()

        [Test]
        [S2C("LongPolling.Client.400")]
        public async Task Server400_LongPollingNotAvailable_StopsWithNotAvailable()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: PairingServerFixture.DefaultOptions() with { EnableLongPolling = false });
            await using var client   = PairingClientFixture.Create(server);

            client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            polling.Start();

            await WaitUntilAsync(() => !polling.IsRunning);

            var stops = recorder.Stops;

            Assert.That(stops, Has.Count.EqualTo(1), "OnStopped is raised exactly once");

            Assert.Multiple(() => {
                Assert.That(server.API.LANOperationsEnabled,   Is.True);
                Assert.That(server.API.LongPollingEnabled,     Is.False);
                Assert.That(polling.StopReason,                Is.EqualTo(LongPollingStopReason.NotAvailable));
                Assert.That(CodeOf(polling.LastStatusCode),    Is.EqualTo(400));
                Assert.That(polling.PollCount,                 Is.EqualTo(1), "a 400 stops the client at once");
                Assert.That(stops[0].Reason,                   Is.EqualTo(LongPollingStopReason.NotAvailable));
                Assert.That(CodeOf(stops[0].StatusCode),       Is.EqualTo(400));
            });

            Assert.DoesNotThrowAsync(async () => await polling.StopAsync(), "stopping a client that already stopped is harmless");
            Assert.That(polling.StopReason, Is.EqualTo(LongPollingStopReason.NotAvailable), "the reason is kept");

        }

        #endregion

        #region Server401_OutsideTheSubnet_StopsWithUnauthorized()

        [Test]
        [S2C("LongPolling.Client.401")]
        public async Task Server401_OutsideTheSubnet_StopsWithUnauthorized()
        {

            await using var server   = await PairingServerFixture.CreateAsync(SubnetPolicy: DenyAllSubnetPolicy.Instance);
            await using var client   = PairingClientFixture.Create(server);

            client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            polling.Start();

            await WaitUntilAsync(() => !polling.IsRunning);

            var stops = recorder.Stops;

            Assert.That(stops, Has.Count.EqualTo(1), "OnStopped is raised exactly once");

            Assert.Multiple(() => {
                Assert.That(server.API.LongPollingEnabled,                Is.True, "long-polling is served, but not for this client");
                Assert.That(polling.StopReason,                           Is.EqualTo(LongPollingStopReason.Unauthorized));
                Assert.That(CodeOf(polling.LastStatusCode),               Is.EqualTo(401));
                Assert.That(polling.PollCount,                            Is.EqualTo(1), "a 401 stops the client at once");
                Assert.That(stops[0].Reason,                              Is.EqualTo(LongPollingStopReason.Unauthorized));
                Assert.That(CodeOf(stops[0].StatusCode),                  Is.EqualTo(401));
                Assert.That(server.API.LongPollingServer!.ClientNodes,    Is.Empty, "a rejected request registers no client node");
            });

        }

        #endregion

        #region Server404_WANEndpoint_StopsWithNotImplemented()

        [Test]
        [S2C("LAN.NotImplemented")]
        public async Task Server404_WANEndpoint_StopsWithNotImplemented()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Deployment.WAN);
            await using var client   = PairingClientFixture.Create(server);

            client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            polling.Start();

            await WaitUntilAsync(() => !polling.IsRunning);

            var stops = recorder.Stops;

            Assert.That(stops, Has.Count.EqualTo(1), "OnStopped is raised exactly once");

            Assert.Multiple(() => {
                Assert.That(server.API.LANOperationsEnabled,   Is.False);
                Assert.That(server.API.LongPollingServer,      Is.Null);
                Assert.That(polling.StopReason,                Is.EqualTo(LongPollingStopReason.NotImplemented));
                Assert.That(CodeOf(polling.LastStatusCode),    Is.EqualTo(404));
                Assert.That(polling.PollCount,                 Is.EqualTo(1), "a 404 stops the client at once");
                Assert.That(stops[0].Reason,                   Is.EqualTo(LongPollingStopReason.NotImplemented));
                Assert.That(CodeOf(stops[0].StatusCode),       Is.EqualTo(404));
            });

        }

        #endregion

        #region Server503_AfterShutdown_KeepsPollingUntilStopped()

        [Test]
        [S2C("LongPolling.Client.503")]
        public async Task Server503_AfterShutdown_KeepsPollingUntilStopped()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            var rm                   = client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            polling.Start();

            await WaitUntilDescribedAsync(server, rm);

            // The shutdown releases the hanging request with 503 and answers every further request with 503.
            await server.API.ShutdownAsync();

            await WaitUntilAsync(() => CodeOf(polling.LastStatusCode) == 503);

            // Every 503 is followed by ServiceUnavailableDelay and a new request.
            var polls = polling.PollCount;
            await WaitUntilAsync(() => polling.PollCount >= polls + 3);

            Assert.Multiple(() => {
                Assert.That(server.API.IsShutdown,                          Is.True);
                Assert.That(polling.IsRunning,                              Is.True, "a 503 does not stop the client");
                Assert.That(polling.StopReason,                             Is.Null);
                Assert.That(CodeOf(polling.LastStatusCode),                 Is.EqualTo(503));
                Assert.That(recorder.Stops,                                 Is.Empty);
            });

            await polling.StopAsync();

            Assert.Multiple(() => {
                Assert.That(polling.IsRunning,                              Is.False);
                Assert.That(polling.StopReason,                             Is.EqualTo(LongPollingStopReason.Stopped));
                Assert.That(recorder.Stops.Select(entry => entry.Reason),   Is.EqualTo(new[] { LongPollingStopReason.Stopped }));
            });

        }

        #endregion

        #region TransportFailure_KeepsRetryingUntilStopped()

        [Test]
        [S2C("LongPolling.Client.500")]
        public async Task TransportFailure_KeepsRetryingUntilStopped()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());

            // Nothing listens at port 1: every request fails before any HTTP status code exists.
            await using var client   = PairingClientFixture.Create(server, PairingUrl: S2BaseURL.Parse("http://127.0.0.1:1/pairing/", AllowHTTP: true));

            client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            polling.Start();

            await WaitUntilAsync(() => polling.PollCount >= 1);

            // Every failed request is followed by ServerErrorDelay and a new request.
            await WaitUntilAsync(() => polling.PollCount >= 3 || !polling.IsRunning);

            Assert.That(polling.IsRunning, Is.True, $"the client must keep retrying after transport failures, but stopped with '{polling.StopReason}' after {polling.PollCount} request(s)");

            Assert.Multiple(() => {
                Assert.That(polling.StopReason,                             Is.Null);
                Assert.That(polling.PollCount,                              Is.GreaterThanOrEqualTo(3));
                Assert.That(polling.LastStatusCode,                         Is.Not.Null);
                Assert.That(CodeOf(polling.LastStatusCode),                 Is.Zero, "a transport failure has no HTTP status code");
                Assert.That(recorder.Stops,                                 Is.Empty);
                Assert.That(server.API.LongPollingServer!.ClientNodes,      Is.Empty, "the server was never reached");
            });

            await polling.StopAsync();

            Assert.Multiple(() => {
                Assert.That(polling.IsRunning,                              Is.False);
                Assert.That(polling.StopReason,                             Is.EqualTo(LongPollingStopReason.Stopped));
                Assert.That(recorder.Stops.Select(entry => entry.Reason),   Is.EqualTo(new[] { LongPollingStopReason.Stopped }));
            });

        }

        #endregion


        #region DisposeAsync_StopsARunningClient()

        [Test]
        public async Task DisposeAsync_StopsARunningClient()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());
            var recorder             = new EventRecorder(polling);

            polling.Start();

            await WaitUntilAsync(() => polling.PollCount >= 1);

            await polling.DisposeAsync();

            var stops = recorder.Stops;

            Assert.That(stops, Has.Count.EqualTo(1), "OnStopped is raised exactly once");

            Assert.Multiple(() => {
                Assert.That(polling.IsRunning,     Is.False);
                Assert.That(polling.StopReason,    Is.EqualTo(LongPollingStopReason.Disposed).Or.EqualTo(LongPollingStopReason.Stopped));
                Assert.That(polling.StopReason,    Is.EqualTo(stops[0].Reason), "the event reports the same reason");
            });

            Assert.That(() => polling.Start(), Throws.TypeOf<ObjectDisposedException>());
            Assert.DoesNotThrowAsync(async () => await polling.DisposeAsync(), "disposing twice is harmless");

        }

        #endregion

        #region Start_AfterDispose_ThrowsObjectDisposedException()

        [Test]
        public async Task Start_AfterDispose_ThrowsObjectDisposedException()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            client.AddNode(EnergyManagementRole.RM);

            await using var polling  = new LongPollingClient(client.Client, ClientOptions());

            await polling.DisposeAsync();

            Assert.That(() => polling.Start(), Throws.TypeOf<ObjectDisposedException>());

            Assert.Multiple(() => {
                Assert.That(polling.IsRunning,   Is.False);
                Assert.That(polling.PollCount,   Is.Zero);
                Assert.That(polling.StopReason,  Is.Null, "a client that never ran has no stop reason");
            });

        }

        #endregion


        #region Constructor_UsesTheDefaultOptions_AndRejectsInvalidOptions()

        [Test]
        public async Task Constructor_UsesTheDefaultOptions_AndRejectsInvalidOptions()
        {

            await using var server   = await PairingServerFixture.CreateAsync(Options: ServerOptions());
            await using var client   = PairingClientFixture.Create(server);

            await using var polling  = new LongPollingClient(client.Client);

            Assert.Multiple(() => {
                Assert.That(polling.Client,    Is.SameAs(client.Client));
                Assert.That(polling.Options,   Is.SameAs(LongPollingClientOptions.Default));
                Assert.That(polling.IsRunning, Is.False);
            });

            Assert.That(() => { using var rejected = new LongPollingClient(client.Client, new LongPollingClientOptions { RequestTimeout = TimeSpan.FromSeconds(29) }); },
                        Throws.TypeOf<ArgumentOutOfRangeException>(),
                        "the constructor validates the options");

        }

        #endregion

        #region Options_Validate_RejectsShortRequestTimeoutAndNegativeDelays()

        [Test]
        public void Options_Validate_RejectsShortRequestTimeoutAndNegativeDelays()
        {

            Assert.Multiple(() => {

                Assert.That(LongPollingClientOptions.Default.RequestTimeout,            Is.EqualTo(S2ConnectDefaults.LongPollingClientTimeout));
                Assert.That(LongPollingClientOptions.Default.RequestTimeout,            Is.EqualTo(TimeSpan.FromSeconds(30)));
                Assert.That(LongPollingClientOptions.Default.AutoPair,                  Is.True);
                Assert.That(LongPollingClientOptions.Default.ForcePairing,              Is.False);
                Assert.That(LongPollingClientOptions.Default.PairingTarget,             Is.Null);
                Assert.That(LongPollingClientOptions.Default.ServerErrorDelay,          Is.GreaterThan(TimeSpan.Zero));
                Assert.That(LongPollingClientOptions.Default.ServiceUnavailableDelay,   Is.GreaterThan(TimeSpan.Zero));

                Assert.That(() => LongPollingClientOptions.Default.Validate(),  Throws.Nothing);
                Assert.That(() => ClientOptions().Validate(),                  Throws.Nothing);

                Assert.That(() => new LongPollingClientOptions { RequestTimeout          = TimeSpan.FromSeconds(29)         }.Validate(),
                            Throws.TypeOf<ArgumentOutOfRangeException>(), "shorter than the normative 30 seconds");

                Assert.That(() => new LongPollingClientOptions { RequestTimeout          = TimeSpan.FromSeconds(30)         }.Validate(),
                            Throws.Nothing, "exactly the normative 30 seconds");

                Assert.That(() => new LongPollingClientOptions { ServerErrorDelay        = TimeSpan.FromSeconds(-1)         }.Validate(),
                            Throws.TypeOf<ArgumentOutOfRangeException>());

                Assert.That(() => new LongPollingClientOptions { ServiceUnavailableDelay = TimeSpan.FromMilliseconds(-1)    }.Validate(),
                            Throws.TypeOf<ArgumentOutOfRangeException>());

                Assert.That(() => new LongPollingClientOptions { ServerErrorDelay = TimeSpan.Zero, ServiceUnavailableDelay = TimeSpan.Zero }.Validate(),
                            Throws.Nothing, "zero delays are allowed");

            });

        }

        #endregion

    }

}

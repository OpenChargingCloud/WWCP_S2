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
using System.Net;

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The LAN-LAN only operations of the pairing server over real HTTP with a plain HttpClient:
    /// GET endpoint, GET nodes, preparePairing, cancelPreparePairing and the long-polling
    /// operation waitForPairing, the subnet check (401) and the 404 of endpoints that do not
    /// serve them (S2 Connect 1.0.0, "LAN-LAN only interactions" and "Long-polling for
    /// constrained endpoints in the LAN"; PLAN.md Phase 6b).
    /// </summary>
    [TestFixture]
    public sealed class LANOperationsHTTPTests
    {

        #region Helpers

        /// <summary>
        /// The upper bound of every wait for a hanging request.
        /// </summary>
        private static readonly TimeSpan  Wait                = TimeSpan.FromSeconds(5);

        /// <summary>
        /// A long-polling timeout bounding the tests in which a hanging request is released by
        /// an action, a shutdown or a rejected concurrent request rather than by the timeout.
        /// </summary>
        private static readonly TimeSpan  BoundedLongPolling  = TimeSpan.FromSeconds(3);


        /// <summary>
        /// A waitForPairing request for the node of the given client: by default only the
        /// client node id, optionally with the node and endpoint descriptions and an error.
        /// </summary>
        private static WaitForPairingRequest PollRequest(TestPairingClient     Client,
                                                         Boolean               WithDescriptions   = false,
                                                         WaitForPairingError?  Error              = null)

            => new ([
                   WithDescriptions
                       ? new WaitForPairingRequestItem(Client.Node.Id, Client.Node, Client.Endpoint, Error)
                       : new WaitForPairingRequestItem(Client.Node.Id, ErrorMessage: Error)
               ]);


        /// <summary>
        /// Wait (bounded) until the given client node has a hanging waitForPairing request at the
        /// long-polling server of the fixture; fails early when the request was already answered.
        /// </summary>
        private static async Task<LongPollingClientNode> WaitUntilPollingAsync(PairingServerFixture  Fixture,
                                                                               Node_Id               ClientNodeId,
                                                                               Task<HTTPResult>      Polling)
        {

            var server  = Fixture.API.LongPollingServer ?? throw new InvalidOperationException("Long-polling is not enabled at this fixture!");
            var timer   = Stopwatch.StartNew();

            while (timer.Elapsed < Wait)
            {

                if (Polling.IsCompleted)
                {
                    var result = await Polling;
                    Assert.Fail($"The waitForPairing request was answered with {(Int32) result.Status} before an action was queued: '{result.Body}'");
                }

                if (server.TryGetClientNode(ClientNodeId, out var clientNode) && clientNode.IsPolling)
                    return clientNode;

                await Task.Delay(10);

            }

            throw new TimeoutException($"Client node {ClientNodeId} did not start polling within {Wait}!");

        }


        /// <summary>
        /// Start a waitForPairing request that hangs: the client sends its descriptions with its
        /// first request, so nothing is queued for it, and return as soon as the long-polling
        /// server has registered the hanging request.
        /// </summary>
        private static async Task<(Task<HTTPResult> Polling, LongPollingClientNode Node)> StartHangingPollAsync(PairingServerFixture  Fixture,
                                                                                                                  TestPairingClient     Client)
        {

            var polling  = Fixture.PostAsync("v1/waitForPairing", PollRequest(Client, WithDescriptions: true).ToJSON());
            var node     = await WaitUntilPollingAsync(Fixture, Client.Node.Id, polling);

            return (polling, node);

        }


        /// <summary>
        /// Call all five LAN-only operations with valid bodies and collect their results.
        /// </summary>
        private static async Task<List<(String Operation, HTTPResult Result)>> CallAllLANOperationsAsync(PairingServerFixture  Fixture,
                                                                                                          TestPairingClient     Client,
                                                                                                          Node_Id               ServerNodeId)
        {

            var results = new List<(String Operation, HTTPResult Result)>();

            results.Add(("GET endpoint",               await Fixture.GetAsync ("v1/endpoint")));
            results.Add(("GET nodes",                  await Fixture.GetAsync ("v1/nodes")));
            results.Add(("POST preparePairing",        await Fixture.PostAsync("v1/preparePairing",       new PreparePairingRequest(Client.Node, Client.Endpoint, ServerNodeId).ToJSON())));
            results.Add(("POST cancelPreparePairing",  await Fixture.PostAsync("v1/cancelPreparePairing", new CancelPreparePairingRequest(Client.Node.Id, ServerNodeId).ToJSON())));
            results.Add(("POST waitForPairing",        await Fixture.PostAsync("v1/waitForPairing",       PollRequest(Client).ToJSON())));

            return results;

        }


        /// <summary>
        /// A readable summary of the results of all LAN-only operations for assertion messages.
        /// </summary>
        private static String Describe(IEnumerable<(String Operation, HTTPResult Result)> Results)

            => String.Join(", ", Results.Select(entry => $"{entry.Operation}: {(Int32) entry.Result.Status} '{entry.Result.Body}'"));

        #endregion


        #region GetEndpoint_ReturnsTheEndpointDescription()

        [Test]
        [S2C("LAN.Endpoint")]
        public async Task GetEndpoint_ReturnsTheEndpointDescription()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(EndpointName: "Living room EMS");

            var result = await fixture.GetAsync("v1/endpoint");

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK), result.Body);
            Assert.That(EndpointDescription.TryParse(result.Object, out var description, out var error), Is.True, error);

            Assert.Multiple(() => {
                Assert.That(result.ContentType,                            Does.StartWith("application/json"));
                Assert.That(result.CacheControl,                           Does.Contain("no-store"));
                Assert.That(result.Object["name"]?.      Value<String>(),  Is.EqualTo("Living room EMS"));
                Assert.That(result.Object["deployment"]?.Value<String>(),  Is.EqualTo("LAN"));
                Assert.That(description!.Name,                             Is.EqualTo("Living room EMS"));
                Assert.That(description.Deployment,                        Is.EqualTo(Deployment.LAN));
                Assert.That(description,                                   Is.EqualTo(fixture.Endpoint.Description));
            });

        }

        #endregion

        #region GetNodes_ReturnsOneDescriptionPerHostedNode()

        [Test]
        [S2C("LAN.Nodes")]
        public async Task GetNodes_ReturnsOneDescriptionPerHostedNode()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var rm   = fixture.AddNode(EnergyManagementRole.RM);
            var cem  = fixture.AddNode(EnergyManagementRole.CEM, Brand: "Other brand");

            var result = await fixture.GetAsync("v1/nodes");

            Assert.That(result.Status,       Is.EqualTo(HttpStatusCode.OK), result.Body);
            Assert.That(result.ContentType,  Does.StartWith("application/json"));
            Assert.That(result.Array,        Has.Count.EqualTo(2));

            var descriptions = new List<NodeDescription>();

            foreach (var item in result.Array)
            {
                Assert.That(NodeDescription.TryParse((JObject) item, out var description, out var error), Is.True, error);
                descriptions.Add(description!);
            }

            Assert.That(descriptions, Is.EquivalentTo(new[] { rm.Description, cem.Description }));

        }

        #endregion

        #region GetNodes_EmptyEndpoint_ReturnsAnEmptyArray()

        [Test]
        [S2C("LAN.Nodes")]
        public async Task GetNodes_EmptyEndpoint_ReturnsAnEmptyArray()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var result = await fixture.GetAsync("v1/nodes");

            Assert.Multiple(() => {
                Assert.That(result.Status,       Is.EqualTo(HttpStatusCode.OK), result.Body);
                Assert.That(result.ContentType,  Does.StartWith("application/json"));
                Assert.That(result.Array,        Is.Empty);
            });

        }

        #endregion


        #region LANOperations_FromWithinTheSubnet_AreServed()

        [Test]
        [S2C("LAN.SubnetCheck")]
        public async Task LANOperations_FromWithinTheSubnet_AreServed()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(SubnetPolicy: AllowAllSubnetPolicy.Instance);

            var rm      = fixture.AddNode(EnergyManagementRole.RM);
            var client  = new TestPairingClient(EnergyManagementRole.CEM);

            var results = await CallAllLANOperationsAsync(fixture, client, rm.Id);

            Assert.Multiple(() => {
                Assert.That(fixture.API.LANOperationsEnabled,  Is.True);
                Assert.That(fixture.API.LongPollingEnabled,    Is.True);
                Assert.That(fixture.API.LongPollingServer,     Is.Not.Null);
                Assert.That(results.Select(entry => (Int32) entry.Result.Status), Is.EqualTo(new[] { 200, 200, 204, 204, 200 }), Describe(results));
            });

        }

        #endregion

        #region LANOperations_FromOutsideTheSubnet_Return401()

        [Test]
        [S2C("LAN.SubnetCheck")]
        public async Task LANOperations_FromOutsideTheSubnet_Return401()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(SubnetPolicy: DenyAllSubnetPolicy.Instance);

            var rm      = fixture.AddNode(EnergyManagementRole.RM);
            var client  = new TestPairingClient(EnergyManagementRole.CEM);

            var results = await CallAllLANOperationsAsync(fixture, client, rm.Id);

            Assert.Multiple(() => {

                foreach (var (operation, result) in results)
                    Assert.That(result.Status, Is.EqualTo(HttpStatusCode.Unauthorized), $"{operation}: {result.Body}");

                Assert.That(results[0].Result.WWWAuthenticate,           Is.Not.Null);
                Assert.That(fixture.PreparePairingRequests,              Is.Empty);
                Assert.That(fixture.CancelPreparePairingRequests,        Is.Empty);
                Assert.That(fixture.API.LongPollingServer!.ClientNodes,  Is.Empty);

            });

        }

        #endregion

        #region LANOperations_OnWANEndpoint_Return404()

        [Test]
        [S2C("LAN.NotImplemented")]
        public async Task LANOperations_OnWANEndpoint_Return404()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.WAN);

            var rm      = fixture.AddNode(EnergyManagementRole.RM);
            var client  = new TestPairingClient(EnergyManagementRole.CEM, Deployment.WAN);

            var results = await CallAllLANOperationsAsync(fixture, client, rm.Id);

            Assert.Multiple(() => {

                Assert.That(fixture.API.LANOperationsEnabled,  Is.False);
                Assert.That(fixture.API.LongPollingEnabled,    Is.False);
                Assert.That(fixture.API.LongPollingServer,     Is.Null);

                foreach (var (operation, result) in results)
                    Assert.That(result.Status, Is.EqualTo(HttpStatusCode.NotFound), $"{operation}: {result.Body}");

                Assert.That(fixture.PreparePairingRequests,        Is.Empty);
                Assert.That(fixture.CancelPreparePairingRequests,  Is.Empty);

            });

        }

        #endregion

        #region LANOperations_OnWANPairingServerForLANEndpoint_Return404()

        [Test]
        [S2C("LAN.NotImplemented")]
        public async Task LANOperations_OnWANPairingServerForLANEndpoint_Return404()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.LAN, IsWANPairingServerForLANEndpoint: true);

            var rm      = fixture.AddNode(EnergyManagementRole.RM);
            var client  = new TestPairingClient(EnergyManagementRole.CEM, Deployment.WAN);

            var results = await CallAllLANOperationsAsync(fixture, client, rm.Id);

            Assert.Multiple(() => {

                Assert.That(fixture.API.UsesLANChallengeResponse,  Is.False);
                Assert.That(fixture.API.LANOperationsEnabled,      Is.False);
                Assert.That(fixture.API.LongPollingServer,         Is.Null);

                foreach (var (operation, result) in results)
                    Assert.That(result.Status, Is.EqualTo(HttpStatusCode.NotFound), $"{operation}: {result.Body}");

            });

        }

        #endregion

        #region LANOperations_DisabledByOptions_Return404()

        [Test]
        [S2C("LAN.NotImplemented")]
        public async Task LANOperations_DisabledByOptions_Return404()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: PairingServerFixture.DefaultOptions() with { EnableLANOperations = false });

            var rm      = fixture.AddNode(EnergyManagementRole.RM);
            var client  = new TestPairingClient(EnergyManagementRole.CEM);

            var results = await CallAllLANOperationsAsync(fixture, client, rm.Id);

            Assert.Multiple(() => {

                Assert.That(fixture.Endpoint.Deployment,           Is.EqualTo(Deployment.LAN));
                Assert.That(fixture.API.UsesLANChallengeResponse,  Is.True);
                Assert.That(fixture.API.LANOperationsEnabled,      Is.False);
                Assert.That(fixture.API.LongPollingEnabled,        Is.False);

                foreach (var (operation, result) in results)
                    Assert.That(result.Status, Is.EqualTo(HttpStatusCode.NotFound), $"{operation}: {result.Body}");

            });

        }

        #endregion

        #region LongPollingWithoutLANOperations_IsRejected()

        [Test]
        [S2C("LongPolling.NotAvailable")]
        public void LongPollingWithoutLANOperations_IsRejected()
        {

            var options = PairingServerFixture.DefaultOptions() with {
                              EnableLANOperations  = false,
                              EnableLongPolling    = true
                          };

            Assert.ThrowsAsync<ArgumentException>(async () => {
                await using var fixture = await PairingServerFixture.CreateAsync(Options: options);
            });

        }

        #endregion


        #region PreparePairing_KnownNode_Returns204_AndRaisesOnPreparePairing()

        [Test]
        [S2C("LAN.PreparePairing")]
        public async Task PreparePairing_KnownNode_Returns204_AndRaisesOnPreparePairing()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var rm       = fixture.AddNode(EnergyManagementRole.RM);
            var client   = new TestPairingClient(EnergyManagementRole.CEM);
            var request  = new PreparePairingRequest(client.Node, client.Endpoint, rm.Id);

            var nodes    = new List<HostedNode?>();
            var remotes  = new List<String?>();

            fixture.API.OnPreparePairing += (_, _, _, node, remote) => {
                                                lock (nodes)
                                                {
                                                    nodes.  Add(node);
                                                    remotes.Add(remote);
                                                }
                                                return Task.CompletedTask;
                                            };

            var result = await fixture.PostAsync("v1/preparePairing", request.ToJSON());

            Assert.Multiple(() => {
                Assert.That(result.Status,                   Is.EqualTo(HttpStatusCode.NoContent), result.Body);
                Assert.That(result.Body,                     Is.Null.Or.Empty);
                Assert.That(fixture.PreparePairingRequests,  Is.EqualTo(new[] { request }));
                Assert.That(nodes,                           Is.EqualTo(new[] { rm }));
                Assert.That(remotes,                         Has.Count.EqualTo(1));
                Assert.That(remotes[0],                      Is.Not.Null.And.Not.Empty);
            });

        }

        #endregion

        #region PreparePairing_UnknownServerNode_Returns204_AndRaisesTheEventWithoutNode()

        [Test]
        [S2C("LAN.PreparePairing")]
        public async Task PreparePairing_UnknownServerNode_Returns204_AndRaisesTheEventWithoutNode()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            fixture.AddNode(EnergyManagementRole.RM);

            var client   = new TestPairingClient(EnergyManagementRole.CEM);
            var request  = new PreparePairingRequest(client.Node, client.Endpoint, Node_Id.NewRandom);

            var nodes    = new List<HostedNode?>();

            fixture.API.OnPreparePairing += (_, _, _, node, _) => {
                                                lock (nodes)
                                                    nodes.Add(node);
                                                return Task.CompletedTask;
                                            };

            var result = await fixture.PostAsync("v1/preparePairing", request.ToJSON());

            Assert.Multiple(() => {
                Assert.That(result.Status,                   Is.EqualTo(HttpStatusCode.NoContent), result.Body);
                Assert.That(fixture.PreparePairingRequests,  Is.EqualTo(new[] { request }));
                Assert.That(nodes,                           Has.Count.EqualTo(1));
                Assert.That(nodes[0],                        Is.Null);
            });

        }

        #endregion

        #region PreparePairing_NodeNotReadyForPairing_Returns400Other()

        [Test]
        [S2C("LAN.PreparePairing")]
        public async Task PreparePairing_NodeNotReadyForPairing_Returns400Other()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var rm      = fixture.AddNode(EnergyManagementRole.RM);
            rm.IsReadyForPairing = false;

            var client  = new TestPairingClient(EnergyManagementRole.CEM);

            var result  = await fixture.PostAsync("v1/preparePairing", new PreparePairingRequest(client.Node, client.Endpoint, rm.Id).ToJSON());

            Assert.Multiple(() => {
                Assert.That(result.Status,                   Is.EqualTo(HttpStatusCode.BadRequest), result.Body);
                Assert.That(result.ErrorMessage,             Is.EqualTo("Other"));
                Assert.That(result.AdditionalInfo,           Does.Contain("not ready"));
                Assert.That(fixture.PreparePairingRequests,  Is.Empty);
            });

        }

        #endregion

        #region PreparePairing_SameRole_Returns400InvalidCombinationOfRoles()

        [Test]
        [S2C("LAN.PreparePairing")]
        public async Task PreparePairing_SameRole_Returns400InvalidCombinationOfRoles()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var rm      = fixture.AddNode(EnergyManagementRole.RM);
            var client  = new TestPairingClient(EnergyManagementRole.RM);

            var result  = await fixture.PostAsync("v1/preparePairing", new PreparePairingRequest(client.Node, client.Endpoint, rm.Id).ToJSON());

            Assert.Multiple(() => {
                Assert.That(result.Status,                   Is.EqualTo(HttpStatusCode.BadRequest), result.Body);
                Assert.That(result.ErrorMessage,             Is.EqualTo("InvalidCombinationOfRoles"));
                Assert.That(fixture.PreparePairingRequests,  Is.Empty);
            });

        }

        #endregion

        #region PreparePairing_InvalidBody_Returns400ParsingError()

        [Test]
        [S2C("LAN.PreparePairing")]
        public async Task PreparePairing_InvalidBody_Returns400ParsingError()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            fixture.AddNode(EnergyManagementRole.RM);

            var invalidJSON       = await fixture.PostAsync("v1/preparePairing", RawBody: "{ not json");
            var notAnObject       = await fixture.PostAsync("v1/preparePairing", RawBody: "[]");
            var missingProperties = await fixture.PostAsync("v1/preparePairing", RawBody: "{}");

            Assert.Multiple(() => {

                Assert.That(invalidJSON.Status,               Is.EqualTo(HttpStatusCode.BadRequest), invalidJSON.Body);
                Assert.That(invalidJSON.ErrorMessage,         Is.EqualTo("ParsingError"));

                Assert.That(notAnObject.Status,               Is.EqualTo(HttpStatusCode.BadRequest), notAnObject.Body);
                Assert.That(notAnObject.ErrorMessage,         Is.EqualTo("ParsingError"));

                Assert.That(missingProperties.Status,         Is.EqualTo(HttpStatusCode.BadRequest), missingProperties.Body);
                Assert.That(missingProperties.ErrorMessage,   Is.EqualTo("ParsingError"));

                Assert.That(fixture.PreparePairingRequests,   Is.Empty);

            });

        }

        #endregion


        #region CancelPreparePairing_KnownNodes_Returns204_AndRaisesOnCancelPreparePairing()

        [Test]
        [S2C("LAN.CancelPreparePairing")]
        public async Task CancelPreparePairing_KnownNodes_Returns204_AndRaisesOnCancelPreparePairing()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var rm       = fixture.AddNode(EnergyManagementRole.RM);
            var client   = new TestPairingClient(EnergyManagementRole.CEM);
            var request  = new CancelPreparePairingRequest(client.Node.Id, rm.Id);

            var nodes    = new List<HostedNode?>();

            fixture.API.OnCancelPreparePairing += (_, _, _, node, _) => {
                                                      lock (nodes)
                                                          nodes.Add(node);
                                                      return Task.CompletedTask;
                                                  };

            var result = await fixture.PostAsync("v1/cancelPreparePairing", request.ToJSON());

            Assert.Multiple(() => {
                Assert.That(result.Status,                         Is.EqualTo(HttpStatusCode.NoContent), result.Body);
                Assert.That(result.Body,                           Is.Null.Or.Empty);
                Assert.That(fixture.CancelPreparePairingRequests,  Is.EqualTo(new[] { request }));
                Assert.That(nodes,                                 Is.EqualTo(new[] { rm }));
            });

        }

        #endregion

        #region CancelPreparePairing_UnknownNodes_Returns204()

        [Test]
        [S2C("LAN.CancelPreparePairing")]
        public async Task CancelPreparePairing_UnknownNodes_Returns204()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            fixture.AddNode(EnergyManagementRole.RM);

            var request  = new CancelPreparePairingRequest(Node_Id.NewRandom, Node_Id.NewRandom);
            var nodes    = new List<HostedNode?>();

            fixture.API.OnCancelPreparePairing += (_, _, _, node, _) => {
                                                      lock (nodes)
                                                          nodes.Add(node);
                                                      return Task.CompletedTask;
                                                  };

            var result = await fixture.PostAsync("v1/cancelPreparePairing", request.ToJSON());

            Assert.Multiple(() => {
                Assert.That(result.Status,                         Is.EqualTo(HttpStatusCode.NoContent), result.Body);
                Assert.That(fixture.CancelPreparePairingRequests,  Is.EqualTo(new[] { request }));
                Assert.That(nodes,                                 Has.Count.EqualTo(1));
                Assert.That(nodes[0],                              Is.Null);
            });

        }

        #endregion

        #region CancelPreparePairing_InvalidBody_Returns400ParsingError()

        [Test]
        [S2C("LAN.CancelPreparePairing")]
        public async Task CancelPreparePairing_InvalidBody_Returns400ParsingError()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            fixture.AddNode(EnergyManagementRole.RM);

            var invalidJSON  = await fixture.PostAsync("v1/cancelPreparePairing", RawBody: "{ not json");
            var missingIds   = await fixture.PostAsync("v1/cancelPreparePairing", RawBody: "{}");

            Assert.Multiple(() => {
                Assert.That(invalidJSON.Status,                    Is.EqualTo(HttpStatusCode.BadRequest), invalidJSON.Body);
                Assert.That(invalidJSON.ErrorMessage,              Is.EqualTo("ParsingError"));
                Assert.That(missingIds.Status,                     Is.EqualTo(HttpStatusCode.BadRequest), missingIds.Body);
                Assert.That(missingIds.ErrorMessage,               Is.EqualTo("ParsingError"));
                Assert.That(fixture.CancelPreparePairingRequests,  Is.Empty);
            });

        }

        #endregion


        #region WaitForPairing_NewNode_IsAskedForItsDescription()

        [Test]
        [S2C("LAN.WaitForPairing")]
        [S2C("LongPolling.SendNodeDescription")]
        public async Task WaitForPairing_NewNode_IsAskedForItsDescription()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client  = new TestPairingClient(EnergyManagementRole.RM);
            var server  = fixture.API.LongPollingServer!;

            var result  = await fixture.PostAsync("v1/waitForPairing", PollRequest(client).ToJSON());

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK), result.Body);
            Assert.That(WaitForPairingResponse.TryParse(result.Array, out var response, out var error), Is.True, error);
            Assert.That(server.TryGetClientNode(client.Node.Id, out var clientNode), Is.True);

            Assert.Multiple(() => {
                Assert.That(result.ContentType,                            Does.StartWith("application/json"));
                Assert.That(result.CacheControl,                           Does.Contain("no-store"));
                Assert.That(result.Array[0]["clientNodeId"]?.Value<String>(), Is.EqualTo(client.Node.Id.ToString()));
                Assert.That(result.Array[0]["action"]?.      Value<String>(), Is.EqualTo("sendNodeDescription"));
                Assert.That(response!.Items.Count,                         Is.EqualTo(1));
                Assert.That(response.Items[0].ClientNodeId,                Is.EqualTo(client.Node.Id));
                Assert.That(response.Items[0].Action,                      Is.EqualTo(WaitForPairingAction.SendNodeDescription));
                Assert.That(clientNode!.Description,                       Is.Null);
                Assert.That(clientNode.EndpointDescription,                Is.Null);
                Assert.That(clientNode.IsPolling,                          Is.False);
                Assert.That(clientNode.PendingActions,                     Is.Empty);
            });

        }

        #endregion

        #region WaitForPairing_WithoutActions_Returns204AfterTheLongPollingTimeout()

        [Test]
        [S2C("LAN.WaitForPairing")]
        [S2C("LongPolling.ServerTimeout")]
        public async Task WaitForPairing_WithoutActions_Returns204AfterTheLongPollingTimeout()
        {

            var timeout = TimeSpan.FromMilliseconds(300);

            await using var fixture = await PairingServerFixture.CreateAsync(Options: PairingServerFixture.DefaultOptions() with { LongPollingTimeout = timeout });

            var client  = new TestPairingClient(EnergyManagementRole.RM);
            var server  = fixture.API.LongPollingServer!;

            Assert.That(server.ResponseTimeout, Is.EqualTo(timeout));

            // The first poll of a new node is answered at once with 'sendNodeDescription'...
            var first = await fixture.PostAsync("v1/waitForPairing", PollRequest(client).ToJSON());
            Assert.That(first.Status, Is.EqualTo(HttpStatusCode.OK), first.Body);

            // ...the next request carries the descriptions and, as nothing is queued, hangs until the timeout.
            var timer   = Stopwatch.StartNew();
            var second  = await fixture.PostAsync("v1/waitForPairing", PollRequest(client, WithDescriptions: true).ToJSON());
            timer.Stop();

            Assert.That(server.TryGetClientNode(client.Node.Id, out var clientNode), Is.True);

            Assert.Multiple(() => {
                Assert.That(second.Status,                    Is.EqualTo(HttpStatusCode.NoContent), second.Body);
                Assert.That(second.JSON,                      Is.Null);
                Assert.That(timer.Elapsed,                    Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(250)), "the server must not answer before the long-polling timeout");
                Assert.That(clientNode!.Description,          Is.EqualTo(client.Node));
                Assert.That(clientNode.EndpointDescription,   Is.EqualTo(client.Endpoint));
                Assert.That(clientNode.IsPolling,             Is.False);
                Assert.That(clientNode.PendingActions,        Is.Empty);
                Assert.That(server.HangingRequests,           Is.Zero);
            });

        }

        #endregion

        #region WaitForPairing_ActionSentWhileHanging_Returns200WithTheAction()

        [Test]
        [S2C("LAN.WaitForPairing")]
        [S2C("LongPolling.Actions")]
        public async Task WaitForPairing_ActionSentWhileHanging_Returns200WithTheAction()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: PairingServerFixture.DefaultOptions() with { LongPollingTimeout = BoundedLongPolling });

            var client  = new TestPairingClient(EnergyManagementRole.RM);
            var server  = fixture.API.LongPollingServer!;

            var (polling, node) = await StartHangingPollAsync(fixture, client);

            // A second task queues the action a little later...
            var sender  = Task.Run(async () => {
                              await Task.Delay(100);
                              return server.SendAction(client.Node.Id, WaitForPairingAction.RequestPairing);
                          });

            var result  = await polling.WaitAsync(Wait);

            Assert.That(await sender,  Is.True, "the node is known to the long-polling server");
            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK), result.Body);
            Assert.That(WaitForPairingResponse.TryParse(result.Array, out var response, out var error), Is.True, error);

            Assert.Multiple(() => {
                Assert.That(result.Array.Count,                                  Is.EqualTo(1));
                Assert.That(result.Array[0]["clientNodeId"]?.Value<String>(),    Is.EqualTo(client.Node.Id.ToString()));
                Assert.That(result.Array[0]["action"]?.      Value<String>(),    Is.EqualTo("requestPairing"));
                Assert.That(response!.Items.Count,                               Is.EqualTo(1));
                Assert.That(response.Items[0].ClientNodeId,                      Is.EqualTo(client.Node.Id));
                Assert.That(response.Items[0].Action,                            Is.EqualTo(WaitForPairingAction.RequestPairing));
                Assert.That(node.IsPolling,                                      Is.False);
                Assert.That(node.PendingActions,                                 Is.Empty);
            });

        }

        #endregion

        #region WaitForPairing_ClientNodesAndIsPolling_FollowTheHangingRequest()

        [Test]
        [S2C("LAN.WaitForPairing")]
        [S2C("LongPolling.ClientNodes")]
        public async Task WaitForPairing_ClientNodesAndIsPolling_FollowTheHangingRequest()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: PairingServerFixture.DefaultOptions() with { LongPollingTimeout = BoundedLongPolling });

            var client  = new TestPairingClient(EnergyManagementRole.RM);
            var server  = fixture.API.LongPollingServer!;

            Assert.That(server.ClientNodes, Is.Empty);

            var (polling, node) = await StartHangingPollAsync(fixture, client);

            Assert.Multiple(() => {
                Assert.That(node.Id,                                         Is.EqualTo(client.Node.Id));
                Assert.That(node.IsPolling,                                  Is.True);
                Assert.That(node.Description,                                Is.EqualTo(client.Node));
                Assert.That(server.ClientNodes.Select(entry => entry.Id),    Is.EqualTo(new[] { client.Node.Id }));
                Assert.That(server.HangingRequests,                          Is.EqualTo(1));
                Assert.That(polling.IsCompleted,                             Is.False);
            });

            Assert.That(server.SendAction(client.Node.Id, WaitForPairingAction.PreparePairing), Is.True);

            var result = await polling.WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(result.Status,                                   Is.EqualTo(HttpStatusCode.OK), result.Body);
                Assert.That(result.Array[0]["action"]?.Value<String>(),      Is.EqualTo("preparePairing"));
                Assert.That(node.IsPolling,                                  Is.False);
                Assert.That(server.HangingRequests,                          Is.Zero);
                Assert.That(server.ClientNodes.Select(entry => entry.Id),    Is.EqualTo(new[] { client.Node.Id }), "the node stays known after its request returned");
            });

        }

        #endregion

        #region WaitForPairing_ErrorMessage_IsRecordedAndRaisesOnClientNodeError()

        [Test]
        [S2C("LAN.WaitForPairing")]
        [S2C("LongPolling.ErrorMessage")]
        public async Task WaitForPairing_ErrorMessage_IsRecordedAndRaisesOnClientNodeError()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client  = new TestPairingClient(EnergyManagementRole.RM);
            var server  = fixture.API.LongPollingServer!;

            var errors  = new List<(Node_Id NodeId, WaitForPairingError Error)>();

            server.OnClientNodeError += (_, _, node, error) => {
                                            lock (errors)
                                                errors.Add((node.Id, error));
                                            return Task.CompletedTask;
                                        };

            var result = await fixture.PostAsync("v1/waitForPairing", PollRequest(client, Error: WaitForPairingError.NoValidTokenOnPairingClient).ToJSON());

            // The new node is still asked for its description, so the request is answered at once.
            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK), result.Body);
            Assert.That(server.TryGetClientNode(client.Node.Id, out var clientNode), Is.True);
            Assert.That(errors, Has.Count.EqualTo(1));

            Assert.Multiple(() => {
                Assert.That(errors[0].NodeId,          Is.EqualTo(client.Node.Id));
                Assert.That(errors[0].Error,           Is.EqualTo(WaitForPairingError.NoValidTokenOnPairingClient));
                Assert.That(clientNode!.LastError,     Is.EqualTo(WaitForPairingError.NoValidTokenOnPairingClient));
                Assert.That(clientNode.LastErrorAt,    Is.Not.Null);
            });

        }

        #endregion

        #region WaitForPairing_LongPollingDisabled_Returns400Other()

        [Test]
        [S2C("LAN.WaitForPairing")]
        [S2C("LongPolling.NotAvailable")]
        public async Task WaitForPairing_LongPollingDisabled_Returns400Other()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: PairingServerFixture.DefaultOptions() with { EnableLongPolling = false });

            var client    = new TestPairingClient(EnergyManagementRole.RM);

            var result    = await fixture.PostAsync("v1/waitForPairing", PollRequest(client).ToJSON());
            var endpoint  = await fixture.GetAsync ("v1/endpoint");

            Assert.Multiple(() => {
                Assert.That(fixture.API.LANOperationsEnabled,  Is.True);
                Assert.That(fixture.API.LongPollingEnabled,    Is.False);
                Assert.That(fixture.API.LongPollingServer,     Is.Null);
                Assert.That(result.Status,                     Is.EqualTo(HttpStatusCode.BadRequest), result.Body);
                Assert.That(result.ErrorMessage,               Is.EqualTo("Other"));
                Assert.That(endpoint.Status,                   Is.EqualTo(HttpStatusCode.OK), "the other LAN-only operations are still served");
            });

        }

        #endregion

        #region WaitForPairing_InvalidBody_Returns400ParsingError()

        [Test]
        [S2C("LAN.WaitForPairing")]
        public async Task WaitForPairing_InvalidBody_Returns400ParsingError()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var nodeId       = Node_Id.NewRandom;

            var invalidJSON  = await fixture.PostAsync("v1/waitForPairing", RawBody: "[ not json");
            var anObject     = await fixture.PostAsync("v1/waitForPairing", RawBody: "{}");
            var emptyArray   = await fixture.PostAsync("v1/waitForPairing", RawBody: "[]");
            var noNodeId     = await fixture.PostAsync("v1/waitForPairing", RawBody: "[ {} ]");
            var duplicates   = await fixture.PostAsync("v1/waitForPairing", new JArray(new JObject(new JProperty("clientNodeId", nodeId.ToString())),
                                                                                       new JObject(new JProperty("clientNodeId", nodeId.ToString()))));

            Assert.Multiple(() => {

                Assert.That(invalidJSON.Status,        Is.EqualTo(HttpStatusCode.BadRequest), invalidJSON.Body);
                Assert.That(invalidJSON.ErrorMessage,  Is.EqualTo("ParsingError"));

                Assert.That(anObject.Status,           Is.EqualTo(HttpStatusCode.BadRequest), anObject.Body);
                Assert.That(anObject.ErrorMessage,     Is.EqualTo("ParsingError"));
                Assert.That(anObject.AdditionalInfo,   Does.Contain("array"));

                Assert.That(emptyArray.Status,         Is.EqualTo(HttpStatusCode.BadRequest), emptyArray.Body);
                Assert.That(emptyArray.ErrorMessage,   Is.EqualTo("ParsingError"));

                Assert.That(noNodeId.Status,           Is.EqualTo(HttpStatusCode.BadRequest), noNodeId.Body);
                Assert.That(noNodeId.ErrorMessage,     Is.EqualTo("ParsingError"));

                Assert.That(duplicates.Status,         Is.EqualTo(HttpStatusCode.BadRequest), duplicates.Body);
                Assert.That(duplicates.ErrorMessage,   Is.EqualTo("ParsingError"));

                Assert.That(fixture.API.LongPollingServer!.ClientNodes, Is.Empty, "rejected requests register no client node");

            });

        }

        #endregion

        #region WaitForPairing_AfterShutdown_Returns503_AndReleasesHangingRequests()

        [Test]
        [S2C("LAN.WaitForPairing")]
        [S2C("LongPolling.Unavailable")]
        public async Task WaitForPairing_AfterShutdown_Returns503_AndReleasesHangingRequests()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: PairingServerFixture.DefaultOptions() with { LongPollingTimeout = BoundedLongPolling });

            var hangingClient  = new TestPairingClient(EnergyManagementRole.RM);
            var lateClient     = new TestPairingClient(EnergyManagementRole.RM, EndpointName: "Late constrained endpoint");
            var server         = fixture.API.LongPollingServer!;

            var (polling, node) = await StartHangingPollAsync(fixture, hangingClient);

            await fixture.API.ShutdownAsync();

            var released  = await polling.WaitAsync(Wait);
            var rejected  = await fixture.PostAsync("v1/waitForPairing", PollRequest(lateClient).ToJSON());

            Assert.Multiple(() => {
                Assert.That(fixture.API.IsShutdown,         Is.True);
                Assert.That(server.IsAcceptingRequests,     Is.False);
                Assert.That(released.Status,                Is.EqualTo(HttpStatusCode.ServiceUnavailable), released.Body);
                Assert.That(released.RetryAfter,            Is.Not.Null);
                Assert.That(node.IsPolling,                 Is.False);
                Assert.That(rejected.Status,                Is.EqualTo(HttpStatusCode.ServiceUnavailable), rejected.Body);
                Assert.That(rejected.RetryAfter,            Is.Not.Null);
                Assert.That(server.HangingRequests,         Is.Zero);
                Assert.That(server.TryGetClientNode(lateClient.Node.Id, out _), Is.False, "a rejected request registers no client node");
            });

        }

        #endregion

        #region WaitForPairing_TooManyHangingRequests_Returns503()

        [Test]
        [S2C("LAN.WaitForPairing")]
        [S2C("LongPolling.Unavailable")]
        public async Task WaitForPairing_TooManyHangingRequests_Returns503()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: PairingServerFixture.DefaultOptions() with {
                                                                                          LongPollingTimeout             = BoundedLongPolling,
                                                                                          MaxHangingLongPollingRequests  = 1
                                                                                      });

            var firstClient   = new TestPairingClient(EnergyManagementRole.RM);
            var secondClient  = new TestPairingClient(EnergyManagementRole.RM, EndpointName: "Second constrained endpoint");
            var server        = fixture.API.LongPollingServer!;

            Assert.That(server.MaxHangingRequests, Is.EqualTo(1));

            var (polling, node) = await StartHangingPollAsync(fixture, firstClient);

            // The only slot is taken: a concurrent request is rejected at once...
            var rejected = await fixture.PostAsync("v1/waitForPairing", PollRequest(secondClient, WithDescriptions: true).ToJSON());

            Assert.Multiple(() => {
                Assert.That(rejected.Status,       Is.EqualTo(HttpStatusCode.ServiceUnavailable), rejected.Body);
                Assert.That(rejected.RetryAfter,   Is.Not.Null);
                Assert.That(polling.IsCompleted,   Is.False, "the first request keeps hanging");
                Assert.That(node.IsPolling,        Is.True);
                Assert.That(server.TryGetClientNode(secondClient.Node.Id, out _), Is.False, "a rejected request registers no client node");
            });

            // ...while the hanging one is still served.
            Assert.That(server.SendAction(firstClient.Node.Id, WaitForPairingAction.RequestPairing), Is.True);

            var result = await polling.WaitAsync(Wait);

            Assert.Multiple(() => {
                Assert.That(result.Status,                                  Is.EqualTo(HttpStatusCode.OK), result.Body);
                Assert.That(result.Array[0]["action"]?.Value<String>(),     Is.EqualTo("requestPairing"));
                Assert.That(server.HangingRequests,                         Is.Zero);
            });

            // With the slot free again the second client is served.
            var accepted = await fixture.PostAsync("v1/waitForPairing", PollRequest(secondClient).ToJSON());

            Assert.Multiple(() => {
                Assert.That(accepted.Status,                                Is.EqualTo(HttpStatusCode.OK), accepted.Body);
                Assert.That(accepted.Array[0]["action"]?.Value<String>(),   Is.EqualTo("sendNodeDescription"));
            });

        }

        #endregion

    }

}

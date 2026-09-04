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

using System.Net;

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The error paths of the pairing server over real HTTP: every error row of requestPairing
    /// (S2 Connect 1.0.0, "1. POST /[version]/requestPairing"), the 401, 403 and 400 answers of
    /// the bearer-authenticated steps ("Invalid interactions"), the interruption of the process
    /// (timeout, shutdown), rate limiting, store failures and the 404 of the LAN-only operations
    /// on WAN endpoints (PLAN.md Phase 6a).
    /// </summary>
    [TestFixture]
    public sealed class PairingErrorTests
    {

        #region Data

        /// <summary>
        /// A pairing attempt started through the fixture (steps 1 to 3 completed).
        /// </summary>
        private sealed record StartedAttempt(PairingServerFixture    Fixture,
                                             HostedNode              ServerNode,
                                             TestPairingClient       Client,
                                             String                  Bearer,
                                             RequestPairingResponse  Response,
                                             PairingAttempt          Attempt)
        {

            /// <summary>
            /// The correct response of the client to the server challenge.
            /// </summary>
            public HmacChallengeResponse CorrectServerResponse
                => Client.ServerChallengeResponse(Fixture, Response.ServerHmacChallenge);

            /// <summary>
            /// A well-formed but wrong response (the answer to another challenge).
            /// </summary>
            public HmacChallengeResponse WrongServerResponse
                => Client.ServerChallengeResponse(Fixture, TokenGenerator.NewChallenge());

        }

        /// <summary>
        /// A store whose writes fail, e.g. because the disk is full.
        /// </summary>
        private sealed class ThrowingStore : IS2Store
        {

            public ValueTask<IReadOnlyList<Pairing>> GetPairingsAsync(Node_Id?           LocalNodeId         = null,
                                                                      CancellationToken  CancellationToken   = default)
                => ValueTask.FromResult<IReadOnlyList<Pairing>>([]);

            public ValueTask<Pairing?> GetPairingAsync(Node_Id            LocalNodeId,
                                                       Node_Id            RemoteNodeId,
                                                       CancellationToken  CancellationToken   = default)
                => ValueTask.FromResult<Pairing?>(null);

            public ValueTask<Pairing?> AddOrReplacePairingAsync(Pairing            Pairing,
                                                                CancellationToken  CancellationToken   = default)
                => ValueTask.FromException<Pairing?>(new IOException("The disk is full!"));

            public ValueTask<Pairing?> RemovePairingAsync(Node_Id            LocalNodeId,
                                                          Node_Id            RemoteNodeId,
                                                          CancellationToken  CancellationToken   = default)
                => ValueTask.FromResult<Pairing?>(null);

            public ValueTask AddPendingAccessTokenAsync(PendingAccessToken PendingAccessToken, CancellationToken CancellationToken = default)
                => ValueTask.FromException(new IOException("The disk is full!"));

            public ValueTask<IReadOnlyList<PendingAccessToken>> GetPendingAccessTokensAsync(Node_Id LocalNodeId, Node_Id RemoteNodeId, CancellationToken CancellationToken = default)
                => ValueTask.FromResult<IReadOnlyList<PendingAccessToken>>([]);

            public ValueTask<PendingAccessToken?> FindPendingAccessTokenAsync(AccessToken Token, CancellationToken CancellationToken = default)
                => ValueTask.FromResult<PendingAccessToken?>(null);

            public ValueTask<Pairing?> ActivateAccessTokenAsync(Node_Id LocalNodeId, Node_Id RemoteNodeId, AccessToken Token, CancellationToken CancellationToken = default)
                => ValueTask.FromException<Pairing?>(new IOException("The disk is full!"));

            public ValueTask<Int32> RemovePendingAccessTokensAsync(Node_Id? LocalNodeId = null, Node_Id? RemoteNodeId = null, DateTimeOffset? CreatedBefore = null, CancellationToken CancellationToken = default)
                => ValueTask.FromResult(0);

            public ValueTask<IReadOnlyList<AccessToken>> GetAccessTokenCandidatesAsync(Node_Id LocalNodeId, Node_Id RemoteNodeId, CancellationToken CancellationToken = default)
                => ValueTask.FromResult<IReadOnlyList<AccessToken>>([]);

            public ValueTask<Pairing?> UnpairAsync(Node_Id LocalNodeId, Node_Id RemoteNodeId, DateTimeOffset At, CancellationToken CancellationToken = default)
                => ValueTask.FromException<Pairing?>(new IOException("The disk is full!"));

            public ValueTask<DateTimeOffset?> GetUnpairedAtAsync(Node_Id LocalNodeId, Node_Id RemoteNodeId, CancellationToken CancellationToken = default)
                => ValueTask.FromResult<DateTimeOffset?>(null);

        }

        #endregion

        #region (private) Helpers

        private static IReadOnlyList<PairingAttempt> CompletedAttemptsOf(PairingServerFixture Fixture)
        {
            lock (Fixture.CompletedAttempts)
            {
                return [.. Fixture.CompletedAttempts];
            }
        }

        private static Int32 CompletedPairingsCountOf(PairingServerFixture Fixture)
        {
            lock (Fixture.CompletedPairings)
            {
                return Fixture.CompletedPairings.Count;
            }
        }

        /// <summary>
        /// Poll the given condition for at most five seconds.
        /// </summary>
        private static async Task WaitUntilAsync(Func<Boolean>  Condition,
                                                 String         Description)
        {

            var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);

            while (!Condition())
            {

                if (DateTimeOffset.UtcNow > deadline)
                    Assert.Fail($"Timed out waiting for {Description}!");

                await Task.Delay(20);

            }

        }

        /// <summary>
        /// The remembered pairing attempt with the given pairingAttemptId (bearer token).
        /// </summary>
        private static PairingAttempt FindAttempt(PairingServerFixture  Fixture,
                                                  String                Bearer)
        {

            var attempt = Fixture.API.Attempts.SingleOrDefault(remembered => remembered.Id.Value == Bearer);

            Assert.That(attempt, Is.Not.Null, "The pairing attempt is not remembered by the pairing server!");

            return attempt!;

        }

        /// <summary>
        /// Step 1 to 3 for the given server node and client, expecting 200.
        /// </summary>
        private static async Task<StartedAttempt> StartAttemptAsync(PairingServerFixture  Fixture,
                                                                    HostedNode            ServerNode,
                                                                    TestPairingClient     Client)
        {

            var result = await Fixture.PostAsync("v1/requestPairing", Client.RequestPairing().ToJSON());

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK), result.Body);
            Assert.That(RequestPairingResponse.TryParse(result.Object, out var response, out var error), Is.True, error);

            var bearer   = response!.PairingAttemptId.Value;
            var attempt  = FindAttempt(Fixture, bearer);

            Assert.Multiple(() => {
                Assert.That(attempt.ServerNode,  Is.SameAs(ServerNode));
                Assert.That(attempt.State,       Is.EqualTo(PairingAttemptState.AwaitingConnectionDetails));
            });

            return new StartedAttempt(Fixture, ServerNode, Client, bearer, response, attempt);

        }

        /// <summary>
        /// Branch A on a LAN fixture: a CEM server node paired by an RM client, the server
        /// node becomes the communication server (requestConnectionDetails is expected).
        /// </summary>
        private static async Task<StartedAttempt> StartBranchAAsync(PairingServerFixture Fixture)
        {

            var client = new TestPairingClient(EnergyManagementRole.RM);
            var cem    = Fixture.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(client.Token);

            var started = await StartAttemptAsync(Fixture, cem, client);

            Assert.That(started.Attempt.ServerCommunicationRole, Is.EqualTo(CommunicationRole.CommunicationServer));

            return started;

        }

        /// <summary>
        /// Branch B on a LAN fixture: an RM server node paired by a CEM client, the server
        /// node becomes the communication client (postConnectionDetails is expected).
        /// </summary>
        private static async Task<StartedAttempt> StartBranchBAsync(PairingServerFixture Fixture)
        {

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = Fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var started = await StartAttemptAsync(Fixture, rm, client);

            Assert.That(started.Attempt.ServerCommunicationRole, Is.EqualTo(CommunicationRole.CommunicationClient));

            return started;

        }

        /// <summary>
        /// A requestPairing request whose client endpoint description has no deployment.
        /// </summary>
        private static RequestPairingRequest RequestWithoutDeployment(TestPairingClient Client)

            => new (Client.Node,
                    new EndpointDescription("Client endpoint without deployment"),
                    [ CommunicationProtocol.WebSocket ],
                    [ Version.S2JSONVersion ],
                    [ HmacHashingAlgorithm.SHA256 ],
                    Client.Challenge);

        private static void AssertBadRequest(HTTPResult  Result,
                                             String      ExpectedErrorMessage)

            => Assert.Multiple(() => {
                   Assert.That(Result.Status,        Is.EqualTo(HttpStatusCode.BadRequest), Result.Body);
                   Assert.That(Result.ErrorMessage,  Is.EqualTo(ExpectedErrorMessage),      Result.Body);
                   Assert.That(Result.ContentType,   Does.StartWith("application/json"));
                   Assert.That(Result.CacheControl,  Does.Contain("no-store"));
               });

        private static void AssertUnauthorized(HTTPResult Result)

            => Assert.Multiple(() => {
                   Assert.That(Result.Status,           Is.EqualTo(HttpStatusCode.Unauthorized), Result.Body);
                   Assert.That(Result.WWWAuthenticate,  Does.Contain("Bearer"));
                   Assert.That(Result.JSON,             Is.Null, "A 401 response must not carry a body!");
               });

        private static async Task AssertFailedAsync(PairingServerFixture  Fixture,
                                                    PairingAttempt        Attempt,
                                                    PairingFailure        ExpectedFailure)
        {

            await WaitUntilAsync(() => CompletedAttemptsOf(Fixture).Contains(Attempt), "the OnPairingAttemptCompleted event");

            Assert.Multiple(() => {
                Assert.That(Attempt.State,               Is.EqualTo(PairingAttemptState.Failed));
                Assert.That(Attempt.Failure,             Is.EqualTo(ExpectedFailure));
                Assert.That(Attempt.FailureDescription,  Is.Not.Null.And.Not.Empty);
                Assert.That(Attempt.IsActive,            Is.False);
                Assert.That(Attempt.CompletedAt,         Is.Not.Null);
                Assert.That(Attempt.Result,              Is.Null);
                Assert.That(Fixture.API.ActiveAttempts,  Does.Not.Contain(Attempt));
            });

        }

        #endregion


        // Node resolution (S2 Connect 1.0.0, requestPairing: nodeId, nodeIdAlias or the only node).

        #region RequestPairing_UnknownNodeId_Returns400NodeNotFound()

        [Test]
        [S2C("Pairing.1.NodeNotFound")]
        public async Task RequestPairing_UnknownNodeId_Returns400NodeNotFound()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var result = await fixture.PostAsync("v1/requestPairing", client.RequestPairing(PairingTarget.ByNodeId(Node_Id.NewRandom)).ToJSON());

            AssertBadRequest(result, "NodeNotFound");
            Assert.That(fixture.API.Attempts, Is.Empty);

        }

        #endregion

        #region RequestPairing_UnknownNodeIdAlias_Returns400NodeNotFound()

        [Test]
        [S2C("Pairing.1.NodeNotFound")]
        public async Task RequestPairing_UnknownNodeIdAlias_Returns400NodeNotFound()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM, NodeIdAlias.Parse("A0"));
            rm.SetStaticPairingToken(client.Token);

            var result = await fixture.PostAsync("v1/requestPairing", client.RequestPairing(PairingTarget.ByAlias(NodeIdAlias.Parse("ZZ"))).ToJSON());

            AssertBadRequest(result, "NodeNotFound");
            Assert.That(fixture.API.Attempts, Is.Empty);

        }

        #endregion

        #region RequestPairing_WithoutTargetOnMultiNodeEndpoint_Returns400NoNodeIdProvided()

        [Test]
        [S2C("Pairing.1.NoNodeIdProvided")]
        public async Task RequestPairing_WithoutTargetOnMultiNodeEndpoint_Returns400NoNodeIdProvided()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);

            // An endpoint without any node...
            var empty = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            AssertBadRequest(empty, "NoNodeIdProvided");

            // ...and an endpoint with two nodes cannot resolve "the only node".
            fixture.AddNode(EnergyManagementRole.RM).SetStaticPairingToken(client.Token);
            fixture.AddNode(EnergyManagementRole.RM).SetStaticPairingToken(client.Token);

            var two = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            AssertBadRequest(two, "NoNodeIdProvided");
            Assert.That(fixture.API.Attempts, Is.Empty);

        }

        #endregion


        // The checks of step 1 in the order of the specification.

        #region RequestPairing_NodeNotReadyForPairing_Returns400Other()

        [Test]
        [S2C("Pairing.1.Other")]
        public async Task RequestPairing_NodeNotReadyForPairing_Returns400Other()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);
            rm.IsReadyForPairing = false;

            var rejected = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            AssertBadRequest(rejected, "Other");
            Assert.That(rejected.AdditionalInfo, Does.Contain("not ready"));

            rm.IsReadyForPairing = true;

            var accepted = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            Assert.That(accepted.Status, Is.EqualTo(HttpStatusCode.OK), accepted.Body);

        }

        #endregion

        #region RequestPairing_WithItself_Returns400Other()

        [Test]
        [S2C("Pairing.1.Other")]
        public async Task RequestPairing_WithItself_Returns400Other()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var rm = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(PairingToken.Parse("ABCD2345"));

            var request = new RequestPairingRequest(
                              new NodeDescription(rm.Id, "ACME", "EMS", "ClientCEM", EnergyManagementRole.CEM),
                              new EndpointDescription("Self", null, Deployment.LAN),
                              [ CommunicationProtocol.WebSocket ],
                              [ Version.S2JSONVersion ],
                              [ HmacHashingAlgorithm.SHA256 ],
                              TokenGenerator.NewChallenge()
                          );

            var result = await fixture.PostAsync("v1/requestPairing", request.ToJSON());

            AssertBadRequest(result, "Other");
            Assert.That(result.AdditionalInfo, Does.Contain("itself"));

        }

        #endregion

        #region RequestPairing_SameRole_Returns400InvalidCombinationOfRoles()

        [Test]
        [S2C("Pairing.1.InvalidCombinationOfRoles")]
        public async Task RequestPairing_SameRole_Returns400InvalidCombinationOfRoles()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.RM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var result = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            AssertBadRequest(result, "InvalidCombinationOfRoles");
            Assert.That(fixture.API.Attempts, Is.Empty);

        }

        #endregion

        #region RequestPairing_WithoutSHA256_Returns400ParsingError()

        [Test]
        [S2C("Pairing.1.IncompatibleHmacHashingAlgorithms")]
        [S2C("Pairing.1.ParsingError")]
        public async Task RequestPairing_WithoutSHA256_Returns400ParsingError()
        {

            // S2 Connect 1.0 defines SHA256 as the only HMAC hashing algorithm and requires it in
            // every offer; a request without it is schema-invalid, so the server answers ParsingError
            // before the IncompatibleHmacHashingAlgorithms check (which is unreachable in 1.0).

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var json = client.RequestPairing().ToJSON();
            json["supportedHmacHashingAlgorithms"] = new JArray("SHA512");

            var result = await fixture.PostAsync("v1/requestPairing", json);

            AssertBadRequest(result, "ParsingError");
            Assert.That(fixture.API.Attempts, Is.Empty);

        }

        #endregion

        #region RequestPairing_IncompatibleCommunicationProtocols_Returns400()

        [Test]
        [S2C("Pairing.1.IncompatibleCommunicationProtocols")]
        public async Task RequestPairing_IncompatibleCommunicationProtocols_Returns400()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM, SupportedCommunicationProtocols: [ CommunicationProtocol.Parse("MQTT") ]);
            rm.SetStaticPairingToken(client.Token);

            var result = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            AssertBadRequest(result, "IncompatibleCommunicationProtocols");
            Assert.That(fixture.API.Attempts, Is.Empty);

        }

        #endregion

        #region RequestPairing_IncompatibleS2MessageVersions_Returns400()

        [Test]
        [S2C("Pairing.1.IncompatibleS2MessageVersions")]
        public async Task RequestPairing_IncompatibleS2MessageVersions_Returns400()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM, SupportedS2MessageVersions: [ "v2.0.0" ]);
            rm.SetStaticPairingToken(client.Token);

            var result = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            AssertBadRequest(result, "IncompatibleS2MessageVersions");
            Assert.That(fixture.API.Attempts, Is.Empty);

        }

        #endregion

        #region RequestPairing_WithoutPairingToken_Returns400NoValidPairingTokenOnPairingServer()

        [Test]
        [S2C("Pairing.1.NoValidPairingTokenOnPairingServer")]
        public async Task RequestPairing_WithoutPairingToken_Returns400NoValidPairingTokenOnPairingServer()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            fixture.AddNode(EnergyManagementRole.RM);

            var result = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            AssertBadRequest(result, "NoValidPairingTokenOnPairingServer");
            Assert.That(fixture.API.Attempts, Is.Empty);

        }

        #endregion

        #region RequestPairing_WithExpiredDynamicPairingToken_Returns400NoValidPairingTokenOnPairingServer()

        [Test]
        [S2C("Pairing.1.NoValidPairingTokenOnPairingServer")]
        [S2C("PairingToken.Dynamic")]
        public async Task RequestPairing_WithExpiredDynamicPairingToken_Returns400NoValidPairingTokenOnPairingServer()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetDynamicPairingToken(client.Token, TimeSpan.FromMilliseconds(50));

            await Task.Delay(TimeSpan.FromMilliseconds(400));

            Assert.That(rm.HasValidPairingToken, Is.False);

            var result = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            AssertBadRequest(result, "NoValidPairingTokenOnPairingServer");

        }

        #endregion

        #region RequestPairing_ChecksFollowTheOrderOfTheSpecification()

        [Test]
        [S2C("Pairing.1.CheckOrder")]
        public async Task RequestPairing_ChecksFollowTheOrderOfTheSpecification()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);

            // Readiness before roles: not ready AND the same role => Other
            var notReady = fixture.AddNode(EnergyManagementRole.CEM);
            notReady.IsReadyForPairing = false;

            // Roles before versions: the same role AND incompatible versions => InvalidCombinationOfRoles
            var sameRole = fixture.AddNode(EnergyManagementRole.CEM, SupportedS2MessageVersions: [ "v2.0.0" ]);

            // Protocols before versions: both incompatible => IncompatibleCommunicationProtocols
            var protocols = fixture.AddNode(EnergyManagementRole.RM, SupportedS2MessageVersions: [ "v2.0.0" ], SupportedCommunicationProtocols: [ CommunicationProtocol.Parse("MQTT") ]);
            protocols.SetStaticPairingToken(client.Token);

            // Versions before the token: incompatible versions AND no token => IncompatibleS2MessageVersions
            var versions = fixture.AddNode(EnergyManagementRole.RM, SupportedS2MessageVersions: [ "v2.0.0" ]);

            var notReadyResult   = await fixture.PostAsync("v1/requestPairing", client.RequestPairing(PairingTarget.ByNodeId(notReady. Id)).ToJSON());
            var sameRoleResult   = await fixture.PostAsync("v1/requestPairing", client.RequestPairing(PairingTarget.ByNodeId(sameRole. Id)).ToJSON());
            var protocolsResult  = await fixture.PostAsync("v1/requestPairing", client.RequestPairing(PairingTarget.ByNodeId(protocols.Id)).ToJSON());
            var versionsResult   = await fixture.PostAsync("v1/requestPairing", client.RequestPairing(PairingTarget.ByNodeId(versions. Id)).ToJSON());

            AssertBadRequest(notReadyResult,   "Other");
            AssertBadRequest(sameRoleResult,   "InvalidCombinationOfRoles");
            AssertBadRequest(protocolsResult,  "IncompatibleCommunicationProtocols");
            AssertBadRequest(versionsResult,   "IncompatibleS2MessageVersions");

            Assert.That(fixture.API.Attempts, Is.Empty);

        }

        #endregion


        // The deployment of the client (decides the communication roles).

        #region RequestPairing_WithoutClientDeployment_Returns400Other()

        [Test]
        [S2C("Pairing.1.Other")]
        [S2C("CommunicationRoles.ClientDeployment")]
        public async Task RequestPairing_WithoutClientDeployment_Returns400Other()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var result = await fixture.PostAsync("v1/requestPairing", RequestWithoutDeployment(client).ToJSON());

            AssertBadRequest(result, "Other");
            Assert.That(result.AdditionalInfo,  Does.Contain("deployment"));
            Assert.That(fixture.API.Attempts,   Is.Empty);

        }

        #endregion

        #region RequestPairing_WithoutClientDeployment_UsesTheDefaultClientDeployment()

        [Test]
        [S2C("CommunicationRoles.ClientDeployment")]
        public async Task RequestPairing_WithoutClientDeployment_UsesTheDefaultClientDeployment()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                          Options: PairingServerFixture.DefaultOptions() with { DefaultClientDeployment = Deployment.LAN }
                                      );

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var result = await fixture.PostAsync("v1/requestPairing", RequestWithoutDeployment(client).ToJSON());

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK), result.Body);
            Assert.That(RequestPairingResponse.TryParse(result.Object, out var response, out var error), Is.True, error);

            var bearer   = response!.PairingAttemptId.Value;
            var attempt  = FindAttempt(fixture, bearer);

            Assert.Multiple(() => {
                Assert.That(attempt.ClientDeployment,                          Is.EqualTo(Deployment.LAN));
                Assert.That(attempt.ClientEndpointDescription.Deployment,      Is.Null);
                Assert.That(attempt.ServerCommunicationRole,                   Is.EqualTo(CommunicationRole.CommunicationClient));
            });

            // Branch B (RM LAN with CEM LAN): the pairing completes and the stored endpoint description carries the default.
            var details    = client.ConnectionDetails();
            var posted     = await fixture.PostAsync("v1/postConnectionDetails", new PostConnectionDetailsRequest(client.ServerChallengeResponse(fixture, response.ServerHmacChallenge), details).ToJSON(), bearer);
            var finalized  = await fixture.PostAsync("v1/finalizePairing",       new FinalizePairingRequest(true).ToJSON(), bearer);
            var pairing    = await fixture.Store.GetPairingAsync(rm.Id, client.Node.Id);

            Assert.Multiple(() => {
                Assert.That(posted.Status,                                     Is.EqualTo(HttpStatusCode.NoContent), posted.Body);
                Assert.That(finalized.Status,                                  Is.EqualTo(HttpStatusCode.NoContent), finalized.Body);
                Assert.That(pairing,                                           Is.Not.Null);
                Assert.That(pairing!.RemoteEndpointDescription.Deployment,     Is.EqualTo(Deployment.LAN));
                Assert.That(pairing.RemoteEndpointDescription.Name,            Is.EqualTo("Client endpoint without deployment"));
                Assert.That(pairing.LocalCommunicationRole,                    Is.EqualTo(CommunicationRole.CommunicationClient));
            });

        }

        #endregion

        #region RequestPairing_WANPairingServerForLANEndpoint_RejectsLANClients()

        [Test]
        [S2C("Pairing.Deployments.WANPairingServerForLANEndpoint")]
        public async Task RequestPairing_WANPairingServerForLANEndpoint_RejectsLANClients()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.LAN, IsWANPairingServerForLANEndpoint: true);

            var client = new TestPairingClient(EnergyManagementRole.CEM, Deployment.LAN);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var result = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            AssertBadRequest(result, "Other");

            Assert.Multiple(() => {
                Assert.That(result.AdditionalInfo,                    Does.Contain("WAN"));
                Assert.That(fixture.API.UsesLANChallengeResponse,     Is.False);
                Assert.That(fixture.API.LANOperationsEnabled,         Is.False);
                Assert.That(fixture.API.Attempts,                     Is.Empty);
            });

        }

        #endregion

        #region RequestPairing_WANPairingServerForLANEndpoint_AcceptsWANClientsWithTheWANFormula()

        [Test]
        [S2C("Pairing.Deployments.WANPairingServerForLANEndpoint")]
        [S2C("ChallengeResponse.WAN")]
        public async Task RequestPairing_WANPairingServerForLANEndpoint_AcceptsWANClientsWithTheWANFormula()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.LAN, IsWANPairingServerForLANEndpoint: true);

            var client = new TestPairingClient(EnergyManagementRole.CEM, Deployment.WAN);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var started = await StartAttemptAsync(fixture, rm, client);

            // R = HMAC(C, T || D) with the domain name of the pairing URL...
            var expected = ChallengeResponse.ComputeForWAN(HmacHashingAlgorithm.SHA256, client.Challenge, client.Token, fixture.Endpoint.DomainName);

            Assert.Multiple(() => {
                Assert.That(started.Response.ClientHmacChallengeResponse.ConstantTimeEquals(expected), Is.True);
                Assert.That(started.Response.ServerEndpointDescription.Deployment,  Is.EqualTo(Deployment.LAN));
                Assert.That(started.Attempt.ClientDeployment,                       Is.EqualTo(Deployment.WAN));
                // ...and the WAN node is the communication server: branch B.
                Assert.That(started.Attempt.ServerCommunicationRole,                Is.EqualTo(CommunicationRole.CommunicationClient));
            });

            var details    = client.ConnectionDetails();
            var posted     = await fixture.PostAsync("v1/postConnectionDetails", new PostConnectionDetailsRequest(started.CorrectServerResponse, details).ToJSON(), started.Bearer);
            var finalized  = await fixture.PostAsync("v1/finalizePairing",       new FinalizePairingRequest(true).ToJSON(), started.Bearer);
            var pairing    = await fixture.Store.GetPairingAsync(rm.Id, client.Node.Id);
            var nodes      = await fixture.GetAsync("v1/nodes");

            Assert.Multiple(() => {
                Assert.That(posted.Status,      Is.EqualTo(HttpStatusCode.NoContent), posted.Body);
                Assert.That(finalized.Status,   Is.EqualTo(HttpStatusCode.NoContent), finalized.Body);
                Assert.That(pairing,            Is.Not.Null);
                Assert.That(pairing!.InitiateSessionUrl, Is.EqualTo(details.InitiateSessionUrl));
                Assert.That(nodes.Status,       Is.EqualTo(HttpStatusCode.NotFound), "A WAN pairing server of a LAN endpoint does not serve the LAN-only operations!");
            });

        }

        #endregion

        #region RequestPairing_LANEndpointWithoutCertificateFingerprint_Returns503()

        [Test]
        [S2C("ChallengeResponse.LAN")]
        [S2C("Pairing.2")]
        public async Task RequestPairing_LANEndpointWithoutCertificateFingerprint_Returns503()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(WithFingerprint: false);

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var result = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            Assert.Multiple(() => {
                Assert.That(result.Status,                            Is.EqualTo(HttpStatusCode.ServiceUnavailable), result.Body);
                Assert.That(fixture.Endpoint.ServerCertificateFingerprint, Is.Null);
                Assert.That(fixture.API.Attempts,                     Is.Empty);
            });

        }

        #endregion


        // The pairingAttemptId as bearer token (S2 Connect 1.0.0, "3. Response status 200": the secret
        // identification of the attempt; missing, unknown, expired or used up => 401).

        #region (private) AssertAllOperationsUnauthorizedAsync(Fixture, Bearer)

        private static HmacChallengeResponse AnyChallengeResponse()
            => ChallengeResponse.ComputeForWAN(HmacHashingAlgorithm.SHA256, TokenGenerator.NewChallenge(), PairingToken.Parse("ABCD2345"), "pairing.example.com");

        private static async Task AssertAllOperationsUnauthorizedAsync(PairingServerFixture  Fixture,
                                                                        String?               Bearer)
        {

            var details    = new TestPairingClient(EnergyManagementRole.CEM).ConnectionDetails();

            var requested  = await Fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(AnyChallengeResponse()).ToJSON(),        Bearer);
            var posted     = await Fixture.PostAsync("v1/postConnectionDetails",    new PostConnectionDetailsRequest(AnyChallengeResponse(), details).ToJSON(),  Bearer);
            var finalized  = await Fixture.PostAsync("v1/finalizePairing",          new FinalizePairingRequest(true).ToJSON(),                                   Bearer);

            AssertUnauthorized(requested);
            AssertUnauthorized(posted);
            AssertUnauthorized(finalized);

        }

        #endregion

        #region BearerAuthentication_MissingBearer_Returns401()

        [Test]
        [S2C("Pairing.PairingAttemptId")]
        public async Task BearerAuthentication_MissingBearer_Returns401()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var started = await StartBranchAAsync(fixture);

            await AssertAllOperationsUnauthorizedAsync(fixture, null);

            // Unauthenticated requests do not touch the attempts of others.
            Assert.That(started.Attempt.State, Is.EqualTo(PairingAttemptState.AwaitingConnectionDetails));

        }

        #endregion

        #region BearerAuthentication_ShortBearer_Returns401()

        [Test]
        [S2C("Pairing.PairingAttemptId")]
        public async Task BearerAuthentication_ShortBearer_Returns401()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var started = await StartBranchAAsync(fixture);

            await AssertAllOperationsUnauthorizedAsync(fixture, "short");
            await AssertAllOperationsUnauthorizedAsync(fixture, started.Bearer[..31]);

            Assert.That(started.Attempt.State, Is.EqualTo(PairingAttemptState.AwaitingConnectionDetails));

        }

        #endregion

        #region BearerAuthentication_UnknownBearer_Returns401()

        [Test]
        [S2C("Pairing.PairingAttemptId")]
        public async Task BearerAuthentication_UnknownBearer_Returns401()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var started = await StartBranchAAsync(fixture);
            var unknown = TokenGenerator.NewPairingAttemptId().Value;

            Assert.That(unknown, Is.Not.EqualTo(started.Bearer));

            await AssertAllOperationsUnauthorizedAsync(fixture, unknown);

            Assert.That(started.Attempt.State, Is.EqualTo(PairingAttemptState.AwaitingConnectionDetails));

        }

        #endregion

        #region AfterForbidden_EveryRequestWithTheBearer_Returns401()

        [Test]
        [S2C("Pairing.PairingAttemptId")]
        [S2C("Pairing.7A")]
        public async Task AfterForbidden_EveryRequestWithTheBearer_Returns401()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var started    = await StartBranchAAsync(fixture);

            var forbidden  = await fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(started.WrongServerResponse).ToJSON(), started.Bearer);

            Assert.That(forbidden.Status, Is.EqualTo(HttpStatusCode.Forbidden), forbidden.Body);

            await AssertFailedAsync(fixture, started.Attempt, PairingFailure.InvalidChallengeResponse);

            // Even the correct response is too late now: the attempt is used up.
            var requested  = await fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(started.CorrectServerResponse).ToJSON(), started.Bearer);
            var finalized  = await fixture.PostAsync("v1/finalizePairing",          new FinalizePairingRequest(true).ToJSON(),                                  started.Bearer);

            AssertUnauthorized(requested);
            AssertUnauthorized(finalized);

            Assert.That(await fixture.Store.GetPairingAsync(started.ServerNode.Id, started.Client.Node.Id), Is.Null);

        }

        #endregion

        #region AfterTimeout_EveryRequestWithTheBearer_Returns401_AndTheAttemptFailsWithTimeout()

        [Test]
        [S2C("Pairing.Interruption")]
        [S2C("Pairing.PairingAttemptId")]
        public async Task AfterTimeout_EveryRequestWithTheBearer_Returns401_AndTheAttemptFailsWithTimeout()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                          Options: PairingServerFixture.DefaultOptions() with { PairingAttemptTimeout = TimeSpan.FromMilliseconds(300) }
                                      );

            var started = await StartBranchAAsync(fixture);

            Assert.That(started.Attempt.ExpiresAt - started.Attempt.CreatedAt, Is.EqualTo(TimeSpan.FromMilliseconds(300)));

            await Task.Delay(TimeSpan.FromMilliseconds(900));

            Assert.That(started.Attempt.HasExpired(DateTimeOffset.UtcNow), Is.True);

            var requested  = await fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(started.CorrectServerResponse).ToJSON(), started.Bearer);
            var finalized  = await fixture.PostAsync("v1/finalizePairing",          new FinalizePairingRequest(true).ToJSON(),                                  started.Bearer);

            AssertUnauthorized(requested);
            AssertUnauthorized(finalized);

            // The expired attempt is failed and reported when the server next purges its
            // attempts, which happens with the next requestPairing of any client.
            var other  = new TestPairingClient(EnergyManagementRole.RM);
            var next   = await fixture.PostAsync("v1/requestPairing", other.RequestPairing().ToJSON());

            Assert.That(next.Status, Is.EqualTo(HttpStatusCode.OK), next.Body);

            await AssertFailedAsync(fixture, started.Attempt, PairingFailure.Timeout);

            Assert.That(await fixture.Store.GetPairingAsync(started.ServerNode.Id, started.Client.Node.Id), Is.Null);

        }

        #endregion

        #region AfterSuccess_FurtherRequestsWithTheBearer_Return401()

        [Test]
        [S2C("Pairing.PairingAttemptId")]
        [S2C("Pairing.10")]
        public async Task AfterSuccess_FurtherRequestsWithTheBearer_Return401()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var started    = await StartBranchBAsync(fixture);
            var details    = started.Client.ConnectionDetails();

            var posted     = await fixture.PostAsync("v1/postConnectionDetails", new PostConnectionDetailsRequest(started.CorrectServerResponse, details).ToJSON(), started.Bearer);
            var finalized  = await fixture.PostAsync("v1/finalizePairing",       new FinalizePairingRequest(true).ToJSON(),                                        started.Bearer);

            Assert.Multiple(() => {
                Assert.That(posted.Status,           Is.EqualTo(HttpStatusCode.NoContent), posted.Body);
                Assert.That(finalized.Status,        Is.EqualTo(HttpStatusCode.NoContent), finalized.Body);
                Assert.That(started.Attempt.State,   Is.EqualTo(PairingAttemptState.Succeeded));
            });

            // Anything but an identical replay of the last request is rejected: the attempt is used up.
            var requested   = await fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(started.CorrectServerResponse).ToJSON(),                              started.Bearer);
            var reposted    = await fixture.PostAsync("v1/postConnectionDetails",    new PostConnectionDetailsRequest(started.CorrectServerResponse, started.Client.ConnectionDetails()).ToJSON(), started.Bearer);
            var refinalized = await fixture.PostAsync("v1/finalizePairing",          new FinalizePairingRequest(false).ToJSON(),                                                              started.Bearer);

            AssertUnauthorized(requested);
            AssertUnauthorized(reposted);
            AssertUnauthorized(refinalized);

            Assert.Multiple(() => {
                Assert.That(started.Attempt.State,          Is.EqualTo(PairingAttemptState.Succeeded));
                Assert.That(CompletedPairingsCountOf(fixture), Is.EqualTo(1));
            });

        }

        #endregion


        // Step 7A/7B: the serverHmacChallengeResponse of the client is verified.

        #region RequestConnectionDetails_WrongServerHmacChallengeResponse_Returns403()

        [Test]
        [S2C("Pairing.7A")]
        public async Task RequestConnectionDetails_WrongServerHmacChallengeResponse_Returns403()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var started = await StartBranchAAsync(fixture);
            var result  = await fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(started.WrongServerResponse).ToJSON(), started.Bearer);

            Assert.Multiple(() => {
                Assert.That(result.Status,  Is.EqualTo(HttpStatusCode.Forbidden), result.Body);
                Assert.That(result.JSON,    Is.Null, "A 403 response must not carry connection details!");
            });

            await AssertFailedAsync(fixture, started.Attempt, PairingFailure.InvalidChallengeResponse);

            Assert.Multiple(() => {
                Assert.That(started.ServerNode.HasValidPairingToken, Is.True, "A failed attempt must not consume the pairing token!");
                Assert.That(CompletedPairingsCountOf(fixture),       Is.EqualTo(0));
            });

            Assert.That(await fixture.Store.GetPairingAsync(started.ServerNode.Id, started.Client.Node.Id), Is.Null);

        }

        #endregion

        #region PostConnectionDetails_WrongServerHmacChallengeResponse_Returns403()

        [Test]
        [S2C("Pairing.7B")]
        public async Task PostConnectionDetails_WrongServerHmacChallengeResponse_Returns403()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var started = await StartBranchBAsync(fixture);
            var result  = await fixture.PostAsync("v1/postConnectionDetails", new PostConnectionDetailsRequest(started.WrongServerResponse, started.Client.ConnectionDetails()).ToJSON(), started.Bearer);

            Assert.Multiple(() => {
                Assert.That(result.Status,  Is.EqualTo(HttpStatusCode.Forbidden), result.Body);
                Assert.That(result.JSON,    Is.Null);
            });

            await AssertFailedAsync(fixture, started.Attempt, PairingFailure.InvalidChallengeResponse);

            Assert.That(await fixture.Store.GetPairingAsync(started.ServerNode.Id, started.Client.Node.Id), Is.Null);

        }

        #endregion


        // Invalid interactions (S2 Connect 1.0.0, "Invalid interactions"): the wrong branch or the wrong order.

        #region PostConnectionDetails_OnBranchA_Returns400Other_AndFailsTheAttempt()

        [Test]
        [S2C("Pairing.InvalidInteractions")]
        [S2C("Pairing.6B")]
        public async Task PostConnectionDetails_OnBranchA_Returns400Other_AndFailsTheAttempt()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            // The server node becomes the communication server, but the client posts connection details.
            var started = await StartBranchAAsync(fixture);
            var result  = await fixture.PostAsync("v1/postConnectionDetails", new PostConnectionDetailsRequest(started.CorrectServerResponse, started.Client.ConnectionDetails()).ToJSON(), started.Bearer);

            AssertBadRequest(result, "Other");
            Assert.That(result.AdditionalInfo, Does.Contain("requestConnectionDetails"));

            await AssertFailedAsync(fixture, started.Attempt, PairingFailure.InvalidInteraction);

            var requested = await fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(started.CorrectServerResponse).ToJSON(), started.Bearer);

            AssertUnauthorized(requested);

        }

        #endregion

        #region RequestConnectionDetails_OnBranchB_Returns400Other_AndFailsTheAttempt()

        [Test]
        [S2C("Pairing.InvalidInteractions")]
        [S2C("Pairing.6A")]
        public async Task RequestConnectionDetails_OnBranchB_Returns400Other_AndFailsTheAttempt()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            // The server node becomes the communication client, but the client requests connection details.
            var started = await StartBranchBAsync(fixture);
            var result  = await fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(started.CorrectServerResponse).ToJSON(), started.Bearer);

            AssertBadRequest(result, "Other");
            Assert.That(result.AdditionalInfo, Does.Contain("postConnectionDetails"));

            await AssertFailedAsync(fixture, started.Attempt, PairingFailure.InvalidInteraction);

            var posted = await fixture.PostAsync("v1/postConnectionDetails", new PostConnectionDetailsRequest(started.CorrectServerResponse, started.Client.ConnectionDetails()).ToJSON(), started.Bearer);

            AssertUnauthorized(posted);

        }

        #endregion

        #region FinalizePairing_BeforeConnectionDetails_Returns400Other_AndFailsTheAttempt()

        [Test]
        [S2C("Pairing.InvalidInteractions")]
        [S2C("Pairing.9")]
        public async Task FinalizePairing_BeforeConnectionDetails_Returns400Other_AndFailsTheAttempt()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var started = await StartBranchAAsync(fixture);
            var result  = await fixture.PostAsync("v1/finalizePairing", new FinalizePairingRequest(true).ToJSON(), started.Bearer);

            AssertBadRequest(result, "Other");

            await AssertFailedAsync(fixture, started.Attempt, PairingFailure.InvalidInteraction);

            Assert.Multiple(() => {
                Assert.That(CompletedPairingsCountOf(fixture),          Is.EqualTo(0));
                Assert.That(started.ServerNode.HasValidPairingToken,    Is.True);
            });

            Assert.That(await fixture.Store.GetPairingAsync(started.ServerNode.Id, started.Client.Node.Id), Is.Null);

        }

        #endregion

        #region RequestConnectionDetails_SecondDifferentRequest_Returns400Other_AndFailsTheAttempt()

        [Test]
        [S2C("Pairing.InvalidInteractions")]
        [S2C("Pairing.ReplayDuplicate")]
        public async Task RequestConnectionDetails_SecondDifferentRequest_Returns400Other_AndFailsTheAttempt()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var started  = await StartBranchAAsync(fixture);
            var first    = await fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(started.CorrectServerResponse).ToJSON(), started.Bearer);

            Assert.Multiple(() => {
                Assert.That(first.Status,            Is.EqualTo(HttpStatusCode.OK), first.Body);
                Assert.That(started.Attempt.State,   Is.EqualTo(PairingAttemptState.AwaitingFinalization));
            });

            // Only an identical request is replayed; a different one after the exchange is an invalid interaction.
            var second = await fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(started.WrongServerResponse).ToJSON(), started.Bearer);

            AssertBadRequest(second, "Other");
            Assert.That(second.AdditionalInfo, Does.Contain("already processed"));

            await AssertFailedAsync(fixture, started.Attempt, PairingFailure.InvalidInteraction);

            var finalized = await fixture.PostAsync("v1/finalizePairing", new FinalizePairingRequest(true).ToJSON(), started.Bearer);

            AssertUnauthorized(finalized);

            Assert.That(await fixture.Store.GetPairingAsync(started.ServerNode.Id, started.Client.Node.Id), Is.Null);

        }

        #endregion


        // Step 9: finalizePairing without success.

        #region FinalizePairing_WithSuccessFalse_Returns204_AndFailsTheAttempt()

        [Test]
        [S2C("Pairing.9")]
        [S2C("Pairing.10")]
        public async Task FinalizePairing_WithSuccessFalse_Returns204_AndFailsTheAttempt()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetDynamicPairingToken(client.Token);

            var started    = await StartAttemptAsync(fixture, rm, client);
            var posted     = await fixture.PostAsync("v1/postConnectionDetails", new PostConnectionDetailsRequest(started.CorrectServerResponse, client.ConnectionDetails()).ToJSON(), started.Bearer);
            var finalized  = await fixture.PostAsync("v1/finalizePairing",       new FinalizePairingRequest(false).ToJSON(),                                                       started.Bearer);

            Assert.Multiple(() => {
                Assert.That(posted.Status,     Is.EqualTo(HttpStatusCode.NoContent), posted.Body);
                Assert.That(finalized.Status,  Is.EqualTo(HttpStatusCode.NoContent), finalized.Body);
                Assert.That(finalized.Body,    Is.Empty);
            });

            await AssertFailedAsync(fixture, started.Attempt, PairingFailure.ClientReportedFailure);

            Assert.Multiple(() => {
                Assert.That(CompletedPairingsCountOf(fixture),  Is.EqualTo(0));
                Assert.That(rm.HasValidPairingToken,            Is.True, "A pairing token is only consumed by a successful pairing!");
            });

            Assert.That(await fixture.Store.GetPairingAsync(rm.Id, client.Node.Id), Is.Null);

        }

        #endregion

        #region FinalizePairing_WithoutSuccessFlag_Returns400ParsingError_AndKeepsTheAttemptActive()

        [Test]
        [S2C("Pairing.9")]
        public async Task FinalizePairing_WithoutSuccessFlag_Returns400ParsingError_AndKeepsTheAttemptActive()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var started  = await StartBranchBAsync(fixture);
            var posted   = await fixture.PostAsync("v1/postConnectionDetails", new PostConnectionDetailsRequest(started.CorrectServerResponse, started.Client.ConnectionDetails()).ToJSON(), started.Bearer);

            Assert.That(posted.Status, Is.EqualTo(HttpStatusCode.NoContent), posted.Body);

            // "success" is not required by the schema, but a pairing must never complete on an absent confirmation.
            var missing = await fixture.PostAsync("v1/finalizePairing", new JObject(), started.Bearer);

            AssertBadRequest(missing, "ParsingError");

            Assert.Multiple(() => {
                Assert.That(started.Attempt.State,     Is.EqualTo(PairingAttemptState.AwaitingFinalization));
                Assert.That(started.Attempt.IsActive,  Is.True);
            });

            var finalized = await fixture.PostAsync("v1/finalizePairing", new FinalizePairingRequest(true).ToJSON(), started.Bearer);

            Assert.Multiple(() => {
                Assert.That(finalized.Status,        Is.EqualTo(HttpStatusCode.NoContent), finalized.Body);
                Assert.That(started.Attempt.State,   Is.EqualTo(PairingAttemptState.Succeeded));
            });

        }

        #endregion


        // Request bodies.

        #region WrongContentType_Returns400ParsingError()

        [Test]
        [S2C("Pairing.1.ParsingError")]
        public async Task WrongContentType_Returns400ParsingError()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client  = new TestPairingClient(EnergyManagementRole.CEM);
            var rm      = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var text = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON(), ContentType: "text/plain");

            AssertBadRequest(text, "ParsingError");

            Assert.Multiple(() => {
                Assert.That(text.AdditionalInfo,    Does.Contain("text/plain"));
                Assert.That(fixture.API.Attempts,   Is.Empty);
            });

            // The content type is checked before the protocol logic: an active attempt is not affected.
            var started  = await StartAttemptAsync(fixture, rm, client);
            var posted   = await fixture.PostAsync("v1/postConnectionDetails", new PostConnectionDetailsRequest(started.CorrectServerResponse, client.ConnectionDetails()).ToJSON(), started.Bearer);
            var xml      = await fixture.PostAsync("v1/finalizePairing",       new FinalizePairingRequest(true).ToJSON(), started.Bearer, "application/xml");

            Assert.That(posted.Status, Is.EqualTo(HttpStatusCode.NoContent), posted.Body);

            AssertBadRequest(xml, "ParsingError");

            Assert.That(started.Attempt.State, Is.EqualTo(PairingAttemptState.AwaitingFinalization));

        }

        #endregion


        // Step 10: the pairing must be persisted before it is confirmed.

        #region FinalizePairing_StoreFailure_Returns500_AndFailsTheAttempt()

        [Test]
        [S2C("Pairing.10")]
        [S2C("Store.Durability")]
        public async Task FinalizePairing_StoreFailure_Returns500_AndFailsTheAttempt()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Store: new ThrowingStore());

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetDynamicPairingToken(client.Token);

            var started    = await StartAttemptAsync(fixture, rm, client);
            var posted     = await fixture.PostAsync("v1/postConnectionDetails", new PostConnectionDetailsRequest(started.CorrectServerResponse, client.ConnectionDetails()).ToJSON(), started.Bearer);
            var finalized  = await fixture.PostAsync("v1/finalizePairing",       new FinalizePairingRequest(true).ToJSON(),                                                        started.Bearer);

            Assert.Multiple(() => {
                Assert.That(posted.Status,     Is.EqualTo(HttpStatusCode.NoContent), posted.Body);
                Assert.That(finalized.Status,  Is.EqualTo(HttpStatusCode.InternalServerError), finalized.Body);
                Assert.That(finalized.JSON,    Is.Null);
            });

            await AssertFailedAsync(fixture, started.Attempt, PairingFailure.StoreFailure);

            Assert.Multiple(() => {
                Assert.That(started.Attempt.FailureDescription,  Does.Contain("disk is full"));
                Assert.That(CompletedPairingsCountOf(fixture),   Is.EqualTo(0));
                Assert.That(rm.HasValidPairingToken,             Is.True, "A pairing token is only consumed by a stored pairing!");
            });

            var again = await fixture.PostAsync("v1/finalizePairing", new FinalizePairingRequest(true).ToJSON(), started.Bearer);

            AssertUnauthorized(again);

        }

        #endregion


        // Step 2: sequential processing per node (brute-force protection).

        #region RequestPairing_TooManyQueuedAttemptsForANode_Returns503WithRetryAfter()

        [Test]
        [S2C("Pairing.2.SequentialProcessing")]
        public async Task RequestPairing_TooManyQueuedAttemptsForANode_Returns503WithRetryAfter()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                          Options: PairingServerFixture.DefaultOptions() with {
                                                       MaxQueuedPairingAttemptsPerNode  = 0,
                                                       RequestPairingDelay              = TimeSpan.FromMilliseconds(500)
                                                   }
                                      );

            var client1  = new TestPairingClient(EnergyManagementRole.CEM);
            var client2  = new TestPairingClient(EnergyManagementRole.CEM);
            var rm       = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client1.Token);

            var results  = await Task.WhenAll(
                               fixture.PostAsync("v1/requestPairing", client1.RequestPairing().ToJSON()),
                               fixture.PostAsync("v1/requestPairing", client2.RequestPairing().ToJSON())
                           );

            var accepted = results.Where(result => result.Status == HttpStatusCode.OK).                ToList();
            var rejected = results.Where(result => result.Status == HttpStatusCode.ServiceUnavailable).ToList();

            Assert.Multiple(() => {
                Assert.That(accepted,                    Has.Count.EqualTo(1), $"{results[0].Status} / {results[1].Status}");
                Assert.That(rejected,                    Has.Count.EqualTo(1), $"{results[0].Status} / {results[1].Status}");
                Assert.That(fixture.API.ActiveAttempts,  Has.Count.EqualTo(1));
            });

            Assert.Multiple(() => {
                Assert.That(rejected[0].RetryAfter,  Is.EqualTo("1"));
                Assert.That(rejected[0].JSON,        Is.Null);
            });

            // After the delay both clients are served: the accepted one is replayed, the rejected one retried.
            var retry1 = await fixture.PostAsync("v1/requestPairing", client1.RequestPairing().ToJSON());
            var retry2 = await fixture.PostAsync("v1/requestPairing", client2.RequestPairing().ToJSON());

            Assert.Multiple(() => {
                Assert.That(retry1.Status,               Is.EqualTo(HttpStatusCode.OK), retry1.Body);
                Assert.That(retry2.Status,               Is.EqualTo(HttpStatusCode.OK), retry2.Body);
                Assert.That(fixture.API.ActiveAttempts,  Has.Count.EqualTo(2));
            });

        }

        #endregion


        // Step 8A: the server must be able to provide connection details.

        #region RequestConnectionDetails_WithoutSessionInitiationUrl_Returns400Other_AndFailsTheAttempt()

        [Test]
        [S2C("Pairing.8A")]
        public async Task RequestConnectionDetails_WithoutSessionInitiationUrl_Returns400Other_AndFailsTheAttempt()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(WithSessionInitiationUrl: false);

            Assert.That(fixture.Endpoint.SessionInitiationUrl, Is.Null);

            var started = await StartBranchAAsync(fixture);
            var result  = await fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(started.CorrectServerResponse).ToJSON(), started.Bearer);

            AssertBadRequest(result, "Other");

            await AssertFailedAsync(fixture, started.Attempt, PairingFailure.ConnectionDetailsUnavailable);

            Assert.That(await fixture.Store.GetPairingAsync(started.ServerNode.Id, started.Client.Node.Id), Is.Null);

        }

        #endregion


        // The LAN-LAN only operations are not implemented by WAN endpoints.

        #region LANOnlyOperations_OnWANEndpoint_Return404()

        [Test]
        [S2C("LANOperations.WANEndpoint")]
        public async Task LANOnlyOperations_OnWANEndpoint_Return404()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.WAN);

            fixture.AddNode(EnergyManagementRole.CEM);

            var endpoint  = await fixture.GetAsync ("v1/endpoint");
            var nodes     = await fixture.GetAsync ("v1/nodes");
            var prepare   = await fixture.PostAsync("v1/preparePairing",        new JObject());
            var cancel    = await fixture.PostAsync("v1/cancelPreparePairing",  new JObject());
            var wait      = await fixture.PostAsync("v1/waitForPairing",        new JArray());

            Assert.Multiple(() => {
                Assert.That(fixture.API.LANOperationsEnabled,   Is.False);
                Assert.That(fixture.API.LongPollingEnabled,     Is.False);
                Assert.That(endpoint.Status,                    Is.EqualTo(HttpStatusCode.NotFound), endpoint.Body);
                Assert.That(nodes.Status,                       Is.EqualTo(HttpStatusCode.NotFound), nodes.Body);
                Assert.That(prepare.Status,                     Is.EqualTo(HttpStatusCode.NotFound), prepare.Body);
                Assert.That(cancel.Status,                      Is.EqualTo(HttpStatusCode.NotFound), cancel.Body);
                Assert.That(wait.Status,                        Is.EqualTo(HttpStatusCode.NotFound), wait.Body);
                Assert.That(fixture.PreparePairingRequests,     Is.Empty);
                Assert.That(fixture.CancelPreparePairingRequests, Is.Empty);
            });

        }

        #endregion


        // Interruption of the process by the server itself.

        #region Shutdown_FailsActiveAttempts_AndRejectsRequestPairingWith503()

        [Test]
        [S2C("Pairing.Interruption")]
        public async Task Shutdown_FailsActiveAttempts_AndRejectsRequestPairingWith503()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var started = await StartBranchAAsync(fixture);

            await fixture.API.ShutdownAsync();

            Assert.That(fixture.API.IsShutdown, Is.True);

            await AssertFailedAsync(fixture, started.Attempt, PairingFailure.Shutdown);

            var other      = new TestPairingClient(EnergyManagementRole.RM);
            var rejected   = await fixture.PostAsync("v1/requestPairing",           other.RequestPairing().ToJSON());
            var requested  = await fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(started.CorrectServerResponse).ToJSON(), started.Bearer);

            Assert.Multiple(() => {
                Assert.That(rejected.Status,      Is.EqualTo(HttpStatusCode.ServiceUnavailable), rejected.Body);
                Assert.That(rejected.RetryAfter,  Is.EqualTo("1"));
            });

            AssertUnauthorized(requested);

        }

        #endregion

    }

}

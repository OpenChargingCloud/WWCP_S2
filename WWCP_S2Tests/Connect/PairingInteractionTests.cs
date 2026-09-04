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
    /// The happy paths of the pairing interaction over real HTTP (S2 Connect 1.0.0, "Pairing
    /// interaction", steps 1 to 10): every row of the communication-role table on its branch
    /// (A: requestConnectionDetails, B: postConnectionDetails), the Initiator flow with an
    /// entered pairing token, the consumption of dynamic pairing tokens, byte-identical replays
    /// of duplicate requests, the automatic unpairing of an RM, re-pairing of the same pair,
    /// the three ways of addressing the targeted node and forcePairing (PLAN.md Phase 6a).
    /// </summary>
    [TestFixture]
    public sealed class PairingInteractionTests
    {

        #region Data

        /// <summary>
        /// The outcome of a complete pairing run through the fixture.
        /// </summary>
        private sealed record PairingRun(String                  Bearer,
                                         RequestPairingResponse  Response,
                                         PairingAttempt          Attempt,
                                         AccessToken             AccessToken,
                                         ConnectionDetails?      ClientConnectionDetails,
                                         Pairing                 Pairing,
                                         Pairing?                Replaced,
                                         IReadOnlyList<Pairing>  Superseded);

        #endregion

        #region (private) Helpers

        private static IReadOnlyList<PairingAttempt> CompletedAttemptsOf(PairingServerFixture Fixture)
        {
            lock (Fixture.CompletedAttempts)
            {
                return [.. Fixture.CompletedAttempts];
            }
        }

        private static IReadOnlyList<(Pairing Pairing, Pairing? Replaced, IReadOnlyList<Pairing> Superseded)> CompletedPairingsOf(PairingServerFixture Fixture)
        {
            lock (Fixture.CompletedPairings)
            {
                return [.. Fixture.CompletedPairings];
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
        /// Step 1 to 3: requestPairing, expecting 200, and return the bearer token and the response.
        /// </summary>
        private static async Task<(String Bearer, RequestPairingResponse Response)> StartAttemptAsync(PairingServerFixture  Fixture,
                                                                                                        TestPairingClient     Client,
                                                                                                        PairingTarget?        Target         = null,
                                                                                                        Boolean               ForcePairing   = false)
        {

            var result = await Fixture.PostAsync("v1/requestPairing", Client.RequestPairing(Target, ForcePairing).ToJSON());

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK), result.Body);
            Assert.That(RequestPairingResponse.TryParse(result.Object, out var response, out var error), Is.True, error);

            return (response!.PairingAttemptId.Value, response);

        }

        /// <summary>
        /// A complete pairing interaction (steps 1 to 10) on the branch the expected communication
        /// role of the server node prescribes, asserting the branch, the stored pairing and the
        /// events on the way.
        /// </summary>
        private static async Task<PairingRun> PairAsync(PairingServerFixture  Fixture,
                                                        HostedNode            ServerNode,
                                                        TestPairingClient     Client,
                                                        CommunicationRole     ExpectedServerRole,
                                                        PairingTarget?        Target         = null,
                                                        Boolean               ForcePairing   = false)
        {

            var pairingsBefore    = CompletedPairingsOf(Fixture).Count;
            var serverIsServer    = ExpectedServerRole == CommunicationRole.CommunicationServer;

            #region Step 1-3: requestPairing

            var (bearer, response) = await StartAttemptAsync(Fixture, Client, Target, ForcePairing);
            var attempt            = FindAttempt(Fixture, bearer);

            Assert.Multiple(() => {
                Assert.That(response.ServerNodeDescription.Id,              Is.EqualTo(ServerNode.Id));
                Assert.That(response.ServerEndpointDescription.Deployment,  Is.EqualTo(Fixture.Endpoint.Deployment));
                Assert.That(response.SelectedHmacHashingAlgorithm,          Is.EqualTo(HmacHashingAlgorithm.SHA256));
                Assert.That(response.ClientHmacChallengeResponse.ConstantTimeEquals(Client.ExpectedClientChallengeResponse(Fixture)), Is.True, "The server answered the client challenge with the wrong response!");
                Assert.That(attempt.ServerNode,                             Is.SameAs(ServerNode));
                Assert.That(attempt.ClientNodeId,                           Is.EqualTo(Client.Node.Id));
                Assert.That(attempt.ClientDeployment,                       Is.EqualTo(Client.Deployment));
                Assert.That(attempt.ServerCommunicationRole,                Is.EqualTo(ExpectedServerRole));
                Assert.That(attempt.ExpectsRequestConnectionDetails,        Is.EqualTo(serverIsServer));
                Assert.That(attempt.ForcePairing,                           Is.EqualTo(ForcePairing));
                Assert.That(attempt.State,                                  Is.EqualTo(PairingAttemptState.AwaitingConnectionDetails));
                Assert.That(attempt.IsActive,                               Is.True);
            });

            #endregion

            #region Step 6A-8A or 6B-8B: exchange the connection details

            var serverResponse  = Client.ServerChallengeResponse(Fixture, response.ServerHmacChallenge);

            AccessToken         accessToken;
            ConnectionDetails?  clientConnectionDetails = null;

            if (serverIsServer)
            {

                var result = await Fixture.PostAsync("v1/requestConnectionDetails", new RequestConnectionDetailsRequest(serverResponse).ToJSON(), bearer);

                Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK), result.Body);
                Assert.That(ConnectionDetails.TryParse(result.Object, out var details, out var error, Fixture.Options.ParserOptions), Is.True, error);

                Assert.Multiple(() => {
                    Assert.That(details!.InitiateSessionUrl,       Is.EqualTo(Fixture.SessionInitiationUrl!.Value));
                    Assert.That(details.AccessToken.Length,        Is.GreaterThanOrEqualTo(S2ConnectDefaults.MinAccessTokenLength));
                    Assert.That(details.CertificateFingerprints,   Is.Null);
                });

                accessToken = details!.AccessToken;

            }

            else
            {

                clientConnectionDetails = Client.ConnectionDetails();

                var result = await Fixture.PostAsync("v1/postConnectionDetails", new PostConnectionDetailsRequest(serverResponse, clientConnectionDetails).ToJSON(), bearer);

                Assert.Multiple(() => {
                    Assert.That(result.Status,  Is.EqualTo(HttpStatusCode.NoContent), result.Body);
                    Assert.That(result.Body,    Is.Empty);
                });

                accessToken = clientConnectionDetails.AccessToken;

            }

            Assert.That(attempt.State, Is.EqualTo(PairingAttemptState.AwaitingFinalization));

            #endregion

            #region Step 9-10: finalizePairing

            var finalized = await Fixture.PostAsync("v1/finalizePairing", new FinalizePairingRequest(true).ToJSON(), bearer);

            Assert.That(finalized.Status, Is.EqualTo(HttpStatusCode.NoContent), finalized.Body);

            #endregion

            #region The stored pairing, the attempt and the events

            await WaitUntilAsync(() => CompletedPairingsOf(Fixture).Count > pairingsBefore, "the OnPairingCompleted event");

            var pairing = await Fixture.Store.GetPairingAsync(ServerNode.Id, Client.Node.Id);

            Assert.That(pairing, Is.Not.Null, "The pairing was not stored!");

            Assert.Multiple(() => {

                Assert.That(pairing!.LocalNodeId,                          Is.EqualTo(ServerNode.Id));
                Assert.That(pairing.RemoteNodeId,                          Is.EqualTo(Client.Node.Id));
                Assert.That(pairing.RemoteNodeDescription,                 Is.EqualTo(Client.Node));
                Assert.That(pairing.RemoteEndpointDescription,             Is.EqualTo(Client.Endpoint));
                Assert.That(pairing.RemoteRole,                            Is.EqualTo(Client.Node.Role));
                Assert.That(pairing.LocalCommunicationRole,                Is.EqualTo(ExpectedServerRole));
                Assert.That(pairing.AccessToken.ConstantTimeEquals(accessToken), Is.True, "The stored access token differs from the exchanged one!");

                if (serverIsServer)
                {
                    Assert.That(pairing.InitiateSessionUrl,                Is.Null);
                    Assert.That(pairing.CertificateFingerprints,           Is.Null);
                }
                else
                {
                    Assert.That(pairing.InitiateSessionUrl,                Is.EqualTo(clientConnectionDetails!.InitiateSessionUrl));
                    Assert.That(pairing.CertificateFingerprints,           Is.Not.Null);
                    Assert.That(pairing.CertificateFingerprints!.TryGetValue(ConnectionDetails.SHA256Key, out var fingerprint), Is.True);
                    Assert.That(fingerprint,                               Is.EqualTo(PairingServerFixture.Fingerprint));
                }

                Assert.That(attempt.State,                                 Is.EqualTo(PairingAttemptState.Succeeded));
                Assert.That(attempt.Failure,                               Is.EqualTo(PairingFailure.None));
                Assert.That(attempt.IsActive,                              Is.False);
                Assert.That(attempt.CompletedAt,                           Is.Not.Null);
                Assert.That(attempt.Result,                                Is.EqualTo(pairing));
                Assert.That(Fixture.API.Attempts,                          Does.Contain(attempt));
                Assert.That(CompletedAttemptsOf(Fixture),                  Does.Contain(attempt));

            });

            var completed = CompletedPairingsOf(Fixture)[^1];

            Assert.That(completed.Pairing, Is.EqualTo(pairing));

            return new PairingRun(
                       bearer,
                       response,
                       attempt,
                       accessToken,
                       clientConnectionDetails,
                       pairing!,
                       completed.Replaced,
                       completed.Superseded
                   );

            #endregion

        }

        #endregion


        // The communication-role table (S2 Connect 1.0.0, "Mapping the CEM and RM to
        // communication server or client"): the CEM is the communication server between
        // similarly deployed nodes, the WAN node between differently deployed nodes.

        #region ServerCEMLAN_ClientRMLAN_ServerIsCommunicationServer_BranchA()

        [Test]
        [S2C("CommunicationRoles.CEM-LAN.RM-LAN")]
        [S2C("Pairing.8A")]
        [S2C("Pairing.10")]
        public async Task ServerCEMLAN_ClientRMLAN_ServerIsCommunicationServer_BranchA()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.LAN);

            var client = new TestPairingClient(EnergyManagementRole.RM, Deployment.LAN);
            var cem    = fixture.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(client.Token);

            var run = await PairAsync(fixture, cem, client, CommunicationRole.CommunicationServer);

            Assert.Multiple(() => {
                Assert.That(fixture.API.UsesLANChallengeResponse,  Is.True);
                Assert.That(run.Pairing.IsCommunicationServer,     Is.True);
                Assert.That(run.Attempt.ServerNodeIsInitiator,     Is.False);
                Assert.That(run.Replaced,                          Is.Null);
                Assert.That(run.Superseded,                        Is.Empty);
            });

        }

        #endregion

        #region ServerRMLAN_ClientCEMLAN_ServerIsCommunicationClient_BranchB()

        [Test]
        [S2C("CommunicationRoles.CEM-LAN.RM-LAN")]
        [S2C("Pairing.8B")]
        [S2C("Pairing.10")]
        public async Task ServerRMLAN_ClientCEMLAN_ServerIsCommunicationClient_BranchB()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.LAN);

            var client = new TestPairingClient(EnergyManagementRole.CEM, Deployment.LAN);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var run = await PairAsync(fixture, rm, client, CommunicationRole.CommunicationClient);

            Assert.Multiple(() => {
                Assert.That(fixture.API.UsesLANChallengeResponse,  Is.True);
                Assert.That(run.Pairing.IsCommunicationClient,     Is.True);
                Assert.That(run.ClientConnectionDetails,           Is.Not.Null);
                Assert.That(run.Replaced,                          Is.Null);
                Assert.That(run.Superseded,                        Is.Empty);
            });

        }

        #endregion

        #region ServerCEMWAN_ClientRMWAN_ServerIsCommunicationServer_BranchA()

        [Test]
        [S2C("CommunicationRoles.CEM-WAN.RM-WAN")]
        [S2C("Pairing.8A")]
        public async Task ServerCEMWAN_ClientRMWAN_ServerIsCommunicationServer_BranchA()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.WAN);

            var client = new TestPairingClient(EnergyManagementRole.RM, Deployment.WAN);
            var cem    = fixture.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(client.Token);

            var run = await PairAsync(fixture, cem, client, CommunicationRole.CommunicationServer);

            Assert.Multiple(() => {
                Assert.That(fixture.API.UsesLANChallengeResponse,  Is.False);
                Assert.That(fixture.Endpoint.DomainName,           Is.EqualTo("127.0.0.1"));
                Assert.That(run.Pairing.IsCommunicationServer,     Is.True);
                Assert.That(run.Replaced,                          Is.Null);
                Assert.That(run.Superseded,                        Is.Empty);
            });

        }

        #endregion

        #region ServerRMWAN_ClientCEMWAN_ServerIsCommunicationClient_BranchB()

        [Test]
        [S2C("CommunicationRoles.CEM-WAN.RM-WAN")]
        [S2C("Pairing.8B")]
        public async Task ServerRMWAN_ClientCEMWAN_ServerIsCommunicationClient_BranchB()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.WAN);

            var client = new TestPairingClient(EnergyManagementRole.CEM, Deployment.WAN);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var run = await PairAsync(fixture, rm, client, CommunicationRole.CommunicationClient);

            Assert.Multiple(() => {
                Assert.That(fixture.API.UsesLANChallengeResponse,  Is.False);
                Assert.That(run.Pairing.IsCommunicationClient,     Is.True);
                Assert.That(run.Replaced,                          Is.Null);
                Assert.That(run.Superseded,                        Is.Empty);
            });

        }

        #endregion

        #region ServerCEMLAN_ClientRMWAN_ServerIsCommunicationClient_BranchB()

        [Test]
        [S2C("CommunicationRoles.CEM-LAN.RM-WAN")]
        [S2C("Pairing.8B")]
        public async Task ServerCEMLAN_ClientRMWAN_ServerIsCommunicationClient_BranchB()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.LAN);

            // The WAN node is the communication server, even though it is the RM.
            var client = new TestPairingClient(EnergyManagementRole.RM, Deployment.WAN);
            var cem    = fixture.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(client.Token);

            var run = await PairAsync(fixture, cem, client, CommunicationRole.CommunicationClient);

            Assert.Multiple(() => {
                Assert.That(run.Pairing.IsCommunicationClient,     Is.True);
                Assert.That(run.Pairing.RemoteRole,                Is.EqualTo(EnergyManagementRole.RM));
                Assert.That(run.Pairing.RemoteEndpointDescription.Deployment, Is.EqualTo(Deployment.WAN));
                Assert.That(run.Replaced,                          Is.Null);
                Assert.That(run.Superseded,                        Is.Empty);
            });

        }

        #endregion

        #region ServerRMLAN_ClientCEMWAN_ServerIsCommunicationClient_BranchB()

        [Test]
        [S2C("CommunicationRoles.CEM-WAN.RM-LAN")]
        [S2C("Pairing.8B")]
        public async Task ServerRMLAN_ClientCEMWAN_ServerIsCommunicationClient_BranchB()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.LAN);

            var client = new TestPairingClient(EnergyManagementRole.CEM, Deployment.WAN);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var run = await PairAsync(fixture, rm, client, CommunicationRole.CommunicationClient);

            Assert.Multiple(() => {
                Assert.That(run.Pairing.IsCommunicationClient,     Is.True);
                Assert.That(run.Pairing.RemoteEndpointDescription.Deployment, Is.EqualTo(Deployment.WAN));
                Assert.That(run.Replaced,                          Is.Null);
                Assert.That(run.Superseded,                        Is.Empty);
            });

        }

        #endregion

        #region ServerCEMWAN_ClientRMLAN_ServerIsCommunicationServer_BranchA()

        [Test]
        [S2C("CommunicationRoles.CEM-WAN.RM-LAN")]
        [S2C("Pairing.8A")]
        public async Task ServerCEMWAN_ClientRMLAN_ServerIsCommunicationServer_BranchA()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.WAN);

            var client = new TestPairingClient(EnergyManagementRole.RM, Deployment.LAN);
            var cem    = fixture.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(client.Token);

            var run = await PairAsync(fixture, cem, client, CommunicationRole.CommunicationServer);

            Assert.Multiple(() => {
                Assert.That(fixture.API.UsesLANChallengeResponse,  Is.False);
                Assert.That(run.Pairing.IsCommunicationServer,     Is.True);
                Assert.That(run.Pairing.RemoteEndpointDescription.Deployment, Is.EqualTo(Deployment.LAN));
                Assert.That(run.Replaced,                          Is.Null);
                Assert.That(run.Superseded,                        Is.Empty);
            });

        }

        #endregion

        #region ServerRMWAN_ClientCEMLAN_ServerIsCommunicationServer_BranchA()

        [Test]
        [S2C("CommunicationRoles.CEM-LAN.RM-WAN")]
        [S2C("Pairing.8A")]
        public async Task ServerRMWAN_ClientCEMLAN_ServerIsCommunicationServer_BranchA()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Deployment.WAN);

            // The WAN node is the communication server, even though it is the RM.
            var client = new TestPairingClient(EnergyManagementRole.CEM, Deployment.LAN);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var run = await PairAsync(fixture, rm, client, CommunicationRole.CommunicationServer);

            Assert.Multiple(() => {
                Assert.That(fixture.API.UsesLANChallengeResponse,  Is.False);
                Assert.That(run.Pairing.IsCommunicationServer,     Is.True);
                Assert.That(run.Pairing.RemoteRole,                Is.EqualTo(EnergyManagementRole.CEM));
                Assert.That(run.Replaced,                          Is.Null);
                Assert.That(run.Superseded,                        Is.Empty);
            });

        }

        #endregion


        // The Initiator flow: the end user entered the client's pairing code at the server node.

        #region ServerNodeIsInitiator_EnteredToken_IsUsedAndConsumed()

        [Test]
        [S2C("Pairing.0.Precondition")]
        [S2C("Pairing.1.Initiator")]
        public async Task ServerNodeIsInitiator_EnteredToken_IsUsedAndConsumed()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);

            // No own token: the end user typed the client's pairing code in at the RM.
            rm.EnterPairingToken(client.Token, client.Node.Id);

            Assert.Multiple(() => {
                Assert.That(rm.HasValidPairingToken,                                  Is.False);
                Assert.That(rm.TryGetEnteredPairingToken(client.Node.Id, out _),      Is.True);
            });

            var run = await PairAsync(fixture, rm, client, CommunicationRole.CommunicationClient);

            Assert.Multiple(() => {
                Assert.That(run.Attempt.ServerNodeIsInitiator,                        Is.True);
                Assert.That(run.Attempt.ToJSON()["serverNodeIsInitiator"]?.Value<Boolean>(), Is.True);
                Assert.That(rm.TryGetEnteredPairingToken(client.Node.Id, out _),      Is.False, "The entered token must be consumed by the successful pairing!");
                Assert.That(rm.HasValidPairingToken,                                  Is.False);
            });

        }

        #endregion

        #region ServerNodeIsInitiator_TokenEnteredForAnyRemoteNode_IsUsedAndConsumed()

        [Test]
        [S2C("Pairing.1.Initiator")]
        public async Task ServerNodeIsInitiator_TokenEnteredForAnyRemoteNode_IsUsedAndConsumed()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.RM);
            var cem    = fixture.AddNode(EnergyManagementRole.CEM);

            cem.EnterPairingToken(client.Token);

            var run = await PairAsync(fixture, cem, client, CommunicationRole.CommunicationServer);

            Assert.Multiple(() => {
                Assert.That(run.Attempt.ServerNodeIsInitiator,                        Is.True);
                Assert.That(cem.TryGetEnteredPairingToken(Node_Id.NewRandom, out _),  Is.False, "The entered token must be consumed by the successful pairing!");
            });

        }

        #endregion


        // Own pairing tokens (the server node is the Responder node).

        #region DynamicPairingToken_IsConsumedByASuccessfulPairing()

        [Test]
        [S2C("PairingToken.Dynamic")]
        [S2C("Pairing.10")]
        public async Task DynamicPairingToken_IsConsumedByASuccessfulPairing()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            var code   = rm.IssueDynamicPairingToken();
            var client = new TestPairingClient(EnergyManagementRole.CEM, Token: code.PairingToken);

            Assert.Multiple(() => {
                Assert.That(code.NodeIdAlias,               Is.Null);
                Assert.That(code.PairingToken.Length,       Is.EqualTo(6));
                Assert.That(rm.HasValidPairingToken,        Is.True);
                Assert.That(rm.PairingTokenIsStatic,        Is.False);
                Assert.That(rm.PairingTokenExpiresAt,       Is.Not.Null);
            });

            var run = await PairAsync(fixture, rm, client, CommunicationRole.CommunicationClient);

            Assert.Multiple(() => {
                Assert.That(run.Attempt.ServerNodeIsInitiator,  Is.False);
                Assert.That(rm.HasValidPairingToken,            Is.False, "A dynamic token must be consumed by the successful pairing!");
                Assert.That(rm.PairingCode,                     Is.Null);
            });

            // The same code cannot be used for a second pairing...
            var second = new TestPairingClient(EnergyManagementRole.CEM, Token: code.PairingToken);
            var result = await fixture.PostAsync("v1/requestPairing", second.RequestPairing().ToJSON());

            Assert.Multiple(() => {
                Assert.That(result.Status,        Is.EqualTo(HttpStatusCode.BadRequest), result.Body);
                Assert.That(result.ErrorMessage,  Is.EqualTo("NoValidPairingTokenOnPairingServer"));
            });

        }

        #endregion

        #region StaticPairingToken_StaysAfterASuccessfulPairing()

        [Test]
        [S2C("PairingToken.Static")]
        public async Task StaticPairingToken_StaysAfterASuccessfulPairing()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            await PairAsync(fixture, rm, client, CommunicationRole.CommunicationClient);

            Assert.Multiple(() => {
                Assert.That(rm.HasValidPairingToken,                     Is.True, "A static token must survive a successful pairing!");
                Assert.That(rm.PairingTokenIsStatic,                     Is.True);
                Assert.That(rm.TryGetPairingToken(out var token),        Is.True);
                Assert.That(token,                                       Is.EqualTo(client.Token));
            });

            // ...so another client can pair with the same printed code.
            var second = new TestPairingClient(EnergyManagementRole.CEM);
            var result = await fixture.PostAsync("v1/requestPairing", second.RequestPairing().ToJSON());

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK), result.Body);

        }

        #endregion


        // Duplicate requests (retries after a lost response) are replayed byte-identically.

        #region RequestPairing_IdenticalDuplicate_ReplaysTheSameResponse()

        [Test]
        [S2C("Pairing.ReplayDuplicate")]
        [S2C("Pairing.3")]
        public async Task RequestPairing_IdenticalDuplicate_ReplaysTheSameResponse()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var first   = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());
            var second  = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            Assert.Multiple(() => {
                Assert.That(first.Status,   Is.EqualTo(HttpStatusCode.OK), first.Body);
                Assert.That(second.Status,  Is.EqualTo(HttpStatusCode.OK), second.Body);
                Assert.That(second.Body,    Is.EqualTo(first.Body), "The duplicate must be answered with the identical response!");
            });

            Assert.That(RequestPairingResponse.TryParse(first.Object,  out var firstResponse,  out var error), Is.True, error);
            Assert.That(RequestPairingResponse.TryParse(second.Object, out var secondResponse, out     error), Is.True, error);

            Assert.Multiple(() => {
                Assert.That(secondResponse!.PairingAttemptId.Value,     Is.EqualTo(firstResponse!.PairingAttemptId.Value));
                Assert.That(secondResponse.ServerHmacChallenge,         Is.EqualTo(firstResponse.ServerHmacChallenge));
                Assert.That(fixture.API.Attempts,                       Has.Count.EqualTo(1));
                Assert.That(fixture.API.ActiveAttempts,                 Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region RequestConnectionDetails_IdenticalDuplicate_ReplaysTheSameAccessToken()

        [Test]
        [S2C("Pairing.ReplayDuplicate")]
        [S2C("Pairing.8A")]
        public async Task RequestConnectionDetails_IdenticalDuplicate_ReplaysTheSameAccessToken()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.RM);
            var cem    = fixture.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(client.Token);

            var (bearer, response) = await StartAttemptAsync(fixture, client);
            var attempt            = FindAttempt(fixture, bearer);
            var request            = new RequestConnectionDetailsRequest(client.ServerChallengeResponse(fixture, response.ServerHmacChallenge)).ToJSON();

            var first   = await fixture.PostAsync("v1/requestConnectionDetails", request, bearer);
            var second  = await fixture.PostAsync("v1/requestConnectionDetails", request, bearer);

            Assert.Multiple(() => {
                Assert.That(first.Status,   Is.EqualTo(HttpStatusCode.OK), first.Body);
                Assert.That(second.Status,  Is.EqualTo(HttpStatusCode.OK), second.Body);
                Assert.That(second.Body,    Is.EqualTo(first.Body), "The duplicate must be answered with the identical connection details!");
                Assert.That(attempt.State,  Is.EqualTo(PairingAttemptState.AwaitingFinalization));
            });

            Assert.That(ConnectionDetails.TryParse(first.Object,  out var firstDetails,  out var error, fixture.Options.ParserOptions), Is.True, error);
            Assert.That(ConnectionDetails.TryParse(second.Object, out var secondDetails, out     error, fixture.Options.ParserOptions), Is.True, error);
            Assert.That(secondDetails!.AccessToken.ConstantTimeEquals(firstDetails!.AccessToken), Is.True);

            var finalized = await fixture.PostAsync("v1/finalizePairing", new FinalizePairingRequest(true).ToJSON(), bearer);
            var pairing   = await fixture.Store.GetPairingAsync(cem.Id, client.Node.Id);

            Assert.Multiple(() => {
                Assert.That(finalized.Status,  Is.EqualTo(HttpStatusCode.NoContent), finalized.Body);
                Assert.That(pairing,           Is.Not.Null);
                Assert.That(pairing!.AccessToken.ConstantTimeEquals(firstDetails!.AccessToken), Is.True);
            });

        }

        #endregion

        #region PostConnectionDetails_IdenticalDuplicate_Returns204Again()

        [Test]
        [S2C("Pairing.ReplayDuplicate")]
        [S2C("Pairing.8B")]
        public async Task PostConnectionDetails_IdenticalDuplicate_Returns204Again()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var (bearer, response) = await StartAttemptAsync(fixture, client);
            var attempt            = FindAttempt(fixture, bearer);
            var details            = client.ConnectionDetails();
            var request            = new PostConnectionDetailsRequest(client.ServerChallengeResponse(fixture, response.ServerHmacChallenge), details).ToJSON();

            var first   = await fixture.PostAsync("v1/postConnectionDetails", request, bearer);
            var second  = await fixture.PostAsync("v1/postConnectionDetails", request, bearer);

            Assert.Multiple(() => {
                Assert.That(first.Status,   Is.EqualTo(HttpStatusCode.NoContent), first.Body);
                Assert.That(second.Status,  Is.EqualTo(HttpStatusCode.NoContent), second.Body);
                Assert.That(attempt.State,  Is.EqualTo(PairingAttemptState.AwaitingFinalization));
            });

            var finalized = await fixture.PostAsync("v1/finalizePairing", new FinalizePairingRequest(true).ToJSON(), bearer);
            var pairing   = await fixture.Store.GetPairingAsync(rm.Id, client.Node.Id);

            Assert.Multiple(() => {
                Assert.That(finalized.Status,  Is.EqualTo(HttpStatusCode.NoContent), finalized.Body);
                Assert.That(pairing,           Is.Not.Null);
                Assert.That(pairing!.AccessToken.ConstantTimeEquals(details.AccessToken), Is.True);
                Assert.That(pairing.InitiateSessionUrl, Is.EqualTo(details.InitiateSessionUrl));
            });

        }

        #endregion

        #region FinalizePairing_IdenticalDuplicate_Returns204Again()

        [Test]
        [S2C("Pairing.ReplayDuplicate")]
        [S2C("Pairing.10")]
        public async Task FinalizePairing_IdenticalDuplicate_Returns204Again()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var run = await PairAsync(fixture, rm, client, CommunicationRole.CommunicationClient);

            // A retry of the final request (e.g. after a lost 204) is answered like the first one
            // (the completed attempt is retained for CompletedAttemptRetention), without storing
            // the pairing or raising the events a second time.
            var again = await fixture.PostAsync("v1/finalizePairing", new FinalizePairingRequest(true).ToJSON(), run.Bearer);

            Assert.Multiple(() => {
                Assert.That(again.Status,                       Is.EqualTo(HttpStatusCode.NoContent), again.Body);
                Assert.That(run.Attempt.State,                  Is.EqualTo(PairingAttemptState.Succeeded));
                Assert.That(CompletedPairingsOf(fixture),       Has.Count.EqualTo(1));
                Assert.That(CompletedAttemptsOf(fixture),       Has.Count.EqualTo(1));
            });

            Assert.That(await fixture.Store.GetPairingsAsync(rm.Id), Has.Count.EqualTo(1));

        }

        #endregion


        // Replacing and superseding earlier pairings.

        #region RMPairedWithASecondCEM_SupersedesTheFirstPairing()

        [Test]
        [S2C("Pairing.RMAutoUnpair")]
        public async Task RMPairedWithASecondCEM_SupersedesTheFirstPairing()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var cem1   = new TestPairingClient(EnergyManagementRole.CEM);
            var cem2   = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(cem1.Token);

            var run1 = await PairAsync(fixture, rm, cem1, CommunicationRole.CommunicationClient);
            var run2 = await PairAsync(fixture, rm, cem2, CommunicationRole.CommunicationClient);

            Assert.Multiple(() => {
                Assert.That(run1.Replaced,          Is.Null);
                Assert.That(run1.Superseded,        Is.Empty);
                Assert.That(run2.Replaced,          Is.Null);
                Assert.That(run2.Superseded,        Has.Count.EqualTo(1));
                Assert.That(run2.Superseded[0],     Is.EqualTo(run1.Pairing), "The earlier pairing of the RM must be reported as superseded!");
                Assert.That(CompletedPairingsOf(fixture), Has.Count.EqualTo(2));
            });

            // The store keeps both; unpairing the superseded one is up to the node layer.
            Assert.That(await fixture.Store.GetPairingsAsync(rm.Id), Has.Count.EqualTo(2));

        }

        #endregion

        #region SamePairPairedAgain_ReplacesTheEarlierPairing()

        [Test]
        [S2C("Pairing.Replace")]
        public async Task SamePairPairedAgain_ReplacesTheEarlierPairing()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.RM);
            var cem    = fixture.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(client.Token);

            var run1 = await PairAsync(fixture, cem, client, CommunicationRole.CommunicationServer);
            var run2 = await PairAsync(fixture, cem, client, CommunicationRole.CommunicationServer);

            var pairings = await fixture.Store.GetPairingsAsync(cem.Id);

            Assert.Multiple(() => {
                Assert.That(run2.Bearer,                       Is.Not.EqualTo(run1.Bearer));
                Assert.That(run2.Replaced,                     Is.EqualTo(run1.Pairing), "The earlier pairing of the same pair must be reported as replaced!");
                Assert.That(run2.Superseded,                   Is.Empty);
                Assert.That(run2.AccessToken.ConstantTimeEquals(run1.AccessToken), Is.False, "A new pairing must issue a new access token!");
                Assert.That(pairings,                          Has.Count.EqualTo(1));
                Assert.That(pairings[0],                       Is.EqualTo(run2.Pairing));
                Assert.That(fixture.API.Attempts,              Has.Count.EqualTo(2));
            });

        }

        #endregion


        // Addressing the targeted node.

        #region Pairing_ByNodeId_TargetsTheGivenNode()

        [Test]
        [S2C("Pairing.1.NodeId")]
        public async Task Pairing_ByNodeId_TargetsTheGivenNode()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm1    = fixture.AddNode(EnergyManagementRole.RM);
            var rm2    = fixture.AddNode(EnergyManagementRole.RM);
            rm1.SetStaticPairingToken(client.Token);
            rm2.SetStaticPairingToken(client.Token);

            var run = await PairAsync(fixture, rm2, client, CommunicationRole.CommunicationClient, PairingTarget.ByNodeId(rm2.Id));

            Assert.Multiple(() => {
                Assert.That(run.Attempt.Request.Target.NodeId,   Is.EqualTo(rm2.Id));
                Assert.That(run.Response.ServerNodeDescription,  Is.EqualTo(rm2.Description));
            });

            Assert.That(await fixture.Store.GetPairingAsync(rm1.Id, client.Node.Id), Is.Null);

        }

        #endregion

        #region Pairing_ByNodeIdAlias_TargetsTheAliasedNode()

        [Test]
        [S2C("Pairing.1.NodeIdAlias")]
        public async Task Pairing_ByNodeIdAlias_TargetsTheAliasedNode()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var alias  = NodeIdAlias.Parse("A0");
            var rm     = fixture.AddNode(EnergyManagementRole.RM, alias);
            var other  = fixture.AddNode(EnergyManagementRole.RM, NodeIdAlias.Parse("B7"));
            var code   = rm.SetStaticPairingToken(client.Token);
            other.SetStaticPairingToken(client.Token);

            // The pairing code shown to the end user carries the alias.
            Assert.Multiple(() => {
                Assert.That(code.NodeIdAlias,   Is.EqualTo(alias));
                Assert.That(code.Value,         Is.EqualTo($"A0-{client.Token.Value}"));
            });

            var run = await PairAsync(fixture, rm, client, CommunicationRole.CommunicationClient, PairingTarget.ByAlias(alias));

            Assert.Multiple(() => {
                Assert.That(run.Attempt.Request.Target.NodeIdAlias,  Is.EqualTo(alias));
                Assert.That(run.Response.ServerNodeDescription.Id,   Is.EqualTo(rm.Id));
            });

            Assert.That(await fixture.Store.GetPairingAsync(other.Id, client.Node.Id), Is.Null);

        }

        #endregion

        #region Pairing_WithoutTarget_UsesTheOnlyNode()

        [Test]
        [S2C("Pairing.1.NoNodeIdProvided")]
        public async Task Pairing_WithoutTarget_UsesTheOnlyNode()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var json = client.RequestPairing().ToJSON();

            Assert.Multiple(() => {
                Assert.That(json.ContainsKey("nodeId"),       Is.False);
                Assert.That(json.ContainsKey("nodeIdAlias"),  Is.False);
            });

            var run = await PairAsync(fixture, rm, client, CommunicationRole.CommunicationClient, PairingTarget.Any);

            Assert.Multiple(() => {
                Assert.That(run.Attempt.Request.Target.IsAny,        Is.True);
                Assert.That(run.Response.ServerNodeDescription.Id,   Is.EqualTo(rm.Id));
            });

        }

        #endregion


        // forcePairing: incompatible S2 message versions or communication protocols are ignored.

        #region ForcePairing_OverridesIncompatibleS2MessageVersions()

        [Test]
        [S2C("Pairing.1.ForcePairing")]
        [S2C("Pairing.1.IncompatibleS2MessageVersions")]
        public async Task ForcePairing_OverridesIncompatibleS2MessageVersions()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM, SupportedS2MessageVersions: [ "v2.0.0" ]);
            rm.SetStaticPairingToken(client.Token);

            var rejected = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            Assert.Multiple(() => {
                Assert.That(rejected.Status,        Is.EqualTo(HttpStatusCode.BadRequest), rejected.Body);
                Assert.That(rejected.ErrorMessage,  Is.EqualTo("IncompatibleS2MessageVersions"));
            });

            var run = await PairAsync(fixture, rm, client, CommunicationRole.CommunicationClient, ForcePairing: true);

            Assert.Multiple(() => {
                Assert.That(run.Attempt.ForcePairing,                             Is.True);
                Assert.That(run.Attempt.ToJSON()["forcePairing"]?.Value<Boolean>(), Is.True);
            });

        }

        #endregion

        #region ForcePairing_OverridesIncompatibleCommunicationProtocols()

        [Test]
        [S2C("Pairing.1.ForcePairing")]
        [S2C("Pairing.1.IncompatibleCommunicationProtocols")]
        public async Task ForcePairing_OverridesIncompatibleCommunicationProtocols()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.RM);
            var cem    = fixture.AddNode(EnergyManagementRole.CEM, SupportedCommunicationProtocols: [ CommunicationProtocol.Parse("MQTT") ]);
            cem.SetStaticPairingToken(client.Token);

            var rejected = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            Assert.Multiple(() => {
                Assert.That(rejected.Status,        Is.EqualTo(HttpStatusCode.BadRequest), rejected.Body);
                Assert.That(rejected.ErrorMessage,  Is.EqualTo("IncompatibleCommunicationProtocols"));
            });

            var run = await PairAsync(fixture, cem, client, CommunicationRole.CommunicationServer, ForcePairing: true);

            Assert.That(run.Attempt.ForcePairing, Is.True);

        }

        #endregion


        // The audit record of an attempt never leaks a secret.

        #region PairingAttempt_AuditRecord_ContainsNoSecrets()

        [Test]
        [S2C("Pairing.Audit")]
        public async Task PairingAttempt_AuditRecord_ContainsNoSecrets()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var run   = await PairAsync(fixture, rm, client, CommunicationRole.CommunicationClient);
            var json  = run.Attempt.ToJSON();
            var text  = json.ToString();

            Assert.Multiple(() => {
                Assert.That(json["pairingAttemptIdHash"]?.Value<String>(),         Is.Not.Null.And.Not.Empty);
                Assert.That(json["state"]?.Value<String>(),                        Is.EqualTo("Succeeded"));
                Assert.That(json["failure"]?.Value<String>(),                      Is.EqualTo("None"));
                Assert.That(json["serverCommunicationRole"]?.Value<String>(),      Is.EqualTo("CommunicationClient"));
                Assert.That(json["serverNodeId"]?.Value<String>(),                 Is.EqualTo(rm.Id.ToString()));
                Assert.That(json["clientNodeId"]?.Value<String>(),                 Is.EqualTo(client.Node.Id.ToString()));
                Assert.That(text,                                                  Does.Not.Contain(run.Bearer));
                Assert.That(text,                                                  Does.Not.Contain(run.AccessToken.Value));
                Assert.That(text,                                                  Does.Not.Contain(client.Token.Value));
                Assert.That(run.Attempt.ToString(),                                Does.Not.Contain(run.Bearer));
            });

        }

        #endregion

    }

}

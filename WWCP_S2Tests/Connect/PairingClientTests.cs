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

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The pairing client against the Phase 6 pairing server over real HTTP (PLAN.md Phase 7):
    /// the eight rows of the communication-role table, the ways of addressing the targeted
    /// node, the mapping of the server's answers to client outcomes, the handling of the
    /// pairing tokens and the client events, the LAN-only operations and the static helpers
    /// of the client (S2 Connect 1.0.0, "Pairing process").
    /// </summary>
    [TestFixture]
    public sealed class PairingClientTests
    {

        #region Data

        /// <summary>
        /// The static pairing token the server node shows and the client enters in most tests.
        /// </summary>
        private static readonly PairingToken  Token       = PairingToken.Parse("ABCD2345");

        /// <summary>
        /// A pairing token the server node does not know.
        /// </summary>
        private static readonly PairingToken  WrongToken  = PairingToken.Parse("WRONG2345");

        #endregion

        #region Helpers

        /// <summary>
        /// The pairing succeeded via branch A: the client is the communication client, so it
        /// received the session initiation URL of the server (requestConnectionDetails), and
        /// both stores hold consistent pairings.
        /// </summary>
        private static async Task AssertClientIsCommunicationClientAsync(PairingServerFixture  Server,
                                                                         PairingClientFixture  Client,
                                                                         HostedNode            ServerNode,
                                                                         HostedNode            ClientNode,
                                                                         PairingClientResult   Result)
        {

            Assert.That(Result.IsSuccess, Is.True, Result.ToString());

            var clientPairing  = await Client.Store.GetPairingAsync(ClientNode.Id, ServerNode.Id);
            var serverPairing  = await Server.Store.GetPairingAsync(ServerNode.Id, ClientNode.Id);

            Assert.That(clientPairing, Is.Not.Null, "the client stored the pairing");
            Assert.That(serverPairing, Is.Not.Null, "the server stored the pairing");

            Assert.Multiple(() => {
                Assert.That(Result.Pairing,                                      Is.EqualTo(clientPairing));
                Assert.That(clientPairing!.LocalCommunicationRole,               Is.EqualTo(CommunicationRole.CommunicationClient));
                Assert.That(serverPairing!.LocalCommunicationRole,               Is.EqualTo(CommunicationRole.CommunicationServer));
                Assert.That(clientPairing.AccessToken,                           Is.EqualTo(serverPairing.AccessToken));
                Assert.That(clientPairing.InitiateSessionUrl,                    Is.EqualTo(Server.SessionInitiationUrl), "the communication client knows the session initiation URL of the server");
                Assert.That(clientPairing.CertificateFingerprints,               Is.Null);
                Assert.That(serverPairing.InitiateSessionUrl,                    Is.Null, "the communication server needs no session initiation URL of its peer");
                Assert.That(serverPairing.CertificateFingerprints,               Is.Null);
                Assert.That(clientPairing.RemoteNodeDescription,                 Is.EqualTo(ServerNode.Description));
                Assert.That(clientPairing.RemoteEndpointDescription.Deployment,  Is.EqualTo(Server.Endpoint.Deployment));
                Assert.That(serverPairing.RemoteNodeDescription,                 Is.EqualTo(ClientNode.Description));
                Assert.That(serverPairing.RemoteEndpointDescription.Deployment,  Is.EqualTo(Client.Endpoint.Deployment));
                Assert.That(Client.Results.Select(result => result.Outcome),     Is.EqualTo(new[] { PairingClientOutcome.Success }));
                Assert.That(Server.CompletedPairings,                            Has.Count.EqualTo(1));
                Assert.That(Server.CompletedPairings[0].Pairing,                 Is.EqualTo(serverPairing));
            });

        }


        /// <summary>
        /// The pairing succeeded via branch B: the client is the communication server, so it
        /// posted its session initiation URL and CA certificate fingerprint to the server
        /// (postConnectionDetails), and both stores hold consistent pairings.
        /// </summary>
        private static async Task AssertClientIsCommunicationServerAsync(PairingServerFixture  Server,
                                                                         PairingClientFixture  Client,
                                                                         HostedNode            ServerNode,
                                                                         HostedNode            ClientNode,
                                                                         PairingClientResult   Result)
        {

            Assert.That(Result.IsSuccess, Is.True, Result.ToString());

            var clientPairing  = await Client.Store.GetPairingAsync(ClientNode.Id, ServerNode.Id);
            var serverPairing  = await Server.Store.GetPairingAsync(ServerNode.Id, ClientNode.Id);

            Assert.That(clientPairing, Is.Not.Null, "the client stored the pairing");
            Assert.That(serverPairing, Is.Not.Null, "the server stored the pairing");

            Assert.Multiple(() => {
                Assert.That(Result.Pairing,                                      Is.EqualTo(clientPairing));
                Assert.That(clientPairing!.LocalCommunicationRole,               Is.EqualTo(CommunicationRole.CommunicationServer));
                Assert.That(serverPairing!.LocalCommunicationRole,               Is.EqualTo(CommunicationRole.CommunicationClient));
                Assert.That(clientPairing.AccessToken,                           Is.EqualTo(serverPairing.AccessToken));
                Assert.That(clientPairing.InitiateSessionUrl,                    Is.Null, "the communication server needs no session initiation URL of its peer");
                Assert.That(clientPairing.CertificateFingerprints,               Is.Null);
                Assert.That(serverPairing.InitiateSessionUrl,                    Is.EqualTo(Client.Endpoint.SessionInitiationUrl), "the communication client knows the session initiation URL of the client");
                Assert.That(serverPairing.CertificateFingerprints,               Is.Not.Null);
                Assert.That(serverPairing.CertificateFingerprints!["SHA256"],    Is.EqualTo(PairingClientFixture.CAFingerprint));
                Assert.That(clientPairing.RemoteNodeDescription,                 Is.EqualTo(ServerNode.Description));
                Assert.That(clientPairing.RemoteEndpointDescription.Deployment,  Is.EqualTo(Server.Endpoint.Deployment));
                Assert.That(serverPairing.RemoteNodeDescription,                 Is.EqualTo(ClientNode.Description));
                Assert.That(serverPairing.RemoteEndpointDescription.Deployment,  Is.EqualTo(Client.Endpoint.Deployment));
                Assert.That(Client.Results.Select(result => result.Outcome),     Is.EqualTo(new[] { PairingClientOutcome.Success }));
                Assert.That(Server.CompletedPairings,                            Has.Count.EqualTo(1));
                Assert.That(Server.CompletedPairings[0].Pairing,                 Is.EqualTo(serverPairing));
            });

        }


        /// <summary>
        /// The server rejected the attempt with 400 during requestPairing, before any
        /// pairing attempt was created.
        /// </summary>
        private static void AssertRejectedAtRequestPairing(PairingServerFixture  Server,
                                                           PairingClientResult   Result,
                                                           PairingResponseError  ExpectedError)
        {

            Assert.Multiple(() => {
                Assert.That(Result.Outcome,              Is.EqualTo(PairingClientOutcome.Rejected), Result.ToString());
                Assert.That(Result.Operation,            Is.EqualTo("requestPairing"));
                Assert.That(Result.StatusCode?.Code,     Is.EqualTo(400));
                Assert.That(Result.Error,                Is.Not.Null);
                Assert.That(Result.Error?.ErrorMessage,  Is.EqualTo(ExpectedError));
                Assert.That(Result.Retryable,            Is.False);
                Assert.That(Result.Pairing,              Is.Null);
                Assert.That(Result.ServerResponse,       Is.Null);
                Assert.That(Server.API.Attempts,         Is.Empty);
                Assert.That(Server.CompletedPairings,    Is.Empty);
            });

        }

        #endregion


        // The communication-role table (S2 Connect 1.0.0, "Mapping the CEM and RM to communication server or client")

        #region Pair_ServerCEMLAN_ClientRMLAN_ClientIsCommunicationClient()

        [Test]
        [S2C("CommunicationRoles.CEM-LAN.RM-LAN")]
        [S2C("Pairing.8A")]
        public async Task Pair_ServerCEMLAN_ClientRMLAN_ClientIsCommunicationClient()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token);

            await AssertClientIsCommunicationClientAsync(server, client, cem, rm, result);

        }

        #endregion

        #region Pair_ServerRMLAN_ClientCEMLAN_ClientIsCommunicationServer()

        [Test]
        [S2C("CommunicationRoles.CEM-LAN.RM-LAN")]
        [S2C("Pairing.8B")]
        public async Task Pair_ServerRMLAN_ClientCEMLAN_ClientIsCommunicationServer()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var rm   = server.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(Token);

            var cem  = client.AddNode(EnergyManagementRole.CEM);

            var result = await client.Client.PairAsync(cem, Token);

            await AssertClientIsCommunicationServerAsync(server, client, rm, cem, result);

        }

        #endregion

        #region Pair_ServerCEMWAN_ClientRMWAN_ClientIsCommunicationClient()

        [Test]
        [S2C("CommunicationRoles.CEM-WAN.RM-WAN")]
        [S2C("Pairing.8A")]
        public async Task Pair_ServerCEMWAN_ClientRMWAN_ClientIsCommunicationClient()
        {

            await using var server  = await PairingServerFixture.CreateAsync(Deployment.WAN);
            await using var client  = PairingClientFixture.Create(server, ClientDeployment: Deployment.WAN);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token);

            Assert.That(client.Client.UsesLANChallengeResponse, Is.False, "a WAN pairing server uses the domain name formula");

            await AssertClientIsCommunicationClientAsync(server, client, cem, rm, result);

        }

        #endregion

        #region Pair_ServerRMWAN_ClientCEMWAN_ClientIsCommunicationServer()

        [Test]
        [S2C("CommunicationRoles.CEM-WAN.RM-WAN")]
        [S2C("Pairing.8B")]
        public async Task Pair_ServerRMWAN_ClientCEMWAN_ClientIsCommunicationServer()
        {

            await using var server  = await PairingServerFixture.CreateAsync(Deployment.WAN);
            await using var client  = PairingClientFixture.Create(server, ClientDeployment: Deployment.WAN);

            var rm   = server.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(Token);

            var cem  = client.AddNode(EnergyManagementRole.CEM);

            var result = await client.Client.PairAsync(cem, Token);

            await AssertClientIsCommunicationServerAsync(server, client, rm, cem, result);

        }

        #endregion

        #region Pair_ServerCEMLAN_ClientRMWAN_ClientIsCommunicationServer()

        [Test]
        [S2C("CommunicationRoles.CEM-LAN.RM-WAN")]
        [S2C("Pairing.8B")]
        public async Task Pair_ServerCEMLAN_ClientRMWAN_ClientIsCommunicationServer()
        {

            // Between differently deployed nodes the WAN node is the communication server, whatever its role.
            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server, ClientDeployment: Deployment.WAN);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token);

            await AssertClientIsCommunicationServerAsync(server, client, cem, rm, result);

        }

        #endregion

        #region Pair_ServerRMLAN_ClientCEMWAN_ClientIsCommunicationServer()

        [Test]
        [S2C("CommunicationRoles.CEM-WAN.RM-LAN")]
        [S2C("Pairing.8B")]
        public async Task Pair_ServerRMLAN_ClientCEMWAN_ClientIsCommunicationServer()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server, ClientDeployment: Deployment.WAN);

            var rm   = server.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(Token);

            var cem  = client.AddNode(EnergyManagementRole.CEM);

            var result = await client.Client.PairAsync(cem, Token);

            await AssertClientIsCommunicationServerAsync(server, client, rm, cem, result);

        }

        #endregion

        #region Pair_ServerCEMWAN_ClientRMLAN_ClientIsCommunicationClient()

        [Test]
        [S2C("CommunicationRoles.CEM-WAN.RM-LAN")]
        [S2C("Pairing.8A")]
        public async Task Pair_ServerCEMWAN_ClientRMLAN_ClientIsCommunicationClient()
        {

            await using var server  = await PairingServerFixture.CreateAsync(Deployment.WAN);
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token);

            await AssertClientIsCommunicationClientAsync(server, client, cem, rm, result);

        }

        #endregion

        #region Pair_ServerRMWAN_ClientCEMLAN_ClientIsCommunicationClient()

        [Test]
        [S2C("CommunicationRoles.CEM-LAN.RM-WAN")]
        [S2C("Pairing.8A")]
        public async Task Pair_ServerRMWAN_ClientCEMLAN_ClientIsCommunicationClient()
        {

            // Between differently deployed nodes the LAN node is the communication client, even a CEM.
            await using var server  = await PairingServerFixture.CreateAsync(Deployment.WAN);
            await using var client  = PairingClientFixture.Create(server);

            var rm   = server.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(Token);

            var cem  = client.AddNode(EnergyManagementRole.CEM);

            var result = await client.Client.PairAsync(cem, Token);

            await AssertClientIsCommunicationClientAsync(server, client, rm, cem, result);

        }

        #endregion


        // Addressing the targeted node (S2 Connect 1.0.0, "1. POST /[version]/requestPairing")

        #region Pair_ByNodeId_TargetsTheGivenNode()

        [Test]
        [S2C("Pairing.1.NodeId")]
        public async Task Pair_ByNodeId_TargetsTheGivenNode()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem1  = server.AddNode(EnergyManagementRole.CEM);
            var cem2  = server.AddNode(EnergyManagementRole.CEM);
            cem1.SetStaticPairingToken(Token);
            cem2.SetStaticPairingToken(Token);

            var rm    = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token, PairingTarget.ByNodeId(cem2.Id));

            Assert.That(result.IsSuccess, Is.True, result.ToString());

            var pairingWithCEM1  = await client.Store.GetPairingAsync(rm.Id, cem1.Id);
            var pairingWithCEM2  = await client.Store.GetPairingAsync(rm.Id, cem2.Id);

            Assert.Multiple(() => {
                Assert.That(result.Pairing!.RemoteNodeId,                     Is.EqualTo(cem2.Id));
                Assert.That(result.ServerResponse!.ServerNodeDescription.Id,  Is.EqualTo(cem2.Id));
                Assert.That(pairingWithCEM1,                                  Is.Null);
                Assert.That(pairingWithCEM2,                                  Is.Not.Null);
                Assert.That(server.API.Attempts,                              Has.Count.EqualTo(1));
                Assert.That(server.API.Attempts[0].ServerNode,                Is.SameAs(cem2));
                Assert.That(server.API.Attempts[0].Request.Target,            Is.EqualTo(PairingTarget.ByNodeId(cem2.Id)));
            });

        }

        #endregion

        #region Pair_ByAlias_TargetsTheAliasedNode()

        [Test]
        [S2C("Pairing.1.NodeIdAlias")]
        public async Task Pair_ByAlias_TargetsTheAliasedNode()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem    = server.AddNode(EnergyManagementRole.CEM, NodeIdAlias.Parse("A0"));
            var other  = server.AddNode(EnergyManagementRole.CEM, NodeIdAlias.Parse("B1"));
            cem.  SetStaticPairingToken(Token);
            other.SetStaticPairingToken(Token);

            var rm     = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token, PairingTarget.ByAlias(NodeIdAlias.Parse("A0")));

            Assert.That(result.IsSuccess, Is.True, result.ToString());

            Assert.Multiple(() => {
                Assert.That(result.Pairing!.RemoteNodeId,                     Is.EqualTo(cem.Id));
                Assert.That(result.ServerResponse!.ServerNodeDescription.Id,  Is.EqualTo(cem.Id));
                Assert.That(server.API.Attempts,                              Has.Count.EqualTo(1));
                Assert.That(server.API.Attempts[0].ServerNode,                Is.SameAs(cem));
                Assert.That(server.API.Attempts[0].Request.Target,            Is.EqualTo(PairingTarget.ByAlias(NodeIdAlias.Parse("A0"))));
            });

        }

        #endregion

        #region Pair_ByTheAliasOfAPairingCode_UsesTheAliasAndTheTokenOfTheCode()

        [Test]
        [S2C("Pairing.PairingCode")]
        [S2C("Pairing.1.NodeIdAlias")]
        [S2C("PairingToken.Dynamic")]
        public async Task Pair_ByTheAliasOfAPairingCode_UsesTheAliasAndTheTokenOfTheCode()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem    = server.AddNode(EnergyManagementRole.CEM, NodeIdAlias.Parse("A0"));
            var other  = server.AddNode(EnergyManagementRole.CEM, NodeIdAlias.Parse("B1"));

            // The pairing code shown by the server node: "[alias]-[token]"...
            var code   = cem.IssueDynamicPairingToken();

            Assert.That(code.NodeIdAlias, Is.Not.Null);
            Assert.That(code.NodeIdAlias, Is.EqualTo(NodeIdAlias.Parse("A0")));

            // ...is split by the end user's client into the target and the token.
            var rm     = client.AddNode(EnergyManagementRole.RM);
            var result = await client.Client.PairAsync(rm, code.PairingToken, PairingTarget.ByAlias(code.NodeIdAlias!.Value));

            Assert.That(result.IsSuccess, Is.True, result.ToString());

            Assert.Multiple(() => {
                Assert.That(result.Pairing!.RemoteNodeId,   Is.EqualTo(cem.Id));
                Assert.That(cem.HasValidPairingToken,       Is.False, "the dynamic token of the server node is consumed");
                Assert.That(other.HasValidPairingToken,     Is.False);
            });

        }

        #endregion

        #region Pair_WithoutTarget_SingleNodeEndpoint_PairsWithTheOnlyNode()

        [Test]
        [S2C("Pairing.1.SingleNode")]
        public async Task Pair_WithoutTarget_SingleNodeEndpoint_PairsWithTheOnlyNode()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token);

            Assert.That(result.IsSuccess, Is.True, result.ToString());

            Assert.Multiple(() => {
                Assert.That(result.Pairing!.RemoteNodeId,             Is.EqualTo(cem.Id));
                Assert.That(server.API.Attempts,                      Has.Count.EqualTo(1));
                Assert.That(server.API.Attempts[0].Request.Target.IsAny,  Is.True, "neither nodeId nor nodeIdAlias was sent");
            });

        }

        #endregion

        #region Pair_WithoutTarget_MultiNodeEndpoint_IsRejectedWithNoNodeIdProvided()

        [Test]
        [S2C("Pairing.1.NoNodeIdProvided")]
        public async Task Pair_WithoutTarget_MultiNodeEndpoint_IsRejectedWithNoNodeIdProvided()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem1  = server.AddNode(EnergyManagementRole.CEM);
            var cem2  = server.AddNode(EnergyManagementRole.CEM);
            cem1.SetStaticPairingToken(Token);
            cem2.SetStaticPairingToken(Token);

            var rm    = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token);

            AssertRejectedAtRequestPairing(server, result, PairingResponseError.NoNodeIdProvided);

        }

        #endregion

        #region Pair_UnknownAlias_IsRejectedWithNodeNotFound()

        [Test]
        [S2C("Pairing.1.NodeNotFound")]
        public async Task Pair_UnknownAlias_IsRejectedWithNodeNotFound()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM, NodeIdAlias.Parse("A0"));
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token, PairingTarget.ByAlias(NodeIdAlias.Parse("ZZ")));

            AssertRejectedAtRequestPairing(server, result, PairingResponseError.NodeNotFound);

        }

        #endregion


        // The mapping of the server's answers to client outcomes

        #region Pair_ServerNodeNotReady_IsRejectedWithOther()

        [Test]
        [S2C("Pairing.1.Other")]
        public async Task Pair_ServerNodeNotReady_IsRejectedWithOther()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(Token);
            cem.IsReadyForPairing = false;

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token);

            AssertRejectedAtRequestPairing(server, result, PairingResponseError.Other);

        }

        #endregion

        #region Pair_SameRole_IsRejectedWithInvalidCombinationOfRoles()

        [Test]
        [S2C("Pairing.1.InvalidCombinationOfRoles")]
        public async Task Pair_SameRole_IsRejectedWithInvalidCombinationOfRoles()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var serverCEM  = server.AddNode(EnergyManagementRole.CEM);
            serverCEM.SetStaticPairingToken(Token);

            var clientCEM  = client.AddNode(EnergyManagementRole.CEM);

            var result = await client.Client.PairAsync(clientCEM, Token);

            AssertRejectedAtRequestPairing(server, result, PairingResponseError.InvalidCombinationOfRoles);

        }

        #endregion

        #region Pair_IncompatibleS2MessageVersions_IsRejected()

        [Test]
        [S2C("Pairing.1.IncompatibleS2MessageVersions")]
        public async Task Pair_IncompatibleS2MessageVersions_IsRejected()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM, SupportedS2MessageVersions: [ "v9.9.9" ]);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token);

            AssertRejectedAtRequestPairing(server, result, PairingResponseError.IncompatibleS2MessageVersions);

            Assert.That(result.Error?.AdditionalInfo, Does.Contain("v9.9.9"));

        }

        #endregion

        #region Pair_IncompatibleS2MessageVersions_ForcePairing_Succeeds()

        [Test]
        [S2C("Pairing.1.ForcePairing")]
        [S2C("Pairing.1.IncompatibleS2MessageVersions")]
        public async Task Pair_IncompatibleS2MessageVersions_ForcePairing_Succeeds()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM, SupportedS2MessageVersions: [ "v9.9.9" ]);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token, ForcePairing: true);

            Assert.That(result.IsSuccess, Is.True, result.ToString());

            Assert.Multiple(() => {
                Assert.That(result.Pairing!.RemoteNodeId,               Is.EqualTo(cem.Id));
                Assert.That(server.API.Attempts,                        Has.Count.EqualTo(1));
                Assert.That(server.API.Attempts[0].ForcePairing,        Is.True);
                Assert.That(server.API.Attempts[0].Request.ForcePairing, Is.True, "forcePairing was sent on the wire");
                Assert.That(server.CompletedPairings,                   Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region Pair_NoTokenOnTheServer_IsRejectedWithNoValidPairingTokenOnPairingServer()

        [Test]
        [S2C("Pairing.1.NoValidPairingTokenOnPairingServer")]
        public async Task Pair_NoTokenOnTheServer_IsRejectedWithNoValidPairingTokenOnPairingServer()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM);   // no own token, no entered token
            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token);

            AssertRejectedAtRequestPairing(server, result, PairingResponseError.NoValidPairingTokenOnPairingServer);

            Assert.That(cem.HasValidPairingToken, Is.False);

        }

        #endregion

        #region Pair_WrongToken_ReturnsChallengeResponseMismatchAndFinalizesWithFalse()

        [Test]
        [S2C("Pairing.4")]
        [S2C("Pairing.3.ClientChecks")]
        public async Task Pair_WrongToken_ReturnsChallengeResponseMismatchAndFinalizesWithFalse()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, WrongToken);

            var clientPairing  = await client.Store.GetPairingAsync(rm.Id, cem.Id);
            var serverPairing  = await server.Store.GetPairingAsync(cem.Id, rm.Id);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,          Is.EqualTo(PairingClientOutcome.ChallengeResponseMismatch), result.ToString());
                Assert.That(result.IsSuccess,        Is.False);
                Assert.That(result.Operation,        Is.EqualTo("requestPairing"));
                Assert.That(result.StatusCode?.Code, Is.EqualTo(200), "the server answered requestPairing, the client detected the wrong clientHmacChallengeResponse");
                Assert.That(result.ServerResponse,   Is.Not.Null);
                Assert.That(result.Retryable,        Is.False, "a new attempt needs the right pairing token");
                Assert.That(result.Pairing,          Is.Null);
                Assert.That(clientPairing,           Is.Null);
                Assert.That(serverPairing,           Is.Null);
                Assert.That(server.API.Attempts,     Has.Count.EqualTo(1));
                Assert.That(server.API.Attempts[0].State,    Is.EqualTo(PairingAttemptState.Failed));
                Assert.That(server.API.Attempts[0].Failure,  Is.EqualTo(PairingFailure.ClientReportedFailure), "the client finalized the attempt with success = false");
                Assert.That(server.CompletedAttempts.Select(attempt => attempt.Failure), Does.Contain(PairingFailure.ClientReportedFailure));
                Assert.That(server.CompletedPairings, Is.Empty);
            });

        }

        #endregion

        #region Pair_ServerWithoutSessionInitiationUrl_BranchA_IsRejectedAtRequestConnectionDetails()

        [Test]
        [S2C("Pairing.6A")]
        [S2C("Pairing.8A")]
        public async Task Pair_ServerWithoutSessionInitiationUrl_BranchA_IsRejectedAtRequestConnectionDetails()
        {

            await using var server  = await PairingServerFixture.CreateAsync(WithSessionInitiationUrl: false);
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,               Is.EqualTo(PairingClientOutcome.Rejected), result.ToString());
                Assert.That(result.Operation,             Is.EqualTo("requestConnectionDetails"));
                Assert.That(result.StatusCode?.Code,      Is.EqualTo(400));
                Assert.That(result.Error?.ErrorMessage,   Is.EqualTo(PairingResponseError.Other));
                Assert.That(result.ServerResponse,        Is.Not.Null, "requestPairing had succeeded");
                Assert.That(result.Retryable,             Is.False);
                Assert.That(result.Pairing,               Is.Null);
                Assert.That(server.CompletedAttempts.Select(attempt => attempt.Failure), Does.Contain(PairingFailure.ConnectionDetailsUnavailable));
                Assert.That(server.CompletedPairings,     Is.Empty);
            });

        }

        #endregion

        #region Pair_ClientWithoutSessionInitiationUrl_BranchB_ReturnsInvalidConfiguration()

        [Test]
        [S2C("Pairing.6B")]
        public async Task Pair_ClientWithoutSessionInitiationUrl_BranchB_ReturnsInvalidConfiguration()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server, WithSessionInitiationUrl: false);

            var rm   = server.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(Token);

            var cem  = client.AddNode(EnergyManagementRole.CEM);   // becomes the communication server, but cannot be reached

            var result = await client.Client.PairAsync(cem, Token);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,          Is.EqualTo(PairingClientOutcome.InvalidConfiguration), result.ToString());
                Assert.That(result.Operation,        Is.EqualTo("postConnectionDetails"));
                Assert.That(result.ServerResponse,   Is.Not.Null);
                Assert.That(result.Retryable,        Is.False);
                Assert.That(result.Pairing,          Is.Null);
                Assert.That(result.Description,      Does.Contain("session initiation URL"));
                Assert.That(server.API.Attempts,     Has.Count.EqualTo(1));
                Assert.That(server.API.Attempts[0].Failure, Is.EqualTo(PairingFailure.ClientReportedFailure), "the client finalized the attempt with success = false");
                Assert.That(server.API.ActiveAttempts, Is.Empty);
                Assert.That(server.CompletedPairings,  Is.Empty);
            });

        }

        #endregion

        #region Pair_ClientWithoutCAFingerprint_BranchB_ReturnsInvalidConfiguration()

        [Test]
        [S2C("Pairing.6B.CertificateFingerprint")]
        public async Task Pair_ClientWithoutCAFingerprint_BranchB_ReturnsInvalidConfiguration()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server, WithCAFingerprint: false);

            var rm   = server.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(Token);

            var cem  = client.AddNode(EnergyManagementRole.CEM);

            var result = await client.Client.PairAsync(cem, Token);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,          Is.EqualTo(PairingClientOutcome.InvalidConfiguration), result.ToString());
                Assert.That(result.Operation,        Is.EqualTo("postConnectionDetails"));
                Assert.That(result.ServerResponse,   Is.Not.Null);
                Assert.That(result.Pairing,          Is.Null);
                Assert.That(result.Description,      Does.Contain("CA certificate fingerprint"));
                Assert.That(server.API.Attempts,     Has.Count.EqualTo(1));
                Assert.That(server.API.Attempts[0].Failure, Is.EqualTo(PairingFailure.ClientReportedFailure));
                Assert.That(server.CompletedPairings, Is.Empty);
            });

        }

        #endregion

        #region Pair_LANServerWithoutAssumedFingerprint_ReturnsInvalidConfigurationBeforeAnyAttempt()

        [Test]
        [S2C("Pairing.ChallengeResponse.LAN")]
        public async Task Pair_LANServerWithoutAssumedFingerprint_ReturnsInvalidConfigurationBeforeAnyAttempt()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server, WithAssumedFingerprint: false);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                                 Is.EqualTo(PairingClientOutcome.InvalidConfiguration), result.ToString());
                Assert.That(result.Operation,                               Is.EqualTo("requestPairing"));
                Assert.That(result.ServerResponse,                          Is.Null, "no requestPairing request was sent");
                Assert.That(result.StatusCode,                              Is.Null);
                Assert.That(result.Description,                             Does.Contain("fingerprint"));
                Assert.That(client.Client.ServerCertificateFingerprint,     Is.Null, "plain HTTP: no certificate was observed and none was assumed");
                Assert.That(client.Client.SelectedAPIVersion,               Is.EqualTo("v1"), "only the version index was fetched");
                Assert.That(server.API.Attempts,                            Is.Empty);
                Assert.That(client.Results,                                 Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region Pair_WrongPairingUrlPort_ReturnsTransportFailure()

        [Test]
        [S2C("Pairing.PairingURL")]
        public async Task Pair_WrongPairingUrlPort_ReturnsTransportFailure()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server, PairingUrl: S2BaseURL.Parse("http://127.0.0.1:1/pairing/", AllowHTTP: true));

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            // Nothing listens on port 1: the connection is refused at once; the token only bounds the test.
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var result = await client.Client.PairAsync(rm, Token, CancellationToken: timeout.Token);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,           Is.EqualTo(PairingClientOutcome.TransportFailure), result.ToString());
                Assert.That(result.Operation,         Is.EqualTo("versionIndex"), "the version index is the first request of an attempt");
                Assert.That(result.Retryable,         Is.True);
                Assert.That(result.StatusCode?.Code,  Is.EqualTo(0), "no server ever answered");
                Assert.That(result.Pairing,           Is.Null);
                Assert.That(client.Client.SelectedAPIVersion, Is.Null);
                Assert.That(server.API.Attempts,      Is.Empty);
            });

        }

        #endregion

        #region Pair_TwoClientsInParallel_TheRejectedOneSucceedsAfterThe503Retry()

        [Test]
        [S2C("Pairing.2.SequentialProcessing")]
        public async Task Pair_TwoClientsInParallel_TheRejectedOneSucceedsAfterThe503Retry()
        {

            // One attempt at a time per server node and no queue: the second concurrent requestPairing
            // is answered with 503 and "Retry-After: 1"; the client repeats it after that second.
            await using var server   = await PairingServerFixture.CreateAsync(
                                           Options: PairingServerFixture.DefaultOptions() with {
                                                        MaxQueuedPairingAttemptsPerNode  = 0,
                                                        RequestPairingDelay              = TimeSpan.FromMilliseconds(500)
                                                    }
                                       );

            await using var client1  = PairingClientFixture.Create(server);
            await using var client2  = PairingClientFixture.Create(server);

            var rm    = server.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(Token);

            var cem1  = client1.AddNode(EnergyManagementRole.CEM);
            var cem2  = client2.AddNode(EnergyManagementRole.CEM);

            // Select the API version first, so that both requestPairing requests race for the lease of the RM.
            await client1.Client.GetVersionsAsync();
            await client2.Client.GetVersionsAsync();

            var results = await Task.WhenAll(
                              client1.Client.PairAsync(cem1, Token),
                              client2.Client.PairAsync(cem2, Token)
                          );

            var pairing1  = await server.Store.GetPairingAsync(rm.Id, cem1.Id);
            var pairing2  = await server.Store.GetPairingAsync(rm.Id, cem2.Id);

            Assert.Multiple(() => {
                Assert.That(results[0].IsSuccess,       Is.True, results[0].ToString());
                Assert.That(results[1].IsSuccess,       Is.True, results[1].ToString());
                Assert.That(pairing1,                   Is.Not.Null);
                Assert.That(pairing2,                   Is.Not.Null);
                Assert.That(server.CompletedPairings,   Has.Count.EqualTo(2));
                Assert.That(server.API.Attempts,        Has.Count.EqualTo(2), "each client ran exactly one attempt, the 503 created none");
            });

        }

        #endregion

        #region Pair_TwoClientsInParallel_WithoutRetries_OneGetsServiceUnavailable()

        [Test]
        [S2C("Pairing.2.SequentialProcessing")]
        [S2C("Pairing.2.RateLimit")]
        public async Task Pair_TwoClientsInParallel_WithoutRetries_OneGetsServiceUnavailable()
        {

            await using var server   = await PairingServerFixture.CreateAsync(
                                           Options: PairingServerFixture.DefaultOptions() with {
                                                        MaxQueuedPairingAttemptsPerNode  = 0,
                                                        RequestPairingDelay              = TimeSpan.FromMilliseconds(500)
                                                    }
                                       );

            var noRetries = PairingClientFixture.DefaultOptions() with { MaxServiceUnavailableRetries = 0 };

            await using var client1  = PairingClientFixture.Create(server, Options: noRetries);
            await using var client2  = PairingClientFixture.Create(server, Options: noRetries);

            var rm    = server.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(Token);

            var cem1  = client1.AddNode(EnergyManagementRole.CEM);
            var cem2  = client2.AddNode(EnergyManagementRole.CEM);

            await client1.Client.GetVersionsAsync();
            await client2.Client.GetVersionsAsync();

            var results = await Task.WhenAll(
                              client1.Client.PairAsync(cem1, Token),
                              client2.Client.PairAsync(cem2, Token)
                          );

            Assert.That(results.Select(result => result.Outcome),
                        Is.EquivalentTo(new[] { PairingClientOutcome.Success, PairingClientOutcome.ServiceUnavailable }),
                        String.Join(" / ", results.Select(result => result.ToString())));

            var unavailable = results.Single(result => result.Outcome == PairingClientOutcome.ServiceUnavailable);

            Assert.Multiple(() => {
                Assert.That(unavailable.Operation,          Is.EqualTo("requestPairing"));
                Assert.That(unavailable.StatusCode?.Code,   Is.EqualTo(503));
                Assert.That(unavailable.Retryable,          Is.True);
                Assert.That(unavailable.Pairing,            Is.Null);
                Assert.That(unavailable.ServerResponse,     Is.Null);
                Assert.That(server.CompletedPairings,       Has.Count.EqualTo(1));
                Assert.That(server.API.Attempts,            Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region Pair_ServerAttemptExpired_ReturnsUnauthorized()

        [Test]
        [S2C("Pairing.Interruption")]
        [S2C("Pairing.PairingAttemptId")]
        public async Task Pair_ServerAttemptExpired_ReturnsUnauthorized()
        {

            // The pairingAttemptId of the server expires one tick after it was issued, so it is already
            // invalid when requestConnectionDetails arrives: the server answers 401 and the client must
            // restart at requestPairing.
            await using var server  = await PairingServerFixture.CreateAsync(
                                          Options: PairingServerFixture.DefaultOptions() with {
                                                       PairingAttemptTimeout = TimeSpan.FromTicks(1)
                                                   }
                                      );

            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PairAsync(rm, Token);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,            Is.EqualTo(PairingClientOutcome.Unauthorized), result.ToString());
                Assert.That(result.Operation,          Is.EqualTo("requestConnectionDetails"));
                Assert.That(result.StatusCode?.Code,   Is.EqualTo(401));
                Assert.That(result.Retryable,          Is.True, "a new attempt may succeed without user interaction");
                Assert.That(result.ServerResponse,     Is.Not.Null);
                Assert.That(result.Pairing,            Is.Null);
                Assert.That(server.CompletedAttempts.Select(attempt => attempt.Failure), Does.Contain(PairingFailure.Timeout));
                Assert.That(server.CompletedPairings,  Is.Empty);
            });

        }

        #endregion


        // Pairing tokens and the client events

        #region Pair_ServerNodeIsInitiator_ConsumesTheDynamicTokenOfTheClientNode()

        [Test]
        [S2C("Pairing.1.Initiator")]
        [S2C("PairingToken.Dynamic")]
        [S2C("Pairing.10")]
        public async Task Pair_ServerNodeIsInitiator_ConsumesTheDynamicTokenOfTheClientNode()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem   = server.AddNode(EnergyManagementRole.CEM);
            var rm    = client.AddNode(EnergyManagementRole.RM);

            // The RM shows its pairing code and the end user enters it at the CEM (the Initiator node)...
            var code  = rm.IssueDynamicPairingToken();
            cem.EnterPairingToken(code.PairingToken, rm.Id);

            Assert.That(rm.HasValidPairingToken, Is.True);

            // ...then the RM, as pairing client, pairs with its own token.
            var result = await client.Client.PairAsync(rm, code.PairingToken);

            Assert.That(result.IsSuccess, Is.True, result.ToString());

            Assert.Multiple(() => {
                Assert.That(rm.HasValidPairingToken,                              Is.False, "the dynamic own token of the client node is consumed");
                Assert.That(rm.PairingCode,                                       Is.Null);
                Assert.That(cem.TryGetEnteredPairingToken(rm.Id, out _),          Is.False, "the entered token of the server node is consumed");
                Assert.That(server.API.Attempts,                                  Has.Count.EqualTo(1));
                Assert.That(server.API.Attempts[0].ServerNodeIsInitiator,         Is.True);
            });

        }

        #endregion

        #region Pair_StaticTokenOfTheClientNode_StaysAfterPairing()

        [Test]
        [S2C("PairingToken.Static")]
        [S2C("Pairing.10")]
        public async Task Pair_StaticTokenOfTheClientNode_StaysAfterPairing()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem   = server.AddNode(EnergyManagementRole.CEM);
            var rm    = client.AddNode(EnergyManagementRole.RM);

            // A token printed on the device: entered at the CEM, never consumed at the RM.
            rm.SetStaticPairingToken(Token);
            cem.EnterPairingToken(Token, rm.Id);

            var result = await client.Client.PairAsync(rm, Token);

            Assert.That(result.IsSuccess, Is.True, result.ToString());

            Assert.Multiple(() => {
                Assert.That(rm.HasValidPairingToken,                       Is.True, "a static token stays");
                Assert.That(rm.PairingTokenIsStatic,                       Is.True);
                Assert.That(rm.PairingCode?.PairingToken,                  Is.EqualTo(Token));
                Assert.That(cem.TryGetEnteredPairingToken(rm.Id, out _),   Is.False, "the entered token of the server node is consumed");
                Assert.That(server.API.Attempts[0].ServerNodeIsInitiator,  Is.True);
            });

        }

        #endregion

        #region Pair_RaisesOnPairingStartedAndOnPairingCompletedOncePerAttempt()

        [Test]
        [S2C("Pairing.10")]
        public async Task Pair_RaisesOnPairingStartedAndOnPairingCompletedOncePerAttempt()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            cem.SetStaticPairingToken(Token);

            var rm   = client.AddNode(EnergyManagementRole.RM);

            var started = new List<(HostedNode Node, PairingTarget Target)>();

            client.Client.OnPairingStarted += (timestamp, sender, node, target) => {
                                                  lock (started)
                                                      started.Add((node, target));
                                                  return Task.CompletedTask;
                                              };

            var success  = await client.Client.PairAsync(rm, Token);
            var failure  = await client.Client.PairAsync(rm, WrongToken, PairingTarget.ByNodeId(cem.Id));

            Assert.Multiple(() => {
                Assert.That(success.Outcome,                              Is.EqualTo(PairingClientOutcome.Success),                   success.ToString());
                Assert.That(failure.Outcome,                              Is.EqualTo(PairingClientOutcome.ChallengeResponseMismatch), failure.ToString());
                Assert.That(started,                                      Has.Count.EqualTo(2));
                Assert.That(started[0].Node,                              Is.SameAs(rm));
                Assert.That(started[0].Target.IsAny,                      Is.True);
                Assert.That(started[1].Node,                              Is.SameAs(rm));
                Assert.That(started[1].Target,                            Is.EqualTo(PairingTarget.ByNodeId(cem.Id)));
                Assert.That(client.Results.Select(result => result.Outcome),
                            Is.EqualTo(new[] { PairingClientOutcome.Success, PairingClientOutcome.ChallengeResponseMismatch }));
                Assert.That(client.Results[0],                            Is.SameAs(success));
                Assert.That(client.Results[1],                            Is.SameAs(failure));
            });

        }

        #endregion


        // The LAN-only operations over the client (S2 Connect 1.0.0, "LAN-LAN only interactions")

        #region GetEndpoint_LANServer_ReturnsTheEndpointDescription()

        [Test]
        [S2C("LAN.Endpoint")]
        public async Task GetEndpoint_LANServer_ReturnsTheEndpointDescription()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            server.AddNode(EnergyManagementRole.CEM);

            var result = await client.Client.GetEndpointAsync();

            Assert.Multiple(() => {
                Assert.That(result.IsSuccess,             Is.True, result.ToString());
                Assert.That(result.StatusCode.Code,       Is.EqualTo(200));
                Assert.That(result.Value,                 Is.EqualTo(server.Endpoint.Description));
                Assert.That(result.Value?.Deployment,     Is.EqualTo(Deployment.LAN));
                Assert.That(result.Error,                 Is.Null);
                Assert.That(client.Client.SelectedAPIVersion, Is.EqualTo("v1"), "the version was selected on the way");
            });

        }

        #endregion

        #region GetNodes_LANServer_ReturnsOneDescriptionPerNode()

        [Test]
        [S2C("LAN.Nodes")]
        public async Task GetNodes_LANServer_ReturnsOneDescriptionPerNode()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            var rm   = server.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.GetNodesAsync();

            Assert.That(result.IsSuccess, Is.True, result.ToString());
            Assert.That(result.Value,     Is.Not.Null);

            Assert.Multiple(() => {
                Assert.That(result.StatusCode.Code,  Is.EqualTo(200));
                Assert.That(result.Value,            Has.Count.EqualTo(2));
                Assert.That(result.Value,            Is.EquivalentTo(new[] { cem.Description, rm.Description }));
            });

        }

        #endregion

        #region PreparePairing_LANServer_IsForwardedToTheServer()

        [Test]
        [S2C("LAN.PreparePairing")]
        public async Task PreparePairing_LANServer_IsForwardedToTheServer()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            var rm   = client.AddNode(EnergyManagementRole.RM);

            var result = await client.Client.PreparePairingAsync(rm, cem.Id);

            Assert.That(result.IsSuccess,                   Is.True, result.ToString());
            Assert.That(server.PreparePairingRequests,      Has.Count.EqualTo(1));

            var request = server.PreparePairingRequests[0];

            Assert.Multiple(() => {
                Assert.That(result.StatusCode.Code,             Is.EqualTo(204));
                Assert.That(result.Value,                       Is.Null);
                Assert.That(request.ServerNodeId,               Is.EqualTo(cem.Id));
                Assert.That(request.ClientNodeDescription,      Is.EqualTo(rm.Description));
                Assert.That(request.ClientEndpointDescription,  Is.EqualTo(client.Endpoint.Description));
                Assert.That(server.CancelPreparePairingRequests, Is.Empty);
            });

        }

        #endregion

        #region CancelPreparePairing_LANServer_IsForwardedToTheServer()

        [Test]
        [S2C("LAN.CancelPreparePairing")]
        public async Task CancelPreparePairing_LANServer_IsForwardedToTheServer()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            var rm   = client.AddNode(EnergyManagementRole.RM);

            var prepared  = await client.Client.PreparePairingAsync(rm, cem.Id);
            var cancelled = await client.Client.CancelPreparePairingAsync(rm.Id, cem.Id);

            Assert.That(prepared.IsSuccess,                     Is.True, prepared.ToString());
            Assert.That(cancelled.IsSuccess,                    Is.True, cancelled.ToString());
            Assert.That(server.CancelPreparePairingRequests,    Has.Count.EqualTo(1));

            var request = server.CancelPreparePairingRequests[0];

            Assert.Multiple(() => {
                Assert.That(cancelled.StatusCode.Code,   Is.EqualTo(204));
                Assert.That(request.ClientNodeId,        Is.EqualTo(rm.Id));
                Assert.That(request.ServerNodeId,        Is.EqualTo(cem.Id));
            });

        }

        #endregion

        #region LANOperations_WANServer_Return404()

        [Test]
        [S2C("LAN.NotImplemented")]
        [S2C("LANOperations.WANEndpoint")]
        public async Task LANOperations_WANServer_Return404()
        {

            await using var server  = await PairingServerFixture.CreateAsync(Deployment.WAN);
            await using var client  = PairingClientFixture.Create(server, ClientDeployment: Deployment.WAN);

            var cem  = server.AddNode(EnergyManagementRole.CEM);
            var rm   = client.AddNode(EnergyManagementRole.RM);

            var endpoint  = await client.Client.GetEndpointAsync();
            var nodes     = await client.Client.GetNodesAsync();
            var prepare   = await client.Client.PreparePairingAsync(rm, cem.Id);
            var cancel    = await client.Client.CancelPreparePairingAsync(rm.Id, cem.Id);
            var wait      = await client.Client.WaitForPairingAsync(new WaitForPairingRequest([ new WaitForPairingRequestItem(rm.Id) ]));

            Assert.Multiple(() => {

                Assert.That(endpoint.IsSuccess,        Is.False);
                Assert.That(endpoint.StatusCode.Code,  Is.EqualTo(404), endpoint.ToString());
                Assert.That(endpoint.Value,            Is.Null);

                Assert.That(nodes.IsSuccess,           Is.False);
                Assert.That(nodes.StatusCode.Code,     Is.EqualTo(404), nodes.ToString());
                Assert.That(nodes.Value,               Is.Null);

                Assert.That(prepare.IsSuccess,         Is.False);
                Assert.That(prepare.StatusCode.Code,   Is.EqualTo(404), prepare.ToString());

                Assert.That(cancel.IsSuccess,          Is.False);
                Assert.That(cancel.StatusCode.Code,    Is.EqualTo(404), cancel.ToString());

                Assert.That(wait.IsSuccess,            Is.False);
                Assert.That(wait.StatusCode.Code,      Is.EqualTo(404), wait.ToString());
                Assert.That(wait.Value,                Is.Null);

                Assert.That(server.PreparePairingRequests,        Is.Empty);
                Assert.That(server.CancelPreparePairingRequests,  Is.Empty);

            });

        }

        #endregion

        #region WaitForPairing_RequestTimeoutBelow30Seconds_IsRejected()

        [Test]
        [S2C("LongPolling.ClientTimeout")]
        public async Task WaitForPairing_RequestTimeoutBelow30Seconds_IsRejected()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var rm       = client.AddNode(EnergyManagementRole.RM);
            var request  = new WaitForPairingRequest([ new WaitForPairingRequestItem(rm.Id) ]);

            // "The client must use a request time-out of at least 30 seconds".
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.Client.WaitForPairingAsync(request, TimeSpan.FromSeconds(10)));
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.Client.WaitForPairingAsync(request, TimeSpan.FromSeconds(29.999)));

            Assert.That(S2ConnectDefaults.LongPollingClientTimeout, Is.EqualTo(TimeSpan.FromSeconds(30)));

        }

        #endregion

        #region WaitForPairing_NewNode_IsAskedForItsDescription_ThenNothingIsQueued()

        [Test]
        [S2C("LAN.WaitForPairing")]
        [S2C("LongPolling.SendNodeDescription")]
        [S2C("LongPolling.ServerTimeout")]
        public async Task WaitForPairing_NewNode_IsAskedForItsDescription_ThenNothingIsQueued()
        {

            await using var server  = await PairingServerFixture.CreateAsync(
                                          Options: PairingServerFixture.DefaultOptions() with {
                                                       LongPollingTimeout = TimeSpan.FromMilliseconds(300)
                                                   }
                                      );

            await using var client  = PairingClientFixture.Create(server);

            var rm = client.AddNode(EnergyManagementRole.RM);

            // The first poll of an unknown node: the server wants its descriptions (200).
            var first = await client.Client.WaitForPairingAsync(new WaitForPairingRequest([ new WaitForPairingRequestItem(rm.Id) ]));

            Assert.That(first.IsSuccess, Is.True, first.ToString());
            Assert.That(first.Value,     Is.Not.Null);

            Assert.Multiple(() => {
                Assert.That(first.StatusCode.Code,             Is.EqualTo(200));
                Assert.That(first.Value!.Items,                Has.Count.EqualTo(1));
                Assert.That(first.Value.Items[0].ClientNodeId, Is.EqualTo(rm.Id));
                Assert.That(first.Value.Items[0].Action,       Is.EqualTo(WaitForPairingAction.SendNodeDescription));
            });

            // The next poll carries the descriptions; nothing is queued, so the server answers 204 after its timeout.
            var second = await client.Client.WaitForPairingAsync(new WaitForPairingRequest([ new WaitForPairingRequestItem(rm.Id, rm.Description, client.Endpoint.Description) ]));

            Assert.That(server.API.LongPollingServer, Is.Not.Null);
            Assert.That(server.API.LongPollingServer!.TryGetClientNode(rm.Id, out var clientNode), Is.True);

            Assert.Multiple(() => {
                Assert.That(second.IsSuccess,                  Is.True, second.ToString());
                Assert.That(second.StatusCode.Code,            Is.EqualTo(204));
                Assert.That(second.Value,                      Is.Null);
                Assert.That(clientNode!.Description,           Is.EqualTo(rm.Description));
                Assert.That(clientNode.EndpointDescription,    Is.EqualTo(client.Endpoint.Description));
                Assert.That(clientNode.PendingActions,         Is.Empty);
            });

        }

        #endregion


        // Static helpers and options

        #region GuessDeployment_LocalHostsAndIPAddresses_AreLAN()

        [Test]
        [S2C("Pairing.Deployments")]
        public void GuessDeployment_LocalHostsAndIPAddresses_AreLAN()
        {

            Assert.Multiple(() => {
                Assert.That(PairingClient.GuessDeployment(S2BaseURL.Parse("https://hostname.local/pairing/")),   Is.EqualTo(Deployment.LAN), ".local hosts are mDNS hosts");
                Assert.That(PairingClient.GuessDeployment(S2BaseURL.Parse("https://HostName.LOCAL/pairing/")),   Is.EqualTo(Deployment.LAN), "the case does not matter");
                Assert.That(PairingClient.GuessDeployment(S2BaseURL.Parse("https://localhost/pairing/")),        Is.EqualTo(Deployment.LAN));
                Assert.That(PairingClient.GuessDeployment(S2BaseURL.Parse("https://192.168.1.10/pairing/")),     Is.EqualTo(Deployment.LAN));
                Assert.That(PairingClient.GuessDeployment(S2BaseURL.Parse("https://[fe80::1]/pairing/")),        Is.EqualTo(Deployment.LAN));
            });

        }

        #endregion

        #region GuessDeployment_DomainNames_AreWAN()

        [Test]
        [S2C("Pairing.Deployments")]
        public void GuessDeployment_DomainNames_AreWAN()
        {

            Assert.Multiple(() => {
                Assert.That(PairingClient.GuessDeployment(S2BaseURL.Parse("https://pairing.example.com/pairing/")),  Is.EqualTo(Deployment.WAN));
                Assert.That(PairingClient.GuessDeployment(S2BaseURL.Parse("https://example.com/")),                  Is.EqualTo(Deployment.WAN));
                Assert.That(PairingClient.GuessDeployment(S2BaseURL.Parse("https://local.example.org/pairing/")),    Is.EqualTo(Deployment.WAN), "only the '.local' top level domain is LAN");
            });

        }

        #endregion

        #region Options_Defaults_EqualTheNormativeValues()

        [Test]
        [S2C("Pairing.Interruption")]
        [S2C("Pairing.HmacHashingAlgorithm")]
        public void Options_Defaults_EqualTheNormativeValues()
        {

            var options = new PairingClientOptions();

            Assert.Multiple(() => {
                Assert.That(options.PairingAttemptTimeout,            Is.EqualTo(S2ConnectDefaults.PairingAttemptTimeout));
                Assert.That(options.PairingAttemptTimeout,            Is.EqualTo(TimeSpan.FromSeconds(15)));
                Assert.That(options.SupportedHmacHashingAlgorithms,   Is.EqualTo(new[] { HmacHashingAlgorithm.SHA256 }));
                Assert.That(options.SupportedAPIVersions,             Is.EqualTo(new[] { "v1" }));
                Assert.That(options.MaxServiceUnavailableRetries,     Is.EqualTo(3));
                Assert.That(options.ServiceUnavailableRetryDelay,     Is.EqualTo(TimeSpan.FromSeconds(1)));
                Assert.That(options.RequestPairingTimeout,            Is.EqualTo(TimeSpan.FromSeconds(15)));
                Assert.That(options.RequestTimeout,                   Is.EqualTo(TimeSpan.FromSeconds(10)));
                Assert.That(options.DefaultServerDeployment,          Is.Null);
                Assert.That(options.AcceptSelfSignedCertificates,     Is.True);
                Assert.That(options.ParserOptions,                    Is.SameAs(S2ParserOptions.Default));
                Assert.That(options.Validate,                         Throws.Nothing);
                Assert.That(PairingClientOptions.Default.Validate,    Throws.Nothing);
            });

        }

        #endregion

        #region Options_Validate_RejectsInvalidValues()

        [Test]
        [S2C("Pairing.HmacHashingAlgorithm")]
        [S2C("Pairing.Interruption")]
        public void Options_Validate_RejectsInvalidValues()
        {

            var valid = PairingClientFixture.DefaultOptions();

            Assert.Multiple(() => {

                Assert.That(valid.Validate, Throws.Nothing);

                Assert.That(() => (valid with { SupportedHmacHashingAlgorithms  = [ HmacHashingAlgorithm.Parse("SHA512") ] }).Validate(),  Throws.ArgumentException, "SHA256 must be offered");
                Assert.That(() => (valid with { SupportedHmacHashingAlgorithms  = [] }).Validate(),                                        Throws.ArgumentException);
                Assert.That(() => (valid with { SupportedAPIVersions            = [] }).Validate(),                                        Throws.ArgumentException);

                Assert.That(() => (valid with { PairingAttemptTimeout           = TimeSpan.Zero }).Validate(),                             Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => (valid with { PairingAttemptTimeout           = TimeSpan.FromSeconds(-1) }).Validate(),                  Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => (valid with { RequestPairingTimeout           = TimeSpan.Zero }).Validate(),                             Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => (valid with { RequestTimeout                  = TimeSpan.Zero }).Validate(),                             Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => (valid with { ServiceUnavailableRetryDelay    = TimeSpan.FromMilliseconds(-1) }).Validate(),             Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => (valid with { MaxServiceUnavailableRetries    = -1 }).Validate(),                                        Throws.TypeOf<ArgumentOutOfRangeException>());

                Assert.That(() => (valid with { ServiceUnavailableRetryDelay    = TimeSpan.Zero }).Validate(),                             Throws.Nothing, "an immediate retry is allowed");
                Assert.That(() => (valid with { MaxServiceUnavailableRetries    = 0 }).Validate(),                                         Throws.Nothing, "no retries at all is allowed");
                Assert.That(() => (valid with { DefaultServerDeployment         = Deployment.LAN }).Validate(),                            Throws.Nothing);

            });

        }

        #endregion

    }

}

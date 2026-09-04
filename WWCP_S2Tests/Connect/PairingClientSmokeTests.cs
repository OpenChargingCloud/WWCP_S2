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
    /// The pairing client against the Phase 6 pairing server over real HTTP: the two branches
    /// of the pairing interaction end with consistent pairings on both sides.
    /// </summary>
    [TestFixture]
    public sealed class PairingClientSmokeTests
    {

        #region VersionIndex_SelectsV1()

        [Test]
        [S2C("Versioning.3")]
        public async Task VersionIndex_SelectsV1()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var versions = await client.Client.GetVersionsAsync();

            Assert.Multiple(() => {
                Assert.That(versions.IsSuccess,                 Is.True, versions.Description);
                Assert.That(versions.Value,                     Is.EqualTo(new[] { "v1" }));
                Assert.That(client.Client.SelectedAPIVersion,   Is.EqualTo("v1"));
            });

        }

        #endregion

        #region Pair_CEMClient_RMServer_LAN_ClientBecomesCommunicationServer()

        [Test]
        [S2C("Pairing.6B")]
        [S2C("Pairing.10")]
        public async Task Pair_CEMClient_RMServer_LAN_ClientBecomesCommunicationServer()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var token   = PairingToken.Parse("ABCD2345");
            var rm      = server.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(token);

            var cem     = client.AddNode(EnergyManagementRole.CEM);

            var result  = await client.Client.PairAsync(cem, token);

            Assert.That(result.IsSuccess, Is.True, result.ToString());

            var clientPairing  = await client.Store.GetPairingAsync(cem.Id, rm.Id);
            var serverPairing  = await server.Store.GetPairingAsync(rm.Id, cem.Id);

            Assert.Multiple(() => {
                Assert.That(clientPairing,                             Is.Not.Null);
                Assert.That(serverPairing,                             Is.Not.Null);
                Assert.That(clientPairing!.LocalCommunicationRole,     Is.EqualTo(CommunicationRole.CommunicationServer));
                Assert.That(serverPairing!.LocalCommunicationRole,     Is.EqualTo(CommunicationRole.CommunicationClient));
                Assert.That(serverPairing.AccessToken,                 Is.EqualTo(clientPairing.AccessToken));
                Assert.That(serverPairing.InitiateSessionUrl,          Is.EqualTo(client.Endpoint.SessionInitiationUrl));
                Assert.That(serverPairing.CertificateFingerprints!["SHA256"], Is.EqualTo(PairingClientFixture.CAFingerprint));
                Assert.That(clientPairing.InitiateSessionUrl,          Is.Null);
                Assert.That(clientPairing.RemoteNodeId,                Is.EqualTo(rm.Id));
                Assert.That(clientPairing.RemoteEndpointDescription.Deployment, Is.EqualTo(Deployment.LAN));
                Assert.That(result.Pairing,                            Is.EqualTo(clientPairing));
                Assert.That(client.Results,                            Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region Pair_RMClient_CEMServer_LAN_ClientBecomesCommunicationClient()

        [Test]
        [S2C("Pairing.6A")]
        [S2C("Pairing.8A")]
        public async Task Pair_RMClient_CEMServer_LAN_ClientBecomesCommunicationClient()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var cem     = server.AddNode(EnergyManagementRole.CEM);
            var code    = cem.IssueDynamicPairingToken();

            var rm      = client.AddNode(EnergyManagementRole.RM);

            var result  = await client.Client.PairAsync(rm, code.PairingToken);

            Assert.That(result.IsSuccess, Is.True, result.ToString());

            var clientPairing  = await client.Store.GetPairingAsync(rm.Id, cem.Id);
            var serverPairing  = await server.Store.GetPairingAsync(cem.Id, rm.Id);

            Assert.Multiple(() => {
                Assert.That(clientPairing,                            Is.Not.Null);
                Assert.That(serverPairing,                            Is.Not.Null);
                Assert.That(clientPairing!.LocalCommunicationRole,    Is.EqualTo(CommunicationRole.CommunicationClient));
                Assert.That(serverPairing!.LocalCommunicationRole,    Is.EqualTo(CommunicationRole.CommunicationServer));
                Assert.That(clientPairing.AccessToken,                Is.EqualTo(serverPairing.AccessToken));
                Assert.That(clientPairing.InitiateSessionUrl,         Is.EqualTo(server.SessionInitiationUrl));
                Assert.That(cem.HasValidPairingToken,                 Is.False, "the dynamic token of the server node is consumed");
            });

        }

        #endregion

        #region Pair_WrongToken_ReturnsForbidden()

        [Test]
        [S2C("Pairing.7B")]
        public async Task Pair_WrongToken_ReturnsForbidden()
        {

            await using var server  = await PairingServerFixture.CreateAsync();
            await using var client  = PairingClientFixture.Create(server);

            var rm  = server.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(PairingToken.Parse("RIGHT2345"));

            var cem     = client.AddNode(EnergyManagementRole.CEM);
            var result  = await client.Client.PairAsync(cem, PairingToken.Parse("WRONG2345"));

            Assert.Multiple(() => {
                Assert.That(result.Outcome,     Is.EqualTo(PairingClientOutcome.ChallengeResponseMismatch), result.ToString());
                Assert.That(result.IsSuccess,   Is.False);
                Assert.That(server.CompletedAttempts.Select(attempt => attempt.Failure), Does.Contain(PairingFailure.ClientReportedFailure));
            });

        }

        #endregion

    }

}

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
    /// The pairing server over real HTTP with a plain HttpClient: version index, routing
    /// and the complete pairing interaction (PLAN.md Phase 6a).
    /// </summary>
    [TestFixture]
    public sealed class PairingServerHTTPTests
    {

        #region VersionIndex_ReturnsSupportedVersions()

        [Test]
        [S2C("Versioning.2")]
        public async Task VersionIndex_ReturnsSupportedVersions()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var result = await fixture.GetAsync("");

            Assert.Multiple(() => {
                Assert.That(result.Status,       Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.ContentType,  Does.StartWith("application/json"));
                Assert.That(result.Array.Select(v => v.Value<String>()), Is.EqualTo(new[] { "v1" }));
                Assert.That(result.CacheControl, Does.Contain("no-store"));
            });

        }

        #endregion

        #region UnknownPath_Returns404()

        [Test]
        public async Task UnknownPath_Returns404()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var result = await fixture.GetAsync("v1/unknown");

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.NotFound));

        }

        #endregion

        #region RequestPairing_LAN_RMServer_CEMClient_Returns200WithValidChallengeResponse()

        [Test]
        [S2C("Pairing.1")]
        [S2C("Pairing.3")]
        public async Task RequestPairing_LAN_RMServer_CEMClient_Returns200WithValidChallengeResponse()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client  = new TestPairingClient(EnergyManagementRole.CEM);
            var rm      = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var result  = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK), result.Body);

            Assert.That(RequestPairingResponse.TryParse(result.Object, out var response, out var error), Is.True, error);

            Assert.Multiple(() => {
                Assert.That(response!.ServerNodeDescription.Id,                 Is.EqualTo(rm.Id));
                Assert.That(response.ServerEndpointDescription.Deployment,      Is.EqualTo(Deployment.LAN));
                Assert.That(response.SelectedHmacHashingAlgorithm,              Is.EqualTo(HmacHashingAlgorithm.SHA256));
                Assert.That(response.ClientHmacChallengeResponse.ConstantTimeEquals(client.ExpectedClientChallengeResponse(fixture)), Is.True);
                Assert.That(response.ServerHmacChallenge.Length,                Is.GreaterThanOrEqualTo(32));
                Assert.That(response.PairingAttemptId.Length,                   Is.GreaterThanOrEqualTo(32));
                Assert.That(fixture.API.ActiveAttempts,                         Has.Count.EqualTo(1));
                Assert.That(fixture.API.ActiveAttempts[0].ServerCommunicationRole, Is.EqualTo(CommunicationRole.CommunicationClient));
            });

        }

        #endregion

        #region RequestPairing_InvalidJSON_Returns400ParsingError()

        [Test]
        [S2C("Pairing.1.ParsingError")]
        public async Task RequestPairing_InvalidJSON_Returns400ParsingError()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();
            fixture.AddNode(EnergyManagementRole.RM);

            var result = await fixture.PostAsync("v1/requestPairing", RawBody: "{ not json");

            Assert.Multiple(() => {
                Assert.That(result.Status,        Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(result.ErrorMessage,  Is.EqualTo("ParsingError"));
            });

        }

        #endregion

    }

}

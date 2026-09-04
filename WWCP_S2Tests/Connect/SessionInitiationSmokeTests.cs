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
    /// Session initiation end to end: the communication client rotates the access token with
    /// the communication server and opens the S2 WebSocket session; unpairing in both directions.
    /// </summary>
    [TestFixture]
    public sealed class SessionInitiationSmokeTests
    {

        #region VersionIndex_ReturnsV1()

        [Test]
        [S2C("SessionInitiation.0")]
        public async Task VersionIndex_ReturnsV1()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var result = await fixture.GetAsync("");

            Assert.Multiple(() => {
                Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.Array.Select(v => v.Value<String>()), Is.EqualTo(new[] { "v1" }));
            });

        }

        #endregion

        #region Connect_RotatesTheAccessToken_AndOpensTheSession()

        [Test]
        [S2C("SessionInitiation.1")]
        [S2C("SessionInitiation.6")]
        [S2C("SessionInitiation.8")]
        public async Task Connect_RotatesTheAccessToken_AndOpensTheSession()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var oldToken  = (await fixture.ClientStore.GetPairingAsync(fixture.RM.Id, fixture.CEM.Id))!.AccessToken;

            var connect   = await fixture.Client.ConnectAsync(fixture.RM, fixture.CEM.Id);

            Assert.That(connect.IsSuccess, Is.True, connect.Initiation.ToString());

            await using var session = connect.Session!;

            var clientPairing  = await fixture.ClientStore.GetPairingAsync(fixture.RM.Id,  fixture.CEM.Id);
            var serverPairing  = await fixture.ServerStore.GetPairingAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.Multiple(() => {
                Assert.That(session.Session.IsConnected,                              Is.True);
                Assert.That(session.Session.NegotiatedVersion,                        Is.EqualTo(Version.S2JSONVersion));
                Assert.That(connect.Initiation.SelectedCommunicationProtocol,        Is.EqualTo(CommunicationProtocol.WebSocket));
                Assert.That(clientPairing!.AccessToken.Equals(oldToken),             Is.False, "the client activated the new token");
                Assert.That(serverPairing!.AccessToken.Equals(clientPairing.AccessToken), Is.True, "both sides hold the same new token");
                Assert.That(fixture.ClientStore.PendingCount,                         Is.EqualTo(0));
                Assert.That(fixture.ServerStore.PendingCount,                         Is.EqualTo(0));
                Assert.That(fixture.ClientResults,                                    Has.Count.EqualTo(1));
            });

            // The WebSocket server started a session with the identity of the pairing.
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (fixture.ServerSessionStarts.Count == 0 && DateTime.UtcNow < deadline)
                await Task.Delay(20);

            Assert.That(fixture.ServerSessionStarts, Has.Count.EqualTo(1));
            Assert.That(fixture.ServerSessionStarts[0].Identity, Is.InstanceOf<S2ConnectSessionIdentity>());
            Assert.That(((S2ConnectSessionIdentity) fixture.ServerSessionStarts[0].Identity!).RemoteNodeId, Is.EqualTo(fixture.RM.Id));

        }

        #endregion

        #region UnpairByServer_ThenInitiateSession_AnswersNoLongerPaired()

        [Test]
        [S2C("Unpairing.ByCommunicationServer")]
        public async Task UnpairByServer_ThenInitiateSession_AnswersNoLongerPaired()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var removed = await fixture.API.UnpairLocallyAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.That(removed, Is.Not.Null);

            var result = await fixture.Client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,               Is.EqualTo(SessionInitiationOutcome.NoLongerPaired), result.ToString());
                Assert.That(result.Error?.ErrorMessage,   Is.EqualTo(CommunicationDetailsError.NoLongerPaired));
                Assert.That(result.Retryable,             Is.False);
            });

            Assert.That(await fixture.ClientStore.GetPairingAsync(fixture.RM.Id, fixture.CEM.Id), Is.Null, "the client removed its security material");
            Assert.That(await fixture.ClientStore.GetUnpairedAtAsync(fixture.RM.Id, fixture.CEM.Id), Is.Not.Null);

        }

        #endregion

        #region UnpairByClient_RemovesThePairingOnBothSides()

        [Test]
        [S2C("Unpairing.ByCommunicationClient")]
        public async Task UnpairByClient_RemovesThePairingOnBothSides()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var result = await fixture.Client.UnpairAsync(fixture.RM.Id, fixture.CEM.Id);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,     Is.EqualTo(SessionInitiationOutcome.Success), result.ToString());
                Assert.That(result.StatusCode?.Code, Is.EqualTo(204));
            });

            Assert.Multiple(async () => {
                Assert.That(await fixture.ServerStore.GetPairingAsync(fixture.CEM.Id, fixture.RM.Id),    Is.Null);
                Assert.That(await fixture.ServerStore.GetUnpairedAtAsync(fixture.CEM.Id, fixture.RM.Id), Is.Not.Null);
                Assert.That(await fixture.ClientStore.GetPairingAsync(fixture.RM.Id, fixture.CEM.Id),    Is.Null);
                Assert.That(fixture.UnpairedAtServer,                                                   Has.Count.EqualTo(1));
            });

            // A second unpairing is answered with 401 and reported as already unpaired.
            var again = await fixture.Client.UnpairAsync(fixture.RM.Id, fixture.CEM.Id);

            Assert.That(again.Outcome, Is.EqualTo(SessionInitiationOutcome.AlreadyUnpaired), again.ToString());

        }

        #endregion

    }

}

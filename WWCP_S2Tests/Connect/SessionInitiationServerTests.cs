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
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.WebSockets;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The session initiation server over real HTTP with a plain HttpClient: the version index,
    /// every check of initiateSession in the order of the specification, the activation of
    /// pending access tokens via confirmAccessToken, unpairing in both directions, store
    /// failures, maintenance and shutdown (S2 Connect 1.0.0, "Session initiation" and
    /// "Unpairing process"). The WebSocket connection itself is covered by
    /// <see cref="SessionInitiationSmokeTests"/> and is not repeated here.
    /// </summary>
    [TestFixture]
    public sealed class SessionInitiationServerTests
    {

        #region Data

        /// <summary>
        /// The communication details carry a "ws://" URL in the tests, which the parser accepts only when told so.
        /// </summary>
        private static readonly S2ParserOptions  InsecureParserOptions  = new () { AllowInsecureURLs = true };

        #endregion


        #region (private static) Helpers

        /// <summary>
        /// POST v1/initiateSession with the given bearer token (default: the well-formed request of the fixture).
        /// </summary>
        private static Task<HTTPResult> InitiateSessionAsync(SessionInitiationFixture  Fixture,
                                                            AccessToken               Bearer,
                                                            InitiateSessionRequest?   Request   = null)

            => Fixture.PostAsync("v1/initiateSession",
                                 (Request ?? Fixture.InitiateSessionRequest()).ToJSON(),
                                 Bearer.Value);

        /// <summary>
        /// POST v1/confirmAccessToken with the given (pending) bearer token; the operation has no request body.
        /// </summary>
        private static Task<HTTPResult> ConfirmAccessTokenAsync(SessionInitiationFixture  Fixture,
                                                               AccessToken               PendingBearer)

            => Fixture.PostAsync("v1/confirmAccessToken",
                                 Bearer: PendingBearer.Value);

        /// <summary>
        /// POST v1/unpair with the given bearer token (default: RM unpairs from CEM).
        /// </summary>
        private static Task<HTTPResult> UnpairAsync(SessionInitiationFixture  Fixture,
                                                   AccessToken               Bearer,
                                                   UnpairRequest?            Request   = null)

            => Fixture.PostAsync("v1/unpair",
                                 (Request ?? new UnpairRequest(Fixture.RM.Id, Fixture.CEM.Id)).ToJSON(),
                                 Bearer.Value);

        /// <summary>
        /// A well-formed initiateSession request of the RM offering the given S2 message versions.
        /// </summary>
        private static InitiateSessionRequest RequestWithVersions(SessionInitiationFixture  Fixture,
                                                                  params String[]           Versions)

            => new (Fixture.RM.Id,
                    Fixture.CEM.Id,
                    Versions,
                    Fixture.RM.SupportedCommunicationProtocols);

        /// <summary>
        /// Assert a 200 response and parse its initiateSession response body.
        /// </summary>
        private static InitiateSessionResponse ParseInitiateSessionResponse(HTTPResult Result)
        {

            Assert.That(Result.Status, Is.EqualTo(HttpStatusCode.OK), Result.Body);
            Assert.That(InitiateSessionResponse.TryParse(Result.Object, out var response, out var error), Is.True, error);

            return response!;

        }

        /// <summary>
        /// Assert a 200 response and parse its communication details, which must be WebSocket communication details.
        /// </summary>
        private static WebSocketCommunicationDetails ParseCommunicationDetails(HTTPResult Result)
        {

            Assert.That(Result.Status, Is.EqualTo(HttpStatusCode.OK), Result.Body);
            Assert.That(CommunicationDetails.TryParse(Result.Object, out var details, out var error, InsecureParserOptions), Is.True, error);
            Assert.That(details, Is.InstanceOf<WebSocketCommunicationDetails>());

            return (WebSocketCommunicationDetails) details!;

        }

        /// <summary>
        /// The pairing of the CEM (communication server) with the RM as stored at the server.
        /// </summary>
        private static Task<Pairing?> ServerPairingAsync(SessionInitiationFixture Fixture)
            => Fixture.ServerStore.GetPairingAsync(Fixture.CEM.Id, Fixture.RM.Id).AsTask();

        #endregion


        // Version index and routing

        #region VersionIndex_ReturnsV1_AndIsNotCached()

        [Test]
        [S2C("SessionInitiation.0")]
        public async Task VersionIndex_ReturnsV1_AndIsNotCached()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var result = await fixture.GetAsync("");

            Assert.Multiple(() => {
                Assert.That(result.Status,        Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.ContentType,   Does.StartWith("application/json"));
                Assert.That(result.Array.Select(v => v.Value<String>()), Is.EqualTo(new[] { Version.S2ConnectAPIVersion }));
                Assert.That(result.CacheControl,  Does.Contain("no-store"));
            });

        }

        #endregion

        #region UnknownPath_Returns404()

        [Test]
        public async Task UnknownPath_Returns404()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var result = await fixture.GetAsync("v1/unknown");

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.NotFound));

        }

        #endregion


        // initiateSession

        #region InitiateSession_IssuesAPendingToken_AndKeepsTheActiveOne()

        [Test]
        [S2C("SessionInitiation.1")]
        [S2C("SessionInitiation.2")]
        [S2C("SessionInitiation.3")]
        public async Task InitiateSession_IssuesAPendingToken_AndKeepsTheActiveOne()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();
            var initiated    = new List<(Pairing Pairing, InitiateSessionResponse Response)>();

            fixture.API.OnSessionInitiated += (_, _, initiatedPairing, initiatedResponse) => {
                                                  lock (initiated)
                                                      initiated.Add((initiatedPairing, initiatedResponse));
                                                  return Task.CompletedTask;
                                              };

            var result    = await InitiateSessionAsync(fixture, activeToken);
            var response  = ParseInitiateSessionResponse(result);
            var pairing   = await ServerPairingAsync(fixture);
            var pending   = await fixture.ServerStore.FindPendingAccessTokenAsync(response.AccessToken);

            Assert.Multiple(() => {
                Assert.That(result.ContentType,                          Does.StartWith("application/json"));
                Assert.That(result.CacheControl,                         Does.Contain("no-store"), "the response carries a secret and must not be cached");
                Assert.That(response.SelectedCommunicationProtocol,      Is.EqualTo(CommunicationProtocol.WebSocket));
                Assert.That(response.SelectedS2MessageVersion,           Is.EqualTo(Version.S2JSONVersion));
                Assert.That(response.AccessToken.Length,                 Is.GreaterThanOrEqualTo(S2ConnectDefaults.MinAccessTokenLength));
                Assert.That(response.AccessToken.Equals(activeToken),    Is.False, "the pending token is a new token");
                Assert.That(response.ServerNodeDescription,              Is.Null, "descriptions are only sent when configured");
                Assert.That(response.ServerEndpointDescription,          Is.Null);
                Assert.That(fixture.ServerStore.PendingCount,            Is.EqualTo(1));
                Assert.That(pending,                                     Is.Not.Null, "the pending token is persisted before it is sent");
                Assert.That(pending!.LocalNodeId,                        Is.EqualTo(fixture.CEM.Id));
                Assert.That(pending!.RemoteNodeId,                       Is.EqualTo(fixture.RM.Id));
                Assert.That(pending!.SelectedCommunicationProtocol,      Is.EqualTo(CommunicationProtocol.WebSocket));
                Assert.That(pending!.SelectedS2MessageVersion,           Is.EqualTo(Version.S2JSONVersion));
                Assert.That(pairing!.AccessToken.Equals(activeToken),    Is.True, "the active token stays until the pending one is confirmed");
                Assert.That(initiated,                                   Has.Count.EqualTo(1));
                Assert.That(initiated[0].Response,                       Is.EqualTo(response));
                Assert.That(initiated[0].Pairing.RemoteNodeId,           Is.EqualTo(fixture.RM.Id));
            });

        }

        #endregion

        #region InitiateSession_Twice_KeepsBothPendingTokens_UntilOneIsConfirmed()

        [Test]
        [S2C("SessionInitiation.1")]
        [S2C("SessionInitiation.6")]
        public async Task InitiateSession_Twice_KeepsBothPendingTokens_UntilOneIsConfirmed()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();
            var first        = ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, activeToken)).AccessToken;
            var second       = ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, activeToken)).AccessToken;
            var candidates   = await fixture.ServerStore.GetAccessTokenCandidatesAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.Multiple(() => {
                Assert.That(first.Equals(second),                Is.False, "every initiateSession issues a new pending token");
                Assert.That(fixture.ServerStore.PendingCount,    Is.EqualTo(2));
                Assert.That(candidates,                          Is.EquivalentTo(new[] { activeToken, first, second }));
                Assert.That(candidates[0],                       Is.EqualTo(activeToken), "the active token comes first");
            });

            // Confirming one pending token activates it and invalidates every other pending token of the pair.
            ParseCommunicationDetails(await ConfirmAccessTokenAsync(fixture, first));

            var stale    = await ConfirmAccessTokenAsync(fixture, second);
            var pairing  = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(fixture.ServerStore.PendingCount,    Is.EqualTo(0));
                Assert.That(pairing!.AccessToken.Equals(first),  Is.True);
                Assert.That(stale.Status,                        Is.EqualTo(HttpStatusCode.Unauthorized), "the other pending token is gone");
            });

        }

        #endregion

        #region InitiateSession_WithoutBearer_Returns401WithWWWAuthenticate()

        [Test]
        [S2C("SessionInitiation.1.Unauthorized")]
        public async Task InitiateSession_WithoutBearer_Returns401WithWWWAuthenticate()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var result = await fixture.PostAsync("v1/initiateSession", fixture.InitiateSessionRequest().ToJSON());

            Assert.Multiple(() => {
                Assert.That(result.Status,                      Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(result.WWWAuthenticate,             Does.Contain("Bearer"));
                Assert.That(result.CacheControl,                Does.Contain("no-store"));
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_WithAMalformedBearer_Returns401()

        [Test]
        [S2C("SessionInitiation.1.Unauthorized")]
        public async Task InitiateSession_WithAMalformedBearer_Returns401()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var result = await fixture.PostAsync("v1/initiateSession", fixture.InitiateSessionRequest().ToJSON(), "not-base64!!");

            Assert.Multiple(() => {
                Assert.That(result.Status,                      Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(result.WWWAuthenticate,             Does.Contain("Bearer"));
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_WithAWrongToken_Returns401()

        [Test]
        [S2C("SessionInitiation.1.Unauthorized")]
        public async Task InitiateSession_WithAWrongToken_Returns401()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            await fixture.PairAsync();

            var result = await InitiateSessionAsync(fixture, TokenGenerator.NewAccessToken());

            Assert.Multiple(() => {
                Assert.That(result.Status,                      Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(result.WWWAuthenticate,             Does.Contain("Bearer"));
                Assert.That(result.ErrorMessage,                Is.Null, "a 401 carries no CommunicationDetailsErrorMessage");
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_AfterLocalUnpairing_ReturnsNoLongerPaired_EvenWithAWrongToken()

        [Test]
        [S2C("SessionInitiation.1.NoLongerPaired")]
        [S2C("Unpairing.ByCommunicationServer")]
        public async Task InitiateSession_AfterLocalUnpairing_ReturnsNoLongerPaired_EvenWithAWrongToken()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();
            var removed      = await fixture.API.UnpairLocallyAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.That(removed, Is.Not.Null);

            // The tombstone is checked before the 401 checks: a wrong token gets NoLongerPaired, not 401.
            var wrong  = await InitiateSessionAsync(fixture, TokenGenerator.NewAccessToken());
            var right  = await InitiateSessionAsync(fixture, activeToken);

            Assert.Multiple(() => {
                Assert.That(wrong.Status,                       Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(wrong.ErrorMessage,                 Is.EqualTo("NoLongerPaired"));
                Assert.That(right.Status,                       Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(right.ErrorMessage,                 Is.EqualTo("NoLongerPaired"));
                Assert.That(right.AdditionalInfo,               Does.Contain("unpaired at"));
                Assert.That(right.CacheControl,                 Does.Contain("no-store"));
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
                Assert.That(fixture.UnpairedAtServer,           Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region InitiateSession_ForAnUnknownServerNode_Returns401()

        [Test]
        [S2C("SessionInitiation.1.Unauthorized")]
        public async Task InitiateSession_ForAnUnknownServerNode_Returns401()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();

            var unknown      = await InitiateSessionAsync(fixture,
                                                          activeToken,
                                                          new InitiateSessionRequest(fixture.RM.Id,
                                                                                     Node_Id.NewRandom,
                                                                                     fixture.RM.SupportedS2MessageVersions,
                                                                                     fixture.RM.SupportedCommunicationProtocols));

            // A pairing whose local node is not hosted by the endpoint is rejected as well.
            var ghost        = Node_Id.NewRandom;

            await fixture.ServerStore.AddOrReplacePairingAsync(new Pairing(ghost,
                                                                           fixture.RM.Description,
                                                                           fixture.ClientEndpoint.Description,
                                                                           CommunicationRole.CommunicationServer,
                                                                           activeToken,
                                                                           DateTimeOffset.UtcNow));

            var notHosted    = await InitiateSessionAsync(fixture,
                                                          activeToken,
                                                          new InitiateSessionRequest(fixture.RM.Id,
                                                                                     ghost,
                                                                                     fixture.RM.SupportedS2MessageVersions,
                                                                                     fixture.RM.SupportedCommunicationProtocols));

            Assert.Multiple(() => {
                Assert.That(unknown.Status,                     Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(notHosted.Status,                   Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_FromAnUnknownClientNode_Returns401()

        [Test]
        [S2C("SessionInitiation.1.Unauthorized")]
        public async Task InitiateSession_FromAnUnknownClientNode_Returns401()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();

            var result       = await InitiateSessionAsync(fixture,
                                                          activeToken,
                                                          new InitiateSessionRequest(Node_Id.NewRandom,
                                                                                     fixture.CEM.Id,
                                                                                     fixture.RM.SupportedS2MessageVersions,
                                                                                     fixture.RM.SupportedCommunicationProtocols));

            Assert.Multiple(() => {
                Assert.That(result.Status,                      Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(result.WWWAuthenticate,             Does.Contain("Bearer"));
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_WithInvalidJSON_Returns400ParsingError()

        [Test]
        [S2C("SessionInitiation.1.ParsingError")]
        public async Task InitiateSession_WithInvalidJSON_Returns400ParsingError()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();

            var notJSON      = await fixture.PostAsync("v1/initiateSession", Bearer: activeToken.Value, RawBody: "{ not json");
            var array        = await fixture.PostAsync("v1/initiateSession", Bearer: activeToken.Value, RawBody: "[]");
            var empty        = await fixture.PostAsync("v1/initiateSession", Bearer: activeToken.Value);

            Assert.Multiple(() => {
                Assert.That(notJSON.Status,                     Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(notJSON.ErrorMessage,               Is.EqualTo("ParsingError"));
                Assert.That(notJSON.AdditionalInfo,             Is.Not.Null.And.Not.Empty, "error details are included by default");
                Assert.That(array.Status,                       Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(array.ErrorMessage,                 Is.EqualTo("ParsingError"));
                Assert.That(empty.Status,                       Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(empty.ErrorMessage,                 Is.EqualTo("ParsingError"));
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_WithMissingMandatoryFields_Returns400ParsingError()

        [Test]
        [S2C("SessionInitiation.1.ParsingError")]
        public async Task InitiateSession_WithMissingMandatoryFields_Returns400ParsingError()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();

            var incomplete   = new JObject(
                                   new JProperty("clientNodeId",  fixture.RM.Id. ToString()),
                                   new JProperty("serverNodeId",  fixture.CEM.Id.ToString())
                               );

            var noVersions   = fixture.InitiateSessionRequest().ToJSON();
            noVersions["supportedS2MessageVersions"] = new JArray();

            var missing      = await fixture.PostAsync("v1/initiateSession", incomplete, activeToken.Value);
            var emptyList    = await fixture.PostAsync("v1/initiateSession", noVersions, activeToken.Value);

            Assert.Multiple(() => {
                Assert.That(missing.Status,                     Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(missing.ErrorMessage,               Is.EqualTo("ParsingError"));
                Assert.That(missing.AdditionalInfo,             Does.Contain("supportedS2MessageVersions"));
                Assert.That(emptyList.Status,                   Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(emptyList.ErrorMessage,             Is.EqualTo("ParsingError"), "an empty list of versions makes a negotiation impossible");
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_WithAWrongContentType_Returns400ParsingError()

        [Test]
        [S2C("SessionInitiation.1.ParsingError")]
        public async Task InitiateSession_WithAWrongContentType_Returns400ParsingError()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();

            var result       = await fixture.PostAsync("v1/initiateSession",
                                                       fixture.InitiateSessionRequest().ToJSON(),
                                                       activeToken.Value,
                                                       ContentType: "text/plain");

            Assert.Multiple(() => {
                Assert.That(result.Status,                      Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(result.ErrorMessage,                Is.EqualTo("ParsingError"));
                Assert.That(result.AdditionalInfo,              Does.Contain("application/json"));
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_WithoutACommonS2MessageVersion_Returns400IncompatibleS2MessageVersions()

        [Test]
        [S2C("SessionInitiation.1.IncompatibleS2MessageVersions")]
        public async Task InitiateSession_WithoutACommonS2MessageVersion_Returns400IncompatibleS2MessageVersions()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();
            var result       = await InitiateSessionAsync(fixture, activeToken, RequestWithVersions(fixture, "v9.9.9"));

            Assert.Multiple(() => {
                Assert.That(result.Status,                      Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(result.ErrorMessage,                Is.EqualTo("IncompatibleS2MessageVersions"));
                Assert.That(result.AdditionalInfo,              Does.Contain(Version.S2JSONVersion), "the versions of the node are named");
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0), "no pending token is issued for a failed negotiation");
            });

            // Offering a supported version among others succeeds.
            ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, activeToken, RequestWithVersions(fixture, "v9.9.9", Version.S2JSONVersion)));

        }

        #endregion

        #region InitiateSession_WithoutACommonCommunicationProtocol_Returns400IncompatibleCommunicationProtocols()

        [Test]
        [S2C("SessionInitiation.1.IncompatibleCommunicationProtocols")]
        [S2C("SessionInitiation.1.ParsingError")]
        public async Task InitiateSession_WithoutACommonCommunicationProtocol_Returns400IncompatibleCommunicationProtocols()
        {

            IReadOnlyList<CommunicationProtocol> mqttOnly = [ CommunicationProtocol.Parse("MQTT") ];

            // S2 Connect 1.0 defines WebSocket only; a server tolerating unknown protocol names
            // finds no overlap with a client offering another protocol.
            await using var lenient = await SessionInitiationFixture.CreateAsync(
                                          ServerOptions: SessionInitiationFixture.DefaultServerOptions() with {
                                                             ParserOptions = new S2ParserOptions {
                                                                                 AllowInsecureURLs        = true,
                                                                                 RejectUnknownEnumValues  = false
                                                                             }
                                                         }
                                      );

            var lenientToken   = await lenient.PairAsync();
            var incompatible   = await InitiateSessionAsync(lenient,
                                                            lenientToken,
                                                            new InitiateSessionRequest(lenient.RM.Id,
                                                                                       lenient.CEM.Id,
                                                                                       lenient.RM.SupportedS2MessageVersions,
                                                                                       mqttOnly));

            // With the closed enumerations of the default options the unknown protocol is a parsing error.
            await using var strict = await SessionInitiationFixture.CreateAsync();

            var strictToken    = await strict.PairAsync();
            var parsingError   = await InitiateSessionAsync(strict,
                                                            strictToken,
                                                            new InitiateSessionRequest(strict.RM.Id,
                                                                                       strict.CEM.Id,
                                                                                       strict.RM.SupportedS2MessageVersions,
                                                                                       mqttOnly));

            Assert.Multiple(() => {
                Assert.That(incompatible.Status,                Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(incompatible.ErrorMessage,          Is.EqualTo("IncompatibleCommunicationProtocols"));
                Assert.That(incompatible.AdditionalInfo,        Does.Contain("WebSocket"), "the protocols of the node are named");
                Assert.That(lenient.ServerStore.PendingCount,   Is.EqualTo(0));
                Assert.That(parsingError.Status,                Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(parsingError.ErrorMessage,          Is.EqualTo("ParsingError"));
                Assert.That(strict.ServerStore.PendingCount,    Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_WhenTheNodeIsNotReady_Returns400Other()

        [Test]
        [S2C("SessionInitiation.1.Other")]
        public async Task InitiateSession_WhenTheNodeIsNotReady_Returns400Other()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();

            fixture.CEM.IsReadyForPairing = false;

            var notReady     = await InitiateSessionAsync(fixture, activeToken);

            Assert.Multiple(() => {
                Assert.That(notReady.Status,                    Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(notReady.ErrorMessage,              Is.EqualTo("Other"));
                Assert.That(notReady.AdditionalInfo,            Does.Contain("not ready"));
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
            });

            // The client may retry later: as soon as the node is ready again the same token works.
            fixture.CEM.IsReadyForPairing = true;

            ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, activeToken));

            Assert.That(fixture.ServerStore.PendingCount, Is.EqualTo(1));

        }

        #endregion

        #region InitiateSession_ChecksInTheOrderOfTheSpecification()

        [Test]
        [S2C("SessionInitiation.1.CheckOrder")]
        public async Task InitiateSession_ChecksInTheOrderOfTheSpecification()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();
            var unsupported  = RequestWithVersions(fixture, "v9.9.9");

            // The access token is checked before the negotiation: 401 wins over IncompatibleS2MessageVersions.
            var wrongToken   = await InitiateSessionAsync(fixture, TokenGenerator.NewAccessToken(), unsupported);

            // The negotiation is checked before the readiness of the node: IncompatibleS2MessageVersions wins over Other.
            fixture.CEM.IsReadyForPairing = false;

            var notReady     = await InitiateSessionAsync(fixture, activeToken, unsupported);

            // The tombstone is checked before everything else: NoLongerPaired wins over 401 and over every other 400.
            await fixture.API.UnpairLocallyAsync(fixture.CEM.Id, fixture.RM.Id);

            var unpaired     = await InitiateSessionAsync(fixture, TokenGenerator.NewAccessToken(), unsupported);

            Assert.Multiple(() => {
                Assert.That(wrongToken.Status,                  Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(notReady.Status,                    Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(notReady.ErrorMessage,              Is.EqualTo("IncompatibleS2MessageVersions"));
                Assert.That(unpaired.Status,                    Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(unpaired.ErrorMessage,              Is.EqualTo("NoLongerPaired"));
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_UpdatesTheClientDescriptions()

        [Test]
        [S2C("SessionInitiation.1.Descriptions")]
        public async Task InitiateSession_UpdatesTheClientDescriptions()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();
            var newNode      = new NodeDescription(fixture.RM.Id, "OtherBrand", "heat pump", "TestRM 2", EnergyManagementRole.RM, null, "Cellar");
            var newEndpoint  = new EndpointDescription("Renamed RM endpoint", null, Deployment.LAN);

            var request      = new InitiateSessionRequest(fixture.RM.Id,
                                                          fixture.CEM.Id,
                                                          fixture.RM.SupportedS2MessageVersions,
                                                          fixture.RM.SupportedCommunicationProtocols,
                                                          newNode,
                                                          newEndpoint);

            var before       = await ServerPairingAsync(fixture);
            var response     = ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, activeToken, request));
            var after        = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(before!.RemoteNodeDescription,               Is.Not.EqualTo(newNode));
                Assert.That(after!.RemoteNodeDescription,                Is.EqualTo(newNode), "the node description of the client was updated");
                Assert.That(after!.RemoteEndpointDescription,            Is.EqualTo(newEndpoint), "the endpoint description of the client was updated");
                Assert.That(after!.RemoteNodeId,                         Is.EqualTo(fixture.RM.Id));
                Assert.That(after!.AccessToken.Equals(activeToken),      Is.True, "the active token is untouched");
                Assert.That(after!.PairedAt,                             Is.EqualTo(before!.PairedAt));
                Assert.That(response.AccessToken.Equals(activeToken),    Is.False);
                Assert.That(fixture.ServerStore.PendingCount,            Is.EqualTo(1));
            });

            // Updating only the node description keeps the endpoint description.
            var newerNode    = new NodeDescription(fixture.RM.Id, "OtherBrand", "heat pump", "TestRM 3", EnergyManagementRole.RM);

            ParseInitiateSessionResponse(await InitiateSessionAsync(fixture,
                                                                    activeToken,
                                                                    new InitiateSessionRequest(fixture.RM.Id,
                                                                                               fixture.CEM.Id,
                                                                                               fixture.RM.SupportedS2MessageVersions,
                                                                                               fixture.RM.SupportedCommunicationProtocols,
                                                                                               newerNode)));

            var afterwards   = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(afterwards!.RemoteNodeDescription,           Is.EqualTo(newerNode));
                Assert.That(afterwards!.RemoteEndpointDescription,       Is.EqualTo(newEndpoint));
            });

        }

        #endregion

        #region InitiateSession_WithAForeignClientNodeDescription_Returns400ParsingError()

        [Test]
        [S2C("SessionInitiation.1.ParsingError")]
        public async Task InitiateSession_WithAForeignClientNodeDescription_Returns400ParsingError()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();
            var foreignNode  = new NodeDescription(Node_Id.NewRandom, "ACME", "heat pump", "TestRM 1", EnergyManagementRole.RM);

            var request      = new InitiateSessionRequest(fixture.RM.Id,
                                                          fixture.CEM.Id,
                                                          fixture.RM.SupportedS2MessageVersions,
                                                          fixture.RM.SupportedCommunicationProtocols,
                                                          foreignNode);

            var before       = await ServerPairingAsync(fixture);
            var result       = await InitiateSessionAsync(fixture, activeToken, request);
            var after        = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(result.Status,                      Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(result.ErrorMessage,                Is.EqualTo("ParsingError"));
                Assert.That(result.AdditionalInfo,              Does.Contain("clientNodeId"));
                Assert.That(after,                              Is.EqualTo(before), "the pairing is untouched");
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_SendsTheServerDescriptions_WhenConfigured()

        [Test]
        [S2C("SessionInitiation.3.Descriptions")]
        public async Task InitiateSession_SendsTheServerDescriptions_WhenConfigured()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                          ServerOptions: SessionInitiationFixture.DefaultServerOptions() with { AlwaysSendDescriptions = true }
                                      );

            var activeToken  = await fixture.PairAsync();
            var response     = ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, activeToken));

            Assert.Multiple(() => {
                Assert.That(response.ServerNodeDescription,          Is.EqualTo(fixture.CEM.Description));
                Assert.That(response.ServerEndpointDescription,      Is.EqualTo(fixture.ServerEndpoint.Description));
                Assert.That(response.ServerEndpointDescription!.Deployment, Is.EqualTo(Deployment.LAN));
            });

        }

        #endregion


        // confirmAccessToken

        #region ConfirmAccessToken_ActivatesThePendingToken_AndIssuesACommunicationToken()

        [Test]
        [S2C("SessionInitiation.5")]
        [S2C("SessionInitiation.6")]
        [S2C("SessionInitiation.7")]
        public async Task ConfirmAccessToken_ActivatesThePendingToken_AndIssuesACommunicationToken()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var oldToken   = await fixture.PairAsync();
            var activated  = new List<(Pairing Pairing, S2ConnectSessionIdentity Identity, CommunicationDetails Details)>();

            fixture.API.OnAccessTokenActivated += (_, _, activatedPairing, activatedIdentity, activatedDetails) => {
                                                      lock (activated)
                                                          activated.Add((activatedPairing, activatedIdentity, activatedDetails));
                                                      return Task.CompletedTask;
                                                  };

            var pending    = ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, oldToken)).AccessToken;
            var result     = await ConfirmAccessTokenAsync(fixture, pending);
            var details    = ParseCommunicationDetails(result);
            var pairing    = await ServerPairingAsync(fixture);

            // The WebSocket server shares the token store: the communication token is redeemable exactly once.
            var redeemed       = fixture.TokenStore.TryRedeem(details.WebsocketToken.Value, out var identity);
            var redeemedTwice  = fixture.TokenStore.TryRedeem(details.WebsocketToken.Value, out _);

            Assert.Multiple(() => {
                Assert.That(result.ContentType,                       Does.StartWith("application/json"));
                Assert.That(result.CacheControl,                      Does.Contain("no-store"), "the response carries a secret and must not be cached");
                Assert.That(result.Object["communicationProtocol"]?.Value<String>(), Is.EqualTo("WebSocket"));
                Assert.That(details.CommunicationProtocol,            Is.EqualTo(CommunicationProtocol.WebSocket));
                Assert.That(details.WebsocketUrl,                     Is.EqualTo(fixture.WebSocketUrl));
                Assert.That(details.WebsocketToken.Length,            Is.GreaterThanOrEqualTo(S2ConnectDefaults.MinCommunicationTokenLength));
                Assert.That(redeemed,                                 Is.True,  "the communication token is known to the WebSocket server");
                Assert.That(redeemedTwice,                            Is.False, "the communication token is single-use");
                Assert.That(identity,                                 Is.InstanceOf<S2ConnectSessionIdentity>());
                Assert.That(pairing!.AccessToken.Equals(pending),     Is.True,  "the pending token is now the active token");
                Assert.That(pairing!.AccessToken.Equals(oldToken),    Is.False, "the old token is gone");
                Assert.That(fixture.ServerStore.PendingCount,         Is.EqualTo(0));
                Assert.That(activated,                                Has.Count.EqualTo(1));
            });

            var sessionIdentity  = (S2ConnectSessionIdentity) identity!;
            var eventDetails     = (WebSocketCommunicationDetails) activated[0].Details;

            Assert.Multiple(() => {
                Assert.That(sessionIdentity.LocalNodeId,              Is.EqualTo(fixture.CEM.Id));
                Assert.That(sessionIdentity.RemoteNodeId,             Is.EqualTo(fixture.RM.Id));
                Assert.That(sessionIdentity.CommunicationProtocol,    Is.EqualTo(CommunicationProtocol.WebSocket));
                Assert.That(sessionIdentity.S2MessageVersion,         Is.EqualTo(Version.S2JSONVersion));
                Assert.That(sessionIdentity.Pairing.AccessToken.Equals(pending), Is.True, "the identity carries the updated pairing");
                Assert.That(activated[0].Identity,                    Is.EqualTo(sessionIdentity));
                Assert.That(activated[0].Pairing,                     Is.EqualTo(pairing));
                Assert.That(eventDetails.WebsocketToken,              Is.EqualTo(details.WebsocketToken));
            });

        }

        #endregion

        #region ConfirmAccessToken_RejectsUnknownActiveAndAlreadyConfirmedTokens()

        [Test]
        [S2C("SessionInitiation.6.Unauthorized")]
        public async Task ConfirmAccessToken_RejectsUnknownActiveAndAlreadyConfirmedTokens()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();

            var unknown      = await ConfirmAccessTokenAsync(fixture, TokenGenerator.NewAccessToken());
            var active       = await ConfirmAccessTokenAsync(fixture, activeToken);

            var pending      = ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, activeToken)).AccessToken;

            ParseCommunicationDetails(await ConfirmAccessTokenAsync(fixture, pending));

            var again        = await ConfirmAccessTokenAsync(fixture, pending);
            var pairing      = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(unknown.Status,                     Is.EqualTo(HttpStatusCode.Unauthorized), "an unknown token");
                Assert.That(unknown.WWWAuthenticate,            Does.Contain("Bearer"));
                Assert.That(active.Status,                      Is.EqualTo(HttpStatusCode.Unauthorized), "the active token is not a pending token");
                Assert.That(again.Status,                       Is.EqualTo(HttpStatusCode.Unauthorized), "a confirmed token is no longer pending");
                Assert.That(pairing!.AccessToken.Equals(pending), Is.True);
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
                Assert.That(fixture.TokenStore.Count,           Is.EqualTo(1), "exactly one communication token was issued");
            });

        }

        #endregion

        #region ConfirmAccessToken_WithoutOrWithAMalformedBearer_Returns401()

        [Test]
        [S2C("SessionInitiation.6.Unauthorized")]
        public async Task ConfirmAccessToken_WithoutOrWithAMalformedBearer_Returns401()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();
            var pending      = ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, activeToken)).AccessToken;

            var missing      = await fixture.PostAsync("v1/confirmAccessToken");
            var malformed    = await fixture.PostAsync("v1/confirmAccessToken", Bearer: "not-base64!!");

            Assert.Multiple(() => {
                Assert.That(missing.Status,                     Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(missing.WWWAuthenticate,            Does.Contain("Bearer"));
                Assert.That(malformed.Status,                   Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(malformed.WWWAuthenticate,          Does.Contain("Bearer"));
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(1), "the pending token is still waiting for its confirmation");
                Assert.That(fixture.TokenStore.Count,           Is.EqualTo(0));
            });

            // The pending token itself is still valid.
            ParseCommunicationDetails(await ConfirmAccessTokenAsync(fixture, pending));

        }

        #endregion

        #region ConfirmAccessToken_AfterThePendingTokenExpired_Returns401_AndRemovesIt()

        [Test]
        [S2C("SessionInitiation.6.Expired")]
        public async Task ConfirmAccessToken_AfterThePendingTokenExpired_Returns401_AndRemovesIt()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                          ServerOptions: SessionInitiationFixture.DefaultServerOptions() with { PendingAccessTokenLifetime = TimeSpan.FromMilliseconds(200) }
                                      );

            var oldToken  = await fixture.PairAsync();
            var pending   = ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, oldToken)).AccessToken;

            Assert.That(fixture.ServerStore.PendingCount, Is.EqualTo(1));

            await Task.Delay(400);

            var expired   = await ConfirmAccessTokenAsync(fixture, pending);
            var pairing   = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(expired.Status,                       Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(fixture.ServerStore.PendingCount,     Is.EqualTo(0), "the expired pending token was removed");
                Assert.That(pairing!.AccessToken.Equals(oldToken), Is.True, "the active token is untouched");
                Assert.That(fixture.TokenStore.Count,             Is.EqualTo(0), "no communication token was issued");
            });

            // The client falls back to the old token and starts over.
            var retry = ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, oldToken));

            Assert.Multiple(() => {
                Assert.That(retry.AccessToken.Equals(pending),    Is.False, "a fresh pending token");
                Assert.That(fixture.ServerStore.PendingCount,     Is.EqualTo(1));
            });

        }

        #endregion

        #region TokenRotation_SurvivesAClientCrashBeforeStep8()

        [Test]
        [S2C("SessionInitiation.8")]
        public async Task TokenRotation_SurvivesAClientCrashBeforeStep8()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var oldToken  = await fixture.PairAsync();
            var pending   = ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, oldToken)).AccessToken;

            ParseCommunicationDetails(await ConfirmAccessTokenAsync(fixture, pending));

            // The client crashed after step 7 and before step 8: it still holds both tokens
            // and, as the specification requires, tries the old one first.
            var withOld   = await InitiateSessionAsync(fixture, oldToken);
            var withNew   = await InitiateSessionAsync(fixture, pending);
            var next      = ParseInitiateSessionResponse(withNew);

            Assert.Multiple(() => {
                Assert.That(withOld.Status,                       Is.EqualTo(HttpStatusCode.Unauthorized), "the old token was invalidated by the activation");
                Assert.That(withNew.Status,                       Is.EqualTo(HttpStatusCode.OK), "the activated token opens the next session initiation");
                Assert.That(next.AccessToken.Equals(pending),     Is.False);
                Assert.That(next.AccessToken.Equals(oldToken),    Is.False);
                Assert.That(fixture.ServerStore.PendingCount,     Is.EqualTo(1));
            });

        }

        #endregion


        // Store failures (a communication server with a store that can be told to fail)

        #region InitiateSession_Returns500_WhenTheStoreCannotBeRead()

        [Test]
        [S2C("SessionInitiation.1.StoreFailure")]
        public async Task InitiateSession_Returns500_WhenTheStoreCannotBeRead()
        {

            await using var server = await FailingStoreServer.CreateAsync();

            var activeToken  = await server.PairAsync();

            server.Store.Fail(nameof(IS2Store.GetUnpairedAtAsync));

            var failed       = await server.InitiateSessionAsync(activeToken);

            Assert.Multiple(() => {
                Assert.That(failed.Status,                      Is.EqualTo(HttpStatusCode.InternalServerError));
                Assert.That(failed.ErrorMessage,                Is.Null, "a 500 carries no CommunicationDetailsErrorMessage");
                Assert.That(server.Store.PendingCount,          Is.EqualTo(0));
            });

            server.Store.Recover();

            ParseInitiateSessionResponse(await server.InitiateSessionAsync(activeToken));

            Assert.That(server.Store.PendingCount, Is.EqualTo(1));

        }

        #endregion

        #region InitiateSession_Returns500_WhenStoringThePendingTokenFails()

        [Test]
        [S2C("SessionInitiation.2.StoreFailure")]
        public async Task InitiateSession_Returns500_WhenStoringThePendingTokenFails()
        {

            await using var server = await FailingStoreServer.CreateAsync();

            var activeToken  = await server.PairAsync();
            var initiated    = new List<InitiateSessionResponse>();

            server.API.OnSessionInitiated += (_, _, _, issued) => {
                                                 lock (initiated)
                                                     initiated.Add(issued);
                                                 return Task.CompletedTask;
                                             };

            server.Store.Fail(nameof(IS2Store.AddPendingAccessTokenAsync));

            var failed       = await server.InitiateSessionAsync(activeToken);
            var pairing      = await server.Store.GetPairingAsync(server.CEM.Id, server.RM.Id);

            Assert.Multiple(() => {
                Assert.That(failed.Status,                      Is.EqualTo(HttpStatusCode.InternalServerError));
                Assert.That(failed.ErrorMessage,                Is.Null);
                Assert.That(server.Store.PendingCount,          Is.EqualTo(0), "a token that could not be persisted is not pending");
                Assert.That(pairing!.AccessToken.Equals(activeToken), Is.True, "the active token is untouched");
                Assert.That(initiated,                          Is.Empty, "no event for a failed request");
            });

            server.Store.Recover();

            var response     = ParseInitiateSessionResponse(await server.InitiateSessionAsync(activeToken));

            Assert.Multiple(() => {
                Assert.That(server.Store.PendingCount,          Is.EqualTo(1));
                Assert.That(initiated,                          Has.Count.EqualTo(1));
                Assert.That(initiated[0],                       Is.EqualTo(response));
            });

        }

        #endregion

        #region ConfirmAccessToken_Returns500_WhenThePendingTokenCannotBeLookedUp()

        [Test]
        [S2C("SessionInitiation.6.StoreFailure")]
        public async Task ConfirmAccessToken_Returns500_WhenThePendingTokenCannotBeLookedUp()
        {

            await using var server = await FailingStoreServer.CreateAsync();

            var activeToken  = await server.PairAsync();
            var pending      = ParseInitiateSessionResponse(await server.InitiateSessionAsync(activeToken)).AccessToken;

            server.Store.Fail(nameof(IS2Store.FindPendingAccessTokenAsync));

            var failed       = await server.ConfirmAccessTokenAsync(pending);
            var pairing      = await server.Store.GetPairingAsync(server.CEM.Id, server.RM.Id);

            Assert.Multiple(() => {
                Assert.That(failed.Status,                      Is.EqualTo(HttpStatusCode.InternalServerError));
                Assert.That(server.Store.PendingCount,          Is.EqualTo(1), "the pending token survives the failure");
                Assert.That(pairing!.AccessToken.Equals(activeToken), Is.True);
                Assert.That(server.TokenStore.Count,            Is.EqualTo(0), "no communication token was issued");
            });

            server.Store.Recover();

            ParseCommunicationDetails(await server.ConfirmAccessTokenAsync(pending));

        }

        #endregion

        #region ConfirmAccessToken_Returns500_WhenTheActivationFails_AndKeepsThePendingToken()

        [Test]
        [S2C("SessionInitiation.6.StoreFailure")]
        public async Task ConfirmAccessToken_Returns500_WhenTheActivationFails_AndKeepsThePendingToken()
        {

            await using var server = await FailingStoreServer.CreateAsync();

            var activeToken  = await server.PairAsync();
            var activations  = new List<S2ConnectSessionIdentity>();

            server.API.OnAccessTokenActivated += (_, _, _, activatedIdentity, _) => {
                                                     lock (activations)
                                                         activations.Add(activatedIdentity);
                                                     return Task.CompletedTask;
                                                 };

            var pending      = ParseInitiateSessionResponse(await server.InitiateSessionAsync(activeToken)).AccessToken;

            server.Store.Fail(nameof(IS2Store.ActivateAccessTokenAsync));

            var failed       = await server.ConfirmAccessTokenAsync(pending);
            var pairing      = await server.Store.GetPairingAsync(server.CEM.Id, server.RM.Id);

            Assert.Multiple(() => {
                Assert.That(failed.Status,                      Is.EqualTo(HttpStatusCode.InternalServerError));
                Assert.That(failed.ErrorMessage,                Is.Null);
                Assert.That(server.Store.PendingCount,          Is.EqualTo(1), "the pending token survives the failure");
                Assert.That(pairing!.AccessToken.Equals(activeToken), Is.True, "the active token is untouched");
                Assert.That(server.TokenStore.Count,            Is.EqualTo(0), "no communication token was issued");
                Assert.That(activations,                        Is.Empty, "no event for a failed request");
            });

            // The client retries the confirmation as soon as the store is back.
            server.Store.Recover();

            var details      = ParseCommunicationDetails(await server.ConfirmAccessTokenAsync(pending));
            var afterwards   = await server.Store.GetPairingAsync(server.CEM.Id, server.RM.Id);
            var redeemed     = server.TokenStore.TryRedeem(details.WebsocketToken.Value, out var identity);

            Assert.Multiple(() => {
                Assert.That(afterwards!.AccessToken.Equals(pending), Is.True, "the pending token is now the active token");
                Assert.That(server.Store.PendingCount,          Is.EqualTo(0));
                Assert.That(redeemed,                           Is.True);
                Assert.That(identity,                           Is.InstanceOf<S2ConnectSessionIdentity>());
                Assert.That(activations,                        Has.Count.EqualTo(1));
                Assert.That(activations[0].RemoteNodeId,        Is.EqualTo(server.RM.Id));
            });

        }

        #endregion

        #region Unpair_Returns500_WhenTheStoreFails_AndKeepsThePairing()

        [Test]
        [S2C("Unpairing.ByCommunicationClient.StoreFailure")]
        public async Task Unpair_Returns500_WhenTheStoreFails_AndKeepsThePairing()
        {

            await using var server = await FailingStoreServer.CreateAsync();

            var activeToken  = await server.PairAsync();
            var unpaired     = new List<Pairing>();

            server.API.OnUnpaired += (_, _, removedPairing, _) => {
                                         lock (unpaired)
                                             unpaired.Add(removedPairing);
                                         return Task.CompletedTask;
                                     };

            server.Store.Fail(nameof(IS2Store.UnpairAsync));

            var failed       = await server.UnpairAsync(activeToken);
            var pairing      = await server.Store.GetPairingAsync(server.CEM.Id, server.RM.Id);
            var unpairedAt   = await server.Store.GetUnpairedAtAsync(server.CEM.Id, server.RM.Id);

            Assert.Multiple(() => {
                Assert.That(failed.Status,                      Is.EqualTo(HttpStatusCode.InternalServerError));
                Assert.That(pairing,                            Is.Not.Null, "the pairing survives the failure");
                Assert.That(unpairedAt,                         Is.Null, "no tombstone was written");
                Assert.That(unpaired,                           Is.Empty, "no event for a failed request");
            });

            server.Store.Recover();

            var result       = await server.UnpairAsync(activeToken);

            Assert.Multiple(() => {
                Assert.That(result.Status,                      Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(server.Store.TombstoneCount,        Is.EqualTo(1));
                Assert.That(unpaired,                           Has.Count.EqualTo(1));
            });

        }

        #endregion


        // unpair

        #region Unpair_WithTheActiveToken_Returns204_AndWritesATombstone()

        [Test]
        [S2C("Unpairing.ByCommunicationClient")]
        public async Task Unpair_WithTheActiveToken_Returns204_AndWritesATombstone()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();

            var result       = await UnpairAsync(fixture, activeToken);
            var pairing      = await ServerPairingAsync(fixture);
            var unpairedAt   = await fixture.ServerStore.GetUnpairedAtAsync(fixture.CEM.Id, fixture.RM.Id);
            var afterwards   = await InitiateSessionAsync(fixture, activeToken);

            Assert.Multiple(() => {
                Assert.That(result.Status,                         Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(result.Body,                           Is.Null.Or.Empty, "a 204 has no body");
                Assert.That(pairing,                               Is.Null, "the security material of the pairing is gone");
                Assert.That(unpairedAt,                            Is.Not.Null, "a tombstone remembers the unpairing");
                Assert.That(fixture.ServerStore.TombstoneCount,    Is.EqualTo(1));
                Assert.That(fixture.UnpairedAtServer,              Has.Count.EqualTo(1));
                Assert.That(fixture.UnpairedAtServer[0].RemoteNodeId, Is.EqualTo(fixture.RM.Id));
                Assert.That(afterwards.Status,                     Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(afterwards.ErrorMessage,               Is.EqualTo("NoLongerPaired"));
            });

            // A second unpairing is a 401: the nodes are no longer paired.
            var again = await UnpairAsync(fixture, activeToken);

            Assert.That(again.Status, Is.EqualTo(HttpStatusCode.Unauthorized));

        }

        #endregion

        #region Unpair_WithAWrongToken_Returns401()

        [Test]
        [S2C("Unpairing.ByCommunicationClient.Unauthorized")]
        public async Task Unpair_WithAWrongToken_Returns401()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            await fixture.PairAsync();

            var result       = await UnpairAsync(fixture, TokenGenerator.NewAccessToken());
            var pairing      = await ServerPairingAsync(fixture);
            var unpairedAt   = await fixture.ServerStore.GetUnpairedAtAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.Multiple(() => {
                Assert.That(result.Status,                      Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(result.WWWAuthenticate,             Does.Contain("Bearer"));
                Assert.That(pairing,                            Is.Not.Null, "the pairing is untouched");
                Assert.That(unpairedAt,                         Is.Null, "no tombstone was written");
                Assert.That(fixture.UnpairedAtServer,           Is.Empty);
            });

        }

        #endregion

        #region Unpair_OfUnknownNodes_Returns401()

        [Test]
        [S2C("Unpairing.ByCommunicationClient.Unauthorized")]
        public async Task Unpair_OfUnknownNodes_Returns401()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken    = await fixture.PairAsync();

            var unknownClient  = await UnpairAsync(fixture, activeToken, new UnpairRequest(Node_Id.NewRandom, fixture.CEM.Id));
            var unknownServer  = await UnpairAsync(fixture, activeToken, new UnpairRequest(fixture.RM.Id, Node_Id.NewRandom));
            var pairing        = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(unknownClient.Status,               Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(unknownServer.Status,               Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(pairing,                            Is.Not.Null, "the pairing is untouched");
                Assert.That(fixture.ServerStore.TombstoneCount, Is.EqualTo(0));
                Assert.That(fixture.UnpairedAtServer,           Is.Empty);
            });

        }

        #endregion

        #region Unpair_WithAPendingToken_Returns204()

        [Test]
        [S2C("Unpairing.ByCommunicationClient")]
        public async Task Unpair_WithAPendingToken_Returns204()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();
            var pending      = ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, activeToken)).AccessToken;

            // Any candidate token of the client, active or pending, unpairs.
            var result       = await UnpairAsync(fixture, pending);
            var pairing      = await ServerPairingAsync(fixture);
            var unpairedAt   = await fixture.ServerStore.GetUnpairedAtAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.Multiple(() => {
                Assert.That(result.Status,                      Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(pairing,                            Is.Null);
                Assert.That(unpairedAt,                         Is.Not.Null);
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0), "the pending tokens of the pair are gone as well");
                Assert.That(fixture.UnpairedAtServer,           Has.Count.EqualTo(1));
            });

            // The pending token cannot be confirmed afterwards.
            var confirm = await ConfirmAccessTokenAsync(fixture, pending);

            Assert.That(confirm.Status, Is.EqualTo(HttpStatusCode.Unauthorized));

        }

        #endregion

        #region Unpair_WithoutBearerOrWithABrokenBody_IsRejected()

        [Test]
        [S2C("Unpairing.ByCommunicationClient.Unauthorized")]
        public async Task Unpair_WithoutBearerOrWithABrokenBody_IsRejected()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();
            var request      = new UnpairRequest(fixture.RM.Id, fixture.CEM.Id).ToJSON();

            var missing      = await fixture.PostAsync("v1/unpair", request);
            var malformed    = await fixture.PostAsync("v1/unpair", request, "not-base64!!");
            var notJSON      = await fixture.PostAsync("v1/unpair", Bearer: activeToken.Value, RawBody: "{ not json");
            var incomplete   = await fixture.PostAsync("v1/unpair", new JObject(new JProperty("clientNodeId", fixture.RM.Id.ToString())), activeToken.Value);
            var pairing      = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(missing.Status,                     Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(missing.WWWAuthenticate,            Does.Contain("Bearer"));
                Assert.That(malformed.Status,                   Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(notJSON.Status,                     Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(notJSON.ErrorMessage,               Is.EqualTo("ParsingError"));
                Assert.That(incomplete.Status,                  Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(incomplete.ErrorMessage,            Is.EqualTo("ParsingError"));
                Assert.That(pairing,                            Is.Not.Null, "the pairing is untouched");
                Assert.That(fixture.ServerStore.TombstoneCount, Is.EqualTo(0));
            });

        }

        #endregion

        #region PairingAgain_ClearsTheTombstone()

        [Test]
        [S2C("Unpairing.Tombstone")]
        public async Task PairingAgain_ClearsTheTombstone()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();
            var unpair       = await UnpairAsync(fixture, activeToken);

            Assert.That(unpair.Status, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(await fixture.ServerStore.GetUnpairedAtAsync(fixture.CEM.Id, fixture.RM.Id), Is.Not.Null);

            // A new pairing process (here: written directly into the stores) replaces the tombstone.
            var newToken     = await fixture.PairAsync();
            var unpairedAt   = await fixture.ServerStore.GetUnpairedAtAsync(fixture.CEM.Id, fixture.RM.Id);
            var stale        = await InitiateSessionAsync(fixture, activeToken);
            var response     = ParseInitiateSessionResponse(await InitiateSessionAsync(fixture, newToken));

            Assert.Multiple(() => {
                Assert.That(unpairedAt,                         Is.Null, "the tombstone is cleared");
                Assert.That(fixture.ServerStore.TombstoneCount, Is.EqualTo(0));
                Assert.That(stale.Status,                       Is.EqualTo(HttpStatusCode.Unauthorized), "the token of the earlier pairing is worthless");
                Assert.That(response.AccessToken.Equals(newToken), Is.False);
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(1));
            });

        }

        #endregion

        #region UnpairLocally_Twice_ReturnsNullTheSecondTime()

        [Test]
        [S2C("Unpairing.ByCommunicationServer")]
        public async Task UnpairLocally_Twice_ReturnsNullTheSecondTime()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();
            var before       = await ServerPairingAsync(fixture);

            var first        = await fixture.API.UnpairLocallyAsync(fixture.CEM.Id, fixture.RM.Id);
            var second       = await fixture.API.UnpairLocallyAsync(fixture.CEM.Id, fixture.RM.Id);
            var unpairedAt   = await fixture.ServerStore.GetUnpairedAtAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.Multiple(() => {
                Assert.That(first,                              Is.EqualTo(before), "the removed pairing is returned");
                Assert.That(first!.AccessToken.Equals(activeToken), Is.True);
                Assert.That(second,                             Is.Null, "already unpaired");
                Assert.That(unpairedAt,                         Is.Not.Null);
                Assert.That(fixture.ServerStore.TombstoneCount, Is.EqualTo(1));
                Assert.That(fixture.UnpairedAtServer,           Has.Count.EqualTo(1), "the event is raised once");
                Assert.That(fixture.UnpairedAtServer[0],        Is.EqualTo(before));
            });

            // Unpairing locally an unknown pair changes nothing.
            Assert.That(await fixture.API.UnpairLocallyAsync(fixture.CEM.Id, Node_Id.NewRandom), Is.Null);
            Assert.That(fixture.ServerStore.TombstoneCount, Is.EqualTo(1));

        }

        #endregion


        // Maintenance and shutdown

        #region PurgePendingAccessTokens_RemovesExpiredTokensOnly()

        [Test]
        [S2C("SessionInitiation.6.Expired")]
        public async Task PurgePendingAccessTokens_RemovesExpiredTokensOnly()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var now      = DateTimeOffset.UtcNow;
            var expired  = new PendingAccessToken(fixture.CEM.Id, fixture.RM.Id, TokenGenerator.NewAccessToken(), now - TimeSpan.FromMinutes(1));
            var fresh    = new PendingAccessToken(fixture.CEM.Id, fixture.RM.Id, TokenGenerator.NewAccessToken(), now);

            await fixture.ServerStore.AddPendingAccessTokenAsync(expired);
            await fixture.ServerStore.AddPendingAccessTokenAsync(fresh);

            var removed  = await fixture.API.PurgePendingAccessTokensAsync();

            Assert.Multiple(() => {
                Assert.That(removed,                            Is.EqualTo(1));
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(1));
            });

            Assert.That(await fixture.ServerStore.FindPendingAccessTokenAsync(expired.Token), Is.Null,     "older than the pending token lifetime");
            Assert.That(await fixture.ServerStore.FindPendingAccessTokenAsync(fresh.Token),   Is.EqualTo(fresh), "still confirmable");

            Assert.That(await fixture.API.PurgePendingAccessTokensAsync(), Is.EqualTo(0), "nothing left to purge");

        }

        #endregion

        #region Shutdown_Answers503WithRetryAfter_ButStillUnpairs()

        [Test]
        [S2C("SessionInitiation.ServiceUnavailable")]
        public async Task Shutdown_Answers503WithRetryAfter_ButStillUnpairs()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var activeToken  = await fixture.PairAsync();

            fixture.API.Shutdown();

            var initiate     = await InitiateSessionAsync(fixture, activeToken);
            var confirm      = await ConfirmAccessTokenAsync(fixture, TokenGenerator.NewAccessToken());

            Assert.Multiple(() => {
                Assert.That(fixture.API.IsShutdown,             Is.True);
                Assert.That(initiate.Status,                    Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(initiate.RetryAfter,                Is.Not.Null, "the client is told when to retry");
                Assert.That(initiate.ErrorMessage,              Is.Null);
                Assert.That(confirm.Status,                     Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(confirm.RetryAfter,                 Is.Not.Null);
                Assert.That(fixture.ServerStore.PendingCount,   Is.EqualTo(0));
            });

            // Unpairing is still served, so that a client can clean up.
            var unpair = await UnpairAsync(fixture, activeToken);

            Assert.Multiple(() => {
                Assert.That(unpair.Status,                      Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(fixture.ServerStore.TombstoneCount, Is.EqualTo(1));
            });

        }

        #endregion

    }


    /// <summary>
    /// An <see cref="IS2Store"/> that delegates every operation to an <see cref="InMemoryS2Store"/>
    /// but throws an <see cref="IOException"/> from every operation named via <see cref="Fail"/>,
    /// so that the tests can observe how the session initiation server handles a failing store.
    /// The set of failing operations is changed between requests only.
    /// </summary>
    internal sealed class FailingStore : IS2Store
    {

        #region Properties

        /// <summary>
        /// The wrapped in-memory store.
        /// </summary>
        public InMemoryS2Store   Inner                { get; }

        /// <summary>
        /// The names of the operations (e.g. nameof(IS2Store.ActivateAccessTokenAsync)) that currently fail.
        /// </summary>
        public HashSet<String>   FailingOperations    { get; } = new (StringComparer.Ordinal);

        /// <summary>
        /// The number of pending access tokens of the wrapped store.
        /// </summary>
        public Int32             PendingCount
            => Inner.PendingCount;

        /// <summary>
        /// The number of tombstones of the wrapped store.
        /// </summary>
        public Int32             TombstoneCount
            => Inner.TombstoneCount;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new failing store around the given (or a new) in-memory store.
        /// </summary>
        /// <param name="Inner">An optional in-memory store to wrap.</param>
        public FailingStore(InMemoryS2Store? Inner = null)
        {
            this.Inner = Inner ?? new InMemoryS2Store();
        }

        #endregion


        #region Fail(Operations) / Recover()

        /// <summary>
        /// Let the given operations fail from now on.
        /// </summary>
        /// <param name="Operations">The names of the operations, e.g. nameof(IS2Store.ActivateAccessTokenAsync).</param>
        public void Fail(params String[] Operations)
        {
            lock (FailingOperations)
            {
                foreach (var operation in Operations)
                    FailingOperations.Add(operation);
            }
        }

        /// <summary>
        /// Let every operation succeed again.
        /// </summary>
        public void Recover()
        {
            lock (FailingOperations)
            {
                FailingOperations.Clear();
            }
        }

        private void ThrowIfFailing([CallerMemberName] String Operation = "")
        {
            lock (FailingOperations)
            {
                if (FailingOperations.Contains(Operation))
                    throw new IOException($"The test store fails during {Operation}!");
            }
        }

        #endregion


        #region IS2Store members (delegating)

        public ValueTask<IReadOnlyList<Pairing>> GetPairingsAsync(Node_Id?           LocalNodeId         = null,
                                                                  CancellationToken  CancellationToken   = default)
        {
            ThrowIfFailing();
            return Inner.GetPairingsAsync(LocalNodeId, CancellationToken);
        }

        public ValueTask<Pairing?> GetPairingAsync(Node_Id            LocalNodeId,
                                                   Node_Id            RemoteNodeId,
                                                   CancellationToken  CancellationToken   = default)
        {
            ThrowIfFailing();
            return Inner.GetPairingAsync(LocalNodeId, RemoteNodeId, CancellationToken);
        }

        public ValueTask<Pairing?> AddOrReplacePairingAsync(Pairing            Pairing,
                                                            CancellationToken  CancellationToken   = default)
        {
            ThrowIfFailing();
            return Inner.AddOrReplacePairingAsync(Pairing, CancellationToken);
        }

        public ValueTask<Pairing?> RemovePairingAsync(Node_Id            LocalNodeId,
                                                      Node_Id            RemoteNodeId,
                                                      CancellationToken  CancellationToken   = default)
        {
            ThrowIfFailing();
            return Inner.RemovePairingAsync(LocalNodeId, RemoteNodeId, CancellationToken);
        }

        public ValueTask AddPendingAccessTokenAsync(PendingAccessToken  PendingAccessToken,
                                                    CancellationToken   CancellationToken   = default)
        {
            ThrowIfFailing();
            return Inner.AddPendingAccessTokenAsync(PendingAccessToken, CancellationToken);
        }

        public ValueTask<IReadOnlyList<PendingAccessToken>> GetPendingAccessTokensAsync(Node_Id            LocalNodeId,
                                                                                        Node_Id            RemoteNodeId,
                                                                                        CancellationToken  CancellationToken   = default)
        {
            ThrowIfFailing();
            return Inner.GetPendingAccessTokensAsync(LocalNodeId, RemoteNodeId, CancellationToken);
        }

        public ValueTask<PendingAccessToken?> FindPendingAccessTokenAsync(AccessToken        Token,
                                                                          CancellationToken  CancellationToken   = default)
        {
            ThrowIfFailing();
            return Inner.FindPendingAccessTokenAsync(Token, CancellationToken);
        }

        public ValueTask<Pairing?> ActivateAccessTokenAsync(Node_Id            LocalNodeId,
                                                            Node_Id            RemoteNodeId,
                                                            AccessToken        Token,
                                                            CancellationToken  CancellationToken   = default)
        {
            ThrowIfFailing();
            return Inner.ActivateAccessTokenAsync(LocalNodeId, RemoteNodeId, Token, CancellationToken);
        }

        public ValueTask<Int32> RemovePendingAccessTokensAsync(Node_Id?           LocalNodeId         = null,
                                                               Node_Id?           RemoteNodeId        = null,
                                                               DateTimeOffset?    CreatedBefore       = null,
                                                               CancellationToken  CancellationToken   = default)
        {
            ThrowIfFailing();
            return Inner.RemovePendingAccessTokensAsync(LocalNodeId, RemoteNodeId, CreatedBefore, CancellationToken);
        }

        public ValueTask<IReadOnlyList<AccessToken>> GetAccessTokenCandidatesAsync(Node_Id            LocalNodeId,
                                                                                   Node_Id            RemoteNodeId,
                                                                                   CancellationToken  CancellationToken   = default)
        {
            ThrowIfFailing();
            return Inner.GetAccessTokenCandidatesAsync(LocalNodeId, RemoteNodeId, CancellationToken);
        }

        public ValueTask<Pairing?> UnpairAsync(Node_Id            LocalNodeId,
                                               Node_Id            RemoteNodeId,
                                               DateTimeOffset     At,
                                               CancellationToken  CancellationToken   = default)
        {
            ThrowIfFailing();
            return Inner.UnpairAsync(LocalNodeId, RemoteNodeId, At, CancellationToken);
        }

        public ValueTask<DateTimeOffset?> GetUnpairedAtAsync(Node_Id            LocalNodeId,
                                                             Node_Id            RemoteNodeId,
                                                             CancellationToken  CancellationToken   = default)
        {
            ThrowIfFailing();
            return Inner.GetUnpairedAtAsync(LocalNodeId, RemoteNodeId, CancellationToken);
        }

        #endregion

    }


    /// <summary>
    /// A communication server (CEM) whose store is a <see cref="FailingStore"/>, on its own Hermod
    /// HTTP server and reachable through a plain <see cref="HttpClient"/>. It exists because
    /// <see cref="SessionInitiationFixture.CreateAsync"/> accepts an <see cref="InMemoryS2Store"/>
    /// only; no WebSocket server is needed, the communication token store suffices to observe
    /// the issued tokens. The RM is represented by its descriptions only.
    /// </summary>
    internal sealed class FailingStoreServer : IAsyncDisposable
    {

        #region Properties

        public HTTPServer                  HTTPServer     { get; }
        public LocalEndpoint               Endpoint       { get; }
        public HostedNode                  CEM            { get; }
        public FailingStore                Store          { get; }
        public CommunicationTokenStore     TokenStore     { get; }
        public SessionInitiationServerAPI  API            { get; }
        public NodeDescription             RM             { get; }
        public EndpointDescription         RMEndpoint     { get; }
        public HttpClient                  HttpClient     { get; }

        #endregion

        #region Constructor(s)

        private FailingStoreServer(HTTPServer                  HTTPServer,
                                   LocalEndpoint               Endpoint,
                                   HostedNode                  CEM,
                                   FailingStore                Store,
                                   CommunicationTokenStore     TokenStore,
                                   SessionInitiationServerAPI  API,
                                   S2BaseURL                   SessionInitiationUrl)
        {

            this.HTTPServer  = HTTPServer;
            this.Endpoint    = Endpoint;
            this.CEM         = CEM;
            this.Store       = Store;
            this.TokenStore  = TokenStore;
            this.API         = API;
            this.RM          = new NodeDescription(Node_Id.NewRandom, "ACME", "heat pump", "TestRM 1", EnergyManagementRole.RM);
            this.RMEndpoint  = new EndpointDescription("Test RM endpoint", null, Deployment.LAN);

            this.HttpClient  = new HttpClient {
                                   BaseAddress  = new Uri(SessionInitiationUrl.Value),
                                   Timeout      = TimeSpan.FromSeconds(60)
                               };

        }

        #endregion


        #region (static) CreateAsync()

        /// <summary>
        /// Start a communication server with a failing store (nothing fails until <see cref="FailingStore.Fail"/> is called).
        /// </summary>
        public static async Task<FailingStoreServer> CreateAsync()
        {

            var httpPort       = PairingServerFixture.FreePort();
            var webSocketPort  = PairingServerFixture.FreePort();

            var pairingUrl     = S2BaseURL.Parse($"http://127.0.0.1:{httpPort}/pairing/",    AllowHTTP: true);
            var sessionUrl     = S2BaseURL.Parse($"http://127.0.0.1:{httpPort}/connection/", AllowHTTP: true);
            var webSocketUrl   = URL.Parse($"ws://127.0.0.1:{webSocketPort}/");

            var endpoint       = new LocalEndpoint(new EndpointDescription("Test CEM endpoint with a failing store"),
                                                   Deployment.LAN,
                                                   pairingUrl,
                                                   sessionUrl);

            var cem            = endpoint.AddNode(new NodeDescription(Node_Id.NewRandom, "GraphDefined", "EMS", "TestCEM 1", EnergyManagementRole.CEM));

            var store          = new FailingStore();
            var tokenStore     = new CommunicationTokenStore();

            var httpServer     = new HTTPServer(IPv4Address.Parse("127.0.0.1"), httpPort, "S2 test HTTP server", AutoStart: false);

            var api            = new SessionInitiationServerAPI(httpServer,
                                                                endpoint,
                                                                store,
                                                                tokenStore,
                                                                webSocketUrl,
                                                                Options: SessionInitiationFixture.DefaultServerOptions());

            await httpServer.Start();

            return new FailingStoreServer(httpServer, endpoint, cem, store, tokenStore, api, sessionUrl);

        }

        #endregion


        #region PairAsync()

        /// <summary>
        /// Store a pairing of the CEM with the RM with a fresh access token (through the failing store).
        /// </summary>
        public async Task<AccessToken> PairAsync()
        {

            var token = TokenGenerator.NewAccessToken();

            await Store.AddOrReplacePairingAsync(new Pairing(CEM.Id,
                                                             RM,
                                                             RMEndpoint,
                                                             CommunicationRole.CommunicationServer,
                                                             token,
                                                             DateTimeOffset.UtcNow));

            return token;

        }

        #endregion

        #region InitiateSessionAsync(Bearer) / ConfirmAccessTokenAsync(PendingBearer) / UnpairAsync(Bearer)

        /// <summary>
        /// POST v1/initiateSession as the RM with the given bearer token.
        /// </summary>
        public Task<HTTPResult> InitiateSessionAsync(AccessToken Bearer)

            => PostAsync("v1/initiateSession",
                         new InitiateSessionRequest(RM.Id,
                                                    CEM.Id,
                                                    CEM.SupportedS2MessageVersions,
                                                    CEM.SupportedCommunicationProtocols).ToJSON(),
                         Bearer);

        /// <summary>
        /// POST v1/confirmAccessToken with the given pending bearer token.
        /// </summary>
        public Task<HTTPResult> ConfirmAccessTokenAsync(AccessToken PendingBearer)

            => PostAsync("v1/confirmAccessToken",
                         null,
                         PendingBearer);

        /// <summary>
        /// POST v1/unpair as the RM with the given bearer token.
        /// </summary>
        public Task<HTTPResult> UnpairAsync(AccessToken Bearer)

            => PostAsync("v1/unpair",
                         new UnpairRequest(RM.Id, CEM.Id).ToJSON(),
                         Bearer);

        private async Task<HTTPResult> PostAsync(String       RelativePath,
                                                 JToken?      Body,
                                                 AccessToken  Bearer)
        {

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(RelativePath, UriKind.Relative));

            if (Body is not null)
                request.Content = new StringContent(Body.ToString(Formatting.None), Encoding.UTF8, "application/json");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Bearer.Value);

            using var response = await HttpClient.SendAsync(request);

            return await HTTPResult.FromAsync(response);

        }

        #endregion


        #region DisposeAsync()

        public async ValueTask DisposeAsync()
        {

            HttpClient.Dispose();

            try
            {
                await HTTPServer.Stop();
            }
            catch (Exception)
            { }

        }

        #endregion

    }

}

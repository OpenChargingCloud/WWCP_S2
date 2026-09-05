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
using System.Text;

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Tests.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// Hostile input for the S2 Connect session initiation server over real HTTP (PLAN.md §11a):
    /// malformed bodies, wrong content types, raw byte payloads and every shape of a broken
    /// bearer access token on "v1/initiateSession", "v1/confirmAccessToken" and "v1/unpair".
    ///
    /// The invariants are the ones of <see cref="S2FuzzCorpus.Violations"/>: never 5xx (a
    /// deliberate 503 with a Retry-After header excepted), never a hang, always a complete HTTP
    /// answer, an error body that is empty or the S2 error shape, no leaked internals, no echoed
    /// secrets - and a server that still initiates a session afterwards.
    ///
    /// The rate limit and the request size limit have their own tests
    /// (<see cref="HardeningSmokeTests"/>); the fixtures here run with the rate limit switched
    /// off, so that the fuzzing is not throttled.
    /// </summary>
    [TestFixture]
    public sealed class SessionInitiationFuzzTests
    {

        #region Data

        /// <summary>
        /// The routes that take a JSON object.
        /// </summary>
        private static readonly String[] bodyRoutes = [
            "v1/initiateSession",
            "v1/unpair"
        ];

        /// <summary>
        /// The route without a request body.
        /// </summary>
        private const String confirmRoute = "v1/confirmAccessToken";

        #endregion

        #region (private static) Helpers

        /// <summary>
        /// A session initiation fixture without the per-source rate limit, so that a fuzzing
        /// loop is not throttled by the default budget of 120 requests per minute.
        /// </summary>
        private static Task<SessionInitiationFixture> CreateFixtureAsync()

            => SessionInitiationFixture.CreateAsync(
                   ServerOptions: SessionInitiationFixture.DefaultServerOptions() with {
                                      EnableRateLimiting = false
                                  }
               );


        /// <summary>
        /// POST the given body and hand the invariant violations of the answer back.
        /// </summary>
        private static async Task<IReadOnlyList<String>> PostAndCheckAsync(SessionInitiationFixture  Fixture,
                                                                           String                    Route,
                                                                           String                    Label,
                                                                           JToken?                   Body             = null,
                                                                           String?                   RawBody          = null,
                                                                           String                    ContentType      = "application/json",
                                                                           String?                   Bearer           = null,
                                                                           Boolean                   MustBeRejected   = false,
                                                                           IEnumerable<String>?      Secrets          = null)
        {

            var stopwatch  = Stopwatch.StartNew();

            var result     = await Fixture.PostAsync(Route,
                                                     Body,
                                                     Bearer,
                                                     RawBody,
                                                     ContentType);

            stopwatch.Stop();

            return S2FuzzCorpus.Violations($"{Route} / {Label}",
                                           result,
                                           stopwatch.Elapsed,
                                           Secrets,
                                           MustBeRejected);

        }


        /// <summary>
        /// Prove that the session initiation server is undamaged: it pairs, initiates a session
        /// and hands out the communication details for the confirmed access token.
        /// </summary>
        private static async Task AssertStillInitiatesASessionAsync(SessionInitiationFixture Fixture)
        {

            var token      = await Fixture.PairAsync();

            var initiated  = await Fixture.PostAsync("v1/initiateSession",
                                                     Fixture.InitiateSessionRequest().ToJSON(),
                                                     token.Value);

            Assert.That(initiated.Status, Is.EqualTo(HttpStatusCode.OK), initiated.Body);
            Assert.That(InitiateSessionResponse.TryParse(initiated.Object, out var response, out var error), Is.True, error);

            var confirmed  = await Fixture.PostAsync(confirmRoute, Bearer: response!.AccessToken.Value);

            Assert.That(confirmed.Status, Is.EqualTo(HttpStatusCode.OK), confirmed.Body);

        }

        #endregion


        // Malformed bodies.

        #region MalformedBodies_OnEverySessionRoute_AreRejectedWith4xx()

        [Test]
        public async Task MalformedBodies_OnEverySessionRoute_AreRejectedWith4xx()
        {

            await using var fixture = await CreateFixtureAsync();

            var token     = await fixture.PairAsync();
            var problems  = new List<String>();

            foreach (var (name, body) in S2FuzzCorpus.MalformedBodies())
            {

                foreach (var route in bodyRoutes)
                    problems.AddRange(await PostAndCheckAsync(fixture, route, name, RawBody: body, Bearer: token.Value, MustBeRejected: true));

                // confirmAccessToken has no request body at all: a hostile one must not confuse
                // it either, and the (active, not pending) token is refused with 401.
                problems.AddRange(await PostAndCheckAsync(fixture, confirmRoute, name, RawBody: body, Bearer: token.Value, MustBeRejected: true));

            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillInitiatesASessionAsync(fixture);

        }

        #endregion

        #region StructurallyHostileBodies_AreRejectedWith4xx()

        [Test]
        public async Task StructurallyHostileBodies_AreRejectedWith4xx()
        {

            await using var fixture = await CreateFixtureAsync();

            var token     = await fixture.PairAsync();
            var problems  = new List<String>();

            foreach (var (name, body) in S2FuzzCorpus.StructurallyHostileBodies())
            {
                foreach (var route in bodyRoutes)
                    problems.AddRange(await PostAndCheckAsync(fixture, route, name, RawBody: body, Bearer: token.Value, MustBeRejected: true));
            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillInitiatesASessionAsync(fixture);

        }

        #endregion

        #region HostileContentTypes_AreAnsweredWithoutAServerError()

        [Test]
        public async Task HostileContentTypes_AreAnsweredWithoutAServerError()
        {

            await using var fixture = await CreateFixtureAsync();

            var token     = await fixture.PairAsync();
            var valid     = fixture.InitiateSessionRequest().ToJSON().ToString();
            var problems  = new List<String>();

            foreach (var contentType in S2FuzzCorpus.RejectedContentTypes)
            {
                problems.AddRange(await PostAndCheckAsync(fixture, "v1/initiateSession", $"content type '{contentType}'",
                                                          RawBody: valid, ContentType: contentType, Bearer: token.Value, MustBeRejected: true));
            }

            // An empty body where a JSON object is expected.
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/initiateSession", "an empty body", RawBody: "", Bearer: token.Value, MustBeRejected: true));
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/unpair",          "an empty body", RawBody: "", Bearer: token.Value, MustBeRejected: true));

            // No body at all, and a valid body without any content type.
            foreach (var route in bodyRoutes)
            {

                var stopwatch  = Stopwatch.StartNew();
                var result     = await S2FuzzCorpus.PostRawAsync(fixture.HttpClient, route, AuthorizationHeader: "Bearer " + token.Value);

                stopwatch.Stop();

                if (result is null)
                    problems.Add($"{route}: the HTTP client refused to send a request without a body!");
                else
                    problems.AddRange(S2FuzzCorpus.Violations($"{route} / no body at all", result, stopwatch.Elapsed, MustBeRejected: true));

            }

            var bare = await S2FuzzCorpus.PostRawAsync(fixture.HttpClient,
                                                       "v1/initiateSession",
                                                       Encoding.UTF8.GetBytes(valid),
                                                       AuthorizationHeader: "Bearer " + token.Value);

            if (bare is null)
                problems.Add("The HTTP client refused to send a body without a content type!");
            else
                problems.AddRange(S2FuzzCorpus.Violations("v1/initiateSession / no content type", bare, TimeSpan.Zero));

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillInitiatesASessionAsync(fixture);

        }

        #endregion

        #region RawByteBodies_AreAnsweredWithoutAServerError()

        [Test]
        public async Task RawByteBodies_AreAnsweredWithoutAServerError()
        {

            await using var fixture = await CreateFixtureAsync();

            var token     = await fixture.PairAsync();
            var problems  = new List<String>();

            foreach (var (name, bytes, contentType) in S2FuzzCorpus.HostileByteBodies())
            {
                foreach (var route in bodyRoutes)
                {

                    var stopwatch  = Stopwatch.StartNew();
                    var result     = await S2FuzzCorpus.PostRawAsync(fixture.HttpClient, route, bytes, contentType, "Bearer " + token.Value);

                    stopwatch.Stop();

                    if (result is null)
                        problems.Add($"{route} / {name}: the HTTP client refused to send the request!");
                    else
                        problems.AddRange(S2FuzzCorpus.Violations($"{route} / {name}", result, stopwatch.Elapsed, MustBeRejected: true));

                }
            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillInitiatesASessionAsync(fixture);

        }

        #endregion


        // The access token as a bearer token.

        #region HostileBearerTokens_AreRejectedWith401()

        [Test]
        public async Task HostileBearerTokens_AreRejectedWith401()
        {

            await using var fixture = await CreateFixtureAsync();

            var token     = await fixture.PairAsync();
            var unknown   = TokenGenerator.NewAccessToken().Value;
            var body      = fixture.InitiateSessionRequest().ToJSON().ToString();
            var problems  = new List<String>();

            // A header the HTTP layer itself refuses - a control character in the field value, a
            // header section beyond its size bound - is answered by that layer with its own
            // (non-S2) body and its own status; the S2 error shape and the 401/400 rule can only
            // be demanded of the answers this API produces.
            (String Name, String? Header, Boolean RefusedByTheTransport)[] headers = [
                ("no Authorization header",              null, false),
                ("the Basic scheme",                     "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:password")), false),
                ("the Digest scheme",                    "Digest username=\"a\", realm=\"b\"", false),
                ("a Basic scheme with the real token",   "Basic " + token.Value, false),
                ("no scheme at all",                     unknown, false),
                ("a lower case scheme",                  "bearer " + unknown, false),
                ("an empty bearer",                      "Bearer ", false),
                ("a bearer of blanks",                   "Bearer " + new String(' ', 44), false),
                ("a bearer that is not Base64",          "Bearer !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!", false),
                ("a bearer of the wrong length",         "Bearer AAA", false),
                ("a single byte bearer",                 "Bearer AA==", false),
                ("an unknown but well formed bearer",    "Bearer " + unknown, false),
                ("a truncated real bearer",              "Bearer " + token.Value[..(token.Value.Length - 4)], false),
                ("a real bearer with a suffix",          "Bearer " + token.Value + "AAAA", false),
                ("a 100000 character bearer",            "Bearer " + new String('A', 100000), true),
                ("a bearer with a control character",    "Bearer " + token.Value[..4] + "\u0001" + token.Value[5..], true),
                ("a bearer with a DEL character",        "Bearer " + token.Value[..4] + "\u007F" + token.Value[5..], true),
                ("a bearer with a NUL escape",           "Bearer " + token.Value[..4] + "%00" + token.Value[7..], false),
                ("two bearer tokens",                    "Bearer " + token.Value + " Bearer " + token.Value, false)
            ];

            foreach (var (name, header, refusedByTheTransport) in headers)
            {
                foreach (var route in new[] { "v1/initiateSession", confirmRoute, "v1/unpair" })
                {

                    var payload    = route == confirmRoute
                                         ? null
                                         : Encoding.UTF8.GetBytes(route == "v1/unpair"
                                                                      ? new UnpairRequest(fixture.RM.Id, fixture.CEM.Id).ToJSON().ToString()
                                                                      : body);

                    var stopwatch  = Stopwatch.StartNew();
                    var result     = await S2FuzzCorpus.PostRawAsync(fixture.HttpClient,
                                                                     route,
                                                                     payload,
                                                                     payload is null ? null : "application/json; charset=utf-8",
                                                                     header);

                    stopwatch.Stop();

                    // Some header values are refused by the local HTTP client itself; that is a
                    // client concern, not a finding about the server.
                    if (result is null)
                        continue;

                    problems.AddRange(S2FuzzCorpus.Violations($"{route} / {name}",
                                                              result,
                                                              stopwatch.Elapsed,
                                                              new[] { token.Value },
                                                              MustBeRejected:   true,
                                                              CheckErrorShape:  !refusedByTheTransport));

                    if (!refusedByTheTransport &&
                        result.Status is not HttpStatusCode.Unauthorized and not HttpStatusCode.BadRequest)
                        problems.Add($"{route} / {name}: expected 401 or 400 but got {(Int32) result.Status}.");

                    if (result.Status == HttpStatusCode.Unauthorized && result.WWWAuthenticate?.Contains("Bearer", StringComparison.Ordinal) != true)
                        problems.Add($"{route} / {name}: the 401 carries no 'WWW-Authenticate: Bearer' header.");

                }
            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            // The pairing is untouched: the real token still works.
            await AssertStillInitiatesASessionAsync(fixture);

        }

        #endregion


        // initiateSession.

        #region HostileInitiateSessionFields_AreAnsweredWithoutAServerError()

        [Test]
        public async Task HostileInitiateSessionFields_AreAnsweredWithoutAServerError()
        {

            await using var fixture = await CreateFixtureAsync();

            var token = await fixture.PairAsync();

            (String Name, Action<JObject> Apply, Boolean MustBeRejected)[] mutations = [

                // Missing mandatory properties.
                ("without clientNodeId",                     json => json.Remove("clientNodeId"),                                                   true),
                ("without serverNodeId",                     json => json.Remove("serverNodeId"),                                                   true),
                ("without supportedS2MessageVersions",       json => json.Remove("supportedS2MessageVersions"),                                     true),
                ("without supportedCommunicationProtocols",  json => json.Remove("supportedCommunicationProtocols"),                                true),

                // Wrong JSON types.
                ("clientNodeId as a number",                 json => json["clientNodeId"] = 42,                                                     true),
                ("clientNodeId as an object",                json => json["clientNodeId"] = new JObject(),                                          true),
                ("clientNodeId as an array",                 json => json["clientNodeId"] = new JArray(),                                           true),
                ("serverNodeId as a boolean",                json => json["serverNodeId"] = true,                                                   true),
                ("supportedS2MessageVersions as a string",   json => json["supportedS2MessageVersions"] = "v1.0.0",                                 true),
                ("supportedS2MessageVersions of numbers",    json => json["supportedS2MessageVersions"] = new JArray(1, 0, 0),                      true),
                ("supportedCommunicationProtocols as an object", json => json["supportedCommunicationProtocols"] = new JObject(),                   true),
                ("clientNodeDescription as a string",        json => json["clientNodeDescription"] = "a node",                                      true),
                ("clientEndpointDescription as an array",    json => json["clientEndpointDescription"] = new JArray(),                              true),

                // Identifiers: S2 Connect node identifications are UUIDs.
                ("an empty clientNodeId",                    json => json["clientNodeId"] = "",                                                     true),
                ("a one character clientNodeId",             json => json["clientNodeId"] = "a",                                                    true),
                ("a 200 character clientNodeId",             json => json["clientNodeId"] = new String('a', 200),                                   true),
                ("a clientNodeId with illegal characters",   json => json["clientNodeId"] = "../../../etc/passwd",                                  true),
                ("a NUL clientNodeId",                       json => json["clientNodeId"] = "\u0000\u0000",                                         true),
                ("an unknown clientNodeId",                  json => json["clientNodeId"] = Node_Id.NewRandom.ToString(),                           true),
                ("an unknown serverNodeId",                  json => json["serverNodeId"] = Node_Id.NewRandom.ToString(),                           true),
                ("the node identifications swapped",         json => {
                                                                        var client = json["clientNodeId"]!.DeepClone();
                                                                        json["clientNodeId"] = json["serverNodeId"]!.DeepClone();
                                                                        json["serverNodeId"] = client;
                                                                     },                                                                             true),

                // Empty and unknown protocol offers.
                ("an empty supportedS2MessageVersions",      json => json["supportedS2MessageVersions"]      = new JArray(),                        true),
                ("an empty supportedCommunicationProtocols", json => json["supportedCommunicationProtocols"] = new JArray(),                        true),
                ("an unknown S2 message version",            json => json["supportedS2MessageVersions"]      = new JArray("v99.0.0"),               true),
                ("an unknown communication protocol",        json => json["supportedCommunicationProtocols"] = new JArray("MQTT"),                  true),
                ("a script as a communication protocol",     json => json["supportedCommunicationProtocols"] = new JArray("<script>alert(1)</script>"), true),
                ("a 10000 character message version",        json => json["supportedS2MessageVersions"]      = new JArray(new String('v', 10000)),  true),

                // Tolerated hostility: the server decides, but never with a server error.
                ("an unknown extra property",                json => json["someUnknownProperty"] = "<script>alert(1)</script>",                      false),
                ("fifty unknown extra properties",           json => { for (var i = 0; i < 50; i++) json[$"extra{i}"] = i; },                       false),
                ("a hundred offered message versions",       json => json["supportedS2MessageVersions"] = new JArray(
                                                                         new[] { Version.S2JSONVersion }.Concat(Enumerable.Range(0, 100).Select(i => $"v{i}.0.0"))), false),
                ("a hundred offered protocols",              json => json["supportedCommunicationProtocols"] = new JArray(
                                                                         new[] { "WebSocket" }.Concat(Enumerable.Range(0, 100).Select(i => $"Protocol{i}"))),        false)

            ];

            var problems = new List<String>();

            foreach (var (name, apply, mustBeRejected) in mutations)
            {

                var json = fixture.InitiateSessionRequest().ToJSON();
                apply(json);

                problems.AddRange(await PostAndCheckAsync(fixture,
                                                          "v1/initiateSession",
                                                          name,
                                                          Body:            json,
                                                          Bearer:          token.Value,
                                                          MustBeRejected:  mustBeRejected,
                                                          Secrets:         new[] { token.Value }));

            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillInitiatesASessionAsync(fixture);

        }

        #endregion


        // confirmAccessToken.

        #region ConfirmAccessToken_ConcurrentDoubleConfirmation_ActivatesTheTokenExactlyOnce()

        [Test]
        public async Task ConfirmAccessToken_ConcurrentDoubleConfirmation_ActivatesTheTokenExactlyOnce()
        {

            await using var fixture = await CreateFixtureAsync();

            var token      = await fixture.PairAsync();

            var initiated  = await fixture.PostAsync("v1/initiateSession", fixture.InitiateSessionRequest().ToJSON(), token.Value);

            Assert.That(initiated.Status, Is.EqualTo(HttpStatusCode.OK), initiated.Body);
            Assert.That(InitiateSessionResponse.TryParse(initiated.Object, out var response, out var error), Is.True, error);

            var pending    = response!.AccessToken;

            // Eight racing confirmations of one pending token: a replay must never hand out a
            // second communication token.
            var tasks      = new List<Task<HTTPResult>>();

            for (var i = 0; i < 8; i++)
                tasks.Add(fixture.PostAsync(confirmRoute, Bearer: pending.Value));

            var stopwatch  = Stopwatch.StartNew();
            var results    = await Task.WhenAll(tasks);

            stopwatch.Stop();

            var problems   = new List<String>();
            var accepted   = 0;

            for (var i = 0; i < results.Length; i++)
            {

                problems.AddRange(S2FuzzCorpus.Violations($"{confirmRoute} / concurrent confirmation {i}", results[i], stopwatch.Elapsed));

                if (results[i].Status == HttpStatusCode.OK)
                    accepted++;

                else if (results[i].Status != HttpStatusCode.Unauthorized)
                    problems.Add($"The concurrent confirmation {i} was answered with {(Int32) results[i].Status}: '{results[i].Body}'.");

            }

            Assert.Multiple(() => {
                Assert.That(problems,                          Is.Empty, String.Join(Environment.NewLine, problems));
                Assert.That(accepted,                          Is.EqualTo(1), "A pending access token may be confirmed exactly once!");
                Assert.That(fixture.ServerStore.PendingCount,  Is.EqualTo(0));
            });

            // A late replay of the very same token is refused as well.
            var replayed = await fixture.PostAsync(confirmRoute, Bearer: pending.Value);

            Assert.That(replayed.Status, Is.EqualTo(HttpStatusCode.Unauthorized), replayed.Body);

        }

        #endregion

        #region ConfirmAccessToken_IgnoresHostileRequestBodies()

        [Test]
        public async Task ConfirmAccessToken_IgnoresHostileRequestBodies()
        {

            await using var fixture = await CreateFixtureAsync();

            var problems = new List<String>();

            // The operation has no request body; a hostile one must neither be parsed nor break it.
            foreach (var (name, body) in S2FuzzCorpus.MalformedBodies().Take(10))
            {

                var token      = await fixture.PairAsync();
                var initiated  = await fixture.PostAsync("v1/initiateSession", fixture.InitiateSessionRequest().ToJSON(), token.Value);

                Assert.That(initiated.Status, Is.EqualTo(HttpStatusCode.OK), initiated.Body);
                Assert.That(InitiateSessionResponse.TryParse(initiated.Object, out var response, out var error), Is.True, error);

                var stopwatch  = Stopwatch.StartNew();
                var result     = await fixture.PostAsync(confirmRoute,
                                                         Bearer:   response!.AccessToken.Value,
                                                         RawBody:  body);

                stopwatch.Stop();

                problems.AddRange(S2FuzzCorpus.Violations($"{confirmRoute} / {name}", result, stopwatch.Elapsed));

                // Whatever the body says, the answer is either the communication details or a
                // clean rejection - never a server error and never a hang.
                if (result.Status is not HttpStatusCode.OK and not HttpStatusCode.BadRequest and not HttpStatusCode.UnsupportedMediaType)
                    problems.Add($"{confirmRoute} / {name}: expected 200 or a clean rejection but got {(Int32) result.Status}: '{result.Body}'.");

            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillInitiatesASessionAsync(fixture);

        }

        #endregion

        #region ConfirmAccessToken_WithAnExpiredPendingToken_IsRejectedWith401()

        [Test]
        public async Task ConfirmAccessToken_WithAnExpiredPendingToken_IsRejectedWith401()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                          ServerOptions: SessionInitiationFixture.DefaultServerOptions() with {
                                                             EnableRateLimiting          = false,
                                                             PendingAccessTokenLifetime  = TimeSpan.FromMilliseconds(300)
                                                         }
                                      );

            var token      = await fixture.PairAsync();
            var initiated  = await fixture.PostAsync("v1/initiateSession", fixture.InitiateSessionRequest().ToJSON(), token.Value);

            Assert.That(initiated.Status, Is.EqualTo(HttpStatusCode.OK), initiated.Body);
            Assert.That(InitiateSessionResponse.TryParse(initiated.Object, out var response, out var error), Is.True, error);

            await Task.Delay(TimeSpan.FromMilliseconds(900));

            var problems = new List<String>();

            // The expired token, and the expired token combined with hostile bodies.
            foreach (var (name, body) in S2FuzzCorpus.MalformedBodies().Take(6))
            {

                var stopwatch  = Stopwatch.StartNew();
                var result     = await fixture.PostAsync(confirmRoute, Bearer: response!.AccessToken.Value, RawBody: body);

                stopwatch.Stop();

                problems.AddRange(S2FuzzCorpus.Violations($"{confirmRoute} / an expired pending token with {name}",
                                                          result,
                                                          stopwatch.Elapsed,
                                                          new[] { response.AccessToken.Value },
                                                          MustBeRejected: true));

            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            // The active token survived: the pairing is intact.
            var again = await fixture.PostAsync("v1/initiateSession", fixture.InitiateSessionRequest().ToJSON(), token.Value);

            Assert.That(again.Status, Is.EqualTo(HttpStatusCode.OK), again.Body);

        }

        #endregion


        // unpair.

        #region HostileUnpairRequests_AreRejectedWithout5xx()

        [Test]
        public async Task HostileUnpairRequests_AreRejectedWithout5xx()
        {

            await using var fixture = await CreateFixtureAsync();

            var token = await fixture.PairAsync();

            (String Name, JObject Body)[] bodies = [
                ("an empty object",                  new JObject()),
                ("only a clientNodeId",              new JObject(new JProperty("clientNodeId", fixture.RM.Id.ToString()))),
                ("only a serverNodeId",              new JObject(new JProperty("serverNodeId", fixture.CEM.Id.ToString()))),
                ("a malformed clientNodeId",         new JObject(new JProperty("clientNodeId", "not-a-uuid"),
                                                                 new JProperty("serverNodeId", fixture.CEM.Id.ToString()))),
                ("an empty clientNodeId",            new JObject(new JProperty("clientNodeId", ""),
                                                                 new JProperty("serverNodeId", fixture.CEM.Id.ToString()))),
                ("a numeric clientNodeId",           new JObject(new JProperty("clientNodeId", 42),
                                                                 new JProperty("serverNodeId", fixture.CEM.Id.ToString()))),
                ("a null serverNodeId",              new JObject(new JProperty("clientNodeId", fixture.RM.Id.ToString()),
                                                                 new JProperty("serverNodeId", JValue.CreateNull()))),
                ("an object as serverNodeId",        new JObject(new JProperty("clientNodeId", fixture.RM.Id.ToString()),
                                                                 new JProperty("serverNodeId", new JObject()))),
                ("a path traversal as clientNodeId", new JObject(new JProperty("clientNodeId", "../../../etc/passwd"),
                                                                 new JProperty("serverNodeId", fixture.CEM.Id.ToString()))),
                ("an unknown clientNodeId",          new JObject(new JProperty("clientNodeId", Node_Id.NewRandom.ToString()),
                                                                 new JProperty("serverNodeId", fixture.CEM.Id.ToString()))),
                ("an unknown serverNodeId",          new JObject(new JProperty("clientNodeId", fixture.RM.Id.ToString()),
                                                                 new JProperty("serverNodeId", Node_Id.NewRandom.ToString()))),
                ("the node identifications swapped", new JObject(new JProperty("clientNodeId", fixture.CEM.Id.ToString()),
                                                                 new JProperty("serverNodeId", fixture.RM.Id.ToString())))
            ];

            var problems = new List<String>();

            foreach (var (name, body) in bodies)
            {
                problems.AddRange(await PostAndCheckAsync(fixture,
                                                          "v1/unpair",
                                                          name,
                                                          Body:            body,
                                                          Bearer:          token.Value,
                                                          MustBeRejected:  true,
                                                          Secrets:         new[] { token.Value }));
            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            // Not one of these hostile requests removed the pairing.
            var pairing = await fixture.ServerStore.GetPairingAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.That(pairing, Is.Not.Null, "A hostile unpair request removed the pairing!");

            await AssertStillInitiatesASessionAsync(fixture);

        }

        #endregion


        // Concurrency and information disclosure.

        #region ConcurrentHostileRequests_LeaveTheSessionServerHealthy()

        [Test]
        public async Task ConcurrentHostileRequests_LeaveTheSessionServerHealthy()
        {

            await using var fixture = await CreateFixtureAsync();

            var token   = await fixture.PairAsync();
            var bodies  = S2FuzzCorpus.MalformedBodies().Take(10).ToList();
            var tasks   = new List<Task<HTTPResult>>();

            foreach (var (_, body) in bodies)
            {
                foreach (var route in bodyRoutes)
                {
                    tasks.Add(fixture.PostAsync(route, RawBody: body, Bearer: token.Value));
                    tasks.Add(fixture.PostAsync(route, RawBody: body));
                }
            }

            var stopwatch  = Stopwatch.StartNew();
            var results    = await Task.WhenAll(tasks);

            stopwatch.Stop();

            var problems = new List<String>();

            for (var i = 0; i < results.Length; i++)
                problems.AddRange(S2FuzzCorpus.Violations($"concurrent hostile request {i}", results[i], stopwatch.Elapsed, MustBeRejected: true));

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            var pairing = await fixture.ServerStore.GetPairingAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.That(pairing, Is.Not.Null, "The concurrent hostile requests removed the pairing!");

            await AssertStillInitiatesASessionAsync(fixture);

        }

        #endregion

        #region ErrorResponses_LeakNeitherInternalsNorSecrets()

        [Test]
        public async Task ErrorResponses_LeakNeitherInternalsNorSecrets()
        {

            await using var fixture = await CreateFixtureAsync();

            var token      = await fixture.PairAsync();
            var initiated  = await fixture.PostAsync("v1/initiateSession", fixture.InitiateSessionRequest().ToJSON(), token.Value);

            Assert.That(initiated.Status, Is.EqualTo(HttpStatusCode.OK), initiated.Body);
            Assert.That(InitiateSessionResponse.TryParse(initiated.Object, out var response, out var error), Is.True, error);

            var secrets = new List<String> {
                              token.Value,
                              response!.AccessToken.Value
                          };

            var problems = new List<String>();

            problems.AddRange(await PostAndCheckAsync(fixture, "v1/initiateSession", "a truncated body",      RawBody: "{\"clientNodeId\":",           Bearer: token.Value,                    MustBeRejected: true, Secrets: secrets));
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/initiateSession", "a wrong content type",  RawBody: "{}", ContentType: "text/plain", Bearer: token.Value,                    MustBeRejected: true, Secrets: secrets));
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/initiateSession", "an empty object",       Body: new JObject(),                      Bearer: token.Value,                    MustBeRejected: true, Secrets: secrets));
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/initiateSession", "an unknown bearer",     Body: fixture.InitiateSessionRequest().ToJSON(), Bearer: TokenGenerator.NewAccessToken().Value, MustBeRejected: true, Secrets: secrets));
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/unpair",          "an empty object",       Body: new JObject(),                      Bearer: token.Value,                    MustBeRejected: true, Secrets: secrets));
            problems.AddRange(await PostAndCheckAsync(fixture, confirmRoute,         "an active bearer",      Bearer: token.Value,                                                              MustBeRejected: true, Secrets: secrets));

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

        }

        #endregion

    }

}

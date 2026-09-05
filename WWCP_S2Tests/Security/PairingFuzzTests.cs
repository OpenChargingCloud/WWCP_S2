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
using System.Net.Http.Headers;
using System.Text;

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Tests.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// Hostile input for the S2 Connect pairing server over real HTTP (PLAN.md §11a): malformed
    /// JSON, wrong content types, raw byte payloads, semantically hostile fields, stolen and
    /// replayed pairing attempt identifications and overlapping pairing attempts against the
    /// same node.
    ///
    /// The invariants asserted for every single request are collected in
    /// <see cref="S2FuzzCorpus.Violations"/>: the server never answers 5xx (a deliberate 503 with
    /// a Retry-After header excepted), never hangs, always sends a complete HTTP response, an
    /// error body is either empty or the S2 error shape, nothing leaks a stack trace, an
    /// exception type, a file path or a secret - and the server still serves a correct request
    /// afterwards.
    ///
    /// The rate limit and the request size limit have their own tests
    /// (<see cref="HardeningSmokeTests"/>); the fixtures here run with the rate limit switched
    /// off, so that the fuzzing is not throttled.
    /// </summary>
    [TestFixture]
    public sealed class PairingFuzzTests
    {

        #region Data

        /// <summary>
        /// The routes that take a JSON object and no authentication.
        /// </summary>
        private static readonly String[] anonymousRoutes = [
            "v1/requestPairing",
            "v1/preparePairing",
            "v1/cancelPreparePairing"
        ];

        /// <summary>
        /// The routes that take a JSON object and a pairingAttemptId as bearer token.
        /// </summary>
        private static readonly String[] authenticatedRoutes = [
            "v1/requestConnectionDetails",
            "v1/postConnectionDetails",
            "v1/finalizePairing"
        ];

        /// <summary>
        /// The long-polling route, which takes a JSON array.
        /// </summary>
        private const String waitForPairingRoute = "v1/waitForPairing";


        /// <summary>
        /// A started pairing attempt.
        /// </summary>
        private sealed record StartedAttempt(HostedNode              ServerNode,
                                             TestPairingClient       Client,
                                             String                  Bearer,
                                             RequestPairingResponse  Response);

        #endregion

        #region (private static) Helpers

        /// <summary>
        /// The options of the fuzzing fixtures: no rate limit (a single test method sends far
        /// more than the 300 requests per minute of the default budget), a short long-polling
        /// timeout (so that a well-formed waitForPairing does not block the test for 25 seconds)
        /// and a generous attempt timeout (so that a long fuzzing loop does not expire the
        /// attempt it is fuzzing).
        /// </summary>
        private static PairingServerOptions FuzzOptions()

            => PairingServerFixture.DefaultOptions() with {
                   EnableRateLimiting     = false,
                   LongPollingTimeout     = TimeSpan.FromMilliseconds(250),
                   PairingAttemptTimeout  = TimeSpan.FromMinutes(5)
               };


        /// <summary>
        /// Start a fresh pairing attempt: a new server node of the given role, targeted by its
        /// identification so that several attempts may live on one endpoint, and a new client
        /// of the opposite role sharing a static pairing token with it.
        /// </summary>
        private static async Task<StartedAttempt> StartAttemptAsync(PairingServerFixture  Fixture,
                                                                    EnergyManagementRole  ServerRole)
        {

            var client      = new TestPairingClient(ServerRole == EnergyManagementRole.CEM
                                                        ? EnergyManagementRole.RM
                                                        : EnergyManagementRole.CEM);

            var serverNode  = Fixture.AddNode(ServerRole);
            serverNode.SetStaticPairingToken(client.Token);

            var result      = await Fixture.PostAsync("v1/requestPairing",
                                                      client.RequestPairing(PairingTarget.ByNodeId(serverNode.Id)).ToJSON());

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK), result.Body);
            Assert.That(RequestPairingResponse.TryParse(result.Object, out var response, out var error), Is.True, error);

            return new StartedAttempt(serverNode, client, response!.PairingAttemptId.Value, response);

        }

        /// <summary>
        /// The correct answer of the client to the server challenge of the given attempt.
        /// </summary>
        private static HmacChallengeResponse CorrectServerResponse(PairingServerFixture  Fixture,
                                                                   StartedAttempt        Attempt)

            => Attempt.Client.ServerChallengeResponse(Fixture, Attempt.Response.ServerHmacChallenge);


        /// <summary>
        /// POST the given body and hand the invariant violations of the answer back.
        /// </summary>
        private static async Task<IReadOnlyList<String>> PostAndCheckAsync(PairingServerFixture  Fixture,
                                                                           String                Route,
                                                                           String                Label,
                                                                           JToken?               Body             = null,
                                                                           String?               RawBody          = null,
                                                                           String                ContentType      = "application/json",
                                                                           String?               Bearer           = null,
                                                                           Boolean               MustBeRejected   = false,
                                                                           IEnumerable<String>?  Secrets          = null)
        {

            var stopwatch  = Stopwatch.StartNew();

            var result     = await Fixture.PostAsync(Route,
                                                     Body,
                                                     Bearer,
                                                     ContentType,
                                                     RawBody);

            stopwatch.Stop();

            return S2FuzzCorpus.Violations($"{Route} / {Label}",
                                           result,
                                           stopwatch.Elapsed,
                                           Secrets,
                                           MustBeRejected);

        }

        /// <summary>
        /// Prove that the pairing server is undamaged: it completes a whole pairing
        /// (branch B: the server node is the communication client) and stores it.
        /// </summary>
        private static async Task AssertStillCompletesAPairingAsync(PairingServerFixture Fixture)
        {

            var started    = await StartAttemptAsync(Fixture, EnergyManagementRole.RM);
            var details    = started.Client.ConnectionDetails();

            var posted     = await Fixture.PostAsync("v1/postConnectionDetails",
                                                     new PostConnectionDetailsRequest(CorrectServerResponse(Fixture, started), details).ToJSON(),
                                                     started.Bearer);

            var finalized  = await Fixture.PostAsync("v1/finalizePairing",
                                                     new FinalizePairingRequest(true).ToJSON(),
                                                     started.Bearer);

            var pairing    = await Fixture.Store.GetPairingAsync(started.ServerNode.Id, started.Client.Node.Id);

            Assert.Multiple(() => {
                Assert.That(posted.Status,     Is.EqualTo(HttpStatusCode.NoContent), posted.Body);
                Assert.That(finalized.Status,  Is.EqualTo(HttpStatusCode.NoContent), finalized.Body);
                Assert.That(pairing,           Is.Not.Null, "The pairing server no longer stores a completed pairing!");
            });

        }

        #endregion


        // Malformed JSON on every route.

        #region MalformedBodies_OnEveryPairingRoute_AreRejectedWith4xx()

        [Test]
        public async Task MalformedBodies_OnEveryPairingRoute_AreRejectedWith4xx()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            // One authenticated attempt, so that the bodies of the bearer-protected routes really
            // reach the JSON parser; a ParsingError never touches the state of the attempt.
            var started   = await StartAttemptAsync(fixture, EnergyManagementRole.CEM);
            var problems  = new List<String>();

            foreach (var (name, body) in S2FuzzCorpus.MalformedBodies())
            {

                foreach (var route in anonymousRoutes)
                    problems.AddRange(await PostAndCheckAsync(fixture, route, name, RawBody: body, MustBeRejected: true));

                foreach (var route in authenticatedRoutes)
                    problems.AddRange(await PostAndCheckAsync(fixture, route, name, RawBody: body, Bearer: started.Bearer, MustBeRejected: true));

                problems.AddRange(await PostAndCheckAsync(fixture, waitForPairingRoute, name, RawBody: body, MustBeRejected: true));

            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillCompletesAPairingAsync(fixture);

        }

        #endregion

        #region OversizedAndDeeplyNestedBodies_AreRejectedWith4xx()

        [Test]
        public async Task OversizedAndDeeplyNestedBodies_AreRejectedWith4xx()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            fixture.AddNode(EnergyManagementRole.RM);

            var problems = new List<String>();

            foreach (var (name, body) in S2FuzzCorpus.StructurallyHostileBodies())
            {
                problems.AddRange(await PostAndCheckAsync(fixture, "v1/requestPairing",       name, RawBody: body, MustBeRejected: true));
                problems.AddRange(await PostAndCheckAsync(fixture, "v1/preparePairing",       name, RawBody: body, MustBeRejected: true));
                problems.AddRange(await PostAndCheckAsync(fixture, waitForPairingRoute,       name, RawBody: body, MustBeRejected: true));
            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillCompletesAPairingAsync(fixture);

        }

        #endregion

        #region HostileContentTypes_AreAnsweredWithoutAServerError()

        [Test]
        public async Task HostileContentTypes_AreAnsweredWithoutAServerError()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            var client    = new TestPairingClient(EnergyManagementRole.CEM);
            var rm        = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var valid     = client.RequestPairing().ToJSON().ToString();
            var problems  = new List<String>();

            // A content type that is not "application/json" must be refused before the body is touched.
            foreach (var contentType in S2FuzzCorpus.RejectedContentTypes)
            {
                problems.AddRange(await PostAndCheckAsync(fixture, "v1/requestPairing", $"content type '{contentType}'", RawBody: valid,  ContentType: contentType, MustBeRejected: true));
                problems.AddRange(await PostAndCheckAsync(fixture, "v1/requestPairing", $"content type '{contentType}' with an empty body", RawBody: "", ContentType: contentType, MustBeRejected: true));
            }

            // An empty body is a parse error, whatever the content type says.
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/requestPairing",       "an empty body", RawBody: "", MustBeRejected: true));
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/preparePairing",       "an empty body", RawBody: "", MustBeRejected: true));
            problems.AddRange(await PostAndCheckAsync(fixture, waitForPairingRoute,       "an empty body", RawBody: "", MustBeRejected: true));

            // No body at all (no Content-Type, no content) must not hang either.
            foreach (var route in anonymousRoutes.Concat(new[] { waitForPairingRoute }))
            {

                var stopwatch  = Stopwatch.StartNew();
                var result     = await S2FuzzCorpus.PostRawAsync(fixture.Client, route);

                stopwatch.Stop();

                if (result is null)
                    problems.Add($"{route}: the HTTP client refused to send a request without a body!");
                else
                    problems.AddRange(S2FuzzCorpus.Violations($"{route} / no body at all", result, stopwatch.Elapsed, MustBeRejected: true));

            }

            // A valid body without any Content-Type header: whatever the server decides, it must
            // decide - and it must not answer with a server error.
            var bare = await S2FuzzCorpus.PostRawAsync(fixture.Client, "v1/requestPairing", Encoding.UTF8.GetBytes(valid));

            if (bare is null)
                problems.Add("The HTTP client refused to send a body without a content type!");
            else
                problems.AddRange(S2FuzzCorpus.Violations("v1/requestPairing / no content type", bare, TimeSpan.Zero));

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillCompletesAPairingAsync(fixture);

        }

        #endregion

        #region RawByteBodies_WithBOM_NUL_AndInvalidUTF8_AreAnsweredWithoutAServerError()

        [Test]
        public async Task RawByteBodies_WithBOM_NUL_AndInvalidUTF8_AreAnsweredWithoutAServerError()
        {

            // The fixture re-encodes every string body as UTF-8, so byte level hostility needs
            // its own request: a byte order mark, broken UTF-8 sequences, embedded NUL bytes and
            // a UTF-16 document.

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            fixture.AddNode(EnergyManagementRole.RM);

            var problems = new List<String>();

            foreach (var (name, bytes, contentType) in S2FuzzCorpus.HostileByteBodies())
            {
                foreach (var route in new[] { "v1/requestPairing", "v1/cancelPreparePairing", waitForPairingRoute })
                {

                    var stopwatch  = Stopwatch.StartNew();
                    var result     = await S2FuzzCorpus.PostRawAsync(fixture.Client, route, bytes, contentType);

                    stopwatch.Stop();

                    if (result is null)
                        problems.Add($"{route} / {name}: the HTTP client refused to send the request!");
                    else
                        problems.AddRange(S2FuzzCorpus.Violations($"{route} / {name}", result, stopwatch.Elapsed));

                }
            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillCompletesAPairingAsync(fixture);

        }

        #endregion


        // Semantic hostility: well-formed JSON with hostile content.

        #region HostileRequestPairingFields_AreAnsweredWithoutAServerError()

        [Test]
        public async Task HostileRequestPairingFields_AreAnsweredWithoutAServerError()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            var client  = new TestPairingClient(EnergyManagementRole.CEM);
            var rm      = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            // (name, mutation, must be rejected)
            (String Name, Action<JObject> Apply, Boolean MustBeRejected)[] mutations = [

                // Missing mandatory properties.
                ("without clientNodeDescription",        json => json.Remove("clientNodeDescription"),                              true),
                ("without clientEndpointDescription",    json => json.Remove("clientEndpointDescription"),                          true),
                ("without supportedCommunicationProtocols", json => json.Remove("supportedCommunicationProtocols"),                 true),
                ("without supportedS2MessageVersions",   json => json.Remove("supportedS2MessageVersions"),                         true),
                ("without supportedHmacHashingAlgorithms", json => json.Remove("supportedHmacHashingAlgorithms"),                   true),
                ("without clientHmacChallenge",          json => json.Remove("clientHmacChallenge"),                                true),

                // Wrong JSON types.
                ("clientNodeDescription as a string",    json => json["clientNodeDescription"]     = "node",                        true),
                ("clientNodeDescription as an array",    json => json["clientNodeDescription"]     = new JArray(),                  true),
                ("clientEndpointDescription as a number", json => json["clientEndpointDescription"] = 42,                           true),
                ("supportedCommunicationProtocols as a string", json => json["supportedCommunicationProtocols"] = "WebSocket",      true),
                ("supportedS2MessageVersions as an object", json => json["supportedS2MessageVersions"] = new JObject(),             true),
                ("supportedHmacHashingAlgorithms as a number", json => json["supportedHmacHashingAlgorithms"] = 256,                true),
                ("clientHmacChallenge as a number",      json => json["clientHmacChallenge"]       = 12345,                         true),
                ("clientHmacChallenge as an object",     json => json["clientHmacChallenge"]       = new JObject(),                 true),

                // Empty values.
                ("an empty clientNodeDescription",       json => json["clientNodeDescription"]     = new JObject(),                 true),
                ("an empty supportedCommunicationProtocols", json => json["supportedCommunicationProtocols"] = new JArray(),        true),
                ("an empty supportedS2MessageVersions",  json => json["supportedS2MessageVersions"] = new JArray(),                 true),
                ("an empty supportedHmacHashingAlgorithms", json => json["supportedHmacHashingAlgorithms"] = new JArray(),          true),
                ("an empty clientHmacChallenge",         json => json["clientHmacChallenge"]       = "",                            true),

                // Identifiers: S2 Connect node identifications are UUIDs.
                ("an empty client node id",              json => json["clientNodeDescription"]!["id"] = "",                          true),
                ("a one character client node id",       json => json["clientNodeDescription"]!["id"] = "a",                         true),
                ("a 100 character client node id",       json => json["clientNodeDescription"]!["id"] = new String('a', 100),        true),
                ("a client node id with illegal characters", json => json["clientNodeDescription"]!["id"] = "no de:id!",             true),
                ("a numeric client node id",             json => json["clientNodeDescription"]!["id"] = 42,                          true),
                ("a NUL client node id",                 json => json["clientNodeDescription"]!["id"] = "\u0000\u0000",              true),
                ("an unknown client role",               json => json["clientNodeDescription"]!["role"] = "OVERLORD",                true),
                ("an unknown client deployment",         json => json["clientEndpointDescription"]!["deployment"] = "MOON",          true),

                // The pairing target.
                ("an unknown nodeId",                    json => json["nodeId"]      = Node_Id.NewRandom.ToString(),                 true),
                ("a malformed nodeId",                   json => json["nodeId"]      = "not-a-uuid",                                 true),
                ("a numeric nodeId",                     json => json["nodeId"]      = 7,                                           true),
                ("an empty nodeIdAlias",                 json => json["nodeIdAlias"] = "",                                          true),
                ("a nodeIdAlias with illegal characters", json => json["nodeIdAlias"] = "A/B",                                      true),
                ("a 10000 character nodeIdAlias",        json => json["nodeIdAlias"] = new String('A', 10000),                       true),

                // The challenge.
                ("a challenge that is not Base64",       json => json["clientHmacChallenge"] = "!!!!",                               true),
                ("a challenge of a single byte",         json => json["clientHmacChallenge"] = "AA==",                               true),
                ("a challenge with white space",         json => json["clientHmacChallenge"] = "AAAA AAAA",                          true),
                ("a 100000 character challenge",         json => json["clientHmacChallenge"] = new String('A', 100000),              true),

                // forcePairing.
                ("forcePairing as a string",             json => json["forcePairing"] = "true",                                     true),
                ("forcePairing as a number",             json => json["forcePairing"] = 1,                                          true),
                ("forcePairing as an array",             json => json["forcePairing"] = new JArray(true),                           true),
                ("forcePairing as an object",            json => json["forcePairing"] = new JObject(),                              true),

                // Tolerated hostility: the server decides, but it must decide without a server error.
                ("forcePairing as null",                 json => json["forcePairing"] = JValue.CreateNull(),                        false),
                ("an unknown extra property",            json => json["someUnknownProperty"] = "<script>alert(1)</script>",         false),
                ("fifty unknown extra properties",       json => { for (var i = 0; i < 50; i++) json[$"extra{i}"] = i; },           false),
                ("a script in the endpoint name",        json => json["clientEndpointDescription"]!["name"] = "<script>alert(1)</script>", false),
                ("a 10000 character brand",              json => json["clientNodeDescription"]!["brand"] = new String('b', 10000),  false),
                ("a hundred offered S2 message versions", json => json["supportedS2MessageVersions"] = new JArray(Enumerable.Range(0, 100).Select(i => $"v{i}.0.0")), false)

            ];

            var problems = new List<String>();

            foreach (var (name, apply, mustBeRejected) in mutations)
            {

                var json = client.RequestPairing().ToJSON();
                apply(json);

                problems.AddRange(await PostAndCheckAsync(fixture, "v1/requestPairing", name, Body: json, MustBeRejected: mustBeRejected));

            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillCompletesAPairingAsync(fixture);

        }

        #endregion

        #region HostileChallengeResponses_AreRejectedWithout5xx()

        [Test]
        public async Task HostileChallengeResponses_AreRejectedWithout5xx()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            (String Name, Func<HmacChallengeResponse, JToken> Value)[] mutations = [
                ("a missing challenge response",             _        => JValue.CreateNull()),
                ("an empty challenge response",              _        => ""),
                ("a challenge response that is not Base64",  _        => "!!!!!!!!"),
                ("a challenge response with white space",    _        => "AAAA AAAA"),
                ("a numeric challenge response",             _        => 42),
                ("an object challenge response",             _        => new JObject()),
                ("an array challenge response",              _        => new JArray()),
                ("a single byte challenge response",         _        => "AA=="),
                ("a truncated challenge response",           correct  => correct.Value[..8]),
                ("a doubled challenge response",             correct  => correct.Value + correct.Value),
                ("a 100000 character challenge response",    _        => new String('A', 100000)),
                ("the answer to another challenge",          _        => ChallengeResponse.ComputeForLAN(HmacHashingAlgorithm.SHA256,
                                                                                                         TokenGenerator.NewChallenge(),
                                                                                                         PairingToken.Parse("ABCD2345"),
                                                                                                         PairingServerFixture.Fingerprint).Value)
            ];

            var problems = new List<String>();

            foreach (var (name, value) in mutations)
            {

                // Every mutation gets its own attempt: a wrong (but well-formed) response
                // legitimately fails the attempt, and a failed attempt answers 401 afterwards.
                var started  = await StartAttemptAsync(fixture, EnergyManagementRole.CEM);
                var json     = new JObject(
                                   new JProperty("serverHmacChallengeResponse", value(CorrectServerResponse(fixture, started)))
                               );

                problems.AddRange(await PostAndCheckAsync(fixture,
                                                          "v1/requestConnectionDetails",
                                                          name,
                                                          Body:            json,
                                                          Bearer:          started.Bearer,
                                                          MustBeRejected:  true,
                                                          Secrets:         new[] { started.Client.Token.ToString() }));

            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillCompletesAPairingAsync(fixture);

        }

        #endregion

        #region HostileConnectionDetails_AreRejectedWithout5xx()

        [Test]
        public async Task HostileConnectionDetails_AreRejectedWithout5xx()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            (String Name, Action<JObject> Apply, Boolean MustBeRejected)[] mutations = [
                ("without connectionDetails",              json => json.Remove("connectionDetails"),                                                                        true),
                ("connectionDetails as a string",          json => json["connectionDetails"] = "http://127.0.0.1/",                                                         true),
                ("connectionDetails as an array",          json => json["connectionDetails"] = new JArray(),                                                                true),
                ("empty connectionDetails",                json => json["connectionDetails"] = new JObject(),                                                               true),
                ("without an initiateSessionUrl",          json => ((JObject) json["connectionDetails"]!).Remove("initiateSessionUrl"),                                     true),
                ("an empty initiateSessionUrl",            json => json["connectionDetails"]!["initiateSessionUrl"] = "",                                                   true),
                ("a javascript initiateSessionUrl",        json => json["connectionDetails"]!["initiateSessionUrl"] = "javascript:alert(1)",                                true),
                ("a file initiateSessionUrl",              json => json["connectionDetails"]!["initiateSessionUrl"] = "file:///etc/passwd",                                 true),
                ("a 100000 character initiateSessionUrl",  json => json["connectionDetails"]!["initiateSessionUrl"] = "http://127.0.0.1/" + new String('p', 100000),         true),
                ("a numeric initiateSessionUrl",           json => json["connectionDetails"]!["initiateSessionUrl"] = 8443,                                                 true),
                ("without an accessToken",                 json => ((JObject) json["connectionDetails"]!).Remove("accessToken"),                                            true),
                ("an empty accessToken",                   json => json["connectionDetails"]!["accessToken"] = "",                                                          true),
                // "should have a minimum length of 32 bytes" is a recommendation, not a rule:
                // the server accepts such a token and logs it as weak (see PairingServerAPI).
                ("a short accessToken",                    json => json["connectionDetails"]!["accessToken"] = "AA==",                                                      false),
                ("an accessToken that is not Base64",      json => json["connectionDetails"]!["accessToken"] = "!!!!!!!!",                                                  true),
                ("a numeric accessToken",                  json => json["connectionDetails"]!["accessToken"] = 42,                                                          true),
                ("a certificateFingerprint array",         json => json["connectionDetails"]!["certificateFingerprint"] = new JArray("AA:BB"),                              true),
                ("a malformed certificateFingerprint",     json => json["connectionDetails"]!["certificateFingerprint"] = new JObject(new JProperty("SHA256", "not a fingerprint")), true),
                ("a numeric certificateFingerprint",       json => json["connectionDetails"]!["certificateFingerprint"] = new JObject(new JProperty("SHA256", 42)),         true),
                // An empty fingerprint map is well-formed; whether the pairing may complete
                // without a pinned certificate is a protocol decision, not a parse error.
                ("an empty certificateFingerprint map",    json => json["connectionDetails"]!["certificateFingerprint"] = new JObject(),                                    false)
            ];

            var problems = new List<String>();

            foreach (var (name, apply, mustBeRejected) in mutations)
            {

                var started  = await StartAttemptAsync(fixture, EnergyManagementRole.RM);
                var json     = new PostConnectionDetailsRequest(CorrectServerResponse(fixture, started),
                                                                started.Client.ConnectionDetails()).ToJSON();
                apply(json);

                problems.AddRange(await PostAndCheckAsync(fixture,
                                                          "v1/postConnectionDetails",
                                                          name,
                                                          Body:            json,
                                                          Bearer:          started.Bearer,
                                                          MustBeRejected:  mustBeRejected));

            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillCompletesAPairingAsync(fixture);

        }

        #endregion

        #region HostileLANOperationBodies_AreRejectedWithout5xx()

        [Test]
        public async Task HostileLANOperationBodies_AreRejectedWithout5xx()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            var node      = fixture.AddNode(EnergyManagementRole.RM);
            var problems  = new List<String>();

            // preparePairing: clientNodeDescription, clientEndpointDescription, serverNodeId.
            (String Name, JObject Body)[] preparePairings = [
                ("an empty object",                     new JObject()),
                ("only a serverNodeId",                 new JObject(new JProperty("serverNodeId", node.Id.ToString()))),
                ("a malformed serverNodeId",            new JObject(new JProperty("serverNodeId", "not-a-uuid"))),
                ("a numeric serverNodeId",              new JObject(new JProperty("serverNodeId", 42))),
                ("an unknown serverNodeId",             new JObject(new JProperty("serverNodeId",              Node_Id.NewRandom.ToString()),
                                                                    new JProperty("clientNodeDescription",     new JObject()),
                                                                    new JProperty("clientEndpointDescription", new JObject()))),
                ("a 100000 character serverNodeId",     new JObject(new JProperty("serverNodeId", new String('a', 100000)))),
                ("descriptions of the wrong type",      new JObject(new JProperty("serverNodeId",              node.Id.ToString()),
                                                                    new JProperty("clientNodeDescription",     "a node"),
                                                                    new JProperty("clientEndpointDescription", new JArray())))
            ];

            foreach (var (name, body) in preparePairings)
                problems.AddRange(await PostAndCheckAsync(fixture, "v1/preparePairing", name, Body: body, MustBeRejected: true));

            // cancelPreparePairing: clientNodeId and serverNodeId.
            (String Name, JObject Body)[] cancellations = [
                ("an empty object",                     new JObject()),
                ("only a clientNodeId",                 new JObject(new JProperty("clientNodeId", Node_Id.NewRandom.ToString()))),
                ("a malformed clientNodeId",            new JObject(new JProperty("clientNodeId", "../../etc/passwd"),
                                                                    new JProperty("serverNodeId", node.Id.ToString()))),
                ("an empty clientNodeId",               new JObject(new JProperty("clientNodeId", ""),
                                                                    new JProperty("serverNodeId", node.Id.ToString()))),
                ("a boolean serverNodeId",              new JObject(new JProperty("clientNodeId", Node_Id.NewRandom.ToString()),
                                                                    new JProperty("serverNodeId", true))),
                ("a null clientNodeId",                 new JObject(new JProperty("clientNodeId", JValue.CreateNull()),
                                                                    new JProperty("serverNodeId", node.Id.ToString())))
            ];

            foreach (var (name, body) in cancellations)
                problems.AddRange(await PostAndCheckAsync(fixture, "v1/cancelPreparePairing", name, Body: body, MustBeRejected: true));

            // waitForPairing: an array of items with a clientNodeId.
            (String Name, JArray Body)[] waits = [
                ("an array of numbers",                 new JArray(1, 2, 3)),
                ("an array of strings",                 new JArray("a", "b")),
                ("an array of empty objects",           new JArray(new JObject())),
                ("an item with a malformed clientNodeId", new JArray(new JObject(new JProperty("clientNodeId", "not-a-uuid")))),
                ("an item with a numeric clientNodeId", new JArray(new JObject(new JProperty("clientNodeId", 42)))),
                ("an item with an unknown errorMessage", new JArray(new JObject(new JProperty("clientNodeId", Node_Id.NewRandom.ToString()),
                                                                                new JProperty("errorMessage", "NOT_AN_ERROR")))),
                ("an item with a nested array",         new JArray(new JObject(new JProperty("clientNodeId", new JArray()))))
            ];

            foreach (var (name, body) in waits)
                problems.AddRange(await PostAndCheckAsync(fixture, waitForPairingRoute, name, Body: body, MustBeRejected: true));

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillCompletesAPairingAsync(fixture);

        }

        #endregion


        // The pairingAttemptId as a bearer token.

        #region HostilePairingAttemptIds_AreRejectedWith401()

        [Test]
        public async Task HostilePairingAttemptIds_AreRejectedWith401()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            var started   = await StartAttemptAsync(fixture, EnergyManagementRole.CEM);
            var correct   = CorrectServerResponse(fixture, started);
            var body      = new RequestConnectionDetailsRequest(correct).ToJSON();
            var problems  = new List<String>();

            // A header holding a character that is not allowed in an HTTP field value is refused
            // by the HTTP layer itself, which answers with its own (non-S2) error body; the S2
            // error shape can only be demanded of the answers this API produces.
            (String Name, String Header, Boolean MalformedAtTheTransport)[] headers = [
                ("the Basic scheme",                      "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:password")), false),
                ("the Digest scheme",                     "Digest username=\"a\", realm=\"b\"",                                       false),
                ("no scheme at all",                      started.Bearer,                                                            false),
                ("an empty bearer",                       "Bearer ",                                                                 false),
                ("a bearer of blanks",                    "Bearer " + new String(' ', 40),                                           false),
                ("a bearer with a tab",                   "Bearer " + started.Bearer[..16] + "\t" + started.Bearer[17..],             true),
                ("a bearer with a DEL character",         "Bearer " + started.Bearer[..16] + "\u007F" + started.Bearer[17..],         true),
                ("a 100000 character bearer",             "Bearer " + new String('A', 100000),                                       true),
                ("a bearer with a percent escape",        "Bearer " + started.Bearer[..16] + "%00" + started.Bearer[19..],            false),
                ("a bearer with a path traversal",        "Bearer ../../../../etc/passwd/../../../../../../../",                     false),
                ("a bearer with a SQL fragment",          "Bearer ' OR 1=1 --                                  ",                    false),
                ("a bearer of the right length",          "Bearer " + new String('A', started.Bearer.Length),                        false),
                ("a bearer with one character appended",  "Bearer " + started.Bearer + "A",                                          false),
                ("two bearer tokens",                     "Bearer " + started.Bearer + " " + started.Bearer,                         false)
            ];

            foreach (var (name, header, malformedAtTheTransport) in headers)
            {

                var stopwatch  = Stopwatch.StartNew();
                var result     = await S2FuzzCorpus.PostRawAsync(fixture.Client,
                                                                 "v1/requestConnectionDetails",
                                                                 Encoding.UTF8.GetBytes(body.ToString()),
                                                                 "application/json; charset=utf-8",
                                                                 header);

                stopwatch.Stop();

                // Some of these header values are refused by the local HTTP client itself;
                // that is a client concern, not a finding about the server.
                if (result is null)
                    continue;

                problems.AddRange(S2FuzzCorpus.Violations($"v1/requestConnectionDetails / {name}",
                                                          result,
                                                          stopwatch.Elapsed,
                                                          new[] { started.Bearer },
                                                          MustBeRejected:   true,
                                                          CheckErrorShape:  !malformedAtTheTransport));

                // A pairingAttemptId that is missing, malformed or unknown is a 401; a header the
                // HTTP layer itself cannot make sense of may also be a 400 - but never anything
                // that advances the attempt. What that layer refuses outright (a control character,
                // a header section beyond its bound) carries its own status and is not ours to fix.
                if (!malformedAtTheTransport &&
                    result.Status is not HttpStatusCode.Unauthorized and not HttpStatusCode.BadRequest)
                    problems.Add($"v1/requestConnectionDetails / {name}: expected 401 or 400 but got {(Int32) result.Status}.");

            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            // The scheme of an Authorization header is case-insensitive (RFC 9110, 11.1), so a
            // lower case "bearer" is a correct request and must be served, not refused.
            var lowerCase = await S2FuzzCorpus.PostRawAsync(fixture.Client,
                                                            "v1/requestConnectionDetails",
                                                            Encoding.UTF8.GetBytes(body.ToString()),
                                                            "application/json; charset=utf-8",
                                                            "bearer " + started.Bearer);

            Assert.That(lowerCase?.Status, Is.EqualTo(HttpStatusCode.OK), lowerCase?.Body);

            // None of that touched the attempt: the correct request still succeeds.
            var accepted = await fixture.PostAsync("v1/requestConnectionDetails", body, started.Bearer);

            Assert.That(accepted.Status, Is.EqualTo(HttpStatusCode.OK), accepted.Body);

        }

        #endregion

        #region PairingAttemptId_OfAnotherAttempt_CannotAdvanceThisOne()

        [Test]
        public async Task PairingAttemptId_OfAnotherAttempt_CannotAdvanceThisOne()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            var first   = await StartAttemptAsync(fixture, EnergyManagementRole.CEM);
            var second  = await StartAttemptAsync(fixture, EnergyManagementRole.CEM);

            Assert.That(first.Bearer, Is.Not.EqualTo(second.Bearer));

            // The answer of the first client to ITS challenge, replayed against the second attempt.
            var replayed = await fixture.PostAsync("v1/requestConnectionDetails",
                                                   new RequestConnectionDetailsRequest(CorrectServerResponse(fixture, first)).ToJSON(),
                                                   second.Bearer);

            var problems = new List<String>(S2FuzzCorpus.Violations("v1/requestConnectionDetails / a replayed challenge response",
                                                                    replayed,
                                                                    TimeSpan.Zero,
                                                                    new[] { first.Bearer, second.Bearer },
                                                                    MustBeRejected: true));

            Assert.Multiple(() => {
                Assert.That(problems,        Is.Empty, String.Join(Environment.NewLine, problems));
                Assert.That(replayed.Status, Is.EqualTo(HttpStatusCode.Forbidden), replayed.Body);
            });

            // The first attempt is untouched and still completes.
            var accepted = await fixture.PostAsync("v1/requestConnectionDetails",
                                                   new RequestConnectionDetailsRequest(CorrectServerResponse(fixture, first)).ToJSON(),
                                                   first.Bearer);

            Assert.That(accepted.Status, Is.EqualTo(HttpStatusCode.OK), accepted.Body);

            // The second attempt is used up: even its own correct answer is too late now.
            var tooLate = await fixture.PostAsync("v1/requestConnectionDetails",
                                                  new RequestConnectionDetailsRequest(CorrectServerResponse(fixture, second)).ToJSON(),
                                                  second.Bearer);

            Assert.That(tooLate.Status, Is.EqualTo(HttpStatusCode.Unauthorized), tooLate.Body);

        }

        #endregion

        #region ExpiredAndCompletedPairingAttemptIds_SurviveHostileBodies()

        [Test]
        public async Task ExpiredAndCompletedPairingAttemptIds_SurviveHostileBodies()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                          Options: FuzzOptions() with { PairingAttemptTimeout = TimeSpan.FromSeconds(2) }
                                      );

            var expiring  = await StartAttemptAsync(fixture, EnergyManagementRole.RM);

            // A completed attempt (branch B, finalised) and an expired one.
            var completed = await StartAttemptAsync(fixture, EnergyManagementRole.RM);
            var posted    = await fixture.PostAsync("v1/postConnectionDetails",
                                                    new PostConnectionDetailsRequest(CorrectServerResponse(fixture, completed),
                                                                                     completed.Client.ConnectionDetails()).ToJSON(),
                                                    completed.Bearer);

            var finalized = await fixture.PostAsync("v1/finalizePairing", new FinalizePairingRequest(true).ToJSON(), completed.Bearer);

            Assert.Multiple(() => {
                Assert.That(posted.Status,     Is.EqualTo(HttpStatusCode.NoContent), posted.Body);
                Assert.That(finalized.Status,  Is.EqualTo(HttpStatusCode.NoContent), finalized.Body);
            });

            // Let the first attempt run into its two second timeout.
            await Task.Delay(TimeSpan.FromMilliseconds(2600));

            var problems = new List<String>();

            foreach (var (label, bearer) in new[] { ("an expired", expiring.Bearer), ("a completed", completed.Bearer) })
            {
                foreach (var (name, rawBody) in S2FuzzCorpus.MalformedBodies().Take(12))
                {
                    foreach (var route in authenticatedRoutes)
                    {

                        var violations = await PostAndCheckAsync(fixture,
                                                                 route,
                                                                 $"{label} pairingAttemptId with {name}",
                                                                 RawBody:         rawBody,
                                                                 Bearer:          bearer,
                                                                 MustBeRejected:  true,
                                                                 Secrets:         new[] { bearer });

                        problems.AddRange(violations);

                    }
                }
            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillCompletesAPairingAsync(fixture);

        }

        #endregion


        // Concurrency: the specification requires sequential handling per node.

        #region ConcurrentPairingAttemptsForOneNode_AreAllAnsweredConsistently()

        [Test]
        public async Task ConcurrentPairingAttemptsForOneNode_AreAllAnsweredConsistently()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            var rm       = fixture.AddNode(EnergyManagementRole.RM);
            var clients  = new List<TestPairingClient>();
            var tasks    = new List<Task<HTTPResult>>();

            for (var i = 0; i < 16; i++)
            {

                var client = new TestPairingClient(EnergyManagementRole.CEM);
                clients.Add(client);

                // All clients share the one static pairing token of the node.
                rm.SetStaticPairingToken(client.Token);

                tasks.Add(fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON()));

            }

            var stopwatch  = Stopwatch.StartNew();
            var results    = await Task.WhenAll(tasks);

            stopwatch.Stop();

            var problems   = new List<String>();
            var accepted   = new List<RequestPairingResponse>();

            for (var i = 0; i < results.Length; i++)
            {

                var result = results[i];

                problems.AddRange(S2FuzzCorpus.Violations($"v1/requestPairing / concurrent request {i}", result, stopwatch.Elapsed));

                if (result.Status == HttpStatusCode.OK)
                {

                    if (!RequestPairingResponse.TryParse(result.Object, out var response, out var error))
                        problems.Add($"The accepted concurrent request {i} carries an unparsable response: {error}");

                    else
                    {

                        accepted.Add(response);

                        // The answer to the client challenge must be the one THIS client expects.
                        if (!response.ClientHmacChallengeResponse.ConstantTimeEquals(clients[i].ExpectedClientChallengeResponse(fixture)))
                            problems.Add($"The accepted concurrent request {i} was answered with the challenge response of another client!");

                        if (response.ServerNodeDescription.Id != rm.Id)
                            problems.Add($"The accepted concurrent request {i} names another server node!");

                    }

                }

                else if (result.Status == HttpStatusCode.ServiceUnavailable)
                {
                    if (result.RetryAfter is null)
                        problems.Add($"The refused concurrent request {i} carries no Retry-After header!");
                }

                else
                    problems.Add($"The concurrent request {i} was answered with {(Int32) result.Status}: '{result.Body}'.");

            }

            Assert.Multiple(() => {

                Assert.That(problems,                                             Is.Empty, String.Join(Environment.NewLine, problems));
                Assert.That(accepted,                                             Is.Not.Empty, "Not a single one of 16 concurrent pairing attempts was accepted!");
                Assert.That(accepted.Select(response => response.PairingAttemptId.Value).Distinct().Count(),
                                                                                  Is.EqualTo(accepted.Count),
                                                                                  "Two concurrent pairing attempts share one pairingAttemptId!");
                Assert.That(fixture.API.ActiveAttempts,                           Has.Count.EqualTo(accepted.Count));

            });

            await AssertStillCompletesAPairingAsync(fixture);

        }

        #endregion

        #region ConcurrentHostileRequests_LeaveTheServerHealthy()

        [Test]
        public async Task ConcurrentHostileRequests_LeaveTheServerHealthy()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            fixture.AddNode(EnergyManagementRole.RM);

            var bodies  = S2FuzzCorpus.MalformedBodies().Take(10).ToList();
            var tasks   = new List<Task<HTTPResult>>();

            // Every route is hit by every malformed body at the same time.
            foreach (var (_, body) in bodies)
            {
                foreach (var route in anonymousRoutes.Concat(new[] { waitForPairingRoute }))
                    tasks.Add(fixture.PostAsync(route, RawBody: body));
            }

            var stopwatch  = Stopwatch.StartNew();
            var results    = await Task.WhenAll(tasks);

            stopwatch.Stop();

            var problems = new List<String>();

            for (var i = 0; i < results.Length; i++)
                problems.AddRange(S2FuzzCorpus.Violations($"concurrent hostile request {i}", results[i], stopwatch.Elapsed, MustBeRejected: true));

            Assert.Multiple(() => {
                Assert.That(problems,               Is.Empty, String.Join(Environment.NewLine, problems));
                Assert.That(fixture.API.IsShutdown, Is.False, "The pairing server shut itself down!");
            });

            await AssertStillCompletesAPairingAsync(fixture);

        }

        #endregion


        // Information disclosure.

        #region ErrorResponses_LeakNeitherInternalsNorSecrets()

        [Test]
        public async Task ErrorResponses_LeakNeitherInternalsNorSecrets()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            var started   = await StartAttemptAsync(fixture, EnergyManagementRole.CEM);
            var secrets   = new List<String> {
                                started.Bearer,
                                started.Client.Token.ToString(),
                                started.Client.Challenge.Value,
                                started.Response.ServerHmacChallenge.Value
                            };

            var problems  = new List<String>();

            // Error answers of every kind: parse errors, protocol errors, authentication errors.
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/requestPairing",           "a truncated body",             RawBody: "{\"clientNodeDescription\":",  MustBeRejected: true, Secrets: secrets));
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/requestPairing",           "a wrong content type",         RawBody: "{}", ContentType: "text/plain", MustBeRejected: true, Secrets: secrets));
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/requestPairing",           "an empty object",              Body: new JObject(),                      MustBeRejected: true, Secrets: secrets));
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/finalizePairing",          "an unknown pairingAttemptId",  Body: new JObject(), Bearer: TokenGenerator.NewPairingAttemptId().Value, MustBeRejected: true, Secrets: secrets));
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/postConnectionDetails",    "the wrong branch",             Body: new PostConnectionDetailsRequest(CorrectServerResponse(fixture, started), started.Client.ConnectionDetails()).ToJSON(), Bearer: started.Bearer, MustBeRejected: true, Secrets: secrets));
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/preparePairing",           "an empty object",              Body: new JObject(),                      MustBeRejected: true, Secrets: secrets));
            problems.AddRange(await PostAndCheckAsync(fixture, "v1/cancelPreparePairing",     "an empty object",              Body: new JObject(),                      MustBeRejected: true, Secrets: secrets));
            problems.AddRange(await PostAndCheckAsync(fixture, waitForPairingRoute,           "an object instead of array",   Body: new JObject(),                      MustBeRejected: true, Secrets: secrets));

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

        }

        #endregion

        #region HostilePaths_AreAnsweredWithoutAServerError()

        [Test]
        public async Task HostilePaths_AreAnsweredWithoutAServerError()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(Options: FuzzOptions());

            fixture.AddNode(EnergyManagementRole.RM);

            String[] paths = [
                "v1/requestPairing/../../etc/passwd",
                "v1/REQUESTPAIRING",
                "v1/requestPairing%00",
                "v1/requestPairing?x=%3Cscript%3E",
                "v2/requestPairing",
                "v1/",
                "v1/requestPairing/extra/segments"
            ];

            var problems = new List<String>();

            foreach (var path in paths)
            {

                var stopwatch  = Stopwatch.StartNew();
                var result     = await S2FuzzCorpus.PostRawAsync(fixture.Client,
                                                                 path,
                                                                 Encoding.UTF8.GetBytes("{}"),
                                                                 "application/json; charset=utf-8");

                stopwatch.Stop();

                if (result is null)
                    problems.Add($"The HTTP client refused to request '{path}'!");
                else
                    // A path outside the API is answered by the HTTP server itself, whose 404
                    // body is not an S2 error object; only the other invariants apply here.
                    problems.AddRange(S2FuzzCorpus.Violations($"POST {path}",
                                                              result,
                                                              stopwatch.Elapsed,
                                                              MustBeRejected:   true,
                                                              CheckErrorShape:  false));

            }

            Assert.That(problems, Is.Empty, String.Join(Environment.NewLine, problems));

            await AssertStillCompletesAPairingAsync(fixture);

        }

        #endregion

    }


    /// <summary>
    /// The corpus of hostile HTTP payloads and the response invariants shared by the S2 Connect
    /// fuzz tests of the pairing server and of the session initiation server.
    /// </summary>
    internal static class S2FuzzCorpus
    {

        #region Data

        /// <summary>
        /// The time after which an answer counts as a hang. Every operation of the fuzzed
        /// servers is either immediate or bounded by a configured timeout.
        /// </summary>
        internal static readonly TimeSpan MaxResponseTime = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Texts that must never appear in a response: stack traces, exception types, namespaces
        /// and file system paths.
        /// </summary>
        private static readonly String[] leakMarkers = [
            "exception",
            "   at ",
            ".cs:line",
            "stack trace",
            "stacktrace",
            "newtonsoft",
            "cloud.charging.open",
            "org.graphdefined",
            "c:\\",
            "/usr/",
            "/home/"
        ];

        /// <summary>
        /// Content types that are not "application/json" and must be refused before the body
        /// is even read.
        /// </summary>
        internal static readonly String[] RejectedContentTypes = [
            "text/plain",
            "text/html",
            "application/xml",
            "application/octet-stream",
            "application/x-www-form-urlencoded",
            "multipart/form-data",
            "application/json-patch+json"
        ];

        #endregion

        #region MalformedBodies()

        /// <summary>
        /// Bodies that are either no JSON at all or JSON that can never be a valid request of any
        /// S2 Connect operation - neither of one taking a JSON object nor of one taking a JSON
        /// array. Every one of them must be answered with 4xx on every route.
        /// </summary>
        internal static IReadOnlyList<(String Name, String Body)> MalformedBodies()

            => [
                   ("an empty body",                     ""),
                   ("whitespace only",                   "   \t\r\n  "),
                   ("a truncated object",                "{\"clientNodeId\":"),
                   ("an opening brace",                  "{"),
                   ("a closing brace",                   "}"),
                   ("an unbalanced bracket",             "{\"a\": [1, 2}"),
                   ("a missing value",                   "{\"clientNodeId\": }"),
                   ("a bare number",                     "42"),
                   ("a bare string",                     "\"just a string\""),
                   ("a bare boolean",                    "true"),
                   ("a bare null",                       "null"),
                   ("an array of numbers",               "[1, 2, 3]"),
                   ("an array of empty objects",         "[{}]"),
                   ("two documents",                     "{} {}"),
                   ("trailing garbage",                  "{\"a\":1} trailing"),
                   ("two lines of JSON",                 "{\"a\":1}\n{\"a\":2}"),
                   ("duplicate keys",                    "{\"clientNodeId\":\"a\",\"clientNodeId\":\"b\"}"),
                   ("single quotes",                     "{'clientNodeId': 'x'}"),
                   ("unquoted keys",                     "{clientNodeId: 1}"),
                   ("a JavaScript comment",              "{/* nothing to see here */}"),
                   ("a trailing comma",                  "{\"a\":1,}"),
                   ("the NaN literal",                   "{\"v\": NaN}"),
                   ("the Infinity literal",              "{\"v\": Infinity}"),
                   ("a hexadecimal number",              "{\"v\": 0x10}"),
                   ("a leading plus",                    "{\"v\": +1}"),
                   ("negative zero",                     "{\"v\": -0}"),
                   ("an exponent overflow",              "{\"v\": 1e999999}"),
                   ("a NUL escape in a string",          "{\"clientNodeId\": \"\\u0000\\u0000\"}"),
                   ("a lone high surrogate",             "{\"clientNodeId\": \"\\ud800\"}"),
                   ("a lone low surrogate",              "{\"clientNodeId\": \"\\udc00\"}"),
                   ("a script tag",                      "<script>alert('S2')</script>"),
                   ("a script inside JSON",              "{\"clientNodeId\": \"<script>alert(1)</script>\"}"),
                   ("an XML document",                   "<?xml version=\"1.0\"?><requestPairing/>"),
                   ("a SQL fragment inside JSON",        "{\"clientNodeId\": \"'; DROP TABLE nodes; --\"}"),
                   ("a form encoded body",               "clientNodeId=abc&forcePairing=true"),
                   ("a path traversal inside JSON",      "{\"clientNodeId\": \"../../../../etc/passwd\"}")
               ];

        #endregion

        #region StructurallyHostileBodies()

        /// <summary>
        /// Bodies that attack the size and the depth of the JSON reader. They stay below the
        /// 64 kByte request budget on purpose, so that they reach the parser instead of the
        /// request size limit (which has its own test).
        /// </summary>
        internal static IReadOnlyList<(String Name, String Body)> StructurallyHostileBodies()

            => [
                   ("5000 nested arrays",                new String('[', 5000) + new String(']', 5000)),
                   ("5000 nested objects",               String.Concat(Enumerable.Repeat("{\"a\":", 5000)) + "1" + new String('}', 5000)),
                   ("a 10000 digit number",              "{\"v\": " + new String('9', 10000) + "}"),
                   ("a 10000 digit negative number",     "{\"v\": -" + new String('9', 10000) + "}"),
                   ("a 60000 character string",          "{\"v\": \"" + new String('A', 60000) + "\"}"),
                   ("a 60000 character property name",   "{\"" + new String('k', 60000) + "\": 1}"),
                   ("5000 properties",                   "{" + String.Join(",", Enumerable.Range(0, 5000).Select(i => $"\"k{i}\":{i}")) + "}"),
                   ("5000 array elements",               "[" + String.Join(",", Enumerable.Range(0, 5000)) + "]")
               ];

        #endregion

        #region HostileByteBodies()

        /// <summary>
        /// Raw byte payloads: a byte order mark, invalid UTF-8 sequences, embedded NUL bytes and
        /// a UTF-16 document. They cannot be expressed through the string based helpers of the
        /// fixtures, which always re-encode as UTF-8.
        /// </summary>
        internal static IReadOnlyList<(String Name, Byte[] Bytes, String? ContentType)> HostileByteBodies()
        {

            const String jsonContentType  = "application/json; charset=utf-8";
            const String validish         = "{\"clientNodeId\":\"a\"}";

            var withBOM = new List<Byte> { 0xEF, 0xBB, 0xBF };
            withBOM.AddRange(Encoding.UTF8.GetBytes(validish));

            return [

                       ("a byte order mark before the JSON",
                        withBOM.ToArray(),
                        jsonContentType),

                       ("a byte order mark only",
                        new Byte[] { 0xEF, 0xBB, 0xBF },
                        jsonContentType),

                       // {"a":"\xFF\xFE\xFD"}
                       ("an invalid UTF-8 sequence",
                        new Byte[] { 0x7B, 0x22, 0x61, 0x22, 0x3A, 0x22, 0xFF, 0xFE, 0xFD, 0x22, 0x7D },
                        jsonContentType),

                       // {"a":"<the first two bytes of a euro sign>"}
                       ("a truncated UTF-8 sequence",
                        new Byte[] { 0x7B, 0x22, 0x61, 0x22, 0x3A, 0x22, 0xE2, 0x82, 0x22, 0x7D },
                        jsonContentType),

                       // {"a":"<an overlong encoding of NUL>"}
                       ("an overlong UTF-8 encoding",
                        new Byte[] { 0x7B, 0x22, 0x61, 0x22, 0x3A, 0x22, 0xC0, 0x80, 0x22, 0x7D },
                        jsonContentType),

                       // {"a":"\0\0"}
                       ("a raw NUL byte inside a string",
                        new Byte[] { 0x7B, 0x22, 0x61, 0x22, 0x3A, 0x22, 0x00, 0x00, 0x22, 0x7D },
                        jsonContentType),

                       // {<NUL>}
                       ("a NUL byte between the tokens",
                        new Byte[] { 0x7B, 0x00, 0x7D },
                        jsonContentType),

                       ("only NUL bytes",
                        new Byte[] { 0x00, 0x00, 0x00, 0x00 },
                        jsonContentType),

                       ("a UTF-16 document",
                        Encoding.Unicode.GetBytes(validish),
                        jsonContentType),

                       ("a UTF-16 document announced as UTF-16",
                        Encoding.Unicode.GetBytes(validish),
                        "application/json; charset=utf-16"),

                       ("random looking binary",
                        new Byte[] { 0x1F, 0x8B, 0x08, 0x00, 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x03 },
                        jsonContentType),

                       ("eight kilobytes of NUL",
                        new Byte[8192],
                        jsonContentType)

                   ];

        }

        #endregion

        #region PostRawAsync(Client, RelativePath, Body = null, ContentType = null, AuthorizationHeader = null)

        /// <summary>
        /// POST raw bytes with a raw Authorization header. Returns null when the local HTTP
        /// client refuses to send the request at all (some header values are rejected before
        /// they ever reach the server, which says nothing about the server).
        /// </summary>
        internal static async Task<HTTPResult?> PostRawAsync(HttpClient  Client,
                                                             String      RelativePath,
                                                             Byte[]?     Body                  = null,
                                                             String?     ContentType           = null,
                                                             String?     AuthorizationHeader   = null)
        {

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(RelativePath, UriKind.Relative));

            if (Body is not null)
            {

                var content = new ByteArrayContent(Body);

                if (ContentType is not null)
                {
                    try
                    {
                        content.Headers.ContentType = MediaTypeHeaderValue.Parse(ContentType);
                    }
                    catch (FormatException)
                    {
                        content.Dispose();
                        return null;
                    }
                }

                request.Content = content;

            }

            if (AuthorizationHeader is not null &&
                !request.Headers.TryAddWithoutValidation("Authorization", AuthorizationHeader))
            {
                return null;
            }

            try
            {

                using var response = await Client.SendAsync(request);

                return await HTTPResult.FromAsync(response);

            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // The local HTTP client rejected the request before it reached the server;
                // a cancellation (the client timeout) is deliberately NOT swallowed, because
                // that would hide a hanging server.
                return null;
            }

        }

        #endregion

        #region Violations(Label, Result, Elapsed, Secrets = null, MustBeRejected = false)

        /// <summary>
        /// The invariants every answer to a hostile request has to satisfy. Returns a
        /// description of each violated invariant, so that a whole fuzzing loop can report all
        /// of its findings at once instead of stopping at the first one.
        /// </summary>
        /// <param name="Label">A label naming the route and the payload.</param>
        /// <param name="Result">The HTTP answer.</param>
        /// <param name="Elapsed">How long the answer took.</param>
        /// <param name="Secrets">Secrets that must not be echoed by an error answer.</param>
        /// <param name="MustBeRejected">Whether the payload must be answered with 4xx.</param>
        /// <param name="CheckErrorShape">Whether the error body must be empty or the S2 error
        /// shape (false for paths that are answered by the HTTP server itself instead of by
        /// the S2 API).</param>
        internal static IReadOnlyList<String> Violations(String                Label,
                                                         HTTPResult            Result,
                                                         TimeSpan              Elapsed,
                                                         IEnumerable<String>?  Secrets          = null,
                                                         Boolean               MustBeRejected   = false,
                                                         Boolean               CheckErrorShape  = true)
        {

            var problems  = new List<String>();
            var status    = (Int32) Result.Status;
            var body      = Result.Body ?? "";

            #region The answer must be timely

            if (Elapsed > MaxResponseTime)
                problems.Add($"{Label}: the answer took {Elapsed.TotalSeconds:F1} seconds (status {status}).");

            #endregion

            #region No server errors, except a deliberate 503 with a Retry-After header

            if (status >= 500 &&
                (status != 503 || Result.RetryAfter is null))
            {
                problems.Add($"{Label}: the server answered {status}: '{Truncate(body)}'.");
            }

            #endregion

            #region A payload that cannot be valid must be refused

            if (MustBeRejected && (status < 400 || status > 499))
                problems.Add($"{Label}: expected a 4xx rejection, but the server answered {status}: '{Truncate(body)}'.");

            #endregion

            #region An error body is either empty or the S2 error shape

            if (CheckErrorShape && status >= 400 && body.Length > 0)
            {

                if (Result.JSON is not JObject error)
                    problems.Add($"{Label}: the {status} body is neither empty nor a JSON object: '{Truncate(body)}'.");

                else
                {

                    var errorMessage = error["errorMessage"];

                    if (errorMessage is null ||
                        errorMessage.Type != JTokenType.String ||
                        String.IsNullOrEmpty(errorMessage.Value<String>()))
                    {
                        problems.Add($"{Label}: the {status} body has no 'errorMessage': '{Truncate(body)}'.");
                    }

                    var unexpected = error.Properties().
                                           Select(property => property.Name).
                                           Where (name     => name != "errorMessage" && name != "additionalInfo").
                                           ToList();

                    if (unexpected.Count > 0)
                        problems.Add($"{Label}: the {status} body carries unexpected properties: {String.Join(", ", unexpected)}.");

                }

            }

            #endregion

            #region Nothing internal leaks

            foreach (var marker in leakMarkers)
            {
                if (body.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    problems.Add($"{Label}: the {status} answer leaks '{marker}': '{Truncate(body)}'.");
            }

            #endregion

            #region No secret is echoed by an error answer

            if (Secrets is not null && status >= 400)
            {
                foreach (var secret in Secrets)
                {
                    if (secret.Length >= 8 && body.Contains(secret, StringComparison.Ordinal))
                        problems.Add($"{Label}: the {status} answer echoes a secret.");
                }
            }

            #endregion

            return problems;

        }

        #endregion

        #region (private static) Truncate(Text)

        private static String Truncate(String Text)
            => Text.Length <= 200
                   ? Text
                   : Text[..200] + "...";

        #endregion

    }

}

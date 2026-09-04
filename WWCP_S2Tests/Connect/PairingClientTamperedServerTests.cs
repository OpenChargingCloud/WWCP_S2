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
using System.Text;

using Microsoft.Extensions.Time.Testing;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The pairing client against a fake pairing server that answers wrongly on purpose:
    /// unusable version indexes, unparseable or tampered requestPairing responses (the client
    /// checks of step 3 and 4), the failure status codes of requestConnectionDetails and
    /// finalizePairing, the 503 retries, the deadline of the attempt and an unreachable
    /// server (S2 Connect 1.0.0, "Pairing interaction" and "Interruption of the process").
    /// Whenever a pairingAttemptId is known the client must inform the server via
    /// finalizePairing(success = false); the fake records every request to prove it.
    /// </summary>
    [TestFixture]
    public sealed class PairingClientTamperedServerTests
    {

        #region Data

        private const String  RequestPairing            = "requestPairing";
        private const String  RequestConnectionDetails  = "requestConnectionDetails";
        private const String  PostConnectionDetails     = "postConnectionDetails";
        private const String  FinalizePairing           = "finalizePairing";

        #endregion

        #region (class) RecordedRequest

        /// <summary>
        /// A request the fake server received: the operation, the JSON object of its body (when
        /// any) and the bearer token of its Authorization header (when any).
        /// </summary>
        private sealed record RecordedRequest(String    Operation,
                                              JObject?  Body,
                                              String?   Bearer)
        {

            /// <summary>
            /// The success flag of a finalizePairing request, or null when the body has none.
            /// </summary>
            public Boolean? Success
            {
                get
                {

                    var token = Body?["success"];

                    return token is not null && token.Type == JTokenType.Boolean
                               ? token.Value<Boolean>()
                               : null;

                }
            }

        }

        #endregion

        #region (class) FakePairingServer

        /// <summary>
        /// A fake pairing server on a Hermod HTTP server: by default it answers every operation
        /// correctly (a LAN endpoint hosting one node that shares its pairing token with the
        /// tests), each answer can be replaced per test, and every request of the pairing API
        /// is recorded.
        /// </summary>
        private sealed class FakePairingServer : IAsyncDisposable
        {

            #region Data

            private readonly List<RecordedRequest>  requests = [];
            private          String?                lastIssuedPairingAttemptId;

            #endregion

            #region Properties

            public IPPort           Port                  { get; }
            public HTTPServer       HTTPServer            { get; }
            public HTTPAPI          API                   { get; }
            public S2BaseURL        PairingUrl            { get; }
            public S2BaseURL        SessionInitiationUrl  { get; }

            /// <summary>
            /// The pairing token the fake expects (the token of its node).
            /// </summary>
            public PairingToken     Token                 { get; }

            /// <summary>
            /// The description of the only node of the fake; replaceable before pairing.
            /// </summary>
            public NodeDescription  ServerNode            { get; set; }

            public Func<HTTPRequest,           Task<HTTPResponse>>  OnVersionIndex              { get; set; }
            public Func<HTTPRequest, JObject?, Task<HTTPResponse>>  OnRequestPairing            { get; set; }
            public Func<HTTPRequest, JObject?, Task<HTTPResponse>>  OnRequestConnectionDetails  { get; set; }
            public Func<HTTPRequest, JObject?, Task<HTTPResponse>>  OnPostConnectionDetails     { get; set; }
            public Func<HTTPRequest, JObject?, Task<HTTPResponse>>  OnFinalizePairing           { get; set; }

            /// <summary>
            /// Every request of the pairing API received so far (the version index is not recorded).
            /// </summary>
            public IReadOnlyList<RecordedRequest> Requests
            {
                get
                {
                    lock (requests)
                    {
                        return [.. requests];
                    }
                }
            }

            /// <summary>
            /// The pairingAttemptId of the last correct requestPairing response, when any.
            /// </summary>
            public String? LastIssuedPairingAttemptId
            {
                get
                {
                    lock (requests)
                    {
                        return lastIssuedPairingAttemptId;
                    }
                }
            }

            #endregion

            #region Constructor(s)

            private FakePairingServer(IPPort           Port,
                                      HTTPServer       HTTPServer,
                                      HTTPAPI          API,
                                      PairingToken     Token,
                                      NodeDescription  ServerNode)
            {

                this.Port                   = Port;
                this.HTTPServer             = HTTPServer;
                this.API                    = API;
                this.Token                  = Token;
                this.ServerNode             = ServerNode;
                this.PairingUrl             = S2BaseURL.Parse($"http://127.0.0.1:{Port}/pairing/",    AllowHTTP: true);
                this.SessionInitiationUrl   = S2BaseURL.Parse($"http://127.0.0.1:{Port}/connection/", AllowHTTP: true);

                OnVersionIndex              = request         => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK, new JArray("v1")));
                OnRequestPairing            = (request, body) => Task.FromResult(CorrectRequestPairing(request, body));
                OnRequestConnectionDetails  = (request, _)    => Task.FromResult(CorrectConnectionDetails(request));
                OnPostConnectionDetails     = (request, _)    => Task.FromResult(EmptyResponse(request, HTTPStatusCode.NoContent));
                OnFinalizePairing           = (request, _)    => Task.FromResult(EmptyResponse(request, HTTPStatusCode.NoContent));

            }

            #endregion


            #region (static) StartAsync(ServerRole = null, Token = null)

            /// <summary>
            /// Start a fake pairing server on a free loopback port.
            /// </summary>
            /// <param name="ServerRole">The role of the node of the fake (default: CEM).</param>
            /// <param name="Token">The pairing token of the node of the fake (default: "ABCD2345").</param>
            public static async Task<FakePairingServer> StartAsync(EnergyManagementRole?  ServerRole   = null,
                                                                   PairingToken?          Token        = null)
            {

                var port        = PairingServerFixture.FreePort();
                var role        = ServerRole ?? EnergyManagementRole.CEM;

                var httpServer  = new HTTPServer(
                                      IPv4Address.Parse("127.0.0.1"),
                                      port,
                                      "S2 fake pairing server",
                                      AutoStart: false
                                  );

                var api         = new HTTPAPI(
                                      httpServer,
                                      RootPath:        HTTPPath.Parse("/pairing/"),
                                      DisableLogging:  true
                                  );

                var fake        = new FakePairingServer(
                                      port,
                                      httpServer,
                                      api,
                                      Token ?? PairingToken.Parse("ABCD2345"),
                                      new NodeDescription(
                                          Node_Id.NewRandom,
                                          "Fake",
                                          role == EnergyManagementRole.CEM ? "EMS"     : "heat pump",
                                          role == EnergyManagementRole.CEM ? "FakeCEM" : "FakeRM",
                                          role
                                      )
                                  );

                // The handlers are looked up per request, so the tests can replace them after the start.
                api.AddHandler(HTTPMethod.GET,  HTTPPath.Root,                                       request => fake.OnVersionIndex(request));
                api.AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/v1/{RequestPairing}"),             request => fake.RecordAndHandleAsync(request, RequestPairing,           fake.OnRequestPairing));
                api.AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/v1/{RequestConnectionDetails}"),   request => fake.RecordAndHandleAsync(request, RequestConnectionDetails, fake.OnRequestConnectionDetails));
                api.AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/v1/{PostConnectionDetails}"),      request => fake.RecordAndHandleAsync(request, PostConnectionDetails,    fake.OnPostConnectionDetails));
                api.AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/v1/{FinalizePairing}"),            request => fake.RecordAndHandleAsync(request, FinalizePairing,          fake.OnFinalizePairing));

                await httpServer.Start();

                return fake;

            }

            #endregion


            #region RequestsOf(Operation)

            /// <summary>
            /// The recorded requests of the given operation, in the order of their arrival.
            /// </summary>
            public IReadOnlyList<RecordedRequest> RequestsOf(String Operation)
                => [.. Requests.Where(request => request.Operation == Operation)];

            #endregion

            #region CorrectRequestPairing(Request, Body, TokenOverride = null, Tamper = null)

            /// <summary>
            /// The correct 200 response to the given requestPairing request: a fresh pairingAttemptId,
            /// the node and endpoint of the fake, SHA256, the answer to the client challenge (computed
            /// with the LAN formula, the fake fingerprint of the server fixture and the token of the
            /// fake unless overridden) and a fresh server challenge. The JSON can be tampered with
            /// before it is sent.
            /// </summary>
            public HTTPResponse CorrectRequestPairing(HTTPRequest       Request,
                                                      JObject?          Body,
                                                      PairingToken?     TokenOverride   = null,
                                                      Action<JObject>?  Tamper          = null)
            {

                if (Body is null)
                    return JSONResponse(Request, HTTPStatusCode.BadRequest, new PairingResponseErrorMessage(PairingResponseError.ParsingError, "a JSON object is expected").ToJSON());

                if (!RequestPairingRequest.TryParse(Body, out var request, out var error))
                    return JSONResponse(Request, HTTPStatusCode.BadRequest, new PairingResponseErrorMessage(PairingResponseError.ParsingError, error).ToJSON());

                var pairingAttemptId  = TokenGenerator.NewPairingAttemptId();

                var json              = new RequestPairingResponse(
                                            pairingAttemptId,
                                            ServerNode,
                                            new EndpointDescription("Fake endpoint", null, Deployment.LAN),
                                            HmacHashingAlgorithm.SHA256,
                                            ChallengeResponse.ComputeForLAN(
                                                HmacHashingAlgorithm.SHA256,
                                                request.ClientHmacChallenge,
                                                TokenOverride ?? Token,
                                                PairingServerFixture.Fingerprint
                                            ),
                                            TokenGenerator.NewChallenge()
                                        ).ToJSON();

                Tamper?.Invoke(json);

                lock (requests)
                {
                    lastIssuedPairingAttemptId = pairingAttemptId.Value;
                }

                return JSONResponse(Request, HTTPStatusCode.OK, json);

            }

            #endregion

            #region CorrectConnectionDetails(Request)

            /// <summary>
            /// The correct 200 response to requestConnectionDetails: the session initiation URL
            /// of the fake and a fresh access token.
            /// </summary>
            public HTTPResponse CorrectConnectionDetails(HTTPRequest Request)

                => JSONResponse(Request,
                                HTTPStatusCode.OK,
                                new ConnectionDetails(SessionInitiationUrl, TokenGenerator.NewAccessToken()).ToJSON());

            #endregion


            #region (private) RecordAndHandleAsync(Request, Operation, Handler)

            private async Task<HTTPResponse> RecordAndHandleAsync(HTTPRequest                                      Request,
                                                                  String                                           Operation,
                                                                  Func<HTTPRequest, JObject?, Task<HTTPResponse>>  Handler)
            {

                // Reading the body here also drains it, so that the remains of an early answer are
                // never mistaken for the next request on the keep-alive connection.
                var body  = Request.HTTPBody;
                var json  = default(JObject);

                if (body is { Length: > 0 })
                {
                    try
                    {
                        json = JObject.Parse(Encoding.UTF8.GetString(body));
                    }
                    catch (JsonException)
                    {
                        // Not a JSON object: recorded without a body.
                    }
                }

                var bearer = Request.Authorization is HTTPBearerAuthentication authentication
                                 ? authentication.Token
                                 : null;

                lock (requests)
                {
                    requests.Add(new RecordedRequest(Operation, json, bearer));
                }

                return await Handler(Request, json);

            }

            #endregion

            #region DisposeAsync()

            public async ValueTask DisposeAsync()
            {
                await HTTPServer.Stop();
            }

            #endregion

        }

        #endregion

        #region (class) TestClient

        /// <summary>
        /// A pairing client with its own LAN endpoint (session initiation URL and CA fingerprint
        /// like the client fixture), one hosted node and an in-memory store, assuming the fake
        /// server certificate fingerprint of the server fixture.
        /// </summary>
        private sealed class TestClient : IAsyncDisposable
        {

            public LocalEndpoint    Endpoint    { get; }
            public HostedNode       Node        { get; }
            public InMemoryS2Store  Store       { get; }
            public PairingClient    Client      { get; }

            private TestClient(LocalEndpoint    Endpoint,
                               HostedNode       Node,
                               InMemoryS2Store  Store,
                               PairingClient    Client)
            {
                this.Endpoint  = Endpoint;
                this.Node      = Node;
                this.Store     = Store;
                this.Client    = Client;
            }

            /// <summary>
            /// Create a pairing client for the given pairing URL.
            /// </summary>
            /// <param name="PairingUrl">The pairing URL (of the fake, or of nothing at all).</param>
            /// <param name="Role">The role of the client node (default: RM).</param>
            /// <param name="Options">Optional client options (default: the options of the client fixture).</param>
            /// <param name="TimeProvider">An optional time provider for the deadline of the attempt.</param>
            public static TestClient Create(S2BaseURL              PairingUrl,
                                            EnergyManagementRole?  Role           = null,
                                            PairingClientOptions?  Options        = null,
                                            TimeProvider?          TimeProvider   = null)
            {

                var role      = Role ?? EnergyManagementRole.RM;

                var endpoint  = new LocalEndpoint(
                                    new EndpointDescription("Test LAN client endpoint"),
                                    Deployment.LAN,
                                    S2BaseURL.Parse("http://127.0.0.1:1/pairing/",    AllowHTTP: true),
                                    S2BaseURL.Parse("http://127.0.0.1:1/connection/", AllowHTTP: true),
                                    CACertificateFingerprint: () => PairingClientFixture.CAFingerprint
                                );

                var node      = endpoint.AddNode(
                                    new NodeDescription(
                                        Node_Id.NewRandom,
                                        "ACME",
                                        role == EnergyManagementRole.CEM ? "EMS"       : "heat pump",
                                        role == EnergyManagementRole.CEM ? "ClientCEM" : "ClientRM",
                                        role
                                    )
                                );

                var store     = new InMemoryS2Store();

                var client    = new PairingClient(
                                    PairingUrl,
                                    endpoint,
                                    store,
                                    Deployment.LAN,
                                    Options ?? PairingClientFixture.DefaultOptions(),
                                    PairingServerFixture.Fingerprint,
                                    TimeProvider: TimeProvider
                                );

                return new TestClient(endpoint, node, store, client);

            }

            /// <summary>
            /// Run a pairing attempt of the node with the given token.
            /// </summary>
            public Task<PairingClientResult> PairAsync(PairingToken       Token,
                                                       CancellationToken  CancellationToken   = default)

                => Client.PairAsync(Node, Token, CancellationToken: CancellationToken);

            public async ValueTask DisposeAsync()
            {
                await Client.DisposeAsync();
            }

        }

        #endregion

        #region HTTP helpers

        /// <summary>
        /// A JSON response with the given status code.
        /// </summary>
        private static HTTPResponse JSONResponse(HTTPRequest     Request,
                                                 HTTPStatusCode  StatusCode,
                                                 JToken          Body)

            => new HTTPResponse.Builder(Request) {
                   HTTPStatusCode  = StatusCode,
                   Connection      = ConnectionType.KeepAlive,
                   ContentType     = HTTPContentType.Application.JSON_UTF8,
                   Content         = Encoding.UTF8.GetBytes(Body.ToString(Formatting.None))
               }.AsImmutable;


        /// <summary>
        /// A response without a body, optionally with a Retry-After header.
        /// </summary>
        private static HTTPResponse EmptyResponse(HTTPRequest     Request,
                                                  HTTPStatusCode  StatusCode,
                                                  String?         RetryAfter   = null)
        {

            var builder = new HTTPResponse.Builder(Request) {
                              HTTPStatusCode  = StatusCode,
                              Connection      = ConnectionType.KeepAlive
                          };

            if (RetryAfter is not null)
                builder.RetryAfter = RetryAfter;

            // Hermod omits the Content-Length header when there is no content and a client would
            // then wait for the connection to close; 204 responses must not carry one (RFC 9110).
            if (StatusCode != HTTPStatusCode.NoContent)
                builder.Content = [];

            return builder.AsImmutable;

        }


        /// <summary>
        /// A plain text response with the given status code.
        /// </summary>
        private static HTTPResponse TextResponse(HTTPRequest     Request,
                                                 HTTPStatusCode  StatusCode,
                                                 String          Text)

            => new HTTPResponse.Builder(Request) {
                   HTTPStatusCode  = StatusCode,
                   Connection      = ConnectionType.KeepAlive,
                   ContentType     = HTTPContentType.Text.PLAIN,
                   Content         = Encoding.UTF8.GetBytes(Text)
               }.AsImmutable;


        /// <summary>
        /// The client informed the fake exactly once via finalizePairing(success = false), using
        /// the given (or the last issued) pairingAttemptId as bearer token.
        /// </summary>
        private static void AssertFinalizedWithFalse(FakePairingServer  Fake,
                                                     String?            ExpectedBearer   = null)
        {

            var finalizations = Fake.RequestsOf(FinalizePairing);

            Assert.That(finalizations, Has.Count.EqualTo(1), "exactly one finalizePairing request is expected");

            Assert.Multiple(() => {
                Assert.That(finalizations[0].Success,  Is.False, "finalizePairing must carry success = false");
                Assert.That(finalizations[0].Bearer,   Is.Not.Null);
                Assert.That(finalizations[0].Bearer,   Is.EqualTo(ExpectedBearer ?? Fake.LastIssuedPairingAttemptId), "the pairingAttemptId of the failed attempt is the bearer token");
            });

        }

        #endregion


        // The version index (S2 Connect 1.0.0, "Selecting the version of REST APIs")

        #region VersionIndex_WithoutV1_ReturnsNoCommonAPIVersion()

        [Test]
        [S2C("Versioning.2")]
        [S2C("Versioning.3")]
        public async Task VersionIndex_WithoutV1_ReturnsNoCommonAPIVersion()
        {

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl);

            fake.OnVersionIndex = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK, new JArray("v2")));

            var result = await client.PairAsync(fake.Token);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                     Is.EqualTo(PairingClientOutcome.NoCommonAPIVersion), result.ToString());
                Assert.That(result.Operation,                   Is.EqualTo("versionIndex"));
                Assert.That(result.StatusCode?.Code,            Is.EqualTo(200));
                Assert.That(result.Retryable,                   Is.False, "the endpoints are not compatible");
                Assert.That(result.Description,                 Does.Contain("v2"));
                Assert.That(client.Client.ServerAPIVersions,    Is.EqualTo(new[] { "v2" }));
                Assert.That(client.Client.SelectedAPIVersion,   Is.Null);
                Assert.That(fake.Requests,                      Is.Empty, "no operation of the pairing API was called");
            });

        }

        #endregion

        #region VersionIndex_NotAnArray_ReturnsInvalidResponse()

        [Test]
        [S2C("Versioning.3")]
        public async Task VersionIndex_NotAnArray_ReturnsInvalidResponse()
        {

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl);

            fake.OnVersionIndex = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK, new JObject(new JProperty("versions", new JArray("v1")))));

            var versions  = await client.Client.GetVersionsAsync();
            var result    = await client.PairAsync(fake.Token);

            Assert.Multiple(() => {
                Assert.That(versions.IsSuccess,                 Is.False, versions.ToString());
                Assert.That(versions.IsTransportFailure,        Is.False);
                Assert.That(versions.StatusCode.Code,           Is.EqualTo(200));
                Assert.That(versions.Value,                     Is.Null);
                Assert.That(versions.Description,               Does.Contain("JSON array"));
                Assert.That(result.Outcome,                     Is.EqualTo(PairingClientOutcome.InvalidResponse), result.ToString());
                Assert.That(result.Operation,                   Is.EqualTo("versionIndex"));
                Assert.That(result.StatusCode?.Code,            Is.EqualTo(200));
                Assert.That(client.Client.SelectedAPIVersion,   Is.Null);
                Assert.That(fake.Requests,                      Is.Empty);
            });

        }

        #endregion


        // The client checks of the requestPairing response (S2 Connect 1.0.0, "3. Response status 200", "4. Check clientHmacChallengeResponse")

        #region RequestPairing_GarbageText_ReturnsInvalidResponseWithoutFinalize()

        [Test]
        [S2C("Pairing.3.ClientChecks")]
        public async Task RequestPairing_GarbageText_ReturnsInvalidResponseWithoutFinalize()
        {

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl);

            fake.OnRequestPairing = (request, _) => Task.FromResult(TextResponse(request, HTTPStatusCode.OK, "this is not JSON"));

            var result = await client.PairAsync(fake.Token);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                              Is.EqualTo(PairingClientOutcome.InvalidResponse), result.ToString());
                Assert.That(result.Operation,                            Is.EqualTo(RequestPairing));
                Assert.That(result.StatusCode?.Code,                     Is.EqualTo(200));
                Assert.That(result.ServerResponse,                       Is.Null);
                Assert.That(result.Retryable,                            Is.False);
                Assert.That(result.Pairing,                              Is.Null);
                Assert.That(fake.RequestsOf(RequestPairing),             Has.Count.EqualTo(1));
                Assert.That(fake.RequestsOf(RequestConnectionDetails),   Is.Empty);
                Assert.That(fake.RequestsOf(FinalizePairing),            Is.Empty, "without a pairingAttemptId nothing can be finalized");
            });

        }

        #endregion

        #region RequestPairing_SchemaViolationWithPairingAttemptId_ReturnsInvalidResponseAndFinalizesWithFalse()

        [Test]
        [S2C("Pairing.3.ClientChecks")]
        [S2C("Pairing.PairingAttemptId")]
        public async Task RequestPairing_SchemaViolationWithPairingAttemptId_ReturnsInvalidResponseAndFinalizesWithFalse()
        {

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl);

            // "Is the response formatted according to the schema? -> call /finalizePairing where success is false if pairingAttemptId is available"
            var id = TokenGenerator.NewPairingAttemptId();

            fake.OnRequestPairing = (request, _) => Task.FromResult(
                                        JSONResponse(request,
                                                     HTTPStatusCode.OK,
                                                     new JObject(
                                                         new JProperty("pairingAttemptId",       id.Value),
                                                         new JProperty("serverNodeDescription",  fake.ServerNode.ToJSON())
                                                     ))
                                    );

            var result = await client.PairAsync(fake.Token);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                              Is.EqualTo(PairingClientOutcome.InvalidResponse), result.ToString());
                Assert.That(result.Operation,                            Is.EqualTo(RequestPairing));
                Assert.That(result.StatusCode?.Code,                     Is.EqualTo(200));
                Assert.That(result.ServerResponse,                       Is.Null);
                Assert.That(result.Description,                          Does.Contain("schema"));
                Assert.That(result.Pairing,                              Is.Null);
                Assert.That(fake.RequestsOf(RequestConnectionDetails),   Is.Empty);
            });

            AssertFinalizedWithFalse(fake, id.Value);

        }

        #endregion

        #region RequestPairing_WrongClientChallengeResponse_ReturnsChallengeResponseMismatchAndFinalizesWithFalse()

        [Test]
        [S2C("Pairing.4")]
        public async Task RequestPairing_WrongClientChallengeResponse_ReturnsChallengeResponseMismatchAndFinalizesWithFalse()
        {

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl);

            // The fake answers the client challenge with another pairing token: the tokens differ.
            fake.OnRequestPairing = (request, body) => Task.FromResult(fake.CorrectRequestPairing(request, body, TokenOverride: PairingToken.Parse("WRONG2345")));

            var result   = await client.PairAsync(fake.Token);
            var pairings = await client.Store.GetPairingsAsync();

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                              Is.EqualTo(PairingClientOutcome.ChallengeResponseMismatch), result.ToString());
                Assert.That(result.Operation,                            Is.EqualTo(RequestPairing));
                Assert.That(result.StatusCode?.Code,                     Is.EqualTo(200));
                Assert.That(result.ServerResponse,                       Is.Not.Null);
                Assert.That(result.Retryable,                            Is.False);
                Assert.That(result.Pairing,                              Is.Null);
                Assert.That(pairings,                                    Is.Empty);
                Assert.That(fake.RequestsOf(RequestConnectionDetails),   Is.Empty, "the client must not continue with a wrong clientHmacChallengeResponse");
            });

            AssertFinalizedWithFalse(fake);

        }

        #endregion

        #region RequestPairing_ServerNodeWithTheSameRole_ReturnsIncompatibleRoleAndFinalizesWithFalse()

        [Test]
        [S2C("Pairing.3.ClientChecks")]
        public async Task RequestPairing_ServerNodeWithTheSameRole_ReturnsIncompatibleRoleAndFinalizesWithFalse()
        {

            await using var fake    = await FakePairingServer.StartAsync(ServerRole: EnergyManagementRole.RM);
            await using var client  = TestClient.Create(fake.PairingUrl, Role: EnergyManagementRole.RM);

            var result = await client.PairAsync(fake.Token);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                                        Is.EqualTo(PairingClientOutcome.IncompatibleRole), result.ToString());
                Assert.That(result.Operation,                                      Is.EqualTo(RequestPairing));
                Assert.That(result.ServerResponse,                                 Is.Not.Null);
                Assert.That(result.ServerResponse?.ServerNodeDescription.Role,     Is.EqualTo(EnergyManagementRole.RM));
                Assert.That(result.Retryable,                                      Is.False);
                Assert.That(result.Pairing,                                        Is.Null);
                Assert.That(fake.RequestsOf(RequestConnectionDetails),             Is.Empty);
                Assert.That(fake.RequestsOf(PostConnectionDetails),                Is.Empty);
            });

            AssertFinalizedWithFalse(fake);

        }

        #endregion

        #region RequestPairing_UnofferedHmacHashingAlgorithm_ReturnsInvalidResponseAndFinalizesWithFalse()

        [Test]
        [S2C("Pairing.3.ClientChecks")]
        [S2C("Pairing.HmacHashingAlgorithm")]
        public async Task RequestPairing_UnofferedHmacHashingAlgorithm_ReturnsInvalidResponseAndFinalizesWithFalse()
        {

            await using var fake    = await FakePairingServer.StartAsync();

            // Lenient parsing, so that the unknown algorithm reaches the client's own check.
            var lenient = PairingClientFixture.DefaultOptions() with {
                              ParserOptions = new S2ParserOptions {
                                                  AllowInsecureURLs        = true,
                                                  RejectUnknownEnumValues  = false
                                              }
                          };

            await using var client  = TestClient.Create(fake.PairingUrl, Options: lenient);

            fake.OnRequestPairing = (request, body) => Task.FromResult(fake.CorrectRequestPairing(request, body, Tamper: json => json["selectedHmacHashingAlgorithm"] = "SHA512"));

            var result = await client.PairAsync(fake.Token);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                                                  Is.EqualTo(PairingClientOutcome.InvalidResponse), result.ToString());
                Assert.That(result.Operation,                                                Is.EqualTo(RequestPairing));
                Assert.That(result.ServerResponse,                                           Is.Not.Null, "the response was parsed, the client's own check failed it");
                Assert.That(result.ServerResponse?.SelectedHmacHashingAlgorithm.ToString(),  Is.EqualTo("SHA512"));
                Assert.That(result.Description,                                              Does.Contain("SHA512"));
                Assert.That(result.Pairing,                                                  Is.Null);
                Assert.That(fake.RequestsOf(RequestConnectionDetails),                       Is.Empty);
            });

            AssertFinalizedWithFalse(fake);

        }

        #endregion

        #region RequestPairing_ServerEndpointWithoutDeployment_ReturnsInvalidResponseAndFinalizesWithFalse()

        [Test]
        [S2C("Pairing.3.ClientChecks")]
        [S2C("CommunicationRoles.ServerDeployment")]
        public async Task RequestPairing_ServerEndpointWithoutDeployment_ReturnsInvalidResponseAndFinalizesWithFalse()
        {

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl);

            // Without the deployment of the server the communication roles cannot be determined.
            fake.OnRequestPairing = (request, body) => Task.FromResult(fake.CorrectRequestPairing(request, body, Tamper: json => json["serverEndpointDescription"] = new JObject(new JProperty("name", "Fake endpoint"))));

            var result = await client.PairAsync(fake.Token);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                                              Is.EqualTo(PairingClientOutcome.InvalidResponse), result.ToString());
                Assert.That(result.Operation,                                            Is.EqualTo(RequestPairing));
                Assert.That(result.ServerResponse,                                       Is.Not.Null);
                Assert.That(result.ServerResponse?.ServerEndpointDescription.Deployment, Is.Null);
                Assert.That(result.Description,                                          Does.Contain("deployment"));
                Assert.That(result.Pairing,                                              Is.Null);
                Assert.That(fake.RequestsOf(RequestConnectionDetails),                   Is.Empty);
                Assert.That(fake.RequestsOf(PostConnectionDetails),                      Is.Empty);
            });

            AssertFinalizedWithFalse(fake);

        }

        #endregion

        #region RequestPairing_ServerEndpointWithoutDeployment_DefaultServerDeploymentLAN_Continues()

        [Test]
        [S2C("Pairing.3.ClientChecks")]
        [S2C("CommunicationRoles.ServerDeployment")]
        public async Task RequestPairing_ServerEndpointWithoutDeployment_DefaultServerDeploymentLAN_Continues()
        {

            await using var fake    = await FakePairingServer.StartAsync();

            var assumeLAN = PairingClientFixture.DefaultOptions() with { DefaultServerDeployment = Deployment.LAN };

            await using var client  = TestClient.Create(fake.PairingUrl, Options: assumeLAN);

            fake.OnRequestPairing = (request, body) => Task.FromResult(fake.CorrectRequestPairing(request, body, Tamper: json => json["serverEndpointDescription"] = new JObject(new JProperty("name", "Fake endpoint"))));

            var result   = await client.PairAsync(fake.Token);

            Assert.That(result.IsSuccess, Is.True, result.ToString());

            var pairing  = await client.Store.GetPairingAsync(client.Node.Id, fake.ServerNode.Id);

            Assert.That(pairing, Is.Not.Null);

            Assert.Multiple(() => {
                Assert.That(result.Pairing,                                  Is.EqualTo(pairing));
                Assert.That(pairing!.LocalCommunicationRole,                 Is.EqualTo(CommunicationRole.CommunicationClient), "RM and CEM in the LAN: the CEM is the communication server");
                Assert.That(pairing.RemoteEndpointDescription.Deployment,    Is.EqualTo(Deployment.LAN), "the assumed deployment is stored");
                Assert.That(pairing.RemoteEndpointDescription.Name,          Is.EqualTo("Fake endpoint"));
                Assert.That(pairing.InitiateSessionUrl,                      Is.EqualTo(fake.SessionInitiationUrl));
                Assert.That(fake.RequestsOf(RequestConnectionDetails),       Has.Count.EqualTo(1));
                Assert.That(fake.RequestsOf(PostConnectionDetails),          Is.Empty);
                Assert.That(fake.RequestsOf(FinalizePairing),                Has.Count.EqualTo(1));
                Assert.That(fake.RequestsOf(FinalizePairing)[0].Success,     Is.True);
                Assert.That(fake.RequestsOf(FinalizePairing)[0].Bearer,      Is.EqualTo(fake.LastIssuedPairingAttemptId));
            });

        }

        #endregion


        // The answers to requestConnectionDetails (S2 Connect 1.0.0, "6A." to "8A.")

        #region RequestConnectionDetails_401_ReturnsUnauthorized()

        [Test]
        [S2C("Pairing.6A")]
        [S2C("Pairing.PairingAttemptId")]
        public async Task RequestConnectionDetails_401_ReturnsUnauthorized()
        {

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl);

            fake.OnRequestConnectionDetails = (request, _) => Task.FromResult(EmptyResponse(request, HTTPStatusCode.Unauthorized));

            var result   = await client.PairAsync(fake.Token);
            var pairings = await client.Store.GetPairingsAsync();

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                              Is.EqualTo(PairingClientOutcome.Unauthorized), result.ToString());
                Assert.That(result.Operation,                            Is.EqualTo(RequestConnectionDetails));
                Assert.That(result.StatusCode?.Code,                     Is.EqualTo(401));
                Assert.That(result.Retryable,                            Is.True, "the attempt may be restarted at requestPairing");
                Assert.That(result.ServerResponse,                       Is.Not.Null);
                Assert.That(result.Pairing,                              Is.Null);
                Assert.That(pairings,                                    Is.Empty);
                Assert.That(fake.RequestsOf(RequestConnectionDetails),   Has.Count.EqualTo(1));
                Assert.That(fake.RequestsOf(RequestConnectionDetails)[0].Bearer, Is.EqualTo(fake.LastIssuedPairingAttemptId));
                Assert.That(fake.RequestsOf(FinalizePairing),            Is.Empty, "the server dropped the attempt, there is nothing to finalize");
            });

        }

        #endregion

        #region RequestConnectionDetails_403_ReturnsForbidden()

        [Test]
        [S2C("Pairing.7A")]
        public async Task RequestConnectionDetails_403_ReturnsForbidden()
        {

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl);

            fake.OnRequestConnectionDetails = (request, _) => Task.FromResult(EmptyResponse(request, HTTPStatusCode.Forbidden));

            var result   = await client.PairAsync(fake.Token);
            var pairings = await client.Store.GetPairingsAsync();

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                              Is.EqualTo(PairingClientOutcome.Forbidden), result.ToString());
                Assert.That(result.Operation,                            Is.EqualTo(RequestConnectionDetails));
                Assert.That(result.StatusCode?.Code,                     Is.EqualTo(403));
                Assert.That(result.Retryable,                            Is.False, "the pairing tokens differ, a new attempt needs user interaction");
                Assert.That(result.ServerResponse,                       Is.Not.Null);
                Assert.That(result.Pairing,                              Is.Null);
                Assert.That(pairings,                                    Is.Empty);
                Assert.That(fake.RequestsOf(RequestConnectionDetails),   Has.Count.EqualTo(1));
                Assert.That(fake.RequestsOf(FinalizePairing),            Is.Empty);
            });

        }

        #endregion

        #region RequestConnectionDetails_503ThreeTimesThen200_SucceedsAfterTheRetries()

        [Test]
        [S2C("Pairing.6A")]
        [S2C("Pairing.ServiceUnavailable")]
        public async Task RequestConnectionDetails_503ThreeTimesThen200_SucceedsAfterTheRetries()
        {

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl);

            Assert.That(client.Client.Options.MaxServiceUnavailableRetries, Is.EqualTo(3), "the default number of retries");

            var answered = 0;

            fake.OnRequestConnectionDetails = (request, _) => {

                var attempt = Interlocked.Increment(ref answered);

                return Task.FromResult(
                           attempt <= 3
                               ? EmptyResponse(request, HTTPStatusCode.ServiceUnavailable, RetryAfter: "0")
                               : fake.CorrectConnectionDetails(request)
                       );

            };

            var result   = await client.PairAsync(fake.Token);

            Assert.That(result.IsSuccess, Is.True, result.ToString());

            var pairing  = await client.Store.GetPairingAsync(client.Node.Id, fake.ServerNode.Id);

            Assert.Multiple(() => {
                Assert.That(answered,                                     Is.EqualTo(4), "one request and three retries");
                Assert.That(fake.RequestsOf(RequestConnectionDetails),    Has.Count.EqualTo(4));
                Assert.That(fake.RequestsOf(RequestConnectionDetails).Select(request => request.Bearer).Distinct().Count(), Is.EqualTo(1), "every retry uses the same pairingAttemptId");
                Assert.That(fake.RequestsOf(FinalizePairing),             Has.Count.EqualTo(1));
                Assert.That(fake.RequestsOf(FinalizePairing)[0].Success,  Is.True);
                Assert.That(pairing,                                      Is.Not.Null);
                Assert.That(result.Pairing,                               Is.EqualTo(pairing));
            });

        }

        #endregion

        #region RequestConnectionDetails_Always503_ReturnsServiceUnavailable()

        [Test]
        [S2C("Pairing.6A")]
        [S2C("Pairing.ServiceUnavailable")]
        public async Task RequestConnectionDetails_Always503_ReturnsServiceUnavailable()
        {

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl);

            fake.OnRequestConnectionDetails = (request, _) => Task.FromResult(EmptyResponse(request, HTTPStatusCode.ServiceUnavailable, RetryAfter: "0"));

            var result   = await client.PairAsync(fake.Token);
            var pairings = await client.Store.GetPairingsAsync();

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                              Is.EqualTo(PairingClientOutcome.ServiceUnavailable), result.ToString());
                Assert.That(result.Operation,                            Is.EqualTo(RequestConnectionDetails));
                Assert.That(result.StatusCode?.Code,                     Is.EqualTo(503));
                Assert.That(result.Retryable,                            Is.True);
                Assert.That(result.ServerResponse,                       Is.Not.Null);
                Assert.That(result.Pairing,                              Is.Null);
                Assert.That(pairings,                                    Is.Empty);
                Assert.That(fake.RequestsOf(RequestConnectionDetails),   Has.Count.EqualTo(1 + client.Client.Options.MaxServiceUnavailableRetries), "one request and every retry");
                Assert.That(fake.RequestsOf(FinalizePairing),            Is.Empty);
            });

        }

        #endregion

        #region RequestConnectionDetails_UnparseableConnectionDetails_ReturnsInvalidResponseAndFinalizesWithFalse()

        [Test]
        [S2C("Pairing.8A")]
        public async Task RequestConnectionDetails_UnparseableConnectionDetails_ReturnsInvalidResponseAndFinalizesWithFalse()
        {

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl);

            fake.OnRequestConnectionDetails = (request, _) => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK, new JObject(new JProperty("initiateSessionUrl", "not a URL"))));

            var result   = await client.PairAsync(fake.Token);
            var pairings = await client.Store.GetPairingsAsync();

            Assert.Multiple(() => {
                Assert.That(result.Outcome,             Is.EqualTo(PairingClientOutcome.InvalidResponse), result.ToString());
                Assert.That(result.Operation,           Is.EqualTo(RequestConnectionDetails));
                Assert.That(result.StatusCode?.Code,    Is.EqualTo(200));
                Assert.That(result.ServerResponse,      Is.Not.Null);
                Assert.That(result.Description,         Does.Contain("connection details"));
                Assert.That(result.Pairing,             Is.Null);
                Assert.That(pairings,                   Is.Empty);
            });

            AssertFinalizedWithFalse(fake);

        }

        #endregion

        #region RequestConnectionDetails_AnsweredAfterTheAttemptDeadline_ReturnsTimeout()

        [Test]
        [S2C("Pairing.Interruption")]
        public async Task RequestConnectionDetails_AnsweredAfterTheAttemptDeadline_ReturnsTimeout()
        {

            // The clock of the client is faked: the fake server "takes" 16 seconds to answer, more
            // than the 15 seconds of the attempt, without any real waiting.
            var clock = new FakeTimeProvider();

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl, TimeProvider: clock);

            fake.OnRequestConnectionDetails = (request, _) => {
                clock.Advance(TimeSpan.FromSeconds(16));
                return Task.FromResult(fake.CorrectConnectionDetails(request));
            };

            var result   = await client.PairAsync(fake.Token);
            var pairings = await client.Store.GetPairingsAsync();

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                              Is.EqualTo(PairingClientOutcome.Timeout), result.ToString());
                Assert.That(result.Operation,                            Is.EqualTo(RequestConnectionDetails));
                Assert.That(result.StatusCode?.Code,                     Is.EqualTo(408));
                Assert.That(result.Retryable,                            Is.True);
                Assert.That(result.ServerResponse,                       Is.Not.Null);
                Assert.That(result.Pairing,                              Is.Null, "connection details that arrive after the deadline must not be used");
                Assert.That(pairings,                                    Is.Empty);
                Assert.That(fake.RequestsOf(RequestConnectionDetails),   Has.Count.EqualTo(1));
                Assert.That(fake.RequestsOf(FinalizePairing),            Is.Empty, "the server considers the attempt failed after 15 seconds anyway");
            });

        }

        #endregion

        #region RequestConnectionDetails_NeverAnsweredWithinTheDeadline_GivesUp()

        [Test]
        [S2C("Pairing.Interruption")]
        public async Task RequestConnectionDetails_NeverAnsweredWithinTheDeadline_GivesUp()
        {

            await using var fake    = await FakePairingServer.StartAsync();

            var handlerFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // The fake answers two seconds later, long after the deadline of the attempt.
            fake.OnRequestConnectionDetails = async (request, _) => {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    return fake.CorrectConnectionDetails(request);
                }
                finally
                {
                    handlerFinished.TrySetResult();
                }
            };

            var impatient = PairingClientFixture.DefaultOptions() with {
                                PairingAttemptTimeout  = TimeSpan.FromMilliseconds(500),
                                RequestTimeout         = TimeSpan.FromSeconds(5)
                            };

            await using var client  = TestClient.Create(fake.PairingUrl, Options: impatient);

            var stopwatch  = Stopwatch.StartNew();
            var result     = await client.PairAsync(fake.Token);
            stopwatch.Stop();

            // Let the fake finish before it is stopped.
            await handlerFinished.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var pairings = await client.Store.GetPairingsAsync();

            Assert.Multiple(() => {
                // The remaining time of the attempt bounds the request; Hermod reports its request
                // timeout as a transport failure (status code 0), which the client maps to TransportFailure.
                Assert.That(result.Outcome,                              Is.EqualTo(PairingClientOutcome.Timeout).Or.EqualTo(PairingClientOutcome.TransportFailure), result.ToString());
                Assert.That(result.Operation,                            Is.EqualTo(RequestConnectionDetails));
                Assert.That(result.Retryable,                            Is.True);
                Assert.That(result.ServerResponse,                       Is.Not.Null);
                Assert.That(result.Pairing,                              Is.Null);
                Assert.That(pairings,                                    Is.Empty);
                Assert.That(stopwatch.Elapsed,                           Is.LessThan(TimeSpan.FromSeconds(1.9)), "the client gave up before the fake answered");
                Assert.That(fake.RequestsOf(RequestConnectionDetails),   Has.Count.EqualTo(1));
                Assert.That(fake.RequestsOf(FinalizePairing),            Is.Empty);
            });

        }

        #endregion


        // The answer to finalizePairing (S2 Connect 1.0.0, "9. POST /[version]/finalizePairing")

        #region FinalizePairing_400_ReturnsRejectedAtFinalizePairing()

        [Test]
        [S2C("Pairing.9")]
        public async Task FinalizePairing_400_ReturnsRejectedAtFinalizePairing()
        {

            await using var fake    = await FakePairingServer.StartAsync();
            await using var client  = TestClient.Create(fake.PairingUrl);

            fake.OnFinalizePairing = (request, _) => Task.FromResult(JSONResponse(request, HTTPStatusCode.BadRequest, new PairingResponseErrorMessage(PairingResponseError.Other, "the server is not yet satisfied").ToJSON()));

            var result   = await client.PairAsync(fake.Token);
            var pairings = await client.Store.GetPairingsAsync();

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                              Is.EqualTo(PairingClientOutcome.Rejected), result.ToString());
                Assert.That(result.Operation,                            Is.EqualTo(FinalizePairing));
                Assert.That(result.StatusCode?.Code,                     Is.EqualTo(400));
                Assert.That(result.Error?.ErrorMessage,                  Is.EqualTo(PairingResponseError.Other));
                Assert.That(result.Error?.AdditionalInfo,                Is.EqualTo("the server is not yet satisfied"));
                Assert.That(result.Retryable,                            Is.False);
                Assert.That(result.ServerResponse,                       Is.Not.Null);
                Assert.That(result.Pairing,                              Is.Null, "a pairing the server did not confirm must not be stored");
                Assert.That(pairings,                                    Is.Empty);
                Assert.That(fake.RequestsOf(RequestConnectionDetails),   Has.Count.EqualTo(1));
                Assert.That(fake.RequestsOf(FinalizePairing),            Has.Count.EqualTo(1));
                Assert.That(fake.RequestsOf(FinalizePairing)[0].Success, Is.True);
                Assert.That(fake.RequestsOf(FinalizePairing)[0].Bearer,  Is.EqualTo(fake.LastIssuedPairingAttemptId));
            });

        }

        #endregion


        // No server at all

        #region NoServerOnThePort_ReturnsTransportFailure()

        [Test]
        [S2C("Pairing.PairingURL")]
        public async Task NoServerOnThePort_ReturnsTransportFailure()
        {

            // A port that was free a moment ago and has no listener: the connection is refused at once.
            var port = PairingServerFixture.FreePort();

            await using var client = TestClient.Create(S2BaseURL.Parse($"http://127.0.0.1:{port}/pairing/", AllowHTTP: true));

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var versions  = await client.Client.GetVersionsAsync(timeout.Token);
            var result    = await client.PairAsync(PairingToken.Parse("ABCD2345"), timeout.Token);

            Assert.Multiple(() => {
                Assert.That(versions.IsSuccess,                Is.False, versions.ToString());
                Assert.That(versions.IsTransportFailure,       Is.True);
                Assert.That(versions.StatusCode.Code,          Is.EqualTo(0));
                Assert.That(versions.Value,                    Is.Null);
                Assert.That(versions.Description,              Is.Not.Null.And.Not.Empty);
                Assert.That(result.Outcome,                    Is.EqualTo(PairingClientOutcome.TransportFailure), result.ToString());
                Assert.That(result.Operation,                  Is.EqualTo("versionIndex"));
                Assert.That(result.StatusCode?.Code,           Is.EqualTo(0));
                Assert.That(result.Retryable,                  Is.True);
                Assert.That(result.Pairing,                    Is.Null);
                Assert.That(client.Client.SelectedAPIVersion,  Is.Null);
            });

        }

        #endregion

    }

}

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
using System.Net.Sockets;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// A pairing server on a Hermod HTTP server bound to a free loopback port, reachable
    /// through a plain <see cref="HttpClient"/> (an independent, non-Hermod client), for the
    /// Phase 6 tests. Plain HTTP is used (TLS is exercised in Phase 11), so the parser options
    /// allow insecure URLs and the LAN challenge-response uses a fixed fake certificate fingerprint.
    /// </summary>
    internal sealed class PairingServerFixture : IAsyncDisposable
    {

        #region Data

        /// <summary>
        /// The fake fingerprint of the (non-existent) TLS server certificate of LAN fixtures.
        /// </summary>
        public static readonly CertificateFingerprint  Fingerprint
            = CertificateFingerprint.Parse("A1:B2:C3:D4:E5:F6:07:18:29:3A:4B:5C:6D:7E:8F:90:A1:B2:C3:D4:E5:F6:07:18:29:3A:4B:5C:6D:7E:8F:90");

        /// <summary>
        /// The default pairing path.
        /// </summary>
        public const String  DefaultPairingPath  = "/pairing/";

        /// <summary>
        /// The default session initiation path.
        /// </summary>
        public const String  DefaultSessionPath  = "/connection/";

        #endregion

        #region Properties

        public IPPort                Port                    { get; }
        public HTTPServer            HTTPServer              { get; }
        public LocalEndpoint         Endpoint                { get; }
        public IS2Store              Store                   { get; }
        public PairingServerAPI      API                     { get; }
        public HttpClient            Client                  { get; }
        public S2BaseURL             PairingUrl              { get; }
        public S2BaseURL?            SessionInitiationUrl    { get; }
        public PairingServerOptions  Options                 { get; }

        /// <summary>
        /// The base address of the pairing API (the pairing URL).
        /// </summary>
        public Uri                   BaseAddress
            => new (PairingUrl.Value);

        /// <summary>
        /// The events collected during the test.
        /// </summary>
        public List<PairingAttempt>  CompletedAttempts       { get; } = [];
        public List<(Pairing Pairing, Pairing? Replaced, IReadOnlyList<Pairing> Superseded)>  CompletedPairings  { get; } = [];
        public List<PreparePairingRequest>        PreparePairingRequests        { get; } = [];
        public List<CancelPreparePairingRequest>  CancelPreparePairingRequests  { get; } = [];

        #endregion

        #region Constructor(s)

        private PairingServerFixture(IPPort                Port,
                                     HTTPServer            HTTPServer,
                                     LocalEndpoint         Endpoint,
                                     IS2Store              Store,
                                     PairingServerAPI      API,
                                     S2BaseURL             PairingUrl,
                                     S2BaseURL?            SessionInitiationUrl,
                                     PairingServerOptions  Options)
        {

            this.Port                  = Port;
            this.HTTPServer            = HTTPServer;
            this.Endpoint              = Endpoint;
            this.Store                 = Store;
            this.API                   = API;
            this.PairingUrl            = PairingUrl;
            this.SessionInitiationUrl  = SessionInitiationUrl;
            this.Options               = Options;

            this.Client                = new HttpClient {
                                             BaseAddress  = BaseAddress,
                                             Timeout      = TimeSpan.FromSeconds(60)
                                         };

            API.OnPairingAttemptCompleted  += (timestamp, sender, attempt) => {
                                                  lock (CompletedAttempts)
                                                      CompletedAttempts.Add(attempt);
                                                  return Task.CompletedTask;
                                              };

            API.OnPairingCompleted         += (timestamp, sender, attempt, pairing, replaced, superseded) => {
                                                  lock (CompletedPairings)
                                                      CompletedPairings.Add((pairing, replaced, superseded));
                                                  return Task.CompletedTask;
                                              };

            API.OnPreparePairing           += (timestamp, sender, request, node, remote) => {
                                                  lock (PreparePairingRequests)
                                                      PreparePairingRequests.Add(request);
                                                  return Task.CompletedTask;
                                              };

            API.OnCancelPreparePairing     += (timestamp, sender, request, node, remote) => {
                                                  lock (CancelPreparePairingRequests)
                                                      CancelPreparePairingRequests.Add(request);
                                                  return Task.CompletedTask;
                                              };

        }

        #endregion


        #region (static) CreateAsync(...)

        /// <summary>
        /// Start a pairing server.
        /// </summary>
        /// <param name="Deployment">The deployment of the endpoint (default: LAN).</param>
        /// <param name="IsWANPairingServerForLANEndpoint">Whether this is a WAN pairing server acting for a LAN endpoint.</param>
        /// <param name="Options">Optional server options (default: a 10 ms requestPairing delay, insecure URLs allowed).</param>
        /// <param name="SubnetPolicy">An optional subnet policy (default: allow all).</param>
        /// <param name="WithSessionInitiationUrl">Whether the endpoint has a session initiation URL (default: true).</param>
        /// <param name="WithFingerprint">Whether the endpoint knows its certificate fingerprint (default: true).</param>
        /// <param name="Store">An optional store (default: a new in-memory store).</param>
        /// <param name="PairingPath">The path of the pairing URL (default: "/pairing/").</param>
        /// <param name="EndpointName">An optional endpoint name.</param>
        /// <param name="Host">The host of the pairing URL (default: 127.0.0.1).</param>
        public static async Task<PairingServerFixture> CreateAsync(Deployment?            Deployment                         = null,
                                                                   Boolean                IsWANPairingServerForLANEndpoint   = false,
                                                                   PairingServerOptions?  Options                            = null,
                                                                   ISubnetPolicy?         SubnetPolicy                       = null,
                                                                   Boolean                WithSessionInitiationUrl           = true,
                                                                   Boolean                WithFingerprint                    = true,
                                                                   IS2Store?              Store                              = null,
                                                                   String                 PairingPath                        = DefaultPairingPath,
                                                                   String?                EndpointName                       = null,
                                                                   String                 Host                               = "127.0.0.1")
        {

            var port        = FreePort();
            var deployment  = Deployment ?? S2.Connect.Deployment.LAN;
            var options     = Options ?? DefaultOptions();

            var pairingUrl  = S2BaseURL.Parse($"http://{Host}:{port}{PairingPath}", AllowHTTP: true);
            var sessionUrl  = WithSessionInitiationUrl
                                  ? S2BaseURL.Parse($"http://{Host}:{port}{DefaultSessionPath}", AllowHTTP: true)
                                  : (S2BaseURL?) null;

            var endpoint    = new LocalEndpoint(
                                  new EndpointDescription(EndpointName ?? $"Test {deployment} endpoint"),
                                  deployment,
                                  pairingUrl,
                                  sessionUrl,
                                  IsWANPairingServerForLANEndpoint,
                                  WithFingerprint ? () => Fingerprint : null
                              );

            var store       = Store ?? new InMemoryS2Store();

            var httpServer  = new HTTPServer(
                                  IPv4Address.Parse("127.0.0.1"),
                                  port,
                                  "S2 test HTTP server",
                                  AutoStart: false
                              );

            var api         = new PairingServerAPI(
                                  httpServer,
                                  endpoint,
                                  store,
                                  Options:       options,
                                  SubnetPolicy:  SubnetPolicy ?? AllowAllSubnetPolicy.Instance
                              );

            await httpServer.Start();

            return new PairingServerFixture(port, httpServer, endpoint, store, api, pairingUrl, sessionUrl, options);

        }

        #endregion

        #region (static) DefaultOptions()

        /// <summary>
        /// The default test options: a 10 ms requestPairing delay and insecure URLs allowed.
        /// </summary>
        public static PairingServerOptions DefaultOptions()

            => new () {
                   RequestPairingDelay  = TimeSpan.FromMilliseconds(10),
                   ParserOptions        = new S2ParserOptions { AllowInsecureURLs = true }
               };

        #endregion

        #region (static) FreePort()

        public static IPPort FreePort()
        {
            var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
            listener.Stop();
            return IPPort.Parse((UInt16) port);
        }

        #endregion


        #region AddNode(Role, Alias = null, ...)

        /// <summary>
        /// Add a hosted node with a generated identification.
        /// </summary>
        public HostedNode AddNode(EnergyManagementRole                  Role,
                                  NodeIdAlias?                          Alias                             = null,
                                  IEnumerable<String>?                  SupportedS2MessageVersions        = null,
                                  IEnumerable<CommunicationProtocol>?   SupportedCommunicationProtocols   = null,
                                  String?                               Brand                             = null)

            => Endpoint.AddNode(
                   new NodeDescription(
                       Node_Id.NewRandom,
                       Brand ?? "GraphDefined",
                       Role == EnergyManagementRole.CEM ? "EMS" : "EV charger",
                       Role == EnergyManagementRole.CEM ? "TestCEM 1" : "TestRM 1",
                       Role
                   ),
                   Alias,
                   SupportedS2MessageVersions,
                   SupportedCommunicationProtocols
               );

        #endregion


        #region GetAsync (RelativePath)

        /// <summary>
        /// GET a path relative to the pairing URL, e.g. "" for the version index or "v1/nodes".
        /// </summary>
        public async Task<HTTPResult> GetAsync(String RelativePath)
        {

            using var response = await Client.GetAsync(new Uri(RelativePath, UriKind.Relative));

            return await HTTPResult.FromAsync(response);

        }

        #endregion

        #region PostAsync(RelativePath, Body = null, Bearer = null, ContentType = "application/json", RawBody = null)

        /// <summary>
        /// POST a JSON body to a path relative to the pairing URL, e.g. "v1/requestPairing".
        /// </summary>
        public async Task<HTTPResult> PostAsync(String   RelativePath,
                                                JToken?  Body          = null,
                                                String?  Bearer        = null,
                                                String   ContentType   = "application/json",
                                                String?  RawBody       = null)
        {

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(RelativePath, UriKind.Relative));

            var text = RawBody ?? Body?.ToString(Formatting.None);

            if (text is not null)
                request.Content = new StringContent(text, Encoding.UTF8, ContentType);

            if (Bearer is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Bearer);

            using var response = await Client.SendAsync(request);

            return await HTTPResult.FromAsync(response);

        }

        #endregion


        #region DisposeAsync()

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await API.DisposeAsync();
            await HTTPServer.Stop();
        }

        #endregion

    }


    /// <summary>
    /// The relevant parts of an HTTP response.
    /// </summary>
    internal sealed record HTTPResult(HttpStatusCode  Status,
                                      String?         Body,
                                      JToken?         JSON,
                                      String?         ContentType,
                                      String?         WWWAuthenticate,
                                      String?         RetryAfter,
                                      String?         CacheControl)
    {

        public JObject  Object
            => JSON as JObject ?? throw new InvalidOperationException($"No JSON object in the {(Int32) Status} response: '{Body}'");

        public JArray   Array
            => JSON as JArray  ?? throw new InvalidOperationException($"No JSON array in the {(Int32) Status} response: '{Body}'");

        public String?  ErrorMessage
            => (JSON as JObject)?["errorMessage"]?.Value<String>();

        public String?  AdditionalInfo
            => (JSON as JObject)?["additionalInfo"]?.Value<String>();

        public static async Task<HTTPResult> FromAsync(HttpResponseMessage Response)
        {

            var body = await Response.Content.ReadAsStringAsync();

            JToken? json = null;

            if (!String.IsNullOrWhiteSpace(body))
            {
                try
                {
                    json = JToken.Parse(body);
                }
                catch (JsonException)
                { }
            }

            return new HTTPResult(
                       Response.StatusCode,
                       body,
                       json,
                       Response.Content.Headers.ContentType?.ToString(),
                       Response.Headers.WwwAuthenticate.Count > 0 ? String.Join(", ", Response.Headers.WwwAuthenticate.Select(h => h.ToString())) : null,
                       Response.Headers.RetryAfter?.ToString(),
                       Response.Headers.CacheControl?.ToString()
                   );

        }

    }


    /// <summary>
    /// A pairing client for the tests: it owns the client node and endpoint descriptions, the
    /// pairing token it shares with the server node and its client challenge, computes the
    /// expected challenge responses and builds the request bodies.
    /// </summary>
    internal sealed class TestPairingClient
    {

        public NodeDescription      Node                  { get; }
        public EndpointDescription  Endpoint              { get; }
        public PairingToken         Token                 { get; }
        public HmacChallenge        Challenge             { get; }
        public Deployment           Deployment            { get; }

        public TestPairingClient(EnergyManagementRole  Role,
                                 Deployment?           Deployment     = null,
                                 PairingToken?         Token          = null,
                                 String?               EndpointName   = null)
        {

            this.Deployment  = Deployment ?? S2.Connect.Deployment.LAN;
            this.Node        = new NodeDescription(Node_Id.NewRandom,
                                                   "ACME",
                                                   Role == EnergyManagementRole.CEM ? "EMS" : "heat pump",
                                                   Role == EnergyManagementRole.CEM ? "ClientCEM" : "ClientRM",
                                                   Role);
            this.Endpoint    = new EndpointDescription(EndpointName ?? "Test client endpoint", null, this.Deployment);
            this.Token       = Token ?? PairingToken.Parse("ABCD2345");
            this.Challenge   = TokenGenerator.NewChallenge();

        }

        /// <summary>
        /// Build a requestPairing request.
        /// </summary>
        public RequestPairingRequest RequestPairing(PairingTarget?                       Target        = null,
                                                    Boolean                              ForcePairing  = false,
                                                    IEnumerable<String>?                 Versions      = null,
                                                    IEnumerable<CommunicationProtocol>?  Protocols     = null,
                                                    IEnumerable<HmacHashingAlgorithm>?   Algorithms    = null)

            => new (Node,
                    Endpoint,
                    [.. Protocols  ?? [ CommunicationProtocol.WebSocket ]],
                    [.. Versions   ?? [ Version.S2JSONVersion ]],
                    [.. Algorithms ?? [ HmacHashingAlgorithm.SHA256 ]],
                    Challenge,
                    Target,
                    ForcePairing);

        /// <summary>
        /// The response the server must give to the client challenge.
        /// </summary>
        public HmacChallengeResponse ExpectedClientChallengeResponse(PairingServerFixture Fixture)

            => Fixture.API.UsesLANChallengeResponse
                   ? ChallengeResponse.ComputeForLAN(HmacHashingAlgorithm.SHA256, Challenge, Token, PairingServerFixture.Fingerprint)
                   : ChallengeResponse.ComputeForWAN(HmacHashingAlgorithm.SHA256, Challenge, Token, Fixture.Endpoint.DomainName);

        /// <summary>
        /// The response of this client to the server challenge.
        /// </summary>
        public HmacChallengeResponse ServerChallengeResponse(PairingServerFixture  Fixture,
                                                             HmacChallenge         ServerChallenge)

            => Fixture.API.UsesLANChallengeResponse
                   ? ChallengeResponse.ComputeForLAN(HmacHashingAlgorithm.SHA256, ServerChallenge, Token, PairingServerFixture.Fingerprint)
                   : ChallengeResponse.ComputeForWAN(HmacHashingAlgorithm.SHA256, ServerChallenge, Token, Fixture.Endpoint.DomainName);

        /// <summary>
        /// Connection details this client offers when it becomes the communication server.
        /// </summary>
        public ConnectionDetails ConnectionDetails(String Host = "127.0.0.1", Int32 Port = 8443)

            => new (S2BaseURL.Parse($"http://{Host}:{Port}/connection/", AllowHTTP: true),
                    TokenGenerator.NewAccessToken(),
                    new Dictionary<String, CertificateFingerprint> { ["SHA256"] = PairingServerFixture.Fingerprint });

    }

}

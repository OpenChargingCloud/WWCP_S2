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

using System.Net.Http.Headers;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Session;
using cloud.charging.open.protocols.S2.WebSockets;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// A communication server (CEM: session initiation API on a Hermod HTTP server plus an
    /// S2 WebSocket server sharing the communication token store) and a communication client
    /// (RM: session initiation client) that are already paired, for the Phase 8 tests.
    /// Plain HTTP and ws:// are used (TLS is exercised in Phase 11).
    /// </summary>
    internal sealed class SessionInitiationFixture : IAsyncDisposable
    {

        #region Properties

        // Communication server (CEM)
        public IPPort                       HTTPPort                { get; }
        public IPPort                       WebSocketPort           { get; }
        public HTTPServer                   HTTPServer              { get; }
        public S2WebSocketServer            WebSocketServer         { get; }
        public CommunicationTokenStore      TokenStore              { get; }
        public LocalEndpoint                ServerEndpoint          { get; }
        public InMemoryS2Store              ServerStore             { get; }
        public HostedNode                   CEM                     { get; }
        public SessionInitiationServerAPI   API                     { get; }
        public S2BaseURL                    SessionInitiationUrl    { get; }
        public URL                          WebSocketUrl            { get; }
        public HttpClient                   HttpClient              { get; }

        // Communication client (RM)
        public LocalEndpoint                ClientEndpoint          { get; }
        public InMemoryS2Store              ClientStore             { get; }
        public HostedNode                   RM                      { get; }
        public SessionInitiationClient      Client                  { get; }

        // Observations
        public List<S2Session>                            ServerSessions       { get; } = [];
        public List<(S2Session Session, Object? Identity)> ServerSessionStarts  { get; } = [];
        public List<Pairing>                              UnpairedAtServer     { get; } = [];
        public List<SessionInitiationClientResult>        ClientResults        { get; } = [];

        #endregion

        #region Constructor(s)

        private SessionInitiationFixture(IPPort                      HTTPPort,
                                         IPPort                      WebSocketPort,
                                         HTTPServer                  HTTPServer,
                                         S2WebSocketServer           WebSocketServer,
                                         CommunicationTokenStore     TokenStore,
                                         LocalEndpoint               ServerEndpoint,
                                         InMemoryS2Store             ServerStore,
                                         HostedNode                  CEM,
                                         SessionInitiationServerAPI  API,
                                         S2BaseURL                   SessionInitiationUrl,
                                         URL                         WebSocketUrl,
                                         LocalEndpoint               ClientEndpoint,
                                         InMemoryS2Store             ClientStore,
                                         HostedNode                  RM,
                                         SessionInitiationClient     Client)
        {

            this.HTTPPort              = HTTPPort;
            this.WebSocketPort         = WebSocketPort;
            this.HTTPServer            = HTTPServer;
            this.WebSocketServer       = WebSocketServer;
            this.TokenStore            = TokenStore;
            this.ServerEndpoint        = ServerEndpoint;
            this.ServerStore           = ServerStore;
            this.CEM                   = CEM;
            this.API                   = API;
            this.SessionInitiationUrl  = SessionInitiationUrl;
            this.WebSocketUrl          = WebSocketUrl;
            this.ClientEndpoint        = ClientEndpoint;
            this.ClientStore           = ClientStore;
            this.RM                    = RM;
            this.Client                = Client;

            this.HttpClient            = new HttpClient {
                                             BaseAddress  = new Uri(SessionInitiationUrl.Value),
                                             Timeout      = TimeSpan.FromSeconds(60)
                                         };

            WebSocketServer.OnSessionStarted += (timestamp, server, session, identity) => {
                                                    lock (ServerSessions)
                                                    {
                                                        ServerSessions.Add(session);
                                                        ServerSessionStarts.Add((session, identity));
                                                    }
                                                    return Task.CompletedTask;
                                                };

            API.OnUnpaired                   += (timestamp, sender, pairing, byRemote) => {
                                                    lock (UnpairedAtServer)
                                                        UnpairedAtServer.Add(pairing);
                                                    return Task.CompletedTask;
                                                };

            Client.OnCompleted               += (timestamp, sender, local, server, result) => {
                                                    lock (ClientResults)
                                                        ClientResults.Add(result);
                                                    return Task.CompletedTask;
                                                };

        }

        #endregion


        #region (static) CreateAsync(...)

        /// <summary>
        /// Start the communication server and create the client; the nodes are paired with
        /// a shared access token unless <paramref name="Paired"/> is false.
        /// </summary>
        /// <param name="ServerOptions">Optional server options (default: insecure URLs allowed).</param>
        /// <param name="ClientOptions">Optional client options (default: insecure URLs allowed, short retry delay).</param>
        /// <param name="Paired">Whether the nodes start paired (default: true).</param>
        /// <param name="ServerStore">An optional server store.</param>
        /// <param name="ClientStore">An optional client store.</param>
        public static async Task<SessionInitiationFixture> CreateAsync(SessionInitiationServerOptions?  ServerOptions   = null,
                                                                       SessionInitiationClientOptions?  ClientOptions   = null,
                                                                       Boolean                          Paired          = true,
                                                                       InMemoryS2Store?                 ServerStore     = null,
                                                                       InMemoryS2Store?                 ClientStore     = null)
        {

            var httpPort        = PairingServerFixture.FreePort();
            var webSocketPort   = PairingServerFixture.FreePort();

            var pairingUrl      = S2BaseURL.Parse($"http://127.0.0.1:{httpPort}/pairing/",    AllowHTTP: true);
            var sessionUrl      = S2BaseURL.Parse($"http://127.0.0.1:{httpPort}/connection/", AllowHTTP: true);
            var webSocketUrl    = URL.Parse($"ws://127.0.0.1:{webSocketPort}/");

            var serverEndpoint  = new LocalEndpoint(new EndpointDescription("Test CEM endpoint"),
                                                    Deployment.LAN,
                                                    pairingUrl,
                                                    sessionUrl,
                                                    ServerCertificateFingerprint: () => PairingServerFixture.Fingerprint);

            var cem             = serverEndpoint.AddNode(new NodeDescription(Node_Id.NewRandom, "GraphDefined", "EMS", "TestCEM 1", EnergyManagementRole.CEM));

            var serverStore     = ServerStore ?? new InMemoryS2Store();
            var tokenStore      = new CommunicationTokenStore();

            var webSocketServer = new S2WebSocketServer(
                                      IPv4Address.Parse("127.0.0.1"),
                                      webSocketPort,
                                      EnergyManagementRole.CEM,
                                      TokenStore:             tokenStore,
                                      SessionOptionsFactory:  (connection, identity) => new S2SessionOptions {
                                                                  Role               = EnergyManagementRole.CEM,
                                                                  Mode               = S2SessionMode.S2Connect,
                                                                  NegotiatedVersion  = (identity as S2ConnectSessionIdentity)?.S2MessageVersion ?? Version.S2JSONVersion
                                                              }
                                  );

            var httpServer      = new HTTPServer(IPv4Address.Parse("127.0.0.1"), httpPort, "S2 test HTTP server", AutoStart: false);

            var serverOptions   = ServerOptions ?? DefaultServerOptions();

            var api             = new SessionInitiationServerAPI(httpServer,
                                                                 serverEndpoint,
                                                                 serverStore,
                                                                 tokenStore,
                                                                 webSocketUrl,
                                                                 Options: serverOptions);

            await webSocketServer.Start();
            await httpServer.Start();

            var clientEndpoint  = new LocalEndpoint(new EndpointDescription("Test RM endpoint"),
                                                    Deployment.LAN,
                                                    S2BaseURL.Parse("http://127.0.0.1:1/pairing/", AllowHTTP: true));

            var rm              = clientEndpoint.AddNode(new NodeDescription(Node_Id.NewRandom, "ACME", "heat pump", "TestRM 1", EnergyManagementRole.RM));

            var clientStore     = ClientStore ?? new InMemoryS2Store();

            var client          = new SessionInitiationClient(sessionUrl,
                                                              clientEndpoint,
                                                              clientStore,
                                                              ClientOptions ?? DefaultClientOptions());

            var fixture         = new SessionInitiationFixture(httpPort, webSocketPort, httpServer, webSocketServer, tokenStore,
                                                               serverEndpoint, serverStore, cem, api, sessionUrl, webSocketUrl,
                                                               clientEndpoint, clientStore, rm, client);

            if (Paired)
                await fixture.PairAsync();

            return fixture;

        }

        #endregion

        #region (static) DefaultServerOptions() / DefaultClientOptions()

        public static SessionInitiationServerOptions DefaultServerOptions()
            => new () { ParserOptions = new S2ParserOptions { AllowInsecureURLs = true } };

        public static SessionInitiationClientOptions DefaultClientOptions()
            => new () {
                   ParserOptions                 = new S2ParserOptions { AllowInsecureURLs = true },
                   ServiceUnavailableRetryDelay  = TimeSpan.FromMilliseconds(100)
               };

        #endregion


        #region PairAsync(Token = null)

        /// <summary>
        /// Store a pairing of the CEM (communication server) and the RM (communication client)
        /// on both sides with the given or a fresh access token.
        /// </summary>
        public async Task<AccessToken> PairAsync(AccessToken? Token = null)
        {

            var token = Token ?? TokenGenerator.NewAccessToken();
            var now   = DateTimeOffset.UtcNow;

            await ServerStore.AddOrReplacePairingAsync(new Pairing(CEM.Id, RM.Description, ClientEndpoint.Description, CommunicationRole.CommunicationServer, token, now));
            await ClientStore.AddOrReplacePairingAsync(new Pairing(RM.Id, CEM.Description, ServerEndpoint.Description, CommunicationRole.CommunicationClient, token, now, SessionInitiationUrl));

            return token;

        }

        #endregion

        #region PostAsync(RelativePath, Body = null, Bearer = null, RawBody = null, ContentType = "application/json")

        /// <summary>
        /// POST to a path relative to the session initiation URL, e.g. "v1/initiateSession".
        /// </summary>
        public async Task<HTTPResult> PostAsync(String   RelativePath,
                                                JToken?  Body          = null,
                                                String?  Bearer        = null,
                                                String?  RawBody       = null,
                                                String   ContentType   = "application/json")
        {

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(RelativePath, UriKind.Relative));

            var text = RawBody ?? Body?.ToString(Formatting.None);

            if (text is not null)
                request.Content = new StringContent(text, Encoding.UTF8, ContentType);

            if (Bearer is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Bearer);

            using var response = await HttpClient.SendAsync(request);

            return await HTTPResult.FromAsync(response);

        }

        #endregion

        #region GetAsync(RelativePath)

        public async Task<HTTPResult> GetAsync(String RelativePath)
        {

            using var response = await HttpClient.GetAsync(new Uri(RelativePath, UriKind.Relative));

            return await HTTPResult.FromAsync(response);

        }

        #endregion

        #region InitiateSessionRequest()

        /// <summary>
        /// A well-formed initiateSession request of the RM for the CEM.
        /// </summary>
        public InitiateSessionRequest InitiateSessionRequest()
            => new (RM.Id, CEM.Id, RM.SupportedS2MessageVersions, RM.SupportedCommunicationProtocols);

        #endregion


        #region DisposeAsync()

        public async ValueTask DisposeAsync()
        {

            HttpClient.Dispose();

            await Client.DisposeAsync();

            try
            {
                await HTTPServer.Stop();
            }
            catch (Exception)
            { }

            try
            {
                await WebSocketServer.Shutdown();
            }
            catch (Exception)
            { }

        }

        #endregion

    }

}

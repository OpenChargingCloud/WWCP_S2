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

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect.Discovery
{

    /// <summary>
    /// The reference WAN endpoint registry API (<see cref="WANRegistryAPI"/>) on a Hermod HTTP
    /// server bound to a free loopback port, backed by an <see cref="InMemoryWANRegistry"/> and
    /// reachable through a <see cref="WANRegistryClient"/> as well as through a plain
    /// <see cref="HttpClient"/> (an independent, non-Hermod client) for raw HTTP assertions,
    /// for the Phase 9c tests. Plain HTTP is used, so the client accepts insecure URLs.
    /// </summary>
    internal sealed class WANRegistryFixture : IAsyncDisposable
    {

        #region Data

        /// <summary>
        /// The default root path of the registry API.
        /// </summary>
        public const String  DefaultRootPath  = "/registry/";

        /// <summary>
        /// The loopback host the HTTP server binds to.
        /// </summary>
        public const String  Host             = "127.0.0.1";

        #endregion

        #region Properties

        public IPPort               Port          { get; }
        public HTTPServer           HTTPServer    { get; }
        public InMemoryWANRegistry  Registry      { get; }
        public WANRegistryAPI       API           { get; }

        /// <summary>
        /// The registry URL (the base of the version index), e.g. "http://127.0.0.1:12345/registry/".
        /// </summary>
        public S2BaseURL            RegistryUrl   { get; }

        /// <summary>
        /// A WAN registry client of this registry (insecure URLs allowed).
        /// </summary>
        public WANRegistryClient    Client        { get; }

        /// <summary>
        /// A plain HTTP client with the registry URL as base address.
        /// </summary>
        public HttpClient           HTTP          { get; }

        /// <summary>
        /// The base address of the plain HTTP client (the registry URL).
        /// </summary>
        public Uri                  BaseAddress
            => new (RegistryUrl.Value);

        #endregion

        #region Constructor(s)

        private WANRegistryFixture(IPPort               Port,
                                   HTTPServer           HTTPServer,
                                   InMemoryWANRegistry  Registry,
                                   WANRegistryAPI       API,
                                   S2BaseURL            RegistryUrl)
        {

            this.Port         = Port;
            this.HTTPServer   = HTTPServer;
            this.Registry     = Registry;
            this.API          = API;
            this.RegistryUrl  = RegistryUrl;

            this.Client       = new WANRegistryClient(RegistryUrl, ClientOptions());

            this.HTTP         = new HttpClient {
                                    BaseAddress  = BaseAddress,
                                    Timeout      = TimeSpan.FromSeconds(60)
                                };

        }

        #endregion


        #region (static) CreateAsync(Records = null, RootPath = DefaultRootPath)

        /// <summary>
        /// Start a WAN registry API with the given endpoint records.
        /// </summary>
        /// <param name="Records">Optional initial endpoint records (default: none).</param>
        /// <param name="RootPath">The root path of the registry API (default: "/registry/"; "/" mounts it at the root).</param>
        /// <param name="RateLimitCapacity">The request budget per remote address (default: the API default; zero disables the limit).</param>
        /// <param name="RateLimitRefillPeriod">The period within which that budget refills.</param>
        public static async Task<WANRegistryFixture> CreateAsync(IEnumerable<EndpointRecord>?  Records                = null,
                                                                 String                        RootPath               = DefaultRootPath,
                                                                 Int32?                        RateLimitCapacity      = null,
                                                                 TimeSpan?                     RateLimitRefillPeriod  = null)
        {

            var rootPath     = NormaliseRootPath(RootPath);
            var port         = PairingServerFixture.FreePort();
            var registry     = new InMemoryWANRegistry(Records);
            var registryUrl  = S2BaseURL.Parse($"http://{Host}:{port}{rootPath}", AllowHTTP: true);

            var httpServer   = new HTTPServer(
                                   IPv4Address.Parse(Host),
                                   port,
                                   "S2 test registry",
                                   AutoStart: false
                               );

            var api          = new WANRegistryAPI(
                                   httpServer,
                                   registry,
                                   HTTPPath.Parse(rootPath),
                                   RateLimitCapacity,
                                   RateLimitRefillPeriod
                               );

            await httpServer.Start();

            return new WANRegistryFixture(port, httpServer, registry, api, registryUrl);

        }

        /// <summary>
        /// A root path always starts and ends with a slash.
        /// </summary>
        private static String NormaliseRootPath(String RootPath)
        {

            var path = RootPath.Trim();

            if (!path.StartsWith('/'))
                path = "/" + path;

            if (!path.EndsWith('/'))
                path += "/";

            return path;

        }

        #endregion

        #region (static) ClientOptions()

        /// <summary>
        /// The default client options of the tests: insecure ("http://") URLs allowed.
        /// </summary>
        public static WANRegistryClientOptions ClientOptions()

            => new () {
                   ParserOptions = new S2ParserOptions { AllowInsecureURLs = true }
               };

        #endregion

        #region CreateClient(Options = null)

        /// <summary>
        /// Create another WAN registry client of this registry.
        /// </summary>
        /// <param name="Options">Optional client options (default: insecure URLs allowed).</param>
        public WANRegistryClient CreateClient(WANRegistryClientOptions? Options = null)
            => new (RegistryUrl, Options ?? ClientOptions());

        #endregion

        #region (static) Record(Name, Regions, Status, CEM, RM, Id = null)

        /// <summary>
        /// Create an endpoint record with example icons and a pairing URL derived from the name.
        /// </summary>
        /// <param name="Name">The user facing name of the endpoint.</param>
        /// <param name="Regions">The ISO 3166-1 alpha-2 country codes of the endpoint.</param>
        /// <param name="Status">The status of the endpoint.</param>
        /// <param name="CEM">Whether the endpoint represents CEMs.</param>
        /// <param name="RM">Whether the endpoint represents RMs.</param>
        /// <param name="Id">An optional identification (default: a new random one).</param>
        public static EndpointRecord Record(String               Name,
                                            IEnumerable<String>  Regions,
                                            EndpointStatus       Status,
                                            Boolean              CEM,
                                            Boolean              RM,
                                            EndpointRecord_Id?   Id   = null)
        {

            var host = new String(Name.Where(character => Char.IsLetterOrDigit(character)).ToArray()).ToLowerInvariant();

            if (host.Length == 0)
                host = "endpoint";

            return new (Id ?? EndpointRecord_Id.NewRandom,
                        Name,
                        $"The {Name} endpoint",
                        URL.Parse("https://example.org/icon32.png"),
                        URL.Parse("https://example.org/icon128.png"),
                        URL.Parse("https://example.org/icon512.png"),
                        S2BaseURL.Parse($"https://{host}.example.org/pairing/"),
                        [.. Regions.Select(CountryCode.Parse)],
                        Status,
                        CEM,
                        RM);

        }

        #endregion


        #region GetAsync(RelativePath)

        /// <summary>
        /// GET a path relative to the registry URL with the plain HTTP client, e.g. "" for the
        /// version index or "v1/endpoint?region=NL,BE".
        /// </summary>
        /// <param name="RelativePath">A path (with an optional query) relative to the registry URL.</param>
        public async Task<HTTPResult> GetAsync(String RelativePath)
        {

            using var response = await HTTP.GetAsync(new Uri(RelativePath, UriKind.Relative));

            return await HTTPResult.FromAsync(response);

        }

        #endregion


        #region DisposeAsync()

        public async ValueTask DisposeAsync()
        {
            HTTP.Dispose();
            await Client.DisposeAsync();
            await HTTPServer.Stop();
        }

        #endregion

    }


    /// <summary>
    /// The endpoint records shared by the registry tests: three public endpoints (a Dutch CEM,
    /// a German RM and a Benelux endpoint hosting both) and one testing endpoint, in this
    /// insertion order.
    /// </summary>
    internal sealed class SampleEndpointRecords
    {

        /// <summary>
        /// "Alpha": NL, public, CEM only.
        /// </summary>
        public EndpointRecord  Alpha    { get; } = WANRegistryFixture.Record("Alpha", [ "NL" ],       EndpointStatus.Public,  CEM: true,  RM: false);

        /// <summary>
        /// "Beta": DE, public, RM only.
        /// </summary>
        public EndpointRecord  Beta     { get; } = WANRegistryFixture.Record("Beta",  [ "DE" ],       EndpointStatus.Public,  CEM: false, RM: true);

        /// <summary>
        /// "Gamma": BE and LU, public, CEM and RM.
        /// </summary>
        public EndpointRecord  Gamma    { get; } = WANRegistryFixture.Record("Gamma", [ "BE", "LU" ], EndpointStatus.Public,  CEM: true,  RM: true);

        /// <summary>
        /// "Delta": NL, testing, CEM and RM.
        /// </summary>
        public EndpointRecord  Delta    { get; } = WANRegistryFixture.Record("Delta", [ "NL" ],       EndpointStatus.Testing, CEM: true,  RM: true);

        /// <summary>
        /// Every record, in insertion order.
        /// </summary>
        public IReadOnlyList<EndpointRecord>  All
            => [ Alpha, Beta, Gamma, Delta ];

        /// <summary>
        /// The public records, in insertion order.
        /// </summary>
        public IReadOnlyList<EndpointRecord>  Public
            => [ Alpha, Beta, Gamma ];

    }

}

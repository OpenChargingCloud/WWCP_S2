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

using Microsoft.Extensions.Logging;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Node;
using cloud.charging.open.protocols.S2.Tests.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Node
{

    /// <summary>
    /// A LAN CEM node and a LAN RM node on one in-memory service discovery, addressed by
    /// ".local" host names and reached over plain HTTP/WebSocket on loopback. The CEM is the
    /// communication server, the RM the communication client. Helpers pair the two and wait for
    /// the session to form.
    /// </summary>
    internal sealed class S2NodeFixture : IAsyncDisposable
    {

        #region Data

        public static readonly CertificateFingerprint  ServerFingerprint
            = CertificateFingerprint.Parse("A1:B2:C3:D4:E5:F6:07:18:29:3A:4B:5C:6D:7E:8F:90:A1:B2:C3:D4:E5:F6:07:18:29:3A:4B:5C:6D:7E:8F:90");

        public static readonly CertificateFingerprint  CAFingerprint
            = CertificateFingerprint.Parse("CA:FE:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD");

        #endregion

        #region Properties

        public InMemoryServiceDiscovery  Discovery       { get; }
        public CEMNode                   CEM             { get; }
        public RMNode                    RM              { get; }
        public IPPort                    CEMHTTPPort     { get; }
        public IPPort                    CEMWSPort       { get; }
        public IPPort                    RMHTTPPort      { get; }

        #endregion

        #region Constructor(s)

        private S2NodeFixture(InMemoryServiceDiscovery  Discovery,
                              CEMNode                   CEM,
                              RMNode                    RM,
                              IPPort                    CEMHTTPPort,
                              IPPort                    CEMWSPort,
                              IPPort                    RMHTTPPort)
        {
            this.Discovery    = Discovery;
            this.CEM          = CEM;
            this.RM           = RM;
            this.CEMHTTPPort  = CEMHTTPPort;
            this.CEMWSPort    = CEMWSPort;
            this.RMHTTPPort   = RMHTTPPort;
        }

        #endregion


        #region (static) CreateAsync(ConfigureCEM = null, ConfigureRM = null, Start = true)

        public static async Task<S2NodeFixture> CreateAsync(Action<CEMNode>?             ConfigureCEM   = null,
                                                            Action<RMNode>?              ConfigureRM    = null,
                                                            ResourceManagerDetails?      Details        = null,
                                                            Boolean                      Start          = true,
                                                            ILoggerFactory?              LoggerFactory  = null)
        {

            var discovery     = new InMemoryServiceDiscovery();
            await discovery.StartAsync();

            var cemHTTPPort   = PairingServerFixture.FreePort();
            var cemWSPort     = PairingServerFixture.FreePort();
            var rmHTTPPort    = PairingServerFixture.FreePort();

            // The CEM: a LAN communication server addressed as "cem.local".
            var cemNode       = new CEMNode(
                                    new HostedNode(new NodeDescription(Node_Id.NewRandom, "GraphDefined", "EMS", "Home Energy Manager", EnergyManagementRole.CEM)),
                                    new S2NodeOptions {
                                        Description                                = new EndpointDescription("Home CEM"),
                                        Deployment                                 = Deployment.LAN,
                                        PairingUrl                                 = S2BaseURL.Parse($"http://cem.local:{cemHTTPPort}/pairing/",    AllowHTTP: true),
                                        SessionInitiationUrl                       = S2BaseURL.Parse($"http://cem.local:{cemHTTPPort}/connection/", AllowHTTP: true),
                                        WebSocketUrl                               = URL.Parse($"ws://cem.local:{cemWSPort}/"),
                                        HTTPPort                                   = cemHTTPPort,
                                        WebSocketPort                              = cemWSPort,
                                        BindAddress                                = IPv4Address.Localhost,
                                        ServerCertificateFingerprint               = () => ServerFingerprint,
                                        CACertificateFingerprint                   = () => CAFingerprint,
                                        AssumedRemoteServerCertificateFingerprint  = ServerFingerprint,
                                        ParserOptions                              = new S2ParserOptions { AllowInsecureURLs = true },
                                        PairingServer                              = new PairingServerOptions {
                                                                                         RequestPairingDelay  = TimeSpan.FromMilliseconds(10),
                                                                                         ParserOptions        = new S2ParserOptions { AllowInsecureURLs = true }
                                                                                     },
                                        PairingClient                              = new PairingClientOptions {
                                                                                         ParserOptions                 = new S2ParserOptions { AllowInsecureURLs = true },
                                                                                         ServiceUnavailableRetryDelay  = TimeSpan.FromMilliseconds(100)
                                                                                     },
                                        SessionInitiationServer                    = new SessionInitiationServerOptions {
                                                                                         ParserOptions = new S2ParserOptions { AllowInsecureURLs = true }
                                                                                     },
                                        SessionInitiationClient                    = new SessionInitiationClientOptions {
                                                                                         ParserOptions                 = new S2ParserOptions { AllowInsecureURLs = true },
                                                                                         ServiceUnavailableRetryDelay  = TimeSpan.FromMilliseconds(100)
                                                                                     },
                                        Advertiser                                 = new EndpointAdvertiserOptions {
                                                                                         CheckInterval  = TimeSpan.FromMilliseconds(50),
                                                                                         Addresses      = [ IPv4Address.Localhost ]
                                                                                     }
                                    },
                                    ServiceDiscovery:  discovery,
                                    LoggerFactory:     LoggerFactory
                                );

            // The RM: a LAN communication client addressed as "rm.local".
            var rmNode        = new RMNode(
                                    new HostedNode(new NodeDescription(Node_Id.NewRandom, "ACME", "EV charger", "WallBox-b100", EnergyManagementRole.RM)),
                                    new S2NodeOptions {
                                        Description                                = new EndpointDescription("EV charger"),
                                        Deployment                                 = Deployment.LAN,
                                        PairingUrl                                 = S2BaseURL.Parse($"http://rm.local:{rmHTTPPort}/pairing/", AllowHTTP: true),
                                        HTTPPort                                   = rmHTTPPort,
                                        BindAddress                                = IPv4Address.Localhost,
                                        ServerCertificateFingerprint               = () => ServerFingerprint,
                                        CACertificateFingerprint                   = () => CAFingerprint,
                                        AssumedRemoteServerCertificateFingerprint  = ServerFingerprint,
                                        ParserOptions                              = new S2ParserOptions { AllowInsecureURLs = true },
                                        PairingServer                              = new PairingServerOptions {
                                                                                         RequestPairingDelay  = TimeSpan.FromMilliseconds(10),
                                                                                         ParserOptions        = new S2ParserOptions { AllowInsecureURLs = true }
                                                                                     },
                                        PairingClient                              = new PairingClientOptions {
                                                                                         ParserOptions                 = new S2ParserOptions { AllowInsecureURLs = true },
                                                                                         ServiceUnavailableRetryDelay  = TimeSpan.FromMilliseconds(100)
                                                                                     },
                                        SessionInitiationClient                    = new SessionInitiationClientOptions {
                                                                                         ParserOptions                 = new S2ParserOptions { AllowInsecureURLs = true },
                                                                                         ServiceUnavailableRetryDelay  = TimeSpan.FromMilliseconds(100)
                                                                                     },
                                        Advertiser                                 = new EndpointAdvertiserOptions {
                                                                                         CheckInterval  = TimeSpan.FromMilliseconds(50),
                                                                                         Addresses      = [ IPv4Address.Localhost ]
                                                                                     }
                                    },
                                    ResourceManagerDetails:  Details,
                                    ServiceDiscovery:        discovery,
                                    LoggerFactory:           LoggerFactory
                                );

            // The in-memory DNS resolves the two ".local" host names to loopback.
            discovery.AddHost(DomainName.Parse("cem.local"), IPv4Address.Localhost);
            discovery.AddHost(DomainName.Parse("rm.local"),  IPv4Address.Localhost);

            ConfigureCEM?.Invoke(cemNode);
            ConfigureRM?. Invoke(rmNode);

            if (Start)
            {
                await cemNode.StartAsync();
                await rmNode. StartAsync();
            }

            return new S2NodeFixture(discovery, cemNode, rmNode, cemHTTPPort, cemWSPort, rmHTTPPort);

        }

        #endregion

        #region MakeCEMPairable()

        /// <summary>
        /// Put the CEM into pairing mode: issue a dynamic pairing token (which also makes the CEM
        /// advertise itself as ready for pairing). Returns the token to enter on the RM.
        /// </summary>
        public PairingToken MakeCEMPairable()
            => CEM.Node.IssueDynamicPairingToken().PairingToken;

        #endregion

        #region PairRMWithCEMAsync(Token = null)

        /// <summary>
        /// The RM pairs with the CEM using the given token (or a freshly issued one).
        /// </summary>
        public async Task PairRMWithCEMAsync(PairingToken? Token = null)
        {

            var token   = Token ?? MakeCEMPairable();

            var result  = await RM.PairAsync(CEM.Options.PairingUrl, token, Deployment.LAN);

            if (!result.IsSuccess)
                throw new InvalidOperationException($"Pairing failed: {result}");

        }

        #endregion

        #region WaitForSessionsAsync(Timeout = null)

        /// <summary>
        /// Wait until both nodes report one running session.
        /// </summary>
        public async Task WaitForSessionsAsync(TimeSpan? Timeout = null)
        {

            var deadline = DateTimeOffset.UtcNow + (Timeout ?? TimeSpan.FromSeconds(10));

            while (DateTimeOffset.UtcNow < deadline)
            {
                if (CEM.Sessions.Count >= 1 && RM.Sessions.Count >= 1)
                    return;
                await Task.Delay(20);
            }

            throw new TimeoutException($"No session formed in time (CEM: {CEM.Sessions.Count}, RM: {RM.Sessions.Count}).");

        }

        #endregion

        #region WaitUntil(Probe, Timeout = null)

        public static async Task WaitUntil(Func<Boolean> Probe, TimeSpan? Timeout = null)
        {

            var deadline = DateTimeOffset.UtcNow + (Timeout ?? TimeSpan.FromSeconds(10));

            while (DateTimeOffset.UtcNow < deadline)
            {
                if (Probe())
                    return;
                await Task.Delay(20);
            }

            throw new TimeoutException("The condition was not met in time!");

        }

        #endregion


        #region DisposeAsync()

        public async ValueTask DisposeAsync()
        {
            await RM.       DisposeAsync();
            await CEM.      DisposeAsync();
            await Discovery.DisposeAsync();
        }

        #endregion

    }

}

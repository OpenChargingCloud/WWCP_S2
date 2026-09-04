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
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect.Discovery
{

    /// <summary>
    /// End-to-end smoke tests of Phase 9: the in-memory service discovery, DNS-SD discovery over
    /// the in-memory Multicast DNS network followed by a pairing through the discovered
    /// "hostname.local" URL, and the WAN registry client against the reference registry API.
    /// </summary>
    [TestFixture]
    public sealed class DiscoverySmokeTests
    {

        #region Helpers

        public static MulticastDNSResponderOptions FastResponderOptions()
            => new () {
                   ProbeInterval           = TimeSpan.FromMilliseconds(10),
                   MaxInitialProbeDelay    = TimeSpan.Zero,
                   AnnouncementInterval    = TimeSpan.FromMilliseconds(10),
                   MinSharedResponseDelay  = TimeSpan.Zero,
                   MaxSharedResponseDelay  = TimeSpan.FromMilliseconds(1)
               };

        public static MulticastDNSClientOptions FastClientOptions()
            => new () {
                   QueryTimeout               = TimeSpan.FromMilliseconds(500),
                   ResponseGracePeriod        = TimeSpan.FromMilliseconds(20),
                   RetransmissionInterval     = TimeSpan.FromMilliseconds(100),
                   CacheSweepInterval         = TimeSpan.FromMilliseconds(50),
                   GoodbyeDelay               = TimeSpan.Zero,
                   BrowseMaintenanceInterval  = TimeSpan.FromMilliseconds(50),
                   InitialBrowseInterval      = TimeSpan.FromMilliseconds(50)
               };

        public static DNSSDServiceDiscoveryOptions FastDiscoveryOptions(IMulticastDNSTransport Transport)
            => new () {
                   Transport         = Transport,
                   OwnsTransport     = false,
                   ResponderOptions  = FastResponderOptions(),
                   ClientOptions     = FastClientOptions(),
                   ParserOptions     = new S2ParserOptions { AllowInsecureURLs = true }
               };

        public static async Task WaitUntil(Func<Boolean> Probe, TimeSpan? Timeout = null)
        {

            var deadline = DateTimeOffset.UtcNow + (Timeout ?? TimeSpan.FromSeconds(5));

            while (DateTimeOffset.UtcNow < deadline)
            {
                if (Probe())
                    return;
                await Task.Delay(10);
            }

            throw new TimeoutException("The condition was not met in time!");

        }

        public static EndpointRecord Record(String Name, String Region, EndpointStatus Status, Boolean CEM, Boolean RM)
            => new (EndpointRecord_Id.NewRandom,
                    Name,
                    $"The {Name} endpoint",
                    URL.Parse("https://example.org/icon32.png"),
                    URL.Parse("https://example.org/icon128.png"),
                    URL.Parse("https://example.org/icon512.png"),
                    S2BaseURL.Parse($"https://{Name.ToLowerInvariant()}.example.org/pairing/"),
                    [ CountryCode.Parse(Region) ],
                    Status,
                    CEM,
                    RM);

        #endregion


        #region InMemoryDiscovery_AdvertiseBrowseResolveWithdraw()

        [Test]
        public async Task InMemoryDiscovery_AdvertiseBrowseResolveWithdraw()
        {

            await using var discovery = new InMemoryServiceDiscovery();
            await discovery.StartAsync();

            await using var rmBrowser   = await discovery.BrowseAsync(EnergyManagementRole.RM);
            await using var cemBrowser  = await discovery.BrowseAsync(EnergyManagementRole.CEM);

            var appeared     = new List<S2DiscoveredEndpoint>();
            var disappeared  = new List<S2DiscoveredEndpoint>();

            rmBrowser.OnEndpointAppeared    += (ts, b, e, ct) => { appeared.   Add(e); return Task.CompletedTask; };
            rmBrowser.OnEndpointDisappeared += (ts, b, e, ct) => { disappeared.Add(e); return Task.CompletedTask; };

            var advertisement = new S2ServiceAdvertisement(
                                    DomainName.Parse("heatpump.local."),
                                    IPPort.Parse(8443),
                                    new S2DNSSDTXTRecord(S2BaseURL.Parse("https://heatpump.local:8443/pairing/"),
                                                         EndpointName: "Heat pump"),
                                    [ EnergyManagementRole.RM ]
                                );

            var handle = await discovery.AdvertiseAsync(advertisement);

            Assert.That(handle.IsPublished, Is.True);
            Assert.That(rmBrowser.Endpoints, Has.Count.EqualTo(1));
            Assert.That(cemBrowser.Endpoints, Is.Empty);

            var endpoint = rmBrowser.Endpoints[0];

            Assert.Multiple(() => {
                Assert.That(endpoint.InstanceName.FullName,       Is.EqualTo("heatpump._s2connect._tcp.local."));
                Assert.That(endpoint.HostName.FullName,           Is.EqualTo("heatpump.local."));
                Assert.That(endpoint.Port.ToUInt16(),             Is.EqualTo((UInt16) 8443));
                Assert.That(endpoint.PairingUrl?.Value,           Is.EqualTo("https://heatpump.local:8443/pairing/"));
                Assert.That(endpoint.Name,                        Is.EqualTo("Heat pump"));
                Assert.That(endpoint.HostsRM,                     Is.True);
                Assert.That(endpoint.HostsCEM,                    Is.False);
                Assert.That(endpoint.Addresses,                   Is.EqualTo(new IIPAddress[] { IPv4Address.Localhost }));
                Assert.That(appeared,                             Has.Count.EqualTo(1));
            });

            var addresses = (await discovery.DNSClient.Query_IPv4Addresses(DomainName.Parse("heatpump.local."))).ToArray();
            Assert.That(addresses, Is.EqualTo(new[] { IPv4Address.Localhost }));

            await handle.WithdrawAsync();

            Assert.Multiple(() => {
                Assert.That(handle.IsPublished,      Is.False);
                Assert.That(rmBrowser.Endpoints,     Is.Empty);
                Assert.That(disappeared,             Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region DNSSD_DiscoverAndPairViaLocalName()

        [Test]
        public async Task DNSSD_DiscoverAndPairViaLocalName()
        {

            var network = new InMemoryMulticastDNSNetwork();

            await using var serverTransport  = network.CreateTransport(IPv4Address.Parse("10.0.0.7"));
            await using var clientTransport  = network.CreateTransport(IPv4Address.Parse("10.0.0.8"));

            await using var serverDiscovery  = new DNSSDServiceDiscovery(FastDiscoveryOptions(serverTransport));
            await using var clientDiscovery  = new DNSSDServiceDiscovery(FastDiscoveryOptions(clientTransport));

            await serverDiscovery.StartAsync();
            await clientDiscovery.StartAsync();

            // The pairing server listens on 127.0.0.1 but is addressed as "s2test.local".
            await using var server = await PairingServerFixture.CreateAsync(Host: "s2test.local");

            var rm = server.AddNode(EnergyManagementRole.RM);

            await using var advertiser = new EndpointAdvertiser(
                                             serverDiscovery,
                                             server.Endpoint,
                                             new EndpointAdvertiserOptions {
                                                 CheckInterval  = TimeSpan.FromMilliseconds(50),
                                                 Addresses      = [ IPv4Address.Localhost ]
                                             }
                                         );

            await advertiser.StartAsync();

            // No pairing token yet: nothing is advertised.
            Assert.That(advertiser.IsAdvertised, Is.False);

            await using var browser = await clientDiscovery.BrowseAsync(EnergyManagementRole.RM);

            var code = rm.IssueDynamicPairingToken();

            await WaitUntil(() => advertiser.IsAdvertised);

            var endpoint = await browser.WaitForEndpointAsync(Timeout: TimeSpan.FromSeconds(5));

            Assert.That(endpoint, Is.Not.Null);
            Assert.Multiple(() => {
                Assert.That(endpoint!.HostName.FullName,      Is.EqualTo("s2test.local."));
                Assert.That(endpoint.Port,                    Is.EqualTo(server.Port));
                Assert.That(endpoint.PairingUrl,              Is.EqualTo(server.PairingUrl));
                Assert.That(endpoint.HostsRM,                 Is.True);
                Assert.That(endpoint.Addresses,               Is.EqualTo(new IIPAddress[] { IPv4Address.Localhost }));
            });

            // ".local" names resolve through the hybrid DNS client of the discovery.
            var addresses = (await clientDiscovery.DNSClient.Query_IPv4Addresses(DomainName.Parse("s2test.local."))).ToArray();
            Assert.That(addresses, Is.EqualTo(new[] { IPv4Address.Localhost }));

            // ... also without a cache (a fresh query) and the way Hermod's TCP client asks.
            clientDiscovery.Client.ClearCache();

            var uncached = (await clientDiscovery.DNSClient.Query_IPv4Addresses(DomainName.Parse("s2test.local"), RecursionDesired: true, ForceUpdate: true)).ToArray();
            Assert.That(uncached, Is.EqualTo(new[] { IPv4Address.Localhost }), "uncached A query");

            var ipv6 = (await clientDiscovery.DNSClient.Query_IPv6Addresses(DomainName.Parse("s2test.local"), RecursionDesired: true, ForceUpdate: true)).ToArray();
            Assert.That(ipv6, Is.Empty, "AAAA query");

            var parallelA  = clientDiscovery.DNSClient.Query_IPv4Addresses(DomainName.Parse("s2test.local"), RecursionDesired: true, ForceUpdate: false);
            var parallelA6 = clientDiscovery.DNSClient.Query_IPv6Addresses(DomainName.Parse("s2test.local"), RecursionDesired: true, ForceUpdate: false);
            await Task.WhenAll(parallelA, parallelA6);
            Assert.That(parallelA.Result.ToArray(), Is.EqualTo(new[] { IPv4Address.Localhost }), "parallel A query");

            // Pair through the discovered URL, resolving "s2test.local" via Multicast DNS.
            var clientEndpoint = new LocalEndpoint(
                                     new EndpointDescription("Test LAN client endpoint"),
                                     Deployment.LAN,
                                     S2BaseURL.Parse("http://127.0.0.1:1/pairing/",    AllowHTTP: true),
                                     S2BaseURL.Parse("http://127.0.0.1:1/connection/", AllowHTTP: true),
                                     CACertificateFingerprint: () => PairingClientFixture.CAFingerprint
                                 );

            var store      = new InMemoryS2Store();
            var recording  = new RecordingDNSClient(clientDiscovery.DNSClient);

            await using var pairingClient = new PairingClient(
                                                endpoint!.PairingUrl!.Value,
                                                clientEndpoint,
                                                store,
                                                Deployment.LAN,
                                                PairingClientFixture.DefaultOptions(),
                                                PairingServerFixture.Fingerprint,
                                                DNSClient: recording
                                            );

            Assert.That(pairingClient.DNSClient,                  Is.SameAs(recording),        "DNS client pass-through");
            Assert.That(pairingClient.RemoteURL.Host.ToString(),  Is.EqualTo("s2test.local"),  "remote host");

            var cem     = clientEndpoint.AddNode(new NodeDescription(Node_Id.NewRandom, "GraphDefined", "EMS", "TestCEM 1", EnergyManagementRole.CEM));
            var result  = await pairingClient.PairAsync(cem, code.PairingToken);

            Assert.That(result.IsSuccess, Is.True, result.ToString() + Environment.NewLine + String.Join(Environment.NewLine, recording.Log));
            Assert.That(await store.GetPairingAsync(cem.Id, rm.Id), Is.Not.Null);

            // The dynamic token was consumed: the endpoint is no longer ready for pairing and disappears.
            await WaitUntil(() => !advertiser.IsAdvertised);
            await WaitUntil(() => browser.Endpoints.Count == 0);

        }

        #endregion

        #region DNSSD_OverRealSockets_DiscoverAndPair()

        /// <summary>
        /// The same flow over real UDP multicast sockets on a private port (two transports of one
        /// process; multicast responses because a unicast reaches only one of two sockets bound
        /// to the same port). Skipped where no multicast capable interface exists.
        /// </summary>
        [Test]
        [Category(TestCategories.Multicast)]
        public async Task DNSSD_OverRealSockets_DiscoverAndPair()
        {

            var port = IPPort.Parse((UInt16) (5400 + Random.Shared.Next(0, 100)));

            static DNSSDServiceDiscoveryOptions RealOptions(IPPort Port)
                => new () {
                       TransportOptions  = new UDPMulticastDNSTransportOptions { Port = Port, EnableIPv6 = false },
                       ResponderOptions  = new MulticastDNSResponderOptions {
                                               ProbeInterval               = TimeSpan.FromMilliseconds(50),
                                               MaxInitialProbeDelay        = TimeSpan.Zero,
                                               AnnouncementInterval        = TimeSpan.FromMilliseconds(50),
                                               MinRecordMulticastInterval  = TimeSpan.Zero
                                           },
                       ClientOptions     = new MulticastDNSClientOptions {
                                               QueryTimeout               = TimeSpan.FromSeconds(2),
                                               ResponseGracePeriod        = TimeSpan.FromMilliseconds(100),
                                               RetransmissionInterval     = TimeSpan.FromMilliseconds(300),
                                               RequestUnicastResponses    = false,
                                               CacheSweepInterval         = TimeSpan.FromMilliseconds(100),
                                               GoodbyeDelay               = TimeSpan.Zero,
                                               BrowseMaintenanceInterval  = TimeSpan.FromMilliseconds(100),
                                               InitialBrowseInterval      = TimeSpan.FromMilliseconds(200)
                                           },
                       ParserOptions     = new S2ParserOptions { AllowInsecureURLs = true }
                   };

            await using var serverDiscovery  = new DNSSDServiceDiscovery(RealOptions(port));
            await using var clientDiscovery  = new DNSSDServiceDiscovery(RealOptions(port));

            try
            {
                await serverDiscovery.StartAsync();
                await clientDiscovery.StartAsync();
            }
            catch (InvalidOperationException e)
            {
                Assert.Ignore($"No multicast capable network interface: {e.Message}");
            }

            await using var server = await PairingServerFixture.CreateAsync(Host: "s2realtest.local");

            var rm    = server.AddNode(EnergyManagementRole.RM);
            var code  = rm.IssueDynamicPairingToken();

            await using var advertiser = new EndpointAdvertiser(
                                             serverDiscovery,
                                             server.Endpoint,
                                             new EndpointAdvertiserOptions {
                                                 CheckInterval  = TimeSpan.FromMilliseconds(100),
                                                 Addresses      = [ IPv4Address.Localhost ]
                                             }
                                         );

            await advertiser.StartAsync();
            Assert.That(advertiser.IsAdvertised, Is.True, "advertised after start (a token exists)");

            await using var browser = await clientDiscovery.BrowseAsync(EnergyManagementRole.RM);

            var endpoint = await browser.WaitForEndpointAsync(Timeout: TimeSpan.FromSeconds(10));

            Assert.That(endpoint, Is.Not.Null, "the endpoint was not discovered over the real sockets");
            Assert.That(endpoint!.PairingUrl, Is.EqualTo(server.PairingUrl));

            var clientEndpoint = new LocalEndpoint(
                                     new EndpointDescription("Test LAN client endpoint"),
                                     Deployment.LAN,
                                     S2BaseURL.Parse("http://127.0.0.1:1/pairing/",    AllowHTTP: true),
                                     S2BaseURL.Parse("http://127.0.0.1:1/connection/", AllowHTTP: true),
                                     CACertificateFingerprint: () => PairingClientFixture.CAFingerprint
                                 );

            var store = new InMemoryS2Store();

            await using var pairingClient = new PairingClient(
                                                endpoint.PairingUrl!.Value,
                                                clientEndpoint,
                                                store,
                                                Deployment.LAN,
                                                PairingClientFixture.DefaultOptions(),
                                                PairingServerFixture.Fingerprint,
                                                DNSClient: clientDiscovery.DNSClient
                                            );

            var cem     = clientEndpoint.AddNode(new NodeDescription(Node_Id.NewRandom, "GraphDefined", "EMS", "TestCEM 1", EnergyManagementRole.CEM));
            var result  = await pairingClient.PairAsync(cem, code.PairingToken);

            Assert.That(result.IsSuccess, Is.True, result.ToString());

        }

        #endregion

        #region (class) RecordingDNSClient

        /// <summary>
        /// Records every query of the wrapped DNS client (for diagnostics).
        /// </summary>
        private sealed class RecordingDNSClient(IDNSClient Inner) : IDNSClient
        {

            private readonly IDNSClient inner = Inner;

            public List<String> Log { get; } = [];

            public async Task<DNSInfo> Query(DomainName DomainName, IEnumerable<DNSResourceRecordTypes> ResourceRecordTypes, TimeSpan? Timeout = null, Boolean? RecursionDesired = true, Boolean? ForceUpdate = false, CancellationToken CancellationToken = default)
            {
                var types = ResourceRecordTypes.ToArray();
                try
                {
                    var info = await inner.Query(DomainName, types, Timeout, RecursionDesired, ForceUpdate, CancellationToken);
                    lock (Log) Log.Add($"DomainName {DomainName.FullName} {String.Join("/", types)} force={ForceUpdate} => {info.ResponseCode} timeout={info.IsTimeout} answers={String.Join(", ", info.Answers)}");
                    return info;
                }
                catch (Exception e)
                {
                    lock (Log) Log.Add($"DomainName {DomainName.FullName} {String.Join("/", types)} => EXCEPTION {e.GetType().Name}: {e.Message}");
                    throw;
                }
            }

            public async Task<DNSInfo> Query(DNSServiceName DNSServiceName, IEnumerable<DNSResourceRecordTypes> ResourceRecordTypes, TimeSpan? Timeout = null, Boolean? RecursionDesired = true, Boolean? ForceUpdate = false, CancellationToken CancellationToken = default)
            {
                var types = ResourceRecordTypes.ToArray();
                try
                {
                    var info = await inner.Query(DNSServiceName, types, Timeout, RecursionDesired, ForceUpdate, CancellationToken);
                    lock (Log) Log.Add($"DNSServiceName {DNSServiceName.FullName} {String.Join("/", types)} force={ForceUpdate} => {info.ResponseCode} timeout={info.IsTimeout} answers={String.Join(", ", info.Answers)}");
                    return info;
                }
                catch (Exception e)
                {
                    lock (Log) Log.Add($"DNSServiceName {DNSServiceName.FullName} {String.Join("/", types)} => EXCEPTION {e.GetType().Name}: {e.Message}");
                    throw;
                }
            }

            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public override String ToString() => "recording DNS client";

        }

        #endregion

        #region WANRegistry_QueryAndGet()

        [Test]
        public async Task WANRegistry_QueryAndGet()
        {

            var port        = PairingServerFixture.FreePort();
            var httpServer  = new HTTPServer(IPv4Address.Parse("127.0.0.1"), port, "S2 test registry", AutoStart: false);

            var nlCEM       = Record("Alpha", "NL", EndpointStatus.Public,  true,  false);
            var deRM        = Record("Beta",  "DE", EndpointStatus.Public,  false, true);
            var testing     = Record("Gamma", "NL", EndpointStatus.Testing, true,  true);

            var registry    = new InMemoryWANRegistry([ nlCEM, deRM, testing ]);
            var api         = new WANRegistryAPI(httpServer, registry, HTTPPath.Parse("/registry/"));

            await httpServer.Start();

            try
            {

                await using var client = new WANRegistryClient(
                                             S2BaseURL.Parse($"http://127.0.0.1:{port}/registry/", AllowHTTP: true),
                                             new WANRegistryClientOptions {
                                                 ParserOptions = new S2ParserOptions { AllowInsecureURLs = true }
                                             }
                                         );

                var versions = await client.GetVersionsAsync();
                Assert.That(versions.IsSuccess, Is.True, versions.Description);
                Assert.That(versions.Value,     Is.EqualTo(new[] { "v1" }));

                var all = await client.QueryEndpointsAsync();
                Assert.That(all.IsSuccess, Is.True, all.Description);
                Assert.That(all.Value!.Select(record => record.Name), Is.EquivalentTo(new[] { "Alpha", "Beta" }));

                var nl = await client.QueryEndpointsAsync(new WANRegistryQuery(Regions: [ CountryCode.Parse("NL") ], CEM: true));
                Assert.That(nl.IsSuccess, Is.True, nl.Description);
                Assert.That(nl.Value!.Select(record => record.Name), Is.EqualTo(new[] { "Alpha" }));

                var testingOnly = await client.QueryEndpointsAsync(new WANRegistryQuery(Status: EndpointStatus.Testing));
                Assert.That(testingOnly.IsSuccess, Is.True, testingOnly.Description);
                Assert.That(testingOnly.Value!.Select(record => record.Name), Is.EqualTo(new[] { "Gamma" }));

                var paged = await client.QueryEndpointsAsync(new WANRegistryQuery(Limit: 1, Offset: 1));
                Assert.That(paged.IsSuccess, Is.True, paged.Description);
                Assert.That(paged.Value!.Select(record => record.Name), Is.EqualTo(new[] { "Beta" }));

                var byId = await client.GetEndpointAsync(deRM.Id);
                Assert.That(byId.IsSuccess, Is.True, byId.Description);
                Assert.That(byId.Value,     Is.EqualTo(deRM));

                var missing = await client.GetEndpointAsync(EndpointRecord_Id.NewRandom);
                Assert.Multiple(() => {
                    Assert.That(missing.IsSuccess,        Is.False);
                    Assert.That(missing.StatusCode.Code,  Is.EqualTo(404));
                });

                using var http      = new HttpClient();
                using var response  = await http.GetAsync(new Uri($"http://127.0.0.1:{port}/registry/v1/endpoint?limit=0"));
                Assert.That((Int32) response.StatusCode, Is.EqualTo(400));

            }
            finally
            {
                await httpServer.Stop();
            }

        }

        #endregion

    }

}

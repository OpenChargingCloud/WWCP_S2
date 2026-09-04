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

using System.Collections.Concurrent;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect.Discovery
{

    /// <summary>
    /// The DNS-SD specifics of the service discovery on an in-memory Multicast DNS network
    /// (S2 Connect 1.0.0, "DNS-SD based discovery"): the validation of TXT records, the role
    /// classification through the "_cem" and "_rm" subtypes, the precedence of the URL of the
    /// TXT record over the SRV record, the publications of the responder, name conflicts and
    /// goodbye packets.
    /// </summary>
    [TestFixture]
    public sealed class DNSSDBrowserTests
    {

        #region Data

        private static readonly TimeSpan  HostTTL      = TimeSpan.FromSeconds(120);
        private static readonly TimeSpan  SharedTTL    = TimeSpan.FromMinutes(75);
        private static readonly TimeSpan  WaitTimeout  = TimeSpan.FromSeconds(5);

        #endregion

        #region (class) Testbed

        /// <summary>
        /// Two DNS-SD discoveries on one in-memory Multicast DNS network: the server (10.0.0.7)
        /// advertises, the client (10.0.0.8) browses with the given parser options.
        /// </summary>
        private sealed class Testbed : IAsyncDisposable
        {

            public InMemoryMulticastDNSTransport  ServerTransport    { get; }
            public InMemoryMulticastDNSTransport  ClientTransport    { get; }
            public DNSSDServiceDiscovery          Server             { get; }
            public DNSSDServiceDiscovery          Client             { get; }

            private Testbed(S2ParserOptions? ClientParserOptions)
            {

                var network = new InMemoryMulticastDNSNetwork();

                ServerTransport  = network.CreateTransport(IPv4Address.Parse("10.0.0.7"));
                ClientTransport  = network.CreateTransport(IPv4Address.Parse("10.0.0.8"));
                Server           = new DNSSDServiceDiscovery(DiscoverySmokeTests.FastDiscoveryOptions(ServerTransport));
                Client           = new DNSSDServiceDiscovery(DiscoveryOptions(ClientTransport, ClientParserOptions));

            }

            public static async Task<Testbed> StartAsync(S2ParserOptions? ClientParserOptions = null)
            {

                var testbed = new Testbed(ClientParserOptions);

                await testbed.Server.StartAsync();
                await testbed.Client.StartAsync();

                return testbed;

            }

            public async ValueTask DisposeAsync()
            {
                await Client.         DisposeAsync();
                await Server.         DisposeAsync();
                await ClientTransport.DisposeAsync();
                await ServerTransport.DisposeAsync();
            }

        }

        #endregion

        #region Helpers

        private static DNSSDServiceDiscoveryOptions DiscoveryOptions(IMulticastDNSTransport  Transport,
                                                                     S2ParserOptions?        ParserOptions)

            => new () {
                   Transport         = Transport,
                   OwnsTransport     = false,
                   ResponderOptions  = DiscoverySmokeTests.FastResponderOptions(),
                   ClientOptions     = DiscoverySmokeTests.FastClientOptions(),
                   ParserOptions     = ParserOptions ?? new S2ParserOptions { AllowInsecureURLs = true }
               };

        private static S2ServiceAdvertisement Advertisement(String                              Host,
                                                            IEnumerable<EnergyManagementRole>?  Roles        = null,
                                                            String?                             PairingUrl   = null,
                                                            UInt16                              Port         = 8443,
                                                            String                              Address      = "10.0.0.7",
                                                            String?                             Name         = null)

            => new (DomainName.Parse(Host),
                    IPPort.Parse(Port),
                    new S2DNSSDTXTRecord(S2BaseURL.Parse(PairingUrl ?? $"https://{Host.TrimEnd('.')}:{Port}/pairing/", AllowHTTP: true),
                                         EndpointName: Name),
                    Roles ?? [ EnergyManagementRole.RM ],
                    [ IPv4Address.Parse(Address) ]);

        /// <summary>
        /// The DNS-SD records of "&lt;Label&gt;._s2connect._tcp.local." built by hand (no subtypes),
        /// so that a TXT record the library itself would never produce can be published.
        /// </summary>
        private static IDNSResourceRecord[] RawRecords(String               Label,
                                                       IEnumerable<String>  TXTStrings)
        {

            var instanceName  = DNSServiceName.Parse($"{Label}.{S2ServiceAdvertisement.ServiceTypeName.FullName}");
            var hostName      = DomainName.    Parse($"{Label}.local.");

            return [
                new PTR(S2ServiceAdvertisement.ServiceTypeName, DNSQueryClasses.IN, SharedTTL, instanceName),
                new SRV(instanceName,                           DNSQueryClasses.IN, HostTTL,   0, 0, IPPort.Parse(8443), hostName),
                new TXT(instanceName,                           DNSQueryClasses.IN, SharedTTL, TXTStrings),
                new A  (hostName,                               DNSQueryClasses.IN, HostTTL,   IPv4Address.Parse("10.0.0.7"))
            ];

        }

        private static Task WaitUntil(Func<Boolean> Probe)
            => DiscoverySmokeTests.WaitUntil(Probe, WaitTimeout);

        private static async Task<Exception?> CatchAsync(Func<Task> Action)
        {
            try
            {
                await Action();
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        #endregion


        #region InvalidTXTRecord_RaisesOnInvalidEndpoint()

        [Test]
        public async Task InvalidTXTRecord_RaisesOnInvalidEndpoint()
        {

            await using var testbed   = await Testbed.StartAsync();
            await using var browser   = await testbed.Client.BrowseAsync();

            var invalid   = new ConcurrentQueue<(String Instance, String Error)>();
            var appeared  = new ConcurrentQueue<S2DiscoveredEndpoint>();

            browser.OnInvalidEndpoint  += (ts, sender, instanceName, error, ct) => { invalid.Enqueue((instanceName.FullName, error)); return Task.CompletedTask; };
            browser.OnEndpointAppeared += (ts, sender, endpoint, ct)            => { appeared.Enqueue(endpoint);                     return Task.CompletedTask; };

            // A service instance without the mandatory "txtver" entry.
            var publication = await testbed.Server.Responder.PublishAsync(
                                  RawRecords("bad", [ "pairingUrl=https://bad.local:8443/pairing/" ]),
                                  false
                              );

            await WaitUntil(() => !invalid.IsEmpty);

            Assert.Multiple(() => {
                Assert.That(publication.State,          Is.EqualTo(MulticastDNSPublicationState.Published));
                Assert.That(invalid.First().Instance,   Is.EqualTo("bad._s2connect._tcp.local."));
                Assert.That(invalid.First().Error,      Does.Contain("txtver"));
                Assert.That(appeared,                   Is.Empty);
                Assert.That(browser.Endpoints,          Is.Empty);
            });

        }

        #endregion

        #region TXTVersAlias_IsAccepted()

        [Test]
        public async Task TXTVersAlias_IsAccepted()
        {

            await using var testbed   = await Testbed.StartAsync();
            await using var browser   = await testbed.Client.BrowseAsync();

            var invalid = new ConcurrentQueue<String>();

            browser.OnInvalidEndpoint += (ts, sender, instanceName, error, ct) => { invalid.Enqueue(error); return Task.CompletedTask; };

            // The avahi example of the specification spells the version key "txtvers".
            await testbed.Server.Responder.PublishAsync(
                      RawRecords("alias", [ "txtvers=1", "e_name=Alias", "pairingUrl=https://alias.local:8443/pairing/" ]),
                      false
                  );

            var endpoint = await browser.WaitForEndpointAsync(Timeout: WaitTimeout);

            Assert.That(endpoint, Is.Not.Null);
            Assert.Multiple(() => {
                Assert.That(endpoint!.InstanceName.FullName,   Is.EqualTo("alias._s2connect._tcp.local."));
                Assert.That(endpoint.HostName.FullName,        Is.EqualTo("alias.local."));
                Assert.That(endpoint.TXT.TXTVersion,           Is.EqualTo("1"));
                Assert.That(endpoint.Name,                     Is.EqualTo("Alias"));
                Assert.That(endpoint.PairingUrl?.Value,        Is.EqualTo("https://alias.local:8443/pairing/"));
                Assert.That(endpoint.Addresses,                Is.EqualTo(new IIPAddress[] { IPv4Address.Parse("10.0.0.7") }));
                Assert.That(endpoint.HostsCEM,                 Is.Null,   "no subtype was published");
                Assert.That(endpoint.HostsRM,                  Is.Null,   "no subtype was published");
                Assert.That(invalid,                           Is.Empty);
            });

        }

        #endregion

        #region RoleClassification_CEMAndRM_BothTrue()

        [Test]
        public async Task RoleClassification_CEMAndRM_BothTrue()
        {

            await using var testbed   = await Testbed.StartAsync();
            await using var browser   = await testbed.Client.BrowseAsync();
            await using var handle    = await testbed.Server.AdvertiseAsync(Advertisement("both.local.", [ EnergyManagementRole.CEM, EnergyManagementRole.RM ]));

            // The service type browser reports the endpoint first, the two subtype browsers add the roles.
            await WaitUntil(() => browser.Endpoints.Any(e => e.HostsCEM == true && e.HostsRM == true));

            var endpoint = browser.Endpoints.Single();

            Assert.Multiple(() => {
                Assert.That(endpoint.HostName.FullName,                         Is.EqualTo("both.local."));
                Assert.That(endpoint.HostsCEM,                                  Is.True);
                Assert.That(endpoint.HostsRM,                                   Is.True);
                Assert.That(endpoint.HostsRole(EnergyManagementRole.CEM),       Is.True);
                Assert.That(endpoint.HostsRole(EnergyManagementRole.RM),        Is.True);
                Assert.That(handle.Advertisement.Roles,                         Is.EquivalentTo(new[] { EnergyManagementRole.CEM, EnergyManagementRole.RM }));
            });

        }

        #endregion

        #region RoleClassification_RMOnly_CEMUnknown()

        [Test]
        public async Task RoleClassification_RMOnly_CEMUnknown()
        {

            await using var testbed     = await Testbed.StartAsync();
            await using var browser     = await testbed.Client.BrowseAsync();
            await using var cemBrowser  = await testbed.Client.BrowseAsync(EnergyManagementRole.CEM);
            await using var handle      = await testbed.Server.AdvertiseAsync(Advertisement("rm.local.", [ EnergyManagementRole.RM ]));

            await WaitUntil(() => browser.Endpoints.Any(e => e.HostsRM == true));

            var endpoint = browser.Endpoints.Single();

            Assert.Multiple(() => {
                Assert.That(endpoint.HostsRM,                                   Is.True);
                Assert.That(endpoint.HostsCEM,                                  Is.Null,   "no '_cem' subtype: unknown, not false");
                Assert.That(endpoint.HostsRole(EnergyManagementRole.CEM),       Is.Null);
            });

            // A CEM browser never sees an RM-only endpoint.
            Assert.That(await cemBrowser.WaitForEndpointAsync(Timeout: TimeSpan.FromMilliseconds(300)), Is.Null);
            Assert.That(cemBrowser.Endpoints, Is.Empty);

        }

        #endregion

        #region PairingUrl_OfTheTXTRecord_WinsOverTheSRVPort()

        [Test]
        public async Task PairingUrl_OfTheTXTRecord_WinsOverTheSRVPort()
        {

            await using var testbed   = await Testbed.StartAsync();
            await using var browser   = await testbed.Client.BrowseAsync(EnergyManagementRole.RM);
            await using var handle    = await testbed.Server.AdvertiseAsync(Advertisement("port.local.", Port: 9999, PairingUrl: "https://port.local:8443/pairing/"));

            var endpoint = await browser.WaitForEndpointAsync(Timeout: WaitTimeout);

            Assert.That(endpoint, Is.Not.Null);
            Assert.Multiple(() => {
                Assert.That(endpoint!.Port.ToUInt16(),                          Is.EqualTo((UInt16) 9999),  "the SRV port");
                Assert.That(endpoint.PairingUrl?.Value,                         Is.EqualTo("https://port.local:8443/pairing/"));
                Assert.That(endpoint.PairingUrl?.URL.Port?.ToUInt16(),          Is.EqualTo((UInt16) 8443),  "the port of the TXT record URL");
                Assert.That(handle.Advertisement.Port.ToUInt16(),               Is.EqualTo((UInt16) 9999));
            });

        }

        #endregion

        #region HttpPairingUrl_IsRejectedWithTheDefaultParserOptions()

        [Test]
        public async Task HttpPairingUrl_IsRejectedWithTheDefaultParserOptions()
        {

            await using var testbed   = await Testbed.StartAsync(S2ParserOptions.Default);
            await using var browser   = await testbed.Client.BrowseAsync();

            var invalid = new ConcurrentQueue<(String Instance, String Error)>();

            browser.OnInvalidEndpoint += (ts, sender, instanceName, error, ct) => { invalid.Enqueue((instanceName.FullName, error)); return Task.CompletedTask; };

            await using var handle = await testbed.Server.AdvertiseAsync(Advertisement("insecure.local.", PairingUrl: "http://insecure.local:8080/pairing/"));

            await WaitUntil(() => !invalid.IsEmpty);

            Assert.Multiple(() => {
                Assert.That(testbed.Client.Options.ParserOptions.AllowInsecureURLs,  Is.False);
                Assert.That(invalid.First().Instance,                                Is.EqualTo("insecure._s2connect._tcp.local."));
                Assert.That(invalid.First().Error,                                   Does.Contain("pairingUrl").And.Contains("https"));
                Assert.That(browser.Endpoints,                                       Is.Empty);
                Assert.That(handle.IsPublished,                                      Is.True,   "the advertiser is not affected");
            });

        }

        #endregion

        #region HttpPairingUrl_IsAcceptedWithAllowInsecureURLs()

        [Test]
        public async Task HttpPairingUrl_IsAcceptedWithAllowInsecureURLs()
        {

            await using var testbed   = await Testbed.StartAsync(new S2ParserOptions { AllowInsecureURLs = true });
            await using var browser   = await testbed.Client.BrowseAsync();

            var invalid = new ConcurrentQueue<String>();

            browser.OnInvalidEndpoint += (ts, sender, instanceName, error, ct) => { invalid.Enqueue(error); return Task.CompletedTask; };

            await using var handle = await testbed.Server.AdvertiseAsync(Advertisement("insecure.local.", PairingUrl: "http://insecure.local:8080/pairing/"));

            var endpoint = await browser.WaitForEndpointAsync(Timeout: WaitTimeout);

            Assert.That(endpoint, Is.Not.Null);
            Assert.Multiple(() => {
                Assert.That(endpoint!.PairingUrl?.Value,   Is.EqualTo("http://insecure.local:8080/pairing/"));
                Assert.That(endpoint.PairingUrl?.IsHTTPS,  Is.False);
                Assert.That(invalid,                       Is.Empty);
            });

        }

        #endregion

        #region Advertisements_ListTheHandleWithItsPublication()

        [Test]
        public async Task Advertisements_ListTheHandleWithItsPublication()
        {

            await using var testbed   = await Testbed.StartAsync();
            await using var browser   = await testbed.Client.BrowseAsync(EnergyManagementRole.RM);

            // No explicit addresses: the addresses of the transport are announced.
            var advertisement = new S2ServiceAdvertisement(
                                    DomainName.Parse("pub.local."),
                                    IPPort.Parse(8443),
                                    new S2DNSSDTXTRecord(S2BaseURL.Parse("https://pub.local:8443/pairing/")),
                                    [ EnergyManagementRole.RM ]
                                );

            var handle  = await testbed.Server.AdvertiseAsync(advertisement);
            var dnssd   = (DNSSDAdvertiser) handle;

            Assert.Multiple(() => {
                Assert.That(testbed.Server.Advertisements,             Is.EquivalentTo(new[] { handle }));
                Assert.That(testbed.Server.Responder.Publications,     Has.Count.EqualTo(1));
                Assert.That(dnssd.Advertisement,                       Is.SameAs(advertisement));
                Assert.That(dnssd.Publication,                         Is.Not.Null);
                Assert.That(dnssd.Publication?.State,                  Is.EqualTo(MulticastDNSPublicationState.Published));
                Assert.That(dnssd.Publication?.IsActive,               Is.True);
                Assert.That(dnssd.Publication?.Records,                Has.Count.EqualTo(5),   "PTR (type), PTR (_rm), SRV, TXT, A");
                Assert.That(dnssd.Addresses,                           Is.EqualTo(testbed.Server.LocalAddresses));
                Assert.That(dnssd.Addresses,                           Is.EqualTo(new IIPAddress[] { IPv4Address.Parse("10.0.0.7") }));
                Assert.That(handle.IsPublished,                        Is.True);
                Assert.That(handle.HasConflict,                        Is.False);
            });

            var endpoint = await browser.WaitForEndpointAsync(Timeout: WaitTimeout);

            Assert.That(endpoint, Is.Not.Null);
            Assert.That(endpoint!.Addresses, Is.EqualTo(new IIPAddress[] { IPv4Address.Parse("10.0.0.7") }));

            await handle.WithdrawAsync();

            Assert.Multiple(() => {
                Assert.That(testbed.Server.Advertisements,             Is.Empty);
                Assert.That(testbed.Server.Responder.Publications,     Is.Empty);
                Assert.That(dnssd.Publication?.State,                  Is.EqualTo(MulticastDNSPublicationState.Withdrawn));
                Assert.That(handle.IsPublished,                        Is.False);
            });

        }

        #endregion

        #region NameConflict_TheSecondAdvertisementIsNotPublished()

        [Test]
        public async Task NameConflict_TheSecondAdvertisementIsNotPublished()
        {

            await using var testbed = await Testbed.StartAsync();

            var conflicts = new ConcurrentQueue<String>();

            testbed.Client.Responder.OnNameConflict += (ts, sender, publication, ownRecord, conflictingRecord, source, ct) => {
                conflicts.Enqueue($"{ownRecord.DomainName.FullName} vs {conflictingRecord.DomainName.FullName} from {source}");
                return Task.CompletedTask;
            };

            // The server owns "dup.local." ...
            var first   = await testbed.Server.AdvertiseAsync(Advertisement("dup.local.", Address: "10.0.0.7", Name: "First"));

            // ... so the client's probes for the same host name are answered with other data.
            var second  = await testbed.Client.AdvertiseAsync(Advertisement("dup.local.", Address: "10.0.0.8", Name: "Second"));

            var secondPublication = ((DNSSDAdvertiser) second).Publication;

            Assert.Multiple(() => {
                Assert.That(first.IsPublished,                     Is.True);
                Assert.That(first.HasConflict,                     Is.False);
                Assert.That(second.IsPublished,                    Is.False);
                Assert.That(second.HasConflict,                    Is.True);
                Assert.That(secondPublication?.State,              Is.EqualTo(MulticastDNSPublicationState.Conflict));
                Assert.That(secondPublication?.OwnConflictedRecord, Is.Not.Null);
                Assert.That(secondPublication?.ConflictingRecord,  Is.Not.Null);
                Assert.That(secondPublication?.ConflictSource,     Is.Not.Null);
                Assert.That(conflicts,                             Is.Not.Empty);
                Assert.That(conflicts.First(),                     Does.Contain("dup"));
                Assert.That(testbed.Client.Advertisements,         Is.EquivalentTo(new[] { second }),   "listed until withdrawn");
            });

            // A conflicted advertisement cannot be updated, only withdrawn.
            Assert.That(await CatchAsync(() => second.UpdateAsync(Advertisement("dup.local.", Address: "10.0.0.8", Name: "Second again"))),
                        Is.TypeOf<InvalidOperationException>());

            await second.WithdrawAsync();

            Assert.Multiple(() => {
                Assert.That(testbed.Client.Advertisements,   Is.Empty);
                Assert.That(second.IsPublished,              Is.False);
                Assert.That(first.IsPublished,               Is.True);
            });

        }

        #endregion

        #region EndpointAdvertiser_NameConflict_WithdrawsReportsAndRecoversWithForce()

        [Test]
        public async Task EndpointAdvertiser_NameConflict_WithdrawsReportsAndRecoversWithForce()
        {

            await using var testbed = await Testbed.StartAsync();

            // The server owns "dup.local." ...
            var other = await testbed.Server.AdvertiseAsync(Advertisement("dup.local.", Address: "10.0.0.7", Name: "Other"));

            // ... and the client's endpoint claims the same host name.
            var endpoint = new LocalEndpoint(
                               new EndpointDescription("Duplicate"),
                               Deployment.LAN,
                               S2BaseURL.Parse("http://dup.local:8443/pairing/", AllowHTTP: true)
                           );

            var conflicts   = new ConcurrentQueue<String>();
            var withdrawn   = new ConcurrentQueue<S2ServiceAdvertisement>();

            await using var advertiser = new EndpointAdvertiser(
                                             testbed.Client,
                                             endpoint,
                                             new EndpointAdvertiserOptions {
                                                 CheckInterval            = TimeSpan.FromHours(1),
                                                 OnlyWhenReadyForPairing  = false,
                                                 Addresses                = [ IPv4Address.Parse("10.0.0.8") ]
                                             }
                                         );

            advertiser.OnConflict  += (ts, sender, description,   ct) => { conflicts.Enqueue(description);   return Task.CompletedTask; };
            advertiser.OnWithdrawn += (ts, sender, advertisement, ct) => { withdrawn.Enqueue(advertisement); return Task.CompletedTask; };

            await advertiser.StartAsync();

            Assert.Multiple(() => {
                Assert.That(advertiser.IsRunning,             Is.True);
                Assert.That(advertiser.HasConflict,           Is.True);
                Assert.That(advertiser.IsAdvertised,          Is.False);
                Assert.That(advertiser.CurrentAdvertisement,  Is.Null);
                Assert.That(conflicts,                        Has.Count.EqualTo(1));
                Assert.That(conflicts.First(),                Does.Contain("already used"));
                Assert.That(withdrawn,                        Is.Empty,   "never published, so nothing to withdraw");
                Assert.That(testbed.Client.Advertisements,    Is.Empty);
                Assert.That(other.IsPublished,                Is.True);
            });

            // A plain refresh does not retry after a conflict...
            await advertiser.RefreshAsync();

            Assert.Multiple(() => {
                Assert.That(advertiser.HasConflict,   Is.True);
                Assert.That(advertiser.IsAdvertised,  Is.False);
                Assert.That(conflicts,                Has.Count.EqualTo(1));
            });

            // ... a forced one does, and succeeds once the other host left.
            await other.WithdrawAsync();
            await advertiser.RefreshAsync(Force: true);

            Assert.Multiple(() => {
                Assert.That(advertiser.HasConflict,                            Is.False);
                Assert.That(advertiser.IsAdvertised,                           Is.True);
                Assert.That(advertiser.CurrentAdvertisement?.HostName.FullName, Is.EqualTo("dup.local."));
                Assert.That(testbed.Client.Advertisements,                     Has.Count.EqualTo(1));
                Assert.That(conflicts,                                         Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region ServiceTypes_ThreeWithoutARole_OneWithARole()

        [Test]
        public async Task ServiceTypes_ThreeWithoutARole_OneWithARole()
        {

            await using var testbed   = await Testbed.StartAsync();
            await using var all       = (DNSSDBrowser) await testbed.Client.BrowseAsync();
            await using var rm        = (DNSSDBrowser) await testbed.Client.BrowseAsync(EnergyManagementRole.RM);
            await using var cem       = (DNSSDBrowser) await testbed.Client.BrowseAsync(EnergyManagementRole.CEM);

            Assert.Multiple(() => {

                Assert.That(all.Role,                                       Is.Null);
                Assert.That(all.ServiceTypes.Select(type => type.FullName), Is.EqualTo(new[] {
                                                                                "_s2connect._tcp.local.",
                                                                                "_cem._sub._s2connect._tcp.local.",
                                                                                "_rm._sub._s2connect._tcp.local."
                                                                            }));

                Assert.That(rm.Role,                                        Is.EqualTo(EnergyManagementRole.RM));
                Assert.That(rm.ServiceTypes.Select(type => type.FullName),  Is.EqualTo(new[] { "_rm._sub._s2connect._tcp.local." }));

                Assert.That(cem.Role,                                       Is.EqualTo(EnergyManagementRole.CEM));
                Assert.That(cem.ServiceTypes.Select(type => type.FullName), Is.EqualTo(new[] { "_cem._sub._s2connect._tcp.local." }));

                // One Multicast DNS browser per service type name.
                Assert.That(testbed.Client.Client.Browsers,                 Has.Count.EqualTo(5));
                Assert.That(all.TimeProvider,                               Is.SameAs(testbed.Client.TimeProvider));

            });

        }

        #endregion

        #region Withdraw_SendsAGoodbye_AndRemovesTheEndpointAtOnce()

        [Test]
        public async Task Withdraw_SendsAGoodbye_AndRemovesTheEndpointAtOnce()
        {

            await using var testbed   = await Testbed.StartAsync();
            await using var browser   = await testbed.Client.BrowseAsync();

            var disappeared = new ConcurrentQueue<S2DiscoveredEndpoint>();

            browser.OnEndpointDisappeared += (ts, sender, endpoint, ct) => { disappeared.Enqueue(endpoint); return Task.CompletedTask; };

            var handle = await testbed.Server.AdvertiseAsync(Advertisement("bye.local."));

            await WaitUntil(() => browser.Endpoints.Any(e => e.HostsRM == true));

            Assert.That(testbed.Client.Client.Options.GoodbyeDelay, Is.EqualTo(TimeSpan.Zero), "a goodbye removes the records at once");

            // The goodbye packet reaches the client synchronously on the in-memory network.
            await handle.WithdrawAsync();

            await WaitUntil(() => browser.Endpoints.Count == 0);

            Assert.Multiple(() => {
                Assert.That(handle.IsPublished,                              Is.False);
                Assert.That(disappeared,                                     Has.Count.EqualTo(1),   "one event, even though three service types were browsed");
                Assert.That(disappeared.First().HostName.FullName,           Is.EqualTo("bye.local."));
                Assert.That(disappeared.First().InstanceName.FullName,       Is.EqualTo("bye._s2connect._tcp.local."));
                Assert.That(browser.Endpoints,                               Is.Empty);
            });

        }

        #endregion

    }

}

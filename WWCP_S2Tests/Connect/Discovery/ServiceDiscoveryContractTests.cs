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
    /// The contract every <see cref="IServiceDiscovery"/> implementation has to fulfil (PLAN.md §3, D2):
    /// advertising, browsing (with and without a role filter), updating and withdrawing, resolving
    /// the advertised host names and the life cycle of the discovery and its browsers. The concrete
    /// fixtures below run the same tests against the in-memory test double and against the DNS-SD
    /// discovery on an in-memory Multicast DNS network. Every advertisement carries explicit
    /// addresses, so that both variants report the same addresses.
    /// </summary>
    public abstract class ServiceDiscoveryContractTests
    {

        #region Data

        private   const     String    DefaultHost     = "hp.local.";
        private   const     String    DefaultName     = "Heat pump";
        private   const     String    DefaultAddress  = "10.0.0.7";

        private   static readonly TimeSpan  WaitTimeout  = TimeSpan.FromSeconds(5);

        private   Func<ValueTask>?  cleanup;

        /// <summary>
        /// The discovery that advertises.
        /// </summary>
        protected IServiceDiscovery  A  { get; private set; } = null!;

        /// <summary>
        /// The discovery that browses (the same instance as A for the in-memory variant).
        /// </summary>
        protected IServiceDiscovery  B  { get; private set; } = null!;

        #endregion

        #region Abstract factories

        /// <summary>
        /// Two started discoveries that see each other.
        /// </summary>
        protected abstract Task<(IServiceDiscovery A, IServiceDiscovery B, Func<ValueTask> Cleanup)> CreatePairAsync();

        /// <summary>
        /// A discovery that was created but not started.
        /// </summary>
        protected abstract (IServiceDiscovery Discovery, Func<ValueTask> Cleanup) CreateUnstarted();

        #endregion

        #region SetUp / TearDown

        [SetUp]
        public async Task SetUpAsync()
        {
            (A, B, cleanup) = await CreatePairAsync();
        }

        [TearDown]
        public async Task TearDownAsync()
        {

            if (cleanup is not null)
                await cleanup();

            cleanup = null;

        }

        #endregion

        #region Helpers

        private static S2ServiceAdvertisement Advertisement(String                              Host      = DefaultHost,
                                                            String                              Name      = DefaultName,
                                                            IEnumerable<EnergyManagementRole>?  Roles     = null,
                                                            String                              Address   = DefaultAddress,
                                                            UInt16                              Port      = 8443)

            => new (DomainName.Parse(Host),
                    IPPort.Parse(Port),
                    new S2DNSSDTXTRecord(S2BaseURL.Parse($"https://{Host.TrimEnd('.')}:{Port}/pairing/"),
                                         EndpointName: Name),
                    Roles ?? [ EnergyManagementRole.RM ],
                    [ IPv4Address.Parse(Address) ]);

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


        #region Start_BothDiscoveriesAreRunning()

        [Test]
        public void Start_BothDiscoveriesAreRunning()
        {
            Assert.Multiple(() => {
                Assert.That(A.IsRunning,   Is.True);
                Assert.That(B.IsRunning,   Is.True);
                Assert.That(A.DNSClient,   Is.Not.Null);
                Assert.That(B.DNSClient,   Is.Not.Null);
            });
        }

        #endregion

        #region Advertise_IsReportedToABrowserOfTheSameRole()

        [Test]
        public async Task Advertise_IsReportedToABrowserOfTheSameRole()
        {

            await using var browser   = await B.BrowseAsync(EnergyManagementRole.RM);
            var             appeared  = new ConcurrentQueue<S2DiscoveredEndpoint>();

            browser.OnEndpointAppeared += (ts, sender, e, ct) => { appeared.Enqueue(e); return Task.CompletedTask; };

            await using var handle    = await A.AdvertiseAsync(Advertisement());
            var             endpoint  = await browser.WaitForEndpointAsync(Timeout: WaitTimeout);

            Assert.That(endpoint, Is.Not.Null);
            Assert.Multiple(() => {
                Assert.That(endpoint!.InstanceName.FullName,         Is.EqualTo("hp._s2connect._tcp.local."));
                Assert.That(endpoint.HostName.FullName,              Is.EqualTo("hp.local."));
                Assert.That(endpoint.Port.ToUInt16(),                Is.EqualTo((UInt16) 8443));
                Assert.That(endpoint.PairingUrl?.Value,              Is.EqualTo("https://hp.local:8443/pairing/"));
                Assert.That(endpoint.Name,                           Is.EqualTo("Heat pump"));
                Assert.That(endpoint.HostsRM,                        Is.True);
                Assert.That(endpoint.Addresses,                      Is.EqualTo(new IIPAddress[] { IPv4Address.Parse("10.0.0.7") }));
                Assert.That(appeared,                                Has.Count.EqualTo(1));
                Assert.That(browser.Role,                            Is.EqualTo(EnergyManagementRole.RM));
                Assert.That(handle.IsPublished,                      Is.True);
                Assert.That(handle.HasConflict,                      Is.False);
                Assert.That(handle.Advertisement.HostName.FullName,  Is.EqualTo("hp.local."));
            });

        }

        #endregion

        #region Advertise_IsNotReportedToABrowserOfAnotherRole()

        [Test]
        public async Task Advertise_IsNotReportedToABrowserOfAnotherRole()
        {

            await using var browser   = await B.BrowseAsync(EnergyManagementRole.CEM);
            await using var handle    = await A.AdvertiseAsync(Advertisement());

            // An RM-only endpoint never shows up on a CEM browser.
            var endpoint = await browser.WaitForEndpointAsync(Timeout: TimeSpan.FromMilliseconds(300));

            Assert.Multiple(() => {
                Assert.That(endpoint,           Is.Null);
                Assert.That(browser.Endpoints,  Is.Empty);
                Assert.That(handle.IsPublished, Is.True);
            });

        }

        #endregion

        #region Advertise_IsReportedToABrowserWithoutRole_WithTheRoleOfTheEndpoint()

        [Test]
        public async Task Advertise_IsReportedToABrowserWithoutRole_WithTheRoleOfTheEndpoint()
        {

            await using var browser   = await B.BrowseAsync();
            await using var handle    = await A.AdvertiseAsync(Advertisement());

            // DNS-SD learns the roles from the subtype browsers a moment after the endpoint appeared.
            var endpoint = await browser.WaitForEndpointAsync(e => e.HostsRM == true, WaitTimeout);

            Assert.That(endpoint, Is.Not.Null);
            Assert.Multiple(() => {
                Assert.That(browser.Role,                  Is.Null);
                Assert.That(endpoint!.HostsRM,             Is.True);
                Assert.That(endpoint.HostsRole(EnergyManagementRole.RM),  Is.True);
                Assert.That(endpoint.HostsCEM,             Is.Not.True, "false (in-memory) or unknown (DNS-SD), never true");
                Assert.That(endpoint.PairingUrl?.Value,    Is.EqualTo("https://hp.local:8443/pairing/"));
                Assert.That(browser.Endpoints,             Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region Endpoints_ListAnEndpointAdvertisedBeforeBrowsing()

        [Test]
        public async Task Endpoints_ListAnEndpointAdvertisedBeforeBrowsing()
        {

            await using var handle    = await A.AdvertiseAsync(Advertisement());
            await using var browser   = await B.BrowseAsync(EnergyManagementRole.RM);

            var endpoint = await browser.WaitForEndpointAsync(Timeout: WaitTimeout);

            Assert.That(endpoint, Is.Not.Null);
            Assert.Multiple(() => {
                Assert.That(browser.Endpoints,                            Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints[0].InstanceName.FullName,   Is.EqualTo("hp._s2connect._tcp.local."));
                Assert.That(browser.Endpoints[0],                         Is.EqualTo(endpoint));
                Assert.That(browser.Endpoints[0].Addresses,               Is.EqualTo(new IIPAddress[] { IPv4Address.Parse("10.0.0.7") }));
            });

        }

        #endregion

        #region Update_WithAChangedName_RaisesOnEndpointUpdated()

        [Test]
        public async Task Update_WithAChangedName_RaisesOnEndpointUpdated()
        {

            await using var browser   = await B.BrowseAsync(EnergyManagementRole.RM);
            var             updated   = new ConcurrentQueue<S2DiscoveredEndpoint>();

            browser.OnEndpointUpdated += (ts, sender, e, ct) => { updated.Enqueue(e); return Task.CompletedTask; };

            await using var handle    = await A.AdvertiseAsync(Advertisement());

            Assert.That(await browser.WaitForEndpointAsync(Timeout: WaitTimeout), Is.Not.Null);

            await handle.UpdateAsync(Advertisement(Name: "Warm pump"));

            await WaitUntil(() => updated.Any(e => e.Name == "Warm pump"));

            Assert.Multiple(() => {
                Assert.That(handle.Advertisement.TXT.EndpointName,   Is.EqualTo("Warm pump"));
                Assert.That(handle.IsPublished,                      Is.True);
                Assert.That(browser.Endpoints,                       Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints[0].Name,               Is.EqualTo("Warm pump"));
                Assert.That(browser.Endpoints[0].HostName.FullName,  Is.EqualTo("hp.local."));
            });

        }

        #endregion

        #region Update_WithAnotherHostName_Throws()

        [Test]
        public async Task Update_WithAnotherHostName_Throws()
        {

            await using var handle = await A.AdvertiseAsync(Advertisement());

            var exception = await CatchAsync(() => handle.UpdateAsync(Advertisement(Host: "other.local.")));

            Assert.Multiple(() => {
                Assert.That(exception,                               Is.TypeOf<ArgumentException>());
                Assert.That(handle.IsPublished,                      Is.True);
                Assert.That(handle.Advertisement.HostName.FullName,  Is.EqualTo("hp.local."));
            });

        }

        #endregion

        #region Withdraw_RaisesOnEndpointDisappeared_AndIsIdempotent()

        [Test]
        public async Task Withdraw_RaisesOnEndpointDisappeared_AndIsIdempotent()
        {

            await using var browser      = await B.BrowseAsync(EnergyManagementRole.RM);
            var             disappeared  = new ConcurrentQueue<S2DiscoveredEndpoint>();

            browser.OnEndpointDisappeared += (ts, sender, e, ct) => { disappeared.Enqueue(e); return Task.CompletedTask; };

            var handle = await A.AdvertiseAsync(Advertisement());

            Assert.That(await browser.WaitForEndpointAsync(Timeout: WaitTimeout), Is.Not.Null);

            await handle.WithdrawAsync();
            await WaitUntil(() => browser.Endpoints.Count == 0);

            Assert.Multiple(() => {
                Assert.That(handle.IsPublished,                          Is.False);
                Assert.That(disappeared,                                 Has.Count.EqualTo(1));
                Assert.That(disappeared.First().HostName.FullName,       Is.EqualTo("hp.local."));
                Assert.That(disappeared.First().InstanceName.FullName,   Is.EqualTo("hp._s2connect._tcp.local."));
            });

            // A second withdrawal is harmless.
            Assert.That(await CatchAsync(() => handle.WithdrawAsync()), Is.Null);
            Assert.That(handle.IsPublished, Is.False);

        }

        #endregion

        #region DNSClient_ResolvesTheAdvertisedHostName()

        [Test]
        public async Task DNSClient_ResolvesTheAdvertisedHostName()
        {

            await using var browser   = await B.BrowseAsync(EnergyManagementRole.RM);
            await using var handle    = await A.AdvertiseAsync(Advertisement());

            Assert.That(await browser.WaitForEndpointAsync(Timeout: WaitTimeout), Is.Not.Null);

            var addresses = (await B.DNSClient.Query_IPv4Addresses(DomainName.Parse("hp.local."))).ToArray();

            Assert.That(addresses, Is.EqualTo(new[] { IPv4Address.Parse("10.0.0.7") }));

        }

        #endregion

        #region Advertise_TwoHosts_AreBothListed()

        [Test]
        public async Task Advertise_TwoHosts_AreBothListed()
        {

            await using var browser   = await B.BrowseAsync(EnergyManagementRole.RM);
            await using var alpha     = await A.AdvertiseAsync(Advertisement("alpha.local.", "Alpha", Address: "10.0.0.71"));
            await using var beta      = await A.AdvertiseAsync(Advertisement("beta.local.",  "Beta",  Address: "10.0.0.72"));

            await WaitUntil(() => browser.Endpoints.Count == 2);

            Assert.Multiple(() => {
                Assert.That(browser.Endpoints.Select(e => e.HostName.FullName),  Is.EquivalentTo(new[] { "alpha.local.", "beta.local." }));
                Assert.That(browser.Endpoints.Select(e => e.Name),               Is.EquivalentTo(new[] { "Alpha", "Beta" }));
                Assert.That(alpha.IsPublished,                                   Is.True);
                Assert.That(beta. IsPublished,                                   Is.True);
            });

        }

        #endregion

        #region Stop_WithdrawsEveryAdvertisement()

        [Test]
        public async Task Stop_WithdrawsEveryAdvertisement()
        {

            await using var browser      = await B.BrowseAsync(EnergyManagementRole.RM);
            var             disappeared  = new ConcurrentQueue<S2DiscoveredEndpoint>();

            browser.OnEndpointDisappeared += (ts, sender, e, ct) => { disappeared.Enqueue(e); return Task.CompletedTask; };

            var handle = await A.AdvertiseAsync(Advertisement());

            Assert.That(await browser.WaitForEndpointAsync(Timeout: WaitTimeout), Is.Not.Null);

            await A.StopAsync();
            await WaitUntil(() => browser.Endpoints.Count == 0);

            Assert.Multiple(() => {
                Assert.That(A.IsRunning,         Is.False);
                Assert.That(handle.IsPublished,  Is.False);
                Assert.That(disappeared,         Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region AdvertiseAndBrowse_OnAStoppedDiscovery_Throw()

        [Test]
        public async Task AdvertiseAndBrowse_OnAStoppedDiscovery_Throw()
        {

            await A.StopAsync();

            var advertise  = await CatchAsync(() => A.AdvertiseAsync(Advertisement()));
            var browse     = await CatchAsync(() => A.BrowseAsync(EnergyManagementRole.RM));

            Assert.Multiple(() => {
                Assert.That(A.IsRunning,  Is.False);
                Assert.That(advertise,    Is.TypeOf<InvalidOperationException>());
                Assert.That(browse,       Is.TypeOf<InvalidOperationException>());
            });

        }

        #endregion

        #region AdvertiseAndBrowse_OnANotStartedDiscovery_Throw()

        [Test]
        public async Task AdvertiseAndBrowse_OnANotStartedDiscovery_Throw()
        {

            var (discovery, cleanupUnstarted) = CreateUnstarted();

            try
            {

                var advertise  = await CatchAsync(() => discovery.AdvertiseAsync(Advertisement()));
                var browse     = await CatchAsync(() => discovery.BrowseAsync());

                Assert.Multiple(() => {
                    Assert.That(discovery.IsRunning,  Is.False);
                    Assert.That(advertise,            Is.TypeOf<InvalidOperationException>());
                    Assert.That(browse,               Is.TypeOf<InvalidOperationException>());
                });

            }
            finally
            {
                await cleanupUnstarted();
            }

        }

        #endregion

        #region DisposedBrowser_ReceivesNoEvents()

        [Test]
        public async Task DisposedBrowser_ReceivesNoEvents()
        {

            var browser   = await B.BrowseAsync(EnergyManagementRole.RM);
            var appeared  = new ConcurrentQueue<S2DiscoveredEndpoint>();

            browser.OnEndpointAppeared += (ts, sender, e, ct) => { appeared.Enqueue(e); return Task.CompletedTask; };

            await browser.DisposeAsync();

            await using var handle = await A.AdvertiseAsync(Advertisement());

            // Give a still-subscribed browser every chance to report the endpoint...
            await Task.Delay(200);

            Assert.Multiple(() => {
                Assert.That(handle.IsPublished,  Is.True);
                Assert.That(appeared,            Is.Empty);
                Assert.That(browser.Endpoints,   Is.Empty);
            });

            // ... and disposing twice is harmless.
            Assert.That(await CatchAsync(() => browser.DisposeAsync().AsTask()), Is.Null);

        }

        #endregion

    }


    /// <summary>
    /// The service discovery contract against the in-memory test double: advertiser and browser
    /// share one instance.
    /// </summary>
    [TestFixture]
    public sealed class InMemoryServiceDiscoveryContractTests : ServiceDiscoveryContractTests
    {

        protected override async Task<(IServiceDiscovery A, IServiceDiscovery B, Func<ValueTask> Cleanup)> CreatePairAsync()
        {

            var discovery = new InMemoryServiceDiscovery();

            await discovery.StartAsync();

            return (discovery, discovery, discovery.DisposeAsync);

        }

        protected override (IServiceDiscovery Discovery, Func<ValueTask> Cleanup) CreateUnstarted()
        {

            var discovery = new InMemoryServiceDiscovery();

            return (discovery, discovery.DisposeAsync);

        }

    }


    /// <summary>
    /// The service discovery contract against DNS-SD: two discoveries on one in-memory Multicast
    /// DNS network with the fast responder and client options of the smoke tests.
    /// </summary>
    [TestFixture]
    public sealed class DNSSDServiceDiscoveryContractTests : ServiceDiscoveryContractTests
    {

        protected override async Task<(IServiceDiscovery A, IServiceDiscovery B, Func<ValueTask> Cleanup)> CreatePairAsync()
        {

            var network     = new InMemoryMulticastDNSNetwork();
            var transportA  = network.CreateTransport(IPv4Address.Parse("10.0.0.1"));
            var transportB  = network.CreateTransport(IPv4Address.Parse("10.0.0.2"));
            var a           = new DNSSDServiceDiscovery(DiscoverySmokeTests.FastDiscoveryOptions(transportA));
            var b           = new DNSSDServiceDiscovery(DiscoverySmokeTests.FastDiscoveryOptions(transportB));

            await a.StartAsync();
            await b.StartAsync();

            return (a, b, async () => {
                await b.DisposeAsync();
                await a.DisposeAsync();
                await transportB.DisposeAsync();
                await transportA.DisposeAsync();
            });

        }

        protected override (IServiceDiscovery Discovery, Func<ValueTask> Cleanup) CreateUnstarted()
        {

            var network    = new InMemoryMulticastDNSNetwork();
            var transport  = network.CreateTransport(IPv4Address.Parse("10.0.0.3"));
            var discovery  = new DNSSDServiceDiscovery(DiscoverySmokeTests.FastDiscoveryOptions(transport));

            return (discovery, async () => {
                await discovery.DisposeAsync();
                await transport.DisposeAsync();
            });

        }

    }

}

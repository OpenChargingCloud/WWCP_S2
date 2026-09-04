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

using Microsoft.Extensions.Time.Testing;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect.Discovery
{

    /// <summary>
    /// The endpoint advertiser (S2 Connect 1.0.0, "DNS-SD based discovery": "An endpoint should
    /// publish its service through DNS-SD once it is ready for pairing, and until it shuts down")
    /// on the in-memory service discovery: the advertisement follows the pairing tokens and the
    /// readiness of the hosted nodes, is updated when the roles change, withdrawn on stop, and
    /// derived from the pairing URL and the options of the local endpoint.
    /// </summary>
    [TestFixture]
    public sealed class EndpointAdvertiserTests
    {

        #region Data

        private const           String        PairingUrlText        = "http://hp.local:8443/pairing/";

        private static readonly PairingToken  StaticToken           = PairingToken.Parse("ABCDEF23");
        private static readonly TimeSpan      DefaultCheckInterval  = TimeSpan.FromMilliseconds(20);
        private static readonly TimeSpan      WaitTimeout           = TimeSpan.FromSeconds(5);

        #endregion

        #region Helpers

        private static async Task<InMemoryServiceDiscovery> StartDiscoveryAsync(TimeProvider? Clock = null)
        {

            var discovery = new InMemoryServiceDiscovery(Clock);

            await discovery.StartAsync();

            return discovery;

        }

        private static LocalEndpoint CreateEndpoint(String         PairingUrl   = PairingUrlText,
                                                    TimeProvider?  Clock        = null)

            => new (new EndpointDescription("Heat pump"),
                    Deployment.LAN,
                    S2BaseURL.Parse(PairingUrl, AllowHTTP: true),
                    TimeProvider: Clock);

        private static NodeDescription Description(EnergyManagementRole Role)

            => new (Node_Id.NewRandom,
                    "GraphDefined",
                    Role == EnergyManagementRole.CEM ? "EMS"   : "heat pump",
                    Role == EnergyManagementRole.CEM ? "EMS 1" : "HP 1",
                    Role);

        private static EndpointAdvertiserOptions Options(TimeSpan?    CheckInterval             = null,
                                                         Boolean      OnlyWhenReadyForPairing   = true,
                                                         S2BaseURL?   LongPollingUrl            = null,
                                                         DomainName?  HostName                  = null)

            => new () {
                   CheckInterval            = CheckInterval ?? DefaultCheckInterval,
                   OnlyWhenReadyForPairing  = OnlyWhenReadyForPairing,
                   LongPollingUrl           = LongPollingUrl,
                   HostName                 = HostName,
                   Addresses                = [ IPv4Address.Localhost ]
               };

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

        /// <summary>
        /// Records the events of an endpoint advertiser (the timer raises them on other threads).
        /// </summary>
        private sealed class Recorder
        {

            public ConcurrentQueue<S2ServiceAdvertisement>  Advertised    { get; } = new ();
            public ConcurrentQueue<S2ServiceAdvertisement>  Withdrawn     { get; } = new ();
            public ConcurrentQueue<String>                  Conflicts     { get; } = new ();

            public Recorder(EndpointAdvertiser Advertiser)
            {
                Advertiser.OnAdvertised  += (ts, sender, advertisement, ct) => { Advertised.Enqueue(advertisement); return Task.CompletedTask; };
                Advertiser.OnWithdrawn   += (ts, sender, advertisement, ct) => { Withdrawn. Enqueue(advertisement); return Task.CompletedTask; };
                Advertiser.OnConflict    += (ts, sender, description,   ct) => { Conflicts. Enqueue(description);   return Task.CompletedTask; };
            }

        }

        #endregion


        #region Start_WithoutAPairingToken_DoesNotAdvertise()

        [Test]
        public async Task Start_WithoutAPairingToken_DoesNotAdvertise()
        {

            await using var discovery   = await StartDiscoveryAsync();
            await using var browser     = await discovery.BrowseAsync(EnergyManagementRole.RM);

            var endpoint = CreateEndpoint();
            var node     = endpoint.AddNode(Description(EnergyManagementRole.RM));

            await using var advertiser  = new EndpointAdvertiser(discovery, endpoint, Options());
            var             recorder    = new Recorder(advertiser);

            await advertiser.StartAsync();

            // Let the periodic check run a few times.
            await Task.Delay(100);

            Assert.Multiple(() => {
                Assert.That(advertiser.Discovery,             Is.SameAs(discovery));
                Assert.That(advertiser.Endpoint,              Is.SameAs(endpoint));
                Assert.That(advertiser.IsRunning,             Is.True);
                Assert.That(node.HasValidPairingToken,        Is.False);
                Assert.That(advertiser.ShouldAdvertise,       Is.False);
                Assert.That(advertiser.IsAdvertised,          Is.False);
                Assert.That(advertiser.HasConflict,           Is.False);
                Assert.That(advertiser.CurrentAdvertisement,  Is.Null);
                Assert.That(recorder.Advertised,              Is.Empty);
                Assert.That(recorder.Withdrawn,               Is.Empty);
                Assert.That(browser.Endpoints,                Is.Empty);
                Assert.That(discovery.Advertisements,         Is.Empty);
            });

        }

        #endregion

        #region Refresh_WithAStaticToken_Advertises()

        [Test]
        public async Task Refresh_WithAStaticToken_Advertises()
        {

            await using var discovery   = await StartDiscoveryAsync();
            await using var browser     = await discovery.BrowseAsync(EnergyManagementRole.RM);

            var endpoint = CreateEndpoint();
            var node     = endpoint.AddNode(Description(EnergyManagementRole.RM));

            await using var advertiser  = new EndpointAdvertiser(discovery, endpoint, Options());
            var             recorder    = new Recorder(advertiser);

            await advertiser.StartAsync();

            Assert.That(advertiser.IsAdvertised, Is.False);

            node.SetStaticPairingToken(StaticToken);
            await advertiser.RefreshAsync();

            var current = advertiser.CurrentAdvertisement;

            Assert.That(current, Is.Not.Null);
            Assert.Multiple(() => {
                Assert.That(advertiser.ShouldAdvertise,        Is.True);
                Assert.That(advertiser.IsAdvertised,           Is.True);
                Assert.That(current!.HostName.FullName,        Is.EqualTo("hp.local."));
                Assert.That(current.InstanceName.FullName,     Is.EqualTo("hp._s2connect._tcp.local."));
                Assert.That(current.Port.ToUInt16(),           Is.EqualTo((UInt16) 8443));
                Assert.That(current.TXT.PairingUrl,            Is.EqualTo(endpoint.PairingUrl));
                Assert.That(current.TXT.LongPollingUrl,        Is.Null);
                Assert.That(current.TXT.EndpointName,          Is.EqualTo("Heat pump"));
                Assert.That(current.Roles,                     Is.EquivalentTo(new[] { EnergyManagementRole.RM }));
                Assert.That(current.Addresses,                 Is.EqualTo(new IIPAddress[] { IPv4Address.Localhost }));
                Assert.That(recorder.Advertised,               Has.Count.EqualTo(1));
                Assert.That(recorder.Advertised.First(),       Is.EqualTo(current));
                Assert.That(recorder.Withdrawn,                Is.Empty);
                Assert.That(discovery.Advertisements,          Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints,                 Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints[0].PairingUrl,   Is.EqualTo(endpoint.PairingUrl));
                Assert.That(browser.Endpoints[0].Name,         Is.EqualTo("Heat pump"));
                Assert.That(browser.Endpoints[0].HostsRM,      Is.True);
                Assert.That(browser.Endpoints[0].Addresses,    Is.EqualTo(new IIPAddress[] { IPv4Address.Localhost }));
            });

            // The periodic check does not announce an unchanged advertisement again.
            await Task.Delay(100);

            Assert.That(recorder.Advertised, Has.Count.EqualTo(1));

        }

        #endregion

        #region Refresh_WithADynamicToken_Advertises()

        [Test]
        public async Task Refresh_WithADynamicToken_Advertises()
        {

            await using var discovery   = await StartDiscoveryAsync();
            await using var browser     = await discovery.BrowseAsync(EnergyManagementRole.RM);

            var endpoint = CreateEndpoint();
            var node     = endpoint.AddNode(Description(EnergyManagementRole.RM));

            await using var advertiser  = new EndpointAdvertiser(discovery, endpoint, Options());
            var             recorder    = new Recorder(advertiser);

            await advertiser.StartAsync();

            var code = node.IssueDynamicPairingToken();
            await advertiser.RefreshAsync();

            Assert.Multiple(() => {
                Assert.That(code.PairingToken.Length,          Is.EqualTo(6));
                Assert.That(node.PairingTokenExpiresAt,        Is.Not.Null);
                Assert.That(advertiser.IsAdvertised,           Is.True);
                Assert.That(recorder.Advertised,               Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints,                 Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints[0].PairingUrl,   Is.EqualTo(endpoint.PairingUrl));
            });

        }

        #endregion

        #region Refresh_WhenTheNodeIsNotReadyForPairing_Withdraws()

        [Test]
        public async Task Refresh_WhenTheNodeIsNotReadyForPairing_Withdraws()
        {

            await using var discovery   = await StartDiscoveryAsync();
            await using var browser     = await discovery.BrowseAsync(EnergyManagementRole.RM);

            var endpoint = CreateEndpoint();
            var node     = endpoint.AddNode(Description(EnergyManagementRole.RM));

            await using var advertiser  = new EndpointAdvertiser(discovery, endpoint, Options());
            var             recorder    = new Recorder(advertiser);

            await advertiser.StartAsync();

            node.SetStaticPairingToken(StaticToken);
            await advertiser.RefreshAsync();

            Assert.That(advertiser.IsAdvertised, Is.True);

            node.IsReadyForPairing = false;
            await advertiser.RefreshAsync();

            Assert.Multiple(() => {
                Assert.That(node.HasValidPairingToken,                     Is.True,  "the token is still there, but the node is not ready");
                Assert.That(advertiser.ShouldAdvertise,                    Is.False);
                Assert.That(advertiser.IsAdvertised,                       Is.False);
                Assert.That(advertiser.CurrentAdvertisement,               Is.Null);
                Assert.That(recorder.Withdrawn,                            Has.Count.EqualTo(1));
                Assert.That(recorder.Withdrawn.First().HostName.FullName,  Is.EqualTo("hp.local."));
                Assert.That(browser.Endpoints,                             Is.Empty);
                Assert.That(discovery.Advertisements,                      Is.Empty);
            });

            // Ready again: advertised again.
            node.IsReadyForPairing = true;
            await advertiser.RefreshAsync();

            Assert.Multiple(() => {
                Assert.That(advertiser.IsAdvertised,   Is.True);
                Assert.That(recorder.Advertised,       Has.Count.EqualTo(2));
                Assert.That(browser.Endpoints,         Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region Refresh_WhenTheDynamicTokenExpired_Withdraws()

        [Test]
        public async Task Refresh_WhenTheDynamicTokenExpired_Withdraws()
        {

            var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 4, 10, 0, 0, TimeSpan.Zero));

            await using var discovery   = await StartDiscoveryAsync(clock);
            await using var browser     = await discovery.BrowseAsync(EnergyManagementRole.RM);

            var endpoint = CreateEndpoint(Clock: clock);
            var node     = endpoint.AddNode(Description(EnergyManagementRole.RM));

            // A long check interval: the fake clock would otherwise run the periodic check once
            // for every elapsed interval of the advance below; the refresh is called explicitly.
            await using var advertiser  = new EndpointAdvertiser(discovery, endpoint, Options(CheckInterval: TimeSpan.FromHours(1)), clock);
            var             recorder    = new Recorder(advertiser);

            await advertiser.StartAsync();

            node.IssueDynamicPairingToken(Lifetime: TimeSpan.FromMinutes(1));
            await advertiser.RefreshAsync();

            Assert.Multiple(() => {
                Assert.That(advertiser.TimeProvider,      Is.SameAs(clock));
                Assert.That(endpoint.TimeProvider,        Is.SameAs(clock));
                Assert.That(node.PairingTokenExpiresAt,   Is.EqualTo(clock.GetUtcNow() + TimeSpan.FromMinutes(1)));
                Assert.That(advertiser.IsAdvertised,      Is.True);
                Assert.That(browser.Endpoints,            Has.Count.EqualTo(1));
            });

            clock.Advance(TimeSpan.FromMinutes(2));
            await advertiser.RefreshAsync();

            Assert.Multiple(() => {
                Assert.That(node.HasValidPairingToken,    Is.False);
                Assert.That(node.PairingTokenExpiresAt,   Is.Null);
                Assert.That(advertiser.ShouldAdvertise,   Is.False);
                Assert.That(advertiser.IsAdvertised,      Is.False);
                Assert.That(recorder.Advertised,          Has.Count.EqualTo(1));
                Assert.That(recorder.Withdrawn,           Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints,            Is.Empty);
            });

        }

        #endregion

        #region Refresh_WhenTheTokenWasCleared_Withdraws()

        [Test]
        public async Task Refresh_WhenTheTokenWasCleared_Withdraws()
        {

            await using var discovery   = await StartDiscoveryAsync();
            await using var browser     = await discovery.BrowseAsync(EnergyManagementRole.RM);

            var endpoint = CreateEndpoint();
            var node     = endpoint.AddNode(Description(EnergyManagementRole.RM));

            await using var advertiser  = new EndpointAdvertiser(discovery, endpoint, Options());
            var             recorder    = new Recorder(advertiser);

            await advertiser.StartAsync();

            node.SetStaticPairingToken(StaticToken);
            await advertiser.RefreshAsync();

            Assert.That(advertiser.IsAdvertised, Is.True);

            node.ClearPairingToken();
            await advertiser.RefreshAsync();

            Assert.Multiple(() => {
                Assert.That(node.HasValidPairingToken,  Is.False);
                Assert.That(advertiser.IsAdvertised,    Is.False);
                Assert.That(recorder.Withdrawn,         Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints,          Is.Empty);
            });

        }

        #endregion

        #region Refresh_WithASecondNodeOfAnotherRole_UpdatesTheAdvertisement()

        [Test]
        public async Task Refresh_WithASecondNodeOfAnotherRole_UpdatesTheAdvertisement()
        {

            await using var discovery   = await StartDiscoveryAsync();
            await using var browser     = await discovery.BrowseAsync(EnergyManagementRole.RM);

            var endpoint = CreateEndpoint();
            var rm       = endpoint.AddNode(Description(EnergyManagementRole.RM));

            await using var advertiser  = new EndpointAdvertiser(discovery, endpoint, Options());
            var             recorder    = new Recorder(advertiser);

            await advertiser.StartAsync();

            rm.SetStaticPairingToken(StaticToken);
            await advertiser.RefreshAsync();

            Assert.That(advertiser.CurrentAdvertisement?.Roles, Is.EquivalentTo(new[] { EnergyManagementRole.RM }));

            // The CEM node has no token of its own; the RM node keeps the endpoint ready for pairing.
            endpoint.AddNode(Description(EnergyManagementRole.CEM));
            await advertiser.RefreshAsync();

            var current = advertiser.CurrentAdvertisement;

            Assert.That(current, Is.Not.Null);
            Assert.Multiple(() => {
                Assert.That(current!.Roles,                       Is.EquivalentTo(new[] { EnergyManagementRole.CEM, EnergyManagementRole.RM }));
                Assert.That(recorder.Advertised,                  Has.Count.EqualTo(2));
                Assert.That(recorder.Advertised.Last().Roles,     Is.EquivalentTo(new[] { EnergyManagementRole.CEM, EnergyManagementRole.RM }));
                Assert.That(recorder.Withdrawn,                   Is.Empty,                "an update, not a withdrawal and a new advertisement");
                Assert.That(discovery.Advertisements,             Has.Count.EqualTo(1),    "the same handle was updated");
                Assert.That(browser.Endpoints,                    Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints[0].HostsCEM,        Is.True);
                Assert.That(browser.Endpoints[0].HostsRM,         Is.True);
            });

        }

        #endregion

        #region Start_WithoutOnlyWhenReadyForPairing_AdvertisesWithoutAToken()

        [Test]
        public async Task Start_WithoutOnlyWhenReadyForPairing_AdvertisesWithoutAToken()
        {

            await using var discovery   = await StartDiscoveryAsync();
            await using var browser     = await discovery.BrowseAsync(EnergyManagementRole.RM);

            var endpoint = CreateEndpoint();
            var node     = endpoint.AddNode(Description(EnergyManagementRole.RM));

            await using var advertiser  = new EndpointAdvertiser(discovery, endpoint, Options(OnlyWhenReadyForPairing: false));
            var             recorder    = new Recorder(advertiser);

            Assert.That(advertiser.ShouldAdvertise, Is.True, "before the start: the option decides, not the tokens");

            await advertiser.StartAsync();

            Assert.Multiple(() => {
                Assert.That(node.HasValidPairingToken,                    Is.False);
                Assert.That(advertiser.IsAdvertised,                      Is.True);
                Assert.That(recorder.Advertised,                          Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints,                            Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints[0].HostName.FullName,       Is.EqualTo("hp.local."));
            });

        }

        #endregion

        #region Stop_WithdrawsAndRaisesOnWithdrawn()

        [Test]
        public async Task Stop_WithdrawsAndRaisesOnWithdrawn()
        {

            await using var discovery   = await StartDiscoveryAsync();
            await using var browser     = await discovery.BrowseAsync(EnergyManagementRole.RM);

            var endpoint = CreateEndpoint();
            var node     = endpoint.AddNode(Description(EnergyManagementRole.RM));

            // A long check interval: the stop, not the periodic check, is under test here.
            await using var advertiser  = new EndpointAdvertiser(discovery, endpoint, Options(CheckInterval: TimeSpan.FromHours(1)));
            var             recorder    = new Recorder(advertiser);

            await advertiser.StartAsync();

            node.SetStaticPairingToken(StaticToken);
            await advertiser.RefreshAsync();

            Assert.That(advertiser.IsAdvertised, Is.True);

            await advertiser.StopAsync();

            Assert.Multiple(() => {
                Assert.That(advertiser.IsRunning,                          Is.False);
                Assert.That(advertiser.IsAdvertised,                       Is.False);
                Assert.That(advertiser.CurrentAdvertisement,               Is.Null);
                Assert.That(recorder.Withdrawn,                            Has.Count.EqualTo(1));
                Assert.That(recorder.Withdrawn.First().HostName.FullName,  Is.EqualTo("hp.local."));
                Assert.That(browser.Endpoints,                             Is.Empty);
                Assert.That(discovery.Advertisements,                      Is.Empty);
            });

            // A refresh of a stopped advertiser does nothing...
            await advertiser.RefreshAsync();

            Assert.That(advertiser.IsAdvertised, Is.False);

            // ... and a stopped advertiser can be started again.
            await advertiser.StartAsync();

            Assert.Multiple(() => {
                Assert.That(advertiser.IsRunning,     Is.True);
                Assert.That(advertiser.IsAdvertised,  Is.True);
                Assert.That(recorder.Advertised,      Has.Count.EqualTo(2));
            });

        }

        #endregion

        #region DisposeAsync_Twice_IsHarmless()

        [Test]
        public async Task DisposeAsync_Twice_IsHarmless()
        {

            await using var discovery   = await StartDiscoveryAsync();

            var endpoint = CreateEndpoint();
            var node     = endpoint.AddNode(Description(EnergyManagementRole.RM));

            // A long check interval: the disposal, not the periodic check, is under test here.
            var advertiser  = new EndpointAdvertiser(discovery, endpoint, Options(CheckInterval: TimeSpan.FromHours(1)));
            var recorder    = new Recorder(advertiser);

            await advertiser.StartAsync();

            node.SetStaticPairingToken(StaticToken);
            await advertiser.RefreshAsync();

            Assert.That(advertiser.IsAdvertised, Is.True);

            await advertiser.DisposeAsync();
            await advertiser.DisposeAsync();

            Assert.Multiple(() => {
                Assert.That(advertiser.IsRunning,        Is.False);
                Assert.That(advertiser.IsAdvertised,     Is.False);
                Assert.That(recorder.Withdrawn,          Has.Count.EqualTo(1));
                Assert.That(discovery.Advertisements,    Is.Empty);
            });

            // A refresh after the disposal is a no-op, a start is refused.
            Assert.That(await CatchAsync(() => advertiser.RefreshAsync()),  Is.Null);
            Assert.That(await CatchAsync(() => advertiser.StartAsync()),    Is.TypeOf<ObjectDisposedException>());

        }

        #endregion

        #region Constructor_RejectsAWANEndpoint()

        [Test]
        public async Task Constructor_RejectsAWANEndpoint()
        {

            await using var discovery = await StartDiscoveryAsync();

            var wan = new LocalEndpoint(
                          new EndpointDescription("Backend"),
                          Deployment.WAN,
                          S2BaseURL.Parse("https://wan.example.org/pairing/")
                      );

            Assert.That(() => new EndpointAdvertiser(discovery, wan, Options()),
                        Throws.ArgumentException.With.Message.Contains("LAN"));

        }

        #endregion

        #region Constructor_RejectsAPairingUrlHostOutsideLocal_WithoutAHostNameOption()

        [Test]
        public async Task Constructor_RejectsAPairingUrlHostOutsideLocal_WithoutAHostNameOption()
        {

            await using var discovery = await StartDiscoveryAsync();

            var endpoint = CreateEndpoint("https://heatpump.example.org/pairing/");

            Assert.That(() => new EndpointAdvertiser(discovery, endpoint, Options()),
                        Throws.ArgumentException.With.Message.Contains(".local"));

            // A non-positive check interval is rejected as well.
            Assert.That(() => new EndpointAdvertiser(discovery, CreateEndpoint(), Options(CheckInterval: TimeSpan.Zero)),
                        Throws.ArgumentException);

        }

        #endregion

        #region HostNameOption_OverridesTheHostOfThePairingUrl()

        [Test]
        public async Task HostNameOption_OverridesTheHostOfThePairingUrl()
        {

            await using var discovery   = await StartDiscoveryAsync();
            await using var browser     = await discovery.BrowseAsync(EnergyManagementRole.RM);

            // The pairing server is addressed by an IP address, but announced as "hp.local".
            var endpoint = CreateEndpoint("http://192.168.1.10:8443/pairing/");
            endpoint.AddNode(Description(EnergyManagementRole.RM));

            Assert.That(() => new EndpointAdvertiser(discovery, endpoint, Options()),
                        Throws.ArgumentException);

            await using var advertiser  = new EndpointAdvertiser(
                                              discovery,
                                              endpoint,
                                              Options(OnlyWhenReadyForPairing:  false,
                                                      HostName:                 DomainName.Parse("hp.local"))
                                          );

            await advertiser.StartAsync();

            var current = advertiser.CurrentAdvertisement;

            Assert.That(current, Is.Not.Null);
            Assert.Multiple(() => {
                Assert.That(current!.HostName.FullName,                 Is.EqualTo("hp.local."));
                Assert.That(current.InstanceName.FullName,              Is.EqualTo("hp._s2connect._tcp.local."));
                Assert.That(current.Port.ToUInt16(),                    Is.EqualTo((UInt16) 8443));
                Assert.That(current.TXT.PairingUrl?.Value,              Is.EqualTo("http://192.168.1.10:8443/pairing/"), "the TXT record keeps the pairing URL as is");
                Assert.That(browser.Endpoints,                          Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints[0].HostName.FullName,     Is.EqualTo("hp.local."));
                Assert.That(browser.Endpoints[0].PairingUrl?.Value,     Is.EqualTo("http://192.168.1.10:8443/pairing/"));
            });

        }

        #endregion

        #region LongPollingUrlOption_IsAnnouncedInTheTXTRecord()

        [Test]
        public async Task LongPollingUrlOption_IsAnnouncedInTheTXTRecord()
        {

            await using var discovery   = await StartDiscoveryAsync();
            await using var browser     = await discovery.BrowseAsync(EnergyManagementRole.RM);

            var longPollingUrl = S2BaseURL.Parse("http://hp.local:8443/longpolling/", AllowHTTP: true);
            var endpoint       = CreateEndpoint();
            var node           = endpoint.AddNode(Description(EnergyManagementRole.RM));

            await using var advertiser  = new EndpointAdvertiser(discovery, endpoint, Options(LongPollingUrl: longPollingUrl));

            await advertiser.StartAsync();

            node.SetStaticPairingToken(StaticToken);
            await advertiser.RefreshAsync();

            var current = advertiser.CurrentAdvertisement;

            Assert.That(current, Is.Not.Null);
            Assert.Multiple(() => {
                Assert.That(current!.TXT.PairingUrl,                     Is.EqualTo(endpoint.PairingUrl));
                Assert.That(current.TXT.LongPollingUrl,                  Is.EqualTo(longPollingUrl));
                Assert.That(current.TXT.HasLongPollingUrl,               Is.True);
                Assert.That(current.TXT.ToStrings(),                     Does.Contain("longpollingUrl=http://hp.local:8443/longpolling/"));
                Assert.That(browser.Endpoints,                           Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints[0].LongPollingUrl,         Is.EqualTo(longPollingUrl));
            });

        }

        #endregion

        #region Timer_AdvertisesAndWithdrawsWithoutAnExplicitRefresh()

        [Test]
        public async Task Timer_AdvertisesAndWithdrawsWithoutAnExplicitRefresh()
        {

            await using var discovery   = await StartDiscoveryAsync();
            await using var browser     = await discovery.BrowseAsync(EnergyManagementRole.RM);

            var endpoint = CreateEndpoint();
            var node     = endpoint.AddNode(Description(EnergyManagementRole.RM));

            await using var advertiser  = new EndpointAdvertiser(discovery, endpoint, Options());
            var             recorder    = new Recorder(advertiser);

            await advertiser.StartAsync();

            Assert.That(advertiser.IsAdvertised, Is.False);

            // The periodic check (every 20 ms) picks up the new token...
            node.SetStaticPairingToken(StaticToken);

            await DiscoverySmokeTests.WaitUntil(() => recorder.Advertised.Count == 1, TimeSpan.FromSeconds(1));

            Assert.Multiple(() => {
                Assert.That(advertiser.IsAdvertised,  Is.True);
                Assert.That(browser.Endpoints,        Has.Count.EqualTo(1));
            });

            // ... and its removal.
            node.ClearPairingToken();

            await DiscoverySmokeTests.WaitUntil(() => recorder.Withdrawn.Count == 1, TimeSpan.FromSeconds(1));

            Assert.Multiple(() => {
                Assert.That(advertiser.IsAdvertised,  Is.False);
                Assert.That(recorder.Advertised,      Has.Count.EqualTo(1));
                Assert.That(browser.Endpoints,        Is.Empty);
            });

        }

        #endregion

    }

}

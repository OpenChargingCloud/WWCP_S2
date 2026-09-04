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

using Microsoft.Extensions.Logging;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// An in-process service discovery without a network: every advertisement is visible to
    /// every browser of the same instance at once, and the DNS client resolves the advertised
    /// host names to the advertised addresses (127.0.0.1 by default). Share one instance
    /// between the endpoints of a test or simulation.
    /// </summary>
    public sealed class InMemoryServiceDiscovery : IServiceDiscovery
    {

        #region Data

        private readonly Lock                  stateLock        = new();
        private readonly List<Handle>          advertisements   = [];
        private readonly List<Browser>         browsers         = [];
        private readonly Dictionary<String, IIPAddress[]>  hosts  = new (StringComparer.OrdinalIgnoreCase);
        private readonly ILogger?              logger;
        private          Boolean               isRunning;
        private          Boolean               isDisposed;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the service discovery is running.
        /// </summary>
        public Boolean                    IsRunning
            => isRunning && !isDisposed;

        /// <summary>
        /// The DNS client resolving advertised (and manually added) host names.
        /// </summary>
        public IDNSClient                 DNSClient          { get; }

        /// <summary>
        /// The time provider.
        /// </summary>
        public TimeProvider               TimeProvider       { get; }

        /// <summary>
        /// The address used for advertisements without explicit addresses (default: 127.0.0.1).
        /// </summary>
        public IIPAddress                 DefaultAddress     { get; }

        /// <summary>
        /// The current advertisements.
        /// </summary>
        public IReadOnlyList<IServiceAdvertisementHandle>  Advertisements
        {
            get
            {
                lock (stateLock)
                    return [.. advertisements];
            }
        }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new in-memory service discovery.
        /// </summary>
        /// <param name="TimeProvider">An optional time provider.</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        /// <param name="DefaultAddress">The address used for advertisements without explicit addresses (default: 127.0.0.1).</param>
        public InMemoryServiceDiscovery(TimeProvider?    TimeProvider     = null,
                                        ILoggerFactory?  LoggerFactory    = null,
                                        IIPAddress?      DefaultAddress   = null)
        {

            this.TimeProvider    = TimeProvider   ?? System.TimeProvider.System;
            this.logger          = LoggerFactory?.CreateLogger<InMemoryServiceDiscovery>();
            this.DefaultAddress  = DefaultAddress ?? IPv4Address.Localhost;
            this.DNSClient       = new InMemoryDNSClient(this);

        }

        #endregion


        #region StartAsync / StopAsync

        /// <summary>
        /// Start the service discovery.
        /// </summary>
        public Task StartAsync(CancellationToken CancellationToken = default)
        {

            ObjectDisposedException.ThrowIf(isDisposed, this);

            isRunning = true;
            return Task.CompletedTask;

        }

        /// <summary>
        /// Stop the service discovery, withdrawing every advertisement.
        /// </summary>
        public async Task StopAsync(CancellationToken CancellationToken = default)
        {

            if (!isRunning)
                return;

            foreach (var handle in Advertisements)
                await handle.WithdrawAsync(CancellationToken).ConfigureAwait(false);

            Browser[] stopping;
            lock (stateLock)
                stopping = [.. browsers];

            foreach (var browser in stopping)
                await browser.DisposeAsync().ConfigureAwait(false);

            isRunning = false;

        }

        #endregion

        #region AddHost(HostName, Addresses)

        /// <summary>
        /// Let the DNS client resolve the given host name to the given addresses.
        /// </summary>
        /// <param name="HostName">A host name.</param>
        /// <param name="Addresses">The addresses of the host.</param>
        public void AddHost(DomainName               HostName,
                            params IIPAddress[]      Addresses)
        {

            ArgumentNullException.ThrowIfNull(HostName);

            lock (stateLock)
                hosts[Key(HostName.FullName)] = Addresses;

        }

        #endregion

        #region AdvertiseAsync(Advertisement, CancellationToken = default)

        /// <summary>
        /// Advertise the given endpoint to every browser of this instance.
        /// </summary>
        public async Task<IServiceAdvertisementHandle> AdvertiseAsync(S2ServiceAdvertisement  Advertisement,
                                                                      CancellationToken       CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Advertisement);

            if (!IsRunning)
                throw new InvalidOperationException("The in-memory service discovery is not running!");

            var handle = new Handle(this, Advertisement);

            lock (stateLock)
                advertisements.Add(handle);

            await NotifyAsync(handle, Change.Appeared, CancellationToken).ConfigureAwait(false);

            return handle;

        }

        #endregion

        #region BrowseAsync(Role = null, CancellationToken = default)

        /// <summary>
        /// Browse for endpoints, optionally only those hosting nodes of the given role.
        /// </summary>
        public Task<IS2EndpointBrowser> BrowseAsync(EnergyManagementRole?  Role                = null,
                                                    CancellationToken      CancellationToken   = default)
        {

            if (!IsRunning)
                throw new InvalidOperationException("The in-memory service discovery is not running!");

            var browser = new Browser(this, Role);

            lock (stateLock)
            {

                browsers.Add(browser);

                foreach (var handle in advertisements.Where(handle => handle.IsPublished && Matches(handle.Advertisement, Role)))
                    browser.Seed(handle.CurrentEndpoint);

            }

            return Task.FromResult<IS2EndpointBrowser>(browser);

        }

        #endregion


        #region (private) NotifyAsync / Matches / ToEndpoint / Key

        private enum Change { Appeared, Updated, Disappeared }

        private async Task NotifyAsync(Handle             Handle,
                                       Change             Change,
                                       CancellationToken  CancellationToken)
        {

            Browser[] targets;

            lock (stateLock)
                targets = [.. browsers];

            var endpoint = Handle.CurrentEndpoint;

            foreach (var browser in targets)
            {

                var matches = Matches(Handle.Advertisement, browser.Role);

                switch (Change)
                {

                    case Change.Appeared when matches:
                        await browser.AppearedAsync(endpoint, CancellationToken).ConfigureAwait(false);
                        break;

                    case Change.Updated:
                        if (matches)
                            await browser.UpdatedAsync(endpoint, CancellationToken).ConfigureAwait(false);
                        else
                            await browser.DisappearedAsync(endpoint, CancellationToken).ConfigureAwait(false);
                        break;

                    case Change.Disappeared:
                        await browser.DisappearedAsync(endpoint, CancellationToken).ConfigureAwait(false);
                        break;

                }

            }

        }

        private static Boolean Matches(S2ServiceAdvertisement  Advertisement,
                                       EnergyManagementRole?   Role)

            => !Role.HasValue || Advertisement.Roles.Contains(Role.Value);

        private S2DiscoveredEndpoint ToEndpoint(S2ServiceAdvertisement Advertisement)

            => new (Advertisement.InstanceName,
                    Advertisement.HostName,
                    Advertisement.Port,
                    Advertisement.Addresses ?? [ DefaultAddress ],
                    Advertisement.TXT,
                    Advertisement.Roles.Contains(EnergyManagementRole.CEM),
                    Advertisement.Roles.Contains(EnergyManagementRole.RM),
                    TimeProvider.GetUtcNow());

        private static String Key(String Name)
            => Name.TrimEnd('.').ToLowerInvariant();

        private IIPAddress[] Resolve(String Name)
        {

            var key = Key(Name);

            lock (stateLock)
            {

                if (hosts.TryGetValue(key, out var addresses))
                    return addresses;

                return [.. advertisements.
                               Where     (handle => handle.IsPublished && Key(handle.Advertisement.HostName.FullName) == key).
                               SelectMany(handle => handle.Advertisement.Addresses ?? [ DefaultAddress ]).
                               Distinct()];

            }

        }

        #endregion


        #region (class) Handle

        private sealed class Handle(InMemoryServiceDiscovery  Discovery,
                                    S2ServiceAdvertisement    Advertisement) : IServiceAdvertisementHandle
        {

            private readonly InMemoryServiceDiscovery  discovery      = Discovery;
            private          S2ServiceAdvertisement    advertisement  = Advertisement;
            private          S2DiscoveredEndpoint      endpoint       = Discovery.ToEndpoint(Advertisement);
            private          Boolean                   isPublished    = true;

            public S2ServiceAdvertisement  Advertisement
                => advertisement;

            public S2DiscoveredEndpoint    CurrentEndpoint
                => endpoint;

            public Boolean                 IsPublished
                => isPublished;

            public Boolean                 HasConflict
                => false;

            public event OnAdvertisementConflictDelegate?  OnConflict;

            public async Task UpdateAsync(S2ServiceAdvertisement  Advertisement,
                                          CancellationToken       CancellationToken   = default)
            {

                ArgumentNullException.ThrowIfNull(Advertisement);

                if (!isPublished)
                    throw new InvalidOperationException("The advertisement was withdrawn!");

                if (!Advertisement.HostName.Equals(advertisement.HostName))
                    throw new ArgumentException("The host name of an advertisement cannot change; withdraw and advertise again!", nameof(Advertisement));

                advertisement  = Advertisement;
                endpoint       = discovery.ToEndpoint(Advertisement);

                await discovery.NotifyAsync(this, Change.Updated, CancellationToken).ConfigureAwait(false);

            }

            public async Task WithdrawAsync(CancellationToken CancellationToken = default)
            {

                if (!isPublished)
                    return;

                isPublished = false;

                lock (discovery.stateLock)
                    discovery.advertisements.Remove(this);

                await discovery.NotifyAsync(this, Change.Disappeared, CancellationToken).ConfigureAwait(false);

                // The event is part of the contract even though this test double never raises it.
                _ = OnConflict;

            }

            public ValueTask DisposeAsync()
                => new (WithdrawAsync());

            public override String ToString()
                => $"{advertisement}{(isPublished ? "" : " (withdrawn)")}";

        }

        #endregion

        #region (class) Browser

        private sealed class Browser(InMemoryServiceDiscovery  Discovery,
                                     EnergyManagementRole?     Role) : IS2EndpointBrowser
        {

            private readonly InMemoryServiceDiscovery                      discovery  = Discovery;
            private readonly EnergyManagementRole?                         role       = Role;
            private readonly Dictionary<String, S2DiscoveredEndpoint>      endpoints  = [];
            private          Boolean                                       isDisposed;

            public EnergyManagementRole?  Role
                => role;

            public TimeProvider           TimeProvider
                => discovery.TimeProvider;

            public IReadOnlyList<S2DiscoveredEndpoint>  Endpoints
            {
                get
                {
                    lock (endpoints)
                        return [.. endpoints.Values];
                }
            }

            public event OnS2EndpointDelegate?         OnEndpointAppeared;
            public event OnS2EndpointDelegate?         OnEndpointUpdated;
            public event OnS2EndpointDelegate?         OnEndpointDisappeared;
            public event OnInvalidS2EndpointDelegate?  OnInvalidEndpoint;

            internal void Seed(S2DiscoveredEndpoint Endpoint)
            {
                lock (endpoints)
                    endpoints[Key(Endpoint.InstanceName.FullName)] = Endpoint;
            }

            internal Task AppearedAsync(S2DiscoveredEndpoint Endpoint, CancellationToken CancellationToken)
            {

                if (isDisposed)
                    return Task.CompletedTask;

                lock (endpoints)
                    endpoints[Key(Endpoint.InstanceName.FullName)] = Endpoint;

                return OnEndpointAppeared.InvokeAllAsync(
                           handler => handler(discovery.TimeProvider.GetUtcNow(), this, Endpoint, CancellationToken),
                           discovery.logger
                       );

            }

            internal Task UpdatedAsync(S2DiscoveredEndpoint Endpoint, CancellationToken CancellationToken)
            {

                if (isDisposed)
                    return Task.CompletedTask;

                Boolean known;

                lock (endpoints)
                {
                    known = endpoints.ContainsKey(Key(Endpoint.InstanceName.FullName));
                    endpoints[Key(Endpoint.InstanceName.FullName)] = Endpoint;
                }

                return known
                           ? OnEndpointUpdated.InvokeAllAsync(
                                 handler => handler(discovery.TimeProvider.GetUtcNow(), this, Endpoint, CancellationToken),
                                 discovery.logger
                             )
                           : OnEndpointAppeared.InvokeAllAsync(
                                 handler => handler(discovery.TimeProvider.GetUtcNow(), this, Endpoint, CancellationToken),
                                 discovery.logger
                             );

            }

            internal Task DisappearedAsync(S2DiscoveredEndpoint Endpoint, CancellationToken CancellationToken)
            {

                if (isDisposed)
                    return Task.CompletedTask;

                Boolean known;

                lock (endpoints)
                    known = endpoints.Remove(Key(Endpoint.InstanceName.FullName));

                if (!known)
                    return Task.CompletedTask;

                return OnEndpointDisappeared.InvokeAllAsync(
                           handler => handler(discovery.TimeProvider.GetUtcNow(), this, Endpoint, CancellationToken),
                           discovery.logger
                       );

            }

            public ValueTask DisposeAsync()
            {

                if (isDisposed)
                    return ValueTask.CompletedTask;

                isDisposed = true;

                lock (discovery.stateLock)
                    discovery.browsers.Remove(this);

                // The event is part of the contract even though this test double never raises it.
                _ = OnInvalidEndpoint;

                return ValueTask.CompletedTask;

            }

            public override String ToString()
                => $"in-memory S2 endpoint browser{(Role.HasValue ? $" for {Role}" : "")} ({Endpoints.Count} endpoint(s))";

        }

        #endregion

        #region (class) InMemoryDNSClient

        private sealed class InMemoryDNSClient(InMemoryServiceDiscovery Discovery) : IDNSClient
        {

            private readonly InMemoryServiceDiscovery discovery = Discovery;

            public Task<DNSInfo> Query(DomainName                           DomainName,
                                       IEnumerable<DNSResourceRecordTypes>  ResourceRecordTypes,
                                       TimeSpan?                            Timeout             = null,
                                       Boolean?                             RecursionDesired    = true,
                                       Boolean?                             ForceUpdate         = false,
                                       CancellationToken                    CancellationToken   = default)

                => Query(DNSServiceName.Parse(DomainName.FullName), ResourceRecordTypes, Timeout, RecursionDesired, ForceUpdate, CancellationToken);

            public Task<DNSInfo> Query(DNSServiceName                       DNSServiceName,
                                       IEnumerable<DNSResourceRecordTypes>  ResourceRecordTypes,
                                       TimeSpan?                            Timeout             = null,
                                       Boolean?                             RecursionDesired    = true,
                                       Boolean?                             ForceUpdate         = false,
                                       CancellationToken                    CancellationToken   = default)
            {

                var stopwatch  = Stopwatch.StartNew();
                var types      = ResourceRecordTypes.ToArray();
                var addresses  = discovery.Resolve(DNSServiceName.FullName);
                var answers    = new List<IDNSResourceRecord>();

                foreach (var address in addresses)
                {

                    if (address is IPv4Address ipv4Address &&
                        (types.Length == 0 || types.Contains(DNSResourceRecordTypes.A) || types.Contains(DNSResourceRecordTypes.Any)))
                    {
                        answers.Add(new A(DNSServiceName, DNSQueryClasses.IN, TimeSpan.FromSeconds(120), ipv4Address));
                    }

                    else if (address is IPv6Address ipv6Address &&
                             (types.Length == 0 || types.Contains(DNSResourceRecordTypes.AAAA) || types.Contains(DNSResourceRecordTypes.Any)))
                    {
                        answers.Add(new AAAA(DNSServiceName, DNSQueryClasses.IN, TimeSpan.FromSeconds(120), ipv6Address));
                    }

                }

                return Task.FromResult(
                           new DNSInfo(
                               new DNSServerConfig(IPv4Address.Localhost, IPPort.DNS, DNSTransport.UDP, Timeout),
                               0,
                               true,
                               false,
                               false,
                               false,
                               addresses.Length > 0 ? DNSResponseCodes.NoError : DNSResponseCodes.NameError,
                               answers,
                               [],
                               [],
                               true,
                               false,
                               Timeout ?? TimeSpan.Zero,
                               stopwatch.Elapsed
                           )
                       );

            }

            public void Dispose()
            { }

            public ValueTask DisposeAsync()
                => ValueTask.CompletedTask;

            public override String ToString()
                => "in-memory DNS client of the in-memory service discovery";

        }

        #endregion


        #region DisposeAsync()

        /// <summary>
        /// Stop the service discovery.
        /// </summary>
        public async ValueTask DisposeAsync()
        {

            if (isDisposed)
                return;

            await StopAsync().ConfigureAwait(false);

            isDisposed = true;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"in-memory service discovery ({Advertisements.Count} advertisement(s))";

        #endregion

    }

}

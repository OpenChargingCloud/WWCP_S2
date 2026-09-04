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

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// Options of the DNS-SD service discovery.
    /// </summary>
    public sealed class DNSSDServiceDiscoveryOptions
    {

        /// <summary>
        /// An optional Multicast DNS transport (default: a UDP transport with the given transport options).
        /// </summary>
        public IMulticastDNSTransport?            Transport                 { get; init; }

        /// <summary>
        /// Optional options of the default UDP transport.
        /// </summary>
        public UDPMulticastDNSTransportOptions?   TransportOptions          { get; init; }

        /// <summary>
        /// Whether the transport is stopped and disposed together with the service discovery (default: true).
        /// </summary>
        public Boolean                            OwnsTransport             { get; init; } = true;

        /// <summary>
        /// Optional options of the Multicast DNS responder.
        /// </summary>
        public MulticastDNSResponderOptions?      ResponderOptions          { get; init; }

        /// <summary>
        /// Optional options of the Multicast DNS client.
        /// </summary>
        public MulticastDNSClientOptions?         ClientOptions             { get; init; }

        /// <summary>
        /// An optional unicast DNS client for every name outside ".local" (default: Hermod's DNSClient).
        /// </summary>
        public IDNSClient?                        UnicastDNSClient          { get; init; }

        /// <summary>
        /// The time-to-live of the SRV, A and AAAA records (default: 120 s, RFC 6762 §10).
        /// </summary>
        public TimeSpan                           HostRecordTimeToLive      { get; init; } = MulticastDNS.HostRecordTimeToLive;

        /// <summary>
        /// The time-to-live of the PTR and TXT records (default: 75 minutes, RFC 6762 §10).
        /// </summary>
        public TimeSpan                           SharedRecordTimeToLive    { get; init; } = MulticastDNS.SharedRecordTimeToLive;

        /// <summary>
        /// The parser options for the TXT records of discovered endpoints (AllowInsecureURLs accepts "http://" URLs).
        /// </summary>
        public S2ParserOptions                    ParserOptions             { get; init; } = S2ParserOptions.Default;

    }


    /// <summary>
    /// The DNS-SD service discovery of S2 Connect on Hermod's Multicast DNS (S2 Connect 1.0.0,
    /// "DNS-SD based discovery"): one shared transport, a responder publishing the local
    /// endpoints, a client browsing "_s2connect._tcp.local" and its subtypes, and a hybrid DNS
    /// client resolving ".local" host names via Multicast DNS and everything else via unicast DNS.
    /// </summary>
    public sealed class DNSSDServiceDiscovery : IServiceDiscovery
    {

        #region Data

        private readonly Lock                    stateLock        = new();
        private readonly List<DNSSDAdvertiser>   advertisements   = [];
        private readonly ILogger?                logger;
        private readonly IDNSClient?             ownedUnicastClient;
        private          Boolean                 isRunning;
        private          Boolean                 isDisposed;

        #endregion

        #region Properties

        /// <summary>
        /// The options of this service discovery.
        /// </summary>
        public DNSSDServiceDiscoveryOptions  Options          { get; }

        /// <summary>
        /// The Multicast DNS transport.
        /// </summary>
        public IMulticastDNSTransport        Transport        { get; }

        /// <summary>
        /// The Multicast DNS responder publishing the local endpoints.
        /// </summary>
        public MulticastDNSResponder         Responder        { get; }

        /// <summary>
        /// The Multicast DNS client browsing for remote endpoints and resolving ".local" names.
        /// </summary>
        public MulticastDNSClient            Client           { get; }

        /// <summary>
        /// The hybrid DNS client (".local" via Multicast DNS, everything else via unicast DNS).
        /// </summary>
        public IDNSClient                    DNSClient        { get; }

        /// <summary>
        /// The time provider.
        /// </summary>
        public TimeProvider                  TimeProvider     { get; }

        /// <summary>
        /// The logger factory, when given.
        /// </summary>
        public ILoggerFactory?               LoggerFactory    { get; }

        /// <summary>
        /// Whether the service discovery is running.
        /// </summary>
        public Boolean                       IsRunning
            => isRunning && !isDisposed;

        /// <summary>
        /// The addresses of this host announced by advertisements without explicit addresses.
        /// </summary>
        public IReadOnlyList<IIPAddress>     LocalAddresses
            => Transport.LocalAddresses;

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
        /// Create a new DNS-SD service discovery.
        /// </summary>
        /// <param name="Options">Optional options.</param>
        /// <param name="TimeProvider">An optional time provider.</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        public DNSSDServiceDiscovery(DNSSDServiceDiscoveryOptions?  Options         = null,
                                     TimeProvider?                  TimeProvider    = null,
                                     ILoggerFactory?                LoggerFactory   = null)
        {

            this.Options        = Options      ?? new DNSSDServiceDiscoveryOptions();
            this.TimeProvider   = TimeProvider ?? System.TimeProvider.System;
            this.LoggerFactory  = LoggerFactory;
            this.logger         = LoggerFactory?.CreateLogger<DNSSDServiceDiscovery>();

            this.Transport      = this.Options.Transport ?? new UDPMulticastDNSTransport(this.Options.TransportOptions, this.TimeProvider, LoggerFactory);
            this.Responder      = new MulticastDNSResponder(this.Transport, this.Options.ResponderOptions, this.TimeProvider, LoggerFactory);
            this.Client         = new MulticastDNSClient   (this.Transport, this.Options.ClientOptions,    this.TimeProvider, LoggerFactory);

            if (this.Options.UnicastDNSClient is null)
                this.ownedUnicastClient = new DNSClient();

            this.DNSClient      = new HybridDNSClient(this.Client, this.Options.UnicastDNSClient ?? this.ownedUnicastClient);

        }

        #endregion


        #region StartAsync(CancellationToken = default)

        /// <summary>
        /// Start the transport, the responder and the client.
        /// </summary>
        public async Task StartAsync(CancellationToken CancellationToken = default)
        {

            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (isRunning)
                return;

            if (!Transport.IsRunning)
                await Transport.StartAsync(CancellationToken).ConfigureAwait(false);

            await Responder.StartAsync(CancellationToken).ConfigureAwait(false);
            await Client.   StartAsync(CancellationToken).ConfigureAwait(false);

            isRunning = true;

            logger?.LogInformation("S2 DNS-SD service discovery started on {Transport} announcing {Addresses}",
                                   Transport,
                                   String.Join(", ", Transport.LocalAddresses));

        }

        #endregion

        #region StopAsync(CancellationToken = default)

        /// <summary>
        /// Withdraw every advertisement (goodbye packets) and stop the client, the responder
        /// and (when owned) the transport.
        /// </summary>
        public async Task StopAsync(CancellationToken CancellationToken = default)
        {

            if (!isRunning)
                return;

            foreach (var advertisement in Advertisements)
                await advertisement.WithdrawAsync(CancellationToken).ConfigureAwait(false);

            await Client.   StopAsync(CancellationToken).ConfigureAwait(false);
            await Responder.StopAsync(CancellationToken).ConfigureAwait(false);

            if (Options.OwnsTransport)
                await Transport.StopAsync(CancellationToken).ConfigureAwait(false);

            isRunning = false;

        }

        #endregion

        #region AdvertiseAsync(Advertisement, CancellationToken = default)

        /// <summary>
        /// Publish the DNS-SD records of the given advertisement (probing the unique names first).
        /// The returned handle is not published when another host already owns the host name
        /// (HasConflict); an advertisement without explicit addresses announces the addresses of
        /// the transport.
        /// </summary>
        public async Task<IServiceAdvertisementHandle> AdvertiseAsync(S2ServiceAdvertisement  Advertisement,
                                                                      CancellationToken       CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Advertisement);

            if (!IsRunning)
                throw new InvalidOperationException("The DNS-SD service discovery is not running!");

            var addresses = Advertisement.Addresses ?? Transport.LocalAddresses;

            if (addresses.Count == 0)
                throw new InvalidOperationException("The transport has no local addresses to announce; give explicit addresses!");

            var handle = new DNSSDAdvertiser(this, Advertisement, addresses, logger);

            lock (stateLock)
                advertisements.Add(handle);

            try
            {
                await handle.PublishAsync(CancellationToken).ConfigureAwait(false);
            }
            catch
            {
                lock (stateLock)
                    advertisements.Remove(handle);
                throw;
            }

            return handle;

        }

        #endregion

        #region BrowseAsync(Role = null, CancellationToken = default)

        /// <summary>
        /// Browse for S2 Connect endpoints: without a role via the service type and both subtypes
        /// (so that the roles of every endpoint become known), with a role via its subtype only.
        /// </summary>
        public async Task<IS2EndpointBrowser> BrowseAsync(EnergyManagementRole?  Role                = null,
                                                          CancellationToken      CancellationToken   = default)
        {

            if (!IsRunning)
                throw new InvalidOperationException("The DNS-SD service discovery is not running!");

            var browser = new DNSSDBrowser(this, Role, logger);

            await browser.StartAsync(CancellationToken).ConfigureAwait(false);

            return browser;

        }

        #endregion


        #region (internal) Remove(Advertiser)

        internal void Remove(DNSSDAdvertiser Advertiser)
        {
            lock (stateLock)
                advertisements.Remove(Advertiser);
        }

        #endregion


        #region DisposeAsync()

        /// <summary>
        /// Stop the service discovery and dispose its components.
        /// </summary>
        public async ValueTask DisposeAsync()
        {

            if (isDisposed)
                return;

            await StopAsync().ConfigureAwait(false);

            isDisposed = true;

            await Client.   DisposeAsync().ConfigureAwait(false);
            await Responder.DisposeAsync().ConfigureAwait(false);

            if (Options.OwnsTransport)
                await Transport.DisposeAsync().ConfigureAwait(false);

            if (ownedUnicastClient is not null)
                await ownedUnicastClient.DisposeAsync().ConfigureAwait(false);

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"S2 DNS-SD service discovery ({Advertisements.Count} advertisement(s)) on {Transport}";

        #endregion

    }


    /// <summary>
    /// A DNS-SD advertisement of a local S2 Connect endpoint: a publication of the Multicast DNS
    /// responder whose conflicts are reported through <see cref="OnConflict"/>.
    /// </summary>
    public sealed class DNSSDAdvertiser : IServiceAdvertisementHandle
    {

        #region Data

        private readonly DNSSDServiceDiscovery      discovery;
        private readonly ILogger?                   logger;
        private          MulticastDNSPublication?   publication;
        private          Boolean                    subscribed;

        #endregion

        #region Properties

        /// <summary>
        /// The advertisement.
        /// </summary>
        public S2ServiceAdvertisement     Advertisement    { get; private set; }

        /// <summary>
        /// The announced addresses of the host.
        /// </summary>
        public IReadOnlyList<IIPAddress>  Addresses        { get; }

        /// <summary>
        /// The publication of the Multicast DNS responder, once published.
        /// </summary>
        public MulticastDNSPublication?   Publication
            => publication;

        /// <summary>
        /// Whether the records are currently answered.
        /// </summary>
        public Boolean                    IsPublished
            => publication?.IsActive == true;

        /// <summary>
        /// Whether another host claimed the host or instance name.
        /// </summary>
        public Boolean                    HasConflict
            => publication?.State == MulticastDNSPublicationState.Conflict;

        #endregion

        #region Events

        /// <summary>
        /// An event fired whenever another host claimed the host or instance name.
        /// </summary>
        public event OnAdvertisementConflictDelegate?  OnConflict;

        #endregion

        #region Constructor(s)

        internal DNSSDAdvertiser(DNSSDServiceDiscovery      Discovery,
                                 S2ServiceAdvertisement     Advertisement,
                                 IReadOnlyList<IIPAddress>  Addresses,
                                 ILogger?                   Logger)
        {

            this.discovery      = Discovery;
            this.Advertisement  = Advertisement;
            this.Addresses      = Addresses;
            this.logger         = Logger;

        }

        #endregion


        #region (internal) PublishAsync(CancellationToken)

        internal async Task PublishAsync(CancellationToken CancellationToken)
        {

            var records = Advertisement.ToResourceRecords(
                              Addresses,
                              discovery.Options.HostRecordTimeToLive,
                              discovery.Options.SharedRecordTimeToLive
                          );

            discovery.Responder.OnNameConflict += OnNameConflictAsync;
            subscribed = true;

            publication = await discovery.Responder.PublishAsync(records, true, CancellationToken).ConfigureAwait(false);

            if (publication.State == MulticastDNSPublicationState.Conflict)
                logger?.LogWarning("S2 DNS-SD: the name '{Name}' is already used by {Source}: {Record}",
                                   publication.OwnConflictedRecord?.DomainName,
                                   publication.ConflictSource,
                                   publication.ConflictingRecord);

        }

        #endregion

        #region UpdateAsync(Advertisement, CancellationToken = default)

        /// <summary>
        /// Replace the records (same host name) and announce them.
        /// </summary>
        public async Task UpdateAsync(S2ServiceAdvertisement  Advertisement,
                                      CancellationToken       CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Advertisement);

            if (!Advertisement.HostName.Equals(this.Advertisement.HostName))
                throw new ArgumentException("The host name of an advertisement cannot change; withdraw and advertise again!", nameof(Advertisement));

            if (publication is null || !publication.IsActive)
                throw new InvalidOperationException($"The advertisement is not published ({publication?.State.ToString() ?? "pending"})!");

            var records = Advertisement.ToResourceRecords(
                              Advertisement.Addresses ?? Addresses,
                              discovery.Options.HostRecordTimeToLive,
                              discovery.Options.SharedRecordTimeToLive
                          );

            await publication.UpdateAsync(records, CancellationToken).ConfigureAwait(false);

            this.Advertisement = Advertisement;

        }

        #endregion

        #region WithdrawAsync(CancellationToken = default)

        /// <summary>
        /// Withdraw the records (goodbye packet).
        /// </summary>
        public async Task WithdrawAsync(CancellationToken CancellationToken = default)
        {

            if (subscribed)
            {
                discovery.Responder.OnNameConflict -= OnNameConflictAsync;
                subscribed = false;
            }

            discovery.Remove(this);

            if (publication is not null)
                await publication.WithdrawAsync(CancellationToken).ConfigureAwait(false);

        }

        #endregion


        #region (private) OnNameConflictAsync(...)

        private Task OnNameConflictAsync(DateTimeOffset           Timestamp,
                                         MulticastDNSResponder    Sender,
                                         MulticastDNSPublication  Publication,
                                         IDNSResourceRecord       OwnRecord,
                                         IDNSResourceRecord       ConflictingRecord,
                                         IPSocket                 Source,
                                         CancellationToken        CancellationToken)
        {

            if (!ReferenceEquals(Publication, publication))
                return Task.CompletedTask;

            return OnConflict.InvokeAllAsync(
                       handler => handler(Timestamp, this, $"'{OwnRecord.DomainName}' is also claimed by {Source}: {ConflictingRecord}", CancellationToken),
                       logger
                   );

        }

        #endregion


        #region DisposeAsync()

        /// <summary>
        /// Withdraw the advertisement.
        /// </summary>
        public ValueTask DisposeAsync()
            => new (WithdrawAsync());

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"{Advertisement} ({publication?.State.ToString() ?? "pending"})";

        #endregion

    }


    /// <summary>
    /// A DNS-SD browser for S2 Connect endpoints: it combines the Multicast DNS browsers of the
    /// service type "_s2connect._tcp.local" and of the subtypes "_cem" and "_rm" into one list
    /// of endpoints with parsed TXT records and known roles. Only the URLs of the TXT record
    /// are used to reach an endpoint.
    /// </summary>
    public sealed class DNSSDBrowser : IS2EndpointBrowser
    {

        #region Data

        private sealed class Entry
        {
            public MulticastDNSServiceInstance?  Instance     { get; set; }
            public S2DiscoveredEndpoint?         Endpoint     { get; set; }
            public HashSet<String>               Sources      { get; } = new (StringComparer.OrdinalIgnoreCase);
            public String?                       LastError    { get; set; }
        }

        private readonly Lock                                          stateLock   = new();
        private readonly Dictionary<String, Entry>                     entries     = new (StringComparer.OrdinalIgnoreCase);
        private readonly List<MulticastDNSBrowser>                     browsers    = [];
        private readonly DNSSDServiceDiscovery                         discovery;
        private readonly ILogger?                                      logger;
        private          Boolean                                       isDisposed;

        #endregion

        #region Properties

        /// <summary>
        /// The optional role filter of this browser.
        /// </summary>
        public EnergyManagementRole?                Role            { get; }

        /// <summary>
        /// The time provider.
        /// </summary>
        public TimeProvider                         TimeProvider
            => discovery.TimeProvider;

        /// <summary>
        /// The browsed DNS-SD service type and subtype names.
        /// </summary>
        public IReadOnlyList<DNSServiceName>        ServiceTypes    { get; }

        /// <summary>
        /// The currently known endpoints with valid TXT records.
        /// </summary>
        public IReadOnlyList<S2DiscoveredEndpoint>  Endpoints
        {
            get
            {
                lock (stateLock)
                    return [.. entries.Values.Where(entry => entry.Endpoint is not null).Select(entry => entry.Endpoint!)];
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// An event fired whenever an endpoint appeared.
        /// </summary>
        public event OnS2EndpointDelegate?         OnEndpointAppeared;

        /// <summary>
        /// An event fired whenever a known endpoint changed.
        /// </summary>
        public event OnS2EndpointDelegate?         OnEndpointUpdated;

        /// <summary>
        /// An event fired whenever a known endpoint disappeared.
        /// </summary>
        public event OnS2EndpointDelegate?         OnEndpointDisappeared;

        /// <summary>
        /// An event fired whenever a service instance with an invalid S2 Connect TXT record was found.
        /// </summary>
        public event OnInvalidS2EndpointDelegate?  OnInvalidEndpoint;

        #endregion

        #region Constructor(s)

        internal DNSSDBrowser(DNSSDServiceDiscovery  Discovery,
                              EnergyManagementRole?  Role,
                              ILogger?               Logger)
        {

            this.discovery     = Discovery;
            this.Role          = Role;
            this.logger        = Logger;

            this.ServiceTypes  = Role.HasValue
                                     ? [ S2ServiceAdvertisement.SubtypeName(Role.Value) ]
                                     : [ S2ServiceAdvertisement.ServiceTypeName,
                                         S2ServiceAdvertisement.CEMSubtypeName,
                                         S2ServiceAdvertisement.RMSubtypeName ];

        }

        #endregion


        #region (internal) StartAsync(CancellationToken)

        internal async Task StartAsync(CancellationToken CancellationToken)
        {

            foreach (var serviceType in ServiceTypes)
            {

                var browser = await discovery.Client.BrowseAsync(serviceType, CancellationToken).ConfigureAwait(false);

                browser.OnInstanceAdded    += OnInstanceAddedAsync;
                browser.OnInstanceUpdated  += OnInstanceUpdatedAsync;
                browser.OnInstanceRemoved  += OnInstanceRemovedAsync;

                lock (stateLock)
                    browsers.Add(browser);

                // Instances already known to the client are listed silently.
                foreach (var instance in browser.Instances)
                    Apply(instance, serviceType, true, out _, out _, out _);

            }

        }

        #endregion

        #region (private) OnInstanceAddedAsync / OnInstanceUpdatedAsync / OnInstanceRemovedAsync

        private Task OnInstanceAddedAsync  (DateTimeOffset Timestamp, MulticastDNSBrowser Sender, MulticastDNSServiceInstance Instance, CancellationToken CancellationToken)
            => HandleAsync(Instance, Sender.ServiceType, true, CancellationToken);

        private Task OnInstanceUpdatedAsync(DateTimeOffset Timestamp, MulticastDNSBrowser Sender, MulticastDNSServiceInstance Instance, CancellationToken CancellationToken)
            => HandleAsync(Instance, Sender.ServiceType, true, CancellationToken);

        private Task OnInstanceRemovedAsync(DateTimeOffset Timestamp, MulticastDNSBrowser Sender, MulticastDNSServiceInstance Instance, CancellationToken CancellationToken)
            => HandleAsync(Instance, Sender.ServiceType, false, CancellationToken);

        private async Task HandleAsync(MulticastDNSServiceInstance  Instance,
                                       DNSServiceName               ServiceType,
                                       Boolean                      Present,
                                       CancellationToken            CancellationToken)
        {

            if (isDisposed)
                return;

            Apply(Instance, ServiceType, Present, out var appeared, out var updated, out var disappeared, out var invalid);

            var now = discovery.TimeProvider.GetUtcNow();

            if (invalid is not null)
                await OnInvalidEndpoint.InvokeAllAsync(
                          handler => handler(now, this, Instance.InstanceName, invalid, CancellationToken),
                          logger
                      ).ConfigureAwait(false);

            if (disappeared is not null)
                await OnEndpointDisappeared.InvokeAllAsync(
                          handler => handler(now, this, disappeared, CancellationToken),
                          logger
                      ).ConfigureAwait(false);

            if (appeared is not null)
                await OnEndpointAppeared.InvokeAllAsync(
                          handler => handler(now, this, appeared, CancellationToken),
                          logger
                      ).ConfigureAwait(false);

            if (updated is not null)
                await OnEndpointUpdated.InvokeAllAsync(
                          handler => handler(now, this, updated, CancellationToken),
                          logger
                      ).ConfigureAwait(false);

        }

        private void Apply(MulticastDNSServiceInstance  Instance,
                           DNSServiceName               ServiceType,
                           Boolean                      Present,
                           out S2DiscoveredEndpoint?    Appeared,
                           out S2DiscoveredEndpoint?    Updated,
                           out S2DiscoveredEndpoint?    Disappeared)

            => Apply(Instance, ServiceType, Present, out Appeared, out Updated, out Disappeared, out _);

        private void Apply(MulticastDNSServiceInstance  Instance,
                           DNSServiceName               ServiceType,
                           Boolean                      Present,
                           out S2DiscoveredEndpoint?    Appeared,
                           out S2DiscoveredEndpoint?    Updated,
                           out S2DiscoveredEndpoint?    Disappeared,
                           out String?                  Invalid)
        {

            Appeared     = null;
            Updated      = null;
            Disappeared  = null;
            Invalid      = null;

            lock (stateLock)
            {

                var key = Instance.InstanceName.FullName.TrimEnd('.');

                if (!entries.TryGetValue(key, out var entry))
                {

                    if (!Present)
                        return;

                    entry = new Entry();
                    entries.Add(key, entry);

                }

                if (Present)
                {
                    entry.Sources.Add(ServiceType.FullName);
                    entry.Instance = Instance;
                }
                else
                    entry.Sources.Remove(ServiceType.FullName);

                if (entry.Sources.Count == 0)
                {
                    entries.Remove(key);
                    Disappeared = entry.Endpoint;
                    return;
                }

                var instance = entry.Instance;

                if (instance is null || !instance.IsResolved || instance.TXT is null || instance.HostName is null || !instance.Port.HasValue)
                    return;

                if (!S2DNSSDTXTRecord.TryParse(instance.TXT, out var txt, out var error, discovery.Options.ParserOptions))
                {

                    if (entry.LastError != error)
                    {
                        entry.LastError = error;
                        Invalid         = error;
                    }

                    if (entry.Endpoint is not null)
                    {
                        Disappeared     = entry.Endpoint;
                        entry.Endpoint  = null;
                    }

                    return;

                }

                entry.LastError = null;

                var hostsCEM  = Role == EnergyManagementRole.CEM || entry.Sources.Contains(S2ServiceAdvertisement.CEMSubtypeName.FullName)
                                    ? true
                                    : (Boolean?) null;

                var hostsRM   = Role == EnergyManagementRole.RM  || entry.Sources.Contains(S2ServiceAdvertisement.RMSubtypeName.FullName)
                                    ? true
                                    : (Boolean?) null;

                var endpoint  = new S2DiscoveredEndpoint(
                                    instance.InstanceName,
                                    instance.HostName,
                                    instance.Port.Value,
                                    instance.Addresses,
                                    txt,
                                    hostsCEM,
                                    hostsRM,
                                    discovery.TimeProvider.GetUtcNow()
                                );

                if (entry.Endpoint is null)
                    Appeared = endpoint;

                else if (!entry.Endpoint.SameContentAs(endpoint))
                    Updated = endpoint;

                entry.Endpoint = endpoint;

            }

        }

        #endregion


        #region DisposeAsync()

        /// <summary>
        /// Stop browsing.
        /// </summary>
        public async ValueTask DisposeAsync()
        {

            if (isDisposed)
                return;

            isDisposed = true;

            MulticastDNSBrowser[] stopping;

            lock (stateLock)
                stopping = [.. browsers];

            foreach (var browser in stopping)
            {

                browser.OnInstanceAdded    -= OnInstanceAddedAsync;
                browser.OnInstanceUpdated  -= OnInstanceUpdatedAsync;
                browser.OnInstanceRemoved  -= OnInstanceRemovedAsync;

                await browser.DisposeAsync().ConfigureAwait(false);

            }

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"S2 DNS-SD browser{(Role.HasValue ? $" for {Role}" : "")} ({Endpoints.Count} endpoint(s))";

        #endregion

    }

}

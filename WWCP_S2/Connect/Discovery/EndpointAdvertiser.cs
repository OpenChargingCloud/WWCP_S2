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

    #region Options

    /// <summary>
    /// Options of the endpoint advertiser.
    /// </summary>
    public sealed class EndpointAdvertiserOptions
    {

        /// <summary>
        /// How often the readiness for pairing and the hosted nodes are re-evaluated (default: 1 s).
        /// </summary>
        public TimeSpan                  CheckInterval              { get; init; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Whether the endpoint is advertised only while at least one hosted node is ready for
        /// pairing and has a valid own pairing token (default: true, S2 Connect 1.0.0: "An endpoint
        /// should publish its service through DNS-SD once it is ready for pairing, and until it
        /// shuts down"). With false the endpoint is advertised as long as the advertiser runs.
        /// </summary>
        public Boolean                   OnlyWhenReadyForPairing    { get; init; } = true;

        /// <summary>
        /// An optional long-polling URL to announce.
        /// </summary>
        public S2BaseURL?                LongPollingUrl             { get; init; }

        /// <summary>
        /// An optional host name (default: the host of the pairing URL).
        /// </summary>
        public DomainName?               HostName                   { get; init; }

        /// <summary>
        /// An optional port (default: the port of the pairing URL).
        /// </summary>
        public IPPort?                   Port                       { get; init; }

        /// <summary>
        /// Optional explicit addresses of the host (default: the addresses of the network interfaces).
        /// </summary>
        public IEnumerable<IIPAddress>?  Addresses                  { get; init; }

    }

    #endregion

    #region Delegates

    /// <summary>
    /// A delegate called whenever the endpoint was advertised, its advertisement updated or withdrawn.
    /// </summary>
    /// <param name="Timestamp">The time of the event.</param>
    /// <param name="Sender">The endpoint advertiser.</param>
    /// <param name="Advertisement">The advertisement.</param>
    /// <param name="CancellationToken">A token to cancel the processing.</param>
    public delegate Task OnEndpointAdvertisementDelegate(DateTimeOffset          Timestamp,
                                                         EndpointAdvertiser      Sender,
                                                         S2ServiceAdvertisement  Advertisement,
                                                         CancellationToken       CancellationToken);

    /// <summary>
    /// A delegate called whenever another host claimed the name of the endpoint.
    /// </summary>
    /// <param name="Timestamp">The time of the event.</param>
    /// <param name="Sender">The endpoint advertiser.</param>
    /// <param name="Description">A description of the conflict.</param>
    /// <param name="CancellationToken">A token to cancel the processing.</param>
    public delegate Task OnEndpointAdvertisementConflictDelegate(DateTimeOffset      Timestamp,
                                                                 EndpointAdvertiser  Sender,
                                                                 String              Description,
                                                                 CancellationToken   CancellationToken);

    #endregion


    /// <summary>
    /// Advertises a local LAN endpoint through a service discovery while it is ready for pairing
    /// (S2 Connect 1.0.0, "DNS-SD based discovery"): the advertisement is derived from the endpoint
    /// (pairing URL, description, roles of the hosted nodes), published when a hosted node has a
    /// valid pairing token, updated when the description or the roles change and withdrawn when
    /// no node is ready any more or the advertiser stops. A name conflict withdraws the endpoint
    /// and is reported; the advertiser does not retry with another name, because a renamed
    /// LAN endpoint also needs another pairing URL and certificate.
    /// </summary>
    public sealed class EndpointAdvertiser : IAsyncDisposable
    {

        #region Data

        private readonly SemaphoreSlim                 refreshLock   = new(1, 1);
        private readonly ILogger?                      logger;
        private          IServiceAdvertisementHandle?  handle;
        private          ITimer?                       timer;
        private          Boolean                       isRunning;
        private          Boolean                       conflicted;
        private          Boolean                       isDisposed;

        #endregion

        #region Properties

        /// <summary>
        /// The service discovery.
        /// </summary>
        public IServiceDiscovery          Discovery         { get; }

        /// <summary>
        /// The advertised local endpoint.
        /// </summary>
        public LocalEndpoint              Endpoint          { get; }

        /// <summary>
        /// The options.
        /// </summary>
        public EndpointAdvertiserOptions  Options           { get; }

        /// <summary>
        /// The time provider.
        /// </summary>
        public TimeProvider               TimeProvider      { get; }

        /// <summary>
        /// Whether the advertiser is running.
        /// </summary>
        public Boolean                    IsRunning
            => isRunning && !isDisposed;

        /// <summary>
        /// Whether the endpoint is currently advertised.
        /// </summary>
        public Boolean                    IsAdvertised
            => handle?.IsPublished == true;

        /// <summary>
        /// Whether a name conflict stopped the advertising (see RefreshAsync(Force: true)).
        /// </summary>
        public Boolean                    HasConflict
            => conflicted;

        /// <summary>
        /// The current advertisement, when advertised.
        /// </summary>
        public S2ServiceAdvertisement?    CurrentAdvertisement
            => handle?.IsPublished == true ? handle.Advertisement : null;

        /// <summary>
        /// Whether the endpoint should be advertised right now: always, or (by default) only
        /// while a hosted node is ready for pairing and has a valid own pairing token, because
        /// only such a node can complete a pairing that a discovering endpoint initiates.
        /// </summary>
        public Boolean                    ShouldAdvertise
            => !Options.OnlyWhenReadyForPairing ||
               Endpoint.Nodes.Any(node => node.IsReadyForPairing && node.HasValidPairingToken);

        #endregion

        #region Events

        /// <summary>
        /// An event fired whenever the endpoint was advertised or its advertisement updated.
        /// </summary>
        public event OnEndpointAdvertisementDelegate?          OnAdvertised;

        /// <summary>
        /// An event fired whenever the advertisement was withdrawn.
        /// </summary>
        public event OnEndpointAdvertisementDelegate?          OnWithdrawn;

        /// <summary>
        /// An event fired whenever another host claimed the name of the endpoint.
        /// </summary>
        public event OnEndpointAdvertisementConflictDelegate?  OnConflict;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new endpoint advertiser.
        /// </summary>
        /// <param name="Discovery">The service discovery.</param>
        /// <param name="Endpoint">The local LAN endpoint to advertise.</param>
        /// <param name="Options">Optional options.</param>
        /// <param name="TimeProvider">An optional time provider (default: the endpoint's).</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        public EndpointAdvertiser(IServiceDiscovery           Discovery,
                                  LocalEndpoint               Endpoint,
                                  EndpointAdvertiserOptions?  Options         = null,
                                  TimeProvider?               TimeProvider    = null,
                                  ILoggerFactory?             LoggerFactory   = null)
        {

            ArgumentNullException.ThrowIfNull(Discovery);
            ArgumentNullException.ThrowIfNull(Endpoint);

            if (Endpoint.Deployment != Deployment.LAN)
                throw new ArgumentException("Only LAN endpoints are advertised via DNS-SD!", nameof(Endpoint));

            this.Discovery     = Discovery;
            this.Endpoint      = Endpoint;
            this.Options       = Options      ?? new EndpointAdvertiserOptions();
            this.TimeProvider  = TimeProvider ?? Endpoint.TimeProvider;
            this.logger        = LoggerFactory?.CreateLogger<EndpointAdvertiser>();

            if (this.Options.CheckInterval <= TimeSpan.Zero)
                throw new ArgumentException("The check interval must be positive!", nameof(Options));

            // Fail early when the endpoint cannot be advertised at all (e.g. a host name outside ".local").
            _ = BuildAdvertisement();

        }

        #endregion


        #region StartAsync(CancellationToken = default)

        /// <summary>
        /// Evaluate the endpoint now and keep re-evaluating it every check interval.
        /// </summary>
        public async Task StartAsync(CancellationToken CancellationToken = default)
        {

            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (isRunning)
                return;

            isRunning = true;

            await RefreshAsync(false, CancellationToken).ConfigureAwait(false);

            timer = TimeProvider.CreateTimer(
                        _ => _ = RefreshSafelyAsync(),
                        null,
                        Options.CheckInterval,
                        Options.CheckInterval
                    );

        }

        #endregion

        #region StopAsync(CancellationToken = default)

        /// <summary>
        /// Stop re-evaluating and withdraw the advertisement.
        /// </summary>
        public async Task StopAsync(CancellationToken CancellationToken = default)
        {

            if (!isRunning)
                return;

            isRunning = false;

            timer?.Dispose();
            timer = null;

            await refreshLock.WaitAsync(CancellationToken).ConfigureAwait(false);

            try
            {
                await WithdrawAsync(CancellationToken).ConfigureAwait(false);
            }
            finally
            {
                refreshLock.Release();
            }

        }

        #endregion

        #region RefreshAsync(Force = false, CancellationToken = default)

        /// <summary>
        /// Re-evaluate the endpoint now: advertise, update or withdraw as needed.
        /// </summary>
        /// <param name="Force">Whether to advertise again after a name conflict.</param>
        /// <param name="CancellationToken">A token to cancel the refresh.</param>
        public async Task RefreshAsync(Boolean            Force               = false,
                                       CancellationToken  CancellationToken   = default)
        {

            if (!isRunning)
                return;

            await refreshLock.WaitAsync(CancellationToken).ConfigureAwait(false);

            try
            {

                // A timer tick that queued up behind StopAsync must not advertise again.
                if (!isRunning)
                    return;

                if (Force)
                    conflicted = false;

                if (!ShouldAdvertise || conflicted)
                {
                    await WithdrawAsync(CancellationToken).ConfigureAwait(false);
                    return;
                }

                var desired = BuildAdvertisement();

                if (handle is null || !handle.IsPublished)
                {

                    if (handle is not null)
                        await WithdrawAsync(CancellationToken).ConfigureAwait(false);

                    var newHandle = await Discovery.AdvertiseAsync(desired, CancellationToken).ConfigureAwait(false);

                    newHandle.OnConflict += OnHandleConflictAsync;
                    handle = newHandle;

                    if (newHandle.HasConflict)
                    {
                        await ConflictAsync("the host or instance name is already used on the link", CancellationToken).ConfigureAwait(false);
                        return;
                    }

                    await OnAdvertised.InvokeAllAsync(
                              handler => handler(TimeProvider.GetUtcNow(), this, desired, CancellationToken),
                              logger
                          ).ConfigureAwait(false);

                    return;

                }

                if (!handle.Advertisement.Equals(desired))
                {

                    await handle.UpdateAsync(desired, CancellationToken).ConfigureAwait(false);

                    await OnAdvertised.InvokeAllAsync(
                              handler => handler(TimeProvider.GetUtcNow(), this, desired, CancellationToken),
                              logger
                          ).ConfigureAwait(false);

                }

            }
            finally
            {
                refreshLock.Release();
            }

        }

        #endregion


        #region (private) BuildAdvertisement()

        private S2ServiceAdvertisement BuildAdvertisement()

            => S2ServiceAdvertisement.FromLocalEndpoint(
                   Endpoint,
                   Options.LongPollingUrl,
                   null,
                   Options.HostName,
                   Options.Port,
                   Options.Addresses
               );

        #endregion

        #region (private) WithdrawAsync(CancellationToken)

        private async Task WithdrawAsync(CancellationToken CancellationToken)
        {

            var current = handle;
            if (current is null)
                return;

            handle = null;
            current.OnConflict -= OnHandleConflictAsync;

            var wasPublished = current.IsPublished;

            await current.WithdrawAsync(CancellationToken).ConfigureAwait(false);

            if (wasPublished)
                await OnWithdrawn.InvokeAllAsync(
                          handler => handler(TimeProvider.GetUtcNow(), this, current.Advertisement, CancellationToken),
                          logger
                      ).ConfigureAwait(false);

        }

        #endregion

        #region (private) ConflictAsync(Description, CancellationToken)

        private async Task ConflictAsync(String             Description,
                                         CancellationToken  CancellationToken)
        {

            conflicted = true;

            logger?.LogWarning("S2 endpoint advertiser: name conflict for '{Endpoint}': {Description}", Endpoint, Description);

            await WithdrawAsync(CancellationToken).ConfigureAwait(false);

            await OnConflict.InvokeAllAsync(
                      handler => handler(TimeProvider.GetUtcNow(), this, Description, CancellationToken),
                      logger
                  ).ConfigureAwait(false);

        }

        #endregion

        #region (private) OnHandleConflictAsync(...)

        private async Task OnHandleConflictAsync(DateTimeOffset               Timestamp,
                                                 IServiceAdvertisementHandle  Sender,
                                                 String                       Description,
                                                 CancellationToken            CancellationToken)
        {

            if (!ReferenceEquals(Sender, handle))
                return;

            await refreshLock.WaitAsync(CancellationToken).ConfigureAwait(false);

            try
            {
                await ConflictAsync(Description, CancellationToken).ConfigureAwait(false);
            }
            finally
            {
                refreshLock.Release();
            }

        }

        #endregion

        #region (private) RefreshSafelyAsync()

        private async Task RefreshSafelyAsync()
        {
            try
            {
                await RefreshAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // The advertiser was disposed while the tick was pending.
            }
            catch (Exception e)
            {
                logger?.LogWarning(e, "S2 endpoint advertiser: refreshing '{Endpoint}' failed", Endpoint);
            }
        }

        #endregion


        #region DisposeAsync()

        /// <summary>
        /// Stop the advertiser and withdraw the advertisement.
        /// </summary>
        public async ValueTask DisposeAsync()
        {

            if (isDisposed)
                return;

            await StopAsync().ConfigureAwait(false);

            isDisposed = true;

            refreshLock.Dispose();

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"advertiser of '{Endpoint}': {(IsAdvertised ? "advertised" : conflicted ? "conflict" : "withdrawn")}";

        #endregion

    }

}

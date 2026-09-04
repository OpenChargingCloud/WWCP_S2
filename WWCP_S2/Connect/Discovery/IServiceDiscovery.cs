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

using org.GraphDefined.Vanaheimr.Hermod.DNS;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    #region Delegates

    /// <summary>
    /// A delegate called whenever a discovered endpoint appeared, changed or disappeared.
    /// </summary>
    /// <param name="Timestamp">The time of the event.</param>
    /// <param name="Sender">The browser.</param>
    /// <param name="Endpoint">The discovered endpoint.</param>
    /// <param name="CancellationToken">A token to cancel the processing.</param>
    public delegate Task OnS2EndpointDelegate              (DateTimeOffset               Timestamp,
                                                            IS2EndpointBrowser           Sender,
                                                            S2DiscoveredEndpoint         Endpoint,
                                                            CancellationToken            CancellationToken);

    /// <summary>
    /// A delegate called whenever a service instance was found whose TXT record is not a valid S2 Connect TXT record.
    /// </summary>
    /// <param name="Timestamp">The time of the event.</param>
    /// <param name="Sender">The browser.</param>
    /// <param name="InstanceName">The service instance name.</param>
    /// <param name="ErrorResponse">Why the TXT record was rejected.</param>
    /// <param name="CancellationToken">A token to cancel the processing.</param>
    public delegate Task OnInvalidS2EndpointDelegate       (DateTimeOffset               Timestamp,
                                                            IS2EndpointBrowser           Sender,
                                                            DNSServiceName               InstanceName,
                                                            String                       ErrorResponse,
                                                            CancellationToken            CancellationToken);

    /// <summary>
    /// A delegate called whenever another host claimed the name of an advertisement.
    /// </summary>
    /// <param name="Timestamp">The time of the event.</param>
    /// <param name="Sender">The advertisement handle.</param>
    /// <param name="Description">A description of the conflict.</param>
    /// <param name="CancellationToken">A token to cancel the processing.</param>
    public delegate Task OnAdvertisementConflictDelegate   (DateTimeOffset               Timestamp,
                                                            IServiceAdvertisementHandle  Sender,
                                                            String                       Description,
                                                            CancellationToken            CancellationToken);

    #endregion


    /// <summary>
    /// The service discovery seam of S2 Connect (PLAN.md §3, D2): advertising the local endpoint,
    /// browsing for remote endpoints and resolving their host names. <see cref="DNSSDServiceDiscovery"/>
    /// implements it with DNS-SD over Multicast DNS, <see cref="InMemoryServiceDiscovery"/> is the
    /// in-process test double for tests and simulations without a network.
    /// </summary>
    public interface IServiceDiscovery : IAsyncDisposable
    {

        /// <summary>
        /// Whether the service discovery is running.
        /// </summary>
        Boolean       IsRunning     { get; }

        /// <summary>
        /// The DNS client resolving the host names of discovered endpoints (".local" names included);
        /// hand it to the S2 Connect clients and the WebSocket client.
        /// </summary>
        IDNSClient    DNSClient     { get; }

        /// <summary>
        /// Start the service discovery.
        /// </summary>
        /// <param name="CancellationToken">A token to cancel the start.</param>
        Task StartAsync(CancellationToken CancellationToken = default);

        /// <summary>
        /// Stop the service discovery, withdrawing every advertisement.
        /// </summary>
        /// <param name="CancellationToken">A token to cancel the stop.</param>
        Task StopAsync(CancellationToken CancellationToken = default);

        /// <summary>
        /// Advertise the given endpoint. The returned handle reports conflicts and withdraws the advertisement.
        /// </summary>
        /// <param name="Advertisement">The advertisement.</param>
        /// <param name="CancellationToken">A token to cancel the advertising.</param>
        Task<IServiceAdvertisementHandle> AdvertiseAsync(S2ServiceAdvertisement  Advertisement,
                                                         CancellationToken       CancellationToken   = default);

        /// <summary>
        /// Browse for S2 Connect endpoints, optionally only those hosting nodes of the given role.
        /// </summary>
        /// <param name="Role">An optional role filter (the DNS-SD subtype).</param>
        /// <param name="CancellationToken">A token to cancel the start of the browser.</param>
        Task<IS2EndpointBrowser> BrowseAsync(EnergyManagementRole?  Role                = null,
                                             CancellationToken      CancellationToken   = default);

    }


    /// <summary>
    /// A running advertisement of the local endpoint.
    /// </summary>
    public interface IServiceAdvertisementHandle : IAsyncDisposable
    {

        /// <summary>
        /// The advertisement.
        /// </summary>
        S2ServiceAdvertisement  Advertisement    { get; }

        /// <summary>
        /// Whether the advertisement is currently published (not withdrawn and without a conflict).
        /// </summary>
        Boolean                 IsPublished      { get; }

        /// <summary>
        /// Whether another host claimed the name of this advertisement.
        /// </summary>
        Boolean                 HasConflict      { get; }

        /// <summary>
        /// An event fired whenever another host claimed the name of this advertisement.
        /// </summary>
        event OnAdvertisementConflictDelegate?  OnConflict;

        /// <summary>
        /// Replace the advertisement (e.g. a changed TXT record or roles) and announce the change.
        /// </summary>
        /// <param name="Advertisement">The new advertisement (same host name).</param>
        /// <param name="CancellationToken">A token to cancel the update.</param>
        Task UpdateAsync(S2ServiceAdvertisement  Advertisement,
                         CancellationToken       CancellationToken   = default);

        /// <summary>
        /// Withdraw the advertisement.
        /// </summary>
        /// <param name="CancellationToken">A token to cancel the withdrawal.</param>
        Task WithdrawAsync(CancellationToken CancellationToken = default);

    }


    /// <summary>
    /// A running browser for S2 Connect endpoints.
    /// </summary>
    public interface IS2EndpointBrowser : IAsyncDisposable
    {

        /// <summary>
        /// The optional role filter of this browser.
        /// </summary>
        EnergyManagementRole?                Role            { get; }

        /// <summary>
        /// The time provider of this browser.
        /// </summary>
        TimeProvider                         TimeProvider    { get; }

        /// <summary>
        /// The currently known endpoints.
        /// </summary>
        IReadOnlyList<S2DiscoveredEndpoint>  Endpoints       { get; }

        /// <summary>
        /// An event fired whenever an endpoint appeared.
        /// </summary>
        event OnS2EndpointDelegate?          OnEndpointAppeared;

        /// <summary>
        /// An event fired whenever a known endpoint changed (URLs, name, addresses, roles).
        /// </summary>
        event OnS2EndpointDelegate?          OnEndpointUpdated;

        /// <summary>
        /// An event fired whenever a known endpoint disappeared.
        /// </summary>
        event OnS2EndpointDelegate?          OnEndpointDisappeared;

        /// <summary>
        /// An event fired whenever a service instance with an invalid S2 Connect TXT record was found.
        /// </summary>
        event OnInvalidS2EndpointDelegate?   OnInvalidEndpoint;

    }


    /// <summary>
    /// Extension methods for S2 endpoint browsers.
    /// </summary>
    public static class S2EndpointBrowserExtensions
    {

        #region WaitForEndpointAsync(this Browser, Predicate = null, Timeout = null, CancellationToken = default)

        /// <summary>
        /// Wait until an endpoint matching the given predicate is known, or the timeout elapsed.
        /// </summary>
        /// <param name="Browser">A browser.</param>
        /// <param name="Predicate">An optional predicate (default: any endpoint).</param>
        /// <param name="Timeout">An optional timeout (default: 10 seconds).</param>
        /// <param name="CancellationToken">A token to cancel the waiting.</param>
        /// <returns>The first matching endpoint, or null when the timeout elapsed.</returns>
        public static async Task<S2DiscoveredEndpoint?> WaitForEndpointAsync(this IS2EndpointBrowser               Browser,
                                                                             Func<S2DiscoveredEndpoint, Boolean>?  Predicate           = null,
                                                                             TimeSpan?                             Timeout             = null,
                                                                             CancellationToken                     CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Browser);

            var predicate  = Predicate ?? (_ => true);
            var found      = new TaskCompletionSource<S2DiscoveredEndpoint>(TaskCreationOptions.RunContinuationsAsynchronously);

            Task OnEndpoint(DateTimeOffset Timestamp, IS2EndpointBrowser Sender, S2DiscoveredEndpoint Endpoint, CancellationToken Token)
            {
                if (predicate(Endpoint))
                    found.TrySetResult(Endpoint);
                return Task.CompletedTask;
            }

            Browser.OnEndpointAppeared += OnEndpoint;
            Browser.OnEndpointUpdated  += OnEndpoint;

            try
            {

                var existing = Browser.Endpoints.FirstOrDefault(predicate);
                if (existing is not null)
                    return existing;

                using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
                timeoutSource.CancelAfter(Timeout ?? TimeSpan.FromSeconds(10));

                using var registration = timeoutSource.Token.Register(() => found.TrySetCanceled());

                try
                {
                    return await found.Task.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!CancellationToken.IsCancellationRequested)
                {
                    return Browser.Endpoints.FirstOrDefault(predicate);
                }

            }
            finally
            {
                Browser.OnEndpointAppeared -= OnEndpoint;
                Browser.OnEndpointUpdated  -= OnEndpoint;
            }

        }

        #endregion

    }

}

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

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Samples
{

    /// <summary>
    /// A DNS-SD pairing helper: it browses the local network for S2 Connect endpoints and prints
    /// what it finds (name, host, pairing URL, roles). It uses a service discovery – over real
    /// Multicast DNS with <see cref="DNSSDServiceDiscovery"/>, or the in-memory discovery for a demo.
    /// </summary>
    public static class PairingTool
    {

        #region BrowseAsync(Discovery, Duration, Role = null, CancellationToken = default)

        /// <summary>
        /// Browse for endpoints for the given duration and print each one as it appears.
        /// </summary>
        /// <param name="Discovery">A started service discovery.</param>
        /// <param name="Duration">How long to browse.</param>
        /// <param name="Role">An optional role filter.</param>
        /// <param name="CancellationToken">A token to stop browsing early.</param>
        public static async Task<IReadOnlyList<S2DiscoveredEndpoint>> BrowseAsync(IServiceDiscovery      Discovery,
                                                                                  TimeSpan               Duration,
                                                                                  EnergyManagementRole?  Role                = null,
                                                                                  CancellationToken      CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Discovery);

            await using var browser = await Discovery.BrowseAsync(Role, CancellationToken);

            browser.OnEndpointAppeared += (timestamp, sender, endpoint, ct) => {
                Console.WriteLine($"  found: {endpoint.Name ?? "(no name)"} at {endpoint.HostName.FullName}:{endpoint.Port}");
                if (endpoint.PairingUrl.HasValue)
                    Console.WriteLine($"         pairing:      {endpoint.PairingUrl.Value.Value}");
                if (endpoint.LongPollingUrl.HasValue)
                    Console.WriteLine($"         long-polling: {endpoint.LongPollingUrl.Value.Value}");
                Console.WriteLine($"         roles:        {(endpoint.HostsCEM == true ? "CEM " : "")}{(endpoint.HostsRM == true ? "RM" : "")}".TrimEnd());
                return Task.CompletedTask;
            };

            browser.OnEndpointDisappeared += (timestamp, sender, endpoint, ct) => {
                Console.WriteLine($"  gone:  {endpoint.HostName.FullName}");
                return Task.CompletedTask;
            };

            try
            {
                await Task.Delay(Duration, CancellationToken);
            }
            catch (OperationCanceledException)
            { }

            return browser.Endpoints;

        }

        #endregion

        #region RunOverMulticastDNSAsync(Duration, CancellationToken = default)

        /// <summary>
        /// Browse the real local network via Multicast DNS for the given duration.
        /// </summary>
        /// <param name="Duration">How long to browse.</param>
        /// <param name="CancellationToken">A token to stop browsing early.</param>
        public static async Task RunOverMulticastDNSAsync(TimeSpan           Duration,
                                                          CancellationToken  CancellationToken   = default)
        {

            await using var discovery = new DNSSDServiceDiscovery();

            await discovery.StartAsync(CancellationToken);

            Console.WriteLine($"Browsing for S2 Connect endpoints for {Duration.TotalSeconds:0} seconds ...");

            var endpoints = await BrowseAsync(discovery, Duration, null, CancellationToken);

            Console.WriteLine($"{endpoints.Count} endpoint(s) found.");

        }

        #endregion

    }

}

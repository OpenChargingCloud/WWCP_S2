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

using System.Net;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// Decides whether a request for one of the LAN-only operations originates from the same
    /// subnet as the pairing server (S2 Connect 1.0.0, "LAN-LAN only interactions": "the pairing
    /// server must check if the request originated from the same subnet ... When a request does not
    /// originate from the same subnet the server must respond with status code 401").
    /// <see cref="SubnetCheck"/> is the default; deployments behind a reverse proxy plug in their own policy.
    /// </summary>
    public interface ISubnetPolicy
    {

        /// <summary>
        /// Whether the given remote address is within the subnet of the given local address.
        /// </summary>
        /// <param name="LocalAddress">The local address the request arrived on.</param>
        /// <param name="RemoteAddress">The address of the peer.</param>
        Boolean IsSameSubnet(IPAddress  LocalAddress,
                             IPAddress  RemoteAddress);

    }


    /// <summary>
    /// A subnet policy accepting every peer, e.g. for tests or for servers whose LAN-only
    /// operations are protected by other means.
    /// </summary>
    public sealed class AllowAllSubnetPolicy : ISubnetPolicy
    {

        /// <summary>
        /// The shared instance.
        /// </summary>
        public static AllowAllSubnetPolicy Instance { get; } = new ();

        /// <inheritdoc/>
        public Boolean IsSameSubnet(IPAddress  LocalAddress,
                                    IPAddress  RemoteAddress)
            => true;

    }


    /// <summary>
    /// A subnet policy rejecting every peer, e.g. for WAN endpoints that must never expose the
    /// LAN-only operations even when they are enabled by mistake.
    /// </summary>
    public sealed class DenyAllSubnetPolicy : ISubnetPolicy
    {

        /// <summary>
        /// The shared instance.
        /// </summary>
        public static DenyAllSubnetPolicy Instance { get; } = new ();

        /// <inheritdoc/>
        public Boolean IsSameSubnet(IPAddress  LocalAddress,
                                    IPAddress  RemoteAddress)
            => false;

    }

}

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

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// An S2 Connect endpoint discovered via DNS-SD (or a service discovery test double):
    /// the service instance, its host and port, the addresses of the host, the parsed TXT
    /// record and, when known, whether the endpoint hosts CEM or RM nodes (the subtypes).
    /// Two discovered endpoints are equal when they describe the same service instance.
    /// </summary>
    public sealed class S2DiscoveredEndpoint : IEquatable<S2DiscoveredEndpoint>
    {

        #region Properties

        /// <summary>
        /// The DNS-SD service instance name (e.g. "myhost._s2connect._tcp.local.").
        /// </summary>
        public DNSServiceName             InstanceName    { get; }

        /// <summary>
        /// The host name of the endpoint (the SRV target).
        /// </summary>
        public DomainName                 HostName        { get; }

        /// <summary>
        /// The port of the endpoint (from the SRV record).
        /// </summary>
        public IPPort                     Port            { get; }

        /// <summary>
        /// The addresses of the host.
        /// </summary>
        public IReadOnlyList<IIPAddress>  Addresses       { get; }

        /// <summary>
        /// The parsed TXT record.
        /// </summary>
        public S2DNSSDTXTRecord           TXT             { get; }

        /// <summary>
        /// Whether the endpoint hosts CEM nodes (subtype "_cem"), or null when unknown.
        /// </summary>
        public Boolean?                   HostsCEM        { get; }

        /// <summary>
        /// Whether the endpoint hosts RM nodes (subtype "_rm"), or null when unknown.
        /// </summary>
        public Boolean?                   HostsRM         { get; }

        /// <summary>
        /// When the endpoint was last seen.
        /// </summary>
        public DateTimeOffset             LastSeen        { get; }

        /// <summary>
        /// The base URL of the pairing API, when announced.
        /// </summary>
        public S2BaseURL?                 PairingUrl
            => TXT.PairingUrl;

        /// <summary>
        /// The base URL of the pairing API supporting long-polling, when announced.
        /// </summary>
        public S2BaseURL?                 LongPollingUrl
            => TXT.LongPollingUrl;

        /// <summary>
        /// The user-facing name of the endpoint, when announced.
        /// </summary>
        public String?                    Name
            => TXT.EndpointName;

        /// <summary>
        /// The endpoint description announced by the TXT record (a LAN endpoint).
        /// </summary>
        public EndpointDescription        EndpointDescription
            => TXT.ToEndpointDescription();

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new discovered endpoint.
        /// </summary>
        /// <param name="InstanceName">The DNS-SD service instance name.</param>
        /// <param name="HostName">The host name of the endpoint.</param>
        /// <param name="Port">The port of the endpoint.</param>
        /// <param name="Addresses">The addresses of the host.</param>
        /// <param name="TXT">The parsed TXT record.</param>
        /// <param name="HostsCEM">Whether the endpoint hosts CEM nodes, or null when unknown.</param>
        /// <param name="HostsRM">Whether the endpoint hosts RM nodes, or null when unknown.</param>
        /// <param name="LastSeen">When the endpoint was last seen.</param>
        public S2DiscoveredEndpoint(DNSServiceName            InstanceName,
                                    DomainName                HostName,
                                    IPPort                    Port,
                                    IEnumerable<IIPAddress>   Addresses,
                                    S2DNSSDTXTRecord          TXT,
                                    Boolean?                  HostsCEM,
                                    Boolean?                  HostsRM,
                                    DateTimeOffset            LastSeen)
        {

            ArgumentNullException.ThrowIfNull(InstanceName);
            ArgumentNullException.ThrowIfNull(HostName);
            ArgumentNullException.ThrowIfNull(Addresses);
            ArgumentNullException.ThrowIfNull(TXT);

            this.InstanceName  = InstanceName;
            this.HostName      = HostName;
            this.Port          = Port;
            this.Addresses     = [.. Addresses.Distinct()];
            this.TXT           = TXT;
            this.HostsCEM      = HostsCEM;
            this.HostsRM       = HostsRM;
            this.LastSeen      = LastSeen;

        }

        #endregion


        #region HostsRole(Role)

        /// <summary>
        /// Whether the endpoint hosts nodes of the given role, or null when unknown.
        /// </summary>
        /// <param name="Role">An energy management role.</param>
        public Boolean? HostsRole(EnergyManagementRole Role)

            => Role == EnergyManagementRole.CEM ? HostsCEM :
               Role == EnergyManagementRole.RM  ? HostsRM  :
                                                  null;

        #endregion

        #region With(...)

        /// <summary>
        /// Return a copy with the given properties replaced.
        /// </summary>
        public S2DiscoveredEndpoint With(IEnumerable<IIPAddress>?  Addresses   = null,
                                         S2DNSSDTXTRecord?         TXT         = null,
                                         Boolean?                  HostsCEM    = null,
                                         Boolean?                  HostsRM     = null,
                                         DateTimeOffset?           LastSeen    = null,
                                         DomainName?               HostName    = null,
                                         IPPort?                   Port        = null)

            => new (InstanceName,
                    HostName  ?? this.HostName,
                    Port      ?? this.Port,
                    Addresses ?? this.Addresses,
                    TXT       ?? this.TXT,
                    HostsCEM  ?? this.HostsCEM,
                    HostsRM   ?? this.HostsRM,
                    LastSeen  ?? this.LastSeen);

        #endregion

        #region SameContentAs(Other)

        /// <summary>
        /// Whether the other discovered endpoint has the same host, port, addresses, TXT record and roles.
        /// </summary>
        /// <param name="Other">Another discovered endpoint.</param>
        public Boolean SameContentAs(S2DiscoveredEndpoint? Other)

            => Other is not null &&
               InstanceName.Equals(Other.InstanceName) &&
               HostName.    Equals(Other.HostName)     &&
               Port.        Equals(Other.Port)         &&
               TXT.         Equals(Other.TXT)          &&
               HostsCEM ==  Other.HostsCEM             &&
               HostsRM  ==  Other.HostsRM              &&
               Addresses.ToHashSet().SetEquals(Other.Addresses);

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two discovered endpoints for equality (same service instance).
        /// </summary>
        public static Boolean operator == (S2DiscoveredEndpoint? Endpoint1, S2DiscoveredEndpoint? Endpoint2)
        {

            if (ReferenceEquals(Endpoint1, Endpoint2))
                return true;

            if (Endpoint1 is null || Endpoint2 is null)
                return false;

            return Endpoint1.Equals(Endpoint2);

        }

        /// <summary>
        /// Compares two discovered endpoints for inequality.
        /// </summary>
        public static Boolean operator != (S2DiscoveredEndpoint? Endpoint1, S2DiscoveredEndpoint? Endpoint2)
            => !(Endpoint1 == Endpoint2);

        #endregion

        #region IEquatable<S2DiscoveredEndpoint> Members

        /// <summary>
        /// Compares two discovered endpoints for equality (same service instance).
        /// </summary>
        public override Boolean Equals(Object? Object)
            => Object is S2DiscoveredEndpoint endpoint &&
                   Equals(endpoint);

        /// <summary>
        /// Compares two discovered endpoints for equality (same service instance).
        /// </summary>
        public Boolean Equals(S2DiscoveredEndpoint? Endpoint)
            => Endpoint is not null &&
               InstanceName.Equals(Endpoint.InstanceName);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => InstanceName.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(
                   Name is not null ? $"'{Name}' " : "",
                   InstanceName.FullName,
                   $" at {HostName.FullName}:{Port}",
                   Addresses.Count > 0 ? $" [{String.Join(", ", Addresses)}]" : "",
                   HostsCEM == true ? " CEM" : "",
                   HostsRM  == true ? " RM"  : "",
                   PairingUrl.HasValue     ? $" pairing: {PairingUrl.Value.Value}"          : "",
                   LongPollingUrl.HasValue ? $" long-polling: {LongPollingUrl.Value.Value}" : ""
               );

        #endregion

    }

}

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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// What an S2 Connect endpoint advertises via DNS-SD (S2 Connect 1.0.0, "DNS-SD based discovery"):
    /// the service instance "&lt;hostname&gt;._s2connect._tcp.local" with the subtypes "_cem" and/or
    /// "_rm", the SRV record pointing at the endpoint's host and port, the TXT record with the
    /// URLs and the address records of the host. The instance name is the host name, as the
    /// specification requires ("Service name: identical to the hostname").
    /// </summary>
    public sealed class S2ServiceAdvertisement : IEquatable<S2ServiceAdvertisement>
    {

        #region Data

        /// <summary>
        /// The DNS-SD service type name of S2 Connect: "_s2connect._tcp.local.".
        /// </summary>
        public static readonly DNSServiceName  ServiceTypeName     = DNSServiceName.Parse($"{S2ConnectDefaults.DNSSDServiceType}.local.");

        /// <summary>
        /// The DNS-SD subtype name of endpoints hosting CEM nodes: "_cem._sub._s2connect._tcp.local.".
        /// </summary>
        public static readonly DNSServiceName  CEMSubtypeName      = SubtypeName(EnergyManagementRole.CEM);

        /// <summary>
        /// The DNS-SD subtype name of endpoints hosting RM nodes: "_rm._sub._s2connect._tcp.local.".
        /// </summary>
        public static readonly DNSServiceName  RMSubtypeName       = SubtypeName(EnergyManagementRole.RM);

        #endregion

        #region Properties

        /// <summary>
        /// The mDNS host name of the endpoint (e.g. "myhost.local.").
        /// </summary>
        public DomainName                          HostName        { get; }

        /// <summary>
        /// The TCP port of the endpoint.
        /// </summary>
        public IPPort                              Port            { get; }

        /// <summary>
        /// The TXT record of the endpoint.
        /// </summary>
        public S2DNSSDTXTRecord                    TXT             { get; }

        /// <summary>
        /// The energy management roles of the hosted nodes (the DNS-SD subtypes).
        /// </summary>
        public IReadOnlySet<EnergyManagementRole>  Roles           { get; }

        /// <summary>
        /// The optional addresses of the host (default: the addresses of the network interfaces).
        /// </summary>
        public IReadOnlyList<IIPAddress>?          Addresses       { get; }

        /// <summary>
        /// The DNS-SD service instance name: "&lt;first label of the host name&gt;._s2connect._tcp.local.".
        /// </summary>
        public DNSServiceName                      InstanceName    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new S2 Connect service advertisement.
        /// </summary>
        /// <param name="HostName">The mDNS host name of the endpoint (must end with ".local").</param>
        /// <param name="Port">The TCP port of the endpoint.</param>
        /// <param name="TXT">The TXT record of the endpoint.</param>
        /// <param name="Roles">The energy management roles of the hosted nodes (the DNS-SD subtypes).</param>
        /// <param name="Addresses">Optional explicit addresses of the host (default: the addresses of the network interfaces).</param>
        public S2ServiceAdvertisement(DomainName                         HostName,
                                      IPPort                             Port,
                                      S2DNSSDTXTRecord                   TXT,
                                      IEnumerable<EnergyManagementRole>?  Roles       = null,
                                      IEnumerable<IIPAddress>?           Addresses   = null)
        {

            ArgumentNullException.ThrowIfNull(HostName);
            ArgumentNullException.ThrowIfNull(TXT);

            if (!MulticastDNS.IsLocalName(HostName))
                throw new ArgumentException($"The host name of a LAN endpoint must be an mDNS name ending with '.local', not '{HostName}'!", nameof(HostName));

            if (HostName.Labels.Count < 2)
                throw new ArgumentException($"The host name must have a label before '.local', not '{HostName}'!", nameof(HostName));

            var roles = new HashSet<EnergyManagementRole>(Roles ?? []);

            foreach (var role in roles)
            {
                if (role != EnergyManagementRole.CEM && role != EnergyManagementRole.RM)
                    throw new ArgumentException($"Unknown energy management role '{role}'!", nameof(Roles));
            }

            var addresses = Addresses?.ToArray();

            if (addresses is not null && addresses.Length == 0)
                throw new ArgumentException("When addresses are given, at least one address is required!", nameof(Addresses));

            this.HostName      = HostName;
            this.Port          = Port;
            this.TXT           = TXT;
            this.Roles         = roles;
            this.Addresses     = addresses;
            this.InstanceName  = DNSServiceName.Parse($"{HostName.Labels[0]}.{ServiceTypeName.FullName}");

        }

        #endregion


        #region (static) SubtypeName(Role)

        /// <summary>
        /// The DNS-SD subtype name of the given role ("_cem._sub._s2connect._tcp.local." or "_rm._sub._s2connect._tcp.local.").
        /// </summary>
        /// <param name="Role">An energy management role.</param>
        public static DNSServiceName SubtypeName(EnergyManagementRole Role)
        {

            var subtype = Role == EnergyManagementRole.CEM
                              ? S2ConnectDefaults.DNSSDSubtypeCEM
                              : Role == EnergyManagementRole.RM
                                    ? S2ConnectDefaults.DNSSDSubtypeRM
                                    : throw new ArgumentException($"Unknown energy management role '{Role}'!", nameof(Role));

            return DNSServiceName.Parse($"{subtype}._sub.{S2ConnectDefaults.DNSSDServiceType}.local.");

        }

        #endregion

        #region (static) FromLocalEndpoint(Endpoint, LongPollingUrl = null, Roles = null, HostName = null, Port = null, Addresses = null)

        /// <summary>
        /// Create the advertisement of the given local endpoint: the host name and port of its
        /// pairing URL, its description (name, logo) and the roles of its hosted nodes.
        /// </summary>
        /// <param name="Endpoint">A local endpoint.</param>
        /// <param name="LongPollingUrl">An optional long-polling URL to announce.</param>
        /// <param name="Roles">Optional roles (default: the roles of the hosted nodes).</param>
        /// <param name="HostName">An optional host name (default: the host of the pairing URL, which must end with ".local").</param>
        /// <param name="Port">An optional port (default: the port of the pairing URL, or 443/80).</param>
        /// <param name="Addresses">Optional explicit addresses of the host.</param>
        public static S2ServiceAdvertisement FromLocalEndpoint(LocalEndpoint                       Endpoint,
                                                               S2BaseURL?                          LongPollingUrl   = null,
                                                               IEnumerable<EnergyManagementRole>?  Roles            = null,
                                                               DomainName?                         HostName         = null,
                                                               IPPort?                             Port             = null,
                                                               IEnumerable<IIPAddress>?            Addresses        = null)
        {

            ArgumentNullException.ThrowIfNull(Endpoint);

            var pairingUrl  = Endpoint.PairingUrl;

            var hostName    = HostName ?? (DomainName.TryParse(pairingUrl.Host, out var parsedHostName, out _)
                                               ? parsedHostName
                                               : throw new ArgumentException($"The host of the pairing URL '{pairingUrl.Host}' is not a domain name; give an explicit host name!", nameof(Endpoint)));

            var port        = Port ?? pairingUrl.URL.Port ?? (pairingUrl.IsHTTPS ? IPPort.HTTPS : IPPort.HTTP);

            var txt         = new S2DNSSDTXTRecord(
                                  pairingUrl,
                                  LongPollingUrl,
                                  Endpoint.Description.Name,
                                  Endpoint.Description.LogoUrl
                              );

            var roles       = Roles ?? Endpoint.Nodes.Select(node => node.Role).Distinct();

            return new S2ServiceAdvertisement(
                       hostName,
                       port,
                       txt,
                       roles,
                       Addresses
                   );

        }

        #endregion

        #region ToResourceRecords(Addresses = null, HostRecordTimeToLive = null, SharedRecordTimeToLive = null)

        /// <summary>
        /// The DNS resource records of this advertisement (RFC 6763 §4 to §6, RFC 6762 §10):
        /// the PTR record of the service type and of every subtype, the SRV and TXT records of
        /// the instance and the A/AAAA records of the host.
        /// </summary>
        /// <param name="Addresses">The addresses of the host (default: the explicit addresses of this advertisement).</param>
        /// <param name="HostRecordTimeToLive">The time-to-live of the SRV, A and AAAA records (default: 120 s).</param>
        /// <param name="SharedRecordTimeToLive">The time-to-live of the PTR and TXT records (default: 75 minutes).</param>
        public IReadOnlyList<IDNSResourceRecord> ToResourceRecords(IEnumerable<IIPAddress>?  Addresses                = null,
                                                                   TimeSpan?                 HostRecordTimeToLive     = null,
                                                                   TimeSpan?                 SharedRecordTimeToLive   = null)
        {

            var addresses  = (Addresses ?? this.Addresses)?.Distinct().ToArray() ?? [];

            if (addresses.Length == 0)
                throw new ArgumentException("At least one address of the host is required!", nameof(Addresses));

            var hostTTL    = HostRecordTimeToLive   ?? MulticastDNS.HostRecordTimeToLive;
            var sharedTTL  = SharedRecordTimeToLive ?? MulticastDNS.SharedRecordTimeToLive;

            var records    = new List<IDNSResourceRecord> {
                                 new PTR(ServiceTypeName, DNSQueryClasses.IN, sharedTTL, InstanceName)
                             };

            foreach (var role in Roles.OrderBy(role => role.ToString(), StringComparer.Ordinal))
                records.Add(new PTR(SubtypeName(role), DNSQueryClasses.IN, sharedTTL, InstanceName));

            records.Add(new SRV(InstanceName, DNSQueryClasses.IN, hostTTL, 0, 0, Port, HostName));
            records.Add(TXT.ToTXT(InstanceName, sharedTTL));

            foreach (var address in addresses)
            {

                if (address is IPv4Address ipv4Address)
                    records.Add(new A(HostName, DNSQueryClasses.IN, hostTTL, ipv4Address));

                else if (address is IPv6Address ipv6Address)
                    records.Add(new AAAA(HostName, DNSQueryClasses.IN, hostTTL, ipv6Address));

            }

            return records;

        }

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two advertisements for equality.
        /// </summary>
        public static Boolean operator == (S2ServiceAdvertisement? Advertisement1, S2ServiceAdvertisement? Advertisement2)
        {

            if (ReferenceEquals(Advertisement1, Advertisement2))
                return true;

            if (Advertisement1 is null || Advertisement2 is null)
                return false;

            return Advertisement1.Equals(Advertisement2);

        }

        /// <summary>
        /// Compares two advertisements for inequality.
        /// </summary>
        public static Boolean operator != (S2ServiceAdvertisement? Advertisement1, S2ServiceAdvertisement? Advertisement2)
            => !(Advertisement1 == Advertisement2);

        #endregion

        #region IEquatable<S2ServiceAdvertisement> Members

        /// <summary>
        /// Compares two advertisements for equality.
        /// </summary>
        public override Boolean Equals(Object? Object)
            => Object is S2ServiceAdvertisement advertisement &&
                   Equals(advertisement);

        /// <summary>
        /// Compares two advertisements for equality (host, port, TXT record, roles and addresses).
        /// </summary>
        public Boolean Equals(S2ServiceAdvertisement? Advertisement)
            => Advertisement is not null &&
               HostName.Equals(Advertisement.HostName) &&
               Port.    Equals(Advertisement.Port)     &&
               TXT.     Equals(Advertisement.TXT)      &&
               Roles.   SetEquals(Advertisement.Roles) &&
               ((Addresses is null && Advertisement.Addresses is null) ||
                (Addresses is not null && Advertisement.Addresses is not null &&
                 Addresses.ToHashSet().SetEquals(Advertisement.Addresses)));

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {
                return HostName.GetHashCode() * 7 ^
                       Port.    GetHashCode() * 5 ^
                       TXT.     GetHashCode() * 3 ^
                       Roles.Count;
            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{InstanceName.FullName} at {HostName.FullName}:{Port} [{String.Join(", ", Roles)}]: {TXT}";

        #endregion

    }

}

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
using System.Net.NetworkInformation;
using System.Net.Sockets;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// A unicast address of a local network interface together with its subnet prefix length.
    /// </summary>
    /// <param name="Address">The unicast address.</param>
    /// <param name="PrefixLength">The subnet prefix length in bits.</param>
    public readonly record struct InterfaceAddress(IPAddress  Address,
                                                   Int32      PrefixLength);


    /// <summary>
    /// The default <see cref="ISubnetPolicy"/>: a peer is accepted when it is the loopback host,
    /// when it shares the subnet prefix of the local interface address the request arrived on
    /// (IPv4 and IPv6, IPv4-mapped IPv6 addresses are unmapped first), or when both addresses
    /// are link-local on the same link (equal IPv6 scope identifiers when known). The interface
    /// addresses are taken from the operating system and cached for a short time.
    /// <c>X-Forwarded-For</c> headers are never consulted (PLAN.md Phase 6b).
    /// </summary>
    public sealed class SubnetCheck : ISubnetPolicy
    {

        #region Data

        private readonly Lock                                lockObject = new ();
        private readonly Func<IEnumerable<InterfaceAddress>>  interfaceAddressProvider;
        private readonly TimeProvider                         timeProvider;
        private          IReadOnlyList<InterfaceAddress>?     cachedAddresses;
        private          DateTimeOffset                       cacheExpiresAt;

        #endregion

        #region Properties

        /// <summary>
        /// How long the interface addresses read from the operating system are cached.
        /// </summary>
        public TimeSpan  CacheLifetime    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new subnet check.
        /// </summary>
        /// <param name="InterfaceAddressProvider">An optional provider of the local interface addresses (default: the operating system).</param>
        /// <param name="TimeProvider">An optional time provider for the cache (default: the system clock).</param>
        /// <param name="CacheLifetime">How long the interface addresses are cached (default: 10 seconds).</param>
        public SubnetCheck(Func<IEnumerable<InterfaceAddress>>?  InterfaceAddressProvider   = null,
                           TimeProvider?                         TimeProvider               = null,
                           TimeSpan?                             CacheLifetime              = null)
        {

            this.interfaceAddressProvider  = InterfaceAddressProvider ?? GetInterfaceAddresses;
            this.timeProvider              = TimeProvider             ?? System.TimeProvider.System;
            this.CacheLifetime             = CacheLifetime            ?? TimeSpan.FromSeconds(10);

            if (this.CacheLifetime < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(CacheLifetime), "The cache lifetime must not be negative!");

        }

        #endregion


        #region IsSameSubnet(LocalAddress, RemoteAddress)

        /// <inheritdoc/>
        public Boolean IsSameSubnet(IPAddress  LocalAddress,
                                    IPAddress  RemoteAddress)

            => IsSameSubnet(LocalAddress,
                            RemoteAddress,
                            InterfaceAddresses);

        #endregion

        #region InterfaceAddresses

        /// <summary>
        /// The (cached) unicast addresses of the local network interfaces.
        /// </summary>
        public IReadOnlyList<InterfaceAddress> InterfaceAddresses
        {
            get
            {

                var now = timeProvider.GetUtcNow();

                lock (lockObject)
                {

                    if (cachedAddresses is null || now >= cacheExpiresAt)
                    {
                        cachedAddresses  = [.. interfaceAddressProvider()];
                        cacheExpiresAt   = now + CacheLifetime;
                    }

                    return cachedAddresses;

                }

            }
        }

        #endregion


        #region (static) IsSameSubnet(LocalAddress, RemoteAddress, InterfaceAddresses)

        /// <summary>
        /// The pure subnet decision: loopback peers are accepted; peers of another address
        /// family are rejected; link-local peers are accepted when the local address is
        /// link-local too (and the IPv6 scope identifiers are equal when both are known);
        /// otherwise the peer must share the prefix of the interface address equal to the
        /// local address, or, when the local address is not an interface address, the prefix
        /// of any interface address of the same family.
        /// </summary>
        /// <param name="LocalAddress">The local address the request arrived on.</param>
        /// <param name="RemoteAddress">The address of the peer.</param>
        /// <param name="InterfaceAddresses">The unicast addresses of the local network interfaces.</param>
        public static Boolean IsSameSubnet(IPAddress                      LocalAddress,
                                           IPAddress                      RemoteAddress,
                                           IEnumerable<InterfaceAddress>  InterfaceAddresses)
        {

            ArgumentNullException.ThrowIfNull(LocalAddress);
            ArgumentNullException.ThrowIfNull(RemoteAddress);
            ArgumentNullException.ThrowIfNull(InterfaceAddresses);

            var local   = Normalise(LocalAddress);
            var remote  = Normalise(RemoteAddress);

            if (IPAddress.IsLoopback(remote) || IPAddress.IsLoopback(local))
                return true;

            if (local.AddressFamily != remote.AddressFamily)
                return false;

            if (IsLinkLocal(remote))
            {

                if (!IsLinkLocal(local))
                    return false;

                if (local.AddressFamily == AddressFamily.InterNetworkV6 &&
                    local. ScopeId != 0 &&
                    remote.ScopeId != 0)
                {
                    return local.ScopeId == remote.ScopeId;
                }

                return true;

            }

            var interfaceAddresses  = InterfaceAddresses.
                                          Select(entry => new InterfaceAddress(Normalise(entry.Address), entry.PrefixLength)).
                                          Where (entry => entry.Address.AddressFamily == local.AddressFamily).
                                          ToList();

            var matchingInterfaces  = interfaceAddresses.
                                          Where(entry => entry.Address.Equals(local)).
                                          ToList();

            if (matchingInterfaces.Count > 0)
                return matchingInterfaces.Any(entry => SharePrefix(local, remote, entry.PrefixLength));

            return interfaceAddresses.Any(entry => SharePrefix(entry.Address, remote, entry.PrefixLength));

        }

        #endregion

        #region (static) SharePrefix(Address1, Address2, PrefixLength)

        /// <summary>
        /// Whether the two addresses of the same family share the given number of leading bits.
        /// </summary>
        /// <param name="Address1">An IP address.</param>
        /// <param name="Address2">Another IP address.</param>
        /// <param name="PrefixLength">The number of leading bits to compare.</param>
        public static Boolean SharePrefix(IPAddress  Address1,
                                          IPAddress  Address2,
                                          Int32      PrefixLength)
        {

            ArgumentNullException.ThrowIfNull(Address1);
            ArgumentNullException.ThrowIfNull(Address2);

            if (Address1.AddressFamily != Address2.AddressFamily)
                return false;

            var bytes1  = Address1.GetAddressBytes();
            var bytes2  = Address2.GetAddressBytes();

            if (PrefixLength < 0 || PrefixLength > bytes1.Length * 8)
                return false;

            var fullBytes  = PrefixLength / 8;
            var restBits   = PrefixLength % 8;

            for (var i = 0; i < fullBytes; i++)
            {
                if (bytes1[i] != bytes2[i])
                    return false;
            }

            if (restBits == 0)
                return true;

            var mask = (Byte) (0xFF << (8 - restBits));

            return (bytes1[fullBytes] & mask) == (bytes2[fullBytes] & mask);

        }

        #endregion

        #region (static) Normalise(Address)

        /// <summary>
        /// Map IPv4-mapped IPv6 addresses (::ffff:a.b.c.d, as reported by dual-stack sockets)
        /// to their IPv4 form; every other address is returned unchanged.
        /// </summary>
        /// <param name="Address">An IP address.</param>
        public static IPAddress Normalise(IPAddress Address)
        {

            ArgumentNullException.ThrowIfNull(Address);

            return Address.IsIPv4MappedToIPv6
                       ? Address.MapToIPv4()
                       : Address;

        }

        #endregion

        #region (static) IsLinkLocal(Address)

        /// <summary>
        /// Whether the given address is link-local: 169.254.0.0/16 (also IPv4-mapped) or fe80::/10.
        /// </summary>
        /// <param name="Address">An IP address.</param>
        public static Boolean IsLinkLocal(IPAddress Address)
        {

            ArgumentNullException.ThrowIfNull(Address);

            Address = Normalise(Address);

            if (Address.AddressFamily == AddressFamily.InterNetworkV6)
                return Address.IsIPv6LinkLocal;

            if (Address.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = Address.GetAddressBytes();
                return bytes[0] == 169 && bytes[1] == 254;
            }

            return false;

        }

        #endregion

        #region (static) GetInterfaceAddresses()

        /// <summary>
        /// Read the unicast addresses and prefix lengths of all operational network interfaces
        /// from the operating system.
        /// </summary>
        public static IEnumerable<InterfaceAddress> GetInterfaceAddresses()
        {

            var result = new List<InterfaceAddress>();

            NetworkInterface[] interfaces;

            try
            {
                interfaces = NetworkInterface.GetAllNetworkInterfaces();
            }
            catch (NetworkInformationException)
            {
                return result;
            }

            foreach (var networkInterface in interfaces)
            {

                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    continue;

                IPInterfaceProperties properties;

                try
                {
                    properties = networkInterface.GetIPProperties();
                }
                catch (NetworkInformationException)
                {
                    continue;
                }

                foreach (var unicast in properties.UnicastAddresses)
                {

                    var prefixLength = TryGetPrefixLength(unicast);

                    if (prefixLength.HasValue)
                        result.Add(new InterfaceAddress(unicast.Address, prefixLength.Value));

                }

            }

            return result;

        }

        private static Int32? TryGetPrefixLength(UnicastIPAddressInformation Unicast)
        {

            try
            {
                return Unicast.PrefixLength;
            }
            catch (PlatformNotSupportedException)
            { }

            if (Unicast.Address.AddressFamily == AddressFamily.InterNetwork)
            {

                try
                {

                    var mask   = Unicast.IPv4Mask.GetAddressBytes();
                    var length = 0;

                    foreach (var maskByte in mask)
                        length += Int32.PopCount(maskByte);

                    return length;

                }
                catch (PlatformNotSupportedException)
                { }

            }

            return null;

        }

        #endregion

    }

}

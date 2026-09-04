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
using System.Net.Sockets;

using Microsoft.Extensions.Time.Testing;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The subnet check guarding the LAN-only operations (PLAN.md Phase 6b): the pure decision
    /// with an explicit list of interface addresses, its helpers, the cached instance policy
    /// and the allow-all / deny-all policies.
    /// </summary>
    [TestFixture]
    public sealed class SubnetCheckTests
    {

        #region Helpers

        /// <summary>
        /// Parse "address/prefix" entries separated by spaces into interface addresses.
        /// </summary>
        private static InterfaceAddress[] Interfaces(String Text)

            => Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).
                    Select(entry => {
                        var slash = entry.LastIndexOf('/');
                        return new InterfaceAddress(IPAddress.Parse(entry[..slash]),
                                                    Int32.Parse(entry[(slash + 1)..], System.Globalization.CultureInfo.InvariantCulture));
                    }).
                    ToArray();

        private static IPAddress IPv6WithScope(String Address, Int64 ScopeId)
            => new (IPAddress.Parse(Address).GetAddressBytes(), ScopeId);

        #endregion


        #region IsSameSubnet(...) decision table

        [S2C("LAN.SubnetCheck")]

        // Same IPv4 subnet
        [TestCase("192.168.1.10",         "192.168.1.77",         "192.168.1.10/24",                       true,  TestName = "IsSameSubnet: IPv4 same /24")]
        [TestCase("192.168.1.10",         "192.168.2.5",          "192.168.1.10/24",                       false, TestName = "IsSameSubnet: IPv4 other /24")]

        // Prefix boundaries
        [TestCase("192.168.1.10",         "192.168.2.5",          "192.168.1.10/16",                       true,  TestName = "IsSameSubnet: IPv4 /16 covers the other /24")]
        [TestCase("192.168.1.10",         "192.168.1.100",        "192.168.1.10/25",                       true,  TestName = "IsSameSubnet: IPv4 /25 same half")]
        [TestCase("192.168.1.10",         "192.168.1.200",        "192.168.1.10/25",                       false, TestName = "IsSameSubnet: IPv4 /25 other half")]
        [TestCase("192.168.1.10",         "192.168.1.10",         "192.168.1.10/32",                       true,  TestName = "IsSameSubnet: IPv4 /32 same address")]
        [TestCase("192.168.1.10",         "192.168.1.11",         "192.168.1.10/32",                       false, TestName = "IsSameSubnet: IPv4 /32 neighbour")]

        // IPv4-mapped IPv6 addresses (dual-stack sockets)
        [TestCase("192.168.1.10",         "::ffff:192.168.1.77",  "192.168.1.10/24",                       true,  TestName = "IsSameSubnet: IPv4-mapped remote")]
        [TestCase("::ffff:192.168.1.10",  "192.168.1.77",         "192.168.1.10/24",                       true,  TestName = "IsSameSubnet: IPv4-mapped local")]
        [TestCase("::ffff:192.168.1.10",  "::ffff:192.168.2.5",   "192.168.1.10/24",                       false, TestName = "IsSameSubnet: IPv4-mapped both, other subnet")]

        // Loopback peers are always accepted
        [TestCase("192.168.1.10",         "127.0.0.1",            "",                                      true,  TestName = "IsSameSubnet: IPv4 loopback remote")]
        [TestCase("192.168.1.10",         "::1",                  "",                                      true,  TestName = "IsSameSubnet: IPv6 loopback remote")]
        [TestCase("2001:db8:1:2::10",     "127.0.0.1",            "",                                      true,  TestName = "IsSameSubnet: IPv4 loopback remote at IPv6 local")]
        [TestCase("192.168.1.10",         "::ffff:127.0.0.1",     "",                                      true,  TestName = "IsSameSubnet: IPv4-mapped loopback remote")]
        [TestCase("127.0.0.1",            "192.168.1.77",         "",                                      true,  TestName = "IsSameSubnet: loopback local")]

        // Different address families
        [TestCase("192.168.1.10",         "2001:db8:1:2::77",     "192.168.1.10/24 2001:db8:1:2::10/64",   false, TestName = "IsSameSubnet: IPv4 local, global IPv6 remote")]
        [TestCase("2001:db8:1:2::10",     "192.168.1.77",         "192.168.1.10/24 2001:db8:1:2::10/64",   false, TestName = "IsSameSubnet: IPv6 local, IPv4 remote")]

        // IPv6 prefixes
        [TestCase("2001:db8:1:2::10",     "2001:db8:1:2::77",     "2001:db8:1:2::10/64",                   true,  TestName = "IsSameSubnet: IPv6 same /64")]
        [TestCase("2001:db8:1:2::10",     "2001:db8:1:3::77",     "2001:db8:1:2::10/64",                   false, TestName = "IsSameSubnet: IPv6 other /64")]
        [TestCase("2001:db8:1:2::10",     "2001:db8:1:3::77",     "2001:db8:1:2::10/48",                   true,  TestName = "IsSameSubnet: IPv6 /48 covers the other /64")]

        // Link-local peers
        [TestCase("fe80::1",              "fe80::2",              "",                                      true,  TestName = "IsSameSubnet: IPv6 link-local both")]
        [TestCase("169.254.10.1",         "169.254.20.2",         "",                                      true,  TestName = "IsSameSubnet: IPv4 link-local both")]
        [TestCase("192.168.1.10",         "169.254.20.2",         "192.168.1.10/24",                       false, TestName = "IsSameSubnet: IPv4 link-local remote at routable local")]
        [TestCase("2001:db8:1:2::10",     "fe80::2",              "2001:db8:1:2::10/64",                   false, TestName = "IsSameSubnet: IPv6 link-local remote at global local")]
        [TestCase("fe80::1",              "2001:db8:1:2::77",     "fe80::1/64",                            false, TestName = "IsSameSubnet: global remote at link-local local")]

        // The local address is not an interface address: any interface of the same family decides
        [TestCase("10.5.5.5",             "10.9.9.9",             "10.0.0.1/8",                            true,  TestName = "IsSameSubnet: fallback to any IPv4 interface, inside")]
        [TestCase("10.5.5.5",             "11.0.0.1",             "10.0.0.1/8",                            false, TestName = "IsSameSubnet: fallback to any IPv4 interface, outside")]
        [TestCase("192.168.1.10",         "192.168.1.77",         "",                                      false, TestName = "IsSameSubnet: no interfaces at all")]
        [TestCase("192.168.1.10",         "192.168.1.77",         "2001:db8:1:2::10/64",                   false, TestName = "IsSameSubnet: only interfaces of another family")]

        // Several entries for the same local address; other interfaces do not count once the local address is known
        [TestCase("192.168.1.10",         "192.168.2.5",          "192.168.1.10/24 192.168.1.10/16",       true,  TestName = "IsSameSubnet: any prefix of the local interface address wins")]
        [TestCase("192.168.1.10",         "10.9.9.9",             "192.168.1.10/24 10.0.0.1/8",            false, TestName = "IsSameSubnet: other interfaces are ignored for a known local address")]

        public void IsSameSubnet_DecisionTable(String   Local,
                                               String   Remote,
                                               String   InterfaceList,
                                               Boolean  Expected)
        {

            var result = SubnetCheck.IsSameSubnet(IPAddress.Parse(Local),
                                                  IPAddress.Parse(Remote),
                                                  Interfaces(InterfaceList));

            Assert.That(result, Is.EqualTo(Expected));

        }

        #endregion

        #region IsSameSubnet(...) link-local scope identifiers

        [Test]
        [S2C("LAN.SubnetCheck")]
        public void IsSameSubnet_LinkLocalIPv6_ComparesScopeIdentifiersOnlyWhenBothAreKnown()
        {

            Assert.Multiple(() => {
                Assert.That(SubnetCheck.IsSameSubnet(IPv6WithScope("fe80::1", 3), IPv6WithScope("fe80::2", 3), []), Is.True,  "equal scope");
                Assert.That(SubnetCheck.IsSameSubnet(IPv6WithScope("fe80::1", 3), IPv6WithScope("fe80::2", 4), []), Is.False, "different scope");
                Assert.That(SubnetCheck.IsSameSubnet(IPv6WithScope("fe80::1", 0), IPv6WithScope("fe80::2", 4), []), Is.True,  "unknown local scope");
                Assert.That(SubnetCheck.IsSameSubnet(IPv6WithScope("fe80::1", 3), IPv6WithScope("fe80::2", 0), []), Is.True,  "unknown remote scope");
            });

        }

        #endregion

        #region IsSameSubnet(...) null guards

        [Test]
        public void IsSameSubnet_RejectsNullArguments()
        {

            var address = IPAddress.Parse("192.168.1.10");

            Assert.Multiple(() => {
                Assert.That(() => SubnetCheck.IsSameSubnet(null!,   address, []),    Throws.ArgumentNullException);
                Assert.That(() => SubnetCheck.IsSameSubnet(address, null!,   []),    Throws.ArgumentNullException);
                Assert.That(() => SubnetCheck.IsSameSubnet(address, address, null!), Throws.ArgumentNullException);
                Assert.That(() => SubnetCheck.SharePrefix (null!,   address, 24),    Throws.ArgumentNullException);
                Assert.That(() => SubnetCheck.SharePrefix (address, null!,   24),    Throws.ArgumentNullException);
                Assert.That(() => SubnetCheck.Normalise   (null!),                   Throws.ArgumentNullException);
                Assert.That(() => SubnetCheck.IsLinkLocal (null!),                   Throws.ArgumentNullException);
            });

        }

        #endregion


        #region SharePrefix(...)

        [TestCase("192.168.1.10",  "10.0.0.1",             0,   true,  TestName = "SharePrefix: prefix 0 matches any IPv4 pair")]
        [TestCase("2001:db8::1",   "2001:db8::2",          0,   true,  TestName = "SharePrefix: prefix 0 matches any IPv6 pair")]
        [TestCase("192.168.1.10",  "192.168.1.77",         24,  true,  TestName = "SharePrefix: IPv4 /24")]
        [TestCase("192.168.1.10",  "192.168.1.77",         25,  true,  TestName = "SharePrefix: IPv4 /25 (partial byte, equal bit)")]
        [TestCase("192.168.1.10",  "192.168.1.77",         26,  false, TestName = "SharePrefix: IPv4 /26 (partial byte, different bit)")]
        [TestCase("192.168.1.10",  "192.168.1.10",         32,  true,  TestName = "SharePrefix: IPv4 /32 same address")]
        [TestCase("192.168.1.10",  "192.168.1.11",         32,  false, TestName = "SharePrefix: IPv4 /32 other address")]
        [TestCase("192.168.1.10",  "192.168.1.77",         -1,  false, TestName = "SharePrefix: negative prefix")]
        [TestCase("192.168.1.10",  "192.168.1.77",         33,  false, TestName = "SharePrefix: IPv4 prefix too long")]
        [TestCase("2001:db8::1",   "2001:db8::1",          128, true,  TestName = "SharePrefix: IPv6 /128 same address")]
        [TestCase("2001:db8::1",   "2001:db8::2",          128, false, TestName = "SharePrefix: IPv6 /128 other address")]
        [TestCase("2001:db8::1",   "2001:db8::2",          129, false, TestName = "SharePrefix: IPv6 prefix too long")]
        [TestCase("192.168.1.10",  "2001:db8::1",          0,   false, TestName = "SharePrefix: different families")]
        [TestCase("192.168.1.10",  "::ffff:192.168.1.10",  0,   false, TestName = "SharePrefix: IPv4-mapped is not normalised here")]

        public void SharePrefix_ComparesTheLeadingBits(String   Address1,
                                                       String   Address2,
                                                       Int32    PrefixLength,
                                                       Boolean  Expected)
        {

            var result = SubnetCheck.SharePrefix(IPAddress.Parse(Address1),
                                                 IPAddress.Parse(Address2),
                                                 PrefixLength);

            Assert.That(result, Is.EqualTo(Expected));

        }

        #endregion

        #region Normalise(...)

        [Test]
        public void Normalise_UnmapsIPv4MappedIPv6Addresses_AndLeavesOthersUntouched()
        {

            var mapped     = IPAddress.Parse("::ffff:192.168.1.77");
            var ipv4       = IPAddress.Parse("192.168.1.77");
            var ipv6       = IPAddress.Parse("2001:db8::1");

            var normalised = SubnetCheck.Normalise(mapped);

            Assert.Multiple(() => {
                Assert.That(normalised.AddressFamily,        Is.EqualTo(AddressFamily.InterNetwork));
                Assert.That(normalised,                      Is.EqualTo(ipv4));
                Assert.That(SubnetCheck.Normalise(ipv4),     Is.SameAs(ipv4));
                Assert.That(SubnetCheck.Normalise(ipv6),     Is.SameAs(ipv6));
            });

        }

        #endregion

        #region IsLinkLocal(...)

        [TestCase("169.254.0.1",      true,  TestName = "IsLinkLocal: 169.254.0.0/16 start")]
        [TestCase("169.254.255.254",  true,  TestName = "IsLinkLocal: 169.254.0.0/16 end")]
        [TestCase("169.253.255.255",  false, TestName = "IsLinkLocal: below 169.254.0.0/16")]
        [TestCase("169.255.0.1",      false, TestName = "IsLinkLocal: above 169.254.0.0/16")]
        [TestCase("192.168.1.10",     false, TestName = "IsLinkLocal: private IPv4")]
        [TestCase("127.0.0.1",        false, TestName = "IsLinkLocal: IPv4 loopback")]
        [TestCase("fe80::1",          true,  TestName = "IsLinkLocal: fe80::/10 start")]
        [TestCase("febf::1",          true,  TestName = "IsLinkLocal: fe80::/10 end")]
        [TestCase("fec0::1",          false, TestName = "IsLinkLocal: site-local is not link-local")]
        [TestCase("2001:db8::1",      false, TestName = "IsLinkLocal: global IPv6")]
        [TestCase("::1",              false, TestName = "IsLinkLocal: IPv6 loopback")]

        public void IsLinkLocal_Recognises169_254_And_fe80(String   Address,
                                                           Boolean  Expected)
        {
            Assert.That(SubnetCheck.IsLinkLocal(IPAddress.Parse(Address)), Is.EqualTo(Expected));
        }

        #endregion


        #region Instance policy: caching of the interface addresses

        [Test]
        [S2C("LAN.SubnetCheck")]
        public void InstancePolicy_ReadsTheInterfaceAddressesOncePerCacheLifetime()
        {

            var clock       = new FakeTimeProvider(new DateTimeOffset(2026, 9, 4, 10, 0, 0, TimeSpan.Zero));
            var interfaces  = Interfaces("192.168.1.10/24");
            var calls       = 0;

            var policy      = new SubnetCheck(() => { calls++; return interfaces; },
                                              clock,
                                              TimeSpan.FromSeconds(10));

            var local       = IPAddress.Parse("192.168.1.10");

            Assert.Multiple(() => {
                Assert.That(policy.CacheLifetime,                                       Is.EqualTo(TimeSpan.FromSeconds(10)));
                Assert.That(policy.IsSameSubnet(local, IPAddress.Parse("192.168.1.77")), Is.True);
                Assert.That(policy.IsSameSubnet(local, IPAddress.Parse("192.168.2.5")),  Is.False);
                Assert.That(policy.InterfaceAddresses,                                  Is.EqualTo(interfaces));
                Assert.That(calls,                                                      Is.EqualTo(1), "within the cache lifetime the provider is asked once");
            });

            clock.Advance(TimeSpan.FromSeconds(9));

            Assert.Multiple(() => {
                Assert.That(policy.IsSameSubnet(local, IPAddress.Parse("192.168.1.77")), Is.True);
                Assert.That(calls,                                                      Is.EqualTo(1), "still cached after 9 seconds");
            });

            clock.Advance(TimeSpan.FromSeconds(2));

            Assert.Multiple(() => {
                Assert.That(policy.IsSameSubnet(local, IPAddress.Parse("192.168.1.77")), Is.True);
                Assert.That(calls,                                                      Is.EqualTo(2), "re-read after the cache lifetime");
            });

            clock.Advance(TimeSpan.FromSeconds(1));

            Assert.Multiple(() => {
                Assert.That(policy.InterfaceAddresses,                                  Has.Count.EqualTo(1));
                Assert.That(calls,                                                      Is.EqualTo(2), "the refreshed cache is used again");
            });

        }

        [Test]
        public void InstancePolicy_WithAZeroCacheLifetime_AsksTheProviderOnEveryAccess()
        {

            var clock   = new FakeTimeProvider();
            var calls   = 0;
            var policy  = new SubnetCheck(() => { calls++; return Interfaces("192.168.1.10/24"); },
                                          clock,
                                          TimeSpan.Zero);

            _ = policy.InterfaceAddresses;
            _ = policy.InterfaceAddresses;
            _ = policy.IsSameSubnet(IPAddress.Parse("192.168.1.10"), IPAddress.Parse("192.168.1.77"));

            Assert.That(calls, Is.EqualTo(3));

        }

        [Test]
        public void InstancePolicy_UsesTheFreshInterfaceListAfterExpiry()
        {

            var clock       = new FakeTimeProvider();
            var interfaces  = new List<InterfaceAddress>(Interfaces("192.168.1.10/24"));
            var policy      = new SubnetCheck(() => interfaces.ToArray(), clock, TimeSpan.FromSeconds(10));

            var local       = IPAddress.Parse("10.5.5.5");
            var remote      = IPAddress.Parse("10.9.9.9");

            Assert.That(policy.IsSameSubnet(local, remote), Is.False, "no IPv4 interface covers 10.0.0.0/8 yet");

            interfaces.Add(Interfaces("10.0.0.1/8")[0]);

            Assert.That(policy.IsSameSubnet(local, remote), Is.False, "the cached list is still used");

            clock.Advance(TimeSpan.FromSeconds(10));

            Assert.That(policy.IsSameSubnet(local, remote), Is.True, "the new interface is seen after the cache expired");

        }

        [Test]
        public void InstancePolicy_RejectsANegativeCacheLifetime()
        {
            Assert.That(() => new SubnetCheck(CacheLifetime: TimeSpan.FromSeconds(-1)), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        #endregion

        #region Instance policy: defaults and the operating system

        [Test]
        public void DefaultConstructor_ReadsTheOperatingSystem_WithoutThrowing()
        {

            var policy = new SubnetCheck();

            Assert.Multiple(() => {
                Assert.That(policy.CacheLifetime,                                            Is.EqualTo(TimeSpan.FromSeconds(10)));
                Assert.That(policy.InterfaceAddresses,                                       Is.Not.Null);
                Assert.That(policy.IsSameSubnet(IPAddress.Loopback, IPAddress.Loopback),     Is.True);
                Assert.That(policy.IsSameSubnet(IPAddress.Loopback, IPAddress.IPv6Loopback), Is.True);
            });

        }

        [Test]
        public void GetInterfaceAddresses_ReturnsAnEnumerableList()
        {

            var addresses = SubnetCheck.GetInterfaceAddresses();

            Assert.That(addresses,          Is.Not.Null);
            Assert.That(addresses.ToList(), Is.Not.Null);

        }

        #endregion


        #region AllowAllSubnetPolicy / DenyAllSubnetPolicy

        [Test]
        public void AllowAllSubnetPolicy_AcceptsEveryPeer()
        {

            var policy = AllowAllSubnetPolicy.Instance;

            Assert.Multiple(() => {
                Assert.That(policy,                                                                              Is.SameAs(AllowAllSubnetPolicy.Instance));
                Assert.That(policy.IsSameSubnet(IPAddress.Parse("192.168.1.10"), IPAddress.Parse("8.8.8.8")),   Is.True);
                Assert.That(policy.IsSameSubnet(IPAddress.Parse("192.168.1.10"), IPAddress.Parse("2001:db8::1")), Is.True);
            });

        }

        [Test]
        public void DenyAllSubnetPolicy_RejectsEveryPeer_EvenLoopback()
        {

            var policy = DenyAllSubnetPolicy.Instance;

            Assert.Multiple(() => {
                Assert.That(policy,                                                                              Is.SameAs(DenyAllSubnetPolicy.Instance));
                Assert.That(policy.IsSameSubnet(IPAddress.Parse("192.168.1.10"), IPAddress.Parse("192.168.1.77")), Is.False);
                Assert.That(policy.IsSameSubnet(IPAddress.Loopback,              IPAddress.Loopback),              Is.False);
            });

        }

        #endregion

    }

}

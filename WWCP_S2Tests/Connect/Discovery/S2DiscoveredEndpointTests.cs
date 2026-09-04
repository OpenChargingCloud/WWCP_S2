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

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect.Discovery
{

    /// <summary>
    /// An S2 Connect endpoint discovered via DNS-SD: the stored and the derived properties,
    /// the subtype flags, copies via With(...), content comparison versus the identity of
    /// the service instance, and the text representation.
    /// </summary>
    [TestFixture]
    public sealed class S2DiscoveredEndpointTests
    {

        #region Data

        private static readonly DNSServiceName  InstanceName    = DNSServiceName.Parse("hp._s2connect._tcp.local.");
        private static readonly DomainName      HostName        = DomainName.Parse("hp.local.");
        private static readonly IPPort          DefaultPort     = IPPort.Parse(8443);
        private static readonly IPv4Address     Address4        = IPv4Address.Parse("10.0.0.7");
        private static readonly IPv6Address     Address6        = IPv6Address.Parse("fd00::7");
        private static readonly S2BaseURL       PairingUrl      = S2BaseURL.Parse("https://hp.local:8443/pairing/");
        private static readonly S2BaseURL       LongPollingUrl  = S2BaseURL.Parse("https://hp.local:8443/longpolling/");
        private static readonly URL             LogoUrl         = URL.Parse("https://hp.local/logo.png");
        private static readonly DateTimeOffset  LastSeen        = new (2026, 9, 4, 10, 0, 0, TimeSpan.Zero);

        private static S2DNSSDTXTRecord TXTRecord(String?     Name          = "Heat pump",
                                                  S2BaseURL?  LongPolling   = null)
            => new (PairingUrl,
                    LongPolling,
                    Name,
                    LogoUrl);

        private static S2DiscoveredEndpoint Endpoint(IIPAddress[]?      Addresses   = null,
                                                     S2DNSSDTXTRecord?  Record      = null,
                                                     Boolean?           HostsCEM    = false,
                                                     Boolean?           HostsRM     = true,
                                                     IPPort?            Port        = null,
                                                     DNSServiceName?    Instance    = null)
            => new (Instance  ?? InstanceName,
                    HostName,
                    Port      ?? DefaultPort,
                    Addresses ?? [ Address4, Address6 ],
                    Record    ?? TXTRecord(),
                    HostsCEM,
                    HostsRM,
                    LastSeen);

        #endregion


        #region Constructor and properties

        [Test]
        public void Constructor_StoresThePropertiesAndDeduplicatesTheAddresses()
        {

            var endpoint = new S2DiscoveredEndpoint(
                               InstanceName,
                               HostName,
                               DefaultPort,
                               [ Address4, Address6, Address4, IPv6Address.Parse("fd00::7") ],
                               TXTRecord(),
                               true,
                               false,
                               LastSeen
                           );

            Assert.Multiple(() => {
                Assert.That(endpoint.InstanceName.FullName,  Is.EqualTo("hp._s2connect._tcp.local."));
                Assert.That(endpoint.HostName.FullName,      Is.EqualTo("hp.local."));
                Assert.That(endpoint.Port,                   Is.EqualTo(IPPort.Parse(8443)));
                Assert.That(endpoint.Addresses,              Has.Count.EqualTo(2));
                Assert.That(endpoint.Addresses,              Is.EqualTo(new IIPAddress[] { Address4, Address6 }), "duplicates are dropped, the order is kept");
                Assert.That(endpoint.TXT,                    Is.EqualTo(TXTRecord()));
                Assert.That(endpoint.HostsCEM,               Is.True);
                Assert.That(endpoint.HostsRM,                Is.False);
                Assert.That(endpoint.LastSeen,               Is.EqualTo(LastSeen));
            });

        }

        [Test]
        public void Constructor_RejectsNullArguments()
        {
            Assert.Multiple(() => {
                Assert.That(() => new S2DiscoveredEndpoint(null!,        HostName, DefaultPort, [ Address4 ], TXTRecord(), null, null, LastSeen), Throws.ArgumentNullException);
                Assert.That(() => new S2DiscoveredEndpoint(InstanceName, null!,    DefaultPort, [ Address4 ], TXTRecord(), null, null, LastSeen), Throws.ArgumentNullException);
                Assert.That(() => new S2DiscoveredEndpoint(InstanceName, HostName, DefaultPort, null!,        TXTRecord(), null, null, LastSeen), Throws.ArgumentNullException);
                Assert.That(() => new S2DiscoveredEndpoint(InstanceName, HostName, DefaultPort, [ Address4 ], null!,       null, null, LastSeen), Throws.ArgumentNullException);
                Assert.That(new S2DiscoveredEndpoint(InstanceName, HostName, DefaultPort, [], TXTRecord(), null, null, LastSeen).Addresses, Is.Empty, "an endpoint without addresses is allowed");
            });
        }

        [Test]
        public void DerivedProperties_ComeFromTheTXTRecord()
        {

            var full     = Endpoint(Record: TXTRecord("Heat pump", LongPollingUrl));
            var minimal  = Endpoint(Record: new S2DNSSDTXTRecord(LongPollingUrl: LongPollingUrl));

            Assert.Multiple(() => {

                Assert.That(full.PairingUrl,                         Is.EqualTo(PairingUrl));
                Assert.That(full.LongPollingUrl,                     Is.EqualTo(LongPollingUrl));
                Assert.That(full.Name,                               Is.EqualTo("Heat pump"));
                Assert.That(full.EndpointDescription.Name,           Is.EqualTo("Heat pump"));
                Assert.That(full.EndpointDescription.LogoUrl,        Is.EqualTo(LogoUrl));
                Assert.That(full.EndpointDescription.Deployment,     Is.EqualTo(Deployment.LAN));
                Assert.That(full.EndpointDescription,                Is.EqualTo(new EndpointDescription("Heat pump", LogoUrl, Deployment.LAN)));

                Assert.That(minimal.PairingUrl,                      Is.Null);
                Assert.That(minimal.LongPollingUrl,                  Is.EqualTo(LongPollingUrl));
                Assert.That(minimal.Name,                            Is.Null);
                Assert.That(minimal.EndpointDescription.Name,        Is.Null);
                Assert.That(minimal.EndpointDescription.LogoUrl,     Is.Null);
                Assert.That(minimal.EndpointDescription.Deployment,  Is.EqualTo(Deployment.LAN));

            });

        }

        [Test]
        [S2C("Discovery.Subtypes")]
        public void HostsRole_ReturnsTheSubtypeFlags()
        {

            var rm       = Endpoint(HostsCEM: false, HostsRM: true);
            var unknown  = Endpoint(HostsCEM: null,  HostsRM: null);
            var both     = Endpoint(HostsCEM: true,  HostsRM: true);

            Assert.Multiple(() => {

                Assert.That(rm.HostsRole(EnergyManagementRole.CEM),               Is.False);
                Assert.That(rm.HostsRole(EnergyManagementRole.RM),                Is.True);

                Assert.That(unknown.HostsCEM,                                     Is.Null);
                Assert.That(unknown.HostsRM,                                      Is.Null);
                Assert.That(unknown.HostsRole(EnergyManagementRole.CEM),          Is.Null);
                Assert.That(unknown.HostsRole(EnergyManagementRole.RM),           Is.Null);

                Assert.That(both.HostsRole(EnergyManagementRole.CEM),             Is.True);
                Assert.That(both.HostsRole(EnergyManagementRole.RM),              Is.True);
                Assert.That(both.HostsRole(EnergyManagementRole.Parse("GRID")),   Is.Null, "unknown roles are never hosted");

            });

        }

        #endregion

        #region With(...) and SameContentAs(...)

        [Test]
        public void With_ReplacesOnlyTheGivenValues()
        {

            var original   = Endpoint();
            var later      = LastSeen + TimeSpan.FromMinutes(5);
            var otherTXT   = TXTRecord("Boiler");
            var otherHost  = DomainName.Parse("pump.local.");

            var copy       = original.With();
            var updated    = original.With(Addresses: [ Address4 ], TXT: otherTXT, HostsCEM: true, LastSeen: later);
            var moved      = original.With(HostName: otherHost, Port: IPPort.Parse(8444));

            Assert.Multiple(() => {

                Assert.That(copy,                          Is.Not.SameAs(original));
                Assert.That(copy.SameContentAs(original),  Is.True);
                Assert.That(copy.LastSeen,                 Is.EqualTo(LastSeen));

                Assert.That(updated.InstanceName,          Is.EqualTo(InstanceName));
                Assert.That(updated.HostName,              Is.EqualTo(HostName));
                Assert.That(updated.Port,                  Is.EqualTo(DefaultPort));
                Assert.That(updated.Addresses,             Is.EqualTo(new IIPAddress[] { Address4 }));
                Assert.That(updated.TXT,                   Is.SameAs(otherTXT));
                Assert.That(updated.Name,                  Is.EqualTo("Boiler"));
                Assert.That(updated.HostsCEM,              Is.True);
                Assert.That(updated.HostsRM,               Is.True, "not given, therefore kept");
                Assert.That(updated.LastSeen,              Is.EqualTo(later));

                Assert.That(moved.InstanceName,            Is.EqualTo(InstanceName), "the identity of the service instance never changes");
                Assert.That(moved.HostName,                Is.EqualTo(otherHost));
                Assert.That(moved.Port,                    Is.EqualTo(IPPort.Parse(8444)));
                Assert.That(moved.Addresses,               Is.EqualTo(original.Addresses));
                Assert.That(moved.TXT,                     Is.SameAs(original.TXT));
                Assert.That(moved.HostsCEM,                Is.EqualTo(original.HostsCEM));
                Assert.That(moved.HostsRM,                 Is.EqualTo(original.HostsRM));
                Assert.That(moved.LastSeen,                Is.EqualTo(LastSeen));

                // The original is untouched.
                Assert.That(original.HostsCEM,             Is.False);
                Assert.That(original.Name,                 Is.EqualTo("Heat pump"));
                Assert.That(original.Addresses,            Has.Count.EqualTo(2));

                // A flag can not be reset to "unknown" through With(...): null means "keep".
                Assert.That(original.With(HostsCEM: null, HostsRM: null).HostsCEM,  Is.False);
                Assert.That(original.With(HostsCEM: null, HostsRM: null).HostsRM,   Is.True);

            });

        }

        [Test]
        public void SameContentAs_ComparesEverythingExceptTheTimestamp()
        {

            var endpoint = Endpoint([ Address4, Address6 ]);

            Assert.Multiple(() => {
                Assert.That(endpoint.SameContentAs(endpoint),                                                              Is.True);
                Assert.That(endpoint.SameContentAs(Endpoint([ Address6, Address4 ])),                                      Is.True,  "the order of the addresses does not matter");
                Assert.That(endpoint.SameContentAs(endpoint.With(LastSeen: LastSeen + TimeSpan.FromHours(1))),             Is.True,  "the timestamp does not matter");
                Assert.That(endpoint.SameContentAs(Endpoint(Record: TXTRecord("Boiler"))),                                 Is.False, "the TXT record differs");
                Assert.That(endpoint.SameContentAs(Endpoint(Port: IPPort.Parse(8444))),                                    Is.False, "the port differs");
                Assert.That(endpoint.SameContentAs(Endpoint(HostsCEM: true)),                                              Is.False, "the CEM flag differs");
                Assert.That(endpoint.SameContentAs(Endpoint(HostsRM: null)),                                               Is.False, "the RM flag differs");
                Assert.That(endpoint.SameContentAs(Endpoint([ Address4 ])),                                                Is.False, "the addresses differ");
                Assert.That(endpoint.SameContentAs(endpoint.With(HostName: DomainName.Parse("pump.local."))),              Is.False, "the host differs");
                Assert.That(endpoint.SameContentAs(Endpoint(Instance: DNSServiceName.Parse("pump._s2connect._tcp.local."))), Is.False, "the service instance differs");
                Assert.That(endpoint.SameContentAs(null),                                                                  Is.False);
            });

        }

        #endregion

        #region Equality and ToString()

        [Test]
        public void Equality_IsByServiceInstanceNameOnly()
        {

            var a              = Endpoint();
            var differentPort  = Endpoint(Port: IPPort.Parse(8444));
            var differentTXT   = Endpoint(Record: TXTRecord("Boiler"), HostsCEM: true);
            var differentCase  = Endpoint(Instance: DNSServiceName.Parse("HP._S2CONNECT._TCP.LOCAL."));
            var other          = Endpoint(Instance: DNSServiceName.Parse("pump._s2connect._tcp.local."));

            Assert.Multiple(() => {

                Assert.That(a,                                Is.EqualTo(differentPort));
                Assert.That(a.GetHashCode(),                  Is.EqualTo(differentPort.GetHashCode()));
                Assert.That(a.SameContentAs(differentPort),   Is.False, "equal, but not the same content");

                Assert.That(a,                                Is.EqualTo(differentTXT));
                Assert.That(a.GetHashCode(),                  Is.EqualTo(differentTXT.GetHashCode()));

                Assert.That(a,                                Is.EqualTo(differentCase), "service instance names are case-insensitive");
                Assert.That(a.GetHashCode(),                  Is.EqualTo(differentCase.GetHashCode()));

                Assert.That(a,                                Is.Not.EqualTo(other));
                Assert.That(a.Equals((Object) differentPort), Is.True);
                Assert.That(a.Equals("hp"),                   Is.False);

                Assert.That(new HashSet<S2DiscoveredEndpoint> { a, differentPort, differentTXT, differentCase, other }, Has.Count.EqualTo(2));

            });

        }

        [Test]
        public void Operators_HandleNull()
        {

            var a  = Endpoint();
            var b  = Endpoint(Port: IPPort.Parse(8444));
            var c  = Endpoint(Instance: DNSServiceName.Parse("pump._s2connect._tcp.local."));

            S2DiscoveredEndpoint? none = null;

            Assert.Multiple(() => {

                Assert.That(a == b,         Is.True);
                Assert.That(a != b,         Is.False);
                Assert.That(a == c,         Is.False);
                Assert.That(a != c,         Is.True);

                Assert.That(a == none,      Is.False);
                Assert.That(none == a,      Is.False);
                Assert.That(a != none,      Is.True);
                Assert.That(none != a,      Is.True);
                Assert.That(none == null,   Is.True);
                Assert.That(a.Equals(none), Is.False);

            });

        }

        [Test]
        public void ToString_MentionsNameHostAddressesAndRoles()
        {

            var rm = Endpoint(HostsCEM: false, HostsRM: true).ToString();

            Assert.Multiple(() => {
                Assert.That(rm, Does.Contain("'Heat pump'"));
                Assert.That(rm, Does.Contain("hp._s2connect._tcp.local."));
                Assert.That(rm, Does.Contain("hp.local.:8443"));
                Assert.That(rm, Does.Contain("10.0.0.7"));
                Assert.That(rm, Does.Contain(" RM"));
                Assert.That(rm, Does.Not.Contain("CEM"));
                Assert.That(rm, Does.Contain("pairing: https://hp.local:8443/pairing/"));
                Assert.That(rm, Does.Not.Contain("long-polling"));
            });

            var cem = Endpoint(Record: new S2DNSSDTXTRecord(PairingUrl, LongPollingUrl), HostsCEM: true, HostsRM: false).ToString();

            Assert.Multiple(() => {
                Assert.That(cem, Does.Not.Contain("'"), "no name, no quotes");
                Assert.That(cem, Does.StartWith("hp._s2connect._tcp.local."));
                Assert.That(cem, Does.Contain(" CEM"));
                Assert.That(cem, Does.Not.Contain(" RM"));
                Assert.That(cem, Does.Contain("long-polling: https://hp.local:8443/longpolling/"));
            });

            var unknown = Endpoint(HostsCEM: null, HostsRM: null).ToString();

            Assert.Multiple(() => {
                Assert.That(unknown, Does.Not.Contain("CEM"));
                Assert.That(unknown, Does.Not.Contain(" RM"));
            });

        }

        #endregion

    }

}

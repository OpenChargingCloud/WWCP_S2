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
    /// What an S2 Connect endpoint advertises via DNS-SD (S2 Connect 1.0.0, "DNS-SD based discovery";
    /// RFC 6763 §4 to §6, RFC 6762 §10): the mDNS host name rules, the service instance and
    /// subtype names, the derivation from a local endpoint, the resource records with their
    /// recommended times-to-live, and equality.
    /// </summary>
    [TestFixture]
    public sealed class S2ServiceAdvertisementTests
    {

        #region Data

        private static readonly DomainName   HeatPumpHost            = DomainName.Parse("hp.local.");
        private static readonly IPPort       HeatPumpPort            = IPPort.Parse(8443);
        private static readonly S2BaseURL    HeatPumpPairingUrl      = S2BaseURL.Parse("https://hp.local:8443/pairing/");
        private static readonly S2BaseURL    HeatPumpLongPollingUrl  = S2BaseURL.Parse("https://hp.local:8443/longpolling/");
        private static readonly URL          HeatPumpLogo            = URL.Parse("https://hp.local/logo.png");
        private static readonly IPv4Address  Address4                = IPv4Address.Parse("10.0.0.7");
        private static readonly IPv6Address  Address6                = IPv6Address.Parse("fd00::7");

        private static S2DNSSDTXTRecord TXTRecord(String? Name = "Heat pump")
            => new (HeatPumpPairingUrl,
                    EndpointName:  Name,
                    LogoUrl:       HeatPumpLogo);

        private static S2ServiceAdvertisement Advertisement(EnergyManagementRole[]?  Roles       = null,
                                                            IIPAddress[]?            Addresses   = null,
                                                            IPPort?                  Port        = null)
            => new (HeatPumpHost,
                    Port  ?? HeatPumpPort,
                    TXTRecord(),
                    Roles ?? [ EnergyManagementRole.CEM, EnergyManagementRole.RM ],
                    Addresses);

        private static LocalEndpoint HeatPump(String   PairingUrl   = "https://hp.local/pairing/",
                                              Boolean  AllowHTTP    = false)
            => new (new EndpointDescription("Heat pump", HeatPumpLogo),
                    Deployment.LAN,
                    S2BaseURL.Parse(PairingUrl, AllowHTTP));

        private static NodeDescription Node(EnergyManagementRole Role)
            => new (Node_Id.NewRandom, "GraphDefined", "EMS", $"Test{Role} 1", Role);

        #endregion


        #region Constructor and names

        [Test]
        [S2C("Discovery.ServiceName")]
        public void Constructor_RequiresAnMDNSHostName()
        {

            Assert.Multiple(() => {
                Assert.That(() => new S2ServiceAdvertisement(DomainName.Parse("hp.example.com."), HeatPumpPort, TXTRecord()), Throws.ArgumentException);
                Assert.That(() => new S2ServiceAdvertisement(DomainName.Parse("hp.localdomain."), HeatPumpPort, TXTRecord()), Throws.ArgumentException);
                Assert.That(() => new S2ServiceAdvertisement(DomainName.Parse("hp."),             HeatPumpPort, TXTRecord()), Throws.ArgumentException);
                Assert.That(() => new S2ServiceAdvertisement(DomainName.Parse("local."),          HeatPumpPort, TXTRecord()), Throws.ArgumentException, "a label before '.local' is required");
            });

            var withoutDot  = new S2ServiceAdvertisement(DomainName.Parse("hp.local"),         HeatPumpPort, TXTRecord());
            var upperCase   = new S2ServiceAdvertisement(DomainName.Parse("HP.LOCAL."),        HeatPumpPort, TXTRecord());
            var subdomain   = new S2ServiceAdvertisement(DomainName.Parse("kitchen.hp.local."), HeatPumpPort, TXTRecord());

            Assert.Multiple(() => {
                Assert.That(withoutDot.HostName.FullName,   Is.EqualTo("hp.local."));
                Assert.That(withoutDot.InstanceName,        Is.EqualTo(DNSServiceName.Parse("hp._s2connect._tcp.local.")));
                Assert.That(upperCase.InstanceName,         Is.EqualTo(DNSServiceName.Parse("hp._s2connect._tcp.local.")), "names are case-insensitive");
                Assert.That(subdomain.InstanceName.FullName, Is.EqualTo("kitchen._s2connect._tcp.local."), "the first label is the instance name");
            });

        }

        [Test]
        public void Constructor_RejectsNullArguments()
        {
            Assert.Multiple(() => {
                Assert.That(() => new S2ServiceAdvertisement(null!,        HeatPumpPort, TXTRecord()), Throws.ArgumentNullException);
                Assert.That(() => new S2ServiceAdvertisement(HeatPumpHost, HeatPumpPort, null!),       Throws.ArgumentNullException);
            });
        }

        [Test]
        public void Constructor_RejectsAnEmptyAddressList()
        {
            Assert.Multiple(() => {
                Assert.That(() => new S2ServiceAdvertisement(HeatPumpHost, HeatPumpPort, TXTRecord(), Addresses: []), Throws.ArgumentException);
                Assert.That(Advertisement().Addresses,                        Is.Null, "no explicit addresses by default");
                Assert.That(Advertisement(Addresses: [ Address4 ]).Addresses, Is.EqualTo(new IIPAddress[] { Address4 }));
            });
        }

        [Test]
        [S2C("Discovery.Subtypes")]
        public void Constructor_RejectsUnknownRoles()
        {
            Assert.Multiple(() => {
                Assert.That(() => new S2ServiceAdvertisement(HeatPumpHost, HeatPumpPort, TXTRecord(), [ EnergyManagementRole.Parse("GRID") ]),              Throws.ArgumentException);
                Assert.That(() => new S2ServiceAdvertisement(HeatPumpHost, HeatPumpPort, TXTRecord(), [ EnergyManagementRole.CEM, default(EnergyManagementRole) ]), Throws.ArgumentException);
                Assert.That(() => S2ServiceAdvertisement.SubtypeName(EnergyManagementRole.Parse("GRID")),                                                    Throws.ArgumentException);
            });
        }

        [Test]
        [S2C("Discovery.ServiceName")]
        public void InstanceName_IsTheFirstLabelOfTheHostNameBelowTheServiceType()
        {

            var advertisement = Advertisement();

            Assert.Multiple(() => {
                Assert.That(advertisement.HostName.FullName,      Is.EqualTo("hp.local."));
                Assert.That(advertisement.InstanceName.FullName,  Is.EqualTo("hp._s2connect._tcp.local."));
                Assert.That(advertisement.Port,                   Is.EqualTo(IPPort.Parse(8443)));
                Assert.That(advertisement.TXT,                    Is.EqualTo(TXTRecord()));
                Assert.That(new S2ServiceAdvertisement(DomainName.Parse("my-heat-pump.local"), HeatPumpPort, TXTRecord()).InstanceName.FullName, Is.EqualTo("my-heat-pump._s2connect._tcp.local."));
            });

        }

        [Test]
        [S2C("Discovery.ServiceType")]
        public void ServiceTypeName_IsS2ConnectOverTCPWithinTheLocalDomain()
        {
            Assert.Multiple(() => {
                Assert.That(S2ServiceAdvertisement.ServiceTypeName.FullName, Is.EqualTo("_s2connect._tcp.local."));
                Assert.That(S2ServiceAdvertisement.ServiceTypeName.FullName, Is.EqualTo($"{S2ConnectDefaults.DNSSDServiceType}.local."));
                Assert.That(S2ServiceAdvertisement.ServiceTypeName.Labels,   Is.EqualTo(new[] { "_s2connect", "_tcp", "local" }));
            });
        }

        [Test]
        [S2C("Discovery.Subtypes")]
        public void SubtypeNames_AreCEMAndRM()
        {
            Assert.Multiple(() => {
                Assert.That(S2ServiceAdvertisement.SubtypeName(EnergyManagementRole.CEM).FullName, Is.EqualTo("_cem._sub._s2connect._tcp.local."));
                Assert.That(S2ServiceAdvertisement.SubtypeName(EnergyManagementRole.RM). FullName, Is.EqualTo("_rm._sub._s2connect._tcp.local."));
                Assert.That(S2ServiceAdvertisement.CEMSubtypeName,                                 Is.EqualTo(S2ServiceAdvertisement.SubtypeName(EnergyManagementRole.CEM)));
                Assert.That(S2ServiceAdvertisement.RMSubtypeName,                                  Is.EqualTo(S2ServiceAdvertisement.SubtypeName(EnergyManagementRole.RM)));
                Assert.That(S2ServiceAdvertisement.CEMSubtypeName.FullName,                        Does.StartWith(S2ConnectDefaults.DNSSDSubtypeCEM + "._sub."));
                Assert.That(S2ServiceAdvertisement.RMSubtypeName. FullName,                        Does.StartWith(S2ConnectDefaults.DNSSDSubtypeRM  + "._sub."));
                Assert.That(S2ServiceAdvertisement.CEMSubtypeName.FullName,                        Does.EndWith(S2ServiceAdvertisement.ServiceTypeName.FullName));
                Assert.That(S2ServiceAdvertisement.RMSubtypeName. FullName,                        Does.EndWith(S2ServiceAdvertisement.ServiceTypeName.FullName));
            });
        }

        [Test]
        public void Roles_AreDeduplicated()
        {

            var advertisement = Advertisement([ EnergyManagementRole.RM, EnergyManagementRole.CEM, EnergyManagementRole.RM, EnergyManagementRole.CEM ]);

            Assert.Multiple(() => {
                Assert.That(advertisement.Roles,                                                            Has.Count.EqualTo(2));
                Assert.That(advertisement.Roles,                                                            Is.EquivalentTo(new[] { EnergyManagementRole.CEM, EnergyManagementRole.RM }));
                Assert.That(Advertisement([ EnergyManagementRole.RM, EnergyManagementRole.RM ]).Roles,      Is.EqualTo(new[] { EnergyManagementRole.RM }));
                Assert.That(Advertisement([]).Roles,                                                        Is.Empty);
                Assert.That(new S2ServiceAdvertisement(HeatPumpHost, HeatPumpPort, TXTRecord()).Roles,      Is.Empty);
            });

        }

        #endregion

        #region FromLocalEndpoint(...)

        [Test]
        [S2C("Discovery.TXT.URLs")]
        public void FromLocalEndpoint_TakesHostPortAndTXTFromThePairingURL()
        {

            var endpoint       = HeatPump("https://hp.local/pairing/");
            var advertisement  = S2ServiceAdvertisement.FromLocalEndpoint(endpoint);

            Assert.Multiple(() => {
                Assert.That(advertisement.HostName.FullName,      Is.EqualTo("hp.local."));
                Assert.That(advertisement.InstanceName.FullName,  Is.EqualTo("hp._s2connect._tcp.local."));
                Assert.That(advertisement.Port,                   Is.EqualTo(IPPort.HTTPS));
                Assert.That(advertisement.Port.ToUInt16(),        Is.EqualTo((UInt16) 443));
                Assert.That(advertisement.TXT.PairingUrl,         Is.EqualTo(endpoint.PairingUrl));
                Assert.That(advertisement.TXT.PairingUrl?.Value,  Is.EqualTo("https://hp.local/pairing/"));
                Assert.That(advertisement.TXT.LongPollingUrl,     Is.Null);
                Assert.That(advertisement.TXT.EndpointName,       Is.EqualTo("Heat pump"));
                Assert.That(advertisement.TXT.LogoUrl,            Is.EqualTo(HeatPumpLogo));
                Assert.That(advertisement.TXT.ToStrings(),        Is.EqualTo(new[] { "txtver=1", "e_name=Heat pump", "e_logoUrl=https://hp.local/logo.png", "pairingUrl=https://hp.local/pairing/" }));
                Assert.That(advertisement.Roles,                  Is.Empty);
                Assert.That(advertisement.Addresses,              Is.Null);
            });

        }

        [Test]
        public void FromLocalEndpoint_UsesThePortOfThePairingURL()
        {
            Assert.Multiple(() => {
                Assert.That(S2ServiceAdvertisement.FromLocalEndpoint(HeatPump("http://hp.local:8080/pairing/", AllowHTTP: true)).Port,                Is.EqualTo(IPPort.Parse(8080)));
                Assert.That(S2ServiceAdvertisement.FromLocalEndpoint(HeatPump("http://hp.local/pairing/",      AllowHTTP: true)).Port,                Is.EqualTo(IPPort.HTTP));
                Assert.That(S2ServiceAdvertisement.FromLocalEndpoint(HeatPump("https://hp.local:8443/pairing/")).Port,                                Is.EqualTo(IPPort.Parse(8443)));
                Assert.That(S2ServiceAdvertisement.FromLocalEndpoint(HeatPump("https://hp.local:8443/pairing/")).TXT.PairingUrl?.Value,               Is.EqualTo("https://hp.local:8443/pairing/"));
                Assert.That(S2ServiceAdvertisement.FromLocalEndpoint(HeatPump("https://hp.local:8443/pairing/"), Port: IPPort.Parse(9443)).Port,      Is.EqualTo(IPPort.Parse(9443)), "an explicit port wins");
            });
        }

        [Test]
        public void FromLocalEndpoint_OmitsAnAbsentNameAndLogo()
        {

            var anonymous      = new LocalEndpoint(new EndpointDescription(), Deployment.LAN, S2BaseURL.Parse("https://hp.local/pairing/"));
            var advertisement  = S2ServiceAdvertisement.FromLocalEndpoint(anonymous);

            Assert.Multiple(() => {
                Assert.That(advertisement.TXT.EndpointName,  Is.Null);
                Assert.That(advertisement.TXT.LogoUrl,       Is.Null);
                Assert.That(advertisement.TXT.ToStrings(),   Is.EqualTo(new[] { "txtver=1", "pairingUrl=https://hp.local/pairing/" }));
            });

        }

        [Test]
        [S2C("Discovery.Subtypes")]
        public void FromLocalEndpoint_TakesTheRolesFromTheHostedNodes()
        {

            var endpoint = HeatPump();

            Assert.That(S2ServiceAdvertisement.FromLocalEndpoint(endpoint).Roles, Is.Empty, "no nodes, no subtypes");

            endpoint.AddNode(Node(EnergyManagementRole.RM));
            Assert.That(S2ServiceAdvertisement.FromLocalEndpoint(endpoint).Roles, Is.EqualTo(new[] { EnergyManagementRole.RM }));

            endpoint.AddNode(Node(EnergyManagementRole.RM));
            Assert.That(S2ServiceAdvertisement.FromLocalEndpoint(endpoint).Roles, Is.EqualTo(new[] { EnergyManagementRole.RM }), "two RM nodes are still one subtype");

            endpoint.AddNode(Node(EnergyManagementRole.CEM));
            Assert.That(S2ServiceAdvertisement.FromLocalEndpoint(endpoint).Roles, Is.EquivalentTo(new[] { EnergyManagementRole.CEM, EnergyManagementRole.RM }));

            // Explicit roles override the hosted nodes.
            Assert.Multiple(() => {
                Assert.That(S2ServiceAdvertisement.FromLocalEndpoint(endpoint, Roles: [ EnergyManagementRole.CEM ]).Roles, Is.EqualTo(new[] { EnergyManagementRole.CEM }));
                Assert.That(S2ServiceAdvertisement.FromLocalEndpoint(endpoint, Roles: []).Roles,                            Is.Empty);
            });

        }

        [Test]
        public void FromLocalEndpoint_AcceptsExplicitOverrides()
        {

            var endpoint = HeatPump();
            endpoint.AddNode(Node(EnergyManagementRole.CEM));

            var advertisement = S2ServiceAdvertisement.FromLocalEndpoint(
                                    endpoint,
                                    LongPollingUrl:  HeatPumpLongPollingUrl,
                                    Roles:           [ EnergyManagementRole.RM ],
                                    HostName:        DomainName.Parse("pump.local."),
                                    Port:            IPPort.Parse(8443),
                                    Addresses:       [ Address4, Address6 ]
                                );

            Assert.Multiple(() => {
                Assert.That(advertisement.HostName.FullName,      Is.EqualTo("pump.local."));
                Assert.That(advertisement.InstanceName.FullName,  Is.EqualTo("pump._s2connect._tcp.local."));
                Assert.That(advertisement.Port,                   Is.EqualTo(IPPort.Parse(8443)));
                Assert.That(advertisement.TXT.PairingUrl,         Is.EqualTo(endpoint.PairingUrl), "the pairing URL always comes from the endpoint");
                Assert.That(advertisement.TXT.LongPollingUrl,     Is.EqualTo(HeatPumpLongPollingUrl));
                Assert.That(advertisement.TXT.EndpointName,       Is.EqualTo("Heat pump"));
                Assert.That(advertisement.Roles,                  Is.EqualTo(new[] { EnergyManagementRole.RM }));
                Assert.That(advertisement.Addresses,              Is.EqualTo(new IIPAddress[] { Address4, Address6 }));
            });

        }

        [Test]
        [S2C("Discovery.ServiceName")]
        public void FromLocalEndpoint_RequiresALocalHostNameUnlessOverridden()
        {

            var wanLike = HeatPump("https://hp.example.com/pairing/");

            Assert.That(() => S2ServiceAdvertisement.FromLocalEndpoint(wanLike), Throws.ArgumentException);

            var advertisement = S2ServiceAdvertisement.FromLocalEndpoint(wanLike, HostName: DomainName.Parse("hp.local."));

            Assert.Multiple(() => {
                Assert.That(advertisement.HostName.FullName,      Is.EqualTo("hp.local."));
                Assert.That(advertisement.InstanceName.FullName,  Is.EqualTo("hp._s2connect._tcp.local."));
                Assert.That(advertisement.TXT.PairingUrl?.Value,  Is.EqualTo("https://hp.example.com/pairing/"), "the TXT record still announces the pairing URL of the endpoint");
            });

            // An override that is not an mDNS name either is rejected as well.
            Assert.That(() => S2ServiceAdvertisement.FromLocalEndpoint(wanLike, HostName: DomainName.Parse("hp.example.org.")), Throws.ArgumentException);

        }

        #endregion

        #region ToResourceRecords(...)

        [Test]
        [S2C("Discovery.Records")]
        public void ToResourceRecords_ContainsThePTRsTheSRVTheTXTAndTheAddressRecords()
        {

            // The roles are given in reverse order to show that the subtypes are emitted in a stable order (CEM before RM).
            var advertisement  = Advertisement([ EnergyManagementRole.RM, EnergyManagementRole.CEM ]);
            var records        = advertisement.ToResourceRecords([ Address4, Address6 ]);

            Assert.That(records, Has.Count.EqualTo(7));

            Assert.Multiple(() => {

                Assert.That(records[0], Is.InstanceOf<PTR>());
                Assert.That(records[1], Is.InstanceOf<PTR>());
                Assert.That(records[2], Is.InstanceOf<PTR>());
                Assert.That(records[3], Is.InstanceOf<SRV>());
                Assert.That(records[4], Is.InstanceOf<TXT>());
                Assert.That(records[5], Is.InstanceOf<A>());
                Assert.That(records[6], Is.InstanceOf<AAAA>());

                Assert.That(records.Select(record => record.Type), Is.EqualTo(new[] {
                    DNSResourceRecordTypes.PTR,
                    DNSResourceRecordTypes.PTR,
                    DNSResourceRecordTypes.PTR,
                    DNSResourceRecordTypes.SRV,
                    DNSResourceRecordTypes.TXT,
                    DNSResourceRecordTypes.A,
                    DNSResourceRecordTypes.AAAA
                }));

                Assert.That(records.Select(record => record.Class), Is.All.EqualTo(DNSQueryClasses.IN));

            });

            var serviceType  = (PTR)  records[0];
            var cemSubtype   = (PTR)  records[1];
            var rmSubtype    = (PTR)  records[2];
            var srv          = (SRV)  records[3];
            var txt          = (TXT)  records[4];
            var a            = (A)    records[5];
            var aaaa         = (AAAA) records[6];

            Assert.Multiple(() => {

                Assert.That(serviceType.DomainName.FullName,  Is.EqualTo("_s2connect._tcp.local."));
                Assert.That(serviceType.Target.FullName,      Is.EqualTo("hp._s2connect._tcp.local."));

                Assert.That(cemSubtype.DomainName.FullName,   Is.EqualTo("_cem._sub._s2connect._tcp.local."));
                Assert.That(cemSubtype.Target.FullName,       Is.EqualTo("hp._s2connect._tcp.local."));

                Assert.That(rmSubtype.DomainName.FullName,    Is.EqualTo("_rm._sub._s2connect._tcp.local."));
                Assert.That(rmSubtype.Target.FullName,        Is.EqualTo("hp._s2connect._tcp.local."));

                Assert.That(srv.DomainName.FullName,          Is.EqualTo("hp._s2connect._tcp.local."));
                Assert.That(srv.Priority,                     Is.Zero);
                Assert.That(srv.Weight,                       Is.Zero);
                Assert.That(srv.Port,                         Is.EqualTo(IPPort.Parse(8443)));
                Assert.That(srv.Target.FullName,              Is.EqualTo("hp.local."));

                Assert.That(txt.DomainName.FullName,          Is.EqualTo("hp._s2connect._tcp.local."));
                Assert.That(txt.Strings,                      Is.EqualTo(advertisement.TXT.ToStrings()));
                Assert.That(txt.KeyValues["pairingUrl"],      Is.EqualTo("https://hp.local:8443/pairing/"));

                Assert.That(a.DomainName.FullName,            Is.EqualTo("hp.local."));
                Assert.That(a.IPv4Address,                    Is.EqualTo(Address4));

                Assert.That(aaaa.DomainName.FullName,         Is.EqualTo("hp.local."));
                Assert.That(aaaa.IPv6Address,                 Is.EqualTo(Address6));

            });

        }

        [Test]
        [S2C("Discovery.Subtypes")]
        public void ToResourceRecords_OmitsTheSubtypesOfMissingRoles()
        {

            var rmOnly   = Advertisement([ EnergyManagementRole.RM ]).ToResourceRecords([ Address4 ]);
            var noRoles  = Advertisement([]).                         ToResourceRecords([ Address4 ]);

            Assert.Multiple(() => {

                Assert.That(rmOnly,                                                            Has.Count.EqualTo(5));
                Assert.That(rmOnly. OfType<PTR>().Select(ptr => ptr.DomainName.FullName),      Is.EqualTo(new[] { "_s2connect._tcp.local.", "_rm._sub._s2connect._tcp.local." }));
                Assert.That(rmOnly. OfType<AAAA>(),                                            Is.Empty);

                Assert.That(noRoles,                                                           Has.Count.EqualTo(4));
                Assert.That(noRoles.OfType<PTR>().Select(ptr => ptr.DomainName.FullName),      Is.EqualTo(new[] { "_s2connect._tcp.local." }));
                Assert.That(noRoles.Select(record => record.Type),                             Is.EqualTo(new[] { DNSResourceRecordTypes.PTR, DNSResourceRecordTypes.SRV, DNSResourceRecordTypes.TXT, DNSResourceRecordTypes.A }));

            });

        }

        [Test]
        [S2C("Discovery.Records.TTL")]
        public void ToResourceRecords_UsesTheRecommendedTimesToLive()
        {

            var records = Advertisement().ToResourceRecords([ Address4, Address6 ]);

            Assert.Multiple(() => {

                // RFC 6762 §10: 75 minutes for shared records (PTR, TXT), 120 seconds for records containing a host name (SRV, A, AAAA).
                Assert.That(records.OfType<PTR>().Select(ptr => ptr.TimeToLive),  Is.All.EqualTo(TimeSpan.FromMinutes(75)));
                Assert.That(records.OfType<TXT>().Single().TimeToLive,            Is.EqualTo(TimeSpan.FromMinutes(75)));
                Assert.That(records.OfType<SRV>().Single().TimeToLive,            Is.EqualTo(TimeSpan.FromSeconds(120)));
                Assert.That(records.OfType<A>().   Single().TimeToLive,           Is.EqualTo(TimeSpan.FromSeconds(120)));
                Assert.That(records.OfType<AAAA>().Single().TimeToLive,           Is.EqualTo(TimeSpan.FromSeconds(120)));

                Assert.That(MulticastDNS.SharedRecordTimeToLive,                  Is.EqualTo(TimeSpan.FromMinutes(75)));
                Assert.That(MulticastDNS.HostRecordTimeToLive,                    Is.EqualTo(TimeSpan.FromSeconds(120)));

            });

        }

        [Test]
        public void ToResourceRecords_AcceptsCustomTimesToLive()
        {

            var records = Advertisement().ToResourceRecords([ Address4, Address6 ],
                                                            HostRecordTimeToLive:    TimeSpan.FromSeconds(30),
                                                            SharedRecordTimeToLive:  TimeSpan.FromMinutes(10));

            Assert.Multiple(() => {
                Assert.That(records.OfType<PTR>().Select(ptr => ptr.TimeToLive),  Is.All.EqualTo(TimeSpan.FromMinutes(10)));
                Assert.That(records.OfType<TXT>().Single().TimeToLive,            Is.EqualTo(TimeSpan.FromMinutes(10)));
                Assert.That(records.OfType<SRV>().Single().TimeToLive,            Is.EqualTo(TimeSpan.FromSeconds(30)));
                Assert.That(records.OfType<A>().   Single().TimeToLive,           Is.EqualTo(TimeSpan.FromSeconds(30)));
                Assert.That(records.OfType<AAAA>().Single().TimeToLive,           Is.EqualTo(TimeSpan.FromSeconds(30)));
            });

            // Only one of the two may be customised as well.
            var hostOnly = Advertisement().ToResourceRecords([ Address4 ], HostRecordTimeToLive: TimeSpan.FromSeconds(30));

            Assert.Multiple(() => {
                Assert.That(hostOnly.OfType<PTR>().Select(ptr => ptr.TimeToLive), Is.All.EqualTo(TimeSpan.FromMinutes(75)));
                Assert.That(hostOnly.OfType<A>().Single().TimeToLive,             Is.EqualTo(TimeSpan.FromSeconds(30)));
            });

        }

        [Test]
        public void ToResourceRecords_CollapsesDuplicateAddresses()
        {

            var records = Advertisement().ToResourceRecords([ Address4, Address4, IPv4Address.Parse("10.0.0.7"), Address6, IPv6Address.Parse("fd00::7") ]);

            Assert.Multiple(() => {
                Assert.That(records,                                       Has.Count.EqualTo(7));
                Assert.That(records.OfType<A>().   Count(),                Is.EqualTo(1));
                Assert.That(records.OfType<AAAA>().Count(),                Is.EqualTo(1));
                Assert.That(records.OfType<A>().   Single().IPv4Address,   Is.EqualTo(Address4));
                Assert.That(records.OfType<AAAA>().Single().IPv6Address,   Is.EqualTo(Address6));
            });

            // Different addresses produce one record each.
            var two = Advertisement().ToResourceRecords([ Address4, IPv4Address.Parse("10.0.0.8") ]);

            Assert.That(two.OfType<A>().Select(a => a.IPv4Address), Is.EqualTo(new[] { Address4, IPv4Address.Parse("10.0.0.8") }));

        }

        [Test]
        public void ToResourceRecords_RequiresAddresses()
        {

            var withoutAddresses  = Advertisement();
            var withAddresses     = Advertisement(Addresses: [ Address4 ]);

            Assert.Multiple(() => {
                Assert.That(() => withoutAddresses.ToResourceRecords(),    Throws.ArgumentException);
                Assert.That(() => withoutAddresses.ToResourceRecords([]),  Throws.ArgumentException);
                Assert.That(() => withAddresses.   ToResourceRecords([]),  Throws.ArgumentException, "an explicitly empty list is not a fall back to the explicit addresses");
            });

            Assert.Multiple(() => {
                Assert.That(withAddresses.ToResourceRecords().OfType<A>().Single().IPv4Address,             Is.EqualTo(Address4), "the explicit addresses of the advertisement are the default");
                Assert.That(withAddresses.ToResourceRecords([ Address6 ]).OfType<AAAA>().Single().IPv6Address, Is.EqualTo(Address6), "given addresses win over the explicit addresses");
                Assert.That(withAddresses.ToResourceRecords([ Address6 ]).OfType<A>(),                      Is.Empty);
            });

        }

        #endregion

        #region Equality and ToString()

        [Test]
        public void Equality_ComparesHostPortTXTRolesAndAddresses()
        {

            var a                   = Advertisement([ EnergyManagementRole.CEM, EnergyManagementRole.RM ], [ Address4, Address6 ]);
            var b                   = Advertisement([ EnergyManagementRole.RM,  EnergyManagementRole.CEM ], [ Address6, Address4 ]);
            var upperCaseHost       = new S2ServiceAdvertisement(DomainName.Parse("HP.LOCAL."),   HeatPumpPort,        TXTRecord(),         [ EnergyManagementRole.CEM, EnergyManagementRole.RM ], [ Address4, Address6 ]);
            var differentPort       = Advertisement([ EnergyManagementRole.CEM, EnergyManagementRole.RM ], [ Address4, Address6 ], IPPort.Parse(8444));
            var differentRoles      = Advertisement([ EnergyManagementRole.CEM ],                          [ Address4, Address6 ]);
            var differentAddresses  = Advertisement([ EnergyManagementRole.CEM, EnergyManagementRole.RM ], [ Address4 ]);
            var noAddresses         = Advertisement([ EnergyManagementRole.CEM, EnergyManagementRole.RM ]);
            var differentTXT        = new S2ServiceAdvertisement(HeatPumpHost,                    HeatPumpPort,        TXTRecord("Boiler"), [ EnergyManagementRole.CEM, EnergyManagementRole.RM ], [ Address4, Address6 ]);
            var differentHost       = new S2ServiceAdvertisement(DomainName.Parse("pump.local."), HeatPumpPort,        TXTRecord(),         [ EnergyManagementRole.CEM, EnergyManagementRole.RM ], [ Address4, Address6 ]);

            Assert.Multiple(() => {

                Assert.That(a,                     Is.EqualTo(b), "the order of roles and addresses does not matter");
                Assert.That(a.GetHashCode(),       Is.EqualTo(b.GetHashCode()));
                Assert.That(a.Equals(b),           Is.True);
                Assert.That(a.Equals((Object) b),  Is.True);
                Assert.That(a == b,                Is.True);
                Assert.That(a != b,                Is.False);

                Assert.That(a,                     Is.EqualTo(upperCaseHost), "host names are case-insensitive");
                Assert.That(a.GetHashCode(),       Is.EqualTo(upperCaseHost.GetHashCode()));

                Assert.That(a,                     Is.Not.EqualTo(differentPort));
                Assert.That(a,                     Is.Not.EqualTo(differentRoles));
                Assert.That(a,                     Is.Not.EqualTo(differentAddresses));
                Assert.That(a,                     Is.Not.EqualTo(noAddresses));
                Assert.That(a,                     Is.Not.EqualTo(differentTXT));
                Assert.That(a,                     Is.Not.EqualTo(differentHost));
                Assert.That(a != differentPort,    Is.True);
                Assert.That(a == differentPort,    Is.False);
                Assert.That(a.Equals("hp.local."), Is.False);

            });

            S2ServiceAdvertisement? none = null;

            Assert.Multiple(() => {
                Assert.That(a == none,       Is.False);
                Assert.That(none == a,       Is.False);
                Assert.That(a != none,       Is.True);
                Assert.That(none == null,    Is.True);
                Assert.That(a.Equals(none),  Is.False);
            });

        }

        [Test]
        public void ToString_ContainsTheInstanceName()
        {

            var text = Advertisement().ToString();

            Assert.Multiple(() => {
                Assert.That(text, Does.Contain("hp._s2connect._tcp.local."));
                Assert.That(text, Does.Contain("hp.local.:8443"));
                Assert.That(text, Does.Contain("CEM"));
                Assert.That(text, Does.Contain("RM"));
                Assert.That(text, Does.Contain("pairingUrl=https://hp.local:8443/pairing/"));
            });

        }

        #endregion

    }

}

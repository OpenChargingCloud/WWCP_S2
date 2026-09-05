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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2;
using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Node;

#endregion

namespace cloud.charging.open.protocols.S2.Samples
{

    /// <summary>
    /// A photovoltaic inverter Resource Manager: an RM that provides power measurements and
    /// forecasts and is controlled through Power Envelope Based Control - a 4 kWp installation on
    /// phase L1 whose feed-in (negative by the S2 sign convention) the CEM may curtail down to
    /// zero. The node composition, pairing and session handling are identical to the EV charger.
    /// </summary>
    public static class PVRM
    {

        #region Details

        /// <summary>
        /// The ResourceManagerDetails of a PV inverter (an energy producer that provides forecasts).
        /// </summary>
        public static ResourceManagerDetails Details(Resource_Id? ResourceId = null)

            => new (ResourceId ?? Resource_Id.Parse("acme_pv_000001"),
                    [ new Role(RoleType.EnergyProducer, Commodity.Electricity) ],
                    Duration.FromMilliseconds(2000),
                    [ ControlType.PowerEnvelopeBasedControl ],
                    ProvidesForecast:                true,
                    ProvidesPowerMeasurementTypes:   [ CommodityQuantity.ElectricPower3PhaseSymmetric ],
                    Name:                            "My PV RM",
                    Manufacturer:                    "ACME",
                    Model:                           "SolarMax-5000");

        #endregion

        #region PowerConstraints

        /// <summary>
        /// What the inverter accepts a CEM to ask of it: on phase L1 the upper limit stays at zero
        /// (it never consumes), and the lower limit - the feed-in - may be curtailed anywhere
        /// between the full 4 kW and nothing. A vanished envelope means the inverter returns to
        /// producing whatever the sun gives.
        /// </summary>
        public static PEBC_PowerConstraints PowerConstraints(DateTimeOffset? ValidFrom = null)

            => new (PowerConstraints_Id.Parse("pvPowerConstraints1"),
                    ValidFrom ?? DateTimeOffset.UtcNow,
                    PEBC_PowerEnvelopeConsequenceType.Vanish,
                    [
                        new PEBC_AllowedLimitRange(CommodityQuantity.ElectricPowerL1,
                                                   PEBC_PowerEnvelopeLimitType.LowerLimit,
                                                   new NumberRange(-4000, 0),
                                                   false),
                        new PEBC_AllowedLimitRange(CommodityQuantity.ElectricPowerL1,
                                                   PEBC_PowerEnvelopeLimitType.UpperLimit,
                                                   new NumberRange(0, 0),
                                                   false)
                    ]);

        #endregion

        #region BuildNode(PairingUrl, HTTPPort, ServiceDiscovery = null, ...)

        /// <summary>
        /// Build a PV RM node (a LAN communication client) with a PEBC resource manager: it publishes
        /// the power constraints above when the CEM selects PEBC and reports the envelopes it is
        /// asked to follow.
        /// </summary>
        public static RMNode BuildNode(S2BaseURL                PairingUrl,
                                       IPPort                   HTTPPort,
                                       IServiceDiscovery?       ServiceDiscovery               = null,
                                       CertificateFingerprint?  ServerCertificateFingerprint   = null,
                                       CertificateFingerprint?  AssumedServerFingerprint       = null)

        {

            var node = new RMNode(
                   new HostedNode(new NodeDescription(Node_Id.NewRandom, "ACME", "PV inverter", "SolarMax-5000", EnergyManagementRole.RM)),
                   new S2NodeOptions {
                       Description                                = new EndpointDescription("PV inverter"),
                       Deployment                                 = Deployment.LAN,
                       PairingUrl                                 = PairingUrl,
                       HTTPPort                                   = HTTPPort,
                       BindAddress                                = IPv4Address.Localhost,
                       ServerCertificateFingerprint               = ServerCertificateFingerprint is not null ? () => ServerCertificateFingerprint.Value : null,
                       AssumedRemoteServerCertificateFingerprint  = AssumedServerFingerprint,
                       ParserOptions                              = new S2ParserOptions { AllowInsecureURLs = true },
                       PairingServer                              = new PairingServerOptions  { ParserOptions = new S2ParserOptions { AllowInsecureURLs = true } },
                       PairingClient                              = new PairingClientOptions  { ParserOptions = new S2ParserOptions { AllowInsecureURLs = true } },
                       SessionInitiationClient                    = new SessionInitiationClientOptions { ParserOptions = new S2ParserOptions { AllowInsecureURLs = true } },
                       Advertiser                                 = new EndpointAdvertiserOptions { Addresses = [ IPv4Address.Localhost ] }
                   },
                   Details(),
                   ServiceDiscovery:  ServiceDiscovery
               );

            var pebc = new PEBCResourceManager(() => PowerConstraints());

            pebc.OnInstruction += (session, instruction, ct) => {
                Console.WriteLine($"  [PV]  power envelope {instruction.Id}: {instruction.PowerEnvelopes.Count} envelope(s) for constraints {instruction.PowerConstraintsId}");
                return Task.FromResult<ReceptionStatusValue?>(null);
            };

            node.RegisterControlType(pebc);

            return node;

        }

        #endregion

    }

}

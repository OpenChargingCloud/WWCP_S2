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
    /// A photovoltaic inverter Resource Manager. It advertises itself as an RM that provides power
    /// measurements and forecasts; the control-type behaviour (Power Envelope Based Control) is
    /// added by registering a PEBC control-type handler, which is not part of this Phase 10 sample
    /// yet. The node composition, pairing and session handling are identical to the EV charger.
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

        #region BuildNode(PairingUrl, HTTPPort, ServiceDiscovery = null, ...)

        /// <summary>
        /// Build a PV RM node (a LAN communication client). Register a PEBC control-type handler on
        /// the returned node to add the control behaviour.
        /// </summary>
        public static RMNode BuildNode(S2BaseURL                PairingUrl,
                                       IPPort                   HTTPPort,
                                       IServiceDiscovery?       ServiceDiscovery               = null,
                                       CertificateFingerprint?  ServerCertificateFingerprint   = null,
                                       CertificateFingerprint?  AssumedServerFingerprint       = null)

            => new (
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

        #endregion

    }

}

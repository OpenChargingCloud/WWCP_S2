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
    /// An EV charger Resource Manager modelled with Fill Rate Based Control, following the S2
    /// worked example (docs.s2standard.org): two operation modes (off, charging 1.4–11 kW),
    /// a battery state of charge from 0 to 100 and a fill level target profile.
    /// </summary>
    public static class EVChargerRM
    {

        #region ResourceManagerDetails

        /// <summary>
        /// The ResourceManagerDetails of the EV charger (S2 worked example).
        /// </summary>
        public static ResourceManagerDetails Details(Resource_Id? ResourceId = null)

            => new (ResourceId ?? Resource_Id.Parse("acme_ev_000001"),
                    [ new Role(RoleType.EnergyConsumer, Commodity.Electricity) ],
                    Duration.FromMilliseconds(3000),
                    [ ControlType.FillRateBasedControl ],
                    ProvidesForecast:                false,
                    ProvidesPowerMeasurementTypes:   [ CommodityQuantity.ElectricPower3PhaseSymmetric ],
                    Name:                            "My Electric Vehicle RM",
                    Manufacturer:                    "ACME",
                    Model:                           "WallBox-b100",
                    SerialNumber:                    "123",
                    FirmwareVersion:                 "v1.0");

        #endregion

        #region SystemDescription

        /// <summary>
        /// The FRBC system description of the EV charger (S2 worked example).
        /// </summary>
        public static FRBC_SystemDescription SystemDescription(DateTimeOffset? ValidFrom = null)
        {

            var off       = new FRBC_OperationMode(
                                OperationMode_Id.Parse("om1"),
                                [ new FRBC_OperationModeElement(
                                      new NumberRange(0, 100),
                                      new NumberRange(0, 0),
                                      [ new PowerRange(0, 0, CommodityQuantity.ElectricPower3PhaseSymmetric) ]
                                  ) ],
                                AbnormalConditionOnly: false,
                                DiagnosticLabel:       "off");

            var charging  = new FRBC_OperationMode(
                                OperationMode_Id.Parse("om2"),
                                [ new FRBC_OperationModeElement(
                                      new NumberRange(0, 100),
                                      new NumberRange(0.00065, 0.0051),
                                      [ new PowerRange(1400, 11000, CommodityQuantity.ElectricPower3PhaseSymmetric) ]
                                  ) ],
                                AbnormalConditionOnly: false,
                                DiagnosticLabel:       "charging");

            var actuator  = new FRBC_ActuatorDescription(
                                Actuator_Id.Parse("actuator1"),
                                [ Commodity.Electricity ],
                                [ off, charging ],
                                [ new Transition(Transition_Id.Parse("t1"), OperationMode_Id.Parse("om1"), OperationMode_Id.Parse("om2"), [], [], false, TransitionDuration: Duration.FromMilliseconds(3000)),
                                  new Transition(Transition_Id.Parse("t2"), OperationMode_Id.Parse("om2"), OperationMode_Id.Parse("om1"), [], [], false, TransitionDuration: Duration.FromMilliseconds(3000)) ],
                                [],
                                DiagnosticLabel: "EV charger actuator");

            var storage   = new FRBC_StorageDescription(
                                ProvidesLeakageBehaviour:        false,
                                ProvidesFillLevelTargetProfile:  true,
                                ProvidesUsageForecast:           false,
                                FillLevelRange:                  new NumberRange(0, 100),
                                DiagnosticLabel:                 "Battery SoC",
                                FillLevelLabel:                  "EV Battery SoC");

            return new FRBC_SystemDescription(
                       ValidFrom ?? DateTimeOffset.UtcNow,
                       [ actuator ],
                       storage
                   );

        }

        #endregion

        #region BuildNode(PairingUrl, HTTPPort, ServiceDiscovery = null, ...)

        /// <summary>
        /// Build an EV charger RM node: a LAN communication client with an FRBC resource manager
        /// that answers instructions and reports its charging state.
        /// </summary>
        /// <param name="PairingUrl">The public pairing URL (an mDNS ".local" host for a LAN endpoint).</param>
        /// <param name="HTTPPort">The local HTTP port.</param>
        /// <param name="ServiceDiscovery">An optional service discovery for advertising and resolving.</param>
        /// <param name="ServerCertificateFingerprint">The fingerprint of the TLS server certificate (the LAN pairing formula's F).</param>
        /// <param name="AssumedServerFingerprint">The assumed fingerprint of remote pairing servers reached over plain HTTP (samples/tests).</param>
        public static RMNode BuildNode(S2BaseURL                PairingUrl,
                                       IPPort                   HTTPPort,
                                       IServiceDiscovery?       ServiceDiscovery               = null,
                                       CertificateFingerprint?  ServerCertificateFingerprint   = null,
                                       CertificateFingerprint?  AssumedServerFingerprint       = null)
        {

            var node = new RMNode(
                           new HostedNode(new NodeDescription(Node_Id.NewRandom, "ACME", "EV charger", "WallBox-b100", EnergyManagementRole.RM)),
                           new S2NodeOptions {
                               Description                                = new EndpointDescription("EV charger"),
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

            var frbc = new FRBCResourceManager(SystemDescription());

            frbc.OnInstruction += (session, instruction, ct) => {
                Console.WriteLine($"  [RM]  charging instruction: actuator {instruction.ActuatorId}, mode {instruction.OperationMode} @ factor {instruction.OperationModeFactor}");
                return Task.FromResult<ReceptionStatusValue?>(null);
            };

            node.RegisterControlType(frbc);

            return node;

        }

        #endregion

    }

}

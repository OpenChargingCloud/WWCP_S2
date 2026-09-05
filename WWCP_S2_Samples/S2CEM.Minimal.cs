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
    /// A minimal Customer Energy Manager: a LAN communication server that pairs with resource
    /// managers, selects Fill Rate Based Control when an RM offers it and logs the RM's system
    /// description, storage status and instruction acknowledgements.
    /// </summary>
    public static class MinimalCEM
    {

        #region BuildNode(PairingUrl, SessionInitiationUrl, WebSocketUrl, HTTPPort, WebSocketPort, ...)

        /// <summary>
        /// Build a minimal CEM node with an FRBC energy manager.
        /// </summary>
        public static CEMNode BuildNode(S2BaseURL                PairingUrl,
                                        S2BaseURL                SessionInitiationUrl,
                                        URL                      WebSocketUrl,
                                        IPPort                   HTTPPort,
                                        IPPort                   WebSocketPort,
                                        IServiceDiscovery?       ServiceDiscovery               = null,
                                        CertificateFingerprint?  ServerCertificateFingerprint   = null,
                                        CertificateFingerprint?  CACertificateFingerprint       = null)
        {

            var node = new CEMNode(
                           new HostedNode(new NodeDescription(Node_Id.NewRandom, "GraphDefined", "EMS", "Home Energy Manager", EnergyManagementRole.CEM)),
                           new S2NodeOptions {
                               Description                    = new EndpointDescription("Home CEM"),
                               Deployment                     = Deployment.LAN,
                               PairingUrl                     = PairingUrl,
                               SessionInitiationUrl           = SessionInitiationUrl,
                               WebSocketUrl                   = WebSocketUrl,
                               HTTPPort                       = HTTPPort,
                               WebSocketPort                  = WebSocketPort,
                               BindAddress                    = IPv4Address.Localhost,
                               ServerCertificateFingerprint   = ServerCertificateFingerprint is not null ? () => ServerCertificateFingerprint.Value : null,
                               CACertificateFingerprint       = CACertificateFingerprint     is not null ? () => CACertificateFingerprint.Value     : null,
                               ParserOptions                  = new S2ParserOptions { AllowInsecureURLs = true },
                               PairingServer                  = new PairingServerOptions           { ParserOptions = new S2ParserOptions { AllowInsecureURLs = true } },
                               SessionInitiationServer        = new SessionInitiationServerOptions { ParserOptions = new S2ParserOptions { AllowInsecureURLs = true } },
                               Advertiser                     = new EndpointAdvertiserOptions       { Addresses = [ IPv4Address.Localhost ] }
                           },
                           ServiceDiscovery:  ServiceDiscovery
                       );

            var frbc = new FRBCEnergyManager();

            frbc.OnSystemDescription += (session, description, ct) => {
                Console.WriteLine($"  [CEM] system description: {description.Actuators.Count} actuator(s), storage '{description.Storage.FillLevelLabel}'");
                return Task.CompletedTask;
            };

            frbc.OnInstructionStatusUpdate += (session, update, ct) => {
                Console.WriteLine($"  [CEM] instruction {update.InstructionId}: {update.StatusType}");
                return Task.CompletedTask;
            };

            node.RegisterControlType(frbc);

            node.OnSessionStarted += (timestamp, sender, session) => {
                Console.WriteLine($"  [CEM] session with {session.RemoteNodeId} started");
                return Task.CompletedTask;
            };

            return node;

        }

        #endregion

    }

}

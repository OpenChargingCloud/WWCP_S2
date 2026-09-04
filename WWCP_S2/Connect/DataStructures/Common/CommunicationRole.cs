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

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The role of a node in the S2 connection that follows a pairing (S2 Connect 1.0.0,
    /// "Mapping the CEM and RM to communication server or client"): the communication server
    /// hosts the session initiation API and the WebSocket server, the communication client
    /// initiates sessions and connects to that WebSocket server. The role is independent of
    /// the HTTPS roles during pairing.
    /// </summary>
    public enum CommunicationRole
    {

        /// <summary>
        /// The node hosts the session initiation API and the WebSocket server.
        /// </summary>
        CommunicationServer,

        /// <summary>
        /// The node initiates sessions and connects to the WebSocket server of its peer.
        /// </summary>
        CommunicationClient

    }


    /// <summary>
    /// Extension methods for communication roles.
    /// </summary>
    public static class CommunicationRoleExtensions
    {

        #region Documentation

        // S2 Connect 1.0.0, "Mapping the CEM and RM to communication server or client":
        //
        //   * If a connection is set up between a WAN node and a LAN node, the WAN node must act as a
        //     communication server, and the local node must act as a communication client.
        //   * If a connection is set up between two nodes that are similarly deployed (i.e. both in WAN,
        //     or both in LAN), the CEM must act as a communication server, and the RM must act as a
        //     communication client.
        //
        //   | CEM deployment | RM deployment | CEM acts as          | RM acts as           |
        //   | WAN            | WAN           | Communication server | Communication client |
        //   | WAN            | LAN           | Communication server | Communication client |
        //   | LAN            | WAN           | Communication client | Communication server |
        //   | LAN            | LAN           | Communication server | Communication client |

        #endregion

        #region (static) Determine(LocalRole, LocalDeployment, RemoteDeployment)

        /// <summary>
        /// Determine the communication role of the local node from the mapping table of the
        /// specification: between differently deployed nodes the WAN node is the communication
        /// server, between similarly deployed nodes the CEM is the communication server.
        /// </summary>
        /// <param name="LocalRole">The energy management role of the local node (CEM or RM).</param>
        /// <param name="LocalDeployment">The deployment of the local node (WAN or LAN).</param>
        /// <param name="RemoteDeployment">The deployment of the remote node (WAN or LAN).</param>
        public static CommunicationRole Determine(EnergyManagementRole  LocalRole,
                                                  Deployment            LocalDeployment,
                                                  Deployment            RemoteDeployment)
        {

            if (LocalRole != EnergyManagementRole.CEM && LocalRole != EnergyManagementRole.RM)
                throw new ArgumentException($"The local role must be CEM or RM, not '{LocalRole}'!", nameof(LocalRole));

            if (LocalDeployment != Deployment.WAN && LocalDeployment != Deployment.LAN)
                throw new ArgumentException($"The local deployment must be WAN or LAN, not '{LocalDeployment}'!", nameof(LocalDeployment));

            if (RemoteDeployment != Deployment.WAN && RemoteDeployment != Deployment.LAN)
                throw new ArgumentException($"The remote deployment must be WAN or LAN, not '{RemoteDeployment}'!", nameof(RemoteDeployment));

            if (LocalDeployment != RemoteDeployment)
                return LocalDeployment == Deployment.WAN
                           ? CommunicationRole.CommunicationServer
                           : CommunicationRole.CommunicationClient;

            return LocalRole == EnergyManagementRole.CEM
                       ? CommunicationRole.CommunicationServer
                       : CommunicationRole.CommunicationClient;

        }

        #endregion

        #region Opposite(this CommunicationRole)

        /// <summary>
        /// The communication role of the peer.
        /// </summary>
        /// <param name="CommunicationRole">A communication role.</param>
        public static CommunicationRole Opposite(this CommunicationRole CommunicationRole)

            => CommunicationRole == CommunicationRole.CommunicationServer
                   ? CommunicationRole.CommunicationClient
                   : CommunicationRole.CommunicationServer;

        #endregion

        #region AsText(this CommunicationRole)

        /// <summary>
        /// The text representation used when a communication role is persisted.
        /// </summary>
        /// <param name="CommunicationRole">A communication role.</param>
        public static String AsText(this CommunicationRole CommunicationRole)

            => CommunicationRole switch {
                   CommunicationRole.CommunicationServer  => "CommunicationServer",
                   CommunicationRole.CommunicationClient  => "CommunicationClient",
                   _                                      => throw new ArgumentOutOfRangeException(nameof(CommunicationRole), CommunicationRole, "Unknown communication role!")
               };

        #endregion

        #region (static) TryParse(Text, out CommunicationRole)

        /// <summary>
        /// Try to parse the given text representation of a communication role.
        /// </summary>
        /// <param name="Text">A text representation of a communication role.</param>
        /// <param name="CommunicationRole">The parsed communication role.</param>
        public static Boolean TryParse(String?                Text,
                                       out CommunicationRole  CommunicationRole)
        {

            switch (Text)
            {

                case "CommunicationServer":
                    CommunicationRole = CommunicationRole.CommunicationServer;
                    return true;

                case "CommunicationClient":
                    CommunicationRole = CommunicationRole.CommunicationClient;
                    return true;

                default:
                    CommunicationRole = default;
                    return false;

            }

        }

        #endregion

    }

}

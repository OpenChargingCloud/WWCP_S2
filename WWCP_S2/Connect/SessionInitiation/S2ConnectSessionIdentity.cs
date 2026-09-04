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
    /// The identity a communication server attaches to an issued communication token: the
    /// pairing the session belongs to and the communication protocol and S2 message version
    /// negotiated during session initiation. The WebSocket server receives it when the token
    /// is redeemed and configures the session accordingly.
    /// </summary>
    /// <param name="Pairing">The pairing the session belongs to (local node = communication server).</param>
    /// <param name="CommunicationProtocol">The communication protocol selected in step 3.</param>
    /// <param name="S2MessageVersion">The S2 message version selected in step 3.</param>
    public sealed record S2ConnectSessionIdentity(Pairing                Pairing,
                                                  CommunicationProtocol  CommunicationProtocol,
                                                  String                 S2MessageVersion)
    {

        /// <summary>
        /// The identification of the local node (the communication server).
        /// </summary>
        public Node_Id  LocalNodeId
            => Pairing.LocalNodeId;

        /// <summary>
        /// The identification of the remote node (the communication client).
        /// </summary>
        public Node_Id  RemoteNodeId
            => Pairing.RemoteNodeId;

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"session of {RemoteNodeId} at {LocalNodeId} ({CommunicationProtocol}, {S2MessageVersion})";

    }

}

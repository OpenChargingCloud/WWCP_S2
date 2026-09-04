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
    /// An access token that was generated (communication server) or received (communication
    /// client) during session initiation but not yet confirmed as the active token of the
    /// pairing (S2 Connect 1.0.0, "2. Generate new pending accessToken", "4. Store pending
    /// accessToken"). The server also remembers the communication protocol and S2 message
    /// version it selected in step 3, so that step 7 can answer with the matching details.
    /// </summary>
    /// <param name="LocalNodeId">The identification of the local node.</param>
    /// <param name="RemoteNodeId">The identification of the remote node.</param>
    /// <param name="Token">The pending access token.</param>
    /// <param name="CreatedAt">When the token was generated or received.</param>
    /// <param name="SelectedCommunicationProtocol">The communication protocol selected for the session (server side).</param>
    /// <param name="SelectedS2MessageVersion">The S2 message version selected for the session (server side).</param>
    public sealed record PendingAccessToken(Node_Id                 LocalNodeId,
                                            Node_Id                 RemoteNodeId,
                                            AccessToken             Token,
                                            DateTimeOffset          CreatedAt,
                                            CommunicationProtocol?  SelectedCommunicationProtocol   = null,
                                            String?                 SelectedS2MessageVersion        = null)
    {

        /// <summary>
        /// Whether the token is older than the given lifetime at the given time.
        /// </summary>
        /// <param name="Now">The current time.</param>
        /// <param name="Lifetime">The maximal age of a pending token.</param>
        public Boolean HasExpired(DateTimeOffset  Now,
                                  TimeSpan        Lifetime)
            => Now - CreatedAt > Lifetime;

        /// <summary>
        /// Return a text representation of this object (without the token).
        /// </summary>
        public override String ToString()
            => $"pending access token of {LocalNodeId} <-> {RemoteNodeId}, created {CreatedAt:O}";

    }

}

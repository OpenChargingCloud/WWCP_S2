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

using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.WebSockets
{

    /// <summary>
    /// The result of validating a communication token.
    /// </summary>
    /// <param name="IsAccepted">Whether the token was accepted.</param>
    /// <param name="Identity">The opaque identity of the client (e.g. its node id) when accepted.</param>
    public readonly record struct CommunicationTokenValidation(Boolean  IsAccepted,
                                                               Object?  Identity   = null)
    {

        /// <summary>
        /// A rejected token.
        /// </summary>
        public static CommunicationTokenValidation Rejected
            => new (false);

        /// <summary>
        /// An accepted token with the given identity.
        /// </summary>
        /// <param name="Identity">The opaque identity of the client.</param>
        public static CommunicationTokenValidation Accepted(Object? Identity = null)
            => new (true, Identity);

    }


    /// <summary>
    /// A delegate validating the bearer token of a WebSocket upgrade request.
    /// </summary>
    /// <param name="Token">The bearer token.</param>
    /// <param name="Request">The HTTP upgrade request.</param>
    /// <param name="CancellationToken">A token to cancel the validation.</param>
    public delegate Task<CommunicationTokenValidation>  ValidateCommunicationTokenDelegate(String             Token,
                                                                                           HTTPRequest        Request,
                                                                                           CancellationToken  CancellationToken);


    /// <summary>
    /// A delegate creating the session options for an accepted connection.
    /// </summary>
    /// <param name="Connection">The accepted WebSocket connection.</param>
    /// <param name="Identity">The identity returned by the token validation.</param>
    public delegate S2SessionOptions  S2SessionOptionsFactory(WebSocketServerConnection  Connection,
                                                              Object?                    Identity);


    /// <summary>
    /// A delegate called when an S2 session was started on an accepted WebSocket connection.
    /// </summary>
    /// <param name="Timestamp">The timestamp.</param>
    /// <param name="Server">The S2 WebSocket server.</param>
    /// <param name="Session">The started session.</param>
    /// <param name="Identity">The identity returned by the token validation.</param>
    public delegate Task  OnS2SessionStartedDelegate(DateTimeOffset     Timestamp,
                                                     S2WebSocketServer  Server,
                                                     S2Session          Session,
                                                     Object?            Identity);


    /// <summary>
    /// A delegate called when an S2 session on an accepted WebSocket connection ended.
    /// </summary>
    /// <param name="Timestamp">The timestamp.</param>
    /// <param name="Server">The S2 WebSocket server.</param>
    /// <param name="Session">The ended session.</param>
    /// <param name="Reason">Why the session ended.</param>
    public delegate Task  OnS2SessionEndedDelegate  (DateTimeOffset     Timestamp,
                                                     S2WebSocketServer  Server,
                                                     S2Session          Session,
                                                     S2CloseReason      Reason);

}

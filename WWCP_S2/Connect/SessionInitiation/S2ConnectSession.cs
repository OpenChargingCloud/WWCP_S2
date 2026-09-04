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

using cloud.charging.open.protocols.S2.Session;
using cloud.charging.open.protocols.S2.WebSockets;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// An S2 session opened by a communication client through session initiation: the
    /// WebSocket client, the running <see cref="S2Session"/> and the result of the initiation.
    /// Disposing it closes the session and the WebSocket.
    /// </summary>
    public sealed class S2ConnectSession : IAsyncDisposable
    {

        #region Properties

        /// <summary>
        /// The WebSocket client carrying the session.
        /// </summary>
        public S2WebSocketClient              WebSocketClient    { get; }

        /// <summary>
        /// The S2 session.
        /// </summary>
        public S2Session                      Session            { get; }

        /// <summary>
        /// The result of the session initiation that opened the session.
        /// </summary>
        public SessionInitiationClientResult  Initiation         { get; }

        /// <summary>
        /// The pairing the session belongs to.
        /// </summary>
        public Pairing                        Pairing
            => Initiation.Pairing!;

        #endregion

        #region Constructor(s)

        internal S2ConnectSession(S2WebSocketClient              WebSocketClient,
                                  S2Session                      Session,
                                  SessionInitiationClientResult  Initiation)
        {
            this.WebSocketClient  = WebSocketClient;
            this.Session          = Session;
            this.Initiation       = Initiation;
        }

        #endregion


        #region CloseAsync(Reason = null, CancellationToken = default)

        /// <summary>
        /// Close the session and the WebSocket.
        /// </summary>
        /// <param name="Reason">An optional reason.</param>
        /// <param name="CancellationToken">A token to cancel the closing.</param>
        public async Task CloseAsync(String?            Reason              = null,
                                     CancellationToken  CancellationToken   = default)
        {

            try
            {
                await WebSocketClient.CloseSessionAsync(Reason, CancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The peer may already be gone.
            }

        }

        #endregion

        #region DisposeAsync()

        /// <summary>
        /// Close the session and release the WebSocket client.
        /// </summary>
        public async ValueTask DisposeAsync()
        {

            await CloseAsync("disposed").ConfigureAwait(false);

            try
            {
                await WebSocketClient.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Best effort.
            }

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"S2 Connect session {Session.Id} of {Pairing.LocalNodeId} with {Pairing.RemoteNodeId} ({Session.State})";

        #endregion

    }

}

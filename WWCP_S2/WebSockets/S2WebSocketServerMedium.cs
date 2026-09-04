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

using Microsoft.Extensions.Logging;

using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.WebSockets
{

    /// <summary>
    /// An S2 medium on top of one accepted connection of a Hermod WebSocket server.
    /// The <see cref="S2WebSocketServer"/> feeds received text messages and the close
    /// notification into it.
    /// </summary>
    public sealed class S2WebSocketServerMedium : AS2WebSocketMedium
    {

        #region Properties

        /// <summary>
        /// The WebSocket server.
        /// </summary>
        public AWebSocketServer           Server        { get; }

        /// <summary>
        /// The accepted WebSocket connection.
        /// </summary>
        public WebSocketServerConnection  Connection    { get; }

        /// <summary>
        /// Whether the connection is open.
        /// </summary>
        public override Boolean           IsConnected
            => !IsClosed && !Connection.IsClosed;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new server-side WebSocket medium.
        /// </summary>
        /// <param name="Server">The WebSocket server.</param>
        /// <param name="Connection">The accepted WebSocket connection.</param>
        /// <param name="Logger">An optional logger.</param>
        public S2WebSocketServerMedium(AWebSocketServer           Server,
                                       WebSocketServerConnection  Connection,
                                       ILogger?                   Logger   = null)

            : base($"WebSocket server connection from {Connection.RemoteSocket}", Logger)

        {
            this.Server      = Server;
            this.Connection  = Connection;
        }

        #endregion


        #region (internal) OnTextMessageReceivedAsync(Text, CancellationToken)

        internal Task OnTextMessageReceivedAsync(String             Text,
                                                 CancellationToken  CancellationToken)

            => DeliverAsync(Text, CancellationToken);

        #endregion

        #region (internal) OnConnectionClosedAsync(Reason)

        internal Task OnConnectionClosedAsync(S2CloseReason Reason)
            => MarkClosedAsync(Reason);

        #endregion


        #region SendAsync (Text, CancellationToken = default)

        /// <summary>
        /// Send the given text message to the client.
        /// </summary>
        /// <param name="Text">A text message.</param>
        /// <param name="CancellationToken">A token to cancel the send operation.</param>
        public override async ValueTask<S2SendResult> SendAsync(String             Text,
                                                                CancellationToken  CancellationToken   = default)
        {

            if (!IsConnected)
                return new S2SendResult(S2SendStatus.ConnectionClosed, "The WebSocket connection is closed.");

            try
            {
                return Map(await Server.SendTextMessage(Connection, Text, null, CancellationToken).ConfigureAwait(false));
            }
            catch (Exception e)
            {
                return new S2SendResult(S2SendStatus.Error, e.Message);
            }

        }

        #endregion

        #region CloseAsync(Reason, CancellationToken = default)

        /// <summary>
        /// Close the WebSocket connection with a normal closure.
        /// </summary>
        /// <param name="Reason">Why the medium is closed.</param>
        /// <param name="CancellationToken">A token to cancel the close operation.</param>
        public override async Task CloseAsync(S2CloseReason      Reason,
                                              CancellationToken  CancellationToken   = default)
        {

            if (!Connection.IsClosed)
            {

                try
                {
                    await Connection.Close(WebSocketFrame.ClosingStatusCode.NormalClosure,
                                           Reason.Description,
                                           CancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger?.LogDebug(e, "Closing the WebSocket connection {Connection} failed.", Description);
                }

            }

            await MarkClosedAsync(Reason).ConfigureAwait(false);

        }

        #endregion

    }

}

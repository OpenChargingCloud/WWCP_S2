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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.WebSockets
{

    /// <summary>
    /// An S2 medium on top of a connected Hermod WebSocket client. Received text messages and
    /// close frames (including Hermod's synthetic 1006 close on a lost TCP connection) are
    /// forwarded from the client's events.
    /// </summary>
    public sealed class S2WebSocketClientMedium : AS2WebSocketMedium
    {

        #region Properties

        /// <summary>
        /// The WebSocket client.
        /// </summary>
        public WebSocketClient            Client        { get; }

        /// <summary>
        /// The established WebSocket connection.
        /// </summary>
        public WebSocketClientConnection  Connection    { get; }

        /// <summary>
        /// Whether the connection is open.
        /// </summary>
        public override Boolean           IsConnected
            => !IsClosed && !Connection.IsClosed;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new client-side WebSocket medium and subscribe to the client's events.
        /// </summary>
        /// <param name="Client">The connected WebSocket client.</param>
        /// <param name="Connection">The established WebSocket connection.</param>
        /// <param name="Logger">An optional logger.</param>
        public S2WebSocketClientMedium(WebSocketClient            Client,
                                       WebSocketClientConnection  Connection,
                                       ILogger?                   Logger   = null)

            : base($"WebSocket client connection to {Connection.RemoteSocket}", Logger)

        {

            this.Client      = Client;
            this.Connection  = Connection;

            Client.OnTextMessageReceived   += OnClientTextMessageReceived;
            Client.OnCloseMessageReceived  += OnClientCloseMessageReceived;

        }

        #endregion


        #region (private) Event forwarding

        private Task OnClientTextMessageReceived(DateTimeOffset             Timestamp,
                                                 WebSocketClient            Client,
                                                 WebSocketClientConnection  Connection,
                                                 WebSocketFrame             Frame,
                                                 EventTracking_Id           EventTrackingId,
                                                 String                     TextMessage,
                                                 CancellationToken          CancellationToken)

            => DeliverAsync(TextMessage, CancellationToken);


        private Task OnClientCloseMessageReceived(DateTimeOffset                    Timestamp,
                                                  IWebSocketClient                  Client,
                                                  WebSocketClientConnection         Connection,
                                                  WebSocketFrame                    Frame,
                                                  EventTracking_Id                  EventTrackingId,
                                                  WebSocketFrame.ClosingStatusCode  StatusCode,
                                                  String?                           Reason,
                                                  CancellationToken                 CancellationToken)

            => MarkClosedAsync(new S2CloseReason($"WebSocket closed by the peer ({StatusCode}): {Reason}", false));

        #endregion


        #region SendAsync (Text, CancellationToken = default)

        /// <summary>
        /// Send the given text message to the server.
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
                return Map(await Client.SendTextMessage(Text, null, CancellationToken).ConfigureAwait(false));
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

            Client.OnTextMessageReceived   -= OnClientTextMessageReceived;
            Client.OnCloseMessageReceived  -= OnClientCloseMessageReceived;

            if (!Connection.IsClosed)
            {

                try
                {
                    await Client.Close(WebSocketFrame.ClosingStatusCode.NormalClosure,
                                       Reason.Description,
                                       null,
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

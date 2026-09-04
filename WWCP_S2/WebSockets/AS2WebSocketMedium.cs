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

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.WebSockets
{

    /// <summary>
    /// The common part of the S2 media on top of a Hermod WebSocket connection: delivery of
    /// received text messages to the session, the closed notification, and the mapping of
    /// Hermod's send status. Hermod invokes the receive callbacks of one connection sequentially
    /// and in wire order, which the session relies on.
    /// </summary>
    public abstract class AS2WebSocketMedium : IS2Medium
    {

        #region Data

        /// <summary>
        /// The optional logger.
        /// </summary>
        protected readonly ILogger?  Logger;

        private            Int32     closed;

        #endregion

        #region Properties

        /// <summary>
        /// A human readable description of the medium.
        /// </summary>
        public String   Description    { get; }

        /// <summary>
        /// Whether the medium is connected.
        /// </summary>
        public abstract Boolean IsConnected { get; }

        /// <summary>
        /// Whether the closed notification was already raised.
        /// </summary>
        protected Boolean IsClosed
            => Volatile.Read(ref closed) != 0;

        #endregion

        #region Events

        /// <summary>
        /// Raised for every received text message, sequentially and in wire order.
        /// </summary>
        public event OnS2TextReceivedDelegate?   OnTextReceived;

        /// <summary>
        /// Raised once when the medium was closed, locally or by the peer.
        /// </summary>
        public event OnS2MediumClosedDelegate?   OnClosed;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new WebSocket medium.
        /// </summary>
        /// <param name="Description">A human readable description of the medium.</param>
        /// <param name="Logger">An optional logger.</param>
        protected AS2WebSocketMedium(String    Description,
                                     ILogger?  Logger)
        {
            this.Description  = Description;
            this.Logger       = Logger;
        }

        #endregion


        #region (protected) DeliverAsync(Text, CancellationToken)

        /// <summary>
        /// Deliver a received text message to the subscribers.
        /// </summary>
        /// <param name="Text">The received text.</param>
        /// <param name="CancellationToken">A token to cancel the processing.</param>
        protected Task DeliverAsync(String             Text,
                                    CancellationToken  CancellationToken)

            => OnTextReceived.InvokeAllAsync(handler => handler(this, Text, CancellationToken), Logger);

        #endregion

        #region (protected) MarkClosedAsync(Reason)

        /// <summary>
        /// Raise the closed notification exactly once.
        /// </summary>
        /// <param name="Reason">Why the medium was closed.</param>
        protected async Task MarkClosedAsync(S2CloseReason Reason)
        {

            if (Interlocked.Exchange(ref closed, 1) != 0)
                return;

            await OnClosed.InvokeAllAsync(handler => handler(this, Reason), Logger).ConfigureAwait(false);

        }

        #endregion

        #region (protected static) Map(SentStatus)

        /// <summary>
        /// Map Hermod's send status to an S2 send result.
        /// </summary>
        /// <param name="SentStatus">A Hermod send status.</param>
        protected static S2SendResult Map(SentStatus SentStatus)

            => SentStatus switch {
                   SentStatus.Success     => S2SendResult.Success,
                   SentStatus.Dropped     => new S2SendResult(S2SendStatus.Dropped,          "The message was dropped by the WebSocket backpressure limit."),
                   SentStatus.FatalError  => new S2SendResult(S2SendStatus.ConnectionClosed, "The WebSocket connection is closed."),
                   _                      => new S2SendResult(S2SendStatus.Error,            $"The WebSocket send failed: {SentStatus}.")
               };

        #endregion


        #region SendAsync (Text, CancellationToken = default)

        /// <summary>
        /// Send the given text message.
        /// </summary>
        /// <param name="Text">A text message.</param>
        /// <param name="CancellationToken">A token to cancel the send operation.</param>
        public abstract ValueTask<S2SendResult> SendAsync(String             Text,
                                                          CancellationToken  CancellationToken   = default);

        #endregion

        #region CloseAsync(Reason, CancellationToken = default)

        /// <summary>
        /// Close the medium.
        /// </summary>
        /// <param name="Reason">Why the medium is closed.</param>
        /// <param name="CancellationToken">A token to cancel the close operation.</param>
        public abstract Task CloseAsync(S2CloseReason      Reason,
                                        CancellationToken  CancellationToken   = default);

        #endregion

        #region DisposeAsync()

        /// <summary>
        /// Close and dispose the medium.
        /// </summary>
        public virtual async ValueTask DisposeAsync()
        {
            await CloseAsync(new S2CloseReason("disposed", true)).ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => Description;

        #endregion

    }

}

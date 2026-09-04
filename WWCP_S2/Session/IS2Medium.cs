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

namespace cloud.charging.open.protocols.S2.Session
{

    /// <summary>
    /// The result of sending a text message over an S2 medium.
    /// </summary>
    public enum S2SendStatus
    {

        /// <summary>
        /// The message was handed to the transport.
        /// </summary>
        Success,

        /// <summary>
        /// The message was dropped, e.g. because of backpressure.
        /// </summary>
        Dropped,

        /// <summary>
        /// The connection is closed.
        /// </summary>
        ConnectionClosed,

        /// <summary>
        /// The transport reported an error.
        /// </summary>
        Error

    }


    /// <summary>
    /// The result of sending a text message over an S2 medium.
    /// </summary>
    /// <param name="Status">The send status.</param>
    /// <param name="Description">An optional description of a failure.</param>
    public readonly record struct S2SendResult(S2SendStatus  Status,
                                               String?       Description   = null)
    {

        /// <summary>
        /// Whether the message was handed to the transport.
        /// </summary>
        public Boolean IsSuccess
            => Status == S2SendStatus.Success;

        /// <summary>
        /// A successful send result.
        /// </summary>
        public static S2SendResult Success
            => new (S2SendStatus.Success);

    }


    /// <summary>
    /// Why an S2 medium was closed.
    /// </summary>
    /// <param name="Description">A human readable description.</param>
    /// <param name="IsLocal">Whether the local side closed the connection.</param>
    /// <param name="Exception">The optional exception that caused the close.</param>
    public sealed record S2CloseReason(String      Description,
                                       Boolean     IsLocal,
                                       Exception?  Exception   = null);


    /// <summary>
    /// A delegate called for every text message received over an S2 medium.
    /// </summary>
    /// <param name="Medium">The medium.</param>
    /// <param name="Text">The received text.</param>
    /// <param name="CancellationToken">A token to cancel the processing.</param>
    public delegate Task OnS2TextReceivedDelegate(IS2Medium          Medium,
                                                  String             Text,
                                                  CancellationToken  CancellationToken);


    /// <summary>
    /// A delegate called once when an S2 medium was closed.
    /// </summary>
    /// <param name="Medium">The medium.</param>
    /// <param name="Reason">Why the medium was closed.</param>
    public delegate Task OnS2MediumClosedDelegate(IS2Medium      Medium,
                                                  S2CloseReason  Reason);


    /// <summary>
    /// A bidirectional, ordered, push-based text channel between two S2 nodes: a WebSocket
    /// connection in production, an in-memory pipe in tests (PLAN.md §3.1). The medium never
    /// interprets the text. Implementations invoke <see cref="OnTextReceived"/> sequentially
    /// and in wire order; the session guarantees that this callback never awaits application code.
    /// </summary>
    public interface IS2Medium : IAsyncDisposable
    {

        /// <summary>
        /// A human readable description of the medium, e.g. the remote endpoint.
        /// </summary>
        String   Description    { get; }

        /// <summary>
        /// Whether the medium is connected.
        /// </summary>
        Boolean  IsConnected    { get; }

        /// <summary>
        /// Raised for every received text message, sequentially and in wire order.
        /// </summary>
        event OnS2TextReceivedDelegate?   OnTextReceived;

        /// <summary>
        /// Raised once when the medium was closed, locally or by the peer.
        /// </summary>
        event OnS2MediumClosedDelegate?   OnClosed;

        /// <summary>
        /// Send the given text message.
        /// </summary>
        /// <param name="Text">A text message.</param>
        /// <param name="CancellationToken">A token to cancel the send operation.</param>
        ValueTask<S2SendResult>  SendAsync (String             Text,
                                            CancellationToken  CancellationToken   = default);

        /// <summary>
        /// Close the medium.
        /// </summary>
        /// <param name="Reason">Why the medium is closed.</param>
        /// <param name="CancellationToken">A token to cancel the close operation.</param>
        Task                     CloseAsync(S2CloseReason      Reason,
                                            CancellationToken  CancellationToken   = default);

    }

}

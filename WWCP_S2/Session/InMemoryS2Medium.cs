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

using System.Threading.Channels;

using Microsoft.Extensions.Logging;

using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.S2.Session
{

    /// <summary>
    /// An in-memory S2 medium: two connected ends that deliver text messages to each other
    /// in order through bounded channels. Used to run a CEM session against an RM session
    /// in-process (tests, simulators).
    /// </summary>
    public sealed class InMemoryS2Medium : IS2Medium
    {

        #region Data

        private readonly Channel<String>          inbound;
        private readonly CancellationTokenSource  cancellation   = new ();
        private readonly ILogger?                 logger;
        private          InMemoryS2Medium?        peer;
        private          Task?                    readerTask;
        private          Int32                    closed;

        #endregion

        #region Properties

        /// <summary>
        /// A human readable description of the medium.
        /// </summary>
        public String   Description    { get; }

        /// <summary>
        /// Whether the medium is connected.
        /// </summary>
        public Boolean  IsConnected
            => Volatile.Read(ref closed) == 0;

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

        private InMemoryS2Medium(String    Description,
                                 Int32     Capacity,
                                 ILogger?  Logger)
        {

            this.Description  = Description;
            this.logger       = Logger;
            this.inbound      = Channel.CreateBounded<String>(new BoundedChannelOptions(Capacity) {
                                    SingleReader  = true,
                                    SingleWriter  = false,
                                    FullMode      = BoundedChannelFullMode.Wait
                                });

        }

        #endregion


        #region (static) CreatePair(DescriptionA = null, DescriptionB = null, Capacity = 64, Logger = null)

        /// <summary>
        /// Create two connected in-memory media. Both start delivering messages immediately.
        /// </summary>
        /// <param name="DescriptionA">An optional description of the first end.</param>
        /// <param name="DescriptionB">An optional description of the second end.</param>
        /// <param name="Capacity">The number of messages buffered per direction before the sender waits.</param>
        /// <param name="Logger">An optional logger.</param>
        public static (InMemoryS2Medium A, InMemoryS2Medium B) CreatePair(String?   DescriptionA   = null,
                                                                          String?   DescriptionB   = null,
                                                                          Int32     Capacity       = 64,
                                                                          ILogger?  Logger         = null)
        {

            var a = new InMemoryS2Medium(DescriptionA ?? "in-memory A", Capacity, Logger);
            var b = new InMemoryS2Medium(DescriptionB ?? "in-memory B", Capacity, Logger);

            a.peer = b;
            b.peer = a;

            a.readerTask = Task.Run(a.ReadLoopAsync);
            b.readerTask = Task.Run(b.ReadLoopAsync);

            return (a, b);

        }

        #endregion


        #region SendAsync(Text, CancellationToken = default)

        /// <summary>
        /// Send the given text message to the peer.
        /// </summary>
        /// <param name="Text">A text message.</param>
        /// <param name="CancellationToken">A token to cancel the send operation.</param>
        public async ValueTask<S2SendResult> SendAsync(String             Text,
                                                       CancellationToken  CancellationToken   = default)
        {

            var target = peer;

            if (!IsConnected || target is null || !target.IsConnected)
                return new S2SendResult(S2SendStatus.ConnectionClosed, "The in-memory medium is closed.");

            try
            {
                await target.inbound.Writer.WriteAsync(Text, CancellationToken).ConfigureAwait(false);
                return S2SendResult.Success;
            }
            catch (ChannelClosedException)
            {
                return new S2SendResult(S2SendStatus.ConnectionClosed, "The peer's in-memory medium is closed.");
            }

        }

        #endregion

        #region CloseAsync(Reason, CancellationToken = default)

        /// <summary>
        /// Close both ends of the in-memory medium.
        /// </summary>
        /// <param name="Reason">Why the medium is closed.</param>
        /// <param name="CancellationToken">A token to cancel the close operation.</param>
        public async Task CloseAsync(S2CloseReason      Reason,
                                     CancellationToken  CancellationToken   = default)
        {

            await CloseLocallyAsync(Reason).ConfigureAwait(false);

            if (peer is not null)
                await peer.CloseLocallyAsync(new S2CloseReason(Reason.Description, false, Reason.Exception)).ConfigureAwait(false);

        }

        private async Task CloseLocallyAsync(S2CloseReason Reason)
        {

            if (Interlocked.Exchange(ref closed, 1) != 0)
                return;

            inbound.Writer.TryComplete();

            try
            {
                if (readerTask is not null)
                    await readerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            { }

            await cancellation.CancelAsync().ConfigureAwait(false);

            await OnClosed.InvokeAllAsync(handler => handler(this, Reason), logger).ConfigureAwait(false);

        }

        #endregion

        #region (private) ReadLoopAsync()

        private async Task ReadLoopAsync()
        {

            try
            {

                await foreach (var text in inbound.Reader.ReadAllAsync(cancellation.Token).ConfigureAwait(false))
                {
                    await OnTextReceived.InvokeAllAsync(handler => handler(this, text, cancellation.Token), logger).ConfigureAwait(false);
                }

            }
            catch (OperationCanceledException)
            { }

        }

        #endregion

        #region DisposeAsync()

        /// <summary>
        /// Close and dispose the medium.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await CloseAsync(new S2CloseReason("disposed", true)).ConfigureAwait(false);
            cancellation.Dispose();
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

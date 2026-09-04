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

using System.Collections.Concurrent;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The brute-force protection of requestPairing (S2 Connect 1.0.0, "2. Calculate
    /// clientHmacChallengeResponse": "For any given node at the server, pairing attempts must be
    /// handled sequentially, such that each second only one pairing attempt can be processed
    /// for a node. Pairing attempts targeting different nodes may be processed in parallel."):
    /// one lease per node at a time, a bounded queue of waiting requests per node and
    /// parallelism across nodes (PLAN.md D16).
    /// </summary>
    public sealed class PairingRateLimiter : IDisposable
    {

        #region Data

        private readonly ConcurrentDictionary<Node_Id, NodeQueue>  queues = new ();
        private          Boolean                                   disposed;

        internal sealed class NodeQueue
        {
            public SemaphoreSlim  Semaphore    { get; } = new (1, 1);
            public Int32          Requests;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The maximal number of requests waiting for the lease of a node (beyond the one
        /// holding it) before further requests are rejected.
        /// </summary>
        public Int32  MaxQueuedRequestsPerNode    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new pairing rate limiter.
        /// </summary>
        /// <param name="MaxQueuedRequestsPerNode">The maximal number of requests waiting per node (default: 4).</param>
        public PairingRateLimiter(Int32 MaxQueuedRequestsPerNode = S2ConnectDefaults.MaxQueuedPairingAttemptsPerNode)
        {

            ArgumentOutOfRangeException.ThrowIfNegative(MaxQueuedRequestsPerNode);

            this.MaxQueuedRequestsPerNode = MaxQueuedRequestsPerNode;

        }

        #endregion


        #region TryAcquireAsync(NodeId, CancellationToken = default)

        /// <summary>
        /// Wait for the lease of the given node; returns null immediately when too many
        /// requests are already waiting for it.
        /// </summary>
        /// <param name="NodeId">The identification of the targeted node.</param>
        /// <param name="CancellationToken">A token to cancel the waiting.</param>
        /// <returns>The lease to dispose after the request was processed, or null when the queue is full.</returns>
        public async ValueTask<Lease?> TryAcquireAsync(Node_Id            NodeId,
                                                       CancellationToken  CancellationToken   = default)
        {

            ObjectDisposedException.ThrowIf(disposed, this);

            var queue = queues.GetOrAdd(NodeId, _ => new NodeQueue());

            if (Interlocked.Increment(ref queue.Requests) > MaxQueuedRequestsPerNode + 1)
            {
                Interlocked.Decrement(ref queue.Requests);
                return null;
            }

            try
            {
                await queue.Semaphore.WaitAsync(CancellationToken).ConfigureAwait(false);
            }
            catch
            {
                Interlocked.Decrement(ref queue.Requests);
                throw;
            }

            return new Lease(queue);

        }

        #endregion

        #region QueueLength(NodeId)

        /// <summary>
        /// The number of requests currently holding or waiting for the lease of the given node.
        /// </summary>
        /// <param name="NodeId">The identification of a node.</param>
        public Int32 QueueLength(Node_Id NodeId)

            => queues.TryGetValue(NodeId, out var queue)
                   ? Volatile.Read(ref queue.Requests)
                   : 0;

        #endregion


        #region (class) Lease

        /// <summary>
        /// The lease of a node; disposing it lets the next request proceed.
        /// </summary>
        public sealed class Lease : IDisposable
        {

            private readonly NodeQueue  queue;
            private          Int32      released;

            internal Lease(NodeQueue Queue)
            {
                this.queue = Queue;
            }

            /// <summary>
            /// Release the lease.
            /// </summary>
            public void Dispose()
            {

                if (Interlocked.Exchange(ref released, 1) == 0)
                {
                    Interlocked.Decrement(ref queue.Requests);
                    queue.Semaphore.Release();
                }

            }

        }

        #endregion

        #region Dispose()

        /// <summary>
        /// Release the resources of this rate limiter.
        /// </summary>
        public void Dispose()
        {

            if (disposed)
                return;

            disposed = true;

            foreach (var queue in queues.Values)
                queue.Semaphore.Dispose();

            queues.Clear();

        }

        #endregion

    }

}

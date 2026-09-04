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

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The per-node lease of requestPairing (S2 Connect 1.0.0, "2. Calculate
    /// clientHmacChallengeResponse": one pairing attempt per node per second, different
    /// nodes in parallel; PLAN.md D16): sequential leases, a bounded queue per node,
    /// cancellation and disposal.
    /// </summary>
    [TestFixture]
    public sealed class PairingRateLimiterTests
    {

        #region Data

        private static readonly TimeSpan  ShortDelay   = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan  WaitTimeout  = TimeSpan.FromSeconds(5);

        #endregion


        #region TryAcquireAsync(...) returns a lease

        [Test]
        [S2C("Pairing.2.RateLimit")]
        public async Task TryAcquireAsync_ReturnsALease_ThatIsCountedUntilDisposed()
        {

            using var limiter = new PairingRateLimiter();
            var nodeId        = Node_Id.NewRandom;

            Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(0), "unknown nodes have an empty queue");

            var lease = await limiter.TryAcquireAsync(nodeId);

            Assert.That(lease,                       Is.Not.Null);
            Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(1));

            lease!.Dispose();

            Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(0));

        }

        #endregion

        #region A second request for the same node waits for the first lease

        [Test]
        [S2C("Pairing.2.RateLimit")]
        public async Task TryAcquireAsync_SameNode_WaitsUntilTheFirstLeaseIsDisposed()
        {

            using var limiter = new PairingRateLimiter();
            var nodeId        = Node_Id.NewRandom;

            var first         = await limiter.TryAcquireAsync(nodeId);
            Assert.That(first, Is.Not.Null);

            var second        = limiter.TryAcquireAsync(nodeId).AsTask();

            await Task.Delay(ShortDelay);

            Assert.Multiple(() => {
                Assert.That(second.IsCompleted,          Is.False, "the second request waits for the lease");
                Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(2));
            });

            first!.Dispose();

            var secondLease = await second.WaitAsync(WaitTimeout);

            Assert.Multiple(() => {
                Assert.That(secondLease,                 Is.Not.Null);
                Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(1));
            });

            secondLease!.Dispose();

            Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(0));

        }

        #endregion

        #region Different nodes proceed in parallel

        [Test]
        [S2C("Pairing.2.RateLimit")]
        public async Task TryAcquireAsync_DifferentNodes_ProceedInParallel()
        {

            using var limiter = new PairingRateLimiter();
            var nodeA         = Node_Id.NewRandom;
            var nodeB         = Node_Id.NewRandom;

            var leaseA        = await limiter.TryAcquireAsync(nodeA);
            Assert.That(leaseA, Is.Not.Null);

            var pendingB      = limiter.TryAcquireAsync(nodeB);

            Assert.That(pendingB.IsCompleted, Is.True, "another node is not blocked by the lease of node A");

            var leaseB        = await pendingB;

            Assert.Multiple(() => {
                Assert.That(leaseB,                     Is.Not.Null);
                Assert.That(limiter.QueueLength(nodeA), Is.EqualTo(1));
                Assert.That(limiter.QueueLength(nodeB), Is.EqualTo(1));
            });

            leaseA!.Dispose();
            leaseB!.Dispose();

        }

        #endregion

        #region The queue per node is bounded

        [Test]
        [S2C("Pairing.2.RateLimit")]
        public async Task TryAcquireAsync_WithOneQueuedRequestAllowed_RejectsTheThirdRequestImmediately()
        {

            using var limiter = new PairingRateLimiter(MaxQueuedRequestsPerNode: 1);
            var nodeId        = Node_Id.NewRandom;

            var holder        = await limiter.TryAcquireAsync(nodeId);
            var waiter        = limiter.TryAcquireAsync(nodeId).AsTask();
            var third         = limiter.TryAcquireAsync(nodeId);

            Assert.Multiple(() => {
                Assert.That(limiter.MaxQueuedRequestsPerNode, Is.EqualTo(1));
                Assert.That(holder,                           Is.Not.Null);
                Assert.That(waiter.IsCompleted,               Is.False);
                Assert.That(third.IsCompleted,                Is.True,  "a rejected request completes immediately");
            });

            Assert.That(await third,                 Is.Null, "the queue is full");
            Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(2), "the rejected request is not counted");

            holder!.Dispose();

            var waiterLease = await waiter.WaitAsync(WaitTimeout);

            Assert.That(waiterLease,                 Is.Not.Null);
            Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(1));

            waiterLease!.Dispose();

            Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(0));

        }

        [Test]
        [S2C("Pairing.2.RateLimit")]
        public async Task TryAcquireAsync_WithoutQueue_RejectsEverySecondRequestWhileTheLeaseIsHeld()
        {

            using var limiter = new PairingRateLimiter(MaxQueuedRequestsPerNode: 0);
            var nodeId        = Node_Id.NewRandom;

            var holder        = await limiter.TryAcquireAsync(nodeId);

            Assert.That(holder,                             Is.Not.Null);
            Assert.That(await limiter.TryAcquireAsync(nodeId), Is.Null);
            Assert.That(limiter.QueueLength(nodeId),        Is.EqualTo(1));

            holder!.Dispose();

            var next = await limiter.TryAcquireAsync(nodeId);

            Assert.That(next, Is.Not.Null, "after the release the node is available again");

            next!.Dispose();

        }

        #endregion

        #region Disposing a lease twice releases once

        [Test]
        public async Task Lease_DisposedTwice_ReleasesOnlyOnce()
        {

            using var limiter = new PairingRateLimiter();
            var nodeId        = Node_Id.NewRandom;

            var lease         = await limiter.TryAcquireAsync(nodeId);

            lease!.Dispose();
            Assert.DoesNotThrow(lease!.Dispose);

            Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(0), "the counter never goes negative");

            // The semaphore was released exactly once: a new holder blocks the next request.
            var holder  = await limiter.TryAcquireAsync(nodeId);
            var waiter  = limiter.TryAcquireAsync(nodeId).AsTask();

            await Task.Delay(ShortDelay);

            Assert.Multiple(() => {
                Assert.That(holder,            Is.Not.Null);
                Assert.That(waiter.IsCompleted, Is.False, "only one lease per node exists at a time");
            });

            holder!.Dispose();

            (await waiter.WaitAsync(WaitTimeout))!.Dispose();

            Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(0));

        }

        #endregion

        #region Cancellation while waiting

        [Test]
        public async Task TryAcquireAsync_CancelledWhileWaiting_ThrowsAndDoesNotLeakTheCount()
        {

            using var limiter = new PairingRateLimiter();
            using var cts     = new CancellationTokenSource();
            var nodeId        = Node_Id.NewRandom;

            var holder        = await limiter.TryAcquireAsync(nodeId);
            var waiter        = limiter.TryAcquireAsync(nodeId, cts.Token).AsTask();

            Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(2));

            await cts.CancelAsync();

            Assert.CatchAsync<OperationCanceledException>(async () => await waiter.WaitAsync(WaitTimeout));

            Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(1), "the cancelled request no longer counts");

            holder!.Dispose();

            Assert.That(limiter.QueueLength(nodeId), Is.EqualTo(0));

            // The node is still usable afterwards.
            var again = await limiter.TryAcquireAsync(nodeId);

            Assert.That(again, Is.Not.Null);

            again!.Dispose();

        }

        #endregion

        #region Constructor guards and disposal

        [Test]
        public void Constructor_RejectsANegativeQueueLength()
        {
            Assert.That(() => new PairingRateLimiter(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_DefaultsToTheNormativeQueueLength()
        {

            using var limiter = new PairingRateLimiter();

            Assert.That(limiter.MaxQueuedRequestsPerNode, Is.EqualTo(S2ConnectDefaults.MaxQueuedPairingAttemptsPerNode));
            Assert.That(limiter.MaxQueuedRequestsPerNode, Is.EqualTo(4));

        }

        [Test]
        public async Task Dispose_MakesTryAcquireAsyncThrowObjectDisposedException()
        {

            var limiter = new PairingRateLimiter();
            var nodeId  = Node_Id.NewRandom;

            (await limiter.TryAcquireAsync(nodeId))!.Dispose();

            limiter.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(async () => await limiter.TryAcquireAsync(nodeId));
            Assert.DoesNotThrow(limiter.Dispose, "disposing twice is harmless");

        }

        #endregion

    }

}

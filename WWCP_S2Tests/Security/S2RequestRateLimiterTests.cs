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

using IPAddress = System.Net.IPAddress;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// The per-source token bucket of the S2 Connect servers (PLAN.md §11a) as a unit: the
    /// budget of an address, its refill, the independence of the addresses, the bounded number
    /// of buckets and their expiry.
    ///
    /// <para>
    /// Every decision is taken with an explicit timestamp, so these tests are fully
    /// deterministic and never sleep.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class S2RequestRateLimiterTests
    {

        #region Data

        /// <summary>
        /// The fixed "now" of every test; time only passes where a test says so.
        /// </summary>
        private static readonly DateTimeOffset  T0         = new (2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

        private static readonly IPAddress       AddressA   = IPAddress.Parse("192.168.1.7");
        private static readonly IPAddress       AddressB   = IPAddress.Parse("192.168.1.8");
        private static readonly IPAddress       AddressC   = IPAddress.Parse("192.168.1.9");
        private static readonly IPAddress       AddressV6  = IPAddress.Parse("2001:db8::1");

        #endregion


        // The budget of one remote address

        #region Capacity_IsSpentTokenByToken_ThenTheAddressIsRefused()

        [Test]
        public void Capacity_IsSpentTokenByToken_ThenTheAddressIsRefused()
        {

            var limiter  = new S2RequestRateLimiter("pairing", 3, TimeSpan.FromMinutes(1));

            var first    = limiter.TryAcquire(AddressA, T0);
            var second   = limiter.TryAcquire(AddressA, T0);
            var third    = limiter.TryAcquire(AddressA, T0);
            var refused  = limiter.TryAcquire(AddressA, T0);

            Assert.Multiple(() => {

                Assert.That(first.Allowed,            Is.True);
                Assert.That(first.RetryAfter,         Is.EqualTo(TimeSpan.Zero));
                Assert.That(first.RemainingTokens,    Is.EqualTo(2));

                Assert.That(second.Allowed,           Is.True);
                Assert.That(second.RemainingTokens,   Is.EqualTo(1));

                Assert.That(third.Allowed,            Is.True);
                Assert.That(third.RemainingTokens,    Is.EqualTo(0), "the last token of the budget");

                Assert.That(refused.Allowed,          Is.False, "the budget of the address is exhausted");
                Assert.That(refused.RemainingTokens,  Is.EqualTo(0));
                Assert.That(refused.RetryAfter,       Is.GreaterThan(TimeSpan.Zero));

            });

        }

        #endregion

        #region RetryAfter_IsPositive_AndCountsDownWhileTheBucketRefills()

        [Test]
        public void RetryAfter_IsPositive_AndCountsDownWhileTheBucketRefills()
        {

            // 4 requests per 8 seconds, i.e. one token every 2 seconds.
            var limiter = new S2RequestRateLimiter("pairing", 4, TimeSpan.FromSeconds(8));

            for (var i = 0; i < 4; i++)
                limiter.TryAcquire(AddressA, T0);

            var atOnce      = limiter.TryAcquire(AddressA, T0);
            var oneLater    = limiter.TryAcquire(AddressA, T0.AddSeconds(1));
            var justBefore  = limiter.TryAcquire(AddressA, T0.AddSeconds(1.5));

            Assert.Multiple(() => {

                Assert.That(atOnce.Allowed,                  Is.False);
                Assert.That(atOnce.RetryAfter.TotalSeconds,  Is.EqualTo(2.0).Within(0.01), "a whole token has to be refilled");

                // The waiting time shrinks with every second that the address stays silent.
                Assert.That(oneLater.Allowed,                    Is.False);
                Assert.That(oneLater.RetryAfter.TotalSeconds,    Is.EqualTo(1.0).Within(0.01));

                Assert.That(justBefore.Allowed,                  Is.False);
                Assert.That(justBefore.RetryAfter.TotalSeconds,  Is.EqualTo(0.5).Within(0.01));

                Assert.That(oneLater.RetryAfter,                 Is.LessThan(atOnce.RetryAfter));
                Assert.That(justBefore.RetryAfter,               Is.LessThan(oneLater.RetryAfter));

            });

        }

        #endregion

        #region RetryAfter_GrowsWithASlowerRefillPeriod()

        [Test]
        public void RetryAfter_GrowsWithASlowerRefillPeriod()
        {

            var fast  = new S2RequestRateLimiter("pairing", 4, TimeSpan.FromSeconds( 8));
            var slow  = new S2RequestRateLimiter("pairing", 4, TimeSpan.FromSeconds(16));

            for (var i = 0; i < 4; i++)
            {
                fast.TryAcquire(AddressA, T0);
                slow.TryAcquire(AddressA, T0);
            }

            var fastRefusal  = fast.TryAcquire(AddressA, T0);
            var slowRefusal  = slow.TryAcquire(AddressA, T0);

            Assert.Multiple(() => {

                Assert.That(fastRefusal.RetryAfter.TotalSeconds,  Is.EqualTo(2.0).Within(0.01));
                Assert.That(slowRefusal.RetryAfter.TotalSeconds,  Is.EqualTo(4.0).Within(0.01), "half the token rate, twice the waiting time");

                Assert.That(slowRefusal.RetryAfter,               Is.GreaterThan(fastRefusal.RetryAfter));

                // The waiting time never exceeds the period an empty bucket needs to refill completely.
                Assert.That(fastRefusal.RetryAfter,               Is.LessThanOrEqualTo(fast.RefillPeriod));
                Assert.That(slowRefusal.RetryAfter,               Is.LessThanOrEqualTo(slow.RefillPeriod));

            });

        }

        #endregion

        #region RefillPeriod_RestoresTheCompleteBudget()

        [Test]
        public void RefillPeriod_RestoresTheCompleteBudget()
        {

            var limiter = new S2RequestRateLimiter("pairing", 4, TimeSpan.FromSeconds(8));

            for (var i = 0; i < 4; i++)
                limiter.TryAcquire(AddressA, T0);

            Assert.That(limiter.TryAcquire(AddressA, T0).Allowed, Is.False, "the budget is exhausted");

            var afterRefill = new List<Boolean>();

            for (var i = 0; i < 5; i++)
                afterRefill.Add(limiter.TryAcquire(AddressA, T0 + limiter.RefillPeriod).Allowed);

            Assert.Multiple(() => {
                Assert.That(afterRefill.Take(4),  Is.All.True,  "the complete budget is available again");
                Assert.That(afterRefill[4],       Is.False,     "and not more than the capacity");
            });

        }

        #endregion

        #region PartialRefill_GivesAPartialBudget()

        [Test]
        public void PartialRefill_GivesAPartialBudget()
        {

            // 4 requests per 8 seconds: after half of the refill period, half of the budget is back.
            var limiter = new S2RequestRateLimiter("pairing", 4, TimeSpan.FromSeconds(8));

            for (var i = 0; i < 4; i++)
                limiter.TryAcquire(AddressA, T0);

            var halfway  = T0.AddSeconds(4);

            var first    = limiter.TryAcquire(AddressA, halfway);
            var second   = limiter.TryAcquire(AddressA, halfway);
            var refused  = limiter.TryAcquire(AddressA, halfway);

            Assert.Multiple(() => {
                Assert.That(first.Allowed,           Is.True);
                Assert.That(first.RemainingTokens,   Is.EqualTo(1));
                Assert.That(second.Allowed,          Is.True);
                Assert.That(second.RemainingTokens,  Is.EqualTo(0));
                Assert.That(refused.Allowed,         Is.False, "only the refilled half of the budget is available");
            });

        }

        #endregion

        #region Budget_DoesNotAccumulateBeyondTheCapacity()

        [Test]
        public void Budget_DoesNotAccumulateBeyondTheCapacity()
        {

            var limiter = new S2RequestRateLimiter("pairing", 4, TimeSpan.FromSeconds(8));

            limiter.TryAcquire(AddressA, T0);

            // A silent hour does not buy a larger burst than the capacity.
            var afterAnHour = new List<Boolean>();

            for (var i = 0; i < 5; i++)
                afterAnHour.Add(limiter.TryAcquire(AddressA, T0.AddHours(1)).Allowed);

            Assert.Multiple(() => {
                Assert.That(afterAnHour.Take(4),  Is.All.True);
                Assert.That(afterAnHour[4],       Is.False, "the bucket never holds more than its capacity");
            });

        }

        #endregion


        // The addresses

        #region DifferentAddresses_HaveIndependentBudgets()

        [Test]
        public void DifferentAddresses_HaveIndependentBudgets()
        {

            var limiter = new S2RequestRateLimiter("pairing", 1, TimeSpan.FromMinutes(10));

            var firstA   = limiter.TryAcquire(AddressA, T0);
            var secondA  = limiter.TryAcquire(AddressA, T0);
            var firstB   = limiter.TryAcquire(AddressB, T0);
            var v6       = limiter.TryAcquire(AddressV6, T0);

            Assert.Multiple(() => {
                Assert.That(firstA.Allowed,    Is.True);
                Assert.That(secondA.Allowed,   Is.False, "the budget of an address is spent");
                Assert.That(firstB.Allowed,    Is.True,  "another address is unaffected by it");
                Assert.That(v6.Allowed,        Is.True,  "an IPv6 address is another address");
                Assert.That(limiter.BucketCount, Is.EqualTo(3));
            });

        }

        #endregion

        #region UnattributableRequests_ShareOneBucket()

        [Test]
        public void UnattributableRequests_ShareOneBucket()
        {

            var limiter = new S2RequestRateLimiter("pairing", 2, TimeSpan.FromMinutes(10));

            var first    = limiter.TryAcquire(null, T0);
            var second   = limiter.TryAcquire(null, T0);
            var refused  = limiter.TryAcquire(null, T0);

            Assert.Multiple(() => {

                Assert.That(first.Allowed,        Is.True);
                Assert.That(second.Allowed,       Is.True);
                Assert.That(refused.Allowed,      Is.False, "requests without a remote address must not get an unlimited budget");

                Assert.That(limiter.BucketCount,  Is.EqualTo(1), "all of them share the bucket of the unknown address");

                // A known address keeps its own budget next to the shared one.
                Assert.That(limiter.TryAcquire(AddressA, T0).Allowed, Is.True);
                Assert.That(limiter.BucketCount,  Is.EqualTo(2));

            });

        }

        #endregion


        // The bounded number of buckets

        #region BucketCount_GrowsPerAddress_AndStaysBoundedByMaximumBuckets()

        [Test]
        public void BucketCount_GrowsPerAddress_AndStaysBoundedByMaximumBuckets()
        {

            var limiter = new S2RequestRateLimiter("pairing", 5, TimeSpan.FromMinutes(10), MaximumBuckets: 3);

            var a  = limiter.TryAcquire(AddressA,  T0);
            var b  = limiter.TryAcquire(AddressB,  T0);
            var c  = limiter.TryAcquire(AddressC,  T0);

            Assert.Multiple(() => {
                Assert.That(a.Allowed,            Is.True);
                Assert.That(b.Allowed,            Is.True);
                Assert.That(c.Allowed,            Is.True);
                Assert.That(limiter.BucketCount,  Is.EqualTo(3), "one bucket per remote address");
            });

            // Beyond the bound the limiter refuses strangers instead of remembering them:
            // spoofed source addresses must not turn it into a memory exhaustion attack.
            var beyond = limiter.TryAcquire(AddressV6, T0);

            Assert.Multiple(() => {
                Assert.That(beyond.Allowed,          Is.False);
                Assert.That(beyond.RemainingTokens,  Is.EqualTo(0));
                Assert.That(beyond.RetryAfter,       Is.EqualTo(limiter.BucketLifetime), "the stranger has to wait for a free slot");
                Assert.That(limiter.BucketCount,     Is.EqualTo(3), "the refused address is not remembered");
            });

        }

        #endregion

        #region BeyondTheBucketBound_TheKnownAddressesAreStillServed()

        [Test]
        public void BeyondTheBucketBound_TheKnownAddressesAreStillServed()
        {

            var limiter = new S2RequestRateLimiter("pairing", 3, TimeSpan.FromMinutes(10), MaximumBuckets: 2);

            limiter.TryAcquire(AddressA, T0);
            limiter.TryAcquire(AddressB, T0);

            Assert.That(limiter.TryAcquire(AddressC, T0).Allowed, Is.False, "the bound is reached");

            Assert.Multiple(() => {
                Assert.That(limiter.TryAcquire(AddressA, T0).Allowed,  Is.True, "a known address keeps its remaining budget");
                Assert.That(limiter.TryAcquire(AddressB, T0).Allowed,  Is.True);
                Assert.That(limiter.BucketCount,                       Is.EqualTo(2));
            });

        }

        #endregion

        #region UnusedBucket_ExpiresAfterTheBucketLifetime_AndFreesItsSlot()

        [Test]
        public void UnusedBucket_ExpiresAfterTheBucketLifetime_AndFreesItsSlot()
        {

            // The lifetime must not be shorter than the refill period (a paused address would
            // otherwise reset its budget), so both are one minute here.
            var lifetime  = TimeSpan.FromMinutes(1);
            var limiter   = new S2RequestRateLimiter("pairing", 1, lifetime,
                                                     MaximumBuckets:  1,
                                                     BucketLifetime:  lifetime);

            Assert.Multiple(() => {
                Assert.That(limiter.TryAcquire(AddressA, T0).Allowed,  Is.True);
                Assert.That(limiter.TryAcquire(AddressB, T0).Allowed,  Is.False, "the only slot is taken");
                Assert.That(limiter.BucketCount,                       Is.EqualTo(1));
            });

            // The bucket of A was not used for its lifetime, so its slot belongs to B now.
            var afterExpiry = limiter.TryAcquire(AddressB, T0 + lifetime);

            Assert.Multiple(() => {
                Assert.That(afterExpiry.Allowed,  Is.True, "the unused bucket expired and freed its slot");
                Assert.That(limiter.BucketCount,  Is.EqualTo(1), "and the bound still holds");
            });

        }

        #endregion

        #region ExpiredBucket_IsForgotten_AndStartsWithAFullBudget()

        [Test]
        public void ExpiredBucket_IsForgotten_AndStartsWithAFullBudget()
        {

            // Lifetime and refill period are equal, the shortest sound combination: a bucket that
            // is forgotten before it has refilled would let an address reset its budget by pausing,
            // which the constructor refuses (see BucketLifetimeBelowTheRefillPeriod_IsRefused).
            var lifetime  = TimeSpan.FromMinutes(1);
            var limiter   = new S2RequestRateLimiter("pairing", 2, lifetime,
                                                     BucketLifetime: lifetime);

            limiter.TryAcquire(AddressA, T0);
            limiter.TryAcquire(AddressA, T0);

            Assert.Multiple(() => {
                Assert.That(limiter.TryAcquire(AddressA, T0).Allowed,  Is.False, "the budget is exhausted");
                Assert.That(limiter.BucketCount,                       Is.EqualTo(1));
            });

            // Every request touches the bucket, so the lifetime only elapses while the address is
            // silent: half a minute of silence is neither a full lifetime nor a full refill.
            var beforeExpiry  = limiter.TryAcquire(AddressA, T0.AddSeconds(30));

            Assert.That(beforeExpiry.RemainingTokens, Is.EqualTo(0), "only part of the budget has refilled");

            // After a full lifetime of silence the address is forgotten - and whether the budget
            // comes from the eviction or from the refill, it is the whole budget, never more.
            var afterExpiry   = limiter.TryAcquire(AddressA, T0.AddSeconds(30) + lifetime);

            Assert.Multiple(() => {
                Assert.That(afterExpiry.Allowed,          Is.True);
                Assert.That(afterExpiry.RemainingTokens,  Is.EqualTo(1), "a forgotten address starts with a full bucket");
            });

        }

        #endregion

        #region BucketLifetimeBelowTheRefillPeriod_IsRefused()

        [Test]
        public void BucketLifetimeBelowTheRefillPeriod_IsRefused()
        {

            // A bucket forgotten before it has refilled hands out a fresh, full budget, so an
            // address would reset its budget simply by pausing - the limit would be no limit.
            Assert.Multiple(() => {

                Assert.That(() => new S2RequestRateLimiter("pairing", 10, TimeSpan.FromMinutes(10),
                                                           BucketLifetime: TimeSpan.FromMinutes(1)),
                            Throws.TypeOf<ArgumentOutOfRangeException>());

                // Equal is sound and accepted.
                Assert.That(() => new S2RequestRateLimiter("pairing", 10, TimeSpan.FromMinutes(10),
                                                           BucketLifetime: TimeSpan.FromMinutes(10)),
                            Throws.Nothing);

            });

        }

        #endregion


        // Keys, text representation and the constructor

        #region KeyOf_ContainsTheScopeAndTheAddress()

        [Test]
        public void KeyOf_ContainsTheScopeAndTheAddress()
        {

            var limiter = new S2RequestRateLimiter("sessionInitiation", 10, TimeSpan.FromMinutes(1));

            Assert.Multiple(() => {

                Assert.That(limiter.KeyOf(AddressA),   Does.Contain("sessionInitiation").
                                                       And.Contain("192.168.1.7"));

                Assert.That(limiter.KeyOf(AddressV6),  Does.Contain("2001:db8::1"));

                Assert.That(limiter.KeyOf(null),       Does.Contain("sessionInitiation").
                                                       And.Contain(S2RequestRateLimiter.UnknownAddress));

                // Different addresses never share a key, and the scope separates the limiters
                // of the pairing and the session initiation server of one host.
                Assert.That(limiter.KeyOf(AddressA),   Is.Not.EqualTo(limiter.KeyOf(AddressB)));
                Assert.That(limiter.KeyOf(AddressA),   Is.Not.EqualTo(new S2RequestRateLimiter("pairing", 10, TimeSpan.FromMinutes(1)).KeyOf(AddressA)));

            });

        }

        #endregion

        #region ToString_MentionsTheScopeAndTheCapacity()

        [Test]
        public void ToString_MentionsTheScopeAndTheCapacity()
        {

            var limiter = new S2RequestRateLimiter("pairing", 300, TimeSpan.FromMinutes(1), MaximumBuckets: 5000);

            limiter.TryAcquire(AddressA, T0);

            var text = limiter.ToString();

            Assert.Multiple(() => {
                Assert.That(text, Does.Contain("pairing"));
                Assert.That(text, Does.Contain("300"),   "the capacity");
                Assert.That(text, Does.Contain("60"),    "the refill period in seconds");
                Assert.That(text, Does.Contain("1/5000"), "the used and the maximal number of buckets");
            });

        }

        #endregion

        #region Constructor_UsesTheDocumentedDefaults()

        [Test]
        public void Constructor_UsesTheDocumentedDefaults()
        {

            var oneMinute  = new S2RequestRateLimiter("pairing", 300, TimeSpan.FromMinutes( 1));
            var oneHour    = new S2RequestRateLimiter("pairing", 300, TimeSpan.FromMinutes(60));
            var explicitly = new S2RequestRateLimiter("pairing", 300, TimeSpan.FromMinutes( 1),
                                                      MaximumBuckets:  7,
                                                      BucketLifetime:  TimeSpan.FromMinutes(2));

            Assert.Multiple(() => {

                Assert.That(oneMinute.Scope,           Is.EqualTo("pairing"));
                Assert.That(oneMinute.Capacity,        Is.EqualTo(300));
                Assert.That(oneMinute.RefillPeriod,    Is.EqualTo(TimeSpan.FromMinutes(1)));
                Assert.That(oneMinute.BucketCount,     Is.EqualTo(0), "buckets are created on demand");

                Assert.That(oneMinute.MaximumBuckets,  Is.EqualTo(S2RequestRateLimiter.DefaultMaximumBuckets));
                Assert.That(oneMinute.MaximumBuckets,  Is.EqualTo(10_000), "the documented default");

                // The bucket lifetime is at least ten minutes, but never shorter than the
                // refill period: an address must not reset its budget by pausing.
                Assert.That(oneMinute.BucketLifetime,  Is.EqualTo(TimeSpan.FromMinutes(10)));
                Assert.That(oneHour.BucketLifetime,    Is.EqualTo(TimeSpan.FromMinutes(60)));

                Assert.That(explicitly.MaximumBuckets, Is.EqualTo(7));
                Assert.That(explicitly.BucketLifetime, Is.EqualTo(TimeSpan.FromMinutes(2)));

            });

        }

        #endregion

        #region Constructor_RejectsAnEmptyScope()

        [Test]
        [TestCase("",     TestName = "S2RequestRateLimiter: an empty scope is rejected")]
        [TestCase("   ",  TestName = "S2RequestRateLimiter: a whitespace scope is rejected")]
        public void Constructor_RejectsAnEmptyScope(String Scope)
        {

            Assert.That(() => new S2RequestRateLimiter(Scope, 10, TimeSpan.FromMinutes(1)),
                        Throws.ArgumentException);

        }

        #endregion

        #region Constructor_RejectsAnInvalidBudget()

        [Test]
        public void Constructor_RejectsAnInvalidBudget()
        {

            Assert.Multiple(() => {

                Assert.That(() => new S2RequestRateLimiter("pairing",  0, TimeSpan.FromMinutes(1)),
                            Throws.TypeOf<ArgumentOutOfRangeException>(),
                            "a capacity below one would refuse everything");

                Assert.That(() => new S2RequestRateLimiter("pairing", -1, TimeSpan.FromMinutes(1)),
                            Throws.TypeOf<ArgumentOutOfRangeException>());

                Assert.That(() => new S2RequestRateLimiter("pairing", 10, TimeSpan.Zero),
                            Throws.TypeOf<ArgumentOutOfRangeException>(),
                            "a bucket without a refill period would never refill");

                Assert.That(() => new S2RequestRateLimiter("pairing", 10, TimeSpan.FromSeconds(-1)),
                            Throws.TypeOf<ArgumentOutOfRangeException>());

                Assert.That(() => new S2RequestRateLimiter("pairing", 10, Timeout.InfiniteTimeSpan),
                            Throws.TypeOf<ArgumentOutOfRangeException>());

                Assert.That(() => new S2RequestRateLimiter("pairing", 10, TimeSpan.FromMinutes(1), MaximumBuckets:  0),
                            Throws.TypeOf<ArgumentOutOfRangeException>(),
                            "at least one remote address must be remembered");

                Assert.That(() => new S2RequestRateLimiter("pairing", 10, TimeSpan.FromMinutes(1), BucketLifetime:  TimeSpan.Zero),
                            Throws.TypeOf<ArgumentOutOfRangeException>());

            });

        }

        #endregion

    }

}

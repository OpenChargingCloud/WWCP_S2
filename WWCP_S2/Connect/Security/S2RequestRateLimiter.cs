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

using System.Net;
using IPAddress = System.Net.IPAddress;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The per-source token bucket of an S2 Connect server (PLAN.md §11a): every remote address
    /// gets a bucket of <see cref="Capacity"/> requests that refills completely within
    /// <see cref="RefillPeriod"/>. A request beyond that budget is refused before any parsing,
    /// any store access and any cryptography happens, which is what keeps a flood cheap for
    /// the server.
    ///
    /// <para>
    /// This is denial-of-service protection and is <em>not</em> the brute-force protection the
    /// specification prescribes: the mandatory sequential handling of the pairing attempts of a
    /// node ("each second only one pairing attempt can be processed for a node") lives in
    /// <see cref="PairingRateLimiter"/> and stays in force whether or not this limiter is used.
    /// </para>
    ///
    /// <para>
    /// The number of buckets is bounded, so that the limiter itself cannot be turned into a
    /// memory exhaustion attack by spoofed source addresses. Once the bound is reached, new
    /// addresses are refused until an unused bucket expires: under an address flood the server
    /// stops serving strangers rather than falling over. There is no least-recently-used
    /// eviction, so that flood does lock out genuinely new clients for the bucket lifetime —
    /// the deliberate trade of availability for strangers against staying up for known peers.
    /// </para>
    ///
    /// <para>
    /// Two routes bypass this limiter and cannot be covered from here: a path this API never
    /// registered and a wrong method on a registered path are answered by the HTTP server itself,
    /// before any handler of this API runs. Bounding those belongs to the HTTP server or the
    /// reverse proxy in front of it.
    /// </para>
    /// </summary>
    public sealed class S2RequestRateLimiter
    {

        #region Data

        /// <summary>
        /// The key of requests whose remote address could not be determined. All of them share
        /// one bucket: an unattributable request must not get an unlimited budget.
        /// </summary>
        public const String  UnknownAddress          = "unknown";

        /// <summary>
        /// The default maximal number of buckets.
        /// </summary>
        public const Int32   DefaultMaximumBuckets   = 10_000;

        private readonly InMemoryTokenBucketRateLimiter  limiter;

        #endregion

        #region Properties

        /// <summary>
        /// The scope of this limiter, e.g. "pairing"; part of every bucket key.
        /// </summary>
        public String    Scope             { get; }

        /// <summary>
        /// The number of requests one remote address may send before it has to wait.
        /// </summary>
        public Int32     Capacity          { get; }

        /// <summary>
        /// The duration within which an empty bucket refills completely.
        /// </summary>
        public TimeSpan  RefillPeriod      { get; }

        /// <summary>
        /// The maximal number of buckets, i.e. of remembered remote addresses.
        /// </summary>
        public Int32     MaximumBuckets    { get; }

        /// <summary>
        /// How long an unused bucket is remembered.
        /// </summary>
        public TimeSpan  BucketLifetime    { get; }

        /// <summary>
        /// The number of remote addresses currently remembered.
        /// </summary>
        public Int32     BucketCount
            => limiter.BucketCount;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new per-source rate limiter.
        /// </summary>
        /// <param name="Scope">The scope of this limiter, e.g. "pairing".</param>
        /// <param name="Capacity">The number of requests one remote address may send before it has to wait.</param>
        /// <param name="RefillPeriod">The duration within which an empty bucket refills completely.</param>
        /// <param name="MaximumBuckets">The maximal number of remembered remote addresses (default: 10.000).</param>
        /// <param name="BucketLifetime">How long an unused bucket is remembered (default: the refill period, at least 10 minutes).</param>
        public S2RequestRateLimiter(String     Scope,
                                    Int32      Capacity,
                                    TimeSpan   RefillPeriod,
                                    Int32?     MaximumBuckets   = null,
                                    TimeSpan?  BucketLifetime   = null)
        {

            if (String.IsNullOrWhiteSpace(Scope))
                throw new ArgumentException("The scope of a rate limiter must not be empty!", nameof(Scope));

            this.Scope           = Scope;
            this.Capacity        = Capacity;
            this.RefillPeriod    = RefillPeriod;
            this.MaximumBuckets  = MaximumBuckets ?? DefaultMaximumBuckets;
            this.BucketLifetime  = BucketLifetime ?? (RefillPeriod > TimeSpan.FromMinutes(10)
                                                          ? RefillPeriod
                                                          : TimeSpan.FromMinutes(10));

            // A bucket that is forgotten before it has refilled hands out a fresh, full budget,
            // so an address would reset its budget simply by pausing - the limit would be no
            // limit at all. The computed default is always long enough.
            if (this.BucketLifetime < RefillPeriod)
                throw new ArgumentOutOfRangeException(
                          nameof(BucketLifetime),
                          $"The bucket lifetime ({this.BucketLifetime}) must not be shorter than the refill period ({RefillPeriod}); an address could reset its budget by pausing!"
                      );

            this.limiter         = new InMemoryTokenBucketRateLimiter(
                                       Capacity,
                                       RefillPeriod,
                                       this.MaximumBuckets,
                                       this.BucketLifetime
                                   );

        }

        #endregion


        #region TryAcquire(RemoteAddress, Timestamp = null)

        /// <summary>
        /// Take one token from the bucket of the given remote address.
        /// </summary>
        /// <param name="RemoteAddress">The remote address of the request, or null when it is unknown.</param>
        /// <param name="Timestamp">The timestamp of the request (default: now).</param>
        public RateLimitDecision TryAcquire(IPAddress?       RemoteAddress,
                                            DateTimeOffset?  Timestamp   = null)

            => limiter.TryAcquire(
                   KeyOf(RemoteAddress),
                   Timestamp
               );

        #endregion

        #region KeyOf(RemoteAddress)

        /// <summary>
        /// The bucket key of the given remote address.
        /// </summary>
        /// <param name="RemoteAddress">A remote address, or null when it is unknown.</param>
        public String KeyOf(IPAddress? RemoteAddress)

            => String.Concat(
                   Scope,
                   "|ip|",
                   RemoteAddress?.ToString() ?? UnknownAddress
               );

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Scope}: {Capacity} requests per {RefillPeriod.TotalSeconds} seconds and remote address ({BucketCount}/{MaximumBuckets} buckets)";

        #endregion

    }

}

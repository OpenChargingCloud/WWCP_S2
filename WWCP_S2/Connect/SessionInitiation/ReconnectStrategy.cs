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

using System.Security.Cryptography;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The exponential back-off of the reconnection strategy (S2 Connect 1.0.0, "Reconnection
    /// strategy"): delay_n = random(0, min(max_delay, base_delay × 2^n)) with base_delay = 2 s
    /// and max_delay = 600 s, n starting at 0 and counted from the last failed attempt.
    /// </summary>
    public sealed class ReconnectStrategy
    {

        #region Data

        private readonly Lock  lockObject = new ();
        private          Int32  attempt;

        #endregion

        #region Properties

        /// <summary>
        /// The base delay (default: 2 seconds).
        /// </summary>
        public TimeSpan  BaseDelay    { get; }

        /// <summary>
        /// The maximal delay (default: 600 seconds).
        /// </summary>
        public TimeSpan  MaxDelay     { get; }

        /// <summary>
        /// The number of the next attempt (starting at 0).
        /// </summary>
        public Int32     Attempt
        {
            get
            {
                lock (lockObject)
                {
                    return attempt;
                }
            }
        }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new reconnection strategy.
        /// </summary>
        /// <param name="BaseDelay">The base delay (default: 2 seconds).</param>
        /// <param name="MaxDelay">The maximal delay (default: 600 seconds).</param>
        public ReconnectStrategy(TimeSpan?  BaseDelay   = null,
                                 TimeSpan?  MaxDelay    = null)
        {

            this.BaseDelay  = BaseDelay ?? S2ConnectDefaults.ReconnectBaseDelay;
            this.MaxDelay   = MaxDelay  ?? S2ConnectDefaults.ReconnectMaxDelay;

            if (this.BaseDelay <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(BaseDelay), "The base delay must be positive!");

            if (this.MaxDelay < this.BaseDelay)
                throw new ArgumentOutOfRangeException(nameof(MaxDelay), "The maximal delay must not be shorter than the base delay!");

        }

        #endregion


        #region UpperBound(Attempt)

        /// <summary>
        /// The upper bound of the delay of the given attempt: min(max_delay, base_delay × 2^n).
        /// </summary>
        /// <param name="Attempt">The number of the attempt (starting at 0).</param>
        public TimeSpan UpperBound(Int32 Attempt)
        {

            ArgumentOutOfRangeException.ThrowIfNegative(Attempt);

            if (Attempt >= 40)
                return MaxDelay;

            var factor  = Math.Pow(2, Attempt);
            var bound   = BaseDelay.TotalMilliseconds * factor;

            return bound >= MaxDelay.TotalMilliseconds
                       ? MaxDelay
                       : TimeSpan.FromMilliseconds(bound);

        }

        #endregion

        #region DelayFor(Attempt)

        /// <summary>
        /// A random delay for the given attempt: random(0, min(max_delay, base_delay × 2^n)).
        /// </summary>
        /// <param name="Attempt">The number of the attempt (starting at 0).</param>
        public TimeSpan DelayFor(Int32 Attempt)
        {

            var upper = (Int32) Math.Min(Int32.MaxValue - 1, UpperBound(Attempt).TotalMilliseconds);

            return TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(upper + 1));

        }

        #endregion

        #region NextDelay()

        /// <summary>
        /// The delay before the next attempt; advances the attempt counter.
        /// </summary>
        public TimeSpan NextDelay()
        {

            Int32 current;

            lock (lockObject)
            {
                current = attempt;
                attempt = attempt == Int32.MaxValue ? attempt : attempt + 1;
            }

            return DelayFor(current);

        }

        #endregion

        #region Reset()

        /// <summary>
        /// Reset the attempt counter after a successful connection.
        /// </summary>
        public void Reset()
        {
            lock (lockObject)
            {
                attempt = 0;
            }
        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"reconnect strategy: base {BaseDelay.TotalSeconds} s, max {MaxDelay.TotalSeconds} s, next attempt {Attempt}";

        #endregion

    }

}

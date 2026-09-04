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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// A duration in milliseconds, as used by S2 JSON for timers, transitions, forecasts
    /// and instruction processing delays. Non-negative integer milliseconds on the wire.
    /// </summary>
    public readonly struct Duration : IEquatable<Duration>,
                                      IComparable<Duration>,
                                      IComparable
    {

        #region Properties

        /// <summary>
        /// The duration in milliseconds.
        /// </summary>
        public Int64     Milliseconds    { get; }

        /// <summary>
        /// The duration as time span.
        /// </summary>
        public TimeSpan  TimeSpan
            => TimeSpan.FromMilliseconds(Milliseconds);

        /// <summary>
        /// The zero duration.
        /// </summary>
        public static Duration Zero
            => new (0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// The largest duration representable as a <see cref="System.TimeSpan"/>, in milliseconds.
        /// </summary>
        public static readonly Int64 MaxMilliseconds = (Int64) System.TimeSpan.MaxValue.TotalMilliseconds;

        /// <summary>
        /// Create a new duration.
        /// </summary>
        /// <param name="Milliseconds">The duration in milliseconds (non-negative, at most <see cref="MaxMilliseconds"/>).</param>
        public Duration(Int64 Milliseconds)
        {

            ArgumentOutOfRangeException.ThrowIfNegative   (Milliseconds);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(Milliseconds, MaxMilliseconds);

            this.Milliseconds = Milliseconds;

        }

        #endregion


        #region Documentation

        // Duration.schema.json
        //   "title":       "Duration",
        //   "type":        "integer",
        //   "minimum":     0,
        //   "description": "Duration in milliseconds"

        #endregion

        #region (static) FromMilliseconds(Milliseconds)

        /// <summary>
        /// Create a duration from the given number of milliseconds.
        /// </summary>
        /// <param name="Milliseconds">The duration in milliseconds (non-negative).</param>
        public static Duration FromMilliseconds(Int64 Milliseconds)
            => new (Milliseconds);

        #endregion

        #region (static) FromSeconds(Seconds)

        /// <summary>
        /// Create a duration from the given number of seconds.
        /// </summary>
        /// <param name="Seconds">The duration in seconds (non-negative).</param>
        public static Duration FromSeconds(Int64 Seconds)
            => new (checked(Seconds * 1000));

        #endregion

        #region (static) FromTimeSpan(TimeSpan)

        /// <summary>
        /// Create a duration from the given time span. Sub-millisecond parts are truncated.
        /// </summary>
        /// <param name="TimeSpan">A non-negative time span.</param>
        public static Duration FromTimeSpan(TimeSpan TimeSpan)
            => new (TimeSpan.Ticks / System.TimeSpan.TicksPerMillisecond);

        #endregion

        #region (static) TryParse(Token, out Duration, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON token as a duration.
        /// </summary>
        /// <param name="Token">A JSON token, expected to be a non-negative integer.</param>
        /// <param name="Duration">The parsed duration.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JToken                            Token,
                                       out Duration                      Duration,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Duration = default;

            if (Token.Type == JTokenType.Integer)
            {

                Int64 milliseconds;

                try
                {
                    milliseconds = Token.Value<Int64>();
                }
                catch (Exception)
                {
                    // an integer beyond the range of Int64
                    ErrorResponse = "a duration is out of range!";
                    return false;
                }

                if (milliseconds < 0)
                {
                    ErrorResponse = $"a duration must not be negative, but '{milliseconds}' was given!";
                    return false;
                }

                if (milliseconds > MaxMilliseconds)
                {
                    ErrorResponse = $"a duration must not exceed {MaxMilliseconds} ms, but '{milliseconds}' was given!";
                    return false;
                }

                Duration       = new Duration(milliseconds);
                ErrorResponse  = null;
                return true;

            }

            // A JSON number that happens to be integral, e.g. 3000.0
            if (Token.Type == JTokenType.Float)
            {

                var value = Token.Value<Double>();

                if (value >= 0 && Math.Floor(value) == value && value <= MaxMilliseconds)
                {
                    Duration       = new Duration((Int64) value);
                    ErrorResponse  = null;
                    return true;
                }

            }

            ErrorResponse = "a duration must be a non-negative integer number of milliseconds!";
            return false;

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this duration: the integer number of milliseconds.
        /// </summary>
        public JValue ToJSON()
            => new (Milliseconds);

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two durations for equality.
        /// </summary>
        public static Boolean operator == (Duration Duration1, Duration Duration2)
            => Duration1.Equals(Duration2);

        /// <summary>
        /// Compares two durations for inequality.
        /// </summary>
        public static Boolean operator != (Duration Duration1, Duration Duration2)
            => !Duration1.Equals(Duration2);

        /// <summary>
        /// Compares two durations.
        /// </summary>
        public static Boolean operator <  (Duration Duration1, Duration Duration2)
            => Duration1.CompareTo(Duration2) < 0;

        /// <summary>
        /// Compares two durations.
        /// </summary>
        public static Boolean operator <= (Duration Duration1, Duration Duration2)
            => Duration1.CompareTo(Duration2) <= 0;

        /// <summary>
        /// Compares two durations.
        /// </summary>
        public static Boolean operator >  (Duration Duration1, Duration Duration2)
            => Duration1.CompareTo(Duration2) > 0;

        /// <summary>
        /// Compares two durations.
        /// </summary>
        public static Boolean operator >= (Duration Duration1, Duration Duration2)
            => Duration1.CompareTo(Duration2) >= 0;

        /// <summary>
        /// Adds two durations.
        /// </summary>
        public static Duration operator + (Duration Duration1, Duration Duration2)
            => new (checked(Duration1.Milliseconds + Duration2.Milliseconds));

        #endregion

        #region IComparable<Duration> Members

        /// <summary>
        /// Compares two durations.
        /// </summary>
        /// <param name="Object">A duration to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Duration duration
                   ? CompareTo(duration)
                   : throw new ArgumentException("The given object is not a duration!", nameof(Object));

        /// <summary>
        /// Compares two durations.
        /// </summary>
        /// <param name="Duration">A duration to compare with.</param>
        public Int32 CompareTo(Duration Duration)
            => Milliseconds.CompareTo(Duration.Milliseconds);

        #endregion

        #region IEquatable<Duration> Members

        /// <summary>
        /// Compares two durations for equality.
        /// </summary>
        /// <param name="Object">A duration to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Duration duration && Equals(duration);

        /// <summary>
        /// Compares two durations for equality.
        /// </summary>
        /// <param name="Duration">A duration to compare with.</param>
        public Boolean Equals(Duration Duration)
            => Milliseconds == Duration.Milliseconds;

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => Milliseconds.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"{Milliseconds} ms";

        #endregion

    }

}

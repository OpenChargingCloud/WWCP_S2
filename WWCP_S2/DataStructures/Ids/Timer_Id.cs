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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// Extension methods for timer identifications.
    /// </summary>
    public static class TimerIdExtensions
    {

        /// <summary>
        /// Indicates whether this timer identification is null or empty.
        /// </summary>
        /// <param name="TimerId">A timer identification.</param>
        public static Boolean IsNullOrEmpty(this Timer_Id? TimerId)
            => !TimerId.HasValue || TimerId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this timer identification is NOT null or empty.
        /// </summary>
        /// <param name="TimerId">A timer identification.</param>
        public static Boolean IsNotNullOrEmpty(this Timer_Id? TimerId)
            => TimerId.HasValue && TimerId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The identification of a timer. Must be unique in the scope of the OMBC.SystemDescription, FRBC.ActuatorDescription or DDBC.ActuatorDescription in which it is used.
    /// </summary>
    public readonly struct Timer_Id : IId,
                                        IEquatable<Timer_Id>,
                                        IComparable<Timer_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the timer identification.
        /// </summary>
        public String   Value               { get; }

        /// <summary>
        /// Indicates whether this identification is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => Value.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this identification is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => Value.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the timer identification.
        /// </summary>
        public UInt64   Length
            => (UInt64) (Value?.Length ?? 0);

        /// <summary>
        /// Whether this identification is a UUID (the form s2-python requires).
        /// </summary>
        public Boolean  IsUUID
            => S2_Id.IsUUID(Value);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new timer identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a timer identification.</param>
        private Timer_Id(String Text)
        {
            this.Value = Text;
        }

        #endregion


        #region Documentation

        // ID.schema.json
        //   "title":       "ID",
        //   "type":        "string",
        //   "pattern":     "[a-zA-Z0-9\\-_:]{2,64}",
        //   "description": "An identifier expressed as a UUID"
        //
        // Timer.schema.json
        //   "id": { "$ref": "../schemas/ID.schema.json",
        //           "description": "ID of the Timer. Must be unique in the scope of the OMBC.SystemDescription, FRBC.ActuatorDescription or DDBC.ActuatorDescription in which it is used." }

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) timer identification.
        /// </summary>
        public static Timer_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a timer identification.
        /// </summary>
        /// <param name="Text">A text representation of a timer identification.</param>
        public static Timer_Id Parse(String Text)
        {

            if (TryParse(Text, out var timerId))
                return timerId;

            throw new ArgumentException($"Invalid text representation of a timer identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a timer identification.
        /// </summary>
        /// <param name="Text">A text representation of a timer identification.</param>
        public static Timer_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var timerId))
                return timerId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out TimerId)

        /// <summary>
        /// Try to parse the given text as a timer identification.
        /// </summary>
        /// <param name="Text">A text representation of a timer identification.</param>
        /// <param name="TimerId">The parsed timer identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out Timer_Id    TimerId)
        {

            if (S2_Id.IsValid(Text))
            {
                TimerId = new Timer_Id(Text);
                return true;
            }

            TimerId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this timer identification.
        /// </summary>
        public Timer_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two timer identifications for equality.
        /// </summary>
        public static Boolean operator == (Timer_Id TimerId1, Timer_Id TimerId2)
            => TimerId1.Equals(TimerId2);

        /// <summary>
        /// Compares two timer identifications for inequality.
        /// </summary>
        public static Boolean operator != (Timer_Id TimerId1, Timer_Id TimerId2)
            => !TimerId1.Equals(TimerId2);

        /// <summary>
        /// Compares two timer identifications.
        /// </summary>
        public static Boolean operator <  (Timer_Id TimerId1, Timer_Id TimerId2)
            => TimerId1.CompareTo(TimerId2) < 0;

        /// <summary>
        /// Compares two timer identifications.
        /// </summary>
        public static Boolean operator <= (Timer_Id TimerId1, Timer_Id TimerId2)
            => TimerId1.CompareTo(TimerId2) <= 0;

        /// <summary>
        /// Compares two timer identifications.
        /// </summary>
        public static Boolean operator >  (Timer_Id TimerId1, Timer_Id TimerId2)
            => TimerId1.CompareTo(TimerId2) > 0;

        /// <summary>
        /// Compares two timer identifications.
        /// </summary>
        public static Boolean operator >= (Timer_Id TimerId1, Timer_Id TimerId2)
            => TimerId1.CompareTo(TimerId2) >= 0;

        #endregion

        #region IComparable<Timer_Id> Members

        /// <summary>
        /// Compares two timer identifications.
        /// </summary>
        /// <param name="Object">A timer identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Timer_Id timerId
                   ? CompareTo(timerId)
                   : throw new ArgumentException("The given object is not a timer identification!", nameof(Object));

        /// <summary>
        /// Compares two timer identifications.
        /// </summary>
        /// <param name="TimerId">A timer identification to compare with.</param>
        public Int32 CompareTo(Timer_Id TimerId)
            => String.Compare(Value, TimerId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<Timer_Id> Members

        /// <summary>
        /// Compares two timer identifications for equality.
        /// </summary>
        /// <param name="Object">A timer identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Timer_Id timerId && Equals(timerId);

        /// <summary>
        /// Compares two timer identifications for equality.
        /// </summary>
        /// <param name="TimerId">A timer identification to compare with.</param>
        public Boolean Equals(Timer_Id TimerId)
            => String.Equals(Value, TimerId.Value, StringComparison.Ordinal);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => Value?.GetHashCode(StringComparison.Ordinal) ?? 0;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => Value ?? "";

        #endregion

    }

}

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
    /// Extension methods for power sequence identifications.
    /// </summary>
    public static class PowerSequenceIdExtensions
    {

        /// <summary>
        /// Indicates whether this power sequence identification is null or empty.
        /// </summary>
        /// <param name="PowerSequenceId">A power sequence identification.</param>
        public static Boolean IsNullOrEmpty(this PowerSequence_Id? PowerSequenceId)
            => !PowerSequenceId.HasValue || PowerSequenceId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this power sequence identification is NOT null or empty.
        /// </summary>
        /// <param name="PowerSequenceId">A power sequence identification.</param>
        public static Boolean IsNotNullOrEmpty(this PowerSequence_Id? PowerSequenceId)
            => PowerSequenceId.HasValue && PowerSequenceId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The identification of a PPBC.PowerSequence. Must be unique in the scope of the PPBC.PowerSequenceContainer in which it is used.
    /// </summary>
    public readonly struct PowerSequence_Id : IId,
                                        IEquatable<PowerSequence_Id>,
                                        IComparable<PowerSequence_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the power sequence identification.
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
        /// The length of the power sequence identification.
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
        /// Create a new power sequence identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a power sequence identification.</param>
        private PowerSequence_Id(String Text)
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
        // PPBC.PowerSequence.schema.json
        //   "id": { "$ref": "../schemas/ID.schema.json",
        //           "description": "ID of the PPBC.PowerSequence. Must be unique in the scope of the PPBC.PowerSequenceContainer in which it is used." }

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) power sequence identification.
        /// </summary>
        public static PowerSequence_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a power sequence identification.
        /// </summary>
        /// <param name="Text">A text representation of a power sequence identification.</param>
        public static PowerSequence_Id Parse(String Text)
        {

            if (TryParse(Text, out var powerSequenceId))
                return powerSequenceId;

            throw new ArgumentException($"Invalid text representation of a power sequence identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a power sequence identification.
        /// </summary>
        /// <param name="Text">A text representation of a power sequence identification.</param>
        public static PowerSequence_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var powerSequenceId))
                return powerSequenceId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out PowerSequenceId)

        /// <summary>
        /// Try to parse the given text as a power sequence identification.
        /// </summary>
        /// <param name="Text">A text representation of a power sequence identification.</param>
        /// <param name="PowerSequenceId">The parsed power sequence identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out PowerSequence_Id    PowerSequenceId)
        {

            if (S2_Id.IsValid(Text))
            {
                PowerSequenceId = new PowerSequence_Id(Text);
                return true;
            }

            PowerSequenceId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power sequence identification.
        /// </summary>
        public PowerSequence_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power sequence identifications for equality.
        /// </summary>
        public static Boolean operator == (PowerSequence_Id PowerSequenceId1, PowerSequence_Id PowerSequenceId2)
            => PowerSequenceId1.Equals(PowerSequenceId2);

        /// <summary>
        /// Compares two power sequence identifications for inequality.
        /// </summary>
        public static Boolean operator != (PowerSequence_Id PowerSequenceId1, PowerSequence_Id PowerSequenceId2)
            => !PowerSequenceId1.Equals(PowerSequenceId2);

        /// <summary>
        /// Compares two power sequence identifications.
        /// </summary>
        public static Boolean operator <  (PowerSequence_Id PowerSequenceId1, PowerSequence_Id PowerSequenceId2)
            => PowerSequenceId1.CompareTo(PowerSequenceId2) < 0;

        /// <summary>
        /// Compares two power sequence identifications.
        /// </summary>
        public static Boolean operator <= (PowerSequence_Id PowerSequenceId1, PowerSequence_Id PowerSequenceId2)
            => PowerSequenceId1.CompareTo(PowerSequenceId2) <= 0;

        /// <summary>
        /// Compares two power sequence identifications.
        /// </summary>
        public static Boolean operator >  (PowerSequence_Id PowerSequenceId1, PowerSequence_Id PowerSequenceId2)
            => PowerSequenceId1.CompareTo(PowerSequenceId2) > 0;

        /// <summary>
        /// Compares two power sequence identifications.
        /// </summary>
        public static Boolean operator >= (PowerSequence_Id PowerSequenceId1, PowerSequence_Id PowerSequenceId2)
            => PowerSequenceId1.CompareTo(PowerSequenceId2) >= 0;

        #endregion

        #region IComparable<PowerSequence_Id> Members

        /// <summary>
        /// Compares two power sequence identifications.
        /// </summary>
        /// <param name="Object">A power sequence identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PowerSequence_Id powerSequenceId
                   ? CompareTo(powerSequenceId)
                   : throw new ArgumentException("The given object is not a power sequence identification!", nameof(Object));

        /// <summary>
        /// Compares two power sequence identifications.
        /// </summary>
        /// <param name="PowerSequenceId">A power sequence identification to compare with.</param>
        public Int32 CompareTo(PowerSequence_Id PowerSequenceId)
            => String.Compare(Value, PowerSequenceId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<PowerSequence_Id> Members

        /// <summary>
        /// Compares two power sequence identifications for equality.
        /// </summary>
        /// <param name="Object">A power sequence identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PowerSequence_Id powerSequenceId && Equals(powerSequenceId);

        /// <summary>
        /// Compares two power sequence identifications for equality.
        /// </summary>
        /// <param name="PowerSequenceId">A power sequence identification to compare with.</param>
        public Boolean Equals(PowerSequence_Id PowerSequenceId)
            => String.Equals(Value, PowerSequenceId.Value, StringComparison.Ordinal);

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

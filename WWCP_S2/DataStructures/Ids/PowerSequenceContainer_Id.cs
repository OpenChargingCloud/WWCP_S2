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
    /// Extension methods for power sequence container identifications.
    /// </summary>
    public static class PowerSequenceContainerIdExtensions
    {

        /// <summary>
        /// Indicates whether this power sequence container identification is null or empty.
        /// </summary>
        /// <param name="PowerSequenceContainerId">A power sequence container identification.</param>
        public static Boolean IsNullOrEmpty(this PowerSequenceContainer_Id? PowerSequenceContainerId)
            => !PowerSequenceContainerId.HasValue || PowerSequenceContainerId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this power sequence container identification is NOT null or empty.
        /// </summary>
        /// <param name="PowerSequenceContainerId">A power sequence container identification.</param>
        public static Boolean IsNotNullOrEmpty(this PowerSequenceContainer_Id? PowerSequenceContainerId)
            => PowerSequenceContainerId.HasValue && PowerSequenceContainerId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The identification of a PPBC.PowerSequenceContainer. Must be unique in the scope of the PPBC.PowerProfileDefinition in which it is used.
    /// </summary>
    public readonly struct PowerSequenceContainer_Id : IId,
                                        IEquatable<PowerSequenceContainer_Id>,
                                        IComparable<PowerSequenceContainer_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the power sequence container identification.
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
        /// The length of the power sequence container identification.
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
        /// Create a new power sequence container identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a power sequence container identification.</param>
        private PowerSequenceContainer_Id(String Text)
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
        // PPBC.PowerSequenceContainer.schema.json
        //   "id": { "$ref": "../schemas/ID.schema.json",
        //           "description": "ID of the PPBC.PowerSequenceContainer. Must be unique in the scope of the PPBC.PowerProfileDefinition in which it is used." }

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) power sequence container identification.
        /// </summary>
        public static PowerSequenceContainer_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a power sequence container identification.
        /// </summary>
        /// <param name="Text">A text representation of a power sequence container identification.</param>
        public static PowerSequenceContainer_Id Parse(String Text)
        {

            if (TryParse(Text, out var powerSequenceContainerId))
                return powerSequenceContainerId;

            throw new ArgumentException($"Invalid text representation of a power sequence container identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a power sequence container identification.
        /// </summary>
        /// <param name="Text">A text representation of a power sequence container identification.</param>
        public static PowerSequenceContainer_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var powerSequenceContainerId))
                return powerSequenceContainerId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out PowerSequenceContainerId)

        /// <summary>
        /// Try to parse the given text as a power sequence container identification.
        /// </summary>
        /// <param name="Text">A text representation of a power sequence container identification.</param>
        /// <param name="PowerSequenceContainerId">The parsed power sequence container identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out PowerSequenceContainer_Id    PowerSequenceContainerId)
        {

            if (S2_Id.IsValid(Text))
            {
                PowerSequenceContainerId = new PowerSequenceContainer_Id(Text);
                return true;
            }

            PowerSequenceContainerId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power sequence container identification.
        /// </summary>
        public PowerSequenceContainer_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power sequence container identifications for equality.
        /// </summary>
        public static Boolean operator == (PowerSequenceContainer_Id PowerSequenceContainerId1, PowerSequenceContainer_Id PowerSequenceContainerId2)
            => PowerSequenceContainerId1.Equals(PowerSequenceContainerId2);

        /// <summary>
        /// Compares two power sequence container identifications for inequality.
        /// </summary>
        public static Boolean operator != (PowerSequenceContainer_Id PowerSequenceContainerId1, PowerSequenceContainer_Id PowerSequenceContainerId2)
            => !PowerSequenceContainerId1.Equals(PowerSequenceContainerId2);

        /// <summary>
        /// Compares two power sequence container identifications.
        /// </summary>
        public static Boolean operator <  (PowerSequenceContainer_Id PowerSequenceContainerId1, PowerSequenceContainer_Id PowerSequenceContainerId2)
            => PowerSequenceContainerId1.CompareTo(PowerSequenceContainerId2) < 0;

        /// <summary>
        /// Compares two power sequence container identifications.
        /// </summary>
        public static Boolean operator <= (PowerSequenceContainer_Id PowerSequenceContainerId1, PowerSequenceContainer_Id PowerSequenceContainerId2)
            => PowerSequenceContainerId1.CompareTo(PowerSequenceContainerId2) <= 0;

        /// <summary>
        /// Compares two power sequence container identifications.
        /// </summary>
        public static Boolean operator >  (PowerSequenceContainer_Id PowerSequenceContainerId1, PowerSequenceContainer_Id PowerSequenceContainerId2)
            => PowerSequenceContainerId1.CompareTo(PowerSequenceContainerId2) > 0;

        /// <summary>
        /// Compares two power sequence container identifications.
        /// </summary>
        public static Boolean operator >= (PowerSequenceContainer_Id PowerSequenceContainerId1, PowerSequenceContainer_Id PowerSequenceContainerId2)
            => PowerSequenceContainerId1.CompareTo(PowerSequenceContainerId2) >= 0;

        #endregion

        #region IComparable<PowerSequenceContainer_Id> Members

        /// <summary>
        /// Compares two power sequence container identifications.
        /// </summary>
        /// <param name="Object">A power sequence container identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PowerSequenceContainer_Id powerSequenceContainerId
                   ? CompareTo(powerSequenceContainerId)
                   : throw new ArgumentException("The given object is not a power sequence container identification!", nameof(Object));

        /// <summary>
        /// Compares two power sequence container identifications.
        /// </summary>
        /// <param name="PowerSequenceContainerId">A power sequence container identification to compare with.</param>
        public Int32 CompareTo(PowerSequenceContainer_Id PowerSequenceContainerId)
            => String.Compare(Value, PowerSequenceContainerId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<PowerSequenceContainer_Id> Members

        /// <summary>
        /// Compares two power sequence container identifications for equality.
        /// </summary>
        /// <param name="Object">A power sequence container identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PowerSequenceContainer_Id powerSequenceContainerId && Equals(powerSequenceContainerId);

        /// <summary>
        /// Compares two power sequence container identifications for equality.
        /// </summary>
        /// <param name="PowerSequenceContainerId">A power sequence container identification to compare with.</param>
        public Boolean Equals(PowerSequenceContainer_Id PowerSequenceContainerId)
            => String.Equals(Value, PowerSequenceContainerId.Value, StringComparison.Ordinal);

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

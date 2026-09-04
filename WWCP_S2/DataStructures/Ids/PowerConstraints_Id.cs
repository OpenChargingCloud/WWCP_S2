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
    /// Extension methods for power constraints identifications.
    /// </summary>
    public static class PowerConstraintsIdExtensions
    {

        /// <summary>
        /// Indicates whether this power constraints identification is null or empty.
        /// </summary>
        /// <param name="PowerConstraintsId">A power constraints identification.</param>
        public static Boolean IsNullOrEmpty(this PowerConstraints_Id? PowerConstraintsId)
            => !PowerConstraintsId.HasValue || PowerConstraintsId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this power constraints identification is NOT null or empty.
        /// </summary>
        /// <param name="PowerConstraintsId">A power constraints identification.</param>
        public static Boolean IsNotNullOrEmpty(this PowerConstraints_Id? PowerConstraintsId)
            => PowerConstraintsId.HasValue && PowerConstraintsId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The identification of a PEBC.PowerConstraints message. Must be unique in the scope of the Resource Manager, for at least the duration of the session between Resource Manager and CEM.
    /// </summary>
    public readonly struct PowerConstraints_Id : IId,
                                        IEquatable<PowerConstraints_Id>,
                                        IComparable<PowerConstraints_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the power constraints identification.
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
        /// The length of the power constraints identification.
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
        /// Create a new power constraints identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a power constraints identification.</param>
        private PowerConstraints_Id(String Text)
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
        // PEBC.PowerConstraints.schema.json
        //   "id": { "$ref": "../schemas/ID.schema.json",
        //           "description": "Identifier of this PEBC.PowerConstraints. Must be unique in the scope of the Resource Manager, for at least the duration of the session between Resource Manager and CEM." }
        // PEBC.Instruction.schema.json
        //   "power_constraints_id": { "$ref": "../schemas/ID.schema.json", "description": "Identifier of the PEBC.PowerConstraints this PEBC.Instruction was based on." }

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) power constraints identification.
        /// </summary>
        public static PowerConstraints_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a power constraints identification.
        /// </summary>
        /// <param name="Text">A text representation of a power constraints identification.</param>
        public static PowerConstraints_Id Parse(String Text)
        {

            if (TryParse(Text, out var powerConstraintsId))
                return powerConstraintsId;

            throw new ArgumentException($"Invalid text representation of a power constraints identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a power constraints identification.
        /// </summary>
        /// <param name="Text">A text representation of a power constraints identification.</param>
        public static PowerConstraints_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var powerConstraintsId))
                return powerConstraintsId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out PowerConstraintsId)

        /// <summary>
        /// Try to parse the given text as a power constraints identification.
        /// </summary>
        /// <param name="Text">A text representation of a power constraints identification.</param>
        /// <param name="PowerConstraintsId">The parsed power constraints identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out PowerConstraints_Id    PowerConstraintsId)
        {

            if (S2_Id.IsValid(Text))
            {
                PowerConstraintsId = new PowerConstraints_Id(Text);
                return true;
            }

            PowerConstraintsId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power constraints identification.
        /// </summary>
        public PowerConstraints_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power constraints identifications for equality.
        /// </summary>
        public static Boolean operator == (PowerConstraints_Id PowerConstraintsId1, PowerConstraints_Id PowerConstraintsId2)
            => PowerConstraintsId1.Equals(PowerConstraintsId2);

        /// <summary>
        /// Compares two power constraints identifications for inequality.
        /// </summary>
        public static Boolean operator != (PowerConstraints_Id PowerConstraintsId1, PowerConstraints_Id PowerConstraintsId2)
            => !PowerConstraintsId1.Equals(PowerConstraintsId2);

        /// <summary>
        /// Compares two power constraints identifications.
        /// </summary>
        public static Boolean operator <  (PowerConstraints_Id PowerConstraintsId1, PowerConstraints_Id PowerConstraintsId2)
            => PowerConstraintsId1.CompareTo(PowerConstraintsId2) < 0;

        /// <summary>
        /// Compares two power constraints identifications.
        /// </summary>
        public static Boolean operator <= (PowerConstraints_Id PowerConstraintsId1, PowerConstraints_Id PowerConstraintsId2)
            => PowerConstraintsId1.CompareTo(PowerConstraintsId2) <= 0;

        /// <summary>
        /// Compares two power constraints identifications.
        /// </summary>
        public static Boolean operator >  (PowerConstraints_Id PowerConstraintsId1, PowerConstraints_Id PowerConstraintsId2)
            => PowerConstraintsId1.CompareTo(PowerConstraintsId2) > 0;

        /// <summary>
        /// Compares two power constraints identifications.
        /// </summary>
        public static Boolean operator >= (PowerConstraints_Id PowerConstraintsId1, PowerConstraints_Id PowerConstraintsId2)
            => PowerConstraintsId1.CompareTo(PowerConstraintsId2) >= 0;

        #endregion

        #region IComparable<PowerConstraints_Id> Members

        /// <summary>
        /// Compares two power constraints identifications.
        /// </summary>
        /// <param name="Object">A power constraints identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PowerConstraints_Id powerConstraintsId
                   ? CompareTo(powerConstraintsId)
                   : throw new ArgumentException("The given object is not a power constraints identification!", nameof(Object));

        /// <summary>
        /// Compares two power constraints identifications.
        /// </summary>
        /// <param name="PowerConstraintsId">A power constraints identification to compare with.</param>
        public Int32 CompareTo(PowerConstraints_Id PowerConstraintsId)
            => String.Compare(Value, PowerConstraintsId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<PowerConstraints_Id> Members

        /// <summary>
        /// Compares two power constraints identifications for equality.
        /// </summary>
        /// <param name="Object">A power constraints identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PowerConstraints_Id powerConstraintsId && Equals(powerConstraintsId);

        /// <summary>
        /// Compares two power constraints identifications for equality.
        /// </summary>
        /// <param name="PowerConstraintsId">A power constraints identification to compare with.</param>
        public Boolean Equals(PowerConstraints_Id PowerConstraintsId)
            => String.Equals(Value, PowerConstraintsId.Value, StringComparison.Ordinal);

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

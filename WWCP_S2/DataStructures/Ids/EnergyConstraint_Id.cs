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
    /// Extension methods for energy constraint identifications.
    /// </summary>
    public static class EnergyConstraintIdExtensions
    {

        /// <summary>
        /// Indicates whether this energy constraint identification is null or empty.
        /// </summary>
        /// <param name="EnergyConstraintId">A energy constraint identification.</param>
        public static Boolean IsNullOrEmpty(this EnergyConstraint_Id? EnergyConstraintId)
            => !EnergyConstraintId.HasValue || EnergyConstraintId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this energy constraint identification is NOT null or empty.
        /// </summary>
        /// <param name="EnergyConstraintId">A energy constraint identification.</param>
        public static Boolean IsNotNullOrEmpty(this EnergyConstraint_Id? EnergyConstraintId)
            => EnergyConstraintId.HasValue && EnergyConstraintId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The identification of a PEBC.EnergyConstraint message. Must be unique in the scope of the Resource Manager, for at least the duration of the session between Resource Manager and CEM.
    /// </summary>
    public readonly struct EnergyConstraint_Id : IId,
                                        IEquatable<EnergyConstraint_Id>,
                                        IComparable<EnergyConstraint_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the energy constraint identification.
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
        /// The length of the energy constraint identification.
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
        /// Create a new energy constraint identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a energy constraint identification.</param>
        private EnergyConstraint_Id(String Text)
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
        // PEBC.EnergyConstraint.schema.json
        //   "id": { "$ref": "../schemas/ID.schema.json",
        //           "description": "Identifier of this PEBC.EnergyConstraint. Must be unique in the scope of the Resource Manager, for at least the duration of the session between Resource Manager and CEM." }

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) energy constraint identification.
        /// </summary>
        public static EnergyConstraint_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a energy constraint identification.
        /// </summary>
        /// <param name="Text">A text representation of a energy constraint identification.</param>
        public static EnergyConstraint_Id Parse(String Text)
        {

            if (TryParse(Text, out var energyConstraintId))
                return energyConstraintId;

            throw new ArgumentException($"Invalid text representation of a energy constraint identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a energy constraint identification.
        /// </summary>
        /// <param name="Text">A text representation of a energy constraint identification.</param>
        public static EnergyConstraint_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var energyConstraintId))
                return energyConstraintId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out EnergyConstraintId)

        /// <summary>
        /// Try to parse the given text as a energy constraint identification.
        /// </summary>
        /// <param name="Text">A text representation of a energy constraint identification.</param>
        /// <param name="EnergyConstraintId">The parsed energy constraint identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out EnergyConstraint_Id    EnergyConstraintId)
        {

            if (S2_Id.IsValid(Text))
            {
                EnergyConstraintId = new EnergyConstraint_Id(Text);
                return true;
            }

            EnergyConstraintId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy constraint identification.
        /// </summary>
        public EnergyConstraint_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two energy constraint identifications for equality.
        /// </summary>
        public static Boolean operator == (EnergyConstraint_Id EnergyConstraintId1, EnergyConstraint_Id EnergyConstraintId2)
            => EnergyConstraintId1.Equals(EnergyConstraintId2);

        /// <summary>
        /// Compares two energy constraint identifications for inequality.
        /// </summary>
        public static Boolean operator != (EnergyConstraint_Id EnergyConstraintId1, EnergyConstraint_Id EnergyConstraintId2)
            => !EnergyConstraintId1.Equals(EnergyConstraintId2);

        /// <summary>
        /// Compares two energy constraint identifications.
        /// </summary>
        public static Boolean operator <  (EnergyConstraint_Id EnergyConstraintId1, EnergyConstraint_Id EnergyConstraintId2)
            => EnergyConstraintId1.CompareTo(EnergyConstraintId2) < 0;

        /// <summary>
        /// Compares two energy constraint identifications.
        /// </summary>
        public static Boolean operator <= (EnergyConstraint_Id EnergyConstraintId1, EnergyConstraint_Id EnergyConstraintId2)
            => EnergyConstraintId1.CompareTo(EnergyConstraintId2) <= 0;

        /// <summary>
        /// Compares two energy constraint identifications.
        /// </summary>
        public static Boolean operator >  (EnergyConstraint_Id EnergyConstraintId1, EnergyConstraint_Id EnergyConstraintId2)
            => EnergyConstraintId1.CompareTo(EnergyConstraintId2) > 0;

        /// <summary>
        /// Compares two energy constraint identifications.
        /// </summary>
        public static Boolean operator >= (EnergyConstraint_Id EnergyConstraintId1, EnergyConstraint_Id EnergyConstraintId2)
            => EnergyConstraintId1.CompareTo(EnergyConstraintId2) >= 0;

        #endregion

        #region IComparable<EnergyConstraint_Id> Members

        /// <summary>
        /// Compares two energy constraint identifications.
        /// </summary>
        /// <param name="Object">A energy constraint identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyConstraint_Id energyConstraintId
                   ? CompareTo(energyConstraintId)
                   : throw new ArgumentException("The given object is not a energy constraint identification!", nameof(Object));

        /// <summary>
        /// Compares two energy constraint identifications.
        /// </summary>
        /// <param name="EnergyConstraintId">A energy constraint identification to compare with.</param>
        public Int32 CompareTo(EnergyConstraint_Id EnergyConstraintId)
            => String.Compare(Value, EnergyConstraintId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<EnergyConstraint_Id> Members

        /// <summary>
        /// Compares two energy constraint identifications for equality.
        /// </summary>
        /// <param name="Object">A energy constraint identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is EnergyConstraint_Id energyConstraintId && Equals(energyConstraintId);

        /// <summary>
        /// Compares two energy constraint identifications for equality.
        /// </summary>
        /// <param name="EnergyConstraintId">A energy constraint identification to compare with.</param>
        public Boolean Equals(EnergyConstraint_Id EnergyConstraintId)
            => String.Equals(Value, EnergyConstraintId.Value, StringComparison.Ordinal);

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

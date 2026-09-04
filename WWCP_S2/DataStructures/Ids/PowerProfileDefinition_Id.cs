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
    /// Extension methods for power profile definition identifications.
    /// </summary>
    public static class PowerProfileDefinitionIdExtensions
    {

        /// <summary>
        /// Indicates whether this power profile definition identification is null or empty.
        /// </summary>
        /// <param name="PowerProfileDefinitionId">A power profile definition identification.</param>
        public static Boolean IsNullOrEmpty(this PowerProfileDefinition_Id? PowerProfileDefinitionId)
            => !PowerProfileDefinitionId.HasValue || PowerProfileDefinitionId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this power profile definition identification is NOT null or empty.
        /// </summary>
        /// <param name="PowerProfileDefinitionId">A power profile definition identification.</param>
        public static Boolean IsNotNullOrEmpty(this PowerProfileDefinition_Id? PowerProfileDefinitionId)
            => PowerProfileDefinitionId.HasValue && PowerProfileDefinitionId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The identification of a PPBC.PowerProfileDefinition. Must be unique in the scope of the Resource Manager, for at least the duration of the session between Resource Manager and CEM.
    /// </summary>
    public readonly struct PowerProfileDefinition_Id : IId,
                                        IEquatable<PowerProfileDefinition_Id>,
                                        IComparable<PowerProfileDefinition_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the power profile definition identification.
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
        /// The length of the power profile definition identification.
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
        /// Create a new power profile definition identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a power profile definition identification.</param>
        private PowerProfileDefinition_Id(String Text)
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
        // PPBC.PowerProfileDefinition.schema.json
        //   "id": { "$ref": "../schemas/ID.schema.json",
        //           "description": "Identifier of this PPBC.PowerProfileDefinition. Must be unique in the scope of the Resource Manager, for at least the duration of the session between Resource Manager and CEM." }

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) power profile definition identification.
        /// </summary>
        public static PowerProfileDefinition_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a power profile definition identification.
        /// </summary>
        /// <param name="Text">A text representation of a power profile definition identification.</param>
        public static PowerProfileDefinition_Id Parse(String Text)
        {

            if (TryParse(Text, out var powerProfileDefinitionId))
                return powerProfileDefinitionId;

            throw new ArgumentException($"Invalid text representation of a power profile definition identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a power profile definition identification.
        /// </summary>
        /// <param name="Text">A text representation of a power profile definition identification.</param>
        public static PowerProfileDefinition_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var powerProfileDefinitionId))
                return powerProfileDefinitionId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out PowerProfileDefinitionId)

        /// <summary>
        /// Try to parse the given text as a power profile definition identification.
        /// </summary>
        /// <param name="Text">A text representation of a power profile definition identification.</param>
        /// <param name="PowerProfileDefinitionId">The parsed power profile definition identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out PowerProfileDefinition_Id    PowerProfileDefinitionId)
        {

            if (S2_Id.IsValid(Text))
            {
                PowerProfileDefinitionId = new PowerProfileDefinition_Id(Text);
                return true;
            }

            PowerProfileDefinitionId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power profile definition identification.
        /// </summary>
        public PowerProfileDefinition_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power profile definition identifications for equality.
        /// </summary>
        public static Boolean operator == (PowerProfileDefinition_Id PowerProfileDefinitionId1, PowerProfileDefinition_Id PowerProfileDefinitionId2)
            => PowerProfileDefinitionId1.Equals(PowerProfileDefinitionId2);

        /// <summary>
        /// Compares two power profile definition identifications for inequality.
        /// </summary>
        public static Boolean operator != (PowerProfileDefinition_Id PowerProfileDefinitionId1, PowerProfileDefinition_Id PowerProfileDefinitionId2)
            => !PowerProfileDefinitionId1.Equals(PowerProfileDefinitionId2);

        /// <summary>
        /// Compares two power profile definition identifications.
        /// </summary>
        public static Boolean operator <  (PowerProfileDefinition_Id PowerProfileDefinitionId1, PowerProfileDefinition_Id PowerProfileDefinitionId2)
            => PowerProfileDefinitionId1.CompareTo(PowerProfileDefinitionId2) < 0;

        /// <summary>
        /// Compares two power profile definition identifications.
        /// </summary>
        public static Boolean operator <= (PowerProfileDefinition_Id PowerProfileDefinitionId1, PowerProfileDefinition_Id PowerProfileDefinitionId2)
            => PowerProfileDefinitionId1.CompareTo(PowerProfileDefinitionId2) <= 0;

        /// <summary>
        /// Compares two power profile definition identifications.
        /// </summary>
        public static Boolean operator >  (PowerProfileDefinition_Id PowerProfileDefinitionId1, PowerProfileDefinition_Id PowerProfileDefinitionId2)
            => PowerProfileDefinitionId1.CompareTo(PowerProfileDefinitionId2) > 0;

        /// <summary>
        /// Compares two power profile definition identifications.
        /// </summary>
        public static Boolean operator >= (PowerProfileDefinition_Id PowerProfileDefinitionId1, PowerProfileDefinition_Id PowerProfileDefinitionId2)
            => PowerProfileDefinitionId1.CompareTo(PowerProfileDefinitionId2) >= 0;

        #endregion

        #region IComparable<PowerProfileDefinition_Id> Members

        /// <summary>
        /// Compares two power profile definition identifications.
        /// </summary>
        /// <param name="Object">A power profile definition identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PowerProfileDefinition_Id powerProfileDefinitionId
                   ? CompareTo(powerProfileDefinitionId)
                   : throw new ArgumentException("The given object is not a power profile definition identification!", nameof(Object));

        /// <summary>
        /// Compares two power profile definition identifications.
        /// </summary>
        /// <param name="PowerProfileDefinitionId">A power profile definition identification to compare with.</param>
        public Int32 CompareTo(PowerProfileDefinition_Id PowerProfileDefinitionId)
            => String.Compare(Value, PowerProfileDefinitionId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<PowerProfileDefinition_Id> Members

        /// <summary>
        /// Compares two power profile definition identifications for equality.
        /// </summary>
        /// <param name="Object">A power profile definition identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PowerProfileDefinition_Id powerProfileDefinitionId && Equals(powerProfileDefinitionId);

        /// <summary>
        /// Compares two power profile definition identifications for equality.
        /// </summary>
        /// <param name="PowerProfileDefinitionId">A power profile definition identification to compare with.</param>
        public Boolean Equals(PowerProfileDefinition_Id PowerProfileDefinitionId)
            => String.Equals(Value, PowerProfileDefinitionId.Value, StringComparison.Ordinal);

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

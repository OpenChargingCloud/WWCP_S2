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
    /// Extension methods for power envelope identifications.
    /// </summary>
    public static class PowerEnvelopeIdExtensions
    {

        /// <summary>
        /// Indicates whether this power envelope identification is null or empty.
        /// </summary>
        /// <param name="PowerEnvelopeId">A power envelope identification.</param>
        public static Boolean IsNullOrEmpty(this PowerEnvelope_Id? PowerEnvelopeId)
            => !PowerEnvelopeId.HasValue || PowerEnvelopeId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this power envelope identification is NOT null or empty.
        /// </summary>
        /// <param name="PowerEnvelopeId">A power envelope identification.</param>
        public static Boolean IsNotNullOrEmpty(this PowerEnvelope_Id? PowerEnvelopeId)
            => PowerEnvelopeId.HasValue && PowerEnvelopeId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The identification of a PEBC.PowerEnvelope. Must be unique in the scope of the Resource Manager, for at least the duration of the session between Resource Manager and CEM.
    /// </summary>
    public readonly struct PowerEnvelope_Id : IId,
                                        IEquatable<PowerEnvelope_Id>,
                                        IComparable<PowerEnvelope_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the power envelope identification.
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
        /// The length of the power envelope identification.
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
        /// Create a new power envelope identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a power envelope identification.</param>
        private PowerEnvelope_Id(String Text)
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
        // PEBC.PowerEnvelope.schema.json
        //   "id": { "$ref": "../schemas/ID.schema.json",
        //           "description": "Identifier of this PEBC.PowerEnvelope. Must be unique in the scope of the Resource Manager, for at least the duration of the session between Resource Manager and CEM." }

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) power envelope identification.
        /// </summary>
        public static PowerEnvelope_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a power envelope identification.
        /// </summary>
        /// <param name="Text">A text representation of a power envelope identification.</param>
        public static PowerEnvelope_Id Parse(String Text)
        {

            if (TryParse(Text, out var powerEnvelopeId))
                return powerEnvelopeId;

            throw new ArgumentException($"Invalid text representation of a power envelope identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a power envelope identification.
        /// </summary>
        /// <param name="Text">A text representation of a power envelope identification.</param>
        public static PowerEnvelope_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var powerEnvelopeId))
                return powerEnvelopeId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out PowerEnvelopeId)

        /// <summary>
        /// Try to parse the given text as a power envelope identification.
        /// </summary>
        /// <param name="Text">A text representation of a power envelope identification.</param>
        /// <param name="PowerEnvelopeId">The parsed power envelope identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out PowerEnvelope_Id    PowerEnvelopeId)
        {

            if (S2_Id.IsValid(Text))
            {
                PowerEnvelopeId = new PowerEnvelope_Id(Text);
                return true;
            }

            PowerEnvelopeId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power envelope identification.
        /// </summary>
        public PowerEnvelope_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power envelope identifications for equality.
        /// </summary>
        public static Boolean operator == (PowerEnvelope_Id PowerEnvelopeId1, PowerEnvelope_Id PowerEnvelopeId2)
            => PowerEnvelopeId1.Equals(PowerEnvelopeId2);

        /// <summary>
        /// Compares two power envelope identifications for inequality.
        /// </summary>
        public static Boolean operator != (PowerEnvelope_Id PowerEnvelopeId1, PowerEnvelope_Id PowerEnvelopeId2)
            => !PowerEnvelopeId1.Equals(PowerEnvelopeId2);

        /// <summary>
        /// Compares two power envelope identifications.
        /// </summary>
        public static Boolean operator <  (PowerEnvelope_Id PowerEnvelopeId1, PowerEnvelope_Id PowerEnvelopeId2)
            => PowerEnvelopeId1.CompareTo(PowerEnvelopeId2) < 0;

        /// <summary>
        /// Compares two power envelope identifications.
        /// </summary>
        public static Boolean operator <= (PowerEnvelope_Id PowerEnvelopeId1, PowerEnvelope_Id PowerEnvelopeId2)
            => PowerEnvelopeId1.CompareTo(PowerEnvelopeId2) <= 0;

        /// <summary>
        /// Compares two power envelope identifications.
        /// </summary>
        public static Boolean operator >  (PowerEnvelope_Id PowerEnvelopeId1, PowerEnvelope_Id PowerEnvelopeId2)
            => PowerEnvelopeId1.CompareTo(PowerEnvelopeId2) > 0;

        /// <summary>
        /// Compares two power envelope identifications.
        /// </summary>
        public static Boolean operator >= (PowerEnvelope_Id PowerEnvelopeId1, PowerEnvelope_Id PowerEnvelopeId2)
            => PowerEnvelopeId1.CompareTo(PowerEnvelopeId2) >= 0;

        #endregion

        #region IComparable<PowerEnvelope_Id> Members

        /// <summary>
        /// Compares two power envelope identifications.
        /// </summary>
        /// <param name="Object">A power envelope identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PowerEnvelope_Id powerEnvelopeId
                   ? CompareTo(powerEnvelopeId)
                   : throw new ArgumentException("The given object is not a power envelope identification!", nameof(Object));

        /// <summary>
        /// Compares two power envelope identifications.
        /// </summary>
        /// <param name="PowerEnvelopeId">A power envelope identification to compare with.</param>
        public Int32 CompareTo(PowerEnvelope_Id PowerEnvelopeId)
            => String.Compare(Value, PowerEnvelopeId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<PowerEnvelope_Id> Members

        /// <summary>
        /// Compares two power envelope identifications for equality.
        /// </summary>
        /// <param name="Object">A power envelope identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PowerEnvelope_Id powerEnvelopeId && Equals(powerEnvelopeId);

        /// <summary>
        /// Compares two power envelope identifications for equality.
        /// </summary>
        /// <param name="PowerEnvelopeId">A power envelope identification to compare with.</param>
        public Boolean Equals(PowerEnvelope_Id PowerEnvelopeId)
            => String.Equals(Value, PowerEnvelopeId.Value, StringComparison.Ordinal);

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

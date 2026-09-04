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
    /// Extension methods for power envelope consequence types.
    /// </summary>
    public static class PEBC_PowerEnvelopeConsequenceTypeExtensions
    {

        /// <summary>
        /// Indicates whether this power envelope consequence type is null or empty.
        /// </summary>
        /// <param name="PEBC_PowerEnvelopeConsequenceType">A power envelope consequence type.</param>
        public static Boolean IsNullOrEmpty(this PEBC_PowerEnvelopeConsequenceType? PEBC_PowerEnvelopeConsequenceType)
            => !PEBC_PowerEnvelopeConsequenceType.HasValue || PEBC_PowerEnvelopeConsequenceType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this power envelope consequence type is NOT null or empty.
        /// </summary>
        /// <param name="PEBC_PowerEnvelopeConsequenceType">A power envelope consequence type.</param>
        public static Boolean IsNotNullOrEmpty(this PEBC_PowerEnvelopeConsequenceType? PEBC_PowerEnvelopeConsequenceType)
            => PEBC_PowerEnvelopeConsequenceType.HasValue && PEBC_PowerEnvelopeConsequenceType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The consequence of limiting load or generation with a PEBC power envelope: the energy vanishes or is deferred.
    /// </summary>
    public readonly struct PEBC_PowerEnvelopeConsequenceType : IS2PredefinedString,
                                         IId,
                                         IEquatable<PEBC_PowerEnvelopeConsequenceType>,
                                         IComparable<PEBC_PowerEnvelopeConsequenceType>
    {

        #region Data

        private readonly static Dictionary<String, PEBC_PowerEnvelopeConsequenceType>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this power envelope consequence type is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this power envelope consequence type is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the power envelope consequence type.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All power envelope consequence types defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<PEBC_PowerEnvelopeConsequenceType> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power envelope consequence type based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a power envelope consequence type.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private PEBC_PowerEnvelopeConsequenceType(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // PEBC.PowerEnvelopeConsequenceType.schema.json
        //   "title": "PEBC_PowerEnvelopeConsequenceType",
        //   "type":  "string",
        //   "enum":  ["VANISH", "DEFER"],
        //   "description":
        //     VANISH: Indicating that the limited load or generated will be lost and not reappear in the future (see Clause 7.6.2)
        //     DEFER: Indicating that the limited load or generation will be postponed to a later moment (see Clause 7.6.2)

        #endregion

        #region (private static) Register(Text)

        private static PEBC_PowerEnvelopeConsequenceType Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new PEBC_PowerEnvelopeConsequenceType(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a power envelope consequence type.
        /// </summary>
        /// <param name="Text">A text representation of a power envelope consequence type.</param>
        public static PEBC_PowerEnvelopeConsequenceType Parse(String Text)
        {

            if (TryParse(Text, out var pEBC_PowerEnvelopeConsequenceType))
                return pEBC_PowerEnvelopeConsequenceType;

            throw new ArgumentException($"Invalid text representation of a power envelope consequence type: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a power envelope consequence type.
        /// </summary>
        /// <param name="Text">A text representation of a power envelope consequence type.</param>
        public static PEBC_PowerEnvelopeConsequenceType? TryParse(String Text)
        {

            if (TryParse(Text, out var pEBC_PowerEnvelopeConsequenceType))
                return pEBC_PowerEnvelopeConsequenceType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out PEBC_PowerEnvelopeConsequenceType)

        /// <summary>
        /// Try to parse the given text as a power envelope consequence type. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a power envelope consequence type.</param>
        /// <param name="PEBC_PowerEnvelopeConsequenceType">The parsed power envelope consequence type.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out PEBC_PowerEnvelopeConsequenceType    PEBC_PowerEnvelopeConsequenceType)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out PEBC_PowerEnvelopeConsequenceType))
                    PEBC_PowerEnvelopeConsequenceType = new PEBC_PowerEnvelopeConsequenceType(Text, false);

                return true;

            }

            PEBC_PowerEnvelopeConsequenceType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power envelope consequence type.
        /// </summary>
        public PEBC_PowerEnvelopeConsequenceType Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// VANISH: Indicating that the limited load or generated will be lost and not reappear in the future (see Clause 7.6.2)
        /// </summary>
        public static PEBC_PowerEnvelopeConsequenceType  Vanish    { get; }
            = Register("VANISH");

        /// <summary>
        /// DEFER: Indicating that the limited load or generation will be postponed to a later moment (see Clause 7.6.2)
        /// </summary>
        public static PEBC_PowerEnvelopeConsequenceType  Defer    { get; }
            = Register("DEFER");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power envelope consequence types for equality.
        /// </summary>
        public static Boolean operator == (PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType1, PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType2)
            => PEBC_PowerEnvelopeConsequenceType1.Equals(PEBC_PowerEnvelopeConsequenceType2);

        /// <summary>
        /// Compares two power envelope consequence types for inequality.
        /// </summary>
        public static Boolean operator != (PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType1, PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType2)
            => !PEBC_PowerEnvelopeConsequenceType1.Equals(PEBC_PowerEnvelopeConsequenceType2);

        /// <summary>
        /// Compares two power envelope consequence types.
        /// </summary>
        public static Boolean operator <  (PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType1, PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType2)
            => PEBC_PowerEnvelopeConsequenceType1.CompareTo(PEBC_PowerEnvelopeConsequenceType2) < 0;

        /// <summary>
        /// Compares two power envelope consequence types.
        /// </summary>
        public static Boolean operator <= (PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType1, PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType2)
            => PEBC_PowerEnvelopeConsequenceType1.CompareTo(PEBC_PowerEnvelopeConsequenceType2) <= 0;

        /// <summary>
        /// Compares two power envelope consequence types.
        /// </summary>
        public static Boolean operator >  (PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType1, PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType2)
            => PEBC_PowerEnvelopeConsequenceType1.CompareTo(PEBC_PowerEnvelopeConsequenceType2) > 0;

        /// <summary>
        /// Compares two power envelope consequence types.
        /// </summary>
        public static Boolean operator >= (PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType1, PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType2)
            => PEBC_PowerEnvelopeConsequenceType1.CompareTo(PEBC_PowerEnvelopeConsequenceType2) >= 0;

        #endregion

        #region IComparable<PEBC_PowerEnvelopeConsequenceType> Members

        /// <summary>
        /// Compares two power envelope consequence types.
        /// </summary>
        /// <param name="Object">A power envelope consequence type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PEBC_PowerEnvelopeConsequenceType pEBC_PowerEnvelopeConsequenceType
                   ? CompareTo(pEBC_PowerEnvelopeConsequenceType)
                   : throw new ArgumentException("The given object is not a power envelope consequence type!", nameof(Object));

        /// <summary>
        /// Compares two power envelope consequence types.
        /// </summary>
        /// <param name="PEBC_PowerEnvelopeConsequenceType">A power envelope consequence type to compare with.</param>
        public Int32 CompareTo(PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType)
            => String.Compare(InternalId, PEBC_PowerEnvelopeConsequenceType.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<PEBC_PowerEnvelopeConsequenceType> Members

        /// <summary>
        /// Compares two power envelope consequence types for equality.
        /// </summary>
        /// <param name="Object">A power envelope consequence type to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PEBC_PowerEnvelopeConsequenceType pEBC_PowerEnvelopeConsequenceType && Equals(pEBC_PowerEnvelopeConsequenceType);

        /// <summary>
        /// Compares two power envelope consequence types for equality.
        /// </summary>
        /// <param name="PEBC_PowerEnvelopeConsequenceType">A power envelope consequence type to compare with.</param>
        public Boolean Equals(PEBC_PowerEnvelopeConsequenceType PEBC_PowerEnvelopeConsequenceType)
            => String.Equals(InternalId, PEBC_PowerEnvelopeConsequenceType.InternalId, StringComparison.Ordinal);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => InternalId?.GetHashCode(StringComparison.Ordinal) ?? 0;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => InternalId ?? "";

        #endregion

    }

}

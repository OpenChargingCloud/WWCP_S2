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
    /// Extension methods for power envelope limit types.
    /// </summary>
    public static class PEBC_PowerEnvelopeLimitTypeExtensions
    {

        /// <summary>
        /// Indicates whether this power envelope limit type is null or empty.
        /// </summary>
        /// <param name="PEBC_PowerEnvelopeLimitType">A power envelope limit type.</param>
        public static Boolean IsNullOrEmpty(this PEBC_PowerEnvelopeLimitType? PEBC_PowerEnvelopeLimitType)
            => !PEBC_PowerEnvelopeLimitType.HasValue || PEBC_PowerEnvelopeLimitType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this power envelope limit type is NOT null or empty.
        /// </summary>
        /// <param name="PEBC_PowerEnvelopeLimitType">A power envelope limit type.</param>
        public static Boolean IsNotNullOrEmpty(this PEBC_PowerEnvelopeLimitType? PEBC_PowerEnvelopeLimitType)
            => PEBC_PowerEnvelopeLimitType.HasValue && PEBC_PowerEnvelopeLimitType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// Whether a PEBC allowed limit range describes the upper or the lower limit of a power envelope.
    /// </summary>
    public readonly struct PEBC_PowerEnvelopeLimitType : IS2PredefinedString,
                                         IId,
                                         IEquatable<PEBC_PowerEnvelopeLimitType>,
                                         IComparable<PEBC_PowerEnvelopeLimitType>
    {

        #region Data

        private readonly static Dictionary<String, PEBC_PowerEnvelopeLimitType>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this power envelope limit type is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this power envelope limit type is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the power envelope limit type.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All power envelope limit types defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<PEBC_PowerEnvelopeLimitType> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power envelope limit type based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a power envelope limit type.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private PEBC_PowerEnvelopeLimitType(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // PEBC.PowerEnvelopeLimitType.schema.json
        //   "title": "PEBC_PowerEnvelopeLimitType",
        //   "type":  "string",
        //   "enum":  ["UPPER_LIMIT", "LOWER_LIMIT"],
        //   "description":
        //     UPPER_LIMIT: Indicating the upper limit of a PEBC.PowerEnvelope (see Clause 7.6.2)
        //     LOWER_LIMIT: Indicating the lower limit of a PEBC.PowerEnvelope (see Clause 7.6.2)

        #endregion

        #region (private static) Register(Text)

        private static PEBC_PowerEnvelopeLimitType Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new PEBC_PowerEnvelopeLimitType(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a power envelope limit type.
        /// </summary>
        /// <param name="Text">A text representation of a power envelope limit type.</param>
        public static PEBC_PowerEnvelopeLimitType Parse(String Text)
        {

            if (TryParse(Text, out var pEBC_PowerEnvelopeLimitType))
                return pEBC_PowerEnvelopeLimitType;

            throw new ArgumentException($"Invalid text representation of a power envelope limit type: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a power envelope limit type.
        /// </summary>
        /// <param name="Text">A text representation of a power envelope limit type.</param>
        public static PEBC_PowerEnvelopeLimitType? TryParse(String Text)
        {

            if (TryParse(Text, out var pEBC_PowerEnvelopeLimitType))
                return pEBC_PowerEnvelopeLimitType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out PEBC_PowerEnvelopeLimitType)

        /// <summary>
        /// Try to parse the given text as a power envelope limit type. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a power envelope limit type.</param>
        /// <param name="PEBC_PowerEnvelopeLimitType">The parsed power envelope limit type.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out PEBC_PowerEnvelopeLimitType    PEBC_PowerEnvelopeLimitType)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out PEBC_PowerEnvelopeLimitType))
                    PEBC_PowerEnvelopeLimitType = new PEBC_PowerEnvelopeLimitType(Text, false);

                return true;

            }

            PEBC_PowerEnvelopeLimitType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power envelope limit type.
        /// </summary>
        public PEBC_PowerEnvelopeLimitType Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// UPPER_LIMIT: Indicating the upper limit of a PEBC.PowerEnvelope (see Clause 7.6.2)
        /// </summary>
        public static PEBC_PowerEnvelopeLimitType  UpperLimit    { get; }
            = Register("UPPER_LIMIT");

        /// <summary>
        /// LOWER_LIMIT: Indicating the lower limit of a PEBC.PowerEnvelope (see Clause 7.6.2)
        /// </summary>
        public static PEBC_PowerEnvelopeLimitType  LowerLimit    { get; }
            = Register("LOWER_LIMIT");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power envelope limit types for equality.
        /// </summary>
        public static Boolean operator == (PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType1, PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType2)
            => PEBC_PowerEnvelopeLimitType1.Equals(PEBC_PowerEnvelopeLimitType2);

        /// <summary>
        /// Compares two power envelope limit types for inequality.
        /// </summary>
        public static Boolean operator != (PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType1, PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType2)
            => !PEBC_PowerEnvelopeLimitType1.Equals(PEBC_PowerEnvelopeLimitType2);

        /// <summary>
        /// Compares two power envelope limit types.
        /// </summary>
        public static Boolean operator <  (PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType1, PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType2)
            => PEBC_PowerEnvelopeLimitType1.CompareTo(PEBC_PowerEnvelopeLimitType2) < 0;

        /// <summary>
        /// Compares two power envelope limit types.
        /// </summary>
        public static Boolean operator <= (PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType1, PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType2)
            => PEBC_PowerEnvelopeLimitType1.CompareTo(PEBC_PowerEnvelopeLimitType2) <= 0;

        /// <summary>
        /// Compares two power envelope limit types.
        /// </summary>
        public static Boolean operator >  (PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType1, PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType2)
            => PEBC_PowerEnvelopeLimitType1.CompareTo(PEBC_PowerEnvelopeLimitType2) > 0;

        /// <summary>
        /// Compares two power envelope limit types.
        /// </summary>
        public static Boolean operator >= (PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType1, PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType2)
            => PEBC_PowerEnvelopeLimitType1.CompareTo(PEBC_PowerEnvelopeLimitType2) >= 0;

        #endregion

        #region IComparable<PEBC_PowerEnvelopeLimitType> Members

        /// <summary>
        /// Compares two power envelope limit types.
        /// </summary>
        /// <param name="Object">A power envelope limit type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PEBC_PowerEnvelopeLimitType pEBC_PowerEnvelopeLimitType
                   ? CompareTo(pEBC_PowerEnvelopeLimitType)
                   : throw new ArgumentException("The given object is not a power envelope limit type!", nameof(Object));

        /// <summary>
        /// Compares two power envelope limit types.
        /// </summary>
        /// <param name="PEBC_PowerEnvelopeLimitType">A power envelope limit type to compare with.</param>
        public Int32 CompareTo(PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType)
            => String.Compare(InternalId, PEBC_PowerEnvelopeLimitType.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<PEBC_PowerEnvelopeLimitType> Members

        /// <summary>
        /// Compares two power envelope limit types for equality.
        /// </summary>
        /// <param name="Object">A power envelope limit type to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PEBC_PowerEnvelopeLimitType pEBC_PowerEnvelopeLimitType && Equals(pEBC_PowerEnvelopeLimitType);

        /// <summary>
        /// Compares two power envelope limit types for equality.
        /// </summary>
        /// <param name="PEBC_PowerEnvelopeLimitType">A power envelope limit type to compare with.</param>
        public Boolean Equals(PEBC_PowerEnvelopeLimitType PEBC_PowerEnvelopeLimitType)
            => String.Equals(InternalId, PEBC_PowerEnvelopeLimitType.InternalId, StringComparison.Ordinal);

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

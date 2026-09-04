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

using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// An ISO 3166-1 alpha-2 country code as used by the WAN pairing endpoint registry
    /// (s2-connect-wan-endpoint-registry.yml, CountryCode: pattern "^[A-Z]{2}$").
    /// </summary>
    public readonly partial struct CountryCode : IId,
                                                 IEquatable<CountryCode>,
                                                 IComparable<CountryCode>
    {

        #region Data

        [GeneratedRegex("^[A-Z]{2}$", RegexOptions.CultureInvariant)]
        private static partial Regex CodeRegExpr();

        #endregion

        #region Properties

        /// <summary>
        /// The two upper-case letters.
        /// </summary>
        public String   Value               { get; }

        /// <summary>
        /// Indicates whether this country code is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => Value.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this country code is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => Value.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the country code (2).
        /// </summary>
        public UInt64   Length
            => (UInt64) (Value?.Length ?? 0);

        #endregion

        #region Constructor(s)

        private CountryCode(String Text)
        {
            this.Value = Text;
        }

        #endregion


        #region (static) IsValid(Text)

        /// <summary>
        /// Whether the given text is a valid ISO 3166-1 alpha-2 country code (two upper-case letters).
        /// </summary>
        /// <param name="Text">A text.</param>
        public static Boolean IsValid(String? Text)
            => Text is not null && CodeRegExpr().IsMatch(Text);

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a country code.
        /// </summary>
        /// <param name="Text">A text representation of a country code.</param>
        public static CountryCode Parse(String Text)
        {

            if (TryParse(Text, out var code))
                return code;

            throw new ArgumentException($"Invalid text representation of a country code: '{Text}'!", nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text, out CountryCode)

        /// <summary>
        /// Try to parse the given text as a country code.
        /// </summary>
        /// <param name="Text">A text representation of a country code.</param>
        /// <param name="CountryCode">The parsed country code.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out CountryCode    CountryCode)
        {

            if (IsValid(Text))
            {
                CountryCode = new CountryCode(Text);
                return true;
            }

            CountryCode = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this country code.
        /// </summary>
        public CountryCode Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two country codes for equality.
        /// </summary>
        public static Boolean operator == (CountryCode CountryCode1, CountryCode CountryCode2)
            => CountryCode1.Equals(CountryCode2);

        /// <summary>
        /// Compares two country codes for inequality.
        /// </summary>
        public static Boolean operator != (CountryCode CountryCode1, CountryCode CountryCode2)
            => !CountryCode1.Equals(CountryCode2);

        /// <summary>
        /// Compares two country codes.
        /// </summary>
        public static Boolean operator <  (CountryCode CountryCode1, CountryCode CountryCode2)
            => CountryCode1.CompareTo(CountryCode2) < 0;

        /// <summary>
        /// Compares two country codes.
        /// </summary>
        public static Boolean operator <= (CountryCode CountryCode1, CountryCode CountryCode2)
            => CountryCode1.CompareTo(CountryCode2) <= 0;

        /// <summary>
        /// Compares two country codes.
        /// </summary>
        public static Boolean operator >  (CountryCode CountryCode1, CountryCode CountryCode2)
            => CountryCode1.CompareTo(CountryCode2) > 0;

        /// <summary>
        /// Compares two country codes.
        /// </summary>
        public static Boolean operator >= (CountryCode CountryCode1, CountryCode CountryCode2)
            => CountryCode1.CompareTo(CountryCode2) >= 0;

        #endregion

        #region IComparable<CountryCode> Members

        /// <summary>
        /// Compares two country codes.
        /// </summary>
        /// <param name="Object">A country code to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is CountryCode code
                   ? CompareTo(code)
                   : throw new ArgumentException("The given object is not a country code!", nameof(Object));

        /// <summary>
        /// Compares two country codes.
        /// </summary>
        /// <param name="CountryCode">A country code to compare with.</param>
        public Int32 CompareTo(CountryCode CountryCode)
            => String.Compare(Value, CountryCode.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<CountryCode> Members

        /// <summary>
        /// Compares two country codes for equality.
        /// </summary>
        /// <param name="Object">A country code to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is CountryCode code && Equals(code);

        /// <summary>
        /// Compares two country codes for equality.
        /// </summary>
        /// <param name="CountryCode">A country code to compare with.</param>
        public Boolean Equals(CountryCode CountryCode)
            => String.Equals(Value, CountryCode.Value, StringComparison.Ordinal);

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

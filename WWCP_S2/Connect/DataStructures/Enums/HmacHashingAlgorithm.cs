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

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// Extension methods for HMAC hashing algorithms.
    /// </summary>
    public static class HmacHashingAlgorithmExtensions
    {

        /// <summary>
        /// Indicates whether this HMAC hashing algorithm is null or empty.
        /// </summary>
        /// <param name="HmacHashingAlgorithm">A HMAC hashing algorithm.</param>
        public static Boolean IsNullOrEmpty(this HmacHashingAlgorithm? HmacHashingAlgorithm)
            => !HmacHashingAlgorithm.HasValue || HmacHashingAlgorithm.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this HMAC hashing algorithm is NOT null or empty.
        /// </summary>
        /// <param name="HmacHashingAlgorithm">A HMAC hashing algorithm.</param>
        public static Boolean IsNotNullOrEmpty(this HmacHashingAlgorithm? HmacHashingAlgorithm)
            => HmacHashingAlgorithm.HasValue && HmacHashingAlgorithm.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The cryptographic hash function used by the HMAC challenge-response process of pairing. S2 Connect 1.0 defines SHA256 only; the client offers a list and the server selects one.
    /// </summary>
    public readonly struct HmacHashingAlgorithm : IS2PredefinedString,
                                         IId,
                                         IEquatable<HmacHashingAlgorithm>,
                                         IComparable<HmacHashingAlgorithm>
    {

        #region Data

        private readonly static Dictionary<String, HmacHashingAlgorithm>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this HMAC hashing algorithm is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this HMAC hashing algorithm is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the HMAC hashing algorithm.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All HMAC hashing algorithms defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<HmacHashingAlgorithm> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new HMAC hashing algorithm based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a HMAC hashing algorithm.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private HmacHashingAlgorithm(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml, HmacHashingAlgorithm: enum ["SHA256"]

        #endregion

        #region (private static) Register(Text)

        private static HmacHashingAlgorithm Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new HmacHashingAlgorithm(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a HMAC hashing algorithm.
        /// </summary>
        /// <param name="Text">A text representation of a HMAC hashing algorithm.</param>
        public static HmacHashingAlgorithm Parse(String Text)
        {

            if (TryParse(Text, out var hmacHashingAlgorithm))
                return hmacHashingAlgorithm;

            throw new ArgumentException($"Invalid text representation of a HMAC hashing algorithm: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a HMAC hashing algorithm.
        /// </summary>
        /// <param name="Text">A text representation of a HMAC hashing algorithm.</param>
        public static HmacHashingAlgorithm? TryParse(String Text)
        {

            if (TryParse(Text, out var hmacHashingAlgorithm))
                return hmacHashingAlgorithm;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out HmacHashingAlgorithm)

        /// <summary>
        /// Try to parse the given text as a HMAC hashing algorithm. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a HMAC hashing algorithm.</param>
        /// <param name="HmacHashingAlgorithm">The parsed HMAC hashing algorithm.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out HmacHashingAlgorithm    HmacHashingAlgorithm)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out HmacHashingAlgorithm))
                    HmacHashingAlgorithm = new HmacHashingAlgorithm(Text, false);

                return true;

            }

            HmacHashingAlgorithm = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this HMAC hashing algorithm.
        /// </summary>
        public HmacHashingAlgorithm Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// SHA256: HMAC with SHA-256 (the only algorithm of S2 Connect 1.0, mandatory in every offer).
        /// </summary>
        public static HmacHashingAlgorithm  SHA256    { get; }
            = Register("SHA256");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two HMAC hashing algorithms for equality.
        /// </summary>
        public static Boolean operator == (HmacHashingAlgorithm HmacHashingAlgorithm1, HmacHashingAlgorithm HmacHashingAlgorithm2)
            => HmacHashingAlgorithm1.Equals(HmacHashingAlgorithm2);

        /// <summary>
        /// Compares two HMAC hashing algorithms for inequality.
        /// </summary>
        public static Boolean operator != (HmacHashingAlgorithm HmacHashingAlgorithm1, HmacHashingAlgorithm HmacHashingAlgorithm2)
            => !HmacHashingAlgorithm1.Equals(HmacHashingAlgorithm2);

        /// <summary>
        /// Compares two HMAC hashing algorithms.
        /// </summary>
        public static Boolean operator <  (HmacHashingAlgorithm HmacHashingAlgorithm1, HmacHashingAlgorithm HmacHashingAlgorithm2)
            => HmacHashingAlgorithm1.CompareTo(HmacHashingAlgorithm2) < 0;

        /// <summary>
        /// Compares two HMAC hashing algorithms.
        /// </summary>
        public static Boolean operator <= (HmacHashingAlgorithm HmacHashingAlgorithm1, HmacHashingAlgorithm HmacHashingAlgorithm2)
            => HmacHashingAlgorithm1.CompareTo(HmacHashingAlgorithm2) <= 0;

        /// <summary>
        /// Compares two HMAC hashing algorithms.
        /// </summary>
        public static Boolean operator >  (HmacHashingAlgorithm HmacHashingAlgorithm1, HmacHashingAlgorithm HmacHashingAlgorithm2)
            => HmacHashingAlgorithm1.CompareTo(HmacHashingAlgorithm2) > 0;

        /// <summary>
        /// Compares two HMAC hashing algorithms.
        /// </summary>
        public static Boolean operator >= (HmacHashingAlgorithm HmacHashingAlgorithm1, HmacHashingAlgorithm HmacHashingAlgorithm2)
            => HmacHashingAlgorithm1.CompareTo(HmacHashingAlgorithm2) >= 0;

        #endregion

        #region IComparable<HmacHashingAlgorithm> Members

        /// <summary>
        /// Compares two HMAC hashing algorithms.
        /// </summary>
        /// <param name="Object">A HMAC hashing algorithm to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is HmacHashingAlgorithm hmacHashingAlgorithm
                   ? CompareTo(hmacHashingAlgorithm)
                   : throw new ArgumentException("The given object is not a HMAC hashing algorithm!", nameof(Object));

        /// <summary>
        /// Compares two HMAC hashing algorithms.
        /// </summary>
        /// <param name="HmacHashingAlgorithm">A HMAC hashing algorithm to compare with.</param>
        public Int32 CompareTo(HmacHashingAlgorithm HmacHashingAlgorithm)
            => String.Compare(InternalId, HmacHashingAlgorithm.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<HmacHashingAlgorithm> Members

        /// <summary>
        /// Compares two HMAC hashing algorithms for equality.
        /// </summary>
        /// <param name="Object">A HMAC hashing algorithm to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is HmacHashingAlgorithm hmacHashingAlgorithm && Equals(hmacHashingAlgorithm);

        /// <summary>
        /// Compares two HMAC hashing algorithms for equality.
        /// </summary>
        /// <param name="HmacHashingAlgorithm">A HMAC hashing algorithm to compare with.</param>
        public Boolean Equals(HmacHashingAlgorithm HmacHashingAlgorithm)
            => String.Equals(InternalId, HmacHashingAlgorithm.InternalId, StringComparison.Ordinal);

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

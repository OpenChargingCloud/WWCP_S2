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
    /// Extension methods for long-polling errors.
    /// </summary>
    public static class WaitForPairingErrorExtensions
    {

        /// <summary>
        /// Indicates whether this long-polling error is null or empty.
        /// </summary>
        /// <param name="WaitForPairingError">A long-polling error.</param>
        public static Boolean IsNullOrEmpty(this WaitForPairingError? WaitForPairingError)
            => !WaitForPairingError.HasValue || WaitForPairingError.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this long-polling error is NOT null or empty.
        /// </summary>
        /// <param name="WaitForPairingError">A long-polling error.</param>
        public static Boolean IsNotNullOrEmpty(this WaitForPairingError? WaitForPairingError)
            => WaitForPairingError.HasValue && WaitForPairingError.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The error a long-polling client reports for one of its nodes (waitForPairing request item errorMessage).
    /// </summary>
    public readonly struct WaitForPairingError : IS2PredefinedString,
                                         IId,
                                         IEquatable<WaitForPairingError>,
                                         IComparable<WaitForPairingError>
    {

        #region Data

        private readonly static Dictionary<String, WaitForPairingError>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this long-polling error is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this long-polling error is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the long-polling error.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All long-polling errors defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<WaitForPairingError> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new long-polling error based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a long-polling error.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private WaitForPairingError(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml, /waitForPairing request item errorMessage: enum ["NoValidTokenOnPairingClient"]

        #endregion

        #region (private static) Register(Text)

        private static WaitForPairingError Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new WaitForPairingError(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a long-polling error.
        /// </summary>
        /// <param name="Text">A text representation of a long-polling error.</param>
        public static WaitForPairingError Parse(String Text)
        {

            if (TryParse(Text, out var waitForPairingError))
                return waitForPairingError;

            throw new ArgumentException($"Invalid text representation of a long-polling error: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a long-polling error.
        /// </summary>
        /// <param name="Text">A text representation of a long-polling error.</param>
        public static WaitForPairingError? TryParse(String Text)
        {

            if (TryParse(Text, out var waitForPairingError))
                return waitForPairingError;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out WaitForPairingError)

        /// <summary>
        /// Try to parse the given text as a long-polling error. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a long-polling error.</param>
        /// <param name="WaitForPairingError">The parsed long-polling error.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out WaitForPairingError    WaitForPairingError)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out WaitForPairingError))
                    WaitForPairingError = new WaitForPairingError(Text, false);

                return true;

            }

            WaitForPairingError = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this long-polling error.
        /// </summary>
        public WaitForPairingError Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// NoValidTokenOnPairingClient: The node was asked to request pairing, but has no valid (unexpired) pairing token.
        /// </summary>
        public static WaitForPairingError  NoValidTokenOnPairingClient    { get; }
            = Register("NoValidTokenOnPairingClient");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two long-polling errors for equality.
        /// </summary>
        public static Boolean operator == (WaitForPairingError WaitForPairingError1, WaitForPairingError WaitForPairingError2)
            => WaitForPairingError1.Equals(WaitForPairingError2);

        /// <summary>
        /// Compares two long-polling errors for inequality.
        /// </summary>
        public static Boolean operator != (WaitForPairingError WaitForPairingError1, WaitForPairingError WaitForPairingError2)
            => !WaitForPairingError1.Equals(WaitForPairingError2);

        /// <summary>
        /// Compares two long-polling errors.
        /// </summary>
        public static Boolean operator <  (WaitForPairingError WaitForPairingError1, WaitForPairingError WaitForPairingError2)
            => WaitForPairingError1.CompareTo(WaitForPairingError2) < 0;

        /// <summary>
        /// Compares two long-polling errors.
        /// </summary>
        public static Boolean operator <= (WaitForPairingError WaitForPairingError1, WaitForPairingError WaitForPairingError2)
            => WaitForPairingError1.CompareTo(WaitForPairingError2) <= 0;

        /// <summary>
        /// Compares two long-polling errors.
        /// </summary>
        public static Boolean operator >  (WaitForPairingError WaitForPairingError1, WaitForPairingError WaitForPairingError2)
            => WaitForPairingError1.CompareTo(WaitForPairingError2) > 0;

        /// <summary>
        /// Compares two long-polling errors.
        /// </summary>
        public static Boolean operator >= (WaitForPairingError WaitForPairingError1, WaitForPairingError WaitForPairingError2)
            => WaitForPairingError1.CompareTo(WaitForPairingError2) >= 0;

        #endregion

        #region IComparable<WaitForPairingError> Members

        /// <summary>
        /// Compares two long-polling errors.
        /// </summary>
        /// <param name="Object">A long-polling error to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is WaitForPairingError waitForPairingError
                   ? CompareTo(waitForPairingError)
                   : throw new ArgumentException("The given object is not a long-polling error!", nameof(Object));

        /// <summary>
        /// Compares two long-polling errors.
        /// </summary>
        /// <param name="WaitForPairingError">A long-polling error to compare with.</param>
        public Int32 CompareTo(WaitForPairingError WaitForPairingError)
            => String.Compare(InternalId, WaitForPairingError.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<WaitForPairingError> Members

        /// <summary>
        /// Compares two long-polling errors for equality.
        /// </summary>
        /// <param name="Object">A long-polling error to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is WaitForPairingError waitForPairingError && Equals(waitForPairingError);

        /// <summary>
        /// Compares two long-polling errors for equality.
        /// </summary>
        /// <param name="WaitForPairingError">A long-polling error to compare with.</param>
        public Boolean Equals(WaitForPairingError WaitForPairingError)
            => String.Equals(InternalId, WaitForPairingError.InternalId, StringComparison.Ordinal);

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

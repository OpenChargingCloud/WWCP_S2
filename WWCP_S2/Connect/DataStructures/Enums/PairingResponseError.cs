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
    /// Extension methods for pairing response errors.
    /// </summary>
    public static class PairingResponseErrorExtensions
    {

        /// <summary>
        /// Indicates whether this pairing response error is null or empty.
        /// </summary>
        /// <param name="PairingResponseError">A pairing response error.</param>
        public static Boolean IsNullOrEmpty(this PairingResponseError? PairingResponseError)
            => !PairingResponseError.HasValue || PairingResponseError.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this pairing response error is NOT null or empty.
        /// </summary>
        /// <param name="PairingResponseError">A pairing response error.</param>
        public static Boolean IsNotNullOrEmpty(this PairingResponseError? PairingResponseError)
            => PairingResponseError.HasValue && PairingResponseError.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The error reported with HTTP 400 during the pairing process (PairingResponseErrorMessage.errorMessage).
    /// </summary>
    public readonly struct PairingResponseError : IS2PredefinedString,
                                         IId,
                                         IEquatable<PairingResponseError>,
                                         IComparable<PairingResponseError>
    {

        #region Data

        private readonly static Dictionary<String, PairingResponseError>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this pairing response error is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this pairing response error is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the pairing response error.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All pairing response errors defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<PairingResponseError> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new pairing response error based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a pairing response error.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private PairingResponseError(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml, PairingResponseErrorMessage.errorMessage: enum ["InvalidCombinationOfRoles", "IncompatibleS2MessageVersions", "IncompatibleHmacHashingAlgorithms", "IncompatibleCommunicationProtocols", "NodeNotFound", "NoNodeIdProvided", "NoValidPairingTokenOnPairingServer", "ParsingError", "Other"]

        #endregion

        #region (private static) Register(Text)

        private static PairingResponseError Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new PairingResponseError(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a pairing response error.
        /// </summary>
        /// <param name="Text">A text representation of a pairing response error.</param>
        public static PairingResponseError Parse(String Text)
        {

            if (TryParse(Text, out var pairingResponseError))
                return pairingResponseError;

            throw new ArgumentException($"Invalid text representation of a pairing response error: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a pairing response error.
        /// </summary>
        /// <param name="Text">A text representation of a pairing response error.</param>
        public static PairingResponseError? TryParse(String Text)
        {

            if (TryParse(Text, out var pairingResponseError))
                return pairingResponseError;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out PairingResponseError)

        /// <summary>
        /// Try to parse the given text as a pairing response error. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a pairing response error.</param>
        /// <param name="PairingResponseError">The parsed pairing response error.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out PairingResponseError    PairingResponseError)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out PairingResponseError))
                    PairingResponseError = new PairingResponseError(Text, false);

                return true;

            }

            PairingResponseError = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this pairing response error.
        /// </summary>
        public PairingResponseError Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// InvalidCombinationOfRoles: The targeted node has the same role as the client node (two CEMs or two RMs cannot pair).
        /// </summary>
        public static PairingResponseError  InvalidCombinationOfRoles    { get; }
            = Register("InvalidCombinationOfRoles");

        /// <summary>
        /// IncompatibleS2MessageVersions: No common S2 JSON message version (can be ignored with forcePairing).
        /// </summary>
        public static PairingResponseError  IncompatibleS2MessageVersions    { get; }
            = Register("IncompatibleS2MessageVersions");

        /// <summary>
        /// IncompatibleHmacHashingAlgorithms: The server accepts none of the offered HMAC hashing algorithms.
        /// </summary>
        public static PairingResponseError  IncompatibleHmacHashingAlgorithms    { get; }
            = Register("IncompatibleHmacHashingAlgorithms");

        /// <summary>
        /// IncompatibleCommunicationProtocols: No common communication protocol (can be ignored with forcePairing).
        /// </summary>
        public static PairingResponseError  IncompatibleCommunicationProtocols    { get; }
            = Register("IncompatibleCommunicationProtocols");

        /// <summary>
        /// NodeNotFound: The server does not recognise the nodeId or nodeIdAlias.
        /// </summary>
        public static PairingResponseError  NodeNotFound    { get; }
            = Register("NodeNotFound");

        /// <summary>
        /// NoNodeIdProvided: Neither nodeId nor nodeIdAlias was provided, but the endpoint represents more than one node.
        /// </summary>
        public static PairingResponseError  NoNodeIdProvided    { get; }
            = Register("NoNodeIdProvided");

        /// <summary>
        /// NoValidPairingTokenOnPairingServer: The targeted node has no valid (unexpired) pairing token, or the end user did not enter one.
        /// </summary>
        public static PairingResponseError  NoValidPairingTokenOnPairingServer    { get; }
            = Register("NoValidPairingTokenOnPairingServer");

        /// <summary>
        /// ParsingError: The request is not properly formatted or does not follow the schema.
        /// </summary>
        public static PairingResponseError  ParsingError    { get; }
            = Register("ParsingError");

        /// <summary>
        /// Other: The endpoint or node is not ready for pairing, or another reason described in additionalInfo.
        /// </summary>
        public static PairingResponseError  Other    { get; }
            = Register("Other");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two pairing response errors for equality.
        /// </summary>
        public static Boolean operator == (PairingResponseError PairingResponseError1, PairingResponseError PairingResponseError2)
            => PairingResponseError1.Equals(PairingResponseError2);

        /// <summary>
        /// Compares two pairing response errors for inequality.
        /// </summary>
        public static Boolean operator != (PairingResponseError PairingResponseError1, PairingResponseError PairingResponseError2)
            => !PairingResponseError1.Equals(PairingResponseError2);

        /// <summary>
        /// Compares two pairing response errors.
        /// </summary>
        public static Boolean operator <  (PairingResponseError PairingResponseError1, PairingResponseError PairingResponseError2)
            => PairingResponseError1.CompareTo(PairingResponseError2) < 0;

        /// <summary>
        /// Compares two pairing response errors.
        /// </summary>
        public static Boolean operator <= (PairingResponseError PairingResponseError1, PairingResponseError PairingResponseError2)
            => PairingResponseError1.CompareTo(PairingResponseError2) <= 0;

        /// <summary>
        /// Compares two pairing response errors.
        /// </summary>
        public static Boolean operator >  (PairingResponseError PairingResponseError1, PairingResponseError PairingResponseError2)
            => PairingResponseError1.CompareTo(PairingResponseError2) > 0;

        /// <summary>
        /// Compares two pairing response errors.
        /// </summary>
        public static Boolean operator >= (PairingResponseError PairingResponseError1, PairingResponseError PairingResponseError2)
            => PairingResponseError1.CompareTo(PairingResponseError2) >= 0;

        #endregion

        #region IComparable<PairingResponseError> Members

        /// <summary>
        /// Compares two pairing response errors.
        /// </summary>
        /// <param name="Object">A pairing response error to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PairingResponseError pairingResponseError
                   ? CompareTo(pairingResponseError)
                   : throw new ArgumentException("The given object is not a pairing response error!", nameof(Object));

        /// <summary>
        /// Compares two pairing response errors.
        /// </summary>
        /// <param name="PairingResponseError">A pairing response error to compare with.</param>
        public Int32 CompareTo(PairingResponseError PairingResponseError)
            => String.Compare(InternalId, PairingResponseError.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<PairingResponseError> Members

        /// <summary>
        /// Compares two pairing response errors for equality.
        /// </summary>
        /// <param name="Object">A pairing response error to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PairingResponseError pairingResponseError && Equals(pairingResponseError);

        /// <summary>
        /// Compares two pairing response errors for equality.
        /// </summary>
        /// <param name="PairingResponseError">A pairing response error to compare with.</param>
        public Boolean Equals(PairingResponseError PairingResponseError)
            => String.Equals(InternalId, PairingResponseError.InternalId, StringComparison.Ordinal);

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

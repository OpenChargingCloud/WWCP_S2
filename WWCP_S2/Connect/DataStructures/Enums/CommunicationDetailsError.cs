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
    /// Extension methods for communication details errors.
    /// </summary>
    public static class CommunicationDetailsErrorExtensions
    {

        /// <summary>
        /// Indicates whether this communication details error is null or empty.
        /// </summary>
        /// <param name="CommunicationDetailsError">A communication details error.</param>
        public static Boolean IsNullOrEmpty(this CommunicationDetailsError? CommunicationDetailsError)
            => !CommunicationDetailsError.HasValue || CommunicationDetailsError.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this communication details error is NOT null or empty.
        /// </summary>
        /// <param name="CommunicationDetailsError">A communication details error.</param>
        public static Boolean IsNotNullOrEmpty(this CommunicationDetailsError? CommunicationDetailsError)
            => CommunicationDetailsError.HasValue && CommunicationDetailsError.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The error reported with HTTP 400 during session initiation (CommunicationDetailsErrorMessage.errorMessage).
    /// </summary>
    public readonly struct CommunicationDetailsError : IS2PredefinedString,
                                         IId,
                                         IEquatable<CommunicationDetailsError>,
                                         IComparable<CommunicationDetailsError>
    {

        #region Data

        private readonly static Dictionary<String, CommunicationDetailsError>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this communication details error is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this communication details error is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the communication details error.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All communication details errors defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<CommunicationDetailsError> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new communication details error based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a communication details error.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private CommunicationDetailsError(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // s2-connect-session-init.yml, CommunicationDetailsErrorMessage.errorMessage: enum ["IncompatibleS2MessageVersions", "IncompatibleCommunicationProtocols", "NoLongerPaired", "ParsingError", "Other"]

        #endregion

        #region (private static) Register(Text)

        private static CommunicationDetailsError Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new CommunicationDetailsError(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a communication details error.
        /// </summary>
        /// <param name="Text">A text representation of a communication details error.</param>
        public static CommunicationDetailsError Parse(String Text)
        {

            if (TryParse(Text, out var communicationDetailsError))
                return communicationDetailsError;

            throw new ArgumentException($"Invalid text representation of a communication details error: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a communication details error.
        /// </summary>
        /// <param name="Text">A text representation of a communication details error.</param>
        public static CommunicationDetailsError? TryParse(String Text)
        {

            if (TryParse(Text, out var communicationDetailsError))
                return communicationDetailsError;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out CommunicationDetailsError)

        /// <summary>
        /// Try to parse the given text as a communication details error. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a communication details error.</param>
        /// <param name="CommunicationDetailsError">The parsed communication details error.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out CommunicationDetailsError    CommunicationDetailsError)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out CommunicationDetailsError))
                    CommunicationDetailsError = new CommunicationDetailsError(Text, false);

                return true;

            }

            CommunicationDetailsError = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this communication details error.
        /// </summary>
        public CommunicationDetailsError Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// IncompatibleS2MessageVersions: No common S2 JSON message version; the client may retry later.
        /// </summary>
        public static CommunicationDetailsError  IncompatibleS2MessageVersions    { get; }
            = Register("IncompatibleS2MessageVersions");

        /// <summary>
        /// IncompatibleCommunicationProtocols: No common communication protocol; the client may retry later.
        /// </summary>
        public static CommunicationDetailsError  IncompatibleCommunicationProtocols    { get; }
            = Register("IncompatibleCommunicationProtocols");

        /// <summary>
        /// NoLongerPaired: The nodes were paired but have been unpaired; the client must not retry and should inform the end user.
        /// </summary>
        public static CommunicationDetailsError  NoLongerPaired    { get; }
            = Register("NoLongerPaired");

        /// <summary>
        /// ParsingError: The request is not properly formatted or does not follow the schema.
        /// </summary>
        public static CommunicationDetailsError  ParsingError    { get; }
            = Register("ParsingError");

        /// <summary>
        /// Other: The endpoint or node is not ready for connecting; the client may retry later.
        /// </summary>
        public static CommunicationDetailsError  Other    { get; }
            = Register("Other");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two communication details errors for equality.
        /// </summary>
        public static Boolean operator == (CommunicationDetailsError CommunicationDetailsError1, CommunicationDetailsError CommunicationDetailsError2)
            => CommunicationDetailsError1.Equals(CommunicationDetailsError2);

        /// <summary>
        /// Compares two communication details errors for inequality.
        /// </summary>
        public static Boolean operator != (CommunicationDetailsError CommunicationDetailsError1, CommunicationDetailsError CommunicationDetailsError2)
            => !CommunicationDetailsError1.Equals(CommunicationDetailsError2);

        /// <summary>
        /// Compares two communication details errors.
        /// </summary>
        public static Boolean operator <  (CommunicationDetailsError CommunicationDetailsError1, CommunicationDetailsError CommunicationDetailsError2)
            => CommunicationDetailsError1.CompareTo(CommunicationDetailsError2) < 0;

        /// <summary>
        /// Compares two communication details errors.
        /// </summary>
        public static Boolean operator <= (CommunicationDetailsError CommunicationDetailsError1, CommunicationDetailsError CommunicationDetailsError2)
            => CommunicationDetailsError1.CompareTo(CommunicationDetailsError2) <= 0;

        /// <summary>
        /// Compares two communication details errors.
        /// </summary>
        public static Boolean operator >  (CommunicationDetailsError CommunicationDetailsError1, CommunicationDetailsError CommunicationDetailsError2)
            => CommunicationDetailsError1.CompareTo(CommunicationDetailsError2) > 0;

        /// <summary>
        /// Compares two communication details errors.
        /// </summary>
        public static Boolean operator >= (CommunicationDetailsError CommunicationDetailsError1, CommunicationDetailsError CommunicationDetailsError2)
            => CommunicationDetailsError1.CompareTo(CommunicationDetailsError2) >= 0;

        #endregion

        #region IComparable<CommunicationDetailsError> Members

        /// <summary>
        /// Compares two communication details errors.
        /// </summary>
        /// <param name="Object">A communication details error to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is CommunicationDetailsError communicationDetailsError
                   ? CompareTo(communicationDetailsError)
                   : throw new ArgumentException("The given object is not a communication details error!", nameof(Object));

        /// <summary>
        /// Compares two communication details errors.
        /// </summary>
        /// <param name="CommunicationDetailsError">A communication details error to compare with.</param>
        public Int32 CompareTo(CommunicationDetailsError CommunicationDetailsError)
            => String.Compare(InternalId, CommunicationDetailsError.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<CommunicationDetailsError> Members

        /// <summary>
        /// Compares two communication details errors for equality.
        /// </summary>
        /// <param name="Object">A communication details error to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is CommunicationDetailsError communicationDetailsError && Equals(communicationDetailsError);

        /// <summary>
        /// Compares two communication details errors for equality.
        /// </summary>
        /// <param name="CommunicationDetailsError">A communication details error to compare with.</param>
        public Boolean Equals(CommunicationDetailsError CommunicationDetailsError)
            => String.Equals(InternalId, CommunicationDetailsError.InternalId, StringComparison.Ordinal);

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

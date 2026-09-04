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
    /// Extension methods for session request types.
    /// </summary>
    public static class SessionRequestTypeExtensions
    {

        /// <summary>
        /// Indicates whether this session request type is null or empty.
        /// </summary>
        /// <param name="SessionRequestType">A session request type.</param>
        public static Boolean IsNullOrEmpty(this SessionRequestType? SessionRequestType)
            => !SessionRequestType.HasValue || SessionRequestType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this session request type is NOT null or empty.
        /// </summary>
        /// <param name="SessionRequestType">A session request type.</param>
        public static Boolean IsNotNullOrEmpty(this SessionRequestType? SessionRequestType)
            => SessionRequestType.HasValue && SessionRequestType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The type of a SessionRequest: reconnect or terminate the session.
    /// </summary>
    public readonly struct SessionRequestType : IS2PredefinedString,
                                         IId,
                                         IEquatable<SessionRequestType>,
                                         IComparable<SessionRequestType>
    {

        #region Data

        private readonly static Dictionary<String, SessionRequestType>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this session request type is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this session request type is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the session request type.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All session request types defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<SessionRequestType> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new session request type based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a session request type.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private SessionRequestType(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // SessionRequestType.schema.json
        //   "title": "SessionRequestType",
        //   "type":  "string",
        //   "enum":  ["RECONNECT", "TERMINATE"],
        //   "description":
        //     RECONNECT: Please reconnect the WebSocket session. Once reconnected, it starts from scratch with a handshake.
        //     TERMINATE: Disconnect the session (client can try to reconnecting with exponential backoff)

        #endregion

        #region (private static) Register(Text)

        private static SessionRequestType Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new SessionRequestType(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a session request type.
        /// </summary>
        /// <param name="Text">A text representation of a session request type.</param>
        public static SessionRequestType Parse(String Text)
        {

            if (TryParse(Text, out var sessionRequestType))
                return sessionRequestType;

            throw new ArgumentException($"Invalid text representation of a session request type: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a session request type.
        /// </summary>
        /// <param name="Text">A text representation of a session request type.</param>
        public static SessionRequestType? TryParse(String Text)
        {

            if (TryParse(Text, out var sessionRequestType))
                return sessionRequestType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out SessionRequestType)

        /// <summary>
        /// Try to parse the given text as a session request type. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a session request type.</param>
        /// <param name="SessionRequestType">The parsed session request type.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out SessionRequestType    SessionRequestType)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out SessionRequestType))
                    SessionRequestType = new SessionRequestType(Text, false);

                return true;

            }

            SessionRequestType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this session request type.
        /// </summary>
        public SessionRequestType Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// RECONNECT: Please reconnect the WebSocket session. Once reconnected, it starts from scratch with a handshake.
        /// </summary>
        public static SessionRequestType  Reconnect    { get; }
            = Register("RECONNECT");

        /// <summary>
        /// TERMINATE: Disconnect the session (client can try to reconnecting with exponential backoff)
        /// </summary>
        public static SessionRequestType  Terminate    { get; }
            = Register("TERMINATE");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two session request types for equality.
        /// </summary>
        public static Boolean operator == (SessionRequestType SessionRequestType1, SessionRequestType SessionRequestType2)
            => SessionRequestType1.Equals(SessionRequestType2);

        /// <summary>
        /// Compares two session request types for inequality.
        /// </summary>
        public static Boolean operator != (SessionRequestType SessionRequestType1, SessionRequestType SessionRequestType2)
            => !SessionRequestType1.Equals(SessionRequestType2);

        /// <summary>
        /// Compares two session request types.
        /// </summary>
        public static Boolean operator <  (SessionRequestType SessionRequestType1, SessionRequestType SessionRequestType2)
            => SessionRequestType1.CompareTo(SessionRequestType2) < 0;

        /// <summary>
        /// Compares two session request types.
        /// </summary>
        public static Boolean operator <= (SessionRequestType SessionRequestType1, SessionRequestType SessionRequestType2)
            => SessionRequestType1.CompareTo(SessionRequestType2) <= 0;

        /// <summary>
        /// Compares two session request types.
        /// </summary>
        public static Boolean operator >  (SessionRequestType SessionRequestType1, SessionRequestType SessionRequestType2)
            => SessionRequestType1.CompareTo(SessionRequestType2) > 0;

        /// <summary>
        /// Compares two session request types.
        /// </summary>
        public static Boolean operator >= (SessionRequestType SessionRequestType1, SessionRequestType SessionRequestType2)
            => SessionRequestType1.CompareTo(SessionRequestType2) >= 0;

        #endregion

        #region IComparable<SessionRequestType> Members

        /// <summary>
        /// Compares two session request types.
        /// </summary>
        /// <param name="Object">A session request type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is SessionRequestType sessionRequestType
                   ? CompareTo(sessionRequestType)
                   : throw new ArgumentException("The given object is not a session request type!", nameof(Object));

        /// <summary>
        /// Compares two session request types.
        /// </summary>
        /// <param name="SessionRequestType">A session request type to compare with.</param>
        public Int32 CompareTo(SessionRequestType SessionRequestType)
            => String.Compare(InternalId, SessionRequestType.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<SessionRequestType> Members

        /// <summary>
        /// Compares two session request types for equality.
        /// </summary>
        /// <param name="Object">A session request type to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is SessionRequestType sessionRequestType && Equals(sessionRequestType);

        /// <summary>
        /// Compares two session request types for equality.
        /// </summary>
        /// <param name="SessionRequestType">A session request type to compare with.</param>
        public Boolean Equals(SessionRequestType SessionRequestType)
            => String.Equals(InternalId, SessionRequestType.InternalId, StringComparison.Ordinal);

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

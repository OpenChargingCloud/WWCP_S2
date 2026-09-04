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
    /// Extension methods for communication protocols.
    /// </summary>
    public static class CommunicationProtocolExtensions
    {

        /// <summary>
        /// Indicates whether this communication protocol is null or empty.
        /// </summary>
        /// <param name="CommunicationProtocol">A communication protocol.</param>
        public static Boolean IsNullOrEmpty(this CommunicationProtocol? CommunicationProtocol)
            => !CommunicationProtocol.HasValue || CommunicationProtocol.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this communication protocol is NOT null or empty.
        /// </summary>
        /// <param name="CommunicationProtocol">A communication protocol.</param>
        public static Boolean IsNotNullOrEmpty(this CommunicationProtocol? CommunicationProtocol)
            => CommunicationProtocol.HasValue && CommunicationProtocol.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A communication protocol for exchanging S2 messages after session initiation. S2 Connect 1.0 defines WebSocket only; MQTT is announced for a later version.
    /// </summary>
    public readonly struct CommunicationProtocol : IS2PredefinedString,
                                         IId,
                                         IEquatable<CommunicationProtocol>,
                                         IComparable<CommunicationProtocol>
    {

        #region Data

        private readonly static Dictionary<String, CommunicationProtocol>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this communication protocol is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this communication protocol is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the communication protocol.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All communication protocols defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<CommunicationProtocol> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new communication protocol based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a communication protocol.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private CommunicationProtocol(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // s2-connect-common.yml, CommunicationProtocol: enum ["WebSocket"]

        #endregion

        #region (private static) Register(Text)

        private static CommunicationProtocol Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new CommunicationProtocol(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a communication protocol.
        /// </summary>
        /// <param name="Text">A text representation of a communication protocol.</param>
        public static CommunicationProtocol Parse(String Text)
        {

            if (TryParse(Text, out var communicationProtocol))
                return communicationProtocol;

            throw new ArgumentException($"Invalid text representation of a communication protocol: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a communication protocol.
        /// </summary>
        /// <param name="Text">A text representation of a communication protocol.</param>
        public static CommunicationProtocol? TryParse(String Text)
        {

            if (TryParse(Text, out var communicationProtocol))
                return communicationProtocol;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out CommunicationProtocol)

        /// <summary>
        /// Try to parse the given text as a communication protocol. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a communication protocol.</param>
        /// <param name="CommunicationProtocol">The parsed communication protocol.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out CommunicationProtocol    CommunicationProtocol)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out CommunicationProtocol))
                    CommunicationProtocol = new CommunicationProtocol(Text, false);

                return true;

            }

            CommunicationProtocol = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this communication protocol.
        /// </summary>
        public CommunicationProtocol Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// WebSocket: S2 messages are exchanged over a WebSocket (RFC 6455) connection.
        /// </summary>
        public static CommunicationProtocol  WebSocket    { get; }
            = Register("WebSocket");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two communication protocols for equality.
        /// </summary>
        public static Boolean operator == (CommunicationProtocol CommunicationProtocol1, CommunicationProtocol CommunicationProtocol2)
            => CommunicationProtocol1.Equals(CommunicationProtocol2);

        /// <summary>
        /// Compares two communication protocols for inequality.
        /// </summary>
        public static Boolean operator != (CommunicationProtocol CommunicationProtocol1, CommunicationProtocol CommunicationProtocol2)
            => !CommunicationProtocol1.Equals(CommunicationProtocol2);

        /// <summary>
        /// Compares two communication protocols.
        /// </summary>
        public static Boolean operator <  (CommunicationProtocol CommunicationProtocol1, CommunicationProtocol CommunicationProtocol2)
            => CommunicationProtocol1.CompareTo(CommunicationProtocol2) < 0;

        /// <summary>
        /// Compares two communication protocols.
        /// </summary>
        public static Boolean operator <= (CommunicationProtocol CommunicationProtocol1, CommunicationProtocol CommunicationProtocol2)
            => CommunicationProtocol1.CompareTo(CommunicationProtocol2) <= 0;

        /// <summary>
        /// Compares two communication protocols.
        /// </summary>
        public static Boolean operator >  (CommunicationProtocol CommunicationProtocol1, CommunicationProtocol CommunicationProtocol2)
            => CommunicationProtocol1.CompareTo(CommunicationProtocol2) > 0;

        /// <summary>
        /// Compares two communication protocols.
        /// </summary>
        public static Boolean operator >= (CommunicationProtocol CommunicationProtocol1, CommunicationProtocol CommunicationProtocol2)
            => CommunicationProtocol1.CompareTo(CommunicationProtocol2) >= 0;

        #endregion

        #region IComparable<CommunicationProtocol> Members

        /// <summary>
        /// Compares two communication protocols.
        /// </summary>
        /// <param name="Object">A communication protocol to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is CommunicationProtocol communicationProtocol
                   ? CompareTo(communicationProtocol)
                   : throw new ArgumentException("The given object is not a communication protocol!", nameof(Object));

        /// <summary>
        /// Compares two communication protocols.
        /// </summary>
        /// <param name="CommunicationProtocol">A communication protocol to compare with.</param>
        public Int32 CompareTo(CommunicationProtocol CommunicationProtocol)
            => String.Compare(InternalId, CommunicationProtocol.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<CommunicationProtocol> Members

        /// <summary>
        /// Compares two communication protocols for equality.
        /// </summary>
        /// <param name="Object">A communication protocol to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is CommunicationProtocol communicationProtocol && Equals(communicationProtocol);

        /// <summary>
        /// Compares two communication protocols for equality.
        /// </summary>
        /// <param name="CommunicationProtocol">A communication protocol to compare with.</param>
        public Boolean Equals(CommunicationProtocol CommunicationProtocol)
            => String.Equals(InternalId, CommunicationProtocol.InternalId, StringComparison.Ordinal);

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

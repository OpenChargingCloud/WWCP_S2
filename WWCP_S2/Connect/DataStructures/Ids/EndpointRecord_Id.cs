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
    /// Extension methods for endpoint record identifications.
    /// </summary>
    public static class EndpointRecordIdExtensions
    {

        /// <summary>
        /// Indicates whether this endpoint record identification is null or empty.
        /// </summary>
        /// <param name="EndpointRecordId">A endpoint record identification.</param>
        public static Boolean IsNullOrEmpty(this EndpointRecord_Id? EndpointRecordId)
            => !EndpointRecordId.HasValue || EndpointRecordId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this endpoint record identification is NOT null or empty.
        /// </summary>
        /// <param name="EndpointRecordId">A endpoint record identification.</param>
        public static Boolean IsNotNullOrEmpty(this EndpointRecord_Id? EndpointRecordId)
            => EndpointRecordId.HasValue && EndpointRecordId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of an endpoint record in the WAN pairing endpoint registry
    /// (s2-connect-wan-endpoint-registry.yml, Id: "Unique identifier of the WAN endpoint registration", format uuid).
    /// </summary>
    public readonly struct EndpointRecord_Id : IId,
                                     IEquatable<EndpointRecord_Id>,
                                     IComparable<EndpointRecord_Id>
    {

        #region Properties

        /// <summary>
        /// The UUID value of the endpoint record identification.
        /// </summary>
        public Guid     Value               { get; }

        /// <summary>
        /// Indicates whether this identification is the empty UUID.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => Value == Guid.Empty;

        /// <summary>
        /// Indicates whether this identification is NOT the empty UUID.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => Value != Guid.Empty;

        /// <summary>
        /// The length of the text representation (36 characters).
        /// </summary>
        public UInt64   Length
            => 36;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new endpoint record identification based on the given UUID.
        /// </summary>
        /// <param name="Value">A UUID.</param>
        public EndpointRecord_Id(Guid Value)
        {
            this.Value = Value;
        }

        #endregion


        #region Documentation

        // s2-connect-wan-endpoint-registry.yml
        //   Id:
        //     description: Unique identifier of the WAN endpoint registration
        //     type:        string
        //     format:      uuid

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) endpoint record identification.
        /// </summary>
        public static EndpointRecord_Id NewRandom
            => new (Guid.CreateVersion7());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a endpoint record identification.
        /// </summary>
        /// <param name="Text">A text representation of a endpoint record identification (canonical hyphenated UUID).</param>
        public static EndpointRecord_Id Parse(String Text)
        {

            if (TryParse(Text, out var nodeId))
                return nodeId;

            throw new ArgumentException($"Invalid text representation of a endpoint record identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a endpoint record identification.
        /// </summary>
        /// <param name="Text">A text representation of a endpoint record identification.</param>
        public static EndpointRecord_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var nodeId))
                return nodeId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out EndpointRecordId)

        /// <summary>
        /// Try to parse the given text as a endpoint record identification. Only the canonical
        /// hyphenated form (8-4-4-4-12 hexadecimal digits, any case) is accepted.
        /// </summary>
        /// <param name="Text">A text representation of a endpoint record identification.</param>
        /// <param name="EndpointRecordId">The parsed endpoint record identification.</param>
        public static Boolean TryParse(String                              Text,
                                       [NotNullWhen(true)] out EndpointRecord_Id     EndpointRecordId)
        {

            if (Guid.TryParseExact(Text, "D", out var guid))
            {
                EndpointRecordId = new EndpointRecord_Id(guid);
                return true;
            }

            EndpointRecordId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this endpoint record identification.
        /// </summary>
        public EndpointRecord_Id Clone()
            => new (Value);

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two endpoint record identifications for equality.
        /// </summary>
        public static Boolean operator == (EndpointRecord_Id EndpointRecordId1, EndpointRecord_Id EndpointRecordId2)
            => EndpointRecordId1.Equals(EndpointRecordId2);

        /// <summary>
        /// Compares two endpoint record identifications for inequality.
        /// </summary>
        public static Boolean operator != (EndpointRecord_Id EndpointRecordId1, EndpointRecord_Id EndpointRecordId2)
            => !EndpointRecordId1.Equals(EndpointRecordId2);

        /// <summary>
        /// Compares two endpoint record identifications.
        /// </summary>
        public static Boolean operator <  (EndpointRecord_Id EndpointRecordId1, EndpointRecord_Id EndpointRecordId2)
            => EndpointRecordId1.CompareTo(EndpointRecordId2) < 0;

        /// <summary>
        /// Compares two endpoint record identifications.
        /// </summary>
        public static Boolean operator <= (EndpointRecord_Id EndpointRecordId1, EndpointRecord_Id EndpointRecordId2)
            => EndpointRecordId1.CompareTo(EndpointRecordId2) <= 0;

        /// <summary>
        /// Compares two endpoint record identifications.
        /// </summary>
        public static Boolean operator >  (EndpointRecord_Id EndpointRecordId1, EndpointRecord_Id EndpointRecordId2)
            => EndpointRecordId1.CompareTo(EndpointRecordId2) > 0;

        /// <summary>
        /// Compares two endpoint record identifications.
        /// </summary>
        public static Boolean operator >= (EndpointRecord_Id EndpointRecordId1, EndpointRecord_Id EndpointRecordId2)
            => EndpointRecordId1.CompareTo(EndpointRecordId2) >= 0;

        #endregion

        #region IComparable<EndpointRecord_Id> Members

        /// <summary>
        /// Compares two endpoint record identifications.
        /// </summary>
        /// <param name="Object">A endpoint record identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EndpointRecord_Id nodeId
                   ? CompareTo(nodeId)
                   : throw new ArgumentException("The given object is not a endpoint record identification!", nameof(Object));

        /// <summary>
        /// Compares two endpoint record identifications.
        /// </summary>
        /// <param name="EndpointRecordId">A endpoint record identification to compare with.</param>
        public Int32 CompareTo(EndpointRecord_Id EndpointRecordId)
            => Value.CompareTo(EndpointRecordId.Value);

        #endregion

        #region IEquatable<EndpointRecord_Id> Members

        /// <summary>
        /// Compares two endpoint record identifications for equality.
        /// </summary>
        /// <param name="Object">A endpoint record identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is EndpointRecord_Id nodeId && Equals(nodeId);

        /// <summary>
        /// Compares two endpoint record identifications for equality.
        /// </summary>
        /// <param name="EndpointRecordId">A endpoint record identification to compare with.</param>
        public Boolean Equals(EndpointRecord_Id EndpointRecordId)
            => Value.Equals(EndpointRecordId.Value);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => Value.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return the canonical lower-case hyphenated text representation of this UUID.
        /// </summary>
        public override String ToString()
            => Value.ToString("D");

        #endregion

    }

}

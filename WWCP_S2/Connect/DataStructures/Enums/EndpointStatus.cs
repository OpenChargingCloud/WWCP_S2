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
    /// Extension methods for endpoint statuses.
    /// </summary>
    public static class EndpointStatusExtensions
    {

        /// <summary>
        /// Indicates whether this endpoint status is null or empty.
        /// </summary>
        /// <param name="EndpointStatus">A endpoint status.</param>
        public static Boolean IsNullOrEmpty(this EndpointStatus? EndpointStatus)
            => !EndpointStatus.HasValue || EndpointStatus.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this endpoint status is NOT null or empty.
        /// </summary>
        /// <param name="EndpointStatus">A endpoint status.</param>
        public static Boolean IsNotNullOrEmpty(this EndpointStatus? EndpointStatus)
            => EndpointStatus.HasValue && EndpointStatus.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of an endpoint in the WAN pairing endpoint registry.
    /// </summary>
    public readonly struct EndpointStatus : IS2PredefinedString,
                                         IId,
                                         IEquatable<EndpointStatus>,
                                         IComparable<EndpointStatus>
    {

        #region Data

        private readonly static Dictionary<String, EndpointStatus>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this endpoint status is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this endpoint status is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the endpoint status.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All endpoint statuses defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<EndpointStatus> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new endpoint status based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a endpoint status.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private EndpointStatus(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // s2-connect-wan-endpoint-registry.yml, Status: enum ["public", "testing"], default "public"

        #endregion

        #region (private static) Register(Text)

        private static EndpointStatus Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new EndpointStatus(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a endpoint status.
        /// </summary>
        /// <param name="Text">A text representation of a endpoint status.</param>
        public static EndpointStatus Parse(String Text)
        {

            if (TryParse(Text, out var endpointStatus))
                return endpointStatus;

            throw new ArgumentException($"Invalid text representation of a endpoint status: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a endpoint status.
        /// </summary>
        /// <param name="Text">A text representation of a endpoint status.</param>
        public static EndpointStatus? TryParse(String Text)
        {

            if (TryParse(Text, out var endpointStatus))
                return endpointStatus;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EndpointStatus)

        /// <summary>
        /// Try to parse the given text as a endpoint status. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a endpoint status.</param>
        /// <param name="EndpointStatus">The parsed endpoint status.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out EndpointStatus    EndpointStatus)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out EndpointStatus))
                    EndpointStatus = new EndpointStatus(Text, false);

                return true;

            }

            EndpointStatus = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this endpoint status.
        /// </summary>
        public EndpointStatus Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// public: The endpoint is publicly available.
        /// </summary>
        public static EndpointStatus  Public    { get; }
            = Register("public");

        /// <summary>
        /// testing: The endpoint is available for testing only.
        /// </summary>
        public static EndpointStatus  Testing    { get; }
            = Register("testing");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two endpoint statuses for equality.
        /// </summary>
        public static Boolean operator == (EndpointStatus EndpointStatus1, EndpointStatus EndpointStatus2)
            => EndpointStatus1.Equals(EndpointStatus2);

        /// <summary>
        /// Compares two endpoint statuses for inequality.
        /// </summary>
        public static Boolean operator != (EndpointStatus EndpointStatus1, EndpointStatus EndpointStatus2)
            => !EndpointStatus1.Equals(EndpointStatus2);

        /// <summary>
        /// Compares two endpoint statuses.
        /// </summary>
        public static Boolean operator <  (EndpointStatus EndpointStatus1, EndpointStatus EndpointStatus2)
            => EndpointStatus1.CompareTo(EndpointStatus2) < 0;

        /// <summary>
        /// Compares two endpoint statuses.
        /// </summary>
        public static Boolean operator <= (EndpointStatus EndpointStatus1, EndpointStatus EndpointStatus2)
            => EndpointStatus1.CompareTo(EndpointStatus2) <= 0;

        /// <summary>
        /// Compares two endpoint statuses.
        /// </summary>
        public static Boolean operator >  (EndpointStatus EndpointStatus1, EndpointStatus EndpointStatus2)
            => EndpointStatus1.CompareTo(EndpointStatus2) > 0;

        /// <summary>
        /// Compares two endpoint statuses.
        /// </summary>
        public static Boolean operator >= (EndpointStatus EndpointStatus1, EndpointStatus EndpointStatus2)
            => EndpointStatus1.CompareTo(EndpointStatus2) >= 0;

        #endregion

        #region IComparable<EndpointStatus> Members

        /// <summary>
        /// Compares two endpoint statuses.
        /// </summary>
        /// <param name="Object">A endpoint status to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EndpointStatus endpointStatus
                   ? CompareTo(endpointStatus)
                   : throw new ArgumentException("The given object is not a endpoint status!", nameof(Object));

        /// <summary>
        /// Compares two endpoint statuses.
        /// </summary>
        /// <param name="EndpointStatus">A endpoint status to compare with.</param>
        public Int32 CompareTo(EndpointStatus EndpointStatus)
            => String.Compare(InternalId, EndpointStatus.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<EndpointStatus> Members

        /// <summary>
        /// Compares two endpoint statuses for equality.
        /// </summary>
        /// <param name="Object">A endpoint status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is EndpointStatus endpointStatus && Equals(endpointStatus);

        /// <summary>
        /// Compares two endpoint statuses for equality.
        /// </summary>
        /// <param name="EndpointStatus">A endpoint status to compare with.</param>
        public Boolean Equals(EndpointStatus EndpointStatus)
            => String.Equals(InternalId, EndpointStatus.InternalId, StringComparison.Ordinal);

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

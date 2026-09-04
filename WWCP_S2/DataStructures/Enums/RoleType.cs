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
    /// Extension methods for role types.
    /// </summary>
    public static class RoleTypeExtensions
    {

        /// <summary>
        /// Indicates whether this role type is null or empty.
        /// </summary>
        /// <param name="RoleType">A role type.</param>
        public static Boolean IsNullOrEmpty(this RoleType? RoleType)
            => !RoleType.HasValue || RoleType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this role type is NOT null or empty.
        /// </summary>
        /// <param name="RoleType">A role type.</param>
        public static Boolean IsNotNullOrEmpty(this RoleType? RoleType)
            => RoleType.HasValue && RoleType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The energy role of a Resource Manager for a commodity: producer, consumer or storage.
    /// </summary>
    public readonly struct RoleType : IS2PredefinedString,
                                         IId,
                                         IEquatable<RoleType>,
                                         IComparable<RoleType>
    {

        #region Data

        private readonly static Dictionary<String, RoleType>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this role type is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this role type is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the role type.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All role types defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<RoleType> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new role type based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a role type.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private RoleType(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // RoleType.schema.json
        //   "title": "RoleType",
        //   "type":  "string",
        //   "enum":  ["ENERGY_PRODUCER", "ENERGY_CONSUMER", "ENERGY_STORAGE"],
        //   "description":
        //     ENERGY_PRODUCER: Identifier for RoleType Producer
        //     ENERGY_CONSUMER: Identifier for RoleType Consumer
        //     ENERGY_STORAGE: Identifier for RoleType Storage

        #endregion

        #region (private static) Register(Text)

        private static RoleType Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new RoleType(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a role type.
        /// </summary>
        /// <param name="Text">A text representation of a role type.</param>
        public static RoleType Parse(String Text)
        {

            if (TryParse(Text, out var roleType))
                return roleType;

            throw new ArgumentException($"Invalid text representation of a role type: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a role type.
        /// </summary>
        /// <param name="Text">A text representation of a role type.</param>
        public static RoleType? TryParse(String Text)
        {

            if (TryParse(Text, out var roleType))
                return roleType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out RoleType)

        /// <summary>
        /// Try to parse the given text as a role type. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a role type.</param>
        /// <param name="RoleType">The parsed role type.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out RoleType    RoleType)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out RoleType))
                    RoleType = new RoleType(Text, false);

                return true;

            }

            RoleType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this role type.
        /// </summary>
        public RoleType Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// ENERGY_PRODUCER: Identifier for RoleType Producer
        /// </summary>
        public static RoleType  EnergyProducer    { get; }
            = Register("ENERGY_PRODUCER");

        /// <summary>
        /// ENERGY_CONSUMER: Identifier for RoleType Consumer
        /// </summary>
        public static RoleType  EnergyConsumer    { get; }
            = Register("ENERGY_CONSUMER");

        /// <summary>
        /// ENERGY_STORAGE: Identifier for RoleType Storage
        /// </summary>
        public static RoleType  EnergyStorage    { get; }
            = Register("ENERGY_STORAGE");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two role types for equality.
        /// </summary>
        public static Boolean operator == (RoleType RoleType1, RoleType RoleType2)
            => RoleType1.Equals(RoleType2);

        /// <summary>
        /// Compares two role types for inequality.
        /// </summary>
        public static Boolean operator != (RoleType RoleType1, RoleType RoleType2)
            => !RoleType1.Equals(RoleType2);

        /// <summary>
        /// Compares two role types.
        /// </summary>
        public static Boolean operator <  (RoleType RoleType1, RoleType RoleType2)
            => RoleType1.CompareTo(RoleType2) < 0;

        /// <summary>
        /// Compares two role types.
        /// </summary>
        public static Boolean operator <= (RoleType RoleType1, RoleType RoleType2)
            => RoleType1.CompareTo(RoleType2) <= 0;

        /// <summary>
        /// Compares two role types.
        /// </summary>
        public static Boolean operator >  (RoleType RoleType1, RoleType RoleType2)
            => RoleType1.CompareTo(RoleType2) > 0;

        /// <summary>
        /// Compares two role types.
        /// </summary>
        public static Boolean operator >= (RoleType RoleType1, RoleType RoleType2)
            => RoleType1.CompareTo(RoleType2) >= 0;

        #endregion

        #region IComparable<RoleType> Members

        /// <summary>
        /// Compares two role types.
        /// </summary>
        /// <param name="Object">A role type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is RoleType roleType
                   ? CompareTo(roleType)
                   : throw new ArgumentException("The given object is not a role type!", nameof(Object));

        /// <summary>
        /// Compares two role types.
        /// </summary>
        /// <param name="RoleType">A role type to compare with.</param>
        public Int32 CompareTo(RoleType RoleType)
            => String.Compare(InternalId, RoleType.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<RoleType> Members

        /// <summary>
        /// Compares two role types for equality.
        /// </summary>
        /// <param name="Object">A role type to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is RoleType roleType && Equals(roleType);

        /// <summary>
        /// Compares two role types for equality.
        /// </summary>
        /// <param name="RoleType">A role type to compare with.</param>
        public Boolean Equals(RoleType RoleType)
            => String.Equals(InternalId, RoleType.InternalId, StringComparison.Ordinal);

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

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
    /// Extension methods for energy management roles.
    /// </summary>
    public static class EnergyManagementRoleExtensions
    {

        /// <summary>
        /// Indicates whether this energy management role is null or empty.
        /// </summary>
        /// <param name="EnergyManagementRole">A energy management role.</param>
        public static Boolean IsNullOrEmpty(this EnergyManagementRole? EnergyManagementRole)
            => !EnergyManagementRole.HasValue || EnergyManagementRole.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this energy management role is NOT null or empty.
        /// </summary>
        /// <param name="EnergyManagementRole">A energy management role.</param>
        public static Boolean IsNotNullOrEmpty(this EnergyManagementRole? EnergyManagementRole)
            => EnergyManagementRole.HasValue && EnergyManagementRole.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// An energy management role: Customer Energy Manager (CEM) or Resource Manager (RM).
    /// </summary>
    public readonly struct EnergyManagementRole : IS2PredefinedString,
                                         IId,
                                         IEquatable<EnergyManagementRole>,
                                         IComparable<EnergyManagementRole>
    {

        #region Data

        private readonly static Dictionary<String, EnergyManagementRole>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this energy management role is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this energy management role is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the energy management role.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All energy management roles defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<EnergyManagementRole> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy management role based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a energy management role.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private EnergyManagementRole(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // EnergyManagementRole.schema.json
        //   "title": "EnergyManagementRole",
        //   "type":  "string",
        //   "enum":  ["CEM", "RM"],
        //   "description":
        //     CEM: Customer Energy Manager
        //     RM: Resource Manager

        #endregion

        #region (private static) Register(Text)

        private static EnergyManagementRole Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new EnergyManagementRole(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a energy management role.
        /// </summary>
        /// <param name="Text">A text representation of a energy management role.</param>
        public static EnergyManagementRole Parse(String Text)
        {

            if (TryParse(Text, out var energyManagementRole))
                return energyManagementRole;

            throw new ArgumentException($"Invalid text representation of a energy management role: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a energy management role.
        /// </summary>
        /// <param name="Text">A text representation of a energy management role.</param>
        public static EnergyManagementRole? TryParse(String Text)
        {

            if (TryParse(Text, out var energyManagementRole))
                return energyManagementRole;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EnergyManagementRole)

        /// <summary>
        /// Try to parse the given text as a energy management role. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a energy management role.</param>
        /// <param name="EnergyManagementRole">The parsed energy management role.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out EnergyManagementRole    EnergyManagementRole)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out EnergyManagementRole))
                    EnergyManagementRole = new EnergyManagementRole(Text, false);

                return true;

            }

            EnergyManagementRole = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy management role.
        /// </summary>
        public EnergyManagementRole Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// CEM: Customer Energy Manager
        /// </summary>
        public static EnergyManagementRole  CEM    { get; }
            = Register("CEM");

        /// <summary>
        /// RM: Resource Manager
        /// </summary>
        public static EnergyManagementRole  RM    { get; }
            = Register("RM");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two energy management roles for equality.
        /// </summary>
        public static Boolean operator == (EnergyManagementRole EnergyManagementRole1, EnergyManagementRole EnergyManagementRole2)
            => EnergyManagementRole1.Equals(EnergyManagementRole2);

        /// <summary>
        /// Compares two energy management roles for inequality.
        /// </summary>
        public static Boolean operator != (EnergyManagementRole EnergyManagementRole1, EnergyManagementRole EnergyManagementRole2)
            => !EnergyManagementRole1.Equals(EnergyManagementRole2);

        /// <summary>
        /// Compares two energy management roles.
        /// </summary>
        public static Boolean operator <  (EnergyManagementRole EnergyManagementRole1, EnergyManagementRole EnergyManagementRole2)
            => EnergyManagementRole1.CompareTo(EnergyManagementRole2) < 0;

        /// <summary>
        /// Compares two energy management roles.
        /// </summary>
        public static Boolean operator <= (EnergyManagementRole EnergyManagementRole1, EnergyManagementRole EnergyManagementRole2)
            => EnergyManagementRole1.CompareTo(EnergyManagementRole2) <= 0;

        /// <summary>
        /// Compares two energy management roles.
        /// </summary>
        public static Boolean operator >  (EnergyManagementRole EnergyManagementRole1, EnergyManagementRole EnergyManagementRole2)
            => EnergyManagementRole1.CompareTo(EnergyManagementRole2) > 0;

        /// <summary>
        /// Compares two energy management roles.
        /// </summary>
        public static Boolean operator >= (EnergyManagementRole EnergyManagementRole1, EnergyManagementRole EnergyManagementRole2)
            => EnergyManagementRole1.CompareTo(EnergyManagementRole2) >= 0;

        #endregion

        #region IComparable<EnergyManagementRole> Members

        /// <summary>
        /// Compares two energy management roles.
        /// </summary>
        /// <param name="Object">A energy management role to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyManagementRole energyManagementRole
                   ? CompareTo(energyManagementRole)
                   : throw new ArgumentException("The given object is not a energy management role!", nameof(Object));

        /// <summary>
        /// Compares two energy management roles.
        /// </summary>
        /// <param name="EnergyManagementRole">A energy management role to compare with.</param>
        public Int32 CompareTo(EnergyManagementRole EnergyManagementRole)
            => String.Compare(InternalId, EnergyManagementRole.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<EnergyManagementRole> Members

        /// <summary>
        /// Compares two energy management roles for equality.
        /// </summary>
        /// <param name="Object">A energy management role to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is EnergyManagementRole energyManagementRole && Equals(energyManagementRole);

        /// <summary>
        /// Compares two energy management roles for equality.
        /// </summary>
        /// <param name="EnergyManagementRole">A energy management role to compare with.</param>
        public Boolean Equals(EnergyManagementRole EnergyManagementRole)
            => String.Equals(InternalId, EnergyManagementRole.InternalId, StringComparison.Ordinal);

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

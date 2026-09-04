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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// The role of a Resource Manager for a commodity, e.g. energy consumer of electricity.
    /// </summary>
    public sealed class Role : IEquatable<Role>
    {

        #region Properties

        /// <summary>
        /// The role type of the Resource Manager for the given commodity.
        /// </summary>
        [Mandatory]
        public RoleType   RoleType     { get; }

        /// <summary>
        /// The commodity the role refers to.
        /// </summary>
        [Mandatory]
        public Commodity  Commodity    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new role.
        /// </summary>
        /// <param name="RoleType">The role type of the Resource Manager for the given commodity.</param>
        /// <param name="Commodity">The commodity the role refers to.</param>
        public Role(RoleType   RoleType,
                    Commodity  Commodity)
        {

            if (RoleType.IsNullOrEmpty)
                throw new ArgumentException("The role type of a role must not be null or empty!",
                                            nameof(RoleType));

            if (Commodity.IsNullOrEmpty)
                throw new ArgumentException("The commodity of a role must not be null or empty!",
                                            nameof(Commodity));

            this.RoleType   = RoleType;
            this.Commodity  = Commodity;

            unchecked
            {
                hashCode = this.RoleType. GetHashCode() * 3 ^
                           this.Commodity.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // Role.schema.json
        //   "title": "Role",
        //   "properties": {
        //     "role":      { "$ref": "../schemas/RoleType.schema.json",
        //                    "description": "Role type of the Resource Manager for the given commodity" },
        //     "commodity": { "$ref": "../schemas/Commodity.schema.json",
        //                    "description": "Commodity the role refers to." }
        //   },
        //   "required": ["role", "commodity"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out Role, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a role.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Role">The parsed role.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                           JSON,
                                       [NotNullWhen(true)]  out Role?    Role,
                                       [NotNullWhen(false)] out String?  ErrorResponse)

            => TryParse(JSON,
                        out Role,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a role.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Role">The parsed role.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                           JSON,
                                       [NotNullWhen(true)]  out Role?    Role,
                                       [NotNullWhen(false)] out String?  ErrorResponse,
                                       S2ParserOptions?                  Options)

            => TryParse(JSON,
                        out Role,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a role.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Role">The parsed role.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomRoleParser">A delegate to parse custom roles.</param>
        public static Boolean TryParse(JObject                             JSON,
                                       [NotNullWhen(true)]  out Role?      Role,
                                       [NotNullWhen(false)] out String?    ErrorResponse,
                                       S2ParserOptions?                    Options,
                                       CustomJObjectParserDelegate<Role>?  CustomRoleParser)
        {

            try
            {

                Role = null;

                #region role         [mandatory]

                if (!JSON.ParseMandatoryS2Enum("role",
                                               "role type",
                                               RoleType.TryParse,
                                               Options,
                                               out RoleType roleType,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region commodity    [mandatory]

                if (!JSON.ParseMandatoryS2Enum("commodity",
                                               "commodity",
                                               Commodity.TryParse,
                                               Options,
                                               out Commodity commodity,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "role",
                                                    "commodity"))
                {
                    return false;
                }

                #endregion


                Role = new Role(
                           roleType,
                           commodity
                       );

                if (CustomRoleParser is not null)
                    Role = CustomRoleParser(JSON,
                                            Role);

                return true;

            }
            catch (Exception e)
            {
                Role           = null;
                ErrorResponse  = "The given JSON representation of a role is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomRoleSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomRoleSerializer">A delegate to serialize custom roles.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<Role>? CustomRoleSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("role",       RoleType. ToString()),
                           new JProperty("commodity",  Commodity.ToString())
                       );

            return CustomRoleSerializer is not null
                       ? CustomRoleSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this role.
        /// </summary>
        public Role Clone()

            => new (
                   RoleType. Clone(),
                   Commodity.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two roles for equality.
        /// </summary>
        public static Boolean operator == (Role? Role1, Role? Role2)
        {

            if (ReferenceEquals(Role1, Role2))
                return true;

            if (Role1 is null || Role2 is null)
                return false;

            return Role1.Equals(Role2);

        }

        /// <summary>
        /// Compares two roles for inequality.
        /// </summary>
        public static Boolean operator != (Role? Role1, Role? Role2)
            => !(Role1 == Role2);

        #endregion

        #region IEquatable<Role> Members

        /// <summary>
        /// Compares two roles for equality.
        /// </summary>
        /// <param name="Object">A role to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Role role && Equals(role);

        /// <summary>
        /// Compares two roles for equality.
        /// </summary>
        /// <param name="Role">A role to compare with.</param>
        public Boolean Equals(Role? Role)

            => Role is not null &&

               RoleType. Equals(Role.RoleType) &&
               Commodity.Equals(Role.Commodity);

        #endregion

        #region (override) GetHashCode()

        private readonly Int32 hashCode;

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => hashCode;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"{RoleType} of {Commodity}";

        #endregion

    }

}

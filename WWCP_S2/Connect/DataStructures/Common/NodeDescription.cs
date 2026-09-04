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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// Information about an S2 Connect node (a CEM or RM instance): its identification,
    /// brand, type, model name, optional logo and user-defined name, and its role
    /// (s2-connect-common.yml, NodeDescription). Exchanged during pairing and session initiation.
    /// </summary>
    public sealed class NodeDescription : IEquatable<NodeDescription>
    {

        #region Properties

        /// <summary>
        /// The unique identification of the node.
        /// </summary>
        [Mandatory]
        public Node_Id               Id                 { get; }

        /// <summary>
        /// The brand of the node, e.g. the manufacturer of the device.
        /// </summary>
        [Mandatory]
        public String                Brand              { get; }

        /// <summary>
        /// The type of the node, e.g. "EV charger" or "heat pump".
        /// </summary>
        [Mandatory]
        public String                Type               { get; }

        /// <summary>
        /// The model name of the node.
        /// </summary>
        [Mandatory]
        public String                ModelName          { get; }

        /// <summary>
        /// The role of the node: CEM or RM.
        /// </summary>
        [Mandatory]
        public EnergyManagementRole  Role               { get; }

        /// <summary>
        /// The optional URL of a logo of the node.
        /// </summary>
        [Optional]
        public URL?                  LogoUrl            { get; }

        /// <summary>
        /// The optional name the end user gave to the node.
        /// </summary>
        [Optional]
        public String?               UserDefinedName    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new node description.
        /// </summary>
        /// <param name="Id">The unique identification of the node.</param>
        /// <param name="Brand">The brand of the node.</param>
        /// <param name="Type">The type of the node.</param>
        /// <param name="ModelName">The model name of the node.</param>
        /// <param name="Role">The role of the node: CEM or RM.</param>
        /// <param name="LogoUrl">An optional URL of a logo of the node.</param>
        /// <param name="UserDefinedName">An optional name the end user gave to the node.</param>
        public NodeDescription(Node_Id               Id,
                               String                Brand,
                               String                Type,
                               String                ModelName,
                               EnergyManagementRole  Role,
                               URL?                  LogoUrl           = null,
                               String?               UserDefinedName   = null)
        {

            ArgumentNullException.ThrowIfNull(Brand);
            ArgumentNullException.ThrowIfNull(Type);
            ArgumentNullException.ThrowIfNull(ModelName);

            if (Role != EnergyManagementRole.CEM && Role != EnergyManagementRole.RM)
                throw new ArgumentException($"The role must be CEM or RM, not '{Role}'!", nameof(Role));

            this.Id               = Id;
            this.Brand            = Brand;
            this.Type             = Type;
            this.ModelName        = ModelName;
            this.Role             = Role;
            this.LogoUrl          = LogoUrl;
            this.UserDefinedName  = UserDefinedName;

            unchecked
            {
                hashCode = this.Id.              GetHashCode()                          * 17 ^
                           this.Brand.           GetHashCode(StringComparison.Ordinal)  * 13 ^
                           this.Type.            GetHashCode(StringComparison.Ordinal)  * 11 ^
                           this.ModelName.       GetHashCode(StringComparison.Ordinal)  *  7 ^
                           this.Role.            GetHashCode()                          *  5 ^
                          (this.LogoUrl?.        GetHashCode() ?? 0)                    *  3 ^
                          (this.UserDefinedName?.GetHashCode(StringComparison.Ordinal) ?? 0);
            }

        }

        #endregion


        #region Documentation

        // s2-connect-common.yml
        //   NodeDescription:
        //     required: ["id", "brand", "type", "modelName", "role"]
        //     type: object
        //     description: Information about the node
        //     properties:
        //       id:               { $ref: NodeId }        (string, format uuid)
        //       brand:            { type: string }
        //       logoUrl:          { type: string, format: uri }
        //       type:             { type: string }
        //       modelName:        { type: string }
        //       userDefinedName:  { type: string }
        //       role:             { $ref: Role }          (enum ["CEM", "RM"], the same values as EnergyManagementRole of S2 JSON)

        #endregion

        #region (static) TryParse(JSON, out NodeDescription, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a node description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="NodeDescription">The parsed node description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out NodeDescription?  NodeDescription,
                                       [NotNullWhen(false)] out String?           ErrorResponse)

            => TryParse(JSON,
                        out NodeDescription,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a node description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="NodeDescription">The parsed node description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out NodeDescription?  NodeDescription,
                                       [NotNullWhen(false)] out String?           ErrorResponse,
                                       S2ParserOptions?                           Options)

            => TryParse(JSON,
                        out NodeDescription,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a node description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="NodeDescription">The parsed node description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomNodeDescriptionParser">A delegate to parse custom node descriptions.</param>
        public static Boolean TryParse(JObject                                        JSON,
                                       [NotNullWhen(true)]  out NodeDescription?      NodeDescription,
                                       [NotNullWhen(false)] out String?               ErrorResponse,
                                       S2ParserOptions?                               Options,
                                       CustomJObjectParserDelegate<NodeDescription>?  CustomNodeDescriptionParser)
        {

            try
            {

                NodeDescription = null;

                #region id                 [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
                                             "node identification",
                                             Node_Id.TryParse,
                                             Options,
                                             out Node_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region brand              [mandatory]

                if (!JSON.ParseMandatoryS2String("brand",
                                                 "brand",
                                                 out String? brand,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region logoUrl            [optional]

                if (!JSON.ParseOptionalS2URL("logoUrl",
                                             "logo URL",
                                             out URL? logoUrl,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region type               [mandatory]

                if (!JSON.ParseMandatoryS2String("type",
                                                 "type",
                                                 out String? type,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region modelName          [mandatory]

                if (!JSON.ParseMandatoryS2String("modelName",
                                                 "model name",
                                                 out String? modelName,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region userDefinedName    [optional]

                if (!JSON.ParseOptionalS2String("userDefinedName",
                                                "user defined name",
                                                out String? userDefinedName,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region role               [mandatory]

                if (!JSON.ParseMandatoryS2Enum("role",
                                               "role",
                                               EnergyManagementRole.TryParse,
                                               Options,
                                               out EnergyManagementRole role,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "id",
                                                    "brand",
                                                    "logoUrl",
                                                    "type",
                                                    "modelName",
                                                    "userDefinedName",
                                                    "role"))
                {
                    return false;
                }

                #endregion


                NodeDescription = new NodeDescription(
                                      id,
                                      brand,
                                      type,
                                      modelName,
                                      role,
                                      logoUrl,
                                      userDefinedName
                                  );

                if (CustomNodeDescriptionParser is not null)
                    NodeDescription = CustomNodeDescriptionParser(JSON,
                                                                  NodeDescription);

                return true;

            }
            catch (Exception e)
            {
                NodeDescription  = null;
                ErrorResponse    = "The given JSON representation of a node description is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomNodeDescriptionSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomNodeDescriptionSerializer">A delegate to serialize custom node descriptions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<NodeDescription>? CustomNodeDescriptionSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("id",                Id.ToString()),
                                 new JProperty("brand",             Brand),

                           LogoUrl.HasValue
                               ? new JProperty("logoUrl",           LogoUrl.Value.ToString())
                               : null,

                                 new JProperty("type",              Type),
                                 new JProperty("modelName",         ModelName),

                           UserDefinedName is not null
                               ? new JProperty("userDefinedName",   UserDefinedName)
                               : null,

                                 new JProperty("role",              Role.ToString())

                       );

            return CustomNodeDescriptionSerializer is not null
                       ? CustomNodeDescriptionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this node description.
        /// </summary>
        public NodeDescription Clone()

            => new (
                   Id.Clone(),
                   Brand.    CloneString(),
                   Type.     CloneString(),
                   ModelName.CloneString(),
                   Role.Clone(),
                   LogoUrl?.Clone(),
                   UserDefinedName?.CloneString()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two node descriptions for equality.
        /// </summary>
        public static Boolean operator == (NodeDescription? NodeDescription1, NodeDescription? NodeDescription2)
        {

            if (ReferenceEquals(NodeDescription1, NodeDescription2))
                return true;

            if (NodeDescription1 is null || NodeDescription2 is null)
                return false;

            return NodeDescription1.Equals(NodeDescription2);

        }

        /// <summary>
        /// Compares two node descriptions for inequality.
        /// </summary>
        public static Boolean operator != (NodeDescription? NodeDescription1, NodeDescription? NodeDescription2)
            => !(NodeDescription1 == NodeDescription2);

        #endregion

        #region IEquatable<NodeDescription> Members

        /// <summary>
        /// Compares two node descriptions for equality.
        /// </summary>
        /// <param name="Object">A node description to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is NodeDescription nodeDescription && Equals(nodeDescription);

        /// <summary>
        /// Compares two node descriptions for equality.
        /// </summary>
        /// <param name="NodeDescription">A node description to compare with.</param>
        public Boolean Equals(NodeDescription? NodeDescription)

            => NodeDescription is not null &&

               Id.  Equals(NodeDescription.Id)   &&
               Role.Equals(NodeDescription.Role) &&

               String.Equals(Brand,           NodeDescription.Brand,           StringComparison.Ordinal) &&
               String.Equals(Type,            NodeDescription.Type,            StringComparison.Ordinal) &&
               String.Equals(ModelName,       NodeDescription.ModelName,       StringComparison.Ordinal) &&
               String.Equals(UserDefinedName, NodeDescription.UserDefinedName, StringComparison.Ordinal) &&

               Nullable.Equals(LogoUrl, NodeDescription.LogoUrl);

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

            => String.Concat(

                   $"{Role} node {Id}: {Brand} {ModelName} ({Type})",

                   UserDefinedName is not null
                       ? $" '{UserDefinedName}'"
                       : ""

               );

        #endregion

    }

}

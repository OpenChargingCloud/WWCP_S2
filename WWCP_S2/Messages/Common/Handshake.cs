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
    /// The first message of a plain S2-JSON-over-WebSocket session: each side announces its
    /// role and the RM the protocol versions it supports. Not used in S2 Connect mode, where
    /// pairing and session initiation make it redundant.
    /// </summary>
    public sealed class Handshake : AS2Message,
                                    IEquatable<Handshake>
    {

        #region Data

        /// <summary>
        /// The message type "Handshake".
        /// </summary>
        public const String MessageTypeName = "Handshake";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "Handshake".
        /// </summary>
        public override String                 MessageType
            => MessageTypeName;

        /// <summary>
        /// The role of the sender of this message.
        /// </summary>
        [Mandatory]
        public EnergyManagementRole            Role                         { get; }

        /// <summary>
        /// The protocol versions supported by the sender of this message.
        /// Mandatory for the RM, optional for the CEM.
        /// </summary>
        [Optional]
        public IReadOnlyList<String>?          SupportedProtocolVersions    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new handshake.
        /// </summary>
        /// <param name="Role">The role of the sender of this message.</param>
        /// <param name="SupportedProtocolVersions">The protocol versions supported by the sender (mandatory for the RM, at least one).</param>
        /// <param name="MessageId">An optional message identification.</param>
        public Handshake(EnergyManagementRole   Role,
                         IEnumerable<String>?   SupportedProtocolVersions   = null,
                         Message_Id?            MessageId                   = null)

            : base(MessageId)

        {

            var versions = SupportedProtocolVersions?.ToList();

            if (versions is not null && versions.Count == 0)
                throw new ArgumentException("The list of supported protocol versions must contain at least one item when present!",
                                            nameof(SupportedProtocolVersions));

            if (Role == EnergyManagementRole.RM && versions is null)
                throw new ArgumentException("A Resource Manager must announce its supported protocol versions!",
                                            nameof(SupportedProtocolVersions));

            this.Role                       = Role;
            this.SupportedProtocolVersions  = versions;

            unchecked
            {
                hashCode = this.MessageId.                 GetHashCode()  * 5 ^
                           this.Role.                      GetHashCode()  * 3 ^
                          (this.SupportedProtocolVersions?.CalcHashCode() ?? 0);
            }

        }

        #endregion


        #region Documentation

        // Handshake.schema.json
        //   "title": "Handshake",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "Handshake" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "role":         { "$ref": "../schemas/EnergyManagementRole.schema.json",
        //                       "description": "The role of the sender of this message" },
        //     "supported_protocol_versions": {
        //       "type": "array", "minItems": 1, "items": { "type": "string" },
        //       "description": "Protocol versions supported by the sender of this message.
        //                       This field is mandatory for the RM, but optional for the CEM."
        //     }
        //   },
        //   "required": ["message_type", "message_id", "role"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out Handshake, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a handshake.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Handshake">The parsed handshake.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                              JSON,
                                       [NotNullWhen(true)]  out Handshake?  Handshake,
                                       [NotNullWhen(false)] out String?     ErrorResponse)

            => TryParse(JSON,
                        out Handshake,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a handshake.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Handshake">The parsed handshake.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                              JSON,
                                       [NotNullWhen(true)]  out Handshake?  Handshake,
                                       [NotNullWhen(false)] out String?     ErrorResponse,
                                       S2ParserOptions?                     Options)

            => TryParse(JSON,
                        out Handshake,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a handshake.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Handshake">The parsed handshake.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomHandshakeParser">A delegate to parse custom handshakes.</param>
        public static Boolean TryParse(JObject                                  JSON,
                                       [NotNullWhen(true)]  out Handshake?      Handshake,
                                       [NotNullWhen(false)] out String?         ErrorResponse,
                                       S2ParserOptions?                         Options,
                                       CustomJObjectParserDelegate<Handshake>?  CustomHandshakeParser)
        {

            try
            {

                Handshake = null;

                #region message_type, message_id      [mandatory]

                if (!TryParseHeader(JSON,
                                    MessageTypeName,
                                    Options,
                                    out var messageId,
                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region role                          [mandatory]

                if (!JSON.ParseMandatoryS2Enum("role",
                                               "energy management role",
                                               EnergyManagementRole.TryParse,
                                               Options,
                                               out EnergyManagementRole role,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region supported_protocol_versions   [optional, mandatory for the RM]

                IReadOnlyList<String>? supportedProtocolVersions = null;

                if (JSON.ContainsKey("supported_protocol_versions"))
                {

                    if (!JSON.ParseMandatoryS2Strings("supported_protocol_versions",
                                                      "supported protocol versions",
                                                      1,
                                                      null,
                                                      out supportedProtocolVersions,
                                                      out ErrorResponse))
                    {
                        return false;
                    }

                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "message_type",
                                                    "message_id",
                                                    "role",
                                                    "supported_protocol_versions"))
                {
                    return false;
                }

                #endregion


                Handshake = new Handshake(
                                role,
                                supportedProtocolVersions,
                                messageId
                            );

                if (CustomHandshakeParser is not null)
                    Handshake = CustomHandshakeParser(JSON,
                                                      Handshake);

                return true;

            }
            catch (Exception e)
            {
                Handshake      = null;
                ErrorResponse  = "The given JSON representation of a handshake is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomHandshakeSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomHandshakeSerializer">A delegate to serialize custom handshakes.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<Handshake>? CustomHandshakeSerializer)
        {

            var json = CreateJSON(

                                 new JProperty("role",                         Role.ToString()),

                           SupportedProtocolVersions is not null
                               ? new JProperty("supported_protocol_versions",  new JArray(SupportedProtocolVersions))
                               : null

                       );

            return CustomHandshakeSerializer is not null
                       ? CustomHandshakeSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this handshake.
        /// </summary>
        public Handshake Clone()

            => new (
                   Role.Clone(),
                   SupportedProtocolVersions?.Select(version => version.CloneString()).ToList(),
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two handshakes for equality.
        /// </summary>
        public static Boolean operator == (Handshake? Handshake1, Handshake? Handshake2)
        {

            if (ReferenceEquals(Handshake1, Handshake2))
                return true;

            if (Handshake1 is null || Handshake2 is null)
                return false;

            return Handshake1.Equals(Handshake2);

        }

        /// <summary>
        /// Compares two handshakes for inequality.
        /// </summary>
        public static Boolean operator != (Handshake? Handshake1, Handshake? Handshake2)
            => !(Handshake1 == Handshake2);

        #endregion

        #region IEquatable<Handshake> Members

        /// <summary>
        /// Compares two handshakes for equality.
        /// </summary>
        /// <param name="Object">A handshake to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Handshake handshake && Equals(handshake);

        /// <summary>
        /// Compares two handshakes for equality.
        /// </summary>
        /// <param name="Handshake">A handshake to compare with.</param>
        public Boolean Equals(Handshake? Handshake)

            => Handshake is not null &&

               MessageId.Equals(Handshake.MessageId) &&
               Role.     Equals(Handshake.Role)      &&

             ((SupportedProtocolVersions is     null && Handshake.SupportedProtocolVersions is     null) ||
              (SupportedProtocolVersions is not null && Handshake.SupportedProtocolVersions is not null &&
               SupportedProtocolVersions.SequenceEqual(Handshake.SupportedProtocolVersions, StringComparer.Ordinal)));

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

                   $"Handshake of {Role}",

                   SupportedProtocolVersions is not null
                       ? $" supporting {String.Join(", ", SupportedProtocolVersions)}"
                       : "",

                   $" [{MessageId}]"

               );

        #endregion

    }

}

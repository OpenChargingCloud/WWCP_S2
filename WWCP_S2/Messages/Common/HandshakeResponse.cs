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
    /// The answer of the CEM to the Handshake of the Resource Manager: the protocol
    /// version the CEM selected for this session out of the versions the RM announced.
    /// </summary>
    public sealed class HandshakeResponse : AS2Message,
                                            IEquatable<HandshakeResponse>
    {

        #region Data

        /// <summary>
        /// The message type "HandshakeResponse".
        /// </summary>
        public const String MessageTypeName = "HandshakeResponse";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "HandshakeResponse".
        /// </summary>
        public override String  MessageType
            => MessageTypeName;

        /// <summary>
        /// The protocol version the CEM selected for this session.
        /// </summary>
        [Mandatory]
        public String           SelectedProtocolVersion    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new handshake response.
        /// </summary>
        /// <param name="SelectedProtocolVersion">The protocol version the CEM selected for this session.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public HandshakeResponse(String       SelectedProtocolVersion,
                                 Message_Id?  MessageId   = null)

            : base(MessageId)

        {

            // The schema declares selected_protocol_version as a plain string without
            // minLength, so an empty string is valid; only null is rejected.
            ArgumentNullException.ThrowIfNull(SelectedProtocolVersion);

            this.SelectedProtocolVersion = SelectedProtocolVersion;

            unchecked
            {
                hashCode = this.MessageId.              GetHashCode() * 3 ^
                           this.SelectedProtocolVersion.GetHashCode(StringComparison.Ordinal);
            }

        }

        #endregion


        #region Documentation

        // HandshakeResponse.schema.json
        //   "title": "HandshakeResponse",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "HandshakeResponse" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "selected_protocol_version": {
        //       "type": "string",
        //       "description": "The protocol version the CEM selected for this session"
        //     }
        //   },
        //   "required": ["message_type", "message_id", "selected_protocol_version"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out HandshakeResponse, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a handshake response.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="HandshakeResponse">The parsed handshake response.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out HandshakeResponse?  HandshakeResponse,
                                       [NotNullWhen(false)] out String?             ErrorResponse)

            => TryParse(JSON,
                        out HandshakeResponse,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a handshake response.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="HandshakeResponse">The parsed handshake response.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out HandshakeResponse?  HandshakeResponse,
                                       [NotNullWhen(false)] out String?             ErrorResponse,
                                       S2ParserOptions?                             Options)

            => TryParse(JSON,
                        out HandshakeResponse,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a handshake response.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="HandshakeResponse">The parsed handshake response.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomHandshakeResponseParser">A delegate to parse custom handshake responses.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out HandshakeResponse?      HandshakeResponse,
                                       [NotNullWhen(false)] out String?                 ErrorResponse,
                                       S2ParserOptions?                                 Options,
                                       CustomJObjectParserDelegate<HandshakeResponse>?  CustomHandshakeResponseParser)
        {

            try
            {

                HandshakeResponse = null;

                #region message_type, message_id    [mandatory]

                if (!TryParseHeader(JSON,
                                    MessageTypeName,
                                    Options,
                                    out var messageId,
                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region selected_protocol_version   [mandatory]

                if (!JSON.ParseMandatoryS2String("selected_protocol_version",
                                                 "selected protocol version",
                                                 out var selectedProtocolVersion,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "message_type",
                                                    "message_id",
                                                    "selected_protocol_version"))
                {
                    return false;
                }

                #endregion


                HandshakeResponse = new HandshakeResponse(
                                        selectedProtocolVersion,
                                        messageId
                                    );

                if (CustomHandshakeResponseParser is not null)
                    HandshakeResponse = CustomHandshakeResponseParser(JSON,
                                                                      HandshakeResponse);

                return true;

            }
            catch (Exception e)
            {
                HandshakeResponse  = null;
                ErrorResponse      = "The given JSON representation of a handshake response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomHandshakeResponseSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomHandshakeResponseSerializer">A delegate to serialize custom handshake responses.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<HandshakeResponse>? CustomHandshakeResponseSerializer)
        {

            var json = CreateJSON(
                           new JProperty("selected_protocol_version",  SelectedProtocolVersion)
                       );

            return CustomHandshakeResponseSerializer is not null
                       ? CustomHandshakeResponseSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this handshake response.
        /// </summary>
        public HandshakeResponse Clone()

            => new (
                   SelectedProtocolVersion.CloneString(),
                   MessageId.              Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two handshake responses for equality.
        /// </summary>
        public static Boolean operator == (HandshakeResponse? HandshakeResponse1, HandshakeResponse? HandshakeResponse2)
        {

            if (ReferenceEquals(HandshakeResponse1, HandshakeResponse2))
                return true;

            if (HandshakeResponse1 is null || HandshakeResponse2 is null)
                return false;

            return HandshakeResponse1.Equals(HandshakeResponse2);

        }

        /// <summary>
        /// Compares two handshake responses for inequality.
        /// </summary>
        public static Boolean operator != (HandshakeResponse? HandshakeResponse1, HandshakeResponse? HandshakeResponse2)
            => !(HandshakeResponse1 == HandshakeResponse2);

        #endregion

        #region IEquatable<HandshakeResponse> Members

        /// <summary>
        /// Compares two handshake responses for equality.
        /// </summary>
        /// <param name="Object">A handshake response to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is HandshakeResponse handshakeResponse && Equals(handshakeResponse);

        /// <summary>
        /// Compares two handshake responses for equality.
        /// </summary>
        /// <param name="HandshakeResponse">A handshake response to compare with.</param>
        public Boolean Equals(HandshakeResponse? HandshakeResponse)

            => HandshakeResponse is not null &&

               MessageId.Equals(HandshakeResponse.MessageId) &&

               String.Equals(SelectedProtocolVersion, HandshakeResponse.SelectedProtocolVersion, StringComparison.Ordinal);

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
            => $"HandshakeResponse selecting {SelectedProtocolVersion} [{MessageId}]";

        #endregion

    }

}

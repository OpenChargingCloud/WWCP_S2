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
    /// The CEM activates (or deactivates with NO_SELECTION) a control type offered by the
    /// Resource Manager in its ResourceManagerDetails.
    /// </summary>
    public sealed class SelectControlType : AS2Message,
                                            IEquatable<SelectControlType>
    {

        #region Data

        /// <summary>
        /// The message type "SelectControlType".
        /// </summary>
        public const String MessageTypeName = "SelectControlType";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "SelectControlType".
        /// </summary>
        public override String  MessageType
            => MessageTypeName;

        /// <summary>
        /// The control type to activate. Must be one of the available control types as
        /// defined in the ResourceManagerDetails (validated by the session layer).
        /// </summary>
        [Mandatory]
        public ControlType      ControlType    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new select control type message.
        /// </summary>
        /// <param name="ControlType">The control type to activate.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public SelectControlType(ControlType  ControlType,
                                 Message_Id?  MessageId   = null)

            : base(MessageId)

        {

            this.ControlType = ControlType;

            unchecked
            {
                hashCode = this.MessageId.  GetHashCode() * 3 ^
                           this.ControlType.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // SelectControlType.schema.json
        //   "title": "SelectControlType",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "SelectControlType" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "control_type": { "$ref": "../schemas/ControlType.schema.json",
        //                       "description": "The ControlType to activate. Must be one of the available ControlTypes
        //                                       as defined in the ResourceManagerDetails" }
        //   },
        //   "required": ["message_type", "message_id", "control_type"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out SelectControlType, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a select control type message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SelectControlType">The parsed message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out SelectControlType?  SelectControlType,
                                       [NotNullWhen(false)] out String?             ErrorResponse)

            => TryParse(JSON,
                        out SelectControlType,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a select control type message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SelectControlType">The parsed message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out SelectControlType?  SelectControlType,
                                       [NotNullWhen(false)] out String?             ErrorResponse,
                                       S2ParserOptions?                             Options)

            => TryParse(JSON,
                        out SelectControlType,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a select control type message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SelectControlType">The parsed message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomSelectControlTypeParser">A delegate to parse custom select control type messages.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out SelectControlType?      SelectControlType,
                                       [NotNullWhen(false)] out String?                 ErrorResponse,
                                       S2ParserOptions?                                 Options,
                                       CustomJObjectParserDelegate<SelectControlType>?  CustomSelectControlTypeParser)
        {

            try
            {

                SelectControlType = null;

                #region message_type, message_id   [mandatory]

                if (!TryParseHeader(JSON,
                                    MessageTypeName,
                                    Options,
                                    out var messageId,
                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region control_type               [mandatory]

                if (!JSON.ParseMandatoryS2Enum("control_type",
                                               "control type",
                                               ControlType.TryParse,
                                               Options,
                                               out ControlType controlType,
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
                                                    "control_type"))
                {
                    return false;
                }

                #endregion


                SelectControlType = new SelectControlType(
                                        controlType,
                                        messageId
                                    );

                if (CustomSelectControlTypeParser is not null)
                    SelectControlType = CustomSelectControlTypeParser(JSON,
                                                                      SelectControlType);

                return true;

            }
            catch (Exception e)
            {
                SelectControlType  = null;
                ErrorResponse      = "The given JSON representation of a select control type message is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomSelectControlTypeSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomSelectControlTypeSerializer">A delegate to serialize custom select control type messages.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<SelectControlType>? CustomSelectControlTypeSerializer)
        {

            var json = CreateJSON(
                           new JProperty("control_type",  ControlType.ToString())
                       );

            return CustomSelectControlTypeSerializer is not null
                       ? CustomSelectControlTypeSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this message.
        /// </summary>
        public SelectControlType Clone()

            => new (
                   ControlType.Clone(),
                   MessageId.  Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two select control type messages for equality.
        /// </summary>
        public static Boolean operator == (SelectControlType? SelectControlType1, SelectControlType? SelectControlType2)
        {

            if (ReferenceEquals(SelectControlType1, SelectControlType2))
                return true;

            if (SelectControlType1 is null || SelectControlType2 is null)
                return false;

            return SelectControlType1.Equals(SelectControlType2);

        }

        /// <summary>
        /// Compares two select control type messages for inequality.
        /// </summary>
        public static Boolean operator != (SelectControlType? SelectControlType1, SelectControlType? SelectControlType2)
            => !(SelectControlType1 == SelectControlType2);

        #endregion

        #region IEquatable<SelectControlType> Members

        /// <summary>
        /// Compares two select control type messages for equality.
        /// </summary>
        /// <param name="Object">A select control type message to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is SelectControlType selectControlType && Equals(selectControlType);

        /// <summary>
        /// Compares two select control type messages for equality.
        /// </summary>
        /// <param name="SelectControlType">A select control type message to compare with.</param>
        public Boolean Equals(SelectControlType? SelectControlType)

            => SelectControlType is not null &&

               MessageId.  Equals(SelectControlType.MessageId) &&
               ControlType.Equals(SelectControlType.ControlType);

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
            => $"SelectControlType {ControlType} [{MessageId}]";

        #endregion

    }

}

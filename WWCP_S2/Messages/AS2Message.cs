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
    /// The abstract base of all S2 messages that carry a "message_id".
    /// </summary>
    public abstract class AS2Message : IS2MessageWithId
    {

        #region Properties

        /// <summary>
        /// The message type as used in the "message_type" property, e.g. "FRBC.SystemDescription".
        /// </summary>
        public abstract String      MessageType    { get; }

        /// <summary>
        /// The unique identification of this message.
        /// </summary>
        [Mandatory]
        public          Message_Id  MessageId      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new S2 message.
        /// </summary>
        /// <param name="MessageId">An optional message identification; a new time-ordered UUID when omitted.</param>
        protected AS2Message(Message_Id? MessageId)
        {
            this.MessageId = MessageId ?? Message_Id.NewRandom;
        }

        #endregion


        #region (protected static) TryParseHeader(JSON, ExpectedMessageType, Options, out MessageId, out ErrorResponse)

        /// <summary>
        /// Parse the "message_type" and "message_id" properties every message with an
        /// identification starts with.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ExpectedMessageType">The message type this parser expects.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="MessageId">The parsed message identification.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        protected static Boolean TryParseHeader(JObject                           JSON,
                                                String                            ExpectedMessageType,
                                                S2ParserOptions?                  Options,
                                                out Message_Id                    MessageId,
                                                [NotNullWhen(false)] out String?  ErrorResponse)
        {

            MessageId = default;

            if (!JSON.ParseMandatoryS2String("message_type",
                                             "message type",
                                             out var messageType,
                                             out ErrorResponse))
            {
                return false;
            }

            if (!String.Equals(messageType, ExpectedMessageType, StringComparison.Ordinal))
            {
                ErrorResponse = $"Unexpected message type '{messageType}', expected '{ExpectedMessageType}'!";
                return false;
            }

            return JSON.ParseMandatoryS2Id("message_id",
                                           "message identification",
                                           Message_Id.TryParse,
                                           Options,
                                           out MessageId,
                                           out ErrorResponse);

        }

        #endregion

        #region (protected) CreateJSON(Properties)

        /// <summary>
        /// Create the JSON representation of this message: "message_type", "message_id"
        /// and the given properties in order; null entries are skipped.
        /// </summary>
        /// <param name="Properties">The message-specific properties in schema order.</param>
        protected JObject CreateJSON(params ReadOnlySpan<JProperty?> Properties)
        {

            var json = new JObject {
                           new JProperty("message_type",  MessageType),
                           new JProperty("message_id",    MessageId.ToString())
                       };

            foreach (var property in Properties)
            {
                if (property is not null)
                    json.Add(property);
            }

            return json;

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public abstract JObject ToJSON();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"{MessageType} [{MessageId}]";

        #endregion

    }

}

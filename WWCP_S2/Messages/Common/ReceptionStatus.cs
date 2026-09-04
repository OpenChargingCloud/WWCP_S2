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
    /// The acknowledgement of a received message. Every message that carries a message_id is
    /// answered with exactly one ReceptionStatus; a ReceptionStatus itself has no message_id
    /// and is never acknowledged.
    /// </summary>
    public sealed class ReceptionStatus : IS2Message,
                                          IEquatable<ReceptionStatus>
    {

        #region Data

        /// <summary>
        /// The message type "ReceptionStatus".
        /// </summary>
        public const String MessageTypeName = "ReceptionStatus";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "ReceptionStatus".
        /// </summary>
        public String                MessageType
            => MessageTypeName;

        /// <summary>
        /// The message this ReceptionStatus refers to (the null UUID when it could not be determined).
        /// </summary>
        [Mandatory]
        public Message_Id            SubjectMessageId    { get; }

        /// <summary>
        /// The reception status value.
        /// </summary>
        [Mandatory]
        public ReceptionStatusValue  Status              { get; }

        /// <summary>
        /// An optional diagnostic label that can be used to provide additional information
        /// for debugging. However, not for HMI purposes.
        /// </summary>
        [Optional]
        public String?               DiagnosticLabel     { get; }

        /// <summary>
        /// Whether the status is OK.
        /// </summary>
        public Boolean               IsOK
            => Status == ReceptionStatusValue.OK;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new reception status.
        /// </summary>
        /// <param name="SubjectMessageId">The message this ReceptionStatus refers to.</param>
        /// <param name="Status">The reception status value.</param>
        /// <param name="DiagnosticLabel">An optional diagnostic label for debugging.</param>
        public ReceptionStatus(Message_Id            SubjectMessageId,
                               ReceptionStatusValue  Status,
                               String?               DiagnosticLabel   = null)
        {

            this.SubjectMessageId  = SubjectMessageId;
            this.Status            = Status;
            this.DiagnosticLabel   = DiagnosticLabel;

            unchecked
            {
                hashCode = this.SubjectMessageId.GetHashCode() * 5 ^
                           this.Status.          GetHashCode() * 3 ^
                          (this.DiagnosticLabel?.GetHashCode(StringComparison.Ordinal) ?? 0);
            }

        }

        #endregion


        #region Documentation

        // ReceptionStatus.schema.json
        //   "title": "ReceptionStatus",
        //   "properties": {
        //     "message_type":       { "type": "string", "const": "ReceptionStatus" },
        //     "subject_message_id": { "$ref": "../schemas/ID.schema.json",
        //                             "description": "The message this ReceptionStatus refers to" },
        //     "status":             { "$ref": "../schemas/ReceptionStatusValues.schema.json",
        //                             "description": "Enumeration of status values" },
        //     "diagnostic_label":   { "type": "string",
        //                             "description": "Diagnostic label that can be used to provide additional information
        //                                             for debugging. However, not for HMI purposes." }
        //   },
        //   "required": ["message_type", "subject_message_id", "status"],
        //   "additionalProperties": false

        #endregion

        #region (static) OK(SubjectMessageId)

        /// <summary>
        /// Create a ReceptionStatus OK for the given message.
        /// </summary>
        /// <param name="SubjectMessageId">The acknowledged message.</param>
        public static ReceptionStatus OK(Message_Id SubjectMessageId)
            => new (SubjectMessageId, ReceptionStatusValue.OK);

        #endregion

        #region (static) TryParse(JSON, out ReceptionStatus, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a reception status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ReceptionStatus">The parsed reception status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out ReceptionStatus?  ReceptionStatus,
                                       [NotNullWhen(false)] out String?           ErrorResponse)

            => TryParse(JSON,
                        out ReceptionStatus,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a reception status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ReceptionStatus">The parsed reception status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out ReceptionStatus?  ReceptionStatus,
                                       [NotNullWhen(false)] out String?           ErrorResponse,
                                       S2ParserOptions?                           Options)

            => TryParse(JSON,
                        out ReceptionStatus,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a reception status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ReceptionStatus">The parsed reception status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomReceptionStatusParser">A delegate to parse custom reception statuses.</param>
        public static Boolean TryParse(JObject                                        JSON,
                                       [NotNullWhen(true)]  out ReceptionStatus?      ReceptionStatus,
                                       [NotNullWhen(false)] out String?               ErrorResponse,
                                       S2ParserOptions?                               Options,
                                       CustomJObjectParserDelegate<ReceptionStatus>?  CustomReceptionStatusParser)
        {

            try
            {

                ReceptionStatus = null;

                #region message_type          [mandatory]

                if (!JSON.ParseMandatoryS2String("message_type",
                                                 "message type",
                                                 out var messageType,
                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!String.Equals(messageType, MessageTypeName, StringComparison.Ordinal))
                {
                    ErrorResponse = $"Unexpected message type '{messageType}', expected '{MessageTypeName}'!";
                    return false;
                }

                #endregion

                #region subject_message_id    [mandatory]

                if (!JSON.ParseMandatoryS2Id("subject_message_id",
                                             "subject message identification",
                                             Message_Id.TryParse,
                                             Options,
                                             out Message_Id subjectMessageId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region status                [mandatory]

                if (!JSON.ParseMandatoryS2Enum("status",
                                               "reception status value",
                                               ReceptionStatusValue.TryParse,
                                               Options,
                                               out ReceptionStatusValue status,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region diagnostic_label      [optional]

                if (!JSON.ParseOptionalS2String("diagnostic_label",
                                                "diagnostic label",
                                                out String? diagnosticLabel,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "message_type",
                                                    "subject_message_id",
                                                    "status",
                                                    "diagnostic_label"))
                {
                    return false;
                }

                #endregion


                ReceptionStatus = new ReceptionStatus(
                                      subjectMessageId,
                                      status,
                                      diagnosticLabel
                                  );

                if (CustomReceptionStatusParser is not null)
                    ReceptionStatus = CustomReceptionStatusParser(JSON,
                                                                  ReceptionStatus);

                return true;

            }
            catch (Exception e)
            {
                ReceptionStatus  = null;
                ErrorResponse    = "The given JSON representation of a reception status is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomReceptionStatusSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomReceptionStatusSerializer">A delegate to serialize custom reception statuses.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ReceptionStatus>? CustomReceptionStatusSerializer)
        {

            var json = JSONObject.Create(

                                 new JProperty("message_type",        MessageTypeName),
                                 new JProperty("subject_message_id",  SubjectMessageId.ToString()),
                                 new JProperty("status",              Status.          ToString()),

                           DiagnosticLabel is not null
                               ? new JProperty("diagnostic_label",    DiagnosticLabel)
                               : null

                       );

            return CustomReceptionStatusSerializer is not null
                       ? CustomReceptionStatusSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this reception status.
        /// </summary>
        public ReceptionStatus Clone()

            => new (
                   SubjectMessageId.Clone(),
                   Status.          Clone(),
                   DiagnosticLabel?.CloneString()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two reception statuses for equality.
        /// </summary>
        public static Boolean operator == (ReceptionStatus? ReceptionStatus1, ReceptionStatus? ReceptionStatus2)
        {

            if (ReferenceEquals(ReceptionStatus1, ReceptionStatus2))
                return true;

            if (ReceptionStatus1 is null || ReceptionStatus2 is null)
                return false;

            return ReceptionStatus1.Equals(ReceptionStatus2);

        }

        /// <summary>
        /// Compares two reception statuses for inequality.
        /// </summary>
        public static Boolean operator != (ReceptionStatus? ReceptionStatus1, ReceptionStatus? ReceptionStatus2)
            => !(ReceptionStatus1 == ReceptionStatus2);

        #endregion

        #region IEquatable<ReceptionStatus> Members

        /// <summary>
        /// Compares two reception statuses for equality.
        /// </summary>
        /// <param name="Object">A reception status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is ReceptionStatus receptionStatus && Equals(receptionStatus);

        /// <summary>
        /// Compares two reception statuses for equality.
        /// </summary>
        /// <param name="ReceptionStatus">A reception status to compare with.</param>
        public Boolean Equals(ReceptionStatus? ReceptionStatus)

            => ReceptionStatus is not null &&

               SubjectMessageId.Equals(ReceptionStatus.SubjectMessageId) &&
               Status.          Equals(ReceptionStatus.Status)           &&

               String.Equals(DiagnosticLabel, ReceptionStatus.DiagnosticLabel, StringComparison.Ordinal);

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

                   $"ReceptionStatus {Status} for {SubjectMessageId}",

                   DiagnosticLabel is not null
                       ? $" ({DiagnosticLabel})"
                       : ""

               );

        #endregion

    }

}

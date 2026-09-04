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
    /// The status of a timer of an Operation Mode Based Control (OMBC) system sent by the
    /// Resource Manager: when the timer will be (or was) finished.
    /// </summary>
    public sealed class OMBC_TimerStatus : AS2Message,
                                           IEquatable<OMBC_TimerStatus>
    {

        #region Data

        /// <summary>
        /// The message type "OMBC.TimerStatus".
        /// </summary>
        public const String MessageTypeName = "OMBC.TimerStatus";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "OMBC.TimerStatus".
        /// </summary>
        public override String  MessageType
            => MessageTypeName;

        /// <summary>
        /// The identification of the timer this message refers to.
        /// </summary>
        [Mandatory]
        public Timer_Id         TimerId       { get; }

        /// <summary>
        /// Indicates when the timer will be finished. If the timestamp is in the future, the timer
        /// is not yet finished. If the timestamp is in the past, the timer is finished. If the timer
        /// was never started, the value can be an arbitrary timestamp in the past.
        /// </summary>
        [Mandatory]
        public DateTimeOffset   FinishedAt    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new OMBC timer status.
        /// </summary>
        /// <param name="TimerId">The identification of the timer this message refers to.</param>
        /// <param name="FinishedAt">When the timer will be (or was) finished.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public OMBC_TimerStatus(Timer_Id        TimerId,
                                DateTimeOffset  FinishedAt,
                                Message_Id?     MessageId   = null)

            : base(MessageId)

        {

            if (TimerId.IsNullOrEmpty)
                throw new ArgumentException("The timer identification of an OMBC timer status must not be null or empty!",
                                            nameof(TimerId));

            this.TimerId     = TimerId;
            this.FinishedAt  = FinishedAt;

            unchecked
            {
                hashCode = this.MessageId. GetHashCode() * 5 ^
                           this.TimerId.   GetHashCode() * 3 ^
                           this.FinishedAt.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // OMBC.TimerStatus.schema.json
        //   "title": "OMBC_TimerStatus",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "OMBC.TimerStatus" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "timer_id":     { "$ref": "../schemas/ID.schema.json",
        //                       "description": "The ID of the timer this message refers to" },
        //     "finished_at":  { "type": "string", "format": "date-time",
        //                       "description": "Indicates when the Timer will be finished. If the DateTimeStamp is in the future,
        //                                       the timer is not yet finished. If the DateTimeStamp is in the past, the timer is
        //                                       finished. If the timer was never started, the value can be an arbitrary
        //                                       DateTimeStamp in the past." }
        //   },
        //   "required": ["message_type", "message_id", "timer_id", "finished_at"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out OMBC_TimerStatus, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an OMBC timer status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCTimerStatus">The parsed OMBC timer status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out OMBC_TimerStatus?  OMBCTimerStatus,
                                       [NotNullWhen(false)] out String?            ErrorResponse)

            => TryParse(JSON,
                        out OMBCTimerStatus,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an OMBC timer status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCTimerStatus">The parsed OMBC timer status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out OMBC_TimerStatus?  OMBCTimerStatus,
                                       [NotNullWhen(false)] out String?            ErrorResponse,
                                       S2ParserOptions?                            Options)

            => TryParse(JSON,
                        out OMBCTimerStatus,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an OMBC timer status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCTimerStatus">The parsed OMBC timer status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomOMBCTimerStatusParser">A delegate to parse custom OMBC timer statuses.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out OMBC_TimerStatus?      OMBCTimerStatus,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       S2ParserOptions?                                Options,
                                       CustomJObjectParserDelegate<OMBC_TimerStatus>?  CustomOMBCTimerStatusParser)
        {

            try
            {

                OMBCTimerStatus = null;

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

                #region timer_id                   [mandatory]

                if (!JSON.ParseMandatoryS2Id("timer_id",
                                             "timer identification",
                                             Timer_Id.TryParse,
                                             Options,
                                             out Timer_Id timerId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region finished_at                [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("finished_at",
                                                    "finished at timestamp",
                                                    Options,
                                                    out DateTimeOffset finishedAt,
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
                                                    "timer_id",
                                                    "finished_at"))
                {
                    return false;
                }

                #endregion


                OMBCTimerStatus = new OMBC_TimerStatus(
                                      timerId,
                                      finishedAt,
                                      messageId
                                  );

                if (CustomOMBCTimerStatusParser is not null)
                    OMBCTimerStatus = CustomOMBCTimerStatusParser(JSON,
                                                                  OMBCTimerStatus);

                return true;

            }
            catch (Exception e)
            {
                OMBCTimerStatus  = null;
                ErrorResponse    = "The given JSON representation of an OMBC timer status is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomOMBCTimerStatusSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomOMBCTimerStatusSerializer">A delegate to serialize custom OMBC timer statuses.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<OMBC_TimerStatus>? CustomOMBCTimerStatusSerializer)
        {

            var json = CreateJSON(

                           new JProperty("timer_id",     TimerId.   ToString()),
                           new JProperty("finished_at",  FinishedAt.ToS2Timestamp())

                       );

            return CustomOMBCTimerStatusSerializer is not null
                       ? CustomOMBCTimerStatusSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this OMBC timer status.
        /// </summary>
        public OMBC_TimerStatus Clone()

            => new (
                   TimerId.  Clone(),
                   FinishedAt,
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two OMBC timer statuses for equality.
        /// </summary>
        public static Boolean operator == (OMBC_TimerStatus? OMBCTimerStatus1, OMBC_TimerStatus? OMBCTimerStatus2)
        {

            if (ReferenceEquals(OMBCTimerStatus1, OMBCTimerStatus2))
                return true;

            if (OMBCTimerStatus1 is null || OMBCTimerStatus2 is null)
                return false;

            return OMBCTimerStatus1.Equals(OMBCTimerStatus2);

        }

        /// <summary>
        /// Compares two OMBC timer statuses for inequality.
        /// </summary>
        public static Boolean operator != (OMBC_TimerStatus? OMBCTimerStatus1, OMBC_TimerStatus? OMBCTimerStatus2)
            => !(OMBCTimerStatus1 == OMBCTimerStatus2);

        #endregion

        #region IEquatable<OMBC_TimerStatus> Members

        /// <summary>
        /// Compares two OMBC timer statuses for equality.
        /// </summary>
        /// <param name="Object">An OMBC timer status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is OMBC_TimerStatus ombcTimerStatus && Equals(ombcTimerStatus);

        /// <summary>
        /// Compares two OMBC timer statuses for equality.
        /// </summary>
        /// <param name="OMBCTimerStatus">An OMBC timer status to compare with.</param>
        public Boolean Equals(OMBC_TimerStatus? OMBCTimerStatus)

            => OMBCTimerStatus is not null &&

               MessageId. Equals(OMBCTimerStatus.MessageId) &&
               TimerId.   Equals(OMBCTimerStatus.TimerId)   &&
               FinishedAt.Equals(OMBCTimerStatus.FinishedAt);

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
            => $"OMBC.TimerStatus: timer {TimerId} finished at {FinishedAt.ToS2Timestamp()} [{MessageId}]";

        #endregion

    }

}

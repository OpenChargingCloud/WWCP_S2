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
    /// The Resource Manager reports when a timer of a DDBC actuator will be (or was) finished.
    /// </summary>
    public sealed class DDBC_TimerStatus : AS2Message,
                                           IEquatable<DDBC_TimerStatus>
    {

        #region Data

        /// <summary>
        /// The message type "DDBC.TimerStatus".
        /// </summary>
        public const String MessageTypeName = "DDBC.TimerStatus";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "DDBC.TimerStatus".
        /// </summary>
        public override String  MessageType
            => MessageTypeName;

        /// <summary>
        /// The identification of the timer this message refers to.
        /// </summary>
        [Mandatory]
        public Timer_Id         TimerId       { get; }

        /// <summary>
        /// The identification of the actuator the timer belongs to.
        /// </summary>
        [Mandatory]
        public Actuator_Id      ActuatorId    { get; }

        /// <summary>
        /// When the timer will be finished. If the timestamp is in the future, the timer is not
        /// yet finished. If the timestamp is in the past, the timer is finished. If the timer was
        /// never started, the value can be an arbitrary timestamp in the past.
        /// </summary>
        [Mandatory]
        public DateTimeOffset   FinishedAt    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new DDBC timer status.
        /// </summary>
        /// <param name="TimerId">The identification of the timer this message refers to.</param>
        /// <param name="ActuatorId">The identification of the actuator the timer belongs to.</param>
        /// <param name="FinishedAt">When the timer will be (or was) finished.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public DDBC_TimerStatus(Timer_Id        TimerId,
                                Actuator_Id     ActuatorId,
                                DateTimeOffset  FinishedAt,
                                Message_Id?     MessageId   = null)

            : base(MessageId)

        {

            this.TimerId     = TimerId;
            this.ActuatorId  = ActuatorId;
            this.FinishedAt  = FinishedAt;

            unchecked
            {
                hashCode = this.MessageId. GetHashCode() * 7 ^
                           this.TimerId.   GetHashCode() * 5 ^
                           this.ActuatorId.GetHashCode() * 3 ^
                           this.FinishedAt.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // DDBC.TimerStatus.schema.json
        //   "title": "DDBC_TimerStatus",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "DDBC.TimerStatus" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "timer_id":     { "$ref": "../schemas/ID.schema.json",
        //                       "description": "The ID of the timer this message refers to" },
        //     "actuator_id":  { "$ref": "../schemas/ID.schema.json",
        //                       "description": "The ID of the actuator the timer belongs to" },
        //     "finished_at":  { "type": "string", "format": "date-time",
        //                       "description": "Indicates when the Timer will be finished. If the DateTimeStamp is in the
        //                                       future, the timer is not yet finished. If the DateTimeStamp is in the past,
        //                                       the timer is finished. If the timer was never started, the value can be an
        //                                       arbitrary DateTimeStamp in the past." }
        //   },
        //   "required": ["message_type", "message_id", "timer_id", "actuator_id", "finished_at"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out DDBCTimerStatus, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a DDBC timer status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCTimerStatus">The parsed DDBC timer status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out DDBC_TimerStatus?  DDBCTimerStatus,
                                       [NotNullWhen(false)] out String?            ErrorResponse)

            => TryParse(JSON,
                        out DDBCTimerStatus,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC timer status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCTimerStatus">The parsed DDBC timer status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out DDBC_TimerStatus?  DDBCTimerStatus,
                                       [NotNullWhen(false)] out String?            ErrorResponse,
                                       S2ParserOptions?                            Options)

            => TryParse(JSON,
                        out DDBCTimerStatus,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC timer status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCTimerStatus">The parsed DDBC timer status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomDDBCTimerStatusParser">A delegate to parse custom DDBC timer statuses.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out DDBC_TimerStatus?      DDBCTimerStatus,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       S2ParserOptions?                                Options,
                                       CustomJObjectParserDelegate<DDBC_TimerStatus>?  CustomDDBCTimerStatusParser)
        {

            try
            {

                DDBCTimerStatus = null;

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

                #region actuator_id                [mandatory]

                if (!JSON.ParseMandatoryS2Id("actuator_id",
                                             "actuator identification",
                                             Actuator_Id.TryParse,
                                             Options,
                                             out Actuator_Id actuatorId,
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
                                                    "actuator_id",
                                                    "finished_at"))
                {
                    return false;
                }

                #endregion


                DDBCTimerStatus = new DDBC_TimerStatus(
                                      timerId,
                                      actuatorId,
                                      finishedAt,
                                      messageId
                                  );

                if (CustomDDBCTimerStatusParser is not null)
                    DDBCTimerStatus = CustomDDBCTimerStatusParser(JSON,
                                                                  DDBCTimerStatus);

                return true;

            }
            catch (Exception e)
            {
                DDBCTimerStatus  = null;
                ErrorResponse    = "The given JSON representation of a DDBC timer status is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDDBCTimerStatusSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomDDBCTimerStatusSerializer">A delegate to serialize custom DDBC timer statuses.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DDBC_TimerStatus>? CustomDDBCTimerStatusSerializer)
        {

            var json = CreateJSON(
                           new JProperty("timer_id",     TimerId.   ToString()),
                           new JProperty("actuator_id",  ActuatorId.ToString()),
                           new JProperty("finished_at",  FinishedAt.ToS2Timestamp())
                       );

            return CustomDDBCTimerStatusSerializer is not null
                       ? CustomDDBCTimerStatusSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this DDBC timer status.
        /// </summary>
        public DDBC_TimerStatus Clone()

            => new (
                   TimerId.   Clone(),
                   ActuatorId.Clone(),
                   FinishedAt,
                   MessageId. Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two DDBC timer statuses for equality.
        /// </summary>
        public static Boolean operator == (DDBC_TimerStatus? DDBCTimerStatus1, DDBC_TimerStatus? DDBCTimerStatus2)
        {

            if (ReferenceEquals(DDBCTimerStatus1, DDBCTimerStatus2))
                return true;

            if (DDBCTimerStatus1 is null || DDBCTimerStatus2 is null)
                return false;

            return DDBCTimerStatus1.Equals(DDBCTimerStatus2);

        }

        /// <summary>
        /// Compares two DDBC timer statuses for inequality.
        /// </summary>
        public static Boolean operator != (DDBC_TimerStatus? DDBCTimerStatus1, DDBC_TimerStatus? DDBCTimerStatus2)
            => !(DDBCTimerStatus1 == DDBCTimerStatus2);

        #endregion

        #region IEquatable<DDBC_TimerStatus> Members

        /// <summary>
        /// Compares two DDBC timer statuses for equality.
        /// </summary>
        /// <param name="Object">A DDBC timer status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is DDBC_TimerStatus ddbcTimerStatus && Equals(ddbcTimerStatus);

        /// <summary>
        /// Compares two DDBC timer statuses for equality.
        /// </summary>
        /// <param name="DDBCTimerStatus">A DDBC timer status to compare with.</param>
        public Boolean Equals(DDBC_TimerStatus? DDBCTimerStatus)

            => DDBCTimerStatus is not null &&

               MessageId. Equals(DDBCTimerStatus.MessageId)  &&
               TimerId.   Equals(DDBCTimerStatus.TimerId)    &&
               ActuatorId.Equals(DDBCTimerStatus.ActuatorId) &&
               FinishedAt.Equals(DDBCTimerStatus.FinishedAt);

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
            => $"DDBC.TimerStatus of timer {TimerId} of actuator {ActuatorId} finished at {FinishedAt.ToS2Timestamp()} [{MessageId}]";

        #endregion

    }

}

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
    /// The Resource Manager informs the CEM when a timer of an FRBC actuator will be
    /// (or was) finished.
    /// </summary>
    public sealed class FRBC_TimerStatus : AS2Message,
                                           IEquatable<FRBC_TimerStatus>
    {

        #region Data

        /// <summary>
        /// The message type "FRBC.TimerStatus".
        /// </summary>
        public const String MessageTypeName = "FRBC.TimerStatus";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "FRBC.TimerStatus".
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
        /// Indicates when the timer will be finished. If the timestamp is in the future,
        /// the timer is not yet finished. If the timestamp is in the past, the timer is
        /// finished. If the timer was never started, the value can be an arbitrary
        /// timestamp in the past.
        /// </summary>
        [Mandatory]
        public DateTimeOffset   FinishedAt    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new FRBC timer status.
        /// </summary>
        /// <param name="TimerId">The identification of the timer this message refers to.</param>
        /// <param name="ActuatorId">The identification of the actuator the timer belongs to.</param>
        /// <param name="FinishedAt">When the timer will be (or was) finished.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public FRBC_TimerStatus(Timer_Id        TimerId,
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

        // FRBC.TimerStatus.schema.json
        //   "title": "FRBC_TimerStatus",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "FRBC.TimerStatus" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "timer_id":     { "$ref": "../schemas/ID.schema.json",
        //                       "description": "The ID of the timer this message refers to" },
        //     "actuator_id":  { "$ref": "../schemas/ID.schema.json",
        //                       "description": "The ID of the actuator the timer belongs to" },
        //     "finished_at":  { "type": "string", "format": "date-time",
        //                       "description": "Indicates when the Timer will be finished. If the DateTimeStamp is in the future,
        //                                       the timer is not yet finished. If the DateTimeStamp is in the past, the timer is
        //                                       finished. If the timer was never started, the value can be an arbitrary
        //                                       DateTimeStamp in the past." }
        //   },
        //   "required": ["message_type", "message_id", "timer_id", "actuator_id", "finished_at"],
        //   "additionalProperties": false
        //
        // Cross-message rules (session layer): timer_id and actuator_id refer to a timer and an
        // actuator declared in the active FRBC.SystemDescription.

        #endregion

        #region (static) TryParse(JSON, out FRBC_TimerStatus, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an FRBC timer status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="TimerStatus">The parsed FRBC timer status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out FRBC_TimerStatus?  TimerStatus,
                                       [NotNullWhen(false)] out String?            ErrorResponse)

            => TryParse(JSON,
                        out TimerStatus,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC timer status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="TimerStatus">The parsed FRBC timer status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out FRBC_TimerStatus?  TimerStatus,
                                       [NotNullWhen(false)] out String?            ErrorResponse,
                                       S2ParserOptions?                            Options)

            => TryParse(JSON,
                        out TimerStatus,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC timer status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="TimerStatus">The parsed FRBC timer status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomTimerStatusParser">A delegate to parse custom FRBC timer statuses.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out FRBC_TimerStatus?      TimerStatus,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       S2ParserOptions?                                Options,
                                       CustomJObjectParserDelegate<FRBC_TimerStatus>?  CustomTimerStatusParser)
        {

            try
            {

                TimerStatus = null;

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


                TimerStatus = new FRBC_TimerStatus(
                                  timerId,
                                  actuatorId,
                                  finishedAt,
                                  messageId
                              );

                if (CustomTimerStatusParser is not null)
                    TimerStatus = CustomTimerStatusParser(JSON,
                                                          TimerStatus);

                return true;

            }
            catch (Exception e)
            {
                TimerStatus    = null;
                ErrorResponse  = "The given JSON representation of an FRBC timer status is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomTimerStatusSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomTimerStatusSerializer">A delegate to serialize custom FRBC timer statuses.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_TimerStatus>? CustomTimerStatusSerializer)
        {

            var json = CreateJSON(
                           new JProperty("timer_id",     TimerId.   ToString()),
                           new JProperty("actuator_id",  ActuatorId.ToString()),
                           new JProperty("finished_at",  FinishedAt.ToS2Timestamp())
                       );

            return CustomTimerStatusSerializer is not null
                       ? CustomTimerStatusSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this FRBC timer status.
        /// </summary>
        public FRBC_TimerStatus Clone()

            => new (
                   TimerId.   Clone(),
                   ActuatorId.Clone(),
                   FinishedAt,
                   MessageId. Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two FRBC timer statuses for equality.
        /// </summary>
        public static Boolean operator == (FRBC_TimerStatus? TimerStatus1, FRBC_TimerStatus? TimerStatus2)
        {

            if (ReferenceEquals(TimerStatus1, TimerStatus2))
                return true;

            if (TimerStatus1 is null || TimerStatus2 is null)
                return false;

            return TimerStatus1.Equals(TimerStatus2);

        }

        /// <summary>
        /// Compares two FRBC timer statuses for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_TimerStatus? TimerStatus1, FRBC_TimerStatus? TimerStatus2)
            => !(TimerStatus1 == TimerStatus2);

        #endregion

        #region IEquatable<FRBC_TimerStatus> Members

        /// <summary>
        /// Compares two FRBC timer statuses for equality.
        /// </summary>
        /// <param name="Object">An FRBC timer status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_TimerStatus timerStatus && Equals(timerStatus);

        /// <summary>
        /// Compares two FRBC timer statuses for equality.
        /// </summary>
        /// <param name="TimerStatus">An FRBC timer status to compare with.</param>
        public Boolean Equals(FRBC_TimerStatus? TimerStatus)

            => TimerStatus is not null &&

               MessageId. Equals(TimerStatus.MessageId)  &&
               TimerId.   Equals(TimerStatus.TimerId)    &&
               ActuatorId.Equals(TimerStatus.ActuatorId) &&
               FinishedAt.Equals(TimerStatus.FinishedAt);

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
            => $"FRBC.TimerStatus of timer '{TimerId}' of actuator '{ActuatorId}' finished at {FinishedAt.ToS2Timestamp()} [{MessageId}]";

        #endregion

    }

}

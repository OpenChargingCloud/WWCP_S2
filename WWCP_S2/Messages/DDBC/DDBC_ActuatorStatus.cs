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
    /// The Resource Manager reports the presently active operation mode of a DDBC actuator,
    /// its operation mode factor and, unless this is the first operation mode it is aware of,
    /// the previous operation mode and the moment the transition was initiated.
    /// </summary>
    public sealed class DDBC_ActuatorStatus : AS2Message,
                                              IEquatable<DDBC_ActuatorStatus>
    {

        #region Data

        /// <summary>
        /// The message type "DDBC.ActuatorStatus".
        /// </summary>
        public const String MessageTypeName = "DDBC.ActuatorStatus";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "DDBC.ActuatorStatus".
        /// </summary>
        public override String    MessageType
            => MessageTypeName;

        /// <summary>
        /// The identification of the actuator this message refers to.
        /// </summary>
        [Mandatory]
        public Actuator_Id        ActuatorId                 { get; }

        /// <summary>
        /// The operation mode that is presently active for this actuator.
        /// </summary>
        [Mandatory]
        public OperationMode_Id   ActiveOperationModeId      { get; }

        /// <summary>
        /// The factor with which the DDBC.OperationMode is configured.
        /// The factor is greater than or equal to 0 and less than or equal to 1.
        /// </summary>
        [Mandatory]
        public Double             OperationModeFactor        { get; }

        /// <summary>
        /// The identification of the DDBC.OperationMode that was active before the present one.
        /// This value shall always be provided, unless the active DDBC.OperationMode is the first
        /// DDBC.OperationMode the Resource Manager is aware of.
        /// </summary>
        [Optional]
        public OperationMode_Id?  PreviousOperationModeId    { get; }

        /// <summary>
        /// The time at which the transition from the previous DDBC.OperationMode to the active
        /// DDBC.OperationMode was initiated. This value shall always be provided, unless the active
        /// DDBC.OperationMode is the first DDBC.OperationMode the Resource Manager is aware of.
        /// </summary>
        [Optional]
        public DateTimeOffset?    TransitionTimestamp        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new DDBC actuator status.
        /// </summary>
        /// <param name="ActuatorId">The identification of the actuator this message refers to.</param>
        /// <param name="ActiveOperationModeId">The operation mode that is presently active for this actuator.</param>
        /// <param name="OperationModeFactor">The factor with which the operation mode is configured (0..1).</param>
        /// <param name="PreviousOperationModeId">The optional operation mode that was active before the present one.</param>
        /// <param name="TransitionTimestamp">The optional time at which the transition to the active operation mode was initiated.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public DDBC_ActuatorStatus(Actuator_Id        ActuatorId,
                                   OperationMode_Id   ActiveOperationModeId,
                                   Double             OperationModeFactor,
                                   OperationMode_Id?  PreviousOperationModeId   = null,
                                   DateTimeOffset?    TransitionTimestamp       = null,
                                   Message_Id?        MessageId                 = null)

            : base(MessageId)

        {

            if (Double.IsNaN(OperationModeFactor) || OperationModeFactor < 0 || OperationModeFactor > 1)
                throw new ArgumentException("The operation mode factor must be greater than or equal to 0 and less than or equal to 1!",
                                            nameof(OperationModeFactor));

            this.ActuatorId               = ActuatorId;
            this.ActiveOperationModeId    = ActiveOperationModeId;
            this.OperationModeFactor      = OperationModeFactor;
            this.PreviousOperationModeId  = PreviousOperationModeId;
            this.TransitionTimestamp      = TransitionTimestamp;

            unchecked
            {
                hashCode = this.MessageId.               GetHashCode()       * 13 ^
                           this.ActuatorId.              GetHashCode()       * 11 ^
                           this.ActiveOperationModeId.   GetHashCode()       *  7 ^
                           this.OperationModeFactor.     GetHashCode()       *  5 ^
                          (this.PreviousOperationModeId?.GetHashCode() ?? 0) *  3 ^
                          (this.TransitionTimestamp?.    GetHashCode() ?? 0);
            }

        }

        #endregion


        #region Documentation

        // DDBC.ActuatorStatus.schema.json
        //   "title": "DDBC_ActuatorStatus",
        //   "properties": {
        //     "message_type":               { "type": "string", "const": "DDBC.ActuatorStatus" },
        //     "message_id":                 { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "actuator_id":                { "$ref": "../schemas/ID.schema.json",
        //                                     "description": "ID of the actuator this messages refers to" },
        //     "active_operation_mode_id":   { "$ref": "../schemas/ID.schema.json",
        //                                     "description": "The operation mode that is presently active for this actuator." },
        //     "operation_mode_factor":      { "type": "number",
        //                                     "description": "The number indicates the factor with which the DDBC.OperationMode is
        //                                                     configured. The factor should be greater than or equal to 0 and less
        //                                                     or equal to 1." },
        //     "previous_operation_mode_id": { "$ref": "../schemas/ID.schema.json",
        //                                     "description": "ID of the DDBC,OperationMode that was active before the present one.
        //                                                     This value shall always be provided, unless the active DDBC.OperationMode
        //                                                     is the first DDBC.OperationMode the Resource Manager is aware of." },
        //     "transition_timestamp":       { "type": "string", "format": "date-time",
        //                                     "description": "Time at which the transition from the previous DDBC.OperationMode to
        //                                                     the active DDBC.OperationMode was initiated. This value shall always be
        //                                                     provided, unless the active DDBC.OperationMode is the first
        //                                                     DDBC.OperationMode the Resource Manager is aware of." }
        //   },
        //   "required": ["message_type", "message_id", "actuator_id", "active_operation_mode_id", "operation_mode_factor"],
        //   "additionalProperties": false
        //
        // Semantic rules (CONVENTIONS.md §8):
        //   - operation_mode_factor ∈ [0, 1].

        #endregion

        #region (static) TryParse(JSON, out DDBCActuatorStatus, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a DDBC actuator status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCActuatorStatus">The parsed DDBC actuator status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                        JSON,
                                       [NotNullWhen(true)]  out DDBC_ActuatorStatus?  DDBCActuatorStatus,
                                       [NotNullWhen(false)] out String?               ErrorResponse)

            => TryParse(JSON,
                        out DDBCActuatorStatus,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC actuator status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCActuatorStatus">The parsed DDBC actuator status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                        JSON,
                                       [NotNullWhen(true)]  out DDBC_ActuatorStatus?  DDBCActuatorStatus,
                                       [NotNullWhen(false)] out String?               ErrorResponse,
                                       S2ParserOptions?                               Options)

            => TryParse(JSON,
                        out DDBCActuatorStatus,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC actuator status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCActuatorStatus">The parsed DDBC actuator status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomDDBCActuatorStatusParser">A delegate to parse custom DDBC actuator statuses.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out DDBC_ActuatorStatus?      DDBCActuatorStatus,
                                       [NotNullWhen(false)] out String?                   ErrorResponse,
                                       S2ParserOptions?                                   Options,
                                       CustomJObjectParserDelegate<DDBC_ActuatorStatus>?  CustomDDBCActuatorStatusParser)
        {

            try
            {

                DDBCActuatorStatus = null;

                #region message_type, message_id       [mandatory]

                if (!TryParseHeader(JSON,
                                    MessageTypeName,
                                    Options,
                                    out var messageId,
                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region actuator_id                    [mandatory]

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

                #region active_operation_mode_id       [mandatory]

                if (!JSON.ParseMandatoryS2Id("active_operation_mode_id",
                                             "active operation mode identification",
                                             OperationMode_Id.TryParse,
                                             Options,
                                             out OperationMode_Id activeOperationModeId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region operation_mode_factor          [mandatory]

                if (!JSON.ParseMandatoryS2Number("operation_mode_factor",
                                                 "operation mode factor",
                                                 out Double operationModeFactor,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region previous_operation_mode_id     [optional]

                if (!JSON.ParseOptionalS2Id("previous_operation_mode_id",
                                            "previous operation mode identification",
                                            OperationMode_Id.TryParse,
                                            Options,
                                            out OperationMode_Id? previousOperationModeId,
                                            out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region transition_timestamp           [optional]

                if (!JSON.ParseOptionalS2Timestamp("transition_timestamp",
                                                   "transition timestamp",
                                                   Options,
                                                   out DateTimeOffset? transitionTimestamp,
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
                                                    "actuator_id",
                                                    "active_operation_mode_id",
                                                    "operation_mode_factor",
                                                    "previous_operation_mode_id",
                                                    "transition_timestamp"))
                {
                    return false;
                }

                #endregion


                DDBCActuatorStatus = new DDBC_ActuatorStatus(
                                         actuatorId,
                                         activeOperationModeId,
                                         operationModeFactor,
                                         previousOperationModeId,
                                         transitionTimestamp,
                                         messageId
                                     );

                if (CustomDDBCActuatorStatusParser is not null)
                    DDBCActuatorStatus = CustomDDBCActuatorStatusParser(JSON,
                                                                        DDBCActuatorStatus);

                return true;

            }
            catch (Exception e)
            {
                DDBCActuatorStatus  = null;
                ErrorResponse       = "The given JSON representation of a DDBC actuator status is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDDBCActuatorStatusSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomDDBCActuatorStatusSerializer">A delegate to serialize custom DDBC actuator statuses.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DDBC_ActuatorStatus>? CustomDDBCActuatorStatusSerializer)
        {

            var json = CreateJSON(

                                 new JProperty("actuator_id",                 ActuatorId.           ToString()),
                                 new JProperty("active_operation_mode_id",    ActiveOperationModeId.ToString()),
                                 new JProperty("operation_mode_factor",       OperationModeFactor),

                           PreviousOperationModeId.HasValue
                               ? new JProperty("previous_operation_mode_id",  PreviousOperationModeId.Value.ToString())
                               : null,

                           TransitionTimestamp.HasValue
                               ? new JProperty("transition_timestamp",        TransitionTimestamp.Value.ToS2Timestamp())
                               : null

                       );

            return CustomDDBCActuatorStatusSerializer is not null
                       ? CustomDDBCActuatorStatusSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this DDBC actuator status.
        /// </summary>
        public DDBC_ActuatorStatus Clone()

            => new (
                   ActuatorId.           Clone(),
                   ActiveOperationModeId.Clone(),
                   OperationModeFactor,
                   PreviousOperationModeId?.Clone(),
                   TransitionTimestamp,
                   MessageId.            Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two DDBC actuator statuses for equality.
        /// </summary>
        public static Boolean operator == (DDBC_ActuatorStatus? DDBCActuatorStatus1, DDBC_ActuatorStatus? DDBCActuatorStatus2)
        {

            if (ReferenceEquals(DDBCActuatorStatus1, DDBCActuatorStatus2))
                return true;

            if (DDBCActuatorStatus1 is null || DDBCActuatorStatus2 is null)
                return false;

            return DDBCActuatorStatus1.Equals(DDBCActuatorStatus2);

        }

        /// <summary>
        /// Compares two DDBC actuator statuses for inequality.
        /// </summary>
        public static Boolean operator != (DDBC_ActuatorStatus? DDBCActuatorStatus1, DDBC_ActuatorStatus? DDBCActuatorStatus2)
            => !(DDBCActuatorStatus1 == DDBCActuatorStatus2);

        #endregion

        #region IEquatable<DDBC_ActuatorStatus> Members

        /// <summary>
        /// Compares two DDBC actuator statuses for equality.
        /// </summary>
        /// <param name="Object">A DDBC actuator status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is DDBC_ActuatorStatus ddbcActuatorStatus && Equals(ddbcActuatorStatus);

        /// <summary>
        /// Compares two DDBC actuator statuses for equality.
        /// </summary>
        /// <param name="DDBCActuatorStatus">A DDBC actuator status to compare with.</param>
        public Boolean Equals(DDBC_ActuatorStatus? DDBCActuatorStatus)

            => DDBCActuatorStatus is not null &&

               MessageId.            Equals(DDBCActuatorStatus.MessageId)             &&
               ActuatorId.           Equals(DDBCActuatorStatus.ActuatorId)            &&
               ActiveOperationModeId.Equals(DDBCActuatorStatus.ActiveOperationModeId) &&
               OperationModeFactor.  Equals(DDBCActuatorStatus.OperationModeFactor)   &&

               Nullable.Equals(PreviousOperationModeId, DDBCActuatorStatus.PreviousOperationModeId) &&
               Nullable.Equals(TransitionTimestamp,     DDBCActuatorStatus.TransitionTimestamp);

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

                   $"DDBC.ActuatorStatus of actuator {ActuatorId}: operation mode {ActiveOperationModeId} with factor {OperationModeFactor}",

                   PreviousOperationModeId.HasValue
                       ? $", previously {PreviousOperationModeId.Value}"
                       : "",

                   TransitionTimestamp.HasValue
                       ? $" since {TransitionTimestamp.Value.ToS2Timestamp()}"
                       : "",

                   $" [{MessageId}]"

               );

        #endregion

    }

}

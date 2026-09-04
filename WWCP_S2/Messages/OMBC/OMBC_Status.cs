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
    /// The status of an Operation Mode Based Control (OMBC) system sent by the Resource Manager:
    /// the active operation mode, its factor, and the operation mode it transitioned from.
    /// </summary>
    public sealed class OMBC_Status : AS2Message,
                                      IEquatable<OMBC_Status>
    {

        #region Data

        /// <summary>
        /// The message type "OMBC.Status".
        /// </summary>
        public const String MessageTypeName = "OMBC.Status";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "OMBC.Status".
        /// </summary>
        public override String    MessageType
            => MessageTypeName;

        /// <summary>
        /// The identification of the active OMBC.OperationMode.
        /// </summary>
        [Mandatory]
        public OperationMode_Id   ActiveOperationModeId      { get; }

        /// <summary>
        /// The factor with which the OMBC.OperationMode is configured.
        /// The factor is greater than or equal to 0 and less than or equal to 1.
        /// </summary>
        [Mandatory]
        public Double             OperationModeFactor        { get; }

        /// <summary>
        /// The identification of the OMBC.OperationMode that was previously active. This value shall
        /// always be provided, unless the active OMBC.OperationMode is the first OMBC.OperationMode
        /// the Resource Manager is aware of.
        /// </summary>
        [Optional]
        public OperationMode_Id?  PreviousOperationModeId    { get; }

        /// <summary>
        /// The time at which the transition from the previous OMBC.OperationMode to the active
        /// OMBC.OperationMode was initiated. This value shall always be provided, unless the active
        /// OMBC.OperationMode is the first OMBC.OperationMode the Resource Manager is aware of.
        /// </summary>
        [Optional]
        public DateTimeOffset?    TransitionTimestamp        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new OMBC status.
        /// </summary>
        /// <param name="ActiveOperationModeId">The identification of the active OMBC.OperationMode.</param>
        /// <param name="OperationModeFactor">The factor with which the OMBC.OperationMode is configured (0..1).</param>
        /// <param name="PreviousOperationModeId">The optional identification of the previously active OMBC.OperationMode.</param>
        /// <param name="TransitionTimestamp">The optional time at which the transition to the active OMBC.OperationMode was initiated.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public OMBC_Status(OperationMode_Id   ActiveOperationModeId,
                           Double             OperationModeFactor,
                           OperationMode_Id?  PreviousOperationModeId   = null,
                           DateTimeOffset?    TransitionTimestamp       = null,
                           Message_Id?        MessageId                 = null)

            : base(MessageId)

        {

            if (ActiveOperationModeId.IsNullOrEmpty)
                throw new ArgumentException("The active operation mode identification of an OMBC status must not be null or empty!",
                                            nameof(ActiveOperationModeId));

            // The negated form also rejects NaN.
            if (!(OperationModeFactor >= 0 && OperationModeFactor <= 1))
                throw new ArgumentException($"The operation mode factor of an OMBC status must be within [0, 1], but {OperationModeFactor} was given!",
                                            nameof(OperationModeFactor));

            this.ActiveOperationModeId    = ActiveOperationModeId;
            this.OperationModeFactor      = OperationModeFactor;
            this.PreviousOperationModeId  = PreviousOperationModeId;
            this.TransitionTimestamp      = TransitionTimestamp;

            unchecked
            {
                hashCode = this.MessageId.               GetHashCode()       * 11 ^
                           this.ActiveOperationModeId.   GetHashCode()       *  7 ^
                           this.OperationModeFactor.     GetHashCode()       *  5 ^
                          (this.PreviousOperationModeId?.GetHashCode() ?? 0) *  3 ^
                          (this.TransitionTimestamp?.    GetHashCode() ?? 0);
            }

        }

        #endregion


        #region Documentation

        // OMBC.Status.schema.json
        //   "title": "OMBC_Status",
        //   "properties": {
        //     "message_type":               { "type": "string", "const": "OMBC.Status" },
        //     "message_id":                 { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "active_operation_mode_id":   { "$ref": "../schemas/ID.schema.json",
        //                                     "description": "ID of the active OMBC.OperationMode." },
        //     "operation_mode_factor":      { "type": "number",
        //                                     "description": "The number indicates the factor with which the OMBC.OperationMode should be
        //                                                     configured. The factor should be greater than or equal than 0 and less or equal to 1." },
        //     "previous_operation_mode_id": { "$ref": "../schemas/ID.schema.json",
        //                                     "description": "ID of the OMBC.OperationMode that was previously active. This value shall always
        //                                                     be provided, unless the active OMBC.OperationMode is the first OMBC.OperationMode
        //                                                     the Resource Manager is aware of." },
        //     "transition_timestamp":       { "type": "string", "format": "date-time",
        //                                     "description": "Time at which the transition from the previous OMBC.OperationMode to the active
        //                                                     OMBC.OperationMode was initiated. This value shall always be provided, unless the
        //                                                     active OMBC.OperationMode is the first OMBC.OperationMode the Resource Manager is aware of." }
        //   },
        //   "required": ["message_type", "message_id", "active_operation_mode_id", "operation_mode_factor"],
        //   "additionalProperties": false
        //
        // Semantic rule (CONVENTIONS.md §8): operation_mode_factor ∈ [0, 1].

        #endregion

        #region (static) TryParse(JSON, out OMBC_Status, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an OMBC status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCStatus">The parsed OMBC status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                JSON,
                                       [NotNullWhen(true)]  out OMBC_Status?  OMBCStatus,
                                       [NotNullWhen(false)] out String?       ErrorResponse)

            => TryParse(JSON,
                        out OMBCStatus,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an OMBC status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCStatus">The parsed OMBC status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                JSON,
                                       [NotNullWhen(true)]  out OMBC_Status?  OMBCStatus,
                                       [NotNullWhen(false)] out String?       ErrorResponse,
                                       S2ParserOptions?                       Options)

            => TryParse(JSON,
                        out OMBCStatus,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an OMBC status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCStatus">The parsed OMBC status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomOMBCStatusParser">A delegate to parse custom OMBC statuses.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out OMBC_Status?      OMBCStatus,
                                       [NotNullWhen(false)] out String?           ErrorResponse,
                                       S2ParserOptions?                           Options,
                                       CustomJObjectParserDelegate<OMBC_Status>?  CustomOMBCStatusParser)
        {

            try
            {

                OMBCStatus = null;

                #region message_type, message_id     [mandatory]

                if (!TryParseHeader(JSON,
                                    MessageTypeName,
                                    Options,
                                    out var messageId,
                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region active_operation_mode_id     [mandatory]

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

                #region operation_mode_factor        [mandatory]

                if (!JSON.ParseMandatoryS2Number("operation_mode_factor",
                                                 "operation mode factor",
                                                 out Double operationModeFactor,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region previous_operation_mode_id   [optional]

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

                #region transition_timestamp         [optional]

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
                                                    "active_operation_mode_id",
                                                    "operation_mode_factor",
                                                    "previous_operation_mode_id",
                                                    "transition_timestamp"))
                {
                    return false;
                }

                #endregion


                OMBCStatus = new OMBC_Status(
                                 activeOperationModeId,
                                 operationModeFactor,
                                 previousOperationModeId,
                                 transitionTimestamp,
                                 messageId
                             );

                if (CustomOMBCStatusParser is not null)
                    OMBCStatus = CustomOMBCStatusParser(JSON,
                                                        OMBCStatus);

                return true;

            }
            catch (Exception e)
            {
                OMBCStatus     = null;
                ErrorResponse  = "The given JSON representation of an OMBC status is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomOMBCStatusSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomOMBCStatusSerializer">A delegate to serialize custom OMBC statuses.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<OMBC_Status>? CustomOMBCStatusSerializer)
        {

            var json = CreateJSON(

                                 new JProperty("active_operation_mode_id",    ActiveOperationModeId.ToString()),
                                 new JProperty("operation_mode_factor",       OperationModeFactor),

                           PreviousOperationModeId.HasValue
                               ? new JProperty("previous_operation_mode_id",  PreviousOperationModeId.Value.ToString())
                               : null,

                           TransitionTimestamp.HasValue
                               ? new JProperty("transition_timestamp",        TransitionTimestamp.Value.ToS2Timestamp())
                               : null

                       );

            return CustomOMBCStatusSerializer is not null
                       ? CustomOMBCStatusSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this OMBC status.
        /// </summary>
        public OMBC_Status Clone()

            => new (
                   ActiveOperationModeId.   Clone(),
                   OperationModeFactor,
                   PreviousOperationModeId?.Clone(),
                   TransitionTimestamp,
                   MessageId.               Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two OMBC statuses for equality.
        /// </summary>
        public static Boolean operator == (OMBC_Status? OMBCStatus1, OMBC_Status? OMBCStatus2)
        {

            if (ReferenceEquals(OMBCStatus1, OMBCStatus2))
                return true;

            if (OMBCStatus1 is null || OMBCStatus2 is null)
                return false;

            return OMBCStatus1.Equals(OMBCStatus2);

        }

        /// <summary>
        /// Compares two OMBC statuses for inequality.
        /// </summary>
        public static Boolean operator != (OMBC_Status? OMBCStatus1, OMBC_Status? OMBCStatus2)
            => !(OMBCStatus1 == OMBCStatus2);

        #endregion

        #region IEquatable<OMBC_Status> Members

        /// <summary>
        /// Compares two OMBC statuses for equality.
        /// </summary>
        /// <param name="Object">An OMBC status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is OMBC_Status ombcStatus && Equals(ombcStatus);

        /// <summary>
        /// Compares two OMBC statuses for equality.
        /// </summary>
        /// <param name="OMBCStatus">An OMBC status to compare with.</param>
        public Boolean Equals(OMBC_Status? OMBCStatus)

            => OMBCStatus is not null &&

               MessageId.            Equals(OMBCStatus.MessageId)             &&
               ActiveOperationModeId.Equals(OMBCStatus.ActiveOperationModeId) &&
               OperationModeFactor.  Equals(OMBCStatus.OperationModeFactor)   &&

               Nullable.Equals(PreviousOperationModeId, OMBCStatus.PreviousOperationModeId) &&
               Nullable.Equals(TransitionTimestamp,     OMBCStatus.TransitionTimestamp);

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

                   $"OMBC.Status: {ActiveOperationModeId} at factor {OperationModeFactor}",

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

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
    /// The CEM instructs a DDBC actuator to switch to an operation mode with a given
    /// operation mode factor at the given execution time.
    /// </summary>
    public sealed class DDBC_Instruction : AS2Message,
                                           IInstruction,
                                           IEquatable<DDBC_Instruction>
    {

        #region Data

        /// <summary>
        /// The message type "DDBC.Instruction".
        /// </summary>
        public const String MessageTypeName = "DDBC.Instruction";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "DDBC.Instruction".
        /// </summary>
        public override String   MessageType
            => MessageTypeName;

        /// <summary>
        /// The identification of this DDBC.Instruction. Must be unique in the scope of the
        /// Resource Manager, for at least the duration of the session between Resource Manager and CEM.
        /// </summary>
        [Mandatory]
        public Instruction_Id    Id                     { get; }

        /// <summary>
        /// The moment the execution of the instruction shall start. When the specified execution
        /// time is in the past, execution must start as soon as possible.
        /// </summary>
        [Mandatory]
        public DateTimeOffset    ExecutionTime          { get; }

        /// <summary>
        /// Whether this is an instruction during an abnormal condition.
        /// </summary>
        [Mandatory]
        public Boolean           AbnormalCondition      { get; }

        /// <summary>
        /// The identification of the actuator this instruction belongs to.
        /// </summary>
        [Mandatory]
        public Actuator_Id       ActuatorId             { get; }

        /// <summary>
        /// The identification of the DDBC.OperationMode.
        /// </summary>
        [Mandatory]
        public OperationMode_Id  OperationModeId        { get; }

        /// <summary>
        /// The factor with which the operation mode should be configured.
        /// The factor is greater than or equal to 0 and less than or equal to 1.
        /// </summary>
        [Mandatory]
        public Double            OperationModeFactor    { get; }

        /// <summary>
        /// The revokable object type "DDBC.Instruction".
        /// </summary>
        public RevokableObject   RevokableObjectType
            => RevokableObject.DDBC_Instruction;

        /// <summary>
        /// The identification a RevokeObject uses to refer to this instruction: its "id".
        /// </summary>
        public S2Object_Id       RevokableObjectId
            => S2Object_Id.From(Id);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new DDBC instruction.
        /// </summary>
        /// <param name="Id">The identification of this instruction.</param>
        /// <param name="ExecutionTime">The moment the execution of the instruction shall start.</param>
        /// <param name="AbnormalCondition">Whether this is an instruction during an abnormal condition.</param>
        /// <param name="ActuatorId">The identification of the actuator this instruction belongs to.</param>
        /// <param name="OperationModeId">The identification of the DDBC.OperationMode.</param>
        /// <param name="OperationModeFactor">The factor with which the operation mode should be configured (0..1).</param>
        /// <param name="MessageId">An optional message identification.</param>
        public DDBC_Instruction(Instruction_Id    Id,
                                DateTimeOffset    ExecutionTime,
                                Boolean           AbnormalCondition,
                                Actuator_Id       ActuatorId,
                                OperationMode_Id  OperationModeId,
                                Double            OperationModeFactor,
                                Message_Id?       MessageId   = null)

            : base(MessageId)

        {

            if (Double.IsNaN(OperationModeFactor) || OperationModeFactor < 0 || OperationModeFactor > 1)
                throw new ArgumentException("The operation mode factor must be greater than or equal to 0 and less than or equal to 1!",
                                            nameof(OperationModeFactor));

            this.Id                   = Id;
            this.ExecutionTime        = ExecutionTime;
            this.AbnormalCondition    = AbnormalCondition;
            this.ActuatorId           = ActuatorId;
            this.OperationModeId      = OperationModeId;
            this.OperationModeFactor  = OperationModeFactor;

            unchecked
            {
                hashCode = this.MessageId.          GetHashCode() * 17 ^
                           this.Id.                 GetHashCode() * 13 ^
                           this.ExecutionTime.      GetHashCode() * 11 ^
                           this.AbnormalCondition.  GetHashCode() *  7 ^
                           this.ActuatorId.         GetHashCode() *  5 ^
                           this.OperationModeId.    GetHashCode() *  3 ^
                           this.OperationModeFactor.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // DDBC.Instruction.schema.json
        //   "title": "DDBC_Instruction",
        //   "properties": {
        //     "message_type":          { "type": "string", "const": "DDBC.Instruction" },
        //     "message_id":            { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "id":                    { "$ref": "../schemas/ID.schema.json",
        //                                "description": "Identifier of this DDBC.Instruction. Must be unique in the scope of the
        //                                                Resource Manager, for at least the duration of the session between
        //                                                Resource Manager and CEM." },
        //     "execution_time":        { "type": "string", "format": "date-time",
        //                                "description": "Indicates the moment the execution of the instruction shall start. When
        //                                                the specified execution time is in the past, execution must start as
        //                                                soon as possible." },
        //     "abnormal_condition":    { "type": "boolean",
        //                                "description": "Indicates if this is an instruction during an abnormal condition" },
        //     "actuator_id":           { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the actuator this Instruction belongs to." },
        //     "operation_mode_id":     { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the DDBC.OperationMode" },
        //     "operation_mode_factor": { "type": "number",
        //                                "description": "The number indicates the factor with which the OMBC.OperationMode should
        //                                                be configured. The factor should be greater than or equal to 0 and less
        //                                                or equal to 1." }
        //   },
        //   "required": ["message_type", "message_id", "id", "execution_time", "abnormal_condition",
        //                "actuator_id", "operation_mode_id", "operation_mode_factor"],
        //   "additionalProperties": false
        //
        // Wire-name quirk: the key is "operation_mode_id" (as in OMBC.Instruction), not "operation_mode" (FRBC.Instruction).
        //
        // Semantic rules (CONVENTIONS.md §8):
        //   - operation_mode_factor ∈ [0, 1].
        //   - Revoked via RevokeObject by its "id" (RevokableObject "DDBC.Instruction").

        #endregion

        #region (static) TryParse(JSON, out DDBCInstruction, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a DDBC instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCInstruction">The parsed DDBC instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out DDBC_Instruction?  DDBCInstruction,
                                       [NotNullWhen(false)] out String?            ErrorResponse)

            => TryParse(JSON,
                        out DDBCInstruction,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCInstruction">The parsed DDBC instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out DDBC_Instruction?  DDBCInstruction,
                                       [NotNullWhen(false)] out String?            ErrorResponse,
                                       S2ParserOptions?                            Options)

            => TryParse(JSON,
                        out DDBCInstruction,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCInstruction">The parsed DDBC instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomDDBCInstructionParser">A delegate to parse custom DDBC instructions.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out DDBC_Instruction?      DDBCInstruction,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       S2ParserOptions?                                Options,
                                       CustomJObjectParserDelegate<DDBC_Instruction>?  CustomDDBCInstructionParser)
        {

            try
            {

                DDBCInstruction = null;

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

                #region id                         [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
                                             "instruction identification",
                                             Instruction_Id.TryParse,
                                             Options,
                                             out Instruction_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region execution_time             [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("execution_time",
                                                    "execution time",
                                                    Options,
                                                    out DateTimeOffset executionTime,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region abnormal_condition         [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("abnormal_condition",
                                                  "abnormal condition",
                                                  out Boolean abnormalCondition,
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

                #region operation_mode_id          [mandatory]

                if (!JSON.ParseMandatoryS2Id("operation_mode_id",
                                             "operation mode identification",
                                             OperationMode_Id.TryParse,
                                             Options,
                                             out OperationMode_Id operationModeId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region operation_mode_factor      [mandatory]

                if (!JSON.ParseMandatoryS2Number("operation_mode_factor",
                                                 "operation mode factor",
                                                 out Double operationModeFactor,
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
                                                    "id",
                                                    "execution_time",
                                                    "abnormal_condition",
                                                    "actuator_id",
                                                    "operation_mode_id",
                                                    "operation_mode_factor"))
                {
                    return false;
                }

                #endregion


                DDBCInstruction = new DDBC_Instruction(
                                      id,
                                      executionTime,
                                      abnormalCondition,
                                      actuatorId,
                                      operationModeId,
                                      operationModeFactor,
                                      messageId
                                  );

                if (CustomDDBCInstructionParser is not null)
                    DDBCInstruction = CustomDDBCInstructionParser(JSON,
                                                                  DDBCInstruction);

                return true;

            }
            catch (Exception e)
            {
                DDBCInstruction  = null;
                ErrorResponse    = "The given JSON representation of a DDBC instruction is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDDBCInstructionSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomDDBCInstructionSerializer">A delegate to serialize custom DDBC instructions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DDBC_Instruction>? CustomDDBCInstructionSerializer)
        {

            var json = CreateJSON(
                           new JProperty("id",                     Id.             ToString()),
                           new JProperty("execution_time",         ExecutionTime.  ToS2Timestamp()),
                           new JProperty("abnormal_condition",     AbnormalCondition),
                           new JProperty("actuator_id",            ActuatorId.     ToString()),
                           new JProperty("operation_mode_id",      OperationModeId.ToString()),
                           new JProperty("operation_mode_factor",  OperationModeFactor)
                       );

            return CustomDDBCInstructionSerializer is not null
                       ? CustomDDBCInstructionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this DDBC instruction.
        /// </summary>
        public DDBC_Instruction Clone()

            => new (
                   Id.             Clone(),
                   ExecutionTime,
                   AbnormalCondition,
                   ActuatorId.     Clone(),
                   OperationModeId.Clone(),
                   OperationModeFactor,
                   MessageId.      Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two DDBC instructions for equality.
        /// </summary>
        public static Boolean operator == (DDBC_Instruction? DDBCInstruction1, DDBC_Instruction? DDBCInstruction2)
        {

            if (ReferenceEquals(DDBCInstruction1, DDBCInstruction2))
                return true;

            if (DDBCInstruction1 is null || DDBCInstruction2 is null)
                return false;

            return DDBCInstruction1.Equals(DDBCInstruction2);

        }

        /// <summary>
        /// Compares two DDBC instructions for inequality.
        /// </summary>
        public static Boolean operator != (DDBC_Instruction? DDBCInstruction1, DDBC_Instruction? DDBCInstruction2)
            => !(DDBCInstruction1 == DDBCInstruction2);

        #endregion

        #region IEquatable<DDBC_Instruction> Members

        /// <summary>
        /// Compares two DDBC instructions for equality.
        /// </summary>
        /// <param name="Object">A DDBC instruction to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is DDBC_Instruction ddbcInstruction && Equals(ddbcInstruction);

        /// <summary>
        /// Compares two DDBC instructions for equality.
        /// </summary>
        /// <param name="DDBCInstruction">A DDBC instruction to compare with.</param>
        public Boolean Equals(DDBC_Instruction? DDBCInstruction)

            => DDBCInstruction is not null &&

               MessageId.          Equals(DDBCInstruction.MessageId)           &&
               Id.                 Equals(DDBCInstruction.Id)                  &&
               ExecutionTime.      Equals(DDBCInstruction.ExecutionTime)       &&
               AbnormalCondition.  Equals(DDBCInstruction.AbnormalCondition)   &&
               ActuatorId.         Equals(DDBCInstruction.ActuatorId)          &&
               OperationModeId.    Equals(DDBCInstruction.OperationModeId)     &&
               OperationModeFactor.Equals(DDBCInstruction.OperationModeFactor);

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

                   $"DDBC.Instruction {Id}: actuator {ActuatorId} to operation mode {OperationModeId} with factor {OperationModeFactor} at {ExecutionTime.ToS2Timestamp()}",

                   AbnormalCondition
                       ? " (abnormal condition)"
                       : "",

                   $" [{MessageId}]"

               );

        #endregion

    }

}

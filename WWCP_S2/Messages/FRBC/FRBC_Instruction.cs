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
    /// The CEM instructs the Resource Manager to activate an FRBC.OperationMode of one of its
    /// actuators with a given operation mode factor at a given moment. The instruction can be
    /// revoked by its identification.
    /// </summary>
    public sealed class FRBC_Instruction : AS2Message,
                                           IInstruction,
                                           IEquatable<FRBC_Instruction>
    {

        #region Data

        /// <summary>
        /// The message type "FRBC.Instruction".
        /// </summary>
        public const String MessageTypeName = "FRBC.Instruction";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "FRBC.Instruction".
        /// </summary>
        public override String   MessageType
            => MessageTypeName;

        /// <summary>
        /// The revokable object type "FRBC.Instruction".
        /// </summary>
        public RevokableObject   RevokableObjectType
            => RevokableObject.FRBC_Instruction;

        /// <summary>
        /// The identification a RevokeObject uses to refer to this message: the identification of the instruction.
        /// </summary>
        public S2Object_Id       RevokableObjectId
            => S2Object_Id.From(Id);

        /// <summary>
        /// The identification of the instruction. Must be unique in the scope of the Resource Manager,
        /// for at least the duration of the session between Resource Manager and CEM.
        /// </summary>
        [Mandatory]
        public Instruction_Id    Id                     { get; }

        /// <summary>
        /// The identification of the actuator this instruction belongs to.
        /// </summary>
        [Mandatory]
        public Actuator_Id       ActuatorId             { get; }

        /// <summary>
        /// The identification of the FRBC.OperationMode that should be activated.
        /// </summary>
        [Mandatory]
        public OperationMode_Id  OperationMode          { get; }

        /// <summary>
        /// The factor with which the FRBC.OperationMode should be configured.
        /// The factor is greater than or equal to 0 and less than or equal to 1.
        /// </summary>
        [Mandatory]
        public Double            OperationModeFactor    { get; }

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

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new FRBC instruction.
        /// </summary>
        /// <param name="Id">The identification of the instruction.</param>
        /// <param name="ActuatorId">The identification of the actuator this instruction belongs to.</param>
        /// <param name="OperationMode">The identification of the operation mode that should be activated.</param>
        /// <param name="OperationModeFactor">The factor with which the operation mode should be configured (0..1).</param>
        /// <param name="ExecutionTime">The moment the execution of the instruction shall start.</param>
        /// <param name="AbnormalCondition">Whether this is an instruction during an abnormal condition.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public FRBC_Instruction(Instruction_Id    Id,
                                Actuator_Id       ActuatorId,
                                OperationMode_Id  OperationMode,
                                Double            OperationModeFactor,
                                DateTimeOffset    ExecutionTime,
                                Boolean           AbnormalCondition,
                                Message_Id?       MessageId   = null)

            : base(MessageId)

        {

            if (Double.IsNaN(OperationModeFactor) || OperationModeFactor < 0 || OperationModeFactor > 1)
                throw new ArgumentException($"The operation mode factor must be within [0, 1], but {OperationModeFactor} was given!",
                                            nameof(OperationModeFactor));

            this.Id                   = Id;
            this.ActuatorId           = ActuatorId;
            this.OperationMode        = OperationMode;
            this.OperationModeFactor  = OperationModeFactor;
            this.ExecutionTime        = ExecutionTime;
            this.AbnormalCondition    = AbnormalCondition;

            unchecked
            {
                hashCode = this.MessageId.          GetHashCode() * 17 ^
                           this.Id.                 GetHashCode() * 13 ^
                           this.ActuatorId.         GetHashCode() * 11 ^
                           this.OperationMode.      GetHashCode() *  7 ^
                           this.OperationModeFactor.GetHashCode() *  5 ^
                           this.ExecutionTime.      GetHashCode() *  3 ^
                           this.AbnormalCondition.  GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // FRBC.Instruction.schema.json
        //   "title": "FRBC_Instruction",
        //   "properties": {
        //     "message_type":          { "type": "string", "const": "FRBC.Instruction" },
        //     "message_id":            { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "id":                    { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the instruction. Must be unique in the scope of the Resource Manager,
        //                                                for at least the duration of the session between Resource Manager and CEM." },
        //     "actuator_id":           { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the actuator this instruction belongs to." },
        //     "operation_mode":        { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the FRBC.OperationMode that should be activated." },
        //     "operation_mode_factor": { "type": "number",
        //                                "description": "The number indicates the factor with which the FRBC.OperationMode should be
        //                                                configured. The factor should be greater than or equal to 0 and less or equal to 1." },
        //     "execution_time":        { "type": "string", "format": "date-time",
        //                                "description": "Indicates the moment the execution of the instruction shall start. When the
        //                                                specified execution time is in the past, execution must start as soon as possible." },
        //     "abnormal_condition":    { "type": "boolean",
        //                                "description": "Indicates if this is an instruction during an abnormal condition." }
        //   },
        //   "required": ["message_type", "message_id", "id", "actuator_id", "operation_mode", "operation_mode_factor",
        //                "execution_time", "abnormal_condition"],
        //   "additionalProperties": false
        //
        // Note: The JSON key is "operation_mode" (not "operation_mode_id" as in OMBC/DDBC.Instruction).
        //
        // Semantic rules (CONVENTIONS.md §8):
        //   - operation_mode_factor ∈ [0, 1].
        //   - The message is revokable (RevokableObjects: "FRBC.Instruction") by its id.
        //   - actuator_id and operation_mode refer to the FRBC.SystemDescription (validated by the session layer).

        #endregion

        #region (static) TryParse(JSON, out FRBCInstruction, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an FRBC instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCInstruction">The parsed FRBC instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out FRBC_Instruction?  FRBCInstruction,
                                       [NotNullWhen(false)] out String?            ErrorResponse)

            => TryParse(JSON,
                        out FRBCInstruction,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCInstruction">The parsed FRBC instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out FRBC_Instruction?  FRBCInstruction,
                                       [NotNullWhen(false)] out String?            ErrorResponse,
                                       S2ParserOptions?                            Options)

            => TryParse(JSON,
                        out FRBCInstruction,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCInstruction">The parsed FRBC instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomFRBCInstructionParser">A delegate to parse custom FRBC instructions.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out FRBC_Instruction?      FRBCInstruction,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       S2ParserOptions?                                Options,
                                       CustomJObjectParserDelegate<FRBC_Instruction>?  CustomFRBCInstructionParser)
        {

            try
            {

                FRBCInstruction = null;

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

                #region operation_mode             [mandatory]

                if (!JSON.ParseMandatoryS2Id("operation_mode",
                                             "operation mode identification",
                                             OperationMode_Id.TryParse,
                                             Options,
                                             out OperationMode_Id operationMode,
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

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "message_type",
                                                    "message_id",
                                                    "id",
                                                    "actuator_id",
                                                    "operation_mode",
                                                    "operation_mode_factor",
                                                    "execution_time",
                                                    "abnormal_condition"))
                {
                    return false;
                }

                #endregion


                FRBCInstruction = new FRBC_Instruction(
                                      id,
                                      actuatorId,
                                      operationMode,
                                      operationModeFactor,
                                      executionTime,
                                      abnormalCondition,
                                      messageId
                                  );

                if (CustomFRBCInstructionParser is not null)
                    FRBCInstruction = CustomFRBCInstructionParser(JSON,
                                                                  FRBCInstruction);

                return true;

            }
            catch (Exception e)
            {
                FRBCInstruction  = null;
                ErrorResponse    = "The given JSON representation of an FRBC instruction is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomFRBCInstructionSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomFRBCInstructionSerializer">A delegate to serialize custom FRBC instructions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_Instruction>? CustomFRBCInstructionSerializer)
        {

            var json = CreateJSON(
                           new JProperty("id",                     Id.           ToString()),
                           new JProperty("actuator_id",            ActuatorId.   ToString()),
                           new JProperty("operation_mode",         OperationMode.ToString()),
                           new JProperty("operation_mode_factor",  OperationModeFactor),
                           new JProperty("execution_time",         ExecutionTime.ToS2Timestamp()),
                           new JProperty("abnormal_condition",     AbnormalCondition)
                       );

            return CustomFRBCInstructionSerializer is not null
                       ? CustomFRBCInstructionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this FRBC instruction.
        /// </summary>
        public FRBC_Instruction Clone()

            => new (
                   Id.           Clone(),
                   ActuatorId.   Clone(),
                   OperationMode.Clone(),
                   OperationModeFactor,
                   ExecutionTime,
                   AbnormalCondition,
                   MessageId.    Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two FRBC instructions for equality.
        /// </summary>
        public static Boolean operator == (FRBC_Instruction? FRBCInstruction1, FRBC_Instruction? FRBCInstruction2)
        {

            if (ReferenceEquals(FRBCInstruction1, FRBCInstruction2))
                return true;

            if (FRBCInstruction1 is null || FRBCInstruction2 is null)
                return false;

            return FRBCInstruction1.Equals(FRBCInstruction2);

        }

        /// <summary>
        /// Compares two FRBC instructions for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_Instruction? FRBCInstruction1, FRBC_Instruction? FRBCInstruction2)
            => !(FRBCInstruction1 == FRBCInstruction2);

        #endregion

        #region IEquatable<FRBC_Instruction> Members

        /// <summary>
        /// Compares two FRBC instructions for equality.
        /// </summary>
        /// <param name="Object">An FRBC instruction to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_Instruction frbcInstruction && Equals(frbcInstruction);

        /// <summary>
        /// Compares two FRBC instructions for equality.
        /// </summary>
        /// <param name="FRBCInstruction">An FRBC instruction to compare with.</param>
        public Boolean Equals(FRBC_Instruction? FRBCInstruction)

            => FRBCInstruction is not null &&

               MessageId.          Equals(FRBCInstruction.MessageId)           &&
               Id.                 Equals(FRBCInstruction.Id)                  &&
               ActuatorId.         Equals(FRBCInstruction.ActuatorId)          &&
               OperationMode.      Equals(FRBCInstruction.OperationMode)       &&
               OperationModeFactor.Equals(FRBCInstruction.OperationModeFactor) &&
               ExecutionTime.      Equals(FRBCInstruction.ExecutionTime)       &&
               AbnormalCondition.  Equals(FRBCInstruction.AbnormalCondition);

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

                   $"FRBC.Instruction {Id}: {ActuatorId} -> {OperationMode} x {OperationModeFactor} at {ExecutionTime.ToS2Timestamp()}",

                   AbnormalCondition
                       ? " (abnormal condition)"
                       : "",

                   $" [{MessageId}]"

               );

        #endregion

    }

}

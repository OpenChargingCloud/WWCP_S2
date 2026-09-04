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
    /// An instruction of the CEM to an Operation Mode Based Control (OMBC) system: activate
    /// the given operation mode with the given factor at the given execution time.
    /// </summary>
    public sealed class OMBC_Instruction : AS2Message,
                                           IInstruction,
                                           IEquatable<OMBC_Instruction>
    {

        #region Data

        /// <summary>
        /// The message type "OMBC.Instruction".
        /// </summary>
        public const String MessageTypeName = "OMBC.Instruction";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "OMBC.Instruction".
        /// </summary>
        public override String   MessageType
            => MessageTypeName;

        /// <summary>
        /// The identification of the instruction. Must be unique in the scope of the Resource
        /// Manager, for at least the duration of the session between Resource Manager and CEM.
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
        /// The identification of the OMBC.OperationMode that should be activated.
        /// </summary>
        [Mandatory]
        public OperationMode_Id  OperationModeId        { get; }

        /// <summary>
        /// The factor with which the OMBC.OperationMode should be configured.
        /// The factor is greater than or equal to 0 and less than or equal to 1.
        /// </summary>
        [Mandatory]
        public Double            OperationModeFactor    { get; }

        /// <summary>
        /// Indicates if this is an instruction during an abnormal condition.
        /// </summary>
        [Mandatory]
        public Boolean           AbnormalCondition      { get; }


        /// <summary>
        /// The revokable object type "OMBC.Instruction".
        /// </summary>
        public RevokableObject   RevokableObjectType
            => RevokableObject.OMBC_Instruction;

        /// <summary>
        /// The identification a RevokeObject uses to refer to this message: the instruction identification.
        /// </summary>
        public S2Object_Id       RevokableObjectId
            => S2Object_Id.From(Id);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new OMBC instruction.
        /// </summary>
        /// <param name="Id">The identification of the instruction.</param>
        /// <param name="ExecutionTime">The moment the execution of the instruction shall start.</param>
        /// <param name="OperationModeId">The identification of the OMBC.OperationMode that should be activated.</param>
        /// <param name="OperationModeFactor">The factor with which the OMBC.OperationMode should be configured (0..1).</param>
        /// <param name="AbnormalCondition">Whether this is an instruction during an abnormal condition.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public OMBC_Instruction(Instruction_Id    Id,
                                DateTimeOffset    ExecutionTime,
                                OperationMode_Id  OperationModeId,
                                Double            OperationModeFactor,
                                Boolean           AbnormalCondition,
                                Message_Id?       MessageId   = null)

            : base(MessageId)

        {

            if (Id.IsNullOrEmpty)
                throw new ArgumentException("The identification of an OMBC instruction must not be null or empty!",
                                            nameof(Id));

            if (OperationModeId.IsNullOrEmpty)
                throw new ArgumentException("The operation mode identification of an OMBC instruction must not be null or empty!",
                                            nameof(OperationModeId));

            // The negated form also rejects NaN.
            if (!(OperationModeFactor >= 0 && OperationModeFactor <= 1))
                throw new ArgumentException($"The operation mode factor of an OMBC instruction must be within [0, 1], but {OperationModeFactor} was given!",
                                            nameof(OperationModeFactor));

            this.Id                   = Id;
            this.ExecutionTime        = ExecutionTime;
            this.OperationModeId      = OperationModeId;
            this.OperationModeFactor  = OperationModeFactor;
            this.AbnormalCondition    = AbnormalCondition;

            unchecked
            {
                hashCode = this.MessageId.          GetHashCode() * 13 ^
                           this.Id.                 GetHashCode() * 11 ^
                           this.ExecutionTime.      GetHashCode() *  7 ^
                           this.OperationModeId.    GetHashCode() *  5 ^
                           this.OperationModeFactor.GetHashCode() *  3 ^
                           this.AbnormalCondition.  GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // OMBC.Instruction.schema.json
        //   "title": "OMBC_Instruction",
        //   "properties": {
        //     "message_type":          { "type": "string", "const": "OMBC.Instruction" },
        //     "message_id":            { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "id":                    { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the instruction. Must be unique in the scope of the Resource Manager,
        //                                                for at least the duration of the session between Resource Manager and CEM." },
        //     "execution_time":        { "type": "string", "format": "date-time",
        //                                "description": "Indicates the moment the execution of the instruction shall start. When the
        //                                                specified execution time is in the past, execution must start as soon as possible." },
        //     "operation_mode_id":     { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the OMBC.OperationMode that should be activated" },
        //     "operation_mode_factor": { "type": "number",
        //                                "description": "The number indicates the factor with which the OMBC.OperationMode should be
        //                                                configured. The factor should be greater than or equal than 0 and less or equal to 1." },
        //     "abnormal_condition":    { "type": "boolean",
        //                                "description": "Indicates if this is an instruction during an abnormal condition" }
        //   },
        //   "required": ["message_type", "message_id", "id", "execution_time", "operation_mode_id", "operation_mode_factor", "abnormal_condition"],
        //   "additionalProperties": false
        //
        // Note: the JSON key is "operation_mode_id" (FRBC.Instruction uses "operation_mode").
        // Semantic rule (CONVENTIONS.md §8): operation_mode_factor ∈ [0, 1].
        // Revokable via RevokeObject with object_type "OMBC.Instruction" and object_id = id.

        #endregion

        #region (static) TryParse(JSON, out OMBC_Instruction, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an OMBC instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCInstruction">The parsed OMBC instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out OMBC_Instruction?  OMBCInstruction,
                                       [NotNullWhen(false)] out String?            ErrorResponse)

            => TryParse(JSON,
                        out OMBCInstruction,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an OMBC instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCInstruction">The parsed OMBC instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out OMBC_Instruction?  OMBCInstruction,
                                       [NotNullWhen(false)] out String?            ErrorResponse,
                                       S2ParserOptions?                            Options)

            => TryParse(JSON,
                        out OMBCInstruction,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an OMBC instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCInstruction">The parsed OMBC instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomOMBCInstructionParser">A delegate to parse custom OMBC instructions.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out OMBC_Instruction?      OMBCInstruction,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       S2ParserOptions?                                Options,
                                       CustomJObjectParserDelegate<OMBC_Instruction>?  CustomOMBCInstructionParser)
        {

            try
            {

                OMBCInstruction = null;

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
                                                    "execution_time",
                                                    "operation_mode_id",
                                                    "operation_mode_factor",
                                                    "abnormal_condition"))
                {
                    return false;
                }

                #endregion


                OMBCInstruction = new OMBC_Instruction(
                                      id,
                                      executionTime,
                                      operationModeId,
                                      operationModeFactor,
                                      abnormalCondition,
                                      messageId
                                  );

                if (CustomOMBCInstructionParser is not null)
                    OMBCInstruction = CustomOMBCInstructionParser(JSON,
                                                                  OMBCInstruction);

                return true;

            }
            catch (Exception e)
            {
                OMBCInstruction  = null;
                ErrorResponse    = "The given JSON representation of an OMBC instruction is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomOMBCInstructionSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomOMBCInstructionSerializer">A delegate to serialize custom OMBC instructions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<OMBC_Instruction>? CustomOMBCInstructionSerializer)
        {

            var json = CreateJSON(

                           new JProperty("id",                     Id.             ToString()),
                           new JProperty("execution_time",         ExecutionTime.  ToS2Timestamp()),
                           new JProperty("operation_mode_id",      OperationModeId.ToString()),
                           new JProperty("operation_mode_factor",  OperationModeFactor),
                           new JProperty("abnormal_condition",     AbnormalCondition)

                       );

            return CustomOMBCInstructionSerializer is not null
                       ? CustomOMBCInstructionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this OMBC instruction.
        /// </summary>
        public OMBC_Instruction Clone()

            => new (
                   Id.             Clone(),
                   ExecutionTime,
                   OperationModeId.Clone(),
                   OperationModeFactor,
                   AbnormalCondition,
                   MessageId.      Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two OMBC instructions for equality.
        /// </summary>
        public static Boolean operator == (OMBC_Instruction? OMBCInstruction1, OMBC_Instruction? OMBCInstruction2)
        {

            if (ReferenceEquals(OMBCInstruction1, OMBCInstruction2))
                return true;

            if (OMBCInstruction1 is null || OMBCInstruction2 is null)
                return false;

            return OMBCInstruction1.Equals(OMBCInstruction2);

        }

        /// <summary>
        /// Compares two OMBC instructions for inequality.
        /// </summary>
        public static Boolean operator != (OMBC_Instruction? OMBCInstruction1, OMBC_Instruction? OMBCInstruction2)
            => !(OMBCInstruction1 == OMBCInstruction2);

        #endregion

        #region IEquatable<OMBC_Instruction> Members

        /// <summary>
        /// Compares two OMBC instructions for equality.
        /// </summary>
        /// <param name="Object">An OMBC instruction to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is OMBC_Instruction ombcInstruction && Equals(ombcInstruction);

        /// <summary>
        /// Compares two OMBC instructions for equality.
        /// </summary>
        /// <param name="OMBCInstruction">An OMBC instruction to compare with.</param>
        public Boolean Equals(OMBC_Instruction? OMBCInstruction)

            => OMBCInstruction is not null &&

               MessageId.          Equals(OMBCInstruction.MessageId)           &&
               Id.                 Equals(OMBCInstruction.Id)                  &&
               ExecutionTime.      Equals(OMBCInstruction.ExecutionTime)       &&
               OperationModeId.    Equals(OMBCInstruction.OperationModeId)     &&
               OperationModeFactor.Equals(OMBCInstruction.OperationModeFactor) &&
               AbnormalCondition.  Equals(OMBCInstruction.AbnormalCondition);

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

                   $"OMBC.Instruction {Id}: activate {OperationModeId} at factor {OperationModeFactor} at {ExecutionTime.ToS2Timestamp()}",

                   AbnormalCondition
                       ? " (abnormal condition)"
                       : "",

                   $" [{MessageId}]"

               );

        #endregion

    }

}

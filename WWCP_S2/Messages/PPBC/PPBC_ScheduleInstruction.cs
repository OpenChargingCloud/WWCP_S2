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
    /// The CEM selects one power sequence of a power sequence container of a power profile
    /// definition and schedules it to start at the given execution time.
    /// </summary>
    public sealed class PPBC_ScheduleInstruction : AS2Message,
                                                   IInstruction,
                                                   IEquatable<PPBC_ScheduleInstruction>
    {

        #region Data

        /// <summary>
        /// The message type "PPBC.ScheduleInstruction".
        /// </summary>
        public const String MessageTypeName = "PPBC.ScheduleInstruction";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "PPBC.ScheduleInstruction".
        /// </summary>
        public override String              MessageType
            => MessageTypeName;

        /// <summary>
        /// The identification of this instruction. Must be unique in the scope of the Resource
        /// Manager, for at least the duration of the session between Resource Manager and CEM.
        /// </summary>
        [Mandatory]
        public Instruction_Id               Id                     { get; }

        /// <summary>
        /// The identification of the PPBC.PowerProfileDefinition of which the
        /// PPBC.PowerSequence is being selected and scheduled by the CEM.
        /// </summary>
        [Mandatory]
        public PowerProfileDefinition_Id    PowerProfileId         { get; }

        /// <summary>
        /// The identification of the PPBC.PowerSequenceContainer of which the
        /// PPBC.PowerSequence is being selected and scheduled by the CEM.
        /// </summary>
        [Mandatory]
        public PowerSequenceContainer_Id    SequenceContainerId    { get; }

        /// <summary>
        /// The identification of the PPBC.PowerSequence that is being selected and scheduled by the CEM.
        /// </summary>
        [Mandatory]
        public PowerSequence_Id             PowerSequenceId        { get; }

        /// <summary>
        /// The moment the PPBC.PowerSequence shall start. When the specified execution
        /// time is in the past, execution must start as soon as possible.
        /// </summary>
        [Mandatory]
        public DateTimeOffset               ExecutionTime          { get; }

        /// <summary>
        /// Whether this is an instruction during an abnormal condition.
        /// </summary>
        [Mandatory]
        public Boolean                      AbnormalCondition      { get; }

        /// <summary>
        /// The revokable object type "PPBC.ScheduleInstruction".
        /// </summary>
        public RevokableObject              RevokableObjectType
            => RevokableObject.PPBC_ScheduleInstruction;

        /// <summary>
        /// The identification a RevokeObject uses to refer to this instruction (its "id").
        /// </summary>
        public S2Object_Id                  RevokableObjectId
            => S2Object_Id.From(Id);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new PPBC schedule instruction.
        /// </summary>
        /// <param name="Id">The identification of this instruction (unique in the scope of the Resource Manager).</param>
        /// <param name="PowerProfileId">The identification of the PPBC.PowerProfileDefinition of which the PPBC.PowerSequence is being selected and scheduled.</param>
        /// <param name="SequenceContainerId">The identification of the PPBC.PowerSequenceContainer of which the PPBC.PowerSequence is being selected and scheduled.</param>
        /// <param name="PowerSequenceId">The identification of the PPBC.PowerSequence that is being selected and scheduled.</param>
        /// <param name="ExecutionTime">The moment the PPBC.PowerSequence shall start.</param>
        /// <param name="AbnormalCondition">Whether this is an instruction during an abnormal condition.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public PPBC_ScheduleInstruction(Instruction_Id             Id,
                                        PowerProfileDefinition_Id  PowerProfileId,
                                        PowerSequenceContainer_Id  SequenceContainerId,
                                        PowerSequence_Id           PowerSequenceId,
                                        DateTimeOffset             ExecutionTime,
                                        Boolean                    AbnormalCondition,
                                        Message_Id?                MessageId   = null)

            : base(MessageId)

        {

            this.Id                   = Id;
            this.PowerProfileId       = PowerProfileId;
            this.SequenceContainerId  = SequenceContainerId;
            this.PowerSequenceId      = PowerSequenceId;
            this.ExecutionTime        = ExecutionTime;
            this.AbnormalCondition    = AbnormalCondition;

            unchecked
            {
                hashCode = this.MessageId.          GetHashCode() * 17 ^
                           this.Id.                 GetHashCode() * 13 ^
                           this.PowerProfileId.     GetHashCode() * 11 ^
                           this.SequenceContainerId.GetHashCode() *  7 ^
                           this.PowerSequenceId.    GetHashCode() *  5 ^
                           this.ExecutionTime.      GetHashCode() *  3 ^
                           this.AbnormalCondition.  GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // PPBC.ScheduleInstruction.schema.json
        //   "title": "PPBC_ScheduleInstruction",
        //   "properties": {
        //     "message_type":          { "type": "string", "const": "PPBC.ScheduleInstruction" },
        //     "message_id":            { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "id":                    { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the Instruction. Must be unique in the scope of the Resource Manager,
        //                                                for at least the duration of the session between Resource Manager and CEM." },
        //     "power_profile_id":      { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the PPBC.PowerProfileDefinition of which the PPBC.PowerSequence is being
        //                                                selected and scheduled by the CEM." },
        //     "sequence_container_id": { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the PPBC.PowerSequnceContainer of which the PPBC.PowerSequence is being
        //                                                selected and scheduled by the CEM." },
        //     "power_sequence_id":     { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the PPBC.PowerSequence that is being selected and scheduled by the CEM." },
        //     "execution_time":        { "type": "string", "format": "date-time",
        //                                "description": "Indicates the moment the PPBC.PowerSequence shall start. When the specified
        //                                                execution time is in the past, execution must start as soon as possible." },
        //     "abnormal_condition":    { "type": "boolean",
        //                                "description": "Indicates if this is an instruction during an abnormal condition" }
        //   },
        //   "required": ["message_type", "message_id", "id", "power_profile_id", "sequence_container_id",
        //                "power_sequence_id", "execution_time", "abnormal_condition"],
        //   "additionalProperties": false
        //
        // The references to the power profile definition, the power sequence container and the
        // power sequence are cross-message rules and are validated by the session layer.

        #endregion

        #region (static) TryParse(JSON, out PPBC_ScheduleInstruction, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a PPBC schedule instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_ScheduleInstruction">The parsed PPBC schedule instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)]  out PPBC_ScheduleInstruction?  PPBC_ScheduleInstruction,
                                       [NotNullWhen(false)] out String?                    ErrorResponse)

            => TryParse(JSON,
                        out PPBC_ScheduleInstruction,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC schedule instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_ScheduleInstruction">The parsed PPBC schedule instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)]  out PPBC_ScheduleInstruction?  PPBC_ScheduleInstruction,
                                       [NotNullWhen(false)] out String?                    ErrorResponse,
                                       S2ParserOptions?                                    Options)

            => TryParse(JSON,
                        out PPBC_ScheduleInstruction,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC schedule instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_ScheduleInstruction">The parsed PPBC schedule instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPPBC_ScheduleInstructionParser">A delegate to parse custom PPBC schedule instructions.</param>
        public static Boolean TryParse(JObject                                                 JSON,
                                       [NotNullWhen(true)]  out PPBC_ScheduleInstruction?      PPBC_ScheduleInstruction,
                                       [NotNullWhen(false)] out String?                        ErrorResponse,
                                       S2ParserOptions?                                        Options,
                                       CustomJObjectParserDelegate<PPBC_ScheduleInstruction>?  CustomPPBC_ScheduleInstructionParser)
        {

            try
            {

                PPBC_ScheduleInstruction = null;

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

                #region power_profile_id           [mandatory]

                if (!JSON.ParseMandatoryS2Id("power_profile_id",
                                             "power profile definition identification",
                                             PowerProfileDefinition_Id.TryParse,
                                             Options,
                                             out PowerProfileDefinition_Id powerProfileId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region sequence_container_id      [mandatory]

                if (!JSON.ParseMandatoryS2Id("sequence_container_id",
                                             "power sequence container identification",
                                             PowerSequenceContainer_Id.TryParse,
                                             Options,
                                             out PowerSequenceContainer_Id sequenceContainerId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region power_sequence_id          [mandatory]

                if (!JSON.ParseMandatoryS2Id("power_sequence_id",
                                             "power sequence identification",
                                             PowerSequence_Id.TryParse,
                                             Options,
                                             out PowerSequence_Id powerSequenceId,
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
                                                    "power_profile_id",
                                                    "sequence_container_id",
                                                    "power_sequence_id",
                                                    "execution_time",
                                                    "abnormal_condition"))
                {
                    return false;
                }

                #endregion


                PPBC_ScheduleInstruction = new PPBC_ScheduleInstruction(
                                               id,
                                               powerProfileId,
                                               sequenceContainerId,
                                               powerSequenceId,
                                               executionTime,
                                               abnormalCondition,
                                               messageId
                                           );

                if (CustomPPBC_ScheduleInstructionParser is not null)
                    PPBC_ScheduleInstruction = CustomPPBC_ScheduleInstructionParser(JSON,
                                                                                    PPBC_ScheduleInstruction);

                return true;

            }
            catch (Exception e)
            {
                PPBC_ScheduleInstruction  = null;
                ErrorResponse             = "The given JSON representation of a PPBC schedule instruction is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPPBC_ScheduleInstructionSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomPPBC_ScheduleInstructionSerializer">A delegate to serialize custom PPBC schedule instructions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PPBC_ScheduleInstruction>? CustomPPBC_ScheduleInstructionSerializer)
        {

            var json = CreateJSON(
                           new JProperty("id",                     Id.                 ToString()),
                           new JProperty("power_profile_id",       PowerProfileId.     ToString()),
                           new JProperty("sequence_container_id",  SequenceContainerId.ToString()),
                           new JProperty("power_sequence_id",      PowerSequenceId.    ToString()),
                           new JProperty("execution_time",         ExecutionTime.      ToS2Timestamp()),
                           new JProperty("abnormal_condition",     AbnormalCondition)
                       );

            return CustomPPBC_ScheduleInstructionSerializer is not null
                       ? CustomPPBC_ScheduleInstructionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this PPBC schedule instruction.
        /// </summary>
        public PPBC_ScheduleInstruction Clone()

            => new (
                   Id.                 Clone(),
                   PowerProfileId.     Clone(),
                   SequenceContainerId.Clone(),
                   PowerSequenceId.    Clone(),
                   ExecutionTime,
                   AbnormalCondition,
                   MessageId.          Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two PPBC schedule instructions for equality.
        /// </summary>
        public static Boolean operator == (PPBC_ScheduleInstruction? PPBC_ScheduleInstruction1, PPBC_ScheduleInstruction? PPBC_ScheduleInstruction2)
        {

            if (ReferenceEquals(PPBC_ScheduleInstruction1, PPBC_ScheduleInstruction2))
                return true;

            if (PPBC_ScheduleInstruction1 is null || PPBC_ScheduleInstruction2 is null)
                return false;

            return PPBC_ScheduleInstruction1.Equals(PPBC_ScheduleInstruction2);

        }

        /// <summary>
        /// Compares two PPBC schedule instructions for inequality.
        /// </summary>
        public static Boolean operator != (PPBC_ScheduleInstruction? PPBC_ScheduleInstruction1, PPBC_ScheduleInstruction? PPBC_ScheduleInstruction2)
            => !(PPBC_ScheduleInstruction1 == PPBC_ScheduleInstruction2);

        #endregion

        #region IEquatable<PPBC_ScheduleInstruction> Members

        /// <summary>
        /// Compares two PPBC schedule instructions for equality.
        /// </summary>
        /// <param name="Object">A PPBC schedule instruction to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PPBC_ScheduleInstruction ppbcScheduleInstruction && Equals(ppbcScheduleInstruction);

        /// <summary>
        /// Compares two PPBC schedule instructions for equality.
        /// </summary>
        /// <param name="PPBC_ScheduleInstruction">A PPBC schedule instruction to compare with.</param>
        public Boolean Equals(PPBC_ScheduleInstruction? PPBC_ScheduleInstruction)

            => PPBC_ScheduleInstruction is not null &&

               MessageId.          Equals(PPBC_ScheduleInstruction.MessageId)           &&
               Id.                 Equals(PPBC_ScheduleInstruction.Id)                  &&
               PowerProfileId.     Equals(PPBC_ScheduleInstruction.PowerProfileId)      &&
               SequenceContainerId.Equals(PPBC_ScheduleInstruction.SequenceContainerId) &&
               PowerSequenceId.    Equals(PPBC_ScheduleInstruction.PowerSequenceId)     &&
               ExecutionTime.      Equals(PPBC_ScheduleInstruction.ExecutionTime)       &&
               AbnormalCondition.  Equals(PPBC_ScheduleInstruction.AbnormalCondition);

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

                   $"PPBC.ScheduleInstruction {Id}: sequence {PowerSequenceId} of container {SequenceContainerId} of profile {PowerProfileId} at {ExecutionTime.ToS2Timestamp()}",

                   AbnormalCondition
                       ? " (abnormal condition)"
                       : "",

                   $" [{MessageId}]"

               );

        #endregion

    }

}

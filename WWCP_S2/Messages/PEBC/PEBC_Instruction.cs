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
    /// The CEM instructs the Resource Manager to follow one or more power envelopes
    /// (Power Envelope Based Control), chosen within the allowed limit ranges of the
    /// referenced PEBC.PowerConstraints, starting at the execution time.
    /// </summary>
    public sealed class PEBC_Instruction : AS2Message,
                                           IInstruction,
                                           IEquatable<PEBC_Instruction>
    {

        #region Data

        /// <summary>
        /// The message type "PEBC.Instruction".
        /// </summary>
        public const String MessageTypeName = "PEBC.Instruction";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "PEBC.Instruction".
        /// </summary>
        public override String                     MessageType
            => MessageTypeName;

        /// <summary>
        /// The identification of this instruction. Must be unique in the scope of the Resource
        /// Manager, for at least the duration of the session between Resource Manager and CEM.
        /// </summary>
        [Mandatory]
        public Instruction_Id                      Id                     { get; }

        /// <summary>
        /// The moment the execution of the instruction shall start. When the specified
        /// execution time is in the past, execution must start as soon as possible.
        /// </summary>
        [Mandatory]
        public DateTimeOffset                      ExecutionTime          { get; }

        /// <summary>
        /// Whether this is an instruction during an abnormal condition.
        /// </summary>
        [Mandatory]
        public Boolean                             AbnormalCondition      { get; }

        /// <summary>
        /// The identification of the PEBC.PowerConstraints this instruction was based on.
        /// </summary>
        [Mandatory]
        public PowerConstraints_Id                 PowerConstraintsId     { get; }

        /// <summary>
        /// The power envelope(s) that should be followed by the Resource Manager (1..10).
        /// There shall be at least one power envelope, but at most one power envelope
        /// for each commodity quantity.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PEBC_PowerEnvelope>   PowerEnvelopes         { get; }

        /// <summary>
        /// The revokable object type of this message: PEBC.Instruction.
        /// </summary>
        public RevokableObject                     RevokableObjectType
            => RevokableObject.PEBC_Instruction;

        /// <summary>
        /// The identification a RevokeObject uses to refer to this message: its "id".
        /// </summary>
        public S2Object_Id                         RevokableObjectId
            => S2Object_Id.From(Id);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new PEBC instruction.
        /// </summary>
        /// <param name="Id">The identification of this instruction.</param>
        /// <param name="ExecutionTime">The moment the execution of the instruction shall start.</param>
        /// <param name="AbnormalCondition">Whether this is an instruction during an abnormal condition.</param>
        /// <param name="PowerConstraintsId">The identification of the PEBC.PowerConstraints this instruction was based on.</param>
        /// <param name="PowerEnvelopes">The power envelope(s) to be followed (1..10, at most one per commodity quantity).</param>
        /// <param name="MessageId">An optional message identification.</param>
        public PEBC_Instruction(Instruction_Id                     Id,
                                DateTimeOffset                     ExecutionTime,
                                Boolean                            AbnormalCondition,
                                PowerConstraints_Id                PowerConstraintsId,
                                IReadOnlyList<PEBC_PowerEnvelope>  PowerEnvelopes,
                                Message_Id?                        MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(PowerEnvelopes);

            if (PowerEnvelopes.Count < 1)
                throw new ArgumentException("A PEBC instruction must contain at least one power envelope!",
                                            nameof(PowerEnvelopes));

            if (PowerEnvelopes.Count > 10)
                throw new ArgumentException($"A PEBC instruction must not contain more than 10 power envelopes, but {PowerEnvelopes.Count} were given!",
                                            nameof(PowerEnvelopes));

            if (PowerEnvelopes.Any(powerEnvelope => powerEnvelope is null))
                throw new ArgumentException("The power envelopes must not contain null items!",
                                            nameof(PowerEnvelopes));

            var duplicateCommodityQuantity = PowerEnvelopes.
                                                 GroupBy(powerEnvelope => powerEnvelope.CommodityQuantity).
                                                 FirstOrDefault(group => group.Count() > 1);

            if (duplicateCommodityQuantity is not null)
                throw new ArgumentException($"A PEBC instruction must contain at most one power envelope per commodity quantity, but '{duplicateCommodityQuantity.Key}' occurs {duplicateCommodityQuantity.Count()} times!",
                                            nameof(PowerEnvelopes));

            this.Id                  = Id;
            this.ExecutionTime       = ExecutionTime;
            this.AbnormalCondition   = AbnormalCondition;
            this.PowerConstraintsId  = PowerConstraintsId;
            this.PowerEnvelopes      = [.. PowerEnvelopes];

            unchecked
            {
                hashCode = this.MessageId.         GetHashCode() * 13 ^
                           this.Id.                GetHashCode() * 11 ^
                           this.ExecutionTime.     GetHashCode() *  7 ^
                           this.AbnormalCondition. GetHashCode() *  5 ^
                           this.PowerConstraintsId.GetHashCode() *  3 ^
                           this.PowerEnvelopes.    CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // PEBC.Instruction.schema.json
        //   "title": "PEBC_Instruction",
        //   "properties": {
        //     "message_type":         { "type": "string", "const": "PEBC.Instruction" },
        //     "message_id":           { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "id":                   { "$ref": "../schemas/ID.schema.json",
        //                               "description": "Identifier of this PEBC.Instruction. Must be unique in the scope of the
        //                                               Resource Manager, for at least the duration of the session between Resource
        //                                               Manager and CEM." },
        //     "execution_time":       { "type": "string", "format": "date-time",
        //                               "description": "Indicates the moment the execution of the instruction shall start. When the
        //                                               specified execution time is in the past, execution must start as soon as possible." },
        //     "abnormal_condition":   { "type": "boolean",
        //                               "description": "Indicates if this is an instruction during an abnormal condition." },
        //     "power_constraints_id": { "$ref": "../schemas/ID.schema.json",
        //                               "description": "Identifier of the PEBC.PowerConstraints this PEBC.Instruction was based on." },
        //     "power_envelopes": {
        //       "type": "array", "minItems": 1, "maxItems": 10,
        //       "items": { "$ref": "../schemas/PEBC.PowerEnvelope.schema.json" },
        //       "description": "The PEBC.PowerEnvelope(s) that should be followed by the Resource Manager. There shall be at least one
        //                       PEBC.PowerEnvelope, but at most one PEBC.PowerEnvelope for each CommodityQuantity."
        //     }
        //   },
        //   "required": ["message_type", "message_id", "id", "execution_time", "abnormal_condition", "power_constraints_id", "power_envelopes"],
        //   "additionalProperties": false
        //
        // Semantic rules (CONVENTIONS.md §8):
        //   - power_envelopes: 1..10 items, at most one PEBC.PowerEnvelope per CommodityQuantity.
        //   - power_constraints_id references a PEBC.PowerConstraints of the session (checked by the session layer).

        #endregion

        #region (static) TryParse(JSON, out Instruction, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a PEBC instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Instruction">The parsed PEBC instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out PEBC_Instruction?  Instruction,
                                       [NotNullWhen(false)] out String?            ErrorResponse)

            => TryParse(JSON,
                        out Instruction,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PEBC instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Instruction">The parsed PEBC instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out PEBC_Instruction?  Instruction,
                                       [NotNullWhen(false)] out String?            ErrorResponse,
                                       S2ParserOptions?                            Options)

            => TryParse(JSON,
                        out Instruction,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PEBC instruction.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Instruction">The parsed PEBC instruction.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomInstructionParser">A delegate to parse custom PEBC instructions.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out PEBC_Instruction?      Instruction,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       S2ParserOptions?                                Options,
                                       CustomJObjectParserDelegate<PEBC_Instruction>?  CustomInstructionParser)
        {

            try
            {

                Instruction = null;

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

                #region power_constraints_id       [mandatory]

                if (!JSON.ParseMandatoryS2Id("power_constraints_id",
                                             "power constraints identification",
                                             PowerConstraints_Id.TryParse,
                                             Options,
                                             out PowerConstraints_Id powerConstraintsId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region power_envelopes            [mandatory]

                if (!JSON.ParseMandatoryS2List("power_envelopes",
                                               "power envelopes",
                                               PEBC_PowerEnvelope.TryParse,
                                               Options,
                                               1,
                                               10,
                                               out IReadOnlyList<PEBC_PowerEnvelope>? powerEnvelopes,
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
                                                    "power_constraints_id",
                                                    "power_envelopes"))
                {
                    return false;
                }

                #endregion


                Instruction = new PEBC_Instruction(
                                  id,
                                  executionTime,
                                  abnormalCondition,
                                  powerConstraintsId,
                                  powerEnvelopes,
                                  messageId
                              );

                if (CustomInstructionParser is not null)
                    Instruction = CustomInstructionParser(JSON,
                                                          Instruction);

                return true;

            }
            catch (Exception e)
            {
                Instruction    = null;
                ErrorResponse  = "The given JSON representation of a PEBC instruction is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomInstructionSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomInstructionSerializer">A delegate to serialize custom PEBC instructions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PEBC_Instruction>? CustomInstructionSerializer)
        {

            var json = CreateJSON(
                           new JProperty("id",                    Id.                ToString()),
                           new JProperty("execution_time",        ExecutionTime.     ToS2Timestamp()),
                           new JProperty("abnormal_condition",    AbnormalCondition),
                           new JProperty("power_constraints_id",  PowerConstraintsId.ToString()),
                           new JProperty("power_envelopes",       new JArray(PowerEnvelopes.Select(powerEnvelope => powerEnvelope.ToJSON())))
                       );

            return CustomInstructionSerializer is not null
                       ? CustomInstructionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this PEBC instruction.
        /// </summary>
        public PEBC_Instruction Clone()

            => new (
                   Id.                Clone(),
                   ExecutionTime,
                   AbnormalCondition,
                   PowerConstraintsId.Clone(),
                   [.. PowerEnvelopes.Select(powerEnvelope => powerEnvelope.Clone())],
                   MessageId.         Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two PEBC instructions for equality.
        /// </summary>
        public static Boolean operator == (PEBC_Instruction? Instruction1, PEBC_Instruction? Instruction2)
        {

            if (ReferenceEquals(Instruction1, Instruction2))
                return true;

            if (Instruction1 is null || Instruction2 is null)
                return false;

            return Instruction1.Equals(Instruction2);

        }

        /// <summary>
        /// Compares two PEBC instructions for inequality.
        /// </summary>
        public static Boolean operator != (PEBC_Instruction? Instruction1, PEBC_Instruction? Instruction2)
            => !(Instruction1 == Instruction2);

        #endregion

        #region IEquatable<PEBC_Instruction> Members

        /// <summary>
        /// Compares two PEBC instructions for equality.
        /// </summary>
        /// <param name="Object">A PEBC instruction to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PEBC_Instruction instruction && Equals(instruction);

        /// <summary>
        /// Compares two PEBC instructions for equality.
        /// </summary>
        /// <param name="Instruction">A PEBC instruction to compare with.</param>
        public Boolean Equals(PEBC_Instruction? Instruction)

            => Instruction is not null &&

               MessageId.         Equals(Instruction.MessageId)          &&
               Id.                Equals(Instruction.Id)                 &&
               ExecutionTime.     Equals(Instruction.ExecutionTime)      &&
               AbnormalCondition. Equals(Instruction.AbnormalCondition)  &&
               PowerConstraintsId.Equals(Instruction.PowerConstraintsId) &&

               PowerEnvelopes.SequenceEqual(Instruction.PowerEnvelopes);

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

                   $"PEBC.Instruction '{Id}' at {ExecutionTime.ToS2Timestamp()} based on power constraints '{PowerConstraintsId}', ",
                   $"{PowerEnvelopes.Count} power envelope(s)",

                   AbnormalCondition
                       ? " (abnormal condition)"
                       : "",

                   $" [{MessageId}]"

               );

        #endregion

    }

}

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
    /// The Resource Manager informs the CEM about the present status of an instruction
    /// and the moment that status was reached.
    /// </summary>
    public sealed class InstructionStatusUpdate : AS2Message,
                                                  IEquatable<InstructionStatusUpdate>
    {

        #region Data

        /// <summary>
        /// The message type "InstructionStatusUpdate".
        /// </summary>
        public const String MessageTypeName = "InstructionStatusUpdate";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "InstructionStatusUpdate".
        /// </summary>
        public override String    MessageType
            => MessageTypeName;

        /// <summary>
        /// The identification of the instruction (as provided by the CEM).
        /// </summary>
        [Mandatory]
        public Instruction_Id     InstructionId    { get; }

        /// <summary>
        /// The present status of this instruction.
        /// </summary>
        [Mandatory]
        public InstructionStatus  StatusType       { get; }

        /// <summary>
        /// The timestamp when the status type has changed the last time.
        /// </summary>
        [Mandatory]
        public DateTimeOffset     Timestamp        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new instruction status update.
        /// </summary>
        /// <param name="InstructionId">The identification of the instruction (as provided by the CEM).</param>
        /// <param name="StatusType">The present status of this instruction.</param>
        /// <param name="Timestamp">The timestamp when the status type has changed the last time.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public InstructionStatusUpdate(Instruction_Id     InstructionId,
                                       InstructionStatus  StatusType,
                                       DateTimeOffset     Timestamp,
                                       Message_Id?        MessageId   = null)

            : base(MessageId)

        {

            if (InstructionId.IsNullOrEmpty)
                throw new ArgumentException("The instruction identification must not be null or empty!",
                                            nameof(InstructionId));

            if (StatusType.IsNullOrEmpty)
                throw new ArgumentException("The instruction status must not be null or empty!",
                                            nameof(StatusType));

            this.InstructionId  = InstructionId;
            this.StatusType     = StatusType;
            this.Timestamp      = Timestamp;

            unchecked
            {
                hashCode = this.MessageId.    GetHashCode() * 7 ^
                           this.InstructionId.GetHashCode() * 5 ^
                           this.StatusType.   GetHashCode() * 3 ^
                           this.Timestamp.    GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // InstructionStatusUpdate.schema.json
        //   "title": "InstructionStatusUpdate",
        //   "properties": {
        //     "message_type":   { "type": "string", "const": "InstructionStatusUpdate" },
        //     "message_id":     { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "instruction_id": { "$ref": "../schemas/ID.schema.json",
        //                         "description": "ID of this instruction (as provided by the CEM) " },
        //     "status_type":    { "$ref": "../schemas/InstructionStatus.schema.json",
        //                         "description": "Present status of this instruction." },
        //     "timestamp":      { "type": "string", "format": "date-time",
        //                         "description": "Timestamp when status_type has changed the last time." }
        //   },
        //   "required": ["message_type", "message_id", "instruction_id", "status_type", "timestamp"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out InstructionStatusUpdate, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an instruction status update.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="InstructionStatusUpdate">The parsed instruction status update.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out InstructionStatusUpdate?  InstructionStatusUpdate,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)

            => TryParse(JSON,
                        out InstructionStatusUpdate,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an instruction status update.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="InstructionStatusUpdate">The parsed instruction status update.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out InstructionStatusUpdate?  InstructionStatusUpdate,
                                       [NotNullWhen(false)] out String?                   ErrorResponse,
                                       S2ParserOptions?                                   Options)

            => TryParse(JSON,
                        out InstructionStatusUpdate,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an instruction status update.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="InstructionStatusUpdate">The parsed instruction status update.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomInstructionStatusUpdateParser">A delegate to parse custom instruction status updates.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out InstructionStatusUpdate?      InstructionStatusUpdate,
                                       [NotNullWhen(false)] out String?                       ErrorResponse,
                                       S2ParserOptions?                                       Options,
                                       CustomJObjectParserDelegate<InstructionStatusUpdate>?  CustomInstructionStatusUpdateParser)
        {

            try
            {

                InstructionStatusUpdate = null;

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

                #region instruction_id             [mandatory]

                if (!JSON.ParseMandatoryS2Id("instruction_id",
                                             "instruction identification",
                                             Instruction_Id.TryParse,
                                             Options,
                                             out Instruction_Id instructionId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region status_type                [mandatory]

                if (!JSON.ParseMandatoryS2Enum("status_type",
                                               "instruction status",
                                               InstructionStatus.TryParse,
                                               Options,
                                               out InstructionStatus statusType,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region timestamp                  [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("timestamp",
                                                    "timestamp",
                                                    Options,
                                                    out DateTimeOffset timestamp,
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
                                                    "instruction_id",
                                                    "status_type",
                                                    "timestamp"))
                {
                    return false;
                }

                #endregion


                InstructionStatusUpdate = new InstructionStatusUpdate(
                                              instructionId,
                                              statusType,
                                              timestamp,
                                              messageId
                                          );

                if (CustomInstructionStatusUpdateParser is not null)
                    InstructionStatusUpdate = CustomInstructionStatusUpdateParser(JSON,
                                                                                  InstructionStatusUpdate);

                return true;

            }
            catch (Exception e)
            {
                InstructionStatusUpdate  = null;
                ErrorResponse            = "The given JSON representation of an instruction status update is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomInstructionStatusUpdateSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomInstructionStatusUpdateSerializer">A delegate to serialize custom instruction status updates.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<InstructionStatusUpdate>? CustomInstructionStatusUpdateSerializer)
        {

            var json = CreateJSON(
                           new JProperty("instruction_id",  InstructionId.ToString()),
                           new JProperty("status_type",     StatusType.   ToString()),
                           new JProperty("timestamp",       Timestamp.    ToS2Timestamp())
                       );

            return CustomInstructionStatusUpdateSerializer is not null
                       ? CustomInstructionStatusUpdateSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this instruction status update.
        /// </summary>
        public InstructionStatusUpdate Clone()

            => new (
                   InstructionId.Clone(),
                   StatusType.   Clone(),
                   Timestamp,
                   MessageId.    Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two instruction status updates for equality.
        /// </summary>
        public static Boolean operator == (InstructionStatusUpdate? InstructionStatusUpdate1, InstructionStatusUpdate? InstructionStatusUpdate2)
        {

            if (ReferenceEquals(InstructionStatusUpdate1, InstructionStatusUpdate2))
                return true;

            if (InstructionStatusUpdate1 is null || InstructionStatusUpdate2 is null)
                return false;

            return InstructionStatusUpdate1.Equals(InstructionStatusUpdate2);

        }

        /// <summary>
        /// Compares two instruction status updates for inequality.
        /// </summary>
        public static Boolean operator != (InstructionStatusUpdate? InstructionStatusUpdate1, InstructionStatusUpdate? InstructionStatusUpdate2)
            => !(InstructionStatusUpdate1 == InstructionStatusUpdate2);

        #endregion

        #region IEquatable<InstructionStatusUpdate> Members

        /// <summary>
        /// Compares two instruction status updates for equality.
        /// </summary>
        /// <param name="Object">An instruction status update to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is InstructionStatusUpdate instructionStatusUpdate && Equals(instructionStatusUpdate);

        /// <summary>
        /// Compares two instruction status updates for equality.
        /// </summary>
        /// <param name="InstructionStatusUpdate">An instruction status update to compare with.</param>
        public Boolean Equals(InstructionStatusUpdate? InstructionStatusUpdate)

            => InstructionStatusUpdate is not null &&

               MessageId.    Equals(InstructionStatusUpdate.MessageId)     &&
               InstructionId.Equals(InstructionStatusUpdate.InstructionId) &&
               StatusType.   Equals(InstructionStatusUpdate.StatusType)    &&
               Timestamp.    Equals(InstructionStatusUpdate.Timestamp);

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
            => $"InstructionStatusUpdate of '{InstructionId}': {StatusType} at {Timestamp.ToS2Timestamp()} [{MessageId}]";

        #endregion

    }

}

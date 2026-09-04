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
    /// The Resource Manager offers the CEM a power profile: a chronological list of power
    /// sequence containers, each holding alternative power sequences of which the CEM
    /// selects and schedules one.
    /// </summary>
    public sealed class PPBC_PowerProfileDefinition : AS2Message,
                                                      IRevokable,
                                                      IEquatable<PPBC_PowerProfileDefinition>
    {

        #region Data

        /// <summary>
        /// The message type "PPBC.PowerProfileDefinition".
        /// </summary>
        public const String MessageTypeName = "PPBC.PowerProfileDefinition";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "PPBC.PowerProfileDefinition".
        /// </summary>
        public override String                                 MessageType
            => MessageTypeName;

        /// <summary>
        /// The identification of this power profile definition. Must be unique in the scope
        /// of the Resource Manager, for at least the duration of the session between
        /// Resource Manager and CEM.
        /// </summary>
        [Mandatory]
        public PowerProfileDefinition_Id                       Id                          { get; }

        /// <summary>
        /// The first possible time the first PPBC.PowerSequence could start.
        /// </summary>
        [Mandatory]
        public DateTimeOffset                                  StartTime                   { get; }

        /// <summary>
        /// When the last PPBC.PowerSequence shall be finished at the latest.
        /// </summary>
        [Mandatory]
        public DateTimeOffset                                  EndTime                     { get; }

        /// <summary>
        /// The PPBC.PowerSequenceContainers that make up this power profile definition.
        /// There shall be at least one container that includes at least one power sequence.
        /// The containers must be placed in chronological order.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PPBC_PowerSequenceContainer>      PowerSequencesContainers    { get; }

        /// <summary>
        /// The revokable object type "PPBC.PowerProfileDefinition".
        /// </summary>
        public RevokableObject                                 RevokableObjectType
            => RevokableObject.PPBC_PowerProfileDefinition;

        /// <summary>
        /// The identification a RevokeObject uses to refer to this power profile definition (its "id").
        /// </summary>
        public S2Object_Id                                     RevokableObjectId
            => S2Object_Id.From(Id);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new PPBC power profile definition.
        /// </summary>
        /// <param name="Id">The identification of this power profile definition (unique in the scope of the Resource Manager).</param>
        /// <param name="StartTime">The first possible time the first PPBC.PowerSequence could start.</param>
        /// <param name="EndTime">When the last PPBC.PowerSequence shall be finished at the latest (not before the start time).</param>
        /// <param name="PowerSequencesContainers">The PPBC.PowerSequenceContainers that make up this power profile definition (1..1000 entries, unique ids, chronological order).</param>
        /// <param name="MessageId">An optional message identification.</param>
        public PPBC_PowerProfileDefinition(PowerProfileDefinition_Id                   Id,
                                           DateTimeOffset                              StartTime,
                                           DateTimeOffset                              EndTime,
                                           IReadOnlyList<PPBC_PowerSequenceContainer>  PowerSequencesContainers,
                                           Message_Id?                                 MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(PowerSequencesContainers);

            if (EndTime < StartTime)
                throw new ArgumentException($"The end time '{EndTime.ToS2Timestamp()}' must not be before the start time '{StartTime.ToS2Timestamp()}'!",
                                            nameof(EndTime));

            if (PowerSequencesContainers.Count < 1)
                throw new ArgumentException("The power sequences containers must contain at least one entry!",
                                            nameof(PowerSequencesContainers));

            if (PowerSequencesContainers.Count > 1000)
                throw new ArgumentException($"The power sequences containers must not contain more than 1000 entries, but {PowerSequencesContainers.Count} were given!",
                                            nameof(PowerSequencesContainers));

            var duplicate = PowerSequencesContainers.GroupBy(container => container.Id).
                                                     FirstOrDefault(group => group.Count() > 1);

            if (duplicate is not null)
                throw new ArgumentException($"The power sequence container identifications must be unique within the power profile definition, but '{duplicate.Key}' occurs {duplicate.Count()} times!",
                                            nameof(PowerSequencesContainers));

            this.Id                        = Id;
            this.StartTime                 = StartTime;
            this.EndTime                   = EndTime;
            this.PowerSequencesContainers  = [.. PowerSequencesContainers];

            unchecked
            {
                hashCode = this.MessageId.               GetHashCode()  * 11 ^
                           this.Id.                      GetHashCode()  *  7 ^
                           this.StartTime.               GetHashCode()  *  5 ^
                           this.EndTime.                 GetHashCode()  *  3 ^
                           this.PowerSequencesContainers.CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // PPBC.PowerProfileDefinition.schema.json
        //   "title": "PPBC_PowerProfileDefinition",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "PPBC.PowerProfileDefinition" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "id":           { "$ref": "../schemas/ID.schema.json",
        //                       "description": "ID of the PPBC.PowerProfileDefinition. Must be unique in the scope of the Resource Manager,
        //                                       for at least the duration of the session between Resource Manager and CEM." },
        //     "start_time":   { "type": "string", "format": "date-time",
        //                       "description": "Indicates the first possible time the first PPBC.PowerSequence could start" },
        //     "end_time":     { "type": "string", "format": "date-time",
        //                       "description": "Indicates when the last PPBC.PowerSequence shall be finished at the latest" },
        //     "power_sequences_containers": {
        //       "type": "array", "minItems": 1, "maxItems": 1000,
        //       "items": { "$ref": "../schemas/PPBC.PowerSequenceContainer.schema.json" },
        //       "description": "The PPBC.PowerSequenceContainers that make up this PPBC.PowerProfileDefinition. There shall be at
        //                       least one PPBC.PowerSequenceContainer that includes at least one PPBC.PowerSequence.
        //                       PPBC.PowerSequenceContainers must be placed in chronological order."
        //     }
        //   },
        //   "required": ["message_type", "message_id", "id", "start_time", "end_time", "power_sequences_containers"],
        //   "additionalProperties": false
        //
        // Semantic rules (CONVENTIONS.md §8): end_time >= start_time; the power sequence container
        // identifications are unique within the power profile definition.
        // Note the wire name "power_sequences_containers" (plural "sequences") is kept verbatim.

        #endregion

        #region (static) TryParse(JSON, out PPBC_PowerProfileDefinition, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power profile definition.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerProfileDefinition">The parsed PPBC power profile definition.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerProfileDefinition?  PPBC_PowerProfileDefinition,
                                       [NotNullWhen(false)] out String?                       ErrorResponse)

            => TryParse(JSON,
                        out PPBC_PowerProfileDefinition,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power profile definition.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerProfileDefinition">The parsed PPBC power profile definition.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerProfileDefinition?  PPBC_PowerProfileDefinition,
                                       [NotNullWhen(false)] out String?                       ErrorResponse,
                                       S2ParserOptions?                                       Options)

            => TryParse(JSON,
                        out PPBC_PowerProfileDefinition,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power profile definition.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerProfileDefinition">The parsed PPBC power profile definition.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPPBC_PowerProfileDefinitionParser">A delegate to parse custom PPBC power profile definitions.</param>
        public static Boolean TryParse(JObject                                                    JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerProfileDefinition?      PPBC_PowerProfileDefinition,
                                       [NotNullWhen(false)] out String?                           ErrorResponse,
                                       S2ParserOptions?                                           Options,
                                       CustomJObjectParserDelegate<PPBC_PowerProfileDefinition>?  CustomPPBC_PowerProfileDefinitionParser)
        {

            try
            {

                PPBC_PowerProfileDefinition = null;

                #region message_type, message_id      [mandatory]

                if (!TryParseHeader(JSON,
                                    MessageTypeName,
                                    Options,
                                    out var messageId,
                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region id                            [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
                                             "power profile definition identification",
                                             PowerProfileDefinition_Id.TryParse,
                                             Options,
                                             out PowerProfileDefinition_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region start_time                    [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("start_time",
                                                    "start time",
                                                    Options,
                                                    out DateTimeOffset startTime,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region end_time                      [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("end_time",
                                                    "end time",
                                                    Options,
                                                    out DateTimeOffset endTime,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region power_sequences_containers    [mandatory]

                if (!JSON.ParseMandatoryS2List("power_sequences_containers",
                                               "power sequences containers",
                                               PPBC_PowerSequenceContainer.TryParse,
                                               Options,
                                               1,
                                               1000,
                                               out IReadOnlyList<PPBC_PowerSequenceContainer>? powerSequencesContainers,
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
                                                    "start_time",
                                                    "end_time",
                                                    "power_sequences_containers"))
                {
                    return false;
                }

                #endregion


                PPBC_PowerProfileDefinition = new PPBC_PowerProfileDefinition(
                                                  id,
                                                  startTime,
                                                  endTime,
                                                  powerSequencesContainers,
                                                  messageId
                                              );

                if (CustomPPBC_PowerProfileDefinitionParser is not null)
                    PPBC_PowerProfileDefinition = CustomPPBC_PowerProfileDefinitionParser(JSON,
                                                                                          PPBC_PowerProfileDefinition);

                return true;

            }
            catch (Exception e)
            {
                PPBC_PowerProfileDefinition  = null;
                ErrorResponse                = "The given JSON representation of a PPBC power profile definition is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPPBC_PowerProfileDefinitionSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomPPBC_PowerProfileDefinitionSerializer">A delegate to serialize custom PPBC power profile definitions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PPBC_PowerProfileDefinition>? CustomPPBC_PowerProfileDefinitionSerializer)
        {

            var json = CreateJSON(
                           new JProperty("id",                          Id.       ToString()),
                           new JProperty("start_time",                  StartTime.ToS2Timestamp()),
                           new JProperty("end_time",                    EndTime.  ToS2Timestamp()),
                           new JProperty("power_sequences_containers",  new JArray(PowerSequencesContainers.Select(container => container.ToJSON())))
                       );

            return CustomPPBC_PowerProfileDefinitionSerializer is not null
                       ? CustomPPBC_PowerProfileDefinitionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this PPBC power profile definition.
        /// </summary>
        public PPBC_PowerProfileDefinition Clone()

            => new (
                   Id.Clone(),
                   StartTime,
                   EndTime,
                   [.. PowerSequencesContainers.Select(container => container.Clone())],
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two PPBC power profile definitions for equality.
        /// </summary>
        public static Boolean operator == (PPBC_PowerProfileDefinition? PPBC_PowerProfileDefinition1, PPBC_PowerProfileDefinition? PPBC_PowerProfileDefinition2)
        {

            if (ReferenceEquals(PPBC_PowerProfileDefinition1, PPBC_PowerProfileDefinition2))
                return true;

            if (PPBC_PowerProfileDefinition1 is null || PPBC_PowerProfileDefinition2 is null)
                return false;

            return PPBC_PowerProfileDefinition1.Equals(PPBC_PowerProfileDefinition2);

        }

        /// <summary>
        /// Compares two PPBC power profile definitions for inequality.
        /// </summary>
        public static Boolean operator != (PPBC_PowerProfileDefinition? PPBC_PowerProfileDefinition1, PPBC_PowerProfileDefinition? PPBC_PowerProfileDefinition2)
            => !(PPBC_PowerProfileDefinition1 == PPBC_PowerProfileDefinition2);

        #endregion

        #region IEquatable<PPBC_PowerProfileDefinition> Members

        /// <summary>
        /// Compares two PPBC power profile definitions for equality.
        /// </summary>
        /// <param name="Object">A PPBC power profile definition to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PPBC_PowerProfileDefinition ppbcPowerProfileDefinition && Equals(ppbcPowerProfileDefinition);

        /// <summary>
        /// Compares two PPBC power profile definitions for equality.
        /// </summary>
        /// <param name="PPBC_PowerProfileDefinition">A PPBC power profile definition to compare with.</param>
        public Boolean Equals(PPBC_PowerProfileDefinition? PPBC_PowerProfileDefinition)

            => PPBC_PowerProfileDefinition is not null &&

               MessageId.               Equals       (PPBC_PowerProfileDefinition.MessageId) &&
               Id.                      Equals       (PPBC_PowerProfileDefinition.Id)        &&
               StartTime.               Equals       (PPBC_PowerProfileDefinition.StartTime) &&
               EndTime.                 Equals       (PPBC_PowerProfileDefinition.EndTime)   &&
               PowerSequencesContainers.SequenceEqual(PPBC_PowerProfileDefinition.PowerSequencesContainers);

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

            => $"PPBC.PowerProfileDefinition {Id}: {PowerSequencesContainers.Count} container(s) from {StartTime.ToS2Timestamp()} to {EndTime.ToS2Timestamp()} [{MessageId}]";

        #endregion

    }

}

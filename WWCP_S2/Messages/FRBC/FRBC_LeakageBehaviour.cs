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
    /// The Resource Manager describes how fast the fill level of the storage decreases
    /// due to leakage, depending on the current fill level.
    /// </summary>
    public sealed class FRBC_LeakageBehaviour : AS2Message,
                                                IEquatable<FRBC_LeakageBehaviour>
    {

        #region Data

        /// <summary>
        /// The message type "FRBC.LeakageBehaviour".
        /// </summary>
        public const String MessageTypeName = "FRBC.LeakageBehaviour";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "FRBC.LeakageBehaviour".
        /// </summary>
        public override String                             MessageType
            => MessageTypeName;

        /// <summary>
        /// The moment this leakage behaviour starts to be valid. If the leakage behaviour
        /// is immediately valid, the timestamp should be now or in the past.
        /// </summary>
        [Mandatory]
        public DateTimeOffset                              ValidFrom    { get; }

        /// <summary>
        /// The list of elements that model the leakage behaviour of the buffer.
        /// The fill level ranges of the elements are contiguous.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<FRBC_LeakageBehaviourElement>  Elements     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new FRBC leakage behaviour.
        /// </summary>
        /// <param name="ValidFrom">The moment this leakage behaviour starts to be valid.</param>
        /// <param name="Elements">The elements modelling the leakage behaviour of the buffer (1..288 entries, contiguous fill level ranges).</param>
        /// <param name="MessageId">An optional message identification.</param>
        public FRBC_LeakageBehaviour(DateTimeOffset                              ValidFrom,
                                     IReadOnlyList<FRBC_LeakageBehaviourElement>  Elements,
                                     Message_Id?                                 MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(Elements);

            if (Elements.Count < 1)
                throw new ArgumentException("An FRBC leakage behaviour must contain at least one element!",
                                            nameof(Elements));

            if (Elements.Count > 288)
                throw new ArgumentException($"An FRBC leakage behaviour must not contain more than 288 elements, but {Elements.Count} were given!",
                                            nameof(Elements));

            var sortedElements = Elements.OrderBy(element => element.FillLevelRange.StartOfRange).ToArray();

            for (var i = 0; i < sortedElements.Length - 1; i++)
            {
                if (sortedElements[i].FillLevelRange.EndOfRange != sortedElements[i + 1].FillLevelRange.StartOfRange)
                    throw new ArgumentException($"The fill level ranges of the elements of an FRBC leakage behaviour must be contiguous, but the range {sortedElements[i].FillLevelRange} is followed by {sortedElements[i + 1].FillLevelRange}!",
                                                nameof(Elements));
            }

            this.ValidFrom  = ValidFrom;
            this.Elements   = [.. Elements];

            unchecked
            {
                hashCode = this.MessageId.GetHashCode()  * 5 ^
                           this.ValidFrom.GetHashCode()  * 3 ^
                           this.Elements. CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // FRBC.LeakageBehaviour.schema.json
        //   "title": "FRBC_LeakageBehaviour",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "FRBC.LeakageBehaviour" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "valid_from":   { "type": "string", "format": "date-time",
        //                       "description": "Moment this FRBC.LeakageBehaviour starts to be valid. If the FRBC.LeakageBehaviour
        //                                       is immediately valid, the DateTimeStamp should be now or in the past." },
        //     "elements":     { "type": "array", "minItems": 1, "maxItems": 288,
        //                       "items": { "$ref": "../schemas/FRBC.LeakageBehaviourElement.schema.json" },
        //                       "description": "List of elements that model the leakage behaviour of the buffer.
        //                                       The fill_level_ranges of the elements must be contiguous." }
        //   },
        //   "required": ["message_type", "message_id", "valid_from", "elements"],
        //   "additionalProperties": false
        //
        // Semantic rule (CONVENTIONS.md §8): the fill level ranges of the elements are contiguous, i.e. sorted by
        // start_of_range, each element's end_of_range equals the next element's start_of_range.

        #endregion

        #region (static) TryParse(JSON, out FRBC_LeakageBehaviour, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an FRBC leakage behaviour.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="LeakageBehaviour">The parsed FRBC leakage behaviour.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out FRBC_LeakageBehaviour?  LeakageBehaviour,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)

            => TryParse(JSON,
                        out LeakageBehaviour,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC leakage behaviour.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="LeakageBehaviour">The parsed FRBC leakage behaviour.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out FRBC_LeakageBehaviour?  LeakageBehaviour,
                                       [NotNullWhen(false)] out String?                 ErrorResponse,
                                       S2ParserOptions?                                 Options)

            => TryParse(JSON,
                        out LeakageBehaviour,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC leakage behaviour.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="LeakageBehaviour">The parsed FRBC leakage behaviour.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomLeakageBehaviourParser">A delegate to parse custom FRBC leakage behaviours.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out FRBC_LeakageBehaviour?      LeakageBehaviour,
                                       [NotNullWhen(false)] out String?                     ErrorResponse,
                                       S2ParserOptions?                                     Options,
                                       CustomJObjectParserDelegate<FRBC_LeakageBehaviour>?  CustomLeakageBehaviourParser)
        {

            try
            {

                LeakageBehaviour = null;

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

                #region valid_from                 [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("valid_from",
                                                    "valid from timestamp",
                                                    Options,
                                                    out DateTimeOffset validFrom,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region elements                   [mandatory]

                if (!JSON.ParseMandatoryS2List("elements",
                                               "leakage behaviour elements",
                                               FRBC_LeakageBehaviourElement.TryParse,
                                               Options,
                                               1,
                                               288,
                                               out IReadOnlyList<FRBC_LeakageBehaviourElement>? elements,
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
                                                    "valid_from",
                                                    "elements"))
                {
                    return false;
                }

                #endregion


                LeakageBehaviour = new FRBC_LeakageBehaviour(
                                       validFrom,
                                       elements,
                                       messageId
                                   );

                if (CustomLeakageBehaviourParser is not null)
                    LeakageBehaviour = CustomLeakageBehaviourParser(JSON,
                                                                    LeakageBehaviour);

                return true;

            }
            catch (Exception e)
            {
                LeakageBehaviour  = null;
                ErrorResponse     = "The given JSON representation of an FRBC leakage behaviour is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomLeakageBehaviourSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomLeakageBehaviourSerializer">A delegate to serialize custom FRBC leakage behaviours.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_LeakageBehaviour>? CustomLeakageBehaviourSerializer)
        {

            var json = CreateJSON(
                           new JProperty("valid_from",  ValidFrom.ToS2Timestamp()),
                           new JProperty("elements",    new JArray(Elements.Select(element => element.ToJSON())))
                       );

            return CustomLeakageBehaviourSerializer is not null
                       ? CustomLeakageBehaviourSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this FRBC leakage behaviour.
        /// </summary>
        public FRBC_LeakageBehaviour Clone()

            => new (
                   ValidFrom,
                   Elements. Select(element => element.Clone()).ToList(),
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two FRBC leakage behaviours for equality.
        /// </summary>
        public static Boolean operator == (FRBC_LeakageBehaviour? LeakageBehaviour1, FRBC_LeakageBehaviour? LeakageBehaviour2)
        {

            if (ReferenceEquals(LeakageBehaviour1, LeakageBehaviour2))
                return true;

            if (LeakageBehaviour1 is null || LeakageBehaviour2 is null)
                return false;

            return LeakageBehaviour1.Equals(LeakageBehaviour2);

        }

        /// <summary>
        /// Compares two FRBC leakage behaviours for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_LeakageBehaviour? LeakageBehaviour1, FRBC_LeakageBehaviour? LeakageBehaviour2)
            => !(LeakageBehaviour1 == LeakageBehaviour2);

        #endregion

        #region IEquatable<FRBC_LeakageBehaviour> Members

        /// <summary>
        /// Compares two FRBC leakage behaviours for equality.
        /// </summary>
        /// <param name="Object">An FRBC leakage behaviour to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_LeakageBehaviour leakageBehaviour && Equals(leakageBehaviour);

        /// <summary>
        /// Compares two FRBC leakage behaviours for equality.
        /// </summary>
        /// <param name="LeakageBehaviour">An FRBC leakage behaviour to compare with.</param>
        public Boolean Equals(FRBC_LeakageBehaviour? LeakageBehaviour)

            => LeakageBehaviour is not null &&

               MessageId.Equals(LeakageBehaviour.MessageId) &&
               ValidFrom.Equals(LeakageBehaviour.ValidFrom) &&
               Elements. SequenceEqual(LeakageBehaviour.Elements);

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
            => $"FRBC.LeakageBehaviour valid from {ValidFrom.ToS2Timestamp()} with {Elements.Count} element(s) [{MessageId}]";

        #endregion

    }

}

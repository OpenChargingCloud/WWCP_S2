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
    /// The description of an Operation Mode Based Control (OMBC) system sent by the Resource
    /// Manager: the operation modes the CEM can choose from, the transitions between them and
    /// the timers constraining those transitions. Revokable by its message identification.
    /// </summary>
    public sealed class OMBC_SystemDescription : AS2Message,
                                                 IRevokable,
                                                 IEquatable<OMBC_SystemDescription>
    {

        #region Data

        /// <summary>
        /// The message type "OMBC.SystemDescription".
        /// </summary>
        public const String MessageTypeName = "OMBC.SystemDescription";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "OMBC.SystemDescription".
        /// </summary>
        public override String                     MessageType
            => MessageTypeName;

        /// <summary>
        /// The moment this OMBC.SystemDescription starts to be valid. If the system description
        /// is immediately valid, the timestamp should be now or in the past.
        /// </summary>
        [Mandatory]
        public DateTimeOffset                      ValidFrom         { get; }

        /// <summary>
        /// The OMBC.OperationModes available for the CEM in order to coordinate the device behaviour.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<OMBC_OperationMode>   OperationModes    { get; }

        /// <summary>
        /// The possible transitions to switch from one OMBC.OperationMode to another.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<Transition>           Transitions       { get; }

        /// <summary>
        /// The timers that control when certain transitions can be made.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<Timer>                Timers            { get; }


        /// <summary>
        /// The revokable object type "OMBC.SystemDescription".
        /// </summary>
        public RevokableObject                     RevokableObjectType
            => RevokableObject.OMBC_SystemDescription;

        /// <summary>
        /// The identification a RevokeObject uses to refer to this message: its message identification,
        /// as a system description carries no identification of its own.
        /// </summary>
        public S2Object_Id                         RevokableObjectId
            => S2Object_Id.From(MessageId);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new OMBC system description.
        /// </summary>
        /// <param name="ValidFrom">The moment this OMBC.SystemDescription starts to be valid.</param>
        /// <param name="OperationModes">The OMBC.OperationModes available for the CEM (1..100 entries with unique identifications).</param>
        /// <param name="Transitions">The possible transitions between operation modes (0..1000 entries with unique identifications).</param>
        /// <param name="Timers">The timers that control when certain transitions can be made (0..1000 entries with unique identifications).</param>
        /// <param name="MessageId">An optional message identification.</param>
        public OMBC_SystemDescription(DateTimeOffset                     ValidFrom,
                                      IReadOnlyList<OMBC_OperationMode>  OperationModes,
                                      IReadOnlyList<Transition>          Transitions,
                                      IReadOnlyList<Timer>               Timers,
                                      Message_Id?                        MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(OperationModes);
            ArgumentNullException.ThrowIfNull(Transitions);
            ArgumentNullException.ThrowIfNull(Timers);

            #region Operation modes: 1..100, unique ids

            if (OperationModes.Count < 1)
                throw new ArgumentException("An OMBC system description must contain at least one operation mode!",
                                            nameof(OperationModes));

            if (OperationModes.Count > 100)
                throw new ArgumentException($"An OMBC system description must not contain more than 100 operation modes, but {OperationModes.Count} were given!",
                                            nameof(OperationModes));

            var duplicateOperationModeId = OperationModes.
                                               GroupBy(operationMode => operationMode.Id).
                                               Select (group         => (group.Key, Count: group.Count())).
                                               FirstOrDefault(group => group.Count > 1);

            if (duplicateOperationModeId.Count > 1)
                throw new ArgumentException($"The operation mode identifications of an OMBC system description must be unique, but '{duplicateOperationModeId.Key}' occurs {duplicateOperationModeId.Count} times!",
                                            nameof(OperationModes));

            #endregion

            #region Timers: 0..1000, unique ids

            if (Timers.Count > 1000)
                throw new ArgumentException($"An OMBC system description must not contain more than 1000 timers, but {Timers.Count} were given!",
                                            nameof(Timers));

            var duplicateTimerId = Timers.
                                       GroupBy(timer => timer.Id).
                                       Select (group => (group.Key, Count: group.Count())).
                                       FirstOrDefault(group => group.Count > 1);

            if (duplicateTimerId.Count > 1)
                throw new ArgumentException($"The timer identifications of an OMBC system description must be unique, but '{duplicateTimerId.Key}' occurs {duplicateTimerId.Count} times!",
                                            nameof(Timers));

            #endregion

            #region Transitions: 0..1000, unique ids, references to declared operation modes and timers

            if (Transitions.Count > 1000)
                throw new ArgumentException($"An OMBC system description must not contain more than 1000 transitions, but {Transitions.Count} were given!",
                                            nameof(Transitions));

            var duplicateTransitionId = Transitions.
                                            GroupBy(transition => transition.Id).
                                            Select (group      => (group.Key, Count: group.Count())).
                                            FirstOrDefault(group => group.Count > 1);

            if (duplicateTransitionId.Count > 1)
                throw new ArgumentException($"The transition identifications of an OMBC system description must be unique, but '{duplicateTransitionId.Key}' occurs {duplicateTransitionId.Count} times!",
                                            nameof(Transitions));

            var operationModeIds = OperationModes.Select(operationMode => operationMode.Id).ToHashSet();
            var timerIds         = Timers.        Select(timer         => timer.Id).        ToHashSet();

            foreach (var transition in Transitions)
            {

                if (!operationModeIds.Contains(transition.From))
                    throw new ArgumentException($"The transition '{transition.Id}' references the unknown operation mode '{transition.From}' in 'from'!",
                                                nameof(Transitions));

                if (!operationModeIds.Contains(transition.To))
                    throw new ArgumentException($"The transition '{transition.Id}' references the unknown operation mode '{transition.To}' in 'to'!",
                                                nameof(Transitions));

                foreach (var startTimer in transition.StartTimers)
                {
                    if (!timerIds.Contains(startTimer))
                        throw new ArgumentException($"The transition '{transition.Id}' references the unknown timer '{startTimer}' in 'start_timers'!",
                                                    nameof(Transitions));
                }

                foreach (var blockingTimer in transition.BlockingTimers)
                {
                    if (!timerIds.Contains(blockingTimer))
                        throw new ArgumentException($"The transition '{transition.Id}' references the unknown timer '{blockingTimer}' in 'blocking_timers'!",
                                                    nameof(Transitions));
                }

            }

            #endregion

            this.ValidFrom       = ValidFrom;
            this.OperationModes  = [.. OperationModes];
            this.Transitions     = [.. Transitions];
            this.Timers          = [.. Timers];

            unchecked
            {
                hashCode = this.MessageId.     GetHashCode()  * 11 ^
                           this.ValidFrom.     GetHashCode()  *  7 ^
                           this.OperationModes.CalcHashCode() *  5 ^
                           this.Transitions.   CalcHashCode() *  3 ^
                           this.Timers.        CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // OMBC.SystemDescription.schema.json
        //   "title": "OMBC_SystemDescription",
        //   "properties": {
        //     "message_type":    { "type": "string", "const": "OMBC.SystemDescription" },
        //     "message_id":      { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "valid_from":      { "type": "string", "format": "date-time",
        //                          "description": "Moment this OMBC.SystemDescription starts to be valid. If the system description
        //                                          is immediately valid, the DateTimeStamp should be now or in the past." },
        //     "operation_modes": { "type": "array", "minItems": 1, "maxItems": 100,
        //                          "items": { "$ref": "../schemas/OMBC.OperationMode.schema.json" },
        //                          "description": "OMBC.OperationModes available for the CEM in order to coordinate the device behaviour." },
        //     "transitions":     { "type": "array", "minItems": 0, "maxItems": 1000,
        //                          "items": { "$ref": "../schemas/Transition.schema.json" },
        //                          "description": "Possible transitions to switch from one OMBC.OperationMode to another." },
        //     "timers":          { "type": "array", "minItems": 0, "maxItems": 1000,
        //                          "items": { "$ref": "../schemas/Timer.schema.json" },
        //                          "description": "Timers that control when certain transitions can be made." }
        //   },
        //   "required": ["message_type", "message_id", "valid_from", "operation_modes", "transitions", "timers"],
        //   "additionalProperties": false
        //
        // Semantic rules (CONVENTIONS.md §5 and §8):
        //   - Operation mode ids, transition ids and timer ids are unique within the system description.
        //   - Every transition's from/to references a declared operation mode.
        //   - Every start_timers/blocking_timers entry of a transition references a declared timer.
        //   - "At most one PowerRange per CommodityQuantity" is checked by OMBC_OperationMode itself; the
        //     commodities of the ResourceManagerDetails roles are unknown at message level, so no
        //     supported-commodity check is performed here (session layer).
        //   - Revokable via RevokeObject with object_type "OMBC.SystemDescription" and object_id = message_id.

        #endregion

        #region (static) TryParse(JSON, out OMBC_SystemDescription, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an OMBC system description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCSystemDescription">The parsed OMBC system description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out OMBC_SystemDescription?  OMBCSystemDescription,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)

            => TryParse(JSON,
                        out OMBCSystemDescription,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an OMBC system description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCSystemDescription">The parsed OMBC system description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out OMBC_SystemDescription?  OMBCSystemDescription,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options)

            => TryParse(JSON,
                        out OMBCSystemDescription,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an OMBC system description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCSystemDescription">The parsed OMBC system description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomOMBCSystemDescriptionParser">A delegate to parse custom OMBC system descriptions.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out OMBC_SystemDescription?      OMBCSystemDescription,
                                       [NotNullWhen(false)] out String?                      ErrorResponse,
                                       S2ParserOptions?                                      Options,
                                       CustomJObjectParserDelegate<OMBC_SystemDescription>?  CustomOMBCSystemDescriptionParser)
        {

            try
            {

                OMBCSystemDescription = null;

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

                #region operation_modes            [mandatory]

                if (!JSON.ParseMandatoryS2List("operation_modes",
                                               "operation modes",
                                               OMBC_OperationMode.TryParse,
                                               Options,
                                               1,
                                               100,
                                               out IReadOnlyList<OMBC_OperationMode>? operationModes,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region transitions                [mandatory]

                if (!JSON.ParseMandatoryS2List("transitions",
                                               "transitions",
                                               Transition.TryParse,
                                               Options,
                                               0,
                                               1000,
                                               out IReadOnlyList<Transition>? transitions,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region timers                     [mandatory]

                if (!JSON.ParseMandatoryS2List("timers",
                                               "timers",
                                               Timer.TryParse,
                                               Options,
                                               0,
                                               1000,
                                               out IReadOnlyList<Timer>? timers,
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
                                                    "operation_modes",
                                                    "transitions",
                                                    "timers"))
                {
                    return false;
                }

                #endregion


                OMBCSystemDescription = new OMBC_SystemDescription(
                                            validFrom,
                                            operationModes,
                                            transitions,
                                            timers,
                                            messageId
                                        );

                if (CustomOMBCSystemDescriptionParser is not null)
                    OMBCSystemDescription = CustomOMBCSystemDescriptionParser(JSON,
                                                                              OMBCSystemDescription);

                return true;

            }
            catch (Exception e)
            {
                OMBCSystemDescription  = null;
                ErrorResponse          = "The given JSON representation of an OMBC system description is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomOMBCSystemDescriptionSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomOMBCSystemDescriptionSerializer">A delegate to serialize custom OMBC system descriptions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<OMBC_SystemDescription>? CustomOMBCSystemDescriptionSerializer)
        {

            var json = CreateJSON(

                           new JProperty("valid_from",       ValidFrom.ToS2Timestamp()),
                           new JProperty("operation_modes",  new JArray(OperationModes.Select(operationMode => operationMode.ToJSON()))),
                           new JProperty("transitions",      new JArray(Transitions.   Select(transition    => transition.   ToJSON()))),
                           new JProperty("timers",           new JArray(Timers.        Select(timer         => timer.        ToJSON())))

                       );

            return CustomOMBCSystemDescriptionSerializer is not null
                       ? CustomOMBCSystemDescriptionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this OMBC system description.
        /// </summary>
        public OMBC_SystemDescription Clone()

            => new (
                   ValidFrom,
                   [.. OperationModes.Select(operationMode => operationMode.Clone())],
                   [.. Transitions.   Select(transition    => transition.   Clone())],
                   [.. Timers.        Select(timer         => timer.        Clone())],
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two OMBC system descriptions for equality.
        /// </summary>
        public static Boolean operator == (OMBC_SystemDescription? OMBCSystemDescription1, OMBC_SystemDescription? OMBCSystemDescription2)
        {

            if (ReferenceEquals(OMBCSystemDescription1, OMBCSystemDescription2))
                return true;

            if (OMBCSystemDescription1 is null || OMBCSystemDescription2 is null)
                return false;

            return OMBCSystemDescription1.Equals(OMBCSystemDescription2);

        }

        /// <summary>
        /// Compares two OMBC system descriptions for inequality.
        /// </summary>
        public static Boolean operator != (OMBC_SystemDescription? OMBCSystemDescription1, OMBC_SystemDescription? OMBCSystemDescription2)
            => !(OMBCSystemDescription1 == OMBCSystemDescription2);

        #endregion

        #region IEquatable<OMBC_SystemDescription> Members

        /// <summary>
        /// Compares two OMBC system descriptions for equality.
        /// </summary>
        /// <param name="Object">An OMBC system description to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is OMBC_SystemDescription ombcSystemDescription && Equals(ombcSystemDescription);

        /// <summary>
        /// Compares two OMBC system descriptions for equality.
        /// </summary>
        /// <param name="OMBCSystemDescription">An OMBC system description to compare with.</param>
        public Boolean Equals(OMBC_SystemDescription? OMBCSystemDescription)

            => OMBCSystemDescription is not null &&

               MessageId.     Equals       (OMBCSystemDescription.MessageId)      &&
               ValidFrom.     Equals       (OMBCSystemDescription.ValidFrom)      &&
               OperationModes.SequenceEqual(OMBCSystemDescription.OperationModes) &&
               Transitions.   SequenceEqual(OMBCSystemDescription.Transitions)    &&
               Timers.        SequenceEqual(OMBCSystemDescription.Timers);

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
            => $"OMBC.SystemDescription valid from {ValidFrom.ToS2Timestamp()}: {OperationModes.Count} operation mode(s), {Transitions.Count} transition(s), {Timers.Count} timer(s) [{MessageId}]";

        #endregion

    }

}

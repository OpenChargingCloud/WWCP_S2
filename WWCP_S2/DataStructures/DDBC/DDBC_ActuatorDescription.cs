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
    /// A Demand Driven Based Control (DDBC) actuator description: the supported commodities,
    /// operation modes, transitions and timers of one actuator.
    /// Wire-name quirk: the supported commodities are serialised under the JSON key
    /// "supported_commodites" (sic, typo in the S2 JSON schema).
    /// </summary>
    public sealed class DDBC_ActuatorDescription : IEquatable<DDBC_ActuatorDescription>
    {

        #region Properties

        /// <summary>
        /// The identification of this DDBC.ActuatorDescription. Must be unique in the scope of the
        /// Resource Manager, for at least the duration of the session between Resource Manager and CEM.
        /// </summary>
        [Mandatory]
        public Actuator_Id                          Id                      { get; }

        /// <summary>
        /// The optional human readable name/description of the actuator. This element is only
        /// intended for diagnostic purposes and not for HMI applications.
        /// </summary>
        [Optional]
        public String?                              DiagnosticLabel         { get; }

        /// <summary>
        /// The commodities supported by the operation modes of this actuator.
        /// There shall be at least one commodity (1..4 unique entries).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<Commodity>             SupportedCommodities    { get; }

        /// <summary>
        /// The list of all operation modes that are available for this actuator.
        /// There shall be at least one DDBC.OperationMode (1..100 entries).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<DDBC_OperationMode>    OperationModes          { get; }

        /// <summary>
        /// The list of transitions between operation modes (0..1000 entries).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<Transition>            Transitions             { get; }

        /// <summary>
        /// The list of timers associated with transitions for this actuator. Can be empty (0..1000 entries).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<Timer>                 Timers                  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new DDBC actuator description.
        /// </summary>
        /// <param name="Id">The identification of this actuator description (unique within the Resource Manager).</param>
        /// <param name="SupportedCommodities">The commodities supported by the operation modes of this actuator (1..4 unique entries).</param>
        /// <param name="OperationModes">All operation modes available for this actuator (1..100 entries with unique identifications).</param>
        /// <param name="Transitions">The transitions between operation modes (0..1000 entries with unique identifications).</param>
        /// <param name="Timers">The timers associated with the transitions (0..1000 entries with unique identifications).</param>
        /// <param name="DiagnosticLabel">An optional human readable name/description of the actuator (diagnostic purposes only).</param>
        public DDBC_ActuatorDescription(Actuator_Id                        Id,
                                        IReadOnlyList<Commodity>           SupportedCommodities,
                                        IReadOnlyList<DDBC_OperationMode>  OperationModes,
                                        IReadOnlyList<Transition>          Transitions,
                                        IReadOnlyList<Timer>               Timers,
                                        String?                            DiagnosticLabel   = null)
        {

            ArgumentNullException.ThrowIfNull(SupportedCommodities);
            ArgumentNullException.ThrowIfNull(OperationModes);
            ArgumentNullException.ThrowIfNull(Transitions);
            ArgumentNullException.ThrowIfNull(Timers);

            #region Schema constraints (minItems/maxItems)

            if (SupportedCommodities.Count < 1 || SupportedCommodities.Count > 4)
                throw new ArgumentException($"A DDBC actuator description must have between 1 and 4 supported commodities, but {SupportedCommodities.Count} were given!",
                                            nameof(SupportedCommodities));

            if (OperationModes.Count < 1 || OperationModes.Count > 100)
                throw new ArgumentException($"A DDBC actuator description must have between 1 and 100 operation modes, but {OperationModes.Count} were given!",
                                            nameof(OperationModes));

            if (Transitions.Count > 1000)
                throw new ArgumentException($"A DDBC actuator description must have at most 1000 transitions, but {Transitions.Count} were given!",
                                            nameof(Transitions));

            if (Timers.Count > 1000)
                throw new ArgumentException($"A DDBC actuator description must have at most 1000 timers, but {Timers.Count} were given!",
                                            nameof(Timers));

            #endregion

            #region Uniqueness of supported commodities, operation mode ids, transition ids and timer ids

            var duplicateCommodity = SupportedCommodities.GroupBy(commodity => commodity).
                                                          FirstOrDefault(group => group.Count() > 1);

            if (duplicateCommodity is not null)
                throw new ArgumentException($"The supported commodities must be unique, but '{duplicateCommodity.Key}' occurs {duplicateCommodity.Count()} times!",
                                            nameof(SupportedCommodities));

            var duplicateOperationModeId = OperationModes.GroupBy(operationMode => operationMode.Id).
                                                          FirstOrDefault(group => group.Count() > 1);

            if (duplicateOperationModeId is not null)
                throw new ArgumentException($"The operation mode identifications must be unique, but '{duplicateOperationModeId.Key}' occurs {duplicateOperationModeId.Count()} times!",
                                            nameof(OperationModes));

            var duplicateTransitionId = Transitions.GroupBy(transition => transition.Id).
                                                    FirstOrDefault(group => group.Count() > 1);

            if (duplicateTransitionId is not null)
                throw new ArgumentException($"The transition identifications must be unique, but '{duplicateTransitionId.Key}' occurs {duplicateTransitionId.Count()} times!",
                                            nameof(Transitions));

            var duplicateTimerId = Timers.GroupBy(timer => timer.Id).
                                          FirstOrDefault(group => group.Count() > 1);

            if (duplicateTimerId is not null)
                throw new ArgumentException($"The timer identifications must be unique, but '{duplicateTimerId.Key}' occurs {duplicateTimerId.Count()} times!",
                                            nameof(Timers));

            #endregion

            #region References of transitions to operation modes and timers

            var operationModeIds  = OperationModes.Select(operationMode => operationMode.Id).ToHashSet();
            var timerIds          = Timers.        Select(timer         => timer.Id).        ToHashSet();

            foreach (var transition in Transitions)
            {

                if (!operationModeIds.Contains(transition.From))
                    throw new ArgumentException($"Transition '{transition.Id}' switches from an unknown operation mode '{transition.From}'!",
                                                nameof(Transitions));

                if (!operationModeIds.Contains(transition.To))
                    throw new ArgumentException($"Transition '{transition.Id}' switches to an unknown operation mode '{transition.To}'!",
                                                nameof(Transitions));

                foreach (var startTimer in transition.StartTimers)
                {
                    if (!timerIds.Contains(startTimer))
                        throw new ArgumentException($"Transition '{transition.Id}' references an unknown start timer '{startTimer}'!",
                                                    nameof(Transitions));
                }

                foreach (var blockingTimer in transition.BlockingTimers)
                {
                    if (!timerIds.Contains(blockingTimer))
                        throw new ArgumentException($"Transition '{transition.Id}' references an unknown blocking timer '{blockingTimer}'!",
                                                    nameof(Transitions));
                }

            }

            #endregion

            #region Power ranges per supported commodity

            var supportedCommodities = SupportedCommodities.ToHashSet();

            foreach (var operationMode in OperationModes)
            {

                foreach (var powerRange in operationMode.PowerRanges)
                {

                    var commodity = powerRange.CommodityQuantity.Commodity;

                    if (commodity is null || !supportedCommodities.Contains(commodity.Value))
                        throw new ArgumentException($"Operation mode '{operationMode.Id}' has a power range for commodity quantity '{powerRange.CommodityQuantity}', which is not a supported commodity!",
                                                    nameof(OperationModes));

                }

                foreach (var supportedCommodity in SupportedCommodities)
                {

                    if (!operationMode.PowerRanges.Any(powerRange => powerRange.CommodityQuantity.Commodity == supportedCommodity))
                        throw new ArgumentException($"Operation mode '{operationMode.Id}' has no power range for the supported commodity '{supportedCommodity}'!",
                                                    nameof(OperationModes));

                }

            }

            #endregion

            this.Id                    = Id;
            this.SupportedCommodities  = [.. SupportedCommodities];
            this.OperationModes        = [.. OperationModes];
            this.Transitions           = [.. Transitions];
            this.Timers                = [.. Timers];
            this.DiagnosticLabel       = DiagnosticLabel;

            unchecked
            {
                hashCode = this.Id.                  GetHashCode()  * 13 ^
                           this.SupportedCommodities.CalcHashCode() * 11 ^
                           this.OperationModes.      CalcHashCode() *  7 ^
                           this.Transitions.         CalcHashCode() *  5 ^
                           this.Timers.              CalcHashCode() *  3 ^
                          (this.DiagnosticLabel?.    GetHashCode(StringComparison.Ordinal) ?? 0);
            }

        }

        #endregion


        #region Documentation

        // DDBC.ActuatorDescription.schema.json
        //   "title": "DDBC_ActuatorDescription",
        //   "properties": {
        //     "id":                    { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of this DDBC.ActuatorDescription. Must be unique in the scope of the Resource
        //                                                Manager, for at least the duration of the session between Resource Manager and CEM." },
        //     "diagnostic_label":      { "type": "string",
        //                                "description": "Human readable name/description of the actuator. This element is only intended
        //                                                for diagnostic purposes and not for HMI applications." },
        //     "supported_commodites":  { "type": "array", "minItems": 1, "maxItems": 4,
        //                                "items": { "$ref": "../schemas/Commodity.schema.json" },
        //                                "description": "Commodities supported by the operation modes of this actuator. There shall be
        //                                                at least one commodity" },
        //     "operation_modes":       { "type": "array", "minItems": 1, "maxItems": 100,
        //                                "items": { "$ref": "../schemas/DDBC.OperationMode.schema.json" },
        //                                "description": "List of all Operation Modes that are available for this actuator. There shall
        //                                                be at least one DDBC.OperationMode." },
        //     "transitions":           { "type": "array", "minItems": 0, "maxItems": 1000,
        //                                "items": { "$ref": "../schemas/Transition.schema.json" },
        //                                "description": "List of Transitions between Operation Modes. Shall contain at least one Transition." },
        //     "timers":                { "type": "array", "minItems": 0, "maxItems": 1000,
        //                                "items": { "$ref": "../schemas/Timer.schema.json" },
        //                                "description": "List of Timers associated with Transitions for this Actuator. Can be empty." }
        //   },
        //   "required": ["id", "supported_commodites", "operation_modes", "transitions", "timers"],
        //   "additionalProperties": false
        //
        // Wire-name quirk (CONVENTIONS.md §1, PLAN.md §3.8): the supported commodities are serialised under the
        // misspelled JSON key "supported_commodites" (FRBC.ActuatorDescription uses "supported_commodities").
        // Kept verbatim, as the schema has "additionalProperties": false.
        //
        // Note: The schema says "transitions ... Shall contain at least one Transition", but its minItems is 0;
        // the schema wins (an actuator with a single operation mode needs no transitions).
        //
        // Semantic rules (CONVENTIONS.md §5, PLAN.md §3.3), all checked in the constructor:
        //   * supported commodities are unique;
        //   * operation mode ids, transition ids and timer ids are unique within this actuator description;
        //   * every Transition.From/To references a declared operation mode;
        //   * every Transition.StartTimers/BlockingTimers entry references a declared timer;
        //   * every operation mode carries a PowerRange for every supported commodity and no PowerRange for
        //     an unsupported commodity (CommodityQuantity → Commodity mapping via CommodityQuantity.Commodity);
        //     the "at most one PowerRange per CommodityQuantity" rule is checked by DDBC_OperationMode itself.

        #endregion

        #region (static) TryParse(JSON, out DDBC_ActuatorDescription, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a DDBC actuator description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCActuatorDescription">The parsed DDBC actuator description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)]  out DDBC_ActuatorDescription?  DDBCActuatorDescription,
                                       [NotNullWhen(false)] out String?                    ErrorResponse)

            => TryParse(JSON,
                        out DDBCActuatorDescription,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC actuator description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCActuatorDescription">The parsed DDBC actuator description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)]  out DDBC_ActuatorDescription?  DDBCActuatorDescription,
                                       [NotNullWhen(false)] out String?                    ErrorResponse,
                                       S2ParserOptions?                                    Options)

            => TryParse(JSON,
                        out DDBCActuatorDescription,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC actuator description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCActuatorDescription">The parsed DDBC actuator description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomDDBCActuatorDescriptionParser">A delegate to parse custom DDBC actuator descriptions.</param>
        public static Boolean TryParse(JObject                                                 JSON,
                                       [NotNullWhen(true)]  out DDBC_ActuatorDescription?      DDBCActuatorDescription,
                                       [NotNullWhen(false)] out String?                        ErrorResponse,
                                       S2ParserOptions?                                        Options,
                                       CustomJObjectParserDelegate<DDBC_ActuatorDescription>?  CustomDDBCActuatorDescriptionParser)
        {

            try
            {

                DDBCActuatorDescription = null;

                #region id                      [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
                                             "actuator identification",
                                             Actuator_Id.TryParse,
                                             Options,
                                             out Actuator_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region diagnostic_label        [optional]

                if (!JSON.ParseOptionalS2String("diagnostic_label",
                                                "diagnostic label",
                                                out String? diagnosticLabel,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region supported_commodites    [mandatory]

                // Wire-name quirk: "supported_commodites" (sic)!
                if (!JSON.ParseMandatoryS2Enums("supported_commodites",
                                                "supported commodities",
                                                Commodity.TryParse,
                                                Options,
                                                1,
                                                4,
                                                out IReadOnlyList<Commodity>? supportedCommodities,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region operation_modes         [mandatory]

                if (!JSON.ParseMandatoryS2List("operation_modes",
                                               "operation modes",
                                               DDBC_OperationMode.TryParse,
                                               Options,
                                               1,
                                               100,
                                               out IReadOnlyList<DDBC_OperationMode>? operationModes,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region transitions             [mandatory]

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

                #region timers                  [mandatory]

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
                                                    "id",
                                                    "diagnostic_label",
                                                    "supported_commodites",
                                                    "operation_modes",
                                                    "transitions",
                                                    "timers"))
                {
                    return false;
                }

                #endregion


                DDBCActuatorDescription = new DDBC_ActuatorDescription(
                                              id,
                                              supportedCommodities,
                                              operationModes,
                                              transitions,
                                              timers,
                                              diagnosticLabel
                                          );

                if (CustomDDBCActuatorDescriptionParser is not null)
                    DDBCActuatorDescription = CustomDDBCActuatorDescriptionParser(JSON,
                                                                                  DDBCActuatorDescription);

                return true;

            }
            catch (Exception e)
            {
                DDBCActuatorDescription  = null;
                ErrorResponse            = "The given JSON representation of a DDBC actuator description is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDDBCActuatorDescriptionSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomDDBCActuatorDescriptionSerializer">A delegate to serialize custom DDBC actuator descriptions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DDBC_ActuatorDescription>? CustomDDBCActuatorDescriptionSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("id",                    Id.ToString()),

                           DiagnosticLabel is not null
                               ? new JProperty("diagnostic_label",      DiagnosticLabel)
                               : null,

                                 // Wire-name quirk: "supported_commodites" (sic)!
                                 new JProperty("supported_commodites",  new JArray(SupportedCommodities.Select(commodity     => commodity.    ToString()))),

                                 new JProperty("operation_modes",       new JArray(OperationModes.      Select(operationMode => operationMode.ToJSON()))),

                                 new JProperty("transitions",           new JArray(Transitions.         Select(transition    => transition.   ToJSON()))),

                                 new JProperty("timers",                new JArray(Timers.              Select(timer         => timer.        ToJSON())))

                       );

            return CustomDDBCActuatorDescriptionSerializer is not null
                       ? CustomDDBCActuatorDescriptionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this DDBC actuator description.
        /// </summary>
        public DDBC_ActuatorDescription Clone()

            => new (
                   Id.Clone(),
                   SupportedCommodities.Select(commodity     => commodity.    Clone()).ToList(),
                   OperationModes.      Select(operationMode => operationMode.Clone()).ToList(),
                   Transitions.         Select(transition    => transition.   Clone()).ToList(),
                   Timers.              Select(timer         => timer.        Clone()).ToList(),
                   DiagnosticLabel?.CloneString()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two DDBC actuator descriptions for equality.
        /// </summary>
        public static Boolean operator == (DDBC_ActuatorDescription? DDBCActuatorDescription1, DDBC_ActuatorDescription? DDBCActuatorDescription2)
        {

            if (ReferenceEquals(DDBCActuatorDescription1, DDBCActuatorDescription2))
                return true;

            if (DDBCActuatorDescription1 is null || DDBCActuatorDescription2 is null)
                return false;

            return DDBCActuatorDescription1.Equals(DDBCActuatorDescription2);

        }

        /// <summary>
        /// Compares two DDBC actuator descriptions for inequality.
        /// </summary>
        public static Boolean operator != (DDBC_ActuatorDescription? DDBCActuatorDescription1, DDBC_ActuatorDescription? DDBCActuatorDescription2)
            => !(DDBCActuatorDescription1 == DDBCActuatorDescription2);

        #endregion

        #region IEquatable<DDBC_ActuatorDescription> Members

        /// <summary>
        /// Compares two DDBC actuator descriptions for equality.
        /// </summary>
        /// <param name="Object">A DDBC actuator description to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is DDBC_ActuatorDescription ddbcActuatorDescription && Equals(ddbcActuatorDescription);

        /// <summary>
        /// Compares two DDBC actuator descriptions for equality.
        /// </summary>
        /// <param name="DDBCActuatorDescription">A DDBC actuator description to compare with.</param>
        public Boolean Equals(DDBC_ActuatorDescription? DDBCActuatorDescription)

            => DDBCActuatorDescription is not null &&

               Id.                  Equals       (DDBCActuatorDescription.Id) &&
               SupportedCommodities.SequenceEqual(DDBCActuatorDescription.SupportedCommodities) &&
               OperationModes.      SequenceEqual(DDBCActuatorDescription.OperationModes) &&
               Transitions.         SequenceEqual(DDBCActuatorDescription.Transitions) &&
               Timers.              SequenceEqual(DDBCActuatorDescription.Timers) &&

               String.Equals(DiagnosticLabel, DDBCActuatorDescription.DiagnosticLabel, StringComparison.Ordinal);

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

                   $"{Id}: {SupportedCommodities.Count} commodity/ies, {OperationModes.Count} operation mode(s), {Transitions.Count} transition(s), {Timers.Count} timer(s)",

                   DiagnosticLabel is not null
                       ? $" ({DiagnosticLabel})"
                       : ""

               );

        #endregion

    }

}

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
    /// The description of a Fill Rate Based Control (FRBC) actuator: its supported commodities,
    /// operation modes, the transitions between them and the timers constraining those transitions.
    /// </summary>
    public sealed class FRBC_ActuatorDescription : IEquatable<FRBC_ActuatorDescription>
    {

        #region Properties

        /// <summary>
        /// The identification of the actuator. Must be unique in the scope of the Resource Manager,
        /// for at least the duration of the session between Resource Manager and CEM.
        /// </summary>
        [Mandatory]
        public Actuator_Id                        Id                      { get; }

        /// <summary>
        /// The optional human readable name/description for the actuator. This element is only
        /// intended for diagnostic purposes and not for HMI applications.
        /// </summary>
        [Optional]
        public String?                            DiagnosticLabel         { get; }

        /// <summary>
        /// The list of all supported commodities.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<Commodity>           SupportedCommodities    { get; }

        /// <summary>
        /// The provided FRBC.OperationModes associated with this actuator.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<FRBC_OperationMode>  OperationModes          { get; }

        /// <summary>
        /// The possible transitions between FRBC.OperationModes associated with this actuator.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<Transition>          Transitions             { get; }

        /// <summary>
        /// The list of timers associated with this actuator.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<Timer>               Timers                  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new FRBC actuator description.
        /// </summary>
        /// <param name="Id">The identification of the actuator.</param>
        /// <param name="SupportedCommodities">The list of all supported commodities (1..4 unique entries).</param>
        /// <param name="OperationModes">The provided FRBC.OperationModes (1..100 entries with unique identifications).</param>
        /// <param name="Transitions">The possible transitions between operation modes (0..1000 entries with unique identifications).</param>
        /// <param name="Timers">The timers associated with this actuator (0..1000 entries with unique identifications).</param>
        /// <param name="DiagnosticLabel">An optional human readable name/description for the actuator (diagnostic purposes only).</param>
        public FRBC_ActuatorDescription(Actuator_Id                        Id,
                                        IReadOnlyList<Commodity>           SupportedCommodities,
                                        IReadOnlyList<FRBC_OperationMode>  OperationModes,
                                        IReadOnlyList<Transition>          Transitions,
                                        IReadOnlyList<Timer>               Timers,
                                        String?                            DiagnosticLabel   = null)
        {

            ArgumentNullException.ThrowIfNull(SupportedCommodities);
            ArgumentNullException.ThrowIfNull(OperationModes);
            ArgumentNullException.ThrowIfNull(Transitions);
            ArgumentNullException.ThrowIfNull(Timers);

            #region Supported commodities: 1..4, unique

            if (SupportedCommodities.Count < 1)
                throw new ArgumentException("An FRBC actuator description must contain at least one supported commodity!",
                                            nameof(SupportedCommodities));

            if (SupportedCommodities.Count > 4)
                throw new ArgumentException($"An FRBC actuator description must not contain more than 4 supported commodities, but {SupportedCommodities.Count} were given!",
                                            nameof(SupportedCommodities));

            var duplicateCommodity = SupportedCommodities.
                                         GroupBy(commodity => commodity).
                                         Select (group     => (group.Key, Count: group.Count())).
                                         FirstOrDefault(group => group.Count > 1);

            if (duplicateCommodity.Count > 1)
                throw new ArgumentException($"The supported commodities of an FRBC actuator description must be unique, but '{duplicateCommodity.Key}' occurs {duplicateCommodity.Count} times!",
                                            nameof(SupportedCommodities));

            #endregion

            #region Operation modes: 1..100, unique ids

            if (OperationModes.Count < 1)
                throw new ArgumentException("An FRBC actuator description must contain at least one operation mode!",
                                            nameof(OperationModes));

            if (OperationModes.Count > 100)
                throw new ArgumentException($"An FRBC actuator description must not contain more than 100 operation modes, but {OperationModes.Count} were given!",
                                            nameof(OperationModes));

            var duplicateOperationModeId = OperationModes.
                                               GroupBy(operationMode => operationMode.Id).
                                               Select (group         => (group.Key, Count: group.Count())).
                                               FirstOrDefault(group => group.Count > 1);

            if (duplicateOperationModeId.Count > 1)
                throw new ArgumentException($"The operation mode identifications of an FRBC actuator description must be unique, but '{duplicateOperationModeId.Key}' occurs {duplicateOperationModeId.Count} times!",
                                            nameof(OperationModes));

            #endregion

            #region Timers: 0..1000, unique ids

            if (Timers.Count > 1000)
                throw new ArgumentException($"An FRBC actuator description must not contain more than 1000 timers, but {Timers.Count} were given!",
                                            nameof(Timers));

            var duplicateTimerId = Timers.
                                       GroupBy(timer => timer.Id).
                                       Select (group => (group.Key, Count: group.Count())).
                                       FirstOrDefault(group => group.Count > 1);

            if (duplicateTimerId.Count > 1)
                throw new ArgumentException($"The timer identifications of an FRBC actuator description must be unique, but '{duplicateTimerId.Key}' occurs {duplicateTimerId.Count} times!",
                                            nameof(Timers));

            #endregion

            #region Transitions: 0..1000, unique ids, references to declared operation modes and timers

            if (Transitions.Count > 1000)
                throw new ArgumentException($"An FRBC actuator description must not contain more than 1000 transitions, but {Transitions.Count} were given!",
                                            nameof(Transitions));

            var duplicateTransitionId = Transitions.
                                            GroupBy(transition => transition.Id).
                                            Select (group      => (group.Key, Count: group.Count())).
                                            FirstOrDefault(group => group.Count > 1);

            if (duplicateTransitionId.Count > 1)
                throw new ArgumentException($"The transition identifications of an FRBC actuator description must be unique, but '{duplicateTransitionId.Key}' occurs {duplicateTransitionId.Count} times!",
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

            #region Operation mode elements: at least one power range per supported commodity, none for unsupported commodities

            // "At most one PowerRange per CommodityQuantity" is checked by FRBC_OperationModeElement itself;
            // several CommodityQuantities of one commodity (ELECTRIC.POWER.L1/L2/L3) are allowed by the schema.
            foreach (var operationMode in OperationModes)
            {
                foreach (var element in operationMode.Elements)
                {

                    foreach (var powerRange in element.PowerRanges)
                    {

                        var commodity = powerRange.CommodityQuantity.Commodity;

                        if (commodity is null || !SupportedCommodities.Contains(commodity.Value))
                            throw new ArgumentException($"The operation mode '{operationMode.Id}' contains a power range for the commodity quantity '{powerRange.CommodityQuantity}', which is not a supported commodity of the actuator!",
                                                        nameof(OperationModes));

                    }

                    foreach (var supportedCommodity in SupportedCommodities)
                    {

                        if (!element.PowerRanges.Any(powerRange => powerRange.CommodityQuantity.Commodity == supportedCommodity))
                            throw new ArgumentException($"Every element of the operation mode '{operationMode.Id}' must contain at least one power range for the supported commodity '{supportedCommodity}', but none was found for fill level range {element.FillLevelRange}!",
                                                        nameof(OperationModes));

                    }

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
                          (this.DiagnosticLabel?.    GetHashCode(StringComparison.Ordinal) ?? 0) * 11 ^
                           this.SupportedCommodities.CalcHashCode() *  7 ^
                           this.OperationModes.      CalcHashCode() *  5 ^
                           this.Transitions.         CalcHashCode() *  3 ^
                           this.Timers.              CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // FRBC.ActuatorDescription.schema.json
        //   "title": "FRBC_ActuatorDescription",
        //   "properties": {
        //     "id":                    { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the Actuator. Must be unique in the scope of the Resource Manager, for at
        //                                                least the duration of the session between Resource Manager and CEM." },
        //     "diagnostic_label":      { "type": "string",
        //                                "description": "Human readable name/description for the actuator. This element is only intended
        //                                                for diagnostic purposes and not for HMI applications." },
        //     "supported_commodities": { "type": "array", "minItems": 1, "maxItems": 4,
        //                                "items": { "$ref": "../schemas/Commodity.schema.json" },
        //                                "description": "List of all supported Commodities." },
        //     "operation_modes":       { "type": "array", "minItems": 1, "maxItems": 100,
        //                                "items": { "$ref": "../schemas/FRBC.OperationMode.schema.json" },
        //                                "description": "Provided FRBC.OperationModes associated with this actuator" },
        //     "transitions":           { "type": "array", "minItems": 0, "maxItems": 1000,
        //                                "items": { "$ref": "../schemas/Transition.schema.json" },
        //                                "description": "Possible transitions between FRBC.OperationModes associated with this actuator." },
        //     "timers":                { "type": "array", "minItems": 0, "maxItems": 1000,
        //                                "items": { "$ref": "../schemas/Timer.schema.json" },
        //                                "description": "List of Timers associated with this actuator" }
        //   },
        //   "required": ["id", "supported_commodities", "operation_modes", "transitions", "timers"],
        //   "additionalProperties": false
        //
        // Semantic rules (CONVENTIONS.md §5):
        //   - supported_commodities are unique.
        //   - Operation mode ids, transition ids and timer ids are unique within the actuator description.
        //   - Every transition's from/to references a declared operation mode.
        //   - Every start_timers/blocking_timers entry of a transition references a declared timer.
        //   - Every operation mode element carries at least one PowerRange per supported commodity
        //     (CommodityQuantity → Commodity: ELECTRIC.* → ELECTRICITY, NATURAL_GAS.*/HYDROGEN.* → GAS, HEAT.* → HEAT, OIL.* → OIL)
        //     and no PowerRange for a commodity that is not supported. Several CommodityQuantities of one commodity
        //     (ELECTRIC.POWER.L1/L2/L3) are allowed; "at most one PowerRange per CommodityQuantity" is checked by
        //     FRBC_OperationModeElement itself. Same interpretation as DDBC_ActuatorDescription.

        #endregion

        #region (static) TryParse(JSON, out FRBC_ActuatorDescription, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an FRBC actuator description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCActuatorDescription">The parsed FRBC actuator description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)]  out FRBC_ActuatorDescription?  FRBCActuatorDescription,
                                       [NotNullWhen(false)] out String?                    ErrorResponse)

            => TryParse(JSON,
                        out FRBCActuatorDescription,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC actuator description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCActuatorDescription">The parsed FRBC actuator description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)]  out FRBC_ActuatorDescription?  FRBCActuatorDescription,
                                       [NotNullWhen(false)] out String?                    ErrorResponse,
                                       S2ParserOptions?                                    Options)

            => TryParse(JSON,
                        out FRBCActuatorDescription,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC actuator description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCActuatorDescription">The parsed FRBC actuator description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomFRBCActuatorDescriptionParser">A delegate to parse custom FRBC actuator descriptions.</param>
        public static Boolean TryParse(JObject                                                 JSON,
                                       [NotNullWhen(true)]  out FRBC_ActuatorDescription?      FRBCActuatorDescription,
                                       [NotNullWhen(false)] out String?                        ErrorResponse,
                                       S2ParserOptions?                                        Options,
                                       CustomJObjectParserDelegate<FRBC_ActuatorDescription>?  CustomFRBCActuatorDescriptionParser)
        {

            try
            {

                FRBCActuatorDescription = null;

                #region id                       [mandatory]

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

                #region diagnostic_label         [optional]

                if (!JSON.ParseOptionalS2String("diagnostic_label",
                                                "diagnostic label",
                                                out String? diagnosticLabel,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region supported_commodities    [mandatory]

                if (!JSON.ParseMandatoryS2Enums("supported_commodities",
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

                #region operation_modes          [mandatory]

                if (!JSON.ParseMandatoryS2List("operation_modes",
                                               "operation modes",
                                               FRBC_OperationMode.TryParse,
                                               Options,
                                               1,
                                               100,
                                               out IReadOnlyList<FRBC_OperationMode>? operationModes,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region transitions              [mandatory]

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

                #region timers                   [mandatory]

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
                                                    "supported_commodities",
                                                    "operation_modes",
                                                    "transitions",
                                                    "timers"))
                {
                    return false;
                }

                #endregion


                FRBCActuatorDescription = new FRBC_ActuatorDescription(
                                              id,
                                              supportedCommodities,
                                              operationModes,
                                              transitions,
                                              timers,
                                              diagnosticLabel
                                          );

                if (CustomFRBCActuatorDescriptionParser is not null)
                    FRBCActuatorDescription = CustomFRBCActuatorDescriptionParser(JSON,
                                                                                  FRBCActuatorDescription);

                return true;

            }
            catch (Exception e)
            {
                FRBCActuatorDescription  = null;
                ErrorResponse            = "The given JSON representation of an FRBC actuator description is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomFRBCActuatorDescriptionSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomFRBCActuatorDescriptionSerializer">A delegate to serialize custom FRBC actuator descriptions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_ActuatorDescription>? CustomFRBCActuatorDescriptionSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("id",                     Id.ToString()),

                           DiagnosticLabel is not null
                               ? new JProperty("diagnostic_label",       DiagnosticLabel)
                               : null,

                                 new JProperty("supported_commodities",  new JArray(SupportedCommodities.Select(commodity     => commodity.    ToString()))),
                                 new JProperty("operation_modes",        new JArray(OperationModes.      Select(operationMode => operationMode.ToJSON()))),
                                 new JProperty("transitions",            new JArray(Transitions.         Select(transition    => transition.   ToJSON()))),
                                 new JProperty("timers",                 new JArray(Timers.              Select(timer         => timer.        ToJSON())))

                       );

            return CustomFRBCActuatorDescriptionSerializer is not null
                       ? CustomFRBCActuatorDescriptionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this FRBC actuator description.
        /// </summary>
        public FRBC_ActuatorDescription Clone()

            => new (
                   Id.Clone(),
                   [.. SupportedCommodities.Select(commodity     => commodity.    Clone())],
                   [.. OperationModes.      Select(operationMode => operationMode.Clone())],
                   [.. Transitions.         Select(transition    => transition.   Clone())],
                   [.. Timers.              Select(timer         => timer.        Clone())],
                   DiagnosticLabel?.CloneString()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two FRBC actuator descriptions for equality.
        /// </summary>
        public static Boolean operator == (FRBC_ActuatorDescription? FRBCActuatorDescription1, FRBC_ActuatorDescription? FRBCActuatorDescription2)
        {

            if (ReferenceEquals(FRBCActuatorDescription1, FRBCActuatorDescription2))
                return true;

            if (FRBCActuatorDescription1 is null || FRBCActuatorDescription2 is null)
                return false;

            return FRBCActuatorDescription1.Equals(FRBCActuatorDescription2);

        }

        /// <summary>
        /// Compares two FRBC actuator descriptions for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_ActuatorDescription? FRBCActuatorDescription1, FRBC_ActuatorDescription? FRBCActuatorDescription2)
            => !(FRBCActuatorDescription1 == FRBCActuatorDescription2);

        #endregion

        #region IEquatable<FRBC_ActuatorDescription> Members

        /// <summary>
        /// Compares two FRBC actuator descriptions for equality.
        /// </summary>
        /// <param name="Object">An FRBC actuator description to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_ActuatorDescription frbcActuatorDescription && Equals(frbcActuatorDescription);

        /// <summary>
        /// Compares two FRBC actuator descriptions for equality.
        /// </summary>
        /// <param name="FRBCActuatorDescription">An FRBC actuator description to compare with.</param>
        public Boolean Equals(FRBC_ActuatorDescription? FRBCActuatorDescription)

            => FRBCActuatorDescription is not null &&

               Id.Equals(FRBCActuatorDescription.Id) &&
               String.Equals(DiagnosticLabel, FRBCActuatorDescription.DiagnosticLabel, StringComparison.Ordinal) &&
               SupportedCommodities.SequenceEqual(FRBCActuatorDescription.SupportedCommodities) &&
               OperationModes.      SequenceEqual(FRBCActuatorDescription.OperationModes) &&
               Transitions.         SequenceEqual(FRBCActuatorDescription.Transitions) &&
               Timers.              SequenceEqual(FRBCActuatorDescription.Timers);

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

                   $"{Id}: {SupportedCommodities.Count} commodity/commodities, {OperationModes.Count} operation mode(s), {Transitions.Count} transition(s), {Timers.Count} timer(s)",

                   DiagnosticLabel is not null
                       ? $" ({DiagnosticLabel})"
                       : ""

               );

        #endregion

    }

}

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
    /// A Demand Driven Based Control (DDBC) operation mode: the power ranges and the supply
    /// range an actuator can deliver, both scaled by the operation mode factor.
    /// Wire-name quirk: the identification is serialised under the JSON key "Id" (capital I).
    /// </summary>
    public sealed class DDBC_OperationMode : IEquatable<DDBC_OperationMode>
    {

        #region Properties

        /// <summary>
        /// The identification of this operation mode. Must be unique in the scope of the
        /// DDBC.ActuatorDescription in which it is used.
        /// </summary>
        [Mandatory]
        public OperationMode_Id          Id                       { get; }

        /// <summary>
        /// The optional human readable name/description of the DDBC.OperationMode. This element
        /// is only intended for diagnostic purposes and not for HMI applications.
        /// </summary>
        [Optional]
        public String?                   DiagnosticLabel          { get; }

        /// <summary>
        /// The power produced or consumed by this operation mode. The start of each PowerRange
        /// is associated with an operation_mode_factor of 0, the end is associated with an
        /// operation_mode_factor of 1. In the array there must be at least one PowerRange,
        /// and at most one PowerRange per CommodityQuantity.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PowerRange>  PowerRanges              { get; }

        /// <summary>
        /// The supply rate this DDBC.OperationMode can deliver for the CEM to match the demand
        /// rate. The start of the NumberRange is associated with an operation_mode_factor of 0,
        /// the end is associated with an operation_mode_factor of 1.
        /// </summary>
        [Mandatory]
        public NumberRange               SupplyRange              { get; }

        /// <summary>
        /// The optional additional costs per second (e.g. wear, services) associated with this
        /// operation mode in the currency defined by the ResourceManagerDetails, excluding the
        /// commodity cost. The range is expressing uncertainty and is not linked to the
        /// operation_mode_factor.
        /// </summary>
        [Optional]
        public NumberRange?              RunningCosts             { get; }

        /// <summary>
        /// Indicates if this DDBC.OperationMode may only be used during an abnormal condition.
        /// </summary>
        [Mandatory]
        public Boolean                   AbnormalConditionOnly    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new DDBC operation mode.
        /// </summary>
        /// <param name="Id">The identification of this operation mode (unique within its DDBC.ActuatorDescription).</param>
        /// <param name="PowerRanges">The power produced or consumed by this operation mode (1..10 entries, at most one per commodity quantity).</param>
        /// <param name="SupplyRange">The supply rate this operation mode can deliver for the CEM to match the demand rate.</param>
        /// <param name="AbnormalConditionOnly">Whether this operation mode may only be used during an abnormal condition.</param>
        /// <param name="DiagnosticLabel">An optional human readable name/description of the operation mode (diagnostic purposes only).</param>
        /// <param name="RunningCosts">Optional additional costs per second associated with this operation mode, excluding the commodity cost.</param>
        public DDBC_OperationMode(OperationMode_Id          Id,
                                  IReadOnlyList<PowerRange>  PowerRanges,
                                  NumberRange               SupplyRange,
                                  Boolean                   AbnormalConditionOnly,
                                  String?                   DiagnosticLabel   = null,
                                  NumberRange?              RunningCosts      = null)
        {

            ArgumentNullException.ThrowIfNull(PowerRanges);
            ArgumentNullException.ThrowIfNull(SupplyRange);

            if (PowerRanges.Count < 1 || PowerRanges.Count > 10)
                throw new ArgumentException($"A DDBC operation mode must have between 1 and 10 power ranges, but {PowerRanges.Count} were given!",
                                            nameof(PowerRanges));

            var duplicateCommodityQuantity = PowerRanges.GroupBy(powerRange => powerRange.CommodityQuantity).
                                                         FirstOrDefault(group => group.Count() > 1);

            if (duplicateCommodityQuantity is not null)
                throw new ArgumentException($"There must be at most one power range per commodity quantity, but '{duplicateCommodityQuantity.Key}' occurs {duplicateCommodityQuantity.Count()} times!",
                                            nameof(PowerRanges));

            this.Id                     = Id;
            this.PowerRanges            = [.. PowerRanges];
            this.SupplyRange            = SupplyRange;
            this.AbnormalConditionOnly  = AbnormalConditionOnly;
            this.DiagnosticLabel        = DiagnosticLabel;
            this.RunningCosts           = RunningCosts;

            unchecked
            {
                hashCode = this.Id.                   GetHashCode()       * 13 ^
                           this.PowerRanges.          CalcHashCode()      * 11 ^
                           this.SupplyRange.          GetHashCode()       *  7 ^
                           this.AbnormalConditionOnly.GetHashCode()       *  5 ^
                          (this.DiagnosticLabel?.     GetHashCode(StringComparison.Ordinal) ?? 0) * 3 ^
                          (this.RunningCosts?.        GetHashCode()       ?? 0);
            }

        }

        #endregion


        #region Documentation

        // DDBC.OperationMode.schema.json
        //   "title": "DDBC_OperationMode",
        //   "properties": {
        //     "Id":                      { "$ref": "../schemas/ID.schema.json",
        //                                  "description": "ID of this operation mode. Must be unique in the scope of the
        //                                                  DDBC.ActuatorDescription in which it is used." },
        //     "diagnostic_label":        { "type": "string",
        //                                  "description": "Human readable name/description of the DDBC.OperationMode. This element
        //                                                  is only intended for diagnostic purposes and not for HMI applications." },
        //     "power_ranges":            { "type": "array", "minItems": 1, "maxItems": 10,
        //                                  "items": { "$ref": "../schemas/PowerRange.schema.json" },
        //                                  "description": "The power produced or consumed by this operation mode. The start of each
        //                                                  PowerRange is associated with an operation_mode_factor of 0, the end is
        //                                                  associated with an operation_mode_factor of 1. In the array there must be
        //                                                  at least one PowerRange, and at most one PowerRange per CommodityQuantity." },
        //     "supply_range":            { "$ref": "../schemas/NumberRange.schema.json",
        //                                  "description": "The supply rate this DDBC.OperationMode can deliver for the CEM to match
        //                                                  the demand rate. The start of the NumberRange is associated with an
        //                                                  operation_mode_factor of 0, the end is associated with an
        //                                                  operation_mode_factor of 1." },
        //     "running_costs":           { "$ref": "../schemas/NumberRange.schema.json",
        //                                  "description": "Additional costs per second (e.g. wear, services) associated with this
        //                                                  operation mode in the currency defined by the ResourceManagerDetails,
        //                                                  excluding the commodity cost. The range is expressing uncertainty and is
        //                                                  not linked to the operation_mode_factor." },
        //     "abnormal_condition_only": { "type": "boolean",
        //                                  "description": "Indicates if this DDBC.OperationMode may only be used during an abnormal condition." }
        //   },
        //   "required": ["Id", "power_ranges", "supply_range", "abnormal_condition_only"],
        //   "additionalProperties": false
        //
        // Wire-name quirk (CONVENTIONS.md §1, PLAN.md §3.8): the identification is serialised under the
        // JSON key "Id" with a capital I (every other S2 type uses "id"). Kept verbatim.
        //
        // Semantic rule (CONVENTIONS.md §5, PLAN.md §3.3): at most one PowerRange per CommodityQuantity.
        // The "at least one PowerRange for every supported commodity, none for an unsupported commodity" rule
        // (CONVENTIONS.md §5) is checked by DDBC_ActuatorDescription; several CommodityQuantities of one
        // commodity (ELECTRIC.POWER.L1/L2/L3 for ELECTRICITY) are allowed.

        #endregion

        #region (static) TryParse(JSON, out DDBC_OperationMode, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a DDBC operation mode.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCOperationMode">The parsed DDBC operation mode.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out DDBC_OperationMode?  DDBCOperationMode,
                                       [NotNullWhen(false)] out String?              ErrorResponse)

            => TryParse(JSON,
                        out DDBCOperationMode,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC operation mode.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCOperationMode">The parsed DDBC operation mode.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out DDBC_OperationMode?  DDBCOperationMode,
                                       [NotNullWhen(false)] out String?              ErrorResponse,
                                       S2ParserOptions?                              Options)

            => TryParse(JSON,
                        out DDBCOperationMode,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC operation mode.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCOperationMode">The parsed DDBC operation mode.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomDDBCOperationModeParser">A delegate to parse custom DDBC operation modes.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out DDBC_OperationMode?      DDBCOperationMode,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options,
                                       CustomJObjectParserDelegate<DDBC_OperationMode>?  CustomDDBCOperationModeParser)
        {

            try
            {

                DDBCOperationMode = null;

                #region Id                         [mandatory]

                // Wire-name quirk: capital "Id"!
                if (!JSON.ParseMandatoryS2Id("Id",
                                             "operation mode identification",
                                             OperationMode_Id.TryParse,
                                             Options,
                                             out OperationMode_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region diagnostic_label           [optional]

                if (!JSON.ParseOptionalS2String("diagnostic_label",
                                                "diagnostic label",
                                                out String? diagnosticLabel,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region power_ranges               [mandatory]

                if (!JSON.ParseMandatoryS2List("power_ranges",
                                               "power ranges",
                                               PowerRange.TryParse,
                                               Options,
                                               1,
                                               10,
                                               out IReadOnlyList<PowerRange>? powerRanges,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region supply_range               [mandatory]

                if (!JSON.ParseMandatoryS2("supply_range",
                                           "supply range",
                                           NumberRange.TryParse,
                                           Options,
                                           out NumberRange? supplyRange,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region running_costs              [optional]

                if (!JSON.ParseOptionalS2("running_costs",
                                          "running costs",
                                          NumberRange.TryParse,
                                          Options,
                                          out NumberRange? runningCosts,
                                          out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region abnormal_condition_only    [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("abnormal_condition_only",
                                                  "abnormal condition only",
                                                  out Boolean abnormalConditionOnly,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "Id",
                                                    "diagnostic_label",
                                                    "power_ranges",
                                                    "supply_range",
                                                    "running_costs",
                                                    "abnormal_condition_only"))
                {
                    return false;
                }

                #endregion


                DDBCOperationMode = new DDBC_OperationMode(
                                        id,
                                        powerRanges,
                                        supplyRange,
                                        abnormalConditionOnly,
                                        diagnosticLabel,
                                        runningCosts
                                    );

                if (CustomDDBCOperationModeParser is not null)
                    DDBCOperationMode = CustomDDBCOperationModeParser(JSON,
                                                                      DDBCOperationMode);

                return true;

            }
            catch (Exception e)
            {
                DDBCOperationMode  = null;
                ErrorResponse      = "The given JSON representation of a DDBC operation mode is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDDBCOperationModeSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomDDBCOperationModeSerializer">A delegate to serialize custom DDBC operation modes.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DDBC_OperationMode>? CustomDDBCOperationModeSerializer = null)
        {

            var json = JSONObject.Create(

                                 // Wire-name quirk: capital "Id"!
                                 new JProperty("Id",                       Id.ToString()),

                           DiagnosticLabel is not null
                               ? new JProperty("diagnostic_label",         DiagnosticLabel)
                               : null,

                                 new JProperty("power_ranges",             new JArray(PowerRanges.Select(powerRange => powerRange.ToJSON()))),

                                 new JProperty("supply_range",             SupplyRange.ToJSON()),

                           RunningCosts is not null
                               ? new JProperty("running_costs",            RunningCosts.ToJSON())
                               : null,

                                 new JProperty("abnormal_condition_only",  AbnormalConditionOnly)

                       );

            return CustomDDBCOperationModeSerializer is not null
                       ? CustomDDBCOperationModeSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this DDBC operation mode.
        /// </summary>
        public DDBC_OperationMode Clone()

            => new (
                   Id.Clone(),
                   PowerRanges.Select(powerRange => powerRange.Clone()).ToList(),
                   SupplyRange.Clone(),
                   AbnormalConditionOnly,
                   DiagnosticLabel?.CloneString(),
                   RunningCosts?.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two DDBC operation modes for equality.
        /// </summary>
        public static Boolean operator == (DDBC_OperationMode? DDBCOperationMode1, DDBC_OperationMode? DDBCOperationMode2)
        {

            if (ReferenceEquals(DDBCOperationMode1, DDBCOperationMode2))
                return true;

            if (DDBCOperationMode1 is null || DDBCOperationMode2 is null)
                return false;

            return DDBCOperationMode1.Equals(DDBCOperationMode2);

        }

        /// <summary>
        /// Compares two DDBC operation modes for inequality.
        /// </summary>
        public static Boolean operator != (DDBC_OperationMode? DDBCOperationMode1, DDBC_OperationMode? DDBCOperationMode2)
            => !(DDBCOperationMode1 == DDBCOperationMode2);

        #endregion

        #region IEquatable<DDBC_OperationMode> Members

        /// <summary>
        /// Compares two DDBC operation modes for equality.
        /// </summary>
        /// <param name="Object">A DDBC operation mode to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is DDBC_OperationMode ddbcOperationMode && Equals(ddbcOperationMode);

        /// <summary>
        /// Compares two DDBC operation modes for equality.
        /// </summary>
        /// <param name="DDBCOperationMode">A DDBC operation mode to compare with.</param>
        public Boolean Equals(DDBC_OperationMode? DDBCOperationMode)

            => DDBCOperationMode is not null &&

               Id.                   Equals(DDBCOperationMode.Id) &&
               PowerRanges.          SequenceEqual(DDBCOperationMode.PowerRanges) &&
               SupplyRange.          Equals(DDBCOperationMode.SupplyRange) &&
               AbnormalConditionOnly.Equals(DDBCOperationMode.AbnormalConditionOnly) &&

               String.Equals(DiagnosticLabel, DDBCOperationMode.DiagnosticLabel, StringComparison.Ordinal) &&
               RunningCosts == DDBCOperationMode.RunningCosts;

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

                   $"{Id}: supply {SupplyRange}, {PowerRanges.Count} power range(s)",

                   AbnormalConditionOnly
                       ? ", abnormal condition only"
                       : "",

                   DiagnosticLabel is not null
                       ? $" ({DiagnosticLabel})"
                       : ""

               );

        #endregion

    }

}

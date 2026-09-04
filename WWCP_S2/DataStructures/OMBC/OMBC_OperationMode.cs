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
    /// An operation mode of an Operation Mode Based Control (OMBC) system: the power
    /// produced or consumed by the system, optionally its running costs, and whether
    /// the mode may only be used during an abnormal condition.
    /// </summary>
    public sealed class OMBC_OperationMode : IEquatable<OMBC_OperationMode>
    {

        #region Properties

        /// <summary>
        /// The identification of the OMBC.OperationMode. Must be unique in the scope of the
        /// Resource Manager, for at least the duration of the session between Resource Manager and CEM.
        /// </summary>
        [Mandatory]
        public OperationMode_Id          Id                       { get; }

        /// <summary>
        /// The optional human readable name/description of the OMBC.OperationMode. This element
        /// is only intended for diagnostic purposes and not for HMI applications.
        /// </summary>
        [Optional]
        public String?                   DiagnosticLabel          { get; }

        /// <summary>
        /// The power produced or consumed by this operation mode. The start of each PowerRange is
        /// associated with an operation_mode_factor of 0, the end is associated with an
        /// operation_mode_factor of 1. There must be at least one PowerRange, and at most one
        /// PowerRange per CommodityQuantity.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PowerRange>  PowerRanges              { get; }

        /// <summary>
        /// The optional additional costs per second (e.g. wear, services) associated with this
        /// operation mode in the currency defined by the ResourceManagerDetails, excluding the
        /// commodity cost. The range is expressing uncertainty and is not linked to the
        /// operation_mode_factor.
        /// </summary>
        [Optional]
        public NumberRange?              RunningCosts             { get; }

        /// <summary>
        /// Indicates if this OMBC.OperationMode may only be used during an abnormal condition.
        /// </summary>
        [Mandatory]
        public Boolean                   AbnormalConditionOnly    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new OMBC operation mode.
        /// </summary>
        /// <param name="Id">The identification of the OMBC.OperationMode.</param>
        /// <param name="PowerRanges">The power produced or consumed by this operation mode (1..10 entries, at most one per commodity quantity).</param>
        /// <param name="AbnormalConditionOnly">Whether this OMBC.OperationMode may only be used during an abnormal condition.</param>
        /// <param name="DiagnosticLabel">An optional human readable name/description of the OMBC.OperationMode (diagnostic purposes only).</param>
        /// <param name="RunningCosts">Optional additional costs per second associated with this operation mode.</param>
        public OMBC_OperationMode(OperationMode_Id         Id,
                                  IReadOnlyList<PowerRange>  PowerRanges,
                                  Boolean                  AbnormalConditionOnly,
                                  String?                  DiagnosticLabel   = null,
                                  NumberRange?             RunningCosts      = null)
        {

            ArgumentNullException.ThrowIfNull(PowerRanges);

            if (PowerRanges.Count < 1)
                throw new ArgumentException("An OMBC operation mode must contain at least one power range!",
                                            nameof(PowerRanges));

            if (PowerRanges.Count > 10)
                throw new ArgumentException($"An OMBC operation mode must not contain more than 10 power ranges, but {PowerRanges.Count} were given!",
                                            nameof(PowerRanges));

            var duplicateCommodityQuantity = PowerRanges.
                                                 GroupBy(powerRange => powerRange.CommodityQuantity).
                                                 Select (group      => (group.Key, Count: group.Count())).
                                                 FirstOrDefault(group => group.Count > 1);

            if (duplicateCommodityQuantity.Count > 1)
                throw new ArgumentException($"An OMBC operation mode must contain at most one power range per commodity quantity, but '{duplicateCommodityQuantity.Key}' occurs {duplicateCommodityQuantity.Count} times!",
                                            nameof(PowerRanges));

            this.Id                     = Id;
            this.PowerRanges            = [.. PowerRanges];
            this.AbnormalConditionOnly  = AbnormalConditionOnly;
            this.DiagnosticLabel        = DiagnosticLabel;
            this.RunningCosts           = RunningCosts;

            unchecked
            {
                hashCode = this.Id.                   GetHashCode()       * 11 ^
                          (this.DiagnosticLabel?.     GetHashCode(StringComparison.Ordinal) ?? 0) * 7 ^
                           this.PowerRanges.          CalcHashCode()      *  5 ^
                          (this.RunningCosts?.        GetHashCode() ?? 0) *  3 ^
                           this.AbnormalConditionOnly.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // OMBC.OperationMode.schema.json
        //   "title": "OMBC_OperationMode",
        //   "properties": {
        //     "id":                      { "$ref": "../schemas/ID.schema.json",
        //                                  "description": "ID of the OBMC.OperationMode. Must be unique in the scope of the Resource Manager,
        //                                                  for at least the duration of the session between Resource Manager and CEM." },
        //     "diagnostic_label":        { "type": "string",
        //                                  "description": "Human readable name/description of the OMBC.OperationMode. This element is only
        //                                                  intended for diagnostic purposes and not for HMI applications." },
        //     "power_ranges":            { "type": "array", "minItems": 1, "maxItems": 10,
        //                                  "items": { "$ref": "../schemas/PowerRange.schema.json" },
        //                                  "description": "The power produced or consumed by this operation mode. The start of each PowerRange
        //                                                  is associated with an operation_mode_factor of 0, the end is associated with an
        //                                                  operation_mode_factor of 1. In the array there must be at least one PowerRange,
        //                                                  and at most one PowerRange per CommodityQuantity." },
        //     "running_costs":           { "$ref": "../schemas/NumberRange.schema.json",
        //                                  "description": "Additional costs per second (e.g. wear, services) associated with this operation
        //                                                  mode in the currency defined by the ResourceManagerDetails, excluding the commodity
        //                                                  cost. The range is expressing uncertainty and is not linked to the operation_mode_factor." },
        //     "abnormal_condition_only": { "type": "boolean",
        //                                  "description": "Indicates if this OMBC.OperationMode may only be used during an abnormal condition." }
        //   },
        //   "required": ["id", "power_ranges", "abnormal_condition_only"],
        //   "additionalProperties": false
        //
        // Semantic rule (CONVENTIONS.md §5): at most one PowerRange per CommodityQuantity.

        #endregion

        #region (static) TryParse(JSON, out OMBC_OperationMode, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an OMBC operation mode.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCOperationMode">The parsed OMBC operation mode.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out OMBC_OperationMode?  OMBCOperationMode,
                                       [NotNullWhen(false)] out String?              ErrorResponse)

            => TryParse(JSON,
                        out OMBCOperationMode,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an OMBC operation mode.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCOperationMode">The parsed OMBC operation mode.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out OMBC_OperationMode?  OMBCOperationMode,
                                       [NotNullWhen(false)] out String?              ErrorResponse,
                                       S2ParserOptions?                              Options)

            => TryParse(JSON,
                        out OMBCOperationMode,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an OMBC operation mode.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OMBCOperationMode">The parsed OMBC operation mode.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomOMBCOperationModeParser">A delegate to parse custom OMBC operation modes.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out OMBC_OperationMode?      OMBCOperationMode,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options,
                                       CustomJObjectParserDelegate<OMBC_OperationMode>?  CustomOMBCOperationModeParser)
        {

            try
            {

                OMBCOperationMode = null;

                #region id                         [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
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
                                                    "id",
                                                    "diagnostic_label",
                                                    "power_ranges",
                                                    "running_costs",
                                                    "abnormal_condition_only"))
                {
                    return false;
                }

                #endregion


                OMBCOperationMode = new OMBC_OperationMode(
                                        id,
                                        powerRanges,
                                        abnormalConditionOnly,
                                        diagnosticLabel,
                                        runningCosts
                                    );

                if (CustomOMBCOperationModeParser is not null)
                    OMBCOperationMode = CustomOMBCOperationModeParser(JSON,
                                                                      OMBCOperationMode);

                return true;

            }
            catch (Exception e)
            {
                OMBCOperationMode  = null;
                ErrorResponse      = "The given JSON representation of an OMBC operation mode is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomOMBCOperationModeSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomOMBCOperationModeSerializer">A delegate to serialize custom OMBC operation modes.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<OMBC_OperationMode>? CustomOMBCOperationModeSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("id",                       Id.ToString()),

                           DiagnosticLabel is not null
                               ? new JProperty("diagnostic_label",         DiagnosticLabel)
                               : null,

                                 new JProperty("power_ranges",             new JArray(PowerRanges.Select(powerRange => powerRange.ToJSON()))),

                           RunningCosts is not null
                               ? new JProperty("running_costs",            RunningCosts.ToJSON())
                               : null,

                                 new JProperty("abnormal_condition_only",  AbnormalConditionOnly)

                       );

            return CustomOMBCOperationModeSerializer is not null
                       ? CustomOMBCOperationModeSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this OMBC operation mode.
        /// </summary>
        public OMBC_OperationMode Clone()

            => new (
                   Id.Clone(),
                   [.. PowerRanges.Select(powerRange => powerRange.Clone())],
                   AbnormalConditionOnly,
                   DiagnosticLabel?.CloneString(),
                   RunningCosts?.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two OMBC operation modes for equality.
        /// </summary>
        public static Boolean operator == (OMBC_OperationMode? OMBCOperationMode1, OMBC_OperationMode? OMBCOperationMode2)
        {

            if (ReferenceEquals(OMBCOperationMode1, OMBCOperationMode2))
                return true;

            if (OMBCOperationMode1 is null || OMBCOperationMode2 is null)
                return false;

            return OMBCOperationMode1.Equals(OMBCOperationMode2);

        }

        /// <summary>
        /// Compares two OMBC operation modes for inequality.
        /// </summary>
        public static Boolean operator != (OMBC_OperationMode? OMBCOperationMode1, OMBC_OperationMode? OMBCOperationMode2)
            => !(OMBCOperationMode1 == OMBCOperationMode2);

        #endregion

        #region IEquatable<OMBC_OperationMode> Members

        /// <summary>
        /// Compares two OMBC operation modes for equality.
        /// </summary>
        /// <param name="Object">An OMBC operation mode to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is OMBC_OperationMode ombcOperationMode && Equals(ombcOperationMode);

        /// <summary>
        /// Compares two OMBC operation modes for equality.
        /// </summary>
        /// <param name="OMBCOperationMode">An OMBC operation mode to compare with.</param>
        public Boolean Equals(OMBC_OperationMode? OMBCOperationMode)

            => OMBCOperationMode is not null &&

               Id.Equals(OMBCOperationMode.Id) &&
               String.Equals(DiagnosticLabel, OMBCOperationMode.DiagnosticLabel, StringComparison.Ordinal) &&
               PowerRanges.SequenceEqual(OMBCOperationMode.PowerRanges) &&
               RunningCosts == OMBCOperationMode.RunningCosts &&
               AbnormalConditionOnly.Equals(OMBCOperationMode.AbnormalConditionOnly);

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

                   $"{Id}: {PowerRanges.Count} power range(s)",

                   DiagnosticLabel is not null
                       ? $" ({DiagnosticLabel})"
                       : "",

                   AbnormalConditionOnly
                       ? ", abnormal condition only"
                       : ""

               );

        #endregion

    }

}

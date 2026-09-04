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
    /// An element of a Fill Rate Based Control (FRBC) operation mode: the fill rate,
    /// power ranges and running costs of the operation mode within a range of the fill level.
    /// </summary>
    public sealed class FRBC_OperationModeElement : IEquatable<FRBC_OperationModeElement>
    {

        #region Properties

        /// <summary>
        /// The range of the fill level for which this FRBC.OperationModeElement applies.
        /// The start of the range must be smaller than the end of the range.
        /// </summary>
        [Mandatory]
        public NumberRange                FillLevelRange    { get; }

        /// <summary>
        /// The change in fill level per second. The start of the range is associated with an
        /// operation_mode_factor of 0, the end is associated with an operation_mode_factor of 1.
        /// </summary>
        [Mandatory]
        public NumberRange                FillRate          { get; }

        /// <summary>
        /// The power produced or consumed by this operation mode. The start of each PowerRange is
        /// associated with an operation_mode_factor of 0, the end is associated with an
        /// operation_mode_factor of 1. There must be at least one PowerRange, and at most one
        /// PowerRange per CommodityQuantity.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PowerRange>  PowerRanges       { get; }

        /// <summary>
        /// The optional additional costs per second (e.g. wear, services) associated with this
        /// operation mode in the currency defined by the ResourceManagerDetails, excluding the
        /// commodity cost. The range is expressing uncertainty and is not linked to the
        /// operation_mode_factor.
        /// </summary>
        [Optional]
        public NumberRange?               RunningCosts      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new FRBC operation mode element.
        /// </summary>
        /// <param name="FillLevelRange">The range of the fill level for which this element applies (start strictly smaller than end).</param>
        /// <param name="FillRate">The change in fill level per second.</param>
        /// <param name="PowerRanges">The power produced or consumed by this operation mode (1..10 entries, at most one per commodity quantity).</param>
        /// <param name="RunningCosts">Optional additional costs per second associated with this operation mode.</param>
        public FRBC_OperationModeElement(NumberRange                FillLevelRange,
                                         NumberRange                FillRate,
                                         IReadOnlyList<PowerRange>  PowerRanges,
                                         NumberRange?               RunningCosts   = null)
        {

            ArgumentNullException.ThrowIfNull(FillLevelRange);
            ArgumentNullException.ThrowIfNull(FillRate);
            ArgumentNullException.ThrowIfNull(PowerRanges);

            if (FillLevelRange.StartOfRange >= FillLevelRange.EndOfRange)
                throw new ArgumentException($"The start of the fill level range ({FillLevelRange.StartOfRange}) must be smaller than its end ({FillLevelRange.EndOfRange})!",
                                            nameof(FillLevelRange));

            if (PowerRanges.Count < 1)
                throw new ArgumentException("An FRBC operation mode element must contain at least one power range!",
                                            nameof(PowerRanges));

            if (PowerRanges.Count > 10)
                throw new ArgumentException($"An FRBC operation mode element must not contain more than 10 power ranges, but {PowerRanges.Count} were given!",
                                            nameof(PowerRanges));

            var duplicateCommodityQuantity = PowerRanges.
                                                 GroupBy(powerRange => powerRange.CommodityQuantity).
                                                 Select (group      => (group.Key, Count: group.Count())).
                                                 FirstOrDefault(group => group.Count > 1);

            if (duplicateCommodityQuantity.Count > 1)
                throw new ArgumentException($"An FRBC operation mode element must contain at most one power range per commodity quantity, but '{duplicateCommodityQuantity.Key}' occurs {duplicateCommodityQuantity.Count} times!",
                                            nameof(PowerRanges));

            this.FillLevelRange  = FillLevelRange;
            this.FillRate        = FillRate;
            this.PowerRanges     = [.. PowerRanges];
            this.RunningCosts    = RunningCosts;

            unchecked
            {
                hashCode = this.FillLevelRange.GetHashCode()       * 7 ^
                           this.FillRate.      GetHashCode()       * 5 ^
                           this.PowerRanges.   CalcHashCode()      * 3 ^
                          (this.RunningCosts?. GetHashCode() ?? 0);
            }

        }

        #endregion


        #region Documentation

        // FRBC.OperationModeElement.schema.json
        //   "title": "FRBC_OperationModeElement",
        //   "properties": {
        //     "fill_level_range": { "$ref": "../schemas/NumberRange.schema.json",
        //                           "description": "The range of the fill level for which this FRBC.OperationModeElement applies.
        //                                           The start of the NumberRange shall be smaller than the end of the NumberRange." },
        //     "fill_rate":        { "$ref": "../schemas/NumberRange.schema.json",
        //                           "description": "Indicates the change in fill_level per second. The lower_boundary of the NumberRange
        //                                           is associated with an operation_mode_factor of 0, the upper_boundary is associated
        //                                           with an operation_mode_factor of 1." },
        //     "power_ranges":     { "type": "array", "minItems": 1, "maxItems": 10,
        //                           "items": { "$ref": "../schemas/PowerRange.schema.json" },
        //                           "description": "The power produced or consumed by this operation mode. The start of each PowerRange
        //                                           is associated with an operation_mode_factor of 0, the end is associated with an
        //                                           operation_mode_factor of 1. In the array there must be at least one PowerRange,
        //                                           and at most one PowerRange per CommodityQuantity." },
        //     "running_costs":    { "$ref": "../schemas/NumberRange.schema.json",
        //                           "description": "Additional costs per second (e.g. wear, services) associated with this operation mode
        //                                           in the currency defined by the ResourceManagerDetails, excluding the commodity cost.
        //                                           The range is expressing uncertainty and is not linked to the operation_mode_factor." }
        //   },
        //   "required": ["fill_level_range", "fill_rate", "power_ranges"],
        //   "additionalProperties": false
        //
        // Semantic rules (CONVENTIONS.md §5):
        //   - fill_level_range.start_of_range < fill_level_range.end_of_range (strict).
        //   - At most one PowerRange per CommodityQuantity.

        #endregion

        #region (static) TryParse(JSON, out FRBC_OperationModeElement, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an FRBC operation mode element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCOperationModeElement">The parsed FRBC operation mode element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out FRBC_OperationModeElement?  FRBCOperationModeElement,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)

            => TryParse(JSON,
                        out FRBCOperationModeElement,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC operation mode element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCOperationModeElement">The parsed FRBC operation mode element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out FRBC_OperationModeElement?  FRBCOperationModeElement,
                                       [NotNullWhen(false)] out String?                     ErrorResponse,
                                       S2ParserOptions?                                     Options)

            => TryParse(JSON,
                        out FRBCOperationModeElement,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC operation mode element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCOperationModeElement">The parsed FRBC operation mode element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomFRBCOperationModeElementParser">A delegate to parse custom FRBC operation mode elements.</param>
        public static Boolean TryParse(JObject                                                  JSON,
                                       [NotNullWhen(true)]  out FRBC_OperationModeElement?      FRBCOperationModeElement,
                                       [NotNullWhen(false)] out String?                         ErrorResponse,
                                       S2ParserOptions?                                         Options,
                                       CustomJObjectParserDelegate<FRBC_OperationModeElement>?  CustomFRBCOperationModeElementParser)
        {

            try
            {

                FRBCOperationModeElement = null;

                #region fill_level_range    [mandatory]

                if (!JSON.ParseMandatoryS2("fill_level_range",
                                           "fill level range",
                                           NumberRange.TryParse,
                                           Options,
                                           out NumberRange? fillLevelRange,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region fill_rate           [mandatory]

                if (!JSON.ParseMandatoryS2("fill_rate",
                                           "fill rate",
                                           NumberRange.TryParse,
                                           Options,
                                           out NumberRange? fillRate,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region power_ranges        [mandatory]

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

                #region running_costs       [optional]

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

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "fill_level_range",
                                                    "fill_rate",
                                                    "power_ranges",
                                                    "running_costs"))
                {
                    return false;
                }

                #endregion


                FRBCOperationModeElement = new FRBC_OperationModeElement(
                                               fillLevelRange,
                                               fillRate,
                                               powerRanges,
                                               runningCosts
                                           );

                if (CustomFRBCOperationModeElementParser is not null)
                    FRBCOperationModeElement = CustomFRBCOperationModeElementParser(JSON,
                                                                                    FRBCOperationModeElement);

                return true;

            }
            catch (Exception e)
            {
                FRBCOperationModeElement  = null;
                ErrorResponse             = "The given JSON representation of an FRBC operation mode element is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomFRBCOperationModeElementSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomFRBCOperationModeElementSerializer">A delegate to serialize custom FRBC operation mode elements.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_OperationModeElement>? CustomFRBCOperationModeElementSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("fill_level_range",  FillLevelRange.ToJSON()),
                                 new JProperty("fill_rate",         FillRate.      ToJSON()),
                                 new JProperty("power_ranges",      new JArray(PowerRanges.Select(powerRange => powerRange.ToJSON()))),

                           RunningCosts is not null
                               ? new JProperty("running_costs",     RunningCosts.ToJSON())
                               : null

                       );

            return CustomFRBCOperationModeElementSerializer is not null
                       ? CustomFRBCOperationModeElementSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this FRBC operation mode element.
        /// </summary>
        public FRBC_OperationModeElement Clone()

            => new (
                   FillLevelRange.Clone(),
                   FillRate.      Clone(),
                   [.. PowerRanges.Select(powerRange => powerRange.Clone())],
                   RunningCosts?. Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two FRBC operation mode elements for equality.
        /// </summary>
        public static Boolean operator == (FRBC_OperationModeElement? FRBCOperationModeElement1, FRBC_OperationModeElement? FRBCOperationModeElement2)
        {

            if (ReferenceEquals(FRBCOperationModeElement1, FRBCOperationModeElement2))
                return true;

            if (FRBCOperationModeElement1 is null || FRBCOperationModeElement2 is null)
                return false;

            return FRBCOperationModeElement1.Equals(FRBCOperationModeElement2);

        }

        /// <summary>
        /// Compares two FRBC operation mode elements for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_OperationModeElement? FRBCOperationModeElement1, FRBC_OperationModeElement? FRBCOperationModeElement2)
            => !(FRBCOperationModeElement1 == FRBCOperationModeElement2);

        #endregion

        #region IEquatable<FRBC_OperationModeElement> Members

        /// <summary>
        /// Compares two FRBC operation mode elements for equality.
        /// </summary>
        /// <param name="Object">An FRBC operation mode element to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_OperationModeElement frbcOperationModeElement && Equals(frbcOperationModeElement);

        /// <summary>
        /// Compares two FRBC operation mode elements for equality.
        /// </summary>
        /// <param name="FRBCOperationModeElement">An FRBC operation mode element to compare with.</param>
        public Boolean Equals(FRBC_OperationModeElement? FRBCOperationModeElement)

            => FRBCOperationModeElement is not null &&

               FillLevelRange.Equals(FRBCOperationModeElement.FillLevelRange) &&
               FillRate.      Equals(FRBCOperationModeElement.FillRate) &&
               PowerRanges.   SequenceEqual(FRBCOperationModeElement.PowerRanges) &&
               RunningCosts == FRBCOperationModeElement.RunningCosts;

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

                   $"fill level {FillLevelRange}, fill rate {FillRate}, {PowerRanges.Count} power range(s)",

                   RunningCosts is not null
                       ? $", running costs {RunningCosts}"
                       : ""

               );

        #endregion

    }

}

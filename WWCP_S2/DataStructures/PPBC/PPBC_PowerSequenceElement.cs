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
    /// An element of a PPBC power sequence: the (forecasted) power values of all
    /// commodity quantities for a given duration.
    /// </summary>
    public sealed class PPBC_PowerSequenceElement : IEquatable<PPBC_PowerSequenceElement>
    {

        #region Properties

        /// <summary>
        /// The duration of this power sequence element.
        /// </summary>
        [Mandatory]
        public Duration                             Duration       { get; }

        /// <summary>
        /// The value of power and deviations for the given duration. The list contains
        /// at least one power forecast value and at most one per commodity quantity.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PowerForecastValue>    PowerValues    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new PPBC power sequence element.
        /// </summary>
        /// <param name="Duration">The duration of this power sequence element.</param>
        /// <param name="PowerValues">The value of power and deviations for the given duration (1..10 entries, at most one per commodity quantity).</param>
        public PPBC_PowerSequenceElement(Duration                           Duration,
                                         IReadOnlyList<PowerForecastValue>  PowerValues)
        {

            ArgumentNullException.ThrowIfNull(PowerValues);

            if (PowerValues.Count < 1)
                throw new ArgumentException("The power values must contain at least one entry!",
                                            nameof(PowerValues));

            if (PowerValues.Count > 10)
                throw new ArgumentException($"The power values must not contain more than 10 entries, but {PowerValues.Count} were given!",
                                            nameof(PowerValues));

            var duplicate = PowerValues.GroupBy(powerValue => powerValue.CommodityQuantity).
                                        FirstOrDefault(group => group.Count() > 1);

            if (duplicate is not null)
                throw new ArgumentException($"The power values must contain at most one entry per commodity quantity, but '{duplicate.Key}' occurs {duplicate.Count()} times!",
                                            nameof(PowerValues));

            this.Duration     = Duration;
            this.PowerValues  = [.. PowerValues];

            unchecked
            {
                hashCode = this.Duration.   GetHashCode() * 3 ^
                           this.PowerValues.CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // PPBC.PowerSequenceElement.schema.json
        //   "title": "PPBC_PowerSequenceElement",
        //   "properties": {
        //     "duration":     { "$ref": "../schemas/Duration.schema.json",
        //                       "description": "Duration of the PPBC.PowerSequenceElement." },
        //     "power_values": { "type": "array", "minItems": 1, "maxItems": 10,
        //                       "items": { "$ref": "../schemas/PowerForecastValue.schema.json" },
        //                       "description": "The value of power and deviations for the given duration. The array should
        //                                       contain at least one PowerForecastValue and at most one PowerForecastValue
        //                                       per CommodityQuantity." }
        //   },
        //   "required": ["duration", "power_values"],
        //   "additionalProperties": false
        //
        // Semantic rule (CONVENTIONS.md §5): at most one power value per CommodityQuantity.

        #endregion

        #region (static) TryParse(JSON, out PPBC_PowerSequenceElement, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power sequence element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerSequenceElement">The parsed PPBC power sequence element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerSequenceElement?  PPBC_PowerSequenceElement,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)

            => TryParse(JSON,
                        out PPBC_PowerSequenceElement,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power sequence element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerSequenceElement">The parsed PPBC power sequence element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerSequenceElement?  PPBC_PowerSequenceElement,
                                       [NotNullWhen(false)] out String?                     ErrorResponse,
                                       S2ParserOptions?                                     Options)

            => TryParse(JSON,
                        out PPBC_PowerSequenceElement,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power sequence element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerSequenceElement">The parsed PPBC power sequence element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPPBC_PowerSequenceElementParser">A delegate to parse custom PPBC power sequence elements.</param>
        public static Boolean TryParse(JObject                                                  JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerSequenceElement?      PPBC_PowerSequenceElement,
                                       [NotNullWhen(false)] out String?                         ErrorResponse,
                                       S2ParserOptions?                                         Options,
                                       CustomJObjectParserDelegate<PPBC_PowerSequenceElement>?  CustomPPBC_PowerSequenceElementParser)
        {

            try
            {

                PPBC_PowerSequenceElement = null;

                #region duration        [mandatory]

                if (!JSON.ParseMandatoryS2Duration("duration",
                                                   "duration",
                                                   out Duration duration,
                                                   out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region power_values    [mandatory]

                if (!JSON.ParseMandatoryS2List("power_values",
                                               "power values",
                                               PowerForecastValue.TryParse,
                                               Options,
                                               1,
                                               10,
                                               out IReadOnlyList<PowerForecastValue>? powerValues,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "duration",
                                                    "power_values"))
                {
                    return false;
                }

                #endregion


                PPBC_PowerSequenceElement = new PPBC_PowerSequenceElement(
                                                duration,
                                                powerValues
                                            );

                if (CustomPPBC_PowerSequenceElementParser is not null)
                    PPBC_PowerSequenceElement = CustomPPBC_PowerSequenceElementParser(JSON,
                                                                                      PPBC_PowerSequenceElement);

                return true;

            }
            catch (Exception e)
            {
                PPBC_PowerSequenceElement  = null;
                ErrorResponse              = "The given JSON representation of a PPBC power sequence element is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPPBC_PowerSequenceElementSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPPBC_PowerSequenceElementSerializer">A delegate to serialize custom PPBC power sequence elements.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PPBC_PowerSequenceElement>? CustomPPBC_PowerSequenceElementSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("duration",      Duration.ToJSON()),
                           new JProperty("power_values",  new JArray(PowerValues.Select(powerValue => powerValue.ToJSON())))
                       );

            return CustomPPBC_PowerSequenceElementSerializer is not null
                       ? CustomPPBC_PowerSequenceElementSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this PPBC power sequence element.
        /// </summary>
        public PPBC_PowerSequenceElement Clone()

            => new (
                   Duration,
                   [.. PowerValues.Select(powerValue => powerValue.Clone())]
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two PPBC power sequence elements for equality.
        /// </summary>
        public static Boolean operator == (PPBC_PowerSequenceElement? PPBC_PowerSequenceElement1, PPBC_PowerSequenceElement? PPBC_PowerSequenceElement2)
        {

            if (ReferenceEquals(PPBC_PowerSequenceElement1, PPBC_PowerSequenceElement2))
                return true;

            if (PPBC_PowerSequenceElement1 is null || PPBC_PowerSequenceElement2 is null)
                return false;

            return PPBC_PowerSequenceElement1.Equals(PPBC_PowerSequenceElement2);

        }

        /// <summary>
        /// Compares two PPBC power sequence elements for inequality.
        /// </summary>
        public static Boolean operator != (PPBC_PowerSequenceElement? PPBC_PowerSequenceElement1, PPBC_PowerSequenceElement? PPBC_PowerSequenceElement2)
            => !(PPBC_PowerSequenceElement1 == PPBC_PowerSequenceElement2);

        #endregion

        #region IEquatable<PPBC_PowerSequenceElement> Members

        /// <summary>
        /// Compares two PPBC power sequence elements for equality.
        /// </summary>
        /// <param name="Object">A PPBC power sequence element to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PPBC_PowerSequenceElement ppbcPowerSequenceElement && Equals(ppbcPowerSequenceElement);

        /// <summary>
        /// Compares two PPBC power sequence elements for equality.
        /// </summary>
        /// <param name="PPBC_PowerSequenceElement">A PPBC power sequence element to compare with.</param>
        public Boolean Equals(PPBC_PowerSequenceElement? PPBC_PowerSequenceElement)

            => PPBC_PowerSequenceElement is not null &&

               Duration.   Equals       (PPBC_PowerSequenceElement.Duration) &&
               PowerValues.SequenceEqual(PPBC_PowerSequenceElement.PowerValues);

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

            => $"{Duration}: {PowerValues.Count} power value(s)";

        #endregion

    }

}

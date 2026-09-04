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
    /// An element of a power forecast: the power values that are expected for a
    /// given period of time. There is at least one power forecast value and at
    /// most one per commodity quantity.
    /// </summary>
    public sealed class PowerForecastElement : IEquatable<PowerForecastElement>
    {

        #region Properties

        /// <summary>
        /// The duration of the power forecast element.
        /// </summary>
        [Mandatory]
        public Duration                          Duration       { get; }

        /// <summary>
        /// The values of power that are expected for the given period of time.
        /// There shall be at least one power forecast value, and at most one
        /// power forecast value per commodity quantity.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PowerForecastValue>  PowerValues    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power forecast element.
        /// </summary>
        /// <param name="Duration">The duration of the power forecast element.</param>
        /// <param name="PowerValues">The values of power that are expected for the given period of time (1..10, at most one per commodity quantity).</param>
        public PowerForecastElement(Duration                          Duration,
                                    IReadOnlyList<PowerForecastValue>  PowerValues)
        {

            ArgumentNullException.ThrowIfNull(PowerValues);

            if (PowerValues.Count < 1)
                throw new ArgumentException("A power forecast element must contain at least one power forecast value!",
                                            nameof(PowerValues));

            if (PowerValues.Count > 10)
                throw new ArgumentException($"A power forecast element must not contain more than 10 power forecast values, but {PowerValues.Count} were given!",
                                            nameof(PowerValues));

            var duplicate = PowerValues.GroupBy(pv => pv.CommodityQuantity).FirstOrDefault(g => g.Count() > 1);

            if (duplicate is not null)
                throw new ArgumentException($"A power forecast element must contain at most one power forecast value per commodity quantity, but '{duplicate.Key}' occurs {duplicate.Count()} times!",
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

        // PowerForecastElement.schema.json
        //   "title": "PowerForecastElement",
        //   "properties": {
        //     "duration":     { "$ref": "../schemas/Duration.schema.json",
        //                       "description": "Duration of the PowerForecastElement" },
        //     "power_values": { "type": "array", "minItems": 1, "maxItems": 10,
        //                       "items": { "$ref": "../schemas/PowerForecastValue.schema.json" },
        //                       "description": "The values of power that are expected for the given period of time. There shall be
        //                                       at least one PowerForecastValue, and at most one PowerForecastValue per CommodityQuantity." }
        //   },
        //   "required": ["duration", "power_values"],
        //   "additionalProperties": false
        //
        // Semantic rule (CONVENTIONS.md §5): at most one PowerForecastValue per CommodityQuantity.

        #endregion

        #region (static) TryParse(JSON, out PowerForecastElement, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a power forecast element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerForecastElement">The parsed power forecast element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out PowerForecastElement?  PowerForecastElement,
                                       [NotNullWhen(false)] out String?                ErrorResponse)

            => TryParse(JSON,
                        out PowerForecastElement,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power forecast element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerForecastElement">The parsed power forecast element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out PowerForecastElement?  PowerForecastElement,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       S2ParserOptions?                                Options)

            => TryParse(JSON,
                        out PowerForecastElement,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power forecast element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerForecastElement">The parsed power forecast element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPowerForecastElementParser">A delegate to parse custom power forecast elements.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)]  out PowerForecastElement?      PowerForecastElement,
                                       [NotNullWhen(false)] out String?                    ErrorResponse,
                                       S2ParserOptions?                                    Options,
                                       CustomJObjectParserDelegate<PowerForecastElement>?  CustomPowerForecastElementParser)
        {

            try
            {

                PowerForecastElement = null;

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
                                               "power forecast values",
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


                PowerForecastElement = new PowerForecastElement(
                                           duration,
                                           powerValues
                                       );

                if (CustomPowerForecastElementParser is not null)
                    PowerForecastElement = CustomPowerForecastElementParser(JSON,
                                                                            PowerForecastElement);

                return true;

            }
            catch (Exception e)
            {
                PowerForecastElement  = null;
                ErrorResponse         = "The given JSON representation of a power forecast element is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerForecastElementSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPowerForecastElementSerializer">A delegate to serialize custom power forecast elements.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PowerForecastElement>? CustomPowerForecastElementSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("duration",      Duration.ToJSON()),
                           new JProperty("power_values",  new JArray(PowerValues.Select(powerValue => powerValue.ToJSON())))
                       );

            return CustomPowerForecastElementSerializer is not null
                       ? CustomPowerForecastElementSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power forecast element.
        /// </summary>
        public PowerForecastElement Clone()

            => new (
                   Duration,
                   [.. PowerValues.Select(powerValue => powerValue.Clone())]
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power forecast elements for equality.
        /// </summary>
        public static Boolean operator == (PowerForecastElement? PowerForecastElement1, PowerForecastElement? PowerForecastElement2)
        {

            if (ReferenceEquals(PowerForecastElement1, PowerForecastElement2))
                return true;

            if (PowerForecastElement1 is null || PowerForecastElement2 is null)
                return false;

            return PowerForecastElement1.Equals(PowerForecastElement2);

        }

        /// <summary>
        /// Compares two power forecast elements for inequality.
        /// </summary>
        public static Boolean operator != (PowerForecastElement? PowerForecastElement1, PowerForecastElement? PowerForecastElement2)
            => !(PowerForecastElement1 == PowerForecastElement2);

        #endregion

        #region IEquatable<PowerForecastElement> Members

        /// <summary>
        /// Compares two power forecast elements for equality.
        /// </summary>
        /// <param name="Object">A power forecast element to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PowerForecastElement powerForecastElement && Equals(powerForecastElement);

        /// <summary>
        /// Compares two power forecast elements for equality.
        /// </summary>
        /// <param name="PowerForecastElement">A power forecast element to compare with.</param>
        public Boolean Equals(PowerForecastElement? PowerForecastElement)

            => PowerForecastElement is not null &&

               Duration.   Equals       (PowerForecastElement.Duration) &&
               PowerValues.SequenceEqual(PowerForecastElement.PowerValues);

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
            => $"{Duration}: {String.Join(", ", PowerValues)}";

        #endregion

    }

}

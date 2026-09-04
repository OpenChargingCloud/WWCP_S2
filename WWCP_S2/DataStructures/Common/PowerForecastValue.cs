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
    /// A forecasted power value for a commodity quantity: the expected value and
    /// optional upper/lower boundaries with 68 %, 95 % and 100 % certainty.
    /// </summary>
    public sealed class PowerForecastValue : IEquatable<PowerForecastValue>
    {

        #region Properties

        /// <summary>
        /// The upper boundary of the range with 100 % certainty the power value is in it.
        /// </summary>
        [Optional]
        public Double?            ValueUpperLimit      { get; }

        /// <summary>
        /// The upper boundary of the range with 95 % certainty the power value is in it.
        /// </summary>
        [Optional]
        public Double?            ValueUpper95PPR      { get; }

        /// <summary>
        /// The upper boundary of the range with 68 % certainty the power value is in it.
        /// </summary>
        [Optional]
        public Double?            ValueUpper68PPR      { get; }

        /// <summary>
        /// The expected power value.
        /// </summary>
        [Mandatory]
        public Double             ValueExpected        { get; }

        /// <summary>
        /// The lower boundary of the range with 68 % certainty the power value is in it.
        /// </summary>
        [Optional]
        public Double?            ValueLower68PPR      { get; }

        /// <summary>
        /// The lower boundary of the range with 95 % certainty the power value is in it.
        /// </summary>
        [Optional]
        public Double?            ValueLower95PPR      { get; }

        /// <summary>
        /// The lower boundary of the range with 100 % certainty the power value is in it.
        /// </summary>
        [Optional]
        public Double?            ValueLowerLimit      { get; }

        /// <summary>
        /// The power quantity the value refers to.
        /// </summary>
        [Mandatory]
        public CommodityQuantity  CommodityQuantity    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power forecast value.
        /// </summary>
        /// <param name="ValueExpected">The expected power value.</param>
        /// <param name="CommodityQuantity">The power quantity the value refers to.</param>
        /// <param name="ValueUpperLimit">The optional upper boundary of the range with 100 % certainty the power value is in it.</param>
        /// <param name="ValueUpper95PPR">The optional upper boundary of the range with 95 % certainty the power value is in it.</param>
        /// <param name="ValueUpper68PPR">The optional upper boundary of the range with 68 % certainty the power value is in it.</param>
        /// <param name="ValueLower68PPR">The optional lower boundary of the range with 68 % certainty the power value is in it.</param>
        /// <param name="ValueLower95PPR">The optional lower boundary of the range with 95 % certainty the power value is in it.</param>
        /// <param name="ValueLowerLimit">The optional lower boundary of the range with 100 % certainty the power value is in it.</param>
        public PowerForecastValue(Double             ValueExpected,
                                  CommodityQuantity  CommodityQuantity,
                                  Double?            ValueUpperLimit   = null,
                                  Double?            ValueUpper95PPR   = null,
                                  Double?            ValueUpper68PPR   = null,
                                  Double?            ValueLower68PPR   = null,
                                  Double?            ValueLower95PPR   = null,
                                  Double?            ValueLowerLimit   = null)
        {

            if (!Double.IsFinite(ValueExpected))
                throw new ArgumentException("The expected power value must be a finite number!",
                                            nameof(ValueExpected));

            if (CommodityQuantity.IsNullOrEmpty)
                throw new ArgumentException("The commodity quantity of a power forecast value must not be null or empty!",
                                            nameof(CommodityQuantity));

            if (ValueUpperLimit.HasValue && !Double.IsFinite(ValueUpperLimit.Value))
                throw new ArgumentException("The upper limit must be a finite number!",       nameof(ValueUpperLimit));

            if (ValueUpper95PPR.HasValue && !Double.IsFinite(ValueUpper95PPR.Value))
                throw new ArgumentException("The upper 95 % boundary must be a finite number!", nameof(ValueUpper95PPR));

            if (ValueUpper68PPR.HasValue && !Double.IsFinite(ValueUpper68PPR.Value))
                throw new ArgumentException("The upper 68 % boundary must be a finite number!", nameof(ValueUpper68PPR));

            if (ValueLower68PPR.HasValue && !Double.IsFinite(ValueLower68PPR.Value))
                throw new ArgumentException("The lower 68 % boundary must be a finite number!", nameof(ValueLower68PPR));

            if (ValueLower95PPR.HasValue && !Double.IsFinite(ValueLower95PPR.Value))
                throw new ArgumentException("The lower 95 % boundary must be a finite number!", nameof(ValueLower95PPR));

            if (ValueLowerLimit.HasValue && !Double.IsFinite(ValueLowerLimit.Value))
                throw new ArgumentException("The lower limit must be a finite number!",       nameof(ValueLowerLimit));

            this.ValueUpperLimit    = ValueUpperLimit;
            this.ValueUpper95PPR    = ValueUpper95PPR;
            this.ValueUpper68PPR    = ValueUpper68PPR;
            this.ValueExpected      = ValueExpected;
            this.ValueLower68PPR    = ValueLower68PPR;
            this.ValueLower95PPR    = ValueLower95PPR;
            this.ValueLowerLimit    = ValueLowerLimit;
            this.CommodityQuantity  = CommodityQuantity;

            unchecked
            {
                hashCode = (this.ValueUpperLimit?.  GetHashCode() ?? 0) * 23 ^
                           (this.ValueUpper95PPR?.  GetHashCode() ?? 0) * 19 ^
                           (this.ValueUpper68PPR?.  GetHashCode() ?? 0) * 17 ^
                            this.ValueExpected.     GetHashCode()       * 13 ^
                           (this.ValueLower68PPR?.  GetHashCode() ?? 0) * 11 ^
                           (this.ValueLower95PPR?.  GetHashCode() ?? 0) *  7 ^
                           (this.ValueLowerLimit?.  GetHashCode() ?? 0) *  3 ^
                            this.CommodityQuantity. GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // PowerForecastValue.schema.json
        //   "title": "PowerForecastValue",
        //   "properties": {
        //     "value_upper_limit":  { "type": "number", "description": "The upper boundary of the range with 100 % certainty the power value is in it" },
        //     "value_upper_95PPR":  { "type": "number", "description": "The upper boundary of the range with 95 % certainty the power value is in it" },
        //     "value_upper_68PPR":  { "type": "number", "description": "The upper boundary of the range with 68 % certainty the power value is in it" },
        //     "value_expected":     { "type": "number", "description": "The expected power value." },
        //     "value_lower_68PPR":  { "type": "number", "description": "The lower boundary of the range with 68 % certainty the power value is in it" },
        //     "value_lower_95PPR":  { "type": "number", "description": "The lower boundary of the range with 95 % certainty the power value is in it" },
        //     "value_lower_limit":  { "type": "number", "description": "The lower boundary of the range with 100 % certainty the power value is in it" },
        //     "commodity_quantity": { "$ref": "../schemas/CommodityQuantity.schema.json",
        //                             "description": "The power quantity the value refers to" }
        //   },
        //   "required": ["value_expected", "commodity_quantity"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out PowerForecastValue, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a power forecast value.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerForecastValue">The parsed power forecast value.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out PowerForecastValue?  PowerForecastValue,
                                       [NotNullWhen(false)] out String?              ErrorResponse)

            => TryParse(JSON,
                        out PowerForecastValue,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power forecast value.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerForecastValue">The parsed power forecast value.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out PowerForecastValue?  PowerForecastValue,
                                       [NotNullWhen(false)] out String?              ErrorResponse,
                                       S2ParserOptions?                              Options)

            => TryParse(JSON,
                        out PowerForecastValue,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power forecast value.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerForecastValue">The parsed power forecast value.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPowerForecastValueParser">A delegate to parse custom power forecast values.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out PowerForecastValue?      PowerForecastValue,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options,
                                       CustomJObjectParserDelegate<PowerForecastValue>?  CustomPowerForecastValueParser)
        {

            try
            {

                PowerForecastValue = null;

                #region value_upper_limit     [optional]

                if (!JSON.ParseOptionalS2Number("value_upper_limit",
                                                "upper limit",
                                                out Double? valueUpperLimit,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region value_upper_95PPR     [optional]

                if (!JSON.ParseOptionalS2Number("value_upper_95PPR",
                                                "upper 95 % boundary",
                                                out Double? valueUpper95PPR,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region value_upper_68PPR     [optional]

                if (!JSON.ParseOptionalS2Number("value_upper_68PPR",
                                                "upper 68 % boundary",
                                                out Double? valueUpper68PPR,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region value_expected        [mandatory]

                if (!JSON.ParseMandatoryS2Number("value_expected",
                                                 "expected power value",
                                                 out Double valueExpected,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region value_lower_68PPR     [optional]

                if (!JSON.ParseOptionalS2Number("value_lower_68PPR",
                                                "lower 68 % boundary",
                                                out Double? valueLower68PPR,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region value_lower_95PPR     [optional]

                if (!JSON.ParseOptionalS2Number("value_lower_95PPR",
                                                "lower 95 % boundary",
                                                out Double? valueLower95PPR,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region value_lower_limit     [optional]

                if (!JSON.ParseOptionalS2Number("value_lower_limit",
                                                "lower limit",
                                                out Double? valueLowerLimit,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region commodity_quantity    [mandatory]

                if (!JSON.ParseMandatoryS2Enum("commodity_quantity",
                                               "commodity quantity",
                                               CommodityQuantity.TryParse,
                                               Options,
                                               out CommodityQuantity commodityQuantity,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "value_upper_limit",
                                                    "value_upper_95PPR",
                                                    "value_upper_68PPR",
                                                    "value_expected",
                                                    "value_lower_68PPR",
                                                    "value_lower_95PPR",
                                                    "value_lower_limit",
                                                    "commodity_quantity"))
                {
                    return false;
                }

                #endregion


                PowerForecastValue = new PowerForecastValue(
                                         valueExpected,
                                         commodityQuantity,
                                         valueUpperLimit,
                                         valueUpper95PPR,
                                         valueUpper68PPR,
                                         valueLower68PPR,
                                         valueLower95PPR,
                                         valueLowerLimit
                                     );

                if (CustomPowerForecastValueParser is not null)
                    PowerForecastValue = CustomPowerForecastValueParser(JSON,
                                                                        PowerForecastValue);

                return true;

            }
            catch (Exception e)
            {
                PowerForecastValue  = null;
                ErrorResponse       = "The given JSON representation of a power forecast value is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerForecastValueSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPowerForecastValueSerializer">A delegate to serialize custom power forecast values.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PowerForecastValue>? CustomPowerForecastValueSerializer = null)
        {

            var json = JSONObject.Create(

                           ValueUpperLimit.HasValue
                               ? new JProperty("value_upper_limit",   ValueUpperLimit.Value)
                               : null,

                           ValueUpper95PPR.HasValue
                               ? new JProperty("value_upper_95PPR",   ValueUpper95PPR.Value)
                               : null,

                           ValueUpper68PPR.HasValue
                               ? new JProperty("value_upper_68PPR",   ValueUpper68PPR.Value)
                               : null,

                                 new JProperty("value_expected",      ValueExpected),

                           ValueLower68PPR.HasValue
                               ? new JProperty("value_lower_68PPR",   ValueLower68PPR.Value)
                               : null,

                           ValueLower95PPR.HasValue
                               ? new JProperty("value_lower_95PPR",   ValueLower95PPR.Value)
                               : null,

                           ValueLowerLimit.HasValue
                               ? new JProperty("value_lower_limit",   ValueLowerLimit.Value)
                               : null,

                                 new JProperty("commodity_quantity",  CommodityQuantity.ToString())

                       );

            return CustomPowerForecastValueSerializer is not null
                       ? CustomPowerForecastValueSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power forecast value.
        /// </summary>
        public PowerForecastValue Clone()

            => new (
                   ValueExpected,
                   CommodityQuantity.Clone(),
                   ValueUpperLimit,
                   ValueUpper95PPR,
                   ValueUpper68PPR,
                   ValueLower68PPR,
                   ValueLower95PPR,
                   ValueLowerLimit
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power forecast values for equality.
        /// </summary>
        public static Boolean operator == (PowerForecastValue? PowerForecastValue1, PowerForecastValue? PowerForecastValue2)
        {

            if (ReferenceEquals(PowerForecastValue1, PowerForecastValue2))
                return true;

            if (PowerForecastValue1 is null || PowerForecastValue2 is null)
                return false;

            return PowerForecastValue1.Equals(PowerForecastValue2);

        }

        /// <summary>
        /// Compares two power forecast values for inequality.
        /// </summary>
        public static Boolean operator != (PowerForecastValue? PowerForecastValue1, PowerForecastValue? PowerForecastValue2)
            => !(PowerForecastValue1 == PowerForecastValue2);

        #endregion

        #region IEquatable<PowerForecastValue> Members

        /// <summary>
        /// Compares two power forecast values for equality.
        /// </summary>
        /// <param name="Object">A power forecast value to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PowerForecastValue powerForecastValue && Equals(powerForecastValue);

        /// <summary>
        /// Compares two power forecast values for equality.
        /// </summary>
        /// <param name="PowerForecastValue">A power forecast value to compare with.</param>
        public Boolean Equals(PowerForecastValue? PowerForecastValue)

            => PowerForecastValue is not null &&

               ValueExpected.    Equals(PowerForecastValue.ValueExpected)     &&
               CommodityQuantity.Equals(PowerForecastValue.CommodityQuantity) &&

               Nullable.Equals(ValueUpperLimit, PowerForecastValue.ValueUpperLimit) &&
               Nullable.Equals(ValueUpper95PPR, PowerForecastValue.ValueUpper95PPR) &&
               Nullable.Equals(ValueUpper68PPR, PowerForecastValue.ValueUpper68PPR) &&
               Nullable.Equals(ValueLower68PPR, PowerForecastValue.ValueLower68PPR) &&
               Nullable.Equals(ValueLower95PPR, PowerForecastValue.ValueLower95PPR) &&
               Nullable.Equals(ValueLowerLimit, PowerForecastValue.ValueLowerLimit);

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

                   $"{ValueExpected} ({CommodityQuantity})",

                   ValueLowerLimit.HasValue || ValueUpperLimit.HasValue
                       ? $" [{ValueLowerLimit?.ToString() ?? "?"} .. {ValueUpperLimit?.ToString() ?? "?"}]"
                       : ""

               );

        #endregion

    }

}

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
    /// One element of a Demand Driven Based Control (DDBC) average demand rate forecast:
    /// the expected demand rate for the given duration, optionally with probability bands.
    /// </summary>
    public sealed class DDBC_AverageDemandRateForecastElement : IEquatable<DDBC_AverageDemandRateForecastElement>
    {

        #region Properties

        /// <summary>
        /// The duration of the element.
        /// </summary>
        [Mandatory]
        public Duration  Duration                 { get; }

        /// <summary>
        /// The optional upper limit of the range with a 100 % probability that the demand rate is within that range.
        /// </summary>
        [Optional]
        public Double?   DemandRateUpperLimit     { get; }

        /// <summary>
        /// The optional upper limit of the range with a 95 % probability that the demand rate is within that range.
        /// </summary>
        [Optional]
        public Double?   DemandRateUpper95PPR     { get; }

        /// <summary>
        /// The optional upper limit of the range with a 68 % probability that the demand rate is within that range.
        /// </summary>
        [Optional]
        public Double?   DemandRateUpper68PPR     { get; }

        /// <summary>
        /// The most likely value for the demand rate; the expected increase or decrease of the fill_level per second.
        /// </summary>
        [Mandatory]
        public Double    DemandRateExpected       { get; }

        /// <summary>
        /// The optional lower limit of the range with a 68 % probability that the demand rate is within that range.
        /// </summary>
        [Optional]
        public Double?   DemandRateLower68PPR     { get; }

        /// <summary>
        /// The optional lower limit of the range with a 95 % probability that the demand rate is within that range.
        /// </summary>
        [Optional]
        public Double?   DemandRateLower95PPR     { get; }

        /// <summary>
        /// The optional lower limit of the range with a 100 % probability that the demand rate is within that range.
        /// </summary>
        [Optional]
        public Double?   DemandRateLowerLimit     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new DDBC average demand rate forecast element.
        /// </summary>
        /// <param name="Duration">The duration of the element.</param>
        /// <param name="DemandRateExpected">The most likely value for the demand rate.</param>
        /// <param name="DemandRateUpperLimit">The optional upper limit of the 100 % probability range.</param>
        /// <param name="DemandRateUpper95PPR">The optional upper limit of the 95 % probability range.</param>
        /// <param name="DemandRateUpper68PPR">The optional upper limit of the 68 % probability range.</param>
        /// <param name="DemandRateLower68PPR">The optional lower limit of the 68 % probability range.</param>
        /// <param name="DemandRateLower95PPR">The optional lower limit of the 95 % probability range.</param>
        /// <param name="DemandRateLowerLimit">The optional lower limit of the 100 % probability range.</param>
        public DDBC_AverageDemandRateForecastElement(Duration  Duration,
                                                     Double    DemandRateExpected,
                                                     Double?   DemandRateUpperLimit   = null,
                                                     Double?   DemandRateUpper95PPR   = null,
                                                     Double?   DemandRateUpper68PPR   = null,
                                                     Double?   DemandRateLower68PPR   = null,
                                                     Double?   DemandRateLower95PPR   = null,
                                                     Double?   DemandRateLowerLimit   = null)
        {

            if (Double.IsNaN(DemandRateExpected))
                throw new ArgumentException("The expected demand rate must not be NaN!",
                                            nameof(DemandRateExpected));

            if (DemandRateUpperLimit.HasValue && Double.IsNaN(DemandRateUpperLimit.Value))
                throw new ArgumentException("The upper limit of the demand rate must not be NaN!",
                                            nameof(DemandRateUpperLimit));

            if (DemandRateUpper95PPR.HasValue && Double.IsNaN(DemandRateUpper95PPR.Value))
                throw new ArgumentException("The upper 95 % limit of the demand rate must not be NaN!",
                                            nameof(DemandRateUpper95PPR));

            if (DemandRateUpper68PPR.HasValue && Double.IsNaN(DemandRateUpper68PPR.Value))
                throw new ArgumentException("The upper 68 % limit of the demand rate must not be NaN!",
                                            nameof(DemandRateUpper68PPR));

            if (DemandRateLower68PPR.HasValue && Double.IsNaN(DemandRateLower68PPR.Value))
                throw new ArgumentException("The lower 68 % limit of the demand rate must not be NaN!",
                                            nameof(DemandRateLower68PPR));

            if (DemandRateLower95PPR.HasValue && Double.IsNaN(DemandRateLower95PPR.Value))
                throw new ArgumentException("The lower 95 % limit of the demand rate must not be NaN!",
                                            nameof(DemandRateLower95PPR));

            if (DemandRateLowerLimit.HasValue && Double.IsNaN(DemandRateLowerLimit.Value))
                throw new ArgumentException("The lower limit of the demand rate must not be NaN!",
                                            nameof(DemandRateLowerLimit));

            this.Duration              = Duration;
            this.DemandRateExpected    = DemandRateExpected;
            this.DemandRateUpperLimit  = DemandRateUpperLimit;
            this.DemandRateUpper95PPR  = DemandRateUpper95PPR;
            this.DemandRateUpper68PPR  = DemandRateUpper68PPR;
            this.DemandRateLower68PPR  = DemandRateLower68PPR;
            this.DemandRateLower95PPR  = DemandRateLower95PPR;
            this.DemandRateLowerLimit  = DemandRateLowerLimit;

            unchecked
            {
                hashCode = this.Duration.             GetHashCode()       * 19 ^
                           this.DemandRateExpected.   GetHashCode()       * 17 ^
                          (this.DemandRateUpperLimit?.GetHashCode() ?? 0) * 13 ^
                          (this.DemandRateUpper95PPR?.GetHashCode() ?? 0) * 11 ^
                          (this.DemandRateUpper68PPR?.GetHashCode() ?? 0) *  7 ^
                          (this.DemandRateLower68PPR?.GetHashCode() ?? 0) *  5 ^
                          (this.DemandRateLower95PPR?.GetHashCode() ?? 0) *  3 ^
                          (this.DemandRateLowerLimit?.GetHashCode() ?? 0);
            }

        }

        #endregion


        #region Documentation

        // DDBC.AverageDemandRateForecastElement.schema.json
        //   "title": "DDBC_AverageDemandRateForecastElement",
        //   "properties": {
        //     "duration":                { "$ref": "../schemas/Duration.schema.json",
        //                                  "description": "Duration of the element" },
        //     "demand_rate_upper_limit": { "type": "number",
        //                                  "description": "The upper limit of the range with a 100 % probability that the demand rate is within that range" },
        //     "demand_rate_upper_95PPR": { "type": "number",
        //                                  "description": "The upper limit of the range with a 95 % probability that the demand rate is within that range" },
        //     "demand_rate_upper_68PPR": { "type": "number",
        //                                  "description": "The upper limit of the range with a 68 % probability that the demand rate is within that range" },
        //     "demand_rate_expected":    { "type": "number",
        //                                  "description": "The most likely value for the demand rate; the expected increase or decrease of the fill_level per second" },
        //     "demand_rate_lower_68PPR": { "type": "number",
        //                                  "description": "The lower limit of the range with a 68 % probability that the demand rate is within that range" },
        //     "demand_rate_lower_95PPR": { "type": "number",
        //                                  "description": "The lower limit of the range with a 95 % probability that the demand rate is within that range" },
        //     "demand_rate_lower_limit": { "type": "number",
        //                                  "description": "The lower limit of the range with a 100 % probability that the demand rate is within that range" }
        //   },
        //   "required": ["duration", "demand_rate_expected"],
        //   "additionalProperties": false
        //
        // No semantic rule of CONVENTIONS.md §5 / PLAN.md §3.3 applies to this type (the ordering of the
        // probability bands is not validated by s2-python either); NaN values are rejected like in NumberRange.

        #endregion

        #region (static) TryParse(JSON, out DDBC_AverageDemandRateForecastElement, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a DDBC average demand rate forecast element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCAverageDemandRateForecastElement">The parsed DDBC average demand rate forecast element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                          JSON,
                                       [NotNullWhen(true)]  out DDBC_AverageDemandRateForecastElement?  DDBCAverageDemandRateForecastElement,
                                       [NotNullWhen(false)] out String?                                 ErrorResponse)

            => TryParse(JSON,
                        out DDBCAverageDemandRateForecastElement,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC average demand rate forecast element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCAverageDemandRateForecastElement">The parsed DDBC average demand rate forecast element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                          JSON,
                                       [NotNullWhen(true)]  out DDBC_AverageDemandRateForecastElement?  DDBCAverageDemandRateForecastElement,
                                       [NotNullWhen(false)] out String?                                 ErrorResponse,
                                       S2ParserOptions?                                                 Options)

            => TryParse(JSON,
                        out DDBCAverageDemandRateForecastElement,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC average demand rate forecast element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCAverageDemandRateForecastElement">The parsed DDBC average demand rate forecast element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomDDBCAverageDemandRateForecastElementParser">A delegate to parse custom DDBC average demand rate forecast elements.</param>
        public static Boolean TryParse(JObject                                                              JSON,
                                       [NotNullWhen(true)]  out DDBC_AverageDemandRateForecastElement?      DDBCAverageDemandRateForecastElement,
                                       [NotNullWhen(false)] out String?                                     ErrorResponse,
                                       S2ParserOptions?                                                     Options,
                                       CustomJObjectParserDelegate<DDBC_AverageDemandRateForecastElement>?  CustomDDBCAverageDemandRateForecastElementParser)
        {

            try
            {

                DDBCAverageDemandRateForecastElement = null;

                #region duration                   [mandatory]

                if (!JSON.ParseMandatoryS2Duration("duration",
                                                   "duration",
                                                   out Duration duration,
                                                   out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region demand_rate_upper_limit    [optional]

                if (!JSON.ParseOptionalS2Number("demand_rate_upper_limit",
                                                "demand rate upper limit",
                                                out Double? demandRateUpperLimit,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region demand_rate_upper_95PPR    [optional]

                if (!JSON.ParseOptionalS2Number("demand_rate_upper_95PPR",
                                                "demand rate upper 95PPR",
                                                out Double? demandRateUpper95PPR,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region demand_rate_upper_68PPR    [optional]

                if (!JSON.ParseOptionalS2Number("demand_rate_upper_68PPR",
                                                "demand rate upper 68PPR",
                                                out Double? demandRateUpper68PPR,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region demand_rate_expected       [mandatory]

                if (!JSON.ParseMandatoryS2Number("demand_rate_expected",
                                                 "demand rate expected",
                                                 out Double demandRateExpected,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region demand_rate_lower_68PPR    [optional]

                if (!JSON.ParseOptionalS2Number("demand_rate_lower_68PPR",
                                                "demand rate lower 68PPR",
                                                out Double? demandRateLower68PPR,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region demand_rate_lower_95PPR    [optional]

                if (!JSON.ParseOptionalS2Number("demand_rate_lower_95PPR",
                                                "demand rate lower 95PPR",
                                                out Double? demandRateLower95PPR,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region demand_rate_lower_limit    [optional]

                if (!JSON.ParseOptionalS2Number("demand_rate_lower_limit",
                                                "demand rate lower limit",
                                                out Double? demandRateLowerLimit,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "duration",
                                                    "demand_rate_upper_limit",
                                                    "demand_rate_upper_95PPR",
                                                    "demand_rate_upper_68PPR",
                                                    "demand_rate_expected",
                                                    "demand_rate_lower_68PPR",
                                                    "demand_rate_lower_95PPR",
                                                    "demand_rate_lower_limit"))
                {
                    return false;
                }

                #endregion


                DDBCAverageDemandRateForecastElement = new DDBC_AverageDemandRateForecastElement(
                                                           duration,
                                                           demandRateExpected,
                                                           demandRateUpperLimit,
                                                           demandRateUpper95PPR,
                                                           demandRateUpper68PPR,
                                                           demandRateLower68PPR,
                                                           demandRateLower95PPR,
                                                           demandRateLowerLimit
                                                       );

                if (CustomDDBCAverageDemandRateForecastElementParser is not null)
                    DDBCAverageDemandRateForecastElement = CustomDDBCAverageDemandRateForecastElementParser(JSON,
                                                                                                            DDBCAverageDemandRateForecastElement);

                return true;

            }
            catch (Exception e)
            {
                DDBCAverageDemandRateForecastElement  = null;
                ErrorResponse                         = "The given JSON representation of a DDBC average demand rate forecast element is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDDBCAverageDemandRateForecastElementSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomDDBCAverageDemandRateForecastElementSerializer">A delegate to serialize custom DDBC average demand rate forecast elements.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DDBC_AverageDemandRateForecastElement>? CustomDDBCAverageDemandRateForecastElementSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("duration",                 Duration.ToJSON()),

                           DemandRateUpperLimit.HasValue
                               ? new JProperty("demand_rate_upper_limit",  DemandRateUpperLimit.Value)
                               : null,

                           DemandRateUpper95PPR.HasValue
                               ? new JProperty("demand_rate_upper_95PPR",  DemandRateUpper95PPR.Value)
                               : null,

                           DemandRateUpper68PPR.HasValue
                               ? new JProperty("demand_rate_upper_68PPR",  DemandRateUpper68PPR.Value)
                               : null,

                                 new JProperty("demand_rate_expected",     DemandRateExpected),

                           DemandRateLower68PPR.HasValue
                               ? new JProperty("demand_rate_lower_68PPR",  DemandRateLower68PPR.Value)
                               : null,

                           DemandRateLower95PPR.HasValue
                               ? new JProperty("demand_rate_lower_95PPR",  DemandRateLower95PPR.Value)
                               : null,

                           DemandRateLowerLimit.HasValue
                               ? new JProperty("demand_rate_lower_limit",  DemandRateLowerLimit.Value)
                               : null

                       );

            return CustomDDBCAverageDemandRateForecastElementSerializer is not null
                       ? CustomDDBCAverageDemandRateForecastElementSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this DDBC average demand rate forecast element.
        /// </summary>
        public DDBC_AverageDemandRateForecastElement Clone()

            => new (
                   Duration,
                   DemandRateExpected,
                   DemandRateUpperLimit,
                   DemandRateUpper95PPR,
                   DemandRateUpper68PPR,
                   DemandRateLower68PPR,
                   DemandRateLower95PPR,
                   DemandRateLowerLimit
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two DDBC average demand rate forecast elements for equality.
        /// </summary>
        public static Boolean operator == (DDBC_AverageDemandRateForecastElement? DDBCAverageDemandRateForecastElement1, DDBC_AverageDemandRateForecastElement? DDBCAverageDemandRateForecastElement2)
        {

            if (ReferenceEquals(DDBCAverageDemandRateForecastElement1, DDBCAverageDemandRateForecastElement2))
                return true;

            if (DDBCAverageDemandRateForecastElement1 is null || DDBCAverageDemandRateForecastElement2 is null)
                return false;

            return DDBCAverageDemandRateForecastElement1.Equals(DDBCAverageDemandRateForecastElement2);

        }

        /// <summary>
        /// Compares two DDBC average demand rate forecast elements for inequality.
        /// </summary>
        public static Boolean operator != (DDBC_AverageDemandRateForecastElement? DDBCAverageDemandRateForecastElement1, DDBC_AverageDemandRateForecastElement? DDBCAverageDemandRateForecastElement2)
            => !(DDBCAverageDemandRateForecastElement1 == DDBCAverageDemandRateForecastElement2);

        #endregion

        #region IEquatable<DDBC_AverageDemandRateForecastElement> Members

        /// <summary>
        /// Compares two DDBC average demand rate forecast elements for equality.
        /// </summary>
        /// <param name="Object">A DDBC average demand rate forecast element to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is DDBC_AverageDemandRateForecastElement ddbcAverageDemandRateForecastElement && Equals(ddbcAverageDemandRateForecastElement);

        /// <summary>
        /// Compares two DDBC average demand rate forecast elements for equality.
        /// </summary>
        /// <param name="DDBCAverageDemandRateForecastElement">A DDBC average demand rate forecast element to compare with.</param>
        public Boolean Equals(DDBC_AverageDemandRateForecastElement? DDBCAverageDemandRateForecastElement)

            => DDBCAverageDemandRateForecastElement is not null &&

               Duration.          Equals(DDBCAverageDemandRateForecastElement.Duration) &&
               DemandRateExpected.Equals(DDBCAverageDemandRateForecastElement.DemandRateExpected) &&

               Nullable.Equals(DemandRateUpperLimit, DDBCAverageDemandRateForecastElement.DemandRateUpperLimit) &&
               Nullable.Equals(DemandRateUpper95PPR, DDBCAverageDemandRateForecastElement.DemandRateUpper95PPR) &&
               Nullable.Equals(DemandRateUpper68PPR, DDBCAverageDemandRateForecastElement.DemandRateUpper68PPR) &&
               Nullable.Equals(DemandRateLower68PPR, DDBCAverageDemandRateForecastElement.DemandRateLower68PPR) &&
               Nullable.Equals(DemandRateLower95PPR, DDBCAverageDemandRateForecastElement.DemandRateLower95PPR) &&
               Nullable.Equals(DemandRateLowerLimit, DDBCAverageDemandRateForecastElement.DemandRateLowerLimit);

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

                   $"{Duration}: {DemandRateExpected} expected",

                   DemandRateLowerLimit.HasValue || DemandRateUpperLimit.HasValue
                       ? $" [{DemandRateLowerLimit?.ToString() ?? "?"} .. {DemandRateUpperLimit?.ToString() ?? "?"}]"
                       : ""

               );

        #endregion

    }

}

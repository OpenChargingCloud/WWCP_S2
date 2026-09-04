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
    /// An element of a FRBC.UsageForecast: the expected usage rate of the storage
    /// (decrease of the fill level per second) for a given duration, optionally with
    /// probability ranges.
    /// </summary>
    public sealed class FRBC_UsageForecastElement : IEquatable<FRBC_UsageForecastElement>
    {

        #region Properties

        /// <summary>
        /// Indicator for how long the given usage rate is valid.
        /// </summary>
        [Mandatory]
        public Duration  Duration                { get; }

        /// <summary>
        /// The upper limit of the range with a 100 % probability that the usage rate is within
        /// that range. A positive value indicates that the fill level will decrease due to usage.
        /// </summary>
        [Optional]
        public Double?   UsageRateUpperLimit     { get; }

        /// <summary>
        /// The upper limit of the range with a 95 % probability that the usage rate is within
        /// that range. A positive value indicates that the fill level will decrease due to usage.
        /// </summary>
        [Optional]
        public Double?   UsageRateUpper95PPR     { get; }

        /// <summary>
        /// The upper limit of the range with a 68 % probability that the usage rate is within
        /// that range. A positive value indicates that the fill level will decrease due to usage.
        /// </summary>
        [Optional]
        public Double?   UsageRateUpper68PPR     { get; }

        /// <summary>
        /// The most likely value for the usage rate; the expected increase or decrease of the
        /// fill level per second. A positive value indicates that the fill level will decrease due to usage.
        /// </summary>
        [Mandatory]
        public Double    UsageRateExpected       { get; }

        /// <summary>
        /// The lower limit of the range with a 68 % probability that the usage rate is within
        /// that range. A positive value indicates that the fill level will decrease due to usage.
        /// </summary>
        [Optional]
        public Double?   UsageRateLower68PPR     { get; }

        /// <summary>
        /// The lower limit of the range with a 95 % probability that the usage rate is within
        /// that range. A positive value indicates that the fill level will decrease due to usage.
        /// </summary>
        [Optional]
        public Double?   UsageRateLower95PPR     { get; }

        /// <summary>
        /// The lower limit of the range with a 100 % probability that the usage rate is within
        /// that range. A positive value indicates that the fill level will decrease due to usage.
        /// </summary>
        [Optional]
        public Double?   UsageRateLowerLimit     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new usage forecast element.
        /// </summary>
        /// <param name="Duration">Indicator for how long the given usage rate is valid.</param>
        /// <param name="UsageRateExpected">The most likely value for the usage rate (decrease of the fill level per second).</param>
        /// <param name="UsageRateUpperLimit">The optional upper limit of the range with a 100 % probability that the usage rate is within that range.</param>
        /// <param name="UsageRateUpper95PPR">The optional upper limit of the range with a 95 % probability that the usage rate is within that range.</param>
        /// <param name="UsageRateUpper68PPR">The optional upper limit of the range with a 68 % probability that the usage rate is within that range.</param>
        /// <param name="UsageRateLower68PPR">The optional lower limit of the range with a 68 % probability that the usage rate is within that range.</param>
        /// <param name="UsageRateLower95PPR">The optional lower limit of the range with a 95 % probability that the usage rate is within that range.</param>
        /// <param name="UsageRateLowerLimit">The optional lower limit of the range with a 100 % probability that the usage rate is within that range.</param>
        public FRBC_UsageForecastElement(Duration  Duration,
                                         Double    UsageRateExpected,
                                         Double?   UsageRateUpperLimit   = null,
                                         Double?   UsageRateUpper95PPR   = null,
                                         Double?   UsageRateUpper68PPR   = null,
                                         Double?   UsageRateLower68PPR   = null,
                                         Double?   UsageRateLower95PPR   = null,
                                         Double?   UsageRateLowerLimit   = null)
        {

            if (Double.IsNaN(UsageRateExpected))
                throw new ArgumentException("The expected usage rate must not be NaN!",
                                            nameof(UsageRateExpected));

            if (UsageRateUpperLimit.HasValue && Double.IsNaN(UsageRateUpperLimit.Value))
                throw new ArgumentException("The usage rate upper limit must not be NaN!",
                                            nameof(UsageRateUpperLimit));

            if (UsageRateUpper95PPR.HasValue && Double.IsNaN(UsageRateUpper95PPR.Value))
                throw new ArgumentException("The usage rate upper 95 PPR must not be NaN!",
                                            nameof(UsageRateUpper95PPR));

            if (UsageRateUpper68PPR.HasValue && Double.IsNaN(UsageRateUpper68PPR.Value))
                throw new ArgumentException("The usage rate upper 68 PPR must not be NaN!",
                                            nameof(UsageRateUpper68PPR));

            if (UsageRateLower68PPR.HasValue && Double.IsNaN(UsageRateLower68PPR.Value))
                throw new ArgumentException("The usage rate lower 68 PPR must not be NaN!",
                                            nameof(UsageRateLower68PPR));

            if (UsageRateLower95PPR.HasValue && Double.IsNaN(UsageRateLower95PPR.Value))
                throw new ArgumentException("The usage rate lower 95 PPR must not be NaN!",
                                            nameof(UsageRateLower95PPR));

            if (UsageRateLowerLimit.HasValue && Double.IsNaN(UsageRateLowerLimit.Value))
                throw new ArgumentException("The usage rate lower limit must not be NaN!",
                                            nameof(UsageRateLowerLimit));

            this.Duration             = Duration;
            this.UsageRateUpperLimit  = UsageRateUpperLimit;
            this.UsageRateUpper95PPR  = UsageRateUpper95PPR;
            this.UsageRateUpper68PPR  = UsageRateUpper68PPR;
            this.UsageRateExpected    = UsageRateExpected;
            this.UsageRateLower68PPR  = UsageRateLower68PPR;
            this.UsageRateLower95PPR  = UsageRateLower95PPR;
            this.UsageRateLowerLimit  = UsageRateLowerLimit;

            unchecked
            {
                hashCode =  this.Duration.            GetHashCode()       * 23 ^
                           (this.UsageRateUpperLimit?.GetHashCode() ?? 0) * 19 ^
                           (this.UsageRateUpper95PPR?.GetHashCode() ?? 0) * 17 ^
                           (this.UsageRateUpper68PPR?.GetHashCode() ?? 0) * 13 ^
                            this.UsageRateExpected.   GetHashCode()       * 11 ^
                           (this.UsageRateLower68PPR?.GetHashCode() ?? 0) *  7 ^
                           (this.UsageRateLower95PPR?.GetHashCode() ?? 0) *  5 ^
                           (this.UsageRateLowerLimit?.GetHashCode() ?? 0) *  3;
            }

        }

        #endregion


        #region Documentation

        // FRBC.UsageForecastElement.schema.json
        //   "title": "FRBC_UsageForecastElement",
        //   "properties": {
        //     "duration":               { "$ref": "../schemas/Duration.schema.json",
        //                                 "description": "Indicator for how long the given usage_rate is valid." },
        //     "usage_rate_upper_limit": { "type": "number",
        //                                 "description": "The upper limit of the range with a 100 % probability that the usage rate is
        //                                                 within that range. A positive value indicates that the fill level will decrease
        //                                                 due to usage." },
        //     "usage_rate_upper_95PPR": { "type": "number",
        //                                 "description": "The upper limit of the range with a 95 % probability that the usage rate is
        //                                                 within that range. A positive value indicates that the fill level will decrease
        //                                                 due to usage." },
        //     "usage_rate_upper_68PPR": { "type": "number",
        //                                 "description": "The upper limit of the range with a 68 % probability that the usage rate is
        //                                                 within that range. A positive value indicates that the fill level will decrease
        //                                                 due to usage." },
        //     "usage_rate_expected":    { "type": "number",
        //                                 "description": "The most likely value for the usage rate; the expected increase or decrease of
        //                                                 the fill_level per second. A positive value indicates that the fill level will
        //                                                 decrease due to usage." },
        //     "usage_rate_lower_68PPR": { "type": "number",
        //                                 "description": "The lower limit of the range with a 68 % probability that the usage rate is
        //                                                 within that range. A positive value indicates that the fill level will decrease
        //                                                 due to usage." },
        //     "usage_rate_lower_95PPR": { "type": "number",
        //                                 "description": "The lower limit of the range with a 95 % probability that the usage rate is
        //                                                 within that range. A positive value indicates that the fill level will decrease
        //                                                 due to usage." },
        //     "usage_rate_lower_limit": { "type": "number",
        //                                 "description": "The lower limit of the range with a 100 % probability that the usage rate is
        //                                                 within that range. A positive value indicates that the fill level will decrease
        //                                                 due to usage." }
        //   },
        //   "required": ["duration", "usage_rate_expected"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out UsageForecastElement, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a usage forecast element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="UsageForecastElement">The parsed usage forecast element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out FRBC_UsageForecastElement?  UsageForecastElement,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)

            => TryParse(JSON,
                        out UsageForecastElement,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a usage forecast element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="UsageForecastElement">The parsed usage forecast element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out FRBC_UsageForecastElement?  UsageForecastElement,
                                       [NotNullWhen(false)] out String?                     ErrorResponse,
                                       S2ParserOptions?                                     Options)

            => TryParse(JSON,
                        out UsageForecastElement,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a usage forecast element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="UsageForecastElement">The parsed usage forecast element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomUsageForecastElementParser">A delegate to parse custom usage forecast elements.</param>
        public static Boolean TryParse(JObject                                                  JSON,
                                       [NotNullWhen(true)]  out FRBC_UsageForecastElement?      UsageForecastElement,
                                       [NotNullWhen(false)] out String?                         ErrorResponse,
                                       S2ParserOptions?                                         Options,
                                       CustomJObjectParserDelegate<FRBC_UsageForecastElement>?  CustomUsageForecastElementParser)
        {

            try
            {

                UsageForecastElement = null;

                #region duration                  [mandatory]

                if (!JSON.ParseMandatoryS2Duration("duration",
                                                   "duration",
                                                   out Duration duration,
                                                   out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region usage_rate_upper_limit    [optional]

                if (!JSON.ParseOptionalS2Number("usage_rate_upper_limit",
                                                "usage rate upper limit",
                                                out Double? usageRateUpperLimit,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region usage_rate_upper_95PPR    [optional]

                if (!JSON.ParseOptionalS2Number("usage_rate_upper_95PPR",
                                                "usage rate upper 95 PPR",
                                                out Double? usageRateUpper95PPR,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region usage_rate_upper_68PPR    [optional]

                if (!JSON.ParseOptionalS2Number("usage_rate_upper_68PPR",
                                                "usage rate upper 68 PPR",
                                                out Double? usageRateUpper68PPR,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region usage_rate_expected       [mandatory]

                if (!JSON.ParseMandatoryS2Number("usage_rate_expected",
                                                 "expected usage rate",
                                                 out Double usageRateExpected,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region usage_rate_lower_68PPR    [optional]

                if (!JSON.ParseOptionalS2Number("usage_rate_lower_68PPR",
                                                "usage rate lower 68 PPR",
                                                out Double? usageRateLower68PPR,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region usage_rate_lower_95PPR    [optional]

                if (!JSON.ParseOptionalS2Number("usage_rate_lower_95PPR",
                                                "usage rate lower 95 PPR",
                                                out Double? usageRateLower95PPR,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region usage_rate_lower_limit    [optional]

                if (!JSON.ParseOptionalS2Number("usage_rate_lower_limit",
                                                "usage rate lower limit",
                                                out Double? usageRateLowerLimit,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "duration",
                                                    "usage_rate_upper_limit",
                                                    "usage_rate_upper_95PPR",
                                                    "usage_rate_upper_68PPR",
                                                    "usage_rate_expected",
                                                    "usage_rate_lower_68PPR",
                                                    "usage_rate_lower_95PPR",
                                                    "usage_rate_lower_limit"))
                {
                    return false;
                }

                #endregion


                UsageForecastElement = new FRBC_UsageForecastElement(
                                           duration,
                                           usageRateExpected,
                                           usageRateUpperLimit,
                                           usageRateUpper95PPR,
                                           usageRateUpper68PPR,
                                           usageRateLower68PPR,
                                           usageRateLower95PPR,
                                           usageRateLowerLimit
                                       );

                if (CustomUsageForecastElementParser is not null)
                    UsageForecastElement = CustomUsageForecastElementParser(JSON,
                                                                            UsageForecastElement);

                return true;

            }
            catch (Exception e)
            {
                UsageForecastElement  = null;
                ErrorResponse         = "The given JSON representation of a usage forecast element is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomUsageForecastElementSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomUsageForecastElementSerializer">A delegate to serialize custom usage forecast elements.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_UsageForecastElement>? CustomUsageForecastElementSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("duration",                Duration.ToJSON()),

                           UsageRateUpperLimit.HasValue
                               ? new JProperty("usage_rate_upper_limit",  UsageRateUpperLimit.Value)
                               : null,

                           UsageRateUpper95PPR.HasValue
                               ? new JProperty("usage_rate_upper_95PPR",  UsageRateUpper95PPR.Value)
                               : null,

                           UsageRateUpper68PPR.HasValue
                               ? new JProperty("usage_rate_upper_68PPR",  UsageRateUpper68PPR.Value)
                               : null,

                                 new JProperty("usage_rate_expected",     UsageRateExpected),

                           UsageRateLower68PPR.HasValue
                               ? new JProperty("usage_rate_lower_68PPR",  UsageRateLower68PPR.Value)
                               : null,

                           UsageRateLower95PPR.HasValue
                               ? new JProperty("usage_rate_lower_95PPR",  UsageRateLower95PPR.Value)
                               : null,

                           UsageRateLowerLimit.HasValue
                               ? new JProperty("usage_rate_lower_limit",  UsageRateLowerLimit.Value)
                               : null

                       );

            return CustomUsageForecastElementSerializer is not null
                       ? CustomUsageForecastElementSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this usage forecast element.
        /// </summary>
        public FRBC_UsageForecastElement Clone()

            => new (
                   Duration,
                   UsageRateExpected,
                   UsageRateUpperLimit,
                   UsageRateUpper95PPR,
                   UsageRateUpper68PPR,
                   UsageRateLower68PPR,
                   UsageRateLower95PPR,
                   UsageRateLowerLimit
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two usage forecast elements for equality.
        /// </summary>
        public static Boolean operator == (FRBC_UsageForecastElement? UsageForecastElement1,
                                           FRBC_UsageForecastElement? UsageForecastElement2)
        {

            if (ReferenceEquals(UsageForecastElement1, UsageForecastElement2))
                return true;

            if (UsageForecastElement1 is null || UsageForecastElement2 is null)
                return false;

            return UsageForecastElement1.Equals(UsageForecastElement2);

        }

        /// <summary>
        /// Compares two usage forecast elements for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_UsageForecastElement? UsageForecastElement1,
                                           FRBC_UsageForecastElement? UsageForecastElement2)
            => !(UsageForecastElement1 == UsageForecastElement2);

        #endregion

        #region IEquatable<FRBC_UsageForecastElement> Members

        /// <summary>
        /// Compares two usage forecast elements for equality.
        /// </summary>
        /// <param name="Object">A usage forecast element to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_UsageForecastElement usageForecastElement && Equals(usageForecastElement);

        /// <summary>
        /// Compares two usage forecast elements for equality.
        /// </summary>
        /// <param name="UsageForecastElement">A usage forecast element to compare with.</param>
        public Boolean Equals(FRBC_UsageForecastElement? UsageForecastElement)

            => UsageForecastElement is not null &&

               Duration.         Equals(UsageForecastElement.Duration)          &&
               UsageRateExpected.Equals(UsageForecastElement.UsageRateExpected) &&

               Nullable.Equals(UsageRateUpperLimit, UsageForecastElement.UsageRateUpperLimit) &&
               Nullable.Equals(UsageRateUpper95PPR, UsageForecastElement.UsageRateUpper95PPR) &&
               Nullable.Equals(UsageRateUpper68PPR, UsageForecastElement.UsageRateUpper68PPR) &&
               Nullable.Equals(UsageRateLower68PPR, UsageForecastElement.UsageRateLower68PPR) &&
               Nullable.Equals(UsageRateLower95PPR, UsageForecastElement.UsageRateLower95PPR) &&
               Nullable.Equals(UsageRateLowerLimit, UsageForecastElement.UsageRateLowerLimit);

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

                   $"usage rate {UsageRateExpected}/s",

                   UsageRateLowerLimit.HasValue || UsageRateUpperLimit.HasValue
                       ? $" [{UsageRateLowerLimit?.ToString() ?? "?"} .. {UsageRateUpperLimit?.ToString() ?? "?"}]"
                       : "",

                   $" for {Duration}"

               );

        #endregion

    }

}

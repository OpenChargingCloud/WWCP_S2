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
    /// An element of a power envelope (Power Envelope Based Control): for the given duration
    /// the Resource Manager is requested to keep the power values for the commodity quantity
    /// of the containing power envelope between the lower and the upper limit.
    /// </summary>
    public sealed class PEBC_PowerEnvelopeElement : IEquatable<PEBC_PowerEnvelopeElement>
    {

        #region Properties

        /// <summary>
        /// The duration of the element.
        /// </summary>
        [Mandatory]
        public Duration  Duration      { get; }

        /// <summary>
        /// The upper power limit according to the commodity quantity of the containing power envelope.
        /// The lower limit must be smaller or equal to the upper limit. The Resource Manager is requested
        /// to keep the power values for the given commodity quantity equal to or below the upper limit.
        /// The upper limit shall be in accordance with the constraints provided by the Resource Manager
        /// through any allowed limit range with limit type UPPER_LIMIT.
        /// </summary>
        [Mandatory]
        public Double    UpperLimit    { get; }

        /// <summary>
        /// The lower power limit according to the commodity quantity of the containing power envelope.
        /// The lower limit must be smaller or equal to the upper limit. The Resource Manager is requested
        /// to keep the power values for the given commodity quantity equal to or above the lower limit.
        /// The lower limit shall be in accordance with the constraints provided by the Resource Manager
        /// through any allowed limit range with limit type LOWER_LIMIT.
        /// </summary>
        [Mandatory]
        public Double    LowerLimit    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power envelope element.
        /// </summary>
        /// <param name="Duration">The duration of the element.</param>
        /// <param name="UpperLimit">The upper power limit (not smaller than the lower limit).</param>
        /// <param name="LowerLimit">The lower power limit (not greater than the upper limit).</param>
        public PEBC_PowerEnvelopeElement(Duration  Duration,
                                         Double    UpperLimit,
                                         Double    LowerLimit)
        {

            if (Double.IsNaN(UpperLimit) || Double.IsNaN(LowerLimit))
                throw new ArgumentException("The limits of a power envelope element must not be NaN!");

            if (LowerLimit > UpperLimit)
                throw new ArgumentException($"The lower limit ({LowerLimit}) must be smaller or equal to the upper limit ({UpperLimit})!",
                                            nameof(LowerLimit));

            this.Duration    = Duration;
            this.UpperLimit  = UpperLimit;
            this.LowerLimit  = LowerLimit;

            unchecked
            {
                hashCode = this.Duration.  GetHashCode() * 5 ^
                           this.UpperLimit.GetHashCode() * 3 ^
                           this.LowerLimit.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // PEBC.PowerEnvelopeElement.schema.json
        //   "title": "PEBC_PowerEnvelopeElement",
        //   "properties": {
        //     "duration":    { "$ref": "../schemas/Duration.schema.json",
        //                      "description": "The duration of the element" },
        //     "upper_limit": { "type": "number",
        //                      "description": "Upper power limit according to the commodity_quantity of the containing
        //                                      PEBC.PowerEnvelope. The lower_limit must be smaller or equal to the upper_limit.
        //                                      The Resource Manager is requested to keep the power values for the given commodity
        //                                      quantity equal to or below the upper_limit. The upper_limit shall be in accordance
        //                                      with the constraints provided by the Resource Manager through any
        //                                      PEBC.AllowedLimitRange with limit_type UPPER_LIMIT." },
        //     "lower_limit": { "type": "number",
        //                      "description": "Lower power limit according to the commodity_quantity of the containing
        //                                      PEBC.PowerEnvelope. The lower_limit must be smaller or equal to the upper_limit.
        //                                      The Resource Manager is requested to keep the power values for the given commodity
        //                                      quantity equal to or above the lower_limit. The lower_limit shall be in accordance
        //                                      with the constraints provided by the Resource Manager through any
        //                                      PEBC.AllowedLimitRange with limit_type LOWER_LIMIT." }
        //   },
        //   "required": ["duration", "upper_limit", "lower_limit"],
        //   "additionalProperties": false
        //
        // Semantic rule (PLAN.md §3.3, CONVENTIONS.md §5): lower_limit <= upper_limit.
        // Cross-object rule (session layer): the limits shall be within the PEBC.AllowedLimitRanges of the
        // PEBC.PowerConstraints with the matching limit_type.

        #endregion

        #region (static) TryParse(JSON, out PEBC_PowerEnvelopeElement, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a power envelope element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerEnvelopeElement">The parsed power envelope element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out PEBC_PowerEnvelopeElement?  PowerEnvelopeElement,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)

            => TryParse(JSON,
                        out PowerEnvelopeElement,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power envelope element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerEnvelopeElement">The parsed power envelope element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out PEBC_PowerEnvelopeElement?  PowerEnvelopeElement,
                                       [NotNullWhen(false)] out String?                     ErrorResponse,
                                       S2ParserOptions?                                     Options)

            => TryParse(JSON,
                        out PowerEnvelopeElement,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power envelope element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerEnvelopeElement">The parsed power envelope element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPowerEnvelopeElementParser">A delegate to parse custom power envelope elements.</param>
        public static Boolean TryParse(JObject                                                  JSON,
                                       [NotNullWhen(true)]  out PEBC_PowerEnvelopeElement?      PowerEnvelopeElement,
                                       [NotNullWhen(false)] out String?                         ErrorResponse,
                                       S2ParserOptions?                                         Options,
                                       CustomJObjectParserDelegate<PEBC_PowerEnvelopeElement>?  CustomPowerEnvelopeElementParser)
        {

            try
            {

                PowerEnvelopeElement = null;

                #region duration       [mandatory]

                if (!JSON.ParseMandatoryS2Duration("duration",
                                                   "duration",
                                                   out Duration duration,
                                                   out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region upper_limit    [mandatory]

                if (!JSON.ParseMandatoryS2Number("upper_limit",
                                                 "upper limit",
                                                 out Double upperLimit,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region lower_limit    [mandatory]

                if (!JSON.ParseMandatoryS2Number("lower_limit",
                                                 "lower limit",
                                                 out Double lowerLimit,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "duration",
                                                    "upper_limit",
                                                    "lower_limit"))
                {
                    return false;
                }

                #endregion


                PowerEnvelopeElement = new PEBC_PowerEnvelopeElement(
                                           duration,
                                           upperLimit,
                                           lowerLimit
                                       );

                if (CustomPowerEnvelopeElementParser is not null)
                    PowerEnvelopeElement = CustomPowerEnvelopeElementParser(JSON,
                                                                            PowerEnvelopeElement);

                return true;

            }
            catch (Exception e)
            {
                PowerEnvelopeElement  = null;
                ErrorResponse         = "The given JSON representation of a power envelope element is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerEnvelopeElementSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPowerEnvelopeElementSerializer">A delegate to serialize custom power envelope elements.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PEBC_PowerEnvelopeElement>? CustomPowerEnvelopeElementSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("duration",     Duration.ToJSON()),
                           new JProperty("upper_limit",  UpperLimit),
                           new JProperty("lower_limit",  LowerLimit)
                       );

            return CustomPowerEnvelopeElementSerializer is not null
                       ? CustomPowerEnvelopeElementSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power envelope element.
        /// </summary>
        public PEBC_PowerEnvelopeElement Clone()

            => new (
                   Duration,
                   UpperLimit,
                   LowerLimit
               );

        #endregion


        #region Contains(Value)

        /// <summary>
        /// Whether the given power value lies within the limits of this element (both limits inclusive).
        /// </summary>
        /// <param name="Value">A power value.</param>
        public Boolean Contains(Double Value)
            => Value >= LowerLimit && Value <= UpperLimit;

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power envelope elements for equality.
        /// </summary>
        public static Boolean operator == (PEBC_PowerEnvelopeElement? PowerEnvelopeElement1, PEBC_PowerEnvelopeElement? PowerEnvelopeElement2)
        {

            if (ReferenceEquals(PowerEnvelopeElement1, PowerEnvelopeElement2))
                return true;

            if (PowerEnvelopeElement1 is null || PowerEnvelopeElement2 is null)
                return false;

            return PowerEnvelopeElement1.Equals(PowerEnvelopeElement2);

        }

        /// <summary>
        /// Compares two power envelope elements for inequality.
        /// </summary>
        public static Boolean operator != (PEBC_PowerEnvelopeElement? PowerEnvelopeElement1, PEBC_PowerEnvelopeElement? PowerEnvelopeElement2)
            => !(PowerEnvelopeElement1 == PowerEnvelopeElement2);

        #endregion

        #region IEquatable<PEBC_PowerEnvelopeElement> Members

        /// <summary>
        /// Compares two power envelope elements for equality.
        /// </summary>
        /// <param name="Object">A power envelope element to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PEBC_PowerEnvelopeElement powerEnvelopeElement && Equals(powerEnvelopeElement);

        /// <summary>
        /// Compares two power envelope elements for equality.
        /// </summary>
        /// <param name="PowerEnvelopeElement">A power envelope element to compare with.</param>
        public Boolean Equals(PEBC_PowerEnvelopeElement? PowerEnvelopeElement)

            => PowerEnvelopeElement is not null &&

               Duration.  Equals(PowerEnvelopeElement.Duration)   &&
               UpperLimit.Equals(PowerEnvelopeElement.UpperLimit) &&
               LowerLimit.Equals(PowerEnvelopeElement.LowerLimit);

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
            => $"{LowerLimit} .. {UpperLimit} for {Duration}";

        #endregion

    }

}

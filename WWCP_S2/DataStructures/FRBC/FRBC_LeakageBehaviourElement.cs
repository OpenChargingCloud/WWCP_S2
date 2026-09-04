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
    /// An element of a FRBC.LeakageBehaviour: the leakage rate of the storage within
    /// a given fill level range.
    /// </summary>
    public sealed class FRBC_LeakageBehaviourElement : IEquatable<FRBC_LeakageBehaviourElement>
    {

        #region Properties

        /// <summary>
        /// The fill level range for which this leakage behaviour element applies.
        /// The start of the range must be less than the end of the range.
        /// </summary>
        [Mandatory]
        public NumberRange  FillLevelRange    { get; }

        /// <summary>
        /// Indicates how fast the momentary fill level will decrease per second due to leakage
        /// within the given range of the fill level. A positive value indicates that the fill
        /// level decreases over time due to leakage.
        /// </summary>
        [Mandatory]
        public Double       LeakageRate       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new leakage behaviour element.
        /// </summary>
        /// <param name="FillLevelRange">The fill level range for which this element applies (start strictly less than end).</param>
        /// <param name="LeakageRate">How fast the momentary fill level decreases per second due to leakage within the given range.</param>
        public FRBC_LeakageBehaviourElement(NumberRange  FillLevelRange,
                                            Double       LeakageRate)
        {

            ArgumentNullException.ThrowIfNull(FillLevelRange);

            if (FillLevelRange.StartOfRange >= FillLevelRange.EndOfRange)
                throw new ArgumentException($"The start of the fill level range ({FillLevelRange.StartOfRange}) must be less than its end ({FillLevelRange.EndOfRange})!",
                                            nameof(FillLevelRange));

            if (Double.IsNaN(LeakageRate))
                throw new ArgumentException("The leakage rate must not be NaN!",
                                            nameof(LeakageRate));

            this.FillLevelRange  = FillLevelRange;
            this.LeakageRate     = LeakageRate;

            unchecked
            {
                hashCode = this.FillLevelRange.GetHashCode() * 3 ^
                           this.LeakageRate.   GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // FRBC.LeakageBehaviourElement.schema.json
        //   "title": "FRBC_LeakageBehaviourElement",
        //   "properties": {
        //     "fill_level_range": { "$ref": "../schemas/NumberRange.schema.json",
        //                           "description": "The fill level range for which this FRBC.LeakageBehaviourElement applies.
        //                                           The start of the range must be less than the end of the range." },
        //     "leakage_rate":     { "type": "number",
        //                           "description": "Indicates how fast the momentary fill level will decrease per second due to
        //                                           leakage within the given range of the fill level. A positive value indicates
        //                                           that the fill level decreases over time due to leakage." }
        //   },
        //   "required": ["fill_level_range", "leakage_rate"],
        //   "additionalProperties": false
        //
        // Semantic rule (CONVENTIONS.md §5): fill_level_range.start_of_range < fill_level_range.end_of_range (strict).

        #endregion

        #region (static) TryParse(JSON, out LeakageBehaviourElement, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a leakage behaviour element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="LeakageBehaviourElement">The parsed leakage behaviour element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                 JSON,
                                       [NotNullWhen(true)]  out FRBC_LeakageBehaviourElement?  LeakageBehaviourElement,
                                       [NotNullWhen(false)] out String?                        ErrorResponse)

            => TryParse(JSON,
                        out LeakageBehaviourElement,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a leakage behaviour element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="LeakageBehaviourElement">The parsed leakage behaviour element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                 JSON,
                                       [NotNullWhen(true)]  out FRBC_LeakageBehaviourElement?  LeakageBehaviourElement,
                                       [NotNullWhen(false)] out String?                        ErrorResponse,
                                       S2ParserOptions?                                        Options)

            => TryParse(JSON,
                        out LeakageBehaviourElement,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a leakage behaviour element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="LeakageBehaviourElement">The parsed leakage behaviour element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomLeakageBehaviourElementParser">A delegate to parse custom leakage behaviour elements.</param>
        public static Boolean TryParse(JObject                                                     JSON,
                                       [NotNullWhen(true)]  out FRBC_LeakageBehaviourElement?      LeakageBehaviourElement,
                                       [NotNullWhen(false)] out String?                            ErrorResponse,
                                       S2ParserOptions?                                            Options,
                                       CustomJObjectParserDelegate<FRBC_LeakageBehaviourElement>?  CustomLeakageBehaviourElementParser)
        {

            try
            {

                LeakageBehaviourElement = null;

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

                #region leakage_rate        [mandatory]

                if (!JSON.ParseMandatoryS2Number("leakage_rate",
                                                 "leakage rate",
                                                 out Double leakageRate,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "fill_level_range",
                                                    "leakage_rate"))
                {
                    return false;
                }

                #endregion


                LeakageBehaviourElement = new FRBC_LeakageBehaviourElement(
                                              fillLevelRange,
                                              leakageRate
                                          );

                if (CustomLeakageBehaviourElementParser is not null)
                    LeakageBehaviourElement = CustomLeakageBehaviourElementParser(JSON,
                                                                                  LeakageBehaviourElement);

                return true;

            }
            catch (Exception e)
            {
                LeakageBehaviourElement  = null;
                ErrorResponse            = "The given JSON representation of a leakage behaviour element is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomLeakageBehaviourElementSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomLeakageBehaviourElementSerializer">A delegate to serialize custom leakage behaviour elements.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_LeakageBehaviourElement>? CustomLeakageBehaviourElementSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("fill_level_range",  FillLevelRange.ToJSON()),
                           new JProperty("leakage_rate",      LeakageRate)
                       );

            return CustomLeakageBehaviourElementSerializer is not null
                       ? CustomLeakageBehaviourElementSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this leakage behaviour element.
        /// </summary>
        public FRBC_LeakageBehaviourElement Clone()

            => new (
                   FillLevelRange.Clone(),
                   LeakageRate
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two leakage behaviour elements for equality.
        /// </summary>
        public static Boolean operator == (FRBC_LeakageBehaviourElement? LeakageBehaviourElement1,
                                           FRBC_LeakageBehaviourElement? LeakageBehaviourElement2)
        {

            if (ReferenceEquals(LeakageBehaviourElement1, LeakageBehaviourElement2))
                return true;

            if (LeakageBehaviourElement1 is null || LeakageBehaviourElement2 is null)
                return false;

            return LeakageBehaviourElement1.Equals(LeakageBehaviourElement2);

        }

        /// <summary>
        /// Compares two leakage behaviour elements for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_LeakageBehaviourElement? LeakageBehaviourElement1,
                                           FRBC_LeakageBehaviourElement? LeakageBehaviourElement2)
            => !(LeakageBehaviourElement1 == LeakageBehaviourElement2);

        #endregion

        #region IEquatable<FRBC_LeakageBehaviourElement> Members

        /// <summary>
        /// Compares two leakage behaviour elements for equality.
        /// </summary>
        /// <param name="Object">A leakage behaviour element to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_LeakageBehaviourElement leakageBehaviourElement && Equals(leakageBehaviourElement);

        /// <summary>
        /// Compares two leakage behaviour elements for equality.
        /// </summary>
        /// <param name="LeakageBehaviourElement">A leakage behaviour element to compare with.</param>
        public Boolean Equals(FRBC_LeakageBehaviourElement? LeakageBehaviourElement)

            => LeakageBehaviourElement is not null &&

               FillLevelRange.Equals(LeakageBehaviourElement.FillLevelRange) &&
               LeakageRate.   Equals(LeakageBehaviourElement.LeakageRate);

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
            => $"fill level {FillLevelRange}: leakage rate {LeakageRate}/s";

        #endregion

    }

}

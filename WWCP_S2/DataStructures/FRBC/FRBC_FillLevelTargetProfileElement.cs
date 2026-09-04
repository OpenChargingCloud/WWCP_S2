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
    /// An element of a FRBC.FillLevelTargetProfile: the fill level range the storage
    /// should be in for the given duration.
    /// </summary>
    public sealed class FRBC_FillLevelTargetProfileElement : IEquatable<FRBC_FillLevelTargetProfileElement>
    {

        #region Properties

        /// <summary>
        /// The duration of the element.
        /// </summary>
        [Mandatory]
        public Duration     Duration          { get; }

        /// <summary>
        /// The target range in which the fill level must be for the time period during which
        /// the element is active. The start of the range must be smaller or equal to the end
        /// of the range. The CEM must take best-effort actions to proactively achieve this target.
        /// </summary>
        [Mandatory]
        public NumberRange  FillLevelRange    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new fill level target profile element.
        /// </summary>
        /// <param name="Duration">The duration of the element.</param>
        /// <param name="FillLevelRange">The target range in which the fill level must be while the element is active.</param>
        public FRBC_FillLevelTargetProfileElement(Duration     Duration,
                                                  NumberRange  FillLevelRange)
        {

            ArgumentNullException.ThrowIfNull(FillLevelRange);

            this.Duration        = Duration;
            this.FillLevelRange  = FillLevelRange;

            unchecked
            {
                hashCode = this.Duration.      GetHashCode() * 3 ^
                           this.FillLevelRange.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // FRBC.FillLevelTargetProfileElement.schema.json
        //   "title": "FRBC_FillLevelTargetProfileElement",
        //   "properties": {
        //     "duration":         { "$ref": "../schemas/Duration.schema.json",
        //                           "description": "The duration of the element." },
        //     "fill_level_range": { "$ref": "../schemas/NumberRange.schema.json",
        //                           "description": "The target range in which the fill_level must be for the time period during
        //                                           which the element is active. The start of the range must be smaller or equal
        //                                           to the end of the range. The CEM must take best-effort actions to proactively
        //                                           achieve this target." }
        //   },
        //   "required": ["duration", "fill_level_range"],
        //   "additionalProperties": false
        //
        // Semantic rule: fill_level_range.start_of_range <= fill_level_range.end_of_range (validated by NumberRange).

        #endregion

        #region (static) TryParse(JSON, out FillLevelTargetProfileElement, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a fill level target profile element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FillLevelTargetProfileElement">The parsed fill level target profile element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                       JSON,
                                       [NotNullWhen(true)]  out FRBC_FillLevelTargetProfileElement?  FillLevelTargetProfileElement,
                                       [NotNullWhen(false)] out String?                              ErrorResponse)

            => TryParse(JSON,
                        out FillLevelTargetProfileElement,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a fill level target profile element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FillLevelTargetProfileElement">The parsed fill level target profile element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                       JSON,
                                       [NotNullWhen(true)]  out FRBC_FillLevelTargetProfileElement?  FillLevelTargetProfileElement,
                                       [NotNullWhen(false)] out String?                              ErrorResponse,
                                       S2ParserOptions?                                              Options)

            => TryParse(JSON,
                        out FillLevelTargetProfileElement,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a fill level target profile element.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FillLevelTargetProfileElement">The parsed fill level target profile element.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomFillLevelTargetProfileElementParser">A delegate to parse custom fill level target profile elements.</param>
        public static Boolean TryParse(JObject                                                           JSON,
                                       [NotNullWhen(true)]  out FRBC_FillLevelTargetProfileElement?      FillLevelTargetProfileElement,
                                       [NotNullWhen(false)] out String?                                  ErrorResponse,
                                       S2ParserOptions?                                                  Options,
                                       CustomJObjectParserDelegate<FRBC_FillLevelTargetProfileElement>?  CustomFillLevelTargetProfileElementParser)
        {

            try
            {

                FillLevelTargetProfileElement = null;

                #region duration            [mandatory]

                if (!JSON.ParseMandatoryS2Duration("duration",
                                                   "duration",
                                                   out Duration duration,
                                                   out ErrorResponse))
                {
                    return false;
                }

                #endregion

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

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "duration",
                                                    "fill_level_range"))
                {
                    return false;
                }

                #endregion


                FillLevelTargetProfileElement = new FRBC_FillLevelTargetProfileElement(
                                                    duration,
                                                    fillLevelRange
                                                );

                if (CustomFillLevelTargetProfileElementParser is not null)
                    FillLevelTargetProfileElement = CustomFillLevelTargetProfileElementParser(JSON,
                                                                                              FillLevelTargetProfileElement);

                return true;

            }
            catch (Exception e)
            {
                FillLevelTargetProfileElement  = null;
                ErrorResponse                  = "The given JSON representation of a fill level target profile element is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomFillLevelTargetProfileElementSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomFillLevelTargetProfileElementSerializer">A delegate to serialize custom fill level target profile elements.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_FillLevelTargetProfileElement>? CustomFillLevelTargetProfileElementSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("duration",          Duration.      ToJSON()),
                           new JProperty("fill_level_range",  FillLevelRange.ToJSON())
                       );

            return CustomFillLevelTargetProfileElementSerializer is not null
                       ? CustomFillLevelTargetProfileElementSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this fill level target profile element.
        /// </summary>
        public FRBC_FillLevelTargetProfileElement Clone()

            => new (
                   Duration,
                   FillLevelRange.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two fill level target profile elements for equality.
        /// </summary>
        public static Boolean operator == (FRBC_FillLevelTargetProfileElement? FillLevelTargetProfileElement1,
                                           FRBC_FillLevelTargetProfileElement? FillLevelTargetProfileElement2)
        {

            if (ReferenceEquals(FillLevelTargetProfileElement1, FillLevelTargetProfileElement2))
                return true;

            if (FillLevelTargetProfileElement1 is null || FillLevelTargetProfileElement2 is null)
                return false;

            return FillLevelTargetProfileElement1.Equals(FillLevelTargetProfileElement2);

        }

        /// <summary>
        /// Compares two fill level target profile elements for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_FillLevelTargetProfileElement? FillLevelTargetProfileElement1,
                                           FRBC_FillLevelTargetProfileElement? FillLevelTargetProfileElement2)
            => !(FillLevelTargetProfileElement1 == FillLevelTargetProfileElement2);

        #endregion

        #region IEquatable<FRBC_FillLevelTargetProfileElement> Members

        /// <summary>
        /// Compares two fill level target profile elements for equality.
        /// </summary>
        /// <param name="Object">A fill level target profile element to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_FillLevelTargetProfileElement fillLevelTargetProfileElement && Equals(fillLevelTargetProfileElement);

        /// <summary>
        /// Compares two fill level target profile elements for equality.
        /// </summary>
        /// <param name="FillLevelTargetProfileElement">A fill level target profile element to compare with.</param>
        public Boolean Equals(FRBC_FillLevelTargetProfileElement? FillLevelTargetProfileElement)

            => FillLevelTargetProfileElement is not null &&

               Duration.      Equals(FillLevelTargetProfileElement.Duration) &&
               FillLevelRange.Equals(FillLevelTargetProfileElement.FillLevelRange);

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
            => $"fill level {FillLevelRange} for {Duration}";

        #endregion

    }

}

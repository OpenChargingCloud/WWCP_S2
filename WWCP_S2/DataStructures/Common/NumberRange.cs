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
    /// A range of numbers, e.g. a fill level range or a fill rate range.
    /// The start of the range must not be greater than its end.
    /// </summary>
    public sealed class NumberRange : IEquatable<NumberRange>
    {

        #region Properties

        /// <summary>
        /// Number that defines the start of the range.
        /// </summary>
        [Mandatory]
        public Double  StartOfRange    { get; }

        /// <summary>
        /// Number that defines the end of the range.
        /// </summary>
        [Mandatory]
        public Double  EndOfRange      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new number range.
        /// </summary>
        /// <param name="StartOfRange">Number that defines the start of the range.</param>
        /// <param name="EndOfRange">Number that defines the end of the range (not smaller than the start).</param>
        public NumberRange(Double  StartOfRange,
                           Double  EndOfRange)
        {

            if (!Double.IsFinite(StartOfRange) || !Double.IsFinite(EndOfRange))
                throw new ArgumentException("A number range must consist of finite numbers (no NaN or infinity)!");

            if (StartOfRange > EndOfRange)
                throw new ArgumentException($"The start of the range ({StartOfRange}) must not be greater than its end ({EndOfRange})!",
                                            nameof(StartOfRange));

            this.StartOfRange  = StartOfRange;
            this.EndOfRange    = EndOfRange;

            unchecked
            {
                hashCode = this.StartOfRange.GetHashCode() * 3 ^
                           this.EndOfRange.  GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // NumberRange.schema.json
        //   "title": "NumberRange",
        //   "properties": {
        //     "start_of_range": { "type": "number", "description": "Number that defines the start of the range" },
        //     "end_of_range":   { "type": "number", "description": "Number that defines the end of the range" }
        //   },
        //   "required": ["start_of_range", "end_of_range"],
        //   "additionalProperties": false
        //
        // Semantic rule (PLAN.md §3.3, s2-python validate_start_end_order): start_of_range <= end_of_range.

        #endregion

        #region (static) TryParse(JSON, out NumberRange, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a number range.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="NumberRange">The parsed number range.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                JSON,
                                       [NotNullWhen(true)]  out NumberRange?  NumberRange,
                                       [NotNullWhen(false)] out String?       ErrorResponse)

            => TryParse(JSON,
                        out NumberRange,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a number range.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="NumberRange">The parsed number range.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                JSON,
                                       [NotNullWhen(true)]  out NumberRange?  NumberRange,
                                       [NotNullWhen(false)] out String?       ErrorResponse,
                                       S2ParserOptions?                       Options)

            => TryParse(JSON,
                        out NumberRange,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a number range.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="NumberRange">The parsed number range.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomNumberRangeParser">A delegate to parse custom number ranges.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out NumberRange?      NumberRange,
                                       [NotNullWhen(false)] out String?           ErrorResponse,
                                       S2ParserOptions?                           Options,
                                       CustomJObjectParserDelegate<NumberRange>?  CustomNumberRangeParser)
        {

            try
            {

                NumberRange = null;

                #region start_of_range    [mandatory]

                if (!JSON.ParseMandatoryS2Number("start_of_range",
                                                 "start of range",
                                                 out Double startOfRange,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region end_of_range      [mandatory]

                if (!JSON.ParseMandatoryS2Number("end_of_range",
                                                 "end of range",
                                                 out Double endOfRange,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "start_of_range",
                                                    "end_of_range"))
                {
                    return false;
                }

                #endregion


                NumberRange = new NumberRange(
                                  startOfRange,
                                  endOfRange
                              );

                if (CustomNumberRangeParser is not null)
                    NumberRange = CustomNumberRangeParser(JSON,
                                                          NumberRange);

                return true;

            }
            catch (Exception e)
            {
                NumberRange    = null;
                ErrorResponse  = "The given JSON representation of a number range is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomNumberRangeSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomNumberRangeSerializer">A delegate to serialize custom number ranges.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<NumberRange>? CustomNumberRangeSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("start_of_range",  StartOfRange),
                           new JProperty("end_of_range",    EndOfRange)
                       );

            return CustomNumberRangeSerializer is not null
                       ? CustomNumberRangeSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this number range.
        /// </summary>
        public NumberRange Clone()

            => new (
                   StartOfRange,
                   EndOfRange
               );

        #endregion


        #region Contains(Value)

        /// <summary>
        /// Whether the given value lies within this range (both boundaries inclusive).
        /// </summary>
        /// <param name="Value">A value.</param>
        public Boolean Contains(Double Value)
            => Value >= StartOfRange && Value <= EndOfRange;

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two number ranges for equality.
        /// </summary>
        public static Boolean operator == (NumberRange? NumberRange1, NumberRange? NumberRange2)
        {

            if (ReferenceEquals(NumberRange1, NumberRange2))
                return true;

            if (NumberRange1 is null || NumberRange2 is null)
                return false;

            return NumberRange1.Equals(NumberRange2);

        }

        /// <summary>
        /// Compares two number ranges for inequality.
        /// </summary>
        public static Boolean operator != (NumberRange? NumberRange1, NumberRange? NumberRange2)
            => !(NumberRange1 == NumberRange2);

        #endregion

        #region IEquatable<NumberRange> Members

        /// <summary>
        /// Compares two number ranges for equality.
        /// </summary>
        /// <param name="Object">A number range to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is NumberRange numberRange && Equals(numberRange);

        /// <summary>
        /// Compares two number ranges for equality.
        /// </summary>
        /// <param name="NumberRange">A number range to compare with.</param>
        public Boolean Equals(NumberRange? NumberRange)

            => NumberRange is not null &&
               StartOfRange.Equals(NumberRange.StartOfRange) &&
               EndOfRange.  Equals(NumberRange.EndOfRange);

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
            => $"{StartOfRange} .. {EndOfRange}";

        #endregion

    }

}

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
    /// A range of power values for a commodity quantity, e.g. 1400 .. 11000 W of
    /// three-phase symmetric electric power. The start of the range must not be
    /// greater than its end.
    /// </summary>
    public sealed class PowerRange : IEquatable<PowerRange>
    {

        #region Properties

        /// <summary>
        /// Power value that defines the start of the range.
        /// </summary>
        [Mandatory]
        public Double             StartOfRange         { get; }

        /// <summary>
        /// Power value that defines the end of the range.
        /// </summary>
        [Mandatory]
        public Double             EndOfRange           { get; }

        /// <summary>
        /// The power quantity the values refer to.
        /// </summary>
        [Mandatory]
        public CommodityQuantity  CommodityQuantity    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power range.
        /// </summary>
        /// <param name="StartOfRange">Power value that defines the start of the range.</param>
        /// <param name="EndOfRange">Power value that defines the end of the range (not smaller than the start).</param>
        /// <param name="CommodityQuantity">The power quantity the values refer to.</param>
        public PowerRange(Double             StartOfRange,
                          Double             EndOfRange,
                          CommodityQuantity  CommodityQuantity)
        {

            if (!Double.IsFinite(StartOfRange) || !Double.IsFinite(EndOfRange))
                throw new ArgumentException("A power range must consist of finite numbers!");

            if (StartOfRange > EndOfRange)
                throw new ArgumentException($"The start of the power range ({StartOfRange}) must not be greater than its end ({EndOfRange})!",
                                            nameof(StartOfRange));

            if (CommodityQuantity.IsNullOrEmpty)
                throw new ArgumentException("The commodity quantity of a power range must not be null or empty!",
                                            nameof(CommodityQuantity));

            this.StartOfRange       = StartOfRange;
            this.EndOfRange         = EndOfRange;
            this.CommodityQuantity  = CommodityQuantity;

            unchecked
            {
                hashCode = this.StartOfRange.     GetHashCode() * 5 ^
                           this.EndOfRange.       GetHashCode() * 3 ^
                           this.CommodityQuantity.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // PowerRange.schema.json
        //   "title": "PowerRange",
        //   "properties": {
        //     "start_of_range":     { "type": "number", "description": "Power value that defines the start of the range." },
        //     "end_of_range":       { "type": "number", "description": "Power value that defines the end of the range." },
        //     "commodity_quantity": { "$ref": "../schemas/CommodityQuantity.schema.json",
        //                             "description": "The power quantity the values refer to" }
        //   },
        //   "required": ["start_of_range", "end_of_range", "commodity_quantity"],
        //   "additionalProperties": false
        //
        // Semantic rule (CONVENTIONS.md §5, s2-python validate_start_end_order): start_of_range <= end_of_range.

        #endregion

        #region (static) TryParse(JSON, out PowerRange, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a power range.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerRange">The parsed power range.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                               JSON,
                                       [NotNullWhen(true)]  out PowerRange?  PowerRange,
                                       [NotNullWhen(false)] out String?      ErrorResponse)

            => TryParse(JSON,
                        out PowerRange,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power range.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerRange">The parsed power range.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                               JSON,
                                       [NotNullWhen(true)]  out PowerRange?  PowerRange,
                                       [NotNullWhen(false)] out String?      ErrorResponse,
                                       S2ParserOptions?                      Options)

            => TryParse(JSON,
                        out PowerRange,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power range.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerRange">The parsed power range.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPowerRangeParser">A delegate to parse custom power ranges.</param>
        public static Boolean TryParse(JObject                                   JSON,
                                       [NotNullWhen(true)]  out PowerRange?      PowerRange,
                                       [NotNullWhen(false)] out String?          ErrorResponse,
                                       S2ParserOptions?                          Options,
                                       CustomJObjectParserDelegate<PowerRange>?  CustomPowerRangeParser)
        {

            try
            {

                PowerRange = null;

                #region start_of_range        [mandatory]

                if (!JSON.ParseMandatoryS2Number("start_of_range",
                                                 "start of range",
                                                 out Double startOfRange,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region end_of_range          [mandatory]

                if (!JSON.ParseMandatoryS2Number("end_of_range",
                                                 "end of range",
                                                 out Double endOfRange,
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
                                                    "start_of_range",
                                                    "end_of_range",
                                                    "commodity_quantity"))
                {
                    return false;
                }

                #endregion


                PowerRange = new PowerRange(
                                 startOfRange,
                                 endOfRange,
                                 commodityQuantity
                             );

                if (CustomPowerRangeParser is not null)
                    PowerRange = CustomPowerRangeParser(JSON,
                                                        PowerRange);

                return true;

            }
            catch (Exception e)
            {
                PowerRange     = null;
                ErrorResponse  = "The given JSON representation of a power range is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerRangeSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPowerRangeSerializer">A delegate to serialize custom power ranges.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PowerRange>? CustomPowerRangeSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("start_of_range",      StartOfRange),
                           new JProperty("end_of_range",        EndOfRange),
                           new JProperty("commodity_quantity",  CommodityQuantity.ToString())
                       );

            return CustomPowerRangeSerializer is not null
                       ? CustomPowerRangeSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power range.
        /// </summary>
        public PowerRange Clone()

            => new (
                   StartOfRange,
                   EndOfRange,
                   CommodityQuantity.Clone()
               );

        #endregion


        #region Contains(Value)

        /// <summary>
        /// Whether the given power value lies within this range (both boundaries inclusive).
        /// </summary>
        /// <param name="Value">A power value.</param>
        public Boolean Contains(Double Value)
            => Value >= StartOfRange && Value <= EndOfRange;

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power ranges for equality.
        /// </summary>
        public static Boolean operator == (PowerRange? PowerRange1, PowerRange? PowerRange2)
        {

            if (ReferenceEquals(PowerRange1, PowerRange2))
                return true;

            if (PowerRange1 is null || PowerRange2 is null)
                return false;

            return PowerRange1.Equals(PowerRange2);

        }

        /// <summary>
        /// Compares two power ranges for inequality.
        /// </summary>
        public static Boolean operator != (PowerRange? PowerRange1, PowerRange? PowerRange2)
            => !(PowerRange1 == PowerRange2);

        #endregion

        #region IEquatable<PowerRange> Members

        /// <summary>
        /// Compares two power ranges for equality.
        /// </summary>
        /// <param name="Object">A power range to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PowerRange powerRange && Equals(powerRange);

        /// <summary>
        /// Compares two power ranges for equality.
        /// </summary>
        /// <param name="PowerRange">A power range to compare with.</param>
        public Boolean Equals(PowerRange? PowerRange)

            => PowerRange is not null &&

               StartOfRange.     Equals(PowerRange.StartOfRange)  &&
               EndOfRange.       Equals(PowerRange.EndOfRange)    &&
               CommodityQuantity.Equals(PowerRange.CommodityQuantity);

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
            => $"{StartOfRange} .. {EndOfRange} ({CommodityQuantity})";

        #endregion

    }

}

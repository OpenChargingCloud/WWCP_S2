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
    /// A power value for a commodity quantity, expressed in the unit associated
    /// with the commodity quantity (e.g. Watt for electric power).
    /// </summary>
    public sealed class PowerValue : IEquatable<PowerValue>
    {

        #region Properties

        /// <summary>
        /// The power quantity the value refers to.
        /// </summary>
        [Mandatory]
        public CommodityQuantity  CommodityQuantity    { get; }

        /// <summary>
        /// Power value expressed in the unit associated with the commodity quantity.
        /// </summary>
        [Mandatory]
        public Double             Value                { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power value.
        /// </summary>
        /// <param name="CommodityQuantity">The power quantity the value refers to.</param>
        /// <param name="Value">Power value expressed in the unit associated with the commodity quantity.</param>
        public PowerValue(CommodityQuantity  CommodityQuantity,
                          Double             Value)
        {

            if (CommodityQuantity.IsNullOrEmpty)
                throw new ArgumentException("The commodity quantity of a power value must not be null or empty!",
                                            nameof(CommodityQuantity));

            if (!Double.IsFinite(Value))
                throw new ArgumentException("A power value must be a finite number!",
                                            nameof(Value));

            this.CommodityQuantity  = CommodityQuantity;
            this.Value              = Value;

            unchecked
            {
                hashCode = this.CommodityQuantity.GetHashCode() * 3 ^
                           this.Value.            GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // PowerValue.schema.json
        //   "title": "PowerValue",
        //   "properties": {
        //     "commodity_quantity": { "$ref": "../schemas/CommodityQuantity.schema.json",
        //                             "description": "The power quantity the value refers to" },
        //     "value":              { "type": "number",
        //                             "description": "Power value expressed in the unit associated with the CommodityQuantity" }
        //   },
        //   "required": ["commodity_quantity", "value"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out PowerValue, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a power value.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerValue">The parsed power value.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                               JSON,
                                       [NotNullWhen(true)]  out PowerValue?  PowerValue,
                                       [NotNullWhen(false)] out String?      ErrorResponse)

            => TryParse(JSON,
                        out PowerValue,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power value.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerValue">The parsed power value.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                               JSON,
                                       [NotNullWhen(true)]  out PowerValue?  PowerValue,
                                       [NotNullWhen(false)] out String?      ErrorResponse,
                                       S2ParserOptions?                      Options)

            => TryParse(JSON,
                        out PowerValue,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power value.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerValue">The parsed power value.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPowerValueParser">A delegate to parse custom power values.</param>
        public static Boolean TryParse(JObject                                   JSON,
                                       [NotNullWhen(true)]  out PowerValue?      PowerValue,
                                       [NotNullWhen(false)] out String?          ErrorResponse,
                                       S2ParserOptions?                          Options,
                                       CustomJObjectParserDelegate<PowerValue>?  CustomPowerValueParser)
        {

            try
            {

                PowerValue = null;

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

                #region value                 [mandatory]

                if (!JSON.ParseMandatoryS2Number("value",
                                                 "power value",
                                                 out Double value,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "commodity_quantity",
                                                    "value"))
                {
                    return false;
                }

                #endregion


                PowerValue = new PowerValue(
                                 commodityQuantity,
                                 value
                             );

                if (CustomPowerValueParser is not null)
                    PowerValue = CustomPowerValueParser(JSON,
                                                        PowerValue);

                return true;

            }
            catch (Exception e)
            {
                PowerValue     = null;
                ErrorResponse  = "The given JSON representation of a power value is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerValueSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPowerValueSerializer">A delegate to serialize custom power values.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PowerValue>? CustomPowerValueSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("commodity_quantity",  CommodityQuantity.ToString()),
                           new JProperty("value",               Value)
                       );

            return CustomPowerValueSerializer is not null
                       ? CustomPowerValueSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power value.
        /// </summary>
        public PowerValue Clone()

            => new (
                   CommodityQuantity.Clone(),
                   Value
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power values for equality.
        /// </summary>
        public static Boolean operator == (PowerValue? PowerValue1, PowerValue? PowerValue2)
        {

            if (ReferenceEquals(PowerValue1, PowerValue2))
                return true;

            if (PowerValue1 is null || PowerValue2 is null)
                return false;

            return PowerValue1.Equals(PowerValue2);

        }

        /// <summary>
        /// Compares two power values for inequality.
        /// </summary>
        public static Boolean operator != (PowerValue? PowerValue1, PowerValue? PowerValue2)
            => !(PowerValue1 == PowerValue2);

        #endregion

        #region IEquatable<PowerValue> Members

        /// <summary>
        /// Compares two power values for equality.
        /// </summary>
        /// <param name="Object">A power value to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PowerValue powerValue && Equals(powerValue);

        /// <summary>
        /// Compares two power values for equality.
        /// </summary>
        /// <param name="PowerValue">A power value to compare with.</param>
        public Boolean Equals(PowerValue? PowerValue)

            => PowerValue is not null &&

               CommodityQuantity.Equals(PowerValue.CommodityQuantity) &&
               Value.            Equals(PowerValue.Value);

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
            => $"{Value} ({CommodityQuantity})";

        #endregion

    }

}

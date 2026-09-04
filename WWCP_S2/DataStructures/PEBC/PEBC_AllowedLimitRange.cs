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
    /// A range of allowed values for the upper or lower limit of a power envelope
    /// (Power Envelope Based Control). The CEM is allowed to choose values within
    /// this range for the power envelope for the limit described by the limit type.
    /// </summary>
    public sealed class PEBC_AllowedLimitRange : IEquatable<PEBC_AllowedLimitRange>
    {

        #region Properties

        /// <summary>
        /// The type of power quantity this allowed limit range applies to.
        /// </summary>
        [Mandatory]
        public CommodityQuantity            CommodityQuantity        { get; }

        /// <summary>
        /// Indicates whether this range applies to the upper limit or the lower limit.
        /// </summary>
        [Mandatory]
        public PEBC_PowerEnvelopeLimitType  LimitType                { get; }

        /// <summary>
        /// The boundaries of the power range of this allowed limit range. The CEM is allowed
        /// to choose values within this range for the power envelope for the limit as described
        /// in the limit type. The start of the range shall be smaller or equal than the end of the range.
        /// </summary>
        [Mandatory]
        public NumberRange                  RangeBoundary            { get; }

        /// <summary>
        /// Indicates whether this allowed limit range may only be used during an abnormal condition.
        /// </summary>
        [Mandatory]
        public Boolean                      AbnormalConditionOnly    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new allowed limit range.
        /// </summary>
        /// <param name="CommodityQuantity">The type of power quantity this allowed limit range applies to.</param>
        /// <param name="LimitType">Whether this range applies to the upper limit or the lower limit.</param>
        /// <param name="RangeBoundary">The boundaries of the power range of this allowed limit range.</param>
        /// <param name="AbnormalConditionOnly">Whether this allowed limit range may only be used during an abnormal condition.</param>
        public PEBC_AllowedLimitRange(CommodityQuantity            CommodityQuantity,
                                      PEBC_PowerEnvelopeLimitType  LimitType,
                                      NumberRange                  RangeBoundary,
                                      Boolean                      AbnormalConditionOnly)
        {

            ArgumentNullException.ThrowIfNull(RangeBoundary);

            this.CommodityQuantity      = CommodityQuantity;
            this.LimitType              = LimitType;
            this.RangeBoundary          = RangeBoundary;
            this.AbnormalConditionOnly  = AbnormalConditionOnly;

            unchecked
            {
                hashCode = this.CommodityQuantity.    GetHashCode() * 7 ^
                           this.LimitType.            GetHashCode() * 5 ^
                           this.RangeBoundary.        GetHashCode() * 3 ^
                           this.AbnormalConditionOnly.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // PEBC.AllowedLimitRange.schema.json
        //   "title": "PEBC_AllowedLimitRange",
        //   "properties": {
        //     "commodity_quantity":      { "$ref": "../schemas/CommodityQuantity.schema.json",
        //                                  "description": "Type of power quantity this PEBC.AllowedLimitRange applies to" },
        //     "limit_type":              { "$ref": "../schemas/PEBC.PowerEnvelopeLimitType.schema.json",
        //                                  "description": "Indicates if this ranges applies to the upper limit or the lower limit" },
        //     "range_boundary":          { "$ref": "../schemas/NumberRange.schema.json",
        //                                  "description": "Boundaries of the power range of this PEBC.AllowedLimitRange. The CEM is
        //                                                  allowed to choose values within this range for the power envelope for the
        //                                                  limit as described in limit_type. The start of the range shall be smaller
        //                                                  or equal than the end of the range." },
        //     "abnormal_condition_only": { "type": "boolean",
        //                                  "description": "Indicates if this PEBC.AllowedLimitRange may only be used during an abnormal condition" }
        //   },
        //   "required": ["commodity_quantity", "limit_type", "range_boundary", "abnormal_condition_only"],
        //   "additionalProperties": false
        //
        // Semantic rule: range_boundary.start_of_range <= range_boundary.end_of_range (validated by NumberRange).

        #endregion

        #region (static) TryParse(JSON, out PEBC_AllowedLimitRange, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an allowed limit range.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AllowedLimitRange">The parsed allowed limit range.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out PEBC_AllowedLimitRange?  AllowedLimitRange,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)

            => TryParse(JSON,
                        out AllowedLimitRange,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an allowed limit range.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AllowedLimitRange">The parsed allowed limit range.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out PEBC_AllowedLimitRange?  AllowedLimitRange,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options)

            => TryParse(JSON,
                        out AllowedLimitRange,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an allowed limit range.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AllowedLimitRange">The parsed allowed limit range.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomAllowedLimitRangeParser">A delegate to parse custom allowed limit ranges.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out PEBC_AllowedLimitRange?      AllowedLimitRange,
                                       [NotNullWhen(false)] out String?                      ErrorResponse,
                                       S2ParserOptions?                                      Options,
                                       CustomJObjectParserDelegate<PEBC_AllowedLimitRange>?  CustomAllowedLimitRangeParser)
        {

            try
            {

                AllowedLimitRange = null;

                #region commodity_quantity         [mandatory]

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

                #region limit_type                 [mandatory]

                if (!JSON.ParseMandatoryS2Enum("limit_type",
                                               "limit type",
                                               PEBC_PowerEnvelopeLimitType.TryParse,
                                               Options,
                                               out PEBC_PowerEnvelopeLimitType limitType,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region range_boundary             [mandatory]

                if (!JSON.ParseMandatoryS2("range_boundary",
                                           "range boundary",
                                           NumberRange.TryParse,
                                           Options,
                                           out NumberRange? rangeBoundary,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region abnormal_condition_only    [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("abnormal_condition_only",
                                                  "abnormal condition only",
                                                  out Boolean abnormalConditionOnly,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "commodity_quantity",
                                                    "limit_type",
                                                    "range_boundary",
                                                    "abnormal_condition_only"))
                {
                    return false;
                }

                #endregion


                AllowedLimitRange = new PEBC_AllowedLimitRange(
                                        commodityQuantity,
                                        limitType,
                                        rangeBoundary,
                                        abnormalConditionOnly
                                    );

                if (CustomAllowedLimitRangeParser is not null)
                    AllowedLimitRange = CustomAllowedLimitRangeParser(JSON,
                                                                      AllowedLimitRange);

                return true;

            }
            catch (Exception e)
            {
                AllowedLimitRange  = null;
                ErrorResponse      = "The given JSON representation of an allowed limit range is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomAllowedLimitRangeSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomAllowedLimitRangeSerializer">A delegate to serialize custom allowed limit ranges.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PEBC_AllowedLimitRange>? CustomAllowedLimitRangeSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("commodity_quantity",       CommodityQuantity.ToString()),
                           new JProperty("limit_type",               LimitType.        ToString()),
                           new JProperty("range_boundary",           RangeBoundary.    ToJSON()),
                           new JProperty("abnormal_condition_only",  AbnormalConditionOnly)
                       );

            return CustomAllowedLimitRangeSerializer is not null
                       ? CustomAllowedLimitRangeSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this allowed limit range.
        /// </summary>
        public PEBC_AllowedLimitRange Clone()

            => new (
                   CommodityQuantity,
                   LimitType,
                   RangeBoundary.Clone(),
                   AbnormalConditionOnly
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two allowed limit ranges for equality.
        /// </summary>
        public static Boolean operator == (PEBC_AllowedLimitRange? AllowedLimitRange1, PEBC_AllowedLimitRange? AllowedLimitRange2)
        {

            if (ReferenceEquals(AllowedLimitRange1, AllowedLimitRange2))
                return true;

            if (AllowedLimitRange1 is null || AllowedLimitRange2 is null)
                return false;

            return AllowedLimitRange1.Equals(AllowedLimitRange2);

        }

        /// <summary>
        /// Compares two allowed limit ranges for inequality.
        /// </summary>
        public static Boolean operator != (PEBC_AllowedLimitRange? AllowedLimitRange1, PEBC_AllowedLimitRange? AllowedLimitRange2)
            => !(AllowedLimitRange1 == AllowedLimitRange2);

        #endregion

        #region IEquatable<PEBC_AllowedLimitRange> Members

        /// <summary>
        /// Compares two allowed limit ranges for equality.
        /// </summary>
        /// <param name="Object">An allowed limit range to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PEBC_AllowedLimitRange allowedLimitRange && Equals(allowedLimitRange);

        /// <summary>
        /// Compares two allowed limit ranges for equality.
        /// </summary>
        /// <param name="AllowedLimitRange">An allowed limit range to compare with.</param>
        public Boolean Equals(PEBC_AllowedLimitRange? AllowedLimitRange)

            => AllowedLimitRange is not null &&

               CommodityQuantity.    Equals(AllowedLimitRange.CommodityQuantity) &&
               LimitType.            Equals(AllowedLimitRange.LimitType)         &&
               RangeBoundary.        Equals(AllowedLimitRange.RangeBoundary)     &&
               AbnormalConditionOnly.Equals(AllowedLimitRange.AbnormalConditionOnly);

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

                   $"{LimitType} of {CommodityQuantity}: {RangeBoundary}",

                   AbnormalConditionOnly
                       ? " (abnormal condition only)"
                       : ""

               );

        #endregion

    }

}

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
    /// A power envelope (Power Envelope Based Control): a chronological sequence of
    /// power envelope elements, each requesting the Resource Manager to keep the power
    /// values of one commodity quantity between a lower and an upper limit for its duration.
    /// </summary>
    public sealed class PEBC_PowerEnvelope : IEquatable<PEBC_PowerEnvelope>
    {

        #region Properties

        /// <summary>
        /// The identification of this power envelope. Must be unique in the scope of the
        /// Resource Manager, for at least the duration of the session between Resource Manager and CEM.
        /// </summary>
        [Mandatory]
        public PowerEnvelope_Id                          Id                       { get; }

        /// <summary>
        /// The type of power quantity this power envelope applies to.
        /// </summary>
        [Mandatory]
        public CommodityQuantity                         CommodityQuantity        { get; }

        /// <summary>
        /// The elements of this power envelope. Shall contain at least one element (1..288).
        /// Elements must be placed in chronological order.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PEBC_PowerEnvelopeElement>  PowerEnvelopeElements    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power envelope.
        /// </summary>
        /// <param name="Id">The identification of this power envelope.</param>
        /// <param name="CommodityQuantity">The type of power quantity this power envelope applies to.</param>
        /// <param name="PowerEnvelopeElements">The elements of this power envelope in chronological order (1..288).</param>
        public PEBC_PowerEnvelope(PowerEnvelope_Id                          Id,
                                  CommodityQuantity                         CommodityQuantity,
                                  IReadOnlyList<PEBC_PowerEnvelopeElement>  PowerEnvelopeElements)
        {

            ArgumentNullException.ThrowIfNull(PowerEnvelopeElements);

            if (PowerEnvelopeElements.Count < 1)
                throw new ArgumentException("A power envelope must contain at least one power envelope element!",
                                            nameof(PowerEnvelopeElements));

            if (PowerEnvelopeElements.Count > 288)
                throw new ArgumentException($"A power envelope must not contain more than 288 power envelope elements, but {PowerEnvelopeElements.Count} were given!",
                                            nameof(PowerEnvelopeElements));

            if (PowerEnvelopeElements.Any(element => element is null))
                throw new ArgumentException("A power envelope must not contain null elements!",
                                            nameof(PowerEnvelopeElements));

            this.Id                     = Id;
            this.CommodityQuantity      = CommodityQuantity;
            this.PowerEnvelopeElements  = [.. PowerEnvelopeElements];

            unchecked
            {
                hashCode = this.Id.                   GetHashCode()  * 5 ^
                           this.CommodityQuantity.    GetHashCode()  * 3 ^
                           this.PowerEnvelopeElements.CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // PEBC.PowerEnvelope.schema.json
        //   "title": "PEBC_PowerEnvelope",
        //   "properties": {
        //     "id":                      { "$ref": "../schemas/ID.schema.json",
        //                                  "description": "Identifier of this PEBC.PowerEnvelope. Must be unique in the scope of the
        //                                                  Resource Manager, for at least the duration of the session between
        //                                                  Resource Manager and CEM." },
        //     "commodity_quantity":      { "$ref": "../schemas/CommodityQuantity.schema.json",
        //                                  "description": "Type of power quantity this PEBC.PowerEnvelope applies to" },
        //     "power_envelope_elements": { "type": "array", "minItems": 1, "maxItems": 288,
        //                                  "items": { "$ref": "../schemas/PEBC.PowerEnvelopeElement.schema.json" },
        //                                  "description": "The elements of this PEBC.PowerEnvelope. Shall contain at least one
        //                                                  element. Elements must be placed in chronological order." }
        //   },
        //   "required": ["id", "commodity_quantity", "power_envelope_elements"],
        //   "additionalProperties": false
        //
        // Schema constraints: 1 <= power_envelope_elements.Count <= 288 (validated in the constructor).
        // Cross-message rule (session layer, PEBC.Instruction): at most one power envelope per commodity quantity
        // per instruction; every element's limits shall be within the allowed limit ranges of the PEBC.PowerConstraints.

        #endregion

        #region (static) TryParse(JSON, out PEBC_PowerEnvelope, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a power envelope.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerEnvelope">The parsed power envelope.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out PEBC_PowerEnvelope?  PowerEnvelope,
                                       [NotNullWhen(false)] out String?              ErrorResponse)

            => TryParse(JSON,
                        out PowerEnvelope,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power envelope.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerEnvelope">The parsed power envelope.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out PEBC_PowerEnvelope?  PowerEnvelope,
                                       [NotNullWhen(false)] out String?              ErrorResponse,
                                       S2ParserOptions?                              Options)

            => TryParse(JSON,
                        out PowerEnvelope,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power envelope.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerEnvelope">The parsed power envelope.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPowerEnvelopeParser">A delegate to parse custom power envelopes.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out PEBC_PowerEnvelope?      PowerEnvelope,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options,
                                       CustomJObjectParserDelegate<PEBC_PowerEnvelope>?  CustomPowerEnvelopeParser)
        {

            try
            {

                PowerEnvelope = null;

                #region id                         [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
                                             "power envelope identification",
                                             PowerEnvelope_Id.TryParse,
                                             Options,
                                             out PowerEnvelope_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

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

                #region power_envelope_elements    [mandatory]

                if (!JSON.ParseMandatoryS2List("power_envelope_elements",
                                               "power envelope elements",
                                               PEBC_PowerEnvelopeElement.TryParse,
                                               Options,
                                               1,
                                               288,
                                               out IReadOnlyList<PEBC_PowerEnvelopeElement>? powerEnvelopeElements,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "id",
                                                    "commodity_quantity",
                                                    "power_envelope_elements"))
                {
                    return false;
                }

                #endregion


                PowerEnvelope = new PEBC_PowerEnvelope(
                                    id,
                                    commodityQuantity,
                                    powerEnvelopeElements
                                );

                if (CustomPowerEnvelopeParser is not null)
                    PowerEnvelope = CustomPowerEnvelopeParser(JSON,
                                                              PowerEnvelope);

                return true;

            }
            catch (Exception e)
            {
                PowerEnvelope  = null;
                ErrorResponse  = "The given JSON representation of a power envelope is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerEnvelopeSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPowerEnvelopeSerializer">A delegate to serialize custom power envelopes.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PEBC_PowerEnvelope>? CustomPowerEnvelopeSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("id",                       Id.               ToString()),
                           new JProperty("commodity_quantity",       CommodityQuantity.ToString()),
                           new JProperty("power_envelope_elements",  new JArray(PowerEnvelopeElements.Select(element => element.ToJSON())))
                       );

            return CustomPowerEnvelopeSerializer is not null
                       ? CustomPowerEnvelopeSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power envelope.
        /// </summary>
        public PEBC_PowerEnvelope Clone()

            => new (
                   Id.Clone(),
                   CommodityQuantity,
                   [.. PowerEnvelopeElements.Select(element => element.Clone())]
               );

        #endregion


        #region TotalDuration

        /// <summary>
        /// The total duration of this power envelope: the sum of the durations of all elements.
        /// </summary>
        public Duration TotalDuration
            => PowerEnvelopeElements.Aggregate(Duration.Zero, (sum, element) => sum + element.Duration);

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power envelopes for equality.
        /// </summary>
        public static Boolean operator == (PEBC_PowerEnvelope? PowerEnvelope1, PEBC_PowerEnvelope? PowerEnvelope2)
        {

            if (ReferenceEquals(PowerEnvelope1, PowerEnvelope2))
                return true;

            if (PowerEnvelope1 is null || PowerEnvelope2 is null)
                return false;

            return PowerEnvelope1.Equals(PowerEnvelope2);

        }

        /// <summary>
        /// Compares two power envelopes for inequality.
        /// </summary>
        public static Boolean operator != (PEBC_PowerEnvelope? PowerEnvelope1, PEBC_PowerEnvelope? PowerEnvelope2)
            => !(PowerEnvelope1 == PowerEnvelope2);

        #endregion

        #region IEquatable<PEBC_PowerEnvelope> Members

        /// <summary>
        /// Compares two power envelopes for equality.
        /// </summary>
        /// <param name="Object">A power envelope to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PEBC_PowerEnvelope powerEnvelope && Equals(powerEnvelope);

        /// <summary>
        /// Compares two power envelopes for equality.
        /// </summary>
        /// <param name="PowerEnvelope">A power envelope to compare with.</param>
        public Boolean Equals(PEBC_PowerEnvelope? PowerEnvelope)

            => PowerEnvelope is not null &&

               Id.               Equals(PowerEnvelope.Id)                &&
               CommodityQuantity.Equals(PowerEnvelope.CommodityQuantity) &&

               PowerEnvelopeElements.SequenceEqual(PowerEnvelope.PowerEnvelopeElements);

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

            => $"{Id}: {CommodityQuantity}, {PowerEnvelopeElements.Count} element(s), {TotalDuration}";

        #endregion

    }

}

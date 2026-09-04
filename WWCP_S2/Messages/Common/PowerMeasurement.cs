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
    /// A power measurement of the Resource Manager: the power values measured at a
    /// given moment, at most one per commodity quantity.
    /// </summary>
    public sealed class PowerMeasurement : AS2Message,
                                           IEquatable<PowerMeasurement>
    {

        #region Data

        /// <summary>
        /// The message type "PowerMeasurement".
        /// </summary>
        public const String MessageTypeName = "PowerMeasurement";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "PowerMeasurement".
        /// </summary>
        public override String            MessageType
            => MessageTypeName;

        /// <summary>
        /// The timestamp when the power values were measured.
        /// </summary>
        [Mandatory]
        public DateTimeOffset             MeasurementTimestamp    { get; }

        /// <summary>
        /// The measured power values. Contains at least one item and at most one
        /// item per commodity quantity (defined inside the power value).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PowerValue>  Values                  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power measurement.
        /// </summary>
        /// <param name="MeasurementTimestamp">The timestamp when the power values were measured.</param>
        /// <param name="Values">The measured power values (1..10, at most one per commodity quantity).</param>
        /// <param name="MessageId">An optional message identification.</param>
        public PowerMeasurement(DateTimeOffset             MeasurementTimestamp,
                                IReadOnlyList<PowerValue>  Values,
                                Message_Id?                MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(Values);

            if (Values.Count < 1)
                throw new ArgumentException("A power measurement must contain at least one power value!",
                                            nameof(Values));

            if (Values.Count > 10)
                throw new ArgumentException($"A power measurement must not contain more than 10 power values, but {Values.Count} were given!",
                                            nameof(Values));

            var duplicate = Values.GroupBy(powerValue => powerValue.CommodityQuantity).FirstOrDefault(group => group.Count() > 1);

            if (duplicate is not null)
                throw new ArgumentException($"A power measurement must contain at most one power value per commodity quantity, but '{duplicate.Key}' occurs {duplicate.Count()} times!",
                                            nameof(Values));

            this.MeasurementTimestamp  = MeasurementTimestamp;
            this.Values                = [.. Values];

            unchecked
            {
                hashCode = this.MessageId.           GetHashCode() * 5 ^
                           this.MeasurementTimestamp.GetHashCode() * 3 ^
                           this.Values.              CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // PowerMeasurement.schema.json
        //   "title": "PowerMeasurement",
        //   "properties": {
        //     "message_type":          { "type": "string", "const": "PowerMeasurement" },
        //     "message_id":            { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "measurement_timestamp": { "type": "string", "format": "date-time",
        //                                "description": "Timestamp when PowerValues were measured." },
        //     "values":                { "type": "array", "minItems": 1, "maxItems": 10,
        //                                "items": { "$ref": "../schemas/PowerValue.schema.json" },
        //                                "description": "Array of measured PowerValues. Must contain at least one item and at most
        //                                                one item per ‘commodity_quantity’ (defined inside the PowerValue)." }
        //   },
        //   "required": ["message_type", "message_id", "measurement_timestamp", "values"],
        //   "additionalProperties": false
        //
        // Semantic rule (CONVENTIONS.md §8): at most one PowerValue per CommodityQuantity.

        #endregion

        #region (static) TryParse(JSON, out PowerMeasurement, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a power measurement.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerMeasurement">The parsed power measurement.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out PowerMeasurement?  PowerMeasurement,
                                       [NotNullWhen(false)] out String?            ErrorResponse)

            => TryParse(JSON,
                        out PowerMeasurement,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power measurement.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerMeasurement">The parsed power measurement.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out PowerMeasurement?  PowerMeasurement,
                                       [NotNullWhen(false)] out String?            ErrorResponse,
                                       S2ParserOptions?                            Options)

            => TryParse(JSON,
                        out PowerMeasurement,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power measurement.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerMeasurement">The parsed power measurement.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPowerMeasurementParser">A delegate to parse custom power measurements.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out PowerMeasurement?      PowerMeasurement,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       S2ParserOptions?                                Options,
                                       CustomJObjectParserDelegate<PowerMeasurement>?  CustomPowerMeasurementParser)
        {

            try
            {

                PowerMeasurement = null;

                #region message_type, message_id   [mandatory]

                if (!TryParseHeader(JSON,
                                    MessageTypeName,
                                    Options,
                                    out var messageId,
                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region measurement_timestamp      [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("measurement_timestamp",
                                                    "measurement timestamp",
                                                    Options,
                                                    out DateTimeOffset measurementTimestamp,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region values                     [mandatory]

                if (!JSON.ParseMandatoryS2List("values",
                                               "power values",
                                               PowerValue.TryParse,
                                               Options,
                                               1,
                                               10,
                                               out IReadOnlyList<PowerValue>? values,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "message_type",
                                                    "message_id",
                                                    "measurement_timestamp",
                                                    "values"))
                {
                    return false;
                }

                #endregion


                PowerMeasurement = new PowerMeasurement(
                                       measurementTimestamp,
                                       values,
                                       messageId
                                   );

                if (CustomPowerMeasurementParser is not null)
                    PowerMeasurement = CustomPowerMeasurementParser(JSON,
                                                                    PowerMeasurement);

                return true;

            }
            catch (Exception e)
            {
                PowerMeasurement  = null;
                ErrorResponse     = "The given JSON representation of a power measurement is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerMeasurementSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomPowerMeasurementSerializer">A delegate to serialize custom power measurements.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PowerMeasurement>? CustomPowerMeasurementSerializer)
        {

            var json = CreateJSON(
                           new JProperty("measurement_timestamp",  MeasurementTimestamp.ToS2Timestamp()),
                           new JProperty("values",                 new JArray(Values.Select(powerValue => powerValue.ToJSON())))
                       );

            return CustomPowerMeasurementSerializer is not null
                       ? CustomPowerMeasurementSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power measurement.
        /// </summary>
        public PowerMeasurement Clone()

            => new (
                   MeasurementTimestamp,
                   Values.   Select(powerValue => powerValue.Clone()).ToList(),
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power measurements for equality.
        /// </summary>
        public static Boolean operator == (PowerMeasurement? PowerMeasurement1, PowerMeasurement? PowerMeasurement2)
        {

            if (ReferenceEquals(PowerMeasurement1, PowerMeasurement2))
                return true;

            if (PowerMeasurement1 is null || PowerMeasurement2 is null)
                return false;

            return PowerMeasurement1.Equals(PowerMeasurement2);

        }

        /// <summary>
        /// Compares two power measurements for inequality.
        /// </summary>
        public static Boolean operator != (PowerMeasurement? PowerMeasurement1, PowerMeasurement? PowerMeasurement2)
            => !(PowerMeasurement1 == PowerMeasurement2);

        #endregion

        #region IEquatable<PowerMeasurement> Members

        /// <summary>
        /// Compares two power measurements for equality.
        /// </summary>
        /// <param name="Object">A power measurement to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PowerMeasurement powerMeasurement && Equals(powerMeasurement);

        /// <summary>
        /// Compares two power measurements for equality.
        /// </summary>
        /// <param name="PowerMeasurement">A power measurement to compare with.</param>
        public Boolean Equals(PowerMeasurement? PowerMeasurement)

            => PowerMeasurement is not null &&

               MessageId.           Equals(PowerMeasurement.MessageId)            &&
               MeasurementTimestamp.Equals(PowerMeasurement.MeasurementTimestamp) &&
               Values.              SequenceEqual(PowerMeasurement.Values);

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
            => $"PowerMeasurement at {MeasurementTimestamp.ToS2Timestamp()}: {String.Join(", ", Values)} [{MessageId}]";

        #endregion

    }

}

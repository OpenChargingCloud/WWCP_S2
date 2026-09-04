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
    /// A power forecast of the Resource Manager: the expected power values for a
    /// sequence of chronological time periods starting at a given moment.
    /// </summary>
    public sealed class PowerForecast : AS2Message,
                                        IEquatable<PowerForecast>
    {

        #region Data

        /// <summary>
        /// The message type "PowerForecast".
        /// </summary>
        public const String MessageTypeName = "PowerForecast";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "PowerForecast".
        /// </summary>
        public override String                      MessageType
            => MessageTypeName;

        /// <summary>
        /// The start time of the time period that is covered by the profile.
        /// </summary>
        [Mandatory]
        public DateTimeOffset                       StartTime    { get; }

        /// <summary>
        /// The elements of which this forecast consists. Contains at least one
        /// element. Elements must be placed in chronological order.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PowerForecastElement>  Elements     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power forecast.
        /// </summary>
        /// <param name="StartTime">The start time of the time period that is covered by the profile.</param>
        /// <param name="Elements">The elements of which this forecast consists, in chronological order (1..288).</param>
        /// <param name="MessageId">An optional message identification.</param>
        public PowerForecast(DateTimeOffset                       StartTime,
                             IReadOnlyList<PowerForecastElement>  Elements,
                             Message_Id?                          MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(Elements);

            if (Elements.Count < 1)
                throw new ArgumentException("A power forecast must contain at least one element!",
                                            nameof(Elements));

            if (Elements.Count > 288)
                throw new ArgumentException($"A power forecast must not contain more than 288 elements, but {Elements.Count} were given!",
                                            nameof(Elements));

            this.StartTime  = StartTime;
            this.Elements   = [.. Elements];

            unchecked
            {
                hashCode = this.MessageId.GetHashCode() * 5 ^
                           this.StartTime.GetHashCode() * 3 ^
                           this.Elements. CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // PowerForecast.schema.json
        //   "title": "PowerForecast",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "PowerForecast" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "start_time":   { "type": "string", "format": "date-time",
        //                       "description": "Start time of time period that is covered by the profile." },
        //     "elements":     { "type": "array", "minItems": 1, "maxItems": 288,
        //                       "items": { "$ref": "../schemas/PowerForecastElement.schema.json" },
        //                       "description": "Elements of which this forecast consists. Contains at least one element.
        //                                       Elements must be placed in chronological order." }
        //   },
        //   "required": ["message_type", "message_id", "start_time", "elements"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out PowerForecast, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a power forecast.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerForecast">The parsed power forecast.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                  JSON,
                                       [NotNullWhen(true)]  out PowerForecast?  PowerForecast,
                                       [NotNullWhen(false)] out String?         ErrorResponse)

            => TryParse(JSON,
                        out PowerForecast,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power forecast.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerForecast">The parsed power forecast.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                  JSON,
                                       [NotNullWhen(true)]  out PowerForecast?  PowerForecast,
                                       [NotNullWhen(false)] out String?         ErrorResponse,
                                       S2ParserOptions?                         Options)

            => TryParse(JSON,
                        out PowerForecast,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power forecast.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerForecast">The parsed power forecast.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPowerForecastParser">A delegate to parse custom power forecasts.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out PowerForecast?      PowerForecast,
                                       [NotNullWhen(false)] out String?             ErrorResponse,
                                       S2ParserOptions?                             Options,
                                       CustomJObjectParserDelegate<PowerForecast>?  CustomPowerForecastParser)
        {

            try
            {

                PowerForecast = null;

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

                #region start_time                 [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("start_time",
                                                    "start time",
                                                    Options,
                                                    out DateTimeOffset startTime,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region elements                   [mandatory]

                if (!JSON.ParseMandatoryS2List("elements",
                                               "power forecast elements",
                                               PowerForecastElement.TryParse,
                                               Options,
                                               1,
                                               288,
                                               out IReadOnlyList<PowerForecastElement>? elements,
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
                                                    "start_time",
                                                    "elements"))
                {
                    return false;
                }

                #endregion


                PowerForecast = new PowerForecast(
                                    startTime,
                                    elements,
                                    messageId
                                );

                if (CustomPowerForecastParser is not null)
                    PowerForecast = CustomPowerForecastParser(JSON,
                                                              PowerForecast);

                return true;

            }
            catch (Exception e)
            {
                PowerForecast  = null;
                ErrorResponse  = "The given JSON representation of a power forecast is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerForecastSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomPowerForecastSerializer">A delegate to serialize custom power forecasts.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PowerForecast>? CustomPowerForecastSerializer)
        {

            var json = CreateJSON(
                           new JProperty("start_time",  StartTime.ToS2Timestamp()),
                           new JProperty("elements",    new JArray(Elements.Select(element => element.ToJSON())))
                       );

            return CustomPowerForecastSerializer is not null
                       ? CustomPowerForecastSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power forecast.
        /// </summary>
        public PowerForecast Clone()

            => new (
                   StartTime,
                   Elements. Select(element => element.Clone()).ToList(),
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power forecasts for equality.
        /// </summary>
        public static Boolean operator == (PowerForecast? PowerForecast1, PowerForecast? PowerForecast2)
        {

            if (ReferenceEquals(PowerForecast1, PowerForecast2))
                return true;

            if (PowerForecast1 is null || PowerForecast2 is null)
                return false;

            return PowerForecast1.Equals(PowerForecast2);

        }

        /// <summary>
        /// Compares two power forecasts for inequality.
        /// </summary>
        public static Boolean operator != (PowerForecast? PowerForecast1, PowerForecast? PowerForecast2)
            => !(PowerForecast1 == PowerForecast2);

        #endregion

        #region IEquatable<PowerForecast> Members

        /// <summary>
        /// Compares two power forecasts for equality.
        /// </summary>
        /// <param name="Object">A power forecast to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PowerForecast powerForecast && Equals(powerForecast);

        /// <summary>
        /// Compares two power forecasts for equality.
        /// </summary>
        /// <param name="PowerForecast">A power forecast to compare with.</param>
        public Boolean Equals(PowerForecast? PowerForecast)

            => PowerForecast is not null &&

               MessageId.Equals(PowerForecast.MessageId) &&
               StartTime.Equals(PowerForecast.StartTime) &&
               Elements. SequenceEqual(PowerForecast.Elements);

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
            => $"PowerForecast from {StartTime.ToS2Timestamp()} with {Elements.Count} element(s) [{MessageId}]";

        #endregion

    }

}

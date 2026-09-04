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
    /// The Resource Manager forecasts the average demand rate of the Demand Driven Based
    /// Control system as a chronological profile of forecast elements.
    /// </summary>
    public sealed class DDBC_AverageDemandRateForecast : AS2Message,
                                                         IEquatable<DDBC_AverageDemandRateForecast>
    {

        #region Data

        /// <summary>
        /// The message type "DDBC.AverageDemandRateForecast".
        /// </summary>
        public const String MessageTypeName = "DDBC.AverageDemandRateForecast";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "DDBC.AverageDemandRateForecast".
        /// </summary>
        public override String                                         MessageType
            => MessageTypeName;

        /// <summary>
        /// The start time of the profile.
        /// </summary>
        [Mandatory]
        public DateTimeOffset                                          StartTime    { get; }

        /// <summary>
        /// The elements of the profile (1..288), placed in chronological order.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<DDBC_AverageDemandRateForecastElement>    Elements     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new DDBC average demand rate forecast.
        /// </summary>
        /// <param name="StartTime">The start time of the profile.</param>
        /// <param name="Elements">The elements of the profile (1..288), in chronological order.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public DDBC_AverageDemandRateForecast(DateTimeOffset                                        StartTime,
                                              IReadOnlyList<DDBC_AverageDemandRateForecastElement>  Elements,
                                              Message_Id?                                           MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(Elements);

            if (Elements.Count < 1)
                throw new ArgumentException("The list of forecast elements must contain at least one item!",
                                            nameof(Elements));

            if (Elements.Count > 288)
                throw new ArgumentException("The list of forecast elements must not contain more than 288 items!",
                                            nameof(Elements));

            this.StartTime  = StartTime;
            this.Elements   = [.. Elements];

            unchecked
            {
                hashCode = this.MessageId.GetHashCode()  * 5 ^
                           this.StartTime.GetHashCode()  * 3 ^
                           this.Elements. CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // DDBC.AverageDemandRateForecast.schema.json
        //   "title": "DDBC_AverageDemandRateForecast",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "DDBC.AverageDemandRateForecast" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "start_time":   { "type": "string", "format": "date-time",
        //                       "description": "Start time of the profile." },
        //     "elements":     { "type": "array", "minItems": 1, "maxItems": 288,
        //                       "items": { "$ref": "../schemas/DDBC.AverageDemandRateForecastElement.schema.json" },
        //                       "description": "Elements of the profile. Elements must be placed in chronological order." }
        //   },
        //   "required": ["message_type", "message_id", "start_time", "elements"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out DDBCAverageDemandRateForecast, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a DDBC average demand rate forecast.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCAverageDemandRateForecast">The parsed DDBC average demand rate forecast.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                   JSON,
                                       [NotNullWhen(true)]  out DDBC_AverageDemandRateForecast?  DDBCAverageDemandRateForecast,
                                       [NotNullWhen(false)] out String?                          ErrorResponse)

            => TryParse(JSON,
                        out DDBCAverageDemandRateForecast,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC average demand rate forecast.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCAverageDemandRateForecast">The parsed DDBC average demand rate forecast.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                   JSON,
                                       [NotNullWhen(true)]  out DDBC_AverageDemandRateForecast?  DDBCAverageDemandRateForecast,
                                       [NotNullWhen(false)] out String?                          ErrorResponse,
                                       S2ParserOptions?                                          Options)

            => TryParse(JSON,
                        out DDBCAverageDemandRateForecast,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC average demand rate forecast.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCAverageDemandRateForecast">The parsed DDBC average demand rate forecast.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomDDBCAverageDemandRateForecastParser">A delegate to parse custom DDBC average demand rate forecasts.</param>
        public static Boolean TryParse(JObject                                                       JSON,
                                       [NotNullWhen(true)]  out DDBC_AverageDemandRateForecast?      DDBCAverageDemandRateForecast,
                                       [NotNullWhen(false)] out String?                              ErrorResponse,
                                       S2ParserOptions?                                              Options,
                                       CustomJObjectParserDelegate<DDBC_AverageDemandRateForecast>?  CustomDDBCAverageDemandRateForecastParser)
        {

            try
            {

                DDBCAverageDemandRateForecast = null;

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
                                               "average demand rate forecast elements",
                                               DDBC_AverageDemandRateForecastElement.TryParse,
                                               Options,
                                               1,
                                               288,
                                               out IReadOnlyList<DDBC_AverageDemandRateForecastElement>? elements,
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


                DDBCAverageDemandRateForecast = new DDBC_AverageDemandRateForecast(
                                                    startTime,
                                                    elements,
                                                    messageId
                                                );

                if (CustomDDBCAverageDemandRateForecastParser is not null)
                    DDBCAverageDemandRateForecast = CustomDDBCAverageDemandRateForecastParser(JSON,
                                                                                              DDBCAverageDemandRateForecast);

                return true;

            }
            catch (Exception e)
            {
                DDBCAverageDemandRateForecast  = null;
                ErrorResponse                  = "The given JSON representation of a DDBC average demand rate forecast is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDDBCAverageDemandRateForecastSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomDDBCAverageDemandRateForecastSerializer">A delegate to serialize custom DDBC average demand rate forecasts.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DDBC_AverageDemandRateForecast>? CustomDDBCAverageDemandRateForecastSerializer)
        {

            var json = CreateJSON(
                           new JProperty("start_time",  StartTime.ToS2Timestamp()),
                           new JProperty("elements",    new JArray(Elements.Select(element => element.ToJSON())))
                       );

            return CustomDDBCAverageDemandRateForecastSerializer is not null
                       ? CustomDDBCAverageDemandRateForecastSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this DDBC average demand rate forecast.
        /// </summary>
        public DDBC_AverageDemandRateForecast Clone()

            => new (
                   StartTime,
                   Elements.Select(element => element.Clone()).ToList(),
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two DDBC average demand rate forecasts for equality.
        /// </summary>
        public static Boolean operator == (DDBC_AverageDemandRateForecast? DDBCAverageDemandRateForecast1, DDBC_AverageDemandRateForecast? DDBCAverageDemandRateForecast2)
        {

            if (ReferenceEquals(DDBCAverageDemandRateForecast1, DDBCAverageDemandRateForecast2))
                return true;

            if (DDBCAverageDemandRateForecast1 is null || DDBCAverageDemandRateForecast2 is null)
                return false;

            return DDBCAverageDemandRateForecast1.Equals(DDBCAverageDemandRateForecast2);

        }

        /// <summary>
        /// Compares two DDBC average demand rate forecasts for inequality.
        /// </summary>
        public static Boolean operator != (DDBC_AverageDemandRateForecast? DDBCAverageDemandRateForecast1, DDBC_AverageDemandRateForecast? DDBCAverageDemandRateForecast2)
            => !(DDBCAverageDemandRateForecast1 == DDBCAverageDemandRateForecast2);

        #endregion

        #region IEquatable<DDBC_AverageDemandRateForecast> Members

        /// <summary>
        /// Compares two DDBC average demand rate forecasts for equality.
        /// </summary>
        /// <param name="Object">A DDBC average demand rate forecast to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is DDBC_AverageDemandRateForecast ddbcAverageDemandRateForecast && Equals(ddbcAverageDemandRateForecast);

        /// <summary>
        /// Compares two DDBC average demand rate forecasts for equality.
        /// </summary>
        /// <param name="DDBCAverageDemandRateForecast">A DDBC average demand rate forecast to compare with.</param>
        public Boolean Equals(DDBC_AverageDemandRateForecast? DDBCAverageDemandRateForecast)

            => DDBCAverageDemandRateForecast is not null &&

               MessageId.Equals       (DDBCAverageDemandRateForecast.MessageId) &&
               StartTime.Equals       (DDBCAverageDemandRateForecast.StartTime) &&
               Elements. SequenceEqual(DDBCAverageDemandRateForecast.Elements);

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
            => $"DDBC.AverageDemandRateForecast starting at {StartTime.ToS2Timestamp()} with {Elements.Count} element(s) [{MessageId}]";

        #endregion

    }

}

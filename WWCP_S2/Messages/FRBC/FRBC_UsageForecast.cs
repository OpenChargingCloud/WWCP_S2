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
    /// The Resource Manager forecasts the usage of the storage, i.e. how fast the fill
    /// level is expected to decrease due to the demand of the user (e.g. hot water taps).
    /// </summary>
    public sealed class FRBC_UsageForecast : AS2Message,
                                             IEquatable<FRBC_UsageForecast>
    {

        #region Data

        /// <summary>
        /// The message type "FRBC.UsageForecast".
        /// </summary>
        public const String MessageTypeName = "FRBC.UsageForecast";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "FRBC.UsageForecast".
        /// </summary>
        public override String                          MessageType
            => MessageTypeName;

        /// <summary>
        /// The time at which this usage forecast starts.
        /// </summary>
        [Mandatory]
        public DateTimeOffset                           StartTime    { get; }

        /// <summary>
        /// The elements that model the usage forecast. There shall be at least one element.
        /// Elements are placed in chronological order.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<FRBC_UsageForecastElement>  Elements     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new FRBC usage forecast.
        /// </summary>
        /// <param name="StartTime">The time at which this usage forecast starts.</param>
        /// <param name="Elements">The elements modelling the usage forecast, in chronological order (1..288 entries).</param>
        /// <param name="MessageId">An optional message identification.</param>
        public FRBC_UsageForecast(DateTimeOffset                           StartTime,
                                  IReadOnlyList<FRBC_UsageForecastElement>  Elements,
                                  Message_Id?                              MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(Elements);

            if (Elements.Count < 1)
                throw new ArgumentException("An FRBC usage forecast must contain at least one element!",
                                            nameof(Elements));

            if (Elements.Count > 288)
                throw new ArgumentException($"An FRBC usage forecast must not contain more than 288 elements, but {Elements.Count} were given!",
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

        // FRBC.UsageForecast.schema.json
        //   "title": "FRBC_UsageForecast",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "FRBC.UsageForecast" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "start_time":   { "type": "string", "format": "date-time",
        //                       "description": "Time at which the FRBC.UsageForecast starts." },
        //     "elements":     { "type": "array", "minItems": 1, "maxItems": 288,
        //                       "items": { "$ref": "../schemas/FRBC.UsageForecastElement.schema.json" },
        //                       "description": "Further elements that model the profile. There shall be at least one element.
        //                                       Elements must be placed in chronological order." }
        //   },
        //   "required": ["message_type", "message_id", "start_time", "elements"],
        //   "additionalProperties": false
        //
        // The chronological order of the elements is implied by their durations: every element starts
        // when the previous element ends; the first element starts at start_time.

        #endregion

        #region (static) TryParse(JSON, out FRBC_UsageForecast, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an FRBC usage forecast.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="UsageForecast">The parsed FRBC usage forecast.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out FRBC_UsageForecast?  UsageForecast,
                                       [NotNullWhen(false)] out String?              ErrorResponse)

            => TryParse(JSON,
                        out UsageForecast,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC usage forecast.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="UsageForecast">The parsed FRBC usage forecast.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out FRBC_UsageForecast?  UsageForecast,
                                       [NotNullWhen(false)] out String?              ErrorResponse,
                                       S2ParserOptions?                              Options)

            => TryParse(JSON,
                        out UsageForecast,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC usage forecast.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="UsageForecast">The parsed FRBC usage forecast.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomUsageForecastParser">A delegate to parse custom FRBC usage forecasts.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out FRBC_UsageForecast?      UsageForecast,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options,
                                       CustomJObjectParserDelegate<FRBC_UsageForecast>?  CustomUsageForecastParser)
        {

            try
            {

                UsageForecast = null;

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
                                               "usage forecast elements",
                                               FRBC_UsageForecastElement.TryParse,
                                               Options,
                                               1,
                                               288,
                                               out IReadOnlyList<FRBC_UsageForecastElement>? elements,
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


                UsageForecast = new FRBC_UsageForecast(
                                    startTime,
                                    elements,
                                    messageId
                                );

                if (CustomUsageForecastParser is not null)
                    UsageForecast = CustomUsageForecastParser(JSON,
                                                              UsageForecast);

                return true;

            }
            catch (Exception e)
            {
                UsageForecast  = null;
                ErrorResponse  = "The given JSON representation of an FRBC usage forecast is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomUsageForecastSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomUsageForecastSerializer">A delegate to serialize custom FRBC usage forecasts.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_UsageForecast>? CustomUsageForecastSerializer)
        {

            var json = CreateJSON(
                           new JProperty("start_time",  StartTime.ToS2Timestamp()),
                           new JProperty("elements",    new JArray(Elements.Select(element => element.ToJSON())))
                       );

            return CustomUsageForecastSerializer is not null
                       ? CustomUsageForecastSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this FRBC usage forecast.
        /// </summary>
        public FRBC_UsageForecast Clone()

            => new (
                   StartTime,
                   Elements. Select(element => element.Clone()).ToList(),
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two FRBC usage forecasts for equality.
        /// </summary>
        public static Boolean operator == (FRBC_UsageForecast? UsageForecast1, FRBC_UsageForecast? UsageForecast2)
        {

            if (ReferenceEquals(UsageForecast1, UsageForecast2))
                return true;

            if (UsageForecast1 is null || UsageForecast2 is null)
                return false;

            return UsageForecast1.Equals(UsageForecast2);

        }

        /// <summary>
        /// Compares two FRBC usage forecasts for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_UsageForecast? UsageForecast1, FRBC_UsageForecast? UsageForecast2)
            => !(UsageForecast1 == UsageForecast2);

        #endregion

        #region IEquatable<FRBC_UsageForecast> Members

        /// <summary>
        /// Compares two FRBC usage forecasts for equality.
        /// </summary>
        /// <param name="Object">An FRBC usage forecast to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_UsageForecast usageForecast && Equals(usageForecast);

        /// <summary>
        /// Compares two FRBC usage forecasts for equality.
        /// </summary>
        /// <param name="UsageForecast">An FRBC usage forecast to compare with.</param>
        public Boolean Equals(FRBC_UsageForecast? UsageForecast)

            => UsageForecast is not null &&

               MessageId.Equals(UsageForecast.MessageId) &&
               StartTime.Equals(UsageForecast.StartTime) &&
               Elements. SequenceEqual(UsageForecast.Elements);

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
            => $"FRBC.UsageForecast starting at {StartTime.ToS2Timestamp()} with {Elements.Count} element(s) [{MessageId}]";

        #endregion

    }

}

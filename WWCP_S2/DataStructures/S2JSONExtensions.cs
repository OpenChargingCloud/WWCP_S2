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

using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// A delegate to parse the JSON representation of an S2 data structure with parser options.
    /// Every S2 data structure exposes a <c>TryParse</c> overload with exactly this shape.
    /// </summary>
    /// <typeparam name="T">The type of the data structure.</typeparam>
    /// <param name="JSON">The JSON object to be parsed.</param>
    /// <param name="Value">The parsed data structure.</param>
    /// <param name="ErrorResponse">An error message when parsing failed.</param>
    /// <param name="Options">Optional parser options; null means <see cref="S2ParserOptions.Default"/>.</param>
    public delegate Boolean S2TryParser<T>(JObject                           JSON,
                                           [NotNullWhen(true)]  out T?       Value,
                                           [NotNullWhen(false)] out String?  ErrorResponse,
                                           S2ParserOptions?                  Options);


    /// <summary>
    /// JSON helpers for S2 data structures: parsing of S2 JSON text without lossy date
    /// conversion, RFC 3339 timestamps, durations, options-aware parsing of nested objects,
    /// arrays, identifiers and enumerations, and the additional-properties check.
    /// </summary>
    public static partial class S2JSONExtensions
    {

        #region Data

        private static readonly Regex timestampOffsetRegExpr = TimestampOffsetRegExpr();

        [GeneratedRegex(@"(Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex TimestampOffsetRegExpr();

        // RFC 3339 date-time: full date, 'T', full time with optional fractional seconds, 'Z' or a numeric offset.
        // Naive values (no offset) are accepted by the last two formats and interpreted as UTC unless rejected by the options.
        private static readonly String[] timestampFormats = [
            "yyyy-MM-dd'T'HH:mm:ssK",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"
        ];

        #endregion


        #region S2JSON.Parse / TryParse (Text)

        /// <summary>
        /// Parse the given S2 JSON text into a JSON object. Date-time strings are kept as
        /// strings so that the original UTC offset survives (Newtonsoft.Json would otherwise
        /// convert them into DateTime values).
        /// </summary>
        /// <param name="Text">An S2 JSON text.</param>
        public static JObject ParseS2JSON(String Text)
        {

            using var reader = new JsonTextReader(new StringReader(Text)) {
                                   DateParseHandling   = DateParseHandling.None,
                                   FloatParseHandling  = FloatParseHandling.Double
                               };

            var json = JObject.Load(reader);

            if (reader.Read())
                throw new JsonReaderException("Additional content after the JSON object!");

            return json;

        }

        /// <summary>
        /// Try to parse the given S2 JSON text into a JSON object.
        /// </summary>
        /// <param name="Text">An S2 JSON text.</param>
        /// <param name="JSON">The parsed JSON object.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParseS2JSON(String                             Text,
                                             [NotNullWhen(true)]  out JObject?  JSON,
                                             [NotNullWhen(false)] out String?   ErrorResponse)
        {

            try
            {
                JSON           = ParseS2JSON(Text);
                ErrorResponse  = null;
                return true;
            }
            catch (Exception e)
            {
                JSON           = null;
                ErrorResponse  = "Invalid JSON: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToS2Timestamp (this DateTimeOffset)

        /// <summary>
        /// Format the given timestamp as RFC 3339 date-time as used in S2 JSON:
        /// "2019-08-24T14:15:22Z" for UTC, "2019-08-24T16:15:22+02:00" otherwise;
        /// fractional seconds are only written when they are not zero and with at most six
        /// digits (microseconds), which is what s2-python and most peers parse; the seventh
        /// digit of .NET ticks is truncated.
        /// </summary>
        /// <param name="Timestamp">A timestamp.</param>
        public static String ToS2Timestamp(this DateTimeOffset Timestamp)
        {

            var fractions  = Timestamp.Ticks % TimeSpan.TicksPerSecond != 0;
            var format     = fractions ? "yyyy-MM-dd'T'HH:mm:ss.FFFFFF" : "yyyy-MM-dd'T'HH:mm:ss";

            return Timestamp.Offset == TimeSpan.Zero
                       ? Timestamp.ToString(format, CultureInfo.InvariantCulture) + "Z"
                       : Timestamp.ToString(format + "zzz", CultureInfo.InvariantCulture);

        }

        #endregion

        #region TryParseS2Timestamp (Text, Options, out Timestamp, out ErrorResponse)

        /// <summary>
        /// Try to parse the given RFC 3339 date-time text. A value without an explicit
        /// offset is interpreted as UTC unless the options reject naive timestamps.
        /// </summary>
        /// <param name="Text">A date-time text.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="Timestamp">The parsed timestamp.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParseS2Timestamp(String                            Text,
                                                  S2ParserOptions?                  Options,
                                                  out DateTimeOffset                Timestamp,
                                                  [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (!timestampOffsetRegExpr.IsMatch(Text) && (Options ?? S2ParserOptions.Default).RejectNaiveTimestamps)
            {
                Timestamp      = default;
                ErrorResponse  = $"The timestamp '{Text}' has no UTC offset!";
                return false;
            }

            // RFC 3339 allows lower-case 't' and 'z'; the .NET format strings do not.
            var normalised = Text.Replace('t', 'T').Replace('z', 'Z');

            if (DateTimeOffset.TryParseExact(normalised,
                                             timestampFormats,
                                             CultureInfo.InvariantCulture,
                                             DateTimeStyles.AssumeUniversal,
                                             out Timestamp))
            {
                ErrorResponse = null;
                return true;
            }

            ErrorResponse = $"Invalid RFC 3339 timestamp '{Text}'!";
            return false;

        }

        #endregion


        #region ParseMandatoryS2Timestamp / ParseOptionalS2Timestamp

        /// <summary>
        /// Parse the mandatory RFC 3339 date-time property with the given name.
        /// </summary>
        public static Boolean ParseMandatoryS2Timestamp(this JObject                      JSON,
                                                        String                            PropertyName,
                                                        String                            PropertyDescription,
                                                        S2ParserOptions?                  Options,
                                                        out DateTimeOffset                Timestamp,
                                                        [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Timestamp = default;

            if (!TryGetMandatoryToken(JSON, PropertyName, PropertyDescription, out var token, out ErrorResponse))
                return false;

            return TryParseTimestampToken(token, PropertyName, PropertyDescription, Options, out Timestamp, out ErrorResponse);

        }

        /// <summary>
        /// Parse the optional RFC 3339 date-time property with the given name.
        /// Returns false only when the property is present but invalid.
        /// </summary>
        public static Boolean ParseOptionalS2Timestamp(this JObject                      JSON,
                                                       String                            PropertyName,
                                                       String                            PropertyDescription,
                                                       S2ParserOptions?                  Options,
                                                       out DateTimeOffset?               Timestamp,
                                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Timestamp = null;

            if (!TryGetOptionalToken(JSON, PropertyName, out var token))
            {
                ErrorResponse = null;
                return true;
            }

            if (!TryParseTimestampToken(token, PropertyName, PropertyDescription, Options, out var timestamp, out ErrorResponse))
                return false;

            Timestamp = timestamp;
            return true;

        }

        private static Boolean TryParseTimestampToken(JToken                            Token,
                                                      String                            PropertyName,
                                                      String                            PropertyDescription,
                                                      S2ParserOptions?                  Options,
                                                      out DateTimeOffset                Timestamp,
                                                      [NotNullWhen(false)] out String?  ErrorResponse)
        {

            switch (Token.Type)
            {

                case JTokenType.String:
                    if (TryParseS2Timestamp(Token.Value<String>() ?? "", Options, out Timestamp, out var error))
                    {
                        ErrorResponse = null;
                        return true;
                    }
                    ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': {error}";
                    return false;

                // Newtonsoft.Json already converted the text (JObject.Parse instead of ParseS2JSON was used).
                // A DateTime of kind Unspecified stems from a text without a UTC offset ("naive" timestamp).
                case JTokenType.Date:

                    if (Token is JValue { Value: DateTimeOffset dto })
                    {
                        Timestamp      = dto;
                        ErrorResponse  = null;
                        return true;
                    }

                    var dateTime = Token.Value<DateTime>();

                    if (dateTime.Kind == DateTimeKind.Unspecified && (Options ?? S2ParserOptions.Default).RejectNaiveTimestamps)
                    {
                        Timestamp      = default;
                        ErrorResponse  = $"Invalid {PropertyDescription} '{PropertyName}': The timestamp '{dateTime:yyyy-MM-dd'T'HH:mm:ss}' has no UTC offset!";
                        return false;
                    }

                    Timestamp      = dateTime.Kind == DateTimeKind.Unspecified
                                         ? new DateTimeOffset(dateTime, TimeSpan.Zero)
                                         : new DateTimeOffset(dateTime.ToUniversalTime());
                    ErrorResponse  = null;
                    return true;

                default:
                    Timestamp      = default;
                    ErrorResponse  = $"Invalid {PropertyDescription} '{PropertyName}': a date-time string is expected!";
                    return false;

            }

        }

        #endregion

        #region ParseMandatoryS2Duration  / ParseOptionalS2Duration

        /// <summary>
        /// Parse the mandatory duration property (integer milliseconds) with the given name.
        /// </summary>
        public static Boolean ParseMandatoryS2Duration(this JObject                      JSON,
                                                       String                            PropertyName,
                                                       String                            PropertyDescription,
                                                       out Duration                      Duration,
                                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Duration = default;

            if (!TryGetMandatoryToken(JSON, PropertyName, PropertyDescription, out var token, out ErrorResponse))
                return false;

            if (Duration.TryParse(token, out Duration, out var error))
            {
                ErrorResponse = null;
                return true;
            }

            ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': {error}";
            return false;

        }

        /// <summary>
        /// Parse the optional duration property (integer milliseconds) with the given name.
        /// Returns false only when the property is present but invalid.
        /// </summary>
        public static Boolean ParseOptionalS2Duration(this JObject                      JSON,
                                                      String                            PropertyName,
                                                      String                            PropertyDescription,
                                                      out Duration?                     Duration,
                                                      [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Duration = null;

            if (!TryGetOptionalToken(JSON, PropertyName, out var token))
            {
                ErrorResponse = null;
                return true;
            }

            if (S2.Duration.TryParse(token, out var duration, out var error))
            {
                Duration       = duration;
                ErrorResponse  = null;
                return true;
            }

            ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': {error}";
            return false;

        }

        #endregion

        #region ParseMandatoryS2Number    / ParseOptionalS2Number

        /// <summary>
        /// Parse the mandatory JSON number property with the given name.
        /// </summary>
        public static Boolean ParseMandatoryS2Number(this JObject                      JSON,
                                                     String                            PropertyName,
                                                     String                            PropertyDescription,
                                                     out Double                        Number,
                                                     [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Number = default;

            if (!TryGetMandatoryToken(JSON, PropertyName, PropertyDescription, out var token, out ErrorResponse))
                return false;

            return TryParseNumberToken(token, PropertyName, PropertyDescription, out Number, out ErrorResponse);

        }

        /// <summary>
        /// Parse the optional JSON number property with the given name.
        /// Returns false only when the property is present but invalid.
        /// </summary>
        public static Boolean ParseOptionalS2Number(this JObject                      JSON,
                                                    String                            PropertyName,
                                                    String                            PropertyDescription,
                                                    out Double?                       Number,
                                                    [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Number = null;

            if (!TryGetOptionalToken(JSON, PropertyName, out var token))
            {
                ErrorResponse = null;
                return true;
            }

            if (!TryParseNumberToken(token, PropertyName, PropertyDescription, out var number, out ErrorResponse))
                return false;

            Number = number;
            return true;

        }

        private static Boolean TryParseNumberToken(JToken                            Token,
                                                   String                            PropertyName,
                                                   String                            PropertyDescription,
                                                   out Double                        Number,
                                                   [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Number = default;

            if (Token.Type != JTokenType.Integer && Token.Type != JTokenType.Float)
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': a number is expected!";
                return false;
            }

            try
            {
                Number = Token.Value<Double>();
            }
            catch (Exception)
            {
                // e.g. an integer beyond the range of Int64/Double
                ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': the number is out of range!";
                return false;
            }

            // Newtonsoft.Json accepts the non-standard literals NaN and Infinity; the schema type "number" does not.
            if (!Double.IsFinite(Number))
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': a finite number is expected!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region ParseMandatoryS2Boolean   / ParseOptionalS2Boolean

        /// <summary>
        /// Parse the mandatory JSON boolean property with the given name.
        /// </summary>
        public static Boolean ParseMandatoryS2Boolean(this JObject                      JSON,
                                                      String                            PropertyName,
                                                      String                            PropertyDescription,
                                                      out Boolean                       Value,
                                                      [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = default;

            if (!TryGetMandatoryToken(JSON, PropertyName, PropertyDescription, out var token, out ErrorResponse))
                return false;

            if (token.Type == JTokenType.Boolean)
            {
                Value          = token.Value<Boolean>();
                ErrorResponse  = null;
                return true;
            }

            ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': a boolean is expected!";
            return false;

        }

        /// <summary>
        /// Parse the optional JSON boolean property with the given name.
        /// Returns false only when the property is present but invalid.
        /// </summary>
        public static Boolean ParseOptionalS2Boolean(this JObject                      JSON,
                                                     String                            PropertyName,
                                                     String                            PropertyDescription,
                                                     out Boolean?                      Value,
                                                     [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = null;

            if (!TryGetOptionalToken(JSON, PropertyName, out var token))
            {
                ErrorResponse = null;
                return true;
            }

            if (token.Type == JTokenType.Boolean)
            {
                Value          = token.Value<Boolean>();
                ErrorResponse  = null;
                return true;
            }

            ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': a boolean is expected!";
            return false;

        }

        #endregion

        #region ParseMandatoryS2String    / ParseOptionalS2String

        /// <summary>
        /// Parse the mandatory JSON string property with the given name.
        /// </summary>
        public static Boolean ParseMandatoryS2String(this JObject                      JSON,
                                                     String                            PropertyName,
                                                     String                            PropertyDescription,
                                                     [NotNullWhen(true)]  out String?  Value,
                                                     [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = null;

            if (!TryGetMandatoryToken(JSON, PropertyName, PropertyDescription, out var token, out ErrorResponse))
                return false;

            if (token.Type == JTokenType.String)
            {
                Value          = token.Value<String>() ?? "";
                ErrorResponse  = null;
                return true;
            }

            ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': a string is expected!";
            return false;

        }

        /// <summary>
        /// Parse the optional JSON string property with the given name.
        /// Returns false only when the property is present but invalid.
        /// </summary>
        public static Boolean ParseOptionalS2String(this JObject                      JSON,
                                                    String                            PropertyName,
                                                    String                            PropertyDescription,
                                                    out String?                       Value,
                                                    [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = null;

            if (!TryGetOptionalToken(JSON, PropertyName, out var token))
            {
                ErrorResponse = null;
                return true;
            }

            if (token.Type == JTokenType.String)
            {
                Value          = token.Value<String>() ?? "";
                ErrorResponse  = null;
                return true;
            }

            ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': a string is expected!";
            return false;

        }

        /// <summary>
        /// Parse the mandatory JSON string array property with the given name.
        /// </summary>
        public static Boolean ParseMandatoryS2Strings(this JObject                                JSON,
                                                      String                                      PropertyName,
                                                      String                                      PropertyDescription,
                                                      UInt32?                                     MinItems,
                                                      UInt32?                                     MaxItems,
                                                      [NotNullWhen(true)]  out IReadOnlyList<String>?  Values,
                                                      [NotNullWhen(false)] out String?            ErrorResponse)
        {

            Values = null;

            if (!TryGetMandatoryArray(JSON, PropertyName, PropertyDescription, MinItems, MaxItems, out var array, out ErrorResponse))
                return false;

            var values = new List<String>(array.Count);

            foreach (var item in array)
            {

                if (item.Type != JTokenType.String)
                {
                    ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': a string array is expected!";
                    return false;
                }

                values.Add(item.Value<String>() ?? "");

            }

            Values         = values;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region ParseMandatoryS2Id        / ParseOptionalS2Id / ParseMandatoryS2Ids

        /// <summary>
        /// Parse the mandatory identifier property with the given name.
        /// When the options require UUIDs, a non-UUID identifier is an error.
        /// </summary>
        public static Boolean ParseMandatoryS2Id<T>(this JObject                      JSON,
                                                    String                            PropertyName,
                                                    String                            PropertyDescription,
                                                    TryParser<T>                      TryParser,
                                                    S2ParserOptions?                  Options,
                                                    out T                             Id,
                                                    [NotNullWhen(false)] out String?  ErrorResponse)

            where T : struct, IId

        {

            Id = default;

            if (!TryGetMandatoryToken(JSON, PropertyName, PropertyDescription, out var token, out ErrorResponse))
                return false;

            return TryParseIdToken(token, PropertyName, PropertyDescription, TryParser, Options, out Id, out ErrorResponse);

        }

        /// <summary>
        /// Parse the optional identifier property with the given name.
        /// Returns false only when the property is present but invalid.
        /// </summary>
        public static Boolean ParseOptionalS2Id<T>(this JObject                      JSON,
                                                   String                            PropertyName,
                                                   String                            PropertyDescription,
                                                   TryParser<T>                      TryParser,
                                                   S2ParserOptions?                  Options,
                                                   out T?                            Id,
                                                   [NotNullWhen(false)] out String?  ErrorResponse)

            where T : struct, IId

        {

            Id = null;

            if (!TryGetOptionalToken(JSON, PropertyName, out var token))
            {
                ErrorResponse = null;
                return true;
            }

            if (!TryParseIdToken(token, PropertyName, PropertyDescription, TryParser, Options, out var id, out ErrorResponse))
                return false;

            Id = id;
            return true;

        }

        /// <summary>
        /// Parse the mandatory identifier array property with the given name.
        /// </summary>
        public static Boolean ParseMandatoryS2Ids<T>(this JObject                                 JSON,
                                                     String                                       PropertyName,
                                                     String                                       PropertyDescription,
                                                     TryParser<T>                                 TryParser,
                                                     S2ParserOptions?                             Options,
                                                     UInt32?                                      MinItems,
                                                     UInt32?                                      MaxItems,
                                                     [NotNullWhen(true)]  out IReadOnlyList<T>?   Ids,
                                                     [NotNullWhen(false)] out String?             ErrorResponse)

            where T : struct, IId

        {

            Ids = null;

            if (!TryGetMandatoryArray(JSON, PropertyName, PropertyDescription, MinItems, MaxItems, out var array, out ErrorResponse))
                return false;

            var ids = new List<T>(array.Count);

            foreach (var item in array)
            {

                if (!TryParseIdToken(item, PropertyName, PropertyDescription, TryParser, Options, out var id, out ErrorResponse))
                    return false;

                ids.Add(id);

            }

            Ids            = ids;
            ErrorResponse  = null;
            return true;

        }

        private static Boolean TryParseIdToken<T>(JToken                            Token,
                                                  String                            PropertyName,
                                                  String                            PropertyDescription,
                                                  TryParser<T>                      TryParser,
                                                  S2ParserOptions?                  Options,
                                                  out T                             Id,
                                                  [NotNullWhen(false)] out String?  ErrorResponse)

            where T : struct, IId

        {

            Id = default;

            if (Token.Type != JTokenType.String)
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': a string is expected!";
                return false;
            }

            var text = Token.Value<String>() ?? "";

            if ((Options ?? S2ParserOptions.Default).RequireUUIDs && !Guid.TryParseExact(text, "D", out _))
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': '{text}' is not a UUID!";
                return false;
            }

            if (TryParser(text, out var id))
            {
                Id             = id;
                ErrorResponse  = null;
                return true;
            }

            ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': '{text}'!";
            return false;

        }

        #endregion

        #region ParseMandatoryS2Enum      / ParseOptionalS2Enum / ParseMandatoryS2Enums

        /// <summary>
        /// Parse the mandatory enumeration property with the given name.
        /// When the options reject unknown enumeration values, a value not defined by the
        /// S2 JSON schemas is an error.
        /// </summary>
        public static Boolean ParseMandatoryS2Enum<T>(this JObject                      JSON,
                                                      String                            PropertyName,
                                                      String                            PropertyDescription,
                                                      TryParser<T>                      TryParser,
                                                      S2ParserOptions?                  Options,
                                                      out T                             Value,
                                                      [NotNullWhen(false)] out String?  ErrorResponse)

            where T : struct, IS2PredefinedString

        {

            Value = default;

            if (!TryGetMandatoryToken(JSON, PropertyName, PropertyDescription, out var token, out ErrorResponse))
                return false;

            return TryParseEnumToken(token, PropertyName, PropertyDescription, TryParser, Options, out Value, out ErrorResponse);

        }

        /// <summary>
        /// Parse the optional enumeration property with the given name.
        /// Returns false only when the property is present but invalid.
        /// </summary>
        public static Boolean ParseOptionalS2Enum<T>(this JObject                      JSON,
                                                     String                            PropertyName,
                                                     String                            PropertyDescription,
                                                     TryParser<T>                      TryParser,
                                                     S2ParserOptions?                  Options,
                                                     out T?                            Value,
                                                     [NotNullWhen(false)] out String?  ErrorResponse)

            where T : struct, IS2PredefinedString

        {

            Value = null;

            if (!TryGetOptionalToken(JSON, PropertyName, out var token))
            {
                ErrorResponse = null;
                return true;
            }

            if (!TryParseEnumToken(token, PropertyName, PropertyDescription, TryParser, Options, out var value, out ErrorResponse))
                return false;

            Value = value;
            return true;

        }

        /// <summary>
        /// Parse the mandatory enumeration array property with the given name.
        /// </summary>
        public static Boolean ParseMandatoryS2Enums<T>(this JObject                                 JSON,
                                                       String                                       PropertyName,
                                                       String                                       PropertyDescription,
                                                       TryParser<T>                                 TryParser,
                                                       S2ParserOptions?                             Options,
                                                       UInt32?                                      MinItems,
                                                       UInt32?                                      MaxItems,
                                                       [NotNullWhen(true)]  out IReadOnlyList<T>?   Values,
                                                       [NotNullWhen(false)] out String?             ErrorResponse)

            where T : struct, IS2PredefinedString

        {

            Values = null;

            if (!TryGetMandatoryArray(JSON, PropertyName, PropertyDescription, MinItems, MaxItems, out var array, out ErrorResponse))
                return false;

            var values = new List<T>(array.Count);

            foreach (var item in array)
            {

                if (!TryParseEnumToken(item, PropertyName, PropertyDescription, TryParser, Options, out var value, out ErrorResponse))
                    return false;

                values.Add(value);

            }

            Values         = values;
            ErrorResponse  = null;
            return true;

        }

        private static Boolean TryParseEnumToken<T>(JToken                            Token,
                                                    String                            PropertyName,
                                                    String                            PropertyDescription,
                                                    TryParser<T>                      TryParser,
                                                    S2ParserOptions?                  Options,
                                                    out T                             Value,
                                                    [NotNullWhen(false)] out String?  ErrorResponse)

            where T : struct, IS2PredefinedString

        {

            Value = default;

            if (Token.Type != JTokenType.String)
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': a string is expected!";
                return false;
            }

            var text = Token.Value<String>() ?? "";

            if (!TryParser(text, out var value))
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': '{text}'!";
                return false;
            }

            if (!value.IsKnown && (Options ?? S2ParserOptions.Default).RejectUnknownEnumValues)
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': '{text}' is not a known value!";
                return false;
            }

            Value          = value;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region ParseMandatoryS2          / ParseOptionalS2 (nested objects)

        /// <summary>
        /// Parse the mandatory nested S2 data structure with the given property name.
        /// </summary>
        public static Boolean ParseMandatoryS2<T>(this JObject                      JSON,
                                                  String                            PropertyName,
                                                  String                            PropertyDescription,
                                                  S2TryParser<T>                    TryParser,
                                                  S2ParserOptions?                  Options,
                                                  [NotNullWhen(true)]  out T?       Value,
                                                  [NotNullWhen(false)] out String?  ErrorResponse)

            where T : class

        {

            Value = null;

            if (!TryGetMandatoryToken(JSON, PropertyName, PropertyDescription, out var token, out ErrorResponse))
                return false;

            if (token is not JObject json)
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': a JSON object is expected!";
                return false;
            }

            if (TryParser(json, out Value, out var error, Options))
            {
                ErrorResponse = null;
                return true;
            }

            ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': {error}";
            return false;

        }

        /// <summary>
        /// Parse the optional nested S2 data structure with the given property name.
        /// Returns false only when the property is present but invalid.
        /// </summary>
        public static Boolean ParseOptionalS2<T>(this JObject                      JSON,
                                                 String                            PropertyName,
                                                 String                            PropertyDescription,
                                                 S2TryParser<T>                    TryParser,
                                                 S2ParserOptions?                  Options,
                                                 out T?                            Value,
                                                 [NotNullWhen(false)] out String?  ErrorResponse)

            where T : class

        {

            Value = null;

            if (!TryGetOptionalToken(JSON, PropertyName, out var token))
            {
                ErrorResponse = null;
                return true;
            }

            if (token is not JObject json)
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': a JSON object is expected!";
                return false;
            }

            if (TryParser(json, out Value, out var error, Options))
            {
                ErrorResponse = null;
                return true;
            }

            ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': {error}";
            return false;

        }

        #endregion

        #region ParseMandatoryS2List      / ParseOptionalS2List (arrays of nested objects)

        /// <summary>
        /// Parse the mandatory array of nested S2 data structures with the given property name,
        /// preserving the order of the array.
        /// </summary>
        public static Boolean ParseMandatoryS2List<T>(this JObject                                 JSON,
                                                      String                                       PropertyName,
                                                      String                                       PropertyDescription,
                                                      S2TryParser<T>                               TryParser,
                                                      S2ParserOptions?                             Options,
                                                      UInt32?                                      MinItems,
                                                      UInt32?                                      MaxItems,
                                                      [NotNullWhen(true)]  out IReadOnlyList<T>?   Values,
                                                      [NotNullWhen(false)] out String?             ErrorResponse)

            where T : class

        {

            Values = null;

            if (!TryGetMandatoryArray(JSON, PropertyName, PropertyDescription, MinItems, MaxItems, out var array, out ErrorResponse))
                return false;

            return TryParseListItems(array, PropertyName, PropertyDescription, TryParser, Options, out Values, out ErrorResponse);

        }

        /// <summary>
        /// Parse the optional array of nested S2 data structures with the given property name,
        /// preserving the order of the array. Returns false only when the property is present but invalid.
        /// </summary>
        public static Boolean ParseOptionalS2List<T>(this JObject                      JSON,
                                                     String                            PropertyName,
                                                     String                            PropertyDescription,
                                                     S2TryParser<T>                    TryParser,
                                                     S2ParserOptions?                  Options,
                                                     UInt32?                           MinItems,
                                                     UInt32?                           MaxItems,
                                                     out IReadOnlyList<T>?             Values,
                                                     [NotNullWhen(false)] out String?  ErrorResponse)

            where T : class

        {

            Values = null;

            if (!TryGetOptionalToken(JSON, PropertyName, out var token))
            {
                ErrorResponse = null;
                return true;
            }

            if (!CheckArray(token, PropertyName, PropertyDescription, MinItems, MaxItems, out var array, out ErrorResponse))
                return false;

            return TryParseListItems(array, PropertyName, PropertyDescription, TryParser, Options, out Values, out ErrorResponse);

        }

        private static Boolean TryParseListItems<T>(JArray                                       Array,
                                                    String                                       PropertyName,
                                                    String                                       PropertyDescription,
                                                    S2TryParser<T>                               TryParser,
                                                    S2ParserOptions?                             Options,
                                                    [NotNullWhen(true)]  out IReadOnlyList<T>?   Values,
                                                    [NotNullWhen(false)] out String?             ErrorResponse)

            where T : class

        {

            Values = null;

            var values = new List<T>(Array.Count);
            var index  = 0;

            foreach (var item in Array)
            {

                if (item is not JObject json)
                {
                    ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': item {index} is not a JSON object!";
                    return false;
                }

                if (!TryParser(json, out var value, out var error, Options))
                {
                    ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': item {index}: {error}";
                    return false;
                }

                values.Add(value);
                index++;

            }

            Values         = values;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region ParseMandatoryS2URL       / ParseOptionalS2URL

        /// <summary>
        /// Parse the mandatory URL property ("format": "uri") with the given name.
        /// </summary>
        public static Boolean ParseMandatoryS2URL(this JObject                      JSON,
                                                  String                            PropertyName,
                                                  String                            PropertyDescription,
                                                  out URL                           URL,
                                                  [NotNullWhen(false)] out String?  ErrorResponse)
        {

            URL = default;

            if (!TryGetMandatoryToken(JSON, PropertyName, PropertyDescription, out var token, out ErrorResponse))
                return false;

            return TryParseURLToken(token, PropertyName, PropertyDescription, out URL, out ErrorResponse);

        }

        /// <summary>
        /// Parse the optional URL property ("format": "uri") with the given name.
        /// Returns false only when the property is present but invalid.
        /// </summary>
        public static Boolean ParseOptionalS2URL(this JObject                      JSON,
                                                 String                            PropertyName,
                                                 String                            PropertyDescription,
                                                 out URL?                          URL,
                                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            URL = null;

            if (!TryGetOptionalToken(JSON, PropertyName, out var token))
            {
                ErrorResponse = null;
                return true;
            }

            if (!TryParseURLToken(token, PropertyName, PropertyDescription, out var url, out ErrorResponse))
                return false;

            URL = url;
            return true;

        }

        private static Boolean TryParseURLToken(JToken                            Token,
                                                String                            PropertyName,
                                                String                            PropertyDescription,
                                                out URL                           URL,
                                                [NotNullWhen(false)] out String?  ErrorResponse)
        {

            URL = default;

            if (Token.Type != JTokenType.String)
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': a string is expected!";
                return false;
            }

            var text = Token.Value<String>() ?? "";

            if (text.Length == 0 || !org.GraphDefined.Vanaheimr.Hermod.HTTP.URL.TryParse(text, out URL))
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{PropertyName}': '{text}' is not a valid URL!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region CheckAdditionalProperties (this JSON, Options, out ErrorResponse, KnownProperties)

        /// <summary>
        /// Check that the JSON object contains no property other than the known ones, when
        /// the options reject additional properties ("additionalProperties": false in every
        /// S2 JSON schema).
        /// </summary>
        /// <param name="JSON">A JSON object.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="ErrorResponse">An error message when an unknown property is present.</param>
        /// <param name="KnownProperties">The property names defined by the schema.</param>
        public static Boolean CheckAdditionalProperties(this JObject                      JSON,
                                                        S2ParserOptions?                  Options,
                                                        [NotNullWhen(false)] out String?  ErrorResponse,
                                                        params ReadOnlySpan<String>       KnownProperties)
        {

            ErrorResponse = null;

            if (!(Options ?? S2ParserOptions.Default).RejectAdditionalProperties)
                return true;

            foreach (var property in JSON.Properties())
            {

                if (KnownProperties.IndexOf(property.Name) < 0)
                {
                    ErrorResponse = $"Unexpected property '{property.Name}'!";
                    return false;
                }

            }

            return true;

        }

        #endregion


        #region (private) Token helpers

        private static Boolean TryGetMandatoryToken(JObject                           JSON,
                                                    String                            PropertyName,
                                                    String                            PropertyDescription,
                                                    [NotNullWhen(true)]  out JToken?  Token,
                                                    [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (!JSON.TryGetValue(PropertyName, StringComparison.Ordinal, out Token) ||
                Token is null ||
                Token.Type == JTokenType.Null)
            {
                Token          = null;
                ErrorResponse  = $"The {PropertyDescription} '{PropertyName}' is missing!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        private static Boolean TryGetOptionalToken(JObject                           JSON,
                                                   String                            PropertyName,
                                                   [NotNullWhen(true)]  out JToken?  Token)
        {

            if (!JSON.TryGetValue(PropertyName, StringComparison.Ordinal, out Token) ||
                Token is null ||
                Token.Type == JTokenType.Null)
            {
                Token = null;
                return false;
            }

            return true;

        }

        private static Boolean TryGetMandatoryArray(JObject                           JSON,
                                                    String                            PropertyName,
                                                    String                            PropertyDescription,
                                                    UInt32?                           MinItems,
                                                    UInt32?                           MaxItems,
                                                    [NotNullWhen(true)]  out JArray?  Array,
                                                    [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Array = null;

            if (!TryGetMandatoryToken(JSON, PropertyName, PropertyDescription, out var token, out ErrorResponse))
                return false;

            return CheckArray(token, PropertyName, PropertyDescription, MinItems, MaxItems, out Array, out ErrorResponse);

        }

        private static Boolean CheckArray(JToken                            Token,
                                          String                            PropertyName,
                                          String                            PropertyDescription,
                                          UInt32?                           MinItems,
                                          UInt32?                           MaxItems,
                                          [NotNullWhen(true)]  out JArray?  Array,
                                          [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (Token is not JArray array)
            {
                Array          = null;
                ErrorResponse  = $"Invalid {PropertyDescription} '{PropertyName}': a JSON array is expected!";
                return false;
            }

            if (MinItems.HasValue && array.Count < MinItems.Value)
            {
                Array          = null;
                ErrorResponse  = $"Invalid {PropertyDescription} '{PropertyName}': at least {MinItems.Value} item(s) expected, but {array.Count} found!";
                return false;
            }

            if (MaxItems.HasValue && array.Count > MaxItems.Value)
            {
                Array          = null;
                ErrorResponse  = $"Invalid {PropertyDescription} '{PropertyName}': at most {MaxItems.Value} item(s) allowed, but {array.Count} found!";
                return false;
            }

            Array          = array;
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}

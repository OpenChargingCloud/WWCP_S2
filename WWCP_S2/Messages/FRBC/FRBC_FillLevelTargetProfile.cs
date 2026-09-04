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
    /// The Resource Manager informs the CEM which fill levels of the storage have to be
    /// reached within which time frames, e.g. the state of charge an EV needs at departure.
    /// </summary>
    public sealed class FRBC_FillLevelTargetProfile : AS2Message,
                                                      IEquatable<FRBC_FillLevelTargetProfile>
    {

        #region Data

        /// <summary>
        /// The message type "FRBC.FillLevelTargetProfile".
        /// </summary>
        public const String MessageTypeName = "FRBC.FillLevelTargetProfile";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "FRBC.FillLevelTargetProfile".
        /// </summary>
        public override String                                  MessageType
            => MessageTypeName;

        /// <summary>
        /// The time at which this fill level target profile starts.
        /// </summary>
        [Mandatory]
        public DateTimeOffset                                   StartTime    { get; }

        /// <summary>
        /// The list of different fill levels that have to be targeted within a given duration.
        /// There shall be at least one element. Elements are placed in chronological order.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<FRBC_FillLevelTargetProfileElement>  Elements     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new FRBC fill level target profile.
        /// </summary>
        /// <param name="StartTime">The time at which this fill level target profile starts.</param>
        /// <param name="Elements">The fill levels that have to be targeted within a given duration, in chronological order (1..288 entries).</param>
        /// <param name="MessageId">An optional message identification.</param>
        public FRBC_FillLevelTargetProfile(DateTimeOffset                                   StartTime,
                                           IReadOnlyList<FRBC_FillLevelTargetProfileElement>  Elements,
                                           Message_Id?                                      MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(Elements);

            if (Elements.Count < 1)
                throw new ArgumentException("An FRBC fill level target profile must contain at least one element!",
                                            nameof(Elements));

            if (Elements.Count > 288)
                throw new ArgumentException($"An FRBC fill level target profile must not contain more than 288 elements, but {Elements.Count} were given!",
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

        // FRBC.FillLevelTargetProfile.schema.json
        //   "title": "FRBC_FillLevelTargetProfile",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "FRBC.FillLevelTargetProfile" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "start_time":   { "type": "string", "format": "date-time",
        //                       "description": "Time at which the FRBC.FillLevelTargetProfile starts." },
        //     "elements":     { "type": "array", "minItems": 1, "maxItems": 288,
        //                       "items": { "$ref": "../schemas/FRBC.FillLevelTargetProfileElement.schema.json" },
        //                       "description": "List of different fill levels that have to be targeted within a given duration.
        //                                       There shall be at least one element. Elements must be placed in chronological order." }
        //   },
        //   "required": ["message_type", "message_id", "start_time", "elements"],
        //   "additionalProperties": false
        //
        // The chronological order of the elements is implied by their durations: every element starts
        // when the previous element ends; the first element starts at start_time.

        #endregion

        #region (static) TryParse(JSON, out FRBC_FillLevelTargetProfile, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an FRBC fill level target profile.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FillLevelTargetProfile">The parsed FRBC fill level target profile.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out FRBC_FillLevelTargetProfile?  FillLevelTargetProfile,
                                       [NotNullWhen(false)] out String?                       ErrorResponse)

            => TryParse(JSON,
                        out FillLevelTargetProfile,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC fill level target profile.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FillLevelTargetProfile">The parsed FRBC fill level target profile.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out FRBC_FillLevelTargetProfile?  FillLevelTargetProfile,
                                       [NotNullWhen(false)] out String?                       ErrorResponse,
                                       S2ParserOptions?                                       Options)

            => TryParse(JSON,
                        out FillLevelTargetProfile,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC fill level target profile.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FillLevelTargetProfile">The parsed FRBC fill level target profile.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomFillLevelTargetProfileParser">A delegate to parse custom FRBC fill level target profiles.</param>
        public static Boolean TryParse(JObject                                                    JSON,
                                       [NotNullWhen(true)]  out FRBC_FillLevelTargetProfile?      FillLevelTargetProfile,
                                       [NotNullWhen(false)] out String?                           ErrorResponse,
                                       S2ParserOptions?                                           Options,
                                       CustomJObjectParserDelegate<FRBC_FillLevelTargetProfile>?  CustomFillLevelTargetProfileParser)
        {

            try
            {

                FillLevelTargetProfile = null;

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
                                               "fill level target profile elements",
                                               FRBC_FillLevelTargetProfileElement.TryParse,
                                               Options,
                                               1,
                                               288,
                                               out IReadOnlyList<FRBC_FillLevelTargetProfileElement>? elements,
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


                FillLevelTargetProfile = new FRBC_FillLevelTargetProfile(
                                             startTime,
                                             elements,
                                             messageId
                                         );

                if (CustomFillLevelTargetProfileParser is not null)
                    FillLevelTargetProfile = CustomFillLevelTargetProfileParser(JSON,
                                                                                FillLevelTargetProfile);

                return true;

            }
            catch (Exception e)
            {
                FillLevelTargetProfile  = null;
                ErrorResponse           = "The given JSON representation of an FRBC fill level target profile is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomFillLevelTargetProfileSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomFillLevelTargetProfileSerializer">A delegate to serialize custom FRBC fill level target profiles.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_FillLevelTargetProfile>? CustomFillLevelTargetProfileSerializer)
        {

            var json = CreateJSON(
                           new JProperty("start_time",  StartTime.ToS2Timestamp()),
                           new JProperty("elements",    new JArray(Elements.Select(element => element.ToJSON())))
                       );

            return CustomFillLevelTargetProfileSerializer is not null
                       ? CustomFillLevelTargetProfileSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this FRBC fill level target profile.
        /// </summary>
        public FRBC_FillLevelTargetProfile Clone()

            => new (
                   StartTime,
                   Elements. Select(element => element.Clone()).ToList(),
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two FRBC fill level target profiles for equality.
        /// </summary>
        public static Boolean operator == (FRBC_FillLevelTargetProfile? FillLevelTargetProfile1, FRBC_FillLevelTargetProfile? FillLevelTargetProfile2)
        {

            if (ReferenceEquals(FillLevelTargetProfile1, FillLevelTargetProfile2))
                return true;

            if (FillLevelTargetProfile1 is null || FillLevelTargetProfile2 is null)
                return false;

            return FillLevelTargetProfile1.Equals(FillLevelTargetProfile2);

        }

        /// <summary>
        /// Compares two FRBC fill level target profiles for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_FillLevelTargetProfile? FillLevelTargetProfile1, FRBC_FillLevelTargetProfile? FillLevelTargetProfile2)
            => !(FillLevelTargetProfile1 == FillLevelTargetProfile2);

        #endregion

        #region IEquatable<FRBC_FillLevelTargetProfile> Members

        /// <summary>
        /// Compares two FRBC fill level target profiles for equality.
        /// </summary>
        /// <param name="Object">An FRBC fill level target profile to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_FillLevelTargetProfile fillLevelTargetProfile && Equals(fillLevelTargetProfile);

        /// <summary>
        /// Compares two FRBC fill level target profiles for equality.
        /// </summary>
        /// <param name="FillLevelTargetProfile">An FRBC fill level target profile to compare with.</param>
        public Boolean Equals(FRBC_FillLevelTargetProfile? FillLevelTargetProfile)

            => FillLevelTargetProfile is not null &&

               MessageId.Equals(FillLevelTargetProfile.MessageId) &&
               StartTime.Equals(FillLevelTargetProfile.StartTime) &&
               Elements. SequenceEqual(FillLevelTargetProfile.Elements);

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
            => $"FRBC.FillLevelTargetProfile starting at {StartTime.ToS2Timestamp()} with {Elements.Count} element(s) [{MessageId}]";

        #endregion

    }

}

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
    /// A request of either side (RM or CEM) to close the current connection and
    /// either terminate the session or reconnect and start a new one.
    /// </summary>
    public sealed class SessionRequest : AS2Message,
                                         IEquatable<SessionRequest>
    {

        #region Data

        /// <summary>
        /// The message type "SessionRequest".
        /// </summary>
        public const String MessageTypeName = "SessionRequest";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "SessionRequest".
        /// </summary>
        public override String    MessageType
            => MessageTypeName;

        /// <summary>
        /// The type of request (RECONNECT or TERMINATE).
        /// </summary>
        [Mandatory]
        public SessionRequestType  Request            { get; }

        /// <summary>
        /// An optional human readable description for debugging purposes.
        /// </summary>
        [Optional]
        public String?             DiagnosticLabel    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new session request.
        /// </summary>
        /// <param name="Request">The type of request.</param>
        /// <param name="DiagnosticLabel">An optional human readable description for debugging purposes.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public SessionRequest(SessionRequestType  Request,
                              String?             DiagnosticLabel   = null,
                              Message_Id?         MessageId         = null)

            : base(MessageId)

        {

            if (Request.IsNullOrEmpty)
                throw new ArgumentException("The session request type must not be null or empty!",
                                            nameof(Request));

            this.Request          = Request;
            this.DiagnosticLabel  = DiagnosticLabel;

            unchecked
            {
                hashCode = this.MessageId.       GetHashCode() * 5 ^
                           this.Request.         GetHashCode() * 3 ^
                          (this.DiagnosticLabel?.GetHashCode(StringComparison.Ordinal) ?? 0);
            }

        }

        #endregion


        #region Documentation

        // SessionRequest.schema.json
        //   "title": "SessionRequest",
        //   "properties": {
        //     "message_type":     { "type": "string", "const": "SessionRequest" },
        //     "message_id":       { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "request":          { "$ref": "../schemas/SessionRequestType.schema.json",
        //                           "description": "The type of request" },
        //     "diagnostic_label": { "type": "string",
        //                           "description": "Optional field for a human readible descirption for debugging purposes" }
        //   },
        //   "required": ["message_type", "message_id", "request"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out SessionRequest, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a session request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SessionRequest">The parsed session request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                   JSON,
                                       [NotNullWhen(true)]  out SessionRequest?  SessionRequest,
                                       [NotNullWhen(false)] out String?          ErrorResponse)

            => TryParse(JSON,
                        out SessionRequest,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a session request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SessionRequest">The parsed session request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                   JSON,
                                       [NotNullWhen(true)]  out SessionRequest?  SessionRequest,
                                       [NotNullWhen(false)] out String?          ErrorResponse,
                                       S2ParserOptions?                          Options)

            => TryParse(JSON,
                        out SessionRequest,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a session request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SessionRequest">The parsed session request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomSessionRequestParser">A delegate to parse custom session requests.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out SessionRequest?      SessionRequest,
                                       [NotNullWhen(false)] out String?              ErrorResponse,
                                       S2ParserOptions?                              Options,
                                       CustomJObjectParserDelegate<SessionRequest>?  CustomSessionRequestParser)
        {

            try
            {

                SessionRequest = null;

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

                #region request                    [mandatory]

                if (!JSON.ParseMandatoryS2Enum("request",
                                               "session request type",
                                               SessionRequestType.TryParse,
                                               Options,
                                               out SessionRequestType request,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region diagnostic_label           [optional]

                if (!JSON.ParseOptionalS2String("diagnostic_label",
                                                "diagnostic label",
                                                out String? diagnosticLabel,
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
                                                    "request",
                                                    "diagnostic_label"))
                {
                    return false;
                }

                #endregion


                SessionRequest = new SessionRequest(
                                     request,
                                     diagnosticLabel,
                                     messageId
                                 );

                if (CustomSessionRequestParser is not null)
                    SessionRequest = CustomSessionRequestParser(JSON,
                                                                SessionRequest);

                return true;

            }
            catch (Exception e)
            {
                SessionRequest  = null;
                ErrorResponse   = "The given JSON representation of a session request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomSessionRequestSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomSessionRequestSerializer">A delegate to serialize custom session requests.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<SessionRequest>? CustomSessionRequestSerializer)
        {

            var json = CreateJSON(

                                 new JProperty("request",           Request.ToString()),

                           DiagnosticLabel is not null
                               ? new JProperty("diagnostic_label",  DiagnosticLabel)
                               : null

                       );

            return CustomSessionRequestSerializer is not null
                       ? CustomSessionRequestSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this session request.
        /// </summary>
        public SessionRequest Clone()

            => new (
                   Request.         Clone(),
                   DiagnosticLabel?.CloneString(),
                   MessageId.       Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two session requests for equality.
        /// </summary>
        public static Boolean operator == (SessionRequest? SessionRequest1, SessionRequest? SessionRequest2)
        {

            if (ReferenceEquals(SessionRequest1, SessionRequest2))
                return true;

            if (SessionRequest1 is null || SessionRequest2 is null)
                return false;

            return SessionRequest1.Equals(SessionRequest2);

        }

        /// <summary>
        /// Compares two session requests for inequality.
        /// </summary>
        public static Boolean operator != (SessionRequest? SessionRequest1, SessionRequest? SessionRequest2)
            => !(SessionRequest1 == SessionRequest2);

        #endregion

        #region IEquatable<SessionRequest> Members

        /// <summary>
        /// Compares two session requests for equality.
        /// </summary>
        /// <param name="Object">A session request to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is SessionRequest sessionRequest && Equals(sessionRequest);

        /// <summary>
        /// Compares two session requests for equality.
        /// </summary>
        /// <param name="SessionRequest">A session request to compare with.</param>
        public Boolean Equals(SessionRequest? SessionRequest)

            => SessionRequest is not null &&

               MessageId.Equals(SessionRequest.MessageId) &&
               Request.  Equals(SessionRequest.Request)   &&

               String.Equals(DiagnosticLabel, SessionRequest.DiagnosticLabel, StringComparison.Ordinal);

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

                   $"SessionRequest {Request}",

                   DiagnosticLabel is not null
                       ? $" ({DiagnosticLabel})"
                       : "",

                   $" [{MessageId}]"

               );

        #endregion

    }

}

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

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The request body of POST /requestConnectionDetails (s2-connect-pairing.yml): the pairing
    /// client answers the HMAC challenge of the server and asks for the connection details,
    /// because the pairing server will also be the communication server. Authenticated by the
    /// pairing attempt identification as bearer token.
    /// </summary>
    public sealed class RequestConnectionDetailsRequest : IEquatable<RequestConnectionDetailsRequest>
    {

        #region Properties

        /// <summary>
        /// The response of the client to the HMAC challenge of the server.
        /// </summary>
        [Mandatory]
        public HmacChallengeResponse  ServerHmacChallengeResponse    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new request connection details request.
        /// </summary>
        /// <param name="ServerHmacChallengeResponse">The response of the client to the HMAC challenge of the server.</param>
        public RequestConnectionDetailsRequest(HmacChallengeResponse ServerHmacChallengeResponse)
        {

            if (ServerHmacChallengeResponse.Length == 0)
                throw new ArgumentException("The server HMAC challenge response must not be empty!", nameof(ServerHmacChallengeResponse));

            this.ServerHmacChallengeResponse = ServerHmacChallengeResponse;

            unchecked
            {
                hashCode = this.ServerHmacChallengeResponse.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   /requestConnectionDetails: post:
        //     summary: Request connection information from the server. This is only used if the PairingServer is also
        //              the CommunicationServer.
        //     security: [ pairingAttemptId ]
        //     requestBody:
        //       description: A Json message with the server HMAC challenge response.
        //       content: application/json: schema:
        //         type: object
        //         required: ["serverHmacChallengeResponse"]
        //         properties:
        //           serverHmacChallengeResponse:  { $ref: HmacChallengeResponse }   (string, format byte)
        //     responses:
        //       '200': ConnectionDetails
        //       '400': The server did not understand the request or is not able to provide connection details.
        //       '401': Provided pairingAttemptId not accepted.
        //       '403': The server did not accept the provided HmacChallengeResponse.

        #endregion

        #region (static) TryParse(JSON, out RequestConnectionDetailsRequest, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a request connection details request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RequestConnectionDetailsRequest">The parsed request connection details request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                    JSON,
                                       [NotNullWhen(true)]  out RequestConnectionDetailsRequest?  RequestConnectionDetailsRequest,
                                       [NotNullWhen(false)] out String?                           ErrorResponse)

            => TryParse(JSON,
                        out RequestConnectionDetailsRequest,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a request connection details request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RequestConnectionDetailsRequest">The parsed request connection details request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                    JSON,
                                       [NotNullWhen(true)]  out RequestConnectionDetailsRequest?  RequestConnectionDetailsRequest,
                                       [NotNullWhen(false)] out String?                           ErrorResponse,
                                       S2ParserOptions?                                           Options)

            => TryParse(JSON,
                        out RequestConnectionDetailsRequest,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a request connection details request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RequestConnectionDetailsRequest">The parsed request connection details request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomRequestConnectionDetailsRequestParser">A delegate to parse custom request connection details requests.</param>
        public static Boolean TryParse(JObject                                                        JSON,
                                       [NotNullWhen(true)]  out RequestConnectionDetailsRequest?      RequestConnectionDetailsRequest,
                                       [NotNullWhen(false)] out String?                               ErrorResponse,
                                       S2ParserOptions?                                               Options,
                                       CustomJObjectParserDelegate<RequestConnectionDetailsRequest>?  CustomRequestConnectionDetailsRequestParser)
        {

            try
            {

                RequestConnectionDetailsRequest = null;

                #region serverHmacChallengeResponse    [mandatory]

                if (!JSON.ParseMandatoryS2String("serverHmacChallengeResponse",
                                                 "server HMAC challenge response",
                                                 out String? serverHmacChallengeResponseText,
                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!HmacChallengeResponse.TryParse(serverHmacChallengeResponseText,
                                                    out HmacChallengeResponse serverHmacChallengeResponse))
                {
                    ErrorResponse = "Invalid server HMAC challenge response 'serverHmacChallengeResponse': a standard Base64 text is expected!";
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "serverHmacChallengeResponse"))
                {
                    return false;
                }

                #endregion


                RequestConnectionDetailsRequest = new RequestConnectionDetailsRequest(
                                                      serverHmacChallengeResponse
                                                  );

                if (CustomRequestConnectionDetailsRequestParser is not null)
                    RequestConnectionDetailsRequest = CustomRequestConnectionDetailsRequestParser(JSON,
                                                                                                  RequestConnectionDetailsRequest);

                return true;

            }
            catch (Exception e)
            {
                RequestConnectionDetailsRequest  = null;
                ErrorResponse                    = "The given JSON representation of a request connection details request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomRequestConnectionDetailsRequestSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomRequestConnectionDetailsRequestSerializer">A delegate to serialize custom request connection details requests.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<RequestConnectionDetailsRequest>? CustomRequestConnectionDetailsRequestSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("serverHmacChallengeResponse",  ServerHmacChallengeResponse.Value)
                       );

            return CustomRequestConnectionDetailsRequestSerializer is not null
                       ? CustomRequestConnectionDetailsRequestSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this request connection details request.
        /// </summary>
        public RequestConnectionDetailsRequest Clone()

            => new (
                   ServerHmacChallengeResponse
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two request connection details requests for equality.
        /// </summary>
        public static Boolean operator == (RequestConnectionDetailsRequest? RequestConnectionDetailsRequest1, RequestConnectionDetailsRequest? RequestConnectionDetailsRequest2)
        {

            if (ReferenceEquals(RequestConnectionDetailsRequest1, RequestConnectionDetailsRequest2))
                return true;

            if (RequestConnectionDetailsRequest1 is null || RequestConnectionDetailsRequest2 is null)
                return false;

            return RequestConnectionDetailsRequest1.Equals(RequestConnectionDetailsRequest2);

        }

        /// <summary>
        /// Compares two request connection details requests for inequality.
        /// </summary>
        public static Boolean operator != (RequestConnectionDetailsRequest? RequestConnectionDetailsRequest1, RequestConnectionDetailsRequest? RequestConnectionDetailsRequest2)
            => !(RequestConnectionDetailsRequest1 == RequestConnectionDetailsRequest2);

        #endregion

        #region IEquatable<RequestConnectionDetailsRequest> Members

        /// <summary>
        /// Compares two request connection details requests for equality.
        /// </summary>
        /// <param name="Object">A request connection details request to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is RequestConnectionDetailsRequest requestConnectionDetailsRequest && Equals(requestConnectionDetailsRequest);

        /// <summary>
        /// Compares two request connection details requests for equality.
        /// </summary>
        /// <param name="RequestConnectionDetailsRequest">A request connection details request to compare with.</param>
        public Boolean Equals(RequestConnectionDetailsRequest? RequestConnectionDetailsRequest)

            => RequestConnectionDetailsRequest is not null &&

               ServerHmacChallengeResponse.Equals(RequestConnectionDetailsRequest.ServerHmacChallengeResponse);

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
        /// Return a text representation of this object (the challenge response is redacted).
        /// </summary>
        public override String ToString()
            => $"Connection details request with {ServerHmacChallengeResponse}";

        #endregion

    }

}

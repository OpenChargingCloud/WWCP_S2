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
    /// The 200 response body of POST /requestPairing (s2-connect-pairing.yml): the pairing
    /// server assigns the pairing attempt its secret identification, describes its node and
    /// endpoint, selects the HMAC hashing algorithm, answers the client challenge and provides
    /// its own challenge for the mutual challenge-response process.
    /// </summary>
    public sealed class RequestPairingResponse : IEquatable<RequestPairingResponse>
    {

        #region Properties

        /// <summary>
        /// The secret identification of this pairing attempt, used by the client as bearer token
        /// for all further requests of the attempt.
        /// </summary>
        [Mandatory]
        public PairingAttemptId       PairingAttemptId                { get; }

        /// <summary>
        /// The description of the node of the pairing server.
        /// </summary>
        [Mandatory]
        public NodeDescription        ServerNodeDescription           { get; }

        /// <summary>
        /// The description of the endpoint of the pairing server.
        /// </summary>
        [Mandatory]
        public EndpointDescription    ServerEndpointDescription       { get; }

        /// <summary>
        /// The HMAC hashing algorithm the server selected; it applies to both the client and
        /// the server challenge.
        /// </summary>
        [Mandatory]
        public HmacHashingAlgorithm   SelectedHmacHashingAlgorithm    { get; }

        /// <summary>
        /// The response of the server to the HMAC challenge of the client.
        /// </summary>
        [Mandatory]
        public HmacChallengeResponse  ClientHmacChallengeResponse     { get; }

        /// <summary>
        /// The HMAC challenge of the server (at least 32 random bytes), to be answered by the client.
        /// </summary>
        [Mandatory]
        public HmacChallenge          ServerHmacChallenge             { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new request pairing response.
        /// </summary>
        /// <param name="PairingAttemptId">The secret identification of this pairing attempt.</param>
        /// <param name="ServerNodeDescription">The description of the node of the pairing server.</param>
        /// <param name="ServerEndpointDescription">The description of the endpoint of the pairing server.</param>
        /// <param name="SelectedHmacHashingAlgorithm">The HMAC hashing algorithm the server selected.</param>
        /// <param name="ClientHmacChallengeResponse">The response of the server to the HMAC challenge of the client.</param>
        /// <param name="ServerHmacChallenge">The HMAC challenge of the server (at least 32 bytes).</param>
        public RequestPairingResponse(PairingAttemptId       PairingAttemptId,
                                      NodeDescription        ServerNodeDescription,
                                      EndpointDescription    ServerEndpointDescription,
                                      HmacHashingAlgorithm   SelectedHmacHashingAlgorithm,
                                      HmacChallengeResponse  ClientHmacChallengeResponse,
                                      HmacChallenge          ServerHmacChallenge)
        {

            ArgumentNullException.ThrowIfNull(ServerNodeDescription);
            ArgumentNullException.ThrowIfNull(ServerEndpointDescription);

            if (PairingAttemptId.Length == 0)
                throw new ArgumentException("The pairing attempt identification must not be empty!", nameof(PairingAttemptId));

            if (SelectedHmacHashingAlgorithm.IsNullOrEmpty)
                throw new ArgumentException("The selected HMAC hashing algorithm must not be empty!", nameof(SelectedHmacHashingAlgorithm));

            if (ClientHmacChallengeResponse.Length == 0)
                throw new ArgumentException("The client HMAC challenge response must not be empty!", nameof(ClientHmacChallengeResponse));

            if (ServerHmacChallenge.Length < HmacChallenge.MinimumBytes)
                throw new ArgumentException($"The server HMAC challenge must have at least {HmacChallenge.MinimumBytes} bytes!", nameof(ServerHmacChallenge));

            this.PairingAttemptId              = PairingAttemptId;
            this.ServerNodeDescription         = ServerNodeDescription;
            this.ServerEndpointDescription     = ServerEndpointDescription;
            this.SelectedHmacHashingAlgorithm  = SelectedHmacHashingAlgorithm;
            this.ClientHmacChallengeResponse   = ClientHmacChallengeResponse;
            this.ServerHmacChallenge           = ServerHmacChallenge;

            unchecked
            {
                hashCode = this.PairingAttemptId.            GetHashCode() * 13 ^
                           this.ServerNodeDescription.       GetHashCode() * 11 ^
                           this.ServerEndpointDescription.   GetHashCode() *  7 ^
                           this.SelectedHmacHashingAlgorithm.GetHashCode() *  5 ^
                           this.ClientHmacChallengeResponse. GetHashCode() *  3 ^
                           this.ServerHmacChallenge.         GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   /requestPairing: post: responses: '200':
        //     description: Pairing is possible and provides information to continue the mutual challenge-response
        //                  process. The selectedHmacHashingAlgorithm is applied to both the client and server HMAC challenges.
        //     content: application/json: schema:
        //       type: object
        //       required: ["pairingAttemptId", "serverNodeDescription", "serverEndpointDescription",
        //                  "selectedHmacHashingAlgorithm", "clientHmacChallengeResponse", "serverHmacChallenge"]
        //       properties:
        //         pairingAttemptId:              { $ref: PairingAttemptId }            (string, minLength 32, secret)
        //         serverNodeDescription:         { $ref: s2-connect-common.yml#/components/schemas/NodeDescription }
        //         serverEndpointDescription:     { $ref: s2-connect-common.yml#/components/schemas/EndpointDescription }
        //         selectedHmacHashingAlgorithm:  { $ref: HmacHashingAlgorithm }        (enum ["SHA256"])
        //         clientHmacChallengeResponse:   { $ref: HmacChallengeResponse }       (string, format byte)
        //         serverHmacChallenge:           { $ref: HmacChallenge }               (string, format byte, at least 32 bytes)
        //
        //   securitySchemes: pairingAttemptId: { type: http, scheme: bearer }

        #endregion

        #region (static) TryParse(JSON, out RequestPairingResponse, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a request pairing response.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RequestPairingResponse">The parsed request pairing response.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out RequestPairingResponse?  RequestPairingResponse,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)

            => TryParse(JSON,
                        out RequestPairingResponse,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a request pairing response.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RequestPairingResponse">The parsed request pairing response.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out RequestPairingResponse?  RequestPairingResponse,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options)

            => TryParse(JSON,
                        out RequestPairingResponse,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a request pairing response.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RequestPairingResponse">The parsed request pairing response.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomRequestPairingResponseParser">A delegate to parse custom request pairing responses.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out RequestPairingResponse?      RequestPairingResponse,
                                       [NotNullWhen(false)] out String?                      ErrorResponse,
                                       S2ParserOptions?                                      Options,
                                       CustomJObjectParserDelegate<RequestPairingResponse>?  CustomRequestPairingResponseParser)
        {

            try
            {

                RequestPairingResponse = null;

                #region pairingAttemptId                [mandatory]

                if (!JSON.ParseMandatoryS2String("pairingAttemptId",
                                                 "pairing attempt identification",
                                                 out String? pairingAttemptIdText,
                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!PairingAttemptId.TryParse(pairingAttemptIdText,
                                               out PairingAttemptId pairingAttemptId))
                {
                    ErrorResponse = $"Invalid pairing attempt identification 'pairingAttemptId': at least {S2ConnectDefaults.MinPairingAttemptIdLength} characters without white space are expected!";
                    return false;
                }

                #endregion

                #region serverNodeDescription           [mandatory]

                if (!JSON.ParseMandatoryS2("serverNodeDescription",
                                           "server node description",
                                           NodeDescription.TryParse,
                                           Options,
                                           out NodeDescription? serverNodeDescription,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region serverEndpointDescription       [mandatory]

                if (!JSON.ParseMandatoryS2("serverEndpointDescription",
                                           "server endpoint description",
                                           EndpointDescription.TryParse,
                                           Options,
                                           out EndpointDescription? serverEndpointDescription,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region selectedHmacHashingAlgorithm    [mandatory]

                if (!JSON.ParseMandatoryS2Enum("selectedHmacHashingAlgorithm",
                                               "selected HMAC hashing algorithm",
                                               HmacHashingAlgorithm.TryParse,
                                               Options,
                                               out HmacHashingAlgorithm selectedHmacHashingAlgorithm,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region clientHmacChallengeResponse     [mandatory]

                if (!JSON.ParseMandatoryS2String("clientHmacChallengeResponse",
                                                 "client HMAC challenge response",
                                                 out String? clientHmacChallengeResponseText,
                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!HmacChallengeResponse.TryParse(clientHmacChallengeResponseText,
                                                    out HmacChallengeResponse clientHmacChallengeResponse))
                {
                    ErrorResponse = "Invalid client HMAC challenge response 'clientHmacChallengeResponse': a standard Base64 text is expected!";
                    return false;
                }

                #endregion

                #region serverHmacChallenge             [mandatory]

                if (!JSON.ParseMandatoryS2String("serverHmacChallenge",
                                                 "server HMAC challenge",
                                                 out String? serverHmacChallengeText,
                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!HmacChallenge.TryParse(serverHmacChallengeText,
                                            out HmacChallenge serverHmacChallenge))
                {
                    ErrorResponse = $"Invalid server HMAC challenge 'serverHmacChallenge': a standard Base64 text of at least {HmacChallenge.MinimumBytes} bytes is expected!";
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "pairingAttemptId",
                                                    "serverNodeDescription",
                                                    "serverEndpointDescription",
                                                    "selectedHmacHashingAlgorithm",
                                                    "clientHmacChallengeResponse",
                                                    "serverHmacChallenge"))
                {
                    return false;
                }

                #endregion


                RequestPairingResponse = new RequestPairingResponse(
                                             pairingAttemptId,
                                             serverNodeDescription,
                                             serverEndpointDescription,
                                             selectedHmacHashingAlgorithm,
                                             clientHmacChallengeResponse,
                                             serverHmacChallenge
                                         );

                if (CustomRequestPairingResponseParser is not null)
                    RequestPairingResponse = CustomRequestPairingResponseParser(JSON,
                                                                                RequestPairingResponse);

                return true;

            }
            catch (Exception e)
            {
                RequestPairingResponse  = null;
                ErrorResponse           = "The given JSON representation of a request pairing response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomRequestPairingResponseSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomRequestPairingResponseSerializer">A delegate to serialize custom request pairing responses.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<RequestPairingResponse>? CustomRequestPairingResponseSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("pairingAttemptId",              PairingAttemptId.Value),
                           new JProperty("serverNodeDescription",         ServerNodeDescription.    ToJSON()),
                           new JProperty("serverEndpointDescription",     ServerEndpointDescription.ToJSON()),
                           new JProperty("selectedHmacHashingAlgorithm",  SelectedHmacHashingAlgorithm.ToString()),
                           new JProperty("clientHmacChallengeResponse",   ClientHmacChallengeResponse.Value),
                           new JProperty("serverHmacChallenge",           ServerHmacChallenge.Value)
                       );

            return CustomRequestPairingResponseSerializer is not null
                       ? CustomRequestPairingResponseSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this request pairing response.
        /// </summary>
        public RequestPairingResponse Clone()

            => new (
                   PairingAttemptId,
                   ServerNodeDescription.    Clone(),
                   ServerEndpointDescription.Clone(),
                   SelectedHmacHashingAlgorithm.Clone(),
                   ClientHmacChallengeResponse,
                   ServerHmacChallenge
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two request pairing responses for equality.
        /// </summary>
        public static Boolean operator == (RequestPairingResponse? RequestPairingResponse1, RequestPairingResponse? RequestPairingResponse2)
        {

            if (ReferenceEquals(RequestPairingResponse1, RequestPairingResponse2))
                return true;

            if (RequestPairingResponse1 is null || RequestPairingResponse2 is null)
                return false;

            return RequestPairingResponse1.Equals(RequestPairingResponse2);

        }

        /// <summary>
        /// Compares two request pairing responses for inequality.
        /// </summary>
        public static Boolean operator != (RequestPairingResponse? RequestPairingResponse1, RequestPairingResponse? RequestPairingResponse2)
            => !(RequestPairingResponse1 == RequestPairingResponse2);

        #endregion

        #region IEquatable<RequestPairingResponse> Members

        /// <summary>
        /// Compares two request pairing responses for equality.
        /// </summary>
        /// <param name="Object">A request pairing response to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is RequestPairingResponse requestPairingResponse && Equals(requestPairingResponse);

        /// <summary>
        /// Compares two request pairing responses for equality.
        /// </summary>
        /// <param name="RequestPairingResponse">A request pairing response to compare with.</param>
        public Boolean Equals(RequestPairingResponse? RequestPairingResponse)

            => RequestPairingResponse is not null &&

               PairingAttemptId.            Equals(RequestPairingResponse.PairingAttemptId)             &&
               ServerNodeDescription.       Equals(RequestPairingResponse.ServerNodeDescription)        &&
               ServerEndpointDescription.   Equals(RequestPairingResponse.ServerEndpointDescription)    &&
               SelectedHmacHashingAlgorithm.Equals(RequestPairingResponse.SelectedHmacHashingAlgorithm) &&
               ClientHmacChallengeResponse. Equals(RequestPairingResponse.ClientHmacChallengeResponse)  &&
               ServerHmacChallenge.         Equals(RequestPairingResponse.ServerHmacChallenge);

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
        /// Return a text representation of this object (the pairing attempt identification,
        /// the challenge and the challenge response are redacted).
        /// </summary>
        public override String ToString()

            => $"Pairing response of {ServerNodeDescription.Role} node {ServerNodeDescription.Id} using {SelectedHmacHashingAlgorithm} ({PairingAttemptId})";

        #endregion

    }

}

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
    /// The request body of POST /postConnectionDetails (s2-connect-pairing.yml): the pairing
    /// client answers the HMAC challenge of the server and sends the connection details of
    /// its own session initiation API, because the pairing client will be the communication
    /// server. The connection details therefore always carry the fingerprint of the CA
    /// certificate. Authenticated by the pairing attempt identification as bearer token.
    /// </summary>
    public sealed class PostConnectionDetailsRequest : IEquatable<PostConnectionDetailsRequest>
    {

        #region Properties

        /// <summary>
        /// The response of the client to the HMAC challenge of the server.
        /// </summary>
        [Mandatory]
        public HmacChallengeResponse  ServerHmacChallengeResponse    { get; }

        /// <summary>
        /// The connection details of the communication server (the pairing client),
        /// including the fingerprint of its CA certificate.
        /// </summary>
        [Mandatory]
        public ConnectionDetails      ConnectionDetails              { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new post connection details request.
        /// </summary>
        /// <param name="ServerHmacChallengeResponse">The response of the client to the HMAC challenge of the server.</param>
        /// <param name="ConnectionDetails">The connection details of the communication server, including the fingerprint of its CA certificate.</param>
        public PostConnectionDetailsRequest(HmacChallengeResponse  ServerHmacChallengeResponse,
                                            ConnectionDetails      ConnectionDetails)
        {

            ArgumentNullException.ThrowIfNull(ConnectionDetails);

            if (ServerHmacChallengeResponse.Length == 0)
                throw new ArgumentException("The server HMAC challenge response must not be empty!", nameof(ServerHmacChallengeResponse));

            if (ConnectionDetails.CertificateFingerprints is null)
                throw new ArgumentException("The connection details must include the certificate fingerprint of the CA certificate, as the pairing client becomes the communication server (S2 Connect 1.0.0, 6B)!",
                                            nameof(ConnectionDetails));

            this.ServerHmacChallengeResponse  = ServerHmacChallengeResponse;
            this.ConnectionDetails            = ConnectionDetails;

            unchecked
            {
                hashCode = this.ServerHmacChallengeResponse.GetHashCode() * 3 ^
                           this.ConnectionDetails.          GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   /postConnectionDetails: post:
        //     summary: Send connection information to the server. This only used if the PairingClient is the CommunicationServer.
        //     security: [ pairingAttemptId ]
        //     requestBody:
        //       description: The information the PairingClient needs to set up a S2 connection after pairing.
        //       content: application/json: schema:
        //         type: object
        //         required: ["serverHmacChallengeResponse", "connectionDetails"]
        //         properties:
        //           serverHmacChallengeResponse:  { $ref: HmacChallengeResponse }   (string, format byte)
        //           connectionDetails:            { $ref: ConnectionDetails }
        //     responses:
        //       '204': The information was successfully received by the server.
        //       '400': The request was not understood by the server or the server was not expecting connection details.
        //       '401': Provided pairingAttemptId not accepted.
        //       '403': The server did not accept the provided HmacChallengeResponse.
        //
        //   ConnectionDetails.certificateFingerprint: "The property certificateFingerprint is mandatory when the pairing
        //   client will be come the communication server."
        //
        // S2 Connect 1.0.0, "6B. POST /[version]/postConnectionDetails": the pairing server becomes the communication
        // client and needs the fingerprint of the CA certificate of the communication server to pin it.
        //
        // Semantic rule (validated in the constructor): ConnectionDetails.CertificateFingerprints must be present.

        #endregion

        #region (static) TryParse(JSON, out PostConnectionDetailsRequest, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a post connection details request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PostConnectionDetailsRequest">The parsed post connection details request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                 JSON,
                                       [NotNullWhen(true)]  out PostConnectionDetailsRequest?  PostConnectionDetailsRequest,
                                       [NotNullWhen(false)] out String?                        ErrorResponse)

            => TryParse(JSON,
                        out PostConnectionDetailsRequest,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a post connection details request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PostConnectionDetailsRequest">The parsed post connection details request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                 JSON,
                                       [NotNullWhen(true)]  out PostConnectionDetailsRequest?  PostConnectionDetailsRequest,
                                       [NotNullWhen(false)] out String?                        ErrorResponse,
                                       S2ParserOptions?                                        Options)

            => TryParse(JSON,
                        out PostConnectionDetailsRequest,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a post connection details request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PostConnectionDetailsRequest">The parsed post connection details request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPostConnectionDetailsRequestParser">A delegate to parse custom post connection details requests.</param>
        public static Boolean TryParse(JObject                                                     JSON,
                                       [NotNullWhen(true)]  out PostConnectionDetailsRequest?      PostConnectionDetailsRequest,
                                       [NotNullWhen(false)] out String?                            ErrorResponse,
                                       S2ParserOptions?                                            Options,
                                       CustomJObjectParserDelegate<PostConnectionDetailsRequest>?  CustomPostConnectionDetailsRequestParser)
        {

            try
            {

                PostConnectionDetailsRequest = null;

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

                #region connectionDetails              [mandatory]

                if (!JSON.ParseMandatoryS2("connectionDetails",
                                           "connection details",
                                           ConnectionDetails.TryParse,
                                           Options,
                                           out ConnectionDetails? connectionDetails,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "serverHmacChallengeResponse",
                                                    "connectionDetails"))
                {
                    return false;
                }

                #endregion


                PostConnectionDetailsRequest = new PostConnectionDetailsRequest(
                                                   serverHmacChallengeResponse,
                                                   connectionDetails
                                               );

                if (CustomPostConnectionDetailsRequestParser is not null)
                    PostConnectionDetailsRequest = CustomPostConnectionDetailsRequestParser(JSON,
                                                                                            PostConnectionDetailsRequest);

                return true;

            }
            catch (Exception e)
            {
                PostConnectionDetailsRequest  = null;
                ErrorResponse                 = "The given JSON representation of a post connection details request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPostConnectionDetailsRequestSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPostConnectionDetailsRequestSerializer">A delegate to serialize custom post connection details requests.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PostConnectionDetailsRequest>? CustomPostConnectionDetailsRequestSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("serverHmacChallengeResponse",  ServerHmacChallengeResponse.Value),
                           new JProperty("connectionDetails",            ConnectionDetails.ToJSON())
                       );

            return CustomPostConnectionDetailsRequestSerializer is not null
                       ? CustomPostConnectionDetailsRequestSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this post connection details request.
        /// </summary>
        public PostConnectionDetailsRequest Clone()

            => new (
                   ServerHmacChallengeResponse,
                   ConnectionDetails.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two post connection details requests for equality.
        /// </summary>
        public static Boolean operator == (PostConnectionDetailsRequest? PostConnectionDetailsRequest1, PostConnectionDetailsRequest? PostConnectionDetailsRequest2)
        {

            if (ReferenceEquals(PostConnectionDetailsRequest1, PostConnectionDetailsRequest2))
                return true;

            if (PostConnectionDetailsRequest1 is null || PostConnectionDetailsRequest2 is null)
                return false;

            return PostConnectionDetailsRequest1.Equals(PostConnectionDetailsRequest2);

        }

        /// <summary>
        /// Compares two post connection details requests for inequality.
        /// </summary>
        public static Boolean operator != (PostConnectionDetailsRequest? PostConnectionDetailsRequest1, PostConnectionDetailsRequest? PostConnectionDetailsRequest2)
            => !(PostConnectionDetailsRequest1 == PostConnectionDetailsRequest2);

        #endregion

        #region IEquatable<PostConnectionDetailsRequest> Members

        /// <summary>
        /// Compares two post connection details requests for equality.
        /// </summary>
        /// <param name="Object">A post connection details request to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PostConnectionDetailsRequest postConnectionDetailsRequest && Equals(postConnectionDetailsRequest);

        /// <summary>
        /// Compares two post connection details requests for equality.
        /// </summary>
        /// <param name="PostConnectionDetailsRequest">A post connection details request to compare with.</param>
        public Boolean Equals(PostConnectionDetailsRequest? PostConnectionDetailsRequest)

            => PostConnectionDetailsRequest is not null &&

               ServerHmacChallengeResponse.Equals(PostConnectionDetailsRequest.ServerHmacChallengeResponse) &&
               ConnectionDetails.          Equals(PostConnectionDetailsRequest.ConnectionDetails);

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
        /// Return a text representation of this object (the challenge response and the access token are redacted).
        /// </summary>
        public override String ToString()
            => $"Connection details for {ConnectionDetails} with {ServerHmacChallengeResponse}";

        #endregion

    }

}

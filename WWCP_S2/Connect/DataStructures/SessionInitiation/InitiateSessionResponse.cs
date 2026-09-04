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
    /// The HTTP 200 response body of the initiateSession operation: the communication
    /// protocol and the S2 message version the server decided upon, the pending access
    /// token for the next session initiation, and optionally updated node and endpoint
    /// descriptions of the server (s2-connect-session-init.yml, POST /initiateSession, 200).
    /// The access token is a secret: <see cref="ToString"/> never prints it.
    /// </summary>
    public sealed class InitiateSessionResponse : IEquatable<InitiateSessionResponse>
    {

        #region Properties

        /// <summary>
        /// The communication protocol selected by the server from the list of
        /// supported communication protocols provided by the client.
        /// </summary>
        [Mandatory]
        public CommunicationProtocol  SelectedCommunicationProtocol    { get; }

        /// <summary>
        /// The S2 message (protocol) version selected by the server from the list of
        /// supported protocol versions provided by the client.
        /// </summary>
        [Mandatory]
        public String                 SelectedS2MessageVersion         { get; }

        /// <summary>
        /// The pending one-time access token the client must store and confirm via
        /// confirmAccessToken; it authenticates the next session initiation.
        /// </summary>
        [Mandatory]
        public AccessToken            AccessToken                      { get; }

        /// <summary>
        /// An optional (updated) description of the server node.
        /// When not provided the client will use the stored description.
        /// </summary>
        [Optional]
        public NodeDescription?       ServerNodeDescription            { get; }

        /// <summary>
        /// An optional (updated) description of the server endpoint.
        /// When not provided the client will use the stored description.
        /// </summary>
        [Optional]
        public EndpointDescription?   ServerEndpointDescription        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new initiate session response.
        /// </summary>
        /// <param name="SelectedCommunicationProtocol">The communication protocol selected by the server.</param>
        /// <param name="SelectedS2MessageVersion">The S2 message version selected by the server (the schema allows an empty string).</param>
        /// <param name="AccessToken">The pending one-time access token for the next session initiation.</param>
        /// <param name="ServerNodeDescription">An optional (updated) description of the server node.</param>
        /// <param name="ServerEndpointDescription">An optional (updated) description of the server endpoint.</param>
        public InitiateSessionResponse(CommunicationProtocol  SelectedCommunicationProtocol,
                                       String                 SelectedS2MessageVersion,
                                       AccessToken            AccessToken,
                                       NodeDescription?       ServerNodeDescription       = null,
                                       EndpointDescription?   ServerEndpointDescription   = null)
        {

            ArgumentNullException.ThrowIfNull(SelectedS2MessageVersion);

            if (SelectedCommunicationProtocol.IsNullOrEmpty)
                throw new ArgumentException("The selected communication protocol must not be empty!",
                                            nameof(SelectedCommunicationProtocol));

            if (AccessToken.Length < AccessToken.MinimumBytes)
                throw new ArgumentException("The access token must not be empty!",
                                            nameof(AccessToken));

            this.SelectedCommunicationProtocol  = SelectedCommunicationProtocol;
            this.SelectedS2MessageVersion       = SelectedS2MessageVersion;
            this.AccessToken                    = AccessToken;
            this.ServerNodeDescription          = ServerNodeDescription;
            this.ServerEndpointDescription      = ServerEndpointDescription;

            unchecked
            {
                hashCode = this.SelectedCommunicationProtocol.GetHashCode()                          * 11 ^
                           this.SelectedS2MessageVersion.     GetHashCode(StringComparison.Ordinal)  *  7 ^
                           this.AccessToken.                  GetHashCode()                          *  5 ^
                          (this.ServerNodeDescription?.       GetHashCode() ?? 0)                    *  3 ^
                          (this.ServerEndpointDescription?.   GetHashCode() ?? 0);
            }

        }

        #endregion


        #region Documentation

        // s2-connect-session-init.yml
        //   /initiateSession:
        //     post:
        //       responses:
        //         '200':
        //           description: Inform the client about the communication protocol, the S2 message version the server
        //                        decided upon, as well as the pending accessToken. Optionally the server can provide an
        //                        updated NodeDescription and/or EndpointDescription.
        //           schema:
        //             type: object
        //             required: ["selectedCommunicationProtocol", "selectedS2MessageVersion", "accessToken"]
        //             properties:
        //               selectedCommunicationProtocol:  { $ref: CommunicationProtocol }   (enum ["WebSocket"])
        //               selectedS2MessageVersion:       { type: string }
        //                 description: The protocol version selected by the server from the list of supported protocol
        //                              versions provided by the client.
        //               accessToken:                    { $ref: AccessToken }             (string, format byte)
        //               serverNodeDescription:          { $ref: NodeDescription }
        //                 description: Optional field to provide (an updated) NodeDescription. When not provided the
        //                              client will use the stored description.
        //               serverEndpointDescription:      { $ref: EndpointDescription }
        //                 description: Optional field to provide (an updated) EndpointDescription. When not provided the
        //                              client will use the stored description.

        #endregion

        #region (static) TryParse(JSON, out InitiateSessionResponse, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an initiate session response.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="InitiateSessionResponse">The parsed initiate session response.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out InitiateSessionResponse?  InitiateSessionResponse,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)

            => TryParse(JSON,
                        out InitiateSessionResponse,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an initiate session response.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="InitiateSessionResponse">The parsed initiate session response.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out InitiateSessionResponse?  InitiateSessionResponse,
                                       [NotNullWhen(false)] out String?                   ErrorResponse,
                                       S2ParserOptions?                                   Options)

            => TryParse(JSON,
                        out InitiateSessionResponse,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an initiate session response.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="InitiateSessionResponse">The parsed initiate session response.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomInitiateSessionResponseParser">A delegate to parse custom initiate session responses.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out InitiateSessionResponse?      InitiateSessionResponse,
                                       [NotNullWhen(false)] out String?                       ErrorResponse,
                                       S2ParserOptions?                                       Options,
                                       CustomJObjectParserDelegate<InitiateSessionResponse>?  CustomInitiateSessionResponseParser)
        {

            try
            {

                InitiateSessionResponse = null;

                #region selectedCommunicationProtocol    [mandatory]

                if (!JSON.ParseMandatoryS2Enum("selectedCommunicationProtocol",
                                               "selected communication protocol",
                                               CommunicationProtocol.TryParse,
                                               Options,
                                               out CommunicationProtocol selectedCommunicationProtocol,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region selectedS2MessageVersion         [mandatory]

                if (!JSON.ParseMandatoryS2String("selectedS2MessageVersion",
                                                 "selected S2 message version",
                                                 out String? selectedS2MessageVersion,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region accessToken                      [mandatory]

                if (!JSON.ParseMandatoryS2String("accessToken",
                                                 "access token",
                                                 out String? accessTokenText,
                                                 out ErrorResponse))
                {
                    return false;
                }

                // The token value is a secret and is therefore not repeated in the error message.
                if (!AccessToken.TryParse(accessTokenText, out var accessToken))
                {
                    ErrorResponse = "Invalid access token 'accessToken': a standard Base64 text is expected!";
                    return false;
                }

                #endregion

                #region serverNodeDescription            [optional]

                if (!JSON.ParseOptionalS2("serverNodeDescription",
                                          "server node description",
                                          NodeDescription.TryParse,
                                          Options,
                                          out NodeDescription? serverNodeDescription,
                                          out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region serverEndpointDescription        [optional]

                if (!JSON.ParseOptionalS2("serverEndpointDescription",
                                          "server endpoint description",
                                          EndpointDescription.TryParse,
                                          Options,
                                          out EndpointDescription? serverEndpointDescription,
                                          out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "selectedCommunicationProtocol",
                                                    "selectedS2MessageVersion",
                                                    "accessToken",
                                                    "serverNodeDescription",
                                                    "serverEndpointDescription"))
                {
                    return false;
                }

                #endregion


                InitiateSessionResponse = new InitiateSessionResponse(
                                              selectedCommunicationProtocol,
                                              selectedS2MessageVersion,
                                              accessToken,
                                              serverNodeDescription,
                                              serverEndpointDescription
                                          );

                if (CustomInitiateSessionResponseParser is not null)
                    InitiateSessionResponse = CustomInitiateSessionResponseParser(JSON,
                                                                                  InitiateSessionResponse);

                return true;

            }
            catch (Exception e)
            {
                InitiateSessionResponse  = null;
                ErrorResponse            = "The given JSON representation of an initiate session response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomInitiateSessionResponseSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomInitiateSessionResponseSerializer">A delegate to serialize custom initiate session responses.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<InitiateSessionResponse>? CustomInitiateSessionResponseSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("selectedCommunicationProtocol",  SelectedCommunicationProtocol.ToString()),
                                 new JProperty("selectedS2MessageVersion",       SelectedS2MessageVersion),
                                 new JProperty("accessToken",                    AccessToken.Value),

                           ServerNodeDescription is not null
                               ? new JProperty("serverNodeDescription",          ServerNodeDescription.ToJSON())
                               : null,

                           ServerEndpointDescription is not null
                               ? new JProperty("serverEndpointDescription",      ServerEndpointDescription.ToJSON())
                               : null

                       );

            return CustomInitiateSessionResponseSerializer is not null
                       ? CustomInitiateSessionResponseSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this initiate session response.
        /// </summary>
        public InitiateSessionResponse Clone()

            => new (
                   SelectedCommunicationProtocol.Clone(),
                   SelectedS2MessageVersion.     CloneString(),
                   AccessToken,                                  // an immutable readonly struct
                   ServerNodeDescription?.    Clone(),
                   ServerEndpointDescription?.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two initiate session responses for equality.
        /// </summary>
        public static Boolean operator == (InitiateSessionResponse? InitiateSessionResponse1, InitiateSessionResponse? InitiateSessionResponse2)
        {

            if (ReferenceEquals(InitiateSessionResponse1, InitiateSessionResponse2))
                return true;

            if (InitiateSessionResponse1 is null || InitiateSessionResponse2 is null)
                return false;

            return InitiateSessionResponse1.Equals(InitiateSessionResponse2);

        }

        /// <summary>
        /// Compares two initiate session responses for inequality.
        /// </summary>
        public static Boolean operator != (InitiateSessionResponse? InitiateSessionResponse1, InitiateSessionResponse? InitiateSessionResponse2)
            => !(InitiateSessionResponse1 == InitiateSessionResponse2);

        #endregion

        #region IEquatable<InitiateSessionResponse> Members

        /// <summary>
        /// Compares two initiate session responses for equality.
        /// </summary>
        /// <param name="Object">An initiate session response to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is InitiateSessionResponse initiateSessionResponse && Equals(initiateSessionResponse);

        /// <summary>
        /// Compares two initiate session responses for equality.
        /// </summary>
        /// <param name="InitiateSessionResponse">An initiate session response to compare with.</param>
        public Boolean Equals(InitiateSessionResponse? InitiateSessionResponse)

            => InitiateSessionResponse is not null &&

               SelectedCommunicationProtocol.Equals(InitiateSessionResponse.SelectedCommunicationProtocol) &&
               AccessToken.                  Equals(InitiateSessionResponse.AccessToken)                   &&

               String.Equals(SelectedS2MessageVersion, InitiateSessionResponse.SelectedS2MessageVersion, StringComparison.Ordinal) &&

               ServerNodeDescription     == InitiateSessionResponse.ServerNodeDescription &&
               ServerEndpointDescription == InitiateSessionResponse.ServerEndpointDescription;

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
        /// Return a text representation of this object. The access token is redacted.
        /// </summary>
        public override String ToString()

            => $"Session via {SelectedCommunicationProtocol} using S2 message version '{SelectedS2MessageVersion}', {AccessToken}";

        #endregion

    }

}

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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The communication details of a WebSocket S2 message communication channel: the
    /// single-use token with which the client authenticates its WebSocket upgrade request
    /// and the "wss://" URL of the channel (s2-connect-session-init.yml,
    /// WebSocketCommunicationDetails). The token is a secret: <see cref="ToString"/> never prints it.
    /// </summary>
    public sealed class WebSocketCommunicationDetails : CommunicationDetails,
                                                        IEquatable<WebSocketCommunicationDetails>
    {

        #region Properties

        /// <summary>
        /// The communication protocol: WebSocket.
        /// </summary>
        public override CommunicationProtocol  CommunicationProtocol
            => Connect.CommunicationProtocol.WebSocket;

        /// <summary>
        /// The single-use, short-lived token with which the client authenticates its
        /// WebSocket upgrade request (valid for at most 30 seconds).
        /// </summary>
        [Mandatory]
        public CommunicationToken              WebsocketToken    { get; }

        /// <summary>
        /// The URL of the communication channel endpoint, e.g. "wss://cem.example.com/s2".
        /// </summary>
        [Mandatory]
        public URL                             WebsocketUrl      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create new WebSocket communication details.
        /// </summary>
        /// <remarks>
        /// The constructor accepts both "wss://" and (for tests and development) "ws://" URLs.
        /// Whether a plain "ws://" URL is acceptable on the wire is a parser policy
        /// (<see cref="S2ParserOptions.AllowInsecureURLs"/>) enforced in <c>TryParse</c>.
        /// </remarks>
        /// <param name="WebsocketToken">The single-use token authenticating the WebSocket upgrade request.</param>
        /// <param name="WebsocketUrl">The URL of the communication channel endpoint ("wss://", or "ws://" for development only).</param>
        public WebSocketCommunicationDetails(CommunicationToken  WebsocketToken,
                                             URL                 WebsocketUrl)
        {

            if (WebsocketToken.Length < CommunicationToken.MinimumBytes)
                throw new ArgumentException("The WebSocket token must not be empty!",
                                            nameof(WebsocketToken));

            if (WebsocketUrl.Scheme != URIScheme.wss &&
                WebsocketUrl.Scheme != URIScheme.ws)
            {
                throw new ArgumentException($"The WebSocket URL must use the scheme 'wss://' (or 'ws://' for development only), but '{WebsocketUrl}' was given!",
                                            nameof(WebsocketUrl));
            }

            this.WebsocketToken  = WebsocketToken;
            this.WebsocketUrl    = WebsocketUrl;

            unchecked
            {
                hashCode = this.WebsocketToken.GetHashCode() * 3 ^
                           this.WebsocketUrl.  GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // s2-connect-session-init.yml
        //   WebSocketCommunicationDetails:
        //     allOf:
        //       - $ref: CommunicationDetails            (required: ["communicationProtocol"], the discriminator)
        //       - type: object
        //         required: ["websocketToken", "websocketUrl"]
        //         properties:
        //           websocketToken:  { $ref: CommunicationToken }   (string, format byte)
        //           websocketUrl:    { type: string, format: url }
        //             description: The URL of the communication channel endpoint
        //
        //   /confirmAccessToken, response '200': discriminator mapping "WebSocket" -> WebSocketCommunicationDetails
        //
        // Semantic rule: S2 Connect requires TLS everywhere, so the URL must use the scheme "wss://";
        // plain "ws://" is accepted by TryParse only when S2ParserOptions.AllowInsecureURLs is set.

        #endregion

        #region (static) TryParse(JSON, out WebSocketCommunicationDetails, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of WebSocket communication details.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="WebSocketCommunicationDetails">The parsed WebSocket communication details.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                  JSON,
                                       [NotNullWhen(true)]  out WebSocketCommunicationDetails?  WebSocketCommunicationDetails,
                                       [NotNullWhen(false)] out String?                         ErrorResponse)

            => TryParse(JSON,
                        out WebSocketCommunicationDetails,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of WebSocket communication details.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="WebSocketCommunicationDetails">The parsed WebSocket communication details.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                  JSON,
                                       [NotNullWhen(true)]  out WebSocketCommunicationDetails?  WebSocketCommunicationDetails,
                                       [NotNullWhen(false)] out String?                         ErrorResponse,
                                       S2ParserOptions?                                         Options)

            => TryParse(JSON,
                        out WebSocketCommunicationDetails,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of WebSocket communication details.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="WebSocketCommunicationDetails">The parsed WebSocket communication details.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomWebSocketCommunicationDetailsParser">A delegate to parse custom WebSocket communication details.</param>
        public static Boolean TryParse(JObject                                                      JSON,
                                       [NotNullWhen(true)]  out WebSocketCommunicationDetails?      WebSocketCommunicationDetails,
                                       [NotNullWhen(false)] out String?                             ErrorResponse,
                                       S2ParserOptions?                                             Options,
                                       CustomJObjectParserDelegate<WebSocketCommunicationDetails>?  CustomWebSocketCommunicationDetailsParser)
        {

            try
            {

                WebSocketCommunicationDetails = null;

                #region communicationProtocol    [mandatory]

                if (!JSON.ParseMandatoryS2Enum("communicationProtocol",
                                               "communication protocol",
                                               Connect.CommunicationProtocol.TryParse,
                                               Options,
                                               out CommunicationProtocol communicationProtocol,
                                               out ErrorResponse))
                {
                    return false;
                }

                if (communicationProtocol != Connect.CommunicationProtocol.WebSocket)
                {
                    ErrorResponse = $"Invalid communication protocol 'communicationProtocol': 'WebSocket' is expected, but '{communicationProtocol}' was given!";
                    return false;
                }

                #endregion

                #region websocketToken           [mandatory]

                if (!JSON.ParseMandatoryS2String("websocketToken",
                                                 "WebSocket token",
                                                 out String? websocketTokenText,
                                                 out ErrorResponse))
                {
                    return false;
                }

                // The token value is a secret and is therefore not repeated in the error message.
                if (!CommunicationToken.TryParse(websocketTokenText, out var websocketToken))
                {
                    ErrorResponse = "Invalid WebSocket token 'websocketToken': a standard Base64 text is expected!";
                    return false;
                }

                #endregion

                #region websocketUrl             [mandatory]

                if (!JSON.ParseMandatoryS2URL("websocketUrl",
                                              "WebSocket URL",
                                              out URL websocketUrl,
                                              out ErrorResponse))
                {
                    return false;
                }

                // S2 Connect requires TLS everywhere: "wss://" is mandatory; plain "ws://" is accepted
                // only when the parser options explicitly allow insecure URLs (tests and development).
                if (websocketUrl.Scheme != URIScheme.wss &&
                   !(websocketUrl.Scheme == URIScheme.ws && (Options ?? S2ParserOptions.Default).AllowInsecureURLs))
                {
                    ErrorResponse = $"Invalid WebSocket URL 'websocketUrl': '{websocketUrl}' must use the scheme 'wss://'!";
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "communicationProtocol",
                                                    "websocketToken",
                                                    "websocketUrl"))
                {
                    return false;
                }

                #endregion


                WebSocketCommunicationDetails = new WebSocketCommunicationDetails(
                                                    websocketToken,
                                                    websocketUrl
                                                );

                if (CustomWebSocketCommunicationDetailsParser is not null)
                    WebSocketCommunicationDetails = CustomWebSocketCommunicationDetailsParser(JSON,
                                                                                              WebSocketCommunicationDetails);

                return true;

            }
            catch (Exception e)
            {
                WebSocketCommunicationDetails  = null;
                ErrorResponse                  = "The given JSON representation of WebSocket communication details is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomWebSocketCommunicationDetailsSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomWebSocketCommunicationDetailsSerializer">A delegate to serialize custom WebSocket communication details.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<WebSocketCommunicationDetails>? CustomWebSocketCommunicationDetailsSerializer)
        {

            var json = JSONObject.Create(
                           new JProperty("communicationProtocol",  CommunicationProtocol.ToString()),
                           new JProperty("websocketToken",         WebsocketToken.Value),
                           new JProperty("websocketUrl",           WebsocketUrl.ToString())
                       );

            return CustomWebSocketCommunicationDetailsSerializer is not null
                       ? CustomWebSocketCommunicationDetailsSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone these WebSocket communication details.
        /// </summary>
        public WebSocketCommunicationDetails Clone()

            => new (
                   WebsocketToken,          // an immutable readonly struct
                   WebsocketUrl.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two WebSocket communication details for equality.
        /// </summary>
        public static Boolean operator == (WebSocketCommunicationDetails? WebSocketCommunicationDetails1, WebSocketCommunicationDetails? WebSocketCommunicationDetails2)
        {

            if (ReferenceEquals(WebSocketCommunicationDetails1, WebSocketCommunicationDetails2))
                return true;

            if (WebSocketCommunicationDetails1 is null || WebSocketCommunicationDetails2 is null)
                return false;

            return WebSocketCommunicationDetails1.Equals(WebSocketCommunicationDetails2);

        }

        /// <summary>
        /// Compares two WebSocket communication details for inequality.
        /// </summary>
        public static Boolean operator != (WebSocketCommunicationDetails? WebSocketCommunicationDetails1, WebSocketCommunicationDetails? WebSocketCommunicationDetails2)
            => !(WebSocketCommunicationDetails1 == WebSocketCommunicationDetails2);

        #endregion

        #region IEquatable<WebSocketCommunicationDetails> Members

        /// <summary>
        /// Compares two WebSocket communication details for equality.
        /// </summary>
        /// <param name="Object">WebSocket communication details to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is WebSocketCommunicationDetails webSocketCommunicationDetails && Equals(webSocketCommunicationDetails);

        /// <summary>
        /// Compares two WebSocket communication details for equality.
        /// </summary>
        /// <param name="WebSocketCommunicationDetails">WebSocket communication details to compare with.</param>
        public Boolean Equals(WebSocketCommunicationDetails? WebSocketCommunicationDetails)

            => WebSocketCommunicationDetails is not null &&

               WebsocketToken.Equals(WebSocketCommunicationDetails.WebsocketToken) &&
               WebsocketUrl.  Equals(WebSocketCommunicationDetails.WebsocketUrl);

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
        /// Return a text representation of this object. The WebSocket token is redacted.
        /// </summary>
        public override String ToString()
            => $"WebSocket channel {WebsocketUrl} with {WebsocketToken}";

        #endregion

    }

}

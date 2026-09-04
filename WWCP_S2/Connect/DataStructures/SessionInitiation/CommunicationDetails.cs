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
    /// The abstract base of the communication details returned by the confirmAccessToken
    /// operation: how the client shall open the S2 message communication channel. The
    /// concrete type is selected by the discriminator "communicationProtocol"; S2 Connect 1.0
    /// defines <see cref="WebSocketCommunicationDetails"/> only
    /// (s2-connect-session-init.yml, CommunicationDetails and POST /confirmAccessToken, 200).
    /// </summary>
    public abstract class CommunicationDetails
    {

        #region Properties

        /// <summary>
        /// The communication protocol of the S2 message communication channel (the discriminator).
        /// </summary>
        [Mandatory]
        public abstract CommunicationProtocol  CommunicationProtocol    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create new communication details.
        /// </summary>
        protected CommunicationDetails()
        { }

        #endregion


        #region Documentation

        // s2-connect-session-init.yml
        //   /confirmAccessToken:
        //     post:
        //       operationId: confirmAccessToken
        //       security: [ accessToken ]
        //       description: An accessToken to authenticate at the initiateSession endpoint can only be used one time so
        //                    it crucial that the server knows certainly that client has properly stored it's access token.
        //                    By calling this endpoint the client informs the server that it has done so after which the
        //                    server shares the communication details.
        //       responses:
        //         '200':
        //           description: Confirmation received. Sharing the communication details.
        //           schema:
        //             oneOf:
        //               - $ref: WebSocketCommunicationDetails
        //             discriminator:
        //               propertyName: communicationProtocol
        //               mapping:
        //                 WebSocket: WebSocketCommunicationDetails
        //
        //   CommunicationDetails:
        //     type: object
        //     required: ["communicationProtocol"]
        //     properties:
        //       communicationProtocol:  { $ref: CommunicationProtocol }   (enum ["WebSocket"])
        //     discriminator:
        //       propertyName: communicationProtocol

        #endregion

        #region (static) TryParse(JSON, out CommunicationDetails, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of communication details,
        /// dispatching on the discriminator "communicationProtocol".
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CommunicationDetails">The parsed communication details.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out CommunicationDetails?  CommunicationDetails,
                                       [NotNullWhen(false)] out String?                ErrorResponse)

            => TryParse(JSON,
                        out CommunicationDetails,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of communication details,
        /// dispatching on the discriminator "communicationProtocol".
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CommunicationDetails">The parsed communication details.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out CommunicationDetails?  CommunicationDetails,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       S2ParserOptions?                                Options)

            => TryParse(JSON,
                        out CommunicationDetails,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of communication details,
        /// dispatching on the discriminator "communicationProtocol". A missing discriminator
        /// or a protocol without a known subtype is an error, even when the options tolerate
        /// unknown enumeration values.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CommunicationDetails">The parsed communication details.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomCommunicationDetailsParser">A delegate to parse custom communication details.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)]  out CommunicationDetails?      CommunicationDetails,
                                       [NotNullWhen(false)] out String?                    ErrorResponse,
                                       S2ParserOptions?                                    Options,
                                       CustomJObjectParserDelegate<CommunicationDetails>?  CustomCommunicationDetailsParser)
        {

            try
            {

                CommunicationDetails = null;

                #region communicationProtocol    [mandatory, discriminator]

                if (!JSON.ParseMandatoryS2Enum("communicationProtocol",
                                               "communication protocol",
                                               Connect.CommunicationProtocol.TryParse,
                                               Options,
                                               out CommunicationProtocol communicationProtocol,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Dispatch on the discriminator

                if (communicationProtocol == Connect.CommunicationProtocol.WebSocket)
                {

                    if (!WebSocketCommunicationDetails.TryParse(JSON,
                                                                out var webSocketCommunicationDetails,
                                                                out ErrorResponse,
                                                                Options))
                    {
                        return false;
                    }

                    CommunicationDetails = webSocketCommunicationDetails;

                }

                else
                {
                    // Even when the options tolerate unknown enumeration values, an unknown protocol
                    // is an error here: no subtype can parse its communication details.
                    ErrorResponse = $"Unknown communication protocol '{communicationProtocol}': no communication details can be parsed for it!";
                    return false;
                }

                #endregion


                if (CustomCommunicationDetailsParser is not null)
                    CommunicationDetails = CustomCommunicationDetailsParser(JSON,
                                                                            CommunicationDetails);

                return true;

            }
            catch (Exception e)
            {
                CommunicationDetails  = null;
                ErrorResponse         = "The given JSON representation of communication details is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        public abstract JObject ToJSON();

        #endregion

    }

}

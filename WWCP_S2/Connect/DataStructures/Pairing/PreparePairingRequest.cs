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
    /// The request body of POST /preparePairing (s2-connect-pairing.yml, LAN-LAN only): the
    /// client informs the server that the end user has started the process to pair one of
    /// the client's nodes with a node of the server, so that the server can e.g. show its
    /// pairing token in its user interface.
    /// </summary>
    public sealed class PreparePairingRequest : IEquatable<PreparePairingRequest>
    {

        #region Properties

        /// <summary>
        /// The description of the node of the client that is preparing to pair.
        /// </summary>
        [Mandatory]
        public NodeDescription      ClientNodeDescription        { get; }

        /// <summary>
        /// The description of the endpoint of the client.
        /// </summary>
        [Mandatory]
        public EndpointDescription  ClientEndpointDescription    { get; }

        /// <summary>
        /// The identification of the node of the server the client intends to pair with.
        /// </summary>
        [Mandatory]
        public Node_Id              ServerNodeId                 { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new prepare pairing request.
        /// </summary>
        /// <param name="ClientNodeDescription">The description of the node of the client that is preparing to pair.</param>
        /// <param name="ClientEndpointDescription">The description of the endpoint of the client.</param>
        /// <param name="ServerNodeId">The identification of the node of the server the client intends to pair with.</param>
        public PreparePairingRequest(NodeDescription      ClientNodeDescription,
                                     EndpointDescription  ClientEndpointDescription,
                                     Node_Id              ServerNodeId)
        {

            ArgumentNullException.ThrowIfNull(ClientNodeDescription);
            ArgumentNullException.ThrowIfNull(ClientEndpointDescription);

            this.ClientNodeDescription      = ClientNodeDescription;
            this.ClientEndpointDescription  = ClientEndpointDescription;
            this.ServerNodeId               = ServerNodeId;

            unchecked
            {
                hashCode = this.ClientNodeDescription.    GetHashCode() * 5 ^
                           this.ClientEndpointDescription.GetHashCode() * 3 ^
                           this.ServerNodeId.             GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   /preparePairing: post:
        //     summary: Inform the server that a node on the client is planning to attempt pairing with a node on the server.
        //     tags: [ LAN-LAN only extensions ]
        //     description: Notify the server that the end user has started the process on the client to pair with a node on
        //                  this server. It is up to the server implementation to decide what to do with this signal, but it
        //                  can be used to display a pop-up with the pairing token in its UI to improve the user experience.
        //                  ... When a preparePairing is called, it is not guaranteed that a call to pairingRequest or
        //                  cancelPreparePairing will follow.
        //     requestBody:
        //       description: A JSON message to inform the server which client is preparing to pair.
        //       content: application/json: schema:
        //         type: object
        //         required: ["clientNodeDescription", "clientEndpointDescription", "serverNodeId"]
        //         properties:
        //           clientNodeDescription:      { $ref: s2-connect-common.yml#/components/schemas/NodeDescription }
        //           clientEndpointDescription:  { $ref: s2-connect-common.yml#/components/schemas/EndpointDescription }
        //           serverNodeId:               { $ref: s2-connect-common.yml#/components/schemas/NodeId }   (string, format uuid)
        //     responses:
        //       '204': Notification received (also used when provided serverNodeId is not known).
        //       '400': PairingResponseErrorMessage - the server already notifies the client it is not willing to pair with the node.
        //       '401': The requests originates from outside the LAN
        //       '404': Not implemented (this is the recommended response from WAN endpoints)

        #endregion

        #region (static) TryParse(JSON, out PreparePairingRequest, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a prepare pairing request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PreparePairingRequest">The parsed prepare pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out PreparePairingRequest?  PreparePairingRequest,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)

            => TryParse(JSON,
                        out PreparePairingRequest,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a prepare pairing request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PreparePairingRequest">The parsed prepare pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out PreparePairingRequest?  PreparePairingRequest,
                                       [NotNullWhen(false)] out String?                 ErrorResponse,
                                       S2ParserOptions?                                 Options)

            => TryParse(JSON,
                        out PreparePairingRequest,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a prepare pairing request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PreparePairingRequest">The parsed prepare pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPreparePairingRequestParser">A delegate to parse custom prepare pairing requests.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out PreparePairingRequest?      PreparePairingRequest,
                                       [NotNullWhen(false)] out String?                     ErrorResponse,
                                       S2ParserOptions?                                     Options,
                                       CustomJObjectParserDelegate<PreparePairingRequest>?  CustomPreparePairingRequestParser)
        {

            try
            {

                PreparePairingRequest = null;

                #region clientNodeDescription        [mandatory]

                if (!JSON.ParseMandatoryS2("clientNodeDescription",
                                           "client node description",
                                           NodeDescription.TryParse,
                                           Options,
                                           out NodeDescription? clientNodeDescription,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region clientEndpointDescription    [mandatory]

                if (!JSON.ParseMandatoryS2("clientEndpointDescription",
                                           "client endpoint description",
                                           EndpointDescription.TryParse,
                                           Options,
                                           out EndpointDescription? clientEndpointDescription,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region serverNodeId                 [mandatory]

                if (!JSON.ParseMandatoryS2Id("serverNodeId",
                                             "server node identification",
                                             Node_Id.TryParse,
                                             Options,
                                             out Node_Id serverNodeId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "clientNodeDescription",
                                                    "clientEndpointDescription",
                                                    "serverNodeId"))
                {
                    return false;
                }

                #endregion


                PreparePairingRequest = new PreparePairingRequest(
                                            clientNodeDescription,
                                            clientEndpointDescription,
                                            serverNodeId
                                        );

                if (CustomPreparePairingRequestParser is not null)
                    PreparePairingRequest = CustomPreparePairingRequestParser(JSON,
                                                                              PreparePairingRequest);

                return true;

            }
            catch (Exception e)
            {
                PreparePairingRequest  = null;
                ErrorResponse          = "The given JSON representation of a prepare pairing request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPreparePairingRequestSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPreparePairingRequestSerializer">A delegate to serialize custom prepare pairing requests.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PreparePairingRequest>? CustomPreparePairingRequestSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("clientNodeDescription",      ClientNodeDescription.    ToJSON()),
                           new JProperty("clientEndpointDescription",  ClientEndpointDescription.ToJSON()),
                           new JProperty("serverNodeId",               ServerNodeId.ToString())
                       );

            return CustomPreparePairingRequestSerializer is not null
                       ? CustomPreparePairingRequestSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this prepare pairing request.
        /// </summary>
        public PreparePairingRequest Clone()

            => new (
                   ClientNodeDescription.    Clone(),
                   ClientEndpointDescription.Clone(),
                   ServerNodeId.             Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two prepare pairing requests for equality.
        /// </summary>
        public static Boolean operator == (PreparePairingRequest? PreparePairingRequest1, PreparePairingRequest? PreparePairingRequest2)
        {

            if (ReferenceEquals(PreparePairingRequest1, PreparePairingRequest2))
                return true;

            if (PreparePairingRequest1 is null || PreparePairingRequest2 is null)
                return false;

            return PreparePairingRequest1.Equals(PreparePairingRequest2);

        }

        /// <summary>
        /// Compares two prepare pairing requests for inequality.
        /// </summary>
        public static Boolean operator != (PreparePairingRequest? PreparePairingRequest1, PreparePairingRequest? PreparePairingRequest2)
            => !(PreparePairingRequest1 == PreparePairingRequest2);

        #endregion

        #region IEquatable<PreparePairingRequest> Members

        /// <summary>
        /// Compares two prepare pairing requests for equality.
        /// </summary>
        /// <param name="Object">A prepare pairing request to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PreparePairingRequest preparePairingRequest && Equals(preparePairingRequest);

        /// <summary>
        /// Compares two prepare pairing requests for equality.
        /// </summary>
        /// <param name="PreparePairingRequest">A prepare pairing request to compare with.</param>
        public Boolean Equals(PreparePairingRequest? PreparePairingRequest)

            => PreparePairingRequest is not null &&

               ClientNodeDescription.    Equals(PreparePairingRequest.ClientNodeDescription)     &&
               ClientEndpointDescription.Equals(PreparePairingRequest.ClientEndpointDescription) &&
               ServerNodeId.             Equals(PreparePairingRequest.ServerNodeId);

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
            => $"Prepare pairing of {ClientNodeDescription.Role} node {ClientNodeDescription.Id} with server node {ServerNodeId}";

        #endregion

    }

}

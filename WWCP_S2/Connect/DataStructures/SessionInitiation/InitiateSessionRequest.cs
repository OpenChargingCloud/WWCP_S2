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
    /// The request body of the initiateSession operation: the communication client
    /// identifies itself and the server node, announces the S2 message versions and the
    /// communication protocols it supports and may update its node and endpoint
    /// descriptions (s2-connect-session-init.yml, POST /initiateSession).
    /// </summary>
    public sealed class InitiateSessionRequest : IEquatable<InitiateSessionRequest>
    {

        #region Properties

        /// <summary>
        /// The identification of the client node that needs an access token
        /// (and the connection URL) for opening the S2 message communication channel.
        /// </summary>
        [Mandatory]
        public Node_Id                               ClientNodeId                       { get; }

        /// <summary>
        /// The identification of the server node the client wants to communicate with.
        /// </summary>
        [Mandatory]
        public Node_Id                               ServerNodeId                       { get; }

        /// <summary>
        /// The S2 message (protocol) versions supported by the client, at least one.
        /// The server picks one of them and returns it in the response; when no common
        /// version can be found, an error is returned.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<String>                 SupportedS2MessageVersions         { get; }

        /// <summary>
        /// The communication protocols supported by the client, at least one.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<CommunicationProtocol>  SupportedCommunicationProtocols    { get; }

        /// <summary>
        /// An optional (updated) description of the client node.
        /// When not provided the server will use the stored description.
        /// </summary>
        [Optional]
        public NodeDescription?                      ClientNodeDescription              { get; }

        /// <summary>
        /// An optional (updated) description of the client endpoint.
        /// When not provided the server will use the stored description.
        /// </summary>
        [Optional]
        public EndpointDescription?                  ClientEndpointDescription          { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new initiate session request.
        /// </summary>
        /// <param name="ClientNodeId">The identification of the client node.</param>
        /// <param name="ServerNodeId">The identification of the server node.</param>
        /// <param name="SupportedS2MessageVersions">The S2 message versions supported by the client (at least one).</param>
        /// <param name="SupportedCommunicationProtocols">The communication protocols supported by the client (at least one).</param>
        /// <param name="ClientNodeDescription">An optional (updated) description of the client node.</param>
        /// <param name="ClientEndpointDescription">An optional (updated) description of the client endpoint.</param>
        public InitiateSessionRequest(Node_Id                               ClientNodeId,
                                      Node_Id                               ServerNodeId,
                                      IReadOnlyList<String>                 SupportedS2MessageVersions,
                                      IReadOnlyList<CommunicationProtocol>  SupportedCommunicationProtocols,
                                      NodeDescription?                      ClientNodeDescription       = null,
                                      EndpointDescription?                  ClientEndpointDescription   = null)
        {

            ArgumentNullException.ThrowIfNull(SupportedS2MessageVersions);
            ArgumentNullException.ThrowIfNull(SupportedCommunicationProtocols);

            if (SupportedS2MessageVersions.Count < 1)
                throw new ArgumentException("The list of supported S2 message versions must contain at least one entry!",
                                            nameof(SupportedS2MessageVersions));

            if (SupportedCommunicationProtocols.Count < 1)
                throw new ArgumentException("The list of supported communication protocols must contain at least one entry!",
                                            nameof(SupportedCommunicationProtocols));

            this.ClientNodeId                     = ClientNodeId;
            this.ServerNodeId                     = ServerNodeId;
            this.SupportedS2MessageVersions       = [.. SupportedS2MessageVersions];
            this.SupportedCommunicationProtocols  = [.. SupportedCommunicationProtocols];
            this.ClientNodeDescription            = ClientNodeDescription;
            this.ClientEndpointDescription        = ClientEndpointDescription;

            unchecked
            {
                hashCode = this.ClientNodeId.                   GetHashCode()  * 17 ^
                           this.ServerNodeId.                   GetHashCode()  * 13 ^
                           this.SupportedS2MessageVersions.     CalcHashCode() * 11 ^
                           this.SupportedCommunicationProtocols.CalcHashCode() *  7 ^
                          (this.ClientNodeDescription?.         GetHashCode() ?? 0) * 5 ^
                          (this.ClientEndpointDescription?.     GetHashCode() ?? 0);
            }

        }

        #endregion


        #region Documentation

        // s2-connect-session-init.yml
        //   /initiateSession:
        //     post:
        //       operationId: initiateSession
        //       security: [ accessToken ]
        //       description: Start the session initiation process.
        //       requestBody:
        //         description: A json message with the identifier of the client that needs an access token
        //                      (and the connection URL) for opening the S2 message communication channel.
        //         schema:
        //           type: object
        //           required: ["clientNodeId", "serverNodeId", "supportedS2MessageVersions", "supportedCommunicationProtocols"]
        //           properties:
        //             clientNodeId:                     { $ref: NodeId }                        (string, format uuid)
        //             serverNodeId:                     { $ref: NodeId }
        //             supportedS2MessageVersions:       { type: array, items: { type: string } }
        //               description: List of supported protocols versions by the client. The server will pick one of the
        //                            supported protocols versions and return it in the connection details. If no common
        //                            protocol can be found, an error is returned.
        //             supportedCommunicationProtocols:  { type: array, items: { $ref: CommunicationProtocol } }   (enum ["WebSocket"])
        //             clientNodeDescription:            { $ref: NodeDescription }
        //               description: Optional field to provide (an updated) NodeDescription. When not provided the server
        //                            will use the stored description.
        //             clientEndpointDescription:        { $ref: EndpointDescription }
        //               description: Optional field to provide (an updated) EndpointDescription. When not provided the
        //                            server will use the stored description.
        //
        // Semantic rule: both lists must contain at least one entry (an empty list makes a negotiation impossible).

        #endregion

        #region (static) TryParse(JSON, out InitiateSessionRequest, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an initiate session request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="InitiateSessionRequest">The parsed initiate session request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out InitiateSessionRequest?  InitiateSessionRequest,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)

            => TryParse(JSON,
                        out InitiateSessionRequest,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an initiate session request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="InitiateSessionRequest">The parsed initiate session request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out InitiateSessionRequest?  InitiateSessionRequest,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options)

            => TryParse(JSON,
                        out InitiateSessionRequest,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an initiate session request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="InitiateSessionRequest">The parsed initiate session request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomInitiateSessionRequestParser">A delegate to parse custom initiate session requests.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out InitiateSessionRequest?      InitiateSessionRequest,
                                       [NotNullWhen(false)] out String?                      ErrorResponse,
                                       S2ParserOptions?                                      Options,
                                       CustomJObjectParserDelegate<InitiateSessionRequest>?  CustomInitiateSessionRequestParser)
        {

            try
            {

                InitiateSessionRequest = null;

                #region clientNodeId                       [mandatory]

                if (!JSON.ParseMandatoryS2Id("clientNodeId",
                                             "client node identification",
                                             Node_Id.TryParse,
                                             Options,
                                             out Node_Id clientNodeId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region serverNodeId                       [mandatory]

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

                #region supportedS2MessageVersions         [mandatory]

                if (!JSON.ParseMandatoryS2Strings("supportedS2MessageVersions",
                                                  "supported S2 message versions",
                                                  null,
                                                  null,
                                                  out IReadOnlyList<String>? supportedS2MessageVersions,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region supportedCommunicationProtocols    [mandatory]

                if (!JSON.ParseMandatoryS2Enums("supportedCommunicationProtocols",
                                                "supported communication protocols",
                                                CommunicationProtocol.TryParse,
                                                Options,
                                                null,
                                                null,
                                                out IReadOnlyList<CommunicationProtocol>? supportedCommunicationProtocols,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region clientNodeDescription              [optional]

                if (!JSON.ParseOptionalS2("clientNodeDescription",
                                          "client node description",
                                          NodeDescription.TryParse,
                                          Options,
                                          out NodeDescription? clientNodeDescription,
                                          out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region clientEndpointDescription          [optional]

                if (!JSON.ParseOptionalS2("clientEndpointDescription",
                                          "client endpoint description",
                                          EndpointDescription.TryParse,
                                          Options,
                                          out EndpointDescription? clientEndpointDescription,
                                          out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "clientNodeId",
                                                    "serverNodeId",
                                                    "supportedS2MessageVersions",
                                                    "supportedCommunicationProtocols",
                                                    "clientNodeDescription",
                                                    "clientEndpointDescription"))
                {
                    return false;
                }

                #endregion


                InitiateSessionRequest = new InitiateSessionRequest(
                                             clientNodeId,
                                             serverNodeId,
                                             supportedS2MessageVersions,
                                             supportedCommunicationProtocols,
                                             clientNodeDescription,
                                             clientEndpointDescription
                                         );

                if (CustomInitiateSessionRequestParser is not null)
                    InitiateSessionRequest = CustomInitiateSessionRequestParser(JSON,
                                                                                InitiateSessionRequest);

                return true;

            }
            catch (Exception e)
            {
                InitiateSessionRequest  = null;
                ErrorResponse           = "The given JSON representation of an initiate session request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomInitiateSessionRequestSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomInitiateSessionRequestSerializer">A delegate to serialize custom initiate session requests.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<InitiateSessionRequest>? CustomInitiateSessionRequestSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("clientNodeId",                     ClientNodeId.ToString()),
                                 new JProperty("serverNodeId",                     ServerNodeId.ToString()),
                                 new JProperty("supportedS2MessageVersions",       new JArray(SupportedS2MessageVersions)),
                                 new JProperty("supportedCommunicationProtocols",  new JArray(SupportedCommunicationProtocols.Select(protocol => protocol.ToString()))),

                           ClientNodeDescription is not null
                               ? new JProperty("clientNodeDescription",            ClientNodeDescription.ToJSON())
                               : null,

                           ClientEndpointDescription is not null
                               ? new JProperty("clientEndpointDescription",        ClientEndpointDescription.ToJSON())
                               : null

                       );

            return CustomInitiateSessionRequestSerializer is not null
                       ? CustomInitiateSessionRequestSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this initiate session request.
        /// </summary>
        public InitiateSessionRequest Clone()

            => new (
                   ClientNodeId.Clone(),
                   ServerNodeId.Clone(),
                   SupportedS2MessageVersions.     Select(version  => version. CloneString()).ToList(),
                   SupportedCommunicationProtocols.Select(protocol => protocol.Clone()).      ToList(),
                   ClientNodeDescription?.    Clone(),
                   ClientEndpointDescription?.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two initiate session requests for equality.
        /// </summary>
        public static Boolean operator == (InitiateSessionRequest? InitiateSessionRequest1, InitiateSessionRequest? InitiateSessionRequest2)
        {

            if (ReferenceEquals(InitiateSessionRequest1, InitiateSessionRequest2))
                return true;

            if (InitiateSessionRequest1 is null || InitiateSessionRequest2 is null)
                return false;

            return InitiateSessionRequest1.Equals(InitiateSessionRequest2);

        }

        /// <summary>
        /// Compares two initiate session requests for inequality.
        /// </summary>
        public static Boolean operator != (InitiateSessionRequest? InitiateSessionRequest1, InitiateSessionRequest? InitiateSessionRequest2)
            => !(InitiateSessionRequest1 == InitiateSessionRequest2);

        #endregion

        #region IEquatable<InitiateSessionRequest> Members

        /// <summary>
        /// Compares two initiate session requests for equality.
        /// </summary>
        /// <param name="Object">An initiate session request to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is InitiateSessionRequest initiateSessionRequest && Equals(initiateSessionRequest);

        /// <summary>
        /// Compares two initiate session requests for equality.
        /// </summary>
        /// <param name="InitiateSessionRequest">An initiate session request to compare with.</param>
        public Boolean Equals(InitiateSessionRequest? InitiateSessionRequest)

            => InitiateSessionRequest is not null &&

               ClientNodeId.Equals(InitiateSessionRequest.ClientNodeId) &&
               ServerNodeId.Equals(InitiateSessionRequest.ServerNodeId) &&

               SupportedS2MessageVersions.     SequenceEqual(InitiateSessionRequest.SupportedS2MessageVersions, StringComparer.Ordinal) &&
               SupportedCommunicationProtocols.SequenceEqual(InitiateSessionRequest.SupportedCommunicationProtocols) &&

               ClientNodeDescription     == InitiateSessionRequest.ClientNodeDescription &&
               ClientEndpointDescription == InitiateSessionRequest.ClientEndpointDescription;

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

            => $"Initiate session of client {ClientNodeId} with server {ServerNodeId}: " +
               $"versions [{String.Join(", ", SupportedS2MessageVersions)}], " +
               $"protocols [{String.Join(", ", SupportedCommunicationProtocols)}]";

        #endregion

    }

}

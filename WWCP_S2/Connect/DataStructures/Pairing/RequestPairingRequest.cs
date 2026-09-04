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
    /// The request body of POST /requestPairing (s2-connect-pairing.yml): the pairing client
    /// describes its node and endpoint, addresses the targeted node of the server (by node id,
    /// by node id alias, or not at all), lists the communication protocols, S2 message versions
    /// and HMAC hashing algorithms it supports, and provides its HMAC challenge.
    /// </summary>
    public sealed class RequestPairingRequest : IEquatable<RequestPairingRequest>
    {

        #region Properties

        /// <summary>
        /// The description of the node of the pairing client.
        /// </summary>
        [Mandatory]
        public NodeDescription                       ClientNodeDescription              { get; }

        /// <summary>
        /// The description of the endpoint of the pairing client.
        /// </summary>
        [Mandatory]
        public EndpointDescription                   ClientEndpointDescription          { get; }

        /// <summary>
        /// The targeted node of the pairing server: by node id ("nodeId"), by node id alias
        /// ("nodeIdAlias"), or the only node of the endpoint (neither property is written).
        /// Never null.
        /// </summary>
        [Optional]
        public PairingTarget                         Target                             { get; }

        /// <summary>
        /// The communication protocols the client supports for exchanging S2 messages (at least one).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<CommunicationProtocol>  SupportedCommunicationProtocols    { get; }

        /// <summary>
        /// The versions of the S2 JSON message schemas this node implementation currently supports (at least one).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<String>                 SupportedS2MessageVersions         { get; }

        /// <summary>
        /// The HMAC hashing algorithms the client supports for the challenge-response process;
        /// SHA256 is always among them (S2 Connect 1.0: "currently only SHA256 is supported and must be present").
        /// </summary>
        [Mandatory]
        public IReadOnlyList<HmacHashingAlgorithm>   SupportedHmacHashingAlgorithms     { get; }

        /// <summary>
        /// The HMAC challenge of the client (at least 32 random bytes), to be answered by the server.
        /// </summary>
        [Mandatory]
        public HmacChallenge                         ClientHmacChallenge                { get; }

        /// <summary>
        /// Whether the server shall attempt pairing even though the S2 message versions are not
        /// compatible; the nodes will not be able to communicate until a software update.
        /// Defaults to false and is only written when true.
        /// </summary>
        [Optional]
        public Boolean                               ForcePairing                       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new request pairing request.
        /// </summary>
        /// <param name="ClientNodeDescription">The description of the node of the pairing client.</param>
        /// <param name="ClientEndpointDescription">The description of the endpoint of the pairing client.</param>
        /// <param name="SupportedCommunicationProtocols">The communication protocols the client supports (at least one).</param>
        /// <param name="SupportedS2MessageVersions">The S2 JSON message versions the client supports (at least one).</param>
        /// <param name="SupportedHmacHashingAlgorithms">The HMAC hashing algorithms the client supports (must contain SHA256).</param>
        /// <param name="ClientHmacChallenge">The HMAC challenge of the client (at least 32 bytes).</param>
        /// <param name="Target">The optional targeted node of the server; the only node of the endpoint when omitted.</param>
        /// <param name="ForcePairing">Whether to pair even though the S2 message versions are not compatible.</param>
        public RequestPairingRequest(NodeDescription                       ClientNodeDescription,
                                     EndpointDescription                   ClientEndpointDescription,
                                     IReadOnlyList<CommunicationProtocol>  SupportedCommunicationProtocols,
                                     IReadOnlyList<String>                 SupportedS2MessageVersions,
                                     IReadOnlyList<HmacHashingAlgorithm>   SupportedHmacHashingAlgorithms,
                                     HmacChallenge                         ClientHmacChallenge,
                                     PairingTarget?                        Target         = null,
                                     Boolean                               ForcePairing   = false)
        {

            ArgumentNullException.ThrowIfNull(ClientNodeDescription);
            ArgumentNullException.ThrowIfNull(ClientEndpointDescription);
            ArgumentNullException.ThrowIfNull(SupportedCommunicationProtocols);
            ArgumentNullException.ThrowIfNull(SupportedS2MessageVersions);
            ArgumentNullException.ThrowIfNull(SupportedHmacHashingAlgorithms);

            if (SupportedCommunicationProtocols.Count == 0)
                throw new ArgumentException("The list of supported communication protocols must not be empty!",
                                            nameof(SupportedCommunicationProtocols));

            if (SupportedS2MessageVersions.Count == 0)
                throw new ArgumentException("The list of supported S2 message versions must not be empty!",
                                            nameof(SupportedS2MessageVersions));

            if (SupportedHmacHashingAlgorithms.Count == 0)
                throw new ArgumentException("The list of supported HMAC hashing algorithms must not be empty!",
                                            nameof(SupportedHmacHashingAlgorithms));

            if (!SupportedHmacHashingAlgorithms.Contains(HmacHashingAlgorithm.SHA256))
                throw new ArgumentException($"The list of supported HMAC hashing algorithms must contain '{HmacHashingAlgorithm.SHA256}' (S2 Connect 1.0: currently only SHA256 is supported and must be present)!",
                                            nameof(SupportedHmacHashingAlgorithms));

            if (ClientHmacChallenge.Length < HmacChallenge.MinimumBytes)
                throw new ArgumentException($"The client HMAC challenge must have at least {HmacChallenge.MinimumBytes} bytes!",
                                            nameof(ClientHmacChallenge));

            this.ClientNodeDescription            = ClientNodeDescription;
            this.ClientEndpointDescription        = ClientEndpointDescription;
            this.Target                           = Target ?? PairingTarget.Any;
            this.SupportedCommunicationProtocols  = [.. SupportedCommunicationProtocols];
            this.SupportedS2MessageVersions       = [.. SupportedS2MessageVersions];
            this.SupportedHmacHashingAlgorithms   = [.. SupportedHmacHashingAlgorithms];
            this.ClientHmacChallenge              = ClientHmacChallenge;
            this.ForcePairing                     = ForcePairing;

            unchecked
            {
                hashCode = this.ClientNodeDescription.          GetHashCode()  * 23 ^
                           this.ClientEndpointDescription.      GetHashCode()  * 19 ^
                           this.Target.                         GetHashCode()  * 17 ^
                           this.SupportedCommunicationProtocols.CalcHashCode() * 13 ^
                           this.SupportedS2MessageVersions.     CalcHashCode() * 11 ^
                           this.SupportedHmacHashingAlgorithms. CalcHashCode() *  7 ^
                           this.ClientHmacChallenge.            GetHashCode()  *  5 ^
                           this.ForcePairing.                   GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   /requestPairing: post: requestBody: content: application/json: schema:
        //     type: object
        //     required: ["clientNodeDescription", "clientEndpointDescription", "supportedCommunicationProtocols",
        //                "supportedS2MessageVersions", "supportedHmacHashingAlgorithms", "clientHmacChallenge"]
        //     properties:
        //       clientNodeDescription:            { $ref: s2-connect-common.yml#/components/schemas/NodeDescription }
        //       clientEndpointDescription:        { $ref: s2-connect-common.yml#/components/schemas/EndpointDescription }
        //       nodeId:                           { $ref: s2-connect-common.yml#/components/schemas/NodeId }   (string, format uuid)
        //       nodeIdAlias:                      { $ref: NodeIdAlias }                                      (string, pattern ^[0-9a-zA-Z]+$)
        //       supportedCommunicationProtocols:  { type: array, items: { $ref: CommunicationProtocol } }    (enum ["WebSocket"])
        //       supportedS2MessageVersions:       { type: array, items: { type: string },
        //                                           description: The versions of the S2 JSON message schemas this node
        //                                                        implementation currently supports. }
        //       supportedHmacHashingAlgorithms:   { type: array, items: { $ref: HmacHashingAlgorithm } }     (enum ["SHA256"])
        //       clientHmacChallenge:              { $ref: HmacChallenge }                                    (string, format byte, at least 32 bytes)
        //       forcePairing:                     { type: boolean, default: false,
        //                                           description: Forces the server to attempt pairing, even though the S2 message
        //                                                        versions are not compatible. In this case the nodes won't be able to
        //                                                        communicate after pairing, but this could later be solved through a
        //                                                        software update on one or both of the nodes. }
        //
        //   Operation description: "The (optional) properties nodeId and nodeIdAlias may never be used at the same time.
        //   When the client knows the NodeId of the node represented by the server it must provide a value for nodeId
        //   (and not nodeIdAlias). If it knows the NodeIdAlias (which can be part of the pairing code that was entered by
        //   the end user) of the node it must provide a value for nodeIdAlias (and not nodeId). If it doesn't know any,
        //   but expects the endpoint to represent only one node (which can be the case in LAN deployments) it doesn't
        //   provide a value for nodeId and nodeIdAlias."
        //
        // S2 Connect 1.0.0, "1. POST /[version]/requestPairing", supportedHmacHashingAlgorithms:
        //   "currently only SHA256 is supported and must be present".
        //
        // Semantic rules (validated in the constructor and when parsing): nodeId and nodeIdAlias are never given
        // together (PairingTarget); the three lists must not be empty; supportedHmacHashingAlgorithms must contain
        // SHA256; the client challenge has at least 32 bytes. A missing forcePairing means false.

        #endregion

        #region (static) TryParse(JSON, out RequestPairingRequest, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a request pairing request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RequestPairingRequest">The parsed request pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out RequestPairingRequest?  RequestPairingRequest,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)

            => TryParse(JSON,
                        out RequestPairingRequest,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a request pairing request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RequestPairingRequest">The parsed request pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out RequestPairingRequest?  RequestPairingRequest,
                                       [NotNullWhen(false)] out String?                 ErrorResponse,
                                       S2ParserOptions?                                 Options)

            => TryParse(JSON,
                        out RequestPairingRequest,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a request pairing request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RequestPairingRequest">The parsed request pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomRequestPairingRequestParser">A delegate to parse custom request pairing requests.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out RequestPairingRequest?      RequestPairingRequest,
                                       [NotNullWhen(false)] out String?                     ErrorResponse,
                                       S2ParserOptions?                                     Options,
                                       CustomJObjectParserDelegate<RequestPairingRequest>?  CustomRequestPairingRequestParser)
        {

            try
            {

                RequestPairingRequest = null;

                #region clientNodeDescription              [mandatory]

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

                #region clientEndpointDescription          [mandatory]

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

                #region nodeId                             [optional]

                if (!JSON.ParseOptionalS2Id("nodeId",
                                            "node identification",
                                            Node_Id.TryParse,
                                            Options,
                                            out Node_Id? nodeId,
                                            out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region nodeIdAlias                        [optional]

                // Not parsed as an identifier: an alias is deliberately not a UUID (S2ParserOptions.RequireUUIDs).
                if (!JSON.ParseOptionalS2String("nodeIdAlias",
                                                "node id alias",
                                                out String? nodeIdAliasText,
                                                out ErrorResponse))
                {
                    return false;
                }

                NodeIdAlias? nodeIdAlias = null;

                if (nodeIdAliasText is not null)
                {

                    if (!NodeIdAlias.TryParse(nodeIdAliasText, out var alias))
                    {
                        ErrorResponse = $"Invalid node id alias 'nodeIdAlias': '{nodeIdAliasText}' does not match {S2ConnectDefaults.NodeIdAliasRegExpr}!";
                        return false;
                    }

                    nodeIdAlias = alias;

                }

                if (nodeId.HasValue && nodeIdAlias.HasValue)
                {
                    ErrorResponse = "The properties 'nodeId' and 'nodeIdAlias' may never be used at the same time!";
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

                #region supportedHmacHashingAlgorithms     [mandatory]

                if (!JSON.ParseMandatoryS2Enums("supportedHmacHashingAlgorithms",
                                                "supported HMAC hashing algorithms",
                                                HmacHashingAlgorithm.TryParse,
                                                Options,
                                                null,
                                                null,
                                                out IReadOnlyList<HmacHashingAlgorithm>? supportedHmacHashingAlgorithms,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region clientHmacChallenge                [mandatory]

                if (!JSON.ParseMandatoryS2String("clientHmacChallenge",
                                                 "client HMAC challenge",
                                                 out String? clientHmacChallengeText,
                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!HmacChallenge.TryParse(clientHmacChallengeText,
                                            out HmacChallenge clientHmacChallenge))
                {
                    ErrorResponse = $"Invalid client HMAC challenge 'clientHmacChallenge': a standard Base64 text of at least {HmacChallenge.MinimumBytes} bytes is expected!";
                    return false;
                }

                #endregion

                #region forcePairing                       [optional]

                if (!JSON.ParseOptionalS2Boolean("forcePairing",
                                                 "force pairing flag",
                                                 out Boolean? forcePairing,
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
                                                    "nodeId",
                                                    "nodeIdAlias",
                                                    "supportedCommunicationProtocols",
                                                    "supportedS2MessageVersions",
                                                    "supportedHmacHashingAlgorithms",
                                                    "clientHmacChallenge",
                                                    "forcePairing"))
                {
                    return false;
                }

                #endregion


                RequestPairingRequest = new RequestPairingRequest(
                                            clientNodeDescription,
                                            clientEndpointDescription,
                                            supportedCommunicationProtocols,
                                            supportedS2MessageVersions,
                                            supportedHmacHashingAlgorithms,
                                            clientHmacChallenge,
                                            PairingTarget.From(nodeId, nodeIdAlias),
                                            forcePairing ?? false
                                        );

                if (CustomRequestPairingRequestParser is not null)
                    RequestPairingRequest = CustomRequestPairingRequestParser(JSON,
                                                                              RequestPairingRequest);

                return true;

            }
            catch (Exception e)
            {
                RequestPairingRequest  = null;
                ErrorResponse          = "The given JSON representation of a request pairing request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomRequestPairingRequestSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomRequestPairingRequestSerializer">A delegate to serialize custom request pairing requests.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<RequestPairingRequest>? CustomRequestPairingRequestSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("clientNodeDescription",            ClientNodeDescription.    ToJSON()),
                                 new JProperty("clientEndpointDescription",        ClientEndpointDescription.ToJSON()),

                           Target.NodeId.HasValue
                               ? new JProperty("nodeId",                           Target.NodeId.     Value.ToString())
                               : null,

                           Target.NodeIdAlias.HasValue
                               ? new JProperty("nodeIdAlias",                      Target.NodeIdAlias.Value.ToString())
                               : null,

                                 new JProperty("supportedCommunicationProtocols",  new JArray(SupportedCommunicationProtocols.Select(protocol  => protocol. ToString()))),
                                 new JProperty("supportedS2MessageVersions",       new JArray(SupportedS2MessageVersions)),
                                 new JProperty("supportedHmacHashingAlgorithms",   new JArray(SupportedHmacHashingAlgorithms. Select(algorithm => algorithm.ToString()))),
                                 new JProperty("clientHmacChallenge",              ClientHmacChallenge.Value),

                           ForcePairing
                               ? new JProperty("forcePairing",                     true)
                               : null

                       );

            return CustomRequestPairingRequestSerializer is not null
                       ? CustomRequestPairingRequestSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this request pairing request.
        /// </summary>
        public RequestPairingRequest Clone()

            => new (
                   ClientNodeDescription.    Clone(),
                   ClientEndpointDescription.Clone(),
                   [.. SupportedCommunicationProtocols.Select(protocol  => protocol. Clone())],
                   [.. SupportedS2MessageVersions.     Select(version   => version.  CloneString())],
                   [.. SupportedHmacHashingAlgorithms. Select(algorithm => algorithm.Clone())],
                   ClientHmacChallenge,
                   Target,
                   ForcePairing
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two request pairing requests for equality.
        /// </summary>
        public static Boolean operator == (RequestPairingRequest? RequestPairingRequest1, RequestPairingRequest? RequestPairingRequest2)
        {

            if (ReferenceEquals(RequestPairingRequest1, RequestPairingRequest2))
                return true;

            if (RequestPairingRequest1 is null || RequestPairingRequest2 is null)
                return false;

            return RequestPairingRequest1.Equals(RequestPairingRequest2);

        }

        /// <summary>
        /// Compares two request pairing requests for inequality.
        /// </summary>
        public static Boolean operator != (RequestPairingRequest? RequestPairingRequest1, RequestPairingRequest? RequestPairingRequest2)
            => !(RequestPairingRequest1 == RequestPairingRequest2);

        #endregion

        #region IEquatable<RequestPairingRequest> Members

        /// <summary>
        /// Compares two request pairing requests for equality.
        /// </summary>
        /// <param name="Object">A request pairing request to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is RequestPairingRequest requestPairingRequest && Equals(requestPairingRequest);

        /// <summary>
        /// Compares two request pairing requests for equality.
        /// </summary>
        /// <param name="RequestPairingRequest">A request pairing request to compare with.</param>
        public Boolean Equals(RequestPairingRequest? RequestPairingRequest)

            => RequestPairingRequest is not null &&

               ClientNodeDescription.    Equals(RequestPairingRequest.ClientNodeDescription)     &&
               ClientEndpointDescription.Equals(RequestPairingRequest.ClientEndpointDescription) &&
               Target.                   Equals(RequestPairingRequest.Target)                    &&
               ClientHmacChallenge.      Equals(RequestPairingRequest.ClientHmacChallenge)       &&
               ForcePairing ==                  RequestPairingRequest.ForcePairing               &&

               SupportedCommunicationProtocols.SequenceEqual(RequestPairingRequest.SupportedCommunicationProtocols) &&
               SupportedS2MessageVersions.     SequenceEqual(RequestPairingRequest.SupportedS2MessageVersions, StringComparer.Ordinal) &&
               SupportedHmacHashingAlgorithms. SequenceEqual(RequestPairingRequest.SupportedHmacHashingAlgorithms);

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
        /// Return a text representation of this object (the challenge is redacted).
        /// </summary>
        public override String ToString()

            => String.Concat(

                   $"Pairing request of {ClientNodeDescription.Role} node {ClientNodeDescription.Id} targeting {Target}",
                   $", S2 versions {String.Join(", ", SupportedS2MessageVersions)}",

                   ForcePairing
                       ? ", forced"
                       : ""

               );

        #endregion

    }

}

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
    /// The request body of POST /cancelPreparePairing (s2-connect-pairing.yml, LAN-LAN only):
    /// the client cancels a previous preparePairing notification, because the end user has
    /// stopped the process to pair the client node with the server node.
    /// </summary>
    public sealed class CancelPreparePairingRequest : IEquatable<CancelPreparePairingRequest>
    {

        #region Properties

        /// <summary>
        /// The identification of the node of the client that cancels the pairing preparation.
        /// </summary>
        [Mandatory]
        public Node_Id  ClientNodeId    { get; }

        /// <summary>
        /// The identification of the targeted node of the server.
        /// </summary>
        [Mandatory]
        public Node_Id  ServerNodeId    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new cancel prepare pairing request.
        /// </summary>
        /// <param name="ClientNodeId">The identification of the node of the client that cancels the pairing preparation.</param>
        /// <param name="ServerNodeId">The identification of the targeted node of the server.</param>
        public CancelPreparePairingRequest(Node_Id  ClientNodeId,
                                           Node_Id  ServerNodeId)
        {

            this.ClientNodeId  = ClientNodeId;
            this.ServerNodeId  = ServerNodeId;

            unchecked
            {
                hashCode = this.ClientNodeId.GetHashCode() * 3 ^
                           this.ServerNodeId.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   /cancelPreparePairing: post:
        //     summary: Cancel a previous call to preparePairing
        //     tags: [ LAN-LAN only extensions ]
        //     description: Cancel a previous notification of the preparePairing. This happens when the end user has stopped
        //                  the process to pair a node on the client with a node on the server. It is up to the server
        //                  implementation to decide what to do with this signal, but it can for example be used to close the
        //                  pop-up showing the pairing token that was opened after preparePairing was called.
        //     requestBody:
        //       description: The clientNodeId of the client cancelling the pairing, as well as te targeted serverNodeId.
        //       content: application/json: schema:
        //         type: object
        //         required: ["clientNodeId", "serverNodeId"]
        //         properties:
        //           clientNodeId:  { $ref: s2-connect-common.yml#/components/schemas/NodeId }   (string, format uuid)
        //           serverNodeId:  { $ref: s2-connect-common.yml#/components/schemas/NodeId }   (string, format uuid)
        //     responses:
        //       '204': Cancellation received (also used when provided clientNodeId or serverNode is not known).
        //       '401': The requests originates from outside the LAN
        //       '404': Not implemented (this is the recommended response from WAN endpoints)

        #endregion

        #region (static) TryParse(JSON, out CancelPreparePairingRequest, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a cancel prepare pairing request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CancelPreparePairingRequest">The parsed cancel prepare pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out CancelPreparePairingRequest?  CancelPreparePairingRequest,
                                       [NotNullWhen(false)] out String?                       ErrorResponse)

            => TryParse(JSON,
                        out CancelPreparePairingRequest,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a cancel prepare pairing request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CancelPreparePairingRequest">The parsed cancel prepare pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out CancelPreparePairingRequest?  CancelPreparePairingRequest,
                                       [NotNullWhen(false)] out String?                       ErrorResponse,
                                       S2ParserOptions?                                       Options)

            => TryParse(JSON,
                        out CancelPreparePairingRequest,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a cancel prepare pairing request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CancelPreparePairingRequest">The parsed cancel prepare pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomCancelPreparePairingRequestParser">A delegate to parse custom cancel prepare pairing requests.</param>
        public static Boolean TryParse(JObject                                                    JSON,
                                       [NotNullWhen(true)]  out CancelPreparePairingRequest?      CancelPreparePairingRequest,
                                       [NotNullWhen(false)] out String?                           ErrorResponse,
                                       S2ParserOptions?                                           Options,
                                       CustomJObjectParserDelegate<CancelPreparePairingRequest>?  CustomCancelPreparePairingRequestParser)
        {

            try
            {

                CancelPreparePairingRequest = null;

                #region clientNodeId    [mandatory]

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

                #region serverNodeId    [mandatory]

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
                                                    "clientNodeId",
                                                    "serverNodeId"))
                {
                    return false;
                }

                #endregion


                CancelPreparePairingRequest = new CancelPreparePairingRequest(
                                                  clientNodeId,
                                                  serverNodeId
                                              );

                if (CustomCancelPreparePairingRequestParser is not null)
                    CancelPreparePairingRequest = CustomCancelPreparePairingRequestParser(JSON,
                                                                                          CancelPreparePairingRequest);

                return true;

            }
            catch (Exception e)
            {
                CancelPreparePairingRequest  = null;
                ErrorResponse                = "The given JSON representation of a cancel prepare pairing request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomCancelPreparePairingRequestSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomCancelPreparePairingRequestSerializer">A delegate to serialize custom cancel prepare pairing requests.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<CancelPreparePairingRequest>? CustomCancelPreparePairingRequestSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("clientNodeId",  ClientNodeId.ToString()),
                           new JProperty("serverNodeId",  ServerNodeId.ToString())
                       );

            return CustomCancelPreparePairingRequestSerializer is not null
                       ? CustomCancelPreparePairingRequestSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this cancel prepare pairing request.
        /// </summary>
        public CancelPreparePairingRequest Clone()

            => new (
                   ClientNodeId.Clone(),
                   ServerNodeId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two cancel prepare pairing requests for equality.
        /// </summary>
        public static Boolean operator == (CancelPreparePairingRequest? CancelPreparePairingRequest1, CancelPreparePairingRequest? CancelPreparePairingRequest2)
        {

            if (ReferenceEquals(CancelPreparePairingRequest1, CancelPreparePairingRequest2))
                return true;

            if (CancelPreparePairingRequest1 is null || CancelPreparePairingRequest2 is null)
                return false;

            return CancelPreparePairingRequest1.Equals(CancelPreparePairingRequest2);

        }

        /// <summary>
        /// Compares two cancel prepare pairing requests for inequality.
        /// </summary>
        public static Boolean operator != (CancelPreparePairingRequest? CancelPreparePairingRequest1, CancelPreparePairingRequest? CancelPreparePairingRequest2)
            => !(CancelPreparePairingRequest1 == CancelPreparePairingRequest2);

        #endregion

        #region IEquatable<CancelPreparePairingRequest> Members

        /// <summary>
        /// Compares two cancel prepare pairing requests for equality.
        /// </summary>
        /// <param name="Object">A cancel prepare pairing request to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is CancelPreparePairingRequest cancelPreparePairingRequest && Equals(cancelPreparePairingRequest);

        /// <summary>
        /// Compares two cancel prepare pairing requests for equality.
        /// </summary>
        /// <param name="CancelPreparePairingRequest">A cancel prepare pairing request to compare with.</param>
        public Boolean Equals(CancelPreparePairingRequest? CancelPreparePairingRequest)

            => CancelPreparePairingRequest is not null &&

               ClientNodeId.Equals(CancelPreparePairingRequest.ClientNodeId) &&
               ServerNodeId.Equals(CancelPreparePairingRequest.ServerNodeId);

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
            => $"Cancel prepare pairing of client node {ClientNodeId} with server node {ServerNodeId}";

        #endregion

    }

}

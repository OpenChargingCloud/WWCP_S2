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
    /// The request body of the unpair operation: the communication client asks the server
    /// to unpair the two nodes (s2-connect-session-init.yml, POST /unpair).
    /// </summary>
    public sealed class UnpairRequest : IEquatable<UnpairRequest>
    {

        #region Properties

        /// <summary>
        /// The identification of the client node that should be unpaired.
        /// </summary>
        [Mandatory]
        public Node_Id  ClientNodeId    { get; }

        /// <summary>
        /// The identification of the server node the client was paired with.
        /// </summary>
        [Mandatory]
        public Node_Id  ServerNodeId    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new unpair request.
        /// </summary>
        /// <param name="ClientNodeId">The identification of the client node that should be unpaired.</param>
        /// <param name="ServerNodeId">The identification of the server node the client was paired with.</param>
        public UnpairRequest(Node_Id  ClientNodeId,
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

        // s2-connect-session-init.yml
        //   /unpair:
        //     post:
        //       operationId: unpair
        //       security: [ accessToken ]
        //       description: Perform unpair operation for a specific client.
        //       requestBody:
        //         description: A json message with the identifier of the client that should be unpaired.
        //         schema:
        //           type: object
        //           required: ["clientNodeId", "serverNodeId"]
        //           properties:
        //             clientNodeId:  { $ref: NodeId }   (string, format uuid)
        //             serverNodeId:  { $ref: NodeId }
        //       responses:
        //         '204': Unpairing successful.
        //         '401': Unauthorized, clientNodeId or serverNodeId not found or accessToken not accepted
        //                (this could be because the nodes are already unpaired).

        #endregion

        #region (static) TryParse(JSON, out UnpairRequest, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an unpair request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="UnpairRequest">The parsed unpair request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                  JSON,
                                       [NotNullWhen(true)]  out UnpairRequest?  UnpairRequest,
                                       [NotNullWhen(false)] out String?         ErrorResponse)

            => TryParse(JSON,
                        out UnpairRequest,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an unpair request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="UnpairRequest">The parsed unpair request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                  JSON,
                                       [NotNullWhen(true)]  out UnpairRequest?  UnpairRequest,
                                       [NotNullWhen(false)] out String?         ErrorResponse,
                                       S2ParserOptions?                         Options)

            => TryParse(JSON,
                        out UnpairRequest,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an unpair request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="UnpairRequest">The parsed unpair request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomUnpairRequestParser">A delegate to parse custom unpair requests.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out UnpairRequest?      UnpairRequest,
                                       [NotNullWhen(false)] out String?             ErrorResponse,
                                       S2ParserOptions?                             Options,
                                       CustomJObjectParserDelegate<UnpairRequest>?  CustomUnpairRequestParser)
        {

            try
            {

                UnpairRequest = null;

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


                UnpairRequest = new UnpairRequest(
                                    clientNodeId,
                                    serverNodeId
                                );

                if (CustomUnpairRequestParser is not null)
                    UnpairRequest = CustomUnpairRequestParser(JSON,
                                                              UnpairRequest);

                return true;

            }
            catch (Exception e)
            {
                UnpairRequest  = null;
                ErrorResponse  = "The given JSON representation of an unpair request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomUnpairRequestSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomUnpairRequestSerializer">A delegate to serialize custom unpair requests.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<UnpairRequest>? CustomUnpairRequestSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("clientNodeId",  ClientNodeId.ToString()),
                           new JProperty("serverNodeId",  ServerNodeId.ToString())
                       );

            return CustomUnpairRequestSerializer is not null
                       ? CustomUnpairRequestSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this unpair request.
        /// </summary>
        public UnpairRequest Clone()

            => new (
                   ClientNodeId.Clone(),
                   ServerNodeId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two unpair requests for equality.
        /// </summary>
        public static Boolean operator == (UnpairRequest? UnpairRequest1, UnpairRequest? UnpairRequest2)
        {

            if (ReferenceEquals(UnpairRequest1, UnpairRequest2))
                return true;

            if (UnpairRequest1 is null || UnpairRequest2 is null)
                return false;

            return UnpairRequest1.Equals(UnpairRequest2);

        }

        /// <summary>
        /// Compares two unpair requests for inequality.
        /// </summary>
        public static Boolean operator != (UnpairRequest? UnpairRequest1, UnpairRequest? UnpairRequest2)
            => !(UnpairRequest1 == UnpairRequest2);

        #endregion

        #region IEquatable<UnpairRequest> Members

        /// <summary>
        /// Compares two unpair requests for equality.
        /// </summary>
        /// <param name="Object">An unpair request to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is UnpairRequest unpairRequest && Equals(unpairRequest);

        /// <summary>
        /// Compares two unpair requests for equality.
        /// </summary>
        /// <param name="UnpairRequest">An unpair request to compare with.</param>
        public Boolean Equals(UnpairRequest? UnpairRequest)

            => UnpairRequest is not null &&

               ClientNodeId.Equals(UnpairRequest.ClientNodeId) &&
               ServerNodeId.Equals(UnpairRequest.ServerNodeId);

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
            => $"Unpair client {ClientNodeId} from server {ServerNodeId}";

        #endregion

    }

}

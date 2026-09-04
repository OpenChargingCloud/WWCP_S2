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
    /// One item of the request body of POST /waitForPairing (s2-connect-pairing.yml, LAN-LAN
    /// only, long polling): a node of the client that is available for pairing. By default
    /// only the client node id is given; the node and endpoint descriptions follow the
    /// 'sendNodeDescription' action of the server, and the error message reports that a
    /// requested action could not be performed.
    /// </summary>
    public sealed class WaitForPairingRequestItem : IEquatable<WaitForPairingRequestItem>
    {

        #region Properties

        /// <summary>
        /// The identification of the node of the client.
        /// </summary>
        [Mandatory]
        public Node_Id               ClientNodeId                 { get; }

        /// <summary>
        /// The optional description of the node of the client, provided in the request that
        /// follows the 'sendNodeDescription' action of the server.
        /// </summary>
        [Optional]
        public NodeDescription?      ClientNodeDescription        { get; }

        /// <summary>
        /// The optional description of the endpoint of the client, provided in the request that
        /// follows the 'sendNodeDescription' action of the server.
        /// </summary>
        [Optional]
        public EndpointDescription?  ClientEndpointDescription    { get; }

        /// <summary>
        /// The optional error the client reports for this node, e.g. that it has no valid
        /// pairing token although it was asked to request pairing.
        /// </summary>
        [Optional]
        public WaitForPairingError?  ErrorMessage                 { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new wait-for-pairing request item.
        /// </summary>
        /// <param name="ClientNodeId">The identification of the node of the client.</param>
        /// <param name="ClientNodeDescription">An optional description of the node of the client.</param>
        /// <param name="ClientEndpointDescription">An optional description of the endpoint of the client.</param>
        /// <param name="ErrorMessage">An optional error the client reports for this node.</param>
        public WaitForPairingRequestItem(Node_Id               ClientNodeId,
                                         NodeDescription?      ClientNodeDescription       = null,
                                         EndpointDescription?  ClientEndpointDescription   = null,
                                         WaitForPairingError?  ErrorMessage                = null)
        {

            this.ClientNodeId               = ClientNodeId;
            this.ClientNodeDescription      = ClientNodeDescription;
            this.ClientEndpointDescription  = ClientEndpointDescription;
            this.ErrorMessage               = ErrorMessage;

            unchecked
            {
                hashCode = this.ClientNodeId.              GetHashCode()       * 7 ^
                          (this.ClientNodeDescription?.    GetHashCode() ?? 0) * 5 ^
                          (this.ClientEndpointDescription?.GetHashCode() ?? 0) * 3 ^
                          (this.ErrorMessage?.             GetHashCode() ?? 0);
            }

        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   /waitForPairing: post: requestBody:
        //     description: By default the only data that is provided in the request is the clientNodeId. Only when the
        //                  server has responded with the 'sendNodeDescription' action, the client must provide the
        //                  NodeDescription and the EndpointDescription data in the next request.
        //     content: application/json: schema:
        //       type: array
        //       items:
        //         type: object
        //         required: ["clientNodeId"]
        //         properties:
        //           clientNodeId:               { $ref: s2-connect-common.yml#/components/schemas/NodeId }   (string, format uuid)
        //           clientNodeDescription:      { $ref: s2-connect-common.yml#/components/schemas/NodeDescription }
        //           clientEndpointDescription:  { $ref: s2-connect-common.yml#/components/schemas/EndpointDescription }
        //           errorMessage:               { type: string, enum: ["NoValidTokenOnPairingClient"] }

        #endregion

        #region (static) TryParse(JSON, out WaitForPairingRequestItem, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a wait-for-pairing request item.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="WaitForPairingRequestItem">The parsed wait-for-pairing request item.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out WaitForPairingRequestItem?  WaitForPairingRequestItem,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)

            => TryParse(JSON,
                        out WaitForPairingRequestItem,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a wait-for-pairing request item.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="WaitForPairingRequestItem">The parsed wait-for-pairing request item.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out WaitForPairingRequestItem?  WaitForPairingRequestItem,
                                       [NotNullWhen(false)] out String?                     ErrorResponse,
                                       S2ParserOptions?                                     Options)

            => TryParse(JSON,
                        out WaitForPairingRequestItem,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a wait-for-pairing request item.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="WaitForPairingRequestItem">The parsed wait-for-pairing request item.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomWaitForPairingRequestItemParser">A delegate to parse custom wait-for-pairing request items.</param>
        public static Boolean TryParse(JObject                                                  JSON,
                                       [NotNullWhen(true)]  out WaitForPairingRequestItem?      WaitForPairingRequestItem,
                                       [NotNullWhen(false)] out String?                         ErrorResponse,
                                       S2ParserOptions?                                         Options,
                                       CustomJObjectParserDelegate<WaitForPairingRequestItem>?  CustomWaitForPairingRequestItemParser)
        {

            try
            {

                WaitForPairingRequestItem = null;

                #region clientNodeId                 [mandatory]

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

                #region clientNodeDescription        [optional]

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

                #region clientEndpointDescription    [optional]

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

                #region errorMessage                 [optional]

                if (!JSON.ParseOptionalS2Enum("errorMessage",
                                              "error message",
                                              WaitForPairingError.TryParse,
                                              Options,
                                              out WaitForPairingError? errorMessage,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "clientNodeId",
                                                    "clientNodeDescription",
                                                    "clientEndpointDescription",
                                                    "errorMessage"))
                {
                    return false;
                }

                #endregion


                WaitForPairingRequestItem = new WaitForPairingRequestItem(
                                                clientNodeId,
                                                clientNodeDescription,
                                                clientEndpointDescription,
                                                errorMessage
                                            );

                if (CustomWaitForPairingRequestItemParser is not null)
                    WaitForPairingRequestItem = CustomWaitForPairingRequestItemParser(JSON,
                                                                                      WaitForPairingRequestItem);

                return true;

            }
            catch (Exception e)
            {
                WaitForPairingRequestItem  = null;
                ErrorResponse              = "The given JSON representation of a wait-for-pairing request item is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomWaitForPairingRequestItemSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomWaitForPairingRequestItemSerializer">A delegate to serialize custom wait-for-pairing request items.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<WaitForPairingRequestItem>? CustomWaitForPairingRequestItemSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("clientNodeId",               ClientNodeId.ToString()),

                           ClientNodeDescription is not null
                               ? new JProperty("clientNodeDescription",      ClientNodeDescription.    ToJSON())
                               : null,

                           ClientEndpointDescription is not null
                               ? new JProperty("clientEndpointDescription",  ClientEndpointDescription.ToJSON())
                               : null,

                           ErrorMessage.HasValue
                               ? new JProperty("errorMessage",               ErrorMessage.Value.ToString())
                               : null

                       );

            return CustomWaitForPairingRequestItemSerializer is not null
                       ? CustomWaitForPairingRequestItemSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this wait-for-pairing request item.
        /// </summary>
        public WaitForPairingRequestItem Clone()

            => new (
                   ClientNodeId.              Clone(),
                   ClientNodeDescription?.    Clone(),
                   ClientEndpointDescription?.Clone(),
                   ErrorMessage?.             Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two wait-for-pairing request items for equality.
        /// </summary>
        public static Boolean operator == (WaitForPairingRequestItem? WaitForPairingRequestItem1, WaitForPairingRequestItem? WaitForPairingRequestItem2)
        {

            if (ReferenceEquals(WaitForPairingRequestItem1, WaitForPairingRequestItem2))
                return true;

            if (WaitForPairingRequestItem1 is null || WaitForPairingRequestItem2 is null)
                return false;

            return WaitForPairingRequestItem1.Equals(WaitForPairingRequestItem2);

        }

        /// <summary>
        /// Compares two wait-for-pairing request items for inequality.
        /// </summary>
        public static Boolean operator != (WaitForPairingRequestItem? WaitForPairingRequestItem1, WaitForPairingRequestItem? WaitForPairingRequestItem2)
            => !(WaitForPairingRequestItem1 == WaitForPairingRequestItem2);

        #endregion

        #region IEquatable<WaitForPairingRequestItem> Members

        /// <summary>
        /// Compares two wait-for-pairing request items for equality.
        /// </summary>
        /// <param name="Object">A wait-for-pairing request item to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is WaitForPairingRequestItem waitForPairingRequestItem && Equals(waitForPairingRequestItem);

        /// <summary>
        /// Compares two wait-for-pairing request items for equality.
        /// </summary>
        /// <param name="WaitForPairingRequestItem">A wait-for-pairing request item to compare with.</param>
        public Boolean Equals(WaitForPairingRequestItem? WaitForPairingRequestItem)

            => WaitForPairingRequestItem is not null &&

               ClientNodeId.Equals(WaitForPairingRequestItem.ClientNodeId) &&

               ClientNodeDescription     == WaitForPairingRequestItem.ClientNodeDescription     &&
               ClientEndpointDescription == WaitForPairingRequestItem.ClientEndpointDescription &&

               Nullable.Equals(ErrorMessage, WaitForPairingRequestItem.ErrorMessage);

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

            => String.Concat(

                   $"Client node {ClientNodeId}",

                   ClientNodeDescription is not null
                       ? $" ({ClientNodeDescription.Brand} {ClientNodeDescription.ModelName})"
                       : "",

                   ErrorMessage.HasValue
                       ? $", error: {ErrorMessage.Value}"
                       : ""

               );

        #endregion

    }

}

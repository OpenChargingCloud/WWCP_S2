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
    /// One item of the 200 response body of POST /waitForPairing (s2-connect-pairing.yml,
    /// LAN-LAN only, long polling): the action the server asks the client to perform for
    /// one of its nodes.
    /// </summary>
    public sealed class WaitForPairingResponseItem : IEquatable<WaitForPairingResponseItem>
    {

        #region Properties

        /// <summary>
        /// The identification of the node of the client the action applies to.
        /// </summary>
        [Mandatory]
        public Node_Id               ClientNodeId    { get; }

        /// <summary>
        /// The action the client shall perform for the node: sendNodeDescription,
        /// preparePairing, cancelPreparePairing or requestPairing.
        /// </summary>
        [Mandatory]
        public WaitForPairingAction  Action          { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new wait-for-pairing response item.
        /// </summary>
        /// <param name="ClientNodeId">The identification of the node of the client the action applies to.</param>
        /// <param name="Action">The action the client shall perform for the node.</param>
        public WaitForPairingResponseItem(Node_Id               ClientNodeId,
                                          WaitForPairingAction  Action)
        {

            if (Action.IsNullOrEmpty)
                throw new ArgumentException("The action must not be empty!", nameof(Action));

            this.ClientNodeId  = ClientNodeId;
            this.Action        = Action;

            unchecked
            {
                hashCode = this.ClientNodeId.GetHashCode() * 3 ^
                           this.Action.      GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   /waitForPairing: post: responses: '200':
        //     description: Message telling the client what to do next. The server may only provide at most one item for
        //                  each clientNodeId.
        //     content: application/json: schema:
        //       type: array
        //       minItems: 1
        //       items:
        //         type: object
        //         required: ["clientNodeId", "action"]
        //         properties:
        //           clientNodeId:  { $ref: s2-connect-common.yml#/components/schemas/NodeId }   (string, format uuid)
        //           action:        { type: string,
        //                            enum: ["sendNodeDescription", "preparePairing", "cancelPreparePairing", "requestPairing"] }

        #endregion

        #region (static) TryParse(JSON, out WaitForPairingResponseItem, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a wait-for-pairing response item.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="WaitForPairingResponseItem">The parsed wait-for-pairing response item.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out WaitForPairingResponseItem?  WaitForPairingResponseItem,
                                       [NotNullWhen(false)] out String?                      ErrorResponse)

            => TryParse(JSON,
                        out WaitForPairingResponseItem,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a wait-for-pairing response item.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="WaitForPairingResponseItem">The parsed wait-for-pairing response item.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out WaitForPairingResponseItem?  WaitForPairingResponseItem,
                                       [NotNullWhen(false)] out String?                      ErrorResponse,
                                       S2ParserOptions?                                      Options)

            => TryParse(JSON,
                        out WaitForPairingResponseItem,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a wait-for-pairing response item.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="WaitForPairingResponseItem">The parsed wait-for-pairing response item.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomWaitForPairingResponseItemParser">A delegate to parse custom wait-for-pairing response items.</param>
        public static Boolean TryParse(JObject                                                   JSON,
                                       [NotNullWhen(true)]  out WaitForPairingResponseItem?      WaitForPairingResponseItem,
                                       [NotNullWhen(false)] out String?                          ErrorResponse,
                                       S2ParserOptions?                                          Options,
                                       CustomJObjectParserDelegate<WaitForPairingResponseItem>?  CustomWaitForPairingResponseItemParser)
        {

            try
            {

                WaitForPairingResponseItem = null;

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

                #region action          [mandatory]

                if (!JSON.ParseMandatoryS2Enum("action",
                                               "action",
                                               WaitForPairingAction.TryParse,
                                               Options,
                                               out WaitForPairingAction action,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "clientNodeId",
                                                    "action"))
                {
                    return false;
                }

                #endregion


                WaitForPairingResponseItem = new WaitForPairingResponseItem(
                                                 clientNodeId,
                                                 action
                                             );

                if (CustomWaitForPairingResponseItemParser is not null)
                    WaitForPairingResponseItem = CustomWaitForPairingResponseItemParser(JSON,
                                                                                        WaitForPairingResponseItem);

                return true;

            }
            catch (Exception e)
            {
                WaitForPairingResponseItem  = null;
                ErrorResponse               = "The given JSON representation of a wait-for-pairing response item is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomWaitForPairingResponseItemSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomWaitForPairingResponseItemSerializer">A delegate to serialize custom wait-for-pairing response items.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<WaitForPairingResponseItem>? CustomWaitForPairingResponseItemSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("clientNodeId",  ClientNodeId.ToString()),
                           new JProperty("action",        Action.      ToString())
                       );

            return CustomWaitForPairingResponseItemSerializer is not null
                       ? CustomWaitForPairingResponseItemSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this wait-for-pairing response item.
        /// </summary>
        public WaitForPairingResponseItem Clone()

            => new (
                   ClientNodeId.Clone(),
                   Action.      Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two wait-for-pairing response items for equality.
        /// </summary>
        public static Boolean operator == (WaitForPairingResponseItem? WaitForPairingResponseItem1, WaitForPairingResponseItem? WaitForPairingResponseItem2)
        {

            if (ReferenceEquals(WaitForPairingResponseItem1, WaitForPairingResponseItem2))
                return true;

            if (WaitForPairingResponseItem1 is null || WaitForPairingResponseItem2 is null)
                return false;

            return WaitForPairingResponseItem1.Equals(WaitForPairingResponseItem2);

        }

        /// <summary>
        /// Compares two wait-for-pairing response items for inequality.
        /// </summary>
        public static Boolean operator != (WaitForPairingResponseItem? WaitForPairingResponseItem1, WaitForPairingResponseItem? WaitForPairingResponseItem2)
            => !(WaitForPairingResponseItem1 == WaitForPairingResponseItem2);

        #endregion

        #region IEquatable<WaitForPairingResponseItem> Members

        /// <summary>
        /// Compares two wait-for-pairing response items for equality.
        /// </summary>
        /// <param name="Object">A wait-for-pairing response item to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is WaitForPairingResponseItem waitForPairingResponseItem && Equals(waitForPairingResponseItem);

        /// <summary>
        /// Compares two wait-for-pairing response items for equality.
        /// </summary>
        /// <param name="WaitForPairingResponseItem">A wait-for-pairing response item to compare with.</param>
        public Boolean Equals(WaitForPairingResponseItem? WaitForPairingResponseItem)

            => WaitForPairingResponseItem is not null &&

               ClientNodeId.Equals(WaitForPairingResponseItem.ClientNodeId) &&
               Action.      Equals(WaitForPairingResponseItem.Action);

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
            => $"Client node {ClientNodeId}: {Action}";

        #endregion

    }

}

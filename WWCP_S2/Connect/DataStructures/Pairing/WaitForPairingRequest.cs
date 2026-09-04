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
    /// The request body of POST /waitForPairing (s2-connect-pairing.yml, LAN-LAN only, long
    /// polling): the list of the nodes of the client that are available for pairing, one item
    /// per node. The body is a JSON array, not an object.
    /// </summary>
    public sealed class WaitForPairingRequest : IEquatable<WaitForPairingRequest>
    {

        #region Properties

        /// <summary>
        /// The nodes of the client that are available for pairing (at least one, unique client node ids).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<WaitForPairingRequestItem>  Items    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new wait-for-pairing request.
        /// </summary>
        /// <param name="Items">The nodes of the client that are available for pairing (at least one, unique client node ids).</param>
        public WaitForPairingRequest(IReadOnlyList<WaitForPairingRequestItem> Items)
        {

            ArgumentNullException.ThrowIfNull(Items);

            if (Items.Count == 0)
                throw new ArgumentException("A wait-for-pairing request must contain at least one item!",
                                            nameof(Items));

            var duplicate = Items.GroupBy(item => item.ClientNodeId).FirstOrDefault(group => group.Count() > 1);

            if (duplicate is not null)
                throw new ArgumentException($"The client node identifications of a wait-for-pairing request must be unique, but '{duplicate.Key}' occurs {duplicate.Count()} times!",
                                            nameof(Items));

            this.Items = [.. Items];

            unchecked
            {
                hashCode = this.Items.CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   /waitForPairing: post:
        //     summary: Long polling operation to indicate to the server that the client is available for pairing.
        //     tags: [ LAN-LAN only extensions ]
        //     description: Long polling path for indicating to a server that a client is available for pairing. The client
        //                  calls this operation on the server. The server will wait with its response until it wants the
        //                  client to take action, or when a predetermined timer is exceeded. Once the server responds, the
        //                  client immediately stars a new request to the server. The client does this even when it is
        //                  already paired. Note that the client can represent multiple nodes so the request body and the
        //                  response contains a list.
        //     requestBody:
        //       description: By default the only data that is provided in the request is the clientNodeId. Only when the
        //                    server has responded with the 'sendNodeDescription' action, the client must provide the
        //                    NodeDescription and the EndpointDescription data in the next request.
        //       content: application/json: schema:
        //         type: array
        //         items: { type: object, required: ["clientNodeId"], ... }   (see WaitForPairingRequestItem)
        //     responses:
        //       '200': WaitForPairingResponse (JSON array, minItems 1)
        //       '204': No action for the client, try again.
        //       '400': The server is not available for long polling.
        //       '401': The requests originates from outside the LAN. Do not attempt to connect again.
        //       '404': Not implemented (this is the recommended response from WAN endpoints)
        //       '503': Long-polling temporarily not available, try again later.
        //
        // Semantic rules (validated in the constructor): at least one item; every client node id occurs at most once.

        #endregion

        #region (static) TryParse(JSON, out WaitForPairingRequest, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON array representation of a wait-for-pairing request.
        /// </summary>
        /// <param name="JSON">The JSON array to be parsed.</param>
        /// <param name="WaitForPairingRequest">The parsed wait-for-pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JArray                                           JSON,
                                       [NotNullWhen(true)]  out WaitForPairingRequest?  WaitForPairingRequest,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)

            => TryParse(JSON,
                        out WaitForPairingRequest,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON array representation of a wait-for-pairing request.
        /// </summary>
        /// <param name="JSON">The JSON array to be parsed.</param>
        /// <param name="WaitForPairingRequest">The parsed wait-for-pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JArray                                           JSON,
                                       [NotNullWhen(true)]  out WaitForPairingRequest?  WaitForPairingRequest,
                                       [NotNullWhen(false)] out String?                 ErrorResponse,
                                       S2ParserOptions?                                 Options)

            => TryParse(JSON,
                        out WaitForPairingRequest,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON array representation of a wait-for-pairing request.
        /// </summary>
        /// <param name="JSON">The JSON array to be parsed.</param>
        /// <param name="WaitForPairingRequest">The parsed wait-for-pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomWaitForPairingRequestParser">A delegate to parse custom wait-for-pairing requests.</param>
        public static Boolean TryParse(JArray                                              JSON,
                                       [NotNullWhen(true)]  out WaitForPairingRequest?     WaitForPairingRequest,
                                       [NotNullWhen(false)] out String?                    ErrorResponse,
                                       S2ParserOptions?                                    Options,
                                       CustomJArrayParserDelegate<WaitForPairingRequest>?  CustomWaitForPairingRequestParser)
        {

            try
            {

                WaitForPairingRequest = null;

                #region items    [mandatory]

                var items  = new List<WaitForPairingRequestItem>(JSON.Count);
                var index  = 0;

                foreach (var token in JSON)
                {

                    if (token is not JObject itemJSON)
                    {
                        ErrorResponse = $"Invalid wait-for-pairing request: item {index} is not a JSON object!";
                        return false;
                    }

                    if (!WaitForPairingRequestItem.TryParse(itemJSON,
                                                            out var item,
                                                            out var itemError,
                                                            Options))
                    {
                        ErrorResponse = $"Invalid wait-for-pairing request: item {index}: {itemError}";
                        return false;
                    }

                    items.Add(item);
                    index++;

                }

                #endregion


                WaitForPairingRequest = new WaitForPairingRequest(
                                            items
                                        );

                if (CustomWaitForPairingRequestParser is not null)
                    WaitForPairingRequest = CustomWaitForPairingRequestParser(JSON,
                                                                              WaitForPairingRequest);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                WaitForPairingRequest  = null;
                ErrorResponse          = "The given JSON representation of a wait-for-pairing request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomWaitForPairingRequestSerializer = null)

        /// <summary>
        /// Return the JSON array representation of this object.
        /// </summary>
        /// <param name="CustomWaitForPairingRequestSerializer">A delegate to serialize custom wait-for-pairing requests.</param>
        public JArray ToJSON(CustomJArraySerializerDelegate<WaitForPairingRequest>? CustomWaitForPairingRequestSerializer = null)
        {

            var json = new JArray(Items.Select(item => item.ToJSON()));

            return CustomWaitForPairingRequestSerializer is not null
                       ? CustomWaitForPairingRequestSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this wait-for-pairing request.
        /// </summary>
        public WaitForPairingRequest Clone()

            => new (
                   [.. Items.Select(item => item.Clone())]
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two wait-for-pairing requests for equality.
        /// </summary>
        public static Boolean operator == (WaitForPairingRequest? WaitForPairingRequest1, WaitForPairingRequest? WaitForPairingRequest2)
        {

            if (ReferenceEquals(WaitForPairingRequest1, WaitForPairingRequest2))
                return true;

            if (WaitForPairingRequest1 is null || WaitForPairingRequest2 is null)
                return false;

            return WaitForPairingRequest1.Equals(WaitForPairingRequest2);

        }

        /// <summary>
        /// Compares two wait-for-pairing requests for inequality.
        /// </summary>
        public static Boolean operator != (WaitForPairingRequest? WaitForPairingRequest1, WaitForPairingRequest? WaitForPairingRequest2)
            => !(WaitForPairingRequest1 == WaitForPairingRequest2);

        #endregion

        #region IEquatable<WaitForPairingRequest> Members

        /// <summary>
        /// Compares two wait-for-pairing requests for equality.
        /// </summary>
        /// <param name="Object">A wait-for-pairing request to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is WaitForPairingRequest waitForPairingRequest && Equals(waitForPairingRequest);

        /// <summary>
        /// Compares two wait-for-pairing requests for equality (same items in the same order).
        /// </summary>
        /// <param name="WaitForPairingRequest">A wait-for-pairing request to compare with.</param>
        public Boolean Equals(WaitForPairingRequest? WaitForPairingRequest)

            => WaitForPairingRequest is not null &&

               Items.SequenceEqual(WaitForPairingRequest.Items);

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
            => $"Wait-for-pairing request for {Items.Count} node(s): {String.Join(", ", Items.Select(item => item.ClientNodeId))}";

        #endregion

    }

}

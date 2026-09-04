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
    /// The 200 response body of POST /waitForPairing (s2-connect-pairing.yml, LAN-LAN only,
    /// long polling): the actions the server asks the client to perform, at most one per
    /// client node. The body is a JSON array (minItems 1), not an object; a server without
    /// an action answers 204 instead.
    /// </summary>
    public sealed class WaitForPairingResponse : IEquatable<WaitForPairingResponse>
    {

        #region Properties

        /// <summary>
        /// The actions the client shall perform (at least one, at most one per client node id).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<WaitForPairingResponseItem>  Items    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new wait-for-pairing response.
        /// </summary>
        /// <param name="Items">The actions the client shall perform (at least one, at most one per client node id).</param>
        public WaitForPairingResponse(IReadOnlyList<WaitForPairingResponseItem> Items)
        {

            ArgumentNullException.ThrowIfNull(Items);

            if (Items.Count == 0)
                throw new ArgumentException("A wait-for-pairing response must contain at least one item!",
                                            nameof(Items));

            var duplicate = Items.GroupBy(item => item.ClientNodeId).FirstOrDefault(group => group.Count() > 1);

            if (duplicate is not null)
                throw new ArgumentException($"A wait-for-pairing response may contain at most one item per client node identification, but '{duplicate.Key}' occurs {duplicate.Count()} times!",
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
        //   /waitForPairing: post: responses: '200':
        //     description: Message telling the client what to do next. The server may only provide at most one item for
        //                  each clientNodeId.
        //     content: application/json: schema:
        //       type: array
        //       minItems: 1
        //       items: { type: object, required: ["clientNodeId", "action"], ... }   (see WaitForPairingResponseItem)
        //
        //   '204': No action for the client, try again.
        //
        // Semantic rules (validated in the constructor): at least one item (minItems 1); at most one item per client node id.

        #endregion

        #region (static) TryParse(JSON, out WaitForPairingResponse, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON array representation of a wait-for-pairing response.
        /// </summary>
        /// <param name="JSON">The JSON array to be parsed.</param>
        /// <param name="WaitForPairingResponse">The parsed wait-for-pairing response.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JArray                                            JSON,
                                       [NotNullWhen(true)]  out WaitForPairingResponse?  WaitForPairingResponse,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)

            => TryParse(JSON,
                        out WaitForPairingResponse,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON array representation of a wait-for-pairing response.
        /// </summary>
        /// <param name="JSON">The JSON array to be parsed.</param>
        /// <param name="WaitForPairingResponse">The parsed wait-for-pairing response.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JArray                                            JSON,
                                       [NotNullWhen(true)]  out WaitForPairingResponse?  WaitForPairingResponse,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options)

            => TryParse(JSON,
                        out WaitForPairingResponse,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON array representation of a wait-for-pairing response.
        /// </summary>
        /// <param name="JSON">The JSON array to be parsed.</param>
        /// <param name="WaitForPairingResponse">The parsed wait-for-pairing response.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomWaitForPairingResponseParser">A delegate to parse custom wait-for-pairing responses.</param>
        public static Boolean TryParse(JArray                                               JSON,
                                       [NotNullWhen(true)]  out WaitForPairingResponse?     WaitForPairingResponse,
                                       [NotNullWhen(false)] out String?                     ErrorResponse,
                                       S2ParserOptions?                                     Options,
                                       CustomJArrayParserDelegate<WaitForPairingResponse>?  CustomWaitForPairingResponseParser)
        {

            try
            {

                WaitForPairingResponse = null;

                #region items    [mandatory, minItems 1]

                var items  = new List<WaitForPairingResponseItem>(JSON.Count);
                var index  = 0;

                foreach (var token in JSON)
                {

                    if (token is not JObject itemJSON)
                    {
                        ErrorResponse = $"Invalid wait-for-pairing response: item {index} is not a JSON object!";
                        return false;
                    }

                    if (!WaitForPairingResponseItem.TryParse(itemJSON,
                                                             out var item,
                                                             out var itemError,
                                                             Options))
                    {
                        ErrorResponse = $"Invalid wait-for-pairing response: item {index}: {itemError}";
                        return false;
                    }

                    items.Add(item);
                    index++;

                }

                #endregion


                WaitForPairingResponse = new WaitForPairingResponse(
                                             items
                                         );

                if (CustomWaitForPairingResponseParser is not null)
                    WaitForPairingResponse = CustomWaitForPairingResponseParser(JSON,
                                                                                WaitForPairingResponse);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                WaitForPairingResponse  = null;
                ErrorResponse           = "The given JSON representation of a wait-for-pairing response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomWaitForPairingResponseSerializer = null)

        /// <summary>
        /// Return the JSON array representation of this object.
        /// </summary>
        /// <param name="CustomWaitForPairingResponseSerializer">A delegate to serialize custom wait-for-pairing responses.</param>
        public JArray ToJSON(CustomJArraySerializerDelegate<WaitForPairingResponse>? CustomWaitForPairingResponseSerializer = null)
        {

            var json = new JArray(Items.Select(item => item.ToJSON()));

            return CustomWaitForPairingResponseSerializer is not null
                       ? CustomWaitForPairingResponseSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this wait-for-pairing response.
        /// </summary>
        public WaitForPairingResponse Clone()

            => new (
                   [.. Items.Select(item => item.Clone())]
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two wait-for-pairing responses for equality.
        /// </summary>
        public static Boolean operator == (WaitForPairingResponse? WaitForPairingResponse1, WaitForPairingResponse? WaitForPairingResponse2)
        {

            if (ReferenceEquals(WaitForPairingResponse1, WaitForPairingResponse2))
                return true;

            if (WaitForPairingResponse1 is null || WaitForPairingResponse2 is null)
                return false;

            return WaitForPairingResponse1.Equals(WaitForPairingResponse2);

        }

        /// <summary>
        /// Compares two wait-for-pairing responses for inequality.
        /// </summary>
        public static Boolean operator != (WaitForPairingResponse? WaitForPairingResponse1, WaitForPairingResponse? WaitForPairingResponse2)
            => !(WaitForPairingResponse1 == WaitForPairingResponse2);

        #endregion

        #region IEquatable<WaitForPairingResponse> Members

        /// <summary>
        /// Compares two wait-for-pairing responses for equality.
        /// </summary>
        /// <param name="Object">A wait-for-pairing response to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is WaitForPairingResponse waitForPairingResponse && Equals(waitForPairingResponse);

        /// <summary>
        /// Compares two wait-for-pairing responses for equality (same items in the same order).
        /// </summary>
        /// <param name="WaitForPairingResponse">A wait-for-pairing response to compare with.</param>
        public Boolean Equals(WaitForPairingResponse? WaitForPairingResponse)

            => WaitForPairingResponse is not null &&

               Items.SequenceEqual(WaitForPairingResponse.Items);

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
            => $"Wait-for-pairing response with {Items.Count} action(s): {String.Join(", ", Items)}";

        #endregion

    }

}

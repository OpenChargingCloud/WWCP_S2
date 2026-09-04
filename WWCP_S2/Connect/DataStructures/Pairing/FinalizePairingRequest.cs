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
    /// The request body of POST /finalizePairing (s2-connect-pairing.yml): the pairing client
    /// confirms that it has received all the information it needs (success), or lets the
    /// server know that it cannot pair with the provided information. Authenticated by the
    /// pairing attempt identification as bearer token.
    /// </summary>
    public sealed class FinalizePairingRequest : IEquatable<FinalizePairingRequest>
    {

        #region Properties

        /// <summary>
        /// The optional success flag: true when the client is satisfied and the pairing is
        /// complete, false when it is not. The schema does not require the flag.
        /// </summary>
        [Optional]
        public Boolean?  Success      { get; }

        /// <summary>
        /// Whether the pairing attempt succeeded. Only an explicit "success": true counts as
        /// success; a missing flag counts as failure, because a pairing must never be
        /// completed on the basis of an absent confirmation.
        /// </summary>
        public Boolean   IsSuccess
            => Success == true;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new finalize pairing request.
        /// </summary>
        /// <param name="Success">The optional success flag; a missing flag counts as failure.</param>
        public FinalizePairingRequest(Boolean? Success = null)
        {

            this.Success = Success;

            unchecked
            {
                hashCode = this.Success switch {
                               true   => 3,
                               false  => 2,
                               null   => 1
                           };
            }

        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   /finalizePairing: post:
        //     summary: Confirm that the pairing was successful or has failed.
        //     operationId: confirmPairing
        //     security: [ pairingAttemptId ]
        //     description: Finalize the pairing process by confirming that the client has received all the necessary
        //                  information for pairing, or to let the server know it was not satisfied with provided
        //                  information and cannot pair.
        //     requestBody: content: application/json: schema:
        //       type: object
        //       properties:
        //         success:  { type: boolean }
        //     responses:
        //       '204': The server confirms that the pairing attempt was completed (with or without success).
        //       '400': The request was not understood or the server is not yet satisfied with the pairing process.
        //       '401': Provided pairingAttemptId not accepted.
        //
        // The success flag is not required by the schema; this implementation treats a missing flag as failure.

        #endregion

        #region (static) TryParse(JSON, out FinalizePairingRequest, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a finalize pairing request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FinalizePairingRequest">The parsed finalize pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out FinalizePairingRequest?  FinalizePairingRequest,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)

            => TryParse(JSON,
                        out FinalizePairingRequest,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a finalize pairing request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FinalizePairingRequest">The parsed finalize pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out FinalizePairingRequest?  FinalizePairingRequest,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options)

            => TryParse(JSON,
                        out FinalizePairingRequest,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a finalize pairing request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FinalizePairingRequest">The parsed finalize pairing request.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomFinalizePairingRequestParser">A delegate to parse custom finalize pairing requests.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out FinalizePairingRequest?      FinalizePairingRequest,
                                       [NotNullWhen(false)] out String?                      ErrorResponse,
                                       S2ParserOptions?                                      Options,
                                       CustomJObjectParserDelegate<FinalizePairingRequest>?  CustomFinalizePairingRequestParser)
        {

            try
            {

                FinalizePairingRequest = null;

                #region success    [optional]

                if (!JSON.ParseOptionalS2Boolean("success",
                                                 "success flag",
                                                 out Boolean? success,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "success"))
                {
                    return false;
                }

                #endregion


                FinalizePairingRequest = new FinalizePairingRequest(
                                             success
                                         );

                if (CustomFinalizePairingRequestParser is not null)
                    FinalizePairingRequest = CustomFinalizePairingRequestParser(JSON,
                                                                                FinalizePairingRequest);

                return true;

            }
            catch (Exception e)
            {
                FinalizePairingRequest  = null;
                ErrorResponse           = "The given JSON representation of a finalize pairing request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomFinalizePairingRequestSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomFinalizePairingRequestSerializer">A delegate to serialize custom finalize pairing requests.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FinalizePairingRequest>? CustomFinalizePairingRequestSerializer = null)
        {

            var json = JSONObject.Create(

                           Success.HasValue
                               ? new JProperty("success",  Success.Value)
                               : null

                       );

            return CustomFinalizePairingRequestSerializer is not null
                       ? CustomFinalizePairingRequestSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this finalize pairing request.
        /// </summary>
        public FinalizePairingRequest Clone()

            => new (
                   Success
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two finalize pairing requests for equality.
        /// </summary>
        public static Boolean operator == (FinalizePairingRequest? FinalizePairingRequest1, FinalizePairingRequest? FinalizePairingRequest2)
        {

            if (ReferenceEquals(FinalizePairingRequest1, FinalizePairingRequest2))
                return true;

            if (FinalizePairingRequest1 is null || FinalizePairingRequest2 is null)
                return false;

            return FinalizePairingRequest1.Equals(FinalizePairingRequest2);

        }

        /// <summary>
        /// Compares two finalize pairing requests for inequality.
        /// </summary>
        public static Boolean operator != (FinalizePairingRequest? FinalizePairingRequest1, FinalizePairingRequest? FinalizePairingRequest2)
            => !(FinalizePairingRequest1 == FinalizePairingRequest2);

        #endregion

        #region IEquatable<FinalizePairingRequest> Members

        /// <summary>
        /// Compares two finalize pairing requests for equality.
        /// </summary>
        /// <param name="Object">A finalize pairing request to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FinalizePairingRequest finalizePairingRequest && Equals(finalizePairingRequest);

        /// <summary>
        /// Compares two finalize pairing requests for equality (a missing flag differs from an explicit false).
        /// </summary>
        /// <param name="FinalizePairingRequest">A finalize pairing request to compare with.</param>
        public Boolean Equals(FinalizePairingRequest? FinalizePairingRequest)

            => FinalizePairingRequest is not null &&

               Nullable.Equals(Success, FinalizePairingRequest.Success);

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

            => Success switch {
                   true   => "Pairing succeeded",
                   false  => "Pairing failed",
                   null   => "Pairing failed (no success flag given)"
               };

        #endregion

    }

}

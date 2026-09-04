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
    /// The error a pairing server reports with HTTP status 400 to a requestPairing or
    /// preparePairing request (s2-connect-pairing.yml, PairingResponseErrorMessage): the
    /// reason why pairing is refused and an optional free-text explanation.
    /// </summary>
    public sealed class PairingResponseErrorMessage : IEquatable<PairingResponseErrorMessage>
    {

        #region Properties

        /// <summary>
        /// The reason why the pairing request was refused.
        /// </summary>
        [Mandatory]
        public PairingResponseError  ErrorMessage      { get; }

        /// <summary>
        /// Optional additional information about the error, e.g. the reason behind "Other".
        /// </summary>
        [Optional]
        public String?               AdditionalInfo    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new pairing response error message.
        /// </summary>
        /// <param name="ErrorMessage">The reason why the pairing request was refused.</param>
        /// <param name="AdditionalInfo">Optional additional information about the error.</param>
        public PairingResponseErrorMessage(PairingResponseError  ErrorMessage,
                                           String?               AdditionalInfo   = null)
        {

            if (ErrorMessage.IsNullOrEmpty)
                throw new ArgumentException("The error message must not be empty!", nameof(ErrorMessage));

            this.ErrorMessage    = ErrorMessage;
            this.AdditionalInfo  = AdditionalInfo;

            unchecked
            {
                hashCode = this.ErrorMessage.   GetHashCode() * 3 ^
                          (this.AdditionalInfo?.GetHashCode(StringComparison.Ordinal) ?? 0);
            }

        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   PairingResponseErrorMessage:
        //     type: object
        //     required: ["errorMessage"]
        //     properties:
        //       errorMessage:    { type: string,
        //                          enum: ["InvalidCombinationOfRoles", "IncompatibleS2MessageVersions",
        //                                 "IncompatibleHmacHashingAlgorithms", "IncompatibleCommunicationProtocols",
        //                                 "NodeNotFound", "NoNodeIdProvided", "NoValidPairingTokenOnPairingServer",
        //                                 "ParsingError", "Other"] }
        //       additionalInfo:  { type: string }
        //
        //   Used as the body of the "400 Pairing unsuccessful" response of /requestPairing and of the
        //   "400 ... not willing to pair with the node" response of /preparePairing.

        #endregion

        #region (static) TryParse(JSON, out PairingResponseErrorMessage, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a pairing response error message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PairingResponseErrorMessage">The parsed pairing response error message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out PairingResponseErrorMessage?  PairingResponseErrorMessage,
                                       [NotNullWhen(false)] out String?                       ErrorResponse)

            => TryParse(JSON,
                        out PairingResponseErrorMessage,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a pairing response error message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PairingResponseErrorMessage">The parsed pairing response error message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out PairingResponseErrorMessage?  PairingResponseErrorMessage,
                                       [NotNullWhen(false)] out String?                       ErrorResponse,
                                       S2ParserOptions?                                       Options)

            => TryParse(JSON,
                        out PairingResponseErrorMessage,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a pairing response error message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PairingResponseErrorMessage">The parsed pairing response error message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPairingResponseErrorMessageParser">A delegate to parse custom pairing response error messages.</param>
        public static Boolean TryParse(JObject                                                    JSON,
                                       [NotNullWhen(true)]  out PairingResponseErrorMessage?      PairingResponseErrorMessage,
                                       [NotNullWhen(false)] out String?                           ErrorResponse,
                                       S2ParserOptions?                                           Options,
                                       CustomJObjectParserDelegate<PairingResponseErrorMessage>?  CustomPairingResponseErrorMessageParser)
        {

            try
            {

                PairingResponseErrorMessage = null;

                #region errorMessage      [mandatory]

                if (!JSON.ParseMandatoryS2Enum("errorMessage",
                                               "error message",
                                               PairingResponseError.TryParse,
                                               Options,
                                               out PairingResponseError errorMessage,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additionalInfo    [optional]

                if (!JSON.ParseOptionalS2String("additionalInfo",
                                                "additional information",
                                                out String? additionalInfo,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "errorMessage",
                                                    "additionalInfo"))
                {
                    return false;
                }

                #endregion


                PairingResponseErrorMessage = new PairingResponseErrorMessage(
                                                  errorMessage,
                                                  additionalInfo
                                              );

                if (CustomPairingResponseErrorMessageParser is not null)
                    PairingResponseErrorMessage = CustomPairingResponseErrorMessageParser(JSON,
                                                                                          PairingResponseErrorMessage);

                return true;

            }
            catch (Exception e)
            {
                PairingResponseErrorMessage  = null;
                ErrorResponse                = "The given JSON representation of a pairing response error message is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPairingResponseErrorMessageSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPairingResponseErrorMessageSerializer">A delegate to serialize custom pairing response error messages.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PairingResponseErrorMessage>? CustomPairingResponseErrorMessageSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("errorMessage",     ErrorMessage.ToString()),

                           AdditionalInfo is not null
                               ? new JProperty("additionalInfo",   AdditionalInfo)
                               : null

                       );

            return CustomPairingResponseErrorMessageSerializer is not null
                       ? CustomPairingResponseErrorMessageSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this pairing response error message.
        /// </summary>
        public PairingResponseErrorMessage Clone()

            => new (
                   ErrorMessage.Clone(),
                   AdditionalInfo?.CloneString()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two pairing response error messages for equality.
        /// </summary>
        public static Boolean operator == (PairingResponseErrorMessage? PairingResponseErrorMessage1, PairingResponseErrorMessage? PairingResponseErrorMessage2)
        {

            if (ReferenceEquals(PairingResponseErrorMessage1, PairingResponseErrorMessage2))
                return true;

            if (PairingResponseErrorMessage1 is null || PairingResponseErrorMessage2 is null)
                return false;

            return PairingResponseErrorMessage1.Equals(PairingResponseErrorMessage2);

        }

        /// <summary>
        /// Compares two pairing response error messages for inequality.
        /// </summary>
        public static Boolean operator != (PairingResponseErrorMessage? PairingResponseErrorMessage1, PairingResponseErrorMessage? PairingResponseErrorMessage2)
            => !(PairingResponseErrorMessage1 == PairingResponseErrorMessage2);

        #endregion

        #region IEquatable<PairingResponseErrorMessage> Members

        /// <summary>
        /// Compares two pairing response error messages for equality.
        /// </summary>
        /// <param name="Object">A pairing response error message to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PairingResponseErrorMessage pairingResponseErrorMessage && Equals(pairingResponseErrorMessage);

        /// <summary>
        /// Compares two pairing response error messages for equality.
        /// </summary>
        /// <param name="PairingResponseErrorMessage">A pairing response error message to compare with.</param>
        public Boolean Equals(PairingResponseErrorMessage? PairingResponseErrorMessage)

            => PairingResponseErrorMessage is not null &&

               ErrorMessage.Equals(PairingResponseErrorMessage.ErrorMessage) &&

               String.Equals(AdditionalInfo, PairingResponseErrorMessage.AdditionalInfo, StringComparison.Ordinal);

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

                   ErrorMessage.ToString(),

                   AdditionalInfo is not null
                       ? $": {AdditionalInfo}"
                       : ""

               );

        #endregion

    }

}

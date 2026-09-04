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
    /// The body of the HTTP 400 response of the initiateSession operation: which error
    /// occurred and optional additional information for the end user or the developer
    /// (s2-connect-session-init.yml, CommunicationDetailsErrorMessage).
    /// </summary>
    public sealed class CommunicationDetailsErrorMessage : IEquatable<CommunicationDetailsErrorMessage>
    {

        #region Properties

        /// <summary>
        /// The error that occurred during session initiation.
        /// </summary>
        [Mandatory]
        public CommunicationDetailsError  ErrorMessage      { get; }

        /// <summary>
        /// Optional additional information about the error.
        /// </summary>
        [Optional]
        public String?                    AdditionalInfo    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new communication details error message.
        /// </summary>
        /// <param name="ErrorMessage">The error that occurred during session initiation.</param>
        /// <param name="AdditionalInfo">Optional additional information about the error.</param>
        public CommunicationDetailsErrorMessage(CommunicationDetailsError  ErrorMessage,
                                                String?                    AdditionalInfo   = null)
        {

            if (ErrorMessage.IsNullOrEmpty)
                throw new ArgumentException("The error message must not be empty!",
                                            nameof(ErrorMessage));

            this.ErrorMessage    = ErrorMessage;
            this.AdditionalInfo  = AdditionalInfo;

            unchecked
            {
                hashCode = this.ErrorMessage.   GetHashCode()                          * 3 ^
                          (this.AdditionalInfo?.GetHashCode(StringComparison.Ordinal) ?? 0);
            }

        }

        #endregion


        #region Documentation

        // s2-connect-session-init.yml
        //   CommunicationDetailsErrorMessage:
        //     type: object
        //     required: ["errorMessage"]
        //     properties:
        //       errorMessage:    { type: string, enum: ["IncompatibleS2MessageVersions", "IncompatibleCommunicationProtocols",
        //                                              "NoLongerPaired", "ParsingError", "Other"] }
        //       additionalInfo:  { type: string }
        //
        //   /initiateSession, response '400': Message that gives information what went wrong.

        #endregion

        #region (static) TryParse(JSON, out CommunicationDetailsErrorMessage, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a communication details error message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CommunicationDetailsErrorMessage">The parsed communication details error message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                     JSON,
                                       [NotNullWhen(true)]  out CommunicationDetailsErrorMessage?  CommunicationDetailsErrorMessage,
                                       [NotNullWhen(false)] out String?                            ErrorResponse)

            => TryParse(JSON,
                        out CommunicationDetailsErrorMessage,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a communication details error message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CommunicationDetailsErrorMessage">The parsed communication details error message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                     JSON,
                                       [NotNullWhen(true)]  out CommunicationDetailsErrorMessage?  CommunicationDetailsErrorMessage,
                                       [NotNullWhen(false)] out String?                            ErrorResponse,
                                       S2ParserOptions?                                            Options)

            => TryParse(JSON,
                        out CommunicationDetailsErrorMessage,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a communication details error message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CommunicationDetailsErrorMessage">The parsed communication details error message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomCommunicationDetailsErrorMessageParser">A delegate to parse custom communication details error messages.</param>
        public static Boolean TryParse(JObject                                                         JSON,
                                       [NotNullWhen(true)]  out CommunicationDetailsErrorMessage?      CommunicationDetailsErrorMessage,
                                       [NotNullWhen(false)] out String?                                ErrorResponse,
                                       S2ParserOptions?                                                Options,
                                       CustomJObjectParserDelegate<CommunicationDetailsErrorMessage>?  CustomCommunicationDetailsErrorMessageParser)
        {

            try
            {

                CommunicationDetailsErrorMessage = null;

                #region errorMessage      [mandatory]

                if (!JSON.ParseMandatoryS2Enum("errorMessage",
                                               "error message",
                                               CommunicationDetailsError.TryParse,
                                               Options,
                                               out CommunicationDetailsError errorMessage,
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


                CommunicationDetailsErrorMessage = new CommunicationDetailsErrorMessage(
                                                       errorMessage,
                                                       additionalInfo
                                                   );

                if (CustomCommunicationDetailsErrorMessageParser is not null)
                    CommunicationDetailsErrorMessage = CustomCommunicationDetailsErrorMessageParser(JSON,
                                                                                                    CommunicationDetailsErrorMessage);

                return true;

            }
            catch (Exception e)
            {
                CommunicationDetailsErrorMessage  = null;
                ErrorResponse                     = "The given JSON representation of a communication details error message is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomCommunicationDetailsErrorMessageSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomCommunicationDetailsErrorMessageSerializer">A delegate to serialize custom communication details error messages.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<CommunicationDetailsErrorMessage>? CustomCommunicationDetailsErrorMessageSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("errorMessage",     ErrorMessage.ToString()),

                           AdditionalInfo is not null
                               ? new JProperty("additionalInfo",   AdditionalInfo)
                               : null

                       );

            return CustomCommunicationDetailsErrorMessageSerializer is not null
                       ? CustomCommunicationDetailsErrorMessageSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this communication details error message.
        /// </summary>
        public CommunicationDetailsErrorMessage Clone()

            => new (
                   ErrorMessage.Clone(),
                   AdditionalInfo?.CloneString()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two communication details error messages for equality.
        /// </summary>
        public static Boolean operator == (CommunicationDetailsErrorMessage? CommunicationDetailsErrorMessage1, CommunicationDetailsErrorMessage? CommunicationDetailsErrorMessage2)
        {

            if (ReferenceEquals(CommunicationDetailsErrorMessage1, CommunicationDetailsErrorMessage2))
                return true;

            if (CommunicationDetailsErrorMessage1 is null || CommunicationDetailsErrorMessage2 is null)
                return false;

            return CommunicationDetailsErrorMessage1.Equals(CommunicationDetailsErrorMessage2);

        }

        /// <summary>
        /// Compares two communication details error messages for inequality.
        /// </summary>
        public static Boolean operator != (CommunicationDetailsErrorMessage? CommunicationDetailsErrorMessage1, CommunicationDetailsErrorMessage? CommunicationDetailsErrorMessage2)
            => !(CommunicationDetailsErrorMessage1 == CommunicationDetailsErrorMessage2);

        #endregion

        #region IEquatable<CommunicationDetailsErrorMessage> Members

        /// <summary>
        /// Compares two communication details error messages for equality.
        /// </summary>
        /// <param name="Object">A communication details error message to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is CommunicationDetailsErrorMessage communicationDetailsErrorMessage && Equals(communicationDetailsErrorMessage);

        /// <summary>
        /// Compares two communication details error messages for equality.
        /// </summary>
        /// <param name="CommunicationDetailsErrorMessage">A communication details error message to compare with.</param>
        public Boolean Equals(CommunicationDetailsErrorMessage? CommunicationDetailsErrorMessage)

            => CommunicationDetailsErrorMessage is not null &&

               ErrorMessage.Equals(CommunicationDetailsErrorMessage.ErrorMessage) &&

               String.Equals(AdditionalInfo, CommunicationDetailsErrorMessage.AdditionalInfo, StringComparison.Ordinal);

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

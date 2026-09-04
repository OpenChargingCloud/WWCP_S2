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
    /// The details a communication client needs to set up an S2 communication channel after
    /// pairing (s2-connect-pairing.yml, ConnectionDetails): the base URL of the session
    /// initiation API of the communication server, the one-time access token, and, when the
    /// communication server uses a self-signed CA certificate (LAN deployments), the
    /// fingerprints of that CA certificate keyed by hashing algorithm. Returned by
    /// requestConnectionDetails (pairing server = communication server) or sent with
    /// postConnectionDetails (pairing client = communication server).
    /// </summary>
    public sealed class ConnectionDetails : IEquatable<ConnectionDetails>
    {

        #region Data

        /// <summary>
        /// The key of the mandatory entry of the certificate fingerprint map: "SHA256".
        /// </summary>
        public  const String  SHA256Key      = S2ConnectDefaults.CertificateFingerprintSHA256Key;

        /// <summary>
        /// The misspelled key used by the description of the OpenAPI file ("SHA265"),
        /// accepted as an alias of "SHA256" when parsing, but never written.
        /// </summary>
        private const String  SHA265TypoKey  = "SHA265";

        #endregion

        #region Properties

        /// <summary>
        /// The base URL of the session initiation API of the communication server,
        /// e.g. "https://cem.example.com/connection/".
        /// </summary>
        [Mandatory]
        public S2BaseURL                                              InitiateSessionUrl         { get; }

        /// <summary>
        /// The one-time access token for the session initiation API.
        /// </summary>
        [Mandatory]
        public AccessToken                                            AccessToken                { get; }

        /// <summary>
        /// The optional fingerprints of the CA certificate used by the communication server,
        /// keyed by the name of the hashing algorithm. When given, the map is not empty and
        /// always contains the key "SHA256". Mandatory when the pairing client becomes the
        /// communication server (postConnectionDetails).
        /// </summary>
        [Optional]
        public IReadOnlyDictionary<String, CertificateFingerprint>?  CertificateFingerprints    { get; }

        /// <summary>
        /// The SHA-256 fingerprint of the CA certificate of the communication server, when
        /// certificate fingerprints are given.
        /// </summary>
        public CertificateFingerprint?                                SHA256Fingerprint

            => CertificateFingerprints is not null &&
               CertificateFingerprints.TryGetValue(SHA256Key, out var fingerprint)
                   ? fingerprint
                   : null;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create new connection details.
        /// </summary>
        /// <param name="InitiateSessionUrl">The base URL of the session initiation API of the communication server.</param>
        /// <param name="AccessToken">The one-time access token for the session initiation API.</param>
        /// <param name="CertificateFingerprints">Optional fingerprints of the CA certificate of the communication server, keyed by hashing algorithm; must contain "SHA256" when given.</param>
        public ConnectionDetails(S2BaseURL                                              InitiateSessionUrl,
                                 AccessToken                                            AccessToken,
                                 IReadOnlyDictionary<String, CertificateFingerprint>?  CertificateFingerprints   = null)
        {

            if (String.IsNullOrEmpty(InitiateSessionUrl.Value))
                throw new ArgumentException("The initiate session URL must not be empty!", nameof(InitiateSessionUrl));

            if (AccessToken.Length == 0)
                throw new ArgumentException("The access token must not be empty!", nameof(AccessToken));

            if (CertificateFingerprints is not null)
            {

                var fingerprints = new Dictionary<String, CertificateFingerprint>(CertificateFingerprints, StringComparer.Ordinal);

                if (fingerprints.Count == 0)
                    throw new ArgumentException("The certificate fingerprints must not be empty when given!", nameof(CertificateFingerprints));

                if (fingerprints.Keys.Any(String.IsNullOrWhiteSpace))
                    throw new ArgumentException("The hashing algorithm names of the certificate fingerprints must not be empty!", nameof(CertificateFingerprints));

                if (!fingerprints.ContainsKey(SHA256Key))
                    throw new ArgumentException($"The certificate fingerprints must contain the key '{SHA256Key}'!", nameof(CertificateFingerprints));

                this.CertificateFingerprints = fingerprints;

            }

            this.InitiateSessionUrl  = InitiateSessionUrl;
            this.AccessToken         = AccessToken;

            unchecked
            {
                hashCode = this.InitiateSessionUrl.GetHashCode() * 5 ^
                           this.AccessToken.       GetHashCode() * 3 ^
                          (this.CertificateFingerprints?.
                               Select(kv => kv.Key.GetHashCode(StringComparison.Ordinal) ^ kv.Value.GetHashCode()).
                               CalcHashCode() ?? 0);
            }

        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   ConnectionDetails:
        //     description: Details the Connection client needs to set up an S2 communication channel
        //     type: object
        //     required: ["initiateSessionUrl", "accessToken"]
        //     properties:
        //       initiateSessionUrl:      { type: string, format: uri }
        //       accessToken:             { $ref: s2-connect-common.yml#/components/schemas/AccessToken }   (string, format byte)
        //       certificateFingerprint:  { type: object, additionalProperties: { type: string },
        //                                  description: "A map containing the fingerprints of the CA certificate that is being
        //                                  used by the server for communication. The key of the map is the hashing algorithm
        //                                  used to create the fingerprint, the value is the fingerprint itself. All fingerprints
        //                                  must refer to the same CA certificate. The key "SHA265" must always be provided. The
        //                                  property certificateFingerprint is mandatory when the pairing client will be come the
        //                                  communication server." }
        //
        // The OpenAPI description misspells the mandatory key as "SHA265"; the specification text
        // (S2 Connect 1.0.0, "6B. POST /[version]/postConnectionDetails") says "SHA256". This type always
        // writes "SHA256" and, when parsing, accepts "SHA265" as an alias of "SHA256".
        //
        // The initiateSessionUrl follows the base URL rules of S2 Connect (scheme "https://", no API version,
        // trailing slash, see S2BaseURL); "http://" is accepted only with S2ParserOptions.AllowInsecureURLs.

        #endregion

        #region (static) TryParse(JSON, out ConnectionDetails, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of connection details.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ConnectionDetails">The parsed connection details.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out ConnectionDetails?  ConnectionDetails,
                                       [NotNullWhen(false)] out String?             ErrorResponse)

            => TryParse(JSON,
                        out ConnectionDetails,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of connection details.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ConnectionDetails">The parsed connection details.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out ConnectionDetails?  ConnectionDetails,
                                       [NotNullWhen(false)] out String?             ErrorResponse,
                                       S2ParserOptions?                             Options)

            => TryParse(JSON,
                        out ConnectionDetails,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of connection details.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ConnectionDetails">The parsed connection details.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomConnectionDetailsParser">A delegate to parse custom connection details.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out ConnectionDetails?      ConnectionDetails,
                                       [NotNullWhen(false)] out String?                 ErrorResponse,
                                       S2ParserOptions?                                 Options,
                                       CustomJObjectParserDelegate<ConnectionDetails>?  CustomConnectionDetailsParser)
        {

            try
            {

                ConnectionDetails = null;

                #region initiateSessionUrl        [mandatory]

                if (!JSON.ParseMandatoryS2String("initiateSessionUrl",
                                                 "initiate session URL",
                                                 out String? initiateSessionUrlText,
                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!S2BaseURL.TryParse(initiateSessionUrlText,
                                        out S2BaseURL initiateSessionUrl,
                                        out var urlError,
                                        Options?.AllowInsecureURLs ?? false))
                {
                    ErrorResponse = $"Invalid initiate session URL 'initiateSessionUrl': {urlError}";
                    return false;
                }

                #endregion

                #region accessToken               [mandatory]

                if (!JSON.ParseMandatoryS2String("accessToken",
                                                 "access token",
                                                 out String? accessTokenText,
                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!AccessToken.TryParse(accessTokenText,
                                          out AccessToken accessToken))
                {
                    ErrorResponse = "Invalid access token 'accessToken': a standard Base64 text is expected!";
                    return false;
                }

                #endregion

                #region certificateFingerprint    [optional]

                if (!TryParseCertificateFingerprints(JSON,
                                                     out var certificateFingerprints,
                                                     out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "initiateSessionUrl",
                                                    "accessToken",
                                                    "certificateFingerprint"))
                {
                    return false;
                }

                #endregion


                ConnectionDetails = new ConnectionDetails(
                                        initiateSessionUrl,
                                        accessToken,
                                        certificateFingerprints
                                    );

                if (CustomConnectionDetailsParser is not null)
                    ConnectionDetails = CustomConnectionDetailsParser(JSON,
                                                                      ConnectionDetails);

                return true;

            }
            catch (Exception e)
            {
                ConnectionDetails  = null;
                ErrorResponse      = "The given JSON representation of connection details is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (private static) TryParseCertificateFingerprints(JSON, out Fingerprints, out ErrorResponse)

        /// <summary>
        /// Parse the optional "certificateFingerprint" map (hashing algorithm name to hexadecimal
        /// fingerprint). The misspelled key "SHA265" of the OpenAPI description is stored under "SHA256".
        /// </summary>
        internal static Boolean TryParseCertificateFingerprints(JObject                                                    JSON,
                                                               out IReadOnlyDictionary<String, CertificateFingerprint>?  Fingerprints,
                                                               [NotNullWhen(false)] out String?                          ErrorResponse)
        {

            Fingerprints = null;

            if (!JSON.TryGetValue("certificateFingerprint", StringComparison.Ordinal, out var token) ||
                token is null ||
                token.Type == JTokenType.Null)
            {
                ErrorResponse = null;
                return true;
            }

            if (token is not JObject json)
            {
                ErrorResponse = "Invalid certificate fingerprints 'certificateFingerprint': a JSON object is expected!";
                return false;
            }

            var fingerprints = new Dictionary<String, CertificateFingerprint>(StringComparer.Ordinal);

            foreach (var property in json.Properties())
            {

                if (property.Value.Type != JTokenType.String)
                {
                    ErrorResponse = $"Invalid certificate fingerprints 'certificateFingerprint': the value of '{property.Name}' is not a string!";
                    return false;
                }

                if (!CertificateFingerprint.TryParse(property.Value.Value<String>() ?? "",
                                                     out var fingerprint))
                {
                    ErrorResponse = $"Invalid certificate fingerprints 'certificateFingerprint': the value of '{property.Name}' is not a hexadecimal fingerprint!";
                    return false;
                }

                var key = String.Equals(property.Name, SHA265TypoKey, StringComparison.Ordinal)
                              ? SHA256Key
                              : property.Name;

                // Both spellings of the mandatory key are present...
                if (fingerprints.TryGetValue(key, out var existing))
                {

                    if (!existing.Equals(fingerprint))
                    {
                        ErrorResponse = $"Invalid certificate fingerprints 'certificateFingerprint': '{SHA256Key}' and '{SHA265TypoKey}' carry different fingerprints!";
                        return false;
                    }

                    continue;

                }

                fingerprints.Add(key, fingerprint);

            }

            Fingerprints   = fingerprints;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region ToJSON(CustomConnectionDetailsSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomConnectionDetailsSerializer">A delegate to serialize custom connection details.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ConnectionDetails>? CustomConnectionDetailsSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("initiateSessionUrl",       InitiateSessionUrl.ToString()),
                                 new JProperty("accessToken",              AccessToken.Value),

                           CertificateFingerprints is not null
                               ? new JProperty("certificateFingerprint",   FingerprintsToJSON(CertificateFingerprints))
                               : null

                       );

            return CustomConnectionDetailsSerializer is not null
                       ? CustomConnectionDetailsSerializer(this, json)
                       : json;

        }


        /// <summary>
        /// Return the JSON object of the given certificate fingerprints: "SHA256" first,
        /// the other hashing algorithms in ordinal order.
        /// </summary>
        /// <param name="Fingerprints">Certificate fingerprints keyed by hashing algorithm.</param>
        private static JObject FingerprintsToJSON(IReadOnlyDictionary<String, CertificateFingerprint> Fingerprints)
        {

            var json = new JObject();

            if (Fingerprints.TryGetValue(SHA256Key, out var sha256))
                json.Add(SHA256Key, sha256.ToString());

            foreach (var fingerprint in Fingerprints.
                                            Where  (kv => !String.Equals(kv.Key, SHA256Key, StringComparison.Ordinal)).
                                            OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                json.Add(fingerprint.Key, fingerprint.Value.ToString());
            }

            return json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone these connection details.
        /// </summary>
        public ConnectionDetails Clone()

            => new (
                   InitiateSessionUrl,
                   AccessToken,
                   CertificateFingerprints?.ToDictionary(kv => kv.Key.CloneString(),
                                                         kv => kv.Value,
                                                         StringComparer.Ordinal)
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two connection details for equality.
        /// </summary>
        public static Boolean operator == (ConnectionDetails? ConnectionDetails1, ConnectionDetails? ConnectionDetails2)
        {

            if (ReferenceEquals(ConnectionDetails1, ConnectionDetails2))
                return true;

            if (ConnectionDetails1 is null || ConnectionDetails2 is null)
                return false;

            return ConnectionDetails1.Equals(ConnectionDetails2);

        }

        /// <summary>
        /// Compares two connection details for inequality.
        /// </summary>
        public static Boolean operator != (ConnectionDetails? ConnectionDetails1, ConnectionDetails? ConnectionDetails2)
            => !(ConnectionDetails1 == ConnectionDetails2);

        #endregion

        #region IEquatable<ConnectionDetails> Members

        /// <summary>
        /// Compares two connection details for equality.
        /// </summary>
        /// <param name="Object">Connection details to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is ConnectionDetails connectionDetails && Equals(connectionDetails);

        /// <summary>
        /// Compares two connection details for equality: same URL, same token, and the same
        /// fingerprint for every hashing algorithm.
        /// </summary>
        /// <param name="ConnectionDetails">Connection details to compare with.</param>
        public Boolean Equals(ConnectionDetails? ConnectionDetails)

            => ConnectionDetails is not null &&

               InitiateSessionUrl.Equals(ConnectionDetails.InitiateSessionUrl) &&
               AccessToken.       Equals(ConnectionDetails.AccessToken)        &&

               FingerprintsEqual(CertificateFingerprints, ConnectionDetails.CertificateFingerprints);


        /// <summary>
        /// Whether both fingerprint maps are absent, or both contain the same keys with equal fingerprints.
        /// </summary>
        private static Boolean FingerprintsEqual(IReadOnlyDictionary<String, CertificateFingerprint>?  Fingerprints1,
                                                 IReadOnlyDictionary<String, CertificateFingerprint>?  Fingerprints2)
        {

            if (Fingerprints1 is null || Fingerprints2 is null)
                return Fingerprints1 is null && Fingerprints2 is null;

            if (Fingerprints1.Count != Fingerprints2.Count)
                return false;

            foreach (var fingerprint in Fingerprints1)
            {

                if (!Fingerprints2.TryGetValue(fingerprint.Key, out var other) ||
                    !other.Equals(fingerprint.Value))
                {
                    return false;
                }

            }

            return true;

        }

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
        /// Return a text representation of this object (the access token is redacted).
        /// </summary>
        public override String ToString()

            => String.Concat(

                   $"{InitiateSessionUrl} with {AccessToken}",

                   CertificateFingerprints is not null
                       ? $" and {CertificateFingerprints.Count} CA certificate fingerprint(s)"
                       : ""

               );

        #endregion

    }

}

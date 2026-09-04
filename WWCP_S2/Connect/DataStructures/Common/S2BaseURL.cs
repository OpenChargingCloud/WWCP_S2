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

using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The base URL of an S2 Connect REST API (the pairing URL, the initiateSessionUrl or the
    /// registry URL): it must include the scheme "https://", must not include the version of
    /// the API, and must end with a slash, e.g. "https://hostname.local/pairing/"
    /// (S2 Connect 1.0.0, "Pairing URL"; s2-connect-wan-endpoint-registry.yml, pairingUrl).
    /// The versioned API base is derived with <see cref="ForVersion"/>. Plain "http://" is
    /// accepted only when explicitly allowed (tests, development), never by default.
    /// </summary>
    public readonly partial struct S2BaseURL : IEquatable<S2BaseURL>
    {

        #region Data

        [GeneratedRegex(@"/v\d+/$", RegexOptions.CultureInvariant)]
        private static partial Regex VersionSuffixRegExpr();

        #endregion

        #region Properties

        /// <summary>
        /// The base URL as text, ending with a slash.
        /// </summary>
        public String   Value           { get; }

        /// <summary>
        /// The parsed URL.
        /// </summary>
        public URL      URL             { get; }

        /// <summary>
        /// Whether the URL uses HTTPS.
        /// </summary>
        public Boolean  IsHTTPS
            => URL.Scheme == URIScheme.https;

        /// <summary>
        /// The host name of the URL (domain name or IP address) as text, e.g. "pairing.s2.example.com".
        /// </summary>
        public String   Host
            => URL.Host.ToString();

        #endregion

        #region Constructor(s)

        private S2BaseURL(String  Text,
                          URL     URL)
        {
            this.Value  = Text;
            this.URL    = URL;
        }

        #endregion


        #region Documentation

        // S2 Connect 1.0.0, "Pairing URL":
        //   The pairing URL is the base URL of the pairing API of an endpoint. It must include the protocol
        //   ("https://"), it must not include the version of the API, but it must include a trailing slash
        //   (e.g. "https://hostname.local/pairing/").
        //
        // S2 Connect 1.0.0, "Versioning of OpenAPI files":
        //   The major version of the API is embedded in the base URL of the API as /v[major] (e.g. /v1).
        //   The server must always serve an index (e.g. https://hostname.local/pairing/) which returns a
        //   JSON array with all supported versions as they are defined as part of the URL (e.g. ["v1", "v2"]).
        //
        // s2-connect-wan-endpoint-registry.yml, EndpointRecord.pairingUrl:
        //   Must include the protocol ('https://') and end with a slash ('/'), and may not include the API
        //   version (e.g. 'https://hostname.tld/pairing/').

        #endregion

        #region (static) Parse    (Text, AllowHTTP = false)

        /// <summary>
        /// Parse the given text as an S2 Connect base URL.
        /// </summary>
        /// <param name="Text">A base URL, e.g. "https://hostname.local/pairing/".</param>
        /// <param name="AllowHTTP">Whether to accept plain "http://" (tests and development only).</param>
        public static S2BaseURL Parse(String   Text,
                                      Boolean  AllowHTTP   = false)
        {

            if (TryParse(Text, out var url, out var errorResponse, AllowHTTP))
                return url;

            throw new ArgumentException($"Invalid S2 Connect base URL '{Text}': {errorResponse}", nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text, out BaseURL, out ErrorResponse, AllowHTTP = false)

        /// <summary>
        /// Try to parse the given text as an S2 Connect base URL.
        /// </summary>
        /// <param name="Text">A base URL.</param>
        /// <param name="BaseURL">The parsed base URL.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="AllowHTTP">Whether to accept plain "http://" (tests and development only).</param>
        public static Boolean TryParse(String                            Text,
                                       [NotNullWhen(true)] out S2BaseURL  BaseURL,
                                       [NotNullWhen(false)] out String?  ErrorResponse,
                                       Boolean                           AllowHTTP   = false)
        {

            BaseURL = default;

            if (String.IsNullOrWhiteSpace(Text))
            {
                ErrorResponse = "the URL is empty!";
                return false;
            }

            var text = Text.Trim();

            if (!text.EndsWith('/'))
            {
                ErrorResponse = "the URL must end with a slash!";
                return false;
            }

            if (text.Contains('?', StringComparison.Ordinal) || text.Contains('#', StringComparison.Ordinal))
            {
                ErrorResponse = "the URL must not contain a query or a fragment!";
                return false;
            }

            if (VersionSuffixRegExpr().IsMatch(text))
            {
                ErrorResponse = "the URL must not include the API version!";
                return false;
            }

            if (!URL.TryParse(text, out var url))
            {
                ErrorResponse = "the URL could not be parsed!";
                return false;
            }

            if (url.Scheme != URIScheme.https && !(AllowHTTP && url.Scheme == URIScheme.http))
            {
                ErrorResponse = "the URL must use the scheme 'https://'!";
                return false;
            }

            if (url.Host.IsNullOrEmpty)
            {
                ErrorResponse = "the URL has no host!";
                return false;
            }

            BaseURL        = new S2BaseURL(text, url);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (static) TryParse (Text, out BaseURL)

        /// <summary>
        /// Try to parse the given text as an S2 Connect base URL (HTTPS only).
        /// </summary>
        /// <param name="Text">A base URL.</param>
        /// <param name="BaseURL">The parsed base URL.</param>
        public static Boolean TryParse(String                             Text,
                                       [NotNullWhen(true)] out S2BaseURL  BaseURL)

            => TryParse(Text, out BaseURL, out _);

        #endregion


        #region ForVersion(Version)

        /// <summary>
        /// The base URL of the given API major version, e.g. "https://hostname.local/pairing/v1/".
        /// </summary>
        /// <param name="Version">An API major version as used in the URL, e.g. "v1".</param>
        public URL ForVersion(String Version)
            => URL.Parse(Value + Version.Trim('/') + "/");

        #endregion

        #region Operation(Version, OperationName)

        /// <summary>
        /// The URL of an operation of the given API major version, e.g. "https://hostname.local/pairing/v1/requestPairing".
        /// </summary>
        /// <param name="Version">An API major version as used in the URL, e.g. "v1".</param>
        /// <param name="OperationName">An operation name, e.g. "requestPairing".</param>
        public URL Operation(String  Version,
                             String  OperationName)
            => URL.Parse(Value + Version.Trim('/') + "/" + OperationName.TrimStart('/'));

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two base URLs for equality.
        /// </summary>
        public static Boolean operator == (S2BaseURL BaseURL1, S2BaseURL BaseURL2)
            => BaseURL1.Equals(BaseURL2);

        /// <summary>
        /// Compares two base URLs for inequality.
        /// </summary>
        public static Boolean operator != (S2BaseURL BaseURL1, S2BaseURL BaseURL2)
            => !BaseURL1.Equals(BaseURL2);

        #endregion

        #region IEquatable<S2BaseURL> Members

        /// <summary>
        /// Compares two base URLs for equality.
        /// </summary>
        /// <param name="Object">A base URL to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is S2BaseURL url && Equals(url);

        /// <summary>
        /// Compares two base URLs for equality (ordinal, case-sensitive text comparison).
        /// </summary>
        /// <param name="BaseURL">A base URL to compare with.</param>
        public Boolean Equals(S2BaseURL BaseURL)
            => String.Equals(Value, BaseURL.Value, StringComparison.Ordinal);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => Value?.GetHashCode(StringComparison.Ordinal) ?? 0;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return the base URL as text.
        /// </summary>
        public override String ToString()
            => Value ?? "";

        #endregion

    }

}

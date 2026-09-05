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

using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The redaction layer of S2 Connect (PLAN.md §3.6): the secrets of the pairing and the
    /// session initiation flow must never reach a log file, a trace or a crash report.
    ///
    /// <para>
    /// Three shapes are masked, because a secret reaches a log in all three: the value of a
    /// secret JSON property (a logged request or response body), an HTTP Authorization header
    /// (a logged HTTP PDU) and a "name=value" or "name: value" pair (a log message or a query
    /// string). Everything else is left untouched, so that a redacted log stays readable.
    /// </para>
    ///
    /// <para>
    /// Redaction is the second line of defence, not the first: every type that holds a secret
    /// masks it in its <c>ToString()</c> and the library never logs a request body.
    /// </para>
    /// </summary>
    public static partial class S2LogRedaction
    {

        #region Data

        /// <summary>
        /// What a redacted value is replaced with.
        /// </summary>
        public const String  Mask                      = "[REDACTED]";

        /// <summary>
        /// The prefix of a secret fingerprint.
        /// </summary>
        public const String  FingerprintPrefix         = "sha256:";

        /// <summary>
        /// The default number of hexadecimal characters of a secret fingerprint. 12 characters
        /// are 48 bits: enough to correlate two log lines, far too few to recover the secret.
        /// </summary>
        public const Int32   DefaultFingerprintLength  = 12;

        #endregion

        #region Properties

        /// <summary>
        /// The names whose values are secrets: the tokens and challenge responses of S2 Connect
        /// (S2 Connect 1.0.0 and PLAN.md §3.6) and the usual generic names, compared ignoring
        /// case. Redacting a name that is no secret costs readability; missing one leaks a
        /// credential, so this list is deliberately generous.
        /// </summary>
        public static IReadOnlySet<String>  SecretNames    { get; }

            = new HashSet<String>(StringComparer.OrdinalIgnoreCase) {

                  // S2 Connect pairing
                  "pairingAttemptId",
                  "pairingToken",
                  "pairingCode",
                  "clientHmacChallenge",
                  "clientHmacChallengeResponse",
                  "serverHmacChallenge",
                  "serverHmacChallengeResponse",

                  // S2 Connect session initiation and communication
                  "accessToken",
                  "websocketToken",
                  "communicationToken",

                  // Generic
                  "token",
                  "secret",
                  "password",
                  "passphrase",
                  "apiKey",
                  "refreshToken",
                  "privateKey",
                  "authorization"

              };

        #endregion

        #region (private) Regular expressions

        // "accessToken": "abc"  ->  "accessToken": "[REDACTED]"
        // A string, a number or a boolean is masked; null is left alone, because "the property
        // is absent" is no secret and a redacted null would be misleading.
        [GeneratedRegex(
            "\"(?<name>pairingAttemptId|pairingToken|pairingCode|clientHmacChallenge|clientHmacChallengeResponse|serverHmacChallenge|serverHmacChallengeResponse|accessToken|websocketToken|communicationToken|token|secret|password|passphrase|apiKey|refreshToken|privateKey|authorization)\"\\s*:\\s*(?<value>\"(?:[^\"\\\\]|\\\\.)*\"|-?\\d+(?:\\.\\d+)?(?:[eE][-+]?\\d+)?|true|false)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            matchTimeoutMilliseconds: 1000
        )]
        private static partial Regex JSONPropertyRegex();

        // Authorization: Bearer abc  ->  Authorization: Bearer [REDACTED]
        // The scheme is kept: which scheme was used is diagnostic information, not a secret.
        // A PDU is often logged as an escaped string, so a literal "\r"/"\n" starts a header
        // line just as a real one does.
        [GeneratedRegex(
            "(?<start>^|[\\r\\n]|\\\\[rn])(?<name>[ \\t]*(?:Proxy-)?Authorization[ \\t]*:[ \\t]*)(?<scheme>[A-Za-z][A-Za-z0-9\\-._~+/]*[ \\t]+)?(?<value>[^\\r\\n\\\\]*)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            matchTimeoutMilliseconds: 1000
        )]
        private static partial Regex AuthorizationHeaderRegex();

        // accessToken=abc, token: abc  ->  accessToken=[REDACTED], token: [REDACTED]
        // Only for unquoted values; the quoted JSON shape is handled above. The value must not
        // be the mask itself: the closing bracket of "[REDACTED]" is outside the value character
        // class, so without that guard a second pass would append another one.
        // "authorization" is deliberately absent: the header rule above keeps its scheme.
        [GeneratedRegex(
            "\\b(?<name>pairingAttemptId|pairingToken|pairingCode|clientHmacChallenge|clientHmacChallengeResponse|serverHmacChallenge|serverHmacChallengeResponse|accessToken|websocketToken|communicationToken|apiKey|refreshToken|privateKey|password|passphrase|token|secret)(?<separator>\\s*[:=]\\s*)(?<value>(?!\\[REDACTED\\])[^\\s,;&)\\]}\"']+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            matchTimeoutMilliseconds: 1000
        )]
        private static partial Regex NameValueRegex();

        #endregion


        #region Redact  (Text)

        /// <summary>
        /// Redact every secret of the given text: secret JSON properties, Authorization headers
        /// and "name=value" pairs. Returns the text unchanged when it holds no secret and never
        /// throws: a failing redaction must not take down the logging of an application.
        /// </summary>
        /// <param name="Text">The text to redact.</param>
        public static String? Redact(String? Text)
        {

            if (String.IsNullOrEmpty(Text))
                return Text;

            try
            {

                var text = AuthorizationHeaderRegex().Replace(
                               Text,
                               match => String.Concat(
                                            match.Groups["start"]. Value,
                                            match.Groups["name"].  Value,
                                            match.Groups["scheme"].Value,
                                            match.Groups["value"].Value.Length > 0
                                                ? Mask
                                                : ""
                                        )
                           );

                text = JSONPropertyRegex().Replace(
                           text,
                           match => String.Concat("\"", match.Groups["name"].Value, "\": \"", Mask, "\"")
                       );

                text = NameValueRegex().Replace(
                           text,
                           match => String.Concat(
                                        match.Groups["name"].     Value,
                                        match.Groups["separator"].Value,
                                        Mask
                                    )
                       );

                return text;

            }
            catch (RegexMatchTimeoutException)
            {
                // A pathological input must never leak: when the redaction cannot finish,
                // the text is dropped instead of being logged unredacted.
                return Mask;
            }
            catch (Exception)
            {
                return Mask;
            }

        }

        #endregion

        #region RedactJSON(JSON)

        /// <summary>
        /// Return a deep copy of the given JSON in which the value of every secret property is
        /// masked, at any depth and within arrays. The given JSON is not modified.
        /// </summary>
        /// <param name="JSON">The JSON to redact.</param>
        public static JToken? RedactJSON(JToken? JSON)
        {

            if (JSON is null)
                return null;

            var clone = JSON.DeepClone();

            RedactInPlace(clone);

            return clone;

        }

        private static void RedactInPlace(JToken Token)
        {

            switch (Token)
            {

                case JObject jsonObject:
                    foreach (var property in jsonObject.Properties().ToArray())
                    {

                        if (IsSecretName(property.Name) &&
                            property.Value.Type != JTokenType.Null)
                        {
                            property.Value = Mask;
                        }

                        else
                            RedactInPlace(property.Value);

                    }
                    break;

                case JArray jsonArray:
                    foreach (var element in jsonArray)
                        RedactInPlace(element);
                    break;

            }

        }

        #endregion

        #region IsSecretName(Name)

        /// <summary>
        /// Whether the value of a property or parameter of the given name is a secret.
        /// </summary>
        /// <param name="Name">The name of a property or parameter.</param>
        public static Boolean IsSecretName(String? Name)

            => Name is not null &&
               SecretNames.Contains(Name);

        #endregion

        #region Fingerprint(Secret, Length = 12)

        /// <summary>
        /// The fingerprint of a secret, for audit records and for correlating log lines: the
        /// first characters of its SHA-256, prefixed with the hash algorithm (PLAN.md §3.6:
        /// "audit records reference tokens by a truncated SHA-256").
        /// </summary>
        /// <param name="Secret">The secret to fingerprint.</param>
        /// <param name="Length">The number of hexadecimal characters (2..64, default 12).</param>
        public static String Fingerprint(String?  Secret,
                                         Int32    Length   = DefaultFingerprintLength)
        {

            if (Secret is null)
                return String.Concat(FingerprintPrefix, "-");

            if (Length < 2)
                Length = 2;

            if (Length > 64)
                Length = 64;

            var hash = Convert.ToHexStringLower(
                           SHA256.HashData(
                               Encoding.UTF8.GetBytes(Secret)
                           )
                       );

            return String.Concat(FingerprintPrefix, hash.AsSpan(0, Length));

        }

        #endregion

    }

}

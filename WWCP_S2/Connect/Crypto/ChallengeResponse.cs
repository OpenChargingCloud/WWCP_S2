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
using System.Globalization;
using System.Security.Cryptography;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The two-way HMAC challenge-response function of the pairing process
    /// (S2 Connect 1.0.0, "Challenge response process"):
    /// <code>
    ///   When the pairing server is deployed in the LAN:  R = HMAC(C, T || F)
    ///   When the pairing server is deployed in the WAN:  R = HMAC(C, T || D)
    /// </code>
    /// where C is the challenge (the HMAC key), T the pairing token as ASCII bytes,
    /// F the raw bytes of the SHA-256 fingerprint of the server's TLS leaf certificate,
    /// D the domain name of the HTTPS server as ASCII bytes, and || the concatenation.
    /// The same function is used by the node that issued the challenge (to compute the
    /// expected response) and by the node that received it (to answer).
    /// </summary>
    public static class ChallengeResponse
    {

        #region ComputeForLAN(Algorithm, Challenge, PairingToken, ServerLeafCertificateFingerprint)

        /// <summary>
        /// Compute the response to a challenge of a pairing server deployed in the LAN.
        /// </summary>
        /// <param name="Algorithm">The selected HMAC hashing algorithm.</param>
        /// <param name="Challenge">The challenge (the HMAC key).</param>
        /// <param name="PairingToken">The pairing token.</param>
        /// <param name="ServerLeafCertificateFingerprint">The SHA-256 fingerprint of the TLS leaf certificate of the pairing server.</param>
        public static HmacChallengeResponse ComputeForLAN(HmacHashingAlgorithm    Algorithm,
                                                          HmacChallenge           Challenge,
                                                          PairingToken            PairingToken,
                                                          CertificateFingerprint  ServerLeafCertificateFingerprint)

            => Compute(Algorithm,
                       Challenge,
                       PairingToken.ToBytes(),
                       ServerLeafCertificateFingerprint.Bytes.Span);

        #endregion

        #region ComputeForWAN(Algorithm, Challenge, PairingToken, ServerDomainName)

        /// <summary>
        /// Compute the response to a challenge of a pairing server deployed in the WAN.
        /// </summary>
        /// <param name="Algorithm">The selected HMAC hashing algorithm.</param>
        /// <param name="Challenge">The challenge (the HMAC key).</param>
        /// <param name="PairingToken">The pairing token.</param>
        /// <param name="ServerDomainName">The domain name of the HTTPS server, including subdomains, without protocol, port or trailing slashes (e.g. "pairing.s2.example.com"); normalised with <see cref="NormaliseDomainName"/>.</param>
        public static HmacChallengeResponse ComputeForWAN(HmacHashingAlgorithm  Algorithm,
                                                          HmacChallenge         Challenge,
                                                          PairingToken          PairingToken,
                                                          String                ServerDomainName)

            => Compute(Algorithm,
                       Challenge,
                       PairingToken.ToBytes(),
                       Encoding.ASCII.GetBytes(NormaliseDomainName(ServerDomainName)));

        #endregion

        #region VerifyForLAN / VerifyForWAN(..., Response)

        /// <summary>
        /// Verify (in constant time) the response of the peer to a challenge of a pairing server deployed in the LAN.
        /// </summary>
        /// <param name="Algorithm">The selected HMAC hashing algorithm.</param>
        /// <param name="Challenge">The challenge that was sent.</param>
        /// <param name="PairingToken">The pairing token.</param>
        /// <param name="ServerLeafCertificateFingerprint">The SHA-256 fingerprint of the TLS leaf certificate of the pairing server.</param>
        /// <param name="Response">The received response.</param>
        public static Boolean VerifyForLAN(HmacHashingAlgorithm    Algorithm,
                                           HmacChallenge           Challenge,
                                           PairingToken            PairingToken,
                                           CertificateFingerprint  ServerLeafCertificateFingerprint,
                                           HmacChallengeResponse   Response)

            => ComputeForLAN(Algorithm, Challenge, PairingToken, ServerLeafCertificateFingerprint).ConstantTimeEquals(Response);


        /// <summary>
        /// Verify (in constant time) the response of the peer to a challenge of a pairing server deployed in the WAN.
        /// </summary>
        /// <param name="Algorithm">The selected HMAC hashing algorithm.</param>
        /// <param name="Challenge">The challenge that was sent.</param>
        /// <param name="PairingToken">The pairing token.</param>
        /// <param name="ServerDomainName">The domain name of the HTTPS server.</param>
        /// <param name="Response">The received response.</param>
        public static Boolean VerifyForWAN(HmacHashingAlgorithm   Algorithm,
                                           HmacChallenge          Challenge,
                                           PairingToken           PairingToken,
                                           String                 ServerDomainName,
                                           HmacChallengeResponse  Response)

            => ComputeForWAN(Algorithm, Challenge, PairingToken, ServerDomainName).ConstantTimeEquals(Response);

        #endregion

        #region NormaliseDomainName(DomainName)

        /// <summary>
        /// Normalise a domain name for the WAN challenge-response function: trimmed, without a
        /// trailing dot, converted to its ASCII (IDNA/Punycode) form and lower case. A value with
        /// a scheme, port, path or user information is rejected.
        /// </summary>
        /// <param name="DomainName">A domain name, e.g. "Pairing.S2.Example.com".</param>
        public static String NormaliseDomainName(String DomainName)
        {

            var name = DomainName.Trim().TrimEnd('.');

            if (name.Length == 0)
                throw new ArgumentException("The domain name must not be empty!", nameof(DomainName));

            if (name.Contains('/', StringComparison.Ordinal) ||
                name.Contains(':', StringComparison.Ordinal) ||
                name.Contains('@', StringComparison.Ordinal) ||
                name.Any(Char.IsWhiteSpace))
            {
                throw new ArgumentException($"'{DomainName}' is not a plain domain name (no scheme, port, path or user information allowed)!", nameof(DomainName));
            }

            try
            {
                return new IdnMapping().GetAscii(name).ToLowerInvariant();
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"'{DomainName}' is not a valid domain name: {e.Message}", nameof(DomainName), e);
            }

        }

        #endregion


        #region (private) Compute(Algorithm, Challenge, TokenBytes, SuffixBytes)

        private static HmacChallengeResponse Compute(HmacHashingAlgorithm  Algorithm,
                                                     HmacChallenge         Challenge,
                                                     ReadOnlySpan<Byte>    TokenBytes,
                                                     ReadOnlySpan<Byte>    SuffixBytes)
        {

            var message = new Byte[TokenBytes.Length + SuffixBytes.Length];
            TokenBytes. CopyTo(message);
            SuffixBytes.CopyTo(message.AsSpan(TokenBytes.Length));

            if (Algorithm == HmacHashingAlgorithm.SHA256)
                return HmacChallengeResponse.FromBytes(HMACSHA256.HashData(Challenge.Bytes.Span, message));

            throw new NotSupportedException($"The HMAC hashing algorithm '{Algorithm}' is not supported!");

        }

        #endregion

    }

}

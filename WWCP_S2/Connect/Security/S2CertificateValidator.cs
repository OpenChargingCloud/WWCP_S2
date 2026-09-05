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

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// Why a TLS server certificate was rejected (or accepted) by <see cref="S2CertificateValidator"/>.
    /// </summary>
    public enum S2CertificateVerdict
    {

        /// <summary>
        /// The certificate matches a pinned fingerprint of this host and is otherwise valid.
        /// </summary>
        Pinned,

        /// <summary>
        /// Nothing is pinned for this host yet and the certificate was accepted for a pairing
        /// (trust on first use); its fingerprint is what the pairing then pins.
        /// </summary>
        AcceptedForPairing,

        /// <summary>
        /// The certificate chains to a trusted root of the operating system.
        /// </summary>
        SystemTrusted,

        /// <summary>
        /// No certificate was presented.
        /// </summary>
        NoCertificate,

        /// <summary>
        /// The host has pinned fingerprints, but the presented certificate matches none of them.
        /// </summary>
        PinMismatch,

        /// <summary>
        /// The certificate is not valid at this time (not yet valid, or expired).
        /// </summary>
        Expired,

        /// <summary>
        /// The host name does not appear in the certificate's subject alternative names.
        /// </summary>
        HostNameMismatch,

        /// <summary>
        /// The certificate would be pinned but is not self-signed, so pinning it would break as
        /// soon as its issuer rotates the leaf (PLAN.md D13: never pin a leaf that is not its own CA).
        /// </summary>
        NotSelfSigned,

        /// <summary>
        /// The certificate is neither pinned nor system-trusted.
        /// </summary>
        Untrusted

    }


    /// <summary>
    /// The result of validating a TLS server certificate.
    /// </summary>
    /// <param name="Verdict">Why the certificate was accepted or rejected.</param>
    /// <param name="IsValid">Whether the connection may proceed.</param>
    /// <param name="Fingerprint">The SHA-256 fingerprint of the presented certificate, when one was presented.</param>
    /// <param name="Description">A human readable description.</param>
    public sealed record S2CertificateValidationResult(S2CertificateVerdict     Verdict,
                                                       Boolean                  IsValid,
                                                       CertificateFingerprint?  Fingerprint,
                                                       String                   Description);


    /// <summary>
    /// The TLS server certificate validation of S2 Connect (PLAN.md D13, Phase 11a). It replaces the
    /// operating system's judgement for the private, self-signed certificates S2 Connect LAN
    /// endpoints use, and defers to the system for publicly signed WAN endpoints.
    ///
    /// <para>The rules, in order:</para>
    /// <list type="number">
    ///   <item>No certificate at all is always a failure.</item>
    ///   <item>The validity period is always checked, whatever else holds.</item>
    ///   <item>If the host has pinned fingerprints, the presented certificate must match one of them
    ///         (constant time). A mismatch fails, even if the operating system trusts the chain —
    ///         that is the point of pinning.</item>
    ///   <item>Otherwise, if the operating system trusts the chain and the host name matches, the
    ///         connection proceeds (a WAN endpoint with a public certificate).</item>
    ///   <item>Otherwise, during a pairing (<see cref="AcceptUnpinnedForPairing"/>), a self-signed
    ///         certificate is accepted so that its fingerprint can be pinned; it must be its own CA,
    ///         because D13 established that a private CA root never reaches the client.</item>
    ///   <item>Anything else fails.</item>
    /// </list>
    /// </summary>
    public sealed class S2CertificateValidator
    {

        #region Properties

        /// <summary>
        /// The pinned certificates.
        /// </summary>
        public CertificatePinStore  PinStore                    { get; }

        /// <summary>
        /// Whether an unpinned, self-signed certificate is accepted (trust on first use during a
        /// pairing). Off for every connection after the pairing.
        /// </summary>
        public Boolean              AcceptUnpinnedForPairing    { get; }

        /// <summary>
        /// Whether the host name must appear in the certificate's subject alternative names
        /// (default: true; a pairing over an IP literal may switch it off).
        /// </summary>
        public Boolean              CheckHostName               { get; }

        /// <summary>
        /// The time provider used for the validity check.
        /// </summary>
        public TimeProvider         TimeProvider                { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new S2 Connect TLS server certificate validator.
        /// </summary>
        /// <param name="PinStore">The pinned certificates.</param>
        /// <param name="AcceptUnpinnedForPairing">Whether an unpinned self-signed certificate is accepted (pairing only).</param>
        /// <param name="CheckHostName">Whether the host name must match a subject alternative name.</param>
        /// <param name="TimeProvider">An optional time provider.</param>
        public S2CertificateValidator(CertificatePinStore  PinStore,
                                      Boolean              AcceptUnpinnedForPairing   = false,
                                      Boolean              CheckHostName              = true,
                                      TimeProvider?        TimeProvider               = null)
        {

            ArgumentNullException.ThrowIfNull(PinStore);

            this.PinStore                  = PinStore;
            this.AcceptUnpinnedForPairing  = AcceptUnpinnedForPairing;
            this.CheckHostName             = CheckHostName;
            this.TimeProvider              = TimeProvider ?? System.TimeProvider.System;

        }

        #endregion


        #region Validate(HostName, Certificate, Chain, PolicyErrors)

        /// <summary>
        /// Validate the TLS server certificate of the given host.
        /// </summary>
        /// <param name="HostName">The host that was connected to.</param>
        /// <param name="Certificate">The presented server certificate.</param>
        /// <param name="Chain">The chain the platform built, when any.</param>
        /// <param name="PolicyErrors">The policy errors the platform reported.</param>
        public S2CertificateValidationResult Validate(String             HostName,
                                                      X509Certificate2?  Certificate,
                                                      X509Chain?         Chain,
                                                      SslPolicyErrors    PolicyErrors)
        {

            #region 1. A certificate is required

            if (Certificate is null)
                return new S2CertificateValidationResult(
                           S2CertificateVerdict.NoCertificate,
                           false,
                           null,
                           "The server did not present a certificate!"
                       );

            #endregion

            var fingerprint  = CertificateFingerprint.FromCertificate(Certificate);
            var domainName   = NormaliseHostName(HostName);

            #region 2. The validity period is always checked

            var now = TimeProvider.GetUtcNow();

            if (now < Certificate.NotBefore.ToUniversalTime() ||
                now > Certificate.NotAfter. ToUniversalTime())
            {
                return new S2CertificateValidationResult(
                           S2CertificateVerdict.Expired,
                           false,
                           fingerprint,
                           $"The server certificate of '{domainName}' is only valid from {Certificate.NotBefore.ToUniversalTime():O} to {Certificate.NotAfter.ToUniversalTime():O}!"
                       );
            }

            #endregion

            #region 3. A pinned host must present a pinned certificate

            if (PinStore.IsPinned(domainName))
            {

                if (!PinStore.Matches(domainName, fingerprint))
                    return new S2CertificateValidationResult(
                               S2CertificateVerdict.PinMismatch,
                               false,
                               fingerprint,
                               $"The server certificate of '{domainName}' ({fingerprint.ToHexString()}) matches none of the pinned certificates!"
                           );

                if (CheckHostName && !MatchesHostName(Certificate, domainName))
                    return new S2CertificateValidationResult(
                               S2CertificateVerdict.HostNameMismatch,
                               false,
                               fingerprint,
                               $"The pinned server certificate does not name the host '{domainName}'!"
                           );

                return new S2CertificateValidationResult(
                           S2CertificateVerdict.Pinned,
                           true,
                           fingerprint,
                           $"The server certificate of '{domainName}' matches a pinned certificate."
                       );

            }

            #endregion

            #region 4. An unpinned host may be trusted by the operating system (WAN endpoints)

            if (PolicyErrors == SslPolicyErrors.None)
                return new S2CertificateValidationResult(
                           S2CertificateVerdict.SystemTrusted,
                           true,
                           fingerprint,
                           $"The server certificate of '{domainName}' is trusted by the operating system."
                       );

            #endregion

            #region 5. During a pairing a self-signed certificate is accepted, so that it can be pinned

            if (AcceptUnpinnedForPairing)
            {

                // Only chain problems may be waived: a wrong host name or a missing certificate are
                // never acceptable, not even for a first pairing.
                if ((PolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None &&
                    !(CheckHostName is false && PolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch)))
                {
                    return new S2CertificateValidationResult(
                               S2CertificateVerdict.Untrusted,
                               false,
                               fingerprint,
                               $"The server certificate of '{domainName}' is not acceptable: {PolicyErrors}."
                           );
                }

                if (CheckHostName && !MatchesHostName(Certificate, domainName))
                    return new S2CertificateValidationResult(
                               S2CertificateVerdict.HostNameMismatch,
                               false,
                               fingerprint,
                               $"The server certificate does not name the host '{domainName}'!"
                           );

                // D13: only a self-signed certificate may be pinned, because a private CA root is
                // never transmitted and a rotated leaf would silently break the pin.
                if (!IsSelfSigned(Certificate))
                    return new S2CertificateValidationResult(
                               S2CertificateVerdict.NotSelfSigned,
                               false,
                               fingerprint,
                               $"The server certificate of '{domainName}' is signed by another authority, which this client never receives; S2 Connect LAN endpoints must present a self-signed certificate that is its own CA."
                           );

                return new S2CertificateValidationResult(
                           S2CertificateVerdict.AcceptedForPairing,
                           true,
                           fingerprint,
                           $"The self-signed server certificate of '{domainName}' was accepted for pairing and can be pinned."
                       );

            }

            #endregion

            return new S2CertificateValidationResult(
                       S2CertificateVerdict.Untrusted,
                       false,
                       fingerprint,
                       $"The server certificate of '{domainName}' is neither pinned nor trusted by the operating system ({PolicyErrors})."
                   );

        }

        #endregion

        #region (private static) NormaliseHostName(HostName)

        /// <summary>
        /// Normalise a host name for the pin lookup. <see cref="ChallengeResponse.NormaliseDomainName"/>
        /// rejects everything that is not a domain name (an IPv6 literal, a host with a port), and a
        /// TLS validation callback must never throw, so such a host falls back to plain lower-casing.
        /// </summary>
        private static String NormaliseHostName(String HostName)
        {

            if (String.IsNullOrWhiteSpace(HostName))
                return "";

            try
            {
                return ChallengeResponse.NormaliseDomainName(HostName);
            }
            catch (ArgumentException)
            {
                return HostName.Trim().TrimEnd('.').ToLowerInvariant();
            }

        }

        #endregion

        #region (static) IsSelfSigned(Certificate)

        /// <summary>
        /// Whether the certificate is its own issuer and its signature verifies with its own public
        /// key (a certificate that merely claims the same subject and issuer is not enough).
        /// </summary>
        /// <param name="Certificate">A certificate.</param>
        public static Boolean IsSelfSigned(X509Certificate2 Certificate)
        {

            ArgumentNullException.ThrowIfNull(Certificate);

            if (!Certificate.SubjectName.RawData.SequenceEqual(Certificate.IssuerName.RawData))
                return false;

            try
            {
                return Certificate.Verify() ||
                       VerifiesWithOwnKey(Certificate);
            }
            catch
            {
                return VerifiesWithOwnKey(Certificate);
            }

        }

        private static Boolean VerifiesWithOwnKey(X509Certificate2 Certificate)
        {

            // Verify() consults the platform trust store, which never contains a private root.
            // Building a one-element chain that trusts exactly this certificate answers the real
            // question: is the signature the certificate's own?
            using var chain = new X509Chain();

            chain.ChainPolicy.TrustMode                 = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.RevocationMode            = X509RevocationMode.NoCheck;
            chain.ChainPolicy.DisableCertificateDownloads = true;
            chain.ChainPolicy.VerificationFlags         = X509VerificationFlags.IgnoreNotTimeValid;
            chain.ChainPolicy.CustomTrustStore.Add(Certificate);

            return chain.Build(Certificate) &&
                   chain.ChainElements.Count == 1;

        }

        #endregion

        #region (static) MatchesHostName(Certificate, HostName)

        /// <summary>
        /// Whether the host name appears among the certificate's subject alternative names
        /// (wildcards of one leading label included), or, when it has none, in its common name.
        /// </summary>
        /// <param name="Certificate">A certificate.</param>
        /// <param name="HostName">A normalised host name.</param>
        public static Boolean MatchesHostName(X509Certificate2  Certificate,
                                              String            HostName)
        {

            ArgumentNullException.ThrowIfNull(Certificate);

            if (String.IsNullOrWhiteSpace(HostName))
                return false;

            var hostName = HostName.Trim().TrimEnd('.').ToLowerInvariant();
            var names    = SubjectAlternativeNames(Certificate);

            if (names.Count == 0)
            {

                var commonName = Certificate.GetNameInfo(X509NameType.SimpleName, false)?.Trim().TrimEnd('.').ToLowerInvariant();

                return commonName is not null &&
                       commonName.Equals(hostName, StringComparison.Ordinal);

            }

            foreach (var name in names)
            {

                if (name.Equals(hostName, StringComparison.Ordinal))
                    return true;

                // A wildcard matches exactly one label: "*.example.org" covers "a.example.org".
                if (name.StartsWith("*.", StringComparison.Ordinal))
                {

                    var suffix = name[1..];

                    if (hostName.EndsWith(suffix, StringComparison.Ordinal) &&
                        hostName.Length > suffix.Length &&
                        !hostName[..(hostName.Length - suffix.Length)].Contains('.', StringComparison.Ordinal))
                    {
                        return true;
                    }

                }

            }

            return false;

        }

        private static IReadOnlyList<String> SubjectAlternativeNames(X509Certificate2 Certificate)
        {

            var names = new List<String>();

            foreach (var extension in Certificate.Extensions)
            {

                if (extension.Oid?.Value != "2.5.29.17")
                    continue;

                // Decode the extension instead of formatting it: AsnEncodedData.Format returns
                // localised text on Windows ("DNS-Name=…") and, on Linux, may not decode this OID
                // at all, which would silently make every host name mismatch.
                var subjectAltName = extension as X509SubjectAlternativeNameExtension
                                         ?? new X509SubjectAlternativeNameExtension(extension.RawData, extension.Critical);

                foreach (var dnsName in subjectAltName.EnumerateDnsNames())
                {
                    if (!String.IsNullOrWhiteSpace(dnsName))
                        names.Add(dnsName.Trim().TrimEnd('.').ToLowerInvariant());
                }

                foreach (var ipAddress in subjectAltName.EnumerateIPAddresses())
                    names.Add(ipAddress.ToString().ToLowerInvariant());

            }

            return names;

        }

        #endregion


        #region CreateHTTPClientValidator(HostName) / CreateWebSocketClientValidator(HostName)

        /// <summary>
        /// A Hermod HTTP client validation handler applying these rules to the given host.
        /// </summary>
        /// <param name="HostName">The host that is connected to.</param>
        public RemoteTLSServerCertificateValidationHandler<IHTTPClient> CreateHTTPClientValidator(String HostName)

            => (sender, certificate, chain, client, policyErrors) => ToHermod(Validate(HostName, certificate, chain, policyErrors));

        /// <summary>
        /// A Hermod WebSocket client validation handler applying these rules to the given host.
        /// </summary>
        /// <param name="HostName">The host that is connected to.</param>
        public RemoteTLSServerCertificateValidationHandler<IWebSocketClient> CreateWebSocketClientValidator(String HostName)

            => (sender, certificate, chain, client, policyErrors) => ToHermod(Validate(HostName, certificate, chain, policyErrors));

        private static TLSValidationResult ToHermod(S2CertificateValidationResult Result)

            => Result.IsValid
                   ? TLSValidationResult.Success()
                   : TLSValidationResult.Failed(Result.Description);

        #endregion

    }

}

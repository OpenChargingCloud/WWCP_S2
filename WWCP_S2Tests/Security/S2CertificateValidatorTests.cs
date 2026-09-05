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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.Time.Testing;

using org.GraphDefined.Vanaheimr.Hermod;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// The TLS server certificate validation of S2 Connect (PLAN.md D13, Phase 11a): the ordered
    /// rules of <see cref="S2CertificateValidator"/> against real certificates - no certificate,
    /// the validity window, pinning (which overrides system trust), system trust for WAN
    /// endpoints, trust on first use during a pairing (self-signed only!) and the host name check.
    /// </summary>
    [TestFixture]
    public sealed class S2CertificateValidatorTests
    {

        #region Helpers

        /// <summary>
        /// A validator for the given pin store.
        /// </summary>
        private static S2CertificateValidator Validator(CertificatePinStore  PinStore,
                                                        Boolean              AcceptUnpinnedForPairing   = false,
                                                        Boolean              CheckHostName              = true,
                                                        TimeProvider?        TimeProvider               = null)

            => new (PinStore,
                    AcceptUnpinnedForPairing,
                    CheckHostName,
                    TimeProvider);

        /// <summary>
        /// A self-signed certificate whose only subject alternative name is a wildcard.
        /// </summary>
        private static X509Certificate2 CreateWildcardCertificate(String WildcardName = "*.example.org")
        {

            using var rsa = RSA.Create(2048);

            var request = new CertificateRequest("CN=S2 Wildcard Test",
                                                 rsa,
                                                 HashAlgorithmName.SHA256,
                                                 RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(WildcardName);
            request.CertificateExtensions.Add(sanBuilder.Build());

            var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),
                                                       DateTimeOffset.UtcNow.AddDays(30));

            return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx),
                                                    null,
                                                    X509KeyStorageFlags.Exportable);

        }

        #endregion


        #region NoCertificate_IsRejected()

        /// <summary>
        /// Rule 1: no certificate at all is always a failure.
        /// </summary>
        [Test]
        public void NoCertificate_IsRejected()
        {

            var validator  = Validator(new CertificatePinStore(),
                                       AcceptUnpinnedForPairing: true);

            var result     = validator.Validate("myhost.local",
                                                null,
                                                null,
                                                SslPolicyErrors.RemoteCertificateNotAvailable);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,      Is.EqualTo(S2CertificateVerdict.NoCertificate));
                Assert.That(result.IsValid,      Is.False);
                Assert.That(result.Fingerprint,  Is.Null);
                Assert.That(result.Description,  Is.Not.Empty);

            });

        }

        #endregion

        #region ExpiredCertificate_IsRejected()

        /// <summary>
        /// Rule 2: the validity period is always checked - even a pinned certificate expires.
        /// </summary>
        [Test]
        public void ExpiredCertificate_IsRejected()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var pinStore  = new CertificatePinStore();
            pinStore.Pin("myhost.local", CertificateFingerprint.FromCertificate(certificate));

            // The helper certificates are valid from yesterday for 30 days.
            var validator = Validator(pinStore,
                                      TimeProvider: new FakeTimeProvider(DateTimeOffset.UtcNow.AddDays(365)));

            var result    = validator.Validate("myhost.local",
                                               certificate,
                                               null,
                                               SslPolicyErrors.None);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,      Is.EqualTo(S2CertificateVerdict.Expired));
                Assert.That(result.IsValid,      Is.False);
                Assert.That(result.Fingerprint,  Is.Not.Null);

            });

        }

        #endregion

        #region NotYetValidCertificate_IsRejected()

        /// <summary>
        /// Rule 2, the other end of the window: a certificate that is not yet valid.
        /// </summary>
        [Test]
        public void NotYetValidCertificate_IsRejected()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var validator = Validator(new CertificatePinStore(),
                                      TimeProvider: new FakeTimeProvider(DateTimeOffset.UtcNow.AddDays(-365)));

            var result    = validator.Validate("myhost.local",
                                               certificate,
                                               null,
                                               SslPolicyErrors.None);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,  Is.EqualTo(S2CertificateVerdict.Expired));
                Assert.That(result.IsValid,  Is.False);

            });

        }

        #endregion


        #region PinnedCertificate_IsAccepted()

        /// <summary>
        /// Rule 3: the pinned certificate of a pinned host, naming that host.
        /// </summary>
        [Test]
        public void PinnedCertificate_IsAccepted()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var fingerprint  = CertificateFingerprint.FromCertificate(certificate);
            var pinStore     = new CertificatePinStore();
            pinStore.Pin("myhost.local", fingerprint);

            var validator    = Validator(pinStore);

            // A self-signed certificate always produces chain errors: the pin is what counts.
            var result       = validator.Validate("myhost.local",
                                                  certificate,
                                                  null,
                                                  SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,                    Is.EqualTo(S2CertificateVerdict.Pinned));
                Assert.That(result.IsValid,                    Is.True);
                Assert.That(result.Fingerprint?.ToHexString(), Is.EqualTo(fingerprint.ToHexString()));

            });

        }

        #endregion

        #region PinnedHost_WithAnotherCertificate_IsAPinMismatch()

        /// <summary>
        /// Rule 3 is the whole point of pinning: a pinned host that presents another certificate
        /// is rejected <em>even when the operating system trusts the chain</em>.
        /// </summary>
        [Test]
        public void PinnedHost_WithAnotherCertificate_IsAPinMismatch()
        {

            using var pinnedCertificate     = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");
            using var presentedCertificate  = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var pinStore = new CertificatePinStore();
            pinStore.Pin("myhost.local", CertificateFingerprint.FromCertificate(pinnedCertificate));

            var validator = Validator(pinStore,
                                      AcceptUnpinnedForPairing: true);

            // SslPolicyErrors.None: the operating system is happy - pinning still says no.
            var result    = validator.Validate("myhost.local",
                                               presentedCertificate,
                                               null,
                                               SslPolicyErrors.None);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,                    Is.EqualTo(S2CertificateVerdict.PinMismatch));
                Assert.That(result.IsValid,                    Is.False,
                            "a pin mismatch must not be rescued by system trust!");
                Assert.That(result.Fingerprint?.ToHexString(), Is.EqualTo(CertificateFingerprint.FromCertificate(presentedCertificate).ToHexString()));

            });

        }

        #endregion

        #region PinnedHost_WithAWrongHostName_IsAHostNameMismatch()

        /// <summary>
        /// Rule 3 with the host name check on: the fingerprint matches, but the certificate does
        /// not name the host that was connected to.
        /// </summary>
        [Test]
        public void PinnedHost_WithAWrongHostName_IsAHostNameMismatch()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var pinStore = new CertificatePinStore();
            pinStore.Pin("other.local", CertificateFingerprint.FromCertificate(certificate));

            var validator = Validator(pinStore,
                                      CheckHostName: true);

            var result    = validator.Validate("other.local",
                                               certificate,
                                               null,
                                               SslPolicyErrors.None);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,  Is.EqualTo(S2CertificateVerdict.HostNameMismatch));
                Assert.That(result.IsValid,  Is.False);

            });

        }

        #endregion

        #region PinnedHost_WithAWrongHostName_WithoutHostNameCheck_IsPinned()

        /// <summary>
        /// The same case with the host name check off (a pairing over an IP literal): the pin
        /// alone decides.
        /// </summary>
        [Test]
        public void PinnedHost_WithAWrongHostName_WithoutHostNameCheck_IsPinned()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var pinStore = new CertificatePinStore();
            pinStore.Pin("other.local", CertificateFingerprint.FromCertificate(certificate));

            var validator = Validator(pinStore,
                                      CheckHostName: false);

            var result    = validator.Validate("other.local",
                                               certificate,
                                               null,
                                               SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,  Is.EqualTo(S2CertificateVerdict.Pinned));
                Assert.That(result.IsValid,  Is.True);

            });

        }

        #endregion


        #region UnpinnedHost_TrustedByTheOperatingSystem_IsAccepted()

        /// <summary>
        /// Rule 4: an unpinned host whose chain the operating system accepts - a WAN endpoint
        /// with a public certificate.
        /// </summary>
        [Test]
        public void UnpinnedHost_TrustedByTheOperatingSystem_IsAccepted()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var validator = Validator(new CertificatePinStore());

            var result    = validator.Validate("myhost.local",
                                               certificate,
                                               null,
                                               SslPolicyErrors.None);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,  Is.EqualTo(S2CertificateVerdict.SystemTrusted));
                Assert.That(result.IsValid,  Is.True);

            });

        }

        #endregion

        #region UnpinnedHost_SelfSignedForPairing_IsAcceptedAndPinnable()

        /// <summary>
        /// Rule 5, trust on first use: during a pairing an unpinned, self-signed certificate is
        /// accepted, and the fingerprint the result carries is exactly what the pairing pins.
        /// </summary>
        [Test]
        public void UnpinnedHost_SelfSignedForPairing_IsAcceptedAndPinnable()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var validator = Validator(new CertificatePinStore(),
                                      AcceptUnpinnedForPairing: true);

            var result    = validator.Validate("myhost.local",
                                               certificate,
                                               null,
                                               SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,                    Is.EqualTo(S2CertificateVerdict.AcceptedForPairing));
                Assert.That(result.IsValid,                    Is.True);
                Assert.That(result.Fingerprint?.ToHexString(), Is.EqualTo(CertificateFingerprint.FromCertificate(certificate).ToHexString()));

            });

        }

        #endregion

        #region UnpinnedHost_CASignedLeafForPairing_IsRejected()

        /// <summary>
        /// D13, the single most important rule of this validator: a leaf signed by a private CA
        /// must never be pinned, because the client never receives that CA and a rotated leaf
        /// would silently break the pin. Only a certificate that is its own CA may be pinned.
        /// </summary>
        [Test]
        public void UnpinnedHost_CASignedLeafForPairing_IsRejected()
        {

            using var ca    = D13ChainSpikeTests.CreateCA();
            using var leaf  = D13ChainSpikeTests.IssueLeaf(ca, "myhost.local");

            var validator = Validator(new CertificatePinStore(),
                                      AcceptUnpinnedForPairing: true);

            var result    = validator.Validate("myhost.local",
                                               leaf,
                                               null,
                                               SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,  Is.EqualTo(S2CertificateVerdict.NotSelfSigned),
                            "a CA-signed leaf must never be accepted for pairing (PLAN.md D13)!");
                Assert.That(result.IsValid,  Is.False);

            });

        }

        #endregion

        #region UnpinnedHost_SelfSignedWithAWrongHostNameForPairing_IsRejected()

        /// <summary>
        /// Rule 5 does not waive the host name: a self-signed certificate naming another host is
        /// rejected even during a pairing.
        /// </summary>
        [Test]
        public void UnpinnedHost_SelfSignedWithAWrongHostNameForPairing_IsRejected()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var validator = Validator(new CertificatePinStore(),
                                      AcceptUnpinnedForPairing: true,
                                      CheckHostName:            true);

            var result    = validator.Validate("different.local",
                                               certificate,
                                               null,
                                               SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,  Is.EqualTo(S2CertificateVerdict.HostNameMismatch));
                Assert.That(result.IsValid,  Is.False);

            });

        }

        #endregion

        #region UnpinnedHost_NameMismatchWithoutHostNameCheckForPairing_IsAccepted()

        /// <summary>
        /// Rule 5 waives chain errors, and - only when the host name check is off - the platform's
        /// name mismatch as well (a pairing over an IP literal).
        /// </summary>
        [Test]
        public void UnpinnedHost_NameMismatchWithoutHostNameCheckForPairing_IsAccepted()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var validator = Validator(new CertificatePinStore(),
                                      AcceptUnpinnedForPairing: true,
                                      CheckHostName:            false);

            var result    = validator.Validate("different.local",
                                               certificate,
                                               null,
                                               SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,  Is.EqualTo(S2CertificateVerdict.AcceptedForPairing));
                Assert.That(result.IsValid,  Is.True);

            });

        }

        #endregion

        #region UnpinnedHost_NameMismatchWithHostNameCheckForPairing_IsRejected()

        /// <summary>
        /// The counterpart: with the host name check on, a platform name mismatch is never
        /// waived - only chain errors are.
        /// </summary>
        [Test]
        public void UnpinnedHost_NameMismatchWithHostNameCheckForPairing_IsRejected()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var validator = Validator(new CertificatePinStore(),
                                      AcceptUnpinnedForPairing: true,
                                      CheckHostName:            true);

            var result    = validator.Validate("myhost.local",
                                               certificate,
                                               null,
                                               SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,  Is.EqualTo(S2CertificateVerdict.Untrusted));
                Assert.That(result.IsValid,  Is.False);

            });

        }

        #endregion

        #region UnpinnedHost_ChainErrorsWithoutPairing_IsUntrusted()

        /// <summary>
        /// Rule 6: after the pairing, an unpinned host with an untrusted chain is simply rejected.
        /// </summary>
        [Test]
        public void UnpinnedHost_ChainErrorsWithoutPairing_IsUntrusted()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var validator = Validator(new CertificatePinStore(),
                                      AcceptUnpinnedForPairing: false);

            var result    = validator.Validate("myhost.local",
                                               certificate,
                                               null,
                                               SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.Multiple(() => {

                Assert.That(result.Verdict,  Is.EqualTo(S2CertificateVerdict.Untrusted));
                Assert.That(result.IsValid,  Is.False);

            });

        }

        #endregion


        #region IsSelfSigned_...()

        /// <summary>
        /// The D13 fallback shape: a self-signed server certificate that is its own CA.
        /// </summary>
        [Test]
        public void IsSelfSigned_ASelfSignedLeaf_IsTrue()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            Assert.That(S2CertificateValidator.IsSelfSigned(certificate),  Is.True);

        }

        /// <summary>
        /// A self-signed root CA is self-signed as well.
        /// </summary>
        [Test]
        public void IsSelfSigned_ARootCA_IsTrue()
        {

            using var ca = D13ChainSpikeTests.CreateCA();

            Assert.That(S2CertificateValidator.IsSelfSigned(ca),  Is.True);

        }

        /// <summary>
        /// A leaf signed by another authority is not self-signed - and must never be pinned.
        /// </summary>
        [Test]
        public void IsSelfSigned_ACASignedLeaf_IsFalse()
        {

            using var ca    = D13ChainSpikeTests.CreateCA();
            using var leaf  = D13ChainSpikeTests.IssueLeaf(ca, "myhost.local");

            Assert.That(S2CertificateValidator.IsSelfSigned(leaf),  Is.False);

        }

        #endregion


        #region MatchesHostName_TheSubjectAlternativeName_Matches()

        /// <summary>
        /// The host name is matched against the subject alternative names, case-insensitively and
        /// with or without a trailing dot.
        /// </summary>
        [Test]
        public void MatchesHostName_TheSubjectAlternativeName_Matches()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf();

            Assert.Multiple(() => {

                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "localhost"),    Is.True);
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "LOCALHOST"),    Is.True,  "case-insensitive");
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "LocalHost."),   Is.True,  "trailing dot");
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, " localhost "),  Is.True,  "trimmed");

            });

        }

        #endregion

        #region MatchesHostName_AnUnrelatedName_DoesNotMatch()

        /// <summary>
        /// An unrelated name - and an empty one - never match.
        /// </summary>
        [Test]
        public void MatchesHostName_AnUnrelatedName_DoesNotMatch()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf();

            Assert.Multiple(() => {

                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "example.org"),        Is.False);
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "notlocalhost"),       Is.False);
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "localhost.evil.org"), Is.False);
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, ""),                   Is.False);

            });

        }

        #endregion

        #region MatchesHostName_AWildcard_MatchesExactlyOneLabel()

        /// <summary>
        /// A wildcard covers exactly one leading label: "*.example.org" matches "a.example.org",
        /// but neither the bare domain nor a deeper subdomain.
        /// </summary>
        [Test]
        public void MatchesHostName_AWildcard_MatchesExactlyOneLabel()
        {

            using var certificate = CreateWildcardCertificate();

            Assert.Multiple(() => {

                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "a.example.org"),    Is.True);
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "A.Example.ORG"),    Is.True);
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "example.org"),      Is.False, "a wildcard does not match the bare domain");
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "a.b.example.org"),  Is.False, "a wildcard covers exactly one label");
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "example.org.evil.com"), Is.False);

            });

        }

        #endregion


        #region CreateHTTPClientValidator_...()

        /// <summary>
        /// The Hermod HTTP client handler applies exactly these rules: a pinned certificate is
        /// valid.
        /// </summary>
        [Test]
        public void CreateHTTPClientValidator_APinnedCertificate_IsValid()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var pinStore = new CertificatePinStore();
            pinStore.Pin("myhost.local", CertificateFingerprint.FromCertificate(certificate));

            var validator = Validator(pinStore);
            var handler   = validator.CreateHTTPClientValidator("myhost.local");

            TLSValidationResult result = handler(this,
                                                 certificate,
                                                 null,
                                                 null!,
                                                 SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.Multiple(() => {

                Assert.That(result.IsValid,  Is.True);
                Assert.That(result.Errors,   Is.Empty);
                Assert.That(validator.Validate("myhost.local", certificate, null, SslPolicyErrors.RemoteCertificateChainErrors).IsValid,
                            Is.EqualTo(result.IsValid));

            });

        }

        /// <summary>
        /// ... and a certificate that matches none of the pins is not.
        /// </summary>
        [Test]
        public void CreateHTTPClientValidator_AMismatchedPin_IsInvalid()
        {

            using var pinnedCertificate     = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");
            using var presentedCertificate  = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var pinStore = new CertificatePinStore();
            pinStore.Pin("myhost.local", CertificateFingerprint.FromCertificate(pinnedCertificate));

            var validator = Validator(pinStore);
            var handler   = validator.CreateHTTPClientValidator("myhost.local");

            TLSValidationResult result = handler(this,
                                                 presentedCertificate,
                                                 null,
                                                 null!,
                                                 SslPolicyErrors.None);

            Assert.Multiple(() => {

                Assert.That(result.IsValid,  Is.False);
                Assert.That(result.Errors,   Is.Not.Empty);
                Assert.That(validator.Validate("myhost.local", presentedCertificate, null, SslPolicyErrors.None).IsValid,
                            Is.EqualTo(result.IsValid));

            });

        }

        /// <summary>
        /// The WebSocket client handler answers the same way.
        /// </summary>
        [Test]
        public void CreateWebSocketClientValidator_APinnedCertificate_IsValid()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("myhost.local");

            var pinStore = new CertificatePinStore();
            pinStore.Pin("myhost.local", CertificateFingerprint.FromCertificate(certificate));

            var handler = Validator(pinStore).CreateWebSocketClientValidator("myhost.local");

            TLSValidationResult result = handler(this,
                                                 certificate,
                                                 null,
                                                 null!,
                                                 SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.That(result.IsValid,  Is.True);

        }

        #endregion

    }

}

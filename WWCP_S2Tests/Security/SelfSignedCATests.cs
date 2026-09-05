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

using System.Security.Cryptography.X509Certificates;

using org.GraphDefined.Vanaheimr.Hermod;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// The self-signed TLS material of an S2 Connect LAN endpoint (PLAN.md D13, Phase 11a): the
    /// self-signed server certificate that is its own CA and whose SHA-256 fingerprint is pinned
    /// during a pairing, and the two-level hierarchy for deployments that distribute a root out
    /// of band.
    /// </summary>
    [TestFixture]
    public sealed class SelfSignedCATests
    {

        #region Helpers

        /// <summary>
        /// The formatted text of the subject alternative name extension (OID 2.5.29.17), or "".
        /// </summary>
        private static String SubjectAlternativeNames(X509Certificate2 Certificate)
        {

            foreach (var extension in Certificate.Extensions)
            {
                if (extension.Oid?.Value == "2.5.29.17")
                    return extension.Format(true);
            }

            return "";

        }

        #endregion


        #region CreateSelfSignedServerCertificate_IsAPinnableServerCertificate()

        /// <summary>
        /// The D13 fallback shape: a certificate with a private key, that is its own issuer, that
        /// names its host and that is valid right now.
        /// </summary>
        [Test]
        public void CreateSelfSignedServerCertificate_IsAPinnableServerCertificate()
        {

            using var certificate = SelfSignedCA.CreateSelfSignedServerCertificate("myhost.local");

            var now = DateTime.UtcNow;

            Assert.Multiple(() => {

                Assert.That(certificate.HasPrivateKey,                                            Is.True,   "a server needs its private key");
                Assert.That(S2CertificateValidator.IsSelfSigned (certificate),                    Is.True,   "a LAN endpoint certificate must be its own CA");
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "myhost.local"),  Is.True);
                Assert.That(certificate.Subject,                                                  Does.Contain("myhost.local"));

                Assert.That(certificate.NotBefore.ToUniversalTime(),                              Is.LessThanOrEqualTo(now),   "valid now");
                Assert.That(certificate.NotAfter. ToUniversalTime(),                              Is.GreaterThan(now),         "valid now");

            });

        }

        #endregion

        #region CreateSelfSignedServerCertificate_HasTheDefaultLifetime()

        /// <summary>
        /// The default lifetime is the 6 month leaf rotation interval of PLAN.md Phase 11a
        /// (plus a few minutes of clock skew tolerance at the front).
        /// </summary>
        [Test]
        public void CreateSelfSignedServerCertificate_HasTheDefaultLifetime()
        {

            using var certificate = SelfSignedCA.CreateSelfSignedServerCertificate("myhost.local");

            var lifetime = certificate.NotAfter - certificate.NotBefore;

            Assert.Multiple(() => {

                Assert.That(SelfSignedCA.DefaultServerCertificateLifetime.TotalDays,  Is.EqualTo(180.0));
                Assert.That(lifetime.TotalDays,                                       Is.EqualTo(180.0).Within(1.0));

            });

        }

        #endregion

        #region CreateSelfSignedServerCertificate_ACustomLifetime_IsHonoured()

        /// <summary>
        /// A shorter lifetime is honoured.
        /// </summary>
        [Test]
        public void CreateSelfSignedServerCertificate_ACustomLifetime_IsHonoured()
        {

            using var certificate = SelfSignedCA.CreateSelfSignedServerCertificate(
                                        "myhost.local",
                                        Lifetime: TimeSpan.FromDays(7)
                                    );

            var lifetime = certificate.NotAfter - certificate.NotBefore;

            Assert.That(lifetime.TotalDays,  Is.EqualTo(7.0).Within(0.5));

        }

        #endregion

        #region CreateSelfSignedServerCertificate_AdditionalHostNames_AreSubjectAlternativeNames()

        /// <summary>
        /// Every additional host name becomes a DNS subject alternative name; the host name itself
        /// is never duplicated.
        /// </summary>
        [Test]
        public void CreateSelfSignedServerCertificate_AdditionalHostNames_AreSubjectAlternativeNames()
        {

            using var certificate = SelfSignedCA.CreateSelfSignedServerCertificate(
                                        "myhost.local",
                                        AdditionalHostNames: [ "alias.local", "s2.example.org", "myhost.local" ]
                                    );

            Assert.Multiple(() => {

                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "myhost.local"),    Is.True);
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "alias.local"),     Is.True);
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "s2.example.org"),  Is.True);
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "other.local"),     Is.False);

            });

        }

        #endregion

        #region CreateSelfSignedServerCertificate_IPAddresses_AreSubjectAlternativeNames()

        /// <summary>
        /// IP addresses become IP subject alternative names, which is what a pairing over an IP
        /// literal needs.
        /// </summary>
        [Test]
        public void CreateSelfSignedServerCertificate_IPAddresses_AreSubjectAlternativeNames()
        {

            using var certificate = SelfSignedCA.CreateSelfSignedServerCertificate(
                                        "myhost.local",
                                        IPAddresses: [ IPv4Address.Parse("192.168.23.42"), IPv4Address.Localhost ]
                                    );

            var subjectAlternativeNames = SubjectAlternativeNames(certificate);

            Assert.Multiple(() => {

                Assert.That(subjectAlternativeNames,  Is.Not.Empty);
                Assert.That(subjectAlternativeNames,  Does.Contain("192.168.23.42"));
                Assert.That(subjectAlternativeNames,  Does.Contain("127.0.0.1"));
                Assert.That(subjectAlternativeNames,  Does.Contain("myhost.local"));

            });

        }

        #endregion

        #region CreateSelfSignedServerCertificate_EveryKeyType_Works(KeyType)

        /// <summary>
        /// Every key type produces a usable certificate with its private key
        /// (RSA 4096 is covered by an explicit test, because it is slow).
        /// </summary>
        [TestCase(S2KeyType.ECC_P256, TestName = "CreateSelfSignedServerCertificate: ECC P-256")]
        [TestCase(S2KeyType.ECC_P384, TestName = "CreateSelfSignedServerCertificate: ECC P-384")]
        [TestCase(S2KeyType.RSA_2048, TestName = "CreateSelfSignedServerCertificate: RSA 2048")]
        public void CreateSelfSignedServerCertificate_EveryKeyType_Works(S2KeyType KeyType)
        {

            using var certificate = SelfSignedCA.CreateSelfSignedServerCertificate(
                                        "myhost.local",
                                        KeyType: KeyType
                                    );

            Assert.Multiple(() => {

                Assert.That(certificate.HasPrivateKey,                                            Is.True);
                Assert.That(S2CertificateValidator.IsSelfSigned (certificate),                    Is.True);
                Assert.That(S2CertificateValidator.MatchesHostName(certificate, "myhost.local"),  Is.True);

            });

        }

        #endregion

        #region CreateSelfSignedServerCertificate_RSA4096_Works()

        /// <summary>
        /// RSA 4096 key generation takes seconds, so this one is not part of every run.
        /// </summary>
        [Test]
        [Explicit("RSA 4096 key generation is slow.")]
        public void CreateSelfSignedServerCertificate_RSA4096_Works()
        {

            using var certificate = SelfSignedCA.CreateSelfSignedServerCertificate(
                                        "myhost.local",
                                        KeyType: S2KeyType.RSA_4096
                                    );

            Assert.Multiple(() => {

                Assert.That(certificate.HasPrivateKey,                          Is.True);
                Assert.That(S2CertificateValidator.IsSelfSigned(certificate),   Is.True);

            });

        }

        #endregion

        #region CreateSelfSignedServerCertificate_TwoCalls_HaveDifferentFingerprints()

        /// <summary>
        /// Every call generates a fresh key pair, so two certificates of the same host are never
        /// the same pin (which is exactly why rotating one requires a re-pairing).
        /// </summary>
        [Test]
        public void CreateSelfSignedServerCertificate_TwoCalls_HaveDifferentFingerprints()
        {

            using var first   = SelfSignedCA.CreateSelfSignedServerCertificate("myhost.local");
            using var second  = SelfSignedCA.CreateSelfSignedServerCertificate("myhost.local");

            var fingerprint1  = CertificateFingerprint.FromCertificate(first);
            var fingerprint2  = CertificateFingerprint.FromCertificate(second);

            Assert.Multiple(() => {

                Assert.That(fingerprint1.Length,                          Is.EqualTo(32), "SHA-256");
                Assert.That(fingerprint1.ConstantTimeEquals(fingerprint2), Is.False);
                Assert.That(fingerprint1.ToHexString(),                   Is.Not.EqualTo(fingerprint2.ToHexString()));

            });

        }

        #endregion

        #region CreateSelfSignedServerCertificate_WithoutAHostName_Throws()

        /// <summary>
        /// A certificate without a host name would be unpinnable and unmatchable.
        /// </summary>
        [Test]
        public void CreateSelfSignedServerCertificate_WithoutAHostName_Throws()
        {

            Assert.Multiple(() => {

                Assert.That(() => { SelfSignedCA.CreateSelfSignedServerCertificate(""); },     Throws.InstanceOf<ArgumentException>());
                Assert.That(() => { SelfSignedCA.CreateSelfSignedServerCertificate("   "); },  Throws.InstanceOf<ArgumentException>());
                Assert.That(() => { SelfSignedCA.CreateSelfSignedServerCertificate(null!); },  Throws.InstanceOf<ArgumentException>());

            });

        }

        #endregion


        #region CreateRootCA_IsASelfSignedAuthority()

        /// <summary>
        /// A root CA is self-signed, carries no private key in its public representation and names
        /// itself.
        /// </summary>
        [Test]
        public void CreateRootCA_IsASelfSignedAuthority()
        {

            var ca = SelfSignedCA.CreateRootCA("My S2 Test CA");

            using var caCertificate = ca.PublicCertificate;

            Assert.Multiple(() => {

                Assert.That(S2CertificateValidator.IsSelfSigned(caCertificate),  Is.True);
                Assert.That(caCertificate.HasPrivateKey,                         Is.False, "the public certificate carries no private key");
                Assert.That(caCertificate.Subject,                               Does.Contain("My S2 Test CA"));
                Assert.That(caCertificate.Issuer,                                Is.EqualTo(caCertificate.Subject));

            });

        }

        #endregion

        #region CreateRootCA_FingerprintMatchesItsCertificate()

        /// <summary>
        /// The fingerprint of the authority is the SHA-256 of its certificate, and its text
        /// representation names it.
        /// </summary>
        [Test]
        public void CreateRootCA_FingerprintMatchesItsCertificate()
        {

            var ca = SelfSignedCA.CreateRootCA("My S2 Test CA");

            using var caCertificate = ca.PublicCertificate;

            var expected = CertificateFingerprint.FromCertificate(caCertificate);

            Assert.Multiple(() => {

                Assert.That(ca.Fingerprint.ToHexString(),  Is.EqualTo(expected.ToHexString()));
                Assert.That(ca.Fingerprint.Length,         Is.EqualTo(32));
                Assert.That(ca.ToString(),                 Does.Contain(expected.ToHexString()));
                Assert.That(ca.ToString(),                 Does.Contain("My S2 Test CA"));

            });

        }

        #endregion

        #region CreateRootCA_WithoutAName_Throws()

        /// <summary>
        /// A certificate authority without a name is a programming error.
        /// </summary>
        [Test]
        public void CreateRootCA_WithoutAName_Throws()
        {

            Assert.Multiple(() => {

                Assert.That(() => { SelfSignedCA.CreateRootCA(""); },     Throws.InstanceOf<ArgumentException>());
                Assert.That(() => { SelfSignedCA.CreateRootCA("   "); },  Throws.InstanceOf<ArgumentException>());

            });

        }

        #endregion

        #region IssueServerCertificate_IsSignedByTheCA()

        /// <summary>
        /// A leaf issued by a root CA is not self-signed - and therefore must never be pinned
        /// (PLAN.md D13) - but it does name its host and is issued by that CA.
        /// </summary>
        [Test]
        public void IssueServerCertificate_IsSignedByTheCA()
        {

            var ca = SelfSignedCA.CreateRootCA("My S2 Test CA");

            using var caCertificate  = ca.PublicCertificate;
            using var leaf           = SelfSignedCA.IssueServerCertificate(ca, "leaf.local");

            Assert.Multiple(() => {

                Assert.That(leaf.HasPrivateKey,                                            Is.True);
                Assert.That(S2CertificateValidator.IsSelfSigned(leaf),                     Is.False, "an issued leaf is never its own CA");
                Assert.That(S2CertificateValidator.MatchesHostName(leaf, "leaf.local"),    Is.True);
                Assert.That(S2CertificateValidator.MatchesHostName(leaf, "myhost.local"),  Is.False);
                Assert.That(leaf.Issuer,                                                   Is.EqualTo(caCertificate.Subject));
                Assert.That(leaf.Subject,                                                  Is.Not.EqualTo(leaf.Issuer));

            });

        }

        #endregion

        #region IssueServerCertificate_WithoutAHostName_Throws()

        /// <summary>
        /// An issued leaf needs a host name as well.
        /// </summary>
        [Test]
        public void IssueServerCertificate_WithoutAHostName_Throws()
        {

            var ca = SelfSignedCA.CreateRootCA("My S2 Test CA");

            using var caCertificate = ca.PublicCertificate;

            Assert.Multiple(() => {

                Assert.That(() => { SelfSignedCA.IssueServerCertificate(ca,    ""); },            Throws.InstanceOf<ArgumentException>());
                Assert.That(() => { SelfSignedCA.IssueServerCertificate(null!, "leaf.local"); },  Throws.InstanceOf<ArgumentException>());

            });

        }

        #endregion

    }

}

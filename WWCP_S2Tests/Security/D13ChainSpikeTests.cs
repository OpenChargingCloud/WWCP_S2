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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Tests.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// The D13 spike (PLAN.md): does a Hermod TLS server transmit a private, self-signed CA root in
    /// the handshake, so that a client can pin the CA by its SHA-256 and accept rotated leaves?
    ///
    /// Hermod builds its server certificate context with
    /// <c>SslStreamCertificateContext.Create(target: leaf, additionalCertificates: null, …)</c>, so
    /// the answer decides the pinning strategy: pin the CA root (leaf may rotate) if the root is
    /// transmitted, otherwise the documented fallback — a single self-signed server certificate
    /// acting as its own CA, pinned as the CA.
    ///
    /// These tests measure what the client's chain actually contains and are the regression guard
    /// for the decision recorded in PLAN.md and CONVENTIONS.md.
    /// </summary>
    [TestFixture]
    public class D13ChainSpikeTests
    {

        #region Certificate helpers

        /// <summary>
        /// Create a self-signed certificate authority.
        /// </summary>
        public static X509Certificate2 CreateCA(String Name = "CN=S2 Test CA")
        {

            using var rsa = RSA.Create(2048);

            var request = new CertificateRequest(Name, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

            var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),
                                                       DateTimeOffset.UtcNow.AddDays(30));

            return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.Exportable);

        }

        /// <summary>
        /// Issue a server certificate signed by the given CA.
        /// </summary>
        public static X509Certificate2 IssueLeaf(X509Certificate2  CA,
                                                 String            HostName   = "localhost")
        {

            using var rsa = RSA.Create(2048);

            var request = new CertificateRequest($"CN={HostName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([ new Oid("1.3.6.1.5.5.7.3.1") ], false));

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(HostName);
            sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
            request.CertificateExtensions.Add(sanBuilder.Build());

            var serial = new Byte[16];
            RandomNumberGenerator.Fill(serial);

            var certificate = request.Create(CA,
                                             DateTimeOffset.UtcNow.AddDays(-1),
                                             DateTimeOffset.UtcNow.AddDays(29),
                                             serial);

            using var withKey = certificate.CopyWithPrivateKey(rsa);

            return X509CertificateLoader.LoadPkcs12(withKey.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.Exportable);

        }

        /// <summary>
        /// Create a self-signed server certificate that is its own CA (the D13 fallback shape).
        /// </summary>
        public static X509Certificate2 CreateSelfSignedLeaf(String HostName = "localhost")
        {

            using var rsa = RSA.Create(2048);

            var request = new CertificateRequest($"CN={HostName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([ new Oid("1.3.6.1.5.5.7.3.1") ], false));

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(HostName);
            sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
            request.CertificateExtensions.Add(sanBuilder.Build());

            var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),
                                                       DateTimeOffset.UtcNow.AddDays(30));

            return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.Exportable);

        }

        #endregion

        #region (private) MeasureChainAsync(ServerCertificate)

        /// <summary>
        /// Start a Hermod HTTPS server with the given certificate, connect once and report what the
        /// client's chain contained.
        /// </summary>
        private static async Task<(Int32 Elements, IReadOnlyList<String> Subjects, SslPolicyErrors Errors, IReadOnlyList<String> Sent)> MeasureChainAsync(X509Certificate2 ServerCertificate)
        {

            var port        = PairingServerFixture.FreePort();

            var httpServer  = new HTTPServer(
                                  IPv4Address.Localhost,
                                  port,
                                  "S2 D13 spike",
                                  ServerCertificateSelector: (tcpServer, tcpClient) => ServerCertificate,
                                  AutoStart:                 false
                              );

            var api         = new HTTPAPI(httpServer, RootPath: HTTPPath.Root, DisableLogging: true);

            api.AddHandler(HTTPMethod.GET,
                           HTTPPath.Root,
                           request => Task.FromResult(
                                          new HTTPResponse.Builder(request) {
                                              HTTPStatusCode  = HTTPStatusCode.OK,
                                              Content         = "ok".ToUTF8Bytes(),
                                              ContentType     = HTTPContentType.Text.PLAIN
                                          }.AsImmutable
                                      ));

            await httpServer.Start();

            var subjects  = new List<String>();
            var sent      = new List<String>();
            var elements  = 0;
            var errors    = SslPolicyErrors.None;

            try
            {

                using var handler = new HttpClientHandler {
                                        ServerCertificateCustomValidationCallback = (message, certificate, chain, policyErrors) => {

                                            errors = policyErrors;

                                            if (chain is not null)
                                            {

                                                elements = chain.ChainElements.Count;

                                                foreach (var element in chain.ChainElements)
                                                    subjects.Add(element.Certificate.Subject);

                                                // ExtraStore holds exactly the certificates the peer sent besides the
                                                // leaf — unlike ChainElements, it cannot be filled from a local cache.
                                                foreach (var extra in chain.ChainPolicy.ExtraStore)
                                                    sent.Add(extra.Subject);

                                            }

                                            return true;

                                        }
                                    };

                using var client = new HttpClient(handler) {
                                       Timeout = TimeSpan.FromSeconds(20)
                                   };

                using var response = await client.GetAsync($"https://localhost:{port}/");

                Assert.That((Int32) response.StatusCode, Is.EqualTo(200));

            }
            finally
            {
                await httpServer.Stop();
            }

            return (elements, subjects, errors, sent);

        }

        #endregion


        #region D13_CASignedLeaf_DoesTheServerTransmitTheRoot()

        /// <summary>
        /// A leaf signed by a private CA: Hermod passes no additional certificates, so the client
        /// receives the leaf alone and cannot see (let alone pin) the CA root.
        /// </summary>
        [Test]
        public async Task D13_CASignedLeaf_DoesTheServerTransmitTheRoot()
        {

            using var ca    = CreateCA();
            using var leaf  = IssueLeaf(ca);

            var (elements, subjects, errors, sent) = await MeasureChainAsync(leaf);

            TestContext.Out.WriteLine($"D13 spike (CA-signed leaf) on {System.Runtime.InteropServices.RuntimeInformation.OSDescription}:");
            TestContext.Out.WriteLine($"  chain elements:   {elements}");
            foreach (var subject in subjects)
                TestContext.Out.WriteLine($"    {subject}");
            TestContext.Out.WriteLine($"  sent by the peer: {sent.Count}");
            foreach (var subject in sent)
                TestContext.Out.WriteLine($"    {subject}");
            TestContext.Out.WriteLine($"  policy errors:    {errors}");

            // Only what the peer actually sent counts: ChainElements may be completed from a local
            // cache, which in this single-process test would flatter the result.
            var rootWasSent = sent.Any(subject => subject.Contains("S2 Test CA", StringComparison.Ordinal));

            Assert.Multiple(() => {

                Assert.That(elements,     Is.GreaterThanOrEqualTo(1));
                Assert.That(rootWasSent,  Is.False,
                            "The private CA root was transmitted: the D13 decision (pin the self-signed leaf) can be revisited.");
                Assert.That(errors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors), Is.True,
                            "A privately signed leaf without a trusted root must not validate against the system trust store.");

            });

        }

        #endregion

        #region D13_SelfSignedLeaf_IsItsOwnPinnableCA()

        /// <summary>
        /// The D13 fallback: a self-signed server certificate is its own CA, so the single chain
        /// element the client receives is the certificate whose SHA-256 was pinned during pairing.
        /// </summary>
        [Test]
        public async Task D13_SelfSignedLeaf_IsItsOwnPinnableCA()
        {

            using var leaf = CreateSelfSignedLeaf();

            var (elements, subjects, errors, sent) = await MeasureChainAsync(leaf);

            TestContext.Out.WriteLine($"D13 spike (self-signed leaf): chain elements {elements}, sent extra {sent.Count}, errors {errors}");
            foreach (var subject in subjects)
                TestContext.Out.WriteLine($"    {subject}");

            Assert.Multiple(() => {
                Assert.That(elements,     Is.EqualTo(1),                            "a self-signed certificate is the whole chain");
                Assert.That(subjects[0],  Does.Contain("CN=localhost"));
                Assert.That(errors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors), Is.True, "untrusted root");
            });

        }

        #endregion

    }

}

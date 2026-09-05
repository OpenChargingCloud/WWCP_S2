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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Tests.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// Certificate pinning over real TLS (PLAN.md Phase 11a, D13): an S2 Connect client talking
    /// HTTPS to a Hermod server accepts a self-signed certificate while pairing, and afterwards
    /// accepts exactly the pinned one and nothing else.
    /// </summary>
    [TestFixture]
    public sealed class CertificatePinningHTTPSTests
    {

        #region (private) StartRegistryAsync(ServerCertificate)

        /// <summary>
        /// Start an HTTPS WAN registry (the smallest S2 Connect API on an
        /// <see cref="AS2ConnectClient"/>) with the given server certificate.
        /// </summary>
        private static async Task<(HTTPServer Server, IPPort Port, S2BaseURL Url)> StartRegistryAsync(X509Certificate2 ServerCertificate)
        {

            var port    = PairingServerFixture.FreePort();

            var server  = new HTTPServer(
                              IPv4Address.Localhost,
                              port,
                              "S2 pinning test registry",
                              ServerCertificateSelector: (tcpServer, tcpClient) => ServerCertificate,
                              AutoStart:                 false
                          );

            _ = new WANRegistryAPI(server, new InMemoryWANRegistry(), HTTPPath.Parse("/registry/"));

            await server.Start();

            return (server, port, S2BaseURL.Parse($"https://localhost:{port}/registry/"));

        }

        #endregion

        #region (private) CreateClient(Url, PinStore, AcceptUnpinnedForPairing)

        private static WANRegistryClient CreateClient(S2BaseURL            Url,
                                                      CertificatePinStore  PinStore,
                                                      Boolean              AcceptUnpinnedForPairing)

            => new (Url,
                    new WANRegistryClientOptions()) {
                       CertificateValidator = new S2CertificateValidator(
                                                  PinStore,
                                                  AcceptUnpinnedForPairing
                                              )
                   };

        #endregion


        #region SelfSignedServer_IsAcceptedWhilePairing_AndPinnedAfterwards()

        [Test]
        public async Task SelfSignedServer_IsAcceptedWhilePairing_AndPinnedAfterwards()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("localhost");

            var (server, _, url) = await StartRegistryAsync(certificate);

            try
            {

                var pinStore = new CertificatePinStore();

                // 1. While pairing an unpinned, self-signed certificate is accepted.
                await using (var pairingPhase = CreateClient(url, pinStore, AcceptUnpinnedForPairing: true))
                {

                    var versions = await pairingPhase.GetVersionsAsync();

                    Assert.That(versions.IsSuccess, Is.True, versions.Description);

                    // The client observed the certificate; that fingerprint is what gets pinned.
                    Assert.That(pairingPhase.ServerCertificateFingerprint, Is.Not.Null);

                    pinStore.Pin("localhost", pairingPhase.ServerCertificateFingerprint!.Value);

                }

                Assert.That(pinStore.IsPinned("localhost"), Is.True);
                Assert.That(pinStore.Matches("localhost", CertificateFingerprint.FromCertificate(certificate)), Is.True);

                // 2. Afterwards the very same certificate is accepted with pinning enforced.
                await using var pinned = CreateClient(url, pinStore, AcceptUnpinnedForPairing: false);

                var afterwards = await pinned.GetVersionsAsync();

                Assert.That(afterwards.IsSuccess, Is.True, afterwards.Description);

            }
            finally
            {
                await server.Stop();
            }

        }

        #endregion

        #region PinnedHost_RejectsADifferentCertificate()

        [Test]
        public async Task PinnedHost_RejectsADifferentCertificate()
        {

            using var pinnedCertificate  = D13ChainSpikeTests.CreateSelfSignedLeaf("localhost");
            using var otherCertificate   = D13ChainSpikeTests.CreateSelfSignedLeaf("localhost");

            // The server presents a different certificate than the one that was pinned.
            var (server, _, url) = await StartRegistryAsync(otherCertificate);

            try
            {

                var pinStore = new CertificatePinStore();
                pinStore.Pin("localhost", CertificateFingerprint.FromCertificate(pinnedCertificate));

                await using var client = CreateClient(url, pinStore, AcceptUnpinnedForPairing: false);

                var versions = await client.GetVersionsAsync();

                Assert.Multiple(() => {
                    Assert.That(versions.IsSuccess,          Is.False, "a certificate that is not pinned must not be accepted");
                    Assert.That(versions.IsTransportFailure, Is.True,  "the TLS handshake must fail");
                });

            }
            finally
            {
                await server.Stop();
            }

        }

        #endregion

        #region PinningOverridesSystemTrust_EvenForAnUnpinnedButOtherwiseValidCertificate()

        [Test]
        public async Task UnpinnedHost_WithoutPairingMode_IsRejected()
        {

            using var certificate = D13ChainSpikeTests.CreateSelfSignedLeaf("localhost");

            var (server, _, url) = await StartRegistryAsync(certificate);

            try
            {

                // Nothing pinned, and this is not a pairing: a self-signed certificate is not enough.
                await using var client = CreateClient(url, new CertificatePinStore(), AcceptUnpinnedForPairing: false);

                var versions = await client.GetVersionsAsync();

                Assert.That(versions.IsSuccess, Is.False, "an unpinned self-signed certificate must be rejected outside a pairing");

            }
            finally
            {
                await server.Stop();
            }

        }

        #endregion

        #region CASignedLeaf_IsRejectedForPinning()

        [Test]
        public async Task CASignedLeaf_IsRejectedForPinning()
        {

            // D13: a leaf signed by a private CA can never be pinned, because the client never
            // receives the CA — so even in pairing mode it must be refused.
            using var ca    = D13ChainSpikeTests.CreateCA();
            using var leaf  = D13ChainSpikeTests.IssueLeaf(ca, "localhost");

            var (server, _, url) = await StartRegistryAsync(leaf);

            try
            {

                await using var client = CreateClient(url, new CertificatePinStore(), AcceptUnpinnedForPairing: true);

                var versions = await client.GetVersionsAsync();

                Assert.That(versions.IsSuccess, Is.False, "a CA-signed leaf must not be accepted for pinning");

            }
            finally
            {
                await server.Stop();
            }

        }

        #endregion

    }

}

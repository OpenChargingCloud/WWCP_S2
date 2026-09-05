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

using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.PKI;

using BCx509 = Org.BouncyCastle.X509;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The self-signed TLS material of a LAN endpoint (PLAN.md D13, Phase 11a), built on Hermod's
    /// <see cref="PKIFactory"/>.
    ///
    /// <para>
    /// The D13 spike showed that a Hermod TLS server transmits no private CA root: it builds its
    /// certificate context with <c>additionalCertificates: null</c>, so a client receives the leaf
    /// alone and can never see a separate CA. Since S2 Connect pins a CA by its SHA-256 fingerprint
    /// only (the <c>certificateFingerprint</c> map carries no certificate), the pinned certificate
    /// must be one the client actually receives. Hence the documented fallback:
    /// <see cref="CreateSelfSignedServerCertificate"/> issues a <em>single self-signed server
    /// certificate that is its own CA</em>, and that certificate's fingerprint is what gets pinned.
    /// Rotating it requires re-pairing.
    /// </para>
    ///
    /// <para>
    /// <see cref="CreateRootCA"/> and <see cref="IssueServerCertificate"/> build a real two-level
    /// hierarchy for deployments that distribute the root out of band (a WAN endpoint behind a
    /// reverse proxy, or a future Hermod that sends the chain). A leaf issued by such a CA must
    /// never be pinned itself — see <see cref="S2CertificateValidator"/>.
    /// </para>
    /// </summary>
    public static class SelfSignedCA
    {

        #region Data

        /// <summary>
        /// The default lifetime of a self-signed server certificate: 6 months, the leaf rotation
        /// interval of PLAN.md Phase 11a.
        /// </summary>
        public static readonly TimeSpan  DefaultServerCertificateLifetime  = TimeSpan.FromDays(180);

        /// <summary>
        /// The default lifetime of a root CA: 5 years.
        /// </summary>
        public static readonly TimeSpan  DefaultCALifetime                 = TimeSpan.FromDays(5 * 365);

        #endregion

        #region CreateSelfSignedServerCertificate(HostName, AdditionalHostNames = null, IPAddresses = null, Lifetime = null, KeyType = null)

        /// <summary>
        /// Create a self-signed server certificate that acts as its own certificate authority: the
        /// certificate a LAN endpoint presents, and whose SHA-256 fingerprint the peer pins during
        /// pairing (D13 fallback).
        /// </summary>
        /// <param name="HostName">The mDNS host name of the endpoint (e.g. "myhost.local"), used as common name and first subject alternative name.</param>
        /// <param name="AdditionalHostNames">Optional further DNS subject alternative names.</param>
        /// <param name="IPAddresses">Optional IP subject alternative names.</param>
        /// <param name="Lifetime">The lifetime (default: 6 months).</param>
        /// <param name="KeyType">The key type (default: an ECC key on secp256r1).</param>
        public static X509Certificate2 CreateSelfSignedServerCertificate(String                    HostName,
                                                                         IEnumerable<String>?      AdditionalHostNames   = null,
                                                                         IEnumerable<IIPAddress>?  IPAddresses           = null,
                                                                         TimeSpan?                 Lifetime              = null,
                                                                         S2KeyType?                KeyType               = null)
        {

            ArgumentException.ThrowIfNullOrWhiteSpace(HostName);

            var keyPair      = GenerateKeyPair(KeyType);

            var certificate  = PKIFactory.SelfSignServerCertificate(
                                   ServerName:       HostName,
                                   SubjectAltNames:  SubjectAltNames(HostName, AdditionalHostNames, IPAddresses),
                                   ServerKeyPair:    keyPair,
                                   LifeTime:         Lifetime ?? DefaultServerCertificateLifetime
                               );

            return ToDotNet(certificate, keyPair.Private);

        }

        #endregion

        #region CreateRootCA(Name, Lifetime = null, KeyType = null)

        /// <summary>
        /// Create a self-signed root certificate authority. Only useful when the root is
        /// distributed to the peers out of band: a Hermod TLS server does not transmit it (D13).
        /// </summary>
        /// <param name="Name">The common name of the CA (e.g. "My Home CA").</param>
        /// <param name="Lifetime">The lifetime (default: 5 years).</param>
        /// <param name="KeyType">The key type (default: an ECC key on secp256r1).</param>
        public static S2CertificateAuthority CreateRootCA(String      Name,
                                                          TimeSpan?   Lifetime   = null,
                                                          S2KeyType?  KeyType    = null)
        {

            ArgumentException.ThrowIfNullOrWhiteSpace(Name);

            var keyPair      = GenerateKeyPair(KeyType);

            var certificate  = PKIFactory.CreateRootCACertificate(
                                   SubjectName:  Name,
                                   RootKeyPair:  keyPair,
                                   LifeTime:     Lifetime ?? DefaultCALifetime
                               );

            return new S2CertificateAuthority(
                       certificate,
                       keyPair,
                       ToDotNet(certificate, null)
                   );

        }

        #endregion

        #region IssueServerCertificate(CA, HostName, AdditionalHostNames = null, IPAddresses = null, Lifetime = null, KeyType = null)

        /// <summary>
        /// Issue a server certificate signed by the given certificate authority. The leaf must never
        /// be pinned; the peers must know the CA out of band (D13).
        /// </summary>
        /// <param name="CA">The issuing certificate authority.</param>
        /// <param name="HostName">The host name, used as common name and first subject alternative name.</param>
        /// <param name="AdditionalHostNames">Optional further DNS subject alternative names.</param>
        /// <param name="IPAddresses">Optional IP subject alternative names.</param>
        /// <param name="Lifetime">The lifetime (default: 6 months).</param>
        /// <param name="KeyType">The key type (default: an ECC key on secp256r1).</param>
        public static X509Certificate2 IssueServerCertificate(S2CertificateAuthority    CA,
                                                              String                    HostName,
                                                              IEnumerable<String>?      AdditionalHostNames   = null,
                                                              IEnumerable<IIPAddress>?  IPAddresses           = null,
                                                              TimeSpan?                 Lifetime              = null,
                                                              S2KeyType?                KeyType               = null)
        {

            ArgumentNullException.ThrowIfNull(CA);
            ArgumentException.ThrowIfNullOrWhiteSpace(HostName);

            var keyPair      = GenerateKeyPair(KeyType);

            var certificate  = PKIFactory.SignServerCertificate(
                                   ServerName:         HostName,
                                   SubjectAltNames:    SubjectAltNames(HostName, AdditionalHostNames, IPAddresses),
                                   ServerPublicKey:    keyPair.Public,
                                   IssuerPrivateKey:   CA.KeyPair.Private,
                                   IssuerCertificate:  CA.Certificate,
                                   LifeTime:           Lifetime ?? DefaultServerCertificateLifetime
                               );

            return ToDotNet(certificate, keyPair.Private);

        }

        #endregion


        #region (private) SubjectAltNames(...) / GenerateKeyPair(...) / ToDotNet(...)

        private static List<GeneralName> SubjectAltNames(String                    HostName,
                                                         IEnumerable<String>?      AdditionalHostNames,
                                                         IEnumerable<IIPAddress>?  IPAddresses)
        {

            var names = new List<GeneralName> {
                            new (GeneralName.DnsName, HostName)
                        };

            foreach (var hostName in AdditionalHostNames ?? [])
            {
                if (!String.IsNullOrWhiteSpace(hostName) &&
                    !hostName.Equals(HostName, StringComparison.OrdinalIgnoreCase))
                {
                    names.Add(new GeneralName(GeneralName.DnsName, hostName));
                }
            }

            foreach (var ipAddress in IPAddresses ?? [])
                names.Add(new GeneralName(GeneralName.IPAddress, ipAddress.ToString()));

            return names;

        }

        private static AsymmetricCipherKeyPair GenerateKeyPair(S2KeyType? KeyType)

            => (KeyType ?? S2KeyType.ECC_P256) switch {
                   S2KeyType.RSA_2048  => PKIFactory.GenerateRSAKeyPair(2048),
                   S2KeyType.RSA_4096  => PKIFactory.GenerateRSAKeyPair(4096),
                   S2KeyType.ECC_P384  => PKIFactory.GenerateECCKeyPair("secp384r1"),
                   _                   => PKIFactory.GenerateECCKeyPair("secp256r1")
               };

        private static X509Certificate2 ToDotNet(BCx509.X509Certificate   Certificate,
                                                 AsymmetricKeyParameter?  PrivateKey)

            => Certificate.ToDotNet(PrivateKey)
                   ?? throw new InvalidOperationException("The generated certificate could not be converted into a .NET certificate!");

        #endregion

    }


    /// <summary>
    /// The key types <see cref="SelfSignedCA"/> can generate.
    /// </summary>
    public enum S2KeyType
    {

        /// <summary>
        /// An elliptic curve key on secp256r1 (the default: small, fast, widely supported).
        /// </summary>
        ECC_P256,

        /// <summary>
        /// An elliptic curve key on secp384r1.
        /// </summary>
        ECC_P384,

        /// <summary>
        /// An RSA key of 2048 bits.
        /// </summary>
        RSA_2048,

        /// <summary>
        /// An RSA key of 4096 bits.
        /// </summary>
        RSA_4096

    }


    /// <summary>
    /// A certificate authority created by <see cref="SelfSignedCA.CreateRootCA"/>: the certificate,
    /// its key pair (needed to issue leaves) and the .NET representation of the public certificate.
    /// </summary>
    public sealed class S2CertificateAuthority
    {

        /// <summary>
        /// The CA certificate.
        /// </summary>
        public BCx509.X509Certificate    Certificate       { get; }

        /// <summary>
        /// The key pair of the CA (its private key signs the leaves).
        /// </summary>
        public AsymmetricCipherKeyPair   KeyPair           { get; }

        /// <summary>
        /// The public CA certificate as a .NET certificate (without the private key).
        /// </summary>
        public X509Certificate2          PublicCertificate { get; }

        /// <summary>
        /// The SHA-256 fingerprint of the CA certificate.
        /// </summary>
        public CertificateFingerprint    Fingerprint
            => CertificateFingerprint.FromCertificate(PublicCertificate);

        internal S2CertificateAuthority(BCx509.X509Certificate   Certificate,
                                        AsymmetricCipherKeyPair  KeyPair,
                                        X509Certificate2         PublicCertificate)
        {
            this.Certificate        = Certificate;
            this.KeyPair            = KeyPair;
            this.PublicCertificate  = PublicCertificate;
        }

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"CA {PublicCertificate.Subject} ({Fingerprint.ToHexString()})";

    }

}

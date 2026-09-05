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
using System.Security.Authentication;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// The TLS profiles of S2 Connect (PLAN.md §3.6, D13): the protocol versions of the modern,
    /// the interoperable and the default profile, the AEAD-only cipher suite list and the
    /// platform-dependent cipher suites policy (Windows' Schannel does not take one).
    /// </summary>
    [TestFixture]
    public sealed class TLSProfilesTests
    {

        #region Data

        // SslProtocols.Ssl3, .Tls (TLS 1.0) and .Tls11 are [Obsolete] in .NET; their numeric
        // values are used here so that this fixture does not have to suppress the obsoletion.
        private const SslProtocols  Ssl3   = (SslProtocols)  48;
        private const SslProtocols  Tls10  = (SslProtocols) 192;
        private const SslProtocols  Tls11  = (SslProtocols) 768;

        #endregion


        #region Modern_IsTLS13Only()

        /// <summary>
        /// The modern profile is exactly TLS 1.3 - nothing else, not even TLS 1.2.
        /// </summary>
        [Test]
        public void Modern_IsTLS13Only()
        {

            Assert.Multiple(() => {

                Assert.That(TLSProfiles.Modern,                       Is.EqualTo(SslProtocols.Tls13));
                Assert.That(TLSProfiles.Modern.HasFlag(SslProtocols.Tls12),  Is.False, "the modern profile must not fall back to TLS 1.2");

            });

        }

        #endregion

        #region Interoperable_IsTLS13AndTLS12()

        /// <summary>
        /// The interoperable profile is TLS 1.3 with TLS 1.2 as the only fallback.
        /// </summary>
        [Test]
        public void Interoperable_IsTLS13AndTLS12()
        {

            Assert.Multiple(() => {

                Assert.That(TLSProfiles.Interoperable,                              Is.EqualTo(SslProtocols.Tls13 | SslProtocols.Tls12));
                Assert.That(TLSProfiles.Interoperable.HasFlag(SslProtocols.Tls13),  Is.True);
                Assert.That(TLSProfiles.Interoperable.HasFlag(SslProtocols.Tls12),  Is.True);

            });

        }

        #endregion

        #region Interoperable_ContainsNoLegacyProtocol()

        /// <summary>
        /// Never anything below TLS 1.2: no TLS 1.1, no TLS 1.0 and no SSL 3.0.
        /// </summary>
        [Test]
        public void Interoperable_ContainsNoLegacyProtocol()
        {

            Assert.Multiple(() => {

                Assert.That(TLSProfiles.Interoperable.HasFlag(Tls11),  Is.False, "TLS 1.1 must not be part of any S2 Connect profile");
                Assert.That(TLSProfiles.Interoperable.HasFlag(Tls10),  Is.False, "TLS 1.0 must not be part of any S2 Connect profile");
                Assert.That(TLSProfiles.Interoperable.HasFlag(Ssl3),   Is.False, "SSL 3.0 must not be part of any S2 Connect profile");

                Assert.That(TLSProfiles.Modern.HasFlag(Tls11),         Is.False);
                Assert.That(TLSProfiles.Modern.HasFlag(Tls10),         Is.False);
                Assert.That(TLSProfiles.Modern.HasFlag(Ssl3),          Is.False);

            });

        }

        #endregion

        #region Default_EqualsInteroperable()

        /// <summary>
        /// The default profile of this library is the interoperable one: S2 Connect does not
        /// mandate TLS 1.3 and a LAN resource manager may be an embedded device.
        /// </summary>
        [Test]
        public void Default_EqualsInteroperable()
        {

            Assert.Multiple(() => {

                Assert.That(TLSProfiles.Default,  Is.EqualTo(TLSProfiles.Interoperable));
                Assert.That(TLSProfiles.Default,  Is.Not.EqualTo(TLSProfiles.Modern));

            });

        }

        #endregion


        #region ModernCipherSuites_StartWithTheThreeTLS13Suites()

        /// <summary>
        /// The cipher suite list is not empty and is ordered strongest first: the three TLS 1.3
        /// suites come before the TLS 1.2 ECDHE suites.
        /// </summary>
        [Test]
        public void ModernCipherSuites_StartWithTheThreeTLS13Suites()
        {

            var cipherSuites = TLSProfiles.ModernCipherSuites;

            Assert.Multiple(() => {

                Assert.That(cipherSuites,        Is.Not.Null);
                Assert.That(cipherSuites.Count,  Is.GreaterThanOrEqualTo(3));

                Assert.That(cipherSuites[0],     Is.EqualTo(TlsCipherSuite.TLS_AES_256_GCM_SHA384));
                Assert.That(cipherSuites[1],     Is.EqualTo(TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256));
                Assert.That(cipherSuites[2],     Is.EqualTo(TlsCipherSuite.TLS_AES_128_GCM_SHA256));

            });

        }

        #endregion

        #region ModernCipherSuites_AreAEADOnly()

        /// <summary>
        /// Every suite is an AEAD suite: AES-GCM or ChaCha20-Poly1305. Nothing with a NULL
        /// cipher, nothing with RC4 and nothing with CBC/SHA-1.
        /// </summary>
        [Test]
        public void ModernCipherSuites_AreAEADOnly()
        {

            Assert.That(TLSProfiles.ModernCipherSuites, Is.Not.Empty);

            Assert.Multiple(() => {

                foreach (var cipherSuite in TLSProfiles.ModernCipherSuites)
                {

                    var name = cipherSuite.ToString();

                    Assert.That(name.Contains("GCM",      StringComparison.Ordinal) ||
                                name.Contains("CHACHA20", StringComparison.Ordinal),
                                Is.True,
                                $"'{name}' is neither an AES-GCM nor a ChaCha20-Poly1305 suite!");

                    Assert.That(name,  Does.Not.Contain("NULL"),  $"'{name}' has a NULL cipher!");
                    Assert.That(name,  Does.Not.Contain("RC4"),   $"'{name}' uses RC4!");
                    Assert.That(name,  Does.Not.Contain("_CBC_"), $"'{name}' is a CBC suite!");
                    Assert.That(name,  Does.Not.Contain("_3DES"), $"'{name}' uses 3DES!");

                }

            });

        }

        #endregion

        #region ModernCipherSuites_HaveNoDuplicates()

        /// <summary>
        /// A cipher suites policy with a duplicate entry would be a copy-and-paste accident.
        /// </summary>
        [Test]
        public void ModernCipherSuites_HaveNoDuplicates()
        {

            var cipherSuites = TLSProfiles.ModernCipherSuites;

            Assert.That(cipherSuites.Distinct().Count(),  Is.EqualTo(cipherSuites.Count));

        }

        #endregion


        #region SupportsCipherSuitesPolicy_MatchesThePlatform()

        /// <summary>
        /// Linux and macOS support a cipher suites policy, Windows (Schannel) does not.
        /// </summary>
        [Test]
        public void SupportsCipherSuitesPolicy_MatchesThePlatform()
        {

            Assert.That(TLSProfiles.SupportsCipherSuitesPolicy,  Is.EqualTo(!OperatingSystem.IsWindows()));

        }

        #endregion

        #region ApplyModernCipherSuites_MatchesThePlatform()

        /// <summary>
        /// Applying the modern cipher suites restricts the options everywhere except on Windows,
        /// whose Schannel takes no cipher suites policy.
        /// </summary>
        [Test]
        public void ApplyModernCipherSuites_MatchesThePlatform()
        {

            var options  = new SslClientAuthenticationOptions();

            var applied  = TLSProfiles.ApplyModernCipherSuites(options);

            Assert.Multiple(() => {

                Assert.That(applied, Is.EqualTo(!OperatingSystem.IsWindows()));
                Assert.That(applied, Is.EqualTo(TLSProfiles.SupportsCipherSuitesPolicy));

                if (!OperatingSystem.IsWindows())
                    Assert.That(options.CipherSuitesPolicy, Is.Not.Null, "a platform supporting cipher suites policies must get one!");

            });

        }

        #endregion

        #region ApplyModernCipherSuites_IsStable()

        /// <summary>
        /// Applying twice never throws and always answers the same way.
        /// </summary>
        [Test]
        public void ApplyModernCipherSuites_IsStable()
        {

            var first   = TLSProfiles.ApplyModernCipherSuites(new SslClientAuthenticationOptions());
            var second  = TLSProfiles.ApplyModernCipherSuites(new SslClientAuthenticationOptions());

            Assert.Multiple(() => {
                Assert.That(first,   Is.EqualTo(second));
                Assert.That(first,   Is.EqualTo(TLSProfiles.SupportsCipherSuitesPolicy));
            });

        }

        #endregion

        #region ApplyModernCipherSuites_RejectsNull()

        /// <summary>
        /// Applying to null options is a programming error.
        /// </summary>
        [Test]
        public void ApplyModernCipherSuites_RejectsNull()
        {
            Assert.That(() => TLSProfiles.ApplyModernCipherSuites(null!),
                        Throws.InstanceOf<ArgumentNullException>());
        }

        #endregion

    }

}

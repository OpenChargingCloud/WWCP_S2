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

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The TLS profiles of S2 Connect (PLAN.md §3.6, D13). S2 Connect requires HTTPS and WSS for
    /// every endpoint; these profiles fix the protocol versions and, where the platform allows it,
    /// the cipher suites.
    ///
    /// A <see cref="CipherSuitesPolicy"/> is only ever applied on <em>clients</em> and only on
    /// platforms that support it: Windows' Schannel does not, and constructing the policy there
    /// throws <see cref="PlatformNotSupportedException"/>. Hermod's servers expose no cipher policy
    /// at all, so a server is pinned through <see cref="SslProtocols"/> alone.
    /// </summary>
    public static class TLSProfiles
    {

        #region Protocol versions

        /// <summary>
        /// The modern profile: TLS 1.3 only. This is what a fresh S2 Connect deployment uses.
        /// </summary>
        public static SslProtocols  Modern
            => SslProtocols.Tls13;

        /// <summary>
        /// The interoperable profile: TLS 1.3 with TLS 1.2 as fallback, for peers that do not speak
        /// TLS 1.3 yet. Never includes anything below TLS 1.2.
        /// </summary>
        public static SslProtocols  Interoperable
            => SslProtocols.Tls13 | SslProtocols.Tls12;

        /// <summary>
        /// The default profile of this library (<see cref="Interoperable"/>): S2 Connect does not
        /// mandate TLS 1.3, and a LAN resource manager may be an embedded device.
        /// </summary>
        public static SslProtocols  Default
            => Interoperable;

        #endregion

        #region Cipher suites

        /// <summary>
        /// Whether this platform supports a <see cref="System.Net.Security.CipherSuitesPolicy"/>
        /// (Linux and macOS do, Windows does not).
        /// </summary>
        public static Boolean       SupportsCipherSuitesPolicy
            => !OperatingSystem.IsWindows();

        /// <summary>
        /// The AEAD cipher suites of the modern profile, strongest first: the three TLS 1.3 suites
        /// and, for the interoperable profile, ECDHE with AES-GCM or ChaCha20-Poly1305.
        /// </summary>
        public static IReadOnlyList<TlsCipherSuite>  ModernCipherSuites { get; }
            = [
                  TlsCipherSuite.TLS_AES_256_GCM_SHA384,
                  TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256,
                  TlsCipherSuite.TLS_AES_128_GCM_SHA256,
                  TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
                  TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
                  TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
                  TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
                  TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
                  TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256
              ];

        /// <summary>
        /// Restrict the given TLS <em>client</em> options to the AEAD cipher suites of the modern
        /// profile, where the platform supports it. On Windows, whose Schannel takes no cipher
        /// suites policy, the options are left untouched.
        ///
        /// <para>
        /// The policy is applied rather than returned on purpose: <c>CipherSuitesPolicy</c> is
        /// unsupported on Windows, so naming it in a public signature would make every Windows
        /// caller trip the platform-compatibility analyzer.
        /// </para>
        /// </summary>
        /// <param name="Options">The TLS client authentication options to restrict.</param>
        /// <returns>Whether a cipher suites policy was applied.</returns>
        public static Boolean ApplyModernCipherSuites(SslClientAuthenticationOptions Options)
        {

            ArgumentNullException.ThrowIfNull(Options);

            // Windows' Schannel does not take a cipher suites policy; constructing one throws.
            if (OperatingSystem.IsWindows())
                return false;

            try
            {
                Options.CipherSuitesPolicy = new CipherSuitesPolicy(ModernCipherSuites);
                return true;
            }
            catch (PlatformNotSupportedException)
            {
                return false;
            }

        }

        #endregion

    }

}

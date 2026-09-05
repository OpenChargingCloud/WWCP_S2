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

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// Protects the secrets a persistent store writes to disk (access tokens, pending tokens):
    /// <see cref="Protect"/> is applied before a value is written, <see cref="Unprotect"/> after
    /// it is read back (PLAN.md D8). The default implementation is a no-op (plaintext); a
    /// deployment may plug in DPAPI, a KMS or an HSM. Protect and Unprotect must round-trip.
    /// </summary>
    public interface ISecretProtector
    {

        /// <summary>
        /// The identifier of this protector, written to the file so that a value protected by one
        /// scheme is not read back by another (an empty string for the plaintext default).
        /// </summary>
        String  SchemeId    { get; }

        /// <summary>
        /// Protect the given plaintext secret.
        /// </summary>
        /// <param name="Plaintext">A plaintext secret.</param>
        /// <returns>The protected representation (stored verbatim).</returns>
        String Protect(String Plaintext);

        /// <summary>
        /// Recover the plaintext of a value written by <see cref="Protect"/>.
        /// </summary>
        /// <param name="Protected">A protected value.</param>
        /// <returns>The plaintext secret.</returns>
        String Unprotect(String Protected);

    }


    /// <summary>
    /// The default secret protector: it stores secrets verbatim (plaintext). Its scheme id is the
    /// empty string, so a plaintext file stays readable regardless of which protector is configured.
    /// </summary>
    public sealed class PlaintextSecretProtector : ISecretProtector
    {

        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static PlaintextSecretProtector  Instance    { get; } = new();

        /// <inheritdoc/>
        public String  SchemeId    => "";

        private PlaintextSecretProtector()
        { }

        /// <inheritdoc/>
        public String Protect(String Plaintext)
            => Plaintext;

        /// <inheritdoc/>
        public String Unprotect(String Protected)
            => Protected;

    }

}

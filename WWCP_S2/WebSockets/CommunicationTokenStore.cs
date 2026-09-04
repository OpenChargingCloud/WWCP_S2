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

using System.Collections.Concurrent;
using System.Security.Cryptography;

#endregion

namespace cloud.charging.open.protocols.S2.WebSockets
{

    /// <summary>
    /// The single-use, short-lived communication tokens with which a WebSocket client
    /// authenticates its upgrade request (S2 Connect: "For each S2 WebSocket session the client
    /// must authenticate itself using the commToken in the authorization header";
    /// s2-connect-common.yml, CommunicationToken: "valid for maximum 30 seconds"). A token is
    /// issued by session initiation (Phase 8) together with an opaque identity of the client
    /// node and is redeemed exactly once by the WebSocket server.
    /// </summary>
    public sealed class CommunicationTokenStore
    {

        #region Data

        private readonly ConcurrentDictionary<String, (Object? Identity, DateTimeOffset ExpiresAt)>  tokens = new (StringComparer.Ordinal);
        private readonly TimeProvider                                                                timeProvider;

        #endregion

        #region Properties

        /// <summary>
        /// The default lifetime of an issued token.
        /// </summary>
        public TimeSpan  DefaultLifetime    { get; }

        /// <summary>
        /// The number of tokens currently stored (including expired ones not yet purged).
        /// </summary>
        public Int32     Count
            => tokens.Count;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new communication token store.
        /// </summary>
        /// <param name="TimeProvider">An optional time provider for the expiry (default: the system clock).</param>
        /// <param name="DefaultLifetime">The default lifetime of an issued token (default: 30 seconds).</param>
        public CommunicationTokenStore(TimeProvider?  TimeProvider      = null,
                                       TimeSpan?      DefaultLifetime   = null)
        {

            this.timeProvider     = TimeProvider    ?? System.TimeProvider.System;
            this.DefaultLifetime  = DefaultLifetime ?? S2ConnectDefaults.CommunicationTokenLifetime;

            if (this.DefaultLifetime <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(DefaultLifetime), "The token lifetime must be positive!");

        }

        #endregion


        #region (static) GenerateToken(NumberOfBytes = 32)

        /// <summary>
        /// Generate a new random token: the given number of bytes from a cryptographically
        /// secure generator, encoded as standard Base64.
        /// </summary>
        /// <param name="NumberOfBytes">The number of random bytes (at least 32).</param>
        public static String GenerateToken(Int32 NumberOfBytes = S2ConnectDefaults.MinCommunicationTokenLength)
        {

            ArgumentOutOfRangeException.ThrowIfLessThan(NumberOfBytes, S2ConnectDefaults.MinCommunicationTokenLength);

            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(NumberOfBytes));

        }

        #endregion

        #region Issue(Identity = null, Lifetime = null, Token = null)

        /// <summary>
        /// Issue a token that can be redeemed once within its lifetime.
        /// </summary>
        /// <param name="Identity">An opaque identity of the client (e.g. its node id) returned when the token is redeemed.</param>
        /// <param name="Lifetime">An optional lifetime (default: the store's default lifetime).</param>
        /// <param name="Token">An optional token text; a new random token is generated when omitted.</param>
        /// <returns>The issued token.</returns>
        public String Issue(Object?    Identity   = null,
                            TimeSpan?  Lifetime   = null,
                            String?    Token      = null)
        {

            var token     = Token ?? GenerateToken();
            var lifetime  = Lifetime ?? DefaultLifetime;

            if (lifetime <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(Lifetime), "The token lifetime must be positive!");

            if (!tokens.TryAdd(token, (Identity, timeProvider.GetUtcNow() + lifetime)))
                throw new InvalidOperationException("The token is already issued!");

            return token;

        }

        #endregion

        #region TryRedeem(Token, out Identity)

        /// <summary>
        /// Redeem a token: it is removed from the store, so a second redemption fails.
        /// </summary>
        /// <param name="Token">A token.</param>
        /// <param name="Identity">The identity given when the token was issued.</param>
        /// <returns>False when the token is unknown, already redeemed or expired.</returns>
        public Boolean TryRedeem(String        Token,
                                 out Object?   Identity)
        {

            Identity = null;

            if (!tokens.TryRemove(Token, out var entry))
                return false;

            if (entry.ExpiresAt < timeProvider.GetUtcNow())
                return false;

            Identity = entry.Identity;
            return true;

        }

        #endregion

        #region Purge()

        /// <summary>
        /// Remove all expired tokens.
        /// </summary>
        /// <returns>The number of removed tokens.</returns>
        public Int32 Purge()
        {

            var now      = timeProvider.GetUtcNow();
            var removed  = 0;

            foreach (var kvp in tokens)
            {
                if (kvp.Value.ExpiresAt < now && tokens.TryRemove(kvp.Key, out _))
                    removed++;
            }

            return removed;

        }

        #endregion

    }

}

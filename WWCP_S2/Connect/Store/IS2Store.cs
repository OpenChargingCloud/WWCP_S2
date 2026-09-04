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
    /// The persistent store of the S2 Connect security material (PLAN.md D8, §3.5): pairings
    /// with their active access tokens, session initiation URLs and pinned certificate
    /// fingerprints, the pending access tokens of running session initiations, and the
    /// tombstones of unpaired nodes. Every operation must be durable before it returns and
    /// implementations must be thread-safe. A failing operation throws; the callers map that to
    /// HTTP 500 and abort the running procedure (S2 Connect 1.0.0 requires tokens to be
    /// persisted before they are confirmed).
    /// </summary>
    public interface IS2Store
    {

        #region Pairings

        /// <summary>
        /// Return all pairings, optionally only those of the given local node.
        /// </summary>
        /// <param name="LocalNodeId">An optional identification of a local node.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        ValueTask<IReadOnlyList<Pairing>>             GetPairingsAsync               (Node_Id?           LocalNodeId         = null,
                                                                                      CancellationToken  CancellationToken   = default);

        /// <summary>
        /// Return the pairing of the given local and remote node, or null.
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        ValueTask<Pairing?>                           GetPairingAsync                (Node_Id            LocalNodeId,
                                                                                      Node_Id            RemoteNodeId,
                                                                                      CancellationToken  CancellationToken   = default);

        /// <summary>
        /// Store the given pairing atomically, replacing an earlier pairing of the same local and
        /// remote node (and thereby every earlier access token of the pair) and clearing a
        /// tombstone of the pair. Pending access tokens of the pair are kept.
        /// </summary>
        /// <param name="Pairing">The pairing to store.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        /// <returns>The replaced pairing, or null when the pair was not paired before.</returns>
        ValueTask<Pairing?>                           AddOrReplacePairingAsync       (Pairing            Pairing,
                                                                                      CancellationToken  CancellationToken   = default);

        /// <summary>
        /// Remove the pairing of the given local and remote node together with all of its security
        /// material (including pending access tokens), without writing a tombstone.
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        /// <returns>The removed pairing, or null when the pair was not paired.</returns>
        ValueTask<Pairing?>                           RemovePairingAsync             (Node_Id            LocalNodeId,
                                                                                      Node_Id            RemoteNodeId,
                                                                                      CancellationToken  CancellationToken   = default);

        #endregion

        #region Pending access tokens (session initiation)

        /// <summary>
        /// Remember a pending access token of a running session initiation.
        /// </summary>
        /// <param name="PendingAccessToken">The pending access token.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        ValueTask                                     AddPendingAccessTokenAsync     (PendingAccessToken  PendingAccessToken,
                                                                                      CancellationToken   CancellationToken   = default);

        /// <summary>
        /// Return the pending access tokens of the given pair, newest first.
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        ValueTask<IReadOnlyList<PendingAccessToken>>  GetPendingAccessTokensAsync    (Node_Id            LocalNodeId,
                                                                                      Node_Id            RemoteNodeId,
                                                                                      CancellationToken  CancellationToken   = default);

        /// <summary>
        /// Find the pending access token entry carrying the given token (the bearer token of
        /// confirmAccessToken), or null.
        /// </summary>
        /// <param name="Token">A pending access token.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        ValueTask<PendingAccessToken?>                FindPendingAccessTokenAsync    (AccessToken        Token,
                                                                                      CancellationToken  CancellationToken   = default);

        /// <summary>
        /// Make the given pending access token the active token of the pairing atomically,
        /// invalidating the old active token and every other pending token of the pair
        /// (S2 Connect 1.0.0, "6. Activate new accessToken", "8. Remove old accessToken").
        /// Activating the token that is already active only removes the pending tokens; a null
        /// result changes nothing.
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="Token">The pending (or already active) access token.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        /// <returns>The updated pairing, or null when the pair is not paired or the token is neither pending nor active.</returns>
        ValueTask<Pairing?>                           ActivateAccessTokenAsync       (Node_Id            LocalNodeId,
                                                                                      Node_Id            RemoteNodeId,
                                                                                      AccessToken        Token,
                                                                                      CancellationToken  CancellationToken   = default);

        /// <summary>
        /// Remove pending access tokens: all of a pair, or all created before the given time
        /// (expired ones), or both criteria combined; null criteria match everything.
        /// </summary>
        /// <param name="LocalNodeId">An optional identification of the local node.</param>
        /// <param name="RemoteNodeId">An optional identification of the remote node.</param>
        /// <param name="CreatedBefore">An optional creation time limit.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        /// <returns>The number of removed tokens.</returns>
        ValueTask<Int32>                              RemovePendingAccessTokensAsync (Node_Id?           LocalNodeId         = null,
                                                                                      Node_Id?           RemoteNodeId        = null,
                                                                                      DateTimeOffset?    CreatedBefore       = null,
                                                                                      CancellationToken  CancellationToken   = default);

        /// <summary>
        /// The access tokens a communication client may try for the given pair: the active token
        /// first, then the pending tokens newest first (S2 Connect 1.0.0, "1. POST
        /// /[version]/initiateSession": "the communication client must keep a persisted list of
        /// accessTokens"). Pending tokens are returned even when the pair is not (or no longer) paired.
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        ValueTask<IReadOnlyList<AccessToken>>         GetAccessTokenCandidatesAsync  (Node_Id            LocalNodeId,
                                                                                      Node_Id            RemoteNodeId,
                                                                                      CancellationToken  CancellationToken   = default);

        #endregion

        #region Unpairing

        /// <summary>
        /// Unpair atomically: remove the pairing and its pending access tokens and, when the pair
        /// was paired, write a tombstone, so that a later initiateSession of the remote node is
        /// answered with NoLongerPaired instead of 401 (S2 Connect 1.0.0, "Unpairing process").
        /// Pending tokens of the pair are removed even when the pair was not paired; a second
        /// unpairing keeps the first tombstone.
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="At">The time of the unpairing.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        /// <returns>The removed pairing, or null when the pair was not paired.</returns>
        ValueTask<Pairing?>                           UnpairAsync                    (Node_Id            LocalNodeId,
                                                                                      Node_Id            RemoteNodeId,
                                                                                      DateTimeOffset     At,
                                                                                      CancellationToken  CancellationToken   = default);

        /// <summary>
        /// When the given pair was paired and unpaired later (and not paired again since),
        /// the time of the unpairing; otherwise null.
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        ValueTask<DateTimeOffset?>                    GetUnpairedAtAsync             (Node_Id            LocalNodeId,
                                                                                      Node_Id            RemoteNodeId,
                                                                                      CancellationToken  CancellationToken   = default);

        #endregion

    }

}

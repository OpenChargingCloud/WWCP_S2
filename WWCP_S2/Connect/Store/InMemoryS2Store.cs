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
    /// The reference implementation of <see cref="IS2Store"/>: a thread-safe in-memory store
    /// (PLAN.md D8). Nothing survives a restart; production deployments plug in a durable store.
    /// </summary>
    public sealed class InMemoryS2Store : IS2Store
    {

        #region Data

        private readonly Lock                                          lockObject     = new ();
        private readonly Dictionary<(Node_Id, Node_Id), Pairing>       pairings       = [];
        private readonly List<PendingAccessToken>                      pendingTokens  = [];
        private readonly Dictionary<(Node_Id, Node_Id), DateTimeOffset>  tombstones   = [];

        #endregion

        #region Properties

        /// <summary>
        /// The number of stored pairings.
        /// </summary>
        public Int32 Count
        {
            get
            {
                lock (lockObject)
                {
                    return pairings.Count;
                }
            }
        }

        /// <summary>
        /// The number of pending access tokens.
        /// </summary>
        public Int32 PendingCount
        {
            get
            {
                lock (lockObject)
                {
                    return pendingTokens.Count;
                }
            }
        }

        /// <summary>
        /// The number of tombstones of unpaired pairs.
        /// </summary>
        public Int32 TombstoneCount
        {
            get
            {
                lock (lockObject)
                {
                    return tombstones.Count;
                }
            }
        }

        #endregion


        #region GetPairingsAsync              (LocalNodeId = null, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<Pairing>> GetPairingsAsync(Node_Id?           LocalNodeId         = null,
                                                                  CancellationToken  CancellationToken   = default)
        {

            CancellationToken.ThrowIfCancellationRequested();

            lock (lockObject)
            {

                IReadOnlyList<Pairing> result = LocalNodeId.HasValue
                                                    ? [.. pairings.Values.Where(pairing => pairing.LocalNodeId == LocalNodeId.Value)]
                                                    : [.. pairings.Values];

                return ValueTask.FromResult(result);

            }

        }

        #endregion

        #region GetPairingAsync               (LocalNodeId, RemoteNodeId, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<Pairing?> GetPairingAsync(Node_Id            LocalNodeId,
                                                   Node_Id            RemoteNodeId,
                                                   CancellationToken  CancellationToken   = default)
        {

            CancellationToken.ThrowIfCancellationRequested();

            lock (lockObject)
            {
                return ValueTask.FromResult(
                           pairings.TryGetValue((LocalNodeId, RemoteNodeId), out var pairing)
                               ? pairing
                               : null
                       );
            }

        }

        #endregion

        #region AddOrReplacePairingAsync      (Pairing, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<Pairing?> AddOrReplacePairingAsync(Pairing            Pairing,
                                                            CancellationToken  CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Pairing);
            CancellationToken.ThrowIfCancellationRequested();

            lock (lockObject)
            {

                var key = (Pairing.LocalNodeId, Pairing.RemoteNodeId);

                pairings.TryGetValue(key, out var replaced);
                pairings[key] = Pairing;
                tombstones.Remove(key);

                return ValueTask.FromResult(replaced);

            }

        }

        #endregion

        #region RemovePairingAsync            (LocalNodeId, RemoteNodeId, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<Pairing?> RemovePairingAsync(Node_Id            LocalNodeId,
                                                      Node_Id            RemoteNodeId,
                                                      CancellationToken  CancellationToken   = default)
        {

            CancellationToken.ThrowIfCancellationRequested();

            lock (lockObject)
            {

                pendingTokens.RemoveAll(pending => pending.LocalNodeId == LocalNodeId && pending.RemoteNodeId == RemoteNodeId);

                return ValueTask.FromResult(
                           pairings.Remove((LocalNodeId, RemoteNodeId), out var removed)
                               ? removed
                               : null
                       );

            }

        }

        #endregion


        #region AddPendingAccessTokenAsync    (PendingAccessToken, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask AddPendingAccessTokenAsync(PendingAccessToken  PendingAccessToken,
                                                    CancellationToken   CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(PendingAccessToken);
            CancellationToken.ThrowIfCancellationRequested();

            lock (lockObject)
            {

                if (!pendingTokens.Any(pending => pending.Token.Equals(PendingAccessToken.Token)))
                    pendingTokens.Add(PendingAccessToken);

            }

            return ValueTask.CompletedTask;

        }

        #endregion

        #region GetPendingAccessTokensAsync   (LocalNodeId, RemoteNodeId, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<PendingAccessToken>> GetPendingAccessTokensAsync(Node_Id            LocalNodeId,
                                                                                        Node_Id            RemoteNodeId,
                                                                                        CancellationToken  CancellationToken   = default)
        {

            CancellationToken.ThrowIfCancellationRequested();

            lock (lockObject)
            {

                IReadOnlyList<PendingAccessToken> result = [.. pendingTokens.
                                                                   Where  (pending => pending.LocalNodeId == LocalNodeId && pending.RemoteNodeId == RemoteNodeId).
                                                                   OrderByDescending(pending => pending.CreatedAt)];

                return ValueTask.FromResult(result);

            }

        }

        #endregion

        #region FindPendingAccessTokenAsync   (Token, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<PendingAccessToken?> FindPendingAccessTokenAsync(AccessToken        Token,
                                                                          CancellationToken  CancellationToken   = default)
        {

            CancellationToken.ThrowIfCancellationRequested();

            lock (lockObject)
            {
                return ValueTask.FromResult(pendingTokens.FirstOrDefault(pending => pending.Token.ConstantTimeEquals(Token)));
            }

        }

        #endregion

        #region ActivateAccessTokenAsync      (LocalNodeId, RemoteNodeId, Token, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<Pairing?> ActivateAccessTokenAsync(Node_Id            LocalNodeId,
                                                            Node_Id            RemoteNodeId,
                                                            AccessToken        Token,
                                                            CancellationToken  CancellationToken   = default)
        {

            CancellationToken.ThrowIfCancellationRequested();

            lock (lockObject)
            {

                var key = (LocalNodeId, RemoteNodeId);

                if (!pairings.TryGetValue(key, out var pairing))
                    return ValueTask.FromResult<Pairing?>(null);

                var isPending  = pendingTokens.Any(pending => pending.LocalNodeId == LocalNodeId && pending.RemoteNodeId == RemoteNodeId && pending.Token.Equals(Token));
                var isActive   = pairing.AccessToken.Equals(Token);

                if (!isPending && !isActive)
                    return ValueTask.FromResult<Pairing?>(null);

                if (!isActive)
                {
                    pairing        = pairing.WithAccessToken(Token);
                    pairings[key]  = pairing;
                }

                pendingTokens.RemoveAll(pending => pending.LocalNodeId == LocalNodeId && pending.RemoteNodeId == RemoteNodeId);

                return ValueTask.FromResult<Pairing?>(pairing);

            }

        }

        #endregion

        #region RemovePendingAccessTokensAsync(LocalNodeId = null, RemoteNodeId = null, CreatedBefore = null, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<Int32> RemovePendingAccessTokensAsync(Node_Id?           LocalNodeId         = null,
                                                               Node_Id?           RemoteNodeId        = null,
                                                               DateTimeOffset?    CreatedBefore       = null,
                                                               CancellationToken  CancellationToken   = default)
        {

            CancellationToken.ThrowIfCancellationRequested();

            lock (lockObject)
            {
                return ValueTask.FromResult(
                           pendingTokens.RemoveAll(pending => (!LocalNodeId.  HasValue || pending.LocalNodeId  == LocalNodeId.Value)  &&
                                                              (!RemoteNodeId. HasValue || pending.RemoteNodeId == RemoteNodeId.Value) &&
                                                              (!CreatedBefore.HasValue || pending.CreatedAt    <  CreatedBefore.Value))
                       );
            }

        }

        #endregion

        #region GetAccessTokenCandidatesAsync (LocalNodeId, RemoteNodeId, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<AccessToken>> GetAccessTokenCandidatesAsync(Node_Id            LocalNodeId,
                                                                                   Node_Id            RemoteNodeId,
                                                                                   CancellationToken  CancellationToken   = default)
        {

            CancellationToken.ThrowIfCancellationRequested();

            lock (lockObject)
            {

                var candidates = new List<AccessToken>();

                if (pairings.TryGetValue((LocalNodeId, RemoteNodeId), out var pairing))
                    candidates.Add(pairing.AccessToken);

                foreach (var pending in pendingTokens.
                                            Where  (pending => pending.LocalNodeId == LocalNodeId && pending.RemoteNodeId == RemoteNodeId).
                                            OrderByDescending(pending => pending.CreatedAt))
                {
                    if (!candidates.Any(candidate => candidate.Equals(pending.Token)))
                        candidates.Add(pending.Token);
                }

                return ValueTask.FromResult<IReadOnlyList<AccessToken>>(candidates);

            }

        }

        #endregion


        #region UnpairAsync                   (LocalNodeId, RemoteNodeId, At, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<Pairing?> UnpairAsync(Node_Id            LocalNodeId,
                                               Node_Id            RemoteNodeId,
                                               DateTimeOffset     At,
                                               CancellationToken  CancellationToken   = default)
        {

            CancellationToken.ThrowIfCancellationRequested();

            lock (lockObject)
            {

                var key = (LocalNodeId, RemoteNodeId);

                pendingTokens.RemoveAll(pending => pending.LocalNodeId == LocalNodeId && pending.RemoteNodeId == RemoteNodeId);

                if (pairings.Remove(key, out var removed))
                {
                    tombstones[key] = At;
                    return ValueTask.FromResult<Pairing?>(removed);
                }

                return ValueTask.FromResult<Pairing?>(null);

            }

        }

        #endregion

        #region GetUnpairedAtAsync            (LocalNodeId, RemoteNodeId, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<DateTimeOffset?> GetUnpairedAtAsync(Node_Id            LocalNodeId,
                                                             Node_Id            RemoteNodeId,
                                                             CancellationToken  CancellationToken   = default)
        {

            CancellationToken.ThrowIfCancellationRequested();

            lock (lockObject)
            {
                return ValueTask.FromResult(
                           tombstones.TryGetValue((LocalNodeId, RemoteNodeId), out var at)
                               ? at
                               : (DateTimeOffset?) null
                       );
            }

        }

        #endregion


        #region Clear()

        /// <summary>
        /// Remove every pairing, pending token and tombstone.
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                pairings.     Clear();
                pendingTokens.Clear();
                tombstones.   Clear();
            }
        }

        #endregion

    }

}

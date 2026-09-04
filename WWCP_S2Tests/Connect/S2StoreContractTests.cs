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

using cloud.charging.open.protocols.S2.Connect;

using static cloud.charging.open.protocols.S2.Tests.Connect.S2StoreTestData;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The node identifications and pairings shared by the store contract tests and the
    /// fixtures derived from them (kept outside the generic fixture: no static members on
    /// generic types).
    /// </summary>
    internal static class S2StoreTestData
    {

        public static readonly Node_Id         LocalNodeA   = Node_Id.Parse("6f2f5c1e-0000-4000-8000-0000000000a1");
        public static readonly Node_Id         LocalNodeB   = Node_Id.Parse("6f2f5c1e-0000-4000-8000-0000000000b2");
        public static readonly Node_Id         RemoteNode1  = Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000011");
        public static readonly Node_Id         RemoteNode2  = Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000022");

        public static readonly DateTimeOffset  PairedAt     = new (2026, 9, 4, 10,  0, 0, TimeSpan.Zero);

        // Creation times of pending access tokens (session initiations after the pairing) and the time of an unpairing.
        public static readonly DateTimeOffset  CreatedAt1   = new (2026, 9, 4, 10,  1, 0, TimeSpan.Zero);
        public static readonly DateTimeOffset  CreatedAt2   = new (2026, 9, 4, 10,  2, 0, TimeSpan.Zero);
        public static readonly DateTimeOffset  CreatedAt3   = new (2026, 9, 4, 10,  3, 0, TimeSpan.Zero);
        public static readonly DateTimeOffset  UnpairedAt   = new (2026, 9, 4, 10, 30, 0, TimeSpan.Zero);

        /// <summary>
        /// Create a communication server pairing of the given nodes.
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="AccessToken">An optional access token (default: a new random token).</param>
        public static Pairing CreatePairing(Node_Id       LocalNodeId,
                                            Node_Id       RemoteNodeId,
                                            AccessToken?  AccessToken   = null)

            => new (LocalNodeId,
                    new NodeDescription(RemoteNodeId, "ACME", "EV charger", "WallBox-b100", EnergyManagementRole.RM),
                    new EndpointDescription("Remote endpoint", null, Deployment.LAN),
                    CommunicationRole.CommunicationServer,
                    AccessToken ?? TokenGenerator.NewAccessToken(),
                    PairedAt);

        /// <summary>
        /// Create a pending access token of the given pair as a communication server remembers it
        /// during session initiation (WebSocket, S2 JSON v1.0.0 selected).
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="CreatedAt">When the token was generated.</param>
        /// <param name="Token">An optional access token (default: a new random token).</param>
        public static PendingAccessToken CreatePendingToken(Node_Id         LocalNodeId,
                                                            Node_Id         RemoteNodeId,
                                                            DateTimeOffset  CreatedAt,
                                                            AccessToken?    Token   = null)

            => new (LocalNodeId,
                    RemoteNodeId,
                    Token ?? TokenGenerator.NewAccessToken(),
                    CreatedAt,
                    CommunicationProtocol.WebSocket,
                    Version.S2JSONVersion);

    }


    /// <summary>
    /// The contract every <see cref="IS2Store"/> implementation must fulfil (PLAN.md D8, §3.5):
    /// derive a fixture per implementation and provide a fresh store via <see cref="CreateStore"/>.
    /// </summary>
    /// <typeparam name="TStore">The store implementation under test.</typeparam>
    public abstract class S2StoreContractTests<TStore>

        where TStore : IS2Store

    {

        #region (abstract) CreateStore()

        /// <summary>
        /// Create a fresh, empty store.
        /// </summary>
        protected abstract TStore CreateStore();

        #endregion


        #region Empty store

        [Test]
        public async Task EmptyStore_ReturnsNoPairings()
        {

            var store = CreateStore();

            Assert.That(await store.GetPairingsAsync(),                                Is.Empty);
            Assert.That(await store.GetPairingsAsync(LocalNodeA),                      Is.Empty);
            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode1),          Is.Null);
            Assert.That(await store.RemovePairingAsync(LocalNodeA, RemoteNode1),       Is.Null);

        }

        #endregion

        #region AddOrReplacePairingAsync(...)

        [Test]
        public async Task AddOrReplacePairing_ReturnsNullTheFirstTime_AndTheReplacedPairingAfterwards()
        {

            var store    = CreateStore();
            var first    = CreatePairing(LocalNodeA, RemoteNode1);
            var second   = CreatePairing(LocalNodeA, RemoteNode1);

            Assert.That(await store.AddOrReplacePairingAsync(first),  Is.Null, "no earlier pairing");
            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode1), Is.EqualTo(first));

            Assert.That(await store.AddOrReplacePairingAsync(second), Is.EqualTo(first), "the replaced pairing is returned");

            var stored   = await store.GetPairingAsync(LocalNodeA, RemoteNode1);
            var all      = await store.GetPairingsAsync();

            Assert.Multiple(() => {
                Assert.That(stored,                     Is.EqualTo(second), "the store holds the new pairing");
                Assert.That(stored!.AccessToken,        Is.EqualTo(second.AccessToken));
                Assert.That(stored!.AccessToken,        Is.Not.EqualTo(first.AccessToken), "the earlier access token is gone");
                Assert.That(all,                        Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region GetPairingsAsync(...)

        [Test]
        public async Task GetPairings_FiltersByLocalNode()
        {

            var store  = CreateStore();
            var a1     = CreatePairing(LocalNodeA, RemoteNode1);
            var a2     = CreatePairing(LocalNodeA, RemoteNode2);
            var b1     = CreatePairing(LocalNodeB, RemoteNode1);

            await store.AddOrReplacePairingAsync(a1);
            await store.AddOrReplacePairingAsync(a2);
            await store.AddOrReplacePairingAsync(b1);

            var all    = await store.GetPairingsAsync();
            var ofA    = await store.GetPairingsAsync(LocalNodeA);
            var ofB    = await store.GetPairingsAsync(LocalNodeB);
            var ofNone = await store.GetPairingsAsync(RemoteNode1);

            Assert.Multiple(() => {
                Assert.That(all,    Is.EquivalentTo(new[] { a1, a2, b1 }));
                Assert.That(ofA,    Is.EquivalentTo(new[] { a1, a2 }));
                Assert.That(ofB,    Is.EquivalentTo(new[] { b1 }));
                Assert.That(ofNone, Is.Empty, "a remote node identification is not a local node");
            });

        }

        #endregion

        #region RemovePairingAsync(...)

        [Test]
        public async Task RemovePairing_ReturnsTheRemovedPairing_AndNullAfterwards()
        {

            var store  = CreateStore();
            var a1     = CreatePairing(LocalNodeA, RemoteNode1);
            var a2     = CreatePairing(LocalNodeA, RemoteNode2);

            await store.AddOrReplacePairingAsync(a1);
            await store.AddOrReplacePairingAsync(a2);

            Assert.That(await store.RemovePairingAsync(LocalNodeA, RemoteNode1), Is.EqualTo(a1));
            Assert.That(await store.RemovePairingAsync(LocalNodeA, RemoteNode1), Is.Null, "already removed");
            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode1),    Is.Null);
            Assert.That(await store.GetPairingsAsync(LocalNodeA),                Is.EqualTo(new[] { a2 }), "other pairings are untouched");

        }

        #endregion

        #region Independence of local nodes

        [Test]
        public async Task DifferentLocalNodes_WithTheSameRemoteNode_AreIndependent()
        {

            var store  = CreateStore();
            var a1     = CreatePairing(LocalNodeA, RemoteNode1);
            var b1     = CreatePairing(LocalNodeB, RemoteNode1);

            Assert.That(await store.AddOrReplacePairingAsync(a1), Is.Null);
            Assert.That(await store.AddOrReplacePairingAsync(b1), Is.Null, "B's pairing does not replace A's pairing with the same remote node");

            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode1), Is.EqualTo(a1));
            Assert.That(await store.GetPairingAsync(LocalNodeB, RemoteNode1), Is.EqualTo(b1));

            var b1New = CreatePairing(LocalNodeB, RemoteNode1);

            Assert.That(await store.AddOrReplacePairingAsync(b1New),          Is.EqualTo(b1));
            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode1), Is.EqualTo(a1), "replacing B's pairing leaves A's pairing alone");

            Assert.That(await store.RemovePairingAsync(LocalNodeA, RemoteNode1), Is.EqualTo(a1));
            Assert.That(await store.GetPairingAsync(LocalNodeB, RemoteNode1),    Is.EqualTo(b1New), "removing A's pairing leaves B's pairing alone");
            Assert.That(await store.GetPairingsAsync(),                          Has.Count.EqualTo(1));

        }

        #endregion

        #region Cancellation

        [Test]
        public async Task CancelledToken_ThrowsOperationCanceledException()
        {

            var store     = CreateStore();
            var pairing   = CreatePairing(LocalNodeA, RemoteNode1);

            await store.AddOrReplacePairingAsync(pairing);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            Assert.CatchAsync<OperationCanceledException>(async () => await store.GetPairingsAsync        (null,       cts.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await store.GetPairingsAsync        (LocalNodeA, cts.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await store.GetPairingAsync         (LocalNodeA, RemoteNode1, cts.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode2), cts.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await store.RemovePairingAsync      (LocalNodeA, RemoteNode1, cts.Token));

            Assert.That(await store.GetPairingsAsync(), Is.EqualTo(new[] { pairing }), "a cancelled operation changes nothing");

        }

        #endregion

        #region Concurrency

        [Test]
        public async Task ConcurrentAdds_AllEndUpInTheStore()
        {

            var store     = CreateStore();
            var pairings  = Enumerable.Range(0, 100).
                                       Select(_ => CreatePairing(LocalNodeA, Node_Id.NewRandom)).
                                       ToList();

            var replaced  = await Task.WhenAll(pairings.Select(pairing => Task.Run(() => store.AddOrReplacePairingAsync(pairing).AsTask())));

            var stored    = await store.GetPairingsAsync(LocalNodeA);

            Assert.Multiple(() => {
                Assert.That(replaced, Has.All.Null, "every pair was new");
                Assert.That(stored,   Has.Count.EqualTo(pairings.Count));
                Assert.That(stored,   Is.EquivalentTo(pairings));
            });

            foreach (var pairing in pairings)
                Assert.That(await store.GetPairingAsync(LocalNodeA, pairing.RemoteNodeId), Is.EqualTo(pairing));

        }

        #endregion


        // Pending access tokens, token activation and tombstones (session initiation and unpairing)

        #region (private static) AddPendingTokensAsync(Store, Tokens)

        private static async Task AddPendingTokensAsync(TStore                       Store,
                                                        params PendingAccessToken[]  Tokens)
        {
            foreach (var token in Tokens)
                await Store.AddPendingAccessTokenAsync(token);
        }

        #endregion

        #region Empty store (pending tokens and tombstones)

        [Test]
        public async Task EmptyStore_HasNoPendingTokensAndNoTombstones()
        {

            var store = CreateStore();

            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),                                 Is.Empty);
            Assert.That(await store.FindPendingAccessTokenAsync(TokenGenerator.NewAccessToken()),                          Is.Null);
            Assert.That(await store.GetAccessTokenCandidatesAsync(LocalNodeA, RemoteNode1),                               Is.Empty);
            Assert.That(await store.ActivateAccessTokenAsync(LocalNodeA, RemoteNode1, TokenGenerator.NewAccessToken()),   Is.Null);
            Assert.That(await store.RemovePendingAccessTokensAsync(),                                                     Is.EqualTo(0));
            Assert.That(await store.UnpairAsync(LocalNodeA, RemoteNode1, UnpairedAt),                                     Is.Null);
            Assert.That(await store.GetUnpairedAtAsync(LocalNodeA, RemoteNode1),                                          Is.Null);

        }

        #endregion

        #region AddPendingAccessTokenAsync(...) / GetPendingAccessTokensAsync(...)

        [Test]
        [S2C("SessionInitiation.2")]
        public async Task AddPendingAccessToken_ListsThePendingTokensOfAPair_NewestFirst()
        {

            var store  = CreateStore();
            var a1t1   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var a1t2   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt2);
            var a1t3   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt3);
            var a2t1   = CreatePendingToken(LocalNodeA, RemoteNode2, CreatedAt1);
            var b1t1   = CreatePendingToken(LocalNodeB, RemoteNode1, CreatedAt1);

            // Added out of order: the store orders by creation time.
            await AddPendingTokensAsync(store, a1t2, a1t1, a2t1, a1t3, b1t1);

            var ofA1   = await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1);
            var ofA2   = await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode2);
            var ofB1   = await store.GetPendingAccessTokensAsync(LocalNodeB, RemoteNode1);
            var ofB2   = await store.GetPendingAccessTokensAsync(LocalNodeB, RemoteNode2);

            Assert.Multiple(() => {
                Assert.That(ofA1,  Is.EqualTo(new[] { a1t3, a1t2, a1t1 }), "newest first");
                Assert.That(ofA2,  Is.EqualTo(new[] { a2t1 }));
                Assert.That(ofB1,  Is.EqualTo(new[] { b1t1 }), "the same remote node at another local node is another pair");
                Assert.That(ofB2,  Is.Empty);
            });

        }

        [Test]
        public async Task AddPendingAccessToken_StoresTheSameTokenOnce()
        {

            var store    = CreateStore();
            var pending  = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var copy     = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1, pending.Token);

            await store.AddPendingAccessTokenAsync(pending);
            await store.AddPendingAccessTokenAsync(pending);
            await store.AddPendingAccessTokenAsync(copy);

            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),  Is.EqualTo(new[] { pending }));
            Assert.That(await store.FindPendingAccessTokenAsync(pending.Token),             Is.EqualTo(pending));

        }

        [Test]
        public async Task AddOrReplacePairing_KeepsThePendingTokensOfThePair()
        {

            var store    = CreateStore();
            var pending  = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));
            await store.AddPendingAccessTokenAsync(pending);
            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));

            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1), Is.EqualTo(new[] { pending }), "a description update during session initiation keeps the pending token");

        }

        #endregion

        #region FindPendingAccessTokenAsync(...)

        [Test]
        [S2C("SessionInitiation.5")]
        public async Task FindPendingAccessToken_FindsTheEntryByToken_AcrossAllPairs()
        {

            var store  = CreateStore();
            var a1     = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var b2     = CreatePendingToken(LocalNodeB, RemoteNode2, CreatedAt2);

            await AddPendingTokensAsync(store, a1, b2);

            Assert.That(await store.FindPendingAccessTokenAsync(a1.Token),                       Is.EqualTo(a1));
            Assert.That(await store.FindPendingAccessTokenAsync(b2.Token),                       Is.EqualTo(b2));
            Assert.That(await store.FindPendingAccessTokenAsync(TokenGenerator.NewAccessToken()), Is.Null);

            // The active token of a pairing is not a pending token.
            var pairing = CreatePairing(LocalNodeA, RemoteNode1);

            await store.AddOrReplacePairingAsync(pairing);

            Assert.That(await store.FindPendingAccessTokenAsync(pairing.AccessToken), Is.Null);

        }

        #endregion

        #region ActivateAccessTokenAsync(...)

        [Test]
        [S2C("SessionInitiation.6")]
        [S2C("SessionInitiation.8")]
        public async Task ActivateAccessToken_MakesThePendingTokenActive_AndRemovesEveryPendingTokenOfThePair()
        {

            var store    = CreateStore();
            var pairing  = CreatePairing(LocalNodeA, RemoteNode1);
            var other    = CreatePairing(LocalNodeA, RemoteNode2);
            var a1t1     = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var a1t2     = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt2);
            var a2t1     = CreatePendingToken(LocalNodeA, RemoteNode2, CreatedAt1);
            var b1t1     = CreatePendingToken(LocalNodeB, RemoteNode1, CreatedAt1);

            await store.AddOrReplacePairingAsync(pairing);
            await store.AddOrReplacePairingAsync(other);
            await AddPendingTokensAsync(store, a1t1, a1t2, a2t1, b1t1);

            var activated  = await store.ActivateAccessTokenAsync(LocalNodeA, RemoteNode1, a1t1.Token);
            var stored     = await store.GetPairingAsync(LocalNodeA, RemoteNode1);

            Assert.Multiple(() => {
                Assert.That(activated,               Is.EqualTo(pairing.WithAccessToken(a1t1.Token)), "the pairing with the new token is returned");
                Assert.That(stored,                  Is.EqualTo(activated), "and stored");
                Assert.That(stored!.AccessToken,     Is.EqualTo(a1t1.Token));
                Assert.That(stored!.AccessToken,     Is.Not.EqualTo(pairing.AccessToken), "the old active token is gone");
            });

            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),    Is.Empty, "every pending token of the pair is removed, the unconfirmed newer one as well");
            Assert.That(await store.FindPendingAccessTokenAsync(a1t1.Token),                 Is.Null);
            Assert.That(await store.FindPendingAccessTokenAsync(a1t2.Token),                 Is.Null);
            Assert.That(await store.GetAccessTokenCandidatesAsync(LocalNodeA, RemoteNode1),  Is.EqualTo(new[] { a1t1.Token }));
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode2),    Is.EqualTo(new[] { a2t1 }), "other pairs are untouched");
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeB, RemoteNode1),    Is.EqualTo(new[] { b1t1 }));
            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode2),                Is.EqualTo(other));

        }

        [Test]
        [S2C("SessionInitiation.6")]
        public async Task ActivateAccessToken_ReturnsNull_ForAnUnknownPair()
        {

            var store    = CreateStore();
            var pending  = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);

            // A pending token without a pairing (the pairing was removed meanwhile).
            await store.AddPendingAccessTokenAsync(pending);

            Assert.That(await store.ActivateAccessTokenAsync(LocalNodeA, RemoteNode1, pending.Token),  Is.Null, "not paired");
            Assert.That(await store.ActivateAccessTokenAsync(LocalNodeB, RemoteNode2, pending.Token),  Is.Null, "unknown pair");

            Assert.That(await store.GetPairingsAsync(),                                    Is.Empty, "nothing was paired on the way");
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),  Is.EqualTo(new[] { pending }), "a failed activation changes nothing");

        }

        [Test]
        [S2C("SessionInitiation.6")]
        public async Task ActivateAccessToken_ReturnsNull_ForATokenThatIsNeitherPendingNorActive()
        {

            var store    = CreateStore();
            var pairing  = CreatePairing(LocalNodeA, RemoteNode1);
            var a1       = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var a2       = CreatePendingToken(LocalNodeA, RemoteNode2, CreatedAt1);

            await store.AddOrReplacePairingAsync(pairing);
            await AddPendingTokensAsync(store, a1, a2);

            Assert.That(await store.ActivateAccessTokenAsync(LocalNodeA, RemoteNode1, TokenGenerator.NewAccessToken()),  Is.Null, "an unknown token");
            Assert.That(await store.ActivateAccessTokenAsync(LocalNodeA, RemoteNode1, a2.Token),                          Is.Null, "a pending token of another pair");

            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode1),               Is.EqualTo(pairing), "a failed activation changes nothing");
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),   Is.EqualTo(new[] { a1 }));
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode2),   Is.EqualTo(new[] { a2 }));

        }

        [Test]
        [S2C("SessionInitiation.6")]
        public async Task ActivateAccessToken_WithTheActiveToken_OnlyRemovesThePendingTokens()
        {

            var store    = CreateStore();
            var pairing  = CreatePairing(LocalNodeA, RemoteNode1);
            var a1t1     = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var a1t2     = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt2);

            await store.AddOrReplacePairingAsync(pairing);
            await AddPendingTokensAsync(store, a1t1, a1t2);

            var activated = await store.ActivateAccessTokenAsync(LocalNodeA, RemoteNode1, pairing.AccessToken);

            Assert.That(activated,                                                             Is.EqualTo(pairing), "the pairing is unchanged");
            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode1),                  Is.EqualTo(pairing));
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),      Is.Empty);
            Assert.That(await store.GetAccessTokenCandidatesAsync(LocalNodeA, RemoteNode1),    Is.EqualTo(new[] { pairing.AccessToken }));

        }

        #endregion

        #region RemovePendingAccessTokensAsync(...)

        [Test]
        public async Task RemovePendingAccessTokens_ByPair_RemovesOnlyThePendingTokensOfThatPair()
        {

            var store  = CreateStore();
            var a1t1   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var a1t2   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt2);
            var a2t1   = CreatePendingToken(LocalNodeA, RemoteNode2, CreatedAt1);
            var b1t1   = CreatePendingToken(LocalNodeB, RemoteNode1, CreatedAt1);

            await AddPendingTokensAsync(store, a1t1, a1t2, a2t1, b1t1);

            Assert.That(await store.RemovePendingAccessTokensAsync(LocalNodeA, RemoteNode1),  Is.EqualTo(2));

            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),     Is.Empty);
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode2),     Is.EqualTo(new[] { a2t1 }));
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeB, RemoteNode1),     Is.EqualTo(new[] { b1t1 }));

            Assert.That(await store.RemovePendingAccessTokensAsync(LocalNodeA, RemoteNode1),  Is.EqualTo(0), "already removed");

        }

        [Test]
        public async Task RemovePendingAccessTokens_ByLocalNode_RemovesThePendingTokensOfAllItsPairs()
        {

            var store  = CreateStore();
            var a1t1   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var a1t2   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt2);
            var a2t1   = CreatePendingToken(LocalNodeA, RemoteNode2, CreatedAt1);
            var b1t1   = CreatePendingToken(LocalNodeB, RemoteNode1, CreatedAt1);

            await AddPendingTokensAsync(store, a1t1, a1t2, a2t1, b1t1);

            Assert.That(await store.RemovePendingAccessTokensAsync(LocalNodeA),               Is.EqualTo(3));

            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),     Is.Empty);
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode2),     Is.Empty);
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeB, RemoteNode1),     Is.EqualTo(new[] { b1t1 }), "another local node is untouched");

        }

        [Test]
        [S2C("SessionInitiation.6.Expired")]
        public async Task RemovePendingAccessTokens_ByCreationTime_RemovesOnlyOlderTokens()
        {

            var store  = CreateStore();
            var a1t1   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var a1t2   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt2);
            var a1t3   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt3);
            var b1t1   = CreatePendingToken(LocalNodeB, RemoteNode1, CreatedAt1);

            await AddPendingTokensAsync(store, a1t1, a1t2, a1t3, b1t1);

            // Strictly before: a token created exactly at the limit stays.
            Assert.That(await store.RemovePendingAccessTokensAsync(CreatedBefore: CreatedAt2),  Is.EqualTo(2), "the tokens of both pairs created before 10:02");

            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),       Is.EqualTo(new[] { a1t3, a1t2 }));
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeB, RemoteNode1),       Is.Empty);

            Assert.That(await store.RemovePendingAccessTokensAsync(CreatedBefore: CreatedAt3.AddTicks(1)),  Is.EqualTo(2));
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),                  Is.Empty);

        }

        [Test]
        [S2C("SessionInitiation.6.Expired")]
        public async Task RemovePendingAccessTokens_ByPairAndCreationTime_CombinesBothCriteria()
        {

            var store  = CreateStore();
            var a1t1   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var a1t3   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt3);
            var a2t1   = CreatePendingToken(LocalNodeA, RemoteNode2, CreatedAt1);

            await AddPendingTokensAsync(store, a1t1, a1t3, a2t1);

            Assert.That(await store.RemovePendingAccessTokensAsync(LocalNodeA, RemoteNode1, CreatedAt2),  Is.EqualTo(1), "only the old token of the pair");

            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),                 Is.EqualTo(new[] { a1t3 }));
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode2),                 Is.EqualTo(new[] { a2t1 }), "the equally old token of another pair stays");

        }

        [Test]
        public async Task RemovePendingAccessTokens_WithoutCriteria_RemovesEveryPendingToken_ButNoPairing()
        {

            var store    = CreateStore();
            var pairing  = CreatePairing(LocalNodeA, RemoteNode1);
            var a1t1     = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var a2t1     = CreatePendingToken(LocalNodeA, RemoteNode2, CreatedAt1);
            var b1t1     = CreatePendingToken(LocalNodeB, RemoteNode1, CreatedAt1);

            await store.AddOrReplacePairingAsync(pairing);
            await AddPendingTokensAsync(store, a1t1, a2t1, b1t1);

            Assert.That(await store.RemovePendingAccessTokensAsync(),                          Is.EqualTo(3));

            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),      Is.Empty);
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode2),      Is.Empty);
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeB, RemoteNode1),      Is.Empty);
            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode1),                  Is.EqualTo(pairing), "the pairing stays");
            Assert.That(await store.GetAccessTokenCandidatesAsync(LocalNodeA, RemoteNode1),    Is.EqualTo(new[] { pairing.AccessToken }));

            Assert.That(await store.RemovePendingAccessTokensAsync(),                          Is.EqualTo(0), "nothing left");

        }

        #endregion

        #region GetAccessTokenCandidatesAsync(...)

        [Test]
        [S2C("SessionInitiation.1")]
        public async Task GetAccessTokenCandidates_ListsTheActiveTokenFirst_ThenThePendingTokensNewestFirst()
        {

            var store    = CreateStore();
            var pairing  = CreatePairing(LocalNodeA, RemoteNode1);
            var a1t1     = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var a1t2     = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt2);
            var a1t3     = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt3);
            var a2t1     = CreatePendingToken(LocalNodeA, RemoteNode2, CreatedAt1);

            await store.AddOrReplacePairingAsync(pairing);
            await AddPendingTokensAsync(store, a1t1, a1t3, a1t2, a2t1);

            Assert.That(await store.GetAccessTokenCandidatesAsync(LocalNodeA, RemoteNode1),
                        Is.EqualTo(new[] { pairing.AccessToken, a1t3.Token, a1t2.Token, a1t1.Token }));

        }

        [Test]
        [S2C("SessionInitiation.1")]
        public async Task GetAccessTokenCandidates_ContainsNoDuplicates()
        {

            var store      = CreateStore();
            var pairing    = CreatePairing(LocalNodeA, RemoteNode1);
            var a1t1       = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var duplicate  = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt2, pairing.AccessToken);
            var a1t3       = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt3);

            await store.AddOrReplacePairingAsync(pairing);
            await AddPendingTokensAsync(store, a1t1, duplicate, a1t3);

            Assert.That(await store.GetAccessTokenCandidatesAsync(LocalNodeA, RemoteNode1),
                        Is.EqualTo(new[] { pairing.AccessToken, a1t3.Token, a1t1.Token }),
                        "the active token that is pending as well is listed once");

        }

        [Test]
        [S2C("SessionInitiation.1")]
        public async Task GetAccessTokenCandidates_IsEmpty_WhenNotPaired()
        {

            var store = CreateStore();

            Assert.That(await store.GetAccessTokenCandidatesAsync(LocalNodeA, RemoteNode1), Is.Empty, "never paired");

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));
            await AddPendingTokensAsync(store, CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1));
            await store.UnpairAsync(LocalNodeA, RemoteNode1, UnpairedAt);

            Assert.That(await store.GetAccessTokenCandidatesAsync(LocalNodeA, RemoteNode1), Is.Empty, "unpaired");

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));
            await AddPendingTokensAsync(store, CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt2));
            await store.RemovePairingAsync(LocalNodeA, RemoteNode1);

            Assert.That(await store.GetAccessTokenCandidatesAsync(LocalNodeA, RemoteNode1), Is.Empty, "pairing removed");

        }

        #endregion

        #region UnpairAsync(...) / GetUnpairedAtAsync(...)

        [Test]
        [S2C("Unpairing.Tombstone")]
        public async Task Unpair_RemovesThePairingAndItsPendingTokens_AndWritesATombstone()
        {

            var store  = CreateStore();
            var a1     = CreatePairing(LocalNodeA, RemoteNode1);
            var a2     = CreatePairing(LocalNodeA, RemoteNode2);
            var b1     = CreatePairing(LocalNodeB, RemoteNode1);
            var a1t1   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var a1t2   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt2);
            var a2t1   = CreatePendingToken(LocalNodeA, RemoteNode2, CreatedAt1);

            await store.AddOrReplacePairingAsync(a1);
            await store.AddOrReplacePairingAsync(a2);
            await store.AddOrReplacePairingAsync(b1);
            await AddPendingTokensAsync(store, a1t1, a1t2, a2t1);

            var removed = await store.UnpairAsync(LocalNodeA, RemoteNode1, UnpairedAt);

            Assert.That(removed, Is.EqualTo(a1), "the removed pairing is returned");

            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode1),                Is.Null, "the security material is gone");
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),    Is.Empty, "the pending tokens of the pair as well");
            Assert.That(await store.FindPendingAccessTokenAsync(a1t1.Token),                 Is.Null);
            Assert.That(await store.GetAccessTokenCandidatesAsync(LocalNodeA, RemoteNode1),  Is.Empty);
            Assert.That(await store.GetUnpairedAtAsync(LocalNodeA, RemoteNode1),             Is.EqualTo(UnpairedAt), "the tombstone remembers the time of the unpairing");

            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode2),                Is.EqualTo(a2), "other pairs are untouched");
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode2),    Is.EqualTo(new[] { a2t1 }));
            Assert.That(await store.GetUnpairedAtAsync(LocalNodeA, RemoteNode2),             Is.Null);
            Assert.That(await store.GetPairingAsync(LocalNodeB, RemoteNode1),                Is.EqualTo(b1));
            Assert.That(await store.GetUnpairedAtAsync(LocalNodeB, RemoteNode1),             Is.Null);
            Assert.That(await store.GetPairingsAsync(),                                      Has.Count.EqualTo(2));

        }

        [Test]
        [S2C("Unpairing.Tombstone")]
        public async Task Unpair_ReturnsNull_AndWritesNoTombstone_ForAnUnknownPair()
        {

            var store  = CreateStore();
            var b1     = CreatePairing(LocalNodeB, RemoteNode1);

            Assert.That(await store.UnpairAsync(LocalNodeA, RemoteNode1, UnpairedAt),  Is.Null, "empty store");
            Assert.That(await store.GetUnpairedAtAsync(LocalNodeA, RemoteNode1),       Is.Null, "no tombstone for a pair that was never paired");

            await store.AddOrReplacePairingAsync(b1);

            Assert.That(await store.UnpairAsync(LocalNodeA, RemoteNode1, UnpairedAt),  Is.Null, "another pair is paired");
            Assert.That(await store.GetUnpairedAtAsync(LocalNodeA, RemoteNode1),       Is.Null);
            Assert.That(await store.GetPairingAsync(LocalNodeB, RemoteNode1),          Is.EqualTo(b1), "the other pair is untouched");

        }

        [Test]
        [S2C("Unpairing.Tombstone")]
        public async Task Unpair_Twice_ReturnsNullTheSecondTime_AndKeepsTheFirstTombstone()
        {

            var store    = CreateStore();
            var pairing  = CreatePairing(LocalNodeA, RemoteNode1);

            await store.AddOrReplacePairingAsync(pairing);

            Assert.That(await store.UnpairAsync(LocalNodeA, RemoteNode1, UnpairedAt),                 Is.EqualTo(pairing));
            Assert.That(await store.UnpairAsync(LocalNodeA, RemoteNode1, UnpairedAt.AddMinutes(5)),   Is.Null, "already unpaired");
            Assert.That(await store.GetUnpairedAtAsync(LocalNodeA, RemoteNode1),                      Is.EqualTo(UnpairedAt), "the time of the actual unpairing");

        }

        [Test]
        [S2C("Unpairing.Tombstone")]
        public async Task AddOrReplacePairing_ClearsTheTombstone()
        {

            var store  = CreateStore();
            var a1     = CreatePairing(LocalNodeA, RemoteNode1);
            var a2     = CreatePairing(LocalNodeA, RemoteNode2);

            await store.AddOrReplacePairingAsync(a1);
            await store.UnpairAsync(LocalNodeA, RemoteNode1, UnpairedAt);

            Assert.That(await store.GetUnpairedAtAsync(LocalNodeA, RemoteNode1), Is.EqualTo(UnpairedAt));

            // A pairing of another pair does not clear the tombstone.
            await store.AddOrReplacePairingAsync(a2);

            Assert.That(await store.GetUnpairedAtAsync(LocalNodeA, RemoteNode1), Is.EqualTo(UnpairedAt));

            // A new pairing of the pair does.
            var again = CreatePairing(LocalNodeA, RemoteNode1);

            Assert.That(await store.AddOrReplacePairingAsync(again),              Is.Null, "the pair was not paired at that moment");
            Assert.That(await store.GetUnpairedAtAsync(LocalNodeA, RemoteNode1),  Is.Null, "the tombstone is cleared");
            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode1),     Is.EqualTo(again));

        }

        [Test]
        public async Task RemovePairing_RemovesThePendingTokens_ButWritesNoTombstone()
        {

            var store  = CreateStore();
            var a1     = CreatePairing(LocalNodeA, RemoteNode1);
            var a1t1   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var a1t2   = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt2);
            var a2t1   = CreatePendingToken(LocalNodeA, RemoteNode2, CreatedAt1);

            await store.AddOrReplacePairingAsync(a1);
            await AddPendingTokensAsync(store, a1t1, a1t2, a2t1);

            Assert.That(await store.RemovePairingAsync(LocalNodeA, RemoteNode1),                 Is.EqualTo(a1));

            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),        Is.Empty, "the pending tokens of the pair are gone");
            Assert.That(await store.FindPendingAccessTokenAsync(a1t1.Token),                     Is.Null);
            Assert.That(await store.GetAccessTokenCandidatesAsync(LocalNodeA, RemoteNode1),      Is.Empty);
            Assert.That(await store.GetUnpairedAtAsync(LocalNodeA, RemoteNode1),                 Is.Null, "no tombstone: a later initiateSession is answered with 401, not NoLongerPaired");
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode2),        Is.EqualTo(new[] { a2t1 }), "other pairs are untouched");

        }

        #endregion

        #region Cancellation (pending tokens and unpairing)

        [Test]
        public async Task CancelledToken_ThrowsOperationCanceledException_ForPendingTokensAndUnpairing()
        {

            var store    = CreateStore();
            var pairing  = CreatePairing(LocalNodeA, RemoteNode1);
            var pending  = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);

            await store.AddOrReplacePairingAsync(pairing);
            await store.AddPendingAccessTokenAsync(pending);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            Assert.CatchAsync<OperationCanceledException>(async () => await store.AddPendingAccessTokenAsync    (CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt2), cts.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await store.GetPendingAccessTokensAsync   (LocalNodeA, RemoteNode1, cts.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await store.FindPendingAccessTokenAsync   (pending.Token, cts.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await store.ActivateAccessTokenAsync      (LocalNodeA, RemoteNode1, pending.Token, cts.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await store.RemovePendingAccessTokensAsync(LocalNodeA, RemoteNode1, null, cts.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await store.GetAccessTokenCandidatesAsync (LocalNodeA, RemoteNode1, cts.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await store.UnpairAsync                   (LocalNodeA, RemoteNode1, UnpairedAt, cts.Token));
            Assert.CatchAsync<OperationCanceledException>(async () => await store.GetUnpairedAtAsync            (LocalNodeA, RemoteNode1, cts.Token));

            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode1),              Is.EqualTo(pairing), "a cancelled operation changes nothing");
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),  Is.EqualTo(new[] { pending }));
            Assert.That(await store.GetUnpairedAtAsync(LocalNodeA, RemoteNode1),           Is.Null);

        }

        #endregion

        #region Concurrency (pending tokens)

        [Test]
        public async Task ConcurrentPendingTokenAdds_AllEndUpInTheStore_AndOneActivationRemovesThemAll()
        {

            var store     = CreateStore();
            var pairing   = CreatePairing(LocalNodeA, RemoteNode1);
            var pendings  = Enumerable.Range(0, 100).
                                       Select(i => CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1.AddSeconds(i))).
                                       ToList();

            await store.AddOrReplacePairingAsync(pairing);
            await Task.WhenAll(pendings.Select(pending => Task.Run(() => store.AddPendingAccessTokenAsync(pending).AsTask())));

            var stored = await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1);

            Assert.Multiple(() => {
                Assert.That(stored,                                     Has.Count.EqualTo(pendings.Count));
                Assert.That(stored,                                     Is.EquivalentTo(pendings));
                Assert.That(stored.Select(pending => pending.CreatedAt), Is.Ordered.Descending);
            });

            var activated = await store.ActivateAccessTokenAsync(LocalNodeA, RemoteNode1, pendings[42].Token);

            Assert.That(activated!.AccessToken,                                             Is.EqualTo(pendings[42].Token));
            Assert.That(await store.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1),   Is.Empty, "one activation removes every pending token of the pair");
            Assert.That(await store.GetAccessTokenCandidatesAsync(LocalNodeA, RemoteNode1), Is.EqualTo(new[] { pendings[42].Token }));

        }

        #endregion

    }

}

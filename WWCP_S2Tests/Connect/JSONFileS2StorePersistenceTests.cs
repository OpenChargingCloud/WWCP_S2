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

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.S2.Connect;

using static cloud.charging.open.protocols.S2.Tests.Connect.S2StoreTestData;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The file-specific behaviour of the <see cref="JSONFileS2Store"/> (PLAN.md D8) that the
    /// shared store contract does not cover: durability across a dispose/re-open, the on-disk
    /// JSON shape (format version, secret scheme, the three arrays), immediate and atomic
    /// persistence, secret protection via an <see cref="ISecretProtector"/>, format-version and
    /// scheme guarding, serialised concurrent writes, and the round-tripping of the pending
    /// token fields.
    /// </summary>
    [TestFixture]
    public sealed class JSONFileS2StorePersistenceTests
    {

        #region Data

        private readonly List<JSONFileS2Store>  stores   = [];
        private readonly List<String>           paths    = [];

        #endregion

        #region Helpers

        private String NewPath()
        {
            var path = Path.Combine(Path.GetTempPath(), "wwcp-s2-store-tests", Guid.NewGuid().ToString("N") + ".json");
            paths.Add(path);
            return path;
        }

        private async Task<JSONFileS2Store> OpenStore(String             FilePath,
                                                      ISecretProtector?  SecretProtector   = null)
        {
            var store = await JSONFileS2Store.OpenAsync(FilePath, SecretProtector);
            stores.Add(store);
            return store;
        }

        #endregion

        #region TearDown()

        [TearDown]
        public void TearDown()
        {

            foreach (var store in stores)
                store.Dispose();

            foreach (var path in paths)
            {

                if (File.Exists(path))
                    File.Delete(path);

                if (File.Exists(path + ".tmp"))
                    File.Delete(path + ".tmp");

            }

            stores.Clear();
            paths. Clear();

        }

        #endregion


        #region RoundTrip_PairingPendingTokenAndTombstone_SurviveReopen()

        [Test]
        public async Task RoundTrip_PairingPendingTokenAndTombstone_SurviveReopen()
        {

            var path       = NewPath();
            var r1Pairing  = CreatePairing(LocalNodeA, RemoteNode1);
            var pending    = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);
            var r2Pairing  = CreatePairing(LocalNodeA, RemoteNode2);

            var store = await OpenStore(path);
            await store.AddOrReplacePairingAsync(r1Pairing);
            await store.AddPendingAccessTokenAsync(pending);
            await store.AddOrReplacePairingAsync(r2Pairing);
            await store.UnpairAsync(LocalNodeA, RemoteNode2, UnpairedAt);   // tombstone for (A, R2); keeps (A, R1) and its pending token
            store.Dispose();

            var reopened = await OpenStore(path);

            Assert.Multiple(() => {
                Assert.That(reopened.Count,          Is.EqualTo(1));
                Assert.That(reopened.PendingCount,   Is.EqualTo(1));
                Assert.That(reopened.TombstoneCount, Is.EqualTo(1));
            });

            var storedPairing   = await reopened.GetPairingAsync(LocalNodeA, RemoteNode1);
            var foundPending    = await reopened.FindPendingAccessTokenAsync(pending.Token);
            var unpairedAt      = await reopened.GetUnpairedAtAsync(LocalNodeA, RemoteNode2);
            var removedPairing  = await reopened.GetPairingAsync(LocalNodeA, RemoteNode2);

            Assert.Multiple(() => {
                Assert.That(storedPairing,               Is.EqualTo(r1Pairing),             "the pairing survived with equal content");
                Assert.That(storedPairing!.AccessToken,  Is.EqualTo(r1Pairing.AccessToken), "including its exact access token");
                Assert.That(foundPending,                Is.EqualTo(pending),               "the pending token survived");
                Assert.That(unpairedAt,                  Is.EqualTo(UnpairedAt),            "the tombstone survived");
                Assert.That(removedPairing,              Is.Null,                           "the unpaired pairing is gone");
            });

        }

        #endregion

        #region Open_NonExistentPath_StartsEmpty()

        [Test]
        public async Task Open_NonExistentPath_StartsEmpty()
        {

            var path   = NewPath();
            var store  = await OpenStore(path);

            Assert.Multiple(() => {
                Assert.That(store.Count,          Is.EqualTo(0));
                Assert.That(store.PendingCount,   Is.EqualTo(0));
                Assert.That(store.TombstoneCount, Is.EqualTo(0));
                Assert.That(File.Exists(path),    Is.False, "opening an absent file does not create it");
            });

            Assert.That(await store.GetPairingsAsync(), Is.Empty);

        }

        #endregion

        #region File_HasFormatVersionSecretSchemeAndTheThreeArrays()

        [Test]
        public async Task File_HasFormatVersionSecretSchemeAndTheThreeArrays()
        {

            var path   = NewPath();
            var store  = await OpenStore(path);

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));

            var json           = JObject.Parse(await File.ReadAllTextAsync(path));
            var pairingsArray  = json["pairings"]      as JArray;
            var pendingsArray  = json["pendingTokens"] as JArray;
            var unpairedArray  = json["unpaired"]      as JArray;

            Assert.Multiple(() => {

                Assert.That(json["formatVersion"]?.Value<Int32>(),  Is.EqualTo(JSONFileS2Store.FormatVersion));
                Assert.That(json["formatVersion"]?.Value<Int32>(),  Is.EqualTo(1));

                Assert.That(json["secretScheme"],                   Is.Not.Null, "a secretScheme property is written");
                Assert.That(json["secretScheme"]?.Value<String>(),  Is.EqualTo(""), "the plaintext scheme id is the empty string");

                Assert.That(pairingsArray,                          Is.Not.Null, "a pairings array is written");
                Assert.That(pendingsArray,                          Is.Not.Null, "a pendingTokens array is written");
                Assert.That(unpairedArray,                          Is.Not.Null, "an unpaired array is written");

            });

            Assert.That(pairingsArray!.Count, Is.EqualTo(1));

        }

        #endregion

        #region Flush_WritesTheCurrentStateToDisk()

        [Test]
        public async Task Flush_WritesTheCurrentStateToDisk()
        {

            var path   = NewPath();
            var store  = await OpenStore(path);

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));
            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeB, RemoteNode2));
            await store.AddPendingAccessTokenAsync(CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1));

            await store.FlushAsync();

            var json = JObject.Parse(await File.ReadAllTextAsync(path));

            Assert.Multiple(() => {
                Assert.That((json["pairings"]      as JArray)?.Count, Is.EqualTo(2), "both pairings are on disk");
                Assert.That((json["pendingTokens"] as JArray)?.Count, Is.EqualTo(1), "the pending token is on disk");
            });

        }

        #endregion

        #region EveryMutation_PersistsImmediately_WithoutAnExplicitFlush()

        [Test]
        public async Task EveryMutation_PersistsImmediately_WithoutAnExplicitFlush()
        {

            var path     = NewPath();
            var pairing  = CreatePairing(LocalNodeA, RemoteNode1);
            var store    = await OpenStore(path);

            await store.AddOrReplacePairingAsync(pairing);
            // Deliberately no FlushAsync(): the mutation itself must already have written the file.

            Assert.That(File.Exists(path), Is.True, "the file exists right after the first mutation");

            var json           = JObject.Parse(await File.ReadAllTextAsync(path));
            var pairingsArray  = json["pairings"] as JArray;

            Assert.That(pairingsArray, Is.Not.Null);
            Assert.That(pairingsArray!.Count, Is.EqualTo(1));

            Assert.Multiple(() => {
                Assert.That(pairingsArray![0]["localNodeId"]?.Value<String>(),  Is.EqualTo(LocalNodeA.ToString()));
                Assert.That(pairingsArray![0]["accessToken"]?.Value<String>(),  Is.EqualTo(pairing.AccessToken.Value), "the pairing was persisted before any flush");
            });

        }

        #endregion

        #region AccessToken_IsStoredInPlaintext_ByDefault()

        [Test]
        public async Task AccessToken_IsStoredInPlaintext_ByDefault()
        {

            var path     = NewPath();
            var token    = TokenGenerator.NewAccessToken();
            var pending  = CreatePendingToken(LocalNodeA, RemoteNode2, CreatedAt1, TokenGenerator.NewAccessToken());
            var store    = await OpenStore(path);

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1, token));
            await store.AddPendingAccessTokenAsync(pending);

            var text = await File.ReadAllTextAsync(path);

            Assert.Multiple(() => {
                Assert.That(text, Does.Contain(token.Value),         "the active access token appears verbatim");
                Assert.That(text, Does.Contain(pending.Token.Value), "the pending access token appears verbatim");
            });

        }

        #endregion

        #region CustomProtector_HidesThePlaintextToken_AndRoundTripsWithTheSameScheme()

        [Test]
        public async Task CustomProtector_HidesThePlaintextToken_AndRoundTripsWithTheSameScheme()
        {

            var path       = NewPath();
            var protector  = new ReversingSecretProtector();
            var token      = TokenGenerator.NewAccessToken();
            var pairing    = CreatePairing(LocalNodeA, RemoteNode1, token);

            var store = await OpenStore(path, protector);
            await store.AddOrReplacePairingAsync(pairing);
            store.Dispose();

            var text = await File.ReadAllTextAsync(path);

            Assert.Multiple(() => {
                Assert.That(text, Does.Not.Contain(token.Value),                 "the plaintext token is not on disk");
                Assert.That(text, Does.Contain(protector.Protect(token.Value)),  "the protected (reversed) form is on disk");
            });

            var reopened       = await OpenStore(path, new ReversingSecretProtector());
            var storedPairing  = await reopened.GetPairingAsync(LocalNodeA, RemoteNode1);

            Assert.Multiple(() => {
                Assert.That(reopened.SecretProtector.SchemeId,  Is.EqualTo("test-reverse"));
                Assert.That(storedPairing,                      Is.EqualTo(pairing), "the same scheme recovers the pairing");
                Assert.That(storedPairing!.AccessToken,         Is.EqualTo(token),   "including the exact token");
            });

        }

        #endregion

        #region Reopening_WithADifferentSecretScheme_ThrowsNotSupported()

        [Test]
        public async Task Reopening_WithADifferentSecretScheme_ThrowsNotSupported()
        {

            var path   = NewPath();
            var store  = await OpenStore(path, new ReversingSecretProtector());

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));
            store.Dispose();

            // The file records scheme "test-reverse"; the default plaintext protector uses "".
            Assert.ThrowsAsync<NotSupportedException>(async () => await JSONFileS2Store.OpenAsync(path));

        }

        #endregion

        #region CustomProtector_AlsoProtectsThePendingTokenField()

        [Test]
        public async Task CustomProtector_AlsoProtectsThePendingTokenField()
        {

            var path       = NewPath();
            var protector  = new ReversingSecretProtector();
            var pending    = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1, TokenGenerator.NewAccessToken());

            var store = await OpenStore(path, protector);
            await store.AddPendingAccessTokenAsync(pending);
            store.Dispose();

            var text = await File.ReadAllTextAsync(path);

            Assert.Multiple(() => {
                Assert.That(text, Does.Not.Contain(pending.Token.Value),                 "the plaintext pending token is not on disk");
                Assert.That(text, Does.Contain(protector.Protect(pending.Token.Value)),  "the protected pending token is on disk");
            });

            var reopened = await OpenStore(path, new ReversingSecretProtector());

            Assert.That(await reopened.FindPendingAccessTokenAsync(pending.Token), Is.EqualTo(pending), "the same scheme recovers the pending token");

        }

        #endregion

        #region Open_WithHigherFormatVersion_ThrowsNotSupported()

        [Test]
        public async Task Open_WithHigherFormatVersion_ThrowsNotSupported()
        {

            var path = NewPath();

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            await File.WriteAllTextAsync(
                      path,
                      new JObject(
                          new JProperty("formatVersion",  99),
                          new JProperty("secretScheme",   ""),
                          new JProperty("pairings",       new JArray()),
                          new JProperty("pendingTokens",  new JArray()),
                          new JProperty("unpaired",       new JArray())
                      ).ToString()
                  );

            Assert.ThrowsAsync<NotSupportedException>(async () => await JSONFileS2Store.OpenAsync(path));

        }

        #endregion

        #region AtomicWrite_LeavesNoTempFileBehind()

        [Test]
        public async Task AtomicWrite_LeavesNoTempFileBehind()
        {

            var path   = NewPath();
            var store  = await OpenStore(path);

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));
            await store.FlushAsync();

            Assert.Multiple(() => {
                Assert.That(File.Exists(path),          Is.True,  "the store file exists");
                Assert.That(File.Exists(path + ".tmp"), Is.False, "no temporary file is left behind after a successful write");
            });

        }

        #endregion

        #region ConcurrentWrites_AreSerialized_AndAllPersisted()

        [Test]
        public async Task ConcurrentWrites_AreSerialized_AndAllPersisted()
        {

            var path      = NewPath();
            var store     = await OpenStore(path);
            var pairings  = Enumerable.Range(0, 20).
                                       Select(_ => CreatePairing(LocalNodeA, Node_Id.NewRandom)).
                                       ToList();

            // Fire all 20 writes together; each mutation persists under the internal file lock.
            var writes = pairings.Select(pairing => store.AddOrReplacePairingAsync(pairing).AsTask()).ToArray();
            await Task.WhenAll(writes);
            store.Dispose();

            var reopened  = await OpenStore(path);
            var stored    = await reopened.GetPairingsAsync(LocalNodeA);

            // The file must still be well-formed JSON after the concurrent writes.
            var json = JObject.Parse(await File.ReadAllTextAsync(path));

            Assert.Multiple(() => {
                Assert.That(stored,                              Has.Count.EqualTo(20));
                Assert.That(stored,                              Is.EquivalentTo(pairings));
                Assert.That((json["pairings"] as JArray)?.Count, Is.EqualTo(20), "all 20 pairings parse back from the file");
            });

        }

        #endregion

        #region PendingToken_WithSelectedProtocolAndVersion_RoundTrips()

        [Test]
        public async Task PendingToken_WithSelectedProtocolAndVersion_RoundTrips()
        {

            var path     = NewPath();
            var pending  = CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1);   // sets WebSocket + the S2 JSON version

            var store = await OpenStore(path);
            await store.AddPendingAccessTokenAsync(pending);
            store.Dispose();

            var reopened  = await OpenStore(path);
            var stored    = await reopened.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1);

            Assert.That(stored, Has.Count.EqualTo(1));

            var loaded = stored[0];

            Assert.Multiple(() => {
                Assert.That(loaded,                               Is.EqualTo(pending), "every field round-trips");
                Assert.That(loaded.SelectedCommunicationProtocol, Is.Not.Null);
                Assert.That(loaded.SelectedCommunicationProtocol, Is.EqualTo(pending.SelectedCommunicationProtocol), "the selected protocol round-trips");
                Assert.That(loaded.SelectedS2MessageVersion,      Is.Not.Null);
                Assert.That(loaded.SelectedS2MessageVersion,      Is.EqualTo(pending.SelectedS2MessageVersion), "the selected S2 message version round-trips");
                Assert.That(loaded.CreatedAt,                     Is.EqualTo(CreatedAt1));
            });

        }

        #endregion

        #region PendingToken_WithNullSelections_RoundTripsAsNull()

        [Test]
        public async Task PendingToken_WithNullSelections_RoundTripsAsNull()
        {

            var path     = NewPath();
            var pending  = new PendingAccessToken(LocalNodeA, RemoteNode1, TokenGenerator.NewAccessToken(), CreatedAt1);

            var store = await OpenStore(path);
            await store.AddPendingAccessTokenAsync(pending);
            store.Dispose();

            var reopened  = await OpenStore(path);
            var stored    = await reopened.GetPendingAccessTokensAsync(LocalNodeA, RemoteNode1);

            Assert.That(stored, Has.Count.EqualTo(1));

            var loaded = stored[0];

            Assert.Multiple(() => {
                Assert.That(loaded,                               Is.EqualTo(pending));
                Assert.That(loaded.SelectedCommunicationProtocol, Is.Null, "an unset protocol round-trips as null");
                Assert.That(loaded.SelectedS2MessageVersion,      Is.Null, "an unset version round-trips as null");
            });

        }

        #endregion

        #region Counts_SurviveReopen()

        [Test]
        public async Task Counts_SurviveReopen()
        {

            var path   = NewPath();
            var store  = await OpenStore(path);

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));                     // kept pairing
            await store.AddPendingAccessTokenAsync(CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1));  // kept pending token
            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode2));                     // to be unpaired
            await store.UnpairAsync(LocalNodeA, RemoteNode2, UnpairedAt);                                     // tombstone
            store.Dispose();

            var reopened = await OpenStore(path);

            Assert.Multiple(() => {
                Assert.That(reopened.Count,          Is.EqualTo(1), "one pairing survived");
                Assert.That(reopened.PendingCount,   Is.EqualTo(1), "one pending token survived");
                Assert.That(reopened.TombstoneCount, Is.EqualTo(1), "one tombstone survived");
            });

        }

        #endregion

        #region ToString_ContainsThePathAndTheCounts()

        [Test]
        public async Task ToString_ContainsThePathAndTheCounts()
        {

            var path   = NewPath();
            var store  = await OpenStore(path);

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));
            await store.AddPendingAccessTokenAsync(CreatePendingToken(LocalNodeA, RemoteNode1, CreatedAt1));
            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode2));
            await store.UnpairAsync(LocalNodeA, RemoteNode2, UnpairedAt);

            var text = store.ToString();

            Assert.Multiple(() => {
                Assert.That(text, Does.Contain(path),             "the file path");
                Assert.That(text, Does.Contain("1 pairing(s)"),   "the pairing count");
                Assert.That(text, Does.Contain("1 pending"),      "the pending count");
                Assert.That(text, Does.Contain("1 tombstone(s)"), "the tombstone count");
            });

        }

        #endregion


        #region (private class) ReversingSecretProtector

        /// <summary>
        /// A test secret protector with a non-empty scheme id that reverses the characters of a
        /// value (a cheap, reversible transform: the round-trip holds and the protected form
        /// differs from the plaintext).
        /// </summary>
        private sealed class ReversingSecretProtector : ISecretProtector
        {

            public String SchemeId => "test-reverse";

            public String Protect(String Plaintext)
            {
                var chars = Plaintext.ToCharArray();
                Array.Reverse(chars);
                return new String(chars);
            }

            public String Unprotect(String Protected)
            {
                var chars = Protected.ToCharArray();
                Array.Reverse(chars);
                return new String(chars);
            }

        }

        #endregion

    }

}

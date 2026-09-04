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
    /// The reference in-memory store (PLAN.md D8): the store contract plus its
    /// <see cref="InMemoryS2Store.Count"/> and <see cref="InMemoryS2Store.Clear"/> members.
    /// </summary>
    [TestFixture]
    public sealed class InMemoryS2StoreTests : S2StoreContractTests<InMemoryS2Store>
    {

        #region CreateStore()

        protected override InMemoryS2Store CreateStore()
            => new ();

        #endregion


        #region Count

        [Test]
        public async Task Count_FollowsAddsReplacementsAndRemovals()
        {

            var store = CreateStore();

            Assert.That(store.Count, Is.EqualTo(0));

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));
            Assert.That(store.Count, Is.EqualTo(1));

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));
            Assert.That(store.Count, Is.EqualTo(1), "a replacement does not add");

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeB, RemoteNode1));
            Assert.That(store.Count, Is.EqualTo(2));

            await store.RemovePairingAsync(LocalNodeA, RemoteNode1);
            Assert.That(store.Count, Is.EqualTo(1));

            await store.RemovePairingAsync(LocalNodeA, RemoteNode1);
            Assert.That(store.Count, Is.EqualTo(1), "removing an unknown pairing changes nothing");

        }

        #endregion

        #region Clear()

        [Test]
        public async Task Clear_RemovesEveryPairing()
        {

            var store = CreateStore();

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));
            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode2));
            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeB, RemoteNode1));

            store.Clear();

            Assert.Multiple(() => {
                Assert.That(store.Count,       Is.EqualTo(0));
                Assert.That(store.Clear,       Throws.Nothing, "clearing an empty store is harmless");
            });

            Assert.That(await store.GetPairingsAsync(),                       Is.Empty);
            Assert.That(await store.GetPairingAsync(LocalNodeA, RemoteNode1), Is.Null);

            await store.AddOrReplacePairingAsync(CreatePairing(LocalNodeA, RemoteNode1));

            Assert.That(store.Count, Is.EqualTo(1), "the store is usable after clearing");

        }

        #endregion

        #region Null guard

        [Test]
        public void AddOrReplacePairing_RejectsNull()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await CreateStore().AddOrReplacePairingAsync(null!));
        }

        #endregion

    }

}

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

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The persistent JSON file store (PLAN.md D8) run against the shared <see cref="IS2Store"/>
    /// contract: every contract test gets its own <see cref="JSONFileS2Store"/> on a unique
    /// temporary path, opened synchronously via <see cref="JSONFileS2Store.OpenAsync"/>. The
    /// stores are disposed and their files (and any leftover ".tmp" sibling) removed after
    /// each test.
    /// </summary>
    [TestFixture]
    public sealed class JSONFileS2StoreContractTests : S2StoreContractTests<JSONFileS2Store>
    {

        #region Data

        private readonly List<JSONFileS2Store>  stores   = [];
        private readonly List<String>           paths    = [];

        #endregion

        #region (override) CreateStore()

        protected override JSONFileS2Store CreateStore()
        {

            var path   = Path.Combine(Path.GetTempPath(), "wwcp-s2-store-tests", Guid.NewGuid().ToString("N") + ".json");
            var store  = JSONFileS2Store.OpenAsync(path).GetAwaiter().GetResult();

            paths. Add(path);
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

    }

}

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

namespace cloud.charging.open.protocols.S2.Tests.Connect.Discovery
{

    /// <summary>
    /// The in-memory WAN registry (the reference implementation and test double behind the
    /// registry API): initial records, add-or-replace by identification, removal, clearing,
    /// queries with status, regions, flags and paging, single records, insertion order and
    /// its text representation.
    /// </summary>
    [TestFixture]
    public sealed class InMemoryWANRegistryTests
    {

        #region Constructor_WithoutRecords_IsEmpty()

        [Test]
        public void Constructor_WithoutRecords_IsEmpty()
        {

            var registry = new InMemoryWANRegistry();

            Assert.Multiple(() => {
                Assert.That(registry.Count,       Is.EqualTo(0));
                Assert.That(registry.Records,     Is.Empty);
                Assert.That(registry.ToString(),  Does.Contain("0 record"));
            });

            var explicitlyEmpty = new InMemoryWANRegistry([]);

            Assert.That(explicitlyEmpty.Count,    Is.EqualTo(0));

        }

        #endregion

        #region Constructor_WithRecords_KeepsInsertionOrder()

        [Test]
        public void Constructor_WithRecords_KeepsInsertionOrder()
        {

            var sample    = new SampleEndpointRecords();
            var registry  = new InMemoryWANRegistry(sample.All);

            Assert.Multiple(() => {
                Assert.That(registry.Count,     Is.EqualTo(4));
                Assert.That(registry.Records,   Is.EqualTo(sample.All));
                Assert.That(registry.Records.Select(record => record.Name), Is.EqualTo(new[] { "Alpha", "Beta", "Gamma", "Delta" }));
            });

            // Duplicate identifications in the initial records: the last one wins, the position is the first.
            var first   = WANRegistryFixture.Record("First",  [ "NL" ], EndpointStatus.Public, CEM: true, RM: false);
            var second  = WANRegistryFixture.Record("Second", [ "BE" ], EndpointStatus.Public, CEM: true, RM: false, Id: first.Id);
            var other   = WANRegistryFixture.Record("Other",  [ "DE" ], EndpointStatus.Public, CEM: true, RM: false);

            var deduplicated = new InMemoryWANRegistry([ first, other, second ]);

            Assert.Multiple(() => {
                Assert.That(deduplicated.Count,     Is.EqualTo(2));
                Assert.That(deduplicated.Records,   Is.EqualTo(new[] { second, other }));
            });

        }

        #endregion

        #region AddOrReplace_NewId_AppendsTheRecord()

        [Test]
        public void AddOrReplace_NewId_AppendsTheRecord()
        {

            var sample    = new SampleEndpointRecords();
            var registry  = new InMemoryWANRegistry([ sample.Alpha, sample.Beta ]);

            var returned  = registry.AddOrReplace(sample.Gamma);

            Assert.Multiple(() => {
                Assert.That(returned,           Is.SameAs(registry), "AddOrReplace is fluent");
                Assert.That(registry.Count,     Is.EqualTo(3));
                Assert.That(registry.Records,   Is.EqualTo(new[] { sample.Alpha, sample.Beta, sample.Gamma }));
            });

            // Chained additions keep the insertion order.
            registry.AddOrReplace(sample.Delta).
                     AddOrReplace(WANRegistryFixture.Record("Epsilon", [ "FR" ], EndpointStatus.Public, CEM: false, RM: true));

            Assert.That(registry.Count,                                     Is.EqualTo(5));
            Assert.That(registry.Records.Select(record => record.Name),     Is.EqualTo(new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" }));

        }

        #endregion

        #region AddOrReplace_SameId_ReplacesTheRecordInPlace()

        [Test]
        public void AddOrReplace_SameId_ReplacesTheRecordInPlace()
        {

            var sample       = new SampleEndpointRecords();
            var registry     = new InMemoryWANRegistry(sample.All);

            var replacement  = WANRegistryFixture.Record("Beta Cloud", [ "DE", "AT" ], EndpointStatus.Testing, CEM: true, RM: true, Id: sample.Beta.Id);

            registry.AddOrReplace(replacement);

            Assert.Multiple(() => {
                Assert.That(registry.Count,                 Is.EqualTo(4), "a replacement does not add a record");
                Assert.That(registry.Records,               Does.Contain(replacement));
                Assert.That(registry.Records,               Does.Not.Contain(sample.Beta));
                Assert.That(registry.Records[1],            Is.EqualTo(replacement), "the replacement takes the position of the replaced record");
                Assert.That(registry.Records[1].Name,       Is.EqualTo("Beta Cloud"));
                Assert.That(registry.Records[1].Status,     Is.EqualTo(EndpointStatus.Testing));
                Assert.That(registry.Records[1].Regions,    Is.EqualTo(new[] { CountryCode.Parse("DE"), CountryCode.Parse("AT") }));
            });

            // Adding an equal record again changes nothing.
            registry.AddOrReplace(replacement);

            Assert.That(registry.Count,   Is.EqualTo(4));

        }

        #endregion

        #region AddOrReplace_Null_Throws()

        [Test]
        public void AddOrReplace_Null_Throws()
        {

            var registry = new InMemoryWANRegistry();

            Assert.That(() => registry.AddOrReplace(null!), Throws.ArgumentNullException);
            Assert.That(registry.Count, Is.EqualTo(0));

        }

        #endregion

        #region Remove_KnownId_ReturnsTrueAndRemovesTheRecord()

        [Test]
        public void Remove_KnownId_ReturnsTrueAndRemovesTheRecord()
        {

            var sample    = new SampleEndpointRecords();
            var registry  = new InMemoryWANRegistry(sample.All);

            var removed   = registry.Remove(sample.Beta.Id);

            Assert.Multiple(() => {
                Assert.That(removed,            Is.True);
                Assert.That(registry.Count,     Is.EqualTo(3));
                Assert.That(registry.Records,   Is.EqualTo(new[] { sample.Alpha, sample.Gamma, sample.Delta }), "the remaining records keep their order");
            });

            // A second removal of the same identification finds nothing.
            Assert.That(registry.Remove(sample.Beta.Id),  Is.False);
            Assert.That(registry.Count,                   Is.EqualTo(3));

        }

        #endregion

        #region Remove_UnknownId_ReturnsFalse()

        [Test]
        public void Remove_UnknownId_ReturnsFalse()
        {

            var sample    = new SampleEndpointRecords();
            var registry  = new InMemoryWANRegistry(sample.All);

            var removed   = registry.Remove(EndpointRecord_Id.NewRandom);

            Assert.Multiple(() => {
                Assert.That(removed,            Is.False);
                Assert.That(registry.Count,     Is.EqualTo(4));
                Assert.That(registry.Records,   Is.EqualTo(sample.All));
            });

        }

        #endregion

        #region Clear_RemovesEveryRecord()

        [Test]
        public async Task Clear_RemovesEveryRecord()
        {

            var sample    = new SampleEndpointRecords();
            var registry  = new InMemoryWANRegistry(sample.All);

            registry.Clear();

            Assert.Multiple(() => {
                Assert.That(registry.Count,       Is.EqualTo(0));
                Assert.That(registry.Records,     Is.Empty);
                Assert.That(registry.ToString(),  Does.Contain("0 record"));
            });

            Assert.That(await registry.QueryAsync(WANRegistryQuery.All),  Is.Empty);
            Assert.That(await registry.GetAsync(sample.Alpha.Id),         Is.Null);

            // The registry is usable after clearing.
            registry.AddOrReplace(sample.Alpha);

            Assert.That(registry.Records, Is.EqualTo(new[] { sample.Alpha }));

        }

        #endregion

        #region QueryAsync_AppliesStatusRegionsFlagsAndPaging()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryAsync_AppliesStatusRegionsFlagsAndPaging()
        {

            var sample    = new SampleEndpointRecords();
            var registry  = new InMemoryWANRegistry(sample.All);

            var all       = await registry.QueryAsync(WANRegistryQuery.All);
            var testing   = await registry.QueryAsync(new WANRegistryQuery(Status: EndpointStatus.Testing));
            var benelux   = await registry.QueryAsync(new WANRegistryQuery(Regions: [ CountryCode.Parse("NL"), CountryCode.Parse("BE") ]));
            var cems      = await registry.QueryAsync(new WANRegistryQuery(CEM: true));
            var noRMs     = await registry.QueryAsync(new WANRegistryQuery(RM: false));
            var paged     = await registry.QueryAsync(new WANRegistryQuery(Limit: 1, Offset: 1));
            var filtered  = await registry.QueryAsync(new WANRegistryQuery(Regions: [ CountryCode.Parse("NL") ], Status: EndpointStatus.Testing, CEM: true, RM: true));
            var nothing   = await registry.QueryAsync(new WANRegistryQuery(Regions: [ CountryCode.Parse("AT") ]));

            Assert.Multiple(() => {
                Assert.That(all,        Is.EqualTo(sample.Public), "the default status is public");
                Assert.That(testing,    Is.EqualTo(new[] { sample.Delta }));
                Assert.That(benelux,    Is.EqualTo(new[] { sample.Alpha, sample.Gamma }), "endpoints active in at least one of the regions");
                Assert.That(cems,       Is.EqualTo(new[] { sample.Alpha, sample.Gamma }));
                Assert.That(noRMs,      Is.EqualTo(new[] { sample.Alpha }));
                Assert.That(paged,      Is.EqualTo(new[] { sample.Beta }), "paging in insertion order");
                Assert.That(filtered,   Is.EqualTo(new[] { sample.Delta }));
                Assert.That(nothing,    Is.Empty);
            });

        }

        #endregion

        #region QueryAsync_Null_Throws()

        [Test]
        public void QueryAsync_Null_Throws()
        {

            var registry = new InMemoryWANRegistry();

            Assert.ThrowsAsync<ArgumentNullException>(async () => await registry.QueryAsync(null!));

        }

        #endregion

        #region GetAsync_KnownId_ReturnsTheRecord()

        [Test]
        [S2C("Registry.GetEndpoint")]
        public async Task GetAsync_KnownId_ReturnsTheRecord()
        {

            var sample    = new SampleEndpointRecords();
            var registry  = new InMemoryWANRegistry(sample.All);

            var beta      = await registry.GetAsync(sample.Beta.Id);
            var delta     = await registry.GetAsync(sample.Delta.Id);

            Assert.Multiple(() => {
                Assert.That(beta,     Is.SameAs(sample.Beta));
                Assert.That(delta,    Is.SameAs(sample.Delta), "the status does not matter for a single record");
            });

        }

        #endregion

        #region GetAsync_UnknownId_ReturnsNull()

        [Test]
        [S2C("Registry.GetEndpoint.404")]
        public async Task GetAsync_UnknownId_ReturnsNull()
        {

            var sample    = new SampleEndpointRecords();
            var registry  = new InMemoryWANRegistry(sample.All);

            Assert.That(await registry.GetAsync(EndpointRecord_Id.NewRandom), Is.Null);

            registry.Remove(sample.Alpha.Id);

            Assert.That(await registry.GetAsync(sample.Alpha.Id),             Is.Null, "a removed record is unknown");

        }

        #endregion

        #region Records_ReturnsASnapshot()

        [Test]
        public void Records_ReturnsASnapshot()
        {

            var sample    = new SampleEndpointRecords();
            var registry  = new InMemoryWANRegistry([ sample.Alpha, sample.Beta ]);

            var snapshot  = registry.Records;

            registry.AddOrReplace(sample.Gamma);
            registry.Remove(sample.Alpha.Id);

            Assert.Multiple(() => {
                Assert.That(snapshot,           Is.EqualTo(new[] { sample.Alpha, sample.Beta }), "the snapshot does not change");
                Assert.That(registry.Records,   Is.EqualTo(new[] { sample.Beta, sample.Gamma }));
            });

        }

        #endregion

        #region ToString_ContainsTheCount()

        [Test]
        public void ToString_ContainsTheCount()
        {

            var sample    = new SampleEndpointRecords();
            var registry  = new InMemoryWANRegistry(sample.All);

            Assert.That(registry.ToString(), Does.Contain("4 record").And.Contain("WAN registry"));

            registry.Remove(sample.Delta.Id);

            Assert.That(registry.ToString(), Does.Contain("3 record"));

        }

        #endregion

    }

}

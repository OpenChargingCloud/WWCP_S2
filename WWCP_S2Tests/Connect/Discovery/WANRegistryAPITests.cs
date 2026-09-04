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

using System.Net;

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect.Discovery
{

    /// <summary>
    /// The reference WAN endpoint registry API over real HTTP with a plain HttpClient
    /// (s2-connect-wan-endpoint-registry.yml): the version index, GET /v1/endpoint with its
    /// filters (region, status, cem, rm) and paging (limit, offset), the 400 answers to
    /// invalid query parameters, GET /v1/endpoint/{id} with its 404 answers, the root path
    /// variants and the visibility of registry changes.
    /// </summary>
    [TestFixture]
    public sealed class WANRegistryAPITests
    {

        #region Helpers

        /// <summary>
        /// The "message" of a JSON error object, when any.
        /// </summary>
        private static String? Message(HTTPResult Result)
            => (Result.JSON as JObject)?["message"]?.Value<String>();

        /// <summary>
        /// The response is 200 with a JSON array of exactly the given records (in this order),
        /// each serialised like <see cref="EndpointRecord.ToJSON"/>.
        /// </summary>
        private static void AssertRecords(HTTPResult               Result,
                                          params EndpointRecord[]  Expected)
        {

            Assert.That(Result.Status,       Is.EqualTo(HttpStatusCode.OK), Result.Body);
            Assert.That(Result.ContentType,  Does.StartWith("application/json"));

            var array = Result.Array;

            Assert.That(array.Select(token => token["name"]?.Value<String>()),
                        Is.EqualTo(Expected.Select(record => record.Name)),
                        "the names of the records, in order");

            for (var i = 0; i < Expected.Length; i++)
            {
                Assert.That(array[i],                                        Is.TypeOf<JObject>());
                Assert.That(JToken.DeepEquals(array[i], Expected[i].ToJSON()), Is.True, $"the JSON of '{Expected[i].Name}' is its ToJSON()");
            }

        }

        #endregion


        // The version index (S2 Connect 1.0.0, "Selecting the version of REST APIs")

        #region VersionIndex_Returns200WithV1AsJSON()

        [Test]
        [S2C("Versioning.2")]
        [S2C("Registry.VersionIndex")]
        public async Task VersionIndex_Returns200WithV1AsJSON()
        {

            await using var fixture = await WANRegistryFixture.CreateAsync();

            var result = await fixture.GetAsync("");

            Assert.Multiple(() => {
                Assert.That(result.Status,                                    Is.EqualTo(HttpStatusCode.OK), result.Body);
                Assert.That(result.ContentType,                               Does.StartWith("application/json"));
                Assert.That(result.Array.Select(v => v.Value<String>()),      Is.EqualTo(new[] { "v1" }));
                Assert.That(JToken.DeepEquals(result.JSON, new JArray("v1")), Is.True, result.Body);
                Assert.That(fixture.API.SupportedAPIVersions,                 Is.EqualTo(new[] { "v1" }));
                Assert.That(WANRegistryAPI.APIVersion,                        Is.EqualTo("v1"));
                Assert.That(fixture.API.Registry,                             Is.SameAs(fixture.Registry));
            });

        }

        #endregion

        #region UnknownPath_Returns404()

        [Test]
        public async Task UnknownPath_Returns404()
        {

            await using var fixture = await WANRegistryFixture.CreateAsync();

            var result = await fixture.GetAsync("v1/unknown");

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.NotFound), result.Body);

        }

        #endregion


        // GET /v1/endpoint: filters

        #region QueryEndpoints_WithoutParameters_ReturnsPublicRecordsInInsertionOrder()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_WithoutParameters_ReturnsPublicRecordsInInsertionOrder()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var result = await fixture.GetAsync("v1/endpoint");

            // The testing endpoint "Delta" is not part of the default (public) listing.
            AssertRecords(result, sample.Alpha, sample.Beta, sample.Gamma);

        }

        #endregion

        #region QueryEndpoints_StatusPublic_IsTheDefault()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_StatusPublic_IsTheDefault()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var explicitly  = await fixture.GetAsync("v1/endpoint?status=public");
            var implicitly  = await fixture.GetAsync("v1/endpoint");

            AssertRecords(explicitly, sample.Alpha, sample.Beta, sample.Gamma);

            Assert.That(JToken.DeepEquals(explicitly.JSON, implicitly.JSON), Is.True, "status=public and no status parameter answer the same list");

        }

        #endregion

        #region QueryEndpoints_StatusTesting_ReturnsTestingRecords()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_StatusTesting_ReturnsTestingRecords()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var result = await fixture.GetAsync("v1/endpoint?status=testing");

            AssertRecords(result, sample.Delta);

        }

        #endregion

        #region QueryEndpoints_RegionList_MatchesAnyOfTheRegions()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_RegionList_MatchesAnyOfTheRegions()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            // "region=NL,BE" (style form, explode false): endpoints active in at least one of the codes.
            var result = await fixture.GetAsync("v1/endpoint?region=NL,BE");

            AssertRecords(result, sample.Alpha, sample.Gamma);

            // A single region of a multi-region endpoint is enough.
            var luxembourg = await fixture.GetAsync("v1/endpoint?region=LU");

            AssertRecords(luxembourg, sample.Gamma);

            // No public endpoint in Austria.
            var austria = await fixture.GetAsync("v1/endpoint?region=AT");

            AssertRecords(austria);

        }

        #endregion

        #region QueryEndpoints_RepeatedRegionParameter_MatchesAnyOfTheRegions()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_RepeatedRegionParameter_MatchesAnyOfTheRegions()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            // The repeated parameter form (style form, explode true) is accepted as well.
            var result = await fixture.GetAsync("v1/endpoint?region=NL&region=DE");

            AssertRecords(result, sample.Alpha, sample.Beta);

        }

        #endregion

        #region QueryEndpoints_CEMTrue_ReturnsCEMRecords()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_CEMTrue_ReturnsCEMRecords()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var cems    = await fixture.GetAsync("v1/endpoint?cem=true");
            var noCEMs  = await fixture.GetAsync("v1/endpoint?cem=false");

            AssertRecords(cems,   sample.Alpha, sample.Gamma);
            AssertRecords(noCEMs, sample.Beta);

        }

        #endregion

        #region QueryEndpoints_RMFalse_ReturnsRecordsWithoutRM()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_RMFalse_ReturnsRecordsWithoutRM()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var noRMs  = await fixture.GetAsync("v1/endpoint?rm=false");
            var rms    = await fixture.GetAsync("v1/endpoint?rm=true");

            AssertRecords(noRMs, sample.Alpha);
            AssertRecords(rms,   sample.Beta, sample.Gamma);

        }

        #endregion

        #region QueryEndpoints_CombinedFilters_ApplyAllOfThem()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_CombinedFilters_ApplyAllOfThem()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var result = await fixture.GetAsync("v1/endpoint?region=NL,BE&cem=true&rm=true");

            // "Alpha" is Dutch and a CEM but no RM, "Delta" is Dutch with both roles but testing only.
            AssertRecords(result, sample.Gamma);

            var testing = await fixture.GetAsync("v1/endpoint?region=NL,BE&cem=true&rm=true&status=testing");

            AssertRecords(testing, sample.Delta);

        }

        #endregion


        // GET /v1/endpoint: paging

        #region QueryEndpoints_LimitAndOffset_PageInInsertionOrder()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_LimitAndOffset_PageInInsertionOrder()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            AssertRecords(await fixture.GetAsync("v1/endpoint?limit=1&offset=1"), sample.Beta);
            AssertRecords(await fixture.GetAsync("v1/endpoint?limit=2"),          sample.Alpha, sample.Beta);
            AssertRecords(await fixture.GetAsync("v1/endpoint?offset=2"),         sample.Gamma);
            AssertRecords(await fixture.GetAsync("v1/endpoint?offset=3"));
            AssertRecords(await fixture.GetAsync("v1/endpoint?limit=10"),         sample.Alpha, sample.Beta, sample.Gamma);

        }

        #endregion

        #region QueryEndpoints_Paging_AppliesAfterFiltering()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_Paging_AppliesAfterFiltering()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            // The CEMs are "Alpha" and "Gamma": the second page of size one is "Gamma".
            AssertRecords(await fixture.GetAsync("v1/endpoint?cem=true&limit=1&offset=1"), sample.Gamma);

            // Only one testing endpoint: the second page is empty.
            AssertRecords(await fixture.GetAsync("v1/endpoint?status=testing&limit=1&offset=1"));

        }

        #endregion


        // GET /v1/endpoint: invalid query parameters (400)

        #region QueryEndpoints_InvalidParameter_Returns400WithMessage(Parameter)

        [TestCase("region=nl")]
        [TestCase("region=NLD")]
        [TestCase("region=NL,nl")]
        [TestCase("status=private")]
        [TestCase("cem=yes")]
        [TestCase("rm=maybe")]
        [TestCase("limit=0")]
        [TestCase("limit=-5")]
        [TestCase("limit=abc")]
        [TestCase("offset=-1")]
        [TestCase("offset=abc")]
        [S2C("Registry.QueryEndpoints.400")]
        public async Task QueryEndpoints_InvalidParameter_Returns400WithMessage(String Parameter)
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var result = await fixture.GetAsync($"v1/endpoint?{Parameter}");

            Assert.Multiple(() => {
                Assert.That(result.Status,        Is.EqualTo(HttpStatusCode.BadRequest), result.Body);
                Assert.That(result.ContentType,   Does.StartWith("application/json"));
                Assert.That(result.JSON,          Is.TypeOf<JObject>(), result.Body);
                Assert.That(Message(result),      Is.Not.Null.And.Not.Empty, result.Body);
            });

            // The registry still answers valid queries afterwards (nothing is left on the connection).
            AssertRecords(await fixture.GetAsync("v1/endpoint"), sample.Alpha, sample.Beta, sample.Gamma);

        }

        #endregion

        #region QueryEndpoints_InvalidParameter_MessageNamesTheValue()

        [Test]
        [S2C("Registry.QueryEndpoints.400")]
        public async Task QueryEndpoints_InvalidParameter_MessageNamesTheValue()
        {

            await using var fixture = await WANRegistryFixture.CreateAsync();

            var region  = await fixture.GetAsync("v1/endpoint?region=NLD");
            var status  = await fixture.GetAsync("v1/endpoint?status=private");
            var cem     = await fixture.GetAsync("v1/endpoint?cem=yes");
            var limit   = await fixture.GetAsync("v1/endpoint?limit=0");

            Assert.Multiple(() => {
                Assert.That(Message(region),   Does.Contain("NLD"));
                Assert.That(Message(status),   Does.Contain("private"));
                Assert.That(Message(cem),      Does.Contain("yes").And.Contain("cem"));
                Assert.That(Message(limit),    Does.Contain("limit"));
            });

        }

        #endregion


        // GET /v1/endpoint/{id}

        #region GetEndpoint_KnownId_Returns200WithTheRecord()

        [Test]
        [S2C("Registry.GetEndpoint")]
        public async Task GetEndpoint_KnownId_Returns200WithTheRecord()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var result = await fixture.GetAsync($"v1/endpoint/{sample.Beta.Id}");

            Assert.Multiple(() => {
                Assert.That(result.Status,                                          Is.EqualTo(HttpStatusCode.OK), result.Body);
                Assert.That(result.ContentType,                                     Does.StartWith("application/json"));
                Assert.That(result.JSON,                                            Is.TypeOf<JObject>(), result.Body);
                Assert.That(JToken.DeepEquals(result.JSON, sample.Beta.ToJSON()),   Is.True, result.Body);
            });

            // The JSON parses back into an equal record.
            Assert.That(EndpointRecord.TryParse(result.Object, out var parsed, out var error), Is.True, error);
            Assert.That(parsed, Is.EqualTo(sample.Beta));

        }

        #endregion

        #region GetEndpoint_TestingRecord_IsServedById()

        [Test]
        [S2C("Registry.GetEndpoint")]
        public async Task GetEndpoint_TestingRecord_IsServedById()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            // The status filter applies to the listing only; a single record is served regardless of its status.
            var result = await fixture.GetAsync($"v1/endpoint/{sample.Delta.Id}");

            Assert.Multiple(() => {
                Assert.That(result.Status,                                          Is.EqualTo(HttpStatusCode.OK), result.Body);
                Assert.That(JToken.DeepEquals(result.JSON, sample.Delta.ToJSON()),  Is.True, result.Body);
            });

        }

        #endregion

        #region GetEndpoint_UnknownId_Returns404WithMessage()

        [Test]
        [S2C("Registry.GetEndpoint.404")]
        public async Task GetEndpoint_UnknownId_Returns404WithMessage()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var unknown = EndpointRecord_Id.NewRandom;
            var result  = await fixture.GetAsync($"v1/endpoint/{unknown}");

            Assert.Multiple(() => {
                Assert.That(result.Status,        Is.EqualTo(HttpStatusCode.NotFound), result.Body);
                Assert.That(result.ContentType,   Does.StartWith("application/json"));
                Assert.That(result.JSON,          Is.TypeOf<JObject>(), result.Body);
                Assert.That(Message(result),      Does.Contain(unknown.ToString()));
            });

        }

        #endregion

        #region GetEndpoint_NonUUID_Returns404WithMessage()

        [Test]
        [S2C("Registry.GetEndpoint.404")]
        public async Task GetEndpoint_NonUUID_Returns404WithMessage()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var result = await fixture.GetAsync("v1/endpoint/abc");

            Assert.Multiple(() => {
                Assert.That(result.Status,        Is.EqualTo(HttpStatusCode.NotFound), result.Body);
                Assert.That(result.ContentType,   Does.StartWith("application/json"));
                Assert.That(Message(result),      Does.Contain("abc").And.Contain("UUID"));
            });

        }

        #endregion


        // Root paths

        #region RootPathSlash_ServesTheAPIAtTheRoot()

        [Test]
        [S2C("Registry.VersionIndex")]
        public async Task RootPathSlash_ServesTheAPIAtTheRoot()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All, RootPath: "/");

            Assert.That(fixture.RegistryUrl.Value, Is.EqualTo($"http://127.0.0.1:{fixture.Port}/"));

            var versions  = await fixture.GetAsync("");
            var endpoints = await fixture.GetAsync("v1/endpoint");
            var single    = await fixture.GetAsync($"v1/endpoint/{sample.Alpha.Id}");

            Assert.Multiple(() => {
                Assert.That(versions.Status,                                        Is.EqualTo(HttpStatusCode.OK), versions.Body);
                Assert.That(versions.Array.Select(v => v.Value<String>()),          Is.EqualTo(new[] { "v1" }));
                Assert.That(single.Status,                                          Is.EqualTo(HttpStatusCode.OK), single.Body);
                Assert.That(JToken.DeepEquals(single.JSON, sample.Alpha.ToJSON()),  Is.True, single.Body);
            });

            AssertRecords(endpoints, sample.Alpha, sample.Beta, sample.Gamma);

        }

        #endregion

        #region RootPathWithoutTrailingSlash_IsNormalised()

        [Test]
        public async Task RootPathWithoutTrailingSlash_IsNormalised()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All, RootPath: "/wan");

            Assert.That(fixture.RegistryUrl.Value, Does.EndWith("/wan/"));

            var versions  = await fixture.GetAsync("");
            var endpoints = await fixture.GetAsync("v1/endpoint");

            Assert.That(versions.Status, Is.EqualTo(HttpStatusCode.OK), versions.Body);
            Assert.That(versions.Array.Select(v => v.Value<String>()), Is.EqualTo(new[] { "v1" }));

            AssertRecords(endpoints, sample.Alpha, sample.Beta, sample.Gamma);

        }

        #endregion


        // Registry changes

        #region RegistryChanges_AreVisibleImmediately()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        [S2C("Registry.GetEndpoint")]
        public async Task RegistryChanges_AreVisibleImmediately()
        {

            await using var fixture = await WANRegistryFixture.CreateAsync();

            AssertRecords(await fixture.GetAsync("v1/endpoint"));

            var record = WANRegistryFixture.Record("Epsilon", [ "FR" ], EndpointStatus.Public, CEM: true, RM: false);

            fixture.Registry.AddOrReplace(record);

            AssertRecords(await fixture.GetAsync("v1/endpoint"), record);

            var single = await fixture.GetAsync($"v1/endpoint/{record.Id}");

            Assert.That(single.Status, Is.EqualTo(HttpStatusCode.OK), single.Body);
            Assert.That(JToken.DeepEquals(single.JSON, record.ToJSON()), Is.True, single.Body);

            // A replacement with the same identification is served instead of the old record.
            var renamed = WANRegistryFixture.Record("Epsilon Cloud", [ "FR", "BE" ], EndpointStatus.Public, CEM: true, RM: true, Id: record.Id);

            fixture.Registry.AddOrReplace(renamed);

            AssertRecords(await fixture.GetAsync("v1/endpoint"), renamed);

            var replaced = await fixture.GetAsync($"v1/endpoint/{record.Id}");

            Assert.That(replaced.Status, Is.EqualTo(HttpStatusCode.OK), replaced.Body);
            Assert.That(JToken.DeepEquals(replaced.JSON, renamed.ToJSON()), Is.True, replaced.Body);

            // After the removal the record is gone from the listing and answers 404.
            Assert.That(fixture.Registry.Remove(record.Id), Is.True);

            AssertRecords(await fixture.GetAsync("v1/endpoint"));

            var removed = await fixture.GetAsync($"v1/endpoint/{record.Id}");

            Assert.That(removed.Status, Is.EqualTo(HttpStatusCode.NotFound), removed.Body);

        }

        #endregion

    }

}

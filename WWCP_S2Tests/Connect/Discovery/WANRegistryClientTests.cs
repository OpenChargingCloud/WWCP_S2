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

using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect.Discovery
{

    /// <summary>
    /// The WAN registry client against the reference registry API (version index, queries
    /// with filters and paging, single records, 404) and against a fake registry that answers
    /// wrongly on purpose (no common API version, an unusable version index, records violating
    /// the schema, 400 and unexpected status codes), the reuse of the selected version, the
    /// query parameters as the server parses them back, an unreachable registry and the
    /// validation of the client options.
    /// </summary>
    [TestFixture]
    public sealed class WANRegistryClientTests
    {

        #region (class) FakeRegistry

        /// <summary>
        /// A fake WAN registry on a plain Hermod HTTP API: by default it answers the version
        /// index with ["v1"], every query with an empty list and every single record with 404;
        /// each answer can be replaced per test. It counts the version index and endpoint
        /// requests and records every query as the server side parses it.
        /// </summary>
        private sealed class FakeRegistry : IAsyncDisposable
        {

            #region Data

            private readonly List<WANRegistryQuery?>  queries = [];
            private          Int32                    versionIndexRequests;
            private          Int32                    endpointRequests;

            #endregion

            #region Properties

            public IPPort      Port          { get; }
            public HTTPServer  HTTPServer    { get; }
            public HTTPAPI     API           { get; }
            public S2BaseURL   RegistryUrl   { get; }

            public Func<HTTPRequest, Task<HTTPResponse>>  OnVersionIndex     { get; set; }
            public Func<HTTPRequest, Task<HTTPResponse>>  OnQueryEndpoints   { get; set; }
            public Func<HTTPRequest, Task<HTTPResponse>>  OnGetEndpoint      { get; set; }

            /// <summary>
            /// The number of version index requests received so far.
            /// </summary>
            public Int32 VersionIndexRequests
                => Volatile.Read(ref versionIndexRequests);

            /// <summary>
            /// The number of GET /v1/endpoint and GET /v1/endpoint/{id} requests received so far.
            /// </summary>
            public Int32 EndpointRequests
                => Volatile.Read(ref endpointRequests);

            /// <summary>
            /// The queries of the GET /v1/endpoint requests as the server side parsed them
            /// (null when the query parameters were invalid), in the order of their arrival.
            /// </summary>
            public IReadOnlyList<WANRegistryQuery?> Queries
            {
                get
                {
                    lock (queries)
                    {
                        return [.. queries];
                    }
                }
            }

            #endregion

            #region Constructor(s)

            private FakeRegistry(IPPort      Port,
                                 HTTPServer  HTTPServer,
                                 HTTPAPI     API,
                                 S2BaseURL   RegistryUrl)
            {

                this.Port         = Port;
                this.HTTPServer   = HTTPServer;
                this.API          = API;
                this.RegistryUrl  = RegistryUrl;

                OnVersionIndex    = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK,       new JArray("v1")));
                OnQueryEndpoints  = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK,       new JArray()));
                OnGetEndpoint     = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.NotFound, new JObject(new JProperty("message", "No such endpoint record!"))));

            }

            #endregion


            #region (static) StartAsync(RootPath = "/other/")

            /// <summary>
            /// Start a fake registry on a free loopback port.
            /// </summary>
            /// <param name="RootPath">The root path of the fake registry API.</param>
            public static async Task<FakeRegistry> StartAsync(String RootPath = "/other/")
            {

                var port        = PairingServerFixture.FreePort();

                var httpServer  = new HTTPServer(
                                      IPv4Address.Parse("127.0.0.1"),
                                      port,
                                      "S2 fake registry",
                                      AutoStart: false
                                  );

                var api         = new HTTPAPI(
                                      httpServer,
                                      RootPath:        HTTPPath.Parse(RootPath),
                                      DisableLogging:  true
                                  );

                var fake        = new FakeRegistry(
                                      port,
                                      httpServer,
                                      api,
                                      S2BaseURL.Parse($"http://127.0.0.1:{port}{RootPath}", AllowHTTP: true)
                                  );

                // The handlers are looked up per request, so the tests can replace them after the start.
                api.AddHandler(HTTPMethod.GET, HTTPPath.Root,                          fake.HandleVersionIndexAsync);
                api.AddHandler(HTTPMethod.GET, HTTPPath.Parse("/v1/endpoint"),         fake.HandleQueryEndpointsAsync);
                api.AddHandler(HTTPMethod.GET, HTTPPath.Parse("/v1/endpoint/{id}"),    fake.HandleGetEndpointAsync);

                await httpServer.Start();

                return fake;

            }

            #endregion


            #region (private) HandleVersionIndexAsync / HandleQueryEndpointsAsync / HandleGetEndpointAsync

            private Task<HTTPResponse> HandleVersionIndexAsync(HTTPRequest Request)
            {
                Interlocked.Increment(ref versionIndexRequests);
                return OnVersionIndex(Request);
            }

            private Task<HTTPResponse> HandleQueryEndpointsAsync(HTTPRequest Request)
            {

                Interlocked.Increment(ref endpointRequests);

                var query = WANRegistryQuery.TryParse(Request.QueryString, out var parsed, out _)
                                ? parsed
                                : null;

                lock (queries)
                {
                    queries.Add(query);
                }

                return OnQueryEndpoints(Request);

            }

            private Task<HTTPResponse> HandleGetEndpointAsync(HTTPRequest Request)
            {
                Interlocked.Increment(ref endpointRequests);
                return OnGetEndpoint(Request);
            }

            #endregion

            #region DisposeAsync()

            public async ValueTask DisposeAsync()
            {
                await HTTPServer.Stop();
            }

            #endregion

        }

        #endregion

        #region HTTP helpers

        /// <summary>
        /// A JSON response with the given status code.
        /// </summary>
        private static HTTPResponse JSONResponse(HTTPRequest     Request,
                                                 HTTPStatusCode  StatusCode,
                                                 JToken          Body)

            => new HTTPResponse.Builder(Request) {
                   HTTPStatusCode  = StatusCode,
                   Connection      = ConnectionType.KeepAlive,
                   ContentType     = HTTPContentType.Application.JSON_UTF8,
                   Content         = Encoding.UTF8.GetBytes(Body.ToString(Formatting.None))
               }.AsImmutable;


        /// <summary>
        /// A plain text response with the given status code.
        /// </summary>
        private static HTTPResponse TextResponse(HTTPRequest     Request,
                                                 HTTPStatusCode  StatusCode,
                                                 String          Text)

            => new HTTPResponse.Builder(Request) {
                   HTTPStatusCode  = StatusCode,
                   Connection      = ConnectionType.KeepAlive,
                   ContentType     = HTTPContentType.Text.PLAIN,
                   Content         = Encoding.UTF8.GetBytes(Text)
               }.AsImmutable;


        /// <summary>
        /// The JSON of a record that violates the schema: the mandatory "name" is missing.
        /// </summary>
        private static JObject RecordWithoutName()
        {

            var json = WANRegistryFixture.Record("Broken", [ "NL" ], EndpointStatus.Public, CEM: true, RM: false).ToJSON();

            json.Remove("name");

            return json;

        }

        #endregion


        // Construction

        #region Properties_ReflectTheConstruction()

        [Test]
        public async Task Properties_ReflectTheConstruction()
        {

            await using var fixture = await WANRegistryFixture.CreateAsync();

            var options = WANRegistryFixture.ClientOptions();

            await using var client = fixture.CreateClient(options);

            Assert.Multiple(() => {
                Assert.That(client.RegistryUrl,                        Is.EqualTo(fixture.RegistryUrl));
                Assert.That(client.RegistryUrl.Value,                  Is.EqualTo($"http://127.0.0.1:{fixture.Port}/registry/"));
                Assert.That(client.Options,                            Is.SameAs(options));
                Assert.That(client.Options.ParserOptions.AllowInsecureURLs, Is.True);
                Assert.That(client.SupportedAPIVersions,               Is.EqualTo(new[] { "v1" }));
                Assert.That(client.SelectedAPIVersion,                 Is.Null, "no request was sent yet");
                Assert.That(client.ServerAPIVersions,                  Is.Null, "no request was sent yet");
            });

            // The defaults of the options.
            var defaults = new WANRegistryClientOptions();

            Assert.Multiple(() => {
                Assert.That(defaults.SupportedAPIVersions,             Is.EqualTo(new[] { "v1" }));
                Assert.That(defaults.RequestTimeout,                   Is.EqualTo(TimeSpan.FromSeconds(10)));
                Assert.That(defaults.ParserOptions,                    Is.EqualTo(S2ParserOptions.Default));
                Assert.That(defaults.AcceptSelfSignedCertificates,     Is.False);
            });

        }

        #endregion

        #region Options_NonPositiveRequestTimeoutOrNoVersions_Throw()

        [Test]
        public void Options_NonPositiveRequestTimeoutOrNoVersions_Throw()
        {

            var url = S2BaseURL.Parse("http://127.0.0.1:1/registry/", AllowHTTP: true);

            Assert.That(() => new WANRegistryClient(url, new WANRegistryClientOptions { RequestTimeout = TimeSpan.Zero }),
                        Throws.ArgumentException);

            Assert.That(() => new WANRegistryClient(url, new WANRegistryClientOptions { RequestTimeout = TimeSpan.FromSeconds(-1) }),
                        Throws.ArgumentException);

            Assert.That(() => new WANRegistryClient(url, new WANRegistryClientOptions { SupportedAPIVersions = [] }),
                        Throws.ArgumentException);

        }

        #endregion


        // The version index (S2 Connect 1.0.0, "Selecting the version of REST APIs")

        #region GetVersions_SelectsV1()

        [Test]
        [S2C("Versioning.3")]
        [S2C("Registry.VersionIndex")]
        public async Task GetVersions_SelectsV1()
        {

            await using var fixture = await WANRegistryFixture.CreateAsync();

            var versions = await fixture.Client.GetVersionsAsync();

            Assert.Multiple(() => {
                Assert.That(versions.IsSuccess,                 Is.True, versions.ToString());
                Assert.That(versions.StatusCode.Code,           Is.EqualTo(200));
                Assert.That(versions.Value,                     Is.EqualTo(new[] { "v1" }));
                Assert.That(versions.IsTransportFailure,        Is.False);
                Assert.That(versions.NoCommonAPIVersion,        Is.False);
                Assert.That(fixture.Client.ServerAPIVersions,   Is.EqualTo(new[] { "v1" }));
                Assert.That(fixture.Client.SelectedAPIVersion,  Is.EqualTo("v1"));
            });

        }

        #endregion

        #region VersionIndex_WithoutV1_ReturnsNoCommonAPIVersion()

        [Test]
        [S2C("Versioning.2")]
        [S2C("Versioning.3")]
        public async Task VersionIndex_WithoutV1_ReturnsNoCommonAPIVersion()
        {

            await using var fake    = await FakeRegistry.StartAsync();
            await using var client  = new WANRegistryClient(fake.RegistryUrl, WANRegistryFixture.ClientOptions());

            fake.OnVersionIndex = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK, new JArray("v2")));

            var query   = await client.QueryEndpointsAsync();
            var single  = await client.GetEndpointAsync(EndpointRecord_Id.NewRandom);

            Assert.Multiple(() => {
                Assert.That(query.IsSuccess,                Is.False, query.ToString());
                Assert.That(query.NoCommonAPIVersion,       Is.True);
                Assert.That(query.IsTransportFailure,       Is.False, "the server answered, it is just incompatible");
                Assert.That(query.StatusCode.Code,          Is.EqualTo(200));
                Assert.That(query.Value,                    Is.Null);
                Assert.That(query.Description,              Does.Contain("v2").And.Contain("v1"));
                Assert.That(single.IsSuccess,               Is.False, single.ToString());
                Assert.That(single.NoCommonAPIVersion,      Is.True);
                Assert.That(single.Value,                   Is.Null);
                Assert.That(client.ServerAPIVersions,       Is.EqualTo(new[] { "v2" }));
                Assert.That(client.SelectedAPIVersion,      Is.Null);
                Assert.That(fake.EndpointRequests,          Is.EqualTo(0), "no operation of the registry API was called");
            });

        }

        #endregion

        #region VersionIndex_InvalidJSON_ReturnsFailure()

        [Test]
        [S2C("Versioning.3")]
        public async Task VersionIndex_InvalidJSON_ReturnsFailure()
        {

            await using var fake    = await FakeRegistry.StartAsync();
            await using var client  = new WANRegistryClient(fake.RegistryUrl, WANRegistryFixture.ClientOptions());

            fake.OnVersionIndex = request => Task.FromResult(TextResponse(request, HTTPStatusCode.OK, "this is not JSON"));

            var versions  = await client.GetVersionsAsync();
            var query     = await client.QueryEndpointsAsync();

            Assert.Multiple(() => {
                Assert.That(versions.IsSuccess,             Is.False, versions.ToString());
                Assert.That(versions.IsTransportFailure,    Is.False);
                Assert.That(versions.NoCommonAPIVersion,    Is.False);
                Assert.That(versions.StatusCode.Code,       Is.EqualTo(200));
                Assert.That(versions.Value,                 Is.Null);
                Assert.That(versions.Description,           Does.Contain("JSON"));
                Assert.That(query.IsSuccess,                Is.False, query.ToString());
                Assert.That(query.IsTransportFailure,       Is.False);
                Assert.That(query.NoCommonAPIVersion,       Is.False);
                Assert.That(query.Value,                    Is.Null);
                Assert.That(query.Description,              Does.Contain("JSON"));
                Assert.That(client.SelectedAPIVersion,      Is.Null);
                Assert.That(fake.EndpointRequests,          Is.EqualTo(0));
            });

            // A version index that is a JSON array of the wrong element type is rejected as well.
            fake.OnVersionIndex = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK, new JArray(1, 2)));

            var numbers = await client.GetVersionsAsync();

            Assert.Multiple(() => {
                Assert.That(numbers.IsSuccess,              Is.False, numbers.ToString());
                Assert.That(numbers.Description,            Does.Contain("version strings"));
                Assert.That(client.SelectedAPIVersion,      Is.Null);
            });

        }

        #endregion

        #region FollowingOperations_ReuseTheSelectedVersion()

        [Test]
        [S2C("Versioning.3")]
        public async Task FollowingOperations_ReuseTheSelectedVersion()
        {

            await using var fake    = await FakeRegistry.StartAsync();
            await using var client  = new WANRegistryClient(fake.RegistryUrl, WANRegistryFixture.ClientOptions());

            var first   = await client.QueryEndpointsAsync();
            var second  = await client.QueryEndpointsAsync(new WANRegistryQuery(CEM: true));
            var third   = await client.GetEndpointAsync(EndpointRecord_Id.NewRandom);

            Assert.Multiple(() => {
                Assert.That(first.IsSuccess,                Is.True, first.ToString());
                Assert.That(second.IsSuccess,               Is.True, second.ToString());
                Assert.That(third.StatusCode.Code,          Is.EqualTo(404), third.ToString());
                Assert.That(client.SelectedAPIVersion,      Is.EqualTo("v1"));
                Assert.That(fake.VersionIndexRequests,      Is.EqualTo(1), "the version index is fetched once");
                Assert.That(fake.EndpointRequests,          Is.EqualTo(3));
            });

            // An explicit GetVersionsAsync fetches the index again, but the operations still do not.
            var versions  = await client.GetVersionsAsync();
            var fourth    = await client.QueryEndpointsAsync();

            Assert.Multiple(() => {
                Assert.That(versions.IsSuccess,             Is.True, versions.ToString());
                Assert.That(fourth.IsSuccess,               Is.True, fourth.ToString());
                Assert.That(fake.VersionIndexRequests,      Is.EqualTo(2));
                Assert.That(fake.EndpointRequests,          Is.EqualTo(4));
            });

        }

        #endregion


        // GET /v1/endpoint against the reference registry

        #region QueryEndpoints_WithoutQuery_ReturnsAllPublicRecords()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_WithoutQuery_ReturnsAllPublicRecords()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var result = await fixture.Client.QueryEndpointsAsync();

            Assert.That(result.IsSuccess, Is.True, result.ToString());

            Assert.Multiple(() => {
                Assert.That(result.StatusCode.Code,         Is.EqualTo(200));
                Assert.That(result.IsTransportFailure,      Is.False);
                Assert.That(result.NoCommonAPIVersion,      Is.False);
                Assert.That(result.Description,             Is.Null);
                Assert.That(result.Value,                   Is.EqualTo(sample.Public), "the public records, in insertion order, equal by value");
                Assert.That(fixture.Client.SelectedAPIVersion, Is.EqualTo("v1"), "the version index was fetched implicitly");
            });

            // The explicit "all" query is the same as no query.
            var all = await fixture.Client.QueryEndpointsAsync(WANRegistryQuery.All);

            Assert.That(all.IsSuccess, Is.True, all.ToString());
            Assert.That(all.Value,     Is.EqualTo(sample.Public));

        }

        #endregion

        #region QueryEndpoints_RegionsAndCEM_FilterEndToEnd()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_RegionsAndCEM_FilterEndToEnd()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var benelux  = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(Regions: [ CountryCode.Parse("NL"), CountryCode.Parse("BE") ], CEM: true));
            var belgium  = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(Regions: [ CountryCode.Parse("BE") ]));
            var germany  = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(Regions: [ CountryCode.Parse("DE") ], CEM: true));
            var repeated = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(Regions: [ CountryCode.Parse("NL"), CountryCode.Parse("NL"), CountryCode.Parse("DE") ]));

            Assert.Multiple(() => {
                Assert.That(benelux.IsSuccess,   Is.True, benelux.ToString());
                Assert.That(benelux.Value,       Is.EqualTo(new[] { sample.Alpha, sample.Gamma }));
                Assert.That(belgium.IsSuccess,   Is.True, belgium.ToString());
                Assert.That(belgium.Value,       Is.EqualTo(new[] { sample.Gamma }));
                Assert.That(germany.IsSuccess,   Is.True, germany.ToString());
                Assert.That(germany.Value,       Is.Empty, "the German endpoint is no CEM");
                Assert.That(repeated.IsSuccess,  Is.True, repeated.ToString());
                Assert.That(repeated.Value,      Is.EqualTo(new[] { sample.Alpha, sample.Beta }), "duplicate regions are sent once");
            });

        }

        #endregion

        #region QueryEndpoints_StatusTesting_ReturnsTestingRecords()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_StatusTesting_ReturnsTestingRecords()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var testing  = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(Status: EndpointStatus.Testing));
            var isPublic = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(Status: EndpointStatus.Public));

            Assert.Multiple(() => {
                Assert.That(testing.IsSuccess,    Is.True, testing.ToString());
                Assert.That(testing.Value,        Is.EqualTo(new[] { sample.Delta }));
                Assert.That(isPublic.IsSuccess,   Is.True, isPublic.ToString());
                Assert.That(isPublic.Value,       Is.EqualTo(sample.Public));
            });

        }

        #endregion

        #region QueryEndpoints_RMFalse_ReturnsRecordsWithoutRM()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_RMFalse_ReturnsRecordsWithoutRM()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var noRMs  = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(RM: false));
            var rms    = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(RM: true));
            var both   = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(CEM: true, RM: true));

            Assert.Multiple(() => {
                Assert.That(noRMs.IsSuccess,  Is.True, noRMs.ToString());
                Assert.That(noRMs.Value,      Is.EqualTo(new[] { sample.Alpha }));
                Assert.That(rms.IsSuccess,    Is.True, rms.ToString());
                Assert.That(rms.Value,        Is.EqualTo(new[] { sample.Beta, sample.Gamma }));
                Assert.That(both.IsSuccess,   Is.True, both.ToString());
                Assert.That(both.Value,       Is.EqualTo(new[] { sample.Gamma }));
            });

        }

        #endregion

        #region QueryEndpoints_LimitAndOffset_PageTheResults()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_LimitAndOffset_PageTheResults()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var second   = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(Limit: 1, Offset: 1));
            var firstTwo = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(Limit: 2));
            var rest     = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(Offset: 2));
            var beyond   = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(Offset: 3));
            var filtered = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(CEM: true, Limit: 1, Offset: 1));

            Assert.Multiple(() => {
                Assert.That(second.IsSuccess,     Is.True, second.ToString());
                Assert.That(second.Value,         Is.EqualTo(new[] { sample.Beta }));
                Assert.That(firstTwo.IsSuccess,   Is.True, firstTwo.ToString());
                Assert.That(firstTwo.Value,       Is.EqualTo(new[] { sample.Alpha, sample.Beta }));
                Assert.That(rest.IsSuccess,       Is.True, rest.ToString());
                Assert.That(rest.Value,           Is.EqualTo(new[] { sample.Gamma }));
                Assert.That(beyond.IsSuccess,     Is.True, beyond.ToString());
                Assert.That(beyond.Value,         Is.Empty);
                Assert.That(filtered.IsSuccess,   Is.True, filtered.ToString());
                Assert.That(filtered.Value,       Is.EqualTo(new[] { sample.Gamma }), "paging applies after filtering");
            });

        }

        #endregion

        #region QueryEndpoints_EmptyRegistry_ReturnsAnEmptyList()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_EmptyRegistry_ReturnsAnEmptyList()
        {

            await using var fixture = await WANRegistryFixture.CreateAsync();

            var result = await fixture.Client.QueryEndpointsAsync();

            Assert.Multiple(() => {
                Assert.That(result.IsSuccess,         Is.True, result.ToString());
                Assert.That(result.StatusCode.Code,   Is.EqualTo(200));
                Assert.That(result.Value,             Is.Not.Null);
                Assert.That(result.Value,             Is.Empty);
            });

        }

        #endregion

        #region QueryEndpoints_SendsTheQueryParameters_TheServerParsesThemBack()

        [Test]
        [S2C("Registry.QueryEndpoints")]
        public async Task QueryEndpoints_SendsTheQueryParameters_TheServerParsesThemBack()
        {

            await using var fake    = await FakeRegistry.StartAsync();
            await using var client  = new WANRegistryClient(fake.RegistryUrl, WANRegistryFixture.ClientOptions());

            var query = new WANRegistryQuery(
                            Regions:  [ CountryCode.Parse("NL"), CountryCode.Parse("BE") ],
                            Status:   EndpointStatus.Testing,
                            CEM:      true,
                            RM:       false,
                            Limit:    5,
                            Offset:   2
                        );

            var filtered  = await client.QueryEndpointsAsync(query);
            var all       = await client.QueryEndpointsAsync();
            var minimal   = await client.QueryEndpointsAsync(new WANRegistryQuery(RM: true));

            Assert.Multiple(() => {
                Assert.That(filtered.IsSuccess,   Is.True, filtered.ToString());
                Assert.That(all.IsSuccess,        Is.True, all.ToString());
                Assert.That(minimal.IsSuccess,    Is.True, minimal.ToString());
                Assert.That(fake.Queries,         Has.Count.EqualTo(3));
            });

            var parsed = fake.Queries[0];

            Assert.That(parsed, Is.Not.Null, "the registry could parse the query parameters sent by the client");

            Assert.Multiple(() => {
                Assert.That(parsed,            Is.EqualTo(query));
                Assert.That(parsed!.Regions,   Is.EquivalentTo(new[] { CountryCode.Parse("NL"), CountryCode.Parse("BE") }));
                Assert.That(parsed.Status,     Is.EqualTo(EndpointStatus.Testing));
                Assert.That(parsed.CEM,        Is.True);
                Assert.That(parsed.RM,         Is.False);
                Assert.That(parsed.Limit,      Is.EqualTo(5));
                Assert.That(parsed.Offset,     Is.EqualTo(2));
                Assert.That(fake.Queries[1],   Is.EqualTo(WANRegistryQuery.All), "no query sends no parameters");
                Assert.That(fake.Queries[2],   Is.EqualTo(new WANRegistryQuery(RM: true)));
                Assert.That(fake.Queries[2]!.Regions, Is.Empty);
                Assert.That(fake.Queries[2]!.Status,  Is.Null);
                Assert.That(fake.Queries[2]!.CEM,     Is.Null);
                Assert.That(fake.Queries[2]!.Limit,   Is.Null);
                Assert.That(fake.Queries[2]!.Offset,  Is.Null);
            });

        }

        #endregion


        // GET /v1/endpoint/{id} against the reference registry

        #region GetEndpoint_KnownId_ReturnsTheRecord()

        [Test]
        [S2C("Registry.GetEndpoint")]
        public async Task GetEndpoint_KnownId_ReturnsTheRecord()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var beta   = await fixture.Client.GetEndpointAsync(sample.Beta.Id);
            var delta  = await fixture.Client.GetEndpointAsync(sample.Delta.Id);

            Assert.Multiple(() => {
                Assert.That(beta.IsSuccess,           Is.True, beta.ToString());
                Assert.That(beta.StatusCode.Code,     Is.EqualTo(200));
                Assert.That(beta.Value,               Is.EqualTo(sample.Beta));
                Assert.That(beta.Value?.Id,           Is.EqualTo(sample.Beta.Id));
                Assert.That(beta.Value?.Regions,      Is.EqualTo(new[] { CountryCode.Parse("DE") }));
                Assert.That(beta.Description,         Is.Null);
                Assert.That(delta.IsSuccess,          Is.True, delta.ToString());
                Assert.That(delta.Value,              Is.EqualTo(sample.Delta), "a testing record is served by its identification");
            });

        }

        #endregion

        #region GetEndpoint_UnknownId_ReturnsA404Failure()

        [Test]
        [S2C("Registry.GetEndpoint.404")]
        public async Task GetEndpoint_UnknownId_ReturnsA404Failure()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All);

            var unknown = EndpointRecord_Id.NewRandom;
            var result  = await fixture.Client.GetEndpointAsync(unknown);

            Assert.Multiple(() => {
                Assert.That(result.IsSuccess,             Is.False, result.ToString());
                Assert.That(result.StatusCode.Code,       Is.EqualTo(404));
                Assert.That(result.Value,                 Is.Null);
                Assert.That(result.IsTransportFailure,    Is.False);
                Assert.That(result.NoCommonAPIVersion,    Is.False);
                Assert.That(result.Description,           Does.Contain("no endpoint record"));
                Assert.That(result.Description,           Does.Contain(unknown.ToString()), "the message of the registry is part of the description");
            });

            // The client is still usable afterwards.
            var known = await fixture.Client.GetEndpointAsync(sample.Alpha.Id);

            Assert.That(known.IsSuccess, Is.True, known.ToString());
            Assert.That(known.Value,     Is.EqualTo(sample.Alpha));

        }

        #endregion


        // Wrong answers of a fake registry

        #region QueryEndpoints_RecordViolatingTheSchema_ReturnsFailure()

        [Test]
        [S2C("Registry.EndpointRecord")]
        public async Task QueryEndpoints_RecordViolatingTheSchema_ReturnsFailure()
        {

            await using var fake    = await FakeRegistry.StartAsync();
            await using var client  = new WANRegistryClient(fake.RegistryUrl, WANRegistryFixture.ClientOptions());

            fake.OnQueryEndpoints = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK, new JArray(RecordWithoutName())));

            var result = await client.QueryEndpointsAsync();

            Assert.Multiple(() => {
                Assert.That(result.IsSuccess,             Is.False, result.ToString());
                Assert.That(result.StatusCode.Code,       Is.EqualTo(200));
                Assert.That(result.Value,                 Is.Null);
                Assert.That(result.IsTransportFailure,    Is.False);
                Assert.That(result.NoCommonAPIVersion,    Is.False);
                Assert.That(result.Description,           Does.Contain("parsed"));
            });

            // A list element that is no JSON object at all.
            fake.OnQueryEndpoints = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK, new JArray("not a record")));

            var notAnObject = await client.QueryEndpointsAsync();

            Assert.Multiple(() => {
                Assert.That(notAnObject.IsSuccess,        Is.False, notAnObject.ToString());
                Assert.That(notAnObject.Value,            Is.Null);
                Assert.That(notAnObject.Description,      Does.Contain("parsed"));
            });

            // A valid record is accepted again.
            var valid = WANRegistryFixture.Record("Valid", [ "NL" ], EndpointStatus.Public, CEM: true, RM: false);

            fake.OnQueryEndpoints = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK, new JArray(valid.ToJSON())));

            var ok = await client.QueryEndpointsAsync();

            Assert.That(ok.IsSuccess, Is.True, ok.ToString());
            Assert.That(ok.Value,     Is.EqualTo(new[] { valid }));

        }

        #endregion

        #region GetEndpoint_RecordViolatingTheSchema_ReturnsFailure()

        [Test]
        [S2C("Registry.EndpointRecord")]
        public async Task GetEndpoint_RecordViolatingTheSchema_ReturnsFailure()
        {

            await using var fake    = await FakeRegistry.StartAsync();
            await using var client  = new WANRegistryClient(fake.RegistryUrl, WANRegistryFixture.ClientOptions());

            fake.OnGetEndpoint = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK, RecordWithoutName()));

            var result = await client.GetEndpointAsync(EndpointRecord_Id.NewRandom);

            Assert.Multiple(() => {
                Assert.That(result.IsSuccess,             Is.False, result.ToString());
                Assert.That(result.StatusCode.Code,       Is.EqualTo(200));
                Assert.That(result.Value,                 Is.Null);
                Assert.That(result.IsTransportFailure,    Is.False);
                Assert.That(result.Description,           Does.Contain("parsed").And.Contain("name"));
            });

            // A JSON array instead of the record object.
            fake.OnGetEndpoint = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK, new JArray()));

            var notAnObject = await client.GetEndpointAsync(EndpointRecord_Id.NewRandom);

            Assert.Multiple(() => {
                Assert.That(notAnObject.IsSuccess,        Is.False, notAnObject.ToString());
                Assert.That(notAnObject.Value,            Is.Null);
                Assert.That(notAnObject.Description,      Does.Contain("JSON object"));
            });

        }

        #endregion

        #region QueryEndpoints_NotAJSONArray_ReturnsFailure()

        [Test]
        public async Task QueryEndpoints_NotAJSONArray_ReturnsFailure()
        {

            await using var fake    = await FakeRegistry.StartAsync();
            await using var client  = new WANRegistryClient(fake.RegistryUrl, WANRegistryFixture.ClientOptions());

            fake.OnQueryEndpoints = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.OK, new JObject(new JProperty("endpoints", new JArray()))));

            var wrapped = await client.QueryEndpointsAsync();

            fake.OnQueryEndpoints = request => Task.FromResult(TextResponse(request, HTTPStatusCode.OK, "this is not JSON"));

            var text    = await client.QueryEndpointsAsync();

            Assert.Multiple(() => {
                Assert.That(wrapped.IsSuccess,      Is.False, wrapped.ToString());
                Assert.That(wrapped.Value,          Is.Null);
                Assert.That(wrapped.Description,    Does.Contain("JSON array"));
                Assert.That(text.IsSuccess,         Is.False, text.ToString());
                Assert.That(text.Value,             Is.Null);
                Assert.That(text.Description,       Does.Contain("JSON array"));
            });

        }

        #endregion

        #region QueryEndpoints_400FromTheServer_ReturnsFailureWithStatus400()

        [Test]
        [S2C("Registry.QueryEndpoints.400")]
        public async Task QueryEndpoints_400FromTheServer_ReturnsFailureWithStatus400()
        {

            await using var fake    = await FakeRegistry.StartAsync();
            await using var client  = new WANRegistryClient(fake.RegistryUrl, WANRegistryFixture.ClientOptions());

            fake.OnQueryEndpoints = request => Task.FromResult(JSONResponse(request, HTTPStatusCode.BadRequest, new JObject(new JProperty("message", "Invalid country code 'nl'!"))));

            var result = await client.QueryEndpointsAsync(new WANRegistryQuery(Regions: [ CountryCode.Parse("NL") ]));

            Assert.Multiple(() => {
                Assert.That(result.IsSuccess,             Is.False, result.ToString());
                Assert.That(result.StatusCode.Code,       Is.EqualTo(400));
                Assert.That(result.Value,                 Is.Null);
                Assert.That(result.IsTransportFailure,    Is.False);
                Assert.That(result.NoCommonAPIVersion,    Is.False);
                Assert.That(result.Description,           Does.Contain("invalid query parameters").And.Contain("Invalid country code 'nl'!"));
            });

        }

        #endregion

        #region QueryEndpoints_UnexpectedStatusCode_ReturnsFailure()

        [Test]
        public async Task QueryEndpoints_UnexpectedStatusCode_ReturnsFailure()
        {

            await using var fake    = await FakeRegistry.StartAsync();
            await using var client  = new WANRegistryClient(fake.RegistryUrl, WANRegistryFixture.ClientOptions());

            fake.OnQueryEndpoints  = request => Task.FromResult(TextResponse(request, HTTPStatusCode.InternalServerError, "boom"));
            fake.OnGetEndpoint     = request => Task.FromResult(TextResponse(request, HTTPStatusCode.InternalServerError, "boom"));

            var query   = await client.QueryEndpointsAsync();
            var single  = await client.GetEndpointAsync(EndpointRecord_Id.NewRandom);

            Assert.Multiple(() => {
                Assert.That(query.IsSuccess,              Is.False, query.ToString());
                Assert.That(query.StatusCode.Code,        Is.EqualTo(500));
                Assert.That(query.Value,                  Is.Null);
                Assert.That(query.IsTransportFailure,     Is.False);
                Assert.That(query.Description,            Does.Contain("500").And.Contain("boom"));
                Assert.That(single.IsSuccess,             Is.False, single.ToString());
                Assert.That(single.StatusCode.Code,       Is.EqualTo(500));
                Assert.That(single.Value,                 Is.Null);
                Assert.That(single.Description,           Does.Contain("500").And.Contain("boom"));
            });

        }

        #endregion


        // No server at all

        #region NoServerOnThePort_ReturnsTransportFailure()

        [Test]
        public async Task NoServerOnThePort_ReturnsTransportFailure()
        {

            // A port that was free a moment ago and has no listener: the connection is refused at once.
            var port = PairingServerFixture.FreePort();

            await using var client = new WANRegistryClient(
                                         S2BaseURL.Parse($"http://127.0.0.1:{port}/registry/", AllowHTTP: true),
                                         WANRegistryFixture.ClientOptions()
                                     );

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var versions  = await client.GetVersionsAsync(timeout.Token);
            var query     = await client.QueryEndpointsAsync(CancellationToken: timeout.Token);
            var single    = await client.GetEndpointAsync(EndpointRecord_Id.NewRandom, timeout.Token);

            Assert.Multiple(() => {
                Assert.That(versions.IsSuccess,             Is.False, versions.ToString());
                Assert.That(versions.IsTransportFailure,    Is.True);
                Assert.That(versions.NoCommonAPIVersion,    Is.False);
                Assert.That(versions.StatusCode.Code,       Is.EqualTo(0));
                Assert.That(versions.Value,                 Is.Null);
                Assert.That(versions.Description,           Is.Not.Null.And.Not.Empty);
                Assert.That(query.IsSuccess,                Is.False, query.ToString());
                Assert.That(query.IsTransportFailure,       Is.True);
                Assert.That(query.NoCommonAPIVersion,       Is.False);
                Assert.That(query.StatusCode.Code,          Is.EqualTo(0));
                Assert.That(query.Value,                    Is.Null);
                Assert.That(single.IsSuccess,               Is.False, single.ToString());
                Assert.That(single.IsTransportFailure,      Is.True);
                Assert.That(single.StatusCode.Code,         Is.EqualTo(0));
                Assert.That(single.Value,                   Is.Null);
                Assert.That(client.SelectedAPIVersion,      Is.Null);
                Assert.That(client.ServerAPIVersions,       Is.Null);
            });

        }

        #endregion


        // Root paths

        #region RootPathSlash_ClientWorksAtTheRoot()

        [Test]
        [S2C("Registry.VersionIndex")]
        public async Task RootPathSlash_ClientWorksAtTheRoot()
        {

            var sample = new SampleEndpointRecords();

            await using var fixture = await WANRegistryFixture.CreateAsync(sample.All, RootPath: "/");

            var versions  = await fixture.Client.GetVersionsAsync();
            var query     = await fixture.Client.QueryEndpointsAsync(new WANRegistryQuery(CEM: true));
            var single    = await fixture.Client.GetEndpointAsync(sample.Beta.Id);

            Assert.Multiple(() => {
                Assert.That(fixture.Client.RegistryUrl.Value,   Is.EqualTo($"http://127.0.0.1:{fixture.Port}/"));
                Assert.That(versions.IsSuccess,                 Is.True, versions.ToString());
                Assert.That(versions.Value,                     Is.EqualTo(new[] { "v1" }));
                Assert.That(query.IsSuccess,                    Is.True, query.ToString());
                Assert.That(query.Value,                        Is.EqualTo(new[] { sample.Alpha, sample.Gamma }));
                Assert.That(single.IsSuccess,                   Is.True, single.ToString());
                Assert.That(single.Value,                       Is.EqualTo(sample.Beta));
            });

        }

        #endregion

    }

}

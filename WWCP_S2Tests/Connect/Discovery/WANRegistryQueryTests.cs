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

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect.Discovery
{

    /// <summary>
    /// The query of the S2 Connect WAN endpoint registry (s2-connect-wan-endpoint-registry.yml,
    /// GET /v1/endpoint): constructor guards, the HTTP query string in both directions, the
    /// matching rules (regions any-of, status defaulting to public, CEM/RM flags), paging and
    /// equality.
    /// </summary>
    [TestFixture]
    public sealed class WANRegistryQueryTests
    {

        #region Data

        private static readonly CountryCode NL = CountryCode.Parse("NL");
        private static readonly CountryCode BE = CountryCode.Parse("BE");
        private static readonly CountryCode DE = CountryCode.Parse("DE");
        private static readonly CountryCode FR = CountryCode.Parse("FR");

        private static EndpointRecord Record(String          Name,
                                             String[]        Regions,
                                             EndpointStatus  Status,
                                             Boolean         CEM,
                                             Boolean         RM)
            => new (EndpointRecord_Id.NewRandom,
                    Name,
                    $"The {Name} endpoint",
                    URL.Parse("https://example.org/icon32.png"),
                    URL.Parse("https://example.org/icon128.png"),
                    URL.Parse("https://example.org/icon512.png"),
                    S2BaseURL.Parse($"https://{Name.ToLowerInvariant()}.example.org/pairing/"),
                    [.. Regions.Select(region => CountryCode.Parse(region))],
                    Status,
                    CEM,
                    RM);

        private static WANRegistryQuery Parse(String Text)
        {

            var success = WANRegistryQuery.TryParse(QueryString.Parse(Text), out var query, out var errorResponse);

            Assert.That(success, Is.True, errorResponse ?? "");
            Assert.That(query,   Is.Not.Null);

            return query!;

        }

        private static String? ParseError(String Text)
        {

            var success = WANRegistryQuery.TryParse(QueryString.Parse(Text), out var query, out var errorResponse);

            Assert.That(success,       Is.False);
            Assert.That(query,         Is.Null);
            Assert.That(errorResponse, Is.Not.Null.And.Not.Empty);

            return errorResponse;

        }

        /// <summary>
        /// The query string as text with any percent-encoding undone
        /// (the comma of "region=NL,BE" may or may not be encoded by the query string builder).
        /// </summary>
        private static String Decoded(WANRegistryQuery Query)
            => Uri.UnescapeDataString(Query.ToQueryString().ToString());

        #endregion


        #region Constructor and All

        [Test]
        [S2C("Registry.Query.Paging")]
        public void Constructor_RejectsInvalidLimitsOffsetsAndAnEmptyStatus()
        {
            Assert.Multiple(() => {
                Assert.That(() => new WANRegistryQuery(Limit:   0),                       Throws.ArgumentException);
                Assert.That(() => new WANRegistryQuery(Limit:  -5),                       Throws.ArgumentException);
                Assert.That(() => new WANRegistryQuery(Offset: -1),                       Throws.ArgumentException);
                Assert.That(() => new WANRegistryQuery(Status:  default(EndpointStatus)), Throws.ArgumentException);
                Assert.That(new WANRegistryQuery(Limit: 1, Offset: 0).Limit,              Is.EqualTo(1));
                Assert.That(new WANRegistryQuery(Limit: 1, Offset: 0).Offset,             Is.EqualTo(0));
            });
        }

        [Test]
        [S2C("Registry.Query.Region")]
        public void Constructor_DeduplicatesTheRegions()
        {

            var query = new WANRegistryQuery(Regions: [ NL, BE, NL, CountryCode.Parse("BE") ]);

            Assert.Multiple(() => {
                Assert.That(query.Regions, Has.Count.EqualTo(2));
                Assert.That(query.Regions, Is.EqualTo(new[] { NL, BE }), "the first occurrences keep their order");
            });

        }

        [Test]
        public void All_HasNoFilters()
        {
            Assert.Multiple(() => {
                Assert.That(WANRegistryQuery.All.Regions, Is.Empty);
                Assert.That(WANRegistryQuery.All.Status,  Is.Null);
                Assert.That(WANRegistryQuery.All.CEM,     Is.Null);
                Assert.That(WANRegistryQuery.All.RM,      Is.Null);
                Assert.That(WANRegistryQuery.All.Limit,   Is.Null);
                Assert.That(WANRegistryQuery.All.Offset,  Is.Null);
                Assert.That(WANRegistryQuery.All,         Is.EqualTo(new WANRegistryQuery()));
                Assert.That(WANRegistryQuery.All,         Is.SameAs(WANRegistryQuery.All));
            });
        }

        #endregion

        #region ToQueryString()

        [Test]
        [S2C("Registry.Query.Region")]
        public void ToQueryString_Region_IsACommaSeparatedList()
        {
            Assert.Multiple(() => {
                Assert.That(Decoded(new WANRegistryQuery(Regions: [ NL, BE ])),  Is.EqualTo("?region=NL,BE"));
                Assert.That(Decoded(new WANRegistryQuery(Regions: [ NL ])),      Is.EqualTo("?region=NL"));
                Assert.That(Decoded(new WANRegistryQuery(Regions: [])),          Is.Empty);
            });
        }

        [Test]
        [S2C("Registry.Query.Status")]
        public void ToQueryString_Status()
        {
            Assert.Multiple(() => {
                Assert.That(Decoded(new WANRegistryQuery(Status: EndpointStatus.Testing)), Is.EqualTo("?status=testing"));
                Assert.That(Decoded(new WANRegistryQuery(Status: EndpointStatus.Public)),  Is.EqualTo("?status=public"));
            });
        }

        [Test]
        [S2C("Registry.Query.CEM")]
        [S2C("Registry.Query.RM")]
        public void ToQueryString_CEMAndRM()
        {
            Assert.Multiple(() => {
                Assert.That(Decoded(new WANRegistryQuery(CEM: true)),  Is.EqualTo("?cem=true"));
                Assert.That(Decoded(new WANRegistryQuery(CEM: false)), Is.EqualTo("?cem=false"));
                Assert.That(Decoded(new WANRegistryQuery(RM:  true)),  Is.EqualTo("?rm=true"));
                Assert.That(Decoded(new WANRegistryQuery(RM:  false)), Is.EqualTo("?rm=false"));
            });
        }

        [Test]
        [S2C("Registry.Query.Paging")]
        public void ToQueryString_LimitAndOffset()
        {
            Assert.Multiple(() => {
                Assert.That(Decoded(new WANRegistryQuery(Limit:  10)), Is.EqualTo("?limit=10"));
                Assert.That(Decoded(new WANRegistryQuery(Offset:  5)), Is.EqualTo("?offset=5"));
            });
        }

        [Test]
        public void ToQueryString_OfAll_IsEmpty()
        {

            var queryString = WANRegistryQuery.All.ToQueryString();

            Assert.Multiple(() => {
                Assert.That(queryString.Any(),                            Is.False);
                Assert.That(queryString.ToString(),                       Is.Empty);
                Assert.That(new WANRegistryQuery().ToQueryString().Any(), Is.False);
            });

        }

        [Test]
        public void ToQueryString_ContainsEveryFilterInOrder()
        {

            var query        = new WANRegistryQuery([ NL, BE ], EndpointStatus.Testing, true, false, 10, 5);
            var queryString  = query.ToQueryString();

            Assert.Multiple(() => {
                Assert.That(Decoded(query),                       Is.EqualTo("?region=NL,BE&status=testing&cem=true&rm=false&limit=10&offset=5"));
                Assert.That(queryString.GetStrings("region"),     Is.EqualTo(new[] { "NL", "BE" }));
                Assert.That(queryString.GetString ("status"),     Is.EqualTo("testing"));
                Assert.That(queryString.GetString ("cem"),        Is.EqualTo("true"));
                Assert.That(queryString.GetString ("rm"),         Is.EqualTo("false"));
                Assert.That(queryString.GetString ("limit"),      Is.EqualTo("10"));
                Assert.That(queryString.GetString ("offset"),     Is.EqualTo("5"));
            });

        }

        [Test]
        public void ToQueryString_RoundTripsThroughTryParse()
        {

            var query = new WANRegistryQuery([ NL, BE ], EndpointStatus.Testing, true, false, 10, 5);

            Assert.That(WANRegistryQuery.TryParse(query.ToQueryString(), out var parsed, out var errorResponse), Is.True, errorResponse ?? "");
            Assert.That(parsed, Is.EqualTo(query));

            // ... also via the text representation, as sent over the wire.
            Assert.That(Parse(query.ToQueryString().ToString()), Is.EqualTo(query));

        }

        #endregion

        #region TryParse(QueryString)

        [Test]
        [S2C("Registry.Query")]
        public void TryParse_ReadsEveryParameter()
        {

            var query = Parse("?region=NL,BE&status=testing&cem=true&rm=false&limit=1&offset=2");

            Assert.Multiple(() => {
                Assert.That(query.Regions, Is.EqualTo(new[] { NL, BE }));
                Assert.That(query.Status,  Is.EqualTo(EndpointStatus.Testing));
                Assert.That(query.CEM,     Is.True);
                Assert.That(query.RM,      Is.False);
                Assert.That(query.Limit,   Is.EqualTo(1));
                Assert.That(query.Offset,  Is.EqualTo(2));
            });

            // The leading '?' is optional.
            Assert.That(Parse("region=DE&limit=3"), Is.EqualTo(new WANRegistryQuery(Regions: [ DE ], Limit: 3)));

        }

        [Test]
        [S2C("Registry.Query.Region")]
        public void TryParse_AcceptsTheRegionParameterMoreThanOnce()
        {
            Assert.Multiple(() => {
                Assert.That(Parse("?region=NL&region=BE").Regions,     Is.EqualTo(new[] { NL, BE }));
                Assert.That(Parse("?region=NL,BE&region=DE").Regions,  Is.EqualTo(new[] { NL, BE, DE }));
                Assert.That(Parse("?region=NL&region=NL").Regions,     Is.EqualTo(new[] { NL }), "duplicates are collapsed");
                Assert.That(Parse("?region=NL,,BE").Regions,           Is.EqualTo(new[] { NL, BE }), "empty entries are ignored");
            });
        }

        [Test]
        [S2C("Registry.Query.400")]
        public void TryParse_RejectsInvalidRegions()
        {
            Assert.Multiple(() => {
                Assert.That(ParseError("?region=nl"),    Does.Contain("nl"),  "lower case");
                Assert.That(ParseError("?region=NLD"),   Does.Contain("NLD"), "three letters");
                Assert.That(ParseError("?region=N1"),    Does.Contain("N1"),  "a digit");
                Assert.That(ParseError("?region=NL,be"), Does.Contain("be"),  "one invalid code spoils the list");
            });
        }

        [Test]
        [S2C("Registry.Query.Status")]
        [S2C("Registry.Query.400")]
        public void TryParse_RejectsUnknownStatuses()
        {
            Assert.Multiple(() => {
                Assert.That(ParseError("?status=private"),   Does.Contain("private"));
                Assert.That(ParseError("?status=Public"),    Does.Contain("Public"), "the enumeration is case-sensitive");
                Assert.That(ParseError("?status="),          Is.Not.Null);
                Assert.That(Parse("?status=public").Status,  Is.EqualTo(EndpointStatus.Public));
                Assert.That(Parse("?status=testing").Status, Is.EqualTo(EndpointStatus.Testing));
            });
        }

        [Test]
        [S2C("Registry.Query.CEM")]
        [S2C("Registry.Query.RM")]
        [S2C("Registry.Query.400")]
        public void TryParse_RejectsInvalidBooleans()
        {
            Assert.Multiple(() => {
                Assert.That(ParseError("?cem=yes"),             Does.Contain("yes").And.Contain("cem"));
                Assert.That(ParseError("?rm=1"),                Does.Contain("rm"));
                Assert.That(ParseError("?cem=true&rm=maybe"),   Does.Contain("rm"));
                Assert.That(Parse("?cem=TRUE&rm=False").CEM,    Is.True,  "the booleans are case-insensitive");
                Assert.That(Parse("?cem=TRUE&rm=False").RM,     Is.False);
            });
        }

        [Test]
        [S2C("Registry.Query.Paging")]
        [S2C("Registry.Query.400")]
        public void TryParse_RejectsInvalidLimitsAndOffsets()
        {
            Assert.Multiple(() => {
                Assert.That(ParseError("?limit=0"),     Does.Contain("limit"));
                Assert.That(ParseError("?limit=-1"),    Does.Contain("limit"));
                Assert.That(ParseError("?limit=abc"),   Does.Contain("abc"));
                Assert.That(ParseError("?limit=1.5"),   Does.Contain("limit"));
                Assert.That(ParseError("?offset=-1"),   Does.Contain("offset"));
                Assert.That(ParseError("?offset=abc"),  Does.Contain("offset"));
                Assert.That(Parse("?offset=0").Offset,  Is.EqualTo(0));
                Assert.That(Parse("?limit=1").Limit,    Is.EqualTo(1));
            });
        }

        [Test]
        public void TryParse_AnEmptyQueryString_IsAll()
        {

            Assert.Multiple(() => {
                Assert.That(Parse(""),         Is.EqualTo(WANRegistryQuery.All));
                Assert.That(Parse("?"),        Is.EqualTo(WANRegistryQuery.All));
                Assert.That(Parse("?foo=bar"), Is.EqualTo(WANRegistryQuery.All), "unknown parameters are ignored");
            });

            Assert.That(WANRegistryQuery.TryParse(QueryString.Empty, out var query, out var errorResponse), Is.True, errorResponse ?? "");
            Assert.That(query, Is.EqualTo(WANRegistryQuery.All));

        }

        #endregion

        #region Matches(Record) and Apply(Records)

        [Test]
        [S2C("Registry.Query.Status")]
        public void Matches_DefaultsToPublicEndpoints()
        {

            var publicRecord   = Record("Alpha", [ "NL" ], EndpointStatus.Public,  true, false);
            var testingRecord  = Record("Beta",  [ "NL" ], EndpointStatus.Testing, true, false);

            Assert.Multiple(() => {
                Assert.That(WANRegistryQuery.All.Matches(publicRecord),                                   Is.True);
                Assert.That(WANRegistryQuery.All.Matches(testingRecord),                                  Is.False);
                Assert.That(new WANRegistryQuery(Status: EndpointStatus.Public). Matches(publicRecord),   Is.True);
                Assert.That(new WANRegistryQuery(Status: EndpointStatus.Public). Matches(testingRecord),  Is.False);
                Assert.That(new WANRegistryQuery(Status: EndpointStatus.Testing).Matches(testingRecord),  Is.True);
                Assert.That(new WANRegistryQuery(Status: EndpointStatus.Testing).Matches(publicRecord),   Is.False);
            });

        }

        [Test]
        [S2C("Registry.Query.Region")]
        public void Matches_RegionsAreAnyOf()
        {

            var record = Record("Alpha", [ "NL", "DE" ], EndpointStatus.Public, true, true);

            Assert.Multiple(() => {
                Assert.That(new WANRegistryQuery(Regions: [ DE, BE ]).Matches(record),  Is.True,  "DE is shared");
                Assert.That(new WANRegistryQuery(Regions: [ NL ]).    Matches(record),  Is.True);
                Assert.That(new WANRegistryQuery(Regions: [ NL, DE ]).Matches(record),  Is.True);
                Assert.That(new WANRegistryQuery(Regions: [ BE, FR ]).Matches(record),  Is.False, "nothing shared");
                Assert.That(new WANRegistryQuery(Regions: []).        Matches(record),  Is.True,  "no region filter");
            });

        }

        [Test]
        [S2C("Registry.Query.CEM")]
        [S2C("Registry.Query.RM")]
        public void Matches_CEMAndRMFlags()
        {

            var cemOnly  = Record("Alpha", [ "NL" ], EndpointStatus.Public, true, false);
            var both     = Record("Beta",  [ "NL" ], EndpointStatus.Public, true, true);

            Assert.Multiple(() => {
                Assert.That(new WANRegistryQuery(CEM: true). Matches(cemOnly),            Is.True);
                Assert.That(new WANRegistryQuery(CEM: false).Matches(cemOnly),            Is.False);
                Assert.That(new WANRegistryQuery(RM:  true). Matches(cemOnly),            Is.False);
                Assert.That(new WANRegistryQuery(RM:  false).Matches(cemOnly),            Is.True);
                Assert.That(new WANRegistryQuery(CEM: true, RM: true). Matches(cemOnly),  Is.False);
                Assert.That(new WANRegistryQuery(CEM: true, RM: true). Matches(both),     Is.True);
                Assert.That(new WANRegistryQuery(CEM: true, RM: false).Matches(both),     Is.False);
                Assert.That(WANRegistryQuery.All.Matches(both),                           Is.True);
            });

        }

        [Test]
        public void Matches_IgnoresPaging()
        {

            var record = Record("Alpha", [ "NL" ], EndpointStatus.Public, true, false);

            Assert.That(new WANRegistryQuery(Limit: 1, Offset: 100).Matches(record), Is.True);

        }

        [Test]
        [S2C("Registry.Query.Paging")]
        public void Apply_FiltersAndPagesKeepingTheOrder()
        {

            var alpha    = Record("Alpha",   [ "NL" ],       EndpointStatus.Public,  true,  false);
            var beta     = Record("Beta",    [ "DE" ],       EndpointStatus.Public,  false, true);
            var gamma    = Record("Gamma",   [ "NL" ],       EndpointStatus.Testing, true,  true);
            var delta    = Record("Delta",   [ "BE", "NL" ], EndpointStatus.Public,  true,  true);
            var epsilon  = Record("Epsilon", [ "NL" ],       EndpointStatus.Public,  false, true);

            var records  = new[] { alpha, beta, gamma, delta, epsilon };

            Assert.Multiple(() => {
                Assert.That(WANRegistryQuery.All.Apply(records),                                        Is.EqualTo(new[] { alpha, beta, delta, epsilon }));
                Assert.That(new WANRegistryQuery(Offset: 1).Apply(records),                             Is.EqualTo(new[] { beta, delta, epsilon }));
                Assert.That(new WANRegistryQuery(Limit: 2).Apply(records),                              Is.EqualTo(new[] { alpha, beta }));
                Assert.That(new WANRegistryQuery(Limit: 2, Offset: 1).Apply(records),                   Is.EqualTo(new[] { beta, delta }));
                Assert.That(new WANRegistryQuery(Offset: 10).Apply(records),                            Is.Empty);
                Assert.That(new WANRegistryQuery(Regions: [ NL ], RM: true).Apply(records),             Is.EqualTo(new[] { delta, epsilon }));
                Assert.That(new WANRegistryQuery(Regions: [ NL ], RM: true, Limit: 1).Apply(records),   Is.EqualTo(new[] { delta }));
                Assert.That(new WANRegistryQuery(Status: EndpointStatus.Testing).Apply(records),        Is.EqualTo(new[] { gamma }));
                Assert.That(new WANRegistryQuery(Regions: [ FR ]).Apply(records),                       Is.Empty);
                Assert.That(WANRegistryQuery.All.Apply([]),                                             Is.Empty);
            });

        }

        #endregion

        #region Equality and ToString()

        [Test]
        public void Equality_ComparesTheFiltersWithTheRegionsAsASet()
        {

            var a = new WANRegistryQuery([ NL, BE ], EndpointStatus.Testing, true, false, 10, 5);
            var b = new WANRegistryQuery([ NL, BE ], EndpointStatus.Testing, true, false, 10, 5);
            var c = new WANRegistryQuery([ BE, NL ], EndpointStatus.Testing, true, false, 10, 5);

            Assert.Multiple(() => {
                Assert.That(a,                     Is.EqualTo(b));
                Assert.That(a.GetHashCode(),       Is.EqualTo(b.GetHashCode()));
                Assert.That(a,                     Is.EqualTo(c), "the order of the regions does not matter");
                Assert.That(a == b,                Is.True);
                Assert.That(a != b,                Is.False);
                Assert.That(a == c,                Is.True);
                Assert.That(a.Equals((Object) b),  Is.True);
                Assert.That(a.Equals("query"),     Is.False);
            });

            Assert.Multiple(() => {
                Assert.That(a, Is.Not.EqualTo(new WANRegistryQuery([ NL ],     EndpointStatus.Testing, true, false, 10, 5)), "different regions");
                Assert.That(a, Is.Not.EqualTo(new WANRegistryQuery([ NL, BE ], EndpointStatus.Public,  true, false, 10, 5)), "different status");
                Assert.That(a, Is.Not.EqualTo(new WANRegistryQuery([ NL, BE ], null,                   true, false, 10, 5)), "no status");
                Assert.That(a, Is.Not.EqualTo(new WANRegistryQuery([ NL, BE ], EndpointStatus.Testing, null, false, 10, 5)), "no CEM flag");
                Assert.That(a, Is.Not.EqualTo(new WANRegistryQuery([ NL, BE ], EndpointStatus.Testing, true, true,  10, 5)), "different RM flag");
                Assert.That(a, Is.Not.EqualTo(new WANRegistryQuery([ NL, BE ], EndpointStatus.Testing, true, false, 11, 5)), "different limit");
                Assert.That(a, Is.Not.EqualTo(new WANRegistryQuery([ NL, BE ], EndpointStatus.Testing, true, false, 10, 6)), "different offset");
                Assert.That(a, Is.Not.EqualTo(WANRegistryQuery.All));
                Assert.That(a != WANRegistryQuery.All, Is.True);
            });

            WANRegistryQuery? none = null;

            Assert.Multiple(() => {
                Assert.That(a == none,       Is.False);
                Assert.That(none == a,       Is.False);
                Assert.That(a != none,       Is.True);
                Assert.That(none == null,    Is.True);
                Assert.That(a.Equals(none),  Is.False);
            });

        }

        [Test]
        public void ToString_DescribesTheQuery()
        {
            Assert.Multiple(() => {
                Assert.That(WANRegistryQuery.All.ToString(),                                  Is.EqualTo("(all public endpoints)"));
                Assert.That(new WANRegistryQuery().ToString(),                                Is.EqualTo("(all public endpoints)"));
                Assert.That(new WANRegistryQuery(Status: EndpointStatus.Testing).ToString(),  Is.EqualTo("?status=testing"));
                Assert.That(new WANRegistryQuery(CEM: true, Limit: 3).ToString(),             Is.EqualTo(new WANRegistryQuery(CEM: true, Limit: 3).ToQueryString().ToString()));
                Assert.That(Uri.UnescapeDataString(new WANRegistryQuery(Regions: [ NL, BE ]).ToString()), Is.EqualTo("?region=NL,BE"));
            });
        }

        #endregion

    }

}

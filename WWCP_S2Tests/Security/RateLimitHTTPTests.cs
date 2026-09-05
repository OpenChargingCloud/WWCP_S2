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

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Tests.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// The enforcement of the per-source request budget of the pairing server and of the
    /// session initiation server over real HTTP (PLAN.md §11a): every route is limited, the
    /// budget is shared by the routes of one server, the refusal happens before any
    /// authentication and parsing, and it carries a Retry-After header but no secret.
    ///
    /// <para>
    /// The tests use a tiny capacity and a long refill period, so that no wall-clock timing
    /// can influence the outcome: within one test the budget never refills.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class RateLimitHTTPTests
    {

        #region Data

        /// <summary>
        /// A refill period long enough that no test ever observes a refill.
        /// </summary>
        private static readonly TimeSpan  NoRefill  = TimeSpan.FromMinutes(10);

        #endregion

        #region (private static) RequestAsync(Fixture, Method, Path)

        /// <summary>
        /// Send a request without a body to a route of the pairing server.
        /// </summary>
        private static Task<HTTPResult> RequestAsync(PairingServerFixture  Fixture,
                                                     String                Method,
                                                     String                Path)

            => Method == "GET"
                   ? Fixture.GetAsync (Path)
                   : Fixture.PostAsync(Path);

        /// <summary>
        /// Send a request without a body to a route of the session initiation server.
        /// </summary>
        private static Task<HTTPResult> RequestAsync(SessionInitiationFixture  Fixture,
                                                     String                    Method,
                                                     String                    Path)

            => Method == "GET"
                   ? Fixture.GetAsync (Path)
                   : Fixture.PostAsync(Path);

        #endregion

        #region (private static) LimitedPairingOptions / LimitedSessionOptions(Capacity, ...)

        /// <summary>
        /// Pairing server options with a tiny request budget that does not refill during a test.
        /// </summary>
        private static PairingServerOptions LimitedPairingOptions(Int32    Capacity,
                                                                  Int32?   MaxSources        = null,
                                                                  Boolean  TooManyRequests   = false,
                                                                  Boolean  Enabled           = true)

            => PairingServerFixture.DefaultOptions() with {
                   EnableRateLimiting            = Enabled,
                   RateLimitCapacity             = Capacity,
                   RateLimitRefillPeriod         = NoRefill,
                   RateLimitMaxSources           = MaxSources ?? S2RequestRateLimiter.DefaultMaximumBuckets,
                   UseTooManyRequestsStatusCode  = TooManyRequests
               };

        /// <summary>
        /// Session initiation server options with a tiny request budget that does not refill
        /// during a test.
        /// </summary>
        private static SessionInitiationServerOptions LimitedSessionOptions(Int32    Capacity,
                                                                            Boolean  TooManyRequests   = false,
                                                                            Boolean  Enabled           = true)

            => SessionInitiationFixture.DefaultServerOptions() with {
                   EnableRateLimiting            = Enabled,
                   RateLimitCapacity             = Capacity,
                   RateLimitRefillPeriod         = NoRefill,
                   UseTooManyRequestsStatusCode  = TooManyRequests
               };

        #endregion


        // Every route of both servers is limited

        #region Pairing_RateLimit_AppliesToEveryRoute(Method, Path)

        [Test]
        [TestCase("GET",  "",                            TestName = "Rate limit: pairing GET version index")]
        [TestCase("POST", "v1/requestPairing",           TestName = "Rate limit: pairing POST requestPairing")]
        [TestCase("POST", "v1/requestConnectionDetails", TestName = "Rate limit: pairing POST requestConnectionDetails")]
        [TestCase("POST", "v1/postConnectionDetails",    TestName = "Rate limit: pairing POST postConnectionDetails")]
        [TestCase("POST", "v1/finalizePairing",          TestName = "Rate limit: pairing POST finalizePairing")]
        [TestCase("GET",  "v1/endpoint",                 TestName = "Rate limit: pairing GET endpoint")]
        [TestCase("GET",  "v1/nodes",                    TestName = "Rate limit: pairing GET nodes")]
        [TestCase("POST", "v1/preparePairing",           TestName = "Rate limit: pairing POST preparePairing")]
        [TestCase("POST", "v1/cancelPreparePairing",     TestName = "Rate limit: pairing POST cancelPreparePairing")]
        [TestCase("POST", "v1/waitForPairing",           TestName = "Rate limit: pairing POST waitForPairing")]
        public async Task Pairing_RateLimit_AppliesToEveryRoute(String  Method,
                                                                String  Path)
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: LimitedPairingOptions(Capacity: 1)
                                            );

            var first    = await RequestAsync(fixture, Method, Path);
            var refused  = await RequestAsync(fixture, Method, Path);

            Assert.Multiple(() => {
                Assert.That(first.Status,        Is.Not.EqualTo(HttpStatusCode.ServiceUnavailable), $"the first request of '{Path}' is within the budget");
                Assert.That(refused.Status,      Is.EqualTo    (HttpStatusCode.ServiceUnavailable), $"the second request of '{Path}' exceeds the budget");
                Assert.That(refused.RetryAfter,  Is.Not.Null);
            });

        }

        #endregion

        #region SessionInitiation_RateLimit_AppliesToEveryRoute(Method, Path)

        [Test]
        [TestCase("GET",  "",                       TestName = "Rate limit: session initiation GET version index")]
        [TestCase("POST", "v1/initiateSession",     TestName = "Rate limit: session initiation POST initiateSession")]
        [TestCase("POST", "v1/confirmAccessToken",  TestName = "Rate limit: session initiation POST confirmAccessToken")]
        [TestCase("POST", "v1/unpair",              TestName = "Rate limit: session initiation POST unpair")]
        public async Task SessionInitiation_RateLimit_AppliesToEveryRoute(String  Method,
                                                                          String  Path)
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                                ServerOptions: LimitedSessionOptions(Capacity: 1)
                                            );

            var first    = await RequestAsync(fixture, Method, Path);
            var refused  = await RequestAsync(fixture, Method, Path);

            Assert.Multiple(() => {
                Assert.That(first.Status,        Is.Not.EqualTo(HttpStatusCode.ServiceUnavailable), $"the first request of '{Path}' is within the budget");
                Assert.That(refused.Status,      Is.EqualTo    (HttpStatusCode.ServiceUnavailable), $"the second request of '{Path}' exceeds the budget");
                Assert.That(refused.RetryAfter,  Is.Not.Null);
            });

        }

        #endregion


        // One budget per server, not per route

        #region Pairing_RateLimit_BudgetIsSharedByTheRoutes()

        [Test]
        public async Task Pairing_RateLimit_BudgetIsSharedByTheRoutes()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: LimitedPairingOptions(Capacity: 3)
                                            );

            // The budget is spent on the cheapest route ...
            var index = new List<HttpStatusCode>();

            for (var i = 0; i < 3; i++)
                index.Add((await fixture.GetAsync("")).Status);

            // ... and every other route of the same server is refused afterwards.
            var post      = await fixture.PostAsync("v1/requestPairing");
            var lanGet    = await fixture.GetAsync ("v1/nodes");
            var stillOut  = await fixture.GetAsync ("");

            Assert.Multiple(() => {
                Assert.That(index,             Is.All.EqualTo(HttpStatusCode.OK));
                Assert.That(post.Status,       Is.EqualTo(HttpStatusCode.ServiceUnavailable), "the budget is shared with the POST routes");
                Assert.That(lanGet.Status,     Is.EqualTo(HttpStatusCode.ServiceUnavailable), "and with the LAN-only routes");
                Assert.That(stillOut.Status,   Is.EqualTo(HttpStatusCode.ServiceUnavailable), "the budget does not refill within the test");
            });

        }

        #endregion

        #region SessionInitiation_RateLimit_BudgetIsSharedByTheRoutes()

        [Test]
        public async Task SessionInitiation_RateLimit_BudgetIsSharedByTheRoutes()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                                ServerOptions: LimitedSessionOptions(Capacity: 2)
                                            );

            var firstIndex   = await fixture.GetAsync("");
            var secondIndex  = await fixture.GetAsync("");

            var initiate     = await fixture.PostAsync("v1/initiateSession",
                                                       Bearer:  TokenGenerator.NewAccessToken().Value);

            var confirm      = await fixture.PostAsync("v1/confirmAccessToken",
                                                       Bearer:  TokenGenerator.NewAccessToken().Value);

            Assert.Multiple(() => {
                Assert.That(firstIndex.Status,   Is.EqualTo(HttpStatusCode.OK));
                Assert.That(secondIndex.Status,  Is.EqualTo(HttpStatusCode.OK));
                Assert.That(initiate.Status,     Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(confirm.Status,      Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            });

        }

        #endregion


        // The refusal precedes everything the request could cost

        #region Pairing_RateLimit_RefusesBeforeAuthenticationAndParsing()

        [Test]
        public async Task Pairing_RateLimit_RefusesBeforeAuthenticationAndParsing()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: LimitedPairingOptions(Capacity: 1)
                                            );

            Assert.That((await fixture.GetAsync("")).Status, Is.EqualTo(HttpStatusCode.OK));

            // No bearer token (would be 401) and a body that is neither JSON nor an S2 request
            // (would be 400): the flood is refused before any of that is looked at.
            var refused = await fixture.PostAsync("v1/finalizePairing",
                                                  RawBody: "{ definitely not json ");

            Assert.Multiple(() => {
                Assert.That(refused.Status,           Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(refused.Status,           Is.Not.EqualTo(HttpStatusCode.Unauthorized), "the rate limit precedes the authentication");
                Assert.That(refused.Status,           Is.Not.EqualTo(HttpStatusCode.BadRequest),   "the rate limit precedes the parsing");
                Assert.That(refused.WWWAuthenticate,  Is.Null);
                Assert.That(refused.ErrorMessage,     Is.Null,  "a refusal is no S2 pairing error");
                Assert.That(refused.RetryAfter,       Is.Not.Null);
            });

        }

        #endregion

        #region SessionInitiation_RateLimit_RefusesBeforeAuthenticationAndParsing()

        [Test]
        public async Task SessionInitiation_RateLimit_RefusesBeforeAuthenticationAndParsing()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                                ServerOptions: LimitedSessionOptions(Capacity: 1)
                                            );

            Assert.That((await fixture.GetAsync("")).Status, Is.EqualTo(HttpStatusCode.OK));

            var refused = await fixture.PostAsync("v1/unpair",
                                                  RawBody: "{ definitely not json ");

            Assert.Multiple(() => {
                Assert.That(refused.Status,           Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(refused.Status,           Is.Not.EqualTo(HttpStatusCode.Unauthorized), "the rate limit precedes the authentication");
                Assert.That(refused.Status,           Is.Not.EqualTo(HttpStatusCode.BadRequest),   "the rate limit precedes the parsing");
                Assert.That(refused.WWWAuthenticate,  Is.Null);
                Assert.That(refused.ErrorMessage,     Is.Null);
                Assert.That(refused.RetryAfter,       Is.Not.Null);
            });

        }

        #endregion


        // The shape of a refusal

        #region Pairing_RateLimit_RetryAfterIsAPositiveNumberOfSeconds()

        [Test]
        public async Task Pairing_RateLimit_RetryAfterIsAPositiveNumberOfSeconds()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: LimitedPairingOptions(Capacity: 1)
                                            );

            await fixture.GetAsync("");

            var refused = await fixture.GetAsync("");

            Assert.That(refused.RetryAfter, Is.Not.Null);

            var isNumeric = Int32.TryParse(refused.RetryAfter, out var seconds);

            Assert.Multiple(() => {
                Assert.That(isNumeric,  Is.True, $"'{refused.RetryAfter}' is a delta-seconds value, not an HTTP date");
                // At most the refill period of an empty bucket (rounded up to whole seconds).
                Assert.That(seconds,    Is.InRange(1, (Int32) NoRefill.TotalSeconds + 1));
            });

        }

        #endregion

        #region SessionInitiation_RateLimit_TooManyRequestsStatusCode_Returns429()

        [Test]
        public async Task SessionInitiation_RateLimit_TooManyRequestsStatusCode_Returns429()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                                ServerOptions: LimitedSessionOptions(Capacity:         1,
                                                                                     TooManyRequests:  true)
                                            );

            var first    = await fixture.GetAsync("");
            var refused  = await fixture.GetAsync("");
            var onPost   = await fixture.PostAsync("v1/initiateSession");

            Assert.Multiple(() => {
                Assert.That(first.Status,        Is.EqualTo(HttpStatusCode.OK));
                Assert.That(refused.Status,      Is.EqualTo(HttpStatusCode.TooManyRequests));
                Assert.That(refused.RetryAfter,  Is.Not.Null);
                Assert.That(onPost.Status,       Is.EqualTo(HttpStatusCode.TooManyRequests), "the opt-in status code is used on every route");
            });

        }

        #endregion

        #region Pairing_RateLimit_RefusalCarriesNoSecret_AndIsNotCacheable()

        [Test]
        public async Task Pairing_RateLimit_RefusalCarriesNoSecret_AndIsNotCacheable()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: LimitedPairingOptions(Capacity: 1)
                                            );

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            // The one allowed request creates a pairing attempt and answers with its secret.
            var ok = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            Assert.That(ok.Status, Is.EqualTo(HttpStatusCode.OK), ok.Body);
            Assert.That(RequestPairingResponse.TryParse(ok.Object, out var response, out var error), Is.True, error);

            var secret   = response!.PairingAttemptId.Value;

            var refused  = await fixture.GetAsync("");

            Assert.Multiple(() => {

                Assert.That(ok.Body,                 Does.Contain(secret), "the allowed response does carry the secret");

                Assert.That(refused.Status,          Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(refused.Body ?? "",      Is.Empty,             "a refusal has no body at all");
                Assert.That(refused.Body ?? "",      Does.Not.Contain(secret));
                Assert.That(refused.CacheControl,    Does.Contain("no-store"), "even a refusal must never be cached");

            });

        }

        #endregion


        // The configuration

        #region RateLimit_Disabled_ServesEveryRouteOfBothServers()

        [Test]
        public async Task RateLimit_Disabled_ServesEveryRouteOfBothServers()
        {

            // A capacity of one would refuse everything after the first request; the disabled
            // limiter has to win over it (and its parameters are not validated any more).
            await using var pairing = await PairingServerFixture.CreateAsync(
                                                Options: LimitedPairingOptions(Capacity:  1,
                                                                               Enabled:   false)
                                            );

            await using var session = await SessionInitiationFixture.CreateAsync(
                                                ServerOptions: LimitedSessionOptions(Capacity:  1,
                                                                                     Enabled:   false)
                                            );

            var pairingIndex  = new List<HttpStatusCode>();
            var sessionIndex  = new List<HttpStatusCode>();

            for (var i = 0; i < 10; i++)
            {
                pairingIndex.Add((await pairing.GetAsync("")).Status);
                sessionIndex.Add((await session.GetAsync("")).Status);
            }

            var nodes         = await pairing.GetAsync ("v1/nodes");
            var requestPair   = await pairing.PostAsync("v1/requestPairing");
            var initiate      = await session.PostAsync("v1/initiateSession");

            Assert.Multiple(() => {

                Assert.That(pairingIndex,       Is.All.EqualTo(HttpStatusCode.OK));
                Assert.That(sessionIndex,       Is.All.EqualTo(HttpStatusCode.OK));

                Assert.That(nodes.Status,       Is.EqualTo(HttpStatusCode.OK));

                // The operations answer their own errors instead of a refusal.
                Assert.That(requestPair.Status, Is.EqualTo(HttpStatusCode.BadRequest),   "an empty body is a parsing error, not a refusal");
                Assert.That(initiate.Status,    Is.EqualTo(HttpStatusCode.Unauthorized), "a missing access token is a 401, not a refusal");

            });

        }

        #endregion

        #region Pairing_RateLimit_WithASingleRememberedSource_StillServesTheOnlyClient()

        [Test]
        public async Task Pairing_RateLimit_WithASingleRememberedSource_StillServesTheOnlyClient()
        {

            // The bound on the number of remembered addresses must not cost the only client
            // its budget: it owns the single bucket.
            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: LimitedPairingOptions(Capacity:    3,
                                                                               MaxSources:  1)
                                            );

            var allowed = new List<HttpStatusCode>();

            for (var i = 0; i < 3; i++)
                allowed.Add((await fixture.GetAsync("")).Status);

            var refused = await fixture.GetAsync("");

            Assert.Multiple(() => {
                Assert.That(allowed,             Is.All.EqualTo(HttpStatusCode.OK));
                Assert.That(refused.Status,      Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(refused.RetryAfter,  Is.Not.Null);
            });

        }

        #endregion

    }

}

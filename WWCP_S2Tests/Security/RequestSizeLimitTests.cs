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
    /// The request body size limit of the pairing server and of the session initiation server
    /// over real HTTP (PLAN.md §11a): a body beyond the limit is answered "413 Request Entity
    /// Too Large" with the S2 error shape and without being parsed, a body at the limit is
    /// served, and the options reject an unusable limit.
    /// </summary>
    [TestFixture]
    public sealed class RequestSizeLimitTests
    {

        #region Data

        /// <summary>
        /// The request body size limit of the tests, small enough to be exceeded by hand.
        /// </summary>
        private const Int32  Limit         = 512;

        /// <summary>
        /// A limit large enough for a complete requestPairing request, so that a pairing
        /// attempt can be started before the oversized bodies are sent.
        /// </summary>
        private const Int32  AttemptLimit  = 4096;

        #endregion

        #region (private static) BodyOfExactly(Bytes)

        /// <summary>
        /// A well-formed JSON object of exactly the given number of UTF-8 bytes (ASCII only,
        /// so that one character is one byte).
        /// </summary>
        /// <param name="Bytes">The wanted size of the request body.</param>
        private static String BodyOfExactly(Int32 Bytes)
        {

            const String prefix = "{\"clientNodeId\":\"";
            const String suffix = "\"}";

            return String.Concat(
                       prefix,
                       new String('x', Bytes - prefix.Length - suffix.Length),
                       suffix
                   );

        }

        #endregion

        #region (private static) SizeLimitedOptions(MaxRequestBodySize, IncludeErrorDetails = true)

        private static PairingServerOptions SizeLimitedPairingOptions(Int32    MaxRequestBodySize,
                                                                      Boolean  IncludeErrorDetails   = true)

            => PairingServerFixture.DefaultOptions() with {
                   MaxRequestBodySize   = MaxRequestBodySize,
                   IncludeErrorDetails  = IncludeErrorDetails
               };

        private static SessionInitiationServerOptions SizeLimitedSessionOptions(Int32    MaxRequestBodySize,
                                                                                Boolean  IncludeErrorDetails   = true)

            => SessionInitiationFixture.DefaultServerOptions() with {
                   MaxRequestBodySize   = MaxRequestBodySize,
                   IncludeErrorDetails  = IncludeErrorDetails
               };

        #endregion


        // Every route that takes a body enforces the limit

        #region Pairing_OversizedBody_Returns413OnEveryUnauthenticatedRoute(Path)

        [Test]
        [TestCase("v1/requestPairing",        TestName = "Size limit: pairing POST requestPairing")]
        [TestCase("v1/preparePairing",        TestName = "Size limit: pairing POST preparePairing")]
        [TestCase("v1/cancelPreparePairing",  TestName = "Size limit: pairing POST cancelPreparePairing")]
        [TestCase("v1/waitForPairing",        TestName = "Size limit: pairing POST waitForPairing")]
        public async Task Pairing_OversizedBody_Returns413OnEveryUnauthenticatedRoute(String Path)
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: SizeLimitedPairingOptions(Limit)
                                            );

            var result = await fixture.PostAsync(Path,
                                                 RawBody: BodyOfExactly(4 * Limit));

            Assert.Multiple(() => {
                Assert.That(result.Status,          Is.EqualTo(HttpStatusCode.RequestEntityTooLarge), result.Body);
                Assert.That(result.ErrorMessage,    Is.EqualTo("ParsingError"));
                Assert.That(result.AdditionalInfo,  Does.Contain(Limit.ToString()));
            });

        }

        #endregion

        #region Pairing_OversizedBody_Returns413OnTheAttemptAuthenticatedRoutes()

        [Test]
        public async Task Pairing_OversizedBody_Returns413OnTheAttemptAuthenticatedRoutes()
        {

            // requestConnectionDetails, postConnectionDetails and finalizePairing check the
            // pairingAttemptId first (the 401 checks precede the 400 checks), so a real
            // pairing attempt is needed to reach their body handling at all.
            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: SizeLimitedPairingOptions(AttemptLimit)
                                            );

            var client   = new TestPairingClient(EnergyManagementRole.CEM);
            var rm       = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var started  = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            Assert.That(started.Status, Is.EqualTo(HttpStatusCode.OK), started.Body);
            Assert.That(RequestPairingResponse.TryParse(started.Object, out var response, out var error), Is.True, error);

            var bearer     = response!.PairingAttemptId.Value;
            var oversized  = BodyOfExactly(AttemptLimit + 1);

            var request    = await fixture.PostAsync("v1/requestConnectionDetails", RawBody: oversized, Bearer: bearer);
            var post       = await fixture.PostAsync("v1/postConnectionDetails",    RawBody: oversized, Bearer: bearer);
            var finalize   = await fixture.PostAsync("v1/finalizePairing",          RawBody: oversized, Bearer: bearer);

            Assert.Multiple(() => {

                Assert.That(request.Status,        Is.EqualTo(HttpStatusCode.RequestEntityTooLarge), request.Body);
                Assert.That(request.ErrorMessage,  Is.EqualTo("ParsingError"));

                Assert.That(post.Status,           Is.EqualTo(HttpStatusCode.RequestEntityTooLarge), post.Body);
                Assert.That(post.ErrorMessage,     Is.EqualTo("ParsingError"));

                Assert.That(finalize.Status,       Is.EqualTo(HttpStatusCode.RequestEntityTooLarge), finalize.Body);
                Assert.That(finalize.ErrorMessage, Is.EqualTo("ParsingError"));

            });

        }

        #endregion

        #region SessionInitiation_OversizedBody_Returns413OnEveryBodyRoute(Path)

        [Test]
        [TestCase("v1/initiateSession",  TestName = "Size limit: session initiation POST initiateSession")]
        [TestCase("v1/unpair",           TestName = "Size limit: session initiation POST unpair")]
        public async Task SessionInitiation_OversizedBody_Returns413OnEveryBodyRoute(String Path)
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                                ServerOptions: SizeLimitedSessionOptions(Limit)
                                            );

            // Both operations read their bearer token before their body, so a well-formed
            // (but unknown) access token is needed to reach the size check.
            var result = await fixture.PostAsync(Path,
                                                 Bearer:   TokenGenerator.NewAccessToken().Value,
                                                 RawBody:  BodyOfExactly(4 * Limit));

            Assert.Multiple(() => {
                Assert.That(result.Status,          Is.EqualTo(HttpStatusCode.RequestEntityTooLarge), result.Body);
                Assert.That(result.ErrorMessage,    Is.EqualTo("ParsingError"));
                Assert.That(result.AdditionalInfo,  Does.Contain(Limit.ToString()));
            });

        }

        #endregion


        // The boundary

        #region Pairing_BodyExactlyAtTheLimit_IsStillParsed()

        [Test]
        public async Task Pairing_BodyExactlyAtTheLimit_IsStillParsed()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: SizeLimitedPairingOptions(Limit)
                                            );

            var body = BodyOfExactly(Limit);

            Assert.That(body, Has.Length.EqualTo(Limit), "the test body is exactly at the limit");

            var result = await fixture.PostAsync("v1/requestPairing", RawBody: body);

            Assert.Multiple(() => {
                Assert.That(result.Status,        Is.Not.EqualTo(HttpStatusCode.RequestEntityTooLarge), "a body at the limit is accepted");
                Assert.That(result.Status,        Is.EqualTo(HttpStatusCode.BadRequest),                "and answered by the parser instead");
                Assert.That(result.ErrorMessage,  Is.EqualTo("ParsingError"));
            });

        }

        #endregion

        #region Pairing_BodyOneByteOverTheLimit_Returns413()

        [Test]
        public async Task Pairing_BodyOneByteOverTheLimit_Returns413()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: SizeLimitedPairingOptions(Limit)
                                            );

            var body = BodyOfExactly(Limit + 1);

            Assert.That(body, Has.Length.EqualTo(Limit + 1));

            var result = await fixture.PostAsync("v1/requestPairing", RawBody: body);

            Assert.Multiple(() => {
                Assert.That(result.Status,        Is.EqualTo(HttpStatusCode.RequestEntityTooLarge), result.Body);
                Assert.That(result.ErrorMessage,  Is.EqualTo("ParsingError"));
            });

        }

        #endregion

        #region SessionInitiation_BodyExactlyAtTheLimit_IsStillParsed()

        [Test]
        public async Task SessionInitiation_BodyExactlyAtTheLimit_IsStillParsed()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                                ServerOptions: SizeLimitedSessionOptions(Limit)
                                            );

            var result = await fixture.PostAsync("v1/initiateSession",
                                                 Bearer:   TokenGenerator.NewAccessToken().Value,
                                                 RawBody:  BodyOfExactly(Limit));

            Assert.Multiple(() => {
                Assert.That(result.Status,        Is.Not.EqualTo(HttpStatusCode.RequestEntityTooLarge), "a body at the limit is accepted");
                Assert.That(result.Status,        Is.EqualTo(HttpStatusCode.BadRequest),                "and answered by the parser instead");
                Assert.That(result.ErrorMessage,  Is.EqualTo("ParsingError"));
            });

        }

        #endregion

        #region SessionInitiation_BodyOneByteOverTheLimit_Returns413()

        [Test]
        public async Task SessionInitiation_BodyOneByteOverTheLimit_Returns413()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                                ServerOptions: SizeLimitedSessionOptions(Limit)
                                            );

            var result = await fixture.PostAsync("v1/initiateSession",
                                                 Bearer:   TokenGenerator.NewAccessToken().Value,
                                                 RawBody:  BodyOfExactly(Limit + 1));

            Assert.Multiple(() => {
                Assert.That(result.Status,        Is.EqualTo(HttpStatusCode.RequestEntityTooLarge), result.Body);
                Assert.That(result.ErrorMessage,  Is.EqualTo("ParsingError"));
            });

        }

        #endregion


        // The shape of the refusal

        #region Pairing_OversizedBody_IsRefusedBeforeItIsParsed()

        [Test]
        public async Task Pairing_OversizedBody_IsRefusedBeforeItIsParsed()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: SizeLimitedPairingOptions(Limit)
                                            );

            // Not even JSON: an oversized body is refused by its announced length, so the
            // parser never sees it.
            var result = await fixture.PostAsync("v1/requestPairing",
                                                 RawBody: new String('x', 4 * Limit));

            Assert.Multiple(() => {
                Assert.That(result.Status,  Is.EqualTo    (HttpStatusCode.RequestEntityTooLarge), result.Body);
                Assert.That(result.Status,  Is.Not.EqualTo(HttpStatusCode.BadRequest),            "the size limit precedes the parsing");
            });

        }

        #endregion

        #region Pairing_TooLargeError_IsTheS2ErrorShape()

        [Test]
        public async Task Pairing_TooLargeError_IsTheS2ErrorShape()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: SizeLimitedPairingOptions(Limit)
                                            );

            var result = await fixture.PostAsync("v1/requestPairing",
                                                 RawBody: BodyOfExactly(4 * Limit));

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.RequestEntityTooLarge), result.Body);

            Assert.Multiple(() => {

                Assert.That(result.ContentType,     Does.StartWith("application/json"));
                Assert.That(result.CacheControl,    Does.Contain("no-store"));

                Assert.That(result.Object.Properties().Select(property => property.Name),
                            Is.EquivalentTo(new[] { "errorMessage", "additionalInfo" }),
                            "a PairingResponseErrorMessage and nothing else");

                Assert.That(result.ErrorMessage,    Is.EqualTo("ParsingError"));
                Assert.That(result.AdditionalInfo,  Does.Contain(Limit.ToString()).
                                                    And.Contain("bytes"));

            });

        }

        #endregion

        #region Pairing_WithoutErrorDetails_LeaksNoAdditionalInfo()

        [Test]
        public async Task Pairing_WithoutErrorDetails_LeaksNoAdditionalInfo()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: SizeLimitedPairingOptions(Limit, IncludeErrorDetails: false)
                                            );

            var result = await fixture.PostAsync("v1/requestPairing",
                                                 RawBody: BodyOfExactly(4 * Limit));

            Assert.Multiple(() => {
                Assert.That(result.Status,          Is.EqualTo(HttpStatusCode.RequestEntityTooLarge), result.Body);
                Assert.That(result.ErrorMessage,    Is.EqualTo("ParsingError"), "the error itself stays");
                Assert.That(result.AdditionalInfo,  Is.Null,                    "but not its details");
                Assert.That(result.Body ?? "",      Does.Not.Contain(Limit.ToString()), "the configured limit is not disclosed");
            });

        }

        #endregion

        #region SessionInitiation_WithoutErrorDetails_LeaksNoAdditionalInfo()

        [Test]
        public async Task SessionInitiation_WithoutErrorDetails_LeaksNoAdditionalInfo()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                                ServerOptions: SizeLimitedSessionOptions(Limit, IncludeErrorDetails: false)
                                            );

            var result = await fixture.PostAsync("v1/initiateSession",
                                                 Bearer:   TokenGenerator.NewAccessToken().Value,
                                                 RawBody:  BodyOfExactly(4 * Limit));

            Assert.Multiple(() => {
                Assert.That(result.Status,          Is.EqualTo(HttpStatusCode.RequestEntityTooLarge), result.Body);
                Assert.That(result.ErrorMessage,    Is.EqualTo("ParsingError"));
                Assert.That(result.AdditionalInfo,  Is.Null);
                Assert.That(result.Body ?? "",      Does.Not.Contain(Limit.ToString()));
            });

        }

        #endregion


        // The options

        #region PairingServerOptions_Validate_RejectsAnUnusableLimit()

        [Test]
        public void PairingServerOptions_Validate_RejectsAnUnusableLimit()
        {

            var options = new PairingServerOptions();

            Assert.Multiple(() => {

                // The defaults of the hardening (PLAN.md §11a).
                Assert.That(options.MaxRequestBodySize,            Is.EqualTo(64 * 1024));
                Assert.That(options.EnableRateLimiting,            Is.True);
                Assert.That(options.RateLimitCapacity,             Is.EqualTo(300));
                Assert.That(options.RateLimitRefillPeriod,         Is.EqualTo(TimeSpan.FromMinutes(1)));
                Assert.That(options.RateLimitMaxSources,           Is.EqualTo(S2RequestRateLimiter.DefaultMaximumBuckets));
                Assert.That(options.UseTooManyRequestsStatusCode,  Is.False, "503 is the interoperable answer");
                Assert.That(options.Validate,                      Throws.Nothing);

                // The request body size limit is always validated.
                Assert.That((options with { MaxRequestBodySize =  0 }).Validate,  Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { MaxRequestBodySize = -1 }).Validate,  Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { MaxRequestBodySize =  1 }).Validate,  Throws.Nothing);

                // The rate limit is validated while it is enabled.
                Assert.That((options with { RateLimitCapacity     =  0 }).Validate,                       Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { RateLimitCapacity     = -1 }).Validate,                       Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { RateLimitRefillPeriod = TimeSpan.Zero }).Validate,            Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { RateLimitRefillPeriod = TimeSpan.FromSeconds(-1) }).Validate, Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { RateLimitMaxSources   =  0 }).Validate,                       Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { RateLimitCapacity     =  1 }).Validate,                       Throws.Nothing);

                // A disabled rate limit does not need usable parameters.
                Assert.That((options with { EnableRateLimiting = false, RateLimitCapacity     = 0             }).Validate,  Throws.Nothing);
                Assert.That((options with { EnableRateLimiting = false, RateLimitRefillPeriod = TimeSpan.Zero }).Validate,  Throws.Nothing);

            });

        }

        #endregion

        #region SessionInitiationServerOptions_Validate_RejectsAnUnusableLimit()

        [Test]
        public void SessionInitiationServerOptions_Validate_RejectsAnUnusableLimit()
        {

            var options = new SessionInitiationServerOptions();

            Assert.Multiple(() => {

                Assert.That(options.MaxRequestBodySize,            Is.EqualTo(64 * 1024));
                Assert.That(options.EnableRateLimiting,            Is.True);
                Assert.That(options.RateLimitCapacity,             Is.EqualTo(120));
                Assert.That(options.RateLimitRefillPeriod,         Is.EqualTo(TimeSpan.FromMinutes(1)));
                Assert.That(options.RateLimitMaxSources,           Is.EqualTo(S2RequestRateLimiter.DefaultMaximumBuckets));
                Assert.That(options.UseTooManyRequestsStatusCode,  Is.False);
                Assert.That(options.Validate,                      Throws.Nothing);

                Assert.That((options with { MaxRequestBodySize =  0 }).Validate,  Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { MaxRequestBodySize = -1 }).Validate,  Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { MaxRequestBodySize =  1 }).Validate,  Throws.Nothing);

                Assert.That((options with { RateLimitCapacity     =  0 }).Validate,                       Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { RateLimitCapacity     = -1 }).Validate,                       Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { RateLimitRefillPeriod = TimeSpan.Zero }).Validate,            Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { RateLimitRefillPeriod = TimeSpan.FromSeconds(-1) }).Validate, Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((options with { RateLimitMaxSources   =  0 }).Validate,                       Throws.TypeOf<ArgumentOutOfRangeException>());

                Assert.That((options with { EnableRateLimiting = false, RateLimitCapacity     = 0             }).Validate,  Throws.Nothing);
                Assert.That((options with { EnableRateLimiting = false, RateLimitRefillPeriod = TimeSpan.Zero }).Validate,  Throws.Nothing);

            });

        }

        #endregion

    }

}

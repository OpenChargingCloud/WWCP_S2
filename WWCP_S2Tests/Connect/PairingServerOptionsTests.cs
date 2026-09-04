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
    /// The pairing server options (PLAN.md §3.7) with the normative defaults of S2 Connect
    /// 1.0.0 and their validation, and the transport-independent pairing server results
    /// (PLAN.md D15).
    /// </summary>
    [TestFixture]
    public sealed class PairingServerOptionsTests
    {

        #region Defaults

        [Test]
        [S2C("Pairing.2.Delay")]
        [S2C("Pairing.Interruption")]
        [S2C("LongPolling.ServerTimeout")]
        public void Defaults_EqualTheNormativeValues()
        {

            var options = new PairingServerOptions();

            Assert.Multiple(() => {

                Assert.That(options.RequestPairingDelay,             Is.EqualTo(S2ConnectDefaults.RequestPairingDelay));
                Assert.That(options.RequestPairingDelay,             Is.EqualTo(TimeSpan.FromSeconds(1)));
                Assert.That(options.PairingAttemptTimeout,           Is.EqualTo(S2ConnectDefaults.PairingAttemptTimeout));
                Assert.That(options.PairingAttemptTimeout,           Is.EqualTo(TimeSpan.FromSeconds(15)));
                Assert.That(options.CompletedAttemptRetention,       Is.EqualTo(S2ConnectDefaults.PairingAttemptTimeout));
                Assert.That(options.MaxQueuedPairingAttemptsPerNode, Is.EqualTo(S2ConnectDefaults.MaxQueuedPairingAttemptsPerNode));
                Assert.That(options.MaxQueuedPairingAttemptsPerNode, Is.EqualTo(4));
                Assert.That(options.LongPollingTimeout,              Is.EqualTo(S2ConnectDefaults.LongPollingServerTimeout));
                Assert.That(options.LongPollingTimeout,              Is.EqualTo(TimeSpan.FromSeconds(25)));
                Assert.That(options.SupportedHmacHashingAlgorithms,  Is.EqualTo(new[] { HmacHashingAlgorithm.SHA256 }));
                Assert.That(options.MaxHangingLongPollingRequests,   Is.EqualTo(64));
                Assert.That(options.AutoRequestNodeDescriptions,     Is.True);
                Assert.That(options.IncludeErrorDetails,             Is.True);
                Assert.That(options.DefaultClientDeployment,         Is.Null);
                Assert.That(options.EnableLANOperations,             Is.Null);
                Assert.That(options.EnableLongPolling,               Is.Null);
                Assert.That(options.ParserOptions,                   Is.SameAs(S2ParserOptions.Default));

                Assert.That(options.Validate,                        Throws.Nothing);
                Assert.That(PairingServerOptions.Default.Validate,   Throws.Nothing);

            });

        }

        #endregion

        #region Validate()

        [Test]
        [S2C("Pairing.2.Delay")]
        public void Validate_RejectsANegativeRequestPairingDelay_ButAcceptsZero()
        {
            Assert.That((PairingServerOptions.Default with { RequestPairingDelay = TimeSpan.FromMilliseconds(-1) }).Validate, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That((PairingServerOptions.Default with { RequestPairingDelay = TimeSpan.Zero }).Validate,                 Throws.Nothing, "zero delay for tests");
        }

        [Test]
        [S2C("Pairing.Interruption")]
        public void Validate_RejectsANonPositivePairingAttemptTimeout()
        {
            Assert.That((PairingServerOptions.Default with { PairingAttemptTimeout = TimeSpan.Zero }).Validate,            Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That((PairingServerOptions.Default with { PairingAttemptTimeout = TimeSpan.FromSeconds(-1) }).Validate, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Validate_RejectsANegativeCompletedAttemptRetention_ButAcceptsZero()
        {
            Assert.That((PairingServerOptions.Default with { CompletedAttemptRetention = TimeSpan.FromSeconds(-1) }).Validate, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That((PairingServerOptions.Default with { CompletedAttemptRetention = TimeSpan.Zero }).Validate,            Throws.Nothing);
        }

        [Test]
        [S2C("Pairing.2.RateLimit")]
        public void Validate_RejectsANegativeQueueLength_ButAcceptsZero()
        {
            Assert.That((PairingServerOptions.Default with { MaxQueuedPairingAttemptsPerNode = -1 }).Validate, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That((PairingServerOptions.Default with { MaxQueuedPairingAttemptsPerNode =  0 }).Validate, Throws.Nothing);
        }

        [Test]
        [S2C("Pairing.HmacHashingAlgorithm")]
        public void Validate_RequiresSHA256AsTheOnlyHmacHashingAlgorithm()
        {

            var sha512 = HmacHashingAlgorithm.Parse("SHA512");

            Assert.Multiple(() => {
                Assert.That(sha512.IsKnown,                                                                                                             Is.False);
                Assert.That((PairingServerOptions.Default with { SupportedHmacHashingAlgorithms = [] }).Validate,                                        Throws.ArgumentException, "empty list");
                Assert.That((PairingServerOptions.Default with { SupportedHmacHashingAlgorithms = null! }).Validate,                                     Throws.ArgumentException, "no list");
                Assert.That((PairingServerOptions.Default with { SupportedHmacHashingAlgorithms = [ sha512 ] }).Validate,                                Throws.ArgumentException, "unknown algorithm");
                Assert.That((PairingServerOptions.Default with { SupportedHmacHashingAlgorithms = [ HmacHashingAlgorithm.SHA256, sha512 ] }).Validate,   Throws.ArgumentException, "unknown algorithm besides SHA256");
                Assert.That((PairingServerOptions.Default with { SupportedHmacHashingAlgorithms = [ HmacHashingAlgorithm.SHA256 ] }).Validate,           Throws.Nothing);
            });

        }

        [Test]
        public void Validate_RequiresALANOrWANDefaultClientDeployment()
        {
            Assert.That((PairingServerOptions.Default with { DefaultClientDeployment = Deployment.Parse("CLOUD") }).Validate, Throws.ArgumentException);
            Assert.That((PairingServerOptions.Default with { DefaultClientDeployment = Deployment.LAN }).Validate,            Throws.Nothing);
            Assert.That((PairingServerOptions.Default with { DefaultClientDeployment = Deployment.WAN }).Validate,            Throws.Nothing);
        }

        [Test]
        [S2C("LongPolling.ServerTimeout")]
        public void Validate_RequiresALongPollingTimeoutBetweenZeroAnd25Seconds()
        {

            Assert.Multiple(() => {
                Assert.That((PairingServerOptions.Default with { LongPollingTimeout = TimeSpan.FromSeconds(26) }).Validate,                   Throws.TypeOf<ArgumentOutOfRangeException>(), "above the normative maximum");
                Assert.That((PairingServerOptions.Default with { LongPollingTimeout = TimeSpan.FromSeconds(25) + TimeSpan.FromTicks(1) }).Validate, Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((PairingServerOptions.Default with { LongPollingTimeout = TimeSpan.Zero }).Validate,                              Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((PairingServerOptions.Default with { LongPollingTimeout = TimeSpan.FromSeconds(-1) }).Validate,                   Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((PairingServerOptions.Default with { LongPollingTimeout = TimeSpan.FromSeconds(25) }).Validate,                   Throws.Nothing);
                Assert.That((PairingServerOptions.Default with { LongPollingTimeout = TimeSpan.FromSeconds(1) }).Validate,                    Throws.Nothing);
            });

        }

        [Test]
        public void Validate_RequiresAtLeastOneHangingLongPollingRequest()
        {
            Assert.That((PairingServerOptions.Default with { MaxHangingLongPollingRequests =  0 }).Validate, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That((PairingServerOptions.Default with { MaxHangingLongPollingRequests = -1 }).Validate, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That((PairingServerOptions.Default with { MaxHangingLongPollingRequests =  1 }).Validate, Throws.Nothing);
        }

        [Test]
        public void Validate_RequiresParserOptions()
        {
            Assert.That((PairingServerOptions.Default with { ParserOptions = null! }).Validate,                                            Throws.ArgumentNullException);
            Assert.That((PairingServerOptions.Default with { ParserOptions = new S2ParserOptions { AllowInsecureURLs = true } }).Validate, Throws.Nothing);
        }

        #endregion


        #region PairingServerResult factories

        [Test]
        public void NoContent_Is204()
        {

            var result = PairingServerResult.NoContent();

            Assert.Multiple(() => {
                Assert.That(result.StatusCode.Code, Is.EqualTo(204));
                Assert.That(result.IsSuccess,       Is.True);
                Assert.That(result.Error,           Is.Null);
                Assert.That(result.Description,     Is.Null);
                Assert.That(result.RetryAfter,      Is.Null);
                Assert.That(result.ToString(),      Is.EqualTo("204"));
            });

        }

        [Test]
        [S2C("Pairing.1.ErrorMessage")]
        public void BadRequest_Is400WithAPairingResponseErrorMessage()
        {

            var withInfo    = PairingServerResult.BadRequest(PairingResponseError.NodeNotFound, "no such node");
            var withoutInfo = PairingServerResult.BadRequest(PairingResponseError.ParsingError);

            Assert.Multiple(() => {

                Assert.That(withInfo.StatusCode.Code,        Is.EqualTo(400));
                Assert.That(withInfo.IsSuccess,              Is.False);
                Assert.That(withInfo.Error,                  Is.Not.Null);
                Assert.That(withInfo.Error!.ErrorMessage,    Is.EqualTo(PairingResponseError.NodeNotFound));
                Assert.That(withInfo.Error!.AdditionalInfo,  Is.EqualTo("no such node"));
                Assert.That(withInfo.Description,            Is.EqualTo("no such node"), "the description defaults to the additional information");
                Assert.That(withInfo.RetryAfter,             Is.Null);
                Assert.That(withInfo.ToString(),             Is.EqualTo("400 NodeNotFound (no such node)"));

                Assert.That(withoutInfo.Error!.ErrorMessage, Is.EqualTo(PairingResponseError.ParsingError));
                Assert.That(withoutInfo.Error!.AdditionalInfo, Is.Null);
                Assert.That(withoutInfo.Description,         Is.Null);
                Assert.That(withoutInfo.ToString(),          Is.EqualTo("400 ParsingError"));

            });

        }

        [Test]
        public void Unauthorized_Forbidden_NotFound_InternalServerError_CarryTheStatusCodeAndDescription()
        {

            Assert.Multiple(() => {

                Assert.That(PairingServerResult.Unauthorized("expired").StatusCode.Code,      Is.EqualTo(401));
                Assert.That(PairingServerResult.Unauthorized("expired").Description,          Is.EqualTo("expired"));
                Assert.That(PairingServerResult.Unauthorized("expired").ToString(),           Is.EqualTo("401 (expired)"));
                Assert.That(PairingServerResult.Unauthorized().Description,                   Is.Null);
                Assert.That(PairingServerResult.Unauthorized().ToString(),                    Is.EqualTo("401"));

                Assert.That(PairingServerResult.Forbidden("wrong response").StatusCode.Code,  Is.EqualTo(403));
                Assert.That(PairingServerResult.Forbidden("wrong response").Description,      Is.EqualTo("wrong response"));

                Assert.That(PairingServerResult.NotFound("not served").StatusCode.Code,       Is.EqualTo(404));
                Assert.That(PairingServerResult.NotFound("not served").Description,           Is.EqualTo("not served"));

                Assert.That(PairingServerResult.InternalServerError("store").StatusCode.Code, Is.EqualTo(500));
                Assert.That(PairingServerResult.InternalServerError("store").Description,     Is.EqualTo("store"));

                foreach (var result in new[] { PairingServerResult.Unauthorized(), PairingServerResult.Forbidden(), PairingServerResult.NotFound(), PairingServerResult.InternalServerError() })
                {
                    Assert.That(result.IsSuccess,  Is.False, result.ToString());
                    Assert.That(result.Error,      Is.Null,  result.ToString());
                    Assert.That(result.RetryAfter, Is.Null,  result.ToString());
                }

            });

        }

        [Test]
        public void ServiceUnavailable_Is503WithAnOptionalRetryAfter()
        {

            var withRetry    = PairingServerResult.ServiceUnavailable(TimeSpan.FromSeconds(2), "busy");
            var withoutRetry = PairingServerResult.ServiceUnavailable();

            Assert.Multiple(() => {
                Assert.That(withRetry.StatusCode.Code,    Is.EqualTo(503));
                Assert.That(withRetry.IsSuccess,          Is.False);
                Assert.That(withRetry.RetryAfter,         Is.EqualTo(TimeSpan.FromSeconds(2)));
                Assert.That(withRetry.Description,        Is.EqualTo("busy"));
                Assert.That(withRetry.Error,              Is.Null);
                Assert.That(withoutRetry.StatusCode.Code, Is.EqualTo(503));
                Assert.That(withoutRetry.RetryAfter,      Is.Null);
                Assert.That(withoutRetry.Description,     Is.Null);
            });

        }

        [Test]
        public void OK_Is200WithTheBody()
        {

            var body   = new EndpointDescription("Test endpoint", null, Deployment.LAN);
            var result = PairingServerResult.OK(body);

            Assert.Multiple(() => {
                Assert.That(result,                 Is.InstanceOf<PairingServerResult<EndpointDescription>>());
                Assert.That(result.StatusCode.Code, Is.EqualTo(200));
                Assert.That(result.IsSuccess,       Is.True);
                Assert.That(result.Value,           Is.SameAs(body));
                Assert.That(result.Error,           Is.Null);
                Assert.That(result.Description,     Is.Null);
                Assert.That(result.RetryAfter,      Is.Null);
                Assert.That(result.ToString(),      Is.EqualTo("200"));
            });

            Assert.That(() => PairingServerResult.OK<EndpointDescription>(null!), Throws.ArgumentNullException);

        }

        [Test]
        public void Wrap_LiftsAResultWithoutBodyIntoATypedResult()
        {

            var badRequest  = PairingServerResult.BadRequest(PairingResponseError.Other, "details");
            var unavailable = PairingServerResult.ServiceUnavailable(TimeSpan.FromSeconds(3), "busy");

            var wrapped1    = PairingServerResult.Wrap<EndpointDescription>(badRequest);
            var wrapped2    = PairingServerResult.Wrap<EndpointDescription>(unavailable);

            Assert.Multiple(() => {

                Assert.That(wrapped1.StatusCode,   Is.SameAs(badRequest.StatusCode));
                Assert.That(wrapped1.IsSuccess,    Is.False);
                Assert.That(wrapped1.Value,        Is.Null);
                Assert.That(wrapped1.Error,        Is.SameAs(badRequest.Error));
                Assert.That(wrapped1.Description,  Is.EqualTo("details"));
                Assert.That(wrapped1.RetryAfter,   Is.Null);
                Assert.That(wrapped1.ToString(),   Is.EqualTo(badRequest.ToString()));

                Assert.That(wrapped2.StatusCode.Code, Is.EqualTo(503));
                Assert.That(wrapped2.Value,        Is.Null);
                Assert.That(wrapped2.Error,        Is.Null);
                Assert.That(wrapped2.Description,  Is.EqualTo("busy"));
                Assert.That(wrapped2.RetryAfter,   Is.EqualTo(TimeSpan.FromSeconds(3)));

            });

            Assert.That(() => PairingServerResult.Wrap<EndpointDescription>(null!), Throws.ArgumentNullException);

        }

        #endregion

    }

}

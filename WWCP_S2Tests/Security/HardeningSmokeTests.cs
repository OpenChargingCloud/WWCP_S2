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
using System.Text;

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Tests.Connect;
using cloud.charging.open.protocols.S2.Tests.Connect.Discovery;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// The hardening of the S2 Connect servers over real HTTP (PLAN.md §11a): the per-source
    /// rate limit, the request size limit and the redaction of the secrets.
    /// </summary>
    [TestFixture]
    public sealed class HardeningSmokeTests
    {

        #region RateLimit_BeyondCapacity_Returns503WithRetryAfter()

        [Test]
        public async Task RateLimit_BeyondCapacity_Returns503WithRetryAfter()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: PairingServerFixture.DefaultOptions() with {
                                                             RateLimitCapacity      = 3,
                                                             RateLimitRefillPeriod  = TimeSpan.FromMinutes(10)
                                                         }
                                            );

            var statuses = new List<HttpStatusCode>();

            for (var i = 0; i < 4; i++)
                statuses.Add((await fixture.GetAsync("")).Status);

            var refused = await fixture.GetAsync("");

            Assert.Multiple(() => {
                Assert.That(statuses.Take(3),   Is.All.EqualTo(HttpStatusCode.OK));
                Assert.That(statuses[3],        Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(refused.Status,     Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(refused.RetryAfter, Is.Not.Null);
            });

        }

        #endregion

        #region RateLimit_Disabled_ServesEverything()

        [Test]
        public async Task RateLimit_Disabled_ServesEverything()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: PairingServerFixture.DefaultOptions() with {
                                                             EnableRateLimiting = false
                                                         }
                                            );

            for (var i = 0; i < 20; i++)
                Assert.That((await fixture.GetAsync("")).Status, Is.EqualTo(HttpStatusCode.OK));

        }

        #endregion

        #region RateLimit_TooManyRequestsStatusCode_Returns429()

        [Test]
        public async Task RateLimit_TooManyRequestsStatusCode_Returns429()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: PairingServerFixture.DefaultOptions() with {
                                                             RateLimitCapacity             = 1,
                                                             RateLimitRefillPeriod         = TimeSpan.FromMinutes(10),
                                                             UseTooManyRequestsStatusCode  = true
                                                         }
                                            );

            Assert.That((await fixture.GetAsync("")).Status, Is.EqualTo(HttpStatusCode.OK));

            var refused = await fixture.GetAsync("");

            Assert.Multiple(() => {
                Assert.That(refused.Status,     Is.EqualTo(HttpStatusCode.TooManyRequests));
                Assert.That(refused.RetryAfter, Is.Not.Null);
            });

        }

        #endregion

        #region WANRegistry_RateLimit_BeyondCapacity_Returns503WithRetryAfter()

        [Test]
        public async Task WANRegistry_RateLimit_BeyondCapacity_Returns503WithRetryAfter()
        {

            // A WAN registry answers the whole internet, so it carries the same brake.
            await using var fixture = await WANRegistryFixture.CreateAsync(
                                                RateLimitCapacity:      2,
                                                RateLimitRefillPeriod:  TimeSpan.FromMinutes(10)
                                            );

            var index     = await fixture.GetAsync("");
            var endpoints = await fixture.GetAsync("v1/endpoint");

            Assert.Multiple(() => {
                Assert.That(index.Status,      Is.EqualTo(HttpStatusCode.OK));
                Assert.That(endpoints.Status,  Is.EqualTo(HttpStatusCode.OK));
            });

            var refused = await fixture.GetAsync("v1/endpoint");

            Assert.Multiple(() => {
                Assert.That(refused.Status,     Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(refused.RetryAfter, Is.Not.Null);
            });

        }

        #endregion

        #region WANRegistry_RateLimit_Disabled_ServesEverything()

        [Test]
        public async Task WANRegistry_RateLimit_Disabled_ServesEverything()
        {

            await using var fixture = await WANRegistryFixture.CreateAsync(RateLimitCapacity: 0);

            for (var i = 0; i < 12; i++)
                Assert.That((await fixture.GetAsync("v1/endpoint")).Status, Is.EqualTo(HttpStatusCode.OK));

        }

        #endregion

        #region OversizedBody_Returns413()

        [Test]
        public async Task OversizedBody_Returns413()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: PairingServerFixture.DefaultOptions() with {
                                                             MaxRequestBodySize = 512
                                                         }
                                            );

            fixture.AddNode(EnergyManagementRole.RM);

            var oversized = new JObject(
                                new JProperty("clientNodeId", new String('x', 2000))
                            );

            var result = await fixture.PostAsync("v1/requestPairing", oversized);

            Assert.Multiple(() => {
                Assert.That(result.Status,        Is.EqualTo(HttpStatusCode.RequestEntityTooLarge));
                Assert.That(result.ErrorMessage,  Is.EqualTo("ParsingError"));
            });

        }

        #endregion

        #region BodyWithinLimit_IsParsed()

        [Test]
        public async Task BodyWithinLimit_IsParsed()
        {

            await using var fixture = await PairingServerFixture.CreateAsync(
                                                Options: PairingServerFixture.DefaultOptions() with {
                                                             MaxRequestBodySize = 64 * 1024
                                                         }
                                            );

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            var result = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            Assert.That(result.Status, Is.EqualTo(HttpStatusCode.OK), result.Body);

        }

        #endregion

        #region Redaction_MasksTheSecretsOfAPairingInteraction()

        [Test]
        public void Redaction_MasksTheSecretsOfAPairingInteraction()
        {

            var body = new JObject(
                           new JProperty("clientNodeId",                 "node-1"),
                           new JProperty("pairingAttemptId",             "6d6f7265-746f-6b65-6e73-2d746f2d6869"),
                           new JProperty("clientHmacChallengeResponse",  "cbf43926d0f1a19bd0f1a19b"),
                           new JProperty("connectionDetails",
                               new JObject(
                                   new JProperty("accessToken",  "s3cr3t-access-token"),
                                   new JProperty("host",         "192.168.1.7")
                               )
                           )
                       ).ToString();

            var redacted = S2LogRedaction.Redact(body) ?? "";

            Assert.Multiple(() => {

                Assert.That(redacted, Does.Not.Contain("6d6f7265-746f-6b65-6e73-2d746f2d6869"));
                Assert.That(redacted, Does.Not.Contain("cbf43926d0f1a19bd0f1a19b"));
                Assert.That(redacted, Does.Not.Contain("s3cr3t-access-token"));

                // Everything that is no secret stays readable.
                Assert.That(redacted, Does.Contain("node-1"));
                Assert.That(redacted, Does.Contain("192.168.1.7"));
                Assert.That(redacted, Does.Contain("pairingAttemptId"));

            });

        }

        #endregion

        #region Redaction_MasksAnAuthorizationHeader()

        [Test]
        public void Redaction_MasksAnAuthorizationHeader()
        {

            var pdu = String.Join(
                          "\r\n",
                          "POST /pairing/v1/finalizePairing HTTP/1.1",
                          "Host: 192.168.1.7:8443",
                          "Authorization: Bearer 0a1b2c3d4e5f60718293a4b5c6d7e8f9",
                          "Content-Type: application/json",
                          ""
                      );

            var redacted = S2LogRedaction.Redact(pdu) ?? "";

            Assert.Multiple(() => {
                Assert.That(redacted, Does.Not.Contain("0a1b2c3d4e5f60718293a4b5c6d7e8f9"));
                Assert.That(redacted, Does.Contain("Authorization: Bearer [REDACTED]"));
                Assert.That(redacted, Does.Contain("Host: 192.168.1.7:8443"));
            });

        }

        #endregion

        #region Redaction_IsIdempotent()

        [Test]
        public void Redaction_IsIdempotent()
        {

            String[] texts = [
                "accessToken=abc123",
                "pairingAttemptId: 0a1b2c3d",
                "Authorization: Bearer 0a1b2c3d",
                "{\"accessToken\": \"abc123\", \"nodeId\": \"n-1\"}",
                "?websocketToken=abc&node=n-1"
            ];

            foreach (var text in texts)
            {

                var once   = S2LogRedaction.Redact(text);
                var twice  = S2LogRedaction.Redact(once);

                Assert.Multiple(() => {
                    Assert.That(once,  Is.Not.EqualTo(text), $"'{text}' was not redacted at all");
                    Assert.That(twice, Is.EqualTo(once),     $"redacting '{text}' twice changed it again: '{twice}'");
                });

            }

        }

        #endregion

        #region Fuzz_MalformedBodies_NeverCrashTheServer()

        [Test]
        public async Task Fuzz_MalformedBodies_NeverCrashTheServer()
        {

            await using var fixture = await PairingServerFixture.CreateAsync();

            var client = new TestPairingClient(EnergyManagementRole.CEM);
            var rm     = fixture.AddNode(EnergyManagementRole.RM);
            rm.SetStaticPairingToken(client.Token);

            String[] bodies = [
                "",
                "{",
                "[]",
                "null",
                "\"a string\"",
                "{\"clientNodeId\": }",
                "{} {}",
                "{\"clientNodeId\": \"" + new String('�', 100) + "\"}",
                "{\"clientNodeId\": 42}",
                new String('[', 200) + new String(']', 200),

                // A deeply nested document must be refused by the parser's depth guard, not by
                // exhausting the server: 10.000 levels, answered in milliseconds.
                new String('[', 10_000) + new String(']', 10_000)
            ];

            foreach (var body in bodies)
            {

                var result = await fixture.PostAsync("v1/requestPairing", RawBody: body);

                Assert.That(
                    (Int32) result.Status,
                    Is.InRange(400, 499),
                    $"'{(body.Length > 40 ? body[..40] + "..." : body)}' was answered with {(Int32) result.Status}"
                );

            }

            // The server still serves a valid request after all of that.
            var valid = await fixture.PostAsync("v1/requestPairing", client.RequestPairing().ToJSON());

            Assert.That(valid.Status, Is.EqualTo(HttpStatusCode.OK), valid.Body);

        }

        #endregion

    }

}

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

using Microsoft.Extensions.Logging;

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Node;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Node
{

    /// <summary>
    /// The demand of PLAN.md §3.6 — "A test greps log output for a known token" — over a real
    /// end-to-end run: a CEM node and an RM node pair over loopback HTTP, open a WebSocket
    /// session and unpair, while a <see cref="CapturingLoggerFactory"/> records everything they
    /// log. Afterwards every secret the test can reach (the dynamic pairing token, the access
    /// token of the pairing on both sides, every access token candidate and every pending token
    /// of both stores) must be absent from the captured text, while the node identifications
    /// must be present — a log that says nothing cannot leak anything, and would pass a
    /// weaker test.
    ///
    /// <para>
    /// The negative control proves that the grep has teeth: the very same log entry, written
    /// through a node with <c>RedactSecretsInLogs = false</c>, does carry the secret.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class SecretsInLogsTests
    {

        #region Data

        /// <summary>
        /// The unpairing tears down the session through the reconnecting client, like the
        /// end-to-end tests of the node layer.
        /// </summary>
        private static readonly TimeSpan  ReconnectTimeout  = TimeSpan.FromSeconds(8);

        #endregion

        #region Helpers

        /// <summary>
        /// A fresh hosted CEM node.
        /// </summary>
        private static HostedNode HostedCEM()

            => new (new NodeDescription(
                        Node_Id.NewRandom,
                        "GraphDefined",
                        "EMS",
                        "Home Energy Manager",
                        EnergyManagementRole.CEM
                    ));

        /// <summary>
        /// The options of a LAN CEM (a communication server). The nodes built with these are
        /// never started: no port is bound, no request is sent.
        /// </summary>
        private static S2NodeOptions CEMOptions()

            => new () {
                   Description           = new EndpointDescription("Home CEM"),
                   Deployment            = Deployment.LAN,
                   PairingUrl            = S2BaseURL.Parse("https://cem.local/pairing/"),
                   SessionInitiationUrl  = S2BaseURL.Parse("https://cem.local/connection/")
               };

        /// <summary>
        /// A body of the shape S2 Connect puts a secret into: the connection details of the
        /// pairing process, carrying an access token (S2 Connect 1.0.0, "postConnectionDetails").
        /// </summary>
        /// <param name="AccessToken">The secret to embed.</param>
        private static String ConnectionDetailsBody(String AccessToken)

            => $$"""
                 { "connectionDetails": { "accessToken": "{{AccessToken}}", "host": "192.168.1.7", "port": 8443 } }
                 """;

        #endregion


        #region PairSessionUnpair_LeaksNoSecretIntoTheLog()

        [Test]
        public async Task PairSessionUnpair_LeaksNoSecretIntoTheLog()
        {

            using var capturing = new CapturingLoggerFactory();

            await using var fixture = await S2NodeFixture.CreateAsync(LoggerFactory: capturing);

            // Every secret of this run, by its value, so that a failure can name the one that leaked.
            var secrets = new Dictionary<String, String>(StringComparer.Ordinal);

            void Remember(String Description, String? Secret)
            {
                if (!String.IsNullOrEmpty(Secret))
                    secrets.TryAdd(Secret, Description);
            }

            // The active access token of every pairing and every token a communication client
            // would still try (the active one plus the pending ones of a session initiation).
            async Task RememberTokensAsync(String When)
            {

                foreach (var pairing in await fixture.CEM.Store.GetPairingsAsync(fixture.CEM.NodeId))
                    Remember($"the access token of the CEM pairing with {pairing.RemoteNodeId} ({When})", pairing.AccessToken.Value);

                foreach (var pairing in await fixture.RM. Store.GetPairingsAsync(fixture.RM. NodeId))
                    Remember($"the access token of the RM pairing with {pairing.RemoteNodeId} ({When})",  pairing.AccessToken.Value);

                foreach (var candidate in await fixture.CEM.Store.GetAccessTokenCandidatesAsync(fixture.CEM.NodeId, fixture.RM. NodeId))
                    Remember($"an access token candidate of the CEM ({When})", candidate.Value);

                foreach (var candidate in await fixture.RM. Store.GetAccessTokenCandidatesAsync(fixture.RM. NodeId, fixture.CEM.NodeId))
                    Remember($"an access token candidate of the RM ({When})",  candidate.Value);

                foreach (var pending in await fixture.CEM.Store.GetPendingAccessTokensAsync(fixture.CEM.NodeId, fixture.RM. NodeId))
                    Remember($"a pending access token of the CEM ({When})", pending.Token.Value);

                foreach (var pending in await fixture.RM. Store.GetPendingAccessTokensAsync(fixture.RM. NodeId, fixture.CEM.NodeId))
                    Remember($"a pending access token of the RM ({When})",  pending.Token.Value);

            }

            // The CEM issues a dynamic pairing token, the RM pairs with it: the token is the HMAC
            // key of the challenge and must never appear anywhere.
            var pairingToken = fixture.MakeCEMPairable();
            Remember("the dynamic pairing token of the CEM", pairingToken.Value);

            await fixture.PairRMWithCEMAsync(pairingToken);
            await RememberTokensAsync("after the pairing");

            // The session initiation rotates the access token, so both generations are collected.
            await fixture.WaitForSessionsAsync();
            await RememberTokensAsync("after the session initiation");

            // Unpairing closes the session and removes the security material on both sides.
            var unpaired = await fixture.RM.UnpairAsync(fixture.CEM.NodeId);
            Assert.That(unpaired, Is.True);

            await S2NodeFixture.WaitUntil(() => fixture.RM.Sessions.Count == 0, ReconnectTimeout);

            var log = capturing.AllText;

            Assert.Multiple(() => {

                // A log that says nothing leaks nothing: the run must have logged, and it must
                // have logged something useful.
                Assert.That(capturing.Lines, Is.Not.Empty,
                            "the nodes logged nothing at all, so the grep below proves nothing");

                Assert.That(log, Does.Contain(fixture.CEM.NodeId.ToString()),
                            "the identification of the CEM node is no secret and should be diagnosable");

                Assert.That(log, Does.Contain(fixture.RM. NodeId.ToString()),
                            "the identification of the RM node is no secret and should be diagnosable");

                Assert.That(secrets, Is.Not.Empty,
                            "the test collected no secret at all");

                // ... and none of the secrets of this run reached it.
                foreach (var (secret, description) in secrets)
                    Assert.That(log, Does.Not.Contain(secret),
                                $"{description} leaked into the log: {capturing.FirstLineContaining(secret)}");

            });

        }

        #endregion

        #region WithoutRedaction_TheSameEntryLeaksTheSecret()

        [Test]
        public async Task WithoutRedaction_TheSameEntryLeaksTheSecret()
        {

            // A made-up access token, in the JSON shape the pairing process transports it in.
            const String accessToken  = "Zm9yLXRoZS1uZWdhdGl2ZS1jb250cm9sLW9ubHk9";

            var body                  = ConnectionDetailsBody(accessToken);

            using var redactedLog     = new CapturingLoggerFactory();
            using var plainLog        = new CapturingLoggerFactory();

            // Two identical CEM nodes: one redacting (the default), one not.
            await using var redactingNode  = new CEMNode(HostedCEM(), CEMOptions(),                                       LoggerFactory: redactedLog);
            await using var plainNode      = new CEMNode(HostedCEM(), CEMOptions() with { RedactSecretsInLogs = false },  LoggerFactory: plainLog);

            Assert.That(redactingNode.LoggerFactory, Is.Not.Null, "the redacting node kept no logger factory");
            Assert.That(plainNode.    LoggerFactory, Is.Not.Null, "the plain node kept no logger factory");

            redactingNode.LoggerFactory!.CreateLogger("S2 test").LogInformation("S2 test: postConnectionDetails {Body}", body);
            plainNode.    LoggerFactory!.CreateLogger("S2 test").LogInformation("S2 test: postConnectionDetails {Body}", body);

            Assert.Multiple(() => {

                // The control: without the redaction the very same entry carries the secret, so
                // the grep of the test above is not vacuous.
                Assert.That(plainLog.AllText,    Does.Contain(accessToken),
                            "the capturing logger did not even record the secret it was given");

                // With the redaction the secret is masked and the rest stays readable.
                Assert.That(redactedLog.AllText, Does.Not.Contain(accessToken),
                            $"the access token leaked: {redactedLog.FirstLineContaining(accessToken)}");

                Assert.That(redactedLog.AllText, Does.Contain(S2LogRedaction.Mask));
                Assert.That(redactedLog.AllText, Does.Contain("192.168.1.7"));

            });

        }

        #endregion

        #region RedactSecretsInLogs_DefaultsToTrue_AndSelectsTheLoggerFactory()

        [Test]
        public async Task RedactSecretsInLogs_DefaultsToTrue_AndSelectsTheLoggerFactory()
        {

            using var capturing = new CapturingLoggerFactory();

            var defaultOptions             = CEMOptions();

            await using var redactingNode  = new CEMNode(HostedCEM(), defaultOptions,                                       LoggerFactory: capturing);
            await using var plainNode      = new CEMNode(HostedCEM(), defaultOptions with { RedactSecretsInLogs = false },  LoggerFactory: capturing);
            await using var quietNode      = new CEMNode(HostedCEM(), defaultOptions);

            Assert.Multiple(() => {

                Assert.That(defaultOptions.RedactSecretsInLogs, Is.True,
                            "the redaction of the secrets is on by default (PLAN.md §3.6)");

                // The node wraps the given factory once, keeping the given one inside...
                Assert.That(redactingNode.LoggerFactory,  Is.InstanceOf<S2RedactingLoggerFactory>());
                Assert.That(((S2RedactingLoggerFactory) redactingNode.LoggerFactory!).InnerFactory, Is.SameAs(capturing));

                // ... and hands out the plain one when the redaction is switched off.
                Assert.That(plainNode.LoggerFactory,      Is.SameAs(capturing));

                // No logger factory stays no logger factory: the wrapper does not invent one.
                Assert.That(quietNode.LoggerFactory,      Is.Null);

            });

        }

        #endregion

    }

}

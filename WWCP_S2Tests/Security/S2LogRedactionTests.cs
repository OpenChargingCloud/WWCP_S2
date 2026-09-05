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
using System.Diagnostics;
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// The redaction layer of S2 Connect (PLAN.md §3.6): the three shapes in which a secret
    /// reaches a log (a secret JSON property, an HTTP Authorization header and an unquoted
    /// "name=value" pair), everything that must stay readable, the JSON redaction that never
    /// touches the given token, the secret names and the truncated SHA-256 fingerprints of
    /// the audit records.
    ///
    /// <para>
    /// The two smoke tests of <c>HardeningSmokeTests</c> cover the happy path of a pairing
    /// body and of a "Authorization: Bearer ..." PDU; this fixture covers the edges.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class S2LogRedactionTests
    {

        #region Redact_ASecretJSONProperty_MasksTheValue()

        /// <summary>
        /// The first shape: a secret JSON property of a logged request or response body.
        /// Whitespace around the colon is irrelevant, escaped quotes within the value do not
        /// end it, and the redacted property is always written back in canonical form.
        /// </summary>
        [Test]
        public void Redact_ASecretJSONProperty_MasksTheValue()
        {

            var noSpace      = S2LogRedaction.Redact("""{"accessToken":"n0-space"}""");
            var oneSpace     = S2LogRedaction.Redact("""{"accessToken": "0ne-space"}""");
            var manySpaces   = S2LogRedaction.Redact("""{"accessToken":     "m4ny-spaces"}""");
            var newLine      = S2LogRedaction.Redact("{\"accessToken\":\r\n  \"0n-1ts-0wn-line\"}");
            var escapedQuote = S2LogRedaction.Redact("""{"accessToken": "a\"b\\c"}""");

            Assert.Multiple(() => {

                Assert.That(noSpace,       Is.EqualTo("""{"accessToken": "[REDACTED]"}"""));
                Assert.That(oneSpace,      Is.EqualTo("""{"accessToken": "[REDACTED]"}"""));
                Assert.That(manySpaces,    Is.EqualTo("""{"accessToken": "[REDACTED]"}"""));
                Assert.That(newLine,       Is.EqualTo("""{"accessToken": "[REDACTED]"}"""));

                // An escaped quote inside the value does not end the value.
                Assert.That(escapedQuote,  Is.EqualTo("""{"accessToken": "[REDACTED]"}"""));

            });

        }

        #endregion

        #region Redact_ASecretJSONPropertyWithANumberOrABoolean_MasksTheValue()

        /// <summary>
        /// A secret does not have to be a JSON string: a number or a boolean is masked as well
        /// (and becomes a string, which keeps the redacted body valid JSON).
        /// </summary>
        [Test]
        public void Redact_ASecretJSONPropertyWithANumberOrABoolean_MasksTheValue()
        {

            var integer   = S2LogRedaction.Redact("""{"accessToken": 1234567}""");
            var negative  = S2LogRedaction.Redact("""{"accessToken": -1.5e3}""");
            var boolTrue  = S2LogRedaction.Redact("""{"accessToken": true}""");
            var boolFalse = S2LogRedaction.Redact("""{"accessToken": false}""");

            Assert.Multiple(() => {

                Assert.That(integer,    Is.EqualTo("""{"accessToken": "[REDACTED]"}"""));
                Assert.That(negative,   Is.EqualTo("""{"accessToken": "[REDACTED]"}"""));
                Assert.That(boolTrue,   Is.EqualTo("""{"accessToken": "[REDACTED]"}"""));
                Assert.That(boolFalse,  Is.EqualTo("""{"accessToken": "[REDACTED]"}"""));

                Assert.That(integer,    Does.Not.Contain("1234567"));
                Assert.That(negative,   Does.Not.Contain("1.5e3"));

            });

        }

        #endregion

        #region Redact_ASecretNestedInAnObjectOrAnArray_MasksTheValue()

        /// <summary>
        /// The regular expressions do not care about the depth: a secret nested in an object
        /// or in an array of objects is masked just as a top level one.
        /// </summary>
        [Test]
        public void Redact_ASecretNestedInAnObjectOrAnArray_MasksTheValue()
        {

            var nested   = S2LogRedaction.Redact("""{"connectionDetails": {"accessToken": "d33p-s3cr3t", "host": "192.168.1.7"}}""") ?? "";
            var inArray  = S2LogRedaction.Redact("""{"pendingTokens": [{"accessToken": "1n-arr4y"}, {"accessToken": "als0-1n-arr4y"}]}""") ?? "";

            Assert.Multiple(() => {

                Assert.That(nested,   Does.Not.Contain("d33p-s3cr3t"));
                Assert.That(nested,   Does.Contain("""{"accessToken": "[REDACTED]", "host": "192.168.1.7"}"""));

                Assert.That(inArray,  Does.Not.Contain("1n-arr4y"));
                Assert.That(inArray,  Does.Not.Contain("als0-1n-arr4y"));
                Assert.That(inArray,  Is.EqualTo("""{"pendingTokens": [{"accessToken": "[REDACTED]"}, {"accessToken": "[REDACTED]"}]}"""));

            });

        }

        #endregion

        #region Redact_AllS2ConnectSecrets_AreMasked()

        /// <summary>
        /// Every secret name of the pairing and of the session initiation flow, in the JSON
        /// shape: none of the values survives, all of the names do.
        /// </summary>
        [Test]
        public void Redact_AllS2ConnectSecrets_AreMasked()
        {

            var body      = """
                            {"pairingAttemptId": "V4L-4TT3MPT", "pairingToken": "V4L-P41RT0K3N", "pairingCode": "V4L-C0D3", "clientHmacChallenge": "V4L-CCH", "clientHmacChallengeResponse": "V4L-CCHR", "serverHmacChallenge": "V4L-SCH", "serverHmacChallengeResponse": "V4L-SCHR", "accessToken": "V4L-4CC3SS", "websocketToken": "V4L-W3BS0CK3T", "communicationToken": "V4L-C0MM"}
                            """;

            var redacted  = S2LogRedaction.Redact(body) ?? "";

            String[] names   = ["pairingAttemptId", "pairingToken", "pairingCode",
                                "clientHmacChallenge", "clientHmacChallengeResponse",
                                "serverHmacChallenge", "serverHmacChallengeResponse",
                                "accessToken", "websocketToken", "communicationToken"];

            String[] secrets = ["V4L-4TT3MPT", "V4L-P41RT0K3N", "V4L-C0D3", "V4L-CCH", "V4L-CCHR",
                                "V4L-SCH", "V4L-SCHR", "V4L-4CC3SS", "V4L-W3BS0CK3T", "V4L-C0MM"];

            Assert.Multiple(() => {

                foreach (var secret in secrets)
                    Assert.That(redacted, Does.Not.Contain(secret), $"the value of '{secret}' still is in the text");

                // The names stay readable: a redacted log must still say what was there.
                foreach (var name in names)
                    Assert.That(redacted, Does.Contain($"\"{name}\": \"[REDACTED]\""), $"the property '{name}' is not masked");

            });

        }

        #endregion

        #region Redact_AJSONNullValue_StaysNull()

        /// <summary>
        /// "The property is absent" is no secret, and a masked null would be misleading:
        /// a null valued secret property is left alone.
        /// </summary>
        [Test]
        public void Redact_AJSONNullValue_StaysNull()
        {

            var body      = """{"accessToken": null, "pairingToken": null, "clientNodeId": "node-1"}""";

            var redacted  = S2LogRedaction.Redact(body);

            Assert.Multiple(() => {
                Assert.That(redacted,  Is.EqualTo(body));
                Assert.That(redacted,  Does.Not.Contain(S2LogRedaction.Mask));
            });

        }

        #endregion

        #region Redact_KeepsTheNamesAndTheNonSecretValues()

        /// <summary>
        /// A redacted log has to stay useful: every property name, every non-secret value and
        /// every structural character survives.
        /// </summary>
        [Test]
        public void Redact_KeepsTheNamesAndTheNonSecretValues()
        {

            var body      = """{"nodeId": "6f2f5c1e-0000-4000-8000-000000000001", "clientNodeId": "node-1", "host": "rm.example.org", "port": 8443, "accessToken": "n0b0dy-sh0uld-s33-m3", "supportedS2MessageVersions": ["1.0.0"]}""";

            var redacted  = S2LogRedaction.Redact(body) ?? "";

            Assert.Multiple(() => {

                Assert.That(redacted,  Does.Not.Contain("n0b0dy-sh0uld-s33-m3"));

                Assert.That(redacted,  Does.Contain("\"nodeId\": \"6f2f5c1e-0000-4000-8000-000000000001\""));
                Assert.That(redacted,  Does.Contain("\"clientNodeId\": \"node-1\""));
                Assert.That(redacted,  Does.Contain("\"host\": \"rm.example.org\""));
                Assert.That(redacted,  Does.Contain("\"port\": 8443"));
                Assert.That(redacted,  Does.Contain("\"supportedS2MessageVersions\": [\"1.0.0\"]"));

                // The name of the secret is diagnostic information, only its value is not.
                Assert.That(redacted,  Does.Contain("\"accessToken\""));

            });

        }

        #endregion


        #region Redact_AnAuthorizationHeaderAtTheStartOfTheText_MasksTheValue()

        /// <summary>
        /// The second shape: an HTTP Authorization header of a logged PDU. The header may be
        /// the very first thing in the text (a header dump without a request line) and it may
        /// be indented, as Hermod indents the headers of its traces.
        /// </summary>
        [Test]
        public void Redact_AnAuthorizationHeaderAtTheStartOfTheText_MasksTheValue()
        {

            var atStart   = S2LogRedaction.Redact("Authorization: Bearer 0a1b2c3d4e5f60718293a4b5c6d7e8f9");
            var indented  = S2LogRedaction.Redact("    Authorization: Bearer 0a1b2c3d4e5f60718293a4b5c6d7e8f9");
            var noSpace   = S2LogRedaction.Redact("Authorization:Bearer 0a1b2c3d4e5f60718293a4b5c6d7e8f9");

            Assert.Multiple(() => {

                Assert.That(atStart,   Is.EqualTo("Authorization: Bearer [REDACTED]"));
                Assert.That(indented,  Is.EqualTo("    Authorization: Bearer [REDACTED]"));
                Assert.That(noSpace,   Is.EqualTo("Authorization:Bearer [REDACTED]"));

            });

        }

        #endregion

        #region Redact_ALowerCaseAndAProxyAuthorizationHeader_AreMasked()

        /// <summary>
        /// HTTP/2 and HTTP/3 write header names in lower case and a proxy adds its own
        /// authorization header: both are masked, both keep their scheme, and every other
        /// header of the PDU is untouched.
        /// </summary>
        [Test]
        public void Redact_ALowerCaseAndAProxyAuthorizationHeader_AreMasked()
        {

            var pdu       = String.Join(
                                "\r\n",
                                "GET /s2 HTTP/1.1",
                                "Host: rm.example.org:8443",
                                "authorization: bearer l0w3rc4s3-t0k3n",
                                "Proxy-Authorization: Basic cHJveHk6c2VjcmV0",
                                "Sec-WebSocket-Protocol: s2",
                                ""
                            );

            var redacted  = S2LogRedaction.Redact(pdu) ?? "";

            Assert.Multiple(() => {

                Assert.That(redacted,  Does.Not.Contain("l0w3rc4s3-t0k3n"));
                Assert.That(redacted,  Does.Not.Contain("cHJveHk6c2VjcmV0"));

                // The scheme is diagnostic information and is kept, as is the original casing.
                Assert.That(redacted,  Does.Contain("authorization: bearer [REDACTED]"));
                Assert.That(redacted,  Does.Contain("Proxy-Authorization: Basic [REDACTED]"));

                Assert.That(redacted,  Does.Contain("GET /s2 HTTP/1.1"));
                Assert.That(redacted,  Does.Contain("Host: rm.example.org:8443"));
                Assert.That(redacted,  Does.Contain("Sec-WebSocket-Protocol: s2"));

            });

        }

        #endregion

        #region Redact_AnAuthorizationHeaderWithoutAValue_IsLeftAlone()

        /// <summary>
        /// An empty authorization header holds nothing to redact, so it stays as it is: a
        /// "[REDACTED]" behind an empty header would invent a credential that never existed.
        /// A scheme without a value, however, cannot be told apart from a value and is masked.
        /// </summary>
        [Test]
        public void Redact_AnAuthorizationHeaderWithoutAValue_IsLeftAlone()
        {

            var empty       = S2LogRedaction.Redact("Authorization:");
            var trailing    = S2LogRedaction.Redact("Authorization: ");
            var withinAPDU  = S2LogRedaction.Redact("Host: rm.example.org\r\nAuthorization:\r\nAccept: application/json");
            var schemeOnly  = S2LogRedaction.Redact("Authorization: Bearer");

            Assert.Multiple(() => {

                Assert.That(empty,       Is.EqualTo("Authorization:"));
                Assert.That(trailing,    Is.EqualTo("Authorization: "));
                Assert.That(withinAPDU,  Is.EqualTo("Host: rm.example.org\r\nAuthorization:\r\nAccept: application/json"));

                // Without a following value the scheme itself is treated as the value.
                Assert.That(schemeOnly,  Is.EqualTo("Authorization: [REDACTED]"));

            });

        }

        #endregion


        #region Redact_AQueryString_MasksOnlyTheSecretParameter()

        /// <summary>
        /// The third shape: an unquoted "name=value" pair, e.g. a query string of a logged
        /// request line. The value ends at the parameter separator, so everything behind it
        /// survives.
        /// </summary>
        [Test]
        public void Redact_AQueryString_MasksOnlyTheSecretParameter()
        {

            var redacted = S2LogRedaction.Redact("GET /v1/nodes?accessToken=s3cr3t-t0k3n&foo=bar HTTP/1.1");

            Assert.That(redacted, Is.EqualTo("GET /v1/nodes?accessToken=[REDACTED]&foo=bar HTTP/1.1"));

        }

        #endregion

        #region Redact_ALogLine_MasksTheValueUpToTheDelimiter()

        /// <summary>
        /// An unquoted "name: value" pair of a log line: the value ends at the comma, the
        /// name and everything that is no secret stay readable.
        /// </summary>
        [Test]
        public void Redact_ALogLine_MasksTheValueUpToTheDelimiter()
        {

            var redacted = S2LogRedaction.Redact("pairing attempt started: pairingAttemptId: 1234abcd, nodeId: node-1, delay: 250 ms") ?? "";

            Assert.Multiple(() => {

                Assert.That(redacted,  Does.Not.Contain("1234abcd"));
                Assert.That(redacted,  Does.Contain("pairingAttemptId: [REDACTED],"));
                Assert.That(redacted,  Does.Contain("nodeId: node-1"));
                Assert.That(redacted,  Does.Contain("delay: 250 ms"));

            });

        }

        #endregion

        #region Redact_ASecretNameThatIsOnlyASuffix_IsNotMasked()

        /// <summary>
        /// The unquoted shape requires a word boundary in front of the name, so a longer
        /// identifier that merely ends with a secret name keeps its value.
        /// </summary>
        [Test]
        public void Redact_ASecretNameThatIsOnlyASuffix_IsNotMasked()
        {

            var redacted = S2LogRedaction.Redact("myAccessToken=n0t-4-s3cr3t, x-accessToken=1nd33d-4-s3cr3t") ?? "";

            Assert.Multiple(() => {
                Assert.That(redacted,  Does.Contain("myAccessToken=n0t-4-s3cr3t"));
                Assert.That(redacted,  Does.Contain("x-accessToken=[REDACTED]"),  "a hyphen is a word boundary");
            });

        }

        #endregion

        #region Redact_AGenericTokenOrSecretInAQueryString_IsNotMasked()

        /// <summary>
        /// The generic names "token" and "secret" are masked in both shapes, so the "?token=..."
        /// of a logged URL does not survive. "authorization" is the deliberate exception: it is
        /// masked as a header, where the scheme is kept, and an "Authorization=..." pair without
        /// a colon is no HTTP header and is left alone rather than losing the scheme.
        /// </summary>
        [Test]
        public void Redact_AGenericTokenOrSecretInAQueryString_IsMasked()
        {

            var inJSON    = S2LogRedaction.Redact("""{"token": "j50n-t0k3n", "secret": "j50n-s3cr3t"}""") ?? "";

            var inQuery   = S2LogRedaction.Redact("wss://rm.example.org/s2?token=qu3ry-t0k3n") ?? "";
            var inPair    = S2LogRedaction.Redact("secret=qu3ry-s3cr3t") ?? "";
            var asAPair   = S2LogRedaction.Redact("Authorization=Bearer n0-c0l0n-n0-h34d3r") ?? "";

            Assert.Multiple(() => {

                // The JSON shape knows the generic names.
                Assert.That(inJSON,   Does.Not.Contain("j50n-t0k3n"));
                Assert.That(inJSON,   Does.Not.Contain("j50n-s3cr3t"));

                // And so does the unquoted shape.
                Assert.That(inQuery,  Is.EqualTo("wss://rm.example.org/s2?token=[REDACTED]"));
                Assert.That(inPair,   Is.EqualTo("secret=[REDACTED]"));

                // Except for "authorization" without a colon, which is no HTTP header.
                Assert.That(asAPair,  Is.EqualTo("Authorization=Bearer n0-c0l0n-n0-h34d3r"));

            });

        }

        #endregion


        #region Redact_ATextWithoutASecret_IsUnchanged()

        /// <summary>
        /// A text that holds no secret is returned as it is: redaction must not cost the
        /// readability of the 99 % of the log lines that are harmless.
        /// </summary>
        [Test]
        public void Redact_ATextWithoutASecret_IsUnchanged()
        {

            var text      = "POST /v1/requestPairing from node-1 (192.168.1.7) -> 200 OK in 12 ms, nodeId: 6f2f5c1e-0000-4000-8000-000000000001";

            var redacted  = S2LogRedaction.Redact(text);

            Assert.Multiple(() => {
                Assert.That(redacted,  Is.EqualTo(text));
                Assert.That(redacted,  Does.Not.Contain(S2LogRedaction.Mask));
            });

        }

        #endregion

        #region Redact_NullOrEmpty_IsReturnedUnchanged()

        /// <summary>
        /// Nothing in, nothing out: the redaction of a log message must never turn a null
        /// into an empty text or an empty text into a mask.
        /// </summary>
        [Test]
        public void Redact_NullOrEmpty_IsReturnedUnchanged()
        {

            var ofNull   = S2LogRedaction.Redact(null);
            var ofEmpty  = S2LogRedaction.Redact("");

            Assert.Multiple(() => {
                Assert.That(ofNull,   Is.Null);
                Assert.That(ofEmpty,  Is.Empty);
            });

        }

        #endregion

        #region Redact_Twice_IsIdempotentForJSONAndForHeaders()

        /// <summary>
        /// A PDU may well pass the redaction more than once (the message and the state of a
        /// log entry, a proxy logging what it forwards, ...): for the JSON shape and for the
        /// header shape redacting twice is the same as redacting once.
        /// </summary>
        [Test]
        public void Redact_Twice_IsIdempotentForJSONAndForHeaders()
        {

            var pdu    = String.Join(
                             "\r\n",
                             "POST /pairing/v1/finalizePairing HTTP/1.1",
                             "Host: rm.example.org:8443",
                             "Authorization: Bearer 0a1b2c3d4e5f60718293a4b5c6d7e8f9",
                             "",
                             """{"clientNodeId": "node-1", "accessToken": "s3cr3t-access-token", "count": 3}"""
                         );

            var once   = S2LogRedaction.Redact(pdu);
            var twice  = S2LogRedaction.Redact(once);

            Assert.Multiple(() => {

                Assert.That(twice,  Is.EqualTo(once),  "redacting twice differs from redacting once");

                Assert.That(once,   Does.Not.Contain("0a1b2c3d4e5f60718293a4b5c6d7e8f9"));
                Assert.That(once,   Does.Not.Contain("s3cr3t-access-token"));

            });

        }

        #endregion

        #region Redact_Twice_OfAnUnquotedPair_IsIdempotent()

        /// <summary>
        /// The value pattern of the unquoted shape excludes "]", so without a guard it would
        /// match the mask it had written itself, one character too short, and every further
        /// redaction would append another "]". A negative lookahead for the mask keeps the
        /// redaction idempotent, which matters because a message may pass more than one
        /// redacting logger.
        /// </summary>
        [Test]
        public void Redact_Twice_OfAnUnquotedPair_IsIdempotent()
        {

            var once        = S2LogRedaction.Redact("GET /v1/x?accessToken=s3cr3t-t0k3n&foo=bar");
            var twice       = S2LogRedaction.Redact(once);
            var threeTimes  = S2LogRedaction.Redact(twice);

            Assert.Multiple(() => {

                Assert.That(once,        Is.EqualTo("GET /v1/x?accessToken=[REDACTED]&foo=bar"));

                Assert.That(twice,       Is.EqualTo(once),  "redacting twice must not change the text again");
                Assert.That(threeTimes,  Is.EqualTo(once),  "and it must stay stable");

                // Whatever happens to the brackets, the secret never comes back.
                Assert.That(twice,       Does.Not.Contain("s3cr3t-t0k3n"));
                Assert.That(threeTimes,  Does.Not.Contain("s3cr3t-t0k3n"));

            });

        }

        #endregion

        #region Redact_ALongAdversarialText_IsFastAndNeverThrows()

        /// <summary>
        /// A logged PDU is attacker influenced, so the redaction must not be a denial of
        /// service: 100.000 characters of nested quotes and backslashes, ending in an
        /// unterminated escape sequence, are redacted well within a second and never throw.
        /// </summary>
        [Test]
        public void Redact_ALongAdversarialText_IsFastAndNeverThrows()
        {

            var builder = new StringBuilder(120_000);

            while (builder.Length < 100_000)
                builder.Append("""{"clientNodeId": "node-1", "accessToken": "a\"b\\c", "note": "\\", "pairingAttemptId": "x"} """);

            // An unterminated, deeply escaped value: the worst case of the JSON value pattern.
            builder.Append("{\"accessToken\": \"").Append(new String('\\', 512));

            var adversarial  = builder.ToString();

            var stopwatch    = Stopwatch.StartNew();
            var redacted     = S2LogRedaction.Redact(adversarial) ?? "";
            stopwatch.Stop();

            Assert.Multiple(() => {

                Assert.That(adversarial.Length,  Is.GreaterThan(100_000));
                Assert.That(stopwatch.Elapsed,   Is.LessThan(TimeSpan.FromSeconds(1)),  $"the redaction took {stopwatch.ElapsedMilliseconds} ms");

                Assert.That(redacted,            Does.Contain(S2LogRedaction.Mask));
                Assert.That(redacted,            Does.Not.Contain("""a\"b\\c"""));

            });

        }

        #endregion


        #region RedactJSON_DoesNotModifyTheGivenJSON()

        /// <summary>
        /// The redaction works on a deep copy: the caller keeps a usable object, so a body
        /// that is logged is not damaged for the code that still has to process it.
        /// </summary>
        [Test]
        public void RedactJSON_DoesNotModifyTheGivenJSON()
        {

            var json      = new JObject(
                                new JProperty("clientNodeId",  "node-1"),
                                new JProperty("accessToken",   "s3cr3t-access-token")
                            );

            var redacted  = S2LogRedaction.RedactJSON(json) as JObject;

            Assert.Multiple(() => {

                Assert.That(json["accessToken"]?.  Value<String>(),  Is.EqualTo("s3cr3t-access-token"),  "the given JSON was modified");
                Assert.That(json["clientNodeId"]?. Value<String>(),  Is.EqualTo("node-1"));

                Assert.That(redacted,                                Is.Not.Null);
                Assert.That(redacted,                                Is.Not.SameAs(json));
                Assert.That(redacted!["accessToken"]?.Value<String>(),   Is.EqualTo(S2LogRedaction.Mask));
                Assert.That(redacted!["clientNodeId"]?.Value<String>(),  Is.EqualTo("node-1"));

            });

        }

        #endregion

        #region RedactJSON_MasksSecretsAtAnyDepthAndInsideArrays()

        /// <summary>
        /// Every secret property is masked, whatever its depth, and an array is descended
        /// into element by element - also when the array is the root of the document.
        /// </summary>
        [Test]
        public void RedactJSON_MasksSecretsAtAnyDepthAndInsideArrays()
        {

            var json       = JObject.Parse("""{"clientNodeId": "node-1", "connectionDetails": {"accessToken": "d33p-s3cr3t", "host": "192.168.1.7", "inner": {"websocketToken": "d33p3r-s3cr3t"}}, "pendingTokens": [{"accessToken": "1n-arr4y"}, {"accessToken": "als0-1n-arr4y"}], "pairingAttemptId": "p41r1ng-4tt3mpt"}""");

            var redacted   = S2LogRedaction.RedactJSON(json) as JObject;

            var rootArray  = S2LogRedaction.RedactJSON(
                                 new JArray(
                                     new JObject(new JProperty("accessToken", "1n-r00t-4rr4y"))
                                 )
                             ) as JArray;

            Assert.Multiple(() => {

                Assert.That(redacted!["clientNodeId"]?.Value<String>(),                                 Is.EqualTo("node-1"));
                Assert.That(redacted!["connectionDetails"]?["accessToken"]?.Value<String>(),            Is.EqualTo(S2LogRedaction.Mask));
                Assert.That(redacted!["connectionDetails"]?["host"]?.Value<String>(),                   Is.EqualTo("192.168.1.7"));
                Assert.That(redacted!["connectionDetails"]?["inner"]?["websocketToken"]?.Value<String>(), Is.EqualTo(S2LogRedaction.Mask));
                Assert.That(redacted!["pendingTokens"]?[0]?["accessToken"]?.Value<String>(),            Is.EqualTo(S2LogRedaction.Mask));
                Assert.That(redacted!["pendingTokens"]?[1]?["accessToken"]?.Value<String>(),            Is.EqualTo(S2LogRedaction.Mask));
                Assert.That(redacted!["pairingAttemptId"]?.Value<String>(),                             Is.EqualTo(S2LogRedaction.Mask));

                Assert.That(redacted!.ToString(),                                                       Does.Not.Contain("d33p-s3cr3t"));
                Assert.That(redacted!.ToString(),                                                       Does.Not.Contain("d33p3r-s3cr3t"));

                Assert.That(rootArray![0]?["accessToken"]?.Value<String>(),                             Is.EqualTo(S2LogRedaction.Mask));

            });

        }

        #endregion

        #region RedactJSON_ANullValuedSecret_StaysNull()

        /// <summary>
        /// A null valued secret property stays null, exactly as in the textual redaction.
        /// </summary>
        [Test]
        public void RedactJSON_ANullValuedSecret_StaysNull()
        {

            var json      = JObject.Parse("""{"accessToken": null, "clientNodeId": "node-1"}""");

            var redacted  = S2LogRedaction.RedactJSON(json) as JObject;

            Assert.Multiple(() => {
                Assert.That(redacted!["accessToken"]?.Type,               Is.EqualTo(JTokenType.Null));
                Assert.That(redacted!["clientNodeId"]?.Value<String>(),   Is.EqualTo("node-1"));
            });

        }

        #endregion

        #region RedactJSON_ANonObjectToken_IsReturnedAsIs()

        /// <summary>
        /// The JSON redaction masks by property name, so a token that is neither an object nor
        /// an array comes back with its value - even when the value itself looks like a secret
        /// (that is what the textual redaction is for).
        /// </summary>
        [Test]
        public void RedactJSON_ANonObjectToken_IsReturnedAsIs()
        {

            var text      = S2LogRedaction.RedactJSON(new JValue("accessToken=n0t-4-pr0p3rty"));
            var number    = S2LogRedaction.RedactJSON(new JValue(42));

            Assert.Multiple(() => {
                Assert.That(text?.Type,             Is.EqualTo(JTokenType.String));
                Assert.That(text?.Value<String>(),  Is.EqualTo("accessToken=n0t-4-pr0p3rty"));
                Assert.That(number?.Value<Int32>(), Is.EqualTo(42));
            });

        }

        #endregion

        #region RedactJSON_Null_ReturnsNull()

        /// <summary>
        /// No JSON, nothing to redact.
        /// </summary>
        [Test]
        public void RedactJSON_Null_ReturnsNull()
        {

            var redacted = S2LogRedaction.RedactJSON(null);

            Assert.That(redacted, Is.Null);

        }

        #endregion

        #region RedactJSON_KeepsThePropertyOrderAndTheNonSecretValues()

        /// <summary>
        /// Only the values of the secret properties change: the order of the properties and
        /// every other value are those of the original document.
        /// </summary>
        [Test]
        public void RedactJSON_KeepsThePropertyOrderAndTheNonSecretValues()
        {

            var json      = JObject.Parse("""{"a": 1, "accessToken": "s3cr3t-access-token", "z": "last", "nodeId": "node-1", "flag": true}""");

            var redacted  = S2LogRedaction.RedactJSON(json) as JObject;

            Assert.Multiple(() => {

                Assert.That(redacted!.Properties().Select(property => property.Name),
                            Is.EqualTo(new[] { "a", "accessToken", "z", "nodeId", "flag" }));

                Assert.That(redacted!["a"]?.    Value<Int32>(),    Is.EqualTo(1));
                Assert.That(redacted!["z"]?.    Value<String>(),   Is.EqualTo("last"));
                Assert.That(redacted!["nodeId"]?.Value<String>(),  Is.EqualTo("node-1"));
                Assert.That(redacted!["flag"]?. Value<Boolean>(),  Is.True);

            });

        }

        #endregion


        #region SecretNames_ContainTheS2ConnectSecrets()

        /// <summary>
        /// The list of the secret names carries the tokens and challenge responses of
        /// S2 Connect (PLAN.md §3.6) and nothing that is merely an identifier.
        /// </summary>
        [Test]
        public void SecretNames_ContainTheS2ConnectSecrets()
        {

            Assert.Multiple(() => {

                Assert.That(S2LogRedaction.SecretNames,  Does.Contain("pairingAttemptId"));
                Assert.That(S2LogRedaction.SecretNames,  Does.Contain("pairingToken"));
                Assert.That(S2LogRedaction.SecretNames,  Does.Contain("accessToken"));
                Assert.That(S2LogRedaction.SecretNames,  Does.Contain("websocketToken"));
                Assert.That(S2LogRedaction.SecretNames,  Does.Contain("clientHmacChallengeResponse"));
                Assert.That(S2LogRedaction.SecretNames,  Does.Contain("serverHmacChallengeResponse"));

                // Identifiers are no secrets: masking them would make every log useless.
                Assert.That(S2LogRedaction.SecretNames,  Does.Not.Contain("nodeId"));
                Assert.That(S2LogRedaction.SecretNames,  Does.Not.Contain("clientNodeId"));

            });

        }

        #endregion

        #region IsSecretName_IsCaseInsensitive()

        /// <summary>
        /// The names on the wire are camel case, the names of a structured log entry are
        /// Pascal case, and a hand written log line may be anything: the comparison ignores
        /// the case.
        /// </summary>
        [Test]
        public void IsSecretName_IsCaseInsensitive()
        {

            Assert.Multiple(() => {

                Assert.That(S2LogRedaction.IsSecretName("accessToken"),          Is.True);
                Assert.That(S2LogRedaction.IsSecretName("AccessToken"),          Is.True);
                Assert.That(S2LogRedaction.IsSecretName("ACCESSTOKEN"),          Is.True);
                Assert.That(S2LogRedaction.IsSecretName("PairingAttemptId"),     Is.True);

                Assert.That(S2LogRedaction.SecretNames.Contains("ACCESSTOKEN"),  Is.True,  "the set itself compares ignoring case");

            });

        }

        #endregion

        #region IsSecretName_OfAHarmlessNameOrNull_IsFalse()

        /// <summary>
        /// Everything that is not on the list is no secret, and a missing name is no secret
        /// either.
        /// </summary>
        [Test]
        public void IsSecretName_OfAHarmlessNameOrNull_IsFalse()
        {

            Assert.Multiple(() => {

                Assert.That(S2LogRedaction.IsSecretName("nodeId"),        Is.False);
                Assert.That(S2LogRedaction.IsSecretName("clientNodeId"),  Is.False);
                Assert.That(S2LogRedaction.IsSecretName("host"),          Is.False);
                Assert.That(S2LogRedaction.IsSecretName("pairingUrl"),    Is.False);

                Assert.That(S2LogRedaction.IsSecretName(null),            Is.False);
                Assert.That(S2LogRedaction.IsSecretName(""),              Is.False);

            });

        }

        #endregion


        #region Fingerprint_HasThePrefixAndTheDefaultLength()

        /// <summary>
        /// An audit record references a token by "sha256:" plus, by default, 12 hexadecimal
        /// characters: 48 bits, enough to correlate two log lines, far too few to recover the
        /// secret.
        /// </summary>
        [Test]
        public void Fingerprint_HasThePrefixAndTheDefaultLength()
        {

            var fingerprint = S2LogRedaction.Fingerprint("s3cr3t-access-token");
            var hex         = fingerprint[S2LogRedaction.FingerprintPrefix.Length..];

            Assert.Multiple(() => {

                Assert.That(S2LogRedaction.FingerprintPrefix,         Is.EqualTo("sha256:"));
                Assert.That(S2LogRedaction.DefaultFingerprintLength,  Is.EqualTo(12));
                Assert.That(S2LogRedaction.Mask,                      Is.EqualTo("[REDACTED]"));

                Assert.That(fingerprint,                              Does.StartWith("sha256:"));
                Assert.That(hex,                                      Has.Length.EqualTo(12));
                Assert.That(hex,                                      Does.Match("^[0-9a-f]+$"),  "lower case hexadecimal only");

            });

        }

        #endregion

        #region Fingerprint_IsDeterministicAndDiffersPerSecret()

        /// <summary>
        /// Two log lines of the same token get the same fingerprint (that is the whole point),
        /// two different tokens get different ones.
        /// </summary>
        [Test]
        public void Fingerprint_IsDeterministicAndDiffersPerSecret()
        {

            var first   = S2LogRedaction.Fingerprint("s3cr3t-access-token");
            var again   = S2LogRedaction.Fingerprint("s3cr3t-access-token");
            var another = S2LogRedaction.Fingerprint("s3cr3t-access-tokeN");
            var empty   = S2LogRedaction.Fingerprint("");

            Assert.Multiple(() => {

                Assert.That(again,    Is.EqualTo(first));
                Assert.That(another,  Is.Not.EqualTo(first),  "a single changed character changes the fingerprint");

                // The empty text is a secret like any other, and is not the null fingerprint.
                Assert.That(empty,    Is.EqualTo("sha256:e3b0c44298fc"));

            });

        }

        #endregion

        #region Fingerprint_IsTheTruncatedLowerCaseSHA256OfTheUTF8Bytes()

        /// <summary>
        /// Computed here independently of the implementation: the fingerprint is the first
        /// characters of the lower case hexadecimal SHA-256 of the UTF-8 bytes of the secret.
        /// The secret carries non-ASCII characters, so a different encoding would show.
        /// </summary>
        [Test]
        public void Fingerprint_IsTheTruncatedLowerCaseSHA256OfTheUTF8Bytes()
        {

            // Two characters that are two UTF-8 bytes each: a different encoding would show.
            var secret    = "s3cr3t-ümläut-token";

            var expected  = String.Concat(
                                SHA256.HashData(Encoding.UTF8.GetBytes(secret)).
                                       Select(digit => digit.ToString("x2"))
                            );

            Assert.Multiple(() => {

                Assert.That(S2LogRedaction.Fingerprint(secret, 64),  Is.EqualTo($"sha256:{expected}"));
                Assert.That(S2LogRedaction.Fingerprint(secret, 12),  Is.EqualTo($"sha256:{expected[..12]}"));
                Assert.That(S2LogRedaction.Fingerprint(secret),      Is.EqualTo($"sha256:{expected[..12]}"));

            });

        }

        #endregion

        #region Fingerprint_TheLength_IsClampedTo2To64()

        /// <summary>
        /// A caller cannot ask for the whole secret and cannot ask for nothing: the length is
        /// clamped to 2..64 hexadecimal characters (a SHA-256 has 64).
        /// </summary>
        [Test]
        public void Fingerprint_TheLength_IsClampedTo2To64()
        {

            var secret  = "s3cr3t-access-token";
            var full    = S2LogRedaction.Fingerprint(secret, 64)[S2LogRedaction.FingerprintPrefix.Length..];

            Assert.Multiple(() => {

                Assert.That(full,                                         Has.Length.EqualTo(64));

                Assert.That(S2LogRedaction.Fingerprint(secret,     0),    Is.EqualTo($"sha256:{full[..2]}"));
                Assert.That(S2LogRedaction.Fingerprint(secret,    -5),    Is.EqualTo($"sha256:{full[..2]}"));
                Assert.That(S2LogRedaction.Fingerprint(secret,     1),    Is.EqualTo($"sha256:{full[..2]}"));
                Assert.That(S2LogRedaction.Fingerprint(secret,     2),    Is.EqualTo($"sha256:{full[..2]}"));

                Assert.That(S2LogRedaction.Fingerprint(secret,   100),    Is.EqualTo($"sha256:{full}"));
                Assert.That(S2LogRedaction.Fingerprint(secret, 65535),    Is.EqualTo($"sha256:{full}"));

            });

        }

        #endregion

        #region Fingerprint_OfNull_IsTheEmptyFingerprint()

        /// <summary>
        /// A missing secret gets a fingerprint that cannot collide with any real one.
        /// </summary>
        [Test]
        public void Fingerprint_OfNull_IsTheEmptyFingerprint()
        {

            Assert.Multiple(() => {
                Assert.That(S2LogRedaction.Fingerprint(null),      Is.EqualTo("sha256:-"));
                Assert.That(S2LogRedaction.Fingerprint(null, 64),  Is.EqualTo("sha256:-"));
            });

        }

        #endregion

        #region Fingerprint_NeverContainsTheSecret()

        /// <summary>
        /// The whole reason for the fingerprint: it may be written to an audit record, so it
        /// must not carry the secret - not even at the full length of 64 characters.
        /// </summary>
        [Test]
        public void Fingerprint_NeverContainsTheSecret()
        {

            var secret = "s3cr3t-access-token";

            Assert.Multiple(() => {
                Assert.That(S2LogRedaction.Fingerprint(secret),      Does.Not.Contain(secret));
                Assert.That(S2LogRedaction.Fingerprint(secret, 64),  Does.Not.Contain(secret));
                Assert.That(S2LogRedaction.Fingerprint(secret, 64),  Does.Not.Contain("s3cr3t"));
            });

        }

        #endregion

    }

}

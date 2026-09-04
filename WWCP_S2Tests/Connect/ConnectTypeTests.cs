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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The S2 Connect data types and crypto primitives of Phase 5: identifiers, tokens,
    /// pairing codes, fingerprints, base URLs, the HMAC challenge-response function with
    /// known-answer vectors, and the common DTOs.
    /// </summary>
    [TestFixture]
    public sealed class ConnectTypeTests
    {

        #region Identifiers and aliases

        [Test]
        public void NodeId_IsACanonicalUUID()
        {

            var id = Node_Id.NewRandom;
            Assert.That(id.ToString(),                                                Has.Length.EqualTo(36));
            Assert.That(id.ToString(),                                                Is.EqualTo(id.ToString().ToLowerInvariant()));
            Assert.That(Node_Id.TryParse(id.ToString().ToUpperInvariant(), out var upper), Is.True);
            Assert.That(upper,                                                        Is.EqualTo(id));

            Assert.That(Node_Id.TryParse("not-a-uuid",                          out _), Is.False);
            Assert.That(Node_Id.TryParse("{6f2f5c1e-0000-4000-8000-000000000001}", out _), Is.False);
            Assert.That(Node_Id.TryParse("6f2f5c1e000040008000000000000001",      out _), Is.False);

        }

        [Test]
        [S2C("Pairing.NodeIdAlias")]
        public void NodeIdAlias_AcceptsLettersAndDigitsOnly()
        {
            Assert.That(NodeIdAlias.TryParse("A0",   out _), Is.True);
            Assert.That(NodeIdAlias.TryParse("a",    out _), Is.True);
            Assert.That(NodeIdAlias.TryParse("",     out _), Is.False);
            Assert.That(NodeIdAlias.TryParse("A-0",  out _), Is.False);
            Assert.That(NodeIdAlias.TryParse("A 0",  out _), Is.False);
            Assert.That(NodeIdAlias.Parse("A0"),             Is.Not.EqualTo(NodeIdAlias.Parse("a0")));
        }

        #endregion

        #region Pairing tokens and codes

        [Test]
        [S2C("Pairing.PairingToken")]
        public void PairingToken_LengthRules_AndGeneration()
        {

            Assert.That(PairingToken.TryParse("abc",    out _),          Is.False);
            Assert.That(PairingToken.TryParse("abcd",   out var dynamic), Is.True);
            Assert.That(dynamic.MeetsStaticRequirement,                   Is.False);
            Assert.That(PairingToken.TryParse("abcdef", out var isStatic), Is.True);
            Assert.That(isStatic.MeetsStaticRequirement,                  Is.True);
            Assert.That(PairingToken.TryParse("abc-d",  out _),          Is.False);
            Assert.That(PairingToken.TryParse("äbcdef", out _),          Is.False);

            var generated = PairingToken.NewDynamic();
            Assert.That(generated.Length,                                 Is.EqualTo(6));
            Assert.That(generated.Value.All(c => S2ConnectDefaults.PairingTokenAlphabet.Contains(c)), Is.True);
            Assert.That(PairingToken.NewStatic().Length,                  Is.EqualTo(8));
            Assert.That(() => PairingToken.NewDynamic(3),                 Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => PairingToken.NewStatic(5),                  Throws.TypeOf<ArgumentOutOfRangeException>());

            // Secrets never appear in ToString().
            Assert.That(generated.ToString(),                             Does.Not.Contain(generated.Value));
            Assert.That(PairingToken.Parse("abcd"),                       Is.EqualTo(PairingToken.Parse("abcd")));
            Assert.That(PairingToken.Parse("abcd"),                       Is.Not.EqualTo(PairingToken.Parse("ABCD")));

        }

        [Test]
        [S2C("Pairing.PairingCode")]
        public void PairingCode_SplitsAtTheDash()
        {

            Assert.That(PairingCode.TryParse("A0-K7Q2P9", out var withAlias), Is.True);
            Assert.That(withAlias.NodeIdAlias?.Value,                        Is.EqualTo("A0"));
            Assert.That(withAlias.PairingToken.Value,                        Is.EqualTo("K7Q2P9"));
            Assert.That(withAlias.Value,                                     Is.EqualTo("A0-K7Q2P9"));

            Assert.That(PairingCode.TryParse("K7Q2P9", out var tokenOnly),   Is.True);
            Assert.That(tokenOnly.NodeIdAlias,                               Is.Null);
            Assert.That(tokenOnly.PairingToken.Value,                        Is.EqualTo("K7Q2P9"));

            Assert.That(PairingCode.TryParse("A0-abc",   out _),             Is.False);   // token too short
            Assert.That(PairingCode.TryParse("A0-B1-C2", out _),             Is.False);   // two dashes
            Assert.That(PairingCode.TryParse("-K7Q2P9",  out _),             Is.False);
            Assert.That(PairingCode.TryParse("",         out _),             Is.False);

            Assert.That(withAlias.ToString(), Does.Not.Contain("K7Q2P9"));

        }

        #endregion

        #region Binary tokens

        [Test]
        [S2C("Pairing.PairingAttemptId")]
        public void PairingAttemptId_IsAtLeast32Characters_AndGeneratedFrom24Bytes()
        {

            var id = PairingAttemptId.NewRandom();
            Assert.That(id.Length,                                                   Is.EqualTo(32));
            Assert.That(Convert.FromBase64String(id.Value),                          Has.Length.EqualTo(24));
            Assert.That(PairingAttemptId.TryParse(new String('x', 31), out _),        Is.False);
            Assert.That(PairingAttemptId.TryParse(new String('x', 32), out _),        Is.True);
            Assert.That(PairingAttemptId.TryParse(new String('x', 31) + " ", out _),  Is.False);
            Assert.That(id.ToString(),                                               Does.Not.Contain(id.Value));
            Assert.That(PairingAttemptId.Parse(id.Value),                            Is.EqualTo(id));

        }

        [Test]
        public void AccessToken_IsStandardBase64_KeptVerbatim()
        {

            var token = AccessToken.NewRandom();
            Assert.That(token.Length,                                     Is.EqualTo(32));
            Assert.That(token.Value,                                      Has.Length.EqualTo(44));
            Assert.That(AccessToken.TryParse(token.Value, out var parsed), Is.True);
            Assert.That(parsed,                                           Is.EqualTo(token));
            Assert.That(parsed.Value,                                     Is.EqualTo(token.Value));

            Assert.That(AccessToken.TryParse("",             out _), Is.False);
            Assert.That(AccessToken.TryParse("abc",          out _), Is.False);   // length not a multiple of 4
            Assert.That(AccessToken.TryParse("ab cd",        out _), Is.False);   // white space
            Assert.That(AccessToken.TryParse("ab-cd_ef==",   out _), Is.False);   // Base64Url is not accepted
            Assert.That(AccessToken.TryParse("AAECAwQFBgc=", out var ok), Is.True);
            Assert.That(ok.Bytes.ToArray(),                          Is.EqualTo(new Byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));

            Assert.That(() => AccessToken.NewRandom(16), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(token.ToString(),                Does.Not.Contain(token.Value));

        }

        [Test]
        [S2C("Pairing.ChallengeLength")]
        public void HmacChallenge_RequiresAtLeast32Bytes()
        {
            Assert.That(HmacChallenge.TryParse(Convert.ToBase64String(new Byte[31]), out _), Is.False);
            Assert.That(HmacChallenge.TryParse(Convert.ToBase64String(new Byte[32]), out _), Is.True);
            Assert.That(HmacChallenge.NewRandom().Length,                                    Is.EqualTo(32));
            Assert.That(HmacChallengeResponse.TryParse("AQ==", out var response),            Is.True);
            Assert.That(response.Length,                                                     Is.EqualTo(1));
        }

        #endregion

        #region Certificate fingerprints

        [Test]
        public void CertificateFingerprint_AcceptsColonsAndPlainHex_AndComparesByValue()
        {

            var withColons  = "AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89";
            var plainLower  = withColons.Replace(":", "").ToLowerInvariant();

            Assert.That(CertificateFingerprint.TryParse(withColons, out var a), Is.True);
            Assert.That(CertificateFingerprint.TryParse(plainLower, out var b), Is.True);
            Assert.That(a,                       Is.EqualTo(b));
            Assert.That(a.Length,                Is.EqualTo(32));
            Assert.That(a.ToString(),            Is.EqualTo(withColons));
            Assert.That(a.ToHexString(false),    Is.EqualTo(withColons.Replace(":", "")));

            Assert.That(CertificateFingerprint.TryParse("ABC",  out _), Is.False);   // odd length
            Assert.That(CertificateFingerprint.TryParse("XYZW", out _), Is.False);   // not hex
            Assert.That(CertificateFingerprint.TryParse("",     out _), Is.False);

        }

        #endregion

        #region Challenge-response function

        // Known-answer vectors computed independently with Python:
        //   hmac.new(challenge, token + fingerprint_bytes, hashlib.sha256)   (LAN)
        //   hmac.new(challenge, token + domain_ascii,      hashlib.sha256)   (WAN)
        private static readonly HmacChallenge           Challenge    = HmacChallenge.Parse("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");   // bytes 0x00..0x1f
        private static readonly PairingToken            Token        = PairingToken.Parse("ABCD");
        private static readonly CertificateFingerprint  Fingerprint  = CertificateFingerprint.Parse("AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89");

        [Test]
        [S2C("Pairing.ChallengeResponse.LAN")]
        public void ChallengeResponse_LAN_MatchesTheKnownAnswer()
        {

            var response = ChallengeResponse.ComputeForLAN(HmacHashingAlgorithm.SHA256, Challenge, Token, Fingerprint);

            Assert.That(response.Value, Is.EqualTo("AO1kaP5yQ9JMNJ4wcag5RdmrFbbZrp8SxF33PLkHh+A="));
            Assert.That(ChallengeResponse.VerifyForLAN(HmacHashingAlgorithm.SHA256, Challenge, Token, Fingerprint, response), Is.True);
            Assert.That(ChallengeResponse.VerifyForLAN(HmacHashingAlgorithm.SHA256, Challenge, PairingToken.Parse("ABCE"), Fingerprint, response), Is.False);
            Assert.That(ChallengeResponse.VerifyForLAN(HmacHashingAlgorithm.SHA256, Challenge, Token, Fingerprint, HmacChallengeResponse.Parse("AQ==")), Is.False);

        }

        [Test]
        [S2C("Pairing.ChallengeResponse.LAN")]
        public void ChallengeResponse_LAN_SecondVector()
        {

            var challenge2 = HmacChallenge.Parse("ZjnIb4ToQB7JKKBZyIxKfTgoDfGsy6nw06bXQmhKRDwxMjM0NTY3OA==");   // 40 bytes
            var token2     = PairingToken.Parse("x7Kq2Zp9");

            Assert.That(challenge2.Length, Is.EqualTo(40));
            Assert.That(ChallengeResponse.ComputeForLAN(HmacHashingAlgorithm.SHA256, challenge2, token2, Fingerprint).Value,
                        Is.EqualTo("jklYGTm28U4+gioqqNczY8U1XR4xHPL/F40j6O2cVco="));

        }

        [Test]
        [S2C("Pairing.ChallengeResponse.WAN")]
        public void ChallengeResponse_WAN_MatchesTheKnownAnswer_AndNormalisesTheDomain()
        {

            var expected = "r5HqyHo0B1UZnj4n+U6pixlnFNaBV073LFFut5Ps67A=";

            Assert.That(ChallengeResponse.ComputeForWAN(HmacHashingAlgorithm.SHA256, Challenge, Token, "pairing.s2.example.com").Value,   Is.EqualTo(expected));
            Assert.That(ChallengeResponse.ComputeForWAN(HmacHashingAlgorithm.SHA256, Challenge, Token, "Pairing.S2.Example.COM.").Value,  Is.EqualTo(expected));
            Assert.That(ChallengeResponse.ComputeForWAN(HmacHashingAlgorithm.SHA256, Challenge, Token, " pairing.s2.example.com ").Value, Is.EqualTo(expected));

            Assert.That(ChallengeResponse.NormaliseDomainName("Bücher.example"),            Is.EqualTo("xn--bcher-kva.example"));
            Assert.That(() => ChallengeResponse.NormaliseDomainName("https://pairing.s2.example.com/"), Throws.ArgumentException);
            Assert.That(() => ChallengeResponse.NormaliseDomainName("pairing.s2.example.com:443"),      Throws.ArgumentException);
            Assert.That(() => ChallengeResponse.NormaliseDomainName(""),                                Throws.ArgumentException);

            var unknown = HmacHashingAlgorithm.Parse("SHA3-512");
            Assert.That(unknown.IsKnown, Is.False);
            Assert.That(() => ChallengeResponse.ComputeForWAN(unknown, Challenge, Token, "example.com"), Throws.TypeOf<NotSupportedException>());

        }

        #endregion

        #region Base URLs

        [Test]
        [S2C("Pairing.PairingURL")]
        public void S2BaseURL_EnforcesTheURLRules()
        {

            Assert.That(S2BaseURL.TryParse("https://hostname.local/pairing/", out var url, out var error), Is.True, error);
            Assert.That(url.IsHTTPS,                              Is.True);
            Assert.That(url.Host,                                 Is.EqualTo("hostname.local"));
            Assert.That(url.ForVersion("v1").ToString(),          Is.EqualTo("https://hostname.local/pairing/v1/"));
            Assert.That(url.Operation("v1", "requestPairing").ToString(), Is.EqualTo("https://hostname.local/pairing/v1/requestPairing"));

            Assert.That(S2BaseURL.TryParse("https://hostname.local/pairing",     out _, out error), Is.False);
            Assert.That(error, Does.Contain("slash"));
            Assert.That(S2BaseURL.TryParse("https://hostname.local/pairing/v1/", out _, out error), Is.False);
            Assert.That(error, Does.Contain("version"));
            Assert.That(S2BaseURL.TryParse("http://hostname.local/pairing/",     out _, out error), Is.False);
            Assert.That(error, Does.Contain("https"));
            Assert.That(S2BaseURL.TryParse("http://hostname.local/pairing/",     out _, out _, AllowHTTP: true), Is.True);
            Assert.That(S2BaseURL.TryParse("https://hostname.local/pairing/?x=1", out _, out _), Is.False);
            Assert.That(S2BaseURL.TryParse("",                                    out _, out _), Is.False);

        }

        #endregion

        #region Common DTOs

        [Test]
        public void NodeDescription_RoundTrips_WithCamelCaseKeys()
        {

            var description = new NodeDescription(Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000001"),
                                                  "ACME", "EV charger", "WallBox-b100", EnergyManagementRole.RM,
                                                  URL.Parse("https://acme.example/logo.png"), "Garage");
            var json = description.ToJSON();

            Assert.That(json["id"]?.Value<String>(),        Is.EqualTo("6f2f5c1e-0000-4000-8000-000000000001"));
            Assert.That(json["modelName"]?.Value<String>(), Is.EqualTo("WallBox-b100"));
            Assert.That(json["logoUrl"]?.Value<String>(),   Is.EqualTo("https://acme.example/logo.png"));
            Assert.That(json["role"]?.Value<String>(),      Is.EqualTo("RM"));

            Assert.That(NodeDescription.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed, Is.EqualTo(description));

            var minimal = new NodeDescription(Node_Id.NewRandom, "ACME", "EV charger", "WallBox-b100", EnergyManagementRole.CEM);
            Assert.That(minimal.ToJSON().ContainsKey("logoUrl"),         Is.False);
            Assert.That(minimal.ToJSON().ContainsKey("userDefinedName"), Is.False);

            var missing = JObject.Parse("""{ "id": "6f2f5c1e-0000-4000-8000-000000000001", "brand": "ACME", "type": "x", "role": "RM" }""");
            Assert.That(NodeDescription.TryParse(missing, out _, out error), Is.False);
            Assert.That(error, Does.Contain("modelName"));

        }

        [Test]
        public void EndpointDescription_AllPropertiesOptional()
        {

            var empty = new EndpointDescription();
            Assert.That(empty.ToJSON(), Has.Count.EqualTo(0));
            Assert.That(EndpointDescription.TryParse(JObject.Parse("{}"), out var parsedEmpty, out _), Is.True);
            Assert.That(parsedEmpty, Is.EqualTo(empty));

            var lan = new EndpointDescription("EVSE1038", null, Deployment.LAN);
            Assert.That(EndpointDescription.TryParse(lan.ToJSON(), out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed!.Deployment, Is.EqualTo(Deployment.LAN));

            Assert.That(EndpointDescription.TryParse(JObject.Parse("""{ "deployment": "CLOUD" }"""), out _, out error), Is.False);
            Assert.That(error, Does.Contain("not a known value"));

        }

        [Test]
        public void PairingTarget_NeverHasNodeIdAndAlias()
        {
            Assert.That(PairingTarget.Any.IsAny,                                         Is.True);
            Assert.That(PairingTarget.ByAlias(NodeIdAlias.Parse("A0")).NodeIdAlias?.Value, Is.EqualTo("A0"));
            Assert.That(() => PairingTarget.From(Node_Id.NewRandom, NodeIdAlias.Parse("A0")), Throws.ArgumentException);
        }

        #endregion

        #region Enumerations and tokens

        [Test]
        public void ConnectEnumerations_MatchTheOpenAPIValues()
        {

            Assert.That(CommunicationProtocol.All.Select(v => v.ToString()),     Is.EquivalentTo(new[] { "WebSocket" }));
            Assert.That(Deployment.All.Select(v => v.ToString()),                Is.EquivalentTo(new[] { "WAN", "LAN" }));
            Assert.That(HmacHashingAlgorithm.All.Select(v => v.ToString()),      Is.EquivalentTo(new[] { "SHA256" }));
            Assert.That(PairingResponseError.All.Count(),                        Is.EqualTo(9));
            Assert.That(CommunicationDetailsError.All.Select(v => v.ToString()), Is.EquivalentTo(new[] { "IncompatibleS2MessageVersions", "IncompatibleCommunicationProtocols", "NoLongerPaired", "ParsingError", "Other" }));
            Assert.That(WaitForPairingAction.All.Select(v => v.ToString()),      Is.EquivalentTo(new[] { "sendNodeDescription", "preparePairing", "cancelPreparePairing", "requestPairing" }));
            Assert.That(WaitForPairingError.All.Select(v => v.ToString()),       Is.EquivalentTo(new[] { "NoValidTokenOnPairingClient" }));
            Assert.That(EndpointStatus.All.Select(v => v.ToString()),            Is.EquivalentTo(new[] { "public", "testing" }));

            Assert.That(Deployment.Parse("lan").IsKnown, Is.False);   // case-sensitive like every S2 enumeration

        }

        [Test]
        public void CommunicationToken_AndTokenGenerator_ProduceStandardBase64()
        {

            var token = TokenGenerator.NewCommunicationToken();
            Assert.That(token.Length,                                            Is.EqualTo(32));
            Assert.That(CommunicationToken.TryParse(token.Value, out var parsed), Is.True);
            Assert.That(parsed,                                                  Is.EqualTo(token));
            Assert.That(token.ToString(),                                        Does.Not.Contain(token.Value));

            Assert.That(TokenGenerator.NewAccessToken().Length,                  Is.EqualTo(32));
            Assert.That(TokenGenerator.NewChallenge().Length,                    Is.EqualTo(32));
            Assert.That(TokenGenerator.NewPairingAttemptId().Length,             Is.EqualTo(32));
            Assert.That(TokenGenerator.NewDynamicPairingToken().Length,          Is.EqualTo(6));
            Assert.That(TokenGenerator.NewStaticPairingToken().Length,           Is.EqualTo(8));

            // Two generated tokens never collide.
            Assert.That(TokenGenerator.NewAccessToken(), Is.Not.EqualTo(TokenGenerator.NewAccessToken()));

        }

        #endregion

    }

}

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
    /// The S2 Connect pairing DTOs of Phase 5: the request and response bodies of
    /// /requestPairing, /requestConnectionDetails, /postConnectionDetails, /finalizePairing,
    /// /preparePairing, /cancelPreparePairing and /waitForPairing, plus the shared
    /// PairingResponseErrorMessage and ConnectionDetails schemas. Every DTO round-trips
    /// under the strict parser options, uses exactly the camelCase keys of the OpenAPI
    /// files, omits unset optional keys, reports missing mandatory keys by name, rejects
    /// additional properties under the strict options, and enforces its semantic rules
    /// both in the constructor and when parsing.
    /// </summary>
    [TestFixture]
    public sealed class PairingDTOTests
    {

        #region Data

        private static readonly Node_Id                 ClientNodeId        = Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000001");
        private static readonly Node_Id                 ServerNodeId        = Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000002");
        private static readonly Node_Id                 OtherNodeId         = Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000003");

        private static readonly NodeDescription         ClientNode          = new (ClientNodeId, "ACME",   "EV charger", "WallBox-b100", EnergyManagementRole.RM,
                                                                                   URL.Parse("https://acme.example/logo.png"), "Garage");
        private static readonly NodeDescription         ServerNode          = new (ServerNodeId, "GridCo", "CEM",        "HomeHub 2",    EnergyManagementRole.CEM);
        private static readonly EndpointDescription     LANEndpoint         = new ("EVSE1038",     null,                                        Deployment.LAN);
        private static readonly EndpointDescription     WANEndpoint         = new ("GridCo Cloud", URL.Parse("https://gridco.example/logo.png"), Deployment.WAN);

        private static readonly PairingToken            Token               = PairingToken.Parse("K7Q2P9");
        private static readonly HmacChallenge           ClientChallenge     = HmacChallenge.Parse("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");   // bytes 0x00..0x1f
        private static readonly HmacChallenge           ServerChallenge     = HmacChallenge.NewRandom();
        private static readonly CertificateFingerprint  CAFingerprint       = CertificateFingerprint.Parse("AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89");
        private static readonly CertificateFingerprint  CAFingerprintSHA1   = CertificateFingerprint.Parse("01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67");
        private static readonly HmacChallengeResponse   ClientResponse      = ChallengeResponse.ComputeForWAN(HmacHashingAlgorithm.SHA256, ClientChallenge, Token, "pairing.s2.example.com");
        private static readonly HmacChallengeResponse   ServerResponse      = ChallengeResponse.ComputeForLAN(HmacHashingAlgorithm.SHA256, ServerChallenge, Token, CAFingerprint);
        private static readonly PairingAttemptId        AttemptId           = PairingAttemptId.NewRandom();
        private static readonly AccessToken             SessionAccessToken  = AccessToken.FromBytes(Enumerable.Range(0x20, 32).Select(i => (Byte) i).ToArray());   // bytes 0x20..0x3f
        private static readonly S2BaseURL               SessionURL          = S2BaseURL.Parse("https://cem.example.com/connection/");

        /// <summary>
        /// A fresh fingerprint map with the mandatory "SHA256" entry and an additional "SHA1" entry.
        /// </summary>
        private static Dictionary<String, CertificateFingerprint> Fingerprints
            => new (StringComparer.Ordinal) {
                   ["SHA256"]  = CAFingerprint,
                   ["SHA1"]    = CAFingerprintSHA1
               };

        private static RequestPairingRequest NewRequestPairingRequest(PairingTarget?  Target         = null,
                                                                      Boolean         ForcePairing   = false)

            => new (ClientNode,
                    LANEndpoint,
                    [ CommunicationProtocol.WebSocket ],
                    [ "1.0.0", "0.0.2-beta" ],
                    [ HmacHashingAlgorithm.SHA256 ],
                    ClientChallenge,
                    Target,
                    ForcePairing);

        private static RequestPairingResponse NewRequestPairingResponse()

            => new (AttemptId,
                    ServerNode,
                    WANEndpoint,
                    HmacHashingAlgorithm.SHA256,
                    ClientResponse,
                    ServerChallenge);

        #endregion


        #region PairingResponseErrorMessage

        [Test]
        public void PairingResponseErrorMessage_RoundTrips_WithCamelCaseKeys()
        {

            var message = new PairingResponseErrorMessage(PairingResponseError.Other, "The node is busy, try again later.");
            var json    = message.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),    Is.EqualTo(new[] { "errorMessage", "additionalInfo" }));
            Assert.That(json["errorMessage"]?.  Value<String>(),  Is.EqualTo("Other"));
            Assert.That(json["additionalInfo"]?.Value<String>(),  Is.EqualTo("The node is busy, try again later."));

            Assert.That(PairingResponseErrorMessage.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                 Is.EqualTo(message));
            Assert.That(parsed!.GetHashCode(),  Is.EqualTo(message.GetHashCode()));
            Assert.That(parsed. Clone(),        Is.EqualTo(message));
            Assert.That(parsed. ToString(),     Does.Contain("Other").And.Contain("busy"));

            var minimal      = new PairingResponseErrorMessage(PairingResponseError.NodeNotFound);
            var minimalJSON  = minimal.ToJSON();

            Assert.That(minimalJSON.Properties().Select(p => p.Name), Is.EqualTo(new[] { "errorMessage" }));
            Assert.That(PairingResponseErrorMessage.TryParse(minimalJSON, out var parsedMinimal, out error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsedMinimal,                  Is.EqualTo(minimal));
            Assert.That(parsedMinimal!.AdditionalInfo,  Is.Null);
            Assert.That(parsedMinimal,                  Is.Not.EqualTo(message));

        }

        [Test]
        public void PairingResponseErrorMessage_MissingMandatoryProperty_Fails()
        {

            var json = JObject.Parse("""{ "additionalInfo": "no reason given" }""");

            Assert.That(PairingResponseErrorMessage.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("errorMessage"));

        }

        [Test]
        public void PairingResponseErrorMessage_Strict_RejectsAdditionalProperty()
        {

            var json = JObject.Parse("""{ "errorMessage": "NodeNotFound", "foo": 1 }""");

            Assert.That(PairingResponseErrorMessage.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PairingResponseErrorMessage.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void PairingResponseErrorMessage_RejectsUnknownErrors_ByDefault()
        {

            var json = JObject.Parse("""{ "errorMessage": "Teapot" }""");

            Assert.That(PairingResponseErrorMessage.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("not a known value"));

            var lenient = new S2ParserOptions { RejectUnknownEnumValues = false };
            Assert.That(PairingResponseErrorMessage.TryParse(json, out var parsed, out error, lenient), Is.True, error);
            Assert.That(parsed!.ErrorMessage.IsKnown, Is.False);

            Assert.That(() => new PairingResponseErrorMessage(default), Throws.ArgumentException);

        }

        #endregion

        #region ConnectionDetails

        [Test]
        [S2C("Pairing.ConnectionDetails.SHA256")]
        public void ConnectionDetails_RoundTrips_WithCamelCaseKeys()
        {

            var details = new ConnectionDetails(SessionURL, SessionAccessToken, Fingerprints);
            var json    = details.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),          Is.EqualTo(new[] { "initiateSessionUrl", "accessToken", "certificateFingerprint" }));
            Assert.That(json["initiateSessionUrl"]?.Value<String>(),    Is.EqualTo("https://cem.example.com/connection/"));
            Assert.That(json["accessToken"]?.       Value<String>(),    Is.EqualTo(SessionAccessToken.Value));

            var fingerprints = json["certificateFingerprint"] as JObject;
            Assert.That(fingerprints,                                   Is.Not.Null);
            Assert.That(fingerprints!.Properties().Select(p => p.Name), Is.EqualTo(new[] { "SHA256", "SHA1" }));
            Assert.That(fingerprints["SHA256"]?.Value<String>(),        Is.EqualTo(CAFingerprint.ToString()));

            Assert.That(ConnectionDetails.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                            Is.EqualTo(details));
            Assert.That(parsed!.GetHashCode(),             Is.EqualTo(details.GetHashCode()));
            Assert.That(parsed. Clone(),                   Is.EqualTo(details));
            Assert.That(parsed. SHA256Fingerprint,         Is.EqualTo(CAFingerprint));
            Assert.That(parsed. CertificateFingerprints,   Has.Count.EqualTo(2));
            Assert.That(parsed. AccessToken.Value,         Is.EqualTo(SessionAccessToken.Value));

            // The access token is a secret and never appears in ToString().
            Assert.That(parsed.ToString(),                 Does.Not.Contain(SessionAccessToken.Value));

            // Different fingerprints are different connection details.
            var sha256Only = new Dictionary<String, CertificateFingerprint>(StringComparer.Ordinal) { ["SHA256"] = CAFingerprint };
            Assert.That(parsed, Is.Not.EqualTo(new ConnectionDetails(SessionURL, SessionAccessToken, sha256Only)));

        }

        [Test]
        public void ConnectionDetails_MandatoryOnly_OmitsTheCertificateFingerprint()
        {

            var details = new ConnectionDetails(SessionURL, SessionAccessToken);
            var json    = details.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] { "initiateSessionUrl", "accessToken" }));

            Assert.That(ConnectionDetails.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                            Is.EqualTo(details));
            Assert.That(parsed!.CertificateFingerprints,   Is.Null);
            Assert.That(parsed. SHA256Fingerprint,         Is.Null);
            Assert.That(parsed. Clone(),                   Is.EqualTo(details));

        }

        [Test]
        public void ConnectionDetails_MissingMandatoryProperty_Fails()
        {

            var noURL = new ConnectionDetails(SessionURL, SessionAccessToken).ToJSON();
            noURL.Remove("initiateSessionUrl");
            Assert.That(ConnectionDetails.TryParse(noURL, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("initiateSessionUrl"));

            var noToken = new ConnectionDetails(SessionURL, SessionAccessToken).ToJSON();
            noToken.Remove("accessToken");
            Assert.That(ConnectionDetails.TryParse(noToken, out _, out error), Is.False);
            Assert.That(error, Does.Contain("accessToken"));

        }

        [Test]
        public void ConnectionDetails_Strict_RejectsAdditionalProperty()
        {

            var json = new ConnectionDetails(SessionURL, SessionAccessToken).ToJSON();
            json["foo"] = 1;

            Assert.That(ConnectionDetails.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(ConnectionDetails.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Pairing.ConnectionDetails.SHA256")]
        public void ConnectionDetails_RequiresTheSHA256Fingerprint_WhenFingerprintsAreGiven()
        {

            Assert.That(() => new ConnectionDetails(SessionURL, SessionAccessToken, new Dictionary<String, CertificateFingerprint>(StringComparer.Ordinal)),                                Throws.ArgumentException);
            Assert.That(() => new ConnectionDetails(SessionURL, SessionAccessToken, new Dictionary<String, CertificateFingerprint>(StringComparer.Ordinal) { ["SHA1"] = CAFingerprintSHA1 }), Throws.ArgumentException);
            Assert.That(() => new ConnectionDetails(SessionURL, SessionAccessToken, new Dictionary<String, CertificateFingerprint>(StringComparer.Ordinal) { ["SHA256"] = CAFingerprint, [""] = CAFingerprintSHA1 }), Throws.ArgumentException);

            var empty = new ConnectionDetails(SessionURL, SessionAccessToken).ToJSON();
            empty["certificateFingerprint"] = new JObject();
            Assert.That(ConnectionDetails.TryParse(empty, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("empty"));

            var sha1Only = new ConnectionDetails(SessionURL, SessionAccessToken).ToJSON();
            sha1Only["certificateFingerprint"] = new JObject(new JProperty("SHA1", CAFingerprintSHA1.ToString()));
            Assert.That(ConnectionDetails.TryParse(sha1Only, out _, out error), Is.False);
            Assert.That(error, Does.Contain("SHA256"));

            var notAnObject = new ConnectionDetails(SessionURL, SessionAccessToken).ToJSON();
            notAnObject["certificateFingerprint"] = CAFingerprint.ToString();
            Assert.That(ConnectionDetails.TryParse(notAnObject, out _, out error), Is.False);
            Assert.That(error, Does.Contain("certificateFingerprint"));

            var notHex = new ConnectionDetails(SessionURL, SessionAccessToken).ToJSON();
            notHex["certificateFingerprint"] = new JObject(new JProperty("SHA256", "not a fingerprint"));
            Assert.That(ConnectionDetails.TryParse(notHex, out _, out error), Is.False);
            Assert.That(error, Does.Contain("certificateFingerprint").And.Contain("SHA256"));

        }

        [Test]
        [S2C("Pairing.ConnectionDetails.SHA265Typo")]
        public void ConnectionDetails_ToleratesTheSHA265TypoOfTheOpenAPIFile_ButAlwaysWritesSHA256()
        {

            var json = new ConnectionDetails(SessionURL, SessionAccessToken).ToJSON();
            json["certificateFingerprint"] = new JObject(new JProperty("SHA265", CAFingerprint.ToString()));

            Assert.That(ConnectionDetails.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed!.SHA256Fingerprint,                 Is.EqualTo(CAFingerprint));
            Assert.That(parsed. CertificateFingerprints!.Keys,     Is.EqualTo(new[] { "SHA256" }));

            var reserialised = parsed.ToJSON()["certificateFingerprint"] as JObject;
            Assert.That(reserialised!.Properties().Select(p => p.Name), Is.EqualTo(new[] { "SHA256" }));

            // Both spellings with the same value are tolerated...
            json["certificateFingerprint"] = new JObject(new JProperty("SHA256", CAFingerprint.ToString()),
                                                         new JProperty("SHA265", CAFingerprint.ToHexString(false).ToLowerInvariant()));

            Assert.That(ConnectionDetails.TryParse(json, out parsed, out error), Is.True, error);
            Assert.That(parsed!.CertificateFingerprints!.Keys, Is.EqualTo(new[] { "SHA256" }));

            // ...with different values they are rejected.
            json["certificateFingerprint"] = new JObject(new JProperty("SHA256", CAFingerprint.ToString()),
                                                         new JProperty("SHA265", CAFingerprintSHA1.ToString()));

            Assert.That(ConnectionDetails.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("SHA265"));

        }

        [Test]
        [S2C("Pairing.PairingURL")]
        public void ConnectionDetails_RejectsInsecureURLs_UnlessAllowed_AndInvalidTokens()
        {

            var json = new ConnectionDetails(SessionURL, SessionAccessToken).ToJSON();
            json["initiateSessionUrl"] = "http://cem.example.com/connection/";

            Assert.That(ConnectionDetails.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("initiateSessionUrl").And.Contain("https"));

            var insecure = new S2ParserOptions { AllowInsecureURLs = true };
            Assert.That(ConnectionDetails.TryParse(json, out var parsed, out error, insecure), Is.True, error);
            Assert.That(parsed!.InitiateSessionUrl.IsHTTPS, Is.False);

            var versioned = new ConnectionDetails(SessionURL, SessionAccessToken).ToJSON();
            versioned["initiateSessionUrl"] = "https://cem.example.com/connection/v1/";
            Assert.That(ConnectionDetails.TryParse(versioned, out _, out error), Is.False);
            Assert.That(error, Does.Contain("initiateSessionUrl").And.Contain("version"));

            var badToken = new ConnectionDetails(SessionURL, SessionAccessToken).ToJSON();
            badToken["accessToken"] = "not base64!";
            Assert.That(ConnectionDetails.TryParse(badToken, out _, out error), Is.False);
            Assert.That(error, Does.Contain("accessToken"));

            Assert.That(() => new ConnectionDetails(default,    SessionAccessToken), Throws.ArgumentException);
            Assert.That(() => new ConnectionDetails(SessionURL, default),            Throws.ArgumentException);

        }

        #endregion

        #region RequestPairingRequest

        [Test]
        [S2C("Pairing.RequestPairing.Request")]
        public void RequestPairingRequest_RoundTrips_WithCamelCaseKeys()
        {

            var request = NewRequestPairingRequest(PairingTarget.ByNodeId(ServerNodeId), ForcePairing: true);
            var json    = request.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] {
                "clientNodeDescription", "clientEndpointDescription", "nodeId",
                "supportedCommunicationProtocols", "supportedS2MessageVersions", "supportedHmacHashingAlgorithms",
                "clientHmacChallenge", "forcePairing"
            }));

            Assert.That(json["clientNodeDescription"]?["id"]?.Value<String>(),              Is.EqualTo(ClientNodeId.ToString()));
            Assert.That(json["clientEndpointDescription"]?["deployment"]?.Value<String>(),  Is.EqualTo("LAN"));
            Assert.That(json["nodeId"]?.Value<String>(),                                    Is.EqualTo(ServerNodeId.ToString()));
            Assert.That(json["supportedCommunicationProtocols"]?.Values<String>(),          Is.EqualTo(new[] { "WebSocket" }));
            Assert.That(json["supportedS2MessageVersions"]?.Values<String>(),               Is.EqualTo(new[] { "1.0.0", "0.0.2-beta" }));
            Assert.That(json["supportedHmacHashingAlgorithms"]?.Values<String>(),           Is.EqualTo(new[] { "SHA256" }));
            Assert.That(json["clientHmacChallenge"]?.Value<String>(),                       Is.EqualTo(ClientChallenge.Value));
            Assert.That(json["forcePairing"]?.Value<Boolean>(),                             Is.True);

            Assert.That(RequestPairingRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                          Is.EqualTo(request));
            Assert.That(parsed!.GetHashCode(),           Is.EqualTo(request.GetHashCode()));
            Assert.That(parsed. Clone(),                 Is.EqualTo(request));
            Assert.That(parsed. Target.NodeId,           Is.EqualTo(ServerNodeId));
            Assert.That(parsed. Target.NodeIdAlias,      Is.Null);
            Assert.That(parsed. ForcePairing,            Is.True);
            Assert.That(parsed. ClientHmacChallenge,     Is.EqualTo(ClientChallenge));

            // The challenge is the HMAC key and never appears in ToString().
            Assert.That(parsed.ToString(),               Does.Not.Contain(ClientChallenge.Value));
            Assert.That(parsed.ToString(),               Does.Contain(ClientNodeId.ToString()).And.Contain(ServerNodeId.ToString()));

        }

        [Test]
        [S2C("Pairing.RequestPairing.NodeIdOrAlias")]
        public void RequestPairingRequest_TargetByAlias_WritesNodeIdAliasOnly_AndParsesUnderStrictOptions()
        {

            var request = NewRequestPairingRequest(PairingTarget.ByAlias(NodeIdAlias.Parse("A0")));
            var json    = request.ToJSON();

            Assert.That(json.ContainsKey("nodeId"),              Is.False);
            Assert.That(json["nodeIdAlias"]?.Value<String>(),    Is.EqualTo("A0"));
            Assert.That(json.ContainsKey("forcePairing"),        Is.False);

            // An alias is not a UUID, but the strict options (RequireUUIDs) must not reject it.
            Assert.That(RequestPairingRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                              Is.EqualTo(request));
            Assert.That(parsed!.Target.NodeIdAlias?.Value,   Is.EqualTo("A0"));
            Assert.That(parsed. Target.NodeId,               Is.Null);
            Assert.That(parsed. ForcePairing,                Is.False);

            json["nodeIdAlias"] = "A-0";
            Assert.That(RequestPairingRequest.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("nodeIdAlias"));

        }

        [Test]
        public void RequestPairingRequest_MandatoryOnly_TargetsTheOnlyNode()
        {

            var request = NewRequestPairingRequest();
            var json    = request.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] {
                "clientNodeDescription", "clientEndpointDescription",
                "supportedCommunicationProtocols", "supportedS2MessageVersions", "supportedHmacHashingAlgorithms",
                "clientHmacChallenge"
            }));

            Assert.That(RequestPairingRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                  Is.EqualTo(request));
            Assert.That(parsed!.Target.IsAny,    Is.True);
            Assert.That(parsed. Target,          Is.EqualTo(PairingTarget.Any));
            Assert.That(parsed. ForcePairing,    Is.False);

            // An explicit "forcePairing": false is the same request.
            json["forcePairing"] = false;
            Assert.That(RequestPairingRequest.TryParse(json, out var explicitFalse, out error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(explicitFalse, Is.EqualTo(request));

            Assert.That(parsed, Is.Not.EqualTo(NewRequestPairingRequest(ForcePairing: true)));
            Assert.That(parsed, Is.Not.EqualTo(NewRequestPairingRequest(PairingTarget.ByNodeId(ServerNodeId))));

        }

        [Test]
        public void RequestPairingRequest_MissingMandatoryProperty_Fails()
        {

            foreach (var key in new[] { "clientNodeDescription", "clientEndpointDescription", "supportedCommunicationProtocols",
                                        "supportedS2MessageVersions", "supportedHmacHashingAlgorithms", "clientHmacChallenge" })
            {

                var json = NewRequestPairingRequest().ToJSON();
                json.Remove(key);

                Assert.That(RequestPairingRequest.TryParse(json, out _, out var error), Is.False, key);
                Assert.That(error, Does.Contain(key), key);

            }

        }

        [Test]
        public void RequestPairingRequest_Strict_RejectsAdditionalProperty()
        {

            var json = NewRequestPairingRequest().ToJSON();
            json["foo"] = 1;

            Assert.That(RequestPairingRequest.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(RequestPairingRequest.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

            var nested = NewRequestPairingRequest().ToJSON();
            nested["clientNodeDescription"]!["bar"] = 2;

            Assert.That(RequestPairingRequest.TryParse(nested, out _, out error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("bar"));

        }

        [Test]
        [S2C("Pairing.RequestPairing.NodeIdOrAlias")]
        public void RequestPairingRequest_RejectsNodeIdAndAliasTogether()
        {

            var json = NewRequestPairingRequest(PairingTarget.ByNodeId(ServerNodeId)).ToJSON();
            json["nodeIdAlias"] = "A0";

            Assert.That(RequestPairingRequest.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("nodeId").And.Contain("nodeIdAlias"));

            Assert.That(() => PairingTarget.From(ServerNodeId, NodeIdAlias.Parse("A0")), Throws.ArgumentException);

        }

        [Test]
        [S2C("Pairing.RequestPairing.SHA256")]
        public void RequestPairingRequest_RequiresSHA256_AndNonEmptyLists()
        {

            Assert.That(() => new RequestPairingRequest(ClientNode, LANEndpoint, [],                                  [ "1.0.0" ], [ HmacHashingAlgorithm.SHA256 ],          ClientChallenge), Throws.ArgumentException);
            Assert.That(() => new RequestPairingRequest(ClientNode, LANEndpoint, [ CommunicationProtocol.WebSocket ], [],          [ HmacHashingAlgorithm.SHA256 ],          ClientChallenge), Throws.ArgumentException);
            Assert.That(() => new RequestPairingRequest(ClientNode, LANEndpoint, [ CommunicationProtocol.WebSocket ], [ "1.0.0" ], [],                                       ClientChallenge), Throws.ArgumentException);
            Assert.That(() => new RequestPairingRequest(ClientNode, LANEndpoint, [ CommunicationProtocol.WebSocket ], [ "1.0.0" ], [ HmacHashingAlgorithm.Parse("SHA512") ], ClientChallenge), Throws.ArgumentException);
            Assert.That(() => new RequestPairingRequest(ClientNode, LANEndpoint, [ CommunicationProtocol.WebSocket ], [ "1.0.0" ], [ HmacHashingAlgorithm.SHA256 ],          default),         Throws.ArgumentException);

            // SHA256 among other algorithms is fine.
            Assert.That(() => new RequestPairingRequest(ClientNode, LANEndpoint, [ CommunicationProtocol.WebSocket ], [ "1.0.0" ], [ HmacHashingAlgorithm.Parse("SHA512"), HmacHashingAlgorithm.SHA256 ], ClientChallenge), Throws.Nothing);

            var lenient = new S2ParserOptions { RejectUnknownEnumValues = false };

            var noSHA256 = NewRequestPairingRequest().ToJSON();
            noSHA256["supportedHmacHashingAlgorithms"] = new JArray("SHA512");
            Assert.That(RequestPairingRequest.TryParse(noSHA256, out _, out var error, lenient), Is.False);
            Assert.That(error, Does.Contain("SHA256"));

            // Unknown algorithms are rejected by the default options before the rule is reached.
            Assert.That(RequestPairingRequest.TryParse(noSHA256, out _, out error), Is.False);
            Assert.That(error, Does.Contain("not a known value"));

            foreach (var key in new[] { "supportedCommunicationProtocols", "supportedS2MessageVersions", "supportedHmacHashingAlgorithms" })
            {

                var json = NewRequestPairingRequest().ToJSON();
                json[key] = new JArray();

                Assert.That(RequestPairingRequest.TryParse(json, out _, out error), Is.False, key);
                Assert.That(error, Does.Contain("must not be empty"), key);

            }

        }

        [Test]
        [S2C("Pairing.ChallengeLength")]
        public void RequestPairingRequest_RejectsShortOrInvalidChallenges()
        {

            var json = NewRequestPairingRequest().ToJSON();
            json["clientHmacChallenge"] = Convert.ToBase64String(new Byte[31]);

            Assert.That(RequestPairingRequest.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("clientHmacChallenge"));

            json["clientHmacChallenge"] = "not base64";
            Assert.That(RequestPairingRequest.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("clientHmacChallenge"));

        }

        #endregion

        #region RequestPairingResponse

        [Test]
        public void RequestPairingResponse_RoundTrips_WithCamelCaseKeys()
        {

            var response = NewRequestPairingResponse();
            var json     = response.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] {
                "pairingAttemptId", "serverNodeDescription", "serverEndpointDescription",
                "selectedHmacHashingAlgorithm", "clientHmacChallengeResponse", "serverHmacChallenge"
            }));

            Assert.That(json["pairingAttemptId"]?.Value<String>(),                          Is.EqualTo(AttemptId.Value));
            Assert.That(json["serverNodeDescription"]?["role"]?.Value<String>(),            Is.EqualTo("CEM"));
            Assert.That(json["serverEndpointDescription"]?["deployment"]?.Value<String>(),  Is.EqualTo("WAN"));
            Assert.That(json["selectedHmacHashingAlgorithm"]?.Value<String>(),              Is.EqualTo("SHA256"));
            Assert.That(json["clientHmacChallengeResponse"]?.Value<String>(),               Is.EqualTo(ClientResponse.Value));
            Assert.That(json["serverHmacChallenge"]?.Value<String>(),                       Is.EqualTo(ServerChallenge.Value));

            // All properties are mandatory, so this is also the mandatory-only instance.
            Assert.That(RequestPairingResponse.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                                 Is.EqualTo(response));
            Assert.That(parsed!.GetHashCode(),                  Is.EqualTo(response.GetHashCode()));
            Assert.That(parsed. Clone(),                        Is.EqualTo(response));
            Assert.That(parsed. PairingAttemptId,               Is.EqualTo(AttemptId));
            Assert.That(parsed. SelectedHmacHashingAlgorithm,   Is.EqualTo(HmacHashingAlgorithm.SHA256));
            Assert.That(parsed. ClientHmacChallengeResponse,    Is.EqualTo(ClientResponse));
            Assert.That(parsed. ServerHmacChallenge,            Is.EqualTo(ServerChallenge));

            // The pairing attempt id (bearer token), the challenge and the challenge response never appear in ToString().
            Assert.That(parsed.ToString(), Does.Not.Contain(AttemptId.Value));
            Assert.That(parsed.ToString(), Does.Not.Contain(ServerChallenge.Value));
            Assert.That(parsed.ToString(), Does.Not.Contain(ClientResponse.Value));
            Assert.That(parsed.ToString(), Does.Contain(ServerNodeId.ToString()));

            Assert.That(parsed, Is.Not.EqualTo(new RequestPairingResponse(PairingAttemptId.NewRandom(), ServerNode, WANEndpoint, HmacHashingAlgorithm.SHA256, ClientResponse, ServerChallenge)));

        }

        [Test]
        public void RequestPairingResponse_MissingMandatoryProperty_Fails()
        {

            foreach (var key in new[] { "pairingAttemptId", "serverNodeDescription", "serverEndpointDescription",
                                        "selectedHmacHashingAlgorithm", "clientHmacChallengeResponse", "serverHmacChallenge" })
            {

                var json = NewRequestPairingResponse().ToJSON();
                json.Remove(key);

                Assert.That(RequestPairingResponse.TryParse(json, out _, out var error), Is.False, key);
                Assert.That(error, Does.Contain(key), key);

            }

        }

        [Test]
        public void RequestPairingResponse_Strict_RejectsAdditionalProperty()
        {

            var json = NewRequestPairingResponse().ToJSON();
            json["foo"] = 1;

            Assert.That(RequestPairingResponse.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(RequestPairingResponse.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Pairing.PairingAttemptId")]
        public void RequestPairingResponse_RejectsInvalidValues()
        {

            var shortId = NewRequestPairingResponse().ToJSON();
            shortId["pairingAttemptId"] = new String('x', 31);
            Assert.That(RequestPairingResponse.TryParse(shortId, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("pairingAttemptId"));

            var unknownAlgorithm = NewRequestPairingResponse().ToJSON();
            unknownAlgorithm["selectedHmacHashingAlgorithm"] = "SHA3";
            Assert.That(RequestPairingResponse.TryParse(unknownAlgorithm, out _, out error), Is.False);
            Assert.That(error, Does.Contain("not a known value"));

            var invalidNode = NewRequestPairingResponse().ToJSON();
            ((JObject) invalidNode["serverNodeDescription"]!).Remove("modelName");
            Assert.That(RequestPairingResponse.TryParse(invalidNode, out _, out error), Is.False);
            Assert.That(error, Does.Contain("serverNodeDescription").And.Contain("modelName"));

            var shortChallenge = NewRequestPairingResponse().ToJSON();
            shortChallenge["serverHmacChallenge"] = Convert.ToBase64String(new Byte[16]);
            Assert.That(RequestPairingResponse.TryParse(shortChallenge, out _, out error), Is.False);
            Assert.That(error, Does.Contain("serverHmacChallenge"));

            Assert.That(() => new RequestPairingResponse(default,   ServerNode, WANEndpoint, HmacHashingAlgorithm.SHA256, ClientResponse, ServerChallenge), Throws.ArgumentException);
            Assert.That(() => new RequestPairingResponse(AttemptId, ServerNode, WANEndpoint, default,                     ClientResponse, ServerChallenge), Throws.ArgumentException);
            Assert.That(() => new RequestPairingResponse(AttemptId, ServerNode, WANEndpoint, HmacHashingAlgorithm.SHA256, default,        ServerChallenge), Throws.ArgumentException);
            Assert.That(() => new RequestPairingResponse(AttemptId, ServerNode, WANEndpoint, HmacHashingAlgorithm.SHA256, ClientResponse, default),         Throws.ArgumentException);

        }

        #endregion

        #region RequestConnectionDetailsRequest

        [Test]
        public void RequestConnectionDetailsRequest_RoundTrips_WithCamelCaseKeys()
        {

            var request = new RequestConnectionDetailsRequest(ServerResponse);
            var json    = request.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),                Is.EqualTo(new[] { "serverHmacChallengeResponse" }));
            Assert.That(json["serverHmacChallengeResponse"]?.Value<String>(), Is.EqualTo(ServerResponse.Value));

            // All properties are mandatory, so this is also the mandatory-only instance.
            Assert.That(RequestConnectionDetailsRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                              Is.EqualTo(request));
            Assert.That(parsed!.GetHashCode(),               Is.EqualTo(request.GetHashCode()));
            Assert.That(parsed. Clone(),                     Is.EqualTo(request));
            Assert.That(parsed. ServerHmacChallengeResponse, Is.EqualTo(ServerResponse));
            Assert.That(parsed. ToString(),                  Does.Not.Contain(ServerResponse.Value));

            Assert.That(parsed, Is.Not.EqualTo(new RequestConnectionDetailsRequest(ClientResponse)));

        }

        [Test]
        public void RequestConnectionDetailsRequest_MissingMandatoryProperty_Fails()
        {

            Assert.That(RequestConnectionDetailsRequest.TryParse(JObject.Parse("{}"), out _, out var error), Is.False);
            Assert.That(error, Does.Contain("serverHmacChallengeResponse"));

            var invalid = JObject.Parse("""{ "serverHmacChallengeResponse": "not base64" }""");
            Assert.That(RequestConnectionDetailsRequest.TryParse(invalid, out _, out error), Is.False);
            Assert.That(error, Does.Contain("serverHmacChallengeResponse"));

            Assert.That(() => new RequestConnectionDetailsRequest(default), Throws.ArgumentException);

        }

        [Test]
        public void RequestConnectionDetailsRequest_Strict_RejectsAdditionalProperty()
        {

            var json = new RequestConnectionDetailsRequest(ServerResponse).ToJSON();
            json["foo"] = 1;

            Assert.That(RequestConnectionDetailsRequest.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(RequestConnectionDetailsRequest.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        #endregion

        #region PostConnectionDetailsRequest

        [Test]
        [S2C("Pairing.6B.CertificateFingerprint")]
        public void PostConnectionDetailsRequest_RoundTrips_WithCamelCaseKeys()
        {

            var request = new PostConnectionDetailsRequest(ServerResponse, new ConnectionDetails(SessionURL, SessionAccessToken, Fingerprints));
            var json    = request.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),                                          Is.EqualTo(new[] { "serverHmacChallengeResponse", "connectionDetails" }));
            Assert.That(json["serverHmacChallengeResponse"]?.Value<String>(),                           Is.EqualTo(ServerResponse.Value));
            Assert.That(json["connectionDetails"]?["initiateSessionUrl"]?.Value<String>(),              Is.EqualTo(SessionURL.ToString()));
            Assert.That(json["connectionDetails"]?["certificateFingerprint"]?["SHA256"]?.Value<String>(), Is.EqualTo(CAFingerprint.ToString()));

            // All properties are mandatory, so this is also the mandatory-only instance.
            Assert.That(PostConnectionDetailsRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                                        Is.EqualTo(request));
            Assert.That(parsed!.GetHashCode(),                         Is.EqualTo(request.GetHashCode()));
            Assert.That(parsed. Clone(),                               Is.EqualTo(request));
            Assert.That(parsed. ConnectionDetails.SHA256Fingerprint,   Is.EqualTo(CAFingerprint));
            Assert.That(parsed. ToString(),                            Does.Not.Contain(ServerResponse.Value).And.Not.Contain(SessionAccessToken.Value));

        }

        [Test]
        public void PostConnectionDetailsRequest_MissingMandatoryProperty_Fails()
        {

            var request = new PostConnectionDetailsRequest(ServerResponse, new ConnectionDetails(SessionURL, SessionAccessToken, Fingerprints));

            foreach (var key in new[] { "serverHmacChallengeResponse", "connectionDetails" })
            {

                var json = request.ToJSON();
                json.Remove(key);

                Assert.That(PostConnectionDetailsRequest.TryParse(json, out _, out var error), Is.False, key);
                Assert.That(error, Does.Contain(key), key);

            }

            var nested = request.ToJSON();
            ((JObject) nested["connectionDetails"]!).Remove("accessToken");
            Assert.That(PostConnectionDetailsRequest.TryParse(nested, out _, out var nestedError), Is.False);
            Assert.That(nestedError, Does.Contain("connectionDetails").And.Contain("accessToken"));

        }

        [Test]
        public void PostConnectionDetailsRequest_Strict_RejectsAdditionalProperty()
        {

            var request = new PostConnectionDetailsRequest(ServerResponse, new ConnectionDetails(SessionURL, SessionAccessToken, Fingerprints));

            var json = request.ToJSON();
            json["foo"] = 1;
            Assert.That(PostConnectionDetailsRequest.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PostConnectionDetailsRequest.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

            var nested = request.ToJSON();
            nested["connectionDetails"]!["bar"] = 2;
            Assert.That(PostConnectionDetailsRequest.TryParse(nested, out _, out error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("bar"));

        }

        [Test]
        [S2C("Pairing.6B.CertificateFingerprint")]
        public void PostConnectionDetailsRequest_RequiresTheCertificateFingerprint()
        {

            Assert.That(() => new PostConnectionDetailsRequest(ServerResponse, new ConnectionDetails(SessionURL, SessionAccessToken)),               Throws.ArgumentException);
            Assert.That(() => new PostConnectionDetailsRequest(default,        new ConnectionDetails(SessionURL, SessionAccessToken, Fingerprints)), Throws.ArgumentException);

            var json = new JObject(
                           new JProperty("serverHmacChallengeResponse",  ServerResponse.Value),
                           new JProperty("connectionDetails",            new ConnectionDetails(SessionURL, SessionAccessToken).ToJSON())
                       );

            Assert.That(PostConnectionDetailsRequest.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("certificate fingerprint"));

        }

        #endregion

        #region FinalizePairingRequest

        [Test]
        [S2C("Pairing.FinalizePairing.Success")]
        public void FinalizePairingRequest_RoundTrips_AndTreatsAMissingFlagAsFailure()
        {

            var success = new FinalizePairingRequest(true);
            var json    = success.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),  Is.EqualTo(new[] { "success" }));
            Assert.That(json["success"]?.Value<Boolean>(),      Is.True);

            Assert.That(FinalizePairingRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                 Is.EqualTo(success));
            Assert.That(parsed!.GetHashCode(),  Is.EqualTo(success.GetHashCode()));
            Assert.That(parsed. Clone(),        Is.EqualTo(success));
            Assert.That(parsed. Success,        Is.True);
            Assert.That(parsed. IsSuccess,      Is.True);
            Assert.That(parsed. ToString(),     Does.Contain("succeeded"));

            var failure = new FinalizePairingRequest(false);
            Assert.That(FinalizePairingRequest.TryParse(failure.ToJSON(), out var parsedFailure, out error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsedFailure,              Is.EqualTo(failure));
            Assert.That(parsedFailure!.IsSuccess,   Is.False);
            Assert.That(parsedFailure. ToString(),  Does.Contain("failed"));

            // Mandatory-only: the body is an empty object, and a missing flag counts as failure.
            var unknown = new FinalizePairingRequest();
            Assert.That(unknown.ToJSON(), Has.Count.EqualTo(0));
            Assert.That(FinalizePairingRequest.TryParse(JObject.Parse("{}"), out var parsedUnknown, out error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsedUnknown,              Is.EqualTo(unknown));
            Assert.That(parsedUnknown!.Success,     Is.Null);
            Assert.That(parsedUnknown. IsSuccess,   Is.False);

            Assert.That(unknown, Is.Not.EqualTo(failure));
            Assert.That(unknown, Is.Not.EqualTo(success));
            Assert.That(failure, Is.Not.EqualTo(success));

        }

        [Test]
        public void FinalizePairingRequest_RejectsANonBooleanFlag()
        {

            // There is no mandatory property; an invalid optional one is the closest failure.
            var json = JObject.Parse("""{ "success": "yes" }""");

            Assert.That(FinalizePairingRequest.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("success"));

        }

        [Test]
        public void FinalizePairingRequest_Strict_RejectsAdditionalProperty()
        {

            var json = JObject.Parse("""{ "success": true, "foo": 1 }""");

            Assert.That(FinalizePairingRequest.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FinalizePairingRequest.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        #endregion

        #region PreparePairingRequest

        [Test]
        public void PreparePairingRequest_RoundTrips_WithCamelCaseKeys()
        {

            var request = new PreparePairingRequest(ClientNode, LANEndpoint, ServerNodeId);
            var json    = request.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),                        Is.EqualTo(new[] { "clientNodeDescription", "clientEndpointDescription", "serverNodeId" }));
            Assert.That(json["clientNodeDescription"]?["id"]?.Value<String>(),        Is.EqualTo(ClientNodeId.ToString()));
            Assert.That(json["clientEndpointDescription"]?["name"]?.Value<String>(),  Is.EqualTo("EVSE1038"));
            Assert.That(json["serverNodeId"]?.Value<String>(),                        Is.EqualTo(ServerNodeId.ToString()));

            // All properties are mandatory, so this is also the mandatory-only instance.
            Assert.That(PreparePairingRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                  Is.EqualTo(request));
            Assert.That(parsed!.GetHashCode(),   Is.EqualTo(request.GetHashCode()));
            Assert.That(parsed. Clone(),         Is.EqualTo(request));
            Assert.That(parsed. ServerNodeId,    Is.EqualTo(ServerNodeId));
            Assert.That(parsed. ToString(),      Does.Contain(ClientNodeId.ToString()).And.Contain(ServerNodeId.ToString()));

            Assert.That(parsed, Is.Not.EqualTo(new PreparePairingRequest(ClientNode, LANEndpoint, OtherNodeId)));

        }

        [Test]
        public void PreparePairingRequest_MissingMandatoryProperty_Fails()
        {

            foreach (var key in new[] { "clientNodeDescription", "clientEndpointDescription", "serverNodeId" })
            {

                var json = new PreparePairingRequest(ClientNode, LANEndpoint, ServerNodeId).ToJSON();
                json.Remove(key);

                Assert.That(PreparePairingRequest.TryParse(json, out _, out var error), Is.False, key);
                Assert.That(error, Does.Contain(key), key);

            }

            var notAUUID = new PreparePairingRequest(ClientNode, LANEndpoint, ServerNodeId).ToJSON();
            notAUUID["serverNodeId"] = "server-1";
            Assert.That(PreparePairingRequest.TryParse(notAUUID, out _, out var uuidError), Is.False);
            Assert.That(uuidError, Does.Contain("serverNodeId"));

        }

        [Test]
        public void PreparePairingRequest_Strict_RejectsAdditionalProperty()
        {

            var json = new PreparePairingRequest(ClientNode, LANEndpoint, ServerNodeId).ToJSON();
            json["foo"] = 1;

            Assert.That(PreparePairingRequest.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PreparePairingRequest.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        #endregion

        #region CancelPreparePairingRequest

        [Test]
        public void CancelPreparePairingRequest_RoundTrips_WithCamelCaseKeys()
        {

            var request = new CancelPreparePairingRequest(ClientNodeId, ServerNodeId);
            var json    = request.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),   Is.EqualTo(new[] { "clientNodeId", "serverNodeId" }));
            Assert.That(json["clientNodeId"]?.Value<String>(),   Is.EqualTo(ClientNodeId.ToString()));
            Assert.That(json["serverNodeId"]?.Value<String>(),   Is.EqualTo(ServerNodeId.ToString()));

            // All properties are mandatory, so this is also the mandatory-only instance.
            Assert.That(CancelPreparePairingRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                  Is.EqualTo(request));
            Assert.That(parsed!.GetHashCode(),   Is.EqualTo(request.GetHashCode()));
            Assert.That(parsed. Clone(),         Is.EqualTo(request));
            Assert.That(parsed. ToString(),      Does.Contain(ClientNodeId.ToString()).And.Contain(ServerNodeId.ToString()));

            Assert.That(parsed, Is.Not.EqualTo(new CancelPreparePairingRequest(ServerNodeId, ClientNodeId)));

        }

        [Test]
        public void CancelPreparePairingRequest_MissingMandatoryProperty_Fails()
        {

            var noClient = JObject.Parse($$"""{ "serverNodeId": "{{ServerNodeId}}" }""");
            Assert.That(CancelPreparePairingRequest.TryParse(noClient, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("clientNodeId"));

            var noServer = JObject.Parse($$"""{ "clientNodeId": "{{ClientNodeId}}" }""");
            Assert.That(CancelPreparePairingRequest.TryParse(noServer, out _, out error), Is.False);
            Assert.That(error, Does.Contain("serverNodeId"));

        }

        [Test]
        public void CancelPreparePairingRequest_Strict_RejectsAdditionalProperty()
        {

            var json = new CancelPreparePairingRequest(ClientNodeId, ServerNodeId).ToJSON();
            json["foo"] = 1;

            Assert.That(CancelPreparePairingRequest.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(CancelPreparePairingRequest.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        #endregion

        #region WaitForPairingRequestItem

        [Test]
        public void WaitForPairingRequestItem_RoundTrips_WithCamelCaseKeys()
        {

            var item = new WaitForPairingRequestItem(ClientNodeId, ClientNode, LANEndpoint, WaitForPairingError.NoValidTokenOnPairingClient);
            var json = item.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),                     Is.EqualTo(new[] { "clientNodeId", "clientNodeDescription", "clientEndpointDescription", "errorMessage" }));
            Assert.That(json["clientNodeId"]?.Value<String>(),                     Is.EqualTo(ClientNodeId.ToString()));
            Assert.That(json["clientNodeDescription"]?["brand"]?.Value<String>(),  Is.EqualTo("ACME"));
            Assert.That(json["errorMessage"]?.Value<String>(),                     Is.EqualTo("NoValidTokenOnPairingClient"));

            Assert.That(WaitForPairingRequestItem.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                              Is.EqualTo(item));
            Assert.That(parsed!.GetHashCode(),               Is.EqualTo(item.GetHashCode()));
            Assert.That(parsed. Clone(),                     Is.EqualTo(item));
            Assert.That(parsed. ClientNodeDescription,       Is.EqualTo(ClientNode));
            Assert.That(parsed. ClientEndpointDescription,   Is.EqualTo(LANEndpoint));
            Assert.That(parsed. ErrorMessage,                Is.EqualTo(WaitForPairingError.NoValidTokenOnPairingClient));
            Assert.That(parsed. ToString(),                  Does.Contain(ClientNodeId.ToString()).And.Contain("NoValidTokenOnPairingClient"));

        }

        [Test]
        public void WaitForPairingRequestItem_MandatoryOnly_OmitsOptionalProperties()
        {

            var item = new WaitForPairingRequestItem(ClientNodeId);
            var json = item.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] { "clientNodeId" }));

            Assert.That(WaitForPairingRequestItem.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                              Is.EqualTo(item));
            Assert.That(parsed!.ClientNodeDescription,       Is.Null);
            Assert.That(parsed. ClientEndpointDescription,   Is.Null);
            Assert.That(parsed. ErrorMessage,                Is.Null);

            Assert.That(parsed, Is.Not.EqualTo(new WaitForPairingRequestItem(ClientNodeId, ErrorMessage: WaitForPairingError.NoValidTokenOnPairingClient)));
            Assert.That(parsed, Is.Not.EqualTo(new WaitForPairingRequestItem(ClientNodeId, ClientNode)));
            Assert.That(parsed, Is.Not.EqualTo(new WaitForPairingRequestItem(OtherNodeId)));

        }

        [Test]
        public void WaitForPairingRequestItem_MissingMandatoryProperty_Fails()
        {

            var json = JObject.Parse("""{ "errorMessage": "NoValidTokenOnPairingClient" }""");

            Assert.That(WaitForPairingRequestItem.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("clientNodeId"));

            var unknownError = JObject.Parse($$"""{ "clientNodeId": "{{ClientNodeId}}", "errorMessage": "Teapot" }""");
            Assert.That(WaitForPairingRequestItem.TryParse(unknownError, out _, out error), Is.False);
            Assert.That(error, Does.Contain("not a known value"));

        }

        [Test]
        public void WaitForPairingRequestItem_Strict_RejectsAdditionalProperty()
        {

            var json = new WaitForPairingRequestItem(ClientNodeId).ToJSON();
            json["foo"] = 1;

            Assert.That(WaitForPairingRequestItem.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(WaitForPairingRequestItem.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        #endregion

        #region WaitForPairingRequest

        [Test]
        [S2C("Pairing.WaitForPairing.Request")]
        public void WaitForPairingRequest_IsAJSONArray_AndRoundTrips()
        {

            var request = new WaitForPairingRequest([
                              new WaitForPairingRequestItem(ClientNodeId, ClientNode, LANEndpoint),
                              new WaitForPairingRequestItem(OtherNodeId,  ErrorMessage: WaitForPairingError.NoValidTokenOnPairingClient)
                          ]);

            var json = request.ToJSON();

            Assert.That(json,                                                     Has.Count.EqualTo(2));
            Assert.That(json[0]["clientNodeId"]?.Value<String>(),                 Is.EqualTo(ClientNodeId.ToString()));
            Assert.That(json[0]["clientNodeDescription"]?["id"]?.Value<String>(), Is.EqualTo(ClientNodeId.ToString()));
            Assert.That(json[1]["clientNodeId"]?.Value<String>(),                 Is.EqualTo(OtherNodeId.ToString()));
            Assert.That(json[1]["errorMessage"]?.Value<String>(),                 Is.EqualTo("NoValidTokenOnPairingClient"));

            Assert.That(WaitForPairingRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                          Is.EqualTo(request));
            Assert.That(parsed!.GetHashCode(),           Is.EqualTo(request.GetHashCode()));
            Assert.That(parsed. Clone(),                 Is.EqualTo(request));
            Assert.That(parsed. Items,                   Has.Count.EqualTo(2));
            Assert.That(parsed. Items[1].ClientNodeId,   Is.EqualTo(OtherNodeId));
            Assert.That(parsed. ToString(),              Does.Contain(ClientNodeId.ToString()).And.Contain(OtherNodeId.ToString()));

            // The default parser options work as well, and the order of the items matters.
            Assert.That(WaitForPairingRequest.TryParse(json, out var parsedDefault, out error), Is.True, error);
            Assert.That(parsedDefault, Is.EqualTo(request));
            Assert.That(parsed, Is.Not.EqualTo(new WaitForPairingRequest([.. request.Items.Reverse()])));

            // A single node is the mandatory-only case: an array with one object holding just the clientNodeId.
            var single = new WaitForPairingRequest([ new WaitForPairingRequestItem(ClientNodeId) ]);
            Assert.That(single.ToJSON().ToString(Newtonsoft.Json.Formatting.None), Is.EqualTo($$"""[{"clientNodeId":"{{ClientNodeId}}"}]"""));
            Assert.That(WaitForPairingRequest.TryParse(single.ToJSON(), out var parsedSingle, out error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsedSingle, Is.EqualTo(single));

        }

        [Test]
        public void WaitForPairingRequest_MissingMandatoryProperty_Fails()
        {

            var json = JArray.Parse($$"""[ { "clientNodeId": "{{ClientNodeId}}" }, { "errorMessage": "NoValidTokenOnPairingClient" } ]""");

            Assert.That(WaitForPairingRequest.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("clientNodeId").And.Contain("item 1"));

            var notAnObject = JArray.Parse("""[ 42 ]""");
            Assert.That(WaitForPairingRequest.TryParse(notAnObject, out _, out error), Is.False);
            Assert.That(error, Does.Contain("item 0"));

        }

        [Test]
        public void WaitForPairingRequest_Strict_RejectsAdditionalProperty()
        {

            var json = new WaitForPairingRequest([ new WaitForPairingRequestItem(ClientNodeId) ]).ToJSON();
            json[0]["foo"] = 1;

            Assert.That(WaitForPairingRequest.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(WaitForPairingRequest.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Pairing.WaitForPairing.Request")]
        public void WaitForPairingRequest_RequiresAtLeastOneItem_AndUniqueClientNodeIds()
        {

            Assert.That(() => new WaitForPairingRequest([]), Throws.ArgumentException);
            Assert.That(() => new WaitForPairingRequest([ new WaitForPairingRequestItem(ClientNodeId), new WaitForPairingRequestItem(ClientNodeId, ClientNode) ]), Throws.ArgumentException);

            Assert.That(WaitForPairingRequest.TryParse(JArray.Parse("[]"), out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at least one"));

            var duplicates = JArray.Parse($$"""[ { "clientNodeId": "{{ClientNodeId}}" }, { "clientNodeId": "{{ClientNodeId}}" } ]""");
            Assert.That(WaitForPairingRequest.TryParse(duplicates, out _, out error), Is.False);
            Assert.That(error, Does.Contain("unique").And.Contain(ClientNodeId.ToString()));

        }

        #endregion

        #region WaitForPairingResponseItem

        [Test]
        public void WaitForPairingResponseItem_RoundTrips_WithCamelCaseKeys()
        {

            var item = new WaitForPairingResponseItem(ClientNodeId, WaitForPairingAction.RequestPairing);
            var json = item.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),   Is.EqualTo(new[] { "clientNodeId", "action" }));
            Assert.That(json["clientNodeId"]?.Value<String>(),   Is.EqualTo(ClientNodeId.ToString()));
            Assert.That(json["action"]?.Value<String>(),         Is.EqualTo("requestPairing"));

            // All properties are mandatory, so this is also the mandatory-only instance.
            Assert.That(WaitForPairingResponseItem.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                  Is.EqualTo(item));
            Assert.That(parsed!.GetHashCode(),   Is.EqualTo(item.GetHashCode()));
            Assert.That(parsed. Clone(),         Is.EqualTo(item));
            Assert.That(parsed. Action,          Is.EqualTo(WaitForPairingAction.RequestPairing));
            Assert.That(parsed. ToString(),      Does.Contain(ClientNodeId.ToString()).And.Contain("requestPairing"));

            Assert.That(parsed, Is.Not.EqualTo(new WaitForPairingResponseItem(ClientNodeId, WaitForPairingAction.PreparePairing)));
            Assert.That(parsed, Is.Not.EqualTo(new WaitForPairingResponseItem(OtherNodeId,  WaitForPairingAction.RequestPairing)));

        }

        [Test]
        public void WaitForPairingResponseItem_MissingMandatoryProperty_Fails()
        {

            var noAction = JObject.Parse($$"""{ "clientNodeId": "{{ClientNodeId}}" }""");
            Assert.That(WaitForPairingResponseItem.TryParse(noAction, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("action"));

            var noNode = JObject.Parse("""{ "action": "requestPairing" }""");
            Assert.That(WaitForPairingResponseItem.TryParse(noNode, out _, out error), Is.False);
            Assert.That(error, Does.Contain("clientNodeId"));

            var unknownAction = JObject.Parse($$"""{ "clientNodeId": "{{ClientNodeId}}", "action": "reboot" }""");
            Assert.That(WaitForPairingResponseItem.TryParse(unknownAction, out _, out error), Is.False);
            Assert.That(error, Does.Contain("not a known value"));

            Assert.That(() => new WaitForPairingResponseItem(ClientNodeId, default), Throws.ArgumentException);

        }

        [Test]
        public void WaitForPairingResponseItem_Strict_RejectsAdditionalProperty()
        {

            var json = new WaitForPairingResponseItem(ClientNodeId, WaitForPairingAction.SendNodeDescription).ToJSON();
            json["foo"] = 1;

            Assert.That(WaitForPairingResponseItem.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(WaitForPairingResponseItem.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        #endregion

        #region WaitForPairingResponse

        [Test]
        [S2C("Pairing.WaitForPairing.Response")]
        public void WaitForPairingResponse_IsAJSONArray_AndRoundTrips()
        {

            var response = new WaitForPairingResponse([
                               new WaitForPairingResponseItem(ClientNodeId, WaitForPairingAction.SendNodeDescription),
                               new WaitForPairingResponseItem(OtherNodeId,  WaitForPairingAction.RequestPairing)
                           ]);

            var json = response.ToJSON();

            Assert.That(json,                                        Has.Count.EqualTo(2));
            Assert.That(json[0]["clientNodeId"]?.Value<String>(),    Is.EqualTo(ClientNodeId.ToString()));
            Assert.That(json[0]["action"]?.Value<String>(),          Is.EqualTo("sendNodeDescription"));
            Assert.That(json[1]["action"]?.Value<String>(),          Is.EqualTo("requestPairing"));

            Assert.That(WaitForPairingResponse.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                      Is.EqualTo(response));
            Assert.That(parsed!.GetHashCode(),       Is.EqualTo(response.GetHashCode()));
            Assert.That(parsed. Clone(),             Is.EqualTo(response));
            Assert.That(parsed. Items,               Has.Count.EqualTo(2));
            Assert.That(parsed. Items[0].Action,     Is.EqualTo(WaitForPairingAction.SendNodeDescription));
            Assert.That(parsed. ToString(),          Does.Contain("sendNodeDescription").And.Contain("requestPairing"));

            // The default parser options work as well, and the order of the items matters.
            Assert.That(WaitForPairingResponse.TryParse(json, out var parsedDefault, out error), Is.True, error);
            Assert.That(parsedDefault, Is.EqualTo(response));
            Assert.That(parsed, Is.Not.EqualTo(new WaitForPairingResponse([.. response.Items.Reverse()])));

            // A single action is the mandatory-only case (minItems 1).
            var single = new WaitForPairingResponse([ new WaitForPairingResponseItem(ClientNodeId, WaitForPairingAction.CancelPreparePairing) ]);
            Assert.That(single.ToJSON().ToString(Newtonsoft.Json.Formatting.None), Is.EqualTo($$"""[{"clientNodeId":"{{ClientNodeId}}","action":"cancelPreparePairing"}]"""));
            Assert.That(WaitForPairingResponse.TryParse(single.ToJSON(), out var parsedSingle, out error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsedSingle, Is.EqualTo(single));

        }

        [Test]
        public void WaitForPairingResponse_MissingMandatoryProperty_Fails()
        {

            var json = JArray.Parse($$"""[ { "clientNodeId": "{{ClientNodeId}}" } ]""");

            Assert.That(WaitForPairingResponse.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("action").And.Contain("item 0"));

            Assert.That(WaitForPairingResponse.TryParse(JArray.Parse("""[ "requestPairing" ]"""), out _, out error), Is.False);
            Assert.That(error, Does.Contain("item 0"));

        }

        [Test]
        public void WaitForPairingResponse_Strict_RejectsAdditionalProperty()
        {

            var json = new WaitForPairingResponse([ new WaitForPairingResponseItem(ClientNodeId, WaitForPairingAction.RequestPairing) ]).ToJSON();
            json[0]["foo"] = 1;

            Assert.That(WaitForPairingResponse.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(WaitForPairingResponse.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Pairing.WaitForPairing.OneItemPerNode")]
        public void WaitForPairingResponse_RequiresAtLeastOneItem_AndAtMostOneItemPerClientNode()
        {

            Assert.That(() => new WaitForPairingResponse([]), Throws.ArgumentException);
            Assert.That(() => new WaitForPairingResponse([
                                  new WaitForPairingResponseItem(ClientNodeId, WaitForPairingAction.PreparePairing),
                                  new WaitForPairingResponseItem(ClientNodeId, WaitForPairingAction.RequestPairing)
                              ]),
                        Throws.ArgumentException);

            Assert.That(WaitForPairingResponse.TryParse(JArray.Parse("[]"), out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at least one"));

            var duplicates = JArray.Parse($$"""
                [
                  { "clientNodeId": "{{ClientNodeId}}", "action": "preparePairing" },
                  { "clientNodeId": "{{ClientNodeId}}", "action": "requestPairing" }
                ]
                """);

            Assert.That(WaitForPairingResponse.TryParse(duplicates, out _, out error), Is.False);
            Assert.That(error, Does.Contain("at most one").And.Contain(ClientNodeId.ToString()));

        }

        #endregion

    }

}

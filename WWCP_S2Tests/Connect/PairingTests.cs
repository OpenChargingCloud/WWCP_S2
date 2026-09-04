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

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The persisted pairing relation (PLAN.md §3.5): constructor guards, the JSON
    /// persistence format, token rotation, description updates, equality and redaction.
    /// </summary>
    [TestFixture]
    public sealed class PairingTests
    {

        #region Data

        private static readonly Node_Id                 LocalNodeId      = Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000001");
        private static readonly Node_Id                 RemoteNodeId     = Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000002");

        private static readonly NodeDescription         RemoteNode       = new (RemoteNodeId, "ACME", "EV charger", "WallBox-b100", EnergyManagementRole.RM, null, "Garage");
        private static readonly EndpointDescription     RemoteEndpoint   = new ("ACME cloud", null, Deployment.WAN);

        private static readonly AccessToken             Token            = AccessToken.Parse("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");   // bytes 0x00..0x1f
        private static readonly AccessToken             OtherToken       = AccessToken.Parse("Hx4dHBsaGRgXFhUUExIREA8ODQwLCgkIBwYFBAMCAQA=");   // bytes 0x1f..0x00
        private static readonly DateTimeOffset          PairedAt         = new (2026, 9, 4, 10, 0, 0, TimeSpan.Zero);

        private static readonly S2BaseURL               SessionUrl       = S2BaseURL.Parse("https://cem.example.com/connection/");
        private static readonly CertificateFingerprint  CAFingerprint    = CertificateFingerprint.Parse("AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89");
        private static readonly CertificateFingerprint  CAFingerprintSHA1 = CertificateFingerprint.Parse("01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67");

        private static Dictionary<String, CertificateFingerprint> Fingerprints()
            => new (StringComparer.Ordinal) {
                   ["SHA256"]  = CAFingerprint,
                   ["SHA1"]    = CAFingerprintSHA1
               };

        private static Pairing ServerPairing(AccessToken? WithToken = null)
            => new (LocalNodeId,
                    RemoteNode,
                    RemoteEndpoint,
                    CommunicationRole.CommunicationServer,
                    WithToken ?? Token,
                    PairedAt);

        private static Pairing ClientPairing(Boolean WithFingerprints = true)
            => new (LocalNodeId,
                    RemoteNode,
                    RemoteEndpoint,
                    CommunicationRole.CommunicationClient,
                    Token,
                    PairedAt,
                    SessionUrl,
                    WithFingerprints ? Fingerprints() : null);

        #endregion


        #region Constructor

        [Test]
        public void Constructor_ExposesTheProperties()
        {

            var server = ServerPairing();
            var client = ClientPairing();

            Assert.Multiple(() => {

                Assert.That(server.LocalNodeId,               Is.EqualTo(LocalNodeId));
                Assert.That(server.RemoteNodeId,              Is.EqualTo(RemoteNodeId));
                Assert.That(server.RemoteNodeDescription,     Is.SameAs(RemoteNode));
                Assert.That(server.RemoteEndpointDescription, Is.SameAs(RemoteEndpoint));
                Assert.That(server.RemoteRole,                Is.EqualTo(EnergyManagementRole.RM));
                Assert.That(server.LocalCommunicationRole,    Is.EqualTo(CommunicationRole.CommunicationServer));
                Assert.That(server.IsCommunicationServer,     Is.True);
                Assert.That(server.IsCommunicationClient,     Is.False);
                Assert.That(server.AccessToken,               Is.EqualTo(Token));
                Assert.That(server.PairedAt,                  Is.EqualTo(PairedAt));
                Assert.That(server.InitiateSessionUrl,        Is.Null);
                Assert.That(server.CertificateFingerprints,   Is.Null);

                Assert.That(client.IsCommunicationServer,     Is.False);
                Assert.That(client.IsCommunicationClient,     Is.True);
                Assert.That(client.InitiateSessionUrl,        Is.EqualTo(SessionUrl));
                Assert.That(client.CertificateFingerprints,   Is.Not.Null);
                Assert.That(client.CertificateFingerprints,   Has.Count.EqualTo(2));
                Assert.That(client.CertificateFingerprints!["SHA256"], Is.EqualTo(CAFingerprint));

            });

        }

        [Test]
        public void Constructor_CopiesTheFingerprintMap()
        {

            var fingerprints = Fingerprints();
            var pairing      = new Pairing(LocalNodeId, RemoteNode, RemoteEndpoint, CommunicationRole.CommunicationClient, Token, PairedAt, SessionUrl, fingerprints);

            fingerprints.Remove("SHA1");

            Assert.That(pairing.CertificateFingerprints, Has.Count.EqualTo(2), "later changes of the given map are not visible");

        }

        [Test]
        public void Constructor_RejectsSelfPairingAndEmptyIdentifications()
        {

            Assert.Multiple(() => {
                Assert.That(() => new Pairing(RemoteNodeId,     RemoteNode, RemoteEndpoint, CommunicationRole.CommunicationServer, Token, PairedAt), Throws.ArgumentException, "a node cannot pair with itself");
                Assert.That(() => new Pairing(default(Node_Id), RemoteNode, RemoteEndpoint, CommunicationRole.CommunicationServer, Token, PairedAt), Throws.ArgumentException, "empty local node identification");
                Assert.That(() => new Pairing(LocalNodeId,      null!,      RemoteEndpoint, CommunicationRole.CommunicationServer, Token, PairedAt), Throws.ArgumentNullException);
                Assert.That(() => new Pairing(LocalNodeId,      RemoteNode, null!,          CommunicationRole.CommunicationServer, Token, PairedAt), Throws.ArgumentNullException);
            });

        }

        [Test]
        public void Constructor_RejectsAnEmptyAccessToken()
        {
            Assert.That(() => new Pairing(LocalNodeId, RemoteNode, RemoteEndpoint, CommunicationRole.CommunicationServer, default, PairedAt), Throws.ArgumentException);
        }

        [Test]
        public void Constructor_RejectsACommunicationClientWithoutInitiateSessionUrl()
        {
            Assert.That(() => new Pairing(LocalNodeId, RemoteNode, RemoteEndpoint, CommunicationRole.CommunicationClient, Token, PairedAt),             Throws.ArgumentException);
            Assert.That(() => new Pairing(LocalNodeId, RemoteNode, RemoteEndpoint, CommunicationRole.CommunicationClient, Token, PairedAt, SessionUrl), Throws.Nothing);
        }

        [Test]
        public void Constructor_RejectsAnEmptyFingerprintMap_ButAcceptsNone()
        {
            Assert.That(() => new Pairing(LocalNodeId, RemoteNode, RemoteEndpoint, CommunicationRole.CommunicationClient, Token, PairedAt, SessionUrl, new Dictionary<String, CertificateFingerprint>(StringComparer.Ordinal)), Throws.ArgumentException);
            Assert.That(() => new Pairing(LocalNodeId, RemoteNode, RemoteEndpoint, CommunicationRole.CommunicationClient, Token, PairedAt, SessionUrl, null),                                                                    Throws.Nothing);
        }

        [Test]
        public void Constructor_RejectsAnUndefinedCommunicationRole()
        {
            Assert.That(() => new Pairing(LocalNodeId, RemoteNode, RemoteEndpoint, (CommunicationRole) 42, Token, PairedAt), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        #endregion


        #region JSON persistence format

        [Test]
        public void ToJSON_ServerPairing_RoundTripsStrictly()
        {

            var pairing = ServerPairing();
            var json    = pairing.ToJSON();

            Assert.Multiple(() => {
                Assert.That(json["localNodeId"]?.Value<String>(),            Is.EqualTo("6f2f5c1e-0000-4000-8000-000000000001"));
                Assert.That(json["remoteNodeDescription"]?["id"]?.Value<String>(), Is.EqualTo("6f2f5c1e-0000-4000-8000-000000000002"));
                Assert.That(json["remoteEndpointDescription"]?["deployment"]?.Value<String>(), Is.EqualTo("WAN"));
                Assert.That(json["localCommunicationRole"]?.Value<String>(), Is.EqualTo("CommunicationServer"));
                Assert.That(json["accessToken"]?.Value<String>(),            Is.EqualTo(Token.Value));
                Assert.That(json["pairedAt"]?.Value<String>(),               Is.EqualTo("2026-09-04T10:00:00Z"));
                Assert.That(json.ContainsKey("initiateSessionUrl"),          Is.False);
                Assert.That(json.ContainsKey("certificateFingerprint"),      Is.False);
            });

            Assert.That(Pairing.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);

            Assert.Multiple(() => {
                Assert.That(parsed,                    Is.EqualTo(pairing));
                Assert.That(parsed!.GetHashCode(),     Is.EqualTo(pairing.GetHashCode()));
                Assert.That(parsed!.AccessToken.Value, Is.EqualTo(Token.Value), "the Base64 text is kept verbatim");
            });

        }

        [Test]
        [S2C("Pairing.6B.CertificateFingerprint")]
        public void ToJSON_ClientPairing_WithUrlAndFingerprints_RoundTripsStrictly()
        {

            var pairing = ClientPairing();
            var json    = pairing.ToJSON();

            Assert.Multiple(() => {
                Assert.That(json["localCommunicationRole"]?.Value<String>(),              Is.EqualTo("CommunicationClient"));
                Assert.That(json["initiateSessionUrl"]?.Value<String>(),                  Is.EqualTo("https://cem.example.com/connection/"));
                Assert.That(json["certificateFingerprint"]?["SHA256"]?.Value<String>(),   Is.EqualTo(CAFingerprint.ToHexString()));
                Assert.That(json["certificateFingerprint"]?["SHA1"]?.Value<String>(),     Is.EqualTo(CAFingerprintSHA1.ToHexString()));
            });

            Assert.That(Pairing.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);

            Assert.Multiple(() => {
                Assert.That(parsed,                                      Is.EqualTo(pairing));
                Assert.That(parsed!.InitiateSessionUrl,                  Is.EqualTo(SessionUrl));
                Assert.That(parsed!.CertificateFingerprints,             Has.Count.EqualTo(2));
                Assert.That(parsed!.CertificateFingerprints!["SHA256"],  Is.EqualTo(CAFingerprint));
                Assert.That(parsed!.CertificateFingerprints!["SHA1"],    Is.EqualTo(CAFingerprintSHA1));
            });

        }

        [Test]
        public void TryParse_AcceptsAnInsecureInitiateSessionUrl_OnlyWhenAllowed()
        {

            var json = ClientPairing(WithFingerprints: false).ToJSON();
            json["initiateSessionUrl"] = "http://cem.local/connection/";

            Assert.Multiple(() => {
                Assert.That(Pairing.TryParse(json, out _, out var error),                                                          Is.False);
                Assert.That(error,                                                                                                  Does.Contain("initiateSessionUrl"));
                Assert.That(Pairing.TryParse(json, out var parsed, out _, new S2ParserOptions { AllowInsecureURLs = true }),        Is.True);
                Assert.That(parsed?.InitiateSessionUrl?.Value,                                                                      Is.EqualTo("http://cem.local/connection/"));
            });

        }

        [Test]
        public void TryParse_RejectsAnUnknownLocalCommunicationRole()
        {

            var json = ServerPairing().ToJSON();
            json["localCommunicationRole"] = "Peer";

            Assert.That(Pairing.TryParse(json, out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error,  Does.Contain("localCommunicationRole"));

        }

        [Test]
        public void TryParse_RejectsAdditionalProperties_OnlyWithStrictOptions()
        {

            var json = ServerPairing().ToJSON();
            json.Add("extra", 1);

            Assert.Multiple(() => {
                Assert.That(Pairing.TryParse(json, out _,          out var strictError, S2ParserOptions.Strict),  Is.False);
                Assert.That(strictError,                                                                          Is.Not.Null);
                Assert.That(Pairing.TryParse(json, out var parsed, out var defaultError, S2ParserOptions.Default), Is.True, defaultError);
                Assert.That(parsed,                                                                               Is.EqualTo(ServerPairing()));
                Assert.That(Pairing.TryParse(json, out _,          out _),                                        Is.True, "additional properties are tolerated by default");
            });

        }

        [Test]
        public void TryParse_RejectsMissingAndInvalidMandatoryProperties()
        {

            var withoutToken = ServerPairing().ToJSON();
            withoutToken.Remove("accessToken");

            var invalidToken = ServerPairing().ToJSON();
            invalidToken["accessToken"] = "not base64!";

            var withoutPairedAt = ServerPairing().ToJSON();
            withoutPairedAt.Remove("pairedAt");

            Assert.Multiple(() => {
                Assert.That(Pairing.TryParse(withoutToken,    out _, out var error1), Is.False);
                Assert.That(error1,                                                   Does.Contain("accessToken"));
                Assert.That(Pairing.TryParse(invalidToken,    out _, out var error2), Is.False);
                Assert.That(error2,                                                   Does.Contain("accessToken"));
                Assert.That(Pairing.TryParse(withoutPairedAt, out _, out var error3), Is.False);
                Assert.That(error3,                                                   Does.Contain("pairedAt"));
            });

        }

        #endregion


        #region WithAccessToken(...) / WithDescriptions(...)

        [Test]
        [S2C("SessionInitiation.TokenRotation")]
        public void WithAccessToken_RotatesTheToken_AndKeepsEverythingElse()
        {

            var pairing  = ClientPairing();
            var rotated  = pairing.WithAccessToken(OtherToken);

            Assert.Multiple(() => {
                Assert.That(rotated,                            Is.Not.SameAs(pairing));
                Assert.That(rotated.AccessToken,                Is.EqualTo(OtherToken));
                Assert.That(pairing.AccessToken,                Is.EqualTo(Token), "the original is immutable");
                Assert.That(rotated.LocalNodeId,                Is.EqualTo(pairing.LocalNodeId));
                Assert.That(rotated.RemoteNodeDescription,      Is.SameAs(pairing.RemoteNodeDescription));
                Assert.That(rotated.RemoteEndpointDescription,  Is.SameAs(pairing.RemoteEndpointDescription));
                Assert.That(rotated.LocalCommunicationRole,     Is.EqualTo(pairing.LocalCommunicationRole));
                Assert.That(rotated.PairedAt,                   Is.EqualTo(pairing.PairedAt));
                Assert.That(rotated.InitiateSessionUrl,         Is.EqualTo(pairing.InitiateSessionUrl));
                Assert.That(rotated.CertificateFingerprints,    Is.EqualTo(pairing.CertificateFingerprints));
                Assert.That(rotated,                            Is.Not.EqualTo(pairing));
                Assert.That(rotated.WithAccessToken(Token),     Is.EqualTo(pairing), "rotating back restores equality");
            });

        }

        [Test]
        public void WithAccessToken_RejectsAnEmptyToken()
        {
            Assert.That(() => ServerPairing().WithAccessToken(default), Throws.ArgumentException);
        }

        [Test]
        public void WithDescriptions_UpdatesTheRemoteDescriptions()
        {

            var pairing      = ClientPairing();
            var newNode      = new NodeDescription(RemoteNodeId, "ACME", "EV charger", "WallBox-b200", EnergyManagementRole.RM, null, "Carport");
            var newEndpoint  = new EndpointDescription("ACME cloud v2", null, Deployment.WAN);

            var updated      = pairing.WithDescriptions(newNode, newEndpoint);

            Assert.Multiple(() => {
                Assert.That(updated.RemoteNodeDescription,     Is.SameAs(newNode));
                Assert.That(updated.RemoteEndpointDescription, Is.SameAs(newEndpoint));
                Assert.That(updated.RemoteNodeId,              Is.EqualTo(RemoteNodeId));
                Assert.That(updated.AccessToken,               Is.EqualTo(Token));
                Assert.That(updated.InitiateSessionUrl,        Is.EqualTo(SessionUrl));
                Assert.That(updated.CertificateFingerprints,   Is.EqualTo(pairing.CertificateFingerprints));
                Assert.That(updated.PairedAt,                  Is.EqualTo(PairedAt));
                Assert.That(pairing.RemoteNodeDescription,     Is.SameAs(RemoteNode), "the original is immutable");
                Assert.That(updated,                           Is.Not.EqualTo(pairing));
            });

        }

        [Test]
        public void WithDescriptions_RejectsAnotherRemoteNodeId_AndNulls()
        {

            var pairing   = ServerPairing();
            var otherNode = new NodeDescription(Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000003"), "ACME", "EV charger", "WallBox-b100", EnergyManagementRole.RM);

            Assert.Multiple(() => {
                Assert.That(() => pairing.WithDescriptions(otherNode,  RemoteEndpoint), Throws.ArgumentException);
                Assert.That(() => pairing.WithDescriptions(null!,      RemoteEndpoint), Throws.ArgumentNullException);
                Assert.That(() => pairing.WithDescriptions(RemoteNode, null!),          Throws.ArgumentNullException);
            });

        }

        #endregion


        #region Equality and hash codes

        [Test]
        public void Equals_ComparesAllProperties()
        {

            var a    = ServerPairing();
            var b    = ServerPairing();
            var c1   = ClientPairing();
            var c2   = ClientPairing();
            var none = Enumerable.Empty<Pairing>().FirstOrDefault();

            Assert.Multiple(() => {

                Assert.That(a.Equals(b),                Is.True);
                Assert.That(a.Equals((Object) b),       Is.True);
                Assert.That(a == b,                     Is.True);
                Assert.That(a != b,                     Is.False);
                Assert.That(a.GetHashCode(),            Is.EqualTo(b.GetHashCode()));

                Assert.That(c1,                         Is.EqualTo(c2));
                Assert.That(c1.GetHashCode(),           Is.EqualTo(c2.GetHashCode()));

                Assert.That(a.Equals(none),             Is.False);
                Assert.That(a == none,                  Is.False);
                Assert.That(none == a,                  Is.False);
                Assert.That(a != none,                  Is.True);
                Assert.That(none == null,               Is.True);

                Assert.That(a,                          Is.Not.EqualTo(c1), "communication server vs client");
                Assert.That(a,                          Is.Not.EqualTo(ServerPairing(OtherToken)), "different access token");
                Assert.That(a,                          Is.Not.EqualTo(new Pairing(LocalNodeId, RemoteNode, RemoteEndpoint, CommunicationRole.CommunicationServer, Token, PairedAt + TimeSpan.FromSeconds(1))), "different timestamp");
                Assert.That(a,                          Is.Not.EqualTo(new Pairing(LocalNodeId, RemoteNode, new EndpointDescription("Other", null, Deployment.WAN), CommunicationRole.CommunicationServer, Token, PairedAt)), "different remote endpoint");
                Assert.That(c1,                         Is.Not.EqualTo(ClientPairing(WithFingerprints: false)), "with vs without fingerprints");
                Assert.That(c1,                         Is.Not.EqualTo(new Pairing(LocalNodeId, RemoteNode, RemoteEndpoint, CommunicationRole.CommunicationClient, Token, PairedAt, SessionUrl, new Dictionary<String, CertificateFingerprint>(StringComparer.Ordinal) { ["SHA256"] = CAFingerprint })), "different fingerprint maps");
                Assert.That(c1,                         Is.Not.EqualTo(new Pairing(LocalNodeId, RemoteNode, RemoteEndpoint, CommunicationRole.CommunicationClient, Token, PairedAt, S2BaseURL.Parse("https://cem.example.com/other/"), Fingerprints())), "different session initiation URL");

            });

        }

        [Test]
        public void Equals_TreatsEquivalentRemoteDescriptionsAsEqual()
        {

            var a = ServerPairing();
            var b = new Pairing(LocalNodeId,
                                new NodeDescription(RemoteNodeId, "ACME", "EV charger", "WallBox-b100", EnergyManagementRole.RM, null, "Garage"),
                                new EndpointDescription("ACME cloud", null, Deployment.WAN),
                                CommunicationRole.CommunicationServer,
                                AccessToken.Parse(Token.Value),
                                new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.FromHours(2)));

            Assert.Multiple(() => {
                Assert.That(a,               Is.EqualTo(b), "value equality of descriptions, token bytes and instants");
                Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            });

        }

        #endregion

        #region ToString()

        [Test]
        public void ToString_DoesNotContainTheAccessToken()
        {

            var text = ClientPairing().ToString();

            Assert.Multiple(() => {
                Assert.That(text, Does.Not.Contain(Token.Value));
                Assert.That(text, Does.Contain(LocalNodeId.ToString()));
                Assert.That(text, Does.Contain(RemoteNodeId.ToString()));
                Assert.That(text, Does.Contain("CommunicationClient"));
                Assert.That(text, Does.Contain("2026-09-04T10:00:00Z"));
            });

        }

        #endregion

    }

}

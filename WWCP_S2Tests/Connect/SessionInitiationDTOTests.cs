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
    /// The S2 Connect session initiation DTOs (the initiateSession request and response, the
    /// HTTP 400 error message, the communication details returned by confirmAccessToken and
    /// the unpair request) and the endpoint record of the WAN endpoint registry: JSON round
    /// trips with the exact camelCase schema keys, mandatory-only instances, missing mandatory
    /// properties, strict additional-property rejection and the semantic rules.
    /// </summary>
    [TestFixture]
    public sealed class SessionInitiationDTOTests
    {

        #region Test data

        private static readonly Node_Id             ClientNodeId  = Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000001");
        private static readonly Node_Id             ServerNodeId  = Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000002");

        // The bytes 0x00..0x1f, standard Base64 with padding.
        private const           String              TokenBase64   = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";
        private static readonly AccessToken         Token         = AccessToken.       Parse(TokenBase64);
        private static readonly CommunicationToken  WSToken       = CommunicationToken.Parse(TokenBase64);

        private static NodeDescription ClientNode()
            => new (ClientNodeId, "ACME", "EV charger", "WallBox-b100", EnergyManagementRole.RM, URL.Parse("https://acme.example/logo.png"), "Garage");

        private static NodeDescription ServerNode()
            => new (ServerNodeId, "GridCo", "CEM", "HomeCEM 2", EnergyManagementRole.CEM);

        private static EndpointDescription LANEndpoint()
            => new ("EVSE1038", null, Deployment.LAN);

        private static EndpointDescription WANEndpoint()
            => new ("GridCo Cloud", URL.Parse("https://gridco.example/logo.png"), Deployment.WAN);

        private static InitiateSessionRequest FullRequest()
            => new (ClientNodeId,
                    ServerNodeId,
                    [ "v1.0.0", "0.0.2-beta" ],
                    [ CommunicationProtocol.WebSocket ],
                    ClientNode(),
                    LANEndpoint());

        private static InitiateSessionResponse FullResponse()
            => new (CommunicationProtocol.WebSocket,
                    "v1.0.0",
                    Token,
                    ServerNode(),
                    WANEndpoint());

        private static WebSocketCommunicationDetails WebSocketDetails()
            => new (WSToken, URL.Parse("wss://cem.example/s2"));

        private static EndpointRecord Record(String?                      Name          = null,
                                             String?                      Description   = null,
                                             IReadOnlyList<CountryCode>?  Regions       = null)

            => new (EndpointRecord_Id.Parse("0d1e2f3a-0000-4000-8000-000000000010"),
                    Name        ?? "GridCo Cloud CEM",
                    Description ?? "The cloud CEM of GridCo for households in the Benelux.",
                    URL.Parse("https://gridco.example/icons/32.png"),
                    URL.Parse("https://gridco.example/icons/128.png"),
                    URL.Parse("https://gridco.example/icons/512.png"),
                    S2BaseURL.Parse("https://hostname.tld/pairing/"),
                    Regions     ?? [ CountryCode.Parse("NL"), CountryCode.Parse("BE") ],
                    EndpointStatus.Public,
                    false,
                    true);

        private static EndpointRecord FullRecord()
            => Record();

        private static IEnumerable<String> Keys(JObject JSON)
            => JSON.Properties().Select(property => property.Name);

        private static JObject Without(JObject JSON, String Key)
        {
            var copy = (JObject) JSON.DeepClone();
            copy.Remove(Key);
            return copy;
        }

        #endregion


        #region CommunicationDetailsErrorMessage

        [Test]
        [S2C("SessionInitiation.CommunicationDetailsErrorMessage")]
        public void CommunicationDetailsErrorMessage_RoundTrips_WithCamelCaseKeys()
        {

            var message = new CommunicationDetailsErrorMessage(CommunicationDetailsError.IncompatibleS2MessageVersions, "Only v1.0.0 is supported.");
            var json    = message.ToJSON();

            Assert.That(Keys(json),                              Is.EqualTo(new[] { "errorMessage", "additionalInfo" }));
            Assert.That(json["errorMessage"]?.  Value<String>(), Is.EqualTo("IncompatibleS2MessageVersions"));
            Assert.That(json["additionalInfo"]?.Value<String>(), Is.EqualTo("Only v1.0.0 is supported."));

            Assert.That(CommunicationDetailsErrorMessage.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                Is.EqualTo(message));
            Assert.That(parsed!.Clone(),       Is.EqualTo(message));
            Assert.That(parsed.GetHashCode(),  Is.EqualTo(message.GetHashCode()));
            Assert.That(parsed.ToString(),     Is.EqualTo("IncompatibleS2MessageVersions: Only v1.0.0 is supported."));

        }

        [Test]
        public void CommunicationDetailsErrorMessage_MandatoryOnly_OmitsAdditionalInfo()
        {

            var minimal = new CommunicationDetailsErrorMessage(CommunicationDetailsError.NoLongerPaired);
            var json    = minimal.ToJSON();

            Assert.That(Keys(json), Is.EqualTo(new[] { "errorMessage" }));

            Assert.That(CommunicationDetailsErrorMessage.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                 Is.EqualTo(minimal));
            Assert.That(parsed!.AdditionalInfo, Is.Null);
            Assert.That(parsed.ToString(),      Is.EqualTo("NoLongerPaired"));

        }

        [Test]
        public void CommunicationDetailsErrorMessage_MissingErrorMessage_AndUnknownValue_Fail()
        {

            Assert.That(CommunicationDetailsErrorMessage.TryParse(JObject.Parse("""{ "additionalInfo": "x" }"""), out _, out var error), Is.False);
            Assert.That(error, Does.Contain("'errorMessage'"));

            var unknown = JObject.Parse("""{ "errorMessage": "Teapot" }""");
            Assert.That(CommunicationDetailsErrorMessage.TryParse(unknown, out _, out error), Is.False);
            Assert.That(error, Does.Contain("not a known value"));

            Assert.That(CommunicationDetailsErrorMessage.TryParse(unknown, out var lenient, out error, new S2ParserOptions { RejectUnknownEnumValues = false }), Is.True, error);
            Assert.That(lenient!.ErrorMessage.IsKnown, Is.False);

        }

        [Test]
        public void CommunicationDetailsErrorMessage_Strict_RejectsAdditionalProperties()
        {

            var json = JObject.Parse("""{ "errorMessage": "Other", "foo": 1 }""");

            Assert.That(CommunicationDetailsErrorMessage.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(CommunicationDetailsErrorMessage.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void CommunicationDetailsErrorMessage_RejectsAnEmptyError()
        {
            Assert.That(() => new CommunicationDetailsErrorMessage(default), Throws.ArgumentException);
        }

        #endregion

        #region InitiateSessionRequest

        [Test]
        [S2C("SessionInitiation.InitiateSession")]
        public void InitiateSessionRequest_RoundTrips_WithCamelCaseKeys()
        {

            var request = FullRequest();
            var json    = request.ToJSON();

            Assert.That(Keys(json), Is.EqualTo(new[] { "clientNodeId",
                                                       "serverNodeId",
                                                       "supportedS2MessageVersions",
                                                       "supportedCommunicationProtocols",
                                                       "clientNodeDescription",
                                                       "clientEndpointDescription" }));

            Assert.That(json["clientNodeId"]?.                   Value<String>(),  Is.EqualTo("6f2f5c1e-0000-4000-8000-000000000001"));
            Assert.That(json["serverNodeId"]?.                   Value<String>(),  Is.EqualTo("6f2f5c1e-0000-4000-8000-000000000002"));
            Assert.That(json["supportedS2MessageVersions"]?.     Values<String>(), Is.EqualTo(new[] { "v1.0.0", "0.0.2-beta" }));
            Assert.That(json["supportedCommunicationProtocols"]?.Values<String>(), Is.EqualTo(new[] { "WebSocket" }));
            Assert.That(json["clientNodeDescription"]?["modelName"]?.   Value<String>(), Is.EqualTo("WallBox-b100"));
            Assert.That(json["clientEndpointDescription"]?["deployment"]?.Value<String>(), Is.EqualTo("LAN"));

            Assert.That(InitiateSessionRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                            Is.EqualTo(request));
            Assert.That(parsed!.Clone(),                   Is.EqualTo(request));
            Assert.That(parsed.GetHashCode(),              Is.EqualTo(request.GetHashCode()));
            Assert.That(parsed.ClientNodeDescription,      Is.EqualTo(ClientNode()));
            Assert.That(parsed.ClientEndpointDescription,  Is.EqualTo(LANEndpoint()));
            Assert.That(parsed.ToString(),                 Does.Contain("v1.0.0").And.Contain("WebSocket"));

        }

        [Test]
        public void InitiateSessionRequest_MandatoryOnly_OmitsTheDescriptions()
        {

            var minimal = new InitiateSessionRequest(ClientNodeId, ServerNodeId, [ "v1.0.0" ], [ CommunicationProtocol.WebSocket ]);
            var json    = minimal.ToJSON();

            Assert.That(Keys(json), Is.EqualTo(new[] { "clientNodeId", "serverNodeId", "supportedS2MessageVersions", "supportedCommunicationProtocols" }));

            Assert.That(InitiateSessionRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                            Is.EqualTo(minimal));
            Assert.That(parsed!.ClientNodeDescription,     Is.Null);
            Assert.That(parsed.ClientEndpointDescription,  Is.Null);

        }

        [Test]
        public void InitiateSessionRequest_MissingMandatoryProperties_Fail()
        {

            var json = FullRequest().ToJSON();

            foreach (var key in new[] { "clientNodeId", "serverNodeId", "supportedS2MessageVersions", "supportedCommunicationProtocols" })
            {
                Assert.That(InitiateSessionRequest.TryParse(Without(json, key), out _, out var error), Is.False, key);
                Assert.That(error, Does.Contain($"'{key}'"));
            }

        }

        [Test]
        public void InitiateSessionRequest_Strict_RejectsAdditionalProperties_AlsoInNestedObjects()
        {

            var json = FullRequest().ToJSON();
            json["foo"] = 1;

            Assert.That(InitiateSessionRequest.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(InitiateSessionRequest.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

            // The parser options are passed on to the nested descriptions.
            var nested = FullRequest().ToJSON();
            nested["clientNodeDescription"]!["bar"] = 2;

            Assert.That(InitiateSessionRequest.TryParse(nested, out _, out _,     S2ParserOptions.Default), Is.True);
            Assert.That(InitiateSessionRequest.TryParse(nested, out _, out error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("bar").And.Contain("clientNodeDescription"));

        }

        [Test]
        [S2C("SessionInitiation.InitiateSession")]
        public void InitiateSessionRequest_RequiresAtLeastOneVersionAndOneProtocol()
        {

            Assert.That(() => new InitiateSessionRequest(ClientNodeId, ServerNodeId, [],           [ CommunicationProtocol.WebSocket ]), Throws.ArgumentException);
            Assert.That(() => new InitiateSessionRequest(ClientNodeId, ServerNodeId, [ "v1.0.0" ], []),                                  Throws.ArgumentException);

            var noVersions = FullRequest().ToJSON();
            noVersions["supportedS2MessageVersions"] = new JArray();

            Assert.That(InitiateSessionRequest.TryParse(noVersions, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at least one"));

            var noProtocols = FullRequest().ToJSON();
            noProtocols["supportedCommunicationProtocols"] = new JArray();

            Assert.That(InitiateSessionRequest.TryParse(noProtocols, out _, out error), Is.False);
            Assert.That(error, Does.Contain("at least one"));

        }

        [Test]
        public void InitiateSessionRequest_UnknownProtocol_IsRejectedByDefault_ButToleratedWhenAllowed()
        {

            // A future client may announce protocols this library does not know; the server can
            // tolerate them (RejectUnknownEnumValues = false) and still negotiate WebSocket.
            var json = JObject.Parse("""
                {
                  "clientNodeId":                    "6f2f5c1e-0000-4000-8000-000000000001",
                  "serverNodeId":                    "6f2f5c1e-0000-4000-8000-000000000002",
                  "supportedS2MessageVersions":      [ "v1.0.0" ],
                  "supportedCommunicationProtocols": [ "MQTT", "WebSocket" ]
                }
                """);

            Assert.That(InitiateSessionRequest.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("MQTT"));

            Assert.That(InitiateSessionRequest.TryParse(json, out var parsed, out error, new S2ParserOptions { RejectUnknownEnumValues = false }), Is.True, error);
            Assert.That(parsed!.SupportedCommunicationProtocols,          Has.Count.EqualTo(2));
            Assert.That(parsed.SupportedCommunicationProtocols[0].IsKnown, Is.False);
            Assert.That(parsed.SupportedCommunicationProtocols[1],         Is.EqualTo(CommunicationProtocol.WebSocket));

        }

        #endregion

        #region InitiateSessionResponse

        [Test]
        [S2C("SessionInitiation.InitiateSession")]
        public void InitiateSessionResponse_RoundTrips_WithCamelCaseKeys()
        {

            var response = FullResponse();
            var json     = response.ToJSON();

            Assert.That(Keys(json), Is.EqualTo(new[] { "selectedCommunicationProtocol",
                                                       "selectedS2MessageVersion",
                                                       "accessToken",
                                                       "serverNodeDescription",
                                                       "serverEndpointDescription" }));

            Assert.That(json["selectedCommunicationProtocol"]?.Value<String>(), Is.EqualTo("WebSocket"));
            Assert.That(json["selectedS2MessageVersion"]?.     Value<String>(), Is.EqualTo("v1.0.0"));
            Assert.That(json["accessToken"]?.                  Value<String>(), Is.EqualTo(TokenBase64));
            Assert.That(json["serverNodeDescription"]?["role"]?.          Value<String>(), Is.EqualTo("CEM"));
            Assert.That(json["serverEndpointDescription"]?["deployment"]?.Value<String>(), Is.EqualTo("WAN"));

            Assert.That(InitiateSessionResponse.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                            Is.EqualTo(response));
            Assert.That(parsed!.Clone(),                   Is.EqualTo(response));
            Assert.That(parsed.GetHashCode(),              Is.EqualTo(response.GetHashCode()));
            Assert.That(parsed.AccessToken.Value,          Is.EqualTo(TokenBase64));
            Assert.That(parsed.ServerNodeDescription,      Is.EqualTo(ServerNode()));
            Assert.That(parsed.ServerEndpointDescription,  Is.EqualTo(WANEndpoint()));

        }

        [Test]
        public void InitiateSessionResponse_MandatoryOnly_OmitsTheDescriptions_AndAllowsAnEmptyVersion()
        {

            // The schema does not constrain selectedS2MessageVersion, so an empty string is valid.
            var minimal = new InitiateSessionResponse(CommunicationProtocol.WebSocket, "", Token);
            var json    = minimal.ToJSON();

            Assert.That(Keys(json), Is.EqualTo(new[] { "selectedCommunicationProtocol", "selectedS2MessageVersion", "accessToken" }));

            Assert.That(InitiateSessionResponse.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                            Is.EqualTo(minimal));
            Assert.That(parsed!.SelectedS2MessageVersion,  Is.Empty);
            Assert.That(parsed.ServerNodeDescription,      Is.Null);
            Assert.That(parsed.ServerEndpointDescription,  Is.Null);

        }

        [Test]
        public void InitiateSessionResponse_MissingMandatoryProperties_Fail()
        {

            var json = FullResponse().ToJSON();

            foreach (var key in new[] { "selectedCommunicationProtocol", "selectedS2MessageVersion", "accessToken" })
            {
                Assert.That(InitiateSessionResponse.TryParse(Without(json, key), out _, out var error), Is.False, key);
                Assert.That(error, Does.Contain($"'{key}'"));
            }

        }

        [Test]
        public void InitiateSessionResponse_InvalidAccessToken_FailsWithoutLeakingIt()
        {

            var json = FullResponse().ToJSON();
            json["accessToken"] = "ab-cd_ef==";   // Base64Url is not standard Base64

            Assert.That(InitiateSessionResponse.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("'accessToken'"));
            Assert.That(error, Does.Not.Contain("ab-cd_ef=="));

        }

        [Test]
        public void InitiateSessionResponse_Strict_RejectsAdditionalProperties()
        {

            var json = FullResponse().ToJSON();
            json["foo"] = 1;

            Assert.That(InitiateSessionResponse.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(InitiateSessionResponse.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("SessionInitiation.InitiateSession")]
        public void InitiateSessionResponse_RequiresAVersionAProtocolAndAToken()
        {
            Assert.That(() => new InitiateSessionResponse(CommunicationProtocol.WebSocket, null!,    Token),   Throws.ArgumentNullException);
            Assert.That(() => new InitiateSessionResponse(CommunicationProtocol.WebSocket, "v1.0.0", default), Throws.ArgumentException);
            Assert.That(() => new InitiateSessionResponse(default,                         "v1.0.0", Token),   Throws.ArgumentException);
        }

        [Test]
        public void InitiateSessionResponse_ToString_NeverContainsTheAccessToken()
        {

            var response = FullResponse();

            Assert.That(response.ToString(), Does.Not.Contain(TokenBase64));
            Assert.That(response.ToString(), Does.Contain("WebSocket").And.Contain("v1.0.0"));

        }

        #endregion

        #region WebSocketCommunicationDetails and the CommunicationDetails dispatch

        [Test]
        [S2C("SessionInitiation.ConfirmAccessToken")]
        public void WebSocketCommunicationDetails_RoundTrips_WithCamelCaseKeys()
        {

            var details = WebSocketDetails();
            var json    = details.ToJSON();

            // All three properties are mandatory: there is no smaller instance.
            Assert.That(Keys(json), Is.EqualTo(new[] { "communicationProtocol", "websocketToken", "websocketUrl" }));
            Assert.That(json["communicationProtocol"]?.Value<String>(), Is.EqualTo("WebSocket"));
            Assert.That(json["websocketToken"]?.       Value<String>(), Is.EqualTo(TokenBase64));
            Assert.That(json["websocketUrl"]?.         Value<String>(), Is.EqualTo("wss://cem.example/s2"));

            Assert.That(WebSocketCommunicationDetails.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                        Is.EqualTo(details));
            Assert.That(parsed!.Clone(),               Is.EqualTo(details));
            Assert.That(parsed.GetHashCode(),          Is.EqualTo(details.GetHashCode()));
            Assert.That(parsed.CommunicationProtocol,  Is.EqualTo(CommunicationProtocol.WebSocket));
            Assert.That(parsed.WebsocketToken.Value,   Is.EqualTo(TokenBase64));
            Assert.That(parsed.WebsocketUrl.ToString(), Is.EqualTo("wss://cem.example/s2"));

            // The WebSocket token is a secret.
            Assert.That(details.ToString(), Does.Not.Contain(TokenBase64));
            Assert.That(details.ToString(), Does.Contain("wss://cem.example/s2"));

        }

        [Test]
        public void WebSocketCommunicationDetails_MissingMandatoryProperties_Fail()
        {

            var json = WebSocketDetails().ToJSON();

            foreach (var key in new[] { "communicationProtocol", "websocketToken", "websocketUrl" })
            {
                Assert.That(WebSocketCommunicationDetails.TryParse(Without(json, key), out _, out var error), Is.False, key);
                Assert.That(error, Does.Contain($"'{key}'"));
            }

            var wrongProtocol = WebSocketDetails().ToJSON();
            wrongProtocol["communicationProtocol"] = "MQTT";

            Assert.That(WebSocketCommunicationDetails.TryParse(wrongProtocol, out _, out var protocolError, new S2ParserOptions { RejectUnknownEnumValues = false }), Is.False);
            Assert.That(protocolError, Does.Contain("MQTT"));

        }

        [Test]
        public void WebSocketCommunicationDetails_InvalidToken_FailsWithoutLeakingIt()
        {

            var json = WebSocketDetails().ToJSON();
            json["websocketToken"] = "ab-cd_ef==";

            Assert.That(WebSocketCommunicationDetails.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("'websocketToken'"));
            Assert.That(error, Does.Not.Contain("ab-cd_ef=="));

        }

        [Test]
        public void WebSocketCommunicationDetails_Strict_RejectsAdditionalProperties()
        {

            var json = WebSocketDetails().ToJSON();
            json["foo"] = 1;

            Assert.That(WebSocketCommunicationDetails.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(WebSocketCommunicationDetails.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("SessionInitiation.WebSocketURL")]
        public void WebSocketCommunicationDetails_RequiresWSS_UnlessInsecureURLsAreAllowed()
        {

            // The constructor accepts "ws://" (development); the wire policy is enforced by TryParse.
            var insecure = new WebSocketCommunicationDetails(WSToken, URL.Parse("ws://cem.local:8080/s2"));
            var json     = insecure.ToJSON();

            Assert.That(WebSocketCommunicationDetails.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("'websocketUrl'").And.Contain("wss"));
            Assert.That(WebSocketCommunicationDetails.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.False);

            Assert.That(WebSocketCommunicationDetails.TryParse(json, out var parsed, out error, new S2ParserOptions { AllowInsecureURLs = true }), Is.True, error);
            Assert.That(parsed, Is.EqualTo(insecure));

            // Anything but wss:// (or ws:// when allowed) is rejected.
            var https = WebSocketDetails().ToJSON();
            https["websocketUrl"] = "https://cem.example/s2";

            Assert.That(WebSocketCommunicationDetails.TryParse(https, out _, out error, new S2ParserOptions { AllowInsecureURLs = true }), Is.False);
            Assert.That(error, Does.Contain("'websocketUrl'"));

            Assert.That(() => new WebSocketCommunicationDetails(WSToken, URL.Parse("https://cem.example/s2")), Throws.ArgumentException);
            Assert.That(() => new WebSocketCommunicationDetails(default, URL.Parse("wss://cem.example/s2")),   Throws.ArgumentException);

        }

        [Test]
        [S2C("SessionInitiation.CommunicationDetails")]
        public void CommunicationDetails_DispatchesOnTheDiscriminator()
        {

            var json = WebSocketDetails().ToJSON();

            Assert.That(CommunicationDetails.TryParse(json, out var details, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(details,                          Is.TypeOf<WebSocketCommunicationDetails>());
            Assert.That(details,                          Is.EqualTo(WebSocketDetails()));
            Assert.That(details!.CommunicationProtocol,   Is.EqualTo(CommunicationProtocol.WebSocket));
            Assert.That(JToken.DeepEquals(details.ToJSON(), json), Is.True);

            // An unknown protocol is an error, because no subtype can parse it...
            var mqtt = JObject.Parse("""{ "communicationProtocol": "MQTT", "brokerUrl": "mqtts://broker.example" }""");

            Assert.That(CommunicationDetails.TryParse(mqtt, out _, out error), Is.False);
            Assert.That(error, Does.Contain("MQTT"));

            // ...even when unknown enumeration values are tolerated.
            Assert.That(CommunicationDetails.TryParse(mqtt, out _, out error, new S2ParserOptions { RejectUnknownEnumValues = false }), Is.False);
            Assert.That(error, Does.Contain("MQTT"));

            // A missing discriminator is an error naming it.
            var missing = JObject.Parse("""{ "websocketToken": "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=", "websocketUrl": "wss://cem.example/s2" }""");

            Assert.That(CommunicationDetails.TryParse(missing, out _, out error), Is.False);
            Assert.That(error, Does.Contain("'communicationProtocol'"));

            // The URL scheme policy applies through the dispatcher as well.
            var ws = new WebSocketCommunicationDetails(WSToken, URL.Parse("ws://cem.local/s2")).ToJSON();

            Assert.That(CommunicationDetails.TryParse(ws, out _,            out error), Is.False);
            Assert.That(CommunicationDetails.TryParse(ws, out var insecure, out error, new S2ParserOptions { AllowInsecureURLs = true }), Is.True, error);
            Assert.That(insecure, Is.TypeOf<WebSocketCommunicationDetails>());

        }

        #endregion

        #region UnpairRequest

        [Test]
        [S2C("SessionInitiation.Unpair")]
        public void UnpairRequest_RoundTrips_WithCamelCaseKeys()
        {

            var request = new UnpairRequest(ClientNodeId, ServerNodeId);
            var json    = request.ToJSON();

            // Both properties are mandatory: there is no smaller instance.
            Assert.That(Keys(json),                            Is.EqualTo(new[] { "clientNodeId", "serverNodeId" }));
            Assert.That(json["clientNodeId"]?.Value<String>(), Is.EqualTo("6f2f5c1e-0000-4000-8000-000000000001"));
            Assert.That(json["serverNodeId"]?.Value<String>(), Is.EqualTo("6f2f5c1e-0000-4000-8000-000000000002"));

            Assert.That(UnpairRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                Is.EqualTo(request));
            Assert.That(parsed!.Clone(),       Is.EqualTo(request));
            Assert.That(parsed.GetHashCode(),  Is.EqualTo(request.GetHashCode()));
            Assert.That(parsed.ToString(),     Does.Contain(ClientNodeId.ToString()).And.Contain(ServerNodeId.ToString()));

            Assert.That(new UnpairRequest(ServerNodeId, ClientNodeId), Is.Not.EqualTo(request));

        }

        [Test]
        public void UnpairRequest_MissingOrInvalidMandatoryProperties_Fail()
        {

            var json = new UnpairRequest(ClientNodeId, ServerNodeId).ToJSON();

            foreach (var key in new[] { "clientNodeId", "serverNodeId" })
            {
                Assert.That(UnpairRequest.TryParse(Without(json, key), out _, out var error), Is.False, key);
                Assert.That(error, Does.Contain($"'{key}'"));
            }

            var notAUUID = JObject.Parse("""{ "clientNodeId": "client-1", "serverNodeId": "6f2f5c1e-0000-4000-8000-000000000002" }""");

            Assert.That(UnpairRequest.TryParse(notAUUID, out _, out var idError), Is.False);
            Assert.That(idError, Does.Contain("'clientNodeId'"));

        }

        [Test]
        public void UnpairRequest_Strict_RejectsAdditionalProperties()
        {

            var json = new UnpairRequest(ClientNodeId, ServerNodeId).ToJSON();
            json["foo"] = 1;

            Assert.That(UnpairRequest.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(UnpairRequest.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        #endregion

        #region EndpointRecord

        [Test]
        [S2C("Registry.EndpointRecord")]
        public void EndpointRecord_RoundTrips_WithCamelCaseKeys()
        {

            var record = FullRecord();
            var json   = record.ToJSON();

            // All eleven properties are mandatory: there is no smaller instance.
            Assert.That(Keys(json), Is.EqualTo(new[] { "id", "name", "description", "icon32", "icon128", "icon512",
                                                       "pairingUrl", "regions", "status", "cem", "rm" }));

            Assert.That(json["id"]?.         Value<String>(),  Is.EqualTo("0d1e2f3a-0000-4000-8000-000000000010"));
            Assert.That(json["name"]?.       Value<String>(),  Is.EqualTo("GridCo Cloud CEM"));
            Assert.That(json["description"]?.Value<String>(),  Is.EqualTo("The cloud CEM of GridCo for households in the Benelux."));
            Assert.That(json["icon32"]?.     Value<String>(),  Is.EqualTo("https://gridco.example/icons/32.png"));
            Assert.That(json["icon128"]?.    Value<String>(),  Is.EqualTo("https://gridco.example/icons/128.png"));
            Assert.That(json["icon512"]?.    Value<String>(),  Is.EqualTo("https://gridco.example/icons/512.png"));
            Assert.That(json["pairingUrl"]?. Value<String>(),  Is.EqualTo("https://hostname.tld/pairing/"));
            Assert.That(json["regions"]?.    Values<String>(), Is.EqualTo(new[] { "NL", "BE" }));
            Assert.That(json["status"]?.     Value<String>(),  Is.EqualTo("public"));
            Assert.That(json["cem"]?.        Value<Boolean>(), Is.False);
            Assert.That(json["rm"]?.         Value<Boolean>(), Is.True);

            // Strict requires UUIDs for identifiers; country codes are not identifiers and still parse.
            Assert.That(EndpointRecord.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                                          Is.EqualTo(record));
            Assert.That(parsed!.Clone(),                                 Is.EqualTo(record));
            Assert.That(parsed.GetHashCode(),                            Is.EqualTo(record.GetHashCode()));
            Assert.That(parsed.PairingUrl.ForVersion("v1").ToString(),   Is.EqualTo("https://hostname.tld/pairing/v1/"));
            Assert.That(parsed.ToString(),                               Does.Contain("GridCo Cloud CEM").And.Contain("NL, BE").And.Contain("RM"));

        }

        [Test]
        [S2C("Registry.EndpointRecord")]
        public void EndpointRecord_ParsesTheRegistryExample()
        {

            // A record as the WAN endpoint registry serves it: a name of at most 50 characters, three PNG
            // icons, the pairing base URL, the regions, a public status and the roles of the hosted nodes.
            var json = JObject.Parse("""
                {
                  "id":          "0d1e2f3a-0000-4000-8000-000000000010",
                  "name":        "GridCo Cloud CEM",
                  "description": "The cloud CEM of GridCo for households in the Benelux.",
                  "icon32":      "https://gridco.example/icons/32.png",
                  "icon128":     "https://gridco.example/icons/128.png",
                  "icon512":     "https://gridco.example/icons/512.png",
                  "pairingUrl":  "https://hostname.tld/pairing/",
                  "regions":     [ "NL", "BE" ],
                  "status":      "public",
                  "cem":         false,
                  "rm":          true
                }
                """);

            Assert.That(EndpointRecord.TryParse(json, out var record, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(record,                Is.EqualTo(FullRecord()));
            Assert.That(record!.Name,          Has.Length.LessThanOrEqualTo(EndpointRecord.MaxNameLength));
            Assert.That(record.Description,    Has.Length.LessThanOrEqualTo(EndpointRecord.MaxDescriptionLength));
            Assert.That(record.Icon32.  ToString(), Does.EndWith(".png"));
            Assert.That(record.Icon128. ToString(), Does.EndWith(".png"));
            Assert.That(record.Icon512. ToString(), Does.EndWith(".png"));
            Assert.That(record.PairingUrl.Value, Is.EqualTo("https://hostname.tld/pairing/"));
            Assert.That(record.PairingUrl.IsHTTPS, Is.True);
            Assert.That(record.Regions,        Is.EqualTo(new[] { CountryCode.Parse("NL"), CountryCode.Parse("BE") }));
            Assert.That(record.Status,         Is.EqualTo(EndpointStatus.Public));
            Assert.That(record.CEM,            Is.False);
            Assert.That(record.RM,             Is.True);

            Assert.That(JToken.DeepEquals(record.ToJSON(), json), Is.True);

        }

        [Test]
        public void EndpointRecord_MissingOrInvalidMandatoryProperties_Fail()
        {

            var json = FullRecord().ToJSON();

            foreach (var key in new[] { "id", "name", "description", "icon32", "icon128", "icon512", "pairingUrl", "regions", "status", "cem", "rm" })
            {
                Assert.That(EndpointRecord.TryParse(Without(json, key), out _, out var error), Is.False, key);
                Assert.That(error, Does.Contain($"'{key}'"));
            }

            var badIcon = FullRecord().ToJSON();
            badIcon["icon128"] = "";

            Assert.That(EndpointRecord.TryParse(badIcon, out _, out var iconError), Is.False);
            Assert.That(iconError, Does.Contain("'icon128'"));

            var badId = FullRecord().ToJSON();
            badId["id"] = "endpoint-1";

            Assert.That(EndpointRecord.TryParse(badId, out _, out var idError), Is.False);
            Assert.That(idError, Does.Contain("'id'"));

            var unknownStatus = FullRecord().ToJSON();
            unknownStatus["status"] = "beta";

            Assert.That(EndpointRecord.TryParse(unknownStatus, out _, out var statusError), Is.False);
            Assert.That(statusError, Does.Contain("not a known value"));
            Assert.That(EndpointRecord.TryParse(unknownStatus, out var lenient, out statusError, new S2ParserOptions { RejectUnknownEnumValues = false }), Is.True, statusError);
            Assert.That(lenient!.Status.IsKnown, Is.False);

        }

        [Test]
        public void EndpointRecord_Strict_RejectsAdditionalProperties()
        {

            var json = FullRecord().ToJSON();
            json["foo"] = 1;

            Assert.That(EndpointRecord.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(EndpointRecord.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Registry.EndpointRecord")]
        public void EndpointRecord_EnforcesTheLengthAndRegionRules()
        {

            Assert.That(() => Record(Name:        new String('x', 50)),  Throws.Nothing);
            Assert.That(() => Record(Name:        new String('x', 51)),  Throws.ArgumentException);
            Assert.That(() => Record(Description: new String('x', 500)), Throws.Nothing);
            Assert.That(() => Record(Description: new String('x', 501)), Throws.ArgumentException);

            // JSON Schema counts Unicode code points: 50 astral characters are 100 UTF-16 code units, but still valid.
            Assert.That(() => Record(Name: String.Concat(Enumerable.Repeat("\U0001F50C", 50))), Throws.Nothing);
            Assert.That(() => Record(Name: String.Concat(Enumerable.Repeat("\U0001F50C", 51))), Throws.ArgumentException);

            Assert.That(() => Record(Regions: []),                                                   Throws.ArgumentException);
            Assert.That(() => Record(Regions: [ CountryCode.Parse("NL"), CountryCode.Parse("NL") ]), Throws.ArgumentException);

            var tooLong = FullRecord().ToJSON();
            tooLong["name"] = new String('x', 51);

            Assert.That(EndpointRecord.TryParse(tooLong, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("50"));

            var noRegions = FullRecord().ToJSON();
            noRegions["regions"] = new JArray();

            Assert.That(EndpointRecord.TryParse(noRegions, out _, out error), Is.False);
            Assert.That(error, Does.Contain("'regions'"));

            var duplicate = FullRecord().ToJSON();
            duplicate["regions"] = new JArray("NL", "NL");

            Assert.That(EndpointRecord.TryParse(duplicate, out _, out error), Is.False);
            Assert.That(error, Does.Contain("unique"));

            var lowerCase = FullRecord().ToJSON();
            lowerCase["regions"] = new JArray("nl");

            Assert.That(EndpointRecord.TryParse(lowerCase, out _, out error), Is.False);
            Assert.That(error, Does.Contain("'regions'"));

        }

        [Test]
        [S2C("Pairing.PairingURL")]
        public void EndpointRecord_PairingUrl_FollowsTheBaseURLRules()
        {

            var noSlash = FullRecord().ToJSON();
            noSlash["pairingUrl"] = "https://hostname.tld/pairing";

            Assert.That(EndpointRecord.TryParse(noSlash, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("'pairingUrl'").And.Contain("slash"));

            var versioned = FullRecord().ToJSON();
            versioned["pairingUrl"] = "https://hostname.tld/pairing/v1/";

            Assert.That(EndpointRecord.TryParse(versioned, out _, out error), Is.False);
            Assert.That(error, Does.Contain("'pairingUrl'").And.Contain("version"));

            var http = FullRecord().ToJSON();
            http["pairingUrl"] = "http://hostname.tld/pairing/";

            Assert.That(EndpointRecord.TryParse(http, out _, out error), Is.False);
            Assert.That(error, Does.Contain("'pairingUrl'").And.Contain("https"));

            Assert.That(EndpointRecord.TryParse(http, out var insecure, out error, new S2ParserOptions { AllowInsecureURLs = true }), Is.True, error);
            Assert.That(insecure!.PairingUrl.IsHTTPS, Is.False);

        }

        #endregion

    }

}

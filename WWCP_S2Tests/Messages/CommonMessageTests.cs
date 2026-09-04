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

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Messages
{

    /// <summary>
    /// Tests of the message templates (ReceptionStatus, Handshake, SelectControlType)
    /// and of the message parser's error mapping (PLAN.md §3.2).
    /// </summary>
    [TestFixture]
    public sealed class CommonMessageTests
    {

        #region ReceptionStatus

        [Test]
        public void ReceptionStatus_RoundTrips_AndIsSchemaValid()
        {

            var status = new ReceptionStatus(Message_Id.Parse("eab48b05-99be-4c34-899d-30edaf5626db"),
                                             ReceptionStatusValue.InvalidContent,
                                             "unknown actuator");
            var json   = status.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "ReceptionStatus");

            Assert.That(json["message_type"]?.Value<String>(), Is.EqualTo("ReceptionStatus"));
            Assert.That(json.ContainsKey("message_id"),        Is.False);

            Assert.That(ReceptionStatus.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,       Is.EqualTo(status));
            Assert.That(parsed!.IsOK, Is.False);

            var ok = ReceptionStatus.OK(Message_Id.NewRandom);
            Assert.That(ok.IsOK,                                        Is.True);
            Assert.That(ok.ToJSON().ContainsKey("diagnostic_label"),    Is.False);
            S2SchemaValidator.AssertValidMessage(ok.ToJSON(), "ReceptionStatus");

        }

        #endregion

        #region Handshake

        [Test]
        public void Handshake_MatchesTheDocumentationExample()
        {

            var json = JObject.Parse("""
                {
                  "message_type": "Handshake",
                  "message_id": "ea2e0f4f-5294-4578-a050-73fdd6110eec",
                  "role": "RM",
                  "supported_protocol_versions": [ "0.0.2-beta" ]
                }
                """);

            Assert.That(Handshake.TryParse(json, out var handshake, out var error), Is.True, error);
            Assert.That(handshake!.Role,                              Is.EqualTo(EnergyManagementRole.RM));
            Assert.That(handshake.SupportedProtocolVersions,          Is.EqualTo(new[] { "0.0.2-beta" }));
            Assert.That(handshake.MessageId.ToString(),               Is.EqualTo("ea2e0f4f-5294-4578-a050-73fdd6110eec"));

            var reserialised = handshake.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "Handshake");
            Assert.That(JToken.DeepEquals(reserialised, json), Is.True, reserialised.ToString());

        }

        [Test]
        public void Handshake_OfCEM_MayOmitVersions_ButRMMustAnnounceThem()
        {

            var cem = new Handshake(EnergyManagementRole.CEM);
            Assert.That(cem.ToJSON().ContainsKey("supported_protocol_versions"), Is.False);
            S2SchemaValidator.AssertValidMessage(cem.ToJSON(), "Handshake");

            Assert.That(() => new Handshake(EnergyManagementRole.RM),      Throws.ArgumentException);
            Assert.That(() => new Handshake(EnergyManagementRole.CEM, []), Throws.ArgumentException);

            var json = JObject.Parse("""{ "message_type": "Handshake", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "role": "RM" }""");
            Assert.That(Handshake.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("supported protocol versions"));

        }

        #endregion

        #region SelectControlType

        [Test]
        public void SelectControlType_RoundTrips_AndIsSchemaValid()
        {

            var message = new SelectControlType(ControlType.FillRateBasedControl);
            var json    = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "SelectControlType");

            Assert.That(SelectControlType.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed, Is.EqualTo(message));

            var wrongType = JObject.Parse("""{ "message_type": "Handshake", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "control_type": "NO_SELECTION" }""");
            Assert.That(SelectControlType.TryParse(wrongType, out _, out error), Is.False);
            Assert.That(error, Does.Contain("Unexpected message type"));

        }

        #endregion

        #region S2MessageParser

        [Test]
        public void MessageParser_DispatchesOnMessageType()
        {

            var text = new SelectControlType(ControlType.NoSelection, Message_Id.Parse("fe9f4f07-c731-4f91-8801-3b53a9fecaaf")).ToJSON().ToString();

            Assert.That(S2MessageParser.TryParse(text, null, out var message, out var error), Is.True, error?.DiagnosticLabel);
            Assert.That(message,                                Is.TypeOf<SelectControlType>());
            Assert.That(((SelectControlType) message!).ControlType, Is.EqualTo(ControlType.NoSelection));

            Assert.That(S2MessageParser.TryParse("""{ "message_type": "ReceptionStatus", "subject_message_id": "fe9f4f07-c731-4f91-8801-3b53a9fecaaf", "status": "OK" }""",
                                                 null, out message, out error), Is.True, error?.DiagnosticLabel);
            Assert.That(message, Is.TypeOf<ReceptionStatus>());

        }

        [Test]
        [S2C("Rules.ReceptionStatus.Table")]
        public void MessageParser_MapsErrors_ToReceptionStatusValues()
        {

            // not JSON → INVALID_DATA / null UUID
            Assert.That(S2MessageParser.TryParse("this is not json", null, out _, out var error), Is.False);
            Assert.That(error!.Status,           Is.EqualTo(ReceptionStatusValue.InvalidData));
            Assert.That(error.SubjectMessageId,  Is.EqualTo(Message_Id.Null));

            // JSON without message_id → INVALID_DATA / null UUID
            Assert.That(S2MessageParser.TryParse("""{ "message_type": "SelectControlType", "control_type": "NO_SELECTION" }""", null, out _, out error), Is.False);
            Assert.That(error!.Status,           Is.EqualTo(ReceptionStatusValue.InvalidData));
            Assert.That(error.SubjectMessageId,  Is.EqualTo(Message_Id.Null));

            // unknown message type with message_id → INVALID_MESSAGE / that id
            Assert.That(S2MessageParser.TryParse("""{ "message_type": "FUTURE.Message", "message_id": "9a8d2f1c-0000-4000-8000-000000000001" }""", null, out _, out error), Is.False);
            Assert.That(error!.Status,                      Is.EqualTo(ReceptionStatusValue.InvalidMessage));
            Assert.That(error.SubjectMessageId.ToString(),  Is.EqualTo("9a8d2f1c-0000-4000-8000-000000000001"));

            // schema failure with message_id → INVALID_MESSAGE / that id
            Assert.That(S2MessageParser.TryParse("""{ "message_type": "SelectControlType", "message_id": "9a8d2f1c-0000-4000-8000-000000000001" }""", null, out _, out error), Is.False);
            Assert.That(error!.Status,                      Is.EqualTo(ReceptionStatusValue.InvalidMessage));
            Assert.That(error.SubjectMessageId.ToString(),  Is.EqualTo("9a8d2f1c-0000-4000-8000-000000000001"));
            Assert.That(error.DiagnosticLabel,              Does.Contain("control_type"));

            var reception = error.ToReceptionStatus();
            Assert.That(reception.Status,                   Is.EqualTo(ReceptionStatusValue.InvalidMessage));
            S2SchemaValidator.AssertValidMessage(reception.ToJSON(), "ReceptionStatus");

            // message_id present but message_type missing → INVALID_MESSAGE against that id
            Assert.That(S2MessageParser.TryParse("""{ "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "control_type": "NO_SELECTION" }""", null, out _, out error), Is.False);
            Assert.That(error!.Status,                      Is.EqualTo(ReceptionStatusValue.InvalidMessage));
            Assert.That(error.SubjectMessageId.ToString(),  Is.EqualTo("9a8d2f1c-0000-4000-8000-000000000001"));

            // a malformed ReceptionStatus is flagged so that the session never answers it
            Assert.That(S2MessageParser.TryParse("""{ "message_type": "ReceptionStatus", "status": "OK" }""", null, out _, out error), Is.False);
            Assert.That(error!.IsReceptionStatus,           Is.True);
            Assert.That(error.MessageType,                  Is.EqualTo("ReceptionStatus"));

        }

        #endregion

    }

}

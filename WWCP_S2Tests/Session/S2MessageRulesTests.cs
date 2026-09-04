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

using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Session
{

    /// <summary>
    /// The "State of communication" table of S2 Connect 1.0.0 as test oracle for
    /// S2MessageRules, including the documented exceptions (PLAN.md §3.4).
    /// </summary>
    [TestFixture]
    [S2C("Communication.StateOfCommunication")]
    public sealed class S2MessageRulesTests
    {

        private static readonly EnergyManagementRole CEM = EnergyManagementRole.CEM;
        private static readonly EnergyManagementRole RM  = EnergyManagementRole.RM;

        private static S2RuleVerdict Check(String MessageType, EnergyManagementRole Sender, S2SessionState State, ControlType? Active = null, S2SessionMode Mode = S2SessionMode.S2Connect)
            => S2MessageRules.Check(MessageType, Sender, Mode, State, Active);

        // The spec table, row "WebSocket Connected".
        [Test]
        public void WebSocketConnected_AllowsTheCommonMessagesOnly()
        {

            Assert.That(Check("SelectControlType",       CEM, S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("SessionRequest",          CEM, S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("ReceptionStatus",         CEM, S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.Allowed));

            Assert.That(Check("ResourceManagerDetails",  RM,  S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("PowerMeasurement",        RM,  S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("PowerForecast",           RM,  S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("SessionRequest",          RM,  S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.Allowed));

            Assert.That(Check("FRBC.Instruction",        CEM, S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.OutOfState));
            Assert.That(Check("FRBC.SystemDescription",  RM,  S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.OutOfState));
            Assert.That(Check("RevokeObject",            RM,  S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.OutOfState));
            Assert.That(Check("InstructionStatusUpdate", RM,  S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.OutOfState));

        }

        [Test]
        public void Directions_FollowTheTable()
        {
            Assert.That(Check("ResourceManagerDetails",  CEM, S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.WrongDirection));
            Assert.That(Check("SelectControlType",       RM,  S2SessionState.WebSocketConnected), Is.EqualTo(S2RuleVerdict.WrongDirection));
            Assert.That(Check("FRBC.Instruction",        RM,  S2SessionState.ControlTypeActivated, ControlType.FillRateBasedControl), Is.EqualTo(S2RuleVerdict.WrongDirection));
            Assert.That(Check("FRBC.ActuatorStatus",     CEM, S2SessionState.ControlTypeActivated, ControlType.FillRateBasedControl), Is.EqualTo(S2RuleVerdict.WrongDirection));
            Assert.That(Check("InstructionStatusUpdate", CEM, S2SessionState.ControlTypeActivated, ControlType.FillRateBasedControl), Is.EqualTo(S2RuleVerdict.WrongDirection));
        }

        // The spec table, rows "ControlType … activated" for all five control types.
        [TestCase("PEBC.Instruction",                  "PEBC.EnergyConstraint",           "POWER_ENVELOPE_BASED_CONTROL")]
        [TestCase("PPBC.ScheduleInstruction",          "PPBC.PowerProfileDefinition",     "POWER_PROFILE_BASED_CONTROL")]
        [TestCase("PPBC.StartInterruptionInstruction", "PPBC.PowerProfileStatus",         "POWER_PROFILE_BASED_CONTROL")]
        [TestCase("PPBC.EndInterruptionInstruction",   "PPBC.PowerProfileStatus",         "POWER_PROFILE_BASED_CONTROL")]
        [TestCase("OMBC.Instruction",                  "OMBC.SystemDescription",          "OPERATION_MODE_BASED_CONTROL")]
        [TestCase("FRBC.Instruction",                  "FRBC.SystemDescription",          "FILL_RATE_BASED_CONTROL")]
        [TestCase("DDBC.Instruction",                  "DDBC.SystemDescription",          "DEMAND_DRIVEN_BASED_CONTROL")]
        public void ControlTypeActivated_AllowsItsOwnMessages(String CEMMessage, String RMMessage, String ControlTypeText)
        {

            var active = ControlType.Parse(ControlTypeText);

            Assert.That(Check(CEMMessage,                CEM, S2SessionState.ControlTypeActivated, active), Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check(RMMessage,                 RM,  S2SessionState.ControlTypeActivated, active), Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("InstructionStatusUpdate", RM,  S2SessionState.ControlTypeActivated, active), Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("SelectControlType",       CEM, S2SessionState.ControlTypeActivated, active), Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("ResourceManagerDetails",  RM,  S2SessionState.ControlTypeActivated, active), Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("PowerMeasurement",        RM,  S2SessionState.ControlTypeActivated, active), Is.EqualTo(S2RuleVerdict.Allowed));

            // Messages of another control type are out of state.
            var other = active == ControlType.FillRateBasedControl ? "OMBC.Instruction" : "FRBC.Instruction";
            Assert.That(Check(other, CEM, S2SessionState.ControlTypeActivated, active), Is.EqualTo(S2RuleVerdict.OutOfState));

        }

        // Documented exceptions to the spec table.
        [Test]
        [S2C("Rules.PresentDemandStatus")]
        public void DDBC_PresentDemandStatus_IsAllowed_AlthoughTheTableOmitsIt()
            => Assert.That(Check("DDBC.PresentDemandStatus", RM, S2SessionState.ControlTypeActivated, ControlType.DemandDrivenBasedControl), Is.EqualTo(S2RuleVerdict.Allowed));

        [Test]
        [S2C("Rules.RevokeObjectBothDirections")]
        public void RevokeObject_IsAllowed_InBothDirections()
        {
            Assert.That(Check("RevokeObject", CEM, S2SessionState.ControlTypeActivated, ControlType.PowerEnvelopeBasedControl), Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("RevokeObject", RM,  S2SessionState.ControlTypeActivated, ControlType.PowerEnvelopeBasedControl), Is.EqualTo(S2RuleVerdict.Allowed));
        }

        [Test]
        [S2C("Communication.NoHandshakeInS2Connect")]
        public void Handshake_IsForbidden_InS2ConnectMode_AndAllowedInPlainMode()
        {
            Assert.That(Check("Handshake",         RM,  S2SessionState.AwaitingHandshake, null, S2SessionMode.S2Connect), Is.EqualTo(S2RuleVerdict.ForbiddenInMode));
            Assert.That(Check("HandshakeResponse", CEM, S2SessionState.AwaitingHandshake, null, S2SessionMode.S2Connect), Is.EqualTo(S2RuleVerdict.ForbiddenInMode));
            Assert.That(Check("Handshake",         RM,  S2SessionState.AwaitingHandshake, null, S2SessionMode.Plain),     Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("Handshake",         CEM, S2SessionState.AwaitingHandshake, null, S2SessionMode.Plain),     Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("HandshakeResponse", CEM, S2SessionState.AwaitingHandshake, null, S2SessionMode.Plain),     Is.EqualTo(S2RuleVerdict.Allowed));
            Assert.That(Check("HandshakeResponse", RM,  S2SessionState.AwaitingHandshake, null, S2SessionMode.Plain),     Is.EqualTo(S2RuleVerdict.WrongDirection));
            Assert.That(Check("ResourceManagerDetails", RM, S2SessionState.AwaitingHandshake, null, S2SessionMode.Plain), Is.EqualTo(S2RuleVerdict.OutOfState));
        }

        [Test]
        public void EveryMessageSchema_IsAllowedSomewhere()
        {

            foreach (var name in EmbeddedSchemas.MessageSchemaNames)
            {

                var messageType = name[(name.IndexOf(".messages.", StringComparison.Ordinal) + ".messages.".Length)..^".schema.json".Length];
                var allowed     = false;

                foreach (var sender in new[] { CEM, RM })
                foreach (var mode in new[] { S2SessionMode.S2Connect, S2SessionMode.Plain })
                foreach (var state in new[] { S2SessionState.AwaitingHandshake, S2SessionState.WebSocketConnected, S2SessionState.ControlTypeActivated })
                foreach (var active in new ControlType?[] { null, ControlType.PowerEnvelopeBasedControl, ControlType.PowerProfileBasedControl, ControlType.OperationModeBasedControl, ControlType.FillRateBasedControl, ControlType.DemandDrivenBasedControl })
                    allowed |= S2MessageRules.Check(messageType, sender, mode, state, active) == S2RuleVerdict.Allowed;

                Assert.That(allowed, Is.True, messageType);

            }

        }

        [Test]
        public void ControlTypePrefixes_RoundTrip()
        {
            Assert.That(S2MessageRules.ControlTypeOf("FRBC.Instruction"),                 Is.EqualTo(ControlType.FillRateBasedControl));
            Assert.That(S2MessageRules.ControlTypeOf("PowerMeasurement"),                 Is.Null);
            Assert.That(S2MessageRules.ControlTypePrefix(ControlType.DemandDrivenBasedControl), Is.EqualTo("DDBC."));
            Assert.That(S2MessageRules.ControlTypePrefix(ControlType.NoSelection),        Is.Null);
            Assert.That(S2MessageRules.IsInstruction("PPBC.EndInterruptionInstruction"),  Is.True);
            Assert.That(S2MessageRules.IsInstruction("FRBC.ActuatorStatus"),              Is.False);
        }

    }

}

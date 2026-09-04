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

namespace cloud.charging.open.protocols.S2.Tests.Messages.FRBC
{

    /// <summary>
    /// Tests of the core FRBC messages: FRBC.SystemDescription, FRBC.ActuatorStatus,
    /// FRBC.StorageStatus and FRBC.Instruction. The fixtures follow the EV charger example
    /// of the S2 documentation (actuator1 with the operation modes om1 "Off" and om2 "Charging",
    /// the transitions transition1/transition2 and the battery storage with a state of charge of 0..100 %).
    /// </summary>
    [TestFixture]
    public sealed class FRBCCoreMessageTests
    {

        #region Fixtures

        private static readonly DateTimeOffset    timestamp         = new (2019, 8, 24, 14, 15, 22, TimeSpan.Zero);
        private static readonly Actuator_Id       actuatorId        = Actuator_Id.     Parse("actuator1");
        private static readonly OperationMode_Id  om1               = OperationMode_Id.Parse("om1");
        private static readonly OperationMode_Id  om2               = OperationMode_Id.Parse("om2");

        private static readonly Message_Id        uuidMessageId     = Message_Id.      Parse("0190a3d2-6b0c-7b3e-9c2a-4d9f3b1e8a01");
        private static readonly Actuator_Id       uuidActuatorId    = Actuator_Id.     Parse("0190a3d2-6b0c-7b3e-9c2a-4d9f3b1e8a02");
        private static readonly OperationMode_Id  uuidOm1           = OperationMode_Id.Parse("0190a3d2-6b0c-7b3e-9c2a-4d9f3b1e8a03");
        private static readonly OperationMode_Id  uuidOm2           = OperationMode_Id.Parse("0190a3d2-6b0c-7b3e-9c2a-4d9f3b1e8a04");
        private static readonly Instruction_Id    uuidInstructionId = Instruction_Id.  Parse("0190a3d2-6b0c-7b3e-9c2a-4d9f3b1e8a05");

        /// <summary>
        /// The operation mode "Off" of the EV charger: no fill rate, no power.
        /// </summary>
        private static FRBC_OperationMode Off()

            => new (
                   om1,
                   [
                       new FRBC_OperationModeElement(
                           new NumberRange(0, 100),
                           new NumberRange(0, 0),
                           [ new PowerRange(0, 0, CommodityQuantity.ElectricPower3PhaseSymmetric) ]
                       )
                   ],
                   false,
                   "Off"
               );

        /// <summary>
        /// The operation mode "Charging" of the EV charger: 1400..11000 W.
        /// </summary>
        private static FRBC_OperationMode Charging()

            => new (
                   om2,
                   [
                       new FRBC_OperationModeElement(
                           new NumberRange(0, 100),
                           new NumberRange(0.00065, 0.0051),
                           [ new PowerRange(1400, 11000, CommodityQuantity.ElectricPower3PhaseSymmetric) ]
                       )
                   ],
                   false,
                   "Charging"
               );

        private static Transition TransitionOf(String Id, OperationMode_Id From, OperationMode_Id To)

            => new (
                   Transition_Id.Parse(Id),
                   From,
                   To,
                   [],
                   [],
                   false,
                   null,
                   Duration.FromMilliseconds(3000)
               );

        /// <summary>
        /// The EV charger actuator of the S2 documentation.
        /// </summary>
        private static FRBC_ActuatorDescription EVCharger(String Id = "actuator1")

            => new (
                   Actuator_Id.Parse(Id),
                   [ Commodity.Electricity ],
                   [ Off(), Charging() ],
                   [ TransitionOf("transition1", om1, om2), TransitionOf("transition2", om2, om1) ],
                   [],
                   "EV charger"
               );

        /// <summary>
        /// The EV battery storage of the S2 documentation.
        /// </summary>
        private static FRBC_StorageDescription EVBattery()

            => new (
                   false,
                   true,
                   false,
                   new NumberRange(0, 100),
                   "Battery SoC",
                   "EV Battery SoC"
               );

        /// <summary>
        /// The complete FRBC.SystemDescription of the S2 documentation.
        /// </summary>
        private static FRBC_SystemDescription EVSystem()

            => new (
                   timestamp,
                   [ EVCharger() ],
                   EVBattery(),
                   Message_Id.Parse("0c84b415-4e5e-429c-b5b6-116a5de6bfbf")
               );


        #endregion


        #region FRBC_SystemDescription

        [Test]
        public void SystemDescription_MatchesTheDocumentationExample()
        {

            var json = JObject.Parse("""
                {
                  "message_type": "FRBC.SystemDescription",
                  "message_id": "0c84b415-4e5e-429c-b5b6-116a5de6bfbf",
                  "valid_from": "2019-08-24T14:15:22Z",
                  "actuators": [
                    {
                      "id": "actuator1",
                      "diagnostic_label": "EV charger",
                      "supported_commodities": [
                        "ELECTRICITY"
                      ],
                      "operation_modes": [
                        {
                          "id": "om1",
                          "diagnostic_label": "Off",
                          "elements": [
                            {
                              "fill_level_range": {
                                "start_of_range": 0,
                                "end_of_range": 100
                              },
                              "fill_rate": {
                                "start_of_range": 0,
                                "end_of_range": 0
                              },
                              "power_ranges": [
                                {
                                  "start_of_range": 0,
                                  "end_of_range": 0,
                                  "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC"
                                }
                              ]
                            }
                          ],
                          "abnormal_condition_only": false
                        },
                        {
                          "id": "om2",
                          "diagnostic_label": "Charging",
                          "elements": [
                            {
                              "fill_level_range": {
                                "start_of_range": 0,
                                "end_of_range": 100
                              },
                              "fill_rate": {
                                "start_of_range": 0.00065,
                                "end_of_range": 0.0051
                              },
                              "power_ranges": [
                                {
                                  "start_of_range": 1400,
                                  "end_of_range": 11000,
                                  "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC"
                                }
                              ]
                            }
                          ],
                          "abnormal_condition_only": false
                        }
                      ],
                      "transitions": [
                        {
                          "id": "transition1",
                          "from": "om1",
                          "to": "om2",
                          "start_timers": [],
                          "blocking_timers": [],
                          "transition_duration": 3000,
                          "abnormal_condition_only": false
                        },
                        {
                          "id": "transition2",
                          "from": "om2",
                          "to": "om1",
                          "start_timers": [],
                          "blocking_timers": [],
                          "transition_duration": 3000,
                          "abnormal_condition_only": false
                        }
                      ],
                      "timers": []
                    }
                  ],
                  "storage": {
                    "diagnostic_label": "Battery SoC",
                    "fill_level_label": "EV Battery SoC",
                    "provides_leakage_behaviour": false,
                    "provides_fill_level_target_profile": true,
                    "provides_usage_forecast": false,
                    "fill_level_range": {
                      "start_of_range": 0,
                      "end_of_range": 100
                    }
                  }
                }
                """);

            S2SchemaValidator.AssertValidMessage(json, "FRBC.SystemDescription");

            Assert.That(FRBC_SystemDescription.TryParse(json, out var systemDescription, out var error), Is.True, error);
            Assert.That(systemDescription!.MessageId.ToString(),                    Is.EqualTo("0c84b415-4e5e-429c-b5b6-116a5de6bfbf"));
            Assert.That(systemDescription.ValidFrom,                                Is.EqualTo(timestamp));
            Assert.That(systemDescription.Actuators.Count,                          Is.EqualTo(1));
            Assert.That(systemDescription.Actuators[0].Id,                          Is.EqualTo(actuatorId));
            Assert.That(systemDescription.Actuators[0].DiagnosticLabel,             Is.EqualTo("EV charger"));
            Assert.That(systemDescription.Actuators[0].OperationModes.Count,        Is.EqualTo(2));
            Assert.That(systemDescription.Actuators[0].OperationModes[1].Id,        Is.EqualTo(om2));
            Assert.That(systemDescription.Actuators[0].Transitions.Count,           Is.EqualTo(2));
            Assert.That(systemDescription.Actuators[0].Timers,                      Is.Empty);
            Assert.That(systemDescription.Storage.FillLevelLabel,                   Is.EqualTo("EV Battery SoC"));
            Assert.That(systemDescription.Storage.ProvidesFillLevelTargetProfile,   Is.True);
            Assert.That(systemDescription.Storage.FillLevelRange,                   Is.EqualTo(new NumberRange(0, 100)));

            // The parsed message equals the hand-built fixture...
            Assert.That(systemDescription, Is.EqualTo(EVSystem()));

            // ...and re-serialises to the documentation example.
            var reserialised = systemDescription.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "FRBC.SystemDescription");
            JSONAssert.AssertDeepEquals(json, reserialised);
            JSONAssert.AssertDeepEquals(json, EVSystem().ToJSON());

        }

        [Test]
        public void SystemDescription_RoundTrips_AndIsSchemaValid()
        {

            // All properties of FRBC.SystemDescription are mandatory, so this is also the mandatory-only instance.
            var systemDescription = EVSystem();
            var json              = systemDescription.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "FRBC.SystemDescription");

            Assert.That(json.Properties().Select(p => p.Name),  Is.EqualTo(new[] { "message_type", "message_id", "valid_from", "actuators", "storage" }));
            Assert.That(json["message_type"]?.Value<String>(),  Is.EqualTo("FRBC.SystemDescription"));
            Assert.That(json["valid_from"]?.  Value<String>(),  Is.EqualTo("2019-08-24T14:15:22Z"));

            Assert.That(FRBC_SystemDescription.TryParse(json, out var parsed, out var error, new S2ParserOptions { RejectAdditionalProperties = true }), Is.True, error);
            Assert.That(parsed,                        Is.EqualTo(systemDescription));
            Assert.That(parsed!.GetHashCode(),         Is.EqualTo(systemDescription.GetHashCode()));
            Assert.That(parsed.Clone(),                Is.EqualTo(systemDescription));
            Assert.That(parsed == systemDescription,   Is.True);
            Assert.That(parsed != systemDescription,   Is.False);
            Assert.That(parsed.MessageType,            Is.EqualTo(FRBC_SystemDescription.MessageTypeName));
            Assert.That(parsed.ToString(),             Does.EndWith($"[{systemDescription.MessageId}]"));

            // IRevokable: revoked by its message identification.
            IRevokable revokable = parsed;
            Assert.That(revokable.RevokableObjectType,  Is.EqualTo(RevokableObject.FRBC_SystemDescription));
            Assert.That(revokable.RevokableObjectId,    Is.EqualTo(S2Object_Id.From(systemDescription.MessageId)));

            // A different message identification makes a different message.
            var other = new FRBC_SystemDescription(timestamp, [ EVCharger() ], EVBattery());
            Assert.That(other,            Is.Not.EqualTo(systemDescription));
            Assert.That(other.MessageId,  Is.Not.EqualTo(systemDescription.MessageId));

        }

        [Test]
        public void SystemDescription_MissingMandatoryProperty_Fails()
        {

            foreach (var property in new[] { "message_id", "valid_from", "actuators", "storage" })
            {

                var json = EVSystem().ToJSON();
                json.Remove(property);

                Assert.That(FRBC_SystemDescription.TryParse(json, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

            var wrongType = EVSystem().ToJSON();
            wrongType["message_type"] = "FRBC.ActuatorStatus";
            Assert.That(FRBC_SystemDescription.TryParse(wrongType, out _, out var typeError), Is.False);
            Assert.That(typeError, Does.Contain("Unexpected message type"));

        }

        [Test]
        public void SystemDescription_Strict_RejectsAdditionalProperties()
        {

            var json = EVSystem().ToJSON();
            json.Add("foo", 1);

            Assert.That(FRBC_SystemDescription.TryParse(json, out _, out _,         S2ParserOptions.Default),                                    Is.True);
            Assert.That(FRBC_SystemDescription.TryParse(json, out _, out var error, new S2ParserOptions { RejectAdditionalProperties = true }),  Is.False);
            Assert.That(error, Does.Contain("foo"));

            // The strict preset additionally requires UUID identifiers, which the documentation example does not use.
            Assert.That(FRBC_SystemDescription.TryParse(EVSystem().ToJSON(), out _, out error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("UUID"));

        }

        [Test]
        [S2C("Rules.FRBC.SystemDescription.UniqueActuatorIds")]
        public void SystemDescription_RejectsDuplicateActuatorIds()
        {

            Assert.That(() => new FRBC_SystemDescription(timestamp, [ EVCharger(), EVCharger() ], EVBattery()),
                        Throws.ArgumentException.With.Message.Contains("unique"));

            // Two different actuators are fine.
            var two = new FRBC_SystemDescription(timestamp, [ EVCharger("actuator1"), EVCharger("actuator2") ], EVBattery());
            Assert.That(two.Actuators.Count, Is.EqualTo(2));
            S2SchemaValidator.AssertValidMessage(two.ToJSON(), "FRBC.SystemDescription");

            var json = two.ToJSON();
            ((JArray) json["actuators"]!)[1]!["id"] = "actuator1";

            Assert.That(FRBC_SystemDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("unique"));

        }

        [Test]
        [S2C("Rules.FRBC.SystemDescription.Actuators")]
        public void SystemDescription_RejectsEmptyOrTooManyActuators()
        {

            Assert.That(() => new FRBC_SystemDescription(timestamp, [], EVBattery()),
                        Throws.ArgumentException.With.Message.Contains("at least one"));

            var eleven = Enumerable.Range(1, 11).Select(i => EVCharger($"actuator{i}")).ToList();
            Assert.That(() => new FRBC_SystemDescription(timestamp, eleven, EVBattery()),
                        Throws.ArgumentException.With.Message.Contains("10"));

            var json = EVSystem().ToJSON();
            json["actuators"] = new JArray();

            Assert.That(FRBC_SystemDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("actuators"));
            Assert.That(error, Does.Contain("at least 1"));

        }

        #endregion

        #region FRBC_ActuatorStatus

        [Test]
        public void ActuatorStatus_MatchesTheDocumentationExample()
        {

            var json = JObject.Parse("""
                {
                  "message_type": "FRBC.ActuatorStatus",
                  "message_id": "f0e229b3-ebbf-4ba2-829c-4e2f854a1d1d",
                  "actuator_id": "actuator1",
                  "active_operation_mode_id": "string",
                  "operation_mode_factor": 0,
                  "previous_operation_mode_id": "string",
                  "transition_timestamp": "2019-08-24T14:15:22Z"
                }
                """);

            S2SchemaValidator.AssertValidMessage(json, "FRBC.ActuatorStatus");

            Assert.That(FRBC_ActuatorStatus.TryParse(json, out var actuatorStatus, out var error), Is.True, error);
            Assert.That(actuatorStatus!.MessageId.ToString(),               Is.EqualTo("f0e229b3-ebbf-4ba2-829c-4e2f854a1d1d"));
            Assert.That(actuatorStatus.ActuatorId,                          Is.EqualTo(actuatorId));
            Assert.That(actuatorStatus.ActiveOperationModeId.ToString(),    Is.EqualTo("string"));
            Assert.That(actuatorStatus.OperationModeFactor,                 Is.EqualTo(0));
            Assert.That(actuatorStatus.PreviousOperationModeId?.ToString(), Is.EqualTo("string"));
            Assert.That(actuatorStatus.TransitionTimestamp,                 Is.EqualTo(timestamp));

            var reserialised = actuatorStatus.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "FRBC.ActuatorStatus");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void ActuatorStatus_AllProperties_RoundTrips_AndIsSchemaValid()
        {

            var actuatorStatus = new FRBC_ActuatorStatus(
                                     uuidActuatorId,
                                     uuidOm2,
                                     0.75,
                                     uuidOm1,
                                     timestamp,
                                     uuidMessageId
                                 );

            var json = actuatorStatus.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "FRBC.ActuatorStatus");

            Assert.That(json.Properties().Select(p => p.Name),           Is.EqualTo(new[] { "message_type", "message_id", "actuator_id", "active_operation_mode_id", "operation_mode_factor", "previous_operation_mode_id", "transition_timestamp" }));
            Assert.That(json["operation_mode_factor"]?.Value<Double>(),  Is.EqualTo(0.75));
            Assert.That(json["transition_timestamp"]?. Value<String>(),  Is.EqualTo("2019-08-24T14:15:22Z"));

            Assert.That(FRBC_ActuatorStatus.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                          Is.EqualTo(actuatorStatus));
            Assert.That(parsed!.GetHashCode(),           Is.EqualTo(actuatorStatus.GetHashCode()));
            Assert.That(parsed.Clone(),                  Is.EqualTo(actuatorStatus));
            Assert.That(parsed == actuatorStatus,        Is.True);
            Assert.That(parsed.MessageType,              Is.EqualTo("FRBC.ActuatorStatus"));
            Assert.That(parsed.PreviousOperationModeId,  Is.EqualTo(uuidOm1));
            Assert.That(parsed.TransitionTimestamp,      Is.EqualTo(timestamp));
            Assert.That(parsed.ToString(),               Does.EndWith($"[{uuidMessageId}]"));

        }

        [Test]
        public void ActuatorStatus_MandatoryOnly_OmitsOptionals_AndIsSchemaValid()
        {

            var actuatorStatus = new FRBC_ActuatorStatus(uuidActuatorId, uuidOm1, 1);
            var json           = actuatorStatus.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "FRBC.ActuatorStatus");

            Assert.That(json.ContainsKey("previous_operation_mode_id"),  Is.False);
            Assert.That(json.ContainsKey("transition_timestamp"),        Is.False);

            Assert.That(FRBC_ActuatorStatus.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                          Is.EqualTo(actuatorStatus));
            Assert.That(parsed!.PreviousOperationModeId, Is.Null);
            Assert.That(parsed.TransitionTimestamp,      Is.Null);
            Assert.That(parsed.Clone(),                  Is.EqualTo(actuatorStatus));

        }

        [Test]
        public void ActuatorStatus_MissingMandatoryProperty_Fails()
        {

            foreach (var property in new[] { "message_id", "actuator_id", "active_operation_mode_id", "operation_mode_factor" })
            {

                var json = new FRBC_ActuatorStatus(uuidActuatorId, uuidOm2, 0.5, uuidOm1, timestamp, uuidMessageId).ToJSON();
                json.Remove(property);

                Assert.That(FRBC_ActuatorStatus.TryParse(json, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

        }

        [Test]
        public void ActuatorStatus_Strict_RejectsAdditionalProperties()
        {

            var json = new FRBC_ActuatorStatus(uuidActuatorId, uuidOm2, 0.5, uuidOm1, timestamp, uuidMessageId).ToJSON();
            json.Add("foo", 1);

            Assert.That(FRBC_ActuatorStatus.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_ActuatorStatus.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Rules.OperationModeFactor.Range")]
        public void ActuatorStatus_RejectsOperationModeFactorOutOfRange()
        {

            Assert.That(() => new FRBC_ActuatorStatus(uuidActuatorId, uuidOm1, -0.1),       Throws.ArgumentException);
            Assert.That(() => new FRBC_ActuatorStatus(uuidActuatorId, uuidOm1,  1.1),       Throws.ArgumentException);
            Assert.That(() => new FRBC_ActuatorStatus(uuidActuatorId, uuidOm1,  Double.NaN), Throws.ArgumentException);

            // The boundaries are included.
            Assert.That(new FRBC_ActuatorStatus(uuidActuatorId, uuidOm1, 0).OperationModeFactor, Is.EqualTo(0));
            Assert.That(new FRBC_ActuatorStatus(uuidActuatorId, uuidOm1, 1).OperationModeFactor, Is.EqualTo(1));

            var json = new FRBC_ActuatorStatus(uuidActuatorId, uuidOm1, 0.5, null, null, uuidMessageId).ToJSON();
            json["operation_mode_factor"] = 1.5;

            Assert.That(FRBC_ActuatorStatus.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("operation mode factor"));

        }

        #endregion

        #region FRBC_StorageStatus

        [Test]
        public void StorageStatus_MatchesTheDocumentationExample()
        {

            // The documentation example uses the placeholder "string" as message_id, which the
            // schema pattern accepts (the strict preset would require a UUID).
            var json = JObject.Parse("""
                {
                  "message_type": "FRBC.StorageStatus",
                  "message_id": "string",
                  "present_fill_level": 0
                }
                """);

            S2SchemaValidator.AssertValidMessage(json, "FRBC.StorageStatus");

            Assert.That(FRBC_StorageStatus.TryParse(json, out var storageStatus, out var error), Is.True, error);
            Assert.That(storageStatus!.MessageId.ToString(),  Is.EqualTo("string"));
            Assert.That(storageStatus.PresentFillLevel,       Is.EqualTo(0));

            var reserialised = storageStatus.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "FRBC.StorageStatus");
            JSONAssert.AssertDeepEquals(json, reserialised);

            Assert.That(FRBC_StorageStatus.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("UUID"));

        }

        [Test]
        public void StorageStatus_RoundTrips_AndIsSchemaValid()
        {

            // All properties of FRBC.StorageStatus are mandatory, so this is also the mandatory-only instance.
            var storageStatus = new FRBC_StorageStatus(42.5, uuidMessageId);
            var json          = storageStatus.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "FRBC.StorageStatus");

            Assert.That(json.Properties().Select(p => p.Name),        Is.EqualTo(new[] { "message_type", "message_id", "present_fill_level" }));
            Assert.That(json["present_fill_level"]?.Value<Double>(),  Is.EqualTo(42.5));

            Assert.That(FRBC_StorageStatus.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                     Is.EqualTo(storageStatus));
            Assert.That(parsed!.GetHashCode(),      Is.EqualTo(storageStatus.GetHashCode()));
            Assert.That(parsed.Clone(),             Is.EqualTo(storageStatus));
            Assert.That(parsed == storageStatus,    Is.True);
            Assert.That(parsed.MessageType,         Is.EqualTo("FRBC.StorageStatus"));
            Assert.That(parsed.ToString(),          Does.EndWith($"[{uuidMessageId}]"));

            Assert.That(new FRBC_StorageStatus(42.5, uuidMessageId) == new FRBC_StorageStatus(43.0, uuidMessageId), Is.False);

        }

        [Test]
        public void StorageStatus_MissingMandatoryProperty_Fails()
        {

            foreach (var property in new[] { "message_id", "present_fill_level" })
            {

                var json = new FRBC_StorageStatus(42.5, uuidMessageId).ToJSON();
                json.Remove(property);

                Assert.That(FRBC_StorageStatus.TryParse(json, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

            var notANumber = new FRBC_StorageStatus(42.5, uuidMessageId).ToJSON();
            notANumber["present_fill_level"] = "full";
            Assert.That(FRBC_StorageStatus.TryParse(notANumber, out _, out var numberError), Is.False);
            Assert.That(numberError, Does.Contain("present_fill_level"));

        }

        [Test]
        public void StorageStatus_Strict_RejectsAdditionalProperties()
        {

            var json = new FRBC_StorageStatus(42.5, uuidMessageId).ToJSON();
            json.Add("foo", 1);

            Assert.That(FRBC_StorageStatus.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_StorageStatus.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        #endregion

        #region FRBC_Instruction

        [Test]
        public void Instruction_MatchesTheDocumentationExample()
        {

            var json = JObject.Parse("""
                {
                  "message_type": "FRBC.Instruction",
                  "message_id": "5bc49243-edee-4884-99b3-65a62766949c",
                  "id": "instruction1",
                  "actuator_id": "actuator1",
                  "operation_mode": "om1",
                  "operation_mode_factor": 1,
                  "execution_time": "2019-08-24T14:15:22Z",
                  "abnormal_condition": false
                }
                """);

            S2SchemaValidator.AssertValidMessage(json, "FRBC.Instruction");

            Assert.That(FRBC_Instruction.TryParse(json, out var instruction, out var error), Is.True, error);
            Assert.That(instruction!.MessageId.ToString(),  Is.EqualTo("5bc49243-edee-4884-99b3-65a62766949c"));
            Assert.That(instruction.Id.ToString(),          Is.EqualTo("instruction1"));
            Assert.That(instruction.ActuatorId,             Is.EqualTo(actuatorId));
            Assert.That(instruction.OperationMode,          Is.EqualTo(om1));
            Assert.That(instruction.OperationModeFactor,    Is.EqualTo(1));
            Assert.That(instruction.ExecutionTime,          Is.EqualTo(timestamp));
            Assert.That(instruction.AbnormalCondition,      Is.False);

            var reserialised = instruction.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "FRBC.Instruction");
            JSONAssert.AssertDeepEquals(json, reserialised);

            // FRBC uses the wire name "operation_mode", unlike OMBC/DDBC ("operation_mode_id").
            Assert.That(reserialised.ContainsKey("operation_mode"),     Is.True);
            Assert.That(reserialised.ContainsKey("operation_mode_id"),  Is.False);

            var wrongKey = (JObject) json.DeepClone();
            wrongKey.Remove("operation_mode");
            wrongKey.Add("operation_mode_id", "om1");
            Assert.That(FRBC_Instruction.TryParse(wrongKey, out _, out error), Is.False);
            Assert.That(error, Does.Contain("operation_mode"));

        }

        [Test]
        public void Instruction_RoundTrips_AndIsSchemaValid()
        {

            // All properties of FRBC.Instruction are mandatory, so this is also the mandatory-only instance.
            var instruction = new FRBC_Instruction(
                                  uuidInstructionId,
                                  uuidActuatorId,
                                  uuidOm2,
                                  0.5,
                                  timestamp,
                                  true,
                                  uuidMessageId
                              );

            var json = instruction.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "FRBC.Instruction");

            Assert.That(json.Properties().Select(p => p.Name),           Is.EqualTo(new[] { "message_type", "message_id", "id", "actuator_id", "operation_mode", "operation_mode_factor", "execution_time", "abnormal_condition" }));
            Assert.That(json["operation_mode"]?.       Value<String>(),  Is.EqualTo(uuidOm2.ToString()));
            Assert.That(json["operation_mode_factor"]?.Value<Double>(),  Is.EqualTo(0.5));
            Assert.That(json["execution_time"]?.       Value<String>(),  Is.EqualTo("2019-08-24T14:15:22Z"));
            Assert.That(json["abnormal_condition"]?.   Value<Boolean>(), Is.True);

            Assert.That(FRBC_Instruction.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                    Is.EqualTo(instruction));
            Assert.That(parsed!.GetHashCode(),     Is.EqualTo(instruction.GetHashCode()));
            Assert.That(parsed.Clone(),            Is.EqualTo(instruction));
            Assert.That(parsed == instruction,     Is.True);
            Assert.That(parsed.MessageType,        Is.EqualTo("FRBC.Instruction"));
            Assert.That(parsed.ToString(),         Does.EndWith($"[{uuidMessageId}]"));

            // IInstruction / IRevokable: revoked by its own identification.
            IInstruction asInstruction = parsed;
            Assert.That(asInstruction.Id,                   Is.EqualTo(uuidInstructionId));
            Assert.That(asInstruction.ExecutionTime,        Is.EqualTo(timestamp));
            Assert.That(asInstruction.AbnormalCondition,    Is.True);
            Assert.That(asInstruction.RevokableObjectType,  Is.EqualTo(RevokableObject.FRBC_Instruction));
            Assert.That(asInstruction.RevokableObjectId,    Is.EqualTo(S2Object_Id.From(uuidInstructionId)));

            // A timestamp with an offset keeps its offset on the wire.
            var local = new FRBC_Instruction(uuidInstructionId, uuidActuatorId, uuidOm2, 0.5, new DateTimeOffset(2019, 8, 24, 16, 15, 22, TimeSpan.FromHours(2)), false);
            Assert.That(local.ToJSON()["execution_time"]?.Value<String>(), Is.EqualTo("2019-08-24T16:15:22+02:00"));
            Assert.That(local.ExecutionTime,                               Is.EqualTo(timestamp));

        }

        [Test]
        public void Instruction_MissingMandatoryProperty_Fails()
        {

            foreach (var property in new[] { "message_id", "id", "actuator_id", "operation_mode", "operation_mode_factor", "execution_time", "abnormal_condition" })
            {

                var json = new FRBC_Instruction(uuidInstructionId, uuidActuatorId, uuidOm2, 0.5, timestamp, false, uuidMessageId).ToJSON();
                json.Remove(property);

                Assert.That(FRBC_Instruction.TryParse(json, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

        }

        [Test]
        public void Instruction_Strict_RejectsAdditionalProperties()
        {

            var json = new FRBC_Instruction(uuidInstructionId, uuidActuatorId, uuidOm2, 0.5, timestamp, false, uuidMessageId).ToJSON();
            json.Add("foo", 1);

            Assert.That(FRBC_Instruction.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_Instruction.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Rules.OperationModeFactor.Range")]
        public void Instruction_RejectsOperationModeFactorOutOfRange()
        {

            Assert.That(() => new FRBC_Instruction(uuidInstructionId, uuidActuatorId, uuidOm2, -0.01,      timestamp, false), Throws.ArgumentException);
            Assert.That(() => new FRBC_Instruction(uuidInstructionId, uuidActuatorId, uuidOm2,  1.01,      timestamp, false), Throws.ArgumentException);
            Assert.That(() => new FRBC_Instruction(uuidInstructionId, uuidActuatorId, uuidOm2, Double.NaN, timestamp, false), Throws.ArgumentException);

            // The boundaries are included.
            Assert.That(new FRBC_Instruction(uuidInstructionId, uuidActuatorId, uuidOm2, 0, timestamp, false).OperationModeFactor, Is.EqualTo(0));
            Assert.That(new FRBC_Instruction(uuidInstructionId, uuidActuatorId, uuidOm2, 1, timestamp, false).OperationModeFactor, Is.EqualTo(1));

            var json = new FRBC_Instruction(uuidInstructionId, uuidActuatorId, uuidOm2, 0.5, timestamp, false, uuidMessageId).ToJSON();
            json["operation_mode_factor"] = -1;

            Assert.That(FRBC_Instruction.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("operation mode factor"));

        }

        #endregion

        #region S2MessageParser

        [Test]
        public void MessageParser_DispatchesFRBCInstruction()
        {

            var instruction = new FRBC_Instruction(uuidInstructionId, uuidActuatorId, uuidOm2, 0.5, timestamp, false, uuidMessageId);
            var text        = instruction.ToJSON().ToString();

            Assert.That(S2MessageParser.KnownMessageTypes, Does.Contain(FRBC_Instruction.MessageTypeName));

            Assert.That(S2MessageParser.TryParse(text, null, out var message, out var error), Is.True, error?.DiagnosticLabel);
            Assert.That(message,                                 Is.TypeOf<FRBC_Instruction>());
            Assert.That(message,                                 Is.EqualTo(instruction));
            Assert.That(((FRBC_Instruction) message!).OperationMode, Is.EqualTo(uuidOm2));
            Assert.That(message,                                 Is.InstanceOf<IInstruction>());

            // A semantic violation is reported as INVALID_MESSAGE for this message identification.
            var json = instruction.ToJSON();
            json["operation_mode_factor"] = 2;

            Assert.That(S2MessageParser.TryParse(json, null, out _, out error), Is.False);
            Assert.That(error!.Status,            Is.EqualTo(ReceptionStatusValue.InvalidMessage));
            Assert.That(error.SubjectMessageId,   Is.EqualTo(uuidMessageId));
            Assert.That(error.DiagnosticLabel,    Does.Contain("operation mode factor"));

        }

        #endregion

    }

}

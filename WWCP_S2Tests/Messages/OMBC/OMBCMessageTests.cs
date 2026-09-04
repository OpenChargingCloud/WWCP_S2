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

namespace cloud.charging.open.protocols.S2.Tests.Messages.OMBC
{

    /// <summary>
    /// Tests of the OMBC messages: OMBC.SystemDescription, OMBC.Status, OMBC.Instruction and
    /// OMBC.TimerStatus, based on a heat pump with the operation modes "Off" and "On"
    /// (800..2400 W electric, 2500..9000 W thermal) and a minimum run timer. The S2 documentation
    /// carries no OMBC message example, so the example JSON below is hand-written.
    /// </summary>
    [TestFixture]
    public sealed class OMBCMessageTests
    {

        #region Data

        private static readonly Message_Id        messageId          = Message_Id.      Parse("3c7f9a1e-2b4d-4c6e-8f0a-000000000100");
        private static readonly Instruction_Id    instructionId      = Instruction_Id.  Parse("3c7f9a1e-2b4d-4c6e-8f0a-000000000200");
        private static readonly OperationMode_Id  offId              = OperationMode_Id.Parse("3c7f9a1e-2b4d-4c6e-8f0a-000000000010");
        private static readonly OperationMode_Id  onId               = OperationMode_Id.Parse("3c7f9a1e-2b4d-4c6e-8f0a-000000000011");
        private static readonly OperationMode_Id  unknownModeId      = OperationMode_Id.Parse("3c7f9a1e-2b4d-4c6e-8f0a-0000000000ff");
        private static readonly Transition_Id     offToOnId          = Transition_Id.   Parse("3c7f9a1e-2b4d-4c6e-8f0a-000000000020");
        private static readonly Transition_Id     onToOffId          = Transition_Id.   Parse("3c7f9a1e-2b4d-4c6e-8f0a-000000000021");
        private static readonly Timer_Id          minRunTimeId       = Timer_Id.        Parse("3c7f9a1e-2b4d-4c6e-8f0a-000000000030");
        private static readonly Timer_Id          unknownTimerId     = Timer_Id.        Parse("3c7f9a1e-2b4d-4c6e-8f0a-0000000000fe");

        private static readonly DateTimeOffset    validFrom          = new (2024, 5, 1, 12, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset    executionTime      = new (2024, 5, 1, 12, 5, 0, TimeSpan.FromHours(2));

        #endregion

        #region (private static) Fixtures

        private static OMBC_OperationMode Off()

            => new (
                   offId,
                   [ new PowerRange(0, 0, CommodityQuantity.ElectricPowerL1) ],
                   false,
                   "Off"
               );

        private static OMBC_OperationMode On()

            => new (
                   onId,
                   [
                       new PowerRange( 800, 2400, CommodityQuantity.ElectricPowerL1),
                       new PowerRange(2500, 9000, CommodityQuantity.HeatThermalPower)
                   ],
                   false,
                   "On",
                   new NumberRange(0.001, 0.002)
               );

        private static Transition OffToOn(IReadOnlyList<Timer_Id>? StartTimers = null)

            => new (
                   offToOnId,
                   offId,
                   onId,
                   StartTimers ?? [],
                   [],
                   false,
                   0.5,
                   Duration.FromMilliseconds(3000)
               );

        private static Transition OnToOff(IReadOnlyList<Timer_Id>? BlockingTimers = null)

            => new (
                   onToOffId,
                   onId,
                   offId,
                   [],
                   BlockingTimers ?? [],
                   false
               );

        private static Timer MinRunTime()
            => new (minRunTimeId, Duration.FromMilliseconds(600000), "Minimum run time");

        /// <summary>
        /// The heat pump system description with all properties set (a timer, transitions using it).
        /// </summary>
        private static OMBC_SystemDescription HeatPump()

            => new (
                   validFrom,
                   [ Off(), On() ],
                   [
                       OffToOn(StartTimers:    [ minRunTimeId ]),
                       OnToOff(BlockingTimers: [ minRunTimeId ])
                   ],
                   [ MinRunTime() ],
                   messageId
               );

        #endregion


        #region OMBC_SystemDescription

        [Test]
        public void SystemDescription_HeatPump_RoundTrips_AndIsSchemaValid()
        {

            var description = HeatPump();
            var json        = description.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "OMBC.SystemDescription");

            Assert.That(json.Properties().Select(p => p.Name),  Is.EqualTo(new[] { "message_type", "message_id", "valid_from", "operation_modes", "transitions", "timers" }));
            Assert.That(json["message_type"]?.Value<String>(),  Is.EqualTo("OMBC.SystemDescription"));
            Assert.That(json["message_id"]?.  Value<String>(),  Is.EqualTo(messageId.ToString()));
            Assert.That(json["valid_from"]?.  Value<String>(),  Is.EqualTo("2024-05-01T12:00:00Z"));
            Assert.That(json["operation_modes"]?.Count(),       Is.EqualTo(2));
            Assert.That(json["transitions"]?.    Count(),       Is.EqualTo(2));
            Assert.That(json["timers"]?.         Count(),       Is.EqualTo(1));

            Assert.That(OMBC_SystemDescription.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                                   Is.EqualTo(description));
            Assert.That(parsed!.GetHashCode(),                    Is.EqualTo(description.GetHashCode()));
            Assert.That(parsed.Clone(),                           Is.EqualTo(description));
            Assert.That(parsed.MessageId,                         Is.EqualTo(messageId));
            Assert.That(parsed.ValidFrom,                         Is.EqualTo(validFrom));
            Assert.That(parsed.OperationModes[1].DiagnosticLabel, Is.EqualTo("On"));
            Assert.That(parsed.Transitions[0].StartTimers,        Is.EqualTo(new[] { minRunTimeId }));
            Assert.That(parsed.Transitions[1].BlockingTimers,     Is.EqualTo(new[] { minRunTimeId }));
            Assert.That(parsed.Timers[0].Duration,                Is.EqualTo(Duration.FromMilliseconds(600000)));
            Assert.That(parsed.ToString(),                        Does.Contain("2 operation mode(s)"));
            Assert.That(parsed.ToString(),                        Does.EndWith($"[{messageId}]"));

            Assert.That(parsed.RevokableObjectType,               Is.EqualTo(RevokableObject.OMBC_SystemDescription));
            Assert.That(parsed.RevokableObjectId,                 Is.EqualTo(S2Object_Id.From(messageId)));
            Assert.That(parsed,                                   Is.InstanceOf<IRevokable>());

        }

        [Test]
        public void SystemDescription_MatchesTheHandWrittenExample()
        {

            // Numbers carry a decimal point so that the parsed token types equal the serialised ones.
            var json = S2JSONExtensions.ParseS2JSON("""
                {
                  "message_type": "OMBC.SystemDescription",
                  "message_id": "3c7f9a1e-2b4d-4c6e-8f0a-000000000100",
                  "valid_from": "2024-05-01T12:00:00Z",
                  "operation_modes": [
                    {
                      "id": "3c7f9a1e-2b4d-4c6e-8f0a-000000000010",
                      "diagnostic_label": "Off",
                      "power_ranges": [
                        { "start_of_range": 0.0, "end_of_range": 0.0, "commodity_quantity": "ELECTRIC.POWER.L1" }
                      ],
                      "abnormal_condition_only": false
                    },
                    {
                      "id": "3c7f9a1e-2b4d-4c6e-8f0a-000000000011",
                      "diagnostic_label": "On",
                      "power_ranges": [
                        { "start_of_range": 800.0,  "end_of_range": 2400.0, "commodity_quantity": "ELECTRIC.POWER.L1" },
                        { "start_of_range": 2500.0, "end_of_range": 9000.0, "commodity_quantity": "HEAT.THERMAL_POWER" }
                      ],
                      "running_costs": { "start_of_range": 0.001, "end_of_range": 0.002 },
                      "abnormal_condition_only": false
                    }
                  ],
                  "transitions": [
                    {
                      "id": "3c7f9a1e-2b4d-4c6e-8f0a-000000000020",
                      "from": "3c7f9a1e-2b4d-4c6e-8f0a-000000000010",
                      "to": "3c7f9a1e-2b4d-4c6e-8f0a-000000000011",
                      "start_timers": [ "3c7f9a1e-2b4d-4c6e-8f0a-000000000030" ],
                      "blocking_timers": [],
                      "transition_costs": 0.5,
                      "transition_duration": 3000,
                      "abnormal_condition_only": false
                    },
                    {
                      "id": "3c7f9a1e-2b4d-4c6e-8f0a-000000000021",
                      "from": "3c7f9a1e-2b4d-4c6e-8f0a-000000000011",
                      "to": "3c7f9a1e-2b4d-4c6e-8f0a-000000000010",
                      "start_timers": [],
                      "blocking_timers": [ "3c7f9a1e-2b4d-4c6e-8f0a-000000000030" ],
                      "abnormal_condition_only": false
                    }
                  ],
                  "timers": [
                    {
                      "id": "3c7f9a1e-2b4d-4c6e-8f0a-000000000030",
                      "diagnostic_label": "Minimum run time",
                      "duration": 600000
                    }
                  ]
                }
                """);

            Assert.That(OMBC_SystemDescription.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed, Is.EqualTo(HeatPump()));

            var reserialised = parsed!.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "OMBC.SystemDescription");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void SystemDescription_MandatoryOnly_RoundTrips_AndIsSchemaValid()
        {

            var description = new OMBC_SystemDescription(validFrom, [ Off() ], [], []);
            var json        = description.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "OMBC.SystemDescription");

            Assert.That(json["transitions"]?.Count(), Is.EqualTo(0));
            Assert.That(json["timers"]?.     Count(), Is.EqualTo(0));
            Assert.That(description.MessageId.IsUUID, Is.True);

            Assert.That(OMBC_SystemDescription.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                     Is.EqualTo(description));
            Assert.That(parsed!.Transitions.Count,  Is.EqualTo(0));
            Assert.That(parsed.Timers.Count,        Is.EqualTo(0));

        }

        [Test]
        public void SystemDescription_MissingMandatoryProperty_Fails()
        {

            foreach (var property in new[] { "message_type", "message_id", "valid_from", "operation_modes", "transitions", "timers" })
            {

                var json = HeatPump().ToJSON();
                json.Remove(property);

                Assert.That(OMBC_SystemDescription.TryParse(json, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

        }

        [Test]
        public void SystemDescription_Strict_RejectsAdditionalProperties()
        {

            var json = HeatPump().ToJSON();
            json.Add("foo", 1);

            Assert.That(OMBC_SystemDescription.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(OMBC_SystemDescription.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void SystemDescription_RejectsEmptyOperationModes()
        {

            Assert.That(() => new OMBC_SystemDescription(validFrom, [], [], []),
                        Throws.ArgumentException.With.Message.Contains("at least one operation mode"));

            var json = HeatPump().ToJSON();
            json["operation_modes"] = new JArray();

            Assert.That(OMBC_SystemDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("operation_modes"));

        }

        [Test]
        [S2C("Rules.OMBC.SystemDescription.UniqueOperationModeIds")]
        public void SystemDescription_RejectsDuplicateOperationModeIds()
        {

            Assert.That(() => new OMBC_SystemDescription(validFrom, [ Off(), Off() ], [], []),
                        Throws.ArgumentException.With.Message.Contains("operation mode identifications").And.Message.Contains("unique"));

            var json = HeatPump().ToJSON();
            json["operation_modes"]![1]!["id"] = offId.ToString();

            Assert.That(OMBC_SystemDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("operation mode identifications").And.Contains("unique"));

        }

        [Test]
        [S2C("Rules.OMBC.SystemDescription.UniqueTransitionIds")]
        public void SystemDescription_RejectsDuplicateTransitionIds()
        {

            var duplicate = new Transition(offToOnId, onId, offId, [], [], false);

            Assert.That(() => new OMBC_SystemDescription(validFrom, [ Off(), On() ], [ OffToOn(), duplicate ], []),
                        Throws.ArgumentException.With.Message.Contains("transition identifications").And.Message.Contains("unique"));

            var json = HeatPump().ToJSON();
            json["transitions"]![1]!["id"] = offToOnId.ToString();

            Assert.That(OMBC_SystemDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("transition identifications").And.Contains("unique"));

        }

        [Test]
        [S2C("Rules.OMBC.SystemDescription.UniqueTimerIds")]
        public void SystemDescription_RejectsDuplicateTimerIds()
        {

            Assert.That(() => new OMBC_SystemDescription(validFrom, [ Off(), On() ], [], [ MinRunTime(), MinRunTime() ]),
                        Throws.ArgumentException.With.Message.Contains("timer identifications").And.Message.Contains("unique"));

            var json = HeatPump().ToJSON();
            (json["timers"] as JArray)!.Add(MinRunTime().ToJSON());

            Assert.That(OMBC_SystemDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("timer identifications").And.Contains("unique"));

        }

        [Test]
        [S2C("Rules.OMBC.SystemDescription.TransitionsReferenceOperationModes")]
        public void SystemDescription_RejectsTransitions_ReferencingUnknownOperationModes()
        {

            var unknownFrom = new Transition(offToOnId, unknownModeId, onId,          [], [], false);
            var unknownTo   = new Transition(onToOffId, onId,          unknownModeId, [], [], false);

            Assert.That(() => new OMBC_SystemDescription(validFrom, [ Off(), On() ], [ unknownFrom ], []),
                        Throws.ArgumentException.With.Message.Contains("unknown operation mode").And.Message.Contains("'from'"));

            Assert.That(() => new OMBC_SystemDescription(validFrom, [ Off(), On() ], [ unknownTo ], []),
                        Throws.ArgumentException.With.Message.Contains("unknown operation mode").And.Message.Contains("'to'"));

            var json = HeatPump().ToJSON();
            json["transitions"]![0]!["to"] = unknownModeId.ToString();

            Assert.That(OMBC_SystemDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("unknown operation mode"));

        }

        [Test]
        [S2C("Rules.OMBC.SystemDescription.TransitionsReferenceTimers")]
        public void SystemDescription_RejectsTransitions_ReferencingUnknownTimers()
        {

            Assert.That(() => new OMBC_SystemDescription(validFrom, [ Off(), On() ], [ OffToOn(StartTimers:    [ unknownTimerId ]) ], [ MinRunTime() ]),
                        Throws.ArgumentException.With.Message.Contains("unknown timer").And.Message.Contains("'start_timers'"));

            Assert.That(() => new OMBC_SystemDescription(validFrom, [ Off(), On() ], [ OnToOff(BlockingTimers: [ unknownTimerId ]) ], [ MinRunTime() ]),
                        Throws.ArgumentException.With.Message.Contains("unknown timer").And.Message.Contains("'blocking_timers'"));

            var json = HeatPump().ToJSON();
            json["timers"] = new JArray();

            Assert.That(OMBC_SystemDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("unknown timer"));

        }

        #endregion

        #region OMBC_Status

        [Test]
        public void Status_AllProperties_RoundTrips_AndIsSchemaValid()
        {

            var status = new OMBC_Status(onId, 0.75, offId, validFrom, messageId);
            var json   = status.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "OMBC.Status");

            Assert.That(json.Properties().Select(p => p.Name),               Is.EqualTo(new[] { "message_type", "message_id", "active_operation_mode_id", "operation_mode_factor", "previous_operation_mode_id", "transition_timestamp" }));
            Assert.That(json["message_type"]?.              Value<String>(), Is.EqualTo("OMBC.Status"));
            Assert.That(json["active_operation_mode_id"]?.  Value<String>(), Is.EqualTo(onId.ToString()));
            Assert.That(json["operation_mode_factor"]?.     Value<Double>(), Is.EqualTo(0.75));
            Assert.That(json["previous_operation_mode_id"]?.Value<String>(), Is.EqualTo(offId.ToString()));
            Assert.That(json["transition_timestamp"]?.      Value<String>(), Is.EqualTo("2024-05-01T12:00:00Z"));

            Assert.That(OMBC_Status.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                          Is.EqualTo(status));
            Assert.That(parsed!.GetHashCode(),           Is.EqualTo(status.GetHashCode()));
            Assert.That(parsed.Clone(),                  Is.EqualTo(status));
            Assert.That(parsed.ActiveOperationModeId,    Is.EqualTo(onId));
            Assert.That(parsed.OperationModeFactor,      Is.EqualTo(0.75));
            Assert.That(parsed.PreviousOperationModeId,  Is.EqualTo(offId));
            Assert.That(parsed.TransitionTimestamp,      Is.EqualTo(validFrom));
            Assert.That(parsed.ToString(),               Does.EndWith($"[{messageId}]"));

        }

        [Test]
        public void Status_MandatoryOnly_OmitsOptionals_AndIsSchemaValid()
        {

            var status = new OMBC_Status(offId, 0);
            var json   = status.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "OMBC.Status");

            Assert.That(json.ContainsKey("previous_operation_mode_id"), Is.False);
            Assert.That(json.ContainsKey("transition_timestamp"),       Is.False);

            Assert.That(OMBC_Status.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                          Is.EqualTo(status));
            Assert.That(parsed!.PreviousOperationModeId, Is.Null);
            Assert.That(parsed.TransitionTimestamp,      Is.Null);

        }

        [Test]
        public void Status_MissingMandatoryProperty_Fails()
        {

            foreach (var property in new[] { "message_type", "message_id", "active_operation_mode_id", "operation_mode_factor" })
            {

                var json = new OMBC_Status(onId, 0.5, offId, validFrom).ToJSON();
                json.Remove(property);

                Assert.That(OMBC_Status.TryParse(json, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

        }

        [Test]
        public void Status_Strict_RejectsAdditionalProperties()
        {

            var json = new OMBC_Status(onId, 0.5).ToJSON();
            json.Add("foo", 1);

            Assert.That(OMBC_Status.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(OMBC_Status.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Rules.OMBC.Status.OperationModeFactorRange")]
        public void Status_RejectsOperationModeFactor_OutsideZeroToOne()
        {

            Assert.That(() => new OMBC_Status(onId, -0.01),     Throws.ArgumentException.With.Message.Contains("operation mode factor").And.Message.Contains("[0, 1]"));
            Assert.That(() => new OMBC_Status(onId,  1.01),     Throws.ArgumentException.With.Message.Contains("operation mode factor").And.Message.Contains("[0, 1]"));
            Assert.That(() => new OMBC_Status(onId,  Double.NaN), Throws.ArgumentException.With.Message.Contains("operation mode factor"));

            Assert.That(() => new OMBC_Status(onId, 0), Throws.Nothing);
            Assert.That(() => new OMBC_Status(onId, 1), Throws.Nothing);

            var json = new OMBC_Status(onId, 0.5).ToJSON();
            json["operation_mode_factor"] = 1.5;

            Assert.That(OMBC_Status.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("operation mode factor").And.Contains("[0, 1]"));

        }

        #endregion

        #region OMBC_Instruction

        [Test]
        public void Instruction_RoundTrips_AndIsSchemaValid()
        {

            var instruction = new OMBC_Instruction(instructionId, executionTime, onId, 0.5, false, messageId);
            var json        = instruction.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "OMBC.Instruction");

            Assert.That(json.Properties().Select(p => p.Name),           Is.EqualTo(new[] { "message_type", "message_id", "id", "execution_time", "operation_mode_id", "operation_mode_factor", "abnormal_condition" }));
            Assert.That(json["message_type"]?.         Value<String>(),  Is.EqualTo("OMBC.Instruction"));
            Assert.That(json["id"]?.                   Value<String>(),  Is.EqualTo(instructionId.ToString()));
            Assert.That(json["execution_time"]?.       Value<String>(),  Is.EqualTo("2024-05-01T12:05:00+02:00"));
            Assert.That(json["operation_mode_id"]?.    Value<String>(),  Is.EqualTo(onId.ToString()));
            Assert.That(json.ContainsKey("operation_mode"),              Is.False);
            Assert.That(json["operation_mode_factor"]?.Value<Double>(),  Is.EqualTo(0.5));
            Assert.That(json["abnormal_condition"]?.   Value<Boolean>(), Is.False);

            Assert.That(OMBC_Instruction.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                       Is.EqualTo(instruction));
            Assert.That(parsed!.GetHashCode(),        Is.EqualTo(instruction.GetHashCode()));
            Assert.That(parsed.Clone(),               Is.EqualTo(instruction));
            Assert.That(parsed.Id,                    Is.EqualTo(instructionId));
            Assert.That(parsed.ExecutionTime,         Is.EqualTo(executionTime));
            Assert.That(parsed.ExecutionTime.Offset,  Is.EqualTo(TimeSpan.FromHours(2)));
            Assert.That(parsed.OperationModeId,       Is.EqualTo(onId));
            Assert.That(parsed.OperationModeFactor,   Is.EqualTo(0.5));
            Assert.That(parsed.AbnormalCondition,     Is.False);
            Assert.That(parsed.ToString(),            Does.EndWith($"[{messageId}]"));

            Assert.That(parsed,                       Is.InstanceOf<IInstruction>());
            Assert.That(parsed.RevokableObjectType,   Is.EqualTo(RevokableObject.OMBC_Instruction));
            Assert.That(parsed.RevokableObjectId,     Is.EqualTo(S2Object_Id.From(instructionId)));

        }

        [Test]
        public void Instruction_MatchesTheHandWrittenExample()
        {

            var json = S2JSONExtensions.ParseS2JSON("""
                {
                  "message_type": "OMBC.Instruction",
                  "message_id": "3c7f9a1e-2b4d-4c6e-8f0a-000000000100",
                  "id": "3c7f9a1e-2b4d-4c6e-8f0a-000000000200",
                  "execution_time": "2024-05-01T12:05:00+02:00",
                  "operation_mode_id": "3c7f9a1e-2b4d-4c6e-8f0a-000000000011",
                  "operation_mode_factor": 0.5,
                  "abnormal_condition": true
                }
                """);

            Assert.That(OMBC_Instruction.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                      Is.EqualTo(new OMBC_Instruction(instructionId, executionTime, onId, 0.5, true, messageId)));
            Assert.That(parsed!.AbnormalCondition,   Is.True);

            var reserialised = parsed.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "OMBC.Instruction");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void Instruction_MissingMandatoryProperty_Fails()
        {

            foreach (var property in new[] { "message_type", "message_id", "id", "execution_time", "operation_mode_id", "operation_mode_factor", "abnormal_condition" })
            {

                var json = new OMBC_Instruction(instructionId, executionTime, onId, 0.5, false).ToJSON();
                json.Remove(property);

                Assert.That(OMBC_Instruction.TryParse(json, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

        }

        [Test]
        public void Instruction_Strict_RejectsAdditionalProperties()
        {

            var json = new OMBC_Instruction(instructionId, executionTime, onId, 0.5, false).ToJSON();
            json.Add("foo", 1);

            Assert.That(OMBC_Instruction.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(OMBC_Instruction.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Rules.OMBC.Instruction.OperationModeFactorRange")]
        public void Instruction_RejectsOperationModeFactor_OutsideZeroToOne()
        {

            Assert.That(() => new OMBC_Instruction(instructionId, executionTime, onId, -0.01,      false), Throws.ArgumentException.With.Message.Contains("operation mode factor").And.Message.Contains("[0, 1]"));
            Assert.That(() => new OMBC_Instruction(instructionId, executionTime, onId,  1.01,      false), Throws.ArgumentException.With.Message.Contains("operation mode factor").And.Message.Contains("[0, 1]"));
            Assert.That(() => new OMBC_Instruction(instructionId, executionTime, onId,  Double.NaN, false), Throws.ArgumentException.With.Message.Contains("operation mode factor"));

            Assert.That(() => new OMBC_Instruction(instructionId, executionTime, onId, 0, false), Throws.Nothing);
            Assert.That(() => new OMBC_Instruction(instructionId, executionTime, onId, 1, false), Throws.Nothing);

            var json = new OMBC_Instruction(instructionId, executionTime, onId, 0.5, false).ToJSON();
            json["operation_mode_factor"] = -1;

            Assert.That(OMBC_Instruction.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("operation mode factor").And.Contains("[0, 1]"));

        }

        #endregion

        #region OMBC_TimerStatus

        [Test]
        public void TimerStatus_RoundTrips_AndIsSchemaValid()
        {

            var finishedAt  = new DateTimeOffset(2024, 5, 1, 12, 10, 0, 500, TimeSpan.Zero);
            var timerStatus = new OMBC_TimerStatus(minRunTimeId, finishedAt, messageId);
            var json        = timerStatus.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "OMBC.TimerStatus");

            Assert.That(json.Properties().Select(p => p.Name),  Is.EqualTo(new[] { "message_type", "message_id", "timer_id", "finished_at" }));
            Assert.That(json["message_type"]?.Value<String>(),  Is.EqualTo("OMBC.TimerStatus"));
            Assert.That(json["timer_id"]?.    Value<String>(),  Is.EqualTo(minRunTimeId.ToString()));
            Assert.That(json["finished_at"]?. Value<String>(),  Is.EqualTo("2024-05-01T12:10:00.5Z"));

            Assert.That(OMBC_TimerStatus.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                 Is.EqualTo(timerStatus));
            Assert.That(parsed!.GetHashCode(),  Is.EqualTo(timerStatus.GetHashCode()));
            Assert.That(parsed.Clone(),         Is.EqualTo(timerStatus));
            Assert.That(parsed.TimerId,         Is.EqualTo(minRunTimeId));
            Assert.That(parsed.FinishedAt,      Is.EqualTo(finishedAt));
            Assert.That(parsed.ToString(),      Does.EndWith($"[{messageId}]"));

        }

        [Test]
        public void TimerStatus_MissingMandatoryProperty_Fails()
        {

            foreach (var property in new[] { "message_type", "message_id", "timer_id", "finished_at" })
            {

                var json = new OMBC_TimerStatus(minRunTimeId, validFrom).ToJSON();
                json.Remove(property);

                Assert.That(OMBC_TimerStatus.TryParse(json, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

        }

        [Test]
        public void TimerStatus_Strict_RejectsAdditionalProperties_AndNaiveTimestamps()
        {

            var json = new OMBC_TimerStatus(minRunTimeId, validFrom).ToJSON();
            json.Add("foo", 1);

            Assert.That(OMBC_TimerStatus.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(OMBC_TimerStatus.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

            var naive = S2JSONExtensions.ParseS2JSON("""{ "message_type": "OMBC.TimerStatus", "message_id": "3c7f9a1e-2b4d-4c6e-8f0a-000000000100", "timer_id": "3c7f9a1e-2b4d-4c6e-8f0a-000000000030", "finished_at": "2024-05-01T12:00:00" }""");

            Assert.That(OMBC_TimerStatus.TryParse(naive, out _, out _,     S2ParserOptions.Default), Is.True);
            Assert.That(OMBC_TimerStatus.TryParse(naive, out _, out error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("finished_at"));

        }

        #endregion

        #region S2MessageParser

        [Test]
        public void MessageParser_DispatchesOMBCInstruction()
        {

            var text = new OMBC_Instruction(instructionId, executionTime, onId, 0.5, false, messageId).ToJSON().ToString();

            Assert.That(S2MessageParser.TryParse(text, null, out var message, out var error), Is.True, error?.DiagnosticLabel);
            Assert.That(message,                                        Is.TypeOf<OMBC_Instruction>());
            Assert.That(((OMBC_Instruction) message!).OperationModeId,  Is.EqualTo(onId));
            Assert.That(((OMBC_Instruction) message).MessageId,         Is.EqualTo(messageId));

        }

        #endregion

    }

}

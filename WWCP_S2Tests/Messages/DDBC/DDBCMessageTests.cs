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

namespace cloud.charging.open.protocols.S2.Tests.Messages.DDBC
{

    /// <summary>
    /// Tests of the DDBC messages: DDBC.SystemDescription, DDBC.ActuatorStatus, DDBC.Instruction,
    /// DDBC.TimerStatus, DDBC.PresentDemandStatus and DDBC.AverageDemandRateForecast (CONVENTIONS.md §8).
    /// The S2 documentation has no DDBC message examples, therefore the values follow the
    /// EV charger example used by the DDBC data structure tests.
    /// </summary>
    [TestFixture]
    public sealed class DDBCMessageTests
    {

        #region Data / helpers

        private static readonly Message_Id        messageId           = Message_Id.      Parse("2b7f1c0e-6d3a-4f8b-9c1d-5e7a9b3c1d2f");
        private static readonly Actuator_Id       actuatorId          = Actuator_Id.     Parse("0d5f0b5e-3a1c-4d7e-9a2b-1c3d5e7f9a0b");
        private static readonly Actuator_Id       secondActuatorId    = Actuator_Id.     Parse("1e6a1c6f-4b2d-4e8f-8b3c-2d4e6f8a0b1c");
        private static readonly OperationMode_Id  idleModeId          = OperationMode_Id.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
        private static readonly OperationMode_Id  chargeModeId        = OperationMode_Id.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
        private static readonly Transition_Id     idleToChargeId      = Transition_Id.   Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
        private static readonly Transition_Id     chargeToIdleId      = Transition_Id.   Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8");
        private static readonly Timer_Id          minimumOnTimerId    = Timer_Id.        Parse("9e107d9d-372b-4c9a-9b7e-0a1b2c3d4e5f");
        private static readonly Instruction_Id    instructionId       = Instruction_Id.  Parse("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d");

        private static readonly DateTimeOffset    validFrom           = new (2024, 5, 1, 10,  0,  0, TimeSpan.Zero);
        private static readonly DateTimeOffset    transitionTimestamp = new (2024, 5, 1, 12, 30, 15, TimeSpan.FromHours(2));
        private static readonly DateTimeOffset    executionTime       = new (2024, 5, 1, 11,  0,  0, TimeSpan.Zero);
        private static readonly DateTimeOffset    finishedAt          = new (2024, 5, 1, 11,  2,  0, TimeSpan.Zero);


        private static PowerRange ElectricPowerRange(Double StartOfRange = 1400, Double EndOfRange = 11000)
            => new (StartOfRange, EndOfRange, CommodityQuantity.ElectricPower3PhaseSymmetric);

        private static DDBC_OperationMode IdleMode()
            => new (idleModeId,
                    [ ElectricPowerRange(0, 0) ],
                    new NumberRange(0, 0),
                    false,
                    "Idle");

        private static DDBC_OperationMode ChargeMode()
            => new (chargeModeId,
                    [ ElectricPowerRange() ],
                    new NumberRange(0.00065, 0.0051),
                    false,
                    "Charging",
                    new NumberRange(0.01, 0.02));

        private static Transition IdleToCharge()
            => new (idleToChargeId,
                    idleModeId,
                    chargeModeId,
                    [ minimumOnTimerId ],
                    [],
                    false,
                    0.5,
                    Duration.FromMilliseconds(3000));

        private static Transition ChargeToIdle()
            => new (chargeToIdleId,
                    chargeModeId,
                    idleModeId,
                    [],
                    [ minimumOnTimerId ],
                    false);

        private static Timer MinimumOnTimer()
            => new (minimumOnTimerId,
                    Duration.FromMilliseconds(120000),
                    "Minimum on time");

        private static DDBC_ActuatorDescription EVChargerActuator(Actuator_Id? Id = null)
            => new (Id ?? actuatorId,
                    [ Commodity.Electricity ],
                    [ IdleMode(), ChargeMode() ],
                    [ IdleToCharge(), ChargeToIdle() ],
                    [ MinimumOnTimer() ],
                    "EV charger");

        private static DDBC_SystemDescription SystemDescription()
            => new (validFrom,
                    [ EVChargerActuator() ],
                    true,
                    messageId);

        private static DDBC_ActuatorStatus ActuatorStatus()
            => new (actuatorId,
                    chargeModeId,
                    0.75,
                    idleModeId,
                    transitionTimestamp,
                    messageId);

        private static DDBC_Instruction Instruction()
            => new (instructionId,
                    executionTime,
                    false,
                    actuatorId,
                    chargeModeId,
                    0.5,
                    messageId);

        private static DDBC_TimerStatus TimerStatus()
            => new (minimumOnTimerId,
                    actuatorId,
                    finishedAt,
                    messageId);

        private static DDBC_PresentDemandStatus PresentDemandStatus()
            => new (new NumberRange(0.002, 0.003),
                    messageId);

        private static DDBC_AverageDemandRateForecastElement FullForecastElement()
            => new (Duration.FromMilliseconds(900000),
                    0.003,
                    DemandRateUpperLimit:  0.0051,
                    DemandRateUpper95PPR:  0.0045,
                    DemandRateUpper68PPR:  0.0035,
                    DemandRateLower68PPR:  0.0025,
                    DemandRateLower95PPR:  0.0015,
                    DemandRateLowerLimit:  0.00065);

        private static DDBC_AverageDemandRateForecast AverageDemandRateForecast()
            => new (validFrom,
                    [ FullForecastElement(), new DDBC_AverageDemandRateForecastElement(Duration.FromMilliseconds(900000), 0.002) ],
                    messageId);

        #endregion


        #region DDBC.SystemDescription

        [Test]
        public void SystemDescription_RoundTrips_AndIsSchemaValid()
        {

            var message = SystemDescription();
            var json    = message.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] { "message_type", "message_id", "valid_from", "actuators", "provides_average_demand_rate_forecast" }));
            Assert.That(json["message_type"]?.Value<String>(),                           Is.EqualTo("DDBC.SystemDescription"));
            Assert.That(json["message_id"]?.Value<String>(),                             Is.EqualTo(messageId.ToString()));
            Assert.That(json["valid_from"]?.Value<String>(),                             Is.EqualTo("2024-05-01T10:00:00Z"));
            Assert.That(json["provides_average_demand_rate_forecast"]?.Value<Boolean>(), Is.True);

            S2SchemaValidator.AssertValidMessage(json, "DDBC.SystemDescription");

            Assert.That(DDBC_SystemDescription.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                                     Is.EqualTo(message));
            Assert.That(parsed!.GetHashCode(),                      Is.EqualTo(message.GetHashCode()));
            Assert.That(parsed.MessageId,                           Is.EqualTo(messageId));
            Assert.That(parsed.ValidFrom,                           Is.EqualTo(validFrom));
            Assert.That(parsed.Actuators,                           Has.Count.EqualTo(1));
            Assert.That(parsed.Actuators[0].Id,                     Is.EqualTo(actuatorId));
            Assert.That(parsed.ProvidesAverageDemandRateForecast,   Is.True);

            Assert.That(message.RevokableObjectType,                Is.EqualTo(RevokableObject.DDBC_SystemDescription));
            Assert.That(message.RevokableObjectId.ToString(),       Is.EqualTo(messageId.ToString()));

            Assert.That(message.Clone(),                            Is.EqualTo(message));
            Assert.That(message == parsed,                          Is.True);
            Assert.That(message != new DDBC_SystemDescription(validFrom, [ EVChargerActuator() ], false, messageId), Is.True);
            Assert.That(message.ToString(),                         Does.Contain("DDBC.SystemDescription").And.Contain($"[{messageId}]"));

        }

        [Test]
        public void SystemDescription_WithoutMessageId_GetsANewOne()
        {

            var message1 = new DDBC_SystemDescription(validFrom, [ EVChargerActuator() ], false);
            var message2 = new DDBC_SystemDescription(validFrom, [ EVChargerActuator() ], false);

            Assert.That(message1.MessageId.IsNotNullOrEmpty,  Is.True);
            Assert.That(message1.MessageId,                   Is.Not.EqualTo(message2.MessageId));
            Assert.That(message1,                             Is.Not.EqualTo(message2));

            S2SchemaValidator.AssertValidMessage(message1.ToJSON(), "DDBC.SystemDescription");

        }

        [Test]
        public void SystemDescription_MissingMandatoryProperty_Fails()
        {

            var json = SystemDescription().ToJSON();

            var withoutValidFrom = (JObject) json.DeepClone();
            withoutValidFrom.Remove("valid_from");
            Assert.That(DDBC_SystemDescription.TryParse(withoutValidFrom, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("valid_from"));

            var withoutActuators = (JObject) json.DeepClone();
            withoutActuators.Remove("actuators");
            Assert.That(DDBC_SystemDescription.TryParse(withoutActuators, out _, out error), Is.False);
            Assert.That(error, Does.Contain("actuators"));

            var withoutForecastFlag = (JObject) json.DeepClone();
            withoutForecastFlag.Remove("provides_average_demand_rate_forecast");
            Assert.That(DDBC_SystemDescription.TryParse(withoutForecastFlag, out _, out error), Is.False);
            Assert.That(error, Does.Contain("provides_average_demand_rate_forecast"));

            var withoutMessageId = (JObject) json.DeepClone();
            withoutMessageId.Remove("message_id");
            Assert.That(DDBC_SystemDescription.TryParse(withoutMessageId, out _, out error), Is.False);
            Assert.That(error, Does.Contain("message_id"));

        }

        [Test]
        public void SystemDescription_Strict_RejectsAdditionalProperty()
        {

            var json = SystemDescription().ToJSON();
            json.Add("storage", new JObject());

            Assert.That(DDBC_SystemDescription.TryParse(json, out _, out var error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("storage"));

            Assert.That(DDBC_SystemDescription.TryParse(json, out var lenient, out error), Is.True, error);
            Assert.That(lenient, Is.EqualTo(SystemDescription()));

        }

        [Test]
        public void SystemDescription_ActuatorIds_MustBeUnique_AndWithinBounds()
        {

            Assert.That(() => new DDBC_SystemDescription(validFrom, [], true),
                        Throws.ArgumentException);

            Assert.That(() => new DDBC_SystemDescription(validFrom, [ EVChargerActuator(), EVChargerActuator() ], true),
                        Throws.ArgumentException.With.Message.Contains("unique"));

            var elevenActuators = Enumerable.Range(0, 11).Select(_ => EVChargerActuator(Actuator_Id.NewRandom)).ToList();
            Assert.That(() => new DDBC_SystemDescription(validFrom, elevenActuators, true),
                        Throws.ArgumentException);

            var twoActuators = new DDBC_SystemDescription(validFrom, [ EVChargerActuator(), EVChargerActuator(secondActuatorId) ], true);
            S2SchemaValidator.AssertValidMessage(twoActuators.ToJSON(), "DDBC.SystemDescription");

            var json = SystemDescription().ToJSON();
            ((JArray) json["actuators"]!).Add(EVChargerActuator().ToJSON());
            Assert.That(DDBC_SystemDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("unique"));

            var empty = SystemDescription().ToJSON();
            empty["actuators"] = new JArray();
            Assert.That(DDBC_SystemDescription.TryParse(empty, out _, out error), Is.False);
            Assert.That(error, Does.Contain("actuators"));

        }

        #endregion

        #region DDBC.ActuatorStatus

        [Test]
        public void ActuatorStatus_RoundTrips_AndIsSchemaValid()
        {

            var message = ActuatorStatus();
            var json    = message.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] { "message_type", "message_id", "actuator_id", "active_operation_mode_id", "operation_mode_factor",
                                           "previous_operation_mode_id", "transition_timestamp" }));
            Assert.That(json["message_type"]?.Value<String>(),          Is.EqualTo("DDBC.ActuatorStatus"));
            Assert.That(json["operation_mode_factor"]?.Value<Double>(), Is.EqualTo(0.75));
            Assert.That(json["transition_timestamp"]?.Value<String>(),  Is.EqualTo("2024-05-01T12:30:15+02:00"));

            S2SchemaValidator.AssertValidMessage(json, "DDBC.ActuatorStatus");

            Assert.That(DDBC_ActuatorStatus.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                            Is.EqualTo(message));
            Assert.That(parsed!.GetHashCode(),             Is.EqualTo(message.GetHashCode()));
            Assert.That(parsed.ActuatorId,                 Is.EqualTo(actuatorId));
            Assert.That(parsed.ActiveOperationModeId,      Is.EqualTo(chargeModeId));
            Assert.That(parsed.OperationModeFactor,        Is.EqualTo(0.75));
            Assert.That(parsed.PreviousOperationModeId,    Is.EqualTo(idleModeId));
            Assert.That(parsed.TransitionTimestamp,        Is.EqualTo(transitionTimestamp));
            Assert.That(parsed.TransitionTimestamp!.Value.Offset, Is.EqualTo(TimeSpan.FromHours(2)));

            Assert.That(message.Clone(),                   Is.EqualTo(message));
            Assert.That(message == parsed,                 Is.True);
            Assert.That(message.ToString(),                Does.Contain(actuatorId.ToString()).And.Contain($"[{messageId}]"));

        }

        [Test]
        public void ActuatorStatus_MandatoryOnly_OmitsOptionalProperties()
        {

            var message = new DDBC_ActuatorStatus(actuatorId, idleModeId, 0, MessageId: messageId);
            var json    = message.ToJSON();

            Assert.That(json.ContainsKey("previous_operation_mode_id"), Is.False);
            Assert.That(json.ContainsKey("transition_timestamp"),       Is.False);

            S2SchemaValidator.AssertValidMessage(json, "DDBC.ActuatorStatus");

            Assert.That(DDBC_ActuatorStatus.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                          Is.EqualTo(message));
            Assert.That(parsed!.PreviousOperationModeId, Is.Null);
            Assert.That(parsed.TransitionTimestamp,      Is.Null);
            Assert.That(parsed,                          Is.Not.EqualTo(ActuatorStatus()));

        }

        [Test]
        public void ActuatorStatus_MissingMandatoryProperty_Fails()
        {

            var json = ActuatorStatus().ToJSON();

            var withoutActuatorId = (JObject) json.DeepClone();
            withoutActuatorId.Remove("actuator_id");
            Assert.That(DDBC_ActuatorStatus.TryParse(withoutActuatorId, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("actuator_id"));

            var withoutActiveMode = (JObject) json.DeepClone();
            withoutActiveMode.Remove("active_operation_mode_id");
            Assert.That(DDBC_ActuatorStatus.TryParse(withoutActiveMode, out _, out error), Is.False);
            Assert.That(error, Does.Contain("active_operation_mode_id"));

            var withoutFactor = (JObject) json.DeepClone();
            withoutFactor.Remove("operation_mode_factor");
            Assert.That(DDBC_ActuatorStatus.TryParse(withoutFactor, out _, out error), Is.False);
            Assert.That(error, Does.Contain("operation_mode_factor"));

        }

        [Test]
        public void ActuatorStatus_Strict_RejectsAdditionalProperty()
        {

            var json = ActuatorStatus().ToJSON();
            json.Add("fill_level", 42);

            Assert.That(DDBC_ActuatorStatus.TryParse(json, out _, out var error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("fill_level"));

            Assert.That(DDBC_ActuatorStatus.TryParse(json, out _, out error), Is.True, error);

        }

        [Test]
        public void ActuatorStatus_OperationModeFactor_MustBeBetweenZeroAndOne()
        {

            Assert.That(() => new DDBC_ActuatorStatus(actuatorId, chargeModeId, -0.1),        Throws.ArgumentException);
            Assert.That(() => new DDBC_ActuatorStatus(actuatorId, chargeModeId,  1.1),        Throws.ArgumentException);
            Assert.That(() => new DDBC_ActuatorStatus(actuatorId, chargeModeId,  Double.NaN), Throws.ArgumentException);

            Assert.That(new DDBC_ActuatorStatus(actuatorId, chargeModeId, 0).OperationModeFactor, Is.EqualTo(0));
            Assert.That(new DDBC_ActuatorStatus(actuatorId, chargeModeId, 1).OperationModeFactor, Is.EqualTo(1));

            var json = ActuatorStatus().ToJSON();
            json["operation_mode_factor"] = 1.5;
            Assert.That(DDBC_ActuatorStatus.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("operation mode factor"));

        }

        #endregion

        #region DDBC.Instruction

        [Test]
        public void Instruction_RoundTrips_AndIsSchemaValid()
        {

            var message = Instruction();
            var json    = message.ToJSON();

            // Wire-name quirk: "operation_mode_id" (as in OMBC), not "operation_mode" (FRBC)!
            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] { "message_type", "message_id", "id", "execution_time", "abnormal_condition",
                                           "actuator_id", "operation_mode_id", "operation_mode_factor" }));
            Assert.That(json.ContainsKey("operation_mode"),             Is.False);
            Assert.That(json["message_type"]?.Value<String>(),          Is.EqualTo("DDBC.Instruction"));
            Assert.That(json["execution_time"]?.Value<String>(),        Is.EqualTo("2024-05-01T11:00:00Z"));
            Assert.That(json["abnormal_condition"]?.Value<Boolean>(),   Is.False);

            S2SchemaValidator.AssertValidMessage(json, "DDBC.Instruction");

            Assert.That(DDBC_Instruction.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                          Is.EqualTo(message));
            Assert.That(parsed!.GetHashCode(),           Is.EqualTo(message.GetHashCode()));
            Assert.That(parsed.Id,                       Is.EqualTo(instructionId));
            Assert.That(parsed.ExecutionTime,            Is.EqualTo(executionTime));
            Assert.That(parsed.AbnormalCondition,        Is.False);
            Assert.That(parsed.ActuatorId,               Is.EqualTo(actuatorId));
            Assert.That(parsed.OperationModeId,          Is.EqualTo(chargeModeId));
            Assert.That(parsed.OperationModeFactor,      Is.EqualTo(0.5));

            IInstruction instruction = message;
            Assert.That(instruction.Id,                             Is.EqualTo(instructionId));
            Assert.That(instruction.ExecutionTime,                  Is.EqualTo(executionTime));
            Assert.That(instruction.AbnormalCondition,              Is.False);
            Assert.That(instruction.RevokableObjectType,            Is.EqualTo(RevokableObject.DDBC_Instruction));
            Assert.That(instruction.RevokableObjectId.ToString(),   Is.EqualTo(instructionId.ToString()));

            Assert.That(message.Clone(),                 Is.EqualTo(message));
            Assert.That(message == parsed,               Is.True);
            Assert.That(message.ToString(),              Does.Contain(instructionId.ToString()).And.Contain($"[{messageId}]"));

        }

        [Test]
        public void Instruction_ParsesHandWrittenJSON()
        {

            var json = JObject.Parse("""
                {
                  "message_type":          "DDBC.Instruction",
                  "message_id":            "2b7f1c0e-6d3a-4f8b-9c1d-5e7a9b3c1d2f",
                  "id":                    "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
                  "execution_time":        "2024-05-01T11:00:00Z",
                  "abnormal_condition":    false,
                  "actuator_id":           "0d5f0b5e-3a1c-4d7e-9a2b-1c3d5e7f9a0b",
                  "operation_mode_id":     "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
                  "operation_mode_factor": 0.5
                }
                """);

            Assert.That(DDBC_Instruction.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed, Is.EqualTo(Instruction()));

            var reserialised = parsed!.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "DDBC.Instruction");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void Instruction_MissingMandatoryProperty_Fails()
        {

            var json = Instruction().ToJSON();

            foreach (var property in new[] { "id", "execution_time", "abnormal_condition", "actuator_id", "operation_mode_id", "operation_mode_factor" })
            {

                var incomplete = (JObject) json.DeepClone();
                incomplete.Remove(property);

                Assert.That(DDBC_Instruction.TryParse(incomplete, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

            // "operation_mode" (the FRBC key) is not a substitute for "operation_mode_id"!
            var frbcKey = (JObject) json.DeepClone();
            frbcKey.Remove("operation_mode_id");
            frbcKey.Add("operation_mode", chargeModeId.ToString());
            Assert.That(DDBC_Instruction.TryParse(frbcKey, out _, out var error2), Is.False);
            Assert.That(error2, Does.Contain("operation_mode_id"));

        }

        [Test]
        public void Instruction_Strict_RejectsAdditionalProperty()
        {

            var json = Instruction().ToJSON();
            json.Add("operation_mode", chargeModeId.ToString());

            Assert.That(DDBC_Instruction.TryParse(json, out _, out var error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("operation_mode"));

            Assert.That(DDBC_Instruction.TryParse(json, out _, out error), Is.True, error);

        }

        [Test]
        public void Instruction_OperationModeFactor_MustBeBetweenZeroAndOne()
        {

            Assert.That(() => new DDBC_Instruction(instructionId, executionTime, false, actuatorId, chargeModeId, -0.01),      Throws.ArgumentException);
            Assert.That(() => new DDBC_Instruction(instructionId, executionTime, false, actuatorId, chargeModeId,  1.01),      Throws.ArgumentException);
            Assert.That(() => new DDBC_Instruction(instructionId, executionTime, false, actuatorId, chargeModeId, Double.NaN), Throws.ArgumentException);

            Assert.That(new DDBC_Instruction(instructionId, executionTime, true, actuatorId, chargeModeId, 0).OperationModeFactor, Is.EqualTo(0));
            Assert.That(new DDBC_Instruction(instructionId, executionTime, true, actuatorId, chargeModeId, 1).OperationModeFactor, Is.EqualTo(1));

            var json = Instruction().ToJSON();
            json["operation_mode_factor"] = -1;
            Assert.That(DDBC_Instruction.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("operation mode factor"));

        }

        [Test]
        public void Instruction_IsDispatchedByTheMessageParser()
        {

            var text = Instruction().ToJSON().ToString();

            Assert.That(S2MessageParser.TryParse(text, null, out var message, out var error), Is.True, error?.DiagnosticLabel);
            Assert.That(message,                                       Is.TypeOf<DDBC_Instruction>());
            Assert.That(message,                                       Is.EqualTo(Instruction()));
            Assert.That(((DDBC_Instruction) message!).OperationModeId, Is.EqualTo(chargeModeId));

            var wrongType = Instruction().ToJSON();
            wrongType["message_type"] = "FRBC.Instruction";
            Assert.That(DDBC_Instruction.TryParse(wrongType, out _, out var parseError), Is.False);
            Assert.That(parseError, Does.Contain("Unexpected message type"));

        }

        #endregion

        #region DDBC.TimerStatus

        [Test]
        public void TimerStatus_RoundTrips_AndIsSchemaValid()
        {

            var message = TimerStatus();
            var json    = message.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] { "message_type", "message_id", "timer_id", "actuator_id", "finished_at" }));
            Assert.That(json["message_type"]?.Value<String>(),  Is.EqualTo("DDBC.TimerStatus"));
            Assert.That(json["finished_at"]?.Value<String>(),   Is.EqualTo("2024-05-01T11:02:00Z"));

            S2SchemaValidator.AssertValidMessage(json, "DDBC.TimerStatus");

            Assert.That(DDBC_TimerStatus.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                    Is.EqualTo(message));
            Assert.That(parsed!.GetHashCode(),     Is.EqualTo(message.GetHashCode()));
            Assert.That(parsed.TimerId,            Is.EqualTo(minimumOnTimerId));
            Assert.That(parsed.ActuatorId,         Is.EqualTo(actuatorId));
            Assert.That(parsed.FinishedAt,         Is.EqualTo(finishedAt));

            Assert.That(message.Clone(),           Is.EqualTo(message));
            Assert.That(message == parsed,         Is.True);
            Assert.That(message != new DDBC_TimerStatus(minimumOnTimerId, secondActuatorId, finishedAt, messageId), Is.True);
            Assert.That(message.ToString(),        Does.Contain(minimumOnTimerId.ToString()).And.Contain($"[{messageId}]"));

        }

        [Test]
        public void TimerStatus_MissingMandatoryProperty_Fails()
        {

            var json = TimerStatus().ToJSON();

            foreach (var property in new[] { "timer_id", "actuator_id", "finished_at" })
            {

                var incomplete = (JObject) json.DeepClone();
                incomplete.Remove(property);

                Assert.That(DDBC_TimerStatus.TryParse(incomplete, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

        }

        [Test]
        public void TimerStatus_Strict_RejectsAdditionalProperty()
        {

            var json = TimerStatus().ToJSON();
            json.Add("started_at", "2024-05-01T11:00:00Z");

            Assert.That(DDBC_TimerStatus.TryParse(json, out _, out var error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("started_at"));

            Assert.That(DDBC_TimerStatus.TryParse(json, out _, out error), Is.True, error);

        }

        [Test]
        public void TimerStatus_InvalidTimestamp_Fails()
        {

            var json = TimerStatus().ToJSON();
            json["finished_at"] = "not a timestamp";

            Assert.That(DDBC_TimerStatus.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("finished_at"));

        }

        #endregion

        #region DDBC.PresentDemandStatus

        [Test]
        public void PresentDemandStatus_RoundTrips_AndIsSchemaValid()
        {

            var message = PresentDemandStatus();
            var json    = message.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] { "message_type", "message_id", "present_demand_rate" }));
            Assert.That(json["message_type"]?.Value<String>(),                              Is.EqualTo("DDBC.PresentDemandStatus"));
            Assert.That(json["present_demand_rate"]?["start_of_range"]?.Value<Double>(),    Is.EqualTo(0.002));
            Assert.That(json["present_demand_rate"]?["end_of_range"]?.  Value<Double>(),    Is.EqualTo(0.003));

            S2SchemaValidator.AssertValidMessage(json, "DDBC.PresentDemandStatus");

            Assert.That(DDBC_PresentDemandStatus.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                        Is.EqualTo(message));
            Assert.That(parsed!.GetHashCode(),         Is.EqualTo(message.GetHashCode()));
            Assert.That(parsed.PresentDemandRate,      Is.EqualTo(new NumberRange(0.002, 0.003)));

            Assert.That(message.Clone(),               Is.EqualTo(message));
            Assert.That(message == parsed,             Is.True);
            Assert.That(message != new DDBC_PresentDemandStatus(new NumberRange(0.002, 0.004), messageId), Is.True);
            Assert.That(message.ToString(),            Does.Contain("DDBC.PresentDemandStatus").And.Contain($"[{messageId}]"));

        }

        [Test]
        public void PresentDemandStatus_MissingMandatoryProperty_Fails()
        {

            var json = PresentDemandStatus().ToJSON();
            json.Remove("present_demand_rate");

            Assert.That(DDBC_PresentDemandStatus.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("present_demand_rate"));

            var notAnObject = PresentDemandStatus().ToJSON();
            notAnObject["present_demand_rate"] = 0.002;
            Assert.That(DDBC_PresentDemandStatus.TryParse(notAnObject, out _, out error), Is.False);
            Assert.That(error, Does.Contain("present_demand_rate"));

        }

        [Test]
        public void PresentDemandStatus_Strict_RejectsAdditionalProperty()
        {

            var json = PresentDemandStatus().ToJSON();
            json.Add("commodity_quantity", "HEAT.THERMAL_POWER");

            Assert.That(DDBC_PresentDemandStatus.TryParse(json, out _, out var error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("commodity_quantity"));

            Assert.That(DDBC_PresentDemandStatus.TryParse(json, out _, out error), Is.True, error);

        }

        [Test]
        public void PresentDemandStatus_InvalidNumberRange_Fails()
        {

            // The nested NumberRange validates start_of_range <= end_of_range.
            var json = JObject.Parse("""
                {
                  "message_type":        "DDBC.PresentDemandStatus",
                  "message_id":          "2b7f1c0e-6d3a-4f8b-9c1d-5e7a9b3c1d2f",
                  "present_demand_rate": { "start_of_range": 0.003, "end_of_range": 0.002 }
                }
                """);

            Assert.That(DDBC_PresentDemandStatus.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("present_demand_rate"));

        }

        #endregion

        #region DDBC.AverageDemandRateForecast

        [Test]
        public void AverageDemandRateForecast_RoundTrips_AndIsSchemaValid()
        {

            var message = AverageDemandRateForecast();
            var json    = message.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] { "message_type", "message_id", "start_time", "elements" }));
            Assert.That(json["message_type"]?.Value<String>(),  Is.EqualTo("DDBC.AverageDemandRateForecast"));
            Assert.That(json["start_time"]?.Value<String>(),    Is.EqualTo("2024-05-01T10:00:00Z"));
            Assert.That(json["elements"],                       Has.Count.EqualTo(2));

            S2SchemaValidator.AssertValidMessage(json, "DDBC.AverageDemandRateForecast");

            Assert.That(DDBC_AverageDemandRateForecast.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                                  Is.EqualTo(message));
            Assert.That(parsed!.GetHashCode(),                   Is.EqualTo(message.GetHashCode()));
            Assert.That(parsed.StartTime,                        Is.EqualTo(validFrom));
            Assert.That(parsed.Elements,                         Has.Count.EqualTo(2));
            Assert.That(parsed.Elements[0],                      Is.EqualTo(FullForecastElement()));
            Assert.That(parsed.Elements[1].DemandRateExpected,   Is.EqualTo(0.002));
            Assert.That(parsed.Elements[1].DemandRateUpperLimit, Is.Null);

            Assert.That(message.Clone(),                         Is.EqualTo(message));
            Assert.That(message == parsed,                       Is.True);
            Assert.That(message.ToString(),                      Does.Contain("2 element(s)").And.Contain($"[{messageId}]"));

        }

        [Test]
        public void AverageDemandRateForecast_MandatoryOnly_IsSchemaValid()
        {

            var message = new DDBC_AverageDemandRateForecast(validFrom,
                                                             [ new DDBC_AverageDemandRateForecastElement(Duration.FromSeconds(900), 0.001) ]);
            var json    = message.ToJSON();

            Assert.That(json["elements"]?[0]?["duration"]?.Value<Int64>(), Is.EqualTo(900000));
            Assert.That(((JObject) json["elements"]![0]!).ContainsKey("demand_rate_upper_limit"), Is.False);

            S2SchemaValidator.AssertValidMessage(json, "DDBC.AverageDemandRateForecast");

            Assert.That(DDBC_AverageDemandRateForecast.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed, Is.EqualTo(message));

        }

        [Test]
        public void AverageDemandRateForecast_MissingMandatoryProperty_Fails()
        {

            var json = AverageDemandRateForecast().ToJSON();

            var withoutStartTime = (JObject) json.DeepClone();
            withoutStartTime.Remove("start_time");
            Assert.That(DDBC_AverageDemandRateForecast.TryParse(withoutStartTime, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("start_time"));

            var withoutElements = (JObject) json.DeepClone();
            withoutElements.Remove("elements");
            Assert.That(DDBC_AverageDemandRateForecast.TryParse(withoutElements, out _, out error), Is.False);
            Assert.That(error, Does.Contain("elements"));

        }

        [Test]
        public void AverageDemandRateForecast_Strict_RejectsAdditionalProperty()
        {

            var json = AverageDemandRateForecast().ToJSON();
            json.Add("end_time", "2024-05-01T10:30:00Z");

            Assert.That(DDBC_AverageDemandRateForecast.TryParse(json, out _, out var error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("end_time"));

            Assert.That(DDBC_AverageDemandRateForecast.TryParse(json, out _, out error), Is.True, error);

        }

        [Test]
        public void AverageDemandRateForecast_Elements_MustBeWithinBounds()
        {

            Assert.That(() => new DDBC_AverageDemandRateForecast(validFrom, []),
                        Throws.ArgumentException);

            var tooMany = Enumerable.Range(0, 289).Select(_ => new DDBC_AverageDemandRateForecastElement(Duration.FromSeconds(300), 0.001)).ToList();
            Assert.That(() => new DDBC_AverageDemandRateForecast(validFrom, tooMany),
                        Throws.ArgumentException);

            var maximum = new DDBC_AverageDemandRateForecast(validFrom, tooMany.Take(288).ToList());
            Assert.That(maximum.Elements, Has.Count.EqualTo(288));

            var json = AverageDemandRateForecast().ToJSON();
            json["elements"] = new JArray();
            Assert.That(DDBC_AverageDemandRateForecast.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("elements"));

        }

        #endregion

    }

}

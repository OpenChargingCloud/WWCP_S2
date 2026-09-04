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

namespace cloud.charging.open.protocols.S2.Tests.DataStructures.DDBC
{

    /// <summary>
    /// Tests of the DDBC data structures: DDBC_OperationMode, DDBC_ActuatorDescription
    /// and DDBC_AverageDemandRateForecastElement (CONVENTIONS.md §6).
    /// Values follow the EV charger example of the S2 documentation
    /// (power 1400..11000 W, 3000 ms transition duration, 120 s minimum-on timer).
    /// </summary>
    [TestFixture]
    public sealed class DDBCTypeTests
    {

        #region Data / helpers

        private static readonly Actuator_Id       actuatorId          = Actuator_Id.     Parse("0d5f0b5e-3a1c-4d7e-9a2b-1c3d5e7f9a0b");
        private static readonly OperationMode_Id  idleModeId          = OperationMode_Id.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
        private static readonly OperationMode_Id  chargeModeId        = OperationMode_Id.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
        private static readonly Transition_Id     idleToChargeId      = Transition_Id.   Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
        private static readonly Transition_Id     chargeToIdleId      = Transition_Id.   Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8");
        private static readonly Timer_Id          minimumOnTimerId    = Timer_Id.        Parse("9e107d9d-372b-4c9a-9b7e-0a1b2c3d4e5f");


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

        private static DDBC_ActuatorDescription EVChargerActuator()
            => new (actuatorId,
                    [ Commodity.Electricity ],
                    [ IdleMode(), ChargeMode() ],
                    [ IdleToCharge(), ChargeToIdle() ],
                    [ MinimumOnTimer() ],
                    "EV charger");

        private static DDBC_AverageDemandRateForecastElement FullForecastElement()
            => new (Duration.FromMilliseconds(900000),
                    0.003,
                    DemandRateUpperLimit:  0.0051,
                    DemandRateUpper95PPR:  0.0045,
                    DemandRateUpper68PPR:  0.0035,
                    DemandRateLower68PPR:  0.0025,
                    DemandRateLower95PPR:  0.0015,
                    DemandRateLowerLimit:  0.00065);

        #endregion


        #region DDBC_OperationMode

        [Test]
        public void OperationMode_RoundTrips_AndIsSchemaValid()
        {

            var operationMode = ChargeMode();
            var json          = operationMode.ToJSON();

            // Wire-name quirk: capital "Id"!
            Assert.That(json.ContainsKey("Id"),                                Is.True);
            Assert.That(json.ContainsKey("id"),                                Is.False);
            Assert.That(json["Id"]?.Value<String>(),                           Is.EqualTo(chargeModeId.ToString()));
            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] { "Id", "diagnostic_label", "power_ranges", "supply_range", "running_costs", "abnormal_condition_only" }));

            S2SchemaValidator.AssertValidType(json, "DDBC.OperationMode");

            Assert.That(DDBC_OperationMode.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                        Is.EqualTo(operationMode));
            Assert.That(parsed!.GetHashCode(),         Is.EqualTo(operationMode.GetHashCode()));
            Assert.That(parsed.Id,                     Is.EqualTo(chargeModeId));
            Assert.That(parsed.DiagnosticLabel,        Is.EqualTo("Charging"));
            Assert.That(parsed.PowerRanges,            Has.Count.EqualTo(1));
            Assert.That(parsed.SupplyRange,            Is.EqualTo(new NumberRange(0.00065, 0.0051)));
            Assert.That(parsed.RunningCosts,           Is.EqualTo(new NumberRange(0.01, 0.02)));
            Assert.That(parsed.AbnormalConditionOnly,  Is.False);

            Assert.That(operationMode.Clone(),         Is.EqualTo(operationMode));
            Assert.That(operationMode == parsed,       Is.True);
            Assert.That(operationMode != IdleMode(),   Is.True);
            Assert.That(operationMode.ToString(),      Does.Contain(chargeModeId.ToString()).And.Contain("Charging"));

        }

        [Test]
        public void OperationMode_MandatoryOnly_OmitsOptionalProperties()
        {

            var operationMode = new DDBC_OperationMode(idleModeId, [ ElectricPowerRange() ], new NumberRange(0, 100), true);
            var json          = operationMode.ToJSON();

            Assert.That(json.ContainsKey("Id"),                       Is.True);
            Assert.That(json.ContainsKey("diagnostic_label"),         Is.False);
            Assert.That(json.ContainsKey("running_costs"),            Is.False);
            Assert.That(json["abnormal_condition_only"]?.Value<Boolean>(), Is.True);

            S2SchemaValidator.AssertValidType(json, "DDBC.OperationMode");

            Assert.That(DDBC_OperationMode.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                  Is.EqualTo(operationMode));
            Assert.That(parsed!.DiagnosticLabel, Is.Null);
            Assert.That(parsed.RunningCosts,     Is.Null);

        }

        [Test]
        public void OperationMode_MissingMandatoryProperty_Fails()
        {

            var json = ChargeMode().ToJSON();

            var withoutSupplyRange = (JObject) json.DeepClone();
            withoutSupplyRange.Remove("supply_range");
            Assert.That(DDBC_OperationMode.TryParse(withoutSupplyRange, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("supply_range"));

            var withoutPowerRanges = (JObject) json.DeepClone();
            withoutPowerRanges.Remove("power_ranges");
            Assert.That(DDBC_OperationMode.TryParse(withoutPowerRanges, out _, out error), Is.False);
            Assert.That(error, Does.Contain("power_ranges"));

            var withoutAbnormalConditionOnly = (JObject) json.DeepClone();
            withoutAbnormalConditionOnly.Remove("abnormal_condition_only");
            Assert.That(DDBC_OperationMode.TryParse(withoutAbnormalConditionOnly, out _, out error), Is.False);
            Assert.That(error, Does.Contain("abnormal_condition_only"));

        }

        [Test]
        public void OperationMode_LowerCaseId_IsNotAccepted()
        {

            // The schema key is "Id" (capital I); a lower-case "id" is a different (unknown) property.
            var json = JObject.Parse("""
                {
                    "id":                      "7c9e6679-7425-40de-944b-e07fc1f90ae7",
                    "power_ranges":            [ { "start_of_range": 1400, "end_of_range": 11000, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" } ],
                    "supply_range":            { "start_of_range": 0, "end_of_range": 100 },
                    "abnormal_condition_only": false
                }
                """);

            Assert.That(DDBC_OperationMode.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("'Id'"));

            json["Id"] = "7c9e6679-7425-40de-944b-e07fc1f90ae7";
            Assert.That(DDBC_OperationMode.TryParse(json, out _, out _,     S2ParserOptions.Default), Is.True);
            Assert.That(DDBC_OperationMode.TryParse(json, out _, out error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("'id'"));

        }

        [Test]
        public void OperationMode_Strict_RejectsAdditionalProperty()
        {

            var json = JObject.Parse("""
                {
                    "Id":                      "7c9e6679-7425-40de-944b-e07fc1f90ae7",
                    "power_ranges":            [ { "start_of_range": 1400, "end_of_range": 11000, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" } ],
                    "supply_range":            { "start_of_range": 0, "end_of_range": 100 },
                    "abnormal_condition_only": false,
                    "foo":                     42
                }
                """);

            Assert.That(DDBC_OperationMode.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(DDBC_OperationMode.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Rules.DDBC.OperationMode.OnePowerRangePerCommodityQuantity")]
        public void OperationMode_RejectsMultiplePowerRangesPerCommodityQuantity()
        {

            Assert.That(() => new DDBC_OperationMode(chargeModeId,
                                                     [ ElectricPowerRange(0, 1000), ElectricPowerRange(1000, 2000) ],
                                                     new NumberRange(0, 100),
                                                     false),
                        Throws.ArgumentException);

            // Different commodity quantities of the same commodity are fine (three-phase device).
            Assert.That(() => new DDBC_OperationMode(chargeModeId,
                                                     [
                                                         new PowerRange(0, 3700, CommodityQuantity.ElectricPowerL1),
                                                         new PowerRange(0, 3700, CommodityQuantity.ElectricPowerL2),
                                                         new PowerRange(0, 3700, CommodityQuantity.ElectricPowerL3)
                                                     ],
                                                     new NumberRange(0, 100),
                                                     false),
                        Throws.Nothing);

            var json = JObject.Parse("""
                {
                    "Id":                      "7c9e6679-7425-40de-944b-e07fc1f90ae7",
                    "power_ranges":            [
                        { "start_of_range": 0,    "end_of_range": 1000, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" },
                        { "start_of_range": 1000, "end_of_range": 2000, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" }
                    ],
                    "supply_range":            { "start_of_range": 0, "end_of_range": 100 },
                    "abnormal_condition_only": false
                }
                """);

            Assert.That(DDBC_OperationMode.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at most one power range per commodity quantity"));

        }

        [Test]
        public void OperationMode_RejectsEmptyOrTooManyPowerRanges()
        {

            Assert.That(() => new DDBC_OperationMode(chargeModeId, [], new NumberRange(0, 100), false),
                        Throws.ArgumentException);

            Assert.That(() => new DDBC_OperationMode(chargeModeId, Enumerable.Repeat(ElectricPowerRange(), 11).ToList(), new NumberRange(0, 100), false),
                        Throws.ArgumentException);

            var json = JObject.Parse("""
                {
                    "Id":                      "7c9e6679-7425-40de-944b-e07fc1f90ae7",
                    "power_ranges":            [],
                    "supply_range":            { "start_of_range": 0, "end_of_range": 100 },
                    "abnormal_condition_only": false
                }
                """);

            Assert.That(DDBC_OperationMode.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("power_ranges"));

        }

        [Test]
        public void OperationMode_InvalidSupplyRange_FailsWithNestedError()
        {

            var json = JObject.Parse("""
                {
                    "Id":                      "7c9e6679-7425-40de-944b-e07fc1f90ae7",
                    "power_ranges":            [ { "start_of_range": 1400, "end_of_range": 11000, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" } ],
                    "supply_range":            { "start_of_range": 100, "end_of_range": 0 },
                    "abnormal_condition_only": false
                }
                """);

            Assert.That(DDBC_OperationMode.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("supply_range").And.Contain("must not be greater"));

        }

        #endregion

        #region DDBC_ActuatorDescription

        [Test]
        public void ActuatorDescription_RoundTrips_AndIsSchemaValid()
        {

            var actuator = EVChargerActuator();
            var json     = actuator.ToJSON();

            // Wire-name quirk: "supported_commodites" (sic)!
            Assert.That(json.ContainsKey("supported_commodites"),   Is.True);
            Assert.That(json.ContainsKey("supported_commodities"),  Is.False);
            Assert.That(json["supported_commodites"]?.Select(c => c.Value<String>()), Is.EqualTo(new[] { "ELECTRICITY" }));
            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] { "id", "diagnostic_label", "supported_commodites", "operation_modes", "transitions", "timers" }));

            // Nested operation modes keep their "Id" quirk.
            Assert.That(json["operation_modes"]![0]!["Id"]?.Value<String>(), Is.EqualTo(idleModeId.ToString()));

            S2SchemaValidator.AssertValidType(json, "DDBC.ActuatorDescription");

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                        Is.EqualTo(actuator));
            Assert.That(parsed!.GetHashCode(),         Is.EqualTo(actuator.GetHashCode()));
            Assert.That(parsed.Id,                     Is.EqualTo(actuatorId));
            Assert.That(parsed.DiagnosticLabel,        Is.EqualTo("EV charger"));
            Assert.That(parsed.SupportedCommodities,   Is.EqualTo(new[] { Commodity.Electricity }));
            Assert.That(parsed.OperationModes,         Has.Count.EqualTo(2));
            Assert.That(parsed.OperationModes[0].Id,   Is.EqualTo(idleModeId));
            Assert.That(parsed.OperationModes[1].Id,   Is.EqualTo(chargeModeId));
            Assert.That(parsed.Transitions,            Has.Count.EqualTo(2));
            Assert.That(parsed.Transitions[0].Id,      Is.EqualTo(idleToChargeId));
            Assert.That(parsed.Timers,                 Has.Count.EqualTo(1));
            Assert.That(parsed.Timers[0].Id,           Is.EqualTo(minimumOnTimerId));

            Assert.That(actuator.Clone(),              Is.EqualTo(actuator));
            Assert.That(actuator == parsed,            Is.True);
            Assert.That(actuator.ToString(),           Does.Contain(actuatorId.ToString()).And.Contain("EV charger"));

        }

        [Test]
        public void ActuatorDescription_MandatoryOnly_OmitsOptionalProperties()
        {

            var actuator = new DDBC_ActuatorDescription(actuatorId,
                                                        [ Commodity.Electricity ],
                                                        [ ChargeMode() ],
                                                        [],
                                                        []);
            var json     = actuator.ToJSON();

            Assert.That(json.ContainsKey("diagnostic_label"),                 Is.False);
            Assert.That(json["transitions"],                                  Is.Empty);
            Assert.That(json["timers"],                                       Is.Empty);

            S2SchemaValidator.AssertValidType(json, "DDBC.ActuatorDescription");

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                  Is.EqualTo(actuator));
            Assert.That(parsed!.DiagnosticLabel, Is.Null);
            Assert.That(parsed.Transitions,      Is.Empty);
            Assert.That(parsed.Timers,           Is.Empty);

        }

        [Test]
        public void ActuatorDescription_MissingMandatoryProperty_Fails()
        {

            var json = EVChargerActuator().ToJSON();

            foreach (var property in new[] { "id", "supported_commodites", "operation_modes", "transitions", "timers" })
            {
                var incomplete = (JObject) json.DeepClone();
                incomplete.Remove(property);
                Assert.That(DDBC_ActuatorDescription.TryParse(incomplete, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));
            }

            // The correctly spelled key is NOT the schema key!
            var correctlySpelled = (JObject) json.DeepClone();
            correctlySpelled.Remove("supported_commodites");
            correctlySpelled.Add("supported_commodities", new JArray("ELECTRICITY"));
            Assert.That(DDBC_ActuatorDescription.TryParse(correctlySpelled, out _, out var spellingError), Is.False);
            Assert.That(spellingError, Does.Contain("supported_commodites"));

        }

        [Test]
        public void ActuatorDescription_Strict_RejectsAdditionalProperty()
        {

            var json = EVChargerActuator().ToJSON();
            json.Add("foo", 42);

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void ActuatorDescription_RejectsSchemaCardinalities()
        {

            Assert.That(() => new DDBC_ActuatorDescription(actuatorId, [],                       [ ChargeMode() ], [], []), Throws.ArgumentException);
            Assert.That(() => new DDBC_ActuatorDescription(actuatorId, [ Commodity.Electricity ], [],               [], []), Throws.ArgumentException);

            var json = EVChargerActuator().ToJSON();
            json["operation_modes"] = new JArray();

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("operation_modes"));

        }

        [Test]
        [S2C("Rules.DDBC.ActuatorDescription.UniqueSupportedCommodities")]
        public void ActuatorDescription_RejectsDuplicateSupportedCommodities()
        {

            Assert.That(() => new DDBC_ActuatorDescription(actuatorId,
                                                           [ Commodity.Electricity, Commodity.Electricity ],
                                                           [ ChargeMode() ],
                                                           [],
                                                           []),
                        Throws.ArgumentException);

            var json = EVChargerActuator().ToJSON();
            json["supported_commodites"] = new JArray("ELECTRICITY", "ELECTRICITY");

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("supported commodities must be unique"));

        }

        [Test]
        [S2C("Rules.DDBC.ActuatorDescription.UniqueOperationModeIds")]
        public void ActuatorDescription_RejectsDuplicateOperationModeIds()
        {

            Assert.That(() => new DDBC_ActuatorDescription(actuatorId,
                                                           [ Commodity.Electricity ],
                                                           [ ChargeMode(), ChargeMode() ],
                                                           [],
                                                           []),
                        Throws.ArgumentException);

            var json = EVChargerActuator().ToJSON();
            json["operation_modes"]![0]!["Id"] = chargeModeId.ToString();

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("operation mode identifications must be unique"));

        }

        [Test]
        [S2C("Rules.DDBC.ActuatorDescription.UniqueTransitionIds")]
        public void ActuatorDescription_RejectsDuplicateTransitionIds()
        {

            var duplicate = new Transition(idleToChargeId, chargeModeId, idleModeId, [], [], false);

            Assert.That(() => new DDBC_ActuatorDescription(actuatorId,
                                                           [ Commodity.Electricity ],
                                                           [ IdleMode(), ChargeMode() ],
                                                           [ IdleToCharge(), duplicate ],
                                                           [ MinimumOnTimer() ]),
                        Throws.ArgumentException);

            var json = EVChargerActuator().ToJSON();
            json["transitions"]![1]!["id"] = idleToChargeId.ToString();

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("transition identifications must be unique"));

        }

        [Test]
        [S2C("Rules.DDBC.ActuatorDescription.UniqueTimerIds")]
        public void ActuatorDescription_RejectsDuplicateTimerIds()
        {

            Assert.That(() => new DDBC_ActuatorDescription(actuatorId,
                                                           [ Commodity.Electricity ],
                                                           [ IdleMode(), ChargeMode() ],
                                                           [ IdleToCharge(), ChargeToIdle() ],
                                                           [ MinimumOnTimer(), new Timer(minimumOnTimerId, Duration.Zero) ]),
                        Throws.ArgumentException);

            var json = EVChargerActuator().ToJSON();
            ((JArray) json["timers"]!).Add(MinimumOnTimer().ToJSON());

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("timer identifications must be unique"));

        }

        [Test]
        [S2C("Rules.DDBC.ActuatorDescription.TransitionOperationModeReferences")]
        public void ActuatorDescription_RejectsTransitionsToUnknownOperationModes()
        {

            var unknownModeId = OperationMode_Id.Parse("ffffffff-ffff-4fff-8fff-ffffffffffff");

            Assert.That(() => new DDBC_ActuatorDescription(actuatorId,
                                                           [ Commodity.Electricity ],
                                                           [ IdleMode(), ChargeMode() ],
                                                           [ new Transition(idleToChargeId, unknownModeId, chargeModeId, [], [], false) ],
                                                           []),
                        Throws.ArgumentException);

            Assert.That(() => new DDBC_ActuatorDescription(actuatorId,
                                                           [ Commodity.Electricity ],
                                                           [ IdleMode(), ChargeMode() ],
                                                           [ new Transition(idleToChargeId, idleModeId, unknownModeId, [], [], false) ],
                                                           []),
                        Throws.ArgumentException);

            var json = EVChargerActuator().ToJSON();
            json["transitions"]![0]!["from"] = unknownModeId.ToString();

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("switches from an unknown operation mode").And.Contain(unknownModeId.ToString()));

            json = EVChargerActuator().ToJSON();
            json["transitions"]![0]!["to"] = unknownModeId.ToString();

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("switches to an unknown operation mode").And.Contain(unknownModeId.ToString()));

        }

        [Test]
        [S2C("Rules.DDBC.ActuatorDescription.TransitionTimerReferences")]
        public void ActuatorDescription_RejectsTransitionsWithUnknownTimers()
        {

            var unknownTimerId = Timer_Id.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee");

            Assert.That(() => new DDBC_ActuatorDescription(actuatorId,
                                                           [ Commodity.Electricity ],
                                                           [ IdleMode(), ChargeMode() ],
                                                           [ new Transition(idleToChargeId, idleModeId, chargeModeId, [ unknownTimerId ], [], false) ],
                                                           [ MinimumOnTimer() ]),
                        Throws.ArgumentException);

            Assert.That(() => new DDBC_ActuatorDescription(actuatorId,
                                                           [ Commodity.Electricity ],
                                                           [ IdleMode(), ChargeMode() ],
                                                           [ new Transition(idleToChargeId, idleModeId, chargeModeId, [], [ unknownTimerId ], false) ],
                                                           [ MinimumOnTimer() ]),
                        Throws.ArgumentException);

            // Transitions referencing timers, but no timers declared at all.
            Assert.That(() => new DDBC_ActuatorDescription(actuatorId,
                                                           [ Commodity.Electricity ],
                                                           [ IdleMode(), ChargeMode() ],
                                                           [ IdleToCharge(), ChargeToIdle() ],
                                                           []),
                        Throws.ArgumentException);

            var json = EVChargerActuator().ToJSON();
            json["transitions"]![0]!["start_timers"] = new JArray(unknownTimerId.ToString());

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("unknown start timer").And.Contain(unknownTimerId.ToString()));

            json = EVChargerActuator().ToJSON();
            json["transitions"]![1]!["blocking_timers"] = new JArray(unknownTimerId.ToString());

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("unknown blocking timer").And.Contain(unknownTimerId.ToString()));

        }

        [Test]
        [S2C("Rules.DDBC.ActuatorDescription.PowerRangePerSupportedCommodity")]
        public void ActuatorDescription_RejectsOperationModesWithoutPowerRangeForSupportedCommodity()
        {

            // Electricity and gas are supported, but the operation modes only carry electric power ranges.
            Assert.That(() => new DDBC_ActuatorDescription(actuatorId,
                                                           [ Commodity.Electricity, Commodity.Gas ],
                                                           [ IdleMode(), ChargeMode() ],
                                                           [],
                                                           []),
                        Throws.ArgumentException);

            var json = EVChargerActuator().ToJSON();
            json["supported_commodites"] = new JArray("ELECTRICITY", "GAS");

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("has no power range for the supported commodity").And.Contain("GAS"));

        }

        [Test]
        [S2C("Rules.DDBC.ActuatorDescription.PowerRangePerSupportedCommodity")]
        public void ActuatorDescription_RejectsOperationModesWithPowerRangeForUnsupportedCommodity()
        {

            var electricAndGasMode = new DDBC_OperationMode(chargeModeId,
                                                            [
                                                                ElectricPowerRange(),
                                                                new PowerRange(0, 0.5, CommodityQuantity.NaturalGasFlowRate)
                                                            ],
                                                            new NumberRange(0, 100),
                                                            false);

            // Only electricity is supported, but the operation mode also carries a gas flow rate range.
            Assert.That(() => new DDBC_ActuatorDescription(actuatorId,
                                                           [ Commodity.Electricity ],
                                                           [ electricAndGasMode ],
                                                           [],
                                                           []),
                        Throws.ArgumentException);

            // With both commodities supported the same operation mode is fine.
            Assert.That(() => new DDBC_ActuatorDescription(actuatorId,
                                                           [ Commodity.Electricity, Commodity.Gas ],
                                                           [ electricAndGasMode ],
                                                           [],
                                                           []),
                        Throws.Nothing);

            var json = new DDBC_ActuatorDescription(actuatorId,
                                                    [ Commodity.Electricity, Commodity.Gas ],
                                                    [ electricAndGasMode ],
                                                    [],
                                                    []).ToJSON();

            json["supported_commodites"] = new JArray("ELECTRICITY");

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("which is not a supported commodity").And.Contain("NATURAL_GAS.FLOW_RATE"));

        }

        [Test]
        [S2C("Rules.DDBC.ActuatorDescription.PowerRangePerSupportedCommodity")]
        public void ActuatorDescription_AcceptsThreePhasePowerRangesForOneCommodity()
        {

            // Three commodity quantities of the same commodity (L1, L2, L3) in one operation mode.
            var threePhaseMode = new DDBC_OperationMode(chargeModeId,
                                                        [
                                                            new PowerRange(0, 3700, CommodityQuantity.ElectricPowerL1),
                                                            new PowerRange(0, 3700, CommodityQuantity.ElectricPowerL2),
                                                            new PowerRange(0, 3700, CommodityQuantity.ElectricPowerL3)
                                                        ],
                                                        new NumberRange(0, 100),
                                                        false);

            var actuator = new DDBC_ActuatorDescription(actuatorId,
                                                        [ Commodity.Electricity ],
                                                        [ threePhaseMode ],
                                                        [],
                                                        []);

            var json = actuator.ToJSON();
            S2SchemaValidator.AssertValidType(json, "DDBC.ActuatorDescription");

            Assert.That(DDBC_ActuatorDescription.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed, Is.EqualTo(actuator));

        }

        #endregion

        #region DDBC_AverageDemandRateForecastElement

        [Test]
        public void AverageDemandRateForecastElement_RoundTrips_AndIsSchemaValid()
        {

            var element = FullForecastElement();
            var json    = element.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] {
                            "duration",
                            "demand_rate_upper_limit",
                            "demand_rate_upper_95PPR",
                            "demand_rate_upper_68PPR",
                            "demand_rate_expected",
                            "demand_rate_lower_68PPR",
                            "demand_rate_lower_95PPR",
                            "demand_rate_lower_limit"
                        }));

            Assert.That(json["duration"]?.Value<Int64>(), Is.EqualTo(900000L));

            S2SchemaValidator.AssertValidType(json, "DDBC.AverageDemandRateForecastElement");

            Assert.That(DDBC_AverageDemandRateForecastElement.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                        Is.EqualTo(element));
            Assert.That(parsed!.GetHashCode(),         Is.EqualTo(element.GetHashCode()));
            Assert.That(parsed.Duration,               Is.EqualTo(Duration.FromMilliseconds(900000)));
            Assert.That(parsed.DemandRateExpected,     Is.EqualTo(0.003));
            Assert.That(parsed.DemandRateUpperLimit,   Is.EqualTo(0.0051));
            Assert.That(parsed.DemandRateUpper95PPR,   Is.EqualTo(0.0045));
            Assert.That(parsed.DemandRateUpper68PPR,   Is.EqualTo(0.0035));
            Assert.That(parsed.DemandRateLower68PPR,   Is.EqualTo(0.0025));
            Assert.That(parsed.DemandRateLower95PPR,   Is.EqualTo(0.0015));
            Assert.That(parsed.DemandRateLowerLimit,   Is.EqualTo(0.00065));

            Assert.That(element.Clone(),               Is.EqualTo(element));
            Assert.That(element == parsed,             Is.True);
            Assert.That(element.ToString(),            Does.Contain("900000 ms"));

        }

        [Test]
        public void AverageDemandRateForecastElement_MandatoryOnly_OmitsOptionalProperties()
        {

            var element = new DDBC_AverageDemandRateForecastElement(Duration.FromMilliseconds(900000), 0.003);
            var json    = element.ToJSON();

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] { "duration", "demand_rate_expected" }));

            S2SchemaValidator.AssertValidType(json, "DDBC.AverageDemandRateForecastElement");

            Assert.That(DDBC_AverageDemandRateForecastElement.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                       Is.EqualTo(element));
            Assert.That(parsed!.DemandRateUpperLimit, Is.Null);
            Assert.That(parsed.DemandRateUpper95PPR,  Is.Null);
            Assert.That(parsed.DemandRateUpper68PPR,  Is.Null);
            Assert.That(parsed.DemandRateLower68PPR,  Is.Null);
            Assert.That(parsed.DemandRateLower95PPR,  Is.Null);
            Assert.That(parsed.DemandRateLowerLimit,  Is.Null);
            Assert.That(element,                      Is.Not.EqualTo(FullForecastElement()));

        }

        [Test]
        public void AverageDemandRateForecastElement_MissingMandatoryProperty_Fails()
        {

            var withoutExpected = JObject.Parse("""{ "duration": 900000, "demand_rate_upper_limit": 0.0051 }""");
            Assert.That(DDBC_AverageDemandRateForecastElement.TryParse(withoutExpected, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("demand_rate_expected"));

            var withoutDuration = JObject.Parse("""{ "demand_rate_expected": 0.003 }""");
            Assert.That(DDBC_AverageDemandRateForecastElement.TryParse(withoutDuration, out _, out error), Is.False);
            Assert.That(error, Does.Contain("duration"));

            var negativeDuration = JObject.Parse("""{ "duration": -1, "demand_rate_expected": 0.003 }""");
            Assert.That(DDBC_AverageDemandRateForecastElement.TryParse(negativeDuration, out _, out error), Is.False);
            Assert.That(error, Does.Contain("duration"));

            var textualRate = JObject.Parse("""{ "duration": 900000, "demand_rate_expected": "0.003" }""");
            Assert.That(DDBC_AverageDemandRateForecastElement.TryParse(textualRate, out _, out error), Is.False);
            Assert.That(error, Does.Contain("demand_rate_expected"));

        }

        [Test]
        public void AverageDemandRateForecastElement_Strict_RejectsAdditionalProperty()
        {

            var json = JObject.Parse("""{ "duration": 900000, "demand_rate_expected": 0.003, "demand_rate_upper_95ppr": 0.0045 }""");

            Assert.That(DDBC_AverageDemandRateForecastElement.TryParse(json, out var parsed, out _, S2ParserOptions.Default), Is.True);
            Assert.That(parsed!.DemandRateUpper95PPR, Is.Null, "Property names are case sensitive!");

            Assert.That(DDBC_AverageDemandRateForecastElement.TryParse(json, out _, out var error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("demand_rate_upper_95ppr"));

        }

        [Test]
        public void AverageDemandRateForecastElement_RejectsNaN()
        {

            Assert.That(() => new DDBC_AverageDemandRateForecastElement(Duration.Zero, Double.NaN),                             Throws.ArgumentException);
            Assert.That(() => new DDBC_AverageDemandRateForecastElement(Duration.Zero, 0.003, DemandRateUpperLimit: Double.NaN), Throws.ArgumentException);
            Assert.That(() => new DDBC_AverageDemandRateForecastElement(Duration.Zero, 0.003, DemandRateLowerLimit: Double.NaN), Throws.ArgumentException);

        }

        #endregion

    }

}

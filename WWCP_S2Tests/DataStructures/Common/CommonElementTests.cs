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

namespace cloud.charging.open.protocols.S2.Tests.DataStructures.Common
{

    /// <summary>
    /// Tests of the common S2 data structure elements: PowerRange, PowerValue,
    /// PowerForecastValue, PowerForecastElement, Role and Transition, plus the
    /// CommodityQuantity → Commodity mapping.
    /// </summary>
    [TestFixture]
    public sealed class CommonElementTests
    {

        #region PowerRange

        [Test]
        public void PowerRange_RoundTrips_AndIsSchemaValid()
        {

            // EV charger FRBC example: 1400 .. 11000 W, three-phase symmetric.
            var powerRange = new PowerRange(1400, 11000, CommodityQuantity.ElectricPower3PhaseSymmetric);
            var json       = powerRange.ToJSON();

            S2SchemaValidator.AssertValidType(json, "PowerRange");

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] { "start_of_range", "end_of_range", "commodity_quantity" }));
            Assert.That(json["commodity_quantity"]?.Value<String>(), Is.EqualTo("ELECTRIC.POWER.3_PHASE_SYMMETRIC"));

            Assert.That(PowerRange.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                     Is.EqualTo(powerRange));
            Assert.That(parsed!.GetHashCode(),      Is.EqualTo(powerRange.GetHashCode()));
            Assert.That(parsed.Clone(),             Is.EqualTo(powerRange));
            Assert.That(parsed.Contains(5000),      Is.True);
            Assert.That(parsed.Contains(11001),     Is.False);
            Assert.That(parsed.ToString(),          Does.Contain("1400").And.Contain("11000"));

            Assert.That(PowerRange.TryParse(json, out var strict, out error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(strict, Is.EqualTo(powerRange));

        }

        [Test]
        public void PowerRange_MissingMandatoryProperty_Fails()
        {

            var json = JObject.Parse("""{ "start_of_range": 1400, "end_of_range": 11000 }""");

            Assert.That(PowerRange.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("commodity_quantity"));

        }

        [Test]
        public void PowerRange_Strict_RejectsAdditionalProperty()
        {

            var json = JObject.Parse("""{ "start_of_range": 1400, "end_of_range": 11000, "commodity_quantity": "ELECTRIC.POWER.L1", "foo": 1 }""");

            Assert.That(PowerRange.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PowerRange.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void PowerRange_RejectsStartGreaterThanEnd()
        {

            Assert.That(() => new PowerRange(11000, 1400, CommodityQuantity.ElectricPowerL1), Throws.ArgumentException);

            var json = JObject.Parse("""{ "start_of_range": 11000, "end_of_range": 1400, "commodity_quantity": "ELECTRIC.POWER.L1" }""");

            Assert.That(PowerRange.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("must not be greater"));

        }

        [Test]
        public void PowerRange_RejectsUnknownCommodityQuantity_ByDefault()
        {

            var json = JObject.Parse("""{ "start_of_range": 0, "end_of_range": 1, "commodity_quantity": "ELECTRIC.POWER.L4" }""");

            Assert.That(PowerRange.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("not a known value"));

            var lenient = new S2ParserOptions { RejectUnknownEnumValues = false };
            Assert.That(PowerRange.TryParse(json, out var parsed, out error, lenient), Is.True, error);
            Assert.That(parsed!.CommodityQuantity.IsKnown, Is.False);

            Assert.That(() => new PowerRange(Double.NaN, 1, CommodityQuantity.ElectricPowerL1), Throws.ArgumentException);
            Assert.That(() => new PowerRange(0, 1, default),                                     Throws.ArgumentException);

        }

        #endregion

        #region PowerValue

        [Test]
        public void PowerValue_RoundTrips_AndIsSchemaValid()
        {

            var powerValue = new PowerValue(CommodityQuantity.ElectricPowerL1, 3680);
            var json       = powerValue.ToJSON();

            S2SchemaValidator.AssertValidType(json, "PowerValue");

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] { "commodity_quantity", "value" }));

            Assert.That(PowerValue.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                 Is.EqualTo(powerValue));
            Assert.That(parsed!.GetHashCode(),  Is.EqualTo(powerValue.GetHashCode()));
            Assert.That(parsed.Clone(),         Is.EqualTo(powerValue));
            Assert.That(parsed.Value,           Is.EqualTo(3680));

            Assert.That(PowerValue.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

        }

        [Test]
        public void PowerValue_MissingMandatoryProperty_Fails()
        {

            var json = JObject.Parse("""{ "commodity_quantity": "ELECTRIC.POWER.L1" }""");

            Assert.That(PowerValue.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("value"));

        }

        [Test]
        public void PowerValue_Strict_RejectsAdditionalProperty()
        {

            var json = JObject.Parse("""{ "commodity_quantity": "ELECTRIC.POWER.L1", "value": 3680, "foo": 1 }""");

            Assert.That(PowerValue.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PowerValue.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void PowerValue_RejectsNaN_AndEmptyCommodityQuantity()
        {
            Assert.That(() => new PowerValue(CommodityQuantity.ElectricPowerL1, Double.NaN),              Throws.ArgumentException);
            Assert.That(() => new PowerValue(CommodityQuantity.ElectricPowerL1, Double.PositiveInfinity), Throws.ArgumentException);
            Assert.That(() => new PowerValue(default,                            1),                      Throws.ArgumentException);
        }

        #endregion

        #region PowerForecastValue

        [Test]
        public void PowerForecastValue_RoundTrips_AndIsSchemaValid()
        {

            var forecastValue = new PowerForecastValue(
                                    ValueExpected:      7000,
                                    CommodityQuantity:  CommodityQuantity.ElectricPower3PhaseSymmetric,
                                    ValueUpperLimit:    11000,
                                    ValueUpper95PPR:    9000,
                                    ValueUpper68PPR:    8000,
                                    ValueLower68PPR:    6000,
                                    ValueLower95PPR:    5000,
                                    ValueLowerLimit:    1400
                                );

            var json = forecastValue.ToJSON();

            S2SchemaValidator.AssertValidType(json, "PowerForecastValue");

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] {
                "value_upper_limit", "value_upper_95PPR", "value_upper_68PPR", "value_expected",
                "value_lower_68PPR", "value_lower_95PPR", "value_lower_limit", "commodity_quantity"
            }));

            Assert.That(PowerForecastValue.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                      Is.EqualTo(forecastValue));
            Assert.That(parsed!.GetHashCode(),       Is.EqualTo(forecastValue.GetHashCode()));
            Assert.That(parsed.Clone(),              Is.EqualTo(forecastValue));
            Assert.That(parsed.ValueUpper95PPR,      Is.EqualTo(9000));
            Assert.That(parsed.ValueLowerLimit,      Is.EqualTo(1400));

            Assert.That(PowerForecastValue.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

        }

        [Test]
        public void PowerForecastValue_MandatoryOnly_OmitsOptionalProperties()
        {

            var forecastValue = new PowerForecastValue(7000, CommodityQuantity.ElectricPowerL1);
            var json          = forecastValue.ToJSON();

            S2SchemaValidator.AssertValidType(json, "PowerForecastValue");

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] { "value_expected", "commodity_quantity" }));

            Assert.That(PowerForecastValue.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                   Is.EqualTo(forecastValue));
            Assert.That(parsed!.ValueUpperLimit,  Is.Null);
            Assert.That(parsed. ValueLower68PPR,  Is.Null);

            Assert.That(parsed, Is.Not.EqualTo(new PowerForecastValue(7000, CommodityQuantity.ElectricPowerL1, ValueUpperLimit: 8000)));

        }

        [Test]
        public void PowerForecastValue_MissingMandatoryProperty_Fails()
        {

            var json = JObject.Parse("""{ "value_upper_limit": 11000, "commodity_quantity": "ELECTRIC.POWER.L1" }""");

            Assert.That(PowerForecastValue.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("value_expected"));

            var invalidOptional = JObject.Parse("""{ "value_expected": 7000, "value_upper_limit": "high", "commodity_quantity": "ELECTRIC.POWER.L1" }""");

            Assert.That(PowerForecastValue.TryParse(invalidOptional, out _, out error), Is.False);
            Assert.That(error, Does.Contain("value_upper_limit"));

        }

        [Test]
        public void PowerForecastValue_Strict_RejectsAdditionalProperty()
        {

            var json = JObject.Parse("""{ "value_expected": 7000, "commodity_quantity": "ELECTRIC.POWER.L1", "foo": 1 }""");

            Assert.That(PowerForecastValue.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PowerForecastValue.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void PowerForecastValue_RejectsNaN()
        {
            Assert.That(() => new PowerForecastValue(Double.NaN, CommodityQuantity.ElectricPowerL1),                              Throws.ArgumentException);
            Assert.That(() => new PowerForecastValue(7000,       CommodityQuantity.ElectricPowerL1, ValueLowerLimit: Double.NaN), Throws.ArgumentException);
            Assert.That(() => new PowerForecastValue(7000,       default),                                                        Throws.ArgumentException);
        }

        #endregion

        #region PowerForecastElement

        [Test]
        public void PowerForecastElement_RoundTrips_AndIsSchemaValid()
        {

            var element = new PowerForecastElement(
                              Duration.FromMilliseconds(900000),
                              [
                                  new PowerForecastValue(3680, CommodityQuantity.ElectricPowerL1, ValueUpperLimit: 3680, ValueLowerLimit: 0),
                                  new PowerForecastValue(3680, CommodityQuantity.ElectricPowerL2, ValueUpperLimit: 3680, ValueLowerLimit: 0),
                                  new PowerForecastValue(3680, CommodityQuantity.ElectricPowerL3, ValueUpperLimit: 3680, ValueLowerLimit: 0)
                              ]
                          );

            var json = element.ToJSON();

            S2SchemaValidator.AssertValidType(json, "PowerForecastElement");

            Assert.That(json.Properties().Select(p => p.Name),  Is.EqualTo(new[] { "duration", "power_values" }));
            Assert.That(json["duration"]?.Value<Int64>(),       Is.EqualTo(900000));
            Assert.That(json["power_values"],                   Has.Count.EqualTo(3));

            Assert.That(PowerForecastElement.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                       Is.EqualTo(element));
            Assert.That(parsed!.GetHashCode(),                        Is.EqualTo(element.GetHashCode()));
            Assert.That(parsed.Clone(),                               Is.EqualTo(element));
            Assert.That(parsed.PowerValues[1].CommodityQuantity,      Is.EqualTo(CommodityQuantity.ElectricPowerL2));

            Assert.That(PowerForecastElement.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

            // Order matters: the same values in another order are a different element.
            var reordered = new PowerForecastElement(element.Duration, [.. element.PowerValues.Reverse()]);
            Assert.That(reordered, Is.Not.EqualTo(element));

        }

        [Test]
        public void PowerForecastElement_MissingMandatoryProperty_Fails()
        {

            var json = JObject.Parse("""{ "duration": 900000 }""");

            Assert.That(PowerForecastElement.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("power_values"));

            var noDuration = JObject.Parse("""{ "power_values": [ { "value_expected": 3680, "commodity_quantity": "ELECTRIC.POWER.L1" } ] }""");

            Assert.That(PowerForecastElement.TryParse(noDuration, out _, out error), Is.False);
            Assert.That(error, Does.Contain("duration"));

        }

        [Test]
        public void PowerForecastElement_Strict_RejectsAdditionalProperty()
        {

            var json = JObject.Parse("""{ "duration": 900000, "power_values": [ { "value_expected": 3680, "commodity_quantity": "ELECTRIC.POWER.L1" } ], "foo": 1 }""");

            Assert.That(PowerForecastElement.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PowerForecastElement.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

            var nestedExtra = JObject.Parse("""{ "duration": 900000, "power_values": [ { "value_expected": 3680, "commodity_quantity": "ELECTRIC.POWER.L1", "bar": 2 } ] }""");

            Assert.That(PowerForecastElement.TryParse(nestedExtra, out _, out error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("bar"));

        }

        [Test]
        public void PowerForecastElement_RejectsDuplicateCommodityQuantity()
        {

            Assert.That(() => new PowerForecastElement(
                                  Duration.FromSeconds(900),
                                  [
                                      new PowerForecastValue(3680, CommodityQuantity.ElectricPowerL1),
                                      new PowerForecastValue(2000, CommodityQuantity.ElectricPowerL1)
                                  ]
                              ),
                        Throws.ArgumentException);

            var json = JObject.Parse("""
                {
                  "duration": 900000,
                  "power_values": [
                    { "value_expected": 3680, "commodity_quantity": "ELECTRIC.POWER.L1" },
                    { "value_expected": 2000, "commodity_quantity": "ELECTRIC.POWER.L1" }
                  ]
                }
                """);

            Assert.That(PowerForecastElement.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at most one").And.Contain("ELECTRIC.POWER.L1"));

        }

        [Test]
        public void PowerForecastElement_EnforcesMinAndMaxItems()
        {

            Assert.That(() => new PowerForecastElement(Duration.Zero, []), Throws.ArgumentException);

            var empty = JObject.Parse("""{ "duration": 900000, "power_values": [] }""");

            Assert.That(PowerForecastElement.TryParse(empty, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at least 1"));

            var tooMany = new JObject(
                              new JProperty("duration",      900000),
                              new JProperty("power_values",  new JArray(
                                  Enumerable.Range(0, 11).Select(i => new JObject(
                                      new JProperty("value_expected",      i),
                                      new JProperty("commodity_quantity",  "ELECTRIC.POWER.L1")
                                  ))
                              ))
                          );

            Assert.That(PowerForecastElement.TryParse(tooMany, out _, out error), Is.False);
            Assert.That(error, Does.Contain("at most 10"));

        }

        #endregion

        #region Role

        [Test]
        public void Role_RoundTrips_AndIsSchemaValid()
        {

            var role = new Role(RoleType.EnergyConsumer, Commodity.Electricity);
            var json = role.ToJSON();

            S2SchemaValidator.AssertValidType(json, "Role");

            Assert.That(json.Properties().Select(p => p.Name),  Is.EqualTo(new[] { "role", "commodity" }));
            Assert.That(json["role"]?.Value<String>(),          Is.EqualTo("ENERGY_CONSUMER"));
            Assert.That(json["commodity"]?.Value<String>(),     Is.EqualTo("ELECTRICITY"));

            Assert.That(Role.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                Is.EqualTo(role));
            Assert.That(parsed!.GetHashCode(), Is.EqualTo(role.GetHashCode()));
            Assert.That(parsed.Clone(),        Is.EqualTo(role));
            Assert.That(parsed.ToString(),     Is.EqualTo("ENERGY_CONSUMER of ELECTRICITY"));

            Assert.That(Role.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

            Assert.That(parsed, Is.Not.EqualTo(new Role(RoleType.EnergyStorage, Commodity.Electricity)));

        }

        [Test]
        public void Role_MissingMandatoryProperty_Fails()
        {

            var json = JObject.Parse("""{ "role": "ENERGY_CONSUMER" }""");

            Assert.That(Role.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("commodity"));

            var noRole = JObject.Parse("""{ "commodity": "ELECTRICITY" }""");

            Assert.That(Role.TryParse(noRole, out _, out error), Is.False);
            Assert.That(error, Does.Contain("role"));

        }

        [Test]
        public void Role_Strict_RejectsAdditionalProperty()
        {

            var json = JObject.Parse("""{ "role": "ENERGY_CONSUMER", "commodity": "ELECTRICITY", "foo": 1 }""");

            Assert.That(Role.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(Role.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void Role_RejectsUnknownEnumValues_ByDefault()
        {

            var json = JObject.Parse("""{ "role": "ENERGY_PROSUMER", "commodity": "ELECTRICITY" }""");

            Assert.That(Role.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("not a known value"));

            Assert.That(() => new Role(default,                  Commodity.Electricity), Throws.ArgumentException);
            Assert.That(() => new Role(RoleType.EnergyConsumer,  default),               Throws.ArgumentException);

        }

        #endregion

        #region Transition

        [Test]
        public void Transition_RoundTrips_AndIsSchemaValid()
        {

            // EV charger FRBC example: transition duration 3000 ms.
            var transition = new Transition(
                                 Transition_Id.   Parse("transition1"),
                                 OperationMode_Id.Parse("omode_idle"),
                                 OperationMode_Id.Parse("omode_charging"),
                                 [ Timer_Id.Parse("timer0") ],
                                 [ Timer_Id.Parse("timer1"), Timer_Id.Parse("timer2") ],
                                 AbnormalConditionOnly:  false,
                                 TransitionCosts:        0.5,
                                 TransitionDuration:     Duration.FromMilliseconds(3000)
                             );

            var json = transition.ToJSON();

            S2SchemaValidator.AssertValidType(json, "Transition");

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] {
                "id", "from", "to", "start_timers", "blocking_timers", "transition_costs", "transition_duration", "abnormal_condition_only"
            }));
            Assert.That(json["transition_duration"]?.Value<Int64>(),  Is.EqualTo(3000));
            Assert.That(json["blocking_timers"],                      Has.Count.EqualTo(2));

            Assert.That(Transition.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                        Is.EqualTo(transition));
            Assert.That(parsed!.GetHashCode(),         Is.EqualTo(transition.GetHashCode()));
            Assert.That(parsed.Clone(),                Is.EqualTo(transition));
            Assert.That(parsed.From.ToString(),        Is.EqualTo("omode_idle"));
            Assert.That(parsed.To.  ToString(),        Is.EqualTo("omode_charging"));
            Assert.That(parsed.StartTimers,            Is.EqualTo(new[] { Timer_Id.Parse("timer0") }));
            Assert.That(parsed.BlockingTimers,         Has.Count.EqualTo(2));
            Assert.That(parsed.TransitionCosts,        Is.EqualTo(0.5));
            Assert.That(parsed.TransitionDuration,     Is.EqualTo(Duration.FromMilliseconds(3000)));
            Assert.That(parsed.AbnormalConditionOnly,  Is.False);
            Assert.That(parsed.ToString(),             Does.Contain("omode_idle -> omode_charging"));

        }

        [Test]
        public void Transition_MandatoryOnly_OmitsOptionalProperties()
        {

            var transition = new Transition(
                                 Transition_Id.   NewRandom,
                                 OperationMode_Id.NewRandom,
                                 OperationMode_Id.NewRandom,
                                 [],
                                 [],
                                 AbnormalConditionOnly: true
                             );

            var json = transition.ToJSON();

            S2SchemaValidator.AssertValidType(json, "Transition");

            Assert.That(json.ContainsKey("transition_costs"),     Is.False);
            Assert.That(json.ContainsKey("transition_duration"),  Is.False);
            Assert.That(json["start_timers"],                     Is.Empty);
            Assert.That(json["blocking_timers"],                  Is.Empty);

            // UUID identifiers: also valid under the strict options.
            Assert.That(Transition.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                        Is.EqualTo(transition));
            Assert.That(parsed!.TransitionCosts,       Is.Null);
            Assert.That(parsed. TransitionDuration,    Is.Null);
            Assert.That(parsed. AbnormalConditionOnly, Is.True);

        }

        [Test]
        public void Transition_MissingMandatoryProperty_Fails()
        {

            var json = JObject.Parse("""
                {
                  "id":                      "transition1",
                  "from":                    "omode_idle",
                  "start_timers":            [],
                  "blocking_timers":         [],
                  "abnormal_condition_only": false
                }
                """);

            Assert.That(Transition.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("to"));

            var noTimers = JObject.Parse("""
                {
                  "id":                      "transition1",
                  "from":                    "omode_idle",
                  "to":                      "omode_charging",
                  "blocking_timers":         [],
                  "abnormal_condition_only": false
                }
                """);

            Assert.That(Transition.TryParse(noTimers, out _, out error), Is.False);
            Assert.That(error, Does.Contain("start_timers"));

            var noFlag = JObject.Parse("""
                {
                  "id":                      "transition1",
                  "from":                    "omode_idle",
                  "to":                      "omode_charging",
                  "start_timers":            [],
                  "blocking_timers":         []
                }
                """);

            Assert.That(Transition.TryParse(noFlag, out _, out error), Is.False);
            Assert.That(error, Does.Contain("abnormal_condition_only"));

        }

        [Test]
        public void Transition_Strict_RejectsAdditionalProperty_AndNonUUIDs()
        {

            var json = new Transition(
                           Transition_Id.   NewRandom,
                           OperationMode_Id.NewRandom,
                           OperationMode_Id.NewRandom,
                           [ Timer_Id.NewRandom ],
                           [],
                           AbnormalConditionOnly: false
                       ).ToJSON();

            json["foo"] = 1;

            Assert.That(Transition.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(Transition.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

            var nonUUID = JObject.Parse("""
                {
                  "id":                      "transition1",
                  "from":                    "omode_idle",
                  "to":                      "omode_charging",
                  "start_timers":            [],
                  "blocking_timers":         [],
                  "abnormal_condition_only": false
                }
                """);

            Assert.That(Transition.TryParse(nonUUID, out _, out _,     S2ParserOptions.Default), Is.True);
            Assert.That(Transition.TryParse(nonUUID, out _, out error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("not a UUID"));

        }

        [Test]
        public void Transition_EnforcesMaxTimers_AndRejectsInvalidValues()
        {

            var tooManyTimers = Enumerable.Range(0, 1001).Select(_ => Timer_Id.NewRandom).ToList();

            Assert.That(() => new Transition(Transition_Id.NewRandom, OperationMode_Id.NewRandom, OperationMode_Id.NewRandom, tooManyTimers, [], false), Throws.ArgumentException);
            Assert.That(() => new Transition(Transition_Id.NewRandom, OperationMode_Id.NewRandom, OperationMode_Id.NewRandom, [], tooManyTimers, false), Throws.ArgumentException);

            var json = new JObject(
                           new JProperty("id",                       "transition1"),
                           new JProperty("from",                     "omode_idle"),
                           new JProperty("to",                       "omode_charging"),
                           new JProperty("start_timers",             new JArray(tooManyTimers.Select(id => id.ToString()))),
                           new JProperty("blocking_timers",          new JArray()),
                           new JProperty("abnormal_condition_only",  false)
                       );

            Assert.That(Transition.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at most 1000"));

            var negativeDuration = JObject.Parse("""
                {
                  "id":                      "transition1",
                  "from":                    "omode_idle",
                  "to":                      "omode_charging",
                  "start_timers":            [],
                  "blocking_timers":         [],
                  "transition_duration":     -1,
                  "abnormal_condition_only": false
                }
                """);

            Assert.That(Transition.TryParse(negativeDuration, out _, out error), Is.False);
            Assert.That(error, Does.Contain("transition_duration"));

            Assert.That(() => new Transition(Transition_Id.NewRandom, OperationMode_Id.NewRandom, OperationMode_Id.NewRandom, [], [], false, TransitionCosts: Double.NaN), Throws.ArgumentException);
            Assert.That(() => new Transition(default,                 OperationMode_Id.NewRandom, OperationMode_Id.NewRandom, [], [], false),                              Throws.ArgumentException);

        }

        #endregion

        #region CommodityQuantity → Commodity

        [Test]
        public void CommodityQuantity_MapsOntoCommodity()
        {

            Assert.That(CommodityQuantity.ElectricPowerL1.              Commodity, Is.EqualTo(Commodity.Electricity));
            Assert.That(CommodityQuantity.ElectricPowerL2.              Commodity, Is.EqualTo(Commodity.Electricity));
            Assert.That(CommodityQuantity.ElectricPowerL3.              Commodity, Is.EqualTo(Commodity.Electricity));
            Assert.That(CommodityQuantity.ElectricPower3PhaseSymmetric. Commodity, Is.EqualTo(Commodity.Electricity));
            Assert.That(CommodityQuantity.NaturalGasFlowRate.           Commodity, Is.EqualTo(Commodity.Gas));
            Assert.That(CommodityQuantity.HydrogenFlowRate.             Commodity, Is.EqualTo(Commodity.Gas));
            Assert.That(CommodityQuantity.HeatTemperature.              Commodity, Is.EqualTo(Commodity.Heat));
            Assert.That(CommodityQuantity.HeatFlowRate.                 Commodity, Is.EqualTo(Commodity.Heat));
            Assert.That(CommodityQuantity.HeatThermalPower.             Commodity, Is.EqualTo(Commodity.Heat));
            Assert.That(CommodityQuantity.OilFlowRate.                  Commodity, Is.EqualTo(Commodity.Oil));

            // Every predefined value belongs to a commodity family.
            Assert.That(CommodityQuantity.All.Select(cq => cq.Commodity), Has.All.Not.Null);

            // Parsed values, known or not, are mapped by their family prefix; other texts map to null.
            Assert.That(CommodityQuantity.Parse("ELECTRIC.POWER.L1").Commodity, Is.EqualTo(Commodity.Electricity));
            Assert.That(CommodityQuantity.Parse("HEAT.SOMETHING").   Commodity, Is.EqualTo(Commodity.Heat));
            Assert.That(CommodityQuantity.Parse("WATER.FLOW_RATE").  Commodity, Is.Null);
            Assert.That(default(CommodityQuantity).                  Commodity, Is.Null);

            Assert.That(CommodityQuantity.ElectricPowerL1.Clone().Commodity,    Is.EqualTo(Commodity.Electricity));

        }

        #endregion

    }

}

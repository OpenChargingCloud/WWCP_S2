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

namespace cloud.charging.open.protocols.S2.Tests.DataStructures.FRBC
{

    /// <summary>
    /// Tests of the FRBC operation mode data structures: FRBC_OperationModeElement,
    /// FRBC_OperationMode and FRBC_ActuatorDescription, based on the EV charger example
    /// of the S2 documentation (fill level 0..100 %, charging power 1400..11000 W,
    /// fill rate 0.00065..0.0051 %/s, transition duration 3000 ms).
    /// </summary>
    [TestFixture]
    public sealed class FRBCOperationModeTests
    {

        #region Data

        private static readonly Actuator_Id       actuatorId       = Actuator_Id.     Parse("8a1c3f0e-1a2b-4c3d-8e9f-000000000001");
        private static readonly OperationMode_Id  offId            = OperationMode_Id.Parse("8a1c3f0e-1a2b-4c3d-8e9f-000000000010");
        private static readonly OperationMode_Id  chargingId       = OperationMode_Id.Parse("8a1c3f0e-1a2b-4c3d-8e9f-000000000011");
        private static readonly OperationMode_Id  unknownModeId    = OperationMode_Id.Parse("8a1c3f0e-1a2b-4c3d-8e9f-0000000000ff");
        private static readonly Transition_Id     offToChargingId  = Transition_Id.   Parse("8a1c3f0e-1a2b-4c3d-8e9f-000000000020");
        private static readonly Transition_Id     chargingToOffId  = Transition_Id.   Parse("8a1c3f0e-1a2b-4c3d-8e9f-000000000021");
        private static readonly Timer_Id          minChargingId    = Timer_Id.        Parse("8a1c3f0e-1a2b-4c3d-8e9f-000000000030");
        private static readonly Timer_Id          unknownTimerId   = Timer_Id.        Parse("8a1c3f0e-1a2b-4c3d-8e9f-0000000000fe");

        #endregion

        #region (private static) Fixtures

        /// <summary>
        /// The "Charging" element of the EV charger example with all properties set.
        /// </summary>
        private static FRBC_OperationModeElement ChargingElement(Double  FillLevelStart   = 0,
                                                                 Double  FillLevelEnd     = 100)

            => new (
                   new NumberRange(FillLevelStart, FillLevelEnd),
                   new NumberRange(0.00065, 0.0051),
                   [ new PowerRange(1400, 11000, CommodityQuantity.ElectricPower3PhaseSymmetric) ],
                   new NumberRange(0.0001, 0.0002)
               );

        /// <summary>
        /// The "Off" element of the EV charger example (mandatory properties only).
        /// </summary>
        private static FRBC_OperationModeElement OffElement()

            => new (
                   new NumberRange(0, 100),
                   new NumberRange(0, 0),
                   [ new PowerRange(0, 0, CommodityQuantity.ElectricPower3PhaseSymmetric) ]
               );

        private static FRBC_OperationMode Off()
            => new (offId,      [ OffElement() ],      false);

        private static FRBC_OperationMode Charging()
            => new (chargingId, [ ChargingElement() ], false, "Charging");

        private static Transition OffToCharging(IReadOnlyList<Timer_Id>?  StartTimers      = null,
                                                IReadOnlyList<Timer_Id>?  BlockingTimers   = null)

            => new (offToChargingId,
                    offId,
                    chargingId,
                    StartTimers    ?? [],
                    BlockingTimers ?? [],
                    false,
                    null,
                    Duration.FromMilliseconds(3000));

        private static Transition ChargingToOff(IReadOnlyList<Timer_Id>?  StartTimers      = null,
                                                IReadOnlyList<Timer_Id>?  BlockingTimers   = null)

            => new (chargingToOffId,
                    chargingId,
                    offId,
                    StartTimers    ?? [],
                    BlockingTimers ?? [],
                    false,
                    null,
                    Duration.FromMilliseconds(3000));

        /// <summary>
        /// The EV charger actuator of the S2 documentation: two operation modes (Off, Charging),
        /// two transitions of 3000 ms, no timers.
        /// </summary>
        private static FRBC_ActuatorDescription EVCharger()

            => new (
                   actuatorId,
                   [ Commodity.Electricity ],
                   [ Off(), Charging() ],
                   [ OffToCharging(), ChargingToOff() ],
                   [],
                   "EV charger"
               );

        #endregion


        #region FRBC_OperationModeElement

        [Test]
        public void OperationModeElement_AllProperties_RoundTrips_AndIsSchemaValid()
        {

            var element = ChargingElement();
            var json    = element.ToJSON();

            S2SchemaValidator.AssertValidType(json, "FRBC.OperationModeElement");

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] { "fill_level_range", "fill_rate", "power_ranges", "running_costs" }));
            Assert.That(json["fill_rate"]?["end_of_range"]?.Value<Double>(), Is.EqualTo(0.0051));

            Assert.That(FRBC_OperationModeElement.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                      Is.EqualTo(element));
            Assert.That(parsed!.GetHashCode(),       Is.EqualTo(element.GetHashCode()));
            Assert.That(parsed.Clone(),              Is.EqualTo(element));
            Assert.That(parsed.FillLevelRange,       Is.EqualTo(new NumberRange(0, 100)));
            Assert.That(parsed.PowerRanges.Count,    Is.EqualTo(1));
            Assert.That(parsed.RunningCosts,         Is.EqualTo(new NumberRange(0.0001, 0.0002)));

        }

        [Test]
        public void OperationModeElement_MandatoryOnly_OmitsOptionals_AndIsSchemaValid()
        {

            var element = OffElement();
            var json    = element.ToJSON();

            S2SchemaValidator.AssertValidType(json, "FRBC.OperationModeElement");

            Assert.That(json.ContainsKey("running_costs"), Is.False);

            Assert.That(FRBC_OperationModeElement.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,               Is.EqualTo(element));
            Assert.That(parsed!.RunningCosts, Is.Null);

        }

        [Test]
        public void OperationModeElement_MissingMandatoryProperty_Fails()
        {

            foreach (var property in new[] { "fill_level_range", "fill_rate", "power_ranges" })
            {

                var json = ChargingElement().ToJSON();
                json.Remove(property);

                Assert.That(FRBC_OperationModeElement.TryParse(json, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

        }

        [Test]
        public void OperationModeElement_Strict_RejectsAdditionalProperties()
        {

            var json = ChargingElement().ToJSON();
            json.Add("foo", 1);

            Assert.That(FRBC_OperationModeElement.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_OperationModeElement.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void OperationModeElement_RejectsEmptyFillLevelRange()
        {

            // NumberRange itself allows start == end, the FRBC element requires a strictly smaller start.
            Assert.That(() => ChargingElement(50, 50),
                        Throws.ArgumentException.With.Message.Contains("must be smaller than its end"));

            var json = JObject.Parse("""
                           {
                               "fill_level_range": { "start_of_range": 50, "end_of_range": 50 },
                               "fill_rate":        { "start_of_range": 0.00065, "end_of_range": 0.0051 },
                               "power_ranges":     [ { "start_of_range": 1400, "end_of_range": 11000, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" } ]
                           }
                           """);

            Assert.That(FRBC_OperationModeElement.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("must be smaller than its end"));

        }

        [Test]
        public void OperationModeElement_RejectsDuplicateCommodityQuantities()
        {

            Assert.That(() => new FRBC_OperationModeElement(
                                  new NumberRange(0, 100),
                                  new NumberRange(0.00065, 0.0051),
                                  [
                                      new PowerRange(1400, 11000, CommodityQuantity.ElectricPower3PhaseSymmetric),
                                      new PowerRange(1400, 11000, CommodityQuantity.ElectricPower3PhaseSymmetric)
                                  ]
                              ),
                        Throws.ArgumentException.With.Message.Contains("at most one power range per commodity quantity"));

            var json = JObject.Parse("""
                           {
                               "fill_level_range": { "start_of_range": 0, "end_of_range": 100 },
                               "fill_rate":        { "start_of_range": 0.00065, "end_of_range": 0.0051 },
                               "power_ranges":     [
                                   { "start_of_range": 1400, "end_of_range": 11000, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" },
                                   { "start_of_range": 1400, "end_of_range": 11000, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" }
                               ]
                           }
                           """);

            Assert.That(FRBC_OperationModeElement.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at most one power range per commodity quantity"));

        }

        [Test]
        public void OperationModeElement_RejectsEmptyOrTooManyPowerRanges()
        {

            Assert.That(() => new FRBC_OperationModeElement(new NumberRange(0, 100), new NumberRange(0, 1), []),
                        Throws.ArgumentException.With.Message.Contains("at least one power range"));

            var elevenRanges = Enumerable.Range(0, 11).
                                   Select(_ => new PowerRange(0, 1, CommodityQuantity.ElectricPowerL1)).
                                   ToList();

            Assert.That(() => new FRBC_OperationModeElement(new NumberRange(0, 100), new NumberRange(0, 1), elevenRanges),
                        Throws.ArgumentException.With.Message.Contains("more than 10 power ranges"));

        }

        #endregion

        #region FRBC_OperationMode

        [Test]
        public void OperationMode_AllProperties_RoundTrips_AndIsSchemaValid()
        {

            var operationMode = Charging();
            var json          = operationMode.ToJSON();

            S2SchemaValidator.AssertValidType(json, "FRBC.OperationMode");

            Assert.That(json.Properties().Select(p => p.Name),       Is.EqualTo(new[] { "id", "diagnostic_label", "elements", "abnormal_condition_only" }));
            Assert.That(json["id"]?.Value<String>(),                  Is.EqualTo(chargingId.ToString()));
            Assert.That(json["diagnostic_label"]?.Value<String>(),    Is.EqualTo("Charging"));

            Assert.That(FRBC_OperationMode.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                  Is.EqualTo(operationMode));
            Assert.That(parsed!.GetHashCode(),   Is.EqualTo(operationMode.GetHashCode()));
            Assert.That(parsed.Clone(),          Is.EqualTo(operationMode));
            Assert.That(parsed.Elements.Count,   Is.EqualTo(1));
            Assert.That(parsed.ToString(),       Does.Contain("Charging"));

        }

        [Test]
        public void OperationMode_MandatoryOnly_OmitsOptionals_AndIsSchemaValid()
        {

            var operationMode = Off();
            var json          = operationMode.ToJSON();

            S2SchemaValidator.AssertValidType(json, "FRBC.OperationMode");

            Assert.That(json.ContainsKey("diagnostic_label"), Is.False);

            Assert.That(FRBC_OperationMode.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                  Is.EqualTo(operationMode));
            Assert.That(parsed!.DiagnosticLabel, Is.Null);

        }

        [Test]
        public void OperationMode_MissingMandatoryProperty_Fails()
        {

            foreach (var property in new[] { "id", "elements", "abnormal_condition_only" })
            {

                var json = Charging().ToJSON();
                json.Remove(property);

                Assert.That(FRBC_OperationMode.TryParse(json, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

        }

        [Test]
        public void OperationMode_Strict_RejectsAdditionalProperties()
        {

            var json = Charging().ToJSON();
            json.Add("foo", 1);

            Assert.That(FRBC_OperationMode.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_OperationMode.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void OperationMode_AcceptsContiguousElements_InAnyOrder()
        {

            // Two elements given in reverse order: contiguity is checked after sorting by start_of_range.
            var operationMode = new FRBC_OperationMode(
                                    chargingId,
                                    [ ChargingElement(50, 100), ChargingElement(0, 50) ],
                                    false
                                );

            Assert.That(operationMode.Elements.Count, Is.EqualTo(2));
            Assert.That(operationMode.Elements[0].FillLevelRange.StartOfRange, Is.EqualTo(50));

            var json = operationMode.ToJSON();
            S2SchemaValidator.AssertValidType(json, "FRBC.OperationMode");

            Assert.That(FRBC_OperationMode.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed, Is.EqualTo(operationMode));

        }

        [Test]
        public void OperationMode_RejectsNonContiguousElements()
        {

            Assert.That(() => new FRBC_OperationMode(
                                  chargingId,
                                  [ ChargingElement(0, 50), ChargingElement(60, 100) ],
                                  false
                              ),
                        Throws.ArgumentException.With.Message.Contains("must be contiguous"));

            // Overlapping ranges are not contiguous either.
            Assert.That(() => new FRBC_OperationMode(
                                  chargingId,
                                  [ ChargingElement(0, 60), ChargingElement(50, 100) ],
                                  false
                              ),
                        Throws.ArgumentException.With.Message.Contains("must be contiguous"));

            var json = new JObject(
                           new JProperty("id",                       chargingId.ToString()),
                           new JProperty("elements",                 new JArray(ChargingElement(0, 50).ToJSON(),
                                                                                ChargingElement(60, 100).ToJSON())),
                           new JProperty("abnormal_condition_only",  false)
                       );

            Assert.That(FRBC_OperationMode.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("must be contiguous"));

        }

        [Test]
        public void OperationMode_RejectsEmptyElements()
        {

            Assert.That(() => new FRBC_OperationMode(chargingId, [], false),
                        Throws.ArgumentException.With.Message.Contains("at least one element"));

            var json = JObject.Parse("""
                           {
                               "id": "8a1c3f0e-1a2b-4c3d-8e9f-000000000011",
                               "elements": [],
                               "abnormal_condition_only": false
                           }
                           """);

            Assert.That(FRBC_OperationMode.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("elements"));

        }

        #endregion

        #region FRBC_ActuatorDescription

        [Test]
        public void ActuatorDescription_EVCharger_RoundTrips_AndIsSchemaValid()
        {

            var actuator = EVCharger();
            var json     = actuator.ToJSON();

            S2SchemaValidator.AssertValidType(json, "FRBC.ActuatorDescription");

            Assert.That(json.Properties().Select(p => p.Name),                  Is.EqualTo(new[] { "id", "diagnostic_label", "supported_commodities", "operation_modes", "transitions", "timers" }));
            Assert.That(json["supported_commodities"]?.Select(c => c.Value<String>()), Is.EqualTo(new[] { "ELECTRICITY" }));
            Assert.That(json["operation_modes"]?.Count(),                        Is.EqualTo(2));
            Assert.That(json["transitions"]?.Count(),                            Is.EqualTo(2));
            Assert.That(json["transitions"]?[0]?["transition_duration"]?.Value<Int64>(), Is.EqualTo(3000));
            Assert.That(json["timers"]?.Count(),                                 Is.EqualTo(0));

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                                   Is.EqualTo(actuator));
            Assert.That(parsed!.GetHashCode(),                    Is.EqualTo(actuator.GetHashCode()));
            Assert.That(parsed.Clone(),                           Is.EqualTo(actuator));
            Assert.That(parsed.DiagnosticLabel,                   Is.EqualTo("EV charger"));
            Assert.That(parsed.OperationModes[1].DiagnosticLabel, Is.EqualTo("Charging"));
            Assert.That(parsed.Transitions[0].From,               Is.EqualTo(offId));
            Assert.That(parsed.Transitions[0].To,                 Is.EqualTo(chargingId));
            Assert.That(parsed.Transitions[1].TransitionDuration, Is.EqualTo(Duration.FromMilliseconds(3000)));
            Assert.That(parsed.ToString(),                        Does.Contain("EV charger"));

        }

        [Test]
        public void ActuatorDescription_WithTimers_RoundTrips_AndIsSchemaValid()
        {

            var actuator = new FRBC_ActuatorDescription(
                               actuatorId,
                               [ Commodity.Electricity ],
                               [ Off(), Charging() ],
                               [
                                   OffToCharging(StartTimers:    [ minChargingId ]),
                                   ChargingToOff(BlockingTimers: [ minChargingId ])
                               ],
                               [ new Timer(minChargingId, Duration.FromMilliseconds(120000), "Minimum charging time") ]
                           );

            var json = actuator.ToJSON();

            S2SchemaValidator.AssertValidType(json, "FRBC.ActuatorDescription");

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                                    Is.EqualTo(actuator));
            Assert.That(parsed!.Timers.Count,                      Is.EqualTo(1));
            Assert.That(parsed.Transitions[0].StartTimers,         Is.EqualTo(new[] { minChargingId }));
            Assert.That(parsed.Transitions[1].BlockingTimers,      Is.EqualTo(new[] { minChargingId }));

        }

        [Test]
        public void ActuatorDescription_MandatoryOnly_OmitsOptionals_AndIsSchemaValid()
        {

            var actuator = new FRBC_ActuatorDescription(
                               actuatorId,
                               [ Commodity.Electricity ],
                               [ Off() ],
                               [],
                               []
                           );

            var json = actuator.ToJSON();

            S2SchemaValidator.AssertValidType(json, "FRBC.ActuatorDescription");

            Assert.That(json.ContainsKey("diagnostic_label"),  Is.False);
            Assert.That(json["transitions"]?.Count(),          Is.EqualTo(0));
            Assert.That(json["timers"]?.Count(),               Is.EqualTo(0));

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                  Is.EqualTo(actuator));
            Assert.That(parsed!.DiagnosticLabel, Is.Null);

        }

        [Test]
        public void ActuatorDescription_MissingMandatoryProperty_Fails()
        {

            foreach (var property in new[] { "id", "supported_commodities", "operation_modes", "transitions", "timers" })
            {

                var json = EVCharger().ToJSON();
                json.Remove(property);

                Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

        }

        [Test]
        public void ActuatorDescription_Strict_RejectsAdditionalProperties()
        {

            var json = EVCharger().ToJSON();
            json.Add("foo", 1);

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void ActuatorDescription_RejectsDuplicateSupportedCommodities()
        {

            Assert.That(() => new FRBC_ActuatorDescription(
                                  actuatorId,
                                  [ Commodity.Electricity, Commodity.Electricity ],
                                  [ Off() ],
                                  [],
                                  []
                              ),
                        Throws.ArgumentException.With.Message.Contains("supported commodities").And.Message.Contains("unique"));

            var json = EVCharger().ToJSON();
            (json["supported_commodities"] as JArray)!.Add("ELECTRICITY");

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("supported commodities").And.Contains("unique"));

        }

        [Test]
        public void ActuatorDescription_RejectsEmptySupportedCommodities_OrEmptyOperationModes()
        {

            Assert.That(() => new FRBC_ActuatorDescription(actuatorId, [],                        [ Off() ], [], []),
                        Throws.ArgumentException.With.Message.Contains("at least one supported commodity"));

            Assert.That(() => new FRBC_ActuatorDescription(actuatorId, [ Commodity.Electricity ], [],        [], []),
                        Throws.ArgumentException.With.Message.Contains("at least one operation mode"));

            var json = EVCharger().ToJSON();
            json["supported_commodities"] = new JArray();

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("supported_commodities"));

            json = EVCharger().ToJSON();
            json["operation_modes"] = new JArray();

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("operation_modes"));

        }

        [Test]
        public void ActuatorDescription_RejectsDuplicateOperationModeIds()
        {

            Assert.That(() => new FRBC_ActuatorDescription(
                                  actuatorId,
                                  [ Commodity.Electricity ],
                                  [ Off(), Off() ],
                                  [],
                                  []
                              ),
                        Throws.ArgumentException.With.Message.Contains("operation mode identifications").And.Message.Contains("unique"));

            var json = EVCharger().ToJSON();
            json["operation_modes"]![1]!["id"] = offId.ToString();

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("operation mode identifications").And.Contains("unique"));

        }

        [Test]
        public void ActuatorDescription_RejectsDuplicateTransitionIds()
        {

            var duplicate = new Transition(offToChargingId, chargingId, offId, [], [], false);

            Assert.That(() => new FRBC_ActuatorDescription(
                                  actuatorId,
                                  [ Commodity.Electricity ],
                                  [ Off(), Charging() ],
                                  [ OffToCharging(), duplicate ],
                                  []
                              ),
                        Throws.ArgumentException.With.Message.Contains("transition identifications").And.Message.Contains("unique"));

            var json = EVCharger().ToJSON();
            json["transitions"]![1]!["id"] = offToChargingId.ToString();

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("transition identifications").And.Contains("unique"));

        }

        [Test]
        public void ActuatorDescription_RejectsDuplicateTimerIds()
        {

            Assert.That(() => new FRBC_ActuatorDescription(
                                  actuatorId,
                                  [ Commodity.Electricity ],
                                  [ Off(), Charging() ],
                                  [],
                                  [
                                      new Timer(minChargingId, Duration.FromMilliseconds(120000)),
                                      new Timer(minChargingId, Duration.FromMilliseconds( 60000))
                                  ]
                              ),
                        Throws.ArgumentException.With.Message.Contains("timer identifications").And.Message.Contains("unique"));

            var json = EVCharger().ToJSON();
            json["timers"] = new JArray(
                                 new Timer(minChargingId, Duration.FromMilliseconds(120000)).ToJSON(),
                                 new Timer(minChargingId, Duration.FromMilliseconds( 60000)).ToJSON()
                             );

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("timer identifications").And.Contains("unique"));

        }

        [Test]
        public void ActuatorDescription_RejectsTransitionsToUnknownOperationModes()
        {

            var fromUnknown = new Transition(offToChargingId, unknownModeId, chargingId,    [], [], false);
            var toUnknown   = new Transition(chargingToOffId, chargingId,    unknownModeId, [], [], false);

            Assert.That(() => new FRBC_ActuatorDescription(actuatorId, [ Commodity.Electricity ], [ Off(), Charging() ], [ fromUnknown ], []),
                        Throws.ArgumentException.With.Message.Contains("unknown operation mode").And.Message.Contains("'from'"));

            Assert.That(() => new FRBC_ActuatorDescription(actuatorId, [ Commodity.Electricity ], [ Off(), Charging() ], [ toUnknown ],   []),
                        Throws.ArgumentException.With.Message.Contains("unknown operation mode").And.Message.Contains("'to'"));

            var json = EVCharger().ToJSON();
            json["transitions"]![0]!["from"] = unknownModeId.ToString();

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("unknown operation mode"));

            json = EVCharger().ToJSON();
            json["transitions"]![0]!["to"] = unknownModeId.ToString();

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("unknown operation mode"));

        }

        [Test]
        public void ActuatorDescription_RejectsTransitionsReferencingUnknownTimers()
        {

            Assert.That(() => new FRBC_ActuatorDescription(
                                  actuatorId,
                                  [ Commodity.Electricity ],
                                  [ Off(), Charging() ],
                                  [ OffToCharging(StartTimers: [ unknownTimerId ]) ],
                                  []
                              ),
                        Throws.ArgumentException.With.Message.Contains("unknown timer").And.Message.Contains("start_timers"));

            Assert.That(() => new FRBC_ActuatorDescription(
                                  actuatorId,
                                  [ Commodity.Electricity ],
                                  [ Off(), Charging() ],
                                  [ ChargingToOff(BlockingTimers: [ unknownTimerId ]) ],
                                  [ new Timer(minChargingId, Duration.FromMilliseconds(120000)) ]
                              ),
                        Throws.ArgumentException.With.Message.Contains("unknown timer").And.Message.Contains("blocking_timers"));

            var json = EVCharger().ToJSON();
            (json["transitions"]![0]!["start_timers"] as JArray)!.Add(unknownTimerId.ToString());

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("unknown timer").And.Contains("start_timers"));

            json = EVCharger().ToJSON();
            (json["transitions"]![1]!["blocking_timers"] as JArray)!.Add(unknownTimerId.ToString());

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("unknown timer").And.Contains("blocking_timers"));

        }

        [Test]
        public void ActuatorDescription_RequiresOnePowerRangePerSupportedCommodity()
        {

            // Electricity and heat are supported, but the elements only carry an electric power range.
            Assert.That(() => new FRBC_ActuatorDescription(
                                  actuatorId,
                                  [ Commodity.Electricity, Commodity.Heat ],
                                  [ Off(), Charging() ],
                                  [],
                                  []
                              ),
                        Throws.ArgumentException.With.Message.Contains("at least one power range").And.Message.Contains("HEAT"));

            // With a thermal power range per element, the heat pump is fine.
            var heatPumpElement = new FRBC_OperationModeElement(
                                      new NumberRange(0, 100),
                                      new NumberRange(0, 0.01),
                                      [
                                          new PowerRange( 800, 2400, CommodityQuantity.ElectricPowerL1),
                                          new PowerRange(2500, 9000, CommodityQuantity.HeatThermalPower)
                                      ]
                                  );

            var heatPump = new FRBC_ActuatorDescription(
                               actuatorId,
                               [ Commodity.Electricity, Commodity.Heat ],
                               [ new FRBC_OperationMode(chargingId, [ heatPumpElement ], false) ],
                               [],
                               []
                           );

            var json = heatPump.ToJSON();
            S2SchemaValidator.AssertValidType(json, "FRBC.ActuatorDescription");

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed, Is.EqualTo(heatPump));

            json = EVCharger().ToJSON();
            (json["supported_commodities"] as JArray)!.Add("HEAT");

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("at least one power range").And.Contains("HEAT"));

        }

        [Test]
        public void ActuatorDescription_AcceptsThreePhasePowerRangesForOneCommodity()
        {

            // Three commodity quantities of the same commodity (L1, L2, L3) in one operation mode element;
            // the schema only demands "at most one PowerRange per CommodityQuantity".
            var threePhaseElement = new FRBC_OperationModeElement(
                                        new NumberRange(0, 100),
                                        new NumberRange(0.00065, 0.0051),
                                        [
                                            new PowerRange(0, 3700, CommodityQuantity.ElectricPowerL1),
                                            new PowerRange(0, 3700, CommodityQuantity.ElectricPowerL2),
                                            new PowerRange(0, 3700, CommodityQuantity.ElectricPowerL3)
                                        ]
                                    );

            var actuator = new FRBC_ActuatorDescription(
                               actuatorId,
                               [ Commodity.Electricity ],
                               [ new FRBC_OperationMode(chargingId, [ threePhaseElement ], false) ],
                               [],
                               []
                           );

            var json = actuator.ToJSON();
            S2SchemaValidator.AssertValidType(json, "FRBC.ActuatorDescription");

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed, Is.EqualTo(actuator));

        }

        [Test]
        public void ActuatorDescription_RejectsPowerRangesOfUnsupportedCommodities()
        {

            // Only heat is supported, but the EV charger elements carry electric power ranges.
            Assert.That(() => new FRBC_ActuatorDescription(
                                  actuatorId,
                                  [ Commodity.Heat ],
                                  [ Off(), Charging() ],
                                  [],
                                  []
                              ),
                        Throws.ArgumentException.With.Message.Contains("not a supported commodity"));

            var json = EVCharger().ToJSON();
            json["supported_commodities"] = new JArray("HEAT");

            Assert.That(FRBC_ActuatorDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("not a supported commodity"));

        }

        #endregion

    }

}

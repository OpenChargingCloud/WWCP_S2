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

namespace cloud.charging.open.protocols.S2.Tests.DataStructures.OMBC
{

    /// <summary>
    /// Tests of the OMBC data structures: OMBC_OperationMode.
    /// </summary>
    [TestFixture]
    public sealed class OMBCTypeTests
    {

        #region Data

        private static readonly OperationMode_Id  heatPumpOn   = OperationMode_Id.Parse("2b4d5c3e-0f0a-4a2c-9c6e-000000000001");
        private static readonly OperationMode_Id  heatPumpOff  = OperationMode_Id.Parse("2b4d5c3e-0f0a-4a2c-9c6e-000000000002");

        #endregion

        #region (private static) HeatPumpOn()

        /// <summary>
        /// An OMBC operation mode with all properties set: a heat pump running between 800 W and 2400 W
        /// on ELECTRIC.POWER.L1 and producing 2.5 kW to 9 kW of heat.
        /// </summary>
        private static OMBC_OperationMode HeatPumpOn()

            => new (
                   heatPumpOn,
                   [
                       new PowerRange( 800,  2400, CommodityQuantity.ElectricPowerL1),
                       new PowerRange(2500,  9000, CommodityQuantity.HeatThermalPower)
                   ],
                   false,
                   "Heat pump on",
                   new NumberRange(0.001, 0.002)
               );

        #endregion


        #region OMBCOperationMode_AllProperties_RoundTrips_AndIsSchemaValid()

        [Test]
        public void OMBCOperationMode_AllProperties_RoundTrips_AndIsSchemaValid()
        {

            var operationMode = HeatPumpOn();
            var json          = operationMode.ToJSON();

            S2SchemaValidator.AssertValidType(json, "OMBC.OperationMode");

            Assert.That(json["id"]?.Value<String>(),                           Is.EqualTo(heatPumpOn.ToString()));
            Assert.That(json["diagnostic_label"]?.Value<String>(),             Is.EqualTo("Heat pump on"));
            Assert.That(json["power_ranges"]?.Count(),                         Is.EqualTo(2));
            Assert.That(json["running_costs"]?["start_of_range"]?.Value<Double>(), Is.EqualTo(0.001));
            Assert.That(json["abnormal_condition_only"]?.Value<Boolean>(),     Is.False);

            Assert.That(OMBC_OperationMode.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                                       Is.EqualTo(operationMode));
            Assert.That(parsed!.GetHashCode(),                        Is.EqualTo(operationMode.GetHashCode()));
            Assert.That(parsed.Clone(),                               Is.EqualTo(operationMode));
            Assert.That(parsed.PowerRanges[1].CommodityQuantity,      Is.EqualTo(CommodityQuantity.HeatThermalPower));
            Assert.That(parsed.RunningCosts,                          Is.EqualTo(new NumberRange(0.001, 0.002)));
            Assert.That(parsed.ToString(),                            Does.Contain("Heat pump on"));

        }

        #endregion

        #region OMBCOperationMode_MandatoryOnly_OmitsOptionals_AndIsSchemaValid()

        [Test]
        public void OMBCOperationMode_MandatoryOnly_OmitsOptionals_AndIsSchemaValid()
        {

            var operationMode = new OMBC_OperationMode(
                                    heatPumpOff,
                                    [ new PowerRange(0, 0, CommodityQuantity.ElectricPowerL1) ],
                                    true
                                );

            var json = operationMode.ToJSON();

            S2SchemaValidator.AssertValidType(json, "OMBC.OperationMode");

            Assert.That(json.ContainsKey("diagnostic_label"),  Is.False);
            Assert.That(json.ContainsKey("running_costs"),     Is.False);
            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] { "id", "power_ranges", "abnormal_condition_only" }));

            Assert.That(OMBC_OperationMode.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                        Is.EqualTo(operationMode));
            Assert.That(parsed!.DiagnosticLabel,       Is.Null);
            Assert.That(parsed. RunningCosts,          Is.Null);
            Assert.That(parsed. AbnormalConditionOnly, Is.True);

        }

        #endregion

        #region OMBCOperationMode_MissingMandatoryProperty_Fails()

        [Test]
        public void OMBCOperationMode_MissingMandatoryProperty_Fails()
        {

            foreach (var property in new[] { "id", "power_ranges", "abnormal_condition_only" })
            {

                var json = HeatPumpOn().ToJSON();
                json.Remove(property);

                Assert.That(OMBC_OperationMode.TryParse(json, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));

            }

        }

        #endregion

        #region OMBCOperationMode_Strict_RejectsAdditionalProperties()

        [Test]
        public void OMBCOperationMode_Strict_RejectsAdditionalProperties()
        {

            var json = HeatPumpOn().ToJSON();
            json.Add("foo", 1);

            Assert.That(OMBC_OperationMode.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(OMBC_OperationMode.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        #endregion

        #region OMBCOperationMode_RejectsDuplicateCommodityQuantities()

        [Test]
        public void OMBCOperationMode_RejectsDuplicateCommodityQuantities()
        {

            Assert.That(() => new OMBC_OperationMode(
                                  heatPumpOn,
                                  [
                                      new PowerRange( 800, 2400, CommodityQuantity.ElectricPowerL1),
                                      new PowerRange(1000, 3000, CommodityQuantity.ElectricPowerL1)
                                  ],
                                  false
                              ),
                        Throws.ArgumentException.With.Message.Contains("at most one power range per commodity quantity"));

            var json = JObject.Parse("""
                           {
                               "id": "2b4d5c3e-0f0a-4a2c-9c6e-000000000001",
                               "power_ranges": [
                                   { "start_of_range":  800, "end_of_range": 2400, "commodity_quantity": "ELECTRIC.POWER.L1" },
                                   { "start_of_range": 1000, "end_of_range": 3000, "commodity_quantity": "ELECTRIC.POWER.L1" }
                               ],
                               "abnormal_condition_only": false
                           }
                           """);

            Assert.That(OMBC_OperationMode.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at most one power range per commodity quantity"));

        }

        #endregion

        #region OMBCOperationMode_RejectsEmptyOrTooManyPowerRanges()

        [Test]
        public void OMBCOperationMode_RejectsEmptyOrTooManyPowerRanges()
        {

            Assert.That(() => new OMBC_OperationMode(heatPumpOn, [], false),
                        Throws.ArgumentException.With.Message.Contains("at least one power range"));

            var elevenRanges = Enumerable.Range(0, 11).
                                   Select(_ => new PowerRange(0, 1, CommodityQuantity.ElectricPowerL1)).
                                   ToList();

            Assert.That(() => new OMBC_OperationMode(heatPumpOn, elevenRanges, false),
                        Throws.ArgumentException.With.Message.Contains("more than 10 power ranges"));

            var json = JObject.Parse("""
                           {
                               "id": "2b4d5c3e-0f0a-4a2c-9c6e-000000000001",
                               "power_ranges": [],
                               "abnormal_condition_only": false
                           }
                           """);

            Assert.That(OMBC_OperationMode.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("power_ranges"));

        }

        #endregion

    }

}

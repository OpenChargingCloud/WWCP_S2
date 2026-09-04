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

namespace cloud.charging.open.protocols.S2.Tests.DataStructures.PEBC
{

    /// <summary>
    /// Tests of the Power Envelope Based Control data structures:
    /// PEBC_AllowedLimitRange, PEBC_PowerEnvelopeElement and PEBC_PowerEnvelope.
    /// The values follow the PV inverter example of the S2 documentation: a 3-phase
    /// symmetric PV system whose feed-in (negative power, S2 sign convention) may be
    /// curtailed by the CEM in 15 minute intervals.
    /// </summary>
    [TestFixture]
    public sealed class PEBCTypeTests
    {

        #region Data

        private static readonly Duration  fifteenMinutes  = Duration.FromMilliseconds(900000);

        private static PEBC_PowerEnvelopeElement CreateElement(Double UpperLimit = 0, Double LowerLimit = -3000)
            => new (fifteenMinutes, UpperLimit, LowerLimit);

        #endregion


        #region PEBC_AllowedLimitRange

        [Test]
        public void AllowedLimitRange_RoundTrips_AndIsSchemaValid()
        {

            var allowedLimitRange = new PEBC_AllowedLimitRange(
                                        CommodityQuantity.ElectricPower3PhaseSymmetric,
                                        PEBC_PowerEnvelopeLimitType.LowerLimit,
                                        new NumberRange(-5000, 0),
                                        false
                                    );

            var json = allowedLimitRange.ToJSON();

            Assert.That(json["commodity_quantity"]?.     Value<String>(),  Is.EqualTo("ELECTRIC.POWER.3_PHASE_SYMMETRIC"));
            Assert.That(json["limit_type"]?.             Value<String>(),  Is.EqualTo("LOWER_LIMIT"));
            Assert.That(json["abnormal_condition_only"]?.Value<Boolean>(), Is.False);
            Assert.That(json["range_boundary"],                            Is.TypeOf<JObject>());

            S2SchemaValidator.AssertValidType(json, "PEBC.AllowedLimitRange");

            Assert.That(PEBC_AllowedLimitRange.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                        Is.EqualTo(allowedLimitRange));
            Assert.That(parsed!.GetHashCode(),         Is.EqualTo(allowedLimitRange.GetHashCode()));
            Assert.That(parsed. CommodityQuantity,     Is.EqualTo(CommodityQuantity.ElectricPower3PhaseSymmetric));
            Assert.That(parsed. LimitType,             Is.EqualTo(PEBC_PowerEnvelopeLimitType.LowerLimit));
            Assert.That(parsed. RangeBoundary,         Is.EqualTo(new NumberRange(-5000, 0)));
            Assert.That(parsed. AbnormalConditionOnly, Is.False);

            var clone = allowedLimitRange.Clone();
            Assert.That(clone,                                 Is.EqualTo(allowedLimitRange));
            Assert.That(ReferenceEquals(clone, allowedLimitRange), Is.False);
            Assert.That(ReferenceEquals(clone.RangeBoundary, allowedLimitRange.RangeBoundary), Is.False);

        }

        [Test]
        public void AllowedLimitRange_MandatoryOnly_HasExactlyTheSchemaKeys()
        {

            // All four properties are mandatory; there are no optional keys to omit.
            var json = new PEBC_AllowedLimitRange(
                           CommodityQuantity.ElectricPower3PhaseSymmetric,
                           PEBC_PowerEnvelopeLimitType.UpperLimit,
                           new NumberRange(0, 0),
                           true
                       ).ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] { "commodity_quantity", "limit_type", "range_boundary", "abnormal_condition_only" }));

            S2SchemaValidator.AssertValidType(json, "PEBC.AllowedLimitRange");

        }

        [Test]
        public void AllowedLimitRange_MissingMandatoryProperty_FailsWithPropertyName()
        {

            var missingRange = JObject.Parse("""
                                   {
                                     "commodity_quantity":      "ELECTRIC.POWER.3_PHASE_SYMMETRIC",
                                     "limit_type":              "LOWER_LIMIT",
                                     "abnormal_condition_only": false
                                   }
                                   """);

            Assert.That(PEBC_AllowedLimitRange.TryParse(missingRange, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("range_boundary"));

            var missingLimitType = JObject.Parse("""
                                       {
                                         "commodity_quantity":      "ELECTRIC.POWER.3_PHASE_SYMMETRIC",
                                         "range_boundary":          { "start_of_range": -5000, "end_of_range": 0 },
                                         "abnormal_condition_only": false
                                       }
                                       """);

            Assert.That(PEBC_AllowedLimitRange.TryParse(missingLimitType, out _, out error), Is.False);
            Assert.That(error, Does.Contain("limit_type"));

        }

        [Test]
        public void AllowedLimitRange_Strict_RejectsAdditionalProperty()
        {

            var extra = JObject.Parse("""
                            {
                              "commodity_quantity":      "ELECTRIC.POWER.3_PHASE_SYMMETRIC",
                              "limit_type":              "LOWER_LIMIT",
                              "range_boundary":          { "start_of_range": -5000, "end_of_range": 0 },
                              "abnormal_condition_only": false,
                              "foo":                     42
                            }
                            """);

            Assert.That(PEBC_AllowedLimitRange.TryParse(extra, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PEBC_AllowedLimitRange.TryParse(extra, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void AllowedLimitRange_RejectsUnknownEnumValues()
        {

            var unknownLimitType = JObject.Parse("""
                                       {
                                         "commodity_quantity":      "ELECTRIC.POWER.3_PHASE_SYMMETRIC",
                                         "limit_type":              "MIDDLE_LIMIT",
                                         "range_boundary":          { "start_of_range": -5000, "end_of_range": 0 },
                                         "abnormal_condition_only": false
                                       }
                                       """);

            Assert.That(PEBC_AllowedLimitRange.TryParse(unknownLimitType, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("limit_type"));

        }

        [Test]
        public void AllowedLimitRange_RangeBoundary_RejectsStartGreaterThanEnd()
        {

            // Semantic rule (§5): start_of_range <= end_of_range of the range boundary.
            Assert.That(() => new PEBC_AllowedLimitRange(
                                  CommodityQuantity.ElectricPower3PhaseSymmetric,
                                  PEBC_PowerEnvelopeLimitType.LowerLimit,
                                  new NumberRange(0, -5000),
                                  false
                              ),
                        Throws.ArgumentException);

            var json = JObject.Parse("""
                           {
                             "commodity_quantity":      "ELECTRIC.POWER.3_PHASE_SYMMETRIC",
                             "limit_type":              "LOWER_LIMIT",
                             "range_boundary":          { "start_of_range": 0, "end_of_range": -5000 },
                             "abnormal_condition_only": false
                           }
                           """);

            Assert.That(PEBC_AllowedLimitRange.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("range_boundary"));
            Assert.That(error, Does.Contain("must not be greater"));

        }

        #endregion

        #region PEBC_PowerEnvelopeElement

        [Test]
        public void PowerEnvelopeElement_RoundTrips_AndIsSchemaValid()
        {

            var element = new PEBC_PowerEnvelopeElement(fifteenMinutes, 0, -3000);
            var json    = element.ToJSON();

            Assert.That(json["duration"]?.   Value<Int64>(),  Is.EqualTo(900000L));
            Assert.That(json["upper_limit"]?.Value<Double>(), Is.EqualTo(0));
            Assert.That(json["lower_limit"]?.Value<Double>(), Is.EqualTo(-3000));

            S2SchemaValidator.AssertValidType(json, "PEBC.PowerEnvelopeElement");

            Assert.That(PEBC_PowerEnvelopeElement.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                 Is.EqualTo(element));
            Assert.That(parsed!.GetHashCode(),  Is.EqualTo(element.GetHashCode()));
            Assert.That(parsed. Duration,       Is.EqualTo(fifteenMinutes));
            Assert.That(parsed. UpperLimit,     Is.EqualTo(0));
            Assert.That(parsed. LowerLimit,     Is.EqualTo(-3000));
            Assert.That(parsed. Contains(-1500), Is.True);
            Assert.That(parsed. Contains(1),     Is.False);

            var clone = element.Clone();
            Assert.That(clone,                             Is.EqualTo(element));
            Assert.That(ReferenceEquals(clone, element),   Is.False);

        }

        [Test]
        public void PowerEnvelopeElement_MandatoryOnly_HasExactlyTheSchemaKeys()
        {

            // All three properties are mandatory; there are no optional keys to omit.
            // Equal limits are allowed (lower_limit <= upper_limit).
            var json = new PEBC_PowerEnvelopeElement(Duration.Zero, 0, 0).ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] { "duration", "upper_limit", "lower_limit" }));

            S2SchemaValidator.AssertValidType(json, "PEBC.PowerEnvelopeElement");

        }

        [Test]
        public void PowerEnvelopeElement_MissingMandatoryProperty_FailsWithPropertyName()
        {

            var missingLower = JObject.Parse("""{ "duration": 900000, "upper_limit": 0 }""");
            Assert.That(PEBC_PowerEnvelopeElement.TryParse(missingLower, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("lower_limit"));

            var missingDuration = JObject.Parse("""{ "upper_limit": 0, "lower_limit": -3000 }""");
            Assert.That(PEBC_PowerEnvelopeElement.TryParse(missingDuration, out _, out error), Is.False);
            Assert.That(error, Does.Contain("duration"));

        }

        [Test]
        public void PowerEnvelopeElement_Strict_RejectsAdditionalProperty()
        {

            var extra = JObject.Parse("""{ "duration": 900000, "upper_limit": 0, "lower_limit": -3000, "foo": 1 }""");

            Assert.That(PEBC_PowerEnvelopeElement.TryParse(extra, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PEBC_PowerEnvelopeElement.TryParse(extra, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void PowerEnvelopeElement_RejectsLowerLimitGreaterThanUpperLimit()
        {

            // Semantic rule (§5): lower_limit <= upper_limit.
            Assert.That(() => new PEBC_PowerEnvelopeElement(fifteenMinutes, -3000, 0), Throws.ArgumentException);

            var json = JObject.Parse("""{ "duration": 900000, "upper_limit": -3000, "lower_limit": 0 }""");
            Assert.That(PEBC_PowerEnvelopeElement.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("lower limit"));
            Assert.That(error, Does.Contain("smaller or equal"));

        }

        [Test]
        public void PowerEnvelopeElement_RejectsNegativeDuration()
        {

            var json = JObject.Parse("""{ "duration": -1, "upper_limit": 0, "lower_limit": -3000 }""");
            Assert.That(PEBC_PowerEnvelopeElement.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("duration"));

        }

        #endregion

        #region PEBC_PowerEnvelope

        [Test]
        public void PowerEnvelope_RoundTrips_AndIsSchemaValid()
        {

            var id       = PowerEnvelope_Id.NewRandom;

            var envelope = new PEBC_PowerEnvelope(
                               id,
                               CommodityQuantity.ElectricPower3PhaseSymmetric,
                               [
                                   new PEBC_PowerEnvelopeElement(fifteenMinutes,     0, -3000),
                                   new PEBC_PowerEnvelopeElement(fifteenMinutes,     0, -1500),
                                   new PEBC_PowerEnvelopeElement(Duration.FromSeconds(3600), 0,     0)
                               ]
                           );

            var json = envelope.ToJSON();

            Assert.That(json["id"]?.                Value<String>(), Is.EqualTo(id.ToString()));
            Assert.That(json["commodity_quantity"]?.Value<String>(), Is.EqualTo("ELECTRIC.POWER.3_PHASE_SYMMETRIC"));
            Assert.That(json["power_envelope_elements"],             Is.TypeOf<JArray>());
            Assert.That(json["power_envelope_elements"]!.Count(),    Is.EqualTo(3));

            S2SchemaValidator.AssertValidType(json, "PEBC.PowerEnvelope");

            Assert.That(PEBC_PowerEnvelope.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                              Is.EqualTo(envelope));
            Assert.That(parsed!.GetHashCode(),               Is.EqualTo(envelope.GetHashCode()));
            Assert.That(parsed. Id,                          Is.EqualTo(id));
            Assert.That(parsed. CommodityQuantity,           Is.EqualTo(CommodityQuantity.ElectricPower3PhaseSymmetric));
            Assert.That(parsed. PowerEnvelopeElements.Count, Is.EqualTo(3));
            Assert.That(parsed. PowerEnvelopeElements[1].LowerLimit, Is.EqualTo(-1500));
            Assert.That(parsed. TotalDuration,               Is.EqualTo(Duration.FromMilliseconds(900000 + 900000 + 3600000)));

            var clone = envelope.Clone();
            Assert.That(clone,                                                                              Is.EqualTo(envelope));
            Assert.That(ReferenceEquals(clone, envelope),                                                   Is.False);
            Assert.That(ReferenceEquals(clone.PowerEnvelopeElements, envelope.PowerEnvelopeElements),       Is.False);
            Assert.That(ReferenceEquals(clone.PowerEnvelopeElements[0], envelope.PowerEnvelopeElements[0]), Is.False);

        }

        [Test]
        public void PowerEnvelope_MandatoryOnly_HasExactlyTheSchemaKeys()
        {

            // All three properties are mandatory; there are no optional keys to omit.
            var json = new PEBC_PowerEnvelope(
                           PowerEnvelope_Id.Parse("pv_envelope_1"),
                           CommodityQuantity.ElectricPowerL1,
                           [ CreateElement() ]
                       ).ToJSON();

            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] { "id", "commodity_quantity", "power_envelope_elements" }));

            S2SchemaValidator.AssertValidType(json, "PEBC.PowerEnvelope");

            Assert.That(PEBC_PowerEnvelope.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed!.Id.ToString(), Is.EqualTo("pv_envelope_1"));

        }

        [Test]
        public void PowerEnvelope_ElementOrderMatters()
        {

            var id = PowerEnvelope_Id.NewRandom;
            var a  = CreateElement(0, -3000);
            var b  = CreateElement(0, -1500);

            var envelope1 = new PEBC_PowerEnvelope(id, CommodityQuantity.ElectricPower3PhaseSymmetric, [ a, b ]);
            var envelope2 = new PEBC_PowerEnvelope(id, CommodityQuantity.ElectricPower3PhaseSymmetric, [ b, a ]);

            Assert.That(envelope1, Is.Not.EqualTo(envelope2));
            Assert.That(envelope1 != envelope2, Is.True);

        }

        [Test]
        public void PowerEnvelope_ConstructorTakesADefensiveCopy()
        {

            var elements = new List<PEBC_PowerEnvelopeElement> { CreateElement() };
            var envelope = new PEBC_PowerEnvelope(PowerEnvelope_Id.NewRandom, CommodityQuantity.ElectricPower3PhaseSymmetric, elements);

            elements.Add(CreateElement());

            Assert.That(envelope.PowerEnvelopeElements.Count, Is.EqualTo(1));

        }

        [Test]
        public void PowerEnvelope_MissingMandatoryProperty_FailsWithPropertyName()
        {

            var missingElements = JObject.Parse("""
                                      {
                                        "id":                 "8cfc3e8e-2d0d-4b5f-9d21-3f1d9d3f0a11",
                                        "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC"
                                      }
                                      """);

            Assert.That(PEBC_PowerEnvelope.TryParse(missingElements, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("power_envelope_elements"));

            var missingId = JObject.Parse("""
                                {
                                  "commodity_quantity":      "ELECTRIC.POWER.3_PHASE_SYMMETRIC",
                                  "power_envelope_elements": [ { "duration": 900000, "upper_limit": 0, "lower_limit": -3000 } ]
                                }
                                """);

            Assert.That(PEBC_PowerEnvelope.TryParse(missingId, out _, out error), Is.False);
            Assert.That(error, Does.Contain("id"));

        }

        [Test]
        public void PowerEnvelope_Strict_RejectsAdditionalProperty_AndNonUUIDIds()
        {

            var extra = JObject.Parse("""
                            {
                              "id":                      "8cfc3e8e-2d0d-4b5f-9d21-3f1d9d3f0a11",
                              "commodity_quantity":      "ELECTRIC.POWER.3_PHASE_SYMMETRIC",
                              "power_envelope_elements": [ { "duration": 900000, "upper_limit": 0, "lower_limit": -3000 } ],
                              "foo":                     "bar"
                            }
                            """);

            Assert.That(PEBC_PowerEnvelope.TryParse(extra, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PEBC_PowerEnvelope.TryParse(extra, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

            var nonUUID = new PEBC_PowerEnvelope(
                              PowerEnvelope_Id.Parse("pv_envelope_1"),
                              CommodityQuantity.ElectricPower3PhaseSymmetric,
                              [ CreateElement() ]
                          ).ToJSON();

            Assert.That(PEBC_PowerEnvelope.TryParse(nonUUID, out _, out _,     S2ParserOptions.Default), Is.True);
            Assert.That(PEBC_PowerEnvelope.TryParse(nonUUID, out _, out error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("not a UUID"));

        }

        [Test]
        public void PowerEnvelope_InvalidElement_FailsWithElementError()
        {

            var json = JObject.Parse("""
                           {
                             "id":                      "8cfc3e8e-2d0d-4b5f-9d21-3f1d9d3f0a11",
                             "commodity_quantity":      "ELECTRIC.POWER.3_PHASE_SYMMETRIC",
                             "power_envelope_elements": [
                               { "duration": 900000, "upper_limit": 0,     "lower_limit": -3000 },
                               { "duration": 900000, "upper_limit": -3000, "lower_limit": 0 }
                             ]
                           }
                           """);

            Assert.That(PEBC_PowerEnvelope.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("power_envelope_elements"));
            Assert.That(error, Does.Contain("lower limit"));

        }

        [Test]
        public void PowerEnvelope_RejectsEmptyElements()
        {

            // Schema constraint: minItems 1.
            Assert.That(() => new PEBC_PowerEnvelope(PowerEnvelope_Id.NewRandom, CommodityQuantity.ElectricPower3PhaseSymmetric, []),
                        Throws.ArgumentException);

            var json = JObject.Parse("""
                           {
                             "id":                      "8cfc3e8e-2d0d-4b5f-9d21-3f1d9d3f0a11",
                             "commodity_quantity":      "ELECTRIC.POWER.3_PHASE_SYMMETRIC",
                             "power_envelope_elements": []
                           }
                           """);

            Assert.That(PEBC_PowerEnvelope.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("power_envelope_elements"));

        }

        [Test]
        public void PowerEnvelope_RejectsMoreThan288Elements()
        {

            // Schema constraint: maxItems 288.
            var elements288 = Enumerable.Range(0, 288).Select(_ => CreateElement()).ToList();
            var elements289 = Enumerable.Range(0, 289).Select(_ => CreateElement()).ToList();

            var envelope288 = new PEBC_PowerEnvelope(PowerEnvelope_Id.NewRandom, CommodityQuantity.ElectricPower3PhaseSymmetric, elements288);
            Assert.That(envelope288.PowerEnvelopeElements.Count, Is.EqualTo(288));
            S2SchemaValidator.AssertValidType(envelope288.ToJSON(), "PEBC.PowerEnvelope");

            Assert.That(() => new PEBC_PowerEnvelope(PowerEnvelope_Id.NewRandom, CommodityQuantity.ElectricPower3PhaseSymmetric, elements289),
                        Throws.ArgumentException);

            var json = envelope288.ToJSON();
            ((JArray) json["power_envelope_elements"]!).Add(CreateElement().ToJSON());

            Assert.That(PEBC_PowerEnvelope.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("power_envelope_elements"));

        }

        [Test]
        public void PowerEnvelope_RejectsUnknownCommodityQuantity()
        {

            var json = JObject.Parse("""
                           {
                             "id":                      "8cfc3e8e-2d0d-4b5f-9d21-3f1d9d3f0a11",
                             "commodity_quantity":      "ELECTRIC.POWER.L4",
                             "power_envelope_elements": [ { "duration": 900000, "upper_limit": 0, "lower_limit": -3000 } ]
                           }
                           """);

            Assert.That(PEBC_PowerEnvelope.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("commodity_quantity"));

        }

        #endregion

    }

}

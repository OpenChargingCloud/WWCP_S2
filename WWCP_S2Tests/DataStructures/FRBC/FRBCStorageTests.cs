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
    /// Tests of the FRBC storage related data structures: FRBC_StorageDescription,
    /// FRBC_FillLevelTargetProfileElement, FRBC_LeakageBehaviourElement and
    /// FRBC_UsageForecastElement. Values follow the EV charger example of the S2
    /// documentation (fill level = state of charge in percent, 0..100).
    /// </summary>
    [TestFixture]
    public sealed class FRBCStorageTests
    {

        #region FRBC_FillLevelTargetProfileElement

        [Test]
        public void FillLevelTargetProfileElement_RoundTrips_AndIsSchemaValid()
        {

            var element = new FRBC_FillLevelTargetProfileElement(
                              Duration.FromMilliseconds(3600000),
                              new NumberRange(80, 100)
                          );

            var json = element.ToJSON();

            S2SchemaValidator.AssertValidType(json, "FRBC.FillLevelTargetProfileElement");

            Assert.That(json["duration"]?.Value<Int64>(),                       Is.EqualTo(3600000L));
            Assert.That(json["fill_level_range"]?["start_of_range"]?.Value<Double>(), Is.EqualTo(80));

            Assert.That(FRBC_FillLevelTargetProfileElement.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                       Is.EqualTo(element));
            Assert.That(parsed!.Duration.Milliseconds, Is.EqualTo(3600000L));
            Assert.That(parsed. FillLevelRange,        Is.EqualTo(new NumberRange(80, 100)));
            Assert.That(parsed. GetHashCode(),         Is.EqualTo(element.GetHashCode()));
            Assert.That(element.Clone(),               Is.EqualTo(element));
            Assert.That(element == parsed,             Is.True);

        }

        [Test]
        public void FillLevelTargetProfileElement_MissingMandatoryProperty_Fails()
        {

            var missingDuration = JObject.Parse("""{ "fill_level_range": { "start_of_range": 80, "end_of_range": 100 } }""");
            Assert.That(FRBC_FillLevelTargetProfileElement.TryParse(missingDuration, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("duration"));

            var missingRange = JObject.Parse("""{ "duration": 3600000 }""");
            Assert.That(FRBC_FillLevelTargetProfileElement.TryParse(missingRange, out _, out error), Is.False);
            Assert.That(error, Does.Contain("fill_level_range"));

        }

        [Test]
        public void FillLevelTargetProfileElement_Strict_RejectsAdditionalProperties()
        {

            var extra = JObject.Parse("""{ "duration": 3600000, "fill_level_range": { "start_of_range": 80, "end_of_range": 100 }, "foo": 1 }""");

            Assert.That(FRBC_FillLevelTargetProfileElement.TryParse(extra, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_FillLevelTargetProfileElement.TryParse(extra, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void FillLevelTargetProfileElement_RejectsInvalidFillLevelRange()
        {

            var json = JObject.Parse("""{ "duration": 3600000, "fill_level_range": { "start_of_range": 100, "end_of_range": 80 } }""");

            Assert.That(FRBC_FillLevelTargetProfileElement.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("fill_level_range"));
            Assert.That(error, Does.Contain("must not be greater"));

            var negativeDuration = JObject.Parse("""{ "duration": -1, "fill_level_range": { "start_of_range": 80, "end_of_range": 100 } }""");
            Assert.That(FRBC_FillLevelTargetProfileElement.TryParse(negativeDuration, out _, out error), Is.False);
            Assert.That(error, Does.Contain("duration"));

        }

        #endregion

        #region FRBC_LeakageBehaviourElement

        [Test]
        public void LeakageBehaviourElement_RoundTrips_AndIsSchemaValid()
        {

            var element = new FRBC_LeakageBehaviourElement(
                              new NumberRange(0, 100),
                              0.0001
                          );

            var json = element.ToJSON();

            S2SchemaValidator.AssertValidType(json, "FRBC.LeakageBehaviourElement");

            Assert.That(json["leakage_rate"]?.Value<Double>(), Is.EqualTo(0.0001));

            Assert.That(FRBC_LeakageBehaviourElement.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                 Is.EqualTo(element));
            Assert.That(parsed!.FillLevelRange, Is.EqualTo(new NumberRange(0, 100)));
            Assert.That(parsed. LeakageRate,    Is.EqualTo(0.0001));
            Assert.That(parsed. GetHashCode(),  Is.EqualTo(element.GetHashCode()));
            Assert.That(element.Clone(),        Is.EqualTo(element));
            Assert.That(element != parsed,      Is.False);

        }

        [Test]
        public void LeakageBehaviourElement_MissingMandatoryProperty_Fails()
        {

            var missingRange = JObject.Parse("""{ "leakage_rate": 0.0001 }""");
            Assert.That(FRBC_LeakageBehaviourElement.TryParse(missingRange, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("fill_level_range"));

            var missingRate = JObject.Parse("""{ "fill_level_range": { "start_of_range": 0, "end_of_range": 100 } }""");
            Assert.That(FRBC_LeakageBehaviourElement.TryParse(missingRate, out _, out error), Is.False);
            Assert.That(error, Does.Contain("leakage_rate"));

        }

        [Test]
        public void LeakageBehaviourElement_Strict_RejectsAdditionalProperties()
        {

            var extra = JObject.Parse("""{ "fill_level_range": { "start_of_range": 0, "end_of_range": 100 }, "leakage_rate": 0.0001, "foo": 1 }""");

            Assert.That(FRBC_LeakageBehaviourElement.TryParse(extra, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_LeakageBehaviourElement.TryParse(extra, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void LeakageBehaviourElement_RequiresStrictlyIncreasingFillLevelRange()
        {

            // Constructor: an empty range (start == end) is rejected (strict '<').
            Assert.That(() => new FRBC_LeakageBehaviourElement(new NumberRange(50, 50), 0.0001), Throws.ArgumentException);

            // TryParse: start == end.
            var equal = JObject.Parse("""{ "fill_level_range": { "start_of_range": 50, "end_of_range": 50 }, "leakage_rate": 0.0001 }""");
            Assert.That(FRBC_LeakageBehaviourElement.TryParse(equal, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("must be less than"));

            // TryParse: start > end is already rejected by NumberRange.
            var reversed = JObject.Parse("""{ "fill_level_range": { "start_of_range": 100, "end_of_range": 0 }, "leakage_rate": 0.0001 }""");
            Assert.That(FRBC_LeakageBehaviourElement.TryParse(reversed, out _, out error), Is.False);
            Assert.That(error, Does.Contain("fill_level_range"));

        }

        [Test]
        public void LeakageBehaviourElement_RejectsNaNLeakageRate()
        {
            Assert.That(() => new FRBC_LeakageBehaviourElement(new NumberRange(0, 100), Double.NaN), Throws.ArgumentException);
        }

        #endregion

        #region FRBC_StorageDescription

        [Test]
        public void StorageDescription_RoundTrips_AndIsSchemaValid()
        {

            var storage = new FRBC_StorageDescription(
                              ProvidesLeakageBehaviour:        true,
                              ProvidesFillLevelTargetProfile:  true,
                              ProvidesUsageForecast:           true,
                              FillLevelRange:                  new NumberRange(0, 100),
                              DiagnosticLabel:                 "EV battery",
                              FillLevelLabel:                  "% state of charge"
                          );

            var json = storage.ToJSON();

            S2SchemaValidator.AssertValidType(json, "FRBC.StorageDescription");

            // Schema order of the properties.
            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] {
                            "diagnostic_label",
                            "fill_level_label",
                            "provides_leakage_behaviour",
                            "provides_fill_level_target_profile",
                            "provides_usage_forecast",
                            "fill_level_range"
                        }));

            Assert.That(FRBC_StorageDescription.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                                 Is.EqualTo(storage));
            Assert.That(parsed!.DiagnosticLabel,                Is.EqualTo("EV battery"));
            Assert.That(parsed. FillLevelLabel,                 Is.EqualTo("% state of charge"));
            Assert.That(parsed. ProvidesLeakageBehaviour,       Is.True);
            Assert.That(parsed. ProvidesFillLevelTargetProfile, Is.True);
            Assert.That(parsed. ProvidesUsageForecast,          Is.True);
            Assert.That(parsed. FillLevelRange,                 Is.EqualTo(new NumberRange(0, 100)));
            Assert.That(parsed. GetHashCode(),                  Is.EqualTo(storage.GetHashCode()));
            Assert.That(storage.Clone(),                        Is.EqualTo(storage));
            Assert.That(storage.ToString(),                     Does.Contain("EV battery"));

        }

        [Test]
        public void StorageDescription_MandatoryOnly_OmitsOptionalProperties()
        {

            var storage = new FRBC_StorageDescription(
                              false,
                              false,
                              false,
                              new NumberRange(0, 100)
                          );

            var json = storage.ToJSON();

            Assert.That(json.ContainsKey("diagnostic_label"), Is.False);
            Assert.That(json.ContainsKey("fill_level_label"), Is.False);
            Assert.That(json.Count,                           Is.EqualTo(4));

            S2SchemaValidator.AssertValidType(json, "FRBC.StorageDescription");

            Assert.That(FRBC_StorageDescription.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                  Is.EqualTo(storage));
            Assert.That(parsed!.DiagnosticLabel, Is.Null);
            Assert.That(parsed. FillLevelLabel,  Is.Null);

            // Optional properties take part in equality.
            var labelled = new FRBC_StorageDescription(false, false, false, new NumberRange(0, 100), "battery");
            Assert.That(labelled, Is.Not.EqualTo(storage));

        }

        [Test]
        public void StorageDescription_MissingMandatoryProperty_Fails()
        {

            var missingLeakage = JObject.Parse("""
                {
                    "provides_fill_level_target_profile": true,
                    "provides_usage_forecast": true,
                    "fill_level_range": { "start_of_range": 0, "end_of_range": 100 }
                }
                """);
            Assert.That(FRBC_StorageDescription.TryParse(missingLeakage, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("provides_leakage_behaviour"));

            var missingTargetProfile = JObject.Parse("""
                {
                    "provides_leakage_behaviour": true,
                    "provides_usage_forecast": true,
                    "fill_level_range": { "start_of_range": 0, "end_of_range": 100 }
                }
                """);
            Assert.That(FRBC_StorageDescription.TryParse(missingTargetProfile, out _, out error), Is.False);
            Assert.That(error, Does.Contain("provides_fill_level_target_profile"));

            var missingUsageForecast = JObject.Parse("""
                {
                    "provides_leakage_behaviour": true,
                    "provides_fill_level_target_profile": true,
                    "fill_level_range": { "start_of_range": 0, "end_of_range": 100 }
                }
                """);
            Assert.That(FRBC_StorageDescription.TryParse(missingUsageForecast, out _, out error), Is.False);
            Assert.That(error, Does.Contain("provides_usage_forecast"));

            var missingRange = JObject.Parse("""
                {
                    "provides_leakage_behaviour": true,
                    "provides_fill_level_target_profile": true,
                    "provides_usage_forecast": true
                }
                """);
            Assert.That(FRBC_StorageDescription.TryParse(missingRange, out _, out error), Is.False);
            Assert.That(error, Does.Contain("fill_level_range"));

            // Wrong type: a string instead of a boolean.
            var wrongType = JObject.Parse("""
                {
                    "provides_leakage_behaviour": "yes",
                    "provides_fill_level_target_profile": true,
                    "provides_usage_forecast": true,
                    "fill_level_range": { "start_of_range": 0, "end_of_range": 100 }
                }
                """);
            Assert.That(FRBC_StorageDescription.TryParse(wrongType, out _, out error), Is.False);
            Assert.That(error, Does.Contain("provides_leakage_behaviour"));

        }

        [Test]
        public void StorageDescription_Strict_RejectsAdditionalProperties()
        {

            var extra = JObject.Parse("""
                {
                    "provides_leakage_behaviour": true,
                    "provides_fill_level_target_profile": true,
                    "provides_usage_forecast": true,
                    "fill_level_range": { "start_of_range": 0, "end_of_range": 100 },
                    "foo": 1
                }
                """);

            Assert.That(FRBC_StorageDescription.TryParse(extra, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_StorageDescription.TryParse(extra, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void StorageDescription_RejectsInvalidFillLevelRange()
        {

            Assert.That(() => new NumberRange(100, 0), Throws.ArgumentException);

            var json = JObject.Parse("""
                {
                    "provides_leakage_behaviour": true,
                    "provides_fill_level_target_profile": true,
                    "provides_usage_forecast": true,
                    "fill_level_range": { "start_of_range": 100, "end_of_range": 0 }
                }
                """);

            Assert.That(FRBC_StorageDescription.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("fill_level_range"));
            Assert.That(error, Does.Contain("must not be greater"));

        }

        #endregion

        #region FRBC_UsageForecastElement

        [Test]
        public void UsageForecastElement_RoundTrips_AndIsSchemaValid()
        {

            var element = new FRBC_UsageForecastElement(
                              Duration:             Duration.FromMilliseconds(3600000),
                              UsageRateExpected:    0.0005,
                              UsageRateUpperLimit:  0.0010,
                              UsageRateUpper95PPR:  0.0008,
                              UsageRateUpper68PPR:  0.0006,
                              UsageRateLower68PPR:  0.0004,
                              UsageRateLower95PPR:  0.0002,
                              UsageRateLowerLimit:  0.0000
                          );

            var json = element.ToJSON();

            S2SchemaValidator.AssertValidType(json, "FRBC.UsageForecastElement");

            // Schema order of the properties, including the "95PPR"/"68PPR" wire-name quirks.
            Assert.That(json.Properties().Select(p => p.Name),
                        Is.EqualTo(new[] {
                            "duration",
                            "usage_rate_upper_limit",
                            "usage_rate_upper_95PPR",
                            "usage_rate_upper_68PPR",
                            "usage_rate_expected",
                            "usage_rate_lower_68PPR",
                            "usage_rate_lower_95PPR",
                            "usage_rate_lower_limit"
                        }));

            Assert.That(FRBC_UsageForecastElement.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                        Is.EqualTo(element));
            Assert.That(parsed!.Duration.Milliseconds, Is.EqualTo(3600000L));
            Assert.That(parsed. UsageRateExpected,     Is.EqualTo(0.0005));
            Assert.That(parsed. UsageRateUpperLimit,   Is.EqualTo(0.0010));
            Assert.That(parsed. UsageRateUpper95PPR,   Is.EqualTo(0.0008));
            Assert.That(parsed. UsageRateUpper68PPR,   Is.EqualTo(0.0006));
            Assert.That(parsed. UsageRateLower68PPR,   Is.EqualTo(0.0004));
            Assert.That(parsed. UsageRateLower95PPR,   Is.EqualTo(0.0002));
            Assert.That(parsed. UsageRateLowerLimit,   Is.EqualTo(0.0000));
            Assert.That(parsed. GetHashCode(),         Is.EqualTo(element.GetHashCode()));
            Assert.That(element.Clone(),               Is.EqualTo(element));

        }

        [Test]
        public void UsageForecastElement_MandatoryOnly_OmitsOptionalProperties()
        {

            var element = new FRBC_UsageForecastElement(
                              Duration.FromMilliseconds(900000),
                              0.0005
                          );

            var json = element.ToJSON();

            Assert.That(json.Count,                                 Is.EqualTo(2));
            Assert.That(json.ContainsKey("duration"),               Is.True);
            Assert.That(json.ContainsKey("usage_rate_expected"),    Is.True);
            Assert.That(json.ContainsKey("usage_rate_upper_limit"), Is.False);
            Assert.That(json.ContainsKey("usage_rate_upper_95PPR"), Is.False);
            Assert.That(json.ContainsKey("usage_rate_upper_68PPR"), Is.False);
            Assert.That(json.ContainsKey("usage_rate_lower_68PPR"), Is.False);
            Assert.That(json.ContainsKey("usage_rate_lower_95PPR"), Is.False);
            Assert.That(json.ContainsKey("usage_rate_lower_limit"), Is.False);

            S2SchemaValidator.AssertValidType(json, "FRBC.UsageForecastElement");

            Assert.That(FRBC_UsageForecastElement.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                      Is.EqualTo(element));
            Assert.That(parsed!.UsageRateUpperLimit, Is.Null);
            Assert.That(parsed. UsageRateLowerLimit, Is.Null);

            // Optional properties take part in equality.
            var withLimit = new FRBC_UsageForecastElement(Duration.FromMilliseconds(900000), 0.0005, UsageRateUpperLimit: 0.001);
            Assert.That(withLimit, Is.Not.EqualTo(element));

        }

        [Test]
        public void UsageForecastElement_MissingMandatoryProperty_Fails()
        {

            var missingDuration = JObject.Parse("""{ "usage_rate_expected": 0.0005 }""");
            Assert.That(FRBC_UsageForecastElement.TryParse(missingDuration, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("duration"));

            var missingExpected = JObject.Parse("""{ "duration": 3600000, "usage_rate_upper_limit": 0.001 }""");
            Assert.That(FRBC_UsageForecastElement.TryParse(missingExpected, out _, out error), Is.False);
            Assert.That(error, Does.Contain("usage_rate_expected"));

            // Wrong type of an optional property is an error, too.
            var wrongType = JObject.Parse("""{ "duration": 3600000, "usage_rate_expected": 0.0005, "usage_rate_upper_95PPR": "high" }""");
            Assert.That(FRBC_UsageForecastElement.TryParse(wrongType, out _, out error), Is.False);
            Assert.That(error, Does.Contain("usage_rate_upper_95PPR"));

        }

        [Test]
        public void UsageForecastElement_Strict_RejectsAdditionalProperties()
        {

            var extra = JObject.Parse("""{ "duration": 3600000, "usage_rate_expected": 0.0005, "foo": 1 }""");

            Assert.That(FRBC_UsageForecastElement.TryParse(extra, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_UsageForecastElement.TryParse(extra, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

            // Lower-case "ppr" is a different (unknown) property name.
            var wrongCase = JObject.Parse("""{ "duration": 3600000, "usage_rate_expected": 0.0005, "usage_rate_upper_95ppr": 0.001 }""");
            Assert.That(FRBC_UsageForecastElement.TryParse(wrongCase, out var lenient, out _, S2ParserOptions.Default), Is.True);
            Assert.That(lenient!.UsageRateUpper95PPR, Is.Null);
            Assert.That(FRBC_UsageForecastElement.TryParse(wrongCase, out _, out error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("usage_rate_upper_95ppr"));

        }

        [Test]
        public void UsageForecastElement_RejectsNaN_AndNegativeDuration()
        {

            Assert.That(() => new FRBC_UsageForecastElement(Duration.Zero, Double.NaN),                            Throws.ArgumentException);
            Assert.That(() => new FRBC_UsageForecastElement(Duration.Zero, 0.0005, UsageRateLowerLimit: Double.NaN), Throws.ArgumentException);

            var negativeDuration = JObject.Parse("""{ "duration": -1000, "usage_rate_expected": 0.0005 }""");
            Assert.That(FRBC_UsageForecastElement.TryParse(negativeDuration, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("duration"));

        }

        #endregion

    }

}

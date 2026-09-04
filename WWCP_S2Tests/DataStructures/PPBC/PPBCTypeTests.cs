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

namespace cloud.charging.open.protocols.S2.Tests.DataStructures.PPBC
{

    /// <summary>
    /// Tests of the PPBC data structures: PPBC_PowerSequenceElement, PPBC_PowerSequence,
    /// PPBC_PowerSequenceContainer and PPBC_PowerSequenceContainerStatus.
    /// </summary>
    [TestFixture]
    public sealed class PPBCTypeTests
    {

        #region Data

        // An EV charger described as a power profile: a short ramp-up, one hour of
        // full-power charging and half an hour of reduced charging (all values in W).

        private static readonly PowerSequence_Id           sequenceId1   = PowerSequence_Id.         Parse("2a2b3c4d-0000-4000-8000-000000000001");
        private static readonly PowerSequence_Id           sequenceId2   = PowerSequence_Id.         Parse("2a2b3c4d-0000-4000-8000-000000000002");
        private static readonly PowerSequenceContainer_Id  containerId   = PowerSequenceContainer_Id.Parse("3a2b3c4d-0000-4000-8000-000000000001");
        private static readonly PowerProfileDefinition_Id  profileId     = PowerProfileDefinition_Id.Parse("4a2b3c4d-0000-4000-8000-000000000001");

        // The 68 %/95 % boundaries are derived from the expected value, but clamped to the
        // 100 %-certainty limits (1400..11000 W): a 95 % boundary can never lie outside the 100 % one.
        private static PowerForecastValue Power3Phase(Double ValueExpected)

            => new (ValueExpected,
                    CommodityQuantity.ElectricPower3PhaseSymmetric,
                    ValueUpperLimit:  11000,
                    ValueUpper95PPR:  Math.Min(ValueExpected + 500, 11000),
                    ValueUpper68PPR:  Math.Min(ValueExpected + 200, 11000),
                    ValueLower68PPR:  Math.Max(ValueExpected - 200,  1400),
                    ValueLower95PPR:  Math.Max(ValueExpected - 500,  1400),
                    ValueLowerLimit:  1400);

        private static PowerForecastValue PowerL1(Double ValueExpected)
            => new (ValueExpected, CommodityQuantity.ElectricPowerL1);

        private static PPBC_PowerSequenceElement RampUp()
            => new (Duration.FromMilliseconds(3000),    [ Power3Phase(1400) ]);

        private static PPBC_PowerSequenceElement FullPower()
            => new (Duration.FromMilliseconds(3600000), [ Power3Phase(11000), PowerL1(3667) ]);

        private static PPBC_PowerSequenceElement ReducedPower()
            => new (Duration.FromMilliseconds(1800000), [ Power3Phase(5500) ]);

        private static PPBC_PowerSequence FullSequence()
            => new (sequenceId1, [ RampUp(), FullPower(), ReducedPower() ], true,  false, Duration.FromMilliseconds(600000));

        private static PPBC_PowerSequence MinimalSequence()
            => new (sequenceId2, [ RampUp(), ReducedPower() ],              false, true);

        private static String Keys(JObject JSON)
            => String.Join(",", JSON.Properties().Select(p => p.Name));

        #endregion


        #region PPBC_PowerSequenceElement

        [Test]
        public void PowerSequenceElement_RoundTrips_AndIsSchemaValid()
        {

            var element = FullPower();
            var json    = element.ToJSON();

            S2SchemaValidator.AssertValidType(json, "PPBC.PowerSequenceElement");

            Assert.That(json["duration"]!.Value<Int64>(),              Is.EqualTo(3600000L));
            Assert.That(((JArray) json["power_values"]!).Count,                 Is.EqualTo(2));

            Assert.That(PPBC_PowerSequenceElement.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                        Is.EqualTo(element));
            Assert.That(parsed!.Duration,                              Is.EqualTo(Duration.FromMilliseconds(3600000)));
            Assert.That(parsed. PowerValues,                           Has.Count.EqualTo(2));
            Assert.That(parsed. PowerValues[0].CommodityQuantity,      Is.EqualTo(CommodityQuantity.ElectricPower3PhaseSymmetric));
            Assert.That(parsed. GetHashCode(),                         Is.EqualTo(element.GetHashCode()));
            Assert.That(parsed. Clone(),                               Is.EqualTo(element));
            Assert.That(parsed. ToString(),                            Does.Contain("3600000 ms"));

            Assert.That(PPBC_PowerSequenceElement.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

        }

        [Test]
        public void PowerSequenceElement_MandatoryOnly_IsSchemaValid()
        {

            var element = new PPBC_PowerSequenceElement(Duration.FromMilliseconds(3000), [ PowerL1(1400) ]);
            var json    = element.ToJSON();

            Assert.That(Keys(json),                Is.EqualTo("duration,power_values"));
            Assert.That(json["power_values"]![0]!["value_expected"]!.Value<Double>(), Is.EqualTo(1400));

            S2SchemaValidator.AssertValidType(json, "PPBC.PowerSequenceElement");

            Assert.That(PPBC_PowerSequenceElement.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed, Is.EqualTo(element));

        }

        [Test]
        public void PowerSequenceElement_MissingMandatoryProperty_Fails()
        {

            var missingPowerValues = JObject.Parse("""{ "duration": 3000 }""");
            Assert.That(PPBC_PowerSequenceElement.TryParse(missingPowerValues, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("power_values"));

            var missingDuration = JObject.Parse("""
                { "power_values": [ { "value_expected": 11000, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" } ] }
                """);
            Assert.That(PPBC_PowerSequenceElement.TryParse(missingDuration, out _, out error), Is.False);
            Assert.That(error, Does.Contain("duration"));

        }

        [Test]
        public void PowerSequenceElement_Strict_RejectsAdditionalProperty()
        {

            var json = JObject.Parse("""
                {
                  "duration":     3000,
                  "power_values": [ { "value_expected": 11000, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" } ],
                  "foo":          1
                }
                """);

            Assert.That(PPBC_PowerSequenceElement.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PPBC_PowerSequenceElement.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void PowerSequenceElement_RejectsDuplicateCommodityQuantity()
        {

            Assert.That(() => new PPBC_PowerSequenceElement(Duration.FromMilliseconds(3000), [ Power3Phase(11000), Power3Phase(5500) ]),
                        Throws.ArgumentException);

            var json = JObject.Parse("""
                {
                  "duration":     3000,
                  "power_values": [
                    { "value_expected": 11000, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" },
                    { "value_expected":  5500, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" }
                  ]
                }
                """);

            Assert.That(PPBC_PowerSequenceElement.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at most one"));
            Assert.That(error, Does.Contain("ELECTRIC.POWER.3_PHASE_SYMMETRIC"));

        }

        [Test]
        public void PowerSequenceElement_RejectsEmptyAndTooManyPowerValues()
        {

            Assert.That(() => new PPBC_PowerSequenceElement(Duration.FromMilliseconds(3000), []), Throws.ArgumentException);

            var empty = JObject.Parse("""{ "duration": 3000, "power_values": [] }""");
            Assert.That(PPBC_PowerSequenceElement.TryParse(empty, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at least 1"));

            var tooMany = new JObject(
                              new JProperty("duration",      3000),
                              new JProperty("power_values",  new JArray(Enumerable.Range(0, 11).Select(i => PowerL1(1000 + i).ToJSON())))
                          );
            Assert.That(PPBC_PowerSequenceElement.TryParse(tooMany, out _, out error), Is.False);
            Assert.That(error, Does.Contain("at most 10"));

        }

        #endregion

        #region PPBC_PowerSequence

        [Test]
        public void PowerSequence_RoundTrips_AndIsSchemaValid()
        {

            var sequence = FullSequence();
            var json     = sequence.ToJSON();

            S2SchemaValidator.AssertValidType(json, "PPBC.PowerSequence");

            Assert.That(Keys(json),
                        Is.EqualTo("id,elements,is_interruptible,max_pause_before,abnormal_condition_only"));
            Assert.That(json["max_pause_before"]!.Value<Int64>(),      Is.EqualTo(600000L));

            Assert.That(PPBC_PowerSequence.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                        Is.EqualTo(sequence));
            Assert.That(parsed!.Id,                                    Is.EqualTo(sequenceId1));
            Assert.That(parsed. Elements,                              Has.Count.EqualTo(3));
            Assert.That(parsed. Elements[1],                           Is.EqualTo(FullPower()));
            Assert.That(parsed. IsInterruptible,                       Is.True);
            Assert.That(parsed. MaxPauseBefore,                        Is.EqualTo(Duration.FromMilliseconds(600000)));
            Assert.That(parsed. AbnormalConditionOnly,                 Is.False);
            Assert.That(parsed. GetHashCode(),                         Is.EqualTo(sequence.GetHashCode()));
            Assert.That(parsed. Clone(),                               Is.EqualTo(sequence));
            Assert.That(parsed. ToString(),                            Does.Contain(sequenceId1.ToString()));

            Assert.That(PPBC_PowerSequence.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

        }

        [Test]
        public void PowerSequence_MandatoryOnly_OmitsMaxPauseBefore()
        {

            var sequence = MinimalSequence();
            var json     = sequence.ToJSON();

            Assert.That(json.ContainsKey("max_pause_before"),          Is.False);
            Assert.That(Keys(json),
                        Is.EqualTo("id,elements,is_interruptible,abnormal_condition_only"));

            S2SchemaValidator.AssertValidType(json, "PPBC.PowerSequence");

            Assert.That(PPBC_PowerSequence.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                        Is.EqualTo(sequence));
            Assert.That(parsed!.MaxPauseBefore,                        Is.Null);
            Assert.That(parsed. AbnormalConditionOnly,                 Is.True);
            Assert.That(parsed, Is.Not.EqualTo(FullSequence()));

        }

        [Test]
        public void PowerSequence_MissingMandatoryProperty_Fails()
        {

            var json = FullSequence().ToJSON();
            json.Remove("is_interruptible");

            Assert.That(PPBC_PowerSequence.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("is_interruptible"));

            json = FullSequence().ToJSON();
            json.Remove("elements");

            Assert.That(PPBC_PowerSequence.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("elements"));

        }

        [Test]
        public void PowerSequence_Strict_RejectsAdditionalProperty()
        {

            var json = FullSequence().ToJSON();
            json.Add("foo", 1);

            Assert.That(PPBC_PowerSequence.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PPBC_PowerSequence.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void PowerSequence_RejectsEmptyElements_AndInvalidNestedElements()
        {

            Assert.That(() => new PPBC_PowerSequence(sequenceId1, [], true, false), Throws.ArgumentException);

            var empty = JObject.Parse("""
                { "id": "2a2b3c4d-0000-4000-8000-000000000001", "elements": [], "is_interruptible": true, "abnormal_condition_only": false }
                """);
            Assert.That(PPBC_PowerSequence.TryParse(empty, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at least 1"));

            // The semantic rule of the nested element surfaces through the parent parser.
            var duplicateCommodity = JObject.Parse("""
                {
                  "id":                      "2a2b3c4d-0000-4000-8000-000000000001",
                  "elements": [
                    {
                      "duration":     3000,
                      "power_values": [
                        { "value_expected": 11000, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" },
                        { "value_expected":  5500, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" }
                      ]
                    }
                  ],
                  "is_interruptible":        true,
                  "abnormal_condition_only": false
                }
                """);
            Assert.That(PPBC_PowerSequence.TryParse(duplicateCommodity, out _, out error), Is.False);
            Assert.That(error, Does.Contain("elements"));
            Assert.That(error, Does.Contain("at most one"));

        }

        #endregion

        #region PPBC_PowerSequenceContainer

        [Test]
        public void PowerSequenceContainer_RoundTrips_AndIsSchemaValid()
        {

            var container = new PPBC_PowerSequenceContainer(containerId, [ FullSequence(), MinimalSequence() ]);
            var json      = container.ToJSON();

            S2SchemaValidator.AssertValidType(json, "PPBC.PowerSequenceContainer");

            Assert.That(Keys(json),         Is.EqualTo("id,power_sequences"));
            Assert.That(((JArray) json["power_sequences"]!).Count,              Is.EqualTo(2));

            Assert.That(PPBC_PowerSequenceContainer.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                        Is.EqualTo(container));
            Assert.That(parsed!.Id,                                    Is.EqualTo(containerId));
            Assert.That(parsed. PowerSequences,                        Has.Count.EqualTo(2));
            Assert.That(parsed. PowerSequences[0].Id,                  Is.EqualTo(sequenceId1));
            Assert.That(parsed. PowerSequences[1].Id,                  Is.EqualTo(sequenceId2));
            Assert.That(parsed. GetHashCode(),                         Is.EqualTo(container.GetHashCode()));
            Assert.That(parsed. Clone(),                               Is.EqualTo(container));
            Assert.That(parsed. ToString(),                            Does.Contain("2 power sequence(s)"));

            Assert.That(PPBC_PowerSequenceContainer.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

        }

        [Test]
        public void PowerSequenceContainer_MandatoryOnly_IsSchemaValid()
        {

            var container = new PPBC_PowerSequenceContainer(containerId, [ MinimalSequence() ]);
            var json      = container.ToJSON();

            Assert.That(Keys(json),         Is.EqualTo("id,power_sequences"));
            Assert.That(json["power_sequences"]![0]!["max_pause_before"], Is.Null);

            S2SchemaValidator.AssertValidType(json, "PPBC.PowerSequenceContainer");

            Assert.That(PPBC_PowerSequenceContainer.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed, Is.EqualTo(container));

        }

        [Test]
        public void PowerSequenceContainer_MissingMandatoryProperty_Fails()
        {

            var missingSequences = JObject.Parse("""{ "id": "3a2b3c4d-0000-4000-8000-000000000001" }""");
            Assert.That(PPBC_PowerSequenceContainer.TryParse(missingSequences, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("power_sequences"));

            var missingId = new PPBC_PowerSequenceContainer(containerId, [ MinimalSequence() ]).ToJSON();
            missingId.Remove("id");
            Assert.That(PPBC_PowerSequenceContainer.TryParse(missingId, out _, out error), Is.False);
            Assert.That(error, Does.Contain("id"));

        }

        [Test]
        public void PowerSequenceContainer_Strict_RejectsAdditionalProperty()
        {

            var json = new PPBC_PowerSequenceContainer(containerId, [ MinimalSequence() ]).ToJSON();
            json.Add("foo", 1);

            Assert.That(PPBC_PowerSequenceContainer.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PPBC_PowerSequenceContainer.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void PowerSequenceContainer_RejectsDuplicateSequenceIds()
        {

            var duplicateId = new PPBC_PowerSequence(sequenceId1, [ RampUp() ], false, false);

            Assert.That(() => new PPBC_PowerSequenceContainer(containerId, [ FullSequence(), duplicateId ]), Throws.ArgumentException);

            var json = new JObject(
                           new JProperty("id",               containerId.ToString()),
                           new JProperty("power_sequences",  new JArray(FullSequence().ToJSON(), duplicateId.ToJSON()))
                       );

            Assert.That(PPBC_PowerSequenceContainer.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("unique"));
            Assert.That(error, Does.Contain(sequenceId1.ToString()));

        }

        [Test]
        public void PowerSequenceContainer_RejectsEmptyPowerSequences()
        {

            Assert.That(() => new PPBC_PowerSequenceContainer(containerId, []), Throws.ArgumentException);

            var empty = JObject.Parse("""{ "id": "3a2b3c4d-0000-4000-8000-000000000001", "power_sequences": [] }""");
            Assert.That(PPBC_PowerSequenceContainer.TryParse(empty, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at least 1"));

        }

        #endregion

        #region PPBC_PowerSequenceContainerStatus

        [Test]
        public void PowerSequenceContainerStatus_RoundTrips_AndIsSchemaValid()
        {

            var status = new PPBC_PowerSequenceContainerStatus(profileId,
                                                               containerId,
                                                               PPBC_PowerSequenceStatus.Executing,
                                                               sequenceId1,
                                                               Duration.FromMilliseconds(903000));
            var json   = status.ToJSON();

            S2SchemaValidator.AssertValidType(json, "PPBC.PowerSequenceContainerStatus");

            Assert.That(Keys(json),
                        Is.EqualTo("power_profile_id,sequence_container_id,selected_sequence_id,progress,status"));
            Assert.That(json["status"]!.Value<String>(),               Is.EqualTo("EXECUTING"));
            Assert.That(json["progress"]!.Value<Int64>(),              Is.EqualTo(903000L));

            Assert.That(PPBC_PowerSequenceContainerStatus.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                        Is.EqualTo(status));
            Assert.That(parsed!.PowerProfileId,                        Is.EqualTo(profileId));
            Assert.That(parsed. SequenceContainerId,                   Is.EqualTo(containerId));
            Assert.That(parsed. SelectedSequenceId,                    Is.EqualTo(sequenceId1));
            Assert.That(parsed. Progress,                              Is.EqualTo(Duration.FromMilliseconds(903000)));
            Assert.That(parsed. Status,                                Is.EqualTo(PPBC_PowerSequenceStatus.Executing));
            Assert.That(parsed. GetHashCode(),                         Is.EqualTo(status.GetHashCode()));
            Assert.That(parsed. Clone(),                               Is.EqualTo(status));
            Assert.That(parsed. ToString(),                            Does.Contain("EXECUTING"));

            Assert.That(PPBC_PowerSequenceContainerStatus.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

        }

        [Test]
        public void PowerSequenceContainerStatus_MandatoryOnly_OmitsOptionalProperties()
        {

            var status = new PPBC_PowerSequenceContainerStatus(profileId, containerId, PPBC_PowerSequenceStatus.NotScheduled);
            var json   = status.ToJSON();

            Assert.That(json.ContainsKey("selected_sequence_id"),      Is.False);
            Assert.That(json.ContainsKey("progress"),                  Is.False);
            Assert.That(Keys(json),
                        Is.EqualTo("power_profile_id,sequence_container_id,status"));

            S2SchemaValidator.AssertValidType(json, "PPBC.PowerSequenceContainerStatus");

            Assert.That(PPBC_PowerSequenceContainerStatus.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                        Is.EqualTo(status));
            Assert.That(parsed!.SelectedSequenceId,                    Is.Null);
            Assert.That(parsed. Progress,                              Is.Null);
            Assert.That(parsed. Status,                                Is.EqualTo(PPBC_PowerSequenceStatus.NotScheduled));

            // A selected but not yet started sequence: no progress.
            var scheduled = new PPBC_PowerSequenceContainerStatus(profileId, containerId, PPBC_PowerSequenceStatus.Scheduled, sequenceId2);
            Assert.That(scheduled.ToJSON().ContainsKey("progress"),    Is.False);
            S2SchemaValidator.AssertValidType(scheduled.ToJSON(), "PPBC.PowerSequenceContainerStatus");
            Assert.That(scheduled, Is.Not.EqualTo(status));

        }

        [Test]
        public void PowerSequenceContainerStatus_MissingMandatoryProperty_Fails()
        {

            var missingStatus = JObject.Parse("""
                {
                  "power_profile_id":      "4a2b3c4d-0000-4000-8000-000000000001",
                  "sequence_container_id": "3a2b3c4d-0000-4000-8000-000000000001"
                }
                """);
            Assert.That(PPBC_PowerSequenceContainerStatus.TryParse(missingStatus, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("status"));

            var missingContainerId = JObject.Parse("""
                {
                  "power_profile_id": "4a2b3c4d-0000-4000-8000-000000000001",
                  "status":           "NOT_SCHEDULED"
                }
                """);
            Assert.That(PPBC_PowerSequenceContainerStatus.TryParse(missingContainerId, out _, out error), Is.False);
            Assert.That(error, Does.Contain("sequence_container_id"));

            var missingProfileId = JObject.Parse("""
                {
                  "sequence_container_id": "3a2b3c4d-0000-4000-8000-000000000001",
                  "status":                "NOT_SCHEDULED"
                }
                """);
            Assert.That(PPBC_PowerSequenceContainerStatus.TryParse(missingProfileId, out _, out error), Is.False);
            Assert.That(error, Does.Contain("power_profile_id"));

        }

        [Test]
        public void PowerSequenceContainerStatus_Strict_RejectsAdditionalProperty()
        {

            var json = JObject.Parse("""
                {
                  "power_profile_id":      "4a2b3c4d-0000-4000-8000-000000000001",
                  "sequence_container_id": "3a2b3c4d-0000-4000-8000-000000000001",
                  "status":                "FINISHED",
                  "foo":                   1
                }
                """);

            Assert.That(PPBC_PowerSequenceContainerStatus.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PPBC_PowerSequenceContainerStatus.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void PowerSequenceContainerStatus_RejectsUnknownStatus_AndInvalidProgress()
        {

            var unknownStatus = JObject.Parse("""
                {
                  "power_profile_id":      "4a2b3c4d-0000-4000-8000-000000000001",
                  "sequence_container_id": "3a2b3c4d-0000-4000-8000-000000000001",
                  "status":                "PAUSED"
                }
                """);
            Assert.That(PPBC_PowerSequenceContainerStatus.TryParse(unknownStatus, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("not a known value"));

            var negativeProgress = JObject.Parse("""
                {
                  "power_profile_id":      "4a2b3c4d-0000-4000-8000-000000000001",
                  "sequence_container_id": "3a2b3c4d-0000-4000-8000-000000000001",
                  "selected_sequence_id":  "2a2b3c4d-0000-4000-8000-000000000001",
                  "progress":              -1,
                  "status":                "EXECUTING"
                }
                """);
            Assert.That(PPBC_PowerSequenceContainerStatus.TryParse(negativeProgress, out _, out error), Is.False);
            Assert.That(error, Does.Contain("progress"));

        }

        #endregion

    }

}

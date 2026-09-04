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

namespace cloud.charging.open.protocols.S2.Tests.Messages.PPBC
{

    /// <summary>
    /// Tests of the PPBC messages: PPBC.PowerProfileDefinition, PPBC.PowerProfileStatus,
    /// PPBC.ScheduleInstruction, PPBC.StartInterruptionInstruction and PPBC.EndInterruptionInstruction.
    /// </summary>
    [TestFixture]
    public sealed class PPBCMessageTests
    {

        #region Data

        // An EV charger offering two power profile alternatives (all values in W): the first
        // container charges right away (full or reduced), the second one an hour later.
        // The S2 documentation has no PPBC example, so the values follow the FRBC EV example.

        private static readonly Message_Id                 messageId       = Message_Id.               Parse("1a2b3c4d-0000-4000-8000-000000000001");
        private static readonly Instruction_Id             instructionId   = Instruction_Id.           Parse("5a2b3c4d-0000-4000-8000-000000000001");
        private static readonly PowerSequence_Id           sequenceId1     = PowerSequence_Id.         Parse("2a2b3c4d-0000-4000-8000-000000000001");
        private static readonly PowerSequence_Id           sequenceId2     = PowerSequence_Id.         Parse("2a2b3c4d-0000-4000-8000-000000000002");
        private static readonly PowerSequenceContainer_Id  containerId1    = PowerSequenceContainer_Id.Parse("3a2b3c4d-0000-4000-8000-000000000001");
        private static readonly PowerSequenceContainer_Id  containerId2    = PowerSequenceContainer_Id.Parse("3a2b3c4d-0000-4000-8000-000000000002");
        private static readonly PowerProfileDefinition_Id  profileId       = PowerProfileDefinition_Id.Parse("4a2b3c4d-0000-4000-8000-000000000001");

        private static readonly DateTimeOffset             startTime       = new (2024, 1, 1,  8, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset             endTime         = new (2024, 1, 1, 18, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset             executionTime   = new (2024, 1, 1,  9, 0, 0, TimeSpan.Zero);

        private static PowerForecastValue Power3Phase(Double ValueExpected)
            => new (ValueExpected, CommodityQuantity.ElectricPower3PhaseSymmetric);

        private static PPBC_PowerSequenceElement RampUp()
            => new (Duration.FromMilliseconds(3000),    [ Power3Phase(1400) ]);

        private static PPBC_PowerSequenceElement FullPower()
            => new (Duration.FromMilliseconds(3600000), [ Power3Phase(11000) ]);

        private static PPBC_PowerSequenceElement ReducedPower()
            => new (Duration.FromMilliseconds(1800000), [ Power3Phase(5500) ]);

        private static PPBC_PowerSequence FullSequence()
            => new (sequenceId1, [ RampUp(), FullPower() ],    true,  false, Duration.FromMilliseconds(600000));

        private static PPBC_PowerSequence ReducedSequence()
            => new (sequenceId2, [ RampUp(), ReducedPower() ], false, false);

        private static PPBC_PowerSequenceContainer Container1()
            => new (containerId1, [ FullSequence(), ReducedSequence() ]);

        private static PPBC_PowerSequenceContainer Container2()
            => new (containerId2, [ ReducedSequence() ]);

        private static PPBC_PowerProfileDefinition FullDefinition()
            => new (profileId, startTime, endTime, [ Container1(), Container2() ], messageId);

        private static PPBC_ScheduleInstruction ScheduleInstruction()
            => new (instructionId, profileId, containerId1, sequenceId1, executionTime, false, messageId);

        private static PPBC_StartInterruptionInstruction StartInterruptionInstruction()
            => new (instructionId, profileId, containerId1, sequenceId1, executionTime, true,  messageId);

        private static PPBC_EndInterruptionInstruction EndInterruptionInstruction()
            => new (instructionId, profileId, containerId1, sequenceId1, executionTime, false, messageId);

        private static String Keys(JObject JSON)
            => String.Join(",", JSON.Properties().Select(p => p.Name));

        private const String instructionKeys = "message_type,message_id,id,power_profile_id,sequence_container_id,power_sequence_id,execution_time,abnormal_condition";

        #endregion


        #region PPBC_PowerProfileDefinition

        [Test]
        public void PowerProfileDefinition_RoundTrips_AndIsSchemaValid()
        {

            var definition = FullDefinition();
            var json       = definition.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "PPBC.PowerProfileDefinition");

            Assert.That(Keys(json),                                     Is.EqualTo("message_type,message_id,id,start_time,end_time,power_sequences_containers"));
            Assert.That(json["message_type"]?.Value<String>(),          Is.EqualTo("PPBC.PowerProfileDefinition"));
            Assert.That(json["message_id"]?.  Value<String>(),          Is.EqualTo("1a2b3c4d-0000-4000-8000-000000000001"));
            Assert.That(json["start_time"]?.  Value<String>(),          Is.EqualTo("2024-01-01T08:00:00Z"));
            Assert.That(json["end_time"]?.    Value<String>(),          Is.EqualTo("2024-01-01T18:00:00Z"));
            Assert.That(((JArray) json["power_sequences_containers"]!).Count, Is.EqualTo(2));

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                         Is.EqualTo(definition));
            Assert.That(parsed!.MessageId,                              Is.EqualTo(messageId));
            Assert.That(parsed. MessageType,                            Is.EqualTo(PPBC_PowerProfileDefinition.MessageTypeName));
            Assert.That(parsed. Id,                                     Is.EqualTo(profileId));
            Assert.That(parsed. StartTime,                              Is.EqualTo(startTime));
            Assert.That(parsed. EndTime,                                Is.EqualTo(endTime));
            Assert.That(parsed. PowerSequencesContainers,               Has.Count.EqualTo(2));
            Assert.That(parsed. PowerSequencesContainers[0],            Is.EqualTo(Container1()));
            Assert.That(parsed. PowerSequencesContainers[1].Id,         Is.EqualTo(containerId2));
            Assert.That(parsed. RevokableObjectType,                    Is.EqualTo(RevokableObject.PPBC_PowerProfileDefinition));
            Assert.That(parsed. RevokableObjectId.ToString(),           Is.EqualTo(profileId.ToString()));
            Assert.That(parsed. GetHashCode(),                          Is.EqualTo(definition.GetHashCode()));
            Assert.That(parsed. Clone(),                                Is.EqualTo(definition));
            Assert.That(parsed. ToString(),                             Does.Contain(profileId.ToString()).And.EndWith($"[{messageId}]"));

            Assert.That(parsed, Is.InstanceOf<IRevokable>());
            Assert.That(parsed, Is.InstanceOf<IS2MessageWithId>());

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

        }

        [Test]
        public void PowerProfileDefinition_HandWrittenJSON_Parses_AndReserialises()
        {

            var json = JObject.Parse("""
                {
                  "message_type": "PPBC.PowerProfileDefinition",
                  "message_id":   "1a2b3c4d-0000-4000-8000-000000000001",
                  "id":           "4a2b3c4d-0000-4000-8000-000000000001",
                  "start_time":   "2024-01-01T08:00:00Z",
                  "end_time":     "2024-01-01T18:00:00Z",
                  "power_sequences_containers": [
                    {
                      "id": "3a2b3c4d-0000-4000-8000-000000000002",
                      "power_sequences": [
                        {
                          "id": "2a2b3c4d-0000-4000-8000-000000000002",
                          "elements": [
                            { "duration": 3000,    "power_values": [ { "value_expected": 1400.0, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" } ] },
                            { "duration": 1800000, "power_values": [ { "value_expected": 5500.0, "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC" } ] }
                          ],
                          "is_interruptible":        false,
                          "abnormal_condition_only": false
                        }
                      ]
                    }
                  ]
                }
                """);

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out var definition, out var error), Is.True, error);
            Assert.That(definition!.PowerSequencesContainers,                              Has.Count.EqualTo(1));
            Assert.That(definition. PowerSequencesContainers[0],                           Is.EqualTo(Container2()));
            Assert.That(definition,                                                        Is.EqualTo(new PPBC_PowerProfileDefinition(profileId, startTime, endTime, [ Container2() ], messageId)));

            var reserialised = definition.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "PPBC.PowerProfileDefinition");

            Assert.That(PPBC_PowerProfileDefinition.TryParse(reserialised, out var reparsed, out error), Is.True, error);
            Assert.That(reparsed, Is.EqualTo(definition));

        }

        [Test]
        public void PowerProfileDefinition_MandatoryOnly_IsSchemaValid()
        {

            var definition = new PPBC_PowerProfileDefinition(profileId, startTime, startTime, [ Container2() ]);
            var json       = definition.ToJSON();

            Assert.That(Keys(json), Is.EqualTo("message_type,message_id,id,start_time,end_time,power_sequences_containers"));
            Assert.That(json["power_sequences_containers"]![0]!["power_sequences"]![0]!["max_pause_before"], Is.Null);

            S2SchemaValidator.AssertValidMessage(json, "PPBC.PowerProfileDefinition");

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,            Is.EqualTo(definition));
            Assert.That(parsed!.MessageId, Is.EqualTo(definition.MessageId));
            Assert.That(parsed,            Is.Not.EqualTo(FullDefinition()));

        }

        [Test]
        public void PowerProfileDefinition_MissingMandatoryProperty_Fails()
        {

            var json = FullDefinition().ToJSON();
            json.Remove("power_sequences_containers");

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("power_sequences_containers"));

            json = FullDefinition().ToJSON();
            json.Remove("end_time");

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("end_time"));

            json = FullDefinition().ToJSON();
            json.Remove("message_id");

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("message_id"));

        }

        [Test]
        public void PowerProfileDefinition_Strict_RejectsAdditionalProperty()
        {

            var json = FullDefinition().ToJSON();
            json.Add("foo", 1);

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void PowerProfileDefinition_RejectsWrongMessageType()
        {

            var json = FullDefinition().ToJSON();
            json["message_type"] = "PPBC.PowerProfileStatus";

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("Unexpected message type"));

        }

        [Test]
        public void PowerProfileDefinition_RejectsEndTimeBeforeStartTime()
        {

            Assert.That(() => new PPBC_PowerProfileDefinition(profileId, endTime, startTime, [ Container2() ]),
                        Throws.ArgumentException);

            var json = FullDefinition().ToJSON();
            json["start_time"] = "2024-01-01T18:00:00Z";
            json["end_time"]   = "2024-01-01T17:59:59Z";

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("end time"));
            Assert.That(error, Does.Contain("start time"));

        }

        [Test]
        public void PowerProfileDefinition_RejectsDuplicateContainerIds()
        {

            Assert.That(() => new PPBC_PowerProfileDefinition(profileId, startTime, endTime, [ Container1(), new PPBC_PowerSequenceContainer(containerId1, [ ReducedSequence() ]) ]),
                        Throws.ArgumentException);

            var json = new PPBC_PowerProfileDefinition(profileId, startTime, endTime, [ Container2() ]).ToJSON();
            ((JArray) json["power_sequences_containers"]!).Add(Container2().ToJSON());

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("unique"));
            Assert.That(error, Does.Contain(containerId2.ToString()));

        }

        [Test]
        public void PowerProfileDefinition_RejectsEmptyContainers_AndInvalidNestedContainers()
        {

            Assert.That(() => new PPBC_PowerProfileDefinition(profileId, startTime, endTime, []), Throws.ArgumentException);

            var json = FullDefinition().ToJSON();
            json["power_sequences_containers"] = new JArray();

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at least 1"));

            // The semantic rule of the nested container (unique sequence ids) surfaces through the message parser.
            json = FullDefinition().ToJSON();
            ((JArray) json["power_sequences_containers"]![0]!["power_sequences"]!).Add(FullSequence().ToJSON());

            Assert.That(PPBC_PowerProfileDefinition.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("power_sequences_containers"));
            Assert.That(error, Does.Contain("unique"));

        }

        #endregion

        #region PPBC_PowerProfileStatus

        [Test]
        public void PowerProfileStatus_RoundTrips_AndIsSchemaValid()
        {

            var status = new PPBC_PowerProfileStatus(
                             [
                                 new PPBC_PowerSequenceContainerStatus(profileId, containerId1, PPBC_PowerSequenceStatus.Executing, sequenceId1, Duration.FromMilliseconds(120000)),
                                 new PPBC_PowerSequenceContainerStatus(profileId, containerId2, PPBC_PowerSequenceStatus.NotScheduled)
                             ],
                             messageId
                         );
            var json   = status.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "PPBC.PowerProfileStatus");

            Assert.That(Keys(json),                                     Is.EqualTo("message_type,message_id,sequence_container_status"));
            Assert.That(json["message_type"]?.Value<String>(),          Is.EqualTo("PPBC.PowerProfileStatus"));
            Assert.That(((JArray) json["sequence_container_status"]!).Count, Is.EqualTo(2));

            Assert.That(PPBC_PowerProfileStatus.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                         Is.EqualTo(status));
            Assert.That(parsed!.MessageId,                              Is.EqualTo(messageId));
            Assert.That(parsed. SequenceContainerStatus,                Has.Count.EqualTo(2));
            Assert.That(parsed. SequenceContainerStatus[0].Status,      Is.EqualTo(PPBC_PowerSequenceStatus.Executing));
            Assert.That(parsed. SequenceContainerStatus[0].Progress,    Is.EqualTo(Duration.FromMilliseconds(120000)));
            Assert.That(parsed. SequenceContainerStatus[1].SelectedSequenceId, Is.Null);
            Assert.That(parsed. GetHashCode(),                          Is.EqualTo(status.GetHashCode()));
            Assert.That(parsed. Clone(),                                Is.EqualTo(status));
            Assert.That(parsed. ToString(),                             Does.Contain("2 container status").And.EndWith($"[{messageId}]"));

            Assert.That(PPBC_PowerProfileStatus.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

        }

        [Test]
        public void PowerProfileStatus_HandWrittenJSON_MatchesReserialisation()
        {

            var json = JObject.Parse("""
                {
                  "message_type": "PPBC.PowerProfileStatus",
                  "message_id":   "1a2b3c4d-0000-4000-8000-000000000001",
                  "sequence_container_status": [
                    {
                      "power_profile_id":      "4a2b3c4d-0000-4000-8000-000000000001",
                      "sequence_container_id": "3a2b3c4d-0000-4000-8000-000000000001",
                      "selected_sequence_id":  "2a2b3c4d-0000-4000-8000-000000000001",
                      "status":                "SCHEDULED"
                    },
                    {
                      "power_profile_id":      "4a2b3c4d-0000-4000-8000-000000000001",
                      "sequence_container_id": "3a2b3c4d-0000-4000-8000-000000000002",
                      "status":                "NOT_SCHEDULED"
                    }
                  ]
                }
                """);

            Assert.That(PPBC_PowerProfileStatus.TryParse(json, out var status, out var error), Is.True, error);
            Assert.That(status!.SequenceContainerStatus[0].SelectedSequenceId, Is.EqualTo(sequenceId1));
            Assert.That(status. SequenceContainerStatus[0].Status,             Is.EqualTo(PPBC_PowerSequenceStatus.Scheduled));

            var reserialised = status.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "PPBC.PowerProfileStatus");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void PowerProfileStatus_MandatoryOnly_IsSchemaValid()
        {

            var status = new PPBC_PowerProfileStatus([ new PPBC_PowerSequenceContainerStatus(profileId, containerId1, PPBC_PowerSequenceStatus.NotScheduled) ]);
            var json   = status.ToJSON();

            Assert.That(Keys(json), Is.EqualTo("message_type,message_id,sequence_container_status"));
            Assert.That(json["sequence_container_status"]![0]!["selected_sequence_id"], Is.Null);
            Assert.That(json["sequence_container_status"]![0]!["progress"],             Is.Null);

            S2SchemaValidator.AssertValidMessage(json, "PPBC.PowerProfileStatus");

            Assert.That(PPBC_PowerProfileStatus.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed, Is.EqualTo(status));

        }

        [Test]
        public void PowerProfileStatus_MissingMandatoryProperty_Fails()
        {

            var json = JObject.Parse("""{ "message_type": "PPBC.PowerProfileStatus", "message_id": "1a2b3c4d-0000-4000-8000-000000000001" }""");

            Assert.That(PPBC_PowerProfileStatus.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("sequence_container_status"));

        }

        [Test]
        public void PowerProfileStatus_Strict_RejectsAdditionalProperty()
        {

            var json = new PPBC_PowerProfileStatus([ new PPBC_PowerSequenceContainerStatus(profileId, containerId1, PPBC_PowerSequenceStatus.Finished) ]).ToJSON();
            json.Add("foo", 1);

            Assert.That(PPBC_PowerProfileStatus.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PPBC_PowerProfileStatus.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void PowerProfileStatus_RejectsEmptyStatusList()
        {

            Assert.That(() => new PPBC_PowerProfileStatus([]), Throws.ArgumentException);

            var json = JObject.Parse("""{ "message_type": "PPBC.PowerProfileStatus", "message_id": "1a2b3c4d-0000-4000-8000-000000000001", "sequence_container_status": [] }""");

            Assert.That(PPBC_PowerProfileStatus.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at least 1"));

        }

        #endregion

        #region PPBC_ScheduleInstruction

        [Test]
        public void ScheduleInstruction_RoundTrips_AndIsSchemaValid()
        {

            var instruction = ScheduleInstruction();
            var json        = instruction.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "PPBC.ScheduleInstruction");

            Assert.That(Keys(json),                                     Is.EqualTo(instructionKeys));
            Assert.That(json["message_type"]?.      Value<String>(),    Is.EqualTo("PPBC.ScheduleInstruction"));
            Assert.That(json["execution_time"]?.    Value<String>(),    Is.EqualTo("2024-01-01T09:00:00Z"));
            Assert.That(json["abnormal_condition"]?.Value<Boolean>(),   Is.False);

            Assert.That(PPBC_ScheduleInstruction.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                         Is.EqualTo(instruction));
            Assert.That(parsed!.MessageId,                              Is.EqualTo(messageId));
            Assert.That(parsed. Id,                                     Is.EqualTo(instructionId));
            Assert.That(parsed. PowerProfileId,                         Is.EqualTo(profileId));
            Assert.That(parsed. SequenceContainerId,                    Is.EqualTo(containerId1));
            Assert.That(parsed. PowerSequenceId,                        Is.EqualTo(sequenceId1));
            Assert.That(parsed. ExecutionTime,                          Is.EqualTo(executionTime));
            Assert.That(parsed. AbnormalCondition,                      Is.False);
            Assert.That(parsed. RevokableObjectType,                    Is.EqualTo(RevokableObject.PPBC_ScheduleInstruction));
            Assert.That(parsed. RevokableObjectId.ToString(),           Is.EqualTo(instructionId.ToString()));
            Assert.That(parsed. GetHashCode(),                          Is.EqualTo(instruction.GetHashCode()));
            Assert.That(parsed. Clone(),                                Is.EqualTo(instruction));
            Assert.That(parsed. ToString(),                             Does.Contain(instructionId.ToString()).And.EndWith($"[{messageId}]"));

            Assert.That(parsed, Is.InstanceOf<IInstruction>());
            Assert.That(((IInstruction) parsed).ExecutionTime,          Is.EqualTo(executionTime));

            Assert.That(PPBC_ScheduleInstruction.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

        }

        [Test]
        public void ScheduleInstruction_HandWrittenJSON_MatchesReserialisation()
        {

            var json = JObject.Parse("""
                {
                  "message_type":          "PPBC.ScheduleInstruction",
                  "message_id":            "1a2b3c4d-0000-4000-8000-000000000001",
                  "id":                    "5a2b3c4d-0000-4000-8000-000000000001",
                  "power_profile_id":      "4a2b3c4d-0000-4000-8000-000000000001",
                  "sequence_container_id": "3a2b3c4d-0000-4000-8000-000000000001",
                  "power_sequence_id":     "2a2b3c4d-0000-4000-8000-000000000001",
                  "execution_time":        "2024-01-01T09:00:00Z",
                  "abnormal_condition":    false
                }
                """);

            Assert.That(PPBC_ScheduleInstruction.TryParse(json, out var instruction, out var error), Is.True, error);
            Assert.That(instruction, Is.EqualTo(ScheduleInstruction()));

            var reserialised = instruction!.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "PPBC.ScheduleInstruction");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void ScheduleInstruction_MandatoryOnly_GetsAMessageId()
        {

            var instruction = new PPBC_ScheduleInstruction(instructionId, profileId, containerId1, sequenceId1, executionTime, true);
            var json        = instruction.ToJSON();

            Assert.That(Keys(json),                                     Is.EqualTo(instructionKeys));
            Assert.That(instruction.MessageId,                          Is.Not.EqualTo(Message_Id.Null));
            Assert.That(json["abnormal_condition"]?.Value<Boolean>(),   Is.True);

            S2SchemaValidator.AssertValidMessage(json, "PPBC.ScheduleInstruction");

            Assert.That(PPBC_ScheduleInstruction.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed, Is.EqualTo(instruction));
            Assert.That(parsed, Is.Not.EqualTo(ScheduleInstruction()));

        }

        [Test]
        public void ScheduleInstruction_MissingMandatoryProperty_Fails()
        {

            foreach (var key in new[] { "id", "power_profile_id", "sequence_container_id", "power_sequence_id", "execution_time", "abnormal_condition" })
            {

                var json = ScheduleInstruction().ToJSON();
                json.Remove(key);

                Assert.That(PPBC_ScheduleInstruction.TryParse(json, out _, out var error), Is.False, key);
                Assert.That(error, Does.Contain(key));

            }

        }

        [Test]
        public void ScheduleInstruction_Strict_RejectsAdditionalProperty()
        {

            var json = ScheduleInstruction().ToJSON();
            json.Add("foo", 1);

            Assert.That(PPBC_ScheduleInstruction.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PPBC_ScheduleInstruction.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void ScheduleInstruction_Strict_RejectsNaiveExecutionTime()
        {

            var json = ScheduleInstruction().ToJSON();
            json["execution_time"] = "2024-01-01T09:00:00";

            Assert.That(PPBC_ScheduleInstruction.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PPBC_ScheduleInstruction.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("execution_time").Or.Contain("offset"));

        }

        #endregion

        #region PPBC_StartInterruptionInstruction

        [Test]
        public void StartInterruptionInstruction_RoundTrips_AndIsSchemaValid()
        {

            var instruction = StartInterruptionInstruction();
            var json        = instruction.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "PPBC.StartInterruptionInstruction");

            Assert.That(Keys(json),                                     Is.EqualTo(instructionKeys));
            Assert.That(json["message_type"]?.      Value<String>(),    Is.EqualTo("PPBC.StartInterruptionInstruction"));
            Assert.That(json["abnormal_condition"]?.Value<Boolean>(),   Is.True);

            Assert.That(PPBC_StartInterruptionInstruction.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                         Is.EqualTo(instruction));
            Assert.That(parsed!.MessageId,                              Is.EqualTo(messageId));
            Assert.That(parsed. Id,                                     Is.EqualTo(instructionId));
            Assert.That(parsed. PowerProfileId,                         Is.EqualTo(profileId));
            Assert.That(parsed. SequenceContainerId,                    Is.EqualTo(containerId1));
            Assert.That(parsed. PowerSequenceId,                        Is.EqualTo(sequenceId1));
            Assert.That(parsed. ExecutionTime,                          Is.EqualTo(executionTime));
            Assert.That(parsed. AbnormalCondition,                      Is.True);
            Assert.That(parsed. RevokableObjectType,                    Is.EqualTo(RevokableObject.PPBC_StartInterruptionInstruction));
            Assert.That(parsed. RevokableObjectId.ToString(),           Is.EqualTo(instructionId.ToString()));
            Assert.That(parsed. GetHashCode(),                          Is.EqualTo(instruction.GetHashCode()));
            Assert.That(parsed. Clone(),                                Is.EqualTo(instruction));
            Assert.That(parsed. ToString(),                             Does.Contain("abnormal condition").And.EndWith($"[{messageId}]"));

            Assert.That(parsed, Is.InstanceOf<IInstruction>());

            Assert.That(PPBC_StartInterruptionInstruction.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

        }

        [Test]
        public void StartInterruptionInstruction_HandWrittenJSON_MatchesReserialisation()
        {

            var json = JObject.Parse("""
                {
                  "message_type":          "PPBC.StartInterruptionInstruction",
                  "message_id":            "1a2b3c4d-0000-4000-8000-000000000001",
                  "id":                    "5a2b3c4d-0000-4000-8000-000000000001",
                  "power_profile_id":      "4a2b3c4d-0000-4000-8000-000000000001",
                  "sequence_container_id": "3a2b3c4d-0000-4000-8000-000000000001",
                  "power_sequence_id":     "2a2b3c4d-0000-4000-8000-000000000001",
                  "execution_time":        "2024-01-01T09:00:00Z",
                  "abnormal_condition":    true
                }
                """);

            Assert.That(PPBC_StartInterruptionInstruction.TryParse(json, out var instruction, out var error), Is.True, error);
            Assert.That(instruction, Is.EqualTo(StartInterruptionInstruction()));

            var reserialised = instruction!.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "PPBC.StartInterruptionInstruction");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void StartInterruptionInstruction_MandatoryOnly_GetsAMessageId()
        {

            var instruction = new PPBC_StartInterruptionInstruction(instructionId, profileId, containerId1, sequenceId1, executionTime, false);
            var json        = instruction.ToJSON();

            Assert.That(Keys(json),            Is.EqualTo(instructionKeys));
            Assert.That(instruction.MessageId, Is.Not.EqualTo(Message_Id.Null));

            S2SchemaValidator.AssertValidMessage(json, "PPBC.StartInterruptionInstruction");

            Assert.That(PPBC_StartInterruptionInstruction.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed, Is.EqualTo(instruction));

        }

        [Test]
        public void StartInterruptionInstruction_MissingMandatoryProperty_Fails()
        {

            foreach (var key in new[] { "id", "power_profile_id", "sequence_container_id", "power_sequence_id", "execution_time", "abnormal_condition" })
            {

                var json = StartInterruptionInstruction().ToJSON();
                json.Remove(key);

                Assert.That(PPBC_StartInterruptionInstruction.TryParse(json, out _, out var error), Is.False, key);
                Assert.That(error, Does.Contain(key));

            }

        }

        [Test]
        public void StartInterruptionInstruction_Strict_RejectsAdditionalProperty()
        {

            var json = StartInterruptionInstruction().ToJSON();
            json.Add("foo", 1);

            Assert.That(PPBC_StartInterruptionInstruction.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PPBC_StartInterruptionInstruction.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void StartInterruptionInstruction_RejectsOtherInstructionTypes()
        {

            // The three PPBC instructions share the same shape and differ only in their message type.
            Assert.That(PPBC_StartInterruptionInstruction.TryParse(ScheduleInstruction().ToJSON(), out _, out var error), Is.False);
            Assert.That(error, Does.Contain("Unexpected message type"));

            Assert.That(PPBC_StartInterruptionInstruction.TryParse(EndInterruptionInstruction().ToJSON(), out _, out error), Is.False);
            Assert.That(error, Does.Contain("Unexpected message type"));

        }

        #endregion

        #region PPBC_EndInterruptionInstruction

        [Test]
        public void EndInterruptionInstruction_RoundTrips_AndIsSchemaValid()
        {

            var instruction = EndInterruptionInstruction();
            var json        = instruction.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "PPBC.EndInterruptionInstruction");

            Assert.That(Keys(json),                                     Is.EqualTo(instructionKeys));
            Assert.That(json["message_type"]?.Value<String>(),          Is.EqualTo("PPBC.EndInterruptionInstruction"));

            Assert.That(PPBC_EndInterruptionInstruction.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                         Is.EqualTo(instruction));
            Assert.That(parsed!.MessageId,                              Is.EqualTo(messageId));
            Assert.That(parsed. Id,                                     Is.EqualTo(instructionId));
            Assert.That(parsed. PowerProfileId,                         Is.EqualTo(profileId));
            Assert.That(parsed. SequenceContainerId,                    Is.EqualTo(containerId1));
            Assert.That(parsed. PowerSequenceId,                        Is.EqualTo(sequenceId1));
            Assert.That(parsed. ExecutionTime,                          Is.EqualTo(executionTime));
            Assert.That(parsed. AbnormalCondition,                      Is.False);
            Assert.That(parsed. RevokableObjectType,                    Is.EqualTo(RevokableObject.PPBC_EndInterruptionInstruction));
            Assert.That(parsed. RevokableObjectId.ToString(),           Is.EqualTo(instructionId.ToString()));
            Assert.That(parsed. GetHashCode(),                          Is.EqualTo(instruction.GetHashCode()));
            Assert.That(parsed. Clone(),                                Is.EqualTo(instruction));
            Assert.That(parsed. ToString(),                             Does.Contain(instructionId.ToString()).And.EndWith($"[{messageId}]"));

            Assert.That(parsed, Is.InstanceOf<IInstruction>());

            Assert.That(PPBC_EndInterruptionInstruction.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.True, error);

        }

        [Test]
        public void EndInterruptionInstruction_HandWrittenJSON_MatchesReserialisation()
        {

            var json = JObject.Parse("""
                {
                  "message_type":          "PPBC.EndInterruptionInstruction",
                  "message_id":            "1a2b3c4d-0000-4000-8000-000000000001",
                  "id":                    "5a2b3c4d-0000-4000-8000-000000000001",
                  "power_profile_id":      "4a2b3c4d-0000-4000-8000-000000000001",
                  "sequence_container_id": "3a2b3c4d-0000-4000-8000-000000000001",
                  "power_sequence_id":     "2a2b3c4d-0000-4000-8000-000000000001",
                  "execution_time":        "2024-01-01T09:00:00Z",
                  "abnormal_condition":    false
                }
                """);

            Assert.That(PPBC_EndInterruptionInstruction.TryParse(json, out var instruction, out var error), Is.True, error);
            Assert.That(instruction, Is.EqualTo(EndInterruptionInstruction()));

            var reserialised = instruction!.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "PPBC.EndInterruptionInstruction");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void EndInterruptionInstruction_MandatoryOnly_GetsAMessageId()
        {

            var instruction = new PPBC_EndInterruptionInstruction(instructionId, profileId, containerId1, sequenceId1, executionTime, false);
            var json        = instruction.ToJSON();

            Assert.That(Keys(json),            Is.EqualTo(instructionKeys));
            Assert.That(instruction.MessageId, Is.Not.EqualTo(Message_Id.Null));

            S2SchemaValidator.AssertValidMessage(json, "PPBC.EndInterruptionInstruction");

            Assert.That(PPBC_EndInterruptionInstruction.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed, Is.EqualTo(instruction));

        }

        [Test]
        public void EndInterruptionInstruction_MissingMandatoryProperty_Fails()
        {

            foreach (var key in new[] { "id", "power_profile_id", "sequence_container_id", "power_sequence_id", "execution_time", "abnormal_condition" })
            {

                var json = EndInterruptionInstruction().ToJSON();
                json.Remove(key);

                Assert.That(PPBC_EndInterruptionInstruction.TryParse(json, out _, out var error), Is.False, key);
                Assert.That(error, Does.Contain(key));

            }

        }

        [Test]
        public void EndInterruptionInstruction_Strict_RejectsAdditionalProperty()
        {

            var json = EndInterruptionInstruction().ToJSON();
            json.Add("foo", 1);

            Assert.That(PPBC_EndInterruptionInstruction.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PPBC_EndInterruptionInstruction.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void EndInterruptionInstruction_RejectsOtherInstructionTypes()
        {

            Assert.That(PPBC_EndInterruptionInstruction.TryParse(ScheduleInstruction().ToJSON(), out _, out var error), Is.False);
            Assert.That(error, Does.Contain("Unexpected message type"));

            Assert.That(PPBC_EndInterruptionInstruction.TryParse(StartInterruptionInstruction().ToJSON(), out _, out error), Is.False);
            Assert.That(error, Does.Contain("Unexpected message type"));

        }

        #endregion

        #region S2MessageParser

        [Test]
        public void MessageParser_DispatchesPPBCScheduleInstruction()
        {

            var text = ScheduleInstruction().ToJSON().ToString();

            Assert.That(S2MessageParser.TryParse(text, null, out var message, out var error), Is.True, error?.DiagnosticLabel);
            Assert.That(message,                                            Is.TypeOf<PPBC_ScheduleInstruction>());
            Assert.That(message,                                            Is.EqualTo(ScheduleInstruction()));
            Assert.That(((PPBC_ScheduleInstruction) message!).PowerSequenceId, Is.EqualTo(sequenceId1));
            Assert.That(((IS2MessageWithId) message).MessageId,             Is.EqualTo(messageId));

        }

        #endregion

    }

}

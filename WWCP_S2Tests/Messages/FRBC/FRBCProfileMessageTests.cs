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

namespace cloud.charging.open.protocols.S2.Tests.Messages.FRBC
{

    /// <summary>
    /// Tests of the FRBC profile messages: FRBC.TimerStatus, FRBC.FillLevelTargetProfile,
    /// FRBC.LeakageBehaviour and FRBC.UsageForecast. The documentation examples are taken
    /// from the heat pump example of the S2 documentation; the other values follow the EV
    /// charger example (fill level = state of charge in percent, 0..100).
    /// </summary>
    [TestFixture]
    public sealed class FRBCProfileMessageTests
    {

        #region Data

        private static readonly DateTimeOffset  utcTime   = new (2019, 8, 24, 14, 15, 22, TimeSpan.Zero);
        private static readonly DateTimeOffset  cestTime  = new (2019, 8, 24, 16, 15, 22, TimeSpan.FromHours(2));

        private static readonly Message_Id      messageId = Message_Id.Parse("9a8d2f1c-0000-4000-8000-000000000001");

        #endregion

        #region (private) Helpers

        /// <summary>
        /// Replace the "elements" array of the given message JSON by the given number of copies of the given element.
        /// </summary>
        private static JObject WithElements(JObject JSON, JObject Element, Int32 Count)
        {
            JSON["elements"] = new JArray(Enumerable.Range(0, Count).Select(_ => Element.DeepClone()));
            return JSON;
        }

        #endregion


        #region FRBC_TimerStatus

        [Test]
        public void TimerStatus_RoundTrips_AndIsSchemaValid()
        {

            var timerId     = Timer_Id.   Parse("6e1f5e2a-0000-4000-8000-0000000000aa");
            var actuatorId  = Actuator_Id.Parse("6e1f5e2a-0000-4000-8000-0000000000bb");

            var message     = new FRBC_TimerStatus(timerId, actuatorId, cestTime, messageId);
            var json        = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "FRBC.TimerStatus");

            Assert.That(json["message_type"]?.Value<String>(),  Is.EqualTo("FRBC.TimerStatus"));
            Assert.That(json["message_id"]?.  Value<String>(),  Is.EqualTo("9a8d2f1c-0000-4000-8000-000000000001"));
            Assert.That(json["timer_id"]?.    Value<String>(),  Is.EqualTo("6e1f5e2a-0000-4000-8000-0000000000aa"));
            Assert.That(json["actuator_id"]?. Value<String>(),  Is.EqualTo("6e1f5e2a-0000-4000-8000-0000000000bb"));
            Assert.That(json["finished_at"]?. Value<String>(),  Is.EqualTo("2019-08-24T16:15:22+02:00"));

            Assert.That(FRBC_TimerStatus.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                       Is.EqualTo(message));
            Assert.That(parsed!.MessageId,            Is.EqualTo(messageId));
            Assert.That(parsed. TimerId,              Is.EqualTo(timerId));
            Assert.That(parsed. ActuatorId,           Is.EqualTo(actuatorId));
            Assert.That(parsed. FinishedAt,           Is.EqualTo(cestTime));
            Assert.That(parsed. FinishedAt.Offset,    Is.EqualTo(TimeSpan.FromHours(2)));
            Assert.That(parsed. GetHashCode(),        Is.EqualTo(message.GetHashCode()));
            Assert.That(message.Clone(),              Is.EqualTo(message));
            Assert.That(message == parsed,            Is.True);
            Assert.That(message.ToString(),           Does.EndWith($"[{messageId}]"));

            // A new message gets a fresh message identification.
            var another = new FRBC_TimerStatus(timerId, actuatorId, cestTime);
            Assert.That(another.MessageId, Is.Not.EqualTo(messageId));
            Assert.That(another,           Is.Not.EqualTo(message));

        }

        [Test]
        public void TimerStatus_MatchesTheDocumentationExample()
        {

            var json = S2JSONExtensions.ParseS2JSON("""
                {
                  "message_type": "FRBC.TimerStatus",
                  "message_id": "2ebec2c8-5cd4-46bc-9784-ea125543b544",
                  "timer_id": "timer0",
                  "actuator_id": "actuator",
                  "finished_at": "2019-08-24T14:15:22Z"
                }
                """);

            Assert.That(FRBC_TimerStatus.TryParse(json, out var message, out var error), Is.True, error);
            Assert.That(message!.MessageId.ToString(),   Is.EqualTo("2ebec2c8-5cd4-46bc-9784-ea125543b544"));
            Assert.That(message. TimerId.  ToString(),   Is.EqualTo("timer0"));
            Assert.That(message. ActuatorId.ToString(),  Is.EqualTo("actuator"));
            Assert.That(message. FinishedAt,             Is.EqualTo(utcTime));

            var reserialised = message.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "FRBC.TimerStatus");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void TimerStatus_MissingMandatoryProperty_Fails()
        {

            var missingTimerId = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.TimerStatus", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "actuator_id": "actuator", "finished_at": "2019-08-24T14:15:22Z" }""");
            Assert.That(FRBC_TimerStatus.TryParse(missingTimerId, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("timer_id"));

            var missingActuatorId = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.TimerStatus", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "timer_id": "timer0", "finished_at": "2019-08-24T14:15:22Z" }""");
            Assert.That(FRBC_TimerStatus.TryParse(missingActuatorId, out _, out error), Is.False);
            Assert.That(error, Does.Contain("actuator_id"));

            var missingFinishedAt = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.TimerStatus", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "timer_id": "timer0", "actuator_id": "actuator" }""");
            Assert.That(FRBC_TimerStatus.TryParse(missingFinishedAt, out _, out error), Is.False);
            Assert.That(error, Does.Contain("finished_at"));

            var missingMessageId = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.TimerStatus", "timer_id": "timer0", "actuator_id": "actuator", "finished_at": "2019-08-24T14:15:22Z" }""");
            Assert.That(FRBC_TimerStatus.TryParse(missingMessageId, out _, out error), Is.False);
            Assert.That(error, Does.Contain("message_id"));

            var wrongType = S2JSONExtensions.ParseS2JSON("""{ "message_type": "OMBC.TimerStatus", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "timer_id": "timer0", "actuator_id": "actuator", "finished_at": "2019-08-24T14:15:22Z" }""");
            Assert.That(FRBC_TimerStatus.TryParse(wrongType, out _, out error), Is.False);
            Assert.That(error, Does.Contain("Unexpected message type"));

        }

        [Test]
        public void TimerStatus_Strict_RejectsAdditionalProperties()
        {

            var extra = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.TimerStatus", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "timer_id": "6e1f5e2a-0000-4000-8000-0000000000aa", "actuator_id": "6e1f5e2a-0000-4000-8000-0000000000bb", "finished_at": "2019-08-24T14:15:22Z", "foo": 1 }""");

            Assert.That(FRBC_TimerStatus.TryParse(extra, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_TimerStatus.TryParse(extra, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        #endregion

        #region FRBC_FillLevelTargetProfile

        [Test]
        public void FillLevelTargetProfile_RoundTrips_AndIsSchemaValid()
        {

            var elements = new List<FRBC_FillLevelTargetProfileElement> {
                               new (Duration.FromMilliseconds(3600000), new NumberRange(50,  70)),
                               new (Duration.FromMilliseconds(7200000), new NumberRange(80, 100))
                           };

            var message  = new FRBC_FillLevelTargetProfile(cestTime, elements, messageId);
            var json     = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "FRBC.FillLevelTargetProfile");

            Assert.That(json["message_type"]?.Value<String>(),                                Is.EqualTo("FRBC.FillLevelTargetProfile"));
            Assert.That(json["message_id"]?.  Value<String>(),                                Is.EqualTo("9a8d2f1c-0000-4000-8000-000000000001"));
            Assert.That(json["start_time"]?.  Value<String>(),                                Is.EqualTo("2019-08-24T16:15:22+02:00"));
            Assert.That(json["elements"]?.    Count(),                                        Is.EqualTo(2));
            Assert.That(json["elements"]?[1]?["duration"]?.Value<Int64>(),                    Is.EqualTo(7200000L));
            Assert.That(json["elements"]?[1]?["fill_level_range"]?["end_of_range"]?.Value<Double>(), Is.EqualTo(100));

            Assert.That(FRBC_FillLevelTargetProfile.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                       Is.EqualTo(message));
            Assert.That(parsed!.MessageId,            Is.EqualTo(messageId));
            Assert.That(parsed. StartTime,            Is.EqualTo(cestTime));
            Assert.That(parsed. Elements,             Is.EqualTo(elements));
            Assert.That(parsed. GetHashCode(),        Is.EqualTo(message.GetHashCode()));
            Assert.That(message.Clone(),              Is.EqualTo(message));
            Assert.That(message == parsed,            Is.True);
            Assert.That(message.ToString(),           Does.EndWith($"[{messageId}]"));

            // The stored list is a defensive copy.
            elements.Clear();
            Assert.That(message.Elements.Count, Is.EqualTo(2));

        }

        [Test]
        public void FillLevelTargetProfile_MandatoryOnly_IsSchemaValid()
        {

            var message = new FRBC_FillLevelTargetProfile(
                              utcTime,
                              [ new FRBC_FillLevelTargetProfileElement(Duration.FromSeconds(3600), new NumberRange(80, 100)) ]
                          );

            var json = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "FRBC.FillLevelTargetProfile");

            Assert.That(json.Properties().Select(property => property.Name),
                        Is.EqualTo(new[] { "message_type", "message_id", "start_time", "elements" }));
            Assert.That(json["start_time"]?.Value<String>(), Is.EqualTo("2019-08-24T14:15:22Z"));

            Assert.That(FRBC_FillLevelTargetProfile.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed, Is.EqualTo(message));

        }

        [Test]
        public void FillLevelTargetProfile_MissingMandatoryProperty_Fails()
        {

            var missingStartTime = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.FillLevelTargetProfile", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "elements": [ { "duration": 3600000, "fill_level_range": { "start_of_range": 80, "end_of_range": 100 } } ] }""");
            Assert.That(FRBC_FillLevelTargetProfile.TryParse(missingStartTime, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("start_time"));

            var missingElements = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.FillLevelTargetProfile", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "start_time": "2019-08-24T14:15:22Z" }""");
            Assert.That(FRBC_FillLevelTargetProfile.TryParse(missingElements, out _, out error), Is.False);
            Assert.That(error, Does.Contain("elements"));

            var missingMessageId = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.FillLevelTargetProfile", "start_time": "2019-08-24T14:15:22Z", "elements": [ { "duration": 3600000, "fill_level_range": { "start_of_range": 80, "end_of_range": 100 } } ] }""");
            Assert.That(FRBC_FillLevelTargetProfile.TryParse(missingMessageId, out _, out error), Is.False);
            Assert.That(error, Does.Contain("message_id"));

        }

        [Test]
        public void FillLevelTargetProfile_Strict_RejectsAdditionalProperties()
        {

            var extra = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.FillLevelTargetProfile", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "start_time": "2019-08-24T14:15:22Z", "elements": [ { "duration": 3600000, "fill_level_range": { "start_of_range": 80, "end_of_range": 100 } } ], "foo": 1 }""");

            Assert.That(FRBC_FillLevelTargetProfile.TryParse(extra, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_FillLevelTargetProfile.TryParse(extra, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void FillLevelTargetProfile_RejectsEmptyAndOversizedElements()
        {

            var element = new FRBC_FillLevelTargetProfileElement(Duration.FromSeconds(300), new NumberRange(80, 100));

            Assert.That(() => new FRBC_FillLevelTargetProfile(utcTime, []),                                            Throws.ArgumentException);
            Assert.That(() => new FRBC_FillLevelTargetProfile(utcTime, Enumerable.Repeat(element, 289).ToList()),      Throws.ArgumentException);
            Assert.That(() => new FRBC_FillLevelTargetProfile(utcTime, Enumerable.Repeat(element, 288).ToList()),      Throws.Nothing);

            var valid = new FRBC_FillLevelTargetProfile(utcTime, [ element ], messageId).ToJSON();

            Assert.That(FRBC_FillLevelTargetProfile.TryParse(WithElements(valid, element.ToJSON(),   0), out _, out var error), Is.False);
            Assert.That(error, Does.Contain("elements"));

            Assert.That(FRBC_FillLevelTargetProfile.TryParse(WithElements(valid, element.ToJSON(), 289), out _, out error),     Is.False);
            Assert.That(error, Does.Contain("elements"));

            Assert.That(FRBC_FillLevelTargetProfile.TryParse(WithElements(valid, element.ToJSON(), 288), out var parsed, out error), Is.True, error);
            Assert.That(parsed!.Elements.Count, Is.EqualTo(288));

        }

        #endregion

        #region FRBC_LeakageBehaviour

        [Test]
        public void LeakageBehaviour_RoundTrips_AndIsSchemaValid()
        {

            var elements = new List<FRBC_LeakageBehaviourElement> {
                               new (new NumberRange( 0,  20), 0.0002),
                               new (new NumberRange(20,  80), 0.0001),
                               new (new NumberRange(80, 100), 0.00005)
                           };

            var message  = new FRBC_LeakageBehaviour(cestTime, elements, messageId);
            var json     = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "FRBC.LeakageBehaviour");

            Assert.That(json["message_type"]?.Value<String>(),                              Is.EqualTo("FRBC.LeakageBehaviour"));
            Assert.That(json["message_id"]?.  Value<String>(),                              Is.EqualTo("9a8d2f1c-0000-4000-8000-000000000001"));
            Assert.That(json["valid_from"]?.  Value<String>(),                              Is.EqualTo("2019-08-24T16:15:22+02:00"));
            Assert.That(json["elements"]?.    Count(),                                      Is.EqualTo(3));
            Assert.That(json["elements"]?[2]?["leakage_rate"]?.Value<Double>(),             Is.EqualTo(0.00005));
            Assert.That(json["elements"]?[2]?["fill_level_range"]?["start_of_range"]?.Value<Double>(), Is.EqualTo(80));

            Assert.That(FRBC_LeakageBehaviour.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                       Is.EqualTo(message));
            Assert.That(parsed!.MessageId,            Is.EqualTo(messageId));
            Assert.That(parsed. ValidFrom,            Is.EqualTo(cestTime));
            Assert.That(parsed. Elements,             Is.EqualTo(elements));
            Assert.That(parsed. GetHashCode(),        Is.EqualTo(message.GetHashCode()));
            Assert.That(message.Clone(),              Is.EqualTo(message));
            Assert.That(message == parsed,            Is.True);
            Assert.That(message.ToString(),           Does.EndWith($"[{messageId}]"));

            // The stored list is a defensive copy.
            elements.Clear();
            Assert.That(message.Elements.Count, Is.EqualTo(3));

        }

        [Test]
        public void LeakageBehaviour_MatchesTheDocumentationExample()
        {

            var json = S2JSONExtensions.ParseS2JSON("""
                {
                  "message_type": "FRBC.LeakageBehaviour",
                  "message_id": "82861374-7653-4094-9153-8c59b4fffed4",
                  "valid_from": "2019-08-24T14:15:22Z",
                  "elements": [
                    {
                      "fill_level_range": {
                        "start_of_range": 45,
                        "end_of_range": 55
                      },
                      "leakage_rate": 0.00005
                    }
                  ]
                }
                """);

            Assert.That(FRBC_LeakageBehaviour.TryParse(json, out var message, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(message!.MessageId.ToString(),                       Is.EqualTo("82861374-7653-4094-9153-8c59b4fffed4"));
            Assert.That(message. ValidFrom,                                  Is.EqualTo(utcTime));
            Assert.That(message. Elements.Count,                             Is.EqualTo(1));
            Assert.That(message. Elements[0].FillLevelRange,                 Is.EqualTo(new NumberRange(45, 55)));
            Assert.That(message. Elements[0].LeakageRate,                    Is.EqualTo(0.00005));

            var reserialised = message.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "FRBC.LeakageBehaviour");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void LeakageBehaviour_MandatoryOnly_IsSchemaValid()
        {

            var message = new FRBC_LeakageBehaviour(
                              utcTime,
                              [ new FRBC_LeakageBehaviourElement(new NumberRange(0, 100), 0.0001) ]
                          );

            var json = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "FRBC.LeakageBehaviour");

            Assert.That(json.Properties().Select(property => property.Name),
                        Is.EqualTo(new[] { "message_type", "message_id", "valid_from", "elements" }));
            Assert.That(json["valid_from"]?.Value<String>(), Is.EqualTo("2019-08-24T14:15:22Z"));

            Assert.That(FRBC_LeakageBehaviour.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed, Is.EqualTo(message));

        }

        [Test]
        public void LeakageBehaviour_MissingMandatoryProperty_Fails()
        {

            var missingValidFrom = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.LeakageBehaviour", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "elements": [ { "fill_level_range": { "start_of_range": 0, "end_of_range": 100 }, "leakage_rate": 0.0001 } ] }""");
            Assert.That(FRBC_LeakageBehaviour.TryParse(missingValidFrom, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("valid_from"));

            var missingElements = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.LeakageBehaviour", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "valid_from": "2019-08-24T14:15:22Z" }""");
            Assert.That(FRBC_LeakageBehaviour.TryParse(missingElements, out _, out error), Is.False);
            Assert.That(error, Does.Contain("elements"));

            var missingMessageId = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.LeakageBehaviour", "valid_from": "2019-08-24T14:15:22Z", "elements": [ { "fill_level_range": { "start_of_range": 0, "end_of_range": 100 }, "leakage_rate": 0.0001 } ] }""");
            Assert.That(FRBC_LeakageBehaviour.TryParse(missingMessageId, out _, out error), Is.False);
            Assert.That(error, Does.Contain("message_id"));

        }

        [Test]
        public void LeakageBehaviour_Strict_RejectsAdditionalProperties()
        {

            var extra = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.LeakageBehaviour", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "valid_from": "2019-08-24T14:15:22Z", "elements": [ { "fill_level_range": { "start_of_range": 0, "end_of_range": 100 }, "leakage_rate": 0.0001 } ], "foo": 1 }""");

            Assert.That(FRBC_LeakageBehaviour.TryParse(extra, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_LeakageBehaviour.TryParse(extra, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void LeakageBehaviour_RejectsEmptyAndOversizedElements()
        {

            var element = new FRBC_LeakageBehaviourElement(new NumberRange(0, 100), 0.0001);

            Assert.That(() => new FRBC_LeakageBehaviour(utcTime, []), Throws.ArgumentException);

            // 289 contiguous ranges 0..1, 1..2, ... are too many; 288 are fine.
            var tooMany  = Enumerable.Range(0, 289).Select(i => new FRBC_LeakageBehaviourElement(new NumberRange(i, i + 1), 0.0001)).ToList();
            var justFine = tooMany.Take(288).ToList();

            Assert.That(() => new FRBC_LeakageBehaviour(utcTime, tooMany),  Throws.ArgumentException);
            Assert.That(() => new FRBC_LeakageBehaviour(utcTime, justFine), Throws.Nothing);

            var valid = new FRBC_LeakageBehaviour(utcTime, [ element ], messageId).ToJSON();

            Assert.That(FRBC_LeakageBehaviour.TryParse(WithElements(valid, element.ToJSON(), 0), out _, out var error), Is.False);
            Assert.That(error, Does.Contain("elements"));

            var oversized = new FRBC_LeakageBehaviour(utcTime, [ element ], messageId).ToJSON();
            oversized["elements"] = new JArray(tooMany.Select(e => e.ToJSON()));

            Assert.That(FRBC_LeakageBehaviour.TryParse(oversized, out _, out error), Is.False);
            Assert.That(error, Does.Contain("elements"));

        }

        [Test]
        [S2C("FRBC.LeakageBehaviour.elements.contiguous")]
        public void LeakageBehaviour_RejectsNonContiguousFillLevelRanges()
        {

            // A gap between the ranges...
            Assert.That(() => new FRBC_LeakageBehaviour(utcTime, [
                                  new FRBC_LeakageBehaviourElement(new NumberRange( 0,  50), 0.0002),
                                  new FRBC_LeakageBehaviourElement(new NumberRange(60, 100), 0.0001)
                              ]),
                        Throws.ArgumentException.With.Message.Contains("contiguous"));

            // ...and overlapping ranges are both rejected.
            Assert.That(() => new FRBC_LeakageBehaviour(utcTime, [
                                  new FRBC_LeakageBehaviourElement(new NumberRange( 0,  60), 0.0002),
                                  new FRBC_LeakageBehaviourElement(new NumberRange(50, 100), 0.0001)
                              ]),
                        Throws.ArgumentException.With.Message.Contains("contiguous"));

            // Elements given out of order are fine as long as their ranges are contiguous when sorted;
            // the original order is preserved.
            var unsorted = new FRBC_LeakageBehaviour(utcTime, [
                               new FRBC_LeakageBehaviourElement(new NumberRange(20, 100), 0.0001),
                               new FRBC_LeakageBehaviourElement(new NumberRange( 0,  20), 0.0002)
                           ]);
            Assert.That(unsorted.Elements[0].FillLevelRange.StartOfRange, Is.EqualTo(20));

            var json = S2JSONExtensions.ParseS2JSON("""
                {
                  "message_type": "FRBC.LeakageBehaviour",
                  "message_id": "9a8d2f1c-0000-4000-8000-000000000001",
                  "valid_from": "2019-08-24T14:15:22Z",
                  "elements": [
                    { "fill_level_range": { "start_of_range":  0, "end_of_range":  50 }, "leakage_rate": 0.0002 },
                    { "fill_level_range": { "start_of_range": 60, "end_of_range": 100 }, "leakage_rate": 0.0001 }
                  ]
                }
                """);

            Assert.That(FRBC_LeakageBehaviour.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("contiguous"));

        }

        #endregion

        #region FRBC_UsageForecast

        [Test]
        public void UsageForecast_RoundTrips_AndIsSchemaValid()
        {

            var elements = new List<FRBC_UsageForecastElement> {
                               new (Duration.FromMilliseconds(900000),
                                    0.002,
                                    UsageRateUpperLimit:  0.004,
                                    UsageRateUpper95PPR:  0.0035,
                                    UsageRateUpper68PPR:  0.003,
                                    UsageRateLower68PPR:  0.0015,
                                    UsageRateLower95PPR:  0.001,
                                    UsageRateLowerLimit:  0),
                               new (Duration.FromMilliseconds(1800000),
                                    0.001)
                           };

            var message  = new FRBC_UsageForecast(cestTime, elements, messageId);
            var json     = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "FRBC.UsageForecast");

            Assert.That(json["message_type"]?.Value<String>(),                              Is.EqualTo("FRBC.UsageForecast"));
            Assert.That(json["message_id"]?.  Value<String>(),                              Is.EqualTo("9a8d2f1c-0000-4000-8000-000000000001"));
            Assert.That(json["start_time"]?.  Value<String>(),                              Is.EqualTo("2019-08-24T16:15:22+02:00"));
            Assert.That(json["elements"]?.    Count(),                                      Is.EqualTo(2));
            Assert.That(json["elements"]?[0]?["usage_rate_upper_95PPR"]?.Value<Double>(),   Is.EqualTo(0.0035));
            Assert.That(json["elements"]?[1]?["usage_rate_expected"]?.  Value<Double>(),    Is.EqualTo(0.001));
            Assert.That(((JObject) json["elements"]![1]!).ContainsKey("usage_rate_upper_limit"), Is.False);

            Assert.That(FRBC_UsageForecast.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                       Is.EqualTo(message));
            Assert.That(parsed!.MessageId,            Is.EqualTo(messageId));
            Assert.That(parsed. StartTime,            Is.EqualTo(cestTime));
            Assert.That(parsed. Elements,             Is.EqualTo(elements));
            Assert.That(parsed. GetHashCode(),        Is.EqualTo(message.GetHashCode()));
            Assert.That(message.Clone(),              Is.EqualTo(message));
            Assert.That(message == parsed,            Is.True);
            Assert.That(message.ToString(),           Does.EndWith($"[{messageId}]"));

            // The stored list is a defensive copy.
            elements.Clear();
            Assert.That(message.Elements.Count, Is.EqualTo(2));

        }

        [Test]
        public void UsageForecast_MatchesTheDocumentationExample()
        {

            var json = S2JSONExtensions.ParseS2JSON("""
                {
                  "message_type": "FRBC.UsageForecast",
                  "message_id": "9616e431-5035-404a-bd93-d673a08ebb44",
                  "start_time": "2019-08-24T14:15:22Z",
                  "elements": [
                    {
                      "duration": 0,
                      "usage_rate_upper_limit": 0,
                      "usage_rate_upper_95PPR": 0,
                      "usage_rate_upper_68PPR": 0,
                      "usage_rate_expected": 0,
                      "usage_rate_lower_68PPR": 0,
                      "usage_rate_lower_95PPR": 0,
                      "usage_rate_lower_limit": 0
                    }
                  ]
                }
                """);

            Assert.That(FRBC_UsageForecast.TryParse(json, out var message, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(message!.MessageId.ToString(),                  Is.EqualTo("9616e431-5035-404a-bd93-d673a08ebb44"));
            Assert.That(message. StartTime,                             Is.EqualTo(utcTime));
            Assert.That(message. Elements.Count,                        Is.EqualTo(1));
            Assert.That(message. Elements[0].Duration,                  Is.EqualTo(Duration.Zero));
            Assert.That(message. Elements[0].UsageRateExpected,         Is.EqualTo(0));
            Assert.That(message. Elements[0].UsageRateUpperLimit,       Is.EqualTo(0));
            Assert.That(message. Elements[0].UsageRateLowerLimit,       Is.EqualTo(0));

            var reserialised = message.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "FRBC.UsageForecast");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void UsageForecast_MandatoryOnly_IsSchemaValid()
        {

            var message = new FRBC_UsageForecast(
                              utcTime,
                              [ new FRBC_UsageForecastElement(Duration.FromSeconds(900), 0.002) ]
                          );

            var json = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "FRBC.UsageForecast");

            Assert.That(json.Properties().Select(property => property.Name),
                        Is.EqualTo(new[] { "message_type", "message_id", "start_time", "elements" }));
            Assert.That(json["start_time"]?.Value<String>(), Is.EqualTo("2019-08-24T14:15:22Z"));
            Assert.That(((JObject) json["elements"]![0]!).Properties().Select(property => property.Name),
                        Is.EqualTo(new[] { "duration", "usage_rate_expected" }));

            Assert.That(FRBC_UsageForecast.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed, Is.EqualTo(message));

        }

        [Test]
        public void UsageForecast_MissingMandatoryProperty_Fails()
        {

            var missingStartTime = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.UsageForecast", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "elements": [ { "duration": 900000, "usage_rate_expected": 0.002 } ] }""");
            Assert.That(FRBC_UsageForecast.TryParse(missingStartTime, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("start_time"));

            var missingElements = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.UsageForecast", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "start_time": "2019-08-24T14:15:22Z" }""");
            Assert.That(FRBC_UsageForecast.TryParse(missingElements, out _, out error), Is.False);
            Assert.That(error, Does.Contain("elements"));

            var missingMessageId = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.UsageForecast", "start_time": "2019-08-24T14:15:22Z", "elements": [ { "duration": 900000, "usage_rate_expected": 0.002 } ] }""");
            Assert.That(FRBC_UsageForecast.TryParse(missingMessageId, out _, out error), Is.False);
            Assert.That(error, Does.Contain("message_id"));

            // An invalid element is reported as well.
            var invalidElement = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.UsageForecast", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "start_time": "2019-08-24T14:15:22Z", "elements": [ { "duration": 900000 } ] }""");
            Assert.That(FRBC_UsageForecast.TryParse(invalidElement, out _, out error), Is.False);
            Assert.That(error, Does.Contain("usage_rate_expected"));

        }

        [Test]
        public void UsageForecast_Strict_RejectsAdditionalProperties()
        {

            var extra = S2JSONExtensions.ParseS2JSON("""{ "message_type": "FRBC.UsageForecast", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "start_time": "2019-08-24T14:15:22Z", "elements": [ { "duration": 900000, "usage_rate_expected": 0.002 } ], "foo": 1 }""");

            Assert.That(FRBC_UsageForecast.TryParse(extra, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(FRBC_UsageForecast.TryParse(extra, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void UsageForecast_RejectsEmptyAndOversizedElements()
        {

            var element = new FRBC_UsageForecastElement(Duration.FromSeconds(300), 0.002);

            Assert.That(() => new FRBC_UsageForecast(utcTime, []),                                        Throws.ArgumentException);
            Assert.That(() => new FRBC_UsageForecast(utcTime, Enumerable.Repeat(element, 289).ToList()),  Throws.ArgumentException);
            Assert.That(() => new FRBC_UsageForecast(utcTime, Enumerable.Repeat(element, 288).ToList()),  Throws.Nothing);

            var valid = new FRBC_UsageForecast(utcTime, [ element ], messageId).ToJSON();

            Assert.That(FRBC_UsageForecast.TryParse(WithElements(valid, element.ToJSON(),   0), out _, out var error), Is.False);
            Assert.That(error, Does.Contain("elements"));

            Assert.That(FRBC_UsageForecast.TryParse(WithElements(valid, element.ToJSON(), 289), out _, out error),     Is.False);
            Assert.That(error, Does.Contain("elements"));

            Assert.That(FRBC_UsageForecast.TryParse(WithElements(valid, element.ToJSON(), 288), out var parsed, out error), Is.True, error);
            Assert.That(parsed!.Elements.Count, Is.EqualTo(288));

        }

        #endregion

        #region S2MessageParser

        [Test]
        public void MessageParser_DispatchesLeakageBehaviour()
        {

            var message = new FRBC_LeakageBehaviour(
                              utcTime,
                              [ new FRBC_LeakageBehaviourElement(new NumberRange(45, 55), 0.00005) ],
                              Message_Id.Parse("82861374-7653-4094-9153-8c59b4fffed4")
                          );

            var text = message.ToJSON().ToString();

            Assert.That(S2MessageParser.TryParse(text, null, out var parsed, out var error), Is.True, error?.DiagnosticLabel);
            Assert.That(parsed,                                          Is.TypeOf<FRBC_LeakageBehaviour>());
            Assert.That(parsed!.MessageType,                             Is.EqualTo("FRBC.LeakageBehaviour"));
            Assert.That(((FRBC_LeakageBehaviour) parsed).MessageId,      Is.EqualTo(message.MessageId));
            Assert.That(((FRBC_LeakageBehaviour) parsed),                Is.EqualTo(message));

        }

        #endregion

    }

}

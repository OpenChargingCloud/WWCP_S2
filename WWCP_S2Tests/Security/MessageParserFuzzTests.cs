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

using System.Diagnostics;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Security
{

    /// <summary>
    /// Hostile input for the S2 JSON message layer, in process and without HTTP: a table of
    /// mutations is applied to a valid JSON of every representative message type and the
    /// invariants of <c>TryParse</c> are asserted for each of them.
    ///
    /// The contract under test (CONVENTIONS.md §3, §8) is:
    /// <list type="bullet">
    ///   <item>TryParse NEVER throws, whatever the JSON contains.</item>
    ///   <item>It returns false with a non-empty error message and a null value, or</item>
    ///   <item>it returns true with a non-null value whose ToJSON() also does not throw.</item>
    /// </list>
    /// Every failure names the message type and the mutation, so a regression points straight
    /// at the offending property.
    /// </summary>
    [TestFixture]
    public sealed class MessageParserFuzzTests
    {

        #region Data

        /// <summary>
        /// A parser of one S2 message type, bound to the (JSON, out message, out error, options) overload.
        /// </summary>
        private delegate Boolean S2MessageTryParser<T>(JObject           JSON,
                                                       out T?            Message,
                                                       out String?       ErrorResponse,
                                                       S2ParserOptions?  Options)
            where T : class;


        /// <summary>
        /// The outcome of a single TryParse call.
        /// </summary>
        private sealed record ParseOutcome(Boolean      Success,
                                           IS2Message?  Message,
                                           String?      Error);


        /// <summary>
        /// One message type under test: a valid JSON, its parser and the classification of its
        /// properties, so that the tables below know which mutation must be rejected.
        /// </summary>
        private sealed record MessageUnderTest(String                                         Name,
                                               String                                         ValidJSON,
                                               Func<JObject, S2ParserOptions?, ParseOutcome>  Parse)
        {

            /// <summary>The mandatory schema properties, without "message_type" and "message_id".</summary>
            public IReadOnlyList<String>  Mandatory       { get; init; } = [];

            /// <summary>The array properties with minItems >= 1.</summary>
            public IReadOnlyList<String>  RequiredArrays  { get; init; } = [];

            /// <summary>The properties of JSON type "number".</summary>
            public IReadOnlyList<String>  Numbers         { get; init; } = [];

            /// <summary>The properties of format "date-time".</summary>
            public IReadOnlyList<String>  Timestamps      { get; init; } = [];

            /// <summary>The properties referencing the Duration schema (integer milliseconds).</summary>
            public IReadOnlyList<String>  Durations       { get; init; } = [];

            /// <summary>The properties referencing the ID schema ([a-zA-Z0-9\-_:]{2,64}).</summary>
            public IReadOnlyList<String>  Identifiers     { get; init; } = [];

            /// <summary>The properties referencing a closed enumeration.</summary>
            public IReadOnlyList<String>  Enumerations    { get; init; } = [];

        }


        /// <summary>
        /// The parser option sets every mutation is run through.
        /// </summary>
        private static readonly (String Name, S2ParserOptions? Options)[] optionSets = [
            ("default options",  S2ParserOptions.Default),
            ("strict options",   S2ParserOptions.Strict),
            ("no options",       null)
        ];

        #endregion

        #region (private static) The messages under test

        private static Func<JObject, S2ParserOptions?, ParseOutcome> ParserOf<T>(S2MessageTryParser<T> TryParse)
            where T : class, IS2Message

            => (json, options) => {
                   var success = TryParse(json, out var message, out var error, options);
                   return new ParseOutcome(success, message, error);
               };


        /// <summary>
        /// The representative set of S2 messages: the two handshake messages, the resource
        /// manager details and the control type selection of the "no control" phase, a power
        /// measurement, an instruction status update and the two central FRBC messages
        /// (the documentation examples of CONVENTIONS.md §8).
        /// </summary>
        private static IReadOnlyList<MessageUnderTest> MessagesUnderTest()

            => [

                   new MessageUnderTest(
                       "Handshake",
                       """
                       {
                         "message_type": "Handshake",
                         "message_id": "ea2e0f4f-5294-4578-a050-73fdd6110eec",
                         "role": "RM",
                         "supported_protocol_versions": [ "0.0.2-beta" ]
                       }
                       """,
                       ParserOf<Handshake>(Handshake.TryParse)
                   ) {
                       // "supported_protocol_versions" is optional in the schema, but an RM must
                       // announce its versions (the constructor rejects an RM without them).
                       Mandatory       = [ "role", "supported_protocol_versions" ],
                       RequiredArrays  = [ "supported_protocol_versions" ],
                       Enumerations    = [ "role" ]
                   },

                   new MessageUnderTest(
                       "HandshakeResponse",
                       """
                       {
                         "message_type": "HandshakeResponse",
                         "message_id": "04f2d3a7-f018-46d5-b769-c62393e02804",
                         "selected_protocol_version": "0.0.2-beta"
                       }
                       """,
                       ParserOf<HandshakeResponse>(HandshakeResponse.TryParse)
                   ) {
                       Mandatory  = [ "selected_protocol_version" ]
                   },

                   new MessageUnderTest(
                       "ResourceManagerDetails",
                       """
                       {
                         "message_type": "ResourceManagerDetails",
                         "message_id": "0c41efc2-771d-468f-afdc-fb69255dad33",
                         "resource_id": "acme_ev_xxxxxx",
                         "name": "My Electric Vehicle RM",
                         "roles": [ { "role": "ENERGY_CONSUMER", "commodity": "ELECTRICITY" } ],
                         "manufacturer": "ACME",
                         "model": "WallBox-b100",
                         "serial_number": "123",
                         "firmware_version": "v1.0",
                         "instruction_processing_delay": 3000,
                         "available_control_types": [ "FILL_RATE_BASED_CONTROL" ],
                         "provides_forecast": false,
                         "provides_power_measurement_types": [ "ELECTRIC.POWER.3_PHASE_SYMMETRIC" ]
                       }
                       """,
                       ParserOf<ResourceManagerDetails>(ResourceManagerDetails.TryParse)
                   ) {
                       Mandatory       = [ "resource_id", "roles", "instruction_processing_delay",
                                           "available_control_types", "provides_forecast",
                                           "provides_power_measurement_types" ],
                       RequiredArrays  = [ "roles", "available_control_types", "provides_power_measurement_types" ],
                       Durations       = [ "instruction_processing_delay" ],
                       Identifiers     = [ "resource_id" ]
                   },

                   new MessageUnderTest(
                       "SelectControlType",
                       """
                       {
                         "message_type": "SelectControlType",
                         "message_id": "5b0b7a0b-1f6b-4d5b-9a3f-2c4e1d6a7b8c",
                         "control_type": "FILL_RATE_BASED_CONTROL"
                       }
                       """,
                       ParserOf<SelectControlType>(SelectControlType.TryParse)
                   ) {
                       Mandatory     = [ "control_type" ],
                       Enumerations  = [ "control_type" ]
                   },

                   new MessageUnderTest(
                       "InstructionStatusUpdate",
                       """
                       {
                         "message_type": "InstructionStatusUpdate",
                         "message_id": "84522a12-7960-48b1-abf3-b7694dac80e6",
                         "instruction_id": "instruction1",
                         "status_type": "NEW",
                         "timestamp": "2019-08-24T14:15:22Z"
                       }
                       """,
                       ParserOf<InstructionStatusUpdate>(InstructionStatusUpdate.TryParse)
                   ) {
                       Mandatory     = [ "instruction_id", "status_type", "timestamp" ],
                       Timestamps    = [ "timestamp" ],
                       Identifiers   = [ "instruction_id" ],
                       Enumerations  = [ "status_type" ]
                   },

                   new MessageUnderTest(
                       "PowerMeasurement",
                       """
                       {
                         "message_type": "PowerMeasurement",
                         "message_id": "72355bab-e58e-47c7-bada-acf0dc893218",
                         "measurement_timestamp": "2019-08-24T14:15:22Z",
                         "values": [
                           {
                             "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC",
                             "value": 10963
                           }
                         ]
                       }
                       """,
                       ParserOf<PowerMeasurement>(PowerMeasurement.TryParse)
                   ) {
                       Mandatory       = [ "measurement_timestamp", "values" ],
                       RequiredArrays  = [ "values" ],
                       Timestamps      = [ "measurement_timestamp" ]
                   },

                   new MessageUnderTest(
                       "FRBC.SystemDescription",
                       """
                       {
                         "message_type": "FRBC.SystemDescription",
                         "message_id": "0c84b415-4e5e-429c-b5b6-116a5de6bfbf",
                         "valid_from": "2019-08-24T14:15:22Z",
                         "actuators": [
                           {
                             "id": "actuator1",
                             "diagnostic_label": "EV charger",
                             "supported_commodities": [ "ELECTRICITY" ],
                             "operation_modes": [
                               {
                                 "id": "om1",
                                 "diagnostic_label": "Off",
                                 "elements": [
                                   {
                                     "fill_level_range": { "start_of_range": 0, "end_of_range": 100 },
                                     "fill_rate":        { "start_of_range": 0, "end_of_range": 0 },
                                     "power_ranges": [
                                       {
                                         "start_of_range": 0,
                                         "end_of_range": 0,
                                         "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC"
                                       }
                                     ]
                                   }
                                 ],
                                 "abnormal_condition_only": false
                               },
                               {
                                 "id": "om2",
                                 "diagnostic_label": "Charging",
                                 "elements": [
                                   {
                                     "fill_level_range": { "start_of_range": 0, "end_of_range": 100 },
                                     "fill_rate":        { "start_of_range": 0.00065, "end_of_range": 0.0051 },
                                     "power_ranges": [
                                       {
                                         "start_of_range": 1400,
                                         "end_of_range": 11000,
                                         "commodity_quantity": "ELECTRIC.POWER.3_PHASE_SYMMETRIC"
                                       }
                                     ]
                                   }
                                 ],
                                 "abnormal_condition_only": false
                               }
                             ],
                             "transitions": [
                               {
                                 "id": "transition1",
                                 "from": "om1",
                                 "to": "om2",
                                 "start_timers": [],
                                 "blocking_timers": [],
                                 "transition_duration": 3000,
                                 "abnormal_condition_only": false
                               }
                             ],
                             "timers": []
                           }
                         ],
                         "storage": {
                           "diagnostic_label": "Battery SoC",
                           "fill_level_label": "EV Battery SoC",
                           "provides_leakage_behaviour": false,
                           "provides_fill_level_target_profile": true,
                           "provides_usage_forecast": false,
                           "fill_level_range": { "start_of_range": 0, "end_of_range": 100 }
                         }
                       }
                       """,
                       ParserOf<FRBC_SystemDescription>(FRBC_SystemDescription.TryParse)
                   ) {
                       Mandatory       = [ "valid_from", "actuators", "storage" ],
                       RequiredArrays  = [ "actuators" ],
                       Timestamps      = [ "valid_from" ]
                   },

                   new MessageUnderTest(
                       "FRBC.Instruction",
                       """
                       {
                         "message_type": "FRBC.Instruction",
                         "message_id": "5bc49243-edee-4884-99b3-65a62766949c",
                         "id": "instruction1",
                         "actuator_id": "actuator1",
                         "operation_mode": "om1",
                         "operation_mode_factor": 1,
                         "execution_time": "2019-08-24T14:15:22Z",
                         "abnormal_condition": false
                       }
                       """,
                       ParserOf<FRBC_Instruction>(FRBC_Instruction.TryParse)
                   ) {
                       Mandatory    = [ "id", "actuator_id", "operation_mode", "operation_mode_factor",
                                        "execution_time", "abnormal_condition" ],
                       Numbers      = [ "operation_mode_factor" ],
                       Timestamps   = [ "execution_time" ],
                       Identifiers  = [ "id", "actuator_id", "operation_mode" ]
                   }

               ];

        #endregion

        #region (private static) Hostile values and structures

        private static JToken NestedArrays(Int32 Depth)
        {

            JToken token = new JValue(1);

            for (var i = 0; i < Depth; i++)
                token = new JArray(token);

            return token;

        }

        private static JToken NestedObjects(Int32 Depth)
        {

            JToken token = new JValue(1);

            for (var i = 0; i < Depth; i++)
                token = new JObject(new JProperty("a", token));

            return token;

        }

        private static JArray HugeArray(Int32 Count)
        {

            var array = new JArray();

            for (var i = 0; i < Count; i++)
                array.Add(new JValue(i));

            return array;

        }


        /// <summary>
        /// The hostile values that are written into every single property of every message.
        /// The factories hand out fresh tokens, because a JToken may only have one parent.
        /// </summary>
        private static IReadOnlyList<(String Name, Func<JToken> Value)> HostileValues()

            => [
                   ("null",                       static () => JValue.CreateNull()),
                   ("empty string",               static () => new JValue("")),
                   ("blank string",               static () => new JValue("   ")),
                   ("NUL character",              static () => new JValue("\u0000")),
                   ("single character",           static () => new JValue("a")),
                   ("65 characters",              static () => new JValue(new String('a', 65))),
                   ("70000 characters",           static () => new JValue(new String('a', 70000))),
                   ("control characters",         static () => new JValue("\u0001\u0002\u0003\u007F")),
                   ("lone high surrogate",        static () => new JValue("\ud800")),
                   ("HTML script",                static () => new JValue("<script>alert('S2')</script>")),
                   ("SQL fragment",               static () => new JValue("'; DROP TABLE nodes; --")),
                   ("path traversal",             static () => new JValue("../../../etc/passwd")),
                   ("format string",              static () => new JValue("%s%s%s%n")),
                   ("zero",                       static () => new JValue(0)),
                   ("negative number",            static () => new JValue(-1)),
                   ("Int64.MaxValue",             static () => new JValue(Int64.MaxValue)),
                   ("Int64.MinValue",             static () => new JValue(Int64.MinValue)),
                   ("Double.NaN",                 static () => new JValue(Double.NaN)),
                   ("Double.PositiveInfinity",    static () => new JValue(Double.PositiveInfinity)),
                   ("Double.NegativeInfinity",    static () => new JValue(Double.NegativeInfinity)),
                   ("Double.MaxValue",            static () => new JValue(Double.MaxValue)),
                   ("Double.Epsilon",             static () => new JValue(Double.Epsilon)),
                   ("boolean",                    static () => new JValue(true)),
                   ("empty array",                static () => new JArray()),
                   ("array of nulls",             static () => new JArray(JValue.CreateNull(), JValue.CreateNull())),
                   ("array of arrays",            static () => new JArray(new JArray(), new JArray())),
                   ("empty object",               static () => new JObject()),
                   ("object",                     static () => new JObject(new JProperty("x", 1))),
                   ("garbage timestamp",          static () => new JValue("2019-13-45T99:99:99Z")),
                   ("year 0001 timestamp",        static () => new JValue("0001-01-01T00:00:00Z")),
                   ("year 9999 timestamp",        static () => new JValue("9999-12-31T23:59:59Z")),
                   ("unknown enumeration value",  static () => new JValue("DEFINITELY_NOT_A_VALID_VALUE")),
                   ("lower case enumeration",     static () => new JValue("electricity")),
                   ("200 nested arrays",          static () => NestedArrays (200)),
                   ("200 nested objects",         static () => NestedObjects(200))
               ];

        #endregion

        #region (private static) Assertions

        /// <summary>
        /// Parse the given JSON, turning an escaping exception into a test failure instead of
        /// letting it tear down the whole test method.
        /// </summary>
        private static ParseOutcome ParseSafely(MessageUnderTest  Message,
                                                JObject           JSON,
                                                S2ParserOptions?  Options,
                                                String            Mutation)
        {

            try
            {
                return Message.Parse(JSON, Options);
            }
            catch (Exception e)
            {

                Assert.Fail($"{Message.Name} / {Mutation}: TryParse threw {e.GetType().FullName}: {e.Message}");

                return new ParseOutcome(false, null, $"{e.GetType().Name}: {e.Message}");

            }

        }

        /// <summary>
        /// The invariants of every TryParse call: no exception, and either a rejection with a
        /// non-empty error message and no value, or a value that can be serialised again.
        /// </summary>
        private static ParseOutcome AssertParseInvariants(MessageUnderTest  Message,
                                                          JObject           JSON,
                                                          S2ParserOptions?  Options,
                                                          String            Mutation)
        {

            var outcome = ParseSafely(Message, JSON, Options, Mutation);

            if (outcome.Success)
            {

                var parsed = outcome.Message;

                Assert.That(parsed, Is.Not.Null, $"{Message.Name} / {Mutation}: TryParse returned true without a message!");

                // An accepted message must survive its own serialisation.
                if (parsed is not null)
                    Assert.That(() => { _ = parsed.ToJSON(); },
                                Throws.Nothing,
                                $"{Message.Name} / {Mutation}: the accepted message cannot be serialised again!");

            }

            else
            {
                Assert.That(outcome.Message,  Is.Null,                   $"{Message.Name} / {Mutation}: TryParse returned false but produced a message!");
                Assert.That(outcome.Error,    Is.Not.Null.And.Not.Empty, $"{Message.Name} / {Mutation}: TryParse returned false without an error message!");
            }

            return outcome;

        }

        /// <summary>
        /// Assert that the given mutation is rejected with a non-empty error message.
        /// </summary>
        private static void AssertRejected(MessageUnderTest  Message,
                                           JObject           JSON,
                                           S2ParserOptions?  Options,
                                           String            Mutation)
        {

            var outcome = AssertParseInvariants(Message, JSON, Options, Mutation);

            Assert.That(outcome.Success, Is.False, $"{Message.Name} / {Mutation}: TryParse accepted a value it must reject!");

        }

        /// <summary>
        /// A fresh, valid JSON of the given message.
        /// </summary>
        private static JObject ValidJSONOf(MessageUnderTest Message)
            => JObject.Parse(Message.ValidJSON);

        #endregion


        // The baseline: without any mutation every message parses.

        #region ValidMessages_AreAccepted_AndRoundTrip()

        [Test]
        public void ValidMessages_AreAccepted_AndRoundTrip()
        {

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {

                    var outcome = AssertParseInvariants(message, ValidJSONOf(message), S2ParserOptions.Default, "the unmutated example");

                    Assert.That(outcome.Success,               Is.True,  $"{message.Name}: the valid example was rejected: {outcome.Error}");
                    Assert.That(outcome.Message?.MessageType,  Is.EqualTo(message.Name), $"{message.Name}: the parsed message reports another message type!");

                }

            });

        }

        #endregion


        // The broad matrix: every hostile value in every property, under every option set.

        #region TryParse_NeverThrows_ForAnyHostileValueInAnyProperty()

        [Test]
        public void TryParse_NeverThrows_ForAnyHostileValueInAnyProperty()
        {

            var hostileValues = HostileValues();

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {

                    var properties = ValidJSONOf(message).Properties().Select(property => property.Name).ToList();

                    foreach (var property in properties)
                    {
                        foreach (var (valueName, value) in hostileValues)
                        {
                            foreach (var (optionsName, options) in optionSets)
                            {

                                var json = ValidJSONOf(message);
                                json[property] = value();

                                AssertParseInvariants(message, json, options, $"'{property}' = {valueName} ({optionsName})");

                            }
                        }
                    }

                }

            });

        }

        #endregion

        #region TryParse_NeverThrows_ForHostileAdditionalProperties()

        [Test]
        public void TryParse_NeverThrows_ForHostileAdditionalProperties()
        {

            (String Name, String Key)[] hostileKeys = [
                ("an empty property name",       ""),
                ("a blank property name",        " "),
                ("a NUL property name",          "\u0000"),
                ("a 10000 character name",       new String('k', 10000)),
                ("a duplicate-looking name",     "Message_Type"),
                ("a prototype pollution name",   "__proto__"),
                ("a dotted name",                "a.b.c"),
                ("a script name",                "<script>")
            ];

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {
                    foreach (var (name, key) in hostileKeys)
                    {
                        foreach (var (optionsName, options) in optionSets)
                        {

                            var json = ValidJSONOf(message);

                            // A JObject cannot hold two properties with the same name; skip a collision.
                            if (json.ContainsKey(key))
                                continue;

                            json.Add(key, new JValue("<script>alert('S2')</script>"));

                            AssertParseInvariants(message, json, options, $"{name} ({optionsName})");

                        }
                    }
                }

            });

        }

        #endregion


        // Mandatory properties.

        #region TryParse_RejectsMissingMandatoryProperties()

        [Test]
        public void TryParse_RejectsMissingMandatoryProperties()
        {

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {
                    foreach (var property in message.Mandatory.Concat(new[] { "message_type", "message_id" }))
                    {

                        var json = ValidJSONOf(message);
                        json.Remove(property);

                        var outcome = AssertParseInvariants(message, json, S2ParserOptions.Default, $"without '{property}'");

                        Assert.That(outcome.Success, Is.False, $"{message.Name}: the mandatory property '{property}' may be omitted!");

                        // The error must name the offending property (the parser spells the
                        // description with spaces where the wire format uses underscores).
                        var error       = outcome.Error ?? "";
                        var namesIt     = error.Contains(property,                   StringComparison.Ordinal) ||
                                          error.Contains(property.Replace('_', ' '), StringComparison.Ordinal);

                        Assert.That(namesIt,
                                    Is.True,
                                    $"{message.Name}: the error for the missing '{property}' does not name it: {error}");

                    }
                }

            });

        }

        #endregion

        #region TryParse_RejectsNullAndWronglyTypedMandatoryProperties()

        [Test]
        public void TryParse_RejectsNullAndWronglyTypedMandatoryProperties()
        {

            (String Name, Func<JToken> Value)[] wrongTypes = [
                ("null",          static () => JValue.CreateNull()),
                ("an object",     static () => new JObject(new JProperty("x", 1))),
                ("an array",      static () => new JArray(1, 2, 3))
            ];

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {
                    foreach (var property in message.Mandatory.Concat(new[] { "message_type", "message_id" }))
                    {
                        foreach (var (name, value) in wrongTypes)
                        {

                            // An array property legitimately holds an array.
                            if (name == "an array" && message.RequiredArrays.Contains(property))
                                continue;

                            var json = ValidJSONOf(message);
                            json[property] = value();

                            AssertRejected(message, json, S2ParserOptions.Default, $"'{property}' = {name}");

                        }
                    }
                }

            });

        }

        #endregion

        #region TryParse_RejectsAForeignMessageTypeAndAMalformedMessageId()

        [Test]
        public void TryParse_RejectsAForeignMessageTypeAndAMalformedMessageId()
        {

            (String Name, JToken Value)[] messageTypes = [
                ("another message type",          new JValue("ReceptionStatus")),
                ("an empty message type",         new JValue("")),
                ("a case-shifted message type",   new JValue("handshake")),
                ("a message type with a space",   new JValue(" Handshake")),
                ("a numeric message type",        new JValue(42))
            ];

            (String Name, JToken Value)[] messageIds = [
                ("an empty message id",           new JValue("")),
                ("a one character message id",    new JValue("x")),
                ("a whitespace message id",       new JValue("  ")),
                ("a punctuation-only message id", new JValue("!!!")),
                ("a numeric message id",          new JValue(42)),
                ("a NUL message id",              new JValue("\u0000\u0000"))
            ];

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {

                    foreach (var (name, value) in messageTypes)
                    {
                        var json = ValidJSONOf(message);
                        json["message_type"] = value.DeepClone();
                        AssertRejected(message, json, S2ParserOptions.Default, name);
                    }

                    foreach (var (name, value) in messageIds)
                    {
                        var json = ValidJSONOf(message);
                        json["message_id"] = value.DeepClone();
                        AssertRejected(message, json, S2ParserOptions.Default, name);
                    }

                }

            });

        }

        #endregion


        // The value domains: numbers, timestamps, durations, enumerations, identifiers, arrays.

        #region TryParse_RejectsNonFiniteAndOutOfRangeNumbers()

        [Test]
        public void TryParse_RejectsNonFiniteAndOutOfRangeNumbers()
        {

            // "operation_mode_factor" is the only number of the messages under test and is
            // restricted to [0, 1] by the S2 semantic rules (CONVENTIONS.md §8).
            (String Name, JToken Value)[] hostileNumbers = [
                ("Double.NaN",                 new JValue(Double.NaN)),
                ("Double.PositiveInfinity",    new JValue(Double.PositiveInfinity)),
                ("Double.NegativeInfinity",    new JValue(Double.NegativeInfinity)),
                ("Double.MaxValue",            new JValue(Double.MaxValue)),
                ("Int64.MaxValue",             new JValue(Int64.MaxValue)),
                ("Int64.MinValue",             new JValue(Int64.MinValue)),
                ("a negative factor",          new JValue(-0.0001)),
                ("a factor above one",         new JValue(1.0001)),
                ("a numeric string",           new JValue("1")),
                ("a boolean",                  new JValue(true))
            ];

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {
                    foreach (var property in message.Numbers)
                    {
                        foreach (var (name, value) in hostileNumbers)
                        {

                            var json = ValidJSONOf(message);
                            json[property] = value.DeepClone();

                            AssertRejected(message, json, S2ParserOptions.Default, $"'{property}' = {name}");

                        }
                    }
                }

                // The nested power value of a PowerMeasurement is a number without a range,
                // but it must still be finite and of the right JSON type.
                var measurement   = MessagesUnderTest().Single(candidate => candidate.Name == "PowerMeasurement");

                (String Name, JToken Value)[] nonNumbers = [
                    ("Double.NaN",                 new JValue(Double.NaN)),
                    ("Double.PositiveInfinity",    new JValue(Double.PositiveInfinity)),
                    ("Double.NegativeInfinity",    new JValue(Double.NegativeInfinity)),
                    ("a numeric string",           new JValue("10963")),
                    ("a boolean",                  new JValue(true)),
                    ("an array",                   new JArray(1))
                ];

                foreach (var (name, value) in nonNumbers)
                {

                    var json = ValidJSONOf(measurement);
                    json["values"]![0]!["value"] = value.DeepClone();

                    AssertRejected(measurement, json, S2ParserOptions.Default, $"'values[0].value' = {name}");

                }

            });

        }

        #endregion

        #region TryParse_RejectsGarbageTimestamps_AndSurvivesTheExtremeYears()

        [Test]
        public void TryParse_RejectsGarbageTimestamps_AndSurvivesTheExtremeYears()
        {

            (String Name, JToken Value)[] garbage = [
                ("an empty timestamp",              new JValue("")),
                ("a blank timestamp",               new JValue("   ")),
                ("a nonsense timestamp",            new JValue("not-a-timestamp")),
                ("an impossible timestamp",         new JValue("2019-13-45T99:99:99Z")),
                ("a 30th of February",              new JValue("2019-02-30T00:00:00Z")),
                ("a date without a time",           new JValue("2019-08-24")),
                ("a Unix epoch number",             new JValue(1566656122)),
                ("a timestamp object",              new JObject(new JProperty("year", 2019))),
                ("a timestamp array",               new JArray("2019-08-24T14:15:22Z")),
                ("a 10000 character timestamp",     new JValue(new String('9', 10000))),
                // A negative offset at the last representable date moves the instant past the
                // end of the calendar; "+14:00" moves it backwards and is representable.
                ("an offset beyond the calendar",   new JValue("9999-12-31T23:59:59-14:00"))
            ];

            // Absurd but representable instants: whatever the parser decides, it must not throw
            // and an accepted message must still be serialisable.
            (String Name, JToken Value)[] extremes = [
                ("the year 0001",                   new JValue("0001-01-01T00:00:00Z")),
                ("the year 9999",                   new JValue("9999-12-31T23:59:59Z")),
                ("a naive timestamp",               new JValue("2019-08-24T14:15:22")),
                ("a fractional timestamp",          new JValue("2019-08-24T14:15:22.1234567Z")),
                ("a negative offset",               new JValue("2019-08-24T14:15:22-11:00"))
            ];

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {
                    foreach (var property in message.Timestamps)
                    {

                        foreach (var (name, value) in garbage)
                        {
                            var json = ValidJSONOf(message);
                            json[property] = value.DeepClone();
                            AssertRejected(message, json, S2ParserOptions.Default, $"'{property}' = {name}");
                        }

                        foreach (var (name, value) in extremes)
                        {
                            foreach (var (optionsName, options) in optionSets)
                            {
                                var json = ValidJSONOf(message);
                                json[property] = value.DeepClone();
                                AssertParseInvariants(message, json, options, $"'{property}' = {name} ({optionsName})");
                            }
                        }

                    }
                }

            });

        }

        #endregion

        #region TryParse_RejectsNegativeAndOversizedDurations()

        [Test]
        public void TryParse_RejectsNegativeAndOversizedDurations()
        {

            (String Name, JToken Value)[] hostileDurations = [
                ("a negative duration",         new JValue(-1)),
                ("Int64.MinValue",              new JValue(Int64.MinValue)),
                ("Int64.MaxValue",              new JValue(Int64.MaxValue)),
                ("a fractional duration",       new JValue(1.5)),
                ("Double.NaN",                  new JValue(Double.NaN)),
                ("Double.PositiveInfinity",     new JValue(Double.PositiveInfinity)),
                ("a duration string",           new JValue("3000")),
                ("an ISO 8601 duration",        new JValue("PT1H")),
                ("a duration object",           new JObject(new JProperty("milliseconds", 3000)))
            ];

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {
                    foreach (var property in message.Durations)
                    {
                        foreach (var (name, value) in hostileDurations)
                        {

                            var json = ValidJSONOf(message);
                            json[property] = value.DeepClone();

                            AssertRejected(message, json, S2ParserOptions.Default, $"'{property}' = {name}");

                        }
                    }
                }

                // The nested transition_duration of an FRBC.SystemDescription actuator.
                var systemDescription = MessagesUnderTest().Single(candidate => candidate.Name == "FRBC.SystemDescription");

                foreach (var (name, value) in hostileDurations)
                {

                    var json = ValidJSONOf(systemDescription);
                    json["actuators"]![0]!["transitions"]![0]!["transition_duration"] = value.DeepClone();

                    AssertRejected(systemDescription, json, S2ParserOptions.Default, $"'transition_duration' = {name}");

                }

            });

        }

        #endregion

        #region TryParse_RejectsUnknownEnumerationValues()

        [Test]
        public void TryParse_RejectsUnknownEnumerationValues()
        {

            (String Name, JToken Value)[] hostileEnums = [
                ("an unknown value",            new JValue("HIBERNATE")),
                ("an empty value",              new JValue("")),
                ("a blank value",               new JValue("   ")),
                ("a lower case value",          new JValue("rm")),
                ("a value with a space",        new JValue("FILL RATE BASED CONTROL")),
                ("a very long value",           new JValue(new String('E', 5000))),
                ("a numeric value",             new JValue(0)),
                ("a boolean value",             new JValue(false)),
                ("an array of values",          new JArray("RM"))
            ];

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {
                    foreach (var property in message.Enumerations)
                    {
                        foreach (var (name, value) in hostileEnums)
                        {

                            var json = ValidJSONOf(message);
                            json[property] = value.DeepClone();

                            AssertRejected(message, json, S2ParserOptions.Default, $"'{property}' = {name}");

                        }
                    }
                }

                // A nested enumeration inside an array of objects.
                var measurement = MessagesUnderTest().Single(candidate => candidate.Name == "PowerMeasurement");

                foreach (var (name, value) in hostileEnums)
                {

                    var json = ValidJSONOf(measurement);
                    json["values"]![0]!["commodity_quantity"] = value.DeepClone();

                    AssertRejected(measurement, json, S2ParserOptions.Default, $"'values[0].commodity_quantity' = {name}");

                }

            });

        }

        #endregion

        #region TryParse_RejectsIdentifiersViolatingTheSchemaPattern()

        [Test]
        public void TryParse_RejectsIdentifiersViolatingTheSchemaPattern()
        {

            // ID.schema.json requires [a-zA-Z0-9\-_:]{2,64}. The pattern is unanchored, as JSON
            // Schema demands, so a text merely has to CONTAIN such a run: "'; DROP TABLE x; --"
            // is a valid identifier and is deliberately not listed here. What is rejected is a
            // text without any such run at all.
            (String Name, JToken Value)[] hostileIds = [
                ("an empty identifier",            new JValue("")),
                ("a blank identifier",             new JValue("     ")),
                ("a single character",             new JValue("a")),
                ("punctuation only",               new JValue("!!!!")),
                ("a NUL identifier",               new JValue("\u0000\u0000")),
                ("control characters",             new JValue("\u0001\u0002")),
                ("a single non-ASCII letter",      new JValue("\u00e4")),
                ("a numeric identifier",           new JValue(42)),
                ("a boolean identifier",           new JValue(true)),
                ("an identifier object",           new JObject(new JProperty("id", "actuator1")))
            ];

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {
                    foreach (var property in message.Identifiers)
                    {
                        foreach (var (name, value) in hostileIds)
                        {

                            var json = ValidJSONOf(message);
                            json[property] = value.DeepClone();

                            AssertRejected(message, json, S2ParserOptions.Default, $"'{property}' = {name}");

                        }
                    }
                }

                // Strict options additionally demand a UUID everywhere.
                var instruction = MessagesUnderTest().Single(candidate => candidate.Name == "FRBC.Instruction");

                AssertRejected(instruction,
                               ValidJSONOf(instruction),
                               S2ParserOptions.Strict,
                               "the non-UUID identifiers of the documentation example under strict options");

            });

        }

        #endregion

        #region TryParse_RejectsEmptyRequiredArrays_AndArraysOfGarbage()

        [Test]
        public void TryParse_RejectsEmptyRequiredArrays_AndArraysOfGarbage()
        {

            (String Name, Func<JToken> Value)[] hostileArrays = [
                ("an empty array",              static () => new JArray()),
                ("an array of nulls",           static () => new JArray(JValue.CreateNull())),
                ("an array of numbers",         static () => new JArray(1, 2, 3)),
                ("an array of empty objects",   static () => new JArray(new JObject())),
                ("an array of arrays",          static () => new JArray(new JArray())),
                ("a scalar instead of array",   static () => new JValue("one")),
                ("an object instead of array",  static () => new JObject())
            ];

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {
                    foreach (var property in message.RequiredArrays)
                    {
                        foreach (var (name, value) in hostileArrays)
                        {

                            // An array of strings is a legitimate "supported_protocol_versions".
                            var json = ValidJSONOf(message);
                            json[property] = value();

                            AssertRejected(message, json, S2ParserOptions.Default, $"'{property}' = {name}");

                        }
                    }
                }

            });

        }

        #endregion


        // Structural hostility: depth and size.

        #region TryParse_SurvivesDeeplyNestedAndOversizedStructures()

        [Test]
        public void TryParse_SurvivesDeeplyNestedAndOversizedStructures()
        {

            (String Name, Func<JToken> Value)[] structures = [
                ("1000 nested arrays",       static () => NestedArrays (1000)),
                ("1000 nested objects",      static () => NestedObjects(1000)),
                ("50000 array elements",     static () => HugeArray(50000)),
                ("a 1000000 character text", static () => new JValue(new String('x', 1000000)))
            ];

            Assert.Multiple(() => {

                foreach (var message in MessagesUnderTest())
                {

                    var properties = ValidJSONOf(message).Properties().Select(property => property.Name).ToList();

                    foreach (var property in properties)
                    {
                        foreach (var (name, value) in structures)
                        {

                            var json = ValidJSONOf(message);
                            json[property] = value();

                            AssertParseInvariants(message, json, S2ParserOptions.Default, $"'{property}' = {name}");

                        }
                    }

                }

            });

        }

        #endregion

        #region TryParse_SurvivesAnArrayOfFarTooManyActuators()

        [Test]
        public void TryParse_SurvivesAnArrayOfFarTooManyActuators()
        {

            // An attacker may repeat a *well-formed* element until the receiver runs out of
            // memory or time. The schema bounds the array (FRBC.SystemDescription: at most ten
            // actuators), so the parser refuses the flood - quickly, without a quadratic
            // uniqueness check and without throwing.
            var systemDescription  = MessagesUnderTest().Single(candidate => candidate.Name == "FRBC.SystemDescription");
            var json               = ValidJSONOf(systemDescription);
            var actuator           = (JObject) json["actuators"]![0]!;
            var manyActuators      = new JArray();

            for (var i = 0; i < 2000; i++)
            {
                var clone = (JObject) actuator.DeepClone();
                clone["id"] = $"actuator-{i}";
                manyActuators.Add(clone);
            }

            json["actuators"] = manyActuators;

            var stopwatch = Stopwatch.StartNew();
            var outcome   = AssertParseInvariants(systemDescription, json, S2ParserOptions.Default, "2000 actuators");

            stopwatch.Stop();

            Assert.Multiple(() => {
                Assert.That(outcome.Success,          Is.False, "2000 actuators passed the schema bound of ten!");
                Assert.That(outcome.Error,            Does.Contain("actuators"), $"the rejection does not name the offending array: {outcome.Error}");
                Assert.That(stopwatch.Elapsed,        Is.LessThan(TimeSpan.FromSeconds(30)), "Parsing 2000 actuators took unreasonably long!");
            });

            // The very same array with a duplicated identifier violates the uniqueness rule.
            var duplicated = (JObject) json.DeepClone();
            duplicated["actuators"]![1]!["id"] = duplicated["actuators"]![0]!["id"]!.DeepClone();

            AssertRejected(systemDescription, duplicated, S2ParserOptions.Default, "2000 actuators with a duplicated id");

        }

        #endregion


        // The dispatcher in front of the individual parsers.

        #region S2MessageParser_NeverThrows_ForHostileEnvelopes()

        [Test]
        public void S2MessageParser_NeverThrows_ForHostileEnvelopes()
        {

            var envelopes = new List<(String Name, JObject JSON)> {
                ("an empty object",                new JObject()),
                ("an unknown message type",        new JObject(new JProperty("message_type", "NoSuchMessage"),
                                                               new JProperty("message_id",   "9a8d2f1c-0000-4000-8000-000000000001"))),
                ("a numeric message type",         new JObject(new JProperty("message_type", 42))),
                ("a null message type",            new JObject(new JProperty("message_type", JValue.CreateNull()))),
                ("an array message type",          new JObject(new JProperty("message_type", new JArray("Handshake")))),
                ("a 100000 character type",        new JObject(new JProperty("message_type", new String('T', 100000)))),
                ("a script message type",          new JObject(new JProperty("message_type", "<script>alert(1)</script>"))),
                ("a type with a NUL character",    new JObject(new JProperty("message_type", "Hand\u0000shake"))),
                ("only a message id",              new JObject(new JProperty("message_id",   "9a8d2f1c-0000-4000-8000-000000000001"))),
                ("a nested envelope",              new JObject(new JProperty("message_type", "Handshake"),
                                                               new JProperty("message_id",   "9a8d2f1c-0000-4000-8000-000000000001"),
                                                               new JProperty("role",         NestedObjects(500))))
            };

            foreach (var message in MessagesUnderTest())
                envelopes.Add(($"a valid {message.Name}", JObject.Parse(message.ValidJSON)));

            Assert.Multiple(() => {

                foreach (var (name, json) in envelopes)
                {
                    foreach (var (optionsName, options) in optionSets)
                    {

                        Boolean        success;
                        IS2Message?    parsed;
                        S2ParseError?  error;

                        try
                        {
                            success = S2MessageParser.TryParse(json, options, out parsed, out error);
                        }
                        catch (Exception e)
                        {
                            Assert.Fail($"S2MessageParser / {name} ({optionsName}): TryParse threw {e.GetType().FullName}: {e.Message}");
                            continue;
                        }

                        if (success)
                        {
                            Assert.That(parsed,  Is.Not.Null, $"S2MessageParser / {name} ({optionsName}): true without a message!");
                            Assert.That(error,   Is.Null,     $"S2MessageParser / {name} ({optionsName}): true with an error!");
                        }
                        else
                        {
                            Assert.That(parsed,                    Is.Null,                   $"S2MessageParser / {name} ({optionsName}): false with a message!");
                            Assert.That(error,                     Is.Not.Null,               $"S2MessageParser / {name} ({optionsName}): false without an error!");
                            Assert.That(error?.DiagnosticLabel,    Is.Not.Null.And.Not.Empty, $"S2MessageParser / {name} ({optionsName}): an error without a diagnostic label!");
                        }

                    }
                }

            });

        }

        #endregion

        #region S2MessageParser_NeverThrows_ForHostileTexts()

        [Test]
        public void S2MessageParser_NeverThrows_ForHostileTexts()
        {

            (String Name, String Text)[] texts = [
                ("an empty text",                ""),
                ("whitespace only",              "   \t\r\n  "),
                ("a truncated object",           "{\"message_type\":"),
                ("unbalanced brackets",          "{\"a\": [1, 2}"),
                ("a bare scalar",                "42"),
                ("a bare string",                "\"Handshake\""),
                ("a bare null",                  "null"),
                ("an array",                     "[ { \"message_type\": \"Handshake\" } ]"),
                ("trailing content",             "{\"message_type\":\"Handshake\"} {}"),
                ("duplicate keys",               "{\"message_type\":\"Handshake\",\"message_type\":\"HandshakeResponse\"}"),
                ("a byte order mark",            "\uFEFF{\"message_type\":\"Handshake\"}"),
                ("a NUL inside a string",        "{\"message_type\":\"Hand\\u0000shake\"}"),
                ("a lone high surrogate",        "{\"message_type\":\"\\ud800\"}"),
                ("a lone low surrogate",         "{\"message_type\":\"\\udc00\"}"),
                ("an XML document",              "<?xml version=\"1.0\"?><Handshake/>"),
                ("an HTML document",             "<html><body><script>alert(1)</script></body></html>"),
                ("the NaN literal",              "{\"message_type\":\"Handshake\",\"v\":NaN}"),
                ("the Infinity literal",         "{\"message_type\":\"Handshake\",\"v\":Infinity}"),
                ("an exponent overflow",         "{\"message_type\":\"Handshake\",\"v\":1e999999}"),
                ("a 10000 digit number",         "{\"message_type\":\"Handshake\",\"v\":" + new String('9', 10000) + "}"),
                ("a 100000 character string",    "{\"message_type\":\"" + new String('A', 100000) + "\"}"),
                ("5000 nested arrays",           new String('[', 5000) + new String(']', 5000)),
                ("5000 nested objects",          String.Concat(Enumerable.Repeat("{\"a\":", 5000)) + "1" + new String('}', 5000))
            ];

            Assert.Multiple(() => {

                foreach (var (name, text) in texts)
                {
                    foreach (var (optionsName, options) in optionSets)
                    {

                        Boolean        success;
                        IS2Message?    parsed;
                        S2ParseError?  error;

                        try
                        {
                            success = S2MessageParser.TryParse(text, options, out parsed, out error);
                        }
                        catch (Exception e)
                        {
                            Assert.Fail($"S2MessageParser / {name} ({optionsName}): TryParse threw {e.GetType().FullName}: {e.Message}");
                            continue;
                        }

                        Assert.That(success,  Is.False,      $"S2MessageParser / {name} ({optionsName}): a hostile text was accepted!");
                        Assert.That(parsed,   Is.Null,       $"S2MessageParser / {name} ({optionsName}): false with a message!");
                        Assert.That(error,    Is.Not.Null,   $"S2MessageParser / {name} ({optionsName}): false without an error!");

                    }
                }

            });

        }

        #endregion

    }

}

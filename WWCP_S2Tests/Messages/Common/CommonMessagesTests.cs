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

namespace cloud.charging.open.protocols.S2.Tests.Messages.Common
{

    /// <summary>
    /// Tests of the common messages HandshakeResponse, ResourceManagerDetails, SessionRequest,
    /// PowerMeasurement, PowerForecast, InstructionStatusUpdate and RevokeObject (CONVENTIONS.md §8).
    /// </summary>
    [TestFixture]
    public sealed class CommonMessagesTests
    {

        #region (private) Helpers

        private static readonly Message_Id      messageId  = Message_Id.Parse("9a8d2f1c-0000-4000-8000-000000000001");
        private static readonly DateTimeOffset  timestamp  = new (2024, 8, 24, 14, 0, 0, TimeSpan.Zero);

        private static ResourceManagerDetails EVResourceManagerDetails()

            => new (Resource_Id.Parse("acme_ev_xxxxxx"),
                    [ new Role(RoleType.EnergyConsumer, Commodity.Electricity) ],
                    Duration.FromMilliseconds(3000),
                    [ ControlType.FillRateBasedControl ],
                    false,
                    [ CommodityQuantity.ElectricPower3PhaseSymmetric ],
                    "My Electric Vehicle RM",
                    "ACME",
                    "WallBox-b100",
                    "123",
                    "v1.0",
                    Currency.EUR,
                    Message_Id.Parse("0c41efc2-771d-468f-afdc-fb69255dad33"));

        private static PowerForecast SimplePowerForecast()

            => new (timestamp,
                    [
                        new PowerForecastElement(Duration.FromMilliseconds(3600000),
                                                 [ new PowerForecastValue(545.1, CommodityQuantity.ElectricPowerL1, 1000, 960, 800, 340, 100, 0) ]),
                        new PowerForecastElement(Duration.FromMilliseconds(1800000),
                                                 [ new PowerForecastValue(400,   CommodityQuantity.ElectricPowerL1) ])
                    ],
                    messageId);

        #endregion


        #region HandshakeResponse

        [Test]
        public void HandshakeResponse_RoundTrips_AndIsSchemaValid()
        {

            var message = new HandshakeResponse("0.0.2-beta", messageId);
            var json    = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "HandshakeResponse");

            Assert.That(json["message_type"]?.Value<String>(),  Is.EqualTo("HandshakeResponse"));
            Assert.That(json["message_id"]?.  Value<String>(),  Is.EqualTo(messageId.ToString()));

            Assert.That(HandshakeResponse.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                           Is.EqualTo(message));
            Assert.That(parsed!.SelectedProtocolVersion,  Is.EqualTo("0.0.2-beta"));
            Assert.That(parsed.GetHashCode(),             Is.EqualTo(message.GetHashCode()));
            Assert.That(message.Clone(),                  Is.EqualTo(message));
            Assert.That(message.ToString(),               Does.EndWith($"[{messageId}]"));

        }

        [Test]
        public void HandshakeResponse_MatchesTheDocumentationExample()
        {

            var json = JObject.Parse("""
                {
                  "message_type": "HandshakeResponse",
                  "message_id": "04f2d3a7-f018-46d5-b769-c62393e02804",
                  "selected_protocol_version": "0.0.2-beta"
                }
                """);

            Assert.That(HandshakeResponse.TryParse(json, out var message, out var error), Is.True, error);
            Assert.That(message!.SelectedProtocolVersion,  Is.EqualTo("0.0.2-beta"));
            Assert.That(message.MessageId.ToString(),      Is.EqualTo("04f2d3a7-f018-46d5-b769-c62393e02804"));

            var reserialised = message.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "HandshakeResponse");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void HandshakeResponse_RejectsMissingMandatory_AndAdditionalProperties()
        {

            var missing = JObject.Parse("""{ "message_type": "HandshakeResponse", "message_id": "9a8d2f1c-0000-4000-8000-000000000001" }""");
            Assert.That(HandshakeResponse.TryParse(missing, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("selected_protocol_version"));

            var extra = JObject.Parse("""{ "message_type": "HandshakeResponse", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "selected_protocol_version": "0.0.2-beta", "foo": 1 }""");
            Assert.That(HandshakeResponse.TryParse(extra, out _, out error), Is.True, error);
            Assert.That(HandshakeResponse.TryParse(extra, out _, out error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("foo"));

            var wrongType = JObject.Parse("""{ "message_type": "Handshake", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "selected_protocol_version": "0.0.2-beta" }""");
            Assert.That(HandshakeResponse.TryParse(wrongType, out _, out error), Is.False);
            Assert.That(error, Does.Contain("Unexpected message type"));

        }

        [Test]
        public void HandshakeResponse_AcceptsAnEmptyProtocolVersion_RejectsNull()
        {

            // The schema declares selected_protocol_version as a plain string without minLength,
            // so an empty string is valid; only null is rejected by the constructor.
            Assert.That(() => new HandshakeResponse(null!), Throws.ArgumentNullException);

            var empty = new HandshakeResponse("", messageId);
            Assert.That(empty.SelectedProtocolVersion, Is.EqualTo(""));

            var emptyJSON = empty.ToJSON();
            S2SchemaValidator.AssertValidMessage(emptyJSON, "HandshakeResponse");

            var json = JObject.Parse("""{ "message_type": "HandshakeResponse", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "selected_protocol_version": "" }""");
            Assert.That(HandshakeResponse.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed!.SelectedProtocolVersion, Is.EqualTo(""));
            JSONAssert.AssertDeepEquals(json, parsed.ToJSON());

        }

        #endregion

        #region ResourceManagerDetails

        [Test]
        public void ResourceManagerDetails_RoundTrips_AndIsSchemaValid()
        {

            var message = EVResourceManagerDetails();
            var json    = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "ResourceManagerDetails");

            Assert.That(json["currency"]?.Value<String>(),                        Is.EqualTo("EUR"));
            Assert.That(json["instruction_processing_delay"]?.Value<Int64>(),     Is.EqualTo(3000));
            Assert.That(json["provides_forecast"]?.Value<Boolean>(),              Is.False);

            Assert.That(ResourceManagerDetails.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                   Is.EqualTo(message));
            Assert.That(parsed!.ResourceId.ToString(),            Is.EqualTo("acme_ev_xxxxxx"));
            Assert.That(parsed.Name,                              Is.EqualTo("My Electric Vehicle RM"));
            Assert.That(parsed.Roles,                             Has.Count.EqualTo(1));
            Assert.That(parsed.Roles[0].RoleType,                 Is.EqualTo(RoleType.EnergyConsumer));
            Assert.That(parsed.Manufacturer,                      Is.EqualTo("ACME"));
            Assert.That(parsed.Model,                             Is.EqualTo("WallBox-b100"));
            Assert.That(parsed.SerialNumber,                      Is.EqualTo("123"));
            Assert.That(parsed.FirmwareVersion,                   Is.EqualTo("v1.0"));
            Assert.That(parsed.InstructionProcessingDelay,        Is.EqualTo(Duration.FromMilliseconds(3000)));
            Assert.That(parsed.AvailableControlTypes,             Is.EqualTo(new[] { ControlType.FillRateBasedControl }));
            Assert.That(parsed.Currency,                          Is.EqualTo(Currency.EUR));
            Assert.That(parsed.ProvidesForecast,                  Is.False);
            Assert.That(parsed.ProvidesPowerMeasurementTypes,     Is.EqualTo(new[] { CommodityQuantity.ElectricPower3PhaseSymmetric }));
            Assert.That(parsed.GetHashCode(),                     Is.EqualTo(message.GetHashCode()));
            Assert.That(message.Clone(),                          Is.EqualTo(message));
            Assert.That(message.ToString(),                       Does.EndWith($"[{message.MessageId}]"));

        }

        [Test]
        public void ResourceManagerDetails_MatchesTheDocumentationExample()
        {

            var json = JObject.Parse("""
                {
                  "message_type": "ResourceManagerDetails",
                  "message_id": "0c41efc2-771d-468f-afdc-fb69255dad33",
                  "resource_id": "acme_ev_xxxxxx",
                  "name": "My Electric Vehicle RM",
                  "roles": [
                    {
                      "role": "ENERGY_CONSUMER",
                      "commodity": "ELECTRICITY"
                    }
                  ],
                  "manufacturer": "ACME",
                  "model": "WallBox-b100",
                  "serial_number": "123",
                  "firmware_version": "v1.0",
                  "instruction_processing_delay": 3000,
                  "available_control_types": [
                    "FILL_RATE_BASED_CONTROL"
                  ],
                  "provides_forecast": false,
                  "provides_power_measurement_types": [
                    "ELECTRIC.POWER.3_PHASE_SYMMETRIC"
                  ]
                }
                """);

            Assert.That(ResourceManagerDetails.TryParse(json, out var message, out var error), Is.True, error);
            Assert.That(message!.MessageId.ToString(),          Is.EqualTo("0c41efc2-771d-468f-afdc-fb69255dad33"));
            Assert.That(message.ResourceId.ToString(),          Is.EqualTo("acme_ev_xxxxxx"));
            Assert.That(message.Roles[0].Commodity,             Is.EqualTo(Commodity.Electricity));
            Assert.That(message.Currency,                       Is.Null);
            Assert.That(message.ProvidesPowerMeasurementTypes,  Is.EqualTo(new[] { CommodityQuantity.ElectricPower3PhaseSymmetric }));

            var reserialised = message.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "ResourceManagerDetails");
            Assert.That(reserialised.ContainsKey("currency"), Is.False);
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void ResourceManagerDetails_MandatoryOnly_OmitsOptionals()
        {

            var message = new ResourceManagerDetails(Resource_Id.Parse("acme_ev_xxxxxx"),
                                                     [ new Role(RoleType.EnergyConsumer, Commodity.Electricity) ],
                                                     Duration.FromMilliseconds(3000),
                                                     [ ControlType.FillRateBasedControl, ControlType.NotControllable ],
                                                     true,
                                                     [ CommodityQuantity.ElectricPowerL1, CommodityQuantity.ElectricPowerL2, CommodityQuantity.ElectricPowerL3 ],
                                                     MessageId: messageId);
            var json    = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "ResourceManagerDetails");

            Assert.That(json.ContainsKey("name"),              Is.False);
            Assert.That(json.ContainsKey("manufacturer"),      Is.False);
            Assert.That(json.ContainsKey("model"),             Is.False);
            Assert.That(json.ContainsKey("serial_number"),     Is.False);
            Assert.That(json.ContainsKey("firmware_version"),  Is.False);
            Assert.That(json.ContainsKey("currency"),          Is.False);

            Assert.That(ResourceManagerDetails.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                                Is.EqualTo(message));
            Assert.That(parsed!.Name,                          Is.Null);
            Assert.That(parsed.Currency,                       Is.Null);
            Assert.That(parsed.AvailableControlTypes,          Has.Count.EqualTo(2));
            Assert.That(parsed.ProvidesPowerMeasurementTypes,  Has.Count.EqualTo(3));

        }

        [Test]
        public void ResourceManagerDetails_RejectsMissingMandatory_AndAdditionalProperties()
        {

            var json = EVResourceManagerDetails().ToJSON();

            foreach (var property in new[] { "resource_id", "roles", "instruction_processing_delay", "available_control_types", "provides_forecast", "provides_power_measurement_types" })
            {
                var missing = (JObject) json.DeepClone();
                missing.Remove(property);
                Assert.That(ResourceManagerDetails.TryParse(missing, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));
            }

            var extra = (JObject) json.DeepClone();
            extra.Add("foo", "bar");
            Assert.That(ResourceManagerDetails.TryParse(extra, out _, out var error2), Is.True, error2);
            Assert.That(ResourceManagerDetails.TryParse(extra, out _, out error2, new S2ParserOptions { RejectAdditionalProperties = true }), Is.False);
            Assert.That(error2, Does.Contain("foo"));

        }

        [Test]
        [S2C("Rules.ResourceManagerDetails.NoSelection")]
        public void ResourceManagerDetails_RejectsNoSelection_AsAvailableControlType()
        {

            Assert.That(() => new ResourceManagerDetails(Resource_Id.Parse("acme_ev_xxxxxx"),
                                                         [ new Role(RoleType.EnergyConsumer, Commodity.Electricity) ],
                                                         Duration.FromMilliseconds(3000),
                                                         [ ControlType.FillRateBasedControl, ControlType.NoSelection ],
                                                         false,
                                                         [ CommodityQuantity.ElectricPowerL1 ]),
                        Throws.ArgumentException.With.Message.Contains("NO_SELECTION"));

            var json = EVResourceManagerDetails().ToJSON();
            json["available_control_types"] = new JArray("NO_SELECTION");

            Assert.That(ResourceManagerDetails.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("NO_SELECTION"));

        }

        [Test]
        public void ResourceManagerDetails_EnforcesArrayBounds()
        {

            var resourceId  = Resource_Id.Parse("acme_ev_xxxxxx");
            var role        = new Role(RoleType.EnergyConsumer, Commodity.Electricity);
            var delay       = Duration.FromMilliseconds(3000);

            // roles 1..3
            Assert.That(() => new ResourceManagerDetails(resourceId, [], delay, [ ControlType.FillRateBasedControl ], false, [ CommodityQuantity.ElectricPowerL1 ]),
                        Throws.ArgumentException.With.Message.Contains("role"));
            Assert.That(() => new ResourceManagerDetails(resourceId, [ role, role, role, role ], delay, [ ControlType.FillRateBasedControl ], false, [ CommodityQuantity.ElectricPowerL1 ]),
                        Throws.ArgumentException.With.Message.Contains("3 roles"));

            // available_control_types 1..5
            Assert.That(() => new ResourceManagerDetails(resourceId, [ role ], delay, [], false, [ CommodityQuantity.ElectricPowerL1 ]),
                        Throws.ArgumentException.With.Message.Contains("control type"));
            Assert.That(() => new ResourceManagerDetails(resourceId, [ role ], delay,
                                                         [ ControlType.PowerEnvelopeBasedControl, ControlType.PowerProfileBasedControl, ControlType.OperationModeBasedControl,
                                                           ControlType.FillRateBasedControl, ControlType.DemandDrivenBasedControl, ControlType.NotControllable ],
                                                         false, [ CommodityQuantity.ElectricPowerL1 ]),
                        Throws.ArgumentException.With.Message.Contains("5 available control types"));

            // provides_power_measurement_types 1..10
            Assert.That(() => new ResourceManagerDetails(resourceId, [ role ], delay, [ ControlType.FillRateBasedControl ], false, []),
                        Throws.ArgumentException.With.Message.Contains("power measurement type"));
            Assert.That(() => new ResourceManagerDetails(resourceId, [ role ], delay, [ ControlType.FillRateBasedControl ], false,
                                                         Enumerable.Repeat(CommodityQuantity.ElectricPowerL1, 11).ToList()),
                        Throws.ArgumentException.With.Message.Contains("10 provided power measurement types"));

            var json = EVResourceManagerDetails().ToJSON();
            json["roles"] = new JArray();
            Assert.That(ResourceManagerDetails.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("roles"));

        }

        #endregion

        #region SessionRequest

        [Test]
        public void SessionRequest_RoundTrips_AndIsSchemaValid()
        {

            var message = new SessionRequest(SessionRequestType.Terminate, "shutting down", messageId);
            var json    = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "SessionRequest");

            Assert.That(json["request"]?.Value<String>(),           Is.EqualTo("TERMINATE"));
            Assert.That(json["diagnostic_label"]?.Value<String>(),  Is.EqualTo("shutting down"));

            Assert.That(SessionRequest.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                    Is.EqualTo(message));
            Assert.That(parsed!.Request,           Is.EqualTo(SessionRequestType.Terminate));
            Assert.That(parsed.DiagnosticLabel,    Is.EqualTo("shutting down"));
            Assert.That(parsed.GetHashCode(),      Is.EqualTo(message.GetHashCode()));
            Assert.That(message.Clone(),           Is.EqualTo(message));
            Assert.That(message.ToString(),        Does.EndWith($"[{messageId}]"));

        }

        [Test]
        public void SessionRequest_MatchesTheDocumentationExample()
        {

            var json = JObject.Parse("""
                {
                  "message_type": "SessionRequest",
                  "message_id": "4f93f58f-0d6c-44b5-b35a-bf6dcb845c5f",
                  "request": "RECONNECT",
                  "diagnostic_label": "string"
                }
                """);

            Assert.That(SessionRequest.TryParse(json, out var message, out var error), Is.True, error);
            Assert.That(message!.Request,          Is.EqualTo(SessionRequestType.Reconnect));
            Assert.That(message.DiagnosticLabel,   Is.EqualTo("string"));

            var reserialised = message.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "SessionRequest");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void SessionRequest_MandatoryOnly_OmitsTheDiagnosticLabel()
        {

            var message = new SessionRequest(SessionRequestType.Reconnect, MessageId: messageId);
            var json    = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "SessionRequest");
            Assert.That(json.ContainsKey("diagnostic_label"), Is.False);

            Assert.That(SessionRequest.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                   Is.EqualTo(message));
            Assert.That(parsed!.DiagnosticLabel,  Is.Null);

        }

        [Test]
        public void SessionRequest_RejectsMissingMandatory_AndAdditionalProperties()
        {

            var missing = JObject.Parse("""{ "message_type": "SessionRequest", "message_id": "9a8d2f1c-0000-4000-8000-000000000001" }""");
            Assert.That(SessionRequest.TryParse(missing, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("request"));

            var unknown = JObject.Parse("""{ "message_type": "SessionRequest", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "request": "HIBERNATE" }""");
            Assert.That(SessionRequest.TryParse(unknown, out _, out error), Is.False);
            Assert.That(error, Does.Contain("HIBERNATE"));

            var extra = JObject.Parse("""{ "message_type": "SessionRequest", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "request": "TERMINATE", "foo": 1 }""");
            Assert.That(SessionRequest.TryParse(extra, out _, out error), Is.True, error);
            Assert.That(SessionRequest.TryParse(extra, out _, out error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        #endregion

        #region PowerMeasurement

        [Test]
        public void PowerMeasurement_RoundTrips_AndIsSchemaValid()
        {

            var message = new PowerMeasurement(timestamp,
                                               [
                                                   new PowerValue(CommodityQuantity.ElectricPowerL1, 1200.5),
                                                   new PowerValue(CommodityQuantity.ElectricPowerL2, 1180),
                                                   new PowerValue(CommodityQuantity.ElectricPowerL3, 1210.25)
                                               ],
                                               messageId);
            var json    = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "PowerMeasurement");

            Assert.That(json["measurement_timestamp"]?.Value<String>(),  Is.EqualTo("2024-08-24T14:00:00Z"));
            Assert.That(json["values"],                                  Has.Count.EqualTo(3));

            Assert.That(PowerMeasurement.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                         Is.EqualTo(message));
            Assert.That(parsed!.MeasurementTimestamp,   Is.EqualTo(timestamp));
            Assert.That(parsed.Values[1].Value,         Is.EqualTo(1180));
            Assert.That(parsed.GetHashCode(),           Is.EqualTo(message.GetHashCode()));
            Assert.That(message.Clone(),                Is.EqualTo(message));
            Assert.That(message.ToString(),             Does.EndWith($"[{messageId}]"));

        }

        [Test]
        public void PowerMeasurement_MatchesTheDocumentationExample()
        {

            var json = JObject.Parse("""
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
                """);

            Assert.That(PowerMeasurement.TryParse(json, out var message, out var error), Is.True, error);
            Assert.That(message!.MeasurementTimestamp,          Is.EqualTo(new DateTimeOffset(2019, 8, 24, 14, 15, 22, TimeSpan.Zero)));
            Assert.That(message.Values,                         Has.Count.EqualTo(1));
            Assert.That(message.Values[0].CommodityQuantity,    Is.EqualTo(CommodityQuantity.ElectricPower3PhaseSymmetric));
            Assert.That(message.Values[0].Value,                Is.EqualTo(10963));

            var reserialised = message.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "PowerMeasurement");
            Assert.That(reserialised["measurement_timestamp"]?.Value<String>(), Is.EqualTo("2019-08-24T14:15:22Z"));
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void PowerMeasurement_RejectsMissingMandatory_AndAdditionalProperties()
        {

            var missingTimestamp = JObject.Parse("""{ "message_type": "PowerMeasurement", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "values": [ { "commodity_quantity": "ELECTRIC.POWER.L1", "value": 1 } ] }""");
            Assert.That(PowerMeasurement.TryParse(missingTimestamp, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("measurement_timestamp"));

            var missingValues = JObject.Parse("""{ "message_type": "PowerMeasurement", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "measurement_timestamp": "2024-08-24T14:00:00Z" }""");
            Assert.That(PowerMeasurement.TryParse(missingValues, out _, out error), Is.False);
            Assert.That(error, Does.Contain("values"));

            var extra = JObject.Parse("""{ "message_type": "PowerMeasurement", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "measurement_timestamp": "2024-08-24T14:00:00Z", "values": [ { "commodity_quantity": "ELECTRIC.POWER.L1", "value": 1 } ], "foo": 1 }""");
            Assert.That(PowerMeasurement.TryParse(extra, out _, out error), Is.True, error);
            Assert.That(PowerMeasurement.TryParse(extra, out _, out error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("foo"));

            var naive = JObject.Parse("""{ "message_type": "PowerMeasurement", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "measurement_timestamp": "2024-08-24T14:00:00", "values": [ { "commodity_quantity": "ELECTRIC.POWER.L1", "value": 1 } ] }""");
            Assert.That(PowerMeasurement.TryParse(naive, out _, out error), Is.True, error);
            Assert.That(PowerMeasurement.TryParse(naive, out _, out error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("UTC offset"));

        }

        [Test]
        [S2C("Rules.PowerMeasurement.OnePerCommodityQuantity")]
        public void PowerMeasurement_RejectsDuplicateCommodityQuantities()
        {

            Assert.That(() => new PowerMeasurement(timestamp,
                                                   [
                                                       new PowerValue(CommodityQuantity.ElectricPowerL1, 100),
                                                       new PowerValue(CommodityQuantity.ElectricPowerL1, 200)
                                                   ]),
                        Throws.ArgumentException.With.Message.Contains("ELECTRIC.POWER.L1"));

            var json = JObject.Parse("""
                {
                  "message_type": "PowerMeasurement",
                  "message_id": "9a8d2f1c-0000-4000-8000-000000000001",
                  "measurement_timestamp": "2024-08-24T14:00:00Z",
                  "values": [
                    { "commodity_quantity": "ELECTRIC.POWER.L1", "value": 100 },
                    { "commodity_quantity": "ELECTRIC.POWER.L1", "value": 200 }
                  ]
                }
                """);

            Assert.That(PowerMeasurement.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("at most one power value per commodity quantity"));

        }

        [Test]
        public void PowerMeasurement_EnforcesArrayBounds()
        {

            Assert.That(() => new PowerMeasurement(timestamp, []),
                        Throws.ArgumentException.With.Message.Contains("at least one"));

            var elevenQuantities = new[] {
                                       CommodityQuantity.ElectricPowerL1, CommodityQuantity.ElectricPowerL2, CommodityQuantity.ElectricPowerL3,
                                       CommodityQuantity.ElectricPower3PhaseSymmetric, CommodityQuantity.NaturalGasFlowRate, CommodityQuantity.HydrogenFlowRate,
                                       CommodityQuantity.HeatTemperature, CommodityQuantity.HeatFlowRate, CommodityQuantity.HeatThermalPower,
                                       CommodityQuantity.OilFlowRate, CommodityQuantity.OilFlowRate
                                   };

            // 11 items: the bound is checked before the uniqueness rule
            Assert.That(() => new PowerMeasurement(timestamp, elevenQuantities.Select(quantity => new PowerValue(quantity, 1)).ToList()),
                        Throws.ArgumentException.With.Message.Contains("10 power values"));

            var json = JObject.Parse("""{ "message_type": "PowerMeasurement", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "measurement_timestamp": "2024-08-24T14:00:00Z", "values": [] }""");
            Assert.That(PowerMeasurement.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("values"));

        }

        #endregion

        #region PowerForecast

        [Test]
        public void PowerForecast_RoundTrips_AndIsSchemaValid()
        {

            var message = SimplePowerForecast();
            var json    = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "PowerForecast");

            Assert.That(json["start_time"]?.Value<String>(),  Is.EqualTo("2024-08-24T14:00:00Z"));
            Assert.That(json["elements"],                     Has.Count.EqualTo(2));

            Assert.That(PowerForecast.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                                          Is.EqualTo(message));
            Assert.That(parsed!.StartTime,                               Is.EqualTo(timestamp));
            Assert.That(parsed.Elements[0].Duration,                     Is.EqualTo(Duration.FromMilliseconds(3600000)));
            Assert.That(parsed.Elements[0].PowerValues[0].ValueExpected, Is.EqualTo(545.1));
            Assert.That(parsed.Elements[1].PowerValues[0].ValueUpperLimit, Is.Null);
            Assert.That(parsed.GetHashCode(),                            Is.EqualTo(message.GetHashCode()));
            Assert.That(message.Clone(),                                 Is.EqualTo(message));
            Assert.That(message.ToString(),                              Does.EndWith($"[{messageId}]"));

        }

        [Test]
        public void PowerForecast_MatchesTheDocumentationExample()
        {

            // nocontrol.md
            var json = JObject.Parse("""
                {
                  "message_type": "PowerForecast",
                  "message_id": "bde674ac-66ef-4fed-8b12-d01b8404e10d",
                  "start_time": "2024-08-24T14:00:00Z",
                  "elements": [
                    {
                      "duration": 3600000,
                      "power_values": [
                        {
                          "value_upper_limit": 1000.0,
                          "value_upper_95PPR": 960.0,
                          "value_upper_68PPR": 800.0,
                          "value_expected": 545.1,
                          "value_lower_68PPR": 340.0,
                          "value_lower_95PPR": 100.0,
                          "value_lower_limit": 0.0,
                          "commodity_quantity": "ELECTRIC.POWER.L1"
                        }
                      ]
                    }
                  ]
                }
                """);

            Assert.That(PowerForecast.TryParse(json, out var message, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(message!.StartTime,                                     Is.EqualTo(timestamp));
            Assert.That(message.Elements,                                       Has.Count.EqualTo(1));
            Assert.That(message.Elements[0].Duration.Milliseconds,              Is.EqualTo(3600000));
            Assert.That(message.Elements[0].PowerValues[0].ValueExpected,       Is.EqualTo(545.1));
            Assert.That(message.Elements[0].PowerValues[0].ValueUpperLimit,     Is.EqualTo(1000.0));
            Assert.That(message.Elements[0].PowerValues[0].ValueLowerLimit,     Is.EqualTo(0.0));
            Assert.That(message.Elements[0].PowerValues[0].CommodityQuantity,   Is.EqualTo(CommodityQuantity.ElectricPowerL1));

            var reserialised = message.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "PowerForecast");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void PowerForecast_RejectsMissingMandatory_AndAdditionalProperties()
        {

            var json = SimplePowerForecast().ToJSON();

            foreach (var property in new[] { "start_time", "elements" })
            {
                var missing = (JObject) json.DeepClone();
                missing.Remove(property);
                Assert.That(PowerForecast.TryParse(missing, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));
            }

            var extra = (JObject) json.DeepClone();
            extra.Add("foo", 1);
            Assert.That(PowerForecast.TryParse(extra, out _, out var error2), Is.True, error2);
            Assert.That(PowerForecast.TryParse(extra, out _, out error2, S2ParserOptions.Strict), Is.False);
            Assert.That(error2, Does.Contain("foo"));

            var invalidTimestamp = (JObject) json.DeepClone();
            invalidTimestamp["start_time"] = "yesterday";
            Assert.That(PowerForecast.TryParse(invalidTimestamp, out _, out error2), Is.False);
            Assert.That(error2, Does.Contain("start_time").Or.Contain("start time").Or.Contain("timestamp"));

        }

        [Test]
        public void PowerForecast_EnforcesArrayBounds()
        {

            Assert.That(() => new PowerForecast(timestamp, []),
                        Throws.ArgumentException.With.Message.Contains("at least one"));

            var element = new PowerForecastElement(Duration.FromMilliseconds(300000),
                                                   [ new PowerForecastValue(1, CommodityQuantity.ElectricPowerL1) ]);

            Assert.That(() => new PowerForecast(timestamp, Enumerable.Repeat(element, 288).ToList()),
                        Throws.Nothing);

            Assert.That(() => new PowerForecast(timestamp, Enumerable.Repeat(element, 289).ToList()),
                        Throws.ArgumentException.With.Message.Contains("288"));

            var json = JObject.Parse("""{ "message_type": "PowerForecast", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "start_time": "2024-08-24T14:00:00Z", "elements": [] }""");
            Assert.That(PowerForecast.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("elements"));

            // the nested rule of PowerForecastElement (at most one value per commodity quantity) surfaces in the error response
            var duplicate = JObject.Parse("""
                {
                  "message_type": "PowerForecast",
                  "message_id": "9a8d2f1c-0000-4000-8000-000000000001",
                  "start_time": "2024-08-24T14:00:00Z",
                  "elements": [
                    {
                      "duration": 3600000,
                      "power_values": [
                        { "value_expected": 1, "commodity_quantity": "ELECTRIC.POWER.L1" },
                        { "value_expected": 2, "commodity_quantity": "ELECTRIC.POWER.L1" }
                      ]
                    }
                  ]
                }
                """);
            Assert.That(PowerForecast.TryParse(duplicate, out _, out error), Is.False);
            Assert.That(error, Does.Contain("ELECTRIC.POWER.L1"));

        }

        #endregion

        #region InstructionStatusUpdate

        [Test]
        public void InstructionStatusUpdate_RoundTrips_AndIsSchemaValid()
        {

            var instructionId = Instruction_Id.Parse("a4e1b4c1-0000-4000-8000-000000000042");
            var message       = new InstructionStatusUpdate(instructionId, InstructionStatus.Succeeded, timestamp, messageId);
            var json          = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "InstructionStatusUpdate");

            Assert.That(json["instruction_id"]?.Value<String>(),  Is.EqualTo(instructionId.ToString()));
            Assert.That(json["status_type"]?.   Value<String>(),  Is.EqualTo("SUCCEEDED"));
            Assert.That(json["timestamp"]?.     Value<String>(),  Is.EqualTo("2024-08-24T14:00:00Z"));

            Assert.That(InstructionStatusUpdate.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                  Is.EqualTo(message));
            Assert.That(parsed!.InstructionId,   Is.EqualTo(instructionId));
            Assert.That(parsed.StatusType,       Is.EqualTo(InstructionStatus.Succeeded));
            Assert.That(parsed.Timestamp,        Is.EqualTo(timestamp));
            Assert.That(parsed.GetHashCode(),    Is.EqualTo(message.GetHashCode()));
            Assert.That(message.Clone(),         Is.EqualTo(message));
            Assert.That(message.ToString(),      Does.EndWith($"[{messageId}]"));

        }

        [Test]
        public void InstructionStatusUpdate_MatchesTheDocumentationExample()
        {

            var json = JObject.Parse("""
                {
                  "message_type": "InstructionStatusUpdate",
                  "message_id": "84522a12-7960-48b1-abf3-b7694dac80e6",
                  "instruction_id": "instruction1",
                  "status_type": "NEW",
                  "timestamp": "2019-08-24T14:15:22Z"
                }
                """);

            Assert.That(InstructionStatusUpdate.TryParse(json, out var message, out var error), Is.True, error);
            Assert.That(message!.InstructionId.ToString(),  Is.EqualTo("instruction1"));
            Assert.That(message.StatusType,                 Is.EqualTo(InstructionStatus.New));
            Assert.That(message.Timestamp,                  Is.EqualTo(new DateTimeOffset(2019, 8, 24, 14, 15, 22, TimeSpan.Zero)));

            var reserialised = message.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "InstructionStatusUpdate");
            JSONAssert.AssertDeepEquals(json, reserialised);

            // "instruction1" is not a UUID: Strict mode rejects it
            Assert.That(InstructionStatusUpdate.TryParse(json, out _, out error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("UUID"));

        }

        [Test]
        public void InstructionStatusUpdate_RejectsMissingMandatory_AndAdditionalProperties()
        {

            var json = new InstructionStatusUpdate(Instruction_Id.Parse("a4e1b4c1-0000-4000-8000-000000000042"), InstructionStatus.Started, timestamp, messageId).ToJSON();

            foreach (var property in new[] { "instruction_id", "status_type", "timestamp" })
            {
                var missing = (JObject) json.DeepClone();
                missing.Remove(property);
                Assert.That(InstructionStatusUpdate.TryParse(missing, out _, out var error), Is.False, property);
                Assert.That(error, Does.Contain(property));
            }

            var unknown = (JObject) json.DeepClone();
            unknown["status_type"] = "PAUSED";
            Assert.That(InstructionStatusUpdate.TryParse(unknown, out _, out var error2), Is.False);
            Assert.That(error2, Does.Contain("PAUSED"));

            var extra = (JObject) json.DeepClone();
            extra.Add("foo", 1);
            Assert.That(InstructionStatusUpdate.TryParse(extra, out _, out error2), Is.True, error2);
            Assert.That(InstructionStatusUpdate.TryParse(extra, out _, out error2, S2ParserOptions.Strict), Is.False);
            Assert.That(error2, Does.Contain("foo"));

        }

        [Test]
        public void InstructionStatusUpdate_RejectsEmptyIdentifications()
        {

            Assert.That(() => new InstructionStatusUpdate(default, InstructionStatus.New, timestamp),
                        Throws.ArgumentException.With.Message.Contains("instruction identification"));

            Assert.That(() => new InstructionStatusUpdate(Instruction_Id.Parse("instruction1"), default, timestamp),
                        Throws.ArgumentException.With.Message.Contains("instruction status"));

        }

        #endregion

        #region RevokeObject

        [Test]
        public void RevokeObject_RoundTrips_AndIsSchemaValid()
        {

            var objectId = S2Object_Id.Parse("a4e1b4c1-0000-4000-8000-000000000042");
            var message  = new RevokeObject(RevokableObject.FRBC_Instruction, objectId, messageId);
            var json     = message.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "RevokeObject");

            Assert.That(json["object_type"]?.Value<String>(),  Is.EqualTo("FRBC.Instruction"));
            Assert.That(json["object_id"]?.  Value<String>(),  Is.EqualTo(objectId.ToString()));

            Assert.That(RevokeObject.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,                 Is.EqualTo(message));
            Assert.That(parsed!.ObjectType,     Is.EqualTo(RevokableObject.FRBC_Instruction));
            Assert.That(parsed.ObjectId,        Is.EqualTo(objectId));
            Assert.That(parsed.GetHashCode(),   Is.EqualTo(message.GetHashCode()));
            Assert.That(message.Clone(),        Is.EqualTo(message));
            Assert.That(message.ToString(),     Does.EndWith($"[{messageId}]"));

        }

        [Test]
        public void RevokeObject_ObjectId_ConvertsFromTypedIdentifications()
        {

            var instructionId  = Instruction_Id.Parse("instruction1");
            var byInstruction  = new RevokeObject(RevokableObject.PEBC_Instruction, S2Object_Id.From(instructionId));
            Assert.That(byInstruction.ObjectId.ToString(), Is.EqualTo("instruction1"));

            var systemDescriptionMessageId = Message_Id.Parse("0c41efc2-771d-468f-afdc-fb69255dad33");
            var bySystemDescription        = new RevokeObject(RevokableObject.FRBC_SystemDescription, S2Object_Id.From(systemDescriptionMessageId));
            Assert.That(bySystemDescription.ObjectId.ToString(), Is.EqualTo("0c41efc2-771d-468f-afdc-fb69255dad33"));

            S2SchemaValidator.AssertValidMessage(byInstruction.ToJSON(),       "RevokeObject");
            S2SchemaValidator.AssertValidMessage(bySystemDescription.ToJSON(), "RevokeObject");

        }

        [Test]
        public void RevokeObject_RejectsMissingMandatory_AndAdditionalProperties()
        {

            var missingType = JObject.Parse("""{ "message_type": "RevokeObject", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "object_id": "instruction1" }""");
            Assert.That(RevokeObject.TryParse(missingType, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("object_type"));

            var missingId = JObject.Parse("""{ "message_type": "RevokeObject", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "object_type": "OMBC.Instruction" }""");
            Assert.That(RevokeObject.TryParse(missingId, out _, out error), Is.False);
            Assert.That(error, Does.Contain("object_id"));

            var unknownType = JObject.Parse("""{ "message_type": "RevokeObject", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "object_type": "FRBC.StorageStatus", "object_id": "instruction1" }""");
            Assert.That(RevokeObject.TryParse(unknownType, out _, out error), Is.False);
            Assert.That(error, Does.Contain("FRBC.StorageStatus"));

            var invalidId = JObject.Parse("""{ "message_type": "RevokeObject", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "object_type": "OMBC.Instruction", "object_id": "x" }""");
            Assert.That(RevokeObject.TryParse(invalidId, out _, out error), Is.False);
            Assert.That(error, Does.Contain("object_id").Or.Contain("object identification"));

            var extra = JObject.Parse("""{ "message_type": "RevokeObject", "message_id": "9a8d2f1c-0000-4000-8000-000000000001", "object_type": "OMBC.Instruction", "object_id": "instruction1", "foo": 1 }""");
            Assert.That(RevokeObject.TryParse(extra, out _, out error), Is.True, error);
            Assert.That(RevokeObject.TryParse(extra, out _, out error, new S2ParserOptions { RejectAdditionalProperties = true }), Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        public void RevokeObject_RejectsEmptyTypeOrIdentification()
        {

            Assert.That(() => new RevokeObject(default, S2Object_Id.Parse("instruction1")),
                        Throws.ArgumentException.With.Message.Contains("revokable object type"));

            Assert.That(() => new RevokeObject(RevokableObject.DDBC_Instruction, default),
                        Throws.ArgumentException.With.Message.Contains("object identification"));

        }

        #endregion

        #region S2MessageParser

        [Test]
        public void MessageParser_DispatchesResourceManagerDetails()
        {

            var message = EVResourceManagerDetails();
            var text    = message.ToJSON().ToString();

            Assert.That(S2MessageParser.TryParse(text, null, out var parsed, out var error), Is.True, error?.DiagnosticLabel);
            Assert.That(parsed,                                                  Is.TypeOf<ResourceManagerDetails>());
            Assert.That(parsed,                                                  Is.EqualTo(message));
            Assert.That(((ResourceManagerDetails) parsed!).ResourceId.ToString(), Is.EqualTo("acme_ev_xxxxxx"));
            Assert.That(S2MessageParser.KnownMessageTypes,                       Does.Contain(ResourceManagerDetails.MessageTypeName));

            // a rule violation surfaces as INVALID_MESSAGE with the message id
            var json = message.ToJSON();
            json["available_control_types"] = new JArray("NO_SELECTION");

            Assert.That(S2MessageParser.TryParse(json.ToString(), null, out _, out error), Is.False);
            Assert.That(error!.Status,                       Is.EqualTo(ReceptionStatusValue.InvalidMessage));
            Assert.That(error.SubjectMessageId,              Is.EqualTo(message.MessageId));
            Assert.That(error.DiagnosticLabel,               Does.Contain("NO_SELECTION"));

        }

        #endregion

    }

}

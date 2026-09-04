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

namespace cloud.charging.open.protocols.S2.Tests.Messages.PEBC
{

    /// <summary>
    /// Tests of the Power Envelope Based Control messages: PEBC.PowerConstraints,
    /// PEBC.EnergyConstraint and PEBC.Instruction. The values follow the PV inverter
    /// example of the S2 documentation (a 4 kWp installation on phase L1 whose feed-in,
    /// negative by the S2 sign convention, may be curtailed by the CEM).
    /// </summary>
    [TestFixture]
    public sealed class PEBCMessageTests
    {

        #region Data

        private static readonly DateTimeOffset  validFrom       = new (2024, 8, 24, 14, 15, 22, TimeSpan.Zero);
        private static readonly DateTimeOffset  validUntil      = new (2024, 8, 25, 14, 15, 22, TimeSpan.Zero);
        private static readonly DateTimeOffset  executionTime   = new (2024, 8, 24, 15,  0,  0, TimeSpan.Zero);
        private static readonly Duration        oneHour         = Duration.FromMilliseconds(3600000);

        /// <summary>
        /// The strict options minus the UUID requirement: the tests use the readable identifiers
        /// of the S2 documentation examples ("powerConstraint1", "pe_L1"), which are valid S2 IDs but no UUIDs.
        /// </summary>
        private static readonly S2ParserOptions strict          = S2ParserOptions.Strict with { RequireUUIDs = false };

        private static PEBC_AllowedLimitRange LowerRange(CommodityQuantity? Quantity = null)
            => new (Quantity ?? CommodityQuantity.ElectricPowerL1,
                    PEBC_PowerEnvelopeLimitType.LowerLimit,
                    new NumberRange(-4000, 0),
                    false);

        private static PEBC_AllowedLimitRange UpperRange(CommodityQuantity? Quantity = null)
            => new (Quantity ?? CommodityQuantity.ElectricPowerL1,
                    PEBC_PowerEnvelopeLimitType.UpperLimit,
                    new NumberRange(0, 0),
                    false);

        private static PEBC_PowerEnvelope Envelope(String Id, CommodityQuantity? Quantity = null)
            => new (PowerEnvelope_Id.Parse(Id),
                    Quantity ?? CommodityQuantity.ElectricPowerL1,
                    [ new PEBC_PowerEnvelopeElement(oneHour, 0, -2000) ]);


        #endregion


        #region PEBC.PowerConstraints

        [Test]
        public void PowerConstraints_RoundTrips_AndIsSchemaValid()
        {

            var powerConstraints = new PEBC_PowerConstraints(
                                       PowerConstraints_Id.Parse("powerConstraint1"),
                                       validFrom,
                                       PEBC_PowerEnvelopeConsequenceType.Vanish,
                                       [ LowerRange(), UpperRange() ],
                                       validUntil
                                   );

            var json = powerConstraints.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "PEBC.PowerConstraints");

            Assert.That(json["message_type"]?.    Value<String>(), Is.EqualTo("PEBC.PowerConstraints"));
            Assert.That(json["message_id"]?.      Value<String>(), Is.EqualTo(powerConstraints.MessageId.ToString()));
            Assert.That(json["id"]?.              Value<String>(), Is.EqualTo("powerConstraint1"));
            Assert.That(json["valid_from"]?.      Value<String>(), Is.EqualTo("2024-08-24T14:15:22Z"));
            Assert.That(json["valid_until"]?.     Value<String>(), Is.EqualTo("2024-08-25T14:15:22Z"));
            Assert.That(json["consequence_type"]?.Value<String>(), Is.EqualTo("VANISH"));
            Assert.That(json["allowed_limit_ranges"],              Is.TypeOf<JArray>());

            Assert.That(PEBC_PowerConstraints.TryParse(json, out var parsed, out var error, strict), Is.True, error);
            Assert.That(parsed,                        Is.EqualTo(powerConstraints));
            Assert.That(parsed!.GetHashCode(),         Is.EqualTo(powerConstraints.GetHashCode()));
            Assert.That(parsed. MessageId,             Is.EqualTo(powerConstraints.MessageId));
            Assert.That(parsed. ValidFrom,             Is.EqualTo(validFrom));
            Assert.That(parsed. ValidUntil,            Is.EqualTo(validUntil));
            Assert.That(parsed. ConsequenceType,       Is.EqualTo(PEBC_PowerEnvelopeConsequenceType.Vanish));
            Assert.That(parsed. AllowedLimitRanges,    Has.Count.EqualTo(2));

            IRevokable revokable = parsed;
            Assert.That(revokable.RevokableObjectType,            Is.EqualTo(RevokableObject.PEBC_PowerConstraints));
            Assert.That(revokable.RevokableObjectId.ToString(),   Is.EqualTo("powerConstraint1"));

            var clone = powerConstraints.Clone();
            Assert.That(clone,                                    Is.EqualTo(powerConstraints));
            Assert.That(ReferenceEquals(clone, powerConstraints), Is.False);
            Assert.That(powerConstraints.ToString(),              Does.EndWith($"[{powerConstraints.MessageId}]"));

        }

        [Test]
        public void PowerConstraints_MatchesTheDocumentationExample()
        {

            // The PV example of the S2 documentation (pv.md), verbatim.
            var json = S2JSONExtensions.ParseS2JSON("""
                {
                  "message_type": "PEBC.PowerConstraints",
                  "message_id": "xxx",
                  "id": "powerConstraint1",
                  "valid_from": "2024-08-24T14:15:22Z",
                  "valid_until": "2024-08-25T14:15:22Z",
                  "consequence_type": "VANISH",
                  "allowed_limit_ranges": [
                    {
                      "commodity_quantity": "ELECTRIC.POWER.L1",
                      "limit_type": "LOWER_LIMIT",
                      "range_boundary": {
                        "start_of_range": -4000,
                        "end_of_range": 0
                      },
                      "abnormal_condition_only": false
                    },
                    {
                      "commodity_quantity": "ELECTRIC.POWER.L1",
                      "limit_type": "UPPER_LIMIT",
                      "range_boundary": {
                        "start_of_range": 0,
                        "end_of_range": 0
                      },
                      "abnormal_condition_only": false
                    }
                  ]
                }
                """);

            Assert.That(PEBC_PowerConstraints.TryParse(json, out var powerConstraints, out var error), Is.True, error);
            Assert.That(powerConstraints!.MessageId.ToString(),                       Is.EqualTo("xxx"));
            Assert.That(powerConstraints. Id.ToString(),                              Is.EqualTo("powerConstraint1"));
            Assert.That(powerConstraints. ValidFrom,                                  Is.EqualTo(validFrom));
            Assert.That(powerConstraints. ValidUntil,                                 Is.EqualTo(validUntil));
            Assert.That(powerConstraints. ConsequenceType,                            Is.EqualTo(PEBC_PowerEnvelopeConsequenceType.Vanish));
            Assert.That(powerConstraints. AllowedLimitRanges[0].LimitType,            Is.EqualTo(PEBC_PowerEnvelopeLimitType.LowerLimit));
            Assert.That(powerConstraints. AllowedLimitRanges[0].RangeBoundary,        Is.EqualTo(new NumberRange(-4000, 0)));
            Assert.That(powerConstraints. AllowedLimitRanges[1].LimitType,            Is.EqualTo(PEBC_PowerEnvelopeLimitType.UpperLimit));
            Assert.That(powerConstraints. AllowedLimitRanges[1].CommodityQuantity,    Is.EqualTo(CommodityQuantity.ElectricPowerL1));

            var reserialised = powerConstraints.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "PEBC.PowerConstraints");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void PowerConstraints_MandatoryOnly_OmitsValidUntil()
        {

            var powerConstraints = new PEBC_PowerConstraints(
                                       PowerConstraints_Id.NewRandom,
                                       validFrom,
                                       PEBC_PowerEnvelopeConsequenceType.Defer,
                                       [ LowerRange(), UpperRange() ]
                                   );

            var json = powerConstraints.ToJSON();

            Assert.That(json.ContainsKey("valid_until"), Is.False);
            Assert.That(powerConstraints.ValidUntil,     Is.Null);

            S2SchemaValidator.AssertValidMessage(json, "PEBC.PowerConstraints");

            Assert.That(PEBC_PowerConstraints.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed,             Is.EqualTo(powerConstraints));
            Assert.That(parsed!.ValidUntil, Is.Null);

        }

        [Test]
        public void PowerConstraints_MissingMandatoryProperty_Fails()
        {

            var json = new PEBC_PowerConstraints(PowerConstraints_Id.NewRandom,
                                                 validFrom,
                                                 PEBC_PowerEnvelopeConsequenceType.Vanish,
                                                 [ LowerRange(), UpperRange() ]).ToJSON();

            json.Remove("consequence_type");

            Assert.That(PEBC_PowerConstraints.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("consequence_type"));

            json.Remove("allowed_limit_ranges");
            json["consequence_type"] = "VANISH";

            Assert.That(PEBC_PowerConstraints.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("allowed_limit_ranges"));

        }

        [Test]
        public void PowerConstraints_Strict_RejectsAdditionalProperties()
        {

            var json = new PEBC_PowerConstraints(PowerConstraints_Id.NewRandom,
                                                 validFrom,
                                                 PEBC_PowerEnvelopeConsequenceType.Vanish,
                                                 [ LowerRange(), UpperRange() ]).ToJSON();

            json["foo"] = 42;

            Assert.That(PEBC_PowerConstraints.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PEBC_PowerConstraints.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Rules.PEBC.PowerConstraints.LimitRanges")]
        public void PowerConstraints_RequiresAtLeastTwoLimitRanges()
        {

            Assert.That(() => new PEBC_PowerConstraints(PowerConstraints_Id.NewRandom,
                                                        validFrom,
                                                        PEBC_PowerEnvelopeConsequenceType.Vanish,
                                                        [ LowerRange() ]),
                        Throws.ArgumentException);

            var json = new PEBC_PowerConstraints(PowerConstraints_Id.NewRandom,
                                                 validFrom,
                                                 PEBC_PowerEnvelopeConsequenceType.Vanish,
                                                 [ LowerRange(), UpperRange() ]).ToJSON();

            ((JArray) json["allowed_limit_ranges"]!).RemoveAt(1);

            Assert.That(PEBC_PowerConstraints.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("allowed_limit_ranges").And.Contain("at least 2"));

        }

        [Test]
        [S2C("Rules.PEBC.PowerConstraints.LimitRanges")]
        public void PowerConstraints_RequiresUpperAndLowerLimitPerCommodityQuantity()
        {

            // Two LOWER_LIMIT ranges, no UPPER_LIMIT
            Assert.That(() => new PEBC_PowerConstraints(PowerConstraints_Id.NewRandom,
                                                        validFrom,
                                                        PEBC_PowerEnvelopeConsequenceType.Vanish,
                                                        [ LowerRange(), LowerRange() ]),
                        Throws.ArgumentException.With.Message.Contains("UPPER_LIMIT"));

            // Two UPPER_LIMIT ranges, no LOWER_LIMIT
            Assert.That(() => new PEBC_PowerConstraints(PowerConstraints_Id.NewRandom,
                                                        validFrom,
                                                        PEBC_PowerEnvelopeConsequenceType.Vanish,
                                                        [ UpperRange(), UpperRange() ]),
                        Throws.ArgumentException.With.Message.Contains("LOWER_LIMIT"));

            // L1 complete, but L2 has only an UPPER_LIMIT
            Assert.That(() => new PEBC_PowerConstraints(PowerConstraints_Id.NewRandom,
                                                        validFrom,
                                                        PEBC_PowerEnvelopeConsequenceType.Vanish,
                                                        [ LowerRange(), UpperRange(), UpperRange(CommodityQuantity.ElectricPowerL2) ]),
                        Throws.ArgumentException.With.Message.Contains("ELECTRIC.POWER.L2"));

            // Multiple ranges with identical commodity quantity and limit type are allowed
            Assert.That(() => new PEBC_PowerConstraints(PowerConstraints_Id.NewRandom,
                                                        validFrom,
                                                        PEBC_PowerEnvelopeConsequenceType.Vanish,
                                                        [ LowerRange(), LowerRange(), UpperRange() ]),
                        Throws.Nothing);

            var json = S2JSONExtensions.ParseS2JSON("""
                {
                  "message_type": "PEBC.PowerConstraints",
                  "message_id": "9a8d2f1c-0000-4000-8000-000000000001",
                  "id": "powerConstraint1",
                  "valid_from": "2024-08-24T14:15:22Z",
                  "consequence_type": "VANISH",
                  "allowed_limit_ranges": [
                    {
                      "commodity_quantity": "ELECTRIC.POWER.L1",
                      "limit_type": "LOWER_LIMIT",
                      "range_boundary": { "start_of_range": -4000, "end_of_range": 0 },
                      "abnormal_condition_only": false
                    },
                    {
                      "commodity_quantity": "ELECTRIC.POWER.L1",
                      "limit_type": "LOWER_LIMIT",
                      "range_boundary": { "start_of_range": -4000, "end_of_range": -1000 },
                      "abnormal_condition_only": true
                    }
                  ]
                }
                """);

            Assert.That(PEBC_PowerConstraints.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("UPPER_LIMIT"));

        }

        [Test]
        [S2C("Rules.PEBC.PowerConstraints.ValidUntil")]
        public void PowerConstraints_ValidUntil_MustNotBeBeforeValidFrom()
        {

            Assert.That(() => new PEBC_PowerConstraints(PowerConstraints_Id.NewRandom,
                                                        validFrom,
                                                        PEBC_PowerEnvelopeConsequenceType.Vanish,
                                                        [ LowerRange(), UpperRange() ],
                                                        validFrom.AddSeconds(-1)),
                        Throws.ArgumentException);

            // valid_until == valid_from is allowed
            Assert.That(() => new PEBC_PowerConstraints(PowerConstraints_Id.NewRandom,
                                                        validFrom,
                                                        PEBC_PowerEnvelopeConsequenceType.Vanish,
                                                        [ LowerRange(), UpperRange() ],
                                                        validFrom),
                        Throws.Nothing);

            var json = new PEBC_PowerConstraints(PowerConstraints_Id.NewRandom,
                                                 validFrom,
                                                 PEBC_PowerEnvelopeConsequenceType.Vanish,
                                                 [ LowerRange(), UpperRange() ],
                                                 validUntil).ToJSON();

            json["valid_until"] = "2024-08-24T14:15:21Z";

            Assert.That(PEBC_PowerConstraints.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("valid_until").And.Contain("valid_from"));

        }

        #endregion

        #region PEBC.EnergyConstraint

        [Test]
        public void EnergyConstraint_RoundTrips_AndIsSchemaValid()
        {

            var energyConstraint = new PEBC_EnergyConstraint(
                                       EnergyConstraint_Id.Parse("energyconstraint1"),
                                       validFrom,
                                       validUntil,
                                       3000,
                                       1000,
                                       CommodityQuantity.ElectricPowerL1
                                   );

            var json = energyConstraint.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "PEBC.EnergyConstraint");

            Assert.That(json["message_type"]?.       Value<String>(), Is.EqualTo("PEBC.EnergyConstraint"));
            Assert.That(json["message_id"]?.         Value<String>(), Is.EqualTo(energyConstraint.MessageId.ToString()));
            Assert.That(json["id"]?.                 Value<String>(), Is.EqualTo("energyconstraint1"));
            Assert.That(json["valid_from"]?.         Value<String>(), Is.EqualTo("2024-08-24T14:15:22Z"));
            Assert.That(json["valid_until"]?.        Value<String>(), Is.EqualTo("2024-08-25T14:15:22Z"));
            Assert.That(json["upper_average_power"]?.Value<Double>(), Is.EqualTo(3000));
            Assert.That(json["lower_average_power"]?.Value<Double>(), Is.EqualTo(1000));
            Assert.That(json["commodity_quantity"]?. Value<String>(), Is.EqualTo("ELECTRIC.POWER.L1"));

            Assert.That(PEBC_EnergyConstraint.TryParse(json, out var parsed, out var error, strict), Is.True, error);
            Assert.That(parsed,                       Is.EqualTo(energyConstraint));
            Assert.That(parsed!.GetHashCode(),        Is.EqualTo(energyConstraint.GetHashCode()));
            Assert.That(parsed. ValidFrom,            Is.EqualTo(validFrom));
            Assert.That(parsed. ValidUntil,           Is.EqualTo(validUntil));
            Assert.That(parsed. UpperAveragePower,    Is.EqualTo(3000));
            Assert.That(parsed. LowerAveragePower,    Is.EqualTo(1000));
            Assert.That(parsed. CommodityQuantity,    Is.EqualTo(CommodityQuantity.ElectricPowerL1));

            IRevokable revokable = parsed;
            Assert.That(revokable.RevokableObjectType,            Is.EqualTo(RevokableObject.PEBC_EnergyConstraint));
            Assert.That(revokable.RevokableObjectId.ToString(),   Is.EqualTo("energyconstraint1"));

            var clone = energyConstraint.Clone();
            Assert.That(clone,                                    Is.EqualTo(energyConstraint));
            Assert.That(ReferenceEquals(clone, energyConstraint), Is.False);
            Assert.That(energyConstraint.ToString(),              Does.EndWith($"[{energyConstraint.MessageId}]"));

        }

        [Test]
        public void EnergyConstraint_MatchesTheDocumentationExample()
        {

            // The PV example of the S2 documentation (pv.md), verbatim.
            var json = S2JSONExtensions.ParseS2JSON("""
                {
                  "message_type": "PEBC.EnergyConstraint",
                  "message_id": "xxx",
                  "id": "energyconstraint1",
                  "valid_from": "2024-12-24T14:15:22Z",
                  "valid_until": "2024-12-25T14:15:22Z",
                  "upper_average_power": 3000,
                  "lower_average_power": 1000,
                  "commodity_quantity": "ELECTRIC.POWER.L1"
                }
                """);

            Assert.That(PEBC_EnergyConstraint.TryParse(json, out var energyConstraint, out var error), Is.True, error);
            Assert.That(energyConstraint!.MessageId.ToString(),   Is.EqualTo("xxx"));
            Assert.That(energyConstraint. Id.ToString(),          Is.EqualTo("energyconstraint1"));
            Assert.That(energyConstraint. ValidFrom,              Is.EqualTo(new DateTimeOffset(2024, 12, 24, 14, 15, 22, TimeSpan.Zero)));
            Assert.That(energyConstraint. ValidUntil,             Is.EqualTo(new DateTimeOffset(2024, 12, 25, 14, 15, 22, TimeSpan.Zero)));
            Assert.That(energyConstraint. UpperAveragePower,      Is.EqualTo(3000));
            Assert.That(energyConstraint. LowerAveragePower,      Is.EqualTo(1000));
            Assert.That(energyConstraint. CommodityQuantity,      Is.EqualTo(CommodityQuantity.ElectricPowerL1));

            var reserialised = energyConstraint.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "PEBC.EnergyConstraint");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void EnergyConstraint_MandatoryOnly_IsSchemaValid()
        {

            // Every property of PEBC.EnergyConstraint is mandatory; negative values denote production.
            var energyConstraint = new PEBC_EnergyConstraint(
                                       EnergyConstraint_Id.NewRandom,
                                       validFrom,
                                       validUntil,
                                       -1000,
                                       -3000,
                                       CommodityQuantity.ElectricPower3PhaseSymmetric
                                   );

            var json = energyConstraint.ToJSON();

            Assert.That(json.Properties().Select(property => property.Name),
                        Is.EqualTo(new[] { "message_type", "message_id", "id", "valid_from", "valid_until",
                                           "upper_average_power", "lower_average_power", "commodity_quantity" }));

            S2SchemaValidator.AssertValidMessage(json, "PEBC.EnergyConstraint");

            Assert.That(PEBC_EnergyConstraint.TryParse(json, out var parsed, out var error, S2ParserOptions.Strict), Is.True, error);
            Assert.That(parsed, Is.EqualTo(energyConstraint));

        }

        [Test]
        public void EnergyConstraint_MissingMandatoryProperty_Fails()
        {

            var json = new PEBC_EnergyConstraint(EnergyConstraint_Id.NewRandom,
                                                 validFrom,
                                                 validUntil,
                                                 3000,
                                                 1000,
                                                 CommodityQuantity.ElectricPowerL1).ToJSON();

            json.Remove("valid_until");

            Assert.That(PEBC_EnergyConstraint.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("valid_until"));

            json["valid_until"] = "2024-08-25T14:15:22Z";
            json.Remove("lower_average_power");

            Assert.That(PEBC_EnergyConstraint.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("lower_average_power"));

        }

        [Test]
        public void EnergyConstraint_Strict_RejectsAdditionalProperties()
        {

            var json = new PEBC_EnergyConstraint(EnergyConstraint_Id.NewRandom,
                                                 validFrom,
                                                 validUntil,
                                                 3000,
                                                 1000,
                                                 CommodityQuantity.ElectricPowerL1).ToJSON();

            json["foo"] = 42;

            Assert.That(PEBC_EnergyConstraint.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PEBC_EnergyConstraint.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Rules.PEBC.EnergyConstraint.AveragePower")]
        public void EnergyConstraint_LowerAveragePower_MustNotExceedUpperAveragePower()
        {

            Assert.That(() => new PEBC_EnergyConstraint(EnergyConstraint_Id.NewRandom,
                                                        validFrom,
                                                        validUntil,
                                                        1000,
                                                        3000,
                                                        CommodityQuantity.ElectricPowerL1),
                        Throws.ArgumentException);

            // equal values are allowed
            Assert.That(() => new PEBC_EnergyConstraint(EnergyConstraint_Id.NewRandom,
                                                        validFrom,
                                                        validUntil,
                                                        2000,
                                                        2000,
                                                        CommodityQuantity.ElectricPowerL1),
                        Throws.Nothing);

            var json = new PEBC_EnergyConstraint(EnergyConstraint_Id.NewRandom,
                                                 validFrom,
                                                 validUntil,
                                                 3000,
                                                 1000,
                                                 CommodityQuantity.ElectricPowerL1).ToJSON();

            json["lower_average_power"] = 3500;

            Assert.That(PEBC_EnergyConstraint.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("lower_average_power").And.Contain("upper_average_power"));

        }

        [Test]
        [S2C("Rules.PEBC.EnergyConstraint.ValidUntil")]
        public void EnergyConstraint_ValidUntil_MustNotBeBeforeValidFrom()
        {

            Assert.That(() => new PEBC_EnergyConstraint(EnergyConstraint_Id.NewRandom,
                                                        validFrom,
                                                        validFrom.AddSeconds(-1),
                                                        3000,
                                                        1000,
                                                        CommodityQuantity.ElectricPowerL1),
                        Throws.ArgumentException);

            var json = new PEBC_EnergyConstraint(EnergyConstraint_Id.NewRandom,
                                                 validFrom,
                                                 validUntil,
                                                 3000,
                                                 1000,
                                                 CommodityQuantity.ElectricPowerL1).ToJSON();

            json["valid_until"] = "2024-08-24T14:15:21Z";

            Assert.That(PEBC_EnergyConstraint.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("valid_until").And.Contain("valid_from"));

        }

        #endregion

        #region PEBC.Instruction

        [Test]
        public void Instruction_RoundTrips_AndIsSchemaValid()
        {

            var instruction = new PEBC_Instruction(
                                  Instruction_Id.Parse("envelope1"),
                                  executionTime,
                                  false,
                                  PowerConstraints_Id.Parse("powerConstraint1"),
                                  [ Envelope("pe_xxx") ]
                              );

            var json = instruction.ToJSON();

            S2SchemaValidator.AssertValidMessage(json, "PEBC.Instruction");

            Assert.That(json["message_type"]?.        Value<String>(),  Is.EqualTo("PEBC.Instruction"));
            Assert.That(json["message_id"]?.          Value<String>(),  Is.EqualTo(instruction.MessageId.ToString()));
            Assert.That(json["id"]?.                  Value<String>(),  Is.EqualTo("envelope1"));
            Assert.That(json["execution_time"]?.      Value<String>(),  Is.EqualTo("2024-08-24T15:00:00Z"));
            Assert.That(json["abnormal_condition"]?.  Value<Boolean>(), Is.False);
            Assert.That(json["power_constraints_id"]?.Value<String>(),  Is.EqualTo("powerConstraint1"));
            Assert.That(json["power_envelopes"],                        Is.TypeOf<JArray>());

            Assert.That(PEBC_Instruction.TryParse(json, out var parsed, out var error, strict), Is.True, error);
            Assert.That(parsed,                                    Is.EqualTo(instruction));
            Assert.That(parsed!.GetHashCode(),                     Is.EqualTo(instruction.GetHashCode()));
            Assert.That(parsed. ExecutionTime,                     Is.EqualTo(executionTime));
            Assert.That(parsed. AbnormalCondition,                 Is.False);
            Assert.That(parsed. PowerConstraintsId.ToString(),     Is.EqualTo("powerConstraint1"));
            Assert.That(parsed. PowerEnvelopes,                    Has.Count.EqualTo(1));
            Assert.That(parsed. PowerEnvelopes[0].Id.ToString(),   Is.EqualTo("pe_xxx"));

            IInstruction asInstruction = parsed;
            Assert.That(asInstruction.Id.ToString(),                     Is.EqualTo("envelope1"));
            Assert.That(asInstruction.ExecutionTime,                     Is.EqualTo(executionTime));
            Assert.That(asInstruction.AbnormalCondition,                 Is.False);
            Assert.That(asInstruction.RevokableObjectType,               Is.EqualTo(RevokableObject.PEBC_Instruction));
            Assert.That(asInstruction.RevokableObjectId.ToString(),      Is.EqualTo("envelope1"));

            var clone = instruction.Clone();
            Assert.That(clone,                                    Is.EqualTo(instruction));
            Assert.That(ReferenceEquals(clone, instruction),      Is.False);
            Assert.That(instruction.ToString(),                   Does.EndWith($"[{instruction.MessageId}]"));

        }

        [Test]
        public void Instruction_MatchesTheDocumentationExample()
        {

            // The PV example of the S2 documentation (pv.md), verbatim.
            var json = S2JSONExtensions.ParseS2JSON("""
                {
                  "message_type": "PEBC.Instruction",
                  "message_id": "xxx",
                  "id": "envelope1",
                  "execution_time": "2024-08-24T15:00:00Z",
                  "abnormal_condition": false,
                  "power_constraints_id": "powerConstraint1",
                  "power_envelopes": [
                    {
                      "id": "pe_xxx",
                      "commodity_quantity": "ELECTRIC.POWER.L1",
                      "power_envelope_elements": [
                        {
                          "duration": 3600000,
                          "upper_limit": 0,
                          "lower_limit": -2000.0
                        }
                      ]
                    }
                  ]
                }
                """);

            Assert.That(PEBC_Instruction.TryParse(json, out var instruction, out var error), Is.True, error);
            Assert.That(instruction!.MessageId.ToString(),                                            Is.EqualTo("xxx"));
            Assert.That(instruction. Id.ToString(),                                                   Is.EqualTo("envelope1"));
            Assert.That(instruction. ExecutionTime,                                                   Is.EqualTo(executionTime));
            Assert.That(instruction. AbnormalCondition,                                               Is.False);
            Assert.That(instruction. PowerConstraintsId.ToString(),                                   Is.EqualTo("powerConstraint1"));
            Assert.That(instruction. PowerEnvelopes,                                                  Has.Count.EqualTo(1));
            Assert.That(instruction. PowerEnvelopes[0].CommodityQuantity,                             Is.EqualTo(CommodityQuantity.ElectricPowerL1));
            Assert.That(instruction. PowerEnvelopes[0].PowerEnvelopeElements,                         Has.Count.EqualTo(1));
            Assert.That(instruction. PowerEnvelopes[0].PowerEnvelopeElements[0].Duration,             Is.EqualTo(oneHour));
            Assert.That(instruction. PowerEnvelopes[0].PowerEnvelopeElements[0].UpperLimit,           Is.EqualTo(0));
            Assert.That(instruction. PowerEnvelopes[0].PowerEnvelopeElements[0].LowerLimit,           Is.EqualTo(-2000));

            var reserialised = instruction.ToJSON();
            S2SchemaValidator.AssertValidMessage(reserialised, "PEBC.Instruction");
            JSONAssert.AssertDeepEquals(json, reserialised);

        }

        [Test]
        public void Instruction_MandatoryOnly_IsSchemaValid()
        {

            // Every property of PEBC.Instruction is mandatory; a three-phase instruction with one envelope per phase.
            var instruction = new PEBC_Instruction(
                                  Instruction_Id.NewRandom,
                                  executionTime,
                                  true,
                                  PowerConstraints_Id.NewRandom,
                                  [
                                      Envelope("pe_L1", CommodityQuantity.ElectricPowerL1),
                                      Envelope("pe_L2", CommodityQuantity.ElectricPowerL2),
                                      Envelope("pe_L3", CommodityQuantity.ElectricPowerL3)
                                  ]
                              );

            var json = instruction.ToJSON();

            Assert.That(json.Properties().Select(property => property.Name),
                        Is.EqualTo(new[] { "message_type", "message_id", "id", "execution_time", "abnormal_condition",
                                           "power_constraints_id", "power_envelopes" }));

            S2SchemaValidator.AssertValidMessage(json, "PEBC.Instruction");

            Assert.That(PEBC_Instruction.TryParse(json, out var parsed, out var error, strict), Is.True, error);
            Assert.That(parsed,                     Is.EqualTo(instruction));
            Assert.That(parsed!.AbnormalCondition,  Is.True);
            Assert.That(parsed. PowerEnvelopes,     Has.Count.EqualTo(3));

        }

        [Test]
        public void Instruction_MissingMandatoryProperty_Fails()
        {

            var json = new PEBC_Instruction(Instruction_Id.NewRandom,
                                            executionTime,
                                            false,
                                            PowerConstraints_Id.NewRandom,
                                            [ Envelope("pe_xxx") ]).ToJSON();

            json.Remove("power_constraints_id");

            Assert.That(PEBC_Instruction.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("power_constraints_id"));

            json["power_constraints_id"] = "powerConstraint1";
            json.Remove("execution_time");

            Assert.That(PEBC_Instruction.TryParse(json, out _, out error), Is.False);
            Assert.That(error, Does.Contain("execution_time"));

        }

        [Test]
        public void Instruction_Strict_RejectsAdditionalProperties()
        {

            var json = new PEBC_Instruction(Instruction_Id.NewRandom,
                                            executionTime,
                                            false,
                                            PowerConstraints_Id.NewRandom,
                                            [ Envelope("5d2c1b0a-0000-4000-8000-0000000000e1") ]).ToJSON();

            json["foo"] = 42;

            Assert.That(PEBC_Instruction.TryParse(json, out _, out _,         S2ParserOptions.Default), Is.True);
            Assert.That(PEBC_Instruction.TryParse(json, out _, out var error, S2ParserOptions.Strict),  Is.False);
            Assert.That(error, Does.Contain("foo"));

        }

        [Test]
        [S2C("Rules.PEBC.Instruction.OnePowerEnvelopePerCommodityQuantity")]
        public void Instruction_AllowsAtMostOnePowerEnvelopePerCommodityQuantity()
        {

            Assert.That(() => new PEBC_Instruction(Instruction_Id.NewRandom,
                                                   executionTime,
                                                   false,
                                                   PowerConstraints_Id.NewRandom,
                                                   [ Envelope("pe_1"), Envelope("pe_2") ]),
                        Throws.ArgumentException.With.Message.Contains("ELECTRIC.POWER.L1"));

            var json = new PEBC_Instruction(Instruction_Id.NewRandom,
                                            executionTime,
                                            false,
                                            PowerConstraints_Id.NewRandom,
                                            [ Envelope("pe_1"), Envelope("pe_2", CommodityQuantity.ElectricPowerL2) ]).ToJSON();

            json["power_envelopes"]![1]!["commodity_quantity"] = "ELECTRIC.POWER.L1";

            Assert.That(PEBC_Instruction.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("commodity quantity"));

        }

        [Test]
        public void Instruction_RequiresOneToTenPowerEnvelopes()
        {

            Assert.That(() => new PEBC_Instruction(Instruction_Id.NewRandom,
                                                   executionTime,
                                                   false,
                                                   PowerConstraints_Id.NewRandom,
                                                   []),
                        Throws.ArgumentException);

            var elevenEnvelopes = Enumerable.Range(1, 11).
                                      Select(i => Envelope($"pe_{i}", CommodityQuantity.ElectricPowerL1)).
                                      ToList();

            Assert.That(() => new PEBC_Instruction(Instruction_Id.NewRandom,
                                                   executionTime,
                                                   false,
                                                   PowerConstraints_Id.NewRandom,
                                                   elevenEnvelopes),
                        Throws.ArgumentException);

            var json = new PEBC_Instruction(Instruction_Id.NewRandom,
                                            executionTime,
                                            false,
                                            PowerConstraints_Id.NewRandom,
                                            [ Envelope("pe_xxx") ]).ToJSON();

            json["power_envelopes"] = new JArray();

            Assert.That(PEBC_Instruction.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("power_envelopes").And.Contain("at least 1"));

        }

        #endregion

        #region S2MessageParser

        [Test]
        public void MessageParser_DispatchesPowerConstraints()
        {

            var text = new PEBC_PowerConstraints(PowerConstraints_Id.Parse("powerConstraint1"),
                                                 validFrom,
                                                 PEBC_PowerEnvelopeConsequenceType.Vanish,
                                                 [ LowerRange(), UpperRange() ],
                                                 validUntil,
                                                 Message_Id.Parse("fe9f4f07-c731-4f91-8801-3b53a9fecaaf")).ToJSON().ToString();

            Assert.That(S2MessageParser.TryParse(text, null, out var message, out var error), Is.True, error?.DiagnosticLabel);
            Assert.That(message,                                                Is.TypeOf<PEBC_PowerConstraints>());
            Assert.That(((PEBC_PowerConstraints) message!).Id.ToString(),       Is.EqualTo("powerConstraint1"));
            Assert.That(((PEBC_PowerConstraints) message).MessageId.ToString(), Is.EqualTo("fe9f4f07-c731-4f91-8801-3b53a9fecaaf"));

        }

        #endregion

    }

}

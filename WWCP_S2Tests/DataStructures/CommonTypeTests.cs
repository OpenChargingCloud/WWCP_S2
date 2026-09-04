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

namespace cloud.charging.open.protocols.S2.Tests.DataStructures
{

    /// <summary>
    /// Tests of the template types of Phase 1a: identifiers, enumerations, Duration,
    /// NumberRange, Timer and the JSON helpers.
    /// </summary>
    [TestFixture]
    public sealed class CommonTypeTests
    {

        #region Message_Id

        [Test]
        public void MessageId_NewRandom_IsLowerCaseUUID()
        {
            var id = Message_Id.NewRandom;
            Assert.That(id.IsUUID,                              Is.True);
            Assert.That(id.ToString(), Is.EqualTo(id.ToString().ToLowerInvariant()));
            Assert.That(id.ToString(), Has.Length.EqualTo(36));
        }

        [Test]
        public void MessageId_AcceptsSchemaPattern_AndRejectsGarbage()
        {
            Assert.That(Message_Id.TryParse("acme_ev_xxxxxx", out var id1), Is.True);
            Assert.That(id1.IsUUID,                                          Is.False);
            Assert.That(Message_Id.TryParse("a", out _),                     Is.False);
            Assert.That(Message_Id.TryParse("   ", out _),                   Is.False);
            Assert.That(Message_Id.TryParse("", out _),                      Is.False);
            Assert.That(Message_Id.Null.ToString(),                          Is.EqualTo("00000000-0000-0000-0000-000000000000"));
        }

        [Test]
        public void MessageId_ComparesOrdinal_CaseSensitive()
        {
            Assert.That(Message_Id.Parse("abc"), Is.Not.EqualTo(Message_Id.Parse("ABC")));
            Assert.That(Message_Id.Parse("abc"), Is.EqualTo(Message_Id.Parse("abc")));
            Assert.That(Message_Id.Parse("abc").GetHashCode(), Is.EqualTo(Message_Id.Parse("abc").GetHashCode()));
        }

        #endregion

        #region ControlType

        [Test]
        public void ControlType_KnownValues_AreRegistered()
        {
            Assert.That(ControlType.All.Count(),                         Is.EqualTo(7));
            Assert.That(ControlType.NotControllable.ToString(),          Is.EqualTo("NOT_CONTROLABLE"));
            Assert.That(ControlType.Parse("FILL_RATE_BASED_CONTROL"),    Is.EqualTo(ControlType.FillRateBasedControl));
            Assert.That(ControlType.FillRateBasedControl.IsKnown,        Is.True);
        }

        [Test]
        public void ControlType_IsCaseSensitive_AndUnknownValuesAreNotRegistered()
        {

            Assert.That(ControlType.TryParse("fill_rate_based_control", out var lower), Is.True);
            Assert.That(lower.IsKnown,                                                   Is.False);
            Assert.That(lower,                                                           Is.Not.EqualTo(ControlType.FillRateBasedControl));
            Assert.That(ControlType.All.Count(),                                         Is.EqualTo(7));

            Assert.That(ControlType.TryParse("FUTURE_CONTROL", out var future), Is.True);
            Assert.That(future.IsKnown,                                          Is.False);
            Assert.That(future.ToString(),                                       Is.EqualTo("FUTURE_CONTROL"));

        }

        [Test]
        public void ParseMandatoryS2Enum_RejectsUnknownValues_ByDefault()
        {

            var json = JObject.Parse("""{ "control_type": "FUTURE_CONTROL" }""");

            Assert.That(json.ParseMandatoryS2Enum("control_type", "control type", ControlType.TryParse, null, out ControlType _, out var error), Is.False);
            Assert.That(error, Does.Contain("not a known value"));

            var lenient = new S2ParserOptions { RejectUnknownEnumValues = false };
            Assert.That(json.ParseMandatoryS2Enum("control_type", "control type", ControlType.TryParse, lenient, out ControlType value, out _), Is.True);
            Assert.That(value.ToString(), Is.EqualTo("FUTURE_CONTROL"));

        }

        #endregion

        #region Duration

        [Test]
        public void Duration_RoundTrips_AndRejectsNegative()
        {

            var duration = Duration.FromSeconds(3);
            Assert.That(duration.Milliseconds,       Is.EqualTo(3000));
            Assert.That(duration.TimeSpan,           Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(duration.ToJSON().Value,     Is.EqualTo(3000L));

            Assert.That(Duration.TryParse(new JValue(3000),   out var parsed, out _), Is.True);
            Assert.That(parsed,                                                       Is.EqualTo(duration));
            Assert.That(Duration.TryParse(new JValue(3000.0), out parsed,     out _), Is.True);
            Assert.That(parsed,                                                       Is.EqualTo(duration));
            Assert.That(Duration.TryParse(new JValue(-1),     out _,          out _), Is.False);
            Assert.That(Duration.TryParse(new JValue(1.5),    out _,          out _), Is.False);
            Assert.That(Duration.TryParse(new JValue("3000"), out _,          out _), Is.False);

            Assert.That(() => new Duration(-1), Throws.TypeOf<ArgumentOutOfRangeException>());

        }

        #endregion

        #region NumberRange

        [Test]
        public void NumberRange_RoundTrips_AndIsSchemaValid()
        {

            var range = new NumberRange(0, 100);
            var json  = range.ToJSON();

            S2SchemaValidator.AssertValidType(json, "NumberRange");

            Assert.That(NumberRange.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed, Is.EqualTo(range));
            Assert.That(parsed!.Contains(50),  Is.True);
            Assert.That(parsed. Contains(101), Is.False);

        }

        [Test]
        public void NumberRange_RejectsStartGreaterThanEnd_AndAdditionalPropertiesWhenStrict()
        {

            Assert.That(() => new NumberRange(1, 0), Throws.ArgumentException);

            var json = JObject.Parse("""{ "start_of_range": 5, "end_of_range": 1 }""");
            Assert.That(NumberRange.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("must not be greater"));

            var extra = JObject.Parse("""{ "start_of_range": 0, "end_of_range": 1, "foo": 1 }""");
            Assert.That(NumberRange.TryParse(extra, out _, out _, S2ParserOptions.Default), Is.True);
            Assert.That(NumberRange.TryParse(extra, out _, out error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("foo"));

            var missing = JObject.Parse("""{ "start_of_range": 0 }""");
            Assert.That(NumberRange.TryParse(missing, out _, out error), Is.False);
            Assert.That(error, Does.Contain("end_of_range"));

        }

        #endregion

        #region Timer

        [Test]
        public void Timer_RoundTrips_AndIsSchemaValid()
        {

            var timer = new Timer(Timer_Id.Parse("timer0"), Duration.FromMilliseconds(120000), "Minimum on time");
            var json  = timer.ToJSON();

            S2SchemaValidator.AssertValidType(json, "Timer");

            Assert.That(Timer.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed,                  Is.EqualTo(timer));
            Assert.That(parsed!.DiagnosticLabel, Is.EqualTo("Minimum on time"));

            var withoutLabel = new Timer(Timer_Id.NewRandom, Duration.Zero);
            Assert.That(withoutLabel.ToJSON().ContainsKey("diagnostic_label"), Is.False);
            S2SchemaValidator.AssertValidType(withoutLabel.ToJSON(), "Timer");

        }

        [Test]
        public void Timer_RequireUUIDs_RejectsNonUUIDIds()
        {

            var json = new Timer(Timer_Id.Parse("timer0"), Duration.Zero).ToJSON();

            Assert.That(Timer.TryParse(json, out _, out var error, S2ParserOptions.Strict), Is.False);
            Assert.That(error, Does.Contain("not a UUID"));

        }

        #endregion

        #region Timestamps

        [Test]
        public void Timestamps_KeepTheirOffset_AndFormatLikeTheExamples()
        {

            var utc    = new DateTimeOffset(2019, 8, 24, 14, 15, 22, TimeSpan.Zero);
            var cest   = new DateTimeOffset(2019, 8, 24, 16, 15, 22, TimeSpan.FromHours(2));
            var millis = new DateTimeOffset(2019, 8, 24, 14, 15, 22, 500, TimeSpan.Zero);

            Assert.That(utc.   ToS2Timestamp(), Is.EqualTo("2019-08-24T14:15:22Z"));
            Assert.That(cest.  ToS2Timestamp(), Is.EqualTo("2019-08-24T16:15:22+02:00"));
            Assert.That(millis.ToS2Timestamp(), Is.EqualTo("2019-08-24T14:15:22.5Z"));

            var json = S2JSONExtensions.ParseS2JSON("""{ "t": "2019-08-24T16:15:22+02:00", "n": "2019-08-24T14:15:22" }""");

            Assert.That(json.ParseMandatoryS2Timestamp("t", "timestamp", null, out var t, out var error), Is.True, error);
            Assert.That(t.Offset, Is.EqualTo(TimeSpan.FromHours(2)));
            Assert.That(t,        Is.EqualTo(utc));

            Assert.That(json.ParseMandatoryS2Timestamp("n", "timestamp", null,                    out var n, out _),     Is.True);
            Assert.That(n,                                                                                                Is.EqualTo(utc));
            Assert.That(json.ParseMandatoryS2Timestamp("n", "timestamp", S2ParserOptions.Strict, out _,     out error), Is.False);
            Assert.That(error, Does.Contain("no UTC offset"));

        }

        [Test]
        public void Timestamps_AcceptOnlyRFC3339_AndLowerCaseDesignators()
        {

            var expected = new DateTimeOffset(2019, 8, 24, 14, 15, 22, TimeSpan.Zero);

            Assert.That(S2JSONExtensions.TryParseS2Timestamp("2019-08-24t14:15:22z",        null, out var t1, out _), Is.True);
            Assert.That(t1,                                                                                                Is.EqualTo(expected));
            Assert.That(S2JSONExtensions.TryParseS2Timestamp("2019-08-24T14:15:22.123456Z", null, out var t2, out _), Is.True);
            Assert.That(t2.Millisecond,                                                                                    Is.EqualTo(123));

            // Not RFC 3339: time only, date only, RFC 1123, textual month, ISO week.
            Assert.That(S2JSONExtensions.TryParseS2Timestamp("14:15:22",                       null, out _, out var error), Is.False, error);
            Assert.That(S2JSONExtensions.TryParseS2Timestamp("2019-08-24",                     null, out _, out _),         Is.False);
            Assert.That(S2JSONExtensions.TryParseS2Timestamp("Sat, 24 Aug 2019 14:15:22 GMT", null, out _, out _),         Is.False);
            Assert.That(S2JSONExtensions.TryParseS2Timestamp("24 August 2019 14:15",          null, out _, out _),         Is.False);

        }

        [Test]
        public void Numbers_MustBeFinite()
        {

            var json = JObject.Parse("""{ "nan": NaN, "inf": Infinity, "ok": 1.5 }""");

            Assert.That(json.ParseMandatoryS2Number("nan", "value", out _, out var error), Is.False);
            Assert.That(error, Does.Contain("finite"));
            Assert.That(json.ParseMandatoryS2Number("inf", "value", out _, out _),      Is.False);
            Assert.That(json.ParseMandatoryS2Number("ok",  "value", out var ok, out _), Is.True);
            Assert.That(ok, Is.EqualTo(1.5));

            Assert.That(() => new NumberRange(Double.NegativeInfinity, 0), Throws.ArgumentException);

        }

        [Test]
        public void Duration_RejectsValuesBeyondTimeSpan_AndHugeIntegers()
        {

            Assert.That(() => new Duration(Duration.MaxMilliseconds + 1), Throws.TypeOf<ArgumentOutOfRangeException>());

            var json = JObject.Parse("""{ "huge": 99999999999999999999999, "big": 9223372036854775807 }""");

            Assert.That(Duration.TryParse(json["huge"]!, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("out of range"));
            Assert.That(Duration.TryParse(json["big"]!,  out _, out error),     Is.False);
            Assert.That(error, Does.Contain("must not exceed"));

        }

        [Test]
        public void IdentifiersAndEnumerations_KeepTheWireValueVerbatim()
        {

            // Padded values are not trimmed: an echoed subject_message_id must equal the peer's message_id.
            Assert.That(Message_Id.TryParse(" abc ", out var padded), Is.True);
            Assert.That(padded.ToString(),                             Is.EqualTo(" abc "));

            Assert.That(ControlType.TryParse(" FILL_RATE_BASED_CONTROL", out var paddedEnum), Is.True);
            Assert.That(paddedEnum.IsKnown,                                                   Is.False);

        }

        #endregion

    }

}

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

namespace cloud.charging.open.protocols.S2.Tests
{

    /// <summary>
    /// JSON comparison helpers shared by the message tests.
    ///
    /// Newtonsoft's JToken.DeepEquals compares token types before values: the documentation
    /// examples write integral numbers as JSON integers ("operation_mode_factor": 1) while the
    /// S2 serializers write every JSON "number" as a Double (1.0), and JObject.Parse turns RFC 3339
    /// strings ("2019-08-24T14:15:22Z") into Date tokens while ToJSON() writes them as strings.
    /// Both are the same JSON value, so the comparison normalises them before comparing.
    /// </summary>
    public static class JSONAssert
    {

        #region Normalise(Token)

        /// <summary>
        /// Return a deep copy of the given JSON in which every number is a Double and every
        /// Date token is its S2 timestamp string, so that DeepEquals compares by value.
        /// </summary>
        /// <param name="Token">A JSON token.</param>
        public static JToken Normalise(JToken Token)
        {

            switch (Token)
            {

                case JObject jsonObject:
                    var normalisedObject = new JObject();
                    foreach (var property in jsonObject.Properties())
                        normalisedObject.Add(property.Name, Normalise(property.Value));
                    return normalisedObject;

                case JArray jsonArray:
                    return new JArray(jsonArray.Select(Normalise));

                case JValue { Type: JTokenType.Integer } integer:
                    return new JValue(integer.Value<Double>());

                case JValue { Type: JTokenType.Date, Value: DateTimeOffset dateTimeOffset }:
                    return new JValue(dateTimeOffset.ToS2Timestamp());

                case JValue { Type: JTokenType.Date, Value: DateTime dateTime }:
                    return new JValue(dateTime.Kind == DateTimeKind.Unspecified
                                          ? new DateTimeOffset(dateTime, TimeSpan.Zero).ToS2Timestamp()
                                          : new DateTimeOffset(dateTime.ToUniversalTime()).ToS2Timestamp());

                default:
                    return Token.DeepClone();

            }

        }

        #endregion

        #region AssertDeepEquals(Expected, Actual)

        /// <summary>
        /// Assert that both JSON objects are equal by value (see <see cref="Normalise(JToken)"/>).
        /// </summary>
        /// <param name="Expected">The expected JSON, e.g. a documentation example.</param>
        /// <param name="Actual">The actual JSON, e.g. a re-serialised message.</param>
        public static void AssertDeepEquals(JObject Expected, JObject Actual)
        {

            var normalisedExpected  = Normalise(Expected);
            var normalisedActual    = Normalise(Actual);

            Assert.That(JToken.DeepEquals(normalisedExpected, normalisedActual),
                        Is.True,
                        "Expected:\n" + normalisedExpected.ToString() + "\n\nActual:\n" + normalisedActual.ToString());

        }

        #endregion

    }

}

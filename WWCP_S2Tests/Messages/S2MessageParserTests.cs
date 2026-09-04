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

namespace cloud.charging.open.protocols.S2.Tests.Messages
{

    /// <summary>
    /// Tests that the message parser knows exactly the 36 message types of the
    /// embedded S2 JSON v1.0.0 message schemas.
    /// </summary>
    [TestFixture]
    public sealed class S2MessageParserTests
    {

        #region (private static) MessageTypesOfTheEmbeddedSchemas()

        /// <summary>
        /// The "message_type" constants of all embedded message schemas.
        /// </summary>
        private static IReadOnlyList<String> MessageTypesOfTheEmbeddedSchemas()

            => EmbeddedSchemas.MessageSchemaNames.
                   Select(name => JObject.Parse(EmbeddedSchemas.Read(name))).
                   Select(schema => schema["properties"]?["message_type"]?["const"]?.Value<String>()
                                        ?? throw new InvalidOperationException("A message schema without a message_type const!")).
                   Order(StringComparer.Ordinal).
                   ToList();

        #endregion


        #region MessageParser_KnowsAllMessageTypesOfTheEmbeddedSchemas()

        [Test]
        public void MessageParser_KnowsAllMessageTypesOfTheEmbeddedSchemas()
        {

            var expected  = MessageTypesOfTheEmbeddedSchemas();
            var known     = S2MessageParser.KnownMessageTypes.Order(StringComparer.Ordinal).ToList();

            Assert.That(expected.Count, Is.EqualTo(36));
            Assert.That(known,          Is.EqualTo(expected));

        }

        #endregion

        #region MessageParser_MessageTypeNamesMatchTheSchemaFileNames()

        [Test]
        public void MessageParser_MessageTypeNamesMatchTheSchemaFileNames()
        {

            foreach (var messageType in S2MessageParser.KnownMessageTypes)
            {

                var schema = JObject.Parse(EmbeddedSchemas.ReadS2JSONMessageSchema(messageType));

                Assert.That(schema["properties"]?["message_type"]?["const"]?.Value<String>(),
                            Is.EqualTo(messageType),
                            $"The schema file of '{messageType}' declares a different message_type const!");

            }

        }

        #endregion

    }

}

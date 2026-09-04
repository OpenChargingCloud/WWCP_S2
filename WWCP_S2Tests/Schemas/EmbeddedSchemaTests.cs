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

namespace cloud.charging.open.protocols.S2.Tests.Schemas
{

    /// <summary>
    /// The embedded normative files: 36 S2 JSON message schemas, 41 S2 JSON type schemas
    /// and 4 S2 Connect OpenAPI files (PLAN.md, Phase 0.3).
    /// </summary>
    [TestFixture]
    public sealed class EmbeddedSchemaTests
    {

        [Test]
        public void AllS2JSONMessageSchemas_AreEmbedded_AndParse()
        {

            var names = EmbeddedSchemas.MessageSchemaNames.ToList();

            Assert.That(names, Has.Count.EqualTo(36));

            foreach (var name in names)
            {
                var json = JObject.Parse(EmbeddedSchemas.Read(name));
                Assert.That(json["properties"]?["message_type"]?["const"]?.Value<String>(), Is.Not.Null.And.Not.Empty, name);
                Assert.That(json["$id"]?.Value<String>(), Does.Contain("/v1.0.0/messages/"), name);
            }

        }

        [Test]
        public void AllS2JSONTypeSchemas_AreEmbedded_AndParse()
        {

            var names = EmbeddedSchemas.TypeSchemaNames.ToList();

            Assert.That(names, Has.Count.EqualTo(41));

            foreach (var name in names)
            {
                var json = JObject.Parse(EmbeddedSchemas.Read(name));
                Assert.That(json["$id"]?.Value<String>(), Does.Contain("/v1.0.0/schemas/"), name);
            }

        }

        [Test]
        public void AllS2ConnectOpenAPIFiles_AreEmbedded()
        {

            var names = EmbeddedSchemas.OpenAPIFileNames.ToList();

            Assert.That(names, Has.Count.EqualTo(4));

            foreach (var name in names)
                Assert.That(EmbeddedSchemas.Read(name), Does.StartWith("openapi: 3.0.3"), name);

        }

        [Test]
        public void MessageTypeConstants_MatchTheSchemaFileNames()
        {

            foreach (var name in EmbeddedSchemas.MessageSchemaNames)
            {

                var json         = JObject.Parse(EmbeddedSchemas.Read(name));
                var messageType  = json["properties"]?["message_type"]?["const"]?.Value<String>();

                // "….messages.FRBC.SystemDescription.schema.json" => "FRBC.SystemDescription"
                var fileName     = name[(name.IndexOf(".messages.", StringComparison.Ordinal) + ".messages.".Length)..];
                var expected     = fileName[..^".schema.json".Length];

                Assert.That(messageType, Is.EqualTo(expected), name);

            }

        }

    }

}

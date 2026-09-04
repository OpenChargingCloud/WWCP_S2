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

using System.Text.Json.Nodes;

using Json.Schema;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.S2.Tests
{

    /// <summary>
    /// Validates serialised S2 data structures and messages against the embedded
    /// s2-json v1.0.0 schemas (JSON Schema draft 2020-12). All 77 schemas are registered
    /// by their "$id", so the relative "../schemas/…" references resolve offline.
    /// </summary>
    public static class S2SchemaValidator
    {

        #region Data

        private const String IdPrefix = "https://raw.githubusercontent.com/flexiblepower/s2-ws-json/v1.0.0/";

        private static readonly Lazy<SchemaRegistry> registry = new (() => {

            var reg = SchemaRegistry.Global;

            foreach (var name in EmbeddedSchemas.MessageSchemaNames.Concat(EmbeddedSchemas.TypeSchemaNames))
            {
                var schema = JsonSchema.FromText(EmbeddedSchemas.Read(name));
                reg.Register(schema);
            }

            return reg;

        });

        private static readonly EvaluationOptions evaluationOptions = new () {
                                                                          OutputFormat         = OutputFormat.List,
                                                                          RequireFormatValidation = true
                                                                      };

        #endregion


        #region Validate(JSON, MessageType | TypeName)

        /// <summary>
        /// Validate the given JSON against the message schema of the given message type,
        /// e.g. "FRBC.SystemDescription".
        /// </summary>
        public static EvaluationResults ValidateMessage(JObject JSON, String MessageType)
            => Validate(JSON, IdPrefix + "messages/" + MessageType + ".schema.json");

        /// <summary>
        /// Validate the given JSON against the type schema of the given type,
        /// e.g. "PowerRange" or "FRBC.OperationMode".
        /// </summary>
        public static EvaluationResults ValidateType(JObject JSON, String TypeName)
            => Validate(JSON, IdPrefix + "schemas/" + TypeName + ".schema.json");

        private static EvaluationResults Validate(JObject JSON, String SchemaId)
        {

            var schema = registry.Value.Get(new Uri(SchemaId)) as JsonSchema
                             ?? throw new InvalidOperationException($"Schema '{SchemaId}' is not registered!");

            var node   = JsonNode.Parse(JSON.ToString(Newtonsoft.Json.Formatting.None));

            return schema.Evaluate(node, evaluationOptions);

        }

        #endregion

        #region AssertValidMessage / AssertValidType

        /// <summary>
        /// Assert that the given JSON is valid against the message schema of the given message type.
        /// </summary>
        public static void AssertValidMessage(JObject JSON, String MessageType)
            => AssertValid(ValidateMessage(JSON, MessageType), JSON);

        /// <summary>
        /// Assert that the given JSON is valid against the type schema of the given type.
        /// </summary>
        public static void AssertValidType(JObject JSON, String TypeName)
            => AssertValid(ValidateType(JSON, TypeName), JSON);

        private static void AssertValid(EvaluationResults Results, JObject JSON)
        {

            if (Results.IsValid)
                return;

            var errors = Results.Details.
                             Where (d => d.HasErrors).
                             Select(d => $"{d.InstanceLocation}: {String.Join("; ", d.Errors!.Select(e => e.Key + " " + e.Value))}");

            Assert.Fail("Schema validation failed:\n" + String.Join("\n", errors) + "\n\nJSON:\n" + JSON.ToString());

        }

        #endregion

    }

}

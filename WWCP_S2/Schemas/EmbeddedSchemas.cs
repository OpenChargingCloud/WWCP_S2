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

using System.Reflection;

#endregion

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// Access to the embedded normative files: the S2 JSON v1.0.0 schemas
    /// (Apache-2.0, flexiblepower/s2-json) and the S2 Connect v1.0 OpenAPI files
    /// (Apache-2.0, flexiblepower/s2-connect). See THIRD-PARTY-NOTICES.md.
    /// </summary>
    public static class EmbeddedSchemas
    {

        #region Data

        private const String ResourcePrefix         = "cloud.charging.open.protocols.S2.Schemas.";
        private const String S2JSONMessagesPrefix   = ResourcePrefix + "S2JSON.v1._0._0.messages.";
        private const String S2JSONSchemasPrefix    = ResourcePrefix + "S2JSON.v1._0._0.schemas.";
        private const String S2ConnectPrefix        = ResourcePrefix + "S2Connect.v1._0.";

        private static readonly Assembly assembly = typeof(EmbeddedSchemas).Assembly;

        #endregion

        #region Properties

        /// <summary>
        /// The resource names of all embedded files.
        /// </summary>
        public static IEnumerable<String> AllResourceNames
            => assembly.GetManifestResourceNames().
                        Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal)).
                        Order(StringComparer.Ordinal);

        /// <summary>
        /// The resource names of the 36 S2 JSON message schemas (e.g. "…messages.FRBC.SystemDescription.schema.json").
        /// </summary>
        public static IEnumerable<String> MessageSchemaNames
            => AllResourceNames.Where(name => name.StartsWith(S2JSONMessagesPrefix, StringComparison.Ordinal));

        /// <summary>
        /// The resource names of the 41 S2 JSON type schemas (e.g. "…schemas.PowerRange.schema.json").
        /// </summary>
        public static IEnumerable<String> TypeSchemaNames
            => AllResourceNames.Where(name => name.StartsWith(S2JSONSchemasPrefix, StringComparison.Ordinal));

        /// <summary>
        /// The resource names of the 4 S2 Connect OpenAPI files.
        /// </summary>
        public static IEnumerable<String> OpenAPIFileNames
            => AllResourceNames.Where(name => name.StartsWith(S2ConnectPrefix, StringComparison.Ordinal) &&
                                              name.EndsWith  (".yml",          StringComparison.Ordinal));

        #endregion

        #region Read(ResourceName)

        /// <summary>
        /// Read the embedded file with the given resource name as UTF-8 text.
        /// </summary>
        /// <param name="ResourceName">A resource name as returned by the name enumerations.</param>
        public static String Read(String ResourceName)
        {

            using var stream  = assembly.GetManifestResourceStream(ResourceName)
                                    ?? throw new ArgumentException($"Unknown embedded resource '{ResourceName}'!", nameof(ResourceName));

            using var reader  = new StreamReader(stream, System.Text.Encoding.UTF8);

            return reader.ReadToEnd();

        }

        #endregion

        #region ReadS2JSONMessageSchema(MessageType)

        /// <summary>
        /// Read the S2 JSON v1.0.0 schema of the given message type, e.g. "FRBC.SystemDescription".
        /// </summary>
        /// <param name="MessageType">A message type as used in the "message_type" property.</param>
        public static String ReadS2JSONMessageSchema(String MessageType)
            => Read(S2JSONMessagesPrefix + MessageType + ".schema.json");

        #endregion

        #region ReadS2JSONTypeSchema(TypeName)

        /// <summary>
        /// Read the S2 JSON v1.0.0 schema of the given type, e.g. "PowerRange" or "FRBC.OperationMode".
        /// </summary>
        /// <param name="TypeName">A schema file name without the ".schema.json" suffix.</param>
        public static String ReadS2JSONTypeSchema(String TypeName)
            => Read(S2JSONSchemasPrefix + TypeName + ".schema.json");

        #endregion

    }

}

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

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// Options controlling how strictly S2 JSON is parsed. There is no process-global
    /// strictness switch: the options are passed explicitly into every TryParse method
    /// and are held per session (PLAN.md §3.7).
    /// </summary>
    public sealed record S2ParserOptions
    {

        #region Properties

        /// <summary>
        /// The S2 JSON version the JSON is expected to conform to, e.g. "v1.0.0".
        /// </summary>
        public String   Version                     { get; init; } = S2.Version.S2JSONVersion;

        /// <summary>
        /// Whether an enumeration value that is not defined by the S2 JSON schemas
        /// is a parse error. The v1.0.0 enumerations are closed, so this defaults to true.
        /// </summary>
        public Boolean  RejectUnknownEnumValues     { get; init; } = true;

        /// <summary>
        /// Whether a property that is not defined by the S2 JSON schemas is a parse error
        /// ("additionalProperties": false in every schema). Defaults to false to be liberal
        /// in what is accepted; the strict options and the schema-validation tests enable it.
        /// </summary>
        public Boolean  RejectAdditionalProperties  { get; init; }

        /// <summary>
        /// Whether every identifier must be a UUID. The ID schema only requires the pattern
        /// [a-zA-Z0-9\-_:]{2,64}, but s2-python rejects non-UUID identifiers; enable this for
        /// strict interoperability testing.
        /// </summary>
        public Boolean  RequireUUIDs                { get; init; }

        /// <summary>
        /// Whether a date-time value without an explicit UTC offset (or "Z") is a parse error.
        /// RFC 3339 requires the offset and s2-python rejects naive timestamps.
        /// </summary>
        public Boolean  RejectNaiveTimestamps       { get; init; }

        /// <summary>
        /// Whether S2 Connect URLs (pairing URL, initiateSessionUrl, websocketUrl) may use the
        /// insecure schemes "http://" and "ws://". S2 Connect requires TLS everywhere; this is
        /// for tests and development only and defaults to false.
        /// </summary>
        public Boolean  AllowInsecureURLs           { get; init; }

        #endregion

        #region Static instances

        /// <summary>
        /// The default options: closed enumerations, additional properties tolerated,
        /// identifiers validated against the schema pattern, naive timestamps tolerated.
        /// </summary>
        public static S2ParserOptions Default    { get; } = new ();

        /// <summary>
        /// Strict options: everything the schemas forbid is rejected, identifiers must be
        /// UUIDs and timestamps must carry an offset. Used by the schema-validation tests
        /// and recommended for interoperability testing.
        /// </summary>
        public static S2ParserOptions Strict     { get; } = new () {
                                                                RejectUnknownEnumValues     = true,
                                                                RejectAdditionalProperties  = true,
                                                                RequireUUIDs                = true,
                                                                RejectNaiveTimestamps       = true
                                                            };

        #endregion

    }

}

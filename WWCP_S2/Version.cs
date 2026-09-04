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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// The versions implemented by this library.
    /// </summary>
    /// <remarks>
    /// The library version is independent of the S2 JSON and S2 Connect versions
    /// (S2 Connect 1.0.0, section "Version": "S2 Connect is not directly linked to the
    /// version of S2 JSON. The exact version of S2 JSON that is being used by the CEM
    /// and RM is negotiated during session initiation.").
    /// </remarks>
    public static class Version
    {

        /// <summary>
        /// The version of this library as text.
        /// </summary>
        public const            String                 LibraryVersionString     = "0.1.0";

        /// <summary>
        /// The version of this library.
        /// </summary>
        public static readonly  Version_Id             LibraryVersion           = Version_Id.Parse(LibraryVersionString);


        /// <summary>
        /// The normative S2 JSON schema version implemented by this library, as it is
        /// negotiated in S2 Connect session initiation and in the plain-mode Handshake.
        /// </summary>
        public const            String                 S2JSONVersion            = "v1.0.0";

        /// <summary>
        /// The pre-release S2 JSON version string still announced by s2-python, s2-analyzer
        /// and the examples of the S2 documentation. It is treated as a legacy alias of the
        /// v1.0.0 message set (see PLAN.md, D10).
        /// </summary>
        public const            String                 S2JSONLegacyVersion      = "0.0.2-beta";

        /// <summary>
        /// All S2 JSON versions supported by default, most preferred first.
        /// </summary>
        public static readonly  IReadOnlyList<String>  S2JSONVersions           = [ S2JSONVersion, S2JSONLegacyVersion ];


        /// <summary>
        /// The S2 Connect REST API major version implemented by this library, as it is
        /// embedded in the URL paths (e.g. "/v1/requestPairing") and returned by the
        /// version index of every pairing and session initiation API.
        /// </summary>
        public const            String                 S2ConnectAPIVersion      = "v1";

        /// <summary>
        /// All S2 Connect REST API major versions supported by default, most preferred first.
        /// </summary>
        public static readonly  IReadOnlyList<String>  S2ConnectAPIVersions     = [ S2ConnectAPIVersion ];

        /// <summary>
        /// The exact version of the S2 Connect OpenAPI files this library was written against.
        /// </summary>
        public const            String                 S2ConnectOpenAPIVersion  = "v1.0";

    }

}

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
    /// The common interface of all S2 enumerations. S2 JSON enumerations are modelled as
    /// extensible "predefined string" structs: every value defined by the schema is
    /// registered and known, but a value from a newer schema version can still be parsed
    /// and carried through; whether such an unknown value is accepted is decided by
    /// <see cref="S2ParserOptions.RejectUnknownEnumValues"/>.
    /// </summary>
    public interface IS2PredefinedString
    {

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        Boolean  IsKnown    { get; }

        /// <summary>
        /// The exact text of this value as used on the wire.
        /// </summary>
        String   ToString();

    }

}

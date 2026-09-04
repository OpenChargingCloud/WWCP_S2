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

using cloud.charging.open.protocols.S2;

#endregion

namespace cloud.charging.open.protocols.S2.Samples
{

    /// <summary>
    /// The WWCP S2 sample applications (EV charger RM, PV RM, minimal CEM, pairing tool).
    /// The individual samples are added in Phase 10b of PLAN.md; until then this
    /// program only prints the library versions.
    /// </summary>
    public static class Program
    {

        /// <summary>
        /// Application entry point.
        /// </summary>
        public static void Main()
        {
            Console.WriteLine($"WWCP S2 samples, library {Version.LibraryVersion}, " +
                              $"S2 JSON {String.Join(", ", Version.S2JSONVersions)}, " +
                              $"S2 Connect API {String.Join(", ", Version.S2ConnectAPIVersions)}");
        }

    }

}

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

namespace cloud.charging.open.protocols.S2.Tests
{

    /// <summary>
    /// The NUnit test categories of this test project (PLAN.md §8). Everything without
    /// a category is a unit or integration test that runs on every push.
    /// </summary>
    public static class TestCategories
    {

        /// <summary>
        /// Real-clock behaviour of Hermod (pings, HTTP timeouts, token buckets); nightly only.
        /// </summary>
        public const String Timing     = "Timing";

        /// <summary>
        /// DNS-SD / mDNS tests that need multicast on the host network; nightly only.
        /// </summary>
        public const String Multicast  = "Multicast";

        /// <summary>
        /// Interoperability tests against s2-python / s2-rust in docker; nightly only.
        /// </summary>
        public const String Interop    = "Interop";

    }


    /// <summary>
    /// Conformance traceability: links a test to a normative table row or rule of the
    /// S2 Connect specification or of PLAN.md §3.3, e.g. [S2C("Pairing.7A")] or
    /// [S2C("Rules.PEBC.PowerConstraints.LimitRanges")]. CONFORMANCE.md is generated from
    /// these properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class S2CAttribute(String Reference) : PropertyAttribute("S2C", Reference)
    {

        /// <summary>
        /// The specification reference.
        /// </summary>
        public String Reference { get; } = Reference;

    }

}

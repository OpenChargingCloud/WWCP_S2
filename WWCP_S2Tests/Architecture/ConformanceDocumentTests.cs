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
using System.Text;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Architecture
{

    /// <summary>
    /// CONFORMANCE.md (PLAN.md §8): the traceability from the normative rules of S2 Connect and
    /// of PLAN.md §3.3 to the tests that exercise them, generated from the <c>[S2C]</c> properties
    /// the tests carry and compared with the checked-in document on every run - so a rule that
    /// loses its last test, or a test that gains a reference, shows up as a diff instead of
    /// quietly changing what the document claims.
    ///
    /// To accept a change, regenerate it and review the diff:
    /// <code>
    ///   S2_UPDATE_CONFORMANCE=1 dotnet test WWCP_S2Tests/WWCP_S2Tests.csproj --filter "FullyQualifiedName~ConformanceDocumentTests"
    /// </code>
    /// (PowerShell: <c>$env:S2_UPDATE_CONFORMANCE = "1"</c> before the run.)
    /// </summary>
    [TestFixture]
    public sealed class ConformanceDocumentTests
    {

        #region Data

        private const String  DocumentResource     = "cloud.charging.open.protocols.S2.Tests.CONFORMANCE.md";
        private const String  DocumentFile         = "CONFORMANCE.md";
        private const String  UpdateEnvironment    = "S2_UPDATE_CONFORMANCE";
        private const String  TestNamespacePrefix  = "cloud.charging.open.protocols.S2.Tests.";

        #endregion


        #region ConformanceDocument_MatchesTheTests()

        [Test]
        public void ConformanceDocument_MatchesTheTests()

            => GeneratedDocument.CompareOrUpdate(
                   Render(References()),
                   DocumentResource,
                   DocumentFile,
                   UpdateEnvironment,
                   "The conformance traceability"
               );

        #endregion

        #region EveryReference_IsAreaAndRule()

        /// <summary>
        /// A reference is "&lt;area&gt;.&lt;rule&gt;" - the area groups the document, and a
        /// reference without one would silently become an area of its own.
        /// </summary>
        [Test]
        public void EveryReference_IsAreaAndRule()
        {

            var malformed = References().
                                Select (reference => reference.Reference).
                                Distinct().
                                Where  (reference => !reference.Contains('.') ||
                                                      reference.StartsWith('.') ||
                                                      reference.EndsWith('.')).
                                Order  (StringComparer.Ordinal).
                                ToArray();

            Assert.That(malformed, Is.Empty, () => "Malformed [S2C] references: " + String.Join(", ", malformed));

        }

        #endregion

        #region TheTestsAreTagged()

        /// <summary>
        /// A document generated from nothing would be green forever, so the generator is guarded:
        /// the suite is expected to carry a substantial number of references.
        /// </summary>
        [Test]
        public void TheTestsAreTagged()
        {

            var references = References().ToArray();

            Assert.Multiple(() => {
                Assert.That(references.Length,                                    Is.GreaterThan(400));
                Assert.That(references.Select(reference => reference.Reference).
                                       Distinct().Count(),                        Is.GreaterThan(150));
                Assert.That(references.Select(Area).Distinct(),                   Does.Contain("Pairing"));
            });

        }

        #endregion


        #region (private static) References()

        /// <summary>
        /// Every [S2C] property of this test assembly: the reference it names and the test that
        /// carries it. A property on a fixture covers the fixture as a whole.
        /// </summary>
        private static IEnumerable<(String Reference, String Test)> References()
        {

            foreach (var type in typeof(ConformanceDocumentTests).Assembly.GetTypes())
            {

                var name = type.FullName?.StartsWith(TestNamespacePrefix, StringComparison.Ordinal) == true
                               ? type.FullName[TestNamespacePrefix.Length..]
                               : type.FullName ?? type.Name;

                foreach (var attribute in type.GetCustomAttributes<S2CAttribute>(inherit: false))
                    yield return (attribute.Reference, $"{name} (entire fixture)");

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    foreach (var attribute in method.GetCustomAttributes<S2CAttribute>(inherit: false))
                        yield return (attribute.Reference, $"{name}.{method.Name}");

            }

        }

        #endregion

        #region (private static) Area(Reference)

        /// <summary>
        /// The part of a reference before its first dot, e.g. "Pairing" of "Pairing.8A".
        /// </summary>
        private static String Area((String Reference, String Test) Reference)
        {

            var dot = Reference.Reference.IndexOf('.');

            return dot > 0
                       ? Reference.Reference[..dot]
                       : Reference.Reference;

        }

        #endregion

        #region (private static) Render(References)

        /// <summary>
        /// The traceability document: a summary of the areas, then one table per area listing
        /// every rule with the tests that exercise it.
        /// </summary>
        private static String Render(IEnumerable<(String Reference, String Test)> References)
        {

            var rows       = References.Distinct().
                                        OrderBy(row => Area(row),       StringComparer.Ordinal).
                                        ThenBy (row => row.Reference,   StringComparer.Ordinal).
                                        ThenBy (row => row.Test,        StringComparer.Ordinal).
                                        ToArray();

            var areas      = rows.GroupBy(Area, StringComparer.Ordinal).
                                  OrderBy (area => area.Key, StringComparer.Ordinal).
                                  ToArray();

            var document   = new StringBuilder();

            document.AppendLine("# S2 conformance traceability");
            document.AppendLine();
            document.AppendLine("<!-- Generated by WWCP_S2Tests/Architecture/ConformanceDocumentTests.cs - do not edit by hand:");
            document.AppendLine("       S2_UPDATE_CONFORMANCE=1 dotnet test WWCP_S2Tests/WWCP_S2Tests.csproj --filter \"FullyQualifiedName~ConformanceDocumentTests\"");
            document.AppendLine("     A test states the rule it exercises with [S2C(\"<area>.<rule>\")]; this file is the index of those statements. -->");
            document.AppendLine();
            document.AppendLine("Every rule below is a normative statement of the");
            document.AppendLine("[S2 Connect 1.0.0 specification](https://docs.s2standard.org/s2-connect/1.0.0/), of its OpenAPI");
            document.AppendLine("files, or of the S2 JSON message rules collected in [PLAN.md](PLAN.md) §3.3, and every row names a");
            document.AppendLine("test that exercises it. The reference is written into the test itself as `[S2C(\"Pairing.8A\")]`, so");
            document.AppendLine("the two cannot drift apart unnoticed: this document is regenerated from those properties and");
            document.AppendLine("compared on every test run.");
            document.AppendLine();
            document.AppendLine("What this shows is which rules the implementation *asserts*, and where to look when one of them is");
            document.AppendLine("in question. What it does not show: that the rules are complete (a rule nobody tagged is invisible");
            document.AppendLine("here), nor that the implementation interoperates with anyone else's - that is measured against the");
            document.AppendLine("reference implementations in");
            document.AppendLine("[S2ConformanceTests](https://github.com/OpenChargingCloud/S2ConformanceTests).");
            document.AppendLine();

            document.AppendLine("## Areas");
            document.AppendLine();
            document.AppendLine("| Area | Rules | Tests |");
            document.AppendLine("|---|---:|---:|");

            foreach (var area in areas)
                document.Append("| [").Append(area.Key).Append("](#").Append(area.Key.ToLowerInvariant()).Append(") | ").
                         Append(area.Select(row => row.Reference).Distinct().Count()).Append(" | ").
                         Append(area.Count()).AppendLine(" |");

            document.Append("| **Total** | **").
                     Append(rows.Select(row => row.Reference).Distinct().Count()).Append("** | **").
                     Append(rows.Length).AppendLine("** |");

            foreach (var area in areas)
            {

                document.AppendLine();
                document.Append("## ").AppendLine(area.Key);
                document.AppendLine();
                document.AppendLine("| Rule | Test |");
                document.AppendLine("|---|---|");

                foreach (var row in area)
                    document.Append("| `").Append(row.Reference).Append("` | `").Append(row.Test).AppendLine("` |");

            }

            return document.ToString();

        }

        #endregion

    }

}

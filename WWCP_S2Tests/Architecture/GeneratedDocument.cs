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

using System.Text;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Architecture
{

    /// <summary>
    /// The mechanics shared by the files this test suite generates and then guards: the public
    /// API baseline (<see cref="PublicAPITests"/>) and the conformance traceability document
    /// (<see cref="ConformanceDocumentTests"/>).
    ///
    /// Both work the same way: the test renders the current state, compares it with the file
    /// checked into the working tree - embedded into this assembly, so the comparison also works
    /// when the tests run from a copied output directory - and fails with a diff when they differ.
    /// Setting the named environment variable to "1" writes the file back instead, which is how a
    /// deliberate change is accepted: regenerate, read the diff, commit it with the change that
    /// caused it.
    /// </summary>
    internal static class GeneratedDocument
    {

        #region CompareOrUpdate(Current, ResourceName, RelativePath, EnvironmentVariable, Description)

        /// <summary>
        /// Compare the rendered document with the checked-in one, or write the latter when the
        /// environment variable is set.
        /// </summary>
        /// <param name="Current">The freshly rendered document.</param>
        /// <param name="ResourceName">The name of the embedded copy of the checked-in document.</param>
        /// <param name="RelativePath">The path of the document within the working tree, relative to the repository root.</param>
        /// <param name="EnvironmentVariable">The environment variable that turns a comparison into an update.</param>
        /// <param name="Description">What the document is, for the failure message.</param>
        public static void CompareOrUpdate(String  Current,
                                           String  ResourceName,
                                           String  RelativePath,
                                           String  EnvironmentVariable,
                                           String  Description)
        {

            var current = Normalize(Current);

            if (Environment.GetEnvironmentVariable(EnvironmentVariable) is String flag &&
                (flag == "1" || flag.Equals("true", StringComparison.OrdinalIgnoreCase)))
            {

                var sourceOf = SourcePath(RelativePath);

                if (sourceOf is null)
                    Assert.Fail($"'{EnvironmentVariable}' is set, but '{RelativePath}' could not be located from '{TestContext.CurrentContext.TestDirectory}'.");

                else
                {
                    File.WriteAllText(sourceOf, current + Environment.NewLine, new UTF8Encoding(false));
                    Assert.Warn($"{Description} was regenerated: '{sourceOf}'. Review the diff before committing it.");
                }

                return;

            }

            var baseline = Read(ResourceName);

            if (baseline == current)
                return;

            var actualOf = Path.Combine(TestContext.CurrentContext.WorkDirectory,
                                        Path.GetFileNameWithoutExtension(RelativePath) + ".actual" + Path.GetExtension(RelativePath));

            File.WriteAllText(actualOf, current, new UTF8Encoding(false));

            Assert.Fail(
                $"{Description} differs from '{RelativePath}'.{Environment.NewLine}{Environment.NewLine}" +
                $"{Diff(baseline, current)}{Environment.NewLine}" +
                $"The full current version was written to '{actualOf}'.{Environment.NewLine}" +
                $"If the change is intended, regenerate the file with '{EnvironmentVariable}=1' and review its diff."
            );

        }

        #endregion

        #region Read(ResourceName)

        /// <summary>
        /// The checked-in document, as embedded into this test assembly.
        /// </summary>
        /// <param name="ResourceName">The name of the embedded resource.</param>
        public static String Read(String ResourceName)
        {

            using var stream = typeof(GeneratedDocument).Assembly.GetManifestResourceStream(ResourceName)
                                   ?? throw new InvalidOperationException($"The embedded document '{ResourceName}' is missing!");

            using var reader = new StreamReader(stream, Encoding.UTF8);

            return Normalize(reader.ReadToEnd());

        }

        #endregion

        #region SourcePath(RelativePath)

        /// <summary>
        /// A file within the working tree, found by walking up from the test assembly until the
        /// directory holding it appears; null when the tests run from somewhere else.
        /// </summary>
        /// <param name="RelativePath">The path of the file relative to the repository root.</param>
        public static String? SourcePath(String RelativePath)
        {

            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

            while (directory is not null)
            {

                var candidate = Path.Combine(directory.FullName, RelativePath);

                if (File.Exists(candidate))
                    return candidate;

                directory = directory.Parent;

            }

            return null;

        }

        #endregion

        #region Normalize(Text)

        /// <summary>
        /// Line endings and the trailing newline must not make a comparison fail: the file is
        /// written on one platform and checked out on another, possibly through git's own
        /// line ending translation.
        /// </summary>
        /// <param name="Text">The text to normalize.</param>
        public static String Normalize(String Text)
            => Text.Replace("\r\n", "\n").TrimEnd('\n');

        #endregion

        #region Diff(Baseline, Current)

        /// <summary>
        /// The first few lines that only one of the two sides has, which is what a reviewer needs
        /// to see in the failure message; the complete document is written to a file next to it.
        /// </summary>
        /// <param name="Baseline">The checked-in document.</param>
        /// <param name="Current">The freshly rendered document.</param>
        public static String Diff(String Baseline, String Current)
        {

            var baseline  = Baseline.Split('\n').ToHashSet(StringComparer.Ordinal);
            var current   = Current. Split('\n').ToHashSet(StringComparer.Ordinal);

            var removed   = baseline.Except(current, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            var added     = current. Except(baseline, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

            var report    = new StringBuilder();

            report.Append("Removed (").Append(removed.Length).AppendLine(" lines):");
            foreach (var line in removed.Take(25))
                report.Append("  - ").AppendLine(line.Trim());
            if (removed.Length > 25)
                report.Append("  ... ").Append(removed.Length - 25).AppendLine(" more");

            report.AppendLine();

            report.Append("Added (").Append(added.Length).AppendLine(" lines):");
            foreach (var line in added.Take(25))
                report.Append("  + ").AppendLine(line.Trim());
            if (added.Length > 25)
                report.Append("  ... ").Append(added.Length - 25).AppendLine(" more");

            return report.ToString();

        }

        #endregion

    }

}

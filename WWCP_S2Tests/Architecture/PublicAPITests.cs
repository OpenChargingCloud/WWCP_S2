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
    /// The public API baseline (PLAN.md, Phase 12): every public and protected member of the
    /// library is rendered into a stable text form and compared with the checked-in baseline
    /// <c>Architecture/PublicAPI.baseline.txt</c>. A change of the public surface is therefore
    /// never accidental - it appears as a diff of that file in the same commit that causes it,
    /// which is what makes the semantic versioning of the package reviewable.
    ///
    /// To accept a deliberate change, regenerate the baseline and review the diff:
    /// <code>
    ///   S2_UPDATE_PUBLIC_API=1 dotnet test WWCP_S2Tests/WWCP_S2Tests.csproj --filter "FullyQualifiedName~PublicAPITests"
    /// </code>
    /// (PowerShell: <c>$env:S2_UPDATE_PUBLIC_API = "1"</c> before the run.)
    ///
    /// What the baseline records is the shape of the surface: namespaces, type kinds, base types,
    /// implemented interfaces, and the signatures of the members. It deliberately does not record
    /// nullability annotations, attributes or default values, so it is a review aid rather than a
    /// binary compatibility checker.
    /// </summary>
    [TestFixture]
    public sealed class PublicAPITests
    {

        #region Data

        private const String  BaselineResource     = "cloud.charging.open.protocols.S2.Tests.Architecture.PublicAPI.baseline.txt";
        private const String  BaselineFile         = "PublicAPI.baseline.txt";
        private const String  UpdateEnvironment    = "S2_UPDATE_PUBLIC_API";

        #endregion


        #region PublicAPI_MatchesTheBaseline()

        [Test]
        public void PublicAPI_MatchesTheBaseline()
        {

            var current   = Render(typeof(Version).Assembly);
            var update    = Environment.GetEnvironmentVariable(UpdateEnvironment) is String flag &&
                            (flag == "1" || flag.Equals("true", StringComparison.OrdinalIgnoreCase));
            var sourceOf  = SourcePath();

            if (update)
            {

                if (sourceOf is null)
                    Assert.Fail($"'{UpdateEnvironment}' is set, but the baseline file could not be located from '{TestContext.CurrentContext.TestDirectory}'.");

                else
                {
                    File.WriteAllText(sourceOf, current, new UTF8Encoding(false));
                    Assert.Warn($"The public API baseline was regenerated: '{sourceOf}'. Review the diff before committing it.");
                }

                return;

            }

            var baseline  = Baseline();

            if (baseline == current)
                return;

            var actualOf  = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PublicAPI.actual.txt");
            File.WriteAllText(actualOf, current, new UTF8Encoding(false));

            Assert.Fail(
                $"The public API of the library differs from '{BaselineFile}'.{Environment.NewLine}{Environment.NewLine}" +
                $"{Diff(baseline, current)}{Environment.NewLine}" +
                $"The full current surface was written to '{actualOf}'.{Environment.NewLine}" +
                $"If the change is intended, regenerate the baseline with '{UpdateEnvironment}=1' and review its diff."
            );

        }

        #endregion

        #region Baseline_IsNotEmpty()

        /// <summary>
        /// A baseline that renders nothing would let every change pass, so the guard is guarded.
        /// </summary>
        [Test]
        public void Baseline_IsNotEmpty()
        {

            var baseline = Baseline();

            Assert.Multiple(() => {
                Assert.That(baseline.Length,                       Is.GreaterThan(10_000));
                Assert.That(baseline, Does.Contain("class cloud.charging.open.protocols.S2.Node.AS2Node"));
                Assert.That(baseline, Does.Contain("cloud.charging.open.protocols.S2.Connect"));
            });

        }

        #endregion


        #region (private static) Baseline()

        private static String Baseline()
        {

            using var stream = typeof(PublicAPITests).Assembly.GetManifestResourceStream(BaselineResource)
                                   ?? throw new InvalidOperationException($"The embedded baseline '{BaselineResource}' is missing!");

            using var reader = new StreamReader(stream, Encoding.UTF8);

            return Normalize(reader.ReadToEnd());

        }

        #endregion

        #region (private static) SourcePath()

        /// <summary>
        /// The baseline within the working tree, found by walking up from the test assembly until
        /// the directory holding the solution appears; null when the tests run from somewhere else.
        /// </summary>
        private static String? SourcePath()
        {

            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

            while (directory is not null)
            {

                var candidate = Path.Combine(directory.FullName, "WWCP_S2Tests", "Architecture", BaselineFile);

                if (File.Exists(candidate))
                    return candidate;

                directory = directory.Parent;

            }

            return null;

        }

        #endregion

        #region (private static) Normalize(Text)

        private static String Normalize(String Text)
            => Text.Replace("\r\n", "\n").TrimEnd('\n');

        #endregion

        #region (private static) Diff(Baseline, Current)

        /// <summary>
        /// The first few lines that only one of the two sides has, which is what a reviewer needs
        /// to see in the failure message; the complete listing is written to a file next to it.
        /// </summary>
        private static String Diff(String Baseline, String Current)
        {

            var baseline  = Baseline.Split('\n').ToHashSet(StringComparer.Ordinal);
            var current   = Current. Split('\n').ToHashSet(StringComparer.Ordinal);

            var removed   = baseline.Except(current, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            var added     = current. Except(baseline, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

            var report    = new StringBuilder();

            report.Append("Removed from the public API (").Append(removed.Length).AppendLine(" lines):");
            foreach (var line in removed.Take(25))
                report.Append("  - ").AppendLine(line.Trim());
            if (removed.Length > 25)
                report.Append("  ... ").Append(removed.Length - 25).AppendLine(" more");

            report.AppendLine();

            report.Append("Added to the public API (").Append(added.Length).AppendLine(" lines):");
            foreach (var line in added.Take(25))
                report.Append("  + ").AppendLine(line.Trim());
            if (added.Length > 25)
                report.Append("  ... ").Append(added.Length - 25).AppendLine(" more");

            return report.ToString();

        }

        #endregion


        #region (private static) Render(Assembly)

        /// <summary>
        /// The public surface of an assembly as deterministic text: one block per type, its
        /// members indented below it, everything ordered ordinally so the file only changes
        /// when the API does.
        /// </summary>
        private static String Render(Assembly Assembly)
        {

            var api = new StringBuilder();

            api.AppendLine("# The public API of cloud.charging.open.protocols.S2.");
            api.AppendLine("# Generated by WWCP_S2Tests/Architecture/PublicAPITests.cs - do not edit by hand:");
            api.AppendLine("#   S2_UPDATE_PUBLIC_API=1 dotnet test WWCP_S2Tests/WWCP_S2Tests.csproj --filter \"FullyQualifiedName~PublicAPITests\"");
            api.AppendLine("# A diff of this file is a change of the public API and needs the version to be considered.");

            foreach (var type in Assembly.GetExportedTypes().
                                          OrderBy(type => RenderTypeHeader(type), StringComparer.Ordinal))
            {

                api.AppendLine();
                api.AppendLine(RenderTypeHeader(type));

                foreach (var member in RenderMembers(type).Order(StringComparer.Ordinal))
                    api.Append("    ").AppendLine(member);

            }

            return Normalize(api.ToString());

        }

        #endregion

        #region (private static) RenderTypeHeader(Type)

        private static String RenderTypeHeader(Type Type)
        {

            var header = new StringBuilder();

            if      (Type.IsEnum)       header.Append("enum ");
            else if (Type.IsInterface)  header.Append("interface ");
            else if (Type.IsValueType)  header.Append(Type.IsByRefLike ? "ref struct " : "struct ");
            else if (Type.IsSubclassOf(typeof(Delegate)))
                                        header.Append("delegate ");
            else
            {
                if (Type.IsAbstract && Type.IsSealed)  header.Append("static ");
                else if (Type.IsAbstract)              header.Append("abstract ");
                else if (Type.IsSealed)                header.Append("sealed ");
                header.Append("class ");
            }

            header.Append(Name(Type));

            var bases = new List<String>();

            if (Type.BaseType is Type baseType &&
                baseType != typeof(Object)     &&
                baseType != typeof(ValueType)  &&
                baseType != typeof(Enum)       &&
                baseType != typeof(MulticastDelegate))
            {
                bases.Add(Name(baseType));
            }

            // Only the interfaces this type adds itself: everything the base type already
            // implements would otherwise be repeated in every derived type.
            var inherited = new HashSet<Type>(Type.BaseType?.GetInterfaces() ?? []);

            bases.AddRange(Type.GetInterfaces().
                                Where(interfaceType => !inherited.Contains(interfaceType)).
                                Select(Name).
                                Order(StringComparer.Ordinal));

            if (bases.Count > 0)
                header.Append(" : ").Append(String.Join(", ", bases));

            return header.ToString();

        }

        #endregion

        #region (private static) RenderMembers(Type)

        private static IEnumerable<String> RenderMembers(Type Type)
        {

            const BindingFlags flags = BindingFlags.Public    |
                                       BindingFlags.NonPublic |
                                       BindingFlags.Instance  |
                                       BindingFlags.Static    |
                                       BindingFlags.DeclaredOnly;

            foreach (var field in Type.GetFields(flags).Where(field => IsVisible(field.IsPublic, field.IsFamily, field.IsFamilyOrAssembly)))
                yield return $"{(field.IsStatic ? "static " : "")}{(field.IsLiteral ? "const " : "")}{Name(field.FieldType)} {field.Name}";

            foreach (var property in Type.GetProperties(flags))
            {

                var getter = property.GetMethod;
                var setter = property.SetMethod;

                if (!IsVisible(getter) && !IsVisible(setter))
                    continue;

                if (property.Name == "EqualityContract")
                    continue;

                var accessors = String.Concat(
                                    IsVisible(getter) ? "get; " : "",
                                    IsVisible(setter) ? (setter!.ReturnParameter.GetRequiredCustomModifiers().
                                                             Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit")
                                                             ? "init; "
                                                             : "set; ")
                                                      : ""
                                );

                var indexer   = property.GetIndexParameters();

                yield return $"{((getter ?? setter)!.IsStatic ? "static " : "")}{Name(property.PropertyType)} {property.Name}" +
                             (indexer.Length > 0 ? $"[{String.Join(", ", indexer.Select(parameter => Name(parameter.ParameterType)))}]" : "") +
                             $" {{ {accessors}}}";

            }

            foreach (var @event in Type.GetEvents(flags).Where(@event => IsVisible(@event.AddMethod)))
                yield return $"event {Name(@event.EventHandlerType!)} {@event.Name}";

            foreach (var constructor in Type.GetConstructors(flags).Where(IsVisible))
                yield return $".ctor({Parameters(constructor)})";

            foreach (var method in Type.GetMethods(flags).Where(IsVisible))
            {

                // Property, event and record boilerplate is rendered through the member it belongs to.
                if (method.IsSpecialName && (method.Name.StartsWith("get_",    StringComparison.Ordinal) ||
                                             method.Name.StartsWith("set_",    StringComparison.Ordinal) ||
                                             method.Name.StartsWith("add_",    StringComparison.Ordinal) ||
                                             method.Name.StartsWith("remove_", StringComparison.Ordinal)))
                    continue;

                if (method.Name.StartsWith('<') || method.Name == "PrintMembers")
                    continue;

                yield return $"{(method.IsStatic ? "static " : "")}{Name(method.ReturnType)} {method.Name}{GenericParameters(method)}({Parameters(method)})";

            }

        }

        #endregion

        #region (private static) Helpers

        private static Boolean IsVisible(Boolean Public, Boolean Family, Boolean FamilyOrAssembly)
            => Public || Family || FamilyOrAssembly;

        private static Boolean IsVisible(MethodBase? Method)
            => Method is not null &&
               IsVisible(Method.IsPublic, Method.IsFamily, Method.IsFamilyOrAssembly);

        private static String Parameters(MethodBase Method)
            => String.Join(
                   ", ",
                   Method.GetParameters().
                          Select(parameter => (parameter.IsOut ? "out " : parameter.ParameterType.IsByRef ? "ref " : "") +
                                              Name(parameter.ParameterType) +
                                              " " +
                                              parameter.Name)
               );

        private static String GenericParameters(MethodInfo Method)
            => Method.IsGenericMethodDefinition
                   ? "<" + String.Join(", ", Method.GetGenericArguments().Select(argument => argument.Name)) + ">"
                   : "";

        /// <summary>
        /// A readable, stable name of a type: generic arguments in angle brackets, arrays and
        /// by-reference types unwrapped, nested types with their declaring type.
        /// </summary>
        private static String Name(Type Type)
        {

            if (Type.IsByRef)
                return Name(Type.GetElementType()!);

            if (Type.IsArray)
                return Name(Type.GetElementType()!) + "[" + new String(',', Type.GetArrayRank() - 1) + "]";

            if (Type.IsGenericParameter)
                return Type.Name;

            if (Nullable.GetUnderlyingType(Type) is Type underlying)
                return Name(underlying) + "?";

            var name = Type.IsNested
                           ? Name(Type.DeclaringType!) + "." + Type.Name
                           : (Type.Namespace is null ? Type.Name : Type.Namespace + "." + Type.Name);

            if (!Type.IsGenericType)
                return name;

            var backtick = name.LastIndexOf('`');

            return (backtick > 0 ? name[..backtick] : name) +
                   "<" + String.Join(", ", Type.GetGenericArguments().Select(Name)) + ">";

        }

        #endregion

    }

}

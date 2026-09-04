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

using NetArchTest.Rules;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Architecture
{

    /// <summary>
    /// The layering rule of PLAN.md D2: the data model and messages (root namespace) never
    /// reference the session layer or S2 Connect, the session layer never references
    /// S2 Connect, and S2 Connect never references the node layer. This keeps a later split
    /// into separate assemblies and a client-only constrained-RM build possible.
    /// </summary>
    [TestFixture]
    public sealed class LayeringTests
    {

        private const String Root        = "cloud.charging.open.protocols.S2";
        private const String Session     = Root + ".Session";
        private const String WebSockets  = Root + ".WebSockets";
        private const String Connect     = Root + ".Connect";
        private const String Node        = Root + ".Node";

        private static Types LibraryTypes
            => Types.InAssembly(typeof(Version).Assembly);

        [Test]
        public void DataModelAndMessages_DoNotReference_SessionConnectOrNode()
        {

            var result = LibraryTypes.
                             That().ResideInNamespace(Root).
                             And().DoNotResideInNamespace(Session).
                             And().DoNotResideInNamespace(WebSockets).
                             And().DoNotResideInNamespace(Connect).
                             And().DoNotResideInNamespace(Node).
                             ShouldNot().HaveDependencyOnAny(Session, WebSockets, Connect, Node).
                             GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                        () => "Root types referencing higher layers: " + String.Join(", ", result.FailingTypeNames ?? []));

        }

        [Test]
        public void SessionLayer_DoesNotReference_ConnectOrNode()
        {

            var result = LibraryTypes.
                             That().ResideInNamespace(Session).
                             ShouldNot().HaveDependencyOnAny(Connect, Node).
                             GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                        () => "Session types referencing higher layers: " + String.Join(", ", result.FailingTypeNames ?? []));

        }

        [Test]
        public void ConnectLayer_DoesNotReference_Node()
        {

            var result = LibraryTypes.
                             That().ResideInNamespace(Connect).
                             ShouldNot().HaveDependencyOn(Node).
                             GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                        () => "Connect types referencing the node layer: " + String.Join(", ", result.FailingTypeNames ?? []));

        }

    }

}

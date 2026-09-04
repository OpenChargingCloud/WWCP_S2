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

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The mapping of CEM and RM to communication server and client (S2 Connect 1.0.0,
    /// "Mapping the CEM and RM to communication server or client"), row by row and from
    /// the perspective of both nodes, plus the text representation used for persistence.
    /// </summary>
    [TestFixture]
    public sealed class CommunicationRoleTests
    {

        #region (private) AssertRow(CEMDeployment, RMDeployment, ExpectedCEMRole, ExpectedRMRole)

        /// <summary>
        /// Assert one row of the mapping table from both perspectives.
        /// </summary>
        private static void AssertRow(Deployment         CEMDeployment,
                                      Deployment         RMDeployment,
                                      CommunicationRole  ExpectedCEMRole,
                                      CommunicationRole  ExpectedRMRole)
        {

            var cemRole = CommunicationRoleExtensions.Determine(EnergyManagementRole.CEM, CEMDeployment, RMDeployment);
            var rmRole  = CommunicationRoleExtensions.Determine(EnergyManagementRole.RM,  RMDeployment,  CEMDeployment);

            Assert.Multiple(() => {
                Assert.That(cemRole,            Is.EqualTo(ExpectedCEMRole), "CEM perspective");
                Assert.That(rmRole,             Is.EqualTo(ExpectedRMRole),  "RM perspective");
                Assert.That(cemRole.Opposite(), Is.EqualTo(rmRole),          "both perspectives agree");
                Assert.That(rmRole.Opposite(),  Is.EqualTo(cemRole),         "both perspectives agree");
            });

        }

        #endregion


        #region Mapping table

        [Test]
        [S2C("CommunicationRoles.WAN-WAN")]
        public void CEM_WAN_RM_WAN_TheCEMIsTheCommunicationServer()
            => AssertRow(Deployment.WAN, Deployment.WAN, CommunicationRole.CommunicationServer, CommunicationRole.CommunicationClient);

        [Test]
        [S2C("CommunicationRoles.WAN-LAN")]
        public void CEM_WAN_RM_LAN_TheCEMIsTheCommunicationServer()
            => AssertRow(Deployment.WAN, Deployment.LAN, CommunicationRole.CommunicationServer, CommunicationRole.CommunicationClient);

        [Test]
        [S2C("CommunicationRoles.LAN-WAN")]
        public void CEM_LAN_RM_WAN_TheRMIsTheCommunicationServer()
            => AssertRow(Deployment.LAN, Deployment.WAN, CommunicationRole.CommunicationClient, CommunicationRole.CommunicationServer);

        [Test]
        [S2C("CommunicationRoles.LAN-LAN")]
        public void CEM_LAN_RM_LAN_TheCEMIsTheCommunicationServer()
            => AssertRow(Deployment.LAN, Deployment.LAN, CommunicationRole.CommunicationServer, CommunicationRole.CommunicationClient);

        #endregion

        #region Determine(...) guards

        [Test]
        public void Determine_RejectsUnknownRolesAndDeployments()
        {

            var unknownRole        = EnergyManagementRole.Parse("BROKER");
            var unknownDeployment  = Deployment.Parse("CLOUD");

            Assert.Multiple(() => {
                Assert.That(unknownRole.IsKnown,       Is.False);
                Assert.That(unknownDeployment.IsKnown, Is.False);
                Assert.That(() => CommunicationRoleExtensions.Determine(unknownRole,              Deployment.LAN,     Deployment.LAN),     Throws.ArgumentException);
                Assert.That(() => CommunicationRoleExtensions.Determine(EnergyManagementRole.CEM, unknownDeployment,  Deployment.LAN),     Throws.ArgumentException);
                Assert.That(() => CommunicationRoleExtensions.Determine(EnergyManagementRole.CEM, Deployment.LAN,     unknownDeployment),  Throws.ArgumentException);
                Assert.That(() => CommunicationRoleExtensions.Determine(default,                  Deployment.LAN,     Deployment.LAN),     Throws.ArgumentException);
                Assert.That(() => CommunicationRoleExtensions.Determine(EnergyManagementRole.RM,  default,            Deployment.LAN),     Throws.ArgumentException);
                Assert.That(() => CommunicationRoleExtensions.Determine(EnergyManagementRole.RM,  Deployment.WAN,     default),            Throws.ArgumentException);
            });

        }

        #endregion

        #region Opposite()

        [Test]
        public void Opposite_SwapsServerAndClient()
        {

            Assert.Multiple(() => {
                Assert.That(CommunicationRole.CommunicationServer.Opposite(), Is.EqualTo(CommunicationRole.CommunicationClient));
                Assert.That(CommunicationRole.CommunicationClient.Opposite(), Is.EqualTo(CommunicationRole.CommunicationServer));
            });

        }

        #endregion

        #region AsText() / TryParse(...)

        [Test]
        public void AsText_AndTryParse_RoundTrip()
        {

            Assert.Multiple(() => {

                Assert.That(CommunicationRole.CommunicationServer.AsText(), Is.EqualTo("CommunicationServer"));
                Assert.That(CommunicationRole.CommunicationClient.AsText(), Is.EqualTo("CommunicationClient"));

                Assert.That(CommunicationRoleExtensions.TryParse("CommunicationServer", out var server), Is.True);
                Assert.That(server,                                                                      Is.EqualTo(CommunicationRole.CommunicationServer));
                Assert.That(CommunicationRoleExtensions.TryParse("CommunicationClient", out var client), Is.True);
                Assert.That(client,                                                                      Is.EqualTo(CommunicationRole.CommunicationClient));

                foreach (var role in Enum.GetValues<CommunicationRole>())
                {
                    Assert.That(CommunicationRoleExtensions.TryParse(role.AsText(), out var parsed), Is.True, role.ToString());
                    Assert.That(parsed,                                                              Is.EqualTo(role));
                }

            });

        }

        [Test]
        public void TryParse_RejectsUnknownAndDifferentlyCasedText()
        {

            Assert.Multiple(() => {
                Assert.That(CommunicationRoleExtensions.TryParse("Peer",                out _), Is.False);
                Assert.That(CommunicationRoleExtensions.TryParse("communicationserver", out _), Is.False);
                Assert.That(CommunicationRoleExtensions.TryParse("",                    out _), Is.False);
                Assert.That(CommunicationRoleExtensions.TryParse(null,                  out _), Is.False);
            });

        }

        [Test]
        public void AsText_RejectsAnUndefinedRole()
        {
            Assert.That(() => ((CommunicationRole) 42).AsText(), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        #endregion

    }

}

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
using cloud.charging.open.protocols.S2.Node;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Node
{

    /// <summary>
    /// The options of an S2 node (PLAN.md Phase 10a, §3.7): <see cref="S2NodeOptions.Validate"/>
    /// rejects incomplete or contradictory options, a minimal record validates, the record's
    /// <c>with</c> expression keeps the other values, and the communication roles of a constructed
    /// <see cref="RMNode"/> / <see cref="CEMNode"/> follow the deployment and the role
    /// (S2 Connect 1.0.0, "Communication roles"). No network: the nodes are built, never started.
    /// </summary>
    [TestFixture]
    public sealed class S2NodeOptionsTests
    {

        #region Helpers

        /// <summary>
        /// A minimal, valid set of node options (LAN, only the three required members).
        /// </summary>
        private static S2NodeOptions ValidOptions()

            => new () {
                   Description  = new EndpointDescription("Test endpoint"),
                   Deployment   = Deployment.LAN,
                   PairingUrl   = S2BaseURL.Parse("https://test.local/pairing/")
               };

        /// <summary>
        /// Options of a LAN CEM (a communication server): pairing and session initiation URL.
        /// </summary>
        private static S2NodeOptions CEMServerOptions()

            => new () {
                   Description           = new EndpointDescription("Test CEM"),
                   Deployment            = Deployment.LAN,
                   PairingUrl            = S2BaseURL.Parse("https://cem.local/pairing/"),
                   SessionInitiationUrl  = S2BaseURL.Parse("https://cem.local/connection/")
               };

        /// <summary>
        /// Options of a LAN RM (a communication client): only the pairing URL.
        /// </summary>
        private static S2NodeOptions RMClientOptions()

            => new () {
                   Description  = new EndpointDescription("Test RM"),
                   Deployment   = Deployment.LAN,
                   PairingUrl   = S2BaseURL.Parse("https://rm.local/pairing/")
               };

        private static HostedNode HostedNodeOf(EnergyManagementRole Role)

            => new (new NodeDescription(
                        Node_Id.NewRandom,
                        "GraphDefined",
                        Role == EnergyManagementRole.CEM ? "EMS" : "EV charger",
                        "TestModel",
                        Role
                    ));

        #endregion


        #region Validate()

        [Test]
        public void Validate_MinimalValidOptions_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ValidOptions().Validate());
        }

        [Test]
        public void Validate_MissingDescription_Throws()
        {

            var options = ValidOptions() with { Description = null! };

            Assert.Throws<ArgumentException>(() => options.Validate());

        }

        [Test]
        public void Validate_UnknownDeployment_Throws()
        {

            var options = ValidOptions() with { Deployment = Deployment.Parse("Cloud") };

            Assert.Throws<ArgumentException>(() => options.Validate());

        }

        [Test]
        public void Validate_EmptyPairingUrl_Throws()
        {

            var options = ValidOptions() with { PairingUrl = default };

            Assert.Throws<ArgumentException>(() => options.Validate());

        }

        [Test]
        public void Validate_WANPairingServerForLANEndpoint_OnWANDeployment_Throws()
        {

            var options = ValidOptions() with {
                              Deployment                        = Deployment.WAN,
                              IsWANPairingServerForLANEndpoint  = true
                          };

            Assert.Throws<ArgumentException>(() => options.Validate());

        }

        [Test]
        public void Validate_EmptySupportedS2MessageVersions_Throws()
        {

            var options = ValidOptions() with { SupportedS2MessageVersions = [] };

            Assert.Throws<ArgumentException>(() => options.Validate());

        }

        [Test]
        public void Validate_EmptySupportedCommunicationProtocols_Throws()
        {

            var options = ValidOptions() with { SupportedCommunicationProtocols = [] };

            Assert.Throws<ArgumentException>(() => options.Validate());

        }

        [Test]
        public void Validate_InvalidWebSocketPingInterval_Throws()
        {

            // Zero is not positive, and 61 s is above the 60 s maximum of the specification.
            Assert.Throws<ArgumentException>(() => (ValidOptions() with { WebSocketPingInterval = TimeSpan.Zero }).            Validate());
            Assert.Throws<ArgumentException>(() => (ValidOptions() with { WebSocketPingInterval = TimeSpan.FromSeconds(61) }).Validate());

        }

        [Test]
        public void Validate_NegativeStopDrainTimeout_Throws()
        {

            var options = ValidOptions() with { StopDrainTimeout = TimeSpan.FromSeconds(-1) };

            Assert.Throws<ArgumentException>(() => options.Validate());

        }

        #endregion

        #region With expression

        [Test]
        public void WithExpression_KeepsOtherValues()
        {

            var baseline  = ValidOptions();
            var modified  = baseline with { StopDrainTimeout = TimeSpan.FromSeconds(9) };

            Assert.Multiple(() => {
                Assert.That(modified.Description,            Is.EqualTo(baseline.Description));
                Assert.That(modified.Deployment,            Is.EqualTo(baseline.Deployment));
                Assert.That(modified.PairingUrl,            Is.EqualTo(baseline.PairingUrl));
                Assert.That(modified.WebSocketPingInterval, Is.EqualTo(baseline.WebSocketPingInterval));
                Assert.That(modified.StopDrainTimeout,      Is.EqualTo(TimeSpan.FromSeconds(9)));
            });

        }

        #endregion


        #region Communication roles (constructed nodes, not started)

        [Test]
        public async Task CEMNode_LAN_IsCommunicationServer()
        {

            await using var node = new CEMNode(HostedNodeOf(EnergyManagementRole.CEM), CEMServerOptions());

            Assert.Multiple(() => {
                Assert.That(node.IsCommunicationServer, Is.True);
                Assert.That(node.IsCommunicationClient, Is.False);
            });

        }

        [Test]
        public async Task RMNode_LAN_IsCommunicationClient()
        {

            await using var node = new RMNode(HostedNodeOf(EnergyManagementRole.RM), RMClientOptions());

            Assert.Multiple(() => {
                Assert.That(node.IsCommunicationServer, Is.False);
                Assert.That(node.IsCommunicationClient, Is.True);
            });

        }

        [Test]
        public async Task RMNode_WAN_IsCommunicationServer()
        {

            // A WAN endpoint is always the communication server, regardless of the role, so it
            // needs a session initiation URL.
            var options = RMClientOptions() with {
                              Deployment            = Deployment.WAN,
                              PairingUrl            = S2BaseURL.Parse("https://rm.example.com/pairing/"),
                              SessionInitiationUrl  = S2BaseURL.Parse("https://rm.example.com/connection/")
                          };

            await using var node = new RMNode(HostedNodeOf(EnergyManagementRole.RM), options);

            Assert.Multiple(() => {
                Assert.That(node.IsCommunicationServer, Is.True);
                Assert.That(node.IsCommunicationClient, Is.False);
            });

        }

        [Test]
        public async Task CEMNode_EnableCommunicationServerFalse_IsNotCommunicationServer()
        {

            var options = CEMServerOptions() with { EnableCommunicationServer = false };

            await using var node = new CEMNode(HostedNodeOf(EnergyManagementRole.CEM), options);

            Assert.That(node.IsCommunicationServer, Is.False);

        }

        [Test]
        public void CommunicationServerWithoutSessionInitiationUrl_ThrowsArgumentException()
        {

            // A CEM defaults to a communication server; without a session initiation URL the
            // constructor rejects the configuration.
            var hosted   = HostedNodeOf(EnergyManagementRole.CEM);
            var options  = CEMServerOptions() with { SessionInitiationUrl = null };

            Assert.Throws<ArgumentException>(() => { _ = new CEMNode(hosted, options); });

        }

        [Test]
        public void HostedNodeRoleMismatch_ThrowsArgumentException()
        {

            // A CEM node hosting an RM node (and vice versa) is rejected by the constructor.
            var rmHostedNode  = HostedNodeOf(EnergyManagementRole.RM);
            var cemOptions    = CEMServerOptions();

            Assert.Throws<ArgumentException>(() => { _ = new CEMNode(rmHostedNode, cemOptions); });

        }

        #endregion

    }

}

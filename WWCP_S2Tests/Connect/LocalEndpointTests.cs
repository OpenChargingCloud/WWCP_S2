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

using Microsoft.Extensions.Time.Testing;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The local endpoint (S2 Connect 1.0.0, "The node and the endpoint"): its deployment,
    /// URLs, domain name and certificate fingerprint, and the registry of hosted nodes.
    /// </summary>
    [TestFixture]
    public sealed class LocalEndpointTests
    {

        #region Data

        private static readonly S2BaseURL               PairingUrl   = S2BaseURL.Parse("https://hostname.local/pairing/");
        private static readonly S2BaseURL               SessionUrl   = S2BaseURL.Parse("https://hostname.local/connection/");
        private static readonly CertificateFingerprint  Fingerprint1 = CertificateFingerprint.Parse("A1:B2:C3:D4:E5:F6:07:18:29:3A:4B:5C:6D:7E:8F:90:A1:B2:C3:D4:E5:F6:07:18:29:3A:4B:5C:6D:7E:8F:90");
        private static readonly CertificateFingerprint  Fingerprint2 = CertificateFingerprint.Parse("AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89");

        private static readonly Node_Id                 RMNodeId     = Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000001");
        private static readonly Node_Id                 CEMNodeId    = Node_Id.Parse("6f2f5c1e-0000-4000-8000-000000000002");
        private static readonly NodeIdAlias             AliasA0      = NodeIdAlias.Parse("A0");
        private static readonly NodeIdAlias             AliasB1      = NodeIdAlias.Parse("B1");

        private static NodeDescription RMDescription(Node_Id? Id = null)
            => new (Id ?? RMNodeId, "ACME", "EV charger", "WallBox-b100", EnergyManagementRole.RM);

        private static NodeDescription CEMDescription(Node_Id? Id = null)
            => new (Id ?? CEMNodeId, "ACME", "EMS", "HomeManager", EnergyManagementRole.CEM);

        private static LocalEndpoint CreateEndpoint(Deployment?                            EndpointDeployment                 = null,
                                                    EndpointDescription?                   Description                        = null,
                                                    S2BaseURL?                             Url                                = null,
                                                    Boolean                                IsWANPairingServerForLANEndpoint   = false,
                                                    ServerCertificateFingerprintDelegate?  FingerprintProvider                = null,
                                                    TimeProvider?                          Clock                              = null)

            => new (Description ?? new EndpointDescription("Test endpoint"),
                    EndpointDeployment ?? Deployment.LAN,
                    Url ?? PairingUrl,
                    SessionUrl,
                    IsWANPairingServerForLANEndpoint,
                    FingerprintProvider,
                    TimeProvider: Clock);

        #endregion


        #region Constructor: deployment and description

        [Test]
        public void Constructor_AcceptsLANAndWAN_AndRejectsOtherDeployments()
        {

            Assert.Multiple(() => {
                Assert.That(CreateEndpoint(Deployment.LAN).Deployment,              Is.EqualTo(Deployment.LAN));
                Assert.That(CreateEndpoint(Deployment.WAN).Deployment,              Is.EqualTo(Deployment.WAN));
                Assert.That(() => CreateEndpoint(Deployment.Parse("CLOUD")),        Throws.ArgumentException);
                Assert.That(() => CreateEndpoint(default(Deployment)),              Throws.ArgumentException);
            });

        }

        [Test]
        public void Constructor_RejectsADescriptionWithAnotherDeployment()
        {

            var wanDescription = new EndpointDescription("Cloud", null, Deployment.WAN);

            Assert.Multiple(() => {
                Assert.That(() => CreateEndpoint(Deployment.LAN, wanDescription),           Throws.ArgumentException);
                Assert.That(CreateEndpoint(Deployment.WAN, wanDescription).Description,     Is.SameAs(wanDescription), "a matching description is kept as it is");
            });

        }

        [Test]
        public void Constructor_FillsInTheDeploymentOfADescriptionWithoutOne()
        {

            var endpoint = CreateEndpoint(Deployment.LAN, new EndpointDescription("Garage"));

            Assert.Multiple(() => {
                Assert.That(endpoint.Description.Deployment, Is.EqualTo(Deployment.LAN));
                Assert.That(endpoint.Description.Name,       Is.EqualTo("Garage"));
                Assert.That(endpoint.Description.LogoUrl,    Is.Null);
                Assert.That(endpoint.Description,            Is.EqualTo(new EndpointDescription("Garage", null, Deployment.LAN)));
            });

        }

        [Test]
        public void Constructor_RejectsNullDescriptionAndEmptyPairingUrl()
        {

            Assert.Multiple(() => {
                Assert.That(() => new LocalEndpoint(null!, Deployment.LAN, PairingUrl), Throws.ArgumentNullException);
                Assert.That(() => CreateEndpoint(Url: default(S2BaseURL)),               Throws.ArgumentException);
            });

        }

        [Test]
        public void Constructor_KeepsTheUrls()
        {

            var endpoint = CreateEndpoint();

            Assert.Multiple(() => {
                Assert.That(endpoint.PairingUrl,           Is.EqualTo(PairingUrl));
                Assert.That(endpoint.SessionInitiationUrl, Is.EqualTo(SessionUrl));
                Assert.That(new LocalEndpoint(new EndpointDescription(), Deployment.WAN, PairingUrl).SessionInitiationUrl, Is.Null);
            });

        }

        #endregion

        #region IsWANPairingServerForLANEndpoint

        [Test]
        [S2C("Pairing.Deployments.WANServerForLANEndpoint")]
        public void IsWANPairingServerForLANEndpoint_RequiresALANEndpoint()
        {

            Assert.Multiple(() => {
                Assert.That(CreateEndpoint().IsWANPairingServerForLANEndpoint,                                                  Is.False);
                Assert.That(CreateEndpoint(Deployment.LAN, IsWANPairingServerForLANEndpoint: true).IsWANPairingServerForLANEndpoint, Is.True);
                Assert.That(() => CreateEndpoint(Deployment.WAN, IsWANPairingServerForLANEndpoint: true),                       Throws.ArgumentException);
            });

        }

        #endregion

        #region DomainName

        [Test]
        [S2C("Pairing.ChallengeResponse.WAN")]
        public void DomainName_IsTheNormalisedHostOfThePairingUrl()
        {

            Assert.Multiple(() => {
                Assert.That(CreateEndpoint(Url: S2BaseURL.Parse("https://Example.COM/pairing/")).DomainName,       Is.EqualTo("example.com"));
                Assert.That(CreateEndpoint(Url: S2BaseURL.Parse("https://hostname.local/pairing/")).DomainName,    Is.EqualTo("hostname.local"));
                Assert.That(CreateEndpoint(Url: S2BaseURL.Parse("https://Pairing.S2.Example.com/")).DomainName,    Is.EqualTo("pairing.s2.example.com"));
            });

        }

        #endregion

        #region ServerCertificateFingerprint

        [Test]
        [S2C("Pairing.ChallengeResponse.LAN")]
        public void ServerCertificateFingerprint_ComesFromTheDelegate()
        {

            var current   = Fingerprint1;
            var endpoint  = CreateEndpoint(FingerprintProvider: () => current);

            Assert.That(endpoint.ServerCertificateFingerprint, Is.EqualTo(Fingerprint1));

            current = Fingerprint2;

            Assert.That(endpoint.ServerCertificateFingerprint, Is.EqualTo(Fingerprint2), "the delegate is asked on every access");

        }

        [Test]
        public void ServerCertificateFingerprint_IsNullWithoutADelegate()
        {
            Assert.That(CreateEndpoint().ServerCertificateFingerprint,                                                     Is.Null);
            Assert.That(CreateEndpoint(FingerprintProvider: () => (CertificateFingerprint?) null).ServerCertificateFingerprint, Is.Null);
        }

        #endregion


        #region AddNode(...)

        [Test]
        public void AddNode_ReturnsTheNode_AndRejectsDuplicateIds()
        {

            var endpoint = CreateEndpoint();
            var node     = new HostedNode(RMDescription(), AliasA0);

            Assert.Multiple(() => {
                Assert.That(endpoint.AddNode(node),                                           Is.SameAs(node));
                Assert.That(endpoint.Count,                                                   Is.EqualTo(1));
                Assert.That(() => endpoint.AddNode(node),                                     Throws.ArgumentException, "the same node twice");
                Assert.That(() => endpoint.AddNode(new HostedNode(RMDescription(), AliasB1)), Throws.ArgumentException, "another node with the same identification");
                Assert.That(() => endpoint.AddNode((HostedNode) null!),                       Throws.ArgumentNullException);
                Assert.That(endpoint.Count,                                                   Is.EqualTo(1));
            });

        }

        [Test]
        [S2C("Pairing.NodeIdAlias")]
        public void AddNode_RejectsDuplicateAliases_ButAllowsNodesWithoutAlias()
        {

            var endpoint = CreateEndpoint();

            endpoint.AddNode(new HostedNode(RMDescription(), AliasA0));

            Assert.Multiple(() => {
                Assert.That(() => endpoint.AddNode(new HostedNode(CEMDescription(), AliasA0)), Throws.ArgumentException, "the alias is already used");
                Assert.That(() => endpoint.AddNode(new HostedNode(CEMDescription())),          Throws.Nothing);
                Assert.That(() => endpoint.AddNode(new HostedNode(RMDescription(Node_Id.NewRandom))), Throws.Nothing, "several nodes without alias");
                Assert.That(endpoint.Count,                                                    Is.EqualTo(3));
            });

        }

        [Test]
        public void AddNode_WithADescription_UsesTheTimeProviderOfTheEndpoint()
        {

            var start    = new DateTimeOffset(2026, 9, 4, 10, 0, 0, TimeSpan.Zero);
            var clock    = new FakeTimeProvider(start);
            var endpoint = CreateEndpoint(Clock: clock);

            var node     = endpoint.AddNode(RMDescription(), AliasA0);

            node.IssueDynamicPairingToken();

            Assert.Multiple(() => {
                Assert.That(endpoint.TimeProvider,         Is.SameAs(clock));
                Assert.That(node.Alias,                    Is.EqualTo(AliasA0));
                Assert.That(node.PairingTokenExpiresAt,    Is.EqualTo(start + S2ConnectDefaults.DynamicPairingTokenLifetime));
                Assert.That(node.HasValidPairingToken,     Is.True);
                Assert.That(endpoint.TryGetNode(RMNodeId, out var found), Is.True);
                Assert.That(found,                         Is.SameAs(node));
            });

            clock.Advance(S2ConnectDefaults.DynamicPairingTokenLifetime);

            Assert.That(node.HasValidPairingToken, Is.False, "the token expired on the clock of the endpoint");

        }

        [Test]
        public void AddNode_WithADescription_PassesVersionsAndProtocols()
        {

            var endpoint = CreateEndpoint();
            var node     = endpoint.AddNode(RMDescription(),
                                            null,
                                            [ "v1.0.0", "0.0.2-beta" ],
                                            [ CommunicationProtocol.WebSocket ]);

            Assert.Multiple(() => {
                Assert.That(node.Alias,                           Is.Null);
                Assert.That(node.SupportedS2MessageVersions,      Is.EqualTo(new[] { "v1.0.0", "0.0.2-beta" }));
                Assert.That(node.SupportedCommunicationProtocols, Is.EqualTo(new[] { CommunicationProtocol.WebSocket }));
            });

        }

        [Test]
        public void TimeProvider_DefaultsToTheSystemClock()
        {
            Assert.That(CreateEndpoint().TimeProvider, Is.SameAs(TimeProvider.System));
        }

        #endregion

        #region TryGetNode(...) / TryGetNodeByAlias(...) / TryGetSingleNode(...)

        [Test]
        public void TryGetNode_ById()
        {

            var endpoint = CreateEndpoint();
            var node     = endpoint.AddNode(new HostedNode(RMDescription(), AliasA0));

            Assert.Multiple(() => {
                Assert.That(endpoint.TryGetNode(RMNodeId,  out var found), Is.True);
                Assert.That(found,                                         Is.SameAs(node));
                Assert.That(endpoint.TryGetNode(CEMNodeId, out var none),  Is.False);
                Assert.That(none,                                          Is.Null);
            });

        }

        [Test]
        [S2C("Pairing.NodeIdAlias")]
        public void TryGetNodeByAlias_IsCaseSensitive()
        {

            var endpoint = CreateEndpoint();
            var node     = endpoint.AddNode(new HostedNode(RMDescription(), AliasA0));

            Assert.Multiple(() => {
                Assert.That(endpoint.TryGetNodeByAlias(AliasA0,                  out var found), Is.True);
                Assert.That(found,                                                              Is.SameAs(node));
                Assert.That(endpoint.TryGetNodeByAlias(AliasB1,                  out var none),  Is.False);
                Assert.That(none,                                                               Is.Null);
                Assert.That(endpoint.TryGetNodeByAlias(NodeIdAlias.Parse("a0"), out _),         Is.False);
            });

        }

        [Test]
        [S2C("Pairing.1.SingleNode")]
        public void TryGetSingleNode_OnlyWithExactlyOneNode()
        {

            var endpoint = CreateEndpoint();

            Assert.That(endpoint.TryGetSingleNode(out var none), Is.False, "no node");
            Assert.That(none,                                    Is.Null);

            var node = endpoint.AddNode(new HostedNode(RMDescription()));

            Assert.That(endpoint.TryGetSingleNode(out var single), Is.True, "exactly one node");
            Assert.That(single,                                    Is.SameAs(node));

            endpoint.AddNode(new HostedNode(CEMDescription()));

            Assert.That(endpoint.TryGetSingleNode(out var two), Is.False, "two nodes");
            Assert.That(two,                                    Is.Null);

        }

        #endregion

        #region RemoveNode(...), Nodes and Count

        [Test]
        public void RemoveNode_ReportsWhetherTheNodeWasHosted()
        {

            var endpoint = CreateEndpoint();

            endpoint.AddNode(new HostedNode(RMDescription(), AliasA0));
            endpoint.AddNode(new HostedNode(CEMDescription()));

            Assert.Multiple(() => {
                Assert.That(endpoint.RemoveNode(RMNodeId),                      Is.True);
                Assert.That(endpoint.RemoveNode(RMNodeId),                      Is.False, "already removed");
                Assert.That(endpoint.Count,                                     Is.EqualTo(1));
                Assert.That(endpoint.TryGetNode(RMNodeId, out _),               Is.False);
                Assert.That(endpoint.TryGetNodeByAlias(AliasA0, out _),         Is.False);
                Assert.That(endpoint.TryGetNode(CEMNodeId, out _),              Is.True);
                Assert.That(() => endpoint.AddNode(new HostedNode(RMDescription(), AliasA0)), Throws.Nothing, "identification and alias are free again");
            });

        }

        [Test]
        public void Nodes_IsASnapshot_AndCountFollowsTheNodes()
        {

            var endpoint = CreateEndpoint();

            Assert.That(endpoint.Nodes, Is.Empty);
            Assert.That(endpoint.Count, Is.EqualTo(0));

            var rm       = endpoint.AddNode(new HostedNode(RMDescription()));
            var snapshot = endpoint.Nodes;

            var cem      = endpoint.AddNode(new HostedNode(CEMDescription()));

            Assert.Multiple(() => {
                Assert.That(snapshot,       Has.Count.EqualTo(1), "the earlier snapshot is not updated");
                Assert.That(snapshot,       Is.EqualTo(new[] { rm }));
                Assert.That(endpoint.Nodes, Is.EquivalentTo(new[] { rm, cem }));
                Assert.That(endpoint.Count, Is.EqualTo(2));
            });

        }

        #endregion

        #region ToString()

        [Test]
        public void ToString_MentionsDeploymentAndNodeCount()
        {

            var endpoint = CreateEndpoint(Deployment.WAN, new EndpointDescription("ACME cloud"));
            endpoint.AddNode(new HostedNode(CEMDescription()));

            var text = endpoint.ToString();

            Assert.Multiple(() => {
                Assert.That(text, Does.Contain("ACME cloud"));
                Assert.That(text, Does.Contain("WAN"));
                Assert.That(text, Does.Contain(PairingUrl.Value));
                Assert.That(text, Does.Contain("1 node(s)"));
            });

        }

        #endregion

    }

}

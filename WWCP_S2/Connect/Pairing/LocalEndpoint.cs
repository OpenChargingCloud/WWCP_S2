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

using System.Diagnostics.CodeAnalysis;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// A delegate returning the SHA-256 fingerprint of the TLS server (leaf) certificate
    /// currently used by the HTTPS server of the endpoint, or null when unknown.
    /// </summary>
    public delegate CertificateFingerprint? ServerCertificateFingerprintDelegate();


    /// <summary>
    /// The local endpoint: the application hosting one or more nodes (S2 Connect 1.0.0,
    /// "The node and the endpoint") with its description, deployment, pairing URL, session
    /// initiation URL and the nodes it represents. Shared by the pairing server, the pairing
    /// client and the session initiation procedures. All members are thread-safe.
    /// </summary>
    public sealed class LocalEndpoint
    {

        #region Data

        private readonly Lock                                       lockObject = new ();
        private readonly Dictionary<Node_Id, HostedNode>            nodes      = [];
        private readonly ServerCertificateFingerprintDelegate?      serverCertificateFingerprint;
        private readonly ServerCertificateFingerprintDelegate?      caCertificateFingerprint;

        #endregion

        #region Properties

        /// <summary>
        /// The description of this endpoint (always including its deployment).
        /// </summary>
        public EndpointDescription  Description                          { get; }

        /// <summary>
        /// The deployment of this endpoint: WAN or LAN.
        /// </summary>
        public Deployment           Deployment                           { get; }

        /// <summary>
        /// The pairing URL of this endpoint (base URL of its pairing API).
        /// </summary>
        public S2BaseURL            PairingUrl                           { get; }

        /// <summary>
        /// The optional base URL of the session initiation API of this endpoint; required
        /// whenever one of its nodes becomes a communication server.
        /// </summary>
        public S2BaseURL?           SessionInitiationUrl                 { get; }

        /// <summary>
        /// Whether this is a WAN pairing server acting for a LAN endpoint (S2 Connect 1.0.0,
        /// "Pairing details for different deployments": an OEM backend pairing on behalf of a
        /// LAN device). Such a server uses the WAN challenge-response formula and accepts
        /// WAN clients only.
        /// </summary>
        public Boolean              IsWANPairingServerForLANEndpoint     { get; }

        /// <summary>
        /// The normalised domain name of the pairing URL: the "D" of the WAN
        /// challenge-response formula (S2 Connect 1.0.0, "Challenge response process").
        /// </summary>
        public String               DomainName                           { get; }

        /// <summary>
        /// The SHA-256 fingerprint of the TLS server (leaf) certificate currently used by
        /// the HTTPS server: the "F" of the LAN challenge-response formula; null when unknown.
        /// </summary>
        public CertificateFingerprint?  ServerCertificateFingerprint
            => serverCertificateFingerprint?.Invoke();

        /// <summary>
        /// The SHA-256 fingerprint of the CA (root) certificate that signed the TLS server
        /// certificate of this endpoint: sent via postConnectionDetails when a node of this
        /// endpoint becomes the communication server (S2 Connect 1.0.0, "6B. POST
        /// /[version]/postConnectionDetails"); null when unknown.
        /// </summary>
        public CertificateFingerprint?  CACertificateFingerprint
            => caCertificateFingerprint?.Invoke();

        /// <summary>
        /// The time provider shared with the hosted nodes.
        /// </summary>
        public TimeProvider         TimeProvider                         { get; }

        /// <summary>
        /// A snapshot of the hosted nodes.
        /// </summary>
        public IReadOnlyList<HostedNode>  Nodes
        {
            get
            {
                lock (lockObject)
                {
                    return [.. nodes.Values];
                }
            }
        }

        /// <summary>
        /// The number of hosted nodes.
        /// </summary>
        public Int32                Count
        {
            get
            {
                lock (lockObject)
                {
                    return nodes.Count;
                }
            }
        }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new local endpoint.
        /// </summary>
        /// <param name="Description">The description of the endpoint; its deployment, when given, must equal the deployment.</param>
        /// <param name="Deployment">The deployment of the endpoint: WAN or LAN.</param>
        /// <param name="PairingUrl">The pairing URL of the endpoint.</param>
        /// <param name="SessionInitiationUrl">The optional base URL of the session initiation API.</param>
        /// <param name="IsWANPairingServerForLANEndpoint">Whether this is a WAN pairing server acting for a LAN endpoint.</param>
        /// <param name="ServerCertificateFingerprint">An optional provider of the fingerprint of the TLS server certificate (required for LAN pairing servers).</param>
        /// <param name="CACertificateFingerprint">An optional provider of the fingerprint of the CA (root) certificate (required when a node becomes the communication server of a pairing client).</param>
        /// <param name="TimeProvider">An optional time provider (default: the system clock).</param>
        public LocalEndpoint(EndpointDescription                    Description,
                             Deployment                             Deployment,
                             S2BaseURL                              PairingUrl,
                             S2BaseURL?                             SessionInitiationUrl               = null,
                             Boolean                                IsWANPairingServerForLANEndpoint   = false,
                             ServerCertificateFingerprintDelegate?  ServerCertificateFingerprint       = null,
                             ServerCertificateFingerprintDelegate?  CACertificateFingerprint           = null,
                             TimeProvider?                          TimeProvider                       = null)
        {

            ArgumentNullException.ThrowIfNull(Description);

            if (Deployment != Deployment.WAN && Deployment != Deployment.LAN)
                throw new ArgumentException($"The deployment must be WAN or LAN, not '{Deployment}'!", nameof(Deployment));

            if (Description.Deployment.HasValue && Description.Deployment.Value != Deployment)
                throw new ArgumentException($"The deployment of the endpoint description ('{Description.Deployment}') differs from the given deployment ('{Deployment}')!", nameof(Description));

            if (String.IsNullOrEmpty(PairingUrl.Value))
                throw new ArgumentException("The pairing URL must not be empty!", nameof(PairingUrl));

            if (IsWANPairingServerForLANEndpoint && Deployment != Deployment.LAN)
                throw new ArgumentException("Only a LAN endpoint can be represented by a WAN pairing server!", nameof(IsWANPairingServerForLANEndpoint));

            this.Description                       = Description.Deployment.HasValue
                                                         ? Description
                                                         : new EndpointDescription(Description.Name,
                                                                                   Description.LogoUrl,
                                                                                   Deployment);
            this.Deployment                        = Deployment;
            this.PairingUrl                        = PairingUrl;
            this.SessionInitiationUrl              = SessionInitiationUrl;
            this.IsWANPairingServerForLANEndpoint  = IsWANPairingServerForLANEndpoint;
            this.serverCertificateFingerprint      = ServerCertificateFingerprint;
            this.caCertificateFingerprint          = CACertificateFingerprint;
            this.DomainName                        = ChallengeResponse.NormaliseDomainName(PairingUrl.Host);
            this.TimeProvider                      = TimeProvider ?? System.TimeProvider.System;

        }

        #endregion


        #region AddNode(Node)

        /// <summary>
        /// Add a hosted node; its identification and its alias (when given) must be unique
        /// within this endpoint.
        /// </summary>
        /// <param name="Node">The hosted node.</param>
        /// <returns>The given node.</returns>
        public HostedNode AddNode(HostedNode Node)
        {

            ArgumentNullException.ThrowIfNull(Node);

            lock (lockObject)
            {

                if (nodes.ContainsKey(Node.Id))
                    throw new ArgumentException($"A node with identification '{Node.Id}' is already hosted by this endpoint!", nameof(Node));

                if (Node.Alias.HasValue && nodes.Values.Any(node => node.Alias.HasValue && node.Alias.Value.Equals(Node.Alias.Value)))
                    throw new ArgumentException($"A node with alias '{Node.Alias}' is already hosted by this endpoint!", nameof(Node));

                nodes.Add(Node.Id, Node);

            }

            return Node;

        }

        #endregion

        #region AddNode(Description, Alias = null, SupportedS2MessageVersions = null, SupportedCommunicationProtocols = null)

        /// <summary>
        /// Create and add a hosted node using the time provider of this endpoint.
        /// </summary>
        /// <param name="Description">The description of the node.</param>
        /// <param name="Alias">An optional node ID alias (unique within the endpoint).</param>
        /// <param name="SupportedS2MessageVersions">The S2 JSON message versions the node supports (default: v1.0.0).</param>
        /// <param name="SupportedCommunicationProtocols">The communication protocols the node supports (default: WebSocket).</param>
        /// <returns>The new hosted node.</returns>
        public HostedNode AddNode(NodeDescription                      Description,
                                  NodeIdAlias?                         Alias                             = null,
                                  IEnumerable<String>?                 SupportedS2MessageVersions        = null,
                                  IEnumerable<CommunicationProtocol>?  SupportedCommunicationProtocols   = null)

            => AddNode(new HostedNode(Description,
                                      Alias,
                                      SupportedS2MessageVersions,
                                      SupportedCommunicationProtocols,
                                      TimeProvider));

        #endregion

        #region RemoveNode(NodeId)

        /// <summary>
        /// Remove the hosted node with the given identification.
        /// </summary>
        /// <param name="NodeId">The identification of the node.</param>
        /// <returns>Whether such a node was hosted.</returns>
        public Boolean RemoveNode(Node_Id NodeId)
        {
            lock (lockObject)
            {
                return nodes.Remove(NodeId);
            }
        }

        #endregion

        #region TryGetNode(NodeId, out Node)

        /// <summary>
        /// Try to get the hosted node with the given identification.
        /// </summary>
        /// <param name="NodeId">The identification of the node.</param>
        /// <param name="Node">The hosted node.</param>
        public Boolean TryGetNode(Node_Id                              NodeId,
                                  [NotNullWhen(true)] out HostedNode?  Node)
        {
            lock (lockObject)
            {
                return nodes.TryGetValue(NodeId, out Node);
            }
        }

        #endregion

        #region TryGetNodeByAlias(Alias, out Node)

        /// <summary>
        /// Try to get the hosted node with the given node ID alias.
        /// </summary>
        /// <param name="Alias">The node ID alias.</param>
        /// <param name="Node">The hosted node.</param>
        public Boolean TryGetNodeByAlias(NodeIdAlias                          Alias,
                                         [NotNullWhen(true)] out HostedNode?  Node)
        {

            lock (lockObject)
            {
                Node = nodes.Values.FirstOrDefault(node => node.Alias.HasValue && node.Alias.Value.Equals(Alias));
            }

            return Node is not null;

        }

        #endregion

        #region TryGetSingleNode(out Node)

        /// <summary>
        /// Try to get the only hosted node (S2 Connect 1.0.0, requestPairing: "If no nodeIdAlias
        /// provided, does this endpoint indeed only represent one node?").
        /// </summary>
        /// <param name="Node">The only hosted node.</param>
        public Boolean TryGetSingleNode([NotNullWhen(true)] out HostedNode? Node)
        {

            lock (lockObject)
            {
                Node = nodes.Count == 1
                           ? nodes.Values.First()
                           : null;
            }

            return Node is not null;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Description.Name ?? "endpoint"} ({Deployment}, {PairingUrl.Value}, {Count} node(s))";

        #endregion

    }

}

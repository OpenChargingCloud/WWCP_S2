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
    /// A pairing client (with its own local endpoint, nodes and store) talking to a
    /// <see cref="PairingServerFixture"/> over plain HTTP, for the Phase 7 tests. The client
    /// assumes the fake server certificate fingerprint of the server fixture, so the LAN
    /// challenge-response formula works without TLS.
    /// </summary>
    internal sealed class PairingClientFixture : IAsyncDisposable
    {

        #region Data

        /// <summary>
        /// The fake fingerprint of the (non-existent) CA certificate of the client endpoint,
        /// sent via postConnectionDetails when the client becomes the communication server.
        /// </summary>
        public static readonly CertificateFingerprint  CAFingerprint
            = CertificateFingerprint.Parse("CA:FE:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD");

        #endregion

        #region Properties

        public PairingServerFixture  Server      { get; }
        public LocalEndpoint         Endpoint    { get; }
        public InMemoryS2Store       Store       { get; }
        public PairingClient         Client      { get; }

        /// <summary>
        /// The results of every pairing attempt reported by the client's OnPairingCompleted event.
        /// </summary>
        public List<PairingClientResult>  Results  { get; } = [];

        #endregion

        #region Constructor(s)

        private PairingClientFixture(PairingServerFixture  Server,
                                     LocalEndpoint         Endpoint,
                                     InMemoryS2Store       Store,
                                     PairingClient         Client)
        {

            this.Server    = Server;
            this.Endpoint  = Endpoint;
            this.Store     = Store;
            this.Client    = Client;

            Client.OnPairingCompleted += (timestamp, sender, node, result) => {
                                             lock (Results)
                                                 Results.Add(result);
                                             return Task.CompletedTask;
                                         };

        }

        #endregion


        #region (static) Create(Server, ...)

        /// <summary>
        /// Create a pairing client for the given server fixture.
        /// </summary>
        /// <param name="Server">The server fixture.</param>
        /// <param name="ClientDeployment">The deployment of the client endpoint (default: LAN).</param>
        /// <param name="Options">Optional client options (default: insecure URLs allowed).</param>
        /// <param name="WithSessionInitiationUrl">Whether the client endpoint has a session initiation URL (default: true).</param>
        /// <param name="WithCAFingerprint">Whether the client endpoint knows its CA certificate fingerprint (default: true).</param>
        /// <param name="PairingUrl">An optional pairing URL to use instead of the server's (e.g. a wrong port).</param>
        /// <param name="PairingServerDeployment">The deployment assumed for the pairing server (default: derived from the server's challenge-response formula).</param>
        /// <param name="AssumedFingerprint">The server certificate fingerprint assumed by the client (default: the fake fingerprint of the server fixture).</param>
        /// <param name="WithAssumedFingerprint">Whether the client assumes a server certificate fingerprint at all (default: true).</param>
        public static PairingClientFixture Create(PairingServerFixture     Server,
                                                  Deployment?              ClientDeployment           = null,
                                                  PairingClientOptions?    Options                    = null,
                                                  Boolean                  WithSessionInitiationUrl   = true,
                                                  Boolean                  WithCAFingerprint          = true,
                                                  S2BaseURL?               PairingUrl                 = null,
                                                  Deployment?              PairingServerDeployment    = null,
                                                  CertificateFingerprint?  AssumedFingerprint         = null,
                                                  Boolean                  WithAssumedFingerprint     = true)
        {

            var deployment  = ClientDeployment ?? Deployment.LAN;

            var endpoint    = new LocalEndpoint(
                                  new EndpointDescription($"Test {deployment} client endpoint"),
                                  deployment,
                                  S2BaseURL.Parse("http://127.0.0.1:1/pairing/", AllowHTTP: true),
                                  WithSessionInitiationUrl
                                      ? S2BaseURL.Parse("http://127.0.0.1:1/connection/", AllowHTTP: true)
                                      : null,
                                  CACertificateFingerprint: WithCAFingerprint ? () => CAFingerprint : null
                              );

            var store       = new InMemoryS2Store();

            var client      = new PairingClient(
                                  PairingUrl ?? Server.PairingUrl,
                                  endpoint,
                                  store,
                                  PairingServerDeployment ?? (Server.API.UsesLANChallengeResponse ? Deployment.LAN : Deployment.WAN),
                                  Options ?? DefaultOptions(),
                                  WithAssumedFingerprint
                                      ? AssumedFingerprint ?? PairingServerFixture.Fingerprint
                                      : null
                              );

            return new PairingClientFixture(Server, endpoint, store, client);

        }

        #endregion

        #region (static) DefaultOptions()

        /// <summary>
        /// The default test options: insecure URLs allowed, short retry delay.
        /// </summary>
        public static PairingClientOptions DefaultOptions()

            => new () {
                   ParserOptions                 = new S2ParserOptions { AllowInsecureURLs = true },
                   ServiceUnavailableRetryDelay  = TimeSpan.FromMilliseconds(100)
               };

        #endregion


        #region AddNode(Role, Alias = null, ...)

        /// <summary>
        /// Add a hosted node with a generated identification to the client endpoint.
        /// </summary>
        public HostedNode AddNode(EnergyManagementRole                  Role,
                                  NodeIdAlias?                          Alias                             = null,
                                  IEnumerable<String>?                  SupportedS2MessageVersions        = null,
                                  IEnumerable<CommunicationProtocol>?   SupportedCommunicationProtocols   = null)

            => Endpoint.AddNode(
                   new NodeDescription(
                       Node_Id.NewRandom,
                       "ACME",
                       Role == EnergyManagementRole.CEM ? "EMS" : "heat pump",
                       Role == EnergyManagementRole.CEM ? "ClientCEM" : "ClientRM",
                       Role
                   ),
                   Alias,
                   SupportedS2MessageVersions,
                   SupportedCommunicationProtocols
               );

        #endregion


        #region DisposeAsync()

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
        }

        #endregion

    }

}

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

using Microsoft.Extensions.Logging;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Session;
using cloud.charging.open.protocols.S2.WebSockets;

#endregion

namespace cloud.charging.open.protocols.S2.Node
{

    /// <summary>
    /// The base of an S2 node (PLAN.md Phase 10a): it composes the S2 Connect pairing server and
    /// client, the session initiation server and client and the WebSocket server and client from
    /// the deployment and role, keeps the hosted node, the pairings and the running sessions, and
    /// (with a service discovery) advertises itself and resolves remote endpoints. The stop order
    /// of <see cref="StopAsync"/> is the one the specification requires: withdraw the DNS-SD
    /// advertisement, stop accepting pairing, close the sessions with a close frame, stop the HTTP
    /// and WebSocket servers, flush the store. <see cref="RMNode"/> and <see cref="CEMNode"/> add
    /// the role-specific behaviour.
    /// </summary>
    public abstract class AS2Node : IAsyncDisposable
    {

        #region Data

        private readonly    SemaphoreSlim                             stateLock          = new(1, 1);
        private readonly    List<Action<S2NodeSession>>               sessionConfigurators = [];
        private readonly    Dictionary<Node_Id, S2NodeSession>        sessions           = [];
        private readonly    Dictionary<Node_Id, ReconnectingSessionClient>  reconnectingClients = [];

        private             HTTPServer?                               httpServer;
        private             Boolean                                   ownsHTTPServer;
        private             PairingServerAPI?                         pairingServerAPI;
        private             CommunicationTokenStore?                  tokenStore;
        private             S2WebSocketServer?                        webSocketServer;
        private             SessionInitiationServerAPI?               sessionInitiationServerAPI;
        private             EndpointAdvertiser?                       advertiser;
        private             CancellationTokenSource?                  nodeCTS;
        private             S2NodeState                               state              = S2NodeState.Created;
        private             Boolean                                   isDisposed;

        #endregion

        #region Properties

        /// <summary>
        /// The options of this node.
        /// </summary>
        public S2NodeOptions        Options            { get; }

        /// <summary>
        /// The local endpoint (description, deployment, URLs, hosted node).
        /// </summary>
        public LocalEndpoint        Endpoint           { get; }

        /// <summary>
        /// The persistent security store.
        /// </summary>
        public IS2Store             Store              { get; }

        /// <summary>
        /// The optional service discovery (DNS-SD or in-memory).
        /// </summary>
        public IServiceDiscovery?   ServiceDiscovery   { get; }

        /// <summary>
        /// The certificates this node pinned per domain name (PLAN.md D13): a successful pairing
        /// pins the peer's fingerprints, and every later TLS connection must present one of them.
        /// </summary>
        public CertificatePinStore  PinStore           { get; }

        /// <summary>
        /// The time provider.
        /// </summary>
        public TimeProvider         TimeProvider       { get; }

        /// <summary>
        /// The logger factory, when given.
        /// </summary>
        public ILoggerFactory?      LoggerFactory      { get; }

        /// <summary>
        /// The energy management role of this node.
        /// </summary>
        public abstract EnergyManagementRole  Role     { get; }

        /// <summary>
        /// The single hosted node of this S2 node.
        /// </summary>
        public HostedNode           Node               { get; }

        /// <summary>
        /// The identification of the hosted node.
        /// </summary>
        public Node_Id              NodeId
            => Node.Id;

        /// <summary>
        /// The current lifecycle state.
        /// </summary>
        public S2NodeState          State
        {
            get
            {
                lock (sessions)
                    return state;
            }
        }

        /// <summary>
        /// Whether the node is running.
        /// </summary>
        public Boolean              IsRunning
            => State == S2NodeState.Running;

        /// <summary>
        /// Whether this node runs a session initiation and WebSocket server (communication server).
        /// </summary>
        public Boolean              IsCommunicationServer    { get; }

        /// <summary>
        /// Whether this node opens sessions towards communication servers (communication client).
        /// </summary>
        public Boolean              IsCommunicationClient    { get; }

        /// <summary>
        /// The pairing server API, once started.
        /// </summary>
        public PairingServerAPI?    PairingServer
            => pairingServerAPI;

        /// <summary>
        /// The session initiation server API, once started (communication servers only).
        /// </summary>
        public SessionInitiationServerAPI?  SessionInitiationServer
            => sessionInitiationServerAPI;

        /// <summary>
        /// The WebSocket server, once started (communication servers only).
        /// </summary>
        public S2WebSocketServer?   WebSocketServer
            => webSocketServer;

        /// <summary>
        /// The DNS-SD endpoint advertiser, once started (advertised LAN endpoints only).
        /// </summary>
        public EndpointAdvertiser?  Advertiser
            => advertiser;

        /// <summary>
        /// The HTTP port the node listens on, once started.
        /// </summary>
        public IPPort?              HTTPPort           { get; private set; }

        /// <summary>
        /// The WebSocket port the node listens on, once started (communication servers only).
        /// </summary>
        public IPPort?              WebSocketPort      { get; private set; }

        /// <summary>
        /// The currently running sessions with paired nodes.
        /// </summary>
        public IReadOnlyList<S2NodeSession>  Sessions
        {
            get
            {
                lock (sessions)
                    return [.. sessions.Values];
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// An event fired whenever the state of the node changed.
        /// </summary>
        public event OnS2NodeStateChangedDelegate?    OnStateChanged;

        /// <summary>
        /// An event fired whenever a pairing was completed.
        /// </summary>
        public event OnS2NodePairedDelegate?          OnPaired;

        /// <summary>
        /// An event fired whenever a pairing ended.
        /// </summary>
        public event OnS2NodeUnpairedDelegate?        OnUnpaired;

        /// <summary>
        /// An event fired whenever a session with a paired node started.
        /// </summary>
        public event OnS2NodeSessionStartedDelegate?  OnSessionStarted;

        /// <summary>
        /// An event fired whenever a session with a paired node ended.
        /// </summary>
        public event OnS2NodeSessionEndedDelegate?    OnSessionEnded;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new S2 node.
        /// </summary>
        /// <param name="Node">The hosted node.</param>
        /// <param name="Options">The node options.</param>
        /// <param name="Store">An optional store (default: a new in-memory store).</param>
        /// <param name="ServiceDiscovery">An optional service discovery.</param>
        /// <param name="HTTPServer">An optional existing HTTP server (the node creates a private one when none is given).</param>
        /// <param name="TimeProvider">An optional time provider.</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        protected AS2Node(HostedNode          Node,
                          S2NodeOptions       Options,
                          IS2Store?           Store              = null,
                          IServiceDiscovery?  ServiceDiscovery   = null,
                          HTTPServer?         HTTPServer         = null,
                          TimeProvider?       TimeProvider       = null,
                          ILoggerFactory?     LoggerFactory      = null)
        {

            ArgumentNullException.ThrowIfNull(Node);
            ArgumentNullException.ThrowIfNull(Options);

            Options.Validate();

            if (Node.Role != Role)
                throw new ArgumentException($"The hosted node's role '{Node.Role}' does not match the node role '{Role}'!", nameof(Node));

            this.Options           = Options;
            this.Node              = Node;
            this.Store             = Store ?? new InMemoryS2Store();
            this.ServiceDiscovery  = ServiceDiscovery;
            this.PinStore          = Options.CertificatePins ?? new CertificatePinStore();
            this.httpServer        = HTTPServer;
            this.ownsHTTPServer    = HTTPServer is null;
            this.TimeProvider      = TimeProvider ?? System.TimeProvider.System;

            // Every component of this node logs through this factory, so wrapping it here
            // redacts the secrets of S2 Connect for all of them at once (PLAN.md §3.6).
            this.LoggerFactory     = Options.RedactSecretsInLogs
                                         ? LoggerFactory.WithS2Redaction()
                                         : LoggerFactory;

            this.Logger            = this.LoggerFactory?.CreateLogger(GetType());

            this.Endpoint          = new LocalEndpoint(
                                         Options.Description,
                                         Options.Deployment,
                                         Options.PairingUrl,
                                         Options.SessionInitiationUrl,
                                         Options.IsWANPairingServerForLANEndpoint,
                                         Options.ServerCertificateFingerprint,
                                         Options.CACertificateFingerprint,
                                         this.TimeProvider
                                     );

            this.Endpoint.AddNode(Node);

            // Communication roles: a WAN endpoint or a LAN CEM is the communication server;
            // a LAN RM is the communication client (S2 Connect 1.0.0, "Communication roles").
            var defaultServer      = Options.Deployment == Deployment.WAN || Role == EnergyManagementRole.CEM;

            this.IsCommunicationServer  = Options.EnableCommunicationServer ?? defaultServer;
            this.IsCommunicationClient  = Options.EnableCommunicationClient ?? !defaultServer;

            if (IsCommunicationServer && !Options.SessionInitiationUrl.HasValue)
                throw new ArgumentException("A communication server needs a session initiation URL!", nameof(Options));

        }

        #endregion


        #region Protected helpers for subclasses

        /// <summary>
        /// The logger of this node, when a logger factory was given.
        /// </summary>
        protected ILogger?  Logger    { get; }

        /// <summary>
        /// Register a callback run for every new session, e.g. to add message handlers or a control type.
        /// </summary>
        /// <param name="Configurator">A configurator of a new session.</param>
        protected void ConfigureEverySession(Action<S2NodeSession> Configurator)
        {
            lock (sessionConfigurators)
                sessionConfigurators.Add(Configurator);
        }

        /// <summary>
        /// Called once for every session right after it started, before <see cref="OnSessionStarted"/>.
        /// The base sends nothing; <see cref="RMNode"/> sends its ResourceManagerDetails here.
        /// </summary>
        /// <param name="Session">The started session.</param>
        /// <param name="CancellationToken">A token to cancel the work.</param>
        protected virtual Task OnSessionEstablishedAsync(S2NodeSession      Session,
                                                         CancellationToken  CancellationToken)
            => Task.CompletedTask;

        #endregion


        #region StartAsync(CancellationToken = default)

        /// <summary>
        /// Start the node: the HTTP server and the pairing server, and (for a communication server)
        /// the WebSocket server and the session initiation server, the advertisement and the
        /// reconnecting session clients of the stored pairings.
        /// </summary>
        public async Task StartAsync(CancellationToken CancellationToken = default)
        {

            ObjectDisposedException.ThrowIf(isDisposed, this);

            await stateLock.WaitAsync(CancellationToken).ConfigureAwait(false);

            try
            {

                if (state is S2NodeState.Running or S2NodeState.Starting)
                    return;

                await SetStateAsync(S2NodeState.Starting).ConfigureAwait(false);

                nodeCTS = new CancellationTokenSource();

                #region HTTP server

                var httpPort = Options.HTTPPort ?? Options.PairingUrl.URL.Port ?? (Options.PairingUrl.IsHTTPS ? IPPort.HTTPS : IPPort.HTTP);

                if (httpServer is null)
                {
                    httpServer = new HTTPServer(
                                     Options.BindAddress ?? IPvXAddress.Any,
                                     httpPort,
                                     $"S2 {Role} node",
                                     AutoStart:        false,
                                     // The redacting factory, so that HTTP PDUs logged by the
                                     // server never carry an Authorization header or a token.
                                     LoggerFactory:    LoggerFactory,
                                     MaxHTTPBodySize:  Options.MaxHTTPBodySize
                                 );
                    ownsHTTPServer = true;
                }

                HTTPPort = httpPort;

                #endregion

                #region Pairing server

                pairingServerAPI = new PairingServerAPI(
                                       httpServer,
                                       Endpoint,
                                       Store,
                                       Options:        Options.PairingServer,
                                       SubnetPolicy:   Options.SubnetPolicy,
                                       TimeProvider:   TimeProvider,
                                       LoggerFactory:  LoggerFactory
                                   );

                pairingServerAPI.OnPairingCompleted += OnPairingServerCompletedAsync;

                #endregion

                #region Communication server (WebSocket + session initiation)

                if (IsCommunicationServer)
                {

                    var webSocketPort = Options.WebSocketPort
                                            ?? Options.WebSocketUrl?.Port
                                            ?? IPPort.Parse((UInt16) (httpPort.ToUInt16() + 1));

                    tokenStore       = new CommunicationTokenStore(TimeProvider);

                    webSocketServer  = new S2WebSocketServer(
                                           Options.BindAddress ?? IPv4Address.Any,
                                           webSocketPort,
                                           Role,
                                           TokenStore:             tokenStore,
                                           WebSocketPingEvery:     Options.WebSocketPingInterval,
                                           SessionOptionsFactory:  SessionOptionsFor,
                                           TimeProvider:           TimeProvider,
                                           LoggerFactory:          LoggerFactory
                                       );

                    // An S2 message is a few kilobytes; a larger one closes the connection
                    // with 1009 instead of being buffered.
                    webSocketServer.MaxTextMessageSizeIn   = Options.MaxWebSocketMessageSize;
                    webSocketServer.MaxTextMessageSizeOut  = Options.MaxWebSocketMessageSize;

                    webSocketServer.OnSessionStarted += OnWebSocketSessionStartedAsync;
                    webSocketServer.OnSessionEnded   += OnWebSocketSessionEndedAsync;

                    WebSocketPort = webSocketPort;

                    var webSocketUrl = Options.WebSocketUrl
                                           ?? URL.Parse($"ws://{Options.PairingUrl.Host}:{webSocketPort}/");

                    sessionInitiationServerAPI = new SessionInitiationServerAPI(
                                                     httpServer,
                                                     Endpoint,
                                                     Store,
                                                     tokenStore,
                                                     webSocketUrl,
                                                     Options:        Options.SessionInitiationServer,
                                                     TimeProvider:   TimeProvider,
                                                     LoggerFactory:  LoggerFactory
                                                 );

                    sessionInitiationServerAPI.OnUnpaired += OnServerUnpairedAsync;

                    await webSocketServer.Start().ConfigureAwait(false);

                }

                #endregion

                if (ownsHTTPServer)
                    await httpServer.Start().ConfigureAwait(false);

                #region Service discovery advertisement

                if (ServiceDiscovery is not null &&
                    Options.Deployment == Deployment.LAN &&
                    Options.AdvertiseViaDNSSD &&
                    !Options.IsWANPairingServerForLANEndpoint)
                {

                    if (Options.StartServiceDiscovery && !ServiceDiscovery.IsRunning)
                        await ServiceDiscovery.StartAsync(CancellationToken).ConfigureAwait(false);

                    advertiser = new EndpointAdvertiser(
                                     ServiceDiscovery,
                                     Endpoint,
                                     Options.Advertiser ?? new EndpointAdvertiserOptions {
                                                               LongPollingUrl = Options.LongPollingUrl
                                                           },
                                     TimeProvider,
                                     LoggerFactory
                                 );

                    await advertiser.StartAsync(CancellationToken).ConfigureAwait(false);

                }

                #endregion

                await SetStateAsync(S2NodeState.Running).ConfigureAwait(false);

                #region Resume the sessions of the stored pairings (communication client side)

                if (IsCommunicationClient)
                {
                    foreach (var pairing in await Store.GetPairingsAsync(NodeId, CancellationToken).ConfigureAwait(false))
                    {
                        if (pairing.IsCommunicationClient && pairing.InitiateSessionUrl.HasValue)
                            StartReconnectingClient(pairing);
                    }
                }

                #endregion

            }
            finally
            {
                stateLock.Release();
            }

        }

        #endregion

        #region StopAsync(Drain = null, CancellationToken = default)

        /// <summary>
        /// Stop the node in the order the specification requires: withdraw the advertisement, stop
        /// accepting pairing, close the sessions with a close frame, stop the servers, flush the store.
        /// </summary>
        /// <param name="Drain">How long to wait for the sessions to close (default: the option's drain timeout).</param>
        /// <param name="CancellationToken">A token to cancel the stop.</param>
        public async Task StopAsync(TimeSpan?          Drain               = null,
                                    CancellationToken  CancellationToken   = default)
        {

            await stateLock.WaitAsync(CancellationToken).ConfigureAwait(false);

            try
            {

                if (state is not (S2NodeState.Running or S2NodeState.Starting))
                    return;

                await SetStateAsync(S2NodeState.Stopping).ConfigureAwait(false);

                var drain = Drain ?? Options.StopDrainTimeout;

                #region 1. Withdraw the DNS-SD advertisement

                if (advertiser is not null)
                {
                    await advertiser.DisposeAsync().ConfigureAwait(false);
                    advertiser = null;
                }

                #endregion

                #region 2. Stop accepting pairing and session initiation

                if (pairingServerAPI is not null)
                    await pairingServerAPI.ShutdownAsync().ConfigureAwait(false);

                sessionInitiationServerAPI?.Shutdown();

                #endregion

                #region 3. Close the sessions with a close frame

                ReconnectingSessionClient[]  clients;
                S2NodeSession[]              openSessions;

                lock (sessions)
                {
                    clients      = [.. reconnectingClients.Values];
                    openSessions = [.. sessions.Values];
                    reconnectingClients.Clear();
                }

                foreach (var client in clients)
                {
                    try { await client.StopAsync().ConfigureAwait(false); }
                    catch (Exception e) { Logger?.LogWarning(e, "S2 node {NodeId}: stopping a reconnecting client failed.", NodeId); }
                }

                using (var drainCTS = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken))
                {

                    drainCTS.CancelAfter(drain);

                    foreach (var session in openSessions)
                    {
                        try
                        {
                            await session.Session.CloseAsync(
                                      new S2CloseReason("The S2 node is shutting down.", true),
                                      drainCTS.Token
                                  ).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            Logger?.LogWarning(e, "S2 node {NodeId}: closing a session failed.", NodeId);
                        }
                    }

                }

                #endregion

                #region 4. Stop the HTTP and WebSocket servers

                if (nodeCTS is not null)
                    await nodeCTS.CancelAsync().ConfigureAwait(false);

                if (webSocketServer is not null)
                {
                    try { await webSocketServer.Shutdown("The S2 node is shutting down.").ConfigureAwait(false); }
                    catch (Exception e) { Logger?.LogWarning(e, "S2 node {NodeId}: shutting down the WebSocket server failed.", NodeId); }
                }

                if (pairingServerAPI is not null)
                    await pairingServerAPI.DisposeAsync().ConfigureAwait(false);

                if (ownsHTTPServer && httpServer is not null)
                {
                    try { await httpServer.Stop().ConfigureAwait(false); }
                    catch (Exception e) { Logger?.LogWarning(e, "S2 node {NodeId}: stopping the HTTP server failed.", NodeId); }
                }

                #endregion

                #region 5. Flush the store

                if (Store is IFlushableS2Store flushable)
                    await flushable.FlushAsync(CancellationToken).ConfigureAwait(false);

                #endregion

                nodeCTS?.Dispose();
                nodeCTS                     = null;
                pairingServerAPI            = null;
                sessionInitiationServerAPI  = null;
                webSocketServer             = null;
                tokenStore                  = null;

                lock (sessions)
                    sessions.Clear();

                await SetStateAsync(S2NodeState.Stopped).ConfigureAwait(false);

            }
            finally
            {
                stateLock.Release();
            }

        }

        #endregion


        #region PairAsync (PairingUrl, PairingToken, ...)

        /// <summary>
        /// Pair with a remote endpoint by its pairing URL (this node acts as pairing client). On
        /// success the pairing is stored and, when this node is the communication client, a
        /// reconnecting session client is started.
        /// </summary>
        /// <param name="PairingUrl">The pairing URL of the remote endpoint.</param>
        /// <param name="PairingToken">The pairing token.</param>
        /// <param name="PairingServerDeployment">The deployment of the remote pairing server (default: guessed from the URL).</param>
        /// <param name="Target">An optional pairing target (a specific remote node or alias).</param>
        /// <param name="CancellationToken">A token to cancel the pairing.</param>
        public async Task<PairingClientResult> PairAsync(S2BaseURL          PairingUrl,
                                                         PairingToken       PairingToken,
                                                         Deployment?        PairingServerDeployment   = null,
                                                         PairingTarget?     Target                    = null,
                                                         CancellationToken  CancellationToken         = default)
        {

            if (!IsRunning)
                throw new InvalidOperationException("The S2 node is not running!");

            await using var pairingClient = new PairingClient(
                                                PairingUrl,
                                                Endpoint,
                                                Store,
                                                PairingServerDeployment,
                                                Options.PairingClient,
                                                Options.AssumedRemoteServerCertificateFingerprint,
                                                TimeProvider:   TimeProvider,
                                                LoggerFactory:  LoggerFactory,
                                                DNSClient:      ServiceDiscovery?.DNSClient
                                            );

            // During a pairing an unpinned, self-signed certificate is accepted (trust on first
            // use); afterwards its fingerprint is pinned and enforced.
            if (Options.EnforceCertificatePinning)
                pairingClient.CertificateValidator = new S2CertificateValidator(
                                                         PinStore,
                                                         AcceptUnpinnedForPairing:  true,
                                                         TimeProvider:              TimeProvider
                                                     );

            var result = await pairingClient.PairAsync(Node, PairingToken, Target, CancellationToken: CancellationToken).ConfigureAwait(false);

            if (result.IsSuccess && result.Pairing is not null)
                await OnPairingEstablishedAsync(result.Pairing, [], CancellationToken).ConfigureAwait(false);

            return result;

        }

        #endregion

        #region UnpairAsync (RemoteNodeId, ...)

        /// <summary>
        /// Unpair from the given remote node. A communication client calls the remote /unpair and
        /// deletes the local material; a communication server unpairs locally, tells the peer to
        /// reconnect (which then fails with NoLongerPaired) and closes the session.
        /// </summary>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="CancellationToken">A token to cancel the unpairing.</param>
        public async Task<Boolean> UnpairAsync(Node_Id            RemoteNodeId,
                                               CancellationToken  CancellationToken   = default)
        {

            var pairing = await Store.GetPairingAsync(NodeId, RemoteNodeId, CancellationToken).ConfigureAwait(false);

            await StopSessionWith(RemoteNodeId, "unpairing", CancellationToken).ConfigureAwait(false);

            if (pairing is not null && pairing.IsCommunicationClient && pairing.InitiateSessionUrl.HasValue)
            {

                await using var client = CreateSessionInitiationClient(pairing.InitiateSessionUrl.Value);

                var result = await client.UnpairAsync(NodeId, RemoteNodeId, CancellationToken).ConfigureAwait(false);

                if (pairing is not null)
                    await RaiseUnpairedAsync(pairing, false, CancellationToken).ConfigureAwait(false);

                return result.Outcome is SessionInitiationOutcome.Success or SessionInitiationOutcome.AlreadyUnpaired;

            }

            // Communication server (or an unknown pairing): unpair locally and notify the peer.
            if (sessionInitiationServerAPI is not null)
            {

                var removed = await sessionInitiationServerAPI.UnpairLocallyAsync(NodeId, RemoteNodeId, CancellationToken).ConfigureAwait(false);
                return removed is not null;

            }

            var unpaired = await Store.UnpairAsync(NodeId, RemoteNodeId, TimeProvider.GetUtcNow(), CancellationToken).ConfigureAwait(false);

            if (unpaired is not null)
                await RaiseUnpairedAsync(unpaired, false, CancellationToken).ConfigureAwait(false);

            return unpaired is not null;

        }

        #endregion


        #region (private) Pairing completion

        private Task OnPairingServerCompletedAsync(DateTimeOffset          Timestamp,
                                                   PairingServerAPI        Sender,
                                                   PairingAttempt          Attempt,
                                                   Pairing                 Pairing,
                                                   Pairing?                ReplacedPairing,
                                                   IReadOnlyList<Pairing>  SupersededPairings)

            => OnPairingEstablishedAsync(Pairing, SupersededPairings, nodeCTS?.Token ?? CancellationToken.None);

        private async Task OnPairingEstablishedAsync(Pairing                 Pairing,
                                                     IReadOnlyList<Pairing>  SupersededPairings,
                                                     CancellationToken       CancellationToken)
        {

            // Pin what the peer sent: the certificates of the host this node will connect to.
            if (Pairing.CertificateFingerprints is not null &&
                Pairing.InitiateSessionUrl.HasValue)
            {

                var pinned = PinStore.PinAll(Pairing.InitiateSessionUrl.Value.Host, Pairing.CertificateFingerprints);

                if (pinned > 0)
                    Logger?.LogInformation("S2 node {NodeId}: pinned {Count} certificate(s) for '{Host}'.",
                                           NodeId, pinned, Pairing.InitiateSessionUrl.Value.Host);

            }

            // An RM that pairs with a new CEM drops its earlier CEMs.
            foreach (var superseded in SupersededPairings)
                await StopSessionWith(superseded.RemoteNodeId, "superseded by a new pairing", CancellationToken).ConfigureAwait(false);

            if (IsCommunicationClient && Pairing.IsCommunicationClient && Pairing.InitiateSessionUrl.HasValue)
                StartReconnectingClient(Pairing);

            await OnPaired.InvokeAllAsync(
                      handler => handler(TimeProvider.GetUtcNow(), this, Pairing, SupersededPairings),
                      Logger
                  ).ConfigureAwait(false);

        }

        #endregion

        #region (private) Reconnecting session client (communication client side)

        private void StartReconnectingClient(Pairing Pairing)
        {

            if (!Pairing.InitiateSessionUrl.HasValue)
                return;

            lock (sessions)
            {
                if (reconnectingClients.ContainsKey(Pairing.RemoteNodeId))
                    return;
            }

            var client = CreateSessionInitiationClient(Pairing.InitiateSessionUrl.Value);

            var reconnecting = new ReconnectingSessionClient(
                                   client,
                                   Node,
                                   Pairing.RemoteNodeId,
                                   Options.ReconnectStrategyFactory?.Invoke(),
                                   TimeProvider:   TimeProvider,
                                   LoggerFactory:  LoggerFactory
                               );

            reconnecting.OnSessionStarted += OnReconnectingSessionStartedAsync;
            reconnecting.OnSessionEnded   += OnReconnectingSessionEndedAsync;

            lock (sessions)
                reconnectingClients[Pairing.RemoteNodeId] = reconnecting;

            reconnecting.Start();

        }

        private SessionInitiationClient CreateSessionInitiationClient(S2BaseURL SessionInitiationUrl)
        {

            // The message size limit of the node applies to the sessions it opens as well,
            // unless the session initiation client options state their own.
            var clientOptions = Options.SessionInitiationClient ?? new SessionInitiationClientOptions();

            if (!clientOptions.MaxWebSocketMessageSize.HasValue)
                clientOptions = clientOptions with { MaxWebSocketMessageSize = Options.MaxWebSocketMessageSize };

            var client = new SessionInitiationClient(
                             SessionInitiationUrl,
                             Endpoint,
                             Store,
                             clientOptions,
                             TimeProvider:   TimeProvider,
                             LoggerFactory:  LoggerFactory,
                             DNSClient:      ServiceDiscovery?.DNSClient
                         );

            // After pairing the peer's certificate is pinned, so nothing unpinned is accepted here.
            if (Options.EnforceCertificatePinning)
                client.CertificateValidator = new S2CertificateValidator(
                                                  PinStore,
                                                  AcceptUnpinnedForPairing:  false,
                                                  TimeProvider:              TimeProvider
                                              );

            return client;

        }

        private async Task OnReconnectingSessionStartedAsync(DateTimeOffset             Timestamp,
                                                             ReconnectingSessionClient  Sender,
                                                             S2ConnectSession           ConnectSession)
        {

            var nodeSession = new S2NodeSession(
                                  this,
                                  ConnectSession.Session,
                                  ConnectSession.Pairing,
                                  Node,
                                  Timestamp,
                                  ReconnectingClient: Sender
                              );

            await TrackAndAnnounceSessionAsync(nodeSession).ConfigureAwait(false);

        }

        private Task OnReconnectingSessionEndedAsync(DateTimeOffset             Timestamp,
                                                     ReconnectingSessionClient  Sender,
                                                     S2ConnectSession           ConnectSession,
                                                     S2CloseReason              Reason)

            => RemoveAndAnnounceSessionAsync(Sender.ServerNodeId, ConnectSession.Session, Reason);

        #endregion

        #region (private) WebSocket session (communication server side)

        private S2SessionOptions SessionOptionsFor(WebSocketServerConnection  Connection,
                                                   Object?                    Identity)
        {

            var version = (Identity as S2ConnectSessionIdentity)?.S2MessageVersion ?? Version.S2JSONVersion;

            return new S2SessionOptions {
                       Role               = Role,
                       Mode               = S2SessionMode.S2Connect,
                       NegotiatedVersion  = version,
                       ParserOptions      = Options.ParserOptions
                   };

        }

        private async Task OnWebSocketSessionStartedAsync(DateTimeOffset     Timestamp,
                                                          S2WebSocketServer  Server,
                                                          S2Session          Session,
                                                          Object?            Identity)
        {

            if (Identity is not S2ConnectSessionIdentity identity)
            {
                Logger?.LogWarning("S2 node {NodeId}: a WebSocket session started without an S2 Connect identity.", NodeId);
                return;
            }

            var nodeSession = new S2NodeSession(
                                  this,
                                  Session,
                                  identity.Pairing,
                                  Node,
                                  Timestamp,
                                  Identity: identity
                              );

            await TrackAndAnnounceSessionAsync(nodeSession).ConfigureAwait(false);

        }

        private Task OnWebSocketSessionEndedAsync(DateTimeOffset     Timestamp,
                                                  S2WebSocketServer  Server,
                                                  S2Session          Session,
                                                  S2CloseReason      Reason)
        {

            Node_Id? remoteNodeId = null;

            lock (sessions)
            {
                foreach (var (nodeId, nodeSession) in sessions)
                {
                    if (nodeSession.Session == Session)
                    {
                        remoteNodeId = nodeId;
                        break;
                    }
                }
            }

            return remoteNodeId.HasValue
                       ? RemoveAndAnnounceSessionAsync(remoteNodeId.Value, Session, Reason)
                       : Task.CompletedTask;

        }

        #endregion

        #region (private) Session tracking

        private async Task TrackAndAnnounceSessionAsync(S2NodeSession Session)
        {

            lock (sessions)
                sessions[Session.RemoteNodeId] = Session;

            // Wire the close event once, so a peer-initiated close is reported for both roles.
            Session.Session.OnClosed += (timestamp, session, reason)
                                            => RemoveAndAnnounceSessionAsync(Session.RemoteNodeId, session, reason);

            Action<S2NodeSession>[] configurators;
            lock (sessionConfigurators)
                configurators = [.. sessionConfigurators];

            foreach (var configurator in configurators)
            {
                try { configurator(Session); }
                catch (Exception e) { Logger?.LogWarning(e, "S2 node {NodeId}: a session configurator failed.", NodeId); }
            }

            try
            {
                await OnSessionEstablishedAsync(Session, nodeCTS?.Token ?? CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger?.LogWarning(e, "S2 node {NodeId}: establishing a session failed.", NodeId);
            }

            await OnSessionStarted.InvokeAllAsync(
                      handler => handler(TimeProvider.GetUtcNow(), this, Session),
                      Logger
                  ).ConfigureAwait(false);

        }

        private async Task RemoveAndAnnounceSessionAsync(Node_Id        RemoteNodeId,
                                                         S2Session      Session,
                                                         S2CloseReason  Reason)
        {

            S2NodeSession? removed = null;

            lock (sessions)
            {
                if (sessions.TryGetValue(RemoteNodeId, out var nodeSession) && nodeSession.Session == Session)
                {
                    removed = nodeSession;
                    sessions.Remove(RemoteNodeId);
                }
            }

            if (removed is not null)
                await OnSessionEnded.InvokeAllAsync(
                          handler => handler(TimeProvider.GetUtcNow(), this, removed, Reason),
                          Logger
                      ).ConfigureAwait(false);

        }

        private async Task StopSessionWith(Node_Id            RemoteNodeId,
                                           String             Reason,
                                           CancellationToken  CancellationToken)
        {

            ReconnectingSessionClient?  client   = null;
            S2NodeSession?              session  = null;

            lock (sessions)
            {
                if (reconnectingClients.Remove(RemoteNodeId, out var c))
                    client = c;
                sessions.TryGetValue(RemoteNodeId, out session);
            }

            if (client is not null)
                await client.StopAsync().ConfigureAwait(false);

            if (session is not null)
                await session.Session.CloseAsync(new S2CloseReason(Reason, true), CancellationToken).ConfigureAwait(false);

        }

        #endregion

        #region (private) Server-initiated unpairing

        private async Task OnServerUnpairedAsync(DateTimeOffset              Timestamp,
                                                 SessionInitiationServerAPI  Sender,
                                                 Pairing                     Pairing,
                                                 Boolean                     ByRemoteNode)
        {

            // Tell the peer to reconnect: its re-initiation is answered with NoLongerPaired,
            // and it cleans up locally. Then close the session.
            S2NodeSession? session;
            lock (sessions)
                sessions.TryGetValue(Pairing.RemoteNodeId, out session);

            if (session is not null && !ByRemoteNode)
            {
                try
                {
                    await session.Session.SendAsync(new SessionRequest(SessionRequestType.Reconnect, "unpaired")).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger?.LogDebug(e, "S2 node {NodeId}: sending the reconnect request on unpair failed.", NodeId);
                }
            }

            await StopSessionWith(Pairing.RemoteNodeId, "unpaired", nodeCTS?.Token ?? CancellationToken.None).ConfigureAwait(false);

            await RaiseUnpairedAsync(Pairing, ByRemoteNode, nodeCTS?.Token ?? CancellationToken.None).ConfigureAwait(false);

        }

        private Task RaiseUnpairedAsync(Pairing            Pairing,
                                        Boolean            ByRemoteNode,
                                        CancellationToken  CancellationToken)

            => OnUnpaired.InvokeAllAsync(
                   handler => handler(TimeProvider.GetUtcNow(), this, Pairing, ByRemoteNode),
                   Logger
               );

        #endregion

        #region (private) SetStateAsync(NewState)

        private async Task SetStateAsync(S2NodeState NewState)
        {

            S2NodeState oldState;

            lock (sessions)
            {
                oldState = state;
                state    = NewState;
            }

            if (oldState != NewState)
                await OnStateChanged.InvokeAllAsync(
                          handler => handler(TimeProvider.GetUtcNow(), this, oldState, NewState),
                          Logger
                      ).ConfigureAwait(false);

        }

        #endregion


        #region DisposeAsync()

        /// <summary>
        /// Stop the node and release its resources.
        /// </summary>
        public async ValueTask DisposeAsync()
        {

            if (isDisposed)
                return;

            isDisposed = true;

            await StopAsync().ConfigureAwait(false);

            stateLock.Dispose();

            GC.SuppressFinalize(this);

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"S2 {Role} node {NodeId} ({State}, {Sessions.Count} session(s))";

        #endregion

    }

}

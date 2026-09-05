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

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using IPAddress = System.Net.IPAddress;
using System.Text;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;


#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The S2 Connect pairing server (S2 Connect 1.0.0, "Pairing process"; s2-connect-pairing.yml
    /// v1.0): a Hermod HTTP API mounted at the path of the pairing URL of the local endpoint,
    /// serving the version index, the four operations of the pairing interaction
    /// (requestPairing, requestConnectionDetails, postConnectionDetails, finalizePairing) and,
    /// for LAN endpoints, the LAN-only operations (endpoint, nodes, preparePairing,
    /// cancelPreparePairing, waitForPairing). The protocol logic is exposed as transport
    /// independent methods returning <see cref="PairingServerResult"/>s, so the same server
    /// can be driven in-process without HTTP.
    /// </summary>
    public class PairingServerAPI : HTTPAPI,
                                    IAsyncDisposable
    {

        #region Data

        /// <summary>
        /// The default HTTP service name (the "Server" header).
        /// </summary>
        public new const String  DefaultHTTPServiceName  = "GraphDefined S2 Pairing Server";

        /// <summary>
        /// The version of the pairing API served by this server.
        /// </summary>
        public const String  APIVersion              = Version.S2ConnectAPIVersion;

        private const String RequestConnectionDetailsOperation  = "requestConnectionDetails";
        private const String PostConnectionDetailsOperation     = "postConnectionDetails";
        private const String FinalizePairingOperation           = "finalizePairing";

        private readonly ConcurrentDictionary<String, PairingAttempt>  attempts = new (StringComparer.Ordinal);
        private readonly PairingRateLimiter                            rateLimiter;
        private readonly ILogger?                                      logger;
        private readonly Boolean                                       ownsLongPollingServer;
        private          Boolean                                       isShutdown;

        #endregion

        #region Properties

        /// <summary>
        /// The local endpoint with the nodes this server pairs.
        /// </summary>
        public LocalEndpoint          Endpoint                     { get; }

        /// <summary>
        /// The store the completed pairings are persisted in.
        /// </summary>
        public IS2Store               Store                        { get; }

        /// <summary>
        /// The options of this server.
        /// </summary>
        public PairingServerOptions   Options                      { get; }

        /// <summary>
        /// The policy deciding whether a request for a LAN-only operation comes from the same subnet.
        /// </summary>
        public ISubnetPolicy          SubnetPolicy                 { get; }

        /// <summary>
        /// The per-remote-address request budget of this server, or null when the rate limit
        /// is disabled. Exposed for diagnostics and metrics.
        /// </summary>
        public S2RequestRateLimiter?  RequestRateLimiter           { get; }

        /// <summary>
        /// The long-polling server, when long-polling is enabled.
        /// </summary>
        public LongPollingServer?     LongPollingServer            { get; }

        /// <summary>
        /// The time provider of this server.
        /// </summary>
        public TimeProvider           TimeProvider                 { get; }

        /// <summary>
        /// Whether the LAN-only operations are served (otherwise they answer 404).
        /// </summary>
        public Boolean                LANOperationsEnabled         { get; }

        /// <summary>
        /// Whether long-polling (waitForPairing) is served.
        /// </summary>
        public Boolean                LongPollingEnabled
            => LongPollingServer is not null;

        /// <summary>
        /// Whether the LAN formula R = HMAC(C, T || F) is used for the challenge-response
        /// process (LAN endpoints not represented by a WAN pairing server); otherwise the WAN
        /// formula R = HMAC(C, T || D) applies.
        /// </summary>
        public Boolean                UsesLANChallengeResponse
            => Endpoint.Deployment == Deployment.LAN && !Endpoint.IsWANPairingServerForLANEndpoint;

        /// <summary>
        /// The versions of the pairing API served by this server (the version index).
        /// </summary>
        public IReadOnlyList<String>  SupportedAPIVersions
            => Version.S2ConnectAPIVersions;

        /// <summary>
        /// A snapshot of all remembered pairing attempts (active, completed and failed).
        /// </summary>
        public IReadOnlyList<PairingAttempt>  Attempts
            => [.. attempts.Values];

        /// <summary>
        /// A snapshot of the pairing attempts still in progress.
        /// </summary>
        public IReadOnlyList<PairingAttempt>  ActiveAttempts
            => [.. attempts.Values.Where(attempt => attempt.IsActive)];

        /// <summary>
        /// Whether the server was shut down.
        /// </summary>
        public Boolean                IsShutdown
            => isShutdown;

        #endregion

        #region Events

        /// <summary>
        /// Raised when a pairing attempt was started (the pairingAttemptId was issued).
        /// </summary>
        public event OnPairingAttemptStartedDelegate?    OnPairingAttemptStarted;

        /// <summary>
        /// Raised when a pairing attempt succeeded, failed or timed out (audit record).
        /// </summary>
        public event OnPairingAttemptCompletedDelegate?  OnPairingAttemptCompleted;

        /// <summary>
        /// Raised when a pairing was completed successfully and stored.
        /// </summary>
        public event OnPairingCompletedDelegate?         OnPairingCompleted;

        /// <summary>
        /// Raised when a LAN client sent the prepare pairing signal.
        /// </summary>
        public event OnPreparePairingDelegate?           OnPreparePairing;

        /// <summary>
        /// Raised when a LAN client sent the cancel prepare pairing signal.
        /// </summary>
        public event OnCancelPreparePairingDelegate?     OnCancelPreparePairing;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new pairing server on the given HTTP server.
        /// </summary>
        /// <param name="HTTPServer">The Hermod HTTP server to mount the API on (its port and TLS certificate are shared with the host).</param>
        /// <param name="Endpoint">The local endpoint with the nodes to pair.</param>
        /// <param name="Store">The store for completed pairings.</param>
        /// <param name="RootPath">An optional root path (default: the path of the pairing URL of the endpoint).</param>
        /// <param name="Options">Optional server options.</param>
        /// <param name="SubnetPolicy">An optional subnet policy for the LAN-only operations (default: <see cref="SubnetCheck"/>).</param>
        /// <param name="LongPollingServer">An optional long-polling server (default: a new one when long-polling is enabled).</param>
        /// <param name="TimeProvider">An optional time provider (default: the time provider of the endpoint).</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        /// <param name="HTTPServerName">An optional HTTP server name.</param>
        /// <param name="HTTPServiceName">An optional HTTP service name (the "Server" header).</param>
        /// <param name="DisableLogging">Whether to disable Hermod's HTTP API logging (default: true).</param>
        /// <param name="LoggingPath">An optional logging path for Hermod's HTTP API logging.</param>
        /// <param name="RegisterWithinHTTPServer">Whether to register this API within the HTTP server (default: true).</param>
        public PairingServerAPI(HTTPServer             HTTPServer,
                                LocalEndpoint          Endpoint,
                                IS2Store               Store,
                                HTTPPath?              RootPath                   = null,
                                PairingServerOptions?  Options                    = null,
                                ISubnetPolicy?         SubnetPolicy               = null,
                                LongPollingServer?     LongPollingServer          = null,
                                TimeProvider?          TimeProvider               = null,
                                ILoggerFactory?        LoggerFactory              = null,
                                String?                HTTPServerName             = null,
                                String?                HTTPServiceName            = null,
                                Boolean?               DisableLogging             = null,
                                String?                LoggingPath                = null,
                                Boolean                RegisterWithinHTTPServer   = true)

            : base(HTTPServer,
                   RootPath:                  NormaliseRootPath(RootPath ?? PathOf(Endpoint)),
                   Description:               I18NString.Create(HTTPServiceName ?? DefaultHTTPServiceName),
                   HTTPServerName:            HTTPServerName,
                   HTTPServiceName:           HTTPServiceName ?? DefaultHTTPServiceName,
                   DisableLogging:            DisableLogging  ?? true,
                   LoggingPath:               LoggingPath,
                   RegisterWithinHTTPServer:  RegisterWithinHTTPServer)

        {

            ArgumentNullException.ThrowIfNull(Endpoint);
            ArgumentNullException.ThrowIfNull(Store);

            this.Endpoint              = Endpoint;
            this.Store                 = Store;
            this.Options               = Options ?? PairingServerOptions.Default;
            this.Options.Validate();

            this.TimeProvider          = TimeProvider ?? Endpoint.TimeProvider;
            this.logger                = LoggerFactory?.CreateLogger<PairingServerAPI>();
            this.rateLimiter           = new PairingRateLimiter(this.Options.MaxQueuedPairingAttemptsPerNode);

            this.RequestRateLimiter    = this.Options.EnableRateLimiting
                                             ? new S2RequestRateLimiter(
                                                   "pairing",
                                                   this.Options.RateLimitCapacity,
                                                   this.Options.RateLimitRefillPeriod,
                                                   this.Options.RateLimitMaxSources
                                               )
                                             : null;

            this.LANOperationsEnabled  = this.Options.EnableLANOperations ?? UsesLANChallengeResponse;
            this.SubnetPolicy          = SubnetPolicy ?? new SubnetCheck(TimeProvider: this.TimeProvider);

            var longPollingEnabled     = this.Options.EnableLongPolling ?? this.LANOperationsEnabled;

            if (longPollingEnabled && !this.LANOperationsEnabled)
                throw new ArgumentException("Long-polling requires the LAN-only operations!", nameof(Options));

            if (longPollingEnabled)
            {

                if (LongPollingServer is not null)
                    this.LongPollingServer = LongPollingServer;

                else
                {
                    this.LongPollingServer      = new LongPollingServer(
                                                      this.TimeProvider,
                                                      this.Options.LongPollingTimeout,
                                                      this.Options.MaxHangingLongPollingRequests,
                                                      this.Options.AutoRequestNodeDescriptions,
                                                      this.logger
                                                  );
                    this.ownsLongPollingServer  = true;
                }

            }

            if (UsesLANChallengeResponse && Endpoint.ServerCertificateFingerprint is null)
                logger?.LogWarning("S2 pairing server: LAN endpoint '{Endpoint}' has no server certificate fingerprint yet; pairing attempts will be rejected until one is available.", Endpoint);

            RegisterURLTemplates();

        }

        private static HTTPPath PathOf(LocalEndpoint Endpoint)
        {
            ArgumentNullException.ThrowIfNull(Endpoint);
            return Endpoint.PairingUrl.URL.Path;
        }

        private static HTTPPath NormaliseRootPath(HTTPPath Path)

            => Path.IsNullOrEmpty
                   ? HTTPPath.Root
                   : Path.EndsWith("/")
                         ? Path
                         : HTTPPath.Parse(Path.ToString() + "/");

        #endregion


        #region (private) RegisterURLTemplates()

        private void RegisterURLTemplates()
        {

            // Every handler is wrapped into the per-source rate limit, so that a flood is
            // refused before any parsing, store access or cryptography happens - and so that
            // a new operation cannot be added without its rate limit.

            // GET {pairingUrl}  =>  ["v1"]
            AddHandler(HTTPMethod.GET,  HTTPPath.Root,                                       RateLimited(HandleVersionIndexAsync));

            // Pairing process
            AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/{APIVersion}/requestPairing"),            RateLimited(HandleRequestPairingAsync));
            AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/{APIVersion}/requestConnectionDetails"),  RateLimited(HandleRequestConnectionDetailsAsync));
            AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/{APIVersion}/postConnectionDetails"),     RateLimited(HandlePostConnectionDetailsAsync));
            AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/{APIVersion}/finalizePairing"),           RateLimited(HandleFinalizePairingAsync));

            // LAN-LAN only extensions (WAN endpoints answer 404)
            AddHandler(HTTPMethod.GET,  HTTPPath.Parse($"/{APIVersion}/endpoint"),                  RateLimited(HandleGetEndpointAsync));
            AddHandler(HTTPMethod.GET,  HTTPPath.Parse($"/{APIVersion}/nodes"),                     RateLimited(HandleGetNodesAsync));
            AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/{APIVersion}/preparePairing"),            RateLimited(HandlePreparePairingAsync));
            AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/{APIVersion}/cancelPreparePairing"),      RateLimited(HandleCancelPreparePairingAsync));
            AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/{APIVersion}/waitForPairing"),            RateLimited(HandleWaitForPairingAsync));

        }

        /// <summary>
        /// Wrap an HTTP handler into the per-source rate limit of this server.
        /// </summary>
        /// <param name="Handler">The HTTP handler to wrap.</param>
        private HTTPDelegate RateLimited(HTTPDelegate Handler)

            => async request => {

                   var refusal = CheckRateLimit(request)
                                     // The announced length is refused here, before the handler
                                     // authenticates or parses anything: otherwise an unauthorized
                                     // request would be answered 401 and its oversized body drained
                                     // anyway, which is exactly what the limit exists to prevent.
                                     ?? CheckAnnouncedRequestSize(request);

                   return refusal is not null
                              ? ResultResponse(request, refusal)
                              : await Handler(request).ConfigureAwait(false);

               };

        #endregion


        // Transport independent protocol logic

        #region RequestPairingAsync         (Request, RemoteAddress = null, CancellationToken = default)

        /// <summary>
        /// Step 1 to 3 of the pairing interaction: check the compatibility of the nodes, resolve
        /// the pairing token, answer the client challenge after the mandatory per-node delay and
        /// issue a pairingAttemptId (S2 Connect 1.0.0, "1. POST /[version]/requestPairing" to
        /// "3. Response status 200").
        /// </summary>
        /// <param name="Request">The requestPairing request.</param>
        /// <param name="RemoteAddress">The optional address of the client for audit records.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        public async Task<PairingServerResult<RequestPairingResponse>>

            RequestPairingAsync(RequestPairingRequest  Request,
                                IPAddress?             RemoteAddress       = null,
                                CancellationToken      CancellationToken   = default)

        {

            ArgumentNullException.ThrowIfNull(Request);

            if (isShutdown)
                return PairingServerResult.Wrap<RequestPairingResponse>(PairingServerResult.ServiceUnavailable(null, "the pairing server is shut down"));

            await PurgeAttemptsAsync(TimeProvider.GetUtcNow()).ConfigureAwait(false);

            #region Resolve the targeted node

            HostedNode? node;

            if (Request.Target.NodeId.HasValue)
            {
                if (!Endpoint.TryGetNode(Request.Target.NodeId.Value, out node))
                    return BadRequest<RequestPairingResponse>(PairingResponseError.NodeNotFound, $"unknown nodeId '{Request.Target.NodeId}'");
            }

            else if (Request.Target.NodeIdAlias.HasValue)
            {
                if (!Endpoint.TryGetNodeByAlias(Request.Target.NodeIdAlias.Value, out node))
                    return BadRequest<RequestPairingResponse>(PairingResponseError.NodeNotFound, $"unknown nodeIdAlias '{Request.Target.NodeIdAlias}'");
            }

            else if (!Endpoint.TryGetSingleNode(out node))
                return BadRequest<RequestPairingResponse>(PairingResponseError.NoNodeIdProvided, $"this endpoint represents {Endpoint.Count} nodes, provide a nodeId or a nodeIdAlias");

            #endregion

            #region Duplicate request => replay the earlier response

            var duplicate = attempts.Values.FirstOrDefault(attempt => attempt.IsActive &&
                                                                      attempt.ServerNode.Id == node.Id &&
                                                                      attempt.ClientNodeId  == Request.ClientNodeDescription.Id &&
                                                                      attempt.Request.Equals(Request));

            if (duplicate is not null)
            {
                logger?.LogDebug("S2 pairing server: replaying the requestPairing response of {Attempt}.", duplicate);
                return PairingServerResult.OK<RequestPairingResponse>(duplicate.Response);
            }

            #endregion

            #region Sequential processing per node (brute-force protection)

            var lease = await rateLimiter.TryAcquireAsync(node.Id, CancellationToken).ConfigureAwait(false);

            if (lease is null)
            {
                logger?.LogWarning("S2 pairing server: too many pairing attempts queued for node {NodeId}, answering 503.", node.Id);
                return PairingServerResult.Wrap<RequestPairingResponse>(PairingServerResult.ServiceUnavailable(Options.RequestPairingDelay > TimeSpan.Zero ? Options.RequestPairingDelay : TimeSpan.FromSeconds(1),
                                                                                                                "too many pairing attempts are queued for this node"));
            }

            using (lease)
            {

                var failure = CheckRequestPairing(node,
                                                  Request,
                                                  out var selectedAlgorithm,
                                                  out var token,
                                                  out var serverNodeIsInitiator,
                                                  out var clientDeployment);

                // S2 Connect 1.0.0, "2. Calculate clientHmacChallengeResponse": the server must enforce
                // a mandatory delay of one second before sending its response to the client.
                if (Options.RequestPairingDelay > TimeSpan.Zero)
                    await Task.Delay(Options.RequestPairingDelay, TimeProvider, CancellationToken).ConfigureAwait(false);

                if (failure is not null)
                {
                    logger?.LogInformation("S2 pairing server: rejected requestPairing of client node {ClientNodeId} for node {NodeId}: {Failure}.", Request.ClientNodeDescription.Id, node.Id, failure);
                    return PairingServerResult.Wrap<RequestPairingResponse>(failure);
                }

                if (!TryComputeChallengeResponse(selectedAlgorithm, Request.ClientHmacChallenge, token, out var clientHmacChallengeResponse, out var error))
                {
                    logger?.LogError("S2 pairing server: cannot answer the client challenge: {Error}.", error);
                    return PairingServerResult.Wrap<RequestPairingResponse>(PairingServerResult.ServiceUnavailable(null, error));
                }

                var serverHmacChallenge = TokenGenerator.NewChallenge();

                if (!TryComputeChallengeResponse(selectedAlgorithm, serverHmacChallenge, token, out var expectedServerHmacChallengeResponse, out error))
                    return PairingServerResult.Wrap<RequestPairingResponse>(PairingServerResult.ServiceUnavailable(null, error));

                var serverCommunicationRole  = CommunicationRoleExtensions.Determine(node.Role, Endpoint.Deployment, clientDeployment);
                var now                      = TimeProvider.GetUtcNow();
                var pairingAttemptId         = TokenGenerator.NewPairingAttemptId();

                var response                 = new RequestPairingResponse(
                                                   pairingAttemptId,
                                                   node.Description,
                                                   Endpoint.Description,
                                                   selectedAlgorithm,
                                                   clientHmacChallengeResponse,
                                                   serverHmacChallenge
                                               );

                var attempt                  = new PairingAttempt(
                                                   pairingAttemptId,
                                                   now,
                                                   now + Options.PairingAttemptTimeout,
                                                   node,
                                                   Request,
                                                   response,
                                                   clientDeployment,
                                                   serverNodeIsInitiator,
                                                   serverCommunicationRole,
                                                   token,
                                                   expectedServerHmacChallengeResponse,
                                                   RemoteAddress?.ToString()
                                               );

                attempts[pairingAttemptId.Value] = attempt;

                logger?.LogInformation("S2 pairing server: started {Attempt} (server node is {ServerCommunicationRole}).", attempt, serverCommunicationRole);

                await OnPairingAttemptStarted.InvokeAllAsync(handler => handler(now, this, attempt), logger).ConfigureAwait(false);

                return PairingServerResult.OK<RequestPairingResponse>(response);

            }

            #endregion

        }

        #endregion

        #region (private) CheckRequestPairing(Node, Request, ...)

        /// <summary>
        /// The server checks of step 1 in the order of the specification's table.
        /// </summary>
        private PairingServerResult? CheckRequestPairing(HostedNode                Node,
                                                         RequestPairingRequest     Request,
                                                         out HmacHashingAlgorithm  SelectedAlgorithm,
                                                         out PairingToken          Token,
                                                         out Boolean               ServerNodeIsInitiator,
                                                         out Deployment            ClientDeployment)
        {

            SelectedAlgorithm      = default;
            Token                  = default;
            ServerNodeIsInitiator  = false;
            ClientDeployment       = default;

            // Are the endpoint and node ready for pairing?
            if (!Node.IsReadyForPairing)
                return PairingServerResult.BadRequest(PairingResponseError.Other, Info("the node is not ready for pairing"));

            if (Request.ClientNodeDescription.Id == Node.Id)
                return PairingServerResult.BadRequest(PairingResponseError.Other, Info("a node cannot be paired with itself"));

            // Does the targeted node have a different role than the Initiator node?
            if (Request.ClientNodeDescription.Role == Node.Role)
                return PairingServerResult.BadRequest(PairingResponseError.InvalidCombinationOfRoles, Info($"both nodes have the role {Node.Role}"));

            // Does the server accept any of the provided hashing algorithms?
            var selectedAlgorithm = Options.SupportedHmacHashingAlgorithms.
                                        Where(algorithm => Request.SupportedHmacHashingAlgorithms.Contains(algorithm)).
                                        Cast<HmacHashingAlgorithm?>().
                                        FirstOrDefault();

            if (!selectedAlgorithm.HasValue)
                return PairingServerResult.BadRequest(PairingResponseError.IncompatibleHmacHashingAlgorithms, Info($"the server supports {String.Join(", ", Options.SupportedHmacHashingAlgorithms)}"));

            SelectedAlgorithm = selectedAlgorithm.Value;

            // Is there overlap between the communication protocols? (can be ignored with forcePairing)
            if (!Request.ForcePairing &&
                !Node.SupportedCommunicationProtocols.Any(Request.SupportedCommunicationProtocols.Contains))
            {
                return PairingServerResult.BadRequest(PairingResponseError.IncompatibleCommunicationProtocols, Info($"the node supports {String.Join(", ", Node.SupportedCommunicationProtocols)}"));
            }

            // Is there overlap between the S2 message versions? (can be ignored with forcePairing)
            if (!Request.ForcePairing &&
                !Node.SupportedS2MessageVersions.Any(version => Request.SupportedS2MessageVersions.Contains(version, StringComparer.Ordinal)))
            {
                return PairingServerResult.BadRequest(PairingResponseError.IncompatibleS2MessageVersions, Info($"the node supports {String.Join(", ", Node.SupportedS2MessageVersions)}"));
            }

            // Does the node have a pairing token: entered by the end user (Initiator) or issued and not expired (Responder)?
            if (!Node.TryResolvePairingToken(Request.ClientNodeDescription.Id, out Token, out ServerNodeIsInitiator))
                return PairingServerResult.BadRequest(PairingResponseError.NoValidPairingTokenOnPairingServer, Info("the node has neither an unexpired own pairing token nor an entered pairing token for the client node"));

            // The deployment of the client decides the communication roles.
            var clientDeployment = Request.ClientEndpointDescription.Deployment ?? Options.DefaultClientDeployment;

            if (!clientDeployment.HasValue)
                return PairingServerResult.BadRequest(PairingResponseError.Other, Info("the deployment of the client endpoint is missing"));

            if (clientDeployment.Value != Deployment.LAN && clientDeployment.Value != Deployment.WAN)
                return PairingServerResult.BadRequest(PairingResponseError.Other, Info($"unknown deployment '{clientDeployment}' of the client endpoint"));

            ClientDeployment = clientDeployment.Value;

            // Is this a WAN pairing server for a LAN endpoint, does the client have a WAN deployment?
            if (Endpoint.IsWANPairingServerForLANEndpoint && ClientDeployment != Deployment.WAN)
                return PairingServerResult.BadRequest(PairingResponseError.Other, Info("this WAN pairing server of a LAN endpoint accepts WAN clients only"));

            return null;

        }

        #endregion

        #region RequestConnectionDetailsAsync(Id, Request, RemoteAddress = null, CancellationToken = default)

        /// <summary>
        /// Step 6A to 8A: verify the serverHmacChallengeResponse and hand out the connection
        /// details of this communication server (S2 Connect 1.0.0, "6A. POST
        /// /[version]/requestConnectionDetails" to "8A. Response status 200").
        /// </summary>
        /// <param name="Id">The pairingAttemptId (bearer token).</param>
        /// <param name="Request">The requestConnectionDetails request.</param>
        /// <param name="RemoteAddress">The optional address of the client.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        public async Task<PairingServerResult<ConnectionDetails>>

            RequestConnectionDetailsAsync(PairingAttemptId                 Id,
                                          RequestConnectionDetailsRequest  Request,
                                          IPAddress?                       RemoteAddress       = null,
                                          CancellationToken                CancellationToken   = default)

        {

            ArgumentNullException.ThrowIfNull(Request);

            var now      = TimeProvider.GetUtcNow();
            var attempt  = await FindAttemptAsync(Id, now).ConfigureAwait(false);

            if (attempt is null)
                return PairingServerResult.Wrap<ConnectionDetails>(PairingServerResult.Unauthorized("unknown or expired pairingAttemptId"));

            await attempt.Semaphore.WaitAsync(CancellationToken).ConfigureAwait(false);

            try
            {

                if (attempt.TryGetReplay(RequestConnectionDetailsOperation, Request, out var replay))
                    return replay as PairingServerResult<ConnectionDetails> ?? PairingServerResult.Wrap<ConnectionDetails>(replay);

                if (!attempt.IsActive)
                    return PairingServerResult.Wrap<ConnectionDetails>(PairingServerResult.Unauthorized("the pairing attempt is no longer active"));

                if (attempt.State != PairingAttemptState.AwaitingConnectionDetails || !attempt.ExpectsRequestConnectionDetails)
                {
                    var description = attempt.ExpectsRequestConnectionDetails
                                          ? "requestConnectionDetails was already processed"
                                          : "the pairing server becomes the communication client, postConnectionDetails is expected";
                    await FailAttemptAsync(attempt, PairingFailure.InvalidInteraction, description, now).ConfigureAwait(false);
                    return BadRequest<ConnectionDetails>(PairingResponseError.Other, description);
                }

                if (!Request.ServerHmacChallengeResponse.ConstantTimeEquals(attempt.ExpectedServerHmacChallengeResponse))
                {
                    await FailAttemptAsync(attempt, PairingFailure.InvalidChallengeResponse, "the serverHmacChallengeResponse was not accepted", now).ConfigureAwait(false);
                    return PairingServerResult.Wrap<ConnectionDetails>(PairingServerResult.Forbidden("the serverHmacChallengeResponse was not accepted"));
                }

                if (Endpoint.SessionInitiationUrl is null)
                {
                    await FailAttemptAsync(attempt, PairingFailure.ConnectionDetailsUnavailable, "the endpoint has no session initiation URL", now).ConfigureAwait(false);
                    return BadRequest<ConnectionDetails>(PairingResponseError.Other, "the server is not able to provide connection details");
                }

                var accessToken        = TokenGenerator.NewAccessToken();
                var connectionDetails  = new ConnectionDetails(Endpoint.SessionInitiationUrl.Value, accessToken);

                attempt.MarkConnectionDetailsExchanged(accessToken, null);

                var result = PairingServerResult.OK<ConnectionDetails>(connectionDetails);
                attempt.RecordReplay(RequestConnectionDetailsOperation, Request, result);

                logger?.LogInformation("S2 pairing server: {Attempt}: the client challenge response was accepted, connection details issued.", attempt);

                return result;

            }
            finally
            {
                attempt.Semaphore.Release();
            }

        }

        #endregion

        #region PostConnectionDetailsAsync   (Id, Request, RemoteAddress = null, CancellationToken = default)

        /// <summary>
        /// Step 6B to 8B: verify the serverHmacChallengeResponse and accept the connection
        /// details of the client, which becomes the communication server (S2 Connect 1.0.0,
        /// "6B. POST /[version]/postConnectionDetails" to "8B. Response status 204").
        /// </summary>
        /// <param name="Id">The pairingAttemptId (bearer token).</param>
        /// <param name="Request">The postConnectionDetails request.</param>
        /// <param name="RemoteAddress">The optional address of the client.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        public async Task<PairingServerResult>

            PostConnectionDetailsAsync(PairingAttemptId              Id,
                                       PostConnectionDetailsRequest  Request,
                                       IPAddress?                    RemoteAddress       = null,
                                       CancellationToken             CancellationToken   = default)

        {

            ArgumentNullException.ThrowIfNull(Request);

            var now      = TimeProvider.GetUtcNow();
            var attempt  = await FindAttemptAsync(Id, now).ConfigureAwait(false);

            if (attempt is null)
                return PairingServerResult.Unauthorized("unknown or expired pairingAttemptId");

            await attempt.Semaphore.WaitAsync(CancellationToken).ConfigureAwait(false);

            try
            {

                if (attempt.TryGetReplay(PostConnectionDetailsOperation, Request, out var replay))
                    return replay;

                if (!attempt.IsActive)
                    return PairingServerResult.Unauthorized("the pairing attempt is no longer active");

                if (attempt.State != PairingAttemptState.AwaitingConnectionDetails || attempt.ExpectsRequestConnectionDetails)
                {
                    var description = attempt.ExpectsRequestConnectionDetails
                                          ? "the pairing server becomes the communication server, requestConnectionDetails is expected"
                                          : "postConnectionDetails was already processed";
                    await FailAttemptAsync(attempt, PairingFailure.InvalidInteraction, description, now).ConfigureAwait(false);
                    return PairingServerResult.BadRequest(PairingResponseError.Other, Info(description));
                }

                if (!Request.ServerHmacChallengeResponse.ConstantTimeEquals(attempt.ExpectedServerHmacChallengeResponse))
                {
                    await FailAttemptAsync(attempt, PairingFailure.InvalidChallengeResponse, "the serverHmacChallengeResponse was not accepted", now).ConfigureAwait(false);
                    return PairingServerResult.Forbidden("the serverHmacChallengeResponse was not accepted");
                }

                // "This token ... should have a minimum length of 32 bytes" (S2 Connect 1.0.0,
                // AccessToken). It is a "should", so a shorter token is accepted - but it is the
                // peer's own credential for every later session, so its weakness is worth saying
                // out loud rather than storing silently.
                WarnAboutAWeakAccessToken(Request.ConnectionDetails, attempt);

                attempt.MarkConnectionDetailsExchanged(null, Request.ConnectionDetails);

                var result = PairingServerResult.NoContent();
                attempt.RecordReplay(PostConnectionDetailsOperation, Request, result);

                logger?.LogInformation("S2 pairing server: {Attempt}: the client challenge response was accepted, connection details received.", attempt);

                return result;

            }
            finally
            {
                attempt.Semaphore.Release();
            }

        }

        #endregion

        #region FinalizePairingAsync         (Id, Request, RemoteAddress = null, CancellationToken = default)

        /// <summary>
        /// Step 9 and 10: complete the pairing attempt; with success = true the pairing is
        /// persisted and the issued access token becomes usable (S2 Connect 1.0.0,
        /// "9. POST /[version]/finalizePairing" and "10. Response status 204").
        /// </summary>
        /// <param name="Id">The pairingAttemptId (bearer token).</param>
        /// <param name="Request">The finalizePairing request.</param>
        /// <param name="RemoteAddress">The optional address of the client.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        public async Task<PairingServerResult>

            FinalizePairingAsync(PairingAttemptId        Id,
                                 FinalizePairingRequest  Request,
                                 IPAddress?              RemoteAddress       = null,
                                 CancellationToken       CancellationToken   = default)

        {

            ArgumentNullException.ThrowIfNull(Request);

            var now      = TimeProvider.GetUtcNow();
            var attempt  = await FindAttemptAsync(Id, now).ConfigureAwait(false);

            if (attempt is null)
                return PairingServerResult.Unauthorized("unknown or expired pairingAttemptId");

            await attempt.Semaphore.WaitAsync(CancellationToken).ConfigureAwait(false);

            try
            {

                if (attempt.TryGetReplay(FinalizePairingOperation, Request, out var replay))
                    return replay;

                if (!attempt.IsActive)
                    return PairingServerResult.Unauthorized("the pairing attempt is no longer active");

                if (!Request.Success.HasValue)
                    return PairingServerResult.BadRequest(PairingResponseError.ParsingError, Info("the property 'success' is required"));

                if (!Request.Success.Value)
                {

                    await FailAttemptAsync(attempt, PairingFailure.ClientReportedFailure, "the client finalized the pairing attempt with success = false", now).ConfigureAwait(false);

                    var failed = PairingServerResult.NoContent();
                    attempt.RecordReplay(FinalizePairingOperation, Request, failed);
                    return failed;

                }

                if (attempt.State != PairingAttemptState.AwaitingFinalization)
                {
                    const String description = "finalizePairing with success = true before the connection details were exchanged";
                    await FailAttemptAsync(attempt, PairingFailure.InvalidInteraction, description, now).ConfigureAwait(false);
                    return PairingServerResult.BadRequest(PairingResponseError.Other, Info(description));
                }

                #region Persist the pairing

                var accessToken = attempt.IssuedAccessToken ?? attempt.ReceivedConnectionDetails?.AccessToken;

                if (!accessToken.HasValue)
                {
                    await FailAttemptAsync(attempt, PairingFailure.InvalidInteraction, "no access token was exchanged", now).ConfigureAwait(false);
                    return PairingServerResult.BadRequest(PairingResponseError.Other, Info("no access token was exchanged"));
                }

                var clientEndpointDescription = attempt.ClientEndpointDescription.Deployment.HasValue
                                                    ? attempt.ClientEndpointDescription
                                                    : new EndpointDescription(attempt.ClientEndpointDescription.Name,
                                                                              attempt.ClientEndpointDescription.LogoUrl,
                                                                              attempt.ClientDeployment);

                var pairing = new Pairing(
                                  attempt.ServerNode.Id,
                                  attempt.ClientNodeDescription,
                                  clientEndpointDescription,
                                  attempt.ServerCommunicationRole,
                                  accessToken.Value,
                                  now,
                                  attempt.ReceivedConnectionDetails?.InitiateSessionUrl,
                                  attempt.ReceivedConnectionDetails?.CertificateFingerprints
                              );

                Pairing?                replacedPairing;
                IReadOnlyList<Pairing>  supersededPairings = [];

                try
                {

                    replacedPairing = await Store.AddOrReplacePairingAsync(pairing, CancellationToken).ConfigureAwait(false);

                    // A RM can only be paired with one CEM at a time: the other pairings must be unpaired by the node layer.
                    if (attempt.ServerNode.Role == EnergyManagementRole.RM)
                        supersededPairings = [.. (await Store.GetPairingsAsync(attempt.ServerNode.Id, CancellationToken).ConfigureAwait(false)).
                                                     Where(other => other.RemoteNodeId != pairing.RemoteNodeId)];

                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    logger?.LogError(e, "S2 pairing server: {Attempt}: storing the pairing failed.", attempt);
                    await FailAttemptAsync(attempt, PairingFailure.StoreFailure, e.Message, now).ConfigureAwait(false);
                    return PairingServerResult.InternalServerError("storing the pairing failed");
                }

                #endregion

                attempt.ServerNode.ConsumePairingToken(attempt.ClientNodeId, attempt.Token);
                attempt.MarkSucceeded(pairing, now);

                var result = PairingServerResult.NoContent();
                attempt.RecordReplay(FinalizePairingOperation, Request, result);

                logger?.LogInformation("S2 pairing server: {Attempt} succeeded: {Pairing}.", attempt, pairing);

                await OnPairingAttemptCompleted.InvokeAllAsync(handler => handler(now, this, attempt), logger).ConfigureAwait(false);
                await OnPairingCompleted.       InvokeAllAsync(handler => handler(now, this, attempt, pairing, replacedPairing, supersededPairings), logger).ConfigureAwait(false);

                return result;

            }
            finally
            {
                attempt.Semaphore.Release();
            }

        }

        #endregion


        #region GetEndpoint          (LocalAddress = null, RemoteAddress = null)

        /// <summary>
        /// GET /[version]/endpoint (LAN-LAN only): the description of this endpoint.
        /// </summary>
        /// <param name="LocalAddress">The optional local address the request arrived on (subnet check when both addresses are given).</param>
        /// <param name="RemoteAddress">The optional address of the client.</param>
        public PairingServerResult<EndpointDescription> GetEndpoint(IPAddress?  LocalAddress    = null,
                                                                    IPAddress?  RemoteAddress   = null)
        {

            var failure = CheckLANOperation(LocalAddress, RemoteAddress);

            return failure is not null
                       ? PairingServerResult.Wrap<EndpointDescription>(failure)
                       : PairingServerResult.OK<EndpointDescription>(Endpoint.Description);

        }

        #endregion

        #region GetNodes             (LocalAddress = null, RemoteAddress = null)

        /// <summary>
        /// GET /[version]/nodes (LAN-LAN only): the descriptions of the nodes represented by this endpoint.
        /// </summary>
        /// <param name="LocalAddress">The optional local address the request arrived on (subnet check when both addresses are given).</param>
        /// <param name="RemoteAddress">The optional address of the client.</param>
        public PairingServerResult<IReadOnlyList<NodeDescription>> GetNodes(IPAddress?  LocalAddress    = null,
                                                                            IPAddress?  RemoteAddress   = null)
        {

            var failure = CheckLANOperation(LocalAddress, RemoteAddress);

            return failure is not null
                       ? PairingServerResult.Wrap<IReadOnlyList<NodeDescription>>(failure)
                       : PairingServerResult.OK<IReadOnlyList<NodeDescription>>([.. Endpoint.Nodes.Select(node => node.Description)]);

        }

        #endregion

        #region PreparePairingAsync  (Request, LocalAddress = null, RemoteAddress = null, CancellationToken = default)

        /// <summary>
        /// POST /[version]/preparePairing (LAN-LAN only): a client announces that the end user
        /// is about to pair with one of the hosted nodes, e.g. to show the pairing code
        /// (S2 Connect 1.0.0, "Sending the prepare pairing signal"). An unknown serverNodeId
        /// is answered with 204 as the OpenAPI file prescribes.
        /// </summary>
        /// <param name="Request">The preparePairing request.</param>
        /// <param name="LocalAddress">The optional local address the request arrived on (subnet check when both addresses are given).</param>
        /// <param name="RemoteAddress">The optional address of the client.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        public async Task<PairingServerResult> PreparePairingAsync(PreparePairingRequest  Request,
                                                                   IPAddress?             LocalAddress        = null,
                                                                   IPAddress?             RemoteAddress       = null,
                                                                   CancellationToken      CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Request);

            var failure = CheckLANOperation(LocalAddress, RemoteAddress);
            if (failure is not null)
                return failure;

            var now = TimeProvider.GetUtcNow();

            if (!Endpoint.TryGetNode(Request.ServerNodeId, out var node))
            {
                await OnPreparePairing.InvokeAllAsync(handler => handler(now, this, Request, null, RemoteAddress?.ToString()), logger).ConfigureAwait(false);
                return PairingServerResult.NoContent();
            }

            if (!node.IsReadyForPairing)
                return PairingServerResult.BadRequest(PairingResponseError.Other, Info("the node is not ready for pairing"));

            if (Request.ClientNodeDescription.Role == node.Role)
                return PairingServerResult.BadRequest(PairingResponseError.InvalidCombinationOfRoles, Info($"both nodes have the role {node.Role}"));

            logger?.LogInformation("S2 pairing server: client node {ClientNodeId} prepares pairing with node {NodeId}.", Request.ClientNodeDescription.Id, node.Id);

            await OnPreparePairing.InvokeAllAsync(handler => handler(now, this, Request, node, RemoteAddress?.ToString()), logger).ConfigureAwait(false);

            return PairingServerResult.NoContent();

        }

        #endregion

        #region CancelPreparePairingAsync(Request, LocalAddress = null, RemoteAddress = null, CancellationToken = default)

        /// <summary>
        /// POST /[version]/cancelPreparePairing (LAN-LAN only): the end user no longer intends
        /// to pair (S2 Connect 1.0.0, "Cancelling the prepare pairing signal"); always 204.
        /// </summary>
        /// <param name="Request">The cancelPreparePairing request.</param>
        /// <param name="LocalAddress">The optional local address the request arrived on (subnet check when both addresses are given).</param>
        /// <param name="RemoteAddress">The optional address of the client.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        public async Task<PairingServerResult> CancelPreparePairingAsync(CancelPreparePairingRequest  Request,
                                                                         IPAddress?                   LocalAddress        = null,
                                                                         IPAddress?                   RemoteAddress       = null,
                                                                         CancellationToken            CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Request);

            var failure = CheckLANOperation(LocalAddress, RemoteAddress);
            if (failure is not null)
                return failure;

            var now = TimeProvider.GetUtcNow();

            Endpoint.TryGetNode(Request.ServerNodeId, out var node);

            await OnCancelPreparePairing.InvokeAllAsync(handler => handler(now, this, Request, node, RemoteAddress?.ToString()), logger).ConfigureAwait(false);

            return PairingServerResult.NoContent();

        }

        #endregion

        #region WaitForPairingAsync  (Request, LocalAddress = null, RemoteAddress = null, CancellationToken = default)

        /// <summary>
        /// POST /[version]/waitForPairing (LAN-LAN only): long-polling of constrained endpoints
        /// (S2 Connect 1.0.0, "Long-polling for constrained endpoints in the LAN").
        /// </summary>
        /// <param name="Request">The waitForPairing request.</param>
        /// <param name="LocalAddress">The optional local address the request arrived on (subnet check when both addresses are given).</param>
        /// <param name="RemoteAddress">The optional address of the client.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        public async Task<PairingServerResult<WaitForPairingResponse>> WaitForPairingAsync(WaitForPairingRequest  Request,
                                                                                           IPAddress?             LocalAddress        = null,
                                                                                           IPAddress?             RemoteAddress       = null,
                                                                                           CancellationToken      CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Request);

            var failure = CheckLANOperation(LocalAddress, RemoteAddress);
            if (failure is not null)
                return PairingServerResult.Wrap<WaitForPairingResponse>(failure);

            if (LongPollingServer is null)
                return BadRequest<WaitForPairingResponse>(PairingResponseError.Other, "long-polling is not available at this endpoint");

            if (isShutdown)
                return PairingServerResult.Wrap<WaitForPairingResponse>(PairingServerResult.ServiceUnavailable(TimeSpan.FromSeconds(1), "the pairing server is shut down"));

            var result = await LongPollingServer.WaitAsync(Request, CancellationToken).ConfigureAwait(false);

            return result.Status switch {
                       LongPollingStatus.Actions   => PairingServerResult.OK<WaitForPairingResponse>(result.Response!),
                       LongPollingStatus.NoAction  => PairingServerResult.Wrap<WaitForPairingResponse>(PairingServerResult.NoContent()),
                       _                           => PairingServerResult.Wrap<WaitForPairingResponse>(PairingServerResult.ServiceUnavailable(TimeSpan.FromSeconds(1), "long-polling is temporarily not available"))
                   };

        }

        #endregion


        #region ShutdownAsync()

        /// <summary>
        /// Shut down: fail every active pairing attempt, release every hanging long-polling
        /// request and reject further requests with 503.
        /// </summary>
        public async Task ShutdownAsync()
        {

            if (isShutdown)
                return;

            isShutdown = true;

            if (ownsLongPollingServer)
                LongPollingServer?.Shutdown();

            var now = TimeProvider.GetUtcNow();

            foreach (var attempt in attempts.Values.Where(attempt => attempt.IsActive))
                await FailAttemptAsync(attempt, PairingFailure.Shutdown, "the pairing server was shut down", now).ConfigureAwait(false);

        }

        #endregion

        #region DisposeAsync()

        /// <summary>
        /// Shut down and release the resources of this server.
        /// </summary>
        public async ValueTask DisposeAsync()
        {

            await ShutdownAsync().ConfigureAwait(false);

            foreach (var attempt in attempts.Values)
                attempt.Dispose();

            attempts.Clear();
            rateLimiter.Dispose();

            if (ownsLongPollingServer)
                LongPollingServer?.Dispose();

            GC.SuppressFinalize(this);

        }

        #endregion


        // Helpers of the protocol logic

        #region (private) TryComputeChallengeResponse(Algorithm, Challenge, Token, out Response, out Error)

        private Boolean TryComputeChallengeResponse(HmacHashingAlgorithm                          Algorithm,
                                                    HmacChallenge                                 Challenge,
                                                    PairingToken                                  Token,
                                                    out HmacChallengeResponse                     Response,
                                                    [NotNullWhen(false)] out String?              Error)
        {

            if (UsesLANChallengeResponse)
            {

                var fingerprint = Endpoint.ServerCertificateFingerprint;

                if (!fingerprint.HasValue)
                {
                    Response  = default;
                    Error     = "the fingerprint of the server certificate is not available";
                    return false;
                }

                Response  = ChallengeResponse.ComputeForLAN(Algorithm, Challenge, Token, fingerprint.Value);
                Error     = null;
                return true;

            }

            Response  = ChallengeResponse.ComputeForWAN(Algorithm, Challenge, Token, Endpoint.DomainName);
            Error     = null;
            return true;

        }

        #endregion

        #region (private) FindAttemptAsync(Id, Now)

        /// <summary>
        /// Find the attempt with the given id; an expired attempt is failed with a timeout
        /// (and reported) but still returned, so that duplicate requests can be replayed and
        /// the callers answer 401.
        /// </summary>
        private async Task<PairingAttempt?> FindAttemptAsync(PairingAttemptId  Id,
                                                             DateTimeOffset    Now)
        {

            if (String.IsNullOrEmpty(Id.Value) || !attempts.TryGetValue(Id.Value, out var attempt))
                return null;

            if (attempt.IsActive && attempt.HasExpired(Now))
                await FailAttemptAsync(attempt, PairingFailure.Timeout, "the pairing attempt was not completed within its maximum duration", Now).ConfigureAwait(false);

            return attempt;

        }

        #endregion

        #region (private) FailAttemptAsync(Attempt, Failure, Description, Now)

        private async Task FailAttemptAsync(PairingAttempt  Attempt,
                                            PairingFailure  Failure,
                                            String?         Description,
                                            DateTimeOffset  Now)
        {

            if (!Attempt.MarkFailed(Failure, Description, Now))
                return;

            logger?.LogInformation("S2 pairing server: {Attempt} failed: {Description}.", Attempt, Description);

            await OnPairingAttemptCompleted.InvokeAllAsync(handler => handler(Now, this, Attempt), logger).ConfigureAwait(false);

        }

        #endregion

        #region (private) PurgeAttemptsAsync(Now)

        /// <summary>
        /// Fail every expired attempt and forget completed attempts after the retention time.
        /// </summary>
        private async Task PurgeAttemptsAsync(DateTimeOffset Now)
        {

            foreach (var attempt in attempts.Values)
            {

                if (attempt.IsActive)
                {

                    if (attempt.HasExpired(Now))
                        await FailAttemptAsync(attempt, PairingFailure.Timeout, "the pairing attempt was not completed within its maximum duration", Now).ConfigureAwait(false);

                    continue;

                }

                var completedAt = attempt.CompletedAt ?? attempt.ExpiresAt;

                if (Now >= completedAt + Options.CompletedAttemptRetention &&
                    attempts.TryRemove(attempt.Id.Value, out var removed))
                {
                    removed.Dispose();
                }

            }

        }

        #endregion

        #region (private) CheckLANRequest(Request)

        /// <summary>
        /// The LAN checks of the HTTP handlers, before any body is parsed: 404 when the LAN-only
        /// operations are not served, 401 when the origin of the request cannot be determined
        /// (fail closed) or lies outside the subnet.
        /// </summary>
        private PairingServerResult? CheckLANRequest(HTTPRequest Request)
        {

            if (!LANOperationsEnabled)
                return PairingServerResult.NotFound("the LAN-only operations are not implemented by this endpoint");

            var localAddress   = LocalAddressOf (Request);
            var remoteAddress  = RemoteAddressOf(Request);

            if (localAddress is null || remoteAddress is null)
            {
                logger?.LogWarning("S2 pairing server: rejected a LAN-only request whose origin could not be determined.");
                return PairingServerResult.Unauthorized("the origin of the request could not be determined");
            }

            return CheckLANOperation(localAddress, remoteAddress);

        }

        #endregion

        #region (private) CheckLANOperation(LocalAddress, RemoteAddress)

        private PairingServerResult? CheckLANOperation(IPAddress?  LocalAddress,
                                                       IPAddress?  RemoteAddress)
        {

            if (!LANOperationsEnabled)
                return PairingServerResult.NotFound("the LAN-only operations are not implemented by this endpoint");

            if (LocalAddress is not null &&
                RemoteAddress is not null &&
                !SubnetPolicy.IsSameSubnet(LocalAddress, RemoteAddress))
            {
                logger?.LogInformation("S2 pairing server: rejected a LAN-only request from {RemoteAddress} outside the subnet of {LocalAddress}.", RemoteAddress, LocalAddress);
                return PairingServerResult.Unauthorized("the request originates from outside the LAN");
            }

            return null;

        }

        #endregion

        #region (private) Info(Text) / BadRequest<T>(Error, Info)

        private String? Info(String Text)
            => Options.IncludeErrorDetails ? Text : null;

        private PairingServerResult<T> BadRequest<T>(PairingResponseError  Error,
                                                     String?               AdditionalInfo)
            where T : class

            => PairingServerResult.Wrap<T>(PairingServerResult.BadRequest(Error, Info(AdditionalInfo ?? "")));

        #endregion


        // HTTP handlers

        #region (private) HandleVersionIndexAsync(Request)

        private Task<HTTPResponse> HandleVersionIndexAsync(HTTPRequest Request)

            => Task.FromResult(
                   JSONResponse(Request,
                                HTTPStatusCode.OK,
                                new JArray(SupportedAPIVersions))
               );

        #endregion

        #region (private) HandleRequestPairingAsync(Request)

        private async Task<HTTPResponse> HandleRequestPairingAsync(HTTPRequest Request)
        {

            if (isShutdown)
                return ResultResponse(Request, PairingServerResult.ServiceUnavailable(TimeSpan.FromSeconds(1), "the pairing server is shut down"));

            if (!TryReadJSONObject(Request, out var json, out var failure))
                return ResultResponse(Request, failure);

            if (!RequestPairingRequest.TryParse(json, out var request, out var parseError, Options.ParserOptions))
                return ResultResponse(Request, PairingServerResult.BadRequest(PairingResponseError.ParsingError, Info(parseError)));

            var result = await RequestPairingAsync(request,
                                                   RemoteAddressOf(Request),
                                                   Request.CancellationToken).ConfigureAwait(false);

            return ResultResponse(Request, result, result.Value?.ToJSON());

        }

        #endregion

        #region (private) HandleRequestConnectionDetailsAsync(Request)

        private async Task<HTTPResponse> HandleRequestConnectionDetailsAsync(HTTPRequest Request)
        {

            if (!TryGetPairingAttemptId(Request, out var pairingAttemptId))
                return ResultResponse(Request, PairingServerResult.Unauthorized("missing or malformed pairingAttemptId"));

            // The 401 checks precede the 400 checks (S2 Connect 1.0.0, 6B/9); completed attempts stay
            // known for the replay of identical duplicate requests, expired ones are failed here.
            var attempt = await FindAttemptAsync(pairingAttemptId, TimeProvider.GetUtcNow()).ConfigureAwait(false);

            if (attempt is null || attempt.HasExpired(TimeProvider.GetUtcNow()))
                return ResultResponse(Request, PairingServerResult.Unauthorized("unknown or expired pairingAttemptId"));

            if (!TryReadJSONObject(Request, out var json, out var failure))
                return ResultResponse(Request, failure);

            if (!RequestConnectionDetailsRequest.TryParse(json, out var request, out var parseError, Options.ParserOptions))
                return ResultResponse(Request, PairingServerResult.BadRequest(PairingResponseError.ParsingError, Info(parseError)));

            var result = await RequestConnectionDetailsAsync(pairingAttemptId,
                                                             request,
                                                             RemoteAddressOf(Request),
                                                             Request.CancellationToken).ConfigureAwait(false);

            return ResultResponse(Request, result, result.Value?.ToJSON());

        }

        #endregion

        #region (private) HandlePostConnectionDetailsAsync(Request)

        private async Task<HTTPResponse> HandlePostConnectionDetailsAsync(HTTPRequest Request)
        {

            if (!TryGetPairingAttemptId(Request, out var pairingAttemptId))
                return ResultResponse(Request, PairingServerResult.Unauthorized("missing or malformed pairingAttemptId"));

            // The 401 checks precede the 400 checks (S2 Connect 1.0.0, 6B/9); completed attempts stay
            // known for the replay of identical duplicate requests, expired ones are failed here.
            var attempt = await FindAttemptAsync(pairingAttemptId, TimeProvider.GetUtcNow()).ConfigureAwait(false);

            if (attempt is null || attempt.HasExpired(TimeProvider.GetUtcNow()))
                return ResultResponse(Request, PairingServerResult.Unauthorized("unknown or expired pairingAttemptId"));

            if (!TryReadJSONObject(Request, out var json, out var failure))
                return ResultResponse(Request, failure);

            if (!PostConnectionDetailsRequest.TryParse(json, out var request, out var parseError, Options.ParserOptions))
                return ResultResponse(Request, PairingServerResult.BadRequest(PairingResponseError.ParsingError, Info(parseError)));

            var result = await PostConnectionDetailsAsync(pairingAttemptId,
                                                          request,
                                                          RemoteAddressOf(Request),
                                                          Request.CancellationToken).ConfigureAwait(false);

            return ResultResponse(Request, result);

        }

        #endregion

        #region (private) HandleFinalizePairingAsync(Request)

        private async Task<HTTPResponse> HandleFinalizePairingAsync(HTTPRequest Request)
        {

            if (!TryGetPairingAttemptId(Request, out var pairingAttemptId))
                return ResultResponse(Request, PairingServerResult.Unauthorized("missing or malformed pairingAttemptId"));

            // The 401 checks precede the 400 checks (S2 Connect 1.0.0, 6B/9); completed attempts stay
            // known for the replay of identical duplicate requests, expired ones are failed here.
            var attempt = await FindAttemptAsync(pairingAttemptId, TimeProvider.GetUtcNow()).ConfigureAwait(false);

            if (attempt is null || attempt.HasExpired(TimeProvider.GetUtcNow()))
                return ResultResponse(Request, PairingServerResult.Unauthorized("unknown or expired pairingAttemptId"));

            if (!TryReadJSONObject(Request, out var json, out var failure))
                return ResultResponse(Request, failure);

            if (!FinalizePairingRequest.TryParse(json, out var request, out var parseError, Options.ParserOptions))
                return ResultResponse(Request, PairingServerResult.BadRequest(PairingResponseError.ParsingError, Info(parseError)));

            var result = await FinalizePairingAsync(pairingAttemptId,
                                                    request,
                                                    RemoteAddressOf(Request),
                                                    Request.CancellationToken).ConfigureAwait(false);

            return ResultResponse(Request, result);

        }

        #endregion

        #region (private) HandleGetEndpointAsync(Request)

        private Task<HTTPResponse> HandleGetEndpointAsync(HTTPRequest Request)
        {

            var failure = CheckLANRequest(Request);

            if (failure is not null)
                return Task.FromResult(ResultResponse(Request, failure));

            var result = GetEndpoint();

            return Task.FromResult(ResultResponse(Request, result, result.Value?.ToJSON()));

        }

        #endregion

        #region (private) HandleGetNodesAsync(Request)

        private Task<HTTPResponse> HandleGetNodesAsync(HTTPRequest Request)
        {

            var failure = CheckLANRequest(Request);

            if (failure is not null)
                return Task.FromResult(ResultResponse(Request, failure));

            var result = GetNodes();

            return Task.FromResult(
                       ResultResponse(Request,
                                      result,
                                      result.Value is not null
                                          ? new JArray(result.Value.Select(node => node.ToJSON()))
                                          : null)
                   );

        }

        #endregion

        #region (private) HandlePreparePairingAsync(Request)

        private async Task<HTTPResponse> HandlePreparePairingAsync(HTTPRequest Request)
        {

            var lanFailure = CheckLANRequest(Request);

            if (lanFailure is not null)
                return ResultResponse(Request, lanFailure);

            if (!TryReadJSONObject(Request, out var json, out var failure))
                return ResultResponse(Request, failure);

            if (!PreparePairingRequest.TryParse(json, out var request, out var parseError, Options.ParserOptions))
                return ResultResponse(Request, PairingServerResult.BadRequest(PairingResponseError.ParsingError, Info(parseError)));

            var result = await PreparePairingAsync(request,
                                                   null,
                                                   RemoteAddressOf(Request),
                                                   Request.CancellationToken).ConfigureAwait(false);

            return ResultResponse(Request, result);

        }

        #endregion

        #region (private) HandleCancelPreparePairingAsync(Request)

        private async Task<HTTPResponse> HandleCancelPreparePairingAsync(HTTPRequest Request)
        {

            var lanFailure = CheckLANRequest(Request);

            if (lanFailure is not null)
                return ResultResponse(Request, lanFailure);

            if (!TryReadJSONObject(Request, out var json, out var failure))
                return ResultResponse(Request, failure);

            if (!CancelPreparePairingRequest.TryParse(json, out var request, out var parseError, Options.ParserOptions))
                return ResultResponse(Request, PairingServerResult.BadRequest(PairingResponseError.ParsingError, Info(parseError)));

            var result = await CancelPreparePairingAsync(request,
                                                         null,
                                                         RemoteAddressOf(Request),
                                                         Request.CancellationToken).ConfigureAwait(false);

            return ResultResponse(Request, result);

        }

        #endregion

        #region (private) HandleWaitForPairingAsync(Request)

        private async Task<HTTPResponse> HandleWaitForPairingAsync(HTTPRequest Request)
        {

            var lanFailure = CheckLANRequest(Request);

            if (lanFailure is not null)
                return ResultResponse(Request, lanFailure);

            if (!TryReadJSONArray(Request, out var json, out var failure))
                return ResultResponse(Request, failure);

            if (!WaitForPairingRequest.TryParse(json, out var request, out var parseError, Options.ParserOptions))
                return ResultResponse(Request, PairingServerResult.BadRequest(PairingResponseError.ParsingError, Info(parseError)));

            var result = await WaitForPairingAsync(request,
                                                   null,
                                                   RemoteAddressOf(Request),
                                                   Request.CancellationToken).ConfigureAwait(false);

            return ResultResponse(Request, result, result.Value?.ToJSON());

        }

        #endregion


        // HTTP helpers

        #region (private static) TryGetPairingAttemptId(Request, out Id)

        private static Boolean TryGetPairingAttemptId(HTTPRequest           Request,
                                                      out PairingAttemptId  Id)
        {

            if (Request.Authorization is HTTPBearerAuthentication bearer &&
                PairingAttemptId.TryParse(bearer.Token, out Id))
            {
                return true;
            }

            Id = default;
            return false;

        }

        #endregion

        #region (private) TryReadJSONObject / TryReadJSONArray(Request, out JSON, out Failure)

        private Boolean TryReadJSONObject(HTTPRequest                            Request,
                                          [NotNullWhen(true)]  out JObject?              JSON,
                                          [NotNullWhen(false)] out PairingServerResult?  Failure)
        {

            JSON = null;

            if (!TryReadJSON(Request, out var token, out Failure))
                return false;

            if (token is not JObject jsonObject)
            {
                Failure = BadRequestResult("a JSON object is expected");
                return false;
            }

            JSON = jsonObject;
            return true;

        }

        private Boolean TryReadJSONArray(HTTPRequest                           Request,
                                         [NotNullWhen(true)]  out JArray?              JSON,
                                         [NotNullWhen(false)] out PairingServerResult?  Failure)
        {

            JSON = null;

            if (!TryReadJSON(Request, out var token, out Failure))
                return false;

            if (token is not JArray jsonArray)
            {
                Failure = BadRequestResult("a JSON array is expected");
                return false;
            }

            JSON = jsonArray;
            return true;

        }

        private Boolean TryReadJSON(HTTPRequest                           Request,
                                    [NotNullWhen(true)]  out JToken?              JSON,
                                    [NotNullWhen(false)] out PairingServerResult?  Failure)
        {

            JSON = null;

            if (Request.ContentType is not null &&
                Request.ContentType != HTTPContentType.Application.JSON_UTF8)
            {
                Failure = BadRequestResult($"unsupported content type '{Request.ContentType.MediaType}', 'application/json' is expected");
                return false;
            }

            // The announced length is checked before the body is touched, so that an oversized
            // request costs nothing beyond reading its headers.
            if (Request.ContentLength > (UInt64) Options.MaxRequestBodySize)
            {
                Failure = TooLargeResult();
                return false;
            }

            Byte[]? body;

            try
            {
                body = Request.HTTPBody;
            }
            catch (HTTPBodyTooLargeException)
            {
                // The HTTP server refused the body while reading it (chunked requests announce
                // no length), before this API ever saw it.
                Failure = TooLargeResult();
                return false;
            }
            catch (Exception e)
            {
                Failure = BadRequestResult("the request body could not be read: " + e.Message);
                return false;
            }

            if (body is null || body.Length == 0)
            {
                Failure = BadRequestResult("the request body is empty");
                return false;
            }

            // A chunked request announces no length, so the received body is checked as well.
            if (body.Length > Options.MaxRequestBodySize)
            {
                Failure = TooLargeResult();
                return false;
            }

            try
            {

                using var reader = new JsonTextReader(new StringReader(Encoding.UTF8.GetString(body))) {
                                       DateParseHandling   = DateParseHandling.None,
                                       FloatParseHandling  = FloatParseHandling.Double
                                   };

                JSON = JToken.Load(reader);

                if (reader.Read())
                {
                    JSON     = null;
                    Failure  = BadRequestResult("additional content after the JSON document");
                    return false;
                }

                Failure = null;
                return true;

            }
            catch (Exception e)
            {
                JSON     = null;
                Failure  = BadRequestResult("invalid JSON: " + e.Message);
                return false;
            }

        }

        private PairingServerResult BadRequestResult(String Error)

            => PairingServerResult.BadRequest(
                   PairingResponseError.ParsingError,
                   Info(Error)
               );

        private PairingServerResult TooLargeResult()

            => PairingServerResult.PayloadTooLarge(
                   Options.MaxRequestBodySize,
                   Info($"the request body must not exceed {Options.MaxRequestBodySize} bytes")
               );

        #endregion

        #region (private) WarnAboutAWeakAccessToken(ConnectionDetails, Attempt)

        /// <summary>
        /// Log a warning when a peer announces an access token below the length the
        /// specification recommends ("should have a minimum length of 32 bytes"). The token is
        /// still accepted: the recommendation is not a requirement, and refusing it would break
        /// a pairing over a detail the peer alone controls.
        /// </summary>
        /// <param name="ConnectionDetails">The received connection details.</param>
        /// <param name="Attempt">The pairing attempt they belong to.</param>
        private void WarnAboutAWeakAccessToken(ConnectionDetails  ConnectionDetails,
                                               PairingAttempt     Attempt)
        {

            if (ConnectionDetails.AccessToken.Length < S2ConnectDefaults.MinAccessTokenLength)
                logger?.LogWarning(
                    "S2 pairing server: {Attempt}: the peer announced an access token of {Length} bytes, but the specification recommends at least {Recommended}.",
                    Attempt,
                    ConnectionDetails.AccessToken.Length,
                    S2ConnectDefaults.MinAccessTokenLength
                );

        }

        #endregion

        #region (private) CheckAnnouncedRequestSize(Request)

        /// <summary>
        /// Refuse a request whose announced Content-Length exceeds the configured limit, before
        /// anything reads its body. Returns the refusal, or null when the request may proceed.
        /// </summary>
        /// <param name="Request">An HTTP request.</param>
        private PairingServerResult? CheckAnnouncedRequestSize(HTTPRequest Request)

            => Request.ContentLength > (UInt64) Options.MaxRequestBodySize
                   ? TooLargeResult()
                   : null;

        #endregion

        #region (private) CheckRateLimit(Request)

        /// <summary>
        /// Take one token from the request budget of the remote address of the given request.
        /// Returns the refusal when the budget is exhausted, otherwise null.
        /// </summary>
        /// <param name="Request">An HTTP request.</param>
        private PairingServerResult? CheckRateLimit(HTTPRequest Request)
        {

            if (RequestRateLimiter is null)
                return null;

            var remoteAddress  = RemoteAddressOf(Request);
            var decision       = RequestRateLimiter.TryAcquire(remoteAddress, TimeProvider.GetUtcNow());

            if (decision.Allowed)
                return null;

            logger?.LogWarning(
                "S2 pairing server: the request budget of {RemoteAddress} is exhausted, retry in {RetryAfter} seconds.",
                remoteAddress?.ToString() ?? S2RequestRateLimiter.UnknownAddress,
                Math.Ceiling(decision.RetryAfter.TotalSeconds)
            );

            return Options.UseTooManyRequestsStatusCode
                       ? PairingServerResult.TooManyRequests   (decision.RetryAfter, "the request budget of the remote address is exhausted")
                       : PairingServerResult.ServiceUnavailable(decision.RetryAfter, "the request budget of the remote address is exhausted");

        }

        #endregion

        #region (private static) LocalAddressOf / RemoteAddressOf(Request)

        private static IPAddress? LocalAddressOf(HTTPRequest Request)
            => ToIPAddress(Request.LocalSocket);

        private static IPAddress? RemoteAddressOf(HTTPRequest Request)
            => ToIPAddress(Request.RemoteSocket);

        private static IPAddress? ToIPAddress(IPSocket Socket)
        {

            try
            {

                var address = Socket.IPAddress;

                if (address is null)
                    return null;

                if (address is IPv6Address ipv6 &&
                    !String.IsNullOrEmpty(ipv6.InterfaceId) &&
                    Int64.TryParse(ipv6.InterfaceId, out var scopeId))
                {
                    return new IPAddress(address.GetBytes(), scopeId);
                }

                return address.ToDotNet();

            }
            catch (Exception)
            {
                return null;
            }

        }

        #endregion

        #region (private) ResultResponse(Request, Result, Content = null)

        private HTTPResponse ResultResponse(HTTPRequest          Request,
                                            PairingServerResult  Result,
                                            JToken?              Content   = null)
        {

            var builder = NewResponse(
                              Request,
                              Result.StatusCode,
                              DrainRequestBody: Result.StatusCode != HTTPStatusCode.RequestEntityTooLarge
                          );

            if (Result.StatusCode == HTTPStatusCode.Unauthorized)
                builder.WWWAuthenticate = WWWAuthenticate.Parse("Bearer realm=\"S2 Connect pairing\"");

            if (Result.RetryAfter.HasValue)
                builder.RetryAfter = Math.Max(1, (Int64) Math.Ceiling(Result.RetryAfter.Value.TotalSeconds)).ToString();

            var json = Result.Error?.ToJSON() ?? (Result.IsSuccess ? Content : null);

            if (json is not null)
            {
                builder.ContentType  = HTTPContentType.Application.JSON_UTF8;
                builder.Content      = json.ToString(Formatting.None).ToUTF8Bytes();
            }

            // Hermod omits the Content-Length header when there is no content; without it a client
            // cannot know that a 401/403/404/503 response has no body and waits for the connection
            // to close. 204 responses must not carry a Content-Length (RFC 9110).
            else if (Result.StatusCode != HTTPStatusCode.NoContent)
                builder.Content = [];

            return builder.AsImmutable;

        }

        #endregion

        #region (private) JSONResponse(Request, StatusCode, JSON)

        private HTTPResponse JSONResponse(HTTPRequest     Request,
                                          HTTPStatusCode  StatusCode,
                                          JToken          JSON)
        {

            var builder = NewResponse(Request, StatusCode);

            builder.ContentType  = HTTPContentType.Application.JSON_UTF8;
            builder.Content      = JSON.ToString(Formatting.None).ToUTF8Bytes();

            return builder.AsImmutable;

        }

        #endregion

        #region (private) NewResponse(Request, StatusCode, DrainRequestBody = true)

        private HTTPResponse.Builder NewResponse(HTTPRequest     Request,
                                                 HTTPStatusCode  StatusCode,
                                                 Boolean         DrainRequestBody   = true)
        {

            // An early answer (401, 404, ...) leaves the request body unread; drain it, so that
            // its remains are not mistaken for the next request on a keep-alive connection.
            // An oversized body is the exception: reading it is exactly what the refusal avoids,
            // so that answer closes the connection instead (as Hermod's own 413 does).
            if (DrainRequestBody)
            {
                try
                {
                    Request.TryReadHTTPBodyStream();
                }
                catch (Exception e)
                {
                    logger?.LogDebug(e, "S2 pairing server: could not drain the request body.");
                }
            }

            var builder = new HTTPResponse.Builder(Request) {
                              HTTPStatusCode  = StatusCode,
                              Server          = HTTPServiceName,
                              Date            = org.GraphDefined.Vanaheimr.Illias.Timestamp.Now,
                              Connection      = DrainRequestBody
                                                    ? ConnectionType.KeepAlive
                                                    : ConnectionType.Close
                          };

            // Responses carry secrets (pairingAttemptId, access tokens) and must never be cached.
            builder.SetCacheControl("no-store");

            return builder;

        }

        #endregion

    }

}

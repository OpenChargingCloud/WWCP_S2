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
using System.Text;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.WebSockets;

using IPAddress = System.Net.IPAddress;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The S2 Connect session initiation server of a communication server (S2 Connect 1.0.0,
    /// "Session initiation" and "Unpairing process"; s2-connect-session-init.yml v1.0): a Hermod
    /// HTTP API mounted at the path of the session initiation URL of the local endpoint, serving
    /// the version index, initiateSession (a pending access token per request), confirmAccessToken
    /// (activation of the pending token in one store operation and a single-use communication
    /// token for the WebSocket server) and unpair. The protocol logic is exposed as transport
    /// independent methods returning <see cref="SessionInitiationResult"/>s.
    /// </summary>
    public class SessionInitiationServerAPI : HTTPAPI
    {

        #region Data

        /// <summary>
        /// The default HTTP service name (the "Server" header).
        /// </summary>
        public new const String  DefaultHTTPServiceName  = "GraphDefined S2 Session Initiation Server";

        /// <summary>
        /// The version of the session initiation API served by this server.
        /// </summary>
        public const String  APIVersion              = Version.S2ConnectAPIVersion;

        private readonly ILogger?  logger;
        private          Boolean   isShutdown;

        #endregion

        #region Properties

        /// <summary>
        /// The local endpoint with the nodes acting as communication servers.
        /// </summary>
        public LocalEndpoint                    Endpoint                { get; }

        /// <summary>
        /// The store of the pairings, pending tokens and tombstones.
        /// </summary>
        public IS2Store                         Store                   { get; }

        /// <summary>
        /// The store of the single-use communication tokens shared with the WebSocket server.
        /// </summary>
        public CommunicationTokenStore          TokenStore              { get; }

        /// <summary>
        /// The URL of the WebSocket server sent with the communication details.
        /// </summary>
        public URL                              WebSocketUrl            { get; }

        /// <summary>
        /// The options of this server.
        /// </summary>
        public SessionInitiationServerOptions   Options                 { get; }

        /// <summary>
        /// The time provider of this server.
        /// </summary>
        public TimeProvider                     TimeProvider            { get; }

        /// <summary>
        /// The per-remote-address request budget of this server, or null when the rate limit
        /// is disabled. Exposed for diagnostics and metrics.
        /// </summary>
        public S2RequestRateLimiter?            RequestRateLimiter      { get; }

        /// <summary>
        /// The versions of the session initiation API served by this server (the version index).
        /// </summary>
        public IReadOnlyList<String>            SupportedAPIVersions
            => Version.S2ConnectAPIVersions;

        /// <summary>
        /// Whether the server was shut down.
        /// </summary>
        public Boolean                          IsShutdown
            => isShutdown;

        #endregion

        #region Events

        /// <summary>
        /// Raised when a communication client received a pending access token (step 3).
        /// </summary>
        public event OnSessionInitiatedDelegate?      OnSessionInitiated;

        /// <summary>
        /// Raised when a pending access token was activated and a communication token issued (step 7).
        /// </summary>
        public event OnAccessTokenActivatedDelegate?  OnAccessTokenActivated;

        /// <summary>
        /// Raised when a pairing was removed, by the client or locally.
        /// </summary>
        public event OnUnpairedDelegate?              OnUnpaired;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new session initiation server on the given HTTP server.
        /// </summary>
        /// <param name="HTTPServer">The Hermod HTTP server to mount the API on.</param>
        /// <param name="Endpoint">The local endpoint with the nodes acting as communication servers.</param>
        /// <param name="Store">The store of the pairings.</param>
        /// <param name="TokenStore">The communication token store shared with the WebSocket server.</param>
        /// <param name="WebSocketUrl">The URL of the WebSocket server sent with the communication details.</param>
        /// <param name="RootPath">An optional root path (default: the path of the session initiation URL of the endpoint).</param>
        /// <param name="Options">Optional server options.</param>
        /// <param name="TimeProvider">An optional time provider (default: the time provider of the endpoint).</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        /// <param name="HTTPServerName">An optional HTTP server name.</param>
        /// <param name="HTTPServiceName">An optional HTTP service name (the "Server" header).</param>
        /// <param name="DisableLogging">Whether to disable Hermod's HTTP API logging (default: true).</param>
        /// <param name="LoggingPath">An optional logging path for Hermod's HTTP API logging.</param>
        /// <param name="RegisterWithinHTTPServer">Whether to register this API within the HTTP server (default: true).</param>
        public SessionInitiationServerAPI(HTTPServer                       HTTPServer,
                                          LocalEndpoint                    Endpoint,
                                          IS2Store                         Store,
                                          CommunicationTokenStore          TokenStore,
                                          URL                              WebSocketUrl,
                                          HTTPPath?                        RootPath                   = null,
                                          SessionInitiationServerOptions?  Options                    = null,
                                          TimeProvider?                    TimeProvider               = null,
                                          ILoggerFactory?                  LoggerFactory              = null,
                                          String?                          HTTPServerName             = null,
                                          String?                          HTTPServiceName            = null,
                                          Boolean?                         DisableLogging             = null,
                                          String?                          LoggingPath                = null,
                                          Boolean                          RegisterWithinHTTPServer   = true)

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
            ArgumentNullException.ThrowIfNull(TokenStore);

            this.Endpoint      = Endpoint;
            this.Store         = Store;
            this.TokenStore    = TokenStore;
            this.WebSocketUrl  = WebSocketUrl;
            this.Options       = Options ?? SessionInitiationServerOptions.Default;
            this.Options.Validate();

            this.TimeProvider  = TimeProvider ?? Endpoint.TimeProvider;
            this.logger        = LoggerFactory?.CreateLogger<SessionInitiationServerAPI>();

            this.RequestRateLimiter  = this.Options.EnableRateLimiting
                                           ? new S2RequestRateLimiter(
                                                 "sessionInitiation",
                                                 this.Options.RateLimitCapacity,
                                                 this.Options.RateLimitRefillPeriod,
                                                 this.Options.RateLimitMaxSources
                                             )
                                           : null;

            RegisterURLTemplates();

        }

        private static HTTPPath PathOf(LocalEndpoint Endpoint)
        {

            ArgumentNullException.ThrowIfNull(Endpoint);

            if (Endpoint.SessionInitiationUrl is null)
                throw new ArgumentException("The local endpoint has no session initiation URL; give a root path explicitly!", nameof(Endpoint));

            return Endpoint.SessionInitiationUrl.Value.URL.Path;

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
            // refused before any parsing, store access or cryptography happens.
            AddHandler(HTTPMethod.GET,  HTTPPath.Root,                                        RateLimited(HandleVersionIndexAsync));
            AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/{APIVersion}/initiateSession"),     RateLimited(HandleInitiateSessionAsync));
            AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/{APIVersion}/confirmAccessToken"),  RateLimited(HandleConfirmAccessTokenAsync));
            AddHandler(HTTPMethod.POST, HTTPPath.Parse($"/{APIVersion}/unpair"),              RateLimited(HandleUnpairAsync));

        }

        /// <summary>
        /// Wrap an HTTP handler into the per-source rate limit of this server.
        /// </summary>
        /// <param name="Handler">The HTTP handler to wrap.</param>
        private HTTPDelegate RateLimited(HTTPDelegate Handler)

            => async request => {

                   var refusal = CheckRateLimit(request)
                                     // The announced length is refused here, before the handler
                                     // authenticates or parses anything - and this is also what
                                     // bounds confirmAccessToken, which reads no body at all.
                                     ?? CheckAnnouncedRequestSize(request);

                   return refusal is not null
                              ? ResultResponse(request, refusal)
                              : await Handler(request).ConfigureAwait(false);

               };

        #endregion


        // Transport independent protocol logic

        #region InitiateSessionAsync  (Bearer, Request, RemoteAddress = null, CancellationToken = default)

        /// <summary>
        /// Step 1 to 3: check the request in the order of the specification (tombstone before
        /// the 401 checks), generate a pending access token and select the communication
        /// protocol and the S2 message version (S2 Connect 1.0.0, "1. POST /[version]/initiateSession"
        /// to "3. Response status 200").
        /// </summary>
        /// <param name="Bearer">The access token presented by the client.</param>
        /// <param name="Request">The initiateSession request.</param>
        /// <param name="RemoteAddress">The optional address of the client for logs.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        public async Task<SessionInitiationResult<InitiateSessionResponse>>

            InitiateSessionAsync(AccessToken             Bearer,
                                 InitiateSessionRequest  Request,
                                 IPAddress?              RemoteAddress       = null,
                                 CancellationToken       CancellationToken   = default)

        {

            ArgumentNullException.ThrowIfNull(Request);

            if (isShutdown)
                return SessionInitiationResult.Wrap<InitiateSessionResponse>(SessionInitiationResult.ServiceUnavailable(TimeSpan.FromSeconds(1), "the session initiation server is shut down"));

            var now = TimeProvider.GetUtcNow();

            try
            {

                // "Was this node ID paired with this node, but was it unpaired?" (before the 401 checks)
                if (await Store.GetUnpairedAtAsync(Request.ServerNodeId, Request.ClientNodeId, CancellationToken).ConfigureAwait(false) is { } unpairedAt)
                {
                    logger?.LogInformation("S2 session initiation: {ClientNodeId} is no longer paired with {ServerNodeId} (since {UnpairedAt}).", Request.ClientNodeId, Request.ServerNodeId, unpairedAt);
                    return BadRequest<InitiateSessionResponse>(CommunicationDetailsError.NoLongerPaired, $"unpaired at {unpairedAt.ToS2Timestamp()}");
                }

                // "Is this clientNodeId paired with the serverNodeId?" / "Is the serverNodeId known?"
                var pairing = await Store.GetPairingAsync(Request.ServerNodeId, Request.ClientNodeId, CancellationToken).ConfigureAwait(false);

                if (pairing is null || !Endpoint.TryGetNode(Request.ServerNodeId, out var node))
                    return SessionInitiationResult.Wrap<InitiateSessionResponse>(SessionInitiationResult.Unauthorized("the nodes are not paired or the server node is unknown"));

                if (!pairing.IsCommunicationServer)
                    return SessionInitiationResult.Wrap<InitiateSessionResponse>(SessionInitiationResult.Unauthorized("the local node is not the communication server of this pairing"));

                // "Is this the correct accessToken for this node ID?"
                if (!pairing.AccessToken.ConstantTimeEquals(Bearer))
                    return SessionInitiationResult.Wrap<InitiateSessionResponse>(SessionInitiationResult.Unauthorized("the access token was not accepted"));

                // "Is there overlap between the communication protocols?"
                var protocol = node.SupportedCommunicationProtocols.
                                   Where(candidate => Request.SupportedCommunicationProtocols.Contains(candidate)).
                                   Cast<CommunicationProtocol?>().
                                   FirstOrDefault();

                if (!protocol.HasValue)
                    return BadRequest<InitiateSessionResponse>(CommunicationDetailsError.IncompatibleCommunicationProtocols, $"the node supports {String.Join(", ", node.SupportedCommunicationProtocols)}");

                // "Is there overlap between the S2 message versions?"
                var version = node.SupportedS2MessageVersions.
                                  FirstOrDefault(candidate => Request.SupportedS2MessageVersions.Contains(candidate, StringComparer.Ordinal));

                if (version is null)
                    return BadRequest<InitiateSessionResponse>(CommunicationDetailsError.IncompatibleS2MessageVersions, $"the node supports {String.Join(", ", node.SupportedS2MessageVersions)}");

                // "Are the endpoint and node ready for connecting?"
                if (!node.IsReadyForPairing)
                    return BadRequest<InitiateSessionResponse>(CommunicationDetailsError.Other, "the node is not ready for connecting");

                if (Request.ClientNodeDescription is not null && Request.ClientNodeDescription.Id != Request.ClientNodeId)
                    return BadRequest<InitiateSessionResponse>(CommunicationDetailsError.ParsingError, "the identification of the client node description differs from clientNodeId");

                #region 2. Generate the pending access token

                var pendingToken = TokenGenerator.NewAccessToken();

                await Store.AddPendingAccessTokenAsync(
                          new PendingAccessToken(
                              pairing.LocalNodeId,
                              pairing.RemoteNodeId,
                              pendingToken,
                              now,
                              protocol.Value,
                              version
                          ),
                          CancellationToken
                      ).ConfigureAwait(false);

                #endregion

                #region Persist optional description updates (after the token, non-fatal)

                if (Request.ClientNodeDescription is not null || Request.ClientEndpointDescription is not null)
                {

                    pairing = pairing.WithDescriptions(Request.ClientNodeDescription     ?? pairing.RemoteNodeDescription,
                                                       Request.ClientEndpointDescription ?? pairing.RemoteEndpointDescription);

                    try
                    {
                        await Store.AddOrReplacePairingAsync(pairing, CancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        logger?.LogWarning(e, "S2 session initiation: persisting the description updates failed; the session initiation continues.");
                    }

                }

                #endregion

                var response = new InitiateSessionResponse(
                                   protocol.Value,
                                   version,
                                   pendingToken,
                                   Options.AlwaysSendDescriptions ? node.Description     : null,
                                   Options.AlwaysSendDescriptions ? Endpoint.Description : null
                               );

                logger?.LogInformation("S2 session initiation: {ClientNodeId} -> {ServerNodeId}: pending access token issued ({Protocol}, {Version}).", pairing.RemoteNodeId, pairing.LocalNodeId, protocol, version);

                await OnSessionInitiated.InvokeAllAsync(handler => handler(now, this, pairing, response), logger).ConfigureAwait(false);

                return SessionInitiationResult.OK(response);

            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger?.LogError(e, "S2 session initiation: the store failed during initiateSession.");
                return SessionInitiationResult.Wrap<InitiateSessionResponse>(SessionInitiationResult.InternalServerError("the store failed"));
            }

        }

        #endregion

        #region ConfirmAccessTokenAsync(PendingBearer, RemoteAddress = null, CancellationToken = default)

        /// <summary>
        /// Step 5 to 7: activate the pending access token presented as bearer token (not older
        /// than 15 seconds) in one store operation and issue a single-use communication token
        /// for the WebSocket server (S2 Connect 1.0.0, "6. Activate new accessToken",
        /// "7. Response status 200"). A failing store answers 500 and leaves the pending token untouched.
        /// </summary>
        /// <param name="PendingBearer">The pending access token presented by the client.</param>
        /// <param name="RemoteAddress">The optional address of the client for logs.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        public async Task<SessionInitiationResult<CommunicationDetails>>

            ConfirmAccessTokenAsync(AccessToken        PendingBearer,
                                    IPAddress?         RemoteAddress       = null,
                                    CancellationToken  CancellationToken   = default)

        {

            if (isShutdown)
                return SessionInitiationResult.Wrap<CommunicationDetails>(SessionInitiationResult.ServiceUnavailable(TimeSpan.FromSeconds(1), "the session initiation server is shut down"));

            var now = TimeProvider.GetUtcNow();

            PendingAccessToken? pending;

            try
            {
                pending = await Store.FindPendingAccessTokenAsync(PendingBearer, CancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger?.LogError(e, "S2 session initiation: the store failed during confirmAccessToken.");
                return SessionInitiationResult.Wrap<CommunicationDetails>(SessionInitiationResult.InternalServerError("the store failed"));
            }

            if (pending is null)
                return SessionInitiationResult.Wrap<CommunicationDetails>(SessionInitiationResult.Unauthorized("the access token is not pending"));

            if (pending.HasExpired(now, Options.PendingAccessTokenLifetime))
            {

                logger?.LogInformation("S2 session initiation: the pending access token of {Pending} expired.", pending);

                try
                {
                    await Store.RemovePendingAccessTokensAsync(pending.LocalNodeId, pending.RemoteNodeId, pending.CreatedAt.AddTicks(1), CancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    logger?.LogWarning(e, "S2 session initiation: removing the expired pending token failed.");
                }

                return SessionInitiationResult.Wrap<CommunicationDetails>(SessionInitiationResult.Unauthorized("the pending access token expired"));

            }

            if (!Endpoint.TryGetNode(pending.LocalNodeId, out _))
                return SessionInitiationResult.Wrap<CommunicationDetails>(SessionInitiationResult.Unauthorized("the server node is unknown"));

            #region 6. Activate the token (one store operation)

            Pairing? pairing;

            try
            {
                pairing = await Store.ActivateAccessTokenAsync(pending.LocalNodeId, pending.RemoteNodeId, pending.Token, CancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger?.LogError(e, "S2 session initiation: activating the access token failed.");
                return SessionInitiationResult.Wrap<CommunicationDetails>(SessionInitiationResult.InternalServerError("the store failed"));
            }

            if (pairing is null)
                return SessionInitiationResult.Wrap<CommunicationDetails>(SessionInitiationResult.Unauthorized("the pairing no longer exists"));

            #endregion

            #region 7. Issue the communication token

            var protocol  = pending.SelectedCommunicationProtocol ?? CommunicationProtocol.WebSocket;
            var version   = pending.SelectedS2MessageVersion      ?? Version.S2JSONVersion;
            var identity  = new S2ConnectSessionIdentity(pairing, protocol, version);
            var token     = TokenStore.Issue(identity, Options.CommunicationTokenLifetime);
            var details   = new WebSocketCommunicationDetails(CommunicationToken.Parse(token), WebSocketUrl);

            #endregion

            logger?.LogInformation("S2 session initiation: access token of {RemoteNodeId} at {LocalNodeId} activated, communication token issued.", pairing.RemoteNodeId, pairing.LocalNodeId);

            await OnAccessTokenActivated.InvokeAllAsync(handler => handler(now, this, pairing, identity, details), logger).ConfigureAwait(false);

            return SessionInitiationResult.OK<CommunicationDetails>(details);

        }

        #endregion

        #region UnpairAsync            (Bearer, Request, RemoteAddress = null, CancellationToken = default)

        /// <summary>
        /// POST /[version]/unpair: the communication client unpairs; every security material
        /// of the pairing is removed and a tombstone written (S2 Connect 1.0.0, "Unpairing by
        /// the communication client"). 401 when the nodes are not paired or the token is wrong.
        /// </summary>
        /// <param name="Bearer">The access token presented by the client.</param>
        /// <param name="Request">The unpair request.</param>
        /// <param name="RemoteAddress">The optional address of the client for logs.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        public async Task<SessionInitiationResult> UnpairAsync(AccessToken        Bearer,
                                                               UnpairRequest      Request,
                                                               IPAddress?         RemoteAddress       = null,
                                                               CancellationToken  CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Request);

            var now = TimeProvider.GetUtcNow();

            try
            {

                var pairing = await Store.GetPairingAsync(Request.ServerNodeId, Request.ClientNodeId, CancellationToken).ConfigureAwait(false);

                if (pairing is null)
                    return SessionInitiationResult.Unauthorized("the nodes are not paired");

                // Any candidate token of the client (active or pending) unpairs.
                var candidates = await Store.GetAccessTokenCandidatesAsync(Request.ServerNodeId, Request.ClientNodeId, CancellationToken).ConfigureAwait(false);

                if (!candidates.Any(candidate => candidate.ConstantTimeEquals(Bearer)))
                    return SessionInitiationResult.Unauthorized("the access token was not accepted");

                var removed = await Store.UnpairAsync(Request.ServerNodeId, Request.ClientNodeId, now, CancellationToken).ConfigureAwait(false);

                if (removed is null)
                    return SessionInitiationResult.Unauthorized("the nodes are not paired");

                logger?.LogInformation("S2 session initiation: {ClientNodeId} unpaired from {ServerNodeId}.", Request.ClientNodeId, Request.ServerNodeId);

                await OnUnpaired.InvokeAllAsync(handler => handler(now, this, removed, true), logger).ConfigureAwait(false);

                return SessionInitiationResult.NoContent();

            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger?.LogError(e, "S2 session initiation: the store failed during unpair.");
                return SessionInitiationResult.InternalServerError("the store failed");
            }

        }

        #endregion

        #region UnpairLocallyAsync     (LocalNodeId, RemoteNodeId, CancellationToken = default)

        /// <summary>
        /// Unpairing by the communication server (S2 Connect 1.0.0, "Unpairing by the
        /// communication server"): remove the security material of the pairing and write the
        /// tombstone, so that the next initiateSession answers NoLongerPaired. The caller then
        /// sends SessionRequest RECONNECT to the client and closes the session.
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        /// <returns>The removed pairing, or null when the nodes were not paired.</returns>
        public async Task<Pairing?> UnpairLocallyAsync(Node_Id            LocalNodeId,
                                                       Node_Id            RemoteNodeId,
                                                       CancellationToken  CancellationToken   = default)
        {

            var now      = TimeProvider.GetUtcNow();
            var removed  = await Store.UnpairAsync(LocalNodeId, RemoteNodeId, now, CancellationToken).ConfigureAwait(false);

            if (removed is not null)
            {
                logger?.LogInformation("S2 session initiation: {LocalNodeId} unpaired from {RemoteNodeId} locally.", LocalNodeId, RemoteNodeId);
                await OnUnpaired.InvokeAllAsync(handler => handler(now, this, removed, false), logger).ConfigureAwait(false);
            }

            return removed;

        }

        #endregion

        #region PurgePendingAccessTokensAsync(CancellationToken = default)

        /// <summary>
        /// Remove every pending access token older than the pending token lifetime.
        /// </summary>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        /// <returns>The number of removed tokens.</returns>
        public ValueTask<Int32> PurgePendingAccessTokensAsync(CancellationToken CancellationToken = default)

            => Store.RemovePendingAccessTokensAsync(null,
                                                    null,
                                                    TimeProvider.GetUtcNow() - Options.PendingAccessTokenLifetime,
                                                    CancellationToken);

        #endregion

        #region Shutdown()

        /// <summary>
        /// Reject further initiateSession and confirmAccessToken requests with 503.
        /// </summary>
        public void Shutdown()
        {
            isShutdown = true;
        }

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

        #region (private) HandleInitiateSessionAsync(Request)

        private async Task<HTTPResponse> HandleInitiateSessionAsync(HTTPRequest Request)
        {

            if (!TryGetBearerToken(Request, out var bearer))
                return ResultResponse(Request, SessionInitiationResult.Unauthorized("missing or malformed access token"));

            if (!TryReadJSONObject(Request, out var json, out var failure))
                return ResultResponse(Request, failure);

            if (!InitiateSessionRequest.TryParse(json, out var request, out var parseError, Options.ParserOptions))
                return ResultResponse(Request, SessionInitiationResult.BadRequest(CommunicationDetailsError.ParsingError, Info(parseError)));

            var result = await InitiateSessionAsync(bearer,
                                                    request,
                                                    RemoteAddressOf(Request),
                                                    Request.CancellationToken).ConfigureAwait(false);

            return ResultResponse(Request, result, result.Value?.ToJSON());

        }

        #endregion

        #region (private) HandleConfirmAccessTokenAsync(Request)

        private async Task<HTTPResponse> HandleConfirmAccessTokenAsync(HTTPRequest Request)
        {

            if (!TryGetBearerToken(Request, out var bearer))
                return ResultResponse(Request, SessionInitiationResult.Unauthorized("missing or malformed access token"));

            var result = await ConfirmAccessTokenAsync(bearer,
                                                       RemoteAddressOf(Request),
                                                       Request.CancellationToken).ConfigureAwait(false);

            return ResultResponse(Request, result, result.Value?.ToJSON());

        }

        #endregion

        #region (private) HandleUnpairAsync(Request)

        private async Task<HTTPResponse> HandleUnpairAsync(HTTPRequest Request)
        {

            if (!TryGetBearerToken(Request, out var bearer))
                return ResultResponse(Request, SessionInitiationResult.Unauthorized("missing or malformed access token"));

            if (!TryReadJSONObject(Request, out var json, out var failure))
                return ResultResponse(Request, failure);

            if (!UnpairRequest.TryParse(json, out var request, out var parseError, Options.ParserOptions))
                return ResultResponse(Request, SessionInitiationResult.BadRequest(CommunicationDetailsError.ParsingError, Info(parseError)));

            var result = await UnpairAsync(bearer,
                                           request,
                                           RemoteAddressOf(Request),
                                           Request.CancellationToken).ConfigureAwait(false);

            return ResultResponse(Request, result);

        }

        #endregion


        // Helpers

        #region (private) Info / BadRequest<T>

        private String? Info(String Text)
            => Options.IncludeErrorDetails ? Text : null;

        private SessionInitiationResult<T> BadRequest<T>(CommunicationDetailsError  Error,
                                                         String?                    AdditionalInfo)
            where T : class

            => SessionInitiationResult.Wrap<T>(SessionInitiationResult.BadRequest(Error, Info(AdditionalInfo ?? "")));

        #endregion

        #region (private static) TryGetBearerToken(Request, out Token)

        private static Boolean TryGetBearerToken(HTTPRequest      Request,
                                                 out AccessToken  Token)
        {

            if (Request.Authorization is HTTPBearerAuthentication bearer &&
                AccessToken.TryParse(bearer.Token, out Token))
            {
                return true;
            }

            Token = default;
            return false;

        }

        #endregion

        #region (private) TryReadJSONObject(Request, out JSON, out Failure)

        private Boolean TryReadJSONObject(HTTPRequest                                Request,
                                          [NotNullWhen(true)]  out JObject?                  JSON,
                                          [NotNullWhen(false)] out SessionInitiationResult?  Failure)
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

                var token = JToken.Load(reader);

                if (reader.Read())
                {
                    Failure = BadRequestResult("additional content after the JSON document");
                    return false;
                }

                if (token is not JObject jsonObject)
                {
                    Failure = BadRequestResult("a JSON object is expected");
                    return false;
                }

                JSON     = jsonObject;
                Failure  = null;
                return true;

            }
            catch (Exception e)
            {
                Failure = BadRequestResult("invalid JSON: " + e.Message);
                return false;
            }

        }

        private SessionInitiationResult BadRequestResult(String Error)

            => SessionInitiationResult.BadRequest(
                   CommunicationDetailsError.ParsingError,
                   Info(Error)
               );

        private SessionInitiationResult TooLargeResult()

            => SessionInitiationResult.PayloadTooLarge(
                   Options.MaxRequestBodySize,
                   Info($"the request body must not exceed {Options.MaxRequestBodySize} bytes")
               );

        #endregion

        #region (private) CheckAnnouncedRequestSize(Request)

        /// <summary>
        /// Refuse a request whose announced Content-Length exceeds the configured limit, before
        /// anything reads its body. Returns the refusal, or null when the request may proceed.
        /// </summary>
        /// <param name="Request">An HTTP request.</param>
        private SessionInitiationResult? CheckAnnouncedRequestSize(HTTPRequest Request)

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
        private SessionInitiationResult? CheckRateLimit(HTTPRequest Request)
        {

            if (RequestRateLimiter is null)
                return null;

            var remoteAddress  = RemoteAddressOf(Request);
            var decision       = RequestRateLimiter.TryAcquire(remoteAddress, TimeProvider.GetUtcNow());

            if (decision.Allowed)
                return null;

            logger?.LogWarning(
                "S2 session initiation: the request budget of {RemoteAddress} is exhausted, retry in {RetryAfter} seconds.",
                remoteAddress?.ToString() ?? S2RequestRateLimiter.UnknownAddress,
                Math.Ceiling(decision.RetryAfter.TotalSeconds)
            );

            return Options.UseTooManyRequestsStatusCode
                       ? SessionInitiationResult.TooManyRequests   (decision.RetryAfter, "the request budget of the remote address is exhausted")
                       : SessionInitiationResult.ServiceUnavailable(decision.RetryAfter, "the request budget of the remote address is exhausted");

        }

        #endregion

        #region (private static) RemoteAddressOf(Request)

        private static IPAddress? RemoteAddressOf(HTTPRequest Request)
        {

            try
            {

                var address = Request.RemoteSocket.IPAddress;

                return address is null
                           ? null
                           : address.ToDotNet();

            }
            catch (Exception)
            {
                return null;
            }

        }

        #endregion

        #region (private) ResultResponse / JSONResponse / NewResponse

        private HTTPResponse ResultResponse(HTTPRequest              Request,
                                            SessionInitiationResult  Result,
                                            JToken?                  Content   = null)
        {

            var builder = NewResponse(
                              Request,
                              Result.StatusCode,
                              DrainRequestBody: Result.StatusCode != HTTPStatusCode.RequestEntityTooLarge
                          );

            if (Result.StatusCode == HTTPStatusCode.Unauthorized)
                builder.WWWAuthenticate = WWWAuthenticate.Parse("Bearer realm=\"S2 Connect session initiation\"");

            if (Result.RetryAfter.HasValue)
                builder.RetryAfter = Math.Max(1, (Int64) Math.Ceiling(Result.RetryAfter.Value.TotalSeconds)).ToString();

            var json = Result.Error?.ToJSON() ?? (Result.IsSuccess ? Content : null);

            if (json is not null)
            {
                builder.ContentType  = HTTPContentType.Application.JSON_UTF8;
                builder.Content      = json.ToString(Formatting.None).ToUTF8Bytes();
            }

            // Hermod omits the Content-Length header without content; clients would wait for the connection to close.
            else if (Result.StatusCode != HTTPStatusCode.NoContent)
                builder.Content = [];

            return builder.AsImmutable;

        }

        private HTTPResponse JSONResponse(HTTPRequest     Request,
                                          HTTPStatusCode  StatusCode,
                                          JToken          JSON)
        {

            var builder = NewResponse(Request, StatusCode);

            builder.ContentType  = HTTPContentType.Application.JSON_UTF8;
            builder.Content      = JSON.ToString(Formatting.None).ToUTF8Bytes();

            return builder.AsImmutable;

        }

        private HTTPResponse.Builder NewResponse(HTTPRequest     Request,
                                                 HTTPStatusCode  StatusCode,
                                                 Boolean         DrainRequestBody   = true)
        {

            // Drain an unread request body, so that its remains are not mistaken for the next
            // request. An oversized body is the exception: reading it is exactly what the refusal
            // avoids, so that answer closes the connection instead.
            if (DrainRequestBody)
            {
                try
                {
                    Request.TryReadHTTPBodyStream();
                }
                catch (Exception e)
                {
                    logger?.LogDebug(e, "S2 session initiation server: could not drain the request body.");
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

            // Responses carry secrets (access and communication tokens) and must never be cached.
            builder.SetCacheControl("no-store");

            return builder;

        }

        #endregion

    }

}

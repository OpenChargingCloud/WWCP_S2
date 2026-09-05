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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.TLS;
using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

using cloud.charging.open.protocols.S2.Session;
using cloud.charging.open.protocols.S2.WebSockets;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The result of <see cref="SessionInitiationClient.ConnectAsync"/>: the session initiation
    /// result and, on success, the opened S2 session.
    /// </summary>
    /// <param name="Initiation">The result of the session initiation.</param>
    /// <param name="Session">The opened session, or null.</param>
    public sealed record S2ConnectResult(SessionInitiationClientResult  Initiation,
                                         S2ConnectSession?              Session)
    {

        /// <summary>
        /// Whether a session was opened.
        /// </summary>
        public Boolean IsSuccess
            => Session is not null;

    }


    /// <summary>
    /// The S2 Connect session initiation client of a communication client (S2 Connect 1.0.0,
    /// "Session initiation", "Reconnection strategy", "Unpairing by the communication client"):
    /// fetches the version index before every procedure, tries the persisted candidate access
    /// tokens sequentially, persists the pending token before confirming it, activates it only
    /// after the 200 of step 7, opens the WebSocket session with the received communication
    /// details, and unpairs with local cleanup.
    /// </summary>
    public class SessionInitiationClient : AS2ConnectClient
    {

        #region Data

        /// <summary>
        /// The default HTTP user agent.
        /// </summary>
        public new const String  DefaultHTTPUserAgent  = "GraphDefined S2 Connect Session Initiation Client";

        private const String InitiateSessionOperation     = "initiateSession";
        private const String ConfirmAccessTokenOperation  = "confirmAccessToken";
        private const String UnpairOperation              = "unpair";
        private const String VersionIndexOperation        = "versionIndex";
        private const String WebSocketOperation           = "webSocket";

        private readonly RemoteTLSServerCertificateValidationHandler<IWebSocketClient>?  webSocketCertificateValidator;

        #endregion

        #region Properties

        /// <summary>
        /// The base URL of the session initiation API of the communication server.
        /// </summary>
        public S2BaseURL                       SessionInitiationUrl    => BaseUrl;

        /// <summary>
        /// The local endpoint with the nodes acting as communication clients.
        /// </summary>
        public LocalEndpoint                   LocalEndpoint           { get; }

        /// <summary>
        /// The store of the pairings and candidate access tokens.
        /// </summary>
        public IS2Store                        Store                   { get; }

        /// <summary>
        /// The options of this client.
        /// </summary>
        public SessionInitiationClientOptions  Options                 { get; }

        /// <inheritdoc/>
        protected override TimeSpan            VersionIndexTimeout
            => Options.RequestTimeout;

        #endregion

        #region Events

        /// <summary>
        /// Raised when a session initiation or an unpairing ended.
        /// </summary>
        public event OnSessionInitiationCompletedDelegate?  OnCompleted;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new session initiation client.
        /// </summary>
        /// <param name="SessionInitiationUrl">The base URL of the session initiation API of the communication server.</param>
        /// <param name="LocalEndpoint">The local endpoint with the nodes acting as communication clients.</param>
        /// <param name="Store">The store of the pairings and candidate access tokens.</param>
        /// <param name="Options">Optional client options.</param>
        /// <param name="RemoteCertificateValidator">An optional custom TLS server certificate validator for the HTTPS requests.</param>
        /// <param name="WebSocketCertificateValidator">An optional custom TLS server certificate validator for the WebSocket connection.</param>
        /// <param name="TimeProvider">An optional time provider (default: the time provider of the endpoint).</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        /// <param name="DNSClient">An optional DNS client.</param>
        /// <param name="HTTPUserAgent">An optional HTTP user agent.</param>
        /// <param name="Description">An optional description.</param>
        /// <param name="DisableLogging">Whether to disable Hermod's client logging (default: true).</param>
        public SessionInitiationClient(S2BaseURL                                                       SessionInitiationUrl,
                                       LocalEndpoint                                                   LocalEndpoint,
                                       IS2Store                                                        Store,
                                       SessionInitiationClientOptions?                                 Options                         = null,
                                       RemoteTLSServerCertificateValidationHandler<IHTTPClient>?       RemoteCertificateValidator      = null,
                                       RemoteTLSServerCertificateValidationHandler<IWebSocketClient>?  WebSocketCertificateValidator   = null,
                                       TimeProvider?                                                   TimeProvider                    = null,
                                       ILoggerFactory?                                                 LoggerFactory                   = null,
                                       IDNSClient?                                                     DNSClient                       = null,
                                       String?                                                         HTTPUserAgent                   = null,
                                       I18NString?                                                     Description                     = null,
                                       Boolean?                                                        DisableLogging                  = null)

            : base(SessionInitiationUrl,
                   (Options ?? SessionInitiationClientOptions.Default).SupportedAPIVersions,
                   (Options ?? SessionInitiationClientOptions.Default).ParserOptions,
                   (Options ?? SessionInitiationClientOptions.Default).AcceptSelfSignedCertificates,
                   null,
                   RemoteCertificateValidator,
                   TimeProvider ?? LocalEndpoint?.TimeProvider,
                   LoggerFactory,
                   DNSClient,
                   HTTPUserAgent,
                   DefaultHTTPUserAgent,
                   Description,
                   DisableLogging)

        {

            ArgumentNullException.ThrowIfNull(LocalEndpoint);
            ArgumentNullException.ThrowIfNull(Store);

            this.LocalEndpoint                  = LocalEndpoint;
            this.Store                          = Store;
            this.Options                        = Options ?? SessionInitiationClientOptions.Default;
            this.Options.Validate();

            this.webSocketCertificateValidator  = WebSocketCertificateValidator;

        }

        #endregion


        #region InitiateSessionAsync(LocalNode, ServerNodeId, CancellationToken = default)

        /// <summary>
        /// Run session initiation for the given local node (S2 Connect 1.0.0, "Session initiation",
        /// steps 1 to 8): the version index is fetched, every candidate access token is tried
        /// sequentially, the pending token is persisted before it is confirmed and activated
        /// only after the 200 of step 7.
        /// </summary>
        /// <param name="LocalNode">The local node (the communication client).</param>
        /// <param name="ServerNodeId">The identification of the node at the communication server.</param>
        /// <param name="CancellationToken">A token to cancel the procedure.</param>
        public async Task<SessionInitiationClientResult> InitiateSessionAsync(HostedNode         LocalNode,
                                                                              Node_Id            ServerNodeId,
                                                                              CancellationToken  CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(LocalNode);

            SessionInitiationClientResult result;

            try
            {
                result = await RunInitiateSessionAsync(LocalNode, ServerNodeId, CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                result = new SessionInitiationClientResult(SessionInitiationOutcome.Cancelled, InitiateSessionOperation, Description: "the procedure was cancelled");
            }

            Logger?.LogInformation("S2 session initiation client: {LocalNodeId} -> {ServerNodeId}: {Result}", LocalNode.Id, ServerNodeId, result);

            await OnCompleted.InvokeAllAsync(handler => handler(TimeProvider.GetUtcNow(), this, LocalNode.Id, ServerNodeId, result), Logger).ConfigureAwait(false);

            return result;

        }

        #endregion

        #region (private) RunInitiateSessionAsync(LocalNode, ServerNodeId, CancellationToken)

        private async Task<SessionInitiationClientResult> RunInitiateSessionAsync(HostedNode         LocalNode,
                                                                                  Node_Id            ServerNodeId,
                                                                                  CancellationToken  CancellationToken)
        {

            #region 0. Preconditions: pairing, candidate tokens, API version

            Pairing?                    pairing;
            IReadOnlyList<AccessToken>  candidates;

            try
            {

                pairing = await Store.GetPairingAsync(LocalNode.Id, ServerNodeId, CancellationToken).ConfigureAwait(false);

                if (pairing is null)
                    return new SessionInitiationClientResult(SessionInitiationOutcome.InvalidConfiguration, InitiateSessionOperation, Description: "the nodes are not paired");

                if (!pairing.IsCommunicationClient)
                    return new SessionInitiationClientResult(SessionInitiationOutcome.InvalidConfiguration, InitiateSessionOperation, Description: "the local node is not the communication client of this pairing");

                candidates = await Store.GetAccessTokenCandidatesAsync(LocalNode.Id, ServerNodeId, CancellationToken).ConfigureAwait(false);

            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                return new SessionInitiationClientResult(SessionInitiationOutcome.StoreFailure, InitiateSessionOperation, Description: e.Message);
            }

            if (candidates.Count == 0)
                return new SessionInitiationClientResult(SessionInitiationOutcome.InvalidConfiguration, InitiateSessionOperation, Description: "no access token is known for this pairing");

            // The version index is fetched before every session initiation.
            ForgetVersion();

            if (await EnsureVersionAsync(CancellationToken).ConfigureAwait(false) is { } versionFailure)
                return VersionResult(versionFailure);

            #endregion

            var request = new InitiateSessionRequest(
                              LocalNode.Id,
                              ServerNodeId,
                              LocalNode.SupportedS2MessageVersions,
                              LocalNode.SupportedCommunicationProtocols,
                              Options.SendDescriptions ? LocalNode.Description     : null,
                              Options.SendDescriptions ? LocalEndpoint.Description : null
                          );

            var body    = request.ToJSON();

            foreach (var candidate in candidates)
            {

                #region 1. POST initiateSession with the candidate token

                var result1 = await SendAsyncWithRetries(InitiateSessionOperation, body, HTTPBearerAuthentication.Parse(candidate.Value), CancellationToken).ConfigureAwait(false);

                if (result1.CertificateChanged)
                    return new SessionInitiationClientResult(SessionInitiationOutcome.CertificateChanged, InitiateSessionOperation, StatusCode: result1.StatusCode, Description: result1.FailureText);

                if (result1.IsTransportFailure)
                    return new SessionInitiationClientResult(SessionInitiationOutcome.TransportFailure, InitiateSessionOperation, StatusCode: result1.StatusCode, Description: result1.FailureText);

                if (result1.StatusCode == HTTPStatusCode.Unauthorized)
                {
                    Logger?.LogDebug("S2 session initiation client: a candidate access token was not accepted, trying the next one.");
                    continue;
                }

                if (result1.StatusCode == HTTPStatusCode.BadRequest)
                {

                    var error = TryParseError(result1);

                    if (error is not null && error.ErrorMessage == CommunicationDetailsError.NoLongerPaired)
                    {

                        if (Options.RemovePairingWhenNoLongerPaired)
                        {
                            try
                            {
                                await Store.UnpairAsync(LocalNode.Id, ServerNodeId, TimeProvider.GetUtcNow(), CancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception e) when (e is not OperationCanceledException)
                            {
                                Logger?.LogWarning(e, "S2 session initiation client: removing the pairing after NoLongerPaired failed.");
                            }
                        }

                        return new SessionInitiationClientResult(SessionInitiationOutcome.NoLongerPaired, InitiateSessionOperation, result1.StatusCode, error,
                                                                 "the server no longer considers the nodes paired; inform the end user");

                    }

                    return new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater, InitiateSessionOperation, result1.StatusCode, error, "the server rejected the request; retry later");

                }

                // 429 is reported like 503: a rate limited server is a temporarily unavailable server.
                if (result1.StatusCode == HTTPStatusCode.ServiceUnavailable ||
                    result1.StatusCode == HTTPStatusCode.TooManyRequests)
                    return new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater, InitiateSessionOperation, result1.StatusCode, Description: "the server is temporarily not available");

                if (result1.StatusCode != HTTPStatusCode.OK)
                    return new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater, InitiateSessionOperation, result1.StatusCode, Description: $"unexpected status code {result1.StatusCode.Code}");

                #endregion

                #region 3. Parse and check the response

                String? parseError = null;

                if (!TryReadJSON(result1, out var json1, out var jsonError) || json1 is not JObject responseJSON ||
                    !InitiateSessionResponse.TryParse(responseJSON, out var response, out parseError, Options.ParserOptions))
                {
                    return new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater, InitiateSessionOperation, result1.StatusCode,
                                                             Description: $"the initiateSession response could not be parsed: {jsonError ?? parseError ?? "a JSON object is expected"}");
                }

                if (!LocalNode.SupportedS2MessageVersions.Contains(response.SelectedS2MessageVersion, StringComparer.Ordinal))
                    return new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater, InitiateSessionOperation, result1.StatusCode,
                                                             Description: $"the server selected the S2 message version '{response.SelectedS2MessageVersion}', which was not offered");

                if (!LocalNode.SupportedCommunicationProtocols.Contains(response.SelectedCommunicationProtocol))
                    return new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater, InitiateSessionOperation, result1.StatusCode,
                                                             Description: $"the server selected the communication protocol '{response.SelectedCommunicationProtocol}', which was not offered");

                #endregion

                #region 4. Persist the pending token

                var pending = new PendingAccessToken(
                                  LocalNode.Id,
                                  ServerNodeId,
                                  response.AccessToken,
                                  TimeProvider.GetUtcNow(),
                                  response.SelectedCommunicationProtocol,
                                  response.SelectedS2MessageVersion
                              );

                try
                {
                    await Store.AddPendingAccessTokenAsync(pending, CancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    Logger?.LogError(e, "S2 session initiation client: persisting the pending access token failed; the session is not initiated.");
                    return new SessionInitiationClientResult(SessionInitiationOutcome.StoreFailure, InitiateSessionOperation, Description: e.Message);
                }

                #endregion

                #region 5. POST confirmAccessToken with the pending token

                var result5 = await SendAsyncWithRetries(ConfirmAccessTokenOperation, null, HTTPBearerAuthentication.Parse(pending.Token.Value), CancellationToken).ConfigureAwait(false);

                if (result5.CertificateChanged)
                    return new SessionInitiationClientResult(SessionInitiationOutcome.CertificateChanged, ConfirmAccessTokenOperation, StatusCode: result5.StatusCode, Description: result5.FailureText);

                if (result5.IsTransportFailure)
                    return new SessionInitiationClientResult(SessionInitiationOutcome.TransportFailure, ConfirmAccessTokenOperation, StatusCode: result5.StatusCode,
                                                             Description: "the confirmation was not answered; the pending token stays a candidate for the next attempt");

                if (result5.StatusCode == HTTPStatusCode.Unauthorized)
                {

                    try
                    {
                        await Store.RemovePendingAccessTokensAsync(LocalNode.Id, ServerNodeId, pending.CreatedAt.AddTicks(1), CancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        Logger?.LogWarning(e, "S2 session initiation client: removing the rejected pending token failed.");
                    }

                    return new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater, ConfirmAccessTokenOperation, result5.StatusCode,
                                                             Description: "the pending access token was not accepted (expired?); retry later starting at step 1");

                }

                if (result5.StatusCode != HTTPStatusCode.OK)
                    return new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater, ConfirmAccessTokenOperation, result5.StatusCode,
                                                             Description: $"unexpected status code {result5.StatusCode.Code}; retry later starting at step 1");

                #endregion

                #region 7. Parse the communication details

                CommunicationDetails? details = null;

                if (!TryReadJSON(result5, out var json5, out jsonError) || json5 is not JObject detailsJSON ||
                    !CommunicationDetails.TryParse(detailsJSON, out details, out parseError, Options.ParserOptions))
                {
                    return new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater, ConfirmAccessTokenOperation, result5.StatusCode,
                                                             Description: $"the communication details could not be parsed: {jsonError ?? parseError ?? "a JSON object is expected"}");
                }

                #endregion

                #region 8. Activate the token, remove the old one, persist description updates

                Pairing? updated;

                try
                {
                    updated = await Store.ActivateAccessTokenAsync(LocalNode.Id, ServerNodeId, pending.Token, CancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    Logger?.LogError(e, "S2 session initiation client: activating the access token failed.");
                    return new SessionInitiationClientResult(SessionInitiationOutcome.StoreFailure, ConfirmAccessTokenOperation, Description: e.Message);
                }

                if (updated is null)
                    return new SessionInitiationClientResult(SessionInitiationOutcome.InvalidConfiguration, ConfirmAccessTokenOperation, Description: "the pairing disappeared during session initiation");

                if (response.ServerNodeDescription is not null || response.ServerEndpointDescription is not null)
                {

                    try
                    {

                        if (response.ServerNodeDescription is null || response.ServerNodeDescription.Id == updated.RemoteNodeId)
                        {

                            updated = updated.WithDescriptions(response.ServerNodeDescription     ?? updated.RemoteNodeDescription,
                                                               response.ServerEndpointDescription ?? updated.RemoteEndpointDescription);

                            await Store.AddOrReplacePairingAsync(updated, CancellationToken).ConfigureAwait(false);

                        }

                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        Logger?.LogWarning(e, "S2 session initiation client: persisting the description updates failed.");
                    }

                }

                #endregion

                return new SessionInitiationClientResult(SessionInitiationOutcome.Success,
                                                         ConfirmAccessTokenOperation,
                                                         result5.StatusCode,
                                                         Pairing:                        updated,
                                                         CommunicationDetails:           details,
                                                         SelectedCommunicationProtocol:  response.SelectedCommunicationProtocol,
                                                         SelectedS2MessageVersion:       response.SelectedS2MessageVersion);

            }

            return new SessionInitiationClientResult(SessionInitiationOutcome.Unauthorized, InitiateSessionOperation, HTTPStatusCode.Unauthorized,
                                                     Description: "no candidate access token was accepted; do not retry, inform the end user");

        }

        #endregion

        #region ConnectAsync(LocalNode, ServerNodeId, ConfigureSession = null, CancellationToken = default)

        /// <summary>
        /// Run session initiation and open the S2 session over the received communication
        /// details (S2 Connect 1.0.0, "WebSocket based communication").
        /// </summary>
        /// <param name="LocalNode">The local node (the communication client).</param>
        /// <param name="ServerNodeId">The identification of the node at the communication server.</param>
        /// <param name="ConfigureSession">An optional customisation of the session options.</param>
        /// <param name="CancellationToken">A token to cancel the procedure.</param>
        public async Task<S2ConnectResult> ConnectAsync(HostedNode                                      LocalNode,
                                                        Node_Id                                         ServerNodeId,
                                                        Func<S2SessionOptions, S2SessionOptions>?       ConfigureSession    = null,
                                                        CancellationToken                               CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(LocalNode);

            var initiation = await InitiateSessionAsync(LocalNode, ServerNodeId, CancellationToken).ConfigureAwait(false);

            if (!initiation.IsSuccess)
                return new S2ConnectResult(initiation, null);

            if (initiation.CommunicationDetails is not WebSocketCommunicationDetails webSocketDetails)
                return new S2ConnectResult(
                           new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater, WebSocketOperation, Pairing: initiation.Pairing,
                                                             Description: $"unsupported communication protocol '{initiation.CommunicationDetails?.CommunicationProtocol}'"),
                           null
                       );

            var sessionOptions = new S2SessionOptions {
                                     Role               = LocalNode.Role,
                                     Mode               = S2SessionMode.S2Connect,
                                     NegotiatedVersion  = initiation.SelectedS2MessageVersion
                                 };

            if (ConfigureSession is not null)
                sessionOptions = ConfigureSession(sessionOptions);

            var webSocketClient = new S2WebSocketClient(
                                      webSocketDetails.WebsocketUrl,
                                      webSocketDetails.WebsocketToken.Value,
                                      sessionOptions,
                                      Options.WebSocketPingInterval,
                                      webSocketCertificateValidator,
                                      Options.RequestTimeout,
                                      DNSClientValue,
                                      TimeProvider,
                                      LoggerFactoryValue
                                  );

            // An S2 message is a few kilobytes; a larger one closes the connection with 1009,
            // so that a peer cannot make this client buffer without bound.
            if (Options.MaxWebSocketMessageSize.HasValue)
            {
                webSocketClient.MaxTextMessageSizeIn   = Options.MaxWebSocketMessageSize;
                webSocketClient.MaxTextMessageSizeOut  = Options.MaxWebSocketMessageSize;
            }

            try
            {

                var session = await webSocketClient.ConnectSessionAsync(CancellationToken).ConfigureAwait(false);

                Logger?.LogInformation("S2 session initiation client: session {SessionId} of {LocalNodeId} with {ServerNodeId} opened.", session.Id, LocalNode.Id, ServerNodeId);

                return new S2ConnectResult(initiation, new S2ConnectSession(webSocketClient, session, initiation));

            }
            catch (S2WebSocketConnectException e)
            {

                await webSocketClient.DisposeAsync().ConfigureAwait(false);

                return new S2ConnectResult(
                           new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater, WebSocketOperation, e.Response.HTTPStatusCode, Pairing: initiation.Pairing,
                                                             Description: $"the WebSocket upgrade was rejected with status {e.Response.HTTPStatusCode.Code}"),
                           null
                       );

            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                await webSocketClient.DisposeAsync().ConfigureAwait(false);
                return new S2ConnectResult(new SessionInitiationClientResult(SessionInitiationOutcome.Cancelled, WebSocketOperation, Pairing: initiation.Pairing), null);
            }
            catch (Exception e)
            {

                await webSocketClient.DisposeAsync().ConfigureAwait(false);

                Logger?.LogWarning(e, "S2 session initiation client: opening the WebSocket session failed.");

                return new S2ConnectResult(
                           new SessionInitiationClientResult(SessionInitiationOutcome.TransportFailure, WebSocketOperation, Pairing: initiation.Pairing, Description: e.Message),
                           null
                       );

            }

        }

        #endregion

        #region UnpairAsync(LocalNodeId, ServerNodeId, CancellationToken = default)

        /// <summary>
        /// Unpair from the communication server (S2 Connect 1.0.0, "Unpairing by the communication
        /// client"): the caller closes the session first; then POST /unpair with a candidate access
        /// token, and the local security material is removed (also when the server answers 401,
        /// i.e. the nodes were already unpaired).
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="ServerNodeId">The identification of the node at the communication server.</param>
        /// <param name="CancellationToken">A token to cancel the procedure.</param>
        public async Task<SessionInitiationClientResult> UnpairAsync(Node_Id            LocalNodeId,
                                                                     Node_Id            ServerNodeId,
                                                                     CancellationToken  CancellationToken   = default)
        {

            SessionInitiationClientResult result;

            try
            {
                result = await RunUnpairAsync(LocalNodeId, ServerNodeId, CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                result = new SessionInitiationClientResult(SessionInitiationOutcome.Cancelled, UnpairOperation, Description: "the procedure was cancelled");
            }

            Logger?.LogInformation("S2 session initiation client: unpair {LocalNodeId} from {ServerNodeId}: {Result}", LocalNodeId, ServerNodeId, result);

            await OnCompleted.InvokeAllAsync(handler => handler(TimeProvider.GetUtcNow(), this, LocalNodeId, ServerNodeId, result), Logger).ConfigureAwait(false);

            return result;

        }

        #endregion

        #region (private) RunUnpairAsync(LocalNodeId, ServerNodeId, CancellationToken)

        private async Task<SessionInitiationClientResult> RunUnpairAsync(Node_Id            LocalNodeId,
                                                                         Node_Id            ServerNodeId,
                                                                         CancellationToken  CancellationToken)
        {

            IReadOnlyList<AccessToken> candidates;

            try
            {

                var pairing = await Store.GetPairingAsync(LocalNodeId, ServerNodeId, CancellationToken).ConfigureAwait(false);

                if (pairing is null)
                {

                    var unpairedAt = await Store.GetUnpairedAtAsync(LocalNodeId, ServerNodeId, CancellationToken).ConfigureAwait(false);

                    return unpairedAt.HasValue
                               ? new SessionInitiationClientResult(SessionInitiationOutcome.AlreadyUnpaired,      UnpairOperation, Description: $"already unpaired at {unpairedAt.Value.ToS2Timestamp()}")
                               : new SessionInitiationClientResult(SessionInitiationOutcome.InvalidConfiguration, UnpairOperation, Description: "the nodes are not paired");

                }

                candidates = await Store.GetAccessTokenCandidatesAsync(LocalNodeId, ServerNodeId, CancellationToken).ConfigureAwait(false);

            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                return new SessionInitiationClientResult(SessionInitiationOutcome.StoreFailure, UnpairOperation, Description: e.Message);
            }

            ForgetVersion();

            if (await EnsureVersionAsync(CancellationToken).ConfigureAwait(false) is { } versionFailure)
                return VersionResult(versionFailure);

            var body = new UnpairRequest(LocalNodeId, ServerNodeId).ToJSON();

            foreach (var candidate in candidates)
            {

                var result = await SendAsyncWithRetries(UnpairOperation, body, HTTPBearerAuthentication.Parse(candidate.Value), CancellationToken).ConfigureAwait(false);

                if (result.CertificateChanged)
                    return new SessionInitiationClientResult(SessionInitiationOutcome.CertificateChanged, UnpairOperation, StatusCode: result.StatusCode, Description: result.FailureText);

                if (result.IsTransportFailure)
                    return new SessionInitiationClientResult(SessionInitiationOutcome.TransportFailure, UnpairOperation, StatusCode: result.StatusCode, Description: result.FailureText);

                if (result.StatusCode == HTTPStatusCode.Unauthorized)
                    continue;

                if (result.StatusCode == HTTPStatusCode.NoContent)
                {
                    await RemoveLocalPairingAsync(LocalNodeId, ServerNodeId, CancellationToken).ConfigureAwait(false);
                    return new SessionInitiationClientResult(SessionInitiationOutcome.Success, UnpairOperation, result.StatusCode);
                }

                return new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater, UnpairOperation, result.StatusCode, Description: $"unexpected status code {result.StatusCode.Code}");

            }

            // 401 for every candidate: the nodes are already unpaired at the server.
            await RemoveLocalPairingAsync(LocalNodeId, ServerNodeId, CancellationToken).ConfigureAwait(false);

            return new SessionInitiationClientResult(SessionInitiationOutcome.AlreadyUnpaired, UnpairOperation, HTTPStatusCode.Unauthorized,
                                                     Description: "the server answered 401: the nodes were already unpaired; the local security material was removed");

        }

        private async Task RemoveLocalPairingAsync(Node_Id            LocalNodeId,
                                                   Node_Id            ServerNodeId,
                                                   CancellationToken  CancellationToken)
        {

            try
            {
                await Store.UnpairAsync(LocalNodeId, ServerNodeId, TimeProvider.GetUtcNow(), CancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Logger?.LogError(e, "S2 session initiation client: removing the local security material failed.");
            }

        }

        #endregion


        // Helpers

        #region (private) SendAsyncWithRetries(Operation, Body, Bearer, CancellationToken)

        private Task<SendResult> SendAsyncWithRetries(String                    Operation,
                                                      JToken?                   Body,
                                                      HTTPBearerAuthentication  Bearer,
                                                      CancellationToken         CancellationToken)

            => SendWithDeadlineAsync(Operation,
                                     Body,
                                     Bearer,
                                     Options.RequestTimeout,
                                     null,
                                     Options.MaxServiceUnavailableRetries,
                                     Options.ServiceUnavailableRetryDelay,
                                     CancellationToken);

        #endregion

        #region (private) TryParseError(Result) / VersionResult(Failure, Operation = ...)

        private CommunicationDetailsErrorMessage? TryParseError(SendResult Result)

            => TryReadJSON(Result, out var json, out _) &&
               json is JObject jsonObject &&
               CommunicationDetailsErrorMessage.TryParse(jsonObject, out var error, out _, Options.ParserOptions)
                   ? error
                   : null;

        private static SessionInitiationClientResult VersionResult(ClientOperationResult<IReadOnlyList<String>>  Failure,
                                                                   String                                        Operation   = VersionIndexOperation)
        {

            if (Failure.NoCommonAPIVersion)
                return new SessionInitiationClientResult(SessionInitiationOutcome.NoCommonAPIVersion, Operation, Failure.StatusCode, Description: Failure.Description);

            return Failure.IsTransportFailure
                       ? new SessionInitiationClientResult(SessionInitiationOutcome.TransportFailure, Operation, Failure.StatusCode, Description: Failure.Description)
                       : new SessionInitiationClientResult(SessionInitiationOutcome.RetryLater,       Operation, Failure.StatusCode, Description: Failure.Description);

        }

        #endregion

    }

}

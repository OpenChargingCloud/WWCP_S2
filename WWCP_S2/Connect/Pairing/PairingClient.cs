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

using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.TLS;

using IPAddress = System.Net.IPAddress;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The S2 Connect pairing client (S2 Connect 1.0.0, "Pairing process"): an HTTPS client of
    /// the pairing API of one remote endpoint that selects the API version, runs the complete
    /// pairing interaction for a local node (steps 1 to 10, including the client checks and
    /// the failure edges of the specification) and the LAN-only operations (endpoint, nodes,
    /// preparePairing, cancelPreparePairing, waitForPairing). The TLS server certificate is
    /// observed on every handshake; its fingerprint is the "F" of the LAN challenge-response
    /// formula and must not change during an attempt.
    /// </summary>
    public class PairingClient : AS2ConnectClient
    {

        #region Data

        /// <summary>
        /// The default HTTP user agent.
        /// </summary>
        public new const String  DefaultHTTPUserAgent  = "GraphDefined S2 Connect Pairing Client";

        private const String RequestPairingOperation            = "requestPairing";
        private const String RequestConnectionDetailsOperation  = "requestConnectionDetails";
        private const String PostConnectionDetailsOperation     = "postConnectionDetails";
        private const String FinalizePairingOperation           = "finalizePairing";
        private const String VersionIndexOperation              = "versionIndex";

        #endregion

        #region Properties

        /// <summary>
        /// The pairing URL of the remote endpoint.
        /// </summary>
        public S2BaseURL             PairingUrl
            => BaseUrl;

        /// <summary>
        /// The deployment of the pairing server, which selects the challenge-response formula:
        /// LAN uses the server certificate fingerprint, WAN the domain name.
        /// </summary>
        public Deployment            PairingServerDeployment    { get; }

        /// <summary>
        /// The local endpoint with the nodes that pair.
        /// </summary>
        public LocalEndpoint         LocalEndpoint              { get; }

        /// <summary>
        /// The store the completed pairings are persisted in.
        /// </summary>
        public IS2Store              Store                      { get; }

        /// <summary>
        /// The options of this client.
        /// </summary>
        public PairingClientOptions  Options                    { get; }

        /// <summary>
        /// Whether the LAN formula R = HMAC(C, T || F) is used.
        /// </summary>
        public Boolean               UsesLANChallengeResponse
            => PairingServerDeployment == Deployment.LAN;

        /// <inheritdoc/>
        protected override TimeSpan  VersionIndexTimeout
            => Options.RequestTimeout;

        #endregion

        #region Events

        /// <summary>
        /// Raised when a pairing attempt starts.
        /// </summary>
        public event OnPairingClientStartedDelegate?    OnPairingStarted;

        /// <summary>
        /// Raised when a pairing attempt ended.
        /// </summary>
        public event OnPairingClientCompletedDelegate?  OnPairingCompleted;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new pairing client.
        /// </summary>
        /// <param name="PairingUrl">The pairing URL of the remote endpoint.</param>
        /// <param name="LocalEndpoint">The local endpoint with the nodes that pair.</param>
        /// <param name="Store">The store for completed pairings.</param>
        /// <param name="PairingServerDeployment">The deployment of the pairing server (default: LAN for ".local" hosts and IP addresses, WAN otherwise).</param>
        /// <param name="Options">Optional client options.</param>
        /// <param name="AssumedServerCertificateFingerprint">The server certificate fingerprint to use when none is observed via TLS (plain HTTP only).</param>
        /// <param name="RemoteCertificateValidator">An optional custom TLS server certificate validator (default: accept self-signed certificates when the options say so, otherwise the OS).</param>
        /// <param name="TimeProvider">An optional time provider (default: the time provider of the endpoint).</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        /// <param name="DNSClient">An optional DNS client.</param>
        /// <param name="HTTPUserAgent">An optional HTTP user agent.</param>
        /// <param name="Description">An optional description.</param>
        /// <param name="DisableLogging">Whether to disable Hermod's client logging (default: true).</param>
        public PairingClient(S2BaseURL                                                  PairingUrl,
                             LocalEndpoint                                              LocalEndpoint,
                             IS2Store                                                   Store,
                             Deployment?                                                PairingServerDeployment               = null,
                             PairingClientOptions?                                      Options                               = null,
                             CertificateFingerprint?                                    AssumedServerCertificateFingerprint   = null,
                             RemoteTLSServerCertificateValidationHandler<IHTTPClient>?  RemoteCertificateValidator            = null,
                             TimeProvider?                                              TimeProvider                          = null,
                             ILoggerFactory?                                            LoggerFactory                         = null,
                             IDNSClient?                                                DNSClient                             = null,
                             String?                                                    HTTPUserAgent                         = null,
                             I18NString?                                                Description                           = null,
                             Boolean?                                                   DisableLogging                        = null)

            : base(PairingUrl,
                   (Options ?? PairingClientOptions.Default).SupportedAPIVersions,
                   (Options ?? PairingClientOptions.Default).ParserOptions,
                   (Options ?? PairingClientOptions.Default).AcceptSelfSignedCertificates,
                   AssumedServerCertificateFingerprint,
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

            this.LocalEndpoint            = LocalEndpoint;
            this.Store                    = Store;
            this.Options                  = Options ?? PairingClientOptions.Default;
            this.Options.Validate();

            this.PairingServerDeployment  = PairingServerDeployment ?? GuessDeployment(PairingUrl);

            if (this.PairingServerDeployment != Deployment.LAN && this.PairingServerDeployment != Deployment.WAN)
                throw new ArgumentException("The deployment of the pairing server must be LAN or WAN!", nameof(PairingServerDeployment));

        }

        #endregion


        #region Clone()

        /// <summary>
        /// Create a second client with the same configuration and its own connection to the
        /// same server, e.g. for long-polling while pairing attempts run (Hermod serialises the
        /// requests of one client). The known API versions are copied.
        /// </summary>
        public PairingClient Clone()
        {

            var clone = new PairingClient(PairingUrl,
                                          LocalEndpoint,
                                          Store,
                                          PairingServerDeployment,
                                          Options,
                                          AssumedServerCertificateFingerprint,
                                          CustomCertificateValidator,
                                          TimeProvider,
                                          LoggerFactoryValue,
                                          DNSClientValue,
                                          HTTPUserAgentValue,
                                          DescriptionValue,
                                          DisableLoggingValue);

            clone.CopyVersionsFrom(this);

            return clone;

        }

        #endregion

        #region (static) GuessDeployment(PairingUrl)

        /// <summary>
        /// Guess the deployment of a pairing server from its URL: ".local" hosts, "localhost"
        /// and IP addresses are LAN, everything else WAN (S2 Connect 1.0.0, "Addressing endpoints").
        /// </summary>
        /// <param name="PairingUrl">The pairing URL.</param>
        public static Deployment GuessDeployment(S2BaseURL PairingUrl)
        {

            var host = PairingUrl.Host.TrimEnd('.');

            if (host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
                host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                IPAddress.TryParse(host.Trim('[', ']'), out _))
            {
                return Deployment.LAN;
            }

            return Deployment.WAN;

        }

        #endregion


        #region PairAsync(LocalNode, PairingToken, Target = null, ForcePairing = false, CancellationToken = default)

        /// <summary>
        /// Run the complete pairing interaction for the given local node (S2 Connect 1.0.0,
        /// "Pairing interaction", steps 1 to 10): the version is selected when not yet known,
        /// then requestPairing, the client checks of the response, requestConnectionDetails or
        /// postConnectionDetails depending on the communication roles, and finalizePairing.
        /// A successful pairing is stored and the pairing token consumed.
        /// </summary>
        /// <param name="LocalNode">The local node that pairs.</param>
        /// <param name="PairingToken">The pairing token shared with the targeted node (entered by the end user, or the own token when the server node is the Initiator).</param>
        /// <param name="Target">The targeted node at the server (default: the only node of the endpoint).</param>
        /// <param name="ForcePairing">Whether to pair despite incompatible S2 message versions or communication protocols.</param>
        /// <param name="CancellationToken">A token to cancel the attempt.</param>
        public async Task<PairingClientResult> PairAsync(HostedNode         LocalNode,
                                                         PairingToken       PairingToken,
                                                         PairingTarget?     Target              = null,
                                                         Boolean            ForcePairing        = false,
                                                         CancellationToken  CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(LocalNode);

            if (PairingToken.Length == 0)
                throw new ArgumentException("The pairing token must not be empty!", nameof(PairingToken));

            var target = Target ?? PairingTarget.Any;
            var now    = TimeProvider.GetUtcNow();

            await OnPairingStarted.InvokeAllAsync(handler => handler(now, this, LocalNode, target), Logger).ConfigureAwait(false);

            PairingClientResult result;

            try
            {
                result = await RunPairingAsync(LocalNode, PairingToken, target, ForcePairing, CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                result = new PairingClientResult(PairingClientOutcome.Cancelled, RequestPairingOperation, Description: "the attempt was cancelled");
            }

            Logger?.LogInformation("S2 pairing client: {Node} -> {PairingUrl}: {Result}", LocalNode.Id, PairingUrl.Value, result);

            await OnPairingCompleted.InvokeAllAsync(handler => handler(TimeProvider.GetUtcNow(), this, LocalNode, result), Logger).ConfigureAwait(false);

            return result;

        }

        #endregion

        #region (private) RunPairingAsync(...)

        private async Task<PairingClientResult> RunPairingAsync(HostedNode         LocalNode,
                                                                PairingToken       PairingToken,
                                                                PairingTarget      Target,
                                                                Boolean            ForcePairing,
                                                                CancellationToken  CancellationToken)
        {

            #region 0. Preconditions: API version, certificate fingerprint

            if (await EnsureVersionAsync(CancellationToken).ConfigureAwait(false) is { } versionFailure)
            {

                if (versionFailure.NoCommonAPIVersion)
                    return new PairingClientResult(PairingClientOutcome.NoCommonAPIVersion, VersionIndexOperation, StatusCode: versionFailure.StatusCode, Description: versionFailure.Description);

                return versionFailure.IsTransportFailure
                           ? new PairingClientResult(PairingClientOutcome.TransportFailure, VersionIndexOperation, StatusCode: versionFailure.StatusCode, Description: versionFailure.Description)
                           : new PairingClientResult(PairingClientOutcome.InvalidResponse,  VersionIndexOperation, StatusCode: versionFailure.StatusCode, Description: versionFailure.Description);

            }

            var attemptFingerprint = ServerCertificateFingerprint;

            if (UsesLANChallengeResponse && !attemptFingerprint.HasValue)
                return new PairingClientResult(PairingClientOutcome.InvalidConfiguration, RequestPairingOperation,
                                               Description: "the fingerprint of the server certificate is unknown, but the LAN challenge-response formula requires it");

            #endregion

            #region 1. POST requestPairing

            var clientChallenge  = TokenGenerator.NewChallenge();

            var request          = new RequestPairingRequest(
                                       LocalNode.Description,
                                       LocalEndpoint.Description,
                                       LocalNode.SupportedCommunicationProtocols,
                                       LocalNode.SupportedS2MessageVersions,
                                       Options.SupportedHmacHashingAlgorithms,
                                       clientChallenge,
                                       Target,
                                       ForcePairing
                                   );

            var result1          = await SendWithRetriesAsync(RequestPairingOperation, request.ToJSON(), null, Options.RequestPairingTimeout, null, CancellationToken).ConfigureAwait(false);

            if (Decide(result1, RequestPairingOperation, out var failure1))
                return failure1;

            if (result1.StatusCode != HTTPStatusCode.OK)
                return new PairingClientResult(PairingClientOutcome.InvalidResponse, RequestPairingOperation, StatusCode: result1.StatusCode,
                                               Description: $"unexpected status code {result1.StatusCode.Code}");

            #endregion

            #region 3. Parse and check the response

            if (!TryReadJSON(result1, out var json1, out var jsonError) || json1 is not JObject responseJSON)
                return new PairingClientResult(PairingClientOutcome.InvalidResponse, RequestPairingOperation, StatusCode: result1.StatusCode,
                                               Description: $"the requestPairing response is not a JSON object: {jsonError ?? "wrong JSON type"}");

            if (!RequestPairingResponse.TryParse(responseJSON, out var serverResponse, out var parseError, Options.ParserOptions))
            {

                // "Is the response formatted according to the schema? -> call /finalizePairing where success is false if pairingAttemptId is available"
                if (responseJSON["pairingAttemptId"]?.Type == JTokenType.String &&
                    PairingAttemptId.TryParse(responseJSON["pairingAttemptId"]!.Value<String>()!, out var availableId))
                {
                    await TryFinalizeFalseAsync(availableId, CancellationToken).ConfigureAwait(false);
                }

                return new PairingClientResult(PairingClientOutcome.InvalidResponse, RequestPairingOperation, StatusCode: result1.StatusCode,
                                               Description: $"the requestPairing response violates the schema: {parseError}");

            }

            // The client deadline starts when the 200 of requestPairing was parsed.
            var deadline          = TimeProvider.GetUtcNow() + Options.PairingAttemptTimeout;
            var pairingAttemptId  = serverResponse.PairingAttemptId;

            if (serverResponse.ServerNodeDescription.Role == LocalNode.Role)
            {
                await TryFinalizeFalseAsync(pairingAttemptId, CancellationToken).ConfigureAwait(false);
                return new PairingClientResult(PairingClientOutcome.IncompatibleRole, RequestPairingOperation, ServerResponse: serverResponse, StatusCode: result1.StatusCode,
                                               Description: $"the node at the server has the same role ({LocalNode.Role}) as the local node");
            }

            if (!Options.SupportedHmacHashingAlgorithms.Contains(serverResponse.SelectedHmacHashingAlgorithm))
            {
                await TryFinalizeFalseAsync(pairingAttemptId, CancellationToken).ConfigureAwait(false);
                return new PairingClientResult(PairingClientOutcome.InvalidResponse, RequestPairingOperation, ServerResponse: serverResponse, StatusCode: result1.StatusCode,
                                               Description: $"the server selected the HMAC hashing algorithm '{serverResponse.SelectedHmacHashingAlgorithm}', which was not offered");
            }

            var serverDeployment = serverResponse.ServerEndpointDescription.Deployment ?? Options.DefaultServerDeployment;

            if (!serverDeployment.HasValue || (serverDeployment.Value != Deployment.LAN && serverDeployment.Value != Deployment.WAN))
            {
                await TryFinalizeFalseAsync(pairingAttemptId, CancellationToken).ConfigureAwait(false);
                return new PairingClientResult(PairingClientOutcome.InvalidResponse, RequestPairingOperation, ServerResponse: serverResponse, StatusCode: result1.StatusCode,
                                               Description: "the deployment of the server endpoint is missing or unknown");
            }

            #endregion

            #region 4. Check clientHmacChallengeResponse

            var algorithm = serverResponse.SelectedHmacHashingAlgorithm;

            if (!TryComputeChallengeResponse(algorithm, clientChallenge, PairingToken, attemptFingerprint, out var expectedClientResponse, out var computeError))
            {
                await TryFinalizeFalseAsync(pairingAttemptId, CancellationToken).ConfigureAwait(false);
                return new PairingClientResult(PairingClientOutcome.InvalidConfiguration, RequestPairingOperation, ServerResponse: serverResponse, Description: computeError);
            }

            if (!serverResponse.ClientHmacChallengeResponse.ConstantTimeEquals(expectedClientResponse))
            {
                await TryFinalizeFalseAsync(pairingAttemptId, CancellationToken).ConfigureAwait(false);
                return new PairingClientResult(PairingClientOutcome.ChallengeResponseMismatch, RequestPairingOperation, ServerResponse: serverResponse, StatusCode: result1.StatusCode,
                                               Description: "the clientHmacChallengeResponse of the server is wrong: the pairing tokens differ or the server is not the expected one");
            }

            #endregion

            #region 5. Calculate serverHmacChallengeResponse, decide the branch

            if (!TryComputeChallengeResponse(algorithm, serverResponse.ServerHmacChallenge, PairingToken, attemptFingerprint, out var serverChallengeResponse, out computeError))
            {
                await TryFinalizeFalseAsync(pairingAttemptId, CancellationToken).ConfigureAwait(false);
                return new PairingClientResult(PairingClientOutcome.InvalidConfiguration, RequestPairingOperation, ServerResponse: serverResponse, Description: computeError);
            }

            var localCommunicationRole  = CommunicationRoleExtensions.Determine(LocalNode.Role, LocalEndpoint.Deployment, serverDeployment.Value);
            var bearer                  = HTTPBearerAuthentication.Parse(pairingAttemptId.Value);

            AccessToken?        accessToken      = null;
            ConnectionDetails?  receivedDetails  = null;

            #endregion

            if (localCommunicationRole == CommunicationRole.CommunicationClient)
            {

                #region 6A. POST requestConnectionDetails

                var result6A = await SendWithRetriesAsync(RequestConnectionDetailsOperation,
                                                          new RequestConnectionDetailsRequest(serverChallengeResponse).ToJSON(),
                                                          bearer,
                                                          Options.RequestTimeout,
                                                          deadline,
                                                          CancellationToken).ConfigureAwait(false);

                if (Decide(result6A, RequestConnectionDetailsOperation, out var failure6A, serverResponse))
                    return failure6A;

                if (result6A.StatusCode != HTTPStatusCode.OK)
                    return new PairingClientResult(PairingClientOutcome.InvalidResponse, RequestConnectionDetailsOperation, ServerResponse: serverResponse, StatusCode: result6A.StatusCode,
                                                   Description: $"unexpected status code {result6A.StatusCode.Code}");

                String? detailsError = null;

                if (!TryReadJSON(result6A, out var json6A, out jsonError) || json6A is not JObject detailsJSON ||
                    !ConnectionDetails.TryParse(detailsJSON, out receivedDetails, out detailsError, Options.ParserOptions))
                {
                    await TryFinalizeFalseAsync(pairingAttemptId, CancellationToken).ConfigureAwait(false);
                    return new PairingClientResult(PairingClientOutcome.InvalidResponse, RequestConnectionDetailsOperation, ServerResponse: serverResponse, StatusCode: result6A.StatusCode,
                                                   Description: $"the connection details could not be parsed: {jsonError ?? detailsError ?? "a JSON object is expected"}");
                }

                accessToken = receivedDetails.AccessToken;

                #endregion

            }

            else
            {

                #region 6B. POST postConnectionDetails

                if (LocalEndpoint.SessionInitiationUrl is null)
                {
                    await TryFinalizeFalseAsync(pairingAttemptId, CancellationToken).ConfigureAwait(false);
                    return new PairingClientResult(PairingClientOutcome.InvalidConfiguration, PostConnectionDetailsOperation, ServerResponse: serverResponse,
                                                   Description: "the local endpoint becomes the communication server, but has no session initiation URL");
                }

                var caFingerprint = LocalEndpoint.CACertificateFingerprint;

                if (!caFingerprint.HasValue)
                {
                    await TryFinalizeFalseAsync(pairingAttemptId, CancellationToken).ConfigureAwait(false);
                    return new PairingClientResult(PairingClientOutcome.InvalidConfiguration, PostConnectionDetailsOperation, ServerResponse: serverResponse,
                                                   Description: "the local endpoint becomes the communication server, but has no CA certificate fingerprint");
                }

                accessToken = TokenGenerator.NewAccessToken();

                var connectionDetails = new ConnectionDetails(
                                            LocalEndpoint.SessionInitiationUrl.Value,
                                            accessToken.Value,
                                            new Dictionary<String, CertificateFingerprint> {
                                                [S2ConnectDefaults.CertificateFingerprintSHA256Key] = caFingerprint.Value
                                            }
                                        );

                var result6B = await SendWithRetriesAsync(PostConnectionDetailsOperation,
                                                          new PostConnectionDetailsRequest(serverChallengeResponse, connectionDetails).ToJSON(),
                                                          bearer,
                                                          Options.RequestTimeout,
                                                          deadline,
                                                          CancellationToken).ConfigureAwait(false);

                if (Decide(result6B, PostConnectionDetailsOperation, out var failure6B, serverResponse))
                    return failure6B;

                if (result6B.StatusCode != HTTPStatusCode.NoContent)
                    return new PairingClientResult(PairingClientOutcome.InvalidResponse, PostConnectionDetailsOperation, ServerResponse: serverResponse, StatusCode: result6B.StatusCode,
                                                   Description: $"unexpected status code {result6B.StatusCode.Code}");

                #endregion

            }

            #region 9. POST finalizePairing(success = true)

            var result9 = await SendWithRetriesAsync(FinalizePairingOperation,
                                                     new FinalizePairingRequest(true).ToJSON(),
                                                     bearer,
                                                     Options.RequestTimeout,
                                                     deadline,
                                                     CancellationToken).ConfigureAwait(false);

            if (Decide(result9, FinalizePairingOperation, out var failure9, serverResponse))
                return failure9;

            if (result9.StatusCode != HTTPStatusCode.NoContent)
                return new PairingClientResult(PairingClientOutcome.InvalidResponse, FinalizePairingOperation, ServerResponse: serverResponse, StatusCode: result9.StatusCode,
                                               Description: $"unexpected status code {result9.StatusCode.Code}");

            #endregion

            #region 10. Store the pairing

            var serverEndpointDescription = serverResponse.ServerEndpointDescription.Deployment.HasValue
                                                ? serverResponse.ServerEndpointDescription
                                                : new EndpointDescription(serverResponse.ServerEndpointDescription.Name,
                                                                          serverResponse.ServerEndpointDescription.LogoUrl,
                                                                          serverDeployment.Value);

            var pairing = new Pairing(
                              LocalNode.Id,
                              serverResponse.ServerNodeDescription,
                              serverEndpointDescription,
                              localCommunicationRole,
                              accessToken!.Value,
                              TimeProvider.GetUtcNow(),
                              receivedDetails?.InitiateSessionUrl,
                              receivedDetails?.CertificateFingerprints
                          );

            try
            {
                await Store.AddOrReplacePairingAsync(pairing, CancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Logger?.LogError(e, "S2 pairing client: storing the pairing failed.");
                return new PairingClientResult(PairingClientOutcome.StoreFailure, FinalizePairingOperation, ServerResponse: serverResponse, Description: e.Message);
            }

            LocalNode.ConsumePairingToken(serverResponse.ServerNodeDescription.Id, PairingToken);

            return new PairingClientResult(PairingClientOutcome.Success, FinalizePairingOperation, pairing, serverResponse, StatusCode: result9.StatusCode);

            #endregion

        }

        #endregion


        #region GetEndpointAsync(CancellationToken = default)

        /// <summary>
        /// GET /[version]/endpoint (LAN-LAN only): the description of the remote endpoint.
        /// </summary>
        /// <param name="CancellationToken">A token to cancel the request.</param>
        public async Task<ClientOperationResult<EndpointDescription>> GetEndpointAsync(CancellationToken CancellationToken = default)
        {

            if (await EnsureVersionAsync(CancellationToken).ConfigureAwait(false) is { } versionFailure)
                return VersionFailure<EndpointDescription>(versionFailure);

            var result = await SendAsync(HTTPMethod.GET, OperationPath("endpoint"), null, null, Options.RequestTimeout, CancellationToken).ConfigureAwait(false);

            if (result.StatusCode != HTTPStatusCode.OK)
                return PairingFailure<EndpointDescription>(result);

            String? parseError = null;

            if (!TryReadJSON(result, out var json, out var error) || json is not JObject jsonObject ||
                !EndpointDescription.TryParse(jsonObject, out var endpoint, out parseError, Options.ParserOptions))
            {
                return PairingFailure<EndpointDescription>(result, $"the endpoint description could not be parsed: {error ?? parseError ?? "a JSON object is expected"}");
            }

            return new ClientOperationResult<EndpointDescription>(result.StatusCode, true, endpoint);

        }

        #endregion

        #region GetNodesAsync(CancellationToken = default)

        /// <summary>
        /// GET /[version]/nodes (LAN-LAN only): the descriptions of the nodes represented by the remote endpoint.
        /// </summary>
        /// <param name="CancellationToken">A token to cancel the request.</param>
        public async Task<ClientOperationResult<IReadOnlyList<NodeDescription>>> GetNodesAsync(CancellationToken CancellationToken = default)
        {

            if (await EnsureVersionAsync(CancellationToken).ConfigureAwait(false) is { } versionFailure)
                return VersionFailure<IReadOnlyList<NodeDescription>>(versionFailure);

            var result = await SendAsync(HTTPMethod.GET, OperationPath("nodes"), null, null, Options.RequestTimeout, CancellationToken).ConfigureAwait(false);

            if (result.StatusCode != HTTPStatusCode.OK)
                return PairingFailure<IReadOnlyList<NodeDescription>>(result);

            if (!TryReadJSON(result, out var json, out var error) || json is not JArray jsonArray)
                return PairingFailure<IReadOnlyList<NodeDescription>>(result, $"the node list is not a JSON array: {error ?? "wrong JSON type"}");

            var nodes = new List<NodeDescription>();

            foreach (var token in jsonArray)
            {

                if (token is not JObject nodeJSON ||
                    !NodeDescription.TryParse(nodeJSON, out var node, out _, Options.ParserOptions))
                {
                    return PairingFailure<IReadOnlyList<NodeDescription>>(result, "a node description could not be parsed");
                }

                nodes.Add(node);

            }

            return new ClientOperationResult<IReadOnlyList<NodeDescription>>(result.StatusCode, true, nodes);

        }

        #endregion

        #region PreparePairingAsync(LocalNode, ServerNodeId, CancellationToken = default)

        /// <summary>
        /// POST /[version]/preparePairing (LAN-LAN only): tell the server that the end user is
        /// about to pair the local node with the given node at the server.
        /// </summary>
        /// <param name="LocalNode">The local node.</param>
        /// <param name="ServerNodeId">The identification of the targeted node at the server.</param>
        /// <param name="CancellationToken">A token to cancel the request.</param>
        public async Task<ClientOperationResult<Object>> PreparePairingAsync(HostedNode         LocalNode,
                                                                             Node_Id            ServerNodeId,
                                                                             CancellationToken  CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(LocalNode);

            if (await EnsureVersionAsync(CancellationToken).ConfigureAwait(false) is { } versionFailure)
                return VersionFailure<Object>(versionFailure);

            var request  = new PreparePairingRequest(LocalNode.Description, LocalEndpoint.Description, ServerNodeId);
            var result   = await SendAsync(HTTPMethod.POST, OperationPath("preparePairing"), request.ToJSON(), null, Options.RequestTimeout, CancellationToken).ConfigureAwait(false);

            return result.StatusCode == HTTPStatusCode.NoContent
                       ? new ClientOperationResult<Object>(result.StatusCode, true)
                       : PairingFailure<Object>(result);

        }

        #endregion

        #region CancelPreparePairingAsync(LocalNodeId, ServerNodeId, CancellationToken = default)

        /// <summary>
        /// POST /[version]/cancelPreparePairing (LAN-LAN only): the end user no longer intends to pair.
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="ServerNodeId">The identification of the targeted node at the server.</param>
        /// <param name="CancellationToken">A token to cancel the request.</param>
        public async Task<ClientOperationResult<Object>> CancelPreparePairingAsync(Node_Id            LocalNodeId,
                                                                                   Node_Id            ServerNodeId,
                                                                                   CancellationToken  CancellationToken   = default)
        {

            if (await EnsureVersionAsync(CancellationToken).ConfigureAwait(false) is { } versionFailure)
                return VersionFailure<Object>(versionFailure);

            var request  = new CancelPreparePairingRequest(LocalNodeId, ServerNodeId);
            var result   = await SendAsync(HTTPMethod.POST, OperationPath("cancelPreparePairing"), request.ToJSON(), null, Options.RequestTimeout, CancellationToken).ConfigureAwait(false);

            return result.StatusCode == HTTPStatusCode.NoContent
                       ? new ClientOperationResult<Object>(result.StatusCode, true)
                       : PairingFailure<Object>(result);

        }

        #endregion

        #region WaitForPairingAsync(Request, RequestTimeout = null, CancellationToken = default)

        /// <summary>
        /// POST /[version]/waitForPairing (LAN-LAN only): one long-polling request; a 204 is a
        /// success without value, a 200 carries the actions of the server.
        /// </summary>
        /// <param name="Request">The waitForPairing request listing every hosted node.</param>
        /// <param name="RequestTimeout">The request timeout (default: 30 seconds, the minimum of the specification).</param>
        /// <param name="CancellationToken">A token to cancel the request.</param>
        public async Task<ClientOperationResult<WaitForPairingResponse>> WaitForPairingAsync(WaitForPairingRequest  Request,
                                                                                             TimeSpan?              RequestTimeout      = null,
                                                                                             CancellationToken      CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Request);

            var timeout = RequestTimeout ?? S2ConnectDefaults.LongPollingClientTimeout;

            if (timeout < S2ConnectDefaults.LongPollingClientTimeout)
                throw new ArgumentOutOfRangeException(nameof(RequestTimeout), $"The long-polling request timeout must be at least {S2ConnectDefaults.LongPollingClientTimeout.TotalSeconds} seconds!");

            if (await EnsureVersionAsync(CancellationToken).ConfigureAwait(false) is { } versionFailure)
                return VersionFailure<WaitForPairingResponse>(versionFailure);

            var result = await SendAsync(HTTPMethod.POST, OperationPath("waitForPairing"), Request.ToJSON(), null, timeout, CancellationToken).ConfigureAwait(false);

            if (result.StatusCode == HTTPStatusCode.NoContent)
                return new ClientOperationResult<WaitForPairingResponse>(result.StatusCode, true);

            if (result.StatusCode != HTTPStatusCode.OK)
                return PairingFailure<WaitForPairingResponse>(result);

            String? parseError = null;

            if (!TryReadJSON(result, out var json, out var error) || json is not JArray jsonArray ||
                !WaitForPairingResponse.TryParse(jsonArray, out var actions, out parseError, Options.ParserOptions))
            {
                return PairingFailure<WaitForPairingResponse>(result, $"the waitForPairing response could not be parsed: {error ?? parseError ?? "a JSON array is expected"}");
            }

            return new ClientOperationResult<WaitForPairingResponse>(result.StatusCode, true, actions);

        }

        #endregion


        // Helpers

        #region (private) TryComputeChallengeResponse(...)

        private Boolean TryComputeChallengeResponse(HmacHashingAlgorithm              Algorithm,
                                                    HmacChallenge                     Challenge,
                                                    PairingToken                      Token,
                                                    CertificateFingerprint?           ServerFingerprint,
                                                    out HmacChallengeResponse         Response,
                                                    [NotNullWhen(false)] out String?  Error)
        {

            try
            {

                if (UsesLANChallengeResponse)
                {

                    if (!ServerFingerprint.HasValue)
                    {
                        Response  = default;
                        Error     = "the fingerprint of the server certificate is unknown";
                        return false;
                    }

                    Response  = ChallengeResponse.ComputeForLAN(Algorithm, Challenge, Token, ServerFingerprint.Value);
                    Error     = null;
                    return true;

                }

                Response  = ChallengeResponse.ComputeForWAN(Algorithm, Challenge, Token, ServerDomainName);
                Error     = null;
                return true;

            }
            catch (NotSupportedException e)
            {
                Response  = default;
                Error     = e.Message;
                return false;
            }

        }

        #endregion

        #region (private) SendWithRetriesAsync(...)

        private Task<SendResult> SendWithRetriesAsync(String                     Operation,
                                                      JToken                     Body,
                                                      HTTPBearerAuthentication?  Bearer,
                                                      TimeSpan                   RequestTimeout,
                                                      DateTimeOffset?            Deadline,
                                                      CancellationToken          CancellationToken)

            => SendWithDeadlineAsync(Operation,
                                     Body,
                                     Bearer,
                                     RequestTimeout,
                                     Deadline,
                                     Options.MaxServiceUnavailableRetries,
                                     Options.ServiceUnavailableRetryDelay,
                                     CancellationToken);

        #endregion

        #region (private) TryFinalizeFalseAsync(Id, CancellationToken)

        /// <summary>
        /// Best effort: inform the server that the attempt failed (finalizePairing with success = false).
        /// </summary>
        private async Task TryFinalizeFalseAsync(PairingAttemptId   Id,
                                                 CancellationToken  CancellationToken)
        {

            try
            {

                var result = await SendAsync(HTTPMethod.POST,
                                             OperationPath(FinalizePairingOperation),
                                             new FinalizePairingRequest(false).ToJSON(),
                                             HTTPBearerAuthentication.Parse(Id.Value),
                                             Options.RequestTimeout,
                                             CancellationToken).ConfigureAwait(false);

                Logger?.LogDebug("S2 pairing client: finalizePairing(success = false) answered {StatusCode}.", result.StatusCode.Code);

            }
            catch (OperationCanceledException)
            {
                // The attempt is over anyway.
            }

        }

        #endregion

        #region (private) Decide(Result, Operation, out Failure, ServerResponse = null)

        /// <summary>
        /// Map the outcomes that end an attempt to results: transport failures, a changed
        /// certificate, the deadline, 400 (rejected, with the error message when given), 401,
        /// 403 and 503.
        /// </summary>
        private Boolean Decide(SendResult                                    Result,
                               String                                        Operation,
                               [NotNullWhen(true)] out PairingClientResult?  Failure,
                               RequestPairingResponse?                       ServerResponse   = null)
        {

            if (Result.CertificateChanged)
            {
                Failure = new PairingClientResult(PairingClientOutcome.CertificateChanged, Operation, ServerResponse: ServerResponse, StatusCode: Result.StatusCode, Description: Result.FailureText);
                return true;
            }

            if (Result.DeadlineExceeded)
            {
                Failure = new PairingClientResult(PairingClientOutcome.Timeout, Operation, ServerResponse: ServerResponse, StatusCode: Result.StatusCode, Description: Result.FailureText);
                return true;
            }

            if (Result.IsTransportFailure)
            {
                Failure = new PairingClientResult(PairingClientOutcome.TransportFailure, Operation, ServerResponse: ServerResponse, StatusCode: Result.StatusCode,
                                                  Description: Result.FailureText ?? "the pairing server could not be reached");
                return true;
            }

            var status = Result.StatusCode;

            if (status == HTTPStatusCode.BadRequest)
            {
                Failure = new PairingClientResult(PairingClientOutcome.Rejected, Operation, ServerResponse: ServerResponse, Error: TryParseError(Result), StatusCode: status,
                                                  Description: "the server rejected the request");
                return true;
            }

            if (status == HTTPStatusCode.Unauthorized)
            {
                Failure = new PairingClientResult(PairingClientOutcome.Unauthorized, Operation, ServerResponse: ServerResponse, StatusCode: status,
                                                  Description: "the server did not accept the pairingAttemptId; restart at requestPairing");
                return true;
            }

            if (status == HTTPStatusCode.Forbidden)
            {
                Failure = new PairingClientResult(PairingClientOutcome.Forbidden, Operation, ServerResponse: ServerResponse, StatusCode: status,
                                                  Description: "the server did not accept the serverHmacChallengeResponse; the pairing tokens differ");
                return true;
            }

            // 429 is reported like 503: a rate limited server is a temporarily unavailable server.
            if (status == HTTPStatusCode.ServiceUnavailable ||
                status == HTTPStatusCode.TooManyRequests)
            {
                Failure = new PairingClientResult(PairingClientOutcome.ServiceUnavailable, Operation, ServerResponse: ServerResponse, StatusCode: status,
                                                  Description: "the server is temporarily not able to process the request");
                return true;
            }

            Failure = null;
            return false;

        }

        #endregion

        #region (private) TryParseError / PairingFailure<T>

        private PairingResponseErrorMessage? TryParseError(SendResult Result)

            => TryReadJSON(Result, out var json, out _) &&
               json is JObject jsonObject &&
               PairingResponseErrorMessage.TryParse(jsonObject, out var error, out _, Options.ParserOptions)
                   ? error
                   : null;

        private ClientOperationResult<T> PairingFailure<T>(SendResult  Result,
                                                           String?     Description   = null)
            where T : class

            => Failure<T>(Result,
                          Description,
                          Result.StatusCode == HTTPStatusCode.BadRequest ? TryParseError(Result) : null);

        #endregion

    }

}

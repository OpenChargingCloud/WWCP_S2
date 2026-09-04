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

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The options of a long-polling client.
    /// </summary>
    public sealed record LongPollingClientOptions
    {

        #region Properties

        /// <summary>
        /// The request timeout of waitForPairing (default: 30 seconds; S2 Connect 1.0.0,
        /// "Long-polling": "The client must use a request time-out of at least 30 seconds").
        /// </summary>
        public TimeSpan        RequestTimeout             { get; init; } = S2ConnectDefaults.LongPollingClientTimeout;

        /// <summary>
        /// The delay before the next request after a 500 response or a transport failure (default: 5 seconds).
        /// </summary>
        public TimeSpan        ServerErrorDelay           { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The delay before the next request after a 503 response without a Retry-After header (default: 2 seconds).
        /// </summary>
        public TimeSpan        ServiceUnavailableDelay    { get; init; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Whether the action "requestPairing" starts a pairing attempt automatically with the
        /// own pairing token of the node (default: true).
        /// </summary>
        public Boolean         AutoPair                   { get; init; } = true;

        /// <summary>
        /// The targeted node at the server for automatic pairing attempts (default: the only
        /// node of the server endpoint).
        /// </summary>
        public PairingTarget?  PairingTarget              { get; init; }

        /// <summary>
        /// Whether automatic pairing attempts use forcePairing (default: false).
        /// </summary>
        public Boolean         ForcePairing               { get; init; }

        #endregion

        #region (static) Default

        /// <summary>
        /// The default options.
        /// </summary>
        public static LongPollingClientOptions Default { get; } = new ();

        #endregion


        #region Validate()

        /// <summary>
        /// Validate the options; throws when a value is out of range.
        /// </summary>
        public void Validate()
        {

            if (RequestTimeout < S2ConnectDefaults.LongPollingClientTimeout)
                throw new ArgumentOutOfRangeException(nameof(RequestTimeout), $"The long-polling request timeout must be at least {S2ConnectDefaults.LongPollingClientTimeout.TotalSeconds} seconds!");

            if (ServerErrorDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ServerErrorDelay), "The server error delay must not be negative!");

            if (ServiceUnavailableDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ServiceUnavailableDelay), "The service unavailable delay must not be negative!");

        }

        #endregion

    }


    /// <summary>
    /// Why a long-polling client stopped.
    /// </summary>
    public enum LongPollingStopReason
    {

        /// <summary>
        /// Stopped by the host.
        /// </summary>
        Stopped,

        /// <summary>
        /// The server answered 400: long-polling is not available until it is advertised again.
        /// </summary>
        NotAvailable,

        /// <summary>
        /// The server answered 401: the client is outside the LAN of the server; do not try again with this server.
        /// </summary>
        Unauthorized,

        /// <summary>
        /// The server answered 404 (or another final status): long-polling is not implemented.
        /// </summary>
        NotImplemented,

        /// <summary>
        /// The local endpoint hosts no nodes.
        /// </summary>
        NoNodes,

        /// <summary>
        /// The server implements no common API version.
        /// </summary>
        NoCommonAPIVersion,

        /// <summary>
        /// The client was disposed.
        /// </summary>
        Disposed

    }


    /// <summary>
    /// The client side of long-polling for constrained LAN endpoints (S2 Connect 1.0.0,
    /// "Long-polling for constrained endpoints in the LAN"): keeps a waitForPairing request open
    /// at the server, always lists every hosted node, sends the descriptions when asked, forwards
    /// the prepare/cancel signals as events, starts pairing attempts on request (reporting
    /// NoValidTokenOnPairingClient when the node has no valid token) and follows the status code
    /// policy of the specification (204 poll again, 400 stop until advertised again, 401 stop
    /// for good, 500 and transport failures wait, 503 wait).
    /// </summary>
    public sealed class LongPollingClient : IAsyncDisposable,
                                            IDisposable
    {

        #region Data

        private readonly Lock                                  lockObject   = new ();
        private readonly Dictionary<Node_Id, NodeState>        nodeStates   = [];
        private readonly List<Task>                            pairingTasks = [];
        private readonly PairingClient                         pollingClient;
        private readonly TimeProvider                          timeProvider;
        private readonly ILogger?                              logger;
        private          CancellationTokenSource?              stopSource;
        private          Task?                                 loopTask;
        private          Boolean                               disposed;

        private sealed class NodeState
        {
            public Boolean               SendDescription    { get; set; }
            public WaitForPairingError?  Error              { get; set; }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The pairing client used for the automatic pairing attempts; the polling loop uses a
        /// clone with its own connection, because Hermod serialises the requests of one client
        /// and a hanging waitForPairing request must not delay the pairing interaction.
        /// </summary>
        public PairingClient             Client            { get; }

        /// <summary>
        /// The options of this client.
        /// </summary>
        public LongPollingClientOptions  Options           { get; }

        /// <summary>
        /// Whether the polling loop is running.
        /// </summary>
        public Boolean                   IsRunning
            => loopTask is not null && !loopTask.IsCompleted;

        /// <summary>
        /// Why the client stopped, once it did.
        /// </summary>
        public LongPollingStopReason?    StopReason        { get; private set; }

        /// <summary>
        /// The number of waitForPairing requests sent.
        /// </summary>
        public Int64                     PollCount         { get; private set; }

        /// <summary>
        /// The status code of the last waitForPairing response.
        /// </summary>
        public HTTPStatusCode?           LastStatusCode    { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Raised for every action the server sent.
        /// </summary>
        public event OnLongPollingActionDelegate?                OnActionReceived;

        /// <summary>
        /// Raised when the server asked a node to prepare pairing (e.g. show its pairing code).
        /// </summary>
        public event OnLongPollingPreparePairingDelegate?        OnPreparePairing;

        /// <summary>
        /// Raised when the server cancelled the prepare pairing signal.
        /// </summary>
        public event OnLongPollingCancelPreparePairingDelegate?  OnCancelPreparePairing;

        /// <summary>
        /// Raised when the server asked a node to pair, but the node has no valid pairing token.
        /// </summary>
        public event OnLongPollingMissingTokenDelegate?          OnMissingPairingToken;

        /// <summary>
        /// Raised when an automatic pairing attempt ended.
        /// </summary>
        public event OnLongPollingPairingCompletedDelegate?      OnPairingCompleted;

        /// <summary>
        /// Raised when the client stopped.
        /// </summary>
        public event OnLongPollingStoppedDelegate?               OnStopped;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new long-polling client.
        /// </summary>
        /// <param name="Client">The pairing client of the server to poll (its local endpoint provides the nodes).</param>
        /// <param name="Options">Optional options.</param>
        /// <param name="TimeProvider">An optional time provider (default: the time provider of the pairing client).</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        public LongPollingClient(PairingClient             Client,
                                 LongPollingClientOptions?  Options         = null,
                                 TimeProvider?              TimeProvider    = null,
                                 ILoggerFactory?            LoggerFactory   = null)
        {

            ArgumentNullException.ThrowIfNull(Client);

            this.Client        = Client;
            this.Options       = Options ?? LongPollingClientOptions.Default;
            this.Options.Validate();

            this.pollingClient = Client.Clone();

            this.timeProvider  = TimeProvider ?? Client.TimeProvider;
            this.logger        = LoggerFactory?.CreateLogger<LongPollingClient>();

        }

        #endregion


        #region Start()

        /// <summary>
        /// Start the polling loop in the background.
        /// </summary>
        public void Start()
        {

            ObjectDisposedException.ThrowIf(disposed, this);

            lock (lockObject)
            {

                if (IsRunning)
                    throw new InvalidOperationException("The long-polling client is already running!");

                if (Client.LocalEndpoint.Count == 0)
                    throw new InvalidOperationException("Long-polling cannot start for an endpoint without nodes (S2 Connect 1.0.0, 'Long-polling')!");

                StopReason  = null;
                stopSource  = new CancellationTokenSource();
                loopTask    = Task.Run(() => RunAsync(stopSource.Token));

            }

        }

        #endregion

        #region StopAsync()

        /// <summary>
        /// Stop the polling loop and wait for it and for the running pairing attempts.
        /// </summary>
        public async Task StopAsync()
        {

            Task?                     loop;
            CancellationTokenSource?  source;

            lock (lockObject)
            {
                loop    = loopTask;
                source  = stopSource;
            }

            if (source is not null && !source.IsCancellationRequested)
                await source.CancelAsync().ConfigureAwait(false);

            if (loop is not null)
            {
                try
                {
                    await loop.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                { }
            }

            Task[] pending;

            lock (lockObject)
            {
                pending = [.. pairingTasks];
            }

            try
            {
                await Task.WhenAll(pending).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The results were reported through the events.
            }

        }

        #endregion


        #region (private) RunAsync(CancellationToken)

        private async Task RunAsync(CancellationToken CancellationToken)
        {

            var reason      = LongPollingStopReason.Stopped;
            var statusCode  = (HTTPStatusCode?) null;

            try
            {

                while (!CancellationToken.IsCancellationRequested)
                {

                    var nodes = Client.LocalEndpoint.Nodes;

                    if (nodes.Count == 0)
                    {
                        reason = LongPollingStopReason.NoNodes;
                        break;
                    }

                    var request = BuildRequest(nodes);

                    PollCount++;

                    var result = await pollingClient.WaitForPairingAsync(request, Options.RequestTimeout, CancellationToken).ConfigureAwait(false);

                    LastStatusCode  = result.StatusCode;
                    statusCode      = result.StatusCode;

                    if (result.NoCommonAPIVersion)
                    {
                        reason = LongPollingStopReason.NoCommonAPIVersion;
                        break;
                    }

                    if (result.IsTransportFailure)
                    {
                        logger?.LogWarning("S2 long-polling client: {Description}; waiting {Delay} before the next request.", result.Description, Options.ServerErrorDelay);
                        await Task.Delay(Options.ServerErrorDelay, timeProvider, CancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    var code = result.StatusCode.Code;

                    if (code == 200 && result.Value is not null)
                    {
                        await ProcessActionsAsync(result.Value, nodes, CancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (code == 204)
                        continue;

                    if (code == 400)
                    {
                        reason = LongPollingStopReason.NotAvailable;
                        break;
                    }

                    if (code == 401)
                    {
                        reason = LongPollingStopReason.Unauthorized;
                        break;
                    }

                    if (code == 503)
                    {
                        var delay = result.RetryAfter ?? Options.ServiceUnavailableDelay;
                        logger?.LogInformation("S2 long-polling client: 503, waiting {Delay} before the next request.", delay);
                        await Task.Delay(delay, timeProvider, CancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (code >= 500)
                    {
                        logger?.LogWarning("S2 long-polling client: {Code}, waiting {Delay} before the next request.", code, Options.ServerErrorDelay);
                        await Task.Delay(Options.ServerErrorDelay, timeProvider, CancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (code == 200)
                    {
                        // A 200 whose body could not be parsed: treat like a server error.
                        logger?.LogWarning("S2 long-polling client: unparseable 200 response ({Description}), waiting {Delay}.", result.Description, Options.ServerErrorDelay);
                        await Task.Delay(Options.ServerErrorDelay, timeProvider, CancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    // 404 and every other unexpected status: long-polling is not implemented here.
                    reason = LongPollingStopReason.NotImplemented;
                    break;

                }

            }
            catch (OperationCanceledException)
            {
                reason = LongPollingStopReason.Stopped;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "S2 long-polling client: the polling loop failed.");
                reason = LongPollingStopReason.Stopped;
            }

            if (disposed)
                reason = LongPollingStopReason.Disposed;

            StopReason = reason;

            // The status code is reported only when it caused the stop.
            if (reason is LongPollingStopReason.Stopped or LongPollingStopReason.NoNodes or LongPollingStopReason.Disposed)
                statusCode = null;

            logger?.LogInformation("S2 long-polling client: stopped ({Reason}).", reason);

            await OnStopped.InvokeAllAsync(handler => handler(timeProvider.GetUtcNow(), this, reason, statusCode), logger).ConfigureAwait(false);

        }

        #endregion

        #region (private) BuildRequest(Nodes)

        private WaitForPairingRequest BuildRequest(IReadOnlyList<HostedNode> Nodes)
        {

            var items = new List<WaitForPairingRequestItem>();

            lock (lockObject)
            {

                foreach (var node in Nodes)
                {

                    if (!nodeStates.TryGetValue(node.Id, out var state))
                    {
                        state = new NodeState();
                        nodeStates.Add(node.Id, state);
                    }

                    items.Add(new WaitForPairingRequestItem(
                                  node.Id,
                                  state.SendDescription ? node.Description                 : null,
                                  state.SendDescription ? Client.LocalEndpoint.Description : null,
                                  state.Error
                              ));

                    // The descriptions and the error are sent once.
                    state.SendDescription  = false;
                    state.Error            = null;

                }

            }

            return new WaitForPairingRequest(items);

        }

        #endregion

        #region (private) ProcessActionsAsync(Response, Nodes, CancellationToken)

        private async Task ProcessActionsAsync(WaitForPairingResponse     Response,
                                               IReadOnlyList<HostedNode>  Nodes,
                                               CancellationToken          CancellationToken)
        {

            var now = timeProvider.GetUtcNow();

            foreach (var item in Response.Items)
            {

                var node = Nodes.FirstOrDefault(node => node.Id == item.ClientNodeId);

                if (node is null)
                {
                    logger?.LogWarning("S2 long-polling client: the server sent an action for the unknown node {NodeId}.", item.ClientNodeId);
                    continue;
                }

                await OnActionReceived.InvokeAllAsync(handler => handler(now, this, node, item.Action), logger).ConfigureAwait(false);

                if (item.Action == WaitForPairingAction.SendNodeDescription)
                {
                    lock (lockObject)
                    {
                        nodeStates[node.Id].SendDescription = true;
                    }
                }

                else if (item.Action == WaitForPairingAction.PreparePairing)
                    await OnPreparePairing.InvokeAllAsync(handler => handler(now, this, node), logger).ConfigureAwait(false);

                else if (item.Action == WaitForPairingAction.CancelPreparePairing)
                    await OnCancelPreparePairing.InvokeAllAsync(handler => handler(now, this, node), logger).ConfigureAwait(false);

                else if (item.Action == WaitForPairingAction.RequestPairing)
                    await StartPairingAsync(node, CancellationToken).ConfigureAwait(false);

                else
                    logger?.LogWarning("S2 long-polling client: unknown action '{Action}' for node {NodeId}.", item.Action, node.Id);

            }

        }

        #endregion

        #region (private) StartPairingAsync(Node, CancellationToken)

        private async Task StartPairingAsync(HostedNode         Node,
                                             CancellationToken  CancellationToken)
        {

            if (!Node.TryGetPairingToken(out var token))
            {

                // "the client must perform a new request with an errorMessage containing the value NoValidTokenOnPairingClient"
                lock (lockObject)
                {
                    nodeStates[Node.Id].Error = WaitForPairingError.NoValidTokenOnPairingClient;
                }

                logger?.LogInformation("S2 long-polling client: the server asked node {NodeId} to pair, but it has no valid pairing token.", Node.Id);

                await OnMissingPairingToken.InvokeAllAsync(handler => handler(timeProvider.GetUtcNow(), this, Node), logger).ConfigureAwait(false);

                return;

            }

            if (!Options.AutoPair)
                return;

            var pairingTask = Task.Run(async () => {

                var result = await Client.PairAsync(Node,
                                                    token,
                                                    Options.PairingTarget,
                                                    Options.ForcePairing,
                                                    CancellationToken).ConfigureAwait(false);

                await OnPairingCompleted.InvokeAllAsync(handler => handler(timeProvider.GetUtcNow(), this, Node, result), logger).ConfigureAwait(false);

            }, CancellationToken);

            lock (lockObject)
            {
                pairingTasks.RemoveAll(task => task.IsCompleted);
                pairingTasks.Add(pairingTask);
            }

        }

        #endregion


        #region DisposeAsync() / Dispose()

        /// <summary>
        /// Stop the polling loop and release the resources of this client.
        /// </summary>
        public async ValueTask DisposeAsync()
        {

            if (disposed)
                return;

            disposed = true;

            await StopAsync().ConfigureAwait(false);

            stopSource?.Dispose();

            await pollingClient.DisposeAsync().ConfigureAwait(false);

        }

        /// <summary>
        /// Stop the polling loop and release the resources of this client.
        /// </summary>
        public void Dispose()
        {

            if (disposed)
                return;

            disposed = true;

            try
            {
                stopSource?.Cancel();
            }
            catch (ObjectDisposedException)
            { }

            stopSource?.Dispose();
            pollingClient.Dispose();

        }

        #endregion

    }

}

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

using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// A client node known to the long-polling server: a node hosted by a constrained LAN
    /// endpoint that announced itself via waitForPairing (S2 Connect 1.0.0, "Long-polling for
    /// constrained endpoints in the LAN").
    /// </summary>
    public sealed class LongPollingClientNode
    {

        #region Data

        internal Queue<WaitForPairingAction>  Actions                 { get; } = [];
        internal Int32                        ActiveRequests;
        internal Boolean                      DescriptionRequested;

        #endregion

        #region Properties

        /// <summary>
        /// The identification of the client node.
        /// </summary>
        public Node_Id               Id                     { get; }

        /// <summary>
        /// The description of the client node, once sent (action "sendNodeDescription").
        /// </summary>
        public NodeDescription?      Description            { get; internal set; }

        /// <summary>
        /// The description of the client endpoint, once sent.
        /// </summary>
        public EndpointDescription?  EndpointDescription    { get; internal set; }

        /// <summary>
        /// When the node polled for the first time.
        /// </summary>
        public DateTimeOffset        FirstSeen              { get; }

        /// <summary>
        /// When the node polled for the last time.
        /// </summary>
        public DateTimeOffset        LastSeen               { get; internal set; }

        /// <summary>
        /// The last error the client reported for this node, e.g. that it has no valid pairing token.
        /// </summary>
        public WaitForPairingError?  LastError              { get; internal set; }

        /// <summary>
        /// When the last error was reported.
        /// </summary>
        public DateTimeOffset?       LastErrorAt            { get; internal set; }

        /// <summary>
        /// Whether a waitForPairing request listing this node is currently hanging.
        /// </summary>
        public Boolean               IsPolling
            => Volatile.Read(ref ActiveRequests) > 0;

        /// <summary>
        /// The actions queued for this node.
        /// </summary>
        public IReadOnlyList<WaitForPairingAction>  PendingActions
        {
            get
            {
                lock (Actions)
                {
                    return [.. Actions];
                }
            }
        }

        #endregion

        #region Constructor(s)

        internal LongPollingClientNode(Node_Id         Id,
                                       DateTimeOffset  Now)
        {
            this.Id         = Id;
            this.FirstSeen  = Now;
            this.LastSeen   = Now;
        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Id}{(Description is not null ? $" ({Description.Role}, {Description.Brand} {Description.ModelName})" : "")}{(IsPolling ? ", polling" : "")}";

        #endregion

    }


    /// <summary>
    /// The status of a waitForPairing request.
    /// </summary>
    public enum LongPollingStatus
    {

        /// <summary>
        /// The server has actions for the client (HTTP 200).
        /// </summary>
        Actions,

        /// <summary>
        /// No action within the response time, the client should poll again (HTTP 204).
        /// </summary>
        NoAction,

        /// <summary>
        /// Long-polling is temporarily not available, e.g. during shutdown or when too many
        /// requests are hanging (HTTP 503).
        /// </summary>
        Unavailable

    }


    /// <summary>
    /// The result of a waitForPairing request.
    /// </summary>
    /// <param name="Status">The status.</param>
    /// <param name="Response">The actions for the client when the status is <see cref="LongPollingStatus.Actions"/>.</param>
    public sealed record LongPollingResult(LongPollingStatus       Status,
                                           WaitForPairingResponse?  Response);


    /// <summary>
    /// The server side of long-polling (S2 Connect 1.0.0, "Long-polling for constrained
    /// endpoints in the LAN"): keeps waitForPairing requests open for at most 25 seconds,
    /// remembers the client nodes that are available for pairing and delivers the actions the
    /// host wants them to execute (sendNodeDescription, preparePairing, cancelPreparePairing,
    /// requestPairing), at most one per client node and response.
    /// </summary>
    public sealed class LongPollingServer : IDisposable
    {

        #region Data

        private readonly Lock                                        lockObject  = new ();
        private readonly Dictionary<Node_Id, LongPollingClientNode>  clientNodes = [];
        private readonly List<Waiter>                                waiters     = [];
        private readonly CancellationTokenSource                     shutdown    = new ();
        private readonly TimeProvider                                timeProvider;
        private readonly ILogger?                                    logger;
        private          Int32                                       hangingRequests;
        private          Boolean                                     disposed;

        private const    Int32                                       MaxQueuedActionsPerNode = 8;

        private sealed class Waiter(IReadOnlyList<Node_Id> NodeIds)
        {
            public IReadOnlyList<Node_Id>         NodeIds    { get; } = NodeIds;
            public TaskCompletionSource<Boolean>  Signal     { get; } = new (TaskCreationOptions.RunContinuationsAsynchronously);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The maximum time a waitForPairing request is kept open.
        /// </summary>
        public TimeSpan  ResponseTimeout                { get; }

        /// <summary>
        /// The maximal number of concurrently hanging requests.
        /// </summary>
        public Int32     MaxHangingRequests             { get; }

        /// <summary>
        /// Whether a client node without descriptions is asked for them automatically.
        /// </summary>
        public Boolean   AutoRequestNodeDescriptions    { get; }

        /// <summary>
        /// The number of currently hanging requests.
        /// </summary>
        public Int32     HangingRequests
            => Volatile.Read(ref hangingRequests);

        /// <summary>
        /// Whether new requests are accepted (false after <see cref="Shutdown"/>).
        /// </summary>
        public Boolean   IsAcceptingRequests            { get; private set; } = true;

        /// <summary>
        /// A snapshot of the known client nodes.
        /// </summary>
        public IReadOnlyList<LongPollingClientNode>  ClientNodes
        {
            get
            {
                lock (lockObject)
                {
                    return [.. clientNodes.Values];
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised whenever a client node polls (IsNew for the first time).
        /// </summary>
        public event OnLongPollingClientNodeSeenDelegate?          OnClientNodeSeen;

        /// <summary>
        /// Raised when a client node sent its node and endpoint descriptions.
        /// </summary>
        public event OnLongPollingClientNodeDescriptionDelegate?   OnClientNodeDescriptionReceived;

        /// <summary>
        /// Raised when a client node reported an error, e.g. NoValidTokenOnPairingClient.
        /// </summary>
        public event OnLongPollingClientNodeErrorDelegate?         OnClientNodeError;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new long-polling server.
        /// </summary>
        /// <param name="TimeProvider">An optional time provider (default: the system clock).</param>
        /// <param name="ResponseTimeout">The maximum time a request is kept open (default: 25 seconds).</param>
        /// <param name="MaxHangingRequests">The maximal number of concurrently hanging requests (default: 64).</param>
        /// <param name="AutoRequestNodeDescriptions">Whether client nodes without descriptions are asked for them automatically (default: true).</param>
        /// <param name="Logger">An optional logger.</param>
        public LongPollingServer(TimeProvider?  TimeProvider                  = null,
                                 TimeSpan?      ResponseTimeout               = null,
                                 Int32          MaxHangingRequests            = 64,
                                 Boolean        AutoRequestNodeDescriptions   = true,
                                 ILogger?       Logger                        = null)
        {

            this.timeProvider                 = TimeProvider    ?? System.TimeProvider.System;
            this.ResponseTimeout              = ResponseTimeout ?? S2ConnectDefaults.LongPollingServerTimeout;
            this.MaxHangingRequests           = MaxHangingRequests;
            this.AutoRequestNodeDescriptions  = AutoRequestNodeDescriptions;
            this.logger                       = Logger;

            if (this.ResponseTimeout <= TimeSpan.Zero || this.ResponseTimeout > S2ConnectDefaults.LongPollingServerTimeout)
                throw new ArgumentOutOfRangeException(nameof(ResponseTimeout), $"The response timeout must be positive and at most {S2ConnectDefaults.LongPollingServerTimeout.TotalSeconds} seconds!");

            ArgumentOutOfRangeException.ThrowIfLessThan(MaxHangingRequests, 1);

        }

        #endregion


        #region WaitAsync(Request, CancellationToken = default)

        /// <summary>
        /// Process a waitForPairing request: register the listed client nodes, deliver queued
        /// actions immediately or keep the request open until an action arrives or the
        /// response timeout elapses.
        /// </summary>
        /// <param name="Request">The waitForPairing request.</param>
        /// <param name="CancellationToken">A token to cancel the request.</param>
        public async Task<LongPollingResult> WaitAsync(WaitForPairingRequest  Request,
                                                       CancellationToken      CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Request);

            if (disposed || !IsAcceptingRequests)
                return new LongPollingResult(LongPollingStatus.Unavailable, null);

            if (Interlocked.Increment(ref hangingRequests) > MaxHangingRequests)
            {
                Interlocked.Decrement(ref hangingRequests);
                logger?.LogWarning("S2 long-polling server: too many hanging requests ({Count}), answering 503.", MaxHangingRequests);
                return new LongPollingResult(LongPollingStatus.Unavailable, null);
            }

            var now       = timeProvider.GetUtcNow();
            var deadline  = now + ResponseTimeout;
            var nodeIds   = Request.Items.Select(item => item.ClientNodeId).Distinct().ToList();

            var seen      = new List<(LongPollingClientNode Node, Boolean IsNew)>();
            var described = new List<LongPollingClientNode>();
            var errors    = new List<(LongPollingClientNode Node, WaitForPairingError Error)>();

            try
            {

                #region Register the client nodes

                lock (lockObject)
                {

                    foreach (var item in Request.Items)
                    {

                        var isNew = false;

                        if (!clientNodes.TryGetValue(item.ClientNodeId, out var clientNode))
                        {
                            clientNode = new LongPollingClientNode(item.ClientNodeId, now);
                            clientNodes.Add(item.ClientNodeId, clientNode);
                            isNew = true;
                        }

                        clientNode.LastSeen = now;

                        if (item.ClientNodeDescription is not null)
                        {
                            clientNode.Description           = item.ClientNodeDescription;
                            clientNode.EndpointDescription   = item.ClientEndpointDescription;
                            clientNode.DescriptionRequested  = false;
                            described.Add(clientNode);
                        }

                        if (item.ErrorMessage.HasValue)
                        {
                            clientNode.LastError    = item.ErrorMessage.Value;
                            clientNode.LastErrorAt  = now;
                            errors.Add((clientNode, item.ErrorMessage.Value));
                        }

                        if (AutoRequestNodeDescriptions &&
                            clientNode.Description is null &&
                            !clientNode.DescriptionRequested)
                        {
                            lock (clientNode.Actions)
                            {
                                if (!clientNode.Actions.Contains(WaitForPairingAction.SendNodeDescription))
                                    clientNode.Actions.Enqueue(WaitForPairingAction.SendNodeDescription);
                            }
                            clientNode.DescriptionRequested = true;
                        }

                        seen.Add((clientNode, isNew));

                    }

                    foreach (var (node, _) in seen.DistinctBy(entry => entry.Node.Id))
                        Interlocked.Increment(ref node.ActiveRequests);

                }

                #endregion

                #region Raise the events

                foreach (var (node, isNew) in seen.DistinctBy(entry => entry.Node.Id))
                    await OnClientNodeSeen.InvokeAllAsync(handler => handler(now, this, node, isNew), logger).ConfigureAwait(false);

                foreach (var node in described)
                    await OnClientNodeDescriptionReceived.InvokeAllAsync(handler => handler(now, this, node), logger).ConfigureAwait(false);

                foreach (var (node, error) in errors)
                    await OnClientNodeError.InvokeAllAsync(handler => handler(now, this, node, error), logger).ConfigureAwait(false);

                #endregion

                #region Deliver actions or wait for them

                while (true)
                {

                    // Register the waiter before looking for actions, so that an action queued
                    // in between is never missed.
                    var waiter = new Waiter(nodeIds);

                    lock (lockObject)
                    {
                        waiters.Add(waiter);
                    }

                    try
                    {

                        var items = CollectActions(nodeIds);

                        if (items.Count > 0)
                            return new LongPollingResult(LongPollingStatus.Actions, new WaitForPairingResponse(items));

                        var remaining = deadline - timeProvider.GetUtcNow();

                        if (remaining <= TimeSpan.Zero)
                            return new LongPollingResult(LongPollingStatus.NoAction, null);

                        using var linked = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, shutdown.Token);

                        var delay     = Task.Delay(remaining, timeProvider, linked.Token);
                        var finished  = await Task.WhenAny(waiter.Signal.Task, delay).ConfigureAwait(false);

                        if (finished == delay)
                        {

                            if (shutdown.IsCancellationRequested)
                                return new LongPollingResult(LongPollingStatus.Unavailable, null);

                            CancellationToken.ThrowIfCancellationRequested();

                            return new LongPollingResult(LongPollingStatus.NoAction, null);

                        }

                        // Stop the delay timer eagerly...
                        await linked.CancelAsync().ConfigureAwait(false);

                    }
                    finally
                    {
                        lock (lockObject)
                        {
                            waiters.Remove(waiter);
                        }
                    }

                }

                #endregion

            }
            finally
            {

                Interlocked.Decrement(ref hangingRequests);

                lock (lockObject)
                {
                    foreach (var (node, _) in seen.DistinctBy(entry => entry.Node.Id))
                        Interlocked.Decrement(ref node.ActiveRequests);
                }

            }

        }

        private List<WaitForPairingResponseItem> CollectActions(IReadOnlyList<Node_Id> NodeIds)
        {

            var items = new List<WaitForPairingResponseItem>();

            lock (lockObject)
            {

                foreach (var nodeId in NodeIds)
                {

                    if (!clientNodes.TryGetValue(nodeId, out var clientNode))
                        continue;

                    lock (clientNode.Actions)
                    {
                        if (clientNode.Actions.TryDequeue(out var action))
                            items.Add(new WaitForPairingResponseItem(nodeId, action));
                    }

                }

            }

            return items;

        }

        #endregion

        #region SendAction(ClientNodeId, Action)

        /// <summary>
        /// Queue an action for the given client node and wake the request listing it
        /// (S2 Connect 1.0.0: "The server may only provide at most one item for each clientNodeId").
        /// An action equal to the last queued one is not queued twice; when more than eight
        /// actions are pending the oldest one is dropped.
        /// </summary>
        /// <param name="ClientNodeId">The identification of the client node.</param>
        /// <param name="Action">The action.</param>
        /// <returns>False when the client node never polled this server.</returns>
        public Boolean SendAction(Node_Id               ClientNodeId,
                                  WaitForPairingAction  Action)
        {

            List<Waiter> toSignal;

            lock (lockObject)
            {

                if (!clientNodes.TryGetValue(ClientNodeId, out var clientNode))
                    return false;

                lock (clientNode.Actions)
                {

                    if (clientNode.Actions.Count >= MaxQueuedActionsPerNode)
                        clientNode.Actions.Dequeue();

                    if (clientNode.Actions.Count == 0 || !clientNode.Actions.Last().Equals(Action))
                        clientNode.Actions.Enqueue(Action);

                }

                toSignal = [.. waiters.Where(waiter => waiter.NodeIds.Contains(ClientNodeId))];

            }

            foreach (var waiter in toSignal)
                waiter.Signal.TrySetResult(true);

            return true;

        }

        #endregion

        #region TryGetClientNode(ClientNodeId, out ClientNode)

        /// <summary>
        /// Try to get the known client node with the given identification.
        /// </summary>
        /// <param name="ClientNodeId">The identification of the client node.</param>
        /// <param name="ClientNode">The client node.</param>
        public Boolean TryGetClientNode(Node_Id                                         ClientNodeId,
                                        [NotNullWhen(true)] out LongPollingClientNode?  ClientNode)
        {
            lock (lockObject)
            {
                return clientNodes.TryGetValue(ClientNodeId, out ClientNode);
            }
        }

        #endregion

        #region RemoveClientNode(ClientNodeId)

        /// <summary>
        /// Forget the given client node and its queued actions.
        /// </summary>
        /// <param name="ClientNodeId">The identification of the client node.</param>
        public Boolean RemoveClientNode(Node_Id ClientNodeId)
        {
            lock (lockObject)
            {
                return clientNodes.Remove(ClientNodeId);
            }
        }

        #endregion

        #region Purge(MaxAge)

        /// <summary>
        /// Forget every client node that is not polling and has not been seen for the given time.
        /// </summary>
        /// <param name="MaxAge">The maximal time since the last poll.</param>
        /// <returns>The number of removed client nodes.</returns>
        public Int32 Purge(TimeSpan MaxAge)
        {

            var threshold = timeProvider.GetUtcNow() - MaxAge;

            lock (lockObject)
            {

                var stale = clientNodes.Values.
                                Where(node => !node.IsPolling && node.LastSeen < threshold).
                                Select(node => node.Id).
                                ToList();

                foreach (var nodeId in stale)
                    clientNodes.Remove(nodeId);

                return stale.Count;

            }

        }

        #endregion

        #region Shutdown()

        /// <summary>
        /// Stop accepting requests and release every hanging request with status
        /// <see cref="LongPollingStatus.Unavailable"/>.
        /// </summary>
        public void Shutdown()
        {

            IsAcceptingRequests = false;

            if (!shutdown.IsCancellationRequested)
                shutdown.Cancel();

        }

        #endregion

        #region Dispose()

        /// <summary>
        /// Shut down and release the resources of this server.
        /// </summary>
        public void Dispose()
        {

            if (disposed)
                return;

            disposed = true;

            Shutdown();
            shutdown.Dispose();

        }

        #endregion

    }

}

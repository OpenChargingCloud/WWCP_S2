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

using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// Why a reconnecting session client stopped.
    /// </summary>
    public enum ReconnectStopReason
    {

        /// <summary>
        /// Stopped by the host.
        /// </summary>
        Stopped,

        /// <summary>
        /// The server answered NoLongerPaired: the nodes are unpaired; inform the end user.
        /// </summary>
        NoLongerPaired,

        /// <summary>
        /// No candidate access token was accepted; inform the end user.
        /// </summary>
        Unauthorized,

        /// <summary>
        /// The nodes are not paired locally.
        /// </summary>
        NotPaired,

        /// <summary>
        /// The client was disposed.
        /// </summary>
        Disposed

    }


    /// <summary>
    /// Keeps the S2 session of a communication client alive (S2 Connect 1.0.0, "Reconnection
    /// strategy"): every (re)connection runs through session initiation, a lost session is
    /// re-established with the exponential back-off of <see cref="ReconnectStrategy"/>, a
    /// SessionRequest RECONNECT closes the session and reconnects immediately, a SessionRequest
    /// TERMINATE closes it and reconnects with back-off, and NoLongerPaired stops the client.
    /// </summary>
    public sealed class ReconnectingSessionClient : IAsyncDisposable,
                                                    IDisposable
    {

        #region Data

        private readonly Lock                                       lockObject = new ();
        private readonly TimeProvider                               timeProvider;
        private readonly ILogger?                                   logger;
        private readonly Func<S2SessionOptions, S2SessionOptions>?  configureSession;
        private          CancellationTokenSource?                   stopSource;
        private          Task?                                      loopTask;
        private          S2ConnectSession?                          currentSession;
        private volatile Boolean                                    disposed;

        #endregion

        #region Properties

        /// <summary>
        /// The session initiation client.
        /// </summary>
        public SessionInitiationClient          Client             { get; }

        /// <summary>
        /// The local node (the communication client).
        /// </summary>
        public HostedNode                       LocalNode          { get; }

        /// <summary>
        /// The identification of the node at the communication server.
        /// </summary>
        public Node_Id                          ServerNodeId       { get; }

        /// <summary>
        /// The reconnection strategy.
        /// </summary>
        public ReconnectStrategy                Strategy           { get; }

        /// <summary>
        /// Whether the client is running.
        /// </summary>
        public Boolean                          IsRunning
            => loopTask is not null && !loopTask.IsCompleted;

        /// <summary>
        /// Why the client stopped, once it did.
        /// </summary>
        public ReconnectStopReason?             StopReason         { get; private set; }

        /// <summary>
        /// The current session, while one is open.
        /// </summary>
        public S2ConnectSession?                CurrentSession
        {
            get
            {
                lock (lockObject)
                {
                    return currentSession;
                }
            }
        }

        /// <summary>
        /// The number of sessions opened so far.
        /// </summary>
        public Int32                            SessionCount       { get; private set; }

        /// <summary>
        /// The result of the last session initiation.
        /// </summary>
        public SessionInitiationClientResult?   LastResult         { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Raised when a session was opened.
        /// </summary>
        public event OnReconnectingSessionStartedDelegate?  OnSessionStarted;

        /// <summary>
        /// Raised when a session ended.
        /// </summary>
        public event OnReconnectingSessionEndedDelegate?    OnSessionEnded;

        /// <summary>
        /// Raised when a connection attempt failed and the next one is scheduled.
        /// </summary>
        public event OnReconnectAttemptFailedDelegate?      OnAttemptFailed;

        /// <summary>
        /// Raised when the client stopped.
        /// </summary>
        public event OnReconnectingClientStoppedDelegate?   OnStopped;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new reconnecting session client.
        /// </summary>
        /// <param name="Client">The session initiation client.</param>
        /// <param name="LocalNode">The local node (the communication client).</param>
        /// <param name="ServerNodeId">The identification of the node at the communication server.</param>
        /// <param name="Strategy">An optional reconnection strategy (default: the strategy of the specification).</param>
        /// <param name="ConfigureSession">An optional customisation of the session options.</param>
        /// <param name="TimeProvider">An optional time provider (default: the time provider of the client).</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        public ReconnectingSessionClient(SessionInitiationClient                    Client,
                                         HostedNode                                 LocalNode,
                                         Node_Id                                    ServerNodeId,
                                         ReconnectStrategy?                         Strategy           = null,
                                         Func<S2SessionOptions, S2SessionOptions>?  ConfigureSession   = null,
                                         TimeProvider?                              TimeProvider       = null,
                                         ILoggerFactory?                            LoggerFactory      = null)
        {

            ArgumentNullException.ThrowIfNull(Client);
            ArgumentNullException.ThrowIfNull(LocalNode);

            this.Client            = Client;
            this.LocalNode         = LocalNode;
            this.ServerNodeId      = ServerNodeId;
            this.Strategy          = Strategy ?? new ReconnectStrategy();
            this.configureSession  = ConfigureSession;
            this.timeProvider      = TimeProvider ?? Client.TimeProvider;
            this.logger            = LoggerFactory?.CreateLogger<ReconnectingSessionClient>();

        }

        #endregion


        #region Start()

        /// <summary>
        /// Start connecting in the background.
        /// </summary>
        public void Start()
        {

            ObjectDisposedException.ThrowIf(disposed, this);

            lock (lockObject)
            {

                if (IsRunning)
                    throw new InvalidOperationException("The reconnecting session client is already running!");

                StopReason  = null;
                stopSource  = new CancellationTokenSource();
                loopTask    = Task.Run(() => RunAsync(stopSource.Token));

            }

        }

        #endregion

        #region StopAsync()

        /// <summary>
        /// Stop reconnecting, close the current session and wait for the loop.
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

        }

        #endregion


        #region (private) RunAsync(CancellationToken)

        private async Task RunAsync(CancellationToken CancellationToken)
        {

            var reason = ReconnectStopReason.Stopped;

            try
            {

                while (!CancellationToken.IsCancellationRequested)
                {

                    var connect = await Client.ConnectAsync(LocalNode, ServerNodeId, configureSession, CancellationToken).ConfigureAwait(false);

                    LastResult = connect.Initiation;

                    if (connect.Session is null)
                    {

                        switch (connect.Initiation.Outcome)
                        {

                            case SessionInitiationOutcome.NoLongerPaired:
                                reason = ReconnectStopReason.NoLongerPaired;
                                break;

                            case SessionInitiationOutcome.Unauthorized:
                                reason = ReconnectStopReason.Unauthorized;
                                break;

                            case SessionInitiationOutcome.InvalidConfiguration:
                                reason = ReconnectStopReason.NotPaired;
                                break;

                            case SessionInitiationOutcome.Cancelled:
                                reason = ReconnectStopReason.Stopped;
                                break;

                        }

                        if (reason != ReconnectStopReason.Stopped || connect.Initiation.Outcome == SessionInitiationOutcome.Cancelled)
                            break;

                        var attempt  = Strategy.Attempt;
                        var delay    = Strategy.NextDelay();

                        logger?.LogInformation("S2 reconnecting client: attempt {Attempt} failed ({Result}); next attempt in {Delay}.", attempt, connect.Initiation, delay);

                        await OnAttemptFailed.InvokeAllAsync(handler => handler(timeProvider.GetUtcNow(), this, connect.Initiation, attempt, delay), logger).ConfigureAwait(false);

                        await Task.Delay(delay, timeProvider, CancellationToken).ConfigureAwait(false);

                        continue;

                    }

                    #region A session is open

                    var session = connect.Session;

                    Strategy.Reset();

                    lock (lockObject)
                    {
                        currentSession = session;
                        SessionCount++;
                    }

                    // Observe the session before anybody else learns about it, so that no close
                    // or session request is missed.
                    var closed              = new TaskCompletionSource<S2CloseReason>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var reconnectRequested  = false;

                    session.Session.OnClosed         += (timestamp, s2Session, closeReason) => {
                                                            closed.TrySetResult(closeReason);
                                                            return Task.CompletedTask;
                                                        };

                    session.Session.OnSessionRequest += async (timestamp, s2Session, request) => {

                                                            if (request.Request == SessionRequestType.Reconnect)
                                                            {
                                                                reconnectRequested = true;
                                                                await s2Session.CloseAsync(new S2CloseReason("SessionRequest RECONNECT received", true)).ConfigureAwait(false);
                                                            }

                                                            else if (request.Request == SessionRequestType.Terminate)
                                                                await s2Session.CloseAsync(new S2CloseReason("SessionRequest TERMINATE received", true)).ConfigureAwait(false);

                                                        };

                    if (!session.Session.IsConnected)
                        closed.TrySetResult(new S2CloseReason("the session closed before it could be observed", false));

                    await OnSessionStarted.InvokeAllAsync(handler => handler(timeProvider.GetUtcNow(), this, session), logger).ConfigureAwait(false);

                    S2CloseReason closeReason;

                    try
                    {
                        closeReason = await closed.Task.WaitAsync(CancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        closeReason = new S2CloseReason("the reconnecting client was stopped", true);
                    }

                    lock (lockObject)
                    {
                        currentSession = null;
                    }

                    await OnSessionEnded.InvokeAllAsync(handler => handler(timeProvider.GetUtcNow(), this, session, closeReason), logger).ConfigureAwait(false);

                    await session.DisposeAsync().ConfigureAwait(false);

                    if (CancellationToken.IsCancellationRequested)
                        break;

                    if (reconnectRequested)
                    {
                        logger?.LogInformation("S2 reconnecting client: reconnecting immediately as requested by the server.");
                        continue;
                    }

                    var reconnectDelay = Strategy.NextDelay();

                    logger?.LogInformation("S2 reconnecting client: the session ended ({Reason}); reconnecting in {Delay}.", closeReason.Description, reconnectDelay);

                    await Task.Delay(reconnectDelay, timeProvider, CancellationToken).ConfigureAwait(false);

                    #endregion

                }

            }
            catch (OperationCanceledException)
            {
                reason = ReconnectStopReason.Stopped;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "S2 reconnecting client: the loop failed.");
                reason = ReconnectStopReason.Stopped;
            }

            S2ConnectSession? leftover;

            lock (lockObject)
            {
                leftover        = currentSession;
                currentSession  = null;
            }

            if (leftover is not null)
                await leftover.DisposeAsync().ConfigureAwait(false);

            if (disposed)
                reason = ReconnectStopReason.Disposed;

            StopReason = reason;

            logger?.LogInformation("S2 reconnecting client: stopped ({Reason}).", reason);

            await OnStopped.InvokeAllAsync(handler => handler(timeProvider.GetUtcNow(), this, reason), logger).ConfigureAwait(false);

        }

        #endregion


        #region DisposeAsync() / Dispose()

        /// <summary>
        /// Stop reconnecting and release the resources of this client.
        /// </summary>
        public async ValueTask DisposeAsync()
        {

            if (disposed)
                return;

            disposed = true;

            await StopAsync().ConfigureAwait(false);

            stopSource?.Dispose();

        }

        /// <summary>
        /// Stop reconnecting and release the resources of this client.
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

        }

        #endregion

    }

}

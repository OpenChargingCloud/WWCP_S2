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

using System.Threading.Channels;

using Microsoft.Extensions.Logging;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.S2.Session
{

    /// <summary>
    /// A stateful S2 session between a CEM and an RM on top of an <see cref="IS2Medium"/>
    /// (PLAN.md §3.1–3.4): parses every received message, answers it with exactly one
    /// ReceptionStatus, enforces the message rules of the current state, tracks the negotiated
    /// version, the active control type, revokable objects and instructions, and dispatches
    /// messages to registered handlers strictly in wire order on a single consumer task, so
    /// that the medium's read loop never blocks on application code.
    /// </summary>
    public sealed class S2Session : IAsyncDisposable
    {

        #region Data

        private readonly Channel<IS2Message>                                    inbound;
        private readonly ReceptionStatusAwaiter                                 awaiter                = new ();
        private readonly Dictionary<Type, List<Func<S2Session, IS2Message, CancellationToken, Task<ReceptionStatusValue?>>>>  handlers = [];
        private readonly Dictionary<ControlType, IS2ControlTypeHandler>         controlTypeHandlers    = [];
        private readonly CancellationTokenSource                                cancellation           = new ();
        private readonly Lock                                                   stateLock              = new ();
        private readonly ILogger?                                               logger;
        private          Task?                                                  consumerTask;
        private          IS2ControlTypeHandler?                                 activeHandler;
        private          IDisposable?                                           activeHandlerRegistration;
        private          Int32                                                  closed;

        #endregion

        #region Properties

        /// <summary>
        /// The unique identification of this session.
        /// </summary>
        public Guid                  Id                    { get; } = Guid.CreateVersion7();

        /// <summary>
        /// The medium this session runs on.
        /// </summary>
        public IS2Medium             Medium                { get; }

        /// <summary>
        /// The session options.
        /// </summary>
        public S2SessionOptions      Options               { get; }

        /// <summary>
        /// The role of the local node.
        /// </summary>
        public EnergyManagementRole  Role
            => Options.Role;

        /// <summary>
        /// The role of the peer.
        /// </summary>
        public EnergyManagementRole  PeerRole
            => Options.Role == EnergyManagementRole.CEM
                   ? EnergyManagementRole.RM
                   : EnergyManagementRole.CEM;

        /// <summary>
        /// The session mode.
        /// </summary>
        public S2SessionMode         Mode
            => Options.Mode;

        /// <summary>
        /// The current session state.
        /// </summary>
        public S2SessionState        State                 { get; private set; } = S2SessionState.Created;

        /// <summary>
        /// The active control type when the state is ControlTypeActivated.
        /// </summary>
        public ControlType?          ActiveControlType     { get; private set; }

        /// <summary>
        /// The S2 JSON version negotiated for this session (by S2 Connect or by the plain-mode handshake).
        /// </summary>
        public String?               NegotiatedVersion     { get; private set; }

        /// <summary>
        /// The registry of revokable objects, instructions and declared identifications.
        /// </summary>
        public S2ObjectRegistry      Registry              { get; } = new ();

        /// <summary>
        /// The time provider used for every deadline of this session.
        /// </summary>
        public TimeProvider          TimeProvider          { get; }

        /// <summary>
        /// The number of own messages waiting for their ReceptionStatus.
        /// </summary>
        public Int32                 PendingReceptions
            => awaiter.PendingCount;

        /// <summary>
        /// Whether the session is connected (started and not yet closed).
        /// </summary>
        public Boolean               IsConnected
            => State is S2SessionState.AwaitingHandshake or S2SessionState.WebSocketConnected or S2SessionState.ControlTypeActivated;

        #endregion

        #region Events

        /// <summary>
        /// Raised for every received message after it was processed (logging only).
        /// </summary>
        public event OnS2MessageReceivedDelegate?           OnMessageReceived;

        /// <summary>
        /// Raised for every sent message (logging only).
        /// </summary>
        public event OnS2MessageSentDelegate?               OnMessageSent;

        /// <summary>
        /// Raised when the session state or the active control type changed.
        /// </summary>
        public event OnS2SessionStateChangedDelegate?       OnStateChanged;

        /// <summary>
        /// Raised when a ReceptionStatus arrived that no sent message awaits.
        /// </summary>
        public event OnS2UnmatchedReceptionStatusDelegate?  OnUnmatchedReceptionStatus;

        /// <summary>
        /// Raised when the peer sent a SessionRequest; the session closes the medium afterwards.
        /// </summary>
        public event OnS2SessionRequestDelegate?            OnSessionRequest;

        /// <summary>
        /// Raised once when the session was closed.
        /// </summary>
        public event OnS2SessionClosedDelegate?             OnClosed;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new S2 session on the given medium. Call <see cref="StartAsync"/> to begin.
        /// </summary>
        /// <param name="Medium">A connected medium.</param>
        /// <param name="Options">The session options.</param>
        /// <param name="TimeProvider">An optional time provider (default: the system clock).</param>
        /// <param name="Logger">An optional logger.</param>
        public S2Session(IS2Medium         Medium,
                         S2SessionOptions  Options,
                         TimeProvider?     TimeProvider   = null,
                         ILogger?          Logger         = null)
        {

            Options.Validate();

            this.Medium        = Medium;
            this.Options       = Options;
            this.TimeProvider  = TimeProvider ?? System.TimeProvider.System;
            this.logger        = Logger;
            this.inbound       = Channel.CreateBounded<IS2Message>(new BoundedChannelOptions(Options.InboundChannelCapacity) {
                                     SingleReader  = true,
                                     SingleWriter  = true,
                                     FullMode      = BoundedChannelFullMode.Wait
                                 });

            if (Options.Mode == S2SessionMode.S2Connect)
                NegotiatedVersion = Options.NegotiatedVersion;

            // Subscribe immediately: a peer (e.g. an s2-python RM sending its Handshake right after
            // the WebSocket opened) may send before StartAsync runs; such messages are queued in the
            // inbound channel and processed once the consumer loop starts.
            Medium.OnTextReceived  += OnMediumTextReceived;
            Medium.OnClosed        += OnMediumClosed;

        }

        #endregion


        #region Handler registration

        #region On<TMessage>(Handler)

        /// <summary>
        /// Register a handler for messages of the given type (a concrete message class or an
        /// interface such as IInstruction). Handlers registered for the same type run in
        /// registration order; handlers of different matching types (e.g. the concrete class
        /// and IInstruction) run grouped by type. The worst status returned wins. Dispose the
        /// registration to remove the handler.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="Handler">The handler.</param>
        public IDisposable On<TMessage>(S2MessageHandler<TMessage> Handler)

            where TMessage : IS2Message

        {

            Func<S2Session, IS2Message, CancellationToken, Task<ReceptionStatusValue?>> wrapper
                = (session, message, ct) => Handler(session, (TMessage) message, ct);

            lock (stateLock)
            {

                if (!handlers.TryGetValue(typeof(TMessage), out var list))
                {
                    list = [];
                    handlers[typeof(TMessage)] = list;
                }

                list.Add(wrapper);

            }

            return new HandlerRegistration(this, typeof(TMessage), wrapper);

        }

        private sealed class HandlerRegistration(S2Session  Session,
                                                 Type       MessageType,
                                                 Func<S2Session, IS2Message, CancellationToken, Task<ReceptionStatusValue?>>  Wrapper) : IDisposable
        {

            public void Dispose()
            {
                lock (Session.stateLock)
                {
                    if (Session.handlers.TryGetValue(MessageType, out var list))
                        list.Remove(Wrapper);
                }
            }

        }

        #endregion

        #region RegisterControlType(Handler)

        /// <summary>
        /// Register the handler of a control type. It is activated when the control type is
        /// selected (received SelectControlType on the RM, sent SelectControlType on the CEM).
        /// </summary>
        /// <param name="Handler">A control type handler.</param>
        public void RegisterControlType(IS2ControlTypeHandler Handler)
        {
            lock (stateLock)
            {
                controlTypeHandlers[Handler.ControlType] = Handler;
            }
        }

        #endregion

        #endregion


        #region StartAsync(CancellationToken = default)

        /// <summary>
        /// Start the session: subscribe to the medium, start the consumer loop and, in plain
        /// mode, send the Handshake.
        /// </summary>
        /// <param name="CancellationToken">A token to cancel the start.</param>
        public async Task StartAsync(CancellationToken CancellationToken = default)
        {

            if (!Medium.IsConnected)
                throw new InvalidOperationException("The medium is not connected!");

            lock (stateLock)
            {

                if (State != S2SessionState.Created)
                    throw new InvalidOperationException($"The session was already started (state {State})!");

                State = Options.Mode == S2SessionMode.Plain
                            ? S2SessionState.AwaitingHandshake
                            : S2SessionState.WebSocketConnected;

            }

            consumerTask = Task.Run(ConsumeAsync, CancellationToken.None);

            await RaiseStateChanged(S2SessionState.Created, State).ConfigureAwait(false);

            if (Options.Mode == S2SessionMode.Plain && Options.SendHandshakeOnStart)
                await SendAsync(new Handshake(Role, Options.SupportedVersions), CancellationToken).ConfigureAwait(false);

        }

        #endregion

        #region SendAsync(Message, CancellationToken = default)

        /// <summary>
        /// Send the given message without waiting for its ReceptionStatus. The message rules
        /// of the current state are enforced for the local role; a violation is returned as
        /// an error result and nothing is sent.
        /// </summary>
        /// <param name="Message">A message.</param>
        /// <param name="CancellationToken">A token to cancel the send operation.</param>
        public async Task<S2SendResult> SendAsync(IS2Message         Message,
                                                  CancellationToken  CancellationToken   = default)
        {

            S2RuleVerdict verdict;

            lock (stateLock)
            {
                verdict = S2MessageRules.Check(Message.MessageType, Role, Mode, State, ActiveControlType);
            }

            if (verdict != S2RuleVerdict.Allowed)
            {
                var result = new S2SendResult(S2SendStatus.Error, $"A {Role} must not send '{Message.MessageType}' in state {State} ({verdict})!");
                await RaiseMessageSent(Message, result).ConfigureAwait(false);
                return result;
            }

            // The CEM activates the control type before the SelectControlType is on the wire, so
            // that the RM's first control-type message can never be rejected as out of state.
            if (Message is SelectControlType selectControlType && Role == EnergyManagementRole.CEM)
                await ActivateControlTypeAsync(selectControlType.ControlType, CancellationToken).ConfigureAwait(false);

            var sendResult = await Medium.SendAsync(Message.ToJSON().ToString(Newtonsoft.Json.Formatting.None),
                                                    CancellationToken).ConfigureAwait(false);

            if (sendResult.IsSuccess)
                await AfterSentAsync(Message, CancellationToken).ConfigureAwait(false);

            await RaiseMessageSent(Message, sendResult).ConfigureAwait(false);

            return sendResult;

        }

        #endregion

        #region SendAndAwaitReceptionStatusAsync(Message, CancellationToken = default)

        /// <summary>
        /// Send the given message and wait for its ReceptionStatus (timeout from the options).
        /// The awaiter is registered before the message is sent. A received PERMANENT_ERROR
        /// closes the session; a TEMPORARY_ERROR is returned so that the caller can retry.
        /// </summary>
        /// <param name="Message">A message with a message identification.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        public async Task<S2SendOutcome> SendAndAwaitReceptionStatusAsync(IS2MessageWithId   Message,
                                                                          CancellationToken  CancellationToken   = default)
        {

            using var registration = awaiter.Register(Message.MessageId, out var receptionTask);

            var sendResult = await SendAsync(Message, CancellationToken).ConfigureAwait(false);

            if (!sendResult.IsSuccess)
                return new S2SendOutcome(sendResult, null, false);

            try
            {

                var receptionStatus = await receptionTask.WaitAsync(Options.ReceptionStatusTimeout,
                                                                    TimeProvider,
                                                                    CancellationToken).ConfigureAwait(false);

                if (receptionStatus.Status == ReceptionStatusValue.PermanentError)
                    await CloseAsync(new S2CloseReason($"The peer answered '{Message.MessageType}' with PERMANENT_ERROR: {receptionStatus.DiagnosticLabel}", true),
                                     CancellationToken).ConfigureAwait(false);

                return new S2SendOutcome(sendResult, receptionStatus, false);

            }
            catch (TimeoutException)
            {

                if (logger is not null && logger.IsEnabled(LogLevel.Warning))
                    logger.LogWarning("S2 session {SessionId}: no ReceptionStatus for {MessageType} [{MessageId}] within {Timeout}.",
                                      Id, Message.MessageType, Message.MessageId, Options.ReceptionStatusTimeout);

                if (Options.CloseOnReceptionTimeout)
                    await CloseAsync(new S2CloseReason($"No ReceptionStatus for '{Message.MessageType}' within {Options.ReceptionStatusTimeout}.", true),
                                     CancellationToken).ConfigureAwait(false);

                return new S2SendOutcome(sendResult, null, true);

            }
            catch (OperationCanceledException) when (!CancellationToken.IsCancellationRequested)
            {
                // The awaiter was cancelled because the session closed.
                return new S2SendOutcome(new S2SendResult(S2SendStatus.ConnectionClosed, "The session was closed while waiting for the ReceptionStatus."), null, false);
            }

        }

        #endregion

        #region CloseAsync(Reason, CancellationToken = default)

        /// <summary>
        /// Close the session and the medium. Pending awaiters fail, the consumer loop stops.
        /// </summary>
        /// <param name="Reason">Why the session is closed.</param>
        /// <param name="CancellationToken">A token to cancel the close operation.</param>
        public async Task CloseAsync(S2CloseReason      Reason,
                                     CancellationToken  CancellationToken   = default)
        {

            if (Interlocked.Exchange(ref closed, 1) != 0)
                return;

            S2SessionState oldState;

            lock (stateLock)
            {
                oldState           = State;
                State              = S2SessionState.Disconnected;
                ActiveControlType  = null;
            }

            Medium.OnTextReceived  -= OnMediumTextReceived;
            Medium.OnClosed        -= OnMediumClosed;

            inbound.Writer.TryComplete();
            awaiter.FailAll(new OperationCanceledException("The S2 session was closed: " + Reason.Description));

            await cancellation.CancelAsync().ConfigureAwait(false);

            if (activeHandler is not null)
            {

                try
                {
                    await activeHandler.DeactivateAsync(this, CancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "S2 session {SessionId}: deactivating the control type handler on close failed.", Id);
                }

                activeHandler = null;

            }

            if (activeHandlerRegistration is not null)
            {
                activeHandlerRegistration.Dispose();
                activeHandlerRegistration = null;
            }

            if (Reason.IsLocal && Medium.IsConnected)
                await Medium.CloseAsync(Reason, CancellationToken).ConfigureAwait(false);

            await RaiseStateChanged(oldState, S2SessionState.Disconnected).ConfigureAwait(false);

            await OnClosed.InvokeAllAsync(handler => handler(Timestamp.Now, this, Reason), logger).ConfigureAwait(false);

        }

        #endregion


        #region (private) Inbound pipeline

        private async Task OnMediumTextReceived(IS2Medium          Medium,
                                                String             Text,
                                                CancellationToken  CancellationToken)
        {

            if (!S2MessageParser.TryParse(Text, Options.ParserOptions, out var message, out var error))
            {

                logger?.LogDebug("S2 session {SessionId}: rejected message ({Status}): {Diagnostic}", Id, error.Status, error.DiagnosticLabel);

                // A ReceptionStatus, even a malformed one, is never answered with a ReceptionStatus.
                if (!error.IsReceptionStatus)
                    await SendReceptionStatusAsync(error.ToReceptionStatus(), CancellationToken).ConfigureAwait(false);

                return;

            }

            if (message is ReceptionStatus receptionStatus)
            {

                if (!awaiter.TryComplete(receptionStatus))
                    await OnUnmatchedReceptionStatus.InvokeAllAsync(handler => handler(Timestamp.Now, this, receptionStatus), logger).ConfigureAwait(false);

                await RaiseMessageReceived(receptionStatus, ReceptionStatusValue.OK).ConfigureAwait(false);
                return;

            }

            try
            {
                await inbound.Writer.WriteAsync(message, cancellation.Token).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            { }
            catch (OperationCanceledException)
            { }

        }

        private async Task OnMediumClosed(IS2Medium      Medium,
                                          S2CloseReason  Reason)

            => await CloseAsync(Reason with { IsLocal = false }).ConfigureAwait(false);


        private async Task ConsumeAsync()
        {

            try
            {

                await foreach (var message in inbound.Reader.ReadAllAsync(cancellation.Token).ConfigureAwait(false))
                {

                    try
                    {
                        await DispatchAsync(message, cancellation.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        logger?.LogError(e, "S2 session {SessionId}: unhandled error while processing {MessageType}.", Id, message.MessageType);
                        await CloseAsync(new S2CloseReason("Unhandled error while processing " + message.MessageType, true, e)).ConfigureAwait(false);
                        break;
                    }

                }

            }
            catch (OperationCanceledException)
            { }

        }

        private async Task DispatchAsync(IS2Message         Message,
                                         CancellationToken  CancellationToken)
        {

            var messageWithId  = Message as IS2MessageWithId;
            var status         = ReceptionStatusValue.OK;
            var diagnostic     = (String?) null;
            var handlerThrew   = false;

            #region 1. Message rules of the current state

            S2RuleVerdict verdict;

            lock (stateLock)
            {
                verdict = S2MessageRules.Check(Message.MessageType, PeerRole, Mode, State, ActiveControlType);
            }

            if (verdict != S2RuleVerdict.Allowed)
            {
                status      = ReceptionStatusValue.InvalidContent;
                diagnostic  = $"'{Message.MessageType}' is not allowed in state {State} ({verdict}).";
            }

            #endregion

            #region 2. Session-level content checks

            if (status == ReceptionStatusValue.OK)
            {
                var contentError = CheckContent(Message);
                if (contentError is not null)
                {
                    status      = ReceptionStatusValue.InvalidContent;
                    diagnostic  = contentError;
                }
            }

            #endregion

            #region 3. Handlers

            if (status == ReceptionStatusValue.OK)
            {

                List<Func<S2Session, IS2Message, CancellationToken, Task<ReceptionStatusValue?>>> matching;

                lock (stateLock)
                {
                    matching = handlers.Where (kvp => kvp.Key.IsInstanceOfType(Message)).
                                        SelectMany(kvp => kvp.Value).
                                        ToList();
                }

                foreach (var handler in matching)
                {

                    try
                    {

                        var result = await handler(this, Message, CancellationToken).ConfigureAwait(false);

                        if (result.HasValue && Worse(result.Value, status))
                        {
                            status      = result.Value;
                            diagnostic  = $"Handler rejected '{Message.MessageType}' with {status}.";
                        }

                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        logger?.LogError(e, "S2 session {SessionId}: handler for {MessageType} failed.", Id, Message.MessageType);
                        status        = ReceptionStatusValue.PermanentError;
                        diagnostic    = $"Handler for '{Message.MessageType}' failed: {e.Message}";
                        handlerThrew  = true;
                        break;
                    }

                }

            }

            #endregion

            #region 4. Exactly one ReceptionStatus

            if (messageWithId is not null)
                await SendReceptionStatusAsync(new ReceptionStatus(messageWithId.MessageId, status, diagnostic), CancellationToken).ConfigureAwait(false);

            await RaiseMessageReceived(Message, status).ConfigureAwait(false);

            #endregion

            #region 5. Bookkeeping and follow-up messages

            if (handlerThrew)
            {
                await CloseAsync(new S2CloseReason($"A handler for '{Message.MessageType}' failed.", true), CancellationToken).ConfigureAwait(false);
                return;
            }

            if (status == ReceptionStatusValue.OK)
                await AfterReceivedAsync(Message, CancellationToken).ConfigureAwait(false);

            #endregion

        }

        private static Boolean Worse(ReceptionStatusValue  Candidate,
                                     ReceptionStatusValue  Current)
            => Rank(Candidate) > Rank(Current);

        private static Int32 Rank(ReceptionStatusValue Status)
        {

            if (Status == ReceptionStatusValue.OK)              return 0;
            if (Status == ReceptionStatusValue.InvalidData)     return 1;
            if (Status == ReceptionStatusValue.InvalidMessage)  return 2;
            if (Status == ReceptionStatusValue.InvalidContent)  return 3;
            if (Status == ReceptionStatusValue.TemporaryError)  return 4;
            if (Status == ReceptionStatusValue.PermanentError)  return 5;

            return 3;

        }

        private async Task SendReceptionStatusAsync(ReceptionStatus    ReceptionStatus,
                                                    CancellationToken  CancellationToken)
        {

            var result = await Medium.SendAsync(ReceptionStatus.ToJSON().ToString(Newtonsoft.Json.Formatting.None),
                                                CancellationToken).ConfigureAwait(false);

            await RaiseMessageSent(ReceptionStatus, result).ConfigureAwait(false);

        }

        #endregion

        #region (private) Content checks

        /// <summary>
        /// Cross-message checks against the registry (INVALID_CONTENT when they fail).
        /// </summary>
        private String? CheckContent(IS2Message Message)
        {

            lock (stateLock)
            {

                switch (Message)
                {

                    case SelectControlType select:
                        if (Registry.ResourceManagerDetails is not null &&
                            select.ControlType != ControlType.NoSelection &&
                            !Registry.ResourceManagerDetails.AvailableControlTypes.Contains(select.ControlType))
                            return $"Control type '{select.ControlType}' is not offered by the Resource Manager.";
                        if (select.ControlType != ControlType.NoSelection &&
                            select.ControlType != ControlType.NotControllable &&
                            S2MessageRules.ControlTypePrefix(select.ControlType) is null)
                            return $"Unknown control type '{select.ControlType}'.";
                        return null;

                    case RevokeObject revoke:
                        return Registry.TryGetRevokable(revoke.ObjectType, revoke.ObjectId, out _)
                                   ? null
                                   : $"Unknown revokable object '{revoke.ObjectType}' with id '{revoke.ObjectId}'.";

                    case InstructionStatusUpdate update:
                        return Registry.TryGetInstruction(update.InstructionId, out _)
                                   ? null
                                   : $"Unknown instruction '{update.InstructionId}'.";

                    case HandshakeResponse response when Role == EnergyManagementRole.RM:
                        return Options.SupportedVersions.Contains(response.SelectedProtocolVersion, StringComparer.Ordinal)
                                   ? null
                                   : $"The selected protocol version '{response.SelectedProtocolVersion}' was not offered.";

                    // ResourceManagerDetails.currency is "mandatory if cost information is published" (PLAN.md §3.3).
                    case FRBC_SystemDescription frbc when PublishesCosts(frbc) && Registry.ResourceManagerDetails?.Currency is null:
                    case OMBC_SystemDescription ombc when PublishesCosts(ombc) && Registry.ResourceManagerDetails?.Currency is null:
                    case DDBC_SystemDescription ddbc when PublishesCosts(ddbc) && Registry.ResourceManagerDetails?.Currency is null:
                        return "The system description publishes running or transition costs, but the ResourceManagerDetails declare no currency.";

                    default:
                        return CheckDeclaredIds(Message);

                }

            }

        }

        private static Boolean PublishesCosts(FRBC_SystemDescription SystemDescription)
            => SystemDescription.Actuators.Any(actuator => actuator.OperationModes.Any(mode => mode.Elements.Any(element => element.RunningCosts is not null)) ||
                                                           actuator.Transitions.   Any(transition => transition.TransitionCosts is not null));

        private static Boolean PublishesCosts(OMBC_SystemDescription SystemDescription)
            => SystemDescription.OperationModes.Any(mode       => mode.RunningCosts         is not null) ||
               SystemDescription.Transitions.   Any(transition => transition.TransitionCosts is not null);

        private static Boolean PublishesCosts(DDBC_SystemDescription SystemDescription)
            => SystemDescription.Actuators.Any(actuator => actuator.OperationModes.Any(mode => mode.RunningCosts is not null) ||
                                                           actuator.Transitions.   Any(transition => transition.TransitionCosts is not null));

        /// <summary>
        /// Instructions, statuses and timer statuses must refer to identifications the active
        /// system description declared (checked only when a system description is known).
        /// </summary>
        private String? CheckDeclaredIds(IS2Message Message)
        {

            var hasActuators  = Registry.Actuators.Count      > 0;
            var hasModes      = Registry.OperationModes.Count > 0;
            var hasTimers     = Registry.Timers.Count         > 0;

            String? actuator(Actuator_Id id)
                => hasActuators && !Registry.Actuators.Contains(id) ? $"Unknown actuator '{id}'." : null;

            String? mode(OperationMode_Id id)
                => hasModes && !Registry.OperationModes.Contains(id) ? $"Unknown operation mode '{id}'." : null;

            String? timer(Timer_Id id)
                => hasTimers && !Registry.Timers.Contains(id) ? $"Unknown timer '{id}'." : null;

            return Message switch {

                FRBC_Instruction     m => actuator(m.ActuatorId) ?? mode(m.OperationMode),
                DDBC_Instruction     m => actuator(m.ActuatorId) ?? mode(m.OperationModeId),
                OMBC_Instruction     m => mode(m.OperationModeId),

                FRBC_ActuatorStatus  m => actuator(m.ActuatorId) ?? mode(m.ActiveOperationModeId),
                DDBC_ActuatorStatus  m => actuator(m.ActuatorId) ?? mode(m.ActiveOperationModeId),
                OMBC_Status          m => mode(m.ActiveOperationModeId),

                FRBC_TimerStatus     m => actuator(m.ActuatorId) ?? timer(m.TimerId),
                DDBC_TimerStatus     m => actuator(m.ActuatorId) ?? timer(m.TimerId),
                OMBC_TimerStatus     m => timer(m.TimerId),

                _                      => null

            };

        }

        #endregion

        #region (private) Bookkeeping after receiving / sending

        private async Task AfterReceivedAsync(IS2Message         Message,
                                              CancellationToken  CancellationToken)
        {

            switch (Message)
            {

                case Handshake handshake:
                    await OnHandshakeReceivedAsync(handshake, CancellationToken).ConfigureAwait(false);
                    break;

                case HandshakeResponse response:
                    await OnHandshakeResponseReceivedAsync(response).ConfigureAwait(false);
                    break;

                case ResourceManagerDetails details:
                    lock (stateLock) { Registry.ResourceManagerDetails = details; }
                    break;

                case SelectControlType select:
                    await ActivateControlTypeAsync(select.ControlType, CancellationToken).ConfigureAwait(false);
                    break;

                case SessionRequest request:
                    await OnSessionRequest.InvokeAllAsync(handler => handler(Timestamp.Now, this, request), logger).ConfigureAwait(false);
                    await CloseAsync(new S2CloseReason($"The peer requested {request.Request}: {request.DiagnosticLabel}", true), CancellationToken).ConfigureAwait(false);
                    break;

                case RevokeObject revoke:
                    lock (stateLock) { Registry.Revoke(revoke, TimeProvider.GetUtcNow()); }
                    break;

                case InstructionStatusUpdate update:
                    lock (stateLock) { Registry.TryUpdateInstruction(update, out _); }
                    break;

                default:
                    lock (stateLock)
                    {
                        RegisterDeclaredIds(Message);
                        if (Message is IRevokable revokable)
                            Registry.Register(revokable, S2MessageDirection.Received, TimeProvider.GetUtcNow());
                    }
                    break;

            }

        }

        private async Task AfterSentAsync(IS2Message         Message,
                                          CancellationToken  CancellationToken)
        {

            switch (Message)
            {

                case ResourceManagerDetails details:
                    lock (stateLock) { Registry.ResourceManagerDetails = details; }
                    break;

                // SelectControlType sent by the CEM: the control type was activated before the send (see SendAsync).

                case HandshakeResponse response when Role == EnergyManagementRole.CEM:
                    await CompleteHandshakeAsync(response.SelectedProtocolVersion).ConfigureAwait(false);
                    break;

                case RevokeObject revoke:
                    lock (stateLock) { Registry.Revoke(revoke, TimeProvider.GetUtcNow()); }
                    break;

                case InstructionStatusUpdate update:
                    lock (stateLock) { Registry.TryUpdateInstruction(update, out _); }
                    break;

                default:
                    lock (stateLock)
                    {
                        RegisterDeclaredIds(Message);
                        if (Message is IRevokable revokable)
                            Registry.Register(revokable, S2MessageDirection.Sent, TimeProvider.GetUtcNow());
                    }
                    break;

            }

        }

        private void RegisterDeclaredIds(IS2Message Message)
        {

            switch (Message)
            {

                case FRBC_SystemDescription frbc:
                    Registry.SetDeclaredIds(frbc.Actuators.Select(a => a.Id),
                                            frbc.Actuators.SelectMany(a => a.OperationModes.Select(om => om.Id)),
                                            frbc.Actuators.SelectMany(a => a.Timers.Select(t => t.Id)));
                    break;

                case DDBC_SystemDescription ddbc:
                    Registry.SetDeclaredIds(ddbc.Actuators.Select(a => a.Id),
                                            ddbc.Actuators.SelectMany(a => a.OperationModes.Select(om => om.Id)),
                                            ddbc.Actuators.SelectMany(a => a.Timers.Select(t => t.Id)));
                    break;

                case OMBC_SystemDescription ombc:
                    Registry.SetDeclaredIds([],
                                            ombc.OperationModes.Select(om => om.Id),
                                            ombc.Timers.Select(t => t.Id));
                    break;

            }

        }

        #endregion

        #region (private) Handshake (plain mode)

        private async Task OnHandshakeReceivedAsync(Handshake          Handshake,
                                                    CancellationToken  CancellationToken)
        {

            if (Role != EnergyManagementRole.CEM)
                return;

            // The CEM selects the first version offered by the RM that it supports itself.
            var selected = Handshake.SupportedProtocolVersions?.FirstOrDefault(version => Options.SupportedVersions.Contains(version, StringComparer.Ordinal));

            if (selected is null)
            {

                var offered = Handshake.SupportedProtocolVersions is not null
                                  ? String.Join(", ", Handshake.SupportedProtocolVersions)
                                  : "(none)";

                await SendAsync(new SessionRequest(SessionRequestType.Terminate,
                                                   $"No common S2 JSON version: RM offers {offered}, CEM supports {String.Join(", ", Options.SupportedVersions)}."),
                                CancellationToken).ConfigureAwait(false);

                await CloseAsync(new S2CloseReason("No common S2 JSON version.", true), CancellationToken).ConfigureAwait(false);
                return;

            }

            await SendAsync(new HandshakeResponse(selected), CancellationToken).ConfigureAwait(false);

        }

        private Task OnHandshakeResponseReceivedAsync(HandshakeResponse Response)
            => Role == EnergyManagementRole.RM
                   ? CompleteHandshakeAsync(Response.SelectedProtocolVersion)
                   : Task.CompletedTask;

        private async Task CompleteHandshakeAsync(String SelectedVersion)
        {

            S2SessionState oldState;

            lock (stateLock)
            {

                if (State != S2SessionState.AwaitingHandshake)
                    return;

                oldState           = State;
                NegotiatedVersion  = SelectedVersion;
                State              = S2SessionState.WebSocketConnected;

            }

            await RaiseStateChanged(oldState, S2SessionState.WebSocketConnected).ConfigureAwait(false);

        }

        #endregion

        #region (private) Control type activation

        private async Task ActivateControlTypeAsync(ControlType        ControlType,
                                                    CancellationToken  CancellationToken)
        {

            IS2ControlTypeHandler?  oldHandler;
            IS2ControlTypeHandler?  newHandler;
            S2SessionState          oldState;
            S2SessionState          newState;
            var                     deactivate = ControlType == ControlType.NoSelection ||
                                                 ControlType == ControlType.NotControllable;

            lock (stateLock)
            {

                oldState = State;

                if (oldState is not (S2SessionState.WebSocketConnected or S2SessionState.ControlTypeActivated))
                    return;

                oldHandler  = activeHandler;
                newHandler  = null;

                if (!deactivate)
                    controlTypeHandlers.TryGetValue(ControlType, out newHandler);

            }

            // 1. The old handler is deactivated while the registry still holds its objects
            //    (it may want to report or abort pending instructions).
            if (oldHandler is not null)
            {
                await oldHandler.DeactivateAsync(this, CancellationToken).ConfigureAwait(false);
                activeHandlerRegistration?.Dispose();
                activeHandlerRegistration = null;
                activeHandler             = null;
            }

            // 2. Then the state switches and the registry is cleared.
            lock (stateLock)
            {
                Registry.Clear();
                ActiveControlType  = deactivate ? null : ControlType;
                newState           = deactivate ? S2SessionState.WebSocketConnected : S2SessionState.ControlTypeActivated;
                State              = newState;
            }

            await RaiseStateChanged(oldState, newState).ConfigureAwait(false);

            // 3. The new handler registers its message handlers and is activated.
            if (newHandler is not null)
            {
                activeHandler              = newHandler;
                activeHandlerRegistration  = newHandler.RegisterHandlers(this);
                await newHandler.ActivateAsync(this, CancellationToken).ConfigureAwait(false);
            }

        }

        #endregion

        #region (private) Events

        private Task RaiseStateChanged(S2SessionState  OldState,
                                       S2SessionState  NewState)
            => OnStateChanged.InvokeAllAsync(handler => handler(Timestamp.Now, this, OldState, NewState, ActiveControlType), logger);

        private Task RaiseMessageReceived(IS2Message            Message,
                                          ReceptionStatusValue  Status)
            => OnMessageReceived.InvokeAllAsync(handler => handler(Timestamp.Now, this, Message, Status), logger);

        private Task RaiseMessageSent(IS2Message    Message,
                                      S2SendResult  Result)
            => OnMessageSent.InvokeAllAsync(handler => handler(Timestamp.Now, this, Message, Result), logger);

        #endregion


        #region DisposeAsync()

        /// <summary>
        /// Close and dispose the session (the medium is closed as well).
        /// </summary>
        public async ValueTask DisposeAsync()
        {

            await CloseAsync(new S2CloseReason("disposed", true)).ConfigureAwait(false);

            try
            {
                if (consumerTask is not null)
                    await consumerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            { }

            cancellation.Dispose();

        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"S2 session {Id} ({Role}, {State}{(ActiveControlType is not null ? ", " + ActiveControlType : "")}) on {Medium.Description}";

        #endregion

    }

}

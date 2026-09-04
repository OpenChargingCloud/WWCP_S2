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

using Microsoft.Extensions.Time.Testing;

using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Session
{

    /// <summary>
    /// CEM and RM sessions connected through the in-memory medium (PLAN.md Phase 3 tests).
    /// </summary>
    [TestFixture]
    public sealed class S2SessionTests
    {

        #region Helpers

        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

        private static (S2Session CEM, S2Session RM, FakeTimeProvider Clock) CreatePair(S2SessionMode Mode = S2SessionMode.S2Connect)
        {

            var (a, b)  = InMemoryS2Medium.CreatePair("CEM", "RM");
            var clock   = new FakeTimeProvider(new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero));

            var cem = new S2Session(a, new S2SessionOptions {
                                           Role               = EnergyManagementRole.CEM,
                                           Mode               = Mode,
                                           NegotiatedVersion  = Mode == S2SessionMode.S2Connect ? Version.S2JSONVersion : null
                                       }, clock);

            var rm  = new S2Session(b, new S2SessionOptions {
                                           Role               = EnergyManagementRole.RM,
                                           Mode               = Mode,
                                           NegotiatedVersion  = Mode == S2SessionMode.S2Connect ? Version.S2JSONVersion : null
                                       }, clock);

            return (cem, rm, clock);

        }

        private static ResourceManagerDetails EVChargerDetails()
            => new (Resource_Id.Parse("acme_ev_xxxxxx"),
                    [ new Role(RoleType.EnergyConsumer, Commodity.Electricity) ],
                    Duration.FromMilliseconds(3000),
                    [ ControlType.FillRateBasedControl ],
                    false,
                    [ CommodityQuantity.ElectricPower3PhaseSymmetric ],
                    Name:          "My Electric Vehicle RM",
                    Manufacturer:  "ACME",
                    Model:         "WallBox-b100",
                    SerialNumber:  "123",
                    FirmwareVersion: "v1.0");

        private static FRBC_SystemDescription EVChargerSystemDescription()
        {

            var off       = new FRBC_OperationMode(OperationMode_Id.Parse("om1"),
                                                   [ new FRBC_OperationModeElement(new NumberRange(0, 100), new NumberRange(0, 0),
                                                                                   [ new PowerRange(0, 0, CommodityQuantity.ElectricPower3PhaseSymmetric) ]) ],
                                                   false, "Off");

            var charging  = new FRBC_OperationMode(OperationMode_Id.Parse("om2"),
                                                   [ new FRBC_OperationModeElement(new NumberRange(0, 100), new NumberRange(0.00065, 0.0051),
                                                                                   [ new PowerRange(1400, 11000, CommodityQuantity.ElectricPower3PhaseSymmetric) ]) ],
                                                   false, "Charging");

            var actuator  = new FRBC_ActuatorDescription(Actuator_Id.Parse("actuator1"),
                                                         [ Commodity.Electricity ],
                                                         [ off, charging ],
                                                         [ new Transition(Transition_Id.Parse("transition1"), off.Id, charging.Id, [], [], false, null, Duration.FromMilliseconds(3000)),
                                                           new Transition(Transition_Id.Parse("transition2"), charging.Id, off.Id, [], [], false, null, Duration.FromMilliseconds(3000)) ],
                                                         [],
                                                         "EV charger");

            var storage   = new FRBC_StorageDescription(false, true, false, new NumberRange(0, 100), "Battery SoC", "EV Battery SoC");

            return new FRBC_SystemDescription(new DateTimeOffset(2019, 8, 24, 14, 15, 22, TimeSpan.Zero), [ actuator ], storage);

        }

        private static async Task<T> WaitFor<T>(TaskCompletionSource<T> Source)
            => await Source.Task.WaitAsync(Wait);

        #endregion


        #region S2 Connect mode: full FRBC example flow

        [Test]
        [S2C("Examples.EV.FRBC")]
        public async Task S2ConnectMode_FRBCExampleFlow_WorksEndToEnd()
        {

            var (cem, rm, clock) = CreatePair();
            await using var cemDisposal = cem;
            await using var rmDisposal  = rm;

            var detailsReceived      = new TaskCompletionSource<ResourceManagerDetails>();
            var systemReceived       = new TaskCompletionSource<FRBC_SystemDescription>();
            var instructionReceived  = new TaskCompletionSource<FRBC_Instruction>();
            var statusReceived       = new TaskCompletionSource<InstructionStatusUpdate>();

            cem.On<ResourceManagerDetails>((s, m, ct) => { detailsReceived.TrySetResult(m); return Task.FromResult<ReceptionStatusValue?>(null); });
            cem.On<FRBC_SystemDescription>((s, m, ct) => { systemReceived. TrySetResult(m); return Task.FromResult<ReceptionStatusValue?>(null); });
            cem.On<InstructionStatusUpdate>((s, m, ct) => { statusReceived. TrySetResult(m); return Task.FromResult<ReceptionStatusValue?>(null); });
            rm. On<FRBC_Instruction>((s, m, ct) => { instructionReceived.TrySetResult(m); return Task.FromResult<ReceptionStatusValue?>(null); });

            await cem.StartAsync();
            await rm. StartAsync();

            Assert.That(cem.State, Is.EqualTo(S2SessionState.WebSocketConnected));
            Assert.That(rm. State, Is.EqualTo(S2SessionState.WebSocketConnected));

            // RM -> CEM: ResourceManagerDetails
            var outcome = await rm.SendAndAwaitReceptionStatusAsync(EVChargerDetails());
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);
            Assert.That((await WaitFor(detailsReceived)).ResourceId.ToString(), Is.EqualTo("acme_ev_xxxxxx"));

            // CEM -> RM: SelectControlType FRBC
            outcome = await cem.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType.FillRateBasedControl));
            Assert.That(outcome.IsOK,              Is.True, outcome.ReceptionStatus?.DiagnosticLabel);
            Assert.That(cem.State,                 Is.EqualTo(S2SessionState.ControlTypeActivated));
            Assert.That(cem.ActiveControlType,     Is.EqualTo(ControlType.FillRateBasedControl));
            Assert.That(rm.State,                  Is.EqualTo(S2SessionState.ControlTypeActivated));

            // RM -> CEM: FRBC.SystemDescription
            var system = EVChargerSystemDescription();
            outcome = await rm.SendAndAwaitReceptionStatusAsync(system);
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);
            Assert.That((await WaitFor(systemReceived)).Actuators, Has.Count.EqualTo(1));
            Assert.That(cem.Registry.OperationModes, Has.Count.EqualTo(2));

            // CEM -> RM: FRBC.Instruction
            var instruction = new FRBC_Instruction(Instruction_Id.Parse("instruction1"),
                                                   Actuator_Id.Parse("actuator1"),
                                                   OperationMode_Id.Parse("om2"),
                                                   0.5,
                                                   clock.GetUtcNow(),
                                                   false);
            outcome = await cem.SendAndAwaitReceptionStatusAsync(instruction);
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);
            Assert.That((await WaitFor(instructionReceived)).Id, Is.EqualTo(instruction.Id));
            Assert.That(rm.Registry.TryGetInstruction(instruction.Id, out var state), Is.True);
            Assert.That(state!.Status, Is.EqualTo(InstructionStatus.New));

            // RM -> CEM: InstructionStatusUpdate ACCEPTED
            outcome = await rm.SendAndAwaitReceptionStatusAsync(new InstructionStatusUpdate(instruction.Id, InstructionStatus.Accepted, clock.GetUtcNow()));
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);
            await WaitFor(statusReceived);
            Assert.That(cem.Registry.TryGetInstruction(instruction.Id, out state), Is.True);
            Assert.That(state!.Status, Is.EqualTo(InstructionStatus.Accepted));

            // An instruction referring to an unknown actuator is INVALID_CONTENT.
            outcome = await cem.SendAndAwaitReceptionStatusAsync(new FRBC_Instruction(Instruction_Id.NewRandom, Actuator_Id.Parse("ghost"), OperationMode_Id.Parse("om2"), 1, clock.GetUtcNow(), false));
            Assert.That(outcome.ReceptionStatus?.Status, Is.EqualTo(ReceptionStatusValue.InvalidContent));

            // RM -> CEM: SessionRequest TERMINATE closes both sides.
            var closed = new TaskCompletionSource<S2CloseReason>();
            cem.OnClosed += (ts, s, reason) => { closed.TrySetResult(reason); return Task.CompletedTask; };
            await rm.SendAsync(new SessionRequest(SessionRequestType.Terminate, "done"));
            await WaitFor(closed);
            Assert.That(cem.State, Is.EqualTo(S2SessionState.Disconnected));

        }

        #endregion

        #region Rules and content checks

        [Test]
        public async Task OutOfStateMessage_IsAnsweredWithInvalidContent_AndNotDispatched()
        {

            var (cem, rm, clock) = CreatePair();
            await using var cemDisposal = cem;
            await using var rmDisposal  = rm;

            var dispatched = false;

            await cem.StartAsync();
            await rm. StartAsync();

            // Sending is refused locally (the CEM knows no control type is active).
            var local = await cem.SendAsync(new FRBC_Instruction(Instruction_Id.NewRandom, Actuator_Id.Parse("actuator1"), OperationMode_Id.Parse("om1"), 1, clock.GetUtcNow(), false));
            Assert.That(local.Status, Is.EqualTo(S2SendStatus.Error));

            // A wrong-direction message injected on the wire is INVALID_CONTENT on the receiver.
            var raw = new SelectControlType(ControlType.NoSelection).ToJSON().ToString();
            var (cemMedium, rmMedium) = InMemoryS2Medium.CreatePair();
            await using var m1 = cemMedium;
            await using var m2 = rmMedium;

            var receiver = new S2Session(rmMedium, new S2SessionOptions { Role = EnergyManagementRole.CEM, NegotiatedVersion = Version.S2JSONVersion });
            await using var receiverDisposal = receiver;
            receiver.On<SelectControlType>((s, m, ct) => { dispatched = true; return Task.FromResult<ReceptionStatusValue?>(null); });
            var answer   = new TaskCompletionSource<String>();
            cemMedium.OnTextReceived += (medium, text, ct) => { answer.TrySetResult(text); return Task.CompletedTask; };
            await receiver.StartAsync();
            await cemMedium.SendAsync(raw);

            var reception = await WaitFor(answer);
            Assert.That(ReceptionStatus.TryParse(S2JSONExtensions.ParseS2JSON(reception), out var status, out var error), Is.True, error);
            Assert.That(status!.Status, Is.EqualTo(ReceptionStatusValue.InvalidContent));
            Assert.That(dispatched,     Is.False);

        }

        [Test]
        public async Task SelectControlType_NotOfferedByTheRM_IsInvalidContent()
        {

            var (cem, rm, _) = CreatePair();
            await using var cemDisposal = cem;
            await using var rmDisposal  = rm;

            await cem.StartAsync();
            await rm. StartAsync();

            await rm.SendAndAwaitReceptionStatusAsync(EVChargerDetails());   // offers FRBC only

            var outcome = await cem.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType.OperationModeBasedControl));
            Assert.That(outcome.ReceptionStatus?.Status, Is.EqualTo(ReceptionStatusValue.InvalidContent));
            Assert.That(rm.State,                        Is.EqualTo(S2SessionState.WebSocketConnected));

        }

        [Test]
        public async Task ControlTypeSwitch_DeactivatesTheOldOne_AndClearsTheRegistry()
        {

            var (cem, rm, clock) = CreatePair();
            await using var cemDisposal = cem;
            await using var rmDisposal  = rm;

            await cem.StartAsync();
            await rm. StartAsync();

            await cem.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType.FillRateBasedControl));
            await rm. SendAndAwaitReceptionStatusAsync(EVChargerSystemDescription());
            Assert.That(cem.Registry.OperationModes, Is.Not.Empty);

            await cem.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType.NoSelection));
            Assert.That(cem.State,                   Is.EqualTo(S2SessionState.WebSocketConnected));
            Assert.That(cem.ActiveControlType,       Is.Null);
            Assert.That(rm.State,                    Is.EqualTo(S2SessionState.WebSocketConnected));
            Assert.That(cem.Registry.OperationModes, Is.Empty);
            Assert.That(rm.Registry.Instructions,    Is.Empty);

            // FRBC messages are now out of state again.
            var outcome = await rm.SendAsync(new FRBC_StorageStatus(42));
            Assert.That(outcome.Status, Is.EqualTo(S2SendStatus.Error));

        }

        #endregion

        #region Pipeline behaviour

        [Test]
        public async Task HandlerMaySendAndAwaitAReceptionStatus_WithoutDeadlock()
        {

            var (cem, rm, clock) = CreatePair();
            await using var cemDisposal = cem;
            await using var rmDisposal  = rm;

            var nested = new TaskCompletionSource<S2SendOutcome>();

            // The CEM reacts to the RM's details by selecting FRBC from inside the handler and awaits the RM's OK.
            cem.On<ResourceManagerDetails>(async (s, m, ct) => {
                nested.TrySetResult(await s.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType.FillRateBasedControl), ct));
                return null;
            });

            await cem.StartAsync();
            await rm. StartAsync();

            var outcome = await rm.SendAndAwaitReceptionStatusAsync(EVChargerDetails());
            Assert.That(outcome.IsOK, Is.True);

            var nestedOutcome = await WaitFor(nested);
            Assert.That(nestedOutcome.IsOK,   Is.True, nestedOutcome.ReceptionStatus?.DiagnosticLabel);
            Assert.That(rm.ActiveControlType, Is.EqualTo(ControlType.FillRateBasedControl));

        }

        [Test]
        public async Task Messages_ArriveInOrder_AndEachGetsExactlyOneReceptionStatus()
        {

            var (cem, rm, clock) = CreatePair();
            await using var cemDisposal = cem;
            await using var rmDisposal  = rm;

            var received   = new List<Int32>();
            var allDone    = new TaskCompletionSource<Boolean>();
            var statuses   = new List<ReceptionStatus>();

            cem.On<PowerMeasurement>((s, m, ct) => {
                received.Add((Int32) m.Values[0].Value);
                if (received.Count == 100) allDone.TrySetResult(true);
                return Task.FromResult<ReceptionStatusValue?>(null);
            });

            rm.OnMessageReceived += (ts, s, m, status) => {
                if (m is ReceptionStatus rs) lock (statuses) statuses.Add(rs);
                return Task.CompletedTask;
            };

            await cem.StartAsync();
            await rm. StartAsync();

            var sends = new List<Task<S2SendOutcome>>();
            for (var i = 0; i < 100; i++)
                sends.Add(rm.SendAndAwaitReceptionStatusAsync(new PowerMeasurement(clock.GetUtcNow(), [ new PowerValue(CommodityQuantity.ElectricPowerL1, i) ])));

            var outcomes = await Task.WhenAll(sends).WaitAsync(Wait);
            await WaitFor(allDone);

            Assert.That(outcomes.All(o => o.IsOK),                    Is.True);
            Assert.That(received,                                     Is.EqualTo(Enumerable.Range(0, 100)));
            Assert.That(statuses,                                     Has.Count.EqualTo(100), "exactly one ReceptionStatus per message");
            Assert.That(statuses.Select(s => s.SubjectMessageId).Distinct().Count(), Is.EqualTo(100));
            Assert.That(rm.PendingReceptions,                         Is.EqualTo(0));

        }

        [Test]
        public async Task ReceptionStatusTimeout_IsReported_ButDoesNotCloseByDefault()
        {

            var (a, b)  = InMemoryS2Medium.CreatePair();
            await using var m1 = a;
            await using var m2 = b;
            var clock   = new FakeTimeProvider();

            // The peer never answers: b has no session, it only swallows the text.
            b.OnTextReceived += (medium, text, ct) => Task.CompletedTask;

            var rm = new S2Session(a, new S2SessionOptions { Role = EnergyManagementRole.RM, NegotiatedVersion = Version.S2JSONVersion }, clock);
            await using var rmDisposal = rm;
            await rm.StartAsync();

            var pending = rm.SendAndAwaitReceptionStatusAsync(new PowerMeasurement(clock.GetUtcNow(), [ new PowerValue(CommodityQuantity.ElectricPowerL1, 1) ]));
            await Task.Delay(50);
            Assert.That(pending.IsCompleted,   Is.False);
            Assert.That(rm.PendingReceptions,  Is.EqualTo(1));

            clock.Advance(S2ConnectDefaults.ReceptionStatusTimeout + TimeSpan.FromSeconds(1));

            var outcome = await pending.WaitAsync(Wait);
            Assert.That(outcome.TimedOut,      Is.True);
            Assert.That(rm.State,              Is.EqualTo(S2SessionState.WebSocketConnected));
            Assert.That(rm.PendingReceptions,  Is.EqualTo(0));

        }

        [Test]
        public async Task PermanentError_FromThePeer_ClosesTheSession()
        {

            var (cem, rm, clock) = CreatePair();
            await using var cemDisposal = cem;
            await using var rmDisposal  = rm;

            cem.On<PowerMeasurement>((s, m, ct) => Task.FromResult<ReceptionStatusValue?>(ReceptionStatusValue.PermanentError));

            await cem.StartAsync();
            await rm. StartAsync();

            var outcome = await rm.SendAndAwaitReceptionStatusAsync(new PowerMeasurement(clock.GetUtcNow(), [ new PowerValue(CommodityQuantity.ElectricPowerL1, 1) ]));
            Assert.That(outcome.ReceptionStatus?.Status, Is.EqualTo(ReceptionStatusValue.PermanentError));
            Assert.That(rm.State,                        Is.EqualTo(S2SessionState.Disconnected));

        }

        [Test]
        public async Task HandlerException_IsAnsweredWithPermanentError_AndClosesTheSession()
        {

            var (cem, rm, clock) = CreatePair();
            await using var cemDisposal = cem;
            await using var rmDisposal  = rm;

            cem.On<PowerMeasurement>((s, m, ct) => throw new InvalidOperationException("boom"));

            var closed = new TaskCompletionSource<S2CloseReason>();
            cem.OnClosed += (ts, s, reason) => { closed.TrySetResult(reason); return Task.CompletedTask; };

            await cem.StartAsync();
            await rm. StartAsync();

            var outcome = await rm.SendAndAwaitReceptionStatusAsync(new PowerMeasurement(clock.GetUtcNow(), [ new PowerValue(CommodityQuantity.ElectricPowerL1, 1) ]));
            Assert.That(outcome.ReceptionStatus?.Status, Is.EqualTo(ReceptionStatusValue.PermanentError));

            var reason = await WaitFor(closed);
            Assert.That(reason.Description, Does.Contain("handler"));
            Assert.That(cem.State,          Is.EqualTo(S2SessionState.Disconnected));

        }

        #endregion

        #region Plain mode handshake

        [Test]
        [S2C("Examples.EV.Handshake")]
        public async Task PlainMode_Handshake_NegotiatesTheVersion_InEitherOrder()
        {

            foreach (var rmFirst in new[] { true, false })
            {

                var (cem, rm, _) = CreatePair(S2SessionMode.Plain);
                await using var cemDisposal = cem;
                await using var rmDisposal  = rm;

                var cemConnected = new TaskCompletionSource<Boolean>();
                var rmConnected  = new TaskCompletionSource<Boolean>();
                cem.OnStateChanged += (ts, s, o, n, a) => { if (n == S2SessionState.WebSocketConnected) cemConnected.TrySetResult(true); return Task.CompletedTask; };
                rm. OnStateChanged += (ts, s, o, n, a) => { if (n == S2SessionState.WebSocketConnected) rmConnected. TrySetResult(true); return Task.CompletedTask; };

                if (rmFirst) { await rm.StartAsync(); await cem.StartAsync(); }
                else         { await cem.StartAsync(); await rm.StartAsync(); }

                await WaitFor(cemConnected);
                await WaitFor(rmConnected);

                Assert.That(cem.NegotiatedVersion, Is.EqualTo(Version.S2JSONVersion), rmFirst ? "RM first" : "CEM first");
                Assert.That(rm.NegotiatedVersion,  Is.EqualTo(Version.S2JSONVersion));

            }

        }

        [Test]
        public async Task PlainMode_LegacyVersion_OfS2Python_IsAccepted()
        {

            var (a, b)  = InMemoryS2Medium.CreatePair();
            await using var m1 = a;
            await using var m2 = b;

            var cem = new S2Session(a, new S2SessionOptions { Role = EnergyManagementRole.CEM, Mode = S2SessionMode.Plain });
            await using var cemDisposal = cem;

            var texts = new List<String>();
            var responseReceived = new TaskCompletionSource<HandshakeResponse>();
            b.OnTextReceived += (medium, text, ct) => {
                lock (texts) texts.Add(text);
                if (S2MessageParser.TryParse(text, null, out var message, out _) && message is HandshakeResponse response)
                    responseReceived.TrySetResult(response);
                return Task.CompletedTask;
            };

            await cem.StartAsync();

            // An s2-python RM announces only the pre-release version string.
            await b.SendAsync(new Handshake(EnergyManagementRole.RM, [ "0.0.2-beta" ]).ToJSON().ToString());

            var response = await WaitFor(responseReceived);
            Assert.That(response.SelectedProtocolVersion, Is.EqualTo("0.0.2-beta"));
            Assert.That(cem.NegotiatedVersion,            Is.EqualTo("0.0.2-beta"));
            Assert.That(cem.State,                        Is.EqualTo(S2SessionState.WebSocketConnected));

            // The RM's Handshake was acknowledged with OK before the HandshakeResponse was sent.
            var kinds = texts.Select(t => S2MessageParser.TryParse(t, null, out var m, out _) ? m.MessageType : "?").ToList();
            Assert.That(kinds.IndexOf("ReceptionStatus"),   Is.LessThan(kinds.IndexOf("HandshakeResponse")));

        }

        [Test]
        public async Task PlainMode_NoCommonVersion_TerminatesTheSession()
        {

            var (a, b)  = InMemoryS2Medium.CreatePair();
            await using var m1 = a;
            await using var m2 = b;

            var cem = new S2Session(a, new S2SessionOptions { Role = EnergyManagementRole.CEM, Mode = S2SessionMode.Plain, SupportedVersions = [ "v1.0.0" ] });
            await using var cemDisposal = cem;

            var terminate = new TaskCompletionSource<SessionRequest>();
            var closed    = new TaskCompletionSource<S2CloseReason>();
            b.OnTextReceived += (medium, text, ct) => {
                if (S2MessageParser.TryParse(text, null, out var message, out _) && message is SessionRequest request)
                    terminate.TrySetResult(request);
                return Task.CompletedTask;
            };
            cem.OnClosed += (ts, s, reason) => { closed.TrySetResult(reason); return Task.CompletedTask; };

            await cem.StartAsync();
            await b.SendAsync(new Handshake(EnergyManagementRole.RM, [ "9.9.9" ]).ToJSON().ToString());

            var request = await WaitFor(terminate);
            Assert.That(request.Request, Is.EqualTo(SessionRequestType.Terminate));

            await WaitFor(closed);
            Assert.That(cem.State, Is.EqualTo(S2SessionState.Disconnected));

        }

        [Test]
        public async Task PlainMode_MessagesReceivedBeforeStart_AreProcessedAfterStart()
        {

            var (a, b)  = InMemoryS2Medium.CreatePair();
            await using var m1 = a;
            await using var m2 = b;

            var cem = new S2Session(a, new S2SessionOptions { Role = EnergyManagementRole.CEM, Mode = S2SessionMode.Plain });
            await using var cemDisposal = cem;

            var responseReceived = new TaskCompletionSource<HandshakeResponse>();
            b.OnTextReceived += (medium, text, ct) => {
                if (S2MessageParser.TryParse(text, null, out var message, out _) && message is HandshakeResponse response)
                    responseReceived.TrySetResult(response);
                return Task.CompletedTask;
            };

            // An s2-python RM sends its Handshake as soon as the socket is open, possibly before StartAsync ran.
            await b.SendAsync(new Handshake(EnergyManagementRole.RM, [ Version.S2JSONVersion ]).ToJSON().ToString());
            await Task.Delay(50);

            await cem.StartAsync();

            var response = await WaitFor(responseReceived);
            Assert.That(response.SelectedProtocolVersion, Is.EqualTo(Version.S2JSONVersion));
            Assert.That(cem.State,                        Is.EqualTo(S2SessionState.WebSocketConnected));

        }

        #endregion

        #region Reception-status and content rules

        [Test]
        [S2C("Rules.ReceptionStatus.NeverAcknowledged")]
        public async Task MalformedReceptionStatus_IsNeverAnswered()
        {

            var (a, b)  = InMemoryS2Medium.CreatePair();
            await using var m1 = a;
            await using var m2 = b;

            var session = new S2Session(a, new S2SessionOptions { Role = EnergyManagementRole.CEM, NegotiatedVersion = Version.S2JSONVersion });
            await using var sessionDisposal = session;

            var received = new List<String>();
            b.OnTextReceived += (medium, text, ct) => { lock (received) received.Add(text); return Task.CompletedTask; };

            await session.StartAsync();

            await b.SendAsync("""{ "message_type": "ReceptionStatus", "status": "OK" }""");
            await b.SendAsync("""{ "message_type": "SelectControlType", "message_id": "9a8d2f1c-0000-4000-8000-000000000001" }""");

            // The malformed SelectControlType (wrong direction anyway, but first of all not parseable) is answered, the malformed ReceptionStatus is not.
            await Task.Delay(200);

            lock (received)
            {
                Assert.That(received, Has.Count.EqualTo(1));
                Assert.That(ReceptionStatus.TryParse(S2JSONExtensions.ParseS2JSON(received[0]), out var status, out _), Is.True);
                Assert.That(status!.Status,                      Is.EqualTo(ReceptionStatusValue.InvalidMessage));
                Assert.That(status.SubjectMessageId.ToString(),  Is.EqualTo("9a8d2f1c-0000-4000-8000-000000000001"));
            }

        }

        [Test]
        [S2C("Rules.ResourceManagerDetails.CurrencyRequiredWithCosts")]
        public async Task SystemDescriptionWithCosts_RequiresACurrency_InTheResourceManagerDetails()
        {

            var (cem, rm, _) = CreatePair();
            await using var cemDisposal = cem;
            await using var rmDisposal  = rm;

            await cem.StartAsync();
            await rm. StartAsync();

            await rm. SendAndAwaitReceptionStatusAsync(EVChargerDetails());   // declares no currency
            await cem.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType.FillRateBasedControl));

            var withCosts = new FRBC_OperationMode(OperationMode_Id.Parse("om1"),
                                                   [ new FRBC_OperationModeElement(new NumberRange(0, 100), new NumberRange(0, 0),
                                                                                   [ new PowerRange(0, 0, CommodityQuantity.ElectricPower3PhaseSymmetric) ],
                                                                                   new NumberRange(0.001, 0.002)) ],
                                                   false);

            var actuator  = new FRBC_ActuatorDescription(Actuator_Id.Parse("actuator1"), [ Commodity.Electricity ], [ withCosts ], [], []);
            var storage   = new FRBC_StorageDescription(false, false, false, new NumberRange(0, 100));
            var system    = new FRBC_SystemDescription(new DateTimeOffset(2019, 8, 24, 14, 15, 22, TimeSpan.Zero), [ actuator ], storage);

            var outcome = await rm.SendAndAwaitReceptionStatusAsync(system);
            Assert.That(outcome.ReceptionStatus?.Status,          Is.EqualTo(ReceptionStatusValue.InvalidContent));
            Assert.That(outcome.ReceptionStatus?.DiagnosticLabel, Does.Contain("currency"));

        }

        #endregion

    }

}

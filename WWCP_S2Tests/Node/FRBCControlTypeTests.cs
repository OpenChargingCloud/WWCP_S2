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

using cloud.charging.open.protocols.S2.Node;
using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Node
{

    /// <summary>
    /// Tests of the FRBC control-type handlers (<see cref="FRBCResourceManager"/> on the RM,
    /// <see cref="FRBCEnergyManager"/> on the CEM), driven through a pair of <see cref="S2Session"/>s
    /// connected by an <see cref="InMemoryS2Medium"/> (no network). Activation is exercised the way
    /// production does it: the CEM sends a <see cref="SelectControlType"/>, which activates the CEM
    /// on send and the RM on receipt, so the real message rules of the session run.
    /// </summary>
    [TestFixture]
    public sealed class FRBCControlTypeTests
    {

        #region Data

        /// <summary>
        /// Upper bound for any single cross-session propagation. In-memory delivery is sub-millisecond,
        /// so this only guards against a genuine hang; every test finishes far below it.
        /// </summary>
        private static readonly TimeSpan  Timeout      = TimeSpan.FromSeconds(2);

        /// <summary>
        /// How long to wait before asserting that something did NOT happen (e.g. no auto-acknowledge).
        /// </summary>
        private static readonly TimeSpan  SettleDelay  = TimeSpan.FromMilliseconds(200);

        #endregion

        #region Helpers

        #region (record) FRBCPair (CEM, RM, EnergyManager, ResourceManager)

        /// <summary>
        /// A connected CEM session (with an <see cref="FRBCEnergyManager"/>) and RM session
        /// (with an <see cref="FRBCResourceManager"/>), both started. Disposing closes both sessions
        /// and, through them, the in-memory medium.
        /// </summary>
        private sealed class FRBCPair : IAsyncDisposable
        {

            public S2Session            CEM              { get; }
            public S2Session            RM               { get; }
            public FRBCEnergyManager    EnergyManager    { get; }
            public FRBCResourceManager  ResourceManager  { get; }

            public FRBCPair(S2Session            CEM,
                            S2Session            RM,
                            FRBCEnergyManager    EnergyManager,
                            FRBCResourceManager  ResourceManager)
            {
                this.CEM              = CEM;
                this.RM               = RM;
                this.EnergyManager    = EnergyManager;
                this.ResourceManager  = ResourceManager;
            }

            public async ValueTask DisposeAsync()
            {
                await CEM.DisposeAsync();
                await RM. DisposeAsync();
            }

        }

        #endregion

        #region CreatePairAsync (ResourceManager = null, EnergyManager = null)

        /// <summary>
        /// Build a started CEM/RM session pair over the in-memory medium and register the FRBC
        /// handlers (a fresh EV-charger <see cref="FRBCResourceManager"/> and <see cref="FRBCEnergyManager"/>
        /// unless supplied). FRBC is not yet activated; call <see cref="ActivateAsync"/> for that.
        /// </summary>
        private static async Task<FRBCPair> CreatePairAsync(FRBCResourceManager?  ResourceManager   = null,
                                                            FRBCEnergyManager?    EnergyManager     = null)
        {

            var (a, b) = InMemoryS2Medium.CreatePair("CEM", "RM");

            var cem = new S2Session(a, new S2SessionOptions {
                                           Role               = EnergyManagementRole.CEM,
                                           Mode               = S2SessionMode.S2Connect,
                                           NegotiatedVersion  = Version.S2JSONVersion
                                       });

            var rm  = new S2Session(b, new S2SessionOptions {
                                           Role               = EnergyManagementRole.RM,
                                           Mode               = S2SessionMode.S2Connect,
                                           NegotiatedVersion  = Version.S2JSONVersion
                                       });

            var energyManager    = EnergyManager   ?? new FRBCEnergyManager();
            var resourceManager  = ResourceManager ?? new FRBCResourceManager(EVChargerSystemDescription());

            cem.RegisterControlType(energyManager);
            rm. RegisterControlType(resourceManager);

            await cem.StartAsync();
            await rm. StartAsync();

            return new FRBCPair(cem, rm, energyManager, resourceManager);

        }

        #endregion

        #region ActivateAsync (Pair)

        /// <summary>
        /// The CEM selects FRBC. The CEM activates on send, the RM on receipt; the RM then sends its
        /// system description. Waits until FRBC is active on both sides and the description reached the CEM.
        /// </summary>
        private static async Task ActivateAsync(FRBCPair Pair)
        {

            var outcome = await Pair.CEM.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType.FillRateBasedControl));
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            await WaitUntil(() => Pair.CEM.ActiveControlType == ControlType.FillRateBasedControl &&
                                  Pair.RM. ActiveControlType == ControlType.FillRateBasedControl,
                            "FRBC was not activated on both sessions.");

            await WaitUntil(() => Pair.EnergyManager.LastSystemDescription is not null,
                            "the RM's system description did not reach the CEM.");

        }

        #endregion

        #region DeactivateAsync (Pair)

        /// <summary>
        /// The CEM selects NO_SELECTION, which the session treats as deactivation (see
        /// S2Session.ActivateControlTypeAsync and S2SessionTests.ControlTypeSwitch...): the active
        /// handler is deactivated and both sides return to WebSocketConnected.
        /// </summary>
        private static async Task DeactivateAsync(FRBCPair Pair)
        {

            var outcome = await Pair.CEM.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType.NoSelection));
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            await WaitUntil(() => Pair.CEM.ActiveControlType is null &&
                                  Pair.RM. ActiveControlType is null,
                            "FRBC was not deactivated on both sessions.");

        }

        #endregion

        #region WaitUntil (Condition, Message)

        /// <summary>
        /// Poll <paramref name="Condition"/> until it holds or <see cref="Timeout"/> elapses (then fail).
        /// </summary>
        private static async Task WaitUntil(Func<Boolean>  Condition,
                                            String         Message)
        {

            var deadline = DateTimeOffset.UtcNow + Timeout;

            while (DateTimeOffset.UtcNow < deadline)
            {
                if (Condition())
                    return;
                await Task.Delay(5);
            }

            Assert.Fail(Message);

        }

        #endregion

        #region EVChargerSystemDescription() / Instruction(...)

        /// <summary>
        /// The FRBC system description of an EV charger (copied from S2SessionTests; a fixed
        /// <c>valid_from</c> so the value survives a JSON round-trip for equality assertions).
        /// </summary>
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

        /// <summary>
        /// An FRBC instruction that charges (om2) on actuator1 - both declared by the system description.
        /// </summary>
        private static FRBC_Instruction Instruction(String Id = "instruction1")
            => new (Instruction_Id.  Parse(Id),
                    Actuator_Id.     Parse("actuator1"),
                    OperationMode_Id.Parse("om2"),
                    0.5,
                    new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero),
                    AbnormalCondition: false);

        #endregion

        #endregion


        #region ControlType_OfBothHandlers_IsFillRateBasedControl()

        [Test]
        public void ControlType_OfBothHandlers_IsFillRateBasedControl()
        {

            var resourceManager  = new FRBCResourceManager(EVChargerSystemDescription());
            var energyManager    = new FRBCEnergyManager();

            Assert.Multiple(() => {
                Assert.That(resourceManager.ControlType,  Is.EqualTo(ControlType.FillRateBasedControl));
                Assert.That(energyManager.  ControlType,  Is.EqualTo(ControlType.FillRateBasedControl));
            });

        }

        #endregion

        #region Constructor_WithNullSystemDescription_ThrowsArgumentNullException()

        [Test]
        public void Constructor_WithNullSystemDescription_ThrowsArgumentNullException()

            => Assert.Throws<ArgumentNullException>(() => { _ = new FRBCResourceManager((FRBC_SystemDescription) null!); });

        #endregion

        #region Constructor_WithNullSystemDescriptionFactory_ThrowsArgumentNullException()

        [Test]
        public void Constructor_WithNullSystemDescriptionFactory_ThrowsArgumentNullException()

            => Assert.Throws<ArgumentNullException>(() => { _ = new FRBCResourceManager((Func<FRBC_SystemDescription>) null!); });

        #endregion

        #region Activation_PutsBothSessionsIntoControlTypeActivated()

        [Test]
        public async Task Activation_PutsBothSessionsIntoControlTypeActivated()
        {

            await using var pair = await CreatePairAsync();

            await ActivateAsync(pair);

            Assert.Multiple(() => {
                Assert.That(pair.CEM.State,              Is.EqualTo(S2SessionState.ControlTypeActivated));
                Assert.That(pair.CEM.ActiveControlType,  Is.EqualTo(ControlType.FillRateBasedControl));
                Assert.That(pair.RM. State,              Is.EqualTo(S2SessionState.ControlTypeActivated));
                Assert.That(pair.RM. ActiveControlType,  Is.EqualTo(ControlType.FillRateBasedControl));
            });

        }

        #endregion

        #region Activation_SendsTheSystemDescription_WhichTheCEMCaches()

        [Test]
        public async Task Activation_SendsTheSystemDescription_WhichTheCEMCaches()
        {

            var systemDescription = EVChargerSystemDescription();

            await using var pair = await CreatePairAsync(ResourceManager: new FRBCResourceManager(systemDescription));

            var received = new TaskCompletionSource<FRBC_SystemDescription>();
            pair.EnergyManager.OnSystemDescription += (session, message, ct) => {
                                                          received.TrySetResult(message);
                                                          return Task.CompletedTask;
                                                      };

            await ActivateAsync(pair);

            var raised = await received.Task.WaitAsync(Timeout);

            // The description sent by the RM (the very instance the handler was built with) round-trips
            // through JSON and equals the cached one on the CEM.
            Assert.Multiple(() => {
                Assert.That(raised,                                    Is.EqualTo(systemDescription));
                Assert.That(pair.EnergyManager.LastSystemDescription,  Is.EqualTo(systemDescription));
            });

        }

        #endregion

        #region Instruction_IsForwardedToOnInstruction_AndAutoAcknowledged()

        [Test]
        public async Task Instruction_IsForwardedToOnInstruction_AndAutoAcknowledged()
        {

            await using var pair = await CreatePairAsync();

            var received = new TaskCompletionSource<FRBC_Instruction>();
            pair.ResourceManager.OnInstruction += (session, instruction, ct) => {
                                                       received.TrySetResult(instruction);
                                                       return Task.FromResult<ReceptionStatusValue?>(null);
                                                   };

            var updates = new List<InstructionStatusUpdate>();
            pair.EnergyManager.OnInstructionStatusUpdate += (session, update, ct) => {
                                                                lock (updates)
                                                                    updates.Add(update);
                                                                return Task.CompletedTask;
                                                            };

            await ActivateAsync(pair);

            var instruction  = Instruction("instruction1");
            var outcome      = await pair.CEM.SendAndAwaitReceptionStatusAsync(instruction);
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            var forwarded = await received.Task.WaitAsync(Timeout);
            await WaitUntil(() => { lock (updates) return updates.Count == 1; },
                            "the RM did not auto-acknowledge the instruction.");

            Assert.Multiple(() => {
                Assert.That(forwarded.Id,             Is.EqualTo(instruction.Id));
                lock (updates)
                {
                    Assert.That(updates[0].StatusType,     Is.EqualTo(InstructionStatus.New));
                    Assert.That(updates[0].InstructionId,  Is.EqualTo(instruction.Id));
                }
            });

        }

        #endregion

        #region WithoutAutoAcknowledge_NoInstructionStatusUpdateIsSent()

        [Test]
        public async Task WithoutAutoAcknowledge_NoInstructionStatusUpdateIsSent()
        {

            var resourceManager = new FRBCResourceManager(EVChargerSystemDescription()) {
                                      AutoAcknowledgeInstructions = false
                                  };

            await using var pair = await CreatePairAsync(ResourceManager: resourceManager);

            var updates = new List<InstructionStatusUpdate>();
            pair.EnergyManager.OnInstructionStatusUpdate += (session, update, ct) => {
                                                                lock (updates)
                                                                    updates.Add(update);
                                                                return Task.CompletedTask;
                                                            };

            // Accept the instruction (return null = OK) but do not auto-acknowledge it.
            pair.ResourceManager.OnInstruction += (session, instruction, ct)
                => Task.FromResult<ReceptionStatusValue?>(null);

            await ActivateAsync(pair);

            var outcome = await pair.CEM.SendAndAwaitReceptionStatusAsync(Instruction("instruction1"));
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            // Give any (unexpected) InstructionStatusUpdate ample time to arrive.
            await Task.Delay(SettleDelay);

            lock (updates)
                Assert.That(updates, Is.Empty, "no InstructionStatusUpdate must be sent when AutoAcknowledgeInstructions is false.");

        }

        #endregion

        #region RejectingInstruction_ReflectsTheStatus_AndSendsNoStatusUpdate()

        [Test]
        public async Task RejectingInstruction_ReflectsTheStatus_AndSendsNoStatusUpdate()
        {

            await using var pair = await CreatePairAsync();

            // The callback rejects the instruction; the handler answers with that status and sends no update.
            pair.ResourceManager.OnInstruction += (session, instruction, ct)
                => Task.FromResult<ReceptionStatusValue?>(ReceptionStatusValue.InvalidContent);

            var updates = new List<InstructionStatusUpdate>();
            pair.EnergyManager.OnInstructionStatusUpdate += (session, update, ct) => {
                                                                lock (updates)
                                                                    updates.Add(update);
                                                                return Task.CompletedTask;
                                                            };

            await ActivateAsync(pair);

            var outcome = await pair.CEM.SendAndAwaitReceptionStatusAsync(Instruction("instruction1"));

            Assert.Multiple(() => {
                Assert.That(outcome.IsOK,                     Is.False);
                Assert.That(outcome.ReceptionStatus?.Status,  Is.EqualTo(ReceptionStatusValue.InvalidContent));
            });

            await Task.Delay(SettleDelay);

            lock (updates)
                Assert.That(updates, Is.Empty, "a rejected instruction must not be acknowledged.");

        }

        #endregion

        #region AcceptingInstructionWithNull_YieldsAnOkOutcome()

        [Test]
        public async Task AcceptingInstructionWithNull_YieldsAnOkOutcome()
        {

            await using var pair = await CreatePairAsync();

            pair.ResourceManager.OnInstruction += (session, instruction, ct)
                => Task.FromResult<ReceptionStatusValue?>(null);

            await ActivateAsync(pair);

            var outcome = await pair.CEM.SendAndAwaitReceptionStatusAsync(Instruction("instruction1"));

            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

        }

        #endregion

        #region StorageAndActuatorStatus_ReachTheCEM_AndUpdateTheLastValues()

        [Test]
        public async Task StorageAndActuatorStatus_ReachTheCEM_AndUpdateTheLastValues()
        {

            await using var pair = await CreatePairAsync();

            var storageStatuses   = new List<FRBC_StorageStatus>();
            var actuatorStatuses  = new List<FRBC_ActuatorStatus>();
            pair.EnergyManager.OnStorageStatus  += (session, message, ct) => { lock (storageStatuses)  storageStatuses. Add(message); return Task.CompletedTask; };
            pair.EnergyManager.OnActuatorStatus += (session, message, ct) => { lock (actuatorStatuses) actuatorStatuses.Add(message); return Task.CompletedTask; };

            await ActivateAsync(pair);

            var storageOutcome  = await pair.RM.SendAndAwaitReceptionStatusAsync(new FRBC_StorageStatus(42));
            Assert.That(storageOutcome.IsOK,  Is.True, storageOutcome.ReceptionStatus?.DiagnosticLabel);

            var actuatorOutcome = await pair.RM.SendAndAwaitReceptionStatusAsync(new FRBC_ActuatorStatus(Actuator_Id.     Parse("actuator1"),
                                                                                                         OperationMode_Id.Parse("om2"),
                                                                                                         0.5));
            Assert.That(actuatorOutcome.IsOK, Is.True, actuatorOutcome.ReceptionStatus?.DiagnosticLabel);

            await WaitUntil(() => pair.EnergyManager.LastStorageStatus  is not null &&
                                  pair.EnergyManager.LastActuatorStatus is not null,
                            "the RM's storage/actuator status did not reach the CEM.");

            Assert.Multiple(() => {
                Assert.That(pair.EnergyManager.LastStorageStatus?. PresentFillLevel,  Is.EqualTo(42.0));
                Assert.That(pair.EnergyManager.LastActuatorStatus?.ActuatorId,        Is.EqualTo(Actuator_Id.Parse("actuator1")));
                Assert.That(pair.EnergyManager.LastActuatorStatus?.ActiveOperationModeId, Is.EqualTo(OperationMode_Id.Parse("om2")));
                lock (storageStatuses)  Assert.That(storageStatuses,  Has.Count.EqualTo(1));
                lock (actuatorStatuses) Assert.That(actuatorStatuses, Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region Deactivation_ClearsTheCachedLastValues()

        [Test]
        public async Task Deactivation_ClearsTheCachedLastValues()
        {

            await using var pair = await CreatePairAsync();

            await ActivateAsync(pair);

            await pair.RM.SendAndAwaitReceptionStatusAsync(new FRBC_StorageStatus(42));
            await pair.RM.SendAndAwaitReceptionStatusAsync(new FRBC_ActuatorStatus(Actuator_Id.     Parse("actuator1"),
                                                                                   OperationMode_Id.Parse("om2"),
                                                                                   0.5));

            await WaitUntil(() => pair.EnergyManager.LastSystemDescription is not null &&
                                  pair.EnergyManager.LastStorageStatus     is not null &&
                                  pair.EnergyManager.LastActuatorStatus    is not null,
                            "the CEM did not cache the FRBC state.");

            // Deactivating on the CEM runs FRBCEnergyManager.DeactivateAsync synchronously during the send.
            await DeactivateAsync(pair);

            Assert.Multiple(() => {
                Assert.That(pair.EnergyManager.LastSystemDescription,  Is.Null);
                Assert.That(pair.EnergyManager.LastStorageStatus,      Is.Null);
                Assert.That(pair.EnergyManager.LastActuatorStatus,     Is.Null);
            });

        }

        #endregion

        #region AfterDeactivation_TheEnergyManagerReceivesNoFurtherStatuses()

        [Test]
        public async Task AfterDeactivation_TheEnergyManagerReceivesNoFurtherStatuses()
        {

            await using var pair = await CreatePairAsync();

            await ActivateAsync(pair);

            await pair.RM.SendAndAwaitReceptionStatusAsync(new FRBC_StorageStatus(50));
            await WaitUntil(() => pair.EnergyManager.LastStorageStatus is not null,
                            "the first storage status did not reach the CEM.");

            await DeactivateAsync(pair);

            // The session refuses an out-of-state FRBC message LOCALLY (see S2Session.SendAsync and
            // S2SessionTests.ControlTypeSwitch...): nothing goes on the wire, so the CEM never sees it.
            var outcome = await pair.RM.SendAsync(new FRBC_StorageStatus(60));

            await Task.Delay(SettleDelay);

            Assert.Multiple(() => {
                Assert.That(outcome.Status,                        Is.EqualTo(S2SendStatus.Error));
                Assert.That(pair.EnergyManager.LastStorageStatus,  Is.Null, "the cached value was cleared on deactivation and must not reappear.");
            });

        }

        #endregion

        #region SystemDescriptionFactory_IsInvokedOnEachActivation()

        [Test]
        public async Task SystemDescriptionFactory_IsInvokedOnEachActivation()
        {

            var activationCount  = 0;
            var resourceManager  = new FRBCResourceManager(() => {
                                                               Interlocked.Increment(ref activationCount);
                                                               return EVChargerSystemDescription();
                                                           });

            await using var pair = await CreatePairAsync(ResourceManager: resourceManager);

            // 1st activation.
            await ActivateAsync(pair);
            await WaitUntil(() => Volatile.Read(ref activationCount) == 1,
                            "the factory was not invoked on the first activation.");

            // Deactivate via NO_SELECTION, then re-activate.
            await DeactivateAsync(pair);

            var outcome = await pair.CEM.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType.FillRateBasedControl));
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            await WaitUntil(() => Volatile.Read(ref activationCount) == 2,
                            "the factory was not invoked on the second activation.");

            Assert.That(activationCount, Is.EqualTo(2));

        }

        #endregion

        #region AfterReactivation_TheInstructionFlowStillWorks()

        [Test]
        public async Task AfterReactivation_TheInstructionFlowStillWorks()
        {

            await using var pair = await CreatePairAsync();

            var instructions = new List<FRBC_Instruction>();
            pair.ResourceManager.OnInstruction += (session, instruction, ct) => {
                                                       lock (instructions)
                                                           instructions.Add(instruction);
                                                       return Task.FromResult<ReceptionStatusValue?>(null);
                                                   };

            var updates = new List<InstructionStatusUpdate>();
            pair.EnergyManager.OnInstructionStatusUpdate += (session, update, ct) => {
                                                                lock (updates)
                                                                    updates.Add(update);
                                                                return Task.CompletedTask;
                                                            };

            // A full activate -> deactivate cycle disposes the CEM's and RM's handler registrations.
            await ActivateAsync(pair);
            await DeactivateAsync(pair);

            // Re-activation must re-register both sides cleanly.
            var outcome = await pair.CEM.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType.FillRateBasedControl));
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);
            await WaitUntil(() => pair.EnergyManager.LastSystemDescription is not null,
                            "the system description was not re-cached after reactivation.");

            var instruction         = Instruction("instruction2");
            var instructionOutcome  = await pair.CEM.SendAndAwaitReceptionStatusAsync(instruction);
            Assert.That(instructionOutcome.IsOK, Is.True, instructionOutcome.ReceptionStatus?.DiagnosticLabel);

            await WaitUntil(() => { lock (instructions) return instructions.Count == 1; },
                            "OnInstruction did not fire after reactivation.");
            await WaitUntil(() => { lock (updates)      return updates.Count == 1;      },
                            "no InstructionStatusUpdate after reactivation.");

            Assert.Multiple(() => {
                lock (instructions) Assert.That(instructions[0].Id,     Is.EqualTo(instruction.Id));
                lock (updates)      Assert.That(updates[0].StatusType,  Is.EqualTo(InstructionStatus.New));
            });

        }

        #endregion

    }

}

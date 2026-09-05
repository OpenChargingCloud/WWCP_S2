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
    /// Tests of the OMBC control-type handlers (<see cref="OMBCResourceManager"/> on the RM,
    /// <see cref="OMBCEnergyManager"/> on the CEM) over a real session pair, with the heat pump of
    /// the OMBC message tests as the device: two operation modes, two transitions and a minimum
    /// run time timer.
    /// </summary>
    [TestFixture]
    public sealed class OMBCControlTypeTests
    {

        #region Data

        private static readonly OperationMode_Id  offId          = OperationMode_Id.Parse("3c7f9a1e-2b4d-4c6e-8f0a-000000000010");
        private static readonly OperationMode_Id  onId           = OperationMode_Id.Parse("3c7f9a1e-2b4d-4c6e-8f0a-000000000011");
        private static readonly Transition_Id     offToOnId      = Transition_Id.   Parse("3c7f9a1e-2b4d-4c6e-8f0a-000000000020");
        private static readonly Transition_Id     onToOffId      = Transition_Id.   Parse("3c7f9a1e-2b4d-4c6e-8f0a-000000000021");
        private static readonly Timer_Id          minRunTimeId   = Timer_Id.        Parse("3c7f9a1e-2b4d-4c6e-8f0a-000000000030");

        private static readonly DateTimeOffset    validFrom      = new (2024, 5, 1, 12, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset    executionTime  = new (2024, 5, 1, 12, 5, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset    finishedAt     = new (2024, 5, 1, 12, 8, 0, TimeSpan.Zero);

        #endregion

        #region Helpers

        /// <summary>
        /// The heat pump of the OMBC message tests: off / on with a minimum run time.
        ///
        /// Without the running and transition costs those tests use: a session rejects a system
        /// description that publishes costs while the ResourceManagerDetails declare no currency
        /// (INVALID_CONTENT, "currency mandatory when costs are published"), and these tests drive
        /// the handlers rather than that rule - S2SessionTests covers it.
        /// </summary>
        private static OMBC_SystemDescription HeatPump()

            => new (validFrom,
                    [
                        new OMBC_OperationMode(offId, [ new PowerRange(0, 0, CommodityQuantity.ElectricPowerL1) ], false, "Off"),
                        new OMBC_OperationMode(onId,
                                               [
                                                   new PowerRange( 800, 2400, CommodityQuantity.ElectricPowerL1),
                                                   new PowerRange(2500, 9000, CommodityQuantity.HeatThermalPower)
                                               ],
                                               false,
                                               "On")
                    ],
                    [
                        new Transition(offToOnId, offId, onId, [ minRunTimeId ], [], false, null, Duration.FromMilliseconds(3000)),
                        new Transition(onToOffId, onId, offId, [], [ minRunTimeId ], false)
                    ],
                    [ new Timer(minRunTimeId, Duration.FromMilliseconds(600000), "Minimum run time") ]);

        private static OMBC_Instruction Instruction(String Id = "ombc-instruction-1")
            => new (Instruction_Id.Parse(Id),
                    executionTime,
                    onId,
                    0.75,
                    AbnormalCondition: false);

        #endregion


        #region ControlType_OfBothHandlers_IsOperationModeBasedControl()

        [Test]
        public void ControlType_OfBothHandlers_IsOperationModeBasedControl()
        {

            Assert.Multiple(() => {
                Assert.That(new OMBCResourceManager(HeatPump()).ControlType,  Is.EqualTo(ControlType.OperationModeBasedControl));
                Assert.That(new OMBCEnergyManager().             ControlType,  Is.EqualTo(ControlType.OperationModeBasedControl));
            });

        }

        #endregion

        #region Constructor_WithNull_ThrowsArgumentNullException()

        [Test]
        public void Constructor_WithNull_ThrowsArgumentNullException()

            => Assert.Multiple(() => {
                   Assert.Throws<ArgumentNullException>(() => { _ = new OMBCResourceManager((OMBC_SystemDescription) null!); });
                   Assert.Throws<ArgumentNullException>(() => { _ = new OMBCResourceManager((Func<OMBC_SystemDescription>) null!); });
               });

        #endregion

        #region Activation_SendsTheSystemDescription_WhichTheCEMCaches()

        [Test]
        public async Task Activation_SendsTheSystemDescription_WhichTheCEMCaches()
        {

            var systemDescription  = HeatPump();
            var energyManager      = new OMBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager,
                                                                            new OMBCResourceManager(systemDescription));

            var received = new TaskCompletionSource<OMBC_SystemDescription>();
            energyManager.OnSystemDescription += (session, message, ct) => {
                                                     received.TrySetResult(message);
                                                     return Task.CompletedTask;
                                                 };

            await pair.ActivateAsync(ControlType.OperationModeBasedControl);

            var raised = await received.Task.WaitAsync(ControlTypeSessionPair.Timeout);

            // The description the RM sent round-trips through JSON and equals the cached one.
            Assert.Multiple(() => {
                Assert.That(raised,                               Is.EqualTo(systemDescription));
                Assert.That(energyManager.LastSystemDescription,  Is.EqualTo(systemDescription));
            });

        }

        #endregion

        #region Instruction_IsForwardedToOnInstruction_AndAutoAcknowledged()

        [Test]
        public async Task Instruction_IsForwardedToOnInstruction_AndAutoAcknowledged()
        {

            var resourceManager  = new OMBCResourceManager(HeatPump());
            var energyManager    = new OMBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager, resourceManager);

            var received = new TaskCompletionSource<OMBC_Instruction>();
            resourceManager.OnInstruction += (session, instruction, ct) => {
                                                 received.TrySetResult(instruction);
                                                 return Task.FromResult<ReceptionStatusValue?>(null);
                                             };

            var updates = new List<InstructionStatusUpdate>();
            energyManager.OnInstructionStatusUpdate += (session, update, ct) => {
                                                           lock (updates)
                                                               updates.Add(update);
                                                           return Task.CompletedTask;
                                                       };

            await pair.ActivateAsync(ControlType.OperationModeBasedControl);

            var instruction  = Instruction();
            var outcome      = await pair.CEM.SendAndAwaitReceptionStatusAsync(instruction);
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            var forwarded = await received.Task.WaitAsync(ControlTypeSessionPair.Timeout);

            await ControlTypeSessionPair.WaitUntil(() => { lock (updates) return updates.Count == 1; },
                                                   "the RM did not auto-acknowledge the instruction.");

            Assert.Multiple(() => {
                Assert.That(forwarded.Id,                 Is.EqualTo(instruction.Id));
                Assert.That(forwarded.OperationModeId,    Is.EqualTo(onId));
                lock (updates)
                {
                    Assert.That(updates[0].StatusType,     Is.EqualTo(InstructionStatus.New));
                    Assert.That(updates[0].InstructionId,  Is.EqualTo(instruction.Id));
                }
            });

        }

        #endregion

        #region RejectedInstruction_IsNotAcknowledged()

        [Test]
        public async Task RejectedInstruction_IsNotAcknowledged()
        {

            var resourceManager  = new OMBCResourceManager(HeatPump());
            var energyManager    = new OMBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager, resourceManager);

            resourceManager.OnInstruction += (session, instruction, ct)
                => Task.FromResult<ReceptionStatusValue?>(ReceptionStatusValue.InvalidContent);

            var updates = new List<InstructionStatusUpdate>();
            energyManager.OnInstructionStatusUpdate += (session, update, ct) => {
                                                           lock (updates)
                                                               updates.Add(update);
                                                           return Task.CompletedTask;
                                                       };

            await pair.ActivateAsync(ControlType.OperationModeBasedControl);

            var outcome = await pair.CEM.SendAndAwaitReceptionStatusAsync(Instruction());

            Assert.That(outcome.ReceptionStatus?.Status, Is.EqualTo(ReceptionStatusValue.InvalidContent));

            await Task.Delay(ControlTypeSessionPair.SettleDelay);

            lock (updates)
                Assert.That(updates, Is.Empty, "a rejected instruction must not be acknowledged.");

        }

        #endregion

        #region WithoutAutoAcknowledge_NoInstructionStatusUpdateIsSent()

        [Test]
        public async Task WithoutAutoAcknowledge_NoInstructionStatusUpdateIsSent()
        {

            var resourceManager  = new OMBCResourceManager(HeatPump()) { AutoAcknowledgeInstructions = false };
            var energyManager    = new OMBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager, resourceManager);

            var updates = new List<InstructionStatusUpdate>();
            energyManager.OnInstructionStatusUpdate += (session, update, ct) => {
                                                           lock (updates)
                                                               updates.Add(update);
                                                           return Task.CompletedTask;
                                                       };

            await pair.ActivateAsync(ControlType.OperationModeBasedControl);

            var outcome = await pair.CEM.SendAndAwaitReceptionStatusAsync(Instruction());
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            await Task.Delay(ControlTypeSessionPair.SettleDelay);

            lock (updates)
                Assert.That(updates, Is.Empty, "no InstructionStatusUpdate must be sent when AutoAcknowledgeInstructions is false.");

        }

        #endregion

        #region StatusAndTimerStatus_ReachTheCEM()

        [Test]
        public async Task StatusAndTimerStatus_ReachTheCEM()
        {

            var energyManager = new OMBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager,
                                                                           new OMBCResourceManager(HeatPump()));

            await pair.ActivateAsync(ControlType.OperationModeBasedControl);

            var status       = new OMBC_Status(onId, 0.75, offId, validFrom);
            var timerStatus  = new OMBC_TimerStatus(minRunTimeId, finishedAt);

            Assert.That((await pair.RM.SendAndAwaitReceptionStatusAsync(status)).     IsOK, Is.True);
            Assert.That((await pair.RM.SendAndAwaitReceptionStatusAsync(timerStatus)). IsOK, Is.True);

            await ControlTypeSessionPair.WaitUntil(() => energyManager.LastStatus      is not null &&
                                                         energyManager.LastTimerStatus is not null,
                                                   "the RM's status messages did not reach the CEM.");

            Assert.Multiple(() => {
                Assert.That(energyManager.LastStatus,       Is.EqualTo(status));
                Assert.That(energyManager.LastTimerStatus,  Is.EqualTo(timerStatus));
            });

        }

        #endregion

        #region Deactivation_ClearsTheCachesOfTheEnergyManager()

        [Test]
        public async Task Deactivation_ClearsTheCachesOfTheEnergyManager()
        {

            var energyManager = new OMBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager,
                                                                           new OMBCResourceManager(HeatPump()));

            await pair.ActivateAsync(ControlType.OperationModeBasedControl);

            await ControlTypeSessionPair.WaitUntil(() => energyManager.LastSystemDescription is not null,
                                                   "the RM's system description did not reach the CEM.");

            await pair.DeactivateAsync();

            Assert.Multiple(() => {
                Assert.That(energyManager.LastSystemDescription,  Is.Null);
                Assert.That(energyManager.LastStatus,             Is.Null);
                Assert.That(energyManager.LastTimerStatus,        Is.Null);
            });

        }

        #endregion

    }

}

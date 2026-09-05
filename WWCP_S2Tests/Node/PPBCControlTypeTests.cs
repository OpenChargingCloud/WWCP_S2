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
    /// Tests of the PPBC control-type handlers (<see cref="PPBCResourceManager"/> on the RM,
    /// <see cref="PPBCEnergyManager"/> on the CEM) over a real session pair, with the two power
    /// profile alternatives of the PPBC message tests. PPBC is the control type with three
    /// instructions - schedule, start interruption, end interruption - and each of them takes the
    /// same path through the handler.
    /// </summary>
    [TestFixture]
    public sealed class PPBCControlTypeTests
    {

        #region Data

        private static readonly Instruction_Id             instructionId  = Instruction_Id.           Parse("5a2b3c4d-0000-4000-8000-000000000001");
        private static readonly PowerSequence_Id           sequenceId1    = PowerSequence_Id.         Parse("2a2b3c4d-0000-4000-8000-000000000001");
        private static readonly PowerSequence_Id           sequenceId2    = PowerSequence_Id.         Parse("2a2b3c4d-0000-4000-8000-000000000002");
        private static readonly PowerSequenceContainer_Id  containerId1   = PowerSequenceContainer_Id.Parse("3a2b3c4d-0000-4000-8000-000000000001");
        private static readonly PowerSequenceContainer_Id  containerId2   = PowerSequenceContainer_Id.Parse("3a2b3c4d-0000-4000-8000-000000000002");
        private static readonly PowerProfileDefinition_Id  profileId      = PowerProfileDefinition_Id.Parse("4a2b3c4d-0000-4000-8000-000000000001");

        private static readonly DateTimeOffset             startTime      = new (2024, 1, 1,  8, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset             endTime        = new (2024, 1, 1, 18, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset             executionTime  = new (2024, 1, 1,  9, 0, 0, TimeSpan.Zero);

        #endregion

        #region Helpers

        private static PowerForecastValue Power3Phase(Double ValueExpected)
            => new (ValueExpected, CommodityQuantity.ElectricPower3PhaseSymmetric);

        private static PPBC_PowerSequenceElement RampUp()
            => new (Duration.FromMilliseconds(3000),    [ Power3Phase(1400) ]);

        private static PPBC_PowerSequenceElement FullPower()
            => new (Duration.FromMilliseconds(3600000), [ Power3Phase(11000) ]);

        private static PPBC_PowerSequenceElement ReducedPower()
            => new (Duration.FromMilliseconds(1800000), [ Power3Phase(5500) ]);

        /// <summary>
        /// An EV charger offering two alternatives: charge now (full or reduced), or an hour later.
        /// </summary>
        private static PPBC_PowerProfileDefinition PowerProfileDefinition()
            => new (profileId,
                    startTime,
                    endTime,
                    [
                        new PPBC_PowerSequenceContainer(containerId1,
                                                        [
                                                            new PPBC_PowerSequence(sequenceId1, [ RampUp(), FullPower() ],    true,  false, Duration.FromMilliseconds(600000)),
                                                            new PPBC_PowerSequence(sequenceId2, [ RampUp(), ReducedPower() ], false, false)
                                                        ]),
                        new PPBC_PowerSequenceContainer(containerId2,
                                                        [
                                                            new PPBC_PowerSequence(sequenceId2, [ RampUp(), ReducedPower() ], false, false)
                                                        ])
                    ]);

        private static PPBC_ScheduleInstruction           ScheduleInstruction()
            => new (instructionId, profileId, containerId1, sequenceId1, executionTime, false);

        private static PPBC_StartInterruptionInstruction  StartInterruptionInstruction()
            => new (Instruction_Id.Parse("5a2b3c4d-0000-4000-8000-000000000002"), profileId, containerId1, sequenceId1, executionTime, false);

        private static PPBC_EndInterruptionInstruction    EndInterruptionInstruction()
            => new (Instruction_Id.Parse("5a2b3c4d-0000-4000-8000-000000000003"), profileId, containerId1, sequenceId1, executionTime, false);

        private static PPBC_PowerProfileStatus            PowerProfileStatus()
            => new ([
                        new PPBC_PowerSequenceContainerStatus(profileId, containerId1, PPBC_PowerSequenceStatus.Executing, sequenceId1, Duration.FromMilliseconds(120000)),
                        new PPBC_PowerSequenceContainerStatus(profileId, containerId2, PPBC_PowerSequenceStatus.NotScheduled)
                    ]);

        #endregion


        #region ControlType_OfBothHandlers_IsPowerProfileBasedControl()

        [Test]
        public void ControlType_OfBothHandlers_IsPowerProfileBasedControl()

            => Assert.Multiple(() => {
                   Assert.That(new PPBCResourceManager(PowerProfileDefinition()).ControlType,  Is.EqualTo(ControlType.PowerProfileBasedControl));
                   Assert.That(new PPBCEnergyManager().                          ControlType,  Is.EqualTo(ControlType.PowerProfileBasedControl));
               });

        #endregion

        #region Constructor_WithNull_ThrowsArgumentNullException()

        [Test]
        public void Constructor_WithNull_ThrowsArgumentNullException()

            => Assert.Multiple(() => {
                   Assert.Throws<ArgumentNullException>(() => { _ = new PPBCResourceManager((PPBC_PowerProfileDefinition) null!); });
                   Assert.Throws<ArgumentNullException>(() => { _ = new PPBCResourceManager((Func<PPBC_PowerProfileDefinition>) null!); });
               });

        #endregion

        #region Activation_SendsThePowerProfileDefinition_WhichTheCEMCaches()

        [Test]
        public async Task Activation_SendsThePowerProfileDefinition_WhichTheCEMCaches()
        {

            var definition     = PowerProfileDefinition();
            var energyManager  = new PPBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager,
                                                                            new PPBCResourceManager(definition));

            var received = new TaskCompletionSource<PPBC_PowerProfileDefinition>();
            energyManager.OnPowerProfileDefinition += (session, message, ct) => {
                                                          received.TrySetResult(message);
                                                          return Task.CompletedTask;
                                                      };

            await pair.ActivateAsync(ControlType.PowerProfileBasedControl);

            var raised = await received.Task.WaitAsync(ControlTypeSessionPair.Timeout);

            Assert.Multiple(() => {
                Assert.That(raised,                                    Is.EqualTo(definition));
                Assert.That(energyManager.LastPowerProfileDefinition,   Is.EqualTo(definition));
            });

        }

        #endregion

        #region AllThreeInstructions_AreForwarded_AndAutoAcknowledged()

        [Test]
        public async Task AllThreeInstructions_AreForwarded_AndAutoAcknowledged()
        {

            var resourceManager  = new PPBCResourceManager(PowerProfileDefinition());
            var energyManager    = new PPBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager, resourceManager);

            var schedule  = new TaskCompletionSource<PPBC_ScheduleInstruction>();
            var start     = new TaskCompletionSource<PPBC_StartInterruptionInstruction>();
            var end       = new TaskCompletionSource<PPBC_EndInterruptionInstruction>();

            resourceManager.OnScheduleInstruction          += (session, instruction, ct) => { schedule.TrySetResult(instruction); return Task.FromResult<ReceptionStatusValue?>(null); };
            resourceManager.OnStartInterruptionInstruction += (session, instruction, ct) => { start.   TrySetResult(instruction); return Task.FromResult<ReceptionStatusValue?>(null); };
            resourceManager.OnEndInterruptionInstruction   += (session, instruction, ct) => { end.     TrySetResult(instruction); return Task.FromResult<ReceptionStatusValue?>(null); };

            var updates = new List<InstructionStatusUpdate>();
            energyManager.OnInstructionStatusUpdate += (session, update, ct) => {
                                                           lock (updates)
                                                               updates.Add(update);
                                                           return Task.CompletedTask;
                                                       };

            await pair.ActivateAsync(ControlType.PowerProfileBasedControl);

            var scheduleInstruction  = ScheduleInstruction();
            var startInstruction     = StartInterruptionInstruction();
            var endInstruction       = EndInterruptionInstruction();

            foreach (var instruction in new IS2MessageWithId[] { scheduleInstruction, startInstruction, endInstruction })
            {
                var outcome = await pair.CEM.SendAndAwaitReceptionStatusAsync(instruction);
                Assert.That(outcome.IsOK, Is.True, $"{instruction.MessageType}: {outcome.ReceptionStatus?.DiagnosticLabel}");
            }

            var forwardedSchedule  = await schedule.Task.WaitAsync(ControlTypeSessionPair.Timeout);
            var forwardedStart     = await start.   Task.WaitAsync(ControlTypeSessionPair.Timeout);
            var forwardedEnd       = await end.     Task.WaitAsync(ControlTypeSessionPair.Timeout);

            await ControlTypeSessionPair.WaitUntil(() => { lock (updates) return updates.Count == 3; },
                                                   "the RM did not auto-acknowledge all three instructions.");

            Assert.Multiple(() => {

                Assert.That(forwardedSchedule.Id,  Is.EqualTo(scheduleInstruction.Id));
                Assert.That(forwardedStart.   Id,  Is.EqualTo(startInstruction.   Id));
                Assert.That(forwardedEnd.     Id,  Is.EqualTo(endInstruction.     Id));

                lock (updates)
                {
                    Assert.That(updates.Select(update => update.InstructionId),
                                Is.EquivalentTo(new[] { scheduleInstruction.Id, startInstruction.Id, endInstruction.Id }));
                    Assert.That(updates.Select(update => update.StatusType), Is.All.EqualTo(InstructionStatus.New));
                }

            });

        }

        #endregion

        #region RejectedScheduleInstruction_IsNotAcknowledged()

        [Test]
        public async Task RejectedScheduleInstruction_IsNotAcknowledged()
        {

            var resourceManager  = new PPBCResourceManager(PowerProfileDefinition());
            var energyManager    = new PPBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager, resourceManager);

            resourceManager.OnScheduleInstruction += (session, instruction, ct)
                => Task.FromResult<ReceptionStatusValue?>(ReceptionStatusValue.InvalidContent);

            var updates = new List<InstructionStatusUpdate>();
            energyManager.OnInstructionStatusUpdate += (session, update, ct) => {
                                                           lock (updates)
                                                               updates.Add(update);
                                                           return Task.CompletedTask;
                                                       };

            await pair.ActivateAsync(ControlType.PowerProfileBasedControl);

            var outcome = await pair.CEM.SendAndAwaitReceptionStatusAsync(ScheduleInstruction());
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

            var resourceManager  = new PPBCResourceManager(PowerProfileDefinition()) { AutoAcknowledgeInstructions = false };
            var energyManager    = new PPBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager, resourceManager);

            var updates = new List<InstructionStatusUpdate>();
            energyManager.OnInstructionStatusUpdate += (session, update, ct) => {
                                                           lock (updates)
                                                               updates.Add(update);
                                                           return Task.CompletedTask;
                                                       };

            await pair.ActivateAsync(ControlType.PowerProfileBasedControl);

            Assert.That((await pair.CEM.SendAndAwaitReceptionStatusAsync(ScheduleInstruction())).IsOK, Is.True);

            await Task.Delay(ControlTypeSessionPair.SettleDelay);

            lock (updates)
                Assert.That(updates, Is.Empty, "no InstructionStatusUpdate must be sent when AutoAcknowledgeInstructions is false.");

        }

        #endregion

        #region PowerProfileStatus_ReachesTheCEM()

        [Test]
        public async Task PowerProfileStatus_ReachesTheCEM()
        {

            var energyManager = new PPBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager,
                                                                            new PPBCResourceManager(PowerProfileDefinition()));

            await pair.ActivateAsync(ControlType.PowerProfileBasedControl);

            var status = PowerProfileStatus();

            Assert.That((await pair.RM.SendAndAwaitReceptionStatusAsync(status)).IsOK, Is.True);

            await ControlTypeSessionPair.WaitUntil(() => energyManager.LastPowerProfileStatus is not null,
                                                   "the power profile status did not reach the CEM.");

            Assert.That(energyManager.LastPowerProfileStatus, Is.EqualTo(status));

        }

        #endregion

        #region Deactivation_ClearsTheCachesOfTheEnergyManager()

        [Test]
        public async Task Deactivation_ClearsTheCachesOfTheEnergyManager()
        {

            var energyManager = new PPBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager,
                                                                            new PPBCResourceManager(PowerProfileDefinition()));

            await pair.ActivateAsync(ControlType.PowerProfileBasedControl);

            Assert.That((await pair.RM.SendAndAwaitReceptionStatusAsync(PowerProfileStatus())).IsOK, Is.True);

            await ControlTypeSessionPair.WaitUntil(() => energyManager.LastPowerProfileDefinition is not null &&
                                                         energyManager.LastPowerProfileStatus     is not null,
                                                   "the RM's messages did not reach the CEM.");

            await pair.DeactivateAsync();

            Assert.Multiple(() => {
                Assert.That(energyManager.LastPowerProfileDefinition,  Is.Null);
                Assert.That(energyManager.LastPowerProfileStatus,      Is.Null);
            });

        }

        #endregion

    }

}

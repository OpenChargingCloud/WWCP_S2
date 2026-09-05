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
    /// Tests of the DDBC control-type handlers (<see cref="DDBCResourceManager"/> on the RM,
    /// <see cref="DDBCEnergyManager"/> on the CEM) over a real session pair, with the EV charger of
    /// the DDBC message tests as the device.
    /// </summary>
    [TestFixture]
    public sealed class DDBCControlTypeTests
    {

        #region Data

        private static readonly Actuator_Id       actuatorId           = Actuator_Id.     Parse("0d5f0b5e-3a1c-4d7e-9a2b-1c3d5e7f9a0b");
        private static readonly OperationMode_Id  idleModeId           = OperationMode_Id.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
        private static readonly OperationMode_Id  chargeModeId         = OperationMode_Id.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
        private static readonly Transition_Id     idleToChargeId       = Transition_Id.   Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
        private static readonly Transition_Id     chargeToIdleId       = Transition_Id.   Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8");
        private static readonly Timer_Id          minimumOnTimerId     = Timer_Id.        Parse("9e107d9d-372b-4c9a-9b7e-0a1b2c3d4e5f");

        private static readonly DateTimeOffset    validFrom            = new (2024, 5, 1, 10,  0,  0, TimeSpan.Zero);
        private static readonly DateTimeOffset    transitionTimestamp  = new (2024, 5, 1, 12, 30, 15, TimeSpan.Zero);
        private static readonly DateTimeOffset    executionTime        = new (2024, 5, 1, 11,  0,  0, TimeSpan.Zero);
        private static readonly DateTimeOffset    finishedAt           = new (2024, 5, 1, 11,  2,  0, TimeSpan.Zero);

        #endregion

        #region Helpers

        private static PowerRange ElectricPowerRange(Double StartOfRange = 1400, Double EndOfRange = 11000)
            => new (StartOfRange, EndOfRange, CommodityQuantity.ElectricPower3PhaseSymmetric);

        /// <summary>
        /// The EV charger of the DDBC message tests, without their running and transition costs:
        /// a session rejects a system description that publishes costs while the
        /// ResourceManagerDetails declare no currency, and these tests drive the handlers rather
        /// than that rule.
        /// </summary>
        private static DDBC_SystemDescription SystemDescription()

            => new (validFrom,
                    [
                        new DDBC_ActuatorDescription(
                            actuatorId,
                            [ Commodity.Electricity ],
                            [
                                new DDBC_OperationMode(idleModeId,   [ ElectricPowerRange(0, 0) ], new NumberRange(0, 0),             false, "Idle"),
                                new DDBC_OperationMode(chargeModeId, [ ElectricPowerRange() ],     new NumberRange(0.00065, 0.0051),  false, "Charging")
                            ],
                            [
                                new Transition(idleToChargeId, idleModeId, chargeModeId, [ minimumOnTimerId ], [], false, null, Duration.FromMilliseconds(3000)),
                                new Transition(chargeToIdleId, chargeModeId, idleModeId, [], [ minimumOnTimerId ], false)
                            ],
                            [ new Timer(minimumOnTimerId, Duration.FromMilliseconds(120000), "Minimum on time") ],
                            "EV charger")
                    ],
                    ProvidesAverageDemandRateForecast: true);

        private static DDBC_Instruction Instruction(String Id = "ddbc-instruction-1")
            => new (Instruction_Id.Parse(Id),
                    executionTime,
                    false,
                    actuatorId,
                    chargeModeId,
                    0.5);

        #endregion


        #region ControlType_OfBothHandlers_IsDemandDrivenBasedControl()

        [Test]
        public void ControlType_OfBothHandlers_IsDemandDrivenBasedControl()

            => Assert.Multiple(() => {
                   Assert.That(new DDBCResourceManager(SystemDescription()).ControlType,  Is.EqualTo(ControlType.DemandDrivenBasedControl));
                   Assert.That(new DDBCEnergyManager().                    ControlType,  Is.EqualTo(ControlType.DemandDrivenBasedControl));
               });

        #endregion

        #region Constructor_WithNull_ThrowsArgumentNullException()

        [Test]
        public void Constructor_WithNull_ThrowsArgumentNullException()

            => Assert.Multiple(() => {
                   Assert.Throws<ArgumentNullException>(() => { _ = new DDBCResourceManager((DDBC_SystemDescription) null!); });
                   Assert.Throws<ArgumentNullException>(() => { _ = new DDBCResourceManager((Func<DDBC_SystemDescription>) null!); });
               });

        #endregion

        #region Activation_SendsTheSystemDescription_WhichTheCEMCaches()

        [Test]
        public async Task Activation_SendsTheSystemDescription_WhichTheCEMCaches()
        {

            var systemDescription  = SystemDescription();
            var energyManager      = new DDBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager,
                                                                            new DDBCResourceManager(systemDescription));

            var received = new TaskCompletionSource<DDBC_SystemDescription>();
            energyManager.OnSystemDescription += (session, message, ct) => {
                                                     received.TrySetResult(message);
                                                     return Task.CompletedTask;
                                                 };

            await pair.ActivateAsync(ControlType.DemandDrivenBasedControl);

            var raised = await received.Task.WaitAsync(ControlTypeSessionPair.Timeout);

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

            var resourceManager  = new DDBCResourceManager(SystemDescription());
            var energyManager    = new DDBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager, resourceManager);

            var received = new TaskCompletionSource<DDBC_Instruction>();
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

            await pair.ActivateAsync(ControlType.DemandDrivenBasedControl);

            var instruction  = Instruction();
            var outcome      = await pair.CEM.SendAndAwaitReceptionStatusAsync(instruction);
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            var forwarded = await received.Task.WaitAsync(ControlTypeSessionPair.Timeout);

            await ControlTypeSessionPair.WaitUntil(() => { lock (updates) return updates.Count == 1; },
                                                   "the RM did not auto-acknowledge the instruction.");

            Assert.Multiple(() => {
                Assert.That(forwarded.Id,               Is.EqualTo(instruction.Id));
                Assert.That(forwarded.ActuatorId,       Is.EqualTo(actuatorId));
                Assert.That(forwarded.OperationModeId,  Is.EqualTo(chargeModeId));
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

            var resourceManager  = new DDBCResourceManager(SystemDescription());
            var energyManager    = new DDBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager, resourceManager);

            resourceManager.OnInstruction += (session, instruction, ct)
                => Task.FromResult<ReceptionStatusValue?>(ReceptionStatusValue.InvalidContent);

            var updates = new List<InstructionStatusUpdate>();
            energyManager.OnInstructionStatusUpdate += (session, update, ct) => {
                                                           lock (updates)
                                                               updates.Add(update);
                                                           return Task.CompletedTask;
                                                       };

            await pair.ActivateAsync(ControlType.DemandDrivenBasedControl);

            var outcome = await pair.CEM.SendAndAwaitReceptionStatusAsync(Instruction());
            Assert.That(outcome.ReceptionStatus?.Status, Is.EqualTo(ReceptionStatusValue.InvalidContent));

            await Task.Delay(ControlTypeSessionPair.SettleDelay);

            lock (updates)
                Assert.That(updates, Is.Empty, "a rejected instruction must not be acknowledged.");

        }

        #endregion

        #region TheFourStatusMessages_ReachTheCEM()

        [Test]
        public async Task TheFourStatusMessages_ReachTheCEM()
        {

            var energyManager = new DDBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager,
                                                                            new DDBCResourceManager(SystemDescription()));

            await pair.ActivateAsync(ControlType.DemandDrivenBasedControl);

            var actuatorStatus  = new DDBC_ActuatorStatus(actuatorId, chargeModeId, 0.75, idleModeId, transitionTimestamp);
            var demandStatus    = new DDBC_PresentDemandStatus(new NumberRange(0.002, 0.003));
            var forecast        = new DDBC_AverageDemandRateForecast(validFrom,
                                                                     [ new DDBC_AverageDemandRateForecastElement(Duration.FromMilliseconds(900000), 0.002) ]);
            var timerStatus     = new DDBC_TimerStatus(minimumOnTimerId, actuatorId, finishedAt);

            foreach (var message in new IS2MessageWithId[] { actuatorStatus, demandStatus, forecast, timerStatus })
            {
                var outcome = await pair.RM.SendAndAwaitReceptionStatusAsync(message);
                Assert.That(outcome.IsOK, Is.True, $"{message.MessageType}: {outcome.ReceptionStatus?.DiagnosticLabel}");
            }

            await ControlTypeSessionPair.WaitUntil(() => energyManager.LastActuatorStatus            is not null &&
                                                         energyManager.LastPresentDemandStatus       is not null &&
                                                         energyManager.LastAverageDemandRateForecast is not null &&
                                                         energyManager.LastTimerStatus               is not null,
                                                   "the RM's status messages did not reach the CEM.");

            Assert.Multiple(() => {
                Assert.That(energyManager.LastActuatorStatus,             Is.EqualTo(actuatorStatus));
                Assert.That(energyManager.LastPresentDemandStatus,        Is.EqualTo(demandStatus));
                Assert.That(energyManager.LastAverageDemandRateForecast,  Is.EqualTo(forecast));
                Assert.That(energyManager.LastTimerStatus,                Is.EqualTo(timerStatus));
            });

        }

        #endregion

        #region Deactivation_ClearsTheCachesOfTheEnergyManager()

        [Test]
        public async Task Deactivation_ClearsTheCachesOfTheEnergyManager()
        {

            var energyManager = new DDBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager,
                                                                            new DDBCResourceManager(SystemDescription()));

            await pair.ActivateAsync(ControlType.DemandDrivenBasedControl);

            await ControlTypeSessionPair.WaitUntil(() => energyManager.LastSystemDescription is not null,
                                                   "the RM's system description did not reach the CEM.");

            Assert.That((await pair.RM.SendAndAwaitReceptionStatusAsync(new DDBC_PresentDemandStatus(new NumberRange(0.002, 0.003)))).IsOK, Is.True);

            await ControlTypeSessionPair.WaitUntil(() => energyManager.LastPresentDemandStatus is not null,
                                                   "the present demand status did not reach the CEM.");

            await pair.DeactivateAsync();

            Assert.Multiple(() => {
                Assert.That(energyManager.LastSystemDescription,          Is.Null);
                Assert.That(energyManager.LastActuatorStatus,             Is.Null);
                Assert.That(energyManager.LastPresentDemandStatus,        Is.Null);
                Assert.That(energyManager.LastAverageDemandRateForecast,  Is.Null);
                Assert.That(energyManager.LastTimerStatus,                Is.Null);
            });

        }

        #endregion

    }

}

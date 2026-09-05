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
    /// Tests of the PEBC control-type handlers (<see cref="PEBCResourceManager"/> on the RM,
    /// <see cref="PEBCEnergyManager"/> on the CEM) over a real session pair, with the PV example of
    /// the PEBC message tests as the device: a 4 kWp installation on phase L1 whose feed-in
    /// (negative by the S2 sign convention) the CEM may curtail.
    /// </summary>
    [TestFixture]
    public sealed class PEBCControlTypeTests
    {

        #region Data

        private static readonly DateTimeOffset  validFrom      = new (2024, 8, 24, 14, 15, 22, TimeSpan.Zero);
        private static readonly DateTimeOffset  validUntil     = new (2024, 8, 25, 14, 15, 22, TimeSpan.Zero);
        private static readonly DateTimeOffset  executionTime  = new (2024, 8, 24, 15,  0,  0, TimeSpan.Zero);
        private static readonly Duration        oneHour        = Duration.FromMilliseconds(3600000);

        #endregion

        #region Helpers

        private static PEBC_AllowedLimitRange LowerRange()
            => new (CommodityQuantity.ElectricPowerL1,
                    PEBC_PowerEnvelopeLimitType.LowerLimit,
                    new NumberRange(-4000, 0),
                    false);

        private static PEBC_AllowedLimitRange UpperRange()
            => new (CommodityQuantity.ElectricPowerL1,
                    PEBC_PowerEnvelopeLimitType.UpperLimit,
                    new NumberRange(0, 0),
                    false);

        private static PEBC_PowerConstraints PowerConstraints(String Id = "powerConstraint1")
            => new (PowerConstraints_Id.Parse(Id),
                    validFrom,
                    PEBC_PowerEnvelopeConsequenceType.Vanish,
                    [ LowerRange(), UpperRange() ],
                    validUntil);

        private static PEBC_EnergyConstraint EnergyConstraint(String Id = "energyConstraint1")
            => new (EnergyConstraint_Id.Parse(Id),
                    validFrom,
                    validUntil,
                    3000,
                    1000,
                    CommodityQuantity.ElectricPowerL1);

        private static PEBC_Instruction Instruction(String              Id                  = "pebc-instruction-1",
                                                    PowerConstraints_Id?  PowerConstraintsId  = null)

            => new (Instruction_Id.Parse(Id),
                    executionTime,
                    false,
                    PowerConstraintsId ?? PowerConstraints_Id.Parse("powerConstraint1"),
                    [ new PEBC_PowerEnvelope(PowerEnvelope_Id.Parse("pe_L1"),
                                             CommodityQuantity.ElectricPowerL1,
                                             [ new PEBC_PowerEnvelopeElement(oneHour, 0, -2000) ]) ]);

        #endregion


        #region ControlType_OfBothHandlers_IsPowerEnvelopeBasedControl()

        [Test]
        public void ControlType_OfBothHandlers_IsPowerEnvelopeBasedControl()

            => Assert.Multiple(() => {
                   Assert.That(new PEBCResourceManager(PowerConstraints()).ControlType,  Is.EqualTo(ControlType.PowerEnvelopeBasedControl));
                   Assert.That(new PEBCEnergyManager().                   ControlType,  Is.EqualTo(ControlType.PowerEnvelopeBasedControl));
               });

        #endregion

        #region Constructor_WithNull_ThrowsArgumentNullException()

        [Test]
        public void Constructor_WithNull_ThrowsArgumentNullException()

            => Assert.Multiple(() => {
                   Assert.Throws<ArgumentNullException>(() => { _ = new PEBCResourceManager((PEBC_PowerConstraints) null!); });
                   Assert.Throws<ArgumentNullException>(() => { _ = new PEBCResourceManager((Func<PEBC_PowerConstraints>) null!); });
               });

        #endregion

        #region Activation_SendsThePowerConstraints_WhichBothSidesRemember()

        [Test]
        public async Task Activation_SendsThePowerConstraints_WhichBothSidesRemember()
        {

            var powerConstraints  = PowerConstraints();
            var resourceManager   = new PEBCResourceManager(powerConstraints);
            var energyManager     = new PEBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager, resourceManager);

            var received = new TaskCompletionSource<PEBC_PowerConstraints>();
            energyManager.OnPowerConstraints += (session, message, ct) => {
                                                    received.TrySetResult(message);
                                                    return Task.CompletedTask;
                                                };

            Assert.That(resourceManager.SentPowerConstraints, Is.Null, "nothing was sent before the activation.");

            await pair.ActivateAsync(ControlType.PowerEnvelopeBasedControl);

            var raised = await received.Task.WaitAsync(ControlTypeSessionPair.Timeout);

            // The CEM sees the message before the RM has its ReceptionStatus back, and the RM only
            // records what was accepted - so the two sides become consistent a moment apart.
            await ControlTypeSessionPair.WaitUntil(() => resourceManager.SentPowerConstraints is not null,
                                                   "the RM did not record the power constraints it sent.");

            Assert.Multiple(() => {
                Assert.That(raised,                               Is.EqualTo(powerConstraints));
                Assert.That(energyManager.LastPowerConstraints,   Is.EqualTo(powerConstraints));
                Assert.That(resourceManager.SentPowerConstraints, Is.EqualTo(powerConstraints));
            });

        }

        #endregion

        #region SendPowerConstraintsAsync_ReplacesThemDuringTheSession()

        [Test]
        public async Task SendPowerConstraintsAsync_ReplacesThemDuringTheSession()
        {

            var resourceManager  = new PEBCResourceManager(PowerConstraints());
            var energyManager    = new PEBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager, resourceManager);

            await pair.ActivateAsync(ControlType.PowerEnvelopeBasedControl);

            await ControlTypeSessionPair.WaitUntil(() => energyManager.LastPowerConstraints is not null,
                                                   "the first power constraints did not reach the CEM.");

            // The fuse rating changed: new constraints, same session.
            var replacement  = PowerConstraints("powerConstraint2");
            var outcome      = await resourceManager.SendPowerConstraintsAsync(pair.RM, replacement);

            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            await ControlTypeSessionPair.WaitUntil(() => energyManager.LastPowerConstraints == replacement,
                                                   "the replacing power constraints did not reach the CEM.");

            Assert.That(resourceManager.SentPowerConstraints, Is.EqualTo(replacement));

        }

        #endregion

        #region Instruction_IsForwardedToOnInstruction_AndAutoAcknowledged()

        [Test]
        public async Task Instruction_IsForwardedToOnInstruction_AndAutoAcknowledged()
        {

            var resourceManager  = new PEBCResourceManager(PowerConstraints());
            var energyManager    = new PEBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager, resourceManager);

            var received = new TaskCompletionSource<PEBC_Instruction>();
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

            await pair.ActivateAsync(ControlType.PowerEnvelopeBasedControl);

            await ControlTypeSessionPair.WaitUntil(() => energyManager.LastPowerConstraints is not null,
                                                   "the power constraints did not reach the CEM.");

            // The instruction refers to the constraints the RM published.
            var instruction  = Instruction(PowerConstraintsId: energyManager.LastPowerConstraints!.Id);
            var outcome      = await pair.CEM.SendAndAwaitReceptionStatusAsync(instruction);
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            var forwarded = await received.Task.WaitAsync(ControlTypeSessionPair.Timeout);

            await ControlTypeSessionPair.WaitUntil(() => { lock (updates) return updates.Count == 1; },
                                                   "the RM did not auto-acknowledge the instruction.");

            Assert.Multiple(() => {
                Assert.That(forwarded.Id,                  Is.EqualTo(instruction.Id));
                Assert.That(forwarded.PowerConstraintsId,  Is.EqualTo(energyManager.LastPowerConstraints!.Id));
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

            var resourceManager  = new PEBCResourceManager(PowerConstraints());
            var energyManager    = new PEBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager, resourceManager);

            resourceManager.OnInstruction += (session, instruction, ct)
                => Task.FromResult<ReceptionStatusValue?>(ReceptionStatusValue.InvalidContent);

            var updates = new List<InstructionStatusUpdate>();
            energyManager.OnInstructionStatusUpdate += (session, update, ct) => {
                                                           lock (updates)
                                                               updates.Add(update);
                                                           return Task.CompletedTask;
                                                       };

            await pair.ActivateAsync(ControlType.PowerEnvelopeBasedControl);

            await ControlTypeSessionPair.WaitUntil(() => energyManager.LastPowerConstraints is not null,
                                                   "the power constraints did not reach the CEM.");

            var outcome = await pair.CEM.SendAndAwaitReceptionStatusAsync(Instruction(PowerConstraintsId: energyManager.LastPowerConstraints!.Id));
            Assert.That(outcome.ReceptionStatus?.Status, Is.EqualTo(ReceptionStatusValue.InvalidContent));

            await Task.Delay(ControlTypeSessionPair.SettleDelay);

            lock (updates)
                Assert.That(updates, Is.Empty, "a rejected instruction must not be acknowledged.");

        }

        #endregion

        #region EnergyConstraints_AreCollectedInOrder()

        [Test]
        public async Task EnergyConstraints_AreCollectedInOrder()
        {

            var energyManager = new PEBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager,
                                                                            new PEBCResourceManager(PowerConstraints()));

            await pair.ActivateAsync(ControlType.PowerEnvelopeBasedControl);

            var first   = EnergyConstraint("energyConstraint1");
            var second  = EnergyConstraint("energyConstraint2");

            Assert.That((await pair.RM.SendAndAwaitReceptionStatusAsync(first)). IsOK, Is.True);
            Assert.That((await pair.RM.SendAndAwaitReceptionStatusAsync(second)).IsOK, Is.True);

            await ControlTypeSessionPair.WaitUntil(() => energyManager.EnergyConstraints.Count == 2,
                                                   "the energy constraints did not reach the CEM.");

            // Several energy constraints are valid at once, so the CEM keeps them all, oldest first.
            Assert.That(energyManager.EnergyConstraints, Is.EqualTo(new[] { first, second }));

        }

        #endregion

        #region Deactivation_ClearsTheConstraintsOfTheEnergyManager()

        [Test]
        public async Task Deactivation_ClearsTheConstraintsOfTheEnergyManager()
        {

            var energyManager = new PEBCEnergyManager();

            await using var pair = await ControlTypeSessionPair.CreateAsync(energyManager,
                                                                            new PEBCResourceManager(PowerConstraints()));

            await pair.ActivateAsync(ControlType.PowerEnvelopeBasedControl);

            Assert.That((await pair.RM.SendAndAwaitReceptionStatusAsync(EnergyConstraint())).IsOK, Is.True);

            await ControlTypeSessionPair.WaitUntil(() => energyManager.LastPowerConstraints is not null &&
                                                         energyManager.EnergyConstraints.Count == 1,
                                                   "the constraints did not reach the CEM.");

            await pair.DeactivateAsync();

            Assert.Multiple(() => {
                Assert.That(energyManager.LastPowerConstraints,  Is.Null);
                Assert.That(energyManager.EnergyConstraints,     Is.Empty);
            });

        }

        #endregion

    }

}

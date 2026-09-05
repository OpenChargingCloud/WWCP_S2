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

using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.Node
{

    /// <summary>
    /// A delegate called on the RM when the CEM sent one of the three PPBC instructions.
    /// </summary>
    /// <typeparam name="TInstruction">The instruction type.</typeparam>
    /// <param name="Session">The session.</param>
    /// <param name="Instruction">The received instruction.</param>
    /// <param name="CancellationToken">A token to cancel the handling.</param>
    /// <returns>The reception status to answer, or null for OK.</returns>
    public delegate Task<ReceptionStatusValue?> OnPPBCInstructionDelegate<in TInstruction>(S2Session          Session,
                                                                                           TInstruction       Instruction,
                                                                                           CancellationToken  CancellationToken)

        where TInstruction : IInstruction;


    /// <summary>
    /// The Resource Manager side of Power Profile Based Control (S2 JSON PPBC): when the CEM selects
    /// PPBC, this handler sends the power sequences the device can run
    /// (<see cref="PPBC_PowerProfileDefinition"/>) and then forwards the CEM's three instructions -
    /// <see cref="PPBC_ScheduleInstruction"/>, <see cref="PPBC_StartInterruptionInstruction"/> and
    /// <see cref="PPBC_EndInterruptionInstruction"/> - to their callbacks, answering each with an
    /// <see cref="InstructionStatusUpdate"/> of status NEW unless the callback returns a status.
    /// The application pushes <see cref="PPBC_PowerProfileStatus"/> and power measurements through
    /// the session itself.
    /// </summary>
    public sealed class PPBCResourceManager : IS2ControlTypeHandler
    {

        #region Data

        private readonly Func<PPBC_PowerProfileDefinition>  powerProfileDefinitionFactory;
        private readonly TimeProvider                       timeProvider;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public ControlType  ControlType
            => ControlType.PowerProfileBasedControl;

        /// <summary>
        /// Whether an InstructionStatusUpdate (NEW) is sent automatically for every received
        /// instruction the callback did not reject (default: true).
        /// </summary>
        public Boolean      AutoAcknowledgeInstructions    { get; init; } = true;

        #endregion

        #region Events

        /// <summary>
        /// An event fired when the CEM scheduled a power sequence.
        /// </summary>
        public event OnPPBCInstructionDelegate<PPBC_ScheduleInstruction>?           OnScheduleInstruction;

        /// <summary>
        /// An event fired when the CEM asked to interrupt a running power sequence.
        /// </summary>
        public event OnPPBCInstructionDelegate<PPBC_StartInterruptionInstruction>?  OnStartInterruptionInstruction;

        /// <summary>
        /// An event fired when the CEM asked to resume an interrupted power sequence.
        /// </summary>
        public event OnPPBCInstructionDelegate<PPBC_EndInterruptionInstruction>?    OnEndInterruptionInstruction;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new PPBC resource manager handler with a fixed power profile definition.
        /// </summary>
        /// <param name="PowerProfileDefinition">The power profile definition sent when PPBC is activated.</param>
        /// <param name="TimeProvider">An optional time provider.</param>
        public PPBCResourceManager(PPBC_PowerProfileDefinition  PowerProfileDefinition,
                                   TimeProvider?                TimeProvider   = null)

            : this(() => PowerProfileDefinition, TimeProvider)

        {
            ArgumentNullException.ThrowIfNull(PowerProfileDefinition);
        }

        /// <summary>
        /// Create a new PPBC resource manager handler with a power profile definition built on
        /// activation (e.g. with the current start and end time).
        /// </summary>
        /// <param name="PowerProfileDefinitionFactory">A factory of the power profile definition.</param>
        /// <param name="TimeProvider">An optional time provider.</param>
        public PPBCResourceManager(Func<PPBC_PowerProfileDefinition>  PowerProfileDefinitionFactory,
                                   TimeProvider?                      TimeProvider   = null)
        {

            ArgumentNullException.ThrowIfNull(PowerProfileDefinitionFactory);

            this.powerProfileDefinitionFactory  = PowerProfileDefinitionFactory;
            this.timeProvider                   = TimeProvider ?? System.TimeProvider.System;

        }

        #endregion


        #region RegisterHandlers(Session)

        /// <inheritdoc/>
        public IDisposable RegisterHandlers(S2Session Session)
        {

            var registrations = new CompositeDisposable();

            registrations.Add(Session.On<PPBC_ScheduleInstruction>          ((session, instruction, ct) => HandleAsync(session, instruction, OnScheduleInstruction,          ct)));
            registrations.Add(Session.On<PPBC_StartInterruptionInstruction> ((session, instruction, ct) => HandleAsync(session, instruction, OnStartInterruptionInstruction, ct)));
            registrations.Add(Session.On<PPBC_EndInterruptionInstruction>   ((session, instruction, ct) => HandleAsync(session, instruction, OnEndInterruptionInstruction,   ct)));

            return registrations;

        }

        #endregion

        #region (private) HandleAsync(Session, Instruction, Handler, CancellationToken)

        /// <summary>
        /// The behaviour shared by the three PPBC instructions: ask the callback, and acknowledge
        /// unless it rejected the instruction.
        /// </summary>
        private async Task<ReceptionStatusValue?> HandleAsync<TInstruction>(S2Session                            Session,
                                                                            TInstruction                         Instruction,
                                                                            OnPPBCInstructionDelegate<TInstruction>?  Handler,
                                                                            CancellationToken                    CancellationToken)

            where TInstruction : IInstruction

        {

            var status = Handler is not null
                             ? await Handler.Invoke(Session, Instruction, CancellationToken).ConfigureAwait(false)
                             : null;

            // A rejected instruction is answered with the rejecting status and not acknowledged.
            if (status.HasValue && status.Value != ReceptionStatusValue.OK)
                return status;

            if (AutoAcknowledgeInstructions)
                await Session.SendAsync(
                          new InstructionStatusUpdate(
                              Instruction.Id,
                              InstructionStatus.New,
                              timeProvider.GetUtcNow()
                          ),
                          CancellationToken
                      ).ConfigureAwait(false);

            return status;

        }

        #endregion

        #region ActivateAsync(Session, CancellationToken)

        /// <inheritdoc/>
        public async Task ActivateAsync(S2Session          Session,
                                        CancellationToken  CancellationToken)
        {

            var powerProfileDefinition = powerProfileDefinitionFactory();

            await Session.SendAndAwaitReceptionStatusAsync(powerProfileDefinition, CancellationToken).ConfigureAwait(false);

        }

        #endregion

        #region DeactivateAsync(Session, CancellationToken)

        /// <inheritdoc/>
        public Task DeactivateAsync(S2Session          Session,
                                    CancellationToken  CancellationToken)

            => Task.CompletedTask;

        #endregion

    }

}

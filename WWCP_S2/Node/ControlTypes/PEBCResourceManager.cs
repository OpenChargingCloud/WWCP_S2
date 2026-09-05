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
    /// A delegate called on the RM when the CEM sent a PEBC instruction.
    /// </summary>
    /// <param name="Session">The session.</param>
    /// <param name="Instruction">The received instruction.</param>
    /// <param name="CancellationToken">A token to cancel the handling.</param>
    /// <returns>The reception status to answer, or null for OK.</returns>
    public delegate Task<ReceptionStatusValue?> OnPEBCInstructionDelegate(S2Session          Session,
                                                                          PEBC_Instruction   Instruction,
                                                                          CancellationToken  CancellationToken);


    /// <summary>
    /// The Resource Manager side of Power Envelope Based Control (S2 JSON PEBC): when the CEM
    /// selects PEBC, this handler sends the limit ranges the device accepts
    /// (<see cref="PEBC_PowerConstraints"/>) and then forwards the CEM's
    /// <see cref="PEBC_Instruction"/>s - the power envelopes to follow - to
    /// <see cref="OnInstruction"/>, answering each with an <see cref="InstructionStatusUpdate"/>
    /// of status NEW unless the callback returns a status.
    ///
    /// PEBC differs from the other control types in that the power constraints are not a one-off
    /// description: they are revokable and may be replaced during a session, which is what
    /// <see cref="SendPowerConstraintsAsync"/> is for - it also records them in
    /// <see cref="SentPowerConstraints"/>, the set a PEBC instruction must refer to.
    /// <see cref="PEBC_EnergyConstraint"/>s and power measurements are pushed by the application
    /// through the session itself.
    /// </summary>
    public sealed class PEBCResourceManager : IS2ControlTypeHandler
    {

        #region Data

        private readonly Func<PEBC_PowerConstraints>  powerConstraintsFactory;
        private readonly TimeProvider                 timeProvider;

        private          PEBC_PowerConstraints?       lastSentPowerConstraints;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public ControlType            ControlType
            => ControlType.PowerEnvelopeBasedControl;

        /// <summary>
        /// Whether an InstructionStatusUpdate (NEW) is sent automatically for every received
        /// instruction the callback did not reject (default: true).
        /// </summary>
        public Boolean                AutoAcknowledgeInstructions    { get; init; } = true;

        /// <summary>
        /// The power constraints most recently sent to the CEM, which is what a PEBC instruction
        /// must refer to.
        /// </summary>
        public PEBC_PowerConstraints?  SentPowerConstraints
            => lastSentPowerConstraints;

        #endregion

        #region Events

        /// <summary>
        /// An event fired when the CEM sent a PEBC instruction; the handler answers with the
        /// returned reception status (null = OK).
        /// </summary>
        public event OnPEBCInstructionDelegate?  OnInstruction;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new PEBC resource manager handler with fixed power constraints.
        /// </summary>
        /// <param name="PowerConstraints">The power constraints sent when PEBC is activated.</param>
        /// <param name="TimeProvider">An optional time provider.</param>
        public PEBCResourceManager(PEBC_PowerConstraints  PowerConstraints,
                                   TimeProvider?          TimeProvider   = null)

            : this(() => PowerConstraints, TimeProvider)

        {
            ArgumentNullException.ThrowIfNull(PowerConstraints);
        }

        /// <summary>
        /// Create a new PEBC resource manager handler with power constraints built on activation
        /// (e.g. with the current ValidFrom).
        /// </summary>
        /// <param name="PowerConstraintsFactory">A factory of the power constraints.</param>
        /// <param name="TimeProvider">An optional time provider.</param>
        public PEBCResourceManager(Func<PEBC_PowerConstraints>  PowerConstraintsFactory,
                                   TimeProvider?                TimeProvider   = null)
        {

            ArgumentNullException.ThrowIfNull(PowerConstraintsFactory);

            this.powerConstraintsFactory  = PowerConstraintsFactory;
            this.timeProvider             = TimeProvider ?? System.TimeProvider.System;

        }

        #endregion


        #region SendPowerConstraintsAsync(Session, PowerConstraints, CancellationToken = default)

        /// <summary>
        /// Send power constraints to the CEM and, once they were accepted, record them in
        /// <see cref="SentPowerConstraints"/>. The previous constraints are not revoked by this:
        /// whether an older set is withdrawn with a RevokeObject is the application's decision.
        /// </summary>
        /// <param name="Session">The session.</param>
        /// <param name="PowerConstraints">The new power constraints.</param>
        /// <param name="CancellationToken">A token to cancel the sending.</param>
        public async Task<S2SendOutcome> SendPowerConstraintsAsync(S2Session              Session,
                                                                   PEBC_PowerConstraints  PowerConstraints,
                                                                   CancellationToken      CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Session);
            ArgumentNullException.ThrowIfNull(PowerConstraints);

            var outcome = await Session.SendAndAwaitReceptionStatusAsync(PowerConstraints, CancellationToken).ConfigureAwait(false);

            if (outcome.IsOK)
                lastSentPowerConstraints = PowerConstraints;

            return outcome;

        }

        #endregion


        #region RegisterHandlers(Session)

        /// <inheritdoc/>
        public IDisposable RegisterHandlers(S2Session Session)

            => Session.On<PEBC_Instruction>(async (session, instruction, ct) => {

                   var status = OnInstruction is not null
                                    ? await OnInstruction.Invoke(session, instruction, ct).ConfigureAwait(false)
                                    : null;

                   // A rejected instruction is answered with the rejecting status and not acknowledged.
                   if (status.HasValue && status.Value != ReceptionStatusValue.OK)
                       return status;

                   if (AutoAcknowledgeInstructions)
                       await session.SendAsync(
                                 new InstructionStatusUpdate(
                                     instruction.Id,
                                     InstructionStatus.New,
                                     timeProvider.GetUtcNow()
                                 ),
                                 ct
                             ).ConfigureAwait(false);

                   return status;

               });

        #endregion

        #region ActivateAsync(Session, CancellationToken)

        /// <inheritdoc/>
        public async Task ActivateAsync(S2Session          Session,
                                        CancellationToken  CancellationToken)
        {

            // The constraints of a previous activation went with the registry the session cleared,
            // so the CEM is told again what this device accepts. The factory is asked rather than
            // the remembered value: a device whose limits have changed passes a closure here.
            await SendPowerConstraintsAsync(
                      Session,
                      powerConstraintsFactory(),
                      CancellationToken
                  ).ConfigureAwait(false);

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

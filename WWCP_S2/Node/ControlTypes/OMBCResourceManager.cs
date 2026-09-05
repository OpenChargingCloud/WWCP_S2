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
    /// A delegate called on the RM when the CEM sent an OMBC instruction.
    /// </summary>
    /// <param name="Session">The session.</param>
    /// <param name="Instruction">The received instruction.</param>
    /// <param name="CancellationToken">A token to cancel the handling.</param>
    /// <returns>The reception status to answer, or null for OK.</returns>
    public delegate Task<ReceptionStatusValue?> OnOMBCInstructionDelegate(S2Session          Session,
                                                                          OMBC_Instruction   Instruction,
                                                                          CancellationToken  CancellationToken);


    /// <summary>
    /// The Resource Manager side of Operation Mode Based Control (S2 JSON OMBC): when the CEM
    /// selects OMBC, this handler sends the operation modes and transitions the device offers
    /// (<see cref="OMBC_SystemDescription"/>) and then forwards the CEM's
    /// <see cref="OMBC_Instruction"/>s to <see cref="OnInstruction"/>, answering each with an
    /// <see cref="InstructionStatusUpdate"/> of status NEW unless the callback returns a status.
    /// The application pushes <see cref="OMBC_Status"/>, <see cref="OMBC_TimerStatus"/> and power
    /// measurements through the session itself.
    /// </summary>
    public sealed class OMBCResourceManager : IS2ControlTypeHandler
    {

        #region Data

        private readonly Func<OMBC_SystemDescription>  systemDescriptionFactory;
        private readonly TimeProvider                  timeProvider;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public ControlType  ControlType
            => ControlType.OperationModeBasedControl;

        /// <summary>
        /// Whether an InstructionStatusUpdate (NEW) is sent automatically for every received
        /// instruction the callback did not reject (default: true).
        /// </summary>
        public Boolean      AutoAcknowledgeInstructions    { get; init; } = true;

        #endregion

        #region Events

        /// <summary>
        /// An event fired when the CEM sent an OMBC instruction; the handler answers with the
        /// returned reception status (null = OK).
        /// </summary>
        public event OnOMBCInstructionDelegate?  OnInstruction;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new OMBC resource manager handler with a fixed system description.
        /// </summary>
        /// <param name="SystemDescription">The OMBC system description sent when OMBC is activated.</param>
        /// <param name="TimeProvider">An optional time provider.</param>
        public OMBCResourceManager(OMBC_SystemDescription  SystemDescription,
                                   TimeProvider?           TimeProvider   = null)

            : this(() => SystemDescription, TimeProvider)

        {
            ArgumentNullException.ThrowIfNull(SystemDescription);
        }

        /// <summary>
        /// Create a new OMBC resource manager handler with a system description built on activation
        /// (e.g. with the current ValidFrom).
        /// </summary>
        /// <param name="SystemDescriptionFactory">A factory of the OMBC system description.</param>
        /// <param name="TimeProvider">An optional time provider.</param>
        public OMBCResourceManager(Func<OMBC_SystemDescription>  SystemDescriptionFactory,
                                   TimeProvider?                 TimeProvider   = null)
        {

            ArgumentNullException.ThrowIfNull(SystemDescriptionFactory);

            this.systemDescriptionFactory  = SystemDescriptionFactory;
            this.timeProvider              = TimeProvider ?? System.TimeProvider.System;

        }

        #endregion


        #region RegisterHandlers(Session)

        /// <inheritdoc/>
        public IDisposable RegisterHandlers(S2Session Session)

            => Session.On<OMBC_Instruction>(async (session, instruction, ct) => {

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

            var systemDescription = systemDescriptionFactory();

            await Session.SendAndAwaitReceptionStatusAsync(systemDescription, CancellationToken).ConfigureAwait(false);

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

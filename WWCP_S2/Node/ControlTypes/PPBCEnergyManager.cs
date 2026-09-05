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
    /// A delegate called on the CEM for a received PPBC message of the given type.
    /// </summary>
    public delegate Task OnPPBCMessageDelegate<in TMessage>(S2Session          Session,
                                                            TMessage           Message,
                                                            CancellationToken  CancellationToken)
        where TMessage : IS2Message;


    /// <summary>
    /// The Customer Energy Manager side of Power Profile Based Control: once PPBC is active it
    /// collects the RM's <see cref="PPBC_PowerProfileDefinition"/> and
    /// <see cref="PPBC_PowerProfileStatus"/> (raised as events) and lets the CEM schedule, interrupt
    /// and resume the power sequences through the session. The latest definition and status are
    /// cached for the current session.
    /// </summary>
    public sealed class PPBCEnergyManager : IS2ControlTypeHandler
    {

        #region Properties

        /// <inheritdoc/>
        public ControlType  ControlType
            => ControlType.PowerProfileBasedControl;

        /// <summary>
        /// The most recently received power profile definition of the current session; a schedule
        /// instruction refers to one of its sequence containers.
        /// </summary>
        public PPBC_PowerProfileDefinition?  LastPowerProfileDefinition    { get; private set; }

        /// <summary>
        /// The most recently received power profile status of the current session.
        /// </summary>
        public PPBC_PowerProfileStatus?      LastPowerProfileStatus        { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// An event fired when the RM sent a power profile definition.
        /// </summary>
        public event OnPPBCMessageDelegate<PPBC_PowerProfileDefinition>?  OnPowerProfileDefinition;

        /// <summary>
        /// An event fired when the RM sent the status of its power sequence containers.
        /// </summary>
        public event OnPPBCMessageDelegate<PPBC_PowerProfileStatus>?      OnPowerProfileStatus;

        /// <summary>
        /// An event fired when the RM sent an instruction status update.
        /// </summary>
        public event OnPPBCMessageDelegate<InstructionStatusUpdate>?      OnInstructionStatusUpdate;

        #endregion

        #region RegisterHandlers(Session)

        /// <inheritdoc/>
        public IDisposable RegisterHandlers(S2Session Session)
        {

            var registrations = new CompositeDisposable();

            registrations.Add(Session.On<PPBC_PowerProfileDefinition>(async (session, message, ct) => {
                LastPowerProfileDefinition = message;
                if (OnPowerProfileDefinition is not null)
                    await OnPowerProfileDefinition.Invoke(session, message, ct).ConfigureAwait(false);
                return null;
            }));

            registrations.Add(Session.On<PPBC_PowerProfileStatus>(async (session, message, ct) => {
                LastPowerProfileStatus = message;
                if (OnPowerProfileStatus is not null)
                    await OnPowerProfileStatus.Invoke(session, message, ct).ConfigureAwait(false);
                return null;
            }));

            registrations.Add(Session.On<InstructionStatusUpdate>(async (session, message, ct) => {
                if (OnInstructionStatusUpdate is not null)
                    await OnInstructionStatusUpdate.Invoke(session, message, ct).ConfigureAwait(false);
                return null;
            }));

            return registrations;

        }

        #endregion

        #region ActivateAsync / DeactivateAsync

        /// <inheritdoc/>
        public Task ActivateAsync(S2Session Session, CancellationToken CancellationToken)
            => Task.CompletedTask;

        /// <inheritdoc/>
        public Task DeactivateAsync(S2Session Session, CancellationToken CancellationToken)
        {
            LastPowerProfileDefinition  = null;
            LastPowerProfileStatus      = null;
            return Task.CompletedTask;
        }

        #endregion

    }

}

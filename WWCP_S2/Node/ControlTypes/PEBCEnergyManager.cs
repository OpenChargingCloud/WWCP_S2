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
    /// A delegate called on the CEM for a received PEBC message of the given type.
    /// </summary>
    public delegate Task OnPEBCMessageDelegate<in TMessage>(S2Session          Session,
                                                            TMessage           Message,
                                                            CancellationToken  CancellationToken)
        where TMessage : IS2Message;


    /// <summary>
    /// The Customer Energy Manager side of Power Envelope Based Control: once PEBC is active it
    /// collects the RM's <see cref="PEBC_PowerConstraints"/> and <see cref="PEBC_EnergyConstraint"/>s
    /// (raised as events) and lets the CEM send <see cref="PEBC_Instruction"/>s through the session.
    ///
    /// The constraints are the CEM's whole picture of what it may ask for, so more than the latest
    /// one matters: <see cref="LastPowerConstraints"/> is what an instruction refers to, and
    /// <see cref="EnergyConstraints"/> keeps every energy constraint received in this session, in
    /// the order they arrived.
    /// </summary>
    public sealed class PEBCEnergyManager : IS2ControlTypeHandler
    {

        #region Data

        private readonly List<PEBC_EnergyConstraint> energyConstraints = [];

        #endregion

        #region Properties

        /// <inheritdoc/>
        public ControlType             ControlType
            => ControlType.PowerEnvelopeBasedControl;

        /// <summary>
        /// The most recently received power constraints of the current session; a
        /// <see cref="PEBC_Instruction"/> must refer to their identification.
        /// </summary>
        public PEBC_PowerConstraints?  LastPowerConstraints    { get; private set; }

        /// <summary>
        /// Every energy constraint received in the current session, oldest first. Several may be
        /// valid at once, for different commodity quantities and periods.
        /// </summary>
        public IReadOnlyList<PEBC_EnergyConstraint>  EnergyConstraints
        {
            get
            {
                lock (energyConstraints)
                    return [.. energyConstraints];
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// An event fired when the RM sent its PEBC power constraints.
        /// </summary>
        public event OnPEBCMessageDelegate<PEBC_PowerConstraints>?    OnPowerConstraints;

        /// <summary>
        /// An event fired when the RM sent a PEBC energy constraint.
        /// </summary>
        public event OnPEBCMessageDelegate<PEBC_EnergyConstraint>?    OnEnergyConstraint;

        /// <summary>
        /// An event fired when the RM sent an instruction status update.
        /// </summary>
        public event OnPEBCMessageDelegate<InstructionStatusUpdate>?  OnInstructionStatusUpdate;

        #endregion

        #region RegisterHandlers(Session)

        /// <inheritdoc/>
        public IDisposable RegisterHandlers(S2Session Session)
        {

            var registrations = new CompositeDisposable();

            registrations.Add(Session.On<PEBC_PowerConstraints>(async (session, message, ct) => {
                LastPowerConstraints = message;
                if (OnPowerConstraints is not null)
                    await OnPowerConstraints.Invoke(session, message, ct).ConfigureAwait(false);
                return null;
            }));

            registrations.Add(Session.On<PEBC_EnergyConstraint>(async (session, message, ct) => {
                lock (energyConstraints)
                    energyConstraints.Add(message);
                if (OnEnergyConstraint is not null)
                    await OnEnergyConstraint.Invoke(session, message, ct).ConfigureAwait(false);
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

            LastPowerConstraints = null;

            lock (energyConstraints)
                energyConstraints.Clear();

            return Task.CompletedTask;

        }

        #endregion

    }

}

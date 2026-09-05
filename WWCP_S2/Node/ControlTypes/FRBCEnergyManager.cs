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
    /// A delegate called on the CEM for a received FRBC message of the given type.
    /// </summary>
    public delegate Task OnFRBCMessageDelegate<in TMessage>(S2Session          Session,
                                                            TMessage           Message,
                                                            CancellationToken  CancellationToken)
        where TMessage : IS2Message;


    /// <summary>
    /// The Customer Energy Manager side of Fill Rate Based Control: once FRBC is active it collects
    /// the RM's <see cref="FRBC_SystemDescription"/>, <see cref="FRBC_ActuatorStatus"/> and
    /// <see cref="FRBC_StorageStatus"/> (raised as events) and lets the CEM send
    /// <see cref="FRBC_Instruction"/>s and target profiles through the session. The latest system
    /// description and storage status are cached for the current session.
    /// </summary>
    public sealed class FRBCEnergyManager : IS2ControlTypeHandler
    {

        #region Properties

        /// <inheritdoc/>
        public ControlType  ControlType
            => ControlType.FillRateBasedControl;

        /// <summary>
        /// The most recently received system description of the current session.
        /// </summary>
        public FRBC_SystemDescription?  LastSystemDescription    { get; private set; }

        /// <summary>
        /// The most recently received storage status of the current session.
        /// </summary>
        public FRBC_StorageStatus?      LastStorageStatus        { get; private set; }

        /// <summary>
        /// The most recently received actuator status of the current session.
        /// </summary>
        public FRBC_ActuatorStatus?     LastActuatorStatus       { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// An event fired when the RM sent its FRBC system description.
        /// </summary>
        public event OnFRBCMessageDelegate<FRBC_SystemDescription>?  OnSystemDescription;

        /// <summary>
        /// An event fired when the RM sent an FRBC storage status.
        /// </summary>
        public event OnFRBCMessageDelegate<FRBC_StorageStatus>?      OnStorageStatus;

        /// <summary>
        /// An event fired when the RM sent an FRBC actuator status.
        /// </summary>
        public event OnFRBCMessageDelegate<FRBC_ActuatorStatus>?     OnActuatorStatus;

        /// <summary>
        /// An event fired when the RM sent an instruction status update.
        /// </summary>
        public event OnFRBCMessageDelegate<InstructionStatusUpdate>? OnInstructionStatusUpdate;

        #endregion

        #region RegisterHandlers(Session)

        /// <inheritdoc/>
        public IDisposable RegisterHandlers(S2Session Session)
        {

            var registrations = new CompositeDisposable();

            registrations.Add(Session.On<FRBC_SystemDescription>(async (session, message, ct) => {
                LastSystemDescription = message;
                if (OnSystemDescription is not null)
                    await OnSystemDescription.Invoke(session, message, ct).ConfigureAwait(false);
                return null;
            }));

            registrations.Add(Session.On<FRBC_StorageStatus>(async (session, message, ct) => {
                LastStorageStatus = message;
                if (OnStorageStatus is not null)
                    await OnStorageStatus.Invoke(session, message, ct).ConfigureAwait(false);
                return null;
            }));

            registrations.Add(Session.On<FRBC_ActuatorStatus>(async (session, message, ct) => {
                LastActuatorStatus = message;
                if (OnActuatorStatus is not null)
                    await OnActuatorStatus.Invoke(session, message, ct).ConfigureAwait(false);
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
            LastSystemDescription  = null;
            LastStorageStatus      = null;
            LastActuatorStatus     = null;
            return Task.CompletedTask;
        }

        #endregion


        #region (class) CompositeDisposable

        private sealed class CompositeDisposable : IDisposable
        {

            private readonly List<IDisposable> disposables = [];

            public void Add(IDisposable Disposable)
                => disposables.Add(Disposable);

            public void Dispose()
            {
                foreach (var disposable in disposables)
                    disposable.Dispose();
                disposables.Clear();
            }

        }

        #endregion

    }

}

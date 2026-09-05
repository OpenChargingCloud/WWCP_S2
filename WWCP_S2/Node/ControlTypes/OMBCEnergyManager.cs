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
    /// A delegate called on the CEM for a received OMBC message of the given type.
    /// </summary>
    public delegate Task OnOMBCMessageDelegate<in TMessage>(S2Session          Session,
                                                            TMessage           Message,
                                                            CancellationToken  CancellationToken)
        where TMessage : IS2Message;


    /// <summary>
    /// The Customer Energy Manager side of Operation Mode Based Control: once OMBC is active it
    /// collects the RM's <see cref="OMBC_SystemDescription"/>, <see cref="OMBC_Status"/> and
    /// <see cref="OMBC_TimerStatus"/> (raised as events) and lets the CEM send
    /// <see cref="OMBC_Instruction"/>s through the session. The latest system description and
    /// status are cached for the current session.
    /// </summary>
    public sealed class OMBCEnergyManager : IS2ControlTypeHandler
    {

        #region Properties

        /// <inheritdoc/>
        public ControlType  ControlType
            => ControlType.OperationModeBasedControl;

        /// <summary>
        /// The most recently received system description of the current session.
        /// </summary>
        public OMBC_SystemDescription?  LastSystemDescription    { get; private set; }

        /// <summary>
        /// The most recently received operation mode status of the current session.
        /// </summary>
        public OMBC_Status?             LastStatus               { get; private set; }

        /// <summary>
        /// The most recently received timer status of the current session.
        /// </summary>
        public OMBC_TimerStatus?        LastTimerStatus          { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// An event fired when the RM sent its OMBC system description.
        /// </summary>
        public event OnOMBCMessageDelegate<OMBC_SystemDescription>?   OnSystemDescription;

        /// <summary>
        /// An event fired when the RM sent an OMBC status.
        /// </summary>
        public event OnOMBCMessageDelegate<OMBC_Status>?              OnStatus;

        /// <summary>
        /// An event fired when the RM reported that one of its timers has finished.
        /// </summary>
        public event OnOMBCMessageDelegate<OMBC_TimerStatus>?         OnTimerStatus;

        /// <summary>
        /// An event fired when the RM sent an instruction status update.
        /// </summary>
        public event OnOMBCMessageDelegate<InstructionStatusUpdate>?  OnInstructionStatusUpdate;

        #endregion

        #region RegisterHandlers(Session)

        /// <inheritdoc/>
        public IDisposable RegisterHandlers(S2Session Session)
        {

            var registrations = new CompositeDisposable();

            registrations.Add(Session.On<OMBC_SystemDescription>(async (session, message, ct) => {
                LastSystemDescription = message;
                if (OnSystemDescription is not null)
                    await OnSystemDescription.Invoke(session, message, ct).ConfigureAwait(false);
                return null;
            }));

            registrations.Add(Session.On<OMBC_Status>(async (session, message, ct) => {
                LastStatus = message;
                if (OnStatus is not null)
                    await OnStatus.Invoke(session, message, ct).ConfigureAwait(false);
                return null;
            }));

            registrations.Add(Session.On<OMBC_TimerStatus>(async (session, message, ct) => {
                LastTimerStatus = message;
                if (OnTimerStatus is not null)
                    await OnTimerStatus.Invoke(session, message, ct).ConfigureAwait(false);
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
            LastStatus             = null;
            LastTimerStatus        = null;
            return Task.CompletedTask;
        }

        #endregion

    }

}

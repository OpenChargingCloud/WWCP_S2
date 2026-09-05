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
    /// A delegate called on the CEM for a received DDBC message of the given type.
    /// </summary>
    public delegate Task OnDDBCMessageDelegate<in TMessage>(S2Session          Session,
                                                            TMessage           Message,
                                                            CancellationToken  CancellationToken)
        where TMessage : IS2Message;


    /// <summary>
    /// The Customer Energy Manager side of Demand Driven Based Control: once DDBC is active it
    /// collects the RM's <see cref="DDBC_SystemDescription"/>, <see cref="DDBC_ActuatorStatus"/>,
    /// <see cref="DDBC_PresentDemandStatus"/>, <see cref="DDBC_AverageDemandRateForecast"/> and
    /// <see cref="DDBC_TimerStatus"/> (raised as events) and lets the CEM send
    /// <see cref="DDBC_Instruction"/>s through the session. The latest of each is cached for the
    /// current session - the present demand rate above all, since that is what a DDBC instruction
    /// responds to.
    /// </summary>
    public sealed class DDBCEnergyManager : IS2ControlTypeHandler
    {

        #region Properties

        /// <inheritdoc/>
        public ControlType  ControlType
            => ControlType.DemandDrivenBasedControl;

        /// <summary>
        /// The most recently received system description of the current session.
        /// </summary>
        public DDBC_SystemDescription?          LastSystemDescription          { get; private set; }

        /// <summary>
        /// The most recently received actuator status of the current session.
        /// </summary>
        public DDBC_ActuatorStatus?             LastActuatorStatus             { get; private set; }

        /// <summary>
        /// The most recently received present demand status of the current session.
        /// </summary>
        public DDBC_PresentDemandStatus?        LastPresentDemandStatus        { get; private set; }

        /// <summary>
        /// The most recently received average demand rate forecast of the current session.
        /// </summary>
        public DDBC_AverageDemandRateForecast?  LastAverageDemandRateForecast  { get; private set; }

        /// <summary>
        /// The most recently received timer status of the current session.
        /// </summary>
        public DDBC_TimerStatus?                LastTimerStatus                { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// An event fired when the RM sent its DDBC system description.
        /// </summary>
        public event OnDDBCMessageDelegate<DDBC_SystemDescription>?          OnSystemDescription;

        /// <summary>
        /// An event fired when the RM sent a DDBC actuator status.
        /// </summary>
        public event OnDDBCMessageDelegate<DDBC_ActuatorStatus>?             OnActuatorStatus;

        /// <summary>
        /// An event fired when the RM reported the demand rate it currently sees.
        /// </summary>
        public event OnDDBCMessageDelegate<DDBC_PresentDemandStatus>?        OnPresentDemandStatus;

        /// <summary>
        /// An event fired when the RM sent an average demand rate forecast.
        /// </summary>
        public event OnDDBCMessageDelegate<DDBC_AverageDemandRateForecast>?  OnAverageDemandRateForecast;

        /// <summary>
        /// An event fired when the RM reported that one of its timers has finished.
        /// </summary>
        public event OnDDBCMessageDelegate<DDBC_TimerStatus>?                OnTimerStatus;

        /// <summary>
        /// An event fired when the RM sent an instruction status update.
        /// </summary>
        public event OnDDBCMessageDelegate<InstructionStatusUpdate>?         OnInstructionStatusUpdate;

        #endregion

        #region RegisterHandlers(Session)

        /// <inheritdoc/>
        public IDisposable RegisterHandlers(S2Session Session)
        {

            var registrations = new CompositeDisposable();

            registrations.Add(Session.On<DDBC_SystemDescription>(async (session, message, ct) => {
                LastSystemDescription = message;
                if (OnSystemDescription is not null)
                    await OnSystemDescription.Invoke(session, message, ct).ConfigureAwait(false);
                return null;
            }));

            registrations.Add(Session.On<DDBC_ActuatorStatus>(async (session, message, ct) => {
                LastActuatorStatus = message;
                if (OnActuatorStatus is not null)
                    await OnActuatorStatus.Invoke(session, message, ct).ConfigureAwait(false);
                return null;
            }));

            registrations.Add(Session.On<DDBC_PresentDemandStatus>(async (session, message, ct) => {
                LastPresentDemandStatus = message;
                if (OnPresentDemandStatus is not null)
                    await OnPresentDemandStatus.Invoke(session, message, ct).ConfigureAwait(false);
                return null;
            }));

            registrations.Add(Session.On<DDBC_AverageDemandRateForecast>(async (session, message, ct) => {
                LastAverageDemandRateForecast = message;
                if (OnAverageDemandRateForecast is not null)
                    await OnAverageDemandRateForecast.Invoke(session, message, ct).ConfigureAwait(false);
                return null;
            }));

            registrations.Add(Session.On<DDBC_TimerStatus>(async (session, message, ct) => {
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
            LastSystemDescription          = null;
            LastActuatorStatus             = null;
            LastPresentDemandStatus        = null;
            LastAverageDemandRateForecast  = null;
            LastTimerStatus                = null;
            return Task.CompletedTask;
        }

        #endregion

    }

}

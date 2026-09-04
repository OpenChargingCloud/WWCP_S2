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

namespace cloud.charging.open.protocols.S2.Session
{

    /// <summary>
    /// A message handler registered on a session: validates and enqueues the message and
    /// decides its ReceptionStatus. Returning null means OK. Handlers must return quickly
    /// (the peer may give up on the ReceptionStatus after a few seconds); long-running work
    /// is dispatched by the application outside the session pipeline (PLAN.md §3.2).
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="Session">The session the message was received on.</param>
    /// <param name="Message">The received message.</param>
    /// <param name="CancellationToken">A token to cancel the processing.</param>
    public delegate Task<ReceptionStatusValue?> S2MessageHandler<in TMessage>(S2Session          Session,
                                                                              TMessage           Message,
                                                                              CancellationToken  CancellationToken)

        where TMessage : IS2Message;


    /// <summary>
    /// The behaviour of one control type on one side of a session (PLAN.md §3.1): a CEM-side
    /// handler consumes system descriptions and statuses and issues instructions, an RM-side
    /// handler translates instructions into device actions and reports statuses. The session
    /// activates the handler when the control type is selected and deactivates it when another
    /// control type (or NO_SELECTION) is selected or the session ends.
    /// </summary>
    public interface IS2ControlTypeHandler
    {

        /// <summary>
        /// The control type this handler implements.
        /// </summary>
        ControlType  ControlType    { get; }

        /// <summary>
        /// Register the message handlers of this control type on the session. The returned
        /// registration is disposed by the session on deactivation.
        /// </summary>
        /// <param name="Session">The session.</param>
        IDisposable  RegisterHandlers(S2Session Session);

        /// <summary>
        /// Called after the control type was activated (the RM typically sends its system
        /// description here).
        /// </summary>
        /// <param name="Session">The session.</param>
        /// <param name="CancellationToken">A token to cancel the activation.</param>
        Task         ActivateAsync  (S2Session          Session,
                                     CancellationToken  CancellationToken);

        /// <summary>
        /// Called before the control type is deactivated; pending instructions are no longer tracked afterwards.
        /// </summary>
        /// <param name="Session">The session.</param>
        /// <param name="CancellationToken">A token to cancel the deactivation.</param>
        Task         DeactivateAsync(S2Session          Session,
                                     CancellationToken  CancellationToken);

    }

}

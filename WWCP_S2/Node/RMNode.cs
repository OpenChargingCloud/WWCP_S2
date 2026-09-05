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

using Microsoft.Extensions.Logging;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.Node
{

    /// <summary>
    /// A Resource Manager node (PLAN.md Phase 10a): one hosted RM that connects to one CEM at a
    /// time. It publishes its <see cref="ResourceManagerDetails"/> as soon as a session opens,
    /// registers the control-type handlers the CEM may select (e.g. an FRBC resource manager), and
    /// exposes the single current session. Measurements and forecasts are sent through the session
    /// independently of the active control type.
    /// </summary>
    public sealed class RMNode : AS2Node
    {

        #region Data

        private readonly    List<IS2ControlTypeHandler>  controlTypeHandlers   = [];
        private             ResourceManagerDetails?      resourceManagerDetails;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public override EnergyManagementRole  Role
            => EnergyManagementRole.RM;

        /// <summary>
        /// The ResourceManagerDetails sent at the start of every session (may be set before or after starting).
        /// </summary>
        public ResourceManagerDetails?  ResourceManagerDetails
        {
            get
            {
                lock (controlTypeHandlers)
                    return resourceManagerDetails;
            }
            set
            {
                lock (controlTypeHandlers)
                    resourceManagerDetails = value;
            }
        }

        /// <summary>
        /// The single current session with the CEM, when connected.
        /// </summary>
        public S2NodeSession?  CurrentSession
        {
            get
            {
                var sessions = Sessions;
                return sessions.Count > 0 ? sessions[0] : null;
            }
        }

        /// <summary>
        /// The control types this RM offers to a CEM.
        /// </summary>
        public IReadOnlyList<ControlType>  OfferedControlTypes
        {
            get
            {
                lock (controlTypeHandlers)
                    return [.. controlTypeHandlers.Select(handler => handler.ControlType)];
            }
        }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new Resource Manager node.
        /// </summary>
        /// <param name="Node">The hosted RM node.</param>
        /// <param name="Options">The node options.</param>
        /// <param name="ResourceManagerDetails">The ResourceManagerDetails to publish at the start of a session (may be set later).</param>
        /// <param name="Store">An optional store.</param>
        /// <param name="ServiceDiscovery">An optional service discovery.</param>
        /// <param name="HTTPServer">An optional existing HTTP server.</param>
        /// <param name="TimeProvider">An optional time provider.</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        public RMNode(HostedNode               Node,
                      S2NodeOptions            Options,
                      ResourceManagerDetails?  ResourceManagerDetails   = null,
                      IS2Store?                Store                    = null,
                      IServiceDiscovery?       ServiceDiscovery         = null,
                      HTTPServer?              HTTPServer               = null,
                      TimeProvider?            TimeProvider             = null,
                      ILoggerFactory?          LoggerFactory            = null)

            : base(Node,
                   Options,
                   Store,
                   ServiceDiscovery,
                   HTTPServer,
                   TimeProvider,
                   LoggerFactory)

        {
            this.resourceManagerDetails = ResourceManagerDetails;
        }

        #endregion


        #region RegisterControlType(Handler)

        /// <summary>
        /// Offer a control type to the CEM. Its handler is registered on every session and
        /// activated when the CEM selects the control type.
        /// </summary>
        /// <param name="Handler">A control-type handler (e.g. an FRBC resource manager).</param>
        public RMNode RegisterControlType(IS2ControlTypeHandler Handler)
        {

            ArgumentNullException.ThrowIfNull(Handler);

            lock (controlTypeHandlers)
            {

                if (controlTypeHandlers.Any(existing => existing.ControlType == Handler.ControlType))
                    throw new ArgumentException($"A handler for the control type '{Handler.ControlType}' is already registered!", nameof(Handler));

                controlTypeHandlers.Add(Handler);

            }

            return this;

        }

        #endregion

        #region (protected override) OnSessionEstablishedAsync(Session, CancellationToken)

        /// <inheritdoc/>
        protected override async Task OnSessionEstablishedAsync(S2NodeSession      Session,
                                                               CancellationToken  CancellationToken)
        {

            IS2ControlTypeHandler[]  handlers;
            ResourceManagerDetails?  details;

            lock (controlTypeHandlers)
            {
                handlers  = [.. controlTypeHandlers];
                details   = resourceManagerDetails;
            }

            // Offer the control types (the CEM activates one via SelectControlType).
            foreach (var handler in handlers)
                Session.Session.RegisterControlType(handler);

            // The RM publishes its details right after the session opened (S2 Connect flow).
            if (details is not null)
            {
                var outcome = await Session.SendAndAwaitReceptionStatusAsync(details, CancellationToken).ConfigureAwait(false);
                if (!outcome.IsOK)
                    Logger?.LogWarning("S2 RM node {NodeId}: the CEM did not accept the ResourceManagerDetails ({Result}).", NodeId, outcome);
            }

        }

        #endregion

    }

}

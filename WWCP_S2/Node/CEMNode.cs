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
    /// A Customer Energy Manager node (PLAN.md Phase 10a): one hosted CEM that supervises many
    /// RMs. For every session it receives the RM's <see cref="ResourceManagerDetails"/>, picks a
    /// control type with the <see cref="SelectControlTypePolicy"/> and sends the corresponding
    /// SelectControlType, registers the CEM-side control-type handlers, and can revoke objects.
    /// </summary>
    public sealed class CEMNode : AS2Node
    {

        #region Data

        private readonly    List<IS2ControlTypeHandler>  controlTypeHandlers   = [];

        #endregion

        #region Properties

        /// <inheritdoc/>
        public override EnergyManagementRole  Role
            => EnergyManagementRole.CEM;

        /// <summary>
        /// The policy choosing a control type from the RM's details (default: the first available
        /// control type of a registered handler, or none). Return null to select no control type.
        /// </summary>
        public Func<S2NodeSession, ResourceManagerDetails, ControlType?>  SelectControlTypePolicy    { get; set; }

        /// <summary>
        /// The control types this CEM can drive.
        /// </summary>
        public IReadOnlyList<ControlType>  SupportedControlTypes
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
        /// Create a new Customer Energy Manager node.
        /// </summary>
        /// <param name="Node">The hosted CEM node.</param>
        /// <param name="Options">The node options.</param>
        /// <param name="Store">An optional store.</param>
        /// <param name="ServiceDiscovery">An optional service discovery.</param>
        /// <param name="HTTPServer">An optional existing HTTP server.</param>
        /// <param name="TimeProvider">An optional time provider.</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        public CEMNode(HostedNode          Node,
                       S2NodeOptions       Options,
                       IS2Store?           Store              = null,
                       IServiceDiscovery?  ServiceDiscovery   = null,
                       HTTPServer?         HTTPServer         = null,
                       TimeProvider?       TimeProvider       = null,
                       ILoggerFactory?     LoggerFactory      = null)

            : base(Node,
                   Options,
                   Store,
                   ServiceDiscovery,
                   HTTPServer,
                   TimeProvider,
                   LoggerFactory)

        {

            this.SelectControlTypePolicy = DefaultPolicy;

        }

        #endregion


        #region RegisterControlType(Handler)

        /// <summary>
        /// Register a CEM-side control-type handler. It is registered on every session and
        /// activated when this CEM selects the control type.
        /// </summary>
        /// <param name="Handler">A control-type handler (e.g. an FRBC energy manager).</param>
        public CEMNode RegisterControlType(IS2ControlTypeHandler Handler)
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

        #region RevokeAsync(RemoteNodeId, ObjectType, ObjectId, CancellationToken = default)

        /// <summary>
        /// Revoke an object the CEM sent earlier to the given RM.
        /// </summary>
        /// <param name="RemoteNodeId">The identification of the RM.</param>
        /// <param name="ObjectType">The type of the revokable object.</param>
        /// <param name="ObjectId">The identification of the object (its id, or the message id for a system description).</param>
        /// <param name="CancellationToken">A token to cancel the sending.</param>
        public async Task<Boolean> RevokeAsync(Node_Id            RemoteNodeId,
                                               RevokableObject    ObjectType,
                                               S2Object_Id        ObjectId,
                                               CancellationToken  CancellationToken   = default)
        {

            var session = Sessions.FirstOrDefault(session => session.RemoteNodeId == RemoteNodeId);
            if (session is null)
                return false;

            var outcome = await session.SendAndAwaitReceptionStatusAsync(
                                    new RevokeObject(ObjectType, ObjectId),
                                    CancellationToken
                                ).ConfigureAwait(false);

            return outcome.IsOK;

        }

        #endregion

        #region (protected override) OnSessionEstablishedAsync(Session, CancellationToken)

        /// <inheritdoc/>
        protected override Task OnSessionEstablishedAsync(S2NodeSession      Session,
                                                         CancellationToken  CancellationToken)
        {

            IS2ControlTypeHandler[] handlers;
            lock (controlTypeHandlers)
                handlers = [.. controlTypeHandlers];

            foreach (var handler in handlers)
                Session.Session.RegisterControlType(handler);

            // The RM sends its ResourceManagerDetails after the session opened; when they arrive the
            // CEM picks a control type and sends the SelectControlType.
            Session.Session.On<ResourceManagerDetails>(async (s, details, ct) => {

                var controlType = SelectControlTypePolicy(Session, details);

                if (controlType.HasValue)
                {
                    var outcome = await Session.SendAndAwaitReceptionStatusAsync(new SelectControlType(controlType.Value), ct).ConfigureAwait(false);
                    if (!outcome.IsOK)
                        Logger?.LogWarning("S2 CEM node {NodeId}: the RM did not accept the SelectControlType ({Result}).", NodeId, outcome);
                }

                return null;

            });

            return Task.CompletedTask;

        }

        #endregion

        #region (private) DefaultPolicy(Session, Details)

        private ControlType? DefaultPolicy(S2NodeSession           Session,
                                           ResourceManagerDetails  Details)
        {

            lock (controlTypeHandlers)
            {
                foreach (var handler in controlTypeHandlers)
                {
                    if (Details.AvailableControlTypes.Contains(handler.ControlType))
                        return handler.ControlType;
                }
            }

            return null;

        }

        #endregion

    }

}

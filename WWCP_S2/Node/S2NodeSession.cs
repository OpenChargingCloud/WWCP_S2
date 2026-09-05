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

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.Node
{

    /// <summary>
    /// An S2 session of a node with a paired remote node: the session itself, the pairing it
    /// belongs to, the local hosted node and, for sessions the node opened as communication
    /// client, the reconnecting client that keeps it alive.
    /// </summary>
    public sealed class S2NodeSession
    {

        #region Properties

        /// <summary>
        /// The node owning this session.
        /// </summary>
        public AS2Node                     Node                     { get; }

        /// <summary>
        /// The S2 session.
        /// </summary>
        public S2Session                   Session                  { get; }

        /// <summary>
        /// The pairing this session belongs to.
        /// </summary>
        public Pairing                     Pairing                  { get; }

        /// <summary>
        /// The local hosted node.
        /// </summary>
        public HostedNode                  LocalNode                { get; }

        /// <summary>
        /// The identification of the remote node.
        /// </summary>
        public Node_Id                     RemoteNodeId
            => Pairing.RemoteNodeId;

        /// <summary>
        /// The description of the remote node as known from the pairing.
        /// </summary>
        public NodeDescription             RemoteNodeDescription
            => Pairing.RemoteNodeDescription;

        /// <summary>
        /// Whether the local node is the communication server of this session (the WebSocket server).
        /// </summary>
        public Boolean                     IsCommunicationServer
            => Pairing.IsCommunicationServer;

        /// <summary>
        /// The session identity when the local node is the communication server.
        /// </summary>
        public S2ConnectSessionIdentity?   Identity                 { get; }

        /// <summary>
        /// The reconnecting client keeping the session alive when the local node is the communication client.
        /// </summary>
        public ReconnectingSessionClient?  ReconnectingClient       { get; }

        /// <summary>
        /// When the session started.
        /// </summary>
        public DateTimeOffset              StartedAt                { get; }

        /// <summary>
        /// The state of the S2 session.
        /// </summary>
        public S2SessionState              State
            => Session.State;

        /// <summary>
        /// The active control type of the S2 session, when one is activated.
        /// </summary>
        public ControlType?                ActiveControlType
            => Session.ActiveControlType;

        /// <summary>
        /// Whether the S2 session is connected.
        /// </summary>
        public Boolean                     IsConnected
            => Session.IsConnected;

        /// <summary>
        /// The ResourceManagerDetails received or sent on this session, when known.
        /// </summary>
        public ResourceManagerDetails?     ResourceManagerDetails
            => Session.Registry.ResourceManagerDetails;

        #endregion

        #region Constructor(s)

        internal S2NodeSession(AS2Node                     Node,
                               S2Session                   Session,
                               Pairing                     Pairing,
                               HostedNode                  LocalNode,
                               DateTimeOffset              StartedAt,
                               S2ConnectSessionIdentity?   Identity             = null,
                               ReconnectingSessionClient?  ReconnectingClient   = null)
        {

            this.Node                = Node;
            this.Session             = Session;
            this.Pairing             = Pairing;
            this.LocalNode           = LocalNode;
            this.StartedAt           = StartedAt;
            this.Identity            = Identity;
            this.ReconnectingClient  = ReconnectingClient;

        }

        #endregion


        #region SendAsync(Message, CancellationToken = default)

        /// <summary>
        /// Send the given message without waiting for its ReceptionStatus.
        /// </summary>
        /// <param name="Message">An S2 message.</param>
        /// <param name="CancellationToken">A token to cancel the sending.</param>
        public Task<S2SendResult> SendAsync(IS2Message         Message,
                                            CancellationToken  CancellationToken   = default)

            => Session.SendAsync(Message, CancellationToken);

        #endregion

        #region SendAndAwaitReceptionStatusAsync(Message, CancellationToken = default)

        /// <summary>
        /// Send the given message and wait for its ReceptionStatus.
        /// </summary>
        /// <param name="Message">An S2 message with an identification.</param>
        /// <param name="CancellationToken">A token to cancel the sending.</param>
        public Task<S2SendOutcome> SendAndAwaitReceptionStatusAsync(IS2MessageWithId   Message,
                                                                    CancellationToken  CancellationToken   = default)

            => Session.SendAndAwaitReceptionStatusAsync(Message, CancellationToken);

        #endregion

        #region CloseAsync(Reason, CancellationToken = default)

        /// <summary>
        /// Close the session (and stop its reconnecting client, when the local node is the communication client).
        /// </summary>
        /// <param name="Reason">The reason of the close.</param>
        /// <param name="CancellationToken">A token to cancel the closing.</param>
        public async Task CloseAsync(S2CloseReason      Reason,
                                     CancellationToken  CancellationToken   = default)
        {

            if (ReconnectingClient is not null)
                await ReconnectingClient.StopAsync().ConfigureAwait(false);

            await Session.CloseAsync(Reason, CancellationToken).ConfigureAwait(false);

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"session {Session.Id} of {LocalNode.Id} with {RemoteNodeId} ({(IsCommunicationServer ? "server" : "client")}, {State}{(ActiveControlType.HasValue ? $", {ActiveControlType}" : "")})";

        #endregion

    }

}

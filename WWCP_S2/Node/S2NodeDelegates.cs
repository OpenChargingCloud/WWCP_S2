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
    /// The lifecycle state of an S2 node.
    /// </summary>
    public enum S2NodeState
    {

        /// <summary>
        /// The node was created but not started.
        /// </summary>
        Created,

        /// <summary>
        /// The node is starting its servers and loops.
        /// </summary>
        Starting,

        /// <summary>
        /// The node is running.
        /// </summary>
        Running,

        /// <summary>
        /// The node is stopping: withdrawing its advertisement, closing sessions and stopping its servers.
        /// </summary>
        Stopping,

        /// <summary>
        /// The node is stopped.
        /// </summary>
        Stopped

    }


    /// <summary>
    /// A delegate called whenever the state of a node changed.
    /// </summary>
    public delegate Task OnS2NodeStateChangedDelegate   (DateTimeOffset          Timestamp,
                                                         AS2Node                 Sender,
                                                         S2NodeState             OldState,
                                                         S2NodeState             NewState);

    /// <summary>
    /// A delegate called whenever a pairing was completed (as pairing server or client).
    /// </summary>
    public delegate Task OnS2NodePairedDelegate         (DateTimeOffset          Timestamp,
                                                         AS2Node                 Sender,
                                                         Pairing                 Pairing,
                                                         IReadOnlyList<Pairing>  SupersededPairings);

    /// <summary>
    /// A delegate called whenever a pairing ended (unpaired locally or by the remote node).
    /// </summary>
    public delegate Task OnS2NodeUnpairedDelegate       (DateTimeOffset          Timestamp,
                                                         AS2Node                 Sender,
                                                         Pairing                 Pairing,
                                                         Boolean                 ByRemoteNode);

    /// <summary>
    /// A delegate called whenever an S2 session with a paired node started.
    /// </summary>
    public delegate Task OnS2NodeSessionStartedDelegate (DateTimeOffset          Timestamp,
                                                         AS2Node                 Sender,
                                                         S2NodeSession           Session);

    /// <summary>
    /// A delegate called whenever an S2 session with a paired node ended.
    /// </summary>
    public delegate Task OnS2NodeSessionEndedDelegate   (DateTimeOffset          Timestamp,
                                                         AS2Node                 Sender,
                                                         S2NodeSession           Session,
                                                         S2CloseReason           Reason);

    /// <summary>
    /// A delegate called whenever a control type was activated or deactivated on a session.
    /// </summary>
    public delegate Task OnS2NodeControlTypeDelegate    (DateTimeOffset          Timestamp,
                                                         AS2Node                 Sender,
                                                         S2NodeSession           Session,
                                                         ControlType?            ControlType);

    /// <summary>
    /// A delegate called for a received S2 message of the given type.
    /// </summary>
    public delegate Task OnS2NodeMessageDelegate<in TMessage>(DateTimeOffset     Timestamp,
                                                              AS2Node            Sender,
                                                              S2NodeSession      Session,
                                                              TMessage           Message)
        where TMessage : IS2Message;

}

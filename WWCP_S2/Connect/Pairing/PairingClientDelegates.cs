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

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The pairing client started a pairing attempt (requestPairing is about to be sent).
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The pairing client.</param>
    /// <param name="LocalNode">The local node that pairs.</param>
    /// <param name="Target">The targeted node at the server.</param>
    public delegate Task OnPairingClientStartedDelegate       (DateTimeOffset          Timestamp,
                                                               PairingClient           Sender,
                                                               HostedNode              LocalNode,
                                                               PairingTarget           Target);

    /// <summary>
    /// A pairing attempt of the pairing client ended.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The pairing client.</param>
    /// <param name="LocalNode">The local node that paired.</param>
    /// <param name="Result">The result.</param>
    public delegate Task OnPairingClientCompletedDelegate     (DateTimeOffset          Timestamp,
                                                               PairingClient           Sender,
                                                               HostedNode              LocalNode,
                                                               PairingClientResult     Result);


    /// <summary>
    /// The long-polling server sent an action for a local node.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The long-polling client.</param>
    /// <param name="LocalNode">The local node the action is for.</param>
    /// <param name="Action">The action.</param>
    public delegate Task OnLongPollingActionDelegate          (DateTimeOffset          Timestamp,
                                                               LongPollingClient       Sender,
                                                               HostedNode              LocalNode,
                                                               WaitForPairingAction    Action);

    /// <summary>
    /// The long-polling server asked a local node to prepare pairing (e.g. show its pairing code).
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The long-polling client.</param>
    /// <param name="LocalNode">The local node.</param>
    public delegate Task OnLongPollingPreparePairingDelegate  (DateTimeOffset          Timestamp,
                                                               LongPollingClient       Sender,
                                                               HostedNode              LocalNode);

    /// <summary>
    /// The long-polling server cancelled the prepare pairing signal for a local node.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The long-polling client.</param>
    /// <param name="LocalNode">The local node.</param>
    public delegate Task OnLongPollingCancelPreparePairingDelegate(DateTimeOffset      Timestamp,
                                                                   LongPollingClient   Sender,
                                                                   HostedNode          LocalNode);

    /// <summary>
    /// The long-polling server asked a local node to initiate pairing, but the node has no
    /// valid pairing token; the next request reports NoValidTokenOnPairingClient.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The long-polling client.</param>
    /// <param name="LocalNode">The local node.</param>
    public delegate Task OnLongPollingMissingTokenDelegate    (DateTimeOffset          Timestamp,
                                                               LongPollingClient       Sender,
                                                               HostedNode              LocalNode);

    /// <summary>
    /// A pairing initiated by the long-polling server ended.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The long-polling client.</param>
    /// <param name="LocalNode">The local node.</param>
    /// <param name="Result">The result.</param>
    public delegate Task OnLongPollingPairingCompletedDelegate(DateTimeOffset          Timestamp,
                                                               LongPollingClient       Sender,
                                                               HostedNode              LocalNode,
                                                               PairingClientResult     Result);

    /// <summary>
    /// The long-polling client stopped.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The long-polling client.</param>
    /// <param name="Reason">Why it stopped.</param>
    /// <param name="StatusCode">The HTTP status code that caused the stop, when any.</param>
    public delegate Task OnLongPollingStoppedDelegate         (DateTimeOffset          Timestamp,
                                                               LongPollingClient       Sender,
                                                               LongPollingStopReason   Reason,
                                                               HTTPStatusCode?         StatusCode);

}

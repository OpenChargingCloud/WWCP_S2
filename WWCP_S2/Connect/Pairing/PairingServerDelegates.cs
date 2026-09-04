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

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// A pairing attempt was started (step 3, the pairingAttemptId was issued).
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The pairing server.</param>
    /// <param name="Attempt">The pairing attempt.</param>
    public delegate Task OnPairingAttemptStartedDelegate            (DateTimeOffset                Timestamp,
                                                                     PairingServerAPI              Sender,
                                                                     PairingAttempt                Attempt);

    /// <summary>
    /// A pairing attempt succeeded, failed or timed out; the attempt carries the outcome
    /// and <see cref="PairingAttempt.ToJSON"/> is its audit record.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The pairing server.</param>
    /// <param name="Attempt">The pairing attempt.</param>
    public delegate Task OnPairingAttemptCompletedDelegate          (DateTimeOffset                Timestamp,
                                                                     PairingServerAPI              Sender,
                                                                     PairingAttempt                Attempt);

    /// <summary>
    /// A pairing was completed successfully and stored. When the same pair was paired before,
    /// the replaced pairing is given (the current communication session should be terminated,
    /// S2 Connect 1.0.0, "Pairing process"); when an RM node is now paired with another CEM,
    /// the superseded pairings must be unpaired by the node layer ("the initial pairing is
    /// automatically unpaired ... after the new pairing is successfully completed").
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The pairing server.</param>
    /// <param name="Attempt">The pairing attempt.</param>
    /// <param name="Pairing">The new pairing.</param>
    /// <param name="ReplacedPairing">The earlier pairing of the same pair, or null.</param>
    /// <param name="SupersededPairings">The other pairings of an RM node that must be unpaired now.</param>
    public delegate Task OnPairingCompletedDelegate                 (DateTimeOffset                Timestamp,
                                                                     PairingServerAPI              Sender,
                                                                     PairingAttempt                Attempt,
                                                                     Pairing                       Pairing,
                                                                     Pairing?                      ReplacedPairing,
                                                                     IReadOnlyList<Pairing>        SupersededPairings);

    /// <summary>
    /// A LAN client sent the prepare pairing signal (e.g. show the pairing code in the UI).
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The pairing server.</param>
    /// <param name="Request">The request.</param>
    /// <param name="ServerNode">The targeted hosted node, or null when it is unknown.</param>
    /// <param name="RemoteAddress">The optional address of the client.</param>
    public delegate Task OnPreparePairingDelegate                   (DateTimeOffset                Timestamp,
                                                                     PairingServerAPI              Sender,
                                                                     PreparePairingRequest         Request,
                                                                     HostedNode?                   ServerNode,
                                                                     String?                       RemoteAddress);

    /// <summary>
    /// A LAN client sent the cancel prepare pairing signal (e.g. hide the pairing code again).
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The pairing server.</param>
    /// <param name="Request">The request.</param>
    /// <param name="ServerNode">The targeted hosted node, or null when it is unknown.</param>
    /// <param name="RemoteAddress">The optional address of the client.</param>
    public delegate Task OnCancelPreparePairingDelegate             (DateTimeOffset                Timestamp,
                                                                     PairingServerAPI              Sender,
                                                                     CancelPreparePairingRequest   Request,
                                                                     HostedNode?                   ServerNode,
                                                                     String?                       RemoteAddress);


    /// <summary>
    /// A client node polled the long-polling server.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The long-polling server.</param>
    /// <param name="ClientNode">The client node.</param>
    /// <param name="IsNew">Whether the node polled for the first time.</param>
    public delegate Task OnLongPollingClientNodeSeenDelegate        (DateTimeOffset                Timestamp,
                                                                     LongPollingServer             Sender,
                                                                     LongPollingClientNode         ClientNode,
                                                                     Boolean                       IsNew);

    /// <summary>
    /// A client node sent its node and endpoint descriptions.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The long-polling server.</param>
    /// <param name="ClientNode">The client node.</param>
    public delegate Task OnLongPollingClientNodeDescriptionDelegate (DateTimeOffset                Timestamp,
                                                                     LongPollingServer             Sender,
                                                                     LongPollingClientNode         ClientNode);

    /// <summary>
    /// A client node reported an error, e.g. NoValidTokenOnPairingClient.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The long-polling server.</param>
    /// <param name="ClientNode">The client node.</param>
    /// <param name="Error">The reported error.</param>
    public delegate Task OnLongPollingClientNodeErrorDelegate       (DateTimeOffset                Timestamp,
                                                                     LongPollingServer             Sender,
                                                                     LongPollingClientNode         ClientNode,
                                                                     WaitForPairingError           Error);

}

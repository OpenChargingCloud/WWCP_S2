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

namespace cloud.charging.open.protocols.S2.Connect
{

    // Server side

    /// <summary>
    /// A communication client started session initiation and received a pending access token (step 3).
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The session initiation server.</param>
    /// <param name="Pairing">The pairing the session belongs to.</param>
    /// <param name="Response">The response sent to the client.</param>
    public delegate Task OnSessionInitiatedDelegate            (DateTimeOffset                 Timestamp,
                                                                SessionInitiationServerAPI     Sender,
                                                                Pairing                        Pairing,
                                                                InitiateSessionResponse        Response);

    /// <summary>
    /// A pending access token was confirmed and activated, and a communication token issued (steps 6 and 7).
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The session initiation server.</param>
    /// <param name="Pairing">The updated pairing (with the new active access token).</param>
    /// <param name="Identity">The identity attached to the communication token.</param>
    /// <param name="Details">The communication details sent to the client.</param>
    public delegate Task OnAccessTokenActivatedDelegate        (DateTimeOffset                 Timestamp,
                                                                SessionInitiationServerAPI     Sender,
                                                                Pairing                        Pairing,
                                                                S2ConnectSessionIdentity       Identity,
                                                                CommunicationDetails           Details);

    /// <summary>
    /// A pairing was removed: by the communication client via POST /unpair, or locally by the
    /// communication server (which must then send SessionRequest RECONNECT and close the session).
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The session initiation server.</param>
    /// <param name="Pairing">The removed pairing.</param>
    /// <param name="ByRemoteNode">Whether the remote node (the communication client) took the initiative.</param>
    public delegate Task OnUnpairedDelegate                    (DateTimeOffset                 Timestamp,
                                                                SessionInitiationServerAPI     Sender,
                                                                Pairing                        Pairing,
                                                                Boolean                        ByRemoteNode);


    // Client side

    /// <summary>
    /// A session initiation or unpairing of the session initiation client ended.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The session initiation client.</param>
    /// <param name="LocalNodeId">The identification of the local node.</param>
    /// <param name="ServerNodeId">The identification of the node at the communication server.</param>
    /// <param name="Result">The result.</param>
    public delegate Task OnSessionInitiationCompletedDelegate  (DateTimeOffset                 Timestamp,
                                                                SessionInitiationClient        Sender,
                                                                Node_Id                        LocalNodeId,
                                                                Node_Id                        ServerNodeId,
                                                                SessionInitiationClientResult  Result);

    /// <summary>
    /// The reconnecting client opened an S2 session.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The reconnecting client.</param>
    /// <param name="Session">The S2 Connect session.</param>
    public delegate Task OnReconnectingSessionStartedDelegate  (DateTimeOffset                 Timestamp,
                                                                ReconnectingSessionClient      Sender,
                                                                S2ConnectSession               Session);

    /// <summary>
    /// An S2 session of the reconnecting client ended.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The reconnecting client.</param>
    /// <param name="Session">The S2 Connect session.</param>
    /// <param name="Reason">Why the session ended.</param>
    public delegate Task OnReconnectingSessionEndedDelegate    (DateTimeOffset                 Timestamp,
                                                                ReconnectingSessionClient      Sender,
                                                                S2ConnectSession               Session,
                                                                S2CloseReason                  Reason);

    /// <summary>
    /// A connection attempt of the reconnecting client failed; the next attempt follows after the delay.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The reconnecting client.</param>
    /// <param name="Result">The result of the failed attempt.</param>
    /// <param name="Attempt">The number of the failed attempt (starting at 0).</param>
    /// <param name="Delay">The delay before the next attempt.</param>
    public delegate Task OnReconnectAttemptFailedDelegate      (DateTimeOffset                 Timestamp,
                                                                ReconnectingSessionClient      Sender,
                                                                SessionInitiationClientResult  Result,
                                                                Int32                          Attempt,
                                                                TimeSpan                       Delay);

    /// <summary>
    /// The reconnecting client stopped.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the event.</param>
    /// <param name="Sender">The reconnecting client.</param>
    /// <param name="Reason">Why it stopped.</param>
    public delegate Task OnReconnectingClientStoppedDelegate   (DateTimeOffset                 Timestamp,
                                                                ReconnectingSessionClient      Sender,
                                                                ReconnectStopReason            Reason);

}

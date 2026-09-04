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
    /// The outcome of sending a message and waiting for its ReceptionStatus.
    /// </summary>
    /// <param name="SendResult">The transport result.</param>
    /// <param name="ReceptionStatus">The received ReceptionStatus, when any.</param>
    /// <param name="TimedOut">Whether no ReceptionStatus arrived within the timeout.</param>
    public sealed record S2SendOutcome(S2SendResult      SendResult,
                                       ReceptionStatus?  ReceptionStatus,
                                       Boolean           TimedOut)
    {

        /// <summary>
        /// Whether the message was sent and acknowledged with OK.
        /// </summary>
        public Boolean IsOK
            => SendResult.IsSuccess && ReceptionStatus?.IsOK == true;

    }


    /// <summary>
    /// A delegate called for every received S2 message after it was processed (logging only).
    /// </summary>
    public delegate Task OnS2MessageReceivedDelegate          (DateTimeOffset        Timestamp,
                                                               S2Session             Session,
                                                               IS2Message            Message,
                                                               ReceptionStatusValue  Status);

    /// <summary>
    /// A delegate called for every sent S2 message (logging only).
    /// </summary>
    public delegate Task OnS2MessageSentDelegate              (DateTimeOffset        Timestamp,
                                                               S2Session             Session,
                                                               IS2Message            Message,
                                                               S2SendResult          Result);

    /// <summary>
    /// A delegate called when the session state or the active control type changed.
    /// </summary>
    public delegate Task OnS2SessionStateChangedDelegate      (DateTimeOffset        Timestamp,
                                                               S2Session             Session,
                                                               S2SessionState        OldState,
                                                               S2SessionState        NewState,
                                                               ControlType?          ActiveControlType);

    /// <summary>
    /// A delegate called when a ReceptionStatus arrived that no sent message awaits.
    /// </summary>
    public delegate Task OnS2UnmatchedReceptionStatusDelegate (DateTimeOffset        Timestamp,
                                                               S2Session             Session,
                                                               ReceptionStatus       ReceptionStatus);

    /// <summary>
    /// A delegate called when the peer sent a SessionRequest (RECONNECT or TERMINATE).
    /// </summary>
    public delegate Task OnS2SessionRequestDelegate           (DateTimeOffset        Timestamp,
                                                               S2Session             Session,
                                                               SessionRequest        SessionRequest);

    /// <summary>
    /// A delegate called once when the session was closed.
    /// </summary>
    public delegate Task OnS2SessionClosedDelegate            (DateTimeOffset        Timestamp,
                                                               S2Session             Session,
                                                               S2CloseReason         Reason);

}

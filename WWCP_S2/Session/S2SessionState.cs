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
    /// How the session was established (PLAN.md D7).
    /// </summary>
    public enum S2SessionMode
    {

        /// <summary>
        /// S2 Connect: pairing and session initiation negotiated everything; the
        /// Handshake and HandshakeResponse messages must not be sent.
        /// </summary>
        S2Connect,

        /// <summary>
        /// Plain S2 JSON over a WebSocket: the session starts with the Handshake exchange
        /// (as s2-python and the documentation examples do).
        /// </summary>
        Plain

    }


    /// <summary>
    /// The state of an S2 session (S2 Connect 1.0.0, "State of communication", extended by
    /// the plain-mode handshake states, PLAN.md §3.4).
    /// </summary>
    public enum S2SessionState
    {

        /// <summary>
        /// The session object exists but has not been started.
        /// </summary>
        Created,

        /// <summary>
        /// Plain mode only: the Handshake exchange has not been completed yet.
        /// </summary>
        AwaitingHandshake,

        /// <summary>
        /// The session is established and no control type is active. ResourceManagerDetails,
        /// PowerMeasurement, PowerForecast, SelectControlType and SessionRequest can be exchanged.
        /// </summary>
        WebSocketConnected,

        /// <summary>
        /// A control type is active; its messages can be exchanged in addition.
        /// </summary>
        ControlTypeActivated,

        /// <summary>
        /// The session has ended.
        /// </summary>
        Disconnected

    }

}

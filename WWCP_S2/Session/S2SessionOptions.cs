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
    /// The options of an S2 session (PLAN.md §3.7).
    /// </summary>
    public sealed record S2SessionOptions
    {

        #region Properties

        /// <summary>
        /// The role of the local node.
        /// </summary>
        public required EnergyManagementRole   Role                      { get; init; }

        /// <summary>
        /// Whether the session was established through S2 Connect (no Handshake) or is a
        /// plain S2-JSON-over-WebSocket session starting with the Handshake exchange.
        /// </summary>
        public S2SessionMode                   Mode                      { get; init; } = S2SessionMode.S2Connect;

        /// <summary>
        /// The S2 JSON versions supported by the local node, most preferred first. In plain
        /// mode the CEM selects the first entry of the RM's list that is contained in this list;
        /// in S2 Connect mode the negotiated version is given as <see cref="NegotiatedVersion"/>.
        /// </summary>
        public IReadOnlyList<String>           SupportedVersions         { get; init; } = Version.S2JSONVersions;

        /// <summary>
        /// The S2 JSON version negotiated by S2 Connect session initiation (S2 Connect mode only).
        /// </summary>
        public String?                         NegotiatedVersion         { get; init; }

        /// <summary>
        /// How strictly received messages are parsed.
        /// </summary>
        public S2ParserOptions                 ParserOptions             { get; init; } = S2ParserOptions.Default;

        /// <summary>
        /// How long to wait for the ReceptionStatus of an own message.
        /// </summary>
        public TimeSpan                        ReceptionStatusTimeout    { get; init; } = S2ConnectDefaults.ReceptionStatusTimeout;

        /// <summary>
        /// Whether a missing ReceptionStatus (timeout) closes the session. Off by default:
        /// s2-python sends no ReceptionStatus for messages it has no handler for.
        /// </summary>
        public Boolean                         CloseOnReceptionTimeout   { get; init; }

        /// <summary>
        /// The capacity of the inbound message channel; a full channel makes the medium wait
        /// (backpressure) instead of dropping messages.
        /// </summary>
        public Int32                           InboundChannelCapacity    { get; init; } = 64;

        /// <summary>
        /// Whether the CEM sends its own Handshake immediately when the session starts (plain mode).
        /// </summary>
        public Boolean                         SendHandshakeOnStart      { get; init; } = true;

        #endregion

        #region Validate()

        /// <summary>
        /// Validate the options.
        /// </summary>
        public void Validate()
        {

            if (SupportedVersions.Count == 0)
                throw new ArgumentException("At least one supported S2 JSON version is required!", nameof(SupportedVersions));

            if (Mode == S2SessionMode.S2Connect && String.IsNullOrWhiteSpace(NegotiatedVersion))
                throw new ArgumentException("An S2 Connect session needs the negotiated S2 JSON version!", nameof(NegotiatedVersion));

            if (ReceptionStatusTimeout <= TimeSpan.Zero)
                throw new ArgumentException("The reception status timeout must be positive!", nameof(ReceptionStatusTimeout));

            if (InboundChannelCapacity < 1)
                throw new ArgumentException("The inbound channel capacity must be at least 1!", nameof(InboundChannelCapacity));

        }

        #endregion

    }

}

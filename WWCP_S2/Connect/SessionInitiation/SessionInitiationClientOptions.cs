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
    /// The options of a session initiation client (PLAN.md §3.7).
    /// </summary>
    public sealed record SessionInitiationClientOptions
    {

        #region Properties

        /// <summary>
        /// The versions of the session initiation API this client implements (default: v1).
        /// </summary>
        public IReadOnlyList<String>  SupportedAPIVersions                 { get; init; } = Version.S2ConnectAPIVersions;

        /// <summary>
        /// The request timeout of every request (default: 10 seconds).
        /// </summary>
        public TimeSpan               RequestTimeout                       { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The delay before a request answered with 503 is repeated (default: 1 second;
        /// a Retry-After header of the response takes precedence).
        /// </summary>
        public TimeSpan               ServiceUnavailableRetryDelay         { get; init; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// How often a request answered with 503 is repeated before the procedure fails (default: 3).
        /// </summary>
        public Int32                  MaxServiceUnavailableRetries         { get; init; } = 3;

        /// <summary>
        /// Whether the node and endpoint descriptions of the local node are sent with every
        /// initiateSession request (default: false, they are only sent to update the server).
        /// </summary>
        public Boolean                SendDescriptions                     { get; init; }

        /// <summary>
        /// Whether the local pairing is removed (with a tombstone) when the server answers
        /// NoLongerPaired (default: true).
        /// </summary>
        public Boolean                RemovePairingWhenNoLongerPaired      { get; init; } = true;

        /// <summary>
        /// Whether self-signed server certificates are accepted (default: true until the
        /// pinned CA of the pairing is enforced in Phase 11).
        /// </summary>
        public Boolean                AcceptSelfSignedCertificates         { get; init; } = true;

        /// <summary>
        /// The parser options for response bodies (default: <see cref="S2ParserOptions.Default"/>).
        /// </summary>
        public S2ParserOptions        ParserOptions                        { get; init; } = S2ParserOptions.Default;

        /// <summary>
        /// The WebSocket ping interval of opened sessions (default: 30 seconds).
        /// </summary>
        public TimeSpan               WebSocketPingInterval                { get; init; } = S2ConnectDefaults.WebSocketPingInterval;

        #endregion

        #region (static) Default

        /// <summary>
        /// The default options.
        /// </summary>
        public static SessionInitiationClientOptions Default { get; } = new ();

        #endregion


        #region Validate()

        /// <summary>
        /// Validate the options; throws when a value is out of range.
        /// </summary>
        public void Validate()
        {

            if (SupportedAPIVersions is null || SupportedAPIVersions.Count == 0)
                throw new ArgumentException("At least one API version must be supported!", nameof(SupportedAPIVersions));

            if (RequestTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(RequestTimeout), "The request timeout must be positive!");

            if (ServiceUnavailableRetryDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ServiceUnavailableRetryDelay), "The retry delay must not be negative!");

            if (MaxServiceUnavailableRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(MaxServiceUnavailableRetries), "The number of retries must not be negative!");

            if (WebSocketPingInterval <= TimeSpan.Zero || WebSocketPingInterval > S2ConnectDefaults.MaxWebSocketPingInterval)
                throw new ArgumentOutOfRangeException(nameof(WebSocketPingInterval), $"The WebSocket ping interval must be positive and at most {S2ConnectDefaults.MaxWebSocketPingInterval.TotalSeconds} seconds!");

            ArgumentNullException.ThrowIfNull(ParserOptions, nameof(ParserOptions));

        }

        #endregion

    }

}

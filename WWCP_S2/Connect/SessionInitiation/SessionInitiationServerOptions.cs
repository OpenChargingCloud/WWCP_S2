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
    /// The options of a session initiation server (PLAN.md §3.7). The defaults are the
    /// normative values of S2 Connect 1.0.0 as collected in <see cref="S2ConnectDefaults"/>.
    /// </summary>
    public sealed record SessionInitiationServerOptions
    {

        #region Properties

        /// <summary>
        /// How long a pending access token may be confirmed (default: 15 seconds;
        /// S2 Connect 1.0.0, "6. Activate new accessToken").
        /// </summary>
        public TimeSpan         PendingAccessTokenLifetime    { get; init; } = S2ConnectDefaults.PendingAccessTokenLifetime;

        /// <summary>
        /// The validity of an issued communication (WebSocket) token (default: 30 seconds).
        /// </summary>
        public TimeSpan         CommunicationTokenLifetime    { get; init; } = S2ConnectDefaults.CommunicationTokenLifetime;

        /// <summary>
        /// Whether the server includes its node and endpoint descriptions in every
        /// initiateSession response (default: false, they are only sent to update the client).
        /// </summary>
        public Boolean          AlwaysSendDescriptions        { get; init; }

        /// <summary>
        /// The parser options for request bodies (default: <see cref="S2ParserOptions.Default"/>).
        /// </summary>
        public S2ParserOptions  ParserOptions                 { get; init; } = S2ParserOptions.Default;

        /// <summary>
        /// Whether the additionalInfo of a CommunicationDetailsErrorMessage carries details
        /// such as parser errors (default: true).
        /// </summary>
        public Boolean          IncludeErrorDetails           { get; init; } = true;

        #endregion

        #region (static) Default

        /// <summary>
        /// The default options.
        /// </summary>
        public static SessionInitiationServerOptions Default { get; } = new ();

        #endregion


        #region Validate()

        /// <summary>
        /// Validate the options; throws when a value is out of range.
        /// </summary>
        public void Validate()
        {

            if (PendingAccessTokenLifetime <= TimeSpan.Zero || PendingAccessTokenLifetime > S2ConnectDefaults.PendingAccessTokenLifetime)
                throw new ArgumentOutOfRangeException(nameof(PendingAccessTokenLifetime), $"The pending access token lifetime must be positive and at most {S2ConnectDefaults.PendingAccessTokenLifetime.TotalSeconds} seconds!");

            if (CommunicationTokenLifetime <= TimeSpan.Zero || CommunicationTokenLifetime > S2ConnectDefaults.CommunicationTokenLifetime)
                throw new ArgumentOutOfRangeException(nameof(CommunicationTokenLifetime), $"The communication token lifetime must be positive and at most {S2ConnectDefaults.CommunicationTokenLifetime.TotalSeconds} seconds!");

            ArgumentNullException.ThrowIfNull(ParserOptions, nameof(ParserOptions));

        }

        #endregion

    }

}

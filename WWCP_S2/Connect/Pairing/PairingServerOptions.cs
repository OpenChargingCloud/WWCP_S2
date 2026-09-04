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
    /// The options of a pairing server (PLAN.md §3.7). The defaults are the normative values
    /// of S2 Connect 1.0.0 as collected in <see cref="S2ConnectDefaults"/>.
    /// </summary>
    public sealed record PairingServerOptions
    {

        #region Properties

        /// <summary>
        /// The mandatory delay before the response to a requestPairing request, enforced
        /// sequentially per targeted node (default: 1 second; smaller values only for tests).
        /// </summary>
        public TimeSpan                             RequestPairingDelay                 { get; init; } = S2ConnectDefaults.RequestPairingDelay;

        /// <summary>
        /// The maximum duration of a pairing attempt, measured from the generation of the
        /// pairingAttemptId (default: 15 seconds).
        /// </summary>
        public TimeSpan                             PairingAttemptTimeout               { get; init; } = S2ConnectDefaults.PairingAttemptTimeout;

        /// <summary>
        /// How long a completed or failed attempt is remembered for replays of duplicate
        /// requests after its completion (default: 15 seconds).
        /// </summary>
        public TimeSpan                             CompletedAttemptRetention           { get; init; } = S2ConnectDefaults.PairingAttemptTimeout;

        /// <summary>
        /// The maximal number of requestPairing requests waiting for the per-node delay before
        /// the server answers 503 (default: 4).
        /// </summary>
        public Int32                                MaxQueuedPairingAttemptsPerNode     { get; init; } = S2ConnectDefaults.MaxQueuedPairingAttemptsPerNode;

        /// <summary>
        /// The HMAC hashing algorithms this server accepts, most preferred first (default: SHA256).
        /// </summary>
        public IReadOnlyList<HmacHashingAlgorithm>  SupportedHmacHashingAlgorithms      { get; init; } = [ HmacHashingAlgorithm.SHA256 ];

        /// <summary>
        /// The deployment assumed for a pairing client whose endpoint description does not
        /// state one (default: null, such requests are rejected with the error "Other").
        /// </summary>
        public Deployment?                          DefaultClientDeployment             { get; init; }

        /// <summary>
        /// Whether the LAN-only operations (GET /endpoint, GET /nodes, POST /preparePairing,
        /// POST /cancelPreparePairing, POST /waitForPairing) are served (default: null, i.e.
        /// only for LAN endpoints that are not WAN pairing servers of LAN endpoints).
        /// WAN endpoints answer 404.
        /// </summary>
        public Boolean?                             EnableLANOperations                 { get; init; }

        /// <summary>
        /// Whether long-polling (POST /waitForPairing) is served (default: null, i.e. whenever
        /// the LAN-only operations are). A LAN endpoint with LAN operations but without
        /// long-polling answers 400.
        /// </summary>
        public Boolean?                             EnableLongPolling                   { get; init; }

        /// <summary>
        /// The maximum time a waitForPairing request is kept open (default: 25 seconds).
        /// </summary>
        public TimeSpan                             LongPollingTimeout                  { get; init; } = S2ConnectDefaults.LongPollingServerTimeout;

        /// <summary>
        /// The maximal number of concurrently hanging waitForPairing requests before the
        /// server answers 503 (default: 64).
        /// </summary>
        public Int32                                MaxHangingLongPollingRequests       { get; init; } = 64;

        /// <summary>
        /// Whether the long-polling server asks a client node for its descriptions
        /// (action "sendNodeDescription") as soon as it polls without them (default: true).
        /// </summary>
        public Boolean                              AutoRequestNodeDescriptions         { get; init; } = true;

        /// <summary>
        /// The parser options for request bodies (default: <see cref="S2ParserOptions.Default"/>).
        /// </summary>
        public S2ParserOptions                      ParserOptions                       { get; init; } = S2ParserOptions.Default;

        /// <summary>
        /// Whether the additionalInfo of a PairingResponseErrorMessage carries details such as
        /// parser errors (default: true).
        /// </summary>
        public Boolean                              IncludeErrorDetails                 { get; init; } = true;

        #endregion

        #region (static) Default

        /// <summary>
        /// The default options.
        /// </summary>
        public static PairingServerOptions Default { get; } = new ();

        #endregion


        #region Validate()

        /// <summary>
        /// Validate the options; throws when a value is out of range.
        /// </summary>
        public void Validate()
        {

            if (RequestPairingDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(RequestPairingDelay), "The requestPairing delay must not be negative!");

            if (PairingAttemptTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(PairingAttemptTimeout), "The pairing attempt timeout must be positive!");

            if (CompletedAttemptRetention < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(CompletedAttemptRetention), "The retention of completed attempts must not be negative!");

            if (MaxQueuedPairingAttemptsPerNode < 0)
                throw new ArgumentOutOfRangeException(nameof(MaxQueuedPairingAttemptsPerNode), "The maximal number of queued pairing attempts must not be negative!");

            if (SupportedHmacHashingAlgorithms is null || SupportedHmacHashingAlgorithms.Count == 0)
                throw new ArgumentException("At least one HMAC hashing algorithm must be supported!", nameof(SupportedHmacHashingAlgorithms));

            if (SupportedHmacHashingAlgorithms.Any(algorithm => algorithm != HmacHashingAlgorithm.SHA256))
                throw new ArgumentException("Only SHA256 is implemented as HMAC hashing algorithm!", nameof(SupportedHmacHashingAlgorithms));

            if (DefaultClientDeployment.HasValue && DefaultClientDeployment.Value != Deployment.LAN && DefaultClientDeployment.Value != Deployment.WAN)
                throw new ArgumentException("The default client deployment must be LAN or WAN!", nameof(DefaultClientDeployment));

            if (EnableLongPolling == true && EnableLANOperations == false)
                throw new ArgumentException("Long-polling requires the LAN-only operations!", nameof(EnableLongPolling));

            if (LongPollingTimeout <= TimeSpan.Zero || LongPollingTimeout > S2ConnectDefaults.LongPollingServerTimeout)
                throw new ArgumentOutOfRangeException(nameof(LongPollingTimeout), $"The long-polling timeout must be positive and at most {S2ConnectDefaults.LongPollingServerTimeout.TotalSeconds} seconds!");

            if (MaxHangingLongPollingRequests < 1)
                throw new ArgumentOutOfRangeException(nameof(MaxHangingLongPollingRequests), "At least one hanging long-polling request must be allowed!");

            ArgumentNullException.ThrowIfNull(ParserOptions, nameof(ParserOptions));

        }

        #endregion

    }

}

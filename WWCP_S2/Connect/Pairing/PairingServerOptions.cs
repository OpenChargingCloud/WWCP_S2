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

        #region Data

        /// <summary>
        /// The default maximal size of a request body: 64 KiB. The largest body the pairing
        /// interaction knows is a requestPairing request with the endpoint and node
        /// descriptions of the client, which is a few kilobytes.
        /// </summary>
        public const Int32              DefaultMaxRequestBodySize      = 64 * 1024;

        /// <summary>
        /// The default number of requests one remote address may send per
        /// <see cref="DefaultRateLimitRefillPeriod"/>: 300, i.e. five per second sustained.
        /// A pairing interaction is four requests, a long-polling client polls about
        /// three times per minute.
        /// </summary>
        public const Int32              DefaultRateLimitCapacity       = 300;

        /// <summary>
        /// The default period within which the request budget of a remote address refills
        /// completely: one minute.
        /// </summary>
        public static readonly TimeSpan DefaultRateLimitRefillPeriod   = TimeSpan.FromMinutes(1);

        #endregion

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


        /// <summary>
        /// The maximal size of a request body in bytes (default: 64 KiB). A larger body is
        /// answered "413 Request Entity Too Large" without being parsed. The HTTP server has
        /// its own, coarser limit; this one bounds what the pairing operations accept.
        /// </summary>
        public Int32                                MaxRequestBodySize                  { get; init; } = DefaultMaxRequestBodySize;

        /// <summary>
        /// Whether the requests of a remote address are rate limited (default: true).
        /// This is denial-of-service protection; the mandatory sequential handling of the
        /// pairing attempts of a node is independent of it and always in force.
        ///
        /// <para>
        /// The remote address is the peer of the TCP connection. Behind a reverse proxy or a
        /// NAT every client therefore shares one budget, and a forwarded-for header is not
        /// trusted, because anyone could set it and get a fresh budget per request. Such a
        /// deployment either raises <see cref="RateLimitCapacity"/> or rate limits at the proxy
        /// and disables this limiter.
        /// </para>
        /// </summary>
        public Boolean                              EnableRateLimiting                  { get; init; } = true;

        /// <summary>
        /// The number of requests one remote address may send before it has to wait
        /// (default: 300).
        /// </summary>
        public Int32                                RateLimitCapacity                   { get; init; } = DefaultRateLimitCapacity;

        /// <summary>
        /// The period within which the request budget of a remote address refills completely
        /// (default: one minute).
        /// </summary>
        public TimeSpan                             RateLimitRefillPeriod               { get; init; } = DefaultRateLimitRefillPeriod;

        /// <summary>
        /// The maximal number of remote addresses the rate limiter remembers (default: 10.000).
        /// Beyond it, unknown addresses are refused until an unused bucket expires.
        /// </summary>
        public Int32                                RateLimitMaxSources                 { get; init; } = S2RequestRateLimiter.DefaultMaximumBuckets;

        /// <summary>
        /// Whether a rate limited request is answered "429 Too Many Requests" instead of
        /// "503 Service Unavailable" (default: false). The specification only defines 503 for
        /// an overloaded pairing server and an S2 Connect client retries after a 503, so 503
        /// is the interoperable answer; 429 is the more precise one where clients understand it.
        /// Both carry a Retry-After header.
        /// </summary>
        public Boolean                              UseTooManyRequestsStatusCode        { get; init; }

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

            if (MaxRequestBodySize < 1)
                throw new ArgumentOutOfRangeException(nameof(MaxRequestBodySize), "The maximal request body size must be positive!");

            if (EnableRateLimiting)
            {

                if (RateLimitCapacity < 1)
                    throw new ArgumentOutOfRangeException(nameof(RateLimitCapacity), "The rate limit capacity must be positive!");

                if (RateLimitRefillPeriod <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(RateLimitRefillPeriod), "The rate limit refill period must be positive!");

                if (RateLimitMaxSources < 1)
                    throw new ArgumentOutOfRangeException(nameof(RateLimitMaxSources), "At least one remote address must be remembered!");

            }

        }

        #endregion

    }

}

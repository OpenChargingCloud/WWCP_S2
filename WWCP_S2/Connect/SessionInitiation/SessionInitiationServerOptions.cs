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

        #region Data

        /// <summary>
        /// The default maximal size of a request body: 64 KiB. The largest body of the session
        /// initiation is an initiateSession request carrying the endpoint and node descriptions
        /// of the client, which is a few kilobytes.
        /// </summary>
        public const Int32              DefaultMaxRequestBodySize      = 64 * 1024;

        /// <summary>
        /// The default number of requests one remote address may send per
        /// <see cref="DefaultRateLimitRefillPeriod"/>: 120, i.e. two per second sustained.
        /// A session is established with three requests and a reconnecting client waits at
        /// least two seconds between its attempts.
        /// </summary>
        public const Int32              DefaultRateLimitCapacity       = 120;

        /// <summary>
        /// The default period within which the request budget of a remote address refills
        /// completely: one minute.
        /// </summary>
        public static readonly TimeSpan DefaultRateLimitRefillPeriod   = TimeSpan.FromMinutes(1);

        #endregion

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


        /// <summary>
        /// The maximal size of a request body in bytes (default: 64 KiB). A larger body is
        /// answered "413 Request Entity Too Large" without being parsed.
        /// </summary>
        public Int32            MaxRequestBodySize            { get; init; } = DefaultMaxRequestBodySize;

        /// <summary>
        /// Whether the requests of a remote address are rate limited (default: true).
        ///
        /// <para>
        /// The remote address is the peer of the TCP connection. Behind a reverse proxy or a
        /// NAT every client therefore shares one budget, and a forwarded-for header is not
        /// trusted, because anyone could set it and get a fresh budget per request. Such a
        /// deployment either raises <see cref="RateLimitCapacity"/> or rate limits at the proxy
        /// and disables this limiter.
        /// </para>
        /// </summary>
        public Boolean          EnableRateLimiting            { get; init; } = true;

        /// <summary>
        /// The number of requests one remote address may send before it has to wait
        /// (default: 120).
        /// </summary>
        public Int32            RateLimitCapacity             { get; init; } = DefaultRateLimitCapacity;

        /// <summary>
        /// The period within which the request budget of a remote address refills completely
        /// (default: one minute).
        /// </summary>
        public TimeSpan         RateLimitRefillPeriod         { get; init; } = DefaultRateLimitRefillPeriod;

        /// <summary>
        /// The maximal number of remote addresses the rate limiter remembers (default: 10.000).
        /// </summary>
        public Int32            RateLimitMaxSources           { get; init; } = S2RequestRateLimiter.DefaultMaximumBuckets;

        /// <summary>
        /// Whether a rate limited request is answered "429 Too Many Requests" instead of
        /// "503 Service Unavailable" (default: false, the interoperable answer). Both carry
        /// a Retry-After header.
        /// </summary>
        public Boolean          UseTooManyRequestsStatusCode  { get; init; }

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

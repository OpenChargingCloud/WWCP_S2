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
    /// The options of a pairing client (PLAN.md §3.7). The defaults are the normative values
    /// of S2 Connect 1.0.0 as collected in <see cref="S2ConnectDefaults"/>.
    /// </summary>
    public sealed record PairingClientOptions
    {

        #region Properties

        /// <summary>
        /// The HMAC hashing algorithms this client offers, most preferred first (default: SHA256).
        /// </summary>
        public IReadOnlyList<HmacHashingAlgorithm>  SupportedHmacHashingAlgorithms    { get; init; } = [ HmacHashingAlgorithm.SHA256 ];

        /// <summary>
        /// The versions of the pairing API this client implements (default: v1).
        /// </summary>
        public IReadOnlyList<String>                SupportedAPIVersions              { get; init; } = Version.S2ConnectAPIVersions;

        /// <summary>
        /// The maximum duration of a pairing attempt, measured from the moment the response of
        /// requestPairing was parsed (default: 15 seconds; S2 Connect 1.0.0, "Interruption of the process").
        /// </summary>
        public TimeSpan                             PairingAttemptTimeout             { get; init; } = S2ConnectDefaults.PairingAttemptTimeout;

        /// <summary>
        /// The request timeout of requestPairing, which includes the mandatory server delay and
        /// the queueing behind other attempts for the same node (default: 15 seconds).
        /// </summary>
        public TimeSpan                             RequestPairingTimeout             { get; init; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// The request timeout of every other request; the remaining attempt time is used when
        /// it is shorter (default: 10 seconds).
        /// </summary>
        public TimeSpan                             RequestTimeout                    { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The delay before a request answered with 503 is repeated (default: 1 second;
        /// a Retry-After header of the response takes precedence).
        /// </summary>
        public TimeSpan                             ServiceUnavailableRetryDelay      { get; init; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// How often a request answered with 503 is repeated before the attempt fails (default: 3).
        /// </summary>
        public Int32                                MaxServiceUnavailableRetries      { get; init; } = 3;

        /// <summary>
        /// The deployment assumed for a pairing server whose endpoint description does not
        /// state one (default: null, such responses fail the attempt).
        /// </summary>
        public Deployment?                          DefaultServerDeployment           { get; init; }

        /// <summary>
        /// Whether self-signed server certificates are accepted during pairing (default: true;
        /// S2 Connect 1.0.0, "Trusting a self-signed root certificate": the pairing client must
        /// accept them, trust is established by the challenge-response process).
        /// </summary>
        public Boolean                              AcceptSelfSignedCertificates      { get; init; } = true;

        /// <summary>
        /// The parser options for response bodies (default: <see cref="S2ParserOptions.Default"/>).
        /// </summary>
        public S2ParserOptions                      ParserOptions                     { get; init; } = S2ParserOptions.Default;

        #endregion

        #region (static) Default

        /// <summary>
        /// The default options.
        /// </summary>
        public static PairingClientOptions Default { get; } = new ();

        #endregion


        #region Validate()

        /// <summary>
        /// Validate the options; throws when a value is out of range.
        /// </summary>
        public void Validate()
        {

            if (SupportedHmacHashingAlgorithms is null || SupportedHmacHashingAlgorithms.Count == 0)
                throw new ArgumentException("At least one HMAC hashing algorithm must be offered!", nameof(SupportedHmacHashingAlgorithms));

            if (!SupportedHmacHashingAlgorithms.Contains(HmacHashingAlgorithm.SHA256))
                throw new ArgumentException("SHA256 must be offered (S2 Connect 1.0.0, requestPairing)!", nameof(SupportedHmacHashingAlgorithms));

            if (SupportedAPIVersions is null || SupportedAPIVersions.Count == 0)
                throw new ArgumentException("At least one API version must be supported!", nameof(SupportedAPIVersions));

            if (PairingAttemptTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(PairingAttemptTimeout), "The pairing attempt timeout must be positive!");

            if (RequestPairingTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(RequestPairingTimeout), "The requestPairing timeout must be positive!");

            if (RequestTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(RequestTimeout), "The request timeout must be positive!");

            if (ServiceUnavailableRetryDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ServiceUnavailableRetryDelay), "The retry delay must not be negative!");

            if (MaxServiceUnavailableRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(MaxServiceUnavailableRetries), "The number of retries must not be negative!");

            if (DefaultServerDeployment.HasValue && DefaultServerDeployment.Value != Deployment.LAN && DefaultServerDeployment.Value != Deployment.WAN)
                throw new ArgumentException("The default server deployment must be LAN or WAN!", nameof(DefaultServerDeployment));

            ArgumentNullException.ThrowIfNull(ParserOptions, nameof(ParserOptions));

        }

        #endregion

    }

}

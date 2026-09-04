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

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// Every normative constant of the S2 Connect 1.0.0 specification and of S2 JSON v1.0.0,
    /// each with the section of the specification it comes from. Options records default to
    /// these values; tests assert that the defaults equal them.
    /// </summary>
    public static class S2ConnectDefaults
    {

        #region Pairing

        /// <summary>
        /// The mandatory delay before the pairing server answers a requestPairing request,
        /// enforced sequentially per node (S2 Connect, "2. Calculate clientHmacChallengeResponse":
        /// "the server must enforce a mandatory delay of one second before sending its response").
        /// </summary>
        public static readonly TimeSpan  RequestPairingDelay               = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The maximum duration of a pairing attempt, measured from the moment the pairingAttemptId
        /// was issued (S2 Connect, "Interruption of the process": "A pairing attempt has a maximum
        /// duration of 15 seconds").
        /// </summary>
        public static readonly TimeSpan  PairingAttemptTimeout             = TimeSpan.FromSeconds(15);

        /// <summary>
        /// The recommended validity of a dynamically generated pairing token (S2 Connect,
        /// "The pairing token, the node ID alias and the pairing code": "five minutes is the
        /// recommended duration").
        /// </summary>
        public static readonly TimeSpan  DynamicPairingTokenLifetime       = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The minimal length of a dynamic pairing token (S2 Connect: "Dynamic pairing tokens
        /// must contain at least 4 characters").
        /// </summary>
        public const           Int32     MinDynamicPairingTokenLength      = 4;

        /// <summary>
        /// The minimal length of a static pairing token (S2 Connect: "Static paring tokens
        /// must contain at least 6 characters").
        /// </summary>
        public const           Int32     MinStaticPairingTokenLength       = 6;

        /// <summary>
        /// The regular expression of a dynamic pairing token.
        /// </summary>
        public const           String    DynamicPairingTokenRegExpr        = "^[0-9a-zA-Z]{4,}$";

        /// <summary>
        /// The regular expression of a static pairing token.
        /// </summary>
        public const           String    StaticPairingTokenRegExpr         = "^[0-9a-zA-Z]{6,}$";

        /// <summary>
        /// The regular expression of a node ID alias (S2 Connect and s2-connect-pairing.yml,
        /// NodeIdAlias: "^[0-9a-zA-Z]+$").
        /// </summary>
        public const           String    NodeIdAliasRegExpr                = "^[0-9a-zA-Z]+$";

        /// <summary>
        /// The regular expression of a pairing code: an optional node ID alias, a dash and
        /// the pairing token (S2 Connect: "^([0-9a-zA-Z]+-)?[0-9a-zA-Z]{4,}$").
        /// </summary>
        public const           String    PairingCodeRegExpr                = "^([0-9a-zA-Z]+-)?[0-9a-zA-Z]{4,}$";

        /// <summary>
        /// The alphabet used when generating pairing tokens: upper case letters and digits
        /// without the easily confused characters 0/O and 1/I/l (see PLAN.md §3.6). Validation
        /// still accepts the full class of the specification.
        /// </summary>
        public const           String    PairingTokenAlphabet              = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        /// <summary>
        /// The number of random bytes of a pairingAttemptId before Base64 encoding
        /// (s2-connect-pairing.yml, PairingAttemptId: "The recommended method is to generate
        /// 24 random bytes and Base64 encode them").
        /// </summary>
        public const           Int32     PairingAttemptIdRandomBytes       = 24;

        /// <summary>
        /// The minimal length of a pairingAttemptId in characters
        /// (s2-connect-pairing.yml, PairingAttemptId: "minLength: 32").
        /// </summary>
        public const           Int32     MinPairingAttemptIdLength         = 32;

        /// <summary>
        /// The minimal length of an HMAC challenge in bytes (S2 Connect, "Challenge response
        /// process": "it must have a minimal length of 32 bytes").
        /// </summary>
        public const           Int32     MinChallengeLength                = 32;

        /// <summary>
        /// The maximal number of pairing attempts waiting for the per-node one-second delay
        /// before the server answers 503 (PLAN.md, Phase 6a; not a normative value).
        /// </summary>
        public const           Int32     MaxQueuedPairingAttemptsPerNode   = 4;

        #endregion

        #region Long-polling

        /// <summary>
        /// The maximum time a long-polling server keeps a waitForPairing request open
        /// (S2 Connect, "Long-polling": "The server must always respond within 25 seconds").
        /// </summary>
        public static readonly TimeSpan  LongPollingServerTimeout          = TimeSpan.FromSeconds(25);

        /// <summary>
        /// The minimal request timeout of a long-polling client
        /// (S2 Connect, "Long-polling": "The client must use a request time-out of at least 30 seconds").
        /// </summary>
        public static readonly TimeSpan  LongPollingClientTimeout          = TimeSpan.FromSeconds(30);

        #endregion

        #region Session initiation and tokens

        /// <summary>
        /// The minimal length of an access token in bytes (S2 Connect, "8A. Response status 200":
        /// "must have a minimum length of 32 bytes").
        /// </summary>
        public const           Int32     MinAccessTokenLength              = 32;

        /// <summary>
        /// The minimal length of a communication (WebSocket) token in bytes
        /// (s2-connect-common.yml, CommunicationToken: "should have a minimum length of 32 bytes").
        /// </summary>
        public const           Int32     MinCommunicationTokenLength       = 32;

        /// <summary>
        /// The time within which a pending access token must be confirmed
        /// (S2 Connect, "6. Activate new accessToken": "not more than 15 seconds ago").
        /// </summary>
        public static readonly TimeSpan  PendingAccessTokenLifetime        = TimeSpan.FromSeconds(15);

        /// <summary>
        /// The maximal validity of a communication (WebSocket) token
        /// (s2-connect-common.yml, CommunicationToken: "valid for maximum 30 seconds").
        /// </summary>
        public static readonly TimeSpan  CommunicationTokenLifetime        = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The base delay of the reconnection back-off (S2 Connect, "Reconnection strategy":
        /// base_delay = 2 seconds).
        /// </summary>
        public static readonly TimeSpan  ReconnectBaseDelay                = TimeSpan.FromSeconds(2);

        /// <summary>
        /// The maximal delay of the reconnection back-off (S2 Connect, "Reconnection strategy":
        /// max_delay = 600 seconds).
        /// </summary>
        public static readonly TimeSpan  ReconnectMaxDelay                 = TimeSpan.FromSeconds(600);

        #endregion

        #region WebSocket

        /// <summary>
        /// The recommended WebSocket ping interval (S2 Connect, "Keepalive &amp; heartbeat":
        /// "should send a ping frame every 30 seconds").
        /// </summary>
        public static readonly TimeSpan  WebSocketPingInterval             = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The maximal WebSocket ping interval (S2 Connect, "Keepalive &amp; heartbeat":
        /// "must not wait more than 60 seconds between sending ping frames").
        /// </summary>
        public static readonly TimeSpan  MaxWebSocketPingInterval          = TimeSpan.FromSeconds(60);

        /// <summary>
        /// The default time to wait for a ReceptionStatus of an own message. The S2 standard
        /// does not define a value; s2-python gives up after 5 seconds, so a peer must answer
        /// well within that time (PLAN.md §3.2).
        /// </summary>
        public static readonly TimeSpan  ReceptionStatusTimeout            = TimeSpan.FromSeconds(5);

        #endregion

        #region Discovery

        /// <summary>
        /// The DNS-SD service type of S2 Connect (S2 Connect, "DNS-SD based discovery":
        /// service type "_s2connect", protocol "_tcp").
        /// </summary>
        public const           String    DNSSDServiceType                  = "_s2connect._tcp";

        /// <summary>
        /// The DNS-SD subtype of endpoints hosting one or more CEM nodes.
        /// </summary>
        public const           String    DNSSDSubtypeCEM                   = "_cem";

        /// <summary>
        /// The DNS-SD subtype of endpoints hosting one or more RM nodes.
        /// </summary>
        public const           String    DNSSDSubtypeRM                    = "_rm";

        /// <summary>
        /// The mandatory TXT record key carrying the TXT record version.
        /// The specification's own avahi example spells it "txtvers"; browsers accept both.
        /// </summary>
        public const           String    DNSSDTXTVersionKey                = "txtver";

        /// <summary>
        /// The alternative spelling of the TXT record version key used by the
        /// specification's avahi example and by RFC 6763 practice.
        /// </summary>
        public const           String    DNSSDTXTVersionKeyAlias           = "txtvers";

        /// <summary>
        /// The TXT record version of this specification ("Must be the literal string value 1").
        /// </summary>
        public const           String    DNSSDTXTVersion                   = "1";

        /// <summary>
        /// The optional TXT record key carrying the endpoint name.
        /// </summary>
        public const           String    DNSSDTXTEndpointNameKey           = "e_name";

        /// <summary>
        /// The optional TXT record key carrying the endpoint logo URL.
        /// </summary>
        public const           String    DNSSDTXTEndpointLogoURLKey        = "e_logoUrl";

        /// <summary>
        /// The TXT record key carrying the pairing URL.
        /// </summary>
        public const           String    DNSSDTXTPairingURLKey             = "pairingUrl";

        /// <summary>
        /// The TXT record key carrying the long-polling URL.
        /// </summary>
        public const           String    DNSSDTXTLongPollingURLKey         = "longpollingUrl";

        /// <summary>
        /// The maximal length of a single TXT record value in bytes
        /// (S2 Connect: "each value has a maximum length of 255 bytes").
        /// </summary>
        public const           Int32     DNSSDTXTMaxValueBytes             = 255;

        #endregion

        #region Cryptography

        /// <summary>
        /// The only HMAC hashing algorithm defined by S2 Connect 1.0
        /// (s2-connect-pairing.yml, HmacHashingAlgorithm: "SHA256").
        /// </summary>
        public const           String    HMACHashingAlgorithmSHA256        = "SHA256";

        /// <summary>
        /// The key of the mandatory entry of a certificateFingerprint map
        /// (S2 Connect, "6B. POST /[version]/postConnectionDetails": "The key SHA256 must always be provided").
        /// </summary>
        public const           String    CertificateFingerprintSHA256Key   = "SHA256";

        #endregion

    }

}

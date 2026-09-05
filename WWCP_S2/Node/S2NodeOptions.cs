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

#region Usings

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Node
{

    /// <summary>
    /// The options of an S2 node (PLAN.md §3.7): the public URLs of the endpoint, the local
    /// sockets, the communication capabilities and the options of the composed S2 Connect
    /// components. Every URL is the public one the peers use; the sockets are where the
    /// node listens (they differ behind a reverse proxy or NAT).
    /// </summary>
    public sealed record S2NodeOptions
    {

        #region Endpoint

        /// <summary>
        /// The description of the endpoint (name, logo); the deployment is taken from <see cref="Deployment"/>.
        /// </summary>
        public required EndpointDescription              Description                          { get; init; }

        /// <summary>
        /// The deployment of the endpoint: WAN or LAN.
        /// </summary>
        public required Deployment                       Deployment                           { get; init; }

        /// <summary>
        /// The public pairing URL of the endpoint (https, trailing slash, no API version).
        /// </summary>
        public required S2BaseURL                        PairingUrl                           { get; init; }

        /// <summary>
        /// The public session initiation URL (default: the pairing URL with the path "/connection/");
        /// only used when the node may act as communication server.
        /// </summary>
        public S2BaseURL?                                SessionInitiationUrl                 { get; init; }

        /// <summary>
        /// The public WebSocket URL handed to communication clients (default: the host of the pairing URL
        /// on <see cref="WebSocketPort"/> with the path "/"); only used when the node may act as communication server.
        /// </summary>
        public URL?                                      WebSocketUrl                         { get; init; }

        /// <summary>
        /// An optional long-polling URL announced via DNS-SD (default: the pairing URL when the pairing
        /// server offers long-polling, otherwise none).
        /// </summary>
        public S2BaseURL?                                LongPollingUrl                       { get; init; }

        /// <summary>
        /// Whether this is a WAN pairing server acting for a LAN endpoint (S2 Connect 1.0.0, "Deployment").
        /// </summary>
        public Boolean                                   IsWANPairingServerForLANEndpoint     { get; init; }

        #endregion

        #region Sockets

        /// <summary>
        /// The address the HTTP and WebSocket servers bind to (default: any).
        /// </summary>
        public IIPAddress?                               BindAddress                          { get; init; }

        /// <summary>
        /// The HTTP port of the pairing and session initiation APIs (default: the port of the pairing URL).
        /// Ignored when an existing HTTP server is supplied.
        /// </summary>
        public IPPort?                                   HTTPPort                             { get; init; }

        /// <summary>
        /// The port of the WebSocket server (default: the port of the WebSocket URL, or the HTTP port plus one).
        /// </summary>
        public IPPort?                                   WebSocketPort                        { get; init; }

        #endregion

        #region Capabilities

        /// <summary>
        /// Whether the node runs a session initiation server and a WebSocket server (default:
        /// WAN endpoints always, LAN endpoints when they host CEM nodes; S2 Connect 1.0.0, "Communication roles").
        /// </summary>
        public Boolean?                                  EnableCommunicationServer            { get; init; }

        /// <summary>
        /// Whether the node opens sessions towards communication servers (default: LAN endpoints).
        /// </summary>
        public Boolean?                                  EnableCommunicationClient            { get; init; }

        /// <summary>
        /// Whether a LAN endpoint is advertised via DNS-SD when a service discovery is given (default: true).
        /// </summary>
        public Boolean                                   AdvertiseViaDNSSD                    { get; init; } = true;

        /// <summary>
        /// Whether the node starts a service discovery that is not running yet (default: true).
        /// </summary>
        public Boolean                                   StartServiceDiscovery                { get; init; } = true;

        #endregion

        #region Component options

        /// <summary>
        /// Optional options of the pairing server.
        /// </summary>
        public PairingServerOptions?                     PairingServer                        { get; init; }

        /// <summary>
        /// Optional options of the pairing clients created by the node.
        /// </summary>
        public PairingClientOptions?                     PairingClient                        { get; init; }

        /// <summary>
        /// Optional options of the long-polling clients created by the node.
        /// </summary>
        public LongPollingClientOptions?                 LongPollingClient                    { get; init; }

        /// <summary>
        /// Optional options of the session initiation server.
        /// </summary>
        public SessionInitiationServerOptions?           SessionInitiationServer              { get; init; }

        /// <summary>
        /// Optional options of the session initiation clients created by the node.
        /// </summary>
        public SessionInitiationClientOptions?           SessionInitiationClient              { get; init; }

        /// <summary>
        /// Optional options of the DNS-SD endpoint advertiser.
        /// </summary>
        public EndpointAdvertiserOptions?                Advertiser                           { get; init; }

        /// <summary>
        /// An optional subnet policy of the LAN-only pairing operations.
        /// </summary>
        public ISubnetPolicy?                            SubnetPolicy                         { get; init; }

        /// <summary>
        /// The parser options of the S2 sessions (default: the standard options).
        /// </summary>
        public S2ParserOptions                           ParserOptions                        { get; init; } = S2ParserOptions.Default;

        /// <summary>
        /// The S2 message versions the hosted nodes support (default: v1.0.0).
        /// </summary>
        public IReadOnlyList<String>                     SupportedS2MessageVersions           { get; init; } = [ Version.S2JSONVersion ];

        /// <summary>
        /// The communication protocols the hosted nodes support (default: WebSocket).
        /// </summary>
        public IReadOnlyList<CommunicationProtocol>      SupportedCommunicationProtocols      { get; init; } = [ CommunicationProtocol.WebSocket ];

        /// <summary>
        /// The interval of the WebSocket pings (default: 30 s, at most 60 s).
        /// </summary>
        public TimeSpan                                  WebSocketPingInterval                { get; init; } = S2ConnectDefaults.WebSocketPingInterval;

        /// <summary>
        /// A factory of the reconnect strategy of communication clients (default: the strategy of the specification).
        /// </summary>
        public Func<ReconnectStrategy>?                  ReconnectStrategyFactory             { get; init; }

        #endregion

        #region Lifecycle

        /// <summary>
        /// How long StopAsync waits for the sessions to close before it stops the servers (default: 5 s).
        /// </summary>
        public TimeSpan                                  StopDrainTimeout                     { get; init; } = TimeSpan.FromSeconds(5);

        #endregion

        #region Certificates (until Phase 11 wires the TLS material through)

        /// <summary>
        /// The fingerprint of the own TLS server certificate (the "F" of the LAN pairing formula).
        /// </summary>
        public ServerCertificateFingerprintDelegate?     ServerCertificateFingerprint         { get; init; }

        /// <summary>
        /// The fingerprint of the own CA certificate handed to communication clients.
        /// </summary>
        public ServerCertificateFingerprintDelegate?     CACertificateFingerprint             { get; init; }

        /// <summary>
        /// The fingerprint assumed for remote pairing servers reached over plain HTTP (tests only).
        /// </summary>
        public CertificateFingerprint?                   AssumedRemoteServerCertificateFingerprint { get; init; }

        /// <summary>
        /// The certificates pinned per domain name (default: a fresh store per node). Pairing pins
        /// the peer's certificate fingerprints; later connections must present a pinned certificate.
        /// </summary>
        public CertificatePinStore?                      CertificatePins                      { get; init; }

        /// <summary>
        /// Whether the node enforces the pinned certificates on its outgoing TLS connections
        /// (default: true, PLAN.md D13). With false the clients fall back to accepting self-signed
        /// certificates, which is what Phases 6 to 10 did.
        /// </summary>
        public Boolean                                   EnforceCertificatePinning            { get; init; } = true;

        #endregion

        #region Hardening

        /// <summary>
        /// The default maximal size of an HTTP request body: 1 MiB.
        /// </summary>
        public const UInt64                              DefaultMaxHTTPBodySize               = 1024 * 1024;

        /// <summary>
        /// The default maximal size of an S2 WebSocket message: 1 MiB.
        /// </summary>
        public const UInt64                              DefaultMaxWebSocketMessageSize       = 1024 * 1024;

        /// <summary>
        /// The maximal size of an HTTP request body the HTTP server of this node accepts
        /// (default: 1 MiB); a larger request is refused with 413 before it is read to its end.
        /// Only used when the node creates its own HTTP server. The pairing and session
        /// initiation APIs enforce their own, much smaller limits on top of this one.
        /// </summary>
        public UInt64                                    MaxHTTPBodySize                      { get; init; } = DefaultMaxHTTPBodySize;

        /// <summary>
        /// The maximal size of an S2 WebSocket message (default: 1 MiB); a larger message
        /// closes the connection with 1009 "message too big". Applies to the sessions this
        /// node accepts and to those it establishes.
        /// </summary>
        public UInt64                                    MaxWebSocketMessageSize              { get; init; } = DefaultMaxWebSocketMessageSize;

        /// <summary>
        /// Whether the loggers of this node redact the secrets of S2 Connect (default: true,
        /// PLAN.md §3.6). The node wraps the given logger factory into an
        /// <see cref="S2RedactingLoggerFactory"/>, so that every component it composes logs
        /// redacted.
        /// </summary>
        public Boolean                                   RedactSecretsInLogs                  { get; init; } = true;

        #endregion


        #region Validate()

        /// <summary>
        /// Validate the options.
        /// </summary>
        public void Validate()
        {

            if (Description is null)
                throw new ArgumentException("The endpoint description is required!", nameof(Description));

            if (Deployment != Deployment.WAN && Deployment != Deployment.LAN)
                throw new ArgumentException($"The deployment must be WAN or LAN, not '{Deployment}'!", nameof(Deployment));

            if (String.IsNullOrEmpty(PairingUrl.Value))
                throw new ArgumentException("The pairing URL is required!", nameof(PairingUrl));

            if (SessionInitiationUrl.HasValue && String.IsNullOrEmpty(SessionInitiationUrl.Value.Value))
                throw new ArgumentException("The session initiation URL must not be empty!", nameof(SessionInitiationUrl));

            if (IsWANPairingServerForLANEndpoint && Deployment != Deployment.LAN)
                throw new ArgumentException("Only a LAN endpoint can be represented by a WAN pairing server!", nameof(IsWANPairingServerForLANEndpoint));

            if (SupportedS2MessageVersions is null || SupportedS2MessageVersions.Count == 0)
                throw new ArgumentException("At least one S2 message version must be supported!", nameof(SupportedS2MessageVersions));

            if (SupportedCommunicationProtocols is null || SupportedCommunicationProtocols.Count == 0)
                throw new ArgumentException("At least one communication protocol must be supported!", nameof(SupportedCommunicationProtocols));

            if (WebSocketPingInterval <= TimeSpan.Zero || WebSocketPingInterval > S2ConnectDefaults.MaxWebSocketPingInterval)
                throw new ArgumentException("The WebSocket ping interval must be positive and at most 60 seconds!", nameof(WebSocketPingInterval));

            if (StopDrainTimeout < TimeSpan.Zero)
                throw new ArgumentException("The stop drain timeout must not be negative!", nameof(StopDrainTimeout));

            if (ParserOptions is null)
                throw new ArgumentException("The parser options are required!", nameof(ParserOptions));

            if (MaxHTTPBodySize < 1 || MaxHTTPBodySize > Int32.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(MaxHTTPBodySize), "The maximal HTTP body size must be between 1 and Int32.MaxValue bytes!");

            if (MaxWebSocketMessageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(MaxWebSocketMessageSize), "The maximal WebSocket message size must be positive!");

        }

        #endregion

    }

}

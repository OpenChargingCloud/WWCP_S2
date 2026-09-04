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

using System.Collections.Concurrent;
using System.Security.Authentication;

using Microsoft.Extensions.Logging;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Sockets;
using org.GraphDefined.Vanaheimr.Hermod.TCP;
using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.WebSockets
{

    /// <summary>
    /// The S2 WebSocket server (the "communication server" of S2 Connect): every accepted
    /// connection must present a single-use communication token as bearer token in its upgrade
    /// request (S2 Connect, "Authentication"), gets permessage-deflate (RFC 7692) and pings every
    /// 30 seconds ("Keepalive &amp; heartbeat"), and runs one <see cref="S2Session"/> in
    /// S2 Connect mode (no Handshake).
    /// </summary>
    public class S2WebSocketServer : WebSocketServer
    {

        #region Data

        /// <summary>
        /// The default HTTP server name.
        /// </summary>
        public const String  DefaultHTTPServerName  = "GraphDefined S2 WebSocket Server";

        /// <summary>
        /// The key of the client identity stored as custom data on every accepted connection.
        /// </summary>
        public     const String  IdentityCustomDataKey  = "S2Identity";

        private readonly ConcurrentDictionary<WebSocketServerConnection, (S2WebSocketServerMedium Medium, S2Session Session, Object? Identity)>  sessions = [];
        private readonly ILogger?  s2Logger;

        #endregion

        #region Properties

        /// <summary>
        /// The role of the local node; the sessions of accepted clients use the peer role.
        /// </summary>
        public EnergyManagementRole                 ServerRole               { get; }

        /// <summary>
        /// The single-use communication tokens accepted by this server (used when no
        /// <see cref="TokenValidator"/> is given).
        /// </summary>
        public CommunicationTokenStore              TokenStore               { get; }

        /// <summary>
        /// An optional custom validator of the bearer tokens of upgrade requests.
        /// </summary>
        public ValidateCommunicationTokenDelegate?  TokenValidator           { get; }

        /// <summary>
        /// The optional URL path clients must connect to; any path is accepted when null.
        /// </summary>
        public HTTPPath?                            WebSocketPath            { get; }

        /// <summary>
        /// Creates the session options for an accepted connection.
        /// </summary>
        public S2SessionOptionsFactory              SessionOptionsFactory    { get; }

        /// <summary>
        /// The time provider used for the sessions.
        /// </summary>
        public TimeProvider                         TimeProvider             { get; }

        /// <summary>
        /// The currently running sessions.
        /// </summary>
        public IEnumerable<S2Session>               Sessions
            => sessions.Values.Select(entry => entry.Session);

        #endregion

        #region Events

        /// <summary>
        /// Raised when an S2 session was started on an accepted connection.
        /// </summary>
        public event OnS2SessionStartedDelegate?  OnSessionStarted;

        /// <summary>
        /// Raised when an S2 session on an accepted connection ended.
        /// </summary>
        public event OnS2SessionEndedDelegate?    OnSessionEnded;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new S2 WebSocket server.
        /// </summary>
        /// <param name="IPAddress">The IP address to listen on (default: any).</param>
        /// <param name="HTTPPort">The TCP port to listen on.</param>
        /// <param name="ServerRole">The role of the local node (default: CEM).</param>
        /// <param name="WebSocketPath">The optional URL path clients must connect to.</param>
        /// <param name="TokenStore">An optional token store (default: a new store with the default lifetime).</param>
        /// <param name="TokenValidator">An optional custom token validator replacing the token store.</param>
        /// <param name="SessionOptionsFactory">An optional factory for the session options of accepted connections.</param>
        /// <param name="NegotiatedVersion">The S2 JSON version negotiated by session initiation (default: v1.0.0).</param>
        /// <param name="WebSocketPingEvery">The ping interval (default: 30 seconds, must not exceed 60 seconds).</param>
        /// <param name="HTTPServerName">An optional HTTP server name.</param>
        /// <param name="Description">An optional description.</param>
        /// <param name="ServerCertificateSelector">The TLS server certificate selector (TLS is used when given).</param>
        /// <param name="ClientCertificateValidator">An optional TLS client certificate validator.</param>
        /// <param name="LocalCertificateSelector">An optional local certificate selector.</param>
        /// <param name="AllowedTLSProtocols">The allowed TLS protocol versions.</param>
        /// <param name="ClientCertificateRequired">Whether a TLS client certificate is required.</param>
        /// <param name="CheckCertificateRevocation">Whether to check certificate revocation.</param>
        /// <param name="MaxClientConnections">The maximal number of concurrent connections.</param>
        /// <param name="DNSClient">An optional DNS client.</param>
        /// <param name="TimeProvider">An optional time provider for the sessions.</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        /// <param name="AutoStart">Whether to start the server immediately.</param>
        public S2WebSocketServer(IIPAddress?                                               IPAddress                    = null,
                                 IPPort?                                                   HTTPPort                     = null,
                                 EnergyManagementRole?                                     ServerRole                   = null,
                                 HTTPPath?                                                 WebSocketPath                = null,
                                 CommunicationTokenStore?                                  TokenStore                   = null,
                                 ValidateCommunicationTokenDelegate?                       TokenValidator               = null,
                                 S2SessionOptionsFactory?                                  SessionOptionsFactory        = null,
                                 String?                                                   NegotiatedVersion            = null,
                                 TimeSpan?                                                 WebSocketPingEvery           = null,
                                 String?                                                   HTTPServerName               = null,
                                 I18NString?                                               Description                  = null,

                                 ServerCertificateSelectorDelegate?                        ServerCertificateSelector    = null,
                                 RemoteTLSClientCertificateValidationHandler<ITCPServer>?  ClientCertificateValidator   = null,
                                 LocalCertificateSelectionHandler?                         LocalCertificateSelector     = null,
                                 SslProtocols?                                             AllowedTLSProtocols          = null,
                                 Boolean?                                                  ClientCertificateRequired    = null,
                                 Boolean?                                                  CheckCertificateRevocation   = null,

                                 UInt32?                                                   MaxClientConnections         = null,
                                 IDNSClient?                                               DNSClient                    = null,
                                 TimeProvider?                                             TimeProvider                 = null,
                                 ILoggerFactory?                                           LoggerFactory                = null,
                                 Boolean                                                   AutoStart                    = false)

            : base(IPAddress:                   IPAddress,
                   HTTPPort:                    HTTPPort,
                   HTTPServerName:              HTTPServerName ?? DefaultHTTPServerName,
                   Description:                 Description,
                   RequireAuthentication:       false,
                   SecWebSocketProtocols:       null,
                   DisableWebSocketPings:       false,
                   WebSocketPingEvery:          WebSocketPingEvery ?? S2ConnectDefaults.WebSocketPingInterval,
                   ServerCertificateSelector:   ServerCertificateSelector,
                   ClientCertificateValidator:  ClientCertificateValidator,
                   LocalCertificateSelector:    LocalCertificateSelector,
                   AllowedTLSProtocols:         AllowedTLSProtocols,
                   ClientCertificateRequired:   ClientCertificateRequired,
                   CheckCertificateRevocation:  CheckCertificateRevocation,
                   MaxClientConnections:        MaxClientConnections,
                   DNSClient:                   DNSClient,
                   LoggerFactory:               LoggerFactory,
                   AutoStart:                   false)

        {

            if (WebSocketPingEvery.HasValue && WebSocketPingEvery.Value > S2ConnectDefaults.MaxWebSocketPingInterval)
                throw new ArgumentOutOfRangeException(nameof(WebSocketPingEvery), "S2 WebSocket pings must not be more than 60 seconds apart!");

            this.ServerRole             = ServerRole ?? EnergyManagementRole.CEM;
            this.TimeProvider           = TimeProvider ?? System.TimeProvider.System;
            this.TokenStore             = TokenStore ?? new CommunicationTokenStore(this.TimeProvider);
            this.TokenValidator         = TokenValidator;
            this.WebSocketPath          = WebSocketPath;
            this.s2Logger                 = LoggerFactory?.CreateLogger<S2WebSocketServer>();

            var negotiatedVersion       = NegotiatedVersion ?? Version.S2JSONVersion;

            this.SessionOptionsFactory  = SessionOptionsFactory ?? ((connection, identity) => new S2SessionOptions {
                                                                        Role               = this.ServerRole,
                                                                        Mode               = S2SessionMode.S2Connect,
                                                                        NegotiatedVersion  = negotiatedVersion
                                                                    });

            // S2 Connect, "Compression": implementations SHOULD support and enable RFC 7692.
            EnablePerMessageDeflate = true;

            OnValidateWebSocketConnection  += ValidateConnectionAsync;
            OnNewWebSocketConnection       += NewConnectionAsync;
            OnTextMessageReceived          += TextMessageReceivedAsync;
            OnCloseMessageReceived         += CloseMessageReceivedAsync;
            OnTCPConnectionClosed          += TCPConnectionClosedAsync;

            if (AutoStart)
                Start().GetAwaiter().GetResult();

        }

        #endregion


        #region (private) ValidateConnectionAsync(...)

        /// <summary>
        /// Reject every upgrade request without a valid single-use bearer token
        /// (S2 Connect, "Authentication") with 401 Unauthorized.
        /// </summary>
        private async Task<HTTPResponse?> ValidateConnectionAsync(DateTimeOffset             Timestamp,
                                                                  AWebSocketServer           Server,
                                                                  WebSocketServerConnection  Connection,
                                                                  EventTracking_Id           EventTrackingId,
                                                                  CancellationToken          CancellationToken)
        {

            // Hermod parses the upgrade request before validating the connection.
            var request = Connection.HTTPRequest
                              ?? throw new InvalidOperationException("The WebSocket upgrade request is missing!");

            if (WebSocketPath is not null && request.Path != WebSocketPath.Value)
            {
                s2Logger?.LogDebug("S2 WebSocket server: rejected upgrade for unknown path '{Path}' from {Remote}.", request.Path, Connection.RemoteSocket);
                return Reject(request, HTTPStatusCode.NotFound);
            }

            if (request.Authorization is not HTTPBearerAuthentication bearer || String.IsNullOrEmpty(bearer.Token))
            {
                s2Logger?.LogDebug("S2 WebSocket server: rejected upgrade without bearer token from {Remote}.", Connection.RemoteSocket);
                return Reject(request, HTTPStatusCode.Unauthorized);
            }

            CommunicationTokenValidation validation;

            if (TokenValidator is not null)
                validation = await TokenValidator(bearer.Token, request, CancellationToken).ConfigureAwait(false);

            else
                validation = TokenStore.TryRedeem(bearer.Token, out var identity)
                                 ? CommunicationTokenValidation.Accepted(identity)
                                 : CommunicationTokenValidation.Rejected;

            if (!validation.IsAccepted)
            {
                s2Logger?.LogInformation("S2 WebSocket server: rejected upgrade with an unknown, used or expired token from {Remote}.", Connection.RemoteSocket);
                return Reject(request, HTTPStatusCode.Unauthorized);
            }

            Connection.TryAddCustomData(IdentityCustomDataKey, validation.Identity);

            return null;

        }

        private HTTPResponse Reject(HTTPRequest     Request,
                                    HTTPStatusCode  StatusCode)
        {

            var builder = new HTTPResponse.Builder(Request);

            builder.HTTPStatusCode  = StatusCode;
            builder.Server          = HTTPServiceName;
            builder.Date            = org.GraphDefined.Vanaheimr.Illias.Timestamp.Now;
            builder.Connection      = ConnectionType.Close;

            if (StatusCode == HTTPStatusCode.Unauthorized)
                builder.WWWAuthenticate = WWWAuthenticate.Parse("Bearer realm=\"S2\"");

            return builder.AsImmutable;

        }

        #endregion

        #region (private) NewConnectionAsync(...)

        private async Task NewConnectionAsync(DateTimeOffset             Timestamp,
                                              AWebSocketServer           Server,
                                              WebSocketServerConnection  Connection,
                                              IEnumerable<String>        SharedSubprotocols,
                                              String?                    SelectedSubprotocol,
                                              EventTracking_Id           EventTrackingId,
                                              CancellationToken          CancellationToken)
        {

            var identity  = Connection.TryGetCustomData(IdentityCustomDataKey);
            var medium    = new S2WebSocketServerMedium(this, Connection, s2Logger);
            var options   = SessionOptionsFactory(Connection, identity);
            var session   = new S2Session(medium, options, TimeProvider, s2Logger);

            sessions[Connection] = (medium, session, identity);

            session.OnClosed += async (timestamp, closedSession, reason) => {

                sessions.TryRemove(Connection, out _);

                await OnSessionEnded.InvokeAllAsync(handler => handler(timestamp, this, closedSession, reason), s2Logger).ConfigureAwait(false);

            };

            try
            {
                await session.StartAsync(CancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                s2Logger?.LogError(e, "S2 WebSocket server: starting the session for {Remote} failed.", Connection.RemoteSocket);
                sessions.TryRemove(Connection, out _);
                await medium.CloseAsync(new S2CloseReason("Starting the S2 session failed.", true, e), CancellationToken).ConfigureAwait(false);
                return;
            }

            await OnSessionStarted.InvokeAllAsync(handler => handler(Timestamp, this, session, identity), s2Logger).ConfigureAwait(false);

        }

        #endregion

        #region (private) TextMessageReceivedAsync(...)

        private Task TextMessageReceivedAsync(DateTimeOffset             Timestamp,
                                              AWebSocketServer           Server,
                                              WebSocketServerConnection  Connection,
                                              WebSocketFrame             Frame,
                                              EventTracking_Id           EventTrackingId,
                                              String                     TextMessage,
                                              CancellationToken          CancellationToken)

            => sessions.TryGetValue(Connection, out var entry)
                   ? entry.Medium.OnTextMessageReceivedAsync(TextMessage, CancellationToken)
                   : Task.CompletedTask;

        #endregion

        #region (private) CloseMessageReceivedAsync(...) / TCPConnectionClosedAsync(...)

        private Task CloseMessageReceivedAsync(DateTimeOffset                    Timestamp,
                                               AWebSocketServer                  Server,
                                               WebSocketServerConnection         Connection,
                                               WebSocketFrame                    Frame,
                                               EventTracking_Id                  EventTrackingId,
                                               WebSocketFrame.ClosingStatusCode  StatusCode,
                                               String?                           Reason,
                                               CancellationToken                 CancellationToken)

            => ConnectionClosedAsync(Connection, $"WebSocket closed by the peer ({StatusCode}): {Reason}");


        private Task TCPConnectionClosedAsync(DateTimeOffset             Timestamp,
                                              AWebSocketServer           Server,
                                              WebSocketServerConnection  Connection,
                                              EventTracking_Id           EventTrackingId,
                                              String?                    Reason,
                                              CancellationToken          CancellationToken)

            => ConnectionClosedAsync(Connection, $"TCP connection closed: {Reason}");


        private async Task ConnectionClosedAsync(WebSocketServerConnection  Connection,
                                                 String                     Reason)
        {

            if (sessions.TryRemove(Connection, out var entry))
                await entry.Medium.OnConnectionClosedAsync(new S2CloseReason(Reason, false)).ConfigureAwait(false);

        }

        #endregion


        #region TryGetSession(Connection, out Session)

        /// <summary>
        /// Try to get the session running on the given connection.
        /// </summary>
        /// <param name="Connection">An accepted WebSocket connection.</param>
        /// <param name="Session">The session.</param>
        public Boolean TryGetSession(WebSocketServerConnection  Connection,
                                     out S2Session?             Session)
        {

            if (sessions.TryGetValue(Connection, out var entry))
            {
                Session = entry.Session;
                return true;
            }

            Session = null;
            return false;

        }

        #endregion

    }

}

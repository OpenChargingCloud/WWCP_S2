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

using Microsoft.Extensions.Logging;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.WebSockets
{

    /// <summary>
    /// The upgrade request of an S2 WebSocket client was rejected.
    /// </summary>
    public sealed class S2WebSocketConnectException : Exception
    {

        /// <summary>
        /// The HTTP response of the server.
        /// </summary>
        public HTTPResponse Response { get; }

        /// <summary>
        /// Create a new exception.
        /// </summary>
        /// <param name="Response">The HTTP response of the server.</param>
        public S2WebSocketConnectException(HTTPResponse Response)

            : base($"The S2 WebSocket upgrade was rejected with HTTP {Response.HTTPStatusCode}.")

        {
            this.Response = Response;
        }

    }


    /// <summary>
    /// The S2 WebSocket client (the "communication client" of S2 Connect): connects to the
    /// websocketUrl received from session initiation, authenticates with the single-use
    /// communication token as bearer token, enables permessage-deflate and 30-second pings,
    /// and runs one <see cref="S2Session"/> in S2 Connect mode. There is no transport-level
    /// reconnect: after a lost connection the caller has to run session initiation again
    /// (S2 Connect, "Reconnection strategy").
    /// </summary>
    public class S2WebSocketClient : WebSocketClient
    {

        #region Data

        private readonly ILogger?  logger;

        #endregion

        #region Properties

        /// <summary>
        /// The options of the session started after connecting.
        /// </summary>
        public S2SessionOptions  SessionOptions    { get; }

        /// <summary>
        /// The time provider used for the session.
        /// </summary>
        public TimeProvider      TimeProvider      { get; }

        /// <summary>
        /// The session, once connected.
        /// </summary>
        public S2Session?        Session           { get; private set; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new S2 WebSocket client.
        /// </summary>
        /// <param name="URL">The websocketUrl received from session initiation (wss://…).</param>
        /// <param name="CommunicationToken">The single-use websocketToken received from session initiation.</param>
        /// <param name="SessionOptions">The session options (role, negotiated version, parser options).</param>
        /// <param name="WebSocketPingEvery">The ping interval (default: 30 seconds, must not exceed 60 seconds).</param>
        /// <param name="RemoteCertificateValidator">An optional TLS server certificate validator (e.g. for a pinned self-signed CA).</param>
        /// <param name="RequestTimeout">An optional timeout of the upgrade request.</param>
        /// <param name="DNSClient">An optional DNS client.</param>
        /// <param name="TimeProvider">An optional time provider for the session.</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        public S2WebSocketClient(URL                                                             URL,
                                 String                                                          CommunicationToken,
                                 S2SessionOptions                                                SessionOptions,
                                 TimeSpan?                                                       WebSocketPingEvery           = null,
                                 RemoteTLSServerCertificateValidationHandler<IWebSocketClient>?  RemoteCertificateValidator   = null,
                                 TimeSpan?                                                       RequestTimeout               = null,
                                 IDNSClient?                                                     DNSClient                    = null,
                                 TimeProvider?                                                   TimeProvider                 = null,
                                 ILoggerFactory?                                                 LoggerFactory                = null)

            : base(URL,
                   HTTPAuthentication:          HTTPBearerAuthentication.Parse(CommunicationToken),
                   RequestTimeout:              RequestTimeout,
                   WebSocketPingEvery:          WebSocketPingEvery ?? S2ConnectDefaults.WebSocketPingInterval,
                   RemoteCertificateValidator:  RemoteCertificateValidator,
                   DNSClient:                   DNSClient,
                   LoggerFactory:               LoggerFactory)

        {

            if (WebSocketPingEvery.HasValue && WebSocketPingEvery.Value > S2ConnectDefaults.MaxWebSocketPingInterval)
                throw new ArgumentOutOfRangeException(nameof(WebSocketPingEvery), "S2 WebSocket pings must not be more than 60 seconds apart!");

            SessionOptions.Validate();

            this.SessionOptions  = SessionOptions;
            this.TimeProvider    = TimeProvider ?? System.TimeProvider.System;
            this.logger          = LoggerFactory?.CreateLogger<S2WebSocketClient>();

            // S2 Connect, "Compression": implementations SHOULD support and enable RFC 7692.
            EnablePerMessageDeflate  = true;

            // S2 Connect, "Reconnection strategy": a lost session is re-established through
            // session initiation, never by reconnecting the transport.
            ReconnectPolicy          = null;

        }

        #endregion


        #region ConnectSessionAsync(CancellationToken = default)

        /// <summary>
        /// Connect to the server and start the S2 session.
        /// </summary>
        /// <param name="CancellationToken">A token to cancel the connection attempt.</param>
        /// <exception cref="S2WebSocketConnectException">The server rejected the upgrade request.</exception>
        public async Task<S2Session> ConnectSessionAsync(CancellationToken CancellationToken = default)
        {

            if (Session is not null && Session.IsConnected)
                return Session;

            var (connection, httpResponse) = await Connect(CancellationToken: CancellationToken).ConfigureAwait(false);

            if (httpResponse.HTTPStatusCode != HTTPStatusCode.SwitchingProtocols)
                throw new S2WebSocketConnectException(httpResponse);

            var medium   = new S2WebSocketClientMedium(this, connection, logger);
            var session  = new S2Session(medium, SessionOptions, TimeProvider, logger);

            Session = session;

            await session.StartAsync(CancellationToken).ConfigureAwait(false);

            return session;

        }

        #endregion

        #region CloseSessionAsync(Reason = null, CancellationToken = default)

        /// <summary>
        /// Close the session and the WebSocket connection.
        /// </summary>
        /// <param name="Reason">An optional reason.</param>
        /// <param name="CancellationToken">A token to cancel the close operation.</param>
        public async Task CloseSessionAsync(String?            Reason              = null,
                                            CancellationToken  CancellationToken   = default)
        {

            if (Session is not null)
                await Session.CloseAsync(new S2CloseReason(Reason ?? "closed by the client", true), CancellationToken).ConfigureAwait(false);

            else
                await Close(WebSocketFrame.ClosingStatusCode.NormalClosure, Reason, null, CancellationToken).ConfigureAwait(false);

        }

        #endregion

    }

}

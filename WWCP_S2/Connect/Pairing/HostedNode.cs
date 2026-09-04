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
    /// A node (CEM or RM instance) hosted by the local endpoint as seen by the pairing
    /// procedures: its description, its optional node ID alias, the S2 message versions and
    /// communication protocols it supports, its readiness for pairing and its pairing tokens
    /// (S2 Connect 1.0.0, "The node and the endpoint", "The pairing token, the node ID alias
    /// and the pairing code"). A node holds at most one <em>own</em> pairing token (dynamic
    /// with expiry or static) that it shows to the end user, and any number of <em>entered</em>
    /// pairing tokens: codes of remote nodes typed in by the end user when this node is the
    /// Initiator node. All members are thread-safe.
    /// </summary>
    public sealed class HostedNode
    {

        #region Data

        private readonly Lock                                                    lockObject = new ();
        private readonly TimeProvider                                            timeProvider;
        private readonly Dictionary<Node_Id, (PairingToken Token, DateTimeOffset? ExpiresAt)>  enteredTokens = [];

        private NodeDescription                          description;
        private Boolean                                  isReadyForPairing = true;

        private PairingToken?                            ownToken;
        private DateTimeOffset?                          ownTokenExpiresAt;
        private Boolean                                  ownTokenIsStatic;

        private (PairingToken Token, DateTimeOffset? ExpiresAt)?  enteredTokenForAnyRemote;

        #endregion

        #region Properties

        /// <summary>
        /// The identification of the node.
        /// </summary>
        public Node_Id                               Id
            => Description.Id;

        /// <summary>
        /// The energy management role of the node: CEM or RM.
        /// </summary>
        public EnergyManagementRole                  Role
            => Description.Role;

        /// <summary>
        /// The description of the node.
        /// </summary>
        public NodeDescription                       Description
        {
            get
            {
                lock (lockObject)
                {
                    return description;
                }
            }
        }

        /// <summary>
        /// The optional node ID alias: a short identifier, unique within the endpoint,
        /// shown to the end user as the first part of the pairing code.
        /// </summary>
        public NodeIdAlias?                          Alias                              { get; }

        /// <summary>
        /// The S2 JSON message versions this node supports (default: v1.0.0).
        /// </summary>
        public IReadOnlyList<String>                 SupportedS2MessageVersions         { get; }

        /// <summary>
        /// The communication protocols this node supports (default: WebSocket).
        /// </summary>
        public IReadOnlyList<CommunicationProtocol>  SupportedCommunicationProtocols    { get; }

        /// <summary>
        /// Whether the node is ready for pairing (S2 Connect 1.0.0, requestPairing check
        /// "Are the endpoint and node ready for pairing?"; default: true).
        /// </summary>
        public Boolean                               IsReadyForPairing
        {
            get
            {
                lock (lockObject)
                {
                    return isReadyForPairing;
                }
            }
            set
            {
                lock (lockObject)
                {
                    isReadyForPairing = value;
                }
            }
        }

        /// <summary>
        /// Whether the node currently has an own pairing token that has not expired.
        /// </summary>
        public Boolean                               HasValidPairingToken
            => TryGetPairingToken(out _);

        /// <summary>
        /// Whether the own pairing token is static (never expires).
        /// </summary>
        public Boolean                               PairingTokenIsStatic
        {
            get
            {
                lock (lockObject)
                {
                    return ownToken.HasValue && ownTokenIsStatic;
                }
            }
        }

        /// <summary>
        /// The expiry of the own dynamic pairing token, or null for static, missing or already expired tokens.
        /// </summary>
        public DateTimeOffset?                       PairingTokenExpiresAt
        {
            get
            {
                lock (lockObject)
                {
                    return ownToken.HasValue && ownTokenExpiresAt.HasValue && timeProvider.GetUtcNow() < ownTokenExpiresAt.Value
                               ? ownTokenExpiresAt
                               : null;
                }
            }
        }

        /// <summary>
        /// The pairing code to show to the end user (node ID alias, dash, own pairing token),
        /// or null when the node has no valid own pairing token.
        /// </summary>
        public PairingCode?                          PairingCode
        {
            get
            {

                if (TryGetPairingToken(out var token))
                    return new PairingCode(token, Alias);

                return null;

            }
        }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new hosted node.
        /// </summary>
        /// <param name="Description">The description of the node.</param>
        /// <param name="Alias">An optional node ID alias (unique within the endpoint).</param>
        /// <param name="SupportedS2MessageVersions">The S2 JSON message versions the node supports (default: v1.0.0).</param>
        /// <param name="SupportedCommunicationProtocols">The communication protocols the node supports (default: WebSocket).</param>
        /// <param name="TimeProvider">An optional time provider for token expiry (default: the system clock).</param>
        public HostedNode(NodeDescription                       Description,
                          NodeIdAlias?                          Alias                             = null,
                          IEnumerable<String>?                  SupportedS2MessageVersions        = null,
                          IEnumerable<CommunicationProtocol>?   SupportedCommunicationProtocols   = null,
                          TimeProvider?                         TimeProvider                      = null)
        {

            ArgumentNullException.ThrowIfNull(Description);

            if (Description.Id.IsNullOrEmpty)
                throw new ArgumentException("The node identification must not be empty!", nameof(Description));

            this.description                      = Description;
            this.Alias                            = Alias;
            this.SupportedS2MessageVersions       = SupportedS2MessageVersions      is not null ? [.. SupportedS2MessageVersions.     Distinct(StringComparer.Ordinal)] : [ Version.S2JSONVersion ];
            this.SupportedCommunicationProtocols  = SupportedCommunicationProtocols is not null ? [.. SupportedCommunicationProtocols.Distinct()]                       : [ CommunicationProtocol.WebSocket ];
            this.timeProvider                     = TimeProvider ?? System.TimeProvider.System;

            if (this.SupportedS2MessageVersions.Count == 0)
                throw new ArgumentException("A node must support at least one S2 message version!", nameof(SupportedS2MessageVersions));

            if (this.SupportedS2MessageVersions.Any(String.IsNullOrWhiteSpace))
                throw new ArgumentException("S2 message versions must not be empty!", nameof(SupportedS2MessageVersions));

            if (this.SupportedCommunicationProtocols.Count == 0)
                throw new ArgumentException("A node must support at least one communication protocol!", nameof(SupportedCommunicationProtocols));

        }

        #endregion


        #region UpdateDescription(Description)

        /// <summary>
        /// Update the description of the node; the identification and the role must not change.
        /// </summary>
        /// <param name="Description">The new description.</param>
        public void UpdateDescription(NodeDescription Description)
        {

            ArgumentNullException.ThrowIfNull(Description);

            lock (lockObject)
            {

                if (Description.Id != description.Id)
                    throw new ArgumentException("The identification of a hosted node must not change!", nameof(Description));

                if (Description.Role != description.Role)
                    throw new ArgumentException("The role of a hosted node must not change!", nameof(Description));

                description = Description;

            }

        }

        #endregion


        #region IssueDynamicPairingToken(Length = 6, Lifetime = null)

        /// <summary>
        /// Issue a new dynamic own pairing token from the cryptographically secure generator,
        /// replacing any earlier own token, and return the pairing code to show to the end user.
        /// </summary>
        /// <param name="Length">The length of the token (at least 4 characters, default: 6).</param>
        /// <param name="Lifetime">The validity of the token (default: 5 minutes).</param>
        public PairingCode IssueDynamicPairingToken(Int32      Length     = 6,
                                                    TimeSpan?  Lifetime   = null)

            => SetDynamicPairingToken(TokenGenerator.NewDynamicPairingToken(Length),
                                      Lifetime);

        #endregion

        #region SetDynamicPairingToken  (Token, Lifetime = null)

        /// <summary>
        /// Use the given token as dynamic own pairing token (e.g. one issued by an external
        /// user interface), replacing any earlier own token, and return the pairing code.
        /// </summary>
        /// <param name="Token">The pairing token.</param>
        /// <param name="Lifetime">The validity of the token (default: 5 minutes).</param>
        public PairingCode SetDynamicPairingToken(PairingToken  Token,
                                                  TimeSpan?     Lifetime   = null)
        {

            var lifetime = Lifetime ?? S2ConnectDefaults.DynamicPairingTokenLifetime;

            if (lifetime <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(Lifetime), "The lifetime of a dynamic pairing token must be positive!");

            if (Token.Length < S2ConnectDefaults.MinDynamicPairingTokenLength)
                throw new ArgumentException($"A dynamic pairing token must have at least {S2ConnectDefaults.MinDynamicPairingTokenLength} characters!", nameof(Token));

            lock (lockObject)
            {
                ownToken           = Token;
                ownTokenExpiresAt  = timeProvider.GetUtcNow() + lifetime;
                ownTokenIsStatic   = false;
            }

            return new PairingCode(Token, Alias);

        }

        #endregion

        #region SetStaticPairingToken   (Token)

        /// <summary>
        /// Use the given token as static (never expiring) own pairing token, e.g. one printed
        /// on the device, replacing any earlier own token, and return the pairing code.
        /// </summary>
        /// <param name="Token">The pairing token (at least 6 characters).</param>
        public PairingCode SetStaticPairingToken(PairingToken Token)
        {

            if (!Token.MeetsStaticRequirement)
                throw new ArgumentException($"A static pairing token must have at least {S2ConnectDefaults.MinStaticPairingTokenLength} characters!", nameof(Token));

            lock (lockObject)
            {
                ownToken           = Token;
                ownTokenExpiresAt  = null;
                ownTokenIsStatic   = true;
            }

            return new PairingCode(Token, Alias);

        }

        #endregion

        #region ClearPairingToken()

        /// <summary>
        /// Remove the own pairing token.
        /// </summary>
        public void ClearPairingToken()
        {
            lock (lockObject)
            {
                ownToken           = null;
                ownTokenExpiresAt  = null;
                ownTokenIsStatic   = false;
            }
        }

        #endregion

        #region TryGetPairingToken(out Token)

        /// <summary>
        /// Try to get the own pairing token when it exists and has not expired.
        /// </summary>
        /// <param name="Token">The own pairing token.</param>
        public Boolean TryGetPairingToken(out PairingToken Token)
        {

            lock (lockObject)
            {

                if (ownToken.HasValue &&
                    (ownTokenIsStatic || !ownTokenExpiresAt.HasValue || timeProvider.GetUtcNow() < ownTokenExpiresAt.Value))
                {
                    Token = ownToken.Value;
                    return true;
                }

            }

            Token = default;
            return false;

        }

        #endregion


        #region EnterPairingToken(Token, RemoteNodeId = null, Lifetime = null)

        /// <summary>
        /// Remember a pairing token of a remote node that the end user typed in at this node
        /// (this node becomes the Initiator node, S2 Connect 1.0.0, requestPairing check "If the
        /// targeted node on the HTTPS server is the Initiator node, did the end user provide a
        /// valid pairing token?").
        /// </summary>
        /// <param name="Token">The pairing token of the remote node.</param>
        /// <param name="RemoteNodeId">The identification of the remote node the token belongs to; null when it is valid for whichever remote node pairs next.</param>
        /// <param name="Lifetime">How long the entered token is kept (default: 5 minutes; <see cref="Timeout.InfiniteTimeSpan"/> keeps it until it is consumed or removed).</param>
        public void EnterPairingToken(PairingToken  Token,
                                      Node_Id?      RemoteNodeId   = null,
                                      TimeSpan?     Lifetime       = null)
        {

            if (Token.Length < S2ConnectDefaults.MinDynamicPairingTokenLength)
                throw new ArgumentException($"A pairing token must have at least {S2ConnectDefaults.MinDynamicPairingTokenLength} characters!", nameof(Token));

            var lifetime = Lifetime ?? S2ConnectDefaults.DynamicPairingTokenLifetime;

            if (lifetime != Timeout.InfiniteTimeSpan && lifetime <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(Lifetime), "The lifetime of an entered pairing token must be positive!");

            var expiresAt = lifetime == Timeout.InfiniteTimeSpan
                                ? (DateTimeOffset?) null
                                : timeProvider.GetUtcNow() + lifetime;

            lock (lockObject)
            {

                if (RemoteNodeId.HasValue)
                    enteredTokens[RemoteNodeId.Value] = (Token, expiresAt);
                else
                    enteredTokenForAnyRemote = (Token, expiresAt);

            }

        }

        #endregion

        #region RemoveEnteredPairingToken(RemoteNodeId = null)

        /// <summary>
        /// Forget the entered pairing token of the given remote node (or the one valid for any remote node).
        /// </summary>
        /// <param name="RemoteNodeId">The identification of the remote node, or null for the token valid for any remote node.</param>
        /// <returns>Whether such a token existed.</returns>
        public Boolean RemoveEnteredPairingToken(Node_Id? RemoteNodeId = null)
        {

            lock (lockObject)
            {

                if (RemoteNodeId.HasValue)
                    return enteredTokens.Remove(RemoteNodeId.Value);

                var existed = enteredTokenForAnyRemote.HasValue;
                enteredTokenForAnyRemote = null;
                return existed;

            }

        }

        #endregion

        #region ClearEnteredPairingTokens()

        /// <summary>
        /// Forget every entered pairing token.
        /// </summary>
        public void ClearEnteredPairingTokens()
        {
            lock (lockObject)
            {
                enteredTokens.Clear();
                enteredTokenForAnyRemote = null;
            }
        }

        #endregion

        #region TryGetEnteredPairingToken(RemoteNodeId, out Token)

        /// <summary>
        /// Try to get the unexpired entered pairing token for the given remote node: the token
        /// entered for exactly this remote node first, then the token entered for any remote node.
        /// </summary>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="Token">The entered pairing token.</param>
        public Boolean TryGetEnteredPairingToken(Node_Id           RemoteNodeId,
                                                 out PairingToken  Token)
        {

            var now = timeProvider.GetUtcNow();

            lock (lockObject)
            {

                if (enteredTokens.TryGetValue(RemoteNodeId, out var entry))
                {

                    if (!entry.ExpiresAt.HasValue || now < entry.ExpiresAt.Value)
                    {
                        Token = entry.Token;
                        return true;
                    }

                    enteredTokens.Remove(RemoteNodeId);

                }

                if (enteredTokenForAnyRemote.HasValue)
                {

                    if (!enteredTokenForAnyRemote.Value.ExpiresAt.HasValue || now < enteredTokenForAnyRemote.Value.ExpiresAt.Value)
                    {
                        Token = enteredTokenForAnyRemote.Value.Token;
                        return true;
                    }

                    enteredTokenForAnyRemote = null;

                }

            }

            Token = default;
            return false;

        }

        #endregion


        #region TryResolvePairingToken(RemoteNodeId, out Token, out IsInitiator)

        /// <summary>
        /// Resolve the pairing token to use for a pairing attempt with the given remote node
        /// (S2 Connect 1.0.0, "0. Precondition": "Both nodes must have a pairing token available.
        /// Either because they issued this token themselves, or because the end user has provided
        /// it through the user interface"): an entered token makes this node the Initiator node,
        /// otherwise the own token makes it the Responder node.
        /// </summary>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="Token">The pairing token to use.</param>
        /// <param name="IsInitiator">Whether this node is the Initiator node of the attempt.</param>
        public Boolean TryResolvePairingToken(Node_Id           RemoteNodeId,
                                              out PairingToken  Token,
                                              out Boolean       IsInitiator)
        {

            if (TryGetEnteredPairingToken(RemoteNodeId, out Token))
            {
                IsInitiator = true;
                return true;
            }

            IsInitiator = false;

            return TryGetPairingToken(out Token);

        }

        #endregion

        #region ConsumePairingToken(RemoteNodeId, Token)

        /// <summary>
        /// Called after a successful pairing: the entered token for the remote node is forgotten
        /// and an own <em>dynamic</em> token is cleared, so that the same code cannot be used for a
        /// second pairing; static tokens stay.
        /// </summary>
        /// <param name="RemoteNodeId">The identification of the remote node.</param>
        /// <param name="Token">The pairing token that was used.</param>
        public void ConsumePairingToken(Node_Id       RemoteNodeId,
                                        PairingToken  Token)
        {

            lock (lockObject)
            {

                if (enteredTokens.TryGetValue(RemoteNodeId, out var entry) && entry.Token.Equals(Token))
                    enteredTokens.Remove(RemoteNodeId);

                else if (enteredTokenForAnyRemote.HasValue && enteredTokenForAnyRemote.Value.Token.Equals(Token))
                    enteredTokenForAnyRemote = null;

                else if (ownToken.HasValue && !ownTokenIsStatic && ownToken.Value.Equals(Token))
                {
                    ownToken           = null;
                    ownTokenExpiresAt  = null;
                }

            }

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object (without any token).
        /// </summary>
        public override String ToString()
        {

            var currentDescription = Description;

            return $"{currentDescription.Id} ({currentDescription.Role}, {currentDescription.Brand} {currentDescription.ModelName}{(Alias.HasValue ? $", alias '{Alias.Value}'" : "")})";

        }

        #endregion

    }

}

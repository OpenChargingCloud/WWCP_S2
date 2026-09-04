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

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The state of a pairing attempt on the pairing server.
    /// </summary>
    public enum PairingAttemptState
    {

        /// <summary>
        /// The pairingAttemptId was issued (step 3); the server waits for step 6A
        /// (requestConnectionDetails) or 6B (postConnectionDetails).
        /// </summary>
        AwaitingConnectionDetails,

        /// <summary>
        /// The connection details were exchanged (step 8A/8B); the server waits for step 9 (finalizePairing).
        /// </summary>
        AwaitingFinalization,

        /// <summary>
        /// The pairing was finalized with success and the pairing is stored.
        /// </summary>
        Succeeded,

        /// <summary>
        /// The attempt failed, see <see cref="PairingAttempt.Failure"/>.
        /// </summary>
        Failed

    }


    /// <summary>
    /// Why a pairing attempt failed.
    /// </summary>
    public enum PairingFailure
    {

        /// <summary>
        /// The attempt has not failed.
        /// </summary>
        None,

        /// <summary>
        /// The serverHmacChallengeResponse of the client was wrong (step 7A/7B, HTTP 403).
        /// </summary>
        InvalidChallengeResponse,

        /// <summary>
        /// A request arrived in the wrong order or for the wrong branch
        /// (S2 Connect 1.0.0, "Invalid interactions", HTTP 400).
        /// </summary>
        InvalidInteraction,

        /// <summary>
        /// The client finalized the attempt with success = false.
        /// </summary>
        ClientReportedFailure,

        /// <summary>
        /// The attempt was not completed within its maximum duration
        /// (S2 Connect 1.0.0, "Interruption of the process").
        /// </summary>
        Timeout,

        /// <summary>
        /// The server could not provide connection details (no session initiation URL).
        /// </summary>
        ConnectionDetailsUnavailable,

        /// <summary>
        /// The pairing could not be persisted.
        /// </summary>
        StoreFailure,

        /// <summary>
        /// The pairing server was shut down.
        /// </summary>
        Shutdown

    }


    /// <summary>
    /// A pairing attempt on the pairing server (S2 Connect 1.0.0, "Pairing interaction"): the
    /// facts agreed on in step 3 (the pairingAttemptId, the targeted server node, the client
    /// node, the selected HMAC hashing algorithm, both challenges and the communication role
    /// the server node will take) and the progress through steps 6A/6B to 10. Secrets (the
    /// pairing token, the expected challenge response, the issued or received access token)
    /// are kept internal and never appear in <see cref="ToString"/> or <see cref="ToJSON"/>.
    /// </summary>
    public sealed class PairingAttempt : IDisposable
    {

        #region Data

        private readonly Lock                                lockObject = new ();
        private readonly Dictionary<String, (Object Request, PairingServerResult Result)>  replays = new (StringComparer.Ordinal);

        private PairingAttemptState  state = PairingAttemptState.AwaitingConnectionDetails;
        private PairingFailure       failure;
        private String?              failureDescription;
        private DateTimeOffset?      completedAt;
        private AccessToken?         issuedAccessToken;
        private ConnectionDetails?   receivedConnectionDetails;
        private Pairing?             result;

        internal PairingToken           Token                                  { get; }
        internal HmacChallengeResponse  ExpectedServerHmacChallengeResponse    { get; }
        internal SemaphoreSlim          Semaphore                              { get; } = new (1, 1);

        #endregion

        #region Properties

        /// <summary>
        /// The secret identification of the attempt (the bearer token of steps 6 to 9).
        /// </summary>
        public PairingAttemptId       Id                              { get; }

        /// <summary>
        /// When the pairingAttemptId was generated.
        /// </summary>
        public DateTimeOffset         CreatedAt                       { get; }

        /// <summary>
        /// When the attempt expires (15 seconds after its creation).
        /// </summary>
        public DateTimeOffset         ExpiresAt                       { get; }

        /// <summary>
        /// The targeted node hosted by the pairing server.
        /// </summary>
        public HostedNode             ServerNode                      { get; }

        /// <summary>
        /// The description of the client node.
        /// </summary>
        public NodeDescription        ClientNodeDescription           { get; }

        /// <summary>
        /// The description of the client endpoint.
        /// </summary>
        public EndpointDescription    ClientEndpointDescription       { get; }

        /// <summary>
        /// The deployment of the client (from its endpoint description or the server's default).
        /// </summary>
        public Deployment             ClientDeployment                { get; }

        /// <summary>
        /// Whether the client asked to pair despite incompatible S2 message versions or
        /// communication protocols.
        /// </summary>
        public Boolean                ForcePairing                    { get; }

        /// <summary>
        /// Whether the server node is the Initiator node of this attempt (the end user entered
        /// the client's pairing code at the server node) or the Responder node (the client
        /// entered the server node's pairing code).
        /// </summary>
        public Boolean                ServerNodeIsInitiator           { get; }

        /// <summary>
        /// The communication role the server node takes after the pairing; it decides between
        /// branch A (requestConnectionDetails, the server is the communication server) and
        /// branch B (postConnectionDetails, the server is the communication client).
        /// </summary>
        public CommunicationRole      ServerCommunicationRole         { get; }

        /// <summary>
        /// The HMAC hashing algorithm selected in step 2.
        /// </summary>
        public HmacHashingAlgorithm   SelectedHmacHashingAlgorithm    { get; }

        /// <summary>
        /// The challenge of the client.
        /// </summary>
        public HmacChallenge          ClientHmacChallenge             { get; }

        /// <summary>
        /// The challenge of the server.
        /// </summary>
        public HmacChallenge          ServerHmacChallenge             { get; }

        /// <summary>
        /// The requestPairing request that started the attempt (for the detection of duplicates).
        /// </summary>
        public RequestPairingRequest  Request                         { get; }

        /// <summary>
        /// The requestPairing response of step 3 (replayed for duplicate requests).
        /// </summary>
        public RequestPairingResponse Response                        { get; }

        /// <summary>
        /// The optional address of the client for audit records.
        /// </summary>
        public String?                RemoteAddress                   { get; }

        /// <summary>
        /// The identification of the client node.
        /// </summary>
        public Node_Id                ClientNodeId
            => ClientNodeDescription.Id;

        /// <summary>
        /// Whether branch A (requestConnectionDetails) is expected.
        /// </summary>
        public Boolean                ExpectsRequestConnectionDetails
            => ServerCommunicationRole == CommunicationRole.CommunicationServer;

        /// <summary>
        /// The current state of the attempt.
        /// </summary>
        public PairingAttemptState    State
        {
            get
            {
                lock (lockObject)
                {
                    return state;
                }
            }
        }

        /// <summary>
        /// Why the attempt failed (<see cref="PairingFailure.None"/> while it is active or succeeded).
        /// </summary>
        public PairingFailure         Failure
        {
            get
            {
                lock (lockObject)
                {
                    return failure;
                }
            }
        }

        /// <summary>
        /// An optional description of the failure.
        /// </summary>
        public String?                FailureDescription
        {
            get
            {
                lock (lockObject)
                {
                    return failureDescription;
                }
            }
        }

        /// <summary>
        /// When the attempt succeeded or failed.
        /// </summary>
        public DateTimeOffset?        CompletedAt
        {
            get
            {
                lock (lockObject)
                {
                    return completedAt;
                }
            }
        }

        /// <summary>
        /// The stored pairing of a succeeded attempt.
        /// </summary>
        public Pairing?               Result
        {
            get
            {
                lock (lockObject)
                {
                    return result;
                }
            }
        }

        /// <summary>
        /// Whether the attempt is still in progress.
        /// </summary>
        public Boolean                IsActive
        {
            get
            {
                lock (lockObject)
                {
                    return state is PairingAttemptState.AwaitingConnectionDetails or PairingAttemptState.AwaitingFinalization;
                }
            }
        }

        /// <summary>
        /// Whether the attempt exceeded its maximum duration at the given time.
        /// </summary>
        /// <param name="Now">The current time.</param>
        public Boolean HasExpired(DateTimeOffset Now)
            => Now >= ExpiresAt;

        internal AccessToken?       IssuedAccessToken
        {
            get
            {
                lock (lockObject)
                {
                    return issuedAccessToken;
                }
            }
        }

        internal ConnectionDetails? ReceivedConnectionDetails
        {
            get
            {
                lock (lockObject)
                {
                    return receivedConnectionDetails;
                }
            }
        }

        #endregion

        #region Constructor(s)

        internal PairingAttempt(PairingAttemptId        Id,
                                DateTimeOffset          CreatedAt,
                                DateTimeOffset          ExpiresAt,
                                HostedNode              ServerNode,
                                RequestPairingRequest   Request,
                                RequestPairingResponse  Response,
                                Deployment              ClientDeployment,
                                Boolean                 ServerNodeIsInitiator,
                                CommunicationRole       ServerCommunicationRole,
                                PairingToken            Token,
                                HmacChallengeResponse   ExpectedServerHmacChallengeResponse,
                                String?                 RemoteAddress)
        {

            this.Id                                   = Id;
            this.CreatedAt                            = CreatedAt;
            this.ExpiresAt                            = ExpiresAt;
            this.ServerNode                           = ServerNode;
            this.Request                              = Request;
            this.Response                             = Response;
            this.ClientNodeDescription                = Request.ClientNodeDescription;
            this.ClientEndpointDescription            = Request.ClientEndpointDescription;
            this.ClientDeployment                     = ClientDeployment;
            this.ForcePairing                         = Request.ForcePairing;
            this.ServerNodeIsInitiator                = ServerNodeIsInitiator;
            this.ServerCommunicationRole              = ServerCommunicationRole;
            this.SelectedHmacHashingAlgorithm         = Response.SelectedHmacHashingAlgorithm;
            this.ClientHmacChallenge                  = Request.ClientHmacChallenge;
            this.ServerHmacChallenge                  = Response.ServerHmacChallenge;
            this.Token                                = Token;
            this.ExpectedServerHmacChallengeResponse  = ExpectedServerHmacChallengeResponse;
            this.RemoteAddress                        = RemoteAddress;

        }

        #endregion


        #region (internal) State transitions

        internal Boolean TryGetReplay(String                                         Operation,
                                      Object                                         Request,
                                      [NotNullWhen(true)] out PairingServerResult?  Result)
        {

            lock (lockObject)
            {

                if (replays.TryGetValue(Operation, out var replay) &&
                    replay.Request.Equals(Request))
                {
                    Result = replay.Result;
                    return true;
                }

            }

            Result = null;
            return false;

        }

        internal void RecordReplay(String               Operation,
                                   Object               Request,
                                   PairingServerResult  Result)
        {
            lock (lockObject)
            {
                replays[Operation] = (Request, Result);
            }
        }

        internal void MarkConnectionDetailsExchanged(AccessToken?        IssuedAccessToken,
                                                     ConnectionDetails?  ReceivedConnectionDetails)
        {
            lock (lockObject)
            {
                state                          = PairingAttemptState.AwaitingFinalization;
                issuedAccessToken              = IssuedAccessToken;
                receivedConnectionDetails      = ReceivedConnectionDetails;
            }
        }

        internal Boolean MarkFailed(PairingFailure  Failure,
                                    String?         Description,
                                    DateTimeOffset  Now)
        {

            lock (lockObject)
            {

                if (state is PairingAttemptState.Succeeded or PairingAttemptState.Failed)
                    return false;

                state               = PairingAttemptState.Failed;
                failure             = Failure;
                failureDescription  = Description;
                completedAt         = Now;

                return true;

            }

        }

        internal void MarkSucceeded(Pairing         Pairing,
                                    DateTimeOffset  Now)
        {
            lock (lockObject)
            {
                state        = PairingAttemptState.Succeeded;
                result       = Pairing;
                completedAt  = Now;
            }
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return an audit record of this attempt (PLAN.md §3.6): the pairingAttemptId is
        /// referenced by a truncated SHA-256 hash, no secret is included.
        /// </summary>
        public JObject ToJSON()
        {

            lock (lockObject)
            {

                return JSONObject.Create(

                           new JProperty("pairingAttemptIdHash",         HashOf(Id.Value)),
                           new JProperty("createdAt",                    CreatedAt.ToS2Timestamp()),
                           new JProperty("expiresAt",                    ExpiresAt.ToS2Timestamp()),
                           new JProperty("serverNodeId",                 ServerNode.Id.ToString()),
                           new JProperty("serverNodeRole",               ServerNode.Role.ToString()),
                           new JProperty("clientNodeId",                 ClientNodeId.ToString()),
                           new JProperty("clientNodeRole",               ClientNodeDescription.Role.ToString()),
                           new JProperty("clientDeployment",             ClientDeployment.ToString()),
                           new JProperty("forcePairing",                 ForcePairing),
                           new JProperty("serverNodeIsInitiator",        ServerNodeIsInitiator),
                           new JProperty("serverCommunicationRole",      ServerCommunicationRole.AsText()),
                           new JProperty("selectedHmacHashingAlgorithm", SelectedHmacHashingAlgorithm.ToString()),
                           new JProperty("state",                        state.ToString()),
                           new JProperty("failure",                      failure.ToString()),

                           failureDescription is not null
                               ? new JProperty("failureDescription",     failureDescription)
                               : null,

                           completedAt.HasValue
                               ? new JProperty("completedAt",            completedAt.Value.ToS2Timestamp())
                               : null,

                           RemoteAddress is not null
                               ? new JProperty("remoteAddress",          RemoteAddress)
                               : null

                       );

            }

        }

        private static String HashOf(String Text)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Text)), 0, 8);

        #endregion

        #region Dispose()

        /// <summary>
        /// Release the resources of this attempt.
        /// </summary>
        public void Dispose()
        {
            Semaphore.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object (without any secret).
        /// </summary>
        public override String ToString()
        {

            lock (lockObject)
            {
                return $"Pairing attempt {HashOf(Id.Value)}: server node {ServerNode.Id} <-> client node {ClientNodeId}, {state}{(failure != PairingFailure.None ? $" ({failure})" : "")}";
            }

        }

        #endregion

    }

}

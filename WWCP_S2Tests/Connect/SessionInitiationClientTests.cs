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

using cloud.charging.open.protocols.S2.Connect;
using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// The session initiation client of a communication client against the Phase 8 session
    /// initiation server over real HTTP and WebSockets (S2 Connect 1.0.0, "Session initiation",
    /// "Unpairing by the communication client"): the candidate access tokens, the mapping of the
    /// server's answers to client outcomes, the persistence order of the pending token, the
    /// opening of the WebSocket session and the client options.
    /// </summary>
    [TestFixture]
    public sealed class SessionInitiationClientTests
    {

        #region Data

        /// <summary>
        /// A session initiation URL nobody listens on.
        /// </summary>
        private static readonly S2BaseURL  UnreachableSessionInitiationUrl  = S2BaseURL.Parse("http://127.0.0.1:1/connection/", AllowHTTP: true);

        #endregion

        #region FailingStore

        /// <summary>
        /// A store forwarding every operation to an inner store, except those listed in
        /// <see cref="FailingOperations"/>, which fail as if the disk was full.
        /// </summary>
        private sealed class FailingStore(IS2Store Inner) : IS2Store
        {

            public HashSet<String> FailingOperations { get; } = [];

            private Boolean Fails(String Operation)
                => FailingOperations.Contains(Operation);

            private static IOException DiskFull()
                => new ("disk full");


            public ValueTask<IReadOnlyList<Pairing>> GetPairingsAsync(Node_Id?           LocalNodeId         = null,
                                                                      CancellationToken  CancellationToken   = default)

                => Fails(nameof(GetPairingsAsync))
                       ? ValueTask.FromException<IReadOnlyList<Pairing>>(DiskFull())
                       : Inner.GetPairingsAsync(LocalNodeId, CancellationToken);


            public ValueTask<Pairing?> GetPairingAsync(Node_Id            LocalNodeId,
                                                       Node_Id            RemoteNodeId,
                                                       CancellationToken  CancellationToken   = default)

                => Fails(nameof(GetPairingAsync))
                       ? ValueTask.FromException<Pairing?>(DiskFull())
                       : Inner.GetPairingAsync(LocalNodeId, RemoteNodeId, CancellationToken);


            public ValueTask<Pairing?> AddOrReplacePairingAsync(Pairing            Pairing,
                                                                CancellationToken  CancellationToken   = default)

                => Fails(nameof(AddOrReplacePairingAsync))
                       ? ValueTask.FromException<Pairing?>(DiskFull())
                       : Inner.AddOrReplacePairingAsync(Pairing, CancellationToken);


            public ValueTask<Pairing?> RemovePairingAsync(Node_Id            LocalNodeId,
                                                          Node_Id            RemoteNodeId,
                                                          CancellationToken  CancellationToken   = default)

                => Fails(nameof(RemovePairingAsync))
                       ? ValueTask.FromException<Pairing?>(DiskFull())
                       : Inner.RemovePairingAsync(LocalNodeId, RemoteNodeId, CancellationToken);


            public ValueTask AddPendingAccessTokenAsync(PendingAccessToken  PendingAccessToken,
                                                        CancellationToken   CancellationToken   = default)

                => Fails(nameof(AddPendingAccessTokenAsync))
                       ? ValueTask.FromException(DiskFull())
                       : Inner.AddPendingAccessTokenAsync(PendingAccessToken, CancellationToken);


            public ValueTask<IReadOnlyList<PendingAccessToken>> GetPendingAccessTokensAsync(Node_Id            LocalNodeId,
                                                                                            Node_Id            RemoteNodeId,
                                                                                            CancellationToken  CancellationToken   = default)

                => Fails(nameof(GetPendingAccessTokensAsync))
                       ? ValueTask.FromException<IReadOnlyList<PendingAccessToken>>(DiskFull())
                       : Inner.GetPendingAccessTokensAsync(LocalNodeId, RemoteNodeId, CancellationToken);


            public ValueTask<PendingAccessToken?> FindPendingAccessTokenAsync(AccessToken        Token,
                                                                              CancellationToken  CancellationToken   = default)

                => Fails(nameof(FindPendingAccessTokenAsync))
                       ? ValueTask.FromException<PendingAccessToken?>(DiskFull())
                       : Inner.FindPendingAccessTokenAsync(Token, CancellationToken);


            public ValueTask<Pairing?> ActivateAccessTokenAsync(Node_Id            LocalNodeId,
                                                                Node_Id            RemoteNodeId,
                                                                AccessToken        Token,
                                                                CancellationToken  CancellationToken   = default)

                => Fails(nameof(ActivateAccessTokenAsync))
                       ? ValueTask.FromException<Pairing?>(DiskFull())
                       : Inner.ActivateAccessTokenAsync(LocalNodeId, RemoteNodeId, Token, CancellationToken);


            public ValueTask<Int32> RemovePendingAccessTokensAsync(Node_Id?           LocalNodeId         = null,
                                                                   Node_Id?           RemoteNodeId        = null,
                                                                   DateTimeOffset?    CreatedBefore       = null,
                                                                   CancellationToken  CancellationToken   = default)

                => Fails(nameof(RemovePendingAccessTokensAsync))
                       ? ValueTask.FromException<Int32>(DiskFull())
                       : Inner.RemovePendingAccessTokensAsync(LocalNodeId, RemoteNodeId, CreatedBefore, CancellationToken);


            public ValueTask<IReadOnlyList<AccessToken>> GetAccessTokenCandidatesAsync(Node_Id            LocalNodeId,
                                                                                       Node_Id            RemoteNodeId,
                                                                                       CancellationToken  CancellationToken   = default)

                => Fails(nameof(GetAccessTokenCandidatesAsync))
                       ? ValueTask.FromException<IReadOnlyList<AccessToken>>(DiskFull())
                       : Inner.GetAccessTokenCandidatesAsync(LocalNodeId, RemoteNodeId, CancellationToken);


            public ValueTask<Pairing?> UnpairAsync(Node_Id            LocalNodeId,
                                                   Node_Id            RemoteNodeId,
                                                   DateTimeOffset     At,
                                                   CancellationToken  CancellationToken   = default)

                => Fails(nameof(UnpairAsync))
                       ? ValueTask.FromException<Pairing?>(DiskFull())
                       : Inner.UnpairAsync(LocalNodeId, RemoteNodeId, At, CancellationToken);


            public ValueTask<DateTimeOffset?> GetUnpairedAtAsync(Node_Id            LocalNodeId,
                                                                 Node_Id            RemoteNodeId,
                                                                 CancellationToken  CancellationToken   = default)

                => Fails(nameof(GetUnpairedAtAsync))
                       ? ValueTask.FromException<DateTimeOffset?>(DiskFull())
                       : Inner.GetUnpairedAtAsync(LocalNodeId, RemoteNodeId, CancellationToken);

        }

        #endregion

        #region Helpers

        /// <summary>
        /// Poll the given condition for at most five seconds.
        /// </summary>
        private static async Task WaitUntilAsync(Func<Boolean>  Condition,
                                                 String         Description)
        {

            var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);

            while (!Condition())
            {

                if (DateTimeOffset.UtcNow > deadline)
                    Assert.Fail($"Timed out waiting for {Description}!");

                await Task.Delay(20);

            }

        }

        /// <summary>
        /// The pairing of the RM (communication client) with the CEM in the client store.
        /// </summary>
        private static async Task<Pairing?> ClientPairingAsync(SessionInitiationFixture Fixture)
            => await Fixture.ClientStore.GetPairingAsync(Fixture.RM.Id, Fixture.CEM.Id);

        /// <summary>
        /// The pairing of the CEM (communication server) with the RM in the server store.
        /// </summary>
        private static async Task<Pairing?> ServerPairingAsync(SessionInitiationFixture Fixture)
            => await Fixture.ServerStore.GetPairingAsync(Fixture.CEM.Id, Fixture.RM.Id);

        /// <summary>
        /// Replace the active access token of the client with one the server does not know.
        /// </summary>
        private static async Task<AccessToken> ReplaceClientTokenAsync(SessionInitiationFixture Fixture)
        {

            var wrongToken  = TokenGenerator.NewAccessToken();
            var pairing     = await ClientPairingAsync(Fixture);

            Assert.That(pairing, Is.Not.Null, "the fixture is paired");

            await Fixture.ClientStore.AddOrReplacePairingAsync(pairing!.WithAccessToken(wrongToken));

            return wrongToken;

        }

        /// <summary>
        /// A session initiation client of the RM of the fixture whose server nobody listens on.
        /// </summary>
        private static SessionInitiationClient UnreachableClient(SessionInitiationFixture Fixture)

            => new (UnreachableSessionInitiationUrl,
                    Fixture.ClientEndpoint,
                    Fixture.ClientStore,
                    SessionInitiationFixture.DefaultClientOptions());

        /// <summary>
        /// The number of sessions the WebSocket server started so far.
        /// </summary>
        private static Int32 ServerSessionCount(SessionInitiationFixture Fixture)
        {
            lock (Fixture.ServerSessions)
            {
                return Fixture.ServerSessions.Count;
            }
        }

        /// <summary>
        /// The session the WebSocket server started at the given index.
        /// </summary>
        private static S2Session ServerSessionAt(SessionInitiationFixture  Fixture,
                                                 Int32                     Index)
        {
            lock (Fixture.ServerSessions)
            {
                return Fixture.ServerSessions[Index];
            }
        }

        #endregion


        // Session initiation (S2 Connect 1.0.0, "Session initiation", steps 0 to 8)

        #region InitiateSession_RotatesTheToken_AndReturnsTheCommunicationDetails()

        [Test]
        [S2C("SessionInitiation.1")]
        [S2C("SessionInitiation.3")]
        [S2C("SessionInitiation.5")]
        [S2C("SessionInitiation.7")]
        [S2C("SessionInitiation.8")]
        public async Task InitiateSession_RotatesTheToken_AndReturnsTheCommunicationDetails()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var oldToken  = (await ClientPairingAsync(fixture))!.AccessToken;

            var result    = await fixture.Client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id);

            Assert.That(result.Outcome, Is.EqualTo(SessionInitiationOutcome.Success), result.ToString());

            var clientPairing  = await ClientPairingAsync(fixture);
            var serverPairing  = await ServerPairingAsync(fixture);

            Assert.That(clientPairing, Is.Not.Null);
            Assert.That(serverPairing, Is.Not.Null);

            Assert.Multiple(() => {

                Assert.That(result.IsSuccess,                                     Is.True);
                Assert.That(result.Operation,                                     Is.EqualTo("confirmAccessToken"), "the outcome was decided by the 200 of step 7");
                Assert.That(result.StatusCode?.Code,                              Is.EqualTo(200));
                Assert.That(result.Error,                                         Is.Null);
                Assert.That(result.Retryable,                                     Is.False);
                Assert.That(result.SelectedCommunicationProtocol,                 Is.EqualTo(CommunicationProtocol.WebSocket));
                Assert.That(result.SelectedS2MessageVersion,                      Is.EqualTo(Version.S2JSONVersion));

                Assert.That(result.CommunicationDetails,                          Is.InstanceOf<WebSocketCommunicationDetails>());
                Assert.That((result.CommunicationDetails as WebSocketCommunicationDetails)?.WebsocketUrl,
                                                                                  Is.EqualTo(fixture.WebSocketUrl));
                Assert.That((result.CommunicationDetails as WebSocketCommunicationDetails)?.WebsocketToken.Length,
                                                                                  Is.GreaterThanOrEqualTo(S2ConnectDefaults.MinCommunicationTokenLength));

                Assert.That(result.Pairing,                                       Is.Not.Null);
                Assert.That(result.Pairing!.AccessToken.Equals(oldToken),         Is.False, "the access token was rotated");
                Assert.That(result.Pairing!.AccessToken,                          Is.EqualTo(clientPairing!.AccessToken), "the result carries the activated token");
                Assert.That(result.Pairing!.LocalNodeId,                          Is.EqualTo(fixture.RM.Id));
                Assert.That(result.Pairing!.RemoteNodeId,                         Is.EqualTo(fixture.CEM.Id));
                Assert.That(clientPairing!.AccessToken,                           Is.EqualTo(serverPairing!.AccessToken), "both sides hold the same new token");
                Assert.That(fixture.ClientStore.PendingCount,                     Is.EqualTo(0), "the client removed its pending token in step 8");
                Assert.That(fixture.ServerStore.PendingCount,                     Is.EqualTo(0), "the server removed its pending token in step 6");
                Assert.That(fixture.ClientResults.Select(r => r.Outcome),         Is.EqualTo(new[] { SessionInitiationOutcome.Success }));
                Assert.That(fixture.ClientResults[0],                             Is.SameAs(result));

            });

        }

        #endregion

        #region InitiateSession_TriesThePersistedCandidateTokens_AfterACrashBetweenStep4And8()

        [Test]
        [S2C("SessionInitiation.1.CandidateTokens")]
        [S2C("SessionInitiation.4")]
        public async Task InitiateSession_TriesThePersistedCandidateTokens_AfterACrashBetweenStep4And8()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            // The last session initiation crashed between step 4 and step 8: the client persisted the
            // pending token B and the server activated it in step 6, but the client never activated it.
            var tokenA         = await fixture.PairAsync();
            var tokenB         = TokenGenerator.NewAccessToken();

            var serverPairing  = await ServerPairingAsync(fixture);

            await fixture.ServerStore.AddOrReplacePairingAsync(serverPairing!.WithAccessToken(tokenB));
            await fixture.ClientStore.AddPendingAccessTokenAsync(new PendingAccessToken(fixture.RM.Id, fixture.CEM.Id, tokenB, DateTimeOffset.UtcNow));

            var candidates     = await fixture.ClientStore.GetAccessTokenCandidatesAsync(fixture.RM.Id, fixture.CEM.Id);

            Assert.That(candidates, Is.EqualTo(new[] { tokenA, tokenB }), "the active token is tried first, then the pending token");

            var result         = await fixture.Client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id);

            Assert.That(result.Outcome, Is.EqualTo(SessionInitiationOutcome.Success), result.ToString());

            var clientPairing     = await ClientPairingAsync(fixture);
            var newServerPairing  = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(clientPairing,                                Is.Not.Null);
                Assert.That(clientPairing!.AccessToken.Equals(tokenA),    Is.False, "A was answered with 401");
                Assert.That(clientPairing.AccessToken.Equals(tokenB),     Is.False, "B was accepted and rotated");
                Assert.That(clientPairing.AccessToken,                    Is.EqualTo(newServerPairing!.AccessToken), "the client activated the newly rotated token");
                Assert.That(result.Pairing?.AccessToken,                  Is.EqualTo(clientPairing.AccessToken));
                Assert.That(fixture.ClientStore.PendingCount,             Is.EqualTo(0), "the stale pending token B and the new pending token are gone");
                Assert.That(fixture.ServerStore.PendingCount,             Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_OnlyAWrongTokenKnown_ReturnsUnauthorized()

        [Test]
        [S2C("SessionInitiation.1.Unauthorized")]
        public async Task InitiateSession_OnlyAWrongTokenKnown_ReturnsUnauthorized()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var serverToken  = (await ServerPairingAsync(fixture))!.AccessToken;
            var wrongToken   = await ReplaceClientTokenAsync(fixture);

            var result       = await fixture.Client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id);

            var clientPairing  = await ClientPairingAsync(fixture);
            var serverPairing  = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                               Is.EqualTo(SessionInitiationOutcome.Unauthorized), result.ToString());
                Assert.That(result.IsSuccess,                             Is.False);
                Assert.That(result.Operation,                             Is.EqualTo("initiateSession"));
                Assert.That(result.StatusCode?.Code,                      Is.EqualTo(401));
                Assert.That(result.Error,                                 Is.Null);
                Assert.That(result.Retryable,                             Is.False, "do not retry, inform the end user");
                Assert.That(result.Pairing,                               Is.Null);
                Assert.That(result.CommunicationDetails,                  Is.Null);
                Assert.That(clientPairing?.AccessToken,                   Is.EqualTo(wrongToken), "the local pairing is kept");
                Assert.That(serverPairing?.AccessToken,                   Is.EqualTo(serverToken), "the server rotated nothing");
                Assert.That(fixture.ClientStore.PendingCount,             Is.EqualTo(0));
                Assert.That(fixture.ServerStore.PendingCount,             Is.EqualTo(0));
                Assert.That(fixture.ClientResults.Select(r => r.Outcome), Is.EqualTo(new[] { SessionInitiationOutcome.Unauthorized }));
            });

        }

        #endregion

        #region InitiateSession_NotPairedLocally_ReturnsInvalidConfiguration()

        [Test]
        [S2C("SessionInitiation.Precondition")]
        public async Task InitiateSession_NotPairedLocally_ReturnsInvalidConfiguration()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(Paired: false);

            var result = await fixture.Client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                               Is.EqualTo(SessionInitiationOutcome.InvalidConfiguration), result.ToString());
                Assert.That(result.IsSuccess,                             Is.False);
                Assert.That(result.Operation,                             Is.EqualTo("initiateSession"));
                Assert.That(result.StatusCode,                            Is.Null, "no request was sent");
                Assert.That(result.Retryable,                             Is.False);
                Assert.That(result.Description,                           Does.Contain("not paired"));
                Assert.That(fixture.Client.SelectedAPIVersion,            Is.Null, "not even the version index was fetched");
                Assert.That(fixture.ClientResults.Select(r => r.Outcome), Is.EqualTo(new[] { SessionInitiationOutcome.InvalidConfiguration }));
            });

        }

        #endregion

        #region InitiateSession_AfterUnpairingByTheServer_ReturnsNoLongerPaired_AndRemovesTheLocalPairing()

        [Test]
        [S2C("SessionInitiation.1.NoLongerPaired")]
        [S2C("Unpairing.ByCommunicationServer")]
        public async Task InitiateSession_AfterUnpairingByTheServer_ReturnsNoLongerPaired_AndRemovesTheLocalPairing()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var removed = await fixture.API.UnpairLocallyAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.That(removed, Is.Not.Null);

            var result = await fixture.Client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id);

            var clientPairing     = await ClientPairingAsync(fixture);
            var clientUnpairedAt  = await fixture.ClientStore.GetUnpairedAtAsync(fixture.RM.Id, fixture.CEM.Id);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                               Is.EqualTo(SessionInitiationOutcome.NoLongerPaired), result.ToString());
                Assert.That(result.IsSuccess,                             Is.False);
                Assert.That(result.Operation,                             Is.EqualTo("initiateSession"));
                Assert.That(result.StatusCode?.Code,                      Is.EqualTo(400));
                Assert.That(result.Error?.ErrorMessage,                   Is.EqualTo(CommunicationDetailsError.NoLongerPaired));
                Assert.That(result.Retryable,                             Is.False, "do not retry, inform the end user");
                Assert.That(result.Pairing,                               Is.Null);
                Assert.That(clientPairing,                                Is.Null, "the client removed its security material");
                Assert.That(clientUnpairedAt,                             Is.Not.Null, "the client wrote a tombstone");
                Assert.That(fixture.ClientStore.PendingCount,             Is.EqualTo(0));
                Assert.That(fixture.ServerStore.PendingCount,             Is.EqualTo(0));
                Assert.That(fixture.UnpairedAtServer,                     Has.Count.EqualTo(1));
                Assert.That(fixture.ClientResults.Select(r => r.Outcome), Is.EqualTo(new[] { SessionInitiationOutcome.NoLongerPaired }));
            });

        }

        #endregion

        #region InitiateSession_NoLongerPaired_KeepsTheLocalPairing_WhenConfigured()

        [Test]
        [S2C("SessionInitiation.1.NoLongerPaired")]
        public async Task InitiateSession_NoLongerPaired_KeepsTheLocalPairing_WhenConfigured()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                          ClientOptions: SessionInitiationFixture.DefaultClientOptions() with {
                                                             RemovePairingWhenNoLongerPaired = false
                                                         }
                                      );

            var token   = (await ClientPairingAsync(fixture))!.AccessToken;

            await fixture.API.UnpairLocallyAsync(fixture.CEM.Id, fixture.RM.Id);

            var result  = await fixture.Client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id);

            var clientPairing     = await ClientPairingAsync(fixture);
            var clientUnpairedAt  = await fixture.ClientStore.GetUnpairedAtAsync(fixture.RM.Id, fixture.CEM.Id);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,               Is.EqualTo(SessionInitiationOutcome.NoLongerPaired), result.ToString());
                Assert.That(result.Error?.ErrorMessage,   Is.EqualTo(CommunicationDetailsError.NoLongerPaired));
                Assert.That(result.Retryable,             Is.False);
                Assert.That(clientPairing,                Is.Not.Null, "the local pairing is kept for the host to decide");
                Assert.That(clientPairing?.AccessToken,   Is.EqualTo(token));
                Assert.That(clientUnpairedAt,             Is.Null, "no tombstone was written");
            });

        }

        #endregion

        #region InitiateSession_ServerNodeNotReady_ReturnsRetryLater()

        [Test]
        [S2C("SessionInitiation.1.Other")]
        public async Task InitiateSession_ServerNodeNotReady_ReturnsRetryLater()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var token = (await ClientPairingAsync(fixture))!.AccessToken;

            fixture.CEM.IsReadyForPairing = false;

            var result = await fixture.Client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id);

            var clientPairing  = await ClientPairingAsync(fixture);
            var serverPairing  = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                   Is.EqualTo(SessionInitiationOutcome.RetryLater), result.ToString());
                Assert.That(result.IsSuccess,                 Is.False);
                Assert.That(result.Operation,                 Is.EqualTo("initiateSession"));
                Assert.That(result.StatusCode?.Code,          Is.EqualTo(400));
                Assert.That(result.Error?.ErrorMessage,       Is.EqualTo(CommunicationDetailsError.Other));
                Assert.That(result.Retryable,                 Is.True, "retry later starting at step 1");
                Assert.That(result.Pairing,                   Is.Null);
                Assert.That(clientPairing?.AccessToken,       Is.EqualTo(token), "the client token is unchanged");
                Assert.That(serverPairing?.AccessToken,       Is.EqualTo(token), "the server token is unchanged");
                Assert.That(fixture.ClientStore.PendingCount, Is.EqualTo(0));
                Assert.That(fixture.ServerStore.PendingCount, Is.EqualTo(0), "the server rejected the request before generating a pending token");
            });

        }

        #endregion

        #region InitiateSession_ServerShutDown_RetriesThe503_ThenReturnsRetryLater()

        [Test]
        [S2C("SessionInitiation.1.ServiceUnavailable")]
        public async Task InitiateSession_ServerShutDown_RetriesThe503_ThenReturnsRetryLater()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var token = (await ClientPairingAsync(fixture))!.AccessToken;

            fixture.API.Shutdown();

            Assert.That(fixture.API.IsShutdown, Is.True);

            // Every 503 of the shut down server carries "Retry-After: 1", which takes precedence over
            // the 100 ms retry delay of the fixture: the three retries take about three seconds.
            var result = await fixture.Client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id);

            var clientPairing  = await ClientPairingAsync(fixture);
            var serverPairing  = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                   Is.EqualTo(SessionInitiationOutcome.RetryLater), result.ToString());
                Assert.That(result.IsSuccess,                 Is.False);
                Assert.That(result.Operation,                 Is.EqualTo("initiateSession"));
                Assert.That(result.StatusCode?.Code,          Is.EqualTo(503));
                Assert.That(result.Error,                     Is.Null);
                Assert.That(result.Retryable,                 Is.True);
                Assert.That(result.Pairing,                   Is.Null);
                Assert.That(fixture.Client.SelectedAPIVersion, Is.EqualTo("v1"), "the version index is still served");
                Assert.That(clientPairing?.AccessToken,       Is.EqualTo(token));
                Assert.That(serverPairing?.AccessToken,       Is.EqualTo(token));
                Assert.That(fixture.ClientStore.PendingCount, Is.EqualTo(0));
                Assert.That(fixture.ServerStore.PendingCount, Is.EqualTo(0));
            });

        }

        #endregion

        #region InitiateSession_PersistingThePendingTokenFails_ReturnsStoreFailure_BeforeConfirming()

        [Test]
        [S2C("SessionInitiation.4")]
        public async Task InitiateSession_PersistingThePendingTokenFails_ReturnsStoreFailure_BeforeConfirming()
        {

            await using var fixture  = await SessionInitiationFixture.CreateAsync();

            var failing              = new FailingStore(fixture.ClientStore);

            await using var client   = new SessionInitiationClient(fixture.SessionInitiationUrl,
                                                                   fixture.ClientEndpoint,
                                                                   failing,
                                                                   SessionInitiationFixture.DefaultClientOptions());

            var token = (await ClientPairingAsync(fixture))!.AccessToken;

            failing.FailingOperations.Add(nameof(IS2Store.AddPendingAccessTokenAsync));

            var result = await client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id);

            var clientPairing  = await ClientPairingAsync(fixture);
            var serverPairing  = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                   Is.EqualTo(SessionInitiationOutcome.StoreFailure), result.ToString());
                Assert.That(result.IsSuccess,                 Is.False);
                Assert.That(result.Operation,                 Is.EqualTo("initiateSession"), "the procedure was aborted before confirmAccessToken");
                Assert.That(result.StatusCode,                Is.Null);
                Assert.That(result.Retryable,                 Is.True);
                Assert.That(result.Description,               Is.EqualTo("disk full"));
                Assert.That(result.Pairing,                   Is.Null);
                Assert.That(clientPairing?.AccessToken,       Is.EqualTo(token), "the client still uses its old token");
                Assert.That(fixture.ClientStore.PendingCount, Is.EqualTo(0), "nothing was persisted");
                Assert.That(serverPairing?.AccessToken,       Is.EqualTo(token), "the server did not activate anything");
                Assert.That(fixture.ServerStore.PendingCount, Is.EqualTo(1), "the pending token of the server stays unconfirmed");
            });

        }

        #endregion

        #region InitiateSession_ActivatingTheTokenFails_ReturnsStoreFailure_AndTheNextAttemptRecoversViaThePendingToken()

        [Test]
        [S2C("SessionInitiation.8")]
        [S2C("SessionInitiation.1.CandidateTokens")]
        public async Task InitiateSession_ActivatingTheTokenFails_ReturnsStoreFailure_AndTheNextAttemptRecoversViaThePendingToken()
        {

            await using var fixture  = await SessionInitiationFixture.CreateAsync();

            var failing              = new FailingStore(fixture.ClientStore);

            await using var client   = new SessionInitiationClient(fixture.SessionInitiationUrl,
                                                                   fixture.ClientEndpoint,
                                                                   failing,
                                                                   SessionInitiationFixture.DefaultClientOptions());

            var oldToken = (await ClientPairingAsync(fixture))!.AccessToken;

            #region 1. The activation of step 8 fails: the server already activated the new token

            failing.FailingOperations.Add(nameof(IS2Store.ActivateAccessTokenAsync));

            var first = await client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id);

            var clientPairingAfterFailure  = await ClientPairingAsync(fixture);
            var serverPairingAfterFailure  = await ServerPairingAsync(fixture);
            var clientPendingAfterFailure  = await fixture.ClientStore.GetPendingAccessTokensAsync(fixture.RM.Id, fixture.CEM.Id);

            Assert.Multiple(() => {
                Assert.That(first.Outcome,                                          Is.EqualTo(SessionInitiationOutcome.StoreFailure), first.ToString());
                Assert.That(first.Operation,                                        Is.EqualTo("confirmAccessToken"));
                Assert.That(first.Retryable,                                        Is.True);
                Assert.That(first.Description,                                      Is.EqualTo("disk full"));
                Assert.That(first.Pairing,                                          Is.Null);
                Assert.That(clientPairingAfterFailure?.AccessToken,                 Is.EqualTo(oldToken), "the client still holds the old token");
                Assert.That(serverPairingAfterFailure!.AccessToken.Equals(oldToken), Is.False, "the server activated the new token in step 6");
                Assert.That(fixture.ServerStore.PendingCount,                       Is.EqualTo(0));
                Assert.That(clientPendingAfterFailure,                              Has.Count.EqualTo(1), "the pending token persisted in step 4 stays a candidate");
                Assert.That(clientPendingAfterFailure[0].Token,                     Is.EqualTo(serverPairingAfterFailure.AccessToken));
            });

            #endregion

            #region 2. The next attempt succeeds with the pending token as candidate

            failing.FailingOperations.Clear();

            var second = await client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id);

            var clientPairing  = await ClientPairingAsync(fixture);
            var serverPairing  = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(second.Outcome,                               Is.EqualTo(SessionInitiationOutcome.Success), second.ToString());
                Assert.That(clientPairing,                                Is.Not.Null);
                Assert.That(clientPairing!.AccessToken.Equals(oldToken),  Is.False);
                Assert.That(clientPairing.AccessToken,                    Is.EqualTo(serverPairing!.AccessToken), "both sides hold the same token again");
                Assert.That(fixture.ClientStore.PendingCount,             Is.EqualTo(0));
                Assert.That(fixture.ServerStore.PendingCount,             Is.EqualTo(0));
            });

            #endregion

        }

        #endregion

        #region InitiateSession_ServerUnreachable_ReturnsTransportFailure()

        [Test]
        [S2C("SessionInitiation.0")]
        public async Task InitiateSession_ServerUnreachable_ReturnsTransportFailure()
        {

            await using var fixture  = await SessionInitiationFixture.CreateAsync();
            await using var client   = UnreachableClient(fixture);

            var token = (await ClientPairingAsync(fixture))!.AccessToken;

            // Nothing listens on port 1: the connection is refused at once; the token only bounds the test.
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var result = await client.InitiateSessionAsync(fixture.RM, fixture.CEM.Id, timeout.Token);

            var clientPairing = await ClientPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                   Is.EqualTo(SessionInitiationOutcome.TransportFailure), result.ToString());
                Assert.That(result.IsSuccess,                 Is.False);
                Assert.That(result.Operation,                 Is.EqualTo("versionIndex"), "the version index is the first request of every procedure");
                Assert.That(result.StatusCode?.Code,          Is.EqualTo(0), "no server ever answered");
                Assert.That(result.Retryable,                 Is.True);
                Assert.That(result.Pairing,                   Is.Null);
                Assert.That(client.SelectedAPIVersion,        Is.Null);
                Assert.That(clientPairing?.AccessToken,       Is.EqualTo(token), "the local pairing is kept");
                Assert.That(fixture.ClientStore.PendingCount, Is.EqualTo(0));
            });

        }

        #endregion


        // Opening the session (S2 Connect 1.0.0, "WebSocket based communication")

        #region Connect_OpensTheSession_AndClosingItEndsTheServerSession()

        [Test]
        [S2C("SessionInitiation.WebSocket")]
        public async Task Connect_OpensTheSession_AndClosingItEndsTheServerSession()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var connect = await fixture.Client.ConnectAsync(fixture.RM, fixture.CEM.Id);

            Assert.That(connect.IsSuccess, Is.True, connect.Initiation.ToString());

            await using var session = connect.Session!;

            await WaitUntilAsync(() => ServerSessionCount(fixture) == 1, "the server session");

            var serverSession  = ServerSessionAt(fixture, 0);
            var clientPairing  = await ClientPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(session.Initiation,                 Is.SameAs(connect.Initiation));
                Assert.That(session.Pairing,                    Is.EqualTo(clientPairing), "the session belongs to the (updated) pairing of the client");
                Assert.That(session.WebSocketClient.Session,    Is.SameAs(session.Session));
                Assert.That(session.Session.IsConnected,        Is.True);
                Assert.That(session.Session.Role,               Is.EqualTo(EnergyManagementRole.RM));
                Assert.That(session.Session.Mode,               Is.EqualTo(S2SessionMode.S2Connect));
                Assert.That(session.Session.NegotiatedVersion,  Is.EqualTo(Version.S2JSONVersion));
                Assert.That(serverSession.IsConnected,          Is.True);
                Assert.That(serverSession.Role,                 Is.EqualTo(EnergyManagementRole.CEM));
                Assert.That(serverSession.NegotiatedVersion,    Is.EqualTo(Version.S2JSONVersion));
            });

            await session.CloseAsync("test done");

            await WaitUntilAsync(() => !serverSession.IsConnected, "the server session to end");

            Assert.Multiple(() => {
                Assert.That(session.Session.IsConnected,  Is.False);
                Assert.That(serverSession.IsConnected,    Is.False);
            });

        }

        #endregion

        #region Connect_WebSocketServerDown_InitiationSucceeds_ButNoSessionIsOpened()

        [Test]
        [S2C("SessionInitiation.WebSocket")]
        public async Task Connect_WebSocketServerDown_InitiationSucceeds_ButNoSessionIsOpened()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(
                                          ClientOptions: SessionInitiationFixture.DefaultClientOptions() with {
                                                             RequestTimeout = TimeSpan.FromSeconds(5)
                                                         }
                                      );

            var oldToken = (await ClientPairingAsync(fixture))!.AccessToken;

            await fixture.WebSocketServer.Shutdown();

            var connect = await fixture.Client.ConnectAsync(fixture.RM, fixture.CEM.Id);

            var clientPairing  = await ClientPairingAsync(fixture);
            var serverPairing  = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {

                Assert.That(connect.IsSuccess,                            Is.False, connect.Initiation.ToString());
                Assert.That(connect.Session,                              Is.Null);
                Assert.That(connect.Initiation.Outcome,                   Is.AnyOf(SessionInitiationOutcome.RetryLater, SessionInitiationOutcome.TransportFailure), connect.Initiation.ToString());
                Assert.That(connect.Initiation.Retryable,                 Is.True);
                Assert.That(connect.Initiation.Operation,                 Is.EqualTo("webSocket"));
                Assert.That(connect.Initiation.Pairing,                   Is.Not.Null, "the pairing of the completed session initiation is reported");

                // The session initiation itself succeeded: the token was rotated on both sides.
                Assert.That(fixture.ClientResults.Select(r => r.Outcome), Is.EqualTo(new[] { SessionInitiationOutcome.Success }));
                Assert.That(clientPairing,                                Is.Not.Null);
                Assert.That(clientPairing!.AccessToken.Equals(oldToken),  Is.False);
                Assert.That(clientPairing.AccessToken,                    Is.EqualTo(serverPairing!.AccessToken));
                Assert.That(connect.Initiation.Pairing?.AccessToken,      Is.EqualTo(clientPairing.AccessToken));
                Assert.That(fixture.ClientStore.PendingCount,             Is.EqualTo(0));
                Assert.That(fixture.ServerStore.PendingCount,             Is.EqualTo(0));

            });

        }

        #endregion


        // Unpairing by the communication client (S2 Connect 1.0.0, "Unpairing by the communication client")

        #region Unpair_WithAPendingCandidateOnly_Succeeds()

        [Test]
        [S2C("Unpairing.ByCommunicationClient")]
        [S2C("Unpairing.CandidateTokens")]
        public async Task Unpair_WithAPendingCandidateOnly_Succeeds()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            // The active token of the client is wrong, but the token the server accepts is still a pending candidate.
            var rightToken = (await ServerPairingAsync(fixture))!.AccessToken;

            await ReplaceClientTokenAsync(fixture);
            await fixture.ClientStore.AddPendingAccessTokenAsync(new PendingAccessToken(fixture.RM.Id, fixture.CEM.Id, rightToken, DateTimeOffset.UtcNow));

            var result = await fixture.Client.UnpairAsync(fixture.RM.Id, fixture.CEM.Id);

            var clientPairing     = await ClientPairingAsync(fixture);
            var serverPairing     = await ServerPairingAsync(fixture);
            var clientUnpairedAt  = await fixture.ClientStore.GetUnpairedAtAsync(fixture.RM.Id, fixture.CEM.Id);
            var serverUnpairedAt  = await fixture.ServerStore.GetUnpairedAtAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                               Is.EqualTo(SessionInitiationOutcome.Success), result.ToString());
                Assert.That(result.Operation,                             Is.EqualTo("unpair"));
                Assert.That(result.StatusCode?.Code,                      Is.EqualTo(204));
                Assert.That(serverPairing,                                Is.Null, "the server removed the pairing");
                Assert.That(serverUnpairedAt,                             Is.Not.Null, "the server wrote a tombstone");
                Assert.That(clientPairing,                                Is.Null, "the client removed its security material");
                Assert.That(clientUnpairedAt,                             Is.Not.Null);
                Assert.That(fixture.ClientStore.PendingCount,             Is.EqualTo(0), "the pending candidates are gone as well");
                Assert.That(fixture.UnpairedAtServer,                     Has.Count.EqualTo(1));
                Assert.That(fixture.ClientResults.Select(r => r.Outcome), Is.EqualTo(new[] { SessionInitiationOutcome.Success }));
            });

        }

        #endregion

        #region Unpair_WrongTokenEverywhere_ReturnsAlreadyUnpaired_AndRemovesTheLocalPairing()

        [Test]
        [S2C("Unpairing.ByCommunicationClient")]
        [S2C("Unpairing.ByCommunicationClient.401")]
        public async Task Unpair_WrongTokenEverywhere_ReturnsAlreadyUnpaired_AndRemovesTheLocalPairing()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync();

            var serverToken = (await ServerPairingAsync(fixture))!.AccessToken;

            await ReplaceClientTokenAsync(fixture);

            var result = await fixture.Client.UnpairAsync(fixture.RM.Id, fixture.CEM.Id);

            var clientPairing     = await ClientPairingAsync(fixture);
            var serverPairing     = await ServerPairingAsync(fixture);
            var clientUnpairedAt  = await fixture.ClientStore.GetUnpairedAtAsync(fixture.RM.Id, fixture.CEM.Id);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,               Is.EqualTo(SessionInitiationOutcome.AlreadyUnpaired), result.ToString());
                Assert.That(result.IsSuccess,             Is.False);
                Assert.That(result.Operation,             Is.EqualTo("unpair"));
                Assert.That(result.StatusCode?.Code,      Is.EqualTo(401));
                Assert.That(result.Retryable,             Is.False);
                Assert.That(clientPairing,                Is.Null, "401 means: the nodes are already unpaired at the server; the local security material is removed");
                Assert.That(clientUnpairedAt,             Is.Not.Null);
                Assert.That(serverPairing?.AccessToken,   Is.EqualTo(serverToken), "the server never accepted the request and keeps its pairing");
                Assert.That(fixture.UnpairedAtServer,     Is.Empty);
            });

        }

        #endregion

        #region Unpair_NotPairedLocally_ReturnsInvalidConfiguration()

        [Test]
        [S2C("Unpairing.ByCommunicationClient")]
        public async Task Unpair_NotPairedLocally_ReturnsInvalidConfiguration()
        {

            await using var fixture = await SessionInitiationFixture.CreateAsync(Paired: false);

            var result = await fixture.Client.UnpairAsync(fixture.RM.Id, fixture.CEM.Id);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,                               Is.EqualTo(SessionInitiationOutcome.InvalidConfiguration), result.ToString());
                Assert.That(result.IsSuccess,                             Is.False);
                Assert.That(result.Operation,                             Is.EqualTo("unpair"));
                Assert.That(result.StatusCode,                            Is.Null, "no request was sent");
                Assert.That(result.Retryable,                             Is.False);
                Assert.That(result.Description,                           Does.Contain("not paired"));
                Assert.That(fixture.UnpairedAtServer,                     Is.Empty);
                Assert.That(fixture.ClientResults.Select(r => r.Outcome), Is.EqualTo(new[] { SessionInitiationOutcome.InvalidConfiguration }));
            });

        }

        #endregion

        #region Unpair_ServerUnreachable_ReturnsTransportFailure_AndKeepsTheLocalPairing()

        [Test]
        [S2C("Unpairing.ByCommunicationClient")]
        public async Task Unpair_ServerUnreachable_ReturnsTransportFailure_AndKeepsTheLocalPairing()
        {

            await using var fixture  = await SessionInitiationFixture.CreateAsync();
            await using var client   = UnreachableClient(fixture);

            var token = (await ClientPairingAsync(fixture))!.AccessToken;

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var result = await client.UnpairAsync(fixture.RM.Id, fixture.CEM.Id, timeout.Token);

            var clientPairing  = await ClientPairingAsync(fixture);
            var serverPairing  = await ServerPairingAsync(fixture);

            Assert.Multiple(() => {
                Assert.That(result.Outcome,               Is.EqualTo(SessionInitiationOutcome.TransportFailure), result.ToString());
                Assert.That(result.IsSuccess,             Is.False);
                Assert.That(result.Operation,             Is.EqualTo("versionIndex"));
                Assert.That(result.Retryable,             Is.True);
                Assert.That(clientPairing?.AccessToken,   Is.EqualTo(token), "the local pairing is kept: the server never confirmed the unpairing");
                Assert.That(serverPairing?.AccessToken,   Is.EqualTo(token));
                Assert.That(fixture.UnpairedAtServer,     Is.Empty);
            });

        }

        #endregion


        // Options

        #region Options_DefaultsEqualTheNormativeValues_AndValidate()

        [Test]
        [S2C("Communication.WebSocket.Keepalive")]
        public void Options_DefaultsEqualTheNormativeValues_AndValidate()
        {

            var options = new SessionInitiationClientOptions();

            Assert.Multiple(() => {
                Assert.That(options.SupportedAPIVersions,                     Is.EqualTo(Version.S2ConnectAPIVersions));
                Assert.That(options.SupportedAPIVersions,                     Is.EqualTo(new[] { "v1" }));
                Assert.That(options.RequestTimeout,                           Is.EqualTo(TimeSpan.FromSeconds(10)));
                Assert.That(options.ServiceUnavailableRetryDelay,             Is.EqualTo(TimeSpan.FromSeconds(1)));
                Assert.That(options.MaxServiceUnavailableRetries,             Is.EqualTo(3));
                Assert.That(options.SendDescriptions,                         Is.False);
                Assert.That(options.RemovePairingWhenNoLongerPaired,          Is.True);
                Assert.That(options.AcceptSelfSignedCertificates,             Is.True);
                Assert.That(options.ParserOptions,                            Is.SameAs(S2ParserOptions.Default));
                Assert.That(options.WebSocketPingInterval,                    Is.EqualTo(S2ConnectDefaults.WebSocketPingInterval));
                Assert.That(options.WebSocketPingInterval,                    Is.EqualTo(TimeSpan.FromSeconds(30)));
                Assert.That(options.Validate,                                 Throws.Nothing);
                Assert.That(SessionInitiationClientOptions.Default.Validate,  Throws.Nothing);
                Assert.That(SessionInitiationFixture.DefaultClientOptions().Validate, Throws.Nothing);
            });

        }

        #endregion

        #region Options_Validate_RejectsOutOfRangeValues()

        [Test]
        [S2C("Communication.WebSocket.Keepalive")]
        public void Options_Validate_RejectsOutOfRangeValues()
        {

            var valid = SessionInitiationClientOptions.Default;

            Assert.Multiple(() => {

                Assert.That((valid with { RequestTimeout = TimeSpan.Zero }).Validate,                                        Throws.TypeOf<ArgumentOutOfRangeException>(), "zero request timeout");
                Assert.That((valid with { RequestTimeout = TimeSpan.FromSeconds(-1) }).Validate,                             Throws.TypeOf<ArgumentOutOfRangeException>(), "negative request timeout");
                Assert.That((valid with { RequestTimeout = TimeSpan.FromMilliseconds(1) }).Validate,                         Throws.Nothing);

                Assert.That((valid with { ServiceUnavailableRetryDelay = TimeSpan.FromMilliseconds(-1) }).Validate,          Throws.TypeOf<ArgumentOutOfRangeException>(), "negative retry delay");
                Assert.That((valid with { ServiceUnavailableRetryDelay = TimeSpan.Zero }).Validate,                          Throws.Nothing, "zero retry delay for tests");

                Assert.That((valid with { MaxServiceUnavailableRetries = -1 }).Validate,                                     Throws.TypeOf<ArgumentOutOfRangeException>(), "negative retries");
                Assert.That((valid with { MaxServiceUnavailableRetries =  0 }).Validate,                                     Throws.Nothing, "no retries");

                Assert.That((valid with { WebSocketPingInterval = TimeSpan.FromSeconds(61) }).Validate,                      Throws.TypeOf<ArgumentOutOfRangeException>(), "pings must not be more than 60 seconds apart");
                Assert.That((valid with { WebSocketPingInterval = TimeSpan.FromSeconds(60) + TimeSpan.FromTicks(1) }).Validate, Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That((valid with { WebSocketPingInterval = TimeSpan.Zero }).Validate,                                 Throws.TypeOf<ArgumentOutOfRangeException>(), "zero ping interval");
                Assert.That((valid with { WebSocketPingInterval = TimeSpan.FromSeconds(-1) }).Validate,                      Throws.TypeOf<ArgumentOutOfRangeException>(), "negative ping interval");
                Assert.That((valid with { WebSocketPingInterval = S2ConnectDefaults.MaxWebSocketPingInterval }).Validate,    Throws.Nothing, "the normative maximum");
                Assert.That((valid with { WebSocketPingInterval = TimeSpan.FromSeconds(1) }).Validate,                       Throws.Nothing);

                Assert.That((valid with { SupportedAPIVersions = [] }).Validate,                                             Throws.ArgumentException, "no API version");
                Assert.That((valid with { SupportedAPIVersions = null! }).Validate,                                          Throws.ArgumentException, "no API version list");

                Assert.That((valid with { ParserOptions = null! }).Validate,                                                 Throws.ArgumentNullException);

            });

        }

        #endregion

        #region Constructor_ValidatesTheOptions()

        [Test]
        public void Constructor_ValidatesTheOptions()
        {

            var endpoint  = new LocalEndpoint(new EndpointDescription("Test RM endpoint"),
                                              Deployment.LAN,
                                              S2BaseURL.Parse("http://127.0.0.1:1/pairing/", AllowHTTP: true));

            var store     = new InMemoryS2Store();

            var invalid   = SessionInitiationFixture.DefaultClientOptions() with {
                                RequestTimeout = TimeSpan.Zero
                            };

            Assert.That(() => new SessionInitiationClient(UnreachableSessionInitiationUrl, endpoint, store, invalid),
                        Throws.TypeOf<ArgumentOutOfRangeException>());

        }

        #endregion

    }

}

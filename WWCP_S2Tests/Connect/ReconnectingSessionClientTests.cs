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
    /// The reconnecting session client of a communication client against the Phase 8 session
    /// initiation server and WebSocket server (S2 Connect 1.0.0, "Reconnection strategy"): every
    /// (re)connection runs through session initiation, a lost session is re-established with
    /// back-off, SessionRequest RECONNECT reconnects immediately, SessionRequest TERMINATE
    /// reconnects with back-off, and NoLongerPaired, Unauthorized and a missing pairing stop the
    /// client. A fast back-off (50 ms base, 300 ms maximum) keeps the tests short.
    /// </summary>
    [TestFixture]
    public sealed class ReconnectingSessionClientTests
    {

        #region Observations

        /// <summary>
        /// Thread-safe records of the events of a reconnecting session client.
        /// </summary>
        private sealed class Observations
        {

            private readonly Lock                                                                     lockObject  = new ();
            private readonly List<S2ConnectSession>                                                   started     = [];
            private readonly List<(S2ConnectSession Session, S2CloseReason Reason)>                   ended       = [];
            private readonly List<(SessionInitiationClientResult Result, Int32 Attempt, TimeSpan Delay)>  failed  = [];
            private readonly List<ReconnectStopReason>                                                stopped     = [];

            public Observations(ReconnectingSessionClient Client)
            {

                Client.OnSessionStarted  += (timestamp, sender, session) => {
                                                lock (lockObject)
                                                {
                                                    started.Add(session);
                                                }
                                                return Task.CompletedTask;
                                            };

                Client.OnSessionEnded    += (timestamp, sender, session, reason) => {
                                                lock (lockObject)
                                                {
                                                    ended.Add((session, reason));
                                                }
                                                return Task.CompletedTask;
                                            };

                Client.OnAttemptFailed   += (timestamp, sender, result, attempt, delay) => {
                                                lock (lockObject)
                                                {
                                                    failed.Add((result, attempt, delay));
                                                }
                                                return Task.CompletedTask;
                                            };

                Client.OnStopped         += (timestamp, sender, reason) => {
                                                lock (lockObject)
                                                {
                                                    stopped.Add(reason);
                                                }
                                                return Task.CompletedTask;
                                            };

            }

            public IReadOnlyList<S2ConnectSession> Started
            {
                get
                {
                    lock (lockObject)
                    {
                        return [.. started];
                    }
                }
            }

            public IReadOnlyList<(S2ConnectSession Session, S2CloseReason Reason)> Ended
            {
                get
                {
                    lock (lockObject)
                    {
                        return [.. ended];
                    }
                }
            }

            public IReadOnlyList<(SessionInitiationClientResult Result, Int32 Attempt, TimeSpan Delay)> Failed
            {
                get
                {
                    lock (lockObject)
                    {
                        return [.. failed];
                    }
                }
            }

            public IReadOnlyList<ReconnectStopReason> Stopped
            {
                get
                {
                    lock (lockObject)
                    {
                        return [.. stopped];
                    }
                }
            }

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
        /// A back-off of 50 ms base delay and 300 ms maximal delay, so that reconnections happen quickly.
        /// </summary>
        private static ReconnectStrategy FastStrategy()
            => new (TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(300));

        /// <summary>
        /// A reconnecting session client of the RM of the fixture towards its CEM with the fast back-off.
        /// </summary>
        private static ReconnectingSessionClient CreateClient(SessionInitiationFixture Fixture)
            => new (Fixture.Client, Fixture.RM, Fixture.CEM.Id, FastStrategy());

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

        /// <summary>
        /// Replace the active access token of the client with one the server does not know.
        /// </summary>
        private static async Task ReplaceClientTokenAsync(SessionInitiationFixture Fixture)
        {

            var pairing = await Fixture.ClientStore.GetPairingAsync(Fixture.RM.Id, Fixture.CEM.Id);

            Assert.That(pairing, Is.Not.Null, "the fixture is paired");

            await Fixture.ClientStore.AddOrReplacePairingAsync(pairing!.WithAccessToken(TokenGenerator.NewAccessToken()));

        }

        /// <summary>
        /// Start the client and wait until its first session is open on both sides.
        /// </summary>
        private static async Task StartAndWaitForTheFirstSessionAsync(SessionInitiationFixture   Fixture,
                                                                      ReconnectingSessionClient  Client,
                                                                      Observations               Observed)
        {

            Client.Start();

            await WaitUntilAsync(() => Observed.Started.Count == 1,    "the first session");
            await WaitUntilAsync(() => ServerSessionCount(Fixture) == 1, "the first server session");

        }

        #endregion


        #region Start_OpensASession_AndStop_ClosesIt()

        [Test]
        [S2C("Reconnection.SessionInitiation")]
        [S2C("Reconnection.Stop")]
        public async Task Start_OpensASession_AndStop_ClosesIt()
        {

            await using var fixture  = await SessionInitiationFixture.CreateAsync();
            await using var client   = CreateClient(fixture);

            var observed = new Observations(client);

            Assert.Multiple(() => {
                Assert.That(client.IsRunning,       Is.False);
                Assert.That(client.StopReason,      Is.Null);
                Assert.That(client.CurrentSession,  Is.Null);
                Assert.That(client.SessionCount,    Is.EqualTo(0));
                Assert.That(client.LastResult,      Is.Null);
            });

            await StartAndWaitForTheFirstSessionAsync(fixture, client, observed);

            var session        = observed.Started[0];
            var serverSession  = ServerSessionAt(fixture, 0);

            Assert.Multiple(() => {
                Assert.That(client.IsRunning,               Is.True);
                Assert.That(client.CurrentSession,          Is.SameAs(session));
                Assert.That(client.SessionCount,            Is.EqualTo(1));
                Assert.That(client.StopReason,              Is.Null);
                Assert.That(client.LastResult?.Outcome,     Is.EqualTo(SessionInitiationOutcome.Success));
                Assert.That(client.Strategy.Attempt,        Is.EqualTo(0), "the back-off is reset after a successful connection");
                Assert.That(session.Session.IsConnected,    Is.True);
                Assert.That(session.Session.Role,           Is.EqualTo(EnergyManagementRole.RM));
                Assert.That(serverSession.IsConnected,      Is.True);
                Assert.That(observed.Ended,                 Is.Empty);
                Assert.That(observed.Failed,                Is.Empty);
                Assert.That(observed.Stopped,               Is.Empty);
            });

            await client.StopAsync();

            await WaitUntilAsync(() => !serverSession.IsConnected, "the server session to end");

            var ended = observed.Ended;

            Assert.Multiple(() => {
                Assert.That(client.IsRunning,               Is.False);
                Assert.That(client.StopReason,              Is.EqualTo(ReconnectStopReason.Stopped));
                Assert.That(client.CurrentSession,          Is.Null);
                Assert.That(client.SessionCount,            Is.EqualTo(1));
                Assert.That(session.Session.IsConnected,    Is.False, "the session was closed");
                Assert.That(ended,                          Has.Count.EqualTo(1));
                Assert.That(ended[0].Session,               Is.SameAs(session));
                Assert.That(ended[0].Reason.IsLocal,        Is.True, "the client closed the session itself");
                Assert.That(observed.Stopped,               Is.EqualTo(new[] { ReconnectStopReason.Stopped }));
                Assert.That(observed.Failed,                Is.Empty);
            });

        }

        #endregion

        #region ServerClosesTheSession_ClientReconnects_WithBackOff()

        [Test]
        [S2C("Reconnection.ConnectionLost")]
        [S2C("Reconnection.BackOff")]
        public async Task ServerClosesTheSession_ClientReconnects_WithBackOff()
        {

            await using var fixture  = await SessionInitiationFixture.CreateAsync();
            await using var client   = CreateClient(fixture);

            var observed = new Observations(client);

            await StartAndWaitForTheFirstSessionAsync(fixture, client, observed);

            var first = observed.Started[0];

            await ServerSessionAt(fixture, 0).CloseAsync(new S2CloseReason("server closed", true));

            await WaitUntilAsync(() => observed.Started.Count == 2,    "the second session");
            await WaitUntilAsync(() => ServerSessionCount(fixture) == 2, "the second server session");

            var second         = observed.Started[1];
            var ended          = observed.Ended;
            var clientPairing  = await fixture.ClientStore.GetPairingAsync(fixture.RM.Id,  fixture.CEM.Id);
            var serverPairing  = await fixture.ServerStore.GetPairingAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.Multiple(() => {
                Assert.That(client.IsRunning,               Is.True);
                Assert.That(client.SessionCount,            Is.EqualTo(2));
                Assert.That(client.CurrentSession,          Is.SameAs(second));
                Assert.That(second,                         Is.Not.SameAs(first));
                Assert.That(first.Session.IsConnected,      Is.False);
                Assert.That(second.Session.IsConnected,     Is.True);
                Assert.That(client.LastResult?.Outcome,     Is.EqualTo(SessionInitiationOutcome.Success), "the reconnection ran through session initiation");
                Assert.That(client.Strategy.Attempt,        Is.EqualTo(0));
                Assert.That(ended,                          Has.Count.EqualTo(1));
                Assert.That(ended[0].Session,               Is.SameAs(first));
                Assert.That(ended[0].Reason.IsLocal,        Is.False, "the peer closed the session");
                Assert.That(observed.Failed,                Is.Empty);
                Assert.That(observed.Stopped,               Is.Empty);
                Assert.That(clientPairing?.AccessToken,     Is.EqualTo(serverPairing?.AccessToken), "the token was rotated again on both sides");
                Assert.That(second.Pairing.AccessToken,     Is.EqualTo(clientPairing?.AccessToken));
            });

            await client.StopAsync();

            Assert.That(client.StopReason, Is.EqualTo(ReconnectStopReason.Stopped));

        }

        #endregion

        #region SessionRequestReconnect_ClientClosesAndReconnectsImmediately()

        [Test]
        [S2C("Reconnection.SessionRequest.RECONNECT")]
        public async Task SessionRequestReconnect_ClientClosesAndReconnectsImmediately()
        {

            await using var fixture  = await SessionInitiationFixture.CreateAsync();
            await using var client   = CreateClient(fixture);

            var observed = new Observations(client);

            await StartAndWaitForTheFirstSessionAsync(fixture, client, observed);

            var first  = observed.Started[0];

            var sent   = await ServerSessionAt(fixture, 0).SendAsync(new SessionRequest(SessionRequestType.Reconnect, "please reconnect"));

            Assert.That(sent.IsSuccess, Is.True, sent.ToString());

            await WaitUntilAsync(() => observed.Started.Count == 2,    "the second session");
            await WaitUntilAsync(() => ServerSessionCount(fixture) == 2, "the second server session");

            var second  = observed.Started[1];
            var ended   = observed.Ended;

            Assert.Multiple(() => {
                Assert.That(client.IsRunning,               Is.True);
                Assert.That(client.SessionCount,            Is.EqualTo(2));
                Assert.That(client.CurrentSession,          Is.SameAs(second));
                Assert.That(first.Session.IsConnected,      Is.False);
                Assert.That(second.Session.IsConnected,     Is.True);
                Assert.That(ended,                          Has.Count.EqualTo(1));
                Assert.That(ended[0].Session,               Is.SameAs(first));
                Assert.That(ended[0].Reason.IsLocal,        Is.True, "the client closed the session as requested");
                Assert.That(ended[0].Reason.Description,    Does.Contain("RECONNECT"));
                Assert.That(observed.Failed,                Is.Empty);
                Assert.That(observed.Stopped,               Is.Empty);
            });

            await client.StopAsync();

        }

        #endregion

        #region SessionRequestTerminate_ClientClosesAndReconnectsWithBackOff()

        [Test]
        [S2C("Reconnection.SessionRequest.TERMINATE")]
        public async Task SessionRequestTerminate_ClientClosesAndReconnectsWithBackOff()
        {

            await using var fixture  = await SessionInitiationFixture.CreateAsync();
            await using var client   = CreateClient(fixture);

            var observed = new Observations(client);

            await StartAndWaitForTheFirstSessionAsync(fixture, client, observed);

            var first  = observed.Started[0];

            var sent   = await ServerSessionAt(fixture, 0).SendAsync(new SessionRequest(SessionRequestType.Terminate, "maintenance"));

            Assert.That(sent.IsSuccess, Is.True, sent.ToString());

            await WaitUntilAsync(() => observed.Started.Count == 2,    "the second session");
            await WaitUntilAsync(() => ServerSessionCount(fixture) == 2, "the second server session");

            var second  = observed.Started[1];
            var ended   = observed.Ended;

            Assert.Multiple(() => {
                Assert.That(client.IsRunning,               Is.True);
                Assert.That(client.SessionCount,            Is.EqualTo(2));
                Assert.That(client.CurrentSession,          Is.SameAs(second));
                Assert.That(first.Session.IsConnected,      Is.False);
                Assert.That(second.Session.IsConnected,     Is.True);
                Assert.That(ended,                          Has.Count.EqualTo(1));
                Assert.That(ended[0].Session,               Is.SameAs(first));
                Assert.That(ended[0].Reason.IsLocal,        Is.True, "the client closed the session as requested");
                Assert.That(ended[0].Reason.Description,    Does.Contain("TERMINATE"));
                Assert.That(observed.Failed,                Is.Empty);
                Assert.That(observed.Stopped,               Is.Empty);
            });

            await client.StopAsync();

        }

        #endregion

        #region ServerUnpairsDuringASession_NextInitiationAnswersNoLongerPaired_AndTheClientStops()

        [Test]
        [S2C("Reconnection.NoLongerPaired")]
        [S2C("Unpairing.ByCommunicationServer")]
        public async Task ServerUnpairsDuringASession_NextInitiationAnswersNoLongerPaired_AndTheClientStops()
        {

            await using var fixture  = await SessionInitiationFixture.CreateAsync();
            await using var client   = CreateClient(fixture);

            var observed = new Observations(client);

            await StartAndWaitForTheFirstSessionAsync(fixture, client, observed);

            var removed = await fixture.API.UnpairLocallyAsync(fixture.CEM.Id, fixture.RM.Id);

            Assert.That(removed, Is.Not.Null);

            await ServerSessionAt(fixture, 0).CloseAsync(new S2CloseReason("unpaired by the communication server", true));

            await WaitUntilAsync(() => !client.IsRunning, "the client to stop");

            var clientPairing     = await fixture.ClientStore.GetPairingAsync(fixture.RM.Id, fixture.CEM.Id);
            var clientUnpairedAt  = await fixture.ClientStore.GetUnpairedAtAsync(fixture.RM.Id, fixture.CEM.Id);

            Assert.Multiple(() => {
                Assert.That(client.StopReason,              Is.EqualTo(ReconnectStopReason.NoLongerPaired));
                Assert.That(client.CurrentSession,          Is.Null);
                Assert.That(client.SessionCount,            Is.EqualTo(1), "no second session was opened");
                Assert.That(client.LastResult?.Outcome,     Is.EqualTo(SessionInitiationOutcome.NoLongerPaired));
                Assert.That(observed.Started,               Has.Count.EqualTo(1));
                Assert.That(observed.Ended,                 Has.Count.EqualTo(1));
                Assert.That(observed.Failed,                Is.Empty, "NoLongerPaired stops the client instead of scheduling a new attempt");
                Assert.That(observed.Stopped,               Is.EqualTo(new[] { ReconnectStopReason.NoLongerPaired }));
                Assert.That(clientPairing,                  Is.Null, "the client removed its security material");
                Assert.That(clientUnpairedAt,               Is.Not.Null);
                Assert.That(fixture.UnpairedAtServer,       Has.Count.EqualTo(1));
            });

        }

        #endregion

        #region WrongTokenFromTheStart_StopsWithUnauthorized()

        [Test]
        [S2C("Reconnection.Unauthorized")]
        public async Task WrongTokenFromTheStart_StopsWithUnauthorized()
        {

            await using var fixture  = await SessionInitiationFixture.CreateAsync();
            await using var client   = CreateClient(fixture);

            var observed = new Observations(client);

            await ReplaceClientTokenAsync(fixture);

            client.Start();

            await WaitUntilAsync(() => !client.IsRunning, "the client to stop");

            Assert.Multiple(() => {
                Assert.That(client.StopReason,              Is.EqualTo(ReconnectStopReason.Unauthorized));
                Assert.That(client.CurrentSession,          Is.Null);
                Assert.That(client.SessionCount,            Is.EqualTo(0));
                Assert.That(client.LastResult?.Outcome,     Is.EqualTo(SessionInitiationOutcome.Unauthorized));
                Assert.That(observed.Started,               Is.Empty);
                Assert.That(observed.Ended,                 Is.Empty);
                Assert.That(observed.Failed,                Is.Empty, "do not retry, inform the end user");
                Assert.That(observed.Stopped,               Is.EqualTo(new[] { ReconnectStopReason.Unauthorized }));
                Assert.That(ServerSessionCount(fixture),    Is.EqualTo(0));
            });

        }

        #endregion

        #region NotPaired_StopsWithNotPaired()

        [Test]
        [S2C("Reconnection.NotPaired")]
        public async Task NotPaired_StopsWithNotPaired()
        {

            await using var fixture  = await SessionInitiationFixture.CreateAsync(Paired: false);
            await using var client   = CreateClient(fixture);

            var observed = new Observations(client);

            client.Start();

            await WaitUntilAsync(() => !client.IsRunning, "the client to stop");

            Assert.Multiple(() => {
                Assert.That(client.StopReason,              Is.EqualTo(ReconnectStopReason.NotPaired));
                Assert.That(client.CurrentSession,          Is.Null);
                Assert.That(client.SessionCount,            Is.EqualTo(0));
                Assert.That(client.LastResult?.Outcome,     Is.EqualTo(SessionInitiationOutcome.InvalidConfiguration));
                Assert.That(observed.Started,               Is.Empty);
                Assert.That(observed.Failed,                Is.Empty);
                Assert.That(observed.Stopped,               Is.EqualTo(new[] { ReconnectStopReason.NotPaired }));
                Assert.That(ServerSessionCount(fixture),    Is.EqualTo(0));
            });

        }

        #endregion

        #region ServerShutDown_AttemptsFailWithGrowingBackOff_UntilStopped()

        [Test]
        [S2C("Reconnection.BackOff")]
        [S2C("Reconnection.Stop")]
        public async Task ServerShutDown_AttemptsFailWithGrowingBackOff_UntilStopped()
        {

            // Without 503 retries every attempt fails at once (a retry would wait for the
            // "Retry-After: 1" of the shut down server), so the back-off itself is observed.
            await using var fixture  = await SessionInitiationFixture.CreateAsync(
                                           ClientOptions: SessionInitiationFixture.DefaultClientOptions() with {
                                                              MaxServiceUnavailableRetries = 0
                                                          }
                                       );

            await using var client   = CreateClient(fixture);

            var observed = new Observations(client);

            fixture.API.Shutdown();

            client.Start();

            await WaitUntilAsync(() => observed.Failed.Count >= 3, "three failed attempts");

            var failed = observed.Failed;

            Assert.Multiple(() => {

                Assert.That(client.IsRunning,                             Is.True, "the client keeps trying");
                Assert.That(client.StopReason,                            Is.Null);
                Assert.That(client.CurrentSession,                        Is.Null);
                Assert.That(client.SessionCount,                          Is.EqualTo(0));
                Assert.That(client.LastResult?.Outcome,                   Is.EqualTo(SessionInitiationOutcome.RetryLater));
                Assert.That(failed.Take(3).Select(f => f.Attempt),        Is.EqualTo(new[] { 0, 1, 2 }), "the attempt counter grows with every failure");

                for (var i = 0; i < 3; i++)
                {
                    Assert.That(failed[i].Result.Outcome,                 Is.EqualTo(SessionInitiationOutcome.RetryLater), failed[i].Result.ToString());
                    Assert.That(failed[i].Result.StatusCode?.Code,        Is.EqualTo(503));
                    Assert.That(failed[i].Delay,                          Is.InRange(TimeSpan.Zero, client.Strategy.UpperBound(i)), $"the delay of attempt {i} is within random(0, min(max_delay, base_delay × 2^{i}))");
                }

                Assert.That(observed.Started,                             Is.Empty);
                Assert.That(observed.Ended,                               Is.Empty);
                Assert.That(observed.Stopped,                             Is.Empty);

            });

            await client.StopAsync();

            Assert.Multiple(() => {
                Assert.That(client.IsRunning,       Is.False);
                Assert.That(client.StopReason,      Is.EqualTo(ReconnectStopReason.Stopped));
                Assert.That(client.CurrentSession,  Is.Null);
                Assert.That(observed.Stopped,       Is.EqualTo(new[] { ReconnectStopReason.Stopped }));
            });

        }

        #endregion

        #region Start_Twice_Throws_AndDisposeAsync_StopsTheClient()

        [Test]
        [S2C("Reconnection.Stop")]
        public async Task Start_Twice_Throws_AndDisposeAsync_StopsTheClient()
        {

            await using var fixture  = await SessionInitiationFixture.CreateAsync();
            await using var client   = CreateClient(fixture);

            var observed = new Observations(client);

            client.Start();

            Assert.That(client.Start, Throws.TypeOf<InvalidOperationException>(), "a running client cannot be started again");

            await WaitUntilAsync(() => observed.Started.Count == 1, "the first session");

            var session = observed.Started[0];

            await client.DisposeAsync();

            Assert.Multiple(() => {
                Assert.That(client.IsRunning,               Is.False);
                Assert.That(client.StopReason,              Is.EqualTo(ReconnectStopReason.Disposed));
                Assert.That(client.CurrentSession,          Is.Null);
                Assert.That(session.Session.IsConnected,    Is.False, "the session was closed");
                Assert.That(observed.Ended,                 Has.Count.EqualTo(1));
                Assert.That(observed.Stopped,               Is.EqualTo(new[] { ReconnectStopReason.Disposed }));
            });

            Assert.That(client.Start, Throws.TypeOf<ObjectDisposedException>());

            // A second disposal is harmless.
            await client.DisposeAsync();

        }

        #endregion

        #region Constructor_UsesTheStrategyOfTheSpecificationByDefault_AndGuardsItsArguments()

        [Test]
        [S2C("Reconnection.BackOff")]
        public async Task Constructor_UsesTheStrategyOfTheSpecificationByDefault_AndGuardsItsArguments()
        {

            // No server is needed: the client is never started.
            var endpoint       = new LocalEndpoint(new EndpointDescription("Test RM endpoint"),
                                                   Deployment.LAN,
                                                   S2BaseURL.Parse("http://127.0.0.1:1/pairing/", AllowHTTP: true));

            var rm             = endpoint.AddNode(new NodeDescription(Node_Id.NewRandom, "ACME", "heat pump", "TestRM 1", EnergyManagementRole.RM));
            var serverNodeId   = Node_Id.NewRandom;

            await using var initiation = new SessionInitiationClient(S2BaseURL.Parse("http://127.0.0.1:1/connection/", AllowHTTP: true),
                                                                     endpoint,
                                                                     new InMemoryS2Store(),
                                                                     SessionInitiationFixture.DefaultClientOptions());

            await using var client = new ReconnectingSessionClient(initiation, rm, serverNodeId);

            Assert.Multiple(() => {
                Assert.That(client.Client,                  Is.SameAs(initiation));
                Assert.That(client.LocalNode,               Is.SameAs(rm));
                Assert.That(client.ServerNodeId,            Is.EqualTo(serverNodeId));
                Assert.That(client.Strategy.BaseDelay,      Is.EqualTo(S2ConnectDefaults.ReconnectBaseDelay));
                Assert.That(client.Strategy.MaxDelay,       Is.EqualTo(S2ConnectDefaults.ReconnectMaxDelay));
                Assert.That(client.Strategy.Attempt,        Is.EqualTo(0));
                Assert.That(client.IsRunning,               Is.False);
                Assert.That(client.StopReason,              Is.Null);
                Assert.That(client.CurrentSession,          Is.Null);
                Assert.That(client.SessionCount,            Is.EqualTo(0));
                Assert.That(client.LastResult,              Is.Null);
                Assert.That(() => new ReconnectingSessionClient(null!,      rm,    serverNodeId), Throws.ArgumentNullException);
                Assert.That(() => new ReconnectingSessionClient(initiation, null!, serverNodeId), Throws.ArgumentNullException);
            });

            // Stopping a client that was never started is a no-op...
            await client.StopAsync();

            Assert.Multiple(() => {
                Assert.That(client.IsRunning,   Is.False);
                Assert.That(client.StopReason,  Is.Null);
            });

            // ...and a disposed client cannot be started.
            await client.DisposeAsync();

            Assert.Multiple(() => {
                Assert.That(client.IsRunning,   Is.False);
                Assert.That(client.StopReason,  Is.Null, "a client that never ran has no stop reason");
                Assert.That(client.Start,       Throws.TypeOf<ObjectDisposedException>());
            });

        }

        #endregion

    }

}

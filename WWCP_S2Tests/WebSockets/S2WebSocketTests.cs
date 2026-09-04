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

using System.Net;
using System.Net.Sockets;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.S2.Session;
using cloud.charging.open.protocols.S2.WebSockets;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.WebSockets
{

    /// <summary>
    /// CEM WebSocket server and RM WebSocket client in-process (PLAN.md Phase 4 tests):
    /// bearer-token authentication, deflate, ordered delivery, session requests and closing.
    /// </summary>
    [TestFixture]
    public sealed class S2WebSocketTests
    {

        #region Helpers

        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

        private static IPPort FreePort()
        {
            var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
            listener.Stop();
            return IPPort.Parse((UInt16) port);
        }

        private static async Task<T> WaitFor<T>(TaskCompletionSource<T> Source)
            => await Source.Task.WaitAsync(Wait);

        private static S2SessionOptions RMOptions()
            => new () { Role = EnergyManagementRole.RM, Mode = S2SessionMode.S2Connect, NegotiatedVersion = Version.S2JSONVersion };

        private static ResourceManagerDetails EVChargerDetails()
            => new (Resource_Id.Parse("acme_ev_xxxxxx"),
                    [ new Role(RoleType.EnergyConsumer, Commodity.Electricity) ],
                    Duration.FromMilliseconds(3000),
                    [ ControlType.FillRateBasedControl ],
                    false,
                    [ CommodityQuantity.ElectricPower3PhaseSymmetric ],
                    Name: "My Electric Vehicle RM");

        private sealed class Fixture : IAsyncDisposable
        {

            public IPPort                               Port            { get; }
            public S2WebSocketServer                    Server          { get; }
            public TaskCompletionSource<(S2Session Session, Object? Identity)>  Started  { get; } = new ();
            public TaskCompletionSource<S2CloseReason>  Ended           { get; } = new ();

            public Fixture()
            {

                Port    = FreePort();
                Server  = new S2WebSocketServer(IPv4Address.Parse("127.0.0.1"), Port, EnergyManagementRole.CEM);

                Server.OnSessionStarted += (ts, server, session, identity) => { Started.TrySetResult((session, identity)); return Task.CompletedTask; };
                Server.OnSessionEnded   += (ts, server, session, reason)   => { Ended.  TrySetResult(reason);              return Task.CompletedTask; };

            }

            public async Task StartAsync()
                => await Server.Start();

            public S2WebSocketClient CreateClient(String Token)
                => new (URL.Parse($"ws://127.0.0.1:{Port}/"), Token, RMOptions());

            public async ValueTask DisposeAsync()
                => await Server.Shutdown();

        }

        #endregion


        [Test]
        public async Task Client_ConnectsWithASingleUseToken_AndSessionsRunEndToEnd()
        {

            await using var fixture = new Fixture();
            await fixture.StartAsync();

            var token   = fixture.Server.TokenStore.Issue("rm-node-1");
            var client  = fixture.CreateClient(token);

            var detailsReceived = new TaskCompletionSource<ResourceManagerDetails>();

            var rmSession = await client.ConnectSessionAsync();
            var (cemSession, identity) = await WaitFor(fixture.Started);

            Assert.That(identity,              Is.EqualTo("rm-node-1"));
            Assert.That(rmSession.State,       Is.EqualTo(S2SessionState.WebSocketConnected));
            Assert.That(cemSession.State,      Is.EqualTo(S2SessionState.WebSocketConnected));
            Assert.That(cemSession.Role,       Is.EqualTo(EnergyManagementRole.CEM));
            Assert.That(fixture.Server.Sessions.Count(), Is.EqualTo(1));

            cemSession.On<ResourceManagerDetails>((s, m, ct) => { detailsReceived.TrySetResult(m); return Task.FromResult<ReceptionStatusValue?>(null); });

            var outcome = await rmSession.SendAndAwaitReceptionStatusAsync(EVChargerDetails());
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);
            Assert.That((await WaitFor(detailsReceived)).ResourceId.ToString(), Is.EqualTo("acme_ev_xxxxxx"));

            outcome = await cemSession.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType.FillRateBasedControl));
            Assert.That(outcome.IsOK,                    Is.True, outcome.ReceptionStatus?.DiagnosticLabel);
            Assert.That(rmSession.ActiveControlType,     Is.EqualTo(ControlType.FillRateBasedControl));

            // permessage-deflate was negotiated on both ends.
            Assert.That(((S2WebSocketClientMedium) rmSession.Medium).Connection.PerMessageDeflate, Is.Not.Null);

            await client.CloseSessionAsync("test done");

            var reason = await WaitFor(fixture.Ended);
            Assert.That(reason.IsLocal,                  Is.False);
            Assert.That(cemSession.State,                Is.EqualTo(S2SessionState.Disconnected));
            Assert.That(fixture.Server.Sessions.Count(), Is.EqualTo(0));

        }

        [Test]
        [S2C("Communication.WebSocket.Authentication")]
        public async Task Client_WithoutValidToken_IsRejectedWith401()
        {

            await using var fixture = new Fixture();
            await fixture.StartAsync();

            var client = fixture.CreateClient("not-a-valid-token");

            var exception = Assert.ThrowsAsync<S2WebSocketConnectException>(async () => await client.ConnectSessionAsync());
            Assert.That(exception!.Response.HTTPStatusCode, Is.EqualTo(HTTPStatusCode.Unauthorized));
            Assert.That(fixture.Server.Sessions.Count(),     Is.EqualTo(0));

        }

        [Test]
        [S2C("Communication.WebSocket.SingleUseToken")]
        public async Task Token_CanBeUsedOnlyOnce()
        {

            await using var fixture = new Fixture();
            await fixture.StartAsync();

            var token = fixture.Server.TokenStore.Issue();

            var first = fixture.CreateClient(token);
            await first.ConnectSessionAsync();
            await WaitFor(fixture.Started);

            var second = fixture.CreateClient(token);
            Assert.ThrowsAsync<S2WebSocketConnectException>(async () => await second.ConnectSessionAsync());

            await first.CloseSessionAsync();

        }

        [Test]
        public async Task Messages_ArriveInOrder_OverTheWire()
        {

            await using var fixture = new Fixture();
            await fixture.StartAsync();

            var client     = fixture.CreateClient(fixture.Server.TokenStore.Issue());
            var received   = new List<Int32>();
            var allDone    = new TaskCompletionSource<Boolean>();

            var rmSession  = await client.ConnectSessionAsync();
            var (cemSession, _) = await WaitFor(fixture.Started);

            cemSession.On<PowerMeasurement>((s, m, ct) => {
                received.Add((Int32) m.Values[0].Value);
                if (received.Count == 100) allDone.TrySetResult(true);
                return Task.FromResult<ReceptionStatusValue?>(null);
            });

            var sends = new List<Task<S2SendOutcome>>();
            for (var i = 0; i < 100; i++)
                sends.Add(rmSession.SendAndAwaitReceptionStatusAsync(new PowerMeasurement(DateTimeOffset.UtcNow, [ new PowerValue(CommodityQuantity.ElectricPowerL1, i) ])));

            var outcomes = await Task.WhenAll(sends).WaitAsync(Wait);
            await WaitFor(allDone);

            Assert.That(outcomes.All(o => o.IsOK), Is.True);
            Assert.That(received,                  Is.EqualTo(Enumerable.Range(0, 100)));

            await client.CloseSessionAsync();

        }

        [Test]
        [S2C("Communication.WebSocket.Termination")]
        public async Task SessionRequestTerminate_FromTheServer_ClosesTheClient()
        {

            await using var fixture = new Fixture();
            await fixture.StartAsync();

            var client     = fixture.CreateClient(fixture.Server.TokenStore.Issue());
            var rmSession  = await client.ConnectSessionAsync();
            var (cemSession, _) = await WaitFor(fixture.Started);

            var rmClosed = new TaskCompletionSource<S2CloseReason>();
            rmSession.OnClosed += (ts, s, reason) => { rmClosed.TrySetResult(reason); return Task.CompletedTask; };

            var outcome = await cemSession.SendAndAwaitReceptionStatusAsync(new SessionRequest(SessionRequestType.Terminate, "maintenance"));
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            var reason = await WaitFor(rmClosed);
            Assert.That(reason.Description,  Does.Contain("TERMINATE"));
            Assert.That(rmSession.State,     Is.EqualTo(S2SessionState.Disconnected));

            await WaitFor(fixture.Ended);
            Assert.That(fixture.Server.Sessions.Count(), Is.EqualTo(0));

        }

        [Test]
        public async Task ServerShutdown_EndsTheClientSession()
        {

            var fixture = new Fixture();
            await fixture.StartAsync();

            var client     = fixture.CreateClient(fixture.Server.TokenStore.Issue());
            var rmSession  = await client.ConnectSessionAsync();
            await WaitFor(fixture.Started);

            var rmClosed = new TaskCompletionSource<S2CloseReason>();
            rmSession.OnClosed += (ts, s, reason) => { rmClosed.TrySetResult(reason); return Task.CompletedTask; };

            await fixture.DisposeAsync();

            await WaitFor(rmClosed);
            Assert.That(rmSession.State, Is.EqualTo(S2SessionState.Disconnected));

        }

    }

}

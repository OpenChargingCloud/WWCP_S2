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

using Microsoft.Extensions.Time.Testing;

using cloud.charging.open.protocols.S2.Connect;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Connect
{

    /// <summary>
    /// A node hosted by the local endpoint (S2 Connect 1.0.0, "The node and the endpoint",
    /// "The pairing token, the node ID alias and the pairing code"): its description and
    /// capabilities, its own dynamic or static pairing token, the pairing tokens entered by
    /// the end user, and the resolution and consumption of tokens during a pairing attempt.
    /// </summary>
    [TestFixture]
    public sealed class HostedNodeTests
    {

        #region Data

        private const           String          TokenAlphabet   = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private static readonly Node_Id         NodeId          = Node_Id.     Parse("6f2f5c1e-0000-4000-8000-000000000001");
        private static readonly Node_Id         RemoteNodeA     = Node_Id.     Parse("6f2f5c1e-0000-4000-8000-0000000000a1");
        private static readonly Node_Id         RemoteNodeB     = Node_Id.     Parse("6f2f5c1e-0000-4000-8000-0000000000b2");
        private static readonly NodeIdAlias     Alias           = NodeIdAlias. Parse("A0");
        private static readonly PairingToken    OwnToken        = PairingToken.Parse("K7Q2P9");
        private static readonly PairingToken    StaticToken     = PairingToken.Parse("STATIC23");
        private static readonly PairingToken    EnteredTokenA   = PairingToken.Parse("AAAA2345");
        private static readonly PairingToken    WildcardToken   = PairingToken.Parse("WXYZ2345");
        private static readonly PairingToken    OtherToken      = PairingToken.Parse("OTHER234");
        private static readonly DateTimeOffset  Start           = new (2026, 9, 4, 10, 0, 0, TimeSpan.Zero);

        private static NodeDescription RMDescription(Node_Id?               Id      = null,
                                                     EnergyManagementRole?  Role    = null,
                                                     String                 Brand   = "ACME")

            => new (Id ?? NodeId,
                    Brand,
                    "EV charger",
                    "WallBox-b100",
                    Role ?? EnergyManagementRole.RM);

        private static HostedNode CreateNode(TimeProvider?  Clock       = null,
                                             Boolean        WithAlias   = true)

            => new (RMDescription(),
                    WithAlias ? Alias : null,
                    TimeProvider: Clock);

        #endregion


        #region Constructor: defaults and guards

        [Test]
        public void Constructor_AppliesTheDefaults()
        {

            var node = CreateNode();

            Assert.Multiple(() => {
                Assert.That(node.Id,                              Is.EqualTo(NodeId));
                Assert.That(node.Role,                            Is.EqualTo(EnergyManagementRole.RM));
                Assert.That(node.Alias,                           Is.EqualTo(Alias));
                Assert.That(node.Description,                     Is.EqualTo(RMDescription()));
                Assert.That(node.SupportedS2MessageVersions,      Is.EqualTo(new[] { Version.S2JSONVersion }));
                Assert.That(node.SupportedS2MessageVersions,      Is.EqualTo(new[] { "v1.0.0" }));
                Assert.That(node.SupportedCommunicationProtocols, Is.EqualTo(new[] { CommunicationProtocol.WebSocket }));
                Assert.That(node.IsReadyForPairing,               Is.True);
                Assert.That(node.HasValidPairingToken,            Is.False);
                Assert.That(node.PairingTokenIsStatic,            Is.False);
                Assert.That(node.PairingTokenExpiresAt,           Is.Null);
                Assert.That(node.PairingCode,                     Is.Null);
            });

        }

        [Test]
        public void Constructor_WithoutAlias()
        {
            Assert.That(CreateNode(WithAlias: false).Alias, Is.Null);
        }

        [Test]
        public void Constructor_DeduplicatesVersionsAndProtocols_KeepingTheOrder()
        {

            var node = new HostedNode(RMDescription(),
                                      SupportedS2MessageVersions:       [ "v1.0.0", "v1.0.0", "0.0.2-beta", "v1.0.0" ],
                                      SupportedCommunicationProtocols:  [ CommunicationProtocol.WebSocket, CommunicationProtocol.WebSocket ]);

            Assert.Multiple(() => {
                Assert.That(node.SupportedS2MessageVersions,      Is.EqualTo(new[] { "v1.0.0", "0.0.2-beta" }));
                Assert.That(node.SupportedCommunicationProtocols, Is.EqualTo(new[] { CommunicationProtocol.WebSocket }));
            });

        }

        [Test]
        public void Constructor_RejectsAnEmptyNodeIdentification()
        {
            Assert.That(() => new HostedNode(RMDescription(Id: default(Node_Id))), Throws.ArgumentException);
        }

        [Test]
        public void Constructor_RejectsEmptyVersionAndProtocolLists()
        {

            Assert.Multiple(() => {
                Assert.That(() => new HostedNode(RMDescription(), SupportedS2MessageVersions:      []),      Throws.ArgumentException);
                Assert.That(() => new HostedNode(RMDescription(), SupportedS2MessageVersions:      [ "" ]),  Throws.ArgumentException);
                Assert.That(() => new HostedNode(RMDescription(), SupportedS2MessageVersions:      [ " " ]), Throws.ArgumentException);
                Assert.That(() => new HostedNode(RMDescription(), SupportedCommunicationProtocols: []),      Throws.ArgumentException);
                Assert.That(() => new HostedNode(null!),                                                     Throws.ArgumentNullException);
            });

        }

        [Test]
        public void IsReadyForPairing_CanBeSwitchedOff()
        {

            var node = CreateNode();

            node.IsReadyForPairing = false;
            Assert.That(node.IsReadyForPairing, Is.False);

            node.IsReadyForPairing = true;
            Assert.That(node.IsReadyForPairing, Is.True);

        }

        #endregion

        #region UpdateDescription(...)

        [Test]
        public void UpdateDescription_ReplacesTheDescription_ButNotIdOrRole()
        {

            var node    = CreateNode();
            var updated = RMDescription(Brand: "ACME Energy");

            node.UpdateDescription(updated);

            Assert.Multiple(() => {
                Assert.That(node.Description,       Is.EqualTo(updated));
                Assert.That(node.Description.Brand, Is.EqualTo("ACME Energy"));
                Assert.That(node.Id,                Is.EqualTo(NodeId));
                Assert.That(node.Role,              Is.EqualTo(EnergyManagementRole.RM));
            });

        }

        [Test]
        public void UpdateDescription_RejectsAnotherIdOrRole()
        {

            var node = CreateNode();

            Assert.Multiple(() => {
                Assert.That(() => node.UpdateDescription(RMDescription(Id:   RemoteNodeA)),               Throws.ArgumentException);
                Assert.That(() => node.UpdateDescription(RMDescription(Role: EnergyManagementRole.CEM)), Throws.ArgumentException);
                Assert.That(() => node.UpdateDescription(null!),                                          Throws.ArgumentNullException);
                Assert.That(node.Description,                                                             Is.EqualTo(RMDescription()));
            });

        }

        #endregion


        #region Own pairing token: dynamic

        [Test]
        [S2C("PairingToken.Dynamic")]
        [S2C("Pairing.PairingCode")]
        public void IssueDynamicPairingToken_ReturnsASixCharacterCodeWithTheAlias()
        {

            var node = CreateNode();
            var code = node.IssueDynamicPairingToken();

            Assert.Multiple(() => {
                Assert.That(code.PairingToken.Length,                                    Is.EqualTo(6));
                Assert.That(code.PairingToken.Value.All(c => TokenAlphabet.Contains(c)), Is.True, "confusion-free alphabet");
                Assert.That(code.NodeIdAlias,                                            Is.EqualTo(Alias));
                Assert.That(code.Value,                                                  Is.EqualTo($"A0-{code.PairingToken.Value}"));
                Assert.That(node.PairingCode,                                            Is.EqualTo(code));
                Assert.That(node.HasValidPairingToken,                                   Is.True);
                Assert.That(node.PairingTokenIsStatic,                                   Is.False);
                Assert.That(node.PairingTokenExpiresAt,                                  Is.Not.Null);
                Assert.That(node.TryGetPairingToken(out var token),                      Is.True);
                Assert.That(token,                                                       Is.EqualTo(code.PairingToken));
            });

        }

        [Test]
        [S2C("PairingToken.Dynamic")]
        public void IssueDynamicPairingToken_HonoursTheLength_AndRejectsTooShortTokens()
        {

            var node = CreateNode(WithAlias: false);
            var code = node.IssueDynamicPairingToken(Length: 8);

            Assert.Multiple(() => {
                Assert.That(code.PairingToken.Length,                   Is.EqualTo(8));
                Assert.That(code.NodeIdAlias,                           Is.Null);
                Assert.That(code.Value,                                 Is.EqualTo(code.PairingToken.Value));
                Assert.That(() => node.IssueDynamicPairingToken(3),     Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(node.PairingCode,                           Is.EqualTo(code), "a rejected call leaves the token untouched");
            });

        }

        [Test]
        [S2C("PairingToken.Dynamic")]
        public void DynamicPairingToken_ExpiresAfterTheDefaultLifetime()
        {

            var clock = new FakeTimeProvider(Start);
            var node  = CreateNode(clock);

            node.IssueDynamicPairingToken();

            Assert.That(node.PairingTokenExpiresAt, Is.EqualTo(Start + S2ConnectDefaults.DynamicPairingTokenLifetime));
            Assert.That(node.PairingTokenExpiresAt, Is.EqualTo(Start + TimeSpan.FromMinutes(5)));

            clock.Advance(S2ConnectDefaults.DynamicPairingTokenLifetime - TimeSpan.FromSeconds(1));

            Assert.Multiple(() => {
                Assert.That(node.HasValidPairingToken,        Is.True, "one second before the expiry");
                Assert.That(node.TryGetPairingToken(out _),   Is.True);
                Assert.That(node.PairingCode,                 Is.Not.Null);
            });

            clock.Advance(TimeSpan.FromSeconds(1));

            Assert.Multiple(() => {
                Assert.That(node.HasValidPairingToken,        Is.False, "at the expiry");
                Assert.That(node.TryGetPairingToken(out _),   Is.False);
                Assert.That(node.PairingCode,                 Is.Null);
            });

        }

        [Test]
        [S2C("PairingToken.Dynamic")]
        public void SetDynamicPairingToken_UsesTheGivenTokenAndLifetime_AndReplacesTheEarlierToken()
        {

            var clock = new FakeTimeProvider(Start);
            var node  = CreateNode(clock);

            var first = node.SetDynamicPairingToken(OwnToken, TimeSpan.FromSeconds(30));

            Assert.Multiple(() => {
                Assert.That(first.PairingToken,                     Is.EqualTo(OwnToken));
                Assert.That(first.NodeIdAlias,                      Is.EqualTo(Alias));
                Assert.That(node.PairingTokenExpiresAt,             Is.EqualTo(Start + TimeSpan.FromSeconds(30)));
                Assert.That(node.TryGetPairingToken(out var token), Is.True);
                Assert.That(token,                                  Is.EqualTo(OwnToken));
            });

            var second = node.SetDynamicPairingToken(OtherToken);

            Assert.Multiple(() => {
                Assert.That(second.PairingToken,                    Is.EqualTo(OtherToken));
                Assert.That(node.PairingTokenExpiresAt,             Is.EqualTo(Start + S2ConnectDefaults.DynamicPairingTokenLifetime));
                Assert.That(node.TryGetPairingToken(out var token), Is.True);
                Assert.That(token,                                  Is.EqualTo(OtherToken), "the earlier token is replaced");
            });

        }

        [Test]
        [S2C("PairingToken.Dynamic")]
        public void SetDynamicPairingToken_RequiresFourCharactersAndAPositiveLifetime()
        {

            var node = CreateNode();

            Assert.Multiple(() => {
                Assert.That(() => node.SetDynamicPairingToken(default),                                   Throws.ArgumentException);
                Assert.That(() => node.SetDynamicPairingToken(OwnToken, TimeSpan.Zero),                   Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => node.SetDynamicPairingToken(OwnToken, TimeSpan.FromSeconds(-1)),        Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => node.SetDynamicPairingToken(PairingToken.Parse("ABCD")),                Throws.Nothing, "four characters are enough for a dynamic token");
                Assert.That(node.HasValidPairingToken,                                                    Is.True);
            });

        }

        #endregion

        #region Own pairing token: static

        [Test]
        [S2C("PairingToken.Static")]
        public void SetStaticPairingToken_NeverExpires()
        {

            var clock = new FakeTimeProvider(Start);
            var node  = CreateNode(clock);

            var code  = node.SetStaticPairingToken(StaticToken);

            Assert.Multiple(() => {
                Assert.That(code.Value,                 Is.EqualTo("A0-STATIC23"));
                Assert.That(node.PairingTokenIsStatic,  Is.True);
                Assert.That(node.PairingTokenExpiresAt, Is.Null);
                Assert.That(node.HasValidPairingToken,  Is.True);
            });

            clock.Advance(TimeSpan.FromDays(3650));

            Assert.Multiple(() => {
                Assert.That(node.HasValidPairingToken,              Is.True, "still valid ten years later");
                Assert.That(node.TryGetPairingToken(out var token), Is.True);
                Assert.That(token,                                  Is.EqualTo(StaticToken));
                Assert.That(node.PairingCode,                       Is.EqualTo(code));
            });

        }

        [Test]
        [S2C("PairingToken.Static")]
        public void SetStaticPairingToken_RequiresSixCharacters()
        {

            var node = CreateNode();

            Assert.Multiple(() => {
                Assert.That(() => node.SetStaticPairingToken(PairingToken.Parse("ABCD")),   Throws.ArgumentException);
                Assert.That(() => node.SetStaticPairingToken(PairingToken.Parse("ABCDE")),  Throws.ArgumentException);
                Assert.That(() => node.SetStaticPairingToken(default),                      Throws.ArgumentException);
                Assert.That(node.HasValidPairingToken,                                      Is.False);
                Assert.That(() => node.SetStaticPairingToken(PairingToken.Parse("ABCDEF")), Throws.Nothing);
                Assert.That(node.PairingTokenIsStatic,                                      Is.True);
            });

        }

        [Test]
        public void StaticAndDynamicTokens_ReplaceEachOther()
        {

            var node = CreateNode();

            node.SetStaticPairingToken(StaticToken);
            node.SetDynamicPairingToken(OwnToken);

            Assert.Multiple(() => {
                Assert.That(node.PairingTokenIsStatic,              Is.False);
                Assert.That(node.PairingTokenExpiresAt,             Is.Not.Null);
                Assert.That(node.TryGetPairingToken(out var token), Is.True);
                Assert.That(token,                                  Is.EqualTo(OwnToken));
            });

            node.SetStaticPairingToken(StaticToken);

            Assert.Multiple(() => {
                Assert.That(node.PairingTokenIsStatic,              Is.True);
                Assert.That(node.PairingTokenExpiresAt,             Is.Null);
                Assert.That(node.TryGetPairingToken(out var token), Is.True);
                Assert.That(token,                                  Is.EqualTo(StaticToken));
            });

        }

        [Test]
        public void ClearPairingToken_RemovesTheOwnToken()
        {

            var node = CreateNode();
            node.SetStaticPairingToken(StaticToken);

            node.ClearPairingToken();

            Assert.Multiple(() => {
                Assert.That(node.HasValidPairingToken,       Is.False);
                Assert.That(node.TryGetPairingToken(out _),  Is.False);
                Assert.That(node.PairingCode,                Is.Null);
                Assert.That(node.PairingTokenIsStatic,       Is.False);
                Assert.That(node.PairingTokenExpiresAt,      Is.Null);
            });

            Assert.That(node.ClearPairingToken, Throws.Nothing, "clearing twice is harmless");

        }

        #endregion


        #region Entered pairing tokens

        [Test]
        [S2C("Pairing.Initiator.EnteredToken")]
        public void EnterPairingToken_ForASpecificRemoteNode()
        {

            var node = CreateNode();

            node.EnterPairingToken(EnteredTokenA, RemoteNodeA);

            Assert.Multiple(() => {
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out var token), Is.True);
                Assert.That(token,                                                     Is.EqualTo(EnteredTokenA));
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeB, out _),        Is.False, "not valid for other remote nodes");
                Assert.That(node.HasValidPairingToken,                                 Is.False, "entered tokens are not own tokens");
            });

        }

        [Test]
        [S2C("Pairing.Initiator.EnteredToken")]
        public void EnterPairingToken_ForAnyRemoteNode()
        {

            var node = CreateNode();

            node.EnterPairingToken(WildcardToken);

            Assert.Multiple(() => {
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out var tokenA), Is.True);
                Assert.That(tokenA,                                                     Is.EqualTo(WildcardToken));
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeB, out var tokenB), Is.True);
                Assert.That(tokenB,                                                     Is.EqualTo(WildcardToken));
            });

        }

        [Test]
        public void EnterPairingToken_Guards()
        {

            var node = CreateNode();

            Assert.Multiple(() => {
                Assert.That(() => node.EnterPairingToken(default),                                       Throws.ArgumentException);
                Assert.That(() => node.EnterPairingToken(EnteredTokenA, RemoteNodeA, TimeSpan.Zero),     Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => node.EnterPairingToken(EnteredTokenA, null,        TimeSpan.FromSeconds(-1)), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out _),                          Is.False);
            });

        }

        [Test]
        [S2C("Pairing.Initiator.EnteredToken")]
        public void TryGetEnteredPairingToken_PrefersTheExactRemoteNodeOverTheWildcard()
        {

            var node = CreateNode();

            node.EnterPairingToken(WildcardToken);
            node.EnterPairingToken(EnteredTokenA, RemoteNodeA);

            Assert.Multiple(() => {
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out var tokenA), Is.True);
                Assert.That(tokenA,                                                     Is.EqualTo(EnteredTokenA));
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeB, out var tokenB), Is.True);
                Assert.That(tokenB,                                                     Is.EqualTo(WildcardToken));
            });

            Assert.That(node.RemoveEnteredPairingToken(RemoteNodeA), Is.True);

            Assert.Multiple(() => {
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out var tokenA), Is.True);
                Assert.That(tokenA,                                                     Is.EqualTo(WildcardToken), "falls back to the wildcard");
            });

        }

        [Test]
        [S2C("PairingToken.Dynamic")]
        public void EnteredPairingToken_ExpiresAfterItsLifetime()
        {

            var clock = new FakeTimeProvider(Start);
            var node  = CreateNode(clock);

            node.EnterPairingToken(EnteredTokenA, RemoteNodeA, TimeSpan.FromMinutes(1));
            node.EnterPairingToken(WildcardToken, null,        TimeSpan.FromMinutes(2));

            clock.Advance(TimeSpan.FromSeconds(59));

            Assert.Multiple(() => {
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out var token), Is.True);
                Assert.That(token,                                                     Is.EqualTo(EnteredTokenA));
            });

            clock.Advance(TimeSpan.FromSeconds(1));

            Assert.Multiple(() => {
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out var token), Is.True);
                Assert.That(token,                                                     Is.EqualTo(WildcardToken), "the expired exact token falls back to the wildcard");
            });

            clock.Advance(TimeSpan.FromMinutes(1));

            Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out _), Is.False, "the wildcard expired too");
            Assert.That(node.TryGetEnteredPairingToken(RemoteNodeB, out _), Is.False);

        }

        [Test]
        public void EnteredPairingToken_WithInfiniteLifetime_NeverExpires()
        {

            var clock = new FakeTimeProvider(Start);
            var node  = CreateNode(clock);

            node.EnterPairingToken(EnteredTokenA, RemoteNodeA, Timeout.InfiniteTimeSpan);
            node.EnterPairingToken(WildcardToken, null,        Timeout.InfiniteTimeSpan);

            clock.Advance(TimeSpan.FromDays(3650));

            Assert.Multiple(() => {
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out var tokenA), Is.True);
                Assert.That(tokenA,                                                     Is.EqualTo(EnteredTokenA));
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeB, out var tokenB), Is.True);
                Assert.That(tokenB,                                                     Is.EqualTo(WildcardToken));
            });

        }

        [Test]
        public void EnteredPairingToken_DefaultLifetime_IsTheDynamicTokenLifetime()
        {

            var clock = new FakeTimeProvider(Start);
            var node  = CreateNode(clock);

            node.EnterPairingToken(EnteredTokenA, RemoteNodeA);

            clock.Advance(S2ConnectDefaults.DynamicPairingTokenLifetime - TimeSpan.FromSeconds(1));
            Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out _), Is.True);

            clock.Advance(TimeSpan.FromSeconds(1));
            Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out _), Is.False);

        }

        [Test]
        public void RemoveEnteredPairingToken_ReportsWhetherATokenExisted()
        {

            var node = CreateNode();

            node.EnterPairingToken(EnteredTokenA, RemoteNodeA);
            node.EnterPairingToken(WildcardToken);

            Assert.Multiple(() => {
                Assert.That(node.RemoveEnteredPairingToken(RemoteNodeB),            Is.False, "nothing entered for B");
                Assert.That(node.RemoveEnteredPairingToken(RemoteNodeA),            Is.True);
                Assert.That(node.RemoveEnteredPairingToken(RemoteNodeA),            Is.False, "already removed");
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out var t), Is.True);
                Assert.That(t,                                                      Is.EqualTo(WildcardToken), "the wildcard remains");
                Assert.That(node.RemoveEnteredPairingToken(),                       Is.True);
                Assert.That(node.RemoveEnteredPairingToken(),                       Is.False);
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out _),     Is.False);
            });

        }

        [Test]
        public void ClearEnteredPairingTokens_ForgetsEverything_ButNotTheOwnToken()
        {

            var node = CreateNode();

            node.SetStaticPairingToken(StaticToken);
            node.EnterPairingToken(EnteredTokenA, RemoteNodeA);
            node.EnterPairingToken(WildcardToken);

            node.ClearEnteredPairingTokens();

            Assert.Multiple(() => {
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out _), Is.False);
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeB, out _), Is.False);
                Assert.That(node.HasValidPairingToken,                          Is.True);
            });

        }

        #endregion


        #region TryResolvePairingToken(...)

        [Test]
        [S2C("Pairing.0.Precondition")]
        public void TryResolvePairingToken_PrefersTheEnteredToken_AndMakesTheNodeTheInitiator()
        {

            var node = CreateNode();

            node.SetStaticPairingToken(StaticToken);
            node.EnterPairingToken(EnteredTokenA, RemoteNodeA);

            Assert.Multiple(() => {
                Assert.That(node.TryResolvePairingToken(RemoteNodeA, out var token, out var isInitiator), Is.True);
                Assert.That(token,                                                                        Is.EqualTo(EnteredTokenA));
                Assert.That(isInitiator,                                                                  Is.True);
            });

        }

        [Test]
        [S2C("Pairing.0.Precondition")]
        public void TryResolvePairingToken_FallsBackToTheOwnToken_AndMakesTheNodeTheResponder()
        {

            var node = CreateNode();

            node.SetStaticPairingToken(StaticToken);
            node.EnterPairingToken(EnteredTokenA, RemoteNodeA);

            Assert.Multiple(() => {
                Assert.That(node.TryResolvePairingToken(RemoteNodeB, out var token, out var isInitiator), Is.True);
                Assert.That(token,                                                                        Is.EqualTo(StaticToken));
                Assert.That(isInitiator,                                                                  Is.False);
            });

        }

        [Test]
        [S2C("Pairing.0.Precondition")]
        public void TryResolvePairingToken_WithoutAnyToken_Fails()
        {

            var clock = new FakeTimeProvider(Start);
            var node  = CreateNode(clock);

            Assert.Multiple(() => {
                Assert.That(node.TryResolvePairingToken(RemoteNodeA, out var token, out var isInitiator), Is.False);
                Assert.That(token,                                                                        Is.EqualTo(default(PairingToken)));
                Assert.That(isInitiator,                                                                  Is.False);
            });

            // An expired own token does not count either.
            node.IssueDynamicPairingToken(Lifetime: TimeSpan.FromSeconds(10));
            clock.Advance(TimeSpan.FromSeconds(10));

            Assert.That(node.TryResolvePairingToken(RemoteNodeA, out _, out _), Is.False);

        }

        #endregion

        #region ConsumePairingToken(...)

        [Test]
        public void ConsumePairingToken_RemovesTheMatchingEnteredToken()
        {

            var node = CreateNode();

            node.EnterPairingToken(EnteredTokenA, RemoteNodeA);
            node.EnterPairingToken(WildcardToken);

            node.ConsumePairingToken(RemoteNodeA, EnteredTokenA);

            Assert.Multiple(() => {
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out var token), Is.True);
                Assert.That(token,                                                     Is.EqualTo(WildcardToken), "the exact token is gone, the wildcard remains");
            });

            node.ConsumePairingToken(RemoteNodeB, WildcardToken);

            Assert.That(node.TryGetEnteredPairingToken(RemoteNodeB, out _), Is.False, "the wildcard was consumed");

        }

        [Test]
        [S2C("PairingToken.Dynamic")]
        public void ConsumePairingToken_ClearsAMatchingDynamicOwnToken()
        {

            var node = CreateNode();

            node.SetDynamicPairingToken(OwnToken);
            node.ConsumePairingToken(RemoteNodeA, OwnToken);

            Assert.Multiple(() => {
                Assert.That(node.HasValidPairingToken,  Is.False);
                Assert.That(node.PairingCode,           Is.Null);
                Assert.That(node.PairingTokenExpiresAt, Is.Null);
            });

        }

        [Test]
        [S2C("PairingToken.Static")]
        public void ConsumePairingToken_KeepsAStaticOwnToken()
        {

            var node = CreateNode();

            node.SetStaticPairingToken(StaticToken);
            node.ConsumePairingToken(RemoteNodeA, StaticToken);

            Assert.Multiple(() => {
                Assert.That(node.HasValidPairingToken,              Is.True);
                Assert.That(node.PairingTokenIsStatic,              Is.True);
                Assert.That(node.TryGetPairingToken(out var token), Is.True);
                Assert.That(token,                                  Is.EqualTo(StaticToken));
            });

        }

        [Test]
        public void ConsumePairingToken_KeepsNonMatchingTokens()
        {

            var node = CreateNode();

            node.SetDynamicPairingToken(OwnToken);
            node.EnterPairingToken(EnteredTokenA, RemoteNodeA);
            node.EnterPairingToken(WildcardToken);

            node.ConsumePairingToken(RemoteNodeA, OtherToken);
            node.ConsumePairingToken(RemoteNodeB, OtherToken);

            Assert.Multiple(() => {
                Assert.That(node.HasValidPairingToken,                                  Is.True);
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeA, out var tokenA), Is.True);
                Assert.That(tokenA,                                                     Is.EqualTo(EnteredTokenA));
                Assert.That(node.TryGetEnteredPairingToken(RemoteNodeB, out var tokenB), Is.True);
                Assert.That(tokenB,                                                     Is.EqualTo(WildcardToken));
            });

        }

        #endregion


        #region ToString()

        [Test]
        public void ToString_ContainsNoTokenText()
        {

            var node = CreateNode();

            node.SetStaticPairingToken(StaticToken);
            node.EnterPairingToken(EnteredTokenA, RemoteNodeA);
            node.EnterPairingToken(WildcardToken);

            var text = node.ToString();

            Assert.Multiple(() => {
                Assert.That(text, Does.Contain(NodeId.ToString()));
                Assert.That(text, Does.Contain("A0"));
                Assert.That(text, Does.Contain("RM"));
                Assert.That(text, Does.Not.Contain(StaticToken.Value));
                Assert.That(text, Does.Not.Contain(EnteredTokenA.Value));
                Assert.That(text, Does.Not.Contain(WildcardToken.Value));
            });

        }

        #endregion

    }

}

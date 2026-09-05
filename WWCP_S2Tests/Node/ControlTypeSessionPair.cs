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

using cloud.charging.open.protocols.S2.Node;
using cloud.charging.open.protocols.S2.Session;

#endregion

namespace cloud.charging.open.protocols.S2.Tests.Node
{

    /// <summary>
    /// A connected pair of started sessions - a CEM and an RM over an <see cref="InMemoryS2Medium"/>,
    /// no network - with one control-type handler registered on each side. Activation is exercised
    /// the way production does it: the CEM sends a <see cref="SelectControlType"/>, which activates
    /// the CEM on send and the RM on receipt, so the real message rules of the session run.
    ///
    /// The FRBC suite (<see cref="FRBCControlTypeTests"/>) predates this helper and carries its own
    /// copy; the PEBC, PPBC, OMBC and DDBC suites share this one.
    /// </summary>
    internal sealed class ControlTypeSessionPair : IAsyncDisposable
    {

        #region Data

        /// <summary>
        /// Upper bound for any single cross-session propagation. In-memory delivery is sub-millisecond,
        /// so this only guards against a genuine hang; every test finishes far below it.
        /// </summary>
        public static readonly TimeSpan  Timeout      = TimeSpan.FromSeconds(2);

        /// <summary>
        /// How long to wait before asserting that something did NOT happen (e.g. no auto-acknowledge).
        /// </summary>
        public static readonly TimeSpan  SettleDelay  = TimeSpan.FromMilliseconds(200);

        #endregion

        #region Properties

        /// <summary>
        /// The CEM side of the pair.
        /// </summary>
        public S2Session  CEM    { get; }

        /// <summary>
        /// The RM side of the pair.
        /// </summary>
        public S2Session  RM     { get; }

        #endregion

        #region Constructor(s)

        private ControlTypeSessionPair(S2Session CEM,
                                       S2Session RM)
        {
            this.CEM  = CEM;
            this.RM   = RM;
        }

        #endregion


        #region CreateAsync(EnergyManager, ResourceManager)

        /// <summary>
        /// Build a started CEM/RM session pair over the in-memory medium and register the two
        /// handlers. The control type is not yet active; call <see cref="ActivateAsync"/> for that.
        /// </summary>
        /// <param name="EnergyManager">The CEM-side handler.</param>
        /// <param name="ResourceManager">The RM-side handler.</param>
        public static async Task<ControlTypeSessionPair> CreateAsync(IS2ControlTypeHandler  EnergyManager,
                                                                     IS2ControlTypeHandler  ResourceManager)
        {

            var (a, b) = InMemoryS2Medium.CreatePair("CEM", "RM");

            var cem = new S2Session(a, new S2SessionOptions {
                                           Role               = EnergyManagementRole.CEM,
                                           Mode               = S2SessionMode.S2Connect,
                                           NegotiatedVersion  = Version.S2JSONVersion
                                       });

            var rm  = new S2Session(b, new S2SessionOptions {
                                           Role               = EnergyManagementRole.RM,
                                           Mode               = S2SessionMode.S2Connect,
                                           NegotiatedVersion  = Version.S2JSONVersion
                                       });

            cem.RegisterControlType(EnergyManager);
            rm. RegisterControlType(ResourceManager);

            await cem.StartAsync();
            await rm. StartAsync();

            return new ControlTypeSessionPair(cem, rm);

        }

        #endregion

        #region ActivateAsync(ControlType)

        /// <summary>
        /// The CEM selects the control type. The CEM activates on send, the RM on receipt; the RM's
        /// handler then sends whatever it opens with. Waits until the control type is active on both
        /// sides - each test waits for its own opening message itself.
        /// </summary>
        /// <param name="ControlType">The control type to select.</param>
        public async Task ActivateAsync(ControlType ControlType)
        {

            var outcome = await CEM.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType));
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            await WaitUntil(() => CEM.ActiveControlType == ControlType &&
                                  RM. ActiveControlType == ControlType,
                            $"{ControlType} was not activated on both sessions.");

        }

        #endregion

        #region DeactivateAsync()

        /// <summary>
        /// The CEM selects NO_SELECTION, which the session treats as deactivation: the active handler
        /// is deactivated and both sides return to WebSocketConnected.
        /// </summary>
        public async Task DeactivateAsync()
        {

            var outcome = await CEM.SendAndAwaitReceptionStatusAsync(new SelectControlType(ControlType.NoSelection));
            Assert.That(outcome.IsOK, Is.True, outcome.ReceptionStatus?.DiagnosticLabel);

            await WaitUntil(() => CEM.ActiveControlType is null &&
                                  RM. ActiveControlType is null,
                            "the control type was not deactivated on both sessions.");

        }

        #endregion

        #region WaitUntil(Condition, Message)

        /// <summary>
        /// Poll the condition until it holds or <see cref="Timeout"/> elapses (then fail).
        /// </summary>
        /// <param name="Condition">The condition to wait for.</param>
        /// <param name="Message">The failure message.</param>
        public static async Task WaitUntil(Func<Boolean>  Condition,
                                           String         Message)
        {

            var deadline = DateTimeOffset.UtcNow + Timeout;

            while (DateTimeOffset.UtcNow < deadline)
            {

                if (Condition())
                    return;

                await Task.Delay(5);

            }

            Assert.Fail(Message);

        }

        #endregion

        #region DisposeAsync()

        /// <summary>
        /// Close both sessions and, through them, the in-memory medium.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await CEM.DisposeAsync();
            await RM. DisposeAsync();
        }

        #endregion

    }

}

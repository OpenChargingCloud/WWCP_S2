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

namespace cloud.charging.open.protocols.S2.Node
{

    /// <summary>
    /// The message-handler registrations of one control-type handler, disposed together when the
    /// session deactivates it. Every CEM-side handler listens to several message types at once,
    /// and <see cref="Session.IS2ControlTypeHandler.RegisterHandlers"/> returns exactly one
    /// disposable for all of them.
    /// </summary>
    internal sealed class CompositeDisposable : IDisposable
    {

        private readonly List<IDisposable> disposables = [];

        /// <summary>
        /// Add a registration to be disposed with this one.
        /// </summary>
        /// <param name="Disposable">A registration.</param>
        public void Add(IDisposable Disposable)
            => disposables.Add(Disposable);

        /// <summary>
        /// Dispose every registration.
        /// </summary>
        public void Dispose()
        {

            foreach (var disposable in disposables)
                disposable.Dispose();

            disposables.Clear();

        }

    }

}

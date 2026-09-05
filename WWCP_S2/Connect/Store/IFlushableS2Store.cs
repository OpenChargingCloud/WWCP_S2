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
    /// A store that buffers writes and can flush them on demand. Every <see cref="IS2Store"/>
    /// operation is durable before it returns; <see cref="FlushAsync"/> only forces out anything a
    /// store batches for performance and is called by the node layer during shutdown. A store that
    /// writes through implements this as a no-op.
    /// </summary>
    public interface IFlushableS2Store : IS2Store
    {

        /// <summary>
        /// Flush any buffered writes to durable storage.
        /// </summary>
        /// <param name="CancellationToken">A token to cancel the flush.</param>
        Task FlushAsync(CancellationToken CancellationToken = default);

    }

}

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

using System.Collections.Concurrent;

#endregion

namespace cloud.charging.open.protocols.S2.Session
{

    /// <summary>
    /// Correlates ReceptionStatus messages with the messages they acknowledge. An awaiter is
    /// registered before the message leaves the socket (PLAN.md §3.1), so a fast peer cannot
    /// answer before anybody listens.
    /// </summary>
    public sealed class ReceptionStatusAwaiter
    {

        #region Data

        private readonly ConcurrentDictionary<Message_Id, TaskCompletionSource<ReceptionStatus>> pending = new ();

        #endregion

        #region Properties

        /// <summary>
        /// The number of messages waiting for their ReceptionStatus.
        /// </summary>
        public Int32 PendingCount
            => pending.Count;

        #endregion


        #region Register(MessageId, out ReceptionStatusTask)

        /// <summary>
        /// Register an awaiter for the ReceptionStatus of the given message. Dispose the
        /// returned registration to stop waiting (e.g. after a timeout).
        /// </summary>
        /// <param name="MessageId">The identification of the message about to be sent.</param>
        /// <param name="ReceptionStatusTask">A task completing with the ReceptionStatus.</param>
        public IDisposable Register(Message_Id                 MessageId,
                                    out Task<ReceptionStatus>  ReceptionStatusTask)
        {

            var tcs = new TaskCompletionSource<ReceptionStatus>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (!pending.TryAdd(MessageId, tcs))
                throw new InvalidOperationException($"A ReceptionStatus for message '{MessageId}' is already awaited!");

            ReceptionStatusTask = tcs.Task;

            return new Registration(this, MessageId);

        }

        #endregion

        #region TryComplete(ReceptionStatus)

        /// <summary>
        /// Hand a received ReceptionStatus to its awaiter.
        /// </summary>
        /// <param name="ReceptionStatus">A received ReceptionStatus.</param>
        /// <returns>False when nobody awaits a ReceptionStatus for the subject message.</returns>
        public Boolean TryComplete(ReceptionStatus ReceptionStatus)
        {

            if (pending.TryRemove(ReceptionStatus.SubjectMessageId, out var tcs))
                return tcs.TrySetResult(ReceptionStatus);

            return false;

        }

        #endregion

        #region FailAll(Exception)

        /// <summary>
        /// Fail every pending awaiter, e.g. because the session was closed.
        /// </summary>
        /// <param name="Exception">The exception the awaiters observe.</param>
        public void FailAll(Exception Exception)
        {

            foreach (var messageId in pending.Keys.ToList())
            {
                if (pending.TryRemove(messageId, out var tcs))
                    tcs.TrySetException(Exception);
            }

        }

        #endregion


        #region (private) Registration

        private sealed class Registration(ReceptionStatusAwaiter  Awaiter,
                                          Message_Id              MessageId) : IDisposable
        {

            public void Dispose()
            {
                if (Awaiter.pending.TryRemove(MessageId, out var tcs))
                    tcs.TrySetCanceled();
            }

        }

        #endregion

    }

}

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

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// The structured result of a failed message parse: the ReceptionStatus value the
    /// receiver has to answer with, the subject message identification (the null UUID when
    /// none could be extracted) and a diagnostic label (PLAN.md §3.2).
    /// </summary>
    /// <param name="Status">The reception status value: INVALID_DATA, INVALID_MESSAGE or INVALID_CONTENT.</param>
    /// <param name="SubjectMessageId">The message identification the error refers to, or Message_Id.Null.</param>
    /// <param name="DiagnosticLabel">A diagnostic label for debugging (not for HMI purposes).</param>
    /// <param name="MessageType">The message type of the failed message, when it could be extracted.</param>
    public sealed record S2ParseError(ReceptionStatusValue  Status,
                                      Message_Id            SubjectMessageId,
                                      String                DiagnosticLabel,
                                      String?               MessageType   = null)
    {

        #region Properties

        /// <summary>
        /// Whether the failed message was (meant to be) a ReceptionStatus, which is never
        /// answered with another ReceptionStatus.
        /// </summary>
        public Boolean IsReceptionStatus
            => String.Equals(MessageType, ReceptionStatus.MessageTypeName, StringComparison.Ordinal);

        #endregion

        #region (static) InvalidData(DiagnosticLabel, MessageType = null)

        /// <summary>
        /// The message was not understood (not valid JSON, not a JSON object, no message identification found).
        /// </summary>
        /// <param name="DiagnosticLabel">A diagnostic label.</param>
        /// <param name="MessageType">The message type, when it could be extracted.</param>
        public static S2ParseError InvalidData(String   DiagnosticLabel,
                                               String?  MessageType   = null)
            => new (ReceptionStatusValue.InvalidData,
                    Message_Id.Null,
                    DiagnosticLabel,
                    MessageType);

        #endregion

        #region (static) InvalidMessage(SubjectMessageId, DiagnosticLabel, MessageType = null)

        /// <summary>
        /// The message was not according to the schema (unknown message type, missing or
        /// invalid properties, violated intra-message rule).
        /// </summary>
        /// <param name="SubjectMessageId">The message identification.</param>
        /// <param name="DiagnosticLabel">A diagnostic label.</param>
        /// <param name="MessageType">The message type, when it could be extracted.</param>
        public static S2ParseError InvalidMessage(Message_Id  SubjectMessageId,
                                                  String      DiagnosticLabel,
                                                  String?     MessageType   = null)
            => new (ReceptionStatusValue.InvalidMessage,
                    SubjectMessageId,
                    DiagnosticLabel,
                    MessageType);

        #endregion

        #region (static) InvalidContent(SubjectMessageId, DiagnosticLabel)

        /// <summary>
        /// The message content is invalid in the context of the session (e.g. refers to a non-existing identification).
        /// </summary>
        /// <param name="SubjectMessageId">The message identification.</param>
        /// <param name="DiagnosticLabel">A diagnostic label.</param>
        public static S2ParseError InvalidContent(Message_Id  SubjectMessageId,
                                                  String      DiagnosticLabel)
            => new (ReceptionStatusValue.InvalidContent,
                    SubjectMessageId,
                    DiagnosticLabel);

        #endregion

        #region ToReceptionStatus()

        /// <summary>
        /// Create the ReceptionStatus message answering the failed message.
        /// </summary>
        public ReceptionStatus ToReceptionStatus()
            => new (SubjectMessageId,
                    Status,
                    DiagnosticLabel);

        #endregion

    }

}

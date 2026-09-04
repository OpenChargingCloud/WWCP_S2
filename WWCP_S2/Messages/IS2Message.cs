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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// The common interface of all S2 messages: the 35 messages that carry a message_id
    /// (see <see cref="IS2MessageWithId"/>) and the ReceptionStatus, which refers to another
    /// message instead.
    /// </summary>
    public interface IS2Message
    {

        /// <summary>
        /// The message type as used in the "message_type" property, e.g. "FRBC.SystemDescription".
        /// </summary>
        String   MessageType    { get; }

        /// <summary>
        /// Return the complete JSON representation of this message, including
        /// "message_type" and, where applicable, "message_id".
        /// </summary>
        JObject  ToJSON();

    }


    /// <summary>
    /// The common interface of all S2 messages that carry a "message_id" and are therefore
    /// acknowledged with a ReceptionStatus.
    /// </summary>
    public interface IS2MessageWithId : IS2Message
    {

        /// <summary>
        /// The unique identification of this message.
        /// </summary>
        Message_Id  MessageId    { get; }

    }


    /// <summary>
    /// The common interface of the 13 revokable message types (RevokableObjects.schema.json).
    /// A RevokeObject refers to a revokable object by its object type and object identification:
    /// the "id" of instructions, constraints and profile definitions, and the "message_id" of
    /// the OMBC/FRBC/DDBC system descriptions, which carry no "id" of their own.
    /// </summary>
    public interface IRevokable : IS2MessageWithId
    {

        /// <summary>
        /// The revokable object type of this message.
        /// </summary>
        RevokableObject  RevokableObjectType    { get; }

        /// <summary>
        /// The identification a RevokeObject uses to refer to this message.
        /// </summary>
        S2Object_Id      RevokableObjectId      { get; }

    }


    /// <summary>
    /// The common interface of the seven instruction messages sent by a CEM
    /// (PEBC.Instruction, PPBC.ScheduleInstruction, PPBC.StartInterruptionInstruction,
    /// PPBC.EndInterruptionInstruction, OMBC.Instruction, FRBC.Instruction, DDBC.Instruction).
    /// </summary>
    public interface IInstruction : IRevokable
    {

        /// <summary>
        /// The identification of this instruction. Must be unique in the scope of the Resource
        /// Manager, for at least the duration of the session between Resource Manager and CEM.
        /// </summary>
        Instruction_Id  Id                   { get; }

        /// <summary>
        /// The moment the execution of the instruction shall start. When the specified
        /// execution time is in the past, execution must start as soon as possible.
        /// </summary>
        DateTimeOffset  ExecutionTime        { get; }

        /// <summary>
        /// Whether this is an instruction during an abnormal condition.
        /// </summary>
        Boolean         AbnormalCondition    { get; }

    }

}

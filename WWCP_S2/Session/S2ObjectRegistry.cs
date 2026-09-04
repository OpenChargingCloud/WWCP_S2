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

namespace cloud.charging.open.protocols.S2.Session
{

    /// <summary>
    /// The direction a message travelled in.
    /// </summary>
    public enum S2MessageDirection
    {

        /// <summary>
        /// The local node sent the message.
        /// </summary>
        Sent,

        /// <summary>
        /// The local node received the message.
        /// </summary>
        Received

    }


    /// <summary>
    /// The state of an instruction tracked by a session.
    /// </summary>
    /// <param name="Instruction">The instruction message.</param>
    /// <param name="Direction">Whether the local node sent or received it.</param>
    /// <param name="Status">The last known status.</param>
    /// <param name="LastUpdate">When the status was last updated.</param>
    public sealed record InstructionState(IInstruction        Instruction,
                                          S2MessageDirection  Direction,
                                          InstructionStatus   Status,
                                          DateTimeOffset      LastUpdate)
    {

        /// <summary>
        /// Whether the instruction reached a final state (REJECTED, REVOKED, SUCCEEDED, ABORTED).
        /// </summary>
        public Boolean IsFinal
            => Status == InstructionStatus.Rejected  ||
               Status == InstructionStatus.Revoked   ||
               Status == InstructionStatus.Succeeded ||
               Status == InstructionStatus.Aborted;

    }


    /// <summary>
    /// The per-session registry of revokable objects and instruction states on both roles
    /// (PLAN.md §3.4): the last message per revokable object keyed by object type and
    /// identification, the instruction lifecycle, and the identifications the active system
    /// description declared (actuators, operation modes, timers) so that references in
    /// instructions, statuses and revocations can be validated (INVALID_CONTENT).
    /// The session mutates the registry only from its single consumer loop and its send path
    /// under its own lock; the registry is not thread-safe on its own, and the collection
    /// properties are live views that callers should snapshot (e.g. with ToList()) before
    /// iterating them outside a handler.
    /// </summary>
    public sealed class S2ObjectRegistry
    {

        #region Data

        private readonly Dictionary<(RevokableObject Type, S2Object_Id Id), (IRevokable Message, S2MessageDirection Direction)>  revokables    = [];
        private readonly Dictionary<Instruction_Id, InstructionState>                                                            instructions  = [];
        private readonly HashSet<Actuator_Id>                                                                                    actuators     = [];
        private readonly HashSet<OperationMode_Id>                                                                               operationModes = [];
        private readonly HashSet<Timer_Id>                                                                                       timers        = [];

        #endregion

        #region Properties

        /// <summary>
        /// The last ResourceManagerDetails exchanged in this session.
        /// </summary>
        public ResourceManagerDetails?                          ResourceManagerDetails    { get; set; }

        /// <summary>
        /// The revokable objects currently known, with the direction they travelled in.
        /// </summary>
        public IEnumerable<(IRevokable Message, S2MessageDirection Direction)>  Revokables
            => revokables.Values;

        /// <summary>
        /// The instructions currently tracked.
        /// </summary>
        public IEnumerable<InstructionState>                    Instructions
            => instructions.Values;

        /// <summary>
        /// The actuator identifications declared by the active system description.
        /// </summary>
        public IReadOnlySet<Actuator_Id>                        Actuators
            => actuators;

        /// <summary>
        /// The operation mode identifications declared by the active system description.
        /// </summary>
        public IReadOnlySet<OperationMode_Id>                   OperationModes
            => operationModes;

        /// <summary>
        /// The timer identifications declared by the active system description.
        /// </summary>
        public IReadOnlySet<Timer_Id>                           Timers
            => timers;

        #endregion


        #region Register(Revokable, Direction, Now)

        /// <summary>
        /// Register (or replace) a revokable object; instructions additionally start their
        /// lifecycle in status NEW.
        /// </summary>
        /// <param name="Revokable">A revokable message.</param>
        /// <param name="Direction">Whether the local node sent or received it.</param>
        /// <param name="Now">The current time.</param>
        public void Register(IRevokable          Revokable,
                             S2MessageDirection  Direction,
                             DateTimeOffset      Now)
        {

            revokables[(Revokable.RevokableObjectType, Revokable.RevokableObjectId)] = (Revokable, Direction);

            if (Revokable is IInstruction instruction)
                instructions[instruction.Id] = new InstructionState(instruction, Direction, InstructionStatus.New, Now);

        }

        #endregion

        #region TryGetRevokable(Type, Id, out Revokable)

        /// <summary>
        /// Try to find a registered revokable object.
        /// </summary>
        /// <param name="Type">The revokable object type.</param>
        /// <param name="Id">The object identification.</param>
        /// <param name="Revokable">The registered message.</param>
        public Boolean TryGetRevokable(RevokableObject  Type,
                                       S2Object_Id      Id,
                                       out IRevokable?  Revokable)
        {

            if (revokables.TryGetValue((Type, Id), out var entry))
            {
                Revokable = entry.Message;
                return true;
            }

            Revokable = null;
            return false;

        }

        #endregion

        #region Revoke(RevokeObject, Now)

        /// <summary>
        /// Apply a RevokeObject: the object is removed and a revoked instruction is marked REVOKED.
        /// </summary>
        /// <param name="RevokeObject">A RevokeObject message.</param>
        /// <param name="Now">The current time.</param>
        /// <returns>False when the object was not registered (INVALID_CONTENT).</returns>
        public Boolean Revoke(RevokeObject    RevokeObject,
                              DateTimeOffset  Now)
        {

            if (!revokables.Remove((RevokeObject.ObjectType, RevokeObject.ObjectId), out var entry))
                return false;

            if (entry.Message is IInstruction instruction &&
                instructions.TryGetValue(instruction.Id, out var state))
            {
                instructions[instruction.Id] = state with { Status = InstructionStatus.Revoked, LastUpdate = Now };
            }

            return true;

        }

        #endregion

        #region TryUpdateInstruction(Update, out State)

        /// <summary>
        /// Apply an InstructionStatusUpdate.
        /// </summary>
        /// <param name="Update">An InstructionStatusUpdate message.</param>
        /// <param name="State">The updated instruction state.</param>
        /// <returns>False when the instruction is unknown (INVALID_CONTENT).</returns>
        public Boolean TryUpdateInstruction(InstructionStatusUpdate  Update,
                                            out InstructionState?    State)
        {

            if (!instructions.TryGetValue(Update.InstructionId, out var current))
            {
                State = null;
                return false;
            }

            State                               = current with { Status = Update.StatusType, LastUpdate = Update.Timestamp };
            instructions[Update.InstructionId]  = State;
            return true;

        }

        #endregion

        #region TryGetInstruction(Id, out State)

        /// <summary>
        /// Try to find a tracked instruction.
        /// </summary>
        /// <param name="Id">The instruction identification.</param>
        /// <param name="State">The instruction state.</param>
        public Boolean TryGetInstruction(Instruction_Id         Id,
                                         out InstructionState?  State)
            => instructions.TryGetValue(Id, out State);

        #endregion

        #region SetDeclaredIds(Actuators, OperationModes, Timers)

        /// <summary>
        /// Replace the identifications declared by a new system description.
        /// </summary>
        /// <param name="Actuators">The actuator identifications (empty for OMBC).</param>
        /// <param name="OperationModes">The operation mode identifications.</param>
        /// <param name="Timers">The timer identifications.</param>
        public void SetDeclaredIds(IEnumerable<Actuator_Id>       Actuators,
                                   IEnumerable<OperationMode_Id>  OperationModes,
                                   IEnumerable<Timer_Id>          Timers)
        {

            actuators.     Clear();
            operationModes.Clear();
            timers.        Clear();

            actuators.     UnionWith(Actuators);
            operationModes.UnionWith(OperationModes);
            timers.        UnionWith(Timers);

        }

        #endregion

        #region Clear()

        /// <summary>
        /// Forget every revokable object, instruction and declared identification, e.g. when the
        /// control type is deactivated or switched. The ResourceManagerDetails are kept.
        /// </summary>
        public void Clear()
        {
            revokables.    Clear();
            instructions.  Clear();
            actuators.     Clear();
            operationModes.Clear();
            timers.        Clear();
        }

        #endregion

    }

}

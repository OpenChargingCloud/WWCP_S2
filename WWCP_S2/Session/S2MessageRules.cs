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
    /// The verdict of the message rules for one received or sent message.
    /// </summary>
    public enum S2RuleVerdict
    {

        /// <summary>
        /// The message is allowed in the current state and direction.
        /// </summary>
        Allowed,

        /// <summary>
        /// The message is not allowed in the current session state (e.g. a control type
        /// message while no or another control type is active).
        /// </summary>
        OutOfState,

        /// <summary>
        /// The message is never sent by this role (e.g. a CEM sending ResourceManagerDetails).
        /// </summary>
        WrongDirection,

        /// <summary>
        /// The message is forbidden in this session mode (Handshake in S2 Connect mode).
        /// </summary>
        ForbiddenInMode,

        /// <summary>
        /// The message type is unknown.
        /// </summary>
        UnknownMessageType

    }


    /// <summary>
    /// Which messages may be exchanged in which session state, derived from the message
    /// type names (control type prefix and instruction suffix) and the sender role. The
    /// "State of communication" table of S2 Connect 1.0.0 is the test oracle, with the
    /// documented exceptions: DDBC.PresentDemandStatus is allowed, RevokeObject is allowed
    /// in both directions (PLAN.md §3.4).
    /// </summary>
    public static class S2MessageRules
    {

        #region Data

        private static readonly Dictionary<String, ControlType> prefixes = new (StringComparer.Ordinal) {
            ["PEBC."]  = ControlType.PowerEnvelopeBasedControl,
            ["PPBC."]  = ControlType.PowerProfileBasedControl,
            ["OMBC."]  = ControlType.OperationModeBasedControl,
            ["FRBC."]  = ControlType.FillRateBasedControl,
            ["DDBC."]  = ControlType.DemandDrivenBasedControl
        };

        // Common messages and the roles that send them.
        private static readonly Dictionary<String, (Boolean CEM, Boolean RM)> commonMessages = new (StringComparer.Ordinal) {
            ["Handshake"]                = (true,  true),
            ["HandshakeResponse"]        = (true,  false),
            ["ReceptionStatus"]          = (true,  true),
            ["ResourceManagerDetails"]   = (false, true),
            ["SelectControlType"]        = (true,  false),
            ["SessionRequest"]           = (true,  true),
            ["PowerMeasurement"]         = (false, true),
            ["PowerForecast"]            = (false, true),
            ["InstructionStatusUpdate"]  = (false, true),
            ["RevokeObject"]             = (true,  true)
        };

        #endregion


        #region ControlTypePrefix(ControlType)

        /// <summary>
        /// The message type prefix of the given control type ("FRBC." for FILL_RATE_BASED_CONTROL),
        /// or null for NOT_CONTROLABLE, NO_SELECTION and unknown control types.
        /// </summary>
        /// <param name="ControlType">A control type.</param>
        public static String? ControlTypePrefix(ControlType ControlType)
        {

            foreach (var kvp in prefixes)
            {
                if (kvp.Value == ControlType)
                    return kvp.Key;
            }

            return null;

        }

        #endregion

        #region ControlTypeOf(MessageType)

        /// <summary>
        /// The control type a message type belongs to ("FRBC.Instruction" → FILL_RATE_BASED_CONTROL),
        /// or null for the common messages.
        /// </summary>
        /// <param name="MessageType">A message type.</param>
        public static ControlType? ControlTypeOf(String MessageType)
        {

            foreach (var kvp in prefixes)
            {
                if (MessageType.StartsWith(kvp.Key, StringComparison.Ordinal))
                    return kvp.Value;
            }

            return null;

        }

        #endregion

        #region IsInstruction(MessageType)

        /// <summary>
        /// Whether the given control type message is an instruction (sent by the CEM).
        /// </summary>
        /// <param name="MessageType">A message type.</param>
        public static Boolean IsInstruction(String MessageType)
            => ControlTypeOf(MessageType) is not null &&
               MessageType.EndsWith("Instruction", StringComparison.Ordinal);

        #endregion

        #region IsSentBy(MessageType, Role)

        /// <summary>
        /// Whether the given message type is sent by the given role.
        /// </summary>
        /// <param name="MessageType">A message type.</param>
        /// <param name="Role">An energy management role.</param>
        public static Boolean IsSentBy(String                MessageType,
                                       EnergyManagementRole  Role)
        {

            if (commonMessages.TryGetValue(MessageType, out var roles))
                return Role == EnergyManagementRole.CEM ? roles.CEM : roles.RM;

            if (ControlTypeOf(MessageType) is not null)
                return IsInstruction(MessageType)
                           ? Role == EnergyManagementRole.CEM
                           : Role == EnergyManagementRole.RM;

            return false;

        }

        #endregion

        #region Check(MessageType, Sender, Mode, State, ActiveControlType)

        /// <summary>
        /// Check whether the given message type may be sent by the given role in the given
        /// session state.
        /// </summary>
        /// <param name="MessageType">A message type.</param>
        /// <param name="Sender">The role sending the message.</param>
        /// <param name="Mode">The session mode.</param>
        /// <param name="State">The session state.</param>
        /// <param name="ActiveControlType">The active control type, when the state is ControlTypeActivated.</param>
        public static S2RuleVerdict Check(String                MessageType,
                                          EnergyManagementRole  Sender,
                                          S2SessionMode         Mode,
                                          S2SessionState        State,
                                          ControlType?          ActiveControlType)
        {

            var isCommon      = commonMessages.ContainsKey(MessageType);
            var controlType   = ControlTypeOf(MessageType);

            if (!isCommon && controlType is null)
                return S2RuleVerdict.UnknownMessageType;

            if (!IsSentBy(MessageType, Sender))
                return S2RuleVerdict.WrongDirection;

            var isHandshake = MessageType is "Handshake" or "HandshakeResponse";

            if (isHandshake && Mode == S2SessionMode.S2Connect)
                return S2RuleVerdict.ForbiddenInMode;

            switch (State)
            {

                case S2SessionState.AwaitingHandshake:
                    return isHandshake || MessageType is "ReceptionStatus" or "SessionRequest"
                               ? S2RuleVerdict.Allowed
                               : S2RuleVerdict.OutOfState;

                case S2SessionState.WebSocketConnected:
                    if (isHandshake || controlType is not null)
                        return S2RuleVerdict.OutOfState;
                    return MessageType is "RevokeObject" or "InstructionStatusUpdate"
                               ? S2RuleVerdict.OutOfState
                               : S2RuleVerdict.Allowed;

                case S2SessionState.ControlTypeActivated:
                    if (isHandshake)
                        return S2RuleVerdict.OutOfState;
                    if (controlType is not null)
                        return controlType == ActiveControlType
                                   ? S2RuleVerdict.Allowed
                                   : S2RuleVerdict.OutOfState;
                    return S2RuleVerdict.Allowed;

                default:
                    return S2RuleVerdict.OutOfState;

            }

        }

        #endregion

    }

}

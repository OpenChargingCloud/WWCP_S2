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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// Parses any S2 JSON message by dispatching on its "message_type" and maps every
    /// failure to the ReceptionStatus the receiver has to answer with (PLAN.md §3.2):
    /// not JSON or no message_id → INVALID_DATA with the null UUID; unknown message type,
    /// schema or intra-message rule violation → INVALID_MESSAGE with the message_id.
    /// </summary>
    public static class S2MessageParser
    {

        #region Data

        /// <summary>
        /// A delegate parsing one message type.
        /// </summary>
        public delegate Boolean MessageTryParser(JObject                              JSON,
                                                 [NotNullWhen(true)]  out IS2Message?  Message,
                                                 [NotNullWhen(false)] out String?      ErrorResponse,
                                                 S2ParserOptions?                      Options);

        private static readonly Dictionary<String, MessageTryParser> parsers = new (StringComparer.Ordinal) {

            // Common
            [ReceptionStatus.                  MessageTypeName]  = Wrap<ReceptionStatus>                  (ReceptionStatus.                  TryParse),
            [Handshake.                        MessageTypeName]  = Wrap<Handshake>                        (Handshake.                        TryParse),
            [HandshakeResponse.                MessageTypeName]  = Wrap<HandshakeResponse>                (HandshakeResponse.                TryParse),
            [ResourceManagerDetails.           MessageTypeName]  = Wrap<ResourceManagerDetails>           (ResourceManagerDetails.           TryParse),
            [SelectControlType.                MessageTypeName]  = Wrap<SelectControlType>                (SelectControlType.                TryParse),
            [SessionRequest.                   MessageTypeName]  = Wrap<SessionRequest>                   (SessionRequest.                   TryParse),
            [PowerMeasurement.                 MessageTypeName]  = Wrap<PowerMeasurement>                 (PowerMeasurement.                 TryParse),
            [PowerForecast.                    MessageTypeName]  = Wrap<PowerForecast>                    (PowerForecast.                    TryParse),
            [InstructionStatusUpdate.          MessageTypeName]  = Wrap<InstructionStatusUpdate>          (InstructionStatusUpdate.          TryParse),
            [RevokeObject.                     MessageTypeName]  = Wrap<RevokeObject>                     (RevokeObject.                     TryParse),

            // PEBC
            [PEBC_PowerConstraints.            MessageTypeName]  = Wrap<PEBC_PowerConstraints>            (PEBC_PowerConstraints.            TryParse),
            [PEBC_EnergyConstraint.            MessageTypeName]  = Wrap<PEBC_EnergyConstraint>            (PEBC_EnergyConstraint.            TryParse),
            [PEBC_Instruction.                 MessageTypeName]  = Wrap<PEBC_Instruction>                 (PEBC_Instruction.                 TryParse),

            // PPBC
            [PPBC_PowerProfileDefinition.      MessageTypeName]  = Wrap<PPBC_PowerProfileDefinition>      (PPBC_PowerProfileDefinition.      TryParse),
            [PPBC_PowerProfileStatus.          MessageTypeName]  = Wrap<PPBC_PowerProfileStatus>          (PPBC_PowerProfileStatus.          TryParse),
            [PPBC_ScheduleInstruction.         MessageTypeName]  = Wrap<PPBC_ScheduleInstruction>         (PPBC_ScheduleInstruction.         TryParse),
            [PPBC_StartInterruptionInstruction.MessageTypeName]  = Wrap<PPBC_StartInterruptionInstruction>(PPBC_StartInterruptionInstruction.TryParse),
            [PPBC_EndInterruptionInstruction.  MessageTypeName]  = Wrap<PPBC_EndInterruptionInstruction>  (PPBC_EndInterruptionInstruction.  TryParse),

            // OMBC
            [OMBC_SystemDescription.           MessageTypeName]  = Wrap<OMBC_SystemDescription>           (OMBC_SystemDescription.           TryParse),
            [OMBC_Status.                      MessageTypeName]  = Wrap<OMBC_Status>                      (OMBC_Status.                      TryParse),
            [OMBC_Instruction.                 MessageTypeName]  = Wrap<OMBC_Instruction>                 (OMBC_Instruction.                 TryParse),
            [OMBC_TimerStatus.                 MessageTypeName]  = Wrap<OMBC_TimerStatus>                 (OMBC_TimerStatus.                 TryParse),

            // FRBC
            [FRBC_SystemDescription.           MessageTypeName]  = Wrap<FRBC_SystemDescription>           (FRBC_SystemDescription.           TryParse),
            [FRBC_ActuatorStatus.              MessageTypeName]  = Wrap<FRBC_ActuatorStatus>              (FRBC_ActuatorStatus.              TryParse),
            [FRBC_StorageStatus.               MessageTypeName]  = Wrap<FRBC_StorageStatus>               (FRBC_StorageStatus.               TryParse),
            [FRBC_Instruction.                 MessageTypeName]  = Wrap<FRBC_Instruction>                 (FRBC_Instruction.                 TryParse),
            [FRBC_TimerStatus.                 MessageTypeName]  = Wrap<FRBC_TimerStatus>                 (FRBC_TimerStatus.                 TryParse),
            [FRBC_FillLevelTargetProfile.      MessageTypeName]  = Wrap<FRBC_FillLevelTargetProfile>      (FRBC_FillLevelTargetProfile.      TryParse),
            [FRBC_LeakageBehaviour.            MessageTypeName]  = Wrap<FRBC_LeakageBehaviour>            (FRBC_LeakageBehaviour.            TryParse),
            [FRBC_UsageForecast.               MessageTypeName]  = Wrap<FRBC_UsageForecast>               (FRBC_UsageForecast.               TryParse),

            // DDBC
            [DDBC_SystemDescription.           MessageTypeName]  = Wrap<DDBC_SystemDescription>           (DDBC_SystemDescription.           TryParse),
            [DDBC_ActuatorStatus.              MessageTypeName]  = Wrap<DDBC_ActuatorStatus>              (DDBC_ActuatorStatus.              TryParse),
            [DDBC_Instruction.                 MessageTypeName]  = Wrap<DDBC_Instruction>                 (DDBC_Instruction.                 TryParse),
            [DDBC_TimerStatus.                 MessageTypeName]  = Wrap<DDBC_TimerStatus>                 (DDBC_TimerStatus.                 TryParse),
            [DDBC_PresentDemandStatus.         MessageTypeName]  = Wrap<DDBC_PresentDemandStatus>         (DDBC_PresentDemandStatus.         TryParse),
            [DDBC_AverageDemandRateForecast.   MessageTypeName]  = Wrap<DDBC_AverageDemandRateForecast>   (DDBC_AverageDemandRateForecast.   TryParse)

        };

        #endregion

        #region Properties

        /// <summary>
        /// All message types this parser knows.
        /// </summary>
        public static IReadOnlyCollection<String> KnownMessageTypes
            => parsers.Keys;

        #endregion


        #region (private static) Wrap<T>(TryParser)

        private static MessageTryParser Wrap<T>(S2TryParser<T> TryParser)

            where T : class, IS2Message

            => (JObject JSON, [NotNullWhen(true)] out IS2Message? Message, [NotNullWhen(false)] out String? ErrorResponse, S2ParserOptions? Options) => {

                   if (TryParser(JSON, out var message, out ErrorResponse, Options))
                   {
                       Message = message;
                       return true;
                   }

                   Message = null;
                   return false;

               };

        #endregion


        #region TryParse(Text, Options, out Message, out Error)

        /// <summary>
        /// Try to parse the given S2 JSON text as an S2 message.
        /// </summary>
        /// <param name="Text">An S2 JSON text.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="Message">The parsed message.</param>
        /// <param name="Error">The structured parse error.</param>
        public static Boolean TryParse(String                                  Text,
                                       S2ParserOptions?                        Options,
                                       [NotNullWhen(true)]  out IS2Message?    Message,
                                       [NotNullWhen(false)] out S2ParseError?  Error)
        {

            Message = null;

            if (!S2JSONExtensions.TryParseS2JSON(Text, out var json, out var errorResponse))
            {
                Error = S2ParseError.InvalidData(errorResponse);
                return false;
            }

            return TryParse(json, Options, out Message, out Error);

        }

        #endregion

        #region TryParse(JSON, Options, out Message, out Error)

        /// <summary>
        /// Try to parse the given JSON object as an S2 message.
        /// </summary>
        /// <param name="JSON">A JSON object.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="Message">The parsed message.</param>
        /// <param name="Error">The structured parse error.</param>
        public static Boolean TryParse(JObject                                 JSON,
                                       S2ParserOptions?                        Options,
                                       [NotNullWhen(true)]  out IS2Message?    Message,
                                       [NotNullWhen(false)] out S2ParseError?  Error)
        {

            Message = null;

            #region message_id (first, so that every later failure can be reported against it)

            var messageId = Message_Id.Null;

            if (JSON.TryGetValue("message_id", StringComparison.Ordinal, out var messageIdToken) &&
                messageIdToken.Type == JTokenType.String &&
                Message_Id.TryParse(messageIdToken.Value<String>() ?? "", out var parsedMessageId))
            {
                messageId = parsedMessageId;
            }

            #endregion

            #region message_type

            if (!JSON.ParseMandatoryS2String("message_type",
                                             "message type",
                                             out var messageType,
                                             out var errorResponse))
            {

                // A message with an identification but without a message type is a schema
                // failure of that message; without an identification it cannot be correlated.
                Error = messageId == Message_Id.Null
                            ? S2ParseError.InvalidData   (errorResponse)
                            : S2ParseError.InvalidMessage(messageId, errorResponse);

                return false;

            }

            #endregion

            #region message_id is mandatory for every message except ReceptionStatus

            if (messageId == Message_Id.Null &&
                !String.Equals(messageType, ReceptionStatus.MessageTypeName, StringComparison.Ordinal))
            {
                Error = S2ParseError.InvalidData("The message identification 'message_id' is missing or invalid!", messageType);
                return false;
            }

            #endregion

            #region dispatch

            if (!parsers.TryGetValue(messageType, out var parser))
            {
                Error = S2ParseError.InvalidMessage(messageId, $"Unknown message type '{messageType}'!", messageType);
                return false;
            }

            if (!parser(JSON, out Message, out errorResponse, Options))
            {
                Error = S2ParseError.InvalidMessage(messageId, errorResponse, messageType);
                return false;
            }

            #endregion

            Error = null;
            return true;

        }

        #endregion

    }

}

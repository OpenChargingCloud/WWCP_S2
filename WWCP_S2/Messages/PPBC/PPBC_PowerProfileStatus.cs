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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// The Resource Manager reports the status of every power sequence container of a
    /// power profile definition: which power sequence was selected, how far it has
    /// progressed and in which state it is.
    /// </summary>
    public sealed class PPBC_PowerProfileStatus : AS2Message,
                                                  IEquatable<PPBC_PowerProfileStatus>
    {

        #region Data

        /// <summary>
        /// The message type "PPBC.PowerProfileStatus".
        /// </summary>
        public const String MessageTypeName = "PPBC.PowerProfileStatus";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "PPBC.PowerProfileStatus".
        /// </summary>
        public override String                                       MessageType
            => MessageTypeName;

        /// <summary>
        /// The status information for all PPBC.PowerSequenceContainers in the PPBC.PowerProfileDefinition.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PPBC_PowerSequenceContainerStatus>      SequenceContainerStatus    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new PPBC power profile status.
        /// </summary>
        /// <param name="SequenceContainerStatus">The status information for all PPBC.PowerSequenceContainers in the PPBC.PowerProfileDefinition (1..1000 entries).</param>
        /// <param name="MessageId">An optional message identification.</param>
        public PPBC_PowerProfileStatus(IReadOnlyList<PPBC_PowerSequenceContainerStatus>  SequenceContainerStatus,
                                       Message_Id?                                       MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(SequenceContainerStatus);

            if (SequenceContainerStatus.Count < 1)
                throw new ArgumentException("The sequence container status list must contain at least one entry!",
                                            nameof(SequenceContainerStatus));

            if (SequenceContainerStatus.Count > 1000)
                throw new ArgumentException($"The sequence container status list must not contain more than 1000 entries, but {SequenceContainerStatus.Count} were given!",
                                            nameof(SequenceContainerStatus));

            this.SequenceContainerStatus = [.. SequenceContainerStatus];

            unchecked
            {
                hashCode = this.MessageId.              GetHashCode() * 3 ^
                           this.SequenceContainerStatus.CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // PPBC.PowerProfileStatus.schema.json
        //   "title": "PPBC_PowerProfileStatus",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "PPBC.PowerProfileStatus" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "sequence_container_status": {
        //       "type": "array", "minItems": 1, "maxItems": 1000,
        //       "items": { "$ref": "../schemas/PPBC.PowerSequenceContainerStatus.schema.json" },
        //       "description": "Array with status information for all PPBC.PowerSequenceContainers in the PPBC.PowerProfileDefinition."
        //     }
        //   },
        //   "required": ["message_type", "message_id", "sequence_container_status"],
        //   "additionalProperties": false
        //
        // The references to the power profile definition and its containers are cross-message
        // rules and are validated by the session layer.

        #endregion

        #region (static) TryParse(JSON, out PPBC_PowerProfileStatus, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power profile status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerProfileStatus">The parsed PPBC power profile status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerProfileStatus?  PPBC_PowerProfileStatus,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)

            => TryParse(JSON,
                        out PPBC_PowerProfileStatus,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power profile status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerProfileStatus">The parsed PPBC power profile status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerProfileStatus?  PPBC_PowerProfileStatus,
                                       [NotNullWhen(false)] out String?                   ErrorResponse,
                                       S2ParserOptions?                                   Options)

            => TryParse(JSON,
                        out PPBC_PowerProfileStatus,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power profile status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerProfileStatus">The parsed PPBC power profile status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPPBC_PowerProfileStatusParser">A delegate to parse custom PPBC power profile statuses.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerProfileStatus?      PPBC_PowerProfileStatus,
                                       [NotNullWhen(false)] out String?                       ErrorResponse,
                                       S2ParserOptions?                                       Options,
                                       CustomJObjectParserDelegate<PPBC_PowerProfileStatus>?  CustomPPBC_PowerProfileStatusParser)
        {

            try
            {

                PPBC_PowerProfileStatus = null;

                #region message_type, message_id     [mandatory]

                if (!TryParseHeader(JSON,
                                    MessageTypeName,
                                    Options,
                                    out var messageId,
                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region sequence_container_status    [mandatory]

                if (!JSON.ParseMandatoryS2List("sequence_container_status",
                                               "sequence container status",
                                               PPBC_PowerSequenceContainerStatus.TryParse,
                                               Options,
                                               1,
                                               1000,
                                               out IReadOnlyList<PPBC_PowerSequenceContainerStatus>? sequenceContainerStatus,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "message_type",
                                                    "message_id",
                                                    "sequence_container_status"))
                {
                    return false;
                }

                #endregion


                PPBC_PowerProfileStatus = new PPBC_PowerProfileStatus(
                                              sequenceContainerStatus,
                                              messageId
                                          );

                if (CustomPPBC_PowerProfileStatusParser is not null)
                    PPBC_PowerProfileStatus = CustomPPBC_PowerProfileStatusParser(JSON,
                                                                                  PPBC_PowerProfileStatus);

                return true;

            }
            catch (Exception e)
            {
                PPBC_PowerProfileStatus  = null;
                ErrorResponse            = "The given JSON representation of a PPBC power profile status is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPPBC_PowerProfileStatusSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomPPBC_PowerProfileStatusSerializer">A delegate to serialize custom PPBC power profile statuses.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PPBC_PowerProfileStatus>? CustomPPBC_PowerProfileStatusSerializer)
        {

            var json = CreateJSON(
                           new JProperty("sequence_container_status",  new JArray(SequenceContainerStatus.Select(status => status.ToJSON())))
                       );

            return CustomPPBC_PowerProfileStatusSerializer is not null
                       ? CustomPPBC_PowerProfileStatusSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this PPBC power profile status.
        /// </summary>
        public PPBC_PowerProfileStatus Clone()

            => new (
                   [.. SequenceContainerStatus.Select(status => status.Clone())],
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two PPBC power profile statuses for equality.
        /// </summary>
        public static Boolean operator == (PPBC_PowerProfileStatus? PPBC_PowerProfileStatus1, PPBC_PowerProfileStatus? PPBC_PowerProfileStatus2)
        {

            if (ReferenceEquals(PPBC_PowerProfileStatus1, PPBC_PowerProfileStatus2))
                return true;

            if (PPBC_PowerProfileStatus1 is null || PPBC_PowerProfileStatus2 is null)
                return false;

            return PPBC_PowerProfileStatus1.Equals(PPBC_PowerProfileStatus2);

        }

        /// <summary>
        /// Compares two PPBC power profile statuses for inequality.
        /// </summary>
        public static Boolean operator != (PPBC_PowerProfileStatus? PPBC_PowerProfileStatus1, PPBC_PowerProfileStatus? PPBC_PowerProfileStatus2)
            => !(PPBC_PowerProfileStatus1 == PPBC_PowerProfileStatus2);

        #endregion

        #region IEquatable<PPBC_PowerProfileStatus> Members

        /// <summary>
        /// Compares two PPBC power profile statuses for equality.
        /// </summary>
        /// <param name="Object">A PPBC power profile status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PPBC_PowerProfileStatus ppbcPowerProfileStatus && Equals(ppbcPowerProfileStatus);

        /// <summary>
        /// Compares two PPBC power profile statuses for equality.
        /// </summary>
        /// <param name="PPBC_PowerProfileStatus">A PPBC power profile status to compare with.</param>
        public Boolean Equals(PPBC_PowerProfileStatus? PPBC_PowerProfileStatus)

            => PPBC_PowerProfileStatus is not null &&

               MessageId.              Equals       (PPBC_PowerProfileStatus.MessageId) &&
               SequenceContainerStatus.SequenceEqual(PPBC_PowerProfileStatus.SequenceContainerStatus);

        #endregion

        #region (override) GetHashCode()

        private readonly Int32 hashCode;

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => hashCode;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"PPBC.PowerProfileStatus: {SequenceContainerStatus.Count} container status(es) [{MessageId}]";

        #endregion

    }

}

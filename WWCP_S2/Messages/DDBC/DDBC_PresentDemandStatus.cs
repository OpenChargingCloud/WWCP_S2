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
    /// The Resource Manager reports the present demand rate that needs to be satisfied by
    /// the Demand Driven Based Control system.
    /// </summary>
    public sealed class DDBC_PresentDemandStatus : AS2Message,
                                                   IEquatable<DDBC_PresentDemandStatus>
    {

        #region Data

        /// <summary>
        /// The message type "DDBC.PresentDemandStatus".
        /// </summary>
        public const String MessageTypeName = "DDBC.PresentDemandStatus";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "DDBC.PresentDemandStatus".
        /// </summary>
        public override String  MessageType
            => MessageTypeName;

        /// <summary>
        /// The present demand rate that needs to be satisfied by the system.
        /// </summary>
        [Mandatory]
        public NumberRange      PresentDemandRate    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new DDBC present demand status.
        /// </summary>
        /// <param name="PresentDemandRate">The present demand rate that needs to be satisfied by the system.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public DDBC_PresentDemandStatus(NumberRange  PresentDemandRate,
                                        Message_Id?  MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(PresentDemandRate);

            this.PresentDemandRate = PresentDemandRate;

            unchecked
            {
                hashCode = this.MessageId.        GetHashCode() * 3 ^
                           this.PresentDemandRate.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // DDBC.PresentDemandStatus.schema.json
        //   "title": "DDBC_PresentDemandStatus",
        //   "properties": {
        //     "message_type":        { "type": "string", "const": "DDBC.PresentDemandStatus" },
        //     "message_id":          { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "present_demand_rate": { "$ref": "../schemas/NumberRange.schema.json",
        //                              "description": "Present demand rate that needs to be satisfied by the system" }
        //   },
        //   "required": ["message_type", "message_id", "present_demand_rate"],
        //   "additionalProperties": false
        //
        // Semantic rules: the NumberRange itself validates start_of_range <= end_of_range.

        #endregion

        #region (static) TryParse(JSON, out DDBCPresentDemandStatus, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a DDBC present demand status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCPresentDemandStatus">The parsed DDBC present demand status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)]  out DDBC_PresentDemandStatus?  DDBCPresentDemandStatus,
                                       [NotNullWhen(false)] out String?                    ErrorResponse)

            => TryParse(JSON,
                        out DDBCPresentDemandStatus,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC present demand status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCPresentDemandStatus">The parsed DDBC present demand status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)]  out DDBC_PresentDemandStatus?  DDBCPresentDemandStatus,
                                       [NotNullWhen(false)] out String?                    ErrorResponse,
                                       S2ParserOptions?                                    Options)

            => TryParse(JSON,
                        out DDBCPresentDemandStatus,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC present demand status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCPresentDemandStatus">The parsed DDBC present demand status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomDDBCPresentDemandStatusParser">A delegate to parse custom DDBC present demand statuses.</param>
        public static Boolean TryParse(JObject                                                 JSON,
                                       [NotNullWhen(true)]  out DDBC_PresentDemandStatus?      DDBCPresentDemandStatus,
                                       [NotNullWhen(false)] out String?                        ErrorResponse,
                                       S2ParserOptions?                                        Options,
                                       CustomJObjectParserDelegate<DDBC_PresentDemandStatus>?  CustomDDBCPresentDemandStatusParser)
        {

            try
            {

                DDBCPresentDemandStatus = null;

                #region message_type, message_id   [mandatory]

                if (!TryParseHeader(JSON,
                                    MessageTypeName,
                                    Options,
                                    out var messageId,
                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region present_demand_rate        [mandatory]

                if (!JSON.ParseMandatoryS2("present_demand_rate",
                                           "present demand rate",
                                           NumberRange.TryParse,
                                           Options,
                                           out NumberRange? presentDemandRate,
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
                                                    "present_demand_rate"))
                {
                    return false;
                }

                #endregion


                DDBCPresentDemandStatus = new DDBC_PresentDemandStatus(
                                              presentDemandRate,
                                              messageId
                                          );

                if (CustomDDBCPresentDemandStatusParser is not null)
                    DDBCPresentDemandStatus = CustomDDBCPresentDemandStatusParser(JSON,
                                                                                  DDBCPresentDemandStatus);

                return true;

            }
            catch (Exception e)
            {
                DDBCPresentDemandStatus  = null;
                ErrorResponse            = "The given JSON representation of a DDBC present demand status is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDDBCPresentDemandStatusSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomDDBCPresentDemandStatusSerializer">A delegate to serialize custom DDBC present demand statuses.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DDBC_PresentDemandStatus>? CustomDDBCPresentDemandStatusSerializer)
        {

            var json = CreateJSON(
                           new JProperty("present_demand_rate",  PresentDemandRate.ToJSON())
                       );

            return CustomDDBCPresentDemandStatusSerializer is not null
                       ? CustomDDBCPresentDemandStatusSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this DDBC present demand status.
        /// </summary>
        public DDBC_PresentDemandStatus Clone()

            => new (
                   PresentDemandRate.Clone(),
                   MessageId.        Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two DDBC present demand statuses for equality.
        /// </summary>
        public static Boolean operator == (DDBC_PresentDemandStatus? DDBCPresentDemandStatus1, DDBC_PresentDemandStatus? DDBCPresentDemandStatus2)
        {

            if (ReferenceEquals(DDBCPresentDemandStatus1, DDBCPresentDemandStatus2))
                return true;

            if (DDBCPresentDemandStatus1 is null || DDBCPresentDemandStatus2 is null)
                return false;

            return DDBCPresentDemandStatus1.Equals(DDBCPresentDemandStatus2);

        }

        /// <summary>
        /// Compares two DDBC present demand statuses for inequality.
        /// </summary>
        public static Boolean operator != (DDBC_PresentDemandStatus? DDBCPresentDemandStatus1, DDBC_PresentDemandStatus? DDBCPresentDemandStatus2)
            => !(DDBCPresentDemandStatus1 == DDBCPresentDemandStatus2);

        #endregion

        #region IEquatable<DDBC_PresentDemandStatus> Members

        /// <summary>
        /// Compares two DDBC present demand statuses for equality.
        /// </summary>
        /// <param name="Object">A DDBC present demand status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is DDBC_PresentDemandStatus ddbcPresentDemandStatus && Equals(ddbcPresentDemandStatus);

        /// <summary>
        /// Compares two DDBC present demand statuses for equality.
        /// </summary>
        /// <param name="DDBCPresentDemandStatus">A DDBC present demand status to compare with.</param>
        public Boolean Equals(DDBC_PresentDemandStatus? DDBCPresentDemandStatus)

            => DDBCPresentDemandStatus is not null &&

               MessageId.        Equals(DDBCPresentDemandStatus.MessageId) &&
               PresentDemandRate.Equals(DDBCPresentDemandStatus.PresentDemandRate);

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
            => $"DDBC.PresentDemandStatus {PresentDemandRate.StartOfRange}..{PresentDemandRate.EndOfRange} [{MessageId}]";

        #endregion

    }

}

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
    /// The Resource Manager reports the present fill level of its FRBC storage. It should
    /// send an update whenever the fill level changes significantly.
    /// </summary>
    public sealed class FRBC_StorageStatus : AS2Message,
                                             IEquatable<FRBC_StorageStatus>
    {

        #region Data

        /// <summary>
        /// The message type "FRBC.StorageStatus".
        /// </summary>
        public const String MessageTypeName = "FRBC.StorageStatus";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "FRBC.StorageStatus".
        /// </summary>
        public override String  MessageType
            => MessageTypeName;

        /// <summary>
        /// The present fill level of the storage.
        /// </summary>
        [Mandatory]
        public Double           PresentFillLevel    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new FRBC storage status.
        /// </summary>
        /// <param name="PresentFillLevel">The present fill level of the storage.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public FRBC_StorageStatus(Double       PresentFillLevel,
                                  Message_Id?  MessageId   = null)

            : base(MessageId)

        {

            if (!Double.IsFinite(PresentFillLevel))
                throw new ArgumentException("The present fill level must be a finite number!", nameof(PresentFillLevel));

            this.PresentFillLevel = PresentFillLevel;

            unchecked
            {
                hashCode = this.MessageId.       GetHashCode() * 3 ^
                           this.PresentFillLevel.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // FRBC.StorageStatus.schema.json
        //   "title": "FRBC_StorageStatus",
        //   "properties": {
        //     "message_type":       { "type": "string", "const": "FRBC.StorageStatus" },
        //     "message_id":         { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "present_fill_level": { "type": "number", "description": "Present fill level of the Storage" }
        //   },
        //   "required": ["message_type", "message_id", "present_fill_level"],
        //   "additionalProperties": false
        //
        // Semantic rules: none at message level (whether the fill level lies within the
        // FRBC.StorageDescription.fill_level_range is a concern of the session layer).

        #endregion

        #region (static) TryParse(JSON, out FRBCStorageStatus, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an FRBC storage status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCStorageStatus">The parsed FRBC storage status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out FRBC_StorageStatus?  FRBCStorageStatus,
                                       [NotNullWhen(false)] out String?              ErrorResponse)

            => TryParse(JSON,
                        out FRBCStorageStatus,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC storage status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCStorageStatus">The parsed FRBC storage status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out FRBC_StorageStatus?  FRBCStorageStatus,
                                       [NotNullWhen(false)] out String?              ErrorResponse,
                                       S2ParserOptions?                              Options)

            => TryParse(JSON,
                        out FRBCStorageStatus,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC storage status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCStorageStatus">The parsed FRBC storage status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomFRBCStorageStatusParser">A delegate to parse custom FRBC storage statuses.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out FRBC_StorageStatus?      FRBCStorageStatus,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options,
                                       CustomJObjectParserDelegate<FRBC_StorageStatus>?  CustomFRBCStorageStatusParser)
        {

            try
            {

                FRBCStorageStatus = null;

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

                #region present_fill_level         [mandatory]

                if (!JSON.ParseMandatoryS2Number("present_fill_level",
                                                 "present fill level",
                                                 out Double presentFillLevel,
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
                                                    "present_fill_level"))
                {
                    return false;
                }

                #endregion


                FRBCStorageStatus = new FRBC_StorageStatus(
                                        presentFillLevel,
                                        messageId
                                    );

                if (CustomFRBCStorageStatusParser is not null)
                    FRBCStorageStatus = CustomFRBCStorageStatusParser(JSON,
                                                                      FRBCStorageStatus);

                return true;

            }
            catch (Exception e)
            {
                FRBCStorageStatus  = null;
                ErrorResponse      = "The given JSON representation of an FRBC storage status is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomFRBCStorageStatusSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomFRBCStorageStatusSerializer">A delegate to serialize custom FRBC storage statuses.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_StorageStatus>? CustomFRBCStorageStatusSerializer)
        {

            var json = CreateJSON(
                           new JProperty("present_fill_level",  PresentFillLevel)
                       );

            return CustomFRBCStorageStatusSerializer is not null
                       ? CustomFRBCStorageStatusSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this FRBC storage status.
        /// </summary>
        public FRBC_StorageStatus Clone()

            => new (
                   PresentFillLevel,
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two FRBC storage statuses for equality.
        /// </summary>
        public static Boolean operator == (FRBC_StorageStatus? FRBCStorageStatus1, FRBC_StorageStatus? FRBCStorageStatus2)
        {

            if (ReferenceEquals(FRBCStorageStatus1, FRBCStorageStatus2))
                return true;

            if (FRBCStorageStatus1 is null || FRBCStorageStatus2 is null)
                return false;

            return FRBCStorageStatus1.Equals(FRBCStorageStatus2);

        }

        /// <summary>
        /// Compares two FRBC storage statuses for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_StorageStatus? FRBCStorageStatus1, FRBC_StorageStatus? FRBCStorageStatus2)
            => !(FRBCStorageStatus1 == FRBCStorageStatus2);

        #endregion

        #region IEquatable<FRBC_StorageStatus> Members

        /// <summary>
        /// Compares two FRBC storage statuses for equality.
        /// </summary>
        /// <param name="Object">An FRBC storage status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_StorageStatus frbcStorageStatus && Equals(frbcStorageStatus);

        /// <summary>
        /// Compares two FRBC storage statuses for equality.
        /// </summary>
        /// <param name="FRBCStorageStatus">An FRBC storage status to compare with.</param>
        public Boolean Equals(FRBC_StorageStatus? FRBCStorageStatus)

            => FRBCStorageStatus is not null &&

               MessageId.       Equals(FRBCStorageStatus.MessageId) &&
               PresentFillLevel.Equals(FRBCStorageStatus.PresentFillLevel);

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
            => $"FRBC.StorageStatus fill level {PresentFillLevel} [{MessageId}]";

        #endregion

    }

}

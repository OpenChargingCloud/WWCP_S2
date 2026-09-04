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
    /// The Resource Manager describes its Fill Rate Based Control (FRBC) device as an abstract
    /// system of a storage and one or more actuators. The description is usually static, but
    /// may be updated by the Resource Manager when the parameters change; it can be revoked by
    /// its message identification.
    /// </summary>
    public sealed class FRBC_SystemDescription : AS2Message,
                                                 IRevokable,
                                                 IEquatable<FRBC_SystemDescription>
    {

        #region Data

        /// <summary>
        /// The message type "FRBC.SystemDescription".
        /// </summary>
        public const String MessageTypeName = "FRBC.SystemDescription";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "FRBC.SystemDescription".
        /// </summary>
        public override String                           MessageType
            => MessageTypeName;

        /// <summary>
        /// The revokable object type "FRBC.SystemDescription".
        /// </summary>
        public RevokableObject                           RevokableObjectType
            => RevokableObject.FRBC_SystemDescription;

        /// <summary>
        /// The identification a RevokeObject uses to refer to this message: its message identification,
        /// as a system description carries no identification of its own.
        /// </summary>
        public S2Object_Id                               RevokableObjectId
            => S2Object_Id.From(MessageId);

        /// <summary>
        /// The moment this FRBC.SystemDescription starts to be valid. If the system description
        /// is immediately valid, the timestamp should be now or in the past.
        /// </summary>
        [Mandatory]
        public DateTimeOffset                            ValidFrom    { get; }

        /// <summary>
        /// The details of all actuators.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<FRBC_ActuatorDescription>   Actuators    { get; }

        /// <summary>
        /// The details of the storage.
        /// </summary>
        [Mandatory]
        public FRBC_StorageDescription                   Storage      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new FRBC system description.
        /// </summary>
        /// <param name="ValidFrom">The moment this system description starts to be valid.</param>
        /// <param name="Actuators">The details of all actuators (1..10 entries with unique identifications).</param>
        /// <param name="Storage">The details of the storage.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public FRBC_SystemDescription(DateTimeOffset                           ValidFrom,
                                      IReadOnlyList<FRBC_ActuatorDescription>  Actuators,
                                      FRBC_StorageDescription                  Storage,
                                      Message_Id?                              MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(Actuators);
            ArgumentNullException.ThrowIfNull(Storage);

            #region Actuators: 1..10, unique ids

            if (Actuators.Count < 1)
                throw new ArgumentException("An FRBC system description must contain at least one actuator!",
                                            nameof(Actuators));

            if (Actuators.Count > 10)
                throw new ArgumentException($"An FRBC system description must not contain more than 10 actuators, but {Actuators.Count} were given!",
                                            nameof(Actuators));

            var duplicateActuatorId = Actuators.
                                          GroupBy(actuator => actuator.Id).
                                          Select (group    => (group.Key, Count: group.Count())).
                                          FirstOrDefault(group => group.Count > 1);

            if (duplicateActuatorId.Count > 1)
                throw new ArgumentException($"The actuator identifications of an FRBC system description must be unique, but '{duplicateActuatorId.Key}' occurs {duplicateActuatorId.Count} times!",
                                            nameof(Actuators));

            #endregion

            this.ValidFrom  = ValidFrom;
            this.Actuators  = [.. Actuators];
            this.Storage    = Storage;

            unchecked
            {
                hashCode = this.MessageId.GetHashCode()  * 7 ^
                           this.ValidFrom.GetHashCode()  * 5 ^
                           this.Actuators.CalcHashCode() * 3 ^
                           this.Storage.  GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // FRBC.SystemDescription.schema.json
        //   "title": "FRBC_SystemDescription",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "FRBC.SystemDescription" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "valid_from":   { "type": "string", "format": "date-time",
        //                       "description": "Moment this FRBC.SystemDescription starts to be valid. If the system description
        //                                       is immediately valid, the DateTimeStamp should be now or in the past." },
        //     "actuators":    { "type": "array", "minItems": 1, "maxItems": 10,
        //                       "items": { "$ref": "../schemas/FRBC.ActuatorDescription.schema.json" },
        //                       "description": "Details of all Actuators." },
        //     "storage":      { "$ref": "../schemas/FRBC.StorageDescription.schema.json",
        //                       "description": "Details of the storage." }
        //   },
        //   "required": ["message_type", "message_id", "valid_from", "actuators", "storage"],
        //   "additionalProperties": false
        //
        // Semantic rules (CONVENTIONS.md §8):
        //   - The actuator identifications are unique within the system description.
        //   - The message is revokable (RevokableObjects: "FRBC.SystemDescription") by its message_id.

        #endregion

        #region (static) TryParse(JSON, out FRBCSystemDescription, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an FRBC system description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCSystemDescription">The parsed FRBC system description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out FRBC_SystemDescription?  FRBCSystemDescription,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)

            => TryParse(JSON,
                        out FRBCSystemDescription,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC system description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCSystemDescription">The parsed FRBC system description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out FRBC_SystemDescription?  FRBCSystemDescription,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options)

            => TryParse(JSON,
                        out FRBCSystemDescription,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC system description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCSystemDescription">The parsed FRBC system description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomFRBCSystemDescriptionParser">A delegate to parse custom FRBC system descriptions.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out FRBC_SystemDescription?      FRBCSystemDescription,
                                       [NotNullWhen(false)] out String?                      ErrorResponse,
                                       S2ParserOptions?                                      Options,
                                       CustomJObjectParserDelegate<FRBC_SystemDescription>?  CustomFRBCSystemDescriptionParser)
        {

            try
            {

                FRBCSystemDescription = null;

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

                #region valid_from                 [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("valid_from",
                                                    "valid from timestamp",
                                                    Options,
                                                    out DateTimeOffset validFrom,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region actuators                  [mandatory]

                if (!JSON.ParseMandatoryS2List("actuators",
                                               "actuators",
                                               FRBC_ActuatorDescription.TryParse,
                                               Options,
                                               1,
                                               10,
                                               out IReadOnlyList<FRBC_ActuatorDescription>? actuators,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region storage                    [mandatory]

                if (!JSON.ParseMandatoryS2("storage",
                                           "storage description",
                                           FRBC_StorageDescription.TryParse,
                                           Options,
                                           out FRBC_StorageDescription? storage,
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
                                                    "valid_from",
                                                    "actuators",
                                                    "storage"))
                {
                    return false;
                }

                #endregion


                FRBCSystemDescription = new FRBC_SystemDescription(
                                            validFrom,
                                            actuators,
                                            storage,
                                            messageId
                                        );

                if (CustomFRBCSystemDescriptionParser is not null)
                    FRBCSystemDescription = CustomFRBCSystemDescriptionParser(JSON,
                                                                              FRBCSystemDescription);

                return true;

            }
            catch (Exception e)
            {
                FRBCSystemDescription  = null;
                ErrorResponse          = "The given JSON representation of an FRBC system description is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomFRBCSystemDescriptionSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomFRBCSystemDescriptionSerializer">A delegate to serialize custom FRBC system descriptions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_SystemDescription>? CustomFRBCSystemDescriptionSerializer)
        {

            var json = CreateJSON(
                           new JProperty("valid_from",  ValidFrom.ToS2Timestamp()),
                           new JProperty("actuators",   new JArray(Actuators.Select(actuator => actuator.ToJSON()))),
                           new JProperty("storage",     Storage.ToJSON())
                       );

            return CustomFRBCSystemDescriptionSerializer is not null
                       ? CustomFRBCSystemDescriptionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this FRBC system description.
        /// </summary>
        public FRBC_SystemDescription Clone()

            => new (
                   ValidFrom,
                   [.. Actuators.Select(actuator => actuator.Clone())],
                   Storage.  Clone(),
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two FRBC system descriptions for equality.
        /// </summary>
        public static Boolean operator == (FRBC_SystemDescription? FRBCSystemDescription1, FRBC_SystemDescription? FRBCSystemDescription2)
        {

            if (ReferenceEquals(FRBCSystemDescription1, FRBCSystemDescription2))
                return true;

            if (FRBCSystemDescription1 is null || FRBCSystemDescription2 is null)
                return false;

            return FRBCSystemDescription1.Equals(FRBCSystemDescription2);

        }

        /// <summary>
        /// Compares two FRBC system descriptions for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_SystemDescription? FRBCSystemDescription1, FRBC_SystemDescription? FRBCSystemDescription2)
            => !(FRBCSystemDescription1 == FRBCSystemDescription2);

        #endregion

        #region IEquatable<FRBC_SystemDescription> Members

        /// <summary>
        /// Compares two FRBC system descriptions for equality.
        /// </summary>
        /// <param name="Object">An FRBC system description to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_SystemDescription frbcSystemDescription && Equals(frbcSystemDescription);

        /// <summary>
        /// Compares two FRBC system descriptions for equality.
        /// </summary>
        /// <param name="FRBCSystemDescription">An FRBC system description to compare with.</param>
        public Boolean Equals(FRBC_SystemDescription? FRBCSystemDescription)

            => FRBCSystemDescription is not null &&

               MessageId.Equals       (FRBCSystemDescription.MessageId) &&
               ValidFrom.Equals       (FRBCSystemDescription.ValidFrom) &&
               Actuators.SequenceEqual(FRBCSystemDescription.Actuators) &&
               Storage.  Equals       (FRBCSystemDescription.Storage);

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

            => $"FRBC.SystemDescription valid from {ValidFrom.ToS2Timestamp()} with {Actuators.Count} actuator(s) and storage {Storage} [{MessageId}]";

        #endregion

    }

}

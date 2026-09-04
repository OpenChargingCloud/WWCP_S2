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
    /// The sender revokes a previously sent revokable object (an instruction, a
    /// constraint, a power profile definition or a system description), identified
    /// by its object type and object identification.
    /// </summary>
    public sealed class RevokeObject : AS2Message,
                                       IEquatable<RevokeObject>
    {

        #region Data

        /// <summary>
        /// The message type "RevokeObject".
        /// </summary>
        public const String MessageTypeName = "RevokeObject";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "RevokeObject".
        /// </summary>
        public override String  MessageType
            => MessageTypeName;

        /// <summary>
        /// The type of object that needs to be revoked.
        /// </summary>
        [Mandatory]
        public RevokableObject  ObjectType    { get; }

        /// <summary>
        /// The identification of the object that needs to be revoked: the "id" of an
        /// instruction, constraint or power profile definition, or the "message_id"
        /// of an OMBC/FRBC/DDBC system description.
        /// </summary>
        [Mandatory]
        public S2Object_Id      ObjectId      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new revoke object message.
        /// </summary>
        /// <param name="ObjectType">The type of object that needs to be revoked.</param>
        /// <param name="ObjectId">The identification of the object that needs to be revoked.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public RevokeObject(RevokableObject  ObjectType,
                            S2Object_Id      ObjectId,
                            Message_Id?      MessageId   = null)

            : base(MessageId)

        {

            if (ObjectType.IsNullOrEmpty)
                throw new ArgumentException("The revokable object type must not be null or empty!",
                                            nameof(ObjectType));

            if (ObjectId.IsNullOrEmpty)
                throw new ArgumentException("The object identification must not be null or empty!",
                                            nameof(ObjectId));

            this.ObjectType  = ObjectType;
            this.ObjectId    = ObjectId;

            unchecked
            {
                hashCode = this.MessageId. GetHashCode() * 5 ^
                           this.ObjectType.GetHashCode() * 3 ^
                           this.ObjectId.  GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // RevokeObject.schema.json
        //   "title": "RevokeObject",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "RevokeObject" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "object_type":  { "$ref": "../schemas/RevokableObjects.schema.json",
        //                       "description": "The type of object that needs to be revoked" },
        //     "object_id":    { "$ref": "../schemas/ID.schema.json",
        //                       "description": "The ID of object that needs to be revoked" }
        //   },
        //   "required": ["message_type", "message_id", "object_type", "object_id"],
        //   "additionalProperties": false

        #endregion

        #region (static) For(Revokable, MessageId = null)

        /// <summary>
        /// Create a revoke object message for the given revokable message.
        /// </summary>
        /// <param name="Revokable">The revokable message to be revoked.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public static RevokeObject For(IRevokable   Revokable,
                                       Message_Id?  MessageId   = null)
        {

            ArgumentNullException.ThrowIfNull(Revokable);

            return new (
                       Revokable.RevokableObjectType,
                       Revokable.RevokableObjectId,
                       MessageId
                   );

        }

        #endregion

        #region (static) TryParse(JSON, out RevokeObject, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a revoke object message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RevokeObject">The parsed revoke object message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                 JSON,
                                       [NotNullWhen(true)]  out RevokeObject?  RevokeObject,
                                       [NotNullWhen(false)] out String?        ErrorResponse)

            => TryParse(JSON,
                        out RevokeObject,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a revoke object message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RevokeObject">The parsed revoke object message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                 JSON,
                                       [NotNullWhen(true)]  out RevokeObject?  RevokeObject,
                                       [NotNullWhen(false)] out String?        ErrorResponse,
                                       S2ParserOptions?                        Options)

            => TryParse(JSON,
                        out RevokeObject,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a revoke object message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RevokeObject">The parsed revoke object message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomRevokeObjectParser">A delegate to parse custom revoke object messages.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out RevokeObject?      RevokeObject,
                                       [NotNullWhen(false)] out String?            ErrorResponse,
                                       S2ParserOptions?                            Options,
                                       CustomJObjectParserDelegate<RevokeObject>?  CustomRevokeObjectParser)
        {

            try
            {

                RevokeObject = null;

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

                #region object_type                [mandatory]

                if (!JSON.ParseMandatoryS2Enum("object_type",
                                               "revokable object type",
                                               RevokableObject.TryParse,
                                               Options,
                                               out RevokableObject objectType,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region object_id                  [mandatory]

                if (!JSON.ParseMandatoryS2Id("object_id",
                                             "object identification",
                                             S2Object_Id.TryParse,
                                             Options,
                                             out S2Object_Id objectId,
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
                                                    "object_type",
                                                    "object_id"))
                {
                    return false;
                }

                #endregion


                RevokeObject = new RevokeObject(
                                   objectType,
                                   objectId,
                                   messageId
                               );

                if (CustomRevokeObjectParser is not null)
                    RevokeObject = CustomRevokeObjectParser(JSON,
                                                            RevokeObject);

                return true;

            }
            catch (Exception e)
            {
                RevokeObject   = null;
                ErrorResponse  = "The given JSON representation of a revoke object message is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomRevokeObjectSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomRevokeObjectSerializer">A delegate to serialize custom revoke object messages.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<RevokeObject>? CustomRevokeObjectSerializer)
        {

            var json = CreateJSON(
                           new JProperty("object_type",  ObjectType.ToString()),
                           new JProperty("object_id",    ObjectId.  ToString())
                       );

            return CustomRevokeObjectSerializer is not null
                       ? CustomRevokeObjectSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this revoke object message.
        /// </summary>
        public RevokeObject Clone()

            => new (
                   ObjectType.Clone(),
                   ObjectId.  Clone(),
                   MessageId. Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two revoke object messages for equality.
        /// </summary>
        public static Boolean operator == (RevokeObject? RevokeObject1, RevokeObject? RevokeObject2)
        {

            if (ReferenceEquals(RevokeObject1, RevokeObject2))
                return true;

            if (RevokeObject1 is null || RevokeObject2 is null)
                return false;

            return RevokeObject1.Equals(RevokeObject2);

        }

        /// <summary>
        /// Compares two revoke object messages for inequality.
        /// </summary>
        public static Boolean operator != (RevokeObject? RevokeObject1, RevokeObject? RevokeObject2)
            => !(RevokeObject1 == RevokeObject2);

        #endregion

        #region IEquatable<RevokeObject> Members

        /// <summary>
        /// Compares two revoke object messages for equality.
        /// </summary>
        /// <param name="Object">A revoke object message to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is RevokeObject revokeObject && Equals(revokeObject);

        /// <summary>
        /// Compares two revoke object messages for equality.
        /// </summary>
        /// <param name="RevokeObject">A revoke object message to compare with.</param>
        public Boolean Equals(RevokeObject? RevokeObject)

            => RevokeObject is not null &&

               MessageId. Equals(RevokeObject.MessageId)  &&
               ObjectType.Equals(RevokeObject.ObjectType) &&
               ObjectId.  Equals(RevokeObject.ObjectId);

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
            => $"RevokeObject {ObjectType} '{ObjectId}' [{MessageId}]";

        #endregion

    }

}

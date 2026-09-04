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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// Extension methods for message identifications.
    /// </summary>
    public static class MessageIdExtensions
    {

        /// <summary>
        /// Indicates whether this message identification is null or empty.
        /// </summary>
        /// <param name="MessageId">A message identification.</param>
        public static Boolean IsNullOrEmpty(this Message_Id? MessageId)
            => !MessageId.HasValue || MessageId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this message identification is NOT null or empty.
        /// </summary>
        /// <param name="MessageId">A message identification.</param>
        public static Boolean IsNotNullOrEmpty(this Message_Id? MessageId)
            => MessageId.HasValue && MessageId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of an S2 message ("message_id"). Every S2 message except
    /// ReceptionStatus carries one; a ReceptionStatus refers to it as "subject_message_id".
    /// </summary>
    public readonly struct Message_Id : IId,
                                        IEquatable<Message_Id>,
                                        IComparable<Message_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the message identification.
        /// </summary>
        public String   Value               { get; }

        /// <summary>
        /// Indicates whether this identification is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => Value.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this identification is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => Value.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the message identification.
        /// </summary>
        public UInt64   Length
            => (UInt64) (Value?.Length ?? 0);

        /// <summary>
        /// Whether this identification is a UUID (the form s2-python requires).
        /// </summary>
        public Boolean  IsUUID
            => S2_Id.IsUUID(Value);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new message identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a message identification.</param>
        private Message_Id(String Text)
        {
            this.Value = Text;
        }

        #endregion


        #region Documentation

        // ID.schema.json
        //   "title":       "ID",
        //   "type":        "string",
        //   "pattern":     "[a-zA-Z0-9\\-_:]{2,64}",
        //   "description": "An identifier expressed as a UUID"
        //
        // Handshake.schema.json (and every other message except ReceptionStatus)
        //   "message_id": { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" }

        #endregion

        #region (static) Null

        /// <summary>
        /// The null UUID "00000000-0000-0000-0000-000000000000", used as subject_message_id
        /// of a ReceptionStatus that cannot be correlated to a message (INVALID_DATA).
        /// </summary>
        public static Message_Id Null
            => new (S2_Id.NullUUID);

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) message identification.
        /// </summary>
        public static Message_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a message identification.
        /// </summary>
        /// <param name="Text">A text representation of a message identification.</param>
        public static Message_Id Parse(String Text)
        {

            if (TryParse(Text, out var messageId))
                return messageId;

            throw new ArgumentException($"Invalid text representation of a message identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a message identification.
        /// </summary>
        /// <param name="Text">A text representation of a message identification.</param>
        public static Message_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var messageId))
                return messageId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out MessageId)

        /// <summary>
        /// Try to parse the given text as a message identification.
        /// </summary>
        /// <param name="Text">A text representation of a message identification.</param>
        /// <param name="MessageId">The parsed message identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out Message_Id    MessageId)
        {

            if (S2_Id.IsValid(Text))
            {
                MessageId = new Message_Id(Text);
                return true;
            }

            MessageId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this message identification.
        /// </summary>
        public Message_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two message identifications for equality.
        /// </summary>
        public static Boolean operator == (Message_Id MessageId1, Message_Id MessageId2)
            => MessageId1.Equals(MessageId2);

        /// <summary>
        /// Compares two message identifications for inequality.
        /// </summary>
        public static Boolean operator != (Message_Id MessageId1, Message_Id MessageId2)
            => !MessageId1.Equals(MessageId2);

        /// <summary>
        /// Compares two message identifications.
        /// </summary>
        public static Boolean operator <  (Message_Id MessageId1, Message_Id MessageId2)
            => MessageId1.CompareTo(MessageId2) < 0;

        /// <summary>
        /// Compares two message identifications.
        /// </summary>
        public static Boolean operator <= (Message_Id MessageId1, Message_Id MessageId2)
            => MessageId1.CompareTo(MessageId2) <= 0;

        /// <summary>
        /// Compares two message identifications.
        /// </summary>
        public static Boolean operator >  (Message_Id MessageId1, Message_Id MessageId2)
            => MessageId1.CompareTo(MessageId2) > 0;

        /// <summary>
        /// Compares two message identifications.
        /// </summary>
        public static Boolean operator >= (Message_Id MessageId1, Message_Id MessageId2)
            => MessageId1.CompareTo(MessageId2) >= 0;

        #endregion

        #region IComparable<Message_Id> Members

        /// <summary>
        /// Compares two message identifications.
        /// </summary>
        /// <param name="Object">A message identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Message_Id messageId
                   ? CompareTo(messageId)
                   : throw new ArgumentException("The given object is not a message identification!", nameof(Object));

        /// <summary>
        /// Compares two message identifications.
        /// </summary>
        /// <param name="MessageId">A message identification to compare with.</param>
        public Int32 CompareTo(Message_Id MessageId)
            => String.Compare(Value, MessageId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<Message_Id> Members

        /// <summary>
        /// Compares two message identifications for equality.
        /// </summary>
        /// <param name="Object">A message identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Message_Id messageId && Equals(messageId);

        /// <summary>
        /// Compares two message identifications for equality.
        /// </summary>
        /// <param name="MessageId">A message identification to compare with.</param>
        public Boolean Equals(Message_Id MessageId)
            => String.Equals(Value, MessageId.Value, StringComparison.Ordinal);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => Value?.GetHashCode(StringComparison.Ordinal) ?? 0;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => Value ?? "";

        #endregion

    }

}

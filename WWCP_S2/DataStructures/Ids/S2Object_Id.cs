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
    /// Extension methods for object identifications.
    /// </summary>
    public static class S2ObjectIdExtensions
    {

        /// <summary>
        /// Indicates whether this object identification is null or empty.
        /// </summary>
        /// <param name="S2ObjectId">A object identification.</param>
        public static Boolean IsNullOrEmpty(this S2Object_Id? S2ObjectId)
            => !S2ObjectId.HasValue || S2ObjectId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this object identification is NOT null or empty.
        /// </summary>
        /// <param name="S2ObjectId">A object identification.</param>
        public static Boolean IsNotNullOrEmpty(this S2Object_Id? S2ObjectId)
            => S2ObjectId.HasValue && S2ObjectId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The generic identification of a revokable S2 object as carried by RevokeObject.object_id: the "id" of an instruction, constraint, power envelope or power profile definition, or the "message_id" of an OMBC/FRBC/DDBC system description. Typed identifications convert to it via their text value.
    /// </summary>
    public readonly struct S2Object_Id : IId,
                                        IEquatable<S2Object_Id>,
                                        IComparable<S2Object_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the object identification.
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
        /// The length of the object identification.
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
        /// Create a new object identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a object identification.</param>
        private S2Object_Id(String Text)
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
        // RevokeObject.schema.json
        //   "object_type": { "$ref": "../schemas/RevokableObjects.schema.json", "description": "The type of object that needs to be revoked" },
        //   "object_id":   { "$ref": "../schemas/ID.schema.json",               "description": "The ID of object that needs to be revoked" }

        #endregion

        #region (static) From(Id)

        /// <summary>
        /// Create an object identification from the text value of any typed S2 identification.
        /// </summary>
        /// <param name="Id">A typed identification, e.g. an Instruction_Id or a Message_Id.</param>
        public static S2Object_Id From(org.GraphDefined.Vanaheimr.Illias.IId Id)
            => new (Id.ToString());

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) object identification.
        /// </summary>
        public static S2Object_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a object identification.
        /// </summary>
        /// <param name="Text">A text representation of a object identification.</param>
        public static S2Object_Id Parse(String Text)
        {

            if (TryParse(Text, out var s2ObjectId))
                return s2ObjectId;

            throw new ArgumentException($"Invalid text representation of a object identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a object identification.
        /// </summary>
        /// <param name="Text">A text representation of a object identification.</param>
        public static S2Object_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var s2ObjectId))
                return s2ObjectId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out S2ObjectId)

        /// <summary>
        /// Try to parse the given text as a object identification.
        /// </summary>
        /// <param name="Text">A text representation of a object identification.</param>
        /// <param name="S2ObjectId">The parsed object identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out S2Object_Id    S2ObjectId)
        {

            if (S2_Id.IsValid(Text))
            {
                S2ObjectId = new S2Object_Id(Text);
                return true;
            }

            S2ObjectId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this object identification.
        /// </summary>
        public S2Object_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two object identifications for equality.
        /// </summary>
        public static Boolean operator == (S2Object_Id S2ObjectId1, S2Object_Id S2ObjectId2)
            => S2ObjectId1.Equals(S2ObjectId2);

        /// <summary>
        /// Compares two object identifications for inequality.
        /// </summary>
        public static Boolean operator != (S2Object_Id S2ObjectId1, S2Object_Id S2ObjectId2)
            => !S2ObjectId1.Equals(S2ObjectId2);

        /// <summary>
        /// Compares two object identifications.
        /// </summary>
        public static Boolean operator <  (S2Object_Id S2ObjectId1, S2Object_Id S2ObjectId2)
            => S2ObjectId1.CompareTo(S2ObjectId2) < 0;

        /// <summary>
        /// Compares two object identifications.
        /// </summary>
        public static Boolean operator <= (S2Object_Id S2ObjectId1, S2Object_Id S2ObjectId2)
            => S2ObjectId1.CompareTo(S2ObjectId2) <= 0;

        /// <summary>
        /// Compares two object identifications.
        /// </summary>
        public static Boolean operator >  (S2Object_Id S2ObjectId1, S2Object_Id S2ObjectId2)
            => S2ObjectId1.CompareTo(S2ObjectId2) > 0;

        /// <summary>
        /// Compares two object identifications.
        /// </summary>
        public static Boolean operator >= (S2Object_Id S2ObjectId1, S2Object_Id S2ObjectId2)
            => S2ObjectId1.CompareTo(S2ObjectId2) >= 0;

        #endregion

        #region IComparable<S2Object_Id> Members

        /// <summary>
        /// Compares two object identifications.
        /// </summary>
        /// <param name="Object">A object identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is S2Object_Id s2ObjectId
                   ? CompareTo(s2ObjectId)
                   : throw new ArgumentException("The given object is not a object identification!", nameof(Object));

        /// <summary>
        /// Compares two object identifications.
        /// </summary>
        /// <param name="S2ObjectId">A object identification to compare with.</param>
        public Int32 CompareTo(S2Object_Id S2ObjectId)
            => String.Compare(Value, S2ObjectId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<S2Object_Id> Members

        /// <summary>
        /// Compares two object identifications for equality.
        /// </summary>
        /// <param name="Object">A object identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is S2Object_Id s2ObjectId && Equals(s2ObjectId);

        /// <summary>
        /// Compares two object identifications for equality.
        /// </summary>
        /// <param name="S2ObjectId">A object identification to compare with.</param>
        public Boolean Equals(S2Object_Id S2ObjectId)
            => String.Equals(Value, S2ObjectId.Value, StringComparison.Ordinal);

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

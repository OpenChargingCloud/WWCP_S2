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
    /// Extension methods for operation mode identifications.
    /// </summary>
    public static class OperationModeIdExtensions
    {

        /// <summary>
        /// Indicates whether this operation mode identification is null or empty.
        /// </summary>
        /// <param name="OperationModeId">A operation mode identification.</param>
        public static Boolean IsNullOrEmpty(this OperationMode_Id? OperationModeId)
            => !OperationModeId.HasValue || OperationModeId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this operation mode identification is NOT null or empty.
        /// </summary>
        /// <param name="OperationModeId">A operation mode identification.</param>
        public static Boolean IsNotNullOrEmpty(this OperationMode_Id? OperationModeId)
            => OperationModeId.HasValue && OperationModeId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The identification of an operation mode (OMBC, FRBC and DDBC). Unique per actuator description (FRBC/DDBC) or per Resource Manager (OMBC), for at least the duration of the session.
    /// </summary>
    public readonly struct OperationMode_Id : IId,
                                        IEquatable<OperationMode_Id>,
                                        IComparable<OperationMode_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the operation mode identification.
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
        /// The length of the operation mode identification.
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
        /// Create a new operation mode identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a operation mode identification.</param>
        private OperationMode_Id(String Text)
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
        // OMBC.OperationMode.schema.json / FRBC.OperationMode.schema.json / DDBC.OperationMode.schema.json
        //   "id": { "$ref": "../schemas/ID.schema.json", "description": "ID of the OperationMode. Must be unique in the scope of ..." }
        //   Note: DDBC.OperationMode spells the property "Id" (capital I) on the wire.

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) operation mode identification.
        /// </summary>
        public static OperationMode_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a operation mode identification.
        /// </summary>
        /// <param name="Text">A text representation of a operation mode identification.</param>
        public static OperationMode_Id Parse(String Text)
        {

            if (TryParse(Text, out var operationModeId))
                return operationModeId;

            throw new ArgumentException($"Invalid text representation of a operation mode identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a operation mode identification.
        /// </summary>
        /// <param name="Text">A text representation of a operation mode identification.</param>
        public static OperationMode_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var operationModeId))
                return operationModeId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out OperationModeId)

        /// <summary>
        /// Try to parse the given text as a operation mode identification.
        /// </summary>
        /// <param name="Text">A text representation of a operation mode identification.</param>
        /// <param name="OperationModeId">The parsed operation mode identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out OperationMode_Id    OperationModeId)
        {

            if (S2_Id.IsValid(Text))
            {
                OperationModeId = new OperationMode_Id(Text);
                return true;
            }

            OperationModeId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this operation mode identification.
        /// </summary>
        public OperationMode_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two operation mode identifications for equality.
        /// </summary>
        public static Boolean operator == (OperationMode_Id OperationModeId1, OperationMode_Id OperationModeId2)
            => OperationModeId1.Equals(OperationModeId2);

        /// <summary>
        /// Compares two operation mode identifications for inequality.
        /// </summary>
        public static Boolean operator != (OperationMode_Id OperationModeId1, OperationMode_Id OperationModeId2)
            => !OperationModeId1.Equals(OperationModeId2);

        /// <summary>
        /// Compares two operation mode identifications.
        /// </summary>
        public static Boolean operator <  (OperationMode_Id OperationModeId1, OperationMode_Id OperationModeId2)
            => OperationModeId1.CompareTo(OperationModeId2) < 0;

        /// <summary>
        /// Compares two operation mode identifications.
        /// </summary>
        public static Boolean operator <= (OperationMode_Id OperationModeId1, OperationMode_Id OperationModeId2)
            => OperationModeId1.CompareTo(OperationModeId2) <= 0;

        /// <summary>
        /// Compares two operation mode identifications.
        /// </summary>
        public static Boolean operator >  (OperationMode_Id OperationModeId1, OperationMode_Id OperationModeId2)
            => OperationModeId1.CompareTo(OperationModeId2) > 0;

        /// <summary>
        /// Compares two operation mode identifications.
        /// </summary>
        public static Boolean operator >= (OperationMode_Id OperationModeId1, OperationMode_Id OperationModeId2)
            => OperationModeId1.CompareTo(OperationModeId2) >= 0;

        #endregion

        #region IComparable<OperationMode_Id> Members

        /// <summary>
        /// Compares two operation mode identifications.
        /// </summary>
        /// <param name="Object">A operation mode identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is OperationMode_Id operationModeId
                   ? CompareTo(operationModeId)
                   : throw new ArgumentException("The given object is not a operation mode identification!", nameof(Object));

        /// <summary>
        /// Compares two operation mode identifications.
        /// </summary>
        /// <param name="OperationModeId">A operation mode identification to compare with.</param>
        public Int32 CompareTo(OperationMode_Id OperationModeId)
            => String.Compare(Value, OperationModeId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<OperationMode_Id> Members

        /// <summary>
        /// Compares two operation mode identifications for equality.
        /// </summary>
        /// <param name="Object">A operation mode identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is OperationMode_Id operationModeId && Equals(operationModeId);

        /// <summary>
        /// Compares two operation mode identifications for equality.
        /// </summary>
        /// <param name="OperationModeId">A operation mode identification to compare with.</param>
        public Boolean Equals(OperationMode_Id OperationModeId)
            => String.Equals(Value, OperationModeId.Value, StringComparison.Ordinal);

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

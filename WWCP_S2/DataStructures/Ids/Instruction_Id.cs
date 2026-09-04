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
    /// Extension methods for instruction identifications.
    /// </summary>
    public static class InstructionIdExtensions
    {

        /// <summary>
        /// Indicates whether this instruction identification is null or empty.
        /// </summary>
        /// <param name="InstructionId">A instruction identification.</param>
        public static Boolean IsNullOrEmpty(this Instruction_Id? InstructionId)
            => !InstructionId.HasValue || InstructionId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this instruction identification is NOT null or empty.
        /// </summary>
        /// <param name="InstructionId">A instruction identification.</param>
        public static Boolean IsNotNullOrEmpty(this Instruction_Id? InstructionId)
            => InstructionId.HasValue && InstructionId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The identification of an instruction (PEBC, PPBC, OMBC, FRBC and DDBC instructions). Must be unique in the scope of the Resource Manager, for at least the duration of the session between Resource Manager and CEM.
    /// </summary>
    public readonly struct Instruction_Id : IId,
                                        IEquatable<Instruction_Id>,
                                        IComparable<Instruction_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the instruction identification.
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
        /// The length of the instruction identification.
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
        /// Create a new instruction identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a instruction identification.</param>
        private Instruction_Id(String Text)
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
        // *.Instruction.schema.json
        //   "id": { "$ref": "../schemas/ID.schema.json",
        //           "description": "Identifier of this Instruction. Must be unique in the scope of the Resource Manager, for at least the duration of the session between Resource Manager and CEM." }
        // InstructionStatusUpdate.schema.json
        //   "instruction_id": { "$ref": "../schemas/ID.schema.json", "description": "ID of the instruction being updated" }

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) instruction identification.
        /// </summary>
        public static Instruction_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a instruction identification.
        /// </summary>
        /// <param name="Text">A text representation of a instruction identification.</param>
        public static Instruction_Id Parse(String Text)
        {

            if (TryParse(Text, out var instructionId))
                return instructionId;

            throw new ArgumentException($"Invalid text representation of a instruction identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a instruction identification.
        /// </summary>
        /// <param name="Text">A text representation of a instruction identification.</param>
        public static Instruction_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var instructionId))
                return instructionId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out InstructionId)

        /// <summary>
        /// Try to parse the given text as a instruction identification.
        /// </summary>
        /// <param name="Text">A text representation of a instruction identification.</param>
        /// <param name="InstructionId">The parsed instruction identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out Instruction_Id    InstructionId)
        {

            if (S2_Id.IsValid(Text))
            {
                InstructionId = new Instruction_Id(Text);
                return true;
            }

            InstructionId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this instruction identification.
        /// </summary>
        public Instruction_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two instruction identifications for equality.
        /// </summary>
        public static Boolean operator == (Instruction_Id InstructionId1, Instruction_Id InstructionId2)
            => InstructionId1.Equals(InstructionId2);

        /// <summary>
        /// Compares two instruction identifications for inequality.
        /// </summary>
        public static Boolean operator != (Instruction_Id InstructionId1, Instruction_Id InstructionId2)
            => !InstructionId1.Equals(InstructionId2);

        /// <summary>
        /// Compares two instruction identifications.
        /// </summary>
        public static Boolean operator <  (Instruction_Id InstructionId1, Instruction_Id InstructionId2)
            => InstructionId1.CompareTo(InstructionId2) < 0;

        /// <summary>
        /// Compares two instruction identifications.
        /// </summary>
        public static Boolean operator <= (Instruction_Id InstructionId1, Instruction_Id InstructionId2)
            => InstructionId1.CompareTo(InstructionId2) <= 0;

        /// <summary>
        /// Compares two instruction identifications.
        /// </summary>
        public static Boolean operator >  (Instruction_Id InstructionId1, Instruction_Id InstructionId2)
            => InstructionId1.CompareTo(InstructionId2) > 0;

        /// <summary>
        /// Compares two instruction identifications.
        /// </summary>
        public static Boolean operator >= (Instruction_Id InstructionId1, Instruction_Id InstructionId2)
            => InstructionId1.CompareTo(InstructionId2) >= 0;

        #endregion

        #region IComparable<Instruction_Id> Members

        /// <summary>
        /// Compares two instruction identifications.
        /// </summary>
        /// <param name="Object">A instruction identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Instruction_Id instructionId
                   ? CompareTo(instructionId)
                   : throw new ArgumentException("The given object is not a instruction identification!", nameof(Object));

        /// <summary>
        /// Compares two instruction identifications.
        /// </summary>
        /// <param name="InstructionId">A instruction identification to compare with.</param>
        public Int32 CompareTo(Instruction_Id InstructionId)
            => String.Compare(Value, InstructionId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<Instruction_Id> Members

        /// <summary>
        /// Compares two instruction identifications for equality.
        /// </summary>
        /// <param name="Object">A instruction identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Instruction_Id instructionId && Equals(instructionId);

        /// <summary>
        /// Compares two instruction identifications for equality.
        /// </summary>
        /// <param name="InstructionId">A instruction identification to compare with.</param>
        public Boolean Equals(Instruction_Id InstructionId)
            => String.Equals(Value, InstructionId.Value, StringComparison.Ordinal);

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

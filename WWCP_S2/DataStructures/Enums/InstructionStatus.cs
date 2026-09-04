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
    /// Extension methods for instruction statuses.
    /// </summary>
    public static class InstructionStatusExtensions
    {

        /// <summary>
        /// Indicates whether this instruction status is null or empty.
        /// </summary>
        /// <param name="InstructionStatus">A instruction status.</param>
        public static Boolean IsNullOrEmpty(this InstructionStatus? InstructionStatus)
            => !InstructionStatus.HasValue || InstructionStatus.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this instruction status is NOT null or empty.
        /// </summary>
        /// <param name="InstructionStatus">A instruction status.</param>
        public static Boolean IsNotNullOrEmpty(this InstructionStatus? InstructionStatus)
            => InstructionStatus.HasValue && InstructionStatus.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of an instruction, as reported in InstructionStatusUpdate messages.
    /// </summary>
    public readonly struct InstructionStatus : IS2PredefinedString,
                                         IId,
                                         IEquatable<InstructionStatus>,
                                         IComparable<InstructionStatus>
    {

        #region Data

        private readonly static Dictionary<String, InstructionStatus>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this instruction status is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this instruction status is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the instruction status.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All instruction statuses defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<InstructionStatus> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new instruction status based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a instruction status.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private InstructionStatus(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // InstructionStatus.schema.json
        //   "title": "InstructionStatus",
        //   "type":  "string",
        //   "enum":  ["NEW", "ACCEPTED", "REJECTED", "REVOKED", "STARTED", "SUCCEEDED", "ABORTED"],
        //   "description":
        //     NEW: Instruction was newly created
        //     ACCEPTED: Instruction has been accepted
        //     REJECTED: Instruction was rejected
        //     REVOKED: Instruction was revoked
        //     STARTED: Instruction was executed
        //     SUCCEEDED: Instruction finished successfully
        //     ABORTED: Instruction was aborted.

        #endregion

        #region (private static) Register(Text)

        private static InstructionStatus Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new InstructionStatus(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a instruction status.
        /// </summary>
        /// <param name="Text">A text representation of a instruction status.</param>
        public static InstructionStatus Parse(String Text)
        {

            if (TryParse(Text, out var instructionStatus))
                return instructionStatus;

            throw new ArgumentException($"Invalid text representation of a instruction status: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a instruction status.
        /// </summary>
        /// <param name="Text">A text representation of a instruction status.</param>
        public static InstructionStatus? TryParse(String Text)
        {

            if (TryParse(Text, out var instructionStatus))
                return instructionStatus;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out InstructionStatus)

        /// <summary>
        /// Try to parse the given text as a instruction status. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a instruction status.</param>
        /// <param name="InstructionStatus">The parsed instruction status.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out InstructionStatus    InstructionStatus)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out InstructionStatus))
                    InstructionStatus = new InstructionStatus(Text, false);

                return true;

            }

            InstructionStatus = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this instruction status.
        /// </summary>
        public InstructionStatus Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// NEW: Instruction was newly created
        /// </summary>
        public static InstructionStatus  New    { get; }
            = Register("NEW");

        /// <summary>
        /// ACCEPTED: Instruction has been accepted
        /// </summary>
        public static InstructionStatus  Accepted    { get; }
            = Register("ACCEPTED");

        /// <summary>
        /// REJECTED: Instruction was rejected
        /// </summary>
        public static InstructionStatus  Rejected    { get; }
            = Register("REJECTED");

        /// <summary>
        /// REVOKED: Instruction was revoked
        /// </summary>
        public static InstructionStatus  Revoked    { get; }
            = Register("REVOKED");

        /// <summary>
        /// STARTED: Instruction was executed
        /// </summary>
        public static InstructionStatus  Started    { get; }
            = Register("STARTED");

        /// <summary>
        /// SUCCEEDED: Instruction finished successfully
        /// </summary>
        public static InstructionStatus  Succeeded    { get; }
            = Register("SUCCEEDED");

        /// <summary>
        /// ABORTED: Instruction was aborted.
        /// </summary>
        public static InstructionStatus  Aborted    { get; }
            = Register("ABORTED");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two instruction statuses for equality.
        /// </summary>
        public static Boolean operator == (InstructionStatus InstructionStatus1, InstructionStatus InstructionStatus2)
            => InstructionStatus1.Equals(InstructionStatus2);

        /// <summary>
        /// Compares two instruction statuses for inequality.
        /// </summary>
        public static Boolean operator != (InstructionStatus InstructionStatus1, InstructionStatus InstructionStatus2)
            => !InstructionStatus1.Equals(InstructionStatus2);

        /// <summary>
        /// Compares two instruction statuses.
        /// </summary>
        public static Boolean operator <  (InstructionStatus InstructionStatus1, InstructionStatus InstructionStatus2)
            => InstructionStatus1.CompareTo(InstructionStatus2) < 0;

        /// <summary>
        /// Compares two instruction statuses.
        /// </summary>
        public static Boolean operator <= (InstructionStatus InstructionStatus1, InstructionStatus InstructionStatus2)
            => InstructionStatus1.CompareTo(InstructionStatus2) <= 0;

        /// <summary>
        /// Compares two instruction statuses.
        /// </summary>
        public static Boolean operator >  (InstructionStatus InstructionStatus1, InstructionStatus InstructionStatus2)
            => InstructionStatus1.CompareTo(InstructionStatus2) > 0;

        /// <summary>
        /// Compares two instruction statuses.
        /// </summary>
        public static Boolean operator >= (InstructionStatus InstructionStatus1, InstructionStatus InstructionStatus2)
            => InstructionStatus1.CompareTo(InstructionStatus2) >= 0;

        #endregion

        #region IComparable<InstructionStatus> Members

        /// <summary>
        /// Compares two instruction statuses.
        /// </summary>
        /// <param name="Object">A instruction status to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is InstructionStatus instructionStatus
                   ? CompareTo(instructionStatus)
                   : throw new ArgumentException("The given object is not a instruction status!", nameof(Object));

        /// <summary>
        /// Compares two instruction statuses.
        /// </summary>
        /// <param name="InstructionStatus">A instruction status to compare with.</param>
        public Int32 CompareTo(InstructionStatus InstructionStatus)
            => String.Compare(InternalId, InstructionStatus.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<InstructionStatus> Members

        /// <summary>
        /// Compares two instruction statuses for equality.
        /// </summary>
        /// <param name="Object">A instruction status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is InstructionStatus instructionStatus && Equals(instructionStatus);

        /// <summary>
        /// Compares two instruction statuses for equality.
        /// </summary>
        /// <param name="InstructionStatus">A instruction status to compare with.</param>
        public Boolean Equals(InstructionStatus InstructionStatus)
            => String.Equals(InternalId, InstructionStatus.InternalId, StringComparison.Ordinal);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => InternalId?.GetHashCode(StringComparison.Ordinal) ?? 0;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => InternalId ?? "";

        #endregion

    }

}

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
    /// Extension methods for power sequence statuses.
    /// </summary>
    public static class PPBC_PowerSequenceStatusExtensions
    {

        /// <summary>
        /// Indicates whether this power sequence status is null or empty.
        /// </summary>
        /// <param name="PPBC_PowerSequenceStatus">A power sequence status.</param>
        public static Boolean IsNullOrEmpty(this PPBC_PowerSequenceStatus? PPBC_PowerSequenceStatus)
            => !PPBC_PowerSequenceStatus.HasValue || PPBC_PowerSequenceStatus.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this power sequence status is NOT null or empty.
        /// </summary>
        /// <param name="PPBC_PowerSequenceStatus">A power sequence status.</param>
        public static Boolean IsNotNullOrEmpty(this PPBC_PowerSequenceStatus? PPBC_PowerSequenceStatus)
            => PPBC_PowerSequenceStatus.HasValue && PPBC_PowerSequenceStatus.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of the selected power sequence within a PPBC power sequence container.
    /// </summary>
    public readonly struct PPBC_PowerSequenceStatus : IS2PredefinedString,
                                         IId,
                                         IEquatable<PPBC_PowerSequenceStatus>,
                                         IComparable<PPBC_PowerSequenceStatus>
    {

        #region Data

        private readonly static Dictionary<String, PPBC_PowerSequenceStatus>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this power sequence status is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this power sequence status is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the power sequence status.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All power sequence statuses defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<PPBC_PowerSequenceStatus> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power sequence status based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a power sequence status.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private PPBC_PowerSequenceStatus(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // PPBC.PowerSequenceStatus.schema.json
        //   "title": "PPBC_PowerSequenceStatus",
        //   "type":  "string",
        //   "enum":  ["NOT_SCHEDULED", "SCHEDULED", "EXECUTING", "INTERRUPTED", "FINISHED", "ABORTED"],
        //   "description":
        //     NOT_SCHEDULED: No PPBC.PowerSequence within the PPBC.PowerSequenceContainer is scheduled
        //     SCHEDULED: The selected PPBC.PowerSequence is scheduled to be executed in the future
        //     EXECUTING: The selected PPBC.PowerSequence is currently being executed
        //     INTERRUPTED: The selected PPBC.PowerSequence is being executed, but is currently interrupted and will continue afterwards
        //     FINISHED: The selected PPBC.PowerSequence was executed and finished successfully
        //     ABORTED: The selected PPBC.PowerSequence was aborted by the device and will not continue

        #endregion

        #region (private static) Register(Text)

        private static PPBC_PowerSequenceStatus Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new PPBC_PowerSequenceStatus(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a power sequence status.
        /// </summary>
        /// <param name="Text">A text representation of a power sequence status.</param>
        public static PPBC_PowerSequenceStatus Parse(String Text)
        {

            if (TryParse(Text, out var pPBC_PowerSequenceStatus))
                return pPBC_PowerSequenceStatus;

            throw new ArgumentException($"Invalid text representation of a power sequence status: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a power sequence status.
        /// </summary>
        /// <param name="Text">A text representation of a power sequence status.</param>
        public static PPBC_PowerSequenceStatus? TryParse(String Text)
        {

            if (TryParse(Text, out var pPBC_PowerSequenceStatus))
                return pPBC_PowerSequenceStatus;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out PPBC_PowerSequenceStatus)

        /// <summary>
        /// Try to parse the given text as a power sequence status. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a power sequence status.</param>
        /// <param name="PPBC_PowerSequenceStatus">The parsed power sequence status.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out PPBC_PowerSequenceStatus    PPBC_PowerSequenceStatus)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out PPBC_PowerSequenceStatus))
                    PPBC_PowerSequenceStatus = new PPBC_PowerSequenceStatus(Text, false);

                return true;

            }

            PPBC_PowerSequenceStatus = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power sequence status.
        /// </summary>
        public PPBC_PowerSequenceStatus Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// NOT_SCHEDULED: No PPBC.PowerSequence within the PPBC.PowerSequenceContainer is scheduled
        /// </summary>
        public static PPBC_PowerSequenceStatus  NotScheduled    { get; }
            = Register("NOT_SCHEDULED");

        /// <summary>
        /// SCHEDULED: The selected PPBC.PowerSequence is scheduled to be executed in the future
        /// </summary>
        public static PPBC_PowerSequenceStatus  Scheduled    { get; }
            = Register("SCHEDULED");

        /// <summary>
        /// EXECUTING: The selected PPBC.PowerSequence is currently being executed
        /// </summary>
        public static PPBC_PowerSequenceStatus  Executing    { get; }
            = Register("EXECUTING");

        /// <summary>
        /// INTERRUPTED: The selected PPBC.PowerSequence is being executed, but is currently interrupted and will continue afterwards
        /// </summary>
        public static PPBC_PowerSequenceStatus  Interrupted    { get; }
            = Register("INTERRUPTED");

        /// <summary>
        /// FINISHED: The selected PPBC.PowerSequence was executed and finished successfully
        /// </summary>
        public static PPBC_PowerSequenceStatus  Finished    { get; }
            = Register("FINISHED");

        /// <summary>
        /// ABORTED: The selected PPBC.PowerSequence was aborted by the device and will not continue
        /// </summary>
        public static PPBC_PowerSequenceStatus  Aborted    { get; }
            = Register("ABORTED");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power sequence statuses for equality.
        /// </summary>
        public static Boolean operator == (PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus1, PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus2)
            => PPBC_PowerSequenceStatus1.Equals(PPBC_PowerSequenceStatus2);

        /// <summary>
        /// Compares two power sequence statuses for inequality.
        /// </summary>
        public static Boolean operator != (PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus1, PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus2)
            => !PPBC_PowerSequenceStatus1.Equals(PPBC_PowerSequenceStatus2);

        /// <summary>
        /// Compares two power sequence statuses.
        /// </summary>
        public static Boolean operator <  (PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus1, PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus2)
            => PPBC_PowerSequenceStatus1.CompareTo(PPBC_PowerSequenceStatus2) < 0;

        /// <summary>
        /// Compares two power sequence statuses.
        /// </summary>
        public static Boolean operator <= (PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus1, PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus2)
            => PPBC_PowerSequenceStatus1.CompareTo(PPBC_PowerSequenceStatus2) <= 0;

        /// <summary>
        /// Compares two power sequence statuses.
        /// </summary>
        public static Boolean operator >  (PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus1, PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus2)
            => PPBC_PowerSequenceStatus1.CompareTo(PPBC_PowerSequenceStatus2) > 0;

        /// <summary>
        /// Compares two power sequence statuses.
        /// </summary>
        public static Boolean operator >= (PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus1, PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus2)
            => PPBC_PowerSequenceStatus1.CompareTo(PPBC_PowerSequenceStatus2) >= 0;

        #endregion

        #region IComparable<PPBC_PowerSequenceStatus> Members

        /// <summary>
        /// Compares two power sequence statuses.
        /// </summary>
        /// <param name="Object">A power sequence status to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PPBC_PowerSequenceStatus pPBC_PowerSequenceStatus
                   ? CompareTo(pPBC_PowerSequenceStatus)
                   : throw new ArgumentException("The given object is not a power sequence status!", nameof(Object));

        /// <summary>
        /// Compares two power sequence statuses.
        /// </summary>
        /// <param name="PPBC_PowerSequenceStatus">A power sequence status to compare with.</param>
        public Int32 CompareTo(PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus)
            => String.Compare(InternalId, PPBC_PowerSequenceStatus.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<PPBC_PowerSequenceStatus> Members

        /// <summary>
        /// Compares two power sequence statuses for equality.
        /// </summary>
        /// <param name="Object">A power sequence status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PPBC_PowerSequenceStatus pPBC_PowerSequenceStatus && Equals(pPBC_PowerSequenceStatus);

        /// <summary>
        /// Compares two power sequence statuses for equality.
        /// </summary>
        /// <param name="PPBC_PowerSequenceStatus">A power sequence status to compare with.</param>
        public Boolean Equals(PPBC_PowerSequenceStatus PPBC_PowerSequenceStatus)
            => String.Equals(InternalId, PPBC_PowerSequenceStatus.InternalId, StringComparison.Ordinal);

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

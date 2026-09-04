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
    /// Extension methods for reception status values.
    /// </summary>
    public static class ReceptionStatusValueExtensions
    {

        /// <summary>
        /// Indicates whether this reception status value is null or empty.
        /// </summary>
        /// <param name="ReceptionStatusValue">A reception status value.</param>
        public static Boolean IsNullOrEmpty(this ReceptionStatusValue? ReceptionStatusValue)
            => !ReceptionStatusValue.HasValue || ReceptionStatusValue.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this reception status value is NOT null or empty.
        /// </summary>
        /// <param name="ReceptionStatusValue">A reception status value.</param>
        public static Boolean IsNotNullOrEmpty(this ReceptionStatusValue? ReceptionStatusValue)
            => ReceptionStatusValue.HasValue && ReceptionStatusValue.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The result of receiving a message, as reported in ReceptionStatus messages.
    /// </summary>
    public readonly struct ReceptionStatusValue : IS2PredefinedString,
                                         IId,
                                         IEquatable<ReceptionStatusValue>,
                                         IComparable<ReceptionStatusValue>
    {

        #region Data

        private readonly static Dictionary<String, ReceptionStatusValue>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this reception status value is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this reception status value is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the reception status value.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All reception status values defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<ReceptionStatusValue> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new reception status value based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a reception status value.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private ReceptionStatusValue(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // ReceptionStatusValues.schema.json
        //   "title": "ReceptionStatusValues",
        //   "type":  "string",
        //   "enum":  ["INVALID_DATA", "INVALID_MESSAGE", "INVALID_CONTENT", "TEMPORARY_ERROR", "PERMANENT_ERROR", "OK"],
        //   "description":
        //     INVALID_DATA: Message not understood (e.g. not valid JSON, no message_id found). Consequence: Message is ignored, proceed if possible
        //     INVALID_MESSAGE: Message was not according to schema. Consequence: Message is ignored, proceed if possible
        //     INVALID_CONTENT: Message contents is invalid (e.g. contains a non-existing ID). Somewhat equivalent to BAD_REQUEST in HTTP.. Consequence: Message is ignored, proceed if possible.
        //     TEMPORARY_ERROR: Receiver encountered an error. Consequence: Try to send to message again
        //     PERMANENT_ERROR: Receiver encountered an error which it cannot recover from. Consequence: Disconnect.
        //     OK: Message processed normally. Consequence: Proceed normally.

        #endregion

        #region (private static) Register(Text)

        private static ReceptionStatusValue Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new ReceptionStatusValue(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a reception status value.
        /// </summary>
        /// <param name="Text">A text representation of a reception status value.</param>
        public static ReceptionStatusValue Parse(String Text)
        {

            if (TryParse(Text, out var receptionStatusValue))
                return receptionStatusValue;

            throw new ArgumentException($"Invalid text representation of a reception status value: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a reception status value.
        /// </summary>
        /// <param name="Text">A text representation of a reception status value.</param>
        public static ReceptionStatusValue? TryParse(String Text)
        {

            if (TryParse(Text, out var receptionStatusValue))
                return receptionStatusValue;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ReceptionStatusValue)

        /// <summary>
        /// Try to parse the given text as a reception status value. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a reception status value.</param>
        /// <param name="ReceptionStatusValue">The parsed reception status value.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out ReceptionStatusValue    ReceptionStatusValue)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out ReceptionStatusValue))
                    ReceptionStatusValue = new ReceptionStatusValue(Text, false);

                return true;

            }

            ReceptionStatusValue = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this reception status value.
        /// </summary>
        public ReceptionStatusValue Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// INVALID_DATA: Message not understood (e.g. not valid JSON, no message_id found). Consequence: Message is ignored, proceed if possible
        /// </summary>
        public static ReceptionStatusValue  InvalidData    { get; }
            = Register("INVALID_DATA");

        /// <summary>
        /// INVALID_MESSAGE: Message was not according to schema. Consequence: Message is ignored, proceed if possible
        /// </summary>
        public static ReceptionStatusValue  InvalidMessage    { get; }
            = Register("INVALID_MESSAGE");

        /// <summary>
        /// INVALID_CONTENT: Message contents is invalid (e.g. contains a non-existing ID). Somewhat equivalent to BAD_REQUEST in HTTP.. Consequence: Message is ignored, proceed if possible.
        /// </summary>
        public static ReceptionStatusValue  InvalidContent    { get; }
            = Register("INVALID_CONTENT");

        /// <summary>
        /// TEMPORARY_ERROR: Receiver encountered an error. Consequence: Try to send to message again
        /// </summary>
        public static ReceptionStatusValue  TemporaryError    { get; }
            = Register("TEMPORARY_ERROR");

        /// <summary>
        /// PERMANENT_ERROR: Receiver encountered an error which it cannot recover from. Consequence: Disconnect.
        /// </summary>
        public static ReceptionStatusValue  PermanentError    { get; }
            = Register("PERMANENT_ERROR");

        /// <summary>
        /// OK: Message processed normally. Consequence: Proceed normally.
        /// </summary>
        public static ReceptionStatusValue  OK    { get; }
            = Register("OK");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two reception status values for equality.
        /// </summary>
        public static Boolean operator == (ReceptionStatusValue ReceptionStatusValue1, ReceptionStatusValue ReceptionStatusValue2)
            => ReceptionStatusValue1.Equals(ReceptionStatusValue2);

        /// <summary>
        /// Compares two reception status values for inequality.
        /// </summary>
        public static Boolean operator != (ReceptionStatusValue ReceptionStatusValue1, ReceptionStatusValue ReceptionStatusValue2)
            => !ReceptionStatusValue1.Equals(ReceptionStatusValue2);

        /// <summary>
        /// Compares two reception status values.
        /// </summary>
        public static Boolean operator <  (ReceptionStatusValue ReceptionStatusValue1, ReceptionStatusValue ReceptionStatusValue2)
            => ReceptionStatusValue1.CompareTo(ReceptionStatusValue2) < 0;

        /// <summary>
        /// Compares two reception status values.
        /// </summary>
        public static Boolean operator <= (ReceptionStatusValue ReceptionStatusValue1, ReceptionStatusValue ReceptionStatusValue2)
            => ReceptionStatusValue1.CompareTo(ReceptionStatusValue2) <= 0;

        /// <summary>
        /// Compares two reception status values.
        /// </summary>
        public static Boolean operator >  (ReceptionStatusValue ReceptionStatusValue1, ReceptionStatusValue ReceptionStatusValue2)
            => ReceptionStatusValue1.CompareTo(ReceptionStatusValue2) > 0;

        /// <summary>
        /// Compares two reception status values.
        /// </summary>
        public static Boolean operator >= (ReceptionStatusValue ReceptionStatusValue1, ReceptionStatusValue ReceptionStatusValue2)
            => ReceptionStatusValue1.CompareTo(ReceptionStatusValue2) >= 0;

        #endregion

        #region IComparable<ReceptionStatusValue> Members

        /// <summary>
        /// Compares two reception status values.
        /// </summary>
        /// <param name="Object">A reception status value to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ReceptionStatusValue receptionStatusValue
                   ? CompareTo(receptionStatusValue)
                   : throw new ArgumentException("The given object is not a reception status value!", nameof(Object));

        /// <summary>
        /// Compares two reception status values.
        /// </summary>
        /// <param name="ReceptionStatusValue">A reception status value to compare with.</param>
        public Int32 CompareTo(ReceptionStatusValue ReceptionStatusValue)
            => String.Compare(InternalId, ReceptionStatusValue.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<ReceptionStatusValue> Members

        /// <summary>
        /// Compares two reception status values for equality.
        /// </summary>
        /// <param name="Object">A reception status value to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is ReceptionStatusValue receptionStatusValue && Equals(receptionStatusValue);

        /// <summary>
        /// Compares two reception status values for equality.
        /// </summary>
        /// <param name="ReceptionStatusValue">A reception status value to compare with.</param>
        public Boolean Equals(ReceptionStatusValue ReceptionStatusValue)
            => String.Equals(InternalId, ReceptionStatusValue.InternalId, StringComparison.Ordinal);

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

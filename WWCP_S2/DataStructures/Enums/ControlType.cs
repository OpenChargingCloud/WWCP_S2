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
    /// Extension methods for control types.
    /// </summary>
    public static class ControlTypeExtensions
    {

        /// <summary>
        /// Indicates whether this control type is null or empty.
        /// </summary>
        /// <param name="ControlType">A control type.</param>
        public static Boolean IsNullOrEmpty(this ControlType? ControlType)
            => !ControlType.HasValue || ControlType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this control type is NOT null or empty.
        /// </summary>
        /// <param name="ControlType">A control type.</param>
        public static Boolean IsNotNullOrEmpty(this ControlType? ControlType)
            => ControlType.HasValue && ControlType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A control type: the sub protocol an RM offers and a CEM selects.
    /// </summary>
    public readonly struct ControlType : IS2PredefinedString,
                                         IId,
                                         IEquatable<ControlType>,
                                         IComparable<ControlType>
    {

        #region Data

        private readonly static Dictionary<String, ControlType>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this control type is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this control type is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the control type.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All control types defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<ControlType> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new control type based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a control type.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private ControlType(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // ControlType.schema.json
        //   "title": "ControlType",
        //   "type":  "string",
        //   "enum":  ["POWER_ENVELOPE_BASED_CONTROL", "POWER_PROFILE_BASED_CONTROL", "OPERATION_MODE_BASED_CONTROL",
        //             "FILL_RATE_BASED_CONTROL", "DEMAND_DRIVEN_BASED_CONTROL", "NOT_CONTROLABLE", "NO_SELECTION"],
        //   "description":
        //     POWER_ENVELOPE_BASED_CONTROL: Identifier for the Power Envelope Based Control type
        //     POWER_PROFILE_BASED_CONTROL:  Identifier for the Power Profile Based Control type
        //     OPERATION_MODE_BASED_CONTROL: Identifier for the Operation Mode Based Control type
        //     FILL_RATE_BASED_CONTROL:      Identifier for the Fill Rate Based Control type
        //     DEMAND_DRIVEN_BASED_CONTROL:  Identifier for the Demand Driven Based Control type
        //     NOT_CONTROLABLE:              Identifier that is to be used if no control is possible.
        //                                   Resources of this type can still provide measurements and forecast
        //     NO_SELECTION:                 Identifier that is to be used if no control type is or has been selected.
        //
        // Note: The schema spells "NOT_CONTROLABLE" with a single L; the wire value is kept verbatim.

        #endregion

        #region (private static) Register(Text)

        private static ControlType Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new ControlType(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a control type.
        /// </summary>
        /// <param name="Text">A text representation of a control type.</param>
        public static ControlType Parse(String Text)
        {

            if (TryParse(Text, out var controlType))
                return controlType;

            throw new ArgumentException($"Invalid text representation of a control type: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a control type.
        /// </summary>
        /// <param name="Text">A text representation of a control type.</param>
        public static ControlType? TryParse(String Text)
        {

            if (TryParse(Text, out var controlType))
                return controlType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ControlType)

        /// <summary>
        /// Try to parse the given text as a control type. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a control type.</param>
        /// <param name="ControlType">The parsed control type.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out ControlType    ControlType)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out ControlType))
                    ControlType = new ControlType(Text, false);

                return true;

            }

            ControlType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this control type.
        /// </summary>
        public ControlType Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// POWER_ENVELOPE_BASED_CONTROL: Identifier for the Power Envelope Based Control type.
        /// </summary>
        public static ControlType  PowerEnvelopeBasedControl    { get; }
            = Register("POWER_ENVELOPE_BASED_CONTROL");

        /// <summary>
        /// POWER_PROFILE_BASED_CONTROL: Identifier for the Power Profile Based Control type.
        /// </summary>
        public static ControlType  PowerProfileBasedControl     { get; }
            = Register("POWER_PROFILE_BASED_CONTROL");

        /// <summary>
        /// OPERATION_MODE_BASED_CONTROL: Identifier for the Operation Mode Based Control type.
        /// </summary>
        public static ControlType  OperationModeBasedControl    { get; }
            = Register("OPERATION_MODE_BASED_CONTROL");

        /// <summary>
        /// FILL_RATE_BASED_CONTROL: Identifier for the Fill Rate Based Control type.
        /// </summary>
        public static ControlType  FillRateBasedControl         { get; }
            = Register("FILL_RATE_BASED_CONTROL");

        /// <summary>
        /// DEMAND_DRIVEN_BASED_CONTROL: Identifier for the Demand Driven Based Control type.
        /// </summary>
        public static ControlType  DemandDrivenBasedControl     { get; }
            = Register("DEMAND_DRIVEN_BASED_CONTROL");

        /// <summary>
        /// NOT_CONTROLABLE (sic): Identifier that is to be used if no control is possible.
        /// Resources of this type can still provide measurements and forecasts.
        /// </summary>
        public static ControlType  NotControllable              { get; }
            = Register("NOT_CONTROLABLE");

        /// <summary>
        /// NO_SELECTION: Identifier that is to be used if no control type is or has been selected.
        /// </summary>
        public static ControlType  NoSelection                  { get; }
            = Register("NO_SELECTION");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two control types for equality.
        /// </summary>
        public static Boolean operator == (ControlType ControlType1, ControlType ControlType2)
            => ControlType1.Equals(ControlType2);

        /// <summary>
        /// Compares two control types for inequality.
        /// </summary>
        public static Boolean operator != (ControlType ControlType1, ControlType ControlType2)
            => !ControlType1.Equals(ControlType2);

        /// <summary>
        /// Compares two control types.
        /// </summary>
        public static Boolean operator <  (ControlType ControlType1, ControlType ControlType2)
            => ControlType1.CompareTo(ControlType2) < 0;

        /// <summary>
        /// Compares two control types.
        /// </summary>
        public static Boolean operator <= (ControlType ControlType1, ControlType ControlType2)
            => ControlType1.CompareTo(ControlType2) <= 0;

        /// <summary>
        /// Compares two control types.
        /// </summary>
        public static Boolean operator >  (ControlType ControlType1, ControlType ControlType2)
            => ControlType1.CompareTo(ControlType2) > 0;

        /// <summary>
        /// Compares two control types.
        /// </summary>
        public static Boolean operator >= (ControlType ControlType1, ControlType ControlType2)
            => ControlType1.CompareTo(ControlType2) >= 0;

        #endregion

        #region IComparable<ControlType> Members

        /// <summary>
        /// Compares two control types.
        /// </summary>
        /// <param name="Object">A control type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ControlType controlType
                   ? CompareTo(controlType)
                   : throw new ArgumentException("The given object is not a control type!", nameof(Object));

        /// <summary>
        /// Compares two control types.
        /// </summary>
        /// <param name="ControlType">A control type to compare with.</param>
        public Int32 CompareTo(ControlType ControlType)
            => String.Compare(InternalId, ControlType.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<ControlType> Members

        /// <summary>
        /// Compares two control types for equality.
        /// </summary>
        /// <param name="Object">A control type to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is ControlType controlType && Equals(controlType);

        /// <summary>
        /// Compares two control types for equality.
        /// </summary>
        /// <param name="ControlType">A control type to compare with.</param>
        public Boolean Equals(ControlType ControlType)
            => String.Equals(InternalId, ControlType.InternalId, StringComparison.Ordinal);

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

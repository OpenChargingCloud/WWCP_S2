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
    /// Extension methods for commodity quantities.
    /// </summary>
    public static class CommodityQuantityExtensions
    {

        /// <summary>
        /// Indicates whether this commodity quantity is null or empty.
        /// </summary>
        /// <param name="CommodityQuantity">A commodity quantity.</param>
        public static Boolean IsNullOrEmpty(this CommodityQuantity? CommodityQuantity)
            => !CommodityQuantity.HasValue || CommodityQuantity.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this commodity quantity is NOT null or empty.
        /// </summary>
        /// <param name="CommodityQuantity">A commodity quantity.</param>
        public static Boolean IsNotNullOrEmpty(this CommodityQuantity? CommodityQuantity)
            => CommodityQuantity.HasValue && CommodityQuantity.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A commodity quantity: a physical quantity of a commodity, e.g. electric power on phase 1 in Watt.
    /// </summary>
    public readonly struct CommodityQuantity : IS2PredefinedString,
                                         IId,
                                         IEquatable<CommodityQuantity>,
                                         IComparable<CommodityQuantity>
    {

        #region Data

        private readonly static Dictionary<String, CommodityQuantity>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this commodity quantity is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this commodity quantity is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the commodity quantity.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// The commodity this commodity quantity belongs to: ELECTRIC.POWER.* → ELECTRICITY,
        /// NATURAL_GAS.FLOW_RATE and HYDROGEN.FLOW_RATE → GAS, HEAT.* → HEAT, OIL.FLOW_RATE → OIL;
        /// null for values that do not belong to a known commodity family.
        /// Used to match the power ranges of an operation mode against the supported
        /// commodities of an actuator description (CONVENTIONS.md §5).
        /// </summary>
        public Commodity?  Commodity           { get; }

        /// <summary>
        /// All commodity quantities defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<CommodityQuantity> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new commodity quantity based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a commodity quantity.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private CommodityQuantity(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
            this.Commodity   = ToCommodity(Text);
        }

        #endregion

        #region (private static) ToCommodity(Text)

        /// <summary>
        /// Map the text of a commodity quantity onto its commodity family.
        /// A plain switch on the text is used deliberately: the static lookup of this
        /// struct is still being initialised while the predefined values are registered.
        /// </summary>
        /// <param name="Text">The text representation of a commodity quantity.</param>
        private static Commodity? ToCommodity(String? Text)
        {

            if (Text is null)
                return null;

            if (Text.StartsWith("ELECTRIC.",    StringComparison.Ordinal))
                return S2.Commodity.Electricity;

            if (Text.StartsWith("NATURAL_GAS.", StringComparison.Ordinal) ||
                Text.StartsWith("HYDROGEN.",    StringComparison.Ordinal))
                return S2.Commodity.Gas;

            if (Text.StartsWith("HEAT.",        StringComparison.Ordinal))
                return S2.Commodity.Heat;

            if (Text.StartsWith("OIL.",         StringComparison.Ordinal))
                return S2.Commodity.Oil;

            return null;

        }

        #endregion


        #region Documentation

        // CommodityQuantity.schema.json
        //   "title": "CommodityQuantity",
        //   "type":  "string",
        //   "enum":  ["ELECTRIC.POWER.L1", "ELECTRIC.POWER.L2", "ELECTRIC.POWER.L3", "ELECTRIC.POWER.3_PHASE_SYMMETRIC", "NATURAL_GAS.FLOW_RATE", "HYDROGEN.FLOW_RATE", "HEAT.TEMPERATURE", "HEAT.FLOW_RATE", "HEAT.THERMAL_POWER", "OIL.FLOW_RATE"],
        //   "description":
        //     ELECTRIC.POWER.L1: Electric power described in Watt on phase 1. If a device utilizes only one phase it should always use L1.
        //     ELECTRIC.POWER.L2: Electric power described in Watt on phase 2. Only applicable for 3 phase devices.
        //     ELECTRIC.POWER.L3: Electric power described in Watt on phase 3. Only applicable for 3 phase devices.
        //     ELECTRIC.POWER.3_PHASE_SYMMETRIC: Electric power described in Watt on when power is equally shared among the three phases. Only applicable for 3 phase devices.
        //     NATURAL_GAS.FLOW_RATE: Gas flow rate described in liters per second
        //     HYDROGEN.FLOW_RATE: Gas flow rate described in grams per second
        //     HEAT.TEMPERATURE: Heat described in degrees Celsius
        //     HEAT.FLOW_RATE: Flow rate of heat carrying gas or liquid in liters per second
        //     HEAT.THERMAL_POWER: Thermal power in Watt
        //     OIL.FLOW_RATE: Oil flow rate described in liters per hour

        #endregion

        #region (private static) Register(Text)

        private static CommodityQuantity Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new CommodityQuantity(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a commodity quantity.
        /// </summary>
        /// <param name="Text">A text representation of a commodity quantity.</param>
        public static CommodityQuantity Parse(String Text)
        {

            if (TryParse(Text, out var commodityQuantity))
                return commodityQuantity;

            throw new ArgumentException($"Invalid text representation of a commodity quantity: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a commodity quantity.
        /// </summary>
        /// <param name="Text">A text representation of a commodity quantity.</param>
        public static CommodityQuantity? TryParse(String Text)
        {

            if (TryParse(Text, out var commodityQuantity))
                return commodityQuantity;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out CommodityQuantity)

        /// <summary>
        /// Try to parse the given text as a commodity quantity. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a commodity quantity.</param>
        /// <param name="CommodityQuantity">The parsed commodity quantity.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out CommodityQuantity    CommodityQuantity)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out CommodityQuantity))
                    CommodityQuantity = new CommodityQuantity(Text, false);

                return true;

            }

            CommodityQuantity = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this commodity quantity.
        /// </summary>
        public CommodityQuantity Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// ELECTRIC.POWER.L1: Electric power described in Watt on phase 1. If a device utilizes only one phase it should always use L1.
        /// </summary>
        public static CommodityQuantity  ElectricPowerL1    { get; }
            = Register("ELECTRIC.POWER.L1");

        /// <summary>
        /// ELECTRIC.POWER.L2: Electric power described in Watt on phase 2. Only applicable for 3 phase devices.
        /// </summary>
        public static CommodityQuantity  ElectricPowerL2    { get; }
            = Register("ELECTRIC.POWER.L2");

        /// <summary>
        /// ELECTRIC.POWER.L3: Electric power described in Watt on phase 3. Only applicable for 3 phase devices.
        /// </summary>
        public static CommodityQuantity  ElectricPowerL3    { get; }
            = Register("ELECTRIC.POWER.L3");

        /// <summary>
        /// ELECTRIC.POWER.3_PHASE_SYMMETRIC: Electric power described in Watt on when power is equally shared among the three phases. Only applicable for 3 phase devices.
        /// </summary>
        public static CommodityQuantity  ElectricPower3PhaseSymmetric    { get; }
            = Register("ELECTRIC.POWER.3_PHASE_SYMMETRIC");

        /// <summary>
        /// NATURAL_GAS.FLOW_RATE: Gas flow rate described in liters per second
        /// </summary>
        public static CommodityQuantity  NaturalGasFlowRate    { get; }
            = Register("NATURAL_GAS.FLOW_RATE");

        /// <summary>
        /// HYDROGEN.FLOW_RATE: Gas flow rate described in grams per second
        /// </summary>
        public static CommodityQuantity  HydrogenFlowRate    { get; }
            = Register("HYDROGEN.FLOW_RATE");

        /// <summary>
        /// HEAT.TEMPERATURE: Heat described in degrees Celsius
        /// </summary>
        public static CommodityQuantity  HeatTemperature    { get; }
            = Register("HEAT.TEMPERATURE");

        /// <summary>
        /// HEAT.FLOW_RATE: Flow rate of heat carrying gas or liquid in liters per second
        /// </summary>
        public static CommodityQuantity  HeatFlowRate    { get; }
            = Register("HEAT.FLOW_RATE");

        /// <summary>
        /// HEAT.THERMAL_POWER: Thermal power in Watt
        /// </summary>
        public static CommodityQuantity  HeatThermalPower    { get; }
            = Register("HEAT.THERMAL_POWER");

        /// <summary>
        /// OIL.FLOW_RATE: Oil flow rate described in liters per hour
        /// </summary>
        public static CommodityQuantity  OilFlowRate    { get; }
            = Register("OIL.FLOW_RATE");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two commodity quantities for equality.
        /// </summary>
        public static Boolean operator == (CommodityQuantity CommodityQuantity1, CommodityQuantity CommodityQuantity2)
            => CommodityQuantity1.Equals(CommodityQuantity2);

        /// <summary>
        /// Compares two commodity quantities for inequality.
        /// </summary>
        public static Boolean operator != (CommodityQuantity CommodityQuantity1, CommodityQuantity CommodityQuantity2)
            => !CommodityQuantity1.Equals(CommodityQuantity2);

        /// <summary>
        /// Compares two commodity quantities.
        /// </summary>
        public static Boolean operator <  (CommodityQuantity CommodityQuantity1, CommodityQuantity CommodityQuantity2)
            => CommodityQuantity1.CompareTo(CommodityQuantity2) < 0;

        /// <summary>
        /// Compares two commodity quantities.
        /// </summary>
        public static Boolean operator <= (CommodityQuantity CommodityQuantity1, CommodityQuantity CommodityQuantity2)
            => CommodityQuantity1.CompareTo(CommodityQuantity2) <= 0;

        /// <summary>
        /// Compares two commodity quantities.
        /// </summary>
        public static Boolean operator >  (CommodityQuantity CommodityQuantity1, CommodityQuantity CommodityQuantity2)
            => CommodityQuantity1.CompareTo(CommodityQuantity2) > 0;

        /// <summary>
        /// Compares two commodity quantities.
        /// </summary>
        public static Boolean operator >= (CommodityQuantity CommodityQuantity1, CommodityQuantity CommodityQuantity2)
            => CommodityQuantity1.CompareTo(CommodityQuantity2) >= 0;

        #endregion

        #region IComparable<CommodityQuantity> Members

        /// <summary>
        /// Compares two commodity quantities.
        /// </summary>
        /// <param name="Object">A commodity quantity to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is CommodityQuantity commodityQuantity
                   ? CompareTo(commodityQuantity)
                   : throw new ArgumentException("The given object is not a commodity quantity!", nameof(Object));

        /// <summary>
        /// Compares two commodity quantities.
        /// </summary>
        /// <param name="CommodityQuantity">A commodity quantity to compare with.</param>
        public Int32 CompareTo(CommodityQuantity CommodityQuantity)
            => String.Compare(InternalId, CommodityQuantity.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<CommodityQuantity> Members

        /// <summary>
        /// Compares two commodity quantities for equality.
        /// </summary>
        /// <param name="Object">A commodity quantity to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is CommodityQuantity commodityQuantity && Equals(commodityQuantity);

        /// <summary>
        /// Compares two commodity quantities for equality.
        /// </summary>
        /// <param name="CommodityQuantity">A commodity quantity to compare with.</param>
        public Boolean Equals(CommodityQuantity CommodityQuantity)
            => String.Equals(InternalId, CommodityQuantity.InternalId, StringComparison.Ordinal);

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

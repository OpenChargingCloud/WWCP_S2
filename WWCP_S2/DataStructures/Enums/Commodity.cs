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
    /// Extension methods for commodities.
    /// </summary>
    public static class CommodityExtensions
    {

        /// <summary>
        /// Indicates whether this commodity is null or empty.
        /// </summary>
        /// <param name="Commodity">A commodity.</param>
        public static Boolean IsNullOrEmpty(this Commodity? Commodity)
            => !Commodity.HasValue || Commodity.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this commodity is NOT null or empty.
        /// </summary>
        /// <param name="Commodity">A commodity.</param>
        public static Boolean IsNotNullOrEmpty(this Commodity? Commodity)
            => Commodity.HasValue && Commodity.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A commodity: the kind of energy a Resource Manager consumes, produces or stores.
    /// </summary>
    public readonly struct Commodity : IS2PredefinedString,
                                         IId,
                                         IEquatable<Commodity>,
                                         IComparable<Commodity>
    {

        #region Data

        private readonly static Dictionary<String, Commodity>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this commodity is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this commodity is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the commodity.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All commodities defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<Commodity> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new commodity based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a commodity.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private Commodity(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // Commodity.schema.json
        //   "title": "Commodity",
        //   "type":  "string",
        //   "enum":  ["GAS", "HEAT", "ELECTRICITY", "OIL"],
        //   "description":
        //     GAS: Identifier for Commodity GAS
        //     HEAT: Identifier for Commodity HEAT
        //     ELECTRICITY: Identifier for Commodity ELECTRICITY
        //     OIL: Identifier for Commodity OIL

        #endregion

        #region (private static) Register(Text)

        private static Commodity Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new Commodity(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a commodity.
        /// </summary>
        /// <param name="Text">A text representation of a commodity.</param>
        public static Commodity Parse(String Text)
        {

            if (TryParse(Text, out var commodity))
                return commodity;

            throw new ArgumentException($"Invalid text representation of a commodity: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a commodity.
        /// </summary>
        /// <param name="Text">A text representation of a commodity.</param>
        public static Commodity? TryParse(String Text)
        {

            if (TryParse(Text, out var commodity))
                return commodity;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out Commodity)

        /// <summary>
        /// Try to parse the given text as a commodity. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a commodity.</param>
        /// <param name="Commodity">The parsed commodity.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out Commodity    Commodity)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out Commodity))
                    Commodity = new Commodity(Text, false);

                return true;

            }

            Commodity = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this commodity.
        /// </summary>
        public Commodity Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// GAS: Identifier for Commodity GAS
        /// </summary>
        public static Commodity  Gas    { get; }
            = Register("GAS");

        /// <summary>
        /// HEAT: Identifier for Commodity HEAT
        /// </summary>
        public static Commodity  Heat    { get; }
            = Register("HEAT");

        /// <summary>
        /// ELECTRICITY: Identifier for Commodity ELECTRICITY
        /// </summary>
        public static Commodity  Electricity    { get; }
            = Register("ELECTRICITY");

        /// <summary>
        /// OIL: Identifier for Commodity OIL
        /// </summary>
        public static Commodity  Oil    { get; }
            = Register("OIL");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two commodities for equality.
        /// </summary>
        public static Boolean operator == (Commodity Commodity1, Commodity Commodity2)
            => Commodity1.Equals(Commodity2);

        /// <summary>
        /// Compares two commodities for inequality.
        /// </summary>
        public static Boolean operator != (Commodity Commodity1, Commodity Commodity2)
            => !Commodity1.Equals(Commodity2);

        /// <summary>
        /// Compares two commodities.
        /// </summary>
        public static Boolean operator <  (Commodity Commodity1, Commodity Commodity2)
            => Commodity1.CompareTo(Commodity2) < 0;

        /// <summary>
        /// Compares two commodities.
        /// </summary>
        public static Boolean operator <= (Commodity Commodity1, Commodity Commodity2)
            => Commodity1.CompareTo(Commodity2) <= 0;

        /// <summary>
        /// Compares two commodities.
        /// </summary>
        public static Boolean operator >  (Commodity Commodity1, Commodity Commodity2)
            => Commodity1.CompareTo(Commodity2) > 0;

        /// <summary>
        /// Compares two commodities.
        /// </summary>
        public static Boolean operator >= (Commodity Commodity1, Commodity Commodity2)
            => Commodity1.CompareTo(Commodity2) >= 0;

        #endregion

        #region IComparable<Commodity> Members

        /// <summary>
        /// Compares two commodities.
        /// </summary>
        /// <param name="Object">A commodity to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Commodity commodity
                   ? CompareTo(commodity)
                   : throw new ArgumentException("The given object is not a commodity!", nameof(Object));

        /// <summary>
        /// Compares two commodities.
        /// </summary>
        /// <param name="Commodity">A commodity to compare with.</param>
        public Int32 CompareTo(Commodity Commodity)
            => String.Compare(InternalId, Commodity.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<Commodity> Members

        /// <summary>
        /// Compares two commodities for equality.
        /// </summary>
        /// <param name="Object">A commodity to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Commodity commodity && Equals(commodity);

        /// <summary>
        /// Compares two commodities for equality.
        /// </summary>
        /// <param name="Commodity">A commodity to compare with.</param>
        public Boolean Equals(Commodity Commodity)
            => String.Equals(InternalId, Commodity.InternalId, StringComparison.Ordinal);

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

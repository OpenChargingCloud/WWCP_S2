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
    /// Extension methods for currencies.
    /// </summary>
    public static class CurrencyExtensions
    {

        /// <summary>
        /// Indicates whether this currency is null or empty.
        /// </summary>
        /// <param name="Currency">A currency.</param>
        public static Boolean IsNullOrEmpty(this Currency? Currency)
            => !Currency.HasValue || Currency.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this currency is NOT null or empty.
        /// </summary>
        /// <param name="Currency">A currency.</param>
        public static Boolean IsNotNullOrEmpty(this Currency? Currency)
            => Currency.HasValue && Currency.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A currency (ISO 4217 code) used when a Resource Manager gives cost information.
    /// </summary>
    public readonly struct Currency : IS2PredefinedString,
                                         IId,
                                         IEquatable<Currency>,
                                         IComparable<Currency>
    {

        #region Data

        private readonly static Dictionary<String, Currency>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this currency is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this currency is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the currency.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All currencies defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<Currency> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new currency based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a currency.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private Currency(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // Currency.schema.json
        //   "title": "Currency",
        //   "type":  "string",
        //   "enum":  ["AED", "ANG", "AUD", "CHE", "CHF", "CHW", "EUR", "GBP", "LBP", "LKR", "LRD", "LSL", "LYD", "MAD", "MDL", "MGA", "MKD", "MMK", "MNT", "MOP", "MRO", "MUR", "MVR", "MWK", "MXN", "MXV", "MYR", "MZN", "NAD", "NGN", "NIO", "NOK", "NPR", "NZD", "OMR", "PAB", "PEN", "PGK", "PHP", "PKR", "PLN", "PYG", "QAR", "RON", "RSD", "RUB", "RWF", "SAR", "SBD", "SCR", "SDG", "SEK", "SGD", "SHP", "SLL", "SOS", "SRD", "SSP", "STD", "SYP", "SZL", "THB", "TJS", "TMT", "TND", "TOP", "TRY", "TTD", "TWD", "TZS", "UAH", "UGX", "USD", "USN", "UYI", "UYU", "UZS", "VEF", "VND", "VUV", "WST", "XAG", "XAU", "XBA", "XBB", "XBC", "XBD", "XCD", "XOF", "XPD", "XPF", "XPT", "XSU", "XTS", "XUA", "XXX", "YER", "ZAR", "ZMW", "ZWL"],
        //   "description":
        //     Currency used when this resource gives cost information

        #endregion

        #region (private static) Register(Text)

        private static Currency Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new Currency(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a currency.
        /// </summary>
        /// <param name="Text">A text representation of a currency.</param>
        public static Currency Parse(String Text)
        {

            if (TryParse(Text, out var currency))
                return currency;

            throw new ArgumentException($"Invalid text representation of a currency: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a currency.
        /// </summary>
        /// <param name="Text">A text representation of a currency.</param>
        public static Currency? TryParse(String Text)
        {

            if (TryParse(Text, out var currency))
                return currency;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out Currency)

        /// <summary>
        /// Try to parse the given text as a currency. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a currency.</param>
        /// <param name="Currency">The parsed currency.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out Currency    Currency)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out Currency))
                    Currency = new Currency(Text, false);

                return true;

            }

            Currency = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this currency.
        /// </summary>
        public Currency Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// AED: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  AED    { get; }
            = Register("AED");

        /// <summary>
        /// ANG: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  ANG    { get; }
            = Register("ANG");

        /// <summary>
        /// AUD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  AUD    { get; }
            = Register("AUD");

        /// <summary>
        /// CHE: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  CHE    { get; }
            = Register("CHE");

        /// <summary>
        /// CHF: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  CHF    { get; }
            = Register("CHF");

        /// <summary>
        /// CHW: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  CHW    { get; }
            = Register("CHW");

        /// <summary>
        /// EUR: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  EUR    { get; }
            = Register("EUR");

        /// <summary>
        /// GBP: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  GBP    { get; }
            = Register("GBP");

        /// <summary>
        /// LBP: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  LBP    { get; }
            = Register("LBP");

        /// <summary>
        /// LKR: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  LKR    { get; }
            = Register("LKR");

        /// <summary>
        /// LRD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  LRD    { get; }
            = Register("LRD");

        /// <summary>
        /// LSL: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  LSL    { get; }
            = Register("LSL");

        /// <summary>
        /// LYD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  LYD    { get; }
            = Register("LYD");

        /// <summary>
        /// MAD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MAD    { get; }
            = Register("MAD");

        /// <summary>
        /// MDL: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MDL    { get; }
            = Register("MDL");

        /// <summary>
        /// MGA: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MGA    { get; }
            = Register("MGA");

        /// <summary>
        /// MKD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MKD    { get; }
            = Register("MKD");

        /// <summary>
        /// MMK: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MMK    { get; }
            = Register("MMK");

        /// <summary>
        /// MNT: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MNT    { get; }
            = Register("MNT");

        /// <summary>
        /// MOP: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MOP    { get; }
            = Register("MOP");

        /// <summary>
        /// MRO: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MRO    { get; }
            = Register("MRO");

        /// <summary>
        /// MUR: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MUR    { get; }
            = Register("MUR");

        /// <summary>
        /// MVR: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MVR    { get; }
            = Register("MVR");

        /// <summary>
        /// MWK: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MWK    { get; }
            = Register("MWK");

        /// <summary>
        /// MXN: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MXN    { get; }
            = Register("MXN");

        /// <summary>
        /// MXV: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MXV    { get; }
            = Register("MXV");

        /// <summary>
        /// MYR: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MYR    { get; }
            = Register("MYR");

        /// <summary>
        /// MZN: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  MZN    { get; }
            = Register("MZN");

        /// <summary>
        /// NAD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  NAD    { get; }
            = Register("NAD");

        /// <summary>
        /// NGN: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  NGN    { get; }
            = Register("NGN");

        /// <summary>
        /// NIO: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  NIO    { get; }
            = Register("NIO");

        /// <summary>
        /// NOK: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  NOK    { get; }
            = Register("NOK");

        /// <summary>
        /// NPR: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  NPR    { get; }
            = Register("NPR");

        /// <summary>
        /// NZD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  NZD    { get; }
            = Register("NZD");

        /// <summary>
        /// OMR: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  OMR    { get; }
            = Register("OMR");

        /// <summary>
        /// PAB: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  PAB    { get; }
            = Register("PAB");

        /// <summary>
        /// PEN: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  PEN    { get; }
            = Register("PEN");

        /// <summary>
        /// PGK: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  PGK    { get; }
            = Register("PGK");

        /// <summary>
        /// PHP: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  PHP    { get; }
            = Register("PHP");

        /// <summary>
        /// PKR: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  PKR    { get; }
            = Register("PKR");

        /// <summary>
        /// PLN: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  PLN    { get; }
            = Register("PLN");

        /// <summary>
        /// PYG: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  PYG    { get; }
            = Register("PYG");

        /// <summary>
        /// QAR: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  QAR    { get; }
            = Register("QAR");

        /// <summary>
        /// RON: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  RON    { get; }
            = Register("RON");

        /// <summary>
        /// RSD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  RSD    { get; }
            = Register("RSD");

        /// <summary>
        /// RUB: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  RUB    { get; }
            = Register("RUB");

        /// <summary>
        /// RWF: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  RWF    { get; }
            = Register("RWF");

        /// <summary>
        /// SAR: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SAR    { get; }
            = Register("SAR");

        /// <summary>
        /// SBD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SBD    { get; }
            = Register("SBD");

        /// <summary>
        /// SCR: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SCR    { get; }
            = Register("SCR");

        /// <summary>
        /// SDG: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SDG    { get; }
            = Register("SDG");

        /// <summary>
        /// SEK: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SEK    { get; }
            = Register("SEK");

        /// <summary>
        /// SGD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SGD    { get; }
            = Register("SGD");

        /// <summary>
        /// SHP: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SHP    { get; }
            = Register("SHP");

        /// <summary>
        /// SLL: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SLL    { get; }
            = Register("SLL");

        /// <summary>
        /// SOS: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SOS    { get; }
            = Register("SOS");

        /// <summary>
        /// SRD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SRD    { get; }
            = Register("SRD");

        /// <summary>
        /// SSP: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SSP    { get; }
            = Register("SSP");

        /// <summary>
        /// STD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  STD    { get; }
            = Register("STD");

        /// <summary>
        /// SYP: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SYP    { get; }
            = Register("SYP");

        /// <summary>
        /// SZL: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  SZL    { get; }
            = Register("SZL");

        /// <summary>
        /// THB: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  THB    { get; }
            = Register("THB");

        /// <summary>
        /// TJS: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  TJS    { get; }
            = Register("TJS");

        /// <summary>
        /// TMT: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  TMT    { get; }
            = Register("TMT");

        /// <summary>
        /// TND: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  TND    { get; }
            = Register("TND");

        /// <summary>
        /// TOP: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  TOP    { get; }
            = Register("TOP");

        /// <summary>
        /// TRY: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  TRY    { get; }
            = Register("TRY");

        /// <summary>
        /// TTD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  TTD    { get; }
            = Register("TTD");

        /// <summary>
        /// TWD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  TWD    { get; }
            = Register("TWD");

        /// <summary>
        /// TZS: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  TZS    { get; }
            = Register("TZS");

        /// <summary>
        /// UAH: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  UAH    { get; }
            = Register("UAH");

        /// <summary>
        /// UGX: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  UGX    { get; }
            = Register("UGX");

        /// <summary>
        /// USD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  USD    { get; }
            = Register("USD");

        /// <summary>
        /// USN: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  USN    { get; }
            = Register("USN");

        /// <summary>
        /// UYI: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  UYI    { get; }
            = Register("UYI");

        /// <summary>
        /// UYU: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  UYU    { get; }
            = Register("UYU");

        /// <summary>
        /// UZS: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  UZS    { get; }
            = Register("UZS");

        /// <summary>
        /// VEF: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  VEF    { get; }
            = Register("VEF");

        /// <summary>
        /// VND: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  VND    { get; }
            = Register("VND");

        /// <summary>
        /// VUV: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  VUV    { get; }
            = Register("VUV");

        /// <summary>
        /// WST: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  WST    { get; }
            = Register("WST");

        /// <summary>
        /// XAG: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XAG    { get; }
            = Register("XAG");

        /// <summary>
        /// XAU: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XAU    { get; }
            = Register("XAU");

        /// <summary>
        /// XBA: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XBA    { get; }
            = Register("XBA");

        /// <summary>
        /// XBB: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XBB    { get; }
            = Register("XBB");

        /// <summary>
        /// XBC: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XBC    { get; }
            = Register("XBC");

        /// <summary>
        /// XBD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XBD    { get; }
            = Register("XBD");

        /// <summary>
        /// XCD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XCD    { get; }
            = Register("XCD");

        /// <summary>
        /// XOF: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XOF    { get; }
            = Register("XOF");

        /// <summary>
        /// XPD: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XPD    { get; }
            = Register("XPD");

        /// <summary>
        /// XPF: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XPF    { get; }
            = Register("XPF");

        /// <summary>
        /// XPT: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XPT    { get; }
            = Register("XPT");

        /// <summary>
        /// XSU: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XSU    { get; }
            = Register("XSU");

        /// <summary>
        /// XTS: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XTS    { get; }
            = Register("XTS");

        /// <summary>
        /// XUA: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XUA    { get; }
            = Register("XUA");

        /// <summary>
        /// XXX: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  XXX    { get; }
            = Register("XXX");

        /// <summary>
        /// YER: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  YER    { get; }
            = Register("YER");

        /// <summary>
        /// ZAR: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  ZAR    { get; }
            = Register("ZAR");

        /// <summary>
        /// ZMW: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  ZMW    { get; }
            = Register("ZMW");

        /// <summary>
        /// ZWL: Currency used when this resource gives cost information
        /// </summary>
        public static Currency  ZWL    { get; }
            = Register("ZWL");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two currencies for equality.
        /// </summary>
        public static Boolean operator == (Currency Currency1, Currency Currency2)
            => Currency1.Equals(Currency2);

        /// <summary>
        /// Compares two currencies for inequality.
        /// </summary>
        public static Boolean operator != (Currency Currency1, Currency Currency2)
            => !Currency1.Equals(Currency2);

        /// <summary>
        /// Compares two currencies.
        /// </summary>
        public static Boolean operator <  (Currency Currency1, Currency Currency2)
            => Currency1.CompareTo(Currency2) < 0;

        /// <summary>
        /// Compares two currencies.
        /// </summary>
        public static Boolean operator <= (Currency Currency1, Currency Currency2)
            => Currency1.CompareTo(Currency2) <= 0;

        /// <summary>
        /// Compares two currencies.
        /// </summary>
        public static Boolean operator >  (Currency Currency1, Currency Currency2)
            => Currency1.CompareTo(Currency2) > 0;

        /// <summary>
        /// Compares two currencies.
        /// </summary>
        public static Boolean operator >= (Currency Currency1, Currency Currency2)
            => Currency1.CompareTo(Currency2) >= 0;

        #endregion

        #region IComparable<Currency> Members

        /// <summary>
        /// Compares two currencies.
        /// </summary>
        /// <param name="Object">A currency to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Currency currency
                   ? CompareTo(currency)
                   : throw new ArgumentException("The given object is not a currency!", nameof(Object));

        /// <summary>
        /// Compares two currencies.
        /// </summary>
        /// <param name="Currency">A currency to compare with.</param>
        public Int32 CompareTo(Currency Currency)
            => String.Compare(InternalId, Currency.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<Currency> Members

        /// <summary>
        /// Compares two currencies for equality.
        /// </summary>
        /// <param name="Object">A currency to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Currency currency && Equals(currency);

        /// <summary>
        /// Compares two currencies for equality.
        /// </summary>
        /// <param name="Currency">A currency to compare with.</param>
        public Boolean Equals(Currency Currency)
            => String.Equals(InternalId, Currency.InternalId, StringComparison.Ordinal);

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

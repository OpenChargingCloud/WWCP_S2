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

using System.Text;
using System.Diagnostics.CodeAnalysis;

using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The DNS-SD TXT record of an S2 Connect endpoint (S2 Connect 1.0.0, "DNS-SD based discovery"):
    /// <c>txtver=1</c> (the alias <c>txtvers</c> is accepted as well), the optional endpoint
    /// name and logo URL, and the pairing and/or long-polling base URL, of which at least one
    /// must be present. The receiver of a service description must use the URL of the TXT
    /// record, never the host and port of the SRV record.
    /// </summary>
    public sealed class S2DNSSDTXTRecord : IEquatable<S2DNSSDTXTRecord>
    {

        #region Data

        /// <summary>
        /// The maximum length of a TXT character-string ("key=value") in bytes of UTF-8.
        /// </summary>
        public const Int32 MaxEntryLength = S2ConnectDefaults.DNSSDTXTMaxValueBytes;

        private static readonly HashSet<String> knownKeys = new (StringComparer.OrdinalIgnoreCase) {
                                                                S2ConnectDefaults.DNSSDTXTVersionKey,
                                                                S2ConnectDefaults.DNSSDTXTVersionKeyAlias,
                                                                S2ConnectDefaults.DNSSDTXTEndpointNameKey,
                                                                S2ConnectDefaults.DNSSDTXTEndpointLogoURLKey,
                                                                S2ConnectDefaults.DNSSDTXTPairingURLKey,
                                                                S2ConnectDefaults.DNSSDTXTLongPollingURLKey
                                                            };

        #endregion

        #region Properties

        /// <summary>
        /// The version of the TXT record specification: "1".
        /// </summary>
        public String                                 TXTVersion           { get; }

        /// <summary>
        /// The optional user-facing name of the endpoint (identical to EndpointDescription.name).
        /// </summary>
        public String?                                EndpointName         { get; }

        /// <summary>
        /// The optional logo URL of the endpoint (identical to EndpointDescription.logoUrl).
        /// </summary>
        public URL?                                   LogoUrl              { get; }

        /// <summary>
        /// The optional base URL of the pairing API of the endpoint.
        /// </summary>
        public S2BaseURL?                             PairingUrl           { get; }

        /// <summary>
        /// The optional base URL of the pairing API supporting long-polling (LAN endpoints only).
        /// </summary>
        public S2BaseURL?                             LongPollingUrl       { get; }

        /// <summary>
        /// Entries with keys this specification does not define, preserved in their order.
        /// </summary>
        public IReadOnlyDictionary<String, String?>   AdditionalEntries    { get; }

        /// <summary>
        /// Whether the endpoint offers a pairing API.
        /// </summary>
        public Boolean                                HasPairingUrl
            => PairingUrl.HasValue;

        /// <summary>
        /// Whether the endpoint offers long-polling.
        /// </summary>
        public Boolean                                HasLongPollingUrl
            => LongPollingUrl.HasValue;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new S2 Connect DNS-SD TXT record.
        /// </summary>
        /// <param name="PairingUrl">The optional base URL of the pairing API.</param>
        /// <param name="LongPollingUrl">The optional base URL of the pairing API supporting long-polling.</param>
        /// <param name="EndpointName">The optional user-facing name of the endpoint.</param>
        /// <param name="LogoUrl">The optional logo URL of the endpoint.</param>
        /// <param name="AdditionalEntries">Optional entries with keys this specification does not define.</param>
        public S2DNSSDTXTRecord(S2BaseURL?                             PairingUrl          = null,
                                S2BaseURL?                             LongPollingUrl      = null,
                                String?                                EndpointName        = null,
                                URL?                                   LogoUrl             = null,
                                IEnumerable<KeyValuePair<String, String?>>?  AdditionalEntries   = null)
        {

            if (!PairingUrl.HasValue && !LongPollingUrl.HasValue)
                throw new ArgumentException("At least one of the pairing URL and the long-polling URL must be given!", nameof(PairingUrl));

            if (PairingUrl.HasValue && String.IsNullOrEmpty(PairingUrl.Value.Value))
                throw new ArgumentException("The pairing URL must not be empty!", nameof(PairingUrl));

            if (LongPollingUrl.HasValue && String.IsNullOrEmpty(LongPollingUrl.Value.Value))
                throw new ArgumentException("The long-polling URL must not be empty!", nameof(LongPollingUrl));

            var additional = new Dictionary<String, String?>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in AdditionalEntries ?? [])
            {

                if (String.IsNullOrEmpty(entry.Key))
                    throw new ArgumentException("An additional TXT key must not be empty!", nameof(AdditionalEntries));

                if (knownKeys.Contains(entry.Key))
                    throw new ArgumentException($"The TXT key '{entry.Key}' is defined by S2 Connect and must be given through its own parameter!", nameof(AdditionalEntries));

                if (entry.Key.Any(character => character < 0x20 || character > 0x7E || character == '='))
                    throw new ArgumentException($"The TXT key '{entry.Key}' must consist of printable US-ASCII characters without '='!", nameof(AdditionalEntries));

                if (!additional.TryAdd(entry.Key, entry.Value))
                    throw new ArgumentException($"The TXT key '{entry.Key}' occurs more than once!", nameof(AdditionalEntries));

            }

            this.TXTVersion         = S2ConnectDefaults.DNSSDTXTVersion;
            this.EndpointName       = EndpointName;
            this.LogoUrl            = LogoUrl;
            this.PairingUrl         = PairingUrl;
            this.LongPollingUrl     = LongPollingUrl;
            this.AdditionalEntries  = additional;

            foreach (var text in ToStrings())
            {
                var length = Encoding.UTF8.GetByteCount(text);
                if (length > MaxEntryLength)
                    throw new ArgumentException($"The TXT entry '{text[..Math.Min(text.Length, 40)]}…' is {length} bytes long, but a DNS character-string must not exceed {MaxEntryLength} bytes!");
            }

        }

        #endregion

        #region Documentation

        // S2 Connect 1.0.0, "DNS-SD based discovery", TXT record:
        //
        //   | Record name    | M/O | Description                                                                   |
        //   |----------------|-----|-------------------------------------------------------------------------------|
        //   | txtver         | M   | Version of this specification of usage of the TXT record. Must be "1".        |
        //   | e_name         | O   | Name of the endpoint (identical to the name property of EndpointDescription). |
        //   | e_logoUrl      | O   | Logo URL of the endpoint (identical to EndpointDescription.logoUrl).          |
        //   | pairingUrl     | O   | The base URL of the pairing API of this endpoint.                             |
        //   | longpollingUrl | O   | The base URL of the pairing API supporting long-polling (LAN only).           |
        //
        // "It is mandatory to provide a value for at least one of the properties pairingUrl and longpollingUrl."
        // "The receiver of the service description must use the URL provided in the TXT record; not the hostname
        //  or IP-address and port associated with the service registry."
        //
        // RFC 6763 §6: every entry is one <character-string> of at most 255 bytes, keys are case-insensitive
        // and only the first occurrence of a key counts. The key "txtvers" (RFC 6763 §6.7 convention) is
        // accepted as an alias of "txtver".

        #endregion


        #region (static) TryParse(Strings, out TXTRecord, out ErrorResponse, Options = null)

        /// <summary>
        /// Try to parse the given TXT character-strings as S2 Connect TXT record.
        /// </summary>
        /// <param name="Strings">The character-strings of a DNS TXT record.</param>
        /// <param name="TXTRecord">The parsed TXT record.</param>
        /// <param name="ErrorResponse">An error message when the parsing failed.</param>
        /// <param name="Options">Optional parser options (AllowInsecureURLs accepts "http://" URLs).</param>
        public static Boolean TryParse(IEnumerable<String>                          Strings,
                                       [NotNullWhen(true)]  out S2DNSSDTXTRecord?   TXTRecord,
                                       [NotNullWhen(false)] out String?             ErrorResponse,
                                       S2ParserOptions?                             Options   = null)
        {

            TXTRecord      = null;
            ErrorResponse  = null;

            ArgumentNullException.ThrowIfNull(Strings);

            var options    = Options ?? S2ParserOptions.Default;
            var entries    = new List<KeyValuePair<String, String?>>();
            var seen       = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

            #region RFC 6763 §6.4: key=value pairs, first occurrence wins, keys case-insensitive

            foreach (var text in Strings)
            {

                if (String.IsNullOrEmpty(text) || text[0] == '=')
                    continue;

                var equals  = text.IndexOf('=');
                var key     = equals < 0 ? text : text[..equals];
                var value   = equals < 0 ? null : text[(equals + 1)..];

                if (seen.Add(key))
                    entries.Add(new KeyValuePair<String, String?>(key, value));

            }

            #endregion

            String? Get(String Key)
                => entries.FirstOrDefault(entry => entry.Key.Equals(Key, StringComparison.OrdinalIgnoreCase)).Value;

            Boolean Has(String Key)
                => seen.Contains(Key);

            #region txtver / txtvers

            var version = Has(S2ConnectDefaults.DNSSDTXTVersionKey)
                              ? Get(S2ConnectDefaults.DNSSDTXTVersionKey)
                              : Has(S2ConnectDefaults.DNSSDTXTVersionKeyAlias)
                                    ? Get(S2ConnectDefaults.DNSSDTXTVersionKeyAlias)
                                    : null;

            if (version is null)
            {
                ErrorResponse = $"The mandatory TXT entry '{S2ConnectDefaults.DNSSDTXTVersionKey}' is missing!";
                return false;
            }

            if (version.Trim() != S2ConnectDefaults.DNSSDTXTVersion)
            {
                ErrorResponse = $"The TXT record version '{version}' is not supported (expected '{S2ConnectDefaults.DNSSDTXTVersion}')!";
                return false;
            }

            #endregion

            #region e_name / e_logoUrl

            var endpointName = Get(S2ConnectDefaults.DNSSDTXTEndpointNameKey);
            if (endpointName is not null && endpointName.Length == 0)
                endpointName = null;

            URL? logoUrl = null;

            var logoText = Get(S2ConnectDefaults.DNSSDTXTEndpointLogoURLKey);
            if (!String.IsNullOrWhiteSpace(logoText))
            {

                if (!URL.TryParse(logoText.Trim(), out var parsedLogoUrl) || parsedLogoUrl.IsNullOrEmpty)
                {
                    ErrorResponse = $"The TXT entry '{S2ConnectDefaults.DNSSDTXTEndpointLogoURLKey}' is not a valid URL!";
                    return false;
                }

                logoUrl = parsedLogoUrl;

            }

            #endregion

            #region pairingUrl / longpollingUrl

            S2BaseURL? pairingUrl      = null;
            S2BaseURL? longPollingUrl  = null;

            var pairingText = Get(S2ConnectDefaults.DNSSDTXTPairingURLKey);
            if (!String.IsNullOrWhiteSpace(pairingText))
            {

                if (!S2BaseURL.TryParse(pairingText, out var parsedPairingUrl, out var urlError, options.AllowInsecureURLs))
                {
                    ErrorResponse = $"The TXT entry '{S2ConnectDefaults.DNSSDTXTPairingURLKey}' is invalid: {urlError}";
                    return false;
                }

                pairingUrl = parsedPairingUrl;

            }

            var longPollingText = Get(S2ConnectDefaults.DNSSDTXTLongPollingURLKey);
            if (!String.IsNullOrWhiteSpace(longPollingText))
            {

                if (!S2BaseURL.TryParse(longPollingText, out var parsedLongPollingUrl, out var urlError, options.AllowInsecureURLs))
                {
                    ErrorResponse = $"The TXT entry '{S2ConnectDefaults.DNSSDTXTLongPollingURLKey}' is invalid: {urlError}";
                    return false;
                }

                longPollingUrl = parsedLongPollingUrl;

            }

            if (!pairingUrl.HasValue && !longPollingUrl.HasValue)
            {
                ErrorResponse = $"At least one of the TXT entries '{S2ConnectDefaults.DNSSDTXTPairingURLKey}' and '{S2ConnectDefaults.DNSSDTXTLongPollingURLKey}' must be present!";
                return false;
            }

            #endregion

            var additional = entries.Where(entry => !knownKeys.Contains(entry.Key)).ToArray();

            try
            {

                TXTRecord = new S2DNSSDTXTRecord(
                                pairingUrl,
                                longPollingUrl,
                                endpointName,
                                logoUrl,
                                additional
                            );

                return true;

            }
            catch (ArgumentException e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(TXT,     out TXTRecord, out ErrorResponse, Options = null)

        /// <summary>
        /// Try to parse the given DNS TXT resource record as S2 Connect TXT record.
        /// </summary>
        /// <param name="TXT">A DNS TXT resource record.</param>
        /// <param name="TXTRecord">The parsed TXT record.</param>
        /// <param name="ErrorResponse">An error message when the parsing failed.</param>
        /// <param name="Options">Optional parser options (AllowInsecureURLs accepts "http://" URLs).</param>
        public static Boolean TryParse(TXT                                          TXT,
                                       [NotNullWhen(true)]  out S2DNSSDTXTRecord?   TXTRecord,
                                       [NotNullWhen(false)] out String?             ErrorResponse,
                                       S2ParserOptions?                             Options   = null)
        {

            ArgumentNullException.ThrowIfNull(TXT);

            return TryParse(TXT.Strings, out TXTRecord, out ErrorResponse, Options);

        }

        #endregion

        #region ToStrings()

        /// <summary>
        /// The TXT character-strings of this record: the version first, then the optional
        /// entries and the additional entries.
        /// </summary>
        public IReadOnlyList<String> ToStrings()
        {

            var strings = new List<String> {
                              $"{S2ConnectDefaults.DNSSDTXTVersionKey}={TXTVersion}"
                          };

            if (EndpointName is not null)
                strings.Add($"{S2ConnectDefaults.DNSSDTXTEndpointNameKey}={EndpointName}");

            if (LogoUrl.HasValue)
                strings.Add($"{S2ConnectDefaults.DNSSDTXTEndpointLogoURLKey}={LogoUrl.Value}");

            if (PairingUrl.HasValue)
                strings.Add($"{S2ConnectDefaults.DNSSDTXTPairingURLKey}={PairingUrl.Value.Value}");

            if (LongPollingUrl.HasValue)
                strings.Add($"{S2ConnectDefaults.DNSSDTXTLongPollingURLKey}={LongPollingUrl.Value.Value}");

            foreach (var entry in AdditionalEntries)
                strings.Add(entry.Value is null
                                ? entry.Key
                                : $"{entry.Key}={entry.Value}");

            return strings;

        }

        #endregion

        #region ToTXT(InstanceName, TimeToLive = null)

        /// <summary>
        /// Create the DNS TXT resource record of the given service instance.
        /// </summary>
        /// <param name="InstanceName">The DNS-SD service instance name (e.g. "myhost._s2connect._tcp.local.").</param>
        /// <param name="TimeToLive">An optional time-to-live (default: 75 minutes, RFC 6762 §10).</param>
        public TXT ToTXT(DNSServiceName  InstanceName,
                         TimeSpan?       TimeToLive   = null)
        {

            ArgumentNullException.ThrowIfNull(InstanceName);

            return new TXT(
                       InstanceName,
                       DNSQueryClasses.IN,
                       TimeToLive ?? MulticastDNS.SharedRecordTimeToLive,
                       ToStrings()
                   );

        }

        #endregion

        #region ToEndpointDescription()

        /// <summary>
        /// The endpoint description announced by this TXT record (a LAN endpoint).
        /// </summary>
        public EndpointDescription ToEndpointDescription()

            => new (EndpointName,
                    LogoUrl,
                    Deployment.LAN);

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two TXT records for equality.
        /// </summary>
        public static Boolean operator == (S2DNSSDTXTRecord? TXTRecord1, S2DNSSDTXTRecord? TXTRecord2)
        {

            if (ReferenceEquals(TXTRecord1, TXTRecord2))
                return true;

            if (TXTRecord1 is null || TXTRecord2 is null)
                return false;

            return TXTRecord1.Equals(TXTRecord2);

        }

        /// <summary>
        /// Compares two TXT records for inequality.
        /// </summary>
        public static Boolean operator != (S2DNSSDTXTRecord? TXTRecord1, S2DNSSDTXTRecord? TXTRecord2)
            => !(TXTRecord1 == TXTRecord2);

        #endregion

        #region IEquatable<S2DNSSDTXTRecord> Members

        /// <summary>
        /// Compares two TXT records for equality.
        /// </summary>
        public override Boolean Equals(Object? Object)
            => Object is S2DNSSDTXTRecord txtRecord &&
                   Equals(txtRecord);

        /// <summary>
        /// Compares two TXT records for equality (same character-strings).
        /// </summary>
        public Boolean Equals(S2DNSSDTXTRecord? TXTRecord)
            => TXTRecord is not null &&
               ToStrings().SequenceEqual(TXTRecord.ToStrings(), StringComparer.Ordinal);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => String.Join('\n', ToStrings()).GetHashCode(StringComparison.Ordinal);

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => String.Join(", ", ToStrings());

        #endregion

    }

}

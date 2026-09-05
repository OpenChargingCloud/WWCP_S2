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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The certificates an S2 Connect node pinned per domain name (PLAN.md §3.5, D13): after a
    /// successful pairing the peer's <c>certificateFingerprint</c> map is pinned against the host of
    /// its pairing or session initiation URL, and every later TLS connection to that host must
    /// present a certificate with one of the pinned fingerprints.
    ///
    /// <para>
    /// Domain names are compared the way the pairing challenge compares them (normalised, without
    /// scheme, port or path), fingerprints in constant time. More than one fingerprint per domain
    /// may be pinned, which is what makes a planned certificate rotation possible: pin the new
    /// fingerprint, roll the server, drop the old one.
    /// </para>
    /// </summary>
    public sealed class CertificatePinStore
    {

        #region Data

        private readonly Lock                                                lockObject  = new();
        private readonly Dictionary<String, List<CertificateFingerprint>>    pins        = [];

        #endregion

        #region Properties

        /// <summary>
        /// The number of pinned domain names.
        /// </summary>
        public Int32 Count
        {
            get
            {
                lock (lockObject)
                    return pins.Count;
            }
        }

        /// <summary>
        /// The pinned domain names.
        /// </summary>
        public IReadOnlyList<String> DomainNames
        {
            get
            {
                lock (lockObject)
                    return [.. pins.Keys];
            }
        }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new, empty certificate pin store.
        /// </summary>
        public CertificatePinStore()
        { }

        #endregion


        #region Pin(DomainName, Fingerprint)

        /// <summary>
        /// Pin the given fingerprint for the given domain name. Pinning the same fingerprint twice
        /// changes nothing; a second, different fingerprint is added (rotation).
        /// </summary>
        /// <param name="DomainName">A domain name (the host of a pairing or session initiation URL).</param>
        /// <param name="Fingerprint">The SHA-256 fingerprint of the certificate to pin.</param>
        /// <returns>Whether the fingerprint was new for this domain name.</returns>
        public Boolean Pin(String                  DomainName,
                           CertificateFingerprint  Fingerprint)
        {

            var key = Normalise(DomainName);

            lock (lockObject)
            {

                if (!pins.TryGetValue(key, out var fingerprints))
                {
                    fingerprints  = [];
                    pins[key]     = fingerprints;
                }

                if (fingerprints.Any(pinned => pinned.ConstantTimeEquals(Fingerprint)))
                    return false;

                fingerprints.Add(Fingerprint);
                return true;

            }

        }

        #endregion

        #region PinAll(DomainName, Fingerprints)

        /// <summary>
        /// Pin every fingerprint of a <c>certificateFingerprint</c> map (the keys name the hash
        /// algorithm; only SHA-256 is defined by S2 Connect and the map is matched case-insensitively).
        /// </summary>
        /// <param name="DomainName">A domain name.</param>
        /// <param name="Fingerprints">A certificate fingerprint map as received during pairing.</param>
        /// <returns>The number of newly pinned fingerprints.</returns>
        public Int32 PinAll(String                                              DomainName,
                            IReadOnlyDictionary<String, CertificateFingerprint>  Fingerprints)
        {

            ArgumentNullException.ThrowIfNull(Fingerprints);

            var pinned = 0;

            foreach (var (algorithm, fingerprint) in Fingerprints)
            {
                if (algorithm.Equals(S2ConnectDefaults.CertificateFingerprintSHA256Key, StringComparison.OrdinalIgnoreCase) &&
                    Pin(DomainName, fingerprint))
                {
                    pinned++;
                }
            }

            return pinned;

        }

        #endregion

        #region IsPinned(DomainName) / TryGetPins(DomainName, out Fingerprints)

        /// <summary>
        /// Whether anything is pinned for the given domain name.
        /// </summary>
        /// <param name="DomainName">A domain name.</param>
        public Boolean IsPinned(String DomainName)
        {
            lock (lockObject)
                return pins.TryGetValue(Normalise(DomainName), out var fingerprints) && fingerprints.Count > 0;
        }

        /// <summary>
        /// The fingerprints pinned for the given domain name.
        /// </summary>
        /// <param name="DomainName">A domain name.</param>
        /// <param name="Fingerprints">The pinned fingerprints.</param>
        public Boolean TryGetPins(String                                        DomainName,
                                  [NotNullWhen(true)] out IReadOnlyList<CertificateFingerprint>?  Fingerprints)
        {

            lock (lockObject)
            {

                if (pins.TryGetValue(Normalise(DomainName), out var fingerprints) && fingerprints.Count > 0)
                {
                    Fingerprints = [.. fingerprints];
                    return true;
                }

                Fingerprints = null;
                return false;

            }

        }

        #endregion

        #region Matches(DomainName, Fingerprint)

        /// <summary>
        /// Whether the given fingerprint is pinned for the given domain name (constant-time
        /// comparison). A domain name with no pins never matches: the caller decides whether an
        /// unpinned host is acceptable (it is during pairing, and it is not afterwards).
        /// </summary>
        /// <param name="DomainName">A domain name.</param>
        /// <param name="Fingerprint">The fingerprint of the presented certificate.</param>
        public Boolean Matches(String                  DomainName,
                               CertificateFingerprint  Fingerprint)
        {

            lock (lockObject)
            {

                if (!pins.TryGetValue(Normalise(DomainName), out var fingerprints))
                    return false;

                var matched = false;

                // Every pin is compared, so the runtime does not reveal which one matched.
                foreach (var pinned in fingerprints)
                    matched |= pinned.ConstantTimeEquals(Fingerprint);

                return matched;

            }

        }

        #endregion

        #region Unpin(DomainName, Fingerprint = null)

        /// <summary>
        /// Remove one fingerprint, or every fingerprint of a domain name (e.g. after unpairing).
        /// </summary>
        /// <param name="DomainName">A domain name.</param>
        /// <param name="Fingerprint">An optional single fingerprint to remove (default: all).</param>
        /// <returns>Whether anything was removed.</returns>
        public Boolean Unpin(String                   DomainName,
                             CertificateFingerprint?  Fingerprint   = null)
        {

            var key = Normalise(DomainName);

            lock (lockObject)
            {

                if (!pins.TryGetValue(key, out var fingerprints))
                    return false;

                if (!Fingerprint.HasValue)
                    return pins.Remove(key);

                var removed = fingerprints.RemoveAll(pinned => pinned.ConstantTimeEquals(Fingerprint.Value)) > 0;

                if (fingerprints.Count == 0)
                    pins.Remove(key);

                return removed;

            }

        }

        #endregion

        #region Clear()

        /// <summary>
        /// Remove every pin.
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
                pins.Clear();
        }

        #endregion


        #region ToJSON() / (static) TryParse(JSON, out PinStore, out ErrorResponse)

        /// <summary>
        /// The JSON representation of this pin store, for persistence alongside the S2 store.
        /// </summary>
        public JObject ToJSON()
        {

            lock (lockObject)
            {

                var json = new JObject();

                foreach (var (domainName, fingerprints) in pins.OrderBy(pin => pin.Key, StringComparer.Ordinal))
                    json.Add(domainName, new JArray(fingerprints.Select(fingerprint => fingerprint.ToHexString())));

                return json;

            }

        }

        /// <summary>
        /// Try to parse a certificate pin store from its JSON representation.
        /// </summary>
        /// <param name="JSON">The JSON representation.</param>
        /// <param name="PinStore">The parsed pin store.</param>
        /// <param name="ErrorResponse">An error message when the parsing failed.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out CertificatePinStore?  PinStore,
                                       [NotNullWhen(false)] out String?               ErrorResponse)
        {

            PinStore       = null;
            ErrorResponse  = null;

            ArgumentNullException.ThrowIfNull(JSON);

            var store = new CertificatePinStore();

            foreach (var property in JSON.Properties())
            {

                if (property.Value is not JArray fingerprints)
                {
                    ErrorResponse = $"The pins of '{property.Name}' are not a JSON array!";
                    return false;
                }

                foreach (var token in fingerprints)
                {

                    if (token.Type != JTokenType.String ||
                        !CertificateFingerprint.TryParse(token.Value<String>() ?? "", out var fingerprint))
                    {
                        ErrorResponse = $"A pinned fingerprint of '{property.Name}' is invalid!";
                        return false;
                    }

                    store.Pin(property.Name, fingerprint);

                }

            }

            PinStore = store;
            return true;

        }

        #endregion

        #region (private static) Normalise(DomainName)

        /// <summary>
        /// Normalise a domain name the way the pairing challenge does (lower case, no trailing dot,
        /// no scheme, port or path).
        /// </summary>
        private static String Normalise(String DomainName)
        {

            ArgumentException.ThrowIfNullOrWhiteSpace(DomainName);

            return ChallengeResponse.NormaliseDomainName(DomainName);

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
        {
            lock (lockObject)
                return $"{pins.Count} pinned domain name(s), {pins.Values.Sum(fingerprints => fingerprints.Count)} fingerprint(s)";
        }

        #endregion

    }

}

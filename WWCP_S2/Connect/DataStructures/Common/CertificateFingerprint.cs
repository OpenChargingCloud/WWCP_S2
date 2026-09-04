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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The SHA-256 fingerprint of an X.509 certificate (the hash of its DER encoding), as used
    /// as input of the LAN challenge-response function (leaf certificate) and pinned for the
    /// self-signed CA certificate of a LAN server. On the wire it is a hexadecimal string,
    /// usually with colons between the bytes; both forms are accepted and compared by value
    /// (S2 Connect, "Challenge response process").
    /// </summary>
    public readonly struct CertificateFingerprint : IEquatable<CertificateFingerprint>
    {

        #region Properties

        /// <summary>
        /// The binary fingerprint (32 bytes for SHA-256).
        /// </summary>
        public ReadOnlyMemory<Byte>  Bytes    { get; }

        /// <summary>
        /// The number of bytes.
        /// </summary>
        public Int32                 Length
            => Bytes.Length;

        #endregion

        #region Constructor(s)

        private CertificateFingerprint(ReadOnlyMemory<Byte> Bytes)
        {
            this.Bytes = Bytes;
        }

        #endregion


        #region Documentation

        // S2 Connect 1.0.0, "Challenge response process":
        //   F: SHA256 fingerprint of the TLS server certificate (i.e. leaf certificate)
        //   SHA256 certificate fingerprints are encoded into a hexadecimal string, and must be decoded as
        //   hexadecimal string before it can be used as input (note that fingerprint strings usually contain
        //   colons to separate bytes).
        //
        // s2-connect-pairing.yml, ConnectionDetails.certificateFingerprint:
        //   A map containing the fingerprints of the CA certificate that is being used by the server for
        //   communication. The key of the map is the hashing algorithm used to create the fingerprint,
        //   the value is the fingerprint itself. The key "SHA256" must always be provided.

        #endregion

        #region (static) FromBytes(Bytes)

        /// <summary>
        /// Create a fingerprint from its binary value.
        /// </summary>
        /// <param name="Bytes">The binary fingerprint (at least one byte).</param>
        public static CertificateFingerprint FromBytes(ReadOnlySpan<Byte> Bytes)
        {

            ArgumentOutOfRangeException.ThrowIfLessThan(Bytes.Length, 1);

            return new CertificateFingerprint(Bytes.ToArray());

        }

        #endregion

        #region (static) FromCertificate(Certificate)

        /// <summary>
        /// Compute the SHA-256 fingerprint of the given certificate (hash of its DER encoding).
        /// </summary>
        /// <param name="Certificate">An X.509 certificate.</param>
        public static CertificateFingerprint FromCertificate(X509Certificate2 Certificate)
            => new (Certificate.GetCertHash(HashAlgorithmName.SHA256));

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given hexadecimal text (with or without colons) as a fingerprint.
        /// </summary>
        /// <param name="Text">A hexadecimal text, e.g. "AB:CD:…" or "abcd…".</param>
        public static CertificateFingerprint Parse(String Text)
        {

            if (TryParse(Text, out var fingerprint))
                return fingerprint;

            throw new ArgumentException($"Invalid hexadecimal text representation of a certificate fingerprint: '{Text}'!", nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text, out Fingerprint)

        /// <summary>
        /// Try to parse the given hexadecimal text (with or without colons or spaces between
        /// the bytes, any case) as a fingerprint.
        /// </summary>
        /// <param name="Text">A hexadecimal text.</param>
        /// <param name="Fingerprint">The parsed fingerprint.</param>
        public static Boolean TryParse(String                                            Text,
                                       [NotNullWhen(true)] out CertificateFingerprint    Fingerprint)
        {

            Fingerprint = default;

            if (String.IsNullOrWhiteSpace(Text))
                return false;

            var hex = Text.Replace(":", "", StringComparison.Ordinal).
                           Replace(" ", "", StringComparison.Ordinal).
                           Trim();

            if (hex.Length == 0 || hex.Length % 2 != 0 || !hex.All(Char.IsAsciiHexDigit))
                return false;

            Fingerprint = new CertificateFingerprint(Convert.FromHexString(hex));
            return true;

        }

        #endregion


        #region ToHexString(WithColons = true)

        /// <summary>
        /// Return the upper-case hexadecimal representation, with a colon between the bytes by default.
        /// </summary>
        /// <param name="WithColons">Whether to separate the bytes with colons.</param>
        public String ToHexString(Boolean WithColons = true)
        {

            var hex = Convert.ToHexString(Bytes.Span);

            if (!WithColons)
                return hex;

            return String.Join(':', Enumerable.Range(0, hex.Length / 2).Select(i => hex.Substring(i * 2, 2)));

        }

        #endregion

        #region ConstantTimeEquals(Fingerprint)

        /// <summary>
        /// Compares two fingerprints in constant time.
        /// </summary>
        /// <param name="Fingerprint">A fingerprint to compare with.</param>
        public Boolean ConstantTimeEquals(CertificateFingerprint Fingerprint)
            => CryptographicOperations.FixedTimeEquals(Bytes.Span, Fingerprint.Bytes.Span);

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two fingerprints for equality.
        /// </summary>
        public static Boolean operator == (CertificateFingerprint Fingerprint1, CertificateFingerprint Fingerprint2)
            => Fingerprint1.Equals(Fingerprint2);

        /// <summary>
        /// Compares two fingerprints for inequality.
        /// </summary>
        public static Boolean operator != (CertificateFingerprint Fingerprint1, CertificateFingerprint Fingerprint2)
            => !Fingerprint1.Equals(Fingerprint2);

        #endregion

        #region IEquatable<CertificateFingerprint> Members

        /// <summary>
        /// Compares two fingerprints for equality.
        /// </summary>
        /// <param name="Object">A fingerprint to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is CertificateFingerprint fingerprint && Equals(fingerprint);

        /// <summary>
        /// Compares two fingerprints for equality (constant time).
        /// </summary>
        /// <param name="Fingerprint">A fingerprint to compare with.</param>
        public Boolean Equals(CertificateFingerprint Fingerprint)
            => ConstantTimeEquals(Fingerprint);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {

            var hash = new HashCode();
            hash.AddBytes(Bytes.Span);
            return hash.ToHashCode();

        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return the colon-separated upper-case hexadecimal representation.
        /// </summary>
        public override String ToString()
            => Bytes.Length == 0 ? "" : ToHexString();

        #endregion

    }

}

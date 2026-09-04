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
using System.Diagnostics.CodeAnalysis;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The nonce of the HMAC challenge-response process (s2-connect-pairing.yml, HmacChallenge):
    /// random binary data of at least 32 bytes from a cryptographically secure generator, standard
    /// Base64 encoded on the wire; it is the key of the HMAC function. The wire text is kept verbatim.
    /// <see cref="ToString"/> is redacted; use <see cref="Value"/> for the wire.
    /// </summary>
    public readonly struct HmacChallenge : IEquatable<HmacChallenge>
    {

        #region Data

        /// <summary>
        /// The number of random bytes of a generated token (at least 32 bytes).
        /// </summary>
        public const Int32  RecommendedBytes  = 32;

        /// <summary>
        /// The minimal number of decoded bytes accepted when parsing (the specification requires at least 32 bytes).
        /// </summary>
        public const Int32  MinimumBytes      = 32;

        #endregion

        #region Properties

        /// <summary>
        /// The standard Base64 text as on the wire.
        /// </summary>
        public String                Value    { get; }

        /// <summary>
        /// The decoded binary value.
        /// </summary>
        public ReadOnlyMemory<Byte>  Bytes    { get; }

        /// <summary>
        /// The number of decoded bytes.
        /// </summary>
        public Int32                 Length
            => Bytes.Length;

        #endregion

        #region Constructor(s)

        private HmacChallenge(String                Text,
                            ReadOnlyMemory<Byte>  Bytes)
        {
            this.Value  = Text;
            this.Bytes  = Bytes;
        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   HmacChallenge:
        //     type:        string
        //     format:      byte
        //     description: Random generated binary data encoded using Base64, used as the challenge for the HMAC
        //                  based challenge response process. The challenge should be send to the other node as
        //                  part of the pairing process. It must have a length of at least 32 bytes.

        #endregion

        #region (static) FromBytes(Bytes)

        /// <summary>
        /// Create a HMAC challenge from the given binary value.
        /// </summary>
        /// <param name="Bytes">The binary value (at least one byte).</param>
        public static HmacChallenge FromBytes(ReadOnlySpan<Byte> Bytes)
        {

            ArgumentOutOfRangeException.ThrowIfLessThan(Bytes.Length, MinimumBytes);

            return new HmacChallenge(Convert.ToBase64String(Bytes), Bytes.ToArray());

        }

        #endregion

        #region (static) NewRandom(NumberOfBytes = 32)

        /// <summary>
        /// Generate a new random HMAC challenge from a cryptographically secure generator.
        /// </summary>
        /// <param name="NumberOfBytes">The number of random bytes (at least 32).</param>
        public static HmacChallenge NewRandom(Int32 NumberOfBytes = RecommendedBytes)
        {

            ArgumentOutOfRangeException.ThrowIfLessThan(NumberOfBytes, RecommendedBytes);

            return FromBytes(RandomNumberGenerator.GetBytes(NumberOfBytes));

        }

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given standard Base64 text as a HMAC challenge.
        /// </summary>
        /// <param name="Text">A Base64 text.</param>
        public static HmacChallenge Parse(String Text)
        {

            if (TryParse(Text, out var token))
                return token;

            throw new ArgumentException("Invalid Base64 text representation of a HMAC challenge!", nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text, out HmacChallenge)

        /// <summary>
        /// Try to parse the given standard Base64 text (with padding, no white space) as a HMAC challenge.
        /// </summary>
        /// <param name="Text">A Base64 text.</param>
        /// <param name="HmacChallenge">The parsed HMAC challenge.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out HmacChallenge    HmacChallenge)
        {

            HmacChallenge = default;

            if (String.IsNullOrEmpty(Text) || Text.Length % 4 != 0)
                return false;

            foreach (var c in Text)
            {
                if (!(Char.IsAsciiLetterOrDigit(c) || c is '+' or '/' or '='))
                    return false;
            }

            var buffer = new Byte[Text.Length / 4 * 3];

            if (!Convert.TryFromBase64String(Text, buffer, out var written) || written < MinimumBytes)
                return false;

            HmacChallenge = new HmacChallenge(Text, buffer.AsMemory(0, written));
            return true;

        }

        #endregion


        #region ConstantTimeEquals(HmacChallenge)

        /// <summary>
        /// Compares the binary values of two HMAC challenges in constant time.
        /// </summary>
        /// <param name="HmacChallenge">A HMAC challenge to compare with.</param>
        public Boolean ConstantTimeEquals(HmacChallenge HmacChallenge)
            => CryptographicOperations.FixedTimeEquals(Bytes.Span, HmacChallenge.Bytes.Span);

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two HMAC challenges for equality.
        /// </summary>
        public static Boolean operator == (HmacChallenge HmacChallenge1, HmacChallenge HmacChallenge2)
            => HmacChallenge1.Equals(HmacChallenge2);

        /// <summary>
        /// Compares two HMAC challenges for inequality.
        /// </summary>
        public static Boolean operator != (HmacChallenge HmacChallenge1, HmacChallenge HmacChallenge2)
            => !HmacChallenge1.Equals(HmacChallenge2);

        #endregion

        #region IEquatable<HmacChallenge> Members

        /// <summary>
        /// Compares two HMAC challenges for equality.
        /// </summary>
        /// <param name="Object">A HMAC challenge to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is HmacChallenge token && Equals(token);

        /// <summary>
        /// Compares the binary values of two HMAC challenges for equality (constant time).
        /// </summary>
        /// <param name="HmacChallenge">A HMAC challenge to compare with.</param>
        public Boolean Equals(HmacChallenge HmacChallenge)
            => ConstantTimeEquals(HmacChallenge);

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
        /// Return a redacted text representation (the token is a secret).
        /// </summary>
        public override String ToString()
            => Value is null ? "" : $"[HMAC challenge, {Length} bytes]";

        #endregion

    }

}

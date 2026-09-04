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
    /// The response of the HMAC challenge-response process (s2-connect-pairing.yml,
    /// HmacChallengeResponse): the HMAC output, standard Base64 encoded on the wire, compared in
    /// constant time. The wire text is kept verbatim. <see cref="ToString"/> is redacted; use
    /// <see cref="Value"/> for the wire.
    /// </summary>
    public readonly struct HmacChallengeResponse : IEquatable<HmacChallengeResponse>
    {

        #region Data

        /// <summary>
        /// The number of random bytes of a generated token (at least 32 bytes).
        /// </summary>
        public const Int32  RecommendedBytes  = 32;

        /// <summary>
        /// The minimal number of decoded bytes accepted when parsing.
        /// </summary>
        public const Int32  MinimumBytes      = 1;

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

        private HmacChallengeResponse(String                Text,
                            ReadOnlyMemory<Byte>  Bytes)
        {
            this.Value  = Text;
            this.Bytes  = Bytes;
        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   HmacChallengeResponse:
        //     type:        string
        //     description: The Base64 encoded response to the challenge that was calculated by the endpoint.
        //     format:      byte

        #endregion

        #region (static) FromBytes(Bytes)

        /// <summary>
        /// Create a HMAC challenge response from the given binary value.
        /// </summary>
        /// <param name="Bytes">The binary value (at least one byte).</param>
        public static HmacChallengeResponse FromBytes(ReadOnlySpan<Byte> Bytes)
        {

            ArgumentOutOfRangeException.ThrowIfLessThan(Bytes.Length, MinimumBytes);

            return new HmacChallengeResponse(Convert.ToBase64String(Bytes), Bytes.ToArray());

        }

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given standard Base64 text as a HMAC challenge response.
        /// </summary>
        /// <param name="Text">A Base64 text.</param>
        public static HmacChallengeResponse Parse(String Text)
        {

            if (TryParse(Text, out var token))
                return token;

            throw new ArgumentException("Invalid Base64 text representation of a HMAC challenge response!", nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text, out HmacChallengeResponse)

        /// <summary>
        /// Try to parse the given standard Base64 text (with padding, no white space) as a HMAC challenge response.
        /// </summary>
        /// <param name="Text">A Base64 text.</param>
        /// <param name="HmacChallengeResponse">The parsed HMAC challenge response.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out HmacChallengeResponse    HmacChallengeResponse)
        {

            HmacChallengeResponse = default;

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

            HmacChallengeResponse = new HmacChallengeResponse(Text, buffer.AsMemory(0, written));
            return true;

        }

        #endregion


        #region ConstantTimeEquals(HmacChallengeResponse)

        /// <summary>
        /// Compares the binary values of two HMAC challenge responses in constant time.
        /// </summary>
        /// <param name="HmacChallengeResponse">A HMAC challenge response to compare with.</param>
        public Boolean ConstantTimeEquals(HmacChallengeResponse HmacChallengeResponse)
            => CryptographicOperations.FixedTimeEquals(Bytes.Span, HmacChallengeResponse.Bytes.Span);

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two HMAC challenge responses for equality.
        /// </summary>
        public static Boolean operator == (HmacChallengeResponse HmacChallengeResponse1, HmacChallengeResponse HmacChallengeResponse2)
            => HmacChallengeResponse1.Equals(HmacChallengeResponse2);

        /// <summary>
        /// Compares two HMAC challenge responses for inequality.
        /// </summary>
        public static Boolean operator != (HmacChallengeResponse HmacChallengeResponse1, HmacChallengeResponse HmacChallengeResponse2)
            => !HmacChallengeResponse1.Equals(HmacChallengeResponse2);

        #endregion

        #region IEquatable<HmacChallengeResponse> Members

        /// <summary>
        /// Compares two HMAC challenge responses for equality.
        /// </summary>
        /// <param name="Object">A HMAC challenge response to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is HmacChallengeResponse token && Equals(token);

        /// <summary>
        /// Compares the binary values of two HMAC challenge responses for equality (constant time).
        /// </summary>
        /// <param name="HmacChallengeResponse">A HMAC challenge response to compare with.</param>
        public Boolean Equals(HmacChallengeResponse HmacChallengeResponse)
            => ConstantTimeEquals(HmacChallengeResponse);

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
            => Value is null ? "" : $"[HMAC challenge response, {Length} bytes]";

        #endregion

    }

}

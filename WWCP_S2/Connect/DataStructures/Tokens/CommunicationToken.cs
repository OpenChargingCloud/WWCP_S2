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
    /// The single-use, short-lived token with which a communication client authenticates its
    /// WebSocket upgrade request (s2-connect-common.yml, CommunicationToken): random binary data of
    /// at least 32 bytes, standard Base64 encoded on the wire, valid for at most 30 seconds. The wire
    /// text is kept verbatim. <see cref="ToString"/> is redacted; use <see cref="Value"/> for the
    /// Authorization header.
    /// </summary>
    public readonly struct CommunicationToken : IEquatable<CommunicationToken>
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

        private CommunicationToken(String                Text,
                            ReadOnlyMemory<Byte>  Bytes)
        {
            this.Value  = Text;
            this.Bytes  = Bytes;
        }

        #endregion


        #region Documentation

        // s2-connect-common.yml
        //   CommunicationToken:
        //     description: Token that is used to authorize the communication channel (e.g. WebSocket) between the
        //                  nodes. It is obtained by calling the requestToken endpoint and is valid for a single
        //                  connection to the S2 message communication channel. This token is valid for maximum
        //                  30 seconds, and should have a minimum length of 32 bytes.
        //     type:        string
        //     format:      byte

        #endregion

        #region (static) FromBytes(Bytes)

        /// <summary>
        /// Create a communication token from the given binary value.
        /// </summary>
        /// <param name="Bytes">The binary value (at least one byte).</param>
        public static CommunicationToken FromBytes(ReadOnlySpan<Byte> Bytes)
        {

            ArgumentOutOfRangeException.ThrowIfLessThan(Bytes.Length, MinimumBytes);

            return new CommunicationToken(Convert.ToBase64String(Bytes), Bytes.ToArray());

        }

        #endregion

        #region (static) NewRandom(NumberOfBytes = 32)

        /// <summary>
        /// Generate a new random communication token from a cryptographically secure generator.
        /// </summary>
        /// <param name="NumberOfBytes">The number of random bytes (at least 32).</param>
        public static CommunicationToken NewRandom(Int32 NumberOfBytes = RecommendedBytes)
        {

            ArgumentOutOfRangeException.ThrowIfLessThan(NumberOfBytes, RecommendedBytes);

            return FromBytes(RandomNumberGenerator.GetBytes(NumberOfBytes));

        }

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given standard Base64 text as a communication token.
        /// </summary>
        /// <param name="Text">A Base64 text.</param>
        public static CommunicationToken Parse(String Text)
        {

            if (TryParse(Text, out var token))
                return token;

            throw new ArgumentException("Invalid Base64 text representation of a communication token!", nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text, out CommunicationToken)

        /// <summary>
        /// Try to parse the given standard Base64 text (with padding, no white space) as a communication token.
        /// </summary>
        /// <param name="Text">A Base64 text.</param>
        /// <param name="CommunicationToken">The parsed communication token.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out CommunicationToken    CommunicationToken)
        {

            CommunicationToken = default;

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

            CommunicationToken = new CommunicationToken(Text, buffer.AsMemory(0, written));
            return true;

        }

        #endregion


        #region ConstantTimeEquals(CommunicationToken)

        /// <summary>
        /// Compares the binary values of two communication tokens in constant time.
        /// </summary>
        /// <param name="CommunicationToken">A communication token to compare with.</param>
        public Boolean ConstantTimeEquals(CommunicationToken CommunicationToken)
            => CryptographicOperations.FixedTimeEquals(Bytes.Span, CommunicationToken.Bytes.Span);

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two communication tokens for equality.
        /// </summary>
        public static Boolean operator == (CommunicationToken CommunicationToken1, CommunicationToken CommunicationToken2)
            => CommunicationToken1.Equals(CommunicationToken2);

        /// <summary>
        /// Compares two communication tokens for inequality.
        /// </summary>
        public static Boolean operator != (CommunicationToken CommunicationToken1, CommunicationToken CommunicationToken2)
            => !CommunicationToken1.Equals(CommunicationToken2);

        #endregion

        #region IEquatable<CommunicationToken> Members

        /// <summary>
        /// Compares two communication tokens for equality.
        /// </summary>
        /// <param name="Object">A communication token to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is CommunicationToken token && Equals(token);

        /// <summary>
        /// Compares the binary values of two communication tokens for equality (constant time).
        /// </summary>
        /// <param name="CommunicationToken">A communication token to compare with.</param>
        public Boolean Equals(CommunicationToken CommunicationToken)
            => ConstantTimeEquals(CommunicationToken);

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
            => Value is null ? "" : $"[communication token, {Length} bytes]";

        #endregion

    }

}

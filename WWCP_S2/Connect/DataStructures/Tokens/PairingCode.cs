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

using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The pairing code the end user copies from the Responder node to the Initiator node:
    /// the node ID alias (when the endpoint hosts more than one node), a dash, and the secret
    /// pairing token; or just the pairing token (S2 Connect, "The pairing token, the node ID
    /// alias and the pairing code"). <see cref="ToString"/> is redacted; use <see cref="Value"/>
    /// to display the code.
    /// </summary>
    public readonly partial struct PairingCode : IEquatable<PairingCode>
    {

        #region Data

        [GeneratedRegex(S2ConnectDefaults.PairingCodeRegExpr, RegexOptions.CultureInvariant)]
        private static partial Regex CodeRegExpr();

        #endregion

        #region Properties

        /// <summary>
        /// The optional node ID alias (the part before the dash).
        /// </summary>
        public NodeIdAlias?  NodeIdAlias     { get; }

        /// <summary>
        /// The secret pairing token (the part after the dash, or the whole code).
        /// </summary>
        public PairingToken  PairingToken    { get; }

        /// <summary>
        /// The complete pairing code as the end user types it: "[alias]-[token]" or "[token]".
        /// </summary>
        public String        Value
            => NodeIdAlias.HasValue
                   ? $"{NodeIdAlias.Value}-{PairingToken.Value}"
                   : PairingToken.Value ?? "";

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new pairing code.
        /// </summary>
        /// <param name="PairingToken">The secret pairing token.</param>
        /// <param name="NodeIdAlias">The optional node ID alias.</param>
        public PairingCode(PairingToken  PairingToken,
                           NodeIdAlias?  NodeIdAlias   = null)
        {
            this.PairingToken  = PairingToken;
            this.NodeIdAlias   = NodeIdAlias;
        }

        #endregion


        #region Documentation

        // S2 Connect 1.0.0, "The pairing token, the node ID alias and the pairing code":
        //
        //   When no node ID alias is used (i.e. the endpoint only contains one node):
        //     [pairing code] = [pairing token]
        //   When a node ID alias ID is used:
        //     [pairing code] = [node ID alias]-[pairing token]
        //
        //   Alternatively, the pairing code can be validated with the following regular expression:
        //     ^([0-9a-zA-Z]+-)?[0-9a-zA-Z]{4,}$
        //
        //   Due to its format the Initiator node can easily extract the node ID alias and the pairing
        //   token from the pairing code by splitting the string at the dash.

        #endregion

        #region (static) IsValid(Text)

        /// <summary>
        /// Whether the given text is a valid pairing code.
        /// </summary>
        /// <param name="Text">A text.</param>
        public static Boolean IsValid(String? Text)
            => Text is not null && CodeRegExpr().IsMatch(Text);

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a pairing code.
        /// </summary>
        /// <param name="Text">A pairing code as typed by the end user.</param>
        public static PairingCode Parse(String Text)
        {

            if (TryParse(Text, out var code))
                return code;

            throw new ArgumentException("Invalid pairing code!", nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text, out PairingCode)

        /// <summary>
        /// Try to parse the given text as a pairing code: it is split at the (single) dash into
        /// the node ID alias and the pairing token.
        /// </summary>
        /// <param name="Text">A pairing code as typed by the end user.</param>
        /// <param name="PairingCode">The parsed pairing code.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out PairingCode    PairingCode)
        {

            PairingCode = default;

            if (!IsValid(Text))
                return false;

            var dash = Text.IndexOf('-', StringComparison.Ordinal);

            if (dash < 0)
            {

                if (!PairingToken.TryParse(Text, out var token))
                    return false;

                PairingCode = new PairingCode(token);
                return true;

            }

            if (!Connect.NodeIdAlias.TryParse(Text[..dash], out var alias))
                return false;

            if (!PairingToken.TryParse(Text[(dash + 1)..], out var pairingToken))
                return false;

            PairingCode = new PairingCode(pairingToken, alias);
            return true;

        }

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two pairing codes for equality.
        /// </summary>
        public static Boolean operator == (PairingCode PairingCode1, PairingCode PairingCode2)
            => PairingCode1.Equals(PairingCode2);

        /// <summary>
        /// Compares two pairing codes for inequality.
        /// </summary>
        public static Boolean operator != (PairingCode PairingCode1, PairingCode PairingCode2)
            => !PairingCode1.Equals(PairingCode2);

        #endregion

        #region IEquatable<PairingCode> Members

        /// <summary>
        /// Compares two pairing codes for equality.
        /// </summary>
        /// <param name="Object">A pairing code to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PairingCode code && Equals(code);

        /// <summary>
        /// Compares two pairing codes for equality.
        /// </summary>
        /// <param name="PairingCode">A pairing code to compare with.</param>
        public Boolean Equals(PairingCode PairingCode)
            => Nullable.Equals(NodeIdAlias, PairingCode.NodeIdAlias) &&
               PairingToken.Equals(PairingCode.PairingToken);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => HashCode.Combine(NodeIdAlias, PairingToken);

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a redacted text representation (the pairing token part is a secret).
        /// </summary>
        public override String ToString()
            => NodeIdAlias.HasValue
                   ? $"{NodeIdAlias.Value}-{PairingToken}"
                   : PairingToken.ToString();

        #endregion

    }

}

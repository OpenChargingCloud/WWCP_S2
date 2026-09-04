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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// A short identifier of a node, unique only within its endpoint, which the end user may
    /// have to type in as the first part of a pairing code (S2 Connect, "The pairing token,
    /// the node ID alias and the pairing code"). Letters and digits only; not secret.
    /// </summary>
    public readonly partial struct NodeIdAlias : IId,
                                                 IEquatable<NodeIdAlias>,
                                                 IComparable<NodeIdAlias>
    {

        #region Data

        [GeneratedRegex(S2ConnectDefaults.NodeIdAliasRegExpr, RegexOptions.CultureInvariant)]
        private static partial Regex AliasRegExpr();

        #endregion

        #region Properties

        /// <summary>
        /// The text value of the alias.
        /// </summary>
        public String   Value               { get; }

        /// <summary>
        /// Indicates whether this alias is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => Value.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this alias is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => Value.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the alias.
        /// </summary>
        public UInt64   Length
            => (UInt64) (Value?.Length ?? 0);

        #endregion

        #region Constructor(s)

        private NodeIdAlias(String Text)
        {
            this.Value = Text;
        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml
        //   NodeIdAlias:
        //     description: A identifier of the node which is unique for the context of the Endpoint. It is used
        //                  as a short identifier (since the user might have to type it in manually) for the node,
        //                  which can be used to lookup the actual nodeId.
        //     type:        string
        //     pattern:     "^[0-9a-zA-Z]+$"
        //     example:     "A0"

        #endregion

        #region (static) IsValid(Text)

        /// <summary>
        /// Whether the given text is a valid node ID alias: at least one letter or digit, nothing else.
        /// </summary>
        /// <param name="Text">A text.</param>
        public static Boolean IsValid(String? Text)
            => Text is not null && AliasRegExpr().IsMatch(Text);

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a node ID alias.
        /// </summary>
        /// <param name="Text">A text representation of a node ID alias.</param>
        public static NodeIdAlias Parse(String Text)
        {

            if (TryParse(Text, out var alias))
                return alias;

            throw new ArgumentException($"Invalid text representation of a node ID alias: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a node ID alias.
        /// </summary>
        /// <param name="Text">A text representation of a node ID alias.</param>
        public static NodeIdAlias? TryParse(String Text)
        {

            if (TryParse(Text, out var alias))
                return alias;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out NodeIdAlias)

        /// <summary>
        /// Try to parse the given text as a node ID alias.
        /// </summary>
        /// <param name="Text">A text representation of a node ID alias.</param>
        /// <param name="NodeIdAlias">The parsed node ID alias.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out NodeIdAlias    NodeIdAlias)
        {

            if (IsValid(Text))
            {
                NodeIdAlias = new NodeIdAlias(Text);
                return true;
            }

            NodeIdAlias = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this node ID alias.
        /// </summary>
        public NodeIdAlias Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two node ID aliases for equality.
        /// </summary>
        public static Boolean operator == (NodeIdAlias NodeIdAlias1, NodeIdAlias NodeIdAlias2)
            => NodeIdAlias1.Equals(NodeIdAlias2);

        /// <summary>
        /// Compares two node ID aliases for inequality.
        /// </summary>
        public static Boolean operator != (NodeIdAlias NodeIdAlias1, NodeIdAlias NodeIdAlias2)
            => !NodeIdAlias1.Equals(NodeIdAlias2);

        /// <summary>
        /// Compares two node ID aliases.
        /// </summary>
        public static Boolean operator <  (NodeIdAlias NodeIdAlias1, NodeIdAlias NodeIdAlias2)
            => NodeIdAlias1.CompareTo(NodeIdAlias2) < 0;

        /// <summary>
        /// Compares two node ID aliases.
        /// </summary>
        public static Boolean operator <= (NodeIdAlias NodeIdAlias1, NodeIdAlias NodeIdAlias2)
            => NodeIdAlias1.CompareTo(NodeIdAlias2) <= 0;

        /// <summary>
        /// Compares two node ID aliases.
        /// </summary>
        public static Boolean operator >  (NodeIdAlias NodeIdAlias1, NodeIdAlias NodeIdAlias2)
            => NodeIdAlias1.CompareTo(NodeIdAlias2) > 0;

        /// <summary>
        /// Compares two node ID aliases.
        /// </summary>
        public static Boolean operator >= (NodeIdAlias NodeIdAlias1, NodeIdAlias NodeIdAlias2)
            => NodeIdAlias1.CompareTo(NodeIdAlias2) >= 0;

        #endregion

        #region IComparable<NodeIdAlias> Members

        /// <summary>
        /// Compares two node ID aliases.
        /// </summary>
        /// <param name="Object">A node ID alias to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is NodeIdAlias alias
                   ? CompareTo(alias)
                   : throw new ArgumentException("The given object is not a node ID alias!", nameof(Object));

        /// <summary>
        /// Compares two node ID aliases.
        /// </summary>
        /// <param name="NodeIdAlias">A node ID alias to compare with.</param>
        public Int32 CompareTo(NodeIdAlias NodeIdAlias)
            => String.Compare(Value, NodeIdAlias.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<NodeIdAlias> Members

        /// <summary>
        /// Compares two node ID aliases for equality.
        /// </summary>
        /// <param name="Object">A node ID alias to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is NodeIdAlias alias && Equals(alias);

        /// <summary>
        /// Compares two node ID aliases for equality (case-sensitive, as the schema pattern
        /// distinguishes upper and lower case letters).
        /// </summary>
        /// <param name="NodeIdAlias">A node ID alias to compare with.</param>
        public Boolean Equals(NodeIdAlias NodeIdAlias)
            => String.Equals(Value, NodeIdAlias.Value, StringComparison.Ordinal);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => Value?.GetHashCode(StringComparison.Ordinal) ?? 0;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => Value ?? "";

        #endregion

    }

}

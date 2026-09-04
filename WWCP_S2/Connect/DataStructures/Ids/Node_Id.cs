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

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// Extension methods for node identifications.
    /// </summary>
    public static class NodeIdExtensions
    {

        /// <summary>
        /// Indicates whether this node identification is null or empty.
        /// </summary>
        /// <param name="NodeId">A node identification.</param>
        public static Boolean IsNullOrEmpty(this Node_Id? NodeId)
            => !NodeId.HasValue || NodeId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this node identification is NOT null or empty.
        /// </summary>
        /// <param name="NodeId">A node identification.</param>
        public static Boolean IsNotNullOrEmpty(this Node_Id? NodeId)
            => NodeId.HasValue && NodeId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The globally unique identification of an S2 Connect node (a CEM or RM instance)
    /// in UUID format (S2 Connect, "Terms and definitions": "Node ID: A globally unique
    /// identifier for a node in the UUID format").
    /// </summary>
    public readonly struct Node_Id : IId,
                                     IEquatable<Node_Id>,
                                     IComparable<Node_Id>
    {

        #region Properties

        /// <summary>
        /// The UUID value of the node identification.
        /// </summary>
        public Guid     Value               { get; }

        /// <summary>
        /// Indicates whether this identification is the empty UUID.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => Value == Guid.Empty;

        /// <summary>
        /// Indicates whether this identification is NOT the empty UUID.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => Value != Guid.Empty;

        /// <summary>
        /// The length of the text representation (36 characters).
        /// </summary>
        public UInt64   Length
            => 36;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new node identification based on the given UUID.
        /// </summary>
        /// <param name="Value">A UUID.</param>
        public Node_Id(Guid Value)
        {
            this.Value = Value;
        }

        #endregion


        #region Documentation

        // s2-connect-common.yml
        //   NodeId:
        //     description: Unique identifier of the node
        //     type:        string
        //     format:      uuid

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) node identification.
        /// </summary>
        public static Node_Id NewRandom
            => new (Guid.CreateVersion7());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a node identification.
        /// </summary>
        /// <param name="Text">A text representation of a node identification (canonical hyphenated UUID).</param>
        public static Node_Id Parse(String Text)
        {

            if (TryParse(Text, out var nodeId))
                return nodeId;

            throw new ArgumentException($"Invalid text representation of a node identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a node identification.
        /// </summary>
        /// <param name="Text">A text representation of a node identification.</param>
        public static Node_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var nodeId))
                return nodeId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out NodeId)

        /// <summary>
        /// Try to parse the given text as a node identification. Only the canonical
        /// hyphenated form (8-4-4-4-12 hexadecimal digits, any case) is accepted.
        /// </summary>
        /// <param name="Text">A text representation of a node identification.</param>
        /// <param name="NodeId">The parsed node identification.</param>
        public static Boolean TryParse(String                              Text,
                                       [NotNullWhen(true)] out Node_Id     NodeId)
        {

            if (Guid.TryParseExact(Text, "D", out var guid))
            {
                NodeId = new Node_Id(guid);
                return true;
            }

            NodeId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this node identification.
        /// </summary>
        public Node_Id Clone()
            => new (Value);

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two node identifications for equality.
        /// </summary>
        public static Boolean operator == (Node_Id NodeId1, Node_Id NodeId2)
            => NodeId1.Equals(NodeId2);

        /// <summary>
        /// Compares two node identifications for inequality.
        /// </summary>
        public static Boolean operator != (Node_Id NodeId1, Node_Id NodeId2)
            => !NodeId1.Equals(NodeId2);

        /// <summary>
        /// Compares two node identifications.
        /// </summary>
        public static Boolean operator <  (Node_Id NodeId1, Node_Id NodeId2)
            => NodeId1.CompareTo(NodeId2) < 0;

        /// <summary>
        /// Compares two node identifications.
        /// </summary>
        public static Boolean operator <= (Node_Id NodeId1, Node_Id NodeId2)
            => NodeId1.CompareTo(NodeId2) <= 0;

        /// <summary>
        /// Compares two node identifications.
        /// </summary>
        public static Boolean operator >  (Node_Id NodeId1, Node_Id NodeId2)
            => NodeId1.CompareTo(NodeId2) > 0;

        /// <summary>
        /// Compares two node identifications.
        /// </summary>
        public static Boolean operator >= (Node_Id NodeId1, Node_Id NodeId2)
            => NodeId1.CompareTo(NodeId2) >= 0;

        #endregion

        #region IComparable<Node_Id> Members

        /// <summary>
        /// Compares two node identifications.
        /// </summary>
        /// <param name="Object">A node identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Node_Id nodeId
                   ? CompareTo(nodeId)
                   : throw new ArgumentException("The given object is not a node identification!", nameof(Object));

        /// <summary>
        /// Compares two node identifications.
        /// </summary>
        /// <param name="NodeId">A node identification to compare with.</param>
        public Int32 CompareTo(Node_Id NodeId)
            => Value.CompareTo(NodeId.Value);

        #endregion

        #region IEquatable<Node_Id> Members

        /// <summary>
        /// Compares two node identifications for equality.
        /// </summary>
        /// <param name="Object">A node identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Node_Id nodeId && Equals(nodeId);

        /// <summary>
        /// Compares two node identifications for equality.
        /// </summary>
        /// <param name="NodeId">A node identification to compare with.</param>
        public Boolean Equals(Node_Id NodeId)
            => Value.Equals(NodeId.Value);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => Value.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return the canonical lower-case hyphenated text representation of this UUID.
        /// </summary>
        public override String ToString()
            => Value.ToString("D");

        #endregion

    }

}

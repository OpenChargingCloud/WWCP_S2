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

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// How a requestPairing request addresses the targeted node at the pairing server: by
    /// its node id, by its node id alias (from the pairing code), or not at all when the
    /// endpoint represents only one node. Node id and alias are never given together
    /// (s2-connect-pairing.yml, /requestPairing: "The (optional) properties nodeId and
    /// nodeIdAlias may never be used at the same time. When the client knows the NodeId ...
    /// it must provide a value for nodeId (and not nodeIdAlias).").
    /// </summary>
    public readonly struct PairingTarget : IEquatable<PairingTarget>
    {

        #region Properties

        /// <summary>
        /// The node identification of the targeted node, when known.
        /// </summary>
        public Node_Id?      NodeId         { get; }

        /// <summary>
        /// The node id alias of the targeted node, when only the alias is known.
        /// </summary>
        public NodeIdAlias?  NodeIdAlias    { get; }

        /// <summary>
        /// Whether neither a node id nor an alias is given (the endpoint must represent exactly one node).
        /// </summary>
        public Boolean       IsAny
            => !NodeId.HasValue && !NodeIdAlias.HasValue;

        #endregion

        #region Constructor(s)

        private PairingTarget(Node_Id?      NodeId,
                              NodeIdAlias?  NodeIdAlias)
        {
            this.NodeId       = NodeId;
            this.NodeIdAlias  = NodeIdAlias;
        }

        #endregion


        #region (static) Any / ByNodeId(NodeId) / ByAlias(NodeIdAlias) / From(NodeId, NodeIdAlias)

        /// <summary>
        /// Address the only node of the endpoint.
        /// </summary>
        public static PairingTarget Any
            => new (null, null);

        /// <summary>
        /// Address the node with the given node id.
        /// </summary>
        /// <param name="NodeId">The node identification.</param>
        public static PairingTarget ByNodeId(Node_Id NodeId)
            => new (NodeId, null);

        /// <summary>
        /// Address the node with the given alias.
        /// </summary>
        /// <param name="NodeIdAlias">The node id alias.</param>
        public static PairingTarget ByAlias(NodeIdAlias NodeIdAlias)
            => new (null, NodeIdAlias);

        /// <summary>
        /// Create a target from optional node id and alias; both together are an error.
        /// </summary>
        /// <param name="NodeId">An optional node identification.</param>
        /// <param name="NodeIdAlias">An optional node id alias.</param>
        public static PairingTarget From(Node_Id?      NodeId,
                                         NodeIdAlias?  NodeIdAlias)
        {

            if (NodeId.HasValue && NodeIdAlias.HasValue)
                throw new ArgumentException("The properties nodeId and nodeIdAlias may never be used at the same time!");

            return new PairingTarget(NodeId, NodeIdAlias);

        }

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two pairing targets for equality.
        /// </summary>
        public static Boolean operator == (PairingTarget PairingTarget1, PairingTarget PairingTarget2)
            => PairingTarget1.Equals(PairingTarget2);

        /// <summary>
        /// Compares two pairing targets for inequality.
        /// </summary>
        public static Boolean operator != (PairingTarget PairingTarget1, PairingTarget PairingTarget2)
            => !PairingTarget1.Equals(PairingTarget2);

        #endregion

        #region IEquatable<PairingTarget> Members

        /// <summary>
        /// Compares two pairing targets for equality.
        /// </summary>
        /// <param name="Object">A pairing target to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PairingTarget target && Equals(target);

        /// <summary>
        /// Compares two pairing targets for equality.
        /// </summary>
        /// <param name="PairingTarget">A pairing target to compare with.</param>
        public Boolean Equals(PairingTarget PairingTarget)
            => Nullable.Equals(NodeId,      PairingTarget.NodeId) &&
               Nullable.Equals(NodeIdAlias, PairingTarget.NodeIdAlias);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => HashCode.Combine(NodeId, NodeIdAlias);

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => NodeId.HasValue      ? $"node {NodeId.Value}"
             : NodeIdAlias.HasValue ? $"alias {NodeIdAlias.Value}"
             :                        "the only node";

        #endregion

    }

}

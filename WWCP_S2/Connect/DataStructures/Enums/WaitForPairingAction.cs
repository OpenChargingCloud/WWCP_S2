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
    /// Extension methods for long-polling actions.
    /// </summary>
    public static class WaitForPairingActionExtensions
    {

        /// <summary>
        /// Indicates whether this long-polling action is null or empty.
        /// </summary>
        /// <param name="WaitForPairingAction">A long-polling action.</param>
        public static Boolean IsNullOrEmpty(this WaitForPairingAction? WaitForPairingAction)
            => !WaitForPairingAction.HasValue || WaitForPairingAction.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this long-polling action is NOT null or empty.
        /// </summary>
        /// <param name="WaitForPairingAction">A long-polling action.</param>
        public static Boolean IsNotNullOrEmpty(this WaitForPairingAction? WaitForPairingAction)
            => WaitForPairingAction.HasValue && WaitForPairingAction.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The action a long-polling server asks a constrained LAN client to perform for one of its nodes (waitForPairing response).
    /// </summary>
    public readonly struct WaitForPairingAction : IS2PredefinedString,
                                         IId,
                                         IEquatable<WaitForPairingAction>,
                                         IComparable<WaitForPairingAction>
    {

        #region Data

        private readonly static Dictionary<String, WaitForPairingAction>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this long-polling action is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this long-polling action is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the long-polling action.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All long-polling actions defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<WaitForPairingAction> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new long-polling action based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a long-polling action.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private WaitForPairingAction(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // s2-connect-pairing.yml, /waitForPairing 200 response item action: enum ["sendNodeDescription", "preparePairing", "cancelPreparePairing", "requestPairing"]

        #endregion

        #region (private static) Register(Text)

        private static WaitForPairingAction Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new WaitForPairingAction(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a long-polling action.
        /// </summary>
        /// <param name="Text">A text representation of a long-polling action.</param>
        public static WaitForPairingAction Parse(String Text)
        {

            if (TryParse(Text, out var waitForPairingAction))
                return waitForPairingAction;

            throw new ArgumentException($"Invalid text representation of a long-polling action: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a long-polling action.
        /// </summary>
        /// <param name="Text">A text representation of a long-polling action.</param>
        public static WaitForPairingAction? TryParse(String Text)
        {

            if (TryParse(Text, out var waitForPairingAction))
                return waitForPairingAction;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out WaitForPairingAction)

        /// <summary>
        /// Try to parse the given text as a long-polling action. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a long-polling action.</param>
        /// <param name="WaitForPairingAction">The parsed long-polling action.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out WaitForPairingAction    WaitForPairingAction)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out WaitForPairingAction))
                    WaitForPairingAction = new WaitForPairingAction(Text, false);

                return true;

            }

            WaitForPairingAction = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this long-polling action.
        /// </summary>
        public WaitForPairingAction Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// sendNodeDescription: Include the NodeDescription and EndpointDescription of the node in the next request.
        /// </summary>
        public static WaitForPairingAction  SendNodeDescription    { get; }
            = Register("sendNodeDescription");

        /// <summary>
        /// preparePairing: Prepare pairing for the node, e.g. show its pairing token in the user interface.
        /// </summary>
        public static WaitForPairingAction  PreparePairing    { get; }
            = Register("preparePairing");

        /// <summary>
        /// cancelPreparePairing: Cancel the previous prepare-pairing signal for the node.
        /// </summary>
        public static WaitForPairingAction  CancelPreparePairing    { get; }
            = Register("cancelPreparePairing");

        /// <summary>
        /// requestPairing: Initiate the pairing for the node by calling requestPairing on the server.
        /// </summary>
        public static WaitForPairingAction  RequestPairing    { get; }
            = Register("requestPairing");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two long-polling actions for equality.
        /// </summary>
        public static Boolean operator == (WaitForPairingAction WaitForPairingAction1, WaitForPairingAction WaitForPairingAction2)
            => WaitForPairingAction1.Equals(WaitForPairingAction2);

        /// <summary>
        /// Compares two long-polling actions for inequality.
        /// </summary>
        public static Boolean operator != (WaitForPairingAction WaitForPairingAction1, WaitForPairingAction WaitForPairingAction2)
            => !WaitForPairingAction1.Equals(WaitForPairingAction2);

        /// <summary>
        /// Compares two long-polling actions.
        /// </summary>
        public static Boolean operator <  (WaitForPairingAction WaitForPairingAction1, WaitForPairingAction WaitForPairingAction2)
            => WaitForPairingAction1.CompareTo(WaitForPairingAction2) < 0;

        /// <summary>
        /// Compares two long-polling actions.
        /// </summary>
        public static Boolean operator <= (WaitForPairingAction WaitForPairingAction1, WaitForPairingAction WaitForPairingAction2)
            => WaitForPairingAction1.CompareTo(WaitForPairingAction2) <= 0;

        /// <summary>
        /// Compares two long-polling actions.
        /// </summary>
        public static Boolean operator >  (WaitForPairingAction WaitForPairingAction1, WaitForPairingAction WaitForPairingAction2)
            => WaitForPairingAction1.CompareTo(WaitForPairingAction2) > 0;

        /// <summary>
        /// Compares two long-polling actions.
        /// </summary>
        public static Boolean operator >= (WaitForPairingAction WaitForPairingAction1, WaitForPairingAction WaitForPairingAction2)
            => WaitForPairingAction1.CompareTo(WaitForPairingAction2) >= 0;

        #endregion

        #region IComparable<WaitForPairingAction> Members

        /// <summary>
        /// Compares two long-polling actions.
        /// </summary>
        /// <param name="Object">A long-polling action to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is WaitForPairingAction waitForPairingAction
                   ? CompareTo(waitForPairingAction)
                   : throw new ArgumentException("The given object is not a long-polling action!", nameof(Object));

        /// <summary>
        /// Compares two long-polling actions.
        /// </summary>
        /// <param name="WaitForPairingAction">A long-polling action to compare with.</param>
        public Int32 CompareTo(WaitForPairingAction WaitForPairingAction)
            => String.Compare(InternalId, WaitForPairingAction.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<WaitForPairingAction> Members

        /// <summary>
        /// Compares two long-polling actions for equality.
        /// </summary>
        /// <param name="Object">A long-polling action to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is WaitForPairingAction waitForPairingAction && Equals(waitForPairingAction);

        /// <summary>
        /// Compares two long-polling actions for equality.
        /// </summary>
        /// <param name="WaitForPairingAction">A long-polling action to compare with.</param>
        public Boolean Equals(WaitForPairingAction WaitForPairingAction)
            => String.Equals(InternalId, WaitForPairingAction.InternalId, StringComparison.Ordinal);

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => InternalId?.GetHashCode(StringComparison.Ordinal) ?? 0;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => InternalId ?? "";

        #endregion

    }

}

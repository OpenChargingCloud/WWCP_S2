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
    /// Extension methods for deployments.
    /// </summary>
    public static class DeploymentExtensions
    {

        /// <summary>
        /// Indicates whether this deployment is null or empty.
        /// </summary>
        /// <param name="Deployment">A deployment.</param>
        public static Boolean IsNullOrEmpty(this Deployment? Deployment)
            => !Deployment.HasValue || Deployment.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this deployment is NOT null or empty.
        /// </summary>
        /// <param name="Deployment">A deployment.</param>
        public static Boolean IsNotNullOrEmpty(this Deployment? Deployment)
            => Deployment.HasValue && Deployment.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// Where a node or endpoint is deployed: in the public internet (WAN) or in the local network of the building (LAN).
    /// </summary>
    public readonly struct Deployment : IS2PredefinedString,
                                         IId,
                                         IEquatable<Deployment>,
                                         IComparable<Deployment>
    {

        #region Data

        private readonly static Dictionary<String, Deployment>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this deployment is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this deployment is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the deployment.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All deployments defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<Deployment> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new deployment based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a deployment.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private Deployment(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // s2-connect-common.yml, Deployment: enum ["WAN", "LAN"]

        #endregion

        #region (private static) Register(Text)

        private static Deployment Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new Deployment(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a deployment.
        /// </summary>
        /// <param name="Text">A text representation of a deployment.</param>
        public static Deployment Parse(String Text)
        {

            if (TryParse(Text, out var deployment))
                return deployment;

            throw new ArgumentException($"Invalid text representation of a deployment: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a deployment.
        /// </summary>
        /// <param name="Text">A text representation of a deployment.</param>
        public static Deployment? TryParse(String Text)
        {

            if (TryParse(Text, out var deployment))
                return deployment;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out Deployment)

        /// <summary>
        /// Try to parse the given text as a deployment. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a deployment.</param>
        /// <param name="Deployment">The parsed deployment.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out Deployment    Deployment)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out Deployment))
                    Deployment = new Deployment(Text, false);

                return true;

            }

            Deployment = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this deployment.
        /// </summary>
        public Deployment Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// WAN: The endpoint is deployed in the WAN (public internet) and addressed by a DNS domain name.
        /// </summary>
        public static Deployment  WAN    { get; }
            = Register("WAN");

        /// <summary>
        /// LAN: The endpoint is deployed in the LAN and addressed by an mDNS hostname; self-signed certificates are used.
        /// </summary>
        public static Deployment  LAN    { get; }
            = Register("LAN");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two deployments for equality.
        /// </summary>
        public static Boolean operator == (Deployment Deployment1, Deployment Deployment2)
            => Deployment1.Equals(Deployment2);

        /// <summary>
        /// Compares two deployments for inequality.
        /// </summary>
        public static Boolean operator != (Deployment Deployment1, Deployment Deployment2)
            => !Deployment1.Equals(Deployment2);

        /// <summary>
        /// Compares two deployments.
        /// </summary>
        public static Boolean operator <  (Deployment Deployment1, Deployment Deployment2)
            => Deployment1.CompareTo(Deployment2) < 0;

        /// <summary>
        /// Compares two deployments.
        /// </summary>
        public static Boolean operator <= (Deployment Deployment1, Deployment Deployment2)
            => Deployment1.CompareTo(Deployment2) <= 0;

        /// <summary>
        /// Compares two deployments.
        /// </summary>
        public static Boolean operator >  (Deployment Deployment1, Deployment Deployment2)
            => Deployment1.CompareTo(Deployment2) > 0;

        /// <summary>
        /// Compares two deployments.
        /// </summary>
        public static Boolean operator >= (Deployment Deployment1, Deployment Deployment2)
            => Deployment1.CompareTo(Deployment2) >= 0;

        #endregion

        #region IComparable<Deployment> Members

        /// <summary>
        /// Compares two deployments.
        /// </summary>
        /// <param name="Object">A deployment to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Deployment deployment
                   ? CompareTo(deployment)
                   : throw new ArgumentException("The given object is not a deployment!", nameof(Object));

        /// <summary>
        /// Compares two deployments.
        /// </summary>
        /// <param name="Deployment">A deployment to compare with.</param>
        public Int32 CompareTo(Deployment Deployment)
            => String.Compare(InternalId, Deployment.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<Deployment> Members

        /// <summary>
        /// Compares two deployments for equality.
        /// </summary>
        /// <param name="Object">A deployment to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Deployment deployment && Equals(deployment);

        /// <summary>
        /// Compares two deployments for equality.
        /// </summary>
        /// <param name="Deployment">A deployment to compare with.</param>
        public Boolean Equals(Deployment Deployment)
            => String.Equals(InternalId, Deployment.InternalId, StringComparison.Ordinal);

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

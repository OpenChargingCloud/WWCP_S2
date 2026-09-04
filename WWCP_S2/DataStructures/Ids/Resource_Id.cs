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

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// Extension methods for resource identifications.
    /// </summary>
    public static class ResourceIdExtensions
    {

        /// <summary>
        /// Indicates whether this resource identification is null or empty.
        /// </summary>
        /// <param name="ResourceId">A resource identification.</param>
        public static Boolean IsNullOrEmpty(this Resource_Id? ResourceId)
            => !ResourceId.HasValue || ResourceId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this resource identification is NOT null or empty.
        /// </summary>
        /// <param name="ResourceId">A resource identification.</param>
        public static Boolean IsNotNullOrEmpty(this Resource_Id? ResourceId)
            => ResourceId.HasValue && ResourceId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The identification of a Resource Manager ("resource_id" in ResourceManagerDetails). Must be unique within the scope of the CEM.
    /// </summary>
    public readonly struct Resource_Id : IId,
                                        IEquatable<Resource_Id>,
                                        IComparable<Resource_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the resource identification.
        /// </summary>
        public String   Value               { get; }

        /// <summary>
        /// Indicates whether this identification is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => Value.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this identification is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => Value.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the resource identification.
        /// </summary>
        public UInt64   Length
            => (UInt64) (Value?.Length ?? 0);

        /// <summary>
        /// Whether this identification is a UUID (the form s2-python requires).
        /// </summary>
        public Boolean  IsUUID
            => S2_Id.IsUUID(Value);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new resource identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a resource identification.</param>
        private Resource_Id(String Text)
        {
            this.Value = Text;
        }

        #endregion


        #region Documentation

        // ID.schema.json
        //   "title":       "ID",
        //   "type":        "string",
        //   "pattern":     "[a-zA-Z0-9\\-_:]{2,64}",
        //   "description": "An identifier expressed as a UUID"
        //
        // ResourceManagerDetails.schema.json
        //   "resource_id": { "$ref": "../schemas/ID.schema.json",
        //                    "description": "Identifier of the Resource Manager. Must be unique within the scope of the CEM." }

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) resource identification.
        /// </summary>
        public static Resource_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a resource identification.
        /// </summary>
        /// <param name="Text">A text representation of a resource identification.</param>
        public static Resource_Id Parse(String Text)
        {

            if (TryParse(Text, out var resourceId))
                return resourceId;

            throw new ArgumentException($"Invalid text representation of a resource identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a resource identification.
        /// </summary>
        /// <param name="Text">A text representation of a resource identification.</param>
        public static Resource_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var resourceId))
                return resourceId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out ResourceId)

        /// <summary>
        /// Try to parse the given text as a resource identification.
        /// </summary>
        /// <param name="Text">A text representation of a resource identification.</param>
        /// <param name="ResourceId">The parsed resource identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out Resource_Id    ResourceId)
        {

            if (S2_Id.IsValid(Text))
            {
                ResourceId = new Resource_Id(Text);
                return true;
            }

            ResourceId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this resource identification.
        /// </summary>
        public Resource_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two resource identifications for equality.
        /// </summary>
        public static Boolean operator == (Resource_Id ResourceId1, Resource_Id ResourceId2)
            => ResourceId1.Equals(ResourceId2);

        /// <summary>
        /// Compares two resource identifications for inequality.
        /// </summary>
        public static Boolean operator != (Resource_Id ResourceId1, Resource_Id ResourceId2)
            => !ResourceId1.Equals(ResourceId2);

        /// <summary>
        /// Compares two resource identifications.
        /// </summary>
        public static Boolean operator <  (Resource_Id ResourceId1, Resource_Id ResourceId2)
            => ResourceId1.CompareTo(ResourceId2) < 0;

        /// <summary>
        /// Compares two resource identifications.
        /// </summary>
        public static Boolean operator <= (Resource_Id ResourceId1, Resource_Id ResourceId2)
            => ResourceId1.CompareTo(ResourceId2) <= 0;

        /// <summary>
        /// Compares two resource identifications.
        /// </summary>
        public static Boolean operator >  (Resource_Id ResourceId1, Resource_Id ResourceId2)
            => ResourceId1.CompareTo(ResourceId2) > 0;

        /// <summary>
        /// Compares two resource identifications.
        /// </summary>
        public static Boolean operator >= (Resource_Id ResourceId1, Resource_Id ResourceId2)
            => ResourceId1.CompareTo(ResourceId2) >= 0;

        #endregion

        #region IComparable<Resource_Id> Members

        /// <summary>
        /// Compares two resource identifications.
        /// </summary>
        /// <param name="Object">A resource identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Resource_Id resourceId
                   ? CompareTo(resourceId)
                   : throw new ArgumentException("The given object is not a resource identification!", nameof(Object));

        /// <summary>
        /// Compares two resource identifications.
        /// </summary>
        /// <param name="ResourceId">A resource identification to compare with.</param>
        public Int32 CompareTo(Resource_Id ResourceId)
            => String.Compare(Value, ResourceId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<Resource_Id> Members

        /// <summary>
        /// Compares two resource identifications for equality.
        /// </summary>
        /// <param name="Object">A resource identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Resource_Id resourceId && Equals(resourceId);

        /// <summary>
        /// Compares two resource identifications for equality.
        /// </summary>
        /// <param name="ResourceId">A resource identification to compare with.</param>
        public Boolean Equals(Resource_Id ResourceId)
            => String.Equals(Value, ResourceId.Value, StringComparison.Ordinal);

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

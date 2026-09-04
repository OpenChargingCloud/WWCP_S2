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
    /// Extension methods for actuator identifications.
    /// </summary>
    public static class ActuatorIdExtensions
    {

        /// <summary>
        /// Indicates whether this actuator identification is null or empty.
        /// </summary>
        /// <param name="ActuatorId">A actuator identification.</param>
        public static Boolean IsNullOrEmpty(this Actuator_Id? ActuatorId)
            => !ActuatorId.HasValue || ActuatorId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this actuator identification is NOT null or empty.
        /// </summary>
        /// <param name="ActuatorId">A actuator identification.</param>
        public static Boolean IsNotNullOrEmpty(this Actuator_Id? ActuatorId)
            => ActuatorId.HasValue && ActuatorId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The identification of an actuator (FRBC and DDBC). Must be unique in the scope of the Resource Manager, for at least the duration of the session between Resource Manager and CEM.
    /// </summary>
    public readonly struct Actuator_Id : IId,
                                        IEquatable<Actuator_Id>,
                                        IComparable<Actuator_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the actuator identification.
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
        /// The length of the actuator identification.
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
        /// Create a new actuator identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a actuator identification.</param>
        private Actuator_Id(String Text)
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
        // FRBC.ActuatorDescription.schema.json / DDBC.ActuatorDescription.schema.json
        //   "id": { "$ref": "../schemas/ID.schema.json",
        //           "description": "ID of the Actuator. Must be unique in the scope of the Resource Manager, for at least the duration of the session between Resource Manager and CEM." }

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) actuator identification.
        /// </summary>
        public static Actuator_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a actuator identification.
        /// </summary>
        /// <param name="Text">A text representation of a actuator identification.</param>
        public static Actuator_Id Parse(String Text)
        {

            if (TryParse(Text, out var actuatorId))
                return actuatorId;

            throw new ArgumentException($"Invalid text representation of a actuator identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a actuator identification.
        /// </summary>
        /// <param name="Text">A text representation of a actuator identification.</param>
        public static Actuator_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var actuatorId))
                return actuatorId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out ActuatorId)

        /// <summary>
        /// Try to parse the given text as a actuator identification.
        /// </summary>
        /// <param name="Text">A text representation of a actuator identification.</param>
        /// <param name="ActuatorId">The parsed actuator identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out Actuator_Id    ActuatorId)
        {

            if (S2_Id.IsValid(Text))
            {
                ActuatorId = new Actuator_Id(Text);
                return true;
            }

            ActuatorId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this actuator identification.
        /// </summary>
        public Actuator_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two actuator identifications for equality.
        /// </summary>
        public static Boolean operator == (Actuator_Id ActuatorId1, Actuator_Id ActuatorId2)
            => ActuatorId1.Equals(ActuatorId2);

        /// <summary>
        /// Compares two actuator identifications for inequality.
        /// </summary>
        public static Boolean operator != (Actuator_Id ActuatorId1, Actuator_Id ActuatorId2)
            => !ActuatorId1.Equals(ActuatorId2);

        /// <summary>
        /// Compares two actuator identifications.
        /// </summary>
        public static Boolean operator <  (Actuator_Id ActuatorId1, Actuator_Id ActuatorId2)
            => ActuatorId1.CompareTo(ActuatorId2) < 0;

        /// <summary>
        /// Compares two actuator identifications.
        /// </summary>
        public static Boolean operator <= (Actuator_Id ActuatorId1, Actuator_Id ActuatorId2)
            => ActuatorId1.CompareTo(ActuatorId2) <= 0;

        /// <summary>
        /// Compares two actuator identifications.
        /// </summary>
        public static Boolean operator >  (Actuator_Id ActuatorId1, Actuator_Id ActuatorId2)
            => ActuatorId1.CompareTo(ActuatorId2) > 0;

        /// <summary>
        /// Compares two actuator identifications.
        /// </summary>
        public static Boolean operator >= (Actuator_Id ActuatorId1, Actuator_Id ActuatorId2)
            => ActuatorId1.CompareTo(ActuatorId2) >= 0;

        #endregion

        #region IComparable<Actuator_Id> Members

        /// <summary>
        /// Compares two actuator identifications.
        /// </summary>
        /// <param name="Object">A actuator identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Actuator_Id actuatorId
                   ? CompareTo(actuatorId)
                   : throw new ArgumentException("The given object is not a actuator identification!", nameof(Object));

        /// <summary>
        /// Compares two actuator identifications.
        /// </summary>
        /// <param name="ActuatorId">A actuator identification to compare with.</param>
        public Int32 CompareTo(Actuator_Id ActuatorId)
            => String.Compare(Value, ActuatorId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<Actuator_Id> Members

        /// <summary>
        /// Compares two actuator identifications for equality.
        /// </summary>
        /// <param name="Object">A actuator identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Actuator_Id actuatorId && Equals(actuatorId);

        /// <summary>
        /// Compares two actuator identifications for equality.
        /// </summary>
        /// <param name="ActuatorId">A actuator identification to compare with.</param>
        public Boolean Equals(Actuator_Id ActuatorId)
            => String.Equals(Value, ActuatorId.Value, StringComparison.Ordinal);

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

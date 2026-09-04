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
    /// Extension methods for transition identifications.
    /// </summary>
    public static class TransitionIdExtensions
    {

        /// <summary>
        /// Indicates whether this transition identification is null or empty.
        /// </summary>
        /// <param name="TransitionId">A transition identification.</param>
        public static Boolean IsNullOrEmpty(this Transition_Id? TransitionId)
            => !TransitionId.HasValue || TransitionId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this transition identification is NOT null or empty.
        /// </summary>
        /// <param name="TransitionId">A transition identification.</param>
        public static Boolean IsNotNullOrEmpty(this Transition_Id? TransitionId)
            => TransitionId.HasValue && TransitionId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The identification of a transition between operation modes. Must be unique in the scope of the OMBC.SystemDescription, FRBC.ActuatorDescription or DDBC.ActuatorDescription in which it is used.
    /// </summary>
    public readonly struct Transition_Id : IId,
                                        IEquatable<Transition_Id>,
                                        IComparable<Transition_Id>
    {

        #region Properties

        /// <summary>
        /// The text value of the transition identification.
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
        /// The length of the transition identification.
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
        /// Create a new transition identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a transition identification.</param>
        private Transition_Id(String Text)
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
        // Transition.schema.json
        //   "id": { "$ref": "../schemas/ID.schema.json",
        //           "description": "ID of the Transition. Must be unique in the scope of the OMBC.SystemDescription, FRBC.ActuatorDescription or DDBC.ActuatorDescription in which it is used." }

        #endregion

        #region (static) NewRandom

        /// <summary>
        /// Create a new random (time-ordered UUID version 7) transition identification.
        /// </summary>
        public static Transition_Id NewRandom
            => new (S2_Id.NewUUID());

        #endregion

        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given string as a transition identification.
        /// </summary>
        /// <param name="Text">A text representation of a transition identification.</param>
        public static Transition_Id Parse(String Text)
        {

            if (TryParse(Text, out var transitionId))
                return transitionId;

            throw new ArgumentException($"Invalid text representation of a transition identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a transition identification.
        /// </summary>
        /// <param name="Text">A text representation of a transition identification.</param>
        public static Transition_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var transitionId))
                return transitionId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text, out TransitionId)

        /// <summary>
        /// Try to parse the given text as a transition identification.
        /// </summary>
        /// <param name="Text">A text representation of a transition identification.</param>
        /// <param name="TransitionId">The parsed transition identification.</param>
        public static Boolean TryParse(String                                Text,
                                       [NotNullWhen(true)] out Transition_Id    TransitionId)
        {

            if (S2_Id.IsValid(Text))
            {
                TransitionId = new Transition_Id(Text);
                return true;
            }

            TransitionId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this transition identification.
        /// </summary>
        public Transition_Id Clone()
            => new (Value.CloneString());

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two transition identifications for equality.
        /// </summary>
        public static Boolean operator == (Transition_Id TransitionId1, Transition_Id TransitionId2)
            => TransitionId1.Equals(TransitionId2);

        /// <summary>
        /// Compares two transition identifications for inequality.
        /// </summary>
        public static Boolean operator != (Transition_Id TransitionId1, Transition_Id TransitionId2)
            => !TransitionId1.Equals(TransitionId2);

        /// <summary>
        /// Compares two transition identifications.
        /// </summary>
        public static Boolean operator <  (Transition_Id TransitionId1, Transition_Id TransitionId2)
            => TransitionId1.CompareTo(TransitionId2) < 0;

        /// <summary>
        /// Compares two transition identifications.
        /// </summary>
        public static Boolean operator <= (Transition_Id TransitionId1, Transition_Id TransitionId2)
            => TransitionId1.CompareTo(TransitionId2) <= 0;

        /// <summary>
        /// Compares two transition identifications.
        /// </summary>
        public static Boolean operator >  (Transition_Id TransitionId1, Transition_Id TransitionId2)
            => TransitionId1.CompareTo(TransitionId2) > 0;

        /// <summary>
        /// Compares two transition identifications.
        /// </summary>
        public static Boolean operator >= (Transition_Id TransitionId1, Transition_Id TransitionId2)
            => TransitionId1.CompareTo(TransitionId2) >= 0;

        #endregion

        #region IComparable<Transition_Id> Members

        /// <summary>
        /// Compares two transition identifications.
        /// </summary>
        /// <param name="Object">A transition identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Transition_Id transitionId
                   ? CompareTo(transitionId)
                   : throw new ArgumentException("The given object is not a transition identification!", nameof(Object));

        /// <summary>
        /// Compares two transition identifications.
        /// </summary>
        /// <param name="TransitionId">A transition identification to compare with.</param>
        public Int32 CompareTo(Transition_Id TransitionId)
            => String.Compare(Value, TransitionId.Value, StringComparison.Ordinal);

        #endregion

        #region IEquatable<Transition_Id> Members

        /// <summary>
        /// Compares two transition identifications for equality.
        /// </summary>
        /// <param name="Object">A transition identification to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is Transition_Id transitionId && Equals(transitionId);

        /// <summary>
        /// Compares two transition identifications for equality.
        /// </summary>
        /// <param name="TransitionId">A transition identification to compare with.</param>
        public Boolean Equals(Transition_Id TransitionId)
            => String.Equals(Value, TransitionId.Value, StringComparison.Ordinal);

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

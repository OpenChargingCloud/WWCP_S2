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
    /// Extension methods for revokable object types.
    /// </summary>
    public static class RevokableObjectExtensions
    {

        /// <summary>
        /// Indicates whether this revokable object type is null or empty.
        /// </summary>
        /// <param name="RevokableObject">A revokable object type.</param>
        public static Boolean IsNullOrEmpty(this RevokableObject? RevokableObject)
            => !RevokableObject.HasValue || RevokableObject.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this revokable object type is NOT null or empty.
        /// </summary>
        /// <param name="RevokableObject">A revokable object type.</param>
        public static Boolean IsNotNullOrEmpty(this RevokableObject? RevokableObject)
            => RevokableObject.HasValue && RevokableObject.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The type of an object that can be revoked with a RevokeObject message.
    /// </summary>
    public readonly struct RevokableObject : IS2PredefinedString,
                                         IId,
                                         IEquatable<RevokableObject>,
                                         IComparable<RevokableObject>
    {

        #region Data

        private readonly static Dictionary<String, RevokableObject>  lookup = new (StringComparer.Ordinal);
        private readonly        String                           InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this value is defined by the S2 JSON schemas known to this library.
        /// </summary>
        public Boolean  IsKnown             { get; }

        /// <summary>
        /// Indicates whether this revokable object type is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this revokable object type is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the revokable object type.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// All revokable object types defined by the S2 JSON schemas.
        /// </summary>
        public static IEnumerable<RevokableObject> All
            => lookup.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new revokable object type based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a revokable object type.</param>
        /// <param name="IsKnown">Whether the value is defined by the S2 JSON schemas.</param>
        private RevokableObject(String   Text,
                            Boolean  IsKnown)
        {
            this.InternalId  = Text;
            this.IsKnown     = IsKnown;
        }

        #endregion


        #region Documentation

        // RevokableObjects.schema.json
        //   "title": "RevokableObjects",
        //   "type":  "string",
        //   "enum":  ["PEBC.PowerConstraints", "PEBC.EnergyConstraint", "PEBC.Instruction", "PPBC.PowerProfileDefinition", "PPBC.ScheduleInstruction", "PPBC.StartInterruptionInstruction", "PPBC.EndInterruptionInstruction", "OMBC.SystemDescription", "OMBC.Instruction", "FRBC.SystemDescription", "FRBC.Instruction", "DDBC.SystemDescription", "DDBC.Instruction"],
        //   "description":
        //     PEBC.PowerConstraints: Object type PEBC.PowerConstraints
        //     PEBC.EnergyConstraint: Object type PEBC.EnergyConstraint
        //     PEBC.Instruction: Object type PEBC.Instruction
        //     PPBC.PowerProfileDefinition: Object type PPBC.PowerProfileDefinition
        //     PPBC.ScheduleInstruction: Object type PPBC.ScheduleInstruction
        //     PPBC.StartInterruptionInstruction: Object type PPBC.StartInterruptionInstruction
        //     PPBC.EndInterruptionInstruction: Object type PPBC.EndInterruptionInstruction
        //     OMBC.SystemDescription: Object type OMBC.SystemDescription
        //     OMBC.Instruction: Object type OMBC.Instruction
        //     FRBC.SystemDescription: Object type FRBC.SystemDescription
        //     FRBC.Instruction: Object type FRBC.Instruction
        //     DDBC.SystemDescription: Object type DDBC.SystemDescription
        //     DDBC.Instruction: Object type DDBC.Instruction

        #endregion

        #region (private static) Register(Text)

        private static RevokableObject Register(String Text)

            => lookup.AddAndReturnValue(
                   Text,
                   new RevokableObject(Text, true)
               );

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a revokable object type.
        /// </summary>
        /// <param name="Text">A text representation of a revokable object type.</param>
        public static RevokableObject Parse(String Text)
        {

            if (TryParse(Text, out var revokableObject))
                return revokableObject;

            throw new ArgumentException($"Invalid text representation of a revokable object type: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a revokable object type.
        /// </summary>
        /// <param name="Text">A text representation of a revokable object type.</param>
        public static RevokableObject? TryParse(String Text)
        {

            if (TryParse(Text, out var revokableObject))
                return revokableObject;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out RevokableObject)

        /// <summary>
        /// Try to parse the given text as a revokable object type. Values not defined by the S2 JSON
        /// schemas are returned with IsKnown == false and are not registered.
        /// </summary>
        /// <param name="Text">A text representation of a revokable object type.</param>
        /// <param name="RevokableObject">The parsed revokable object type.</param>
        public static Boolean TryParse(String                                 Text,
                                       [NotNullWhen(true)] out RevokableObject    RevokableObject)
        {


            if (Text.IsNotNullOrEmpty())
            {

                if (!lookup.TryGetValue(Text, out RevokableObject))
                    RevokableObject = new RevokableObject(Text, false);

                return true;

            }

            RevokableObject = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this revokable object type.
        /// </summary>
        public RevokableObject Clone()

            => new (
                   InternalId.CloneString(),
                   IsKnown
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// PEBC.PowerConstraints: Object type PEBC.PowerConstraints
        /// </summary>
        public static RevokableObject  PEBC_PowerConstraints    { get; }
            = Register("PEBC.PowerConstraints");

        /// <summary>
        /// PEBC.EnergyConstraint: Object type PEBC.EnergyConstraint
        /// </summary>
        public static RevokableObject  PEBC_EnergyConstraint    { get; }
            = Register("PEBC.EnergyConstraint");

        /// <summary>
        /// PEBC.Instruction: Object type PEBC.Instruction
        /// </summary>
        public static RevokableObject  PEBC_Instruction    { get; }
            = Register("PEBC.Instruction");

        /// <summary>
        /// PPBC.PowerProfileDefinition: Object type PPBC.PowerProfileDefinition
        /// </summary>
        public static RevokableObject  PPBC_PowerProfileDefinition    { get; }
            = Register("PPBC.PowerProfileDefinition");

        /// <summary>
        /// PPBC.ScheduleInstruction: Object type PPBC.ScheduleInstruction
        /// </summary>
        public static RevokableObject  PPBC_ScheduleInstruction    { get; }
            = Register("PPBC.ScheduleInstruction");

        /// <summary>
        /// PPBC.StartInterruptionInstruction: Object type PPBC.StartInterruptionInstruction
        /// </summary>
        public static RevokableObject  PPBC_StartInterruptionInstruction    { get; }
            = Register("PPBC.StartInterruptionInstruction");

        /// <summary>
        /// PPBC.EndInterruptionInstruction: Object type PPBC.EndInterruptionInstruction
        /// </summary>
        public static RevokableObject  PPBC_EndInterruptionInstruction    { get; }
            = Register("PPBC.EndInterruptionInstruction");

        /// <summary>
        /// OMBC.SystemDescription: Object type OMBC.SystemDescription
        /// </summary>
        public static RevokableObject  OMBC_SystemDescription    { get; }
            = Register("OMBC.SystemDescription");

        /// <summary>
        /// OMBC.Instruction: Object type OMBC.Instruction
        /// </summary>
        public static RevokableObject  OMBC_Instruction    { get; }
            = Register("OMBC.Instruction");

        /// <summary>
        /// FRBC.SystemDescription: Object type FRBC.SystemDescription
        /// </summary>
        public static RevokableObject  FRBC_SystemDescription    { get; }
            = Register("FRBC.SystemDescription");

        /// <summary>
        /// FRBC.Instruction: Object type FRBC.Instruction
        /// </summary>
        public static RevokableObject  FRBC_Instruction    { get; }
            = Register("FRBC.Instruction");

        /// <summary>
        /// DDBC.SystemDescription: Object type DDBC.SystemDescription
        /// </summary>
        public static RevokableObject  DDBC_SystemDescription    { get; }
            = Register("DDBC.SystemDescription");

        /// <summary>
        /// DDBC.Instruction: Object type DDBC.Instruction
        /// </summary>
        public static RevokableObject  DDBC_Instruction    { get; }
            = Register("DDBC.Instruction");

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two revokable object types for equality.
        /// </summary>
        public static Boolean operator == (RevokableObject RevokableObject1, RevokableObject RevokableObject2)
            => RevokableObject1.Equals(RevokableObject2);

        /// <summary>
        /// Compares two revokable object types for inequality.
        /// </summary>
        public static Boolean operator != (RevokableObject RevokableObject1, RevokableObject RevokableObject2)
            => !RevokableObject1.Equals(RevokableObject2);

        /// <summary>
        /// Compares two revokable object types.
        /// </summary>
        public static Boolean operator <  (RevokableObject RevokableObject1, RevokableObject RevokableObject2)
            => RevokableObject1.CompareTo(RevokableObject2) < 0;

        /// <summary>
        /// Compares two revokable object types.
        /// </summary>
        public static Boolean operator <= (RevokableObject RevokableObject1, RevokableObject RevokableObject2)
            => RevokableObject1.CompareTo(RevokableObject2) <= 0;

        /// <summary>
        /// Compares two revokable object types.
        /// </summary>
        public static Boolean operator >  (RevokableObject RevokableObject1, RevokableObject RevokableObject2)
            => RevokableObject1.CompareTo(RevokableObject2) > 0;

        /// <summary>
        /// Compares two revokable object types.
        /// </summary>
        public static Boolean operator >= (RevokableObject RevokableObject1, RevokableObject RevokableObject2)
            => RevokableObject1.CompareTo(RevokableObject2) >= 0;

        #endregion

        #region IComparable<RevokableObject> Members

        /// <summary>
        /// Compares two revokable object types.
        /// </summary>
        /// <param name="Object">A revokable object type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is RevokableObject revokableObject
                   ? CompareTo(revokableObject)
                   : throw new ArgumentException("The given object is not a revokable object type!", nameof(Object));

        /// <summary>
        /// Compares two revokable object types.
        /// </summary>
        /// <param name="RevokableObject">A revokable object type to compare with.</param>
        public Int32 CompareTo(RevokableObject RevokableObject)
            => String.Compare(InternalId, RevokableObject.InternalId, StringComparison.Ordinal);

        #endregion

        #region IEquatable<RevokableObject> Members

        /// <summary>
        /// Compares two revokable object types for equality.
        /// </summary>
        /// <param name="Object">A revokable object type to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is RevokableObject revokableObject && Equals(revokableObject);

        /// <summary>
        /// Compares two revokable object types for equality.
        /// </summary>
        /// <param name="RevokableObject">A revokable object type to compare with.</param>
        public Boolean Equals(RevokableObject RevokableObject)
            => String.Equals(InternalId, RevokableObject.InternalId, StringComparison.Ordinal);

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

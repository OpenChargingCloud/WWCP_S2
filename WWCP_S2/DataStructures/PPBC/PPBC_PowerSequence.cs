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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.S2
{

    /// <summary>
    /// A PPBC power sequence: a chronological list of power sequence elements that
    /// describes one alternative of running a power profile.
    /// </summary>
    public sealed class PPBC_PowerSequence : IEquatable<PPBC_PowerSequence>
    {

        #region Properties

        /// <summary>
        /// The identification of the power sequence. Must be unique in the scope of the
        /// PPBC.PowerSequenceContainer in which it is used.
        /// </summary>
        [Mandatory]
        public PowerSequence_Id                            Id                       { get; }

        /// <summary>
        /// The list of power sequence elements. Contains at least one element;
        /// the elements are placed in chronological order.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PPBC_PowerSequenceElement>    Elements                 { get; }

        /// <summary>
        /// Indicates whether the option of pausing a sequence is available.
        /// </summary>
        [Mandatory]
        public Boolean                                     IsInterruptible          { get; }

        /// <summary>
        /// The optional maximum duration for which a device can be paused between the end
        /// of the previous running sequence and the start of this one.
        /// </summary>
        [Optional]
        public Duration?                                   MaxPauseBefore           { get; }

        /// <summary>
        /// Indicates whether this power sequence may only be used during an abnormal condition.
        /// </summary>
        [Mandatory]
        public Boolean                                     AbnormalConditionOnly    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new PPBC power sequence.
        /// </summary>
        /// <param name="Id">The identification of the power sequence (unique within its power sequence container).</param>
        /// <param name="Elements">The list of power sequence elements in chronological order (1..288 entries).</param>
        /// <param name="IsInterruptible">Whether the option of pausing a sequence is available.</param>
        /// <param name="AbnormalConditionOnly">Whether this power sequence may only be used during an abnormal condition.</param>
        /// <param name="MaxPauseBefore">An optional maximum duration for which a device can be paused between the end of the previous running sequence and the start of this one.</param>
        public PPBC_PowerSequence(PowerSequence_Id                          Id,
                                  IReadOnlyList<PPBC_PowerSequenceElement>  Elements,
                                  Boolean                                   IsInterruptible,
                                  Boolean                                   AbnormalConditionOnly,
                                  Duration?                                 MaxPauseBefore   = null)
        {

            ArgumentNullException.ThrowIfNull(Elements);

            if (Elements.Count < 1)
                throw new ArgumentException("The elements must contain at least one entry!",
                                            nameof(Elements));

            if (Elements.Count > 288)
                throw new ArgumentException($"The elements must not contain more than 288 entries, but {Elements.Count} were given!",
                                            nameof(Elements));

            this.Id                     = Id;
            this.Elements               = [.. Elements];
            this.IsInterruptible        = IsInterruptible;
            this.AbnormalConditionOnly  = AbnormalConditionOnly;
            this.MaxPauseBefore         = MaxPauseBefore;

            unchecked
            {
                hashCode = this.Id.                   GetHashCode()  * 11 ^
                           this.Elements.             CalcHashCode() *  7 ^
                           this.IsInterruptible.      GetHashCode()  *  5 ^
                          (this.MaxPauseBefore?.      GetHashCode() ?? 0) *  3 ^
                           this.AbnormalConditionOnly.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // PPBC.PowerSequence.schema.json
        //   "title": "PPBC_PowerSequence",
        //   "properties": {
        //     "id":                      { "$ref": "../schemas/ID.schema.json",
        //                                  "description": "ID of the PPBC.PowerSequence. Must be unique in the scope of the
        //                                                  PPBC.PowerSequnceContainer in which it is used." },
        //     "elements":                { "type": "array", "minItems": 1, "maxItems": 288,
        //                                  "items": { "$ref": "../schemas/PPBC.PowerSequenceElement.schema.json" },
        //                                  "description": "List of PPBC.PowerSequenceElements. Shall contain at least one element.
        //                                                  Elements must be placed in chronological order." },
        //     "is_interruptible":        { "type": "boolean",
        //                                  "description": "Indicates whether the option of pausing a sequence is available." },
        //     "max_pause_before":        { "$ref": "../schemas/Duration.schema.json",
        //                                  "description": "The maximum duration for which a device can be paused between the end
        //                                                  of the previous running sequence and the start of this one" },
        //     "abnormal_condition_only": { "type": "boolean",
        //                                  "description": "Indicates if this PPBC.PowerSequence may only be used during an abnormal condition" }
        //   },
        //   "required": ["id", "elements", "is_interruptible", "abnormal_condition_only"],
        //   "additionalProperties": false

        #endregion

        #region (static) TryParse(JSON, out PPBC_PowerSequence, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power sequence.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerSequence">The parsed PPBC power sequence.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerSequence?  PPBC_PowerSequence,
                                       [NotNullWhen(false)] out String?              ErrorResponse)

            => TryParse(JSON,
                        out PPBC_PowerSequence,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power sequence.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerSequence">The parsed PPBC power sequence.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerSequence?  PPBC_PowerSequence,
                                       [NotNullWhen(false)] out String?              ErrorResponse,
                                       S2ParserOptions?                              Options)

            => TryParse(JSON,
                        out PPBC_PowerSequence,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power sequence.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerSequence">The parsed PPBC power sequence.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPPBC_PowerSequenceParser">A delegate to parse custom PPBC power sequences.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerSequence?      PPBC_PowerSequence,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options,
                                       CustomJObjectParserDelegate<PPBC_PowerSequence>?  CustomPPBC_PowerSequenceParser)
        {

            try
            {

                PPBC_PowerSequence = null;

                #region id                         [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
                                             "power sequence identification",
                                             PowerSequence_Id.TryParse,
                                             Options,
                                             out PowerSequence_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region elements                   [mandatory]

                if (!JSON.ParseMandatoryS2List("elements",
                                               "power sequence elements",
                                               PPBC_PowerSequenceElement.TryParse,
                                               Options,
                                               1,
                                               288,
                                               out IReadOnlyList<PPBC_PowerSequenceElement>? elements,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region is_interruptible           [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("is_interruptible",
                                                  "is interruptible",
                                                  out Boolean isInterruptible,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region max_pause_before           [optional]

                if (!JSON.ParseOptionalS2Duration("max_pause_before",
                                                  "maximum pause before",
                                                  out Duration? maxPauseBefore,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region abnormal_condition_only    [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("abnormal_condition_only",
                                                  "abnormal condition only",
                                                  out Boolean abnormalConditionOnly,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "id",
                                                    "elements",
                                                    "is_interruptible",
                                                    "max_pause_before",
                                                    "abnormal_condition_only"))
                {
                    return false;
                }

                #endregion


                PPBC_PowerSequence = new PPBC_PowerSequence(
                                         id,
                                         elements,
                                         isInterruptible,
                                         abnormalConditionOnly,
                                         maxPauseBefore
                                     );

                if (CustomPPBC_PowerSequenceParser is not null)
                    PPBC_PowerSequence = CustomPPBC_PowerSequenceParser(JSON,
                                                                        PPBC_PowerSequence);

                return true;

            }
            catch (Exception e)
            {
                PPBC_PowerSequence  = null;
                ErrorResponse       = "The given JSON representation of a PPBC power sequence is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPPBC_PowerSequenceSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPPBC_PowerSequenceSerializer">A delegate to serialize custom PPBC power sequences.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PPBC_PowerSequence>? CustomPPBC_PowerSequenceSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("id",                       Id.ToString()),
                                 new JProperty("elements",                 new JArray(Elements.Select(element => element.ToJSON()))),
                                 new JProperty("is_interruptible",         IsInterruptible),

                           MaxPauseBefore.HasValue
                               ? new JProperty("max_pause_before",         MaxPauseBefore.Value.ToJSON())
                               : null,

                                 new JProperty("abnormal_condition_only",  AbnormalConditionOnly)

                       );

            return CustomPPBC_PowerSequenceSerializer is not null
                       ? CustomPPBC_PowerSequenceSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this PPBC power sequence.
        /// </summary>
        public PPBC_PowerSequence Clone()

            => new (
                   Id.Clone(),
                   [.. Elements.Select(element => element.Clone())],
                   IsInterruptible,
                   AbnormalConditionOnly,
                   MaxPauseBefore
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two PPBC power sequences for equality.
        /// </summary>
        public static Boolean operator == (PPBC_PowerSequence? PPBC_PowerSequence1, PPBC_PowerSequence? PPBC_PowerSequence2)
        {

            if (ReferenceEquals(PPBC_PowerSequence1, PPBC_PowerSequence2))
                return true;

            if (PPBC_PowerSequence1 is null || PPBC_PowerSequence2 is null)
                return false;

            return PPBC_PowerSequence1.Equals(PPBC_PowerSequence2);

        }

        /// <summary>
        /// Compares two PPBC power sequences for inequality.
        /// </summary>
        public static Boolean operator != (PPBC_PowerSequence? PPBC_PowerSequence1, PPBC_PowerSequence? PPBC_PowerSequence2)
            => !(PPBC_PowerSequence1 == PPBC_PowerSequence2);

        #endregion

        #region IEquatable<PPBC_PowerSequence> Members

        /// <summary>
        /// Compares two PPBC power sequences for equality.
        /// </summary>
        /// <param name="Object">A PPBC power sequence to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PPBC_PowerSequence ppbcPowerSequence && Equals(ppbcPowerSequence);

        /// <summary>
        /// Compares two PPBC power sequences for equality.
        /// </summary>
        /// <param name="PPBC_PowerSequence">A PPBC power sequence to compare with.</param>
        public Boolean Equals(PPBC_PowerSequence? PPBC_PowerSequence)

            => PPBC_PowerSequence is not null &&

               Id.                   Equals       (PPBC_PowerSequence.Id) &&
               Elements.             SequenceEqual(PPBC_PowerSequence.Elements) &&
               IsInterruptible.      Equals       (PPBC_PowerSequence.IsInterruptible) &&
               AbnormalConditionOnly.Equals       (PPBC_PowerSequence.AbnormalConditionOnly) &&

               Nullable.Equals(MaxPauseBefore, PPBC_PowerSequence.MaxPauseBefore);

        #endregion

        #region (override) GetHashCode()

        private readonly Int32 hashCode;

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => hashCode;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   $"{Id}: {Elements.Count} element(s)",

                   IsInterruptible
                       ? ", interruptible"
                       : "",

                   MaxPauseBefore.HasValue
                       ? $", max. pause before: {MaxPauseBefore.Value}"
                       : "",

                   AbnormalConditionOnly
                       ? ", abnormal condition only"
                       : ""

               );

        #endregion

    }

}

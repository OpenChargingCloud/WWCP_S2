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
    /// An operation mode of a Fill Rate Based Control (FRBC) actuator: a list of elements
    /// describing the fill rate, power and costs of the mode depending on the fill level.
    /// </summary>
    public sealed class FRBC_OperationMode : IEquatable<FRBC_OperationMode>
    {

        #region Properties

        /// <summary>
        /// The identification of the FRBC.OperationMode. Must be unique in the scope of the
        /// FRBC.ActuatorDescription in which it is used.
        /// </summary>
        [Mandatory]
        public OperationMode_Id                          Id                       { get; }

        /// <summary>
        /// The optional human readable name/description of the FRBC.OperationMode. This element
        /// is only intended for diagnostic purposes and not for HMI applications.
        /// </summary>
        [Optional]
        public String?                                   DiagnosticLabel          { get; }

        /// <summary>
        /// The FRBC.OperationModeElements, which describe the properties of this FRBC.OperationMode
        /// depending on the fill level. The fill level ranges of the elements must be contiguous.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<FRBC_OperationModeElement>  Elements                 { get; }

        /// <summary>
        /// Indicates if this FRBC.OperationMode may only be used during an abnormal condition.
        /// </summary>
        [Mandatory]
        public Boolean                                   AbnormalConditionOnly    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new FRBC operation mode.
        /// </summary>
        /// <param name="Id">The identification of the FRBC.OperationMode.</param>
        /// <param name="Elements">The elements describing this operation mode depending on the fill level (1..100 entries, contiguous fill level ranges).</param>
        /// <param name="AbnormalConditionOnly">Whether this FRBC.OperationMode may only be used during an abnormal condition.</param>
        /// <param name="DiagnosticLabel">An optional human readable name/description of the FRBC.OperationMode (diagnostic purposes only).</param>
        public FRBC_OperationMode(OperationMode_Id                          Id,
                                  IReadOnlyList<FRBC_OperationModeElement>  Elements,
                                  Boolean                                   AbnormalConditionOnly,
                                  String?                                   DiagnosticLabel   = null)
        {

            ArgumentNullException.ThrowIfNull(Elements);

            if (Elements.Count < 1)
                throw new ArgumentException("An FRBC operation mode must contain at least one element!",
                                            nameof(Elements));

            if (Elements.Count > 100)
                throw new ArgumentException($"An FRBC operation mode must not contain more than 100 elements, but {Elements.Count} were given!",
                                            nameof(Elements));

            var sortedElements = Elements.OrderBy(element => element.FillLevelRange.StartOfRange).ToArray();

            for (var i = 0; i < sortedElements.Length - 1; i++)
            {
                if (sortedElements[i].FillLevelRange.EndOfRange != sortedElements[i + 1].FillLevelRange.StartOfRange)
                    throw new ArgumentException($"The fill level ranges of the elements of an FRBC operation mode must be contiguous, but the range {sortedElements[i].FillLevelRange} is followed by {sortedElements[i + 1].FillLevelRange}!",
                                                nameof(Elements));
            }

            this.Id                     = Id;
            this.Elements               = [.. Elements];
            this.AbnormalConditionOnly  = AbnormalConditionOnly;
            this.DiagnosticLabel        = DiagnosticLabel;

            unchecked
            {
                hashCode = this.Id.                   GetHashCode()  * 7 ^
                          (this.DiagnosticLabel?.     GetHashCode(StringComparison.Ordinal) ?? 0) * 5 ^
                           this.Elements.             CalcHashCode() * 3 ^
                           this.AbnormalConditionOnly.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // FRBC.OperationMode.schema.json
        //   "title": "FRBC_OperationMode",
        //   "properties": {
        //     "id":                      { "$ref": "../schemas/ID.schema.json",
        //                                  "description": "ID of the FRBC.OperationMode. Must be unique in the scope of the
        //                                                  FRBC.ActuatorDescription in which it is used." },
        //     "diagnostic_label":        { "type": "string",
        //                                  "description": "Human readable name/description of the FRBC.OperationMode. This element is only
        //                                                  intended for diagnostic purposes and not for HMI applications." },
        //     "elements":                { "type": "array", "minItems": 1, "maxItems": 100,
        //                                  "items": { "$ref": "../schemas/FRBC.OperationModeElement.schema.json" },
        //                                  "description": "List of FRBC.OperationModeElements, which describe the properties of this
        //                                                  FRBC.OperationMode depending on the fill_level. The fill_level_ranges of the
        //                                                  items in the Array must be contiguous." },
        //     "abnormal_condition_only": { "type": "boolean",
        //                                  "description": "Indicates if this FRBC.OperationMode may only be used during an abnormal condition" }
        //   },
        //   "required": ["id", "elements", "abnormal_condition_only"],
        //   "additionalProperties": false
        //
        // Semantic rule (CONVENTIONS.md §5): the fill level ranges of the elements are contiguous, i.e. sorted by
        // start_of_range, each element's end_of_range equals the next element's start_of_range.

        #endregion

        #region (static) TryParse(JSON, out FRBC_OperationMode, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an FRBC operation mode.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCOperationMode">The parsed FRBC operation mode.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out FRBC_OperationMode?  FRBCOperationMode,
                                       [NotNullWhen(false)] out String?              ErrorResponse)

            => TryParse(JSON,
                        out FRBCOperationMode,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC operation mode.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCOperationMode">The parsed FRBC operation mode.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out FRBC_OperationMode?  FRBCOperationMode,
                                       [NotNullWhen(false)] out String?              ErrorResponse,
                                       S2ParserOptions?                              Options)

            => TryParse(JSON,
                        out FRBCOperationMode,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an FRBC operation mode.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="FRBCOperationMode">The parsed FRBC operation mode.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomFRBCOperationModeParser">A delegate to parse custom FRBC operation modes.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out FRBC_OperationMode?      FRBCOperationMode,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options,
                                       CustomJObjectParserDelegate<FRBC_OperationMode>?  CustomFRBCOperationModeParser)
        {

            try
            {

                FRBCOperationMode = null;

                #region id                         [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
                                             "operation mode identification",
                                             OperationMode_Id.TryParse,
                                             Options,
                                             out OperationMode_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region diagnostic_label           [optional]

                if (!JSON.ParseOptionalS2String("diagnostic_label",
                                                "diagnostic label",
                                                out String? diagnosticLabel,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region elements                   [mandatory]

                if (!JSON.ParseMandatoryS2List("elements",
                                               "operation mode elements",
                                               FRBC_OperationModeElement.TryParse,
                                               Options,
                                               1,
                                               100,
                                               out IReadOnlyList<FRBC_OperationModeElement>? elements,
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
                                                    "diagnostic_label",
                                                    "elements",
                                                    "abnormal_condition_only"))
                {
                    return false;
                }

                #endregion


                FRBCOperationMode = new FRBC_OperationMode(
                                        id,
                                        elements,
                                        abnormalConditionOnly,
                                        diagnosticLabel
                                    );

                if (CustomFRBCOperationModeParser is not null)
                    FRBCOperationMode = CustomFRBCOperationModeParser(JSON,
                                                                      FRBCOperationMode);

                return true;

            }
            catch (Exception e)
            {
                FRBCOperationMode  = null;
                ErrorResponse      = "The given JSON representation of an FRBC operation mode is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomFRBCOperationModeSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomFRBCOperationModeSerializer">A delegate to serialize custom FRBC operation modes.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_OperationMode>? CustomFRBCOperationModeSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("id",                       Id.ToString()),

                           DiagnosticLabel is not null
                               ? new JProperty("diagnostic_label",         DiagnosticLabel)
                               : null,

                                 new JProperty("elements",                 new JArray(Elements.Select(element => element.ToJSON()))),
                                 new JProperty("abnormal_condition_only",  AbnormalConditionOnly)

                       );

            return CustomFRBCOperationModeSerializer is not null
                       ? CustomFRBCOperationModeSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this FRBC operation mode.
        /// </summary>
        public FRBC_OperationMode Clone()

            => new (
                   Id.Clone(),
                   [.. Elements.Select(element => element.Clone())],
                   AbnormalConditionOnly,
                   DiagnosticLabel?.CloneString()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two FRBC operation modes for equality.
        /// </summary>
        public static Boolean operator == (FRBC_OperationMode? FRBCOperationMode1, FRBC_OperationMode? FRBCOperationMode2)
        {

            if (ReferenceEquals(FRBCOperationMode1, FRBCOperationMode2))
                return true;

            if (FRBCOperationMode1 is null || FRBCOperationMode2 is null)
                return false;

            return FRBCOperationMode1.Equals(FRBCOperationMode2);

        }

        /// <summary>
        /// Compares two FRBC operation modes for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_OperationMode? FRBCOperationMode1, FRBC_OperationMode? FRBCOperationMode2)
            => !(FRBCOperationMode1 == FRBCOperationMode2);

        #endregion

        #region IEquatable<FRBC_OperationMode> Members

        /// <summary>
        /// Compares two FRBC operation modes for equality.
        /// </summary>
        /// <param name="Object">An FRBC operation mode to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_OperationMode frbcOperationMode && Equals(frbcOperationMode);

        /// <summary>
        /// Compares two FRBC operation modes for equality.
        /// </summary>
        /// <param name="FRBCOperationMode">An FRBC operation mode to compare with.</param>
        public Boolean Equals(FRBC_OperationMode? FRBCOperationMode)

            => FRBCOperationMode is not null &&

               Id.Equals(FRBCOperationMode.Id) &&
               String.Equals(DiagnosticLabel, FRBCOperationMode.DiagnosticLabel, StringComparison.Ordinal) &&
               Elements.SequenceEqual(FRBCOperationMode.Elements) &&
               AbnormalConditionOnly.Equals(FRBCOperationMode.AbnormalConditionOnly);

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

                   DiagnosticLabel is not null
                       ? $" ({DiagnosticLabel})"
                       : "",

                   AbnormalConditionOnly
                       ? ", abnormal condition only"
                       : ""

               );

        #endregion

    }

}

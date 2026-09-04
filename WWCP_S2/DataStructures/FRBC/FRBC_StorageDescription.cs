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
    /// The description of the storage of a fill rate based control (FRBC) system:
    /// which optional information it can provide and the fill level range the CEM
    /// should keep it in.
    /// </summary>
    public sealed class FRBC_StorageDescription : IEquatable<FRBC_StorageDescription>
    {

        #region Properties

        /// <summary>
        /// The optional human readable name/description of the storage (e.g. hot water buffer
        /// or battery). This element is only intended for diagnostic purposes and not for HMI applications.
        /// </summary>
        [Optional]
        public String?      DiagnosticLabel                   { get; }

        /// <summary>
        /// The optional human readable description of the (physical) units associated with the
        /// fill level (e.g. degrees Celsius or percentage state of charge). This element is only
        /// intended for diagnostic purposes and not for HMI applications.
        /// </summary>
        [Optional]
        public String?      FillLevelLabel                    { get; }

        /// <summary>
        /// Indicates whether the storage could provide details of power leakage behaviour
        /// through the FRBC.LeakageBehaviour.
        /// </summary>
        [Mandatory]
        public Boolean      ProvidesLeakageBehaviour          { get; }

        /// <summary>
        /// Indicates whether the storage could provide a target profile for the fill level
        /// through the FRBC.FillLevelTargetProfile.
        /// </summary>
        [Mandatory]
        public Boolean      ProvidesFillLevelTargetProfile    { get; }

        /// <summary>
        /// Indicates whether the storage could provide a usage forecast through the FRBC.UsageForecast.
        /// </summary>
        [Mandatory]
        public Boolean      ProvidesUsageForecast             { get; }

        /// <summary>
        /// The range in which the fill level should remain. It is expected of the CEM to keep the
        /// fill level within this range. When the fill level is not within this range, the Resource
        /// Manager can ignore instructions from the CEM (except during abnormal conditions).
        /// </summary>
        [Mandatory]
        public NumberRange  FillLevelRange                    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new storage description.
        /// </summary>
        /// <param name="ProvidesLeakageBehaviour">Whether the storage could provide details of power leakage behaviour through the FRBC.LeakageBehaviour.</param>
        /// <param name="ProvidesFillLevelTargetProfile">Whether the storage could provide a target profile for the fill level through the FRBC.FillLevelTargetProfile.</param>
        /// <param name="ProvidesUsageForecast">Whether the storage could provide a usage forecast through the FRBC.UsageForecast.</param>
        /// <param name="FillLevelRange">The range in which the fill level should remain.</param>
        /// <param name="DiagnosticLabel">An optional human readable name/description of the storage (diagnostic purposes only).</param>
        /// <param name="FillLevelLabel">An optional human readable description of the (physical) units of the fill level (diagnostic purposes only).</param>
        public FRBC_StorageDescription(Boolean      ProvidesLeakageBehaviour,
                                       Boolean      ProvidesFillLevelTargetProfile,
                                       Boolean      ProvidesUsageForecast,
                                       NumberRange  FillLevelRange,
                                       String?      DiagnosticLabel   = null,
                                       String?      FillLevelLabel    = null)
        {

            ArgumentNullException.ThrowIfNull(FillLevelRange);

            this.ProvidesLeakageBehaviour        = ProvidesLeakageBehaviour;
            this.ProvidesFillLevelTargetProfile  = ProvidesFillLevelTargetProfile;
            this.ProvidesUsageForecast           = ProvidesUsageForecast;
            this.FillLevelRange                  = FillLevelRange;
            this.DiagnosticLabel                 = DiagnosticLabel;
            this.FillLevelLabel                  = FillLevelLabel;

            unchecked
            {
                hashCode = (this.DiagnosticLabel?.              GetHashCode(StringComparison.Ordinal) ?? 0) * 13 ^
                           (this.FillLevelLabel?.               GetHashCode(StringComparison.Ordinal) ?? 0) * 11 ^
                            this.ProvidesLeakageBehaviour.      GetHashCode()                               *  7 ^
                            this.ProvidesFillLevelTargetProfile.GetHashCode()                               *  5 ^
                            this.ProvidesUsageForecast.         GetHashCode()                               *  3 ^
                            this.FillLevelRange.                GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // FRBC.StorageDescription.schema.json
        //   "title": "FRBC_StorageDescription",
        //   "properties": {
        //     "diagnostic_label":                   { "type": "string",
        //                                             "description": "Human readable name/description of the storage (e.g. hot water buffer
        //                                                             or battery). This element is only intended for diagnostic purposes and
        //                                                             not for HMI applications." },
        //     "fill_level_label":                   { "type": "string",
        //                                             "description": "Human readable description of the (physical) units associated with the
        //                                                             fill_level (e.g. degrees Celsius or percentage state of charge). This
        //                                                             element is only intended for diagnostic purposes and not for HMI applications." },
        //     "provides_leakage_behaviour":         { "type": "boolean",
        //                                             "description": "Indicates whether the Storage could provide details of power leakage
        //                                                             behaviour through the FRBC.LeakageBehaviour." },
        //     "provides_fill_level_target_profile": { "type": "boolean",
        //                                             "description": "Indicates whether the Storage could provide a target profile for the
        //                                                             fill level through the FRBC.FillLevelTargetProfile." },
        //     "provides_usage_forecast":            { "type": "boolean",
        //                                             "description": "Indicates whether the Storage could provide a UsageForecast through
        //                                                             the FRBC.UsageForecast." },
        //     "fill_level_range":                   { "$ref": "../schemas/NumberRange.schema.json",
        //                                             "description": "The range in which the fill_level should remain. It is expected of the
        //                                                             CEM to keep the fill_level within this range. When the fill_level is not
        //                                                             within this range, the Resource Manager can ignore instructions from the
        //                                                             CEM (except during abnormal conditions)." }
        //   },
        //   "required": ["provides_leakage_behaviour", "provides_fill_level_target_profile", "provides_usage_forecast", "fill_level_range"],
        //   "additionalProperties": false
        //
        // Semantic rule: fill_level_range.start_of_range <= fill_level_range.end_of_range (validated by NumberRange).

        #endregion

        #region (static) TryParse(JSON, out StorageDescription, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a storage description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="StorageDescription">The parsed storage description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out FRBC_StorageDescription?  StorageDescription,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)

            => TryParse(JSON,
                        out StorageDescription,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a storage description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="StorageDescription">The parsed storage description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out FRBC_StorageDescription?  StorageDescription,
                                       [NotNullWhen(false)] out String?                   ErrorResponse,
                                       S2ParserOptions?                                   Options)

            => TryParse(JSON,
                        out StorageDescription,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a storage description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="StorageDescription">The parsed storage description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomStorageDescriptionParser">A delegate to parse custom storage descriptions.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out FRBC_StorageDescription?      StorageDescription,
                                       [NotNullWhen(false)] out String?                       ErrorResponse,
                                       S2ParserOptions?                                       Options,
                                       CustomJObjectParserDelegate<FRBC_StorageDescription>?  CustomStorageDescriptionParser)
        {

            try
            {

                StorageDescription = null;

                #region diagnostic_label                      [optional]

                if (!JSON.ParseOptionalS2String("diagnostic_label",
                                                "diagnostic label",
                                                out String? diagnosticLabel,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region fill_level_label                      [optional]

                if (!JSON.ParseOptionalS2String("fill_level_label",
                                                "fill level label",
                                                out String? fillLevelLabel,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region provides_leakage_behaviour            [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("provides_leakage_behaviour",
                                                  "provides leakage behaviour",
                                                  out Boolean providesLeakageBehaviour,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region provides_fill_level_target_profile    [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("provides_fill_level_target_profile",
                                                  "provides fill level target profile",
                                                  out Boolean providesFillLevelTargetProfile,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region provides_usage_forecast               [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("provides_usage_forecast",
                                                  "provides usage forecast",
                                                  out Boolean providesUsageForecast,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region fill_level_range                      [mandatory]

                if (!JSON.ParseMandatoryS2("fill_level_range",
                                           "fill level range",
                                           NumberRange.TryParse,
                                           Options,
                                           out NumberRange? fillLevelRange,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "diagnostic_label",
                                                    "fill_level_label",
                                                    "provides_leakage_behaviour",
                                                    "provides_fill_level_target_profile",
                                                    "provides_usage_forecast",
                                                    "fill_level_range"))
                {
                    return false;
                }

                #endregion


                StorageDescription = new FRBC_StorageDescription(
                                         providesLeakageBehaviour,
                                         providesFillLevelTargetProfile,
                                         providesUsageForecast,
                                         fillLevelRange,
                                         diagnosticLabel,
                                         fillLevelLabel
                                     );

                if (CustomStorageDescriptionParser is not null)
                    StorageDescription = CustomStorageDescriptionParser(JSON,
                                                                        StorageDescription);

                return true;

            }
            catch (Exception e)
            {
                StorageDescription  = null;
                ErrorResponse       = "The given JSON representation of a storage description is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomStorageDescriptionSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomStorageDescriptionSerializer">A delegate to serialize custom storage descriptions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<FRBC_StorageDescription>? CustomStorageDescriptionSerializer = null)
        {

            var json = JSONObject.Create(

                           DiagnosticLabel is not null
                               ? new JProperty("diagnostic_label",                    DiagnosticLabel)
                               : null,

                           FillLevelLabel is not null
                               ? new JProperty("fill_level_label",                    FillLevelLabel)
                               : null,

                                 new JProperty("provides_leakage_behaviour",          ProvidesLeakageBehaviour),
                                 new JProperty("provides_fill_level_target_profile",  ProvidesFillLevelTargetProfile),
                                 new JProperty("provides_usage_forecast",             ProvidesUsageForecast),
                                 new JProperty("fill_level_range",                    FillLevelRange.ToJSON())

                       );

            return CustomStorageDescriptionSerializer is not null
                       ? CustomStorageDescriptionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this storage description.
        /// </summary>
        public FRBC_StorageDescription Clone()

            => new (
                   ProvidesLeakageBehaviour,
                   ProvidesFillLevelTargetProfile,
                   ProvidesUsageForecast,
                   FillLevelRange.  Clone(),
                   DiagnosticLabel?.CloneString(),
                   FillLevelLabel?. CloneString()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two storage descriptions for equality.
        /// </summary>
        public static Boolean operator == (FRBC_StorageDescription? StorageDescription1,
                                           FRBC_StorageDescription? StorageDescription2)
        {

            if (ReferenceEquals(StorageDescription1, StorageDescription2))
                return true;

            if (StorageDescription1 is null || StorageDescription2 is null)
                return false;

            return StorageDescription1.Equals(StorageDescription2);

        }

        /// <summary>
        /// Compares two storage descriptions for inequality.
        /// </summary>
        public static Boolean operator != (FRBC_StorageDescription? StorageDescription1,
                                           FRBC_StorageDescription? StorageDescription2)
            => !(StorageDescription1 == StorageDescription2);

        #endregion

        #region IEquatable<FRBC_StorageDescription> Members

        /// <summary>
        /// Compares two storage descriptions for equality.
        /// </summary>
        /// <param name="Object">A storage description to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is FRBC_StorageDescription storageDescription && Equals(storageDescription);

        /// <summary>
        /// Compares two storage descriptions for equality.
        /// </summary>
        /// <param name="StorageDescription">A storage description to compare with.</param>
        public Boolean Equals(FRBC_StorageDescription? StorageDescription)

            => StorageDescription is not null &&

               String.Equals(DiagnosticLabel, StorageDescription.DiagnosticLabel, StringComparison.Ordinal) &&
               String.Equals(FillLevelLabel,  StorageDescription.FillLevelLabel,  StringComparison.Ordinal) &&

               ProvidesLeakageBehaviour.      Equals(StorageDescription.ProvidesLeakageBehaviour)       &&
               ProvidesFillLevelTargetProfile.Equals(StorageDescription.ProvidesFillLevelTargetProfile) &&
               ProvidesUsageForecast.         Equals(StorageDescription.ProvidesUsageForecast)          &&
               FillLevelRange.                Equals(StorageDescription.FillLevelRange);

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

                   DiagnosticLabel is not null
                       ? $"{DiagnosticLabel}: "
                       : "",

                   $"fill level {FillLevelRange}",

                   FillLevelLabel is not null
                       ? $" {FillLevelLabel}"
                       : "",

                   ProvidesLeakageBehaviour
                       ? ", leakage behaviour"
                       : "",

                   ProvidesFillLevelTargetProfile
                       ? ", fill level target profile"
                       : "",

                   ProvidesUsageForecast
                       ? ", usage forecast"
                       : ""

               );

        #endregion

    }

}

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
    /// The status of a PPBC power sequence container: which power sequence was selected,
    /// how far it has progressed and in which state it is.
    /// </summary>
    public sealed class PPBC_PowerSequenceContainerStatus : IEquatable<PPBC_PowerSequenceContainerStatus>
    {

        #region Properties

        /// <summary>
        /// The identification of the PPBC.PowerProfileDefinition of which the
        /// sequence container identification refers to.
        /// </summary>
        [Mandatory]
        public PowerProfileDefinition_Id    PowerProfileId         { get; }

        /// <summary>
        /// The identification of the PPBC.PowerSequenceContainer this status provides information about.
        /// </summary>
        [Mandatory]
        public PowerSequenceContainer_Id    SequenceContainerId    { get; }

        /// <summary>
        /// The optional identification of the selected PPBC.PowerSequence.
        /// When no identification is given, no sequence was selected yet.
        /// </summary>
        [Optional]
        public PowerSequence_Id?            SelectedSequenceId     { get; }

        /// <summary>
        /// The optional time that has passed since the selected sequence has started.
        /// A value must be provided, unless no sequence has been selected or the
        /// selected sequence has not started yet.
        /// </summary>
        [Optional]
        public Duration?                    Progress               { get; }

        /// <summary>
        /// The status of the selected PPBC.PowerSequence.
        /// </summary>
        [Mandatory]
        public PPBC_PowerSequenceStatus     Status                 { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new PPBC power sequence container status.
        /// </summary>
        /// <param name="PowerProfileId">The identification of the PPBC.PowerProfileDefinition of which the sequence container identification refers to.</param>
        /// <param name="SequenceContainerId">The identification of the PPBC.PowerSequenceContainer this status provides information about.</param>
        /// <param name="Status">The status of the selected PPBC.PowerSequence.</param>
        /// <param name="SelectedSequenceId">An optional identification of the selected PPBC.PowerSequence; absent when no sequence was selected yet.</param>
        /// <param name="Progress">An optional time that has passed since the selected sequence has started.</param>
        public PPBC_PowerSequenceContainerStatus(PowerProfileDefinition_Id  PowerProfileId,
                                                 PowerSequenceContainer_Id  SequenceContainerId,
                                                 PPBC_PowerSequenceStatus   Status,
                                                 PowerSequence_Id?          SelectedSequenceId   = null,
                                                 Duration?                  Progress             = null)
        {

            this.PowerProfileId       = PowerProfileId;
            this.SequenceContainerId  = SequenceContainerId;
            this.Status               = Status;
            this.SelectedSequenceId   = SelectedSequenceId;
            this.Progress             = Progress;

            unchecked
            {
                hashCode = this.PowerProfileId.     GetHashCode()  * 11 ^
                           this.SequenceContainerId.GetHashCode()  *  7 ^
                          (this.SelectedSequenceId?.GetHashCode() ?? 0) *  5 ^
                          (this.Progress?.          GetHashCode() ?? 0) *  3 ^
                           this.Status.             GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // PPBC.PowerSequenceContainerStatus.schema.json
        //   "title": "PPBC_PowerSequenceContainerStatus",
        //   "properties": {
        //     "power_profile_id":      { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the PPBC.PowerProfileDefinition of which the data element
        //                                                ‘sequence_container_id’ refers to. " },
        //     "sequence_container_id": { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of the PPBC.PowerSequenceContainer this PPBC.PowerSequenceContainerStatus
        //                                                provides information about." },
        //     "selected_sequence_id":  { "$ref": "../schemas/ID.schema.json",
        //                                "description": "ID of selected PPBC.PowerSequence. When no ID is given, no sequence was selected yet." },
        //     "progress":              { "$ref": "../schemas/Duration.schema.json",
        //                                "description": "Time that has passed since the selected sequence has started. A value must be
        //                                                provided, unless no sequence has been selected or the selected sequence hasn’t
        //                                                started yet." },
        //     "status":                { "$ref": "../schemas/PPBC.PowerSequenceStatus.schema.json",
        //                                "description": "Status of the selected PPBC.PowerSequence" }
        //   },
        //   "required": ["power_profile_id", "sequence_container_id", "status"],
        //   "additionalProperties": false
        //
        // The references to the power profile definition, the power sequence container and the
        // selected power sequence are cross-message rules and are validated by the session layer.

        #endregion

        #region (static) TryParse(JSON, out PPBC_PowerSequenceContainerStatus, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power sequence container status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerSequenceContainerStatus">The parsed PPBC power sequence container status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                      JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerSequenceContainerStatus?  PPBC_PowerSequenceContainerStatus,
                                       [NotNullWhen(false)] out String?                             ErrorResponse)

            => TryParse(JSON,
                        out PPBC_PowerSequenceContainerStatus,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power sequence container status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerSequenceContainerStatus">The parsed PPBC power sequence container status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                      JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerSequenceContainerStatus?  PPBC_PowerSequenceContainerStatus,
                                       [NotNullWhen(false)] out String?                             ErrorResponse,
                                       S2ParserOptions?                                             Options)

            => TryParse(JSON,
                        out PPBC_PowerSequenceContainerStatus,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power sequence container status.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerSequenceContainerStatus">The parsed PPBC power sequence container status.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPPBC_PowerSequenceContainerStatusParser">A delegate to parse custom PPBC power sequence container status.</param>
        public static Boolean TryParse(JObject                                                          JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerSequenceContainerStatus?      PPBC_PowerSequenceContainerStatus,
                                       [NotNullWhen(false)] out String?                                 ErrorResponse,
                                       S2ParserOptions?                                                 Options,
                                       CustomJObjectParserDelegate<PPBC_PowerSequenceContainerStatus>?  CustomPPBC_PowerSequenceContainerStatusParser)
        {

            try
            {

                PPBC_PowerSequenceContainerStatus = null;

                #region power_profile_id         [mandatory]

                if (!JSON.ParseMandatoryS2Id("power_profile_id",
                                             "power profile definition identification",
                                             PowerProfileDefinition_Id.TryParse,
                                             Options,
                                             out PowerProfileDefinition_Id powerProfileId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region sequence_container_id    [mandatory]

                if (!JSON.ParseMandatoryS2Id("sequence_container_id",
                                             "power sequence container identification",
                                             PowerSequenceContainer_Id.TryParse,
                                             Options,
                                             out PowerSequenceContainer_Id sequenceContainerId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region selected_sequence_id     [optional]

                if (!JSON.ParseOptionalS2Id("selected_sequence_id",
                                            "selected power sequence identification",
                                            PowerSequence_Id.TryParse,
                                            Options,
                                            out PowerSequence_Id? selectedSequenceId,
                                            out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region progress                 [optional]

                if (!JSON.ParseOptionalS2Duration("progress",
                                                  "progress",
                                                  out Duration? progress,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region status                   [mandatory]

                if (!JSON.ParseMandatoryS2Enum("status",
                                               "power sequence status",
                                               PPBC_PowerSequenceStatus.TryParse,
                                               Options,
                                               out PPBC_PowerSequenceStatus status,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "power_profile_id",
                                                    "sequence_container_id",
                                                    "selected_sequence_id",
                                                    "progress",
                                                    "status"))
                {
                    return false;
                }

                #endregion


                PPBC_PowerSequenceContainerStatus = new PPBC_PowerSequenceContainerStatus(
                                                        powerProfileId,
                                                        sequenceContainerId,
                                                        status,
                                                        selectedSequenceId,
                                                        progress
                                                    );

                if (CustomPPBC_PowerSequenceContainerStatusParser is not null)
                    PPBC_PowerSequenceContainerStatus = CustomPPBC_PowerSequenceContainerStatusParser(JSON,
                                                                                                      PPBC_PowerSequenceContainerStatus);

                return true;

            }
            catch (Exception e)
            {
                PPBC_PowerSequenceContainerStatus  = null;
                ErrorResponse                      = "The given JSON representation of a PPBC power sequence container status is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPPBC_PowerSequenceContainerStatusSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPPBC_PowerSequenceContainerStatusSerializer">A delegate to serialize custom PPBC power sequence container status.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PPBC_PowerSequenceContainerStatus>? CustomPPBC_PowerSequenceContainerStatusSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("power_profile_id",       PowerProfileId.     ToString()),
                                 new JProperty("sequence_container_id",  SequenceContainerId.ToString()),

                           SelectedSequenceId.HasValue
                               ? new JProperty("selected_sequence_id",   SelectedSequenceId.Value.ToString())
                               : null,

                           Progress.HasValue
                               ? new JProperty("progress",               Progress.Value.ToJSON())
                               : null,

                                 new JProperty("status",                 Status.ToString())

                       );

            return CustomPPBC_PowerSequenceContainerStatusSerializer is not null
                       ? CustomPPBC_PowerSequenceContainerStatusSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this PPBC power sequence container status.
        /// </summary>
        public PPBC_PowerSequenceContainerStatus Clone()

            => new (
                   PowerProfileId.     Clone(),
                   SequenceContainerId.Clone(),
                   Status.             Clone(),
                   SelectedSequenceId?.Clone(),
                   Progress
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two PPBC power sequence container status for equality.
        /// </summary>
        public static Boolean operator == (PPBC_PowerSequenceContainerStatus? PPBC_PowerSequenceContainerStatus1, PPBC_PowerSequenceContainerStatus? PPBC_PowerSequenceContainerStatus2)
        {

            if (ReferenceEquals(PPBC_PowerSequenceContainerStatus1, PPBC_PowerSequenceContainerStatus2))
                return true;

            if (PPBC_PowerSequenceContainerStatus1 is null || PPBC_PowerSequenceContainerStatus2 is null)
                return false;

            return PPBC_PowerSequenceContainerStatus1.Equals(PPBC_PowerSequenceContainerStatus2);

        }

        /// <summary>
        /// Compares two PPBC power sequence container status for inequality.
        /// </summary>
        public static Boolean operator != (PPBC_PowerSequenceContainerStatus? PPBC_PowerSequenceContainerStatus1, PPBC_PowerSequenceContainerStatus? PPBC_PowerSequenceContainerStatus2)
            => !(PPBC_PowerSequenceContainerStatus1 == PPBC_PowerSequenceContainerStatus2);

        #endregion

        #region IEquatable<PPBC_PowerSequenceContainerStatus> Members

        /// <summary>
        /// Compares two PPBC power sequence container status for equality.
        /// </summary>
        /// <param name="Object">A PPBC power sequence container status to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PPBC_PowerSequenceContainerStatus ppbcPowerSequenceContainerStatus && Equals(ppbcPowerSequenceContainerStatus);

        /// <summary>
        /// Compares two PPBC power sequence container status for equality.
        /// </summary>
        /// <param name="PPBC_PowerSequenceContainerStatus">A PPBC power sequence container status to compare with.</param>
        public Boolean Equals(PPBC_PowerSequenceContainerStatus? PPBC_PowerSequenceContainerStatus)

            => PPBC_PowerSequenceContainerStatus is not null &&

               PowerProfileId.     Equals(PPBC_PowerSequenceContainerStatus.PowerProfileId) &&
               SequenceContainerId.Equals(PPBC_PowerSequenceContainerStatus.SequenceContainerId) &&
               Status.             Equals(PPBC_PowerSequenceContainerStatus.Status) &&

               Nullable.Equals(SelectedSequenceId, PPBC_PowerSequenceContainerStatus.SelectedSequenceId) &&
               Nullable.Equals(Progress,           PPBC_PowerSequenceContainerStatus.Progress);

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

                   $"{PowerProfileId} / {SequenceContainerId}: {Status}",

                   SelectedSequenceId.HasValue
                       ? $", selected: {SelectedSequenceId.Value}"
                       : "",

                   Progress.HasValue
                       ? $", progress: {Progress.Value}"
                       : ""

               );

        #endregion

    }

}

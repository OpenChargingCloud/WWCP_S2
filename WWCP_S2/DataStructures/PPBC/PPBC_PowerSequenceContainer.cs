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
    /// A PPBC power sequence container: a list of alternative power sequences of which
    /// the CEM chooses one.
    /// </summary>
    public sealed class PPBC_PowerSequenceContainer : IEquatable<PPBC_PowerSequenceContainer>
    {

        #region Properties

        /// <summary>
        /// The identification of the power sequence container. Must be unique in the scope
        /// of the PPBC.PowerProfileDefinition in which it is used.
        /// </summary>
        [Mandatory]
        public PowerSequenceContainer_Id             Id                { get; }

        /// <summary>
        /// The list of alternative power sequences where one could be chosen by the CEM.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PPBC_PowerSequence>     PowerSequences    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new PPBC power sequence container.
        /// </summary>
        /// <param name="Id">The identification of the power sequence container (unique within its power profile definition).</param>
        /// <param name="PowerSequences">The list of alternative power sequences where one could be chosen by the CEM (1..288 entries, unique ids).</param>
        public PPBC_PowerSequenceContainer(PowerSequenceContainer_Id          Id,
                                           IReadOnlyList<PPBC_PowerSequence>  PowerSequences)
        {

            ArgumentNullException.ThrowIfNull(PowerSequences);

            if (PowerSequences.Count < 1)
                throw new ArgumentException("The power sequences must contain at least one entry!",
                                            nameof(PowerSequences));

            if (PowerSequences.Count > 288)
                throw new ArgumentException($"The power sequences must not contain more than 288 entries, but {PowerSequences.Count} were given!",
                                            nameof(PowerSequences));

            var duplicate = PowerSequences.GroupBy(powerSequence => powerSequence.Id).
                                           FirstOrDefault(group => group.Count() > 1);

            if (duplicate is not null)
                throw new ArgumentException($"The power sequence identifications must be unique within the container, but '{duplicate.Key}' occurs {duplicate.Count()} times!",
                                            nameof(PowerSequences));

            this.Id              = Id;
            this.PowerSequences  = [.. PowerSequences];

            unchecked
            {
                hashCode = this.Id.            GetHashCode() * 3 ^
                           this.PowerSequences.CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // PPBC.PowerSequenceContainer.schema.json
        //   "title": "PPBC_PowerSequenceContainer",
        //   "properties": {
        //     "id":              { "$ref": "../schemas/ID.schema.json",
        //                          "description": "ID of the PPBC.PowerSequenceContainer. Must be unique in the scope of the
        //                                          PPBC.PowerProfileDefinition in which it is used." },
        //     "power_sequences": { "type": "array", "minItems": 1, "maxItems": 288,
        //                          "items": { "$ref": "../schemas/PPBC.PowerSequence.schema.json" },
        //                          "description": "List of alternative Sequences where one could be chosen by the CEM" }
        //   },
        //   "required": ["id", "power_sequences"],
        //   "additionalProperties": false
        //
        // Semantic rule (CONVENTIONS.md §5): power sequence ids are unique within the container.

        #endregion

        #region (static) TryParse(JSON, out PPBC_PowerSequenceContainer, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power sequence container.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerSequenceContainer">The parsed PPBC power sequence container.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerSequenceContainer?  PPBC_PowerSequenceContainer,
                                       [NotNullWhen(false)] out String?                       ErrorResponse)

            => TryParse(JSON,
                        out PPBC_PowerSequenceContainer,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power sequence container.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerSequenceContainer">The parsed PPBC power sequence container.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerSequenceContainer?  PPBC_PowerSequenceContainer,
                                       [NotNullWhen(false)] out String?                       ErrorResponse,
                                       S2ParserOptions?                                       Options)

            => TryParse(JSON,
                        out PPBC_PowerSequenceContainer,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a PPBC power sequence container.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PPBC_PowerSequenceContainer">The parsed PPBC power sequence container.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPPBC_PowerSequenceContainerParser">A delegate to parse custom PPBC power sequence containers.</param>
        public static Boolean TryParse(JObject                                                    JSON,
                                       [NotNullWhen(true)]  out PPBC_PowerSequenceContainer?      PPBC_PowerSequenceContainer,
                                       [NotNullWhen(false)] out String?                           ErrorResponse,
                                       S2ParserOptions?                                           Options,
                                       CustomJObjectParserDelegate<PPBC_PowerSequenceContainer>?  CustomPPBC_PowerSequenceContainerParser)
        {

            try
            {

                PPBC_PowerSequenceContainer = null;

                #region id                 [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
                                             "power sequence container identification",
                                             PowerSequenceContainer_Id.TryParse,
                                             Options,
                                             out PowerSequenceContainer_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region power_sequences    [mandatory]

                if (!JSON.ParseMandatoryS2List("power_sequences",
                                               "power sequences",
                                               PPBC_PowerSequence.TryParse,
                                               Options,
                                               1,
                                               288,
                                               out IReadOnlyList<PPBC_PowerSequence>? powerSequences,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "id",
                                                    "power_sequences"))
                {
                    return false;
                }

                #endregion


                PPBC_PowerSequenceContainer = new PPBC_PowerSequenceContainer(
                                                  id,
                                                  powerSequences
                                              );

                if (CustomPPBC_PowerSequenceContainerParser is not null)
                    PPBC_PowerSequenceContainer = CustomPPBC_PowerSequenceContainerParser(JSON,
                                                                                          PPBC_PowerSequenceContainer);

                return true;

            }
            catch (Exception e)
            {
                PPBC_PowerSequenceContainer  = null;
                ErrorResponse                = "The given JSON representation of a PPBC power sequence container is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPPBC_PowerSequenceContainerSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPPBC_PowerSequenceContainerSerializer">A delegate to serialize custom PPBC power sequence containers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PPBC_PowerSequenceContainer>? CustomPPBC_PowerSequenceContainerSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("id",               Id.ToString()),
                           new JProperty("power_sequences",  new JArray(PowerSequences.Select(powerSequence => powerSequence.ToJSON())))
                       );

            return CustomPPBC_PowerSequenceContainerSerializer is not null
                       ? CustomPPBC_PowerSequenceContainerSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this PPBC power sequence container.
        /// </summary>
        public PPBC_PowerSequenceContainer Clone()

            => new (
                   Id.Clone(),
                   [.. PowerSequences.Select(powerSequence => powerSequence.Clone())]
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two PPBC power sequence containers for equality.
        /// </summary>
        public static Boolean operator == (PPBC_PowerSequenceContainer? PPBC_PowerSequenceContainer1, PPBC_PowerSequenceContainer? PPBC_PowerSequenceContainer2)
        {

            if (ReferenceEquals(PPBC_PowerSequenceContainer1, PPBC_PowerSequenceContainer2))
                return true;

            if (PPBC_PowerSequenceContainer1 is null || PPBC_PowerSequenceContainer2 is null)
                return false;

            return PPBC_PowerSequenceContainer1.Equals(PPBC_PowerSequenceContainer2);

        }

        /// <summary>
        /// Compares two PPBC power sequence containers for inequality.
        /// </summary>
        public static Boolean operator != (PPBC_PowerSequenceContainer? PPBC_PowerSequenceContainer1, PPBC_PowerSequenceContainer? PPBC_PowerSequenceContainer2)
            => !(PPBC_PowerSequenceContainer1 == PPBC_PowerSequenceContainer2);

        #endregion

        #region IEquatable<PPBC_PowerSequenceContainer> Members

        /// <summary>
        /// Compares two PPBC power sequence containers for equality.
        /// </summary>
        /// <param name="Object">A PPBC power sequence container to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PPBC_PowerSequenceContainer ppbcPowerSequenceContainer && Equals(ppbcPowerSequenceContainer);

        /// <summary>
        /// Compares two PPBC power sequence containers for equality.
        /// </summary>
        /// <param name="PPBC_PowerSequenceContainer">A PPBC power sequence container to compare with.</param>
        public Boolean Equals(PPBC_PowerSequenceContainer? PPBC_PowerSequenceContainer)

            => PPBC_PowerSequenceContainer is not null &&

               Id.            Equals       (PPBC_PowerSequenceContainer.Id) &&
               PowerSequences.SequenceEqual(PPBC_PowerSequenceContainer.PowerSequences);

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

            => $"{Id}: {PowerSequences.Count} power sequence(s)";

        #endregion

    }

}

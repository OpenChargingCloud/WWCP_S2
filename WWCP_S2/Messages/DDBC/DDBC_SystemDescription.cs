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
    /// The Resource Manager describes its Demand Driven Based Control system: the actuators
    /// with their operation modes, transitions and timers, and whether it can provide an
    /// average demand rate forecast. The system description is revoked by its message_id.
    /// </summary>
    public sealed class DDBC_SystemDescription : AS2Message,
                                                 IRevokable,
                                                 IEquatable<DDBC_SystemDescription>
    {

        #region Data

        /// <summary>
        /// The message type "DDBC.SystemDescription".
        /// </summary>
        public const String MessageTypeName = "DDBC.SystemDescription";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "DDBC.SystemDescription".
        /// </summary>
        public override String                             MessageType
            => MessageTypeName;

        /// <summary>
        /// The moment this DDBC.SystemDescription starts to be valid. If the system description
        /// is immediately valid, the timestamp should be now or in the past.
        /// </summary>
        [Mandatory]
        public DateTimeOffset                              ValidFrom                            { get; }

        /// <summary>
        /// The list of all available actuators in the system (1..10, unique identifications).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<DDBC_ActuatorDescription>     Actuators                            { get; }

        /// <summary>
        /// Whether the Resource Manager could provide a demand rate forecast through the
        /// DDBC.AverageDemandRateForecast.
        /// </summary>
        [Mandatory]
        public Boolean                                     ProvidesAverageDemandRateForecast    { get; }

        /// <summary>
        /// The revokable object type "DDBC.SystemDescription".
        /// </summary>
        public RevokableObject                             RevokableObjectType
            => RevokableObject.DDBC_SystemDescription;

        /// <summary>
        /// The identification a RevokeObject uses to refer to this message: its message_id,
        /// as the system description carries no "id" of its own.
        /// </summary>
        public S2Object_Id                                 RevokableObjectId
            => S2Object_Id.From(MessageId);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new DDBC system description.
        /// </summary>
        /// <param name="ValidFrom">The moment this system description starts to be valid.</param>
        /// <param name="Actuators">The list of all available actuators in the system (1..10, unique identifications).</param>
        /// <param name="ProvidesAverageDemandRateForecast">Whether the Resource Manager could provide a DDBC.AverageDemandRateForecast.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public DDBC_SystemDescription(DateTimeOffset                            ValidFrom,
                                      IReadOnlyList<DDBC_ActuatorDescription>   Actuators,
                                      Boolean                                   ProvidesAverageDemandRateForecast,
                                      Message_Id?                               MessageId   = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(Actuators);

            if (Actuators.Count < 1)
                throw new ArgumentException("The list of actuators must contain at least one item!",
                                            nameof(Actuators));

            if (Actuators.Count > 10)
                throw new ArgumentException("The list of actuators must not contain more than 10 items!",
                                            nameof(Actuators));

            if (Actuators.Select(actuator => actuator.Id).Distinct().Count() != Actuators.Count)
                throw new ArgumentException("The identifications of the actuators must be unique!",
                                            nameof(Actuators));

            this.ValidFrom                          = ValidFrom;
            this.Actuators                          = [.. Actuators];
            this.ProvidesAverageDemandRateForecast  = ProvidesAverageDemandRateForecast;

            unchecked
            {
                hashCode = this.MessageId.                        GetHashCode()  * 7 ^
                           this.ValidFrom.                        GetHashCode()  * 5 ^
                           this.Actuators.                        CalcHashCode() * 3 ^
                           this.ProvidesAverageDemandRateForecast.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // DDBC.SystemDescription.schema.json
        //   "title": "DDBC_SystemDescription",
        //   "properties": {
        //     "message_type": { "type": "string", "const": "DDBC.SystemDescription" },
        //     "message_id":   { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "valid_from":   { "type": "string", "format": "date-time",
        //                       "description": "Moment this DDBC.SystemDescription starts to be valid. If the system
        //                                       description is immediately valid, the DateTimeStamp should be now or in the past." },
        //     "actuators":    { "type": "array", "minItems": 1, "maxItems": 10,
        //                       "items": { "$ref": "../schemas/DDBC.ActuatorDescription.schema.json" },
        //                       "description": "List of all available actuators in the system. Must contain at least one
        //                                       DDBC.ActuatorAggregated." },
        //     "provides_average_demand_rate_forecast": {
        //                       "type": "boolean",
        //                       "description": "Indicates whether the Resource Manager could provide a demand rate forecast
        //                                       through the DDBC.AverageDemandRateForecast." }
        //   },
        //   "required": ["message_type", "message_id", "valid_from", "actuators", "provides_average_demand_rate_forecast"],
        //   "additionalProperties": false
        //
        // Semantic rules (CONVENTIONS.md §8):
        //   - The identifications of the actuators must be unique.
        //   - Revoked via RevokeObject by its message_id (RevokableObject "DDBC.SystemDescription").

        #endregion

        #region (static) TryParse(JSON, out DDBCSystemDescription, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a DDBC system description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCSystemDescription">The parsed DDBC system description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out DDBC_SystemDescription?  DDBCSystemDescription,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)

            => TryParse(JSON,
                        out DDBCSystemDescription,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC system description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCSystemDescription">The parsed DDBC system description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out DDBC_SystemDescription?  DDBCSystemDescription,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options)

            => TryParse(JSON,
                        out DDBCSystemDescription,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a DDBC system description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DDBCSystemDescription">The parsed DDBC system description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomDDBCSystemDescriptionParser">A delegate to parse custom DDBC system descriptions.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out DDBC_SystemDescription?      DDBCSystemDescription,
                                       [NotNullWhen(false)] out String?                      ErrorResponse,
                                       S2ParserOptions?                                      Options,
                                       CustomJObjectParserDelegate<DDBC_SystemDescription>?  CustomDDBCSystemDescriptionParser)
        {

            try
            {

                DDBCSystemDescription = null;

                #region message_type, message_id                  [mandatory]

                if (!TryParseHeader(JSON,
                                    MessageTypeName,
                                    Options,
                                    out var messageId,
                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region valid_from                                [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("valid_from",
                                                    "valid from timestamp",
                                                    Options,
                                                    out DateTimeOffset validFrom,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region actuators                                 [mandatory]

                if (!JSON.ParseMandatoryS2List("actuators",
                                               "actuator descriptions",
                                               DDBC_ActuatorDescription.TryParse,
                                               Options,
                                               1,
                                               10,
                                               out IReadOnlyList<DDBC_ActuatorDescription>? actuators,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region provides_average_demand_rate_forecast     [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("provides_average_demand_rate_forecast",
                                                  "provides average demand rate forecast",
                                                  out Boolean providesAverageDemandRateForecast,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "message_type",
                                                    "message_id",
                                                    "valid_from",
                                                    "actuators",
                                                    "provides_average_demand_rate_forecast"))
                {
                    return false;
                }

                #endregion


                DDBCSystemDescription = new DDBC_SystemDescription(
                                            validFrom,
                                            actuators,
                                            providesAverageDemandRateForecast,
                                            messageId
                                        );

                if (CustomDDBCSystemDescriptionParser is not null)
                    DDBCSystemDescription = CustomDDBCSystemDescriptionParser(JSON,
                                                                              DDBCSystemDescription);

                return true;

            }
            catch (Exception e)
            {
                DDBCSystemDescription  = null;
                ErrorResponse          = "The given JSON representation of a DDBC system description is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDDBCSystemDescriptionSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomDDBCSystemDescriptionSerializer">A delegate to serialize custom DDBC system descriptions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DDBC_SystemDescription>? CustomDDBCSystemDescriptionSerializer)
        {

            var json = CreateJSON(
                           new JProperty("valid_from",                             ValidFrom.ToS2Timestamp()),
                           new JProperty("actuators",                              new JArray(Actuators.Select(actuator => actuator.ToJSON()))),
                           new JProperty("provides_average_demand_rate_forecast",  ProvidesAverageDemandRateForecast)
                       );

            return CustomDDBCSystemDescriptionSerializer is not null
                       ? CustomDDBCSystemDescriptionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this DDBC system description.
        /// </summary>
        public DDBC_SystemDescription Clone()

            => new (
                   ValidFrom,
                   Actuators.Select(actuator => actuator.Clone()).ToList(),
                   ProvidesAverageDemandRateForecast,
                   MessageId.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two DDBC system descriptions for equality.
        /// </summary>
        public static Boolean operator == (DDBC_SystemDescription? DDBCSystemDescription1, DDBC_SystemDescription? DDBCSystemDescription2)
        {

            if (ReferenceEquals(DDBCSystemDescription1, DDBCSystemDescription2))
                return true;

            if (DDBCSystemDescription1 is null || DDBCSystemDescription2 is null)
                return false;

            return DDBCSystemDescription1.Equals(DDBCSystemDescription2);

        }

        /// <summary>
        /// Compares two DDBC system descriptions for inequality.
        /// </summary>
        public static Boolean operator != (DDBC_SystemDescription? DDBCSystemDescription1, DDBC_SystemDescription? DDBCSystemDescription2)
            => !(DDBCSystemDescription1 == DDBCSystemDescription2);

        #endregion

        #region IEquatable<DDBC_SystemDescription> Members

        /// <summary>
        /// Compares two DDBC system descriptions for equality.
        /// </summary>
        /// <param name="Object">A DDBC system description to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is DDBC_SystemDescription ddbcSystemDescription && Equals(ddbcSystemDescription);

        /// <summary>
        /// Compares two DDBC system descriptions for equality.
        /// </summary>
        /// <param name="DDBCSystemDescription">A DDBC system description to compare with.</param>
        public Boolean Equals(DDBC_SystemDescription? DDBCSystemDescription)

            => DDBCSystemDescription is not null &&

               MessageId.                        Equals(DDBCSystemDescription.MessageId)                         &&
               ValidFrom.                        Equals(DDBCSystemDescription.ValidFrom)                         &&
               Actuators.                        SequenceEqual(DDBCSystemDescription.Actuators)                  &&
               ProvidesAverageDemandRateForecast.Equals(DDBCSystemDescription.ProvidesAverageDemandRateForecast);

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

                   $"DDBC.SystemDescription valid from {ValidFrom.ToS2Timestamp()} with {Actuators.Count} actuator(s)",

                   ProvidesAverageDemandRateForecast
                       ? ", provides average demand rate forecast"
                       : "",

                   $" [{MessageId}]"

               );

        #endregion

    }

}

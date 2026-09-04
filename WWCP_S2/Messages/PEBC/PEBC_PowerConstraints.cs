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
    /// The Resource Manager specifies the constraints in which the CEM can operate
    /// (Power Envelope Based Control): for every commodity quantity the allowed ranges
    /// of the upper and lower limits of a power envelope, the consequence of limiting
    /// power and the period of validity.
    /// </summary>
    public sealed class PEBC_PowerConstraints : AS2Message,
                                                IRevokable,
                                                IEquatable<PEBC_PowerConstraints>
    {

        #region Data

        /// <summary>
        /// The message type "PEBC.PowerConstraints".
        /// </summary>
        public const String MessageTypeName = "PEBC.PowerConstraints";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "PEBC.PowerConstraints".
        /// </summary>
        public override String                         MessageType
            => MessageTypeName;

        /// <summary>
        /// The identification of this power constraints message. Must be unique in the scope
        /// of the Resource Manager, for at least the duration of the session between Resource
        /// Manager and CEM.
        /// </summary>
        [Mandatory]
        public PowerConstraints_Id                     Id                     { get; }

        /// <summary>
        /// The moment these power constraints start to be valid.
        /// </summary>
        [Mandatory]
        public DateTimeOffset                          ValidFrom              { get; }

        /// <summary>
        /// The moment until these power constraints are valid. When not present, there is
        /// no determined end time of these power constraints.
        /// </summary>
        [Optional]
        public DateTimeOffset?                         ValidUntil             { get; }

        /// <summary>
        /// The type of consequence of limiting power.
        /// </summary>
        [Mandatory]
        public PEBC_PowerEnvelopeConsequenceType       ConsequenceType        { get; }

        /// <summary>
        /// The actual constraints (2..100). There shall be at least one allowed limit range for
        /// the UPPER_LIMIT and at least one allowed limit range for the LOWER_LIMIT of every
        /// commodity quantity. It is allowed to have multiple allowed limit ranges with identical
        /// commodity quantities and limit types.
        /// </summary>
        [Mandatory]
        public IReadOnlyList<PEBC_AllowedLimitRange>   AllowedLimitRanges     { get; }

        /// <summary>
        /// The revokable object type of this message: PEBC.PowerConstraints.
        /// </summary>
        public RevokableObject                         RevokableObjectType
            => RevokableObject.PEBC_PowerConstraints;

        /// <summary>
        /// The identification a RevokeObject uses to refer to this message: its "id".
        /// </summary>
        public S2Object_Id                             RevokableObjectId
            => S2Object_Id.From(Id);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power constraints message.
        /// </summary>
        /// <param name="Id">The identification of this power constraints message.</param>
        /// <param name="ValidFrom">The moment these power constraints start to be valid.</param>
        /// <param name="ConsequenceType">The type of consequence of limiting power.</param>
        /// <param name="AllowedLimitRanges">The actual constraints (2..100, at least one UPPER_LIMIT and one LOWER_LIMIT range per commodity quantity).</param>
        /// <param name="ValidUntil">The optional moment until these power constraints are valid (not before ValidFrom).</param>
        /// <param name="MessageId">An optional message identification.</param>
        public PEBC_PowerConstraints(PowerConstraints_Id                    Id,
                                     DateTimeOffset                         ValidFrom,
                                     PEBC_PowerEnvelopeConsequenceType      ConsequenceType,
                                     IReadOnlyList<PEBC_AllowedLimitRange>  AllowedLimitRanges,
                                     DateTimeOffset?                        ValidUntil   = null,
                                     Message_Id?                            MessageId    = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(AllowedLimitRanges);

            if (AllowedLimitRanges.Count < 2)
                throw new ArgumentException($"The allowed limit ranges must contain at least 2 items, but {AllowedLimitRanges.Count} were given!",
                                            nameof(AllowedLimitRanges));

            if (AllowedLimitRanges.Count > 100)
                throw new ArgumentException($"The allowed limit ranges must not contain more than 100 items, but {AllowedLimitRanges.Count} were given!",
                                            nameof(AllowedLimitRanges));

            if (AllowedLimitRanges.Any(range => range is null))
                throw new ArgumentException("The allowed limit ranges must not contain null items!",
                                            nameof(AllowedLimitRanges));

            foreach (var rangesOfQuantity in AllowedLimitRanges.GroupBy(range => range.CommodityQuantity))
            {

                if (!rangesOfQuantity.Any(range => range.LimitType == PEBC_PowerEnvelopeLimitType.UpperLimit))
                    throw new ArgumentException($"The allowed limit ranges must contain at least one UPPER_LIMIT range for commodity quantity '{rangesOfQuantity.Key}'!",
                                                nameof(AllowedLimitRanges));

                if (!rangesOfQuantity.Any(range => range.LimitType == PEBC_PowerEnvelopeLimitType.LowerLimit))
                    throw new ArgumentException($"The allowed limit ranges must contain at least one LOWER_LIMIT range for commodity quantity '{rangesOfQuantity.Key}'!",
                                                nameof(AllowedLimitRanges));

            }

            if (ValidUntil.HasValue && ValidUntil.Value < ValidFrom)
                throw new ArgumentException($"The 'valid_until' timestamp ({ValidUntil.Value.ToS2Timestamp()}) must not be before the 'valid_from' timestamp ({ValidFrom.ToS2Timestamp()})!",
                                            nameof(ValidUntil));

            this.Id                  = Id;
            this.ValidFrom           = ValidFrom;
            this.ValidUntil          = ValidUntil;
            this.ConsequenceType     = ConsequenceType;
            this.AllowedLimitRanges  = [.. AllowedLimitRanges];

            unchecked
            {
                hashCode = this.MessageId.         GetHashCode()  * 13 ^
                           this.Id.                GetHashCode()  * 11 ^
                           this.ValidFrom.         GetHashCode()  *  7 ^
                          (this.ValidUntil?.       GetHashCode() ?? 0) * 5 ^
                           this.ConsequenceType.   GetHashCode()  *  3 ^
                           this.AllowedLimitRanges.CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // PEBC.PowerConstraints.schema.json
        //   "title": "PEBC_PowerConstraints",
        //   "properties": {
        //     "message_type":     { "type": "string", "const": "PEBC.PowerConstraints" },
        //     "message_id":       { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "id":               { "$ref": "../schemas/ID.schema.json",
        //                           "description": "Identifier of this PEBC.PowerConstraints. Must be unique in the scope of the
        //                                           Resource Manager, for at least the duration of the session between Resource
        //                                           Manager and CEM." },
        //     "valid_from":       { "type": "string", "format": "date-time",
        //                           "description": "Moment this PEBC.PowerConstraints start to be valid" },
        //     "valid_until":      { "type": "string", "format": "date-time",
        //                           "description": "Moment until this PEBC.PowerConstraints is valid. If valid_until is not present,
        //                                           there is no determined end time of this PEBC.PowerConstraints." },
        //     "consequence_type": { "$ref": "../schemas/PEBC.PowerEnvelopeConsequenceType.schema.json",
        //                           "description": "Type of consequence of limiting power" },
        //     "allowed_limit_ranges": {
        //       "type": "array", "minItems": 2, "maxItems": 100,
        //       "items": { "$ref": "../schemas/PEBC.AllowedLimitRange.schema.json" },
        //       "description": "The actual constraints. There shall be at least one PEBC.AllowedLimitRange for the UPPER_LIMIT and
        //                       at least one AllowedLimitRange for the LOWER_LIMIT. It is allowed to have multiple PEBC.AllowedLimitRange
        //                       objects with identical CommodityQuantities and LimitTypes."
        //     }
        //   },
        //   "required": ["message_type", "message_id", "id", "valid_from", "consequence_type", "allowed_limit_ranges"],
        //   "additionalProperties": false
        //
        // Semantic rules (CONVENTIONS.md §8):
        //   - allowed_limit_ranges: 2..100 items, at least one UPPER_LIMIT and one LOWER_LIMIT range per CommodityQuantity.
        //   - valid_until >= valid_from when valid_until is present.

        #endregion

        #region (static) TryParse(JSON, out PowerConstraints, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of a power constraints message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerConstraints">The parsed power constraints message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out PEBC_PowerConstraints?  PowerConstraints,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)

            => TryParse(JSON,
                        out PowerConstraints,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power constraints message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerConstraints">The parsed power constraints message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out PEBC_PowerConstraints?  PowerConstraints,
                                       [NotNullWhen(false)] out String?                 ErrorResponse,
                                       S2ParserOptions?                                 Options)

            => TryParse(JSON,
                        out PowerConstraints,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power constraints message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerConstraints">The parsed power constraints message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomPowerConstraintsParser">A delegate to parse custom power constraints messages.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out PEBC_PowerConstraints?      PowerConstraints,
                                       [NotNullWhen(false)] out String?                     ErrorResponse,
                                       S2ParserOptions?                                     Options,
                                       CustomJObjectParserDelegate<PEBC_PowerConstraints>?  CustomPowerConstraintsParser)
        {

            try
            {

                PowerConstraints = null;

                #region message_type, message_id   [mandatory]

                if (!TryParseHeader(JSON,
                                    MessageTypeName,
                                    Options,
                                    out var messageId,
                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region id                         [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
                                             "power constraints identification",
                                             PowerConstraints_Id.TryParse,
                                             Options,
                                             out PowerConstraints_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region valid_from                 [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("valid_from",
                                                    "valid from timestamp",
                                                    Options,
                                                    out DateTimeOffset validFrom,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region valid_until                [optional]

                if (!JSON.ParseOptionalS2Timestamp("valid_until",
                                                   "valid until timestamp",
                                                   Options,
                                                   out DateTimeOffset? validUntil,
                                                   out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region consequence_type           [mandatory]

                if (!JSON.ParseMandatoryS2Enum("consequence_type",
                                               "power envelope consequence type",
                                               PEBC_PowerEnvelopeConsequenceType.TryParse,
                                               Options,
                                               out PEBC_PowerEnvelopeConsequenceType consequenceType,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region allowed_limit_ranges       [mandatory]

                if (!JSON.ParseMandatoryS2List("allowed_limit_ranges",
                                               "allowed limit ranges",
                                               PEBC_AllowedLimitRange.TryParse,
                                               Options,
                                               2,
                                               100,
                                               out IReadOnlyList<PEBC_AllowedLimitRange>? allowedLimitRanges,
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
                                                    "id",
                                                    "valid_from",
                                                    "valid_until",
                                                    "consequence_type",
                                                    "allowed_limit_ranges"))
                {
                    return false;
                }

                #endregion


                PowerConstraints = new PEBC_PowerConstraints(
                                       id,
                                       validFrom,
                                       consequenceType,
                                       allowedLimitRanges,
                                       validUntil,
                                       messageId
                                   );

                if (CustomPowerConstraintsParser is not null)
                    PowerConstraints = CustomPowerConstraintsParser(JSON,
                                                                    PowerConstraints);

                return true;

            }
            catch (Exception e)
            {
                PowerConstraints  = null;
                ErrorResponse     = "The given JSON representation of a PEBC power constraints message is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerConstraintsSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomPowerConstraintsSerializer">A delegate to serialize custom power constraints messages.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PEBC_PowerConstraints>? CustomPowerConstraintsSerializer)
        {

            var json = CreateJSON(

                                 new JProperty("id",                    Id.       ToString()),
                                 new JProperty("valid_from",            ValidFrom.ToS2Timestamp()),

                           ValidUntil.HasValue
                               ? new JProperty("valid_until",           ValidUntil.Value.ToS2Timestamp())
                               : null,

                                 new JProperty("consequence_type",      ConsequenceType.ToString()),
                                 new JProperty("allowed_limit_ranges",  new JArray(AllowedLimitRanges.Select(range => range.ToJSON())))

                       );

            return CustomPowerConstraintsSerializer is not null
                       ? CustomPowerConstraintsSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this power constraints message.
        /// </summary>
        public PEBC_PowerConstraints Clone()

            => new (
                   Id.             Clone(),
                   ValidFrom,
                   ConsequenceType.Clone(),
                   [.. AllowedLimitRanges.Select(range => range.Clone())],
                   ValidUntil,
                   MessageId.      Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two power constraints messages for equality.
        /// </summary>
        public static Boolean operator == (PEBC_PowerConstraints? PowerConstraints1, PEBC_PowerConstraints? PowerConstraints2)
        {

            if (ReferenceEquals(PowerConstraints1, PowerConstraints2))
                return true;

            if (PowerConstraints1 is null || PowerConstraints2 is null)
                return false;

            return PowerConstraints1.Equals(PowerConstraints2);

        }

        /// <summary>
        /// Compares two power constraints messages for inequality.
        /// </summary>
        public static Boolean operator != (PEBC_PowerConstraints? PowerConstraints1, PEBC_PowerConstraints? PowerConstraints2)
            => !(PowerConstraints1 == PowerConstraints2);

        #endregion

        #region IEquatable<PEBC_PowerConstraints> Members

        /// <summary>
        /// Compares two power constraints messages for equality.
        /// </summary>
        /// <param name="Object">A power constraints message to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PEBC_PowerConstraints powerConstraints && Equals(powerConstraints);

        /// <summary>
        /// Compares two power constraints messages for equality.
        /// </summary>
        /// <param name="PowerConstraints">A power constraints message to compare with.</param>
        public Boolean Equals(PEBC_PowerConstraints? PowerConstraints)

            => PowerConstraints is not null &&

               MessageId.      Equals(PowerConstraints.MessageId)       &&
               Id.             Equals(PowerConstraints.Id)              &&
               ValidFrom.      Equals(PowerConstraints.ValidFrom)       &&
               Nullable.Equals(ValidUntil, PowerConstraints.ValidUntil) &&
               ConsequenceType.Equals(PowerConstraints.ConsequenceType) &&

               AllowedLimitRanges.SequenceEqual(PowerConstraints.AllowedLimitRanges);

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

                   $"PEBC.PowerConstraints '{Id}' ({ConsequenceType}) valid from {ValidFrom.ToS2Timestamp()}",

                   ValidUntil.HasValue
                       ? $" until {ValidUntil.Value.ToS2Timestamp()}"
                       : "",

                   $", {AllowedLimitRanges.Count} allowed limit range(s) [{MessageId}]"

               );

        #endregion

    }

}

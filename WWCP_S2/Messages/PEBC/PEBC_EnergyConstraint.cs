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
    /// The Resource Manager specifies the average power levels it needs within a period of
    /// time (Power Envelope Based Control): multiplied with the duration between valid_from
    /// and valid_until they describe the lowest and highest amount of energy the resource
    /// will consume (or produce, when negative) during that period. The CEM shall take these
    /// energy constraints into consideration while setting power limits.
    /// </summary>
    public sealed class PEBC_EnergyConstraint : AS2Message,
                                                IRevokable,
                                                IEquatable<PEBC_EnergyConstraint>
    {

        #region Data

        /// <summary>
        /// The message type "PEBC.EnergyConstraint".
        /// </summary>
        public const String MessageTypeName = "PEBC.EnergyConstraint";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "PEBC.EnergyConstraint".
        /// </summary>
        public override String       MessageType
            => MessageTypeName;

        /// <summary>
        /// The identification of this energy constraint message. Must be unique in the scope
        /// of the Resource Manager, for at least the duration of the session between Resource
        /// Manager and CEM.
        /// </summary>
        [Mandatory]
        public EnergyConstraint_Id   Id                    { get; }

        /// <summary>
        /// The moment this energy constraint information starts to be valid.
        /// </summary>
        [Mandatory]
        public DateTimeOffset        ValidFrom             { get; }

        /// <summary>
        /// The moment until this energy constraint information is valid.
        /// </summary>
        [Mandatory]
        public DateTimeOffset        ValidUntil            { get; }

        /// <summary>
        /// The upper average power within the time period given by valid_from and valid_until.
        /// Multiplied with the duration it yields the associated upper energy content: the highest
        /// amount of energy the resource will consume during that period of time. The power envelope
        /// created by the CEM must allow at least this much energy consumption (when positive).
        /// Must be greater than or equal to the lower average power; can be negative for energy production.
        /// </summary>
        [Mandatory]
        public Double                UpperAveragePower     { get; }

        /// <summary>
        /// The lower average power within the time period given by valid_from and valid_until.
        /// Multiplied with the duration it yields the associated lower energy content: the lowest
        /// amount of energy the resource will consume during that period of time. The power envelope
        /// created by the CEM must allow at least this much energy production (when negative).
        /// Must be smaller than or equal to the upper average power; can be negative for energy production.
        /// </summary>
        [Mandatory]
        public Double                LowerAveragePower     { get; }

        /// <summary>
        /// The type of power quantity which applies to the upper and lower average power.
        /// </summary>
        [Mandatory]
        public CommodityQuantity     CommodityQuantity     { get; }

        /// <summary>
        /// The revokable object type of this message: PEBC.EnergyConstraint.
        /// </summary>
        public RevokableObject       RevokableObjectType
            => RevokableObject.PEBC_EnergyConstraint;

        /// <summary>
        /// The identification a RevokeObject uses to refer to this message: its "id".
        /// </summary>
        public S2Object_Id           RevokableObjectId
            => S2Object_Id.From(Id);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy constraint message.
        /// </summary>
        /// <param name="Id">The identification of this energy constraint message.</param>
        /// <param name="ValidFrom">The moment this energy constraint information starts to be valid.</param>
        /// <param name="ValidUntil">The moment until this energy constraint information is valid (not before ValidFrom).</param>
        /// <param name="UpperAveragePower">The upper average power within the period of validity.</param>
        /// <param name="LowerAveragePower">The lower average power within the period of validity (not greater than UpperAveragePower).</param>
        /// <param name="CommodityQuantity">The type of power quantity which applies to the upper and lower average power.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public PEBC_EnergyConstraint(EnergyConstraint_Id  Id,
                                     DateTimeOffset       ValidFrom,
                                     DateTimeOffset       ValidUntil,
                                     Double               UpperAveragePower,
                                     Double               LowerAveragePower,
                                     CommodityQuantity    CommodityQuantity,
                                     Message_Id?          MessageId   = null)

            : base(MessageId)

        {

            if (ValidUntil < ValidFrom)
                throw new ArgumentException($"The 'valid_until' timestamp ({ValidUntil.ToS2Timestamp()}) must not be before the 'valid_from' timestamp ({ValidFrom.ToS2Timestamp()})!",
                                            nameof(ValidUntil));

            if (LowerAveragePower > UpperAveragePower)
                throw new ArgumentException($"The 'lower_average_power' ({LowerAveragePower}) must be smaller than or equal to the 'upper_average_power' ({UpperAveragePower})!",
                                            nameof(LowerAveragePower));

            this.Id                 = Id;
            this.ValidFrom          = ValidFrom;
            this.ValidUntil         = ValidUntil;
            this.UpperAveragePower  = UpperAveragePower;
            this.LowerAveragePower  = LowerAveragePower;
            this.CommodityQuantity  = CommodityQuantity;

            unchecked
            {
                hashCode = this.MessageId.        GetHashCode() * 17 ^
                           this.Id.               GetHashCode() * 13 ^
                           this.ValidFrom.        GetHashCode() * 11 ^
                           this.ValidUntil.       GetHashCode() *  7 ^
                           this.UpperAveragePower.GetHashCode() *  5 ^
                           this.LowerAveragePower.GetHashCode() *  3 ^
                           this.CommodityQuantity.GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // PEBC.EnergyConstraint.schema.json
        //   "title": "PEBC_EnergyConstraint",
        //   "properties": {
        //     "message_type":        { "type": "string", "const": "PEBC.EnergyConstraint" },
        //     "message_id":          { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "id":                  { "$ref": "../schemas/ID.schema.json",
        //                              "description": "Identifier of this PEBC.EnergyConstraints. Must be unique in the scope of the
        //                                              Resource Manager, for at least the duration of the session between Resource
        //                                              Manager and CEM." },
        //     "valid_from":          { "type": "string", "format": "date-time",
        //                              "description": "Moment this PEBC.EnergyConstraints information starts to be valid" },
        //     "valid_until":         { "type": "string", "format": "date-time",
        //                              "description": "Moment until this PEBC.EnergyConstraints information is valid." },
        //     "upper_average_power": { "type": "number",
        //                              "description": "Upper average power within the time period given by valid_from and valid_until.
        //                                              If the duration is multiplied with this power value, then the associated upper
        //                                              energy content can be derived. This is the highest amount of energy the resource
        //                                              will consume during that period of time. The Power Envelope created by the CEM
        //                                              must allow at least this much energy consumption (in case the number is positive).
        //                                              Must be greater than or equal to lower_average_power, and can be negative in case
        //                                              of energy production." },
        //     "lower_average_power": { "type": "number",
        //                              "description": "Lower average power within the time period given by valid_from and valid_until.
        //                                              If the duration is multiplied with this power value, then the associated lower
        //                                              energy content can be derived. This is the lowest amount of energy the resource
        //                                              will consume during that period of time. The Power Envelope created by the CEM
        //                                              must allow at least this much energy production (in case the number is negative).
        //                                              Must be greater than or equal to lower_average_power, and can be negative in case
        //                                              of energy production." },
        //     "commodity_quantity":  { "$ref": "../schemas/CommodityQuantity.schema.json",
        //                              "description": "Type of power quantity which applies to upper_average_power and lower_average_power" }
        //   },
        //   "required": ["message_type", "message_id", "id", "valid_from", "valid_until", "upper_average_power",
        //                "lower_average_power", "commodity_quantity"],
        //   "additionalProperties": false
        //
        // Semantic rules (CONVENTIONS.md §8):
        //   - lower_average_power <= upper_average_power.
        //   - valid_until >= valid_from.

        #endregion

        #region (static) TryParse(JSON, out EnergyConstraint, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an energy constraint message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EnergyConstraint">The parsed energy constraint message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out PEBC_EnergyConstraint?  EnergyConstraint,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)

            => TryParse(JSON,
                        out EnergyConstraint,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an energy constraint message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EnergyConstraint">The parsed energy constraint message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out PEBC_EnergyConstraint?  EnergyConstraint,
                                       [NotNullWhen(false)] out String?                 ErrorResponse,
                                       S2ParserOptions?                                 Options)

            => TryParse(JSON,
                        out EnergyConstraint,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an energy constraint message.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EnergyConstraint">The parsed energy constraint message.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomEnergyConstraintParser">A delegate to parse custom energy constraint messages.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out PEBC_EnergyConstraint?      EnergyConstraint,
                                       [NotNullWhen(false)] out String?                     ErrorResponse,
                                       S2ParserOptions?                                     Options,
                                       CustomJObjectParserDelegate<PEBC_EnergyConstraint>?  CustomEnergyConstraintParser)
        {

            try
            {

                EnergyConstraint = null;

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
                                             "energy constraint identification",
                                             EnergyConstraint_Id.TryParse,
                                             Options,
                                             out EnergyConstraint_Id id,
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

                #region valid_until                [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("valid_until",
                                                    "valid until timestamp",
                                                    Options,
                                                    out DateTimeOffset validUntil,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region upper_average_power        [mandatory]

                if (!JSON.ParseMandatoryS2Number("upper_average_power",
                                                 "upper average power",
                                                 out Double upperAveragePower,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region lower_average_power        [mandatory]

                if (!JSON.ParseMandatoryS2Number("lower_average_power",
                                                 "lower average power",
                                                 out Double lowerAveragePower,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region commodity_quantity         [mandatory]

                if (!JSON.ParseMandatoryS2Enum("commodity_quantity",
                                               "commodity quantity",
                                               CommodityQuantity.TryParse,
                                               Options,
                                               out CommodityQuantity commodityQuantity,
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
                                                    "upper_average_power",
                                                    "lower_average_power",
                                                    "commodity_quantity"))
                {
                    return false;
                }

                #endregion


                EnergyConstraint = new PEBC_EnergyConstraint(
                                       id,
                                       validFrom,
                                       validUntil,
                                       upperAveragePower,
                                       lowerAveragePower,
                                       commodityQuantity,
                                       messageId
                                   );

                if (CustomEnergyConstraintParser is not null)
                    EnergyConstraint = CustomEnergyConstraintParser(JSON,
                                                                    EnergyConstraint);

                return true;

            }
            catch (Exception e)
            {
                EnergyConstraint  = null;
                ErrorResponse     = "The given JSON representation of a PEBC energy constraint message is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEnergyConstraintSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomEnergyConstraintSerializer">A delegate to serialize custom energy constraint messages.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PEBC_EnergyConstraint>? CustomEnergyConstraintSerializer)
        {

            var json = CreateJSON(
                           new JProperty("id",                   Id.               ToString()),
                           new JProperty("valid_from",           ValidFrom.        ToS2Timestamp()),
                           new JProperty("valid_until",          ValidUntil.       ToS2Timestamp()),
                           new JProperty("upper_average_power",  UpperAveragePower),
                           new JProperty("lower_average_power",  LowerAveragePower),
                           new JProperty("commodity_quantity",   CommodityQuantity.ToString())
                       );

            return CustomEnergyConstraintSerializer is not null
                       ? CustomEnergyConstraintSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy constraint message.
        /// </summary>
        public PEBC_EnergyConstraint Clone()

            => new (
                   Id.               Clone(),
                   ValidFrom,
                   ValidUntil,
                   UpperAveragePower,
                   LowerAveragePower,
                   CommodityQuantity.Clone(),
                   MessageId.        Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two energy constraint messages for equality.
        /// </summary>
        public static Boolean operator == (PEBC_EnergyConstraint? EnergyConstraint1, PEBC_EnergyConstraint? EnergyConstraint2)
        {

            if (ReferenceEquals(EnergyConstraint1, EnergyConstraint2))
                return true;

            if (EnergyConstraint1 is null || EnergyConstraint2 is null)
                return false;

            return EnergyConstraint1.Equals(EnergyConstraint2);

        }

        /// <summary>
        /// Compares two energy constraint messages for inequality.
        /// </summary>
        public static Boolean operator != (PEBC_EnergyConstraint? EnergyConstraint1, PEBC_EnergyConstraint? EnergyConstraint2)
            => !(EnergyConstraint1 == EnergyConstraint2);

        #endregion

        #region IEquatable<PEBC_EnergyConstraint> Members

        /// <summary>
        /// Compares two energy constraint messages for equality.
        /// </summary>
        /// <param name="Object">An energy constraint message to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is PEBC_EnergyConstraint energyConstraint && Equals(energyConstraint);

        /// <summary>
        /// Compares two energy constraint messages for equality.
        /// </summary>
        /// <param name="EnergyConstraint">An energy constraint message to compare with.</param>
        public Boolean Equals(PEBC_EnergyConstraint? EnergyConstraint)

            => EnergyConstraint is not null &&

               MessageId.        Equals(EnergyConstraint.MessageId)         &&
               Id.               Equals(EnergyConstraint.Id)                &&
               ValidFrom.        Equals(EnergyConstraint.ValidFrom)         &&
               ValidUntil.       Equals(EnergyConstraint.ValidUntil)        &&
               UpperAveragePower.Equals(EnergyConstraint.UpperAveragePower) &&
               LowerAveragePower.Equals(EnergyConstraint.LowerAveragePower) &&
               CommodityQuantity.Equals(EnergyConstraint.CommodityQuantity);

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

            => $"PEBC.EnergyConstraint '{Id}' {LowerAveragePower}..{UpperAveragePower} {CommodityQuantity} " +
               $"from {ValidFrom.ToS2Timestamp()} until {ValidUntil.ToS2Timestamp()} [{MessageId}]";

        #endregion

    }

}

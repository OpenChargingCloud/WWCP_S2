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
    /// The static properties of a Resource Manager: its identification, energy roles,
    /// device information, the control types it offers, whether it provides power
    /// forecasts and which power measurement types it can report.
    /// </summary>
    public sealed class ResourceManagerDetails : AS2Message,
                                                 IEquatable<ResourceManagerDetails>
    {

        #region Data

        /// <summary>
        /// The message type "ResourceManagerDetails".
        /// </summary>
        public const String MessageTypeName = "ResourceManagerDetails";

        #endregion

        #region Properties

        /// <summary>
        /// The message type "ResourceManagerDetails".
        /// </summary>
        public override String                      MessageType
            => MessageTypeName;

        /// <summary>
        /// The identifier of the Resource Manager. Must be unique within the scope of the CEM.
        /// </summary>
        [Mandatory]
        public Resource_Id                          ResourceId                       { get; }

        /// <summary>
        /// The optional human readable name given by the user.
        /// </summary>
        [Optional]
        public String?                              Name                             { get; }

        /// <summary>
        /// The energy roles of this Resource Manager (one to three).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<Role>                  Roles                            { get; }

        /// <summary>
        /// The optional name of the manufacturer.
        /// </summary>
        [Optional]
        public String?                              Manufacturer                     { get; }

        /// <summary>
        /// The optional name of the model of the device (provided by the manufacturer).
        /// </summary>
        [Optional]
        public String?                              Model                            { get; }

        /// <summary>
        /// The optional serial number of the device (provided by the manufacturer).
        /// </summary>
        [Optional]
        public String?                              SerialNumber                     { get; }

        /// <summary>
        /// The optional version identifier of the firmware used in the device (provided by the manufacturer).
        /// </summary>
        [Optional]
        public String?                              FirmwareVersion                  { get; }

        /// <summary>
        /// The average time the combination of Resource Manager and HBES/BACS/SASS or
        /// (smart) device needs to process and execute an instruction.
        /// </summary>
        [Mandatory]
        public Duration                             InstructionProcessingDelay       { get; }

        /// <summary>
        /// The control types supported by this Resource Manager (one to five, never NO_SELECTION).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<ControlType>           AvailableControlTypes            { get; }

        /// <summary>
        /// The optional currency to be used for all information regarding costs.
        /// Mandatory if cost information is published.
        /// </summary>
        [Optional]
        public Currency?                            Currency                         { get; }

        /// <summary>
        /// Whether the Resource Manager is able to provide power forecasts.
        /// </summary>
        [Mandatory]
        public Boolean                              ProvidesForecast                 { get; }

        /// <summary>
        /// All commodity quantities this Resource Manager can provide measurements for (one to ten).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<CommodityQuantity>     ProvidesPowerMeasurementTypes    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create new resource manager details.
        /// </summary>
        /// <param name="ResourceId">The identifier of the Resource Manager, unique within the scope of the CEM.</param>
        /// <param name="Roles">The energy roles of this Resource Manager (1..3).</param>
        /// <param name="InstructionProcessingDelay">The average time needed to process and execute an instruction.</param>
        /// <param name="AvailableControlTypes">The control types supported by this Resource Manager (1..5, never NO_SELECTION).</param>
        /// <param name="ProvidesForecast">Whether the Resource Manager is able to provide power forecasts.</param>
        /// <param name="ProvidesPowerMeasurementTypes">All commodity quantities this Resource Manager can provide measurements for (1..10).</param>
        /// <param name="Name">An optional human readable name given by the user.</param>
        /// <param name="Manufacturer">An optional name of the manufacturer.</param>
        /// <param name="Model">An optional name of the model of the device.</param>
        /// <param name="SerialNumber">An optional serial number of the device.</param>
        /// <param name="FirmwareVersion">An optional version identifier of the firmware used in the device.</param>
        /// <param name="Currency">An optional currency to be used for all information regarding costs.</param>
        /// <param name="MessageId">An optional message identification.</param>
        public ResourceManagerDetails(Resource_Id                       ResourceId,
                                      IReadOnlyList<Role>               Roles,
                                      Duration                          InstructionProcessingDelay,
                                      IReadOnlyList<ControlType>        AvailableControlTypes,
                                      Boolean                           ProvidesForecast,
                                      IReadOnlyList<CommodityQuantity>  ProvidesPowerMeasurementTypes,
                                      String?                           Name              = null,
                                      String?                           Manufacturer      = null,
                                      String?                           Model             = null,
                                      String?                           SerialNumber      = null,
                                      String?                           FirmwareVersion   = null,
                                      Currency?                         Currency          = null,
                                      Message_Id?                       MessageId         = null)

            : base(MessageId)

        {

            ArgumentNullException.ThrowIfNull(Roles);
            ArgumentNullException.ThrowIfNull(AvailableControlTypes);
            ArgumentNullException.ThrowIfNull(ProvidesPowerMeasurementTypes);

            if (ResourceId.IsNullOrEmpty)
                throw new ArgumentException("The resource identification must not be null or empty!",
                                            nameof(ResourceId));

            if (Roles.Count < 1)
                throw new ArgumentException("The resource manager details must contain at least one role!",
                                            nameof(Roles));

            if (Roles.Count > 3)
                throw new ArgumentException($"The resource manager details must not contain more than 3 roles, but {Roles.Count} were given!",
                                            nameof(Roles));

            if (AvailableControlTypes.Count < 1)
                throw new ArgumentException("The resource manager details must contain at least one available control type!",
                                            nameof(AvailableControlTypes));

            if (AvailableControlTypes.Count > 5)
                throw new ArgumentException($"The resource manager details must not contain more than 5 available control types, but {AvailableControlTypes.Count} were given!",
                                            nameof(AvailableControlTypes));

            if (AvailableControlTypes.Any(controlType => controlType == ControlType.NoSelection))
                throw new ArgumentException("The available control types must not contain NO_SELECTION!",
                                            nameof(AvailableControlTypes));

            if (ProvidesPowerMeasurementTypes.Count < 1)
                throw new ArgumentException("The resource manager details must contain at least one provided power measurement type!",
                                            nameof(ProvidesPowerMeasurementTypes));

            if (ProvidesPowerMeasurementTypes.Count > 10)
                throw new ArgumentException($"The resource manager details must not contain more than 10 provided power measurement types, but {ProvidesPowerMeasurementTypes.Count} were given!",
                                            nameof(ProvidesPowerMeasurementTypes));

            this.ResourceId                     = ResourceId;
            this.Name                           = Name;
            this.Roles                          = [.. Roles];
            this.Manufacturer                   = Manufacturer;
            this.Model                          = Model;
            this.SerialNumber                   = SerialNumber;
            this.FirmwareVersion                = FirmwareVersion;
            this.InstructionProcessingDelay     = InstructionProcessingDelay;
            this.AvailableControlTypes          = [.. AvailableControlTypes];
            this.Currency                       = Currency;
            this.ProvidesForecast               = ProvidesForecast;
            this.ProvidesPowerMeasurementTypes  = [.. ProvidesPowerMeasurementTypes];

            unchecked
            {
                hashCode = this.MessageId.                    GetHashCode()                             * 41 ^
                           this.ResourceId.                   GetHashCode()                             * 37 ^
                          (this.Name?.                        GetHashCode(StringComparison.Ordinal) ?? 0) * 31 ^
                           this.Roles.                        CalcHashCode()                            * 29 ^
                          (this.Manufacturer?.                GetHashCode(StringComparison.Ordinal) ?? 0) * 23 ^
                          (this.Model?.                       GetHashCode(StringComparison.Ordinal) ?? 0) * 19 ^
                          (this.SerialNumber?.                GetHashCode(StringComparison.Ordinal) ?? 0) * 17 ^
                          (this.FirmwareVersion?.             GetHashCode(StringComparison.Ordinal) ?? 0) * 13 ^
                           this.InstructionProcessingDelay.   GetHashCode()                             * 11 ^
                           this.AvailableControlTypes.        CalcHashCode()                            *  7 ^
                          (this.Currency?.                    GetHashCode()                         ?? 0) *  5 ^
                           this.ProvidesForecast.             GetHashCode()                             *  3 ^
                           this.ProvidesPowerMeasurementTypes.CalcHashCode();
            }

        }

        #endregion


        #region Documentation

        // ResourceManagerDetails.schema.json
        //   "title": "ResourceManagerDetails",
        //   "properties": {
        //     "message_type":     { "type": "string", "const": "ResourceManagerDetails" },
        //     "message_id":       { "$ref": "../schemas/ID.schema.json", "description": "ID of this message" },
        //     "resource_id":      { "$ref": "../schemas/ID.schema.json",
        //                           "description": "Identifier of the Resource Manager. Must be unique within the scope of the CEM." },
        //     "name":             { "type": "string", "description": "Human readable name given by user" },
        //     "roles":            { "type": "array", "minItems": 1, "maxItems": 3,
        //                           "items": { "$ref": "../schemas/Role.schema.json" },
        //                           "description": "Each Resource Manager provides one or more energy Roles" },
        //     "manufacturer":     { "type": "string", "description": "Name of Manufacturer" },
        //     "model":            { "type": "string", "description": "Name of the model of the device (provided by the manufacturer)" },
        //     "serial_number":    { "type": "string", "description": "Serial number of the device (provided by the manufacturer)" },
        //     "firmware_version": { "type": "string",
        //                           "description": "Version identifier of the firmware used in the device (provided by the manufacturer)" },
        //     "instruction_processing_delay": {
        //                           "$ref": "../schemas/Duration.schema.json",
        //                           "description": "The average time the combination of Resource Manager and HBES/BACS/SASS or (Smart) device
        //                                           needs to process and execute an instruction" },
        //     "available_control_types": {
        //                           "type": "array", "minItems": 1, "maxItems": 5,
        //                           "items": { "$ref": "../schemas/ControlType.schema.json" },
        //                           "description": "The control types supported by this Resource Manager." },
        //     "currency":         { "$ref": "../schemas/Currency.schema.json",
        //                           "description": "Currency to be used for all information regarding costs.
        //                                           Mandatory if cost information is published." },
        //     "provides_forecast": {
        //                           "type": "boolean",
        //                           "description": "Indicates whether the ResourceManager is able to provide PowerForecasts" },
        //     "provides_power_measurement_types": {
        //                           "type": "array", "minItems": 1, "maxItems": 10,
        //                           "items": { "$ref": "../schemas/CommodityQuantity.schema.json" },
        //                           "description": "Array of all CommodityQuantities that this Resource Manager can provide measurements for. " }
        //   },
        //   "required": ["message_type", "message_id", "resource_id", "roles", "instruction_processing_delay",
        //                "available_control_types", "provides_forecast", "provides_power_measurement_types"],
        //   "additionalProperties": false
        //
        // Semantic rule (CONVENTIONS.md §8): available_control_types must not contain NO_SELECTION.

        #endregion

        #region (static) TryParse(JSON, out ResourceManagerDetails, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of resource manager details.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ResourceManagerDetails">The parsed resource manager details.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out ResourceManagerDetails?  ResourceManagerDetails,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)

            => TryParse(JSON,
                        out ResourceManagerDetails,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of resource manager details.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ResourceManagerDetails">The parsed resource manager details.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out ResourceManagerDetails?  ResourceManagerDetails,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       S2ParserOptions?                                  Options)

            => TryParse(JSON,
                        out ResourceManagerDetails,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of resource manager details.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ResourceManagerDetails">The parsed resource manager details.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomResourceManagerDetailsParser">A delegate to parse custom resource manager details.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out ResourceManagerDetails?      ResourceManagerDetails,
                                       [NotNullWhen(false)] out String?                      ErrorResponse,
                                       S2ParserOptions?                                      Options,
                                       CustomJObjectParserDelegate<ResourceManagerDetails>?  CustomResourceManagerDetailsParser)
        {

            try
            {

                ResourceManagerDetails = null;

                #region message_type, message_id            [mandatory]

                if (!TryParseHeader(JSON,
                                    MessageTypeName,
                                    Options,
                                    out var messageId,
                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region resource_id                         [mandatory]

                if (!JSON.ParseMandatoryS2Id("resource_id",
                                             "resource identification",
                                             Resource_Id.TryParse,
                                             Options,
                                             out Resource_Id resourceId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region name                                [optional]

                if (!JSON.ParseOptionalS2String("name",
                                                "name",
                                                out String? name,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region roles                               [mandatory]

                if (!JSON.ParseMandatoryS2List("roles",
                                               "roles",
                                               Role.TryParse,
                                               Options,
                                               1,
                                               3,
                                               out IReadOnlyList<Role>? roles,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region manufacturer                        [optional]

                if (!JSON.ParseOptionalS2String("manufacturer",
                                                "manufacturer",
                                                out String? manufacturer,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region model                               [optional]

                if (!JSON.ParseOptionalS2String("model",
                                                "model",
                                                out String? model,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region serial_number                       [optional]

                if (!JSON.ParseOptionalS2String("serial_number",
                                                "serial number",
                                                out String? serialNumber,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region firmware_version                    [optional]

                if (!JSON.ParseOptionalS2String("firmware_version",
                                                "firmware version",
                                                out String? firmwareVersion,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region instruction_processing_delay        [mandatory]

                if (!JSON.ParseMandatoryS2Duration("instruction_processing_delay",
                                                   "instruction processing delay",
                                                   out Duration instructionProcessingDelay,
                                                   out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region available_control_types             [mandatory]

                if (!JSON.ParseMandatoryS2Enums("available_control_types",
                                                "available control types",
                                                ControlType.TryParse,
                                                Options,
                                                1,
                                                5,
                                                out IReadOnlyList<ControlType>? availableControlTypes,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region currency                            [optional]

                if (!JSON.ParseOptionalS2Enum("currency",
                                              "currency",
                                              S2.Currency.TryParse,
                                              Options,
                                              out Currency? currency,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region provides_forecast                   [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("provides_forecast",
                                                  "provides forecast",
                                                  out Boolean providesForecast,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region provides_power_measurement_types    [mandatory]

                if (!JSON.ParseMandatoryS2Enums("provides_power_measurement_types",
                                                "provided power measurement types",
                                                CommodityQuantity.TryParse,
                                                Options,
                                                1,
                                                10,
                                                out IReadOnlyList<CommodityQuantity>? providesPowerMeasurementTypes,
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
                                                    "resource_id",
                                                    "name",
                                                    "roles",
                                                    "manufacturer",
                                                    "model",
                                                    "serial_number",
                                                    "firmware_version",
                                                    "instruction_processing_delay",
                                                    "available_control_types",
                                                    "currency",
                                                    "provides_forecast",
                                                    "provides_power_measurement_types"))
                {
                    return false;
                }

                #endregion


                ResourceManagerDetails = new ResourceManagerDetails(
                                             resourceId,
                                             roles,
                                             instructionProcessingDelay,
                                             availableControlTypes,
                                             providesForecast,
                                             providesPowerMeasurementTypes,
                                             name,
                                             manufacturer,
                                             model,
                                             serialNumber,
                                             firmwareVersion,
                                             currency,
                                             messageId
                                         );

                if (CustomResourceManagerDetailsParser is not null)
                    ResourceManagerDetails = CustomResourceManagerDetailsParser(JSON,
                                                                                ResourceManagerDetails);

                return true;

            }
            catch (Exception e)
            {
                ResourceManagerDetails  = null;
                ErrorResponse           = "The given JSON representation of resource manager details is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomResourceManagerDetailsSerializer = null)

        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        public override JObject ToJSON()
            => ToJSON(null);


        /// <summary>
        /// Return the complete JSON representation of this message.
        /// </summary>
        /// <param name="CustomResourceManagerDetailsSerializer">A delegate to serialize custom resource manager details.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ResourceManagerDetails>? CustomResourceManagerDetailsSerializer)
        {

            var json = CreateJSON(

                                 new JProperty("resource_id",                       ResourceId.ToString()),

                           Name is not null
                               ? new JProperty("name",                              Name)
                               : null,

                                 new JProperty("roles",                             new JArray(Roles.Select(role => role.ToJSON()))),

                           Manufacturer is not null
                               ? new JProperty("manufacturer",                      Manufacturer)
                               : null,

                           Model is not null
                               ? new JProperty("model",                             Model)
                               : null,

                           SerialNumber is not null
                               ? new JProperty("serial_number",                     SerialNumber)
                               : null,

                           FirmwareVersion is not null
                               ? new JProperty("firmware_version",                  FirmwareVersion)
                               : null,

                                 new JProperty("instruction_processing_delay",      InstructionProcessingDelay.ToJSON()),
                                 new JProperty("available_control_types",           new JArray(AvailableControlTypes.Select(controlType => controlType.ToString()))),

                           Currency is not null
                               ? new JProperty("currency",                          Currency.Value.ToString())
                               : null,

                                 new JProperty("provides_forecast",                 ProvidesForecast),
                                 new JProperty("provides_power_measurement_types",  new JArray(ProvidesPowerMeasurementTypes.Select(commodityQuantity => commodityQuantity.ToString())))

                       );

            return CustomResourceManagerDetailsSerializer is not null
                       ? CustomResourceManagerDetailsSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone these resource manager details.
        /// </summary>
        public ResourceManagerDetails Clone()

            => new (
                   ResourceId.Clone(),
                   Roles.                        Select(role              => role.             Clone()).ToList(),
                   InstructionProcessingDelay,
                   AvailableControlTypes.        Select(controlType       => controlType.      Clone()).ToList(),
                   ProvidesForecast,
                   ProvidesPowerMeasurementTypes.Select(commodityQuantity => commodityQuantity.Clone()).ToList(),
                   Name?.           CloneString(),
                   Manufacturer?.   CloneString(),
                   Model?.          CloneString(),
                   SerialNumber?.   CloneString(),
                   FirmwareVersion?.CloneString(),
                   Currency?.       Clone(),
                   MessageId.       Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two resource manager details for equality.
        /// </summary>
        public static Boolean operator == (ResourceManagerDetails? ResourceManagerDetails1, ResourceManagerDetails? ResourceManagerDetails2)
        {

            if (ReferenceEquals(ResourceManagerDetails1, ResourceManagerDetails2))
                return true;

            if (ResourceManagerDetails1 is null || ResourceManagerDetails2 is null)
                return false;

            return ResourceManagerDetails1.Equals(ResourceManagerDetails2);

        }

        /// <summary>
        /// Compares two resource manager details for inequality.
        /// </summary>
        public static Boolean operator != (ResourceManagerDetails? ResourceManagerDetails1, ResourceManagerDetails? ResourceManagerDetails2)
            => !(ResourceManagerDetails1 == ResourceManagerDetails2);

        #endregion

        #region IEquatable<ResourceManagerDetails> Members

        /// <summary>
        /// Compares two resource manager details for equality.
        /// </summary>
        /// <param name="Object">Resource manager details to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is ResourceManagerDetails resourceManagerDetails && Equals(resourceManagerDetails);

        /// <summary>
        /// Compares two resource manager details for equality.
        /// </summary>
        /// <param name="ResourceManagerDetails">Resource manager details to compare with.</param>
        public Boolean Equals(ResourceManagerDetails? ResourceManagerDetails)

            => ResourceManagerDetails is not null &&

               MessageId.                 Equals(ResourceManagerDetails.MessageId)                  &&
               ResourceId.                Equals(ResourceManagerDetails.ResourceId)                 &&
               Roles.                     SequenceEqual(ResourceManagerDetails.Roles)               &&
               InstructionProcessingDelay.Equals(ResourceManagerDetails.InstructionProcessingDelay) &&
               AvailableControlTypes.     SequenceEqual(ResourceManagerDetails.AvailableControlTypes) &&
               ProvidesForecast.          Equals(ResourceManagerDetails.ProvidesForecast)           &&
               ProvidesPowerMeasurementTypes.SequenceEqual(ResourceManagerDetails.ProvidesPowerMeasurementTypes) &&

               String.Equals(Name,            ResourceManagerDetails.Name,            StringComparison.Ordinal) &&
               String.Equals(Manufacturer,    ResourceManagerDetails.Manufacturer,    StringComparison.Ordinal) &&
               String.Equals(Model,           ResourceManagerDetails.Model,           StringComparison.Ordinal) &&
               String.Equals(SerialNumber,    ResourceManagerDetails.SerialNumber,    StringComparison.Ordinal) &&
               String.Equals(FirmwareVersion, ResourceManagerDetails.FirmwareVersion, StringComparison.Ordinal) &&

               Nullable.Equals(Currency, ResourceManagerDetails.Currency);

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

                   $"ResourceManagerDetails of '{ResourceId}'",

                   Name is not null
                       ? $" ({Name})"
                       : "",

                   $" offering {String.Join(", ", AvailableControlTypes)}",

                   $" [{MessageId}]"

               );

        #endregion

    }

}

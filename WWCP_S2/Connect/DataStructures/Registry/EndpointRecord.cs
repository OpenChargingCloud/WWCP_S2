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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// A record of the S2 Connect WAN endpoint registry: a WAN endpoint (a cloud CEM or RM
    /// service) with its user facing name, description and icons, its pairing URL, the
    /// countries in which it operates, its status and the roles of the nodes it hosts
    /// (s2-connect-wan-endpoint-registry.yml, EndpointRecord).
    /// </summary>
    public sealed class EndpointRecord : IEquatable<EndpointRecord>
    {

        #region Data

        /// <summary>
        /// The maximal length of the user facing name in Unicode code points (schema: maxLength 50).
        /// </summary>
        public const Int32  MaxNameLength         = 50;

        /// <summary>
        /// The maximal length of the user facing description in Unicode code points (schema: maxLength 500).
        /// </summary>
        public const Int32  MaxDescriptionLength  = 500;

        #endregion

        #region Properties

        /// <summary>
        /// The unique identification of the WAN endpoint registration.
        /// </summary>
        [Mandatory]
        public EndpointRecord_Id           Id             { get; }

        /// <summary>
        /// The user facing name of the endpoint (at most 50 characters).
        /// </summary>
        [Mandatory]
        public String                      Name           { get; }

        /// <summary>
        /// The user facing description of the endpoint (at most 500 characters).
        /// </summary>
        [Mandatory]
        public String                      Description    { get; }

        /// <summary>
        /// The URL of the icon of the endpoint in the size of 32 by 32 pixels and in PNG format.
        /// </summary>
        [Mandatory]
        public URL                         Icon32         { get; }

        /// <summary>
        /// The URL of the icon of the endpoint in the size of 128 by 128 pixels and in PNG format.
        /// </summary>
        [Mandatory]
        public URL                         Icon128        { get; }

        /// <summary>
        /// The URL of the icon of the endpoint in the size of 512 by 512 pixels and in PNG format.
        /// </summary>
        [Mandatory]
        public URL                         Icon512        { get; }

        /// <summary>
        /// The base URL of the pairing API: includes the protocol "https://", ends with a
        /// slash and does not include the API version, e.g. "https://hostname.tld/pairing/".
        /// </summary>
        [Mandatory]
        public S2BaseURL                   PairingUrl     { get; }

        /// <summary>
        /// The ISO 3166-1 alpha-2 country codes in which this S2 WAN endpoint operates (at least one, unique).
        /// </summary>
        [Mandatory]
        public IReadOnlyList<CountryCode>  Regions        { get; }

        /// <summary>
        /// The status of the endpoint: public or testing.
        /// </summary>
        [Mandatory]
        public EndpointStatus              Status         { get; }

        /// <summary>
        /// Whether this endpoint represents CEMs.
        /// </summary>
        [Mandatory]
        public Boolean                     CEM            { get; }

        /// <summary>
        /// Whether this endpoint represents RMs.
        /// </summary>
        [Mandatory]
        public Boolean                     RM             { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new endpoint record.
        /// </summary>
        /// <param name="Id">The unique identification of the WAN endpoint registration.</param>
        /// <param name="Name">The user facing name of the endpoint (at most 50 characters).</param>
        /// <param name="Description">The user facing description of the endpoint (at most 500 characters).</param>
        /// <param name="Icon32">The URL of the 32 by 32 pixel PNG icon of the endpoint.</param>
        /// <param name="Icon128">The URL of the 128 by 128 pixel PNG icon of the endpoint.</param>
        /// <param name="Icon512">The URL of the 512 by 512 pixel PNG icon of the endpoint.</param>
        /// <param name="PairingUrl">The base URL of the pairing API.</param>
        /// <param name="Regions">The ISO 3166-1 alpha-2 country codes in which the endpoint operates (at least one, unique).</param>
        /// <param name="Status">The status of the endpoint.</param>
        /// <param name="CEM">Whether this endpoint represents CEMs.</param>
        /// <param name="RM">Whether this endpoint represents RMs.</param>
        public EndpointRecord(EndpointRecord_Id           Id,
                              String                      Name,
                              String                      Description,
                              URL                         Icon32,
                              URL                         Icon128,
                              URL                         Icon512,
                              S2BaseURL                   PairingUrl,
                              IReadOnlyList<CountryCode>  Regions,
                              EndpointStatus              Status,
                              Boolean                     CEM,
                              Boolean                     RM)
        {

            ArgumentNullException.ThrowIfNull(Name);
            ArgumentNullException.ThrowIfNull(Description);
            ArgumentNullException.ThrowIfNull(Regions);

            #region Name and description: maxLength 50 / 500

            // JSON Schema counts the length of a string in Unicode code points, not in UTF-16 code units.
            var nameLength = Name.EnumerateRunes().Count();

            if (nameLength > MaxNameLength)
                throw new ArgumentException($"The name of an endpoint record must not be longer than {MaxNameLength} characters, but {nameLength} were given!",
                                            nameof(Name));

            var descriptionLength = Description.EnumerateRunes().Count();

            if (descriptionLength > MaxDescriptionLength)
                throw new ArgumentException($"The description of an endpoint record must not be longer than {MaxDescriptionLength} characters, but {descriptionLength} were given!",
                                            nameof(Description));

            #endregion

            #region Icons and pairing URL: not empty

            if (Icon32.IsNullOrEmpty)
                throw new ArgumentException("The URL of the 32 by 32 pixel icon must not be empty!",
                                            nameof(Icon32));

            if (Icon128.IsNullOrEmpty)
                throw new ArgumentException("The URL of the 128 by 128 pixel icon must not be empty!",
                                            nameof(Icon128));

            if (Icon512.IsNullOrEmpty)
                throw new ArgumentException("The URL of the 512 by 512 pixel icon must not be empty!",
                                            nameof(Icon512));

            if (String.IsNullOrEmpty(PairingUrl.Value))
                throw new ArgumentException("The pairing URL must not be empty!",
                                            nameof(PairingUrl));

            #endregion

            #region Regions: minItems 1, unique

            if (Regions.Count < 1)
                throw new ArgumentException("An endpoint record must contain at least one region!",
                                            nameof(Regions));

            var duplicateRegion = Regions.
                                      GroupBy(region => region).
                                      Select (group  => (group.Key, Count: group.Count())).
                                      FirstOrDefault(group => group.Count > 1);

            if (duplicateRegion.Count > 1)
                throw new ArgumentException($"The regions of an endpoint record must be unique, but '{duplicateRegion.Key}' occurs {duplicateRegion.Count} times!",
                                            nameof(Regions));

            #endregion

            #region Status: not empty

            if (Status.IsNullOrEmpty)
                throw new ArgumentException("The status must not be empty!",
                                            nameof(Status));

            #endregion

            this.Id           = Id;
            this.Name         = Name;
            this.Description  = Description;
            this.Icon32       = Icon32;
            this.Icon128      = Icon128;
            this.Icon512      = Icon512;
            this.PairingUrl   = PairingUrl;
            this.Regions      = [.. Regions];
            this.Status       = Status;
            this.CEM          = CEM;
            this.RM           = RM;

            unchecked
            {
                hashCode = this.Id.         GetHashCode()                          * 31 ^
                           this.Name.       GetHashCode(StringComparison.Ordinal)  * 29 ^
                           this.Description.GetHashCode(StringComparison.Ordinal)  * 23 ^
                           this.Icon32.     GetHashCode()                          * 19 ^
                           this.Icon128.    GetHashCode()                          * 17 ^
                           this.Icon512.    GetHashCode()                          * 13 ^
                           this.PairingUrl. GetHashCode()                          * 11 ^
                           this.Regions.    CalcHashCode()                         *  7 ^
                           this.Status.     GetHashCode()                          *  5 ^
                           this.CEM.        GetHashCode()                          *  3 ^
                           this.RM.         GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // s2-connect-wan-endpoint-registry.yml
        //   EndpointRecord:
        //     type: object
        //     required: ["id", "name", "description", "icon32", "icon128", "icon512", "pairingUrl", "regions", "status", "cem", "rm"]
        //     properties:
        //       id:           { $ref: Id }                              (string, format uuid)
        //       name:         { type: string, maxLength: 50 }           User facing name of the endpoint.
        //       description:  { maxLength: 500 }                        User facing description of the endpoint.
        //       icon32:       { type: string, format: uri }             URL of the icon of the endpoint in the size of 32 by 32 pixels and in PNG format.
        //       icon128:      { type: string, format: uri }             URL of the icon of the endpoint in the size of 128 by 128 pixels and in PNG format.
        //       icon512:      { type: string, format: uri }             URL of the icon of the endpoint in the size of 512 by 512 pixels and in PNG format.
        //       pairingUrl:   { type: string, format: uri }             URL of the pairing API. Must include the protocol ('https://') and end with
        //                                                               a slash ('/'), and may not include the API version (e.g. 'https://hostname.tld/pairing/').
        //       regions:      { type: array, minItems: 1, items: { $ref: CountryCode } }
        //                                                               The ISO 3166-1 alpha-2 country codes in which this S2 WAN Endpoint operates.
        //       status:       { $ref: Status }                          (enum ["public", "testing"], default "public")
        //       cem:          { type: boolean }                         Indicates if this endpoint represents CEMs
        //       rm:           { type: boolean }                         indicates if this endpoint represents RMs
        //
        //   Id:           { type: string, format: uuid }               Unique identifier of the WAN endpoint registration
        //   CountryCode:  { type: string, pattern: '^[A-Z]{2}$' }       An ISO 3166-1 alpha-2 country code
        //   Status:       { type: string, enum: ["public", "testing"], default: "public" }
        //
        // Semantic rules: name at most 50 and description at most 500 characters; at least one region and
        // every region only once; the pairing URL follows the S2 Connect base URL rules (S2BaseURL).

        #endregion

        #region (static) TryParse(JSON, out EndpointRecord, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an endpoint record.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EndpointRecord">The parsed endpoint record.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                   JSON,
                                       [NotNullWhen(true)]  out EndpointRecord?  EndpointRecord,
                                       [NotNullWhen(false)] out String?          ErrorResponse)

            => TryParse(JSON,
                        out EndpointRecord,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an endpoint record.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EndpointRecord">The parsed endpoint record.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                   JSON,
                                       [NotNullWhen(true)]  out EndpointRecord?  EndpointRecord,
                                       [NotNullWhen(false)] out String?          ErrorResponse,
                                       S2ParserOptions?                          Options)

            => TryParse(JSON,
                        out EndpointRecord,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an endpoint record.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EndpointRecord">The parsed endpoint record.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomEndpointRecordParser">A delegate to parse custom endpoint records.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out EndpointRecord?      EndpointRecord,
                                       [NotNullWhen(false)] out String?              ErrorResponse,
                                       S2ParserOptions?                              Options,
                                       CustomJObjectParserDelegate<EndpointRecord>?  CustomEndpointRecordParser)
        {

            try
            {

                EndpointRecord = null;

                #region id             [mandatory]

                if (!JSON.ParseMandatoryS2Id("id",
                                             "endpoint record identification",
                                             EndpointRecord_Id.TryParse,
                                             Options,
                                             out EndpointRecord_Id id,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region name           [mandatory]

                if (!JSON.ParseMandatoryS2String("name",
                                                 "name",
                                                 out String? name,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region description    [mandatory]

                if (!JSON.ParseMandatoryS2String("description",
                                                 "description",
                                                 out String? description,
                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region icon32         [mandatory]

                if (!JSON.ParseMandatoryS2URL("icon32",
                                              "32 by 32 pixel icon URL",
                                              out URL icon32,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region icon128        [mandatory]

                if (!JSON.ParseMandatoryS2URL("icon128",
                                              "128 by 128 pixel icon URL",
                                              out URL icon128,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region icon512        [mandatory]

                if (!JSON.ParseMandatoryS2URL("icon512",
                                              "512 by 512 pixel icon URL",
                                              out URL icon512,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region pairingUrl     [mandatory]

                if (!JSON.ParseMandatoryS2String("pairingUrl",
                                                 "pairing URL",
                                                 out String? pairingUrlText,
                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!S2BaseURL.TryParse(pairingUrlText,
                                        out var pairingUrl,
                                        out var pairingUrlError,
                                        (Options ?? S2ParserOptions.Default).AllowInsecureURLs))
                {
                    ErrorResponse = $"Invalid pairing URL 'pairingUrl': {pairingUrlError}";
                    return false;
                }

                #endregion

                #region regions        [mandatory]

                // Country codes are not S2 identifiers: the RequireUUIDs option must not apply to them.
                if (!JSON.ParseMandatoryS2Ids("regions",
                                              "regions",
                                              CountryCode.TryParse,
                                              (Options ?? S2ParserOptions.Default) with { RequireUUIDs = false },
                                              1,
                                              null,
                                              out IReadOnlyList<CountryCode>? regions,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region status         [mandatory]

                if (!JSON.ParseMandatoryS2Enum("status",
                                               "status",
                                               EndpointStatus.TryParse,
                                               Options,
                                               out EndpointStatus status,
                                               out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region cem            [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("cem",
                                                  "CEM flag",
                                                  out Boolean cem,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region rm             [mandatory]

                if (!JSON.ParseMandatoryS2Boolean("rm",
                                                  "RM flag",
                                                  out Boolean rm,
                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "id",
                                                    "name",
                                                    "description",
                                                    "icon32",
                                                    "icon128",
                                                    "icon512",
                                                    "pairingUrl",
                                                    "regions",
                                                    "status",
                                                    "cem",
                                                    "rm"))
                {
                    return false;
                }

                #endregion


                EndpointRecord = new EndpointRecord(
                                     id,
                                     name,
                                     description,
                                     icon32,
                                     icon128,
                                     icon512,
                                     pairingUrl,
                                     regions,
                                     status,
                                     cem,
                                     rm
                                 );

                if (CustomEndpointRecordParser is not null)
                    EndpointRecord = CustomEndpointRecordParser(JSON,
                                                                EndpointRecord);

                return true;

            }
            catch (Exception e)
            {
                EndpointRecord  = null;
                ErrorResponse   = "The given JSON representation of an endpoint record is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEndpointRecordSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEndpointRecordSerializer">A delegate to serialize custom endpoint records.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EndpointRecord>? CustomEndpointRecordSerializer = null)
        {

            var json = JSONObject.Create(
                           new JProperty("id",           Id.ToString()),
                           new JProperty("name",         Name),
                           new JProperty("description",  Description),
                           new JProperty("icon32",       Icon32.ToString()),
                           new JProperty("icon128",      Icon128.ToString()),
                           new JProperty("icon512",      Icon512.ToString()),
                           new JProperty("pairingUrl",   PairingUrl.ToString()),
                           new JProperty("regions",      new JArray(Regions.Select(region => region.ToString()))),
                           new JProperty("status",       Status.ToString()),
                           new JProperty("cem",          CEM),
                           new JProperty("rm",           RM)
                       );

            return CustomEndpointRecordSerializer is not null
                       ? CustomEndpointRecordSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this endpoint record.
        /// </summary>
        public EndpointRecord Clone()

            => new (
                   Id.         Clone(),
                   Name.       CloneString(),
                   Description.CloneString(),
                   Icon32.     Clone(),
                   Icon128.    Clone(),
                   Icon512.    Clone(),
                   PairingUrl,                                        // an immutable readonly struct
                   Regions.Select(region => region.Clone()).ToList(),
                   Status.     Clone(),
                   CEM,
                   RM
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two endpoint records for equality.
        /// </summary>
        public static Boolean operator == (EndpointRecord? EndpointRecord1, EndpointRecord? EndpointRecord2)
        {

            if (ReferenceEquals(EndpointRecord1, EndpointRecord2))
                return true;

            if (EndpointRecord1 is null || EndpointRecord2 is null)
                return false;

            return EndpointRecord1.Equals(EndpointRecord2);

        }

        /// <summary>
        /// Compares two endpoint records for inequality.
        /// </summary>
        public static Boolean operator != (EndpointRecord? EndpointRecord1, EndpointRecord? EndpointRecord2)
            => !(EndpointRecord1 == EndpointRecord2);

        #endregion

        #region IEquatable<EndpointRecord> Members

        /// <summary>
        /// Compares two endpoint records for equality.
        /// </summary>
        /// <param name="Object">An endpoint record to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is EndpointRecord endpointRecord && Equals(endpointRecord);

        /// <summary>
        /// Compares two endpoint records for equality.
        /// </summary>
        /// <param name="EndpointRecord">An endpoint record to compare with.</param>
        public Boolean Equals(EndpointRecord? EndpointRecord)

            => EndpointRecord is not null &&

               Id.        Equals(EndpointRecord.Id)         &&
               Icon32.    Equals(EndpointRecord.Icon32)     &&
               Icon128.   Equals(EndpointRecord.Icon128)    &&
               Icon512.   Equals(EndpointRecord.Icon512)    &&
               PairingUrl.Equals(EndpointRecord.PairingUrl) &&
               Status.    Equals(EndpointRecord.Status)     &&

               String.Equals(Name,        EndpointRecord.Name,        StringComparison.Ordinal) &&
               String.Equals(Description, EndpointRecord.Description, StringComparison.Ordinal) &&

               Regions.SequenceEqual(EndpointRecord.Regions) &&

               CEM == EndpointRecord.CEM &&
               RM  == EndpointRecord.RM;

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

                   $"Endpoint '{Name}' ({Id}): {PairingUrl}, regions [{String.Join(", ", Regions)}], status {Status}",

                   CEM ? ", CEM" : "",
                   RM  ? ", RM"  : ""

               );

        #endregion

    }

}

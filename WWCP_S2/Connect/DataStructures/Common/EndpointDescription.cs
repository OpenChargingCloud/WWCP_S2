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
    /// Information about the endpoint (the application) hosting one or more S2 Connect nodes:
    /// its name, logo and deployment (s2-connect-common.yml, EndpointDescription). All
    /// properties are optional in the schema; the deployment is what the pairing process
    /// needs to decide who becomes the communication server.
    /// </summary>
    public sealed class EndpointDescription : IEquatable<EndpointDescription>
    {

        #region Properties

        /// <summary>
        /// The optional user facing name of the endpoint.
        /// </summary>
        [Optional]
        public String?      Name          { get; }

        /// <summary>
        /// The optional URL of a logo of the endpoint.
        /// </summary>
        [Optional]
        public URL?         LogoUrl       { get; }

        /// <summary>
        /// The optional deployment of the endpoint: WAN or LAN.
        /// </summary>
        [Optional]
        public Deployment?  Deployment    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new endpoint description.
        /// </summary>
        /// <param name="Name">An optional user facing name of the endpoint.</param>
        /// <param name="LogoUrl">An optional URL of a logo of the endpoint.</param>
        /// <param name="Deployment">An optional deployment of the endpoint: WAN or LAN.</param>
        public EndpointDescription(String?      Name         = null,
                                   URL?         LogoUrl      = null,
                                   Deployment?  Deployment   = null)
        {

            this.Name        = Name;
            this.LogoUrl     = LogoUrl;
            this.Deployment  = Deployment;

            unchecked
            {
                hashCode = (this.Name?.      GetHashCode(StringComparison.Ordinal) ?? 0) * 5 ^
                           (this.LogoUrl?.   GetHashCode() ?? 0)                         * 3 ^
                           (this.Deployment?.GetHashCode() ?? 0);
            }

        }

        #endregion


        #region Documentation

        // s2-connect-common.yml
        //   EndpointDescription:
        //     description: Information about the pairing endpoint of a node
        //     properties:
        //       name:        { type: string }
        //       logoUrl:     { type: string, format: uri }
        //       deployment:  { $ref: Deployment }   (enum ["WAN", "LAN"])

        #endregion

        #region (static) TryParse(JSON, out EndpointDescription, out ErrorResponse, ...)

        // Note: The following overloads are needed to satisfy the parser delegates! Do not refactor them!

        /// <summary>
        /// Try to parse the given JSON representation of an endpoint description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EndpointDescription">The parsed endpoint description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        public static Boolean TryParse(JObject                                        JSON,
                                       [NotNullWhen(true)]  out EndpointDescription?  EndpointDescription,
                                       [NotNullWhen(false)] out String?               ErrorResponse)

            => TryParse(JSON,
                        out EndpointDescription,
                        out ErrorResponse,
                        null,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an endpoint description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EndpointDescription">The parsed endpoint description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                                        JSON,
                                       [NotNullWhen(true)]  out EndpointDescription?  EndpointDescription,
                                       [NotNullWhen(false)] out String?               ErrorResponse,
                                       S2ParserOptions?                               Options)

            => TryParse(JSON,
                        out EndpointDescription,
                        out ErrorResponse,
                        Options,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an endpoint description.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EndpointDescription">The parsed endpoint description.</param>
        /// <param name="ErrorResponse">An error message when parsing failed.</param>
        /// <param name="Options">Optional parser options.</param>
        /// <param name="CustomEndpointDescriptionParser">A delegate to parse custom endpoint descriptions.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out EndpointDescription?      EndpointDescription,
                                       [NotNullWhen(false)] out String?                   ErrorResponse,
                                       S2ParserOptions?                                   Options,
                                       CustomJObjectParserDelegate<EndpointDescription>?  CustomEndpointDescriptionParser)
        {

            try
            {

                EndpointDescription = null;

                #region name          [optional]

                if (!JSON.ParseOptionalS2String("name",
                                                "name",
                                                out String? name,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region logoUrl       [optional]

                if (!JSON.ParseOptionalS2URL("logoUrl",
                                             "logo URL",
                                             out URL? logoUrl,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region deployment    [optional]

                if (!JSON.ParseOptionalS2Enum("deployment",
                                              "deployment",
                                              Connect.Deployment.TryParse,
                                              Options,
                                              out Deployment? deployment,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "name",
                                                    "logoUrl",
                                                    "deployment"))
                {
                    return false;
                }

                #endregion


                EndpointDescription = new EndpointDescription(
                                          name,
                                          logoUrl,
                                          deployment
                                      );

                if (CustomEndpointDescriptionParser is not null)
                    EndpointDescription = CustomEndpointDescriptionParser(JSON,
                                                                          EndpointDescription);

                return true;

            }
            catch (Exception e)
            {
                EndpointDescription  = null;
                ErrorResponse        = "The given JSON representation of an endpoint description is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEndpointDescriptionSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEndpointDescriptionSerializer">A delegate to serialize custom endpoint descriptions.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EndpointDescription>? CustomEndpointDescriptionSerializer = null)
        {

            var json = JSONObject.Create(

                           Name is not null
                               ? new JProperty("name",        Name)
                               : null,

                           LogoUrl.HasValue
                               ? new JProperty("logoUrl",     LogoUrl.Value.ToString())
                               : null,

                           Deployment.HasValue
                               ? new JProperty("deployment",  Deployment.Value.ToString())
                               : null

                       );

            return CustomEndpointDescriptionSerializer is not null
                       ? CustomEndpointDescriptionSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this endpoint description.
        /// </summary>
        public EndpointDescription Clone()

            => new (
                   Name?.CloneString(),
                   LogoUrl?.Clone(),
                   Deployment?.Clone()
               );

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two endpoint descriptions for equality.
        /// </summary>
        public static Boolean operator == (EndpointDescription? EndpointDescription1, EndpointDescription? EndpointDescription2)
        {

            if (ReferenceEquals(EndpointDescription1, EndpointDescription2))
                return true;

            if (EndpointDescription1 is null || EndpointDescription2 is null)
                return false;

            return EndpointDescription1.Equals(EndpointDescription2);

        }

        /// <summary>
        /// Compares two endpoint descriptions for inequality.
        /// </summary>
        public static Boolean operator != (EndpointDescription? EndpointDescription1, EndpointDescription? EndpointDescription2)
            => !(EndpointDescription1 == EndpointDescription2);

        #endregion

        #region IEquatable<EndpointDescription> Members

        /// <summary>
        /// Compares two endpoint descriptions for equality.
        /// </summary>
        /// <param name="Object">An endpoint description to compare with.</param>
        public override Boolean Equals(Object? Object)
            => Object is EndpointDescription endpointDescription && Equals(endpointDescription);

        /// <summary>
        /// Compares two endpoint descriptions for equality.
        /// </summary>
        /// <param name="EndpointDescription">An endpoint description to compare with.</param>
        public Boolean Equals(EndpointDescription? EndpointDescription)

            => EndpointDescription is not null &&

               String.Equals(Name, EndpointDescription.Name, StringComparison.Ordinal) &&
               Nullable.Equals(LogoUrl,    EndpointDescription.LogoUrl) &&
               Nullable.Equals(Deployment, EndpointDescription.Deployment);

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

                   Name ?? "endpoint",

                   Deployment.HasValue
                       ? $" ({Deployment.Value})"
                       : ""

               );

        #endregion

    }

}

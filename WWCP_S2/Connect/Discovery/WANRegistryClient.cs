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

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.TLS;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// Options of the WAN registry client.
    /// </summary>
    public sealed class WANRegistryClientOptions
    {

        /// <summary>
        /// The registry API versions this client implements (default: v1).
        /// </summary>
        public IReadOnlyList<String>  SupportedAPIVersions            { get; init; } = [ Version.S2ConnectAPIVersion ];

        /// <summary>
        /// The timeout of a single request (default: 10 s).
        /// </summary>
        public TimeSpan               RequestTimeout                  { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The parser options for endpoint records (AllowInsecureURLs accepts "http://" pairing URLs).
        /// </summary>
        public S2ParserOptions        ParserOptions                   { get; init; } = S2ParserOptions.Default;

        /// <summary>
        /// Whether self-signed server certificates are accepted (default: false; a public registry has a CA-issued certificate).
        /// </summary>
        public Boolean                AcceptSelfSignedCertificates    { get; init; }

    }


    /// <summary>
    /// The client of the S2 Connect WAN endpoint registry (s2-connect-wan-endpoint-registry.yml):
    /// the version index of the registry URL, GET /v1/endpoint with filters and paging, and
    /// GET /v1/endpoint/{id}. Results are never exceptions: a 400 or 404 answers with a failed
    /// operation result carrying the status code.
    /// </summary>
    public sealed class WANRegistryClient : AS2ConnectClient
    {

        #region Data

        /// <summary>
        /// The default HTTP user agent.
        /// </summary>
        public new const String DefaultHTTPUserAgent = "GraphDefined S2 WAN Registry Client";

        #endregion

        #region Properties

        /// <summary>
        /// The options of this client.
        /// </summary>
        public WANRegistryClientOptions  Options    { get; }

        /// <summary>
        /// The registry URL (the base of the version index, e.g. "https://registry.example.org/").
        /// </summary>
        public S2BaseURL                 RegistryUrl
            => BaseUrl;

        /// <inheritdoc/>
        protected override TimeSpan      VersionIndexTimeout
            => Options.RequestTimeout;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new WAN registry client.
        /// </summary>
        /// <param name="RegistryUrl">The registry URL (with a trailing slash, without the API version).</param>
        /// <param name="Options">Optional client options.</param>
        /// <param name="RemoteCertificateValidator">An optional TLS server certificate validator.</param>
        /// <param name="TimeProvider">An optional time provider.</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        /// <param name="DNSClient">An optional DNS client.</param>
        /// <param name="HTTPUserAgent">An optional HTTP user agent.</param>
        /// <param name="Description">An optional description of this client.</param>
        /// <param name="DisableLogging">Whether to disable Hermod's HTTP logging (default: true).</param>
        public WANRegistryClient(S2BaseURL                                                  RegistryUrl,
                                 WANRegistryClientOptions?                                  Options                      = null,
                                 RemoteTLSServerCertificateValidationHandler<IHTTPClient>?  RemoteCertificateValidator   = null,
                                 TimeProvider?                                              TimeProvider                 = null,
                                 ILoggerFactory?                                            LoggerFactory                = null,
                                 IDNSClient?                                                DNSClient                    = null,
                                 String?                                                    HTTPUserAgent                = null,
                                 I18NString?                                                Description                  = null,
                                 Boolean?                                                   DisableLogging               = null)

            : base(RegistryUrl,
                   (Options ?? new WANRegistryClientOptions()).SupportedAPIVersions,
                   (Options ?? new WANRegistryClientOptions()).ParserOptions,
                   (Options ?? new WANRegistryClientOptions()).AcceptSelfSignedCertificates,
                   null,
                   RemoteCertificateValidator,
                   TimeProvider,
                   LoggerFactory,
                   DNSClient,
                   HTTPUserAgent,
                   DefaultHTTPUserAgent,
                   Description,
                   DisableLogging)

        {

            this.Options = Options ?? new WANRegistryClientOptions();

            if (this.Options.RequestTimeout <= TimeSpan.Zero)
                throw new ArgumentException("The request timeout must be positive!", nameof(Options));

        }

        #endregion


        #region QueryEndpointsAsync(Query = null, CancellationToken = default)

        /// <summary>
        /// Query the registry (GET /v1/endpoint): without a query every public endpoint is returned.
        /// </summary>
        /// <param name="Query">An optional query (regions, status, CEM/RM flags, paging).</param>
        /// <param name="CancellationToken">A token to cancel the request.</param>
        public async Task<ClientOperationResult<IReadOnlyList<EndpointRecord>>> QueryEndpointsAsync(WANRegistryQuery?  Query               = null,
                                                                                                    CancellationToken  CancellationToken   = default)
        {

            if (await EnsureVersionAsync(CancellationToken).ConfigureAwait(false) is { } versionFailure)
                return VersionFailure<IReadOnlyList<EndpointRecord>>(versionFailure);

            var queryString = (Query ?? WANRegistryQuery.All).ToQueryString();

            var result = await SendAsync(HTTPMethod.GET,
                                         OperationPath("endpoint"),
                                         null,
                                         null,
                                         Options.RequestTimeout,
                                         CancellationToken,
                                         queryString.Any() ? queryString : null).ConfigureAwait(false);

            if (result.IsTransportFailure)
                return Failure<IReadOnlyList<EndpointRecord>>(result);

            if (result.StatusCode != HTTPStatusCode.OK)
                return Failure<IReadOnlyList<EndpointRecord>>(result, StatusDescription(result));

            if (!TryReadJSON(result, out var json, out var error) || json is not JArray jsonArray)
                return Failure<IReadOnlyList<EndpointRecord>>(result, $"the endpoint list is not a JSON array: {error ?? "wrong JSON type"}");

            var records = new List<EndpointRecord>();

            foreach (var token in jsonArray)
            {

                if (token is not JObject recordJSON ||
                    !EndpointRecord.TryParse(recordJSON, out var record, out var parseError, Options.ParserOptions))
                {
                    return Failure<IReadOnlyList<EndpointRecord>>(result, $"an endpoint record could not be parsed: {(token is JObject ? "" : "not a JSON object")}");
                }

                records.Add(record);

            }

            return new ClientOperationResult<IReadOnlyList<EndpointRecord>>(result.StatusCode, true, records);

        }

        #endregion

        #region GetEndpointAsync(Id, CancellationToken = default)

        /// <summary>
        /// Get a single endpoint record (GET /v1/endpoint/{id}); an unknown identification answers a failed result with status 404.
        /// </summary>
        /// <param name="Id">The identification of the endpoint record.</param>
        /// <param name="CancellationToken">A token to cancel the request.</param>
        public async Task<ClientOperationResult<EndpointRecord>> GetEndpointAsync(EndpointRecord_Id  Id,
                                                                                  CancellationToken  CancellationToken   = default)
        {

            if (await EnsureVersionAsync(CancellationToken).ConfigureAwait(false) is { } versionFailure)
                return VersionFailure<EndpointRecord>(versionFailure);

            var result = await SendAsync(HTTPMethod.GET,
                                         OperationPath("endpoint/" + Id.ToString()),
                                         null,
                                         null,
                                         Options.RequestTimeout,
                                         CancellationToken).ConfigureAwait(false);

            if (result.IsTransportFailure)
                return Failure<EndpointRecord>(result);

            if (result.StatusCode != HTTPStatusCode.OK)
                return Failure<EndpointRecord>(result, StatusDescription(result));

            if (!TryReadJSON(result, out var json, out var error) || json is not JObject recordJSON)
                return Failure<EndpointRecord>(result, $"the endpoint record is not a JSON object: {error ?? "wrong JSON type"}");

            if (!EndpointRecord.TryParse(recordJSON, out var record, out var parseError, Options.ParserOptions))
                return Failure<EndpointRecord>(result, $"the endpoint record could not be parsed: {parseError}");

            return new ClientOperationResult<EndpointRecord>(result.StatusCode, true, record);

        }

        #endregion


        #region (private static) StatusDescription(Result)

        private static String StatusDescription(SendResult Result)
        {

            var body = Result.Response is not null ? BodyText(Result.Response) : null;

            return Result.StatusCode == HTTPStatusCode.NotFound   ? $"no endpoint record with the given identification{(body is not null ? $": {body}" : "")}" :
                   Result.StatusCode == HTTPStatusCode.BadRequest ? $"invalid query parameters{(body is not null ? $": {body}" : "")}" :
                                                                    $"unexpected status code {Result.StatusCode.Code}{(body is not null ? $": {body}" : "")}";

        }

        #endregion

    }

}

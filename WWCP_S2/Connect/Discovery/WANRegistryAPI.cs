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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The endpoint records behind a WAN registry API.
    /// </summary>
    public interface IWANRegistry
    {

        /// <summary>
        /// Return the endpoint records matching the given query (filtered and paged).
        /// </summary>
        /// <param name="Query">A registry query.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        ValueTask<IReadOnlyList<EndpointRecord>>  QueryAsync(WANRegistryQuery   Query,
                                                             CancellationToken  CancellationToken   = default);

        /// <summary>
        /// Return the endpoint record with the given identification, or null.
        /// </summary>
        /// <param name="Id">The identification of an endpoint record.</param>
        /// <param name="CancellationToken">A token to cancel the operation.</param>
        ValueTask<EndpointRecord?>                GetAsync  (EndpointRecord_Id  Id,
                                                             CancellationToken  CancellationToken   = default);

    }


    /// <summary>
    /// An in-memory WAN registry: the reference implementation and test double.
    /// </summary>
    public sealed class InMemoryWANRegistry : IWANRegistry
    {

        #region Data

        private readonly Lock                                          recordsLock  = new();
        private readonly Dictionary<EndpointRecord_Id, EndpointRecord>  records     = [];

        #endregion

        #region Properties

        /// <summary>
        /// All endpoint records, in insertion order.
        /// </summary>
        public IReadOnlyList<EndpointRecord>  Records
        {
            get
            {
                lock (recordsLock)
                    return [.. records.Values];
            }
        }

        /// <summary>
        /// The number of endpoint records.
        /// </summary>
        public Int32                          Count
        {
            get
            {
                lock (recordsLock)
                    return records.Count;
            }
        }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new in-memory WAN registry.
        /// </summary>
        /// <param name="Records">Optional initial endpoint records.</param>
        public InMemoryWANRegistry(IEnumerable<EndpointRecord>? Records = null)
        {
            foreach (var record in Records ?? [])
                AddOrReplace(record);
        }

        #endregion


        #region AddOrReplace(Record)

        /// <summary>
        /// Add the given endpoint record, replacing a record with the same identification.
        /// </summary>
        /// <param name="Record">An endpoint record.</param>
        public InMemoryWANRegistry AddOrReplace(EndpointRecord Record)
        {

            ArgumentNullException.ThrowIfNull(Record);

            lock (recordsLock)
                records[Record.Id] = Record;

            return this;

        }

        #endregion

        #region Remove(Id)

        /// <summary>
        /// Remove the endpoint record with the given identification.
        /// </summary>
        /// <param name="Id">The identification of an endpoint record.</param>
        /// <returns>Whether a record was removed.</returns>
        public Boolean Remove(EndpointRecord_Id Id)
        {
            lock (recordsLock)
                return records.Remove(Id);
        }

        #endregion

        #region Clear()

        /// <summary>
        /// Remove every endpoint record.
        /// </summary>
        public void Clear()
        {
            lock (recordsLock)
                records.Clear();
        }

        #endregion

        #region QueryAsync(Query, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<EndpointRecord>> QueryAsync(WANRegistryQuery   Query,
                                                                   CancellationToken  CancellationToken   = default)
        {

            ArgumentNullException.ThrowIfNull(Query);

            return new (Query.Apply(Records));

        }

        #endregion

        #region GetAsync(Id, CancellationToken = default)

        /// <inheritdoc/>
        public ValueTask<EndpointRecord?> GetAsync(EndpointRecord_Id  Id,
                                                   CancellationToken  CancellationToken   = default)
        {
            lock (recordsLock)
                return new (records.TryGetValue(Id, out var record) ? record : null);
        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"in-memory WAN registry ({Count} record(s))";

        #endregion

    }


    /// <summary>
    /// The HTTP API of an S2 Connect WAN endpoint registry (s2-connect-wan-endpoint-registry.yml)
    /// on Hermod's HTTP API: GET {root} answers the version index, GET {root}v1/endpoint the filtered
    /// and paged records (400 on invalid parameters) and GET {root}v1/endpoint/{id} a single record
    /// (404 when unknown). Mount it on a host supplied HTTP server; the records come from an
    /// <see cref="IWANRegistry"/>.
    /// </summary>
    public sealed class WANRegistryAPI : HTTPAPI
    {

        #region Data

        /// <summary>
        /// The default HTTP service name.
        /// </summary>
        public new const String  DefaultHTTPServiceName  = "GraphDefined S2 WAN Registry";

        /// <summary>
        /// The registry API version implemented by this API.
        /// </summary>
        public     const String  APIVersion              = Version.S2ConnectAPIVersion;

        /// <summary>
        /// The default number of requests one remote address may send per minute: 300.
        /// A WAN registry is reachable from the whole internet, so it is rate limited by default.
        /// </summary>
        public     const Int32   DefaultRateLimitCapacity  = 300;

        private readonly ILogger?               logger;
        private readonly S2RequestRateLimiter?  requestRateLimiter;

        #endregion

        #region Properties

        /// <summary>
        /// The endpoint records.
        /// </summary>
        public IWANRegistry           Registry                { get; }

        /// <summary>
        /// The registry API versions offered by the version index.
        /// </summary>
        public IReadOnlyList<String>  SupportedAPIVersions    { get; } = [ APIVersion ];

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new WAN registry API on the given HTTP server.
        /// </summary>
        /// <param name="HTTPServer">The HTTP server.</param>
        /// <param name="Registry">The endpoint records.</param>
        /// <param name="RootPath">An optional root path (default: "/"); it always ends with a slash.</param>
        /// <param name="RateLimitCapacity">The number of requests one remote address may send per <paramref name="RateLimitRefillPeriod"/> (default: 300); zero or less disables the rate limit.</param>
        /// <param name="RateLimitRefillPeriod">The period within which the request budget of a remote address refills completely (default: one minute).</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        /// <param name="HTTPServerName">An optional HTTP server name.</param>
        /// <param name="HTTPServiceName">An optional HTTP service name.</param>
        /// <param name="DisableLogging">Whether to disable Hermod's HTTP logging (default: true).</param>
        /// <param name="LoggingPath">An optional logging path.</param>
        /// <param name="RegisterWithinHTTPServer">Whether to register the API within the HTTP server (default: true).</param>
        public WANRegistryAPI(HTTPServer       HTTPServer,
                              IWANRegistry     Registry,
                              HTTPPath?        RootPath                   = null,
                              Int32?           RateLimitCapacity          = null,
                              TimeSpan?        RateLimitRefillPeriod      = null,
                              ILoggerFactory?  LoggerFactory              = null,
                              String?          HTTPServerName             = null,
                              String?          HTTPServiceName            = null,
                              Boolean?         DisableLogging             = null,
                              String?          LoggingPath                = null,
                              Boolean          RegisterWithinHTTPServer   = true)

            : base(HTTPServer,
                   RootPath:                  NormaliseRootPath(RootPath ?? HTTPPath.Root),
                   Description:               I18NString.Create(HTTPServiceName ?? DefaultHTTPServiceName),
                   HTTPServerName:            HTTPServerName,
                   HTTPServiceName:           HTTPServiceName ?? DefaultHTTPServiceName,
                   DisableLogging:            DisableLogging  ?? true,
                   LoggingPath:               LoggingPath,
                   RegisterWithinHTTPServer:  RegisterWithinHTTPServer)

        {

            ArgumentNullException.ThrowIfNull(Registry);

            this.Registry            = Registry;
            this.logger              = LoggerFactory?.CreateLogger<WANRegistryAPI>();

            var rateLimitCapacity    = RateLimitCapacity ?? DefaultRateLimitCapacity;

            this.requestRateLimiter  = rateLimitCapacity > 0
                                           ? new S2RequestRateLimiter(
                                                 "wanRegistry",
                                                 rateLimitCapacity,
                                                 RateLimitRefillPeriod ?? TimeSpan.FromMinutes(1)
                                             )
                                           : null;

            RegisterURLTemplates();

        }

        private static HTTPPath NormaliseRootPath(HTTPPath Path)

            => Path.IsNullOrEmpty
                   ? HTTPPath.Root
                   : Path.EndsWith("/")
                         ? Path
                         : HTTPPath.Parse(Path.ToString() + "/");

        #endregion


        #region (private) RegisterURLTemplates()

        private void RegisterURLTemplates()
        {

            // Every handler is wrapped into the per-source rate limit: a WAN registry answers
            // the whole internet.

            // GET {root}                    =>  ["v1"]
            AddHandler(HTTPMethod.GET, HTTPPath.Root,                                    RateLimited(HandleVersionIndexAsync));

            // GET {root}v1/endpoint?...     =>  [ EndpointRecord, ... ]
            AddHandler(HTTPMethod.GET, HTTPPath.Parse($"/{APIVersion}/endpoint"),        RateLimited(HandleQueryEndpointsAsync));

            // GET {root}v1/endpoint/{id}    =>  EndpointRecord
            AddHandler(HTTPMethod.GET, HTTPPath.Parse($"/{APIVersion}/endpoint/{{id}}"), RateLimited(HandleGetEndpointAsync));

        }

        /// <summary>
        /// Wrap an HTTP handler into the per-source rate limit of this API.
        /// </summary>
        /// <param name="Handler">The HTTP handler to wrap.</param>
        private HTTPDelegate RateLimited(HTTPDelegate Handler)

            => async request => {

                   if (requestRateLimiter is null)
                       return await Handler(request).ConfigureAwait(false);

                   var remoteAddress  = RemoteAddressOf(request);
                   var decision       = requestRateLimiter.TryAcquire(remoteAddress);

                   if (decision.Allowed)
                       return await Handler(request).ConfigureAwait(false);

                   logger?.LogWarning(
                       "S2 WAN registry: the request budget of {RemoteAddress} is exhausted, retry in {RetryAfter} seconds.",
                       remoteAddress?.ToString() ?? S2RequestRateLimiter.UnknownAddress,
                       Math.Ceiling(decision.RetryAfter.TotalSeconds)
                   );

                   return JSONResponse(
                              request,
                              HTTPStatusCode.ServiceUnavailable,
                              new JObject(new JProperty("message", "Too many requests.")),
                              decision.RetryAfter
                          );

               };

        private static System.Net.IPAddress? RemoteAddressOf(HTTPRequest Request)
        {

            try
            {

                var address = Request.RemoteSocket.IPAddress;

                return address is null
                           ? null
                           : new System.Net.IPAddress(address.GetBytes());

            }
            catch (Exception)
            {
                return null;
            }

        }

        #endregion

        #region (private) HandleVersionIndexAsync(Request)

        private Task<HTTPResponse> HandleVersionIndexAsync(HTTPRequest Request)

            => Task.FromResult(
                   JSONResponse(Request,
                                HTTPStatusCode.OK,
                                new JArray(SupportedAPIVersions))
               );

        #endregion

        #region (private) HandleQueryEndpointsAsync(Request)

        private async Task<HTTPResponse> HandleQueryEndpointsAsync(HTTPRequest Request)
        {

            if (!WANRegistryQuery.TryParse(Request.QueryString, out var query, out var error))
                return JSONResponse(Request,
                                    HTTPStatusCode.BadRequest,
                                    new JObject(new JProperty("message", error)));

            var records = await Registry.QueryAsync(query, Request.CancellationToken).ConfigureAwait(false);

            return JSONResponse(Request,
                                HTTPStatusCode.OK,
                                new JArray(records.Select(record => record.ToJSON())));

        }

        #endregion

        #region (private) HandleGetEndpointAsync(Request)

        private async Task<HTTPResponse> HandleGetEndpointAsync(HTTPRequest Request)
        {

            // Hermod fills the named template parameters ("{id}"); older servers only the positional array.
            var idText = Request.ParsedURLParametersX.TryGetValue("id", out var namedId)
                             ? namedId
                             : Request.ParsedURLParameters.Length > 0
                                   ? Request.ParsedURLParameters[0]
                                   : null;

            if (idText is null ||
                !EndpointRecord_Id.TryParse(idText.Trim(), out var id))
            {
                return JSONResponse(Request,
                                    HTTPStatusCode.NotFound,
                                    new JObject(new JProperty("message", $"The endpoint record identification '{idText}' is not a UUID!")));
            }

            var record = await Registry.GetAsync(id, Request.CancellationToken).ConfigureAwait(false);

            if (record is null)
                return JSONResponse(Request,
                                    HTTPStatusCode.NotFound,
                                    new JObject(new JProperty("message", $"No endpoint record with identification '{id}' found!")));

            return JSONResponse(Request,
                                HTTPStatusCode.OK,
                                record.ToJSON());

        }

        #endregion


        #region (private) JSONResponse(Request, StatusCode, JSON)

        private HTTPResponse JSONResponse(HTTPRequest     Request,
                                          HTTPStatusCode  StatusCode,
                                          JToken          JSON,
                                          TimeSpan?       RetryAfter   = null)
        {

            try
            {
                Request.TryReadHTTPBodyStream();
            }
            catch (Exception e)
            {
                logger?.LogDebug(e, "S2 WAN registry: could not drain the request body.");
            }

            var builder = new HTTPResponse.Builder(Request) {
                              HTTPStatusCode  = StatusCode,
                              Server          = HTTPServiceName,
                              Date            = org.GraphDefined.Vanaheimr.Illias.Timestamp.Now,
                              Connection      = ConnectionType.KeepAlive,
                              ContentType     = HTTPContentType.Application.JSON_UTF8,
                              Content         = JSON.ToString(Formatting.None).ToUTF8Bytes()
                          };

            if (RetryAfter.HasValue)
                builder.RetryAfter = Math.Max(1, (Int64) Math.Ceiling(RetryAfter.Value.TotalSeconds)).ToString();

            return builder.AsImmutable;

        }

        #endregion

    }

}

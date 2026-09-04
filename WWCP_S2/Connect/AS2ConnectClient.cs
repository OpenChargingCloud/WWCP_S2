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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
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
    /// The common base of the S2 Connect HTTPS clients (pairing, session initiation): the
    /// version index and version selection (S2 Connect 1.0.0, "Selecting the version of REST
    /// APIs"), JSON requests with bearer tokens and deadline-aware timeouts on Hermod's
    /// <see cref="AHTTPClient"/>, observation of the TLS server certificate fingerprint and the
    /// acceptance of self-signed certificates where the specification allows it. Transport
    /// failures never surface as exceptions (cancellation excepted).
    /// </summary>
    public abstract class AS2ConnectClient : AHTTPClient
    {

        #region Data

        private static readonly AcceptTypes  jsonAccept = AcceptTypes.FromHTTPContentTypes(HTTPContentType.Application.JSON_UTF8);

        private readonly ValidatorState                                             validatorState;
        private readonly RemoteTLSServerCertificateValidationHandler<IHTTPClient>?  customCertificateValidator;
        private          CertificateFingerprint?                                    observedFingerprint;
        private          String?                                                    selectedAPIVersion;

        /// <summary>
        /// Forwards the TLS validation callback (registered before the client exists) to the client.
        /// </summary>
        private sealed class ValidatorState
        {

            public AS2ConnectClient?  Client    { get; set; }

            public TLSValidationResult Validate(Object             Sender,
                                                X509Certificate2?  Certificate,
                                                X509Chain?         CertificateChain,
                                                IHTTPClient        HTTPClient,
                                                SslPolicyErrors    PolicyErrors)

                => Client is not null
                       ? Client.ValidateServerCertificate(Sender, Certificate, CertificateChain, HTTPClient, PolicyErrors)
                       : TLSValidationExtensions.AskTheOS(Sender, Certificate, CertificateChain, HTTPClient, PolicyErrors);

        }

        /// <summary>
        /// The outcome of one request: the response, or a synthetic failure (transport,
        /// certificate change, deadline) that never came from the server.
        /// </summary>
        protected sealed record SendResult(HTTPResponse?   Response,
                                           HTTPStatusCode  StatusCode,
                                           String?         FailureText,
                                           Boolean         CertificateChanged,
                                           Boolean         DeadlineExceeded)
        {

            /// <summary>
            /// Whether the server could not be reached.
            /// </summary>
            public Boolean  IsTransportFailure
                => StatusCode.Code == 0;

            /// <summary>
            /// Wrap a response of the server.
            /// </summary>
            public static SendResult From(HTTPResponse Response)
                => new (Response, Response.HTTPStatusCode, null, false, false);

            /// <summary>
            /// A transport failure.
            /// </summary>
            public static SendResult TransportFailure(String Text)
                => new (null, HTTPStatusCode.ClientError, Text, false, false);

            /// <summary>
            /// The TLS server certificate changed between two requests.
            /// </summary>
            public static SendResult ChangedCertificate()
                => new (null, HTTPStatusCode.ClientError, "the TLS server certificate changed during the attempt", true, false);

            /// <summary>
            /// The deadline of the procedure passed.
            /// </summary>
            public static SendResult Timeout()
                => new (null, HTTPStatusCode.RequestTimeout, "the server did not respond within the maximum duration of the attempt", false, true);

        }

        #endregion

        #region Properties

        /// <summary>
        /// The base URL of the remote API.
        /// </summary>
        public S2BaseURL               BaseUrl                               { get; }

        /// <summary>
        /// The versions of the API this client implements.
        /// </summary>
        public IReadOnlyList<String>   SupportedAPIVersions                  { get; }

        /// <summary>
        /// The parser options for response bodies.
        /// </summary>
        public S2ParserOptions         ParserOptions                         { get; }

        /// <summary>
        /// Whether self-signed server certificates are accepted (only chain errors reported by
        /// the TLS stack; the validity period is still checked).
        /// </summary>
        public Boolean                 AcceptSelfSignedCertificates          { get; }

        /// <summary>
        /// The time provider of this client.
        /// </summary>
        public TimeProvider            TimeProvider                          { get; }

        /// <summary>
        /// The fingerprint assumed for the server certificate when none was observed via TLS
        /// (plain HTTP tests and development only).
        /// </summary>
        public CertificateFingerprint?  AssumedServerCertificateFingerprint  { get; }

        /// <summary>
        /// The fingerprint of the TLS server certificate observed on the last handshake, or the
        /// assumed fingerprint.
        /// </summary>
        public CertificateFingerprint?  ServerCertificateFingerprint
            => observedFingerprint ?? AssumedServerCertificateFingerprint;

        /// <summary>
        /// The normalised domain name of the base URL.
        /// </summary>
        public String                  ServerDomainName                      { get; }

        /// <summary>
        /// The API version selected from the version index of the server, once known.
        /// </summary>
        public String?                 SelectedAPIVersion
            => selectedAPIVersion;

        /// <summary>
        /// The versions of the API the server implements, once known.
        /// </summary>
        public IReadOnlyList<String>?  ServerAPIVersions                     { get; private set; }

        /// <summary>
        /// The optional logger.
        /// </summary>
        protected ILogger?             Logger                                { get; }

        /// <summary>
        /// The construction parameters kept for clones.
        /// </summary>
        protected ILoggerFactory?      LoggerFactoryValue                    { get; }

        /// <summary>
        /// The construction parameters kept for clones.
        /// </summary>
        protected IDNSClient?          DNSClientValue                        { get; }

        /// <summary>
        /// The construction parameters kept for clones.
        /// </summary>
        protected String?              HTTPUserAgentValue                    { get; }

        /// <summary>
        /// The construction parameters kept for clones.
        /// </summary>
        protected I18NString?          DescriptionValue                      { get; }

        /// <summary>
        /// The construction parameters kept for clones.
        /// </summary>
        protected Boolean?             DisableLoggingValue                   { get; }

        /// <summary>
        /// The construction parameters kept for clones.
        /// </summary>
        protected RemoteTLSServerCertificateValidationHandler<IHTTPClient>?  CustomCertificateValidator
            => customCertificateValidator;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new S2 Connect HTTPS client.
        /// </summary>
        /// <param name="BaseUrl">The base URL of the remote API.</param>
        /// <param name="SupportedAPIVersions">The versions of the API this client implements.</param>
        /// <param name="ParserOptions">The parser options for response bodies.</param>
        /// <param name="AcceptSelfSignedCertificates">Whether self-signed server certificates are accepted.</param>
        /// <param name="AssumedServerCertificateFingerprint">The server certificate fingerprint to use when none is observed via TLS (plain HTTP only).</param>
        /// <param name="RemoteCertificateValidator">An optional custom TLS server certificate validator.</param>
        /// <param name="TimeProvider">An optional time provider (default: the system clock).</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        /// <param name="DNSClient">An optional DNS client.</param>
        /// <param name="HTTPUserAgent">An optional HTTP user agent.</param>
        /// <param name="DefaultUserAgent">The user agent used when none is given.</param>
        /// <param name="Description">An optional description.</param>
        /// <param name="DisableLogging">Whether to disable Hermod's client logging (default: true).</param>
        protected AS2ConnectClient(S2BaseURL                                                  BaseUrl,
                                   IReadOnlyList<String>                                      SupportedAPIVersions,
                                   S2ParserOptions                                            ParserOptions,
                                   Boolean                                                    AcceptSelfSignedCertificates,
                                   CertificateFingerprint?                                    AssumedServerCertificateFingerprint,
                                   RemoteTLSServerCertificateValidationHandler<IHTTPClient>?  RemoteCertificateValidator,
                                   TimeProvider?                                              TimeProvider,
                                   ILoggerFactory?                                            LoggerFactory,
                                   IDNSClient?                                                DNSClient,
                                   String?                                                    HTTPUserAgent,
                                   String                                                     DefaultUserAgent,
                                   I18NString?                                                Description,
                                   Boolean?                                                   DisableLogging)

            : this(new ValidatorState(),
                   BaseUrl,
                   SupportedAPIVersions,
                   ParserOptions,
                   AcceptSelfSignedCertificates,
                   AssumedServerCertificateFingerprint,
                   RemoteCertificateValidator,
                   TimeProvider,
                   LoggerFactory,
                   DNSClient,
                   HTTPUserAgent,
                   DefaultUserAgent,
                   Description,
                   DisableLogging)

        { }

        private AS2ConnectClient(ValidatorState                                             ValidatorState,
                                 S2BaseURL                                                  BaseUrl,
                                 IReadOnlyList<String>                                      SupportedAPIVersions,
                                 S2ParserOptions                                            ParserOptions,
                                 Boolean                                                    AcceptSelfSignedCertificates,
                                 CertificateFingerprint?                                    AssumedServerCertificateFingerprint,
                                 RemoteTLSServerCertificateValidationHandler<IHTTPClient>?  RemoteCertificateValidator,
                                 TimeProvider?                                              TimeProvider,
                                 ILoggerFactory?                                            LoggerFactory,
                                 IDNSClient?                                                DNSClient,
                                 String?                                                    HTTPUserAgent,
                                 String                                                     DefaultUserAgent,
                                 I18NString?                                                Description,
                                 Boolean?                                                   DisableLogging)

            : base(BaseUrl.URL,
                   Description,
                   HTTPUserAgent ?? DefaultUserAgent,
                   null,
                   jsonAccept,
                   HTTPContentType.Application.JSON_UTF8,
                   ConnectionType.KeepAlive,
                   RemoteCertificateValidator:  ValidatorState.Validate,
                   MaxNumberOfRetries:          1,
                   DisableLogging:              DisableLogging ?? true,
                   DNSClient:                   DNSClient,
                   LoggerFactory:               LoggerFactory)

        {

            if (String.IsNullOrEmpty(BaseUrl.Value))
                throw new ArgumentException("The base URL must not be empty!", nameof(BaseUrl));

            ArgumentNullException.ThrowIfNull(SupportedAPIVersions);
            ArgumentNullException.ThrowIfNull(ParserOptions);

            if (SupportedAPIVersions.Count == 0)
                throw new ArgumentException("At least one API version must be supported!", nameof(SupportedAPIVersions));

            this.validatorState                       = ValidatorState;
            this.BaseUrl                              = BaseUrl;
            this.SupportedAPIVersions                 = SupportedAPIVersions;
            this.ParserOptions                        = ParserOptions;
            this.AcceptSelfSignedCertificates         = AcceptSelfSignedCertificates;
            this.AssumedServerCertificateFingerprint  = AssumedServerCertificateFingerprint;
            this.customCertificateValidator           = RemoteCertificateValidator;
            this.TimeProvider                         = TimeProvider ?? System.TimeProvider.System;
            this.Logger                               = LoggerFactory?.CreateLogger(GetType());
            this.LoggerFactoryValue                   = LoggerFactory;
            this.DNSClientValue                       = DNSClient;
            this.HTTPUserAgentValue                   = HTTPUserAgent;
            this.DescriptionValue                     = Description;
            this.DisableLoggingValue                  = DisableLogging;
            this.ServerDomainName                     = ChallengeResponse.NormaliseDomainName(BaseUrl.Host);

            this.validatorState.Client                = this;

        }

        #endregion


        #region GetVersionsAsync(CancellationToken = default)

        /// <summary>
        /// GET the base URL: the versions of the API implemented by the server (S2 Connect
        /// 1.0.0, "Selecting the version of REST APIs"); selects the highest common version for
        /// the following requests.
        /// </summary>
        /// <param name="CancellationToken">A token to cancel the request.</param>
        public async Task<ClientOperationResult<IReadOnlyList<String>>> GetVersionsAsync(CancellationToken CancellationToken = default)
        {

            var result = await SendAsync(HTTPMethod.GET,
                                         BaseUrl.URL.Path,
                                         null,
                                         null,
                                         VersionIndexTimeout,
                                         CancellationToken).ConfigureAwait(false);

            if (result.IsTransportFailure)
                return Failure<IReadOnlyList<String>>(result);

            if (result.StatusCode != HTTPStatusCode.OK)
                return Failure<IReadOnlyList<String>>(result, $"unexpected status code {result.StatusCode.Code} for the version index");

            if (!TryReadJSON(result, out var json, out var error) || json is not JArray versionArray)
                return Failure<IReadOnlyList<String>>(result, $"the version index is not a JSON array: {error ?? "wrong JSON type"}");

            var versions = new List<String>();

            foreach (var token in versionArray)
            {

                if (token.Type != JTokenType.String || String.IsNullOrWhiteSpace(token.Value<String>()))
                    return Failure<IReadOnlyList<String>>(result, "the version index must contain version strings only");

                versions.Add(token.Value<String>()!);

            }

            ServerAPIVersions   = versions;
            selectedAPIVersion  = SelectVersion(versions);

            return new ClientOperationResult<IReadOnlyList<String>>(result.StatusCode, true, versions);

        }

        /// <summary>
        /// The request timeout of the version index.
        /// </summary>
        protected abstract TimeSpan VersionIndexTimeout { get; }

        private String? SelectVersion(IEnumerable<String> ServerVersions)

            => ServerVersions.
                   Where  (version => SupportedAPIVersions.Contains(version, StringComparer.Ordinal)).
                   OrderByDescending(version => Int32.TryParse(version.TrimStart('v', 'V'), out var number) ? number : -1).
                   ThenByDescending (version => version, StringComparer.Ordinal).
                   FirstOrDefault();

        #endregion

        #region (protected) EnsureVersionAsync(CancellationToken) / VersionFailure<T>(Failure)

        /// <summary>
        /// Make sure the API version is selected; returns null when it is, otherwise the
        /// failure to report (a transport failure of the version index, an invalid index or
        /// no common version).
        /// </summary>
        protected async Task<ClientOperationResult<IReadOnlyList<String>>?> EnsureVersionAsync(CancellationToken CancellationToken)
        {

            if (selectedAPIVersion is not null)
                return null;

            var versions = await GetVersionsAsync(CancellationToken).ConfigureAwait(false);

            if (!versions.IsSuccess)
                return versions;

            // The status code of the version index (200) is kept: the server answered, it is just incompatible.
            if (selectedAPIVersion is null)
                return new ClientOperationResult<IReadOnlyList<String>>(
                           versions.StatusCode,
                           false,
                           versions.Value,
                           Description:         $"no common API version: the server implements {String.Join(", ", versions.Value ?? [])}, this client {String.Join(", ", SupportedAPIVersions)}",
                           NoCommonAPIVersion:  true
                       );

            return null;

        }

        /// <summary>
        /// Convert a version failure into the result type of an operation.
        /// </summary>
        protected static ClientOperationResult<T> VersionFailure<T>(ClientOperationResult<IReadOnlyList<String>> Failure)
            where T : class
            => new (Failure.StatusCode,
                    false,
                    null,
                    Failure.Error,
                    Failure.Description,
                    Failure.NoCommonAPIVersion,
                    Failure.RetryAfter);

        /// <summary>
        /// Forget the selected version, so that the next request fetches the version index again.
        /// </summary>
        protected void ForgetVersion()
        {
            selectedAPIVersion  = null;
            ServerAPIVersions   = null;
        }

        /// <summary>
        /// Copy the known versions from another client (clones).
        /// </summary>
        protected void CopyVersionsFrom(AS2ConnectClient Other)
        {
            ArgumentNullException.ThrowIfNull(Other);
            ServerAPIVersions   = Other.ServerAPIVersions;
            selectedAPIVersion  = Other.selectedAPIVersion;
        }

        #endregion

        #region (protected) OperationPath(Operation)

        /// <summary>
        /// The path of an operation of the selected API version, e.g. "/pairing/v1/requestPairing".
        /// </summary>
        /// <param name="Operation">The operation name.</param>
        protected HTTPPath OperationPath(String Operation)
            => BaseUrl.URL.Path + (selectedAPIVersion ?? SupportedAPIVersions[0]) + "/" + Operation;

        #endregion

        #region (protected) SendAsync(Method, Path, Body, Bearer, RequestTimeout, CancellationToken)

        /// <summary>
        /// Send one request; transport failures and a changed server certificate come back as
        /// synthetic results, never as exceptions (cancellation excepted).
        /// </summary>
        protected async Task<SendResult> SendAsync(HTTPMethod                 Method,
                                                   HTTPPath                   Path,
                                                   JToken?                    Body,
                                                   HTTPBearerAuthentication?  Bearer,
                                                   TimeSpan                   RequestTimeout,
                                                   CancellationToken          CancellationToken,
                                                   QueryString?               QueryString   = null)
        {

            var fingerprintBefore = observedFingerprint;

            try
            {

                var response = await RunRequest(Method,
                                                Path,
                                                QueryString:        QueryString,
                                                Accept:             jsonAccept,
                                                Authentication:     Bearer,
                                                Content:            Body?.ToString(Formatting.None).ToUTF8Bytes(),
                                                ContentType:        Body is not null ? HTTPContentType.Application.JSON_UTF8 : null,
                                                MaxNumberOfRetries: 1,
                                                RequestTimeout:     RequestTimeout,
                                                CancellationToken:  CancellationToken).ConfigureAwait(false);

                if (fingerprintBefore.HasValue &&
                    observedFingerprint.HasValue &&
                    !fingerprintBefore.Value.Equals(observedFingerprint.Value))
                {
                    Logger?.LogWarning("S2 Connect client: the TLS server certificate of {BaseUrl} changed.", BaseUrl.Value);
                    return SendResult.ChangedCertificate();
                }

                if (response.HTTPStatusCode.Code == 0)
                    return SendResult.TransportFailure(BodyText(response) ?? "the server could not be reached");

                return SendResult.From(response);

            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                Logger?.LogWarning(e, "S2 Connect client: {Method} {Path} failed.", Method, Path);
                return SendResult.TransportFailure(e.Message);
            }

        }

        #endregion

        #region (protected) SendWithDeadlineAsync(Operation, Body, Bearer, RequestTimeout, Deadline, MaxServiceUnavailableRetries, ServiceUnavailableRetryDelay, CancellationToken)

        /// <summary>
        /// Send a POST request of a procedure, repeating it after a 503 and respecting the
        /// deadline of the procedure: a response after the deadline, or a request that was cut
        /// to the remaining time and timed out, is reported as timeout.
        /// </summary>
        protected async Task<SendResult> SendWithDeadlineAsync(String                     Operation,
                                                               JToken?                    Body,
                                                               HTTPBearerAuthentication?  Bearer,
                                                               TimeSpan                   RequestTimeout,
                                                               DateTimeOffset?            Deadline,
                                                               Int32                      MaxServiceUnavailableRetries,
                                                               TimeSpan                   ServiceUnavailableRetryDelay,
                                                               CancellationToken          CancellationToken)
        {

            var attempt = 0;

            while (true)
            {

                var timeout = RequestTimeout;

                if (Deadline.HasValue)
                {

                    var remaining = Deadline.Value - TimeProvider.GetUtcNow();

                    if (remaining <= TimeSpan.Zero)
                        return SendResult.Timeout();

                    if (remaining < timeout)
                        timeout = remaining;

                }

                var started  = TimeProvider.GetUtcNow();
                var result   = await SendAsync(HTTPMethod.POST, OperationPath(Operation), Body, Bearer, timeout, CancellationToken).ConfigureAwait(false);
                var now      = TimeProvider.GetUtcNow();

                if (Deadline.HasValue)
                {

                    if (!result.IsTransportFailure && now >= Deadline.Value)
                    {
                        Logger?.LogInformation("S2 Connect client: the response to {Operation} arrived after the deadline.", Operation);
                        return SendResult.Timeout();
                    }

                    if (result.IsTransportFailure && !result.CertificateChanged && now - started >= timeout - TimeSpan.FromMilliseconds(100))
                    {
                        Logger?.LogInformation("S2 Connect client: {Operation} timed out within the deadline.", Operation);
                        return SendResult.Timeout();
                    }

                }

                if (result.StatusCode != HTTPStatusCode.ServiceUnavailable || attempt >= MaxServiceUnavailableRetries)
                    return result;

                attempt++;

                var delay = ParseRetryAfter(result) ?? ServiceUnavailableRetryDelay;

                if (Deadline.HasValue && TimeProvider.GetUtcNow() + delay >= Deadline.Value)
                    return result;

                Logger?.LogInformation("S2 Connect client: {Operation} answered 503, retry {Attempt} after {Delay}.", Operation, attempt, delay);

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, TimeProvider, CancellationToken).ConfigureAwait(false);

            }

        }

        #endregion

        #region (protected static) ParseRetryAfter / TryReadJSON / BodyText

        /// <summary>
        /// The Retry-After header of a response as delay, when given in seconds.
        /// </summary>
        protected static TimeSpan? ParseRetryAfter(SendResult Result)
        {

            var retryAfter = Result.Response?.RetryAfter;

            if (String.IsNullOrWhiteSpace(retryAfter))
                return null;

            if (UInt32.TryParse(retryAfter.Trim(), out var seconds))
                return TimeSpan.FromSeconds(Math.Min(seconds, 3600));

            return null;

        }

        /// <summary>
        /// Read the body of a response as JSON (dates are kept as strings).
        /// </summary>
        protected static Boolean TryReadJSON(SendResult                       Result,
                                             [NotNullWhen(true)]  out JToken?  JSON,
                                             [NotNullWhen(false)] out String?  Error)
        {

            JSON = null;

            var body = Result.Response?.HTTPBody;

            if (body is null || body.Length == 0)
            {
                Error = "the response body is empty";
                return false;
            }

            try
            {

                using var reader = new JsonTextReader(new StringReader(Encoding.UTF8.GetString(body))) {
                                       DateParseHandling   = DateParseHandling.None,
                                       FloatParseHandling  = FloatParseHandling.Double
                                   };

                JSON = JToken.Load(reader);

                if (reader.Read())
                {
                    JSON   = null;
                    Error  = "additional content after the JSON document";
                    return false;
                }

                Error = null;
                return true;

            }
            catch (Exception e)
            {
                JSON   = null;
                Error  = "invalid JSON: " + e.Message;
                return false;
            }

        }

        /// <summary>
        /// The first 200 characters of the body of a response, for descriptions.
        /// </summary>
        protected static String? BodyText(HTTPResponse Response)
        {

            var body = Response.HTTPBody;

            if (body is null || body.Length == 0)
                return null;

            var text = Encoding.UTF8.GetString(body).Trim();

            return text.Length > 200
                       ? text[..200]
                       : text;

        }

        #endregion

        #region (protected static) Failure<T>(Result, Description = null, Error = null)

        /// <summary>
        /// A failed operation result.
        /// </summary>
        protected static ClientOperationResult<T> Failure<T>(SendResult                    Result,
                                                             String?                       Description   = null,
                                                             PairingResponseErrorMessage?  Error         = null)
            where T : class

            => new (Result.StatusCode,
                    false,
                    null,
                    Error,
                    Description ?? Result.FailureText,
                    RetryAfter: ParseRetryAfter(Result));

        #endregion

        #region (private) ValidateServerCertificate(...)

        /// <summary>
        /// Observe the fingerprint of the TLS server certificate and validate it: a custom
        /// validator wins; otherwise self-signed chains are accepted when allowed (S2 Connect
        /// 1.0.0, "Trusting a self-signed root certificate"), and the operating system decides
        /// in every other case.
        /// </summary>
        private TLSValidationResult ValidateServerCertificate(Object             Sender,
                                                              X509Certificate2?  Certificate,
                                                              X509Chain?         CertificateChain,
                                                              IHTTPClient        Client,
                                                              SslPolicyErrors    PolicyErrors)
        {

            if (Certificate is not null)
                observedFingerprint = CertificateFingerprint.FromCertificate(Certificate);

            if (customCertificateValidator is not null)
                return customCertificateValidator(Sender, Certificate, CertificateChain, Client, PolicyErrors);

            if (Certificate is null)
                return TLSValidationResult.Failed("The server did not present a certificate!");

            if (AcceptSelfSignedCertificates &&
                (PolicyErrors == SslPolicyErrors.None || PolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors))
            {

                var now = TimeProvider.GetUtcNow();

                if (now < Certificate.NotBefore.ToUniversalTime() || now > Certificate.NotAfter.ToUniversalTime())
                    return TLSValidationResult.Failed("The server certificate is not valid at this time!");

                return TLSValidationResult.Success();

            }

            return TLSValidationExtensions.AskTheOS(Sender, Certificate, CertificateChain, Client, PolicyErrors);

        }

        #endregion

    }

}

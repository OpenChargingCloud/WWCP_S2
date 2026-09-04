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

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// The outcome of a pairing attempt from the perspective of the pairing client
    /// (S2 Connect 1.0.0, "Pairing interaction", the client checks and how to proceed).
    /// </summary>
    public enum PairingClientOutcome
    {

        /// <summary>
        /// The pairing was completed and stored.
        /// </summary>
        Success,

        /// <summary>
        /// The version index of the server contains no API version this client implements;
        /// the endpoints are not compatible.
        /// </summary>
        NoCommonAPIVersion,

        /// <summary>
        /// The server rejected a request with 400; the PairingResponseErrorMessage, when given,
        /// tells why (e.g. NodeNotFound, IncompatibleS2MessageVersions, NoValidPairingTokenOnPairingServer).
        /// </summary>
        Rejected,

        /// <summary>
        /// The server did not accept the pairingAttemptId (401); the attempt was dropped and
        /// may be restarted at requestPairing.
        /// </summary>
        Unauthorized,

        /// <summary>
        /// The server did not accept the serverHmacChallengeResponse (403): the pairing tokens
        /// differ; a new attempt needs a valid pairing token.
        /// </summary>
        Forbidden,

        /// <summary>
        /// The server answered 503 repeatedly; try again later.
        /// </summary>
        ServiceUnavailable,

        /// <summary>
        /// A response could not be parsed or violated the schema; when a pairingAttemptId was
        /// available the server was informed via finalizePairing(success = false).
        /// </summary>
        InvalidResponse,

        /// <summary>
        /// The clientHmacChallengeResponse of the server was wrong (step 4): the pairing tokens
        /// differ or the server is not the expected one; the server was informed via
        /// finalizePairing(success = false).
        /// </summary>
        ChallengeResponseMismatch,

        /// <summary>
        /// The node at the server has the same role as the local node; the server was informed
        /// via finalizePairing(success = false).
        /// </summary>
        IncompatibleRole,

        /// <summary>
        /// The local configuration cannot complete the attempt, e.g. the local endpoint has no
        /// session initiation URL or CA certificate fingerprint although it becomes the
        /// communication server, or the server certificate fingerprint is unknown.
        /// </summary>
        InvalidConfiguration,

        /// <summary>
        /// The TLS server certificate changed during the attempt.
        /// </summary>
        CertificateChanged,

        /// <summary>
        /// The server did not respond within the maximum duration of the attempt.
        /// </summary>
        Timeout,

        /// <summary>
        /// The server could not be reached or the connection failed.
        /// </summary>
        TransportFailure,

        /// <summary>
        /// The pairing succeeded on the wire but could not be persisted locally.
        /// </summary>
        StoreFailure,

        /// <summary>
        /// The attempt was cancelled.
        /// </summary>
        Cancelled

    }


    /// <summary>
    /// The result of a pairing attempt of the pairing client.
    /// </summary>
    public sealed class PairingClientResult
    {

        #region Properties

        /// <summary>
        /// The outcome.
        /// </summary>
        public PairingClientOutcome          Outcome           { get; }

        /// <summary>
        /// Whether the pairing succeeded.
        /// </summary>
        public Boolean                       IsSuccess
            => Outcome == PairingClientOutcome.Success;

        /// <summary>
        /// The stored pairing of a successful attempt.
        /// </summary>
        public Pairing?                      Pairing           { get; }

        /// <summary>
        /// The requestPairing response of the server, when step 3 was reached.
        /// </summary>
        public RequestPairingResponse?       ServerResponse    { get; }

        /// <summary>
        /// The error message of a 400 response, when given.
        /// </summary>
        public PairingResponseErrorMessage?  Error             { get; }

        /// <summary>
        /// The HTTP status code of the response that decided the outcome, when any.
        /// </summary>
        public HTTPStatusCode?               StatusCode        { get; }

        /// <summary>
        /// The operation during which the outcome was decided, e.g. "requestPairing".
        /// </summary>
        public String                        Operation         { get; }

        /// <summary>
        /// An optional description of the outcome.
        /// </summary>
        public String?                       Description       { get; }

        /// <summary>
        /// Whether a new attempt may succeed without user interaction (temporary failures).
        /// </summary>
        public Boolean                       Retryable
            => Outcome is PairingClientOutcome.ServiceUnavailable
                       or PairingClientOutcome.Unauthorized
                       or PairingClientOutcome.Timeout
                       or PairingClientOutcome.TransportFailure;

        #endregion

        #region Constructor(s)

        internal PairingClientResult(PairingClientOutcome          Outcome,
                                     String                        Operation,
                                     Pairing?                      Pairing          = null,
                                     RequestPairingResponse?       ServerResponse   = null,
                                     PairingResponseErrorMessage?  Error            = null,
                                     HTTPStatusCode?               StatusCode       = null,
                                     String?                       Description      = null)
        {

            this.Outcome         = Outcome;
            this.Operation       = Operation;
            this.Pairing         = Pairing;
            this.ServerResponse  = ServerResponse;
            this.Error           = Error;
            this.StatusCode      = StatusCode;
            this.Description     = Description ?? Error?.AdditionalInfo;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Outcome} at {Operation}" +
               (StatusCode  is not null ? $" (HTTP {StatusCode.Code})" : "") +
               (Error       is not null ? $": {Error.ErrorMessage}"    : "") +
               (Description is not null ? $" - {Description}"          : "");

        #endregion

    }


    /// <summary>
    /// The result of a single request of the pairing client, e.g. the version index or one of
    /// the LAN-only operations.
    /// </summary>
    /// <typeparam name="T">The type of the response body.</typeparam>
    public sealed class ClientOperationResult<T>

        where T : class

    {

        #region Properties

        /// <summary>
        /// The HTTP status code of the response, or <see cref="HTTPStatusCode.ClientError"/>
        /// (code 0) when the server could not be reached.
        /// </summary>
        public HTTPStatusCode                StatusCode     { get; }

        /// <summary>
        /// The parsed response body of a successful response.
        /// </summary>
        public T?                            Value          { get; }

        /// <summary>
        /// The error message of a 400 response, when given.
        /// </summary>
        public PairingResponseErrorMessage?  Error          { get; }

        /// <summary>
        /// An optional description of a failure (parse errors, transport errors).
        /// </summary>
        public String?                       Description    { get; }

        /// <summary>
        /// Whether the response was a success (2xx) and, when a body was expected, could be parsed.
        /// </summary>
        public Boolean                       IsSuccess      { get; }

        /// <summary>
        /// Whether the server could not be reached at all.
        /// </summary>
        public Boolean                       IsTransportFailure
            => StatusCode.Code == 0 && !NoCommonAPIVersion;

        /// <summary>
        /// Whether the request was not sent because the server implements no API version
        /// this client supports (a final failure, unlike a transport failure).
        /// </summary>
        public Boolean                       NoCommonAPIVersion    { get; }

        /// <summary>
        /// The Retry-After hint of a 503 response, when given.
        /// </summary>
        public TimeSpan?                     RetryAfter            { get; }

        #endregion

        #region Constructor(s)

        internal ClientOperationResult(HTTPStatusCode                StatusCode,
                                       Boolean                       IsSuccess,
                                       T?                            Value                = null,
                                       PairingResponseErrorMessage?  Error                = null,
                                       String?                       Description          = null,
                                       Boolean                       NoCommonAPIVersion   = false,
                                       TimeSpan?                     RetryAfter           = null)
        {

            ArgumentNullException.ThrowIfNull(StatusCode);

            this.StatusCode          = StatusCode;
            this.IsSuccess           = IsSuccess;
            this.Value               = Value;
            this.Error               = Error;
            this.Description         = Description ?? Error?.AdditionalInfo;
            this.NoCommonAPIVersion  = NoCommonAPIVersion;
            this.RetryAfter          = RetryAfter;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"HTTP {StatusCode.Code}" +
               (IsSuccess               ? " (success)"                : "") +
               (Error       is not null ? $": {Error.ErrorMessage}"   : "") +
               (Description is not null ? $" - {Description}"         : "");

        #endregion

    }

}

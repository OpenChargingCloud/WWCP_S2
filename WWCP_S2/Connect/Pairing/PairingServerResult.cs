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
    /// The outcome of a pairing server operation, independent of the transport: the HTTP status
    /// code the specification prescribes, an optional <see cref="PairingResponseErrorMessage"/>
    /// for 400 responses and an optional Retry-After hint for 503 responses (PLAN.md D15).
    /// </summary>
    public class PairingServerResult
    {

        #region Properties

        /// <summary>
        /// The HTTP status code of the outcome.
        /// </summary>
        public HTTPStatusCode                StatusCode    { get; }

        /// <summary>
        /// The optional error message of a 400 response.
        /// </summary>
        public PairingResponseErrorMessage?  Error         { get; }

        /// <summary>
        /// The optional description of a non-2xx outcome for logs (never sent to the client
        /// unless it is part of <see cref="Error"/>).
        /// </summary>
        public String?                       Description   { get; }

        /// <summary>
        /// The optional delay after which the client may retry (503 responses).
        /// </summary>
        public TimeSpan?                     RetryAfter    { get; }

        /// <summary>
        /// Whether the outcome is a success (2xx).
        /// </summary>
        public Boolean                       IsSuccess
            => StatusCode.Code >= 200 && StatusCode.Code < 300;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new pairing server result.
        /// </summary>
        /// <param name="StatusCode">The HTTP status code of the outcome.</param>
        /// <param name="Error">The optional error message of a 400 response.</param>
        /// <param name="Description">An optional description for logs.</param>
        /// <param name="RetryAfter">An optional delay after which the client may retry.</param>
        protected PairingServerResult(HTTPStatusCode                StatusCode,
                                      PairingResponseErrorMessage?  Error         = null,
                                      String?                       Description   = null,
                                      TimeSpan?                     RetryAfter    = null)
        {

            ArgumentNullException.ThrowIfNull(StatusCode);

            this.StatusCode   = StatusCode;
            this.Error        = Error;
            this.Description  = Description ?? Error?.AdditionalInfo;
            this.RetryAfter   = RetryAfter;

        }

        #endregion


        #region (static) NoContent()

        /// <summary>
        /// 204 No Content.
        /// </summary>
        public static PairingServerResult NoContent()
            => new (HTTPStatusCode.NoContent);

        #endregion

        #region (static) BadRequest(Error, AdditionalInfo = null)

        /// <summary>
        /// 400 Bad Request with a PairingResponseErrorMessage.
        /// </summary>
        /// <param name="Error">The error.</param>
        /// <param name="AdditionalInfo">Optional additional information for the client.</param>
        public static PairingServerResult BadRequest(PairingResponseError  Error,
                                                     String?               AdditionalInfo   = null)
            => new (HTTPStatusCode.BadRequest,
                    new PairingResponseErrorMessage(Error, AdditionalInfo));

        #endregion

        #region (static) Unauthorized(Description = null)

        /// <summary>
        /// 401 Unauthorized: the pairingAttemptId is missing, unknown, expired or used up.
        /// </summary>
        /// <param name="Description">An optional description for logs.</param>
        public static PairingServerResult Unauthorized(String? Description = null)
            => new (HTTPStatusCode.Unauthorized, Description: Description);

        #endregion

        #region (static) Forbidden(Description = null)

        /// <summary>
        /// 403 Forbidden: the serverHmacChallengeResponse was not accepted.
        /// </summary>
        /// <param name="Description">An optional description for logs.</param>
        public static PairingServerResult Forbidden(String? Description = null)
            => new (HTTPStatusCode.Forbidden, Description: Description);

        #endregion

        #region (static) NotFound(Description = null)

        /// <summary>
        /// 404 Not Found: the operation is not implemented by this endpoint.
        /// </summary>
        /// <param name="Description">An optional description for logs.</param>
        public static PairingServerResult NotFound(String? Description = null)
            => new (HTTPStatusCode.NotFound, Description: Description);

        #endregion

        #region (static) ServiceUnavailable(RetryAfter = null, Description = null)

        /// <summary>
        /// 503 Service Unavailable: try again soon.
        /// </summary>
        /// <param name="RetryAfter">An optional delay after which the client may retry.</param>
        /// <param name="Description">An optional description for logs.</param>
        public static PairingServerResult ServiceUnavailable(TimeSpan?  RetryAfter    = null,
                                                             String?    Description   = null)
            => new (HTTPStatusCode.ServiceUnavailable, Description: Description, RetryAfter: RetryAfter);

        #endregion

        #region (static) OK<T>(Value)

        /// <summary>
        /// 200 OK with the given response body.
        /// </summary>
        /// <typeparam name="T">The type of the response body.</typeparam>
        /// <param name="Value">The response body.</param>
        public static PairingServerResult<T> OK<T>(T Value)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(Value);
            return new (HTTPStatusCode.OK, Value);
        }

        #endregion

        #region (static) Wrap<T>(Result)

        /// <summary>
        /// Wrap a result without body into a typed result.
        /// </summary>
        /// <typeparam name="T">The type of the response body.</typeparam>
        /// <param name="Result">A pairing server result.</param>
        public static PairingServerResult<T> Wrap<T>(PairingServerResult Result)
            where T : class
        {

            ArgumentNullException.ThrowIfNull(Result);

            return new (Result.StatusCode,
                        null,
                        Result.Error,
                        Result.Description,
                        Result.RetryAfter);

        }

        #endregion

        #region (static) InternalServerError(Description = null)

        /// <summary>
        /// 500 Internal Server Error, e.g. when the store failed.
        /// </summary>
        /// <param name="Description">An optional description for logs.</param>
        public static PairingServerResult InternalServerError(String? Description = null)
            => new (HTTPStatusCode.InternalServerError, Description: Description);

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => Error is not null
                   ? $"{StatusCode.Code} {Error.ErrorMessage}{(Error.AdditionalInfo is not null ? $" ({Error.AdditionalInfo})" : "")}"
                   : $"{StatusCode.Code}{(Description is not null ? $" ({Description})" : "")}";

        #endregion

    }


    /// <summary>
    /// The outcome of a pairing server operation carrying a response body on success.
    /// </summary>
    /// <typeparam name="T">The type of the response body.</typeparam>
    public sealed class PairingServerResult<T> : PairingServerResult

        where T : class

    {

        #region Properties

        /// <summary>
        /// The response body of a successful outcome.
        /// </summary>
        public T?  Value    { get; }

        #endregion

        #region Constructor(s)

        internal PairingServerResult(HTTPStatusCode                StatusCode,
                                     T?                            Value,
                                    PairingResponseErrorMessage?  Error         = null,
                                    String?                       Description   = null,
                                    TimeSpan?                     RetryAfter    = null)

            : base(StatusCode,
                   Error,
                   Description,
                   RetryAfter)

        {
            this.Value = Value;
        }

        #endregion

    }

}

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
    /// The outcome of a session initiation or unpairing from the perspective of the
    /// communication client (S2 Connect 1.0.0, "Session initiation", the client reactions
    /// "retry later", "do not retry, inform end user", "try with other accessToken").
    /// </summary>
    public enum SessionInitiationOutcome
    {

        /// <summary>
        /// The session was initiated (the communication details were received and the new
        /// access token activated), or the unpairing succeeded.
        /// </summary>
        Success,

        /// <summary>
        /// The nodes were already unpaired at the server (unpairing answered 401); the local
        /// security material was removed.
        /// </summary>
        AlreadyUnpaired,

        /// <summary>
        /// The server answered NoLongerPaired: the pairing was removed at the server; do not
        /// retry, inform the end user.
        /// </summary>
        NoLongerPaired,

        /// <summary>
        /// No candidate access token was accepted (401); do not retry, inform the end user.
        /// </summary>
        Unauthorized,

        /// <summary>
        /// A temporary condition: a 400 (incompatible versions or protocols, parsing error,
        /// other), a 500, a 503 after the retries, an unparseable response or an unconfirmed
        /// token; retry later starting at step 1.
        /// </summary>
        RetryLater,

        /// <summary>
        /// The server could not be reached or the connection failed; retry later.
        /// </summary>
        TransportFailure,

        /// <summary>
        /// The TLS server certificate changed during the procedure.
        /// </summary>
        CertificateChanged,

        /// <summary>
        /// The local store could not persist the pending or the activated token; the procedure
        /// was aborted before confirmAccessToken (S2 Connect 1.0.0, "4. Store pending accessToken").
        /// </summary>
        StoreFailure,

        /// <summary>
        /// The server implements no API version this client supports.
        /// </summary>
        NoCommonAPIVersion,

        /// <summary>
        /// The local configuration cannot run the procedure, e.g. the nodes are not paired locally.
        /// </summary>
        InvalidConfiguration,

        /// <summary>
        /// The procedure was cancelled.
        /// </summary>
        Cancelled

    }


    /// <summary>
    /// The result of a session initiation or unpairing of the session initiation client.
    /// </summary>
    public sealed class SessionInitiationClientResult
    {

        #region Properties

        /// <summary>
        /// The outcome.
        /// </summary>
        public SessionInitiationOutcome           Outcome                          { get; }

        /// <summary>
        /// Whether the procedure succeeded.
        /// </summary>
        public Boolean                            IsSuccess
            => Outcome == SessionInitiationOutcome.Success;

        /// <summary>
        /// The operation during which the outcome was decided, e.g. "initiateSession".
        /// </summary>
        public String                             Operation                        { get; }

        /// <summary>
        /// The HTTP status code of the response that decided the outcome, when any.
        /// </summary>
        public HTTPStatusCode?                    StatusCode                       { get; }

        /// <summary>
        /// The error message of a 400 response, when given.
        /// </summary>
        public CommunicationDetailsErrorMessage?  Error                            { get; }

        /// <summary>
        /// An optional description of the outcome.
        /// </summary>
        public String?                            Description                      { get; }

        /// <summary>
        /// The (updated) pairing after a successful session initiation.
        /// </summary>
        public Pairing?                           Pairing                          { get; }

        /// <summary>
        /// The communication details received in step 7.
        /// </summary>
        public CommunicationDetails?              CommunicationDetails             { get; }

        /// <summary>
        /// The communication protocol selected by the server.
        /// </summary>
        public CommunicationProtocol?             SelectedCommunicationProtocol    { get; }

        /// <summary>
        /// The S2 message version selected by the server.
        /// </summary>
        public String?                            SelectedS2MessageVersion         { get; }

        /// <summary>
        /// Whether a new attempt may succeed without user interaction.
        /// </summary>
        public Boolean                            Retryable
            => Outcome is SessionInitiationOutcome.RetryLater
                       or SessionInitiationOutcome.TransportFailure
                       or SessionInitiationOutcome.StoreFailure;

        #endregion

        #region Constructor(s)

        internal SessionInitiationClientResult(SessionInitiationOutcome           Outcome,
                                               String                             Operation,
                                               HTTPStatusCode?                    StatusCode                      = null,
                                               CommunicationDetailsErrorMessage?  Error                           = null,
                                               String?                            Description                     = null,
                                               Pairing?                           Pairing                         = null,
                                               CommunicationDetails?              CommunicationDetails            = null,
                                               CommunicationProtocol?             SelectedCommunicationProtocol   = null,
                                               String?                            SelectedS2MessageVersion        = null)
        {

            this.Outcome                        = Outcome;
            this.Operation                      = Operation;
            this.StatusCode                     = StatusCode;
            this.Error                          = Error;
            this.Description                    = Description ?? Error?.AdditionalInfo;
            this.Pairing                        = Pairing;
            this.CommunicationDetails           = CommunicationDetails;
            this.SelectedCommunicationProtocol  = SelectedCommunicationProtocol;
            this.SelectedS2MessageVersion       = SelectedS2MessageVersion;

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

}

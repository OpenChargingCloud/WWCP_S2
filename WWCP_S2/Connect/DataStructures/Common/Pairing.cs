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

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// A persisted pairing relation between a local node and a remote node: the security
    /// material agreed on during the pairing process (the access token, for communication
    /// clients also the session initiation URL and the pinned certificate fingerprints of the
    /// communication server) and the descriptions of the remote node and endpoint
    /// (S2 Connect 1.0.0, "Pairing process" and "Session initiation"). Immutable; token
    /// rotation during session initiation creates a new instance via <see cref="WithAccessToken"/>.
    /// </summary>
    public sealed class Pairing : IEquatable<Pairing>
    {

        #region Properties

        /// <summary>
        /// The identification of the local node.
        /// </summary>
        public Node_Id                                                LocalNodeId                  { get; }

        /// <summary>
        /// The identification of the remote node.
        /// </summary>
        public Node_Id                                                RemoteNodeId
            => RemoteNodeDescription.Id;

        /// <summary>
        /// The description of the remote node as received during pairing (or updated later).
        /// </summary>
        public NodeDescription                                        RemoteNodeDescription        { get; }

        /// <summary>
        /// The description of the remote endpoint as received during pairing (or updated later).
        /// </summary>
        public EndpointDescription                                    RemoteEndpointDescription    { get; }

        /// <summary>
        /// The communication role of the local node in this pairing.
        /// </summary>
        public CommunicationRole                                      LocalCommunicationRole       { get; }

        /// <summary>
        /// The access token of this pairing: the token the remote node must present when the
        /// local node is the communication server, or the token the local node presents when
        /// it is the communication client (S2 Connect 1.0.0, "8A. Response status 200").
        /// </summary>
        public AccessToken                                            AccessToken                  { get; }

        /// <summary>
        /// The base URL of the session initiation API of the communication server; only
        /// present when the local node is the communication client.
        /// </summary>
        public S2BaseURL?                                             InitiateSessionUrl           { get; }

        /// <summary>
        /// The pinned fingerprints of the CA certificate of the communication server, keyed by
        /// hashing algorithm (S2 Connect 1.0.0, "6B. POST /[version]/postConnectionDetails");
        /// only present when the local node is the communication client of a LAN peer.
        /// </summary>
        public IReadOnlyDictionary<String, CertificateFingerprint>?  CertificateFingerprints      { get; }

        /// <summary>
        /// The timestamp of the successful completion of the pairing.
        /// </summary>
        public DateTimeOffset                                         PairedAt                     { get; }

        /// <summary>
        /// Whether the local node is the communication server of this pairing.
        /// </summary>
        public Boolean                                                IsCommunicationServer
            => LocalCommunicationRole == CommunicationRole.CommunicationServer;

        /// <summary>
        /// Whether the local node is the communication client of this pairing.
        /// </summary>
        public Boolean                                                IsCommunicationClient
            => LocalCommunicationRole == CommunicationRole.CommunicationClient;

        /// <summary>
        /// The energy management role of the remote node.
        /// </summary>
        public EnergyManagementRole                                   RemoteRole
            => RemoteNodeDescription.Role;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new pairing.
        /// </summary>
        /// <param name="LocalNodeId">The identification of the local node.</param>
        /// <param name="RemoteNodeDescription">The description of the remote node.</param>
        /// <param name="RemoteEndpointDescription">The description of the remote endpoint.</param>
        /// <param name="LocalCommunicationRole">The communication role of the local node.</param>
        /// <param name="AccessToken">The access token of the pairing.</param>
        /// <param name="PairedAt">The timestamp of the successful completion of the pairing.</param>
        /// <param name="InitiateSessionUrl">The session initiation URL of the communication server (required for communication clients).</param>
        /// <param name="CertificateFingerprints">Optional pinned fingerprints of the CA certificate of the communication server.</param>
        public Pairing(Node_Id                                               LocalNodeId,
                       NodeDescription                                       RemoteNodeDescription,
                       EndpointDescription                                   RemoteEndpointDescription,
                       CommunicationRole                                     LocalCommunicationRole,
                       AccessToken                                           AccessToken,
                       DateTimeOffset                                        PairedAt,
                       S2BaseURL?                                            InitiateSessionUrl        = null,
                       IReadOnlyDictionary<String, CertificateFingerprint>?  CertificateFingerprints   = null)
        {

            ArgumentNullException.ThrowIfNull(RemoteNodeDescription);
            ArgumentNullException.ThrowIfNull(RemoteEndpointDescription);

            if (LocalNodeId.IsNullOrEmpty)
                throw new ArgumentException("The local node identification must not be empty!", nameof(LocalNodeId));

            if (LocalNodeId == RemoteNodeDescription.Id)
                throw new ArgumentException("A node cannot be paired with itself!", nameof(RemoteNodeDescription));

            if (AccessToken.Length == 0)
                throw new ArgumentException("The access token must not be empty!", nameof(AccessToken));

            if (LocalCommunicationRole == CommunicationRole.CommunicationClient && InitiateSessionUrl is null)
                throw new ArgumentException("A communication client needs the session initiation URL of its communication server!", nameof(InitiateSessionUrl));

            if (LocalCommunicationRole != CommunicationRole.CommunicationServer && LocalCommunicationRole != CommunicationRole.CommunicationClient)
                throw new ArgumentOutOfRangeException(nameof(LocalCommunicationRole), LocalCommunicationRole, "Unknown communication role!");

            this.LocalNodeId                = LocalNodeId;
            this.RemoteNodeDescription      = RemoteNodeDescription;
            this.RemoteEndpointDescription  = RemoteEndpointDescription;
            this.LocalCommunicationRole     = LocalCommunicationRole;
            this.AccessToken                = AccessToken;
            this.PairedAt                   = PairedAt;
            this.InitiateSessionUrl         = InitiateSessionUrl;

            if (CertificateFingerprints is not null)
            {

                var fingerprints = new Dictionary<String, CertificateFingerprint>(CertificateFingerprints, StringComparer.Ordinal);

                if (fingerprints.Count == 0)
                    throw new ArgumentException("The certificate fingerprints must not be empty when given!", nameof(CertificateFingerprints));

                this.CertificateFingerprints = fingerprints;

            }

            unchecked
            {
                hashCode = this.LocalNodeId.              GetHashCode() * 13 ^
                           this.RemoteNodeDescription.    GetHashCode() * 11 ^
                           this.RemoteEndpointDescription.GetHashCode() *  7 ^
                           this.LocalCommunicationRole.   GetHashCode() *  5 ^
                           this.AccessToken.              GetHashCode() *  3 ^
                           this.PairedAt.                 GetHashCode();
            }

        }

        #endregion


        #region Documentation

        // Persistence format (PLAN.md §3.5, JSON file store, format version 1), not an S2 Connect wire format:
        //
        //   {
        //     "localNodeId":               "8a4d5c2e-…",
        //     "remoteNodeDescription":     { …NodeDescription… },
        //     "remoteEndpointDescription": { …EndpointDescription… },
        //     "localCommunicationRole":    "CommunicationServer" | "CommunicationClient",
        //     "accessToken":               "<Base64>",
        //     "initiateSessionUrl":        "https://hostname.local/connection/",   (communication clients only)
        //     "certificateFingerprint":    { "SHA256": "AB:CD:…" },               (optional)
        //     "pairedAt":                  "2026-09-04T10:00:00Z"
        //   }

        #endregion

        #region (static) TryParse(JSON, out Pairing, out ErrorResponse, Options = null)

        /// <summary>
        /// Try to parse the given JSON representation of a pairing.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Pairing">The parsed pairing.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Options">Optional parser options.</param>
        public static Boolean TryParse(JObject                            JSON,
                                       [NotNullWhen(true)]  out Pairing?  Pairing,
                                       [NotNullWhen(false)] out String?   ErrorResponse,
                                       S2ParserOptions?                   Options   = null)
        {

            try
            {

                Pairing = null;

                #region localNodeId                  [mandatory]

                if (!JSON.ParseMandatoryS2Id("localNodeId",
                                             "local node identification",
                                             Node_Id.TryParse,
                                             Options,
                                             out Node_Id localNodeId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region remoteNodeDescription        [mandatory]

                if (!JSON.ParseMandatoryS2("remoteNodeDescription",
                                           "remote node description",
                                           NodeDescription.TryParse,
                                           Options,
                                           out NodeDescription? remoteNodeDescription,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region remoteEndpointDescription    [mandatory]

                if (!JSON.ParseMandatoryS2("remoteEndpointDescription",
                                           "remote endpoint description",
                                           EndpointDescription.TryParse,
                                           Options,
                                           out EndpointDescription? remoteEndpointDescription,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region localCommunicationRole       [mandatory]

                if (!JSON.ParseMandatoryS2String("localCommunicationRole",
                                                 "local communication role",
                                                 out String? localCommunicationRoleText,
                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!CommunicationRoleExtensions.TryParse(localCommunicationRoleText,
                                                          out CommunicationRole localCommunicationRole))
                {
                    ErrorResponse = "Invalid local communication role 'localCommunicationRole': 'CommunicationServer' or 'CommunicationClient' is expected!";
                    return false;
                }

                #endregion

                #region accessToken                  [mandatory]

                if (!JSON.ParseMandatoryS2String("accessToken",
                                                 "access token",
                                                 out String? accessTokenText,
                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!AccessToken.TryParse(accessTokenText,
                                          out AccessToken accessToken))
                {
                    ErrorResponse = "Invalid access token 'accessToken': a standard Base64 text is expected!";
                    return false;
                }

                #endregion

                #region initiateSessionUrl           [optional]

                if (!JSON.ParseOptionalS2String("initiateSessionUrl",
                                                "initiate session URL",
                                                out String? initiateSessionUrlText,
                                                out ErrorResponse))
                {
                    return false;
                }

                S2BaseURL? initiateSessionUrl = null;

                if (initiateSessionUrlText is not null)
                {

                    if (!S2BaseURL.TryParse(initiateSessionUrlText,
                                            out S2BaseURL parsedInitiateSessionUrl,
                                            out var urlError,
                                            Options?.AllowInsecureURLs ?? false))
                    {
                        ErrorResponse = $"Invalid initiate session URL 'initiateSessionUrl': {urlError}";
                        return false;
                    }

                    initiateSessionUrl = parsedInitiateSessionUrl;

                }

                #endregion

                #region certificateFingerprint       [optional]

                if (!ConnectionDetails.TryParseCertificateFingerprints(JSON,
                                                                       out var certificateFingerprints,
                                                                       out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region pairedAt                     [mandatory]

                if (!JSON.ParseMandatoryS2Timestamp("pairedAt",
                                                    "pairing timestamp",
                                                    Options,
                                                    out DateTimeOffset pairedAt,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region additional properties

                if (!JSON.CheckAdditionalProperties(Options,
                                                    out ErrorResponse,
                                                    "localNodeId",
                                                    "remoteNodeDescription",
                                                    "remoteEndpointDescription",
                                                    "localCommunicationRole",
                                                    "accessToken",
                                                    "initiateSessionUrl",
                                                    "certificateFingerprint",
                                                    "pairedAt"))
                {
                    return false;
                }

                #endregion


                Pairing = new Pairing(
                              localNodeId,
                              remoteNodeDescription,
                              remoteEndpointDescription,
                              localCommunicationRole,
                              accessToken,
                              pairedAt,
                              initiateSessionUrl,
                              certificateFingerprints
                          );

                return true;

            }
            catch (Exception e)
            {
                Pairing        = null;
                ErrorResponse  = "The given JSON representation of a pairing is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this pairing (persistence format, contains the access token!).
        /// </summary>
        public JObject ToJSON()

            => JSONObject.Create(

                         new JProperty("localNodeId",                LocalNodeId.              ToString()),
                         new JProperty("remoteNodeDescription",      RemoteNodeDescription.    ToJSON()),
                         new JProperty("remoteEndpointDescription",  RemoteEndpointDescription.ToJSON()),
                         new JProperty("localCommunicationRole",     LocalCommunicationRole.   AsText()),
                         new JProperty("accessToken",                AccessToken.              Value),

                   InitiateSessionUrl.HasValue
                       ? new JProperty("initiateSessionUrl",         InitiateSessionUrl.Value. Value)
                       : null,

                   CertificateFingerprints is not null
                       ? new JProperty("certificateFingerprint",     new JObject(CertificateFingerprints.Select(kv => new JProperty(kv.Key, kv.Value.ToHexString()))))
                       : null,

                         new JProperty("pairedAt",                   PairedAt.                 ToS2Timestamp())

               );

        #endregion

        #region WithAccessToken(AccessToken)

        /// <summary>
        /// Return a copy of this pairing with another access token (token rotation during
        /// session initiation).
        /// </summary>
        /// <param name="AccessToken">The new access token.</param>
        public Pairing WithAccessToken(AccessToken AccessToken)

            => new (LocalNodeId,
                    RemoteNodeDescription,
                    RemoteEndpointDescription,
                    LocalCommunicationRole,
                    AccessToken,
                    PairedAt,
                    InitiateSessionUrl,
                    CertificateFingerprints);

        #endregion

        #region WithDescriptions(RemoteNodeDescription, RemoteEndpointDescription)

        /// <summary>
        /// Return a copy of this pairing with updated remote descriptions (session initiation
        /// may carry updated node and endpoint descriptions).
        /// </summary>
        /// <param name="RemoteNodeDescription">The updated description of the remote node (same identification).</param>
        /// <param name="RemoteEndpointDescription">The updated description of the remote endpoint.</param>
        public Pairing WithDescriptions(NodeDescription      RemoteNodeDescription,
                                        EndpointDescription  RemoteEndpointDescription)
        {

            ArgumentNullException.ThrowIfNull(RemoteNodeDescription);
            ArgumentNullException.ThrowIfNull(RemoteEndpointDescription);

            if (RemoteNodeDescription.Id != RemoteNodeId)
                throw new ArgumentException("The identification of the remote node must not change!", nameof(RemoteNodeDescription));

            return new (LocalNodeId,
                        RemoteNodeDescription,
                        RemoteEndpointDescription,
                        LocalCommunicationRole,
                        AccessToken,
                        PairedAt,
                        InitiateSessionUrl,
                        CertificateFingerprints);

        }

        #endregion


        #region Operator overloading

        #region Operator == (Pairing1, Pairing2)

        /// <summary>
        /// Compares two pairings for equality.
        /// </summary>
        /// <param name="Pairing1">A pairing.</param>
        /// <param name="Pairing2">Another pairing.</param>
        public static Boolean operator == (Pairing? Pairing1,
                                           Pairing? Pairing2)
        {

            if (ReferenceEquals(Pairing1, Pairing2))
                return true;

            if (Pairing1 is null || Pairing2 is null)
                return false;

            return Pairing1.Equals(Pairing2);

        }

        #endregion

        #region Operator != (Pairing1, Pairing2)

        /// <summary>
        /// Compares two pairings for inequality.
        /// </summary>
        /// <param name="Pairing1">A pairing.</param>
        /// <param name="Pairing2">Another pairing.</param>
        public static Boolean operator != (Pairing? Pairing1,
                                           Pairing? Pairing2)

            => !(Pairing1 == Pairing2);

        #endregion

        #endregion

        #region IEquatable<Pairing> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two pairings for equality.
        /// </summary>
        /// <param name="Object">A pairing to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is Pairing pairing &&
                   Equals(pairing);

        #endregion

        #region Equals(Pairing)

        /// <summary>
        /// Compares two pairings for equality.
        /// </summary>
        /// <param name="Pairing">A pairing to compare with.</param>
        public Boolean Equals(Pairing? Pairing)

            => Pairing is not null &&

               LocalNodeId.              Equals(Pairing.LocalNodeId)               &&
               RemoteNodeDescription.    Equals(Pairing.RemoteNodeDescription)     &&
               RemoteEndpointDescription.Equals(Pairing.RemoteEndpointDescription) &&
               LocalCommunicationRole.   Equals(Pairing.LocalCommunicationRole)    &&
               AccessToken.              Equals(Pairing.AccessToken)               &&
               PairedAt.                 Equals(Pairing.PairedAt)                  &&

             ((!InitiateSessionUrl.HasValue && !Pairing.InitiateSessionUrl.HasValue) ||
               (InitiateSessionUrl.HasValue &&  Pairing.InitiateSessionUrl.HasValue && InitiateSessionUrl.Value.Equals(Pairing.InitiateSessionUrl.Value))) &&

             ((CertificateFingerprints is null && Pairing.CertificateFingerprints is null) ||
              (CertificateFingerprints is not null && Pairing.CertificateFingerprints is not null &&
               CertificateFingerprints.Count == Pairing.CertificateFingerprints.Count &&
               CertificateFingerprints.All(kv => Pairing.CertificateFingerprints.TryGetValue(kv.Key, out var other) && other.Equals(kv.Value))));

        #endregion

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
        /// Return a text representation of this object (without the access token).
        /// </summary>
        public override String ToString()

            => $"{LocalNodeId} <-> {RemoteNodeId} ({RemoteRole}, local: {LocalCommunicationRole}, paired {PairedAt.ToS2Timestamp()})";

        #endregion

    }

}

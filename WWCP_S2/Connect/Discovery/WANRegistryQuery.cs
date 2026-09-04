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

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// A query of the S2 Connect WAN endpoint registry (s2-connect-wan-endpoint-registry.yml,
    /// GET /v1/endpoint): optional country codes (endpoints active in at least one of them),
    /// status (default: public), CEM/RM flags and paging.
    /// </summary>
    public sealed class WANRegistryQuery : IEquatable<WANRegistryQuery>
    {

        #region Properties

        /// <summary>
        /// The optional country codes; endpoints active in at least one of them match.
        /// </summary>
        public IReadOnlyList<CountryCode>  Regions    { get; }

        /// <summary>
        /// The optional status to filter on (the registry defaults to "public").
        /// </summary>
        public EndpointStatus?             Status     { get; }

        /// <summary>
        /// The optional filter on endpoints representing CEMs.
        /// </summary>
        public Boolean?                    CEM        { get; }

        /// <summary>
        /// The optional filter on endpoints representing RMs.
        /// </summary>
        public Boolean?                    RM         { get; }

        /// <summary>
        /// The optional maximum number of results (at least 1).
        /// </summary>
        public Int32?                      Limit      { get; }

        /// <summary>
        /// The optional offset of the results (at least 0).
        /// </summary>
        public Int32?                      Offset     { get; }

        /// <summary>
        /// The query matching every public endpoint.
        /// </summary>
        public static WANRegistryQuery     All        { get; } = new ();

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new registry query.
        /// </summary>
        /// <param name="Regions">Optional country codes; endpoints active in at least one of them match.</param>
        /// <param name="Status">An optional status to filter on (the registry defaults to "public").</param>
        /// <param name="CEM">An optional filter on endpoints representing CEMs.</param>
        /// <param name="RM">An optional filter on endpoints representing RMs.</param>
        /// <param name="Limit">An optional maximum number of results (at least 1).</param>
        /// <param name="Offset">An optional offset of the results (at least 0).</param>
        public WANRegistryQuery(IEnumerable<CountryCode>?  Regions   = null,
                                EndpointStatus?            Status    = null,
                                Boolean?                   CEM       = null,
                                Boolean?                   RM        = null,
                                Int32?                     Limit     = null,
                                Int32?                     Offset    = null)
        {

            if (Limit.HasValue && Limit.Value < 1)
                throw new ArgumentException("The limit must be at least 1!", nameof(Limit));

            if (Offset.HasValue && Offset.Value < 0)
                throw new ArgumentException("The offset must not be negative!", nameof(Offset));

            if (Status.HasValue && Status.Value.IsNullOrEmpty)
                throw new ArgumentException("The status must not be empty!", nameof(Status));

            this.Regions  = [.. (Regions ?? []).Distinct()];
            this.Status   = Status;
            this.CEM      = CEM;
            this.RM       = RM;
            this.Limit    = Limit;
            this.Offset   = Offset;

        }

        #endregion

        #region Documentation

        // s2-connect-wan-endpoint-registry.yml, GET /endpoint, query parameters:
        //   region: array of CountryCode, style form, explode false ("endpoint?region=NL,BE,LU"); results include
        //           endpoints active in at least one of the codes.
        //   status: Status (enum "public"|"testing"); when no value provided the default is "public".
        //   cem:    boolean; filter on endpoints that represent CEMs; no filtering when absent.
        //   rm:     boolean; filter on endpoints that represent RMs; no filtering when absent.
        //   limit:  integer, minimum 1.
        //   offset: integer, minimum 0.
        // Responses: 200 array of EndpointRecord; 400 invalid query parameters.

        #endregion


        #region Matches(Record)

        /// <summary>
        /// Whether the given endpoint record matches this query (regions, status and flags; paging is not applied).
        /// </summary>
        /// <param name="Record">An endpoint record.</param>
        public Boolean Matches(EndpointRecord Record)
        {

            ArgumentNullException.ThrowIfNull(Record);

            if (Regions.Count > 0 && !Record.Regions.Any(Regions.Contains))
                return false;

            var status = Status ?? EndpointStatus.Public;

            if (Record.Status != status)
                return false;

            if (CEM.HasValue && Record.CEM != CEM.Value)
                return false;

            if (RM.HasValue && Record.RM != RM.Value)
                return false;

            return true;

        }

        #endregion

        #region Apply(Records)

        /// <summary>
        /// Filter and page the given endpoint records.
        /// </summary>
        /// <param name="Records">Endpoint records.</param>
        public IReadOnlyList<EndpointRecord> Apply(IEnumerable<EndpointRecord> Records)
        {

            ArgumentNullException.ThrowIfNull(Records);

            IEnumerable<EndpointRecord> matching = Records.Where(Matches);

            if (Offset.HasValue)
                matching = matching.Skip(Offset.Value);

            if (Limit.HasValue)
                matching = matching.Take(Limit.Value);

            return [.. matching];

        }

        #endregion

        #region ToQueryString()

        /// <summary>
        /// The HTTP query string of this query (empty when nothing is filtered).
        /// </summary>
        public QueryString ToQueryString()
        {

            var queryString = QueryString.Empty;

            if (Regions.Count > 0)
                queryString = queryString.Add("region", String.Join(',', Regions.Select(region => region.ToString())));

            if (Status.HasValue)
                queryString = queryString.Add("status", Status.Value.ToString());

            if (CEM.HasValue)
                queryString = queryString.Add("cem", CEM.Value ? "true" : "false");

            if (RM.HasValue)
                queryString = queryString.Add("rm", RM.Value ? "true" : "false");

            if (Limit.HasValue)
                queryString = queryString.Add("limit", Limit.Value.ToString());

            if (Offset.HasValue)
                queryString = queryString.Add("offset", Offset.Value.ToString());

            return queryString;

        }

        #endregion

        #region (static) TryParse(QueryString, out Query, out ErrorResponse)

        /// <summary>
        /// Try to parse the given HTTP query string as registry query (the server side of GET /v1/endpoint).
        /// </summary>
        /// <param name="QueryString">An HTTP query string.</param>
        /// <param name="Query">The parsed query.</param>
        /// <param name="ErrorResponse">An error message when a parameter is invalid.</param>
        public static Boolean TryParse(QueryString                                QueryString,
                                       [NotNullWhen(true)]  out WANRegistryQuery?  Query,
                                       [NotNullWhen(false)] out String?            ErrorResponse)
        {

            Query          = null;
            ErrorResponse  = null;

            ArgumentNullException.ThrowIfNull(QueryString);

            #region region

            var regions = new List<CountryCode>();

            foreach (var regionText in QueryString.GetStrings("region").
                                                   SelectMany(text => text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            {

                if (!CountryCode.TryParse(regionText, out var region))
                {
                    ErrorResponse = $"Invalid country code '{regionText}'!";
                    return false;
                }

                regions.Add(region);

            }

            #endregion

            #region status

            EndpointStatus? status = null;

            var statusText = QueryString.GetString("status");
            if (statusText is not null)
            {

                if (!EndpointStatus.TryParse(statusText.Trim(), out var parsedStatus) || !parsedStatus.IsKnown)
                {
                    ErrorResponse = $"Invalid status '{statusText}'!";
                    return false;
                }

                status = parsedStatus;

            }

            #endregion

            #region cem / rm

            if (!TryParseBoolean(QueryString, "cem", out var cem, out ErrorResponse) ||
                !TryParseBoolean(QueryString, "rm",  out var rm,  out ErrorResponse))
            {
                return false;
            }

            #endregion

            #region limit / offset

            if (!TryParseInt32(QueryString, "limit",  1, out var limit,  out ErrorResponse) ||
                !TryParseInt32(QueryString, "offset", 0, out var offset, out ErrorResponse))
            {
                return false;
            }

            #endregion

            Query = new WANRegistryQuery(regions, status, cem, rm, limit, offset);
            return true;

        }

        private static Boolean TryParseBoolean(QueryString                       QueryString,
                                               String                            Name,
                                               out Boolean?                      Value,
                                               [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value          = null;
            ErrorResponse  = null;

            var text = QueryString.GetString(Name);
            if (text is null)
                return true;

            switch (text.Trim().ToLowerInvariant())
            {

                case "true":
                    Value = true;
                    return true;

                case "false":
                    Value = false;
                    return true;

                default:
                    ErrorResponse = $"Invalid boolean '{text}' for parameter '{Name}'!";
                    return false;

            }

        }

        private static Boolean TryParseInt32(QueryString                       QueryString,
                                             String                            Name,
                                             Int32                             Minimum,
                                             out Int32?                        Value,
                                             [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value          = null;
            ErrorResponse  = null;

            var text = QueryString.GetString(Name);
            if (text is null)
                return true;

            if (!Int32.TryParse(text.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var number) ||
                number < Minimum)
            {
                ErrorResponse = $"Invalid value '{text}' for parameter '{Name}' (an integer of at least {Minimum} is expected)!";
                return false;
            }

            Value = number;
            return true;

        }

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two queries for equality.
        /// </summary>
        public static Boolean operator == (WANRegistryQuery? Query1, WANRegistryQuery? Query2)
        {

            if (ReferenceEquals(Query1, Query2))
                return true;

            if (Query1 is null || Query2 is null)
                return false;

            return Query1.Equals(Query2);

        }

        /// <summary>
        /// Compares two queries for inequality.
        /// </summary>
        public static Boolean operator != (WANRegistryQuery? Query1, WANRegistryQuery? Query2)
            => !(Query1 == Query2);

        #endregion

        #region IEquatable<WANRegistryQuery> Members

        /// <summary>
        /// Compares two queries for equality.
        /// </summary>
        public override Boolean Equals(Object? Object)
            => Object is WANRegistryQuery query &&
                   Equals(query);

        /// <summary>
        /// Compares two queries for equality.
        /// </summary>
        public Boolean Equals(WANRegistryQuery? Query)
            => Query is not null &&
               Regions.ToHashSet().SetEquals(Query.Regions) &&
               Status == Query.Status &&
               CEM    == Query.CEM    &&
               RM     == Query.RM     &&
               Limit  == Query.Limit  &&
               Offset == Query.Offset;

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object (independent of the order of the regions, like Equals).
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {
                return String.Join(',', Regions.Select(region => region.ToString()).Order(StringComparer.Ordinal)).GetHashCode(StringComparison.Ordinal) * 13 ^
                       (Status?.GetHashCode() ?? 0) * 11 ^
                       (CEM?.   GetHashCode() ?? 0) *  7 ^
                       (RM?.    GetHashCode() ?? 0) *  5 ^
                       (Limit?. GetHashCode() ?? 0) *  3 ^
                       (Offset?.GetHashCode() ?? 0);
            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
        {
            var text = ToQueryString().ToString();
            return text.Length == 0 ? "(all public endpoints)" : text;
        }

        #endregion

    }

}

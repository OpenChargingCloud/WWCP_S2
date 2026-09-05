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

using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.S2.Connect
{

    /// <summary>
    /// A persistent <see cref="IS2Store"/> backed by a single JSON file (PLAN.md D8). It reuses the
    /// in-memory store for the logic and, after every mutation, writes the whole state to disk via
    /// a temporary file and an atomic move. The file carries a <c>formatVersion</c> for future
    /// migrations; access tokens pass through an <see cref="ISecretProtector"/> (the default keeps
    /// them in plaintext). Loading an absent file starts empty.
    /// </summary>
    public sealed class JSONFileS2Store : IFlushableS2Store, IDisposable
    {

        #region Data

        /// <summary>
        /// The file format version written to and expected in the file.
        /// </summary>
        public const Int32 FormatVersion = 1;

        private readonly InMemoryS2Store   inner   = new();
        private readonly SemaphoreSlim     fileLock = new(1, 1);
        private          Boolean           isDisposed;

        #endregion

        #region Properties

        /// <summary>
        /// The path of the JSON file.
        /// </summary>
        public String            FilePath           { get; }

        /// <summary>
        /// The secret protector applied to access tokens.
        /// </summary>
        public ISecretProtector  SecretProtector    { get; }

        /// <summary>
        /// The parser options for reading the stored security material.
        /// </summary>
        public S2ParserOptions   ParserOptions      { get; }

        /// <summary>
        /// The number of stored pairings.
        /// </summary>
        public Int32             Count
            => inner.Count;

        /// <summary>
        /// The number of pending access tokens.
        /// </summary>
        public Int32             PendingCount
            => inner.PendingCount;

        /// <summary>
        /// The number of tombstones.
        /// </summary>
        public Int32             TombstoneCount
            => inner.TombstoneCount;

        #endregion

        #region Constructor(s)

        private JSONFileS2Store(String             FilePath,
                                ISecretProtector?  SecretProtector,
                                S2ParserOptions?   ParserOptions)
        {

            ArgumentException.ThrowIfNullOrWhiteSpace(FilePath);

            this.FilePath         = FilePath;
            this.SecretProtector  = SecretProtector ?? PlaintextSecretProtector.Instance;
            this.ParserOptions    = ParserOptions   ?? S2ParserOptions.Default;

        }

        #endregion


        #region (static) OpenAsync(FilePath, SecretProtector = null, ParserOptions = null, CancellationToken = default)

        /// <summary>
        /// Open (or create) a JSON file store, loading its current content.
        /// </summary>
        /// <param name="FilePath">The path of the JSON file.</param>
        /// <param name="SecretProtector">An optional secret protector (default: plaintext).</param>
        /// <param name="ParserOptions">Optional parser options.</param>
        /// <param name="CancellationToken">A token to cancel the loading.</param>
        public static async Task<JSONFileS2Store> OpenAsync(String             FilePath,
                                                            ISecretProtector?  SecretProtector     = null,
                                                            S2ParserOptions?   ParserOptions       = null,
                                                            CancellationToken  CancellationToken   = default)
        {

            var store = new JSONFileS2Store(FilePath, SecretProtector, ParserOptions);

            await store.LoadAsync(CancellationToken).ConfigureAwait(false);

            return store;

        }

        #endregion

        #region LoadAsync(CancellationToken = default)

        /// <summary>
        /// Load the content of the file (an absent file starts empty).
        /// </summary>
        /// <param name="CancellationToken">A token to cancel the loading.</param>
        public async Task LoadAsync(CancellationToken CancellationToken = default)
        {

            await fileLock.WaitAsync(CancellationToken).ConfigureAwait(false);

            try
            {

                if (!File.Exists(FilePath))
                {
                    inner.Load([], [], []);
                    return;
                }

                var text  = await File.ReadAllTextAsync(FilePath, Encoding.UTF8, CancellationToken).ConfigureAwait(false);

                // Keep ISO-8601 timestamps as strings (Newtonsoft otherwise turns them into DateTime
                // tokens, which cannot be read back as DateTimeOffset).
                using var reader = new JsonTextReader(new StringReader(text)) {
                                       DateParseHandling = DateParseHandling.None
                                   };
                var json  = JObject.Load(reader);

                var formatVersion = json["formatVersion"]?.Value<Int32>() ?? 0;
                if (formatVersion > FormatVersion)
                    throw new NotSupportedException($"The S2 store file '{FilePath}' has format version {formatVersion}, but at most {FormatVersion} is supported!");

                var scheme = json["secretScheme"]?.Value<String>() ?? "";
                if (scheme != SecretProtector.SchemeId)
                    throw new NotSupportedException($"The S2 store file '{FilePath}' was protected with scheme '{scheme}', but the configured protector uses '{SecretProtector.SchemeId}'!");

                var pairings    = new List<Pairing>();
                var pendings    = new List<PendingAccessToken>();
                var tombstones  = new List<(Node_Id, Node_Id, DateTimeOffset)>();

                #region Pairings

                foreach (var token in json["pairings"] as JArray ?? [])
                {

                    if (token is not JObject pairingJSON)
                        throw new FormatException("A pairing entry is not a JSON object!");

                    var unprotected = UnprotectToken(pairingJSON, "accessToken");

                    if (!Pairing.TryParse(unprotected, out var pairing, out var error, ParserOptions))
                        throw new FormatException($"A pairing could not be parsed: {error}");

                    pairings.Add(pairing);

                }

                #endregion

                #region Pending access tokens

                foreach (var token in json["pendingTokens"] as JArray ?? [])
                {

                    if (token is not JObject pendingJSON)
                        throw new FormatException("A pending token entry is not a JSON object!");

                    pendings.Add(ParsePendingToken(pendingJSON));

                }

                #endregion

                #region Tombstones

                foreach (var token in json["unpaired"] as JArray ?? [])
                {

                    if (token is not JObject tombstoneJSON)
                        throw new FormatException("A tombstone entry is not a JSON object!");

                    var local   = Node_Id.Parse(tombstoneJSON["localNodeId"]!.Value<String>()!);
                    var remote  = Node_Id.Parse(tombstoneJSON["remoteNodeId"]!.Value<String>()!);
                    var at      = ParseTimestamp(tombstoneJSON["at"]!.Value<String>()!);

                    tombstones.Add((local, remote, at));

                }

                #endregion

                inner.Load(pairings, pendings, tombstones);

            }
            finally
            {
                fileLock.Release();
            }

        }

        #endregion


        #region IS2Store — reads (delegated)

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<Pairing>> GetPairingsAsync(Node_Id? LocalNodeId = null, CancellationToken CancellationToken = default)
            => inner.GetPairingsAsync(LocalNodeId, CancellationToken);

        /// <inheritdoc/>
        public ValueTask<Pairing?> GetPairingAsync(Node_Id LocalNodeId, Node_Id RemoteNodeId, CancellationToken CancellationToken = default)
            => inner.GetPairingAsync(LocalNodeId, RemoteNodeId, CancellationToken);

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<PendingAccessToken>> GetPendingAccessTokensAsync(Node_Id LocalNodeId, Node_Id RemoteNodeId, CancellationToken CancellationToken = default)
            => inner.GetPendingAccessTokensAsync(LocalNodeId, RemoteNodeId, CancellationToken);

        /// <inheritdoc/>
        public ValueTask<PendingAccessToken?> FindPendingAccessTokenAsync(AccessToken Token, CancellationToken CancellationToken = default)
            => inner.FindPendingAccessTokenAsync(Token, CancellationToken);

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<AccessToken>> GetAccessTokenCandidatesAsync(Node_Id LocalNodeId, Node_Id RemoteNodeId, CancellationToken CancellationToken = default)
            => inner.GetAccessTokenCandidatesAsync(LocalNodeId, RemoteNodeId, CancellationToken);

        /// <inheritdoc/>
        public ValueTask<DateTimeOffset?> GetUnpairedAtAsync(Node_Id LocalNodeId, Node_Id RemoteNodeId, CancellationToken CancellationToken = default)
            => inner.GetUnpairedAtAsync(LocalNodeId, RemoteNodeId, CancellationToken);

        #endregion

        #region IS2Store — writes (delegated, then persisted)

        /// <inheritdoc/>
        public async ValueTask<Pairing?> AddOrReplacePairingAsync(Pairing Pairing, CancellationToken CancellationToken = default)
        {
            var result = await inner.AddOrReplacePairingAsync(Pairing, CancellationToken).ConfigureAwait(false);
            await PersistAsync(CancellationToken).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc/>
        public async ValueTask<Pairing?> RemovePairingAsync(Node_Id LocalNodeId, Node_Id RemoteNodeId, CancellationToken CancellationToken = default)
        {
            var result = await inner.RemovePairingAsync(LocalNodeId, RemoteNodeId, CancellationToken).ConfigureAwait(false);
            await PersistAsync(CancellationToken).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc/>
        public async ValueTask AddPendingAccessTokenAsync(PendingAccessToken PendingAccessToken, CancellationToken CancellationToken = default)
        {
            await inner.AddPendingAccessTokenAsync(PendingAccessToken, CancellationToken).ConfigureAwait(false);
            await PersistAsync(CancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask<Pairing?> ActivateAccessTokenAsync(Node_Id LocalNodeId, Node_Id RemoteNodeId, AccessToken Token, CancellationToken CancellationToken = default)
        {
            var result = await inner.ActivateAccessTokenAsync(LocalNodeId, RemoteNodeId, Token, CancellationToken).ConfigureAwait(false);
            await PersistAsync(CancellationToken).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc/>
        public async ValueTask<Int32> RemovePendingAccessTokensAsync(Node_Id? LocalNodeId = null, Node_Id? RemoteNodeId = null, DateTimeOffset? CreatedBefore = null, CancellationToken CancellationToken = default)
        {
            var result = await inner.RemovePendingAccessTokensAsync(LocalNodeId, RemoteNodeId, CreatedBefore, CancellationToken).ConfigureAwait(false);
            await PersistAsync(CancellationToken).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc/>
        public async ValueTask<Pairing?> UnpairAsync(Node_Id LocalNodeId, Node_Id RemoteNodeId, DateTimeOffset At, CancellationToken CancellationToken = default)
        {
            var result = await inner.UnpairAsync(LocalNodeId, RemoteNodeId, At, CancellationToken).ConfigureAwait(false);
            await PersistAsync(CancellationToken).ConfigureAwait(false);
            return result;
        }

        #endregion

        #region FlushAsync(CancellationToken = default)

        /// <inheritdoc/>
        public Task FlushAsync(CancellationToken CancellationToken = default)
            => PersistAsync(CancellationToken);

        #endregion


        #region (private) PersistAsync(CancellationToken)

        private async Task PersistAsync(CancellationToken CancellationToken)
        {

            await fileLock.WaitAsync(CancellationToken).ConfigureAwait(false);

            try
            {

                // Snapshot inside the lock, so the writer that wins the lock writes the latest state
                // (a snapshot taken before the lock could be overtaken and leave the file behind).
                var (pairings, pendings, tombstones) = inner.Snapshot();

                var json = new JObject(
                               new JProperty("formatVersion",  FormatVersion),
                               new JProperty("secretScheme",   SecretProtector.SchemeId),
                               new JProperty("pairings",       new JArray(pairings.Select(pairing => ProtectToken(pairing.ToJSON(), "accessToken")))),
                               new JProperty("pendingTokens",  new JArray(pendings.Select(PendingTokenToJSON))),
                               new JProperty("unpaired",       new JArray(tombstones.Select(tombstone =>
                                                                   new JObject(
                                                                       new JProperty("localNodeId",   tombstone.LocalNodeId. ToString()),
                                                                       new JProperty("remoteNodeId",  tombstone.RemoteNodeId.ToString()),
                                                                       new JProperty("at",            tombstone.At.ToS2Timestamp())
                                                                   ))))
                           );

                var text = json.ToString(Formatting.Indented);

                var directory = Path.GetDirectoryName(FilePath);
                if (!String.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var tempPath = FilePath + ".tmp";

                await File.WriteAllTextAsync(tempPath, text, new UTF8Encoding(false), CancellationToken).ConfigureAwait(false);

                // Atomic replace: File.Move with overwrite is atomic on the same volume.
                File.Move(tempPath, FilePath, overwrite: true);

            }
            finally
            {
                fileLock.Release();
            }

        }

        #endregion

        #region (private) token protection and pending-token JSON

        private JObject ProtectToken(JObject JSON, String Property)
        {
            if (JSON[Property]?.Value<String>() is String plaintext)
                JSON[Property] = SecretProtector.Protect(plaintext);
            return JSON;
        }

        private JObject UnprotectToken(JObject JSON, String Property)
        {
            if (JSON[Property]?.Value<String>() is String protectedValue)
                JSON[Property] = SecretProtector.Unprotect(protectedValue);
            return JSON;
        }

        private JObject PendingTokenToJSON(PendingAccessToken Token)
            => JSONObject.Create(
                   new JProperty("localNodeId",   Token.LocalNodeId. ToString()),
                   new JProperty("remoteNodeId",  Token.RemoteNodeId.ToString()),
                   new JProperty("token",         SecretProtector.Protect(Token.Token.Value)),
                   new JProperty("createdAt",     Token.CreatedAt.ToS2Timestamp()),
                   Token.SelectedCommunicationProtocol.HasValue
                       ? new JProperty("selectedCommunicationProtocol", Token.SelectedCommunicationProtocol.Value.ToString())
                       : null,
                   Token.SelectedS2MessageVersion is not null
                       ? new JProperty("selectedS2MessageVersion", Token.SelectedS2MessageVersion)
                       : null
               );

        private PendingAccessToken ParsePendingToken(JObject JSON)
        {

            var local     = Node_Id.Parse(JSON["localNodeId"]!.Value<String>()!);
            var remote    = Node_Id.Parse(JSON["remoteNodeId"]!.Value<String>()!);
            var token     = AccessToken.Parse(SecretProtector.Unprotect(JSON["token"]!.Value<String>()!));
            var createdAt = ParseTimestamp(JSON["createdAt"]!.Value<String>()!);

            var protocol  = JSON["selectedCommunicationProtocol"]?.Value<String>() is String protocolText
                                ? CommunicationProtocol.TryParse(protocolText)
                                : null;

            var version   = JSON["selectedS2MessageVersion"]?.Value<String>();

            return new PendingAccessToken(local, remote, token, createdAt, protocol, version);

        }

        private static DateTimeOffset ParseTimestamp(String Text)
            => DateTimeOffset.Parse(Text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);

        #endregion


        #region Dispose()

        /// <summary>
        /// Release the file lock.
        /// </summary>
        public void Dispose()
        {

            if (isDisposed)
                return;

            isDisposed = true;

            fileLock.Dispose();

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => $"JSON file S2 store '{FilePath}' ({Count} pairing(s), {PendingCount} pending, {TombstoneCount} tombstone(s))";

        #endregion

    }

}

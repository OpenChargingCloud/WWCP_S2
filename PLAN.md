# WWCP S2 – Development Plan

Implementation of the **S2 standard** (EN 50491-12-2, wire format *S2 JSON v1.0.0*) and
**S2 Connect 1.0.0** (discovery, pairing, session initiation, unpairing) in C# / .NET 10,
built on the Vanaheimr libraries **Styx** (Illias) and **Hermod** (HTTP, WebSocket, DNS, PKI, TLS).

Status: revision 2, 2026-09-02. Revision 1 was reviewed by six independent reviewers
(S2 JSON, S2 Connect, Styx/Hermod fit, .NET architecture, security/interop, completeness) and every
finding was adversarially verified; the confirmed findings are worked into this revision
(see §10 for the list of changes).

Implementation status (2026-09-05): the user accepted all proposed defaults (§2, §7). Phases 0, 1a–1c,
2a–2b, 3, 4, 5, 6a/6b, 7, 8, 9a–9c, 10a/10b, 11a and 12 are implemented (`CONVENTIONS.md` is the binding authoring guide,
`CHANGELOG.md` lists the content); phase 12 (documentation, public API baseline and packaging) is done as well, so every phase of this plan is implemented. Phase 11b (interop and conformance against s2-python/s2-rust) moves to a separate repository at the user's request (2026-09-05) and is no longer part of this plan; it lives at https://github.com/OpenChargingCloud/S2ConformanceTests. Phase 11a notes: the D13 spike ran and chose the self-signed-leaf fallback (see D13); `TLSProfiles`, `SelfSignedCA`, `CertificatePinStore` and `S2CertificateValidator` live in `Connect/Security/`; `AS2ConnectClient.CertificateValidator` enforces the pins and `AS2Node` pins the peer's `certificateFingerprint` map after pairing (`S2NodeOptions.EnforceCertificatePinning`, default true); the remaining 11a items are done as well: `S2RequestRateLimiter` wraps every route of the pairing and session initiation servers (503 + `Retry-After`, 429 optional), `MaxRequestBodySize`/`MaxHTTPBodySize`/`MaxWebSocketMessageSize` bound every input, and `S2LogRedaction`/`S2RedactingLoggerFactory` redact message and structured state of every log entry (`S2NodeOptions.RedactSecretsInLogs`, default true), proven by fuzz, negative and secrets-in-logs tests. Phase 10 notes: `AS2Node`
composes the S2 Connect components from the deployment/role and establishes sessions automatically on pairing; the stop
order is the specification's; `RMNode`/`CEMNode` carry the role behaviour and the FRBC control types are the
`FRBCResourceManager`/`FRBCEnergyManager` `IS2ControlTypeHandler`s (a PEBC handler is deferred, so the PV sample is a
node-composition skeleton); `JSONFileS2Store` reuses `InMemoryS2Store` via an internal snapshot/load seam and persists
atomically with an `ISecretProtector` hook (DPAPI/KMS deferred; default plaintext); server-initiated unpair sends
`SessionRequest RECONNECT` then closes; the samples run end to end in-process via the `demo` command. Phase 9 notes: the Multicast
DNS/DNS-SD implementation (9a) lives in Hermod's working tree (`Hermod/DNS/Multicast/`, multi-string `TXT`) as the parallel
Hermod contribution and is not yet reviewed or released there (CI needs the Hermod commit); the generic DNS-SD browser is part
of Hermod, WWCP_S2 keeps the S2 semantics only; the responder reports a name conflict (`Conflict` state) instead of renaming;
the `.local` resolution seam is Hermod's `HybridDNSClient` exposed through `IServiceDiscovery.DNSClient` (no separate
`MulticastDNSClient` decorator in WWCP_S2); the `Multicast` test category runs over real UDP sockets on a private port and is
excluded from the CI push run; the `EndpointAdvertiser` treats "ready for pairing" as `IsReadyForPairing` plus a valid own
pairing token. Deviations recorded so far: all S2 Connect types stay in the
single namespace `…S2.Connect` (a `Pairing` sub-namespace would clash with the domain type `Pairing`); the
certificate items of Phase 5 (`SelfSignedCA`, `CertificatePinStore`, `TLSProfiles`, D13 spike) and the
`IServiceDiscovery` seam were deferred to Phases 7/9/11 because Phase 6 only needs the leaf fingerprint as a delegate
(`LocalEndpoint.ServerCertificateFingerprint`); `IS2Store` currently covers the pairing operations (the token rotation
operations, tombstones and `JSONFileS2Store` follow with Phases 8 and 10); `preparePairing` with an unknown
`serverNodeId` answers 204 (OpenAPI) rather than the prose table's 400 `NodeNotFound`; the pairing client observes the
TLS server certificate through Hermod's validator but pins no CA yet and has no nested `HTTPClientLogger` (both
Phase 11); the long-polling 401 blacklist is reported through `LongPollingStopReason.Unauthorized` and persisted by
the host (store support with Phase 10); `IS2Store` now covers pairings, pending access tokens and tombstones, the
`JSONFileS2Store` and the pinned-CA operations follow with Phases 10/11; the session initiation client accepts
self-signed certificates until the pinned CA is enforced in Phase 11. TLS for the WebSocket and HTTP transports is wired through (certificate
selector/validator parameters) but exercised only in Phase 11.


## 1. Scope and sources

| Part | Normative source | Version | Local copy (scratchpad, cloned 2026-09-02) |
|---|---|---|---|
| S2 JSON messages (36) + schemas (41) | https://github.com/flexiblepower/s2-json | tag `v1.0.0` (2026-07-09) | `spec/s2-json/{messages,schemas}` |
| S2 JSON previous release (for interop) | same repository | tag `v0.0.2-beta` (2023-09-21) | not yet cloned, see Phase 2b |
| S2 Connect OpenAPI (pairing, session-init, common, WAN registry) | https://github.com/flexiblepower/s2-connect/openapi | `v1.0` | `spec/s2-connect/openapi/*.yml` |
| S2 Connect specification text | https://docs.s2standard.org/s2-connect/1.0.0/discovery-pairing-authentication/ | `1.0.0` | `spec/s2-documentation/website/s2c_versioned_docs/version-1.0.0/` |
| Concepts, examples (EV/FRBC, PV/PEBC, heat pump, no-control) | https://docs.s2standard.org/docs/learn/ | – | `spec/s2-documentation/website/docs/learn/` |
| Reference implementations (interop tests) | s2-python (`0.0.2-beta` on the wire), s2-rust (S2 Connect), s2-example-implementations | – | `spec/s2-python`, GitHub |

Where the OpenAPI/JSON-schema files and the prose overlap, the formal files take precedence
(S2 Connect §"Formal specification and versioning").

### Out of scope (for now)
* A public WAN pairing endpoint *registry server* (only the registry *client* plus a test double).
* Non-WebSocket transports (MQTT is announced by the spec but not specified).
* The "OEM cloud pairing server for LAN devices" variant beyond what falls out of a generic WAN pairing server.
* User interfaces (only the hooks a UI needs: pairing code display, prepare-pairing signals).


## 2. Architectural decisions (proposed defaults – please confirm or change)

| # | Decision | Proposal | Rationale |
|---|---|---|---|
| D1 | Root namespace | `cloud.charging.open.protocols.S2` for the data model and the messages (folders `DataStructures/`, `Messages/`), sub-namespaces `S2.Session`, `S2.WebSockets`, `S2.Connect`, `S2.Node` for the layers above (see CONVENTIONS.md §1) | Same scheme as `cloud.charging.open.protocols.OpenADRv3` / `OCPPv2_1`. |
| D2 | Project layout | One library `WWCP_S2` (folders = namespaces) + `WWCP_S2Tests` + `WWCP_S2_Samples` (console apps). An architecture test enforces `Messages`/`Session` → no `Connect`, `Connect` → no `Node`, so a later split into `WWCP_S2_Connect`/`WWCP_S2_Node` stays possible and a client-only constrained-RM build remains feasible. | Keeps the existing solution; the layering test makes the later split cheap. |
| D3 | JSON stack | Newtonsoft.Json `JObject` – mandated by Hermod (HTTP API, HTTP client and all sibling WWCP libraries are `JObject`-based). All parsers use Styx `ParseMandatory*`/`ParseOptional*` with `[NotNullWhen]` out parameters; every type exposes `TryParse(JObject, out T?, out String? ErrorResponse, CustomJObjectParserDelegate<T>? = null)` and `ToJSON(CustomJObjectSerializerDelegate<T>? = null)` exactly as `WWCP_OpenADR/DataStructures/Complex/EventPayloadDescriptor.cs`. | Interop with the ecosystem; not an open question any more. |
| D4 | Dependencies | Only Styx + Hermod (no `WWCP_Core`). The thin WebSocket wrapper layer that `WWCP_Core` provides for OCPP is re-implemented here on Hermod directly. | The user asked for Styx/Hermod; `WWCP_Core` is OCPP-flavoured and heavy. |
| D5 | Coding conventions | AGPL-3.0 header, `#region` structure, `[Mandatory]`/`[Optional]`, strongly typed `readonly struct X : IId, IEquatable<X>, IComparable<X>` identifiers (template: `WWCP_OpenADR/DataStructures/Ids/Event_Id.cs`) – also for S2 Connect ids/tokens (`NodeId`, `NodeIdAlias`, `AccessToken`, `CommunicationToken`, `PairingAttemptId`, `PairingToken`); extensible predefined-string structs for enumerations (template: `WWCP_OCPPv2.1/DataStructures/PredefinedStrings`, **but with `StringComparer.Ordinal`** because S2 enums are case-sensitive); XML docs on every public member; English only. | Consistency with WWCP_OpenADR / WWCP_OCPP; `record struct` would break `IId`, `ParseMandatory<T>` reuse and `ToString()` conventions. |
| D6 | Modern .NET 10 / C# 14 usage | `Nullable` + `ImplicitUsings` on, `LangVersion latest`, `TreatWarningsAsErrors`, `AnalysisLevel latest-recommended`, BannedApiAnalyzers (see §3.7), collection expressions, pattern matching, `required`/`init`, `System.Threading.Lock`, `TimeProvider` (scope in D14), `Channel<T>` (pipeline in §3.1), `IAsyncEnumerable<T>`, `RandomNumberGenerator`, `HMACSHA256`, `CryptographicOperations.FixedTimeEquals`, `SearchValues` for token alphabet validation. `readonly record struct` only for structural values (e.g. `HmacChallenge`/`HmacChallengeResponse` byte pairs), with redacted `ToString()`/`PrintMembers`. **Standard Base64 (`Convert.ToBase64String`) for every S2 Connect wire field – never `Base64Url`.** | "Modern .NET 10 rules" without abandoning the ecosystem's structural style. All named APIs verified on SDK 10.0.302. |
| D7 | Both S2 modes | (a) *S2 Connect mode*: no `Handshake`/`HandshakeResponse`, WebSocket authenticated by bearer token; (b) *plain S2-JSON-over-WebSocket mode*: `Handshake` exchange as in the docs examples (ordering rules in §3.4). | (b) is what s2-python, s2-analyzer and the example implementations speak today; needed for interop tests. |
| D8 | Persistence | `IS2Store` defined by *operations* (§3.5), with `InMemoryS2Store` (reference, `Lock`-based) and `JSONFileS2Store` (atomic temp-file + move, `formatVersion`, `ISecretProtector` hook); a contract test suite that third-party stores must pass. | Spec requires tokens to be persisted *before* confirmation and 500/abort when storage fails; real deployments plug in their own store. |
| D9 | mDNS / DNS-SD | Track 9a: generic mDNS (RFC 6762) responder + querier and multi-string DNS-SD TXT support contributed to **Hermod** (`Hermod/DNS/MulticastDNS`), as a separate Hermod PR. WWCP_S2 depends only on an `IServiceDiscovery` seam (advertise/withdraw, browse with change events) and an `IDNSClient` decorator that resolves `*.local` via mDNS; both get in-memory implementations first so Phases 6–10 never wait for 9a. Interim fallback for a first release: manual pairing URL + registry client (Q11). | Hermod today has PTR/SRV/TXT records, `DNSServiceName`, `DNSServiceInstanceName`, `InMemoryDNSZone` and an IPv4 multicast UDP *listener* (port 6363, unicast replies) – but no responder/querier, its `TXT` record concatenates all strings (SPF semantics, RFC 6763 needs one string per key=value), and Hermod clients resolve hosts only through their own unicast `DNSClient` (no OS resolver), so `hostname.local` is unreachable without the decorator. |
| D10 | Version strings | S2 Connect: REST major version `v1`; S2 JSON versions = one configurable list `S2JSONVersions`, default `["v1.0.0", "0.0.2-beta"]` (first = preferred), feeding `supportedS2MessageVersions` in pairing/session initiation and `Handshake.supported_protocol_versions` in plain mode; exact-string matching; the CEM selects the first RM-offered entry it supports; no overlap → `SessionRequest TERMINATE` + close. `0.0.2-beta` is treated as a legacy alias of the v1.0.0 message set (s2-python performs no version validation at all; the only interop requirement is that our CEM accepts an RM offering `0.0.2-beta`). A per-session `NegotiatedVersion` is recorded for diagnostics and future version-gated messages. | Spec: "the exact version string must be used (e.g. `v1.0.0`)"; s2-python, s2-analyzer and all docs examples announce `0.0.2-beta`. |
| D11 | Logging & metrics | `Microsoft.Extensions.Logging` via `ILoggerFactory?` on session/node constructors and on the WebSocket server/client and HTTP clients (forwarded to Hermod, which accepts it there); HTTP clients expose a nested `Logger : HTTPClientLogger` (`DefaultContext = $"S2Connect{Version.String}_HTTPClient"`, as `OpenADRClientLogger`); the HTTP APIs use Hermod `HTTPAPI`'s `LoggingPath`/`LogfileCreator` plus per-route `HTTPRequestLogger`/`HTTPResponseLogger` delegates (as `OpenADRHTTPAPI`); `System.Diagnostics.Metrics.Meter("cloud.charging.open.protocols.S2")` for sessions, messages per type, reception-status errors, pairing attempts/failures, reconnects. Mandatory redaction (§3.6). | Hermod's WebSocket/HTTP-client stack is `ILoggerFactory`-based, `HTTPAPI` is not; OCPP's `DebugX` is legacy. |
| D12 | CI and dependency checkout | GitHub Actions from Phase 0: checkout Styx and Hermod as sibling directories (pinned commit SHAs recorded in the workflow, like Hermod's own `ci.yml`), build, `dotnet test --filter "TestCategory!=Multicast&TestCategory!=Interop&TestCategory!=Timing"` on Windows and Linux; nightly job for `Timing`, `Multicast` (`--network host`) and `Interop` (docker-compose with pinned s2-python/s2-rust). | The "tests green" gate of every phase needs CI from day one; TLS behaviour differs per OS. |
| D13 | TLS chain and pinning strategy (LAN) | Spike in Phase 5: verify on Windows and Linux whether a Hermod server can send the self-signed CA root in the handshake (`SslStreamCertificateContext.Create(..., additionalCertificates: [ca])`; Hermod passes `null` today). If the root is transmitted: pin the CA SHA-256 per domain name, leaf may rotate. If .NET strips it: **fallback = single self-signed server certificate acting as its own CA** (root = leaf, pinned as CA; rotation requires re-pairing) plus an explicit error when no self-signed root is in the chain. Never pin a leaf that is not self-signed. Client chain policy for pinned CAs: `TrustMode = CustomRootTrust`, `CustomTrustStore = {pinnedCA}`, `RevocationMode = NoCheck`, SAN = `hostname.local`, validity period checked. **Spike executed in Phase 11a (Windows 11, .NET 10): the fallback applies.** Hermod builds its server context with `SslStreamCertificateContext.Create(target: leaf, additionalCertificates: null)`, and the client's `chain.ChainPolicy.ExtraStore` — which holds exactly what the peer transmitted — was **empty** for a CA-signed leaf. (`ChainElements` did contain the CA, but only because client and server shared a process and the platform certificate cache; that is not the wire.) So a LAN endpoint presents a **single self-signed server certificate that is its own CA**, its SHA-256 is what `certificateFingerprint` carries and what the peer pins, and rotating it requires re-pairing. `S2CertificateValidator` rejects pinning a non-self-signed leaf with `NotSelfSigned`; `WWCP_S2Tests/Security/D13ChainSpikeTests.cs` is the regression guard. | Spec: client must pin the CA (root) and accept new leaves signed by it; .NET servers omit the root by default. |
| D14 | Time sources | S2-owned deadlines/delays (reception-status timeout, 15 s pairing attempt, 1 s delay, 15 s pending token, ≤30 s websocketToken, 5 min pairing token, 25 s long-poll, reconnect back-off) use an injected `TimeProvider` (default `TimeProvider.System`) and `FakeTimeProvider` in tests. Event/log/delegate timestamps use Styx `Timestamp.Now` and carry `EventTracking_Id`, matching Hermod's delegate shapes. Hermod-side timers (ping/pong, HTTP timeouts, token bucket) stay real-time and are tested with short real intervals under `[Category("Timing")]`. S2 code never calls `Timestamp.Now`/`DateTimeOffset.UtcNow` for expiry comparisons. | Hermod's HTTP1/WebSocket/HTTPAPI stack is `Timestamp.Now`-based, its newer parts (HTTP2/3, QUIC, SSH) already inject `TimeProvider` exactly this way; mixing clocks in expiry comparisons breaks fake-clock tests. |
| D15 | Error model, cancellation, disposal | Expected protocol outcomes are *results* (`S2SendResult`, `PairingResult`, `SessionInitiationResult`, `S2ParseError`) carrying the spec's error enums, HTTP status and a `Retryable` flag; exceptions (`S2Exception` → `S2ParseException`, `StoreUnavailableException`, `S2ReceptionException(status, label)`) only for cancellation, I/O and programmer errors. Every public async method takes a `CancellationToken`. `S2Session`, `S2WebSocketServer/Client`, `PairingAttempt`, `LongPollingClient`, `AS2Node` implement `IAsyncDisposable`; dispose completes pending awaiters and channels. | Spec prescribes per-failure client reactions ("retry later", "do not retry, inform end user", "try other accessToken"); Hermod threads `CancellationToken` everywhere. |
| D16 | Concurrency model | Per session: the medium callback only parses and routes (`ReceptionStatus` → awaiter, everything else → one bounded `Channel<IS2Message>`, `BoundedChannelFullMode.Wait` so Hermod's backpressure applies); a single consumer task dispatches handlers strictly in order; the medium callback never awaits application code. Hermod already serialises outbound writes per connection and invokes events sequentially (`EventInvocation`), so no extra send lock is needed. Per node: pairing attempts are sequential per target node (`ConcurrentDictionary<NodeId, SemaphoreSlim>`) and parallel across nodes; `IS2Store` implementations must be thread-safe. | Hermod awaits the text-message callback inside its per-connection read loop on both server and client; a handler that awaits a `ReceptionStatus` inline would deadlock. |


## 3. Cross-cutting design contracts

### 3.1 Session message pipeline
1. `IS2Medium` is **push-based** (Hermod has no pull API and `WebSocketClient` has no virtual hooks):
   `ValueTask<S2SendResult> SendAsync(String, CancellationToken)`, `event Func<String, CancellationToken, Task> OnTextReceived`,
   `event Func<S2CloseReason, Task> OnClosed`, `Task CloseAsync(code, reason, ct)`, `Boolean IsConnected`, `IAsyncDisposable`.
   `S2WebSocketMedium` wraps either a `WebSocketServerConnection` (fed from `ProcessTextMessage`) or a `WebSocketClient`
   (event subscription); `InMemoryS2Medium` connects two sessions for tests. Hermod's `SentStatus` maps to `S2SendResult`.
2. Inbound: parse → `ReceptionStatus` goes synchronously to the `ReceptionStatusAwaiter`; everything else is written
   to the session channel; the single consumer dispatches to the handler registry in order.
3. Outbound `SendAndAwaitReceptionStatus`: register the awaiter (`ConcurrentDictionary<Message_Id, TaskCompletionSource>`)
   **before** sending, then send, then await with `TimeProvider` timeout + `CancellationToken`; `SendAndForget` for
   messages whose status the caller does not wait for. A missing status is reported as a result, never closes the
   session by default (s2-python sends no status for messages without a registered handler). `OnClosed` fails all
   pending awaiters. `Message_Id.Null` (`00000000-0000-0000-0000-000000000000`) is a legal id and the key for
   unmatched `INVALID_DATA` statuses; an `OnUnmatchedReceptionStatus` event exposes the rest.
4. Handler registry: `S2Session.On<TMessage>(Func<S2Session, TMessage, CancellationToken, Task<ReceptionStatusValue?>>) : IDisposable`
   (null = OK) is the single dispatch point; `OnMessageReceived`/`OnMessageSent`/`OnStateChanged` are logging-only
   events raised through Hermod `EventInvocation`. Control types are objects:
   `IS2ControlTypeHandler { ControlType; ActivateAsync; DeactivateAsync; RegisterHandlers(S2Session) }` (defined in
   Phase 3, implemented by `IFRBCResourceManager` etc. in Phase 10).

### 3.2 ReceptionStatus decision table (one status per received `message_id`, never two)
| Situation | `subject_message_id` | Status |
|---|---|---|
| Not valid JSON | `Message_Id.Null` | `INVALID_DATA` |
| JSON but no `message_id` | `Message_Id.Null` | `INVALID_DATA` |
| Schema failure (unknown `message_type`, missing/extra property, closed-enum violation) with `message_id` | that id | `INVALID_MESSAGE` |
| Intra-message semantic rule violated (§3.3) | that id | `INVALID_MESSAGE` |
| Cross-message reference unknown (actuator, operation mode, instruction, revoked object, timer, …) or not one of `available_control_types` | that id | `INVALID_CONTENT` |
| Out-of-state message (§3.4) | that id | `INVALID_CONTENT` (message ignored; **not** `PERMANENT_ERROR`, which forces a disconnect) |
| Handler returned a status | that id | as returned |
| Handler threw | that id | `PERMANENT_ERROR`, then close |
| Received `ReceptionStatus` | – | never acknowledged |
| `Handshake` / `HandshakeResponse` in plain mode | that id | `OK` (s2-python awaits it) |
Sending side: `TEMPORARY_ERROR` is returned to the caller of `SendAndAwaitReceptionStatus`, who decides whether to
retry (the library imposes no retry schedule); `PERMANENT_ERROR` received for an own message → close the session.
A malformed `ReceptionStatus` is logged and never answered. The OK for a message is sent after its handler returns; handlers must
only validate and enqueue (s2-python gives up after 5 s), long work runs outside the pipeline.

### 3.3 Semantic (non-schema) rule matrix – implemented in constructors/`TryParse` (intra-object) and in the session registry (cross-message)
| Rule | Scope | Status on violation |
|---|---|---|
| `PEBC.PowerConstraints.allowed_limit_ranges` ≥ 2 items, ≥ 1 `UPPER_LIMIT` and ≥ 1 `LOWER_LIMIT` per `CommodityQuantity`; `valid_until ≥ valid_from` | message | INVALID_MESSAGE |
| `Transition.start_timers`/`blocking_timers` reference declared `Timer` ids; `from`/`to` reference declared operation modes | `OMBC.SystemDescription`, `FRBC/DDBC.ActuatorDescription` | INVALID_MESSAGE |
| Id uniqueness: timers/transitions per system/actuator description; operation modes per actuator (FRBC/DDBC) or per RM (OMBC); actuators, instructions, `PEBC.PowerEnvelope`/`PowerConstraints`/`EnergyConstraint`, `PPBC.PowerProfileDefinition` per RM session; `PowerSequenceContainer` per profile; `PowerSequence` per container | message / session | INVALID_MESSAGE / INVALID_CONTENT |
| At most one `PowerRange` per `CommodityQuantity` (OMBC/FRBC/DDBC operation modes), one `PowerForecastValue` per quantity (`PowerForecastElement`, `PPBC.PowerSequenceElement`), one `PowerValue` per quantity (`PowerMeasurement`), one `PEBC.PowerEnvelope` per quantity (`PEBC.Instruction`) | message | INVALID_MESSAGE |
| `FRBC.ActuatorDescription.supported_commodities` unique; every `OperationModeElement` carries at least one `PowerRange` for every supported commodity, none for an unsupported one, at most one per `CommodityQuantity` (three-phase devices have L1/L2/L3 for `ELECTRICITY`) | message | INVALID_MESSAGE |
| Range ordering: `PowerRange`/`NumberRange` start ≤ end; `FRBC.OperationModeElement`/`LeakageBehaviourElement.fill_level_range` start < end; `PEBC.PowerEnvelopeElement` lower ≤ upper; `PEBC.EnergyConstraint` lower ≤ upper average power | type | INVALID_MESSAGE |
| Contiguity of `fill_level_range`s in `FRBC.OperationMode.elements` and `FRBC.LeakageBehaviour.elements` | type | INVALID_MESSAGE |
| `operation_mode_factor` ∈ [0, 1] in all instructions, actuator statuses, `OMBC.Status` | type | INVALID_MESSAGE |
| `SelectControlType.control_type` ∈ `ResourceManagerDetails.available_control_types` (which may contain `NOT_CONTROLABLE` but never `NO_SELECTION`) | session | INVALID_CONTENT |
| `ResourceManagerDetails.currency` mandatory when running/transition costs are published | session | INVALID_CONTENT |
| Conditional presence: `previous_operation_mode_id`/`transition_timestamp` unless first mode; `PPBC.PowerSequenceContainerStatus.progress` once started; `PPBC.PowerProfileStatus` covers all containers | message | INVALID_MESSAGE |
| Cross references: `*.Instruction` actuator/operation-mode/constraints/profile/container/sequence ids, `InstructionStatusUpdate.instruction_id`, `RevokeObject.object_id`, `*.TimerStatus.timer_id`/`actuator_id` | session | INVALID_CONTENT |
| `Handshake.supported_protocol_versions` mandatory for RM; `HandshakeResponse.selected_protocol_version` ∈ RM's list | session | INVALID_MESSAGE / INVALID_CONTENT |
The initial list is derived from every `description` in the schema files and from the `@model_validator`s of
s2-python; each rule gets a test.

### 3.4 Session state machine and message rules
* States: `AwaitingHandshake` (plain mode only) → `WebSocketConnected` ⇄ `ControlTypeActivated(ControlType)` → `WebSocketDisconnected`.
  `SelectControlType` is accepted in every connected state: `NO_SELECTION`/`NOT_CONTROLABLE` → `WebSocketConnected`,
  any other value → `ControlTypeActivated(new)`; on every switch the old handler is deactivated (system descriptions
  dropped, open instructions marked untracked, revokable-object registry cleared) before the new one is activated.
* Rules are **derived from the message schemas** (control-type prefix + sender role) and the spec table is only a
  test oracle with a documented exception list: `DDBC.PresentDemandStatus` is allowed RM→CEM in DDBC state although
  the spec table omits it; `RevokeObject` is allowed in **both** directions (7 of the 13 revokable objects are CEM-sent
  instructions); the table's `PEBC.PowerConstraint` means `PEBC.PowerConstraints`. A test asserts every
  `messages/*.schema.json` appears in at least one state/role cell.
* Plain mode: the CEM sends its `Handshake` at connect time without waiting, accepts the RM `Handshake` in any order,
  answers `ReceptionStatus OK` + `HandshakeResponse` immediately (s2-python sends `ResourceManagerDetails` only after
  it received the `HandshakeResponse`).
* Revocation: `RevokeObject.object_id` refers to `message_id` for `OMBC/FRBC/DDBC.SystemDescription` (they carry no
  `id`) and to `id` for the other ten object types. A per-session `S2ObjectRegistry` keyed by
  (`RevokableObject`, id, direction) holds the last message per revokable object plus `InstructionState`
  (NEW→ACCEPTED/REJECTED→STARTED→SUCCEEDED/ABORTED, REVOKED) on both roles; revoking an unknown object → `INVALID_CONTENT`.

### 3.5 Persistence (`IS2Store`)
Operations, all `ValueTask` + `CancellationToken`, durable before returning, thread-safe:
`TryConsumeWebSocketToken(token) → PairingRef?` · `TryConsumePairingToken(nodeId, token)` ·
`AddPendingAccessToken(pairing, token, expiresAt)` · `ActivateAccessToken(pairing, token)` (removes every other
active/pending token of the (client, server) pair) · `GetAccessTokenCandidates(pairing)` · `RemovePairing(pairing)` ·
`AddUnpairedTombstone(clientNodeId, serverNodeId, at)` (needed to answer `NoLongerPaired` instead of 401) ·
pinned CA fingerprints per domain name (including those received via `postConnectionDetails`) · long-polling
401 blacklist · node/endpoint description updates. File format `{"formatVersion":1, "pairings":[…],
"pendingTokens":[…], "pinnedCAs":[…], "unpaired":[…]}`, written via temp file + `File.Move(overwrite: true)`,
migration hook keyed on `formatVersion`, optional `ISecretProtector` (default no-op; sample using
`ProtectedData` on Windows). `S2StoreContractTests<TStore>` is subclassed by both built-in stores.

### 3.6 Security primitives and redaction
* All secrets come from one `TokenGenerator` on `RandomNumberGenerator` (Styx `RandomExtensions.RandomString/RandomBytes/RandomHexString`
  use `Random.Shared` and are **banned** in this library; Styx `SecureRandomBytes` is fine).
* Challenge/response: `R = HMACSHA256(key: C, message: ASCII(T) ‖ hexDecode(F))` for LAN pairing servers (F = SHA-256
  of the *leaf* certificate DER, colons stripped) and `R = HMACSHA256(key: C, message: ASCII(T) ‖ ASCII(D))` for WAN
  servers, where `D = Uri.IdnHost` of the pairing URL (no scheme, port, userinfo or path; punycode; lower-cased on
  both sides). Compare with `CryptographicOperations.FixedTimeEquals`. Known-answer vectors for both formulas,
  including a port and an IDN host, cross-checked against s2-rust.
* Base64: standard alphabet with padding for `HmacChallenge`, `HmacChallengeResponse`, `AccessToken`,
  `CommunicationToken`; `PairingAttemptId` = 24 random bytes → Base64 (≥ 32 chars, opaque on input);
  `certificateFingerprint` map: emit key `SHA256` (the OpenAPI description's `SHA265` is a typo in non-normative prose; keys are matched case-insensitively), hex with or without colons.
* Pairing tokens: generated from the unambiguous alphabet `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`, validated against the
  full `^[0-9a-zA-Z]{4,}$`/`{6,}$` classes, compared ordinally.
* Redaction: `Authorization` headers and the JSON fields `accessToken`, `websocketToken`, `pairingAttemptId`,
  `clientHmacChallengeResponse`, `serverHmacChallengeResponse` are masked in every Hermod request/response logger
  used by the library (Hermod logs the entire PDU); pairing tokens are never logged; audit records reference tokens
  by a truncated SHA-256; secret-holding structs override `ToString()`. A test greps log output for a known token.

### 3.7 Configuration and constants
`S2ConnectDefaults` (static, every normative constant with its spec reference in the XML doc) and immutable option
records with `init` properties, `Validate()`, `TryParse(JObject)`/`ToJSON()`: `S2NodeOptions`, `PairingServerOptions`,
`PairingClientOptions`, `SessionInitiationOptions`, `DiscoveryOptions`, `WebSocketOptions`, `S2ParserOptions`
(`Version`, `RejectUnknownEnumValues`, `RejectAdditionalProperties`, `RequireUUIDs`) – no process-global `Strict` flag.
`S2BaseURL` value type enforces `https://`, trailing slash, no `/v\d+/` segment and a deployment-dependent host
(DNS name for WAN, `.local` for LAN); used for pairing URLs, `initiateSessionUrl`, TXT records and `EndpointRecord`.

### 3.8 Wire-name quirks (verbatim, protected by schema-validation tests)
`NOT_CONTROLABLE` (enum), `DDBC.OperationMode.Id` (capital I), `DDBC.ActuatorDescription.supported_commodites`,
`FRBC.Instruction.operation_mode` vs `OMBC/DDBC.Instruction.operation_mode_id`,
`PPBC.PowerProfileDefinition.power_sequences_containers`, `Transition.from`/`to`, `ReceptionStatus.subject_message_id`
(no `message_id`). C# property names stay idiomatic; the JSON keys are exact.

### 3.9 Identifiers and timestamps
* `ID` schema pattern `[a-zA-Z0-9\-_:]{2,64}` is **unanchored** – parse leniently (substring match like JSON Schema),
  never alter the wire value (no trimming, so an echoed `subject_message_id` always equals the peer's `message_id`),
  generate lowercase time-ordered UUID v7 by default (s2-python rejects non-UUID ids in 38 classes), `RequireUUIDs`
  option for strict interop. Typed ids: `Message_Id`, `Resource_Id`, `Actuator_Id`, `OperationMode_Id`, `Transition_Id`,
  `Timer_Id`, `Instruction_Id`, `PowerConstraints_Id`, `EnergyConstraint_Id`, `PowerEnvelope_Id`,
  `PowerProfileDefinition_Id`, `PowerSequenceContainer_Id`, `PowerSequence_Id`.
* All 29 `format: date-time` properties are `DateTimeOffset`; the parser requires an explicit offset or `Z`
  (naive strings rejected under `RejectNaiveTimestamps`, otherwise accepted with a warning); serialisation via Styx
  `ToISO8601()` preserving the offset; `Duration` = non-negative `Int64` milliseconds ↔ `TimeSpan` with overflow checks.


## 4. Target structure

```
WWCP_S2/
  Version.cs                              LibraryVersion (SemVer, independent of spec versions),
                                          S2JSONVersions (list), S2ConnectAPIVersions (list)
  Options/                                S2ConnectDefaults, *Options records, S2ParserOptions
  DataStructures/
    Ids/                                  13 typed identifiers (§3.9)
    Enums/                                13 predefined-string structs: Commodity, CommodityQuantity, ControlType,
                                          Currency, EnergyManagementRole, InstructionStatus, ReceptionStatusValue,
                                          RevokableObject, RoleType, SessionRequestType,
                                          PEBC.PowerEnvelopeConsequenceType, PEBC.PowerEnvelopeLimitType,
                                          PPBC.PowerSequenceStatus
    Common/                               9 types: Duration, NumberRange, PowerRange, PowerValue,
                                          PowerForecastElement, PowerForecastValue, Role, Timer, Transition
    PEBC/ (3)  PPBC/ (4)  OMBC/ (1)  FRBC/ (7)  DDBC/ (3)     18 non-enum element types (= 41 schema files with ID)
  Messages/
    IS2Message.cs, IS2MessageWithId.cs, AS2Message.cs, S2MessageParser.cs, S2ParseError.cs,
    IRevokable.cs (ObjectId = id or message_id), IInstruction.cs
    Common/ (10)  PEBC/ (3)  PPBC/ (5)  OMBC/ (4)  FRBC/ (8)  DDBC/ (6)      36 messages
  Session/
    S2Session.cs, S2SessionState.cs, S2MessageRules.cs, ReceptionStatusAwaiter.cs, S2ObjectRegistry.cs,
    IS2ControlTypeHandler.cs, IS2Medium.cs, InMemoryS2Medium.cs, S2SendResult.cs
  WebSockets/
    S2WebSocketServer.cs (: WebSocketServer, like WWCP_Core's WWCPWebSocketServer), S2WebSocketClient.cs (: WebSocketClient),
    S2WebSocketMedium.cs
  Connect/
    DataStructures/                       NodeId, NodeIdAlias, NodeDescription, EndpointDescription, Role, Deployment,
                                          CommunicationProtocol, AccessToken, CommunicationToken, PairingAttemptId,
                                          HmacChallenge, HmacChallengeResponse, HmacHashingAlgorithm, ConnectionDetails,
                                          PairingToken, PairingCode, PairingTarget (NodeId | NodeIdAlias | None),
                                          PairingResponseErrorMessage, CommunicationDetailsErrorMessage,
                                          WebSocketCommunicationDetails, EndpointRecord, CountryCode, RegistryStatus,
                                          WaitForPairingItem/Action, S2BaseURL, PairingResult, SessionInitiationResult
    Crypto/                               ChallengeResponse, TokenGenerator, CertificateFingerprint
    Security/                             TLSProfiles, SelfSignedCA (Hermod PKIFactory), CertificatePinStore, SubnetCheck/ISubnetPolicy
    Pairing/                              PairingServerAPI (: HTTPAPI, mounted on a host-supplied HTTPServer), PairingClient (: AHTTPClient),
                                          PairingAttempt, PairingRateLimiter, LongPollingServer, LongPollingClient
    SessionInitiation/                    SessionInitiationServerAPI (: HTTPAPI, mounted on a host-supplied HTTPServer),
                                          SessionInitiationClient (: AHTTPClient), AccessTokenManager, ReconnectStrategy
    Discovery/                            IServiceDiscovery, InMemoryServiceDiscovery, DNSSDAdvertiser, DNSSDBrowser,
                                          MulticastDNSClient (IDNSClient decorator for *.local), WANRegistryClient
    Store/                                IS2Store, InMemoryS2Store, JSONFileS2Store, ISecretProtector
  Node/
    AS2Node.cs (IAsyncDisposable, StartAsync/StopAsync), CEMNode.cs, RMNode.cs,
    ControlTypes/ (IFRBCResourceManager … + CEM-side counterparts)
  Schemas/                                embedded s2-json v1.0.0 (+ v0.0.2-beta) and s2-connect v1.0 OpenAPI files
  THIRD-PARTY-NOTICES.md                  Apache-2.0 texts + attribution (packed into the NuGet)

WWCP_S2Tests/                             categories Unit, Integration, Timing, Multicast, Interop;
                                          [Property("S2C", "<section>.<row>")] traceability → generated CONFORMANCE.md
WWCP_S2_Samples/                          S2RM.EVCharger (FRBC), S2RM.PV (PEBC), S2CEM.Minimal, S2Connect.PairingTool
                                          (console apps; README snippets are extracted from them; used by the interop job)
```

Deployment note: Hermod's `HTTPServer` has no WebSocket upgrade path and `AWebSocketServer` is its own TCP listener,
so the S2 WebSocket **always runs on a separate port** from the pairing/session HTTPS API; both share one
`ServerCertificateSelectorDelegate`; `websocketUrl` is derived from the configured WebSocket port.


## 5. Development phases

Definition of done per phase: zero warnings, all `Unit`/`Integration` tests green on Windows and Linux CI,
XML docs complete, every normative table row touched by the phase has a `[Property("S2C", …)]`-tagged test.
Sizes: S ≈ ½–1 day, M ≈ 2–4 days, L ≈ 1–2 weeks. Total ≈ 12–14 developer-weeks.

### Phase 0 – Foundation, conventions, CI (M)
1. Repository hygiene: `.gitignore`, `LICENSE` (AGPL-3.0), `THIRD-PARTY-NOTICES.md` (s2-json v1.0.0 and
   s2-connect v1.0, Apache-2.0, packed into the NuGet; one-line pointer in every file that quotes a schema in its
   `#region Documentation`), `README.md`, `SECURITY.md`, CLA, `Directory.Build.props` (LangVersion, warnings-as-errors,
   analyzers, BannedApiAnalyzers list, deterministic builds), `InternalsVisibleTo`.
2. `WWCP_S2.csproj` (`RootNamespace`/`AssemblyName` = `cloud.charging.open.protocols.S2`, package metadata,
   `EnablePackageValidation`), `WWCP_S2Tests.csproj` (NUnit 4.6, adapter, Test SDK, `Microsoft.Extensions.TimeProvider.Testing`,
   JSON-schema validator, NetArchTest for the layering rule), `WWCP_S2_Samples/` skeleton.
3. Embed the 77 s2-json v1.0.0 schema files and the 4 OpenAPI files as resources.
4. `Version.cs`, `Options/S2ConnectDefaults.cs`, test categories and the `S2C` traceability attribute.
5. CI workflow per D12; first commit.

### Phase 1a – Identifiers, enumerations, common types (M)
Typed ids (§3.9), 13 enumerations (ordinal matching, `IsKnown`, `All`), 9 common types (`Duration`, ranges,
`PowerValue`, forecast elements, `Role`, `Timer`, `Transition`), `DateTimeOffset` handling. Tests: round-trips,
boundaries, lower-case enum variants rejected, timestamp offsets `Z`/`+02:00`/fractional seconds.

### Phase 1b – PEBC and PPBC element types (M)
7 types with the intra-object rules of §3.3 and the wire-name quirks of §3.8. Tests incl. schema validation of `ToJSON()`.

### Phase 1c – OMBC, FRBC and DDBC element types (M)
11 types incl. contiguity/uniqueness/commodity rules for actuator descriptions. Tests as above.

### Phase 2a – Common, PEBC and PPBC messages (M)
`IS2Message`/`IS2MessageWithId`/`AS2Message`, `IRevokable`, `IInstruction`, 18 messages, official examples round-trip.

### Phase 2b – OMBC, FRBC, DDBC messages, parser, versions (M)
18 messages, `S2MessageParser.TryParse(JObject, S2ParserOptions, out IS2Message, out S2ParseError)` dispatching on
`message_type` with the negotiated version as input; `0.0.2-beta` maps to the same message set (optional spike:
diff tag `v0.0.2-beta`, known difference: no `DDBC.PresentDemandStatus`); negative tests for `additionalProperties`.

### Phase 3 – Transport-agnostic session layer (L)
`IS2Medium` + `InMemoryS2Medium`, the pipeline of §3.1, the decision table of §3.2, state machine and rules of §3.4,
`S2ObjectRegistry`, `IS2ControlTypeHandler`, results/exceptions/cancellation/disposal of D15, `TimeProvider`
deadlines, logging/metrics of D11. Tests: CEM↔RM over the in-memory medium (full FRBC example), rule table per
state, control-type switch/deactivate, handler that sends and awaits a status (no deadlock), duplicate-status guard,
100 parallel sends keep order, both plain-mode handshake orderings.

### Phase 4 – WebSocket transport on Hermod (M)
1. `S2WebSocketServer : WebSocketServer` (Hermod's application base, the same base as `WWCPWebSocketServer` in
   WWCP_Core; an own `ATCPServer` listener with its own port): `RequireAuthentication = false` (Hermod only
   understands Basic), validation entirely in `OnValidateWebSocketConnection` – `Authorization is HTTPBearerAuthentication`
   and `IS2Store.TryConsumeWebSocketToken` (single use, ≤ 30 s, bound to the pairing) and `HTTPRequest.Path` equals
   the issued path, else `401`/`404`; session attached via `connection.TryAddCustomData("s2.session", …)`;
   `ProcessTextMessage` override forwards to the medium; policy for a second connection of the same pairing (close the
   older one); `EnablePerMessageDeflate = true`, `WebSocketPingEvery = 30 s`, `MaxOutstandingPings`, size limits,
   backpressure, `HandshakeTimeout`, `MaxHandshakeRequestSize`, `MaxConnectionsPerIP`; `Shutdown` on node stop.
2. `S2WebSocketClient : WebSocketClient`: `HTTPAuthentication = new HTTPBearerAuthentication(token)`,
   `RemoteCertificateValidator` with pinned CA (D13) or system trust, deflate, pings, **`ReconnectPolicy = null` with no
   setter** (Hermod's defaults 1 s/30 s/×2 would bypass session initiation), `DNSClient` = the `.local` decorator.
3. Tests: token rejection/replay, path mismatch, deflate on/off, ping/pong (`Timing`), clean close, dropped connection
   raises session-closed and does not reopen the socket.

### Phase 5 – S2 Connect data model, crypto, certificates, seams (L)
1. DTOs for the four OpenAPI files (§4; user-facing strings such as `NodeDescription.userDefinedName`/`brand` and
   `EndpointDescription.name` are plain `String` on the wire, never Styx `I18NString`), `PairingTarget` (never both
   `nodeId` and `nodeIdAlias`), `S2BaseURL`,
   `PairingResult`/`SessionInitiationResult` with the spec's error enums and `Retryable` flags, `finalizePairing.success`
   required on the client, missing → 400 on the server.
2. `PairingToken` (dynamic ≥ 4 / static ≥ 6, alphabet and expiry per §3.6), `NodeIdAlias`, `PairingCode` parser
   (`^([0-9a-zA-Z]+-)?[0-9a-zA-Z]{4,}$`, split at the first dash).
3. `ChallengeResponse`, `TokenGenerator`, `CertificateFingerprint` per §3.6, using Styx `SecureRandomBytes`,
   `ToBase64`, `ToHexString`, `FromHEX`.
4. Certificates (moved here from Phase 11 because the LAN HMAC needs a leaf fingerprint in Phase 6/7): `SelfSignedCA`
   on Hermod `PKIFactory` (`CreateRootCACertificate`, `SelfSignServerCertificate`, `SignServerCertificate`) with the
   mDNS hostname as SAN, `CertificatePinStore` (domain → CA SHA-256), `TLSProfiles.Modern` = `SslProtocols.Tls13`
   (`CipherSuitesPolicy` only when `!OperatingSystem.IsWindows()`, clients only – Hermod servers expose no cipher policy),
   chain policy per D13, and the **D13 spike** (root transmitted on Windows/Linux?) with its decision recorded.
5. `IServiceDiscovery` + `InMemoryServiceDiscovery`, `IDNSClient` decorator interface for `.local` (in-memory first).
6. Tests: known-answer vectors, regex tables, Base64 with `+`/`/`/`=`, fingerprint-map key matching, pin store, TLS profile on both OSs.

### Phase 6a – Pairing server core (L)
`PairingServerAPI : HTTPAPI`, constructed on a host-supplied Hermod `HTTPServer` with `RootPath` = the path of the
pairing URL (self-registers via `HTTPServer.AddHTTPAPI`, so a CEM/OEM product shares its existing port and TLS
certificate; `AS2Node` creates a private `HTTPServer` only when none is supplied) with the version index
`GET {pairingUrl}` → `["v1"]`, `POST /v1/requestPairing`,
`/v1/requestConnectionDetails`, `/v1/postConnectionDetails`, `/v1/finalizePairing`; bearer `pairingAttemptId` via
Hermod's `HTTPAuthentication` delegate; `PairingAttempt` state machine: created after the mandatory 1 s delay
(**15 s window starts when the `pairingAttemptId` is generated**), branch A/B from the deployment/role table, wrong
order/branch → 400 + failed, duplicate request → byte-identical replay of the earlier response (no second token),
**7A/7B: recompute `serverHmacChallengeResponse`, constant-time compare, 403 + attempt invalidated on mismatch**,
branch B: validate `certificateFingerprint` (SHA256 key mandatory, 32 hex bytes) and keep `accessToken` +
`initiateSessionUrl` + fingerprint in the attempt, committed to `IS2Store` (pin to host of `initiateSessionUrl`)
only on `finalizePairing(success=true)`; on success revoke every other token of the (client, server) pair in one
store transaction; all server checks with the exact `PairingResponseErrorMessage` values and `forcePairing`
exemptions; `PairingRateLimiter` (sequential per node, bounded queue e.g. 4, **503** beyond, parallel across nodes);
re-pairing semantics (RM auto-unpairs the previous CEM after success using the unpair procedure matching its
communication role; same pair = unpair + pair with fresh token and session termination). Logging/audit record
(`PairingAttemptId` hash, node ids, outcome, error) via the per-route `HTTPRequestLogger`/`HTTPResponseLogger`
delegates and an `OnPairingAttemptCompleted` event. Tests for all six
deployment/role rows, every error row, 403, 503, timing with `FakeTimeProvider`, interleaved attempts.

### Phase 6b – LAN-only operations and long-polling server (M)
`GET /v1/endpoint`, `GET /v1/nodes`, `POST /v1/preparePairing`, `/v1/cancelPreparePairing`, `/v1/waitForPairing`
(WAN endpoints answer 404). `SubnetCheck`: arrival interface from `HTTPRequest.LocalSocket`, its
`UnicastIPAddressInformation` prefix, peer `RemoteSocket` (`MapToIPv4()` for IPv4-mapped IPv6), loopback accepted,
`fe80::/64` requires equal scope id, `X-Forwarded-For` ignored, `ISubnetPolicy` for proxies; 401 otherwise.
Long-polling server: answer within 25 s, per-node action queue (`sendNodeDescription`, `preparePairing`,
`cancelPreparePairing`, `requestPairing`), at most one item per `clientNodeId`, `minItems 1`, 204 keep-alive,
503 while starting/stopping, per-IP/endpoint cap on hanging requests, `NoValidTokenOnPairingClient` handling, driven
by `IServiceDiscovery` state. Table-driven subnet tests, action-table tests.

### Phase 7 – Pairing client (M)
`PairingClient : AHTTPClient` with nested `Logger : HTTPClientLogger` (`DefaultContext = $"S2Connect{Version.String}_HTTPClient"`);
state machine mirroring 6a with explicit failure edges: parse failure (no finalize), schema failure / role mismatch /
`selectedHmacHashingAlgorithm` not offered / unparseable 8A → `finalizePairing(success=false)`, 401/403 → drop the
attempt and restart at `requestPairing` (never reuse the id), 503 → retry after a short delay (not a failure);
client deadline starts when the 200 of `requestPairing` is parsed, `requestPairing` has its own request timeout;
TLS via `RemoteCertificateValidator`: accept self-signed chains during pairing, **capture the leaf fingerprint on the
version-index GET** and require the same fingerprint on every request of the attempt, pin the CA after step 10 per D13,
re-pairing after a server CA regeneration still accepted at `requestPairing`; `PairingTarget` selection (nodeId after
`GET /nodes` or long-polling, alias from the pairing code, none for single-node endpoints); pre-pairing helpers;
`LongPollingClient` with the full status/action policy (200/204/400 stop-until-readvertised/401 persisted
blacklist/500 wait/503 wait, ≥ 30 s request timeout, always list every hosted node, never start with zero nodes, stop
when no longer capable of pairing or when the advertisement disappears via `IServiceDiscovery`). Tests against the
Phase 6 server in-process: tampered responses, certificate change mid-attempt, 401/403/503, timeouts.

### Phase 8 – Session initiation, unpairing, reconnection (L)
1. `SessionInitiationServerAPI : HTTPAPI` (mounted like the pairing API): version index, `POST /v1/initiateSession` (bearer access
   token; check order per spec with the **`NoLongerPaired` tombstone evaluated before the 401 checks**; pending token
   with 15 s lifetime; selection of version/protocol from the offered lists; persist optional description updates),
   `POST /v1/confirmAccessToken` (activate pending → invalidate old in one store operation, **500 when the store
   fails, pending token untouched**, return `WebSocketCommunicationDetails` with single-use `websocketToken` ≤ 30 s and
   `websocketUrl`), `POST /v1/unpair` (delete security material, write tombstone, 401 when already unpaired).
2. `SessionInitiationClient : AHTTPClient`: version-index GET before every `initiateSession` and before `/unpair`;
   persisted candidate-token list; verify that the selected version/protocol were offered (else retry later); store
   the pending token **before** `confirmAccessToken` and abort when the store fails; remove the old token only after
   the 200 of step 7; after an interrupted confirmation try all candidates sequentially; TLS failures = retry later;
   persist optional server description updates; then open `S2WebSocketClient`.
3. `ReconnectStrategy`: `delay_n = random(0, min(600 s, 2 s × 2^n))`, always via session initiation, stop on
   `NoLongerPaired`, honour `SessionRequest TERMINATE`/`RECONNECT`.
4. Unpairing by the communication server: remove security material + tombstone, send `SessionRequest RECONNECT`,
   **close the WebSocket immediately afterwards**, answer the next `initiateSession` with `NoLongerPaired`; by the
   client: close the session, call `/unpair`, delete local material (401 = already unpaired = local cleanup).
5. Tests: token rotation with simulated crash between steps 4 and 8, expiry of pending tokens, store failure → 500,
   `initiateSession` after unpair → `NoLongerPaired` not 401, both unpairing directions, back-off distribution.

### Phase 9a – Hermod: mDNS responder/querier and DNS-SD TXT (L, separate Hermod PR)
Multi-string `TXT` (`Strings`, `KeyValues` view; the current single-`Text` record stays for SPF), RFC 6762 responder +
querier on 5353 for `224.0.0.251` and `ff02::fb` (multicast replies, QU bit, probing/announcing, conflict resolution,
known-answer suppression, cache-flush, TTL 0 goodbye packets on shutdown, cache with TTL), `.local` A/AAAA resolution
exposed as `IDNSClient`. Packet-level tests with recorded avahi traffic. Reviewed and released in the Hermod repository.

### Phase 9b – DNS-SD advertiser/browser and `.local` resolution in WWCP_S2 (M)
`DNSSDAdvertiser` publishes PTR/SRV/TXT/A/AAAA (`_s2connect._tcp.local`, subtypes as extra PTRs
`_cem._sub._s2connect._tcp.local`/`_rm._sub…`, instance name = hostname, TXT `txtver=1`, `e_name`, `e_logoUrl`,
`pairingUrl`, `longpollingUrl`, each value ≤ 255 UTF-8 bytes validated, at least one URL) into an `InMemoryDNSZone`
consumed by the responder, published while ready for pairing and withdrawn on stop; `DNSSDBrowser` returns
`DNSServiceInstanceName`-keyed results with appeared/disappeared/TXT-changed events, accepts `txtver` **and** `txtvers`,
always uses the URL from the TXT record; `MulticastDNSClient` decorator (mDNS for `*.local`, Hermod `DNSClient` for
the rest) passed to every S2 Connect client and `S2WebSocketClient`. Tests: two in-process endpoints discover each
other and connect via `HTTPSClient` to `<hostname>.local` (`Multicast` category).

### Phase 9c – WAN registry client (S, any time after Phase 5)
`GET /v1/endpoint?region=NL,BE&status=public&cem=true&rm=false&limit&offset`, `GET /v1/endpoint/{id}`, version
index; in-memory registry test double.

### Phase 10a – Node layer: composition and end-to-end (L)
`AS2Node : IAsyncDisposable` with `StartAsync(CancellationToken)`/`StopAsync(TimeSpan drain)` and the stop order
withdraw DNS-SD → stop accepting pairing → close sessions with close frame → stop HTTP APIs → flush store; all loops
(long-polling client, reconnect strategy, browser) owned by the node and cancelled via a linked token; hosted-node
list, pairing-token issuance (dynamic with expiry / static), aliases, `IS2Store`, options records; composition of
pairing server/client, session-initiation server/client and WebSocket server/client from the deployment/role tables;
accepts an existing `HTTPServer`. `RMNode` (one CEM at a time, `ResourceManagerDetails`, measurements/forecasts
independent of the control type, `IFRBCResourceManager` etc. as `IS2ControlTypeHandler` implementations) and
`CEMNode` (many RMs, `SelectControlType` policy, CEM-side handlers, revocation, session supervision). Tests: LAN-LAN
and WAN-LAN end-to-end in-process (discovery → pairing → session → messages → unpairing), stop during an open
pairing attempt, stop with an active session (peer sees a clean close), unpair → socket closed before the 204.

### Phase 10b – Samples (M)
`WWCP_S2_Samples`: `S2RM.EVCharger` (FRBC, exactly the docs example), `S2RM.PV` (PEBC), `S2CEM.Minimal`,
`S2Connect.PairingTool`; README quick-start snippets are extracted from them; they are the processes of the interop job.

### Phase 11a – Security hardening (M) — **done**
Leaf rotation ≤ 6 months under the pinned CA, full certificate validation list (authenticity, domain, expiry,
integrity, crypto) on every TLS setup, Hermod `InMemoryTokenBucketRateLimiter` on pairing/session endpoints,
request-size limits, the redaction layer of §3.6 wired into every logger, secrets-in-logs test, fuzz/negative tests
(malformed JSON, oversized arrays, invalid Base64, replayed tokens, overlapping attempts).

Implemented as `Connect/Security/`: `TLSProfiles`, `SelfSignedCA`, `CertificatePinStore`, `S2CertificateValidator`
(the D13 spike decided the self-signed-leaf fallback, see D13), `S2RequestRateLimiter` (one token bucket per remote
address in front of *every* route of both servers, refused before parsing and cryptography, 503 + `Retry-After` by
default because that is the only overload answer the specification defines, 429 on request) and `S2LogRedaction` /
`S2RedactingLogger` / `S2RedactingLoggerFactory` (secret JSON properties, `Authorization` headers and `name=value`
pairs masked in the formatted message *and* the structured state; `AS2Node` wraps the logger factory it is given).
Size limits at three levels: Hermod's `MaxHTTPBodySize` (1 MiB), the APIs' own `MaxRequestBodySize` (64 KiB, 413 in
the S2 error shape) and `MaxTextMessageSizeIn`/`Out` for WebSocket messages (1 MiB, close 1009).

### Phase 11b – Interop and conformance (M)
Docker-compose interop job: message layer against s2-python (plain WebSocket + `Handshake` `0.0.2-beta`, UUID ids,
aware timestamps, 5 s reception-status budget, no double statuses), S2 Connect pairing/session against s2-rust,
HMAC vectors cross-checked; schema validation of every outgoing message; generated `CONFORMANCE.md` from the `S2C`
test properties (the feature matrix of the s2-connect-implementations page).

### Phase 12 – Documentation and packaging (M) — **done**
README (quick starts from the samples, architecture diagram, deployment/port table, feature matrix), XML-doc
completeness check, `CHANGELOG.md`, NuGet packaging with `THIRD-PARTY-NOTICES.md`, public-API baseline.

Implemented: `README.md` rewritten for a reader who has not seen this plan (quick starts for an RM, a CEM and plain
S2 JSON over WebSockets, a Mermaid architecture diagram with the namespace table, a feature matrix that names the two
gaps – no PEBC/PPBC/OMBC/DDBC handlers and no secret protection at rest – the deployment/port table, the security
defaults and the build, test and packaging instructions), CI badges, and `CHANGELOG.md`. The XML-doc completeness
check needs no separate tool: `GenerateDocumentationFile` plus `TreatWarningsAsErrors` make a missing `<summary>` on a
public member a build error (CS1591), and the test and sample projects switch both off. The public-API baseline is
`WWCP_S2Tests/Architecture/PublicAPI.baseline.txt`, rendered and compared by `PublicAPITests` (5,171 lines; regenerate
with `S2_UPDATE_PUBLIC_API=1`), so an API change is a reviewable diff in the commit that causes it.

Packaging found a real defect: `dotnet pack` turned the sibling ProjectReferences into the NuGet dependencies
`org.GraphDefined.Vanaheimr.Hermod 1.0.0` (does not exist) and `Styx 1.0.0` – an id that on nuget.org belongs to an
unrelated library by another author. Both are `PrivateAssets="all"` now, the CI `Package` step greps the generated
`.nuspec` to keep it that way, and `build/cloud.charging.open.protocols.S2.targets` ships in the package so a consuming
build without Styx and Hermod fails with `S2NUG001` and an instruction rather than a `FileNotFoundException` at run
time (verified end to end against a local feed, in both directions). The package carries README, third-party notices,
XML documentation, a `.snupkg` and SourceLink. Because the two references no longer flow transitively, `WWCP_S2Tests`
and `WWCP_S2_Samples` name Styx and Hermod themselves.

CI and nightly follow Hermod's and Styx's workflows: `windows-latest` and Debian 13 in a `debian:13` container,
`fail-fast: false`, TRX artefacts. `nightly.yml` adds what a gate cannot answer – the `Timing` category (fatal), a
`Multicast` probe (informational, because multicast is a property of the runner) and a build against Styx and Hermod
`master` that prints how far the pinned revisions have drifted; that job exists because the `HERMOD_REF` pin had gone
two phases stale unnoticed. `CONFORMANCE.md` (§8) is generated here after all, by `ConformanceDocumentTests` from the `[S2C]`
properties of the tests: 219 rules across 18 areas, 589 references, guarded by the same compare-or-regenerate
mechanism as the API baseline (`S2_UPDATE_CONFORMANCE=1`). Only the interop half of the old phase 11b, the
measurement against s2-python and s2-rust, lives in the separate repository.


## 6. Dependencies between phases

```
P0 (+CI) → P1a → P1b → P1c → P2a → P2b → P3 → P4 ───────────────┐
                                                                  ├→ P8 → P10a → P10b → P11a → P11b → P12
P0 → P5 (crypto, certs, tokens, IServiceDiscovery seam) → P6a → P6b → P7 ─┘        ↑
                                  │                                                   │
                                  └→ P4 (websocketToken), P9c (registry client) ──────┘
P9a (Hermod mDNS PR, parallel track) → P9b (DNS-SD in WWCP_S2) → P10a LAN end-to-end (`Multicast` tests only)
```

P1–P4 (S2 JSON + session) and P5–P7 (S2 Connect pairing) are independent tracks; P9a runs in the Hermod repository
in parallel and gates only the multicast end-to-end tests, not the release build (Q11).


## 7. Open questions for the user

1. Namespace `cloud.charging.open.protocols.S2` – ok? (Alternative: `…S2v1`.)
2. One assembly with the layering test (proposal) or three assemblies from the start?
3. mDNS/DNS-SD: contribute to Hermod (proposal, separate PR and review cycle) or implement locally in WWCP_S2?
4. Samples: separate `WWCP_S2_Samples` console apps (proposal) – ok?
5. Later `WWCP_Core` bridge (WWCP entities ↔ S2 RM/CEM) like `WWCP_OpenADR_Node` – wanted?
6. S2 JSON versions at launch: `v1.0.0` only, or also `0.0.2-beta` in plain-Handshake mode for s2-python interop (proposal: both)?
7. Logging: `Microsoft.Extensions.Logging` via `ILoggerFactory` (proposal) or Styx `DebugX` like OCPP?
8. May the reference `JSONFileS2Store` keep access tokens in plaintext (proposal: yes, with the `ISecretProtector` hook)?
9. Is a client-only constrained-RM build a first-release goal (proposal: no, but the layering test keeps it possible)?
10. Is Hermod mDNS a blocking prerequisite for release 1.0 (proposal: no – manual pairing URL + registry client as fallback)?
11. D13 fallback if .NET does not transmit the self-signed root: single self-signed certificate acting as its own CA (proposal) or an out-of-band CA download?
12. Dependency pinning in CI: sibling checkout with pinned SHAs (proposal, like Hermod) or git submodules (like OpenADRCLI/OCPP)?


## 8. Test strategy summary

| Category | Runs | Content |
|---|---|---|
| `Unit` | every push, Windows + Linux | types, messages, rules, crypto vectors, parser options, store contract |
| `Integration` | every push | in-memory medium sessions, in-process HTTP/WebSocket pairing/session flows with `FakeTimeProvider` |
| `Timing` | nightly | real-clock Hermod behaviour (pings, HTTP timeouts, token bucket) with generous margins |
| `Multicast` | nightly, `--network host` | DNS-SD advertise/browse, `.local` resolution |
| `Interop` | nightly, docker-compose | s2-python (plain mode), s2-rust (S2 Connect), schema validation of all traffic |

Every normative table row of the S2 Connect spec and every rule of §3.3 maps to at least one test tagged
`[Property("S2C", "<section>.<row>")]`; `CONFORMANCE.md` is generated from these tags.


## 9. Known risks

| Risk | Mitigation |
|---|---|
| .NET/Hermod servers omit the self-signed root from the TLS handshake | D13 spike in Phase 5; fallback single self-signed CA=leaf |
| Hermod has no mDNS and cannot resolve `.local` | `IServiceDiscovery`/`IDNSClient` seams with in-memory implementations; Hermod PR as parallel track; manual URL fallback |
| s2-python interop constraints stricter than the schema (UUID ids, aware timestamps, 5 s status budget, `0.0.2-beta`) | §3.9, §3.2, D10; nightly interop job |
| Secrets leaking into Hermod PDU logs | redaction layer + grep test (§3.6) |
| Inline handler execution in Hermod read loops | channel pipeline (D16, §3.1) |
| Two active tokens after overlapping pairing attempts | replay of duplicate requests, single-transaction token activation (§3.5, Phase 6a) |


## 10. Changes made by the review (revision 1 → 2)

* Corrected: `Base64Url` removed (standard Base64 everywhere); TLS "Modern" reduced to TLS 1.3 + client cipher policy
  on non-Windows; `TimeProvider` scope limited to S2-owned timers; 15 s pairing window anchored at `pairingAttemptId`
  issuance; fingerprint continuity starts at the version-index GET; element-type count 18 (not 21); S2 WebSocket on a
  separate port; HTTP APIs as `HTTPAPI` subclasses mounted on a host-supplied `HTTPServer`, clients as `AHTTPClient`;
  ids/tokens as `IId` structs; S2 JSON version list incl. the legacy `0.0.2-beta`.
* Added normative gaps: server-side 7A/7B HMAC check with 403; branch-B fingerprint validation/pinning and token
  commit on finalize; client check tables (schema, role, selected algorithm/version/protocol, 401/403 restart);
  503/500/401 status codes; `finalizePairing.success` handling; long-polling contract details; `NoLongerPaired`
  tombstone; immediate WebSocket close after unpair and the unpair direction for auto-unpair; version-index GET
  before every session initiation/unpair; `nodeId` vs `nodeIdAlias` selection; `S2BaseURL`; `txtver`/`txtvers`;
  fingerprint-map key rules; `D` normalisation; CSPRNG-only tokens with an unambiguous alphabet; subnet-check rules;
  overlapping-attempt/replay semantics.
* Added S2 JSON semantics: rule matrix §3.3, wire-name quirks §3.8, revocation by `message_id` for system
  descriptions and bidirectional `RevokeObject`, control-type switching/deactivation, reception-status decision table,
  `DDBC.PresentDemandStatus` exception, UUID/`DateTimeOffset` policy, `PowerEnvelope_Id`.
* Added cross-cutting contracts: logging/metrics/redaction (D11), CI from Phase 0 (D12), TLS chain strategy (D13),
  time sources (D14), error/cancellation/disposal model (D15), concurrency model (D16), configuration/options,
  persistence operations and file format, lifecycle/stop order, samples project, conformance traceability,
  third-party notices, library versioning and layering test.
* Re-planned: phases split (1a–c, 2a–b, 6a–b, 9a–c, 10a–b, 11a–b), certificates moved from Phase 11 to Phase 5,
  dependency graph corrected, sizes raised to ≈ 12–14 developer-weeks, open questions rewritten (Newtonsoft is settled).

# Changelog

All notable changes to WWCP S2 are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); the library uses semantic versioning
independent of the S2 JSON and S2 Connect versions it implements.

## [Unreleased]

### Added

- Phase 0: project foundation – `Directory.Build.props` (warnings as errors, recommended analyzers,
  XML docs), library/test/samples projects, embedded s2-json v1.0.0 schemas and s2-connect v1.0
  OpenAPI files (Apache-2.0, see `THIRD-PARTY-NOTICES.md`), `Version`, `S2ConnectDefaults`,
  `S2ParserOptions`, banned-API list, test categories, `[S2C]` conformance traceability attribute,
  layering architecture test, GitHub Actions CI with pinned Styx/Hermod checkouts.
- Phase 1: the complete S2 JSON v1.0.0 data model – 14 typed identifiers, 13 enumerations
  (extensible predefined-string structs with `IsKnown`), `Duration`, the common types and the
  PEBC/PPBC/OMBC/FRBC/DDBC element types with constructor validation of the schema constraints and
  the semantic rules of the schema descriptions; `S2JSONExtensions` (RFC 3339 timestamps, options-aware
  parsing of nested objects, arrays, identifiers and enumerations, additional-property check).
- Phase 2: all 36 S2 JSON messages (`AS2Message`, `IRevokable`, `IInstruction`), `S2MessageParser`
  with the ReceptionStatus error mapping (`S2ParseError`), documentation-example round-trip tests.
- Phase 3: the transport-agnostic session layer – `IS2Medium` / `InMemoryS2Medium`, `S2Session`
  (ordered single-consumer pipeline, exactly one ReceptionStatus per message, reception-status
  correlation with `TimeProvider` timeouts, message rules per state and direction, plain-mode
  Handshake negotiation incl. the legacy `0.0.2-beta` version, control-type activation and handlers,
  revocation and instruction bookkeeping in `S2ObjectRegistry`).

- Phase 4: the WebSocket transport on Hermod – `S2WebSocketServer` (single-use bearer
  communication tokens via `CommunicationTokenStore` or a custom validator, 401 with
  `WWW-Authenticate: Bearer` on rejection, optional path check, permessage-deflate, pings every
  30 s, one `S2Session` per accepted connection with `OnSessionStarted`/`OnSessionEnded`),
  `S2WebSocketClient` (bearer token, deflate, pings, no transport-level reconnect,
  `ConnectSessionAsync` returning the started session), the `IS2Medium` adapters
  `S2WebSocketServerMedium`/`S2WebSocketClientMedium`, and in-process tests over real TCP.

- Phase 5: the S2 Connect data model and crypto primitives (namespace `…S2.Connect`) – `Node_Id`
  and `EndpointRecord_Id` (UUIDs), `NodeIdAlias`, `PairingToken` (dynamic/static rules, confusion-free
  alphabet, redacted `ToString`), `PairingCode` (split at the dash), `PairingAttemptId`, the Base64
  secrets `AccessToken`, `CommunicationToken`, `HmacChallenge`, `HmacChallengeResponse` (verbatim wire
  text, constant-time comparison), `CertificateFingerprint`, `CountryCode`, `S2BaseURL` (https, trailing
  slash, no API version), `PairingTarget`, the eight enumerations of the OpenAPI files, the DTOs of the
  pairing, session-initiation and registry APIs (`NodeDescription`, `EndpointDescription`,
  `ConnectionDetails`, `RequestPairingRequest`/`Response`, …, `WebSocketCommunicationDetails`,
  `EndpointRecord`), `ChallengeResponse` (`HMAC(C, T || F)` / `HMAC(C, T || D)` with IDNA domain
  normalisation, verified against independently computed known-answer vectors) and `TokenGenerator`.
  `S2ParserOptions.AllowInsecureURLs` admits `http://`/`ws://` for tests only.

- Phase 6: the S2 Connect pairing server (namespace `…S2.Connect`) – `PairingServerAPI` on Hermod's `HTTPAPI`
  (version index, requestPairing / requestConnectionDetails / postConnectionDetails / finalizePairing with the exact
  `PairingResponseErrorMessage` values, bearer `pairingAttemptId`, byte-identical replay of duplicate requests, 403 on a
  wrong challenge response, 400 on invalid interactions, the mandatory per-node one-second delay via `PairingRateLimiter`
  with 503 beyond the queue limit, 15 s attempt window, audit events), `PairingAttempt` state machine, `LocalEndpoint` /
  `HostedNode` (own dynamic/static and entered pairing tokens, node ID aliases, readiness), `CommunicationRole` mapping
  table, the persisted `Pairing`, `IS2Store` with `InMemoryS2Store` and a store contract test suite, the LAN-only
  operations (endpoint, nodes, preparePairing, cancelPreparePairing, waitForPairing; 404 on WAN endpoints) guarded by
  `ISubnetPolicy` / `SubnetCheck` (IPv4/IPv6, IPv4-mapped, loopback, link-local scope), and `LongPollingServer`
  (25 s window, one action per client node, automatic description requests, 503 while unavailable); transport
  independent `PairingServerResult` methods next to the HTTP handlers; tests over real HTTP with a plain `HttpClient`.

- Phase 7: the S2 Connect pairing client – `PairingClient` on Hermod's `AHTTPClient` (version index and
  selection, the complete pairing interaction with every client check of the specification as an explicit
  `PairingClientOutcome`, best-effort `finalizePairing(success = false)`, 503 retries, the 15 s client deadline,
  LAN and WAN challenge-response formulas, observation of the TLS server certificate fingerprint, the LAN-only
  operations endpoint/nodes/preparePairing/cancelPreparePairing/waitForPairing), `PairingClientOptions`,
  `PairingClientResult` / `ClientOperationResult<T>`, `LongPollingClient` (the client policy table of the
  specification, automatic pairing on `requestPairing`, `NoValidTokenOnPairingClient` reporting) with
  `LongPollingClientOptions`, and `LocalEndpoint.CACertificateFingerprint`; tests of client and server in-process
  over real HTTP plus a tampering fake server.

- Phase 8: session initiation, unpairing and reconnection – the `IS2Store` operations for token rotation
  (`PendingAccessToken`, activation in one operation, candidate tokens, `UnpairAsync` with tombstones),
  `SessionInitiationServerAPI` on Hermod's `HTTPAPI` (version index, initiateSession with the check order of the
  specification and `NoLongerPaired` before the 401 checks, confirmAccessToken activating the pending token and issuing a
  single-use communication token with an `S2ConnectSessionIdentity` for the WebSocket server, unpair, server-initiated
  unpairing), the shared client base `AS2ConnectClient`, `SessionInitiationClient` (candidate tokens, persisted pending
  token before confirmation, activation after the 200 of step 7, `ConnectAsync` opening the `S2WebSocketClient`,
  `UnpairAsync` with local cleanup), `ReconnectStrategy` (exponential back-off of the specification) and
  `ReconnectingSessionClient` (reconnection through session initiation, RECONNECT/TERMINATE handling, stop on
  `NoLongerPaired`); tests over real HTTP and WebSockets in-process.
- Phase 9: discovery – in Hermod (a separate contribution, `Hermod/DNS/Multicast/`) Multicast DNS (RFC 6762) and DNS-SD
  (RFC 6763): `MulticastDNSMessage`, `IMulticastDNSTransport` with the UDP transport and an in-memory network,
  `MulticastDNSResponder` (probing, announcing, known-answer suppression, additional records, rate limit, legacy unicast,
  NSEC negative answers, goodbye packets, conflict detection), `MulticastDNSClient : IDNSClient` with cache and the DNS-SD
  `MulticastDNSBrowser`, `HybridDNSClient` for `.local` names, multi-string `TXT` records and `DNSServiceName` owners for
  `PTR`/`TXT`; in WWCP_S2 the S2 Connect DNS-SD layer: `S2DNSSDTXTRecord`, `S2ServiceAdvertisement`,
  `S2DiscoveredEndpoint`, the `IServiceDiscovery` seam with `DNSSDServiceDiscovery` (advertiser and browser on Hermod's
  Multicast DNS, hybrid DNS client for the S2 Connect clients) and `InMemoryServiceDiscovery`, `EndpointAdvertiser`
  (publishes a LAN endpoint while a hosted node is ready for pairing), and the WAN endpoint registry: `WANRegistryQuery`,
  `WANRegistryClient` and the reference `WANRegistryAPI` with `InMemoryWANRegistry`; tests on the in-memory Multicast DNS
  network (discovery → pairing through `hostname.local`) and, in the `Multicast` category, over real UDP sockets.

### Fixed (after the phase 0–3 code review)

- Timestamps are parsed strictly as RFC 3339 (lower-case `t`/`z` accepted, time-only or textual dates
  rejected) and serialised with at most six fractional digits.
- JSON numbers must be finite (`NaN`/`Infinity` literals and out-of-range integers are rejected);
  `NumberRange` and `FRBC.StorageStatus` reject non-finite values; `Duration` rejects values beyond `TimeSpan`.
- Identifiers and enumeration values are never trimmed, so an echoed `subject_message_id` always equals
  the peer's `message_id`.
- `S2MessageParser` reports a missing `message_type` as `INVALID_MESSAGE` against the `message_id` when
  one is present; a malformed `ReceptionStatus` is flagged and never answered by the session.
- `S2Session` subscribes to the medium in its constructor (messages arriving before `StartAsync` are
  queued), activates the control type before the CEM's `SelectControlType` is on the wire, deactivates
  the old control-type handler before clearing the registry and on close, and enforces the
  "currency mandatory when costs are published" rule.

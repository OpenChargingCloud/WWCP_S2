# WWCP S2

An implementation of the **S2 standard** for energy flexibility (EN 50491-12-2, wire format
[S2 JSON v1.0.0](https://github.com/flexiblepower/s2-json)) and of
[**S2 Connect 1.0.0**](https://docs.s2standard.org/s2-connect/1.0.0/) (discovery, pairing,
session initiation and unpairing) in C# / .NET 10.

S2 defines how a *Customer Energy Manager* (CEM) and *Resource Managers* (RM) of flexible
devices such as EV chargers, heat pumps, batteries and PV systems exchange flexibility
information and instructions. This library is part of the World Wide Charging Protocol Suite
(WWCP) and is built on the Vanaheimr [Styx](https://github.com/Vanaheimr/Styx) and
[Hermod](https://github.com/Vanaheimr/Hermod) libraries.

## Status

Under development, see [PLAN.md](PLAN.md) for the development plan, the architectural
decisions and the phase status. Nothing in this repository is production ready yet.

| Phase | Content | Status |
|---|---|---|
| 0 | Foundation, conventions ([CONVENTIONS.md](CONVENTIONS.md)), CI | done |
| 1a–1c | S2 JSON data model: 14 identifiers, 13 enumerations, 27 element types | done |
| 2a–2b | All 36 S2 JSON messages and the message parser with ReceptionStatus error mapping | done |
| 3 | Transport-agnostic session layer (`S2Session`, message rules, reception-status correlation, object registry, in-memory medium) | done |
| 4 | WebSocket transport on Hermod (`S2WebSocketServer`/`S2WebSocketClient`, bearer-token authentication, deflate, pings) | done |
| 5 | S2 Connect data model and crypto primitives (identifiers, tokens, pairing codes, fingerprints, base URLs, all API DTOs, HMAC challenge-response) | done |
| 6 | S2 Connect pairing server on Hermod's HTTP API (pairing interaction, rate limiting, LAN-only operations, long-polling, subnet check, in-memory store) | done |
| 7 | S2 Connect pairing client and long-polling client on Hermod's HTTP client | done |
| 8 | S2 Connect session initiation with access-token rotation, unpairing in both directions, reconnection strategy | done |
| 9 | Discovery: Multicast DNS and DNS-SD in Hermod, S2 endpoint advertiser and browser with `.local` resolution, in-memory service discovery, WAN endpoint registry client and reference API | done |
| 10–12 | Nodes, samples, hardening, interop, documentation | planned |

Every serialised message and data structure is validated against the embedded s2-json v1.0.0
schemas in the test suite; the data model enforces the semantic rules of the schema descriptions.

## Building

The solution references Styx and Hermod as sibling directories:

```
<parent>/Styx/Styx/Styx.csproj
<parent>/Hermod/Hermod/Hermod.csproj
<parent>/WWCP_S2/WWCP_S2.slnx
```

```bash
dotnet build WWCP_S2.slnx
dotnet test WWCP_S2Tests/WWCP_S2Tests.csproj --filter "TestCategory!=Multicast&TestCategory!=Interop&TestCategory!=Timing"
```

## Licenses

WWCP S2 is released under the GNU Affero General Public License 3.0 or later, see
[LICENSE](LICENSE). The embedded S2 JSON schemas and S2 Connect OpenAPI files are
Apache-2.0 licensed by the FlexiblePower Alliance Network, see
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Your contributions

This software is developed by [GraphDefined GmbH](http://www.graphdefined.com).
We appreciate your participation in this ongoing project, and your help to improve it.
If you find bugs, want to request a feature or send us a pull request, feel free to
use the normal GitHub features to do so. For this please read the
[Contributor License Agreement](Contributor%20License%20Agreement.txt)
carefully and send us a signed copy.

# WWCP S2 – Coding conventions and authoring guide

This document is the contract every data structure, message and test in this repository follows.
It is written for humans and for the coding agents that generate the bulk of the formulaic types.
Reference implementations ("templates") live in the repository; copy their structure exactly.

| Kind | Template file |
|---|---|
| Identifier struct | `WWCP_S2/DataStructures/Ids/Message_Id.cs` (all other ids are generated from it) |
| Enumeration struct | `WWCP_S2/DataStructures/Enums/ControlType.cs` (all other enums are generated from it) |
| Simple value struct | `WWCP_S2/DataStructures/Common/Duration.cs` |
| Complex type (class) | `WWCP_S2/DataStructures/Common/NumberRange.cs`, `WWCP_S2/DataStructures/Common/Timer.cs` |
| JSON helpers | `WWCP_S2/DataStructures/S2JSONExtensions.cs` |
| Parser options | `WWCP_S2/Options/S2ParserOptions.cs` |
| Tests | `WWCP_S2Tests/DataStructures/CommonTypeTests.cs`, schema validation via `WWCP_S2Tests/Schemas/S2SchemaValidator.cs` |

The normative schemas are in `WWCP_S2/Schemas/S2JSON/v1.0.0/{messages,schemas}/*.schema.json`.
Read the schema of a type before writing it; quote it in the `#region Documentation` block.


## 1. File and naming rules

* One public type per file, file name = type name, AGPL header exactly as in the templates
  (`Copyright (c) 2014-2026 GraphDefined GmbH`, `This file is part of WWCP S2`).
* Namespace: `cloud.charging.open.protocols.S2` for every data structure and message
  (block-scoped `namespace … { }`, not file-scoped). Session layer: `…S2.Session`,
  WebSocket transport: `…S2.WebSockets`, S2 Connect: `…S2.Connect`, nodes: `…S2.Node`.
* Type names follow the schema `title`: `PowerRange`, `FRBC_ActuatorDescription`, `PEBC_Instruction`
  (the dot of the schema name becomes an underscore). Identifiers: `Message_Id`, `Actuator_Id`, …
  Enumerations: `ControlType`, `ReceptionStatusValue` (singular), `RevokableObject` (singular).
* C# property names are PascalCase (`FillLevelRange`, `AbnormalConditionOnly`, `ValueUpper95PPR`),
  JSON property names are the exact schema keys (`fill_level_range`, `value_upper_95PPR`).
  Wire-name quirks are kept verbatim: `Id` (capital I) in `DDBC.OperationMode`,
  `supported_commodites` in `DDBC.ActuatorDescription`, `operation_mode` in `FRBC.Instruction` vs.
  `operation_mode_id` in `OMBC/DDBC.Instruction`, `power_sequences_containers` in
  `PPBC.PowerProfileDefinition`, `from`/`to` in `Transition`, `NOT_CONTROLABLE`.
* Folders: `DataStructures/Ids`, `DataStructures/Enums`, `DataStructures/Common`,
  `DataStructures/PEBC|PPBC|OMBC|FRBC|DDBC`, `Messages/Common|PEBC|PPBC|OMBC|FRBC|DDBC`.
* `using` blocks are wrapped in `#region Usings … #endregion`; `using org.GraphDefined.Vanaheimr.Illias;`
  gives `[Mandatory]`, `[Optional]`, `JSONObject.Create`, `CloneString()`, `CalcHashCode()`,
  `CustomJObjectParserDelegate<T>`, `CustomJObjectSerializerDelegate<T>`.
* Every public member has an XML documentation comment (missing docs are build errors).
  Property summaries are the schema `description` texts, lightly edited into English prose.
* Analyzer settings are in `.editorconfig`; warnings are errors. Do not add pragmas or
  `GlobalSuppressions` without a comment explaining why.


## 2. Anatomy of a complex type

```
public sealed class FRBC_OperationModeElement : IEquatable<FRBC_OperationModeElement>
{
    #region Properties            [Mandatory]/[Optional] attributes, get-only, schema order
    #region Constructor(s)        validation (throw ArgumentException), defensive copies of lists, hashCode
    #region Documentation         the schema, quoted as // comments, plus the semantic rules
    #region (static) TryParse     three overloads, see §3
    #region ToJSON                see §4
    #region Clone()
    #region Operator overloading  ==, !=
    #region IEquatable<T> Members Equals(Object?), Equals(T?)
    #region (override) GetHashCode()   returns the precomputed hashCode field
    #region (override) ToString()
}
```

* Mandatory properties are non-nullable; optional properties are nullable (`Double?`, `String?`,
  `NumberRange?`, `Duration?`). Booleans are `Boolean`; JSON numbers are `Double`; integers that
  represent milliseconds are `Duration`; timestamps are `DateTimeOffset`; texts are `String`.
* Arrays are `IReadOnlyList<T>` (order matters in S2, e.g. chronological elements). The constructor
  takes `IReadOnlyList<T>` (or `IEnumerable<T>`) and stores `[.. items]` (a defensive copy), so
  every property is an immutable snapshot. Optional arrays are `IReadOnlyList<T>?`.
* Constructor parameter order: mandatory parameters first in schema order, then optional
  parameters with `= null` defaults. `ArgumentException` (with `nameof(Parameter)`) for every
  violated schema constraint (`minItems`, `maxItems`, ranges) and semantic rule (see §5).
* `hashCode` is computed once in the constructor: combine with `* prime ^`, use
  `Enumerable.CalcHashCode()` from Styx for lists, `?.GetHashCode() ?? 0` for optionals,
  `GetHashCode(StringComparison.Ordinal)` for strings.
* `Equals(T?)`: every property, lists compared with `SequenceEqual`, strings ordinal, optional
  values with `Nullable.Equals` or `==`.
* `Clone()`: deep clone (`Id.Clone()`, `list.Select(x => x.Clone())`, `String.CloneString()`).
* `ToString()`: a short, human readable summary (id, key values), never JSON.


## 3. TryParse – exactly three overloads

```
public static Boolean TryParse(JObject JSON, [NotNullWhen(true)] out T? Value, [NotNullWhen(false)] out String? ErrorResponse)
    => TryParse(JSON, out Value, out ErrorResponse, null, null);

public static Boolean TryParse(JObject JSON, [NotNullWhen(true)] out T? Value, [NotNullWhen(false)] out String? ErrorResponse,
                               S2ParserOptions? Options)
    => TryParse(JSON, out Value, out ErrorResponse, Options, null);

public static Boolean TryParse(JObject JSON, [NotNullWhen(true)] out T? Value, [NotNullWhen(false)] out String? ErrorResponse,
                               S2ParserOptions? Options, CustomJObjectParserDelegate<T>? CustomParser)
{ try { … } catch (Exception e) { Value = null; ErrorResponse = "The given JSON representation of a … is invalid: " + e.Message; return false; } }
```

The second overload is what `S2TryParser<T>` delegates bind to (`FRBC_OperationMode.TryParse` as a
method group); the first one keeps compatibility with the Styx parser delegates. No optional
parameters on any of the three (method-group conversion requires exact signatures).

Inside the third overload, one `#region name [mandatory|optional]` per schema property, in schema
order, using the helpers of `S2JSONExtensions` (all return `false` with an `ErrorResponse` on error;
optional helpers return `true` with a null value when the property is absent):

| Schema construct | Helper |
|---|---|
| `"type": "number"` | `ParseMandatoryS2Number` / `ParseOptionalS2Number` (`out Double` / `out Double?`) |
| `"type": "boolean"` | `ParseMandatoryS2Boolean` / `ParseOptionalS2Boolean` |
| `"type": "string"` | `ParseMandatoryS2String` / `ParseOptionalS2String` |
| `"format": "date-time"` | `ParseMandatoryS2Timestamp` / `ParseOptionalS2Timestamp` (needs `Options`) |
| `$ref Duration` | `ParseMandatoryS2Duration` / `ParseOptionalS2Duration` |
| `$ref ID` | `ParseMandatoryS2Id(name, desc, Actuator_Id.TryParse, Options, out Actuator_Id id, out ErrorResponse)` / `ParseOptionalS2Id` |
| array of `$ref ID` | `ParseMandatoryS2Ids(name, desc, Timer_Id.TryParse, Options, MinItems, MaxItems, out IReadOnlyList<Timer_Id>? ids, out ErrorResponse)` |
| `$ref <enum>` | `ParseMandatoryS2Enum(name, desc, Commodity.TryParse, Options, out Commodity value, out ErrorResponse)` / `ParseOptionalS2Enum` |
| array of `$ref <enum>` | `ParseMandatoryS2Enums(name, desc, Commodity.TryParse, Options, MinItems, MaxItems, out IReadOnlyList<Commodity>? values, out ErrorResponse)` |
| `$ref <object>` | `ParseMandatoryS2(name, desc, NumberRange.TryParse, Options, out NumberRange? value, out ErrorResponse)` / `ParseOptionalS2` |
| array of `$ref <object>` | `ParseMandatoryS2List(name, desc, PowerRange.TryParse, Options, MinItems, MaxItems, out IReadOnlyList<PowerRange>? values, out ErrorResponse)` / `ParseOptionalS2List` |
| `additionalProperties: false` | `JSON.CheckAdditionalProperties(Options, out ErrorResponse, "prop1", "prop2", …)` as the last region before constructing the object |

Pass `MinItems`/`MaxItems` from the schema (`minItems`/`maxItems`); use `null` when the schema
has none. Then construct the object (`new T(…)`; the constructor validates the semantic rules and
the `catch` turns its exception into the error response), apply `CustomParser` when given, return true.


## 4. ToJSON

```
public JObject ToJSON(CustomJObjectSerializerDelegate<T>? CustomTSerializer = null)
{
    var json = JSONObject.Create(
                         new JProperty("id",                Id.ToString()),
                   DiagnosticLabel is not null
                       ? new JProperty("diagnostic_label",  DiagnosticLabel)
                       : null,
                         new JProperty("power_ranges",      new JArray(PowerRanges.Select(pr => pr.ToJSON()))),
                   RunningCosts is not null
                       ? new JProperty("running_costs",     RunningCosts.ToJSON())
                       : null,
                         new JProperty("abnormal_condition_only", AbnormalConditionOnly)
               );
    return CustomTSerializer is not null ? CustomTSerializer(this, json) : json;
}
```

* Properties in schema order; optional properties are omitted (never `null`) when not set.
* Ids and enumerations serialise via `ToString()`; `Duration` via `Duration.ToJSON()`; timestamps
  via `Timestamp.ToS2Timestamp()`; nested objects via their `ToJSON()`; lists via `new JArray(…)`.
* Only the type's own custom serializer is a parameter (nested types use their default `ToJSON()`);
  this is a deliberate simplification of the sibling libraries' cascaded serializer parameters.


## 5. Semantic rules (validated in constructors, tested)

From PLAN.md §3.3 and the schema descriptions. Intra-object rules throw `ArgumentException` in
the constructor; cross-message rules (id references between messages) belong to the session layer.

* `NumberRange`, `PowerRange`: `StartOfRange <= EndOfRange`.
  Strict `<` for `FRBC_OperationModeElement.FillLevelRange` and `FRBC_LeakageBehaviourElement.FillLevelRange`.
* `PEBC_PowerEnvelopeElement`: `LowerLimit <= UpperLimit`.
* At most one entry per `CommodityQuantity`: `PowerForecastElement.PowerValues`,
  `PPBC_PowerSequenceElement.PowerValues`, `PowerRanges` of `OMBC_OperationMode`,
  `FRBC_OperationModeElement` and `DDBC_OperationMode`.
* `FRBC_OperationMode.Elements` and (message) `FRBC.LeakageBehaviour.elements`: fill level ranges
  contiguous – sorted by `StartOfRange`, each element's `EndOfRange` equals the next element's `StartOfRange`.
* `FRBC_ActuatorDescription` / `DDBC_ActuatorDescription`: `SupportedCommodities` unique; unique
  operation mode ids, transition ids and timer ids; every `Transition.From`/`To` references a declared
  operation mode; every `StartTimers`/`BlockingTimers` entry references a declared timer; every
  operation mode (element) carries at least one `PowerRange` for every supported commodity, none for an
  unsupported commodity, and at most one per `CommodityQuantity` (a three-phase device legitimately has
  `ELECTRIC.POWER.L1`, `L2` and `L3` ranges for the single commodity `ELECTRICITY`), using the mapping
  `CommodityQuantity → Commodity` (`ELECTRIC.*` → `ELECTRICITY`, `NATURAL_GAS.*`/`HYDROGEN.*` → `GAS`,
  `HEAT.*` → `HEAT`, `OIL.*` → `OIL`) exposed as `CommodityQuantity.Commodity`. The same rule applies to
  `OMBC.SystemDescription` (Phase 2) with the commodities of the `ResourceManagerDetails` roles unknown at
  message level, so OMBC only checks "at most one per CommodityQuantity".
* `PPBC_PowerSequenceContainer`: sequence ids unique within the container.
* `operation_mode_factor` ∈ [0, 1] (messages, Phase 2).
* Timestamps: `valid_until >= valid_from` (messages, Phase 2).

Every rule has (a) a constructor test (`Throws.ArgumentException`) and (b) a `TryParse` test that
the error response mentions the rule.


## 6. Tests

* One test fixture per type or per small group of types, in `WWCP_S2Tests/DataStructures/<Group>/`.
* For every type: (1) build an instance with all properties (use the values of the S2 documentation
  examples where they exist: EV charger FRBC, PV PEBC), call `ToJSON()`, assert
  `S2SchemaValidator.AssertValidType(json, "<schema name>")`, parse it back with `TryParse` and
  assert equality; (2) an instance with only mandatory properties, assert the optional JSON keys are
  absent and the JSON is schema valid; (3) a missing mandatory property makes `TryParse` fail with
  the property name in the error; (4) `S2ParserOptions.Strict` rejects an additional property;
  (5) one negative test per semantic rule of §5.
* Use `JObject.Parse("""…""")` raw string literals for hand-written JSON.
* NUnit 4 constraint syntax (`Assert.That(x, Is.EqualTo(y))`), no classic asserts.
* Do not run `dotnet build`/`dotnet test` concurrently with other writers of this repository;
  the integration step builds and runs everything.


## 7. API contract of the Phase 1 types (constructor signatures)

Mandatory parameters first in schema order, then optional parameters with `= null`.

### Common
```
PowerRange(Double StartOfRange, Double EndOfRange, CommodityQuantity CommodityQuantity)
PowerValue(CommodityQuantity CommodityQuantity, Double Value)
PowerForecastValue(Double ValueExpected, CommodityQuantity CommodityQuantity,
                   Double? ValueUpperLimit = null, Double? ValueUpper95PPR = null, Double? ValueUpper68PPR = null,
                   Double? ValueLower68PPR = null, Double? ValueLower95PPR = null, Double? ValueLowerLimit = null)
PowerForecastElement(Duration Duration, IReadOnlyList<PowerForecastValue> PowerValues)                      // 1..10
Role(RoleType RoleType, Commodity Commodity)                                                                  // JSON "role", "commodity"
Transition(Transition_Id Id, OperationMode_Id From, OperationMode_Id To,
           IReadOnlyList<Timer_Id> StartTimers, IReadOnlyList<Timer_Id> BlockingTimers,                    // 0..1000 each
           Boolean AbnormalConditionOnly, Double? TransitionCosts = null, Duration? TransitionDuration = null)
Timer(Timer_Id Id, Duration Duration, String? DiagnosticLabel = null)                                        // exists
NumberRange(Double StartOfRange, Double EndOfRange)                                                          // exists
```

### PEBC
```
PEBC_AllowedLimitRange(CommodityQuantity CommodityQuantity, PEBC_PowerEnvelopeLimitType LimitType,
                       NumberRange RangeBoundary, Boolean AbnormalConditionOnly)
PEBC_PowerEnvelopeElement(Duration Duration, Double UpperLimit, Double LowerLimit)
PEBC_PowerEnvelope(PowerEnvelope_Id Id, CommodityQuantity CommodityQuantity,
                   IReadOnlyList<PEBC_PowerEnvelopeElement> PowerEnvelopeElements)                          // 1..288
```

### PPBC
```
PPBC_PowerSequenceElement(Duration Duration, IReadOnlyList<PowerForecastValue> PowerValues)                 // 1..10
PPBC_PowerSequence(PowerSequence_Id Id, IReadOnlyList<PPBC_PowerSequenceElement> Elements,                  // 1..288
                   Boolean IsInterruptible, Boolean AbnormalConditionOnly, Duration? MaxPauseBefore = null)
PPBC_PowerSequenceContainer(PowerSequenceContainer_Id Id, IReadOnlyList<PPBC_PowerSequence> PowerSequences) // 1..288
PPBC_PowerSequenceContainerStatus(PowerProfileDefinition_Id PowerProfileId, PowerSequenceContainer_Id SequenceContainerId,
                                  PPBC_PowerSequenceStatus Status, PowerSequence_Id? SelectedSequenceId = null,
                                  Duration? Progress = null)
```

### OMBC
```
OMBC_OperationMode(OperationMode_Id Id, IReadOnlyList<PowerRange> PowerRanges, Boolean AbnormalConditionOnly,   // 1..10
                   String? DiagnosticLabel = null, NumberRange? RunningCosts = null)
```

### FRBC
```
FRBC_OperationModeElement(NumberRange FillLevelRange, NumberRange FillRate, IReadOnlyList<PowerRange> PowerRanges,   // 1..10
                          NumberRange? RunningCosts = null)
FRBC_OperationMode(OperationMode_Id Id, IReadOnlyList<FRBC_OperationModeElement> Elements,                  // 1..100
                   Boolean AbnormalConditionOnly, String? DiagnosticLabel = null)
FRBC_ActuatorDescription(Actuator_Id Id, IReadOnlyList<Commodity> SupportedCommodities,                     // 1..4
                         IReadOnlyList<FRBC_OperationMode> OperationModes,                                  // 1..100
                         IReadOnlyList<Transition> Transitions, IReadOnlyList<Timer> Timers,                // 0..1000 each
                         String? DiagnosticLabel = null)
FRBC_FillLevelTargetProfileElement(Duration Duration, NumberRange FillLevelRange)
FRBC_LeakageBehaviourElement(NumberRange FillLevelRange, Double LeakageRate)
FRBC_StorageDescription(Boolean ProvidesLeakageBehaviour, Boolean ProvidesFillLevelTargetProfile,
                        Boolean ProvidesUsageForecast, NumberRange FillLevelRange,
                        String? DiagnosticLabel = null, String? FillLevelLabel = null)
FRBC_UsageForecastElement(Duration Duration, Double UsageRateExpected,
                          Double? UsageRateUpperLimit = null, Double? UsageRateUpper95PPR = null, Double? UsageRateUpper68PPR = null,
                          Double? UsageRateLower68PPR = null, Double? UsageRateLower95PPR = null, Double? UsageRateLowerLimit = null)
```

### DDBC
```
DDBC_OperationMode(OperationMode_Id Id, IReadOnlyList<PowerRange> PowerRanges, NumberRange SupplyRange,     // JSON key "Id"!
                   Boolean AbnormalConditionOnly, String? DiagnosticLabel = null, NumberRange? RunningCosts = null)
DDBC_ActuatorDescription(Actuator_Id Id, IReadOnlyList<Commodity> SupportedCommodities,                     // JSON key "supported_commodites"!
                         IReadOnlyList<DDBC_OperationMode> OperationModes, IReadOnlyList<Transition> Transitions,
                         IReadOnlyList<Timer> Timers, String? DiagnosticLabel = null)
DDBC_AverageDemandRateForecastElement(Duration Duration, Double DemandRateExpected,
                          Double? DemandRateUpperLimit = null, Double? DemandRateUpper95PPR = null, Double? DemandRateUpper68PPR = null,
                          Double? DemandRateLower68PPR = null, Double? DemandRateLower95PPR = null, Double? DemandRateLowerLimit = null)
```

### CommodityQuantity → Commodity
`CommodityQuantity` gets a `Commodity` property mapped by the family prefix of the wire value
(`ELECTRIC.` → `Commodity.Electricity`, `NATURAL_GAS.` and `HYDROGEN.` → `Commodity.Gas`, `HEAT.` → `Commodity.Heat`,
`OIL.` → `Commodity.Oil`), so that a future quantity of a known family still maps; values of an unknown family → `null`.


## 8. Messages (Phase 2)

Templates: `WWCP_S2/Messages/Common/ReceptionStatus.cs` (the only message without a message_id),
`WWCP_S2/Messages/Common/Handshake.cs` (optional array property), `WWCP_S2/Messages/Common/SelectControlType.cs`
(minimal message), base classes in `WWCP_S2/Messages/IS2Message.cs` and `WWCP_S2/Messages/AS2Message.cs`,
the dispatcher in `WWCP_S2/Messages/S2MessageParser.cs`.

* Every message except ReceptionStatus derives from `AS2Message` (which owns `MessageId`) and declares
  `public const String MessageTypeName = "<message_type const of the schema>"` plus
  `public override String MessageType => MessageTypeName;`.
* Constructor: schema properties first (mandatory, then optional with `= null`), and `Message_Id? MessageId = null`
  as the LAST parameter (a new UUID v7 is generated when omitted).
* `TryParse` (three overloads as in §3) starts with `TryParseHeader(JSON, MessageTypeName, Options, out var messageId, out ErrorResponse)`,
  then one region per schema property, then `CheckAdditionalProperties` listing `"message_type"`, `"message_id"` and all keys.
* `ToJSON()` is `public override JObject ToJSON() => ToJSON(null);` plus
  `public JObject ToJSON(CustomJObjectSerializerDelegate<T>? CustomTSerializer)` using `CreateJSON(…)` (which writes
  `message_type` and `message_id` first; pass the remaining properties in schema order, `null` for unset optionals).
* `Equals` includes `MessageId`. `ToString()` ends with ` [{MessageId}]`.
* Interfaces: the 13 revokable messages implement `IRevokable` (`RevokableObjectType`, `RevokableObjectId` = own `Id`
  for instructions/constraints/profile definitions, `MessageId` for `OMBC/FRBC/DDBC.SystemDescription`); the seven
  instruction messages implement `IInstruction` (`Id`, `ExecutionTime`, `AbnormalCondition`).
* Timestamps (`format: date-time`) are `DateTimeOffset`, parsed with `ParseMandatoryS2Timestamp`/`ParseOptionalS2Timestamp`
  and serialised with `ToS2Timestamp()`.
* Message-level semantic rules (validated in the constructor): `operation_mode_factor` ∈ [0, 1]; `valid_until >= valid_from`
  (PEBC.PowerConstraints when present, PEBC.EnergyConstraint); `end_time >= start_time` (PPBC.PowerProfileDefinition);
  `PEBC.PowerConstraints.allowed_limit_ranges` ≥ 2 with at least one UPPER_LIMIT and one LOWER_LIMIT per CommodityQuantity;
  `PEBC.EnergyConstraint.lower_average_power <= upper_average_power`; at most one `PEBC.PowerEnvelope` per CommodityQuantity
  in `PEBC.Instruction`; at most one `PowerValue` per CommodityQuantity in `PowerMeasurement`; `FRBC.LeakageBehaviour.elements`
  contiguous; `PPBC.PowerProfileDefinition` container ids unique; `OMBC.SystemDescription` unique operation mode / transition /
  timer ids, transitions reference declared operation modes and timers; `*.SystemDescription.actuators` ids unique;
  `ResourceManagerDetails.available_control_types` must not contain NO_SELECTION.
* Tests per message: round-trip + `S2SchemaValidator.AssertValidMessage(json, "<message_type>")`, the documentation
  examples where they exist (EV example: Handshake, HandshakeResponse, ResourceManagerDetails, SelectControlType,
  FRBC.SystemDescription, PowerMeasurement, FRBC.ActuatorStatus, FRBC.StorageStatus, FRBC.Instruction,
  InstructionStatusUpdate, SessionRequest), missing mandatory property, strict additional property, one negative test per
  semantic rule, and `S2MessageParser.TryParse` returning the concrete type.
* Register every message in `S2MessageParser` (`[T.MessageTypeName] = Wrap<T>(T.TryParse)`), grouped by control type.


## 9. API contract of the messages (constructor signatures)

Schema properties first (mandatory in schema order, then optional with `= null`), `Message_Id? MessageId = null` last.
Implemented interfaces are noted; `IRevokable.RevokableObjectId` is `S2Object_Id.From(Id)` or, for the system
descriptions, `S2Object_Id.From(MessageId)`.

### Common
```
Handshake(EnergyManagementRole Role, IEnumerable<String>? SupportedProtocolVersions = null, Message_Id? MessageId = null)   // exists
HandshakeResponse(String SelectedProtocolVersion, Message_Id? MessageId = null)
ReceptionStatus(Message_Id SubjectMessageId, ReceptionStatusValue Status, String? DiagnosticLabel = null)                     // exists, no MessageId
ResourceManagerDetails(Resource_Id ResourceId, IReadOnlyList<Role> Roles, Duration InstructionProcessingDelay,               // roles 1..3
                       IReadOnlyList<ControlType> AvailableControlTypes, Boolean ProvidesForecast,                            // control types 1..5, never NO_SELECTION
                       IReadOnlyList<CommodityQuantity> ProvidesPowerMeasurementTypes,                                        // 1..10
                       String? Name = null, String? Manufacturer = null, String? Model = null, String? SerialNumber = null,
                       String? FirmwareVersion = null, Currency? Currency = null, Message_Id? MessageId = null)
SelectControlType(ControlType ControlType, Message_Id? MessageId = null)                                                       // exists
SessionRequest(SessionRequestType Request, String? DiagnosticLabel = null, Message_Id? MessageId = null)
PowerMeasurement(DateTimeOffset MeasurementTimestamp, IReadOnlyList<PowerValue> Values, Message_Id? MessageId = null)          // 1..10, one per quantity
PowerForecast(DateTimeOffset StartTime, IReadOnlyList<PowerForecastElement> Elements, Message_Id? MessageId = null)            // 1..288
InstructionStatusUpdate(Instruction_Id InstructionId, InstructionStatus StatusType, DateTimeOffset Timestamp, Message_Id? MessageId = null)
RevokeObject(RevokableObject ObjectType, S2Object_Id ObjectId, Message_Id? MessageId = null)
```

### PEBC
```
PEBC_PowerConstraints(PowerConstraints_Id Id, DateTimeOffset ValidFrom, PEBC_PowerEnvelopeConsequenceType ConsequenceType,   : IRevokable
                      IReadOnlyList<PEBC_AllowedLimitRange> AllowedLimitRanges,                                               // 2..100, at least one UPPER and one LOWER per quantity
                      DateTimeOffset? ValidUntil = null, Message_Id? MessageId = null)                                       // ValidUntil >= ValidFrom
PEBC_EnergyConstraint(EnergyConstraint_Id Id, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil,                            : IRevokable
                      Double UpperAveragePower, Double LowerAveragePower, CommodityQuantity CommodityQuantity,                 // lower <= upper, ValidUntil >= ValidFrom
                      Message_Id? MessageId = null)
PEBC_Instruction(Instruction_Id Id, DateTimeOffset ExecutionTime, Boolean AbnormalCondition,                                  : IInstruction
                 PowerConstraints_Id PowerConstraintsId, IReadOnlyList<PEBC_PowerEnvelope> PowerEnvelopes,                    // 1..10, one per quantity
                 Message_Id? MessageId = null)
```

### PPBC
```
PPBC_PowerProfileDefinition(PowerProfileDefinition_Id Id, DateTimeOffset StartTime, DateTimeOffset EndTime,                    : IRevokable
                            IReadOnlyList<PPBC_PowerSequenceContainer> PowerSequencesContainers,                              // 1..1000, unique ids; JSON key "power_sequences_containers"
                            Message_Id? MessageId = null)                                                                     // EndTime >= StartTime
PPBC_PowerProfileStatus(IReadOnlyList<PPBC_PowerSequenceContainerStatus> SequenceContainerStatus, Message_Id? MessageId = null)  // 1..1000
PPBC_ScheduleInstruction(Instruction_Id Id, PowerProfileDefinition_Id PowerProfileId, PowerSequenceContainer_Id SequenceContainerId,   : IInstruction
                         PowerSequence_Id PowerSequenceId, DateTimeOffset ExecutionTime, Boolean AbnormalCondition, Message_Id? MessageId = null)
PPBC_StartInterruptionInstruction(same parameters as PPBC_ScheduleInstruction)                                                : IInstruction
PPBC_EndInterruptionInstruction(same parameters as PPBC_ScheduleInstruction)                                                  : IInstruction
```

### OMBC
```
OMBC_SystemDescription(DateTimeOffset ValidFrom, IReadOnlyList<OMBC_OperationMode> OperationModes,                            : IRevokable (by MessageId)
                       IReadOnlyList<Transition> Transitions, IReadOnlyList<Timer> Timers, Message_Id? MessageId = null)      // 1..100 / 0..1000 / 0..1000, unique ids, references
OMBC_Status(OperationMode_Id ActiveOperationModeId, Double OperationModeFactor,                                               // factor in [0,1]
            OperationMode_Id? PreviousOperationModeId = null, DateTimeOffset? TransitionTimestamp = null, Message_Id? MessageId = null)
OMBC_Instruction(Instruction_Id Id, DateTimeOffset ExecutionTime, OperationMode_Id OperationModeId, Double OperationModeFactor,  : IInstruction
                 Boolean AbnormalCondition, Message_Id? MessageId = null)                                                     // JSON key "operation_mode_id"
OMBC_TimerStatus(Timer_Id TimerId, DateTimeOffset FinishedAt, Message_Id? MessageId = null)
```

### FRBC
```
FRBC_SystemDescription(DateTimeOffset ValidFrom, IReadOnlyList<FRBC_ActuatorDescription> Actuators,                           : IRevokable (by MessageId)
                       FRBC_StorageDescription Storage, Message_Id? MessageId = null)                                         // actuators 1..10, unique ids
FRBC_ActuatorStatus(Actuator_Id ActuatorId, OperationMode_Id ActiveOperationModeId, Double OperationModeFactor,
                    OperationMode_Id? PreviousOperationModeId = null, DateTimeOffset? TransitionTimestamp = null, Message_Id? MessageId = null)
FRBC_StorageStatus(Double PresentFillLevel, Message_Id? MessageId = null)
FRBC_Instruction(Instruction_Id Id, Actuator_Id ActuatorId, OperationMode_Id OperationMode, Double OperationModeFactor,          : IInstruction
                 DateTimeOffset ExecutionTime, Boolean AbnormalCondition, Message_Id? MessageId = null)                       // JSON key "operation_mode"
FRBC_TimerStatus(Timer_Id TimerId, Actuator_Id ActuatorId, DateTimeOffset FinishedAt, Message_Id? MessageId = null)
FRBC_FillLevelTargetProfile(DateTimeOffset StartTime, IReadOnlyList<FRBC_FillLevelTargetProfileElement> Elements, Message_Id? MessageId = null)   // 1..288
FRBC_LeakageBehaviour(DateTimeOffset ValidFrom, IReadOnlyList<FRBC_LeakageBehaviourElement> Elements, Message_Id? MessageId = null)               // 1..288, contiguous
FRBC_UsageForecast(DateTimeOffset StartTime, IReadOnlyList<FRBC_UsageForecastElement> Elements, Message_Id? MessageId = null)                     // 1..288
```

### DDBC
```
DDBC_SystemDescription(DateTimeOffset ValidFrom, IReadOnlyList<DDBC_ActuatorDescription> Actuators,                           : IRevokable (by MessageId)
                       Boolean ProvidesAverageDemandRateForecast, Message_Id? MessageId = null)                               // actuators 1..10, unique ids
DDBC_ActuatorStatus(Actuator_Id ActuatorId, OperationMode_Id ActiveOperationModeId, Double OperationModeFactor,
                    OperationMode_Id? PreviousOperationModeId = null, DateTimeOffset? TransitionTimestamp = null, Message_Id? MessageId = null)
DDBC_Instruction(Instruction_Id Id, DateTimeOffset ExecutionTime, Boolean AbnormalCondition, Actuator_Id ActuatorId,          : IInstruction
                 OperationMode_Id OperationModeId, Double OperationModeFactor, Message_Id? MessageId = null)                  // JSON key "operation_mode_id"
DDBC_TimerStatus(Timer_Id TimerId, Actuator_Id ActuatorId, DateTimeOffset FinishedAt, Message_Id? MessageId = null)
DDBC_PresentDemandStatus(NumberRange PresentDemandRate, Message_Id? MessageId = null)
DDBC_AverageDemandRateForecast(DateTimeOffset StartTime, IReadOnlyList<DDBC_AverageDemandRateForecastElement> Elements, Message_Id? MessageId = null)   // 1..288
```


## 10. S2 Connect data types (Phase 5)

Namespace `cloud.charging.open.protocols.S2.Connect`, folders `Connect/DataStructures/{Ids,Enums,Tokens,Common,Pairing,SessionInitiation,Registry}`
and `Connect/Crypto`. Templates: `Connect/DataStructures/Common/NodeDescription.cs` (DTO), `Connect/DataStructures/Tokens/AccessToken.cs`
(Base64 secret struct; `CommunicationToken`, `HmacChallenge`, `HmacChallengeResponse` are generated from it), `Connect/DataStructures/Ids/Node_Id.cs`
(UUID identifier), the enumerations are generated from `DataStructures/Enums/ControlType.cs` with inline value lists.

* JSON keys are the exact OpenAPI property names (camelCase: `clientNodeDescription`, `supportedS2MessageVersions`, `websocketUrl`);
  the `#region Documentation` quotes the OpenAPI schema. The same three-overload `TryParse` contract, `CheckAdditionalProperties`
  and `ToJSON` rules as in §3/§4 apply; array bodies (`waitForPairing`) parse a `JArray` and serialise to a `JArray`.
* Secrets (`PairingToken`, `PairingCode`, `PairingAttemptId`, `AccessToken`, `CommunicationToken`, `HmacChallenge`,
  `HmacChallengeResponse`) keep the wire text verbatim in `Value`, compare in constant time (`ConstantTimeEquals`, also used by
  `Equals`), and redact `ToString()`; they are generated only through the CSPRNG factories (`NewRandom`, `NewDynamic`,
  `NewStatic`, `TokenGenerator`). `format: byte` fields are standard Base64 with padding; Base64Url is rejected.
* Identifiers are never trimmed or normalised except `Node_Id`/`EndpointRecord_Id` (UUIDs, written lower-case) and
  `CertificateFingerprint` (hexadecimal with or without colons, compared by value, written upper-case with colons).
* `S2BaseURL` enforces the URL rules of the specification (https, trailing slash, no API version, no query/fragment) and derives
  `ForVersion("v1")` and `Operation("v1", "requestPairing")`. Insecure schemes (`http://`, `ws://`) are accepted only with
  `S2ParserOptions.AllowInsecureURLs` or the explicit `AllowHTTP` parameter (tests and development).
* `NodeDescription.role` reuses `EnergyManagementRole` of S2 JSON (identical wire values `CEM`/`RM`).
* `ChallengeResponse` implements `R = HMAC(C, T || F)` (LAN, F = SHA-256 fingerprint bytes of the server's leaf certificate) and
  `R = HMAC(C, T || D)` (WAN, D = normalised ASCII/IDNA domain name); the challenge is the HMAC key, the token is ASCII;
  the known-answer vectors in `WWCP_S2Tests/Connect/ConnectTypeTests.cs` were computed independently with Python's `hmac`.
* `PairingTarget` models the requestPairing rule "nodeId and nodeIdAlias may never be used at the same time";
  `ConnectionDetails.CertificateFingerprints` must contain `SHA256` when present (the typo key `SHA265` of the OpenAPI
  description is accepted on input, never written).

## 11. S2 Connect pairing server (Phase 6)

Folders `Connect/Pairing`, `Connect/Store`, `Connect/Security`; everything stays in the single namespace
`cloud.charging.open.protocols.S2.Connect` (a sub-namespace `Pairing` would clash with the domain type `Pairing`).

* **Layering of a server operation.** Every operation exists twice: a transport independent method on `PairingServerAPI`
  (`RequestPairingAsync`, `RequestConnectionDetailsAsync`, `PostConnectionDetailsAsync`, `FinalizePairingAsync`,
  `GetEndpoint`, `GetNodes`, `PreparePairingAsync`, `CancelPreparePairingAsync`, `WaitForPairingAsync`) that takes parsed
  DTOs and returns a `PairingServerResult` (Hermod `HTTPStatusCode` + optional `PairingResponseErrorMessage` + `RetryAfter`),
  and a private HTTP handler that only extracts the bearer token, reads and parses the JSON body and serialises the result.
  Handler order: bearer present and active attempt (else 401) → body parse (else 400 `ParsingError`) → protocol logic.
  Every JSON response carries `Cache-Control: no-store`; 401 carries `WWW-Authenticate: Bearer`; 503 carries `Retry-After`.
* **Routes are relative to the API root** (`HTTPPath.Root` for the version index, `/v1/requestPairing`, …); the root path
  always ends with a slash because Hermod derives the API-relative path from it. The API is mounted on a host supplied
  `HTTPServer` (shared port and TLS certificate); tests use plain HTTP with `S2ParserOptions.AllowInsecureURLs`.
* **The hosted side** is modelled by `LocalEndpoint` (description, deployment, pairing URL, optional session initiation
  URL, `IsWANPairingServerForLANEndpoint`, certificate fingerprint delegate, nodes) and `HostedNode` (description, alias,
  supported versions/protocols, readiness, one *own* pairing token (dynamic with expiry or static) and any number of
  *entered* tokens for remote nodes). `HostedNode.TryResolvePairingToken` prefers an entered token (the node is the
  Initiator) over the own token (Responder). After a successful pairing `ConsumePairingToken` forgets the entered token
  and clears a dynamic own token; static tokens stay.
* **Attempt semantics.** `PairingAttempt` is created after the mandatory per-node delay; the 15 s window starts with the
  generation of the `pairingAttemptId`. The branch (A `requestConnectionDetails` / B `postConnectionDetails`) follows
  `CommunicationRoleExtensions.Determine(serverRole, serverDeployment, clientDeployment)`. Wrong branch, wrong order and
  `finalizePairing(success=true)` before the connection details answer 400 `Other` and fail the attempt
  (`PairingFailure.InvalidInteraction`); a wrong `serverHmacChallengeResponse` answers 403 (`InvalidChallengeResponse`);
  identical duplicate requests of every step are replayed with the earlier result (no second token); a missing `success`
  answers 400 `ParsingError` without failing the attempt. Expired attempts fail with `Timeout` on the next lookup or purge.
  `OnPairingAttemptCompleted` delivers every outcome; `PairingAttempt.ToJSON()` is the audit record (hashed id, no secret).
* **Persistence.** `finalizePairing(success=true)` builds a `Pairing` (local node, remote descriptions, local
  communication role, access token, session initiation URL and pinned fingerprints for communication clients) and stores it
  with `IS2Store.AddOrReplacePairingAsync`; a throwing store answers 500 (`StoreFailure`). `OnPairingCompleted` carries the
  replaced pairing of the same pair and, for RM nodes, the superseded pairings with other CEMs that the node layer must
  unpair. `InMemoryS2Store` is the reference; every store must pass `S2StoreContractTests<TStore>`.
* **Rate limiting.** `PairingRateLimiter`: one lease per targeted node, at most `MaxQueuedPairingAttemptsPerNode` waiting
  requests (503 + `Retry-After` beyond), parallel across nodes; the delay applies to every answered check of step 1.
* **LAN-only operations** exist only when `LANOperationsEnabled` (default: LAN endpoints that are not WAN pairing servers
  of LAN endpoints); WAN endpoints answer 404. Subnet decisions come from `ISubnetPolicy` (`SubnetCheck` by default:
  loopback, IPv4-mapped IPv6 unmapping, prefix comparison per interface, link-local scope rule; `AllowAll`/`DenyAll` for
  tests and proxies); `X-Forwarded-For` is never consulted. Unknown `serverNodeId` in `preparePairing` answers 204 as the
  OpenAPI file prescribes (the prose table's 400 `NodeNotFound` is not used). `LongPollingServer` answers within
  `LongPollingTimeout` (≤ 25 s), delivers at most one action per client node and response, asks new client nodes for
  their descriptions automatically, and answers 503 while shutting down or beyond `MaxHangingLongPollingRequests`.
* **Hermod specifics learnt in Phase 6.** Hermod omits the `Content-Length` header when a response has no content, so
  every bodiless non-204 response sets an empty content (`Content = []`), otherwise clients wait for the connection to
  close; an early answer drains the unread request body (`TryReadHTTPBodyStream`) to keep keep-alive connections in
  sync; `HTTPAPI` registers its own `GET {root}serviceCheck` route on every API; `Hermod.IPAddress` (a static helper
  class) shadows `System.Net.IPAddress` and needs an alias.

## 12. S2 Connect pairing client and long-polling client (Phase 7)

* **`PairingClient : AHTTPClient`** (Hermod) targets one pairing URL; requests use `RunRequest` with relative operation
  paths (`{pairingPath}v1/requestPairing`), JSON bodies, bearer `pairingAttemptId` and `MaxNumberOfRetries = 1`
  (the client never lets Hermod repeat a POST; 503 retries are its own, bounded by `MaxServiceUnavailableRetries`).
  Transport failures, a changed server certificate and the attempt deadline are *synthetic* results, never exceptions;
  only cancellation throws. Every operation returns `PairingClientResult` (`PairingClientOutcome` + operation, status,
  error message, `Retryable`) or `ClientOperationResult<T>`.
* **Client checks of the specification** are explicit failure edges: unparseable response → `InvalidResponse` without
  `finalizePairing`; schema violation with a usable `pairingAttemptId`, same role, algorithm not offered, missing
  server deployment, wrong `clientHmacChallengeResponse` (`ChallengeResponseMismatch`), unparseable connection details
  and local configuration gaps (`InvalidConfiguration`) → best-effort `finalizePairing(success = false)`; 400 →
  `Rejected` (+ `PairingResponseErrorMessage`), 401 → `Unauthorized`, 403 → `Forbidden`, 503 after the retries →
  `ServiceUnavailable`. The client deadline (15 s) starts when the 200 of `requestPairing` is parsed; `requestPairing`
  has its own timeout because of the server's mandatory delay; a response after the deadline, or a request that was cut to the
  remaining attempt time and timed out, counts as `Timeout`.
* **Formula selection on the client** follows `PairingServerDeployment` (explicit or `GuessDeployment`: `.local`,
  `localhost` and IP literals are LAN): LAN uses `F` = the fingerprint of the TLS server certificate observed by the
  certificate validator (or `AssumedServerCertificateFingerprint` for plain HTTP), WAN uses `D` = the normalised host of
  the pairing URL. A changed fingerprint between two requests of an attempt ends it with `CertificateChanged`.
  Self-signed chains are accepted during pairing when `AcceptSelfSignedCertificates` (default) and only chain errors
  are reported; the validity period is still checked; pinning of the CA after step 10 belongs to Phase 11.
* **Communication roles on the client**: `Determine(localRole, localDeployment, serverDeployment)`; a communication
  client requests the connection details (branch A), a communication server posts its own (`LocalEndpoint.SessionInitiationUrl`,
  a fresh access token, `LocalEndpoint.CACertificateFingerprint` under `SHA256`). The stored `Pairing` mirrors the
  server side (same token, URL and fingerprints on the communication client). `HostedNode.ConsumePairingToken` runs
  after success on both sides.
* **`LongPollingClient`** owns one polling loop per pairing client, always lists every hosted node, sends descriptions
  once after `sendNodeDescription`, reports `NoValidTokenOnPairingClient` once when asked to pair without a token, starts
  automatic pairing attempts with the node's own token (`AutoPair`), and follows the status table: 204 poll again,
  400 stop (`NotAvailable`), 401 stop (`Unauthorized`), 404 stop (`NotImplemented`), 500 and transport failures wait
  `ServerErrorDelay`, 503 waits `ServiceUnavailableDelay`. `RequestTimeout` must be at least 30 s.

## 13. S2 Connect session initiation, unpairing and reconnection (Phase 8)

* **Store operations for token rotation** (`IS2Store`): `AddPendingAccessTokenAsync`, `GetPendingAccessTokensAsync`,
  `FindPendingAccessTokenAsync` (the bearer of `confirmAccessToken`), `ActivateAccessTokenAsync` (pending → active and every
  other pending token of the pair removed, in one operation), `RemovePendingAccessTokensAsync`, `GetAccessTokenCandidatesAsync`
  (active first, then pendings newest first), `UnpairAsync` (pairing + pendings removed, tombstone written only when the pair was
  paired) and `GetUnpairedAtAsync`; `AddOrReplacePairingAsync` clears a tombstone. `PendingAccessToken` also records the
  protocol and version selected in step 3, so step 7 answers with matching details. Every store passes the extended
  `S2StoreContractTests<TStore>`.
* **`SessionInitiationServerAPI : HTTPAPI`** is mounted at the path of `LocalEndpoint.SessionInitiationUrl` and shares the
  `CommunicationTokenStore` with the `S2WebSocketServer`. Check order of `initiateSession`: parse → tombstone
  (`NoLongerPaired`, before every 401 check) → pairing and node known, local node is the communication server, active token
  (constant time) → protocols → versions → readiness; then the pending token is persisted and the response built.
  `confirmAccessToken` accepts the pending token as bearer (≤ 15 s old), activates it in one store operation (500 with the
  pending token untouched when the store fails) and issues a communication token whose identity is an
  `S2ConnectSessionIdentity` (pairing, protocol, version) for the WebSocket server's `SessionOptionsFactory`.
  `unpair` accepts any candidate token of the pair; `UnpairLocallyAsync` is the server-initiated unpairing (the node layer
  then sends `SessionRequest RECONNECT` and closes the session).
* **`AS2ConnectClient`** is the shared base of `PairingClient` and `SessionInitiationClient`: version index and selection,
  `SendAsync` (synthetic results for transport failures, certificate changes and deadlines), `SendWithDeadlineAsync` (503
  retries, deadline mapping), JSON helpers, the TLS validator with fingerprint observation, and the construction parameters
  kept for clones.
* **`SessionInitiationClient`** fetches the version index before every procedure, tries the candidate tokens sequentially
  (401 → next), persists the pending token before `confirmAccessToken` and aborts with `StoreFailure` when that fails,
  activates the token only after the 200 of step 7, keeps an unconfirmed pending token as candidate for the next attempt,
  maps the client reactions of the specification to `SessionInitiationOutcome` (`NoLongerPaired` removes the local pairing
  with a tombstone unless `RemovePairingWhenNoLongerPaired` is off; `Unauthorized` = no candidate accepted; `RetryLater`
  for 400/500/503/unparseable responses), `ConnectAsync` opens the `S2WebSocketClient` with the received details and
  `UnpairAsync` removes the local material on 204 and on 401 (`AlreadyUnpaired`).
* **`ReconnectStrategy`** implements `delay_n = random(0, min(600 s, 2 s × 2^n))` with `RandomNumberGenerator`;
  **`ReconnectingSessionClient`** reconnects only through session initiation, resets the back-off after a successful
  connection, reconnects immediately on `SessionRequest RECONNECT`, with back-off on `TERMINATE` and after any other end of
  the session, and stops on `NoLongerPaired`, `Unauthorized` (no candidate token) and a missing pairing.

## 14. S2 Connect discovery (Phase 9)

* **Hermod carries the protocol, WWCP_S2 the S2 semantics.** Multicast DNS (RFC 6762) and the generic DNS-SD machinery
  (RFC 6763) live in Hermod (`Hermod/DNS/Multicast/`, a separate contribution in the Hermod working tree, tested there):
  `MulticastDNSMessage` (tolerant parser and serializer carrying the QU and cache-flush bits and TTL overrides),
  `IMulticastDNSTransport` with `UDPMulticastDNSTransport` (one socket per address family on port 5353, address reuse,
  multicast loopback, IP TTL 255, per-interface sends) and `InMemoryMulticastDNSNetwork` (synchronous, deterministic tests
  without sockets), `MulticastDNSResponder` (probing, announcing, answering with known-answer suppression, additional
  records, one multicast per record and second, legacy unicast, NSEC negative answers, goodbye packets, conflict detection
  with state `Conflict` instead of an automatic rename), `MulticastDNSClient : IDNSClient` (one-shot queries with cache,
  retransmission, QU bit and NSEC, `MulticastDNSBrowser` for DNS-SD instances with re-queries at 80 % of the TTL) and
  `HybridDNSClient` (`.local` and the link-local reverse zones via Multicast DNS, everything else via a unicast client).
  Hermod's `TXT` record became multi-string (`Strings`, `KeyValues`, `TryGetValue`, `FromKeyValues`, UTF-8 on the wire, the
  single-`Text` view stays for SPF) and `PTR`/`TXT` accept `DNSServiceName` owners (underscore labels).
* **S2 types in `Connect/Discovery`**: `S2DNSSDTXTRecord` (`txtver`/`txtvers` = 1, `e_name`, `e_logoUrl`, `pairingUrl`,
  `longpollingUrl`, at least one URL, every entry ≤ 255 bytes of UTF-8, unknown keys preserved, keys case-insensitive and
  the first occurrence wins), `S2ServiceAdvertisement` (host name ending with `.local`, port, TXT record, roles = DNS-SD
  subtypes `_cem`/`_rm`, optional explicit addresses; instance name = first label of the host name + `_s2connect._tcp.local`;
  `FromLocalEndpoint` derives host, port, TXT and roles from the pairing URL, the description and the hosted nodes;
  `ToResourceRecords` builds PTR (type and subtypes), SRV, TXT, A and AAAA with the RFC 6762 TTLs) and
  `S2DiscoveredEndpoint` (equal by instance name, `SameContentAs` for change detection, `HostsCEM`/`HostsRM` null when unknown).
* **The seam `IServiceDiscovery`**: `AdvertiseAsync` → `IServiceAdvertisementHandle` (`IsPublished`, `HasConflict`,
  `UpdateAsync` for a changed TXT record or roles, `WithdrawAsync`, `OnConflict`), `BrowseAsync(role)` → `IS2EndpointBrowser`
  (`Endpoints`, appeared/updated/disappeared/invalid events, `WaitForEndpointAsync`) and `DNSClient`, which every S2 Connect
  client and the WebSocket client must receive to reach `hostname.local` URLs. `DNSSDServiceDiscovery` is the production
  implementation (one transport shared by responder and client, hybrid DNS client, `OwnsTransport`); `InMemoryServiceDiscovery`
  is the in-process double (one shared instance per test, advertised hosts resolve to their explicit addresses or 127.0.0.1).
  Both pass `ServiceDiscoveryContractTests`.
* **Browsing** uses only the URLs of the TXT record, never the SRV host and port (the port of the SRV record is reported but
  not dialled). Without a role filter the browser combines the service type and both subtype browsers so that the roles of an
  endpoint become known; with a role filter only that subtype is browsed. Instances with invalid TXT records raise
  `OnInvalidEndpoint` and never become endpoints; `http://` URLs need `S2ParserOptions.AllowInsecureURLs` (tests only).
* **`EndpointAdvertiser`** advertises a LAN endpoint while a hosted node is `IsReadyForPairing` and has a valid own pairing
  token (the specification's "once it is ready for pairing, and until it shuts down"; `OnlyWhenReadyForPairing = false`
  advertises unconditionally), re-evaluates every `CheckInterval` or on `RefreshAsync`, updates the advertisement when the
  description or the roles change, withdraws on stop and after a name conflict, and never renames (a renamed LAN endpoint would
  also need a new pairing URL and certificate; `RefreshAsync(Force: true)` retries after a conflict).
* **WAN registry**: `WANRegistryQuery` (regions any-of, status default `public`, `cem`/`rm`, `limit` ≥ 1, `offset` ≥ 0;
  `TryParse` of a query string on the server, `ToQueryString` on the client, `Matches`/`Apply` for filtering and paging),
  `WANRegistryClient : AS2ConnectClient` (version index, `QueryEndpointsAsync`, `GetEndpointAsync`; 400 and 404 are failed
  results carrying the status code, never exceptions) and the reference `WANRegistryAPI : HTTPAPI` over `IWANRegistry` /
  `InMemoryWANRegistry` (400 with a JSON `message` for invalid parameters, 404 for unknown or malformed identifications).
* **Hermod specifics learnt in Phase 9.** Route templates (`/v1/endpoint/{id}`) deliver their parameters in
  `Request.ParsedURLParametersX["id"]` (the positional `ParsedURLParameters` array stays empty); `AS2ConnectClient.SendAsync`
  passes an optional `QueryString` through to `RunRequest`; Hermod's TCP client resolves every host through the `IDNSClient`
  given to the HTTP client, queries A and AAAA in parallel and forces a cache update on the first connection, so a Multicast
  DNS client must answer AAAA negatively (NSEC, otherwise every connection waits for the query timeout) and must never list
  records of the queried types as known answers (the responder would suppress the answer); two Multicast DNS sockets on the
  same port of one process both receive multicasts but only one of them receives a unicast, so such tests switch
  `RequestUnicastResponses` off.

## 15. S2 node layer and samples (Phase 10)

Folder `Node/` (namespace `cloud.charging.open.protocols.S2.Node`); the layering test allows the node layer to
reference every lower layer, and nothing references the node layer.

* **`AS2Node : IAsyncDisposable`** composes the S2 Connect pieces from the deployment and the role and owns their
  lifecycle. It builds a `LocalEndpoint` from `S2NodeOptions`, adds the single hosted node, and on `StartAsync`
  creates the HTTP server (its own, or a supplied one), a `PairingServerAPI` (always, so a peer can pair), and — for a
  communication server — a `CommunicationTokenStore`, an `S2WebSocketServer` and a `SessionInitiationServerAPI`; with a
  service discovery it starts an `EndpointAdvertiser` (LAN only) and, for a communication client, a
  `ReconnectingSessionClient` per stored pairing. Communication roles follow the specification: a WAN endpoint or a LAN
  CEM is the communication server, a LAN RM is the communication client (`EnableCommunicationServer`/`Client` override it).
* **Session establishment is automatic.** On a completed pairing (through the pairing server or `PairAsync`), a
  communication client starts a reconnecting session client to the pairing's `InitiateSessionUrl`; a communication server
  waits, and its WebSocket server's `OnSessionStarted` carries the `S2ConnectSessionIdentity`. Both paths produce one
  `S2NodeSession` (session, pairing, hosted node, and — for a client — the reconnecting client) tracked per remote node
  and announced through `OnSessionStarted`/`OnSessionEnded`. `OnSessionEstablishedAsync` is the subclass hook run before
  the event; session configurators added with `ConfigureEverySession` run for every session.
* **`StopAsync(drain)` stop order** (the specification's): withdraw the DNS-SD advertisement, stop accepting pairing
  (`PairingServerAPI.ShutdownAsync` → 503) and session initiation (`Shutdown`), close the sessions with a close frame
  within the drain (stopping the reconnecting clients first), cancel the node's linked token and stop the WebSocket and
  HTTP servers (only a node-owned HTTP server is stopped), then flush the store (`IFlushableS2Store`).
* **`PairAsync`** runs a `PairingClient` against a remote pairing URL (this node as pairing client); **`UnpairAsync`** is
  role-aware: a communication client closes its session and calls the remote `/unpair` (deleting the local material), a
  communication server unpairs locally, sends `SessionRequest RECONNECT` (whose re-initiation is answered with
  `NoLongerPaired`) and closes the session.
* **`RMNode`** (role RM, one CEM at a time) publishes its `ResourceManagerDetails` as soon as a session opens and offers
  the control types registered with `RegisterControlType`; **`CEMNode`** (role CEM, many RMs) receives the details, picks
  a control type with `SelectControlTypePolicy` (default: the first offered type the RM supports), sends the
  `SelectControlType`, and can `RevokeAsync` objects. Control types are `IS2ControlTypeHandler`s: `FRBCResourceManager`
  (RM side: sends the `FRBC_SystemDescription` on activation, forwards `FRBC_Instruction`s to `OnInstruction` and
  acknowledges them with `InstructionStatusUpdate` NEW) and `FRBCEnergyManager` (CEM side: raises the RM's system
  description, storage and actuator status and instruction updates, caching the last of each for the session).
* **`JSONFileS2Store : IFlushableS2Store`** reuses `InMemoryS2Store` for the logic (via an internal snapshot/load seam)
  and persists the whole state after every mutation through a temp file and an atomic `File.Move(overwrite)`. The file
  carries `formatVersion` (1) and `secretScheme`; access tokens pass through an `ISecretProtector` (default
  `PlaintextSecretProtector`, scheme id ""), so a deployment can encrypt them and a file protected by one scheme is not
  silently read by another. Both built-in stores pass `S2StoreContractTests<TStore>`.
* **Samples** (`WWCP_S2_Samples`, console): `EVChargerRM` (the FRBC worked example: off/charging modes, 1.4–11 kW,
  battery 0–100), `PVRM` (a PEBC RM skeleton — the node composition is complete, the PEBC control-type handler is left
  for a later phase), `MinimalCEM` (an FRBC energy manager), `PairingTool` (DNS-SD browsing) and `Program` with a `demo`
  command that runs a CEM and an EV charger end to end in-process (discovery → pairing → session → FRBC instruction →
  unpairing) and a `browse` command over real Multicast DNS. The README quick-start is the `RunDemoAsync` body between
  its `README quick-start` markers.

## 16. S2 Connect security hardening (Phase 11a)

Folder `Connect/Security` (beside `SubnetCheck`/`ISubnetPolicy` from Phase 6).

* **The D13 question was decided by measurement, not assumption.** `WWCP_S2Tests/Security/D13ChainSpikeTests.cs`
  starts a Hermod TLS server and reads `chain.ChainPolicy.ExtraStore` in the client's validation callback — the only
  place that shows what the peer actually transmitted. For a leaf signed by a private CA the store was **empty**:
  Hermod calls `SslStreamCertificateContext.Create(target: leaf, additionalCertificates: null)`, so the root never
  reaches the client. `ChainElements` did list the CA, but only because client and server shared a process and the
  platform certificate cache — never trust `ChainElements` for this question. Hence the D13 fallback: **a LAN endpoint
  presents one self-signed server certificate that is its own CA**, and that certificate's SHA-256 is what the
  `certificateFingerprint` map carries and the peer pins. Rotating it requires re-pairing.
* **`TLSProfiles`**: `Modern` = TLS 1.3, `Interoperable` = TLS 1.3 + 1.2 (the library default, because a LAN resource
  manager may be an embedded device), `ModernCipherSuites` = AEAD only. `ApplyModernCipherSuites(SslClientAuthenticationOptions)`
  sets the policy everywhere except on Windows, whose Schannel has none. It *applies* rather than returns the policy on
  purpose: `CipherSuitesPolicy` is unsupported on Windows, so naming it in a public signature makes every Windows caller
  trip CA1416. The internal guard is an inline `OperatingSystem.IsWindows()`, because the analyzer does not see through a
  helper property. A cipher policy is only ever applied to clients — Hermod's servers expose none.
* **`SelfSignedCA`** wraps Hermod's `PKIFactory`: `CreateSelfSignedServerCertificate(hostName, …)` is the shape S2
  Connect LAN endpoints use (its own CA, mDNS host name as SAN, 6-month default lifetime = the Phase 11a rotation
  interval); `CreateRootCA` and `IssueServerCertificate` build a real hierarchy for deployments that distribute the
  root out of band, and their leaves must never be pinned.
* **`CertificatePinStore`** maps a normalised domain name (via `ChallengeResponse.NormaliseDomainName`, the same
  normalisation the pairing challenge uses) to one or more SHA-256 fingerprints. Several pins per host are what makes a
  planned rotation possible: pin the new one, roll the server, drop the old one. Comparisons run in constant time and
  every pin is compared, so the runtime does not reveal which one matched. `ToJSON`/`TryParse` persist it.
* **`S2CertificateValidator`** replaces the operating system's judgement in a fixed order: no certificate fails; the
  validity period is always checked; a **pinned host must present a pinned certificate — a mismatch fails even when the
  operating system trusts the chain**, which is the whole point of pinning; an unpinned host may be system-trusted (a
  WAN endpoint); during a pairing (`AcceptUnpinnedForPairing`) an unpinned but self-signed certificate is accepted so
  that it can be pinned, and a CA-signed leaf is rejected with `NotSelfSigned` per D13. `IsSelfSigned` verifies the
  signature against the certificate's own key through a one-element custom trust chain, because `Verify()` consults a
  platform store that never holds a private root. Subject alternative names are read with
  `X509SubjectAlternativeNameExtension.EnumerateDnsNames()`, never with `AsnEncodedData.Format`, whose output is
  localised on Windows and may not decode this OID at all on Linux; the host name is normalised defensively, because
  `ChallengeResponse.NormaliseDomainName` rejects an IP literal or a host with a port and a TLS callback must not throw.
* **Wiring**: `AS2ConnectClient.CertificateValidator` is consulted for every S2 Connect HTTP client (an explicit
  `RemoteCertificateValidator` still wins). `AS2Node` owns a `CertificatePinStore`, gives its pairing client a validator
  with `AcceptUnpinnedForPairing: true`, pins the peer's `certificateFingerprint` map against the host of the received
  `initiateSessionUrl` when a pairing completes, and gives its session initiation clients a validator that enforces
  those pins. `S2NodeOptions.EnforceCertificatePinning` (default true) turns the whole mechanism off for deployments
  that are not ready for it.
* **`S2RequestRateLimiter`** is a per-remote-address token bucket (Hermod's `InMemoryTokenBucketRateLimiter`) in front
  of *every* route of the pairing and the session initiation server. It is denial-of-service protection and must not be
  confused with `PairingRateLimiter`, which implements the normative "one pairing attempt per node per second" and stays
  in force independently. Both APIs wrap their handlers in one place (`RateLimited(HTTPDelegate)` inside
  `RegisterURLTemplates`), so a request is refused **before** parsing, store access and cryptography — and so a new
  operation cannot be added without its rate limit. The number of buckets is bounded, so spoofed source addresses cannot
  turn the limiter into a memory exhaustion attack; beyond the bound unknown addresses are refused. The refusal is
  **503 with `Retry-After`** by default, because that is the only overload answer the specification defines (for
  `requestPairing` and `waitForPairing`) and every S2 Connect client already retries after it; `UseTooManyRequestsStatusCode`
  switches it to the semantically precise 429. Defaults: 300 requests/minute for pairing, 120 for session initiation
  and 300 for the WAN registry API, which answers the whole internet and is limited the same way. On the client
  side 429 is treated exactly like 503 (`AS2ConnectClient` retries it honouring `Retry-After`, the pairing and
  session initiation clients report it as "temporarily unavailable"), so a peer that answers the precise code
  does not break a client that expected the specified one. The bucket key is the peer of the TCP connection and a
  forwarded-for header is deliberately not trusted (anyone could set it and buy a fresh budget per request), so a
  deployment behind a reverse proxy either raises the capacity or rate limits at the proxy and disables this one.
  Two things the limiter cannot cover, by construction: an unregistered path and a wrong method on a registered path
  are answered by the HTTP server before any handler of the API runs. And `BucketLifetime` must never be shorter than
  `RefillPeriod` — a bucket forgotten before it has refilled hands out a fresh, full budget, so an address would reset
  its budget by pausing; the constructor refuses that combination.
* **Request size limits** exist at two levels. Hermod's `MaxHTTPBodySize` (set from `S2NodeOptions.MaxHTTPBodySize`,
  default 1 MiB) refuses an oversized body while reading it, before the API sees it; the APIs enforce their own, much
  smaller `MaxRequestBodySize` (default 64 KiB). The announced `Content-Length` is checked in the same wrapper as the
  rate limit, i.e. **before the handler authenticates or parses anything** — otherwise an unauthorized oversized request
  would be answered 401 and its body drained anyway, which is exactly what the limit exists to prevent; it is also what
  bounds `confirmAccessToken`, which reads no body at all. The received body is checked again inside `TryReadJSON`,
  because a chunked request announces no length. `Request.HTTPBody` can throw `HTTPBodyTooLargeException`, so the body
  access is wrapped and mapped to 413 rather than escaping the handler. Note what this bound also buys: an S2 identifier
  follows JSON Schema semantics and is therefore unanchored and unbounded free text (`S2_Id`), so the request size limit
  is the only thing that caps how long one can be - treat every identifier as untrusted text when logging or persisting it.
  A 413 is the one answer that does **not** drain
  the request body — reading it is what the refusal avoids — so it closes the connection, as Hermod's own 413 does. The
  WebSocket side is bounded by `MaxTextMessageSizeIn`/`Out` (`S2NodeOptions.MaxWebSocketMessageSize`, default 1 MiB) on
  the server and, via `SessionInitiationClientOptions.MaxWebSocketMessageSize`, on the client; an oversized message
  closes the connection with 1009.
* **`S2LogRedaction`** masks three shapes, because a secret reaches a log in all three: a secret JSON property, an
  HTTP `Authorization` header (the scheme is kept, it is diagnostic and not secret) and an unquoted `name=value` or
  `name: value` pair (log messages and query strings). It never throws — a `RegexMatchTimeoutException` returns the mask
  rather than the text, so a pathological input cannot leak by breaking the redaction. `RedactJSON` returns a masked deep
  copy and leaves the input alone. The overload is named `RedactJSON` and not `Redact`, so that `Redact(null)` is not
  ambiguous at the call site. `Fingerprint(secret)` is the truncated SHA-256 for audit records (PLAN.md §3.6).
* **`S2RedactingLogger`/`S2RedactingLoggerFactory`** decorate an `ILogger`/`ILoggerFactory`: both the formatted message
  and the *structured state* are redacted, because a JSON or OpenTelemetry sink renders the state and never the message.
  The message is **rendered from the redacted values**, not by running the already formatted text through the textual
  redaction: `LogInformation("token {Token} of {NodeId}", secret, id)` produces none of the three shapes, so the secret
  would survive in the message while the state of the very same entry was masked. Rendering replaces each placeholder
  name by its position (`{Count,5:N0}` → `{0,5:N0}`), so alignments and format specifiers survive; anything that cannot
  be rendered falls back to the redacted output of the original formatter. A plain text state is replaced by its
  redacted text; any other state object is forwarded unchanged, because replacing it would break every sink that casts
  it back to its own type.
  `{OriginalFormat}` is passed through unchanged (it is the template, which holds names, not values), and a value type is
  passed through untouched. `AS2Node` wraps the logger factory it is given (`S2NodeOptions.RedactSecretsInLogs`, default
  true), so every component it composes logs redacted. `WithS2Redaction()` never wraps twice. The one thing it cannot
  rewrite is a logged `Exception`: its message is read-only and wrapping it would destroy the type a sink filters on
  and the stack trace it prints — hence the rule that the library never puts request content into an exception it logs.
  The second rule the layer rests on: **every type that holds a secret redacts its own `ToString()`** — no redaction can
  mask a bare token logged without its property name, so a new secret-bearing type (struct or class) must redact itself. This is the second line of
  defence: every type holding a secret already redacts its own `ToString()` and the library logs no request bodies.


## 17. Documentation, public API and packaging (Phase 12)

* **`README.md` is the entry point and is written for someone who has not read `PLAN.md`**: what the library is, a
  quick start per role (RM, CEM, plain S2 JSON over WebSockets), the architecture with its layering rule, a feature
  matrix that says what is *not* implemented as clearly as what is, the deployment and port table, the security
  defaults, how to build and test, and the packaging caveat. Its code blocks are condensed from the samples — which are
  linked next to each of them and compiled by CI — and every signature in them is checked against the public API
  baseline below rather than written from memory. A changed constructor therefore has one place to look: the baseline
  diff tells whether a quick start went stale.
* **XML documentation is not optional and needs no separate check**: the library sets
  `GenerateDocumentationFile` and `TreatWarningsAsErrors`, so a public member without a `<summary>` is CS1591 and the
  build fails. The test and sample projects switch both off — test names document themselves.
* **The public API baseline** (`WWCP_S2Tests/Architecture/PublicAPI.baseline.txt`, generated and compared by
  `PublicAPITests`) records every public and protected member as text: type kinds, base types, interfaces, signatures.
  It is a review aid rather than a binary-compatibility checker (no nullability, attributes or default values), and its
  job is that **an API change appears as a diff in the commit that causes it** and has to be defended there. Regenerate
  it deliberately with `S2_UPDATE_PUBLIC_API=1` and read the diff before committing; a removal or a changed signature
  means the package version has to move accordingly. The baseline is embedded in the test assembly so the comparison
  also works from a copied output directory, and `Baseline_IsNotEmpty` guards the guard.
* **Packaging rule: no unpublished project may become a NuGet dependency.** Styx and Hermod are source siblings that
  nobody has published, and `dotnet pack` would otherwise invent
  `org.GraphDefined.Vanaheimr.Hermod 1.0.0` (does not exist) and `Styx 1.0.0` — an id that on nuget.org belongs to an
  unrelated library by another author, i.e. a package that fails to restore today and could pull a stranger's assembly
  tomorrow. Both references therefore carry `PrivateAssets="all"`, the CI `Package` step greps the generated `.nuspec`
  to make sure that stays true, and `build/cloud.charging.open.protocols.S2.targets` ships inside the package so a
  consuming build that lacks the two assemblies fails with `S2NUG001` and an instruction instead of a
  `FileNotFoundException` at run time. The price is that the references no longer flow transitively, which is why
  `WWCP_S2Tests` and `WWCP_S2_Samples` name Styx and Hermod themselves. Drop all of this once the two are published.
* The package carries `README.md`, `THIRD-PARTY-NOTICES.md`, the XML documentation and a `.snupkg` symbol package;
  `PublishRepositoryUrl` plus `EmbedUntrackedSources` give it SourceLink, and `Deterministic` with
  `ContinuousIntegrationBuild` (set when `GITHUB_ACTIONS` is) make a CI build of the same commit reproducible.
* **CI and nightly** mirror the Hermod and Styx workflows: the same two legs (`windows-latest` and Debian 13 in a
  `debian:13` container), `fail-fast: false`, TRX artefacts on `!cancelled()`. Two differences are deliberate. This
  gate *pins* Styx and Hermod (`STYX_REF`/`HERMOD_REF`), because this repository is downstream of Hermod work written
  for it and an unpinned gate would report another repository's change as this one's breakage — and because a pin can
  go stale unnoticed, the nightly builds the same code against both upstream `master`s and prints the drift. The
  nightly also runs the `Timing` category fatally and probes `Multicast` informationally, since whether two sockets in
  one process see each other's multicast is a property of the runner, not of this code.

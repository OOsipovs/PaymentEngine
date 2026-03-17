
## Key Decisions

### Alaways receive result approach on `PaymentService.Call()`
Real money is involved. The caller must always receive a structured result even if
something completely unexpected happens. Every outcome is captured in `PaymentResult`
rather than propagated as an exception.

### `UnexpectedResponse` status for anything undocumented
Provider docs are incomplete by design. Rather than mapping unknown responses to a
generic error, there is a dedicated status that signals *"we don't know what happened"*.
This is deliberately different from a network error (`ProviderError`) or a known decline.
In production this status would trigger an alert and a reconciliation job.

### Transport errors are treated as "unknown charge state"
If the HTTP call times out or the connection drops, we cannot know whether the provider
processed the charge. The log message says exactly this and recommends reconciliation.
We do **not** retry automatically — a retry on an unknown state risks double-charging.

### `IPaymentProvider` interface
Separates the HTTP transport from the business logic completely. Tests use `PaymentProviderStub`
— a simple field-settable stub — so every scenario is reproducible without HTTP infrastructure.

### Validation is a pure static function
`PaymentRequestValidator.Validate()` takes a request and returns a string or null.
No dependencies, no side effects, trivially testable.

Every log line includes `OrderId` and `MerchantId` so production logs can be filtered
per merchant or per order without additional tooling.

## What I Would Do Differently in Production

| Topic | What I'd add |
|---|---|
| **Persistence** | Record every attempt and outcome in a database before and after the provider call |
| **Retry policy** | Retry *only* on confirmed-not-processed errors (e.g. connection refused before the request left the machine), with exponential back-off |
| **3DS flow** | The `threeds_required` result hands back a URL — a real implementation would need a webhook / redirect flow to complete the charge |
| **Metrics** | Emit counters for approved / declined / unexpected per merchant for anomaly detection |
| **Secrets** | Bearer token via environment variable / secret manager, never hardcoded |
| **More currencies** | Drive the supported currency list from config, not a hardcoded set |

## Intentionally Skipped

- HTTP layer / controller (per task requirements)
- Persistent storage (in-memory / fake is sufficient here)
- 3DS completion flow (noted above)
- Retry logic (too risky without idempotency layer)
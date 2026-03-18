---

## Project Structure

### Core Engine (`PaymentEngine/`)

| File | Purpose |
|---|---|
| **`IPaymentService.cs`** | Public interface — the single entry point for charging a customer. Callers depend on this interface, never on the concrete implementation. |
| **`PaymentService.cs`** | Orchestrates a charge: validates the request, calls the provider, interprets the response, and logs every outcome. All business logic lives here. |
| **`Models/PaymentRequest.cs`** | Input data object — contains merchant ID, order ID, amount, currency, and card token. |
| **`Models/PaymentResult.cs`** | Output data object — structured result returned to the caller with status (Approved, Declined, ThreeDsRequired, etc.) and relevant fields. |
| **`Validation/PaymentRequestValidator.cs`** | Pure static function that validates `PaymentRequest`. Returns `null` if valid, or an error message string if invalid. No dependencies, no side effects. |

### Provider Integration (`PaymentEngine/Provider/`)

| File | Purpose |
|---|---|
| **`IPaymentProvider.cs`** | Interface abstracting the HTTP call to the external payment provider. Tests inject a fake implementation; production uses the real HTTP client. |
| **`PaymentProvider.cs`** | Real HTTP implementation — sends a `POST /charges` request to the provider API with Bearer auth, deserializes the response, and handles network failures. |
| **`ProviderChargeRequest.cs`** | JSON payload sent to the provider API (amount, currency, card_token). Uses `[JsonPropertyName]` to match the provider's snake_case contract. |
| **`ProviderChargeResponse.cs`** | JSON response from the provider. All fields are nullable to safely deserialize unknown or malformed shapes. |
| **`ProviderResult.cs`** | Internal wrapper around the HTTP result — captures status code, parsed response, raw body (for logging unexpected responses), and transport errors (network timeouts). |

### Tests (`PaymentEngine.Tests/`)

| File | Purpose |
|---|---|
| **`PaymentServiceTests.cs`** | Unit tests covering validation, approved/declined/3DS flows, undocumented provider responses, and network failures. No real HTTP — uses a stub. |
| **`PaymentProviderStub.cs`** | Test double for `IPaymentProvider`. Field-settable — tests configure the result before calling the service, making every scenario reproducible without HTTP infrastructure. |

---

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
| **Endpoint security** | Require authentication and authorization for the public endpoint enforce role-based access control, rate limits and input quotas|
| **Retry policy** | Retry *only* on confirmed-not-processed errors (e.g. connection refused before the request left the machine), with exponential back-off |
| **3DS flow** | The `threeds_required` result hands back a URL — a real implementation would need a webhook / redirect flow to complete the charge |
| **Secrets** | Bearer token via environment variable / secret manager, never hardcoded |
| **More currencies** | Drive the supported currency list from config, not a hardcoded set |

## Intentionally Skipped

- HTTP layer / controller (per task requirements)
- Persistent storage (in-memory / fake is sufficient here)
- 3DS completion flow (noted above)
- Retry logic (too risky without idempotency layer)

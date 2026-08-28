# Architecture and Trade-offs — In-Memory Account Ledger Core

This document explains how I designed and built the ledger, why I made each key decision, and what trade-offs I accepted. 

I started from the requirement: an append-only, in-memory account ledger core in C# / .NET 10, 
with no web layer, no database, no persistence, and no UI. 
It must be exercised by a runnable test suite or script that replays a fixed 10‑event stream and prints, 
per day (Day 1–6), the closing ledger balance, fee assessments, authorization states, and errors.

---

## 1. Scope and constraints

The assessment is very explicit about scope:

- No web layer
- No database
- No persistence
- No UI

So I built:

- A console app that replays the fixed event stream and prints six daily reports.
- An xUnit test suite with unit tests, a full end‑to‑end replay test, and one deliberately failing test as required.

Everything is in memory and exists only for the duration of the run. That is not a gap; it is exactly what was asked for.

The ledger is strictly append‑only. Events, entries, holds, fees, accruals, and errors are all immutable records. 
Corrections happen via new records (for example, reversals), never by editing or deleting existing ones.

---

## 2. What I kept from my earlier web API design, and what I dropped

I had previously designed a web API version of a similar ledger with EF Core, repositories, CQRS, 
JWT auth, and deployment concerns. For this assessment, I intentionally stripped all of that away.

**Kept and adapted:**

- Append‑only entry model
- `decimal` money with per‑currency precision (AED 2 decimals, BHD 3 decimals)
- “Never mutate or delete” principle
- Event log as the primary structure (not just an audit side table)
- Clean separation between domain rules and orchestration

**Dropped as not applicable:**

- Repository interfaces, EF Core, Postgres / SQL (DB)
- MediatR / CQRS pipeline, HTTP Idempotency‑Key behavior
- Outbox / QStash dispatch (Message)
- Supabase Auth / JWT
- Minimal API endpoints or controllers, Swagger, health checks
- Serilog/correlation‑id middleware, Render deployment

There is no `LedgerTransaction` header table as a separate concept. Each event is its own transaction, 
and ledger entries link back to their source event by `SourceEventId` for traceability. With no database, 
there is no need for extra join tables just to group entries.

---

## 3. Solution layout

The solution is intentionally small and focused:

```text
LedgerCore.slnx
LedgerCore.Domain/      # value objects, events, entries, authorization records, report shapes
LedgerCore.Engine/      # replay engine, balance projection, fee assessment, interest accrual, instalment splitting
LedgerCore/      		# Program.cs — builds the fixed event stream E1–E10, runs the engine, prints six daily reports
LedgerCore.Tests/       # xUnit — unit tests per engine, full scenario replay, plus one deliberately failing test
```

There is no `Infrastructure`, no `Api`, and no `Application` CQRS layer. 
There is nothing to abstract away: no swappable storage, no transport. 
Domain, Engine, and Console are the whole story.

---

## 4. Domain model

The domain is intentionally simple and explicit.

Core types:

- `Account` — `AccountId`, `Currency`
- `Money` — `decimal Amount`, `Currency Currency`
- `LedgerEvent` (base) — `EventId`, `BookDay`, `ValueDate`, `AccountId`
  - `CreditEvent`
  - `DebitEvent`
  - `AuthorizationEvent` — adds `AuthorizationId`, `HoldAmount`
  - `SettlementEvent` — adds `AuthorizationId`, `SettlementAmount`
  - `ReversalEvent` — adds `ReversedEventId`
  - `InstalmentCreditEvent` — adds `TotalAmount`, `InstalmentCount`
- `LedgerEntry` — `EntryId`, `SourceEventId`, `AccountId`, `SignedAmount`, `ValueDate`, `BookDay`, `Type`
- `AuthorizationRecord` — `AuthorizationId`, `AccountId`, `HoldAmount`, `BookDay`, `Status` (Approved/Declined/Settled)
- `FeeAssessment` — `AccountId`, `AssessedDay`, `Amount`, `SourceEventId`
- `InterestAccrual` — `AccountId`, `Day`, `Amount`
- `RejectedEvent` — `EventId`, `BookDay`, `Reason`
- `DailyReport` — `Day`, per‑account: `ClosingLedgerBalance`, `AvailableBalance`, `FeesAssessedToday`, `Authorizations`, `ErrorsToday`

`Money` enforces rounding based on currency precision: AED has 2 decimal places, BHD has 3. 
There is no separate reference table; a static in‑memory `CurrencyRules` helper is enough since there is no database.

---

## 5. Core design decision: bi‑temporal balance model

The most important design decision is how balances are modeled.

Every ledger entry has two dates:

- `BookDay` — when the event was appended to the stream (the event’s “Day” label).
- `ValueDate` — when the entry is effective for accounting.

E7 is the reason this matters: it is booked on Day 5 but value‑dated Day 2.

“Closing balance for day D” is not a stored field. It is a query:

> Sum of all valid ledger entries where `ValueDate ≤ D` and `BookDay ≤ asOfBookDay`.

This makes both of the following true at the same time:

- Day 2 closing balance as understood on Day 2 (before E7 exists): **AED 250.00**  
  (1200 − 950)
- Day 2 closing balance as evaluated at the end of Day 5 (after E7 posts): **AED −370.00**  
  (1200 − 950 − 620)

Nothing about the original Day‑2 entries changed. A new entry with an older value date changed what “Day 2’s closing balance” means when asked later.

The engine exposes this as a first‑class query, for example:

```csharp
BalanceProjector.ClosingBalance(accountId, valueDate, asOfBookDay)
```

not just as a single running total.

### Consequences for fees

Because E7 retroactively changes Day 2’s balance, 
it can also retroactively change other already‑passed days if their value‑date window absorbs the backdated entry. 
In this scenario, Day 4 and Day 5 also go negative, not just Day 2.

At each day‑close, the engine re‑checks every prior day (1..D) that has not yet been fee‑assessed for that account. 
If that day’s closing balance (as of the current `asOfBookDay`) is negative, 
it appends exactly one `FeeAssessment` for that day. 
Assessed days are tracked per `(AccountId, Day)` so re‑checking is idempotent.

### Consequences for reversals

E9 reverses E7 by posting an equal‑and‑opposite entry, also value‑dated Day 2. 
The reversal restores the balance arithmetic, 
but any fee already assessed off the negative Day‑2/4/5 balances is its own immutable record. Reversing the trigger does not, 
and structurally cannot, un‑assess a fee that was already appended. `FeeAssessment` records are never retracted.

---

## 6. Design decision: day‑close boundaries come from Day labels, not stream position

The given replay order is not monotonic by Day. E10 (Day 5) appears after E9 (Day 6). 
State mutations still apply in the exact given stream order (this matters causally — for example, 
E6 must see that Auth‑A exists but Auth‑Z does not, which depends on E3 having already run). 
But “close Day 5” cannot be triggered the moment a Day 6 event is seen, or E10 gets excluded from Day 5’s own report.

Resolution:

1. Pre‑scan the fixed stream once to find the **last stream index** carrying each Day label.
2. Do a single forward pass applying every event in the given order.
3. Fire day‑close side effects (fee assessment, interest accrual, report snapshot) 
   only once that day’s last index has been reached, regardless of what Day labels appeared in between.

Conceptually:

- Fixed event stream E1..E10 → pre‑scan → last index per Day.
- Apply events in given order → append to:
  - LedgerEntry log
  - Hold store
  - RejectedEvent log
- When the last event for a given Day is reached:
  - FeeAssessor re‑checks days 1..D for negative balances.
  - InterestAccrual computes that day’s accrual.
  - DailyReport snapshot is generated.

This ensures E10 is included in Day 5’s report even though it appears after a Day 6 event.

---

## 7. Business rule engines

### Authorization and holds

Available balance is defined as:

> `AvailableBalance = ClosingLedgerBalance(asOf now) − sum(active approved holds)`

An authorization is approved only if:

> `AvailableBalance − requestedHold ≥ 0`

Otherwise it is declined. No hold is created, but the decline is logged so it shows in that day’s authorization states.

### Settlement

A settlement must reference an existing **Active** hold by `AuthorizationId`.

- If found: release the hold and post a debit `LedgerEntry` for the settlement amount, 
which may differ from the original hold amount. For Auth‑A: held AED 200.00, settled AED 185.00.
- If not found (Auth‑Z in E6): reject the event, log a `RejectedEvent`, and make no balance changes.

This enforces the invariant that settlements must reference an existing, approved authorization.

### Reversal

A reversal posts a new, equal‑and‑opposite `LedgerEntry` referencing the original event’s id and value date. 
It never touches the original record. This keeps the ledger strictly append‑only.

### Overdraft fee

At each day‑close, for each account:

- Re‑evaluate `ClosingBalance` for every day 1..D that has not yet been fee‑assessed for that account.
- If negative, append one `FeeAssessment` (value‑dated to that day) for AED 25.00.
- Track assessed days per `(AccountId, Day)` to ensure idempotency.

### Daily interest

At each day‑close, for each account:

- If that day’s closing balance is positive, compute:
  
  > `accrual = balance × 0.0004` (0.04% per day)

- Round the accrual to the account’s currency precision.
- Store it as that day’s accrual (not yet posted).

On Day 6:

- Sum the six already‑rounded daily figures.
- Post that sum as a single capitalizing credit.
- The sum is exact by construction, since nothing is rounded a second time.

### Money and remainder‑safe splitting (E10)

E10 credits BHD 10.000 as three equal instalments. Three identical rounded values of 3.334 would sum to 10.002, 
which is incorrect.

I use a shared `InstalmentSplitter(Money total, int n)`:

1. Convert total to minor units using currency precision.
2. Compute `baseMinor = totalMinor / n` (integer division).
3. Compute `remainder = totalMinor % n`.
4. First `remainder` instalments get `baseMinor + 1` minor units; the rest get `baseMinor`.

For BHD 10.000 / 3:

- Base = 3.333
- Remainder = 1 minor unit
- Instalments: 3.334, 3.333, 3.333

They sum exactly to 10.000. Instalments are not all identical, 
which contradicts a naive “equal split” assumption but is necessary to satisfy the “sum exactly” constraint.

---

## 8. Daily report shape

Per account, per Day 1–6, the report shows:

- `ClosingLedgerBalance`
- `AvailableBalance`
- `FeesAssessedToday`
- `AuthorizationStates` (per open/settled/declined hold as of that day)
- `ErrorsToday` (rejected events)

Report generation is a pure projection over the immutable stores after day‑close. It never mutates them.

---

## 9. Testing strategy

Tests are a core part of the deliverable.

**Unit tests:**

- `Money` rounding for AED and BHD.
- `InstalmentSplitter` verifying that rounded instalments sum exactly to the total and that remainder distribution works as intended.
- `BalanceProjector` reproducing the Day‑2‑as‑of‑Day‑5 = −370.00 case directly.
- `FeeAssessor` idempotency and multi‑day retroactive detection.
- Hold approval at the exact zero boundary.
- Settlement‑against‑unknown‑auth rejection.
- Reversal‑does‑not‑erase‑fees behavior.

**Scenario test:**

- Single end‑to‑end replay of E1–E10, asserting every day’s figures (balances, fees, auth states, errors).

**Required failing test:**

- One deliberately failing test, annotated inline, demonstrating a genuine edge the model does not cleanly resolve. 
For example, showing that three equal BHD 3.334 instalments cannot sum to 10.000. 
This is not a bug; it is an artifact required by the assessment to show understanding of the ambiguity.

---

## 10. Alternatives considered

### Using an in‑memory database (EF Core in‑memory provider)

I considered using EF Core with an in‑memory provider to model the ledger.

- Pros: familiar patterns, easy to swap to a real DB later.
- Cons: adds unnecessary abstraction, potential behavioral differences from a real DB, 
and violates the “no database” constraint.

Decision: rejected. Plain in‑memory collections are simpler and more appropriate here.

### Minimal API / HTTP layer

I also considered wrapping the engine in a Minimal API or controllers.

- Pros: would look like a “real service”.
- Cons: explicitly forbidden by the requirement; adds complexity without value.

Decision: rejected. Console runner plus tests is exactly what is needed.

### Mutable ledger entries

An alternative would be to mutate entries when reversals occur.

- Pros: simpler balance calculation (single running total).
- Cons: contradicts the append‑only requirement, complicates auditing and reasoning about history.

Decision: rejected. Append‑only with compensating entries is the right model.

### Single running balance per account

Another option is to maintain a single running balance per account.

- Pros: very simple.
- Cons: cannot correctly handle late/backdated events like E7 and E9.

Decision: rejected. Bi‑temporal balance queries are necessary.

---

## 11. Known limitations and future work

This implementation is intentionally narrow in scope:

- Single‑process, in‑memory only. Not suitable for production persistence or multi‑instance deployments.
- No explicit idempotency keys. Not needed for a fixed, hard‑coded event stream.
- Interest and fee policies are hard‑coded. A real system would externalize these.

These limitations are acceptable because the assessment is about correct event replay, balance projection, 
and rule enforcement, not about building a production ledger service.

If this were to evolve into a real product, I would:

- Add durable storage and proper transactional boundaries.
- Introduce idempotency keys and explicit replay controls.
- Externalize fee and interest configuration.
- Add monitoring, logging, and metrics.

For this task, none of that is required.

---

## 12. How this ties to the other documents

- `README.md` — how to run the console script and the test suite, and how to read the report.
- `NUMBERS.md` — every numeric constant (AED 25.00 fee, 0.04% daily interest, currency precisions, rounding mode) and why each value was chosen.
- `AMBIGUITIES.md` — every ambiguity I found (non‑monotonic Day ordering, retroactive multi‑day fee triggering, Auth‑B’s approve/decline outcome, whether “return to pre‑E7 values” was ever achievable given append‑only fees) and how I resolved each one.
- `REJECTED.md` — which acceptance criteria I refused, with reasons. The bi‑temporal model and remainder‑splitting logic directly inform which criteria do not hold up.
- `WORKLOG.md` — timestamped build log showing decisions, implementation steps, and tests.

This architecture document is meant to be read together with those files.
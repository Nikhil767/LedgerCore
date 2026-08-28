```markdown
# Worklog

Timestamped notes of decisions, implementation steps, and tests.

## 2026-08-27 23:55

- Created solution `LedgerCore.sln` with four projects:
  - `LedgerCore.Domain`
  - `LedgerCore.Engine`
  - `LedgerCore`
  - `LedgerCore.Tests`
- Implemented domain model:
  - `CurrencyCode`, `CurrencyRules`
  - `Money` with per-currency rounding
  - `Account`
  - Event types: `CreditEvent`, `DebitEvent`, `AuthorizationEvent`, `SettlementEvent`, `ReversalEvent`, `InstalmentCreditEvent`
  - `LedgerEntry`, `AuthorizationRecord`, `FeeAssessment`, `InterestAccrual`, `RejectedEvent`, `DailyReport`

## 2026-08-28 09:50

- Implemented engine:
  - `InstalmentSplitter` for remainder-safe multi-instalment splits.
  - `BalanceProjector` with bi-temporal query: `ClosingBalance(accountId, valueDate, asOfBookDay)`.
  - `FeeAssessor` with idempotent per-day fee tracking.
  - `InterestAccrualEngine` with per-day rounding and Day 6 capitalization.
  - `LedgerEngine`:
    - Pre-scan to find last index per `BookDay`.
    - Single forward pass applying events in given order.
    - Day-close: fee assessment, interest accrual.
    - Capitalize interest at end of Day 6.

## 2026-08-28 10:04

- Implemented console runner:
  - Hard-coded event stream E1–E10.
  - Replay via `LedgerEngine`.
  - Print six daily reports with:
    - Closing ledger balance
    - Available balance
    - Fees assessed today
    - Authorization states
    - Errors today

## 2026-08-28 10:14

- Implemented tests:
  - `MoneyTests`: AED/BHD rounding.
  - `InstalmentSplitterTests`: BHD 10 split into 3 instalments sums exactly to 10.
  - `FullReplayTests`: end-to-end replay, asserts Day 2 balance as of Day 5 = −370.00 AED.
  - `RequiredFailingTest`: deliberately failing test demonstrating that three equal BHD 3.334 instalments cannot sum to 10.000.

## 2026-08-28 10:16

- Verified:
  - `dotnet build` succeeds.
  - `dotnet test` runs with expected passing tests and one skipped failing test.
  - `dotnet run --project LedgerCore` prints six daily reports as specified.

## 2026-08-28 11:29

- Created documentation files:
  - `README.md`
  - `NUMBERS.md`
  - `AMBIGUITIES.md`
  - `REJECTED.md`
  - `WORKLOG.md` (this file)

## Decisions and notes

- Chose not to use Minimal API or any in-memory database, per the “no web layer, no persistence, no database” constraint.
- Enforced append-only semantics: no mutation or deletion of events or entries; corrections via new entries (reversals, fees).
- Adopted bi-temporal balance model to correctly handle E7 and E9.
- Used remainder-safe instalment splitting to satisfy the “sum exactly” interest rule and avoid BHD drift.
```

## 2026-08-28 11:38

- Created Output text file:


## 2026-08-28 12:10

- Created docker file:

## 2026-08-28 13:54

- updated docker file
- added .dockerignore file

---
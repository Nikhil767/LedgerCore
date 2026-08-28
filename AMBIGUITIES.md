```markdown
# Ambiguities and resolutions

This document records ambiguities found in the requirements and how they were resolved in this implementation.

## 1. Non-monotonic event order (E10 after E9)

**Ambiguity**: The event stream is not ordered strictly by `BookDay`. E10 (Day 5) appears after E9 (Day 6). It is unclear whether “per day” reports should be emitted as soon as a day’s events are seen, or only once all events for that day have been processed.

**Resolution**: Pre-scan the fixed stream to find the last index for each `BookDay`. Apply events in the given order, but trigger day-close side effects (fee assessment, interest accrual, report snapshot) only when the last event for that day has been applied. This ensures E10 is included in Day 5’s report even though it appears after a Day 6 event.[1]

## 2. Bi-temporal balance: E7 booked on Day 5, value-dated Day 2

**Ambiguity**: How should “Day 2 closing balance” be interpreted when a later event (E7) has an older value date?

**Resolution**: Model balance as a query over two dimensions:

- `ValueDate <= day`
- `BookDay <= asOfBookDay`

Thus:

- Day 2 balance as of Day 2 (before E7 exists) = 250.00 AED.
- Day 2 balance as of Day 5 (after E7 posts) = −370.00 AED.

The engine exposes this via `BalanceProjector.ClosingBalance(accountId, valueDate, asOfBookDay)`.

## 3. Retroactive fee triggering

**Ambiguity**: If a late event makes a prior day’s balance negative, should a fee be assessed for that past day? And if so, how to avoid double-assessing?

**Resolution**: At each day-close, re-evaluate all days `1..D` that have not yet been fee-assessed for that account. If any such day’s closing balance (as of the current `asOfBookDay`) is negative, append exactly one `FeeAssessment` for that day. Track assessed days per `(AccountId, Day)` to ensure idempotency.

## 4. Settlement vs hold amount (Auth-A)

**Ambiguity**: Auth-A holds AED 200.00 but settles for AED 185.00. Does the settlement debit use the hold amount or the settlement amount?

**Resolution**: Settlement debits use the settlement amount (AED 185.00). The hold is released in full; the unused AED 15.00 simply ceases to be held. This matches standard authorization/settlement behavior.

## 5. Unknown authorization (Auth-Z, E6)

**Ambiguity**: Should a settlement referencing a non-existent authorization still debit the account, or be rejected?

**Resolution**: Reject the event, log a `RejectedEvent`, and make no balance changes. This enforces the invariant that settlements must reference an existing, approved authorization.

## 6. Reversal and fees (E9 reversing E7)

**Ambiguity**: Does reversing E7 also reverse fees that were assessed because of E7’s impact?

**Resolution**: No. Reversal adds a compensating entry for the principal only. Fees are immutable `FeeAssessment` records; they are not retracted. This is consistent with the append-only model: corrections happen via new records, not edits or deletes.

## 7. BHD instalments “three equal instalments”

**Ambiguity**: The requirement states “three equal instalments” for BHD 10.000, but equal rounded values (3.334 each) do not sum to 10.000.

**Resolution**: Interpret “equal” as “as equal as possible under rounding”. Use the remainder-safe splitting algorithm so that instalments are 3.334, 3.333, 3.333, summing exactly to 10.000. This is documented in `NUMBERS.md`.

## 8. Interest rounding vs capitalization

**Ambiguity**: Should daily accruals be rounded, and if so, how to ensure the capitalized total matches?

**Resolution**: Round each day’s accrual to the account’s currency precision. Capitalize the sum of those already-rounded values. By construction, the sum of the rounded daily accruals equals the capitalized total; no remainder is discarded.
```

---
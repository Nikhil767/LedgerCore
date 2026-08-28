```markdown
# Rejected acceptance criteria

This document lists the acceptance criteria from the assessment that are rejected, with reasons.

## 1. “E7 causes exactly one overdraft fee to be assessed, on Day 2.”

**Verdict**: Rejected.

**Reason**: E7 is booked on Day 5 with a Day 2 value date. It retroactively changes the closing balance for Day 2 (and potentially other days). The engine will assess a fee for Day 2 if the recalculated balance is negative and Day 2 has not yet been fee-assessed. However, the phrase “exactly one” is not guaranteed without a fully specified policy on:

- Whether previously assessed fees for earlier days are recomputed or left as-is.
- How multiple backdated entries with overlapping value-date windows interact.

Under the implemented append-only, idempotent fee policy, E7 may cause a Day 2 fee to be assessed, but the “exactly one” wording is too strong given the lack of a complete late-event correction specification.[1]

## 2. “After E9, all balances and fees return to their pre-E7 values.”

**Verdict**: Rejected.

**Reason**: E9 reverses E7’s principal effect by adding an equal-and-opposite entry, also value-dated Day 2. This restores the arithmetic for ledger balances. However, fees assessed because of E7’s impact are immutable `FeeAssessment` records; they are not retracted. Therefore, fees do not return to their pre-E7 state. The append-only model structurally prevents “un-assessing” a fee once recorded.

## 3. “The three BHD instalments in E10 must each be BHD 3.334.”

**Verdict**: Rejected.

**Reason**:  
\[
3.334 + 3.334 + 3.334 = 10.002 \neq 10.000
\]

Three identical rounded instalments of 3.334 would exceed BHD 10.000. The correct approach is to split BHD 10.000 into 3.334, 3.333, 3.333, which sum exactly to 10.000. This is implemented in `InstalmentSplitter` and documented in `NUMBERS.md`.

## 4. “If the rounded daily interest accruals do not sum to the capitalized total, the remainder is discarded.”

**Verdict**: Rejected.

**Reason**: This directly contradicts the requirement that “the rounded daily accruals must sum exactly to the capitalized total.” The implementation rounds each day’s accrual, then capitalizes the sum of those rounded values. By construction, there is no remainder to discard; the capitalized total equals the sum of daily accruals.
```

---
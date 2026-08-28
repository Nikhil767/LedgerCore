```markdown
# Numbers and constants

This document lists every numeric constant and design choice that affects behavior, and explains why each value was chosen.

## Currency precision

- **AED**: 2 decimal places.
- **BHD**: 3 decimal places.

Reason: These are the standard market precisions for AED and BHD. The domain enforces them in `Money` via `CurrencyRules.DecimalPlaces`.

## Rounding mode

- **Mode**: `MidpointRounding.ToEven` (banker’s rounding).
- Applied whenever constructing a `Money` value.

Reason: .NET’s default for `decimal.Round`; avoids systematic bias in repeated rounding and is appropriate for an internal ledger where no regulatory rounding rule was specified.

## Overdraft fee

- **Amount**: AED 25.00 per account per day.
- **Condition**: Assessed when the closing ledger balance for that account/day is negative.
- **Frequency**: At most once per account per day.

Reason: Directly from the requirement. The fee is modeled as an immutable `FeeAssessment` and an associated debit `LedgerEntry`.

## Daily interest rate

- **Rate**: 0.04% per day = 0.0004 as a multiplier.
- **Base**: Positive closing ledger balance only.
- **Capitalization**: Single credit at end of Day 6 equal to the sum of the six already-rounded daily accruals.

Reason: Directly from the requirement. Rounding happens per day; the capitalization uses the sum of those rounded values so there is no second rounding and the total matches exactly.

## Instalment splitting

- **Algorithm**:
  - Convert total to minor units (e.g., fils/satoshis) using currency precision.
  - Compute `baseMinor = totalMinor / n` (integer division).
  - Compute `remainder = totalMinor % n`.
  - First `remainder` instalments get `baseMinor + 1` minor units; the rest get `baseMinor`.

- **Effect for E10**: BHD 10.000 split into 3 instalments becomes:
  - 1st: BHD 3.334
  - 2nd: BHD 3.333
  - 3rd: BHD 3.333
  - Sum: exactly BHD 10.000.

Reason: Guarantees that rounded instalments sum exactly to the total, avoiding drift. This is necessary because equal rounded values (e.g., 3.334 each) would not sum to 10.000.

## Day window

- **Window**: Day 1 through Day 6 inclusive.
- **Interest capitalization day**: Day 6.

Reason: Explicitly required by the assessment.
```

---
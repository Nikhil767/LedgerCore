# In-Memory Account Ledger Core

A C#/.NET 10 implementation of an append-only, in-memory account ledger.

The application:

- Has no web layer.
- Has no database or persistence.
- Has no UI.
- Replays the fixed E1–E10 event stream.
- Prints a report for Days 1–6.
- Includes an automated xUnit test suite.

## Prerequisites

- .NET 10 SDK
- Git, if cloning the repository

Check the installed SDK:

```bash
dotnet --version
```

## Solution structure

```text
LedgerCore.sln

LedgerCore.Domain/
LedgerCore.Engine/
LedgerCore/

LedgerCore.Tests/
```

### LedgerCore.Domain

Contains the domain types:

- Currency and precision rules.
- `Money` value object.
- Account model.
- Immutable ledger events.
- Ledger entries.
- Authorization records.
- Fee, interest, rejection, and report models.

### LedgerCore.Engine

Contains the business logic:

- Event replay.
- Append-only ledger entry creation.
- Bi-temporal balance projection.
- Authorization approval and settlement.
- Overdraft fee assessment.
- Daily interest accrual.
- Day 6 interest capitalization.
- Currency-safe instalment splitting.

### LedgerCore.Console

A runnable console application that:

1. Creates ACC-001 and ACC-002.
2. Builds events E1–E10.
3. Replays them in the specified order.
4. Prints daily results for Days 1–6.

### LedgerCore.Tests

Contains:

- Money rounding tests.
- Instalment splitting tests.
- Balance projection tests.
- Full event-stream replay tests.
- The required annotated failing-test artifact.

## Build

From the repository root:

```bash
dotnet restore
dotnet build
```

## Run the console application

```bash
dotnet run --project LedgerCore
```

The runner prints results for each account and each day:

```text
=== Day 1 ===
ACC-001
Closing ledger balance: ...
Available balance: ...
Fee assessed today: ...
Active authorizations: ...
Errors today: ...
```

## Run tests

```bash
dotnet test
```

The normal test run should pass. The deliberately failing assessment test is skipped so that the standard test command remains successful. Its purpose is documented inside `RequiredFailingTest.cs`.

To run the skipped test manually, remove or change its `Skip` value in that file.

## Report fields

For each day and account, the console prints:

- **Closing ledger balance**: all valid ledger entries with `ValueDate <= day`, using events available at that point in the replay.
- **Available balance**: closing ledger balance minus active approved authorization holds.
- **Fee assessed today**: any overdraft fee assessed for the account on that day.
- **Active authorizations**: authorization IDs and their current statuses.
- **Errors today**: rejected events processed on that day.

## Important design decisions

### Append-only ledger

Events and ledger entries are never edited or deleted.

A reversal creates a new compensating ledger entry. For example, E9 reverses E7 by adding a positive AED 620.00 entry; E7 itself remains unchanged.

### Book day and value date

Each event has two dates:

- `BookDay`: the day the event enters the stream.
- `ValueDate`: the day the event affects the ledger balance.

E7 is booked on Day 5 but has a value date of Day 2. Therefore, it changes the Day 2 balance when the balance is evaluated after E7 has been replayed.

### Authorization holds

An approved authorization:

- Does not change the ledger balance.
- Reduces the available balance.
- Remains active until settled.

A settlement must reference an approved authorization. Unknown authorization `Auth-Z` is rejected and does not move funds.

### Fees

An overdraft fee is assessed once per account per day when that day's closing ledger balance is negative.

Fee records are append-only and are not automatically removed when a later reversal changes the balance.

### Interest

Daily interest is calculated only for positive closing balances:

```text
closing balance × 0.0004
```

Each daily accrual is rounded to the account currency precision. At the end of Day 6, the rounded daily accruals are summed and posted as one capitalization credit.

### BHD instalments

BHD uses three decimal places. BHD 10.000 split into three instalments is:

```text
BHD 3.334
BHD 3.333
BHD 3.333
```

The sum is exactly BHD 10.000. Three instalments of BHD 3.334 would produce BHD 10.002 and are therefore invalid.

## Assessment documentation

The repository also contains:

- `NUMBERS.md`: constants, precision, rounding, interest, and fee decisions.
- `AMBIGUITIES.md`: identified ambiguities and their resolutions.
- `REJECTED.md`: incorrect acceptance criteria and reasons for rejecting them.
- `WORKLOG.md`: timestamped implementation worklog.

## Useful commands

```bash
# Restore dependencies
dotnet restore

# Build the entire solution
dotnet build

# Run all tests
dotnet test

# Run the console replay
dotnet run --project LedgerCore

# Build only the console project
dotnet build LedgerCore

# Build only the test project
dotnet build LedgerCore.Tests
```

## Scope limitations

This project intentionally does not include:

- ASP.NET Core Minimal API.
- EF Core.
- An in-memory database.
- File or database persistence.
- Authentication.
- UI.
- External services.
- Background workers.

The entire ledger exists only during one process execution, as required by the assessment.

---
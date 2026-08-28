using LedgerCore.Domain;
using LedgerCore.Engine;

var accounts = new List<Account>
{
	new("ACC-001", CurrencyCode.AED, Money.Zero(CurrencyCode.AED)),
	new("ACC-002", CurrencyCode.BHD, Money.Zero(CurrencyCode.BHD))
};

var events = BuildEventStream();

var engine = new LedgerEngine(accounts);
engine.Replay(events);

PrintReport(engine, accounts);

static IReadOnlyList<LedgerEvent> BuildEventStream()
{
	return
	[
		new CreditEvent("E1", 1, 1, "ACC-001", new Money(1200m, CurrencyCode.AED)),
		new DebitEvent("E2", 1, 1, "ACC-001", new Money(950m, CurrencyCode.AED)),
		new AuthorizationEvent("E3", 2, 2, "ACC-001", "Auth-A", new Money(200m, CurrencyCode.AED)),
		new CreditEvent("E4", 3, 3, "ACC-001", new Money(400m, CurrencyCode.AED)),
		new SettlementEvent("E5", 4, 4, "ACC-001", "Auth-A", new Money(185m, CurrencyCode.AED)),
		new SettlementEvent("E6", 4, 4, "ACC-001", "Auth-Z", new Money(180m, CurrencyCode.AED)),
		new DebitEvent("E7", 5, 2, "ACC-001", new Money(620m, CurrencyCode.AED)),
		new AuthorizationEvent("E8", 5, 5, "ACC-001", "Auth-B", new Money(90m, CurrencyCode.AED)),
		new ReversalEvent("E9", 6, 2, "ACC-001", "E7"),
		new InstalmentCreditEvent("E10", 5, 5, "ACC-002", new Money(10m, CurrencyCode.BHD), 3)
	];
}

static void PrintReport(LedgerEngine engine, IReadOnlyList<Account> accounts)
{
	var projector = new BalanceProjector(engine.Entries);

	for (var day = 1; day <= 6; day++)
	{
		Console.WriteLine($"=== Day {day} ===");

		foreach (var account in accounts)
		{
			var closing = projector.ClosingBalance(
				account.AccountId,
				day,
				day,
				account.Currency);

			var activeHolds = projector.TotalActiveHolds(
				account.AccountId,
				day,
				engine.Authorizations,
				account.Currency);

			var available = closing - activeHolds;

			var feesToday = engine.FeeAssessments
				.Where(f => f.AccountId == account.AccountId && f.AssessedDay == day)
				.ToList();

			var auths = engine.Authorizations
				.Where(a => a.AccountId == account.AccountId && a.BookDay <= day)
				.ToList();

			var errorsToday = engine.RejectedEvents
				.Where(e => e.BookDay == day)
				.ToList();

			Console.WriteLine($"{account.AccountId}");
			Console.WriteLine($"Closing ledger balance: {closing}");
			Console.WriteLine($"Available balance: {available}");
			Console.WriteLine($"Fee assessed today: {(feesToday.Count > 0 ? feesToday[0].Amount.ToString() : "None")}");
			Console.WriteLine($"Active authorizations: {(auths.Count > 0 ? string.Join("; ", auths.Select(a => $"{a.AuthorizationId}={a.Status}")) : "none")}");
			Console.WriteLine($"Errors today: {(errorsToday.Count > 0 ? string.Join("; ", errorsToday.Select(e => $"{e.EventId}: {e.Reason}")) : "none")}");
			Console.WriteLine();
		}
	}
}
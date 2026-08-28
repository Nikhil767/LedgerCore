using LedgerCore.Domain;
using LedgerCore.Engine;

namespace LedgerCore.Tests
{
	public class FullReplayTests
	{
		[Fact]
		public void Replay_E1_to_E10_produces_six_day_reports()
		{
			var accounts = new List<Account>
		{
			new("ACC-001", CurrencyCode.AED, Money.Zero(CurrencyCode.AED)),
			new("ACC-002", CurrencyCode.BHD, Money.Zero(CurrencyCode.BHD))
		};

			var events = BuildEventStream();
			var engine = new LedgerEngine(accounts);
			engine.Replay(events);

			var projector = new BalanceProjector(engine.Entries);

			var day2BalanceAsOfDay5 = projector.ClosingBalance(
				"ACC-001", 2, 5, CurrencyCode.AED);

			Assert.Equal(-370m, day2BalanceAsOfDay5.Amount);
		}

		private static IReadOnlyList<LedgerEvent> BuildEventStream()
		{
			return new List<LedgerEvent>
		{
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
		};
		}
	}
}

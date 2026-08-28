using LedgerCore.Domain;

namespace LedgerCore.Engine
{
	public sealed class InterestAccrualEngine
	{
		private readonly BalanceProjector _projector;
		private readonly Dictionary<(string AccountId, int Day), Money> _accruals;

		public InterestAccrualEngine(BalanceProjector projector)
		{
			_projector = projector;
			_accruals = new Dictionary<(string, int), Money>();
		}

		public void AccrueForDay(
			IReadOnlyList<Account> accounts,
			int day,
			int asOfBookDay)
		{
			foreach (var account in accounts)
			{
				var balance = _projector.ClosingBalance(
					account.AccountId,
					day,
					asOfBookDay,
					account.Currency);

				if (balance.Amount <= 0)
				{
					continue;
				}

				var raw = balance.Amount * 0.0004m;
				var rounded = new Money(raw, account.Currency);

				_accruals[(account.AccountId, day)] = rounded;
			}
		}

		public Money CapitalizedInterest(
			string accountId,
			CurrencyCode currency)
		{
			var sum = _accruals
				.Where(kvp => kvp.Key.AccountId == accountId)
				.Sum(kvp => kvp.Value.Amount);

			return new Money(sum, currency);
		}

		public IReadOnlyList<InterestAccrual> AllAccruals =>
			_accruals
				.Select(kvp => new InterestAccrual(
					kvp.Key.AccountId,
					kvp.Key.Day,
					kvp.Value))
				.ToList();
	}
}

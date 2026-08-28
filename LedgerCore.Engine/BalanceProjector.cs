using LedgerCore.Domain;

namespace LedgerCore.Engine
{
	public sealed class BalanceProjector
	{
		private readonly IReadOnlyList<LedgerEntry> _entries;

		public BalanceProjector(IReadOnlyList<LedgerEntry> entries)
		{
			_entries = entries;
		}

		public Money ClosingBalance(
			string accountId,
			int valueDate,
			int asOfBookDay,
			CurrencyCode currency)
		{
			var sum = _entries
				.Where(e => e.AccountId == accountId
							&& e.ValueDate <= valueDate
							&& e.BookDay <= asOfBookDay)
				.Sum(e => e.SignedAmount.Amount);

			return new Money(sum, currency);
		}

		public Money TotalActiveHolds(
			string accountId,
			int asOfBookDay,
			IReadOnlyList<AuthorizationRecord> authorizations,
			CurrencyCode currency)
		{
			var holds = authorizations
				.Where(a => a.AccountId == accountId
							&& a.Status == AuthorizationStatus.Approved
							&& a.BookDay <= asOfBookDay)
				.Sum(a => a.HoldAmount.Amount);

			return new Money(holds, currency);
		}
	}
}

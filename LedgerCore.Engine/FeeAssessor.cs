using LedgerCore.Domain;

namespace LedgerCore.Engine
{
	public sealed class FeeAssessor
	{
		private readonly BalanceProjector _projector;
		private readonly HashSet<(string AccountId, int Day)> _assessedDays;

		public FeeAssessor(BalanceProjector projector)
		{
			_projector = projector;
			_assessedDays = new HashSet<(string, int)>();
		}

		public IReadOnlyList<FeeAssessment> AssessFeesForDay(
			IReadOnlyList<Account> accounts,
			int day,
			int asOfBookDay,
			string sourceEventId)
		{
			var fees = new List<FeeAssessment>();

			foreach (var account in accounts)
			{
				if (_assessedDays.Contains((account.AccountId, day)))
				{
					continue;
				}

				var balance = _projector.ClosingBalance(
					account.AccountId,
					day,
					asOfBookDay,
					account.Currency);

				if (balance.Amount < 0)
				{
					var feeAmount = new Money(25m, account.Currency);
					fees.Add(new FeeAssessment(
						account.AccountId,
						day,
						feeAmount,
						sourceEventId));

					_assessedDays.Add((account.AccountId, day));
				}
			}

			return fees;
		}
	}
}

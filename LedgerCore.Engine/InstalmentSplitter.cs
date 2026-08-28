using LedgerCore.Domain;

namespace LedgerCore.Engine
{
	public static class InstalmentSplitter
	{
		public static IReadOnlyList<Money> Split(Money total, int count)
		{
			if (count <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			var decimals = CurrencyRules.DecimalPlaces(total.Currency);
			var factor = (decimal)Math.Pow(10, decimals);
			var totalMinor = (long)(total.Amount * factor);

			var baseMinor = totalMinor / count;
			var remainder = totalMinor % count;

			var result = new List<Money>(count);
			for (var i = 0; i < count; i++)
			{
				var minor = baseMinor + (i < remainder ? 1 : 0);
				var amount = minor / factor;
				result.Add(new Money(amount, total.Currency));
			}

			return result;
		}
	}
}

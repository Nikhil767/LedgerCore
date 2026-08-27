namespace LedgerCore.Domain
{
	public readonly record struct Money
	{
		public decimal Amount { get; }
		public CurrencyCode Currency { get; }

		public Money(decimal amount, CurrencyCode currency)
		{
			Currency = currency;
			Amount = decimal.Round(
				amount,
				CurrencyRules.DecimalPlaces(currency),
				MidpointRounding.ToEven);
		}
	}
}

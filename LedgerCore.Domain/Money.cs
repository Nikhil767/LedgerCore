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

		public static Money Zero(CurrencyCode currency) => new(0m, currency);

		public static Money operator +(Money left, Money right)
		{
			EnsureSameCurrency(left, right);
			return new Money(left.Amount + right.Amount, left.Currency);
		}

		public static Money operator -(Money left, Money right)
		{
			EnsureSameCurrency(left, right);
			return new Money(left.Amount - right.Amount, left.Currency);
		}

		public static Money operator -(Money value) =>
			new(-value.Amount, value.Currency);

		public override string ToString()
		{
			var decimals = CurrencyRules.DecimalPlaces(Currency);
			return $"{Currency} {Amount.ToString($"F{decimals}")}";
		}

		private static void EnsureSameCurrency(Money left, Money right)
		{
			if (left.Currency != right.Currency)
			{
				throw new InvalidOperationException(
					$"Currency mismatch: {left.Currency} and {right.Currency}.");
			}
		}
	}
}

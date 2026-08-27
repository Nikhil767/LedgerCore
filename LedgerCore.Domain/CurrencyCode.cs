namespace LedgerCore.Domain
{
	public enum CurrencyCode
	{
		AED,
		BHD
	}
	public static class CurrencyRules
	{
		public static int DecimalPlaces(CurrencyCode currency) => currency switch
		{
			CurrencyCode.AED => 2,
			CurrencyCode.BHD => 3,
			_ => throw new ArgumentOutOfRangeException(nameof(currency))
		};
	}
}

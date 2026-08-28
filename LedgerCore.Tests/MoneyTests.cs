using LedgerCore.Domain;

namespace LedgerCore.Tests
{
	public class MoneyTests
	{
		[Fact]
		public void AED_rounds_to_2_decimals()
		{
			var money = new Money(10.125m, CurrencyCode.AED);
			Assert.Equal(10.12m, money.Amount);
		}

		[Fact]
		public void BHD_rounds_to_3_decimals()
		{
			var money = new Money(10.1235m, CurrencyCode.BHD);
			Assert.Equal(10.124m, money.Amount);
		}
	}
}

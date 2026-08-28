using LedgerCore.Domain;

namespace LedgerCore.Tests
{
	public class RequiredFailingTest
	{
		[Fact(Skip = "Deliberately failing: demonstrates that equal rounded BHD instalments cannot sum to exactly 10.000")]
		public void Three_equal_BHD_instalments_cannot_sum_to_10()
		{
			var total = new Money(10m, CurrencyCode.BHD);
			var equal = new Money(3.334m, CurrencyCode.BHD);

			var naiveSum = equal.Amount * 3;

			Assert.Equal(10m, naiveSum);
		}
	}
}

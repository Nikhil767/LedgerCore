using LedgerCore.Domain;
using LedgerCore.Engine;

namespace LedgerCore.Tests
{
	public class InstalmentSplitterTests
	{
		[Fact]
		public async Task BHD_10_split_0_instalments_throws_exception()
		{
			// Arrange
			var total = new Money(10m, CurrencyCode.BHD);

			// Act & Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => InstalmentSplitter.Split(total, 0));
			Assert.Throws<ArgumentOutOfRangeException>(() => InstalmentSplitter.Split(total, -1));
		}

		[Fact]
		public void BHD_10_split_3_instalments_sums_exactly()
		{
			var total = new Money(10m, CurrencyCode.BHD);
			var parts = InstalmentSplitter.Split(total, 3);

			var sum = parts.Sum(p => p.Amount);
			Assert.Equal(10m, sum);
			Assert.Equal(3, parts.Count);
		}
	}
}

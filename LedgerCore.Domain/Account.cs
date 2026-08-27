namespace LedgerCore.Domain
{
	public sealed record Account(string AccountId, CurrencyCode Currency, Money OpeningBalance);
}

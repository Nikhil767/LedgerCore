namespace LedgerCore.Domain
{
	public enum EntryType
	{
		Credit,
		Debit,
		Settlement,
		Reversal,
		OverdraftFee,
		InterestCapitalization
	}

	public sealed record LedgerEntry(Guid EntryId, string SourceEventId, string AccountId, 
	Money SignedAmount, int ValueDate, int BookDay, EntryType Type);
}

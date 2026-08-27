namespace LedgerCore.Domain
{
	public abstract record LedgerEvent(string EventId, int BookDay, int ValueDate, string AccountId);

	public sealed record CreditEvent(
		string EventId,
		int BookDay,
		int ValueDate,
		string AccountId,
		Money Amount)
		: LedgerEvent(EventId, BookDay, ValueDate, AccountId);

	public sealed record DebitEvent(
		string EventId,
		int BookDay,
		int ValueDate,
		string AccountId,
		Money Amount)
		: LedgerEvent(EventId, BookDay, ValueDate, AccountId);

	public sealed record AuthorizationEvent(
		string EventId,
		int BookDay,
		int ValueDate,
		string AccountId,
		string AuthorizationId,
		Money HoldAmount)
		: LedgerEvent(EventId, BookDay, ValueDate, AccountId);

	public sealed record SettlementEvent(
		string EventId,
		int BookDay,
		int ValueDate,
		string AccountId,
		string AuthorizationId,
		Money SettlementAmount)
		: LedgerEvent(EventId, BookDay, ValueDate, AccountId);

	public sealed record ReversalEvent(
		string EventId,
		int BookDay,
		int ValueDate,
		string AccountId,
		string ReversedEventId)
		: LedgerEvent(EventId, BookDay, ValueDate, AccountId);

	public sealed record InstalmentCreditEvent(
		string EventId,
		int BookDay,
		int ValueDate,
		string AccountId,
		Money TotalAmount,
		int InstalmentCount)
		: LedgerEvent(EventId, BookDay, ValueDate, AccountId);
}

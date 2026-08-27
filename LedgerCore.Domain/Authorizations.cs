namespace LedgerCore.Domain
{
	public enum AuthorizationStatus
	{
		Approved,
		Declined,
		Settled
	}

	public sealed record AuthorizationRecord(string AuthorizationId, string AccountId, 
	Money HoldAmount, int BookDay, AuthorizationStatus Status);
}

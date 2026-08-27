namespace LedgerCore.Domain
{
	public sealed record FeeAssessment(string AccountId, int AssessedDay, Money Amount, string SourceEventId);

	public sealed record InterestAccrual(string AccountId, int Day, Money Amount);

	public sealed record RejectedEvent(string EventId, int BookDay, string Reason);
}

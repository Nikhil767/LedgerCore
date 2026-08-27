namespace LedgerCore.Domain
{
	public sealed record AccountDailyReport(string AccountId, Money ClosingLedgerBalance,
	Money AvailableBalance, IReadOnlyList<FeeAssessment> FeesAssessedToday,
	IReadOnlyList<AuthorizationRecord> Authorizations, IReadOnlyList<RejectedEvent> ErrorsToday);

	public sealed record DailyReport(int Day, IReadOnlyList<AccountDailyReport> Accounts);
}

using LedgerCore.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace LedgerCore.Engine
{
	public sealed class LedgerEngine
	{
		private readonly IReadOnlyList<Account> _accounts;
		private readonly List<LedgerEntry> _entries;
		private readonly List<AuthorizationRecord> _authorizations;
		private readonly List<RejectedEvent> _rejectedEvents;
		private readonly List<FeeAssessment> _feeAssessments;
		private readonly InterestAccrualEngine _interestEngine;

		public LedgerEngine(IReadOnlyList<Account> accounts)
		{
			_accounts = accounts;
			_entries = new List<LedgerEntry>();
			_authorizations = new List<AuthorizationRecord>();
			_rejectedEvents = new List<RejectedEvent>();
			_feeAssessments = new List<FeeAssessment>();

			var projector = new BalanceProjector(_entries.AsReadOnly());
			_interestEngine = new InterestAccrualEngine(projector);
		}

		public void Replay(IReadOnlyList<LedgerEvent> events)
		{
			var lastDayIndex = ComputeLastDayIndex(events);
			var feeAssessor = new FeeAssessor(new BalanceProjector(_entries.AsReadOnly()));

			for (var i = 0; i < events.Count; i++)
			{
				var evt = events[i];
				ApplyEvent(evt);

				if (lastDayIndex.TryGetValue(evt.BookDay, out var lastIdx) && i == lastIdx)
				{
					CloseDay(evt.BookDay, feeAssessor, evt.EventId);
				}
			}

			CapitalizeInterest();
		}

		private Dictionary<int, int> ComputeLastDayIndex(IReadOnlyList<LedgerEvent> events)
		{
			var map = new Dictionary<int, int>();
			for (var i = 0; i < events.Count; i++)
			{
				map[events[i].BookDay] = i;
			}
			return map;
		}

		private void ApplyEvent(LedgerEvent evt)
		{
			switch (evt)
			{
				case CreditEvent c:
					_entries.Add(NewEntry(
						c.EventId,
						c.AccountId,
						new Money(c.Amount.Amount, c.Amount.Currency),
						c.ValueDate,
						c.BookDay,
						EntryType.Credit));
					break;

				case DebitEvent d:
					_entries.Add(NewEntry(
						d.EventId,
						d.AccountId,
						new Money(-d.Amount.Amount, d.Amount.Currency),
						d.ValueDate,
						d.BookDay,
						EntryType.Debit));
					break;

				case AuthorizationEvent a:
					var projector = new BalanceProjector(_entries.AsReadOnly());
					var ledgerBal = projector.ClosingBalance(
						a.AccountId,
						a.BookDay,
						a.BookDay,
						GetAccount(a.AccountId).Currency);

					var activeHolds = projector.TotalActiveHolds(
						a.AccountId,
						a.BookDay,
						_authorizations,
						GetAccount(a.AccountId).Currency);

					var available = ledgerBal - activeHolds;
					var afterHold = available - a.HoldAmount;

					if (afterHold.Amount >= 0)
					{
						_authorizations.Add(new AuthorizationRecord(
							a.AuthorizationId,
							a.AccountId,
							a.HoldAmount,
							a.BookDay,
							AuthorizationStatus.Approved));
					}
					else
					{
						_authorizations.Add(new AuthorizationRecord(
							a.AuthorizationId,
							a.AccountId,
							a.HoldAmount,
							a.BookDay,
							AuthorizationStatus.Declined));
					}
					break;

				case SettlementEvent s:
					var auth = _authorizations
						.FirstOrDefault(x =>
							x.AuthorizationId == s.AuthorizationId &&
							x.Status == AuthorizationStatus.Approved);

					if (auth is null)
					{
						_rejectedEvents.Add(new RejectedEvent(
							s.EventId,
							s.BookDay,
							$"Unknown or non-approved authorization: {s.AuthorizationId}"));
					}
					else
					{
						_authorizations.Remove(auth);
						_authorizations.Add(auth with { Status = AuthorizationStatus.Settled });

						_entries.Add(NewEntry(
							s.EventId,
							s.AccountId,
							new Money(-s.SettlementAmount.Amount, s.SettlementAmount.Currency),
							s.ValueDate,
							s.BookDay,
							EntryType.Settlement));
					}
					break;

				case ReversalEvent r:
					var original = _entries
						.First(e => e.SourceEventId == r.ReversedEventId);

					_entries.Add(NewEntry(
						r.EventId,
						r.AccountId,
						new Money(-original.SignedAmount.Amount, original.SignedAmount.Currency),
						r.ValueDate,
						r.BookDay,
						EntryType.Reversal));
					break;

				case InstalmentCreditEvent ic:
					var instalments = InstalmentSplitter.Split(ic.TotalAmount, ic.InstalmentCount);
					foreach (var inst in instalments)
					{
						_entries.Add(NewEntry(
							ic.EventId,
							ic.AccountId,
							inst,
							ic.ValueDate,
							ic.BookDay,
							EntryType.Credit));
					}
					break;
			}
		}

		private void CloseDay(
			int day,
			FeeAssessor feeAssessor,
			string sourceEventId)
		{
			var fees = feeAssessor.AssessFeesForDay(
				_accounts,
				day,
				day,
				sourceEventId);

			foreach (var fee in fees)
			{
				var account = GetAccount(fee.AccountId);
				_entries.Add(NewEntry(
					fee.SourceEventId + "_fee_" + fee.AssessedDay,
					fee.AccountId,
					new Money(-fee.Amount.Amount, account.Currency),
					fee.AssessedDay,
					day,
					EntryType.OverdraftFee));

				_feeAssessments.Add(fee);
			}

			_interestEngine.AccrueForDay(_accounts, day, day);
		}

		private void CapitalizeInterest()
		{
			foreach (var account in _accounts)
			{
				var total = _interestEngine.CapitalizedInterest(
					account.AccountId,
					account.Currency);

				if (total.Amount > 0)
				{
					_entries.Add(NewEntry(
						"interest_cap_" + account.AccountId,
						account.AccountId,
						total,
						6,
						6,
						EntryType.InterestCapitalization));
				}
			}
		}

		private LedgerEntry NewEntry(
			string sourceEventId,
			string accountId,
			Money signedAmount,
			int valueDate,
			int bookDay,
			EntryType type)
		{
			return new LedgerEntry(
				Guid.NewGuid(),
				sourceEventId,
				accountId,
				signedAmount,
				valueDate,
				bookDay,
				type);
		}

		private Account GetAccount(string accountId) =>
			_accounts.Single(a => a.AccountId == accountId);

		public IReadOnlyList<LedgerEntry> Entries => _entries.AsReadOnly();
		public IReadOnlyList<AuthorizationRecord> Authorizations => _authorizations.AsReadOnly();
		public IReadOnlyList<RejectedEvent> RejectedEvents => _rejectedEvents.AsReadOnly();
		public IReadOnlyList<FeeAssessment> FeeAssessments => _feeAssessments.AsReadOnly();
		public IReadOnlyList<InterestAccrual> InterestAccruals => _interestEngine.AllAccruals;
	}
}

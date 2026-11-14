using Paycore.Domain.Common;

namespace Paycore.Domain.Ledger;

public sealed record JournalLine(LedgerAccount Account, EntrySide Side, Money Amount)
{
    public static JournalLine Debit(LedgerAccount account, Money amount) => new(account, EntrySide.Debit, amount);

    public static JournalLine Credit(LedgerAccount account, Money amount) => new(account, EntrySide.Credit, amount);
}

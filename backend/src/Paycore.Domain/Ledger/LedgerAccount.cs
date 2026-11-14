namespace Paycore.Domain.Ledger;

public sealed record LedgerAccount(Guid MerchantId, LedgerAccountType Type);

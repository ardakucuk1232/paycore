namespace Paycore.Domain.Common;

public static class DomainErrorCodes
{
    public const string UnsupportedCurrency = "unsupported_currency";
    public const string CurrencyMismatch = "currency_mismatch";
    public const string InvalidAmount = "invalid_amount";
    public const string AmountOverflow = "amount_overflow";
    public const string InvalidPaymentState = "invalid_payment_state";
    public const string RefundExceedsAvailableAmount = "refund_exceeds_available_amount";
    public const string RefundNotFound = "refund_not_found";
    public const string InvalidRefundState = "invalid_refund_state";
    public const string InvalidJournalEntry = "invalid_journal_entry";
    public const string UnbalancedJournalEntry = "unbalanced_journal_entry";
}

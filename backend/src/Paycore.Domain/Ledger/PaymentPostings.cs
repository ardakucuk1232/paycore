using Paycore.Domain.Common;
using Paycore.Domain.Payments;

namespace Paycore.Domain.Ledger;

public static class PaymentPostings
{
    public static JournalEntry ForCapture(Payment payment, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (payment.CapturedAt is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentState,
                $"Payment {payment.Id} was never captured and has nothing to post.");
        }

        var (settlement, balance) = AccountsOf(payment.MerchantId);

        return JournalEntry.Create(
            payment.MerchantId,
            JournalEntryType.PaymentCaptured,
            payment.Id,
            refundId: null,
            [JournalLine.Debit(settlement, payment.Amount), JournalLine.Credit(balance, payment.Amount)],
            now);
    }

    public static JournalEntry ForRefund(Payment payment, Guid refundId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(payment);
        var refund = payment.GetRefund(refundId);

        if (refund.Status != RefundStatus.Succeeded)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRefundState,
                $"Refund {refund.Id} is {refund.Status}; only succeeded refunds are posted.");
        }

        var (settlement, balance) = AccountsOf(payment.MerchantId);

        return JournalEntry.Create(
            payment.MerchantId,
            JournalEntryType.RefundSucceeded,
            payment.Id,
            refund.Id,
            [JournalLine.Debit(balance, refund.Amount), JournalLine.Credit(settlement, refund.Amount)],
            now);
    }

    private static (LedgerAccount Settlement, LedgerAccount Balance) AccountsOf(Guid merchantId) =>
        (new LedgerAccount(merchantId, LedgerAccountType.BankSettlement), new LedgerAccount(merchantId, LedgerAccountType.MerchantBalance));
}

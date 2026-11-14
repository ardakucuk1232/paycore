using Paycore.Domain.Common;
using Paycore.Domain.Ledger;
using Paycore.Domain.Payments;
using static Paycore.UnitTests.Payments.PaymentScenarios;

namespace Paycore.UnitTests.Ledger;

public sealed class PaymentPostingsTests
{
    private static readonly LedgerAccount Settlement = new(MerchantId, LedgerAccountType.BankSettlement);
    private static readonly LedgerAccount Balance = new(MerchantId, LedgerAccountType.MerchantBalance);

    [Fact]
    public void ForCapture_MovesCapturedAmountFromSettlementToMerchantBalance()
    {
        var payment = Captured();

        var entry = PaymentPostings.ForCapture(payment, CapturedAt);

        Assert.Equal(JournalEntryType.PaymentCaptured, entry.Type);
        Assert.Equal(payment.Id, entry.PaymentId);
        Assert.Null(entry.RefundId);
        Assert.Equal(
            [JournalLine.Debit(Settlement, payment.Amount), JournalLine.Credit(Balance, payment.Amount)],
            entry.Lines);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Canceled)]
    [InlineData(PaymentStatus.Failed)]
    public void ForCapture_IsRejected_ForPaymentsThatWereNeverCaptured(PaymentStatus status)
    {
        var payment = InStatus(status);

        var exception = Assert.Throws<DomainException>(() => PaymentPostings.ForCapture(payment, CapturedAt));

        Assert.Equal(DomainErrorCodes.InvalidPaymentState, exception.Code);
    }

    [Fact]
    public void ForRefund_MovesRefundedAmountBackFromMerchantBalance()
    {
        var payment = Captured();
        var refund = payment.RequestRefund(Try(75_000), RefundedAt);
        payment.CompleteRefund(refund.Id, "RFND-700001", RefundedAt);

        var entry = PaymentPostings.ForRefund(payment, refund.Id, RefundedAt);

        Assert.Equal(JournalEntryType.RefundSucceeded, entry.Type);
        Assert.Equal(refund.Id, entry.RefundId);
        Assert.Equal(
            [JournalLine.Debit(Balance, Try(75_000)), JournalLine.Credit(Settlement, Try(75_000))],
            entry.Lines);
    }

    [Fact]
    public void ForRefund_IsRejected_WhileRefundIsPending()
    {
        var payment = Captured();
        var refund = payment.RequestRefund(Try(75_000), RefundedAt);

        var exception = Assert.Throws<DomainException>(() => PaymentPostings.ForRefund(payment, refund.Id, RefundedAt));

        Assert.Equal(DomainErrorCodes.InvalidRefundState, exception.Code);
    }

    [Fact]
    public void ForRefund_IsRejected_ForFailedRefund()
    {
        var payment = Captured();
        var refund = payment.RequestRefund(Try(75_000), RefundedAt);
        payment.FailRefund(refund.Id, "bank_unavailable", RefundedAt);

        var exception = Assert.Throws<DomainException>(() => PaymentPostings.ForRefund(payment, refund.Id, RefundedAt));

        Assert.Equal(DomainErrorCodes.InvalidRefundState, exception.Code);
    }

    [Fact]
    public void ForRefund_IsRejected_ForRefundOfAnotherPayment()
    {
        var payment = Captured();
        var otherPayment = Captured();
        var otherRefund = otherPayment.RequestRefund(Try(1_000), RefundedAt);
        otherPayment.CompleteRefund(otherRefund.Id, "RFND-700002", RefundedAt);

        var exception = Assert.Throws<DomainException>(() => PaymentPostings.ForRefund(payment, otherRefund.Id, RefundedAt));

        Assert.Equal(DomainErrorCodes.RefundNotFound, exception.Code);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void RandomRefundSequences_KeepLedgerBalanced_AndMatchPaymentTotals(int seed)
    {
        var random = new Random(seed);
        var payment = Captured(random.NextInt64(1, 5_000_000));
        var entries = new List<JournalEntry> { PaymentPostings.ForCapture(payment, CapturedAt) };
        var postedAt = RefundedAt;

        while (payment.RefundableAmount.IsPositive && random.Next(4) != 0)
        {
            postedAt = postedAt.AddMinutes(1);
            var refund = payment.RequestRefund(Try(random.NextInt64(1, payment.RefundableAmount.Amount + 1)), postedAt);

            if (random.Next(3) == 0)
            {
                payment.FailRefund(refund.Id, "bank_unavailable", postedAt);
                continue;
            }

            payment.CompleteRefund(refund.Id, $"RFND-{seed:D3}{entries.Count:D3}", postedAt);
            entries.Add(PaymentPostings.ForRefund(payment, refund.Id, postedAt));
        }

        var expectedNet = payment.Amount.Amount - payment.RefundedAmount.Amount;
        Assert.Equal(SumOf(entries, EntrySide.Debit), SumOf(entries, EntrySide.Credit));
        Assert.Equal(expectedNet, CreditNormalBalance(entries, Balance));
        Assert.Equal(expectedNet, -CreditNormalBalance(entries, Settlement));
    }

    public static TheoryData<int> Seeds => new(Enumerable.Range(1, 200));

    private static long SumOf(IEnumerable<JournalEntry> entries, EntrySide side) =>
        entries.SelectMany(entry => entry.Lines).Where(line => line.Side == side).Sum(line => line.Amount.Amount);

    private static long CreditNormalBalance(IEnumerable<JournalEntry> entries, LedgerAccount account) =>
        entries
            .SelectMany(entry => entry.Lines)
            .Where(line => line.Account == account)
            .Sum(line => line.Side == EntrySide.Credit ? line.Amount.Amount : -line.Amount.Amount);
}

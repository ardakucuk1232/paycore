using Paycore.Domain.Common;
using Paycore.Domain.Payments;
using static Paycore.UnitTests.Payments.PaymentScenarios;

namespace Paycore.UnitTests.Payments;

public sealed class PaymentRefundTests
{
    public static TheoryData<PaymentStatus> StatusesThatCannotBeRefunded =>
        [PaymentStatus.Pending, PaymentStatus.Authorized, PaymentStatus.Canceled, PaymentStatus.Failed, PaymentStatus.Refunded];

    [Fact]
    public void RequestRefund_ReservesAmount_WithoutChangingPaymentStatus()
    {
        var payment = Captured();

        var refund = payment.RequestRefund(Try(100_000), RefundedAt);

        Assert.Equal(RefundStatus.Pending, refund.Status);
        Assert.Equal(PaymentStatus.Captured, payment.Status);
        Assert.Equal(Try(150_000), payment.RefundableAmount);
        Assert.Equal(Money.Zero(Currency.TRY), payment.RefundedAmount);
    }

    [Fact]
    public void RequestRefund_CountsPendingRefundsAgainstAvailableAmount()
    {
        var payment = Captured();
        payment.RequestRefund(Try(200_000), RefundedAt);

        var exception = Assert.Throws<DomainException>(() => payment.RequestRefund(Try(50_001), RefundedAt));

        Assert.Equal(DomainErrorCodes.RefundExceedsAvailableAmount, exception.Code);
        Assert.Single(payment.Refunds);
    }

    [Fact]
    public void RequestRefund_RejectsMoreThanCapturedAmount()
    {
        var payment = Captured();

        var exception = Assert.Throws<DomainException>(() => payment.RequestRefund(Try(CapturedAmount + 1), RefundedAt));

        Assert.Equal(DomainErrorCodes.RefundExceedsAvailableAmount, exception.Code);
    }

    [Fact]
    public void RequestRefund_RejectsZeroAmount()
    {
        var payment = Captured();

        var exception = Assert.Throws<DomainException>(() => payment.RequestRefund(Try(0), RefundedAt));

        Assert.Equal(DomainErrorCodes.InvalidAmount, exception.Code);
    }

    [Fact]
    public void RequestRefund_RejectsDifferentCurrency()
    {
        var payment = Captured();

        var exception = Assert.Throws<DomainException>(
            () => payment.RequestRefund(Money.Of(1_000, Currency.EUR), RefundedAt));

        Assert.Equal(DomainErrorCodes.CurrencyMismatch, exception.Code);
    }

    [Theory]
    [MemberData(nameof(StatusesThatCannotBeRefunded))]
    public void RequestRefund_IsRejected_UnlessCaptured(PaymentStatus status)
    {
        var payment = InStatus(status);

        var exception = Assert.Throws<DomainException>(() => payment.RequestRefund(Try(1), RefundedAt.AddDays(1)));

        Assert.Equal(DomainErrorCodes.InvalidPaymentState, exception.Code);
    }

    [Fact]
    public void CompleteRefund_ForPartOfTheAmount_MarksPaymentPartiallyRefunded()
    {
        var payment = Captured();
        var refund = payment.RequestRefund(Try(40_000), RefundedAt);

        payment.CompleteRefund(refund.Id, "RFND-551200", RefundedAt);

        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(RefundStatus.Succeeded, refund.Status);
        Assert.Equal("RFND-551200", refund.BankReference);
        Assert.Equal(Try(40_000), payment.RefundedAmount);
        Assert.Equal(Try(210_000), payment.RefundableAmount);
    }

    [Fact]
    public void CompleteRefund_ThatReachesCapturedAmount_MarksPaymentRefunded()
    {
        var payment = Captured();
        var first = payment.RequestRefund(Try(100_000), RefundedAt);
        payment.CompleteRefund(first.Id, "RFND-000001", RefundedAt);
        var second = payment.RequestRefund(Try(150_000), RefundedAt.AddHours(1));

        payment.CompleteRefund(second.Id, "RFND-000002", RefundedAt.AddHours(1));

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(Try(CapturedAmount), payment.RefundedAmount);
        Assert.Equal(Money.Zero(Currency.TRY), payment.RefundableAmount);
    }

    [Fact]
    public void FailRefund_ReleasesTheReservedAmount()
    {
        var payment = Captured();
        var refund = payment.RequestRefund(Try(CapturedAmount), RefundedAt);

        payment.FailRefund(refund.Id, "bank_unavailable", RefundedAt);

        Assert.Equal(RefundStatus.Failed, refund.Status);
        Assert.Equal("bank_unavailable", refund.FailureCode);
        Assert.Equal(PaymentStatus.Captured, payment.Status);
        Assert.Equal(Try(CapturedAmount), payment.RefundableAmount);
        Assert.Equal(RefundStatus.Pending, payment.RequestRefund(Try(CapturedAmount), RefundedAt.AddHours(1)).Status);
    }

    [Fact]
    public void CompleteRefund_CannotRunTwice()
    {
        var payment = Captured();
        var refund = payment.RequestRefund(Try(10_000), RefundedAt);
        payment.CompleteRefund(refund.Id, "RFND-000003", RefundedAt);

        var exception = Assert.Throws<DomainException>(() => payment.CompleteRefund(refund.Id, "RFND-000003", RefundedAt));

        Assert.Equal(DomainErrorCodes.InvalidRefundState, exception.Code);
        Assert.Equal(Try(10_000), payment.RefundedAmount);
    }

    [Fact]
    public void FailRefund_CannotUndoASucceededRefund()
    {
        var payment = Captured();
        var refund = payment.RequestRefund(Try(10_000), RefundedAt);
        payment.CompleteRefund(refund.Id, "RFND-000004", RefundedAt);

        var exception = Assert.Throws<DomainException>(() => payment.FailRefund(refund.Id, "late_failure", RefundedAt));

        Assert.Equal(DomainErrorCodes.InvalidRefundState, exception.Code);
        Assert.Equal(RefundStatus.Succeeded, refund.Status);
    }

    [Fact]
    public void CompleteRefund_RejectsUnknownRefund()
    {
        var payment = Captured();

        var exception = Assert.Throws<DomainException>(() => payment.CompleteRefund(Guid.NewGuid(), "RFND-000005", RefundedAt));

        Assert.Equal(DomainErrorCodes.RefundNotFound, exception.Code);
    }
}

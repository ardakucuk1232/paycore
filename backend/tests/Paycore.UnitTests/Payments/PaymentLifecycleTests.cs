using Paycore.Domain.Common;
using Paycore.Domain.Payments;
using static Paycore.UnitTests.Payments.PaymentScenarios;

namespace Paycore.UnitTests.Payments;

public sealed class PaymentLifecycleTests
{
    public static TheoryData<PaymentStatus> AllStatusesExcept(params PaymentStatus[] excluded) =>
        new(Enum.GetValues<PaymentStatus>().Except(excluded));

    public static TheoryData<PaymentStatus> StatusesThatCannotBeCaptured => AllStatusesExcept(PaymentStatus.Authorized);
    public static TheoryData<PaymentStatus> StatusesThatCannotBeCanceled => AllStatusesExcept(PaymentStatus.Authorized);
    public static TheoryData<PaymentStatus> StatusesThatCannotReceiveBankResult => AllStatusesExcept(PaymentStatus.Pending);

    [Fact]
    public void Create_StartsPendingWithoutBankReference()
    {
        var payment = Pending();

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(MerchantId, payment.MerchantId);
        Assert.Equal(Try(CapturedAmount), payment.Amount);
        Assert.Equal(CreatedAt, payment.CreatedAt);
        Assert.Null(payment.BankReference);
        Assert.NotEqual(Guid.Empty, payment.Id);
    }

    [Fact]
    public void Create_RejectsZeroAmount()
    {
        var exception = Assert.Throws<DomainException>(
            () => Payment.Create(MerchantId, Money.Zero(Currency.TRY), CaptureMethod.Automatic, CreatedAt));

        Assert.Equal(DomainErrorCodes.InvalidAmount, exception.Code);
    }

    [Fact]
    public void MarkAuthorized_MovesPendingToAuthorized()
    {
        var payment = Pending();

        payment.MarkAuthorized("AUTH-993012", AuthorizedAt);

        Assert.Equal(PaymentStatus.Authorized, payment.Status);
        Assert.Equal("AUTH-993012", payment.BankReference);
        Assert.Equal(AuthorizedAt, payment.AuthorizedAt);
    }

    [Fact]
    public void MarkFailed_MovesPendingToFailedWithReason()
    {
        var payment = Pending();

        payment.MarkFailed("insufficient_funds", AuthorizedAt);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("insufficient_funds", payment.FailureCode);
        Assert.Equal(AuthorizedAt, payment.FailedAt);
    }

    [Fact]
    public void Capture_MovesAuthorizedToCaptured()
    {
        var payment = Authorized();

        payment.Capture(CapturedAt);

        Assert.Equal(PaymentStatus.Captured, payment.Status);
        Assert.Equal(CapturedAt, payment.CapturedAt);
        Assert.Equal(Try(CapturedAmount), payment.RefundableAmount);
    }

    [Fact]
    public void Cancel_MovesAuthorizedToCanceled()
    {
        var payment = Authorized();

        payment.Cancel(CapturedAt);

        Assert.Equal(PaymentStatus.Canceled, payment.Status);
        Assert.Equal(CapturedAt, payment.CanceledAt);
    }

    [Theory]
    [MemberData(nameof(StatusesThatCannotBeCaptured))]
    public void Capture_IsRejected_OutsideAuthorized(PaymentStatus status)
    {
        var payment = InStatus(status);

        AssertInvalidTransition(payment, () => payment.Capture(RefundedAt.AddDays(1)));
    }

    [Theory]
    [MemberData(nameof(StatusesThatCannotBeCanceled))]
    public void Cancel_IsRejected_OutsideAuthorized(PaymentStatus status)
    {
        var payment = InStatus(status);

        AssertInvalidTransition(payment, () => payment.Cancel(RefundedAt.AddDays(1)));
    }

    [Theory]
    [MemberData(nameof(StatusesThatCannotReceiveBankResult))]
    public void MarkAuthorized_IsRejected_OutsidePending(PaymentStatus status)
    {
        var payment = InStatus(status);

        AssertInvalidTransition(payment, () => payment.MarkAuthorized("AUTH-000001", RefundedAt.AddDays(1)));
    }

    [Theory]
    [MemberData(nameof(StatusesThatCannotReceiveBankResult))]
    public void MarkFailed_IsRejected_OutsidePending(PaymentStatus status)
    {
        var payment = InStatus(status);

        AssertInvalidTransition(payment, () => payment.MarkFailed("card_declined", RefundedAt.AddDays(1)));
    }

    private static void AssertInvalidTransition(Payment payment, Action action)
    {
        var statusBefore = payment.Status;

        var exception = Assert.Throws<DomainException>(action);

        Assert.Equal(DomainErrorCodes.InvalidPaymentState, exception.Code);
        Assert.Equal(statusBefore, payment.Status);
    }
}

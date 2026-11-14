using Paycore.Domain.Common;
using Paycore.Domain.Payments;

namespace Paycore.UnitTests.Payments;

internal static class PaymentScenarios
{
    public const long CapturedAmount = 250_000;

    public static readonly Guid MerchantId = Guid.Parse("0193a4f1-7c2e-7d41-9b8a-5e2f4c6d8a10");
    public static readonly DateTimeOffset CreatedAt = new(2026, 11, 14, 9, 30, 0, TimeSpan.FromHours(3));
    public static readonly DateTimeOffset AuthorizedAt = CreatedAt.AddSeconds(2);
    public static readonly DateTimeOffset CapturedAt = CreatedAt.AddMinutes(5);
    public static readonly DateTimeOffset RefundedAt = CreatedAt.AddDays(2);

    public static Payment Pending(long amount = CapturedAmount) =>
        Payment.Create(MerchantId, Money.Of(amount, Currency.TRY), CaptureMethod.Manual, CreatedAt);

    public static Payment Authorized(long amount = CapturedAmount)
    {
        var payment = Pending(amount);
        payment.MarkAuthorized("AUTH-482913", AuthorizedAt);
        return payment;
    }

    public static Payment Captured(long amount = CapturedAmount)
    {
        var payment = Authorized(amount);
        payment.Capture(CapturedAt);
        return payment;
    }

    public static Payment InStatus(PaymentStatus status)
    {
        switch (status)
        {
            case PaymentStatus.Pending:
                return Pending();
            case PaymentStatus.Authorized:
                return Authorized();
            case PaymentStatus.Captured:
                return Captured();
            case PaymentStatus.PartiallyRefunded:
                return RefundedBy(CapturedAmount / 2);
            case PaymentStatus.Refunded:
                return RefundedBy(CapturedAmount);
            case PaymentStatus.Canceled:
                var canceled = Authorized();
                canceled.Cancel(CapturedAt);
                return canceled;
            case PaymentStatus.Failed:
                var failed = Pending();
                failed.MarkFailed("card_declined", AuthorizedAt);
                return failed;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
    }

    public static Money Try(long amount) => Money.Of(amount, Currency.TRY);

    private static Payment RefundedBy(long amount)
    {
        var payment = Captured();
        var refund = payment.RequestRefund(Try(amount), RefundedAt);
        payment.CompleteRefund(refund.Id, "RFND-120044", RefundedAt);
        return payment;
    }
}

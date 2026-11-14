using Paycore.Domain.Common;

namespace Paycore.Domain.Payments;

public sealed class Payment
{
    private readonly List<Refund> _refunds = [];

    private Payment(Guid id, Guid merchantId, Money amount, CaptureMethod captureMethod, DateTimeOffset createdAt)
    {
        Id = id;
        MerchantId = merchantId;
        Amount = amount;
        CaptureMethod = captureMethod;
        CreatedAt = createdAt;
        Status = PaymentStatus.Pending;
    }

    public Guid Id { get; }

    public Guid MerchantId { get; }

    public Money Amount { get; }

    public CaptureMethod CaptureMethod { get; }

    public PaymentStatus Status { get; private set; }

    public string? BankReference { get; private set; }

    public string? FailureCode { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? AuthorizedAt { get; private set; }

    public DateTimeOffset? CapturedAt { get; private set; }

    public DateTimeOffset? CanceledAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public IReadOnlyList<Refund> Refunds => _refunds.AsReadOnly();

    public Money RefundedAmount => SumOfRefunds(refund => refund.Status == RefundStatus.Succeeded);

    public Money RefundableAmount => CanAcceptRefunds
        ? Amount.Subtract(SumOfRefunds(refund => refund.Status != RefundStatus.Failed))
        : Money.Zero(Amount.Currency);

    private bool CanAcceptRefunds => PaymentTransitions.IsAllowed(Status, PaymentStatus.Refunded);

    public static Payment Create(Guid merchantId, Money amount, CaptureMethod captureMethod, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (merchantId == Guid.Empty)
        {
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        }

        if (!amount.IsPositive)
        {
            throw new DomainException(DomainErrorCodes.InvalidAmount, "Payment amount must be greater than zero.");
        }

        return new Payment(Guid.CreateVersion7(now), merchantId, amount, captureMethod, now);
    }

    public void MarkAuthorized(string bankReference, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bankReference);
        TransitionTo(PaymentStatus.Authorized);

        BankReference = bankReference;
        AuthorizedAt = now;
    }

    public void MarkFailed(string failureCode, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);
        TransitionTo(PaymentStatus.Failed);

        FailureCode = failureCode;
        FailedAt = now;
    }

    public void Capture(DateTimeOffset now)
    {
        TransitionTo(PaymentStatus.Captured);
        CapturedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        TransitionTo(PaymentStatus.Canceled);
        CanceledAt = now;
    }

    public Refund RequestRefund(Money amount, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (!CanAcceptRefunds)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentState,
                $"A refund cannot be requested for a payment in status {Status}.");
        }

        if (!amount.IsPositive)
        {
            throw new DomainException(DomainErrorCodes.InvalidAmount, "Refund amount must be greater than zero.");
        }

        if (amount.Exceeds(RefundableAmount))
        {
            throw new DomainException(
                DomainErrorCodes.RefundExceedsAvailableAmount,
                $"Refund amount {amount} exceeds the refundable amount {RefundableAmount}.");
        }

        var refund = Refund.Request(Id, amount, now);
        _refunds.Add(refund);
        return refund;
    }

    public void CompleteRefund(Guid refundId, string bankReference, DateTimeOffset now)
    {
        var refund = GetRefund(refundId);
        refund.EnsurePending();

        var nextStatus = RefundedAmount.Add(refund.Amount) == Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        EnsureCanTransitionTo(nextStatus);

        refund.MarkSucceeded(bankReference, now);
        Status = nextStatus;
    }

    public void FailRefund(Guid refundId, string failureCode, DateTimeOffset now) =>
        GetRefund(refundId).MarkFailed(failureCode, now);

    public Refund GetRefund(Guid refundId) =>
        _refunds.Find(refund => refund.Id == refundId)
        ?? throw new DomainException(DomainErrorCodes.RefundNotFound, $"Refund {refundId} does not belong to payment {Id}.");

    private void TransitionTo(PaymentStatus nextStatus)
    {
        EnsureCanTransitionTo(nextStatus);
        Status = nextStatus;
    }

    private void EnsureCanTransitionTo(PaymentStatus nextStatus)
    {
        if (!PaymentTransitions.IsAllowed(Status, nextStatus))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentState,
                $"Payment cannot move from {Status} to {nextStatus}.");
        }
    }

    private Money SumOfRefunds(Func<Refund, bool> predicate) =>
        _refunds.Where(predicate).Aggregate(Money.Zero(Amount.Currency), (total, refund) => total.Add(refund.Amount));
}

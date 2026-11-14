using Paycore.Domain.Common;

namespace Paycore.Domain.Payments;

public sealed class Refund
{
    private Refund(Guid id, Guid paymentId, Money amount, DateTimeOffset requestedAt)
    {
        Id = id;
        PaymentId = paymentId;
        Amount = amount;
        RequestedAt = requestedAt;
        Status = RefundStatus.Pending;
    }

    public Guid Id { get; }

    public Guid PaymentId { get; }

    public Money Amount { get; }

    public RefundStatus Status { get; private set; }

    public string? BankReference { get; private set; }

    public string? FailureCode { get; private set; }

    public DateTimeOffset RequestedAt { get; }

    public DateTimeOffset? CompletedAt { get; private set; }

    internal static Refund Request(Guid paymentId, Money amount, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), paymentId, amount, now);

    internal void EnsurePending()
    {
        if (Status != RefundStatus.Pending)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRefundState,
                $"Refund {Id} is {Status}; only pending refunds can succeed or fail.");
        }
    }

    internal void MarkSucceeded(string bankReference, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bankReference);
        EnsurePending();

        Status = RefundStatus.Succeeded;
        BankReference = bankReference;
        CompletedAt = now;
    }

    internal void MarkFailed(string failureCode, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);
        EnsurePending();

        Status = RefundStatus.Failed;
        FailureCode = failureCode;
        CompletedAt = now;
    }
}

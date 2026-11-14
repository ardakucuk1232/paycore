namespace Paycore.Domain.Payments;

public enum PaymentStatus
{
    Pending,
    Authorized,
    Captured,
    PartiallyRefunded,
    Refunded,
    Canceled,
    Failed,
}

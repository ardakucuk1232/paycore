using System.Collections.Frozen;

namespace Paycore.Domain.Payments;

internal static class PaymentTransitions
{
    private static readonly FrozenDictionary<PaymentStatus, FrozenSet<PaymentStatus>> Allowed =
        new Dictionary<PaymentStatus, PaymentStatus[]>
        {
            [PaymentStatus.Pending] = [PaymentStatus.Authorized, PaymentStatus.Failed],
            [PaymentStatus.Authorized] = [PaymentStatus.Captured, PaymentStatus.Canceled],
            [PaymentStatus.Captured] = [PaymentStatus.PartiallyRefunded, PaymentStatus.Refunded],
            [PaymentStatus.PartiallyRefunded] = [PaymentStatus.PartiallyRefunded, PaymentStatus.Refunded],
            [PaymentStatus.Refunded] = [],
            [PaymentStatus.Canceled] = [],
            [PaymentStatus.Failed] = [],
        }.ToFrozenDictionary(pair => pair.Key, pair => pair.Value.ToFrozenSet());

    public static bool IsAllowed(PaymentStatus from, PaymentStatus to) => Allowed[from].Contains(to);
}

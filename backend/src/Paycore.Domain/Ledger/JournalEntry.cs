using System.Collections.Immutable;
using Paycore.Domain.Common;

namespace Paycore.Domain.Ledger;

public sealed class JournalEntry
{
    private const int MinimumLineCount = 2;

    private JournalEntry(
        Guid id,
        Guid merchantId,
        JournalEntryType type,
        Guid paymentId,
        Guid? refundId,
        Currency currency,
        ImmutableArray<JournalLine> lines,
        DateTimeOffset createdAt)
    {
        Id = id;
        MerchantId = merchantId;
        Type = type;
        PaymentId = paymentId;
        RefundId = refundId;
        Currency = currency;
        Lines = lines;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid MerchantId { get; }

    public JournalEntryType Type { get; }

    public Guid PaymentId { get; }

    public Guid? RefundId { get; }

    public Currency Currency { get; }

    public IReadOnlyList<JournalLine> Lines { get; }

    public DateTimeOffset CreatedAt { get; }

    public static JournalEntry Create(
        Guid merchantId,
        JournalEntryType type,
        Guid paymentId,
        Guid? refundId,
        IEnumerable<JournalLine> lines,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var entryLines = lines.ToImmutableArray();

        EnsureStructure(merchantId, entryLines);
        var currency = EnsureSingleCurrency(entryLines);
        EnsureBalanced(entryLines, currency);

        return new JournalEntry(
            Guid.CreateVersion7(createdAt),
            merchantId,
            type,
            paymentId,
            refundId,
            currency,
            entryLines,
            createdAt);
    }

    private static void EnsureStructure(Guid merchantId, ImmutableArray<JournalLine> lines)
    {
        if (lines.Length < MinimumLineCount)
        {
            throw new DomainException(DomainErrorCodes.InvalidJournalEntry, "A journal entry needs at least two lines.");
        }

        if (lines.Any(line => line.Account.MerchantId != merchantId))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidJournalEntry,
                "Every line of a journal entry must belong to the entry's merchant.");
        }

        if (lines.Any(line => !line.Amount.IsPositive))
        {
            throw new DomainException(DomainErrorCodes.InvalidAmount, "Journal lines must have a positive amount.");
        }
    }

    private static Currency EnsureSingleCurrency(ImmutableArray<JournalLine> lines)
    {
        var currency = lines[0].Amount.Currency;

        if (lines.Any(line => line.Amount.Currency != currency))
        {
            throw new DomainException(
                DomainErrorCodes.CurrencyMismatch,
                "All lines of a journal entry must use the same currency.");
        }

        return currency;
    }

    private static void EnsureBalanced(ImmutableArray<JournalLine> lines, Currency currency)
    {
        var debits = TotalOf(lines, EntrySide.Debit, currency);
        var credits = TotalOf(lines, EntrySide.Credit, currency);

        if (debits != credits)
        {
            throw new DomainException(
                DomainErrorCodes.UnbalancedJournalEntry,
                $"Debits ({debits}) must equal credits ({credits}).");
        }
    }

    private static Money TotalOf(ImmutableArray<JournalLine> lines, EntrySide side, Currency currency) =>
        lines
            .Where(line => line.Side == side)
            .Aggregate(Money.Zero(currency), (total, line) => total.Add(line.Amount));
}

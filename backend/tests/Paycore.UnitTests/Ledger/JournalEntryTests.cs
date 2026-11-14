using Paycore.Domain.Common;
using Paycore.Domain.Ledger;

namespace Paycore.UnitTests.Ledger;

public sealed class JournalEntryTests
{
    private static readonly Guid MerchantId = Guid.Parse("0193a4f1-7c2e-7d41-9b8a-5e2f4c6d8a10");
    private static readonly Guid PaymentId = Guid.Parse("0193a4f2-1111-7a00-8000-000000000001");
    private static readonly DateTimeOffset PostedAt = new(2026, 11, 14, 9, 35, 0, TimeSpan.FromHours(3));
    private static readonly LedgerAccount Settlement = new(MerchantId, LedgerAccountType.BankSettlement);
    private static readonly LedgerAccount Balance = new(MerchantId, LedgerAccountType.MerchantBalance);

    [Fact]
    public void Create_AcceptsBalancedLines()
    {
        var entry = Create(
            JournalLine.Debit(Settlement, Try(89_990)),
            JournalLine.Credit(Balance, Try(89_990)));

        Assert.Equal(2, entry.Lines.Count);
        Assert.Equal(Currency.TRY, entry.Currency);
        Assert.Equal(MerchantId, entry.MerchantId);
    }

    [Fact]
    public void Create_AcceptsMultipleLinesThatBalanceInTotal()
    {
        var entry = Create(
            JournalLine.Debit(Settlement, Try(60_000)),
            JournalLine.Debit(Settlement, Try(40_000)),
            JournalLine.Credit(Balance, Try(100_000)));

        Assert.Equal(3, entry.Lines.Count);
    }

    [Fact]
    public void Create_RejectsUnbalancedLines()
    {
        var exception = Assert.Throws<DomainException>(() => Create(
            JournalLine.Debit(Settlement, Try(89_990)),
            JournalLine.Credit(Balance, Try(89_900))));

        Assert.Equal(DomainErrorCodes.UnbalancedJournalEntry, exception.Code);
    }

    [Fact]
    public void Create_RejectsEntriesWithFewerThanTwoLines()
    {
        var exception = Assert.Throws<DomainException>(() => Create(JournalLine.Debit(Settlement, Try(100))));

        Assert.Equal(DomainErrorCodes.InvalidJournalEntry, exception.Code);
    }

    [Fact]
    public void Create_RejectsEntriesWithOnlyOneSide()
    {
        var exception = Assert.Throws<DomainException>(() => Create(
            JournalLine.Debit(Settlement, Try(100)),
            JournalLine.Debit(Balance, Try(100))));

        Assert.Equal(DomainErrorCodes.UnbalancedJournalEntry, exception.Code);
    }

    [Fact]
    public void Create_RejectsMixedCurrencies()
    {
        var exception = Assert.Throws<DomainException>(() => Create(
            JournalLine.Debit(Settlement, Try(100)),
            JournalLine.Credit(Balance, Money.Of(100, Currency.USD))));

        Assert.Equal(DomainErrorCodes.CurrencyMismatch, exception.Code);
    }

    [Fact]
    public void Create_RejectsZeroAmountLines()
    {
        var exception = Assert.Throws<DomainException>(() => Create(
            JournalLine.Debit(Settlement, Try(100)),
            JournalLine.Credit(Balance, Try(100)),
            JournalLine.Credit(Balance, Try(0))));

        Assert.Equal(DomainErrorCodes.InvalidAmount, exception.Code);
    }

    [Fact]
    public void Create_RejectsLinesThatBelongToAnotherMerchant()
    {
        var foreignAccount = new LedgerAccount(Guid.Parse("0193a4f1-0000-7000-8000-00000000beef"), LedgerAccountType.MerchantBalance);

        var exception = Assert.Throws<DomainException>(() => Create(
            JournalLine.Debit(Settlement, Try(100)),
            JournalLine.Credit(foreignAccount, Try(100))));

        Assert.Equal(DomainErrorCodes.InvalidJournalEntry, exception.Code);
    }

    private static JournalEntry Create(params JournalLine[] lines) =>
        JournalEntry.Create(MerchantId, JournalEntryType.PaymentCaptured, PaymentId, refundId: null, lines, PostedAt);

    private static Money Try(long amount) => Money.Of(amount, Currency.TRY);
}

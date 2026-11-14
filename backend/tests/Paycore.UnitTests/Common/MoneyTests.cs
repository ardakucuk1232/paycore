using Paycore.Domain.Common;

namespace Paycore.UnitTests.Common;

public sealed class MoneyTests
{
    [Fact]
    public void Of_RejectsNegativeAmounts()
    {
        var exception = Assert.Throws<DomainException>(() => Money.Of(-1, Currency.TRY));

        Assert.Equal(DomainErrorCodes.InvalidAmount, exception.Code);
    }

    [Fact]
    public void Add_SumsMinorUnits()
    {
        var total = Money.Of(150_075, Currency.TRY).Add(Money.Of(24_925, Currency.TRY));

        Assert.Equal(Money.Of(175_000, Currency.TRY), total);
    }

    [Fact]
    public void Add_RejectsDifferentCurrencies()
    {
        var exception = Assert.Throws<DomainException>(
            () => Money.Of(100, Currency.TRY).Add(Money.Of(100, Currency.USD)));

        Assert.Equal(DomainErrorCodes.CurrencyMismatch, exception.Code);
    }

    [Fact]
    public void Add_DetectsOverflowInsteadOfWrappingAround()
    {
        var exception = Assert.Throws<DomainException>(
            () => Money.Of(long.MaxValue, Currency.TRY).Add(Money.Of(1, Currency.TRY)));

        Assert.Equal(DomainErrorCodes.AmountOverflow, exception.Code);
    }

    [Fact]
    public void Subtract_RejectsResultsBelowZero()
    {
        var exception = Assert.Throws<DomainException>(
            () => Money.Of(100, Currency.EUR).Subtract(Money.Of(101, Currency.EUR)));

        Assert.Equal(DomainErrorCodes.InvalidAmount, exception.Code);
    }

    [Fact]
    public void Subtract_RejectsDifferentCurrencies()
    {
        var exception = Assert.Throws<DomainException>(
            () => Money.Of(100, Currency.EUR).Subtract(Money.Of(1, Currency.TRY)));

        Assert.Equal(DomainErrorCodes.CurrencyMismatch, exception.Code);
    }

    [Fact]
    public void Exceeds_ComparesAmountsOfTheSameCurrency()
    {
        var larger = Money.Of(1_001, Currency.USD);
        var smaller = Money.Of(1_000, Currency.USD);

        Assert.True(larger.Exceeds(smaller));
        Assert.False(smaller.Exceeds(larger));
        Assert.False(smaller.Exceeds(smaller));
    }

    [Fact]
    public void Exceeds_RejectsDifferentCurrencies()
    {
        var exception = Assert.Throws<DomainException>(
            () => Money.Of(1, Currency.USD).Exceeds(Money.Of(1, Currency.EUR)));

        Assert.Equal(DomainErrorCodes.CurrencyMismatch, exception.Code);
    }

    [Fact]
    public void Equality_DependsOnAmountAndCurrency()
    {
        Assert.Equal(Money.Of(500, Currency.TRY), Money.Of(500, Currency.TRY));
        Assert.NotEqual(Money.Of(500, Currency.TRY), Money.Of(500, Currency.EUR));
        Assert.NotEqual(Money.Of(500, Currency.TRY), Money.Of(501, Currency.TRY));
    }

    [Fact]
    public void Zero_IsNotPositive()
    {
        Assert.False(Money.Zero(Currency.TRY).IsPositive);
        Assert.True(Money.Of(1, Currency.TRY).IsPositive);
    }
}

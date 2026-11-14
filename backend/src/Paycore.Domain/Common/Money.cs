namespace Paycore.Domain.Common;

public sealed record Money
{
    private Money(long amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public long Amount { get; }

    public Currency Currency { get; }

    public bool IsPositive => Amount > 0;

    public static Money Of(long amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        if (amount < 0)
        {
            throw new DomainException(DomainErrorCodes.InvalidAmount, "Amount cannot be negative.");
        }

        return new Money(amount, currency);
    }

    public static Money Zero(Currency currency) => Of(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);

        if (other.Amount > long.MaxValue - Amount)
        {
            throw new DomainException(DomainErrorCodes.AmountOverflow, "Amount exceeds the supported range.");
        }

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);

        if (other.Amount > Amount)
        {
            throw new DomainException(DomainErrorCodes.InvalidAmount, "Subtraction would result in a negative amount.");
        }

        return new Money(Amount - other.Amount, Currency);
    }

    public bool Exceeds(Money other)
    {
        EnsureSameCurrency(other);
        return Amount > other.Amount;
    }

    public override string ToString() => $"{Amount} {Currency} (minor units)";

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other.Currency != Currency)
        {
            throw new DomainException(
                DomainErrorCodes.CurrencyMismatch,
                $"Cannot combine amounts in {Currency} and {other.Currency}.");
        }
    }
}

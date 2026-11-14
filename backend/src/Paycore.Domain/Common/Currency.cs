namespace Paycore.Domain.Common;

public sealed record Currency
{
    public static readonly Currency TRY = new("TRY");
    public static readonly Currency USD = new("USD");
    public static readonly Currency EUR = new("EUR");

    public static IReadOnlyList<Currency> Supported { get; } = [TRY, USD, EUR];

    private Currency(string code) => Code = code;

    public string Code { get; }

    public static Currency Parse(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return Supported.FirstOrDefault(currency => string.Equals(currency.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException(
                DomainErrorCodes.UnsupportedCurrency,
                $"Currency '{code}' is not supported. Supported currencies: {string.Join(", ", Supported)}.");
    }

    public override string ToString() => Code;
}

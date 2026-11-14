using Paycore.Domain.Common;

namespace Paycore.UnitTests.Common;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("TRY")]
    [InlineData("try")]
    [InlineData("Try")]
    public void Parse_IsCaseInsensitive_AndReturnsCanonicalCode(string code)
    {
        var currency = Currency.Parse(code);

        Assert.Equal(Currency.TRY, currency);
        Assert.Equal("TRY", currency.Code);
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("")]
    [InlineData("TL")]
    public void Parse_RejectsUnsupportedCodes(string code)
    {
        var exception = Assert.Throws<DomainException>(() => Currency.Parse(code));

        Assert.Equal(DomainErrorCodes.UnsupportedCurrency, exception.Code);
    }

    [Fact]
    public void Supported_ListsTryUsdAndEur()
    {
        Assert.Equal(["TRY", "USD", "EUR"], Currency.Supported.Select(currency => currency.Code));
    }
}

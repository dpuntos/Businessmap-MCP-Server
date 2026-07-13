using BusinessMapNET.Core.Configuration;
using Xunit;

namespace BusinessMapNET.Core.Tests.Configuration;

public sealed class BusinessMapSettingsTests
{
    [Fact]
    public void BaseUrl_IsBuiltFromCompanyName()
    {
        var settings = new BusinessMapSettings { CompanyName = "contoso" };

        Assert.Equal("https://contoso.kanbanize.com/api/v2/", settings.BaseUrl);
    }

    [Fact]
    public void Validate_WithCompanyNameAndApiKey_DoesNotThrow()
    {
        var settings = new BusinessMapSettings
        {
            CompanyName = "contoso",
            ApiKey = "secret",
        };

        var exception = Record.Exception(settings.Validate);

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingCompanyName_Throws(string companyName)
    {
        var settings = new BusinessMapSettings
        {
            CompanyName = companyName,
            ApiKey = "secret",
        };

        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);
        Assert.Contains(nameof(BusinessMapSettings.CompanyName), exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingApiKey_Throws(string apiKey)
    {
        var settings = new BusinessMapSettings
        {
            CompanyName = "contoso",
            ApiKey = apiKey,
        };

        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);
        Assert.Contains(nameof(BusinessMapSettings.ApiKey), exception.Message, StringComparison.Ordinal);
    }
}

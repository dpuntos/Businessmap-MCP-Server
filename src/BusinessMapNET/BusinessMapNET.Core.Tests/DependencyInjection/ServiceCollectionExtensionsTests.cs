using BusinessMapNET.Core.Configuration;
using BusinessMapNET.Core.DependencyInjection;
using BusinessMapNET.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BusinessMapNET.Core.Tests.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBusinessMap_WithConfigureDelegate_ResolvesClientAndResourceApis()
    {
        var services = new ServiceCollection();
        services.AddBusinessMap(settings =>
        {
            settings.CompanyName = "contoso";
            settings.ApiKey = "secret";
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBusinessMapClient>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICardsApi>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBoardsApi>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUsersApi>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IWorkspacesApi>());
    }

    [Fact]
    public void AddBusinessMap_BindsSettingsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BusinessMap:CompanyName"] = "contoso",
                ["BusinessMap:ApiKey"] = "secret",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddBusinessMap(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var client = scope.ServiceProvider.GetRequiredService<IBusinessMapClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddBusinessMap_WithMissingSettings_ThrowsWhenResourceApiResolved()
    {
        var services = new ServiceCollection();
        services.AddBusinessMap(_ => { /* leave CompanyName/ApiKey empty */ });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // The HttpClient configuration calls Validate(), which fails fast for missing settings.
        Assert.Throws<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<ICardsApi>());
    }
}

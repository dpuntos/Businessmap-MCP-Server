using BusinessMapNET.Core.Configuration;
using BusinessMapNET.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BusinessMapNET.Core.DependencyInjection;

/// <summary>
/// Extension methods to register the Businessmap (Kanbanize) client and its resource APIs
/// with an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Businessmap client, binding <see cref="BusinessMapSettings"/> from the
    /// supplied configuration (default section name <see cref="BusinessMapSettings.SectionName"/>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind settings from.</param>
    /// <param name="sectionName">The configuration section name. Defaults to <c>BusinessMap</c>.</param>
    public static IServiceCollection AddBusinessMap(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = BusinessMapSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<BusinessMapSettings>(configuration.GetSection(sectionName));
        return services.AddBusinessMapCore();
    }

    /// <summary>
    /// Registers the Businessmap client, configuring <see cref="BusinessMapSettings"/> in code.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">A delegate used to configure the settings.</param>
    public static IServiceCollection AddBusinessMap(
        this IServiceCollection services,
        Action<BusinessMapSettings> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return services.AddBusinessMapCore();
    }

    private static IServiceCollection AddBusinessMapCore(this IServiceCollection services)
    {
        AddResourceClient<IUsersApi, UsersApi>(services);
        AddResourceClient<IWorkspacesApi, WorkspacesApi>(services);
        AddResourceClient<IBoardsApi, BoardsApi>(services);
        AddResourceClient<ICardsApi, CardsApi>(services);

        services.AddScoped<IBusinessMapClient, BusinessMapClient>();

        return services;
    }

    private static void AddResourceClient<TInterface, TImplementation>(IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddHttpClient<TInterface, TImplementation>(ConfigureHttpClient);
    }

    private static void ConfigureHttpClient(IServiceProvider provider, HttpClient client)
    {
        var settings = provider.GetRequiredService<IOptions<BusinessMapSettings>>().Value;
        settings.Validate();

        client.BaseAddress = new Uri(settings.BaseUrl, UriKind.Absolute);
        client.DefaultRequestHeaders.Remove("apikey");
        client.DefaultRequestHeaders.Add("apikey", settings.ApiKey);
    }
}

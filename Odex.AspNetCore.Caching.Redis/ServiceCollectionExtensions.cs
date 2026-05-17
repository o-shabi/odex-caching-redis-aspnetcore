using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Odex.AspNetCore.Caching.Redis;

/// <summary>Dependency injection registration for Odex Redis caching.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers StackExchange Redis, <see cref="OdexRedisOptions"/>, and <see cref="IOdexRedis"/>
    /// when <c>Redis:Configuration</c> is present and non-empty.
    /// </summary>
    public static IServiceCollection AddOdexRedis(
        this IServiceCollection services,
        IConfiguration configuration) =>
        AddOdexRedis(services, configuration, configureOptions: null);

    /// <summary>
    /// Registers StackExchange Redis with optional post-configuration of <see cref="OdexRedisOptions"/>.
    /// </summary>
    public static IServiceCollection AddOdexRedis(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<OdexRedisOptions>? configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var redisConfig = configuration[OdexRedisOptions.ConfigurationSectionName];
        if (string.IsNullOrWhiteSpace(redisConfig))
            return services;

        var section = configuration.GetSection(OdexRedisOptions.SectionName);

        services
            .AddOptions<OdexRedisOptions>()
            .Bind(section)
            .ValidateOnStart();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<OdexRedisOptions>, OdexRedisOptionsValidator>());

        if (configureOptions is not null)
            services.PostConfigure(configureOptions);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConfig;
            var instanceName = RedisKeyFormatting.FormatInstanceName(section[nameof(OdexRedisOptions.KeyPrefix)]);
            if (instanceName is not null)
                options.InstanceName = instanceName;
        });

        services.TryAddSingleton<IOdexRedis, OdexRedis>();
        return services;
    }
}

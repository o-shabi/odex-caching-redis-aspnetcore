using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Odex.AspNetCore.Caching.Redis.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOdexRedis_empty_configuration_does_not_register_services()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddOdexRedis(configuration);

        Assert.Null(services.FirstOrDefault(d => d.ServiceType == typeof(IOdexRedis)));
    }

    [Fact]
    public void AddOdexRedis_with_configuration_registers_io_dex_redis()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Configuration"] = "localhost:6379,abortConnect=false",
                ["Redis:KeyPrefix"] = "TestApp",
                ["Redis:DefaultExpirationMinutes"] = "30"
            })
            .Build();

        services.AddOdexRedis(configuration);

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IOdexRedis));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddOdexRedis_post_configure_applies_overrides()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Configuration"] = "localhost:6379,abortConnect=false",
                ["Redis:CacheName"] = "from-config"
            })
            .Build();

        services.AddOdexRedis(configuration, o => o.CacheName = "from-code");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OdexRedisOptions>>().Value;
        Assert.Equal("from-code", options.CacheName);
    }

    [Fact]
    public void AddOdexRedis_null_configuration_throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddOdexRedis(null!));
    }

    [Fact]
    public void AddOdexRedis_null_services_throws()
    {
        IServiceCollection? services = null;
        var configuration = new ConfigurationBuilder().Build();
        Assert.Throws<ArgumentNullException>(() => services!.AddOdexRedis(configuration));
    }
}

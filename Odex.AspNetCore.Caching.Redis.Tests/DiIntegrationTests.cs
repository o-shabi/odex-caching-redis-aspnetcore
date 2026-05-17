using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Odex.AspNetCore.Caching.Redis.Tests;

public sealed class DiIntegrationTests
{
    [Fact]
    public void Resolved_OdexRedis_uses_bound_options()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Configuration"] = "localhost:6379,abortConnect=false",
                ["Redis:KeyPrefix"] = "Integration",
                ["Redis:DefaultExpirationMinutes"] = "15",
                ["Redis:CacheName"] = "integration"
            })
            .Build();

        services.AddOdexRedis(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OdexRedisOptions>>().Value;
        Assert.Equal("Integration", options.KeyPrefix);
        Assert.Equal(15, options.DefaultExpirationMinutes);
        Assert.Equal("integration", options.CacheName);

        var cache = provider.GetRequiredService<IOdexRedis>();
        var key = cache.BuildKey("item:1");
        Assert.StartsWith("integration:", key);
        Assert.DoesNotContain("Integration", key);
    }
}

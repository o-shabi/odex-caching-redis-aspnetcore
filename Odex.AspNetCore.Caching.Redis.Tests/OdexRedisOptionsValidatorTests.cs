using Microsoft.Extensions.Options;

namespace Odex.AspNetCore.Caching.Redis.Tests;

public sealed class OdexRedisOptionsValidatorTests
{
    private readonly OdexRedisOptionsValidator _validator = new();

    [Fact]
    public void Validate_negative_expiration_fails()
    {
        var result = _validator.Validate(null, new OdexRedisOptions { DefaultExpirationMinutes = -1 });
        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_empty_cache_name_fails()
    {
        var result = _validator.Validate(null, new OdexRedisOptions { CacheName = "  " });
        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_valid_options_succeeds()
    {
        var result = _validator.Validate(null, new OdexRedisOptions());
        Assert.False(result.Failed);
    }
}

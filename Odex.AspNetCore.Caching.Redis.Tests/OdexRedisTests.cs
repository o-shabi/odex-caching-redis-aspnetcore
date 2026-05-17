using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Odex.AspNetCore.Caching.Redis.Tests;

public sealed class OdexRedisTests
{
    private static OdexRedis CreateSut(
        string keyPrefix = "MyApp",
        int defaultExpirationMinutes = 60,
        string? cacheName = null,
        bool useCamelCaseJson = true)
    {
        var options = Options.Create(new OdexRedisOptions
        {
            KeyPrefix = keyPrefix,
            DefaultExpirationMinutes = defaultExpirationMinutes,
            CacheName = cacheName ?? OdexRedisOptions.DefaultCacheName,
            UseCamelCaseJson = useCamelCaseJson
        });

        IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new OdexRedis(options, cache, cacheName);
    }

    [Fact]
    public async Task SetAsync_then_GetAsync_returns_value()
    {
        var sut = CreateSut();
        var key = sut.BuildKey("product:1");
        var product = new ProductDto(1, "Widget");

        await sut.SetAsync(key, product);
        var result = await sut.GetAsync<ProductDto>(key);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal(product.Name, result.Name);
    }

    [Fact]
    public async Task GetAsync_missing_key_returns_null()
    {
        var sut = CreateSut();
        var result = await sut.GetAsync<ProductDto>(sut.BuildKey("missing"));
        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidateAsync_removes_entry()
    {
        var sut = CreateSut();
        var key = sut.BuildKey("temp");

        await sut.SetAsync(key, new ProductDto(2, "Temp"));
        await sut.InvalidateAsync(key);

        Assert.Null(await sut.GetAsync<ProductDto>(key));
    }

    [Fact]
    public void BuildKey_does_not_embed_key_prefix()
    {
        var sut = CreateSut(keyPrefix: "MyApp", cacheName: "odexredis");
        var key = sut.BuildKey("user:42");

        Assert.DoesNotContain("user:42", key);
        Assert.DoesNotContain("MyApp", key);
        Assert.StartsWith("odexredis:", key);
        Assert.Equal(64, key.Split(':')[^1].Length);
        Assert.All(key.Split(':')[^1], c => Assert.True(char.IsAsciiHexDigitLower(c)));
    }

    [Fact]
    public void BuildKey_same_input_produces_same_key()
    {
        var sut = CreateSut();
        Assert.Equal(sut.BuildKey("stable"), sut.BuildKey("stable"));
    }

    [Fact]
    public void BuildKey_different_inputs_produce_different_keys()
    {
        var sut = CreateSut();
        Assert.NotEqual(sut.BuildKey("a"), sut.BuildKey("b"));
    }

    [Fact]
    public void BuildKey_uses_configured_cache_name()
    {
        var sut = CreateSut(cacheName: "products");
        Assert.StartsWith("products:", sut.BuildKey("x"));
    }

    [Fact]
    public async Task ExistsAsync_returns_true_when_entry_exists()
    {
        var sut = CreateSut();
        var key = sut.BuildKey("exists");

        Assert.False(await sut.ExistsAsync(key));
        await sut.SetAsync(key, new ProductDto(1, "A"));
        Assert.True(await sut.ExistsAsync(key));
    }

    [Fact]
    public async Task GetOrSetAsync_populates_cache_on_miss()
    {
        var sut = CreateSut();
        var key = sut.BuildKey("get-or-set");
        var calls = 0;

        var first = await sut.GetOrSetAsync(key, _ =>
        {
            calls++;
            return Task.FromResult(new ProductDto(9, "Created"));
        });

        var second = await sut.GetOrSetAsync(key, _ =>
        {
            calls++;
            return Task.FromResult(new ProductDto(99, "Should not run"));
        });

        Assert.Equal(1, calls);
        Assert.Equal("Created", first.Name);
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task SetAsync_null_value_throws()
    {
        var sut = CreateSut();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.SetAsync<ProductDto>(sut.BuildKey("null"), null!));
    }

    [Fact]
    public async Task SetAsync_serializes_with_camel_case_by_default()
    {
        var options = Options.Create(new OdexRedisOptions { UseCamelCaseJson = true });
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var sut = new OdexRedis(options, cache);
        var key = sut.BuildKey("camel");

        await sut.SetAsync(key, new ProductDto(1, "X"));

        var raw = await cache.GetStringAsync(key);
        Assert.NotNull(raw);
        using var doc = JsonDocument.Parse(raw);
        Assert.True(doc.RootElement.TryGetProperty("id", out _));
        Assert.True(doc.RootElement.TryGetProperty("name", out _));
    }

    [Fact]
    public async Task SetAsync_with_custom_options_uses_sliding_expiration()
    {
        var sut = CreateSut(defaultExpirationMinutes: 60 * 24);
        var key = sut.BuildKey("sliding");

        await sut.SetAsync(
            key,
            new ProductDto(3, "Slide"),
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMilliseconds(100) });

        Assert.True(await sut.ExistsAsync(key));
        await Task.Delay(250);
        Assert.False(await sut.ExistsAsync(key));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildKey_throws_for_invalid_logical_key(string? value)
    {
        var sut = CreateSut();
        Assert.ThrowsAny<ArgumentException>(() => sut.BuildKey(value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_throws_for_invalid_key(string? key)
    {
        var sut = CreateSut();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.GetAsync<ProductDto>(key!));
    }

    [Fact]
    public void NormalizeCacheName_uses_default_when_null()
    {
        Assert.Equal(OdexRedisOptions.DefaultCacheName, OdexRedis.NormalizeCacheName(null));
    }

    [Fact]
    public void NormalizeCacheName_trims_custom_name()
    {
        Assert.Equal("custom", OdexRedis.NormalizeCacheName("  custom  "));
    }

    private sealed record ProductDto(int Id, string Name);
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Odex.AspNetCore.Caching.Redis;

/// <summary>
/// Redis-backed cache wrapper using <see cref="IDistributedCache"/> with JSON serialization and SHA-256 key hashing.
/// </summary>
public sealed class OdexRedis : IOdexRedis
{
    private readonly DistributedCacheEntryOptions _defaultEntryOptions;
    private readonly OdexRedisOptions _options;
    private readonly IDistributedCache _cache;
    private readonly string _cacheName;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>Creates a new <see cref="OdexRedis"/> instance.</summary>
    public OdexRedis(
        IOptions<OdexRedisOptions> options,
        IDistributedCache cache,
        string? cacheName = null,
        DistributedCacheEntryOptions? defaultEntryOptions = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cache);
        _options = options.Value;
        _cache = cache;
        _cacheName = NormalizeCacheName(cacheName ?? _options.CacheName);
        _jsonOptions = CreateJsonOptions(_options);
        _defaultEntryOptions = defaultEntryOptions ?? CreateDefaultEntryOptions(_options);
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var bytes = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (bytes is null or { Length: 0 })
            return default;

        return JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        var existing = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
            return existing;

        var value = await factory(cancellationToken).ConfigureAwait(false);
        if (value is null)
            throw new InvalidOperationException("Cache factory returned null; null values are not cached.");

        await SetAsync(key, value, cancellationToken).ConfigureAwait(false);
        return value;
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        DistributedCacheEntryOptions entryOptions,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(entryOptions);

        var existing = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
            return existing;

        var value = await factory(cancellationToken).ConfigureAwait(false);
        if (value is null)
            throw new InvalidOperationException("Cache factory returned null; null values are not cached.");

        await SetAsync(key, value, entryOptions, cancellationToken).ConfigureAwait(false);
        return value;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var bytes = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        return bytes is { Length: > 0 };
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) =>
        SetAsync(key, value, _defaultEntryOptions, cancellationToken);

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string key,
        T value,
        DistributedCacheEntryOptions entryOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(entryOptions);
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
        await _cache.SetAsync(key, bytes, entryOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _cache.RemoveAsync(key, cancellationToken);
    }

    /// <inheritdoc />
    public string BuildKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        return $"{_cacheName}:{hash}";
    }

    internal static string NormalizeCacheName(string? cacheName) =>
        string.IsNullOrWhiteSpace(cacheName) ? OdexRedisOptions.DefaultCacheName : cacheName.Trim();

    private static JsonSerializerOptions CreateJsonOptions(OdexRedisOptions options)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        if (options.UseCamelCaseJson)
            jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        return jsonOptions;
    }

    private static DistributedCacheEntryOptions CreateDefaultEntryOptions(OdexRedisOptions options)
    {
        if (options.DefaultExpirationMinutes <= 0)
            return new DistributedCacheEntryOptions();

        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.DefaultExpirationMinutes)
        };
    }
}

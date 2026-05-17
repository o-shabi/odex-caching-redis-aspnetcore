using Microsoft.Extensions.Caching.Distributed;

namespace Odex.AspNetCore.Caching.Redis;

/// <summary>
/// High-level Redis cache operations with JSON serialization and deterministic key hashing.
/// </summary>
public interface IOdexRedis
{
    /// <summary>Retrieves and deserializes a value. Returns <c>default</c> when the key is missing.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a cached value when present; otherwise invokes <paramref name="factory"/>, stores the result, and returns it.
    /// Intended for reference types (<c>where T : class</c>).
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Returns a cached value when present; otherwise invokes <paramref name="factory"/>, stores with custom options, and returns it.
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        DistributedCacheEntryOptions entryOptions,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Returns <c>true</c> when the key exists and has a non-empty payload.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Stores a value using the configured default expiration.</summary>
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    /// <summary>Stores a value with custom cache entry options.</summary>
    Task SetAsync<T>(
        string key,
        T value,
        DistributedCacheEntryOptions entryOptions,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a key from the cache.</summary>
    Task InvalidateAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a deterministic hashed key segment (<c>{cacheName}:{sha256}</c>).
    /// The configured <c>KeyPrefix</c> is applied by Redis via <c>InstanceName</c>, not in this string.
    /// </summary>
    string BuildKey(string value);
}

namespace Odex.AspNetCore.Caching.Redis;

/// <summary>Configuration bound from the <c>Redis</c> configuration section.</summary>
public class OdexRedisOptions
{
    /// <summary>Configuration section name (<c>Redis</c>).</summary>
    public const string SectionName = "Redis";

    /// <summary>Configuration key for the Redis connection string (<c>Redis:Configuration</c>).</summary>
    public const string ConfigurationSectionName = $"{SectionName}:Configuration";

    /// <summary>
    /// Default cache namespace segment used in <see cref="OdexRedis.BuildKey"/> when no custom name is supplied.
    /// </summary>
    public const string DefaultCacheName = "odexredis";

    /// <summary>
    /// Connection string (e.g. <c>localhost:6379</c> or <c>host:6380,password=...,ssl=True</c>).
    /// When null or empty, <see cref="ServiceCollectionExtensions.AddOdexRedis(IServiceCollection, IConfiguration)"/> skips registration.
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Application-level Redis key prefix (StackExchange <c>InstanceName</c>).
    /// Not repeated inside keys returned by <see cref="IOdexRedis.BuildKey"/>.
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Namespace segment in <see cref="IOdexRedis.BuildKey"/> (default: <see cref="DefaultCacheName"/>).
    /// </summary>
    public string CacheName { get; set; } = DefaultCacheName;

    /// <summary>Default absolute expiration in minutes when no explicit entry options are provided. Use <c>0</c> for no expiration.</summary>
    public int DefaultExpirationMinutes { get; set; } = 60 * 24;

    /// <summary>When <c>true</c>, JSON properties are serialized using camelCase.</summary>
    public bool UseCamelCaseJson { get; set; } = true;
}

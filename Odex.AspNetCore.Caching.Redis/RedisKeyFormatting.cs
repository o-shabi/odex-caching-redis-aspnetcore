namespace Odex.AspNetCore.Caching.Redis;

internal static class RedisKeyFormatting
{
    /// <summary>
    /// Formats <see cref="OdexRedisOptions.KeyPrefix"/> for StackExchange Redis <c>InstanceName</c>
    /// (ensures a trailing colon separator when a prefix is configured).
    /// </summary>
    public static string? FormatInstanceName(string? keyPrefix)
    {
        if (string.IsNullOrWhiteSpace(keyPrefix))
            return null;

        var trimmed = keyPrefix.Trim();
        return trimmed.EndsWith(':') ? trimmed : $"{trimmed}:";
    }
}

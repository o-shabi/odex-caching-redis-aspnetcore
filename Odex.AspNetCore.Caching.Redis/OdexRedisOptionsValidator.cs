using Microsoft.Extensions.Options;

namespace Odex.AspNetCore.Caching.Redis;

internal sealed class OdexRedisOptionsValidator : IValidateOptions<OdexRedisOptions>
{
    public ValidateOptionsResult Validate(string? name, OdexRedisOptions options)
    {
        if (options.DefaultExpirationMinutes < 0)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(OdexRedisOptions.DefaultExpirationMinutes)} must be greater than or equal to 0.");
        }

        if (string.IsNullOrWhiteSpace(options.CacheName))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(OdexRedisOptions.CacheName)} must be a non-empty string.");
        }

        return ValidateOptionsResult.Success;
    }
}

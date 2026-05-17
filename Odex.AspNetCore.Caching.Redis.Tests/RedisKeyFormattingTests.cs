namespace Odex.AspNetCore.Caching.Redis.Tests;

public sealed class RedisKeyFormattingTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("MyApp", "MyApp:")]
    [InlineData("MyApp:", "MyApp:")]
    public void FormatInstanceName_formats_expected(string? prefix, string? expected)
    {
        Assert.Equal(expected, RedisKeyFormatting.FormatInstanceName(prefix));
    }
}

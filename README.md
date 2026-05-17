# Odex.AspNetCore.Caching.Redis

[![NuGet](https://img.shields.io/nuget/v/Odex.AspNetCore.Caching.Redis)](https://www.nuget.org/packages/Odex.AspNetCore.Caching.Redis)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Odex.AspNetCore.Caching.Redis)](https://www.nuget.org/packages/Odex.AspNetCore.Caching.Redis)
[![CI](https://github.com/o-shabi/odex-caching-redis-aspnetcore/actions/workflows/ci.yml/badge.svg)](https://github.com/o-shabi/odex-caching-redis-aspnetcore/actions/workflows/ci.yml)
[![Release](https://github.com/o-shabi/odex-caching-redis-aspnetcore/actions/workflows/release.yml/badge.svg)](https://github.com/o-shabi/odex-caching-redis-aspnetcore/actions/workflows/release.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Opinionated **Redis caching** for **ASP.NET Core** on top of `IDistributedCache` (StackExchange.Redis). Get JSON serialization, SHA-256 key hashing, configurable TTL, cache-aside helpers, and optional registration when Redis is not configured.

---

## Table of contents

- [About](#about)
- [Installation](#installation)
- [Requirements](#requirements)
- [Configuration](#configuration)
- [Quick start](#quick-start)
- [Capabilities](#capabilities)
- [Key format](#key-format)
- [Examples](#examples)
- [API reference](#api-reference)
- [Testing without Redis](#testing-without-redis)
- [Upgrading](#upgrading)
- [CI/CD](#cicd)
- [Contributing](#contributing)
- [License](#license)
- [Links](#links)

---

## About

**Odex.AspNetCore.Caching.Redis** wraps `IDistributedCache` with a small, async-first surface area: `GetAsync`, `SetAsync`, `InvalidateAsync`, `GetOrSetAsync`, and `ExistsAsync`. Logical keys are hashed with SHA-256 so Redis keys stay short and safe. Application isolation uses a configurable **key prefix** (StackExchange `InstanceName`) plus a **cache namespace** segment in `BuildKey`.

Registration is **conditional**: if `Redis:Configuration` is missing or empty, `AddOdexRedis` does not register services—useful for local development or feature flags.

---

## Installation

```bash
dotnet add package Odex.AspNetCore.Caching.Redis
```

Pin a version for reproducible builds:

```bash
dotnet add package Odex.AspNetCore.Caching.Redis --version 0.2.0
```

---

## Requirements

| Item | Version |
|------|---------|
| **Target framework** | `net9.0` (.NET 9 and later) |
| **.NET SDK** | 9.x |
| **Redis** | Any server compatible with StackExchange.Redis |

**NuGet dependencies:** `Microsoft.Extensions.Caching.StackExchangeRedis`, `Microsoft.Extensions.Options.ConfigurationExtensions`. StackExchange.Redis is pulled transitively.

---

## Configuration

Add a `Redis` section to `appsettings.json` (or environment variables / secrets):

```json
{
  "Redis": {
    "Configuration": "localhost:6379,abortConnect=false",
    "KeyPrefix": "MyApp",
    "CacheName": "odexredis",
    "DefaultExpirationMinutes": 60,
    "UseCamelCaseJson": true
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `Configuration` | — | Connection string. If null or empty, Redis is not registered. |
| `KeyPrefix` | `""` | Redis `InstanceName` prefix (a trailing `:` is added when set). |
| `CacheName` | `odexredis` | Namespace segment in `BuildKey`. |
| `DefaultExpirationMinutes` | `1440` | Default absolute TTL in minutes (`0` = no default expiration). |
| `UseCamelCaseJson` | `true` | Serialize JSON with camelCase property names. |

Environment variable example: `Redis__Configuration=host:6379,password=...`

---

## Quick start

**1. Register services** in `Program.cs`:

```csharp
using Odex.AspNetCore.Caching.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOdexRedis(builder.Configuration);
var app = builder.Build();
```

**2. Inject `IOdexRedis`** and use hashed keys:

```csharp
public class ProductService(IOdexRedis cache)
{
    public async Task<Product?> GetProductAsync(int id, CancellationToken ct = default)
    {
        var key = cache.BuildKey($"product:{id}");
        return await cache.GetAsync<Product>(key, ct);
    }

    public async Task SetProductAsync(int id, Product product, CancellationToken ct = default)
    {
        var key = cache.BuildKey($"product:{id}");
        await cache.SetAsync(key, product, ct);
    }
}
```

---

## Capabilities

| Area | Summary |
|------|---------|
| **Cache operations** | `GetAsync`, `SetAsync`, `InvalidateAsync`, `ExistsAsync` |
| **Cache-aside** | `GetOrSetAsync<T>` where `T : class` |
| **Keys** | `BuildKey` → `{cacheName}:{sha256_hex}`; prefix via `KeyPrefix` / `InstanceName` |
| **Serialization** | System.Text.Json with optional camelCase |
| **Expiration** | Default TTL from config or per-entry `DistributedCacheEntryOptions` |
| **DI** | `AddOdexRedis(IConfiguration)` and optional `Action<OdexRedisOptions>` |
| **Validation** | `OdexRedisOptions` validated on startup when Redis is enabled |

---

## Key format

`BuildKey` returns the key passed to `IDistributedCache`:

```text
{cacheName}:{sha256_hex_lowercase}
```

`KeyPrefix` is applied by Redis as `InstanceName`, not inside `BuildKey`:

| `KeyPrefix` | `BuildKey("user:42")` | Approximate Redis key |
|-------------|------------------------|------------------------|
| `MyApp` | `odexredis:abc…` | `MyApp:odexredis:abc…` |
| `""` | `odexredis:abc…` | `odexredis:abc…` |

---

## Examples

### Custom expiration

```csharp
var options = new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
    SlidingExpiration = TimeSpan.FromMinutes(2)
};
await cache.SetAsync(key, data, options, cancellationToken);
```

### Get-or-set (cache-aside)

```csharp
var product = await cache.GetOrSetAsync(
    cache.BuildKey($"product:{id}"),
    ct => repository.GetProductAsync(id, ct),
    cancellationToken);
```

### Optional Redis (e.g. local development)

When `Redis:Configuration` is empty, resolve `IOdexRedis` optionally:

```csharp
public class MyService(IServiceProvider services)
{
    private readonly IOdexRedis? _cache = services.GetService<IOdexRedis>();

    public async Task DoWorkAsync(CancellationToken ct)
    {
        if (_cache is null) return;
        await _cache.SetAsync(_cache.BuildKey("key"), "value", ct);
    }
}
```

### Post-configure options

```csharp
builder.Services.AddOdexRedis(builder.Configuration, options =>
{
    options.CacheName = "catalog";
    options.DefaultExpirationMinutes = 30;
});
```

---

## API reference

### `IOdexRedis`

| Member | Description |
|--------|-------------|
| `GetAsync<T>` | Deserialize value; `default` if missing. |
| `GetOrSetAsync<T>` | Cache-aside for reference types. |
| `ExistsAsync` | `true` when the key has a non-empty payload. |
| `SetAsync<T>` | Store with default or custom expiration. |
| `InvalidateAsync` | Remove a key. |
| `BuildKey` | Deterministic hashed key segment. |

### `ServiceCollectionExtensions`

| Method | Description |
|--------|-------------|
| `AddOdexRedis(IServiceCollection, IConfiguration)` | Registers Redis, options, and `IOdexRedis` when configured. |
| `AddOdexRedis(..., Action<OdexRedisOptions>?)` | Same, with optional options override. |

XML documentation is included in the NuGet package for IntelliSense.

---

## Testing without Redis

Use `MemoryDistributedCache` in tests:

```csharp
services.AddDistributedMemoryCache();
services.Configure<OdexRedisOptions>(o =>
{
    o.KeyPrefix = "Test";
    o.DefaultExpirationMinutes = 5;
});
services.AddSingleton<IOdexRedis, OdexRedis>();
```

See **Odex.AspNetCore.Caching.Redis.Tests** in this repository for unit tests.

---

## Upgrading

See the **[changelog](https://github.com/o-shabi/odex-caching-redis-aspnetcore/blob/main/CHANGELOG.md)** for version history and breaking changes.

**0.1.x → 0.2.0 (summary):**

- `AddOdexRedis` now registers `IOdexRedis`.
- `BuildKey` no longer embeds `KeyPrefix` (avoids double-prefix with `InstanceName`).
- `SetAsync` overload uses `DistributedCacheEntryOptions` instead of `object`.
- Prefer `ServiceCollectionExtensions` over obsolete `ServiceCollectionExtension`.

---

## CI/CD

| Workflow | File | Purpose |
|----------|------|---------|
| **CI** | [`.github/workflows/ci.yml`](.github/workflows/ci.yml) | Build and test on push/PR to `main`. |
| **Release** | [`.github/workflows/release.yml`](.github/workflows/release.yml) | Pack and publish to NuGet.org on version tags `v*.*.*`. |

---

## Contributing

See **[CONTRIBUTING.md](CONTRIBUTING.md)** for local setup, pull-request guidelines, and release process.

---

## License

This project is released under the [MIT License](LICENSE).

Copyright (c) Asen O'Shabi.

---

## Links

| Resource | URL |
|----------|-----|
| NuGet Gallery | https://www.nuget.org/packages/Odex.AspNetCore.Caching.Redis |
| Changelog | https://github.com/o-shabi/odex-caching-redis-aspnetcore/blob/main/CHANGELOG.md |
| Source / issues | https://github.com/o-shabi/odex-caching-redis-aspnetcore |

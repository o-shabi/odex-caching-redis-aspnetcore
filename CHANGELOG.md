# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-05-17

### Added

- xUnit test project (`Odex.AspNetCore.Caching.Redis.Tests`)
- Targets `net9.0` (.NET 9 and later)
- GitHub Actions release workflow (NuGet publish on version tags)
- `ServiceCollectionExtensions` (preferred DI entry type)
- `GetOrSetAsync<T>` cache-aside helper (reference types)
- `ExistsAsync` for key presence without deserialization
- `OdexRedisOptions.CacheName` and `UseCamelCaseJson`
- `AddOdexRedis` overload with `Action<OdexRedisOptions>` post-configuration
- Options validation on startup (`OdexRedisOptionsValidator`)
- XML documentation on public API
- GitHub Actions CI workflow
- Source Link via `Microsoft.SourceLink.GitHub`
- Root-level README, LICENSE, and repository metadata

### Fixed

- **`AddOdexRedis` now registers `IOdexRedis`** (previously only registered `IDistributedCache`)
- **`KeyPrefix` no longer duplicated** — `BuildKey` returns `{cacheName}:{hash}`; prefix is applied only via Redis `InstanceName`
- `BuildKey` no longer produces a leading colon when `KeyPrefix` is empty
- Invalid `SetAsync` entry options no longer silently drop expiration (API now uses `DistributedCacheEntryOptions`)
- Stable default cache namespace (`odexredis`) instead of fragile type-name parsing
- SHA-256 hashes use lowercase hex for consistent keys

### Changed

- `SetAsync` overload now takes `DistributedCacheEntryOptions` instead of `object`
- `OdexRedis` is `sealed`; JSON serializer options are static
- Argument validation for null/empty keys
- Package tags and metadata updated for NuGet

### Deprecated

- `ServiceCollectionExtension` — use `ServiceCollectionExtensions` instead

## [0.1.2] and earlier

See git history and NuGet package release notes.

[0.2.0]: https://github.com/o-shabi/odex-caching-redis-aspnetcore/compare/v0.1.2...v0.2.0

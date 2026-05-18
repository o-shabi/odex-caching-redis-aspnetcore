# Security policy

## Supported versions

| Version   | Supported |
|-----------|-----------|
| **0.2.x** | Yes       |
| **< 0.2** | Upgrade recommended |

## Reporting a vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

Report privately so a fix can be prepared before wider disclosure:

1. **[GitHub Security Advisory](https://github.com/o-shabi/odex-caching-redis-aspnetcore/security/advisories/new)** for this repository (preferred when enabled), or
2. Another private channel agreed with the maintainers.

Include:

- Affected **package version(s)** and **.NET** runtime
- Steps to reproduce or a minimal proof of concept
- Potential impact (confidentiality, integrity, availability)

Maintainers should acknowledge receipt within a reasonable time and coordinate a fix, release, and advisory following responsible disclosure where applicable.

## Scope

This repository ships a **.NET class library** consumed as a dependency. Issues in **your** host, Redis deployment, or transitive packages are generally out of scope for this project’s advisory process, but problems in **this package’s code or published artifacts** are in scope.

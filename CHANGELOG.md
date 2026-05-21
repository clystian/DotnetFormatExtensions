# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.8.1-beta] - 2026-05-21

### Added
- README included in NuGet package for better documentation on nuget.org

### Infrastructure
- Version bump script (`scripts/bump-version.ps1`) for consistent releases
- SemVer tag validation in release workflow
- `CHANGELOG.md` for tracking release history

## [0.8.0-beta] - 2026-05-21

### Added
- FMT0001 analyzer: enforces multi-line formatting of C# initializers
- Code fix provider: auto-fixes single-line initializers via `dotnet format`
- Support for object, collection, array, and jagged array initializers
- Nested initializer expansion (recursive multi-line formatting)
- Edge case handling:
  - Single-element initializers ignored (no false positives)
  - Multidimensional array inner initializers preserved
  - Implicit object creation (`new()`) supported
- CI/CD pipeline with GitHub Actions
- Demo project with before/after examples

[0.8.1-beta]: https://github.com/clystian/DotnetFormatExtensions/compare/v0.8.0-beta...v0.8.1-beta
[0.8.0-beta]: https://github.com/clystian/DotnetFormatExtensions/releases/tag/v0.8.0-beta

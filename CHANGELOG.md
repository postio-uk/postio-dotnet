# Changelog

All notable changes to `Postio.Sdk` are documented here. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), versioning
follows [SemVer](https://semver.org/).

## [Unreleased]

## [0.1.2] — 2026-08-12

### Removed

- `county` from the address model. It came from ONS administrative geography
  rather than Royal Mail data, resolved for only part of the country, and is
  not part of a correct UK postal address — Royal Mail removed postal counties
  from PAF in December 2000. `district` and `ward` remain, and are ONS
  administrative geography rather than postal fields.

## [0.1.1] — 2026-05-02

### Changed

- `PhoneResult.IsReachable` typed `bool?` (was `object?`). postio-api
  1.0.3 aligned the spec with the runtime — HLR returns boolean.
- `PhoneResult` nullable parameters drop their `= null` defaults.
  The runtime now always emits explicit nulls for every field.

## [0.1.0] — 2026-05-02

Initial release. First Postio .NET SDK on NuGet.

### Added

- `PostioClient` — async-first via `HttpClient`. Single sync surface
  (`Task<T>` returns); .NET ecosystem standard.
- Address: `client.Address.SearchAsync/PostcodeAsync/UdprnAsync`.
- Email: `client.Email.ValidateAsync`.
- Phone: `client.Phone.ValidateAsync`.
- Health probe: `client.ConnectAsync()`.
- Immutable `record` types for every response shape.
- Typed exception hierarchy: `PostioException` base + 9 subclasses
  (`PostioInvalidKeyException`, `PostioOutOfCreditException`,
  `PostioForbiddenException`, `PostioNotFoundException`,
  `PostioValidationException`, `PostioRateLimitException`,
  `PostioServerException`, `PostioTimeoutException`,
  `PostioConnectionException`). Each carries `Status`, `ErrorCode`,
  `Details`, `RequestId`, `Envelope`.
- Default retry (2x exp backoff full jitter on 408/409/429/5xx +
  network/timeout). Mirrors `@postio/node`.
- Source-only `System.Text.Json` for serialization. No external runtime
  dependencies.
- Inject your own `HttpClient` (proxies, `IHttpClientFactory`, mocks).
- `POSTIO_API_KEY` env var fallback when `ApiKey` is not set on options.

### Notes

- `PhoneResult.IsReachable` is typed `object?` because the live API
  returns booleans there even though the spec says string-only.
  Aligned once postio-api ships a spec/runtime fix.

[Unreleased]: https://github.com/postio-uk/postio-dotnet/compare/v0.1.1...HEAD
[0.1.1]: https://github.com/postio-uk/postio-dotnet/releases/tag/v0.1.1
[0.1.0]: https://github.com/postio-uk/postio-dotnet/releases/tag/v0.1.0

# postio-dotnet — development notes

.NET SDK for the [Postio API](https://postio.co.uk). Mirrors `@postio/core`
(the JS family's runtime client) with idiomatic .NET async-first
ergonomics. Lives in its own repo because the .NET toolchain doesn't
co-exist with the JS-family pnpm workspace.

Read [`README.md`](./README.md) for the customer-facing surface; this
file is the operational guide for contributors and code agents.

## Stack

- **.NET 8 LTS** target. Modern C# (records, primary ctors, file-scoped
  namespaces, `required` keyword).
- **HTTP**: `HttpClient`. Inject your own (e.g. via
  `IHttpClientFactory`) or let the SDK manage one.
- **JSON**: `System.Text.Json` with explicit `[JsonPropertyName]`
  attributes (no source generators yet — small surface).
- **Tests**: xUnit + RichardSzalay.MockHttp for the offline tests;
  live tests tagged with `[Trait("Category", "Live")]` so the offline
  CI run filters them out.

## Layout

```
postio-dotnet/
├── Postio.Sdk.sln
├── src/Postio.Sdk/
│   ├── Postio.Sdk.csproj      NuGet metadata + symbol pkg config
│   ├── PostioClient.cs        client + AddressResource/EmailResource/PhoneResource
│   ├── PostioClientOptions.cs constructor options
│   ├── Exceptions.cs          PostioException + 9 typed subclasses
│   └── Models/Models.cs       all response records + ErrorEnvelope
├── tests/Postio.Sdk.Tests/
│   ├── Postio.Sdk.Tests.csproj
│   ├── PostioClientTests.cs   offline (MockHttp)
│   └── LiveTests.cs           Trait("Category", "Live"); skipped if no key
├── README.md / CLAUDE.md / LICENSE / CHANGELOG.md
└── .github/workflows/
    ├── ci.yml                 build + offline tests on push
    └── release.yml            tag-driven, OIDC trusted publishing on NuGet
```

## Common commands

```bash
dotnet restore
dotnet build -c Release
dotnet test --filter "Category!=Live"        # offline only
set -a && source ../.env && set +a
dotnet test                                    # offline + live
dotnet pack src/Postio.Sdk/Postio.Sdk.csproj -c Release -o artifacts
```

## Branch + deploy model

- `stage` — working branch.
- `master` — push triggers the live-test job. Tag `vX.Y.Z` → release
  workflow → NuGet publish via Trusted Publishers (OIDC).
- `release.yml` is **idempotent** (skips if `Postio.Sdk` at that
  version is already on api.nuget.org).

## Spec ↔ runtime alignment

As of postio-api 1.0.3 the OpenAPI spec and runtime are aligned —
`PhoneResult.IsReachable` is `bool?` (was `object?`) and the runtime
always emits explicit nulls for every nullable field. If a future spec
change re-introduces drift, prefer fixing it at the source (postio-api
Zod schemas + handlers) over patching downstream.

## Secrets the CI needs

| Secret | Used by |
|---|---|
| `POSTIO_API_KEY_STAGE` | live-test job in `ci.yml`. |

NuGet publish uses **Trusted Publishers (OIDC)** — no `NUGET_API_KEY`
secret. Configure once at:
https://www.nuget.org/account/Manage/TrustedPublishers
Bind to `(postio-uk/postio-dotnet, release.yml, environment: nuget)`.


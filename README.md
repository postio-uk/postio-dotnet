# Postio .NET SDK

[![NuGet](https://img.shields.io/nuget/v/Postio.Sdk.svg)](https://www.nuget.org/packages/Postio.Sdk)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://www.nuget.org/packages/Postio.Sdk)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

.NET SDK for [Postio](https://postio.co.uk) — the UK validation API for
addresses, emails and phone numbers. Async-first via `HttpClient`, immutable
`record` types, source-only `System.Text.Json`. Backed by Royal Mail PAF and
Ordnance Survey.

> **First time?** [Sign up free](https://postio.co.uk) — first 100 lookups on us, no card needed.

## Install

```bash
dotnet add package Postio.Sdk
```

Targets `net8.0`+.

## 30-second example

```csharp
using Postio.Sdk;

using var client = new PostioClient("pk_...");  // or read POSTIO_API_KEY env

var result = await client.Address.SearchAsync("downing street");
foreach (var hit in result.Results)
    Console.WriteLine($"{hit.Udprn}: {hit.Suggestion}");

Console.WriteLine($"request id: {result.Meta.RequestId}");
```

## API

| Method | Returns |
|---|---|
| `client.Address.SearchAsync(q, maxResults?)` | `AddressSearchEnvelope` |
| `client.Address.PostcodeAsync(postcode, maxResults?)` | `AddressPostcodeEnvelope` |
| `client.Address.UdprnAsync(udprn)` | `AddressUdprnEnvelope` |
| `client.Email.ValidateAsync(address)` | `EmailEnvelope` |
| `client.Phone.ValidateAsync(number)` | `PhoneEnvelope` |
| `client.ConnectAsync()` | `ConnectSuccess` |

Every method takes an optional `CancellationToken`. All return types are
immutable `record`s.

## Errors

Every non-2xx response throws a typed exception. `PostioException` is the
base; subclasses match HTTP status:

```csharp
try
{
    await client.Address.PostcodeAsync("not-a-postcode");
}
catch (PostioInvalidKeyException e)         { /* 401 */ }
catch (PostioOutOfCreditException e)        { /* 402 */ }
catch (PostioRateLimitException e)          { /* 429 — e.RetryAfter */ }
catch (PostioValidationException e)         { /* 400 / 422 */ }
catch (PostioServerException e)             { /* 5xx */ }
catch (PostioException e)
{
    Console.WriteLine($"{e.Status} {e.ErrorCode} ({e.RequestId}): {e.Details}");
}
```

Every exception carries `Status`, `ErrorCode`, `Details`, `RequestId`, and
the raw `Envelope`.

## Configuration

```csharp
var client = new PostioClient(new PostioClientOptions
{
    ApiKey         = "pk_...",
    BaseUrl        = "https://api.postio.co.uk/v1",       // default
    Timeout        = TimeSpan.FromSeconds(10),             // default
    Retries        = 2,                                    // 0 disables
    RetryBaseDelay = TimeSpan.FromMilliseconds(500),
    RetryCapDelay  = TimeSpan.FromSeconds(8),
});
client.GetType();  // pretend this isn't here

// Or inject your own HttpClient (proxies, custom handlers, IHttpClientFactory):
using var http = new HttpClient();
using var c = new PostioClient(options, http);
```

Default retry policy: 2 retries on 408/409/429/5xx + network/timeout,
exponential backoff with full jitter (500ms → 8s cap).

## ASP.NET Core / DI

Register once via `IHttpClientFactory`:

```csharp
builder.Services.AddHttpClient("postio");
builder.Services.AddSingleton(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("postio");
    return new PostioClient(new PostioClientOptions
    {
        ApiKey = builder.Configuration["Postio:ApiKey"],
    }, http);
});
```

## Links

- [Docs](https://postio.co.uk/docs)
- [API reference (OpenAPI)](https://postio.co.uk/openapi.json)
- [Changelog](./CHANGELOG.md)
- [Issues](https://github.com/postio-uk/postio-dotnet/issues)

## License

MIT — see [LICENSE](./LICENSE).

> *Postio is a trading name of Onno Group Limited, registered in
> England & Wales (company no. 08622799). Registered office:
> Suite 22 Trym Lodge, 1 Henbury Road, Westbury-On-Trym, Bristol BS9 3HQ.*

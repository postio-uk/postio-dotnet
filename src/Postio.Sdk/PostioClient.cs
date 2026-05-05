using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using Postio.Sdk.Models;

namespace Postio.Sdk;

/// <summary>
/// Postio API client. Async-first via <see cref="HttpClient"/>.
/// </summary>
/// <example>
/// <code>
/// var client = new PostioClient(new() { ApiKey = "pk_..." });
/// var result = await client.Address.SearchAsync("downing street");
/// foreach (var hit in result.Results)
///     Console.WriteLine($"{hit.Udprn}: {hit.Suggestion}");
/// </code>
/// </example>
public sealed class PostioClient : IDisposable
{
    private static readonly string Version =
        typeof(PostioClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0]
        ?? typeof(PostioClient).Assembly.GetName().Version?.ToString(3)
        ?? "0.1.0";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = null, // we use explicit JsonPropertyName attributes
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly HashSet<int> RetryableStatuses = new() { 408, 409, 429, 500, 502, 503, 504 };

    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly PostioClientOptions _options;

    public AddressResource Address { get; }
    public EmailResource Email { get; }
    public PhoneResource Phone { get; }

    public PostioClient(PostioClientOptions options, HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var key = options.ApiKey ?? Environment.GetEnvironmentVariable("POSTIO_API_KEY");
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Postio: api key is required (set ApiKey or POSTIO_API_KEY env var).", nameof(options));
        }

        _apiKey = key;
        _baseUrl = options.BaseUrl.TrimEnd('/');
        _options = options;

        if (httpClient is null)
        {
            _http = new HttpClient { Timeout = options.Timeout };
            _ownsHttp = true;
        }
        else
        {
            _http = httpClient;
            _ownsHttp = false;
        }

        Address = new AddressResource(this);
        Email = new EmailResource(this);
        Phone = new PhoneResource(this);
    }

    /// <summary>Convenience constructor: just an API key.</summary>
    public PostioClient(string apiKey) : this(new PostioClientOptions { ApiKey = apiKey }) { }

    /// <summary>Health probe — confirms the API is reachable and the key is valid.</summary>
    public async Task<ConnectSuccess> ConnectAsync(CancellationToken cancellationToken = default)
        => (await RequestAsync<ConnectSuccess>("/connect", null, cancellationToken).ConfigureAwait(false))!;

    internal async Task<T> RequestAsync<T>(string path, IDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var url = BuildUrl(path, query);
        var maxAttempts = _options.Retries + 1;
        Exception? lastException = null;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("x-api-key", _apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.UserAgent.ParseAdd($"postio-dotnet/{Version}");
                request.Headers.TryAddWithoutValidation("x-postio-client", $"postio-dotnet/{Version}");
                foreach (var (k, v) in _options.Headers)
                {
                    request.Headers.TryAddWithoutValidation(k, v);
                }

                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
                var bodyText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

                if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    throw new PostioException(
                        $"Unexpected response content-type: {contentType}",
                        status: (int)response.StatusCode,
                        errorCode: "unexpected_content_type",
                        details: bodyText.Length > 500 ? bodyText[..500] : bodyText);
                }

                if (response.IsSuccessStatusCode)
                {
                    var parsed = JsonSerializer.Deserialize<T>(bodyText, JsonOpts)
                        ?? throw new PostioException("Response body deserialised to null.", status: (int)response.StatusCode, errorCode: "parse_error");
                    return parsed;
                }

                var envelope = TryParseEnvelope(bodyText);
                var status = (int)response.StatusCode;

                if (RetryableStatuses.Contains(status) && attempt < maxAttempts - 1)
                {
                    lastException = BuildException(status, envelope, response);
                    await Task.Delay(BackoffDelay(attempt), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw BuildException(status, envelope, response);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = new PostioTimeoutException("Request timed out.", ex);
                if (attempt == maxAttempts - 1) throw lastException;
                await Task.Delay(BackoffDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                lastException = new PostioConnectionException($"Network error: {ex.Message}", ex);
                if (attempt == maxAttempts - 1) throw lastException;
                await Task.Delay(BackoffDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new PostioException("Postio: retry loop exhausted unexpectedly.");
    }

    private string BuildUrl(string path, IDictionary<string, string?>? query)
    {
        var url = _baseUrl + path;
        if (query is null) return url;

        var pairs = new List<string>();
        foreach (var (k, v) in query)
        {
            if (v is null) continue;
            pairs.Add($"{Uri.EscapeDataString(k)}={Uri.EscapeDataString(v)}");
        }
        return pairs.Count == 0 ? url : $"{url}?{string.Join("&", pairs)}";
    }

    private static ErrorEnvelope? TryParseEnvelope(string body)
    {
        try { return JsonSerializer.Deserialize<ErrorEnvelope>(body, JsonOpts); }
        catch { return null; }
    }

    private static PostioException BuildException(int status, ErrorEnvelope? envelope, HttpResponseMessage response)
    {
        var error = envelope?.Error;
        var details = envelope?.Details;
        var requestId = envelope?.Meta?.RequestId;
        var message = error ?? $"HTTP {status}";

        return status switch
        {
            401 => new PostioInvalidKeyException(message, error, details, requestId, envelope),
            402 => new PostioOutOfCreditException(message, error, details, requestId, envelope),
            403 => new PostioForbiddenException(message, error, details, requestId, envelope),
            404 => new PostioNotFoundException(message, error, details, requestId, envelope),
            400 or 422 => new PostioValidationException(message, status, error, details, requestId, envelope),
            429 => new PostioRateLimitException(
                message, error, details, requestId, envelope,
                retryAfter: ParseRetryAfter(response.Headers.RetryAfter)),
            >= 500 and < 600 => new PostioServerException(message, status, error, details, requestId, envelope),
            _ => new PostioException(message, status, error, details, requestId, envelope),
        };
    }

    private static double? ParseRetryAfter(RetryConditionHeaderValue? header)
    {
        if (header is null) return null;
        if (header.Delta.HasValue) return header.Delta.Value.TotalSeconds;
        if (header.Date.HasValue)
        {
            var span = header.Date.Value - DateTimeOffset.UtcNow;
            return span.TotalSeconds > 0 ? span.TotalSeconds : null;
        }
        return null;
    }

    private TimeSpan BackoffDelay(int attempt)
    {
        var exp = TimeSpan.FromMilliseconds(Math.Min(
            _options.RetryCapDelay.TotalMilliseconds,
            _options.RetryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt)));
        if (exp.TotalMilliseconds <= 0) return TimeSpan.Zero;
        return TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * exp.TotalMilliseconds);
    }

    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }
}

/// <summary>/address/* endpoints.</summary>
public sealed class AddressResource
{
    private readonly PostioClient _client;
    internal AddressResource(PostioClient client) => _client = client;

    public Task<AddressSearchEnvelope> SearchAsync(string q, int? maxResults = null, CancellationToken cancellationToken = default)
        => _client.RequestAsync<AddressSearchEnvelope>("/address/search", new Dictionary<string, string?>
        {
            ["q"] = q,
            ["max_results"] = maxResults?.ToString(),
        }, cancellationToken);

    public Task<AddressPostcodeEnvelope> PostcodeAsync(string postcode, int? maxResults = null, CancellationToken cancellationToken = default)
        => _client.RequestAsync<AddressPostcodeEnvelope>($"/address/postcode/{Uri.EscapeDataString(postcode)}",
            new Dictionary<string, string?> { ["max_results"] = maxResults?.ToString() }, cancellationToken);

    public Task<AddressUdprnEnvelope> UdprnAsync(int udprn, CancellationToken cancellationToken = default)
        => _client.RequestAsync<AddressUdprnEnvelope>($"/address/udprn/{udprn}", null, cancellationToken);
}

/// <summary>/email/* endpoints.</summary>
public sealed class EmailResource
{
    private readonly PostioClient _client;
    internal EmailResource(PostioClient client) => _client = client;

    public Task<EmailEnvelope> ValidateAsync(string address, CancellationToken cancellationToken = default)
        => _client.RequestAsync<EmailEnvelope>($"/email/{Uri.EscapeDataString(address)}", null, cancellationToken);
}

/// <summary>/phone/* endpoints.</summary>
public sealed class PhoneResource
{
    private readonly PostioClient _client;
    internal PhoneResource(PostioClient client) => _client = client;

    public Task<PhoneEnvelope> ValidateAsync(string number, CancellationToken cancellationToken = default)
        => _client.RequestAsync<PhoneEnvelope>($"/phone/{Uri.EscapeDataString(number)}", null, cancellationToken);
}

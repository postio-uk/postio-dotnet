namespace Postio.Sdk;

/// <summary>Options for <see cref="PostioClient"/>.</summary>
public sealed class PostioClientOptions
{
    /// <summary>API key (required if not set via POSTIO_API_KEY env var).</summary>
    public string? ApiKey { get; set; }

    /// <summary>API base URL. Default: https://api.postio.co.uk/v1.</summary>
    public string BaseUrl { get; set; } = "https://api.postio.co.uk/v1";

    /// <summary>Per-request timeout. Default: 10 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Retries on 408/409/429/5xx + network errors. Default 2.</summary>
    public int Retries { get; set; } = 2;

    /// <summary>Base delay for exponential backoff. Default 500ms.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Cap on individual backoff delay. Default 8s.</summary>
    public TimeSpan RetryCapDelay { get; set; } = TimeSpan.FromSeconds(8);

    /// <summary>Extra headers merged into every request.</summary>
    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();
}

using Postio.Sdk.Models;

namespace Postio.Sdk;

/// <summary>Base class for every Postio API failure.</summary>
public class PostioException : Exception
{
    /// <summary>HTTP status code (0 for transport errors).</summary>
    public int Status { get; }

    /// <summary>API error code (e.g. "invalid_api_key"). Empty for transport-level errors.</summary>
    public string? ErrorCode { get; }

    /// <summary>Optional human-readable details from the envelope.</summary>
    public string? Details { get; }

    /// <summary>Request ID — quote in support tickets.</summary>
    public string? RequestId { get; }

    /// <summary>Raw decoded error envelope, if the API returned one.</summary>
    public ErrorEnvelope? Envelope { get; }

    public PostioException(
        string message,
        int status = 0,
        string? errorCode = null,
        string? details = null,
        string? requestId = null,
        ErrorEnvelope? envelope = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Status = status;
        ErrorCode = errorCode;
        Details = details;
        RequestId = requestId;
        Envelope = envelope;
    }
}

/// <summary>400 / 422 — request shape is invalid.</summary>
public sealed class PostioValidationException : PostioException
{
    public PostioValidationException(
        string message,
        int status = 400,
        string? errorCode = null,
        string? details = null,
        string? requestId = null,
        ErrorEnvelope? envelope = null,
        Exception? innerException = null)
        : base(message, status, errorCode, details, requestId, envelope, innerException) { }
}

/// <summary>401 — missing or invalid API key.</summary>
public sealed class PostioInvalidKeyException : PostioException
{
    public PostioInvalidKeyException(
        string message,
        string? errorCode = null,
        string? details = null,
        string? requestId = null,
        ErrorEnvelope? envelope = null,
        Exception? innerException = null)
        : base(message, 401, errorCode, details, requestId, envelope, innerException) { }
}

/// <summary>402 — account is out of credit.</summary>
public sealed class PostioOutOfCreditException : PostioException
{
    public PostioOutOfCreditException(
        string message,
        string? errorCode = null,
        string? details = null,
        string? requestId = null,
        ErrorEnvelope? envelope = null,
        Exception? innerException = null)
        : base(message, 402, errorCode, details, requestId, envelope, innerException) { }
}

/// <summary>403 — origin / IP / key restriction blocked.</summary>
public sealed class PostioForbiddenException : PostioException
{
    public PostioForbiddenException(
        string message,
        string? errorCode = null,
        string? details = null,
        string? requestId = null,
        ErrorEnvelope? envelope = null,
        Exception? innerException = null)
        : base(message, 403, errorCode, details, requestId, envelope, innerException) { }
}

/// <summary>404 — postcode/UDPRN not found.</summary>
public sealed class PostioNotFoundException : PostioException
{
    public PostioNotFoundException(
        string message,
        string? errorCode = null,
        string? details = null,
        string? requestId = null,
        ErrorEnvelope? envelope = null,
        Exception? innerException = null)
        : base(message, 404, errorCode, details, requestId, envelope, innerException) { }
}

/// <summary>429 — rate limited. RetryAfter is the API-suggested wait, in seconds.</summary>
public sealed class PostioRateLimitException : PostioException
{
    public double? RetryAfter { get; }

    public PostioRateLimitException(
        string message,
        string? errorCode = null,
        string? details = null,
        string? requestId = null,
        ErrorEnvelope? envelope = null,
        double? retryAfter = null,
        Exception? innerException = null)
        : base(message, 429, errorCode, details, requestId, envelope, innerException)
    {
        RetryAfter = retryAfter;
    }
}

/// <summary>5xx — server-side failure. Retried by default before surfacing.</summary>
public sealed class PostioServerException : PostioException
{
    public PostioServerException(
        string message,
        int status,
        string? errorCode = null,
        string? details = null,
        string? requestId = null,
        ErrorEnvelope? envelope = null,
        Exception? innerException = null)
        : base(message, status, errorCode, details, requestId, envelope, innerException) { }
}

/// <summary>Local request timeout.</summary>
public sealed class PostioTimeoutException : PostioException
{
    public PostioTimeoutException(string message, Exception? innerException = null)
        : base(message, 0, "request_timeout", null, null, null, innerException) { }
}

/// <summary>Network failure before a response was received.</summary>
public sealed class PostioConnectionException : PostioException
{
    public PostioConnectionException(string message, Exception? innerException = null)
        : base(message, 0, "network_error", null, null, null, innerException) { }
}

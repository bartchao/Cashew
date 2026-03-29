// ApiKeyMiddleware.cs — HTTP middleware that enforces API key authentication.
// Reads the expected key from the "ApiKey" configuration value.

namespace CashewAPI.Middleware;

/// <summary>
/// Middleware that validates the <c>X-Api-Key</c> request header against a configured secret.
/// When no API key is configured (e.g., in development), all requests are allowed through.
/// Swagger / OpenAPI documentation endpoints are always exempt from authentication.
/// </summary>
public class ApiKeyMiddleware
{
    /// <summary>HTTP header name used to transmit the API key.</summary>
    private const string ApiKeyHeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly string? _apiKey;

    /// <summary>
    /// Initialises the middleware and reads the API key from configuration.
    /// </summary>
    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _apiKey = configuration.GetValue<string>("ApiKey");
    }

    /// <summary>
    /// Validates the API key on each request, short-circuiting with 401 if invalid.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API key check if not configured (development mode)
        if (string.IsNullOrEmpty(_apiKey))
        {
            await _next(context);
            return;
        }

        // Allow Swagger/OpenAPI endpoints without auth
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/openapi"))
        {
            await _next(context);
            return;
        }

        // Reject requests with a missing or mismatched API key
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey) ||
            !string.Equals(extractedApiKey, _apiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API key." });
            return;
        }

        await _next(context);
    }
}

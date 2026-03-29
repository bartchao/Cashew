namespace CashewAPI.Middleware;

public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly string? _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _apiKey = configuration.GetValue<string>("ApiKey");
    }

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

namespace Broker.Gateway.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(HeaderName))
        {
            context.Request.Headers[HeaderName] = Guid.NewGuid().ToString();
        }

        context.Response.OnStarting(() =>
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var correlationId))
            {
                context.Response.Headers[HeaderName] = correlationId.ToString();
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

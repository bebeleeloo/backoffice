using System.Text;
using System.Text.Json;

namespace Broker.Auth.Api.Middleware;

public sealed class BasicAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1/auth/login")
            && context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var header = authHeader.ToString();
            if (header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                if (!context.Request.IsHttps)
                {
                    var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                    var middlewareLogger = loggerFactory.CreateLogger<BasicAuthMiddleware>();
                    middlewareLogger.LogWarning("Basic authentication used over insecure HTTP connection from {RemoteIp}",
                        context.Connection.RemoteIpAddress);
                }
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (string.IsNullOrWhiteSpace(body) || body.Trim() == "{}")
                {
                    try
                    {
                        var decoded = Encoding.UTF8.GetString(
                            Convert.FromBase64String(header["Basic ".Length..]));
                        var sep = decoded.IndexOf(':');
                        if (sep > 0)
                        {
                            var json = JsonSerializer.SerializeToUtf8Bytes(new
                            {
                                username = decoded[..sep],
                                password = decoded[(sep + 1)..]
                            });
                            context.Request.Body = new MemoryStream(json);
                            context.Request.ContentLength = json.Length;
                            context.Request.ContentType = "application/json";
                        }
                    }
                    catch (FormatException) { /* malformed base64 — let validator reject */ }
                }
            }
        }

        await next(context);
    }
}

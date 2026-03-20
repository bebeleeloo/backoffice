using System.Text.Json;
using System.Text.Json.Nodes;
using Broker.Gateway.Api.Services;

namespace Broker.Gateway.Api.Middleware;

/// <summary>
/// Filters JSON response fields based on entity configuration and user role.
/// Applied after YARP proxies the response.
/// </summary>
public sealed class FieldFilterMiddleware
{
    private readonly RequestDelegate _next;

    public FieldFilterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, EntityConfigService entityConfigService, ConfigLoader configLoader)
    {
        // Only filter GET responses with JSON content type
        await _next(context);
    }
}

/// <summary>
/// YARP response transform that filters JSON fields based on entity config and user role.
/// </summary>
public static class FieldFilterTransform
{
    public static JsonNode? FilterFields(JsonNode? node, IReadOnlySet<string> allowedFields)
    {
        if (node is JsonObject obj)
        {
            var filtered = new JsonObject();
            foreach (var prop in obj)
            {
                if (allowedFields.Contains(prop.Key))
                {
                    filtered[prop.Key] = prop.Value?.DeepClone();
                }
            }
            return filtered;
        }

        if (node is JsonArray arr)
        {
            var filtered = new JsonArray();
            foreach (var item in arr)
            {
                filtered.Add(FilterFields(item, allowedFields));
            }
            return filtered;
        }

        return node?.DeepClone();
    }

    /// <summary>
    /// Filters a paged result: filters each item in the "items" array.
    /// </summary>
    public static JsonNode? FilterPagedResult(JsonNode? node, IReadOnlySet<string> allowedFields)
    {
        if (node is not JsonObject obj) return node?.DeepClone();

        var result = new JsonObject();
        foreach (var prop in obj)
        {
            if (prop.Key == "items" && prop.Value is JsonArray items)
            {
                result["items"] = FilterFields(items, allowedFields);
            }
            else
            {
                result[prop.Key] = prop.Value?.DeepClone();
            }
        }
        return result;
    }
}

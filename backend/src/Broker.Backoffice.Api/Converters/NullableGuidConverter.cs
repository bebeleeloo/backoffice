using System.Text.Json;
using System.Text.Json.Serialization;

namespace Broker.Backoffice.Api.Converters;

/// <summary>
/// Treats empty strings as null when deserializing Guid? values.
/// Prevents ASP.NET model binding errors when frontend sends "countryId": "".
/// </summary>
public sealed class NullableGuidConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
                return null;

            if (Guid.TryParse(str, out var guid))
                return guid;

            throw new JsonException($"Unable to convert \"{str}\" to Guid.");
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing Guid?.");
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value);
        else
            writer.WriteNullValue();
    }
}

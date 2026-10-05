using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LabSpecimenManagement.Sync.Handlers;

/// <summary>
/// Deserialize payload của sync item (object/JsonElement) sang DTO.
/// Dùng chung cho các ISyncEntityHandler.
/// </summary>
public static class SyncDataSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static T Deserialize<T>(object data)
    {
        if (data is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), Options)!;
        }

        if (data is T typedData)
        {
            return typedData;
        }

        var json = JsonSerializer.Serialize(data);
        return JsonSerializer.Deserialize<T>(json, Options)!;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new LenientDateTimeConverter());
        return options;
    }

    /// <summary>
    /// System.Text.Json mặc định chỉ nhận offset dạng "+07:00".
    /// FE (dayjs format 'ZZ') gửi "+0700" → fallback DateTime.TryParse để không làm hỏng cả item sync.
    /// </summary>
    private sealed class LenientDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TryGetDateTime(out var value))
            {
                return value;
            }

            var text = reader.GetString();
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
            {
                return value;
            }

            throw new JsonException($"Invalid date value: {text}");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}

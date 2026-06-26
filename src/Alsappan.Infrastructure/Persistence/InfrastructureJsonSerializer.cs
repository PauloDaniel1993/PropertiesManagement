using System.Text.Json;
using System.Text.Json.Serialization;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Infrastructure.Persistence;

internal static class InfrastructureJsonSerializer
{
  static InfrastructureJsonSerializer()
  {
    Options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    Options.Converters.Add(new OrganizationIdJsonConverter());
    Options.Converters.Add(new UserIdJsonConverter());
    Options.Converters.Add(new EntityIdJsonConverter());
    Options.Converters.Add(new ConcurrencyTokenJsonConverter());
  }

  public static JsonSerializerOptions Options { get; }

  public static string Serialize<T>(T value) =>
    JsonSerializer.Serialize(value, Options);

  public static T Deserialize<T>(string json) =>
    JsonSerializer.Deserialize<T>(json, Options) ??
      throw new JsonException($"Could not deserialize {typeof(T).Name}.");

  private static Guid ReadGuidBackedValue(ref Utf8JsonReader reader, string typeName)
  {
    if (reader.TokenType == JsonTokenType.String)
    {
      return Guid.Parse(reader.GetString()!);
    }

    if (reader.TokenType == JsonTokenType.StartObject)
    {
      using var document = JsonDocument.ParseValue(ref reader);
      if (document.RootElement.TryGetProperty("value", out var valueElement))
      {
        return valueElement.ValueKind == JsonValueKind.String
          ? Guid.Parse(valueElement.GetString()!)
          : valueElement.GetGuid();
      }
    }

    throw new JsonException($"Expected a {typeName} value.");
  }

  private sealed class OrganizationIdJsonConverter : JsonConverter<OrganizationId>
  {
    public override OrganizationId Read(
      ref Utf8JsonReader reader,
      Type typeToConvert,
      JsonSerializerOptions options) =>
      new(ReadGuidBackedValue(ref reader, nameof(OrganizationId)));

    public override void Write(
      Utf8JsonWriter writer,
      OrganizationId value,
      JsonSerializerOptions options) =>
      writer.WriteStringValue(value.ToString());
  }

  private sealed class UserIdJsonConverter : JsonConverter<UserId>
  {
    public override UserId Read(
      ref Utf8JsonReader reader,
      Type typeToConvert,
      JsonSerializerOptions options) =>
      new(ReadGuidBackedValue(ref reader, nameof(UserId)));

    public override void Write(
      Utf8JsonWriter writer,
      UserId value,
      JsonSerializerOptions options) =>
      writer.WriteStringValue(value.ToString());
  }

  private sealed class EntityIdJsonConverter : JsonConverter<EntityId>
  {
    public override EntityId Read(
      ref Utf8JsonReader reader,
      Type typeToConvert,
      JsonSerializerOptions options) =>
      new(ReadGuidBackedValue(ref reader, nameof(EntityId)));

    public override void Write(
      Utf8JsonWriter writer,
      EntityId value,
      JsonSerializerOptions options) =>
      writer.WriteStringValue(value.ToString());
  }

  private sealed class ConcurrencyTokenJsonConverter : JsonConverter<ConcurrencyToken>
  {
    public override ConcurrencyToken Read(
      ref Utf8JsonReader reader,
      Type typeToConvert,
      JsonSerializerOptions options) =>
      new(reader.GetString() ?? throw new JsonException("Expected a concurrency token value."));

    public override void Write(
      Utf8JsonWriter writer,
      ConcurrencyToken value,
      JsonSerializerOptions options) =>
      writer.WriteStringValue(value.Value);
  }
}

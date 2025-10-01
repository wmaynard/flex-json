using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Maynard.Json.Attributes;

namespace Maynard.Json.Serializers;

public class JsonFlexConverter : JsonConverter<Model>
{
    private readonly Dictionary<Type, PropertyMappingInfo> _typeMappings = new();

    public override Model Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        PropertyMappingInfo mappingInfo = GetOrCreateMappingInfo(typeToConvert);
        Model instance = (Model)Activator.CreateInstance(typeToConvert, true);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return instance;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName token");

            string propertyName = reader.GetString();
            reader.Read(); // Move to the property value

            if (mappingInfo.JsonToProperty.TryGetValue(propertyName, out PropertyInfo property))
            {
                object value = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                property.SetValue(instance, value);
            }
            else
            {
                // Skip unknown properties
                reader.Skip();
            }
        }

        throw new JsonException("Expected EndObject token");
    }

    public override void Write(Utf8JsonWriter writer, Model value, JsonSerializerOptions options)
    {
        Type type = value.GetType();
        PropertyMappingInfo mappingInfo = GetOrCreateMappingInfo(type);

        writer.WriteStartObject();

        foreach ((PropertyInfo property, string jsonKey) in mappingInfo.PropertyToJson)
        {
            FlexIgnore ignoreFlags = mappingInfo.PropertyIgnoreFlags[property];
            
            // Skip if marked to always ignore or ignore in JSON
            if (ignoreFlags.HasFlag(FlexIgnore.Always) || ignoreFlags.HasFlag(FlexIgnore.InJson))
                continue;

            object propertyValue = property.GetValue(value);

            // Skip nulls if configured to do so
            if (propertyValue == null && ignoreFlags.HasFlag(FlexIgnore.WhenJsonNull))
                continue;

            writer.WritePropertyName(jsonKey);
            JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
        }

        writer.WriteEndObject();
    }

    private PropertyMappingInfo GetOrCreateMappingInfo(Type type)
    {
        if (_typeMappings.TryGetValue(type, out PropertyMappingInfo existing))
            return existing;

        PropertyMappingInfo mappingInfo = new PropertyMappingInfo();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo prop in properties)
        {
            FlexKeys flexKeys = prop.GetCustomAttribute<FlexKeys>();
            JsonPropertyNameAttribute jsonPropertyName = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            JsonIgnoreAttribute jsonIgnore = prop.GetCustomAttribute<JsonIgnoreAttribute>();

            // Skip if explicitly ignored
            if (jsonIgnore != null)
                continue;

            string jsonKey;
            FlexIgnore ignoreFlags = FlexIgnore.Never;

            if (flexKeys != null)
            {
                jsonKey = flexKeys.Json;
                ignoreFlags = flexKeys.Ignore;
            }
            else if (jsonPropertyName != null)
            {
                jsonKey = jsonPropertyName.Name;
            }
            else
            {
                // Fall back to property name with camel case (matching JsonHelper.SerializerOptions)
                jsonKey = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
            }

            mappingInfo.JsonToProperty[jsonKey] = prop;
            mappingInfo.PropertyToJson[prop] = jsonKey;
            mappingInfo.PropertyIgnoreFlags[prop] = ignoreFlags;
        }

        _typeMappings[type] = mappingInfo;
        return mappingInfo;
    }

    private class PropertyMappingInfo
    {
        public Dictionary<string, PropertyInfo> JsonToProperty { get; } = new();
        public Dictionary<PropertyInfo, string> PropertyToJson { get; } = new();
        public Dictionary<PropertyInfo, FlexIgnore> PropertyIgnoreFlags { get; } = new();
    }
}
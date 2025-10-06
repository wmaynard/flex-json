using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;
using Maynard.Logging;

namespace Maynard.Json.Serializers;

public class JsonFlexConverter : JsonConverter<FlexModel>
{
    private readonly Dictionary<Type, PropertyMappingInfo> _typeMappings = new();

    public override FlexModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        PropertyMappingInfo mappingInfo = GetOrCreateMappingInfo(typeToConvert);
        FlexModel instance = (FlexModel)Activator.CreateInstance(typeToConvert, true);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return instance;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName token");

            string propertyName = reader.GetString();
            reader.Read(); // Move to the property value

            if (propertyName != null && mappingInfo.JsonToProperty.TryGetValue(propertyName, out PropertyInfo property))
            {
                object value = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                property.SetValue(instance, value);
            }
            else // Skip unknown properties
                reader.Skip();
        }

        throw new JsonException("Expected EndObject token");
    }

    public override void Write(Utf8JsonWriter writer, FlexModel value, JsonSerializerOptions options)
    {
        Type type = value.GetType();
        PropertyMappingInfo mappingInfo = GetOrCreateMappingInfo(type);

        writer.WriteStartObject();

        foreach ((PropertyInfo property, string jsonKey) in mappingInfo.PropertyToJson)
        {
            Ignore policy = mappingInfo.IgnorePolicies[property];
            
            // Skip if marked to always ignore or ignore in JSON
            if (policy.HasFlag(Ignore.Always) || policy.HasFlag(Ignore.InJson))
                continue;

            object propertyValue = property.GetValue(value);

            if (propertyValue == null && (policy.HasFlag(Ignore.WhenJsonNull) || policy.HasFlag(Ignore.WhenJsonDefault)))
                continue;
            
            // Check for default value using the property's actual type
            if (policy.HasFlag(Ignore.WhenJsonDefault) && property.PropertyType.IsValueType)
            {
                // TODO: Probably worth caching types (both here and in the respective BsonSerializer) and their default values so we don't do this with every serialization
                object defaultValue = Activator.CreateInstance(property.PropertyType);
                if (Equals(propertyValue, defaultValue))
                    continue;
            }

            writer.WritePropertyName(jsonKey);
            JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
        }

        writer.WriteEndObject();
    }

    private PropertyMappingInfo GetOrCreateMappingInfo(Type type)
    {
        if (_typeMappings.TryGetValue(type, out PropertyMappingInfo existing))
            return existing;

        PropertyMappingInfo mappingInfo = new();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo prop in properties)
        {
            FlexKeys flexKeys = prop.GetCustomAttribute<FlexKeys>();
            FlexIgnore flexIgnore = prop.GetCustomAttribute<FlexIgnore>();
            JsonPropertyNameAttribute jsonPropertyName = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            JsonIgnoreAttribute jsonIgnore = prop.GetCustomAttribute<JsonIgnoreAttribute>();
            
            string key = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
            Ignore flags = Ignore.Never;

            if (!string.IsNullOrWhiteSpace(flexKeys?.Json))
            {
                key = flexKeys.Json;
                if (!string.IsNullOrWhiteSpace(jsonPropertyName?.Name))
                    Log.Warn($"Found two JSON key names when preparing serialization maps.  {nameof(FlexKeys)} attributes are preferred and thus have priority.", data: new
                    {
                        Type = type,
                        Property = prop.Name,
                        FlexKeysName = flexKeys.Json,
                        JsonPropertyName = jsonPropertyName.Name,
                        Identical = flexKeys.Json == jsonPropertyName.Name
                    });
            }
            else if (!string.IsNullOrWhiteSpace(jsonPropertyName?.Name))
                key = jsonPropertyName.Name;

            if (flexIgnore != null)
            {
                flags = flexIgnore.Ignore;
                if (jsonIgnore != null)
                    Log.Warn($"Found two JSON ignore attributes when preparing serialization maps.  {nameof(FlexIgnore)} attributes are more specific and thus have priority.", data: new
                    {
                        Type = type,
                        Property = prop.Name,
                        FlexIgnore = flags.ToString()
                    });
            }
            else if (jsonIgnore != null)
            {
                flags = jsonIgnore.Condition switch
                {
                    JsonIgnoreCondition.Never => Ignore.Never,
                    JsonIgnoreCondition.Always => Ignore.Always,
                    JsonIgnoreCondition.WhenWritingDefault => Ignore.WhenJsonDefault,
                    JsonIgnoreCondition.WhenWritingNull => Ignore.WhenJsonNull,
                    _ => Ignore.WhenJsonNullOrDefault
                };
                if (flexKeys != null)
                    Log.Warn($"Found a JsonIgnore attribute in addition to a {nameof(FlexKeys)} attribute.  The latter allows more specificity and is preferred.");
            }
            else if (flexKeys != null)
                flags = flexKeys.Ignore;
            else
                Log.Warn($"Could not find an ignore policy for a property in a data model.  Best practice is to mark the model with {nameof(FlexKeys)} or {nameof(FlexIgnore)}.", data: new
                {
                    Type = type,
                    Property = prop.Name
                });

            mappingInfo.JsonToProperty[key] = prop;
            mappingInfo.PropertyToJson[prop] = key;
            mappingInfo.IgnorePolicies[prop] = flags;
        }

        _typeMappings[type] = mappingInfo;
        return mappingInfo;
    }

    private class PropertyMappingInfo
    {
        public Dictionary<string, PropertyInfo> JsonToProperty { get; } = new();
        public Dictionary<PropertyInfo, string> PropertyToJson { get; } = new();
        public Dictionary<PropertyInfo, Ignore> IgnorePolicies { get; } = new();
    }
}
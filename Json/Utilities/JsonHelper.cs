using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Maynard.Json.Exceptions;
using Maynard.Json.Serializers;
using Maynard.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Maynard.Json.Utilities;

public static class JsonHelper
{
    private static JsonSerializerOptions _serializerOptions;
    private static JsonDocumentOptions _documentOptions;

    public static void ConfigureJsonOptions(JsonOptions options)
    {
        // options.JsonSerializerOptions.IgnoreNullValues = JsonHelper.SerializerOptions.IgnoreNullValues;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.IncludeFields = SerializerOptions.IncludeFields;
        options.JsonSerializerOptions.IgnoreReadOnlyFields = SerializerOptions.IgnoreReadOnlyFields;
        options.JsonSerializerOptions.IgnoreReadOnlyProperties = SerializerOptions.IgnoreReadOnlyProperties;
        options.JsonSerializerOptions.PropertyNamingPolicy = SerializerOptions.PropertyNamingPolicy;
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.Converters.Add(new JsonTypeConverter());

        foreach (JsonConverter converter in SerializerOptions.Converters)
            options.JsonSerializerOptions.Converters.Add(converter);

        // As a side effect of dropping Newtonsoft and switching to System.Text.Json, nothing until this point can be reliably serialized to JSON.
        // It throws errors when trying to serialize certain types and breaks the execution to do it.
        Log.Verbose("JSON serializer options configured.");
        // options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
    }

    public static JsonSerializerOptions SerializerOptions => _serializerOptions ??= new()
    {
        // IgnoreNullValues = false,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        IncludeFields = true,
        IgnoreReadOnlyFields = false,
        IgnoreReadOnlyProperties = false,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
        // UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters =
        {
            new JsonTypeConverter(),
            new JsonExceptionConverter(),
            new JsonGenericConverter()
            // new JsonShortConverter(), // These numeric converters are required because otherwise, System.Text.Json
            // new JsonIntConverter(),   // fails deserialization on values where quote marks are in the JSON, like '"313"'.
            // new JsonLongConverter()   // e.g. JsonSerializer.Deserialize<int>("\"313\"", SerializerOptions).
        },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                static jsonTypeInfo =>
                {
                    if (typeof(FlexJson).IsAssignableFrom(jsonTypeInfo.Type))
                    {
                        IList<JsonPropertyInfo> properties = jsonTypeInfo.Properties;
                        for (int i = 0; i < properties.Count; i++)
                        {
                            if (properties[i].PropertyType.IsGenericType && properties[i].PropertyType.GetGenericTypeDefinition() == typeof(IObservable<>))
                                jsonTypeInfo.Properties.RemoveAt(i--);
                        }
                    }
                }
                
            }
        }
    };

    public static JsonDocumentOptions DocumentOptions
    {
        get
        {
            if (_documentOptions.Equals(default(JsonDocumentOptions)))
                _documentOptions = new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip
                };
            return _documentOptions;
        }
    }

    public static JsonElement Optional(JsonDocument json, string key) => Optional(json.RootElement, key);
    public static JsonElement Optional(JsonElement json, string key) => json.TryGetProperty(key, out JsonElement value)
        ? value
        : default;
    
    public static T Optional<T>(JsonDocument json, string key) => Optional<T>(json.RootElement, key);
    public static T Optional<T>(JsonElement json, string key)=> json.TryGetProperty(key, out JsonElement value) 
        ? JsonSerializer.Deserialize<T>(value.GetRawText(), SerializerOptions) 
        : default;

    public static JsonElement Require(JsonDocument json, string key) => Require(json.RootElement, key);
    public static JsonElement Require(JsonElement json, string key) => json.GetProperty(key);
    public static T Require<T>(JsonDocument json, string key) => Require<T>(json.RootElement, key);

    public static T Require<T>(JsonElement json, string key)
    {
        JsonElement element = default;
        try
        {
            element = json.GetProperty(key);
            return JsonSerializer.Deserialize<T>(element.GetRawText(), SerializerOptions);
        }
        catch (KeyNotFoundException)
        {
            return Throw.Ex<T>(new MissingJsonKeyException(json, key));
        }
        catch (Exception e)
        {
            Log.Error($"Unable to deserialize JSON '{key}'.", new
            {
                Element = element,
                AttemptedType = typeof(T).Name,
                Exception = e
            });
            return Throw.Ex<T>(e);
        }
    }
}
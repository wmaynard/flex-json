using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maynard.Extensions;

public static class JsonSerializerOptionsExtension
{
    public static void ApplyTo(this JsonSerializerOptions options, JsonSerializerOptions other)
    {
        other.AllowTrailingCommas = options.AllowTrailingCommas;
        other.DefaultIgnoreCondition = options.DefaultIgnoreCondition;
        other.IncludeFields = options.IncludeFields;
        other.IgnoreReadOnlyFields = options.IgnoreReadOnlyFields;
        other.IgnoreReadOnlyProperties = options.IgnoreReadOnlyProperties;
        other.NumberHandling = options.NumberHandling;
        other.PreferredObjectCreationHandling = options.PreferredObjectCreationHandling;
        other.PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive;
        other.PropertyNamingPolicy = options.PropertyNamingPolicy;
        other.ReadCommentHandling = options.ReadCommentHandling;
        other.ReferenceHandler = options.ReferenceHandler;
        other.TypeInfoResolver = options.TypeInfoResolver;
        other.UnknownTypeHandling = options.UnknownTypeHandling;
        other.WriteIndented = options.WriteIndented;
        
        foreach (JsonConverter converter in options.Converters)
            other.Converters.Add(converter);
    }
}
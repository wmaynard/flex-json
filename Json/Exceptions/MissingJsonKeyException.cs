using System.Text.Json.Serialization;

namespace Maynard.Json.Exceptions;

public class MissingJsonKeyException(string key) : Exception($"JSON did not contain required field '{key}'.")
{
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FlexJson Json { get; init; }
    
    [JsonInclude]
    public string MissingKey { get; init; } = key;

    public MissingJsonKeyException(FlexJson json, string key) : this(key) => Json = json;
}
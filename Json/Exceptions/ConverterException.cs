using System.Text.Json.Serialization;

namespace Maynard.Json.Exceptions;

public class ConverterException(string message, Type attemptedType, Exception inner = null, bool onDeserialize = false)
    : Exception($"Unable to {(onDeserialize ? "de" : "")}serialize {attemptedType.Name}.", inner)
{
    [JsonInclude]
    public string Info { get; init; } = message;
}
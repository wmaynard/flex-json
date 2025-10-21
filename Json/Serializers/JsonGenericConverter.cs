using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Maynard.Json.Exceptions;
using Maynard.Json.Utilities;
using Maynard.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using JsonTokenType = System.Text.Json.JsonTokenType;

namespace Maynard.Json.Serializers;

public class JsonGenericConverter : JsonConverter<FlexJson>
{
    // Debug helpers used with below kluge.
    private static string Debug(ReadOnlySequence<byte> seq) => string.Join("", seq.ToArray().Select(s => (char)s));
    private static string Debug(ReadOnlySpan<byte> seq) => string.Join("", seq.ToArray().Select(s => (char)s));
    #region READ
    public override FlexJson Read(ref Utf8JsonReader reader, Type data, JsonSerializerOptions options)
    {
        try
        {
            // TODO: Identify the root cause of this problem and clean up this kluge.
            // Will on 2021.01.14: This addresses a very specific scenario:
            //   * You are deserializing a Model.
            //   * That Model has a FlexJson field.
            //   * That FlexJson field is coming from stringified JSON.
            // Something is causing the reader to start at the *end* of the stringified JSON.  When this happens,
            // the FlexJson field is skipped entirely, and the reader moves on to a token it shouldn't be accessing,
            // resulting in an Exception later on, which subsequently yields a null FlexJson.
            // This recursive call to deserialize the stringified JSON seems to work, but it's dangerous, janky, and hard to understand.
            if (reader.TokenType == JsonTokenType.String)
                try { return reader.GetString(); }
                catch { }
            return ReadGeneric(ref reader);
        }
        catch (Exception e)
        {
            return Throw.Ex<FlexJson>(new ConverterException(e.Message, typeof(FlexJson), e, onDeserialize: true));
        }
    }

    /// <summary>
    /// Reads a FlexJson object from an endpoint's payload or other raw JSON.
    /// </summary>
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    private static FlexJson ReadGeneric(ref Utf8JsonReader reader)
    {
        FlexJson output = new();
        string key = null;
        while (reader.Read())
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    key = reader.GetString();
                    break;
                case JsonTokenType.True:
                case JsonTokenType.False:
                    output[key] = reader.GetBoolean();
                    break;
                case JsonTokenType.Null:
                case JsonTokenType.None:
                case JsonTokenType.Comment:
                    output[key] = null;
                    break;
                case JsonTokenType.String:
                    output[key] = FlexJson.TrimStringsOnDeserialize
                        ? reader.GetString()?.Trim()
                        : reader.GetString();
                    break;
                case JsonTokenType.StartObject:
                    output[key] = ReadGeneric(ref reader);
                    break;
                case JsonTokenType.EndObject:
                    return output;
                case JsonTokenType.StartArray:
                    output[key] = ReadArray(ref reader);
                break;
                case JsonTokenType.Number:
                    if (!reader.TryGetDecimal(out decimal asDecimal))
                        return Throw.Ex<FlexJson>(new ConverterException("Couldn't parse number.", typeof(FlexJson), onDeserialize: true));
                    if (key == null)
                        return Throw.Ex<FlexJson>(new Exception("Key was null."));
                    output[key] = asDecimal;
                    break;
                case JsonTokenType.EndArray:
                default:
                    return Throw.Ex<FlexJson>(new ConverterException("Operation should be impossible.", typeof(FlexJson), onDeserialize: true));
            }
        return null;
    }

    /// <summary>
    /// Reads an array for a FlexJson object.  Arrays require special handling since they do not have field names.
    /// </summary>
    private static List<object> ReadArray(ref Utf8JsonReader reader)
    {
        List<object> output = new();
        while (reader.Read())
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                case JsonTokenType.False:
                    output.Add(reader.GetBoolean());
                    break;
                case JsonTokenType.Null:
                case JsonTokenType.None:
                case JsonTokenType.Comment:
                    output.Add(null);
                    break;
                case JsonTokenType.String:
                    output.Add(FlexJson.TrimStringsOnDeserialize
                        ? reader.GetString()?.Trim()
                        : reader.GetString()
                    );
                    break;
                case JsonTokenType.StartObject:
                    output.Add(ReadGeneric(ref reader));
                    break;
                case JsonTokenType.StartArray:
                    output.Add(ReadArray(ref reader));
                    break;
                case JsonTokenType.EndArray:
                    return output;
                case JsonTokenType.Number:
                    if (!reader.TryGetDecimal(out decimal asDecimal))
                        return Throw.Ex<List<object>>(new ConverterException("Couldn't parse number.", typeof(FlexJson), onDeserialize: true));
                    output.Add(asDecimal);
                    break;
                case JsonTokenType.PropertyName:
                case JsonTokenType.EndObject:
                default:
                    return Throw.Ex<List<object>>(new ConverterException("Operation should be impossible.", typeof(FlexJson), onDeserialize: true));
            }
        return output;
    }
    #endregion READ

    #region WRITE

    public override void Write(Utf8JsonWriter writer, FlexJson value, JsonSerializerOptions options) 
        => WriteJsonValue(ref writer, ref options, null, value);
    
    private static void WriteJsonValue(ref Utf8JsonWriter writer, ref JsonSerializerOptions options, string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
            writer.WritePropertyName(key);
        
        switch (value)
        {
            case bool asBool:
                writer.WriteBooleanValue(asBool);
                break;
            case Enum asEnum:
                writer.WriteStringValue(asEnum.ToString());
                break;
            case string asString:
                writer.WriteStringValue(asString);
                break;
            case short asShort:
                writer.WriteNumberValue(asShort);
                break;
            case int asInt:
                writer.WriteNumberValue(asInt);
                break;
            case long asLong:
                writer.WriteNumberValue(asLong);
                break;
            case float asFloat:
                writer.WriteNumberValue(asFloat);
                break;
            case double asDouble:
                writer.WriteNumberValue(asDouble);
                break;
            case decimal asDecimal:
                writer.WriteNumberValue(asDecimal);
                break;
            case null:
                writer.WriteNullValue();
                break;
            case FlexJson asGeneric:
                writer.WriteStartObject();
                foreach ((string _key, object _value) in asGeneric)
                    WriteJsonValue(ref writer, ref options, _key, _value);
                writer.WriteEndObject();
                break;
            case IEnumerable asEnumerable:
                writer.WriteStartArray();
                foreach (object _value in asEnumerable)
                    WriteJsonValue(ref writer, ref options, null, _value);
                writer.WriteEndArray();
                break;
            case FlexModel asModel:
                writer.WriteRawValue(asModel.ToJson());
                break;
            case MongoException asMongoException:
                writer.WriteRawValue(asMongoException.ToJson(new JsonWriterSettings
                {
                    OutputMode = JsonOutputMode.CanonicalExtendedJson
                }));
                break;
            default:
                try
                {
                    if (HasObjectCycle(value))
                    {
                        Log.Warn("Tried to serialize something with an object cycle into JSON.  This is not allowed.  It will be cast to a string instead.", new
                        {
                            Help = "Deserializing this value may not be possible.  Consider removing self-referential fields or adding [FlexIgnore] to them to protect against this error.",
                            DataType = value.GetType(),
                        });
                        writer.WriteStringValue(value.ToString());
                    }
                    else
                        writer.WriteRawValue(JsonSerializer.Serialize(value, options));
                }
                catch (Exception e)
                {
                    Log.Error("Could not serialize unexpected data type properly.", data: new
                    {
                        Help = "A custom data type was likely passed into a FlexJson object and JSON may not have serialized as expected.",
                        DataType = value.GetType(),
                        Message = e?.Message
                    });
                    throw;
                }
                break;
        }
    }
    
    private static bool HasObjectCycle(object root)
    {
        HashSet<object> visited = new (ReferenceEqualityComparer.Instance);
        return DetectCycle(root, visited);
    }

    private static bool DetectCycle(object? obj, HashSet<object> visited)
    {
        if (obj == null)
            return false;

        // Primitives, strings, and value types can’t cause cycles
        Type type = obj.GetType();
        if (type.IsValueType || type == typeof(string))
            return false;

        // Already seen this reference → cycle detected
        if (!visited.Add(obj))
            return true;

        // Handle enumerable types
        if (obj is IEnumerable enumerable)
            return enumerable.Cast<object>().Any(item => DetectCycle(item, visited));
        
        // Reflect over properties and fields
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (prop.CanRead && prop.GetIndexParameters().Length == 0)
            {
                object value = null;
                try
                {
                    value = prop.GetValue(obj);
                } 
                catch { }
                if (DetectCycle(value, visited))
                    return true;
            }

        return type
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(field => field.GetValue(obj))
            .Any(value => DetectCycle(value, visited));
    }
#endregion WRITE
}
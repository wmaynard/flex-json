using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maynard.Json.Serializers;

public class JsonTypeConverter : JsonConverter<Type>
{
    public override Type Read(ref Utf8JsonReader rdr, Type type, JsonSerializerOptions options) => Type.GetType(rdr.GetString() 
        ?? throw new InvalidOperationException());

    public override void Write(Utf8JsonWriter writer, Type type, JsonSerializerOptions options) => writer.WriteStringValue(type.AssemblyQualifiedName);
}
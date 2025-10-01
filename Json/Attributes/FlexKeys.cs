namespace Maynard.Json.Attributes;

[AttributeUsage(validOn: AttributeTargets.Property)]
public class FlexKeys(string json, string bson, bool preserveNulls = false) : Attribute
{
    public string Json { get; set; } = json;
    public string Bson { get; set; } = bson;
    public bool PreserveNulls { get; set; } = preserveNulls;
}
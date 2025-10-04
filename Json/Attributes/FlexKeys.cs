using Maynard.Json.Enums;

namespace Maynard.Json.Attributes;

[AttributeUsage(validOn: AttributeTargets.Property)]
public class FlexKeys(string json = null, string bson = null, Ignore ignore = Ignore.WhenNullOrDefault) : Attribute
{
    public string Json { get; set; } = json;
    public string Bson { get; set; } = bson;
    public Ignore Ignore { get; set; } = ignore;
}





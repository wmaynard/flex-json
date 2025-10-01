namespace Maynard.Json.Attributes;

[AttributeUsage(validOn: AttributeTargets.Property)]
public class FlexKeys(string json, string bson, FlexIgnore ignore = FlexIgnore.WhenNull) : Attribute
{
    public string Json { get; set; } = json;
    public string Bson { get; set; } = bson;
    public FlexIgnore Ignore { get; set; } = ignore;
}

[Flags]
public enum FlexIgnore
{
    Never = 0,
    InJson = 0b_0000_0001,
    InBson = 0b_0000_0010,
    WhenJsonNull = 0b_0001_0000,
    WhenBsonNull = 0b_0010_0000,
    WhenNull = 0b_0011_0000,
    Always = int.MaxValue
}
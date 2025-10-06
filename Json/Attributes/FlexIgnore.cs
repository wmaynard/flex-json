using Maynard.Json.Enums;

namespace Maynard.Json.Attributes;

[AttributeUsage(validOn: AttributeTargets.Property)]
public class FlexIgnore(Ignore ignore = Ignore.Always) : Attribute
{
    public Ignore Ignore { get; set; } = ignore;
}
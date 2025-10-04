namespace Maynard.Json.Enums;

[Flags]
public enum Ignore
{
    Never = 0,
    InJson = 0b_0000_0001,
    InBson = 0b_0000_0010,
    WhenJsonNull = 0b_0001_0000,
    WhenJsonDefault = 0b0010_0000,
    WhenJsonNullOrDefault = 0b0011_0000,
    WhenBsonNull = 0b_0100_0000,
    WhenBsonDefault = 0b1000_0000,
    WhenBsonNullOrDefault = 0b1100_0000,
    WhenNull = 0b_0101_0000,
    WhenDefault = 0b_1010_0000,
    WhenNullOrDefault = 0b_1111_0000,
    Always = int.MaxValue
}
using Maynard.Json;

namespace FlexJsonTests.Models;

public class MixedDataTypeModel : FlexModel
{
    public int Int { get; set; }
    public string String { get; set; }
    public char Char { get; set; }
    public decimal Decimal { get; set; }
    public double Double { get; set; }
    public float Float { get; set; }
    public long[] LongArray { get; set; }
    public List<string> StringList { get; set; }
    public HashSet<ushort> UShortSet { get; set; }
}
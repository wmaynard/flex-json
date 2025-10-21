using FlexJsonTests.Abstraction;

namespace FlexJsonTests;

public class _Booleans : Typecaster<_Booleans, bool>
{
    protected override bool[] CreateCollection() => [true, false, true, true, true, false, true, false, false, true];
}

public class _Ints : Typecaster<_Ints, int>
{
    protected override int[] CreateCollection() => [37, 489, 2011, 65, 1058, 82934, 174, 174, 174, 3091];
}

public class _Bytes : Typecaster<_Bytes, byte>
{
    protected override byte[] CreateCollection() => [1, 2, 3, 4, 5, 1, 6, 7, 8, 9];
}

public class _SBytes : Typecaster<_SBytes, sbyte>
{
    protected override sbyte[] CreateCollection() => [10, -5, 0, 127, -128, 10, 64, -64, 32, -32];
}

public class _Shorts : Typecaster<_Shorts, short>
{
    protected override short[] CreateCollection() => [100, 200, 300, 400, 500, 100, 600, 700, 800, 900];
}

public class _UShorts : Typecaster<_UShorts, ushort>
{
    protected override ushort[] CreateCollection() => [1000, 2000, 3000, 4000, 5000, 1000, 6000, 7000, 8000, 9000];
}

public class _UInts : Typecaster<_UInts, uint>
{
    protected override uint[] CreateCollection() => [1U, 2U, 3U, 4U, 5U, 1U, 6U, 7U, 8U, 9U];
}

public class _Longs : Typecaster<_Longs, long>
{
    protected override long[] CreateCollection() => [10000L, 20000L, 30000L, 40000L, 50000L, 10000L, 60000L, 70000L, 80000L, 90000L];
}

public class _ULongs : Typecaster<_ULongs, ulong>
{
    protected override ulong[] CreateCollection() => [100000UL, 200000UL, 300000UL, 400000UL, 500000UL, 100000UL, 600000UL, 700000UL, 800000UL, 900000UL];
}

public class _Floats : Typecaster<_Floats, float>
{
    protected override float[] CreateCollection() => [1.1f, 2.2f, 3.3f, 4.4f, 5.5f, 1.1f, 6.6f, 7.7f, 8.8f, 9.9f];
}

public class _Doubles : Typecaster<_Doubles, double>
{
    protected override double[] CreateCollection() => [10.1, 20.2, 30.3, 40.4, 50.5, 10.1, 60.6, 70.7, 80.8, 90.9];
}

public class _Decimals : Typecaster<_Decimals, decimal>
{
    protected override decimal[] CreateCollection() => [100.01m, 200.02m, 300.03m, 400.04m, 500.05m, 100.01m, 600.06m, 700.07m, 800.08m, 900.09m];
}

public class _Chars : Typecaster<_Chars, char>
{
    protected override char[] CreateCollection() => ['a', 'b', 'c', 'd', 'e', 'a', 'f', 'g', 'h', 'i'];
}

public class _Strings : Typecaster<_Strings, string>
{
    protected override string[] CreateCollection() => ["one", "two", "three", "four", "five", "one", "six", "seven", "eight", "nine"];
}

// TODO: Create casters for standard library types, e.g. DateTime.
// TODO: Create casters for a few different FlexModels.
// TODO: Create casters for anonymous objects.
// TODO: Create casters for custom classes. 
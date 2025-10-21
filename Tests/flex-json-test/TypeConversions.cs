using System.Globalization;
using FlexJsonTests.Abstraction;

namespace FlexJsonTests;

public class TypeConversions_Primitive
{
    public class Booleans : Typecaster<Booleans, bool>
    {
        protected override bool[] CreateCollection() => [true, false, true, true, true, false, true, false, false, true];
    }

    public class Ints : Typecaster<Ints, int>
    {
        protected override int[] CreateCollection() => [37, 489, 2011, 65, 1058, 82934, 174, 174, 174, 3091];
    }

    public class Bytes : Typecaster<Bytes, byte>
    {
        protected override byte[] CreateCollection() => [1, 2, 3, 4, 5, 1, 6, 7, 8, 9];
    }

    public class SBytes : Typecaster<SBytes, sbyte>
    {
        protected override sbyte[] CreateCollection() => [10, -5, 0, 127, -128, 10, 64, -64, 32, -32];
    }

    public class Shorts : Typecaster<Shorts, short>
    {
        protected override short[] CreateCollection() => [100, 200, 300, 400, 500, 100, 600, 700, 800, 900];
    }

    public class UShorts : Typecaster<UShorts, ushort>
    {
        protected override ushort[] CreateCollection() => [1000, 2000, 3000, 4000, 5000, 1000, 6000, 7000, 8000, 9000];
    }

    public class UInts : Typecaster<UInts, uint>
    {
        protected override uint[] CreateCollection() => [1U, 2U, 3U, 4U, 5U, 1U, 6U, 7U, 8U, 9U];
    }

    public class Longs : Typecaster<Longs, long>
    {
        protected override long[] CreateCollection() => [10000L, 20000L, 30000L, 40000L, 50000L, 10000L, 60000L, 70000L, 80000L, 90000L];
    }

    public class ULongs : Typecaster<ULongs, ulong>
    {
        protected override ulong[] CreateCollection() => [100000UL, 200000UL, 300000UL, 400000UL, 500000UL, 100000UL, 600000UL, 700000UL, 800000UL, 900000UL];
    }

    public class Floats : Typecaster<Floats, float>
    {
        protected override float[] CreateCollection() => [1.1f, 2.2f, 3.3f, 4.4f, 5.5f, 1.1f, 6.6f, 7.7f, 8.8f, 9.9f];
    }

    public class Doubles : Typecaster<Doubles, double>
    {
        protected override double[] CreateCollection() => [10.1, 20.2, 30.3, 40.4, 50.5, 10.1, 60.6, 70.7, 80.8, 90.9];
    }

    public class Decimals : Typecaster<Decimals, decimal>
    {
        protected override decimal[] CreateCollection() => [100.01m, 200.02m, 300.03m, 400.04m, 500.05m, 100.01m, 600.06m, 700.07m, 800.08m, 900.09m];
    }

    public class Chars : Typecaster<Chars, char>
    {
        protected override char[] CreateCollection() => ['a', 'b', 'c', 'd', 'e', 'a', 'f', 'g', 'h', 'i'];
    }

    public class Strings : Typecaster<Strings, string>
    {
        protected override string[] CreateCollection() => ["one", "two", "three", "four", "five", "one", "six", "seven", "eight", "nine"];
    }
}


public class TypeConversions_DotNetTypes
{
    public class DateTimes : Typecaster<DateTimes, DateTime>
    {
        protected override DateTime[] CreateCollection() => [
            new(2025, 1, 1),
            new(2025, 6, 15),
            new(2025, 12, 31),
            new(2025, 3, 5),
            new(2025, 7, 20),
            new(2025, 1, 1), // duplicate
            new(2025, 8, 9),
            new(2025, 4, 12),
            new(2025, 9, 25),
            new(2025, 11, 11)
        ];
    }
    public class TimeSpans : Typecaster<TimeSpans, TimeSpan>
    {
        protected override TimeSpan[] CreateCollection() => [
            TimeSpan.FromHours(1),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromDays(1),
            TimeSpan.FromSeconds(45),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromHours(1), // duplicate
            TimeSpan.FromMinutes(15),
            TimeSpan.FromSeconds(90),
            TimeSpan.FromDays(2),
            TimeSpan.FromHours(12)
        ];
    }
    
    public class Guids : Typecaster<Guids, Guid>
    {
        protected override Guid[] CreateCollection() => [
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"), // duplicate
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            Guid.Parse("99999999-9999-9999-9999-999999999999")
        ];
    }
    
    public class Uris : Typecaster<Uris, Uri>
    {
        protected override Uri[] CreateCollection() => [
            new("https://example.com"),
            new("https://google.com"),
            new("https://microsoft.com"),
            new("https://github.com"),
            new ("https://stackoverflow.com"),
            new("https://example.com"), // duplicate
            new("https://openai.com"),
            new("https://reddit.com"),
            new("https://news.ycombinator.com"),
            new("https://dotnet.microsoft.com")
        ];
    }
    
    public class Versions : Typecaster<Versions, Version>
    {
        protected override Version[] CreateCollection() => [
            new(1, 0),
            new(1, 1),
            new(2, 0),
            new(2, 1),
            new(3, 0),
            new(1, 0), // duplicate
            new(3, 1),
            new(4, 0),
            new(4, 1),
            new(5, 0)
        ];
    }

    public class CultureInfos : Typecaster<CultureInfos, CultureInfo>
    {
        protected override CultureInfo[] CreateCollection() => [
            new("en-US"),
            new("fr-FR"),
            new("es-ES"),
            new("de-DE"),
            new("ja-JP"),
            new("en-US"), // duplicate
            new("it-IT"),
            new("pt-BR"),
            new("zh-CN"),
            new("ru-RU")
        ];
    }
}

// TODO: Create casters for more standard library types, e.g. DateTime.
// TODO: Create casters for a few different FlexModels.
// TODO: Create casters for anonymous objects.
// TODO: Create casters for custom classes. 
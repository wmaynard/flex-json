using System.Globalization;
using FlexJsonTests.Abstraction;

namespace FlexJsonTests;

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


// TODO: Create casters for anonymous objects.
// TODO: Create casters for custom classes. 
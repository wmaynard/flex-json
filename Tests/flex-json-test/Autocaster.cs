using Maynard.Json;
using Maynard.Json.Exceptions;

namespace FlexJsonTests;

public class Autocaster
{
    [Fact]
    public void Require_ThrowsWhenKeyNotFound()
    {
        FlexJson json = new();
        bool thrown = false;
        try
        {
            json.Require<string>("key");
        }
        catch (MissingJsonKeyException)
        {
            thrown = true;
        }
        Assert.True(thrown);
    }

    [Fact]
    public void Require_ReturnsValueWhenKeyFound()
    {
        FlexJson json = new()
        {
            { "key", "value" }
        };
        object result = json.Require<object>("key");
        Assert.NotNull(result);   
    }
    
    [Fact]
    public void Optional_ReturnsNullWhenKeyNotFound()
    {
        FlexJson json = new();
        string result = json.Optional<string>("key");
        Assert.Null(result);
    }
    
    [Fact]
    public void Optional_ReturnsValueWhenKeyFound()
    {
        FlexJson json = new()
        {
            { "key", "value" }
        };
        object result = json.Optional<object>("key");
        Assert.NotNull(result);
    }
}
using FlexJsonTests.Models;
using Maynard.Json;
using Maynard.Json.Exceptions;

namespace FlexJsonTests;

public class GithubRegression
{
    [Fact]
    public void GH001_HonorFlexKeysJson()
    {
        User user = new()
        {
            Username = "Joe McFugal",
            Email = "joe.mcfugal@domain.com"
        };
        FlexJson json = new()
        {
            { "user", user }
        };
        
        Assert.Single(json.Keys);
        
        User indirect = json.Require<User>("user");
        Assert.Equal(user.Username, indirect.Username);
        Assert.Equal(user.Email, indirect.Email);
        
        FlexJson raw = user.ToJson();
        Assert.Equal(2, raw.Keys.Count);

        User direct = raw.ToModel<User>();
        Assert.Equal(user.Username, direct.Username);
        Assert.Equal(user.Email, direct.Email);
    }

    [Fact]
    public void GH002_NestedFlexJson()
    {
        User user = new()
        {
            Username = "Joe McFugal",
            Email = "joe.mcfugal@domain.com"
        };
        FlexJson one = new()
        {
            { "user", user }
        };
        FlexJson two = one.Json; // Copy first object, but as a raw JSON string rather than typed.
        
        // Cast the model to FlexJson.
        FlexJson modelToFlexJson = one.Require<FlexJson>("user");
        Assert.NotNull(modelToFlexJson);
        
        // Guarantee the model equates to the original object.
        User fromModel = modelToFlexJson.ToModel<User>();
        Assert.Equal(user.Username, fromModel.Username);
        Assert.Equal(user.Email, fromModel.Email);
        
        // Cast the raw JSON to FlexJson.
        FlexJson modelToRawJson = two.Require<FlexJson>("user");
        Assert.NotNull(modelToRawJson);
        
        User fromRaw = modelToRawJson.ToModel<User>();
        Assert.Equal(user.Username, fromRaw.Username);
        Assert.Equal(user.Email, fromRaw.Email);
    }

    [Fact]
    public void GH003_RequireThrowsWhenKeyNotFound()
    {
        User user = new()
        {
            Username = "Joe McFugal",
            Email = "joe.mcfugal@domain.com"
        };
        FlexJson json = new()
        {
            { "user", user }
        };
        bool thrown = false;
        try
        {
            json.Require<object>("doesntExist");
        }
        catch (MissingJsonKeyException)
        {
            thrown = true;
        }
        Assert.True(thrown);
    }
}
using FlexJsonTests.Models;
using Maynard.Json;

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
        FlexJson json = new()
        {
            { "user", user }
        };
        FlexJson inner = json.Require<FlexJson>("user");
        Assert.NotNull(inner);
        
        User fromInner = inner.ToModel<User>();
        Assert.Equal(user.Username, fromInner.Username);
        Assert.Equal(user.Email, fromInner.Email);
    }
}
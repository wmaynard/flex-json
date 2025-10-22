using FlexJsonTests.Models;
using Maynard.Json;

namespace FlexJsonTests;

public class Serialization
{
    private User GenerateUser(string name = "Joe McFugal", string email = "joe.mcfugal@domain.com") => new User
    {
        Username = name,
        Email = email
    };
    
    [Fact]
    public void Serialize_RawJson()
    {
        User user = GenerateUser();
        string json = user.ToJson();
        const string manual = """{"username":"Joe McFugal","emailAddress":"joe.mcfugal@domain.com"}""";
        Assert.Equal(manual, json);
    }

    public static TheoryData<string> WhitespaceValues =>
    [
        "",             // Empty
        " ",            // Spaces
        "     ",
        "\t",           // Tabs
        "\t\t\t",
        "\n",           // Newlines
        "\n\n\n",
        "\r",           // Carriage Returns
        "\r\r\r",
        "\r\n",         // CRLF
        "\r\n\r\n",
        "\v",           // Vertical Tabs
        "\v\v",
        "\f",           // Form feeds
        "\f\f",
        " \t\n\r\v\f "  // Mixed Whitespace
        
        
    ];

    [Theory, MemberData(nameof(WhitespaceValues))]
    public void Whitespace_IsNull(string whitespace) => Assert.Null((FlexJson)whitespace);

    [Fact]
    public void Serialize_ImplicitStringToFlexJson()
    {
        const string username = "Joe McFugal";
        const string email = "joe.mcfugal@domain.com";

        FlexJson one = $"{{\"username\":\"{username}\",\"emailAddress\":\"{email}\"}}";
        FlexJson two = new()
        {
            { "username", username },
            { "emailAddress", email }
        };
        Assert.Equal(one, two);
        Assert.Equal(username, one.Require<string>("username"));
        Assert.Equal(email, one.Require<string>("emailAddress"));
    }
    

    [Fact]
    public void Serialize_ToFlexJson()
    {
        User user = GenerateUser();
        string json = user.ToJson();
        string manual = """{"username":"Joe McFugal","emailAddress":"joe.mcfugal@domain.com"}""";
    }

    public void Deserialize_WithToModel()
    {
        
    }

    public void Deserialize_WithRequire()
    {
        
    }
}
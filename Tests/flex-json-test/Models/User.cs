using Maynard.Json;
using Maynard.Json.Attributes;

namespace FlexJsonTests.Models;

public class User : FlexModel
{
    [FlexKeys(json: "username", bson: "sn")]
    public string Username { get; set; }
    
    [FlexKeys(json: "emailAddress", bson: "email")]
    public string Email { get; set; }
}
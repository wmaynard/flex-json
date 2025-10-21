using Maynard.Json;
using Maynard.Json.Attributes;

namespace FlexJsonTests.Models;

public class Address : FlexModel
{
    [FlexKeys(json: "name")]
    public string Name { get; set; }
    [FlexKeys(json: "line1")]
    public string AddressLine1 { get; set; }
    [FlexKeys(json: "line2")]
    public string AddressLine2 { get; set; }
    [FlexKeys(json: "city")]
    public string City { get; set; }
    [FlexKeys(json: "state")]
    public string State { get; set; }
    [FlexKeys(json: "zip")]
    public string Zip { get; set; }
    [FlexKeys(json: "cc")]
    public string Country { get; set; }
}
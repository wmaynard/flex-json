using Maynard.Json;

namespace FlexJsonTests.Models;

public class OuterModel : FlexModel
{
    public int Identifier { get; set; }
    public InnerModel InnerModel { get; set; }
}
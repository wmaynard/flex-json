using FlexJsonTests.Abstraction;
using FlexJsonTests.Models;
using Maynard.Json;
using Maynard.Json.Attributes;

namespace FlexJsonTests;

public class TypeConversions_FlexModel
{
    public class AddressTest : Typecaster<AddressTest, Address>
    {
        protected override Address[] CreateCollection() =>
        [
            new(),
            new()
            {
                Name = "John Doe",
                AddressLine1 = "123 Main St.",
                AddressLine2 = "Apt #101",
                City = "Portland",
                State = "OR",
                Zip = "01010",
                Country = "US"
            },
            new()
            {
                Name = "Jane Doe",
                AddressLine1 = "123 Main St.",
                City = "San Jose",
                State = "CA",
                Zip = "95125",
                Country = "US"
            },
            new()
            {
                Name = "Dick Solomon",
                AddressLine1 = "3rd Rock Way",
                City = "Rutherford",
                State = "OH",
                Zip = "01010",
                Country = "ET"
            },
            new()
            {
                Name = "John Doe",
                AddressLine1 = "123 Main St.",
                AddressLine2 = "Apt #101",
                City = "Portland",
                State = "OR",
                Zip = "01010",
                Country = "US"
            },
        ];
    }

    public class MixedModelTest : Typecaster<MixedModelTest, MixedDataTypeModel>
    {
        protected override MixedDataTypeModel[] CreateCollection() =>
        [
            new(),
            new()
            {
                Int = 42,
                String = "Some String",
                Char = '&',
                Decimal = 1.1m,
                Double = 1.1,
                Float = 1.1f,
                LongArray = [ 100, 200, 300 ],
                StringList = [ "abc", "def", "hij" ],
                UShortSet = [ 5, 10, 15, 20, 25 ]
            },
            new()
            {
                Int = 24,
                String = "Other String",
                Char = '#',
                Decimal = 1.8m,
                Double = 1.8,
                Float = 1.8f,
                LongArray = [ 55, 15, 99 ],
                UShortSet = [ 5, 10, 15, 20, 25 ]
            },
            new()
            {
                Decimal = 3.3m,
                Double = 3.1,
                Float = 4.1f,
                LongArray = [ 1010, 2020, 3030 ],
                StringList = [ "abczz", "zzdef", "hizzj" ],
                UShortSet = [ 5, 110, 15, 208, 25 ]
            },
            new()
            {
                Int = 42,
                String = "Some String",
                Char = '&',
                Decimal = 1.1m,
                Double = 1.1,
                Float = 1.1f,
                LongArray = [ 100, 200, 300 ],
                StringList = [ "abc", "def", "hij" ],
                UShortSet = [ 5, 10, 15, 20, 25 ]
            }
        ];
    }

    public class NestedModelTest : Typecaster<NestedModelTest, OuterModel>
    {
        protected override OuterModel[] CreateCollection() =>
        [
            new(),
            new()
            {
                Identifier = 374,
                InnerModel = new()
                {
                    SomeValue = "1566"
                }
            },
            new()
            {
                Identifier = 12271871,
                InnerModel = new()
                {
                    SomeValue = "The Jabberwock with eyes of flame / came whiffling through the tulgey wood / and burbled as it came!"
                }
            },
            new()
            {
                Identifier = 2010,
                InnerModel = new()
                {
                    SomeValue = "Och tamale gazolly gazump deyump deyatty yahoo woo!  Wing wang tricky tracky poo foo joozy woozy skizzle wazzle wang tang orky porky dominorky!"
                }
            },
            new()
            {
                Identifier = 374,
                InnerModel = new()
                {
                    SomeValue = "1566"
                }
            }
        ];
    }
}
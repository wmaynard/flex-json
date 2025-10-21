using Maynard.Json;
using Maynard.Json.Exceptions;

namespace FlexJsonTests.Abstraction;

// ReSharper disable PossibleMultipleEnumeration
public abstract class Typecaster<T, TData> where T : Typecaster<T, TData>, new()
{
    private const string KEY_COLLECTION = "values";
    protected abstract TData[] CreateCollection();

    public static TheoryData<FlexJson> TestData
    {
        get
        {
            T instance = new();

            TData[] collection = instance.CreateCollection();
            FlexJson json = new();

            int count = 0;
            foreach (TData data in collection)
                json[$"key{++count}"] = data;
            
            FlexJson raw = json.Json;

            return [json, raw];
        }
    }
    public static TheoryData<FlexJson> TestData_Collections
    {
        get
        {
            T instance = new();
            TData[] collection = instance.CreateCollection();
            if (!collection.Any())
                Assert.Fail("Test must generate at least one test item to be successful.");

            FlexJson json = new()
            {
                { KEY_COLLECTION, collection }
            };
            FlexJson raw = json.Json;

            return [json, raw];
        }
    }

    [Theory, MemberData(nameof(TestData))]
    public void Require_Simple(FlexJson json) => Casting(json, nameof(FlexJson.Require));
    [Theory, MemberData(nameof(TestData))]
    public void Optional_Simple(FlexJson json) => Casting(json, nameof(FlexJson.Optional));

    private static void Casting(FlexJson json, string methodName)
    {
        string invalidKey = Guid.NewGuid().ToString();
        switch (methodName)
        {
            case nameof(FlexJson.Require):
                foreach (string key in json.Keys)
                    Assert.Equal(TryConvert(json[key]), json.Require<TData>(key));
                try
                {
                    json.Require<TData>(invalidKey);
                    Assert.Fail("Require<T>() did not throw on an invalid key.");
                }
                catch (Exception e)
                {
                    Assert.True(e is MissingJsonKeyException);
                }
                break;
            case nameof(FlexJson.Optional):
                foreach (string key in json.Keys)
                    Assert.Equal(TryConvert(json[key]), json.Optional<TData>(key));
                try
                {
                    TData value = json.Optional<TData>(invalidKey);
                    Assert.Equal(default, value);
                }
                catch
                {
                    Assert.Fail("Optional<T>() threw on an invalid key; this should never happen.");
                }
                break;
            default:
                throw new NotImplementedException("Unsupported method for FlexJson casting.  The tests may need to be updated.");
        }
    }

    private static TData TryConvert(object value) => value is TData asTData
        ? asTData
        : (TData)Convert.ChangeType(value, typeof(TData));

    private void Casting_Collection<TCollection>(FlexJson json, string methodName) where TCollection : IEnumerable<TData>
    {
        // We need to test two scenarios:
        //   1) The FlexJson object still contains the type information it was instantiated with.
        //   2) The FlexJson was loaded from a string and consequently has no type information associated with it.
        object first = json.Values.First();
        TData[] values = typeof(TData[]).IsAssignableFrom(first.GetType())
            ? (TData[])first
            : ((IEnumerable<object>)first).Select(TryConvert).ToArray();

        TCollection collection = default;
        
        try
        {
            collection = methodName switch
            {
                nameof(FlexJson.Require) => json.Require<TCollection>(KEY_COLLECTION),
                nameof(FlexJson.Optional) => json.Optional<TCollection>(KEY_COLLECTION),
                _ => throw new NotImplementedException("Unsupported method for FlexJson casting.  The tests may need to be updated.")
            };
        }
        catch (ConverterException)
        {
            if (!typeof(TCollection).IsInterface)
                throw;
            Assert.Equal(collection, default);
            return;
        }
        
        Assert.NotNull(values);
        Assert.NotNull(collection);

        switch (collection)
        {
            case TData[] asArray:
                Assert.Equal(asArray.Length, values.Length);
                for (int index = 0; index < values.Length; index++)
                    Assert.Equal(values[index], asArray[index]);
                break;
            case List<TData> asList:
                Assert.Equal(asList.Count, values.Length);
                for (int index = 0; index < values.Length; index++)
                    Assert.Equal(values[index], asList[index]);
                break;
            case HashSet<TData> asHashSet:
                TData[] distinct = values.Distinct().ToArray();
                Assert.Equal(asHashSet.Count, distinct.Length);
                for (int index = 0; index < distinct.Length; index++)
                    Assert.Equal(distinct[index], asHashSet.ElementAt(index));
                break;
            case Queue<TData> asQueue:
                Assert.Equal(asQueue.Count, values.Length);
                foreach (TData data in values)
                    Assert.Equal(data, asQueue.Dequeue());
                Assert.Empty(asQueue);
                break;
            case Stack<TData> asStack:
                Assert.Equal(asStack.Count, values.Length);
                foreach (TData data in values.Reverse())
                    Assert.Equal(data, asStack.Pop());
                Assert.Empty(asStack);
                break;
            case IEnumerable<TData> asEnumerable:
                Assert.Equal(asEnumerable.Count(), values.Length);
                for (int index = 0; index < values.Length; index++)
                    Assert.Equal(values[index], asEnumerable.ElementAt(index));
                break;
        }
        
        string invalidKey = Guid.NewGuid().ToString();
        switch (methodName)
        {
            case nameof(FlexJson.Require):
                try
                {
                    json.Require<TCollection>(invalidKey);
                    Assert.Fail("Require<T>() did not throw on an invalid key.");
                }
                catch (Exception e)
                {
                    Assert.True(e is MissingJsonKeyException);
                }
                break;
            case nameof(FlexJson.Optional):
                try
                {
                    TCollection value = json.Optional<TCollection>(invalidKey);
                    Assert.Equal(default, value);
                }
                catch
                {
                    Assert.Fail("Optional<T>() threw on an invalid key; this should never happen.");
                }
                break;
            default:
                throw new NotImplementedException("Unsupported method for FlexJson casting.  The tests may need to be updated.");
        }
    }
    
    #region Require<T> Methods
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Require_Array(FlexJson json) => Casting_Collection<TData[]>(json, nameof(FlexJson.Require));
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Require_List(FlexJson json) => Casting_Collection<List<TData>>(json, nameof(FlexJson.Require));
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Require_HashSet(FlexJson json) => Casting_Collection<HashSet<TData>>(json, nameof(FlexJson.Require));
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Require_Queue(FlexJson json) => Casting_Collection<Queue<TData>>(json, nameof(FlexJson.Require));
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Require_Stack(FlexJson json) => Casting_Collection<Stack<TData>>(json, nameof(FlexJson.Require));
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Require_IEnumerable(FlexJson json) => Casting_Collection<IEnumerable<TData>>(json, nameof(FlexJson.Require));
    #endregion Require<T> Methods
    
    #region Optional<T> Methods
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Optional_Array(FlexJson json) => Casting_Collection<TData[]>(json, nameof(FlexJson.Optional));
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Optional_List(FlexJson json) => Casting_Collection<List<TData>>(json, nameof(FlexJson.Optional));
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Optional_HashSet(FlexJson json) => Casting_Collection<HashSet<TData>>(json, nameof(FlexJson.Optional));
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Optional_Queue(FlexJson json) => Casting_Collection<Queue<TData>>(json, nameof(FlexJson.Optional));
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Optional_Stack(FlexJson json) => Casting_Collection<Stack<TData>>(json, nameof(FlexJson.Optional));
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Optional_IEnumerable(FlexJson json) => Casting_Collection<IEnumerable<TData>>(json, nameof(FlexJson.Optional));
    #endregion
}
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

            FlexJson json = new()
            {
                { KEY_COLLECTION, instance.CreateCollection() }
            };
            FlexJson raw = json.Json;

            return [json, raw];
        }
    }
    
    [Theory, MemberData(nameof(TestData))]
    public void Simple(FlexJson json) => Casting(json);

    private void Casting(FlexJson json)
    {
        foreach (string key in json.Keys)
            Assert.Equal(json[key], json.Require<TData>(key));
    }

    private void Casting_Collection<TCollection>(FlexJson json) where TCollection : IEnumerable<TData>
    {
        // We need to test two scenarios:
        //   1) The FlexJson object still contains the type information it was instantiated with.
        //   2) The FlexJson was loaded from a string and consequently has no type information associated with it.
        object first = json.Values.First();
        TData[] values = typeof(TData[]).IsAssignableFrom(first.GetType())
            ? (TData[])first
            : ((IEnumerable<object>)first).Select(value => (TData) value).ToArray();

        TCollection collection = default;
        
        try
        {
            collection = json.Require<TCollection>(KEY_COLLECTION);
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
    }
    
    [Theory, MemberData(nameof(TestData_Collections))]
    public void IEnumerable(FlexJson json) => Casting_Collection<IEnumerable<TData>>(json);
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Array(FlexJson json) => Casting_Collection<TData[]>(json);
    [Theory, MemberData(nameof(TestData_Collections))]
    public void List(FlexJson json) => Casting_Collection<List<TData>>(json);
    [Theory, MemberData(nameof(TestData_Collections))]
    public void HashSet(FlexJson json) => Casting_Collection<HashSet<TData>>(json);
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Queue(FlexJson json) => Casting_Collection<Queue<TData>>(json);
    [Theory, MemberData(nameof(TestData_Collections))]
    public void Stack(FlexJson json) => Casting_Collection<Stack<TData>>(json);
}
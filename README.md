# FlexJson

A saner way to work with JSON, especially within the context of working with MongoDB.

## Project History

I spent three years working as the Lead Platform Engineer at R Studios, a small gaming studio with a tech stack heavily reliant on microservices, JSON, and MongoDB.  Since C# is a strongly typed language, we struggled to consistently serialize generic data to BSON.  MongoDB would store any models we had with their full type information and a lot of junk rather than break them down to primitive values.  FlexJson originated as a way to accept a generic JSON blob from frontend clients, convert it to a `Dictionary<string, object>`, and then serialize it to BSON so that we could query any of that data from MongoDB.

The project quickly evolved in scope.  With around two dozen microservices, our backend codebase was quickly overwhelmed by `JsonDocument` and `BsonDocument` de/serialization nightmares.  Quality of life improvements were added to reduce the number of decorations we had to add to our models.

## Usage

### Reducing Serialization Boilerplate

Working with JSON isn't simple in C#, even with modern `System.Text.Json`.  The easiest way to work with JSON in stock C# is to have a custom class that maps exactly to a JSON blob:

```csharp
public class Foo
{
    public DateTimeOffset Date { get; set; }
    public string Message { get; set; }
    public Dictionary<string, int> Range { get; set; }
}

public class Program
{
    public static void Main(string[] args)
    {
        string json =
            """
            {
              "Date": "2025-10-01T00:00:00-07:00",
              "Message": "Hello, World!",
              "Range": {
                "Low": 100
                "High": 9999
              }
            }
            """;
        Foo foo = JsonSerializer.Deserialize<Foo>(json);
    }
}
```

And of course, it's far worse if you need to try to parse individual `JsonElement` nodes rather than serialize the entire blob.

This gets ugly quickly when you have a lot of data coming in, especially when it comes to nested objects.  Especially for us at R Studios, our frontend and backend codebases were decoupled; most of the time, the JSON blobs coming in from the clients did not match 1:1 with our models on the backend.

With `FlexJson`, there's no need to have custom models for every single JSON blob, and the methods make working with individual properties much easier:

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        string json =
            """
            {
              "Date": "2025-10-01T00:00:00-07:00",
              "Message": "Hello, World!",
              "Range": {
                "Low": 100
                "High": 9999
              }
            }
            """;
        FlexJson flex = json;
        string message = flex.Require<string>("Message");
        DateTimeOffset date = flex.Optional<DateTimeOffset>("Date");
        
        // Note: if you did have a model, you could also use `flex.ToModel<Foo>()`, or 
        // flex.Require<Foo>("key") if the model was nested in the JSON.
    }
}
```

On its face, this may not seem like much of a reduction by sheer volume, but the helper methods were our bread and butter.  Being able to easily work with JSON without the verbosity of `JsonElement.GetString()` (or any of the other type-specific methods) really made our codebase more readable and maintainable.  In particular, a lot of QOL has been added to the `Optional<T>` / `Require<T>` methods when it comes to types.  If your incoming JSON wraps numeric values in quotes, `Optional<int>` will automatically parse that value into an integer for you.

In addition, the default `JsonSerializerOptions` are incredibly fragile.  Occasionally our services received "malformed" JSON in the form of trailing commas or comments added to the JSON.  It's easy to say that incoming JSON should just be fixed, but minor issues shouldn't be blockers for the backend.

### Reducing Attribute Bloat

Consider the following example of a single property in a model we might have written:

```csharp
public class User : Model
{
    [BsonElement("email"), BsonIgnoreIfNull]
    [JsonInclude, JsonPropertyName("emailAddress"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Email { get; set; }
}
```

The goal here is simple: we just want the frontend-facing key to be more "human-readable" than what gets stored in MongoDB, and we want to omit the property in both the JSON / BSON when the value is null.  Decoupling the keys from the name of the property allows us to rename or refactor properties however we see fit, too.

With `FlexJson`, we can reduce this to:

```csharp
public class User : Model
{
    [FlexKeys(json: "emailAddress", bson: "email", Ignore.WhenNull)]
    public string Email { get; set; }
}
```

If you only want the model omitted in specific cases or isolated from one side of your application, the `Ignore` flags allow you more shorthand via flags:

```csharp
[Flags]
public enum Ignore
{
    Never = 0,
    InJson = 0b_0000_0001,
    InBson = 0b_0000_0010,
    WhenJsonNull = 0b_0001_0000,
    WhenJsonDefault = 0b0010_0000,
    WhenJsonNullOrDefault = 0b0011_0000,
    WhenBsonNull = 0b_0100_0000,
    WhenBsonDefault = 0b1000_0000,
    WhenBsonNullOrDefault = 0b1100_0000,
    WhenNull = 0b_0101_0000,
    WhenDefault = 0b_1010_0000,
    WhenNullOrDefault = 0b_1111_0000,
    Always = int.MaxValue
}
```

These can be combined to create complex rules, and with far less text than it takes to add separate `BsonIgnore` and `JsonIgnore` attributes.

### Models

One of the most-used features of `FlexJson` in our daily work at R Studios was the ability to automatically validate our data models.  

```csharp
public class Message : Model
{
    [FlexKeys(json: "body", bson: "body", Ignore.WhenNull)]
    public string Body { get; set; }
    
    [FlexKeys(json: "type", bson: "type", Ignore.WhenNull)]
    public MessageType Type { get; set; }
    
    [FlexKeys(json: "attachments", bson: "data", Ignore.WhenNull)]
    public FlexJson Attachments { get; set; }
    protected override void Validate(out List<string> errors)
    {
        errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(Body) && (Attachments == null || !Attachments.Keys.Any()))
            errors.Add($"Messages can be blank, but if this is the case, you must provide 'attachments'.");
        if (Type != MessageType.Unassigned)
            errors.Add("Sending a message with an explicit message type is not allowed, it is server-authoritative.");
    }
}

FlexJson json = """{"message": { ... }}""";          // incoming JSON from a client
Message message = json.Require<Message>("message");  // Validation automatically applied!
```

After the `Message` here is deserialized, the `Validate()` method is automatically called.  If that `List<string>` of errors is non-empty, an exception is thrown.  This makes it difficult to accidentally overlook data validation when accepting JSON across multiple endpoints.

Boilerplate doesn't need to make your eyes glaze over!  Let `FlexJson` help you out!

### Real World Use Case: chat-service

Our lead gameplay engineer once asked if I could pipe a raw JSON blob from the client through to MongoDB directly.  Specifically, we had an in-game chat service, and he wanted to add special attachments to messages.  These might be deep links to a player's items, special formatting rules, or custom emoji sprites - all of which are specific to the game client and have no meaning to the backend.  Other than imposing a raw character limit, the backend didn't really care about the incoming data.

At that time, all our deserialization required model updates.  If I sent the JsonDocument directly to MongoDB, the Mongo driver serialized it into type / version / binary information and wasn't queryable through MongoDB Atlas / MongoDB Compass.

The immediate solution we used was to stringify the `Attachment` JSON and store it as a single string in MongoDB.  It was klugey and required regex queries to search through, but it worked for a quick 15-minute solution.  Over time, `FlexJson` allowed us to cleanly split that JSON out into its own raw values, more akin to a proper `BsonDocument`.

## Getting Started

Simply adding the package and using the `FlexJson` type is all you need to use the tools here, but you may want to capture any errors that might occur.  You can do this with a simple call in your application's startup:

```csharp
FlexJson.Configure(log =>
{
    string message = log.Message;
    int severity = log.Severity;
    object data = log.Data;
    // Send these to your own logging system to reroute.
});  // You can optionally use a second parameter (bool) to validate models when deserialized in Require<T>().
```

## What's Next For This Project?

The code is in a rough state in the sense that namespaces will shift.  The project is still a carved-out piece of our monolithic framework package we used internally.  However, we used it at R Studios constantly in our services with very few issues for years.

I'll be adding more documentation and XML comments as I continue to clean the project up.  Once that's done, there will be some work to do with de/serialization performance.  Some of the bugs we encountered were very roughly patched in a crunch, and there are better ways to approach those edge cases.
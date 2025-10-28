using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;
using Maynard.Json.Exceptions;
using Maynard.Json.Utilities;
using Maynard.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace Maynard.Json;

public abstract class FlexModel
{
    private static readonly HashSet<Type> _registeredTypes = new();

    /// <summary>
    /// A self-containing wrapper for use in generating JSON responses for models.  All models should
    /// contain their data in a JSON field using their own name.  For example, if we have an object Foo, the JSON
    /// should be:
    ///   "foo": {
    ///     /* the Foo's JSON-serialized data */
    ///   }
    /// While it may be necessary to override this property in some cases, this should help enforce consistency
    /// across services.
    /// </summary>
    public string ToJson()
    {
        try
        {
            return JsonSerializer.Serialize(this, typeof(FlexModel), JsonHelper.SerializerOptions);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            return null;
        }
    }

    /// <summary>
    /// This is called automatically when a model is deserialized from <see cref="FlexJson.Require{T}" /> or
    /// <see cref="FlexJson.Optional{T}" />.
    /// You may overload the method Validate(out <see cref="List{string}" /> errors).  If you do, and there are any
    /// errors in the out List, this method throws an exception when deserializing.
    /// </summary>
    public void Validate()
    {
        Validate(out List<string> errors);

        if (!errors.Any())
            return;
        
        Throw.Ex<object>(new ModelValidationException(this, errors));
    }

    /// <summary>
    /// Used by platform-common to dynamically call the Validate function.  This is used to validate on deserialization.
    /// The parameter type is not a Model because this is called from generic types.
    /// </summary>
    /// <param name="model"></param>
    internal static void Validate(object model)
    {
        try
        {
            model
                ?.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(method => method.Name == nameof(Validate))
                .Invoke(obj: model, parameters: null);
        }
        catch (Exception e)
        {
            Exception nested = e?.InnerException;
            
            while ((nested = nested?.InnerException) != null)
                if (nested is ModelValidationException)
                    Throw.Ex<object>(nested);
            Throw.Ex<object>(e);
        }
    }

    // TODO: Use an interface or make this abstract to force its adoption?
    protected virtual void Validate(out List<string> errors) => errors = [];

    protected static void Test(bool condition, string error, ref List<string> errors)
    {
        errors ??= [];
        if (!condition)
            errors.Add(error);
    }

    public static T FromJSON<T>(string json) where T : FlexModel => JsonSerializer.Deserialize<T>(json, JsonHelper.SerializerOptions);

    public static implicit operator FlexModel(string json) => FromJSON<FlexModel>(json);

    /// <summary>
    /// This alias is needed to circumnavigate ambiguous reflection issues for registration.
    /// </summary>
    private static void RegisterWithMongo<T>()
    {
        Type type = typeof(T);
        if (_registeredTypes.Contains(type))
            return;

        // Recursively register the base type first, if it's a FlexModel and not object.
        Type baseType = type.BaseType;
        if (baseType != null && baseType != typeof(FlexModel) && baseType != typeof(object) && typeof(FlexModel).IsAssignableFrom(baseType))
        {
            MethodInfo info = typeof(FlexModel).GetMethod(nameof(RegisterWithMongo), BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo generic = info?.MakeGenericMethod(baseType);
            generic?.Invoke(null, null);
        }

        BsonClassMap.RegisterClassMap<T>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        
            // Use DeclaredOnly to ensure we only map properties for the current class in the hierarchy.
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        
            foreach (PropertyInfo prop in properties)
            {
                FlexKeys flexKeys = prop.GetCustomAttribute<FlexKeys>();
                FlexIgnore flexIgnore = prop.GetCustomAttribute<FlexIgnore>();
                BsonIdAttribute bsonId = prop.GetCustomAttribute<BsonIdAttribute>();
                BsonElementAttribute bsonElement = prop.GetCustomAttribute<BsonElementAttribute>();
                BsonIgnoreIfNullAttribute bsonIgnoreIfNull = prop.GetCustomAttribute<BsonIgnoreIfNullAttribute>();
                BsonIgnoreIfDefaultAttribute bsonIgnoreIfDefault = prop.GetCustomAttribute<BsonIgnoreIfDefaultAttribute>();
        
                string key = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
                Ignore policy = Ignore.Never;
        
                // Set the key
                if (bsonId != null)
                {
                    key = "_id";
                    if (!string.IsNullOrWhiteSpace(flexKeys?.Bson) || !string.IsNullOrWhiteSpace(bsonElement?.ElementName))
                        Log.Warn($"Found attempts at overriding the BsonId field for a model via {nameof(FlexKeys)} or {nameof(BsonElement)} attributes.  This is not allowed.", data: new
                        {
                            Type = typeof(T),
                            Property = prop.Name,
                            FlexKeysBson = flexKeys?.Bson,
                            BsonElement = bsonElement?.ElementName
                        });
                }
                else if (!string.IsNullOrWhiteSpace(flexKeys?.Bson))
                {
                    key = flexKeys.Bson;
                    if (!string.IsNullOrWhiteSpace(bsonElement?.ElementName))
                        Log.Warn($"Found two BSON key names when preparing serialization maps.  {nameof(FlexKeys)} attributes are preferred and thus have priority.", data: new
                        {
                            Type = typeof(T),
                            Property = prop.Name,
                            FlexKeysName = flexKeys.Bson,
                            BsonElementName = bsonElement.ElementName,
                            Identical = flexKeys.Bson == bsonElement.ElementName
                        });
                }
                else if (!string.IsNullOrWhiteSpace(bsonElement?.ElementName))
                    key = bsonElement.ElementName;
        
                // Set the ignore policy
                if (flexIgnore != null)
                {
                    policy = flexIgnore.Ignore;
                    if (bsonIgnoreIfNull != null || bsonIgnoreIfDefault != null)
                        Log.Warn($"Found more than one BSON ignore attributes when preparing serialization maps.  {nameof(FlexIgnore)} attributes are more specific and thus have priority.", data: new
                        {
                            Type = typeof(T),
                            Property = prop.Name,
                            FlexIgnore = policy.ToString()
                        });
                }
                else if (bsonIgnoreIfNull != null || bsonIgnoreIfDefault != null)
                {
                    if (bsonIgnoreIfNull != null)
                        policy |= Ignore.WhenBsonNull;
                    if (bsonIgnoreIfDefault != null)
                        policy |= Ignore.WhenBsonDefault;
                    if (flexKeys != null)
                        Log.Warn($"Found a BsonIgnore attribute in addition to a {nameof(FlexKeys)} attribute.  The latter allows more specificity and is preferred.");
                }
                else if (flexKeys != null)
                    policy = flexKeys.Ignore;
                else
                    Log.Warn($"Could not find an ignore policy for a property in a data model.  Best practice is to mark the model with {nameof(FlexKeys)} or {nameof(FlexIgnore)}.", data: new
                    {
                        Type = typeof(T),
                        Property = prop.Name
                    });
        
                if (bsonId != null && policy != Ignore.Never)
                {
                    Log.Warn("Found an attempt to override the ignore policy on _id.  This is not allowed and will be ignored.");
                    policy = Ignore.Never;
                }

                if (policy.HasFlag(Ignore.InBson))
                {
                    cm.UnmapMember(prop);
                    return;
                }

                BsonMemberMap map = cm.MapMember(prop).SetElementName(key);
                
                // Bafflingly, mongo's driver claims IgnoreIfDefault / IgnoreIfNull are mutually exclusive, and throws an
                // exception if both methods are used together.  Since a nullable type set to default is null, default
                // has priority.
                if (policy.HasFlag(Ignore.WhenBsonDefault))
                    map.SetIgnoreIfDefault(true);
                else if (policy.HasFlag(Ignore.WhenBsonNull))
                    map.SetIgnoreIfNull(true);
            }
        });
        _registeredTypes.Add(type);
        Log.Verbose("Registered " + typeof(T));
    }

    // When using nested custom data structures, Mongo will often be confused about de/serialization of those types.  This can lead to uncaught exceptions
    // without much information available on how to fix it.  Typically, this is achieved from running the following during Startup:
    //      BsonClassMap.RegisterClassMap<YourTypeHere>();
    // However, this is inconsistent with the goal of platform-common.  Common should reduce all of this frustrating boilerplate.
    // We can do this with reflection instead.  It's a slower process, but since this only happens once at Startup it's not significant.
    // TODO: Do not process this if mongo is disabled
    // TODO: Better error messages
    public static void RegisterModelsWithMongo(Type[] importedTypes = null)
    {
        importedTypes ??= [];
        Type[] models = Assembly
            .GetEntryAssembly()
            ?.GetExportedTypes()                                        // Add the project's types 
            .Concat(importedTypes)                                      // Add parent library's types, if any
            .Concat(Assembly.GetExecutingAssembly().GetExportedTypes()) // Add calling assembly's types
            .Where(type => !type.IsAbstract)
            .Where(type => type.IsAssignableTo(typeof(FlexModel)))
            .ToArray()
        ?? [];

        List<string> unregisteredTypes = new();
        foreach (Type type in models)
            try
            {
                MethodInfo info = typeof(FlexModel).GetMethod(nameof(RegisterWithMongo), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo generic = info?.MakeGenericMethod(type);

                // TODO: Mongo currently can't process nested data models with generic types
                // Unsure the best method of registering these, but further investigation needed.  For now just exit early.
                if (type.ContainsGenericParameters)
                {
                    Log.Warn($"Unable to register {type.FullName} with Mongo; it uses a nested generic type constraint.");
                    continue;
                }

                generic?.Invoke(obj: CreateFromSmallestConstructor(type), null);
            }
            catch (Exception e)
            {
                unregisteredTypes.Add(type.Name);
                Log.Warn("Unable to register a data model with Mongo.  In rare occasions this can cause deserialization exceptions when reading from the database.", data: new
                {
                    Name = type.FullName,
                    Exception = e
                });
            }
        
        if (unregisteredTypes.Any())
            Log.Warn("Some Models were not able to be registered.", data: new
            { 
                Types = unregisteredTypes
            });
        
        Log.Good("Registered Models with Mongo.");
        RegisterSerializer(models);
    }

    private static bool _serializerRegistered;
    /// <summary>
    /// This is a critical fix for a breaking change introduced in Mongo's C# driver v2.19.  Without this, models can fail
    /// de/serialization when they use custom types or collections of custom types.  It's an insane change that's poorly documented
    /// and likely an overcorrection of some edge case they had with remote code execution.  The driver upgrade began disallowing
    /// types that we have to now manually whitelist via an ObjectSerializer definition.
    ///
    /// https://jira.mongodb.org/browse/CSHARP-4581
    /// </summary>
    /// <param name="models">All models used in the project.</param>
    private static void RegisterSerializer(Type[] models)
    {
        if (_serializerRegistered)
        {
            Log.Warn("Already created an object serializer.  Ignoring subsequent calls.");
            return;
        }
        try
        {
            string[] namespaces = models
                .Select(type => type.FullName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Contains('.') 
                    ? name[..name.IndexOf('.')] 
                    : name
                )
                .Distinct()
                .ToArray();
        
            // This is the important bit; return true in this Func to allow a type through.  It's evaluated at runtime
            // when the serializer needs to run.  Keep in mind that a List<Model> will have a namespace of
            // System.Collections.Generic.List`1[Your.Namespace.Model], so you can't rely on a StartsWith() to determine
            // whether or not a type is allowed.  Consequently, we just check to see if the first bit of namespaces used in
            // the project is used anywhere in the incoming type.  We can be stricter by modifying the above LINQ query
            // if necessary.
            BsonSerializer.RegisterSerializer(new ObjectSerializer(type =>
            {
                string name = type?.FullName;

                if (string.IsNullOrWhiteSpace(name) || name.IndexOf('.') <= 0)
                    return ObjectSerializer.DefaultAllowedTypes(type);
                return ObjectSerializer.DefaultAllowedTypes(type) || namespaces.Any(value => name.Contains(value));
            }));
            _serializerRegistered = true;
            Log.Verbose("Object serializer registered.", data: new
            {
                Types = models.Select(type => type.Name)
            });
        }
        catch (Exception e)
        {
            Log.Error("Unable to create a BsonSerializer!  In 2.19.0+, this can mean that your serialization will fail!", data: e);
        }
    }
    
    /// <summary>
    /// This method uses reflection to create an instance of a Model type.  This is required to register models
    /// with Mongo's class map.  Without updating the class map, it's possible to run into deserialization errors.  Unfortunately,
    /// Mongo requires an instance to do this as opposed to using reflection itself, so this method will find the shortest constructor,
    /// attempt to instantiate the type with default parameter values, and return it for use with RegisterWithMongo.
    /// </summary>
    private static FlexModel CreateFromSmallestConstructor(Type type)
    {
        ConstructorInfo[] constructors = type.GetConstructors(bindingAttr: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        ConstructorInfo min = constructors.MinBy(info => info.GetParameters().Length);

        List<object> _params = [];
        foreach (ParameterInfo info in min.GetParameters())
        {
            // Primitive datatypes
            if (info.ParameterType.IsValueType)
            {
                _params.Add(Activator.CreateInstance(info.ParameterType));
                continue;
            }

            // This covers constructors that use the params attribute.  It attempts to create an empty array of the specified type.
            if (info.GetCustomAttributes(typeof(ParamArrayAttribute), false).Any())
            {
                _params.Add(typeof(Array)
                    .GetMethod(nameof(Array.Empty))
                    // ReSharper disable once AssignNullToNotNullAttribute
                    ?.MakeGenericMethod(info.ParameterType.GetElementType())
                    .Invoke(null, null));
                continue;
            }
            
            // The parameter is a reference type.
            _params.Add(null);
        }

        return (FlexModel)min.Invoke(_params.ToArray());
    }
}
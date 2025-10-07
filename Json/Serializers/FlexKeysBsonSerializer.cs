using System.Text.Json;
using Maynard.Json.Enums;
using Maynard.Logging;
using MongoDB.Bson.Serialization.Attributes;

namespace Maynard.Json.Serializers;

using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Attributes;

public class FlexKeysBsonSerializer<T> : SerializerBase<T> where T : class
{
    private readonly Dictionary<string, PropertyInfo> _bsonToProperty = new();
    private readonly Dictionary<PropertyInfo, string> _propertyToBson = new();
    private readonly Dictionary<PropertyInfo, Ignore> _propertyIgnorePolicy = new();
    private readonly Dictionary<PropertyInfo, IBsonSerializer> _propertySerializers = new();

    public FlexKeysBsonSerializer()
    {
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

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
            
            _bsonToProperty[key] = prop;
            _propertyToBson[prop] = key;
            _propertyIgnorePolicy[prop] = policy;
            _propertySerializers[prop] = bsonId != null
                ? BsonSerializer.LookupSerializer<ObjectId>()
                : BsonSerializer.LookupSerializer(prop.PropertyType);
        }
    }

    public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        IBsonReader reader = context.Reader;
        
        if (reader.GetCurrentBsonType() != BsonType.Document)
            throw new BsonSerializationException("Expected Document");

        T instance = Activator.CreateInstance<T>();
        reader.ReadStartDocument();

        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            string elementName = reader.ReadName();
            
            if (_bsonToProperty.TryGetValue(elementName, out PropertyInfo property))
            {
                IBsonSerializer serializer = _propertySerializers[property];
                object value = elementName == "_id"
                    ? serializer.Deserialize(context).ToString()
                    : serializer.Deserialize(context);
                property.SetValue(instance, value);
            }
            else
                reader.SkipValue(); // Skip unknown elements
        }

        reader.ReadEndDocument();
        return instance;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
    {
        IBsonWriter writer = context.Writer;
        
        writer.WriteStartDocument();

        foreach ((PropertyInfo property, string bsonKey) in _propertyToBson)
        {
            Ignore policy = _propertyIgnorePolicy[property];
            
            object propertyValue = property.GetValue(value);
            // Early out if we're on the _id field, since it can't be ignored.
            if (bsonKey == "_id")
            {
                writer.WriteName(bsonKey);
                switch (propertyValue)
                {
                    case ObjectId asObjectId:
                        writer.WriteObjectId(asObjectId);
                        break;
                    case string asString when !string.IsNullOrWhiteSpace(asString):
                        writer.WriteObjectId(new(asString));
                        break;
                    default:
                        writer.WriteObjectId(ObjectId.GenerateNewId());
                        break;
                }
                continue;
            }
            
            // Check the ignore policies for all other types

            if (propertyValue == null && (policy.HasFlag(Ignore.WhenBsonNull) || policy.HasFlag(Ignore.WhenBsonDefault)))
                continue;
            if (policy.HasFlag(Ignore.WhenBsonDefault) && property.PropertyType.IsValueType)
            {
                object defaultValue = Activator.CreateInstance(property.PropertyType);
                if (Equals(propertyValue, defaultValue))
                    continue;
            }

            writer.WriteName(bsonKey);
            IBsonSerializer serializer = _propertySerializers[property];
            serializer.Serialize(context, propertyValue);
        }

        writer.WriteEndDocument();
    }
}
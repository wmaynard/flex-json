using Maynard.Json.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace Maynard.Json.Serializers;

using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Maynard.Json.Attributes;

public class FlexKeysBsonSerializer<T> : SerializerBase<T> where T : class, new()
{
    private readonly Dictionary<string, PropertyInfo> _bsonToProperty = new();
    private readonly Dictionary<PropertyInfo, string> _propertyToBson = new();
    private readonly Dictionary<PropertyInfo, bool> _propertyPreserveNulls = new();
    private readonly Dictionary<PropertyInfo, IBsonSerializer> _propertySerializers = new();

    public FlexKeysBsonSerializer()
    {
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (PropertyInfo prop in properties)
        {
            FlexKeys flexKeys = prop.GetCustomAttribute<FlexKeys>();
            BsonIdAttribute bsonId = prop.GetCustomAttribute<BsonIdAttribute>();
            BsonElementAttribute bsonElement = prop.GetCustomAttribute<BsonElementAttribute>();
            BsonIgnoreIfNullAttribute bsonIgnoreIfNull = prop.GetCustomAttribute<BsonIgnoreIfNullAttribute>();
            
            if (flexKeys != null)
            {
                _bsonToProperty[flexKeys.Bson] = prop;
                _propertyToBson[prop] = flexKeys.Bson;
                _propertyPreserveNulls[prop] = flexKeys.PreserveNulls;
            }
            else if (bsonId != null)
            {
                _bsonToProperty["_id"] = prop;
                _propertyToBson[prop] = "_id";
                _propertyPreserveNulls[prop] = true;
            }
            else if (bsonElement != null)
            {
                _bsonToProperty[bsonElement.ElementName] = prop;
                _propertyToBson[prop] = bsonElement.ElementName;
                _propertyPreserveNulls[prop] = bsonIgnoreIfNull != null;
            }
            else
            {
                // Fall back to property name if no FlexKeys attribute
                _bsonToProperty[prop.Name] = prop;
                _propertyToBson[prop] = prop.Name;
                _propertyPreserveNulls[prop] = false;
            }

            // Get or create serializer for property type
            _propertySerializers[prop] = bsonId != null
                ? BsonSerializer.LookupSerializer(typeof(ObjectId))
                : BsonSerializer.LookupSerializer(prop.PropertyType);
        }
    }

    public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        IBsonReader reader = context.Reader;
        
        if (reader.GetCurrentBsonType() != BsonType.Document)
            throw new BsonSerializationException("Expected Document");

        T instance = new();
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
            bool preserveNulls = _propertyPreserveNulls[property];
            
            object propertyValue = property.GetValue(value);
            if (bsonKey == "_id")
                propertyValue ??= ObjectId.GenerateNewId();

            if (propertyValue == null && !preserveNulls)
                continue;

            writer.WriteName(bsonKey);
            if (bsonKey == "_id")
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
            else
            {
                IBsonSerializer serializer = _propertySerializers[property];
                serializer.Serialize(context, propertyValue);
            }
        }

        writer.WriteEndDocument();
    }
}
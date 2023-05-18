using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Couchbase.AspNet.Utils
{
    public static class SerializationUtil
    {
        public static T Deserialize<T>(byte[] bytes) where T : class
        {
            var jsonStr = Encoding.UTF8.GetString(bytes);
            if (!string.IsNullOrWhiteSpace(jsonStr))
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    Converters = new List<JsonConverter> { new NameValueJsonConverter<NameValueCollection>() }
                };
                return JsonConvert.DeserializeObject<T>(jsonStr, settings);
            }
            return null;
        }

        public static byte[] Serialize(object value)
        {
            //if already a byte array just return it
            if (value.GetType() == typeof(byte[]))
            {
                return (byte[])value;
            }

            //if not convert it to a JSON byte array
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms))
                {
                    using (var jr = new JsonTextWriter(sw))
                    {
                        var settings = new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.All,
                            Converters = new List<JsonConverter> { new NameValueJsonConverter<NameValueCollection>() }
                        };
                        var serializer = JsonSerializer.Create(settings);
                        serializer.Serialize(jr, value);
                    }
                }
                return ms.ToArray();
            }
        }

        // custom converter since NewtonsoftJson doesn't convert NameValueCollection
        public class NameValueJsonConverter<TNameValueCollection> : JsonConverter
            where TNameValueCollection : NameValueCollection, new()
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(TNameValueCollection).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                while (reader.TokenType == JsonToken.Comment && reader.Read());
                if (reader.TokenType == JsonToken.Null)
                    return null;

                if (reader.TokenType != JsonToken.StartObject)
                    throw new JsonException();

                var collection = (TNameValueCollection)existingValue ?? new TNameValueCollection();
                
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.EndObject)
                        return collection;

                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        var propertyName = reader.Value?.ToString();
                        if (propertyName != "$type")
                        {
                            reader.Read();
                            if (reader.TokenType != JsonToken.StartObject)
                                throw new JsonException();
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonToken.EndObject)
                                    break;
                                if (reader.TokenType == JsonToken.PropertyName)
                                {
                                    var propertyName2 = reader.Value?.ToString();
                                    if (propertyName2 == "$values")
                                    {
                                        reader.Read();
                                        if (reader.TokenType != JsonToken.StartArray)
                                            throw new JsonException();
                                        while (reader.Read())
                                        {
                                            if (reader.TokenType == JsonToken.EndArray)
                                                break;
                                            var value = reader.Value?.ToString();
                                            collection.Add(propertyName, value);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                throw new JsonException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var collection = (TNameValueCollection)value;

                writer.WriteStartObject();

                if (serializer.TypeNameHandling == TypeNameHandling.All)
                {
                    writer.WritePropertyName("$type");
                    writer.WriteValue(GetShortAssemblyQualifiedName(typeof(TNameValueCollection)));
                }

                foreach (var key in collection.AllKeys)
                {
                    writer.WritePropertyName(key);
                    writer.WriteStartObject();
                    writer.WritePropertyName("$type");
                    writer.WriteValue(GetShortAssemblyQualifiedName(typeof(string[])));
                    writer.WritePropertyName("$values");
                    writer.WriteStartArray();
                    foreach (var item in collection.GetValues(key))
                    {
                        writer.WriteValue(item);
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            private static string GetShortAssemblyQualifiedName(Type type) => 
                $"{type.FullName}, {type.Assembly.GetName().Name}";
        }
    }
}

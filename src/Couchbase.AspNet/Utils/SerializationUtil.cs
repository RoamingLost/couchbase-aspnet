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
                return JsonConvert.DeserializeObject<T>(jsonStr);
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
                        var serializer = JsonSerializer.Create();
                        serializer.Serialize(jr, value);
                    }
                }
                return ms.ToArray();
            }
        }
    }
}

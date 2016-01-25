using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Ipc
{
    public class MsDataContractJsonSerializer : IObjectSerializer
    {
        public string SerializeObject<T>(T instance) where T : class
        {
            if (instance == null) return null;

            string jsonStr;
            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof (T));
                serializer.WriteObject(ms, instance);
                jsonStr = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int) ms.Length);
            }

            return jsonStr;
        }

        public T DeserializeObject<T>(string jsonStr) where T : class
        {
            if (string.IsNullOrEmpty(jsonStr)) return null;

            T instance;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonStr)))
            {
                var serializer = new DataContractJsonSerializer(typeof (T));
                instance = (T) serializer.ReadObject(ms);
            }

            return instance;
        }
    }
}
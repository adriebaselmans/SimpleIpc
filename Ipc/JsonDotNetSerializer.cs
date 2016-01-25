using Newtonsoft.Json;

namespace Ipc
{
    public class JsonDotNetSerializer : IObjectSerializer
    {
        public string SerializeObject<T>(T instance) where T : class
        {
            if (instance == null) return null;
            return JsonConvert.SerializeObject(instance);
        }

        public T DeserializeObject<T>(string jsonStr) where T : class
        {
            if (string.IsNullOrEmpty(jsonStr)) return null;
            return JsonConvert.DeserializeObject<T>(jsonStr);
        }
    }
}
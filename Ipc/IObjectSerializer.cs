namespace Ipc
{
    public interface IObjectSerializer
    {
        string SerializeObject<T>(T instance) where T : class;
        T DeserializeObject<T>(string jsonStr) where T : class;
    }
}
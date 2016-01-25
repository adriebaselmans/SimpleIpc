using System.Reflection;

namespace Ipc
{
    public interface IProxyDispatch
    {
        object OnProxyDispatch(MethodInfo methodInfo, object[] args);
    }
}
using System.Runtime.Serialization;

namespace Ipc
{
    [DataContract]
    public class FunctionCallResponseMessage
    {
        [DataMember]
        public bool CaughtException { get; set; }

        [DataMember]
        public bool IsBulky { get; set; }

        [DataMember]
        public object ReturnValue { get; set; }
    }
}
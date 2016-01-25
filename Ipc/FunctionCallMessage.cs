using System;
using System.Runtime.Serialization;

namespace Ipc
{
    [DataContract]
    public class FunctionCallMessage
    {
        [DataMember]
        public string MethodName { get; set; }

        [DataMember]
        public object[] Arguments { get; set; }
    }
}
using System;
namespace Ipc
{
    public struct FunctionCallResult : IDisposable
    {
        public bool DidSucceed { get; set; }
        public object ReturnValue { get; set; }
        public string ExceptionMessage { get; set; }

        public void Dispose()
        {
            if (ReturnValue != null)
            {
                var disposable = ReturnValue as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                    disposable = null;
                }

                ReturnValue = null;
            }
        }
    }
}
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Ipc
{
    public class IpcClient<T> : IDisposable, IProxyDispatch, IBulkProgressNotification
    {
        private readonly IObjectSerializer _serializer;
        private readonly TcpClient _tcpClient;
        private bool _disposed;

        private NetworkStream _networkStream;
        private StreamReader _streamReader;
        private StreamWriter _streamWriter;

        public event Action<double> BulkProgress;

        public IpcClient(IPAddress ipAddress, int port, IObjectSerializer serializer)
        {
            _serializer = serializer;
            Proxy = ProxyFactory.CreateProxy<T>(this);
            _tcpClient = new TcpClient();
            Connect(ipAddress, port);
        }

        public T Proxy { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public object OnProxyDispatch(MethodInfo methodInfo, object[] args)
        {
            SendFunctionCallToServer(methodInfo, args);

            var responseMessage = WaitForResponseMessageFromServer();
            if (responseMessage.CaughtException)
            {
                throw new Exception(responseMessage.ReturnValue.ToString());
            }
            
            if (responseMessage.IsBulky)
            {
                ConfirmBulkResponse();

                int numBytesToRead = int.Parse(responseMessage.ReturnValue.ToString());
               
                var memoryStream = new MemoryStream(numBytesToRead);
                byte[] buffer = new byte[Constants.BulkBufferSizeInBytes];

                RaiseBulkProgress(0.0);

                int numBytesRemaining = numBytesToRead;
                while (numBytesRemaining > 0)
                {
                    var numBytesRead = _networkStream.Read(buffer, 0, buffer.Length);
                    memoryStream.Write(buffer, 0, numBytesRead);
                    numBytesRemaining -= numBytesRead;
                    RaiseBulkProgress((double)(numBytesToRead - numBytesRemaining) / numBytesToRead * 100.0);
                }

                memoryStream.Position = 0;
                return memoryStream;
            }

            return responseMessage.ReturnValue;
        }

        private void RaiseBulkProgress(double percentage)
        {
            if (BulkProgress != null) BulkProgress(percentage);
        }

        private void ConfirmBulkResponse()
        {
            _streamWriter.WriteLine(Constants.ReadyForBulk);
        }

        private FunctionCallResponseMessage WaitForResponseMessageFromServer()
        {
            var responseStr = _streamReader.ReadLine();

            var responseMessage = _serializer.DeserializeObject<FunctionCallResponseMessage>(responseStr);
            return responseMessage;
        }

        private void SendFunctionCallToServer(MethodInfo methodInfo, object[] args)
        {
            var dispatchMessage = new FunctionCallMessage {MethodName = methodInfo.Name, Arguments = args};
            var functionCallStr = _serializer.SerializeObject(dispatchMessage);
            _streamWriter.WriteLine(functionCallStr);
        }

        private void Connect(IPAddress ipAddress, int port)
        {
            _tcpClient.Connect(ipAddress, port);
            _networkStream = _tcpClient.GetStream();

            _streamReader = new StreamReader(_networkStream);
            _streamWriter = new StreamWriter(_networkStream) {AutoFlush = true};
        }

        private void Disconnect()
        {
            if (_tcpClient.Connected)
            {
                _tcpClient.Close();
                _streamWriter.Close();
                _streamReader.Close();
                _networkStream.Close();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Disconnect();
                }
            }
            _disposed = true;
        }
    }
}
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Ipc
{
    public class IpcServer<T> : IDisposable
    {
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly Dispatcher _dispatcher;
        private readonly IObjectSerializer _serializer;
        private readonly T _instance;
        private readonly TcpListener _tcpListener;

        private bool _disposed;
        
        public event Action<IPEndPoint> ClientConnected;
        public event Action<IPEndPoint> ClientDisconnected;

        public IpcServer(T instance, IPAddress ipAddress, int port, IObjectSerializer serializer) :
            this(instance, Dispatcher.CurrentDispatcher, ipAddress, port, serializer)
        {
            //empty
        }

        public IpcServer(T instance, Dispatcher dispatcher, IPAddress ipAddress, int port, IObjectSerializer serializer)
        {
            _tcpListener = new TcpListener(ipAddress, port);

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _instance = instance;
            _dispatcher = dispatcher;
            _serializer = serializer;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();
                }
            }
            _disposed = true;
        }

        public void Listen()
        {
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(AcceptTcpClientCallback, _tcpListener);
        }

        public void AcceptTcpClientCallback(IAsyncResult ar)
        {   
            IPEndPoint clientEndpoint = null;

            Task.Factory.StartNew(() =>
            {
                _tcpListener.BeginAcceptTcpClient(AcceptTcpClientCallback, _tcpListener);

                var tcpClient = _tcpListener.EndAcceptTcpClient(ar);
            
                clientEndpoint = (IPEndPoint) tcpClient.Client.RemoteEndPoint;

                RaiseClientConnected(clientEndpoint);

                var stream = tcpClient.GetStream();
                var streamReader = new StreamReader(stream);
                var streamWriter = new StreamWriter(stream) {AutoFlush = true};

                try
                {
                    RequestProcessingLoop(tcpClient, streamReader, streamWriter);
                }
                finally 
                {   
                    streamReader.Dispose();
                    streamWriter.Dispose();
                    stream.Dispose();
                    tcpClient.Close();

                    RaiseClientDisconnected(clientEndpoint);    
                }
               
            }, _cancellationToken);
        }

        private void RequestProcessingLoop(TcpClient tcpClient, StreamReader streamReader, StreamWriter streamWriter)
        {
            var shouldStop = false;
            while (tcpClient.Connected && !shouldStop)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var requestString = WaitForClientRequest(streamReader);

                if (string.IsNullOrEmpty(requestString))
                {
                    shouldStop = true;
                }
                else
                {
                    var functionCallMessage = _serializer.DeserializeObject<FunctionCallMessage>(requestString);
                    var methodInfo = GetMethodInfo(functionCallMessage);

                    using (var functionCallResult = ExecuteMethodCallOnDispatcher(methodInfo, functionCallMessage))
                    {
                        if (functionCallResult.DidSucceed)
                        {
                            if (IsBulky(functionCallResult.ReturnValue))
                            {
                                HandleBulk(functionCallResult.ReturnValue, streamWriter, streamReader);
                            }
                            else
                            {
                                HandleNonBulk(functionCallResult.ReturnValue, streamWriter);
                            }
                        }
                        else
                        {
                            HandleException(functionCallResult.ExceptionMessage, streamWriter);
                        }
                    } //functionCallResult can and is guaranteed to be disposed. All bytes have been sent to client via TCP...
                }
            }
        }

        private FunctionCallResult ExecuteMethodCallOnDispatcher(MethodInfo methodInfo, FunctionCallMessage functionCallMessage)
        {
            var functionCallResult = new FunctionCallResult();
           
            _dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                try
                {
                    functionCallResult.ReturnValue = methodInfo.Invoke(_instance, functionCallMessage.Arguments);
                    functionCallResult.DidSucceed = true;
                }
                catch (Exception ex)
                {
                    functionCallResult.DidSucceed = false;
                    functionCallResult.ExceptionMessage = ex.ToString();
                }
            }));
            return functionCallResult;
        }

        private static MethodInfo GetMethodInfo(FunctionCallMessage functionCallMessage)
        {
            var t = typeof (T);
            var methodInfo = t.GetMethod(functionCallMessage.MethodName);
            return methodInfo;
        }

        private static string WaitForClientRequest(StreamReader streamReader)
        {
            string jsonStr;
            try
            {
                jsonStr = streamReader.ReadLine();
            }
            catch (IOException)
            {
                jsonStr = null;
            }
            return jsonStr;
        }

        private void HandleException(object returnValue, StreamWriter streamWriter)
        {
            var responseMsg = new FunctionCallResponseMessage
            {
                IsBulky = false,
                ReturnValue = returnValue,
                CaughtException = true
            };

            var responseMsgJson = _serializer.SerializeObject(responseMsg);
            streamWriter.WriteLine(responseMsgJson);
        }

        private void HandleNonBulk(object returnValue, StreamWriter streamWriter)
        {
            var responseMsg = new FunctionCallResponseMessage
            {
                IsBulky = false,
                ReturnValue = returnValue,
                CaughtException = false
            };

            var reponseStr = _serializer.SerializeObject(responseMsg);
            streamWriter.WriteLine(reponseStr);
        }

        private void HandleBulk(object returnValue, StreamWriter streamWriter, StreamReader streamReader)
        {
            var returnValueStream = returnValue as Stream;

            if (returnValueStream == null)
            {
                throw new InvalidCastException(String.Format("HandleBulk: return value type '{0}' is not supported.'", returnValue.GetType()));
            }
    
            var numBytesToWrite = returnValueStream.Length;

            RespondToClientWithBulkRequest(streamWriter, numBytesToWrite);

            if (DoesClientAcknowledgeBulkRequest(streamReader))
            {
                byte[] buffer = new byte[Constants.BulkBufferSizeInBytes];
                int bytesRead = 0;
                while ((bytesRead = returnValueStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    streamWriter.BaseStream.Write(buffer, 0, bytesRead);
                }
            }
        }

        private void RespondToClientWithBulkRequest(StreamWriter streamWriter, long payloadSizeInBytes)
        {
            var responseMsg = new FunctionCallResponseMessage
            {
                IsBulky = true,
                ReturnValue = payloadSizeInBytes,
                CaughtException = false
            };

            var responseStr = _serializer.SerializeObject(responseMsg);
            streamWriter.WriteLine(responseStr);
        }

        private bool DoesClientAcknowledgeBulkRequest(StreamReader streamReader)
        {
            var clientResponseOnBulkRequest = streamReader.ReadLine();
            return (clientResponseOnBulkRequest != null && clientResponseOnBulkRequest.Equals(Constants.ReadyForBulk, StringComparison.InvariantCultureIgnoreCase));
        }

        private static bool IsBulky(object returnValue)
        {
            if (returnValue == null) return false;
            return returnValue.GetType().BaseType == typeof(Stream);
        }

        private void Stop()
        {
            _tcpListener.Stop();
            _cancellationTokenSource.Cancel();
        }

        private void RaiseClientConnected(IPEndPoint ip)
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {

                if (ClientConnected != null)
                {
                    ClientConnected(ip);
                }

            }), null);
        }

        private void RaiseClientDisconnected(IPEndPoint ip)
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {
                if (ClientDisconnected != null)
                {
                    ClientDisconnected(ip);
                }

            }), null);
        }
    }
}
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Discovery
{
    public class ServiceDiscoverer<T> : Disposable
    {
        private readonly IPAddress _multiCastAddress;
        private readonly int _portNr;
        private readonly Thread _thread;
        private readonly string _typeName;
        private bool _stopRequested;

        public ServiceDiscoverer(IPAddress multiCastAddress, int portNr)
        {
            _typeName = typeof (T).FullName;
            _multiCastAddress = multiCastAddress;
            _portNr = portNr;

            _thread = new Thread(DiscoverLoop);
            _thread.Start();
        }

        public event Action<IPAddress> OnServiceFound;

        private void DiscoverLoop()
        {
            var listener = new UdpClient();
            listener.ExclusiveAddressUse = false;
            var ipEndPoint = new IPEndPoint(IPAddress.Any, _portNr);

            listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.ExclusiveAddressUse = false;

            listener.Client.Bind(ipEndPoint);
            listener.JoinMulticastGroup(_multiCastAddress);

            while (!_stopRequested)
            {
                var advertisement = listener.Receive(ref ipEndPoint);
                var interfaceName = Encoding.UTF8.GetString(advertisement);

                if (_typeName.Equals(interfaceName, StringComparison.CurrentCultureIgnoreCase))
                {
                    var point = ipEndPoint;
                    RaiseOnServiceFound(point.Address);
                }
            }
        }

        private void RaiseOnServiceFound(IPAddress ipAddress)
        {
            if (OnServiceFound != null) OnServiceFound(ipAddress);
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed && disposing)
            {
                _stopRequested = true;
                _thread.Join();
            }
            base.Dispose(disposing);
        }
    }
}
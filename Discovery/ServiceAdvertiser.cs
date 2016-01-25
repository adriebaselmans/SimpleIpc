using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Discovery
{
    public class ServiceAdvertiser<T> : Disposable
    {
        private readonly Timer _advertiseTimer;
        private readonly IPEndPoint _ipEndPoint;
        private readonly string _typeName;
        private readonly UdpClient _udpClient;

        public ServiceAdvertiser(IPAddress multicastAddress, int portNr, TimeSpan advertiseInterval)
        {
            _typeName = typeof (T).FullName;

            _udpClient = new UdpClient();
            _udpClient.JoinMulticastGroup(multicastAddress);
            _ipEndPoint = new IPEndPoint(multicastAddress, portNr);

            _advertiseTimer = new Timer(OnTimerAdvertiseInterface, null, TimeSpan.FromMilliseconds(0), advertiseInterval);
        }

        public event Action<DateTime> OnAdvertisement;

        private void OnTimerAdvertiseInterface(object state)
        {
            if (OnAdvertisement != null) OnAdvertisement(DateTime.Now);

            var advertisement = Encoding.UTF8.GetBytes(_typeName);
            _udpClient.Send(advertisement, advertisement.Length, _ipEndPoint);
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed && disposing)
            {
                _advertiseTimer.Dispose();
                _udpClient.Close();
            }
            base.Dispose(disposing);
        }
    }
}
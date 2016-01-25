using System;
using System.Net;
using System.Windows.Threading;
using CalculatorService;
using Discovery;
using Interfaces;
using Ipc;

namespace DiscoveryServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var calculatorImp = new CalculatorImp();
            var dispatcherThread = new DispatcherThread(Dispatcher.Run);
            dispatcherThread.Start();

            var server = new IpcServer<ICalculator>(calculatorImp, dispatcherThread.Dispatcher, IPAddress.Any, 62005, new JsonDotNetSerializer());
            server.ClientConnected += OnClientConnected;
            server.ClientDisconnected += OnClientDisconnected;

            server.Listen();

            var multiCastAddress = IPAddress.Parse("239.0.0.222");
            var advertiser = new ServiceAdvertiser<ICalculator>(multiCastAddress, 2222, TimeSpan.FromMilliseconds(1000));
            advertiser.OnAdvertisement += dt => Console.WriteLine("[{0}]", dt);

            Console.ReadLine();

            server.ClientDisconnected -= OnClientDisconnected;
            server.ClientConnected -= OnClientConnected;


            advertiser.Dispose();
        }

        private static void OnClientDisconnected(EndPoint ip)
        {
            Console.WriteLine("[-] client '{0}' disconnected", ip);
        }

        private static void OnClientConnected(EndPoint ip)
        {
            Console.WriteLine("[+] client '{0}' connected", ip);
        }
    }
}
using System;
using System.Net;
using System.Windows.Threading;
using Ipc;
using Interfaces;

namespace CalculatorService
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var calculatorImp = new CalculatorImp();
            var dispatcherThread = new DispatcherThread(Dispatcher.Run);
            dispatcherThread.Start();

            var server = new IpcServer<ICalculator>(calculatorImp, dispatcherThread.Dispatcher, IPAddress.Loopback, 62001, new JsonDotNetSerializer());
            server.ClientConnected += OnClientConnected;
            server.ClientDisconnected += OnClientDisconnected;

            server.Listen();

            Console.WriteLine("Calculator service running, press any key to exit.");
            Console.ReadLine();

            server.ClientDisconnected -= OnClientDisconnected;
            server.ClientConnected -= OnClientConnected;

            server.Dispose();
            dispatcherThread.Shutdown();
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
using System;
using System.Net;
using System.Windows.Threading;
using Interfaces;
using Ipc;

namespace FileTransferServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var ipAddress = IPAddress.Loopback;

            if (args.Length == 1)
            {
                ipAddress = IPAddress.Parse(args[0]);
            }

            var fileTransferImpl = new FileTransferImp();
            var dispatcherThread = new DispatcherThread(Dispatcher.Run);
            dispatcherThread.Start();

            var server = new IpcServer<IFileTransfer>(fileTransferImpl, dispatcherThread.Dispatcher, ipAddress, 63000, new JsonDotNetSerializer());
            server.ClientConnected += OnClientConnected;
            server.ClientDisconnected += OnClientDisconnected;

            server.Listen();

            Console.WriteLine("FileTransferServer service running, press any key to exit.");
            Console.ReadLine();

            server.ClientDisconnected -= OnClientDisconnected;
            server.ClientConnected -= OnClientConnected;

            server.Dispose();
            fileTransferImpl.Dispose();
            dispatcherThread.Join();
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
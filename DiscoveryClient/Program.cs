using System;
using System.Net;
using System.Windows.Threading;
using Discovery;
using Ipc;
using Interfaces;

namespace DiscoveryClient
{
    internal class Program
    {
        private static ServiceDiscoverer<ICalculator> _serviceDiscoverer;

        private static void Main(string[] args)
        {
            var multiCastAddress = IPAddress.Parse("239.0.0.222");
            _serviceDiscoverer = new ServiceDiscoverer<ICalculator>(multiCastAddress, 2222);
            _serviceDiscoverer.OnServiceFound += ServiceDiscovererOnOnServiceFound;

            Dispatcher.Run();
        }

        private static void ServiceDiscovererOnOnServiceFound(IPAddress ipAddress)
        {
            _serviceDiscoverer.OnServiceFound -= ServiceDiscovererOnOnServiceFound;

            var client = new IpcClient<ICalculator>(ipAddress, 62005, new JsonDotNetSerializer());

            UserInputLoop(client);

            client.Dispose();
            Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
        }

        private static void UserInputLoop(IpcClient<ICalculator> client)
        {
            var line = string.Empty;

            Console.WriteLine("Enter addition, like '4+1' or 'quit'..");
            while ((line = Console.ReadLine()) != "quit")
            {
                if (line.Contains("+"))
                {
                    var tokens = line.Split('+');
                    if (tokens.Length == 2)
                    {
                        try
                        {
                            var a = int.Parse(tokens[0]);
                            var b = int.Parse(tokens[1]);

                            var res = client.Proxy.Add(a, b);

                            Console.WriteLine("The result of the addition is {0}", res);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unsupported command. Please try again..");
                    }
                }
            }
        }
    }
}
#SimpleIpc

SimpleIpc offers a simpe interprocess communication library for the .NET platform.
It is written in C# as an alternative to WCF, aiming to be simpler in use and offer reasonable performance.

Simple Ipc offers a multi-client single-server model.
----------------------------------------------------------------------------------------------------------------------

Example: 
            var calculatorImp = new CalculatorImp();
            var server = new IpcServer<ICalculator>(calculatorImp, IPAddress.Loopback, 62001);
            server.Listen();
                
            var client = new IpcClient<ICalculator>(IPAddress.Loopback, 62001, new JsonDotNetSerializer());
            var result = client.Proxy.Add(2,4); //Would return 6 :)

SimpleIpc provides a basic way to send bigger chunks of data in a more efficient way without serialization.
This is called a bulk transfer and uses a TCP stream directly for higher transfer speeds.
The user can use this by specifying an interface using a Stream as the return type.

Example:
            public interface IFileTransfer
            {
              MemoryStream TransferFile(string filePath);
            }

Furthermore, a mechanism is provided to advertise a service on a network:
----------------------------------------------------------------------------------------------------------------------

Example:
            var multiCastAddress = IPAddress.Parse("239.0.0.222");
            var advertiser = new ServiceAdvertiser<ICalculator>(multiCastAddress, 2222, TimeSpan.FromMilliseconds(1000));

            var serviceDiscoverer = new ServiceDiscoverer<ICalculator>(multiCastAddress, 2222);                                            serviceDiscoverer.OnServiceFound += ServiceDiscovererOnOnServiceFound; //e.g. connect to this service...
  
  




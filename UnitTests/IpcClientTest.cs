using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Threading;
using Ipc;
using NUnit.Framework;
using UnitTests.Stubs;
using UnitTests.Utils;

namespace UnitTests
{
    [TestFixture]
    public class IpcClientTest
    {
        private MD5 _hashAlgo;

        [TestFixtureSetUp]
        public void Setup()
        {
            _hashAlgo = MD5.Create();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            _hashAlgo.Dispose();
        }

        [Test]
        public void VerifyBasicSendReceive()
        {
            var fooStub = new FooStub();
            fooStub.ReturnValueOfBar = "Bar";

            var dispatcherThread = new DispatcherThread(Dispatcher.Run);
            dispatcherThread.Start();

            var ipcServer = new IpcServer<IFoo>(fooStub, dispatcherThread.Dispatcher, IPAddress.Loopback, 62000, new JsonDotNetSerializer());
            ipcServer.Listen();

            var ipcClient = new IpcClient<IFoo>(IPAddress.Loopback, 62000, new JsonDotNetSerializer());

            object response;

            ipcClient.Proxy.Foo();

            Assert.AreEqual(1, fooStub.NumberOfFooCalls);

            response = ipcClient.Proxy.Bar();

            Assert.AreEqual(fooStub.ReturnValueOfBar, response);
            Assert.AreEqual(1, fooStub.NumberOfBarCalls);

            ipcClient.Dispose();
            ipcServer.Dispose();

            dispatcherThread.Shutdown();
        }

        [Test]
        public void VerifyPerformance()
        {
            var fooStub = new FooStub();
            fooStub.ReturnValueOfBar = "Bar";

            var dispatcherThread = new DispatcherThread(Dispatcher.Run);
            dispatcherThread.Start();

            var ipcServer = new IpcServer<IFoo>(fooStub, dispatcherThread.Dispatcher, IPAddress.Loopback, 62000, new JsonDotNetSerializer());
            ipcServer.Listen();

            var ipcClient = new IpcClient<IFoo>(IPAddress.Loopback, 62000, new JsonDotNetSerializer());

            var numberOfCalls = 1000;
            var nanoSecs = Performance.MeasureMs(() =>
            {
                for (var i = 0; i < numberOfCalls; i++)
                {
                    ipcClient.Proxy.Foo();
                }
            });

            var fooAverageMs = nanoSecs/numberOfCalls;
            Console.WriteLine("Foo call took: {0}ms", fooAverageMs);

            Assert.AreEqual(numberOfCalls, fooStub.NumberOfFooCalls);
            Assert.LessOrEqual(TimeSpan.FromMilliseconds(fooAverageMs), TimeSpan.FromMilliseconds(10));

            nanoSecs = Performance.MeasureMs(() =>
            {
                for (var i = 0; i < numberOfCalls; i++)
                {
                    ipcClient.Proxy.Bar();
                }
            });

            var barAverageMs = nanoSecs/numberOfCalls;
            Console.WriteLine("Bar call took: {0}ms", barAverageMs);

            Assert.AreEqual(numberOfCalls, fooStub.NumberOfBarCalls);
            Assert.LessOrEqual(TimeSpan.FromMilliseconds(barAverageMs), TimeSpan.FromMilliseconds(10));

            ipcClient.Dispose();
            ipcServer.Dispose();

            dispatcherThread.Shutdown();
        }

        [Test]
        public void VerifyServerExceptionsAreForwardedToClientAndServerRemainsAlive()
        {
            var fooStub = new FooStubThrowsException();

            var dispatcherThread = new DispatcherThread(Dispatcher.Run);
            dispatcherThread.Start();

            var ipcServer = new IpcServer<IFoo>(fooStub, dispatcherThread.Dispatcher, IPAddress.Loopback, 62000, new JsonDotNetSerializer());
            ipcServer.Listen();

            var ipcClient = new IpcClient<IFoo>(IPAddress.Loopback, 62000, new JsonDotNetSerializer());

            var caughtException = false;
            try
            {
                ipcClient.Proxy.Foo();
            }
            catch (Exception)
            {
                caughtException = true;
            }

            Assert.IsTrue(caughtException);

            //See if server survives failed method call
            ipcClient.Proxy.Bar();
            Assert.IsTrue(fooStub.NumberOfBarCalls == 1);

            ipcClient.Dispose();
            ipcServer.Dispose();

            dispatcherThread.Shutdown();
        }

        [TestCase(   1)]
        [TestCase(  10)]
        public void VerifyBulkyData(int mb)
        {
            int numberKiloBytes = 1024 * 1024 * mb;
            var buffer = new byte[numberKiloBytes];
            for (int i = 0; i < numberKiloBytes; ++i)
            {
                buffer[i] = (byte)(i % 2);
            }

            var bulkDataStub = new BulkDataStub(buffer);
            var bulkDataHash = ComputeHash(bulkDataStub.GetBulkyData());

            var dispatcherThread = new DispatcherThread(Dispatcher.Run);
            dispatcherThread.Start();

            var ipcServer = new IpcServer<IBulkData>(bulkDataStub, dispatcherThread.Dispatcher, IPAddress.Loopback, 62000, new MsDataContractJsonSerializer());
            ipcServer.Listen();

            var ipcClient = new IpcClient<IBulkData>(IPAddress.Loopback, 62000, new MsDataContractJsonSerializer());

            Stream receivingStream = null;
            double ms = Performance.MeasureMs(() =>
            {
                receivingStream = ipcClient.Proxy.GetBulkyData();
            });

            var receivingHash = ComputeHash(receivingStream);
            Assert.That(receivingHash, Is.EqualTo(bulkDataHash));

            Console.WriteLine("Sending of {0}MB took {1}ms", mb, ms);

            ipcClient.Dispose();
            ipcServer.Dispose();

            bulkDataStub.Dispose();
            receivingStream.Dispose();

            dispatcherThread.Shutdown();
        }

        [TestCase(1)]
        [TestCase(10)]
        public void VerifyBulkyData_using_Json_dot_NET_serializer(int mb)
        {
            int numberKiloBytes = 1024 * 1024 * mb;
            var buffer = new byte[numberKiloBytes];
            for (int i = 0; i < numberKiloBytes; ++i)
            {
                buffer[i] = (byte)(i % 2);
            }

            var bulkDataStub = new BulkDataStub(buffer);
            var bulkDataHash = ComputeHash(bulkDataStub.GetBulkyData());

            var dispatcherThread = new DispatcherThread(Dispatcher.Run);
            dispatcherThread.Start();

            var ipcServer = new IpcServer<IBulkData>(bulkDataStub, dispatcherThread.Dispatcher, IPAddress.Loopback, 62000, new JsonDotNetSerializer());
            ipcServer.Listen();

            var ipcClient = new IpcClient<IBulkData>(IPAddress.Loopback, 62000, new JsonDotNetSerializer());

            Stream receivingStream = null;
            double ms = Performance.MeasureMs(() =>
            {
                receivingStream = ipcClient.Proxy.GetBulkyData();
            });

            var receivingHash = ComputeHash(receivingStream);
            Assert.That(receivingHash, Is.EqualTo(bulkDataHash));

            Console.WriteLine("Sending of {0}MB took {1}ms", mb, ms);

            ipcClient.Dispose();
            ipcServer.Dispose();

            bulkDataStub.Dispose();
            receivingStream.Dispose();

            dispatcherThread.Shutdown();
        }

        [TestCase()]
        public void VerifyCanPerformFunctionCallAfterBulk()
        {
            int numberKiloBytes = 1024 * 1024 * 1;
            var buffer = new byte[numberKiloBytes];
            for (int i = 0; i < numberKiloBytes; ++i)
            {
                buffer[i] = (byte)(i % 2);
            }

            var bulkDataStub = new BulkDataStub(buffer);

            var dispatcherThread = new DispatcherThread(Dispatcher.Run);
            dispatcherThread.Start();

            var ipcServer = new IpcServer<IBulkData>(bulkDataStub, dispatcherThread.Dispatcher, IPAddress.Loopback, 62000, new MsDataContractJsonSerializer());
            ipcServer.Listen();

            var ipcClient = new IpcClient<IBulkData>(IPAddress.Loopback, 62000, new MsDataContractJsonSerializer());

            ipcClient.Proxy.GetBulkyData();

            var fooResult = ipcClient.Proxy.Foo();
            Assert.AreEqual("Bar", fooResult);

            ipcClient.Dispose();
            ipcServer.Dispose();

            bulkDataStub.Dispose();

            dispatcherThread.Shutdown();
        }

        public string ComputeHash(Stream stream)
        {
            var hash = _hashAlgo.ComputeHash(stream);
            stream.Position = 0;
            return BitConverter.ToString(hash);
        }

        //http://stackoverflow.com/questions/968935/c-sharp-binary-file-compare
        //static bool StreamEquals(Stream stream1, Stream stream2)
        //{
        //    const int bufferSize = 2048 * 2;
        //    var buffer1 = new byte[bufferSize];
        //    var buffer2 = new byte[bufferSize];

        //    while (true)
        //    {
        //        int count1 = stream1.Read(buffer1, 0, bufferSize);
        //        int count2 = stream2.Read(buffer2, 0, bufferSize);

        //        if (count1 != count2)
        //        {
        //            return false;
        //        }

        //        if (count1 == 0)
        //        {
        //            return true;
        //        }

        //        int iterations = (int)Math.Ceiling((double)count1 / sizeof(Int64));
        //        for (int i = 0; i < iterations; i++)
        //        {
        //            if (BitConverter.ToInt64(buffer1, i * sizeof(Int64)) != BitConverter.ToInt64(buffer2, i * sizeof(Int64)))
        //            {
        //                return false;
        //            }
        //        }
        //    }
        //}
    }
}
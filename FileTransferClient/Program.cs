using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Interfaces;
using Ipc;

namespace FileTransferClient
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

            var client = new IpcClient<IFileTransfer>(ipAddress, 63000, new JsonDotNetSerializer());
            client.BulkProgress += OnBulkProgress;
           
            var line = string.Empty;

            Console.WriteLine(@"Enter filename 'c:\images\photo.bmp' or 'quit'..");
            while ((line = Console.ReadLine()) != "quit")
            {
                try
                {
                    var sw = new Stopwatch();
                    sw.Start();
           
                    using (var memoryStream = client.Proxy.TransferFile(line))
                    {
                        sw.Stop();
                        var newName = line + ".cpy";
                        using (var fs = File.Create(newName))
                        {
                            memoryStream.Position = 0;
                            memoryStream.CopyTo(fs);
                            memoryStream.Flush();
                            fs.Close();
                        }

                        int megaByte = memoryStream.Capacity / 1024 / 1024;
                        double megaBytePerSec = megaByte / sw.Elapsed.TotalSeconds;
                        Console.WriteLine("Successfully wrote file '{0} @ {1:.0} MB/s.", newName, megaBytePerSec);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            client.BulkProgress -= OnBulkProgress;
            client.Dispose();

            Environment.Exit(0);
        }

        private static void OnBulkProgress(double percentage)
        {
            Console.WriteLine("{000:.}%", percentage);
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }
    }
}
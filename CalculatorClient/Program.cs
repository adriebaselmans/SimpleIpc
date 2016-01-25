using System;
using System.Net;
using Ipc;
using Interfaces;

namespace CalculatorClient
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var client = new IpcClient<ICalculator>(IPAddress.Loopback, 62001, new JsonDotNetSerializer());

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
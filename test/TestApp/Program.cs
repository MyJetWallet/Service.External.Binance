using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Connector.Binance.Ws;
using ProtoBuf.Grpc.Client;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();

            var loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    }));

            var client = new BinanceWsOrderBooks(loggerFactory.CreateLogger<BinanceWsOrderBooks>(), new []{ "" }, true);
            client.BestPriceUpdateEvent += BestPriceUpdate;

            client.Start();

            Console.WriteLine("End");
            Console.ReadLine();
        }

        private static void BestPriceUpdate(DateTime arg1, string arg2, decimal arg3, decimal arg4)
        {
            Console.WriteLine($"{arg1} {arg2} {arg3} {arg4}");
        }
    }
}

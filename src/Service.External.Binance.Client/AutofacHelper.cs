using Autofac;
using Service.External.Binance.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.External.Binance.Client
{
    public static class AutofacHelper
    {
        public static void RegisterExternal.BinanceClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new External.BinanceClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetHelloService()).As<IHelloService>().SingleInstance();
        }
    }
}

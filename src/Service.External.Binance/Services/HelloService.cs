using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Service.External.Binance.Grpc;
using Service.External.Binance.Grpc.Models;
using Service.External.Binance.Settings;

namespace Service.External.Binance.Services
{
    public class HelloService: IHelloService
    {
        private readonly ILogger<HelloService> _logger;

        public HelloService(ILogger<HelloService> logger)
        {
            _logger = logger;
        }

        public Task<HelloMessage> SayHelloAsync(HelloRequest request)
        {
            _logger.LogInformation("Hello from {name}", request.Name);

            return Task.FromResult(new HelloMessage
            {
                Message = "Hello " + request.Name
            });
        }
    }
}

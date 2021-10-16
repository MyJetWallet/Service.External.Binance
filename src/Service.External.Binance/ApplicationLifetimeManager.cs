using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.TcpClient;
using SimpleTrading.FeedTcpContext.TcpServer;

namespace Service.External.Binance
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly ServiceBusLifeTime _serviceBusTcpClient;

        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime, 
            ILogger<ApplicationLifetimeManager> logger, 
            ServiceBusLifeTime serviceBusTcpClient)
            : base(appLifetime)
        {
            _logger = logger;
            _serviceBusTcpClient = serviceBusTcpClient;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            _serviceBusTcpClient.Start();
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}

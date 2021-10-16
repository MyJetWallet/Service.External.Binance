using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.TcpClient;
using Service.External.Binance.Services;
using SimpleTrading.FeedTcpContext.TcpServer;

namespace Service.External.Binance
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly ServiceBusLifeTime _serviceBusTcpClient;
        private readonly OrderBookCacheManager _orderBookCacheManager;
        private readonly MarketAndBalanceCache _marketAndBalanceCache;

        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime, 
            ILogger<ApplicationLifetimeManager> logger, 
            ServiceBusLifeTime serviceBusTcpClient, 
            OrderBookCacheManager orderBookCacheManager, 
            MarketAndBalanceCache marketAndBalanceCache)
            : base(appLifetime)
        {
            _logger = logger;
            _serviceBusTcpClient = serviceBusTcpClient;
            _orderBookCacheManager = orderBookCacheManager;
            _marketAndBalanceCache = marketAndBalanceCache;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            _serviceBusTcpClient.Start();
            _orderBookCacheManager.Start();
            _marketAndBalanceCache.Start();
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

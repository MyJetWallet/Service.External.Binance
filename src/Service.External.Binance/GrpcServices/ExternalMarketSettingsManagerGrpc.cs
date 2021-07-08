using System.Threading.Tasks;
using MyJetWallet.Sdk.ExternalMarketsSettings.Grpc;
using MyJetWallet.Sdk.ExternalMarketsSettings.Grpc.Models;
using MyJetWallet.Sdk.ExternalMarketsSettings.Models;
using MyJetWallet.Sdk.ExternalMarketsSettings.Settings;
using Service.External.Binance.Services;

namespace Service.External.Binance.GrpcServices
{
    public class ExternalMarketSettingsManagerGrpc : IExternalMarketSettingsManagerGrpc
    {
        private readonly IExternalMarketSettingsAccessor _accessor;
        private readonly IExternalMarketSettingsManager _manager;
        private readonly OrderBookCacheManager _orderBookCacheManager;

        public ExternalMarketSettingsManagerGrpc(IExternalMarketSettingsAccessor accessor,
            IExternalMarketSettingsManager manager, OrderBookCacheManager orderBookCacheManager)
        {
            _accessor = accessor;
            _manager = manager;
            _orderBookCacheManager = orderBookCacheManager;
        }

        public Task GetExternalMarketSettings(GetMarketRequest request)
        {
            return Task.FromResult(_accessor.GetExternalMarketSettings(request.Symbol));
        }

        public Task<GrpcList<ExternalMarketSettings>> GetExternalMarketSettingsList()
        {
            return Task.FromResult(GrpcList<ExternalMarketSettings>.Create(_accessor.GetExternalMarketSettingsList()));
        }

        public Task AddExternalMarketSettings(ExternalMarketSettings settings)
        {
            _manager.AddExternalMarketSettings(settings);
            return _orderBookCacheManager.Subscribe(settings.Market);
        }

        public Task UpdateExternalMarketSettings(ExternalMarketSettings settings)
        {
            _manager.UpdateExternalMarketSettings(settings);
            return _orderBookCacheManager.Resubscribe(settings.Market);
        }

        public Task RemoveExternalMarketSettings(RemoveMarketRequest request)
        {
            _manager.RemoveExternalMarketSettings(request.Symbol);
            return _orderBookCacheManager.Unsubscribe(request.Symbol);
        }
    }
}
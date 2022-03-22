using System.Linq;
using System.Threading.Tasks;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;

// ReSharper disable ClassNeverInstantiated.Global

namespace Service.External.Binance.Services
{
    public class OrderBookSourceGrpc : IOrderBookSource
    {
        private readonly OrderBookCacheManager _manager;

        public OrderBookSourceGrpc(OrderBookCacheManager manager)
        {
            _manager = manager;
        }

        public Task<GetNameResult> GetNameAsync(GetOrderBookNameRequest request)
        {
            return Task.FromResult(new GetNameResult() { Name = BinanceConst.Name });
        }

        public Task<GetSymbolResponse> GetSymbolsAsync(GetSymbolsRequest request)
        {
            var list = _manager.GetMarkets().Select(e => e.Market).ToList();
            return Task.FromResult(new GetSymbolResponse() {Symbols = list});
        }

        public Task<HasSymbolResponse> HasSymbolAsync(MarketRequest request)
        {
            var result = _manager.GetMarkets().Any(e => e.Market == request.Market);
            return Task.FromResult(new HasSymbolResponse() {Result = result});
        }

        public Task<GetOrderBookResponse> GetOrderBookAsync(MarketRequest request)
        {
            var resp = _manager.GetOrderBookAsync(request);

            return Task.FromResult(resp);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Binance;
using Binance.Cache;
using Binance.WebSocket;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;

namespace Service.External.Binance.Services
{
    public class OrderBookSourceGrpc : IOrderBookSource, IStartable, IDisposable
    {
        private readonly MarketAndBalanceCache _marketService;
        private readonly ILogger<OrderBookSourceGrpc> _logger;
        private BinanceApi _api;
        private DepthWebSocketClient _wsClient;

        private List<string> _symbols = new List<string>();
        private Dictionary<string, DepthWebSocketCache> _readers = new Dictionary<string, DepthWebSocketCache>();

        public OrderBookSourceGrpc(MarketAndBalanceCache marketService, ILogger<OrderBookSourceGrpc> logger)
        {
            _marketService = marketService;
            _logger = logger;
        }

        public Task<GetNameResult> GetNameAsync()
        {
            return Task.FromResult(new GetNameResult() { Name = BinanceConst.Name });
        }

        public Task<GetSymbolResponse> GetSymbolsAsync()
        {
            var list = _marketService.GetMarkets().Select(e => e.Market).ToList();
            return Task.FromResult(new GetSymbolResponse() {Symbols = list});
        }

        public Task<HasSymbolResponse> HasSymbolAsync(MarketRequest request)
        {
            var result = _marketService.GetMarkets().Any(e => e.Market == request.Market);
            return Task.FromResult(new HasSymbolResponse() {Result = result});
        }

        public Task<GetOrderBookResponse> GetOrderBookAsync(MarketRequest request)
        {
            if (!_readers.TryGetValue(request.Market, out var reader) || reader.OrderBook == null)
            {
                return Task.FromResult(new GetOrderBookResponse()
                {
                    OrderBook = null
                });
            }

            var data = reader.OrderBook;
 
            var resp = new GetOrderBookResponse();
            resp.OrderBook = new LeOrderBook();
            resp.OrderBook.Source = BinanceConst.Name;
            resp.OrderBook.Symbol = data.Symbol;
            resp.OrderBook.Timestamp = data.Timestamp;
            resp.OrderBook.Asks = data.Asks.Select(e => new LeOrderBookLevel((double)e.Price, (double)e.Quantity)).ToList();
            resp.OrderBook.Bids = data.Bids.Select(e => new LeOrderBookLevel((double)e.Price, (double)e.Quantity)).ToList();

            return Task.FromResult(resp);
        }

        public void Start()
        {
            _symbols = Program.Settings.Instruments.Split(';').ToList();

            _api = new BinanceApi();

            foreach (var symbol in _symbols)
            {
                var reader = new DepthWebSocketCache(_api, _wsClient);
                reader.Error += (s, e) => { _logger.LogError(e.Exception, "[WS][{symbol}] {message}", symbol, e.Exception.Message); };
                reader.Subscribe(symbol);

                _readers[symbol] = reader;
            }
        }

        public void Dispose()
        {
            foreach (var reader in _readers.Values)
            {
                reader.Unsubscribe();
            }
        }
    }
}
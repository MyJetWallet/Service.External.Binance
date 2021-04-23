using System;
using System.Linq;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Connector.Binance.Ws;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;

namespace Service.External.Binance.Services
{
    public class OrderBookCacheManager: IStartable, IDisposable
    {
        private readonly ILogger<OrderBookCacheManager> _logger;

        private BinanceWsOrderBooks _client;

        private string[] _symbols = { };
        
        public OrderBookCacheManager(ILogger<OrderBookCacheManager> logger)
        {
            _logger = logger;
        }

        public void Start()
        {
            _symbols = Program.Settings.Instruments.Split(';').ToArray();

            _client = new BinanceWsOrderBooks(_logger, _symbols, true);

            _client.Start();
        }

        public void Dispose()
        {
            _client?.Stop();
            _client?.Dispose();
        }

        public GetOrderBookResponse GetOrderBookAsync(MarketRequest request)
        {
            var data = _client.GetOrderBook(request.Market);

            if (data == null)
            {
                return new GetOrderBookResponse()
                {
                    OrderBook = null
                };
            }

            var resp = new GetOrderBookResponse();
            resp.OrderBook = new LeOrderBook();
            resp.OrderBook.Source = BinanceConst.Name;
            resp.OrderBook.Symbol = data.Symbol;
            resp.OrderBook.Timestamp = data.Time;
            resp.OrderBook.Asks = data.Asks.OrderBy(e => e.Key).Select(e => new LeOrderBookLevel((double)e.Key, (double)e.Value)).ToList();
            resp.OrderBook.Bids = data.Bids.OrderByDescending(e => e.Key).Select(e => new LeOrderBookLevel((double)e.Key, (double)e.Value)).ToList();

            return resp;
        }
    }
}
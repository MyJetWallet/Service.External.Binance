using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Connector.Binance.Ws;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Prices;
using MyJetWallet.Sdk.ExternalMarketsSettings.Settings;
using SimpleTrading.FeedTcpContext.TcpServer;

namespace Service.External.Binance.Services
{
    public class OrderBookCacheManager: IStartable, IDisposable
    {
        private readonly ILogger<OrderBookCacheManager> _logger;
        private readonly TextTcpServer _bidAskConsumer;
        private readonly IExternalMarketSettingsAccessor _externalMarketSettingsAccessor;
        private readonly IPublisher<BidAsk> _publisher;

        private BinanceWsOrderBooks _client;

        private string[] _symbols = Array.Empty<string>();
        
        public OrderBookCacheManager(ILogger<OrderBookCacheManager> logger, IExternalMarketSettingsAccessor externalMarketSettingsAccessor, IPublisher<BidAsk> publisher)
        {
            if (!string.IsNullOrEmpty(Program.Settings.StInstrumentsMapping))
            {
                _bidAskConsumer = new TextTcpServer(new TcpServerSettings()
                {
                    InstrumentsMapping = Program.Settings.StInstrumentsMapping,
                    ServerPort = Program.StTextQuoteListenerPort
                });
            }

            
            _logger = logger;
            _externalMarketSettingsAccessor = externalMarketSettingsAccessor;
            _publisher = publisher;
        }

        public void Start()
        {
            _bidAskConsumer?.Start();

            _symbols = _externalMarketSettingsAccessor.GetExternalMarketSettingsList().Select(e => e.Market).ToArray();

            _client = new BinanceWsOrderBooks(_logger, _symbols, true);
            _client.BestPriceUpdateEvent += BestPriceUpdate;


            _client.Start();
        }

        private void BestPriceUpdate(DateTime timestamp, string symbol, decimal bid, decimal ask)
        {
            _bidAskConsumer?.ConsumeBidAsk(symbol, (double)bid, (double)ask, timestamp);

            try
            {
                _publisher.PublishAsync(new BidAsk()
                {
                    Id = symbol,
                    Ask = (double) ask,
                    Bid = (double) bid,
                    DateTime = timestamp,
                    LiquidityProvider = "Binance"
                });
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            _bidAskConsumer?.Stop();
            _client?.Stop();
            _client?.Dispose();
        }

        public async Task Resubscribe(string symbol)
        {
            await _client.Reset(symbol);
        }

        public async Task Subscribe(string symbol)
        {
            await _client.Subscribe(symbol);
        }

        public async Task Unsubscribe(string symbol)
        {
            await _client.Unsubscribe(symbol);
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

            var resp = new GetOrderBookResponse
            {
                OrderBook = new LeOrderBook
                {
                    Source = BinanceConst.Name,
                    Symbol = data.Symbol,
                    Timestamp = data.Time,
                    Asks = data.Asks.OrderBy(e => e.Key)
                        .Select(e => new LeOrderBookLevel((double) e.Key, (double) e.Value))
                        .ToList(),
                    Bids = data.Bids.OrderByDescending(e => e.Key)
                        .Select(e => new LeOrderBookLevel((double) e.Key, (double) e.Value)).ToList()
                }
            };

            return resp;
        }
    }
}
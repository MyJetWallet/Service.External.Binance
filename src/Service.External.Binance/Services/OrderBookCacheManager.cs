using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using MyJetWallet.Connector.Binance.Ws;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Prices;
using MyJetWallet.Sdk.ExternalMarketsSettings.Settings;
using MyJetWallet.Sdk.Service.Tools;
using SimpleTrading.FeedTcpContext.TcpServer;

namespace Service.External.Binance.Services
{
    public class OrderBookCacheManager: IStartable, IDisposable
    {
        private readonly ILogger<OrderBookCacheManager> _logger;
        private readonly TextTcpServer _bidAskConsumer;
        private readonly IExternalMarketSettingsAccessor _externalMarketSettingsAccessor;
        private readonly IPublisher<BidAsk> _publisher;

        private Dictionary<string, BidAsk> _updates = new Dictionary<string, BidAsk>();

        private MyTaskTimer _timer;

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
            _timer = new MyTaskTimer(nameof(OrderBookCacheManager), TimeSpan.FromSeconds(1), logger, DoTime).DisableTelemetry();
        }

        private async Task DoTime()
        {
            List<BidAsk> updates;


            lock (_updates)
            {
                updates = _updates.Values.ToList();
                _updates.Clear();
            }

            var taskList = new List<Task>();
            foreach (var bidAsk in updates)
            {
                taskList.Add(_publisher.PublishAsync(bidAsk).AsTask());
            }

            try
            {
                await Task.WhenAll(taskList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot publish price updates");
            }
        }


        public void Start()
        {
            _bidAskConsumer?.Start();

            _symbols = _externalMarketSettingsAccessor.GetExternalMarketSettingsList().Select(e => e.Market).ToArray();

            _client = new BinanceWsOrderBooks(_logger, _symbols, true);
            _client.BestPriceUpdateEvent += BestPriceUpdate;


            _client.Start();

            _timer.Start();
        }

        private void BestPriceUpdate(DateTime timestamp, string symbol, decimal bid, decimal ask)
        {
            try
            {
                _bidAskConsumer?.ConsumeBidAsk(symbol, (double) bid, (double) ask, timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CAnnot publish BidAsk to TextTcpServer");
            }

            lock (_updates)
            {
                _updates[symbol] = new BidAsk()
                {
                    Id = symbol,
                    Ask = (double) ask,
                    Bid = (double) bid,
                    DateTime = timestamp,
                    LiquidityProvider = BinanceConst.Name
                };
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
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
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
using MyJetWallet.Sdk.ServiceBus;
using SimpleTrading.FeedTcpContext.TcpServer;

namespace Service.External.Binance.Services
{
    public class OrderBookCacheManager: IDisposable
    {
        private readonly ILogger<OrderBookCacheManager> _logger;
        private readonly TextTcpServer _bidAskConsumer;
        private readonly IExternalMarketSettingsAccessor _externalMarketSettingsAccessor;
        private readonly IServiceBusPublisher<BidAsk> _publisher;

        private Dictionary<string, BidAsk> _updates = new Dictionary<string, BidAsk>();

        private Dictionary<string, BinanceWsOrderBooks> _clientBySymbols = new Dictionary<string, BinanceWsOrderBooks>();
        private List<BinanceWsOrderBooks> _clients = new List<BinanceWsOrderBooks>();

        private MyTaskTimer _timer;

        private string[] _symbols = Array.Empty<string>();
        
        public OrderBookCacheManager(ILogger<OrderBookCacheManager> logger, 
            IExternalMarketSettingsAccessor externalMarketSettingsAccessor, 
            IServiceBusPublisher<BidAsk> publisher)
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
            _timer = new MyTaskTimer(nameof(OrderBookCacheManager), TimeSpan.FromMilliseconds(500), logger, DoTime).DisableTelemetry();
        }

        private async Task DoTime()
        {
            List<BidAsk> updates;


            lock (_updates)
            {
                updates = _updates.Values.ToList();
                _updates.Clear();
            }

            try
            {
                if (updates.Any())
                    await _publisher.PublishAsync(updates);
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

            var size = _symbols.Length / 10;
            foreach (var chunk in _symbols.SplitToChunks(size))
            {
                var client = new BinanceWsOrderBooks(_logger, chunk.ToArray(), true);
                _clients.Add(client);
                client.BestPriceUpdateEvent += BestPriceUpdate;
                client.Start();
                
                
                foreach (var symbol in chunk)
                {
                    _clientBySymbols[symbol] = client;
                }
            }
            
            _logger.LogInformation($"Start {_clients.Count} client to binance, by {size} instruments");
            
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
                _logger.LogError(ex, "Cannot publish BidAsk to TextTcpServer");
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

            foreach (var client in _clients)
            {
                client?.Stop();
                client?.Dispose();
            }
            _clients.Clear();
        }

        public async Task Resubscribe(string symbol)
        {
            if (_clientBySymbols.TryGetValue(symbol, out var client))
            {
                await client.Reset(symbol);
            }
        }

        public async Task Subscribe(string symbol)
        {
            if (_clientBySymbols.ContainsKey(symbol))
                return;
            
            var client = _clients.Last();
            await client.Subscribe(symbol);
            _clientBySymbols[symbol] = client;
        }

        public async Task Unsubscribe(string symbol)
        {
            if (_clientBySymbols.TryGetValue(symbol, out var client))
            {
                await client.Unsubscribe(symbol);
                _clientBySymbols.Remove(symbol);
            }
        }

        public GetOrderBookResponse GetOrderBookAsync(MarketRequest request)
        {
            BinanceOrderBookCache data = null;
            if (_clientBySymbols.TryGetValue(request.Market, out var client))
            {
                data = client.GetOrderBook(request.Market);
            } 
            
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
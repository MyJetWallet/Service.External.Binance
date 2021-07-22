using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Binance;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Sdk.ExternalMarketsSettings.Settings;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.Service.Tools;

namespace Service.External.Binance.Services
{
    public class MarketAndBalanceCache: IStartable, IDisposable
    {
        private readonly BinanceApi _client;
        private readonly IBinanceApiUser _user;
        private readonly ILogger<MarketAndBalanceCache> _logger;
        private readonly IExternalMarketSettingsAccessor _externalMarketSettingsAccessor;

        private Dictionary<string, ExchangeBalance> _balances = new Dictionary<string, ExchangeBalance>();
        

        private readonly MyTaskTimer _timer;

        public MarketAndBalanceCache(ILogger<MarketAndBalanceCache> logger, IExternalMarketSettingsAccessor externalMarketSettingsAccessor)
        {
            _logger = logger;
            _externalMarketSettingsAccessor = externalMarketSettingsAccessor;

            _client = new BinanceApi();
            _user = new BinanceApiUser(Program.Settings.BinanceApiKey, Program.Settings.BinanceApiSecret);

            _timer = new MyTaskTimer(nameof(MarketAndBalanceCache), TimeSpan.FromSeconds(1), logger, DoTimer);
        }

        private async Task DoTimer()
        {
            _timer.ChangeInterval(TimeSpan.FromSeconds(Program.Settings.RefreshBalanceIntervalSec));

            using var activity = MyTelemetry.StartActivity("Refresh balance data");
            try
            {
                await RefreshData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on refresh balance");
                ex.FailActivity();
            }
        }

        public async Task RefreshData()
        {
            _logger.LogInformation("Balance and market update");

            var balances = await GetMarginAccountBalances();

            var dict = new Dictionary<string, ExchangeBalance>();

            foreach (var balance in balances)
            {
                using var activityBalance = MyTelemetry.StartActivity($"Update balance {balance.Asset}")?.AddTag("asset", balance.Asset);
                
                try
                {
                    var free = await _client.GetMaxBorrowAsync(_user, balance.Asset);

                    var item = new ExchangeBalance()
                    {
                        Symbol = balance.Asset,
                        Balance = balance.Free,
                        Free = (decimal) free
                    };

                    dict[item.Symbol] = item;
                }
                catch (Exception ex)
                {
                    ex.FailActivity();
                    _logger.LogError(ex, "Canoot update borrow balance");
                }
            }

            _balances = dict;
        }

        private async Task<List<MarginAccountBalance>> GetMarginAccountBalances()
        {
            try
            {
                var markets = GetMarkets();
                var baseAssets = markets.Select(e => e.BaseAsset);
                var quoteAssets = markets.Select(e => e.QuoteAsset);
                var balances = await _client.GetMarginBalancesAsync(_user);
                balances = balances.Where(e => baseAssets.Contains(e.Asset) || quoteAssets.Contains(e.Asset)).ToList();
                return balances;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot get account balance");
                throw;
            }
        }
        
        public void Start()
        {
            _timer.Start();
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }

        public List<ExchangeMarketInfo> GetMarkets()
        {
            try
            {
                var data = _externalMarketSettingsAccessor.GetExternalMarketSettingsList();
                return data.Select(e => new ExchangeMarketInfo()
                {
                    Market = e.Market,
                    BaseAsset = e.BaseAsset,
                    QuoteAsset = e.QuoteAsset,
                    MinVolume = e.MinVolume,
                    PriceAccuracy = e.PriceAccuracy,
                    VolumeAccuracy = e.VolumeAccuracy,
                    AssociateInstrument = e.AssociateInstrument,
                    AssociateBaseAsset = e.AssociateBaseAsset,
                    AssociateQuoteAsset = e.AssociateQuoteAsset
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot get Binance GetMarketInfo");
                throw;
            }
        }

        public List<ExchangeBalance> GetBalances()
        {
            return _balances.Values.ToList();
        }
    }
}
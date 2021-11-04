using System.Collections.Generic;
using System.Net;
using MyJetWallet.Sdk.Service;
using MyYamlParser;
using SimpleTrading.FeedTcpContext.TcpServer;

namespace Service.External.Binance.Settings
{
    //[YamlAttributesOnly]
    public class SettingsModel
    {
        [YamlProperty("ExternalBinance.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }
        
        [YamlProperty("ExternalBinance.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }

        [YamlProperty("ExternalBinance.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("ExternalBinance.RefreshBalanceIntervalSec")]
        public int RefreshBalanceIntervalSec { get; set; }

        [YamlProperty("ExternalBinance.ApiKey")]
        public string BinanceApiKey { get; set; }

        [YamlProperty("ExternalBinance.ApiSecret")]
        public string BinanceApiSecret { get; set; }

        [YamlProperty("ExternalBinance.StInstrumentsMapping")]
        public string StInstrumentsMapping { get; set; }
        
        [YamlProperty("ExternalBinance.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }

        [YamlProperty("ExternalBinance.ServiceBusHostPort")]
        public string ServiceBusHostPort { get; set; }
    }
}

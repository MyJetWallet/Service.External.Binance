using SimpleTrading.SettingsReader;

namespace Service.External.Binance.Settings
{
    [YamlAttributesOnly]
    public class SettingsModel
    {
        [YamlProperty("ExternalBinance.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("ExternalBinance.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("ExternalBinance.Instruments")]
        public string Instruments { get; set; }

        [YamlProperty("ExternalBinance.RefreshBalanceIntervalSec")]
        public int RefreshBalanceIntervalSec { get; set; }

        [YamlProperty("ExternalBinance.BinanceApiKey")]
        public string BinanceApiKey { get; set; }

        [YamlProperty("ExternalBinance.BinanceApiSecret")]
        public string BinanceApiSecret { get; set; }
    }
}

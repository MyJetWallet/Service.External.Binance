using MyYamlParser;

namespace Service.External.Binance.Settings
{
    //[YamlAttributesOnly]
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

        [YamlProperty("ExternalBinance.ApiKey")]
        public string BinanceApiKey { get; set; }

        [YamlProperty("ExternalBinance.ApiSecret")]
        public string BinanceApiSecret { get; set; }
    }
}

using SimpleTrading.SettingsReader;

namespace Service.External.Binance.Settings
{
    [YamlAttributesOnly]
    public class SettingsModel
    {
        [YamlProperty("External.Binance.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("External.Binance.ZipkinUrl")]
        public string ZipkinUrl { get; set; }
    }
}

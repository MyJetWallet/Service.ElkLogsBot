using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.ElkLogsBot.Settings
{
    public class SettingsModel
    {
        [YamlProperty("ElkLogsBot.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("ElkLogsBot.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("ElkLogsBot.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }
        
        [YamlProperty("ElkLogsBot.TelegramApiKey")]
        public string TelegramApiKey { get; set; }
        
        [YamlProperty("ElkLogsBot.TelegramChatId")]
        public string TelegramChatId { get; set; }
    }
}

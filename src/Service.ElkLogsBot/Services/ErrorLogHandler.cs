using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using Nest;
using Newtonsoft.Json;
using Serilog;
using Telegram.Bot;

namespace Service.ElkLogsBot.Services
{
    public class ErrorLogHandler: IStartable
    {
        private MyTaskTimer _timer;
        private ElasticClient _client;
        private DateTime _lastTs = DateTime.UtcNow;
        private ITelegramBotClient _botApiClient;

        public ErrorLogHandler(ILogger<ErrorLogHandler> logger)
        {
            _timer = new MyTaskTimer(nameof(ErrorLogHandler), TimeSpan.FromSeconds(10), logger, DoTime).DisableTelemetry();
        }

        private async Task DoTime()
        {
            Console.WriteLine("Try get data...");
            
            var idexes = Indices.Index( $"{Program.Settings.ElkLogs.IndexPrefix}-{DateTime.UtcNow:yyyy-MM-dd}");

            var result = _client.Search<Item>(s => s
                .Index(idexes)
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.Level)
                        .Query("Error")
                    )
                )
                .Sort(e => e.Descending(i => i.Timestamp))
                .Size(100)
            );

            var items = result.Documents.Where(e => e.Timestamp > _lastTs).ToList();
            Console.WriteLine($"Receive {items.Count} items");
            
            if (!items.Any())
                return;
            
            _lastTs = items.Max(e => e.Timestamp);
            
            foreach (var app in items.GroupBy(e => e.Fields.AppName))
            {
                var sb = new StringBuilder();
                sb.AppendLine("====================");
                sb.AppendLine($"| {app.Key} | count records: {app.Count()}");
                var index = 0;
                foreach (var item in app.OrderByDescending(e => e.Timestamp))
                {
                    sb.AppendLine($"{item.Level}; {item.Timestamp:yyyy-MM-dd HH:mm:ss}; {item.Fields.Host}");
                    sb.AppendLine(item.message);
                    sb.AppendLine();
                    
                    index++;
                    if (index >=5)
                        break;
                }

                if (app.Count() > index)
                    sb.AppendLine("...");

                sb.AppendLine();

                await Send(sb.ToString());
            }


            
        }

        public class Item
        {
            [Text(Name = "level")]
            public string Level { get; set; }
            
            [Text(Name = "message")]
            public string message { get; set; }
            
            [Text(Name = "fields")]
            public Fields Fields { get; set; }
            
            [Text(Name = "@timestamp")]
            public DateTime Timestamp { get; set; }
        }

        public class Fields
        {
            [Text(Name = "app-name")]
            public string AppName { get; set; }
            
            [Text(Name = "host-name")]
            public string Host { get; set; }
        }

        public void Start()
        {
            
            var uris = Program.Settings.ElkLogs.Urls.Values.Select(e => new Uri(e)).ToArray();
            
            var settings = new ConnectionSettings(uris[0])
                //.DefaultIndex("jet-logs-test-2021-11-02")
                .BasicAuthentication(Program.Settings.ElkLogs.User, Program.Settings.ElkLogs.Password)
                .ServerCertificateValidationCallback(CertificateValidations.AllowAll);

             _client = new ElasticClient(settings);
            
            var idexes = Indices.Index( $"{Program.Settings.ElkLogs.IndexPrefix}-{DateTime.UtcNow:yyyy-MM-dd}");

            Console.WriteLine("Try Get test data ...");
            var result = _client.Search<Item>(s => s
                .Index(idexes)
                .Size(1)
            );

            if (result.Documents.Count != 1)
            {
                throw new Exception($"Cannot get data from index: {result.DebugInformation}");
            }
            Console.WriteLine("Get test data success.");
            
            _timer.Start();
            
            _botApiClient = new TelegramBotClient(Program.Settings.TelegramApiKey);
            Send("--Bot is started--").GetAwaiter().GetResult();
        }

        private async Task Send(string message)
        {
            Console.WriteLine(message);

            if (message.Length > 2000)
                message = message.Substring(0, 2000);
            
            await _botApiClient.SendTextMessageAsync(Program.Settings.TelegramChatId, message);
        }
        
    }
}
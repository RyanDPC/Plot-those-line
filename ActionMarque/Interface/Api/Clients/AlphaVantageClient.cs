using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ActionMarque.Interface.Api.Clients
{
    public class AlphaVantageClient
    {
        private readonly ActionMarque.OpenAPIs.AlphaVantage.Client _client;

        public AlphaVantageClient(HttpClient http)
        {
            _client = new ActionMarque.OpenAPIs.AlphaVantage.Client(http);
        }

        public async Task<IDictionary<DateTime, double>> GetMonthlyCloseAsync(string symbol, string apiKey)
        {
            var obj = await _client.QueryAsync("TIME_SERIES_MONTHLY", symbol, apiKey);
            var json = obj as JObject ?? JObject.Parse(obj?.ToString() ?? "{}");

            Console.WriteLine(json.ToString()); // <-- Ajouter pour debug

            var monthly = json["Monthly Time Series"] as JObject;
            if (monthly == null) return new Dictionary<DateTime, double>();

            return monthly.Properties()
                          .Select(p => new KeyValuePair<DateTime, double>(
                              DateTime.Parse(p.Name),
                              (double?)p.Value["4. close"] ?? 0d))
                          .OrderBy(kv => kv.Key)
                          .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}




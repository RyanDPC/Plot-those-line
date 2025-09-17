using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace ActionMarque
{
    public class TwelveDataService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.twelvedata.com";

        public TwelveDataService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public async Task<List<StockDataPoint>> GetStockDataAsync(string symbol, int years = 5)
        {
            try
            {
                // Calculer la date de début pour récupérer les données sur la période demandée
                var endDate = DateTime.Now;
                var startDate = endDate.AddYears(-years);
                var startDateStr = startDate.ToString("yyyy-MM-dd");
                var endDateStr = endDate.ToString("yyyy-MM-dd");

                // Construire l'URL pour l'API Twelve Data time_series
                var url = $"{BaseUrl}/time_series?symbol={symbol}&interval=1day&start_date={startDateStr}&end_date={endDateStr}&apikey={_apiKey}&format=JSON";
                
                System.Diagnostics.Debug.WriteLine($"TwelveData URL: {url}");
                
                var response = await _httpClient.GetStringAsync(url);
                
                // Debug: afficher la réponse complète pour diagnostic
                System.Diagnostics.Debug.WriteLine($"TwelveData Full Response for {symbol}: {response}");
                
                var serializer = new JavaScriptSerializer();
                var jsonData = serializer.DeserializeObject(response) as Dictionary<string, object>;

                // Vérifier s'il y a une erreur dans la réponse
                if (jsonData != null && jsonData.ContainsKey("status") && jsonData["status"].ToString() == "error")
                {
                    System.Diagnostics.Debug.WriteLine($"TwelveData API Error: {(jsonData.ContainsKey("message") ? jsonData["message"] : "Unknown error")}");
                    return new List<StockDataPoint>();
                }

                if (jsonData == null || !jsonData.ContainsKey("values"))
                {
                    System.Diagnostics.Debug.WriteLine($"No data found for symbol {symbol}");
                    return new List<StockDataPoint>();
                }

                // Vérifier les clés disponibles
                var availableKeys = string.Join(", ", jsonData.Keys);
                System.Diagnostics.Debug.WriteLine($"Available keys: {availableKeys}");

                var values = jsonData["values"] as object[];
                if (values == null)
                {
                    System.Diagnostics.Debug.WriteLine("Values array is null");
                    return new List<StockDataPoint>();
                }

                var dataPoints = new List<StockDataPoint>();

                foreach (var item in values)
                {
                    try
                    {
                        var dailyData = item as Dictionary<string, object>;
                        if (dailyData != null && dailyData.ContainsKey("datetime") && dailyData.ContainsKey("close"))
                        {
                            var dateStr = dailyData["datetime"].ToString();
                            var closePriceStr = dailyData["close"].ToString();
                            
                            if (DateTime.TryParse(dateStr, out DateTime date) && 
                                double.TryParse(closePriceStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double closePrice))
                            {
                                if (date >= startDate && date <= endDate)
                                {
                                    dataPoints.Add(new StockDataPoint
                                    {
                                        Date = date,
                                        Price = closePrice
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur parsing item: {ex.Message}");
                    }
                }

                // Trier par date
                dataPoints.Sort((x, y) => x.Date.CompareTo(y.Date));
                
                // Prendre le prix de clôture le plus récent de chaque année
                var yearlyData = new List<StockDataPoint>();
                for (int i = 0; i < years; i++)
                {
                    var targetYear = endDate.Year - years + i + 1;
                    var yearData = dataPoints.FindAll(x => x.Date.Year == targetYear);
                    if (yearData.Count > 0)
                    {
                        // Prendre le point de données le plus récent de l'année
                        var latestDataPoint = yearData.OrderByDescending(x => x.Date).First();
                        yearlyData.Add(new StockDataPoint
                        {
                            Date = new DateTime(targetYear, 12, 31), // Date de fin d'année
                            Price = latestDataPoint.Price,
                            Year = targetYear // Ajouter l'année pour compatibilité
                        });
                    }
                }

                return yearlyData;
            }
            catch (Exception ex)
            {
                // Retourner vide au lieu d'exception pour ne pas interrompre l'UI
                System.Diagnostics.Debug.WriteLine($"GetStockDataAsync error for {symbol}: {ex.Message}");
                return new List<StockDataPoint>();
            }
        }

        public async Task<List<BrandData>> GetMultipleStocksDataAsync(string[] symbols)
        {
            var brands = new List<BrandData>();
            
            foreach (var symbol in symbols)
            {
                try
                {
                    var data = await GetStockDataAsync(symbol);
                    if (data.Count > 0)
                    {
                        brands.Add(new BrandData
                        {
                            Symbol = symbol,
                            Name = symbol,
                            DataPoints = data
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur pour {symbol}: {ex.Message}");
                }
            }

            // Plus de données de démonstration - utiliser uniquement les vraies données de Twelve Data

            return brands;
        }
    }

    public class StockDataPoint
    {
        public DateTime Date { get; set; }
        public double Price { get; set; }
        public int Year { get; set; }
    }

    public class BrandData
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public List<StockDataPoint> DataPoints { get; set; } = new List<StockDataPoint>();
    }
}

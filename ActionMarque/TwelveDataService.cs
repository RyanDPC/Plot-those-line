using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace ActionMarque
{
    public enum DataGranularity
    {
        Intraday,   // Données par heure/minute
        Daily,      // Données journalières
        Weekly,     // Données hebdomadaires
        Monthly,    // Données mensuelles
        Yearly      // Données annuelles (moyennes)
    }

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
            return await GetStockDataAsync(symbol, years, DataGranularity.Yearly);
        }

        public async Task<List<StockDataPoint>> GetStockDataAsync(string symbol, int years, DataGranularity granularity)
        {
            try
            {
                // Dates de référence spécifiques selon les règles
                var startDate = new DateTime(2020, 9, 24); // 24/09/2020 pour le début
                var endDate = new DateTime(2025, 9, 23);   // 23/09/2025 pour la fin
                string interval;
                
                switch (granularity)
                {
                    case DataGranularity.Intraday:
                        interval = "1h"; // Données horaires
                        break;
                    case DataGranularity.Daily:
                        interval = "1day";
                        break;
                    case DataGranularity.Weekly:
                        interval = "1week";
                        break;
                    case DataGranularity.Monthly:
                        interval = "1day"; // On récupère du daily pour créer les tranches mensuelles
                        break;
                    case DataGranularity.Yearly:
                    default:
                        interval = "1day"; // On récupère du daily et on fait la moyenne annuelle
                        break;
                }

                var startDateStr = startDate.ToString("yyyy-MM-dd");
                var endDateStr = endDate.ToString("yyyy-MM-dd");

                // Construire l'URL pour l'API Twelve Data time_series
                var url = $"{BaseUrl}/time_series?symbol={symbol}&interval={interval}&start_date={startDateStr}&end_date={endDateStr}&apikey={_apiKey}&format=JSON";
                
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
                
                // DEBUG: Afficher les données avant extension
                System.Diagnostics.Debug.WriteLine($"=== DEBUG API {symbol} - Données avant extension ===");
                if (dataPoints.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Nombre de points: {dataPoints.Count}");
                    System.Diagnostics.Debug.WriteLine($"Première date: {dataPoints.First().Date:dd/MM/yyyy}");
                    System.Diagnostics.Debug.WriteLine($"Dernière date: {dataPoints.Last().Date:dd/MM/yyyy}");
                }
                
                // S'assurer que les données vont jusqu'à la fin de la période (23/09/2025)
                if (dataPoints.Any())
                {
                    var lastPoint = dataPoints.Last();
                    var finalDate = new DateTime(2025, 9, 23);
                    
                    System.Diagnostics.Debug.WriteLine($"Vérification extension: dernière date = {lastPoint.Date:dd/MM/yyyy}, date cible = {finalDate:dd/MM/yyyy}");
                    
                    // Si la dernière date est avant la fin de la période, étendre les données
                    if (lastPoint.Date < finalDate)
                    {
                        dataPoints.Add(new StockDataPoint
                        {
                            Date = finalDate,
                            Price = lastPoint.Price,
                            Year = finalDate.Year
                        });
                        System.Diagnostics.Debug.WriteLine($"✅ API EXTENSION: Point ajouté à {finalDate:dd/MM/yyyy} avec prix {lastPoint.Price:F2}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"ℹ️ API: Pas d'extension nécessaire - données vont déjà jusqu'à {lastPoint.Date:dd/MM/yyyy}");
                    }
                }
                
                // DEBUG: Afficher les données après extension
                System.Diagnostics.Debug.WriteLine($"=== DEBUG API {symbol} - Données après extension ===");
                if (dataPoints.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Nombre de points: {dataPoints.Count}");
                    System.Diagnostics.Debug.WriteLine($"Première date: {dataPoints.First().Date:dd/MM/yyyy}");
                    System.Diagnostics.Debug.WriteLine($"Dernière date: {dataPoints.Last().Date:dd/MM/yyyy}");
                }
                System.Diagnostics.Debug.WriteLine($"=== FIN DEBUG API {symbol} ===\n");
                
                // Traitement selon la granularité demandée
                switch (granularity)
                {
                    case DataGranularity.Intraday:
                    case DataGranularity.Daily:
                    case DataGranularity.Weekly:
                        // Pour ces granularités, retourner directement les données
                        foreach (var point in dataPoints)
                        {
                            point.Year = point.Date.Year; // Ajouter l'année pour compatibilité
                        }
                        return dataPoints;
                        
                    case DataGranularity.Monthly:
                        // Créer les tranches mensuelles selon les règles spécifiées
                        return CreateMonthlySlices(dataPoints, startDate, endDate);
                        
                    case DataGranularity.Yearly:
                    default:
                        // Points de référence annuels spécifiques
                        return CreateYearlyReferencePoints(dataPoints, startDate, endDate);
                }
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

        /// <summary>
        /// Détermine automatiquement la granularité optimale selon la période de temps
        /// </summary>
        public static DataGranularity DetermineOptimalGranularity(DateTime startDate, DateTime endDate)
        {
            var timeSpan = endDate - startDate;
            
            if (timeSpan.TotalDays <= 7)
            {
                return DataGranularity.Intraday; // Moins d'une semaine → données horaires
            }
            else if (timeSpan.TotalDays <= 180) // 6 mois
            {
                return DataGranularity.Daily; // Jusqu'à 6 mois → données journalières
            }
            else if (timeSpan.TotalDays <= 730) // 2 ans
            {
                return DataGranularity.Weekly; // Jusqu'à 2 ans → données hebdomadaires
            }
            else if (timeSpan.TotalDays <= 1825) // 5 ans
            {
                return DataGranularity.Monthly; // Jusqu'à 5 ans → données mensuelles
            }
            else
            {
                return DataGranularity.Yearly; // Plus de 5 ans → données annuelles
            }
        }

        /// <summary>
        /// Récupère les données avec granularité automatique selon la période
        /// </summary>
        public async Task<List<StockDataPoint>> GetAdaptiveStockDataAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            var granularity = DetermineOptimalGranularity(startDate, endDate);
            var years = Math.Max(1, (int)Math.Ceiling((endDate - startDate).TotalDays / 365.25));
            
            return await GetStockDataAsync(symbol, years, granularity);
        }

        /// <summary>
        /// Crée les tranches mensuelles selon les règles spécifiées
        /// </summary>
        private List<StockDataPoint> CreateMonthlySlices(List<StockDataPoint> dataPoints, DateTime startDate, DateTime endDate)
        {
            var monthlySlices = new List<StockDataPoint>();
            var currentDate = startDate;

            // Trouver la dernière valeur connue pour l'extension
            var lastKnownPrice = dataPoints.Any() ? dataPoints.OrderByDescending(dp => dp.Date).First().Price : 0.0;

            while (currentDate <= endDate)
            {
                // Déterminer la fin du mois
                var monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                if (currentDate.Year == 2025 && currentDate.Month == 9)
                {
                    monthEnd = new DateTime(2025, 9, 23); // Terminer à 23/09/2025
                }

                // Créer les 4 tranches du mois
                var sliceStart = currentDate;
                for (int slice = 0; slice < 4; slice++)
                {
                    DateTime sliceEnd;
                    switch (slice)
                    {
                        case 0: // 01 → 07
                            sliceEnd = new DateTime(currentDate.Year, currentDate.Month, Math.Min(7, monthEnd.Day));
                            break;
                        case 1: // 08 → 14
                            sliceStart = new DateTime(currentDate.Year, currentDate.Month, 8);
                            sliceEnd = new DateTime(currentDate.Year, currentDate.Month, Math.Min(14, monthEnd.Day));
                            break;
                        case 2: // 15 → 21
                            sliceStart = new DateTime(currentDate.Year, currentDate.Month, 15);
                            sliceEnd = new DateTime(currentDate.Year, currentDate.Month, Math.Min(21, monthEnd.Day));
                            break;
                        case 3: // 22 → fin du mois
                            sliceStart = new DateTime(currentDate.Year, currentDate.Month, 22);
                            sliceEnd = monthEnd;
                            break;
                        default:
                            sliceEnd = sliceStart;
                            break;
                    }

                    // Trouver les données dans cette tranche
                    var sliceData = dataPoints.Where(dp => dp.Date >= sliceStart && dp.Date <= sliceEnd).ToList();
                    if (sliceData.Count > 0)
                    {
                        // Prendre la moyenne des prix dans cette tranche
                        var averagePrice = sliceData.Average(dp => dp.Price);
                        monthlySlices.Add(new StockDataPoint
                        {
                            Date = sliceEnd, // Utiliser la fin de la tranche comme date de référence
                            Price = averagePrice,
                            Year = sliceEnd.Year
                        });
                    }
                    else if (currentDate.Year >= 2025)
                    {
                        // Pour les tranches futures (2025), utiliser la dernière valeur connue
                        monthlySlices.Add(new StockDataPoint
                        {
                            Date = sliceEnd,
                            Price = lastKnownPrice,
                            Year = sliceEnd.Year
                        });
                        System.Diagnostics.Debug.WriteLine($"Tranche future ajoutée: {sliceEnd:dd/MM/yyyy} avec prix {lastKnownPrice}");
                    }

                    // Passer à la tranche suivante
                    if (slice < 3)
                    {
                        sliceStart = sliceEnd.AddDays(1);
                    }
                }

                // Passer au mois suivant
                currentDate = monthEnd.AddDays(1);
            }

            return monthlySlices;
        }

        /// <summary>
        /// Crée les points de référence annuels spécifiques
        /// </summary>
        private List<StockDataPoint> CreateYearlyReferencePoints(List<StockDataPoint> dataPoints, DateTime startDate, DateTime endDate)
        {
            var yearlyPoints = new List<StockDataPoint>();
            
            // Points de référence annuels spécifiques
            var referenceDates = new[]
            {
                new DateTime(2020, 9, 24), // 24/09/2020 pour le début
                new DateTime(2021, 1, 1),  // 01/01/2021
                new DateTime(2022, 1, 1),  // 01/01/2022
                new DateTime(2023, 1, 1),  // 01/01/2023
                new DateTime(2024, 1, 1),  // 01/01/2024
                new DateTime(2025, 9, 23)  // 23/09/2025 pour la fin
            };

            // Trouver la dernière valeur connue pour l'extension
            var lastKnownPrice = dataPoints.Any() ? dataPoints.OrderByDescending(dp => dp.Date).First().Price : 0.0;

            foreach (var refDate in referenceDates)
            {
                // Trouver le point de données le plus proche de cette date
                var closestPoint = dataPoints
                    .Where(dp => dp.Date <= refDate)
                    .OrderByDescending(dp => dp.Date)
                    .FirstOrDefault();

                if (closestPoint != null)
                {
                    yearlyPoints.Add(new StockDataPoint
                    {
                        Date = refDate,
                        Price = closestPoint.Price,
                        Year = refDate.Year
                    });
                }
                else if (refDate.Year >= 2025)
                {
                    // Pour les dates futures (2025), utiliser la dernière valeur connue
                    yearlyPoints.Add(new StockDataPoint
                    {
                        Date = refDate,
                        Price = lastKnownPrice,
                        Year = refDate.Year
                    });
                }
            }

            return yearlyPoints;
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

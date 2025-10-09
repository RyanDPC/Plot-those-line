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

                var values = jsonData["values"] as object[];
                if (values == null)
                {
                    System.Diagnostics.Debug.WriteLine("Values array is null");
                    return new List<StockDataPoint>();
                }

                var dataPoints = values
            .OfType<Dictionary<string, object>>()
            .Select(item => ParseStockDataPoint(item, startDate, endDate))
            .Filter(dp => dp != null)
            .OrderBy(dp => dp.Date)
            .ToList();

                // Étendre les données jusqu'à la fin de la période si nécessaire
                LogDataPoints(dataPoints, symbol, "avant extension");
                var extendedDataPoints = ExtendDataToFinalDate(dataPoints, symbol);
                LogDataPoints(extendedDataPoints, symbol, "après extension");
                
                // Traitement selon la granularité demandée
                switch (granularity)
                {
                    case DataGranularity.Intraday:
                    case DataGranularity.Daily:
                    case DataGranularity.Weekly:
                        // Pour ces granularités, retourner directement les données
                        return AddYearToDataPoints(extendedDataPoints);
                        
                    case DataGranularity.Monthly:
                        // Créer les tranches mensuelles selon les règles spécifiées
                        return CreateMonthlySlices(extendedDataPoints, startDate, endDate);
                        
                    case DataGranularity.Yearly:
                    default:
                        // Points de référence annuels spécifiques
                        return CreateYearlyReferencePoints(extendedDataPoints, startDate, endDate);
                }
            }
            catch (Exception ex)
            {
                // Retourner vide au lieu d'exception pour ne pas interrompre l'UI
                System.Diagnostics.Debug.WriteLine($"GetStockDataAsync error for {symbol}: {ex.Message}");
                return new List<StockDataPoint>();
            }
        }

        /// <summary>
        /// Helper simple pour étendre les données jusqu'à la date finale
        /// </summary>
        private List<StockDataPoint> ExtendDataToFinalDate(List<StockDataPoint> dataPoints, string symbol)
        {
            // Si pas de données, retourner une liste vide
            if (!dataPoints.Any())
                return dataPoints;

            var finalDate = new DateTime(2025, 9, 23);
            var lastPoint = dataPoints.Last();
            
            System.Diagnostics.Debug.WriteLine($"Vérification extension: dernière date = {lastPoint.Date:dd/MM/yyyy}, date cible = {finalDate:dd/MM/yyyy}");
            
            // Si la dernière date est avant la fin de la période, ajouter un point
            if (lastPoint.Date < finalDate)
            {
                var extendedList = new List<StockDataPoint>(dataPoints);
                extendedList.Add(new StockDataPoint
                {
                    Date = finalDate,
                    Price = lastPoint.Price,
                    Year = finalDate.Year
                });
                System.Diagnostics.Debug.WriteLine($"✅ API EXTENSION: Point ajouté à {finalDate:dd/MM/yyyy} avec prix {lastPoint.Price:F2}");
                return extendedList;
            }
            
            System.Diagnostics.Debug.WriteLine($"ℹ️ API: Pas d'extension nécessaire - données vont déjà jusqu'à {lastPoint.Date:dd/MM/yyyy}");
            return dataPoints;
        }

        /// <summary>
        /// Helper simple pour ajouter l'année à chaque point de données
        /// </summary>
        private List<StockDataPoint> AddYearToDataPoints(List<StockDataPoint> dataPoints)
        {
            return dataPoints
                .ForEachDo(dp => dp.Year = dp.Date.Year)
                .ToList();
        }

        /// <summary>
        /// Récupère les données pour plusieurs symboles de manière simple
        /// </summary>
        public async Task<List<BrandData>> GetMultipleStocksDataAsync(string[] symbols)
        {
            var tasks = symbols.Select(async symbol =>
            {
                try
                {
                    var data = await GetStockDataAsync(symbol);
                    
                    if (data != null && data.Count > 0)
                    {
                        var brandData = new BrandData
                        {
                            Symbol = symbol,
                            Name = symbol,
                            DataPoints = data
                        };
                        
                        System.Diagnostics.Debug.WriteLine($"✅ Données chargées pour {symbol}: {data.Count} points");
                        return brandData;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Aucune donnée pour {symbol}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur pour {symbol}: {ex.Message}");
                    return null;
                }
            });

            var results = await Task.WhenAll(tasks);
            System.Diagnostics.Debug.WriteLine($"🏁 Chargement terminé pour {symbols.Length} symboles");
            
            return results
                .Filter(b => b != null)
                .ToList();
        }


        /// <summary>
        /// Helper simple pour déterminer la granularité optimale selon la période
        /// </summary>
        public static DataGranularity DetermineOptimalGranularity(DateTime startDate, DateTime endDate)
        {
            // Calculer la durée en jours
            var timeSpan = endDate - startDate;
            var totalDays = timeSpan.TotalDays;
            
            // Moins d'une semaine → données horaires
            if (totalDays <= 7)
                return DataGranularity.Intraday;
            
            // Jusqu'à 6 mois → données journalières
            if (totalDays <= 180)
                return DataGranularity.Daily;
            
            // Jusqu'à 2 ans → données hebdomadaires
            if (totalDays <= 730)
                return DataGranularity.Weekly;
            
            // Jusqu'à 5 ans → données mensuelles
            if (totalDays <= 1825)
                return DataGranularity.Monthly;
            
            // Plus de 5 ans → données annuelles
            return DataGranularity.Yearly;
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
        /// Helper simple pour créer les tranches mensuelles
        /// </summary>
        private List<StockDataPoint> CreateMonthlySlices(List<StockDataPoint> dataPoints, DateTime startDate, DateTime endDate)
        {
            var monthlySlices = new List<StockDataPoint>();
            
            // Trouver le dernier prix connu pour les périodes futures
            var lastKnownPrice = 0.0;
            if (dataPoints.Any())
            {
                var lastPoint = dataPoints.OrderByDescending(dp => dp.Date).First();
                lastKnownPrice = lastPoint.Price;
            }

            var currentDate = startDate;

            // Parcourir tous les mois de la période
            while (currentDate <= endDate)
            {
                var monthEnd = GetMonthEndDate(currentDate, endDate);

                // Créer les 4 tranches du mois
                var slices = CreateMonthSlices(currentDate, monthEnd);

                // Projection fonctionnelle avec extensions
                var slicePoints = slices
                    .Select(slice => CreateSliceDataPoint(slice, dataPoints, lastKnownPrice, currentDate.Year))
                    .Filter(dp => dp != null)
                    .ToList();

                monthlySlices.AddRange(slicePoints);

                currentDate = monthEnd.AddDays(1);
            }
            return monthlySlices;
        }

        /// <summary>
        /// Helper pour obtenir la date de fin de mois
        /// </summary>
        private DateTime GetMonthEndDate(DateTime date, DateTime maxDate)
        {
            var monthEnd = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
            
            // Cas spécial : septembre 2025 se termine le 23
            if (date.Year == 2025 && date.Month == 9)
            {
                monthEnd = new DateTime(2025, 9, 23);
            }
            
            return monthEnd;
        }

        /// <summary>
        /// Helper pour créer les 4 tranches d'un mois
        /// </summary>
        private List<(DateTime Start, DateTime End)> CreateMonthSlices(DateTime monthStart, DateTime monthEnd)
        {
            var slices = new List<(DateTime Start, DateTime End)>();
            
            // Tranche 1: 01 → 07
            slices.Add((monthStart, new DateTime(monthStart.Year, monthStart.Month, Math.Min(7, monthEnd.Day))));
            
            // Tranche 2: 08 → 14
            if (monthEnd.Day >= 8)
            {
                slices.Add((
                    new DateTime(monthStart.Year, monthStart.Month, 8),
                    new DateTime(monthStart.Year, monthStart.Month, Math.Min(14, monthEnd.Day))
                ));
            }
            
            // Tranche 3: 15 → 21
            if (monthEnd.Day >= 15)
            {
                slices.Add((
                    new DateTime(monthStart.Year, monthStart.Month, 15),
                    new DateTime(monthStart.Year, monthStart.Month, Math.Min(21, monthEnd.Day))
                ));
            }
            
            // Tranche 4: 22 → fin du mois
            if (monthEnd.Day >= 22)
            {
                slices.Add((
                    new DateTime(monthStart.Year, monthStart.Month, 22),
                    monthEnd
                ));
            }
            
            return slices;
        }

        /// <summary>
        /// Helper simple pour créer les points de référence annuels
        /// </summary>
        private List<StockDataPoint> CreateYearlyReferencePoints(List<StockDataPoint> dataPoints, DateTime startDate, DateTime endDate)
        {
            var yearlyPoints = new List<StockDataPoint>();
            
            // Définir les dates de référence annuelles
            var referenceDates = new[]
            {
                DateTime.Now.AddYears(-5), // Début de période
                DateTime.Now.AddYears(-4),
                DateTime.Now.AddYears(-3),
                DateTime.Now.AddYears(-2),
                DateTime.Now.AddYears(-1),
                DateTime.Now.Add(TimeSpan.FromDays(-1)) // Fin de période
            };

            // Trouver le dernier prix connu pour les dates futures
            var lastKnownPrice = 0.0;
            if (dataPoints.Any())
            {
                var lastPoint = dataPoints.OrderByDescending(dp => dp.Date).First();
                lastKnownPrice = lastPoint.Price;
            }

            // Pour chaque date de référence, trouver le prix correspondant avec extensions
            referenceDates
                .Select(refDate => CreateYearlyDataPoint(refDate, dataPoints, lastKnownPrice))
                .Filter(dp => dp != null)
                .ForEachDo(dp => yearlyPoints.Add(dp));

            return yearlyPoints;
        }

        /// <summary>
        /// Helper pour parser un point de données depuis un dictionnaire
        /// </summary>
        private StockDataPoint ParseStockDataPoint(Dictionary<string, object> item, DateTime startDate, DateTime endDate)
        {
            try
            {
                if (!item.ContainsKey("datetime") || !item.ContainsKey("close"))
                    return null;

                var dateStr = item["datetime"].ToString();
                var closePriceStr = item["close"].ToString();

                var dateParseSuccess = DateTime.TryParse(dateStr, out DateTime date);
                var priceParseSuccess = double.TryParse(
                    closePriceStr,
                    System.Globalization.NumberStyles.Float, // autorise notations décimales/scientifiques et règle l'erreur si espace blanc " 204.24"
                    System.Globalization.CultureInfo.InvariantCulture, // évite les problèmes de séparateur décimal (,/.)
                    out double closePrice
                );

                if (dateParseSuccess && priceParseSuccess && date >= startDate && date <= endDate)
                {
                    return new StockDataPoint
                    {
                        Date = date,
                        Price = closePrice
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur parsing item: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Helper pour logger les points de données
        /// </summary>
        private void LogDataPoints(List<StockDataPoint> dataPoints, string symbol, string phase)
        {
            System.Diagnostics.Debug.WriteLine($"=== DEBUG API {symbol} - Données {phase} ===");
            if (dataPoints.Any())
            {
                System.Diagnostics.Debug.WriteLine($"Nombre de points: {dataPoints.Count}");
                System.Diagnostics.Debug.WriteLine($"Première date: {dataPoints.First().Date:dd/MM/yyyy}");
                System.Diagnostics.Debug.WriteLine($"Dernière date: {dataPoints.Last().Date:dd/MM/yyyy}");
            }
            System.Diagnostics.Debug.WriteLine($"=== FIN DEBUG API {symbol} ===\n");
        }

        /// <summary>
        /// Helper pour créer un point de données pour une tranche mensuelle
        /// </summary>
        private StockDataPoint CreateSliceDataPoint((DateTime Start, DateTime End) slice, List<StockDataPoint> dataPoints, double lastKnownPrice, int currentYear)
        {
            var sliceData = dataPoints
                .Where(dp => dp.Date >= slice.Start && dp.Date <= slice.End)
                .ToList();

            if (sliceData.Count > 0)
            {
                return new StockDataPoint
                {
                    Date = slice.End,
                    Price = sliceData.Average(dp => dp.Price),
                    Year = slice.End.Year
                };
            }
            else if (currentYear >= 2025)
            {
                return new StockDataPoint
                {
                    Date = slice.End,
                    Price = lastKnownPrice,
                    Year = slice.End.Year
                };
            }

            return null;
        }

        /// <summary>
        /// Helper pour créer un point de données annuel
        /// </summary>
        private StockDataPoint CreateYearlyDataPoint(DateTime refDate, List<StockDataPoint> dataPoints, double lastKnownPrice)
        {
            // Chercher le point le plus proche avant ou à cette date
            var closestPoint = dataPoints
                .Where(dp => dp.Date <= refDate)
                .OrderByDescending(dp => dp.Date)
                .FirstOrDefault();

            // Si on a trouvé un point, l'utiliser
            if (closestPoint != null)
            {
                return new StockDataPoint
                {
                    Date = refDate,
                    Price = closestPoint.Price,
                    Year = refDate.Year
                };
            }
            // Pour les dates futures (2025), utiliser le dernier prix connu
            else if (refDate.Year >= 2025 && lastKnownPrice > 0)
            {
                return new StockDataPoint
                {
                    Date = refDate,
                    Price = lastKnownPrice,
                    Year = refDate.Year
                };
            }

            return null;
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

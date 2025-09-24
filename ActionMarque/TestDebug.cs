using System;
using System.Collections.Generic;
using System.Linq;

namespace ActionMarque
{
    public class TestDebug
    {
        public static void TestDataExtension()
        {
            Console.WriteLine("=== TEST D'EXTENSION DES DONNÉES ===");
            
            // Simuler des données qui s'arrêtent en 2024
            var dataPoints = new List<StockDataPoint>
            {
                new StockDataPoint { Date = new DateTime(2024, 12, 31), Price = 250.0, Year = 2024 },
                new StockDataPoint { Date = new DateTime(2024, 12, 30), Price = 248.0, Year = 2024 },
                new StockDataPoint { Date = new DateTime(2024, 12, 29), Price = 245.0, Year = 2024 }
            };
            
            Console.WriteLine($"Données initiales: {dataPoints.Count} points");
            Console.WriteLine($"Première date: {dataPoints.First().Date:dd/MM/yyyy}");
            Console.WriteLine($"Dernière date: {dataPoints.Last().Date:dd/MM/yyyy}");
            
            // Appliquer la logique d'extension
            var endDate = new DateTime(2025, 9, 23);
            var lastPoint = dataPoints.Last();
            
            if (lastPoint.Date < endDate)
            {
                dataPoints.Add(new StockDataPoint
                {
                    Date = endDate,
                    Price = lastPoint.Price,
                    Year = endDate.Year
                });
                Console.WriteLine($"✅ Extension ajoutée: {endDate:dd/MM/yyyy} avec prix {lastPoint.Price}");
            }
            
            Console.WriteLine($"Données finales: {dataPoints.Count} points");
            Console.WriteLine($"Dernière date: {dataPoints.Last().Date:dd/MM/yyyy}");
            Console.WriteLine("=== TEST TERMINÉ ===");
        }
    }
}

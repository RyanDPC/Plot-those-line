using Microsoft.VisualStudio.TestTools.UnitTesting;
using ActionMarque;
using System;

namespace Action
{
    [TestClass]
    public class TwelveDataServiceTests
    {
        [TestMethod]
        public void TestDetermineOptimalGranularity_ShortPeriod_ShouldReturnIntraday()
        {
            // Arrange
            var start = DateTime.Today;
            var end = start.AddDays(5); // 5 jours

            // Act
            var granularity = TwelveDataService.DetermineOptimalGranularity(start, end);

            // Assert
            Assert.AreEqual(DataGranularity.Intraday, granularity);
        }

        [TestMethod]
        public void TestDetermineOptimalGranularity_MediumPeriod_ShouldReturnDaily()
        {
            // Arrange
            var start = DateTime.Today;
            var end = start.AddMonths(3); // ~90 jours

            // Act
            var granularity = TwelveDataService.DetermineOptimalGranularity(start, end);

            // Assert
            Assert.AreEqual(DataGranularity.Daily, granularity);
        }

        [TestMethod]
        public void TestDetermineOptimalGranularity_LongPeriod_ShouldReturnWeekly()
        {
            // Arrange
            var start = DateTime.Today;
            var end = start.AddYears(1); // 365 jours

            // Act
            var granularity = TwelveDataService.DetermineOptimalGranularity(start, end);

            // Assert
            Assert.AreEqual(DataGranularity.Weekly, granularity);
        }

        [TestMethod]
        public void TestDetermineOptimalGranularity_VeryLongPeriod_ShouldReturnMonthly()
        {
            // Arrange
            var start = DateTime.Today;
            var end = start.AddYears(4); 

            // Act
            var granularity = TwelveDataService.DetermineOptimalGranularity(start, end);

            // Assert
            Assert.AreEqual(DataGranularity.Monthly, granularity);
        }

        [TestMethod]
        public void TestDetermineOptimalGranularity_ExtremelyLongPeriod_ShouldReturnYearly()
        {
            // Arrange
            var start = DateTime.Today;
            var end = start.AddYears(10); 

            // Act
            var granularity = TwelveDataService.DetermineOptimalGranularity(start, end);

            // Assert
            Assert.AreEqual(DataGranularity.Yearly, granularity);
        }
    }
}

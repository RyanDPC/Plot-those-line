using Microsoft.VisualStudio.TestTools.UnitTesting;
using ActionMarque;
using System.Collections.Generic;
using System.Linq;

namespace Action
{
    [TestClass]
    public class ExtensionsTests
    {
        [TestMethod]
        public void TestFilter_ShouldReturnMatchingElements()
        {
            // Arrange
            var numbers = new List<int> { 1, 2, 3, 4, 5, 6 };

            // Act
            var evenNumbers = numbers.Filter(n => n % 2 == 0).ToList();

            // Assert
            Assert.AreEqual(3, evenNumbers.Count);
            Assert.IsTrue(evenNumbers.Contains(2));
            Assert.IsTrue(evenNumbers.Contains(4));
            Assert.IsTrue(evenNumbers.Contains(6));
        }

        [TestMethod]
        public void TestForEachDo_ShouldExecuteActionIsExecuted()
        {
            // Arrange
            var numbers = new List<int> { 1, 2, 3 };
            var sum = 0;

            // Act
            numbers.ForEachDo(n => sum += n);

            // Assert
            Assert.AreEqual(6, sum);
        }

        [TestMethod]
        public void TestForEachDo_ShouldReturnOriginalSequence()
        {
            // Arrange
            var numbers = new List<int> { 1, 2, 3 };

            // Act
            var result = numbers.ForEachDo(n => { });

            // Assert
            CollectionAssert.AreEqual(numbers, result.ToList());
        }
    }
}

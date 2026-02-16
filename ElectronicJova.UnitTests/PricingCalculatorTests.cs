using ElectronicJova.Models;
using ElectronicJova.Utilities;
using Xunit;

namespace ElectronicJova.UnitTests
{
    public class PricingCalculatorTests
    {
        [Theory]
        [InlineData(1, 10.00, 9.00, 8.00, 10.00)] // Count <= 50, should use regular Price
        [InlineData(49, 10.00, 9.00, 8.00, 10.00)]
        [InlineData(50, 10.00, 9.00, 8.00, 10.00)]

        [InlineData(51, 10.00, 9.00, 8.00, 9.00)] // Count > 50 and <= 100, should use Price50
        [InlineData(99, 10.00, 9.00, 8.00, 9.00)]
        [InlineData(100, 10.00, 9.00, 8.00, 9.00)]

        [InlineData(101, 10.00, 9.00, 8.00, 8.00)] // Count > 100, should use Price100
        [InlineData(200, 10.00, 9.00, 8.00, 8.00)]
        public void GetPriceBasedOnQuantity_ShouldReturnCorrectPrice(int count, decimal price, decimal price50, decimal price100, decimal expectedPrice)
        {
            // Arrange
            var product = new Product
            {
                Title = "Test Product",
                Price = price,
                Price50 = price50,
                Price100 = price100
            };

            var shoppingCart = new ShoppingCart
            {
                Count = count,
                Product = product
            };

            // Act
            var result = PricingCalculator.GetPriceBasedOnQuantity(shoppingCart);

            // Assert
            Assert.Equal(expectedPrice, result);
        }
    }
}

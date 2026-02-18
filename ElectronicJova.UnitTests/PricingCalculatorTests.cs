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
        public void GetPriceBasedOnQuantity_ShouldReturnCorrectBasePrice(int count, decimal price, decimal price50, decimal price100, decimal expectedPrice)
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

        [Fact]
        public void GetPriceBasedOnQuantity_ShouldIncludeOptionPrices()
        {
            // Arrange
            var product = new Product
            {
                Title = "Test Product",
                Price = 10.00M,
                Price50 = 9.00M,
                Price100 = 8.00M
            };

            // JSON representativo de las opciones seleccionadas
            string optionsJson = "[{\"Name\":\"Garantía\",\"Value\":\"1 año\",\"AdditionalPrice\":5.00},{\"Name\":\"Edición\",\"Value\":\"Metal\",\"AdditionalPrice\":15.00}]";

            var shoppingCart = new ShoppingCart
            {
                Count = 1,
                Product = product,
                SelectedOptions = optionsJson
            };

            // Act
            var result = PricingCalculator.GetPriceBasedOnQuantity(shoppingCart);

            // Assert
            // Base price (10.00) + Options (5.00 + 15.00) = 30.00
            Assert.Equal(30.00M, result);
        }

        [Fact]
        public void GetPriceBasedOnQuantity_WithInvalidJson_ShouldReturnBasePrice()
        {
            // Arrange
            var product = new Product
            {
                Title = "Test Product",
                Price = 10.00M
            };

            var shoppingCart = new ShoppingCart
            {
                Count = 1,
                Product = product,
                SelectedOptions = "Invalid JSON {{"
            };

            // Act
            var result = PricingCalculator.GetPriceBasedOnQuantity(shoppingCart);

            // Assert
            Assert.Equal(10.00M, result);
        }
    }
}

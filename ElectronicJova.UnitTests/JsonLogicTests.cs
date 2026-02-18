using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace ElectronicJova.UnitTests
{
    public class JsonLogicTests
    {
        [Fact]
        public void Serialization_And_Deserialization_ShouldBeCompatible()
        {
            // Simulate what happens in HomeController (POST Details)
            var options = new[]
            {
                new { Name = "Garantía", Value = "1 año", AdditionalPrice = (decimal?)10.00M },
                new { Name = "Edición", Value = "Coleccionista", AdditionalPrice = (decimal?)25.00M }
            };

            string serialized = JsonSerializer.Serialize(options);

            // Simulate what happens in PricingCalculator
            var deserialized = JsonSerializer.Deserialize<List<OptionPrice>>(serialized);

            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.Count);
            Assert.Equal(10.00M, deserialized[0].AdditionalPrice);
            Assert.Equal(25.00M, deserialized[1].AdditionalPrice);
        }

        private class OptionPrice
        {
            public decimal? AdditionalPrice { get; set; }
        }
    }
}

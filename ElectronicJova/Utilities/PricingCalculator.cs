using ElectronicJova.Models;

namespace ElectronicJova.Utilities
{
    public static class PricingCalculator
    {
        public static decimal GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            decimal basePrice = 0;
            if (shoppingCart.Count <= 50)
            {
                basePrice = shoppingCart.Product!.Price;
            }
            else
            {
                if (shoppingCart.Count <= 100)
                {
                    basePrice = shoppingCart.Product!.Price50;
                }
                else
                {
                    basePrice = shoppingCart.Product!.Price100;
                }
            }

            // Calcular precio extra de las opciones
            decimal extraPrice = 0;
            if (!string.IsNullOrEmpty(shoppingCart.SelectedOptions))
            {
                try
                {
                    var options = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<OptionPrice>>(shoppingCart.SelectedOptions);
                    if (options != null)
                    {
                        extraPrice = options.Sum(o => o.AdditionalPrice ?? 0);
                    }
                }
                catch { /* Ignorar errores de deserialización */ }
            }

            return basePrice + extraPrice;
        }

        private class OptionPrice
        {
            public decimal? AdditionalPrice { get; set; }
        }
    }
}

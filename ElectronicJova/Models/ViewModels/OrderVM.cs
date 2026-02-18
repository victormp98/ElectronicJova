using ElectronicJova.Models;
using System.Collections.Generic;

namespace ElectronicJova.Models.ViewModels
{
    public class OrderVM
    {
        public OrderHeader OrderHeader { get; set; } = new();
        public IEnumerable<ShoppingCart> ShoppingCartList { get; set; } = new List<ShoppingCart>();
    }
}

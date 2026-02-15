using ElectronicJova.Models;

namespace ElectronicJova.Models.ViewModels
{
    public class CartVM
    {
        public IEnumerable<ShoppingCart> ShoppingCartList { get; set; }
        public OrderHeader OrderHeader { get; set; }
    }
}

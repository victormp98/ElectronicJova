using System.Collections.Generic;

namespace ElectronicJova.Models.ViewModels
{
    public class DashboardVM
    {
        public decimal TotalSalesToday { get; set; }
        public int PendingOrders { get; set; }
        public int TotalProducts { get; set; }
        public List<Product> TopProducts { get; set; } = new List<Product>();
    }
}

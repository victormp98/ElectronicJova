using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace ElectronicJova.Models.ViewModels
{
    public class ProductVM
    {
        public Product Product { get; set; } = new();
        public IEnumerable<SelectListItem> CategoryList { get; set; } = new List<SelectListItem>();
        public List<ProductOption> ProductOptions { get; set; } = new();
    }
}

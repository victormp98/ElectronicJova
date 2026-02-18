using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using ElectronicJova.Utilities; // Added for PaginatedList

namespace ElectronicJova.Models.ViewModels
{
    public class SearchVM
    {
        public PaginatedList<Product> Products { get; set; } = null!; // Changed to PaginatedList
        public IEnumerable<SelectListItem> CategoryList { get; set; } = new List<SelectListItem>();
        public string? SearchString { get; set; }
        public int? CategoryId { get; set; }
    }
}

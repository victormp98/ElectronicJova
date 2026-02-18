using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicJova.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        [Required]
        public string ISBN { get; set; }
        [Required]
        public string Author { get; set; }

        [Required]
        [Range(1, 10000)] // Example range for ListPrice
        public decimal ListPrice { get; set; } // Precio de lista - sin descuento
        
        [Required]
        [Range(1, 10000)] // Example range for Price
        public decimal Price { get; set; } // Precio para 1-50 unidades
        
        [Required]
        [Range(1, 10000)] // Example range for Price50
        public decimal Price50 { get; set; } // Precio para 50+ unidades
        
        [Required]
        [Range(1, 10000)] // Example range for Price100
        public decimal Price100 { get; set; } // Precio para 100+ unidades

        [Required]
        [Range(0, 10000)]
        public int Stock { get; set; }

        public string? ImageUrl { get; set; } // Nullable as it might be added later

        [Required]
        public int CategoryId { get; set; } // Foreign Key to Category

        [ForeignKey("CategoryId")]
        public Category Category { get; set; } // Navigation property
    }
}

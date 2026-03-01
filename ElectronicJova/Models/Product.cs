using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ElectronicJova.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Modelo")]
        public string Model { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Marca")]
        public string Brand { get; set; } = string.Empty;

        public string? Specifications { get; set; } // Technical specs
        [Display(Name = "Garantía")]
        public string? Warranty { get; set; }       // Warranty info
        
        [Required]
        [Range(1, 1000000)]
        [Display(Name = "Precio de Lista (PVP)")]
        public decimal ListPrice { get; set; } // Precio de lista - sin descuento
        
        [Required]
        [Range(1, 1000000)]
        [Display(Name = "Precio (1-50 unidades)")]
        public decimal Price { get; set; } // Precio para 1-50 unidades
        
        [Required]
        [Range(1, 1000000)]
        [Display(Name = "Precio (51-100 unidades)")]
        public decimal Price50 { get; set; } // Precio para 50+ unidades
        
        [Required]
        [Range(1, 1000000)]
        [Display(Name = "Precio (+100 unidades)")]
        public decimal Price100 { get; set; } // Precio para 100+ unidades

        [Required]
        [Range(0, 1000000)]
        [Display(Name = "Inventario Disponible")]
        public int Stock { get; set; }

        public string? ImageUrl { get; set; } // Nullable as it might be added later

        [Required]
        [Display(Name = "Categoría")]
        public int CategoryId { get; set; } // Foreign Key to Category

        [ForeignKey("CategoryId")]
        [ValidateNever]  // FIX: Prevent model binding from validating this navigation property on POST
        public Category Category { get; set; } = null!; // Navigation property
    }
}

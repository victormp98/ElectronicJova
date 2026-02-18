using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicJova.Models
{
    public class ProductOption
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required]
        [Display(Name = "Nombre de la opción")]
        public string Name { get; set; } = string.Empty; // Ej: "Garantía", "Color", "Accesorios"

        [Required]
        [Display(Name = "Valor de la opción")]
        public string Value { get; set; } = string.Empty; // Ej: "1 año", "Rojo", "Mouse incluido"

        [Display(Name = "Precio adicional")]
        public decimal? AdditionalPrice { get; set; } // Precio extra si aplica (nullable)

        [Display(Name = "Orden de visualización")]
        public int DisplayOrder { get; set; }
    }
}

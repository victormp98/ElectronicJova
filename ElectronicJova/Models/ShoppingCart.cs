using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using ElectronicJova.Data;

namespace ElectronicJova.Models
{
    public class ShoppingCart
    {
        public int Id { get; set; }

        // FK to Product
        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        // FK to ApplicationUser
        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }

        [Required]
        [Range(1, 1000)]
        public int Count { get; set; }

        [NotMapped]
        public decimal Price { get; set; }

        public string? SelectedOptions { get; set; }
        public string? SpecialNotes { get; set; }
    }
}

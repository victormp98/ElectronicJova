using System.ComponentModel.DataAnnotations;

namespace ElectronicJova.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Range(1, 100)] // Example range: Display order must be between 1 and 100
        public int DisplayOrder { get; set; }
    }
}

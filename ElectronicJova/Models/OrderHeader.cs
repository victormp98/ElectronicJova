using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using ElectronicJova.Data;

namespace ElectronicJova.Models
{
    public class OrderHeader
    {
        public int Id { get; set; }

        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public string? ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ShippingDate { get; set; }

        [Required]
        public decimal OrderTotal { get; set; }

        public string? OrderStatus { get; set; }
        public int? OrderStatusValue { get; set; } // Enums for strict validation
        public string? PaymentStatus { get; set; }
        public int? PaymentStatusValue { get; set; } // Enums for strict validation
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }

        public string? SessionId { get; set; }
        public string? PaymentIntentId { get; set; }

        // Shipping details (frozen at time of order)
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre completo")]
        public string? Name { get; set; }
        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [Display(Name = "Teléfono")]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? PhoneNumber { get; set; } // Added for historical accuracy
        [Required(ErrorMessage = "El correo es obligatorio")]
        [Display(Name = "Correo electrónico")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string? Email { get; set; }       // Added for historical accuracy
        [Required(ErrorMessage = "La dirección de calle es obligatoria")]
        [Display(Name = "Dirección de calle")]
        public string? StreetAddress { get; set; }
        [Required(ErrorMessage = "La ciudad es obligatoria")]
        [Display(Name = "Ciudad")]
        public string? City { get; set; }
        [Required(ErrorMessage = "El estado es obligatorio")]
        [Display(Name = "Estado")]
        public string? State { get; set; }
        [Required(ErrorMessage = "El código postal es obligatorio")]
        [Display(Name = "Código Postal")]
        public string? PostalCode { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = new(); // Navigation property
        public List<OrderStatusLog> StatusLogs { get; set; } = new();
    }
}

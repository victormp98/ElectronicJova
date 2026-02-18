using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using ElectronicJova.Data;

namespace ElectronicJova.Models
{
    public class OrderHeader
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime ShippingDate { get; set; }

        [Required]
        public decimal OrderTotal { get; set; }

        public string? OrderStatus { get; set; }
        public int OrderStatusValue { get; set; } // Enums for strict validation
        public string? PaymentStatus { get; set; }
        public int PaymentStatusValue { get; set; } // Enums for strict validation
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }

        public string? SessionId { get; set; }
        public string? PaymentIntentId { get; set; }

        // Shipping details (frozen at time of order)
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; } // Added for historical accuracy
        public string? Email { get; set; }       // Added for historical accuracy
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = new(); // Navigation property
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using ElectronicJova.Data;

namespace ElectronicJova.Models
{
    public class OrderHeader
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime ShippingDate { get; set; }

        [Required]
        public decimal OrderTotal { get; set; }

        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }

        public string? SessionId { get; set; }
        public string? PaymentIntentId { get; set; }

        // Shipping details (frozen at time of order)
        public string? Name { get; set; }
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
    }
}

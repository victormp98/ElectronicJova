namespace ElectronicJova.Utilities
{
    public static class SD
    {
        public const string Role_Customer = "Customer";
        public const string Role_Admin = "Admin";

        // ── Order Status (string constants — backward compatible) ─────────────
        public const string StatusPending = "Pending";
        public const string StatusApproved = "Approved";
        public const string StatusInProcess = "Processing";
        public const string StatusShipped = "Shipped";
        public const string StatusDelivered = "Delivered";
        public const string StatusCancelled = "Cancelled";

        // ── Payment Status ────────────────────────────────────────────────────
        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusRefunded = "Refunded";

        // ── Typed Enums (for type-safe comparisons and progress bar) ──────────
        public enum OrderStatus
        {
            Pending = 0,
            Approved = 1,
            Processing = 2,
            Shipped = 3,
            Delivered = 4,
            Cancelled = 5
        }

        public enum PaymentStatus
        {
            Pending = 0,
            Approved = 1,
            Refunded = 2
        }

        // ── Helper: convert string status to enum ─────────────────────────────
        public static OrderStatus ParseOrderStatus(string? status) => status switch
        {
            StatusApproved  => OrderStatus.Approved,
            StatusInProcess => OrderStatus.Processing,
            StatusShipped   => OrderStatus.Shipped,
            StatusDelivered => OrderStatus.Delivered,
            StatusCancelled => OrderStatus.Cancelled,
            _               => OrderStatus.Pending
        };

        // ── Helper: get display label ─────────────────────────────────────────
        public static string GetOrderStatusLabel(string? status) => status switch
        {
            StatusPending   => "Pendiente",
            StatusApproved  => "Pagado",
            StatusInProcess => "En Proceso",
            StatusShipped   => "Enviado",
            StatusDelivered => "Entregado",
            StatusCancelled => "Cancelado",
            _               => "Desconocido"
        };

        public static string GetOrderStatusIcon(string? status) => status switch
        {
            StatusPending   => "bi-clock",
            StatusApproved  => "bi-credit-card-2-front",
            StatusInProcess => "bi-gear",
            StatusShipped   => "bi-truck",
            StatusDelivered => "bi-check-circle",
            StatusCancelled => "bi-x-circle",
            _               => "bi-question-circle"
        };
    }
}

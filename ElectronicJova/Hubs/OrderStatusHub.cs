using Microsoft.AspNetCore.SignalR;

namespace ElectronicJova.Hubs
{
    /// <summary>
    /// Hub de SignalR para notificaciones de estado de pedidos en tiempo real.
    /// Los clientes se suscriben a su grupo por OrderId.
    /// </summary>
    public class OrderStatusHub : Hub
    {
        /// <summary>
        /// El cliente llama a este método para suscribirse a las actualizaciones de su orden.
        /// </summary>
        public async Task SubscribeToOrder(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
        }

        /// <summary>
        /// El cliente llama a este método para desuscribirse.
        /// </summary>
        public async Task UnsubscribeFromOrder(string orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");
        }
    }
}

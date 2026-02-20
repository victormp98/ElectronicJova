using ElectronicJova.Data.Repository;
using ElectronicJova.Models;

namespace ElectronicJova.Utilities
{
    public static class OrderStatusLogger
    {
        public static async Task LogAsync(
            IUnitOfWork unitOfWork,
            int orderHeaderId,
            string? fromStatus,
            string? toStatus,
            string? changedBy,
            string? notes = null)
        {
            if (fromStatus == toStatus)
            {
                return;
            }

            await unitOfWork.OrderStatusLog.AddAsync(new OrderStatusLog
            {
                OrderHeaderId = orderHeaderId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ChangedBy = changedBy,
                Notes = notes
            });
        }
    }
}

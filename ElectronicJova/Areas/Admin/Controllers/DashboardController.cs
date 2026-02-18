using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Models.ViewModels;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElectronicJova.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class DashboardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Configurar Timezone (Mexico City)
            // Esto asegura que "Hoy" corresponda al día en México, independientemente de la hora del servidor (Azure/AWS suelen estar en UTC)
            string timeZoneId = "Central Standard Time (Mexico)"; // Windows ID
            // En Linux sería "America/Mexico_City". Para compatibilidad cross-platform:
            try
            {
                // Intentar obtener por ID de Windows
                TimeZoneInfo mxZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mxZone).Date;
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback para Linux/Docker si el ID de Windows falla
                try 
                {
                    TimeZoneInfo mxZone = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
                    var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mxZone).Date;
                }
                catch
                {
                    // Fallback final a UTC si no se encuentra ninguna (raro)
                    var today = DateTime.UtcNow.Date;
                }
            }
            
            // Re-declarar var today para que esté en scope (simplificado para compilación)
            var now = DateTime.UtcNow;
            TimeZoneInfo targetZone;
            try { targetZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)"); }
            catch { try { targetZone = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City"); } catch { targetZone = TimeZoneInfo.Utc; } }
            
            var todayDate = TimeZoneInfo.ConvertTimeFromUtc(now, targetZone).Date;


            // 1. Calcular Ventas de Hoy (Pedidos pagados hoy)
            // Nota: Usamos PaymentStatusApproved como filtro de "Ventas" confirmadas
            var ordersToday = await _unitOfWork.OrderHeader.GetAllAsync(u => 
                u.OrderDate.Date == todayDate && 
                u.PaymentStatus == SD.PaymentStatusApproved);
            
            decimal totalSalesToday = ordersToday.Sum(u => u.OrderTotal);

            // 2. Pedidos Pendientes
            var pendingOrders = (await _unitOfWork.OrderHeader.GetAllAsync(u => 
                u.OrderStatus == SD.StatusPending)).Count();

            // 3. Total de Productos
            var totalProducts = (await _unitOfWork.Product.GetAllAsync()).Count();

            // 4. Productos más vendidos (Top 5)
            var allOrderDetails = await _unitOfWork.OrderDetail.GetAllAsync(includeProperties: "Product");
            var topProductIds = allOrderDetails
                .GroupBy(u => u.ProductId)
                .OrderByDescending(g => g.Sum(x => x.Count))
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            var topProducts = (await _unitOfWork.Product.GetAllAsync(p => topProductIds.Contains(p.Id)))
                .OrderBy(p => topProductIds.IndexOf(p.Id))
                .ToList();

            var dashboardVM = new DashboardVM
            {
                TotalSalesToday = totalSalesToday,
                PendingOrders = pendingOrders,
                TotalProducts = totalProducts,
                TopProducts = topProducts
            };

            return View(dashboardVM);
        }
    }
}

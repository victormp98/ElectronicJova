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
            try {
            // 1. Configurar Timezone (Mexico City) con Fallback robusto
            DateTime todayDate = DateTime.UtcNow.Date;
            try
            {
                // Intentar Windows ID
                TimeZoneInfo mxZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)"); 
                todayDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mxZone).Date;
            }
            catch 
            {
                try 
                {
                    // Intentar IANA ID (Linux/Docker)
                    TimeZoneInfo mxZone = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
                    todayDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mxZone).Date;
                }
                catch
                {
                    // Fallback a UTC si falla todo (no debería ocurrir en entornos modernos)
                    todayDate = DateTime.UtcNow.Date;
                }
            }


            // 1. Calcular Ventas de Hoy (Pedidos pagados hoy)
            // Usamos rango de fechas para evitar problemas de traducción de .Date en EF Core con algunos proveedores
            DateTime startOfDay = todayDate;
            DateTime endOfDay = todayDate.AddDays(1);

            var ordersToday = await _unitOfWork.OrderHeader.GetAllAsync(u => 
                u.OrderDate >= startOfDay && 
                u.OrderDate < endOfDay &&
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

            // 5. Ventas de los últimos 7 días para el gráfico
            var last7DaysSalesList = new List<DailySale>();
            for (int i = 6; i >= 0; i--)
            {
                var date = todayDate.AddDays(-i);
                var nextDate = date.AddDays(1);
                var sales = (await _unitOfWork.OrderHeader.GetAllAsync(u => 
                    u.OrderDate >= date && 
                    u.OrderDate < nextDate &&
                    u.PaymentStatus == SD.PaymentStatusApproved)).Sum(u => u.OrderTotal);
                
                last7DaysSalesList.Add(new DailySale { 
                    Date = date.ToString("dd MMM"), 
                    Total = sales 
                });
            }

            var dashboardVM = new DashboardVM
            {
                TotalSalesToday = totalSalesToday,
                PendingOrders = pendingOrders,
                TotalProducts = totalProducts,
                TopProducts = topProducts,
                Last7DaysSales = last7DaysSalesList
            };

                return View(dashboardVM);
            }
            catch (Exception ex)
            {
                // Loguear error (aunque no tengamos ILogger inyectado aquí, prevenimos el crash)
                Console.WriteLine($"Error en Dashboard: {ex.Message}");
                
                // Retornar vista vacía o con datos en cero para no romper la experiencia
                return View(new DashboardVM());
            }
        }
    }
}

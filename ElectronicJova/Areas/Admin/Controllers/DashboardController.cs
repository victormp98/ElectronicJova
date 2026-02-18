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
            var today = DateTime.Today;

            // 1. Calcular Ventas de Hoy (Pedidos pagados hoy)
            // Nota: Usamos PaymentStatusApproved como filtro de "Ventas" confirmadas
            var ordersToday = await _unitOfWork.OrderHeader.GetAllAsync(u => 
                u.OrderDate.Date == today && 
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

            var topProducts = new List<Product>();
            foreach (var productId in topProductIds)
            {
                var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(u => u.Id == productId);
                if (product != null)
                {
                    topProducts.Add(product);
                }
            }

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

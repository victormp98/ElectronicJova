using Microsoft.AspNetCore.Mvc;
using ElectronicJova.Data.Repository;
using System.Linq;

namespace ElectronicJova.Controllers
{
    public class DiagController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public DiagController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var productCount = _unitOfWork.Product.GetAll().Count();
            var categoryCount = _unitOfWork.Category.GetAll().Count();
            var orderCount = _unitOfWork.OrderHeader.GetQueryable().Count();
            
            return Content($"DB Status: Products={productCount}, Categories={categoryCount}, Orders={orderCount}");
        }
    }
}

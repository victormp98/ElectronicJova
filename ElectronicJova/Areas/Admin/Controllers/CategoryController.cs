using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicJova.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Category> objCategoryList = _unitOfWork.Category.GetAll();
            return View(objCategoryList);
        }

        // GET: Upsert
        public IActionResult Upsert(int? id)
        {
            Category category = new Category();

            if (id == null || id == 0)
            {
                // Create
                return View(category);
            }
            else
            {
                // Edit
                var categoryFromDb = _unitOfWork.Category.GetFirstOrDefault(u => u.Id == id);
                if (categoryFromDb == null)
                {
                    return NotFound();
                }
                category = categoryFromDb;
                return View(category);
            }
        }

        // POST: Upsert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Category obj)
        {
            if (ModelState.IsValid)
            {
                if (obj.Id == 0)
                {
                    _unitOfWork.Category.Add(obj);
                }
                else
                {
                    _unitOfWork.Category.Update(obj); // Now this method exists
                }
                _unitOfWork.Save();
                TempData["success"] = "Category created/updated successfully";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        // POST: Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            Category? obj = _unitOfWork.Category.GetFirstOrDefault(u => u.Id == id); // Use nullable type
            if (obj == null)
            {
                TempData["error"] = "Category not found.";
                return NotFound();
            }

            _unitOfWork.Category.Remove(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category deleted successfully";
            return RedirectToAction("Index");
        }
    }
}

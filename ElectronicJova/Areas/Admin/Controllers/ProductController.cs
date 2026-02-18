using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Models.ViewModels;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectListItem
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace ElectronicJova.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            // Eager load Category for display
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return View(objProductList);
        }

        // GET: Upsert
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                Product = new Product(),
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                ProductOptions = new List<ProductOption>()
            };

            if (id == null || id == 0)
            {
                // Create
                return View(productVM);
            }
            else
            {
                // Edit
                var productFromDb = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
                if (productFromDb == null)
                {
                    return NotFound();
                }
                productVM.Product = productFromDb;
                productVM.ProductOptions = _unitOfWork.ProductOption.GetAll(u => u.ProductId == id).OrderBy(u => u.DisplayOrder).ToList();
                return View(productVM);
            }
        }

        // POST: Upsert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ProductVM productVM, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;
                if (file != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("file", "Solo se permiten imágenes (.jpg, .jpeg, .png, .webp, .gif).");
                        productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                        {
                            Text = u.Name,
                            Value = u.Id.ToString()
                        });
                        return View(productVM);
                    }

                    string fileName = Guid.NewGuid().ToString() + ext;
                    string productPath = Path.Combine(wwwRootPath, "images", "products");

                    if (!Directory.Exists(productPath))
                    {
                        Directory.CreateDirectory(productPath);
                    }

                    if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('/', '\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    productVM.Product.ImageUrl = "/images/products/" + fileName;
                }

                if (productVM.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productVM.Product);
                    await _unitOfWork.SaveAsync(); // Save to get the ID for options
                }
                else
                {
                    _unitOfWork.Product.Update(productVM.Product);
                }

                // Handle Product Options
                if (productVM.ProductOptions != null)
                {
                    var existingOptions = _unitOfWork.ProductOption.GetAll(u => u.ProductId == productVM.Product.Id).ToList();
                    
                    // 1. Update or Add
                    foreach (var option in productVM.ProductOptions)
                    {
                        option.ProductId = productVM.Product.Id;
                        if (option.Id == 0)
                        {
                            _unitOfWork.ProductOption.Add(option);
                        }
                        else
                        {
                            _unitOfWork.ProductOption.Update(option);
                        }
                    }

                    // 2. Delete removed options
                    var inputIds = productVM.ProductOptions.Select(u => u.Id).ToList();
                    var optionsToDelete = existingOptions.Where(u => !inputIds.Contains(u.Id)).ToList();
                    if (optionsToDelete.Any())
                    {
                        _unitOfWork.ProductOption.RemoveRange(optionsToDelete);
                    }
                }
                else
                {
                    // If no options passed but product exists, remove all existing options (case: user removed all rows)
                     if (productVM.Product.Id != 0)
                     {
                         var existingOptions = _unitOfWork.ProductOption.GetAll(u => u.ProductId == productVM.Product.Id).ToList();
                         if (existingOptions.Any())
                         {
                             _unitOfWork.ProductOption.RemoveRange(existingOptions);
                         }
                     }
                }

                await _unitOfWork.SaveAsync();
                TempData["success"] = "Product created/updated successfully";
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
            }
            return View(productVM);
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }

        // DELETE
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Product? productToDelete = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
            if (productToDelete == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            if (!string.IsNullOrEmpty(productToDelete.ImageUrl))
            {
            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, productToDelete.ImageUrl.TrimStart('/', '\\'));
            if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _unitOfWork.Product.Remove(productToDelete);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion
    }
}
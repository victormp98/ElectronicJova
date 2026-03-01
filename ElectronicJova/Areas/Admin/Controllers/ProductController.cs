using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Models.ViewModels;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ElectronicJova.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment, ILogger<ProductController> logger)
        {
            _unitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public IActionResult Index()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return View(objProductList);
        }

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
                return View(productVM);
            }

            var productFromDb = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
            if (productFromDb == null)
            {
                return NotFound();
            }

            productVM.Product = productFromDb;
            productVM.ProductOptions = _unitOfWork.ProductOption.GetAll(u => u.ProductId == id).OrderBy(u => u.DisplayOrder).ToList();
            return View(productVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ProductVM productVM, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                if (file != null)
                {
                    _logger.LogInformation(
                        "Saving image locally for ProductId={ProductId}, FileName={FileName}, Size={Size}",
                        productVM.Product.Id,
                        file.FileName,
                        file.Length);

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("file", "Solo se permiten imagenes (.jpg, .jpeg, .png, .webp, .gif).");
                        productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                        {
                            Text = u.Name,
                            Value = u.Id.ToString()
                        });
                        return View(productVM);
                    }

                    var (success, localUrl, error) = await SaveImageLocallyAsync(file);
                    if (!success || string.IsNullOrWhiteSpace(localUrl))
                    {
                        _logger.LogError("Local image save failed for ProductId={ProductId}. Error={Error}", productVM.Product.Id, error);
                        ModelState.AddModelError("", "No fue posible guardar la imagen localmente.");
                        productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                        {
                            Text = u.Name,
                            Value = u.Id.ToString()
                        });
                        return View(productVM);
                    }

                    TryDeleteOldLocalImage(productVM.Product.ImageUrl, localUrl);
                    productVM.Product.ImageUrl = localUrl;
                    _logger.LogInformation("Image assigned to product. Url: {Url}", localUrl);
                }
                else
                {
                    _logger.LogInformation("No new file uploaded. Keeping existing ImageUrl: {Url}", productVM.Product.ImageUrl);
                }

                if (productVM.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productVM.Product);
                    await _unitOfWork.SaveAsync();
                }
                else
                {
                    _unitOfWork.Product.Update(productVM.Product);
                }

                if (productVM.ProductOptions != null)
                {
                    var existingOptions = _unitOfWork.ProductOption.GetAll(u => u.ProductId == productVM.Product.Id).ToList();

                    foreach (var option in productVM.ProductOptions)
                    {
                        option.ProductId = productVM.Product.Id;
                        if (option.Id == 0)
                        {
                            _unitOfWork.ProductOption.Add(option);
                            _logger.LogInformation(
                                "Admin {User} added option {Option} to Product {ProductId}",
                                User.Identity?.Name,
                                option.Name,
                                productVM.Product.Id);
                        }
                        else
                        {
                            _unitOfWork.ProductOption.Update(option);
                        }
                    }

                    var optionsToDelete = existingOptions.Where(u => !inputIds.Contains(u.Id)).ToList();
                    if (optionsToDelete.Any())
                    {
                        _unitOfWork.ProductOption.RemoveRange(optionsToDelete);
                    }
                }
                else if (productVM.Product.Id != 0)
                {
                    var existingOptions = _unitOfWork.ProductOption.GetAll(u => u.ProductId == productVM.Product.Id).ToList();
                    if (existingOptions.Any())
                    {
                        _unitOfWork.ProductOption.RemoveRange(existingOptions);
                    }
                }

                await _unitOfWork.SaveAsync();

                string actionType = productVM.Product.Id == 0 ? "Created" : "Updated";
                _logger.LogInformation(
                    "Admin {User} {Action} Product: {Name} (ID: {Id})",
                    User.Identity?.Name,
                    actionType,
                    productVM.Product.Name,
                    productVM.Product.Id);

                TempData["success"] = "Product created/updated successfully";
                return RedirectToAction("Index");
            }

            productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });
            return View(productVM);
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Product? productToDelete = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
            if (productToDelete == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            var associatedOrders = _unitOfWork.OrderDetail.GetAll(u => u.ProductId == id);
            if (associatedOrders.Any())
            {
                return Json(new { success = false, message = "No se puede eliminar: El producto tiene historico de ventas. Desactivalo en su lugar." });
            }

            if (!string.IsNullOrEmpty(productToDelete.ImageUrl))
            {
                if (productToDelete.ImageUrl.StartsWith("/") || productToDelete.ImageUrl.StartsWith("\\"))
                {
                    var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, productToDelete.ImageUrl.TrimStart('/', '\\'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
            }

            _unitOfWork.Product.Remove(productToDelete);
            _unitOfWork.Save();

            _logger.LogWarning("Admin {User} DELETED Product: {Name} (ID: {Id})", User.Identity?.Name, productToDelete.Name, id);

            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion

        private async Task<(bool Success, string? Url, string? Error)> SaveImageLocallyAsync(IFormFile file)
        {
            try
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid()}{extension}";
                var imagesRoot = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
                Directory.CreateDirectory(imagesRoot);
                var fullPath = Path.Combine(imagesRoot, fileName);

                await using var output = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(output);

                return (true, $"/images/products/{fileName}", null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        private void TryDeleteOldLocalImage(string? existingImageUrl, string? newImageUrl)
        {
            if (string.IsNullOrWhiteSpace(existingImageUrl))
            {
                return;
            }

            if (string.Equals(existingImageUrl, newImageUrl, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!existingImageUrl.StartsWith("/images/products/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relativePath = existingImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_hostEnvironment.WebRootPath, relativePath);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}

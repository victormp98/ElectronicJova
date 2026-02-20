
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
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace ElectronicJova.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment, Cloudinary cloudinary, ILogger<ProductController> logger)
        {
            _unitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;
            _cloudinary = cloudinary;
            _logger = logger;
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
                if (file != null)
                {
                    _logger.LogInformation("Uploading image to Cloudinary for ProductId={ProductId}, FileName={FileName}, Size={Size}",
                        productVM.Product.Id, file.FileName, file.Length);

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

                    using var stream = file.OpenReadStream();
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        PublicId = Guid.NewGuid().ToString(),
                        Overwrite = true
                    };
                    
                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    
                    if (uploadResult.Error != null)
                    {
                        _logger.LogError("Cloudinary Upload Error: {Error}", uploadResult.Error.Message);
                        ModelState.AddModelError("", $"Error al subir imagen: {uploadResult.Error.Message}");
                        productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                        {
                            Text = u.Name,
                            Value = u.Id.ToString()
                        });
                        return View(productVM);
                    }

                    // Delete old image if it exists and is local (optional logic, skipping for now to prioritize Cloudinary)
                    // If moving from local to cloud, we just overwrite the URL.

                    string newUrl = uploadResult.SecureUrl?.AbsoluteUri ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(newUrl))
                    {
                        _logger.LogError("Cloudinary upload returned empty SecureUrl for ProductId={ProductId}", productVM.Product.Id);
                        ModelState.AddModelError("", "Error al subir imagen: Cloudinary no devolvió una URL válida.");
                        productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                        {
                            Text = u.Name,
                            Value = u.Id.ToString()
                        });
                        return View(productVM);
                    }

                    productVM.Product.ImageUrl = newUrl;
                    _logger.LogInformation("Image Uploaded Successfully. New URL: {Url}", newUrl);
                }
                else
                {
                     // Ensure we don't lose the existing image URL if no file is uploaded
                     // This is handled by asp-for="Product.ImageUrl" hidden input, but logging helps verify.
                     _logger.LogInformation("No new file uploaded. Keeping existing ImageUrl: {Url}", productVM.Product.ImageUrl);
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
                            _logger.LogInformation("Admin {User} added option {Option} to Product {ProductId}", User.Identity?.Name, option.Name, productVM.Product.Id);
                        }
                        else
                        {
                            _unitOfWork.ProductOption.Update(option);
                            // _logger.LogInformation("Admin updated option {OptionId}", option.Id); // Too verbose?
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
                
                string actionType = productVM.Product.Id == 0 ? "Created" : "Updated";
                _logger.LogInformation("Admin {User} {Action} Product: {Title} (ID: {Id})", 
                    User.Identity?.Name, actionType, productVM.Product.Title, productVM.Product.Id);

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

            // ── CRITICAL FIX: INTEGRITY CHECK ──
            // Prevent deletion if the product has associated active orders
            var associatedOrders = _unitOfWork.OrderDetail.GetAll(u => u.ProductId == id);
            if (associatedOrders.Any())
            {
                return Json(new { success = false, message = "No se puede eliminar: El producto tiene histórico de ventas. Desactívalo en su lugar." });
            }

            // Optional: Delete from Cloudinary using Public ID derived from URL if needed.
            // For now, we just remove the record.
            
            if (!string.IsNullOrEmpty(productToDelete.ImageUrl))
            {
                // Check if it's a local file and delete it to keep server clean
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
            
            _logger.LogWarning("Admin {User} DELETED Product: {Title} (ID: {Id})", User.Identity?.Name, productToDelete.Title, id);

            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion
    }
}

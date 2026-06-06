using ClashZone.App.Domains;
using ClashZone.App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClashZone.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/products")]
    public class ProductsController : Controller
    {
        private readonly IProductsService _service;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(IProductsService service, IWebHostEnvironment environment)
        {
            _service = service;
            _environment = environment;
        }

        /// <summary>
        /// Zapisuje przesłany plik zdjęcia do wwwroot/images/products i zwraca
        /// publiczną ścieżkę (jak przy zdjęciach profilowych). Null gdy brak pliku.
        /// </summary>
        private async Task<string?> SaveImageAsync(IFormFile? image)
        {
            if (image == null || image.Length == 0) return null;
            var uploadsRoot = Path.Combine(_environment.WebRootPath ?? "wwwroot", "images", "products");
            if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);
            var ext = Path.GetExtension(image.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsRoot, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }
            return $"/images/products/{fileName}";
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var items = await _service.GetAllAsync();
            return View(items);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new ProductDomain { IsActive = true, ClashCoins = 100 });
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDomain model, IFormFile? primaryImage)
        {
            if (!ModelState.IsValid) return View(model);
            var imagePath = await SaveImageAsync(primaryImage);
            await _service.CreateAsync(model, imagePath);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _service.GetAsync(id);
            if (p == null) return NotFound();
            return View(p);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductDomain model, IFormFile? primaryImage)
        {
            if (!ModelState.IsValid) return View(model);
            model.Id = id;
            var imagePath = await SaveImageAsync(primaryImage);
            await _service.UpdateAsync(model, imagePath);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("delete/{id:int}")]
        public IActionResult Delete(int id) => RedirectToAction(nameof(Index));

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelete(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

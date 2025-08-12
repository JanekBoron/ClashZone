using ClashZone.DataAccess.Models;
using ClashZone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClashZone.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/products")]
    public class ProductsController : Controller
    {
        private readonly IProductsService _service;
        public ProductsController(IProductsService service) => _service = service;

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var items = await _service.GetAllAsync();
            return View(items);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new Product { IsActive = true, ClashCoins = 100 });
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, string? primaryImageUrl)
        {
            if (!ModelState.IsValid) return View(model);
            await _service.CreateAsync(model, primaryImageUrl);
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
        public async Task<IActionResult> Edit(int id, Product model, string? primaryImageUrl)
        {
            if (!ModelState.IsValid) return View(model);
            model.Id = id;
            await _service.UpdateAsync(model, primaryImageUrl);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _service.GetAsync(id);
            if (p == null) return NotFound();
            return View(p);
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelete(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

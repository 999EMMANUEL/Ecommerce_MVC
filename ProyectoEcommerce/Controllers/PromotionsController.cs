using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoEcommerce.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PromotionsController : Controller
    {
        private readonly ProyectoEcommerceContext _context;
        public PromotionsController(ProyectoEcommerceContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            return View(await _context.Promotions.OrderByDescending(p => p.StartDate).ToListAsync());
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PromotionId,Name,DiscountPercent,BadgeText,StartDate,EndDate,IsActive")] Promotion promotion)
        {
            if (!ModelState.IsValid) return View(promotion);
            _context.Add(promotion);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Promotions/Poner
        [HttpGet]
        public async Task<IActionResult> Poner()
        {
            ViewBag.Productos = new SelectList(
                await _context.Products.OrderBy(p => p.Name).ToListAsync(),
                "Id", "Name"
            );

            ViewBag.Promociones = new SelectList(
                await _context.Promotions
                    .Where(p => p.IsActive && p.EndDate >= DateTime.UtcNow)
                    .OrderByDescending(p => p.StartDate)
                    .ToListAsync(),
                "PromotionId", "Name"
            );

            return View();
        }

        // POST: Promotions/Poner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Poner(int Id, int PromotionId)
        {
            var producto = await _context.Products.FindAsync(Id);
            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado.";
                return RedirectToAction(nameof(Poner));
            }

            var promo = await _context.Promotions.FindAsync(PromotionId);
            if (promo == null)
            {
                TempData["Error"] = "Promoción no encontrada.";
                return RedirectToAction(nameof(Poner));
            }

            producto.PromotionId = promo.PromotionId;
            _context.Update(producto);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"La promoción '{promo.Name}' fue aplicada a '{producto.Name}'.";
            return RedirectToAction("Index", "Products");
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();
            return View(promo);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PromotionId,Name,DiscountPercent,BadgeText,StartDate,EndDate,IsActive")] Promotion promotion)
        {
            if (id != promotion.PromotionId) return NotFound();
            if (!ModelState.IsValid) return View(promotion);
            try
            {
                _context.Update(promotion);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Promotions.Any(p => p.PromotionId == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();
            return View(promo);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo != null) _context.Promotions.Remove(promo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
